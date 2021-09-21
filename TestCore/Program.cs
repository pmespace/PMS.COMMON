using System;
using COMMON;

namespace TestCore
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.ReadKey(true);
			Console.WriteLine("Hello World!");
			CLog.LogFileName = "test.core.log";
			CLog.Add("test");
		}
	}
}
