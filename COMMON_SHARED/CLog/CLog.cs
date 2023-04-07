#define DEBUGLOG
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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
		FMNGT = -2,
		_begin = -1,
		DEBUG,
		INFOR,
		TRACE,
		WARNG,
		ERROR,
		EXCPT,
		_end,
		_none
	}

	public class CLogMsg
	{
		public CLogMsg() { Msg = string.Empty; Severity = TLog.TRACE; }
		public CLogMsg(string msg, TLog severity) { Msg = msg; Severity = severity; }
		public string Msg { get; set; }
		public virtual TLog Severity { get => _severity; set => _severity = (CLog.IsTLog(value) ? value : _severity); }
		protected TLog _severity;
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
		protected string ToStringPrefix() => $"{CMisc.BuildDate(CMisc.DateFormat.YYYYMMDDhhmmssfffEx)}{Chars.TAB}{Severity}{Chars.TAB}{Thread.CurrentThread.ManagedThreadId.ToString("X8")}{Chars.TAB}";
		public override string ToString() => $"{(CLog.SeverityToLog <= Severity || TLog.FMNGT == Severity ? CLog.RemoveCRLF(Msg.Trim()) : string.Empty)}";
		internal virtual string ToString(bool addSharedData)
		{
			try
			{
				return (CLog.SeverityToLog <= Severity && !Msg.IsNullOrEmpty()) || TLog.FMNGT == Severity ? ToStringPrefix() + ToStringSC(addSharedData) + ToString() : string.Empty;
			}
			catch (Exception) { }
			return string.Empty;
		}
	}
	class CLogMsgEx : CLogMsg
	{
		public override TLog Severity { get => _severity; set => _severity = (CLog.IsTLog(value) || TLog.FMNGT == value ? value : _severity); }
		protected override string ToStringSC(bool addSharedData = true) => Guid.Empty.ToString() + Chars.TAB;
		internal override string ToString(bool addSharedData)
		{
			try
			{
				return ToStringPrefix() + ToStringSC(addSharedData) + ToString();
			}
			catch (Exception) { }
			return string.Empty;
		}
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
					Add(new CLogMsgEx() { Msg = ls[i], Severity = TLog.FMNGT });
		}
		public override string ToString()
		{
			string r = string.Empty;
			try
			{
				for (int i = 0; i < Count; i++)
				{
					string s = this[i].ToString();
					r += (s.IsNullOrEmpty() ? string.Empty : 0 == i ? s : Chars.CRLF + s);
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
					string s = this[i].ToString(addSharedData);
					r += (s.IsNullOrEmpty() ? string.Empty : 0 == i ? s : Chars.CRLF + s);
				}
			}
			catch (Exception) { }
			return r.Trim();
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
		const string EXTENSION = ".log";
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
		static string _logfilename = default;
		/// <summary>
		/// Path of the log file, without the log file name.
		/// It always ends with "\" (or any other platform folder separator)
		/// </summary>
		public static string LogFilePath
		{
			get => _logfilepath;
			private set => _logfilepath = (value.IsNullOrEmpty() ? default : value + (Path.DirectorySeparatorChar != value[value.Length - 1] ? new string(Path.DirectorySeparatorChar, 1) : default));
		}
		static string _logfilepath = default;
		/// <summary>
		/// Indicates whether autopurge previous log file when opening a new one
		/// </summary>
		public static bool AutoPurgeLogFiles { get; set; } = false;
		/// <summary>
		/// Indicates the number of files to keep if <see cref="AutoPurgeLogFiles"/> is set to true
		/// </summary>
		public static int NumberOfFilesToKeep = KEEP_ALL_FILES;
		/// <summary>
		/// Indicates whether CR, LF and other special characters are kept when logging into file
		/// </summary>
		public static bool KeepCRLF = false;
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
					_severitytolog = IsTLog(value) || TLog.FMNGT == value ? value : _severitytolog;
				}
			}
		}
		static TLog _severitytolog = TLog.TRACE;
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
		static bool _usegmt = false;
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
		static bool _uselocal = false;
		internal static CMisc.DateFormat _dateFormat = CMisc.DateFormat.YYYYMMDDhhmmssfffEx;
		#endregion

		#region privates
		/// <summary>
		/// Original fname used to create the log file
		/// </summary>
		static string originalFName = default;
		/// <summary>
		/// Original name (given by the log file creator) of the log file, without its extension
		/// </summary>
		static string originalFNameWithoutExtension = default;
		/// <summary>
		/// Original extension (given by the log file creator) of the log file
		/// </summary>
		static string originalFNameExtension = default;
		/// <summary>
		/// Indicates whether some settings may be changed or not
		/// </summary>
		static bool canChangeAllSettings = true;
		/// <summary>
		/// Log file handle
		/// </summary>
		static StreamWriter streamWriter = default;
		/// <summary>
		/// Lock object
		/// </summary>
		static readonly Object mylock = new Object();
		#endregion

		#region old per thread shared memory management
		//#if !NET35
		//		/// <summary>
		//		/// Dictionary of MemoryMappedFile indexed by thread number
		//		/// </summary>
		//		static Dictionary<int, MemoryMappedFile> Mmfs = new Dictionary<int, MemoryMappedFile>();
		//		const int offsetInitialized = 0;
		//		const int offsetGuid = sizeof(bool);
		//		const int guidLen = 16;
		//		const int offsetContextLen = offsetGuid + guidLen;
		//		const int labelLen = sizeof(int);
		//		const int offsetContext = offsetContextLen + labelLen;
		//		const int mappedMemoryFileSize = 1024;
		//		/// <summary>
		//		/// Get a <see cref="MemoryMappedFile"/> handle specific to the current thread
		//		/// </summary>
		//		/// <returns></returns>
		//		static MemoryMappedFile GetMmf()
		//		{
		//			MemoryMappedFile mmf = null;
		//			try
		//			{
		//				lock (mylock)
		//				{
		//					try
		//					{
		//						mmf = Mmfs[Thread.CurrentThread.ManagedThreadId];
		//					}
		//					catch (Exception)
		//					{
		//						mmf = MemoryMappedFile.CreateOrOpen(Thread.CurrentThread.ManagedThreadId.ToString("00000"), mappedMemoryFileSize);
		//						Mmfs.Add(Thread.CurrentThread.ManagedThreadId, mmf);
		//					}
		//				}
		//			}
		//			catch (Exception) { mmf = null; }
		//			return mmf;
		//		}
		//		/// <summary>
		//		/// Delete athe <see cref="MemoryMappedFile"/> specific to the thread
		//		/// </summary>
		//		internal static void RazMmf()
		//		{
		//			lock (mylock)
		//			{
		//				try
		//				{
		//					var mmf = GetMmf();
		//					mmf.Dispose();
		//					Mmfs.Remove(Thread.CurrentThread.ManagedThreadId);
		//				}
		//				catch (Exception) { }
		//			}
		//		}
		//#endif
		//		/// <summary>
		//		/// Specific GUID to use when logging data.
		//		/// This value is set per thread.
		//		/// </summary>
		//		public static Guid? SharedGuid
		//		{
		//			get
		//			{
		//#if NET35
		//				return Guid.NewGuid();
		//#else
		//				// access to the shared memory
		//				try
		//				{
		//#if DEBUG && DEBUGLOG
		//					AddEx(new List<string>() { "Guid shared memory name: " + CThread.SharedName }, TLog.DEBUG, false);
		//#endif
		//					//	var mmf = MemoryMappedFile.CreateOrOpen(CThread.SharedName, mappedMemoryFileSize);
		//					var mmf = GetMmf();
		//					using (var accessor = mmf.CreateViewAccessor())
		//					{
		//						accessor.Read(offsetInitialized, out bool b);
		//						if (b)
		//						{
		//							byte[] ab = new byte[guidLen];
		//							accessor.ReadArray<byte>(offsetGuid, ab, 0, guidLen);
		//							return new Guid(ab);
		//						}
		//					}
		//				}
		//				catch (Exception ex)
		//				{
		//					AddException(ex, null, false);
		//				}
		//				return Guid.NewGuid();
		//#endif
		//			}
		//			set
		//			{
		//#if NET35
		//				return;
		//#else
		//				// access to the shared memory
		//				try
		//				{
		//					//var mmf = MemoryMappedFile.CreateOrOpen(CThread.SharedName, mappedMemoryFileSize);
		//					var mmf = GetMmf();
		//					using (var accessor = mmf.CreateViewAccessor())
		//					{
		//						bool b = value.HasValue && Guid.Empty != value.Value;
		//						accessor.Write(offsetInitialized, b);
		//						accessor.WriteArray<byte>(offsetGuid, b ? value.Value.ToByteArray() : Guid.Empty.ToByteArray(), 0, guidLen);
		//					}
		//				}
		//				catch (Exception ex)
		//				{
		//					AddException(ex, null, false);
		//				}
		//#endif
		//			}
		//		}
		//		public static Guid SetNewSharedGuid() { SharedGuid = Guid.NewGuid(); return (Guid)SharedGuid; }
		//		/// <summary>
		//		/// Specific context string to use when logging data.
		//		/// This value is set per thread.
		//		/// It's MAXIMUM length is 1000 bytes in UTF8.
		//		/// </summary>
		//		public static string SharedContext
		//		{
		//			get
		//			{
		//#if NET35
		//				return string.Empty;
		//#else
		//				// access to the shared memory
		//				try
		//				{
		//#if DEBUG && DEBUGLOG
		//					AddEx(new List<string>() { "Context shared memory name: " + CThread.SharedName }, TLog.DEBUG, false);
		//#endif
		//					//var mmf = MemoryMappedFile.CreateOrOpen(CThread.SharedName, mappedMemoryFileSize);
		//					var mmf = GetMmf();
		//					using (var accessor = mmf.CreateViewAccessor())
		//					{
		//						accessor.Read(offsetContextLen, out int i);
		//						if (0 != i)
		//						{
		//							byte[] ab = new byte[i];
		//							accessor.ReadArray<byte>(offsetContext, ab, 0, i);
		//							return Encoding.UTF8.GetString(ab);
		//						}
		//					}
		//				}
		//				catch (Exception ex)
		//				{
		//					AddException(ex, null, false);
		//				}
		//				return string.Empty;
		//#endif
		//			}
		//			set
		//			{
		//#if NET35
		//				return;
		//#else
		//				// access to the shared memory
		//				try
		//				{
		//					//var mmf = MemoryMappedFile.CreateOrOpen(CThread.SharedName, mappedMemoryFileSize);
		//					var mmf = GetMmf();
		//					using (var accessor = mmf.CreateViewAccessor())
		//					{
		//						byte[] ab = Encoding.UTF8.GetBytes(value.IsNullOrEmpty() ? string.Empty : value);
		//						accessor.Write(offsetContextLen, ab.Length);
		//						accessor.WriteArray<byte>(offsetContext, ab, 0, ab.Length);
		//					}
		//				}
		//				catch (Exception ex)
		//				{
		//					AddException(ex, null, false);
		//				}
		//#endif
		//			}
		//		}
		#endregion

		//#region per thread shared memory management
		///// <summary>
		///// Dictionary of memory objects indexed by a key
		///// </summary>
		//static Dictionary<int, Stack<MMF>> Mmfs = new Dictionary<int, Stack<MMF>>();
		//class MMF
		//{
		//	public MMF() { Uid = Guid.NewGuid(); Context = string.Empty; }
		//	public Guid Uid { get; set; }
		//	public string Context { get; set; }
		//}
		///// <summary>
		///// Get the <see cref="MMF"/> object specific to the current thread if available,
		///// a newly created one if unavailable
		///// </summary>
		///// <returns>a <see cref="MMF"/> object</returns>
		//static MMF GetMmf()
		//{
		//	MMF mmf = new MMF();
		//	lock (mylock)
		//	{
		//		try
		//		{
		//			if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
		//				mmf = Mmfs[Thread.CurrentThread.ManagedThreadId].Peek();
		//		}
		//		catch (Exception) { }
		//	}
		//	return mmf;
		//}
		///// <summary>
		///// Pop the last added data
		///// </summary>
		//internal static void PopMmf()
		//{
		//	lock (mylock)
		//	{
		//		try
		//		{
		//			if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
		//				Mmfs[Thread.CurrentThread.ManagedThreadId].Pop();
		//		}
		//		catch (Exception) { }
		//	}
		//	try
		//	{
		//		if (0 == Mmfs[Thread.CurrentThread.ManagedThreadId].Count)
		//			Mmfs.Remove(Thread.CurrentThread.ManagedThreadId);
		//	}
		//	catch (Exception) { }
		//}
		///// <summary>
		///// Add a new memory data
		///// </summary>
		///// <returns>
		///// true if the data has been pushed,
		///// false otherwise
		///// </returns>
		//internal static void PushMmf()
		//{
		//	lock (mylock)
		//	{
		//		try
		//		{
		//			if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
		//				Mmfs[Thread.CurrentThread.ManagedThreadId].Push(new MMF() { Uid = Guid.NewGuid(), Context = Mmfs[Thread.CurrentThread.ManagedThreadId].Peek().Context });
		//			else
		//				Mmfs.Add(Thread.CurrentThread.ManagedThreadId, new Stack<MMF>(new MMF[] { new MMF() }));
		//		}
		//		catch (Exception) { }
		//	}
		//}
		///// <summary>
		///// Specific <see cref="Guid"/> to use when logging data.
		///// This value is set per thread.
		///// </summary>
		///// <returns>a GUID</returns>
		//public static Guid SharedGuid
		//{
		//	get => GetMmf().Uid;
		//	set
		//	{
		//		try
		//		{
		//			if (Guid.NewGuid() == value || default == value)
		//				PopMmf();
		//			else
		//				PushMmf();
		//		}
		//		catch (Exception) { }
		//	}
		//}
		//public static Guid SetNewSharedGuid() => SharedGuid = Guid.NewGuid();
		///// <summary>
		///// Specific context string to use when logging data.
		///// This value is set per thread.
		///// </summary>
		//public static string SharedContext
		//{
		//	get => GetMmf().Context;
		//	set
		//	{
		//		lock (mylock)
		//		{
		//			try
		//			{
		//				if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
		//					Mmfs[Thread.CurrentThread.ManagedThreadId].Peek().Context = value;
		//				else
		//					Mmfs.Add(Thread.CurrentThread.ManagedThreadId, new Stack<MMF>(new MMF[] { new MMF() { Context = value } }));
		//			}
		//			catch (Exception) { }
		//		}
		//	}
		//}
		//#endregion

		//#region per thread shared memory management
		///// <summary>
		///// Dictionary of memory objects indexed by a key
		///// </summary>
		//static Dictionary<int, MMF> Mmfs = new Dictionary<int, MMF>();
		//class MMF
		//{
		//	public MMF() { Uids = new Stack<Guid>(new Guid[] { Guid.NewGuid() }); Context = string.Empty; }
		//	public Stack<Guid> Uids { get; set; }
		//	public string Context { get; set; }
		//}
		///// <summary>
		///// Get the <see cref="MMF"/> object specific to the current thread if available,
		///// a newly created one if unavailable
		///// </summary>
		///// <returns>a <see cref="MMF"/> object</returns>
		//static MMF GetMmf()
		//{
		//	MMF mmf = new MMF();
		//	lock (mylock)
		//	{
		//		try
		//		{
		//			if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
		//				mmf = Mmfs[Thread.CurrentThread.ManagedThreadId];
		//		}
		//		catch (Exception) { }
		//	}
		//	return mmf;
		//}
		///// <summary>
		///// Pop the last added data
		///// </summary>
		//internal static void PopMmf()
		//{
		//	lock (mylock)
		//	{
		//		try
		//		{
		//			if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
		//				Mmfs[Thread.CurrentThread.ManagedThreadId].Uids.Pop();
		//		}
		//		catch (Exception) { }
		//	}
		//	try
		//	{
		//		if (0 == Mmfs[Thread.CurrentThread.ManagedThreadId].Uids.Count)
		//			Mmfs.Remove(Thread.CurrentThread.ManagedThreadId);
		//	}
		//	catch (Exception) { }
		//}
		///// <summary>
		///// Add a new memory data
		///// </summary>
		///// <returns>
		///// true if the data has been pushed,
		///// false otherwise
		///// </returns>
		//internal static void PushMmf()
		//{
		//	lock (mylock)
		//	{
		//		try
		//		{
		//			if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
		//				Mmfs[Thread.CurrentThread.ManagedThreadId].Uids.Push(Mmfs[Thread.CurrentThread.ManagedThreadId].Uids.Peek());
		//			else
		//				Mmfs.Add(Thread.CurrentThread.ManagedThreadId, new MMF());
		//		}
		//		catch (Exception) { }
		//	}
		//}
		///// <summary>
		///// Specific <see cref="Guid"/> to use when logging data.
		///// This value is set per thread.
		///// </summary>
		///// <returns>a GUID</returns>
		//public static Guid SharedGuid
		//{
		//	get => GetMmf().Uids.Peek();
		//	set
		//	{
		//		try
		//		{
		//			if (Guid.NewGuid() == value || default == value)
		//				PopMmf();
		//			else
		//				PushMmf();
		//		}
		//		catch (Exception) { }
		//	}
		//}
		//public static Guid SetNewSharedGuid() => SharedGuid = Guid.NewGuid();
		///// <summary>
		///// Specific context string to use when logging data.
		///// This value is set per thread.
		///// </summary>
		//public static string SharedContext
		//{
		//	get => GetMmf().Context;
		//	set
		//	{
		//		lock (mylock)
		//		{
		//			try
		//			{
		//				if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
		//					Mmfs[Thread.CurrentThread.ManagedThreadId].Peek().Context = value;
		//				else
		//					Mmfs.Add(Thread.CurrentThread.ManagedThreadId, new Stack<MMF>(new MMF[] { new MMF() { Context = value } }));
		//			}
		//			catch (Exception) { }
		//		}
		//	}
		//}
		//#endregion

		#region per thread shared memory management
		/// <summary>
		/// Dictionary of memory objects indexed by a key
		/// </summary>
		static Dictionary<int, MMF> Mmfs = new Dictionary<int, MMF>();
		internal class MMF
		{
			public MMF() { Uid = Guid.NewGuid(); Context = string.Empty; }
			public Guid Uid { get; set; }
			public string Context { get; set; }
		}
		/// <summary>
		/// Get the <see cref="MMF"/> object specific to the current thread if available,
		/// a newly created one if unavailable
		/// </summary>
		/// <returns>a <see cref="MMF"/> object</returns>
		static MMF GetMmf()
		{
			MMF mmf = new MMF();
			lock (mylock)
			{
				try
				{
					if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
						mmf = Mmfs[Thread.CurrentThread.ManagedThreadId];
				}
				catch (Exception) { }
			}
			return mmf;
		}
		/// <summary>
		/// Specific <see cref="Guid"/> to use when logging data.
		/// This value is set per thread.
		/// </summary>
		/// <returns>a GUID</returns>
		public static Guid SharedGuid
		{
			get => GetMmf().Uid;
			set
			{
				lock (mylock)
				{
					try
					{
						if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
						{
							Mmfs[Thread.CurrentThread.ManagedThreadId].Uid = value;
						}
						else if (Guid.Empty == value || default == value)
						{
							Mmfs.Remove(Thread.CurrentThread.ManagedThreadId);
						}
						else
						{
							Mmfs.Add(Thread.CurrentThread.ManagedThreadId, new MMF());
						}
					}
					catch (Exception) { }
				}
			}
		}
		public static Guid SetNewSharedGuid() => SharedGuid = Guid.NewGuid();
		/// <summary>
		/// Indicates whether a shared guid has been set or not (apart from getting a value)
		/// </summary>
		public static bool SharedGuidHasBeenSet
		{
			get
			{
				lock (mylock)
				{
					try
					{
						return Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId);
					}
					catch (Exception) { }
				}
				return false;
			}
		}
		/// <summary>
		/// Specific context string to use when logging data.
		/// This value is set per thread.
		/// </summary>
		public static string SharedContext
		{
			get => GetMmf().Context;
			set
			{
				lock (mylock)
				{
					try
					{
						if (Mmfs.ContainsKey(Thread.CurrentThread.ManagedThreadId))
							Mmfs[Thread.CurrentThread.ManagedThreadId].Context = value;
						else
							Mmfs.Add(Thread.CurrentThread.ManagedThreadId, new MMF() { Context = value });
					}
					catch (Exception) { }
				}
			}
		}
		#endregion

		#region methods
		public static string DEBUG(string s) => Add(s, TLog.DEBUG);
		public static string DEBUG(List<string> ls) => Add(ls, TLog.DEBUG);
		public static string INFOR(string s) => INFORMATION(s);
		public static string INFORMATION(string s) => Add(s, TLog.INFOR);
		public static string INFOR(List<string> ls) => INFORMATION(ls);
		public static string INFORMATION(List<string> ls) => Add(ls, TLog.INFOR);
		public static string TRACE(string s) => Add(s, TLog.TRACE);
		public static string TRACE(List<string> ls) => Add(ls, TLog.TRACE);
		public static string WARNG(string s) => WARNING(s);
		public static string WARNING(string s) => Add(s, TLog.WARNG);
		public static string WARNG(List<string> ls) => WARNING(ls);
		public static string WARNING(List<string> ls) => Add(ls, TLog.WARNG);
		public static string ERROR(string s) => Add(s, TLog.ERROR);
		public static string ERROR(List<string> ls) => Add(ls, TLog.ERROR);
		public static string EXCPT(Exception ex, string s = default) => EXCEPT(ex, s);
		public static string EXCEPT(Exception ex, string s = default) => AddException(ex, s);
		/// <summary>
		/// Test if a severity is within bounds
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool IsTLog(TLog value) => (TLog._begin < value && value < TLog._end);
		/// <summary>
		/// Log a message to the log file
		/// </summary>
		/// <param name="s">Message to log</param>
		/// <param name="severity">severity level</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string Add(string s, TLog severity = TLog.TRACE) => AddEx(new List<string>() { s }, severity);
		/// <summary>
		/// Log a message to the log file
		/// </summary>
		/// <param name="ls">A list of message to log</param>
		/// <param name="severity">severity level</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string Add(List<string> ls, TLog severity = TLog.TRACE) => Add(new CLogMsgs(ls, severity));
		/// <summary>
		/// Log a list of messages to the log file
		/// </summary>
		/// <param name="ls">A list of <see cref="CLogMsg"/> to log</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string Add(CLogMsgs ls) => AddEx(ls);
		/// <summary>
		/// Log an exception to the log file (the whole exception tree is written)
		/// </summary>
		/// <param name="ex">exception to log</param>
		/// <param name="msg">message to complete the log entry</param>
		/// <param name="addSharedData">allows indicating whether shared data must be added to the logged message or not</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		static string AddException(Exception ex, string msg, bool addSharedData = true)
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
				//for (int i = st.FrameCount; 0 != i; i--)
				//{
				//	StackFrame sf = st.GetFrame(i - 1);
				//	string f = string.IsNullOrEmpty(sf.GetFileName()) ? "??" : sf.GetFileName();
				//	string m = string.IsNullOrEmpty(sf.GetMethod().ToString()) ? "??" : $"{sf.GetMethod()}";
				//	ls.Add($"[EXCEPTION #{st.FrameCount - i + 1}] file: {f}; method: {m}; #line: {sf.GetFileLineNumber()}");
				//}
				for (int i = 0; i < st.FrameCount; i++)
				{
					StackFrame sf = st.GetFrame(i);
					string f = string.IsNullOrEmpty(sf.GetFileName()) ? "??" : sf.GetFileName();
					string m = string.IsNullOrEmpty(sf.GetMethod().ToString()) ? "??" : $"{sf.GetMethod()}";
					ls.Add($"[EXCEPTION #{i + 1}] file: {f}; method: {m}; #line: {sf.GetFileLineNumber()}");
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
		static string AddEx(CLogMsgs ls, bool addSharedData = true)
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
		static string AddEx(List<string> ls, TLog severity, bool addSharedData = true)
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
		static void AddToLog(string s)
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
		static void AddToLogUnsafe(string s)
		{
			try
			{
				streamWriter?.WriteLine(s);
			}
			catch (Exception) { }
		}
		/// <summary>
		/// Convert a list of string to a string
		/// </summary>
		/// <param name="l"></param>
		/// <returns></returns>
		public static string StringListToString(List<string> l)
		{
			string s = string.Empty;
			for (int i = 1; i <= l.Count; i++) s += l[i - 1] + (l.Count == i ? string.Empty : LinesSeparator);
			return s;
		}
		#endregion

		#region log file management
		/// <summary>
		/// Open the log file
		/// </summary>
		static bool OpenLogFile(string fileName)
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
								 $"+++++ {LogFileName.ToUpper()} OPENED: {CMisc.BuildDate(_dateFormat, CreatedOn)} (EXE VERSION: {CMisc.Version(CMisc.VersionType.executable)}/{CMisc.Version(CMisc.VersionType.assemblyFile)} - COMMON VERSION: {CMisc.Version(CMisc.VersionType.assembly, Assembly.GetExecutingAssembly())}/{CMisc.Version(CMisc.VersionType.assemblyFile, Assembly.GetExecutingAssembly())})",
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
		static string BuildLogFileName(string fileName)
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
		static void CloseLogFile()
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
		static void PurgeFiles(int numerberOfFilesToKeep)
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
