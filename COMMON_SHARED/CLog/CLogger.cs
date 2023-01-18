using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	[ComVisible(false)]
	public static class CLogger
	{
		public static string DEBUG(string s = default, bool display = true) { return Add(CLog.DEBUG(s), display, default); }
		public static string INFORMATION(string s = default, bool display = true) { return Add(CLog.INFORMATION(s), display, default); }
		public static string TRACE(string s = default, bool display = true) { return Add(CLog.TRACE(s), display, default); }
		public static string WARNING(string s = default, bool display = true) { return Add(CLog.WARNING(s), display, CMisc.WarningColors); ; }
		public static string ERROR(string s = default, bool display = true) { return Add(CLog.ERROR(s), display, CMisc.ErrorColors); }
		public static string EXCEPT(Exception ex, string s = default, bool display = true) { return Add(CLog.EXCEPT(ex, s), display, CMisc.ExceptColors); }
		static string Add(string text, bool display, CColors colors)
		{
			if (text.IsNullOrEmpty()) return default;
			if (display) return Display(text, colors);
			return text;
		}
		public static string Display(string text, CColors colors = default)
		{
			CMisc.TextColors = colors;
			CMisc.TextColors.Apply();
			Console.WriteLine(text);
			CMisc.DefaultColors.Apply();
			return text;
		}
	}
}
