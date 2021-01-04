using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data;

namespace COMMON
{
	[ComVisible(false)]
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
					try
					{
						Database.ConnectionString = value;
						DoConnect();
					}
					catch (Exception ex)
					{
						CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Connection string: " + ConnectionString);
					}
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

		#region methods
		/// <summary>
		/// Return the value to use to test TRUE in SQL
		/// (ACCESS has a different way to use these values)
		/// That function MUST NOT be used to feed an <see cref="OleDbParameter"/>
		/// </summary>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public string TRUE()
		{
			return TRUE(Database);
		}
		/// <summary>
		/// Return the value to use to test FALSE in SQL
		/// (ACCESS has a different way to use these values)
		/// That function MUST NOT be used to feed an <see cref="OleDbParameter"/>
		/// </summary>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public string FALSE()
		{
			return FALSE(Database);
		}
		/// <summary>
		/// Return the value to use to test TRUE in SQL
		/// (ACCESS has a different way to use these values)
		/// That function MUST NOT be used to feed an <see cref="OleDbParameter"/>
		/// </summary>
		/// <param name="db">The database to use</param>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public static string TRUE(OleDbConnection db)
		{
			return TrueFalseDBValue(db, true);
		}
		/// <summary>
		/// Return the value to use to test TRUE in SQL
		/// (ACCESS has a different way to use these values)
		/// That function MUST NOT be used to feed an <see cref="OleDbParameter"/>
		/// </summary>
		/// <param name="db">The database to use</param>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public static string FALSE(OleDbConnection db)
		{
			return TrueFalseDBValue(db, false);
		}
		/// <summary>
		/// Return the value to use to test TRUE or FALSE
		/// - ACCESS has a different way to use these values
		/// That function MUST NOT be used to feed an <see cref="OleDbParameter"/>
		/// </summary>
		/// <param name="db">The database to use</param>
		/// <param name="value">The value to set</param>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public static string TrueFalseDBValue(OleDbConnection db, bool value)
		{
			if (ConnectionState.Open == db.State)
			{
				if (db.Provider.Contains("Microsoft.ACE.OLEDB"))
				{
					return value.ToString();
				}
				else
				{
					return $"'{value}'";
				}
			}
			return null;
		}
		#endregion
	}

	[ComVisible(false)]
	public class CDatabase : CDatabaseBase
	{
		#region constructors
		public CDatabase() { }
		public CDatabase(string connectString) : base(connectString) { }
		#endregion

		#region properties
		#endregion

		#region delegates
		/// <summary>
		/// Delegate function called to feed a record from s SELECT command
		/// </summary>
		/// <param name="reader">Reader to use to feed a record</param>
		/// <returns>An fed record is successfull, null otherwise</returns>
		public delegate object FeedRecordDelegate(OleDbDataReader reader);
		#endregion

		#region data management methods
		/// <summary>
		/// Execute a non select request (insert, update, delete)
		/// </summary>
		/// <param name="command">A complete <see cref="OleDbCommand"/> detailing a non select request to launch</param>
		/// <param name="nbRows">Number of rows impacted by the request</param>
		/// <returns>True if the request was successfull processed, false otherwise</returns>
		public bool NonSelectRequest(OleDbCommand command, ref int nbRows)
		{
			nbRows = 0;
			if (IsOpen && null != command)
			{
				try
				{
					command.Connection = Database;
					nbRows = command.ExecuteNonQuery();
					return true;
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "SQL: " + command.CommandText);
				}
				finally
				{
					command.Dispose();
				}
			}
			return false;
		}
		/// <summary>
		/// Execute a non select request (insert, update, delete)
		/// </summary>
		/// <param name="sql">A non select request to launch</param>
		/// <param name="nbRows">Number of rows impacted by the request</param>
		/// <returns>True if the request was successfull processed, false otherwise</returns>
		public bool NonSelectRequest(string sql, ref int nbRows)
		{
			nbRows = 0;
			if (IsOpen && !string.IsNullOrEmpty(sql))
			{
				sql = sql.Replace("=true", $"={TRUE()}", StringComparison.OrdinalIgnoreCase).Replace("=false", $"={FALSE()}", StringComparison.OrdinalIgnoreCase);
				CLog.Add($"SQL: {sql}");
				return NonSelectRequest(new OleDbCommand(sql), ref nbRows);
			}
			return false;
		}
		/// <summary>
		/// Selects a set of objects from the database and returns them inside a <see cref="OleDbDataAdapter"/> object to be extracted by the caller
		/// </summary>
		/// <param name="command">A complete <see cref="OleDbCommand"/> detailing a select request to launch</param>
		/// <param name="reader">An <see cref="OleDbDataReader"/> object which can be used to fetch data from the result set</param>
		/// <returns>True if successfull, false otherwise</returns>
		public bool SelectRequest(OleDbCommand command, ref OleDbDataReader reader)
		{
			reader = null;
			bool f = false;
			if (IsOpen && null != command)
			{
				try
				{
					command.Connection = Database;
					reader = command.ExecuteReader();
					f = true;
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				}
				finally
				{
					command.Dispose();
				}
			}
			return f;
		}
		/// <summary>
		/// Selects a set of objects from the database and returns them inside a <see cref="OleDbDataAdapter"/> object to be extracted by the caller
		/// </summary>
		/// <param name="sql">A select request to launch</param>
		/// <param name="reader">An <see cref="OleDbDataReader"/> object which can be used to fetch data from the result set</param>
		/// <returns>True if successfull, false otherwise</returns>
		public bool SelectRequest(string sql, ref OleDbDataReader reader)
		{
			reader = null;
			if (IsOpen && !string.IsNullOrEmpty(sql))
			{
				sql = sql.Replace("=true", $"={TRUE()}", StringComparison.OrdinalIgnoreCase).Replace("=false", $"={FALSE()}", StringComparison.OrdinalIgnoreCase);
				CLog.Add($"SQL: {sql}");
				return SelectRequest(new OleDbCommand(sql), ref reader);
			}
			return false;
		}
		/// <summary>
		/// Launch a select request and feed a list of records fetched using the request
		/// </summary>
		/// <param name="command">A complete <see cref="OleDbCommand"/> detailing a select request to launch</param>
		/// <param name="feedRecordFunction">Functions called to feed a TnX object</param>
		/// <returns>A list of records fetched using the select request, null if an error has occurred</returns>
		public List<TnX> SelectRequest<TnX>(OleDbCommand command, FeedRecordDelegate feedRecordFunction)
		{
			List<TnX> l = new List<TnX>();
			OleDbDataReader reader = null;
			if (SelectRequest(command, ref reader))
			{
				while (reader.Read())
				{
					TnX record = (TnX)feedRecordFunction(reader);
					if (null != record)
						l.Add(record);
				}
				return l;
			}
			return null;
		}
		/// <summary>
		/// Launch a select request and feed a list of records fetched using the request
		/// </summary>
		/// <param name="sql">A select request to launch</param>
		/// <param name="feedRecordFunction">Functions called to feed a TnX object</param>
		/// <returns>A list of records fetched using the select request, null if an error has occurred</returns>
		public List<TnX> SelectRequest<TnX>(string sql, FeedRecordDelegate feedRecordFunction)
		{
			if (!string.IsNullOrEmpty(sql))
			{
				sql = sql.Replace("=true", $"={TRUE()}", StringComparison.OrdinalIgnoreCase).Replace("=false", $"={FALSE()}", StringComparison.OrdinalIgnoreCase);
				CLog.Add($"SQL: {sql}");
				return SelectRequest<TnX>(new OleDbCommand(sql), feedRecordFunction);
			}
			return null;
		}
		/// <summary>
		/// Selects a set of objects from the database and returns them inside a <see cref="DataSet"/> object to be extracted by the caller
		/// </summary>
		/// <param name="command">A complete <see cref="OleDbCommand"/> detailing a select request to launch</param>
		/// <param name="dataSet">An <see cref="DataSet"/> object which can be used to fetch data from the result set</param>
		/// <returns>True if successfull, false otherwise</returns>
		public bool SelectRequest(OleDbCommand command, ref DataSet dataSet)
		{
			bool f = false;
			if (IsOpen && null != command)
			{
				OleDbDataAdapter da = null;
				try
				{
					command.Connection = Database;
					da = new OleDbDataAdapter();
					da.SelectCommand = command;
					da.SelectCommand.Connection = Database;
					dataSet = new DataSet();
					da.Fill(dataSet);
					f = true;
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				}
				finally
				{
					if (null != da)
						da.Dispose();
				}
			}
			return f;
		}
		/// <summary>
		/// Selects a set of objects from the database and returns them inside a <see cref="DataSet"/> object to be extracted by the caller
		/// </summary>
		/// <param name="sql">A select request to launch</param>
		/// <param name="dataSet">An <see cref="DataSet"/> object which can be used to fetch data from the result set</param>
		/// <returns>True if successfull, false otherwise</returns>
		public bool SelectRequest(string sql, ref DataSet dataSet)
		{
			if (IsOpen && !string.IsNullOrEmpty(sql))
			{
				sql = sql.Replace("=true", $"={TRUE()}", StringComparison.OrdinalIgnoreCase).Replace("=false", $"={FALSE()}", StringComparison.OrdinalIgnoreCase);
				CLog.Add($"SQL: {sql}");
				return SelectRequest(new OleDbCommand(sql), ref dataSet);
			}
			return false;
		}
		/// <summary>
		/// Retrieve the value of a column inside an <see cref="OleDbDataAdapter"/>
		/// </summary>
		/// <param name="reader">The reader to look inside</param>
		/// <param name="columnName">The column name to fetch</param>
		/// <param name="value">The content of the coumn it it exists, null otherwise</param>
		/// <returns>True if the column has been found and its value returned to the caller, false otherwise</returns>
		public static bool ItemValue<TnX>(OleDbDataReader reader, string columnName, ref TnX value)
		{
			value = default(TnX);
			try
			{
				value = (TnX)reader[columnName];
				return true;
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Column name: " + columnName);
			}
			return false;
		}
		#endregion
	}

	[ComVisible(false)]
	public class CDatabaseTableManager : CDatabaseBase
	{
		#region constructor
		public CDatabaseTableManager() { }
		public CDatabaseTableManager(string connectString) : base(connectString) { }
		#endregion

		#region properties
		/// <summary>
		/// <see cref="OleDbDataAdapter"/> object created to feed the table
		/// </summary>
		public OleDbDataAdapter DataAdapter { get; private set; } = null;
		/// <summary>
		/// <see cref="OleDbCommandBuilder"/> object created to manage the table
		/// </summary>
		public OleDbCommandBuilder CommandBuilder { get; private set; } = null;
		#endregion

		#region methods
		/// <summary>
		/// Fill a <see cref="DataTable"/> with data contained inside a database table
		/// </summary>
		/// <param name="sql"></param>
		/// <returns></returns>
		public DataTable FillTable(string sql)
		{
			if (IsOpen && !string.IsNullOrEmpty(sql))
			{
				try
				{
					sql = sql.Replace("=true", $"={TRUE()}", StringComparison.OrdinalIgnoreCase).Replace("=false", $"={FALSE()}", StringComparison.OrdinalIgnoreCase);
					OleDbDataAdapter da = new OleDbDataAdapter(sql, Database);
					DataTable dataTable = new DataTable();
					da.Fill(dataTable);
					DataAdapter = da;
					CommandBuilder = new OleDbCommandBuilder(DataAdapter);
					DataAdapter.InsertCommand = CommandBuilder.GetInsertCommand();
					DataAdapter.UpdateCommand = CommandBuilder.GetUpdateCommand();
					DataAdapter.DeleteCommand = CommandBuilder.GetDeleteCommand();
					return dataTable;
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, "Connection string: " + ConnectionString);
				}
			}
			return null;
		}
		/// <summary>
		/// Update the content of a <see cref="DataTable"/> object
		/// </summary>
		/// <param name="dt">The <see cref="DataTable"/> object to update</param>
		/// <returns>The number of elements updated is successfull, 0 otherwise</returns>
		public int UpdateData(DataTable dt)
		{
			if (IsOpen && null != DataAdapter && null != dt)
				return DataAdapter.Update(dt);
			return 0;
		}
		#endregion
	}
}
