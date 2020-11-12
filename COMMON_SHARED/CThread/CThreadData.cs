using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Threading;

namespace COMMON
{
	[Guid("CD4AB05D-ED37-493A-B658-C4D5D86B6864")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[ComVisible(true)]
	public interface IThreadData
	{
		#region IThreadData
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
		[DispId(60)]
		EventWaitHandle EventToSignal { get; set; }
		#endregion
	}
	[Guid("A6DA1EAA-A706-4D89-A790-B34710EB2818")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CThreadData: IThreadData
	{
		#region constructor
		public CThreadData() { }
		public CThreadData(CThreadData t)
		{
			Assign(t);
		}
		private void Assign(CThreadData t)
		{
			WindowToWarn = t.WindowToWarn;
			StoppedMessage = t.StoppedMessage;
			InformationMessage = t.InformationMessage;
		}
		#endregion

		#region properties
		public bool IsValid { get => true; }
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
		/// Handle of window to wardn when the thread terminates
		/// </summary>
		public IntPtr WindowToWarn
		{
			get => _windowtowarn;
			set => _windowtowarn = value;
		}
		private IntPtr _windowtowarn = IntPtr.Zero;
		/// <summary>
		/// Event which will be signaled when the thread terminates
		/// </summary>
		public EventWaitHandle EventToSignal
		{
			get => _eventtosignal;
			set => _eventtosignal = value;
		}
		private EventWaitHandle _eventtosignal = null;
		#endregion

		#region methods
		/// <summary>
		/// Returns the content of the class
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string SEP = " - ";
			return "Window to warn: " + (null != WindowToWarn).ToString() + SEP
				+ (null != WindowToWarn ? "Stopped message: " + StoppedMessage + SEP : null)
				+ (null != WindowToWarn ? "Information message: " + InformationMessage + SEP : null);
		}
		/// <summary>
		/// Prepare a structure to use
		/// </summary>
		/// <param name="hwnd">window to warn</param>
		/// <param name="threadstopped">message sent to that window when the thread is stopping</param>
		/// <param name="information">message sent to that window when the thread needs to inform of an event</param>
		/// <returns>A <see cref="CThreadData"/> object</returns>
		public static CThreadData Prepare(IntPtr hwnd, uint threadstopped = WM_THREAD_STOPPED, uint information = WM_THREAD_INFORMATION)
		{
			return new CThreadData()
			{
				WindowToWarn = hwnd,
				StoppedMessage = threadstopped,
				InformationMessage = information,
			};
		}
		#endregion
	}
}
