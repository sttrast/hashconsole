using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using hash_console_app.Properties;

namespace TCI.DataLayer.DataObjects
{
    /// <summary>
    /// Class that manages all lower level ADO.NET data base access.
    /// 
    /// GoF Design Patterns: Singleton, Factory, Proxy.
    /// </summary>
    /// <remarks>
    /// This class is a 'swiss army knife' of data access. It handles all the 
    /// database access details and shields its complexity from its clients.
    /// 
    /// The Factory Design pattern is used to create database specific instances
    /// of Connection objects, Command objects, etc.
    /// 
    /// This class is like a Singleton -- it is a static class (Shared in VB) and 
    /// therefore only one 'instance' ever will exist.
    /// 
    /// This class is a Proxy in that it 'stands in' for the actual DbProviderFactory.
    /// </remarks>
    public static class Db
    {
        // Note: Static initializers are thread safe.
        // If this class had a static constructor then these initialized 
        // would need to be initialized there.
        //private static readonly string dataProvider = ConfigurationManager.AppSettings.Get("DataProvider");
        //private static readonly DbProviderFactory factory = DbProviderFactories.GetFactory(dataProvider);
        //private static readonly string connectionString = ConfigurationManager.ConnectionStrings[dataProvider].ConnectionString;

        private static readonly string dataProvider = Settings.Default.dataprovider;
        private static readonly DbProviderFactory factory = DbProviderFactories.GetFactory(dataProvider);
        private static readonly string connectionString = Settings.Default.ConnectionStr;

        public static DbParameter GetParameter()
        {
            return factory.CreateParameter();
        }

        #region Data Update handlers

        /// <summary>
        /// Executes Update statements in the database.
        /// </summary>
        /// <param name="sql">Sql statement.</param>
        /// <returns>Number of rows affected.</returns>
        public static int Update(string sql)
        {
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                using (DbCommand command = factory.CreateCommand())
                {
                    command.Connection = connection;
                    command.CommandText = sql;

                    connection.Open();
                    return command.ExecuteNonQuery();
                }

            }
        }

        /// <summary>
        /// Executes Update statements in the database.
        /// </summary>
        /// <param name="sql">Sql to execute with named parameters</param>
        /// <param name="paramList">List of parameters</param>
        /// <returns>Number of rows affected.</returns>
        public static int Update(string sql, IList<DbParameter> paramList)
        {
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                using (DbCommand command = factory.CreateCommand())
                {
                    command.Connection = connection;
                    command.CommandText = sql;
                    foreach (DbParameter parameter in paramList)
                    {
                        if (parameter.Value == null)
                        {
                            parameter.Value = DBNull.Value;
                        }
                        command.Parameters.Add(parameter);
                    }

                    connection.Open();
                    //return command.ExecuteNonQuery();
                    try
                    {
                        return command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Console.Write(e.Message);
                        return -1;
                    }
                }

            }
        }



        /// <summary>
        /// Executes Insert statements in the database. Optionally returns new identifier.
        /// </summary>
        /// <param name="sql">Sql statement.</param>
        /// <param name="getId">Value indicating whether newly generated identity is returned.</param>
        /// <returns>Newly generated identity value (autonumber value).</returns>
        public static int Insert(string sql, bool getId)
        {
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                using (DbCommand command = factory.CreateCommand())
                {
                    command.Connection = connection;
                    command.CommandText = sql;

                    connection.Open();
                    //command.ExecuteNonQuery();

                    //int id = -1;

                    int id = command.ExecuteNonQuery();

                    // Check if new identity is needed.
                    if (getId)
                    {
                        // Execute db specific autonumber or identity retrieval code
                        // SELECT SCOPE_IDENTITY() -- for SQL Server
                        // SELECT @@IDENTITY -- for MS Access
                        // SELECT MySequence.NEXTVAL FROM DUAL -- for Oracle
                        string identitySelect;
                        switch (dataProvider)
                        {
                            // Access
                            case "System.Data.OleDb":
                                identitySelect = "SELECT @@IDENTITY";
                                break;
                            // Sql Server
                            case "System.Data.SqlClient":
                                identitySelect = "SELECT SCOPE_IDENTITY()";
                                break;
                            // Oracle
                            case "System.Data.OracleClient":
                                identitySelect = "SELECT MySequence.NEXTVAL FROM DUAL";
                                break;
                            default:
                                identitySelect = "SELECT @@IDENTITY";
                                break;
                        }
                        command.CommandText = identitySelect;
                        id = int.Parse(command.ExecuteScalar().ToString());
                    }
                    return id;
                }
            }
        }

        /// <summary>
        /// Executes Insert statements in the database.
        /// </summary>
        /// <param name="sql">Sql statement.</param>
        public static int Insert(string sql)
        {
            return Insert(sql, false);
        }

        public static int Insert(string sql, IList<DbParameter> paramList)
        {
            return Insert(sql, paramList, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql">Sql statement.</param>
        /// <param name="paramList">List of parameters</param>
        /// <param name="getId">Value indicating whether newly generated identity is returned.</param>
        /// <returns>Newly generated identity value (autonumber value).</returns>
        public static int Insert(string sql, IList<DbParameter> paramList, bool getId)
        {
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                using (DbCommand command = factory.CreateCommand())
                {
                    command.Connection = connection;
                    command.CommandText = sql;

                    //foreach (DbParameter parameter in paramList)
                    //{
                    //    Utilities.LogIt("Parameters", parameter.ParameterName.ToString() + " - " + parameter.Value.ToString());
                    //}

                    foreach (DbParameter parameter in paramList)
                    {
                        if (parameter.Value == null)
                        {
                            parameter.Value = DBNull.Value;
                        }
                        command.Parameters.Add(parameter);

                    }

                    if (getId)
                    {
                        DbParameter parameter = GetParameter();
                        parameter.DbType = DbType.Int32;
                        parameter.ParameterName = "@ID";
                        parameter.Direction = ParameterDirection.Output;
                        command.Parameters.Add(parameter);
                        command.CommandText += "; SET @ID = SCOPE_IDENTITY()";
                    }

                    connection.Open();
                    int id = 0;
                    try
                    {
                        id = command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Console.Write(e.Message);
                    }

                    //int id = -1;

                    // Check if new identity is needed.
                    if (getId)
                    {
                        //// Execute db specific autonumber or identity retrieval code
                        //// SELECT SCOPE_IDENTITY() -- for SQL Server
                        //// SELECT @@IDENTITY -- for MS Access
                        //// SELECT MySequence.NEXTVAL FROM DUAL -- for Oracle
                        //string identitySelect;
                        //switch (dataProvider)
                        //{
                        //    // Access
                        //    case "System.Data.OleDb":
                        //        identitySelect = "SELECT @@IDENTITY";
                        //        break;
                        //    // Sql Server
                        //    case "System.Data.SqlClient":
                        //        identitySelect = "SELECT SCOPE_IDENTITY()";                                
                        //        break;
                        //    // Oracle
                        //    case "System.Data.OracleClient":
                        //        identitySelect = "SELECT MySequence.NEXTVAL FROM DUAL";
                        //        break;
                        //    default:
                        //        identitySelect = "SELECT @@IDENTITY";
                        //        break;
                        //}
                        //command.CommandText = identitySelect;
                        //id = int.Parse(command.ExecuteScalar().ToString());
                        id = (int)command.Parameters["@ID"].Value;
                    }
                    return id;
                }
            }
        }



        #endregion

        #region Data Retrieve Handlers
        private static IDataReader GetDataReader(string sql, CommandBehavior commandBehavior)
        {
            //using (DbConnection connection = factory.CreateConnection())
            DbConnection connection = factory.CreateConnection();
            //{
            connection.ConnectionString = connectionString;
            connection.Open();

            using (DbCommand command = factory.CreateCommand())
            {
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = sql;

                // Command behavior uses bitwise enumeration. Close connection is always used
                // and other behaviors are additive
                IDataReader dr = command.ExecuteReader(CommandBehavior.CloseConnection | commandBehavior);
                return dr;
            }
            //}           
        }

        private static IDataReader GetDataReader(string sql, IList<DbParameter> paramList, CommandBehavior commandBehavior)
        {
            //using (DbConnection connection = factory.CreateConnection())
            DbConnection connection = factory.CreateConnection();
            //{
            connection.ConnectionString = connectionString;
            connection.Open();

            using (DbCommand command = factory.CreateCommand())
            {
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = sql;

                foreach (DbParameter parameter in paramList)
                {
                    if (parameter.Value == null)
                    {
                        parameter.Value = DBNull.Value;
                    }
                    command.Parameters.Add(parameter);
                }
                // Command behavior uses bitwise enumeration. Close connection is always used
                // and other behaviors are additive
                IDataReader dr = command.ExecuteReader(CommandBehavior.CloseConnection | commandBehavior);
                return dr;
            }
            //}           
        }

        public static IDataReader GetDataReader(string sql)
        {
            return GetDataReader(sql, CommandBehavior.Default);
        }
        public static IDataReader GetDataReader(string sql, IList<DbParameter> paramList)
        {
            return GetDataReader(sql, paramList, CommandBehavior.Default);
        }

        public static IDataReader GetDataReaderSingleRow(string sql)
        {
            return GetDataReader(sql, CommandBehavior.SingleRow);
        }

        public static IDataReader GetDataReaderSingleRow(string sql, IList<DbParameter> paramList)
        {
            return GetDataReader(sql, paramList, CommandBehavior.SingleRow);
        }


        /// <summary>
        /// Populates a DataSet according to a Sql statement.
        /// </summary>
        /// <param name="sql">Sql statement.</param>
        /// <returns>Populated DataSet.</returns>
        public static DataSet GetDataSet(string sql)
        {
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                using (DbCommand command = factory.CreateCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;
                    command.CommandText = sql;

                    using (DbDataAdapter adapter = factory.CreateDataAdapter())
                    {
                        adapter.SelectCommand = command;

                        DataSet ds = new DataSet();
                        adapter.Fill(ds);

                        return ds;
                    }
                }
            }
        }

        /// <summary>
        /// Populates a DataTable according to a Sql statement.
        /// </summary>
        /// <param name="sql">Sql statement.</param>
        /// <returns>Populated DataTable.</returns>
        public static DataTable GetDataTable(string sql)
        {
            return GetDataSet(sql).Tables[0];
        }

        /// <summary>
        /// Populates a DataRow according to a Sql statement.
        /// </summary>
        /// <param name="sql">Sql statement.</param>
        /// <returns>Populated DataRow.</returns>
        public static DataRow GetDataRow(string sql)
        {
            DataRow row = null;

            DataTable dt = GetDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                row = dt.Rows[0];
            }

            return row;
        }

        /// <summary>
        /// Executes a Sql statement and returns a scalar value.
        /// </summary>
        /// <param name="sql">Sql statement.</param>
        /// <returns>Scalar value.</returns>
        public static object GetScalar(string sql)
        {
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                using (DbCommand command = factory.CreateCommand())
                {
                    command.Connection = connection;
                    command.CommandText = sql;

                    command.Connection.Open();
                    return command.ExecuteScalar();
                }
            }
        }
        /// <summary>
        /// Executes a Sql statement and returns a scalar value.
        /// </summary>
        /// <param name="sql">Sql statement.</param>
        /// <returns>Scalar value.</returns>
        public static object GetScalar(string sql, IList<DbParameter> paramList)
        {
            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                using (DbCommand command = factory.CreateCommand())
                {
                    command.Connection = connection;
                    command.CommandText = sql;

                    foreach (DbParameter parameter in paramList)
                    {
                        if (parameter.Value == null)
                        {
                            parameter.Value = DBNull.Value;
                        }
                        command.Parameters.Add(parameter);
                    }

                    connection.Open();
                    return command.ExecuteScalar();
                }
            }
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Escapes an input string for database processing, that is, 
        /// surround it with quotes and change any quote in the string to 
        /// two adjacent quotes (i.e. escape it). 
        /// If input string is null or empty a NULL string is returned.
        /// </summary>
        /// <param name="s">Input string.</param>
        /// <returns>Escaped output string.</returns>
        public static string Escape(string s)
        {
            if (String.IsNullOrEmpty(s))
                return "NULL";
            else
                return "'" + s.Trim().Replace("'", "''") + "'";
        }

        /// <summary>
        /// Escapes an input string for database processing, that is, 
        /// surround it with quotes and change any quote in the string to 
        /// two adjacent quotes (i.e. escape it). 
        /// Also trims string at a given maximum length.
        /// If input string is null or empty a NULL string is returned.
        /// </summary>
        /// <param name="s">Input string.</param>
        /// <param name="maxLength">Maximum length of output string.</param>
        /// <returns>Escaped output string.</returns>
        public static string Escape(string s, int maxLength)
        {
            if (String.IsNullOrEmpty(s))
                return "NULL";
            else
            {
                s = s.Trim();
                if (s.Length > maxLength) s = s.Substring(0, maxLength - 1);
                return "'" + s.Trim().Replace("'", "''") + "'";
            }
        }

        /// <summary>
        /// ParsesDate and will return null if date is invalid.
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Valid DatTime or null</returns>
        public static DateTime? ParseDate(string s)
        {
            DateTime retval;
            if (DateTime.TryParse(s, out retval))
            {
                return retval;
            }
            else
            {
                return null;
            }
        }

        public static int? ParseInt(string s)
        {
            int retval;
            if (int.TryParse(s, out retval))
            {
                return retval;
            }
            else
            {
                return null;
            }
        }

        public static decimal? ParseDecimal(string s)
        {
            decimal retval;
            if (decimal.TryParse(s, out retval))
            {
                return retval;
            }
            else
            {
                return null;
            }
        }

        public static bool IsNumeric(object ObjectToTest)
        {
            if (ObjectToTest == null)
            {
                return false;

            }
            else
            {
                double OutValue;
                return double.TryParse(ObjectToTest.ToString().Trim(),
                    System.Globalization.NumberStyles.Any,

                    System.Globalization.CultureInfo.CurrentCulture,

                    out OutValue);
            }
        }

        //public static DBNull NullCheck(string s)
        //{

        //}
        #endregion
    }
}
