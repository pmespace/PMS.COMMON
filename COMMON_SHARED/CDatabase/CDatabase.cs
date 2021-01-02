﻿using System;
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
		public delegate object FeedRecordDelegate(OleDbDataReader reader);
		#endregion

		#region data management methods
		/// <summary>
		/// Execute a non select request (insert, update, delete)
		/// </summary>
		/// <param name="sql">Update request to launch</param>
		/// <param name="nbRows">Number of rows impacted by the request</param>
		/// <returns>True if the request was successfull processed, false otherwise</returns>
		public bool NonSelectRequest(string sql, ref int nbRows)
		{
			nbRows = 0;
			if (IsOpen && !string.IsNullOrEmpty(sql))
			{
				OleDbCommand command = new OleDbCommand();
				try
				{
					command.Connection = Database;
					command.CommandText = sql;
					CLog.Add("SQL: " + command.CommandText);
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
		/// Selects a set of objects from the database and returns them inside a <see cref="OleDbDataAdapter"/> object to be extracted by the caller
		/// </summary>
		/// <param name="sql">Select request to run</param>
		/// <param name="reader">An <see cref="OleDbDataReader"/> object which can be used to fetch data from the result set</param>
		/// <returns>True if successfull, false otherwise</returns>
		public bool SelectRequest(string sql, ref OleDbDataReader reader)
		{
			bool f = false;
			reader = null;
			if (IsOpen && !string.IsNullOrEmpty(sql))
			{
				OleDbCommand command = new OleDbCommand();
				try
				{
					CLog.Add("SQL: " + sql);
					command.Connection = Database;
					command.CommandText = sql;
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
		/// Launch a select request and feed a list of records fetched using the request
		/// </summary>
		/// <param name="sql">Select request to run</param>
		/// <param name="feedRecordFunction">Functions called to feed a TnX object</param>
		/// <returns>A list of records fetched using the select request, null if an error has occurred</returns>
		public List<TnX> SelectRequest<TnX>(string sql, FeedRecordDelegate feedRecordFunction)
		{
			List<TnX> l = new List<TnX>();
			OleDbDataReader reader = null;
			if (SelectRequest(sql, ref reader))
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
		/// Selects a set of objects from the database and returns them inside a <see cref="DataSet"/> object to be extracted by the caller
		/// </summary>
		/// <param name="sql">Select request to run</param>
		/// <param name="dataSet">An <see cref="DataSet"/> object which can be used to fetch data from the result set</param>
		/// <returns>True if successfull, false otherwise</returns>
		public bool SelectRequest(string sql, ref DataSet dataSet)
		{
			bool f = false;
			if (IsOpen && !string.IsNullOrEmpty(sql) && null != dataSet)
			{
				OleDbDataAdapter command = null;
				try
				{
					CLog.Add("SQL: " + sql);
					command = new OleDbDataAdapter();
					command.SelectCommand = new OleDbCommand(sql);
					command.SelectCommand.Connection = Database;
					command.Fill(dataSet);
					f = true;
				}
				catch (Exception ex)
				{
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				}
				finally
				{
					if (null != command)
						command.Dispose();
				}
			}
			return f;
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
		public OleDbDataAdapter DataAdapter { get; private set; }
		public OleDbCommandBuilder CommandBuilder { get; private set; }
		#endregion

		#region methods
		/// <summary>
		/// Fill a <see cref="DataTable"/> with data contained inside a database table
		/// </summary>
		/// <param name="sql"></param>
		/// <returns></returns>
		public DataTable FillTable(string sql)
		{
			if (string.IsNullOrEmpty(sql))
				throw new Exception("Sql command is empty");

			if (!IsOpen)
				return null;

			try
			{
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
			return null;
		}
		/// <summary>
		/// Update the content of a <see cref="DataTable"/> object
		/// </summary>
		/// <param name="dt">The <see cref="DataTable"/> object to update</param>
		/// <returns>The number of elements updated is successfull, 0 otherwise</returns>
		public int UpdateData(DataTable dt)
		{
			if (!IsOpen)
				return 0;
			return DataAdapter.Update(dt);
		}
		#endregion
	}
}
