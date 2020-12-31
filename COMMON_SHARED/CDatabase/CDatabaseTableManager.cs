using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text;
using System.Reflection;

namespace COMMON
{
	public class CDatabaseTableManager : CDatabaseBase
	{
		#region constructor
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
				OleDbDataAdapter da = new OleDbDataAdapter(sql, Database.Database);
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
