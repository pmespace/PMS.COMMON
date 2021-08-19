using System;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	public class EInvalidFormat : Exception { public EInvalidFormat(string s) : base($"{s} is an invalid format") { } }
}