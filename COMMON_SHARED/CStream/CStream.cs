using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Net;
using System;

namespace COMMON
{
	[ComVisible(true)]
	public enum SendAsyncEnum
	{
		OK = 0,
		KO,
		NoData,
		Timeout,
		SendError,
		ReceiveError,
	}

	/// <summary>
	/// SSL or IP stream class
	/// </summary>
	[ComVisible(false)]
	public static class CStream
	{
		#region methods
		public static string Localhost()
		{
			return IPAddress.Loopback.ToString();
			//IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
			//foreach (IPAddress ip in localIPs)
			//	if (AddressFamily.InterNetwork == ip.AddressFamily)
			//		return ip.ToString();
			//// no local IP address
			//return string.Empty;
		}
		/// <summary>
		/// Connect a stream according to the settings provided.
		/// </summary>
		/// <param name="settings">Network settings to use to connect the stream</param>
		/// <returns>A <see cref="CStreamClientIO"/> object is evrything went fine, null otherwise</returns>
		public static CStreamClientIO Connect(CStreamClientSettings settings)
		{
			TcpClient tcpclient = new TcpClient();
			CStreamClientIO stream = null;
			try
			{
				tcpclient.Connect(settings.Address, (int)settings.Port);
				tcpclient.SendBufferSize = (tcpclient.SendBufferSize >= settings.SendBufferSize ? tcpclient.SendBufferSize : settings.SendBufferSize + 1);
				tcpclient.ReceiveBufferSize = (tcpclient.ReceiveBufferSize >= settings.ReceiveBufferSize ? tcpclient.SendBufferSize : settings.ReceiveBufferSize);
				tcpclient.SendTimeout = settings.SendTimeout * CStreamSettings.ONESECOND;
				tcpclient.ReceiveTimeout = settings.ReceiveTimeout * CStreamSettings.ONESECOND;
				// Create an SSL stream that will close the client's stream.
				stream = new CStreamClientIO(tcpclient, settings);
			}
			catch (Exception)
			{
				tcpclient.Close();
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
		/// <param name="addBufferSize">Indicates whether a buffer size block must be added before the buffer to send</param>
		/// <returns>An arry of bytes received in response or if an error occured. In case of a client only request, the function returns the request, as no reply can be returned, if everything went right</returns>
		public static bool Send(CStreamIO stream, byte[] request, bool addBufferSize)
		{
			if (null == stream)
				return false;
			if (null == request || 0 == request.Length)
				return false;
			bool fOK = false;
			try
			{
				// Send message to the server
				CLog.Add("Sending message (message size: " + (addBufferSize ? request.Length : request.Length - (int)stream.LengthBufferSize) + ")");
				if (stream.Send(request, addBufferSize))
				{
					fOK = true;
				}
				else
				{
					CLog.Add("NO MESSAGE HAS BEEN SENT");
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
			return fOK;
		}
		public static bool Send(CStreamIO stream, string request)
		{
			byte[] brequest = string.IsNullOrEmpty(request) ? null : Encoding.UTF8.GetBytes(request);
			return Send(stream, brequest, true);
		}
		/// <summary>
		/// Receive data on the indicated stream.
		/// </summary>
		/// <param name="stream">The connected stream</param>
		/// <param name="replySize">The size of the reply as received</param>
		/// <param name="addBufferSize">Indicates whether a buffer size block must be added before the buffer to send</param>
		/// <param name="timeout">True indicates the function ended up witha timeout, false otherwise</param>
		/// <returns>An arry of bytes received in response or if an error occured. In case of a client only request, the function returns the request, as no reply can be returned, if everything went right</returns>
		public static byte[] Receive(CStreamIO stream, out int replySize, out bool timeout, bool addBufferSize)
		{
			byte[] reply = null;
			replySize = 0;
			timeout = false;
			if (null == stream)
				return null;
			try
			{
				// Read message from the server
				CLog.Add("Waiting to receive a message (buffer size: " + stream.Tcp.ReceiveBufferSize + ")");
				byte[] tmp = stream.Receive(out replySize);
				if (null != tmp)
				{
					CLog.Add("Received message (size: " + (addBufferSize ? tmp.Length : tmp.Length - (int)stream.LengthBufferSize) + ")");
					// rebuild the buffer is required
					if (!addBufferSize)
					{
						// the request was already carrying a size header, we therefore must reinsert the size header inside the received buffer
						reply = new byte[tmp.Length + stream.LengthBufferSize];
						byte[] bb = CMisc.SetBytesFromIntegralTypeValue(tmp.Length, stream.LengthBufferSize);
						Buffer.BlockCopy(bb, 0, reply, 0, stream.LengthBufferSize);
						Buffer.BlockCopy(tmp, 0, reply, stream.LengthBufferSize, tmp.Length);
					}
					else
						reply = tmp;
				}
				else
				{
					CLog.Add("No data received");
					timeout = true;
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
			return reply;
		}
		public static string Receive(CStreamIO stream, out int replySize, out bool timeout)
		{
			return Encoding.UTF8.GetString(Receive(stream, out replySize, out timeout, true));
		}
		/// <summary>
		/// Send (<see cref="Send(CStreamIO, byte[], bool)"/> and <see cref="Send(CStreamIO, string)"/>),
		/// then receive data (<see cref="Receive(CStreamIO, out int, out bool, bool)"/>.
		/// The stream must pre-exist
		/// </summary>
		/// <param name="stream">The connected stream</param>
		/// <param name="request">A array of bytes to send</param>
		/// <param name="replySize">The size of the reply as received</param>
		/// <param name="addBufferSize">Indicates whether a buffer size block must be added before the buffer to send</param>
		/// <param name="timeout">True indicates the function ended up witha timeout, false otherwise</param>
		/// <returns></returns>
		public static byte[] SendReceive(CStreamIO stream, byte[] request, bool addBufferSize, out int replySize, out bool timeout)
		{
			replySize = 0;
			timeout = false;
			if (Send(stream, request, addBufferSize))
				return Receive(stream, out replySize, out timeout, addBufferSize);
			return null;
		}
		public static string SendReceived(CStreamIO stream, string request, out int replySize, out bool timeout)
		{
			return Encoding.UTF8.GetString(SendReceive(stream, Encoding.UTF8.GetBytes(request), true, out replySize, out timeout));
		}
		/// <summary>
		/// Connect (<see cref="Connect(CStreamClientSettings)"/>) and send data (<see cref="Send(CStreamIO, byte[], bool)"/> and <see cref="Send(CStreamIO, string)"/>).
		/// </summary>
		/// <param name="settings">The settings to use for sending data</param>
		/// <param name="request">A array of bytes to send</param>
		/// <param name="addBufferSize">Indicates whether a buffer size block must be added before the buffer to send</param>
		/// <returns>An arry of bytes received in response or if an error occured. In case of a client only request, the function returns the request, as no reply can be returned, if everything went right</returns>
		public static bool ConnectSend(CStreamClientSettings settings, byte[] request, bool addBufferSize)
		{
			bool fOK;
			// adjust buffer size according to buffer to send
			int fullBufferSize = (addBufferSize ? request.Length + (int)settings.LengthBufferSize : request.Length);
			settings.SendBufferSize = (settings.SendBufferSize > fullBufferSize ? settings.SendBufferSize : fullBufferSize + 1);
			CStreamClientIO stream = Connect(settings);
			if (fOK = (null != stream))
			{
				fOK = Send(stream, request, addBufferSize);
				Disconnect(stream);
			}
			return fOK;
		}
		public static bool ConnectSend(CStreamClientSettings settings, string request)
		{
			return ConnectSend(settings, Encoding.UTF8.GetBytes(request), true);
		}
		/// <summary>
		/// Connect (<see cref="Connect(CStreamClientSettings)"/>), send (<see cref="Send(CStreamIO, byte[], bool)"/> and <see cref="Send(CStreamIO, string)"/>) and receive (<see cref="Receive(CStreamIO, out int, out bool, bool)"/> and <see cref="Receive(CStreamIO, out int, out bool)"/>) data.
		/// </summary>
		/// <param name="settings">The settings to use for sending data</param>
		/// <param name="request">A array of bytes to send</param>
		/// <param name="addBufferSize">Indicates whether a buffer size block must be added before the buffer to send</param>
		/// <param name="replySize">The size of the reply as received</param>
		/// <param name="timeout">True indicates the function ended up witha timeout, false otherwise</param>
		/// <returns>An arry of bytes received in response or if an error occured. In case of a client only request, the function returns the request, as no reply can be returned, if everything went right</returns>
		public static byte[] ConnectSendReceive(CStreamClientSettings settings, byte[] request, bool addBufferSize, out int replySize, out bool timeout)
		{
			timeout = false;
			byte[] reply = null;
			replySize = 0;
			// adjust buffer size according to buffer to send
			int fullBufferSize = (addBufferSize ? request.Length + (int)settings.LengthBufferSize : request.Length);
			settings.SendBufferSize = (settings.SendBufferSize > fullBufferSize ? settings.SendBufferSize : fullBufferSize + 1);
			CStreamClientIO stream = Connect(settings);
			if (null != stream)
			{
				if (Send(stream, request, addBufferSize))
					reply = Receive(stream, out replySize, out timeout, addBufferSize);
				Disconnect(stream);
			}
			return reply;
		}
		public static string ConnectSendReceive(CStreamClientSettings settings, string request, out int replySize, out bool timeout)
		{
			return Encoding.UTF8.GetString(ConnectSendReceive(settings, Encoding.UTF8.GetBytes(request), true, out replySize, out timeout));
		}
		/// <summary>
		/// Start a client thread to send and receive data.
		/// The caller is warned when a reply is received by a call to the specified function (<see cref="SendAsyncType"/>).
		/// </summary>
		/// <param name="sendAsync">Settings to start the thread</param>
		/// <param name="request">The request as bytes array</param>
		/// <param name="addBufferSize">Indicates whether or not adding a size header when sending the request</param>
		/// <returns>A <see cref="CThread"/> object if the thread has been started, null otherwise</returns>
		public static CThread SendAsync(SendAsyncType sendAsync, byte[] request, bool addBufferSize)
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
					AddBufferSize = addBufferSize,
					ClientOnly = null == sendAsync.OnReply,
				};
				// prepare the thread object
				CThread thread = new CThread();
				if (thread.Start(SendAsyncThreadMethod, sendAsync.ThreadData, threadParams))
					return thread;
				else
					thread = null;
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
			return null;
		}
		public static CThread SendAsync(SendAsyncType sendAsync, string request)
		{
			return SendAsync(sendAsync, Encoding.UTF8.GetBytes(request), true);
		}
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
			/// <summary>
			/// Parameters to pass to the <see cref="OnReply"/> function
			/// </summary>
			public object Parameters { get; set; } = null;
		}
		/// <summary>
		/// <see cref="CThread.CThreadFunction"/>.
		/// That function supports <see cref="SendAsync(SendAsyncType, byte[], bool)"/> processing/
		/// </summary>
		/// <param name="threadData"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		[ComVisible(false)]
		private static int SendAsyncThreadMethod(CThreadData threadData, object o)
		{
			SendAsyncEnum res = SendAsyncEnum.KO;
			ClientThreadType clientThread = (ClientThreadType)o;
			CLog.Add("SendAsync - Connecting to: " + clientThread.SendAsync.Settings.FullIP);
			if (null != clientThread.SendAsync.OnReply)
			{
				// send & receive 
				byte[] reply = ConnectSendReceive(clientThread.SendAsync.Settings, clientThread.Request, clientThread.AddBufferSize, out int replySize, out bool timeout);
				if (null == reply || 0 == reply.Length)
				{
					res = SendAsyncEnum.NoData;
				}
				else if (reply.Length != replySize)
				{
					res = SendAsyncEnum.ReceiveError;
				}
				else
				{
					// forward reply to the caller
					if (clientThread.SendAsync.OnReply(reply, timeout, threadData, o))
						res = SendAsyncEnum.OK;
					else
						res = SendAsyncEnum.ReceiveError;
				}
			}
			else
			{
				// send only
				if (ConnectSend(clientThread.SendAsync.Settings, clientThread.Request, clientThread.AddBufferSize))
					res = SendAsyncEnum.OK;
				else
					res = SendAsyncEnum.SendError;
			}
			CLog.Add("SendAsync - Result: " + res.ToString(), SendAsyncEnum.OK == res ? TLog.INFOR : TLog.ERROR);
			return (int)res;
		}
		class ClientThreadType
		{
			public SendAsyncType SendAsync { get; set; }
			public byte[] Request { get; set; } = null;
			public bool AddBufferSize { get; set; } = true;
			public bool ClientOnly { get; set; } = false;
		}
		#endregion
	}
}
