using System.Runtime.InteropServices;
using System;

namespace COMMON
{
	[ComVisible(true)]
	[Guid("966E7BDC-A560-4988-8AF7-717FF1341E4D")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IStreamClientThreadSettings
	{
		[DispId(1)]
		bool IsValid { get; }
		[DispId(2)]
		object Parameters { get; set; }
		[DispId(3)]
		CThreadData ThreadData { get; set; }
		[DispId(4)]
		CStreamDelegates.ClientOnReplyDelegate OnReply { get; set; }
	}
	[Guid("1BB1B102-74BC-4795-98A1-14DD94D851B6")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CStreamClientThreadSettings: IStreamClientThreadSettings
	{
		#region properties
		public bool IsValid { get => null == ThreadData || ThreadData.IsValid; }
		/// <summary>
		/// Parameters to pass to the <see cref="OnReply"/> function when called from within the thread
		/// </summary>
		public object Parameters { get; set; } = null;
		/// <summary>
		/// Thread data to use to identify the thread
		/// </summary>
		public CThreadData ThreadData { get; set; } = null;
		/// <summary>
		/// Function called from within the thread when a reply is received
		/// </summary>
		public CStreamDelegates.ClientOnReplyDelegate OnReply { get; set; } = null;
		#endregion
	}

	[ComVisible(true)]
	[Guid("FC4ED317-6B75-474A-B51F-4AD71734881F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IStreamServerThreadSettings
	{
		[DispId(1)]
		bool IsValid { get; }
		[DispId(2)]
		object Parameters { get; set; }
		[DispId(3)]
		CThreadData ThreadData { get; set; }
		[DispId(4)]
		CStreamDelegates.ServerOnStartDelegate OnStart { get; set; }
		[DispId(5)]
		CStreamDelegates.ServerOnMessageDelegate OnMessage { get; set; }
		[DispId(6)]
		CStreamDelegates.ServerOnStopDelegate OnStop { get; set; }
	}
	[ComVisible(true)]
	[Guid("56BF8966-E7FF-4B42-A255-A89EF0226FD1")]
	[ClassInterface(ClassInterfaceType.None)]
	public class CStreamServerThreadSettings: IStreamServerThreadSettings
	{
		#region properties
		public bool IsValid { get => null == ThreadData || ThreadData.IsValid; }
		/// <summary>
		/// Parameters to pass to any function when called from within the thread
		/// </summary>
		public object Parameters { get; set; } = null;
		/// <summary>
		/// Thread data to use to identify the thread
		/// </summary>
		public CThreadData ThreadData { get; set; } = null;
		/// <summary>
		/// Called before starting processing requests from a client.
		/// This function allows to initialise the server context.
		/// </summary>
		public CStreamDelegates.ServerOnStartDelegate OnStart { get; set; } = null;
		/// <summary>
		/// Called before starting processing requests from a client.
		/// This function allows to initialise the server context.
		/// </summary>
		public CStreamDelegates.ServerOnConnectDelegate OnConnect { get; set; } = null;
		/// <summary>
		/// Called when a request has been received to process it and prepare the reply
		/// Bytes array request processing function
		/// </summary>
		public CStreamDelegates.ServerOnMessageDelegate OnMessage { get; set; } = null;
		/// <summary>
		/// Called after the server has received a stop order.
		/// This function allows to clear the server context.
		/// </summary>
		public CStreamDelegates.ServerOnStopDelegate OnStop { get; set; } = null;
		#endregion
	}
}
