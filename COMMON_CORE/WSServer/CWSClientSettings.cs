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
using System.Reflection;

namespace COMMON.WSServer.Client
{
	#region delegates
	/// <summary>
	/// Allows indicating the current status of WS connection
	/// </summary>
	/// <param name="status">a <see cref="WSStatusEnum"/> object indicating the current status of the connection</param>
	/// <returns>
	/// true indicates the processing must carry on,
	/// false indicates the processing must stop
	/// </returns>
	public delegate bool OnStatus(WSStatusEnum status);
	/// <summary>
	/// Generic function call before the client tries to login to the server
	/// </summary>
	/// <param name="credentials">[OUT] credentiels to use to login to the server</param>
	/// <returns>
	/// true if login must be performed,
	/// false indicates the processing must stop
	/// </returns>
	public delegate bool OnLogin(out string credentials);
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
	public delegate bool OnCommand(IPEndPoint client, object ID, string input, out string output, out int nextAction);
	/// <summary>
	/// Generic function eventually call when the server is shutting down on request issued by the server itself
	/// </summary>
	public delegate void OnStop();
	#endregion



	public class CWSClientSettings : CWSSettings
	{
		#region constructor
		public CWSClientSettings() { }
		#endregion

		#region properties
		/// <summary>
		/// Address of the web socket server to reach including the IP or URL, page and the port
		/// </summary>
		public string URI { get; set; }
		/// <summary>
		/// Called whenevr the status of the WS connection changes
		/// </summary>
		public OnStatus OnStatus { get; set; }
		public OnLogin OnLogin { get; set; }
		#endregion

		#region methods
		public override string ToString() => $"{base.ToString()}; URI: {URI}";
		#endregion
	}
}