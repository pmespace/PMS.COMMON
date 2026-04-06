using System;
using System.Collections.Generic;
using System.Text;
using PMS.COMMON.Properties;

namespace PMS.COMMON
{
	public class EDisconnected : Exception { public EDisconnected() : base(Resources.CExceptionStreamDisconnected) { } }
}
