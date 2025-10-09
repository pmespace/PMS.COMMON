using COMMON.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace COMMON
{
	[ComVisible(false)]
	public abstract class CStreamBase
	{
		//public const int ZEROBYTE = 0;
		public const int ONEBYTE = 1;
		public const int TWOBYTES = 2;
		public const int FOURBYTES = 4;
		public const int EIGHTBYTES = 8;

		#region constructor
		public CStreamBase() { }
		public CStreamBase(CStreamBase sb) { SizeHeader = sb.SizeHeader; UseSizeHeader = sb.UseSizeHeader; }
		#endregion constructor

		#region properties
		/// <summary>
		/// Size of buffer containg the size of a message
		/// </summary>
		public int SizeHeader
		{
			get => _lengthbuffersize;
			set
			{
				if (IsHeaderBytes(value))
					_lengthbuffersize = value;
			}
		}
		private int _lengthbuffersize = FOURBYTES;
		/// <summary>
		/// Indicates whether a header bytes of size <see cref="CStreamBase.SizeHeader"/> must be added when sending a message or not
		/// </summary>
		public bool UseSizeHeader { get => _addheaderbytes; set => _addheaderbytes = value; }
		bool _addheaderbytes = true;
		#endregion

		#region methods
		public override string ToString()
		{
			return Resources.CStreamToString.Format(new object[] { SizeHeader, UseSizeHeader });
		}
		public static bool IsHeaderBytes(int value) { return (/*ZEROBYTE == value ||*/ ONEBYTE == value || TWOBYTES == value || FOURBYTES == value || EIGHTBYTES == value); }
		#endregion
	}
}
