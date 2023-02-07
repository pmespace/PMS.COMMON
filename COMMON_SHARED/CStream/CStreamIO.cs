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

namespace COMMON
{
	[ComVisible(false)]
	public abstract class CStreamIO : CStreamBase
	{
		#region constructors
		public CStreamIO(TcpClient tcp, CStreamBase sb) : base(sb)
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
					TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections().Where(x => x.LocalEndPoint.Equals(Tcp.Client.RemoteEndPoint) && x.RemoteEndPoint.Equals(Tcp.Client.LocalEndPoint)).ToArray();
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
		public const string CRLF = "\r\n";
		public const string LFCR = "\n\r";
		public const byte ETX = 0x03;
		public const byte EOT = 0x04;
		#endregion

		#region methods
		public override string ToString()
		{
			if (default != Tcp)
				return $"streamIO => connected: {Tcp.Connected}; remote end point: {Tcp.Client.RemoteEndPoint}; receive buffer size: {Tcp.ReceiveBufferSize}; receive timeout: {Tcp.ReceiveTimeout}";
			return default;
		}
		/// <summary>
		/// Write to the adequate stream
		/// </summary>
		/// <param name="data">buffer to write</param>
		/// <returns>TRUE if write operation has been made, FALSE otherwise</returns>
		private bool Write(byte[] data)
		{
			if (data.IsNullOrEmpty()) return false;

			Stream stream = networkStream;
			if (default == stream) stream = sslStream;
			if (default == stream) return false;

			try
			{
				stream.Write(data, 0, data.Length);
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
		private void Flush()
		{
			Stream stream = networkStream;
			if (default == stream) stream = sslStream;
			if (default == stream) return;

			try
			{
				stream.Flush();
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
		}
		/// <summary>
		/// Read the adequate stream
		/// </summary>
		/// <param name="data">Buffer where read data will be put</param>
		/// <param name="offset">Offset at which to put data inside the buffer</param>
		/// <returns>The number of bytes read</returns>
		private int Read(byte[] data, int offset)
		{
			if (data.IsNullOrEmpty() || data.Length <= offset) return 0;
			int read = 0;

			Stream stream = networkStream;
			if (default == stream) stream = sslStream;
			if (default == stream) return 0;

			try
			{
				read = stream.Read(data, offset, data.Length - offset);
			}
			catch (IOException) { }
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return read;
		}
		/// <summary>
		/// Send a buffer to an outer entity.
		/// The buffer will always be prefixed with a header indicating the size of the message.
		/// </summary>
		/// <param name="data">The message to send</param>
		/// <returns>TRUE if the message has been sent, HALSE otherwise</returns>
		internal bool Send(byte[] data)
		{
			if (data.IsNullOrEmpty())
			{
				CLog.INFORMATION("no data to send");
				return false;
			}
			// if requested, add the size header to the message to send
			int lengthSize = (UseHeaderBytes ? HeaderBytes : 0);
			int size = (int)data.Length + lengthSize;
			byte[] messageToSend = new byte[size];
			Buffer.BlockCopy(data, 0, messageToSend, (int)lengthSize, data.Length);
			if (UseHeaderBytes)
			{
				byte[] bs = CMisc.SetBytesFromIntegralTypeValue((int)data.Length, false);
				Buffer.BlockCopy(bs, 0, messageToSend, 0, (int)lengthSize);
			}
			// arrived here the message is ready to be sent
			CLog.Add(new CLogMsgs()
			{
				new CLogMsg($"sending message {messageToSend.Length} bytes - {(0 == lengthSize ? "without adding any header" : $"after having added {lengthSize} bytes header")}", TLog.INFOR),
				new CLogMsg($"data [{CMisc.AsHexString(messageToSend)}]", TLog.DEBUG),
			});
			if (Write(messageToSend))
			{
				Flush();
				return true;
			}
			CLog.ERROR($"failed sending message {messageToSend.Length} bytes - {(0 == lengthSize ? "without adding any header" : $"after having added {lengthSize} bytes header")}");
			return false;
		}
		/// <summary>
		/// Send a buffer to an outer entity.
		/// The buffer will always be prefixed with a header indicating the size of the message.
		/// </summary>
		/// <param name="data">The message to send as a string</param>
		/// <returns>TRUE if the message has been sent, HALSE otherwise</returns>
		internal bool Send(string data)
		{
			byte[] bdata = (default != data ? Encoding.UTF8.GetBytes(data) : default);
			return Send(bdata);
		}
		/// <summary>
		/// Send a buffer to an outer entity.
		/// The buffer is never prefixed with any header describing its size, it is determined by presence of the <paramref name="EOT"/> finishing the message.
		/// </summary>
		/// <param name="data">The message to send</param>
		/// <param name="EOT">The end of transmission character to use to terminate the mesage (CR+LF by default)</param>
		/// <returns>TRUE if the message has been sent, HALSE otherwise</returns>
		internal bool SendLine(string data, string EOT = CRLF)
		{
			if (EOT.IsNullOrEmpty()) EOT = CRLF;

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
				new CLogMsg($"sending text message {data.Length} characters", TLog.INFOR),
				new CLogMsg($"data [{data}]", TLog.DEBUG),
			});
			byte[] bdata = (default != data ? Encoding.UTF8.GetBytes(data) : default);
			bool ok = Send(bdata);
			if (!ok)
				CLog.ERROR($"failed sending {data.Length} characters text message");
			return ok;
		}
		/// <summary>
		/// Receives a buffer of a specific size from the server.
		/// The function will allocate the buffer of the specified size to receive data.
		/// The function will not return until the buffer has been fully received or an error has occurred (timeout,...).
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <param name="bufferSize">Size of the buffer to receive</param>
		/// <returns>The received buffer, with a 0 length if no data has been received</returns>
		private byte[] ReceiveSizedBuffer(int bufferSize)
		{
			// allocate buffer to receive
			if (0 == bufferSize)
			{
				CLog.ERROR("no size buffer specified");
				return default;
			}
			byte[] buffer = new byte[bufferSize];
			int bytesRead = 0;
			bool doContinue;
			CLog.DEBUG($"waiting to receive fixed buffer of {buffer.Length} bytes");
			do
			{
				// read stream for the specified buffer
				int nbBytes = Read(buffer, bytesRead);
				if (doContinue = (0 != nbBytes))
				{
					bytesRead += nbBytes;
					// we continue until we've filled up the buffer
					doContinue = bytesRead < bufferSize;
				}
			}
			while (doContinue);
			// create a buffer of the real number of bytes received (which can't be higher than the expected number of bytes)
			byte[] bufferReceived = new byte[bytesRead];
			Buffer.BlockCopy(buffer, 0, bufferReceived, 0, bytesRead);
			if (0 == bytesRead)
				CLog.ERROR($"received no data");
			else if (bytesRead != bufferSize)
				CLog.Add(new CLogMsgs()
				{
					new CLogMsg($"received {bytesRead} bytes, expecting {bufferSize}", TLog.ERROR),
					new CLogMsg($"data [{CMisc.AsHexString(bufferReceived)}]", TLog.DEBUG),
				});
			else
				CLog.Add(new CLogMsgs()
				{
					new CLogMsg($"received {bytesRead} bytes", TLog.DEBUG),
					new CLogMsg($"data [{CMisc.AsHexString(bufferReceived)}]",TLog.DEBUG),
				});
			return bufferReceived;
		}
		/// <summary>
		/// Receives a buffer of any size, reallocating memory to fit the buffer.
		/// The function will constantly reallocate the buffer to receive data.
		/// The function will not return until the buffer has been fully received or an error has occurred (timeout,...).
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <param name="EOT">A string which if found marks the end of transmission</param>
		/// <param name="bufferToAllocate">Size of buffer to allocate to read the incoming data (default is 1 Kb)</param>
		/// <returns>The received buffer, with a 0 length if no data has been received</returns>
		private byte[] ReceiveNonSizedBuffer(string EOT, int bufferToAllocate = CStreamSettings.ONEKB)
		{
			if (string.IsNullOrEmpty(EOT))
				EOT = CRLF;
			// allocate buffer to receive
			byte[] buffer = new byte[bufferToAllocate];
			int bytesRead = 0;
			bool doContinue;
			CLog.DEBUG($"waiting to receive buffer with no specific size");
			do
			{
				// read stream for the specified buffer
				int nbBytes = Read(buffer, bytesRead);//, buffer.Length - bytesRead);
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
						doContinue = !Encoding.UTF8.GetString(buffer).Contains(EOT);
				}
			}
			while (doContinue);
			// create a buffer of the real number of bytes received (which can't be higher than the expected number of bytes)
			byte[] bufferReceived = new byte[bytesRead];
			Buffer.BlockCopy(buffer, 0, bufferReceived, 0, bytesRead);
			CLog.Add(new CLogMsgs()
			{
				new CLogMsg($"received {bytesRead} bytes", TLog.INFOR),
				new CLogMsg($"data [{CMisc.AsHexString(bufferReceived)}]", TLog.DEBUG),
			});
			return bufferReceived;
		}
		/// <summary>
		/// Receive a buffer of an unknown size from the server.
		/// The buffer MUST begin with a size header of <see cref="CStreamBase.HeaderBytes"/>
		/// The function will not return until the buffer has been fully received or an error has occurred (timeout,...)
		/// The returned buffer NEVER contains the size header (which is sent back using the "size" data).
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <param name="announcedSize">Size of the buffer as declared by the caller , therefore expected by the receiver.
		/// If that size differs from the size of the actually received buffer then an error has occurred</param>
		/// <returns>The received buffer WITHOUT the heaser size (whose value is indicated in announcedSize) if no error occurred, NULL if any error occured</returns>
		internal byte[] Receive(out int announcedSize)
		{
			announcedSize = 0;

			// determine whether the header must be used or not and the size arrived here, either an automatic header is used or the protocol contains one, get the size of the headerto receive 
			byte[] bufferSize = ReceiveSizedBuffer(HeaderBytes);
			if (!bufferSize.IsNullOrEmpty() && HeaderBytes == bufferSize.Length)
			{
				// get the size of the buffer to read and start reading it
				if (0 != (announcedSize = (int)CMisc.GetIntegralTypeValueFromBytes(bufferSize, 0, HeaderBytes)))
				{
					CLog.DEBUG($"received announce of {announcedSize} bytes");
					byte[] buffer = ReceiveSizedBuffer(announcedSize);
					if (!buffer.IsNullOrEmpty() && announcedSize == buffer.Length)
					{
						return buffer;
					}
				}
				CLog.ERROR($"received announce of 0 byte");
			}
			return default;
		}
		/// <summary>
		/// Receive a string of an unknown size from the server.
		/// The buffer MUST begin with a size header 
		/// The function will not return until the string has been fully received or an error occurred (timeout).
		/// The returned string NEVER contains the size header.
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <returns>The received buffer as a string if no error occurred, an empty string otherwise</returns>
		internal string Receive()
		{
			// receive the buffer
			byte[] buffer = Receive(out int size);
			return (!buffer.IsNullOrEmpty() && size == buffer.Length ? Encoding.UTF8.GetString(buffer) : default);
		}
		/// <summary>
		/// Receive a string of an unknown size from the server.
		/// The message never begins with a heabder.
		/// The string MUST however finish (or at least contain) a CR+LF sequence (or contain it) marking the EOT.
		/// The function will not return until the string has been fully received or an error occurred (timeout).
		/// The returned string NEVER contains the size header.
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <param name="EOT">A string which if found marks the end of transmission</param>
		/// <returns>The received buffer as a string if no error occurred, an empty string otherwise</returns>
		internal string ReceiveLine(string EOT = CRLF)
		{
			// receive the buffer
			byte[] buffer = ReceiveNonSizedBuffer(EOT);
			string s = (default != buffer ? Encoding.UTF8.GetString(buffer) : default);
			// remove EOT if necessary
			if (!string.IsNullOrEmpty(s))
				s = s.Replace(EOT, "");
			CLog.DEBUG($"data as string [{s}]");
			return s;
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
		int HeaderBytes { get; set; }
		[DispId(1002)]
		bool UseHeaderBytes { get; set; }
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
					int tmo = client.ReceiveTimeout * 1000;
					client.ReceiveTimeout = 5000;
					try
					{
						// authenticate aginst the server

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
						sslStream.AuthenticateAsClient(Settings.ServerName);
#endif
						CLog.INFORMATION($"using {sslStream.SslProtocol.ToString().ToUpper()} secured protocol");
					}
					catch (Exception ex)
					{
						CLog.EXCEPT(ex);
						sslStream = default;
						throw;
					}
					finally
					{
						client.ReceiveTimeout = tmo;
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
					CLog.INFORMATION($"Chain element {i + 1}: {(chain.ChainElements[i].Certificate?.Subject ?? "not specified")}");
					//CLog.INFORMATION($"Certificate details: {(chain.ChainElements[i].Certificate?.ToString() ?? "not specified")}");
				}
			}
			catch (Exception) { }

			//return true;
			if (Settings.AllowedSslErrors == (sslPolicyErrors | Settings.AllowedSslErrors))
				return true;

			// arrived here a certificate error occured
			// Do not allow this client to communicate with unauthenticated servers.
			CLog.Add($"Certificate error [{sslPolicyErrors}]", TLog.ERROR);
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
		int HeaderBytes { get; set; }
		[DispId(1002)]
		bool UseHeaderBytes { get; set; }
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
					CLog.EXCEPT(ex);
					sslStream = default;
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
			CLog.Add($"Certificate error [{sslPolicyErrors}]", TLog.ERROR);
			return false;
		}
		#endregion
	}
}
