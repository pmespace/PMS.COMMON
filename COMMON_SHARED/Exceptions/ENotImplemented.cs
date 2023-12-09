using System.Runtime.InteropServices;
using System;
using COMMON.Properties;

namespace COMMON
{
	public class ENotImplemented : Exception
	{
		/// <summary>
		/// Details the exception
		/// </summary>
		/// <param name="methodename">Not implemented method which was called</param>
		/// <param name="objectname">Object not having implemented the method</param>
		public ENotImplemented(string methodename, string objectname) : base(Resources.CExceptionNotImplemented.Format(new object[] { methodename, objectname })) { }
	}
}
