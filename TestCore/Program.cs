using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using COMMON;

namespace TestCore
{
	class Program
	{
		static void Main(string[] args)
		{
			CLog.UseGMT = false;
			CLog.UseLocal = true;
			CLog.LogFileName = "console";
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
			Console.WriteLine($"Option value: {CMisc.SearchInArgs(args, "?", out int index, 1)}; Index: {index}");
			Console.WriteLine($"Option value: {CMisc.SearchInArgs(args, "?", out index, 2)}; Index: {index}");
			Console.WriteLine($"Option value: {CMisc.SearchInArgs(args, "?", out index, 3)}; Index: {index}");
			Console.WriteLine($"Option value: {CMisc.SearchInArgs(args, "?", out index, 4)}; Index: {index}");
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
