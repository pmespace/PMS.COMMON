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
		/// Returns the local IP address (the first, main one).
		/// </summary>
		/// <param name="v4">true to get an IP v4 address, false to get an IP v6 address, true is the default</param>
		/// <param name="loopback">true if the address must be loopback, false if an internet address, false is the default</param>
		/// <returns>
		/// A string containing the local IP address
		/// </returns>
		public static string Localhost(bool loopback = false, bool v4 = true)
		{
			try
			{
				//return IPAddress.Loopback.ToString();
				IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(a => a.AddressFamily == (v4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6)).ToArray();
				if (default != addresses && 0 != addresses.Length)
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
			return default;
		}
		/// <summary>
		/// Returns the local IP addresses (all of them).
		/// </summary>
		/// <returns>
		/// An array of <see cref="IPAddress"/> or null if an error has occurred
		/// </returns>
		public static IPAddress[] Localhosts()
		{
			try
			{
				IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
				return Dns.GetHostAddresses(Dns.GetHostName());
			}
			catch (Exception) { }
			return default;
		}
		/// <summary>
		/// Returns the name of the computer.
		/// </summary>
		/// <returns>
		/// Name of the computer or null if an error has occurred
		/// </returns>
		public static string Name()
		{
			try
			{
				return Dns.GetHostName();
			}
			catch (Exception) { }
			return default;
		}
		/// <summary>
		/// Connects a stream according to the <see cref="CStreamClientSettings"/> provided.
		/// </summary>
		/// <param name="settings">network settings to use to connect the stream</param>
		/// <returns>
		/// A <see cref="CStreamClientIO"/> object if everything went fine, null otherwise
		/// </returns>
		public static CStreamClientIO Connect(CStreamClientSettings settings)
		{
			CStreamClientIO stream = default;
			try
			{
				// determine whether v4 or v6 IP addresse
				IPAddress ip = IPAddress.Parse(settings.IP);
				bool v6 = AddressFamily.InterNetworkV6 == ip.AddressFamily;
				TcpClient tcpclient = new TcpClient(v6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);
				try
				{
					CLog.INFORMATION($"client attempt to connect to {settings.FullIP}");
					var result = tcpclient.BeginConnect(settings.Address, (int)settings.Port, default, default);
					var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(settings.ConnectTimeout));
					if (success)
					{
						// stream is connected
						tcpclient.EndConnect(result);
						CLog.INFORMATION($"client connected to {settings.FullIP}");
						tcpclient.SendBufferSize = (tcpclient.SendBufferSize >= settings.SendBufferSize ? tcpclient.SendBufferSize : settings.SendBufferSize + 1);
						tcpclient.ReceiveBufferSize = (tcpclient.ReceiveBufferSize >= settings.ReceiveBufferSize ? tcpclient.SendBufferSize : settings.ReceiveBufferSize);
						tcpclient.SendTimeout = settings.SendTimeout * CStreamSettings.ONESECOND;
						tcpclient.ReceiveTimeout = settings.ReceiveTimeout * CStreamSettings.ONESECOND;
						// Create an SSL stream that will close the client's stream.
						stream = new CStreamClientIO(tcpclient, settings);
					}
					else
						throw new Exception($"client failed to connect to {settings.FullIP}");
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
		/// Disconnects a stream, freeing resources.
		/// </summary>
		/// <param name="stream">the stream to disconnect</param>
		public static void Disconnect(CStreamIO stream) { stream?.Close(); }
		/// <summary>
		/// Sends data using the given stream.
		/// A size header of <see cref="CStreamBase.SizeHeader"/> bytes is added if <see cref="CStreamBase.UseSizeHeader"/> is true.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="buffer">an array of bytes to send</param>
		/// <returns>
		/// true is successful, false otherwise
		/// </returns>
		public static bool Send(CStreamIO stream, byte[] buffer)
		{
			try
			{
				if (default == stream) throw new ArgumentException("no open stream");

				if (stream.Send(buffer))
					return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Sends data using the given stream.
		/// A size header of <see cref="CStreamBase.SizeHeader"/> bytes is added if <see cref="CStreamBase.UseSizeHeader"/> is true.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="buffer">a string to send</param>
		/// <returns>
		/// true is successful, false otherwise
		/// </returns>
		public static bool Send(CStreamIO stream, string buffer)
		{
			byte[] brequest = buffer.IsNullOrEmpty() ? default : Encoding.UTF8.GetBytes(buffer);
			return Send(stream, brequest);
		}
		/// <summary>
		/// Sends data using the given stream.
		/// No size header is added, the sent buffer will be followed by <paramref name="EOT"/> to indicate the end of the message.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="message">a string to send</param>
		/// <param name="EOT">the string marking the end of the message</param>
		/// <returns>
		/// true is successful, false otherwise
		/// </returns>
		public static bool SendLine(CStreamIO stream, string message, string EOT = CStreamIO.CRLF)
		{
			try
			{
				if (default == stream) throw new ArgumentException("no open stream");

				if (stream.SendLine(message, EOT))
					return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Receives data on the indicated stream.
		/// The buffer MUST begin with a size header of <see cref="CStreamBase.SizeHeader"/> bytes
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <returns>
		/// An array of bytes if successful, null otherwise
		/// </returns>
		public static byte[] Receive(CStreamIO stream)
		{
			byte[] reply = default;
			int announcedSize = 0;
			try
			{
				if (default == stream) throw new ArgumentException("no open stream");

				// Read message from the server
				CLog.INFORMATION($"waiting to receive a message");
				byte[] tmp = stream.Receive(out announcedSize);
				if (default != tmp)
				{
					// rebuild the buffer is required
					if (!stream.UseSizeHeader)
					{
						// the request natively contained a size header, meaningfull to the application, we therefore must reinsert the size header inside the received buffer
						reply = new byte[tmp.Length + stream.SizeHeader];
						byte[] bb = CMisc.SetBytesFromIntegralTypeValue((int)tmp.Length, false);
						Buffer.BlockCopy(bb, 0, reply, 0, (int)stream.SizeHeader);
						Buffer.BlockCopy(tmp, 0, reply, (int)stream.SizeHeader, tmp.Length);
					}
					else
						reply = tmp;
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return reply;
		}
		/// <summary>
		/// Receives data on the indicated stream.
		/// The buffer MUST begin with a size header of <see cref="CStreamBase.SizeHeader"/> bytes.
		/// The buffer is converted to a string after reception and must support this conversion.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <returns>
		/// received message as a string if sucessful, null otherwise
		/// </returns>
		public static string ReceiveAsString(CStreamIO stream)
		{
			byte[] reply = Receive(stream);
			return (null != reply ? Encoding.UTF8.GetString(reply) : null);
		}
		/// <summary>
		/// Receives data of <paramref name="size"/> size on the indicated stream.
		/// Presence of a size header is not required.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="size">size of the buffer to receive</param>
		/// <returns>
		/// An array of bytes if successful, null otherwise
		/// </returns>
		public static byte[] Receive(CStreamIO stream, int size)
		{
			try
			{
				if (default == stream) throw new ArgumentException("no open stream");

				// Read message from the server
				CLog.INFORMATION($"waiting to receive a message");
				return stream.Receive(size);
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Receives data on the indicated stream.
		/// Presence of a size header is not required but the received message must be finished by <paramref name="EOT"/> to know when to stop reading.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="EOT">a string which if found marks the end of transmission</param>
		/// <returns>
		/// received message as a string if sucessful, null otherwise
		/// </returns>
		public static string ReceiveLine(CStreamIO stream, string EOT = CStreamIO.CRLF)
		{
			try
			{
				if (default == stream) throw new ArgumentException("no open stream");

				string s = stream.ReceiveLine(EOT);
				CLog.INFORMATION($"received string message {(s.IsNullOrEmpty() ? 0 : s.Length)} characters");
				return s;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Sends a request message and receives the response.
		/// The stream must be opened and will remain so.
		/// A size header of <see cref="CStreamBase.SizeHeader"/> bytes is added to <paramref name="request"/> if <see cref="CStreamBase.UseSizeHeader"/> is true.
		/// The response MUST begin with a size header of <see cref="CStreamBase.SizeHeader"/> bytes which will be stripped.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="request">an array of bytes to send</param>
		/// <returns>
		/// An array of bytes or null if an error occured. 
		/// </returns>
		public static byte[] SendReceive(CStreamIO stream, byte[] request)
		{
			if (Send(stream, request))
				return Receive(stream);
			return default;
		}
		/// <summary>
		/// Sends a message as a string and receives a string response.
		/// The stream must be opened and will remain so.
		/// A size header of <see cref="CStreamBase.SizeHeader"/> bytes is added to <paramref name="request"/> if <see cref="CStreamBase.UseSizeHeader"/> is true.
		/// The response MUST begin with a size header of <see cref="CStreamBase.SizeHeader"/> bytes which will be stripped.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="request">an array of bytes to send</param>
		/// <returns>
		/// received message as a string if sucessful, null otherwise
		/// </returns>
		public static string SendReceive(CStreamIO stream, string request)
		{
			byte[] reply = SendReceive(stream, default != request ? Encoding.UTF8.GetBytes(request) : default);
			return (default != reply ? Encoding.UTF8.GetString(reply) : default);
		}
		/// <summary>
		/// Sends a string message terminated by a <paramref name="EOT"/> string and receives a string response terminated by the same <paramref name="EOT"/> string.
		/// The stream must be opened and will remain so.
		/// No size header is added to <paramref name="request"/> but <paramref name="EOT"/> will to finish the message.
		/// The response does not need a size header but MUST be finished by <paramref name="EOT"/>.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="request">the string message to send</param>
		/// <param name="EOT">the string marking the end of the message</param>
		/// <returns>
		/// received string message if sucessful, null otherwise
		/// </returns>
		public static string SendReceiveLine(CStreamIO stream, string request, string EOT = CStreamIO.CRLF)
		{
			if (SendLine(stream, request, EOT))
				return ReceiveLine(stream, EOT);
			return default;
		}
		/// <summary>
		/// Connects to a host, sends data then disconnects the stream.
		/// A size header of <see cref="CStreamBase.SizeHeader"/> bytes is added to <paramref name="buffer"/> if <see cref="CStreamBase.UseSizeHeader"/> is true.
		/// </summary>
		/// <param name="settings">the settings to use for opening the stream and sending the data</param>
		/// <param name="buffer">an array of bytes to send</param>
		/// <returns>
		/// true if successful, false otherwise
		/// </returns>
		public static bool ConnectSend(CStreamClientSettings settings, byte[] buffer)
		{
			if (buffer.IsNullOrEmpty()) return false;
			CStreamClientIO stream = ConnectToSend(settings, buffer);
			if (default != stream)
			{
				bool fOK = Send(stream, buffer);
				Disconnect(stream);
				return fOK;
			}
			return false;
		}
		static CStreamClientIO ConnectToSend(CStreamClientSettings settings, byte[] buffer)
		{
			// adjust buffer size according to buffer to send
			int fullBufferSize = (settings.UseSizeHeader ? buffer.Length + settings.SizeHeader : buffer.Length);
			settings.SendBufferSize = (settings.SendBufferSize > fullBufferSize ? settings.SendBufferSize : fullBufferSize + 1);
			return Connect(settings);
		}
		/// <summary>
		/// Connects to a host, sends data as a string then disconnects the stream.
		/// A size header of <see cref="CStreamBase.SizeHeader"/> bytes is added to <paramref name="buffer"/> if <see cref="CStreamBase.UseSizeHeader"/> is true.
		/// </summary>
		/// <param name="settings">the settings to use for opening the stream and sending the data</param>
		/// <param name="buffer">a string message to send</param>
		/// <returns>
		/// true if successful, false otherwise
		/// </returns>
		public static bool ConnectSend(CStreamClientSettings settings, string buffer)
		{
			return ConnectSend(settings, string.IsNullOrEmpty(buffer) ? default : Encoding.UTF8.GetBytes(buffer));
		}
		/// <summary>
		/// Connects to a host, sends a string message finished by <paramref name="EOT"/> and disconnects the stream.
		/// No size header is added to <paramref name="message"/> but <paramref name="EOT"/> will to finish the message.
		/// </summary>
		/// <param name="settings">the settings to use for opening the stream and sending the data</param>
		/// <param name="message">a string to send</param>
		/// <param name="EOT">the string marking the end of the message</param>
		/// <returns>
		/// true if successful, false otherwise
		/// </returns>
		public static bool ConnectSendLine(CStreamClientSettings settings, string message, string EOT = CStreamIO.CRLF)
		{
			if (string.IsNullOrEmpty(message))
				return false;
			CStreamClientIO stream = ConnectToSend(settings, message);
			if (default != stream)
			{
				bool fOK = SendLine(stream, message, EOT);
				Disconnect(stream);
				return fOK;
			}
			return false;
		}
		static CStreamClientIO ConnectToSend(CStreamClientSettings settings, string message)
		{
			// adjust buffer size according to buffer to send
			int fullBufferSize = message.Length + settings.SizeHeader;
			settings.SendBufferSize = (settings.SendBufferSize > fullBufferSize ? settings.SendBufferSize : fullBufferSize + 1);
			return Connect(settings);
		}
		/// <summary>
		/// Connects to a host, sends a request, receives a response then disconnectes the stream.
		/// A size header of <see cref="CStreamBase.SizeHeader"/> bytes is added to <paramref name="request"/> if <see cref="CStreamBase.UseSizeHeader"/> is true.
		/// The response MUST begin with a size header of <see cref="CStreamBase.SizeHeader"/> bytes which will be stripped.
		/// </summary>
		/// <param name="settings">the settings to use for opening the stream and sending the data</param>
		/// <param name="request">an array of bytes to send</param>
		/// <returns>
		/// an array of bytes received in response if successful, null otherwise
		/// </returns>
		public static byte[] ConnectSendReceive(CStreamClientSettings settings, byte[] request)
		{
			byte[] reply = default;
			if (request.IsNullOrEmpty()) return reply;
			CStreamClientIO stream = ConnectToSend(settings, request);
			if (default != stream)
			{
				if (Send(stream, request))
					reply = Receive(stream);
				Disconnect(stream);
			}
			return reply;
		}
		/// <summary>
		/// Connects to a host, sends a request as a string, receives a response and converts it to a string then disconnectes the stream.
		/// A size header of <see cref="CStreamBase.SizeHeader"/> bytes is added to <paramref name="request"/> if <see cref="CStreamBase.UseSizeHeader"/> is true.
		/// The response MUST begin with a size header of <see cref="CStreamBase.SizeHeader"/> bytes which will be stripped.
		/// </summary>
		/// <param name="settings">the settings to use for opening the stream and sending the data</param>
		/// <param name="request">a string to send</param>
		/// <returns>
		/// an array of bytes received in response if successful, null otherwise
		/// </returns>
		public static string ConnectSendReceive(CStreamClientSettings settings, string request)
		{
			byte[] reply = ConnectSendReceive(settings, Encoding.UTF8.GetBytes(request));
			return (default != reply ? Encoding.UTF8.GetString(reply) : default);
		}
		/// <summary>
		/// This function prevents using any size header, using CR+LF as an EOT
		/// No size header is added to <paramref name="request"/> but <paramref name="EOT"/> will to finish the message.
		/// The response does not need a size header but MUST be finished by <paramref name="EOT"/>.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="request"></param>
		/// <param name="EOT"></param>
		/// <returns></returns>
		public static string ConnectSendReceiveLine(CStreamClientSettings settings, string request, string EOT = CStreamIO.CRLF)
		{
			if (string.IsNullOrEmpty(request))
				return default;
			CStreamClientIO stream = ConnectToSend(settings, request);
			if (default != stream)
			{
				string reply = SendReceiveLine(stream, request, EOT);
				Disconnect(stream);
				return reply;
			}
			return default;
		}
		/// <summary>
		/// Starts a client thread to send and receive data.
		/// The caller is warned when a reply is received by a call to the specified function (<see cref="SendAsyncType"/>).
		/// </summary>
		/// <param name="sendAsync">settings to start the thread</param>
		/// <param name="request">an array of bytes to send</param>
		/// <param name="asstring">if true the buffer is sent using <see cref="SendLine(CStreamIO, string, string)"/>, if false it will be sent using <see cref="Send(CStreamIO, byte[])"/></param>
		/// <param name="EOT">the string marking the end of the message (only if <paramref name="asstring"/> is true)</param>
		/// <returns>
		/// a <see cref="CThread"/> object if successful, null otherwise
		/// </returns>
		static CThread SendAsync(SendAsyncType sendAsync, byte[] request, bool asstring, string EOT = CStreamIO.CRLF)
		{
			if (default == sendAsync
				|| default == sendAsync.Settings
				|| !sendAsync.Settings.IsValid
				|| default == request
				|| 0 == request.Length)
				return default;
			try
			{
				// prepare working settings
				ClientThreadType threadParams = new ClientThreadType()
				{
					SendAsync = sendAsync,
					Request = request,
					ClientOnly = default == sendAsync.OnReply,
					AsString = asstring,
					EOT = EOT,
				};
				// prepare the thread object
				CThread thread = new CThread();
				if (thread.Start(SendAsyncThreadMethod, sendAsync.ThreadData, threadParams, default, true))
					return thread;
				else
					thread = default;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Start a client thread to send and receive data.
		/// The message is sent using <see cref="Send(CStreamIO, byte[])"/>
		/// </summary>
		/// <param name="sendAsync">settings to start the thread</param>
		/// <param name="request">a string to send</param>
		/// <returns>
		/// a <see cref="CThread"/> object if successful, null otherwise
		/// </returns>
		public static CThread SendAsync(SendAsyncType sendAsync, byte[] request)
		{
			return SendAsync(sendAsync, request, false);
		}
		/// <summary>
		/// Start a client thread to send and receive data.
		/// The message is sent using <see cref="Send(CStreamIO, byte[])"/>
		/// </summary>
		/// <param name="sendAsync">settings to start the thread</param>
		/// <param name="request">a string to send</param>
		/// <returns>
		/// a <see cref="CThread"/> object if successful, null otherwise
		/// </returns>
		public static CThread SendAsync(SendAsyncType sendAsync, string request)
		{
			return SendAsync(sendAsync, Encoding.UTF8.GetBytes(request), false);
		}
		/// <summary>
		/// Refer to <see cref="SendAsync(SendAsyncType, byte[], bool, string)"/>
		/// The message is sent using <see cref="SendLine(CStreamIO, string, string)"/>
		/// </summary>
		/// <param name="sendAsync">settings to start the thread</param>
		/// <param name="request">a string to send</param>
		/// <param name="EOT">the string marking the end of the message</param>
		/// <returns>
		/// a <see cref="CThread"/> object if successful, null otherwise
		/// </returns>
		public static CThread SendAsyncLine(SendAsyncType sendAsync, string request, string EOT = CStreamIO.CRLF)
		{
			return SendAsync(sendAsync, Encoding.UTF8.GetBytes(request), true, EOT);
		}
		/// <summary>
		/// Class used to specify how to handle asynchronous sending of data
		/// </summary>
		public class SendAsyncType
		{
			/// <summary>
			/// Data used by the thread to communicate with the outside world.
			/// </summary>
			public CThreadData ThreadData { get; set; } = default;
			/// <summary>
			/// Settings to use to reach the server.
			/// </summary>
			public CStreamClientSettings Settings { get; set; } = default;
			/// <summary>
			/// Function that will be called when the reply has been received.
			/// </summary>
			public CStreamDelegates.ClientOnReplyDelegate OnReply { get; set; } = default;
			/// <summary>
			/// Parameters to pass to the <see cref="OnReply"/> function
			/// </summary>
			public object Parameters { get; set; } = default;
		}
		/// <summary>
		/// <see cref="CThread.ThreadFunction"/>.
		/// That function supports <see cref="SendAsync(SendAsyncType, byte[], bool, string)"/> processing/
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		[ComVisible(false)]
		static int SendAsyncThreadMethod(CThread thread, object o)
		{
			SendAsyncEnum res = SendAsyncEnum.KO;
			ClientThreadType threadParams = (ClientThreadType)o;
			if (default != threadParams.SendAsync.OnReply)
			{

				// send & receive 
				if (threadParams.AsString)
				{
					string reply = ConnectSendReceiveLine(threadParams.SendAsync.Settings, Encoding.UTF8.GetString(threadParams.Request), threadParams.EOT);
					if (string.IsNullOrEmpty(reply))
					{
						res = SendAsyncEnum.NoData;
					}
					else
					{
						// forward reply to the caller
						if (threadParams.SendAsync.OnReply(Encoding.UTF8.GetBytes(reply), thread, threadParams.SendAsync.Parameters))
							res = SendAsyncEnum.OK;
						else
							res = SendAsyncEnum.ReceiveError;
					}
				}
				else
				{
					byte[] reply = ConnectSendReceive(threadParams.SendAsync.Settings, threadParams.Request);
					if (default == reply || 0 == reply.Length)
					{
						res = SendAsyncEnum.NoData;
					}
					else
					{
						// forward reply to the caller
						if (threadParams.SendAsync.OnReply(reply, thread, threadParams.SendAsync.Parameters))
							res = SendAsyncEnum.OK;
						else
							res = SendAsyncEnum.ReceiveError;
					}
				}
			}
			else
			{
				// send only
				if (threadParams.AsString)
				{
					if (ConnectSendLine(threadParams.SendAsync.Settings, Encoding.UTF8.GetString(threadParams.Request), threadParams.EOT))
						res = SendAsyncEnum.OK;
					else
						res = SendAsyncEnum.SendError;
				}
				else
				{
					if (ConnectSend(threadParams.SendAsync.Settings, threadParams.Request))
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
			public byte[] Request { get; set; } = default;
			public bool ClientOnly { get; set; } = false;
			public bool AsString { get; set; } = false;
			public string EOT { get; set; } = CStreamIO.CRLF;
		}
		#endregion
	}
}
