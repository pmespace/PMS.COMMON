



using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System;
using COMMON;
using System.Threading.Tasks;

namespace COMMON.WSServer.Client
{
	public class CWSClient : CWS
	{
		#region constructor
		#endregion

		#region properties
		public WSStatusEnum CurrentStatus { get; private set; }

		WSStatusEnum Status;
		ClientWebSocket WS;
		CancellationTokenSource Source = null;
		#endregion

		#region public methods
		#endregion

		#region internal private methods
		#endregion

		#region enums
		enum WSAction
		{
			Error,
			Connect,
			SendLoginRequest,
			ReceiveLoginResponse,
			ReceiveMessage,
			SendAnswer,
			SendNotification,
		}
		#endregion

		async void Start(CWSClientSettings settings)
		{
			bool ok;

			//Func<WSStatusEnum, WSStatusEnum, int> SetCurrentStatus = (WSStatusEnum _sts_, WSStatusEnum _raz_) => (CurrentStatus & (int)_raz_) | (int)(Status = _sts_);
			Func<WSStatusEnum, WSStatusEnum> SetCurrentStatus = (WSStatusEnum _sts_) => CurrentStatus = _sts_;

			try
			{
				Task task;
				// the listener needs to connect to the server
				WS = new ClientWebSocket();
				Source = new CancellationTokenSource();
				// counter of connection attempts
				Exception except;
				bool closing = false;
				bool connected = false;
				string statusError;
				WSAction action = settings.LoginRequired ? WSAction.SendLoginRequest : WSAction.SendAnswer;
				CStreamClientIO streamIO = null;
				byte[] ab = new byte[settings.BufferSize];

				// set staus
				SetCurrentStatus(WSStatusEnum.BeforeConnect);
				if (!(ok = (null == settings.OnStatus ? true : settings.OnStatus(Status))))
				{
					CLog.ERROR($"{Status} to {settings.URI} declined by client");
				}
				else
				{
					// connect to the server
					CLog.INFOR($"connecting to {settings.URI}");
					task = WS.ConnectAsync(new Uri(settings.URI), Source.Token);
					await task;

					if (task.IsCompletedSuccessfully)
					{
						SetCurrentStatus(WSStatusEnum.AfterConnectSuccess);
						if (!(ok = (null == settings.OnStatus ? true : settings.OnStatus(Status))))
						{
							CLog.ERROR($"{Status} to {settings.URI} declined by client");
						}
						else
						{
							try
							{
								#region comment
								string buffer = string.Empty;

								// start reading the socket
								while (WebSocketState.Open == WS.State)
								{
									connected = true;

									// run requested action
									switch (action)
									{
										case WSAction.SendLoginRequest:
											SetCurrentStatus(WSStatusEnum.BeforeLogin);
											if (!(ok = (null == settings.OnStatus ? true : settings.OnStatus(Status))))
											{
												CLog.ERROR($"login request to {settings.URI} declined by client");
												Source.Cancel();
												//throw new WSException();												}
											}
											else
											{
												// request login information
												string credentials = string.Empty;
												if (ok = (null == settings.OnLogin ? true : settings.OnLogin(out credentials)))
												{
													// send login information
													if (ok = await SendAsync(WS, credentials, $"sending login request to server at {settings.URI}", Source))
													{
														buffer = string.Empty;
														action = WSAction.ReceiveLoginResponse;
													}
													else
													{
														CLog.ERROR($"failed to send login credentials to {settings.URI}");
														Source.Cancel();
														//throw new WSException();
													}
												}
												else
												{
													CLog.ERROR($"failed to get login credentials to connect to {settings.URI}, disconnecting from server");
													Source.Cancel();
													//throw new WSException();												}
												}
											}
											break;

										case WSAction.ReceiveLoginResponse:
										case WSAction.ReceiveMessage:
											// receive message
											var result = await WS.ReceiveAsync(new ArraySegment<byte>(ab), Source.Token);
											if (PopulateReceivedBuffer(result, ab, ref buffer, Source) && result.EndOfMessage)
											{
												switch (action)
												{
													case WSAction.ReceiveLoginResponse:
														// convert to a login response
														WSLoginResult loginResult;
														if (null != (loginResult = CJson<WSLoginResult>.Deserialize(buffer)))
														{
															// connected and a login response has been received, we can reset the attempt counter
															if (loginResult.Granted)
															{
																CLog.Add(new CLogMsgs()
																	{
																		new CLogMsg($"login has been granted", TLog.INFOR),
																		new CLogMsg($"data: [{buffer}]", TLog.DEBUG),
																	});

																SetCurrentStatus(WSStatusEnum.AfterLoginSuccess);
																if (!(ok = null == settings.OnStatus ? true : settings.OnStatus(Status)))
																{
																	CLog.ERROR($"login to {settings.URI} declined by client");
																	throw new WSException();
																}
																action = WSAction.ReceiveMessage;
																buffer = string.Empty;
															}
															else
															{
																CLog.Add(new CLogMsgs()
																	{
																		new CLogMsg($"login denied", TLog.ERROR),
																		new CLogMsg($"data: [{buffer}]", TLog.DEBUG),
																	});

																SetCurrentStatus(WSStatusEnum.AfterLoginFailure);
																settings?.OnStatus(Status);
																throw new WSException();
															}
														}
														else
														{
															CLog.Add(new CLogMsgs()
																{
																	new CLogMsg( $"an invalid login response message has been received", TLog.ERROR),
																	new CLogMsg($"data: [{buffer}]", TLog.DEBUG),
																});

															SetCurrentStatus(WSStatusEnum.AfterLoginError);
															if (!(ok = (null == settings.OnStatus ? true : settings.OnStatus(Status))))
															{
																statusError = $"login request to {settings.URI} can't go any further ({Status}), declined by process";
																throw new WSException(statusError);
															}
															settings.OnStatus?.Invoke(WSStatusEnum.DisconnectingFromWSServer);
															throw new WSException();
														}
														break;

													case WSAction.ReceiveMessage:
														if (!buffer.IsNullOrEmpty())
														{
															CLog.Add(new CLogMsgs()
															{
																new CLogMsg($"received request", TLog.INFOR),
																new CLogMsg($"data: [{buffer}]", TLog.DEBUG),
															});
															listenerRequest.Secured = true;
															string norder = CJson<CListenerRequestWS>.Serialize(listenerRequest);
															if (!norder.IsNullOrEmpty()) order = norder;

															// a request has been received, post it to the listener server part
															if (CStream.Send(streamIO, order))
															{
																CLog.INFORMATION($"request sent to the listener");
															}
															else
															{
																CLog.ERROR($"request failed to be sent to the listener");
															}
														}
														else
														{
															CLog.Add(new CLogMsgs()
															{
																new CLogMsg($"invalid request received from the server, still listening", TLog.ERROR),
																new CLogMsg($"data: [{buffer}]", TLog.DEBUG),
															});
															CLog.ERROR($"an empty request has been received, keeping listening");
															CListenerReply reply = new CListenerReply() { RequestAsString = order };
															reply.Status = ReplyStatus.invalidRequest;
															await WS.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(CJson<CListenerReply>.Serialize(reply))), WebSocketMessageType.Text, true, Source.Token);
														}
														break;
												}
												order = string.Empty;
											}
											break;
									}

									// if a cancel event has been received...
									if (Source.Token.IsCancellationRequested)
									{
										CLog.TRACE("received a cancellation event, disconnecting");
									}
								}
								#endregion
							}
							catch (Exception)
							{
								throw;
							}
						}

						// disconnecting from server
						CLog.INFOR($"disconnecting from {settings.URI} ({Status})");
						SetCurrentStatus(WSStatusEnum.BeforeDisconnect);
						settings?.OnStatus(Status);
						await WS.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
						Source.Cancel();
					}
					else
					{
						SetCurrentStatus(WSStatusEnum.AfterConnectFailure);
						CLog.ERROR($"connection to {settings.URI} failed ({task.Status})");
						settings?.OnStatus(Status);
					}
				}
			}
			catch (WSException ex) { }
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
		}
	}
}