using COMMON.Properties;
using System;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	public class EOutOfRange : Exception { public EOutOfRange(string s) : base(Resources.CExceptionOutOfRange.Format(s)) { } }
}