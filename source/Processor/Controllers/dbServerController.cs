
using System;
using System.Data;
using System.Data.SqlClient;
using Contensive.Processor.Exceptions;
//
namespace Contensive.Processor.Controllers {
    //
    //==========================================================================================
    /// <summary>
    /// Manage the sql server (adding catalogs, etc.)
    /// </summary>
    public class DbServerController : IDisposable {
        //
        // objects passed in that are not disposed
        //
        private readonly CoreController core;
        //
        //==========================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core"></param>
        /// <remarks></remarks>
        public DbServerController(CoreController core) {
            try {
                this.core = core;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the correctly formated connection string for this datasource. Called only from within this class
        /// </summary>
        /// <returns>
        /// </returns>
        public string getConnectionStringADONET() {
            //
            // (OLEDB) OLE DB Provider for SQL Server > "Provider=sqloledb;Data Source=MyServerName;Initial Catalog=MyDatabaseName;User Id=MyUsername;Password=MyPassword;"
            //     https://www.codeproject.com/Articles/2304/ADO-Connection-Strings#OLE%20DB%20SqlServer
            //
            // (OLEDB) Microsoft OLE DB Provider for SQL Server connection strings > "Provider=sqloledb;Data Source=myServerAddress;Initial Catalog=myDataBase;User Id = myUsername;Password=myPassword;"
            //     https://www.connectionstrings.com/microsoft-ole-db-provider-for-sql-server-sqloledb/
            //
            // (ADONET) .NET Framework Data Provider for SQL Server > Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password = myPassword;
            //     https://www.connectionstrings.com/sql-server/
            //
            string returnConnString = "";
            try {
                string serverUrl = core.serverConfig.defaultDataSourceAddress;
                if (serverUrl.IndexOf(":") > 0) {
                    serverUrl = serverUrl.left(serverUrl.IndexOf(":"));
                }
                returnConnString += ""
                    + "server=" + serverUrl + ";"
                    + "User Id=" + core.serverConfig.defaultDataSourceUsername + ";"
                    + "Password=" + core.serverConfig.defaultDataSourcePassword + ";"
                    + "";
                //
                // -- add certificate requirement, if true, set yes, if false, no not add it
                if (core.serverConfig.defaultDataSourceSecure) {
                    returnConnString += "Encrypt=yes;";
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnConnString;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create a new catalog in the database
        /// </summary>
        /// <param name="catalogName"></param>
        public void createCatalog(string catalogName) {
            try {
                executeQuery("create database " + catalogName);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Delete a catalog in the database
        /// </summary>
        /// <param name="catalogName"></param>
        public void deleteCatalog(string catalogName) {
            try {
                //
                // -- try a simple drop
                executeQuery("ALTER DATABASE " + catalogName + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
                executeQuery("DROP DATABASE " + catalogName);
                return;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Check if the database exists
        /// </summary>
        /// <param name="catalog"></param>
        /// <returns></returns>
        public bool checkCatalogExists(string catalog) {
            bool returnOk = false;
            try {
                string sql = null;
                DataTable dt = null;
                //
                sql = string.Format("SELECT database_id FROM sys.databases WHERE Name = '{0}'", catalog);
                dt = executeQuery(sql);
                returnOk = (dt.Rows.Count > 0);
                dt.Dispose();
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnOk;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Execute a command or sql statemwent and return a dataTable
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        private DataTable executeQuery(string sql) {
            DataTable returnData = new DataTable();
            try {
                using SqlConnection connSQL = new SqlConnection(getConnectionStringADONET());
                connSQL.Open();
                using SqlCommand cmdSQL = new SqlCommand {
                    CommandType = CommandType.Text,
                    CommandText = sql,
                    Connection = connSQL
                };
                using dynamic adptSQL = new System.Data.SqlClient.SqlDataAdapter(cmdSQL);
                adptSQL.Fill(returnData);
            } catch (Exception ex) {
                var newEx = new GenericException("Exception [" + ex.Message + "] executing master sql [" + sql + "]", ex);
                LogController.logError(core, newEx);
            }
            return returnData;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        #region  IDisposable Support 
        protected bool disposed;
        //
        //==========================================================================================
        /// <summary>
        /// dispose
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks></remarks>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    //
                    // ----- call .dispose for managed objects
                    //
                    //
                    // ----- Close all open csv_ContentSets, and make sure the RS is killed
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~DbServerController() {
            Dispose(false);


        }
        #endregion
    }
}

