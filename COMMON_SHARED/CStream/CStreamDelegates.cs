using System.Runtime.InteropServices;
using System.Net.Sockets;

namespace COMMON
{
	[ComVisible(false)]
	public class CStreamDelegates
	{
		#region server delegates
		/// <summary>
		/// Function called when the server thread starts, before having received any request to process.
		/// </summary>
		/// <param name="threadData">Thread ID as given by the creator of the thread</param>
		/// <param name="o">Private parameters to pass to the thread</param>
		/// <returns>FALSE if the server must stop immediately before receiving any request, TRUE if the server must carry on</returns>
		public delegate bool ServerOnStartDelegate(CThreadData threadData, object o);
		/// <summary>
		/// Function called when a client got connected to the server
		/// </summary>
		/// <param name="client">TCP client which connected to the server</param>
		/// <param name="threadData">Thread ID as given by the creator of the thread</param>
		/// <param name="o">Private parameters to pass to the thread</param>
		/// <returns>FALSE if the server must stop immediately before receiving any request, TRUE if the server must carry on</returns>
		public delegate bool ServerOnConnectDelegate(TcpClient client, CThreadData threadData, object o);
		/// <summary>
		/// Function called inside server context to process a server received message and prepare a reply
		/// </summary>
		/// <param name="client">TCP client which connected to the server</param>
		/// <param name="request">Request received as a byte array</param>
		/// <param name="addBufferSize">Indicates whether the size header has been added or not when creating the reply</param>
		/// <param name="threadData">Thread ID as given by the creator of the thread</param>
		/// <param name="o">Private parameters to pass to the thread</param>
		/// <returns>A message to send or null if no message to send back</returns>
		public delegate byte[] ServerOnMessageDelegate(TcpClient client, byte[] request, out bool addBufferSize, CThreadData threadData, object o);
		/// <summary>
		/// Function called when a client disconnects from the server
		/// </summary>
		/// <param name="remoteClient">The remote address being disconnected</param>
		/// <param name="threadData">Thread ID as given by the creator of the thread</param>
		/// <param name="o">Private parameters to pass to the thread</param>
		public delegate void ServerOnDisconnectDelegate(string remoteClient, CThreadData threadData, object o);
		/// <summary>
		/// Function called when a the server has received a stop request from any client
		/// </summary>
		/// <param name="threadData">Thread ID as given by the creator of the thread</param>
		/// <param name="o">Private parameters to pass to the thread</param>
		public delegate void ServerOnStopDelegate(CThreadData threadData, object o);
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
		/// <param name="threadData">Thread data as given by the creator of the thread</param>
		/// <param name="o">Private parameters to pass to the thread</param>
		/// <returns>A reply to send back in byte array format or NULL if the server must stop receiving messages</returns>
		public delegate byte[] ClientOnReceivedMessageDelegate(byte[] msg, out bool addBufferSize, out int timer, out string header, out bool stopClient, CThreadData threadData, object o);
		/// <summary>
		/// Function called inside server context to process a server received message and prepare a reply
		/// </summary>
		/// <param name="msg">Message received from the server as a byte array</param>
		/// <param name="addBufferSize">Indicates whether the size header has been added or not when creating the reply</param>
		/// <param name="threadData">Thread ID as given by the creator of the thread</param>
		/// <param name="o">Private parameters to pass to the thread</param>
		/// <returns>A reply to send back in byte array format or NULL if the server must stop receiving messages</returns>
		public delegate void ClientOnSendMessageDelegate(byte[] msg, bool addBufferSize, CThreadData threadData, object o);
		/// <summary>
		/// Function called when a client thread received a reply which has been validated.
		/// It is called in the context of the client who can take actions dependinf on the received message.
		/// </summary>
		/// <param name="threadData">Thread ID as given by the creator of the thread</param>
		/// <param name="reply">Reply as received</param>
		/// <param name="timeout">True if a timeout occurred </param>
		/// <param name="o">Private parameters to pass to the thread</param>
		/// <returns>True if processing was OK, False otherwise</returns>
		public delegate bool ClientOnReplyDelegate(byte[] reply, bool timeout, CThreadData threadData, object o);
		#endregion
	}
}
