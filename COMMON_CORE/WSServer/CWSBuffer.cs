using System;
using System.Net.WebSockets;
using System.Text;
using COMMON;

namespace COMMON.WSServer
{
	/// <summary>
	/// An object describing a buffer received by the <see cref="CWSServer"/> object.
	/// </summary>
	public class CWSBuffer
	{
		#region constructor
		public CWSBuffer() { Reset(); }
		#endregion

		#region properties
		/// <summary>
		/// Buffer itself
		/// </summary>
		public object Data { get => default != request ? (default == brequest ? null : brequest) : request; }
		/// <summary>
		/// Length of buffer
		/// </summary>
		public int Length { get => IsBinary ? brequest.Length : request.Length; }
		/// <summary>
		/// True if the buffer is binary (byte[]), false if text (string)
		/// </summary>
		public bool IsBinary { get => default != brequest; }
		#endregion

		#region private
		string request;
		byte[] brequest;
		#endregion

		#region methods
		public override string ToString() => IsBinary ? CMisc.AsHexString(Data as byte[]) : Data.ToString();
		/// <summary>
		/// Reset buffer.
		/// This must be called each timethe buffer has been used.
		/// </summary>
		internal void Reset()
		{
			request = default;
			brequest = default;
		}
		/// <summary>
		/// Update the WS server buffer from the received data
		/// </summary>
		/// <param name="res"><see cref="WebSocketReceiveResult"/> object describing what happened when reading data</param>
		/// <param name="ab">The received buffer</param>
		/// <returns>
		/// true if data has been received and saved,
		/// false if it was a close message or an error has occurred
		/// </returns>
		internal bool Populate(WebSocketReceiveResult res, byte[] ab)
		{
			try
			{
				if (ab.IsNullOrEmpty()) ab = new byte[0];
				if (WebSocketMessageType.Text == res.MessageType)
				{
					request += Encoding.UTF8.GetString(ab, 0, res.Count);
					return true;
				}
				else if (WebSocketMessageType.Binary == res.MessageType)
				{
					if (default == brequest) brequest = new byte[0];
					byte[] tmp = new byte[brequest.Length + res.Count];
					Buffer.BlockCopy(brequest, 0, tmp, 0, brequest.Length);
					Buffer.BlockCopy(ab, 0, tmp, brequest.Length, res.Count);
					brequest = tmp;
					return true;
				}
			}
			catch (Exception _ex_)
			{
				CLog.EXCEPT(_ex_);
			}
			// close or any other reason
			return false;
		}
		#endregion
	}
}