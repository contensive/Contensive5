
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    public abstract class CPFileSystemBaseClass {
        //
        //==========================================================================================
        /// <summary>
        /// The physical file path of the local storage for this resource
        /// </summary>
        public abstract String PhysicalFilePath { get; }
        //==========================================================================================
        /// <summary>
        /// argument details for file and folder methods
        /// </summary>
        public class FileDetail {
            /// <summary>
            /// file name
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int Attributes { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public DateTime? DateCreated { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public DateTime? DateLastAccessed { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public DateTime? DateLastModified { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public long Size { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Type { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Extension {
                get {
                    if (string.IsNullOrEmpty(_extension)) {
                        if (!string.IsNullOrEmpty(Name)) {
                            int pos = Name.LastIndexOf(".");
                            if ((pos >= 0) && (pos < Name.Length)) {
                                _extension = Name.Substring(pos);
                            }
                        }
                    }
                    return _extension;
                }
            }
            string _extension = "";
        }
        //
        //==========================================================================================
        /// <summary>
        /// argument details for file and folder methods
        /// </summary>
        public class FolderDetail {
            public string Name { get; set; }
            public int Attributes { get; set; }
            public DateTime? DateCreated { get; set; }
            public DateTime? DateLastAccessed { get; set; }
            public DateTime? DateLastModified { get; set; }
            public string Type { get; set; }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Append content to end of a text file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileContent"></param>
        public abstract void Append(string filename, string fileContent);
        //
        //==========================================================================================
        /// <summary>
        /// Copy a file within the same filesystem (TempFiles, cndFiles, wwwFiles, privateFiles)
        /// </summary>
        /// <param name="sourcePathFilename"></param>
        /// <param name="destinationPathFilename"></param>
        public abstract void Copy(string sourcePathFilename, string destinationPathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Copy a file to a different filesystem (TempFiles, cndFiles, wwwFiles, privateFiles)
        /// </summary>
        /// <param name="sourcePathFilename"></param>
        /// <param name="destinationPathFilename"></param>
        /// <param name="destinationFileSystem"></param>
        public abstract void Copy(string sourcePathFilename, string destinationPathFilename, CPFileSystemBaseClass destinationFileSystem);
        //
        //==========================================================================================
        /// <summary>
        /// Recursively copy all files in a path to a path the same filesystem (TempFiles, cndFiles, wwwFiles, privateFiles). A path is either blank or begins with a folder name and ends with '\'. Valid sample paths are '', 'MyFolder\SubFolder\'
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        public abstract void CopyPath(string sourcePath, string destinationPath);
        //
        //==========================================================================================
        /// <summary>
        /// Recursively copy all files in a path to a path in a different filesystem (TempFiles, cndFiles, wwwFiles, privateFiles). A path is either blank or begins with a folder name and ends with '\'. Valid sample paths are '', 'MyFolder\SubFolder\'
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="destinationFileSystem"></param>
        public abstract void CopyPath(string sourcePath, string destinationPath, CPFileSystemBaseClass destinationFileSystem);
        //
        //==========================================================================================
        /// <summary>
        /// Copy a file from the local storage to its remote store
        /// </summary>
        /// <param name="pathFilename"></param>
        public abstract void CopyLocalToRemote(string pathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Copy a file from the remote storate to its local store.
        /// </summary>
        /// <param name="pathFilename">The path followed by the filename. Path arguments have no leading slash, but they include a trailing slash. ex ParentFolder/SubFolder/Filename.ext</param>
        public abstract void CopyRemoteToLocal(string pathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Create a folder in a path. 
        /// </summary>
        /// <param name="pathFolder">A path followed by a folder name. (ex ParentFolder/SubFolder/NewFolderName ) Path arguments should have no leading slash.</param>
        public abstract void CreateFolder(string pathFolder);
        //
        //==========================================================================================
        /// <summary>
        /// Create a folder with a unique name. Return the path to the new folder. (Path values have no leading slash, and have a trailing slash, ex NewPath/ )
        /// </summary>
        /// <returns></returns>
        public abstract string CreateUniqueFolder();
        //
        //==========================================================================================
        /// <summary>
        /// Delete a file in a path. Path arguments should have no leading slash. (ex ParentFolder/FileToDelete.txt )
        /// </summary>
        /// <param name="pathFilename"></param>
        public abstract void DeleteFile(string pathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Delete a folder and all files and subfolders. Path arguments should have no leading slash. (ex ParentFolder/FolderToDelete )
        /// </summary>
        /// <param name="folderPath"></param>
        public abstract void DeleteFolder(string folderPath);
        //
        //==========================================================================================
        /// <summary>
        /// Read a text file. Path arguments should have no leading slash. (ex ParentFolder/FileToRead.txt )
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        public abstract string Read(string pathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Read a file to a byte array. Path arguments should have no leading slash. (ex ParentFolder/FileToRead.bin )
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        public abstract byte[] ReadBinary(string pathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Save content to a text file. Path arguments should have no leading slash. (ex ParentFolder/FileToSave.txt )
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="fileContent"></param>
        public abstract void Save(string pathFilename, string fileContent);
        //
        //==========================================================================================
        /// <summary>
        /// Save a byte array to a file. Path arguments should have no leading slash. (ex ParentFolder/FileToSave.bin )
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="fileContent"></param>
        public abstract void SaveBinary(string pathFilename, byte[] fileContent);
        //
        //==========================================================================================
        /// <summary>
        /// Get the details of all files in a folder. Path arguments should have no leading slash. (ex ParentFolder/FolderName )
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public abstract List<FileDetail> FileList(string folderPath, int pageSize, int pageNumber);
        public abstract List<FileDetail> FileList(string folderPath, int pageSize);
        public abstract List<FileDetail> FileList(string folderPath);
        //
        //==========================================================================================
        /// <summary>
        /// Get the details of a file. Returns null if the file is not found. Path arguments should have no leading slash. (ex ParentFolder/FileName.ext )
        /// </summary>
        /// <param name="PathFilename"></param>
        /// <returns></returns>
        public abstract FileDetail FileDetails(string PathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Get the details of all folders in a path. Path arguments should have no leading slash. (ex ParentFolder )
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public abstract List<FolderDetail> FolderList(string folderPath);
        //
        //==========================================================================================
        /// <summary>
        /// Returns true if a file exists in this path
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        public abstract bool FileExists(string pathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Returns true if a folder exists in this path
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public abstract bool FolderExists(string folderName);
        //
        //==========================================================================================
        /// <summary>
        /// Handle a file upload from a submitted post to the /upload path. If successful, return true and the filename.
        /// </summary>
        /// <param name="htmlformName"></param>
        /// <param name="returnFilename"></param>
        /// <returns></returns>
        public abstract bool SaveUpload(string htmlformName, ref string returnFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Handle a file upload to a path from a submitted post. If successful, return true and the filename.
        /// </summary>
        /// <param name="htmlFormName"></param>
        /// <param name="folderPath"></param>
        /// <param name="returnFilename"></param>
        /// <returns></returns>
        public abstract bool SaveUpload(string htmlFormName, string folderPath, ref string returnFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Returns the path of a pathFilename argument. For example "folder1/folder2/file.txt" returns "folder1/folder2/"
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        public abstract string GetPath(string pathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Returns the path of a pathFilename argument. For example "folder1/folder2/file.txt" returns "file.txt"
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        public abstract string GetFilename(string pathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Create a zip file and add all the files in a path recursively
        /// </summary>
        /// <param name="archivePathFilename"></param>
        /// <param name="path"></param>
        public abstract void ZipPath(string archivePathFilename, string path);
        //
        //==========================================================================================
        /// <summary>
        /// unzip a zip file to a folder with the same name
        /// </summary>
        /// <param name="pathFilename"></param>
        public abstract void UnzipFile(string pathFilename);
        //
        // ====================================================================================================
        /// <summary>
        /// Get url to a text file
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public abstract void SaveHttpGet(string url, string pathFilename );
        //
        // ====================================================================================================
        /// <summary>
        /// Post key/values to  a url and save to text file
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="url"></param>
        /// <param name="requestArguments"></param>
        /// <returns></returns>
        public abstract void SaveHttpPost(string url, string pathFilename, List<KeyValuePair<string, string>> requestArguments);
        //
        // ====================================================================================================
        /// <summary>
        /// post entity to a url and save to text file
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="url"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract void SaveHttpPost(string url, string pathFilename, string entity);
        //
        //==========================================================================================
        // deprecated
        //
    }
}

