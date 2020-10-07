using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System;


namespace COMMON
{
	/// <summary>
	/// Basic ClientServer class
	/// </summary>
	[ComVisible(false)]
	public abstract class CStreamIO: CStreamBase
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

		#region methods
		/// <summary>
		/// Write to the adequate stream
		/// </summary>
		/// <param name="data">buffer to write</param>
		/// <returns>TRUE if write operation has been made, FALSE otherwise</returns>
		private bool Write(byte[] data)
		{
			try
			{
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
			}
			catch (Exception) { }
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
			try
			{
				if (null != sslStream)
				{
					read = sslStream.Read(data, offset, count);
				}
				else if (null != networkStream)
				{
					read = networkStream.Read(data, 0, data.Length);
				}
			}
			catch (Exception ex) { read = 0; }
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
			if (null == data)
				return false;
			// if requested, add the size header to the message to send
			//int lengthSize = (addSizeHeader ? SIZE_HEADER_BUFFER_LENGTH : 0);
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
			byte[] bdata = Encoding.UTF8.GetBytes(data);
			return Send(bdata, true);
		}
		/// <summary>
		/// Receive a buffer of a specific size from the server.
		/// The function will not return until the buffer has been fully received or an error occurred (timeout).
		/// >> This function may raise an exception
		/// </summary>
		/// <param name="bufferSize">Size of the buffer to receive</param>
		/// <returns>The received buffer</returns>
		private byte[] Receive(int bufferSize)
		{
			try
			{
				// allocate buffer to receive
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
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
			return null;
		}
		/// <summary>
		/// Receive a buffer of an unknown size from the server.
		/// The function will not return until the buffer has been fully received or an error occurred (timeout)
		/// The returned buffer NEVER contains the size header (which is sent back using the "size" data).
		/// >> This function may raise an exception
		/// </summary>
		/// <param name="size"> size of the buffer to receive actually declared by the sender</param>
		/// <returns>The received buffer if no error occurred, NULL otherwise</returns>
		public byte[] Receive(out int size)
		{
			size = 0;
			try
			{
				// get the size of the buffer to receive
				byte[] bufferSize = Receive((int)LengthBufferSize);
				if ((int)LengthBufferSize == bufferSize.Length)
				{
					// get the size of the buffer to read and start reading it
					size = (int)CMisc.GetIntegralTypeValueFromBytes(bufferSize, LengthBufferSize);
					byte[] buffer = Receive(size);
					return buffer;
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
			return null;
		}
		/// <summary>
		/// Receive a string of an unknown size from the server.
		/// The function will not return until the string has been fully received or an error occurred (timeout).
		/// The returned string NEVER contains the size header.
		/// >> This function may raise an exception
		/// </summary>
		/// <returns>The received buffer as a string if no error occurred, an empty string otherwise</returns>
		public string Receive()
		{
			int size;
			// receive the buffer
			byte[] buffer = Receive(out size);
			if (size == buffer.Length)
				// a buffer has been received, the returned buffer does not contain the size header, we convert the message to a string
				return Encoding.UTF8.GetString(buffer);
			else
				return string.Empty;
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
	public class CStreamClientIO: CStreamIO
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
				// determine the kind of link to use
				if (settings.UseSsl)
				{
					sslStream = new SslStream(client.GetStream(), false, (settings.CheckCertificate ? new RemoteCertificateValidationCallback(ValidateServerCertificate) : null), null);
					try
					{
						// The server name must match the name on the server certificate.
						sslStream.AuthenticateAsClient(settings.ServerName);
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
		public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			//return true;
			if (sslPolicyErrors == SslPolicyErrors.None)
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
	public class CStreamServerIO: CStreamIO
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
