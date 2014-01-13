using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Data.Common;
using System.Configuration;
using System.Data.Sql;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Security.AccessControl;

using TCI.DataLayer.DataObjects;
using hash_console_app.Properties;

namespace hash_console_app
{
    class Program
    {

        #region variables..

        private static string _fullpath;
        public static string FullPath
        {
            get { return _fullpath; }
            set { _fullpath = value; }
        }
        private static string _error_string;
        public static string error_string
        {
            get { return _error_string; }
            set { _error_string = value; }
        }

        #endregion

        public static void send_alert()
        {

            MailMessage message = new MailMessage();

            //email to
            message.To.Add(new MailAddress(Settings.Default.email_to));

            //message.To.Add(new MailAddress("recipient2@foo.bar.com"));

            //email CC/BC
            message.CC.Add(new MailAddress(Settings.Default.email_cc));

            //message.CC.Add(new MailAddress("carboncopy@foo.bar.com"));
            //message.Bcc.Add(new MailAddress("carboncopy@foo.bar.com"));

            //email from
            message.From = new MailAddress(Settings.Default.email_from);

            //email subject
            message.Subject = Settings.Default.email_subject;

            //email body
            message.Body = Settings.Default.email_body;

            //add an attachment if needed
            message.Attachments.Add(new Attachment(Settings.Default.email_attach));

            // Add the server, port and credentials for the email and send it
            SmtpClient client = new SmtpClient(Settings.Default.smtp_server, Settings.Default.smtp_port);

            // Set client credentials
            client.Credentials = new NetworkCredential(Settings.Default.nos_user_id.ToString(), Settings.Default.nos_user_pw.ToString());
            //client.UseDefaultCredentials = true;

            // Send the email
            client.Send(message);

            return;

        }

        class LogIt
        {

            public static void WriteLog(string msg)
            {

                string dirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string fullPath = Path.Combine(dirPath, "Error.Log");
                StreamWriter log = new StreamWriter(fullPath, true);
                log.WriteLine(DateTime.Now.ToString("MM-dd-yyyy HH:mm tt") + Environment.NewLine + msg);
                log.Close();

            }

            public static void STLogIt(string LogFileName, string LogText)
            {

                if (Settings.Default.LogAllActivities == true)
                {

                    string LogDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                        @"\" + Settings.Default.LogFileDirectory;
                    if (!Directory.Exists(LogDirectory))
                    {

                        Directory.CreateDirectory(LogDirectory);
                    }

                    StreamWriter swLogFile = new StreamWriter(LogDirectory + @"\" + LogFileName + ".txt", true);
                    using (swLogFile)
                    {

                        swLogFile.WriteLine(LogText);

                    }

                }

            }

            public static bool log_error(bool verbose, string error_file, string error_txt, Exception err)
            {

                bool success = false;
                if (!verbose)
                {

                    // Log the output with the filename and the date/time
                    error_string = "logging the error with any of the processing";
                    LogIt.STLogIt(error_file, error_txt + err.Message);

                }
                else
                {

                    error_string = "verbose logging the error with any of the processing";
                    StringBuilder verb_err_text = new StringBuilder();
                    verb_err_text.Append("message: " + err.Message + ", ");
                    verb_err_text.Append("data: " + err.Data + ", ");
                    verb_err_text.Append("InnerException: " + err.InnerException + ", ");
                    verb_err_text.Append("source: " + err.Source + ", ");
                    verb_err_text.Append("TargetSite: " + err.TargetSite + ", ");
                    verb_err_text.Append("StackTrace: " + err.StackTrace + ", ");
                    verb_err_text.Append(" while ");
                    verb_err_text.Append(error_string);
                    verb_err_text.Append(" - " + DateTime.Now.ToString());

                    // Log the output with the filename and the date/time
                    LogIt.STLogIt("hash_error", "Could not process file: " + FullPath + ", with error: " + verb_err_text.ToString());

                }

                return success;

            }

        }

        class DataAccess
        {

            #region SQL tables..

            //-- hash table - changed 16jan2013      
            //-- DROP TABLE [DBO].[HASH_INFO]        
            //-- CREATE TABLE [DBO].[HASH_INFO] (    
            //--	SYSTEM_ID INT	IDENTITY(1,1),          
            //--    [HASH_VAL] [VARCHAR] (256) NULL,
            //--	[FILE_SIZE] [INT],                     
            //--	[HASH_DATE] [DATETIME] NULL,          
            //--    [FULL_PATH] [VARCHAR] (256) NULL,       
            //--	[FILE_NAME] [VARCHAR] (256) NULL,
            //--	[FILE_CREATE] [DATETIME] NULL, 
            //--	[FILE_ACCESS] [DATETIME] NULL, 
            //--    [FILE_WRITTEN] [DATETIME] NULL,
            //--    [LIVELINK_OID] [BIGINT] NULL,
            //--    [LIVELINK_VER] [VARCHAR] (256) NULL
            //-- ) ON [PRIMARY]    
            //--CREATE UNIQUE CLUSTERED INDEX HASH_INFO_C1 ON DBO.HASH_INFO(SYSTEM_ID, HASH_VAL)

            #endregion

            public static bool db_insert(string file_name, string hash, string full_path, DateTime hash_date, long file_size, DateTime file_create,
                DateTime file_access, DateTime file_write, long livelink_oid, string livelink_ver)
            {
                bool success = false;
                try
                {

                    StringBuilder sql_insert = new StringBuilder("INSERT INTO " + Settings.Default.sql_user + ".HASH_INFO ");
                    sql_insert.Append("(FILE_NAME, HASH_VAL, FULL_PATH, HASH_DATE, FILE_SIZE, FILE_CREATE, FILE_ACCESS, FILE_WRITTEN, LIVELINK_OID, LIVELINK_VER)");
                    sql_insert.Append(" VALUES ");
                    sql_insert.Append("('" + file_name + "', '" + hash + "', '" + full_path + "', '" + hash_date + "', ");
                    sql_insert.Append(file_size + ", '");
                    sql_insert.Append(file_create + "', '");
                    sql_insert.Append(file_access + "', '");
                    sql_insert.Append(file_write + "',");
                    sql_insert.Append(livelink_oid + ",'");
                    sql_insert.Append(livelink_ver + "')");

                    Db.GetScalar(sql_insert.ToString());
                    success = true;

                }
                catch (Exception db_err)
                {

                    if (!Settings.Default.verbose_error)
                    {

                        // Log the output with the filename and the date/time
                        error_string = "error with db insert";
                        LogIt.STLogIt("hash_error", "Could not process db insert: " + FullPath + " with error " + db_err.Message);

                    }
                    else
                    {

                        error_string = "verbose error with db insert";
                        StringBuilder db_err_text = new StringBuilder();
                        db_err_text.Append("message: " + db_err.Message + ", ");
                        db_err_text.Append("data: " + db_err.Data + ", ");
                        db_err_text.Append("InnerException: " + db_err.InnerException + ", ");
                        db_err_text.Append("source: " + db_err.Source + ", ");
                        db_err_text.Append("TargetSite: " + db_err.TargetSite + ", ");
                        db_err_text.Append("StackTrace: " + db_err.StackTrace + ", ");
                        db_err_text.Append(" while ");
                        db_err_text.Append(error_string);
                        db_err_text.Append(" - " + DateTime.Now.ToString());

                        // Log the output with the filename and the date/time
                        LogIt.STLogIt("hash_error", "Error with db insert for file: " + FullPath + ", with error: " + db_err_text.ToString());

                    }

                    success = false;
                }

                return success;

            }

            public static DbConnection CreateConnection(string providerName, string connectionString)
            {

                DbConnection connection = null;

                try
                {

                    DbProviderFactory factory = DbProviderFactories.GetFactory(providerName);
                    connection = factory.CreateConnection();
                    connection.ConnectionString = connectionString;

                }
                catch (Exception x)
                {

                    LogIt.WriteLog(x.ToString());
                    if (connection != null)
                        connection = null;

                }

                // return the connection
                return connection;

            }

            public static string SelectScalar(string sql)
            {

                ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings["Docs"];
                if ((!string.IsNullOrEmpty(connectionStringSettings.ConnectionString) && (!string.IsNullOrEmpty(connectionStringSettings.ProviderName))))
                    return SelectScalar(sql, connectionStringSettings.ProviderName, connectionStringSettings.ConnectionString);
                else
                    return null;

            }

            public static string SelectScalar(string sql, string providerName, string connectionString)
            {

                // Get a connection object
                DbConnection connection = CreateConnection(providerName, connectionString);
                if (connection != null)
                {

                    using (connection)
                    {

                        try
                        {

                            // create the command object and return scalar output
                            DbCommand command = connection.CreateCommand();
                            command.CommandText = sql;
                            // open the connection.
                            connection.Open();
                            // get the data
                            string scalarVal = command.ExecuteScalar().ToString();
                            // return scalar value as string
                            return scalarVal;

                        }
                        catch (Exception x)
                        {

                            LogIt.WriteLog(x.ToString());
                            // write exception
                            return null;

                        }

                    }

                }
                else
                {
                    // write connection is null
                    return null;
                }
            }

            public static int DbCommandExecute(string sql)
            {

                ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings["Docs"];
                return DbCommandExecute(sql, connectionStringSettings.ProviderName, connectionStringSettings.ConnectionString);

            }

            public static int DbCommandExecute(string sql, string providerName, string connectionString)
            {

                // Get a connection object
                DbConnection connection = CreateConnection(providerName, connectionString);

                if (connection != null)
                {

                    using (connection)
                    {

                        try
                        {

                            // create command object and return number of rows affected
                            DbCommand command = connection.CreateCommand();
                            command.CommandText = sql;

                            // open the connection
                            connection.Open();

                            // execute the command
                            int retVal = command.ExecuteNonQuery();
                            // return number of row affected
                            return retVal;

                        }
                        catch (Exception x)
                        {

                            // write exception
                            LogIt.WriteLog(x.ToString());
                            return -1;

                        }

                    }

                }

                else
                    return -1;

            }

        }

        class hash_functions
        {

            // MD5 hash function #2
            public static string GetHash(FileInfo filename)
            {

                if (!Directory.Exists(Settings.Default.temp_dir + @"\"))
                {
                    Directory.CreateDirectory(Settings.Default.temp_dir + @"\");
                }

                // Build temp file path for processing
                string temp_file = Settings.Default.temp_dir + @"\" + filename.Name;

                try
                {

                    // Create temp file
                    File.Copy(filename.FullName, temp_file, true);

                    StringBuilder md5_hash_val = new StringBuilder();
                    MD5 md5Hasher = MD5.Create();
                    
                    using (FileStream fs = File.OpenRead(temp_file))
                    {

                        foreach (Byte b in md5Hasher.ComputeHash(fs))
                            md5_hash_val.Append(b.ToString("x2").ToLower());

                    }

                    // Delete the temp file
                    File.Delete(temp_file);

                    if (File.Exists(temp_file))
                    {

                        other_utilities.GrantAccess(temp_file);
                        // Delete the temp file
                        File.Delete(temp_file);

                    }

                    // Return hash value
                    return md5_hash_val.ToString();

                }
                catch (Exception err)
                {

                    // Log the failure
                    LogIt.STLogIt("hash_fail", "Hash function on: " + filename.FullName + " temp file: " + temp_file + " failed with error: " + err.Message);

                    // Delete the temp file
                    File.Delete(temp_file);

                    // Return failure code
                    return "-1";

                }

            }

            // For seperate hash routine #4
            public static int file_hash(FileInfo full_path, long filesize, DateTime created, DateTime accessed, DateTime written)
            {

                int success = -1;

                // C# gethashcode function
                success = full_path.GetHashCode();

                return success;

            }

        }

        class other_utilities
        {

            public static string ReplaceValues(string StringValue, int Sharepoint)
            {
                string sAdjustedString = StringValue;
                // Replace any returns or linefeeds
                sAdjustedString = sAdjustedString.Replace("\r\n", " ");
                sAdjustedString = sAdjustedString.Replace("\n", " ");
                sAdjustedString = sAdjustedString.Replace("\r", " ");
                if (Sharepoint == 0)
                {
                    sAdjustedString = sAdjustedString.Replace("'", " ");
                    sAdjustedString = sAdjustedString.Replace(",", " ");
                    sAdjustedString = sAdjustedString.Replace("-", " ");
                    sAdjustedString = sAdjustedString.Replace("/", " ");
                    sAdjustedString = sAdjustedString.Replace("&", " ");
                    if (sAdjustedString.Contains("%"))
                    {
                        sAdjustedString = sAdjustedString.Replace("%", "");
                        decimal dDecimalVal = (decimal.Parse(sAdjustedString) / 100);
                        sAdjustedString = dDecimalVal.ToString();
                    }
                }
                else if (Sharepoint == 1)
                {
                    // Replace these values with underscores, blanks or the actual word. ~ " # % & * : < > ? / \ { | }
                    sAdjustedString = sAdjustedString.Replace(@"'", "_");
                    sAdjustedString = sAdjustedString.Replace(@"-", "_");
                    sAdjustedString = sAdjustedString.Replace(@"/", "_");
                    sAdjustedString = sAdjustedString.Replace(@"&", " and ");
                    sAdjustedString = sAdjustedString.Replace(@"#", "_");
                    sAdjustedString = sAdjustedString.Replace(@"%", "_");
                    sAdjustedString = sAdjustedString.Replace(@"*", "_");
                    sAdjustedString = sAdjustedString.Replace(@"<", "_");
                    sAdjustedString = sAdjustedString.Replace(@">", "_");
                    sAdjustedString = sAdjustedString.Replace(@"?", "_");
                    sAdjustedString = sAdjustedString.Replace(@"\", "_");
                    sAdjustedString = sAdjustedString.Replace(@"{", "_");
                    sAdjustedString = sAdjustedString.Replace(@"}", "_");
                    sAdjustedString = sAdjustedString.Replace(@"|", "_");
                    sAdjustedString = sAdjustedString.Replace(@"~", "_");
                    sAdjustedString = sAdjustedString.Replace(@":", "_");
                    sAdjustedString = sAdjustedString.Replace(@",", " ");
                    sAdjustedString = sAdjustedString.Replace(@".", " ");
                }
                else if (Sharepoint == 3)
                {

                    sAdjustedString = sAdjustedString.Replace("'", "''");
                    sAdjustedString = sAdjustedString.Replace(",", " ");

                    if (sAdjustedString.Contains("%"))
                    {
                        sAdjustedString = sAdjustedString.Replace("%", "");
                        decimal dDecimalVal = (decimal.Parse(sAdjustedString) / 100);
                        sAdjustedString = dDecimalVal.ToString();
                    }

                }
                return sAdjustedString;
            }

            public static bool GrantAccess(string fullPath)
            {
                DirectoryInfo dInfo = new DirectoryInfo(fullPath);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule("everyone", FileSystemRights.FullControl,
                                                                 InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                                                 PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);
                return true;
            }

        }

        static void Main(string[] args)
        {

            #region SQL tables..

            //-- hash table - changed 16jan2013      
            //-- DROP TABLE [DBO].[HASH_INFO]        
            //-- CREATE TABLE [DBO].[HASH_INFO] (    
            //--	SYSTEM_ID INT	IDENTITY(1,1),          
            //--    [HASH_VAL] [VARCHAR] (256) NULL,
            //--	[FILE_SIZE] [INT],                     
            //--	[HASH_DATE] [DATETIME] NULL,          
            //--    [FULL_PATH] [VARCHAR] (256) NULL,       
            //--	[FILE_NAME] [VARCHAR] (256) NULL,
            //--	[FILE_CREATE] [DATETIME] NULL, 
            //--	[FILE_ACCESS] [DATETIME] NULL, 
            //--    [FILE_WRITTEN] [DATETIME] NULL,
            //--    [LIVELINK_OID] [BIGINT] NULL,
            //--    [LIVELINK_VER] [VARCHAR] (256) NULL
            //-- ) ON [PRIMARY]    
            //--CREATE UNIQUE CLUSTERED INDEX HASH_INFO_C1 ON DBO.HASH_INFO(SYSTEM_ID, HASH_VAL)

            #endregion

            try
            {

                #region Command line arguments...

                //command line args should be: binary_filepath(0), run_type(1), filter(2)
                // "c:\temp\image\testfile.txt" "hash" "PDF"

                // Top directory
                string top_directory = args[0].ToString();
                string run_type = args[1].ToString();

                string file_ext = "*.*";

                if (!string.IsNullOrEmpty(args[2].ToString()))
                    file_ext = "*." + args[2].ToString();

                #endregion

                string[] files2hash = Directory.GetFiles(top_directory, file_ext, SearchOption.AllDirectories);

                foreach (string path in files2hash)
                {
                    try
                    {

                        #region hash process..

                        // File info
                        FileInfo hash_file = new FileInfo(path);

                        // Fullpath used for logging if needed
                        FullPath = hash_file.FullName;

                        // General file attributes
                        DateTime file_create = hash_file.CreationTime;
                        DateTime file_access = hash_file.LastAccessTime;
                        DateTime file_write = hash_file.LastWriteTime;
                        long file_size = hash_file.Length;

                        // OpenText Livelink info
                        long livelink_oid = 123456789;
                        string livelink_ver = "new_version";

                        // Date and time of file processing
                        DateTime hash_date = DateTime.Now;

                        // MD5 hash function
                        string hash_val = hash_functions.GetHash(hash_file);

                        // Internal hash function
                        int file_hash = hash_file.GetHashCode();

                        // Log the info and attributes
                        LogIt.STLogIt("hash_files", "File with path: " + hash_file.Name
                            + " hash date: " + hash_date.ToString()
                            + "  created: " + file_create.ToString() + "  accessed: "
                            + file_access.ToString() + "  written: " + file_write.ToString() + "  size: "
                            + file_size + " corned beef hash = " + hash_val);

                        #endregion

                        #region db insert..

                        if (Settings.Default.db_insert)
                        {

                            // Write to dbo.hash_info
                            bool db_insert_succes = DataAccess.db_insert(other_utilities.ReplaceValues(hash_file.Name, 3), hash_val,
                                other_utilities.ReplaceValues(hash_file.FullName, 3), hash_date, hash_file.Length, file_create, file_access, 
                                file_write, livelink_oid, livelink_ver);
                            if (!db_insert_succes)
                            {

                                // If the insert fails, log the output with the filename and the date/time
                                LogIt.STLogIt("hash_db_error", "File: " + hash_file.FullName.ToString() + " failed on db insert");

                            }

                        }

                        #endregion

                    }
                    catch (Exception err)
                    {

                        #region processing error..

                        if (!Settings.Default.verbose_error)
                        {

                            // Log the output with the filename and the date/time
                            LogIt.STLogIt("hash_error", "Could not process file: " + FullPath + " with error " + err.Message);
                            //send_alert();
 
                        }
                        else
                        {

                            error_string = "verbose logging the error with any of the processing";
                            StringBuilder verb_err_text = new StringBuilder();
                            verb_err_text.Append("message: " + err.Message + ", ");
                            verb_err_text.Append("data: " + err.Data + ", ");
                            verb_err_text.Append("InnerException: " + err.InnerException + ", ");
                            verb_err_text.Append("source: " + err.Source + ", ");
                            verb_err_text.Append("TargetSite: " + err.TargetSite + ", ");
                            verb_err_text.Append("StackTrace: " + err.StackTrace + ", ");
                            verb_err_text.Append(" while ");
                            verb_err_text.Append(error_string);
                            verb_err_text.Append(" - " + DateTime.Now.ToString());

                            // Log the output with the filename and the date/time
                            LogIt.STLogIt("hash_error", "Could not process file: " + FullPath + ", with error: " + verb_err_text.ToString());

                        }

                        #endregion

                    }

                }

            }
            catch (Exception main_err)
            {

                #region main error..

                if (!Settings.Default.verbose_error)
                {

                    // Log the output with the filename and the date/time
                    error_string = "logging the error with the main app";
                    LogIt.STLogIt("hash_error", "Main app failed with error: " + main_err.Message);

                }
                else
                {

                    error_string = "verbose logging the error with the main app";
                    StringBuilder main_err_text = new StringBuilder();
                    main_err_text.Append("message: " + main_err.Message + ", ");
                    main_err_text.Append("data: " + main_err.Data + ", ");
                    main_err_text.Append("InnerException: " + main_err.InnerException + ", ");
                    main_err_text.Append("source: " + main_err.Source + ", ");
                    main_err_text.Append("TargetSite: " + main_err.TargetSite + ", ");
                    main_err_text.Append("StackTrace: " + main_err.StackTrace + ", ");
                    main_err_text.Append(" while ");
                    main_err_text.Append(error_string);
                    main_err_text.Append(" - " + DateTime.Now.ToString());

                    // Log the output with the filename and the date/time
                    LogIt.STLogIt("hash_error", "Main app failed with error: " + main_err_text.ToString());

                }

                #endregion

            }

        }

    }

}