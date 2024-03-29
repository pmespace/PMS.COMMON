﻿using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data;
using System.Text.RegularExpressions;
using COMMON.Properties;

namespace COMMON.ODBC
{
	/// <summary>
	/// ODBC database wrapper
	/// </summary>
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
		public OdbcConnection Database { get => _dbconnection; private set => _dbconnection = value; }
		private OdbcConnection _dbconnection = new OdbcConnection();
		/// <summary>
		/// ConnectionString string to connect to the ODBC database
		/// This must be an ODBC string (DSN=...)
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
						CLog.EXCEPT(ex, Resources.ODBCConnectionString.Format(ConnectionString));
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
		/// <returns>True is connected, false otherwise</returns>
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
						CLog.EXCEPT(ex, Resources.ODBCConnectionString.Format(ConnectionString));
					}
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Disconnect from the database
		/// </summary>
		/// <returns>True if disconnected, false otherwise</returns>
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
					CLog.EXCEPT(ex, Resources.ODBCDatabaseStillOpened.Format(Database.Database));
				}
			}
			return !IsOpen;
		}
		#endregion

		#region methods
		/// <summary>
		/// Return the value to use to test TRUE in SQL
		/// - ACCESS has a different way to use these values, this method takes care of it
		/// - That function MUST NOT be used to feed an <see cref="OdbcParameter"/> but only a SQL string
		/// </summary>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public string TRUE()
		{
			return TRUE(Database);
		}
		/// <summary>
		/// Return the value to use to test FALSE in SQL
		/// - ACCESS has a different way to use these values, this method takes care of it
		/// - That function MUST NOT be used to feed an <see cref="OdbcParameter"/> but only a SQL string
		/// </summary>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public string FALSE()
		{
			return FALSE(Database);
		}
		/// <summary>
		/// Return the value to use to test TRUE in SQL
		/// - ACCESS has a different way to use these values, this method takes care of it
		/// - That function MUST NOT be used to feed an <see cref="OdbcParameter"/> but only a SQL string
		/// </summary>
		/// <param name="db">The database to use</param>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public static string TRUE(OdbcConnection db)
		{
			return TrueFalseDBValue(db, true);
		}
		/// <summary>
		/// Return the value to use to test TRUE in SQL
		/// - ACCESS has a different way to use these values, this method takes care of it
		/// - That function MUST NOT be used to feed an <see cref="OdbcParameter"/> but only a SQL string
		/// </summary>
		/// <param name="db">The database to use</param>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public static string FALSE(OdbcConnection db)
		{
			return TrueFalseDBValue(db, false);
		}
		/// <summary>
		/// Return the value to use to test TRUE or FALSE
		/// - ACCESS has a different way to use these values, this method takes care of it
		/// - That function MUST NOT be used to feed an <see cref="OdbcParameter"/> but only a SQL string
		/// </summary>
		/// <param name="db">The database to use</param>
		/// <param name="value">The value to set</param>
		/// <returns>The appropriate value if successfull, null otherwise</returns>
		public static string TrueFalseDBValue(OdbcConnection db, bool value)
		{
			if (ConnectionState.Open == db.State)
			{
				if (IsAccess(db))
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
		/// <summary>
		/// Make sure the "=true" and "=false" is right inside a SQL string
		/// </summary>
		/// <param name="sql"></param>
		/// <returns></returns>
		public string TryToFixTrueFalseTesting(string sql)
		{
			return TryToFixTrueFalseTesting(Database, sql);
		}
		/// <summary>
		/// This function will try to make sure the "= true" or "= false" tests are correctly formatted according to the database being used
		/// </summary>
		/// <param name="db">The database being used</param>
		/// <param name="sql">The SQG string to amend</param>
		/// <returns>A new SQL string to use</returns>
		public static string TryToFixTrueFalseTesting(OdbcConnection db, string sql)
		{
			// replace all "=<any number of whites spaces>true (or) false" to build = true (or) = false"
			string pattern = @"=\s{2,}true";
			Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
			sql = regex.Replace(sql, "=true");
			pattern = pattern.Replace("true", "false");
			regex = new Regex(pattern, RegexOptions.IgnoreCase);
			sql = regex.Replace(sql, "=false");
			// replace comparisons with a valid one according to the database
			return sql.Replace("=true", $"={TRUE(db)}", StringComparison.OrdinalIgnoreCase).Replace("=false", $"={FALSE(db)}", StringComparison.OrdinalIgnoreCase).Replace("= false", $"={FALSE(db)}", StringComparison.OrdinalIgnoreCase).Replace("= true", $"={TRUE(db)}", StringComparison.OrdinalIgnoreCase);
		}
		/// <summary>
		/// Identifies where the database is an ACCESS one or not
		/// </summary>
		/// <param name="db"></param>
		/// <returns>True if ACCESS, false otherwise</returns>
		private static bool IsAccess(OdbcConnection db)
		{
			return db.DataSource.ToLower().Contains("access".ToLower());
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
		public delegate object FeedRecordDelegate(OdbcDataReader reader);
		#endregion

		#region data management methods
		/// <summary>
		/// Execute a non select request (insert, update, delete)
		/// </summary>
		/// <param name="command">A complete <see cref="OdbcCommand"/> detailing a non select request to launch</param>
		/// <param name="nbRows">Number of rows impacted by the request</param>
		/// <returns>True if the request was successfull processed, false otherwise</returns>
		public bool NonSelectRequest(OdbcCommand command, ref int nbRows)
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
					CLog.EXCEPT(ex, $"SQL: {command.CommandText}");
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
				sql = TryToFixTrueFalseTesting(sql);
				CLog.DEBUG($"SQL: {sql}");
				return NonSelectRequest(new OdbcCommand(sql), ref nbRows);
			}
			return false;
		}
		/// <summary>
		/// Selects a set of objects from the database and returns them inside a <see cref="OdbcDataAdapter"/> object to be extracted by the caller
		/// </summary>
		/// <param name="command">A complete <see cref="OdbcCommand"/> detailing a select request to launch</param>
		/// <param name="reader">An <see cref="OdbcDataReader"/> object which can be used to fetch data from the result set</param>
		/// <returns>True if successfull, false otherwise</returns>
		public bool SelectRequest(OdbcCommand command, ref OdbcDataReader reader)
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
					CLog.EXCEPT(ex);
				}
				finally
				{
					command.Dispose();
				}
			}
			return f;
		}
		/// <summary>
		/// Selects a set of objects from the database and returns them inside a <see cref="OdbcDataAdapter"/> object to be extracted by the caller
		/// </summary>
		/// <param name="sql">A select request to launch</param>
		/// <param name="reader">An <see cref="OdbcDataReader"/> object which can be used to fetch data from the result set</param>
		/// <returns>True if successfull, false otherwise</returns>
		public bool SelectRequest(string sql, ref OdbcDataReader reader)
		{
			reader = null;
			if (IsOpen && !string.IsNullOrEmpty(sql))
			{
				sql = TryToFixTrueFalseTesting(sql);
				CLog.DEBUG($"SQL: {sql}");
				return SelectRequest(new OdbcCommand(sql), ref reader);
			}
			return false;
		}
		/// <summary>
		/// Launch a select request and feed a list of records fetched using the request
		/// </summary>
		/// <param name="command">A complete <see cref="OdbcCommand"/> detailing a select request to launch</param>
		/// <param name="feedRecordFunction">Functions called to feed a TnX object</param>
		/// <returns>A list of records fetched using the select request, null if an error has occurred</returns>
		public List<TnX> SelectRequest<TnX>(OdbcCommand command, FeedRecordDelegate feedRecordFunction)
		{
			List<TnX> l = new List<TnX>();
			OdbcDataReader reader = null;
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
				sql = TryToFixTrueFalseTesting(sql);
				CLog.DEBUG($"SQL: {sql}");
				return SelectRequest<TnX>(new OdbcCommand(sql), feedRecordFunction);
			}
			return null;
		}
		/// <summary>
		/// Selects a set of objects from the database and returns them inside a <see cref="DataSet"/> object to be extracted by the caller
		/// </summary>
		/// <param name="command">A complete <see cref="OdbcCommand"/> detailing a select request to launch</param>
		/// <param name="dataSet">An <see cref="DataSet"/> object which can be used to fetch data from the result set</param>
		/// <returns>True if successfull, false otherwise</returns>
		public bool SelectRequest(OdbcCommand command, ref DataSet dataSet)
		{
			bool f = false;
			if (IsOpen && null != command)
			{
				OdbcDataAdapter da = null;
				try
				{
					command.Connection = Database;
					da = new OdbcDataAdapter();
					da.SelectCommand = command;
					da.SelectCommand.Connection = Database;
					dataSet = new DataSet();
					da.Fill(dataSet);
					f = true;
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex);
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
				sql = TryToFixTrueFalseTesting(sql);
				CLog.DEBUG($"SQL: {sql}");
				return SelectRequest(new OdbcCommand(sql), ref dataSet);
			}
			return false;
		}
		/// <summary>
		/// Refer to <see cref="OdbcCommand.ExecuteScalar"/>
		/// </summary>
		/// <param name="sql">SQL request to run</param>
		/// <returns>The scalar value if successfull, -1 in an error has occurred</returns>
		public int SelectScalar(string sql)
		{
			int scalar = -1;
			if (IsOpen && !string.IsNullOrEmpty(sql))
			{
				sql = TryToFixTrueFalseTesting(sql);
				CLog.DEBUG($"SQL: {sql}");

				OdbcDataAdapter da = new OdbcDataAdapter();
				da.SelectCommand = new OdbcCommand();
				try
				{
					da.SelectCommand.Connection = Database;
					da.SelectCommand.CommandText = sql;
					scalar = (int)da.SelectCommand.ExecuteScalar();
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex);
				}
				finally
				{
					da.SelectCommand.Dispose();
				}
			}
			return scalar;
		}
		/// <summary>
		/// Refer to <see cref="SelectScalar(string)"/>
		/// </summary>
		/// <param name="tableName">Table to look for the records</param>
		/// <param name="filter">Filter to apply</param>
		/// <returns> Refer to <see cref="SelectScalar(string)"/></returns>
		public int NbRows(string tableName, string filter)
		{
			return SelectScalar($"SELECT COUNT(*) FROM {tableName}" + (string.IsNullOrEmpty(filter) ? null : $" WHERE {filter}"));
		}
		/// <summary>
		/// Refer to <see cref="SelectScalar(string)"/>
		/// </summary>
		/// <param name="tableName">Table to look for the records</param>
		/// <returns> Refer to <see cref="SelectScalar(string)"/></returns>
		public int NbRows(string tableName)
		{
			return NbRows(tableName, null);
		}
		/// <summary>
		/// Retrieve the value of a column inside an <see cref="OdbcDataAdapter"/>
		/// </summary>
		/// <param name="reader">The reader to look inside</param>
		/// <param name="columnName">The column name to fetch</param>
		/// <param name="value">The content of the column it it exists, null otherwise</param>
		/// <returns>True if the column has been found and its value returned to the caller, false otherwise</returns>
		public static bool ItemValue<TnX>(OdbcDataReader reader, string columnName, ref TnX value)
		{
			value = default(TnX);
			try
			{
				value = (TnX)reader[columnName];
				return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex, Resources.ODBColumnName.Format(columnName));
			}
			return false;
		}
		/// <summary>
		/// Retrieve the value of a column inside an <see cref="OdbcDataAdapter"/>
		/// 
		/// WARNING THIS FUNCTION MAY RAISE AN EXCEPTION
		/// 
		/// </summary>
		/// <param name="reader">The reader to look inside</param>
		/// <param name="columnName">The column name to fetch</param>
		/// <returns>The content of the column it it exists, AN EXCEPTION IF AN ERROR OCCURS</returns>
		public static object ItemValue<TnX>(OdbcDataReader reader, string columnName)
		{
			object value = null;
			try
			{
				value = reader[columnName];
				return value;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex, Resources.ODBColumnName.Format(columnName));
				throw;
			}
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
		/// <see cref="OdbcDataAdapter"/> object created to feed the table
		/// </summary>
		public OdbcDataAdapter DataAdapter { get; private set; } = null;
		/// <summary>
		/// <see cref="OdbcCommandBuilder"/> object created to manage the table
		/// </summary>
		public OdbcCommandBuilder CommandBuilder { get; private set; } = null;
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
					sql = TryToFixTrueFalseTesting(sql);
					OdbcDataAdapter da = new OdbcDataAdapter(sql, Database);
					DataTable dataTable = new DataTable();
					da.Fill(dataTable);
					DataAdapter = da;
					CommandBuilder = new OdbcCommandBuilder(DataAdapter);
					DataAdapter.InsertCommand = CommandBuilder.GetInsertCommand();
					DataAdapter.UpdateCommand = CommandBuilder.GetUpdateCommand();
					DataAdapter.DeleteCommand = CommandBuilder.GetDeleteCommand();
					return dataTable;
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex, Resources.ODBCConnectionString.Format(ConnectionString));
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
