
using System;
using System.Collections.Generic;
using Contensive.BaseClasses;
using Contensive.Models.Db;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class UpgradeController : IDisposable {
        //
        //==============================================================================================================
        /// <summary>
        /// convert fileInfo array to parsable string [filename-attributes-creationTime-lastAccessTime-lastWriteTime-length-entension]
        /// </summary>
        /// <param name="FileInfo"></param>
        /// <returns></returns>
        public static string upgrade51ConvertFileInfoArrayToParseString(List<CPFileSystemBaseClass.FileDetail> FileInfo) {
            var result = new System.Text.StringBuilder();
            if (FileInfo.Count > 0) {
                foreach (CPFileSystemBaseClass.FileDetail fi in FileInfo) {
                    result.Append(Environment.NewLine + fi.Name + "\t" + fi.Attributes + "\t" + fi.DateCreated + "\t" + fi.DateLastAccessed + "\t" + fi.DateLastModified + "\t" + fi.Size + "\t" + fi.Extension);
                }
            }
            return result.ToString();
        }
        //
        //==============================================================================================================
        /// <summary>
        /// convert directoryInfo object to parsable string [filename-attributes-creationTime-lastAccessTime-lastWriteTime-extension]
        /// </summary>
        /// <param name="DirectoryInfo"></param>
        /// <returns></returns>
        public static string upgrade51ConvertDirectoryInfoArrayToParseString(List<CPFileSystemBaseClass.FolderDetail> DirectoryInfo) {
            var result = new System.Text.StringBuilder();
            if (DirectoryInfo.Count > 0) {
                foreach (CPFileSystemBaseClass.FolderDetail di in DirectoryInfo) {
                    result.Append(Environment.NewLine + di.Name + "\t" + (int)di.Attributes + "\t" + di.DateCreated + "\t" + di.DateLastAccessed + "\t" + di.DateLastModified + "\t0\t");
                }
            }
            return result.ToString();
        }
        //
        //====================================================================================================
        /// <summary>
        /// drop editRecordId, editarchive, and editblank and all the indexes that reference them
        /// </summary>
        /// <param name="core"></param>
        /// <param name="DataBuildVersion"></param>
        public static void dropLegacyWorkflowField(CoreController core, string fieldName) {
            try {
                //
                // verify Db field schema for fields handled internally (fix datatime2(0) problem -- need at least 3 digits for precision)
                var tableList = DbBaseModel.createList<TableModel>(core.cpParent, "(1=1)", "dataSourceId");
                foreach (TableModel table in tableList) {
                    core.db.deleteTableField(table.name, fieldName, true);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed;
        //
        public void Dispose()  {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~UpgradeController()  {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }
    //
}