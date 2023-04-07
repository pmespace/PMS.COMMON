using System;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	public class EDisconnected : Exception { public EDisconnected() : base("Stream has been disconnected") { } }
}
