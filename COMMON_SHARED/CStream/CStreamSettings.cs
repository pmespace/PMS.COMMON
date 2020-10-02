using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Net;
using System;
using System.Threading;

namespace COMMON
{
	[ComVisible(false)]
	interface IStreamSettings
	{
		#region IStreamSettings
		[DispId(1001)]
		int NoTimeout { get; }
		[DispId(1002)]
		int ReceiveTimeout { get; set; }
		[DispId(1003)]
		int SendTimeout { get; set; }
		[DispId(1004)]
		int ReceiveBufferSize { get; set; }
		[DispId(1005)]
		int SendBufferSize { get; set; }
		[DispId(1006)]
		bool UseSsl { get; }
		[DispId(1007)]
		string Localhost { get; }
		[DispId(1008)]
		uint DefaultServerPort { get; }
		#endregion
	}
	[ComVisible(false)]
	public abstract class CStreamSettings: CStreamBase
	{
		#region constructors
		public CStreamSettings() { }
		public CStreamSettings(int lengthBufferSize) : base(lengthBufferSize) { }
		#endregion

		#region constants
		public const int ONEKB = 1024;
		public const int ONESECOND = 1000;
		public const uint DEFAULT_PORT = 29413;
		public const int NO_TIMEOUT = 0;
		#endregion

		#region properties
		public abstract bool IsValid { get; }
		public int NoTimeout { get => NO_TIMEOUT; }
		/// <summary>
		/// Reception timer specified in SECONDS
		/// </summary>
		public int ReceiveTimeout
		{
			get => _receivetimeout;
			set
			{
				if (NO_TIMEOUT >= value)
					_receivetimeout = NO_TIMEOUT;
				else
					_receivetimeout = value;
			}
		}
		private int _receivetimeout = DEFAULT_RECEIVE_TIMEOUT;
		public const int DEFAULT_RECEIVE_TIMEOUT = NO_TIMEOUT;
		/// <summary>
		/// Specified in SECONDS
		/// </summary>
		public int SendTimeout
		{
			get => _sendtimeout;
			set
			{
				if (NO_TIMEOUT >= value)
					_sendtimeout = NO_TIMEOUT;
				else
					_sendtimeout = value;
			}
		}
		private int _sendtimeout = DEFAULT_SEND_TIMEOUT;
		public const int DEFAULT_SEND_TIMEOUT = 5;
		/// <summary>
		/// Buffer size in bytes
		/// </summary>
		public int ReceiveBufferSize
		{
			get => _receivebuffersize;
			set
			{
				if (0 == value)
					_receivebuffersize = DEFAULT_RECEIVE_BUFFER_SIZE;
				else
					_receivebuffersize = Math.Abs(value);
			}
		}
		private int _receivebuffersize = DEFAULT_RECEIVE_BUFFER_SIZE;
		public const int DEFAULT_RECEIVE_BUFFER_SIZE = 50 * ONEKB;
		/// <summary>
		/// Buffer size in bytes
		/// </summary>
		public int SendBufferSize
		{
			get => _sendbuffersize;
			set
			{
				if (0 == value)
					_sendbuffersize = DEFAULT_SEND_BUFFER_SIZE;
				else
					_sendbuffersize = Math.Abs(value);
			}
		}
		private int _sendbuffersize = DEFAULT_SEND_BUFFER_SIZE;
		public const int DEFAULT_SEND_BUFFER_SIZE = 50 * ONEKB;
		/// <summary>
		/// Use SSL layer or not
		/// </summary>
		public bool UseSsl { get; protected set; } = false;
		/// <summary>
		/// The local host IP address
		/// </summary>
		public string Localhost { get => CStream.Localhost(); }
		/// <summary>
		/// Default server port to use
		/// </summary>
		public uint DefaultServerPort { get => DEFAULT_PORT; }
		#endregion
	}

	[Guid("BE7495F7-DA7A-4584-AEB9-789AF316C971")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[ComVisible(true)]
	public interface IStreamClientSettings
	{
		#region IStreamClientSettings
		[DispId(1)]
		string IP { get; set; }
		[DispId(2)]
		uint Port { get; set; }
		[DispId(3)]
		bool FoundOnDNS { get; }
		[DispId(4)]
		bool IsValid { get; }
		[DispId(5)]
		string ServerName { get; set; }
		[DispId(6)]
		bool CheckCertificate { get; set; }
		[DispId(7)]
		string FullIP { get; }

		[DispId(100)]
		string ToString();
		#endregion

		#region IStreamSettings
		[DispId(1001)]
		int NoTimeout { get; }
		[DispId(1002)]
		int ReceiveTimeout { get; set; }
		[DispId(1003)]
		int SendTimeout { get; set; }
		[DispId(1004)]
		int ReceiveBufferSize { get; set; }
		[DispId(1005)]
		int SendBufferSize { get; set; }
		[DispId(1006)]
		bool UseSsl { get; }
		[DispId(1007)]
		string Localhost { get; }
		[DispId(1008)]
		uint DefaultServerPort { get; }
		#endregion
	}
	[Guid("990A2D0C-1A1C-4E34-9C9D-75905AC95915")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CStreamClientSettings: CStreamSettings, IStreamClientSettings
	{
		#region constructors
		public CStreamClientSettings() { SetIP(null); }
		public CStreamClientSettings(int lengthBufferSize) : base(lengthBufferSize) { SetIP(null); }
		public CStreamClientSettings(string ip, uint port = DEFAULT_PORT) { SetIP(ip, port); }
		public CStreamClientSettings(int lengthBufferSize, string ip, uint port = DEFAULT_PORT) : base(lengthBufferSize) { SetIP(ip, port); }
		#endregion

		#region public properties
		public override bool IsValid { get => null != EndPoint; }
		/// <summary>
		/// The IP address to reach or an empty string if invalid
		/// </summary>
		public string IP
		{
			get => (IsValid ? EndPoint.Address.ToString() : null);
			set => SetIP(value, Port);
		}
		/// <summary>
		/// The IP port to use or 0 if invalid
		/// </summary>
		public uint Port
		{
			get => IsValid ? (uint)EndPoint.Port : DEFAULT_PORT;
			set => SetIP(IP, IPEndPoint.MinPort < value && IPEndPoint.MaxPort >= value ? value : DEFAULT_PORT);
		}
		/// <summary>
		/// The IP port to use or 0 if invalid
		/// </summary>
		public bool FoundOnDNS { get; private set; } = false;
		/// <summary>
		/// The name of the server to authenticate against. It must be empty if no authentication is required
		/// </summary>
		public string ServerName
		{
			get => _servername;
			set
			{
				_servername = value;
				UseSsl = (!string.IsNullOrEmpty(ServerName));
			}
		}
		private string _servername = string.Empty;
		/// <summary>
		/// Use certificate security or not
		/// </summary>
		public bool CheckCertificate { get; set; } = true;
		/// <summary>
		/// The full IP address
		/// </summary>
		public string FullIP { get => (IsValid ? IP + (0 != Port ? ":" + Port : string.Empty) : string.Empty); }
		#endregion

		#region private properties
		/// <summary>
		/// IP address to targer
		/// </summary>
		internal IPAddress Address { get; private set; } = null;
		/// <summary>
		/// IP end point to target
		/// </summary>
		internal IPEndPoint EndPoint { get; private set; } = null;
		#endregion

		#region methods
		/// <summary>
		/// Returns the full TCP/IP address or an empty string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (null != EndPoint)
				return EndPoint.ToString();
			else
				return string.Empty;
		}
		/// <summary>
		/// Set the TCP/IP address to use
		/// </summary>
		/// <param name="ip">IP or URL to reach</param>
		/// <param name="port">Port to use on this IP</param>
		/// <returns>TRUE if the IP has been set, FALSE otherwise</returns>
		private bool SetIP(string ip, uint port = DEFAULT_PORT)
		{
			bool url = false;
			// test IP address or URL, either that works
			if (CMisc.IsValidFormat(ip, RegexIP.REGEX_IPV4_WITHOUT_PORT) || (url = CMisc.IsValidFormat(ip, RegexIP.REGEX_URL_WITHOUT_PORT)))
			{
				try
				{
					try
					{
						// if DNS is not supported or requested bypass it
						if (!url)
							throw new Exception();
						// URL is valid, let's try to resolve
						IPHostEntry ipHost = Dns.GetHostEntry(ip);
						FoundOnDNS = true;
						Address = ipHost.AddressList[0];
						EndPoint = new IPEndPoint(Address, (int)port);
					}
					catch (Exception)
					{
						// IP address is not found on DNS
						FoundOnDNS = false;
						Address = IPAddress.Parse(ip);
						EndPoint = new IPEndPoint(Address, (int)port);
					}
					return true;
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, new Exception("Invalid IP address: " + ip + (0 < port ? ":" + port : string.Empty), ex));
					Address = null;
					EndPoint = null;
				}
			}
			return false;
		}
		/// <summary>
		/// Create a <see cref="CStreamClientSettings"/> object
		/// </summary>
		/// <param name="ip">IP address or URL to target</param>
		/// <param name="port">Port to target</param>
		/// <param name="servername">Server name to authenticate against</param>
		/// <param name="sendtimeout">Send timeout</param>
		/// <param name="receivetimeout">Receive timeout</param>
		/// <param name="lengthBufferSize">Size of size buffer</param>
		/// <returns>A CStreamSettings object</returns>
		public static CStreamClientSettings Prepare(string ip, uint port, string servername = null, int receivetimeout = NO_TIMEOUT, int sendtimeout = NO_TIMEOUT, int lengthBufferSize = CMisc.FOURBYTES)
		{
			return new CStreamClientSettings(lengthBufferSize, ip, port)
			{
				ServerName = servername,
				CheckCertificate = !string.IsNullOrEmpty(servername),
				SendTimeout = sendtimeout,
				ReceiveTimeout = receivetimeout,
			};
		}
		#endregion
	}

	[Guid("F4BC72B2-4375-4723-B59D-809182C8CFDE")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[ComVisible(true)]
	public interface IStreamServerSettings
	{
		#region IStreamServerSettings
		[DispId(1)]
		bool IsValid { get; }
		[DispId(2)]
		uint Port { get; set; }
		[DispId(3)]
		string Certificate { get; set; }
		[DispId(4)]
		X509Certificate ServerCertificate { get; set; }

		[DispId(100)]
		string ToString();
		#endregion

		#region IStreamSettings
		[DispId(1001)]
		int NoTimeout { get; }
		[DispId(1002)]
		int ReceiveTimeout { get; set; }
		[DispId(1003)]
		int SendTimeout { get; set; }
		[DispId(1004)]
		int ReceiveBufferSize { get; set; }
		[DispId(1005)]
		int SendBufferSize { get; set; }
		[DispId(1006)]
		bool UseSsl { get; }
		[DispId(1007)]
		string Localhost { get; }
		[DispId(1008)]
		uint DefaultServerPort { get; }
		#endregion
	}
	[Guid("EF1D0636-72B9-4F21-98A2-6F0EE3B048B5")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CStreamServerSettings: CStreamSettings, IStreamServerSettings
	{
		#region constructors
		public CStreamServerSettings() { }
		public CStreamServerSettings(int lengthBufferSize) : base(lengthBufferSize) { }
		public CStreamServerSettings(uint port) { Port = port; }
		public CStreamServerSettings(int lengthBufferSize, uint port) : base(lengthBufferSize) { Port = port; }
		#endregion

		#region properties
		public override bool IsValid { get => true; }
		/// <summary>
		/// The IP port to use or 0 if invalid
		/// </summary>
		public uint Port
		{
			get => _port;
			set
			{
				if (IPEndPoint.MinPort < value && IPEndPoint.MaxPort >= value)
					_port = value;
				else
					_port = DEFAULT_PORT;
			}
		}
		private uint _port = DEFAULT_PORT;
		/// <summary>
		/// Certificate file to use to secure the connection
		/// </summary>
		public string Certificate
		{
			get => _certificate;
			set
			{
				_certificate = value;
				if (!string.IsNullOrEmpty(Certificate))
				{
					try
					{
						ServerCertificate = X509Certificate.CreateFromCertFile(Certificate);
					}
					catch (Exception ex)
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
						_certificate = string.Empty;
						ServerCertificate = null;
					}
				}
				else
					ServerCertificate = null;
			}
		}
		private string _certificate = string.Empty;
		/// <summary>
		/// The SSL certificate to use to authenticate the server
		/// </summary>
		public X509Certificate ServerCertificate
		{
			get => _servercertificate;
			set
			{
				_servercertificate = value;
				UseSsl = (null != ServerCertificate);
			}
		}
		private X509Certificate _servercertificate = null;
		#endregion

		#region methods
		/// <summary>
		/// Create a <see cref="CStreamServerSettings"/> object
		/// </summary>
		/// <param name="lengthBufferSize">Size of size buffer</param>
		/// <param name="port">Port to target</param>
		/// <param name="certificate">The certificate file ".CER" to use to authenticate th server. If empty no authentication is done</param>
		/// <param name="sendtimeout">Send timeout</param>
		/// <param name="receivetimeout">Receive timeout</param>
		/// <returns>A CStreamSettings object</returns>
		public static CStreamServerSettings Prepare(uint port, string certificate = null, int receivetimeout = CStreamSettings.NO_TIMEOUT, int sendtimeout = CStreamSettings.NO_TIMEOUT, int lengthBufferSize = CMisc.FOURBYTES)
		{
			return new CStreamServerSettings(lengthBufferSize, port)
			{
				Certificate = certificate,
				SendTimeout = sendtimeout,
				ReceiveTimeout = receivetimeout,
			};
		}
		#endregion
	}
}
