using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	[ComVisible(false)]
	public static class CLogger
	{
		public static string DEBUG(string s = default, bool display = true, Guid? guid = null)
		{
			return Add(CLog.DEBUG(s), display
#if COLORS
				, default);
#else
				);
#endif
		}
		public static string INFORMATION(string s = default, bool display = true, Guid? guid = null)
		{
			return Add(CLog.INFORMATION(s), display
#if COLORS
				, default);
#else
				);
#endif
		}
		public static string TRACE(string s = default, bool display = true, Guid? guid = null)
		{
			return Add(CLog.TRACE(s), display
#if COLORS
				, default);
#else
				);
#endif
		}
		public static string WARNING(string s = default, bool display = true, Guid? guid = null)
		{
			return Add(CLog.WARNING(s), display
#if COLORS
				, CMisc.WarningColors); ;
#else
				);
#endif
		}
		public static string ERROR(string s = default, bool display = true, Guid? guid = null)
		{
			return Add(CLog.ERROR(s), display
#if COLORS
				, CMisc.ErrorColors);
#else
				);
#endif
		}
		public static string EXCEPT(Exception ex, string s = default, bool display = true, Guid? guid = null)
		{
			return Add(CLog.EXCEPT(ex, s), display
#if COLORS
				, CMisc.ExceptColors);
#else
				);
#endif
		}
#if COLORS
static string Add(string text, bool display, CColors colors)
#else
		public static string Add(string text, bool display, Guid? guid = null)
#endif
		{
			if (text.IsNullOrEmpty()) return default;
#if COLORS
			if (display) return Display(text, colors);
#else
			if (display) return Display(text);
#endif
			return text;
		}
#if COLORS
		public static string Display(string text, CColors colors = default)
#else
		public static string Display(string text)
#endif
		{
#if COLORS
			CMisc.TextColors = colors;
			CMisc.TextColors.Apply();
#endif
			Console.WriteLine(text);
#if COLORS
			CMisc.DefaultColors.Apply();
#endif
			return text;
		}
	}
}
