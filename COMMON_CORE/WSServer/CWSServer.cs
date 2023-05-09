using System;
using System.Net;
using System.Web;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;

namespace COMMON.WSServer
{
	/// <summary>
	/// A Web Socket (WS) server ready to use,
	/// only <see cref="WebSocketMessageType.Text"/> messages are supported,
	/// </summary>
	public class CWSServer : CWS
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
		/// All currently connected clients, described by a <see cref="CWSConnectedClient"/> object, the key is connected client IP:port address
		/// </summary>
		public CWSReadOnlyClients Clients { get; private set; }
		CWSClients _clients;
		#endregion

		#region private properties
		CancellationTokenSource Source { get; set; }
		object mylock = new object();
		#endregion

		#region classes
		enum WSAction
		{
			Error = -1,
			None,
			WaitingForLogin,
			LoginOK,
			LoginKO,
			Connected,
			WaitingRequest,
			ReplyAvailable,
		}
		#endregion

		#region methods
		/// <summary>
		/// Starts the WS server itself
		/// </summary>
		/// <param name="args">the parameters to pass to the server when starting</param>
		/// <param name="settings">a <see cref="CWSServerSettings"/> object allowing to set the way the server operates</param>
		public async void Start(string[] args, CWSServerSettings settings)
		{
			if (default == settings)
			{
				CLog.ERROR("no settings provided to start WS server");
				return;
			}
			else if (!settings.IsValid)
			{
				CLog.ERROR($"invalid settings [{settings}]");
				return;
			}

			bool ok;
			bool restart = true;

			while (restart)
			{
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
					CLog.ERROR($"WS server processing failed to start");
					return;
				}

				// arrived here the server processing has started

				// start the WS server
				var builder = WebApplication.CreateBuilder();
				builder.WebHost.UseUrls($"http://localhost:{settings.Port}");
				var app = builder.Build();

				app.UseAuthentication();
				app.UseAuthorization();

				app.UseWebSockets();

				//app.UseEndpoints();

				#region use
				app.Use(async (context, next) =>
				{
					bool ok;
					bool shutdown = false;

					if ((settings.WSName.IsNullOrEmpty() ? string.Empty : $"/{settings.WSName}") == context.Request.Path)
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
							CLog.INFOR($"incoming connection from {sendpoint} ({(isLocalHost ? "local" : "distant")} host)");
							if (context.WebSockets.IsWebSocketRequest)
							{
								using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
								{
									WSAction wsaction = settings.LoginRequired ? WSAction.WaitingForLogin : WSAction.Connected;
									byte[] ab = new byte[settings.BufferSize];
									object ID = null;

									string request = string.Empty;
									string reply = string.Empty;
									int nextAction = (int)WSNextActionEnum.None;

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
												CLog.INFOR($"connection from {sendpoint} has been accepted{(null == ID ? string.Empty : $" (ID: {ID})")}");
												CWSConnectedClient client = default;

												// save connected client details
												try
												{
													client = new CWSConnectedClient(webSocket, ID);
													_clients.TryAdd(sendpoint, client);
												}
												catch (Exception ex)
												{
													client = default;
													CLog.EXCEPT(ex);
												}

												try
												{
													// determines whether an action is requested or not
													Func<int, WSNextActionEnum, bool> NextAction = (int _flags_, WSNextActionEnum _na_) => (int)_na_ == ((int)_na_ & _flags_);

													// verifies immediate actions to perform and take appropriate measures
													Func<int, bool> ImmediateNextAction = (int _flags_) =>
													{
														if (NextAction(_flags_, WSNextActionEnum.ShutdownImmediately))
														{
															CLog.INFOR($"received order to shutdown immediately");
															lock (mylock) { shutdown = true; }
															Source.Cancel();
															return true;
														}
														else if (NextAction(_flags_, WSNextActionEnum.DisconnectImmediately))
														{
															CLog.INFOR($"received order to disconnect {sendpoint} immediately");
															Source.Cancel();
															return true;
														}
														return false;
													};

													// while the WS is operational process messages
													while (WebSocketState.Open == webSocket.State && !Source.Token.IsCancellationRequested)
													{
														switch (wsaction)
														{
															case WSAction.WaitingForLogin:
																{
																	try
																	{
																		CLog.INFOR($"waiting security details from {sendpoint}");
																		var res = await webSocket.ReceiveAsync(new ArraySegment<byte>(ab), Source.Token);
																		if (PopulateReceivedBuffer(res, ab, ref request, Source) && res.EndOfMessage)
																		{
																			CLog.Add(new CLogMsgs()
																			{
																			new CLogMsg($"received security details", TLog.INFOR),
																			new CLogMsg($"data: [{request}]", TLog.DEBUG),
																			});
																			try
																			{
																				// test login credentials ...
																				ok = settings.OnLogin(endpoint, ID, request, out reply, out nextAction);
																				if (default == reply) reply = string.Empty;
																				if (ImmediateNextAction(nextAction)) { }
																				else if (ok)
																				{
																					CLog.Add(new CLogMsgs()
																				{
																				new CLogMsg($"connexion from {sendpoint} has been granted", TLog.INFOR),
																				new CLogMsg($"data: [{reply}]", TLog.DEBUG),
																				});
																					wsaction = WSAction.LoginOK;
																				}
																				else
																				{
																					CLog.Add(new CLogMsgs()
																				{
																				new CLogMsg($"connexion from {sendpoint} has been declined", TLog.INFOR),
																				new CLogMsg($"data: [{reply}]", TLog.DEBUG),
																				});
																					wsaction = WSAction.LoginKO;
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

															case WSAction.LoginOK:
															case WSAction.LoginKO:
																{
																	CLog.INFOR($"sending login result {wsaction} to {sendpoint}");
																	try
																	{
																		await SendMessage(sendpoint, reply);
																	}
																	catch (Exception ex)
																	{
																		CLog.EXCEPT(ex);
																	}
																	switch (wsaction)
																	{
																		case WSAction.LoginOK:
																			wsaction = WSAction.WaitingRequest;
																			break;
																		case WSAction.LoginKO:
																			wsaction = WSAction.WaitingForLogin;
																			break;
																	}
																	reply = default;
																}
																break;

															case WSAction.WaitingRequest:
																{
																	try
																	{
																		CLog.DEBUG($"waiting command");
																		var res = await webSocket.ReceiveAsync(new ArraySegment<byte>(ab), Source.Token);
																		if (PopulateReceivedBuffer(res, ab, ref request, Source) && res.EndOfMessage)
																		{
																			CLog.Add(new CLogMsgs()
																		{
																		new CLogMsg($"received request from {sendpoint} [request of {request.Length} bytes]", TLog.INFOR),
																		new CLogMsg($"data: [{request}]", TLog.DEBUG),
																		});
																			try
																			{
																				ok = settings.OnRequest(endpoint, ID, request, out reply, out nextAction);
																				if (default == reply) reply = string.Empty;
																				if (ImmediateNextAction(nextAction)) { }
																				else if (ok)
																				{
																					CLog.Add(new CLogMsgs()
																				{
																				new CLogMsg($"request has been processed successfully [reply of {reply.Length} bytes]", TLog.INFOR),
																				new CLogMsg($"data: [{reply}]", TLog.DEBUG),
																				});
																					wsaction = WSAction.ReplyAvailable;
																				}
																				else
																				{
																					CLog.Add(new CLogMsgs()
																				{
																				new CLogMsg($"request has been processed unsuccessfully [reply of {reply.Length} bytes]", TLog.INFOR),
																				new CLogMsg($"data: [{reply}]", TLog.DEBUG),
																				});
																					wsaction = WSAction.ReplyAvailable;
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

															case WSAction.ReplyAvailable:
																{
																	CLog.INFOR($"sending reply to {sendpoint}");
																	try
																	{
																		await SendMessage(sendpoint, reply);
																	}
																	catch (Exception ex)
																	{
																		CLog.EXCEPT(ex);
																	}
																	wsaction = WSAction.WaitingRequest;
																	reply = default;
																}
																break;
														}

														// test next actions
														if (NextAction(nextAction, WSNextActionEnum.Shutdown))
														{
															CLog.INFOR($"received order to shutdown");
															lock (mylock) { shutdown = true; }
															Source.Cancel();
														}
														else if (NextAction(nextAction, WSNextActionEnum.DisconnectClient))
														{
															CLog.INFOR($"received order to disconnect {sendpoint}");
															Source.Cancel();
														}
														nextAction = (int)WSNextActionEnum.None;
													}
												}
												catch (Exception ex)
												{
													ok = false;
													CLog.EXCEPT(ex);
												}
												finally
												{
													settings?.OnClose(sendpoint, ID, client);
													try
													{
														_clients.TryRemove(sendpoint, out CWSConnectedClient c);
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
								context.Response.StatusCode = StatusCodes.Status400BadRequest;
								CLog.WARNING($"reception of an HTTP request, connection from {sendpoint} is being closed with status {context.Response.StatusCode}");
							}
						}
						catch (Exception ex)
						{
							CLog.EXCEPT(ex);
						}
						// if shutdown is requested
						if (shutdown) app.Lifetime.StopApplication();
					}
					else
					{
						await next(context);
					}
				});
				#endregion

				await app.RunAsync();
			}

			// stop specific processing
			try
			{
				settings.OnStop();
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			settings.EndedEvt.Set();
		}
		/// <summary>
		/// Sends a message to client connected to the WS server.
		/// </summary>
		/// <param name="endpoint">the remote endpoint of the client</param>
		/// <param name="message">the message to send</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> object returning a bool, true if sending was successful, false otherwise
		/// </returns>
		public async Task<bool> SendMessage(string endpoint, string message)
		{
			if (!Clients.TryGetValue(endpoint, out CWSConnectedClient client))
			{
				CLog.ERROR($"client {endpoint} can't be located, message not sent");
				return false;
			}
			return await SendMessage(client.WS, endpoint, message, Source);
		}
		#endregion
	}
}