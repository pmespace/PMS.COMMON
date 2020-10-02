using System.Runtime.InteropServices;
using System;

namespace COMMON
	{
	/// <summary>
	/// Create an exception to raise when a method is not implemented
	/// </summary>
	[ComVisible(false)]
	public class CNotImplemented: Exception
		{
		/// <summary>
		/// Details the exception
		/// </summary>
		/// <param name="methodename">Not implemented method which was called</param>
		/// <param name="objectname">Object not having implemented the method</param>
		public CNotImplemented(string methodename, string objectname)
		: base("Not implemented method: " + methodename + " - Inside object: " + objectname) { }
		}
	}
