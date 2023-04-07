using System;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	public class EOutOfRange : Exception { public EOutOfRange(string s) : base($"{s} is out of range") { } }
}