using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Net;
using System;
using System.Threading;
using System.Net.Security;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text.RegularExpressions;

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
		[DispId(1009)]
		CStreamDelegates.ClientServerOnMessageToLog OnMessageToLog { get; set; }
		#endregion
	}
	[ComVisible(false)]
	public abstract class CStreamSettings : CStreamBase
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
		[JsonIgnore]
		public abstract bool IsValid { get; }
		[JsonIgnore]
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
		[JsonIgnore]
		public bool UseSsl { get; protected set; } = false;
		/// <summary>
		/// The local host IP v4 address
		/// </summary>
		[JsonIgnore]
		public string Localhost { get => CStream.Localhost(); }
		/// <summary>
		/// The local host IP v6 address
		/// </summary>
		[JsonIgnore]
		public string LocalhostV6 { get => CStream.Localhost(false); }
		/// <summary>
		/// Default server port to use
		/// </summary>
		[JsonIgnore]
		public uint DefaultServerPort { get => DEFAULT_PORT; }
		/// <summary>
		/// A delegate allowing to, when a message either received or about to be sent, review the content of this message before it is logged, thus allowing either to hide or alter the content TO LOG (not the content of the message), thus allowing PCI-DSS compliance
		/// </summary>
		[JsonIgnore]
		public CStreamDelegates.ClientServerOnMessageToLog OnMessageToLog { get => _onmessagetolog; set => _onmessagetolog = value; }
		private CStreamDelegates.ClientServerOnMessageToLog _onmessagetolog = default;
		/// <summary>
		/// Allowed SSL errors while trying to connect
		/// </summary>
		public SslPolicyErrors AllowedSslErrors
		{
			get => _allowedsslerrors;
			set { _allowedsslerrors = value & (SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable); }
		}
		private SslPolicyErrors _allowedsslerrors = SslPolicyErrors.None;
		#endregion

		#region methods
		public override string ToString()
		{
			return $"ReceiveTimeout: {ReceiveTimeout}; SendTimeout: {SendTimeout}; ReceiveBufferSize: {ReceiveBufferSize}; SendBufferSize: {SendBufferSize}; UseSsl: {UseSsl}; Valid: {IsValid}; " + base.ToString();
		}
		#endregion
	}

	[ComVisible(true)]
	[Guid("BE7495F7-DA7A-4584-AEB9-789AF316C971")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
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
		[DispId(8)]
		SslPolicyErrors AllowedSslErrors { get; set; }
		[DispId(9)]
		int ConnectTimeout { get; set; }

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
		[DispId(1009)]
		CStreamDelegates.ClientServerOnMessageToLog OnMessageToLog { get; set; }
		[DispId(1010)]
		string LocalhostV6 { get; }
		#endregion
	}
	[Guid("990A2D0C-1A1C-4E34-9C9D-75905AC95915")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CStreamClientSettings : CStreamSettings, IStreamClientSettings
	{
		#region constructors
		public CStreamClientSettings() { SetIP(default); }
		public CStreamClientSettings(int lengthBufferSize) : base(lengthBufferSize) { SetIP(default); }
		public CStreamClientSettings(string ip, uint port = DEFAULT_PORT) { SetIP(ip, port); }
		public CStreamClientSettings(int lengthBufferSize, string ip, uint port = DEFAULT_PORT) : base(lengthBufferSize) { SetIP(ip, port); }
		#endregion

		#region public properties
		public override bool IsValid { get => default != EndPoint; }
		/// <summary>
		/// The IP address to reach or an empty string if invalid.
		/// This can be set to "localhost" (case irrelevant) and the local host IP address will be resolved and put as the IP address to use
		/// </summary>
		public string IP
		{
			get => (IsValid ? EndPoint.Address.ToString() : default);
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
		[JsonIgnore]
		public bool FoundOnDNS { get => CanBeFoundOnDNS(IP); }
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
		private string _servername = default;
		/// <summary>
		/// Use certificate security or not
		/// </summary>
		[Obsolete("This property is no longer used, check AllowedSslErrors instead")]
		[JsonIgnore]
		public bool CheckCertificate { get; set; } = true;
		/// <summary>
		/// The full IP address
		/// </summary>
		[JsonIgnore]
		public string FullIP { get => (IsValid ? IP + (0 != Port ? ":" + Port : default) : default); }
		/// <summary>
		/// Connection timer specified in SECONDS
		/// </summary>
		public int ConnectTimeout
		{
			get => _connecttimeout;
			set
			{
				if (NO_TIMEOUT >= value)
					_connecttimeout = NO_TIMEOUT;
				else
					_connecttimeout = value;
			}
		}
		private int _connecttimeout = DEFAULT_CONNECT_TIMEOUT;
		public const int DEFAULT_CONNECT_TIMEOUT = 5; // 5 second
		#endregion

		#region private properties
		/// <summary>
		/// IP address to target
		/// </summary>
		internal IPAddress Address { get; private set; } = default;
		/// <summary>
		/// IP end point to target
		/// </summary>
		internal IPEndPoint EndPoint { get; private set; } = default;
		#endregion

		#region methods
		public override string ToString()
		{
			return $"Endpoint: {EndPoint}, Servername: {ServerName}, AllowedSslErrors: {AllowedSslErrors}, " + base.ToString();
		}
		/// <summary>
		/// Tells whether an IP is found on DNS or not
		/// </summary>
		/// <param name="ip">It must be a valid IP address (v4 or v6) WITHOUT the port</param>
		/// <returns>
		/// True if found, false otherwise (or invalid address)
		/// </returns>
		public static bool CanBeFoundOnDNS(string ip)
		{
			try
			{
				IPHostEntry ipHost = Dns.GetHostEntry(ip);
				return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Set the TCP/IP address to use
		/// </summary>
		/// <param name="ip">IP or URL to reach</param>
		/// <param name="port">Port to use on this IP</param>
		/// <returns>TRUE if the IP has been set, FALSE otherwise</returns>
		private bool SetIP(string ip, uint port = DEFAULT_PORT)
		{
			if (!ip.IsNullOrEmpty())
				try
				{
					// if localhost is requested get its address
					if (0 == string.Compare(ip, "localhost", true)) ip = CStream.Localhost();

					try
					{
						Address = IPAddress.Parse(ip);
						EndPoint = new IPEndPoint(Address, (int)port);
						return true;
					}
					catch (Exception ex)
					{
					}
					return false;
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(new Exception("Invalid IP address: " + ip + (0 < port ? $":{port}" : string.Empty), ex));
					Address = default;
					EndPoint = default;
				}
			return false;
		}
		#endregion
	}

	[ComVisible(true)]
	[Guid("F4BC72B2-4375-4723-B59D-809182C8CFDE")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IStreamServerSettings
	{
		#region IStreamServerSettings
		[DispId(1)]
		bool IsValid { get; }
		[DispId(2)]
		uint Port { get; set; }
		[DispId(4)]
		X509Certificate ServerCertificate { get; set; }
		[DispId(8)]
		SslPolicyErrors AllowedSslErrors { get; set; }

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
		[DispId(1009)]
		CStreamDelegates.ClientServerOnMessageToLog OnMessageToLog { get; set; }
		[DispId(1010)]
		string LocalhostV6 { get; }
		#endregion
	}
	[Guid("EF1D0636-72B9-4F21-98A2-6F0EE3B048B5")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CStreamServerSettings : CStreamSettings, IStreamServerSettings
	{
		#region constructors
		public CStreamServerSettings() { }
		public CStreamServerSettings(int lengthBufferSize) : base(lengthBufferSize) { }
		public CStreamServerSettings(uint port) { Port = port; }
		public CStreamServerSettings(int lengthBufferSize, uint port) : base(lengthBufferSize) { Port = port; }
		#endregion

		#region properties
		[JsonIgnore]
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
		/// The SSL certificate to use to authenticate the server
		/// </summary>
		public X509Certificate ServerCertificate
		{
			get => _servercertificate;
			set
			{
				_servercertificate = value;
				UseSsl = (default != ServerCertificate);
			}
		}
		private X509Certificate _servercertificate = default;
		#endregion
	}
}
