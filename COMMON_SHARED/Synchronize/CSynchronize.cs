using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace COMMON
{
	[ComVisible(true)]
	[Guid("0D6D4011-3542-4192-97B8-7B67358F6DBE")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISynchronize
	{
		[DispId(101)]
		bool WaitAll(int timer = Timeout.Infinite);
		[DispId(102)]
		int WaitAny(int timer = Timeout.Infinite);
		[DispId(103)]
		bool Reset();
	}
	/// <summary>
	/// Class used to manage started and stopped flags of a thread
	/// If an application needs to know whether a thread is still on it can use these methods
	/// </summary>
	[Guid("C6E23741-18B2-4770-AF0F-D654B98562FF")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CSynchronize : CSafeList<ManualResetEvent>, ISynchronize
	{
		WaitHandle[] waitHandles
		{
			get
			{
				WaitHandle[] w = new WaitHandle[Count];
				for (int i = 0; i < Count; i++)
					w[i] = this[i];
				return w;
			}
		}
		public int WaitAny(int timer = Timeout.Infinite) { return ManualResetEvent.WaitAny(waitHandles, timer); }
		public bool WaitAll(int timer = Timeout.Infinite) { return ManualResetEvent.WaitAll(waitHandles, timer); }
		public bool Reset()
		{
			bool ok = true;
			for (int i = 0; i < Count; i++)
				ok &= this[i].Reset();
			return ok;
		}
	}
}
