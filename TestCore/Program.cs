using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using PMS.COMMON;

namespace TestCore
{
	class Program
	{
		static void Main(string[] args)
		{
			//decimal d1 = 0;
			//decimal? d2 = null;
			//if (d1 is decimal)
			//	Console.WriteLine("OK");
			//if (d1 is decimal?)
			//	Console.WriteLine("OK");
			//if (d1.GetType() == typeof(decimal))
			//	Console.WriteLine("OK");
			//if (d1.GetType() == typeof(decimal?))
			//	Console.WriteLine("OK");
			//if (d2 is decimal)
			//	Console.WriteLine("OK");
			//if (d2 is decimal?)
			//	Console.WriteLine("OK");
			//if (d2.GetType() == typeof(decimal))
			//	Console.WriteLine("OK");
			//Type ty = typeof(decimal?);
			//if (d2.GetType() == typeof(decimal?))
			//	Console.WriteLine("OK");

			CLog.UseGMT = false;
			CLog.UseLocal = true;
			CLog.Filename = "console";
			CLog.Filename = "console";
			CLog.NumberOfFilesToKeep = 2;
			try
			{
				throw new Exception("test");
			}
			catch (Exception ex)
			{
				Console.WriteLine(CLog.EXCEPT(ex, "voilà"));
			}
			try
			{
				P1();
			}
			catch (Exception ex)
			{
				Console.WriteLine(CLog.EXCEPT(ex, "voilà"));
			}
			CLog.UseGMT = !CLog.UseGMT;
			CLog.SeverityToLog = TLog.WARNG;
			CLog.DEBUG("debug");

			TestCore t = new TestCore();
			byte[] ab = null;
			if (ab.IsNullOrEmpty())
				Console.WriteLine("NULL");
			string s;
			Console.WriteLine($"Option value: {CMisc.SearchInArgs(args, "?", out int index, 1)}{Chars.SEPARATOR}Index: {index}");
			Console.WriteLine($"Option value: {CMisc.SearchInArgs(args, "?", out index, 2)}{Chars.SEPARATOR}Index: {index}");
			Console.WriteLine($"Option value: {CMisc.SearchInArgs(args, "?", out index, 3)}{Chars.SEPARATOR}Index: {index}");
			Console.WriteLine($"Option value: {CMisc.SearchInArgs(args, "?", out index, 4)}{Chars.SEPARATOR}Index: {index}");
			t.Start(args);
		}
		static void P1()
		{
			P2();
		}
		static void P2()
		{
			P3();
		}
		static void P3()
		{
			throw new Exception("test");
		}

	}
}
