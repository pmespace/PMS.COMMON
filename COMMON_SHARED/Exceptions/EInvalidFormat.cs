using COMMON.Properties;
using System;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	public class EInvalidFormat : Exception
	{
		public EInvalidFormat(string s) : base(s) { } // Resources.CExceptionInvalidFormat.Format(s)) { }
	}
}