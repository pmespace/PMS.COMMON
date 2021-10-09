using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Threading;

#if NETFRAMEWORK
using COMMON.WIN32;
#endif

namespace COMMON
{
	[ComVisible(true)]
	[Guid("CD4AB05D-ED37-493A-B658-C4D5D86B6864")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IThreadData
	{
		#region IThreadData

#if NETFRAMEWORK
		[DispId(1)]
		IntPtr WindowToWarn { get; set; }
		[DispId(2)]
		uint StoppedMessage { get; set; }
		[DispId(3)]
		uint InformationMessage { get; set; }
		[DispId(10)]
		bool IsValid { get; }
		[DispId(50)]
		uint WMThreadStopped { get; }
		[DispId(51)]
		uint WMThreadInformation { get; }
#endif

		[DispId(60)]
		EventWaitHandle EventToSignal { get; set; }
		#endregion
	}
	[Guid("A6DA1EAA-A706-4D89-A790-B34710EB2818")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CThreadData : IThreadData
	{
		#region constructor
		public CThreadData() { }
		public CThreadData(CThreadData t)
		{
			Assign(t);
		}
		private void Assign(CThreadData t)
		{
#if NETFRAMEWORK
			WindowToWarn = t.WindowToWarn;
			StoppedMessage = t.StoppedMessage;
			InformationMessage = t.InformationMessage;
#endif
		}
		#endregion

		#region delegates
		/// <summary>
		/// Function called from inside the thread when the thread ends.
		/// </summary>
		/// <param name="id">This is the <see cref="CThread.ID"/> value given to the thread by the calling application</param>
		/// <param name="name">This is the <see cref="CThread.Name"/> value given to the thread by the  calling application</param>
		/// <param name="uniqueId">This is the <see cref="CThread.UniqueID"/> value given to the thread by the system itself</param>
		/// <param name="result">This is the <see cref="CThread.Result"/> of the thread</param>
		public delegate void ThreadTerminates(int id, string name, int uniqueId, int result);
		#endregion

		#region properties
		public bool IsValid { get => true; }

#if NETFRAMEWORK
		/// <summary>
		/// Message sent (by PostMessage) to the caller when the thread has stopped.
		/// </summary>
		public uint StoppedMessage
		{
			get => _threadstoppedmessage;
			set => _threadstoppedmessage = Win32.IsValidWM(value) ? value : _threadstoppedmessage;
		}
		private uint _threadstoppedmessage = WM_THREAD_STOPPED;
		public const uint WM_THREAD_STOPPED = 0x666;
		public uint WMThreadStopped { get => WM_THREAD_STOPPED; }
		/// <summary>
		/// Message sent (by PostMessage) to the caller when the thread needs to inform of a situation.
		/// </summary>
		public uint InformationMessage
		{
			get => _informationmessage;
			set => _informationmessage = Win32.IsValidWM(value) ? value : _informationmessage;
		}
		private uint _informationmessage = WM_THREAD_INFORMATION;
		public const uint WM_THREAD_INFORMATION = WM_THREAD_STOPPED + 1;
		public uint WMThreadInformation { get => WM_THREAD_INFORMATION; }
		/// <summary>
		/// Handle of window to warn when the thread terminates
		/// </summary>
		public IntPtr WindowToWarn
		{
			get => _windowtowarn;
			set => _windowtowarn = value;
		}
		private IntPtr _windowtowarn = IntPtr.Zero;
#endif

		/// <summary>
		/// Event which will be signaled when the thread terminates
		/// </summary>
		public EventWaitHandle EventToSignal
		{
			get => _eventtosignal;
			set => _eventtosignal = value;
		}
		private EventWaitHandle _eventtosignal = null;
		/// <summary>
		/// Function that will be called from inside the thread when it terminates
		/// This is the last action inside the thread
		/// </summary>
		public ThreadTerminates OnTerminates { get; set; }
		#endregion

		#region methods
		/// <summary>
		/// Returns the content of the class
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
#if NETFRAMEWORK
			string SEP = " - ";
			return "Window to warn: " + (null != WindowToWarn).ToString() + SEP
				+ (null != WindowToWarn ? "Stopped message: " + StoppedMessage + SEP : null)
				+ (null != WindowToWarn ? "Information message: " + InformationMessage + SEP : null);
#else
			return base.ToString();
#endif
		}
		#endregion
	}
}
