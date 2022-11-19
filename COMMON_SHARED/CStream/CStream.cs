using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Net;
using System;

namespace COMMON
{
	[ComVisible(true)]
	public enum SendAsyncEnum
	{
		NoData = -5,
		Timeout = -4,
		SendError = -3,
		ReceiveError = -2,
		KO = -1,
		OK = 0,
		UserDefined,
	}

	/// <summary>
	/// SSL or IP stream class
	/// </summary>
	[ComVisible(false)]
	public static class CStream
	{
		#region methods
		/// <summary>
		/// Returns the local IP address (the first, main one)
		/// </summary>
		/// <param name="v4">True if a v4 address is expected, false if a v6 one is expected, v4 is the default</param>
		/// <param name="loopback">True if the address must be loopback, false if an internet address, not loopback is the default</param>
		/// <returns>A string containing the local IP address</returns>
		public static string Localhost(bool loopback = false, bool v4 = true)
		{
			try
			{
				//return IPAddress.Loopback.ToString();
				IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(a => a.AddressFamily == (v4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6)).ToArray();
				if (null != addresses && 0 != addresses.Length)
				{
					foreach (IPAddress addr in addresses)
					{
						// try seartching for a real internet address, not a loopback
						if ((!loopback && !IPAddress.IsLoopback(addr)) || (loopback && IPAddress.IsLoopback(addr)))
						{
							return addr.ToString();
						}
					}
				}
				// no internal address, let's use the loopback
				return addresses[0].ToString();
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			// arrived here no IP address at all
			return null;
		}
		/// <summary>
		/// Returns the local IP addresses (all of them)
		/// </summary>
		/// <returns>An array of <see cref="IPAddress"/> or null if an error has occurred</returns>
		public static IPAddress[] Localhosts()
		{
			try
			{
				IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
				return Dns.GetHostAddresses(Dns.GetHostName());
			}
			catch (Exception) { }
			return null;
		}
		/// <summary>
		/// Returns the name of the computer
		/// </summary>
		/// <returns>Name of the computer or null if an error has occurred</returns>
		public static string Name()
		{
			try
			{
				return Dns.GetHostName();
			}
			catch (Exception) { }
			return null;
		}
		/// <summary>
		/// Connect a stream according to the settings provided.
		/// </summary>
		/// <param name="settings">Network settings to use to connect the stream</param>
		/// <returns>A <see cref="CStreamClientIO"/> object if everything went fine, null otherwise</returns>
		public static CStreamClientIO Connect(CStreamClientSettings settings)
		{
			CStreamClientIO stream = null;
			try
			{
				// determine whether v4 or v6 IP addresse
				IPAddress ip = IPAddress.Parse(settings.IP);
				bool v6 = AddressFamily.InterNetworkV6 == ip.AddressFamily;
				TcpClient tcpclient = new TcpClient(v6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);
				try
				{
					CLog.DEBUG($"Trying to connect to {settings.FullIP}");
					//tcpclient.Connect(settings.Address, (int)settings.Port);
					var result = tcpclient.BeginConnect(settings.Address, (int)settings.Port, null, null);
					var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(settings.ConnectTimeout));
					if (success)
					{
						// stream is connected
						tcpclient.EndConnect(result);
						CLog.DEBUG($"Connected to {settings.FullIP}");
						tcpclient.SendBufferSize = (tcpclient.SendBufferSize >= settings.SendBufferSize ? tcpclient.SendBufferSize : settings.SendBufferSize + 1);
						tcpclient.ReceiveBufferSize = (tcpclient.ReceiveBufferSize >= settings.ReceiveBufferSize ? tcpclient.SendBufferSize : settings.ReceiveBufferSize);
						tcpclient.SendTimeout = settings.SendTimeout * CStreamSettings.ONESECOND;
						tcpclient.ReceiveTimeout = settings.ReceiveTimeout * CStreamSettings.ONESECOND;
						// Create an SSL stream that will close the client's stream.
						stream = new CStreamClientIO(tcpclient, settings);
					}
					else
						throw new Exception($"Connection to {settings.FullIP} has failed");
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex);
					tcpclient.Close();
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return stream;
		}
		/// <summary>
		/// Disconnect a stream freeing resources.
		/// </summary>
		/// <param name="stream">the stream to disconnect</param>
		public static void Disconnect(CStreamIO stream)
		{
			if (null != stream)
			{
				stream.Close();
			}
		}
		/// <summary>
		/// Send data on the given stream.
		/// </summary>
		/// <param name="stream">The connected stream</param>
		/// <param name="request">A array of bytes to send</param>
		/// <param name="addSizeHeader">Indicates whether a buffer size block must be added before the buffer to send</param>
		/// <returns>An arry of bytes received in response or if an error occured. In case of a client only request, the function returns the request, as no reply can be returned, if everything went right</returns>
		public static bool Send(CStreamIO stream, byte[] request, bool addSizeHeader)
		{
			if (null == stream || request.IsNullOrEmpty())
				return false;
			try
			{
				// Send message to the server
				CLog.DEBUG($"About to send message of {(addSizeHeader ? request.Length : request.Length - (int)stream.LengthBufferSize)} bytes");
				if (stream.Send(request, addSizeHeader))
					return true;
				// arrived here the message hasn't been sent
				CLog.ERROR($"An error has occurred while sending the message");
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Refer to <see cref="Send(CStreamIO, byte[], bool)"/>
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		public static bool Send(CStreamIO stream, string request)
		{
			byte[] brequest = string.IsNullOrEmpty(request) ? null : Encoding.UTF8.GetBytes(request);
			return Send(stream, brequest, true);
		}
		/// <summary>
		/// Refer to <see cref="CStreamIO.SendLine(string, string)"/>
		/// This function prevents using any size header, using CR+LF as an EOT
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="request"></param>
		/// <param name="EOT"></param>
		/// <returns></returns>
		public static bool SendLine(CStreamIO stream, string request, string EOT = CStreamIO.CRLF)
		{
			if (null == stream || string.IsNullOrEmpty(request))
				return false;
			try
			{
				CLog.DEBUG($"About to send string message of {request.Length} characters");
				if (stream.SendLine(request, EOT))
					return true;
				CLog.ERROR($"An error has occurred while sending the string message");
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Receive data on the indicated stream.
		/// The buffer MUST begin with a size header of <see cref="CStreamBase.LengthBufferSize"/>
		/// </summary>
		/// <param name="stream">The connected stream</param>
		/// <param name="announcedSize">The size of the reply as announced by the sender</param>
		/// <param name="sizeHeaderAdded">Indicates whether a buffer size header was natively or not inside the request.
		/// If TRUE then the size header has been added by the system meaning the application does not care about it and won't care about it inside the received buffer which will be returned without any size header.
		/// If FALSE then the size header was already part of the buffer meaning the application added it and cares about it. The received buffer must therefore contain the size header.</param>
		/// <param name="error">True indicates the function ended up with an error as no more data was available, false otherwise</param>
		/// <returns>An arry of bytes received in response or if an error occured. In case of a client only request, the function returns the request, as no reply can be returned, if everything went right</returns>
		public static byte[] Receive(CStreamIO stream, out int announcedSize, out bool error, bool sizeHeaderAdded)
		{
			byte[] reply = null;
			announcedSize = 0;
			error = false;
			if (null == stream)
				return null;
			try
			{
				// Read message from the server
				byte[] tmp = stream.Receive(out announcedSize);
				if (null != tmp)
				{
					CLog.DEBUG($"Received message of {(sizeHeaderAdded ? tmp.Length : tmp.Length - (int)stream.LengthBufferSize)} actual bytes (announcing {announcedSize} bytes)");
					// rebuild the buffer is required
					if (!sizeHeaderAdded)
					{
						// the request natively contained a size header, meaningfull to the application, we therefore must reinsert the size header inside the received buffer
						reply = new byte[tmp.Length + stream.LengthBufferSize];
						byte[] bb = CMisc.SetBytesFromIntegralTypeValue(tmp.Length, stream.LengthBufferSize);
						Buffer.BlockCopy(bb, 0, reply, 0, stream.LengthBufferSize);
						Buffer.BlockCopy(tmp, 0, reply, stream.LengthBufferSize, tmp.Length);
					}
					else
						reply = tmp;
				}
				else if (0 != announcedSize)
				{
					CLog.ERROR($"No data has been received though expecting some (invalid announced length,...)");
					error = true;
				}
				else
				{
					CLog.DEBUG($"No data has been received");
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return reply;
		}
		/// <summary>
		/// Refer to <see cref="Receive(CStreamIO, out int, out bool, bool)"/>
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="announcedSize"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		public static string Receive(CStreamIO stream, out int announcedSize, out bool error)
		{
			byte[] reply = Receive(stream, out announcedSize, out error, true);
			return (null != reply ? Encoding.UTF8.GetString(reply) : null);
		}
		/// <summary>
		/// Refer to <see cref="CStreamIO.ReceiveLine(string)"/>
		/// The string does not need to begin by a size header of <see cref="CStreamBase.LengthBufferSize"/> which will be ignored.
		/// The string MUST however finish (or at least contain) a CR+LF sequence (or contain it) marking the EOT.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="error">True indicates the function ended up with a error, false otherwise</param>
		/// <param name="EOT">A string which if found marks the end of transmission</param>
		/// <returns></returns>
		public static string ReceiveLine(CStreamIO stream, out bool error, string EOT = CStreamIO.CRLF)
		{
			error = false;
			if (null == stream)
				return null;
			try
			{
				string s = stream.ReceiveLine(EOT);
				CLog.DEBUG($"Received string message of {(string.IsNullOrEmpty(s) ? 0 : s.Length)} characters");
				return s;
			}
			catch (Exception ex)
			{
				error = true;
				CLog.EXCEPT(ex);
			}
			return null;
		}
		/// <summary>
		/// Send (<see cref="Send(CStreamIO, byte[], bool)"/> and <see cref="Send(CStreamIO, string)"/>),
		/// then receive data (<see cref="Receive(CStreamIO, out int, out bool, bool)"/>.
		/// The stream must pre-exist
		/// </summary>
		/// <param name="stream">The connected stream</param>
		/// <param name="request">A array of bytes to send</param>
		/// <param name="announcedSize">The size of the reply as announced by the sender</param>
		/// <param name="addSizeHeader">Indicates whether a buffer size block must be added before the buffer to send</param>
		/// <param name="error">True indicates the function ended up with a error, false otherwise</param>
		/// <returns></returns>
		public static byte[] SendReceive(CStreamIO stream, byte[] request, bool addSizeHeader, out int announcedSize, out bool error)
		{
			announcedSize = 0;
			error = false;
			if (Send(stream, request, addSizeHeader))
				return Receive(stream, out announcedSize, out error, addSizeHeader);
			return null;
		}
		/// <summary>
		/// Refer to <see cref="SendReceive(CStreamIO, byte[], bool, out int, out bool)"/>
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="request"></param>
		/// <param name="announcedSize"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		public static string SendReceive(CStreamIO stream, string request, out int announcedSize, out bool error)
		{
			byte[] reply = SendReceive(stream, null != request ? Encoding.UTF8.GetBytes(request) : null, true, out announcedSize, out error);
			return (null != reply ? Encoding.UTF8.GetString(reply) : null);
		}
		/// <summary>
		/// This function prevents using any size header, using CR+LF as an EOT
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="request"></param>
		/// <param name="error"></param>
		/// <param name="EOT"></param>
		/// <returns></returns>
		public static string SendReceiveLine(CStreamIO stream, string request, out bool error, string EOT = CStreamIO.CRLF)
		{
			error = false;
			if (SendLine(stream, request, EOT))
				return ReceiveLine(stream, out error, EOT);
			return null;
		}
		/// <summary>
		/// Connect (<see cref="Connect(CStreamClientSettings)"/>) and send data (<see cref="Send(CStreamIO, byte[], bool)"/> and <see cref="Send(CStreamIO, string)"/>).
		/// </summary>
		/// <param name="settings">The settings to use for sending data</param>
		/// <param name="request">A array of bytes to send</param>
		/// <param name="addSizeHeader">Indicates whether a buffer size block must be added before the buffer to send</param>
		/// <returns>An arry of bytes received in response or if an error occured. In case of a client only request, the function returns the request, as no reply can be returned, if everything went right</returns>
		public static bool ConnectSend(CStreamClientSettings settings, byte[] request, bool addSizeHeader)
		{
			if (null == request || 0 == request.Length)
				return false;
			CStreamClientIO stream = ConnectToSend(settings, request, addSizeHeader);
			if (null != stream)
			{
				bool fOK = Send(stream, request, addSizeHeader);
				Disconnect(stream);
				return fOK;
			}
			return false;
		}
		private static CStreamClientIO ConnectToSend(CStreamClientSettings settings, byte[] request, bool addSizeHeader)
		{
			// adjust buffer size according to buffer to send
			int fullBufferSize = (addSizeHeader ? request.Length + (int)settings.LengthBufferSize : request.Length);
			settings.SendBufferSize = (settings.SendBufferSize > fullBufferSize ? settings.SendBufferSize : fullBufferSize + 1);
			return Connect(settings);
		}
		/// <summary>
		/// Refer to <see cref="ConnectSend(CStreamClientSettings, byte[], bool)"/>
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		public static bool ConnectSend(CStreamClientSettings settings, string request)
		{
			return ConnectSend(settings, string.IsNullOrEmpty(request) ? null : Encoding.UTF8.GetBytes(request), true);
		}
		/// <summary>
		/// This function prevents using any size header, using CR+LF as an EOT
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="request"></param>
		/// <param name="EOT"></param>
		/// <returns></returns>
		public static bool ConnectSendLine(CStreamClientSettings settings, string request, string EOT = CStreamIO.CRLF)
		{
			if (string.IsNullOrEmpty(request))
				return false;
			CStreamClientIO stream = ConnectToSend(settings, request);
			if (null != stream)
			{
				bool fOK = SendLine(stream, request, EOT);
				Disconnect(stream);
				return fOK;
			}
			return false;
		}
		private static CStreamClientIO ConnectToSend(CStreamClientSettings settings, string request)
		{
			// adjust buffer size according to buffer to send
			int fullBufferSize = request.Length + (int)settings.LengthBufferSize;
			settings.SendBufferSize = (settings.SendBufferSize > fullBufferSize ? settings.SendBufferSize : fullBufferSize + 1);
			return Connect(settings);
		}
		/// <summary>
		/// Connect (<see cref="Connect(CStreamClientSettings)"/>), send (<see cref="Send(CStreamIO, byte[], bool)"/> and <see cref="Send(CStreamIO, string)"/>) and receive (<see cref="Receive(CStreamIO, out int, out bool, bool)"/> and <see cref="Receive(CStreamIO, out int, out bool)"/>) data.
		/// </summary>
		/// <param name="settings">The settings to use for sending data</param>
		/// <param name="request">A array of bytes to send</param>
		/// <param name="addSizeHeader">Indicates whether a buffer size block must be added before the buffer to send</param>
		/// <param name="announcedSize">The size of the reply as announced by the sender</param>
		/// <param name="error">True indicates the function ended up with a error, false otherwise</param>
		/// <returns>An arry of bytes received in response or if an error occured. In case of a client only request, the function returns the request, as no reply can be returned, if everything went right</returns>
		public static byte[] ConnectSendReceive(CStreamClientSettings settings, byte[] request, bool addSizeHeader, out int announcedSize, out bool error)
		{
			byte[] reply = null;
			error = false;
			announcedSize = 0;
			if (null == request || 0 == request.Length)
				return null;
			CStreamClientIO stream = ConnectToSend(settings, request, addSizeHeader);
			if (null != stream)
			{
				if (Send(stream, request, addSizeHeader))
					reply = Receive(stream, out announcedSize, out error, addSizeHeader);
				Disconnect(stream);
			}
			return reply;
		}
		/// <summary>
		/// Refer to <see cref="ConnectSendReceive(CStreamClientSettings, byte[], bool, out int, out bool)"/>
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="request"></param>
		/// <param name="announcedSize"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		public static string ConnectSendReceive(CStreamClientSettings settings, string request, out int announcedSize, out bool error)
		{
			byte[] reply = ConnectSendReceive(settings, Encoding.UTF8.GetBytes(request), true, out announcedSize, out error);
			return (null != reply ? Encoding.UTF8.GetString(reply) : null);
		}
		/// <summary>
		/// This function prevents using any size header, using CR+LF as an EOT
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="request"></param>
		/// <param name="error"></param>
		/// <param name="EOT"></param>
		/// <returns></returns>
		public static string ConnectSendReceiveLine(CStreamClientSettings settings, string request, out bool error, string EOT = CStreamIO.CRLF)
		{
			error = false;
			if (string.IsNullOrEmpty(request))
				return null;
			CStreamClientIO stream = ConnectToSend(settings, request);
			if (null != stream)
			{
				string reply = SendReceiveLine(stream, request, out error, EOT);
				Disconnect(stream);
				return reply;
			}
			return null;
		}
		/// <summary>
		/// Start a client thread to send and receive data.
		/// The caller is warned when a reply is received by a call to the specified function (<see cref="SendAsyncType"/>).
		/// </summary>
		/// <param name="sendAsync">Settings to start the thread</param>
		/// <param name="request">The request as bytes array</param>
		/// <param name="addSizeHeader">Indicates whether or not adding a size header when sending the request</param>
		/// <param name="lineExchanges">Indicates whether (true) or not (false) the exchanges complete by a new line, not using the size header.
		/// If set to true no buffer size is never used during the exchanges (present or not) and the EOT is always represented by a CR+LF.
		/// Setting this parameter to true supersedes the addSizeHeader one</param>
		/// <param name="EOT"></param>
		/// <returns>A <see cref="CThread"/> object if the thread has been started, null otherwise</returns>
		public static CThread SendAsync(SendAsyncType sendAsync, byte[] request, bool addSizeHeader, bool lineExchanges = false, string EOT = CStreamIO.CRLF)
		{
			if (null == sendAsync
				|| null == sendAsync.Settings
				|| !sendAsync.Settings.IsValid
				|| null == request
				|| 0 == request.Length)
				return null;
			try
			{
				// prepare working settings
				ClientThreadType threadParams = new ClientThreadType()
				{
					SendAsync = sendAsync,
					Request = request,
					AddSizeHeader = addSizeHeader,
					ClientOnly = null == sendAsync.OnReply,
					LineExchanges = lineExchanges,
					EOT = EOT,
				};
				// prepare the thread object
				CThread thread = new CThread();
				//if (thread.Start(SendAsyncThreadMethod, sendAsync.ThreadData, threadParams, null, sendAsync.OnTerminate, true))
				if (thread.Start(SendAsyncThreadMethod, sendAsync.ThreadData, threadParams, null, true))
					return thread;
				else
					thread = null;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return null;
		}
		/// <summary>
		/// Refer to <see cref="SendAsync(SendAsyncType, byte[], bool, bool, string)"/>
		/// </summary>
		/// <param name="sendAsync"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		public static CThread SendAsync(SendAsyncType sendAsync, string request)
		{
			return SendAsync(sendAsync, Encoding.UTF8.GetBytes(request), true, false);
		}
		/// <summary>
		/// Refer to <see cref="SendAsync(SendAsyncType, byte[], bool, bool, string)"/>
		/// This function prevents using any size header, using CR+LF as an EOT
		/// </summary>
		/// <param name="sendAsync"></param>
		/// <param name="request"></param>
		/// <param name="EOT"></param>
		/// <returns></returns>
		public static CThread SendAsyncLine(SendAsyncType sendAsync, string request, string EOT = CStreamIO.CRLF)
		{
			return SendAsync(sendAsync, Encoding.UTF8.GetBytes(request), false, true, EOT);
		}
		/// <summary>
		/// Class used to specify how to handle asynchronous sending of data
		/// </summary>
		public class SendAsyncType
		{
			/// <summary>
			/// Data used by the thread to communicate with the outside world.
			/// </summary>
			public CThreadData ThreadData { get; set; } = null;
			/// <summary>
			/// Settings to use to reach the server.
			/// </summary>
			public CStreamClientSettings Settings { get; set; } = null;
			/// <summary>
			/// Function that will be called when the reply has been received.
			/// </summary>
			public CStreamDelegates.ClientOnReplyDelegate OnReply { get; set; } = null;
			///// <summary>
			///// Function that will be called when the thread is terminated.
			///// </summary>
			//public CThread.CThreadHasEnded OnTerminate { get; set; } = null;
			/// <summary>
			/// Parameters to pass to the <see cref="OnReply"/> function
			/// </summary>
			public object Parameters { get; set; } = null;
		}
		/// <summary>
		/// <see cref="CThread.ThreadFunction"/>.
		/// That function supports <see cref="SendAsync(SendAsyncType, byte[], bool, bool, string)"/> processing/
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		[ComVisible(false)]
		private static int SendAsyncThreadMethod(CThread thread, object o)
		{
			SendAsyncEnum res = SendAsyncEnum.KO;
			ClientThreadType threadParams = (ClientThreadType)o;
			if (null != threadParams.SendAsync.OnReply)
			{

				// send & receive 
				if (threadParams.LineExchanges)
				{
					string reply = ConnectSendReceiveLine(threadParams.SendAsync.Settings, Encoding.UTF8.GetString(threadParams.Request), out bool error, threadParams.EOT);
					if (string.IsNullOrEmpty(reply))
					{
						res = SendAsyncEnum.NoData;
					}
					else
					{
						// forward reply to the caller
						if (threadParams.SendAsync.OnReply(Encoding.UTF8.GetBytes(reply), error, thread, threadParams.SendAsync.Parameters))
							res = SendAsyncEnum.OK;
						else
							res = SendAsyncEnum.ReceiveError;
					}
				}
				else
				{
					byte[] reply = ConnectSendReceive(threadParams.SendAsync.Settings, threadParams.Request, threadParams.AddSizeHeader, out int announcedSize, out bool error);
					if (null == reply || 0 == reply.Length)
					{
						res = SendAsyncEnum.NoData;
					}
					else if (reply.Length != announcedSize)
					{
						res = SendAsyncEnum.ReceiveError;
					}
					else
					{
						// forward reply to the caller
						if (threadParams.SendAsync.OnReply(reply, error, thread, threadParams.SendAsync.Parameters))
							res = SendAsyncEnum.OK;
						else
							res = SendAsyncEnum.ReceiveError;
					}
				}
			}
			else
			{
				// send only
				if (threadParams.LineExchanges)
				{
					if (ConnectSendLine(threadParams.SendAsync.Settings, Encoding.UTF8.GetString(threadParams.Request), threadParams.EOT))
						res = SendAsyncEnum.OK;
					else
						res = SendAsyncEnum.SendError;
				}
				else
				{
					if (ConnectSend(threadParams.SendAsync.Settings, threadParams.Request, threadParams.AddSizeHeader))
						res = SendAsyncEnum.OK;
					else
						res = SendAsyncEnum.SendError;
				}
			}
			return (int)res;
		}
		class ClientThreadType
		{
			public SendAsyncType SendAsync { get; set; }
			public byte[] Request { get; set; } = null;
			public bool AddSizeHeader { get; set; } = true;
			public bool ClientOnly { get; set; } = false;
			public bool LineExchanges { get; set; } = false;
			public string EOT { get; set; } = CStreamIO.CRLF;
		}
		#endregion
	}
}
