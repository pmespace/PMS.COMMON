using System;
using System.Data;
using System.Data.OleDb;
using System.Reflection;

namespace COMMON
{
	public abstract class CDatabaseBase
	{
		#region constructors
		public CDatabaseBase() { }
		public CDatabaseBase(string connectString) { ConnectionString = connectString; }
		#endregion

		#region properties
		/// <summary>
		/// The database connection object
		/// </summary>
		public OleDbConnection Database { get => _dbconnection; private set => _dbconnection = value; }
		private OleDbConnection _dbconnection = new OleDbConnection();
		/// <summary>
		/// ConnectionString string to connect to the database
		/// </summary>
		public string ConnectionString
		{
			get => Database.ConnectionString;
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					if (IsOpen)
					{
						DoDisconnect();
					}
					Database.ConnectionString = value;
					DoConnect();
				}
			}
		}
		/// <summary>
		/// Indicate whether the connection to the database is open or not and allows to connect to or disconnect from the database
		/// </summary>
		public bool IsOpen
		{
			get => null != Database ? ConnectionState.Open == Database.State : false;
			set
			{
				if (value)
					DoConnect();
				else
					DoDisconnect();
			}
		}
		#endregion

		#region database management methods
		/// <summary>
		/// Manage the connection state and behaviour
		/// </summary>
		/// <returns></returns>
		private bool DoConnect()
		{
			try
			{
				if (DoDisconnect())
				{
					try
					{
						Database.Open();
						return true;
					}
					catch (Exception ex)
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Connection string: " + ConnectionString);
					}
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
			}
			return false;
		}
		/// <summary>
		/// Disconnect from the database
		/// </summary>
		/// <returns><see langword="true"/>if disconnected, false otherwise</returns>
		private bool DoDisconnect()
		{
			if (IsOpen)
			{
				try
				{
					Database.Close();
					return true;
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, Database.Database + " still open");
				}
			}
			return !IsOpen;
		}
		#endregion
	}
}
