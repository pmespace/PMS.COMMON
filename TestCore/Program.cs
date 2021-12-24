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
	}
}
