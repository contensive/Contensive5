
using System;
using System.Collections.Generic;
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;

namespace Contensive.Processor {
    public class CPFileSystemClass : BaseClasses.CPFileSystemBaseClass, IDisposable {
        //
        /// <summary>
        /// The instance of the controller used to implement this instance. Either core.TempFiles, core.wwwFiles, core.cdnFiles, or core.appRootFiles
        /// </summary>
        private FileController fileSystemController;
        //
        //==========================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="core"></param>
        public CPFileSystemClass(FileController fileSystemController) {
            this.fileSystemController = fileSystemController;
        }
        //
        //==========================================================================================
        /// <summary>
        /// The physical file path to the local storage used for this file system resource. 
        /// </summary>
        public override string PhysicalFilePath {
            get { return fileSystemController.localAbsRootPath; }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Append a file with content. NOTE: avoid with all remote file systems
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileContent"></param>
        public override void Append(string filename, string fileContent) {
            fileSystemController.appendFile(filename, fileContent);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Copy a file within the same filesystem
        /// </summary>
        /// <param name="sourceFilename"></param>
        /// <param name="destinationFilename"></param>
        public override void Copy(string sourceFilename, string destinationFilename) {
            fileSystemController.copyFile(sourceFilename, destinationFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Copy a file to another file system
        /// </summary>
        /// <param name="sourceFilename"></param>
        /// <param name="destinationFilename"></param>
        /// <param name="destinationFileSystem"></param>
        public override void Copy(string sourceFilename, string destinationFilename, BaseClasses.CPFileSystemBaseClass destinationFileSystem) {
            fileSystemController.copyFile(sourceFilename, destinationFilename, ((CPFileSystemClass)destinationFileSystem).fileSystemController);
        }
        //
        //==========================================================================================
        //
        public override void CopyPath(string sourcePath, string destinationPath) {
            fileSystemController.copyPath(sourcePath, destinationPath);
        }
        //
        //==========================================================================================
        //
        public override void CopyPath(string sourcePath, string destinationPath, CPFileSystemBaseClass destinationFileSystem) {
            fileSystemController.copyPath(sourcePath, destinationPath, ((CPFileSystemClass)destinationFileSystem).fileSystemController);
        }
        //
        //==========================================================================================
        //
        public override void CopyLocalToRemote(string pathFilename) {
            fileSystemController.copyFileLocalToRemote(pathFilename);
        }
        //
        //==========================================================================================
        public override void CopyRemoteToLocal(string pathFilename) {
            fileSystemController.copyFileRemoteToLocal(pathFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Create a folder in a path. Path arguments should have no leading slash.
        /// </summary>
        /// <param name="pathFolder"></param>
        public override void CreateFolder(string pathFolder) {
            fileSystemController.createPath(pathFolder);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Create a unique folder. Return the folder in path form (Path arguments have a trailing slash but no leading slash)
        /// </summary>
        /// <returns></returns>
        public override string CreateUniqueFolder() {
            return fileSystemController.createUniquePath();
        }
        //
        //==========================================================================================
        /// <summary>
        /// Delete a file if it exists.
        /// </summary>
        /// <param name="filename"></param>
        public override void DeleteFile(string filename) {
            fileSystemController.deleteFile(filename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Read a text file. If the file does not exist, returns empty.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public override string Read(string filename) {
            return fileSystemController.readFileText(filename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Read binary file to binary array. If the file does not exist, returns empty.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public override byte[] ReadBinary(string filename) {
            return fileSystemController.readFileBinary(filename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Save text file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileContent"></param>
        public override void Save(string filename, string fileContent) {
            fileSystemController.saveFile(filename, fileContent);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Save response from an http get
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="url"></param>
        public override void SaveHttpGet(string url, string pathFilename) {
            fileSystemController.saveHttpRequestToFile(url, pathFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// save response from an http post
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="url"></param>
        /// <param name="requestArguments"></param>
        public override void SaveHttpPost(string url, string pathFilename, List<KeyValuePair<string, string>> requestArguments) {
            throw new NotImplementedException();
        }
        //
        //==========================================================================================
        /// <summary>
        /// save response from an http post
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="url"></param>
        /// <param name="entity"></param>
        public override void SaveHttpPost(string url,string pathFilename,  string entity) {
            throw new NotImplementedException();
        }
        //
        //==========================================================================================
        /// <summary>
        /// Save binary file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileContent"></param>
        public override void SaveBinary(string filename, byte[] fileContent) {
            fileSystemController.saveFile(filename, fileContent);
        }
        //
        //==========================================================================================
        /// <summary>
        /// return if file exists
        /// </summary>
        /// <param name="pathFileName"></param>
        /// <returns></returns>
        public override bool FileExists(string pathFileName) {
            return fileSystemController.fileExists(pathFileName);
        }
        //
        //==========================================================================================
        /// <summary>
        /// return if a folder exists
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public override bool FolderExists(string folderName) {
            return fileSystemController.pathExists(folderName);
        }
        //
        //==========================================================================================
        /// <summary>
        /// get file details
        /// </summary>
        /// <param name="PathFilename"></param>
        /// <returns></returns>
        public override FileDetail FileDetails(string PathFilename) {
            return fileSystemController.getFileDetails(PathFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// get file list
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public override List<FileDetail> FileList(string folderName, int pageSize, int pageNumber) {
            return fileSystemController.getFileList(folderName);
        }
        //
        //==========================================================================================
        /// <summary>
        /// get file list
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public override List<FileDetail> FileList(string folderName, int pageSize) {
            return fileSystemController.getFileList(folderName);
        }
        //
        //==========================================================================================
        /// <summary>
        /// get file list
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public override List<FileDetail> FileList(string folderName) {
            return fileSystemController.getFileList(folderName);
        }
        //
        //==========================================================================================
        /// <summary>
        /// get folder list
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public override List<FolderDetail> FolderList(string folderName) {
            return fileSystemController.getFolderList(folderName);
        }
        //
        //==========================================================================================
        /// <summary>
        /// delete folder
        /// </summary>
        /// <param name="folderPath"></param>
        public override void DeleteFolder(string folderPath) {
            fileSystemController.deleteFolder(folderPath);
        }
        //
        //==========================================================================================
        /// <summary>
        /// save an upload to a file
        /// </summary>
        /// <param name="htmlformName"></param>
        /// <param name="returnFilename"></param>
        /// <returns></returns>
        public override bool SaveUpload(string htmlformName, ref string returnFilename) {
            return fileSystemController.upload(htmlformName, "\\upload", ref returnFilename);
        }
        //
        //==========================================================================================
        //
        public override bool SaveUpload(string htmlformName, string folderpath, ref string returnFilename) {
            return fileSystemController.upload(htmlformName, folderpath, ref returnFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Returns the path of a pathFilename argument. For example "folder1/folder2/file.txt" returns "folder1/folder2/"
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        public override string GetPath(string pathFilename) {
            return FileController.getPath(pathFilename);
        }
        //
        //==========================================================================================
        /// <summary>
        /// Returns the path of a pathFilename argument. For example "folder1/folder2/file.txt" returns "file.txt"
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        public override string GetFilename(string pathFilename) {
            return FileController.getFilename(pathFilename);
        }
        //
        //==========================================================================================
        //
        public override void ZipPath(string archivePathFilename, string path) {
            fileSystemController.zipPath(archivePathFilename, path);
        }
        //
        //==========================================================================================
        //
        public override void UnzipFile(string pathFilename) {
            fileSystemController.unzipFile(pathFilename);
        }
        //
        //==========================================================================================
        //
        public override bool isValidPathFilename(string pathFilename) {
            return fileSystemController.isValidPathFilename(pathFilename);
        }
        //
        //==========================================================================================
        //
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        protected bool disposed_filesystem;
        //
        //==========================================================================================
        /// <summary>
        /// dispose
        /// </summary>
        /// <param name="disposing_filesystem"></param>
        protected virtual void Dispose(bool disposing_filesystem) {
            if (!this.disposed_filesystem) {
                if (disposing_filesystem) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            disposed_filesystem = true;
        }
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPFileSystemClass() {
            Dispose(false);
        }
        #endregion
    }
}