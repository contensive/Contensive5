
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using static Contensive.BaseClasses.CPFileSystemBaseClass;
using Contensive.Exceptions;

namespace Contensive.Processor {
    //
    //==========================================================================================
    /// <summary>
    /// cpFileClass is a legacy implementation replaced with cdnFiles, appRootFiles and privateFiles. Non-Virtual calls do not limit file destination so are not scale-mode compatible
    /// </summary>
    public class CPFileClass : BaseClasses.CPFileBaseClass, IDisposable {
        //
        private Contensive.Processor.Controllers.CoreController core;
        //
        //==========================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="core"></param>
        public CPFileClass(CPClass cp) {
            this.core = cp.core;
        }
        //
        //==========================================================================================
        /// <summary>
        /// Convert a filepath in the cdnFiles store to a URL
        /// </summary>
        /// <param name="virtualFilename"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override string getVirtualFileLink(string virtualFilename) {
            return GenericController.getCdnFileLink(core, virtualFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Append a file in the cdnFiles store. Deprecated, use cp.CdnFiles.appendFile
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileContent"></param>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override void AppendVirtual(string filename, string fileContent) {
            core.cdnFiles.appendFile(filename, fileContent);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Copy a file within cdnFiles.
        /// </summary>
        /// <param name="sourceFilename"></param>
        /// <param name="destinationFilename"></param>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override void CopyVirtual(string sourceFilename, string destinationFilename) {
            core.cdnFiles.copyFile(sourceFilename, destinationFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Create a folder anywhere on the physical file space of the hosting server. Deprecated, use with cp.cdnFiles, cp.WwwFiles, or cp.privateFiles
        /// </summary>
        /// <param name="folderPath"></param>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override void CreateFolder(string folderPath) {
            if (core.wwwFiles.isinLocalAbsDosPath(folderPath)) {
                core.wwwFiles.createPath(core.wwwFiles.convertLocalAbsToRelativePath(folderPath));
            } else if (core.privateFiles.isinLocalAbsDosPath(folderPath)) {
                core.privateFiles.createPath(core.privateFiles.convertLocalAbsToRelativePath(folderPath));
            } else if (core.cdnFiles.isinLocalAbsDosPath(folderPath)) {
                core.cdnFiles.createPath(core.cdnFiles.convertLocalAbsToRelativePath(folderPath));
            } else {
                throw (new GenericException("Application cannot access this path [" + folderPath + "]"));
            }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Delete a file anywhere on the physical file space of the hosting server.
        /// </summary>
        /// <param name="pathFilename"></param>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override void Delete(string pathFilename) {
            if (core.wwwFiles.isinLocalAbsDosPath(pathFilename)) {
                core.wwwFiles.deleteFile(core.wwwFiles.convertLocalAbsToRelativePath(pathFilename));
            } else if (core.privateFiles.isinLocalAbsDosPath(pathFilename)) {
                core.privateFiles.deleteFile(core.privateFiles.convertLocalAbsToRelativePath(pathFilename));
            } else if (core.cdnFiles.isinLocalAbsDosPath(pathFilename)) {
                core.cdnFiles.deleteFile(core.cdnFiles.convertLocalAbsToRelativePath(pathFilename));
            } else {
                throw (new GenericException("Application cannot access this path [" + pathFilename + "]"));
            }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Delete a file in the cdnFiles store.
        /// </summary>
        /// <param name="pathFilename"></param>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override void DeleteVirtual(string pathFilename) {
            core.cdnFiles.deleteFile(pathFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Save a file anywhere on the physical file space of the hosting server.
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override string Read(string pathFilename) {
            if (core.wwwFiles.isinLocalAbsDosPath(pathFilename)) {
                return core.wwwFiles.readFileText(core.wwwFiles.convertLocalAbsToRelativePath(pathFilename));
            } else if (core.privateFiles.isinLocalAbsDosPath(pathFilename)) {
                return core.privateFiles.readFileText(core.privateFiles.convertLocalAbsToRelativePath(pathFilename));
            } else if (core.cdnFiles.isinLocalAbsDosPath(pathFilename)) {
                return core.cdnFiles.readFileText(core.cdnFiles.convertLocalAbsToRelativePath(pathFilename));
            } else {
                throw (new GenericException("Application cannot access this path [" + pathFilename + "]"));
            }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Read a file from the cdnFiles store.
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override string ReadVirtual(string pathFilename) {
            return core.cdnFiles.readFileText(pathFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Save a file anywhere on the physical file space of the hosting server.
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="fileContent"></param>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override void Save(string pathFilename, string fileContent) {
            if (core.wwwFiles.isinLocalAbsDosPath(pathFilename)) {
                core.wwwFiles.saveFile(core.wwwFiles.convertLocalAbsToRelativePath(pathFilename), fileContent);
            } else if (core.privateFiles.isinLocalAbsDosPath(pathFilename)) {
                core.privateFiles.saveFile(core.privateFiles.convertLocalAbsToRelativePath(pathFilename), fileContent);
            } else if (core.cdnFiles.isinLocalAbsDosPath(pathFilename)) {
                core.cdnFiles.saveFile(core.cdnFiles.convertLocalAbsToRelativePath(pathFilename), fileContent);
            } else {
                throw (new GenericException("Application cannot access this path [" + pathFilename + "]"));
            }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Save a file in the cdnFiles store.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileContent"></param>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override void SaveVirtual(string filename, string fileContent) {
            core.cdnFiles.saveFile(filename, fileContent);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Test if a file exists anywhere on the physical file space of the hosting server.
        /// </summary>
        /// <param name="pathFileName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override bool fileExists(string pathFileName) {
            bool result = false;
            if (core.wwwFiles.isinLocalAbsDosPath(pathFileName)) {
                result = core.wwwFiles.fileExists(core.wwwFiles.convertLocalAbsToRelativePath(pathFileName));
            } else if (core.privateFiles.isinLocalAbsDosPath(pathFileName)) {
                result = core.privateFiles.fileExists(core.privateFiles.convertLocalAbsToRelativePath(pathFileName));
            } else if (core.cdnFiles.isinLocalAbsDosPath(pathFileName)) {
                result = core.cdnFiles.fileExists(core.cdnFiles.convertLocalAbsToRelativePath(pathFileName));
            } else {
                throw (new GenericException("Application cannot access this path [" + pathFileName + "]"));
            }
            return result;
        }
        //
        //==========================================================================================
        /// <summary>
        /// Test if a folder exists anywhere on the physical file space of the hosting server.
        /// </summary>
        /// <param name="pathFolderName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override bool folderExists(string pathFolderName) {
            bool result = false;
            if (core.wwwFiles.isinLocalAbsDosPath(pathFolderName)) {
                result = core.wwwFiles.pathExists(core.wwwFiles.convertLocalAbsToRelativePath(pathFolderName));
            } else if (core.privateFiles.isinLocalAbsDosPath(pathFolderName)) {
                result = core.privateFiles.pathExists(core.privateFiles.convertLocalAbsToRelativePath(pathFolderName));
            } else if (core.cdnFiles.isinLocalAbsDosPath(pathFolderName)) {
                result = core.cdnFiles.pathExists(core.cdnFiles.convertLocalAbsToRelativePath(pathFolderName));
            } else {
                throw (new GenericException("Application cannot access this path [" + pathFolderName + "]"));
            }
            return result;
        }
        //
        //==========================================================================================
        /// <summary>
        /// Return a parsable comma,crlf delimited string of the files available anywhere on the physical file space of the hosting server.
        /// </summary>
        /// <param name="pathFolderName"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override string fileList(string pathFolderName, int pageSize, int pageNumber) {
            string result = "";
            if (core.wwwFiles.isinLocalAbsDosPath(pathFolderName)) {
                List<FileDetail> fi = core.wwwFiles.getFileList(core.wwwFiles.convertLocalAbsToRelativePath(pathFolderName));
                result = UpgradeController.upgrade51ConvertFileInfoArrayToParseString(fi);
            } else if (core.privateFiles.isinLocalAbsDosPath(pathFolderName)) {
                List<FileDetail> fi = core.privateFiles.getFileList(core.privateFiles.convertLocalAbsToRelativePath(pathFolderName));
                result = UpgradeController.upgrade51ConvertFileInfoArrayToParseString(fi);
            } else if (core.cdnFiles.isinLocalAbsDosPath(pathFolderName)) {
                List<FileDetail> fi = core.cdnFiles.getFileList(core.cdnFiles.convertLocalAbsToRelativePath(pathFolderName));
                result = UpgradeController.upgrade51ConvertFileInfoArrayToParseString(fi);
            } else {
                throw (new GenericException("Application cannot access this path [" + pathFolderName + "]"));
            }
            return result;
        }
        //
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override string fileList(string pathFolderName, int pageSize) {
            return fileList(pathFolderName, pageSize, 1);
        }
        //
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override string fileList(string pathFolderName) {
            return fileList(pathFolderName, 9999, 1);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Return a parsable comma,crlf delimited string of the folders available anywhere on the physical file space of the hosting server.
        /// </summary>
        /// <param name="pathFolderName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.privateFiles, cp.WwwFiles, or cp.Files.serverFiles instead.", false)]
        public override string folderList(string pathFolderName) {
            string result = "";
            if (core.wwwFiles.isinLocalAbsDosPath(pathFolderName)) {
                List<FolderDetail> fi = core.wwwFiles.getFolderList(core.wwwFiles.convertLocalAbsToRelativePath(pathFolderName));
                result = UpgradeController.upgrade51ConvertDirectoryInfoArrayToParseString(fi);
            } else if (core.privateFiles.isinLocalAbsDosPath(pathFolderName)) {
                List<FolderDetail> fi = core.privateFiles.getFolderList(core.privateFiles.convertLocalAbsToRelativePath(pathFolderName));
                result = UpgradeController.upgrade51ConvertDirectoryInfoArrayToParseString(fi);
            } else if (core.cdnFiles.isinLocalAbsDosPath(pathFolderName)) {
                List<FolderDetail> fi = core.cdnFiles.getFolderList(core.cdnFiles.convertLocalAbsToRelativePath(pathFolderName));
                result = UpgradeController.upgrade51ConvertDirectoryInfoArrayToParseString(fi);
            } else {
                throw (new GenericException("Application cannot access this path [" + pathFolderName + "]"));
            }
            return result;
        }
        //
        //==========================================================================================
        /// <summary>
        /// Delete a folder anywhere on the physical file space of the hosting server.
        /// </summary>
        /// <param name="pathFolderName"></param>
        [Obsolete("Deprecated, please use cp.CdnFiles, cp.PrivateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override void DeleteFolder(string pathFolderName) {
            if (core.wwwFiles.isinLocalAbsDosPath(pathFolderName)) {
                core.wwwFiles.deleteFolder(core.wwwFiles.convertLocalAbsToRelativePath(pathFolderName));
            } else if (core.privateFiles.isinLocalAbsDosPath(pathFolderName)) {
                core.privateFiles.deleteFolder( core.privateFiles.convertLocalAbsToRelativePath( pathFolderName));
            } else if (core.cdnFiles.isinLocalAbsDosPath(pathFolderName)) {
                core.cdnFiles.deleteFolder( core.cdnFiles.convertLocalAbsToRelativePath( pathFolderName));
            } else {
                throw (new GenericException("Application cannot access this path [" + pathFolderName + "]"));
            }
        }
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        protected bool disposed_file;
        //
        //==========================================================================================
        /// <summary>
        /// dispose
        /// </summary>
        /// <param name="disposing_file"></param>
        protected virtual void Dispose(bool disposing_file) {
            if (!this.disposed_file) {
                if (disposing_file) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            disposed_file = true;
        }
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPFileClass()  {
            Dispose(false);
        }
        #endregion
    }
}