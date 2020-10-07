using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.IO;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.ObjectModel;

namespace COMMON
{
	[ComVisible(true)]
	public enum StreamServerResult
	{
		_begin = ThreadResult._end,
		serverIsStarting,
		serverFailedToStart,
		serverStartDeclined,
		serverIsStopping,
		clientConnectionDeclined,
		clientStartFailed,
		clientReceivedInvalidMessage,
		_end,
	}

	/// <summary>
	/// Structure to use to start a <see cref="CStreamServer"/>
	/// </summary>
	[ComVisible(false)]
	public class CStreamServerStartSettings
	{
		#region properties
		public bool IsValid { get => (null != StreamServerSettings && StreamServerSettings.IsValid) && (null != ThreadData && ThreadData.IsValid) && null != OnMessage; }
		/// <summary>
		/// Synchrounous server (1 thread) or not (1 main thread + 1 thread per client)
		/// </summary>
		public bool Synchronous { get; set; } = true;
		/// <summary>
		/// Thread data to use to identify the thread
		/// </summary>
		public CThreadData ThreadData { get; set; } = null;
		/// <summary>
		/// Server 
		/// </summary>
		public CStreamServerSettings StreamServerSettings { get; set; } = null;
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
		public CStreamDelegates.ServerOnMessageDelegate OnMessage { get; set; } = null;
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
		public static CStreamServerStartSettings Prepare(CStreamServerSettings streamServerSettings, CThreadData threadData, CStreamDelegates.ServerOnMessageDelegate onMessage, CStreamDelegates.ServerOnStartDelegate onStart = null, CStreamDelegates.ServerOnConnectDelegate onConnect = null, CStreamDelegates.ServerOnStopDelegate onStop = null, object parameters = null)
		{
			return new CStreamServerStartSettings()
			{
				ThreadData = threadData,
				StreamServerSettings = streamServerSettings,
				OnStart = onStart,
				OnConnect = onConnect,
				OnMessage = onMessage,
				OnStop = onStop,
				Parameters = parameters,
			};
		}
		#endregion
	}

	/// <summary>
	/// Server processing implementation
	/// </summary>
	[ComVisible(false)]
	public class CStreamServer: CThread
	{
		#region constructor
		public CStreamServer() { }
		#endregion

		#region private properties
		/// <summary>
		/// Copy of start server settings
		/// </summary>
		private CStreamServerStartSettings streamServerStartSettings { get; set; }
		/// <summary>
		/// All clients connected to the server
		/// </summary>
		private object myLock = new object();
		private TcpListener listener = null;
		private CThreadEvents listenerEvents = new CThreadEvents();
		private Clients connectedClients = new Clients();
		private Mutex isCleaningUpMutex = new Mutex(false);
		#endregion

		#region constants
		private const byte EOT = 0x04;
		private const byte ACK = 0x06;
		private const byte NAK = 0x15;
		private static byte[] STOP_SERVER_CLIENT_THREAD_REQUEST_MESSAGE = { EOT }; // stop a client thread
		private static byte[] STOP_SERVER_ACCEPT_REPLY_MESSAGE = { ACK };
		private static byte[] STOP_SERVER_DECLINE_REPLY_MESSAGE = { NAK };
		#endregion

		#region public methods
		/// <summary>
		/// Start a server thread to receive messages
		/// </summary>
		/// <param name="settings">The settings to use to start the server
		/// A synchronous server forwards replies inside the same thread, preventing receiving messages in the meantime.
		/// An asynchronous server forwards replies inside another thread, allowing receiving messages at the same time.</param>
		/// <returns>True if started, false otherwise</returns>
		public bool StartServer(CStreamServerStartSettings settings)
		{
			const string SERVER_NOT_RUNNING = ", server is not running";
			if (!CanStart)
				return false;
			if (null == settings || !settings.IsValid)
				return false;

			streamServerStartSettings = settings;
			connectedClients.Clear();
			try
			{
				// verify wether starting the server is accepted or not
				if (null == streamServerStartSettings.OnStart
					|| streamServerStartSettings.OnStart(streamServerStartSettings.ThreadData, streamServerStartSettings.Parameters))
				{
					// create a TCP/IP socket and start listenning for incoming connections
					listener = new TcpListener(IPAddress.Any, (int)streamServerStartSettings.StreamServerSettings.Port);
					try
					{
						CLog.Add(Description + "Server listener created at " + listener.LocalEndpoint.ToString());
						//listenerEvents.Reset();
						listener.Start();
						try
						{
							// start the thread and sleep to allow him to actually run
							if (Start(StreamServerListenerMethod, streamServerStartSettings.ThreadData, settings.Parameters, listenerEvents.Started))
							{
								return true;
							}
							else
							{
								CLog.Add(Description + "Server thread could not be created" + SERVER_NOT_RUNNING);
							}
						}
						catch (Exception ex)
						{
							CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Starting the server thread generated an exception" + SERVER_NOT_RUNNING);
						}
						// arrived here we can stop the listener
						listener.Stop();
					}
					catch (Exception ex)
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Network listener counld not be started on " + listener.LocalEndpoint.ToString() + SERVER_NOT_RUNNING);
					}
					// arrived here we can dismiss the listener
					listener = null;
				}
				else
				{
					CLog.Add(Description + "The server was not allowed to start", TLog.WARNG);
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Server.OnStart generated an exception. " + SERVER_NOT_RUNNING);
			}
			streamServerStartSettings = null;
			return false;
		}
		/// <summary>
		/// Stop the server 
		/// </summary>
		/// <returns>TRUE if the server has been stopped or did not exist, FALSE otherwise</returns>
		public void StopServer()
		{
			if (IsRunning)
			{
				// clean up and synchronize thread termination
				Cleanup();
				Wait();
			}
		}
		/// <summary>
		/// Stop the server from an external object already connected.
		/// </summary>
		/// <param name="stream"><see cref="CStreamIO"/> stream to the server</param>
		/// <returns>True if stopped, false otherwise</returns>
		private static bool StopServer(CStreamIO stream)
		{
			// Send a stop message to the server on the existing channel
			byte[] reply = CStream.SendReceive(stream,
				STOP_SERVER_CLIENT_THREAD_REQUEST_MESSAGE,
				true, out int replySize, out bool timeout);
			return (!timeout && null != reply && replySize == reply.Length && ACK == reply[0]);
		}
		#endregion

		#region private methods
		/// <summary>
		/// Clean up server context
		/// </summary>
		private void Cleanup()
		{
			if (isCleaningUpMutex.WaitOne(0))
			{
				// stop listener (that will stop the thread waiting for clients)
				if (null != listener)
				{
					CLog.Add("Shutting down listener");
					listener.Stop();
					listenerEvents.WaitStopped();
					listener = null;
				}
				// wait for the listener to be closed
				//stop all client threads
				lock (myLock)
				{
					try
					{
						if (0 != connectedClients.Count)
						{
							foreach (KeyValuePair<string, Client> c in connectedClients)
								try
								{
									c.Value.Stop();
								}
								catch (Exception) { }
						}
					}
					catch (Exception ex)
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Cleaning up the server generated an exception");
					}
				}
				connectedClients.Clear();
				// warn the server is stopping
				try
				{
					streamServerStartSettings.OnStop?.Invoke(streamServerStartSettings.ThreadData, streamServerStartSettings.Parameters);
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Server.OnStop generated an exception");
				}
				//CThread.SendNotification(ThreadData, ID, 0, true);
			}
		}
		/// <summary>
		/// <see cref="CThread.CThreadFunction"/>
		/// Server thread processing all incoming connections.
		/// When a connection is approved a set of threads is created to (first thread) receive messages (second thread) process these messages
		/// in an asynchronous way (without preventing connections or message reception)
		/// </summary>
		/// <param name="threadData"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		private int StreamServerListenerMethod(CThreadData threadData, object o)
		{
			string threadName = Description + "LISTENER - ";
			int res = (int)ThreadResult.UNKNOWN;
			bool keepOnRunning = true;
			// indicate listener is on
			listenerEvents.SetStarted();
			while (keepOnRunning)
			{
				try
				{
					// accept client connection
					TcpClient tcp = listener.AcceptTcpClient();
					// verify if connection is accepted from that ip address
					if (null == streamServerStartSettings.OnConnect
						|| streamServerStartSettings.OnConnect(tcp, streamServerStartSettings.ThreadData, streamServerStartSettings.Parameters))
					{
						tcp.SendTimeout = streamServerStartSettings.StreamServerSettings.SendTimeout * CStreamSettings.ONESECOND;
						tcp.ReceiveTimeout = streamServerStartSettings.StreamServerSettings.ReceiveTimeout * CStreamSettings.ONESECOND;
						Client client = new Client(tcp, streamServerStartSettings.StreamServerSettings);

						// start the processing and reader threads to process messages from this client
						if (client.ReceivingThread.Start(StreamServerReceiverMethod, streamServerStartSettings.ThreadData, client, client.ReceiverEvents.Started))
						{
							if (client.ProcessingThread.Start(StreamServerProcessorMethod, streamServerStartSettings.ThreadData, client, client.ProcessorEvents.Started))
							{
								lock (myLock)
								{
									CLog.Add(threadName + "Client: " + tcp.Client.RemoteEndPoint.ToString() + " is connected to the server");
									connectedClients.Add(client.Key, client);
								}
							}
							else
							{
								CLog.Add(threadName + "Failed to start processor thread for client " + tcp.Client.RemoteEndPoint.ToString(), TLog.ERROR);
								client.StopReceivingThread();
							}
						}
						else
						{
							CLog.Add(threadName + "Failed to start receiver thread for client " + tcp.Client.RemoteEndPoint.ToString(), TLog.ERROR);
						}
					}
					else
					{
						CLog.Add(threadName + "Connection from " + tcp.Client.RemoteEndPoint.ToString() + " has been refused", TLog.WARNG);
					}
				}
				catch (Exception ex)
				{
					if (ex is SocketException)
					{
						res = (int)ThreadResult.OK;
					}
					if (ex is ObjectDisposedException)
					{
						CLog.Add(threadName + "Socket disposed, shutting down");
						res = (int)ThreadResult.OK;
					}
					else
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
						res = (int)ThreadResult.Exception;
					}
					keepOnRunning = false;
				}
			}
			// server cleanup
			listenerEvents.SetStopped();
			// indicate the listener is down
			listener = null;
			Cleanup();
			return res;
		}
		/// <summary>
		/// Server thread processing all incoming messages.
		/// When a message is received it is transfered to the server for processing, then looping on receiving next message.
		/// Exiting the server loop is instructed by the server by a returning FALSE after having processed a message.		/// </summary>
		/// <param name="threadData"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		private int StreamServerReceiverMethod(CThreadData threadData, object o)
		{
			// indicate the thread is on
			Client client = (Client)o;
			string threadName = Description + "RECEIVER - ";
			int res = (int)ThreadResult.UNKNOWN;
			bool keepOnRunning = true;
			bool clientShutdown = false;
			client.ReceiverEvents.SetStarted();
			// start receiving messages for that server
			while (keepOnRunning)
			{
				try
				{
					bool addSizeHeader = true;
					byte[] outgoing = null;
					// wait for receiving a message from the client
					byte[] incoming = client.StreamIO.Receive(out int size);
					if (null != incoming && 0 != incoming.Length)
					{
						// a message has been received and needs to be processed

						if (clientShutdown = ArraysAreEqual(STOP_SERVER_CLIENT_THREAD_REQUEST_MESSAGE, incoming))
						{
							CLog.Add(threadName + "Received client instance stop order");
							// acknowledge instance shutdown
							outgoing = new byte[STOP_SERVER_ACCEPT_REPLY_MESSAGE.Length];
							Buffer.BlockCopy(STOP_SERVER_ACCEPT_REPLY_MESSAGE, 0, outgoing, 0, STOP_SERVER_ACCEPT_REPLY_MESSAGE.Length);
							addSizeHeader = true;
							client.StreamIO.Send(outgoing, addSizeHeader);
							keepOnRunning = false;
						}

						// not a shutdown order, let's save the message for further processing
						else
						{
							// got a request, put it on the stack of messages to process
							lock (client.myLock)
							{
								// store the message inside the client's thread dedicated queue of messages
								client.Messages.Enqueue(incoming);
								client.MessageReceivedEvent.Set();
								Thread.Sleep(1);
							}
							keepOnRunning = true;
						}
					}
					else
					{
						// no message received, might be an exception because the socket was closed
						CLog.Add(threadName + "Reception of an empty message, probably client disconnection");
						//res = (int)StreamServerResult.clientReceivedInvalidMessage;
						//keepOnRunning = false;
					}
				}
				catch (Exception ex)
				{
					if (ex is IOException)
					{
						res = (int)ThreadResult.OK;
					}
					else
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
						res = (int)ThreadResult.Exception;
					}
					keepOnRunning = false;
				}
			}
			client.ReceiverEvents.SetStopped();
			client.Stop();
			return res;
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
		private int StreamServerProcessorMethod(CThreadData threadData, object o)
		{
			string threadName = "PROCESSOR - ";
			int res = (int)ThreadResult.UNKNOWN;
			bool keepOnRunning = true;
			// indicate the thread is on
			Client client = (Client)o;
			WaitHandle[] handles = { client.StopProcessingThreadEvent, client.MessageReceivedEvent };
			client.ProcessorEvents.SetStarted();
			// wait for a message to be ready to process
			do
			{
				try
				{
					switch (WaitHandle.WaitAny(handles))
					{
						// thread termination event
						case 0:
							keepOnRunning = false;
							break;
						// message reception event
						case 1:
							byte[] request = null;
							// dequeue the message
							lock (client.myLock)
							{
								try
								{
									request = client.Messages.Dequeue();
								}
								catch (Exception ex)
								{
									CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, threadName + "Fetching message generated an exception");
									request = null;
								}
							}
							// if a message has been fetched, process it
							try
							{
								if (null != request)
								{
									// forward request for processing
									byte[] reply = streamServerStartSettings.OnMessage(client.Tcp, request, out bool addBufferSize, streamServerStartSettings.ThreadData, streamServerStartSettings.Parameters);
									if (null != reply)
									{
										if (client.StreamIO.Send(reply, addBufferSize))
										{
											CLog.Add(threadName + "Exchange complete - Message [" + reply.Length + " bytes]: " + CMisc.BytesToHexStr(request) + " - Reply (" + reply.Length + "): " + CMisc.BytesToHexStr(reply));
										}
										else
										{
											CLog.Add(threadName + "Error sending message back to the client - Request [" + request.Length + " bytes]: " + CMisc.BytesToHexStr(request));
										}
									}
									else
									{
										CLog.Add(threadName + "No reply to send - Request [" + request.Length + " bytes]: " + CMisc.BytesToHexStr(request));
									}
								}
							}
							catch (Exception ex)
							{
								CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, threadName + "OnRequest method generated an exception");
							}
							break;
						// error
						default:
							CLog.Add(threadName + "Unknown non fatal error");
							break;
					}
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
					res = (int)ThreadResult.Exception;
					keepOnRunning = false;
				}

			} while (keepOnRunning);
			client.ProcessorEvents.SetStopped();
			client.Stop();
			return res;
		}
		/// <summary>
		/// Test 2 array of bytes are equal.
		/// Both being null returns false.
		/// </summary>
		/// <param name="b1">First array of bytes</param>
		/// <param name="b2">Second array of bytes</param>
		/// <returns>TRUE if equal, FALSE otherwise</returns>
		private static bool ArraysAreEqual(byte[] b1, byte[] b2)
		{
			if (null == b2 || null == b2)
				return false;
			bool fOK;
			if (fOK = b1.Length == b2.Length)
				for (int i = 0; i < b1.Length && fOK; i++)
					fOK = b1[i] == b2[i];
			return fOK;
		}
		#endregion

		#region internal classes
		/// <summary>
		/// Queue of messages to process
		/// </summary>
		class QueueOfMessages: Queue<byte[]> { }

		/// <summary>
		/// Connected client
		/// </summary>
		class Client
		{
			#region constructor
			public Client(TcpClient tcp, CStreamServerSettings settings)
			{
				Tcp = tcp;
				Settings = settings;
				ID = Guid.NewGuid();
				ReceivingThread = new CThread();
				ReceiverEvents = new CThreadEvents();
				ProcessingThread = new CThread();
				ProcessorEvents = new CThreadEvents();
				MessageReceivedEvent = new AutoResetEvent(false);
				StopProcessingThreadEvent = new AutoResetEvent(false);
				Messages = new QueueOfMessages();
				StreamIO = new CStreamServerIO(Tcp, Settings);
				WaitBeforeAbort = 5;
			}
			~Client()
			{
				Stop();
			}
			#endregion

			#region properties
			public string Key { get => ToString(); }
			public Guid ID { get; private set; }
			public object myLock = new object();
			public TcpClient Tcp { get; private set; }
			public CStreamServerSettings Settings { get; private set; }
			public CStreamServerIO StreamIO { get; private set; } = null;
			public CThread ReceivingThread { get; private set; }
			public CThread ProcessingThread { get; private set; }
			public QueueOfMessages Messages { get; private set; }
			public CThreadEvents ReceiverEvents { get; private set; }
			public CThreadEvents ProcessorEvents { get; private set; }
			public AutoResetEvent MessageReceivedEvent { get; private set; }
			public AutoResetEvent StopProcessingThreadEvent { get; private set; }
			public int WaitBeforeAbort { get; set; }
			private Mutex isStoppingMutex = new Mutex(false);
			#endregion

			#region methods
			/// <summary>
			/// Stop running threads for this client
			/// </summary>
			public void StopReceivingThread()
			{
				if (null != StreamIO && ReceivingThread.IsRunning)
				{
					// close communication stream
					StreamIO.Close();
					// wait for the thread to actually stop
					ReceiverEvents.WaitStopped();
					StreamIO = null;
				}
			}
			public void StopProcessingThread()
			{
				if (ProcessingThread.IsRunning)
				{
					StopProcessingThreadEvent.Set();
					if (!ProcessorEvents.WaitStopped(WaitBeforeAbort * CStreamSettings.ONESECOND))
						ProcessingThread.Thread.Abort();
				}
			}
			public void Stop()
			{
				if (isStoppingMutex.WaitOne())
				{
					StopReceivingThread();
					StopProcessingThread();
					if (null != StreamIO)
						StreamIO.Close();
					isStoppingMutex.ReleaseMutex();
				}
			}
			public override string ToString()
			{
				return Tcp.Client.RemoteEndPoint.ToString();
			}
			#endregion
		}
		class Clients: Dictionary<string, Client> { }
		#endregion
	}
}
