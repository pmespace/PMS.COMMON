using System;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	class CDisconnected: Exception
	{
		/// <summary>
		/// Details the exception
		/// </summary>
		public CDisconnected()
		: base("Stream has been disconnected") { }
	}
}
