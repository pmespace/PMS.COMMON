using System.Runtime.InteropServices;
using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace COMMON
{
	/// <summary>
	/// Various useful strings
	/// </summary>
	[ComVisible(false)]
	public static class Chars
	{
		public const string CR = "\r";
		public const string LF = "\n";
		public const string TAB = "\t";
		public const string CRLF = "\r\n";
		public const string DATE = "yyyy-MM-dd";
		public const string TIME = "HH:mm:ss";
		public const string TIMEEX = "HH:mm:ss.fff";
		public const string DATETIME = DATE + " " + TIME;
		public const string DATETIMEEX = DATE + " " + TIMEEX;
	}

	[ComVisible(true)]
	public enum TLog
	{
		INFOR,
		WARNG,
		ERROR,
		EXCPT,
	}

	/// <summary>
	/// Provides an easy to use log service
	/// </summary>
	[ComVisible(false)]
	public static class CLog
	{
		#region constructors
		static CLog() { }
		#endregion

		#region const 
		private static string EXTENSION = ".log";
		/// <summary>
		/// Value to use to indicate no file will be purged
		/// </summary>
		public static readonly int KEEP_ALL_FILES = -1;
		#endregion

		#region properties
		private static Object mylock = new Object();
		/// <summary>
		/// The date the file was created
		/// </summary>
		private static DateTime CreatedOn { get; set; }
		/// <summary>
		/// Original fname used to create the log file
		/// </summary>
		private static string OriginalFName { get; set; } = string.Empty;
		/// <summary>
		/// Original name (given by the log file creator) of the log file, without its extension
		/// </summary>
		private static string OriginalFNameWithoutExtension { get; set; } = string.Empty;
		/// <summary>
		/// Full name of log file
		/// </summary>
		public static string LogFileName
		{
			get => _logfilename;
			set
			{
				lock (mylock)
				{
					// if a log file is still in use close it
					if (!string.IsNullOrEmpty(_logfilename) && (OriginalFName != value || string.IsNullOrEmpty(value)))
					{
						CloseLogFile();
					}
					// if a file is to be created, do it
					if (OriginalFName != value)
					{
						// save new file name root
						OriginalFName = value;
						if (!string.IsNullOrEmpty(value))
							OpenLogFile();
					}
				}
			}
		}
		private static string _logfilename = string.Empty;
		/// <summary>
		/// Path of the log file, without the log file name.
		/// It always ends with "\"
		/// </summary>
		public static string LogFilePath
		{
			get => _logfilepath;
			private set
			{
				lock (mylock)
				{
					if (!string.IsNullOrEmpty(value) && '\\' != value[value.Length - 1])
						value += @"\";
					_logfilepath = value;
				}
			}
		}
		private static string _logfilepath = string.Empty;
		/// <summary>
		/// Indicates whether autopurge previous log file when opening a new one
		/// </summary>
		public static bool AutoPurgeLogFiles { get; set; } = false;
		/// <summary>
		/// Indicates the number of files to keep if <see cref="AutoPurgeLogFiles"/> is set to true
		/// </summary>
		public static int NumberOfFilesToKeep { get; set; } = KEEP_ALL_FILES;
		#endregion

		#region methods
		/// <summary>
		/// Log a message to the log file
		/// </summary>
		/// <param name="s">message to log</param>
		/// <param name="severity">severity level</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string Add(string s, TLog severity = TLog.INFOR)
		{
			if (TLog.INFOR > severity || TLog.EXCPT < severity)
				severity = TLog.INFOR;
			lock (mylock)
			{
				// check whether need to open a new file or not
				if (DateTime.Now.Date != CreatedOn.Date)
					// open a new file
					OpenLogFile();
				// arrived here the file is ready for write, write what was meant to be written
				return AddToLog(s, severity);
			}
		}
		/// <summary>
		/// Log an exception to the log file (the whole exception tree is written)
		/// </summary>
		/// <param name="method">calling method</param>
		/// <param name="ex">exception to log</param>
		/// <param name="msg">message to complete the log entry</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		public static string AddException(string method, Exception ex, string msg = null)//, string header = null, string trailer = null)
		{
			string s = Add("*** [" + method + "] - EXCEPTION: " + ex.GetType().Name + " - " + ex.Message + (!string.IsNullOrEmpty(msg) ? " - " + msg : string.Empty), TLog.EXCPT);
			if (null != ex.InnerException)
				AddException(method, ex.InnerException);
			return s;
		}
		/// <summary>
		/// Build the name of the log file, setting properties accordingly
		/// </summary>
		/// <returns>Complete log file name</returns>
		private static string BuildFileName()
		{
			FileInfo fi = new FileInfo(OriginalFName);
			OriginalFNameWithoutExtension = Path.GetFileNameWithoutExtension(fi.FullName);
			LogFilePath = Path.GetDirectoryName(fi.FullName);
			CreatedOn = DateTime.Now;
			return LogFilePath + OriginalFNameWithoutExtension + "-" + BuildDate(dateFormats.YYYYMMDD, CreatedOn) + EXTENSION;
		}
		/// <summary>
		/// Build a date to a specied format
		/// </summary>
		/// <param name="fmt">format to use to build the date</param>
		/// <param name="dt">date to use to build the date</param>
		/// <returns>A string representing the date in the desired format</returns>
		private static string BuildDate(dateFormats fmt, DateTime dt = default(DateTime))
		{
			// if no date was specified, use the current date
			if (default(DateTime) == dt)
				dt = DateTime.Now;
			// build the date with requested format
			switch (fmt)
			{
				case dateFormats.YYYYMMDD:
					return dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00");
				case dateFormats.YYYYMMDDWithSeparators:
					return dt.Year.ToString("0000") + "/" + dt.Month.ToString("00") + "/" + dt.Day.ToString("00");
				case dateFormats.YYYYMMDDhhmmss:
					return dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00") + dt.Hour.ToString("00") + dt.Minute.ToString("00") + dt.Second.ToString("00");
				case dateFormats.YYYYMMDDhhmmssWithSeparators:
					return dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00") + " " + dt.Hour.ToString("00") + ":" + dt.Minute.ToString("00") + ":" + dt.Second.ToString("00");
				case dateFormats.YYYYMMDDhhmmssmmmWithSeparators:
					return dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00") + " " + dt.Hour.ToString("00") + ":" + dt.Minute.ToString("00") + ":" + dt.Second.ToString("00") + dt.Millisecond.ToString("000");
				default:
					return string.Empty;
			}
		}
		private enum dateFormats
		{
			YYYYMMDD,
			YYYYMMDDWithSeparators,
			YYYYMMDDhhmmss,
			YYYYMMDDhhmmssWithSeparators,
			YYYYMMDDhhmmssmmmWithSeparators
		}
		/// <summary>
		/// Remove unwanted chars (CR, LF, TAB) from a string
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		private static string RemoveCRLF(string s)
		{
			s = s.Replace(Chars.CR, " ");
			s = s.Replace(Chars.LF, " ");
			s = s.Replace(Chars.TAB, " ");
			return s;
		}
		/// <summary>
		/// Actually write message to the log file
		/// </summary>
		/// <param name="s"></param>
		/// <param name="severity">severity level</param>
		/// <returns>The string as it has been written, null if an error has occurred</returns>
		private static string AddToLog(string s, TLog severity)
		{
			string v = BuildDate(dateFormats.YYYYMMDDhhmmssWithSeparators) + " - " + severity.ToString() + " - " + Thread.CurrentThread.ManagedThreadId.ToString("X8") + " - " + RemoveCRLF(s.Trim());
			try
			{
				if (!string.IsNullOrEmpty(LogFileName))
				{
					using (StreamWriter file = new StreamWriter(LogFileName, true, Encoding.UTF8))
						file.WriteLine(v);
				}
			}
			catch (Exception)
			{ }
			return string.IsNullOrEmpty(s) ? string.Empty : s;
		}
		/// <summary>
		/// Open the log file
		/// </summary>
		private static void OpenLogFile()
		{
			// only if a file open has been requested
			CloseLogFile();
			try
			{
				// try to open the file
				string name = BuildFileName();
				using (StreamWriter LogFile = new StreamWriter(name, true, Encoding.UTF8))
				{
					// the file has been opened, get its attributes
					FileInfo fi = new FileInfo(name);
					LogFilePath = fi.DirectoryName;
					// this method is called when LogFileName, use the private data instead
					_logfilename = fi.FullName;
				}
				AddToLog("+++++", TLog.INFOR);
				AddToLog("+++++ " + LogFileName.ToUpper() + " OPENED: " + BuildDate(dateFormats.YYYYMMDDhhmmssWithSeparators, CreatedOn) + " (VERSION: " + CMisc.Version(CMisc.VersionType.assembly) + "-" + CMisc.Version(CMisc.VersionType.assemblyFile) + "-" + CMisc.Version(CMisc.VersionType.assemblyInfo) + ")", TLog.INFOR);
				AddToLog("+++++", TLog.INFOR);
				if (AutoPurgeLogFiles)
					PurgeFiles(NumberOfFilesToKeep);
			}
			catch (Exception)
			{
				CloseLogFile();
			}
		}
		/// <summary>
		/// Close the current log file
		/// </summary>
		private static void CloseLogFile()
		{
			if (!string.IsNullOrEmpty(LogFileName))
			{
				// close current log file
				AddToLog("-----", TLog.INFOR);
				AddToLog("----- " + LogFileName.ToUpper() + " CLOSED: " + BuildDate(dateFormats.YYYYMMDDhhmmssWithSeparators), TLog.INFOR);
				AddToLog("-----", TLog.INFOR);
				_logfilename = string.Empty;
			}
		}
		/// <summary>
		/// Reset the current log file (keeping context to reopen it)
		/// </summary>
		private static void ResetLogFile()
		{
			CloseLogFile();
			OpenLogFile();
		}
		/// <summary>
		/// Delete the current log file
		/// </summary>
		public static void DeleteLogFile()
		{
			lock (mylock)
			{
				CloseLogFile();
				try
				{
					DirectoryInfo fi = new DirectoryInfo(LogFileName);
					fi.Delete();
				}
				catch (Exception)
				{
				}
			}
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
					FileSystemInfo[] files = di.GetFileSystemInfos(OriginalFNameWithoutExtension + "*" + EXTENSION);
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
