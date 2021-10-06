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
			t.Start(args);
		}
	}
}
