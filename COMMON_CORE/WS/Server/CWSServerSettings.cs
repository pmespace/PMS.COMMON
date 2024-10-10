//#define USE_WSBUFFER

using System.Net;
using COMMON;
using COMMON.WSServer.Client;

namespace COMMON.WSServer
{
	#region delegates
	/// <summary>
	/// Generic function call when the server is starting
	/// </summary>
	/// <param name="args">the args available at start time</param>
	/// <returns>
	/// true indicates the processing must carry on,
	/// false indicates the processing must stop, the server will stop
	/// </returns>
	public delegate bool OnStart(string[] args);
	/// <summary>
	/// Generic function called when a connection has been made to the WS server
	/// </summary>
	/// <param name="client"><see cref="IPEndPoint"/> object providing information about the requester</param>
	/// <param name="ID">[OUT] an object identifying the connection</param>
	/// <returns>
	/// true indicates the connection is approved and the processing must carry on,
	/// false indicates the connection is declined, the server will stop
	/// </returns>
	public delegate bool OnOpen(IPEndPoint client, out object ID);
	/// <summary>
	/// Generic function called when an existing connection is shutting down
	/// </summary>
	/// <param name="IP">the IP address of the disconnected process</param>
	/// <param name="client">a <see cref="CWSConnectedClient"/>
	/// object describing the connected client,
	/// IT MAY BE NULL IF AN ERROR OCCURRED WHILE TRYING TO SAVE IT</param>
	/// <param name="ID">the ID returned by <see cref="OnOpen"/> function</param>
	public delegate void OnClose(string IP, object ID, CWSConnectedClient client);
	/// <summary>
	/// Generic function call for any request
	/// </summary>
	/// <param name="client"><see cref="IPEndPoint"/> object providing information about the requester</param>
	/// <param name="ID">the ID returned by <see cref="OnOpen"/> function</param>
	/// <param name="input">input data to use to complete the request</param>
	/// <param name="output">[OUT] output data returned by the processing thread</param>
	/// <param name="nextAction">[OUT] populated from a <see cref="WSNextActionEnum"/> to indicate what are the next actions to perform after having processed the command</param>
	/// <returns>
	/// true means successful and processing will carry on,
	/// false means unsuccessful and next step will not be triggered
	/// </returns>
	public delegate bool OnCommand(IPEndPoint client, object ID,
#if USE_WSBUFFER
		CWSBuffer input,
		out byte[] output,
#else
		string input,
		out string output,
#endif
		out int nextAction);
	/// <summary>
	/// Generic function eventually call when the server is shutting down on request issued by the server itself
	/// </summary>
	public delegate void OnStop();
	#endregion

	public class CWSServerSettings : CWSSettings
	{
		#region constructor
		public CWSServerSettings() { }
		#endregion

		#region properties
		/// <summary>
		/// The prefix name to use to identify the WS server.
		/// That name will be used after the IP address to identify the pages.
		/// Default is "ws" (a "/" will be added before that prefix if not empty).
		/// </summary>
		public string WSName
		{
			get => _wsaddr;
			set
			{
				value = value.Replace(" ", "");
				if (!value.IsNullOrEmpty()) _wsaddr = value;
			}
		}
		string _wsaddr = "ws";
		/// <summary>
		/// The port number the WS server will read
		/// </summary>
		public int Port { get => _port; set => _port = IPEndPoint.MinPort <= value && value <= IPEndPoint.MaxPort ? value : _port; }
		int _port = IPEndPoint.MinPort;
		/// <summary>
		/// Called when the server starts the main process (the one which requires the services of the server)
		/// </summary>
		public OnStart OnStart { get; set; }
		/// <summary>
		/// Called when a connection is opened
		/// </summary>
		public OnOpen OnOpen { get; set; }
		/// <summary>
		/// Called when a login is requested
		/// </summary>
		public OnCommand OnLogin { get; set; }
		/// <summary>
		/// Called when a messge has been received
		/// </summary>
		public OnCommand OnRequest { get; set; }
		/// <summary>
		/// Called when a connection is closed
		/// </summary>
		public OnClose OnClose { get; set; }
		/// <summary>
		/// Called when a connection is closed
		/// </summary>
		public OnStop OnStop { get; set; }
		#endregion

		#region methods
		public override string ToString() => $"{base.ToString()}{Chars.SEPARATOR}WS Name: {WSName}{Chars.SEPARATOR}Port: {Port}";
		#endregion
	}
}