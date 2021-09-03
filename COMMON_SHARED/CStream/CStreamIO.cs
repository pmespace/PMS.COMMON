using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System;
using Microsoft.Win32;

namespace COMMON
{
	/// <summary>
	/// Basic ClientServer class
	/// </summary>
	[ComVisible(false)]
	public abstract class CStreamIO : CStreamBase
	{
		#region constructors
		public CStreamIO(int maxlen, TcpClient tcp) : base(maxlen)
		{
			Tcp = tcp;
		}
		~CStreamIO()
		{
			if (null != Tcp)
			{
				Tcp.Close();
				Tcp = null;
			}
		}
		#endregion

		#region properties
		public TcpClient Tcp { get; private set; }
		/// <summary>
		/// SSL stream if SSL security is required
		/// </summary>
		protected SslStream sslStream
		{
			get => _sslstream;
			set
			{
				_sslstream = value;
				if (null != sslStream)
					networkStream = null;
			}
		}
		private SslStream _sslstream = null;
		/// <summary>
		/// Standard stream if no security is required
		/// </summary>
		protected NetworkStream networkStream
		{
			get => _networkstream;
			set
			{
				_networkstream = value;
				if (null != networkStream)
					sslStream = null;
			}
		}
		private NetworkStream _networkstream = null;
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
			return $"TCP: {Tcp}";
		}
		/// <summary>
		/// Write to the adequate stream
		/// </summary>
		/// <param name="data">buffer to write</param>
		/// <returns>TRUE if write operation has been made, FALSE otherwise</returns>
		private bool Write(byte[] data)
		{
			if (null == data || 0 == data.Length)
				return false;
			if (null != sslStream)
			{
				sslStream.Write(data);
				return true;
			}
			else if (null != networkStream)
			{
				networkStream.Write(data, 0, data.Length);
				return true;
			}
			return false;
		}
		/// <summary>
		/// Flush the adequate stream
		/// </summary>
		private void Flush()
		{
			if (null != sslStream)
				sslStream.Flush();
			else if (null != networkStream)
				networkStream.Flush();
		}
		/// <summary>
		/// Read the adequate stream
		/// </summary>
		/// <param name="data">buffer where read data will be put</param>
		/// <param name="offset">offset at which to put data inside the buffer</param>
		/// <param name="count">maximum size of data the buffer can contain</param>
		/// <returns>The number of bytes read</returns>
		private int Read(byte[] data, int offset, int count)
		{
			int read = 0;
			if (null == data || 0 == data.Length)
				return 0;
			if (null != sslStream)
			{
				read = sslStream.Read(data, offset, count);
			}
			else if (null != networkStream)
			{
				read = networkStream.Read(data, offset, count); //0, data.Length);
			}
			return read;
		}
		/// <summary>
		/// Send a buffer to an outer entity
		/// </summary>
		/// <param name="data">The message to send</param>
		/// <param name="addSizeHeader">Indicates whether to add a 4 bytes header or not (it might be already included)</param>
		/// <returns>TRUE if the message has been sent, HALSE otherwise</returns>
		public bool Send(byte[] data, bool addSizeHeader)
		{
			if (null == data || 0 == data.Length)
				return false;
			// if requested, add the size header to the message to send
			int lengthSize = (addSizeHeader ? LengthBufferSize : 0);
			int size = data.Length + lengthSize;
			byte[] messageToSend = new byte[size];
			Buffer.BlockCopy(data, 0, messageToSend, lengthSize, data.Length);
			if (addSizeHeader)
			{
				byte[] bs = CMisc.SetBytesFromIntegralTypeValue(data.Length, LengthBufferSize);
				Buffer.BlockCopy(bs, 0, messageToSend, 0, lengthSize);
			}
			// arrived here the message is ready to be sent
			if (Write(messageToSend))
			{
				Flush();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Send a buffer to an outer entity
		/// </summary>
		/// <param name="data">The message to send as a string</param>
		/// <returns>TRUE if the message has been sent, HALSE otherwise</returns>
		public bool Send(string data)
		{
			byte[] bdata = (null != data ? Encoding.UTF8.GetBytes(data) : null);
			return Send(bdata, true);
		}
		/// <summary>
		/// Send a buffer to an outer entity
		/// This function prevents using any size header, using CR+LF as an EOT
		/// </summary>
		/// <param name="data">The message to send</param>
		/// <param name="EOT"></param>
		/// <returns>TRUE if the message has been sent, HALSE otherwise</returns>
		public bool SendLine(string data, string EOT = CRLF)
		{
			if (string.IsNullOrEmpty(EOT))
				EOT = CRLF;
			// verify the EOL is there, add it if necessary
			if (!string.IsNullOrEmpty(data))
			{
				// replace intermediate EOL
				data = data.Replace(EOT, "");
				// add the EOL at the end of string
				data += EOT;
			}
			byte[] bdata = (null != data ? Encoding.UTF8.GetBytes(data) : null);
			return Send(bdata, false);
		}
		/// <summary>
		/// Receive a buffer of a specific size from the server.
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
				return null;
			byte[] buffer = new byte[bufferSize];
			int bytesRead = 0;
			bool doContinue;
			do
			{
				// read stream for the specified buffer
				int nbBytes = Read(buffer, bytesRead, buffer.Length - bytesRead);
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
			return bufferReceived;
		}
		/// <summary>
		/// Receive a buffer of any size, reallocating memory to fit the buffer.
		/// The function will constantly reallocate the buffer to receive data .
		/// The function will not return until the buffer has been fully received or an error has occurred (timeout,...).
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <param name="EOT">A string which if found marks the end of transmission</param>
		/// <returns>The received buffer, with a 0 length if no data has been received</returns>
		private byte[] ReceiveNonSizedBuffer(string EOT)
		{
			if (string.IsNullOrEmpty(EOT))
				EOT = CRLF;
			// allocate buffer to receive
			byte[] buffer = new byte[CStreamSettings.ONEKB];
			int bytesRead = 0;
			bool doContinue;
			do
			{
				// read stream for the specified buffer
				int nbBytes = Read(buffer, bytesRead, buffer.Length - bytesRead);
				if (doContinue = (0 != nbBytes))
				{
					bytesRead += nbBytes;
					// allocate more memory if the buffer is full
					if (bytesRead == buffer.Length)
					{
						byte[] newbuffer = new byte[bytesRead + CStreamSettings.ONEKB];
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
			return bufferReceived;
		}
		/// <summary>
		/// Receive a buffer of an unknown size from the server.
		/// The buffer MUST begin with a size header of <see cref="CStreamBase.LengthBufferSize"/>
		/// The function will not return until the buffer has been fully received or an error has occurred (timeout,...)
		/// The returned buffer NEVER contains the size header (which is sent back using the "size" data).
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <param name="announcedSize">Size of the buffer as declared by the caller , therefore expected by the receiver.
		/// If that size differs from the size of the actually received buffer then an error has occurred</param>
		/// <returns>The received buffer WITHOUT the heaser size (whose value is indicated in announcedSize) if no error occurred, NULL if any error occured</returns>
		public byte[] Receive(out int announcedSize)
		{
			announcedSize = 0;
			// get the size of the buffer to receive
			byte[] bufferSize = ReceiveSizedBuffer(LengthBufferSize);
			if (LengthBufferSize == bufferSize.Length)
			{
				// get the size of the buffer to read and start reading it
				announcedSize = (int)CMisc.GetIntegralTypeValueFromBytes(bufferSize, LengthBufferSize);
				byte[] buffer = ReceiveSizedBuffer(announcedSize);
				if (announcedSize == buffer.Length)
					return buffer;
			}
			return null;
		}
		/// <summary>
		/// Receive a string of an unknown size from the server.
		/// The buffer MUST begin with a size header of <see cref="CStreamBase.LengthBufferSize"/>
		/// The function will not return until the string has been fully received or an error occurred (timeout).
		/// The returned string NEVER contains the size header.
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <returns>The received buffer as a string if no error occurred, an empty string otherwise</returns>
		public string Receive()
		{
			// receive the buffer
			byte[] buffer = Receive(out int size);
			return (size == buffer.Length ? Encoding.UTF8.GetString(buffer) : null);
		}
		/// <summary>
		/// Receive a string of an unknown size from the server.
		/// The string does not need to begin by a size header of <see cref="CStreamBase.LengthBufferSize"/> which will be ignored.
		/// The string MUST however finish (or at least contain) a CR+LF sequence (or contain it) marking the EOT.
		/// The function will not return until the string has been fully received or an error occurred (timeout).
		/// The returned string NEVER contains the size header.
		/// 
		/// >> THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <param name="EOT">A string which if found marks the end of transmission</param>
		/// <returns>The received buffer as a string if no error occurred, an empty string otherwise</returns>
		public string ReceiveLine(string EOT = CRLF)
		{
			// receive the buffer
			byte[] buffer = ReceiveNonSizedBuffer(EOT);
			string s = (null != buffer ? Encoding.UTF8.GetString(buffer) : null);
			// remove EOL if necessary
			if (!string.IsNullOrEmpty(s))
				s = s.Replace("\r\n", "");
			return s;
		}
		/// <summary>
		/// Close the adequate Stream
		/// </summary>
		public void Close()
		{
			try
			{
				if (null != sslStream)
				{
					sslStream.Close();
				}
				else if (null != networkStream)
				{
					networkStream.Close();
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
			Tcp = null;
		}
		#endregion
	}

	/// <summary>
	/// Client class
	/// </summary>
	[ComVisible(false)]
	public class CStreamClientIO : CStreamIO
	{
		#region constructors
		/// <summary>
		/// Open the adequate stream to the server.
		/// </summary>
		/// <param name="client">TCP client to use to open the stream</param>
		/// <param name="settings">Settings to use when manipulating the stream</param>
		public CStreamClientIO(TcpClient client, CStreamClientSettings settings) : base(settings.LengthBufferSize, client)
		{
			if (settings.IsValid)
			{
				Settings = settings;
				// determine the kind of link to use
				if (Settings.UseSsl)
				{
					sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
					try
					{
						// authenticate aginst the server
#if NET35
						// check if TLS is supported
						bool useTLS = false;
						try
						{
							RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5");
							object value = (null != key ? key.GetValue("SP") : null);
							useTLS = (null != value && 1 <= (int)value);
						}
						catch (Exception ex) { }
						CLog.Add($".NET 3.5 {(useTLS ? "using TLS 1.2" : "not using TLS")}");
						if (useTLS)
							sslStream.AuthenticateAsClient(Settings.ServerName, null, (System.Security.Authentication.SslProtocols)3072, false);
						else
							sslStream.AuthenticateAsClient(Settings.ServerName);
#else
						sslStream.AuthenticateAsClient(Settings.ServerName);
#endif
					}
					catch (Exception ex)
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
						sslStream = null;
						throw;
					}
				}
				else
					networkStream = client.GetStream();
			}
			else
				throw new Exception("Invalid client stream settings.");
		}
		#endregion

		#region properties
		private CStreamClientSettings Settings = null;
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
		public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			//return true;
			if (Settings.AllowedSslErrors == (sslPolicyErrors | Settings.AllowedSslErrors))
				return true;

			// arrived here a certificate error occured
			// Do not allow this client to communicate with unauthenticated servers.
			CLog.Add("Certificate error - " + sslPolicyErrors.ToString(), TLog.ERROR);
			return false;
		}
		#endregion
	}

	/// <summary>
	/// Server class
	/// </summary>
	[ComVisible(false)]
	public class CStreamServerIO : CStreamIO
	{
		#region constructors
		/// <summary>
		/// Open the adequate stream to the server.
		/// </summary>
		/// <param name="client">TCP client to use to open the stream</param>
		/// <param name="settings">Settings to use when manipulating the stream</param>
		public CStreamServerIO(TcpClient client, CStreamServerSettings settings) : base(settings.LengthBufferSize, client)
		{
			Port = settings.Port;
			if (settings.UseSsl)
			{
				// SSL stream
				sslStream = new SslStream(client.GetStream(), false);
				try
				{
					// authenticate the server but don't require the client to authenticate.
					//sslStream.AuthenticateAsServer(settings.ServerCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);
					sslStream.AuthenticateAsServer(settings.ServerCertificate);
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
					sslStream = null;
					throw;
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
		#endregion
	}
}
