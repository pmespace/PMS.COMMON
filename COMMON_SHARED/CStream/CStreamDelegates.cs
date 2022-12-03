using System.Runtime.InteropServices;
using System.Net.Sockets;

namespace COMMON
{
	public class CStreamParameters
	{
		public object Global { get; set; }
		public object Local { get; set; }
	}

	[ComVisible(false)]
	public class CStreamDelegates
	{
		#region server delegates
		/// <summary>
		/// Function called when the server thread starts, before having received any request to process.
		/// </summary>
		/// <param name="threadData">Structure describing the thread communication means</param>
		/// <param name="parameters">Private parameters passed from the calling process to the thread</param>
		/// <returns>FALSE if the server must stop immediately before receiving any request, TRUE if the server must carry on</returns>
		public delegate bool ServerOnStartDelegate(CThreadData threadData, object parameters);
		/// <summary>
		/// Function called when a client got connected to the server
		/// </summary>
		/// <param name="tcpclient">TCP client which connected to the server</param>
		/// <param name="thread">Structure describing the thread</param>
		/// <param name="parameters">Private parameters passed from the calling process to the thread</param>
		/// <param name="privateData">Private data of the client process; if allocated here, the object will be passed to any subsequent function linked to the current client</param>
		/// <returns>FALSE if the server must stop immediately before receiving any request, TRUE if the server must carry on</returns>
		public delegate bool ServerOnConnectDelegate(TcpClient tcpclient, CThread thread, object parameters, ref object privateData);
		/// <summary>
		/// Function called inside server context to process a server received message and prepare a reply
		/// </summary>
		/// <param name="tcpclient">TCP client which connected to the server</param>
		/// <param name="request">Request received as a byte array</param>
		/// <param name="addBufferSize">Indicates whether the size header has been added or not when creating the reply</param>
		/// <param name="thread">Structure describing the thread</param>
		/// <param name="parameters">Private parameters passed from the calling process to the thread</param>
		/// <param name="privateData">Private data of the client process; allocated or not during <see cref="ServerOnConnectDelegate"/> processing</param>
		/// <param name="reserved">Private object to use ONLY if calling asynchronous server functions like <see cref="CStreamServer.Send1WayNotification(byte[], bool, string, object)"/>, in this case this data must be passed to the finction</param>
		/// <returns>A message to send or null if no message to send back</returns>
		public delegate byte[] ServerOnMessageDelegate(TcpClient tcpclient, byte[] request, out bool addBufferSize, CThread thread, object parameters, object privateData, object reserved);
		/// <summary>
		/// Function called when a client disconnects from the server
		/// </summary>
		/// <param name="tcpclient">The remote address being disconnected</param>
		/// <param name="thread">Structure describing the thread</param>
		/// <param name="parameters">Private parameters passed from the calling process to the thread</param>
		/// <param name="statistics">Private data of the client process; allocated or not during <see cref="ServerOnConnectDelegate"/> processing</param>
		public delegate void ServerOnDisconnectDelegate(TcpClient tcpclient, CThread thread, object parameters, CStreamServerStatistics statistics);
		/// <summary>
		/// Function called when a the server has received a stop request from any client
		/// </summary>
		/// <param name="threadData">Structure describing the thread communication means</param>
		/// <param name="parameters">Private parameters passed from the calling process to the thread</param>
		public delegate void ServerOnStopDelegate(CThreadData threadData, object parameters);
		#endregion

		#region client delegates
		/// <summary>
		/// Function called inside server context to process a server received message and prepare a reply
		/// </summary>
		/// <param name="msg">Message received from the server as a byte array</param>
		/// <param name="addBufferSize">Indicates whether the size header has been added or not when creating the reply</param>
		/// <param name="timer">Timer to use if a message is to send after having processed the received one</param>
		/// <param name="header">String to use when logging</param>
		/// <param name="stopClient">An indicator set to true by the applictaion is the client must stop after the message, false otherwise</param>
		/// <param name="thread">Thread data as given by the creator of the thread</param>
		/// <param name="parameters">Private parameters passed from the calling process to the thread</param>
		/// <returns>A reply to send back in byte array format or NULL if the server must stop receiving messages</returns>
		public delegate byte[] ClientOnReceivedMessageDelegate(byte[] msg, out bool addBufferSize, out int timer, out string header, out bool stopClient, CThread thread, object parameters);
		/// <summary>
		/// Function called inside server context to process a server received message and prepare a reply
		/// </summary>
		/// <param name="msg">Message received from the server as a byte array</param>
		/// <param name="addBufferSize">Indicates whether the size header has been added or not when creating the reply</param>
		/// <param name="thread">Structure describing the thread</param>
		/// <param name="parameters">Private parameters passed from the calling process to the thread</param>
		/// <returns>A reply to send back in byte array format or NULL if the server must stop receiving messages</returns>
		public delegate void ClientOnSendMessageDelegate(byte[] msg, bool addBufferSize, CThread thread, object parameters);
		/// <summary>
		/// Function called when a client thread received a reply which has been validated.
		/// It is called in the context of the client who can take actions dependinf on the received message.
		/// </summary>
		/// <param name="thread">Structure describing the thread</param>
		/// <param name="reply">Reply as received</param>
		/// <param name="error">True if an error occurred while receiving the reply</param>
		/// <param name="parameters">Private parameters passed from the calling process to the thread</param>
		/// <returns>True if processing was OK, False otherwise</returns>
		public delegate bool ClientOnReplyDelegate(byte[] reply, bool error, CThread thread, object parameters);
		#endregion

		#region miscellaneous
		/// <summary>
		/// Delegate function that will be called when a message either to be sent or having been received, is about to be logged.
		/// Implementing this function allows to prevent logging a message or to alter its content to avoid logging confidential data.
		/// Not implementing this function makes the data is fully logged in the log file.
		/// The function is called whenever any a message received or to send is ready to be logged; altering the data allows hiding potentially confidential data
		/// This allows PCI-DSS compliance
		/// </summary>
		/// <param name="bytes">The message about to be logged as an array of bytes</param>
		/// <param name="current">The message about to be logged as a string</param>
		/// <param name="isRequest">True if the message is received by the caller, false if the message is about to be sent by the caller</param>
		/// <returns>The message to log, it can be the current one (not altered), a modifier message or null (no logging is performed)</returns>
		public delegate string ClientServerOnMessageToLog(byte[] bytes, string current, bool isRequest);
		#endregion
	}
}
