using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows;

namespace COMMON.WIN32
{
	[ComVisible(false)]
	public static class Win32
	{
		/// <summary>
		/// WinAPI ShellExecute
		/// </summary>
		/// <param name="hwnd">Handle to a parent window.</param>
		/// <param name="lpOperation">Pointer to a null-terminated string, referred to in this case as a verb, that specifies the action to be performed.</param>
		/// <param name="lpFile">Pointer to a null-terminated string that specifies the file or object on which to execute the specified verb.</param>
		/// <param name="lpParameters">If the lpFile parameter specifies an executable file, lpParameters is a pointer to a null-terminated string that specifies the parameters to be passed to the application.</param>
		/// <param name="lpDirectory">Pointer to a null-terminated string that specifies the default directory.</param>
		/// <param name="nShowCmd">Flags that specify how an application is to be displayed when it is opened.</param>
		/// <returns></returns>
		[DllImport("shell32.dll")]
		public static extern IntPtr ShellExecute
			(
			IntPtr hwnd,
			[MarshalAs(UnmanagedType.LPTStr)]
			String lpOperation,
			[MarshalAs(UnmanagedType.LPTStr)]
			String lpFile,
			[MarshalAs(UnmanagedType.LPTStr)]
			String lpParameters,
			[MarshalAs(UnmanagedType.LPTStr)]
			String lpDirectory,
			Int32 nShowCmd
			);
		public const int SW_HIDE = 0;
		public const int SW_SHOWNORMAL = 1;
		public const int SW_SHOWMINIMIZED = 2;
		public const int SW_SHOWMAXIMIZED = 3;
		public const int SW_SHOWNOACTIVATE = 4;
		public const int SW_SHOW = 5;
		public const int SW_MINIMIZE = 6;
		public const int SW_SHOWMINNOACTIVE = 7;
		public const int SW_SHOWNA = 8;
		public const int SW_RESTORE = 9;
		public const int SW_SHOWDEFAULT = 10;
		public const int SW_FORCEMINIMIZE = 11;

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
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "GetDesktopWindow")]
		public static extern IntPtr GetDesktopWindow();
		[DllImport("user32.dll")]
		private static extern long LockWindowUpdate(IntPtr Handle);

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
		/// <summary>
		/// Locks updates on the form
		/// </summary>
		/// <param name="handle"><see cref="Form"/> object</param>
		public static void LockWindowUpdate(Form handle) => LockWindowUpdate(handle.Handle);
		/// <summary>
		/// Locks updates on the control
		/// </summary>
		/// <param name="handle"><see cref="Control"/> object</param>
		public static void LockWindowUpdate(Control handle) => LockWindowUpdate(handle.Handle);
		/// <summary>
		/// Unlocks updates
		/// </summary>
		public static void UnlockWindowUpdate() => LockWindowUpdate(IntPtr.Zero);

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
		public static bool IsValidWMUser(int value) => WM_USER <= value && WM_USER_MAX >= value;

		// WM_APP, starting point of application defined messages
		public const int WM_APP = 0x8000;
		public static int WMApp { get => WM_APP; }
		public const int WM_APP_MAX = 0xBFFF;
		public static int WMAppMax { get => WM_APP_MAX; }
		public static bool IsValidWMApp(int value) => WM_APP <= value && WM_APP_MAX >= value;

		public static IntPtr HWND_DESKTOP { get => GetDesktopWindow(); }
	}
}
