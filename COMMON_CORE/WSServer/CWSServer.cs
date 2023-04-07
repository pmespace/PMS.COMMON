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
using System.Collections.ObjectModel;
using System.Data;

namespace COMMON.WSServer
{
	/// <summary>
	/// A Web Socket (WS) server ready to use,
	/// only <see cref="WebSocketMessageType.Text"/> messages are supported,
	/// </summary>
	public class CWSServer
	{
		#region constructor
		/// <summary>
		/// Allows creating a simple WS server,
		/// allowing only connections from the local host,
		/// allowing only 1 connection at a time		
		/// </summary>
		public CWSServer() => Initialise();
		/// <summary>
		/// Allows creating a simple WS server,
		/// allowing connections as specified by the caller,
		/// allowing as many connections as indicated
		/// </summary>
		/// <param name="allowDistantConnections">if true connections from clients running on another devices are accepted,
		/// if false only connections from the same device are accepted</param>
		/// <param name="maxNumberOfConnections">maximum number of concurrent connections to the WS server, 0 means no limit</param>
		/// <param name="allowedips">a <see cref="List{String}"/> detailing IPs authorised to connect to the server,
		/// if null or empty only local host IP is accepted,
		/// if <paramref name="allowDistantConnections"/> is false that list is not considered</param>
		public CWSServer(bool allowDistantConnections, int maxNumberOfConnections, List<string> allowedips)
		{
			Initialise();
			AllowNonLocalConnection = allowDistantConnections;
			MaxNumberOfConcurrentConnections = maxNumberOfConnections;
			if (default != allowedips && 0 != allowedips.Count) _allowedips = new List<string>(allowedips);
		}
		void Initialise()
		{
			Source = new CancellationTokenSource();
			MaxNumberOfConcurrentConnections = 1;
			AllowNonLocalConnection = false;
			_clients = new CWSClients();
			Clients = new CWSReadOnlyClients(_clients);
			_allowedips = new List<string>() { CStream.Localhost() };
		}
		#endregion

		#region properties
		/// <summary>
		/// If true connections can originate from a distant address,
		/// if true only the local host can connect to the server,
		/// default is false.
		/// </summary>
		public bool AllowNonLocalConnection { get; private set; }
		public ReadOnlyCollection<string> AllowedIPs { get; private set; }
		List<string> _allowedips;
		/// <summary>
		/// Maximum number of connections the server can accept at a time,
		/// default is 1 connection,
		/// 0 means no limit.
		/// </summary>
		public int MaxNumberOfConcurrentConnections
		{
			get => _maxnumberofconcurrentconnections;
			protected set => _maxnumberofconcurrentconnections = 0 <= value ? value : _maxnumberofconcurrentconnections;
		}
		int _maxnumberofconcurrentconnections = 1;
		/// <summary>
		/// All currently connected clients, described by a <see cref="CWSClient"/> object, the key is connected client IP:port address
		/// </summary>
		public CWSReadOnlyClients Clients { get; private set; }
		CWSClients _clients;
		#endregion

		#region private properties
		CancellationTokenSource Source { get; set; }
		object mylock = new object();
		#endregion

		#region classes
		enum WSActionEnum
		{
			Error = -1,
			None,
			WaitingForLogin,
			LoginOK,
			LoginKO,
			Connected,
			WaitingCommand,
			ReceivedReply,
			ReceivedCommand,
			ReceivedNotification,
		}

		public enum WSResultEnum
		{
			OK,
			KO,
			Restart,
			Shutdown,
			Exception,
			FailedToStart,
			InvalidSettings,
			InvalidRequest,
			ConnectionDenied,
			AccessDenied,
		}

		#region wsbuffer
		//class WSBuffer
		//{
		//	#region constructor
		//	public WSBuffer() { Reset(); }
		//	#endregion

		//	#region properties
		//	/// <summary>
		//	/// Buffer itself
		//	/// </summary>
		//	public object Data { get => default != request ? (default == brequest ? null : brequest) : request; }
		//	/// <summary>
		//	/// Length of buffer
		//	/// </summary>
		//	public int Length { get => IsBinary ? brequest.Length : request.Length; }
		//	/// <summary>
		//	/// True if the buffer is binary (byte[]), false if text (string)
		//	/// </summary>
		//	public bool IsBinary { get => default != brequest; }
		//	#endregion

		//	#region private
		//	string request;
		//	byte[] brequest;
		//	#endregion

		//	#region methods
		//	public override string ToString() => IsBinary ? CMisc.AsHexString(Data as byte[]) : Data.ToString();
		//	/// <summary>
		//	/// Reset buffer.
		//	/// This must be called each timethe buffer has been used.
		//	/// </summary>
		//	internal void Reset()
		//	{
		//		request = default;
		//		brequest = default;
		//	}
		//	/// <summary>
		//	/// Update the WS server buffer from the received data
		//	/// </summary>
		//	/// <param name="res"><see cref="WebSocketReceiveResult"/> object describing what happened when reading data</param>
		//	/// <param name="ab">The received buffer</param>
		//	/// <returns>
		//	/// true if data has been received and saved,
		//	/// false if it was a close message or an error has occurred
		//	/// </returns>
		//	public bool Receive(WebSocketReceiveResult res, byte[] ab)
		//	{
		//		try
		//		{
		//			if (WebSocketMessageType.Text == res.MessageType)
		//			{
		//				request += Encoding.UTF8.GetString(ab, 0, res.Count);
		//				return true;
		//			}
		//			else if (WebSocketMessageType.Binary == res.MessageType)
		//			{
		//				if (default == brequest) brequest = new byte[0];
		//				byte[] tmp = new byte[brequest.Length + res.Count];
		//				Buffer.BlockCopy(brequest, 0, tmp, 0, brequest.Length);
		//				Buffer.BlockCopy(ab, 0, tmp, brequest.Length, res.Count);
		//				brequest = tmp;
		//				return true;
		//			}
		//		}
		//		catch (Exception _ex_)
		//		{
		//			CLog.EXCEPT(_ex_);
		//		}
		//		// close or any other reason
		//		return false;
		//	}
		//	#endregion
		//}
		#endregion

		#endregion

		#region methods
		/// <summary>
		/// Starts the WS server itself
		/// </summary>
		/// <param name="args">the parameters to pass to the server when starting</param>
		/// <param name="settings">a <see cref="CWSServerSettings"/> object allowing to set the way the server operates</param>
		public async void Start(string[] args, CWSServerSettings settings)
		{
			Guid guid = Guid.NewGuid();
			if (default == settings)
			{
				CLog.ERROR("no settings provided to start WS server");
				return;// WSResultEnum.InvalidSettings;
			}
			else if (!settings.IsValid)
			{
				CLog.ERROR($"invalid settings [{settings}]");
				return;// WSResultEnum.InvalidSettings;
			}

			bool ok;

			// start specific processing
			try
			{
				ok = (default == settings.OnStart ? true : settings.OnStart.Invoke(args));
			}
			catch (Exception ex)
			{
				ok = false;
				CLog.EXCEPT(ex);
			}
			if (!ok)
			{
				CLog.TRACE($"WS server processing failed to start");
				return;// WSResultEnum.FailedToStart;
			}

			// arrived here the server processing has started

			// start the WS server
			var builder = WebApplication.CreateBuilder();
			builder.WebHost.UseUrls($"http://localhost:{settings.Port}");
			var app = builder.Build();
			app.UseWebSockets();
			app.Map(settings.WSName.IsNullOrEmpty() ? string.Empty : $"/{settings.WSName}", async context =>
			{
				// arrived here a client is connected to the WS server
				try
				{
					IPEndPoint endpoint = new IPEndPoint(context.Connection.RemoteIpAddress, context.Connection.RemotePort);
					string sendpoint = endpoint.ToString();
					bool isLocalHost = false;
					foreach (IPAddress k in CStream.Localhosts())
						if (isLocalHost = (isLocalHost || k.ToString() == endpoint.Address.ToString()))
							break;
					CLog.TRACE($"incoming connection from {sendpoint} ({(isLocalHost ? "local" : "distant")} host)");
					if (context.WebSockets.IsWebSocketRequest)
					{
						using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
						{
							WSActionEnum wsaction = settings.LoginRequired ? WSActionEnum.WaitingForLogin : WSActionEnum.Connected;
							byte[] ab = new byte[settings.BufferSize];
							string request = string.Empty;
							string reply = string.Empty;
							string ID = string.Empty;

							try
							{
								// signal a connection has been opened
								try
								{
									int count = Clients.Count;
									bool allowedIP = false;
									foreach (string k in AllowedIPs)
										if (allowedIP = (allowedIP && (IPAddress.TryParse(k, out IPAddress addr) && AllowedIPs.Contains(addr.ToString()))))
											break;
									bool ipCanConnect = (AllowNonLocalConnection) || (!AllowNonLocalConnection && isLocalHost);
									bool availableConnections = (0 == MaxNumberOfConcurrentConnections) || (count < MaxNumberOfConcurrentConnections);
									bool canOpen = allowedIP && ipCanConnect && availableConnections;

									if (ok = (canOpen && (default == settings.OnOpen ? true : settings.OnOpen(endpoint, out ID))))
									{
										CLog.DEBUG($"connection from {sendpoint} has been accepted{(ID.IsNullOrEmpty() ? string.Empty : $" (ID: {ID})")}");
										CWSClient client = default;

										// save connected client details
										try
										{
											client = new CWSClient(webSocket, ID);
											_clients.TryAdd(sendpoint, client);
										}
										catch (Exception ex)
										{
											client = default;
											CLog.EXCEPT(ex);
										}

										try
										{
											// receive a message and append it to the current buffer
											Func<WebSocketReceiveResult, bool> PopulateRequestBuffer = (WebSocketReceiveResult _res_) =>
											{
												try
												{
													if (WebSocketMessageType.Text == _res_.MessageType)
													{
														request += Encoding.UTF8.GetString(ab, 0, _res_.Count);
														return true;
													}
												}
												catch (Exception _ex_)
												{
													CLog.EXCEPT(_ex_);
												}
												// close or any other reason
												return false;
											};

											// reset all receiving buffers
											Func<bool> ResetRequest = () =>
											{
												request = string.Empty;
												return true;
											};

											// while the WS is operational process messages
											while (WebSocketState.Open == webSocket.State && !Source.Token.IsCancellationRequested)
											{
												switch (wsaction)
												{
													case WSActionEnum.WaitingForLogin:
														{
															try
															{
																CLog.DEBUG($"waiting security details from {sendpoint}");
																var res = await webSocket.ReceiveAsync(new ArraySegment<byte>(ab), Source.Token);
																if (PopulateRequestBuffer(res) && res.EndOfMessage)
																{
																	CLog.DEBUG($"security details [{request}]");
																	try
																	{
																		// test login credentials ...
																		if (settings.OnLogin(endpoint, request, out reply))
																		{
																			CLog.DEBUG($"connexion from {sendpoint} has been granted{(request.IsNullOrEmpty() ? string.Empty : $" with {reply}")}");
																			//WSClients[sendpoint].ID = loginRequest._extendedData.ToString().ToSHA256();
																			wsaction = WSActionEnum.LoginOK;
																		}
																		else
																		{
																			CLog.DEBUG($"connexion from {sendpoint} has been declined");
																			wsaction = WSActionEnum.LoginKO;
																		}
																	}
																	catch (Exception ex)
																	{
																		CLog.EXCEPT(ex);
																	}
																	//request.Reset();
																	request = string.Empty;
																}
															}
															catch (Exception ex)
															{
																request = string.Empty;
																CLog.EXCEPT(ex);
															}
														}
														break;

													case WSActionEnum.LoginOK:
													case WSActionEnum.LoginKO:
														{
															CLogger.TRACE($"sending login result {wsaction} to {sendpoint}");
															try
															{
																await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(reply)), WebSocketMessageType.Text, true, Source.Token);
															}
															catch (Exception ex)
															{
																CLog.EXCEPT(ex);
															}
															switch (wsaction)
															{
																case WSActionEnum.LoginOK:
																	wsaction = WSActionEnum.WaitingCommand;
																	break;
																case WSActionEnum.LoginKO:
																	wsaction = WSActionEnum.WaitingForLogin;
																	break;
															}
															reply = string.Empty;
														}
														break;

													case WSActionEnum.WaitingCommand:
														{
															try
															{
																CLog.DEBUG($"waiting command");
																var res = await webSocket.ReceiveAsync(new ArraySegment<byte>(ab), Source.Token);
																if (PopulateRequestBuffer(res) && res.EndOfMessage)
																{
																	CLogger.TRACE($"{sendpoint}] received notification [{reply}]");
																	try
																	{
																		if (settings.OnCommand(endpoint, request, out reply))
																		{
																			CLogger.TRACE($"[{sendpoint}] received notification [{reply}]");
																		}
																		else
																		{
																			CLogger.TRACE($"received reply [{reply}]");
																		}
																	}
																	catch (Exception ex)
																	{
																		CLog.EXCEPT(ex);
																	}
																	request = string.Empty;
																}
															}
															catch (Exception ex)
															{
																request = string.Empty;
																CLog.EXCEPT(ex);
															}
														}
														break;

													case WSActionEnum.ReceivedReply:
													case WSActionEnum.ReceivedNotification:
														break;
												}
											}
										}
										catch (Exception ex)
										{
											ok = false;
											CLog.EXCEPT(ex);
										}
										finally
										{
											settings?.OnClose(sendpoint, client, ID);
											try
											{
												_clients.TryRemove(sendpoint, out CWSClient c);
											}
											catch (Exception ex)
											{
												CLog.EXCEPT(ex);
											}
											CLog.TRACE($"connection from {sendpoint} has been closed");
										}
									}
									else if (!ipCanConnect || !allowedIP)
									{
										CLog.ERROR($"connection from {sendpoint} has been denied, IP not allowed");
									}
									else if (!availableConnections)
									{
										CLog.ERROR($"connection from {sendpoint} has been denied, no more clients can connect ({count} already connected)");
									}
									else
									{
										CLog.DEBUG($"connection from {sendpoint} has been denied");
									}
								}
								catch (Exception ex)
								{
									ok = false;
									CLog.EXCEPT(ex);
								}

							}
							catch (Exception ex)
							{
								CLog.EXCEPT(ex);
							}

						}
					}
					else
					{
						context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
						CLog.WARNING($"reception of an HTTP request, connection from {sendpoint} is being closed with status {context.Response.StatusCode}");
					}

				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex);
				}

			});

			settings.StartedEvt.Set();
			await app.RunAsync();
			settings.EndedEvt.Set();
		}
		/// <summary>
		/// Send a message to client connected to the WS server.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <param name="message"></param>
		/// <param name="evt"></param>
		public async Task<bool> SendMessage(string endpoint, string message, ManualResetEvent evt)
		{
			bool ok = false;
			if (message.IsNullOrEmpty()) return ok;
			if (!Clients.TryGetValue(endpoint, out CWSClient client))
			{
				CLog.ERROR($"client {endpoint} can't be located, message not sent");
				return ok;
			}

			// arrived the message can be sent
			try
			{
				byte[] ab = Encoding.UTF8.GetBytes(message);
				ArraySegment<byte> arb = new ArraySegment<byte>(ab);
				await client.WS.SendAsync(arb, WebSocketMessageType.Text, true, Source.Token);
				CLog.DEBUG($"message {message} sent to {endpoint}");
				// the message has been sent
				ok = true;
			}
			catch (Exception ex)
			{
				CLogger.EXCEPT(ex);
			}
			evt.Set();
			return ok;
		}
		#endregion
	}
}