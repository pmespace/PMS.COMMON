using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System;

#if NETFRAMEWORK
using COMMON.WIN32;
#endif

namespace COMMON
{
	[ComVisible(true)]
	public enum ThreadResult
	{
		_begin = -1,
		UNKNOWN,
		OK,
		KO,
		Exception,
		_end,
	}

	[ComVisible(true)]
	[Guid("420E0E6B-C6D4-499A-87A7-992FECBFEFC3")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IThread
	{
		#region IThread
		[DispId(5001)]
		bool IsRunning { get; }
		[DispId(5002)]
		int Result { get; }
		[DispId(5003)]
		int UniqueID { get; }
		[DispId(5004)]
		int NoThread { get; }
		//[DispId(5006)]
		//int FinalDelayWhenThreadTerminates { get; set; }
		[DispId(5007)]
		Thread Thread { get; }
		[DispId(5008)]
		int ID { get; set; }
		[DispId(5009)]
		string Name { get; set; }
		[DispId(5010)]
		string Description { get; }
		[DispId(5011)]
		bool HasBeenStarted { get; }
		[DispId(5012)]
		bool CanStart { get; }

		[DispId(5100)]
		bool Wait(int timer = Timeout.Infinite);
#if NETFRAMEWORK
		[DispId(5101)]
		void SendNotification(int value, bool stopped);
#endif
		#endregion
	}
	[Guid("87BB223F-6A59-4592-8A0F-057625532B8C")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CThread : IThread
	{
		#region constructor
		public CThread()
		{
			Initialise();
		}
		private void Initialise()
		{
			Thread = new Thread(Method);
		}
		#endregion

		#region delegates
		/// <summary>
		/// Function called from inside the thread and inside the caller's environement
		/// </summary>
		/// <param name="data"><see cref="CThreadData"/> structure passed by the caller</param>
		/// <param name="o">Parameters passed to the thread</param>
		/// <returns>The result of the function, any muneric value pertaining to the caller. That value will be set inside <see cref="Result"/></returns>
		public delegate int CThreadFunction(CThreadData data = null, object o = null);
		/// <summary>
		/// Function called from inside the thread when the thread ends.
		/// </summary>
		/// <param name="id">This is the <see cref="ID"/> value given to the thread by the calling application</param>
		/// <param name="name">This is the <see cref="Name"/> value given to the thread by the  calling application</param>
		/// <param name="uniqueId">This is the <see cref="UniqueID"/> value given to the thread by the system itself</param>
		/// <param name="result">This is the <see cref="Result"/> of the thread</param>
		public delegate void CThreadHasEnded(int id, string name, int uniqueId, int result);
		#endregion

		#region public properties
		/// <summary>
		/// Indicates whether the thread has ended or not
		/// </summary>
		public bool IsRunning { get; protected set; }
		/// <summary>
		/// Thread final result
		/// </summary>
		public int Result { get; private set; } = (int)ThreadResult.UNKNOWN;
		/// <summary>
		/// The thread unique ID
		/// </summary>
		public int UniqueID { get => null != Thread ? Thread.ManagedThreadId : NO_THREAD; }
		public const int NO_THREAD = 0;
		public int NoThread { get => NO_THREAD; }
		///// <summary>
		///// Timer to wait for when the thread is about to terminate, expressed in milliseconds
		///// The thread terminated event is sent after this timer has gone
		///// </summary>
		//public int FinalDelayWhenThreadTerminates
		//{
		//	get => _finaldelaywhenthreadterminates;
		//	set => _finaldelaywhenthreadterminates = 0 == value ? DEFAULT_FINAL_DELAY_WHEN_THREAD_TERMINATES : value;
		//}
		//private int _finaldelaywhenthreadterminates = DEFAULT_FINAL_DELAY_WHEN_THREAD_TERMINATES;
		//private const int DEFAULT_FINAL_DELAY_WHEN_THREAD_TERMINATES = 5;

		/// <summary>
		/// The thread object itself
		/// </summary>
		public Thread Thread { get; private set; }
		/// <summary>
		/// Parameters used to call the thread
		/// </summary>
		protected CThreadData ThreadData { get; set; }
		/// <summary>
		/// The ID which can be used to identify the thread when it ends.
		/// The ID will be returned to the warned window (through a PostMessage), when the thread ends, inside the wParam
		/// </summary>
		public int ID
		{
			get => _id;
			set
			{
				if (!HasBeenStarted)
					_id = value;
			}
		}
		private int _id = 0;
		/// <summary>
		/// Thread name
		/// </summary>
		public string Name
		{
			get => string.IsNullOrEmpty(_name) ? @"N/A" : _name;
			set
			{
				if (!HasBeenStarted)
					_name = value;
			}
		}
		private string _name = null;
		/// <summary>
		/// Description of the thread
		/// </summary>
		public string Description { get => "Thread: " + Name + (0 != ID ? " - ID: " + ID : null) + " - "; }
		/// <summary>
		/// Indicate whether the thread has already been started
		/// </summary>
		public bool HasBeenStarted { get; private set; } = false;
		/// <summary>
		/// Indicate whethre the thread can be started
		/// </summary>
		public bool CanStart { get => !IsRunning && !HasBeenStarted; }
		#endregion

		#region private properties
		/// <summary>
		/// Events to use to know if the thread is started or stopped
		/// </summary>
		private CThreadEvents Events = new CThreadEvents();
		/// <summary>
		/// The function to call inside the caller's environement to create the thread
		/// </summary>
		private CThreadFunction ThreadMethod { get; set; }
		private CThreadHasEnded ThreadHasEndedMethod { get; set; } = null;
		private object ThreadParams { get; set; }
		#endregion

		#region public methods
		/// <summary>
		/// Start the thread
		/// </summary>
		/// <param name="method">the method to run inside the thread</param>
		/// <param name="threadData">data used inside the thread to communicate with the caller's environment</param>
		/// <param name="threadParams">Parameters to pass to the thread</param>
		/// <param name="evt">An <see cref="ManualResetEvent"/> object created by the calling application to wait for when starting the thread. This event can be used by the calling application to indicate it has finished its own initialisation process and must therefore be set within the calling applictaioin's thread function to unlock processing. Set to null means no event to wait for.</param>
		/// <param name="isBackground">Indicates whether the created thread is a background one or not</param>
		/// <param name="threadHasEndedMethod">Function that will be called when the thread has ended its processing. This is the very last call inside the thread before it terminates. BEWARE, it is then called from inside the thread environment</param>
		/// <returns>True if started, false otherwise</returns>
		public bool Start(CThreadFunction method, CThreadData threadData = null, object threadParams = null, ManualResetEvent evt = null, bool isBackground = true, CThreadHasEnded threadHasEndedMethod = null)
		{
			if (!CanStart)
				return false;
			if (null != threadData && !threadData.IsValid)
				return false;
			try
			{
				HasBeenStarted = true;
				ThreadData = threadData;
				ThreadMethod = method;
				ThreadHasEndedMethod = threadHasEndedMethod;
				ThreadParams = threadParams;
				Events.Reset();
				// start the thread
				Thread.Start();
				Thread.IsBackground = isBackground;
				Events.WaitStarted();
				if (null != evt)
					evt.WaitOne();
				IsRunning = true;
				return IsRunning;
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
			return false;
		}
		/// <summary>
		/// Allows waiting for the thread to end either indefinitely or some time only
		/// </summary>
		/// <param name="timer">Timer to wait for</param>
		/// <returns>TRUE if the thread has ended, FALSE if it is still running</returns>
		public bool Wait(int timer = Timeout.Infinite)
		{
			if (Events.WaitStopped())
			{
				//// give time to the thread to actually terminate
				//Thread.Sleep(FinalDelayWhenThreadTerminates);
				return true;
			}
			return false;
		}
		#endregion

		#region private methods
		/// <summary>
		/// Internal thread method starting the requested method
		/// </summary>
		private void Method()
		{
			// call the processing method
			Result = (int)ThreadResult.UNKNOWN;
			Events.SetStarted();
			try
			{
				if (null != ThreadMethod)
				{
					Result = ThreadMethod(ThreadData, ThreadParams);
					CLog.Add(Description + "Result: " + Result);
				}
				else
					CLog.Add(Description + "No method to call for the thread", TLog.WARNG);
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				Result = (int)ThreadResult.Exception;
			}
			// indicates the thread is off
			if (null != ThreadData && null != ThreadData.EventToSignal)
				ThreadData.EventToSignal.Set();

#if NETFRAMEWORK
			SendNotification(Result, true);
#endif

			// this MUST be the final statements
			// indicate the thread is finished
			IsRunning = false;
			Events.SetStopped();
			try
			{
				if (null != ThreadHasEndedMethod)
					ThreadHasEndedMethod(ID, Name, UniqueID, Result);
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
		}
#if NETFRAMEWORK
		/// <summary>
		/// Send an event notification to the caller if a window handle has been provided
		/// lParam will be set using the "value"
		/// wParam will be set using the thread ID
		/// </summary>
		/// <param name="stopped">Tru if use <see cref="CThreadData.StoppedMessage"/>, using <see cref="CThreadData.InformationMessage"/> otherwise</param>
		/// <param name="value">Value to send, inside lParam</param>
		public void SendNotification(int value, bool stopped)
		{
			if (null != ThreadData
				&& IntPtr.Zero != ThreadData.WindowToWarn)
				SendNotification(ThreadData, ID, value, stopped);
		}
		/// <summary>
		/// Send an event notification to the caller
		/// </summary>
		/// <param name="threadData">Data to use to send the notification</param>
		/// <param name="id">Thread ID, inside wParam</param>
		/// <param name="value">Value to send, inside lParam</param>
		/// <param name="stopped">Tru if use <see cref="CThreadData.StoppedMessage"/>, using <see cref="CThreadData.InformationMessage"/> otherwise</param>
		public static void SendNotification(CThreadData threadData, int id, int value, bool stopped = true)
		{
			if (null != threadData
				&& IntPtr.Zero != threadData.WindowToWarn)
				Win32.PostMessage(threadData.WindowToWarn, stopped ? threadData.StoppedMessage : threadData.InformationMessage, id, value);
		}
#endif
		#endregion
	}

	/// <summary>
	/// Class used to manage started and stopped flags of a thread
	/// If an application needs to know whether a thread is still on it can use these methods
	/// </summary>
	[ComVisible(false)]
	public class CThreadEvents
	{
		public CThreadEvents()
		{
			Started = new ManualResetEvent(false);
			Stopped = new ManualResetEvent(false);
		}
		public ManualResetEvent Started { get; }
		public ManualResetEvent Stopped { get; }
		public bool SetStarted() { return Started.Set(); }
		public bool SetStopped() { return Stopped.Set(); }
		public bool WaitStarted(int timer = Timeout.Infinite) { return Started.WaitOne(timer); }
		public bool WaitStopped(int timer = Timeout.Infinite) { return Stopped.WaitOne(timer); }
		public bool ResetStarted() { return Started.Reset(); }
		public bool ResetStopped() { return Stopped.Reset(); }
		public bool Reset() { return ResetStarted() && ResetStopped(); }
	}
}
