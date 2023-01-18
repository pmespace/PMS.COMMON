using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace COMMON
{
	/// <summary>
	/// Various useful strings
	/// </summary>
	[ComVisible(false)]
	public static class Chars
	{
		public const string CR = "\r";
		public const string CRCR = "\r\r";
		public const string LF = "\n";
		public const string LFLF = "\n\n";
		public const string TAB = "\t";
		public const string TABTAB = "\t\t";
		public const string CRLF = "\r\n";
		public const string LFCR = "\n\r";
		public const string DATE = "yyyy-MM-dd";
		public const string TIME = "HH:mm:ss";
		public const string TIMEEX = "HH:mm:ss.fff";
		public const string DATETIME = DATE + " " + TIME;
		public const string DATETIMEEX = DATE + " " + TIMEEX;
		public const string SEPARATOR = "; ";
		public const string FOLLOWEDBY = " - ";
		public const string SDATE = "yyyyMMdd";
		public const string STIME = "HHmmss";
		public const string STIMEEX = "HHmmssfff";
		public const string SDATETIME = SDATE + STIME;
		public const string SDATETIMEEX = SDATE + STIMEEX;
	}

	[ComVisible(true)]
	public enum TLog
	{
		_begin = -1,
		DEBUG,
		INFOR,
		TRACE,
		WARNG,
		ERROR,
		EXCPT,
		_end
	}

	/// <summary>
	/// Provides an easy to use log service
	/// </summary>
	[ComVisible(false)]
	public static class CLog
	{
		#region constructors
		static CLog()
		{
			//logger = new Logger();
		}
		#endregion

		#region const 
		private const string EXTENSION = ".log";
		/// <summary>
		/// Value to use to indicate no file will be purged
		/// </summary>
		public const int KEEP_ALL_FILES = -1;
		#endregion

		#region properties
		/// <summary>
		/// Indicate whether the LOG system is active or not
		/// </summary>
		public static bool Active { get => !_logfilename.IsNullOrEmpty(); }
		/// <summary>
		/// The date the file was created
		/// </summary>
		public static DateTime CreatedOn { get; private set; } = default;
		/// <summary>
		/// Full name of log file
		/// </summary>
		public static string LogFileName
		{
			get => _logfilename;
			set
			{
				if (_logfilename != value)
				{
					if (value.IsNullOrEmpty())
						CloseLogFile();
					else
						OpenLogFile(value);
				}
			}
		}
		private static string _logfilename = default;
		/// <summary>
		/// Path of the log file, without the log file name.
		/// It always ends with "\" (or any other platform folder separator)
		/// </summary>
		public static string LogFilePath
		{
			get => _logfilepath;
			private set => _logfilepath = (value.IsNullOrEmpty() ? default : value + (Path.DirectorySeparatorChar != value[value.Length - 1] ? new string(Path.DirectorySeparatorChar, 1) : default));
		}
		private static string _logfilepath = default;
		/// <summary>
		/// Indicates whether autopurge previous log file when opening a new one
		/// </summary>
		public static bool AutoPurgeLogFiles { get; set; } = false;
		/// <summary>
		/// Indicates the number of files to keep if <see cref="AutoPurgeLogFiles"/> is set to true
		/// </summary>
		public static int NumberOfFilesToKeep { get; set; } = KEEP_ALL_FILES;
		/// <summary>
		/// Indicates whether CR, LF and other special characters are kept when logging into file
		/// </summary>
		public static bool KeepCRLF { get; set; } = false;
		/// <summary>
		/// The level of severity to log
		/// </summary>
		public static TLog SeverityToLog
		{
			get => _severitytolog;
			set
			{
				lock (mylock)
				{
					_severitytolog = IsTLog(value) ? value : _severitytolog;
				}
			}
		}
		private static TLog _severitytolog = TLog.TRACE;
		/// <summary>
		/// Indicate whether error log must be set to upper or not
		/// </summary>
		public static bool ErrorToUpper { get; set; } = true;
		/// <summary>
		/// Separator to use between multiple lines in a resulting 1 line string
		/// </summary>
		public static string LinesSeparator { get; set; } = " ";// Chars.TAB;
		/// <summary>
		/// Allows indicating whether to always (true) or not (false, thus depending on <see cref="SeverityToLog"/>) DEBUG messages
		/// </summary>
		public static bool AlwaysLogDebug { get; set; } = false;
		/// <summary>
		/// Allows using GMT time inside the log file instead of local time
		/// </summary>
		public static bool UseGMT
		{
			get => _usegmt;
			set
			{
				if (canChangeAllSettings)
					_usegmt = value;
				if (_usegmt)
					_dateFormat = CMisc.DateFormat.GMT;
				else
					_dateFormat = UseLocal ? CMisc.DateFormat.Local : CMisc.DateFormat.YYYYMMDDhhmmssfffEx;
			}
		}
		private static bool _usegmt = false;
		/// <summary>
		/// Allows using GMT time inside the log file instead of local time
		/// </summary>
		public static bool UseLocal
		{
			get => _uselocal;
			set
			{
				if (canChangeAllSettings)
				{
					_uselocal = value;
					UseGMT = UseGMT;
				}
			}
		}
		private static bool _uselocal = false;
		private static CMisc.DateFormat _dateFormat = CMisc.DateFormat.YYYYMMDDhhmmssfffEx;
		#endregion

		#region privates
		/// <summary>
		/// Original fname used to create the log file
		/// </summary>
		private static string originalFName = default;
		/// <summary>
		/// Original name (given by the log file creator) of the log file, without its extension
		/// </summary>
		private static string originalFNameWithoutExtension = default;
		/// <summary>
		/// Original extension (given by the log file creator) of the log file
		/// </summary>
		private static string originalFNameExtension = default;
		/// <summary>
		/// Indicates whether some settings may be changed or not
		/// </summary>
		private static bool canChangeAllSettings = true;
		/// <summary>
		/// Log file handle
		/// </summary>
		private static StreamWriter streamWriter = default;
		/// <summary>
		/// Lock object
		/// </summary>
		private static readonly Object mylock = new Object();
		#endregion

		#region methods
		public static string DEBUG(string s = default) { return Add(s, TLog.DEBUG); }
		public static string INFORMATION(string s = default) { return Add(s, TLog.INFOR); }
		public static string TRACE(string s = default) { return Add(s, TLog.TRACE); }
		public static string WARNING(string s = default) { return Add(s, TLog.WARNG); }
		public static string ERROR(string s = default) { return Add(s, TLog.ERROR); }
		public static string EXCEPT(Exception ex, string s = default) { return AddException(ex, s); }
		/// <summary>
		/// Test if a severity is within bounds
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static bool IsTLog(TLog value) { return (TLog._begin < value && value < TLog._end); }
		/// <summary>
		/// Log a message to the log file
		/// </summary>
		/// <param name="ls">A list of message to log</param>
		/// <param name="severity">severity level</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string Add(List<string> ls, TLog severity = TLog.TRACE)
		{
			// arrived here the file is ready for write, write what was meant to be written
			return AddEx(ls, severity);
		}
		/// <summary>
		/// Log a message to the log file
		/// </summary>
		/// <param name="s">Message to log</param>
		/// <param name="severity">severity level</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string Add(string s, TLog severity = TLog.TRACE)
		{
			// arrived here the file is ready for write, write what was meant to be written
			return AddEx(new List<string>() { s }, severity);
		}
		/// <summary>
		/// Log an exception to the log file (the whole exception tree is written)
		/// </summary>
		/// <param name="ex">exception to log</param>
		/// <param name="msg">message to complete the log entry</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		private static string AddException(Exception ex, string msg)
		{
			string r = default;
			if (default == ex) return r;
			try
			{
				StackTrace st = new StackTrace(ex, true);
				List<string> ls = new List<string>();
				ls.Add($"[EXCEPTION] {ex.GetType()}{(string.IsNullOrEmpty(ex.Message) ? default : $" - {ex.Message}")}{(string.IsNullOrEmpty(msg) ? default : $" - {msg}")}");
				Exception exx = ex.InnerException;
				while (default != exx)
				{
					ls.Add($"[EXCEPTION] {exx.GetType()}{(string.IsNullOrEmpty(exx.Message) ? default : $" - {exx.Message}")}");
					exx = exx.InnerException;
				}
				for (int i = st.FrameCount; 0 != i; i--)
				{
					StackFrame sf = st.GetFrame(i - 1);
					string f = string.IsNullOrEmpty(sf.GetFileName()) ? "??" : sf.GetFileName();
					string m = string.IsNullOrEmpty(sf.GetMethod().ToString()) ? "??" : $"{sf.GetMethod()}";
					ls.Add($"[EXCEPTION #{st.FrameCount - i + 1}] File: {f} - Method: {m} - Line Number: {sf.GetFileLineNumber()}");
				}
				r = AddEx(ls, TLog.EXCPT);
			}
			catch (Exception) { }
			return r;
		}
		public static string AddException(string dummy, Exception ex, string msg = "")
		{
			return AddException(ex, msg);
		}
		/// <summary>
		/// Add the string(s) tothe log file
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="severity"></param>
		/// <returns></returns>
		private static string AddEx(List<string> ls, TLog severity)
		{
			if (default == ls || 0 == ls.Count) return default;

			// create the string to log
			string r = default;
			try
			{
				List<string> lls = StringsToLog(ls, severity, out r);

				// check severity
				if (!IsTLog(severity)) severity = TLog.INFOR;

				// severity to low to be logged, do not do it but return the message to log

#if DEBUG || _DEBUG
				AlwaysLogDebug = true;
#endif

				if (SeverityToLog > severity
					&& (TLog.DEBUG == severity && !AlwaysLogDebug)
					&& (TLog.EXCPT != severity))
				{
					return r;
				}

				// check whether in need to open a new file or not
				if (DateTime.Now.Date != CreatedOn.Date)
					// re-open a new file with the same name but different timestamp
					OpenLogFile(originalFName);

				// arrived here the file is ready for write, write what was meant to be written
				AddToLog(lls, severity);
			}
			catch (Exception) { }
			return r;
		}
		/// <summary>
		/// Remove unwanted chars (CR, LF, TAB) from a string
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		private static string RemoveCRLF(string s)
		{
			if (!KeepCRLF)
			{
				string t;
				do { t = s; s = s.Replace(Chars.CRCR, Chars.CR); } while (t != s);
				do { t = s; s = s.Replace(Chars.CRLF, Chars.CR); } while (t != s);
				do { t = s; s = s.Replace(Chars.LFLF, Chars.LF); } while (t != s);
				do { t = s; s = s.Replace(Chars.LFCR, Chars.CR); } while (t != s);
				do { t = s; s = s.Replace(Chars.TABTAB, Chars.TAB); } while (t != s);
				s = s.Replace(Chars.CR, LinesSeparator);
				s = s.Replace(Chars.LF, LinesSeparator);
				s = s.Replace(Chars.TAB, LinesSeparator);
			}
			return s;
		}
		/// <summary>
		/// Actually write message to the log file
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="severity">severity level</param>
		private static void AddToLog(List<string> ls, TLog severity)
		{
			if (default == ls || 0 == ls.Count) return;
			try
			{
				lock (mylock)
				{
					AddToLogUnsafe(ls, severity);
				}
			}
			catch (Exception) { }
		}
		private static void AddToLog(string s, TLog severity)
		{
			if (!string.IsNullOrEmpty(s)) AddToLog(new List<string>() { s }, severity);
		}
		/// <summary>
		/// Actually write message to the log file
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="severity">severity level</param>
		private static void AddToLogUnsafe(List<string> ls, TLog severity)
		{
			try
			{
				foreach (string s in ls)
					streamWriter?.WriteLine(s);
			}
			catch (Exception) { }
		}
		private static void AddToLogUnsafe(string s, TLog severity)
		{
			AddToLogUnsafe(new List<string>() { s }, severity);
		}
		/// <summary>
		/// Create the complete kist of strings to log
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="severity"></param>
		/// <param name="r"></param>
		/// <returns></returns>
		private static List<string> StringsToLog(List<string> ls, TLog severity, out string r)
		{
			r = default;
			List<string> lls = new List<string>();
			try
			{
				string v = $"{CMisc.BuildDate(_dateFormat)}{Chars.TAB}{severity}{Chars.TAB}{Thread.CurrentThread.ManagedThreadId.ToString("X8")}{Chars.TAB}{Guid.NewGuid()}{Chars.TAB}";
				for (int i = 0; i < ls.Count; i++)
				{
					string q = $"{RemoveCRLF((TLog.ERROR == severity && ErrorToUpper ? ls[i].Trim().ToUpper() : ls[i].Trim()))}";
					lls.Add($"{v}{q}");
					r += ls[i] + (i < ls.Count - 1 ? Chars.CRLF : default);
				}
			}
			catch (Exception) { }
			return lls;
		}
		private static List<string> StringsToLog(string s, TLog severity, out string r)
		{
			return StringsToLog(new List<string>() { s }, severity, out r);
		}

		#region log file management
		/// <summary>
		/// Open the log file
		/// </summary>
		private static bool OpenLogFile(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) return false;
			// only if a file open has been requested
			CloseLogFile();
			try
			{
				lock (mylock)
				{
					try
					{
						// try to open the file
						_logfilename = BuildLogFileName(fileName);
						streamWriter = new StreamWriter(_logfilename, true, Encoding.UTF8);
						streamWriter.AutoFlush = true;
						AddToLogUnsafe($"+++++", TLog.INFOR);
						AddToLogUnsafe($"+++++ {LogFileName.ToUpper()} OPENED: {CMisc.BuildDate(_dateFormat, CreatedOn)} (VERSION: {CMisc.Version(CMisc.VersionType.assembly)}-{CMisc.Version(CMisc.VersionType.assemblyFile)}-{CMisc.Version(CMisc.VersionType.assemblyInfo)})", TLog.INFOR);
						AddToLogUnsafe($"+++++", TLog.INFOR);
						try
						{
							if (AutoPurgeLogFiles)
								PurgeFiles(NumberOfFilesToKeep);
						}
						catch (Exception) { }
						canChangeAllSettings = false;
						return true;
					}
					catch (Exception)
					{
						CloseLogFile();
					}
				}
			}
			catch (Exception) { }
			return false;
		}
		/// <summary>
		/// Build the name of the log file, setting properties accordingly
		/// </summary>
		/// <returns>Complete log file name</returns>
		private static string BuildLogFileName(string fileName)
		{
			try
			{
				FileInfo fi = new FileInfo(fileName);
				originalFName = fi.Name;
				originalFNameExtension = Path.GetExtension(fi.FullName);
				originalFNameWithoutExtension = Path.GetFileNameWithoutExtension(fi.FullName);
				LogFilePath = Path.GetDirectoryName(fi.FullName);
				CreatedOn = DateTime.Now;
				return $"{LogFilePath}{originalFNameWithoutExtension}-{CMisc.BuildDate(CMisc.DateFormat.YYYYMMDD, CreatedOn)}{(string.IsNullOrEmpty(originalFNameExtension) ? EXTENSION : originalFNameExtension)}";
			}
			catch (Exception) { }
			return default;
		}
		/// <summary>
		/// Close the current log file
		/// </summary>
		private static void CloseLogFile()
		{
			lock (mylock)
			{
				try
				{
					if (default != streamWriter)
					{
						// close current log file
						AddToLogUnsafe($"-----", TLog.INFOR);
						AddToLogUnsafe($"----- {LogFileName.ToUpper()} CLOSED: {CMisc.BuildDate(_dateFormat)}", TLog.INFOR);
						AddToLogUnsafe($"-----", TLog.INFOR);
					}
				}
				catch (Exception) { }
			}
			streamWriter?.Close();
			streamWriter = default;
			CreatedOn = default;
			_logfilename = LogFilePath = originalFName = originalFNameExtension = originalFNameWithoutExtension = default;
			canChangeAllSettings = true;
		}
		/// <summary>
		/// Purge existing log file with the same name
		/// </summary>
		/// <param name="numerberOfFilesToKeep">the number of log files to keep</param>
		private static void PurgeFiles(int numerberOfFilesToKeep)
		{
			if (KEEP_ALL_FILES < numerberOfFilesToKeep)
			{
				try
				{
					// get files fitting search pattern and order them by descending creation date
					DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(LogFileName));
					FileSystemInfo[] files = di.GetFileSystemInfos($"{originalFNameWithoutExtension}*{originalFNameExtension}");
					// order is from newer to older
					var orderedFiles = files.OrderByDescending(x => x.CreationTimeUtc).ToList();
					int counter = 0;
					int deleted = 0;
					foreach (var file in orderedFiles)
					{
						if (LogFileName != file.FullName)
						{
							counter += 1;
							if (numerberOfFilesToKeep < counter)
							{
								// delete file
								file.Delete();
								deleted += 1;
							}
						}
					}
				}
				catch (Exception) { }
				finally { }
			}
		}
		#endregion

		#endregion
	}
}
