#define USE_WSBUFFER

using System;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Runtime;
using COMMON;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics.Eventing.Reader;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace COMMON.WSServer
{
	#region exceptions
	/// <summary>
	/// Generic exception
	/// </summary>
	class WSException : Exception
	{
		public WSException() { }
		public WSException(string s) : base(s) { }
	}
	#endregion

	#region enums
	/// <summary>
	/// Actions to perform by he WS server after any command has been processed
	/// </summary>
	[Flags]
	public enum WSNextActionEnum
	{
		None = 0x00,
		Restart = 0x01,
		Shutdown = 0x02,
		DisconnectClient = 0x04,
		ShutdownImmediately = 0x08,
		DisconnectImmediately = 0x10,
	}

	//[Flags]
	//public enum WSStatusEnum
	//{
	//	_none = 0x00,

	//	_start = 0x7FFFFFF0,
	//	Starting = 0x01,
	//	Started = 0x02,
	//	Stopping = 0x04,
	//	Stopped = 0x08,

	//	_connect = 0x7FFFFF0F,
	//	Connecting = 0x10,
	//	Connected = 0x20,
	//	Disconnecting = 0x40,
	//	Disconnected = 0x80,

	//	_login = 0x7FFFF0FF,
	//	Login = 0x100,
	//	Logged = 0x200,
	//	LoginDenied = 0x400,
	//	LoggedOut = 0x800,

	//	_listen = 0x7FFF0FFF,
	//	Listening = 0x1000,
	//	NotListening = 0x2000,
	//}

	public enum WSStatusEnum
	{
		_none,

		Starting,
		Started,
		Stopping,
		Stopped,

		BeforeConnect,
		AfterConnectSuccess,
		AfterConnectFailure,
		BeforeDisconnect,
		AfterDisconnect,

		BeforeLogin,
		AfterLoginSuccess,
		AfterLoginFailure,
		AfterLoginError,

		Listening,
		NotListening,
	}
	#endregion

	public abstract class CWSSettings
	{
		#region constructor
		public CWSSettings()
		{
			LoginRequired = false;
		}
		#endregion

		#region properties
		/// <summary>
		/// Indicates whether a client must log before being able to issue request,
		/// default is false
		/// </summary>
		public bool LoginRequired { get; set; }
		/// <summary>
		/// Buffer size to use when receiving data. 
		/// </summary>
		public int BufferSize { get => _buffersize; set => _buffersize = 0 < value ? value : _buffersize; }
		int _buffersize = 1024 * 5;
		/// <summary>
		/// True if the settings are valid for processing, false otherwise (preventing the WSServer to start)
		/// </summary>
		public virtual bool IsValid { get => true; }
		/// <summary>
		/// An event which will be set when the server has started and is waiting for incoming requests
		/// </summary>
		public ManualResetEvent StartedEvt { get => _startedevt; }
		ManualResetEvent _startedevt = new ManualResetEvent(false);
		/// <summary>
		/// An event which will be set when the server has stopped and no longer accepts incoming requests
		/// </summary>
		public ManualResetEvent EndedEvt { get => _endedevt; }
		ManualResetEvent _endedevt = new ManualResetEvent(false);
		#endregion

		#region methods
		public override string ToString() => $"Login required: {LoginRequired}{Chars.SEPARATOR}Is valid: {IsValid}";
		#endregion
	}

	public abstract class CWS
	{
		#region classes
		public class WSLoginResult
		{
			public bool Granted { get; set; }
			public string Reason { get; set; }
			[JsonExtensionData]
			public Dictionary<string, JToken> _extendedData;
		}
		#endregion

		#region methods
		/// <summary>
		/// Indicates whether a <see cref="WebSocket"/> is closing or not
		/// </summary>
		/// <param name="result">a <see cref="WebSocketReceiveResult"/> object detailing the reception</param>
		/// <param name="source">a <see cref="CancellationTokenSource"/> object managing the availability of the <paramref name="WS"/></param>
		/// <returns>
		/// true if the web socket is closing or closed,
		/// false otherwise
		/// </returns>
		public static bool IsClosingOnReceiveAsync(WebSocketReceiveResult result, CancellationTokenSource source) => (WebSocketMessageType.Close == result.MessageType || (null != result.CloseStatus ? WebSocketCloseStatus.Empty != result.CloseStatus : false) || (default != source ? source.Token.IsCancellationRequested : false));
		/// <summary>
		/// Populate a buffer from data received through a <see cref="WebSocket"/>.
		/// Only text data can be received.
		/// </summary>
		/// <param name="result">a <see cref="WebSocketReceiveResult"/> object detailing the reception</param>
		/// <param name="ab">a set of bytes as received from the <see cref="WebSocket"/></param>
		/// <param name="buffer">[REF] the current buffer that will be updated with a the received data</param>
		/// <param name="source">a <see cref="CancellationTokenSource"/> object managing the availability of the <paramref name="WS"/></param>
		/// <returns>
		/// true if successful, false otherwise
		/// </returns>
		public static bool PopulateReceivedBuffer(WebSocketReceiveResult result, byte[] ab, ref string buffer, CancellationTokenSource source)
		{
			try
			{
				if (!IsClosingOnReceiveAsync(result, source))
				{
					if (ab.IsNullOrEmpty()) ab = new byte[0];
					if (WebSocketMessageType.Text == result.MessageType)
					{
						buffer += Encoding.UTF8.GetString(ab, 0, ab.Length);
						return true;
					}
					else
						CLog.Add(new CLogMsgs()
					{
						new CLogMsg($"received data was not text and has been discarded", TLog.ERROR),
						new CLogMsg($"data: [{ab.AsHexString()}]", TLog.DEBUG),
					});
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			// web socket closed close or any other reason
			return false;
		}
		/// <summary>
		/// Sends a message to client connected to the WS server.
		/// </summary>
		/// <param name="WS">the <see cref="WebSocket"/> to use</param>
		/// <param name="message">the message to send</param>
		/// <param name="log">the message to log along with the data</param>
		/// <param name="source">a <see cref="CancellationTokenSource"/> object managing the availability of the <paramref name="WS"/></param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> object returning a bool, true if sending was successful, false otherwise
		/// </returns>
		public static async Task<bool> SendAsync(WebSocket WS, string message, string log, CancellationTokenSource source)
		{
			if (message.IsNullOrEmpty() || default == WS || source.IsCancellationRequested) return false;

			bool ok = false;

			// arrived the message can be sent
			try
			{
				CLog.Add(new CLogMsgs()
				{
					new CLogMsg($"{(log.IsNullOrEmpty() ? "sending message": log)} [{message.Length} bytes]", TLog.INFOR),
					new CLogMsg($"data: [{message}]", TLog.DEBUG),
				});
				await WS.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, source.Token);
				ok = true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return ok;
		}
		#endregion
	}
}