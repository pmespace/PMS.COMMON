using System;
using System.Collections.Generic;
using System.Text;
using COMMON.Properties;

namespace COMMON
{
	public class EDisconnected : Exception { public EDisconnected() : base(Resources.CExceptionStreamDisconnected) { } }
}
