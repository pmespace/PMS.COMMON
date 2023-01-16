using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	[ComVisible(false)]
	public static class CLogger
	{
		public static CColors WarningColor { get; set; } = new CColors() { Foreground = ConsoleColor.DarkYellow };
		public static CColors ErrorColor { get; set; } = new CColors() { Foreground = ConsoleColor.DarkRed };
		public static CColors ExceptColor { get; set; } = new CColors() { Foreground = ConsoleColor.Red };
		public static string DEBUG(string s = default, bool display = true) { return Add(CLog.DEBUG(s), display, default); }
		public static string INFORMATION(string s = default, bool display = true) { return Add(CLog.INFORMATION(s), display, default); }
		public static string TRACE(string s = default, bool display = true) { return Add(CLog.TRACE(s), display, default); }
		public static string WARNING(string s = default, bool display = true) { return Add(CLog.WARNING(s), display, WarningColor); ; }
		public static string ERROR(string s = default, bool display = true) { return Add(CLog.ERROR(s), display, ErrorColor); }
		public static string EXCEPT(Exception ex, string s = default, bool display = true) { return Add(CLog.EXCEPT(ex, s), display, ExceptColor); }
		static string Add(string text, bool display, CColors colors)
		{
			if (text.IsNullOrEmpty()) return default;
			if (display) return Display(text, colors);
			return text;
		}
		public static string Display(string text, CColors colors = default)
		{
			CMisc.ConsoleColors.Text = colors;
			CMisc.ConsoleColors.ApplyTextColors();
			Console.WriteLine(text);
			CMisc.ConsoleColors.ResetTextColors();
			return text;
		}
	}
}
