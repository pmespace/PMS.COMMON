#define DEBUGLOG
using System.Runtime.InteropServices;
#if !NET35
using System.IO.MemoryMappedFiles;
#endif
using System.Diagnostics;
using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

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

	public class CLogMsg
	{
		public CLogMsg() { Msg = string.Empty; Severity = TLog.TRACE; }
		public CLogMsg(string msg, TLog severity) { Msg = msg; Severity = severity; }
		public string Msg { get; set; }
		public TLog Severity { get => _severity; set => _severity = (CLog.IsTLog(value) ? value : _severity); }
		TLog _severity;
		protected virtual string ToStringPrefix()
		{
			return $"{CMisc.BuildDate(CMisc.DateFormat.YYYYMMDDhhmmssfffEx)}{Chars.TAB}{Severity}{Chars.TAB}{Thread.CurrentThread.ManagedThreadId.ToString("X8")}{Chars.TAB}";
		}
		protected virtual string ToStringSC(bool addSharedData = true)
		{
			Guid? guid;
			string sc;
			if (addSharedData)
			{
				guid = CLog.SharedGuid;
				sc = CLog.SharedContext;
			}
			else
			{
				guid = Guid.NewGuid();
				sc = null;
			}
			return $"{guid}{Chars.TAB}{(sc.IsNullOrEmpty() ? string.Empty : $"[{sc}] ")}";
		}
		public override string ToString() { return $"{CLog.RemoveCRLF(Msg.Trim())}"; }
		public string ToString(bool addSharedData)
		{
			try
			{
				return CLog.SeverityToLog <= Severity && !Msg.IsNullOrEmpty() ? ToStringPrefix() + ToStringSC(addSharedData) + ToString() : string.Empty;
			}
			catch (Exception) { }
			return string.Empty;
		}
	}
	class CLogMsgEx : CLogMsg
	{
		protected override string ToStringPrefix() { return string.Empty; }
		protected override string ToStringSC(bool addSharedData = true) { return string.Empty; }
	}
	public class CLogMsgs : List<CLogMsg>
	{
		public CLogMsgs() { }
		public CLogMsgs(CLogMsg lm) { if (default != lm) Add(lm); }
		public CLogMsgs(List<string> ls, TLog severity = TLog.TRACE)
		{
			if (default == ls || 0 == ls.Count) return;
			for (int i = 0; i < ls.Count; i++)
				if (!ls[i].IsNullOrEmpty())
					Add(new CLogMsg() { Msg = ls[i], Severity = severity });
		}
		internal CLogMsgs(List<string> ls)
		{
			if (default == ls || 0 == ls.Count) return;
			for (int i = 0; i < ls.Count; i++)
				if (!ls[i].IsNullOrEmpty())
					Add(new CLogMsgEx() { Msg = ls[i], Severity = TLog.INFOR });
		}
		public override string ToString()
		{
			string r = string.Empty;
			try
			{
				for (int i = 0; i < Count; i++)
				{
					r += (r.IsNullOrEmpty() ? string.Empty : Chars.CRLF) + this[i].ToString();
				}
			}
			catch (Exception) { }
			return r;
		}
		public string ToString(bool addSharedData)
		{
			string r = string.Empty;
			try
			{
				for (int i = 0; i < Count; i++)
				{
					r += (r.IsNullOrEmpty() ? string.Empty : Chars.CRLF) + this[i].ToString(addSharedData);
				}
			}
			catch (Exception) { }
			return r;
		}
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
		///// <summary>
		///// Indicate whether error log must be set to upper or not
		///// </summary>
		//public static bool ErrorToUpper { get; set; } = false;
		/// <summary>
		/// Separator to use between multiple lines in a resulting 1 line string
		/// </summary>
		public static string LinesSeparator { get; set; } = " ";// Chars.TAB;
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
		internal static CMisc.DateFormat _dateFormat = CMisc.DateFormat.YYYYMMDDhhmmssfffEx;
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

		#region per thread shared memory management

#if !NET35
		/// <summary>
		/// Dictionary of MemoryMappedFile indexed by thread number
		/// </summary>
		static Dictionary<int, MemoryMappedFile> Mmfs = new Dictionary<int, MemoryMappedFile>();
		const int offsetInitialized = 0;
		const int offsetGuid = sizeof(bool);
		const int guidLen = 16;
		const int offsetContextLen = offsetGuid + guidLen;
		const int labelLen = sizeof(int);
		const int offsetContext = offsetContextLen + labelLen;
		const int mappedMemoryFileSize = 1024;
		/// <summary>
		/// Get a <see cref="MemoryMappedFile"/> handle specific to the current thread
		/// </summary>
		/// <returns></returns>
		static MemoryMappedFile GetMmf()
		{
			MemoryMappedFile mmf = null;
			try
			{
				lock (mylock)
				{
					try
					{
						mmf = Mmfs[Thread.CurrentThread.ManagedThreadId];
					}
					catch (Exception)
					{
						mmf = MemoryMappedFile.CreateOrOpen(Thread.CurrentThread.ManagedThreadId.ToString("00000"), mappedMemoryFileSize);
						Mmfs.Add(Thread.CurrentThread.ManagedThreadId, mmf);
					}
				}
			}
			catch (Exception) { mmf = null; }
			return mmf;
		}
		/// <summary>
		/// Delete athe <see cref="MemoryMappedFile"/> specific to the thread
		/// </summary>
		internal static void RazMmf()
		{
			lock (mylock)
			{
				try
				{
					var mmf = GetMmf();
					mmf.Dispose();
					Mmfs.Remove(Thread.CurrentThread.ManagedThreadId);
				}
				catch (Exception) { }
			}
		}
#endif
		/// <summary>
		/// Specific GUID to use when logging data.
		/// This value is set per thread.
		/// </summary>
		public static Guid? SharedGuid
		{
			get
			{
#if NET35
				return Guid.NewGuid();
#else
				// access to the shared memory
				try
				{
#if DEBUG && DEBUGLOG
					AddEx(new List<string>() { "Guid shared memory name: " + CThread.SharedName }, TLog.DEBUG, false);
#endif
					//	var mmf = MemoryMappedFile.CreateOrOpen(CThread.SharedName, mappedMemoryFileSize);
					var mmf = GetMmf();
					using (var accessor = mmf.CreateViewAccessor())
					{
						accessor.Read(offsetInitialized, out bool b);
						if (b)
						{
							byte[] ab = new byte[guidLen];
							accessor.ReadArray<byte>(offsetGuid, ab, 0, guidLen);
							return new Guid(ab);
						}
					}
				}
				catch (Exception ex)
				{
					AddException(ex, null, false);
				}
				return Guid.NewGuid();
#endif
			}
			set
			{
#if NET35
				return;
#else
				// access to the shared memory
				try
				{
					//var mmf = MemoryMappedFile.CreateOrOpen(CThread.SharedName, mappedMemoryFileSize);
					var mmf = GetMmf();
					using (var accessor = mmf.CreateViewAccessor())
					{
						bool b = value.HasValue && Guid.Empty != value.Value;
						accessor.Write(offsetInitialized, b);
						accessor.WriteArray<byte>(offsetGuid, b ? value.Value.ToByteArray() : Guid.Empty.ToByteArray(), 0, guidLen);
					}
				}
				catch (Exception ex)
				{
					AddException(ex, null, false);
				}
#endif
			}
		}
		public static Guid SetNewSharedGuid() { SharedGuid = Guid.NewGuid(); return (Guid)SharedGuid; }
		/// <summary>
		/// Specific context string to use when logging data.
		/// This value is set per thread.
		/// It's MAXIMUM length is 1000 bytes in UTF8.
		/// </summary>
		public static string SharedContext
		{
			get
			{
#if NET35
				return string.Empty;
#else
				// access to the shared memory
				try
				{
#if DEBUG && DEBUGLOG
					AddEx(new List<string>() { "Context shared memory name: " + CThread.SharedName }, TLog.DEBUG, false);
#endif
					//var mmf = MemoryMappedFile.CreateOrOpen(CThread.SharedName, mappedMemoryFileSize);
					var mmf = GetMmf();
					using (var accessor = mmf.CreateViewAccessor())
					{
						accessor.Read(offsetContextLen, out int i);
						if (0 != i)
						{
							byte[] ab = new byte[i];
							accessor.ReadArray<byte>(offsetContext, ab, 0, i);
							return Encoding.UTF8.GetString(ab);
						}
					}
				}
				catch (Exception ex)
				{
					AddException(ex, null, false);
				}
				return string.Empty;
#endif
			}
			set
			{
#if NET35
				return;
#else
				// access to the shared memory
				try
				{
					//var mmf = MemoryMappedFile.CreateOrOpen(CThread.SharedName, mappedMemoryFileSize);
					var mmf = GetMmf();
					using (var accessor = mmf.CreateViewAccessor())
					{
						byte[] ab = Encoding.UTF8.GetBytes(value.IsNullOrEmpty() ? string.Empty : value);
						accessor.Write(offsetContextLen, ab.Length);
						accessor.WriteArray<byte>(offsetContext, ab, 0, ab.Length);
					}
				}
				catch (Exception ex)
				{
					AddException(ex, null, false);
				}
#endif
			}
		}
		#endregion

		#region methods
		public static string DEBUG(string s) { return Add(s, TLog.DEBUG); }
		public static string DEBUG(List<string> ls) { return Add(ls, TLog.DEBUG); }
		public static string INFORMATION(string s) { return Add(s, TLog.INFOR); }
		public static string INFORMATION(List<string> ls) { return Add(ls, TLog.INFOR); }
		public static string TRACE(string s) { return Add(s, TLog.TRACE); }
		public static string TRACE(List<string> ls) { return Add(ls, TLog.TRACE); }
		public static string WARNING(string s) { return Add(s, TLog.WARNG); }
		public static string WARNING(List<string> ls) { return Add(ls, TLog.WARNG); }
		public static string ERROR(string s) { return Add(s, TLog.ERROR); }
		public static string ERROR(List<string> ls) { return Add(ls, TLog.ERROR); }
		public static string EXCEPT(Exception ex, string s = default) { return AddException(ex, s); }
		/// <summary>
		/// Test if a severity is within bounds
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool IsTLog(TLog value) { return (TLog._begin < value && value < TLog._end); }
		/// <summary>
		/// Log a message to the log file
		/// </summary>
		/// <param name="s">Message to log</param>
		/// <param name="severity">severity level</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string Add(string s, TLog severity = TLog.TRACE) { return AddEx(new List<string>() { s }, severity); }
		/// <summary>
		/// Log a message to the log file
		/// </summary>
		/// <param name="ls">A list of message to log</param>
		/// <param name="severity">severity level</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string Add(List<string> ls, TLog severity = TLog.TRACE) { return Add(new CLogMsgs(ls, severity)); }
		/// <summary>
		/// Log a list of messages to the log file
		/// </summary>
		/// <param name="ls">A list of <see cref="CLogMsg"/> to log</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string Add(CLogMsgs ls) { return AddEx(ls); }
		/// <summary>
		/// Log an exception to the log file (the whole exception tree is written)
		/// </summary>
		/// <param name="ex">exception to log</param>
		/// <param name="msg">message to complete the log entry</param>
		/// <param name="addSharedData">allows indicating whether shared data must be added to the logged message or not</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		private static string AddException(Exception ex, string msg, bool addSharedData = true)
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
					ls.Add($"[EXCEPTION #{st.FrameCount - i + 1}] file: {f}; method: {m}; #line: {sf.GetFileLineNumber()}");
				}
				r = AddEx(ls, addSharedData ? TLog.EXCPT : TLog.DEBUG, addSharedData);
			}
			catch (Exception) { }
			return r;
		}
		/// <summary>
		/// Add the string(s) to the log file
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="addSharedData"></param>
		/// <returns></returns>
		private static string AddEx(CLogMsgs ls, bool addSharedData = true)
		{
			if (default == ls || 0 == ls.Count) return default;

			// create the string to log
			string r = default;
			try
			{
				// get the string to log
				r = ls.ToString();
				if (!r.IsNullOrEmpty())
				{
					// check whether need to open a new file
					if (DateTime.Now.Date != CreatedOn.Date)
						// re-open a new file with the same name but different timestamp
						OpenLogFile(originalFName);

					// arrived here the file is ready for write, write what was meant to be written
					AddToLog(ls.ToString(addSharedData));
				}
			}
			catch (Exception) { }
			return r;
		}
		/// <summary>
		/// Add the string(s) tothe log file
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="severity"></param>
		/// <param name="addSharedData"></param>
		/// <returns></returns>
		private static string AddEx(List<string> ls, TLog severity, bool addSharedData = true)
		{
			return AddEx(new CLogMsgs(ls, severity), addSharedData);
		}
		/// <summary>
		/// Remove unwanted chars (CR, LF, TAB) from a string
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string RemoveCRLF(string s)
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
		/// Write safely to the log file
		/// </summary>
		/// <param name="s">The message to write, it can be multiline</param>
		private static void AddToLog(string s)
		{
			if (s.IsNullOrEmpty()) return;
			try
			{
				lock (mylock)
				{
					AddToLogUnsafe(s);
				}
			}
			catch (Exception) { }
		}
		/// <summary>
		/// Unsafely write to the log file
		/// </summary>
		/// <param name="s">The message to write, it can be multiline</param>
		private static void AddToLogUnsafe(string s)
		{
			try
			{
				streamWriter?.WriteLine(s);
			}
			catch (Exception) { }
		}
		#endregion

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
						var ls = new CLogMsgs(
							 new List<string>
							 {
								 $"+++++",
								 $"+++++ {LogFileName.ToUpper()} OPENED: {CMisc.BuildDate(_dateFormat, CreatedOn)} (VERSION: {CMisc.Version(CMisc.VersionType.assembly)}-{CMisc.Version(CMisc.VersionType.assemblyFile)}-{CMisc.Version(CMisc.VersionType.assemblyInfo)})",
								 $"+++++",
							 });
						AddToLog(ls.ToString(false));
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
						var ls = new CLogMsgs(
							new List<string>
							{
								$"-----",
								$"----- {LogFileName.ToUpper()} CLOSED: {CMisc.BuildDate(_dateFormat)}",
								$"-----"
							});
						AddToLog(ls.ToString(false));
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
	}
}
