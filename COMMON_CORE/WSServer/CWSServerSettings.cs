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

namespace COMMON.WSServer
{
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
	/// <param name="ID">[OUT] a string naming the connection</param>
	/// <returns>
	/// true indicates the connection is approved and the processing must carry on,
	/// false indicates the connection is declined, the server will stop
	/// </returns>
	public delegate bool OnOpen(IPEndPoint client, out string ID);
	/// <summary>
	/// Generic function called when an existing connection is shutting down
	/// </summary>
	/// <param name="IP">the IP address of the disconnected process</param>
	/// <param name="client">a <see cref="CWSClient"/>
	/// object describing the connected client,
	/// IT MAY BE NULL IF AN ERROR OCCURRED WHILE TRYING TO SAVE IT</param>
	/// <param name="ID">the ID returned by <see cref="OnOpen"/> function</param>
	public delegate void OnClose(string IP, CWSClient client, string ID);
	/// <summary>
	/// Generic function call for any request
	/// </summary>
	/// <param name="client"><see cref="IPEndPoint"/> object providing information about the requester</param>
	/// <param name="input">input data to use to complete the request</param>
	/// <param name="output">output data returned by the processing thread</param>
	/// <returns>
	/// true means successful and processing will carry on,
	/// false means unsuccessful and next step will not be triggered
	/// </returns>
	public delegate bool OnRequest(IPEndPoint client, string input, out string output);

	public class CWSServerSettings
	{
		#region properties
		///// <summary>
		///// Type of messages that can be exchanged inside with the WS server.
		///// Only <see cref="WebSocketMessageType.Text"/> and <see cref="WebSocketMessageType.Binary"/> can be specified.
		///// </summary>
		//public WebSocketMessageType MessageType { get => _messagetype; set => _messagetype = WebSocketMessageType.Text == value || WebSocketMessageType.Binary == value ? value : _messagetype; }
		//WebSocketMessageType _messagetype = WebSocketMessageType.Text;
		/// <summary>
		/// The prefix name to use to identify the WS server.
		/// That name will be used after the IP address to identify the pages.
		/// Default is "ws" (a "/" will be added before that prefix if not empty).
		/// </summary>
		public string WSName
		{
			get => _wsname;
			set
			{
				value = value.Replace(" ", "");
				if (!value.IsNullOrEmpty()) _wsname = value;
			}
		}
		string _wsname = "ws";
		/// <summary>
		/// The port number the WS server will read
		/// </summary>
		public int Port { get => _port; set => _port = IPEndPoint.MinPort <= value && value <= IPEndPoint.MaxPort ? value : _port; }
		int _port = IPEndPoint.MinPort;
		/// <summary>
		/// Indicates whether a client must log before being able to issue request
		/// </summary>
		public bool LoginRequired { get; set; }
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
		public OnRequest OnLogin { get; set; }
		/// <summary>
		/// Called when a messge has been received
		/// </summary>
		public OnRequest OnCommand { get; set; }
		///// <summary>
		///// Called when a logout is requested
		///// </summary>
		//public OnRequest OnLogout { get; set; }
		/// <summary>
		/// Called when a connection is closed
		/// </summary>
		public OnClose OnClose { get; set; }
		/// <summary>
		/// Buffer size to use when receiving data. 
		/// </summary>
		public int BufferSize { get => _buffersize; set => _buffersize = 0 < value ? value : _buffersize; }
		int _buffersize = 1024 * 5;
		/// <summary>
		/// True if the settings are valid for processing, false otherwise (preventing the WSServer to start)
		/// </summary>
		public bool IsValid { get => null != OnStart && ((LoginRequired && null != OnLogin) || !LoginRequired) && null != OnCommand; }
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
		public override string ToString() => $"Login required: {LoginRequired}; OnStart: {OnStart}; OnLogin: {OnLogin}; OnCommand: {OnCommand}; Is valid: {IsValid}";
		#endregion
	}
}