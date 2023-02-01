using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using COMMON;
using Newtonsoft.Json.Serialization;
using System.Threading;

namespace TestCore
{
	internal class TestCore
	{
		#region types
		private delegate bool DFnc(char c);
		class CMenu
		{
			public string Text { get; set; }
			public DFnc Fnc { get; set; }
		}
		#endregion

		#region constants
		const string USE_SSL = "Use SSL";
		const string DO_NOT_USE_SSL = "Do not use SSL";

		const string USE_LOCAL_HOST = "Use localhost";
		const string DO_NOT_USE_LOCAL_HOST = "Do not use localhost";
		#endregion

		#region properties
		private SortedDictionary<char, CMenu> Menu = new SortedDictionary<char, CMenu>();
		//private Dictionary<ConsoleKey, string> Choice = new Dictionary<ConsoleKey, string>();
		private uint Port = 29134;
		private bool UseSSL = false;
		CStreamServer server = null;
		#endregion

		#region classes
		class MySafeList : CSafeList<string> { }
		class MySafeDict : CSafeDictionary<string, object> { }
		#endregion

		#region main method
		public int Start(string[] args)
		{
			//Console.ForegroundColor = ConsoleColor.Black;
			//Console.BackgroundColor = ConsoleColor.White;
#if COLORS
			CMisc.ResetColors();
			CMisc.InputColors.Background = ConsoleColor.Black;

			CMisc.Input("hello", default, out bool isdefx, "invite");

			CMisc.YesNo("hello", true, true, true);
#endif


			Func<string, uint, bool> ddd = (string _addr_, uint _port_) =>
			{
				CStreamClientSettings _q_;
				DateTime dt1 = DateTime.Now;
				if (_addr_.IsNullOrEmpty() && 0 == _port_)
					_q_ = new CStreamClientSettings();
				else if (_addr_.IsNullOrEmpty())
					_q_ = new CStreamClientSettings() { Port = _port_ };
				else if (0 == _port_)
					_q_ = new CStreamClientSettings() { IP = _addr_ };
				else
					_q_ = new CStreamClientSettings() { IP = _addr_, Port = 2018 };
				DateTime dt2 = DateTime.Now;
				TimeSpan ts = dt2.Subtract(dt1);
				Console.WriteLine(CLog.TRACE($"duration: {ts} [{_addr_}, {_port_}] {_q_}"));
				return true;
			};

			CLog.LogFileName = "testcore.log";
			//CLog.SharedGuidClear();
			CLog.SharedGuid = null;
			for (int i = 0; i <= 5; i++) CLog.TRACE("Step1 " + i);
			//CLog.SharedGuid(Guid.NewGuid());
			CLog.SharedGuid = Guid.NewGuid();
			CLog.SharedContext = "Context1";
			for (int i = 0; i <= 5; i++) CLog.TRACE("Step2 " + i);
			//CLog.SharedGuidClear();
			CLog.SharedGuid = null;
			CLog.SharedContext = "Context2";
			for (int i = 0; i <= 5; i++) CLog.TRACE("Step3 " + i);
			//CLog.SharedGuid(Guid.NewGuid());
			CLog.SharedGuid = Guid.NewGuid();
			CLog.SharedContext = default;
			for (int i = 0; i <= 5; i++) CLog.TRACE("Step4 " + i);

			CLogger.EXCEPT(new Exception("test except"));

			for (int i = 0; i <= 5; i++) CLog.TRACE("Step5 " + i);

			Guid? guid;
			ConsoleKeyInfo keyInfo;
			do
			{
				guid = CLog.SharedGuid;
				ddd(default, 0);
				guid = CLog.SharedGuid;
				ddd("localhost", 0);
				guid = CLog.SharedGuid;
				ddd("localhost", 2018);
				ddd("192.168.0.225", 0);
				ddd("192.168.0.225", 2018);
				ddd("192.168.0.225", 9999);
				ddd("192.168.0.137", 0);
				CLog.SharedGuid = null;
				ddd("192.168.0.137", 2018);

				Console.WriteLine("Sleeping...");
				Thread.Sleep(1000);

				ddd(default, 0);
				ddd("localhost", 0);
				ddd("localhost", 2018);
				CLog.SharedGuid = Guid.NewGuid();
				ddd("192.168.0.225", 0);
				ddd("192.168.0.225", 2018);
				ddd("192.168.0.225", 9999);
				ddd("192.168.0.137", 0);
				ddd("192.168.0.137", 2018);

				Console.WriteLine("Press a key or ESC");
#if COLORS
				CMisc.InputColors.Apply();
#endif
				keyInfo = Console.ReadKey(true);
			} while (keyInfo.Key != ConsoleKey.Escape);


			string unique = default;
			string tmpf = CMisc.GetTempFileName(out string path, out string fname, ref unique, null, null, "json");
			tmpf = CMisc.GetTempFileName(out path, out fname, ref unique);
			unique = null; tmpf = CMisc.GetTempFileName(out path, out fname, ref unique, null, null, ".json", false, "*");
			tmpf = CMisc.GetTempFileName(out path, out fname, ref unique, "hello", "hella", ".json", true, "_");
			unique = null; tmpf = CMisc.GetTempFileName(out path, out fname, ref unique, "hello", "hella", ".json", true, "_");

			string dir = CMisc.VerifyDirectory(".", false);
			dir = CMisc.VerifyDirectory(".", true);
			dir = CMisc.VerifyDirectory("", false);
			dir = CMisc.VerifyDirectory("", true);
			dir = CMisc.VerifyDirectory(@"..\net47\testcommon.exe", false);
			dir = CMisc.VerifyDirectory(@".\testcommon.exe", true);
			dir = CMisc.VerifyDirectory(@"c:\testcommon.exe", false);
			dir = CMisc.VerifyDirectory(@"c:\testcommon.exe", true);

#if COLORS
			CMisc.InputColors.Foreground = ConsoleColor.Cyan;
			CMisc.InputColors.Background = ConsoleColor.DarkYellow;
			CMisc.TextColors.Foreground = ConsoleColor.Magenta;
#endif

			CMisc.Input("hello", default, out bool isdef, "invite");


			MySafeDict hh = new MySafeDict();
			hh.Add("456", new object());
			hh.Add("123", new object());
			object o = hh["123"];
			o = hh["789"];
			string json = hh.ToJson();
			var hhr = hh.ToArray();

			MySafeList ll = new MySafeList();
			ll.Add("456");
			ll.Add("123");
			string ls = ll[0];
			ls = ll[255];
			ll.Insert("wopa", 255);
			ls = ll[255];
			ls = ll[2];
			json = ll.ToJson();
			var llr = ll.ToArray();

			//string[] yes1 = { "OUI", "O" };
			//string[] yes2 = { "123", "3" };
			//string[] no1 = { "123", "3" };
			//string[] no2 = { "Non", "N" };
			//Console.WriteLine(CMisc.YesNo("useDeult/YES/no ESC", true, true, false, yes1, no1, true) ? "YES" : "NO");
			//Console.WriteLine(CMisc.YesNo("not useDeult/YES/ESC", true, true, true, yes2, no2, true) ? "YES" : "NO");
			//Console.WriteLine(CMisc.YesNo("not useDeult/YES/ESC", true, true, true, yes2, null, true) ? "YES" : "NO");
			//Console.WriteLine(CMisc.YesNo("useDeult/YES/no ESC", true, true, false, null, no2, true) ? "YES" : "NO");
			//Console.WriteLine(CMisc.YesNo("not useDeult/YES/ESC", true, true, false, yes2, null) ? "YES" : "NO");
			//Console.WriteLine(CMisc.YesNo("useDeult/YES/no ESC", true, true, false, null, no2) ? "YES" : "NO");


			Menu.Add('1', new CMenu() { Text = "Start server", Fnc = StartServer });
			Menu.Add('2', new CMenu() { Text = "Stop server", Fnc = StopServer });
			Menu.Add('3', new CMenu() { Text = "Send data", Fnc = SendData });
			Menu.Add('4', new CMenu() { Text = "Use SSL", Fnc = SetUseSSL });
			Menu.Add('5', new CMenu() { Text = "Create log", Fnc = CreateLog });
			Menu.Add('6', new CMenu() { Text = "Stop log", Fnc = StopLog });
			Menu.Add('7', new CMenu() { Text = "Stop log", Fnc = TestConnect });
			Menu.Add('8', new CMenu() { Text = "Server statistics", Fnc = ServerStatistics });
			Menu.Add('X', new CMenu() { Text = "Exit", Fnc = Exit });

			bool ok = true;
			while (ok)
			{
				DFnc fnc = DisplayMenu(out char c);
				ok = fnc(c);
			}
			StopServer('2');
#if COLORS
			CMisc.DefaultColors.Apply();
#endif
			return 0;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private DFnc DisplayMenu(out char c)
		{
#if COLORS
			CMisc.TextColors.Apply();
#endif
			Console.WriteLine("");
			foreach (KeyValuePair<char, CMenu> m in Menu)
			{
#if COLORS
				CColors.Apply(ConsoleColor.White, null);
#endif
				Console.Write($"{m.Key}");
#if COLORS
				CMisc.TextColors.Apply();
#endif
				Console.WriteLine($"/ {m.Value.Text}");
			}
			do
			{
#if COLORS
				CMisc.InputColors.Apply();
#endif
				ConsoleKeyInfo keyInfo = Console.ReadKey(true);
				c = keyInfo.KeyChar.ToString().ToUpper()[0];
			} while (!Menu.ContainsKey(c));

			//CMisc.Choice(new Dictionary<ConsoleKey, string>() { })
#if COLORS
			CMisc.DefaultColors.Apply();
#endif
			return Menu[c].Fnc;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="tcpclient"></param>
		/// <param name="thread"></param>
		/// <param name="parameters"></param>
		/// <param name="privateData"></param>
		/// <returns></returns>
		bool ServerOnConnect(TcpClient tcpclient, CThread thread, object parameters, ref object privateData)
		{
			privateData = new ServerClass();
			(privateData as ServerClass).DT = DateTime.Now;
			return true;
		}
		class ServerClass
		{
			public DateTime DT { get; set; }
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="client"></param>
		/// <param name="request"></param>
		/// <param name="addBufferSize"></param>
		/// <param name="thread"></param>
		/// <param name="parameters"></param>
		/// <param name="serverclient"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		byte[] ServerOnMessage(TcpClient client, byte[] request, out bool addBufferSize, CThread thread, object parameters, object privateData, object o)
		{
			addBufferSize = true;
			Console.WriteLine($"SERVER (connected at {(privateData as ServerClass).DT}) RECEIVED: {Encoding.UTF8.GetString(request)}");
			//return Encoding.UTF8.GetBytes("That's fine");
			server.Send1WayNotification(Encoding.UTF8.GetBytes("That's fine"), addBufferSize, "TEST", o);
			return null;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		bool SetUseSSL(char c)
		{
			UseSSL = !UseSSL;
			Console.WriteLine(UseSSL ? "Using SSL" : "Not using SSL");
			return true;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		bool CreateLog(char c)
		{
			CLog.LogFileName = $"test.core.txt";
			CLog.Add($"Starting log with {CLog.LogFileName}");
			return true;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		bool StopLog(char c)
		{
			CLog.LogFileName = null;
			CLog.Add($"Stopping log");
			return true;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		bool TestConnect(char c)
		{
			CStreamClientSettings settings = new CStreamClientSettings()
			{
				AllowedSslErrors = SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors,
				IP = CStream.Localhost(),
				Port = Port,
				ServerName = UseSSL ? "hello world" : null,
				ConnectTimeout = 10,
			};
			CStreamClientIO clientIO = CStream.Connect(settings);
			if (null != clientIO)
			{
				CStream.Send(clientIO, "hello");
#if COLORS
				CMisc.InputColors.Apply();
#endif
				Console.ReadKey();
#if COLORS
				CMisc.TextColors.Apply();
#endif
				Console.WriteLine($"Client connected: {clientIO.Connected}");
#if COLORS
				CMisc.InputColors.Apply();
#endif
				Console.ReadKey();
				StopServer(c);
#if COLORS
				CMisc.InputColors.Apply();
#endif
				Console.ReadKey();
#if COLORS
				CMisc.TextColors.Apply();
#endif
				Console.WriteLine($"Client connected: {clientIO.Connected}");
				CStream.Disconnect(clientIO);
			}
			return true;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		bool ServerStatistics(char c)
		{
			if (null != server)
			{
#if COLORS
				CMisc.TextColors.Apply();
#endif
				Console.WriteLine(server.Statistics());
			}
			return true;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		bool SendData(char c)
		{
			CStreamClientSettings settings = new CStreamClientSettings()
			{
				AllowedSslErrors = SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors,
				IP = CStream.Localhost(),
				Port = Port,
				ServerName = UseSSL ? "hello world" : null,
				ConnectTimeout = 10,
			};
#if COLORS
			CMisc.TextColors.Apply();
#endif
			Console.Write("Message to send: ");
#if COLORS
			CMisc.InputColors.Apply();
#endif
			string request = Console.ReadLine();
			string reply = null;
			if (null != (reply = CStream.ConnectSendReceive(settings, string.IsNullOrEmpty(request) ? DateTime.Now.ToString() : request, out int size)))
				Console.WriteLine("CLIENT RECEIVED: " + reply);
			else
				Console.WriteLine("ERROR RECEIVING DATA");
			return true;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		bool Exit(char c)
		{
			return !CMisc.YesNo("Exit", true, false, true);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		bool StartServer(char c)
		{
			string pwd = null;
			if (UseSSL)
			{
				pwd = CMisc.Input("Password", null, out bool isdef, "Key password =");
				if (string.IsNullOrEmpty(pwd)) return true;
			}

			string s = "[" + (UseSSL ? "using SSL" : "not using SSL") + "]";
			if (null != server)
			{
				Console.WriteLine("Server is running already" + s);
				return true;
			}
			CStreamServerSettings serverSettings = new CStreamServerSettings()
			{
				Port = Port,
				// important in this case to allow a certificate mismatch as MY CERTIFICATE MISMATCHES
				AllowedSslErrors = SslPolicyErrors.RemoteCertificateNotAvailable,// | SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors,
				ServerCertificate = UseSSL ? new X509Certificate2(@"C:\Users\philippe\Documents\Dev\Certificates\PMS.COMMON.SSL.pfx", pwd) : null,
			};
			CStreamServerStartSettings startSettings = new CStreamServerStartSettings()
			{
				OnConnect = ServerOnConnect,
				OnMessage = ServerOnMessage,
				StreamServerSettings = serverSettings,
			};
			server = new CStreamServer();
			if (server.StartServer(startSettings))
			{
				Console.WriteLine("Server is running" + s);
				return true;
			}
			Console.WriteLine("Server failed to start");
			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		bool StopServer(char c)
		{
			if (null != server)
			{
				server.StopServer();
				Console.WriteLine(server.Statistics());
			}
			server = null;
			return true;
		}
		#endregion
	}
}
