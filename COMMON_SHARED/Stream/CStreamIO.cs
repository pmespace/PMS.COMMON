﻿using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Runtime.InteropServices;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System;
using Microsoft.Win32;
using System.Threading;
using COMMON;
using System.Net.NetworkInformation;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using COMMON.Properties;
using System.Threading.Tasks;

namespace COMMON
{
	[ComVisible(false)]
	public abstract class CStreamIO : CStreamSettings
	{
		#region constructors
		public CStreamIO(TcpClient tcp, CStreamSettings sb) : base(sb)
		{
			Tcp = tcp;
		}
		~CStreamIO()
		{
			if (default != Tcp)
			{
				Tcp.Close();
				Tcp = default;
			}
		}
		#endregion

		#region properties
		public TcpClient Tcp
		{
			get => _tcp;
			private set => _tcp = value;
		}
		private TcpClient _tcp = default;
		/// <summary>
		/// SSL stream if SSL security is required
		/// </summary>
		protected SslStream sslStream
		{
			get => _sslstream;
			set
			{
				_sslstream = value;
				if (default != sslStream)
					networkStream = default;
			}
		}
		private SslStream _sslstream = default;
		/// <summary>
		/// Standard stream if no security is required
		/// </summary>
		protected NetworkStream networkStream
		{
			get => _networkstream;
			set
			{
				_networkstream = value;
				if (default != networkStream)
					sslStream = default;
			}
		}
		private NetworkStream _networkstream = default;
		/// <summary>
		/// Indicates whether a StreamIO is connected or not
		/// </summary>
		public bool Connected
		{
			get
			{
				if (default != Tcp)
				{
					IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
					TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections().Where(x => x.LocalEndPoint.Equals(Tcp?.Client?.RemoteEndPoint) && x.RemoteEndPoint.Equals(Tcp.Client.LocalEndPoint)).ToArray();
					if (tcpConnections != default && tcpConnections.Length > 0)
					{
						TcpState stateOfConnection = tcpConnections.First().State;
						return (stateOfConnection == TcpState.Established);
					}
				}
				return false;
			}
		}
		#endregion

		#region constants
		public const byte ETX = 0x03;
		public const byte EOT = 0x04;
		#endregion

		#region methods
		public override string ToString()
		{
			if (default != Tcp)
				return Resources.CStreamIOToString.Format(new object[] { Tcp.Connected, Tcp?.Client?.RemoteEndPoint, Tcp.ReceiveBufferSize, Tcp.ReceiveTimeout });
			return default;
		}
		/// <summary>
		/// Writes to the adequate stream
		/// </summary>
		/// <param name="data">buffer to write</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// True if write operation has been made, false otherwise
		/// </returns>
		private bool Write(byte[] data, CancellationToken token = default)
		{
			if (data.IsNullOrEmpty()) return false;

			Stream stream = networkStream;
			if (default == stream) stream = sslStream;
			if (default == stream) return false;

			try
			{
				//if (CancellationToken.None == token)
				//{
				//	stream.Write(data, 0, data.Length);
				//	return true;
				//}
				//else
				//{
				//	var task = stream.WriteAsync(data, 0, data.Length, token);
				//	task.Wait();
				//	if (task.IsCompleted)
				//		return true;
				//}

				var task = stream.WriteAsync(data, 0, data.Length, token);
				task.Wait();
				if (task.IsCompleted)
					return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Flush the adequate stream
		/// </summary>
		/// <param name="token">cancellation token to use for async operation</param>
		private void Flush(CancellationToken token)
		{
			Stream stream = networkStream;
			if (default == stream) stream = sslStream;
			if (default == stream) return;

			try
			{
				//if (CancellationToken.None == token)
				//{
				//	stream.Flush();
				//}
				//else
				//{
				//	var task = stream.FlushAsync(token);
				//	task.Wait();
				//}

				var task = stream.FlushAsync(token);
				task.Wait();
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
		}
		/// <summary>
		/// Reads the adequate stream
		/// </summary>
		/// <param name="data">buffer to feed with read data</param>
		/// <param name="offset">offset at which to put data inside the buffer</param>
		/// <param name="ioexcept">the exception which stopped the read process</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// The number of bytes read, 0 if no bytes read
		/// </returns>
		private int Read(byte[] data, int offset, out bool ioexcept, CancellationToken token = default)
		{
			ioexcept = false;

			if (data.IsNullOrEmpty() || data.Length <= offset) return 0;
			int read = 0;

			Stream stream = networkStream;
			if (default == stream)
				stream = sslStream;
			if (default == stream)
				return 0;

			try
			{
				//if (CancellationToken.None == token)
				//{
				//	read = stream.Read(data, offset, data.Length - offset);
				//}
				//else
				//{
				//	var task = stream.ReadAsync(data, offset, data.Length - offset);
				//	task.Wait();
				//	if (task.IsCompleted)
				//		read = task.Result;
				//}

#if true
				var task = stream.ReadAsync(data, offset, data.Length - offset, token);
				task.Wait();
				if (task.IsCompleted)
					read = task.Result;
#elif _OLD2
				var task = stream.ReadAsync(data, offset, data.Length - offset, token);
				Task.WaitAny(task, Task.Delay(stream.ReadTimeout));
				if (task.IsCompleted)
					read = task.Result;
				else
				{
					stream.Close();
					read = task.Result;
				}
#else
				if (stream.CanTimeout)
					stream.ReadTimeout = 5000;// NO_TIMEOUT == ReceiveTimeout ? Timeout.Infinite : ReceiveTimeout;
				read = stream.Read(data, offset, data.Length - offset);
#endif
			}
			catch (Exception ex)
			{
				ioexcept = (ex is ObjectDisposedException || ex is IOException);
				CLog.EXCEPT(ex);
			}
			return read;
		}
		/// <summary>
		/// Sends a buffer to an outer entity.
		/// If requested with <see cref="CStreamBase.UseSizeHeader"/> a size header of length <see cref="CStreamBase.SizeHeader"/> is added at the beginning of the message.
		/// </summary>
		/// <param name="data">the message to send</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// True if the message has been sent, false otherwise
		/// </returns>
		internal bool Send(byte[] data, CancellationToken token = default)
		{
			if (data.IsNullOrEmpty())
			{
				CLog.INFOR(Resources.CStreamIOTryingToSendEmptyBuffer);
				return false;
			}
			// if requested, add the size header to the message to send
			int lengthSize = (UseSizeHeader ? SizeHeader : 0);
			int size = (int)data.Length + lengthSize;
			byte[] messageToSend = new byte[size];
			Buffer.BlockCopy(data, 0, messageToSend, (int)lengthSize, data.Length);
			if (UseSizeHeader)
			{
				//byte[] bs = CMisc.SetBytesFromIntegralTypeValue((int)data.Length ? TWOBYTES == SizeHeader ? , false);
				//Buffer.BlockCopy(bs, 0, messageToSend, 0, (int)lengthSize);

				byte[] bs = CMisc.SetBytesFromIntegralTypeValue((long)data.Length, false);
				Buffer.BlockCopy(bs, EIGHTBYTES - SizeHeader, messageToSend, 0, (int)lengthSize);
			}
			// arrived here the message is ready to be sent
			string s1 = Resources.CStreamIONotAddingHeader, s2 = Resources.CStreamIOAddingHeader.Format(lengthSize);
			CLog.Add(new CLogMsgs()
			{
				new CLogMsg(Resources.CStreamIOSendingMessage.Format(new object[] {messageToSend.Length, (0 == lengthSize ? s1 : s2), Tcp?.Client?.RemoteEndPoint}), TLog.INFOR),
				new CLogMsg(Resources.CStreamIOData.Format(CMisc.AsHexString(messageToSend, true)), TLog.DEBUG),
			});
			if (Write(messageToSend, token))
			{
				Flush(token);
				return true;
			}
			CLog.INFOR(Resources.CStreamIOFailedSendingMessage.Format(new object[] { messageToSend.Length, (0 == lengthSize ? s1 : s2), Tcp?.Client?.RemoteEndPoint }));
			return false;
		}
		/// <summary>
		/// Sends a buffer to an outer entity.
		/// If requested with <see cref="CStreamBase.UseSizeHeader"/> a size header of length <see cref="CStreamBase.SizeHeader"/> is added at the beginning of the message.
		/// </summary>
		/// <param name="data">the message to send as a string</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// true if the message has been sent, false otherwise
		/// </returns>
		internal bool Send(string data, CancellationToken token = default)
		{
			//byte[] bdata = (default != data ? Encoding.UTF8.GetBytes(data) : default);
			byte[] bdata = (default != data ? Encoding.GetBytes(data) : default);
			return Send(bdata, token);
		}
		/// <summary>
		/// Sends a buffer to an outer entity.
		/// A size header is never added to the message itself, size of buffer to send is determined by presence of the <paramref name="EOT"/> finishing the message.
		/// </summary>
		/// <param name="data">the message to send</param>
		/// <param name="EOT">the end of transmission character to use to terminate the mesage (CR+LF by default)</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// true if the message has been sent, false otherwise
		/// </returns>
		internal bool SendLine(string data, CancellationToken token = default, string EOT = Chars.CRLF)
		{
			if (EOT.IsNullOrEmpty()) EOT = Chars.CRLF;

			// verify the EOL is there, add it if necessary
			if (!data.IsNullOrEmpty())
			{
				// replace intermediate EOL
				data = data.Replace(EOT, "");
				// add the EOL at the end of string
				data += EOT;
			}
			CLog.Add(new CLogMsgs()
			{
				new CLogMsg(Resources.CStreamIOSendingTextMessage.Format(new object[] {data.Length, Tcp?.Client?.RemoteEndPoint}), TLog.INFOR),
				new CLogMsg(Resources.CStreamIOData.Format(data), TLog.DEBUG),
			});
			//byte[] bdata = (default != data ? Encoding.UTF8.GetBytes(data) : default);
			byte[] bdata = (default != data ? Encoding.GetBytes(data) : default);
			bool ok = Send(bdata, token);
			if (!ok)
				CLog.INFOR(Resources.CStreamIOFailedSendingTextMessage.Format(new object[] { data.Length, Tcp?.Client?.RemoteEndPoint }));
			return ok;
		}
		/// <summary>
		/// Receives a buffer of a specific size from the server.
		/// The function will allocate the buffer of the specified size to receive data.
		/// The function will not return until the buffer has been fully received or an error has occurred (timeout,...).
		/// </summary>
		/// <param name="bufferSize">size of the buffer to receive</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// the received buffer if successful (eventually with a 0 length if no data has been received), null otherwise
		/// </returns>
		byte[] ReceiveSizedBuffer(int bufferSize, CancellationToken token = default)
		{
			try
			{
				// allocate buffer to receive
				if (0 == bufferSize)
					throw new ArgumentException(Resources.CStreamIOZeroLengthBufferGiven);

				byte[] buffer = new byte[bufferSize];
				int bytesRead = 0;
				bool doContinue;
				CLog.DEBUG(Resources.CStreamIOReceivingFixedSizeBuffer.Format(new object[] { buffer.Length, Tcp?.Client?.RemoteEndPoint }));

				do
				{
					// read stream for the specified buffer
					int nbBytes = Read(buffer, bytesRead, out bool ioexcept, token);
					if (doContinue = (0 != nbBytes))
					{
						bytesRead += nbBytes;
						// we continue until we've filled up the buffer
						doContinue = bytesRead < bufferSize;
					}
					else if (ioexcept)
					{
						CLog.INFORMATION(Resources.CStreamIOClientDisconnected.Format(Tcp?.Client?.RemoteEndPoint));
					}
				}
				while (doContinue);

				// create a buffer of the real number of bytes received (which can't be higher than the expected number of bytes)
				byte[] bufferReceived = new byte[bytesRead];
				Buffer.BlockCopy(buffer, 0, bufferReceived, 0, bytesRead);
				// log a message only if not closing the stream
				if (0 == bytesRead)
				{
					CLog.INFOR(Resources.CStreamIOReceivedUnexpectedEmptyBuffer.Format(Tcp?.Client?.RemoteEndPoint));

					//if (!ioexcept)
					//	CLog.ERROR($"unexpectedly received no data");
					//else
					//	CLog.INFORMATION($"client has been disconnected");
				}

				else if (bytesRead != bufferSize)
				{
					CLog.Add(new CLogMsgs()
					{
						new CLogMsg($"{Resources.CStreamIOReceivedMessage.Format(new object[] {bytesRead, Tcp?.Client?.RemoteEndPoint})} {Resources.CStreamIOExpectingBytes.Format(bufferSize)}", TLog.INFOR),
						new CLogMsg(Resources.CStreamIOData.Format(CMisc.AsHexString(bufferReceived, true)), TLog.DEBUG),
					});
				}

				else
				{
					CLog.Add(new CLogMsgs()
					{
						new CLogMsg(Resources.CStreamIOReceivedMessage.Format(new object[] {bytesRead, Tcp?.Client?.RemoteEndPoint}), TLog.DEBUG),
						new CLogMsg(Resources.CStreamIOData.Format(CMisc.AsHexString(bufferReceived, true)),TLog.DEBUG),
					});
				}
				return bufferReceived;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Receives a buffer of any size, reallocating memory to fit the buffer.
		/// The function will constantly reallocate the buffer to receive data.
		/// The function will not return until the buffer has been fully received (the stream is empty) or <paramref name="EOT"/> has been found in the received buffer indicating the end of the message.
		/// </summary>
		/// <param name="EOT">a string which if found marks the end of transmission</param>
		/// <param name="bufferToAllocate">size of buffer to allocate to read the incoming data (default is 1 Kb)</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// the received buffer if successful (eventually with a 0 length if no data has been received), null otherwise
		/// </returns>
		byte[] ReceiveNonSizedBuffer(string EOT, CancellationToken token = default, int bufferToAllocate = CStreamSettings.ONEKB)
		{
			try
			{
				if (string.IsNullOrEmpty(EOT)) EOT = Chars.CRLF;
				// allocate buffer to receive
				byte[] buffer = new byte[bufferToAllocate];
				int bytesRead = 0;
				bool doContinue;
				CLog.DEBUG(Resources.CStreamIOWaitingMessage.Format(Tcp?.Client?.RemoteEndPoint));

				bool ioexcept;
				do
				{
					// read stream for the specified buffer
					int nbBytes = Read(buffer, bytesRead, out ioexcept, token);
					if (doContinue = (0 != nbBytes))
					{
						bytesRead += nbBytes;
						// allocate more memory if the buffer is full
						if (bytesRead == buffer.Length)
						{
							byte[] newbuffer = new byte[bytesRead + bufferToAllocate];
							Buffer.BlockCopy(buffer, 0, newbuffer, 0, bytesRead);
							buffer = newbuffer;
						}
						// if the EOT is found stop reading
						if (!string.IsNullOrEmpty(EOT))
							//doContinue = !Encoding.UTF8.GetString(buffer).Contains(EOT);
							doContinue = !Encoding.GetString(buffer).Contains(EOT);
					}
					else if (ioexcept)
					{
						CLog.INFORMATION(Resources.CStreamIOClientDisconnected.Format(Tcp?.Client?.RemoteEndPoint));
					}
				}
				while (doContinue);
				// create a buffer of the real number of bytes received (which can't be higher than the expected number of bytes)
				byte[] bufferReceived = new byte[bytesRead];
				Buffer.BlockCopy(buffer, 0, bufferReceived, 0, bytesRead);
				CLog.Add(new CLogMsgs()
				{
					new CLogMsg(Resources.CStreamIOReceivedMessage.Format(new object[] {bytesRead, Tcp?.Client?.RemoteEndPoint}), TLog.INFOR),
					new CLogMsg(Resources.CStreamIOData.Format(CMisc.AsHexString(bufferReceived, true)),TLog.DEBUG),
				});
				return bufferReceived;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Receives a buffer of an unknown size from the server.
		/// The buffer MUST begin with a size header of <see cref="CStreamBase.SizeHeader"/> bytes
		/// The function will not return until the buffer has been fully received or an error has occurred (timeout,...)
		/// The returned buffer is ALWAYS stripped of the size header (whose value is returned in <paramref name="announcedSize"/>).
		/// </summary>
		/// <param name="announcedSize">size of the buffer as declared by the caller , therefore expected by the receiver.
		/// If that size differs from the size of the actually received buffer then an error has occurred</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// the  received buffer stripped of the size header (whose value is indicated in announcedSize) if successful, null otherwise
		/// </returns>
		internal byte[] Receive(out int announcedSize, CancellationToken token = default)
		{
			announcedSize = 0;
			try
			{
				// determine whether the header must be used or not and the size arrived here, either an automatic header is used or the protocol contains one, get the size of the headerto receive 
				byte[] bufferSize = ReceiveSizedBuffer(SizeHeader, token);
				if (!bufferSize.IsNullOrEmpty() && SizeHeader == bufferSize.Length)
				{
					// get the size of the buffer to read and start reading it
					if (0 != (announcedSize = (int)CMisc.GetIntegralTypeValueFromBytes(bufferSize, 0, SizeHeader)))
					{
						CLog.DEBUG(Resources.CStreamIOReceiveAnnounceOf.Format(new object[] { announcedSize, Tcp?.Client?.RemoteEndPoint }));
						byte[] buffer = ReceiveSizedBuffer(announcedSize, token);
						if (!buffer.IsNullOrEmpty() && announcedSize == buffer.Length)
						{
							return buffer;
						}
					}
					CLog.INFOR(Resources.CStreamIOReceiveAnnounceOfZero.Format(Tcp?.Client?.RemoteEndPoint));
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Receives a buffer of a known size from the server.
		/// The buffer does not need to begin with a size header as reading will be made on a fixed size.
		/// The function will not return until the buffer has been fully received or an error has occurred (timeout,...).
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <param name="size">size of the buffer to receive.</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// the received buffer if successful (eventually with a 0 length if no data has been received), <see cref="ArgumentException"/> or any other exception otherwise
		/// </returns>
		internal byte[] Receive(int size, CancellationToken token = default)
		{
			return ReceiveSizedBuffer(size, token);
		}
		/// <summary>
		/// Receive a string of an unknown size from the server.
		/// The buffer MUST begin with a size header of <see cref="CStreamBase.SizeHeader"/> bytes
		/// The function will not return until the buffer has been fully received or an error has occurred (timeout,...)
		/// The returned buffer is ALWAYS stripped of the size header.
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <returns>the  received buffer as a string if no error occurred, an empty string otherwise</returns>
		internal string Receive(CancellationToken token)
		{
			// receive the buffer
			byte[] buffer = Receive(out int size, token);
			return (!buffer.IsNullOrEmpty() && size == buffer.Length ? Encoding.GetString(buffer) : default);
		}
		/// <summary>
		/// Receive a string of an unknown size from the server.
		/// The string does not need to begin with a size header as reading will be made until a specific string is found.
		/// The string MUST however finish (or at least contain) a <paramref name="EOT"/> string marking the end of message.
		/// The function will not return until the string has been fully received or an error occurred (timeout).
		/// The returned string NEVER contains the size header.
		/// </summary>
		/// <param name="EOT">a string which if found marks the end of transmission</param>
		/// <param name="token">cancellation token to use for async operation</param>
		/// <returns>
		/// the  received buffer as a string if successful, null otherwise
		/// </returns>
		internal string ReceiveLine(CancellationToken token, string EOT = Chars.CRLF)
		{
			try
			{
				// receive the buffer
				byte[] buffer = ReceiveNonSizedBuffer(EOT, token);
				//string s = (default != buffer ? Encoding.UTF8.GetString(buffer) : default);
				string s = (default != buffer ? Encoding.GetString(buffer) : default);
				// remove EOT if necessary
				if (!string.IsNullOrEmpty(s))
					s = s.Replace(EOT, "");
				CLog.DEBUG(Resources.CStreamIOReceivedTextMessage.Format(new object[] { s, Tcp?.Client?.RemoteEndPoint }));
				return s;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Close the adequate Stream
		/// </summary>
		internal void Close()
		{
			Stream stream = networkStream;
			if (default == stream) stream = sslStream;
			if (default == stream) return;

			try
			{
				stream.Close();
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			Tcp = default;
		}
		#endregion
	}

	[ComVisible(true)]
	[Guid("8032AC79-8819-4FD3-A4E2-A97D24D318FC")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IStreamClientIO
	{
		#region CStreamBase
		[DispId(1001)]
		int SizeHeader { get; set; }
		[DispId(1002)]
		bool UseSizeHeader { get; set; }
		[DispId(1010)]
		string ToString();
		#endregion

		#region CStreamIO
		[DispId(2001)]
		TcpClient Tcp { get; }
		[DispId(2002)]
		bool Connected { get; }

		//[DispId(2101)]
		//bool Send(byte[] data);
		//[DispId(2102)]
		//bool Send(string data);
		//[DispId(2103)]
		//bool SendLine(string data, string EOT = CStreamIO.CRLF);
		//[DispId(2104)]
		//byte[] Receive(out int announcedSize);
		//[DispId(2105)]
		//string Receive();
		//[DispId(2106)]
		//string ReceiveLine(string EOT = CStreamIO.CRLF);
		//[DispId(2107)]
		//void Close();
		#endregion

		#region CStreamClientIO
		#endregion
	}
	[Guid("26BB1F7D-211E-4CCA-A8B1-A99A091C3176")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CStreamClientIO : CStreamIO, IStreamClientIO
	{
		#region constructors
		/// <summary>
		/// Open the adequate stream to the server.
		/// </summary>
		/// <param name="client">TCP client to use to open the stream</param>
		/// <param name="settings">Settings to use when manipulating the stream</param>
		public CStreamClientIO(TcpClient client, CStreamClientSettings settings) : base(client, settings)
		{
			if (settings.IsValid)
			{
				Settings = settings;
				// determine the kind of link to use
				if (Settings.UseSsl)
				{
					sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), default);
					client.ReceiveTimeout = settings.ConnectTimeout * 1000;// CStreamSettings.NO_TIMEOUT;// 5000;
					try
					{
						X509CertificateCollection xcco = default != Settings.Certificates && 0 != Settings.Certificates.Count ? new X509CertificateCollection() : default;
						try
						{
							if (default != xcco)
								foreach (CStreamClientSettings.SCertificate k in Settings.Certificates)
									xcco.Add(new X509Certificate2(k.Filename, k.Password));
						}
						catch (Exception ex)
						{
							CLog.EXCEPT(ex);
							xcco = default;
						}

						// authenticate against the server

#if NET35
						// check if TLS is supported
						bool useTLS = false;
						try
						{
							RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5");
							object value = (default != key ? key.GetValue("SP") : default);
							useTLS = (default != value && 1 <= (int)value);
						}
						catch (Exception ex) { }
						CLog.INFORMATION($".NET 3.5 {(useTLS ? "using TLS 1.2" : "not using TLS")}");
						if (useTLS)
							sslStream.AuthenticateAsClient(Settings.ServerName, default, (System.Security.Authentication.SslProtocols)3072, false);
						else
							sslStream.AuthenticateAsClient(Settings.ServerName);
#else
						sslStream.AuthenticateAsClient(Settings.ServerName, xcco, SslProtocols.None, false);

						//try
						//{
						//	sslStream.BeginAuthenticateAsClient(Settings.ServerName, );

						//}
						//catch (Exception)
						//{

						//	throw;
						//}
#endif
						CLog.INFORMATION($"using {sslStream.SslProtocol.ToString().ToUpper()} secured protocol");
					}
					catch (Exception)
					{
						sslStream = default;
						throw;
					}
					finally
					{
						client.ReceiveTimeout = settings.ReceiveTimeout;
					}
				}
				else
				{
					networkStream = client.GetStream();
					CLog.INFORMATION($"using unsecured protocol");
				}
			}
			else
				throw new Exception("invalid client stream settings");
		}
		#endregion

		#region properties
		private CStreamClientSettings Settings = default;
		#endregion

		#region methods
		/// <summary>
		/// The following method is invoked by the RemoteCertificateValidationDelegate
		/// Refer to .NET specs for a full description
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="sslPolicyErrors"></param>
		/// <returns></returns>
		private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			try
			{
				for (int i = 0; chain.ChainElements.Count > i; i++)
				{
					CLog.INFORMATION(Resources.CStreamIOSecurityChainElement.Format(new object[] { i + 1, (chain.ChainElements[i].Certificate?.Subject ?? Resources.GeneralNotSpecified) }));
					//CLog.INFORMATION($"Certificate details: {(chain.ChainElements[i].Certificate?.ToString() ?? "not specified")}");
				}
			}
			catch (Exception) { }

			//return true;
			if (Settings.AllowedSslErrors == (sslPolicyErrors | Settings.AllowedSslErrors))
				return true;

			// arrived here a certificate error occured
			// Do not allow this client to communicate with unauthenticated servers.
			CLog.Add(Resources.CStreamIOCertificateError.Format(sslPolicyErrors), TLog.ERROR);
			return false;
		}
		#endregion
	}

	[ComVisible(true)]
	[Guid("DBB95FAB-171A-4DFA-BF32-6E0AC64DC506")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IStreamServerIO
	{
		#region CStreamBase
		[DispId(1001)]
		int SizeHeader { get; set; }
		[DispId(1002)]
		bool UseSizeHeader { get; set; }
		[DispId(1010)]
		string ToString();
		#endregion

		#region CStreamIO
		[DispId(2001)]
		TcpClient Tcp { get; }
		[DispId(2002)]
		bool Connected { get; }

		//[DispId(2101)]
		//bool Send(byte[] data);
		//[DispId(2102)]
		//bool Send(string data);
		//[DispId(2103)]
		//bool SendLine(string data, string EOT = CStreamIO.CRLF);
		//[DispId(2104)]
		//byte[] Receive(out int announcedSize);
		//[DispId(2105)]
		//string Receive();
		//[DispId(2106)]
		//string ReceiveLine(string EOT = CStreamIO.CRLF);
		//[DispId(2107)]
		//void Close();
		#endregion

		#region CStreamClientIO
		[DispId(1)]
		uint Port { get; }
		#endregion
	}
	[Guid("9D87E461-0320-41E7-B88A-6F9E7D5A95A0")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CStreamServerIO : CStreamIO, IStreamServerIO
	{
		#region constructors
		/// <summary>
		/// Open the adequate stream to the server.
		/// </summary>
		/// <param name="client">TCP client to use to open the stream</param>
		/// <param name="settings">Settings to use when manipulating the stream</param>
		public CStreamServerIO(TcpClient client, CStreamServerSettings settings) : base(client, settings)
		{
			Port = settings.Port;
			Settings = settings;
			if (settings.UseSsl)
			{
				// SSL stream
				sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), default);
				int tmo = client.ReceiveTimeout * 1000;
				client.ReceiveTimeout = 5000;
				try
				{
					// authenticate the server but don't require the client to authenticate.
					sslStream.AuthenticateAsServer(settings.ServerCertificate);
				}
				catch (Exception ex)
				{
					sslStream = default;
					CLog.EXCEPT(ex);
					throw;
				}
				finally
				{
					client.ReceiveTimeout = tmo;
				}
			}
			else
			{
				// standard Stream
				networkStream = client.GetStream();
			}
		}
		#endregion

		#region properties
		public uint Port { get; private set; }
		private CStreamServerSettings Settings = default;
		#endregion

		#region methods
		/// <summary>
		/// The following method is invoked by the RemoteCertificateValidationDelegate
		/// Refer to .NET specs for a full description
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="sslPolicyErrors"></param>
		/// <returns></returns>
		private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			//return true;
			if (Settings.AllowedSslErrors == (sslPolicyErrors | Settings.AllowedSslErrors))
				return true;

			// arrived here a certificate error occured
			// Do not allow this client to communicate with unauthenticated servers.
			CLog.Add(Resources.CStreamIOCertificateError.Format(sslPolicyErrors), TLog.ERROR);
			return false;
		}
		#endregion
	}
}
