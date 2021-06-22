using System.Reflection;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace COMMON
{
	[ComVisible(true)]
	public enum ActivityEnum
	{
		_none,
		starting,
		stopping,
		information,
		message,
		reset,
		timeout,
		requestSent,
		requestReceived,
		replySent,
		replyReceived,
		error,
		exception,
		_lastpredefined = exception,
	}

	[ComVisible(true)]
	[Guid("FDD924E8-4AA8-4F8A-9302-51E9CE26D07C")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IWin32Activity
	{
		#region IWin32Activity
		[DispId(1)]
		Control Ctrl { get; set; }
		[DispId(2)]
		ActivityEnum Evt { get; set; }
		[DispId(3)]
		string Message { get; set; }
		[DispId(4)]
		object Value { get; set; }

		[DispId(100)]
		string ToString();
		#endregion
	}
	/// <summary>
	/// CWin32Activity class describing what is going to be done
	/// An object of that class is passed to the UICActivity processing function
	/// </summary>
	[Guid("BB58CB42-9E91-4472-B1DB-F6A0DE9E64AE")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CWin32Activity : IWin32Activity
	{
		/// <summary>
		/// The <see cref="Control"/> to use to invoke the UI method
		/// </summary>
		public Control Ctrl { get; set; } = null;
		/// <summary>
		/// The type of activity to pass to the activity processing method
		/// <see cref="ActivityEnum"/> values are for information, it is possible to extend the range as desired
		/// </summary>
		public ActivityEnum Evt { get; set; } = 0;
		/// <summary>
		/// A message to pass to the activity processing method
		/// </summary>
		public string Message { get; set; } = null;
		/// <summary>
		/// An object to pass to the activity processing method
		/// </summary>
		public object Value { get; set; } = null;
		/// <summary>
		/// Striing description of the activity
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"CWin32Activity = Control: {Ctrl} - Event: {Evt} - Message: {Message} - Value: {Value}";
		}
	}

	/// <summary>
	/// Static class allowing to pass UI activities when inside a thread and Windows Controls can't be reached safely
	/// Use this object to easily invoke UI processing from a thread
	/// </summary>
	public static class Win32Activity
	{
		#region delegates
		public delegate void Win32ActivityDelegate(CWin32Activity activity);
		#endregion

		#region methods
		/// <summary>
		/// Calls the <see cref="Win32ActivityDelegate"/> method which is in charge of taking care of the activity, passing it the arguments
		/// </summary>
		/// <param name="method">The <see cref="Win32ActivityDelegate"/> method to call</param>
		/// <param name="activity">The <see cref="CWin32Activity"/> object to pass to the method</param>
		public static void AddActivity(Win32ActivityDelegate method, CWin32Activity activity)
		{
			try
			{
				if (null != method && null != activity && null != activity.Ctrl)
				{
					activity.Ctrl.Invoke(method, activity);
				}
				else
				{
					CLog.Add($"{activity} could not be added", TLog.ERROR);
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, $"Exception while processing {activity}");
			}
		}
		#endregion
	}
}
