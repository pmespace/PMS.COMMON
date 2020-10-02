using System.Runtime.InteropServices;

namespace COMMON
{
	[ComVisible(false)]
	public abstract class CStreamBase
	{
		#region constructor
		public CStreamBase() { LengthBufferSize = CMisc.FOURBYTES; }
		public CStreamBase(int lengthBufferSize) { LengthBufferSize = lengthBufferSize; }
		#endregion constructor

		#region properties
		/// <summary>
		/// Size of buffer containg the size of a message
		/// </summary>
		public int LengthBufferSize
		{
			get => _lengthbuffersize;
			private set
			{
				if (CMisc.ONEBYTE == value
					|| CMisc.TWOBYTES == value
					|| CMisc.FOURBYTES == value
					|| CMisc.EIGHTBYTES == value)
					_lengthbuffersize = value;
			}
		}
		private int _lengthbuffersize = CMisc.FOURBYTES;
		#endregion properties
	}
}
