using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System;
using System.Net.Sockets;
using System.Threading;

namespace COMMON
{
	/// <summary>
	/// 
	/// </summary>
	public static class CStreamServer
	{
		#region constants
		private const byte ETX = 0x03;
		private const byte EOT = 0x04;
		private const byte ACK = 0x06;
		private const byte NAK = 0x15;
		private static byte[] STOP_MAIN_SERVER_REQUEST_MESSAGE = { ETX }; // stop the whole server
		private static byte[] STOP_CLIENT_SERVER_REQUEST_MESSAGE = { EOT }; // stop a client thread
		private static byte[] STOP_SERVER_ACCEPT_REPLY_MESSAGE = { ACK };
		private static byte[] STOP_SERVER_DECLINE_REPLY_MESSAGE = { NAK };
		#endregion

		#region classes
		class Client
		{
			#region constructor
			public Client(Thread mainThread, CThread thread, TcpClient client, CStreamServerSettings settings)
			{
				Tcp = client;
				Settings = settings;
				StreamIO = new CStreamServerIO(client, Settings);
				MainThread = mainThread;
				Thread = thread;
			}
			~Client()
			{
				StreamIO.Close();
			}
			#endregion

			#region properties
			public TcpClient Tcp { get; }
			public CStreamServerSettings Settings { get; }
			public CStreamServerIO StreamIO { get; }
			public Thread MainThread { get; }
			public CThread Thread { get; }
			#endregion
		}
		class Clients: List<Client> { }
		#endregion

		#region methods
		/// <summary>
		/// Start a server thread to receive messages
		/// </summary>
		/// <param name="type">The settings to use to start the server</param>
		/// <param name="sync">Indicates whether the server must act synchronously or not.
		/// A synchronous server forwards replies inside the same thread, preventing receiving messages in the meantime.
		/// An asynchronous server forwards replies inside another thread, allowing receiving messages at the same time.</param>
		/// <returns>A <see cref="CThread"/> object if the thread has been started, null otherwise</returns>
		public static CThread StartServer(StartServerType type, bool sync)
		{
			if (null == type || !type.IsValid)
				return null;
			try
			{
				// prepare the thread object
				CThread thread = new CThread();
				bool f;
				if (sync)
					f = thread.Start(SyncStreamServerMethod, type.ThreadData, new StreamServerMethodType() { startServerType = type, });
				else
					f = thread.Start(AsyncStreamServerMainMethod, type.ThreadData, new StreamServerMethodType() { startServerType = type, });
				if (f)
					return thread;
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
			return null;
		}
		public class StartServerType
		{
			#region properties
			public bool IsValid { get => (null != Settings && Settings.IsValid) && (null != ThreadData && ThreadData.IsValid); }
			/// <summary>
			/// Thread data to use to identify the thread
			/// </summary>
			public CThreadData ThreadData { get; set; } = null;
			/// <summary>
			/// Server 
			/// </summary>
			public CStreamServerSettings Settings { get; set; } = null;
			/// <summary>
			/// Called before starting processing requests from a client.
			/// This function allows to initialise the server context.
			/// </summary>
			public CStreamDelegates.ServerOnStartDelegate OnStart { get; set; } = null;
			/// <summary>
			/// Called when a client connected to the server.
			/// This function allows to initialise the client context inside the server.
			/// </summary>
			public CStreamDelegates.ServerOnConnectDelegate OnConnect { get; set; } = null;
			/// <summary>
			/// Called when a request has been received to process it and prepare the reply
			/// </summary>
			public CStreamDelegates.ServerOnRequestDelegate OnRequest { get; set; } = null;
			/// <summary>
			/// Called after the server has received a stop order.
			/// This function allows to clear the server context.
			/// </summary>
			public CStreamDelegates.ServerOnStopDelegate OnStop { get; set; } = null;
			/// <summary>
			/// Private parameters passed to the thread
			/// </summary>
			public object Parameters { get; set; }
			#endregion

			#region methods
			public static StartServerType Prepare(CStreamServerSettings settings, CThreadData threadData, CStreamDelegates.ServerOnRequestDelegate onRequest, CStreamDelegates.ServerOnStartDelegate onStart = null, CStreamDelegates.ServerOnConnectDelegate onConnect = null, CStreamDelegates.ServerOnStopDelegate onStop = null, object parameters = null)
			{
				return new StartServerType()
				{
					ThreadData = threadData,
					Settings = settings,
					OnStart = onStart,
					OnConnect = onConnect,
					OnRequest = onRequest,
					OnStop = onStop,
					Parameters = parameters,
				};
			}
			#endregion
		}
		/// <summary>
		/// <see cref="CThread.CThreadFunction"/>
		/// Server thread processing all incoming messages.
		/// When a message is received it is transfered to the server for processing, then looping on receiving next message.
		/// Exiting the server loop is instructed by the server by returning FALSE after having processed a message.
		/// </summary>
		/// <param name="threadData"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		[ComVisible(false)]
		private static int SyncStreamServerMethod(CThreadData threadData, object o)
		{
			bool fOK = true;
			StreamServerMethodType serverThread = (StreamServerMethodType)o;
			// create a TCP/IP socket and start listenning for incoming connections
			CLog.Add("Starting server using Connecting port: " + serverThread.startServerType.Settings.Port);
			// start the server ?
			if (null != serverThread.startServerType.OnStart)
				fOK = serverThread.startServerType.OnStart(serverThread.startServerType.ThreadData, serverThread.startServerType.Parameters);
			if (fOK)
			{
				// start listening connections to server
				TcpListener listener = new TcpListener(IPAddress.Any, (int)serverThread.startServerType.Settings.Port);
				try
				{
					listener.Start();
					bool keepOnRunning = true;
					while (keepOnRunning)
					{
						// accept client connection
						TcpClient client = listener.AcceptTcpClient();
						CLog.Add("Connection from: " + client.Client.RemoteEndPoint.ToString());
						client.SendTimeout = CStreamSettings.NOTIMEOUT <= serverThread.startServerType.Settings.SendTimeout ? serverThread.startServerType.Settings.SendTimeout * CStreamSettings.ONESECOND : CStreamSettings.NOTIMEOUT;
						client.ReceiveTimeout = CStreamSettings.NOTIMEOUT <= serverThread.startServerType.Settings.ReceiveTimeout ? serverThread.startServerType.Settings.ReceiveTimeout * CStreamSettings.ONESECOND : CStreamSettings.NOTIMEOUT;
						try
						{
							// initiate the stream to use for communication
							CStreamServerIO server = new CStreamServerIO(client, serverThread.startServerType.Settings);
							try
							{
								bool addSizeHeader = true;
								byte[] reply = null;
								byte[] request = server.Receive(out int size);
								// test if shutdown order
								if (ArraysAreEqual(STOP_MAIN_SERVER_REQUEST_MESSAGE, request))
								{
									if (null == serverThread.startServerType.OnStop
										|| serverThread.startServerType.OnStop(client, serverThread.startServerType.ThreadData, serverThread.startServerType.Parameters))
									{
										CLog.Add("Stop request accepted");
										// shutdown order, reply the server is stopping
										reply = new byte[STOP_SERVER_ACCEPT_REPLY_MESSAGE.Length];
										Buffer.BlockCopy(STOP_SERVER_ACCEPT_REPLY_MESSAGE, 0, reply, 0, STOP_SERVER_ACCEPT_REPLY_MESSAGE.Length);
										addSizeHeader = true;
										keepOnRunning = false;
									}
									else
									{
										CLog.Add("Stop request declined");
										// shutdown order, reply the server is stopping
										reply = new byte[STOP_SERVER_DECLINE_REPLY_MESSAGE.Length];
										Buffer.BlockCopy(STOP_SERVER_DECLINE_REPLY_MESSAGE, 0, reply, 0, STOP_SERVER_DECLINE_REPLY_MESSAGE.Length);
										addSizeHeader = true;
										keepOnRunning = true;
									}
								}
								else
								{
									// call a function inside server context to build the response
									if (null != serverThread.startServerType.OnRequest)
										reply = serverThread.startServerType.OnRequest(client, request, out addSizeHeader, serverThread.startServerType.ThreadData, serverThread.startServerType.Parameters);
								}
								server.Send(reply, addSizeHeader);
							}
							catch (Exception ex)
							{
								CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
							}
							finally
							{
								server.Close();
							}
						}
						catch (Exception ex)
						{
							CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
						}
						finally
						{
							client.Close();
						}
					}
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				}
				finally
				{
					listener.Stop();
				}
			}
			else
				CLog.Add("Immediate stop requested", TLog.WARNG);
			CLog.Add("Shutting down server");
			return 0;
		}
		class StreamServerMethodType
		{
			public StartServerType startServerType { get; set; }
		}
		/// <summary>
		/// <see cref="CThread.CThreadFunction"/>
		/// Server thread processing all incoming messages.
		/// When a message is received it is transfered to the server for processing, then looping on receiving next message.
		/// Exiting the server loop is instructed by the server by a returning FALSE after having processed a message.
		/// </summary>
		/// <param name="threadData"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		[ComVisible(false)]
		private static int AsyncStreamServerMainMethod(CThreadData threadData, object o)
		{
			bool fOK = true;
			StreamServerMethodType serverThread = (StreamServerMethodType)o;
			// create a TCP/IP socket and start listenning for incoming connections
			CLog.Add("Using Connecting port: " + serverThread.startServerType.Settings.Port);
			// start the server ?
			if (null != serverThread.startServerType.OnStart)
				fOK = serverThread.startServerType.OnStart(serverThread.startServerType.ThreadData, serverThread.startServerType.Parameters);
			if (fOK)
			{
				// start listening connections to server
				TcpListener listener = new TcpListener(IPAddress.Any, (int)serverThread.startServerType.Settings.Port);
				try
				{
					listener.Start();
					Clients clients = new Clients();
					try
					{
						while (true)
						{
							fOK = true;
							// accept client connection
							TcpClient tcp = listener.AcceptTcpClient();
							CLog.Add("Connection from: " + tcp.Client.RemoteEndPoint.ToString());
							if (null != serverThread.startServerType.OnConnect)
								fOK = serverThread.startServerType.OnConnect(tcp, null != serverThread.startServerType.Settings.ServerCertificate, serverThread.startServerType.ThreadData, serverThread.startServerType.Parameters);
							if (fOK)
							{
								tcp.SendTimeout = 0 <= serverThread.startServerType.Settings.SendTimeout ? serverThread.startServerType.Settings.SendTimeout * CStreamSettings.ONESECOND : Timeout.Infinite;
								tcp.ReceiveTimeout = 0 <= serverThread.startServerType.Settings.ReceiveTimeout ? serverThread.startServerType.Settings.ReceiveTimeout * CStreamSettings.ONESECOND : Timeout.Infinite;
								Client client = new Client(Thread.CurrentThread, new CThread(), tcp, serverThread.startServerType.Settings);
								// start a thread to process messages from this client
								if (client.Thread.Start(AsyncStreamServerClientMethod, serverThread.startServerType.ThreadData, new ServerThreadClientType() { startServerType = serverThread.startServerType, Client = client }))
									clients.Add(client);
							}
							else
								CLog.Add("Connection from " + tcp.Client.RemoteEndPoint.ToString() + " has been refused", TLog.WARNG);
						}
					}
					catch (Exception ex)
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
					}
					finally
					{
						CLog.Add("Closing server, ending all live connections");
						//stop all client threads
						foreach (Client c in clients)
							CStreamServer.StopClientServer(c.StreamIO);
						clients.Clear();
					}
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				}
				finally
				{
					listener.Stop();
				}
			}
			else
				CLog.Add("Immediate stop requested by caller", TLog.WARNG);
			CLog.Add("Shutting down");
			return 0;
		}
		/// <summary>
		/// <see cref="CThread.CThreadFunction"/>
		/// Server thread processing all incoming messages.
		/// When a message is received it is transfered to the server for processing, then looping on receiving next message.
		/// Exiting the server loop is instructed by the server by a returning FALSE after having processed a message.
		/// </summary>
		/// <param name="threadData"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		[ComVisible(false)]
		private static int AsyncStreamServerClientMethod(CThreadData threadData, object o)
		{
			int res = 0;
			ServerThreadClientType serverThread = (ServerThreadClientType)o;
			// create a TCP/IP socket and start listenning for incoming connections
			// start listening incoming messages
			bool keepOnRunning = true;
			while (keepOnRunning)
			{
				try
				{
					bool addSizeHeader = true;
					byte[] outgoing = null;
					byte[] incoming = serverThread.Client.StreamIO.Receive(out int size);
					if (0 != incoming.Length)
					{
						// a message has been received and needs to be processed

						// test if shutdown order
						if (ArraysAreEqual(STOP_MAIN_SERVER_REQUEST_MESSAGE, incoming))
						{
							CLog.Add("Stop main server request declined");
							// shutdown order, reply the server is stopping
							outgoing = new byte[STOP_SERVER_DECLINE_REPLY_MESSAGE.Length];
							Buffer.BlockCopy(STOP_SERVER_DECLINE_REPLY_MESSAGE, 0, outgoing, 0, STOP_SERVER_DECLINE_REPLY_MESSAGE.Length);
							addSizeHeader = true;
							keepOnRunning = true;
						}
						else if (ArraysAreEqual(STOP_CLIENT_SERVER_REQUEST_MESSAGE, incoming))
						{
							if (null == serverThread.startServerType.OnStop
								|| (null != serverThread.startServerType.OnStop
								&& serverThread.startServerType.OnStop(serverThread.Client.Tcp, serverThread.startServerType.ThreadData, serverThread.startServerType.Parameters)))
							{
								CLog.Add("Stop request accepted");
								// shutdown order, reply the server is stopping
								outgoing = new byte[STOP_SERVER_ACCEPT_REPLY_MESSAGE.Length];
								Buffer.BlockCopy(STOP_SERVER_ACCEPT_REPLY_MESSAGE, 0, outgoing, 0, STOP_SERVER_ACCEPT_REPLY_MESSAGE.Length);
								addSizeHeader = true;
								keepOnRunning = false;
							}
							else
							{
								CLog.Add("Stop request declined");
								// shutdown order, reply the server is stopping
								outgoing = new byte[STOP_SERVER_DECLINE_REPLY_MESSAGE.Length];
								Buffer.BlockCopy(STOP_SERVER_DECLINE_REPLY_MESSAGE, 0, outgoing, 0, STOP_SERVER_DECLINE_REPLY_MESSAGE.Length);
								addSizeHeader = true;
								keepOnRunning = true;
							}
						}
						else
						{
							// got a request, put it on the stack of messages to process

							// call a function inside server context to build the response
							if (null != serverThread.startServerType.OnRequest)
								outgoing = serverThread.startServerType.OnRequest(serverThread.Client.Tcp, incoming, out addSizeHeader, serverThread.startServerType.ThreadData, serverThread.startServerType.Parameters);
						}
						serverThread.Client.StreamIO.Send(outgoing, addSizeHeader);
					}
					else
					{
						// no message received, might be an exception because the socket was closed, anyway we must shutdown the thread
						throw new ServerThreadClientStop();
					}
				}
				catch (ServerThreadClientStop ex)
				{
					CLog.Add("Stream closed by client, thread terminated internally");
					keepOnRunning = false;
					res = 0;
				}
				catch (ThreadAbortException ex)
				{
					CLog.Add("Thread terminated externally");
					keepOnRunning = false;
					res = int.MaxValue;
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
					res = -1;
				}
			}
			// release server resources
			serverThread.Client.StreamIO.Close();
			CLog.Add("Shutting down");
			return res;
		}
		class ServerThreadClientType
		{
			public Client Client { get; set; }
			public StartServerType startServerType { get; set; }
		}
		class ServerThreadClientStop: Exception
		{
			/// <summary>
			/// Details the exception
			/// </summary>
			public ServerThreadClientStop() { }
		}
		/// <summary>
		/// Test 2 array of bytes are equal
		/// </summary>
		/// <param name="b1">First array of bytes</param>
		/// <param name="b2">Second array of bytes</param>
		/// <returns>TRUE if equal, FALSE otherwise</returns>
		private static bool ArraysAreEqual(byte[] b1, byte[] b2)
		{
			bool fOK;
			if (fOK = b1.Length == b2.Length)
				for (int i = 0; i < b1.Length && fOK; i++)
					fOK = b1[i] == b2[i];
			return fOK;
		}
		/// <summary>
		/// Stop a server serving a specific client.
		/// The process must already be connected to the server and use its connection.
		/// </summary>
		/// <param name="streamIO">The stream used by the client to reach the server</param>
		/// <returns>TRUE if the server has been stoppped, FALSE otherwise</returns>
		public static bool StopClientServer(CStreamIO streamIO)
		{
			bool fOK = CStreamServer.Stop(streamIO, STOP_CLIENT_SERVER_REQUEST_MESSAGE, out int replySize, out bool timeout);
			if (fOK)
				CStream.Disconnect(streamIO);
			return fOK;
		}
		/// <summary>
		/// Stop the server itself, stopping all existing clients' servers.
		/// The process must already be connected to the server and use its connection.
		/// </summary>
		/// <param name="streamIO">The stream to use to reach the server</param>
		/// <returns>TRUE if the server has been stoppped, FALSE otherwise</returns>
		public static bool StopMainServer(CStreamIO streamIO)
		{
			return CStreamServer.Stop(streamIO, STOP_MAIN_SERVER_REQUEST_MESSAGE, out int replySize, out bool timeout);
		}
		/// <summary>
		/// Stop a server (main server or client's server).
		/// </summary>
		/// <param name="streamIO">Stream settings to use to reach the server</param>
		/// <param name="msg">Message to send to stop the thread</param>
		/// <param name="replySize">Message to send to stop the thread</param>
		/// <param name="timeout">Message to send to stop the thread</param>
		/// <returns>TRUE if the server has been stoppped, FALSE otherwise</returns>
		private static bool Stop(CStreamIO streamIO, byte[] msg, out int replySize, out bool timeout)
		{
			replySize = 0;
			timeout = false;
			// Send a stop message to the server on the existing channel
			if (null != streamIO && CStream.Send(streamIO, msg, true))
			{
				byte[] reply = CStream.Receive(streamIO, out replySize, out timeout, true);
				return (null != reply && replySize == reply.Length && ACK == reply[0]);
			}
			return false;
		}
		/// <summary>
		/// Stop the server itself, stopping all existing clients' servers.
		/// The process doesn't have to be connected prior to requesting server shutdown.
		/// The function will try to connect to the server then send a shutdown request.
		/// </summary>
		/// <param name="settings">Stream settings to use to reach the server</param>
		/// <returns>TRUE if the server has been stoppped, FALSE otherwise</returns>
		public static bool StopServer(CStreamClientSettings settings)
		{
			if (!settings.IsValid)
				return false;
			// Send a stop message to the server
			byte[] reply = CStream.ConnectSendReceive(settings, STOP_MAIN_SERVER_REQUEST_MESSAGE, out int replySize, out bool timeout, true);
			// test the reply
			return (null != reply && replySize == reply.Length && ACK == reply[0]);
		}
		/// <summary>
		/// Stop the server itself, stopping all existing clients' servers.
		/// The process doesn't have to be connected prior to requesting server shutdown.
		/// The function will try to connect to the server then send a shutdown request.
		/// Stop the server side
		/// </summary>
		/// <param name="type">Settings to start the thread requesting to stop the server</param>
		/// <returns>TRUE if the server has been stoppped, FALSE otherwise</returns>
		public static CThread StopServerAsync(StopServerAsyncType type)
		{
			return CStream.SendAsync(
				new CStream.SendAsyncType()
				{
					Settings = type.Settings,
					ThreadData = type.ThreadData,
					OnReply = type.OnReply,
					Parameters = null
				},
				STOP_MAIN_SERVER_REQUEST_MESSAGE,
				true);
		}
		public class StopServerAsyncType
		{
			#region properties
			public CThreadData ThreadData { get; set; }
			public CStreamClientSettings Settings { get; set; }
			public CStreamDelegates.ClientOnReplyDelegate OnReply { get; set; }
			#endregion

			#region methods
			public static StopServerAsyncType Prepare(CStreamClientSettings settings, CThreadData threadData, CStreamDelegates.ClientOnReplyDelegate onReply = null)
			{
				return new StopServerAsyncType()
				{
					Settings = settings,
					ThreadData = threadData,
					OnReply = onReply
				};
			}
			#endregion
		}
		#endregion
	}
}
