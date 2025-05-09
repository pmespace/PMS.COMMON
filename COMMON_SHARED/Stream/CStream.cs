﻿using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Net;
using System;
using System.Threading;
using COMMON.Properties;

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
					CLog.INFORMATION(Resources.CStreamClientConnect.Format(new object[] { settings.FullIP, settings.ConnectTimeout }));
					var result = tcpclient.BeginConnect(settings.Address, (int)settings.Port, default, default);
					var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(settings.ConnectTimeout));
					if (success)
					{
						// stream is connected
						tcpclient.EndConnect(result);
						CLog.INFORMATION(Resources.CStreamClientConnected.Format(settings.FullIP));
						tcpclient.SendBufferSize = (tcpclient.SendBufferSize >= settings.SendBufferSize ? tcpclient.SendBufferSize : settings.SendBufferSize + 1);
						tcpclient.ReceiveBufferSize = (tcpclient.ReceiveBufferSize >= settings.ReceiveBufferSize ? tcpclient.SendBufferSize : settings.ReceiveBufferSize);
						tcpclient.SendTimeout = settings.SendTimeout * CStreamSettings.ONESECOND;
						tcpclient.ReceiveTimeout = settings.ReceiveTimeout * CStreamSettings.ONESECOND;
						// Create an SSL stream that will close the client's stream.
						stream = new CStreamClientIO(tcpclient, settings);
					}
					else
						throw new Exception(Resources.CStreamClientNotConnected.Format(settings.FullIP));
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// true is successful, false otherwise
		/// </returns>
		public static bool Send(CStreamIO stream, byte[] buffer, CancellationToken token = default)
		{
			try
			{
				if (default == stream) throw new ArgumentException(Resources.CStreamNoOpenedStream);

				if (stream.Send(buffer, token))
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// true is successful, false otherwise
		/// </returns>
		public static bool Send(CStreamIO stream, string buffer, CancellationToken token = default)
		{
			//byte[] brequest = buffer.IsNullOrEmpty() ? default : Encoding.UTF8.GetBytes(buffer);
			byte[] brequest = buffer.IsNullOrEmpty() ? default : stream.Encoding.GetBytes(buffer);
			return Send(stream, brequest, token);
		}
		/// <summary>
		/// Sends data using the given stream.
		/// No size header is added, the sent buffer will be followed by <paramref name="EOT"/> to indicate the end of the message.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="message">a string to send</param>
		/// <param name="EOT">the string marking the end of the message</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// true is successful, false otherwise
		/// </returns>
		public static bool SendLine(CStreamIO stream, string message, string EOT = Chars.CRLF, CancellationToken token = default)
		{
			try
			{
				if (default == stream) throw new ArgumentException(Resources.CStreamNoOpenedStream);

				if (stream.SendLine(message, token, EOT))
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// An array of bytes if successful, null otherwise
		/// </returns>
		public static byte[] Receive(CStreamIO stream, CancellationToken token = default)
		{
			byte[] reply = default;
			int announcedSize = 0;
			try
			{
				if (default == stream) throw new ArgumentException(Resources.CStreamNoOpenedStream);

				// Read message from the server
				CLog.INFORMATION(Resources.CStreamWaitingMessageWithTimeout.Format(new object[] { stream.Tcp?.Client?.RemoteEndPoint, stream.Tcp?.Client?.ReceiveTimeout }));
				byte[] tmp = stream.Receive(out announcedSize, token);
				if (default != tmp)
				{
					// rebuild the buffer is required
					if (!stream.UseSizeHeader)
					{
						// the request natively contained a size header, meaningfull to the application, we therefore must reinsert the size header inside the received buffer
						reply = new byte[tmp.Length + stream.SizeHeader];
						byte[] bb = CMisc.SetBytesFromIntegralTypeValue((long)tmp.Length, false);
						Buffer.BlockCopy(bb, CStreamBase.EIGHTBYTES - stream.SizeHeader, reply, 0, (int)stream.SizeHeader);
						Buffer.BlockCopy(tmp, 0, reply, (int)stream.SizeHeader, tmp.Length);
					}
					else
						reply = tmp;
				}
				else
					CLog.INFOR(Resources.CStreamReceivedEmptyBuffer);
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// received message as a string if sucessful, null otherwise
		/// </returns>
		public static string ReceiveAsString(CStreamIO stream, CancellationToken token = default)
		{
			byte[] reply = Receive(stream, token);
			return (null != reply ? stream.Encoding.GetString(reply) : null);
		}
		/// <summary>
		/// Receives data of <paramref name="size"/> size on the indicated stream.
		/// Presence of a size header is not required.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="size">size of the buffer to receive</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// An array of bytes if successful, null otherwise
		/// </returns>
		public static byte[] Receive(CStreamIO stream, int size, CancellationToken token = default)
		{
			try
			{
				if (default == stream) throw new ArgumentException(Resources.CStreamNoOpenedStream);

				// Read message from the server
				CLog.INFORMATION(Resources.CStreamWaitingMessage.Format(stream.Tcp?.Client?.RemoteEndPoint));
				return stream.Receive(size, token);
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// received message as a string if sucessful, null otherwise
		/// </returns>
		public static string ReceiveLine(CStreamIO stream, string EOT = Chars.CRLF, CancellationToken token = default)
		{
			try
			{
				if (default == stream) throw new ArgumentException(Resources.CStreamNoOpenedStream);

				string s = stream.ReceiveLine(token, EOT);
				CLog.INFORMATION(Resources.CStreamReceivedStringMessage.Format(new object[] { (s.IsNullOrEmpty() ? 0 : s.Length), stream.Tcp?.Client?.RemoteEndPoint }));
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// An array of bytes or null if an error occured. 
		/// </returns>
		public static byte[] SendReceive(CStreamIO stream, byte[] request, CancellationToken token = default)
		{
			if (Send(stream, request, token))
				return Receive(stream, token);
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// received message as a string if sucessful, null otherwise
		/// </returns>
		public static string SendReceive(CStreamIO stream, string request, CancellationToken token = default)
		{
			//byte[] reply = SendReceive(stream, default != request ? Encoding.UTF8.GetBytes(request) : default, token);
			//return (default != reply ? Encoding.UTF8.GetString(reply) : default);
			byte[] reply = SendReceive(stream, default != request ? stream.Encoding.GetBytes(request) : default, token);
			return (default != reply ? stream.Encoding.GetString(reply) : default);
		}
		/// <summary>
		/// Sends a string message terminated by a <paramref name="EOT"/> string and receives a string response terminated by the same <paramref name="EOT"/> string.
		/// The stream must be opened and will remain so.
		/// No size header is added to <paramref name="request"/> but <paramref name="EOT"/> will to finish the message.
		/// The response does not need a size header but MUST be finished by <paramref name="EOT"/>.
		/// </summary>
		/// <param name="stream">the connected stream</param>
		/// <param name="request">the string message to send</param>
		/// <param name="EOT">the string marking the end of the message, default is <see cref="Chars.CRLF"/></param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// received string message if sucessful, null otherwise
		/// </returns>
		public static string SendReceiveLine(CStreamIO stream, string request, string EOT = Chars.CRLF, CancellationToken token = default)
		{
			if (SendLine(stream, request, EOT, token))
				return ReceiveLine(stream, EOT, token);
			return default;
		}
		/// <summary>
		/// Connects to a host, sends data then disconnects the stream.
		/// A size header of <see cref="CStreamBase.SizeHeader"/> bytes is added to <paramref name="buffer"/> if <see cref="CStreamBase.UseSizeHeader"/> is true.
		/// </summary>
		/// <param name="settings">the settings to use for opening the stream and sending the data</param>
		/// <param name="buffer">an array of bytes to send</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// true if successful, false otherwise
		/// </returns>
		public static bool ConnectSend(CStreamClientSettings settings, byte[] buffer, CancellationToken token = default)
		{
			if (buffer.IsNullOrEmpty()) return false;
			CStreamClientIO stream = ConnectToSend(settings, buffer);
			if (default != stream)
			{
				bool fOK = Send(stream, buffer, token);
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// true if successful, false otherwise
		/// </returns>
		public static bool ConnectSend(CStreamClientSettings settings, string buffer, CancellationToken token = default)
		{
			//return ConnectSend(settings, string.IsNullOrEmpty(buffer) ? default : Encoding.UTF8.GetBytes(buffer), token);
			return ConnectSend(settings, string.IsNullOrEmpty(buffer) ? default : settings.Encoding.GetBytes(buffer), token);
		}
		/// <summary>
		/// Connects to a host, sends a string message finished by <paramref name="EOT"/> and disconnects the stream.
		/// No size header is added to <paramref name="message"/> but <paramref name="EOT"/> will to finish the message.
		/// </summary>
		/// <param name="settings">the settings to use for opening the stream and sending the data</param>
		/// <param name="message">a string to send</param>
		/// <param name="EOT">the string marking the end of the message</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// true if successful, false otherwise
		/// </returns>
		public static bool ConnectSendLine(CStreamClientSettings settings, string message, string EOT = Chars.CRLF, CancellationToken token = default)
		{
			if (string.IsNullOrEmpty(message))
				return false;
			CStreamClientIO stream = ConnectToSend(settings, message);
			if (default != stream)
			{
				bool fOK = SendLine(stream, message, EOT, token);
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>v
		/// an array of bytes received in response if successful, null otherwise
		/// </returns>
		public static byte[] ConnectSendReceive(CStreamClientSettings settings, byte[] request, CancellationToken token = default)
		{
			byte[] reply = default;
			if (request.IsNullOrEmpty()) return reply;
			CStreamClientIO stream = ConnectToSend(settings, request);
			if (default != stream)
			{
				if (Send(stream, request, token))
					reply = Receive(stream, token);
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
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// an array of bytes received in response if successful, null otherwise
		/// </returns>
		public static string ConnectSendReceive(CStreamClientSettings settings, string request, CancellationToken token = default)
		{
			//byte[] reply = ConnectSendReceive(settings, Encoding.UTF8.GetBytes(request), token);
			//return (default != reply ? Encoding.UTF8.GetString(reply) : default);
			byte[] reply = ConnectSendReceive(settings, settings.Encoding.GetBytes(request), token);
			return (default != reply ? settings.Encoding.GetString(reply) : default);
		}
		/// <summary>
		/// This function prevents using any size header, using CR+LF as an EOT
		/// No size header is added to <paramref name="request"/> but <paramref name="EOT"/> will to finish the message.
		/// The response does not need a size header but MUST be finished by <paramref name="EOT"/>.
		/// </summary>
		/// <param name="settings"><see cref="CStreamClientSettings"/> to use</param>
		/// <param name="request">Request to send</param>
		/// <param name="EOT"><see cref="Chars.CRLF"/> is the default</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// The received string using <see cref="SendReceiveLine(CStreamIO, string, string, CancellationToken)"/>
		/// </returns>
		public static string ConnectSendReceiveLine(CStreamClientSettings settings, string request, string EOT = Chars.CRLF, CancellationToken token = default)
		{
			if (string.IsNullOrEmpty(request))
				return default;
			CStreamClientIO stream = ConnectToSend(settings, request);
			if (default != stream)
			{
				string reply = SendReceiveLine(stream, request, EOT, token);
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
		/// <param name="asstring">if true the buffer is sent using <see cref="SendLine(CStreamIO, string, string, CancellationToken)"/>, if false it will be sent using <see cref="Send(CStreamIO, byte[], CancellationToken)"/></param>
		/// <param name="EOT">the string marking the end of the message (only if <paramref name="asstring"/> is true)</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// a <see cref="CThread"/> object if successful, null otherwise
		/// </returns>
		static CThread SendAsync(SendAsyncType sendAsync, byte[] request, bool asstring, string EOT = Chars.CRLF, CancellationToken token = default)
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
					Token = token,
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
		/// The message is sent using <see cref="Send(CStreamIO, byte[],CancellationToken)"/>
		/// </summary>
		/// <param name="sendAsync">settings to start the thread</param>
		/// <param name="request">a string to send</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// a <see cref="CThread"/> object if successful, null otherwise
		/// </returns>
		public static CThread SendAsync(SendAsyncType sendAsync, byte[] request, CancellationToken token = default)
		{
			return SendAsync(sendAsync, request, false, Chars.CRLF, token);
		}
		/// <summary>
		/// Start a client thread to send and receive data.
		/// The message is sent using <see cref="Send(CStreamIO, byte[], CancellationToken)"/>
		/// </summary>
		/// <param name="sendAsync">settings to start the thread</param>
		/// <param name="request">a string to send</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// a <see cref="CThread"/> object if successful, null otherwise
		/// </returns>
		public static CThread SendAsync(SendAsyncType sendAsync, string request, CancellationToken token = default)
		{
			//return SendAsync(sendAsync, Encoding.UTF8.GetBytes(request), false, Chars.CRLF, token);
			return SendAsync(sendAsync, sendAsync.Settings.Encoding.GetBytes(request), false, Chars.CRLF, token);
		}
		/// <summary>
		/// Refer to <see cref="SendAsync(SendAsyncType, byte[], bool, string, CancellationToken)"/>
		/// The message is sent using <see cref="SendLine(CStreamIO, string, string, CancellationToken)"/>
		/// </summary>
		/// <param name="sendAsync">settings to start the thread</param>
		/// <param name="request">a string to send</param>
		/// <param name="EOT">the string marking the end of the message</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// a <see cref="CThread"/> object if successful, null otherwise
		/// </returns>
		public static CThread SendAsyncLine(SendAsyncType sendAsync, string request, string EOT = Chars.CRLF, CancellationToken token = default)
		{
			//return SendAsync(sendAsync, Encoding.UTF8.GetBytes(request), true, EOT, token);
			return SendAsync(sendAsync, sendAsync.Settings.Encoding.GetBytes(request), true, EOT, token);
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
		/// That function supports <see cref="SendAsync(SendAsyncType, byte[], bool, string, CancellationToken)"/> processing/
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
					//string reply = ConnectSendReceiveLine(threadParams.SendAsync.Settings, Encoding.UTF8.GetString(threadParams.Request), threadParams.EOT, threadParams.Token);
					string reply = ConnectSendReceiveLine(threadParams.SendAsync.Settings, threadParams.SendAsync.Settings.Encoding.GetString(threadParams.Request), threadParams.EOT, threadParams.Token);
					if (string.IsNullOrEmpty(reply))
					{
						res = SendAsyncEnum.NoData;
					}
					else
					{
						// forward reply to the caller
						//if (threadParams.SendAsync.OnReply(Encoding.UTF8.GetBytes(reply), thread, threadParams.SendAsync.Parameters))
						if (threadParams.SendAsync.OnReply(threadParams.SendAsync.Settings.Encoding.GetBytes(reply), thread, threadParams.SendAsync.Parameters))
							res = SendAsyncEnum.OK;
						else
							res = SendAsyncEnum.ReceiveError;
					}
				}
				else
				{
					byte[] reply = ConnectSendReceive(threadParams.SendAsync.Settings, threadParams.Request, threadParams.Token);
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
					//if (ConnectSendLine(threadParams.SendAsync.Settings, Encoding.UTF8.GetString(threadParams.Request), threadParams.EOT, threadParams.Token))
					if (ConnectSendLine(threadParams.SendAsync.Settings, threadParams.SendAsync.Settings.Encoding.GetString(threadParams.Request), threadParams.EOT, threadParams.Token))
						res = SendAsyncEnum.OK;
					else
						res = SendAsyncEnum.SendError;
				}
				else
				{
					if (ConnectSend(threadParams.SendAsync.Settings, threadParams.Request, threadParams.Token))
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
			public string EOT { get; set; } = Chars.CRLF;
			public CancellationToken Token = default;
		}
		#endregion
	}
}
