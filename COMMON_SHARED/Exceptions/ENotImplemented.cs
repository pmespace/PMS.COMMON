using System.Runtime.InteropServices;
using System;

namespace COMMON
{
	public class ENotImplemented : Exception
	{
		/// <summary>
		/// Details the exception
		/// </summary>
		/// <param name="methodename">Not implemented method which was called</param>
		/// <param name="objectname">Object not having implemented the method</param>
		public ENotImplemented(string methodename, string objectname) : base("Not implemented method: " + methodename + " - Inside object: " + objectname) { }
	}
}
