using System;
using System.Runtime.InteropServices;

namespace COMMON.WIN32
{
	[ComVisible(false)]
	public static class Win32
	{
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr CreateWaitableTimer(IntPtr lpTimerAttributes, bool bManualReset, string lpTimerName);
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SetWaitableTimer(IntPtr hTimer, [In] ref long pDueTime, int lPeriod, TimerCompleteDelegate pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, bool fResume);
		public delegate void TimerCompleteDelegate();
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
		public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, SWL nIndex, IntPtr dw);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "SetWindowLong")]
		public static extern int SetWindowLong32(IntPtr hWnd, SWL nIndex, IntPtr newLong);

		/// <summary>
		/// Set a window property (refer to Platform SDK)
		/// </summary>
		/// <param name="hWnd">Window to set the pointer</param>
		/// <param name="nIndex">Type of data to set</param>
		/// <param name="newLong">Value to set</param>
		/// <returns></returns>
		public static IntPtr SetWindowLongPtr(IntPtr hWnd, SWL nIndex, IntPtr newLong)
		{
			try
			{
				if (IntPtr.Size == 8)
					return SetWindowLongPtr64(hWnd, nIndex, newLong);
				else
					return new IntPtr(SetWindowLong32(hWnd, nIndex, newLong));
			}
			catch (Exception) { }
			return IntPtr.Zero;
		}

		public enum SWL
		{
			GWL_EXSTYLE = -20,
			GWLP_HINSTANCE = -6,
			GWLP_HWNDPARENT = -8,
			GWL_ID = -12,
			GWL_STYLE = -16,
			GWL_USERDATA = -21,
			GWL_WNDPROC = -4,
			DWLP_USER = 0x08,
			DWLP_MSGRESULT = 0x00,
			DWLP_DLGPROC = 0x04,
		}

		// WM_USER, starting point of user defined messages
		public const int WM_USER = 0x0400;
		public static int WMUser { get => WM_USER; }
		public const int WM_USER_MAX = 0x7FFF;
		public static int WMUserMax { get => WM_USER_MAX; }
		public static bool IsValidWMUser(int value) { return WM_USER <= value && WM_USER_MAX >= value; }

		// WM_APP, starting point of application defined messages
		public const int WM_APP = 0x8000;
		public static int WMApp { get => WM_APP; }
		public const int WM_APP_MAX = 0xBFFF;
		public static int WMAppMax { get => WM_APP_MAX; }
		public static bool IsValidWMApp(int value) { return WM_APP <= value && WM_APP_MAX >= value; }
	}
}
