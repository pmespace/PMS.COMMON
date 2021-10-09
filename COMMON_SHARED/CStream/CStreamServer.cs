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
	[Guid("8E3BBBB0-F498-47BF-AB18-5C84B80EE4B4")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IStreamServerStartSettings
	{
		[DispId(1)]
		bool IsValid { get; }
		[DispId(2)]
		CThreadData ThreadData { get; set; }
		[DispId(3)]
		CStreamServerSettings StreamServerSettings { get; set; }
		[DispId(4)]
		object Parameters { get; set; }
		[DispId(5)]
		bool Synchronous { get; set; }
		[DispId(100)]
		CStreamDelegates.ServerOnStartDelegate OnStart { get; set; }
		[DispId(101)]
		CStreamDelegates.ServerOnConnectDelegate OnConnect { get; set; }
		[DispId(102)]
		CStreamDelegates.ServerOnMessageDelegate OnMessage { get; set; }
		[DispId(103)]
		CStreamDelegates.ServerOnDisconnectDelegate OnDisconnect { get; set; }
		[DispId(104)]
		CStreamDelegates.ServerOnStopDelegate OnStop { get; set; }
		//[DispId(105)]
		//CThread.CThreadHasEnded OnTerminate { get; set; }
	}
	/// <summary>
	/// Structure to use to start a <see cref="CStreamServer"/>
	/// </summary>
	[Guid("7D25C068-E0B0-4C2E-ADC1-DE726C6E7EE5")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CStreamServerStartSettings : IStreamServerStartSettings
	{
		#region properties
		/// <summary>
		/// Indicates whether the object is valid or not
		/// </summary>
		public bool IsValid { get => (null != StreamServerSettings && StreamServerSettings.IsValid) && (null == ThreadData || ThreadData.IsValid) && null != OnMessage; }
		/// <summary>
		/// Thread data to use to identify the thread
		/// </summary>
		public CThreadData ThreadData { get; set; } // = null;
		/// <summary>
		/// Server 
		/// </summary>
		public CStreamServerSettings StreamServerSettings { get; set; } // = null;
		/// <summary>
		/// Private parameters passed to the thread
		/// </summary>
		public object Parameters { get; set; }
		/// <summary>
		/// Synchrounous server (1 thread) or not (1 main thread + 1 thread per client)
		/// </summary>
		public bool Synchronous { get; set; } // = true;
		/// <summary>
		/// Called before starting processing requests from a client.
		/// This function allows to initialise the server context.
		/// </summary>
		public CStreamDelegates.ServerOnStartDelegate OnStart { get; set; } // = null;
		/// <summary>
		/// Called when a client connected to the server.
		/// This function allows to initialise the client context inside the server.
		/// </summary>
		public CStreamDelegates.ServerOnConnectDelegate OnConnect { get; set; } // = null;
		/// <summary>
		/// Called when a request has been received to process it and prepare the reply
		/// </summary>
		public CStreamDelegates.ServerOnMessageDelegate OnMessage { get; set; } // = null;
		/// <summary>
		/// Called when a client connected to the server.
		/// This function allows to initialise the client context inside the server.
		/// </summary>
		public CStreamDelegates.ServerOnDisconnectDelegate OnDisconnect { get; set; } // = null;
		/// <summary>
		/// Called after the server has received a stop order.
		/// This function allows to clear the server context.
		/// </summary>
		public CStreamDelegates.ServerOnStopDelegate OnStop { get; set; } // = null;
																								///// <summary>
																								///// Called when the thread terminates
																								///// </summary>
																								//public CThread.CThreadHasEnded OnTerminate { get; set; } // = null;
		#endregion
	}

	[ComVisible(true)]
	[Guid("7E3C2011-C388-4813-B9C5-B24D6A14892F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IStreamServer
	{
		[DispId(1)]
		uint Port { get; }
		[DispId(2)]
		string Address { get; }
		[DispId(3)]
		string FullAddress { get; }
		[DispId(4)]
		int ID { get; set; }
		[DispId(5)]
		int UniqueID { get; }
		[DispId(6)]
		string Name { get; set; }
		[DispId(7)]
		bool IsRunning { get; }
		[DispId(8)]
		string Description { get; }
		[DispId(9)]
		int Result { get; }

		[DispId(100)]
		bool StartServer(CStreamServerStartSettings settings);
		[DispId(101)]
		void StopServer();
	}
	/// <summary>
	/// Server processing implementation
	/// </summary>
	[ComVisible(false)]
	public class CStreamServer : IStreamServer
	{
		#region constructor
		public CStreamServer() { }
		#endregion

		#region private properties
		/// <summary>
		/// Copy of start server settings
		/// </summary>
		private CStreamServerStartSettings StartSettings { get; set; }
		/// <summary>
		/// All clients connected to the server
		/// </summary>
		private object myLock = new object();
		private TcpListener listener = null;
		private CThreadEvents listenerEvents = new CThreadEvents();
		private StreamServerClients connectedClients = new StreamServerClients();
		private Mutex isCleaningUpMutex = new Mutex(false);
		private bool isCleaningUp = false;
		private CThread mainThread = new CThread();
		#endregion

		#region properties
		/// <summary>
		/// The port used by the server
		/// </summary>
		public uint Port { get => (null != listener ? (uint)((IPEndPoint)listener.LocalEndpoint).Port : 0); }
		/// <summary>
		/// The IP address of the server
		/// </summary>
		public string Address { get => (null != listener ? ((IPEndPoint)listener.LocalEndpoint).Address.ToString() : null); }
		/// <summary>
		/// The full IP address + port of the server
		/// </summary>
		public string FullAddress { get => (null != listener ? Address + (0 != Port ? $":{Port}" : null) : null); }
		/// <summary>
		/// <see cref="CThread.ID"/>
		/// </summary>
		public int ID { get => mainThread.ID; set => mainThread.ID = value; }
		/// <summary>
		/// <see cref="CThread.UniqueID"/>
		/// </summary>
		public int UniqueID { get => mainThread.UniqueID; }
		/// <summary>
		/// <see cref="CThread.Name"/>
		/// </summary>
		public string Name { get => mainThread.Name; set => mainThread.Name = value; }
		/// <summary>
		/// <see cref="CThread.IsRunning"/>
		/// </summary>
		public bool IsRunning { get => mainThread.IsRunning; }
		/// <summary>
		/// <see cref="CThread.Description"/>
		/// </summary>
		public string Description { get => mainThread.Description; }
		/// <summary>
		/// <see cref="CThread.Result"/>
		/// </summary>
		public int Result { get => mainThread.Result; }
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
			if (!mainThread.CanStart)
				return false;
			if (null == settings || !settings.IsValid)
				return false;

			StartSettings = settings;
			connectedClients.Clear();
			try
			{
				bool ok = true;
				// verify wether starting the server is accepted or not
				try
				{
					ok = (null == settings.OnStart ? true : settings.OnStart(settings.ThreadData, settings.Parameters));
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "OnStart generated an exception");
					// by default let's start the server
					ok = true;
				}
				if (ok)
				{
					// create a TCP/IP socket and start listenning for incoming connections
					listener = new TcpListener(IPAddress.Any, (int)settings.StreamServerSettings.Port);
					try
					{
						CLog.Add(mainThread.Description + "Server listener created reading port " + settings.StreamServerSettings.Port);
						//listenerEvents.Reset();
						listener.Start();
						try
						{
							// start the thread and sleep to allow him to actually run
							if (mainThread.Start(StreamServerListenerMethod, settings.ThreadData, null, listenerEvents.Started, true))
							{
								return true;
							}
							else
							{
								CLog.Add(mainThread.Description + "Server thread could not be created" + SERVER_NOT_RUNNING);
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
					CLog.Add(mainThread.Description + "The server was not allowed to start", TLog.WARNG);
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Server.OnStart generated an exception. " + SERVER_NOT_RUNNING);
			}
			return false;
		}
		/// <summary>
		/// Stop the server 
		/// </summary>
		/// <returns>TRUE if the server has been stopped or did not exist, FALSE otherwise</returns>
		public void StopServer()
		{
			if (mainThread.IsRunning)
			{
				// clean up and synchronize thread termination
				Cleanup();
				mainThread.Wait();
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
				// indicate the server is stopping
				isCleaningUp = true;
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
							foreach (KeyValuePair<string, StreamServerClient> c in connectedClients)
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
					StartSettings.OnStop?.Invoke(StartSettings.ThreadData, StartSettings.Parameters);
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "OnStop generated an exception");
				}
				//CThread.SendNotification(ThreadData, ID, 0, true);
			}
		}
		/// <summary>
		/// <see cref="CThread.ThreadFunction"/>
		/// Server thread processing all incoming connections.
		/// When a connection is approved a set of threads is created to (first thread) receive messages (second thread) process these messages
		/// in an asynchronous way (without preventing connections or message reception)
		/// </summary>
		/// <param name="threadData"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		private int StreamServerListenerMethod(CThreadData threadData, object o)
		{
			string threadName = mainThread.Description + "LISTENER - ";
			int res = (int)ThreadResult.OK;
			bool keepOnRunning = true;
			// indicate listener is on
			listenerEvents.SetStarted();
			while (keepOnRunning)
			{
				bool ok = false;
				TcpClient tcp = null;
				try
				{
					// accept client connection
					tcp = listener.AcceptTcpClient();
					StreamServerClient client = null;
					try
					{
						EndPoint clientEndPoint = tcp.Client.RemoteEndPoint;
						client = new StreamServerClient(tcp, StartSettings.StreamServerSettings);
						string clientKey = client.Key;
						try
						{
							tcp.SendTimeout = StartSettings.StreamServerSettings.SendTimeout * CStreamSettings.ONESECOND;
							tcp.ReceiveTimeout = StartSettings.StreamServerSettings.ReceiveTimeout * CStreamSettings.ONESECOND;
							// start the processing and receiving threads to process messages from this client
							if (client.ReceivingThread.Start(StreamServerReceiverMethod, StartSettings.ThreadData, client, client.ReceiverEvents.Started))
							{
								if (client.ProcessingThread.Start(StreamServerProcessorMethod, StartSettings.ThreadData, client, client.ProcessorEvents.Started))
								{
									// arrived here everything's in place, let's verify whether the client is accepted or not from that ip address
									try
									{
										client.Connected = (null == StartSettings.OnConnect ? true : StartSettings.OnConnect(tcp, StartSettings.ThreadData, StartSettings.Parameters));
									}
									catch (Exception ex)
									{
										CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "OnConnect generated an exception");
									}
									if (client.Connected)
									{
										CLog.Add(threadName + "Client: " + clientEndPoint.ToString() + " is connected to the server");
										lock (myLock)
										{
											connectedClients.Add(client.Key, client);
											ok = true;
										}
									}
									else
									{
										CLog.Add(threadName + "Connection from " + clientEndPoint.ToString() + " has been refused", TLog.WARNG);
									}
								}
								else
								{
									CLog.Add(threadName + "Failed to start processor thread for client " + clientEndPoint.ToString(), TLog.ERROR);
								}
							}
							else
							{
								CLog.Add(threadName + "Failed to start receiver thread for client " + clientEndPoint.ToString(), TLog.ERROR);
							}
						}
						catch (Exception ex)
						{
							CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "failed to start server");
							if (!ok)
								try
								{
									if (connectedClients.ContainsKey(clientKey))
										connectedClients.Remove(clientKey);
								}
								catch (Exception) { }
							res = (int)ThreadResult.Exception;
						}
					}
					catch (Exception ex)
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "failed to prepare server to start");
						res = (int)ThreadResult.Exception;
					}
					finally
					{
						// cleanup if necesary
						if (null != client && !ok)
						{
							client.Stop();
						}
					}
					if (!ok && null != tcp)
						tcp.Close();
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Server is stopping");
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
			StreamServerClient client = (StreamServerClient)o;
			string threadName = mainThread.Description + "RECEIVER - ";
			int res = (int)ThreadResult.UNKNOWN;
			bool keepOnRunning = true;
			bool clientShutdown = false;
			client.ReceiverEvents.SetStarted();
			EndPoint clientEndPoint = null;
			try
			{
				clientEndPoint = client.Tcp.Client.RemoteEndPoint;
			}
			catch (Exception) { }
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
						keepOnRunning = false;
					}
				}
				catch (Exception ex)
				{
					if (ex is IOException || ex is EDisconnected)
					{
						// the connection has been closed, normal stop
						CLog.Add(threadName + "Client " + (null != clientEndPoint ? clientEndPoint.ToString() : "[address not available]") + " is disconnecting");
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
			// warn the client is disconnecting from the server
			try
			{
				/* 
				 * OnDisconnect is called ONLY if the server is not stopping because: if the server is stopping the main thread
				 * is waiting for the server to stop. Calling OnDisconnect if any UI request is issued (displaying a status,...)
				 * will block the main thread preventing the application to close.
				 */
				if (client.Connected && !isCleaningUp)
					StartSettings.OnDisconnect?.Invoke(null != clientEndPoint ? clientEndPoint.ToString() : "[address not available]", StartSettings.ThreadData, StartSettings.Parameters);
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "OnDisconnect generated an exception");
			}
			client.ReceiverEvents.SetStopped();
			client.Stop();
			return res;
		}
		/// <summary>
		/// <see cref="CThread.ThreadFunction"/>
		/// Server thread processing all incoming messages.
		/// When a message is received it is transfered to the server for processing, then looping on receiving next message.
		/// Exiting the server loop is instructed by the server by a returning FALSE after having processed a message.
		/// </summary>
		/// <param name="threadData"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		private int StreamServerProcessorMethod(CThreadData threadData, object o)
		{
			StreamServerClient client = (StreamServerClient)o;
			string threadName = mainThread.Description + "PROCESSOR - ";
			int res = (int)ThreadResult.UNKNOWN;
			bool keepOnRunning = true;

			// indicate the thread is on
			WaitHandle[] handles = { client.StopProcessingThreadEvent, client.MessageReceivedEvent };
			client.ProcessorEvents.SetStarted();

			// wait for a message to be ready to process
			do
			{
				try
				{
					int index = WaitHandle.WaitAny(handles);
					if (client.StopProcessingThreadEvent == handles[index])
					{
						// thread termination event
						keepOnRunning = false;
					}
					else if (client.MessageReceivedEvent == handles[index])
					{

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
							if (null != request && 0 != request.Length)
							{
								// check whether the messge must be hidden or not
								CLog.Add(threadName + $"Request [{request.Length} bytes] {MessageToLog(client, request, true)}");
								// forward request for processing
								byte[] reply = StartSettings.OnMessage(client.Tcp, request, out bool addBufferSize, threadData, StartSettings.Parameters);
								if (null != reply && 0 != reply.Length)
								{
									CLog.Add(threadName + $"Reply [{reply.Length} bytes] {MessageToLog(client, reply, false)}");
									if (null == client.StreamIO || !client.StreamIO.Send(reply, addBufferSize))
									{
										CLog.Add(threadName + $"The reply was not sent to the client", TLog.ERROR);
									}
								}
								else
								{
									CLog.Add(threadName + $"No reply to send", TLog.WARNG);
								}
							}
							else
							{
								CLog.Add(threadName + $"No request has been received", TLog.WARNG);
							}
						}
						catch (Exception ex)
						{
							CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, threadName + "OnRequest method generated an exception");
						}
					}
					else
					{
						CLog.Add(threadName + "Unknown non fatal error");
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
		private static string MessageToLog(StreamServerClient client, byte[] buffer, bool isRequest)
		{
			// check whether the message must be hidden or not
			if (null != client.StreamServerSettings.OnMessageToLog)
			{
				string s = client.StreamServerSettings.OnMessageToLog(buffer, CMisc.BytesToHexStr(buffer), isRequest);
				return (string.IsNullOrEmpty(s) ? "<MESSAGE HIDDEN>" : s);
			}
			else
			{
				return CMisc.BytesToHexStr(buffer);
			}
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
		class QueueOfMessages : Queue<byte[]> { }

		/// <summary>
		/// Connected client
		/// </summary>
		class StreamServerClient
		{
			#region constructor
			public StreamServerClient(TcpClient tcp, CStreamServerSettings settings)
			{
				Tcp = tcp;
				StreamServerSettings = settings;
				ID = Guid.NewGuid();
				ReceivingThread = new CThread();
				ReceiverEvents = new CThreadEvents();
				ProcessingThread = new CThread();
				ProcessorEvents = new CThreadEvents();
				MessageReceivedEvent = new AutoResetEvent(false);
				StopProcessingThreadEvent = new AutoResetEvent(false);
				Messages = new QueueOfMessages();
				StreamIO = new CStreamServerIO(Tcp, StreamServerSettings);
				WaitBeforeAbort = 5;
				Connected = false;
			}
			~StreamServerClient()
			{
				Stop();
			}
			#endregion

			#region properties
			public bool Connected { get; set; }
			public string Key { get => ToString(); }
			public Guid ID { get; }
			public object myLock = new object();
			public TcpClient Tcp { get; }
			public CStreamServerSettings StreamServerSettings { get; }
			public CStreamServerIO StreamIO { get; private set; } = null;
			public CThread ReceivingThread { get; }
			public CThread ProcessingThread { get; }
			public QueueOfMessages Messages { get; }
			public CThreadEvents ReceiverEvents { get; }
			public CThreadEvents ProcessorEvents { get; }
			public AutoResetEvent MessageReceivedEvent { get; }
			public AutoResetEvent StopProcessingThreadEvent { get; }
			public int WaitBeforeAbort { get; }
			private Mutex isStoppingMutex = new Mutex(false);
			internal string Server = null;
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
#pragma warning disable SYSLIB0006
						ProcessingThread.Thread.Abort();
#pragma warning restore SYSLIB0006
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
				try
				{
					return Tcp.Client.RemoteEndPoint.ToString();
				}
				catch (Exception) { }
				return "[not connected]";
			}
			#endregion
		}
		class StreamServerClients : Dictionary<string, StreamServerClient> { }
		#endregion
	}
}
