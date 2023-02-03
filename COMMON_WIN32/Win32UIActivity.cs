using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace COMMON.WIN32
{
	/// <summary>
	/// Pre-defined activities
	/// </summary>
	[ComVisible(false)]
	public enum UIActivityEnum
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

	//[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	//[Guid("FDD924E8-4AA8-4F8A-9302-51E9CE26D07C")]
	[ComVisible(false)]
	public interface IUIActivity
	{
		#region IUIActivity
		[DispId(1)]
		Control Ctrl { get; set; }
		[DispId(2)]
		UIActivityEnum Evt { get; set; }
		[DispId(3)]
		string Message { get; set; }
		[DispId(4)]
		object Value { get; set; }

		[DispId(100)]
		string ToString();
		#endregion
	}
	/// <summary>
	/// UIActivity class describing an activity that can be passed from a child thread to the UI .NET Framework thread allowing to interract with the UI without crashing the application
	/// To use this object create a "private void UIProcessing(Win32UIActivity activity) { switch (activity.Evt) { case UIActivityEnum.message:;// do something		}	}
	/// And use <see cref="Win32UIActivity.AddActivity(Win32UIActivity.Win32UIActivityDelegate, UIActivity)"/> to send a notification that function
	/// </summary>
	//[Guid("BB58CB42-9E91-4472-B1DB-F6A0DE9E64AE")]
	//[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(false)]
	public class UIActivity : IUIActivity
	{
		/// <summary>
		/// The <see cref="Control"/> to use to invoke the UI method
		/// </summary>
		public Control Ctrl { get; set; } = default;
		/// <summary>
		/// The type of activity to pass to the activity processing method
		/// <see cref="UIActivityEnum"/> values are for information, it is possible to extend the range as desired
		/// </summary>
		public UIActivityEnum Evt { get; set; } = 0;
		/// <summary>
		/// A message to pass to the activity processing method
		/// </summary>
		public string Message { get; set; } = default;
		/// <summary>
		/// An object to pass to the activity processing method
		/// </summary>
		public object Value { get; set; } = default;
		/// <summary>
		/// Striing description of the activity
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"UIActivity = control: {Ctrl}; event: {Evt}; message: {Message}; value: {Value}";
		}
	}

	/// <summary>
	/// Static class allowing to pass UI activities when inside a thread and Windows Controls can't be reached safely
	/// Use this object to easily invoke UI processing from a thread (<see cref="UIActivity"/> for more details)
	/// </summary>
	[ComVisible(false)]
	public static class Win32UIActivity
	{
		#region delegates
		public delegate void Win32UIActivityDelegate(UIActivity activity);
		#endregion

		#region methods
		/// <summary>
		/// Calls the <see cref="Win32UIActivityDelegate"/> method which is in charge of taking care of the activity, passing it the arguments
		/// </summary>
		/// <param name="method">The <see cref="Win32UIActivityDelegate"/> method to call</param>
		/// <param name="activity">The <see cref="UIActivity"/> object to pass to the method</param>
		public static void AddActivity(Win32UIActivityDelegate method, UIActivity activity)
		{
			try
			{
				if (default != method && default != activity && default != activity.Ctrl)
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
				CLog.EXCEPT(ex, $"while processing {activity}");
			}
		}
		#endregion
	}
}
