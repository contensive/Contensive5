
using Amazon.S3;
using Amazon.S3.Model;
using Contensive.BaseClasses;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Extensions;
using Contensive.Processor.Models.Domain;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using static Contensive.BaseClasses.CPFileSystemBaseClass;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    //
    //==============================================================================================================
    /// <summary>
    /// Basic file access class for all scaling targets (cdn, private, appRoot, etc).
    /// set isLocal true and all files are handled on the local server
    /// set isLocal false:
    /// - the local filesystem becomes a mirror for the remote system
    /// - after a transaction is complete (read,write,copy) the local file can be deleted
    /// - on remote system, ALL filenames will be converted lowercase
    /// 
    /// </summary>
    public class FileController : IDisposable {
        /// <summary>
        /// core object
        /// </summary>
        private readonly CoreController core;
        /// <summary>
        /// true if the filesystem is local, false if files transfered through the local system to the remote system
        /// </summary>
        public bool isLocal { get; }
        /// <summary>
        /// local location for files accessed by this filesystem, starts with drive-letter, ends with dos slash \
        /// </summary>
        public string localAbsRootPath { get; }
        /// <summary>
        /// For remote files, this path is prefixed to the content. starts with subfolder name, ends in uniz slash /
        /// </summary>
        private string remotePathPrefix { get; }
        /// <summary>
        /// list of files to delete when this object is disposed
        /// </summary>
        public List<string> deleteOnDisposeFileList { get; } = new List<string>();
        /// <summary>
        /// for remote filesystem, a lazy created s3 client
        /// </summary>
        internal AmazonS3Client s3Client {
            get {
                if (local_s3Client == null) {
                    LogController.logInfo(core, "construct Amazon S3 client");

                    local_s3Client = new AmazonS3Client(core.serverConfig.awsAccessKey, core.serverConfig.awsSecretAccessKey, core.awsCredentials.awsRegion);
                };
                return local_s3Client;
            }
        }
        private AmazonS3Client local_s3Client { get; set; }
        /// <summary>
        /// list of paths verified during the scope of this execution. If a path is deleted, it must be removed from this list
        /// </summary>
        private List<string> verifiedRemotePathList { get; } = new List<string>();
        //
        //==============================================================================================================
        /// <summary>
        /// Create a filesystem
        /// </summary>
        /// <param name="core"></param>
        /// <param name="isLocal">If true, thie object reads/saves to the local filesystem</param>
        /// <param name="rootLocalPath"></param>
        /// <param name="remotePathPrefix">If not isLocal, this is added to the remote content path. Ex a\ with content b\c.txt = a\b\c.txt</param>
        public FileController(CoreController core, bool isLocal, string rootLocalPath, string remotePathPrefix) {
            if (string.IsNullOrEmpty(rootLocalPath)) {
                LogController.logError(core, new ArgumentException("Blank file system root path not permitted."));
            } else {
                this.core = core;
                this.isLocal = isLocal;
                this.localAbsRootPath = normalizeDosPath(rootLocalPath);
                this.remotePathPrefix = normalizeDosPath(remotePathPrefix);
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Create a local filesystem
        /// </summary>
        /// <param name="core"></param>
        /// <param name="rootLocalPath"></param>
        public FileController(CoreController core, string rootLocalPath) {
            if (string.IsNullOrEmpty(rootLocalPath)) {
                LogController.logError(core, new ArgumentException("Attempt to create a FileController with blank rootLocalpath."));
                throw new GenericException("Attempt to create a FileController with blank rootLocalpath.");
            }
            this.core = core;
            this.isLocal = true;
            this.localAbsRootPath = normalizeDosPath(rootLocalPath);
            this.remotePathPrefix = "";
        }
        //
        //==============================================================================================================
        /// <summary>
        /// join two paths together to make a single path or filename. changes / to \, and makes sure there is one and only one at the joint
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public string joinPath(string path, string pathFilename) {
            try {
                string dosPath = normalizeDosPath(path);
                string dosPathFilename = normalizeDosPathFilename(pathFilename);
                return Path.Combine(dosPath, dosPathFilename);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Read in a file from a given PathFilename, return content
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public string readFileText(string pathFilename) => readFileText(pathFilename, isLocal);
        //
        // ====================================================================================================
        /// <summary>
        /// test api
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="isLocalFileSystem">if true, files read/write from local, if false files remote, copying to/from local store.</param>
        /// <returns></returns>
        public string readFileText(string pathFilename, bool isLocalFileSystem) {
            string returnContent = "";
            try {
                if (!string.IsNullOrEmpty(pathFilename)) {
                    pathFilename = normalizeDosPathFilename(pathFilename);
                    if (!isLocalFileSystem) {
                        //
                        // -- copy remote file to local
                        if (!copyFileRemoteToLocal(pathFilename)) {
                            //
                            // -- if remote file does not exist, delete local mirror
                            deleteFile_local(pathFilename);
                        }
                    }
                    if (fileExists_local(pathFilename)) {
                        using (StreamReader sr = File.OpenText(convertRelativeToLocalAbsPath(pathFilename))) {
                            returnContent = sr.ReadToEnd();
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnContent;
        }
        //
        //==============================================================================================================
        /// <summary>
        /// reads a binary file and returns a byte array
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public byte[] readFileBinary(string pathFilename) => readFileBinary(pathFilename, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api, called from tests and within this class.
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="isLocalFileSystem"></param>
        /// <returns></returns>
        public byte[] readFileBinary(string pathFilename, bool isLocalFileSystem) {
            byte[] returnContent = Array.Empty<byte>();
            try {
                if (!string.IsNullOrEmpty(pathFilename)) {
                    pathFilename = normalizeDosPathFilename(pathFilename);
                    if (!isLocalFileSystem) {
                        //
                        // -- copy remote file to local
                        if (!copyFileRemoteToLocal(pathFilename)) {
                            //
                            // -- if remote file does not exist, delete local mirror
                            deleteFile_local(pathFilename);
                        }
                    }
                    if (fileExists(pathFilename, isLocalFileSystem)) {
                        using (FileStream sr = File.OpenRead(convertRelativeToLocalAbsPath(pathFilename))) {
                            returnContent = new byte[sr.Length];
                            int bytesRead = sr.Read(returnContent, 0, (int)sr.Length);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnContent;
        }
        //
        //==============================================================================================================
        /// <summary>
        /// save text file
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="FileContent"></param>
        public void saveFile(string pathFilename, string FileContent) => saveFile(pathFilename, FileContent, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api, called from tests and within this class.
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="FileContent"></param>
        /// <param name="isLocalFileSystem"></param>
        public void saveFile(string pathFilename, string FileContent, bool isLocalFileSystem) {
            //
            LogController.logTrace(core, $"FileController.saveFile, [{pathFilename}], isLocalFileSystem [{isLocalFileSystem}]");
            //
            saveFile_TextBinary(pathFilename, FileContent, null, false, isLocalFileSystem);
        }
        //
        //==============================================================================================================
        /// <summary>
        /// test api. save binary file
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="FileContent"></param>
        public void saveFile(string pathFilename, byte[] FileContent)
            => saveFile(pathFilename, FileContent, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api, called from tests and within this class. save binary file
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="FileContent"></param>
        /// <param name="isLocalFileSystem"></param>
        public void saveFile(string pathFilename, byte[] FileContent, bool isLocalFileSystem) {
            saveFile_TextBinary(pathFilename, null, FileContent, true, isLocalFileSystem);
        }
        //
        //==============================================================================================================
        /// <summary>
        /// save binary or text file
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="textContent"></param>
        /// <param name="binaryContent"></param>
        /// <param name="isBinary"></param>
        private void saveFile_TextBinary(string pathFilename, string textContent, byte[] binaryContent, bool isBinary)
            => saveFile_TextBinary(pathFilename, textContent, binaryContent, isBinary, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api, called from tests and within this class. save binary or text file
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="textContent"></param>
        /// <param name="binaryContent"></param>
        /// <param name="isBinary"></param>
        /// <param name="isLocalFileSystem"></param>
        private void saveFile_TextBinary(string pathFilename, string textContent, byte[] binaryContent, bool isBinary, bool isLocalFileSystem) {
            try {
                //
                LogController.logTrace(core, $"FileController.saveFile_TextBinary, [{pathFilename}], isLocalFileSystem [{isLocalFileSystem}]");
                //
                pathFilename = normalizeDosPathFilename(pathFilename);
                //
                // -- write local file
                string path = "";
                string filename = "";
                splitDosPathFilename(pathFilename, ref path, ref filename);
                verifyPath(path, isLocalFileSystem);
                try {
                    if (isBinary) {
                        File.WriteAllBytes(convertRelativeToLocalAbsPath(pathFilename), binaryContent);
                    } else {
                        File.WriteAllText(convertRelativeToLocalAbsPath(pathFilename), textContent);
                    }
                } catch (Exception ex) {
                    LogController.logError(core, ex);
                    throw;
                }
                if (!isLocalFileSystem) {
                    // copy to remote
                    copyFileLocalToRemote(pathFilename);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// append text file
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="fileContent"></param>
        public void appendFile(string pathFilename, string fileContent)
            => appendFile(pathFilename, fileContent, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api, called from tests and within this class. append text file
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="fileContent"></param>
        /// <param name="isLocalFileSystem"></param>
        public void appendFile(string pathFilename, string fileContent, bool isLocalFileSystem) {
            try {
                if (string.IsNullOrWhiteSpace(pathFilename)) {
                    throw new ArgumentException("appendFile called with blank pathname.");
                } else if (!string.IsNullOrEmpty(fileContent)) {
                    //
                    // -- verify local path
                    string absFilename = convertRelativeToLocalAbsPath(pathFilename);
                    string path = "";
                    string filename = "";
                    splitDosPathFilename(pathFilename, ref path, ref filename);
                    verifyPath(path, isLocalFileSystem);
                    if (!isLocalFileSystem) {
                        //
                        // -- non-local, copy remote file to local
                        if (!copyFileRemoteToLocal(pathFilename)) {
                            deleteFile_local(pathFilename);
                        }
                    }
                    if (!File.Exists(absFilename)) {
                        using StreamWriter sw = File.CreateText(absFilename);
                        sw.Write(fileContent);
                    } else {
                        using StreamWriter sw = File.AppendText(absFilename);
                        sw.Write(fileContent);
                    }
                    if (!isLocalFileSystem) {
                        //
                        // -- non-local, copy local file to remote
                        copyFileLocalToRemote(pathFilename);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Creates a file folder if it does not exist
        /// </summary>
        /// <param name="path">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        private void createPath_local(string path) {
            createPathAbs_local(convertRelativeToLocalAbsPath(path));
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Creates a file folder if it does not exist
        /// </summary>
        /// <param name="absPath">Absolute Dos Path and filename in the form "d:\myfiles\myfolder\subfolder\MyFile.txt"</param>
        private void createPathAbs_local(string absPath) {
            try {
                string PartialPath = null;
                int Position = 0;
                string WorkingPath = null;
                //
                if (string.IsNullOrEmpty(absPath)) {
                    throw new ArgumentException("CreateLocalFileFolder called with blank path.");
                } else {
                    WorkingPath = normalizeDosPath(absPath);
                    if (!Directory.Exists(WorkingPath)) {
                        Position = GenericController.strInstr(1, WorkingPath, "\\");
                        while (Position != 0) {
                            PartialPath = WorkingPath.left(Position - 1);
                            if (!Directory.Exists(PartialPath)) {
                                Directory.CreateDirectory(PartialPath);
                            }
                            Position = GenericController.strInstr(Position + 1, WorkingPath, "\\");
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Creates a file folder if it does not exist
        /// </summary>
        /// <param name="pathFolder">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        public void createPath(string pathFolder) => createPath(pathFolder, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api, called from tests and within this class. Creates a file folder if it does not exist
        /// </summary>
        /// <param name="pathFolder">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="isLocalFileSystem"></param>
        public void createPath(string pathFolder, bool isLocalFileSystem) {
            try {
                if (!isLocalFileSystem) {
                    //
                    // -- veriofy remote path only for remote mode
                    verifyPath_remote(pathFolder);
                }
                //
                // todo - consider making a different method that verifies the local path for cases like this...
                // -- always verify local path. Added for collection folder case so developer will see path they need to work in.
                createPath_local(pathFolder);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Create a unique folder. Return the folder in path form (Path arguments have a trailing slash but no leading slash)
        /// </summary>
        /// <returns></returns>
        public string createUniquePath() => createUniquePath(isLocal);
        //
        //==========================================================================================
        /// <summary>
        /// test api, Create a unique folder. Return the folder in path form (Path arguments have a trailing slash but no leading slash)
        /// </summary>
        /// <param name="isLocalFileSystem"></param>
        /// <returns></returns>
        public string createUniquePath(bool isLocalFileSystem) {
            string uniquePath = GenericController.getGUID().Replace("-", "").Replace("{", "").Replace("}", "").ToLowerInvariant() + @"\";
            createPath(uniquePath, isLocalFileSystem);
            return uniquePath;
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Deletes a file if it exists
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        public void deleteFile(string pathFilename)
            => deleteFile(pathFilename, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api, Deletes a file if it exists
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="isLocalFileSystem"></param>
        public void deleteFile(string pathFilename, bool isLocalFileSystem) {
            try {
                if (!string.IsNullOrEmpty(pathFilename)) {
                    if (isLocalFileSystem) {
                        deleteFile_local(pathFilename);
                    } else {
                        deleteFile_remote(pathFilename);
                        deleteFile_local(pathFilename);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Deletes a file if it exists
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        public void deleteFile_remote(string pathFilename) {
            try {
                if (!string.IsNullOrWhiteSpace(pathFilename)) {
                    string DosPathFilename = normalizeDosPathFilename(pathFilename);
                    if (fileExists_remote(DosPathFilename)) {
                        string remoteUnixPathFilename = convertToUnixSlash(joinPath(remotePathPrefix, DosPathFilename));
                        DeleteObjectRequest deleteObjectRequest = new DeleteObjectRequest {
                            BucketName = core.serverConfig.awsBucketName,
                            Key = remoteUnixPathFilename
                        };
                        LogController.logInfo(core, "deleteFile_remote, s3Client.DeleteObject");
                        s3Client.DeleteObjectAsync(deleteObjectRequest).waitSynchronously();
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Delete a local file if it exists
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        private void deleteFile_local(string pathFilename) {
            try {
                pathFilename = normalizeDosPathFilename(pathFilename);
                if (!string.IsNullOrEmpty(pathFilename)) {
                    if (fileExists_local(pathFilename)) {
                        File.Delete(convertRelativeToLocalAbsPath(pathFilename));
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Delete a folder recursively
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        public void deleteFolder(string path) => deleteFolder(path, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api, Delete a folder recursively
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <param name="isLocalFileSystem"></param>
        public void deleteFolder(string path, bool isLocalFileSystem) {
            try {
                string dosPath = normalizeDosPath(path);
                if (!string.IsNullOrEmpty(dosPath)) {
                    if (!isLocalFileSystem) {
                        string remoteUnixPath = joinPath(remotePathPrefix, dosPath).Trim();
                        if ((remoteUnixPath.Length > 1) && (remoteUnixPath.Substring(0, 1) == "\\")) {
                            remoteUnixPath = remoteUnixPath.Substring(1);
                        }
                        if (!string.IsNullOrEmpty(remoteUnixPath)) {
                            //
                            // -- get a list of all objects to delete (the delete call does not include prefix)
                            string unixPath = convertToUnixSlash(joinPath(remotePathPrefix, dosPath));
                            ListObjectsRequest listrequest = new ListObjectsRequest {
                                BucketName = core.serverConfig.awsBucketName,
                                Prefix = unixPath
                            };
                            ListObjectsResponse listResponse = s3Client.ListObjectsAsync(listrequest).waitSynchronously();
                            //
                            // -- create delete request from object list
                            DeleteObjectsRequest deleteRequest = new DeleteObjectsRequest {
                                BucketName = core.serverConfig.awsBucketName
                            };
                            foreach (S3Object entry in listResponse.S3Objects) {
                                deleteRequest.AddKey(entry.Key);
                            }
                            if (deleteRequest.Objects.Count>0) {
                                DeleteObjectsResponse deleteResponse = s3Client.DeleteObjectsAsync(deleteRequest).waitSynchronously();
                            }
                        }
                    }
                    //
                    // -- delete local file if local or remote
                    string localDosAbsPath = joinPath(localAbsRootPath, dosPath);
                    if (localDosAbsPath.Substring(localDosAbsPath.Length - 1) == "\\") {
                        localDosAbsPath = localDosAbsPath.left(localDosAbsPath.Length - 1);
                    }
                    if (pathExists_local(dosPath)) {
                        Directory.Delete(localDosAbsPath, true);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Copies a file to a different file system (FileAppRoot, FileTemp, FileCdn, FilePrivate)
        /// </summary>
        /// <param name="srcPathFilename">Source path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="dstPathFilename">Destination path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="dstFileSystem"></param>
        public void copyFile(string srcPathFilename, string dstPathFilename, FileController dstFileSystem) {
            string hint = "src [" + srcPathFilename + "], dst [" + dstPathFilename + "]";
            try {
                if (string.IsNullOrEmpty(srcPathFilename)) {
                    throw new ArgumentException("Invalid source file.");
                } else if (string.IsNullOrEmpty(dstPathFilename)) {
                    throw new ArgumentException("Invalid destination file.");
                } else if (srcPathFilename.ToLowerInvariant().Equals(dstPathFilename.ToLowerInvariant())) {
                    //
                    // copy file to itself, deletes the file
                    //
                    return;
                } else {
                    hint += ",normalize";
                    srcPathFilename = normalizeDosPathFilename(srcPathFilename);
                    dstPathFilename = normalizeDosPathFilename(dstPathFilename);
                    if (!isLocal) {
                        //
                        // src is remote file - copy file to local mirror
                        hint += ",!isLocal";
                        if (fileExists_remote(srcPathFilename)) {
                            verifyPath_remote(getPath(srcPathFilename));
                            hint += ",copyRemoteToLocal";
                            copyFileRemoteToLocal(srcPathFilename);
                            //
                            // -- copy src to dst on local mirror
                            string dstPath = "";
                            string dstFilename = "";
                            splitDosPathFilename(dstPathFilename, ref dstPath, ref dstFilename);
                            if (!dstFileSystem.pathExists_local(dstPath)) {
                                dstFileSystem.createPath_local(dstPath);
                            }
                            string srcFullPathFilename = joinPath(localAbsRootPath, srcPathFilename);
                            string dstFullPathFilename = joinPath(dstFileSystem.localAbsRootPath, dstPathFilename);
                            if (!dstFileSystem.fileExists_local(dstPathFilename)) {
                                hint += ",does not exist on local dst [" + dstPathFilename + "]";
                            } else {
                                hint += ",delete local dst [" + dstPathFilename + "]";
                                dstFileSystem.deleteFile_local(dstPathFilename);
                            }
                            hint += ",File.copy";
                            File.Copy(srcFullPathFilename, dstFullPathFilename);
                            if (!dstFileSystem.isLocal) {
                                //
                                // -- dst is remote, copy file to remote source
                                dstFileSystem.copyFileLocalToRemote(dstPathFilename);
                            }
                        }
                    } else {
                        //
                        // -- src is local file, copy to dst local
                        hint += "isLocal";
                        if (fileExists_local(srcPathFilename)) {
                            string dstPath = "";
                            string dstFilename = "";
                            splitDosPathFilename(dstPathFilename, ref dstPath, ref dstFilename);
                            if (!dstFileSystem.pathExists_local(dstPath)) {
                                dstFileSystem.createPath_local(dstPath);
                            }
                            string srcFullPathFilename = joinPath(localAbsRootPath, srcPathFilename);
                            string DstFullPathFilename = joinPath(dstFileSystem.localAbsRootPath, dstPathFilename);
                            if (dstFileSystem.fileExists(dstPathFilename, isLocal)) {
                                dstFileSystem.deleteFile(dstPathFilename, dstFileSystem.isLocal);
                            }
                            File.Copy(srcFullPathFilename, DstFullPathFilename);
                            if (!dstFileSystem.isLocal) {
                                //
                                // -- dst is remote, copy file to remote source
                                dstFileSystem.copyFileLocalToRemote(dstPathFilename);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex, hint);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Copy a file within the same filesystem (FileAppRoot, FileTemp, FileCdn, FilePrivate)
        /// </summary>
        /// <param name="srcPathFilename">Source path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="dstPathFilename">Destination path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        public void copyFile(string srcPathFilename, string dstPathFilename) {
            copyFile(srcPathFilename, dstPathFilename, this);
        }
        //
        //==============================================================================================================
        /// <summary>
        /// list of files from the appropriate local/remote filesystem
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns></returns>
        public List<FileDetail> getFileList(string path) {
            try {
                if (!isLocal) {
                    return getFileList_remote(path);
                }
                return getFileList_local(path);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// list of files from the remote server
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns></returns>
        public List<FileDetail> getFileList_remote(string path) {
            try {
                string dosPath = normalizeDosPath(path);
                string remoteUnixPath = convertToUnixSlash(joinPath(remotePathPrefix, dosPath));
                ListObjectsRequest request = new ListObjectsRequest {
                    BucketName = core.serverConfig.awsBucketName,
                    Prefix = remoteUnixPath
                };
                ListObjectsResponse response = s3Client.ListObjectsAsync(request).waitSynchronously();
                IEnumerable<S3Object> fileList = response.S3Objects.Where(x => !x.Key.EndsWith(@"/"));
                var returnFileList = new List<FileDetail>();
                foreach (S3Object file in fileList) {
                    //
                    // -- create a fileDetail for each file found
                    string fileName = file.Key;
                    string keyPath = "";
                    int pos = fileName.LastIndexOf("/");
                    if (pos > -1) {
                        keyPath = fileName.Substring(0, pos + 1);
                        fileName = fileName.Substring(pos + 1);
                    }
                    if (remoteUnixPath.Equals(keyPath)) {
                        returnFileList.Add(new FileDetail {
                            Attributes = 0,
                            Type = "",
                            DateCreated = file.LastModified,
                            DateLastAccessed = file.LastModified,
                            DateLastModified = file.LastModified,
                            Name = fileName,
                            Size = file.Size
                        });

                    }
                };
                return returnFileList;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// list of files from the local server
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns></returns>
        public List<FileDetail> getFileList_local(string path) {
            var returnFileList = new List<FileDetail>();
            try {
                path = normalizeDosPath(path);
                if (pathExists_local(path)) {
                    string localPath = convertRelativeToLocalAbsPath(path);
                    DirectoryInfo di = new DirectoryInfo(localPath);
                    foreach (var file in di.GetFiles()) {
                        //
                        // -- create a fileDetail for each file found
                        returnFileList.Add(new FileDetail {
                            Attributes = (int)file.Attributes,
                            DateCreated = file.CreationTime,
                            DateLastAccessed = file.LastAccessTime,
                            DateLastModified = file.LastWriteTime,
                            Name = file.Name,
                            Size = file.Length,
                            Type = ""
                        });
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnFileList;
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Returns a list of folders in a path, comma delimited
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns></returns>
        public string getFolderNameList(string path) {
            string returnList = "";
            try {
                path = normalizeDosPath(path);
                if (!isLocal) {
                    foreach (var folder in getFolderList_remote(path)) {
                        returnList += "," + folder.Name;
                    }
                    if (!string.IsNullOrEmpty(returnList)) {
                        returnList = returnList.Substring(1);
                    }
                } else {
                    foreach (FolderDetail folder in getFolderList_local(path)) {
                        returnList += "," + folder.Name;
                    }
                    if (!string.IsNullOrEmpty(returnList)) {
                        returnList = returnList.Substring(1);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnList;
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Return a list of subfolders in the path.
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns></returns>
        public List<FolderDetail> getFolderList(string path) => getFolderList(path, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// Return a list of subfolders in the path.
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <param name="isLocalFileSystem"></param>
        /// <returns></returns>
        public List<FolderDetail> getFolderList(string path, bool isLocalFileSystem) {
            try {
                if (!isLocalFileSystem) {
                    return getFolderList_remote(path);
                }
                return getFolderList_local(path);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// REturn a list of subfolders in the path.
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns></returns>
        private List<FolderDetail> getFolderList_remote(string path) {
            try {
                path = normalizeDosPath(path);
                string remoteUnixPath = convertToUnixSlash(joinPath(remotePathPrefix, path));
                ListObjectsRequest request = new ListObjectsRequest {
                    BucketName = core.serverConfig.awsBucketName,
                    Prefix = remoteUnixPath,
                    Delimiter = @"/"
                };
                int prefixLength = remoteUnixPath.Length;
                //
                // Build your call out to S3 and store the response
                var returnFolders = new List<FolderDetail>();
                LogController.logInfo(core, "getFolderList_remote, s3Client.ListObjects, path [" + path + "]");
                ListObjectsResponse response = s3Client.ListObjectsAsync(request).waitSynchronously();
                foreach (var commonPrefix in response.CommonPrefixes) {
                    string subFolder = commonPrefix.Substring(prefixLength);
                    if (string.IsNullOrWhiteSpace(subFolder)) { continue; }
                    //
                    // -- remove trailing slash as this returns folder names, not paths (path ends in slash, folder name ends in the name)
                    subFolder = subFolder.Substring(0, subFolder.Length - 1);
                    //
                    // -- skip subfolders as they match the ends-with-a-slash query
                    if (subFolder.Contains("/")) { continue; }
                    returnFolders.Add(new FolderDetail {
                        Attributes = 0,
                        Type = "",
                        DateCreated = DateTime.MinValue,
                        DateLastAccessed = DateTime.MinValue,
                        DateLastModified = DateTime.MinValue,
                        Name = subFolder
                    });
                }
                return returnFolders;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// REturn a list of subfolders in the path.
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\". </param>
        /// <returns></returns>
        private List<FolderDetail> getFolderList_local(string path) {
            try {
                path = normalizeDosPath(path);
                var returnFolders = new List<FolderDetail>();
                if (pathExists_local(path)) {
                    string localPath = convertRelativeToLocalAbsPath(path);
                    DirectoryInfo di = new DirectoryInfo(localPath);
                    foreach (var folder in di.GetDirectories()) {
                        returnFolders.Add(new FolderDetail {
                            Attributes = (int)folder.Attributes,
                            Type = "",
                            DateCreated = folder.CreationTime,
                            DateLastAccessed = folder.LastWriteTime,
                            DateLastModified = folder.LastWriteTime,
                            Name = folder.Name
                        });
                    }
                }
                return returnFolders;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// return true if the file exists in the selected file system (local or remote)
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        public bool fileExists(string pathFilename) => fileExists(pathFilename, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api.
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="isLocalFileSystem"></param>
        public bool fileExists(string pathFilename, bool isLocalFileSystem) {
            try {
                if (!isLocalFileSystem) {
                    return fileExists_remote(pathFilename);
                }
                string dosPathFilename = normalizeDosPathFilename(pathFilename);
                return fileExists_local(dosPathFilename);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Returns true if the file exists in the local file system
        /// </summary>
        /// <param name="pathFilename">Dos Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        private bool fileExists_local(string pathFilename) {
            try {
                string absDosPathFilename = convertRelativeToLocalAbsPath(pathFilename);
                return File.Exists(absDosPathFilename);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Returns true if the file exists in the remote file system
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        private bool fileExists_remote(string pathFilename) {
            try {
                string remoteUnixPathFilename = convertToUnixSlash(joinPath(remotePathPrefix, pathFilename));
                var request = new ListObjectsRequest {
                    BucketName = core.serverConfig.awsBucketName,
                    Prefix = remoteUnixPathFilename
                };
                var response = s3Client.ListObjectsAsync(request).waitSynchronously();
                return response.S3Objects.Count.Equals(1);
            } catch (AmazonS3Exception ex) {
                //
                // -- support this unwillingly
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return false;
                }
                throw;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Returns true if the path exists
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns></returns>
        public bool pathExists(string path) => pathExists(path, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api. Returns true if the path exists
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <param name="isLocalFileSystem"></param>
        /// <returns></returns>
        public bool pathExists(string path, bool isLocalFileSystem) {
            try {
                if (!isLocalFileSystem) {
                    //
                    // -- remote
                    if (!pathExists_remote(path)) { return false; }
                    //
                    // -- if path exists remote, verify local path is a copy of remote (use case is someone deleting local folder)
                    if (!pathExists_local(path)) {
                        createPath_local(path);
                        copyPathRemoteToLocal(path);
                    }
                    return true;
                } else {
                    //
                    // -- local
                    return pathExists_local(path);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Returns true if the local folder exists
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns></returns>
        public bool pathExists_local(string path) {
            try {
                string absPath = convertRelativeToLocalAbsPath(path);
                return Directory.Exists(absPath);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Returns true if the remote folder exists
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns></returns>
        public bool pathExists_remote(string path) {
            try {
                //
                // -- get a list of all objects to delete (the delete call does not include prefix)
                string unixPath = convertToUnixSlash(joinPath(remotePathPrefix, path));
                ListObjectsRequest listrequest = new ListObjectsRequest {
                    BucketName = core.serverConfig.awsBucketName,
                    Prefix = unixPath,
                    MaxKeys = 1
                };
                ListObjectsResponse listResponse = s3Client.ListObjectsAsync(listrequest).waitSynchronously();
                return listResponse.S3Objects.Count > 0;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Rename a file
        /// </summary>
        /// <param name="srcPathFilename">Source path and filename in the form "myfolder\subfolder\MyFile1.txt"</param>
        /// <param name="dstFilename">New filename in the form "MyFile2.txt"</param>
        public void renameFile(string srcPathFilename, string dstFilename)
            => renameFile(srcPathFilename, dstFilename, isLocal);
        //
        //==============================================================================================================
        /// <summary>
        /// test api. Rename a file
        /// </summary>
        /// <param name="srcPathFilename">Source path and filename in the form "myfolder\subfolder\MyFile1.txt"</param>
        /// <param name="dstFilename">New filename in the form "MyFile2.txt"</param>
        /// <param name="isLocalFileSystem"></param>
        public void renameFile(string srcPathFilename, string dstFilename, bool isLocalFileSystem) {
            try {
                srcPathFilename = normalizeDosPathFilename(srcPathFilename);
                if (string.IsNullOrEmpty(srcPathFilename)) {
                    throw new GenericException("Invalid source file");
                } else {
                    if (!isLocalFileSystem) {
                        string dstPath = getPath(srcPathFilename);
                        copyFile(srcPathFilename, dstPath + dstFilename);
                        deleteFile(srcPathFilename, isLocalFileSystem);
                    } else {
                        string srcFullPathFilename = joinPath(localAbsRootPath, srcPathFilename);
                        int Pos = srcPathFilename.LastIndexOf("\\") + 1;
                        string srcFullPath = "";
                        if (Pos >= 0) {
                            srcFullPath = joinPath(localAbsRootPath, srcPathFilename.left(Pos));
                        }
                        if (string.IsNullOrEmpty(dstFilename)) {
                            throw new GenericException("Invalid destination file []");
                        } else if (dstFilename.IndexOf("\\") != -1) {
                            throw new GenericException("Invalid '\\' character in destination filename [" + dstFilename + "]");
                        } else if (dstFilename.IndexOf("/") != -1) {
                            throw new GenericException("Invalid '/' character in destination filename [" + dstFilename + "]");
                        } else if (!fileExists(srcPathFilename, isLocalFileSystem)) {
                            //
                            // not an error, to minimize file use, empty files are not created, so missing files are just empty
                            //
                        } else {
                            File.Move(srcFullPathFilename, joinPath(srcFullPath, dstFilename));
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// getDriveFreeSpace
        /// </summary>
        public double getDriveFreeSpace() {
            double returnSize = 0;
            try {
                if (!isLocal) {
                    // todo remote file case
                    returnSize = 1000000000;
                } else {
                    DriveInfo scriptingDrive = null;
                    string driveLetter;
                    //
                    // Drive Space
                    //
                    driveLetter = localAbsRootPath.left(1);
                    scriptingDrive = new DriveInfo(driveLetter);
                    if (scriptingDrive.IsReady) {
                        returnSize = scriptingDrive.AvailableFreeSpace;
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnSize;
        }
        //
        //==============================================================================================================
        /// <summary>
        /// copy one folder to another, include subfolders
        /// </summary>
        /// <param name="srcAbsDosPath"></param>
        /// <param name="dstDosPath"></param>
        /// <param name="dstFileSystem"></param>
        private void copyFolder_srcLocal(string srcAbsDosPath, string dstDosPath, FileController dstFileSystem = null) {
            try {
                if (Directory.Exists(srcAbsDosPath)) {
                    if (dstFileSystem == null) {
                        dstFileSystem = this;
                    }
                    string dstAbsDstPath = joinPath(dstFileSystem.localAbsRootPath, dstDosPath);
                    //
                    // -- create destination folder
                    if (!Directory.Exists(dstAbsDstPath)) {
                        createPathAbs_local(dstAbsDstPath);
                    }
                    DirectoryInfo srcDirectoryInfo = new DirectoryInfo(srcAbsDosPath);
                    DirectoryInfo dstDiretoryInfo = new DirectoryInfo(dstAbsDstPath);
                    //
                    // -- copy each file
                    foreach (FileInfo srcFile in srcDirectoryInfo.GetFiles()) {
                        srcFile.CopyTo(joinPath(dstDiretoryInfo.ToString(), srcFile.Name), true);
                        if (!dstFileSystem.isLocal) {
                            //
                            // -- now copy the dst file to the remote
                            dstFileSystem.copyFileLocalToRemote(joinPath(dstDosPath, srcFile.Name));
                        }
                    }
                    //
                    // -- copy each folder
                    foreach (DirectoryInfo srcSubDirectory in srcDirectoryInfo.GetDirectories()) {
                        string dstFolder = srcSubDirectory.Name;
                        string dstSubPath = dstDosPath + dstFolder + "\\";
                        DirectoryInfo dstSubDirectory = dstDiretoryInfo.CreateSubdirectory(dstFolder);
                        copyFolder_srcLocal(srcSubDirectory.FullName, dstSubPath, dstFileSystem);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// copy one folder to another, include subfolders
        /// </summary>
        /// <param name="srcAbsDosPath">Source absolute path in the form "d:\myfiles\myfolder\subfolder\MyFilesrc.txt"</param>
        /// <param name="dstDosPath">Destination Path in the form "myfolder\subfolder\MyFiledst.txt"</param>
        /// <param name="dstFileSystem"></param>
        private void copyFolder_srcRemote(string srcAbsDosPath, string dstDosPath, FileController dstFileSystem = null) {
            try {
                if (Directory.Exists(srcAbsDosPath)) {
                    if (dstFileSystem == null) {
                        dstFileSystem = this;
                    }
                    string dstAbsDstPath = joinPath(dstFileSystem.localAbsRootPath, dstDosPath);
                    //
                    // -- create destination folder
                    if (!Directory.Exists(dstAbsDstPath)) {
                        createPathAbs_local(dstAbsDstPath);
                    }
                    DirectoryInfo srcDirectoryInfo = new DirectoryInfo(srcAbsDosPath);
                    DirectoryInfo dstDiretoryInfo = new DirectoryInfo(dstAbsDstPath);
                    //
                    // -- copy each file
                    foreach (FileInfo srcFile in srcDirectoryInfo.GetFiles()) {
                        srcFile.CopyTo(joinPath(dstDiretoryInfo.ToString(), srcFile.Name), true);
                        if (!dstFileSystem.isLocal) {
                            //
                            // -- now copy the dst file to the remote
                            dstFileSystem.copyFileLocalToRemote(joinPath(dstDosPath, srcFile.Name));
                        }
                    }
                    //
                    // -- copy each folder
                    foreach (DirectoryInfo srcSubDirectory in srcDirectoryInfo.GetDirectories()) {
                        string dstFolder = srcSubDirectory.Name;
                        string dstSubPath = dstDosPath + dstFolder + "\\";
                        DirectoryInfo dstSubDirectory = dstDiretoryInfo.CreateSubdirectory(dstFolder);
                        copyFolder_srcLocal(srcSubDirectory.FullName, dstSubPath, dstFileSystem);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// copy one folder to another, include subfolders
        /// </summary>
        /// <param name="srcPath">Source path in the form "myfolder\subfolder\"</param>
        /// <param name="dstPath">Destination path in the form "myfolder\subfolder\"</param>
        /// <param name="dstFileSystem"></param>
        public void copyPath(string srcPath, string dstPath, FileController dstFileSystem = null) {
            try {
                srcPath = normalizeDosPath(srcPath);
                dstPath = normalizeDosPath(dstPath);
                if (!isLocal) {
                    //
                    // -- src remote
                    copyFolder_srcRemote(joinPath(localAbsRootPath, srcPath), dstPath, dstFileSystem);
                } else {
                    //
                    // -- src local
                    copyFolder_srcLocal(joinPath(localAbsRootPath, srcPath), dstPath, dstFileSystem);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=========================================================================================================
        /// <summary>
        /// saveHttpRequestToFile
        /// </summary>
        /// <param name="Link"></param>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        public void saveHttpRequestToFile(string Link, string pathFilename)
            => saveHttpRequestToFile(Link, pathFilename, isLocal);
        //
        //=========================================================================================================
        /// <summary>
        /// saveHttpRequestToFile
        /// </summary>
        /// <param name="Link"></param>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="isLocalFileSystem"></param>
        public void saveHttpRequestToFile(string Link, string pathFilename, bool isLocalFileSystem) {
            try {
                pathFilename = normalizeDosPathFilename(pathFilename);
                if ((!string.IsNullOrEmpty(pathFilename)) && (!string.IsNullOrEmpty(Link))) {
                    string URLLink = GenericController.strReplace(Link, " ", "%20");
                    HttpController HTTP = new HttpController {
                        timeout = 600
                    };
                    HTTP.getUrlToFile(encodeText(URLLink), convertRelativeToLocalAbsPath(pathFilename));
                    //
                    if (!isLocalFileSystem) {
                        copyFileLocalToRemote(pathFilename);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Unzip a zipfile
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.zip"</param>
        public void unzipFile(string pathFilename) {
            try {
                pathFilename = normalizeDosPathFilename(pathFilename);
                bool processLocalFile = true;
                if (!isLocal) {
                    //
                    // -- remote file, copy file to local drive
                    if (!copyFileRemoteToLocal(pathFilename)) {
                        processLocalFile = false;
                        deleteFile_local(pathFilename);
                    }
                }
                if (processLocalFile) {
                    string path = "";
                    string filename = "";
                    splitDosPathFilename(pathFilename, ref path, ref filename);
                    string absPathFilename = convertRelativeToLocalAbsPath(pathFilename);
                    string absPath = "";
                    splitDosPathFilename(absPathFilename, ref absPath, ref filename);
                    string fileFilter = null;
                    FastZip fastZip = new FastZip();
                    fastZip.ExtractZip(absPathFilename, absPath, fileFilter);
                    //
                    if (!isLocal) {
                        //
                        // -- remote file, copy files back to remote
                        using (var fs = new FileStream(absPathFilename, FileMode.Open, FileAccess.Read)) {
                            using (var zf = new ZipFile(fs)) {
                                foreach (ZipEntry ze in zf) {
                                    if (ze.IsDirectory) {
                                        verifyPath_remote(getPath(joinPath(path, ze.Name)));
                                    } else {
                                        copyFileLocalToRemote(joinPath(path, ze.Name));
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// Zip a folder and add to a zip file
        /// </summary>
        /// <param name="archivePathFilename">Path and filename of the zip file in the form "myfolder\subfolder\MyFile.zip"</param>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        public void zipPath(string archivePathFilename, string path) {
            try {
                archivePathFilename = normalizeDosPathFilename(archivePathFilename);
                path = normalizeDosPath(path);
                //
                string archivepath = "";
                string archiveFilename = "";
                splitDosPathFilename(archivePathFilename, ref archivepath, ref archiveFilename);
                //
                string archivePath = getPath(archivePathFilename);
                verifyPath(archivepath, true);
                //
                FastZip fastZip = new FastZip();
                fastZip.CreateZip(convertRelativeToLocalAbsPath(archivePathFilename), convertRelativeToLocalAbsPath(path), true, null);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a path argument (relative to rootPath) into a full absolute path. Allow for the case where the path is incorrectly a full path within the rootpath
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public string convertRelativeToLocalAbsPath(string pathFilename) {
            string result = pathFilename;
            try {
                string normalizedPathFilename = normalizeDosPathFilename(pathFilename);
                if (string.IsNullOrEmpty(normalizedPathFilename)) {
                    result = localAbsRootPath;
                } else if (isinLocalAbsDosPath(normalizedPathFilename)) {
                    result = normalizedPathFilename;
                } else if (normalizedPathFilename.IndexOf(":\\") >= 0) {
                    throw new GenericException("Attempt to access an invalid path [" + normalizedPathFilename + "] that is not within the allowed path [" + localAbsRootPath + "].");
                } else {
                    result = joinPath(localAbsRootPath, normalizedPathFilename);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Convert an absolute DOS absolute pathFilename (d:\myfiles\myfolder\myfile.txt) to a relative pathFilename (myfolder\myfile.txt)
        /// </summary>
        /// <param name="absDosPathFilename">Path and filename in the form "d:\myfiles\myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public string convertLocalAbsToRelativePath(string absDosPathFilename) {
            //
            // -- protect against argument issue
            if (string.IsNullOrWhiteSpace(absDosPathFilename)) { return string.Empty; }
            //
            if (absDosPathFilename.ToLower(CultureInfo.InvariantCulture).IndexOf(localAbsRootPath.ToLower(CultureInfo.InvariantCulture)).Equals(0)) {
                return absDosPathFilename.Substring(localAbsRootPath.Length);
            }
            return absDosPathFilename;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Remove characters not valid in a filename. NOT for PathFilename. This will remote the path delimiters (slashes)
        /// </summary>
        /// <param name="filename">filename in the form "MyFile.txt"</param>
        /// <returns></returns>
        public static string normalizeDosFilename(string filename) {
            string invalid = new string(Path.GetInvalidFileNameChars());
            foreach (char c in invalid) {
                filename = filename.Replace(c.ToString(), "_");
            }
            return filename;
        }

        //
        //====================================================================================================
        /// <summary>
        /// Result dos-slashed, can be empty (), a path (mypath\), a filename (myfile.bin), or a pathFilename (mypath\myFile.bin)
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public static string normalizeDosPathFilename(string pathFilename) {
            //
            // -- protect against argument issue
            if (string.IsNullOrWhiteSpace(pathFilename)) {
                return string.Empty;
            }
            //
            // -- convert to dos slash and lowercase()
            // no, should not lowercase the filenames, just the path. An uploaded image to S3 must match the link saved for it so any case change must happen before call to fileController.
            string returnPathFilename = pathFilename.Replace("/", "\\");
            //
            // -- remove accidental double slashes
            while (returnPathFilename.IndexOf("\\\\") >= 0) {
                returnPathFilename = returnPathFilename.Replace("\\\\", "\\");
            }
            if (string.IsNullOrEmpty(returnPathFilename) || (returnPathFilename == "\\")) {
                //
                // -- return empty if result is empty or just a slash
                return string.Empty;
            } else if (returnPathFilename.Substring(0, 1) == "\\") {
                //
                // -- if path starts with a slash, return string without slash
                return returnPathFilename.Substring(1);
            };
            return returnPathFilename;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Result dos-slashed, can be empty (), a path that starts with a foldername and ends with a slash (mypath\) (mypath\another\)
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder"</param>
        /// <returns></returns>
        public static string normalizeDosPath(string path) {
            if (!string.IsNullOrWhiteSpace(path)) {
                //
                // -- normalize, allowing for a trailing filename
                string dosPath = normalizeDosPathFilename(path);
                if (!string.IsNullOrWhiteSpace(dosPath)) {
                    //
                    // -- verify the trailing string is a path, not a file
                    if (dosPath.Substring(dosPath.Length - 1, 1) != "\\") {
                        return dosPath + "\\";
                    } else {
                        return dosPath;
                    }
                }
            }
            return string.Empty;
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns true only if the path is an absolute path and it is within the filesystems root path. False if not absolute path or absolute path not in filesystem
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder"</param>
        /// <returns></returns>
        public bool isinLocalAbsDosPath(string path) {
            return (normalizeDosPath(path).ToLowerInvariant().IndexOf(localAbsRootPath.ToLowerInvariant()) == 0);
        }
        //
        //========================================================================
        /// <summary>
        /// save a file uploaded to the website. Path is where to store it, returnFilename is the resulting file
        /// </summary>
        /// <param name="htmlTagName"></param>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <param name="returnFilename"></param>
        /// <returns></returns>
        public bool upload(string htmlTagName, string path, ref string returnFilename) {
            //
            // -- protect against argument issue
            if (string.IsNullOrWhiteSpace(htmlTagName)) {
                LogController.logWarn(core, "upload called with nullOrWhieSpace htmlTagName.");
                return false;
            }
            returnFilename = "";
            try {
                string key = htmlTagName.ToLowerInvariant();
                if (core.docProperties.containsKey(key)) {
                    var docProperty = core.docProperties.getProperty(key);
                    if ((docProperty.propertyType == DocPropertyModel.DocPropertyTypesEnum.file) && (docProperty.name.ToLowerInvariant() == key)) {
                        string dstDosPath = FileController.normalizeDosPath(path);
                        returnFilename = encodeDosFilename(docProperty.value);
                        string dstDosPathFilename = dstDosPath + returnFilename;
                        deleteFile(dstDosPathFilename, isLocal);
                        if (docProperty.windowsTempfilename != "") {
                            //
                            // copy tmp private files to the appropriate folder in the destination file system
                            //
                            var WindowsTempFiles = new FileController(core, System.IO.Path.GetTempPath());
                            WindowsTempFiles.copyFile(docProperty.windowsTempfilename, dstDosPathFilename, this);

                            //core.tempFiles.copyFile(docProperty.tempfilename, dstDosPathFilename, this);
                            //
                            if (!isLocal) {
                                copyFileLocalToRemote(dstDosPathFilename);
                            }
                            return true;
                        }
                    }
                }
                return false;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// return the standard tablename fieldname path -- always lowercase.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static string getVirtualTableFieldUnixPath(string tableName, string fieldName) {
            string result = tableName + "/" + fieldName + "/";
            return result.ToLowerInvariant().Replace(" ", "_").Replace(".", "_");
        }
        //
        //========================================================================
        /// <summary>
        /// Create and return the standard record file path
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordID"></param>
        /// <returns></returns>
        public static string getVirtualRecordUnixPath(string tableName, string fieldName, int recordID) {
            return getVirtualTableFieldUnixPath(tableName, fieldName) + recordID.ToString().PadLeft(12, '0') + "/";
        }
        //
        //========================================================================
        /// <summary>
        /// Create a filename for the Virtual Directory for a fieldtypeFile or Image (an uploaded file)
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordID"></param>
        /// <param name="originalFilename"></param>
        /// <returns></returns>
        public static string getVirtualRecordUnixPathFilename(string tableName, string fieldName, int recordID, string originalFilename) {
            return getVirtualRecordUnixPath(tableName, fieldName, recordID) + originalFilename;
        }
        //
        //========================================================================
        /// <summary>
        /// Create a filename for the virtual directory for field types not associated to upload files
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="recordId"></param>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        public static string getVirtualRecordUnixPathFilename(string tableName, string fieldName, int recordId, CPContentClass.FieldTypeIdEnum fieldType) {
            string idFilename;
            if (recordId == 0) {
                idFilename = getGUID().Replace("{", "").Replace("}", "").Replace("-", "");
            } else {
                idFilename = recordId.ToString().PadLeft(12, '0');
            }
            switch (fieldType) {
                case CPContentBaseClass.FieldTypeIdEnum.FileCSS: {
                        return getVirtualTableFieldUnixPath(tableName, fieldName) + idFilename + ".css";
                    }
                case CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                        return getVirtualTableFieldUnixPath(tableName, fieldName) + idFilename + ".xml";
                    }
                case CPContentBaseClass.FieldTypeIdEnum.FileJavascript: {
                        return getVirtualTableFieldUnixPath(tableName, fieldName) + idFilename + ".js";
                    }
                case CPContentBaseClass.FieldTypeIdEnum.FileHTML: {
                        return getVirtualTableFieldUnixPath(tableName, fieldName) + idFilename + ".html";
                    }
                case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                        return getVirtualTableFieldUnixPath(tableName, fieldName) + idFilename + ".html";
                    }
                default: {
                        return getVirtualTableFieldUnixPath(tableName, fieldName) + idFilename + ".txt";
                    }
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Return the file size
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public int getFileSize(string pathFilename) {
            try {
                string dosPathFilename = normalizeDosPathFilename(pathFilename);
                if (!isLocal) {
                    //
                    // -- 0 if the remote file does not exist
                    FileDetail remoteFile = getFileDetails_remote(dosPathFilename);
                    if (remoteFile == null) return 0;
                    return (int)remoteFile.Size;
                } else {
                    List<FileDetail> files = getFileList(dosPathFilename);
                    if (files.Count > 0) {
                        return (int)(files[0].Size);
                    }
                    return 0;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if in remote file mode and the local file needs to be updated from the remote file
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public bool localFileStale(string pathFilename) {
            if (isLocal) return false;
            //
            // -- true if the local file does not exist
            if (!fileExists_local(pathFilename)) return true;
            //
            // -- true if the local file details cannot be read
            string dosPathFilename = normalizeDosPathFilename(pathFilename);
            FileDetail localFile = getFileDetails_local(dosPathFilename);
            if (localFile == null) return true;
            //
            // -- false if the remote file does not exist
            FileDetail remoteFile = getFileDetails_remote(dosPathFilename);
            if (remoteFile == null) return false;
            //
            // -- false if remote and local files are the same size and modification date, or the remote is older, dont copy
            return ((remoteFile.Size != localFile.Size) || (remoteFile.DateLastModified > localFile.DateLastModified));
        }
        //
        //====================================================================================================
        /// <summary>
        /// copy a file (object) up to s3. Returns false if the local file does not exist.
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public bool copyFileLocalToRemote(string pathFilename) {
            bool result = false;
            try {
                //
                LogController.logTrace(core, $"copyFileLocalToRemote, [{pathFilename}], isLocal [{isLocal}]");
                //
                // -- if local mode, done
                if (isLocal) return false;
                //
                // -- if local file does not exist, done
                pathFilename = normalizeDosPathFilename(pathFilename);
                if (!fileExists_local(pathFilename)) return false;
                //
                // -- verify the remote path
                verifyPath_remote(getPath(pathFilename));
                //
                // -- Setup request for putting an object in S3.
                PutObjectRequest request = new PutObjectRequest {
                    BucketName = core.serverConfig.awsBucketName,
                    Key = convertToUnixSlash(joinPath(remotePathPrefix, pathFilename)),
                    FilePath = joinPath(localAbsRootPath, convertToDosSlash(pathFilename))
                };
                //
                // -- Make service call and get back the response.
                PutObjectResponse response = s3Client.PutObjectAsync(request).waitSynchronously();
                result = true;
                //
                LogController.logTrace(core, $"copyFileLocalToRemote, exit [{result}]");
                //
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// copy a file (object) from remote to local. Returns false if the remote file does not exist. The localDosPath must exist.
        /// not exist
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        public bool copyFileRemoteToLocal(string pathFilename) {
            try {
                if (isLocal) return false;
                string dosPathFilename = normalizeDosPathFilename(pathFilename);
                //
                // -- check if local mirror has an up-to-date copy of the file
                if (!localFileStale(dosPathFilename)) return true;
                //
                // note: local call is not exception, can be used regardless of isLocal
                verifyPath_remote(getPath(dosPathFilename));
                string localDosPathFilename = convertToDosSlash(dosPathFilename);
                //
                // -- delete local file (for both cases, remote exists and remote does not)
                deleteFile_local(localDosPathFilename);
                if (!fileExists_remote(dosPathFilename)) {
                    return false;
                }
                //
                // -- remote file exists, verify local folder (or AWS returns error)
                verifyPath_local(getPath(dosPathFilename));
                //
                // -- remote file exists, copy remote to local
                string remoteUnixAbsPathFilename = convertToUnixSlash(joinPath(remotePathPrefix, dosPathFilename));
                GetObjectRequest request = new GetObjectRequest {
                    BucketName = core.serverConfig.awsBucketName,
                    Key = remoteUnixAbsPathFilename
                };
                using (GetObjectResponse response = s3Client.GetObjectAsync(request).waitSynchronously())
                using (var source = new CancellationTokenSource()) {
                    try {
                        response.WriteResponseStreamToFileAsync(joinPath(localAbsRootPath, localDosPathFilename), true, source.Token).waitSynchronously();
                    } catch (System.IO.IOException) {
                        // -- pause 1 second and retry
                        System.Threading.Thread.Sleep(1000);
                        response.WriteResponseStreamToFileAsync(joinPath(localAbsRootPath, localDosPathFilename), true, source.Token).waitSynchronously();
                    }
                }
                return true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Copy a remote path to the local file system
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder"</param>
        public void copyPathLocalToRemote(string path) {
            string dosPath = normalizeDosPath(path);
            foreach (var folder in getFolderList_local(dosPath)) {
                copyPathLocalToRemote(dosPath + folder.Name + "\\");
            }
            foreach (var file in getFileList_local(dosPath)) {
                copyFileLocalToRemote(dosPath + file.Name);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Copy a path from the remote file store to the local file store
        /// </summary>
        /// <param name="path">Path in the form "MyFolder\SubFolder\"</param>
        public void copyPathRemoteToLocal(string path) {
            try {
                path = normalizeDosPath(path);
                foreach (var folder in getFolderList_remote(path)) {
                    copyPathRemoteToLocal(path + folder.Name + "\\");
                }
                foreach (var file in getFileList_remote(path)) {
                    copyFileRemoteToLocal(path + file.Name);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify a path exist within the local filesystem
        /// </summary>
        /// <param name="path">Path in the form "MyFolder\SubFolder\"</param>
        private void verifyPath_local(string path) {
            try {
                if (string.IsNullOrEmpty(path)) { return; }
                path = normalizeDosPath(path);
                if (!pathExists_local(path)) {
                    createPath_local(path);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// if remote path does not exist, it is created
        /// </summary>
        /// <param name="path">Path in the form "MyFolder\SubFolder\"</param>
        private void verifyPath_remote(string path) {
            try {
                if (string.IsNullOrEmpty(path)) { return; }
                string remoteUnixPath = convertToUnixSlash(joinPath(remotePathPrefix, path));
                if (!verifiedRemotePathList.Contains(remoteUnixPath)) {
                    UrlDetailsClass remoteUnixPathSplit = GenericController.splitUrl(remoteUnixPath);
                    string bucketKey = "";
                    foreach (string subPathLowercase in remoteUnixPathSplit.pathSegments) {
                        bucketKey += subPathLowercase + "/";
                        string unixPath = convertToUnixSlash(bucketKey);
                        if (!verifiedRemotePathList.Contains(unixPath)) {
                            //
                            // -- is there at least one object that matches the prefix
                            ListObjectsRequest listrequest = new ListObjectsRequest {
                                BucketName = core.serverConfig.awsBucketName,
                                Prefix = unixPath,
                                MaxKeys = 1
                            };
                            ListObjectsResponse listResponse = s3Client.ListObjectsAsync(listrequest).waitSynchronously();
                            if (listResponse.S3Objects.Count == 0) {
                                //
                                // -- creates a zero-length object with the name of the folder (hack AWS uses for thier consol)
                                PutObjectRequest request = new PutObjectRequest();
                                request.BucketName = core.serverConfig.awsBucketName;
                                request.Key = remoteUnixPath;
                                request.ContentType = "text/plain";
                                request.ContentBody = "";
                                s3Client.PutObjectAsync(request).GetAwaiter().GetResult();
                            }
                            verifiedRemotePathList.Add(unixPath);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// verify the path exists. If it does not it is created
        /// </summary>
        /// <param name="path">Path in the form "MyFolder\SubFolder\"</param>
        public void verifyPath(string path) => verifyPath(path, isLocal);
        //
        //====================================================================================================
        /// <summary>
        /// test api, called from tests and within this class.
        /// </summary>
        /// <param name="path">Path in the form "MyFolder\SubFolder\"</param>
        /// <param name="isLocalFileSystem"></param>
        public void verifyPath(string path, bool isLocalFileSystem) {
            try {
                path = normalizeDosPath(path);
                if (isLocalFileSystem) {
                    // -- files stored locally
                    verifyPath_local(path);
                } else {
                    // -- files transfered through local to remote
                    verifyPath_local(path);
                    verifyPath_remote(path);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the actual filename, or blank if the file is not found
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns>The correct case for the path and filename</returns>
        public string correctFilenameCase(string pathFilename) {
            try {
                string filename = "";
                string path = "";
                splitDosPathFilename(pathFilename, ref path, ref filename);
                filename = filename.ToLowerInvariant();
                FileDetail resultFile = getFileList(path).Find(x => x.Name.ToLowerInvariant() == filename);
                if (resultFile != null) {
                    filename = resultFile.Name;
                }
                return filename;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a string a valid Dos filename. Replace all non-allowed characters with underscore.
        /// </summary>
        /// <param name="filename">Filename in the form "MyFile.txt"</param>
        /// <returns></returns>
        public static string encodeDosFilename(string filename) {
            const string allowed = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ^&'@{}[],$-#()%.+~_";
            return encodeFilename(filename, allowed);
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a string a valid Unix filename. Replace all non-allowed characters with underscore.
        /// </summary>
        /// <param name="filename">Filename in the form "MyFile.txt"</param>
        /// <returns></returns>
        public static string encodeUnixFilename(string filename) {
            const string allowed = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._";
            return encodeFilename(filename, allowed);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Replace all non-allowed characters with underscore.
        /// </summary>
        /// <param name="filename">Filename in the form "MyFile.txt"</param>
        /// <param name="allowedCharacters"></param>
        /// <returns></returns>
        private static string encodeFilename(string filename, string allowedCharacters) {
            string result = "";
            int Cnt = filename.Length;
            if (Cnt > 254) Cnt = 254;
            for (int Ptr = 1; Ptr <= Cnt; Ptr++) {
                string chr = filename.Substring(Ptr - 1, 1);
                if (allowedCharacters.IndexOf(chr) + 1 >= 0) {
                    result += chr;
                } else {
                    result += "_";
                }
            }
            return result;
        }
        //
        //==============================================================================================================
        /// <summary>
        /// return a FileDetail object if the file is found, else return null
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public FileDetail getFileDetails(string pathFilename) {
            try {
                if (!isLocal) {
                    return getFileDetails_remote(pathFilename);
                }
                return getFileDetails_local(pathFilename);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// internal method, return a FileDetail object if the file is found, else return null
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        private FileDetail getFileDetails_local(string pathFilename) {
            try {
                //
                // Create new FileInfo object and get the Length.
                string dosPathFilename = encodeDosFilename(pathFilename);
                string absDosPathFilename = convertRelativeToLocalAbsPath(dosPathFilename);
                FileInfo fileInfo = new FileInfo(absDosPathFilename);
                if (!fileInfo.Exists) { return null; }
                return new FileDetail {
                    Attributes = (int)fileInfo.Attributes,
                    DateCreated = fileInfo.CreationTime,
                    DateLastAccessed = fileInfo.LastAccessTime,
                    DateLastModified = fileInfo.LastWriteTime,
                    Name = fileInfo.Name,
                    Size = fileInfo.Length,
                    Type = ""
                };
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// internal method, return a FileDetail object if the file is found, else return null
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        private FileDetail getFileDetails_remote(string pathFilename) {
            try {
                if (string.IsNullOrWhiteSpace(pathFilename)) { return null; }
                string dosPathFilename = normalizeDosPathFilename(pathFilename);
                string filename = getFilename(dosPathFilename);
                if (string.IsNullOrWhiteSpace(filename)) { return null; }
                string remoteUnixPathFilename = convertToUnixSlash(joinPath(remotePathPrefix, dosPathFilename));
                string remoteUnixPath = convertToUnixSlash(getPath(remoteUnixPathFilename));
                ListObjectsRequest request = new ListObjectsRequest {
                    BucketName = core.serverConfig.awsBucketName,
                    Prefix = remoteUnixPathFilename
                };
                ListObjectsResponse response = s3Client.ListObjectsAsync(request).waitSynchronously();
                IEnumerable<S3Object> s3fileList = response.S3Objects.Where(x => x.Key == remoteUnixPathFilename);
                foreach (var s3File in s3fileList) {
                    //
                    // -- create a fileDetail for each file found
                    string fileName = s3File.Key;
                    string keyPath = "";
                    int pos = fileName.LastIndexOf("/");
                    if (pos > -1) {
                        keyPath = fileName.Substring(0, pos + 1);
                        fileName = fileName.Substring(pos + 1);
                    }
                    if (remoteUnixPath.Equals(keyPath)) {
                        return new FileDetail {
                            Attributes = 0,
                            Type = "",
                            DateCreated = s3File.LastModified,
                            DateLastAccessed = s3File.LastModified,
                            DateLastModified = s3File.LastModified,
                            Name = fileName,
                            Size = s3File.Size
                        };
                    }
                };
                return null;
            } catch (Amazon.S3.AmazonS3Exception ex) {
                //
                // -- support this unwillingly
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                }
                throw;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// return a path and a filename from a pathFilename
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="path">Returns path in the form "myfolder\subfolder"</param>
        /// <param name="filename">Returns filename in the form "MyFile.txt"</param>
        public void splitDosPathFilename(string pathFilename, ref string path, ref string filename) {
            try {
                string dosPathFilename = encodeDosFilename(pathFilename);
                filename = Path.GetFileName(dosPathFilename);
                path = getPath(dosPathFilename);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //==============================================================================================================
        /// <summary>
        /// return the path and filename with unix slashes
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <param name="path">Returns path and filename in the form "myfolder\subfolder"</param>
        /// <param name="filename">Returns filename in the form "MyFile.txt"</param>
        public void splitUnixPathFilename(string pathFilename, ref string path, ref string filename) {
            string unixPathFilename = encodeDosFilename(pathFilename);
            filename = Path.GetFileName(unixPathFilename);
            path = getPath(unixPathFilename);
            //splitDosPathFilename(pathFilename, ref path, ref filename);
            //path = convertToUnixSlash(path);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the path of a pathFilename.
        /// myfilename.txt returns empty
        /// mypath\ returns mypath\
        /// mypath\myfilename returns mypath\
        /// mypath\more\myfilename returns mypath\more\
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public static string getPath(string pathFilename) {
            string path = Path.GetDirectoryName(pathFilename);
            if (string.IsNullOrEmpty(path)) { return string.Empty; }
            if (path.right(1).Equals(@"\")) { return path; }
            return path + @"\";
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the filename of a pathFilename.
        /// myfilename.txt returns myfilename.txt
        /// mypath\ returns empty
        /// mypath\myfilename returns myfilename
        /// </summary>
        /// <param name="pathFilename">Path and filename in the form "myfolder\subfolder\MyFile.txt"</param>
        /// <returns></returns>
        public static string getFilename(string pathFilename) {
            return Path.GetFileName(pathFilename);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Convert Unix slashes to DOS slashes
        /// </summary>
        /// <param name="path">Path in the form "myfolder/subfolder/"</param>
        /// <returns>Returns path in the form "myfolder\subfolder\"</returns>
        public static string convertToDosSlash(string path) {
            return path.Replace("/", "\\");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Convert DOS slashes to Unix slashes
        /// </summary>
        /// <param name="path">Path in the form "myfolder\subfolder\"</param>
        /// <returns>Return path in the form "myfolder/subfolder/"</returns>
        public static string convertToUnixSlash(string path) {
            return path.Replace("\\", "/");
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        // dispose
        //
        #region  IDisposable Support 
        /// <summary>
        /// state of dispose
        /// </summary>
        protected bool disposed;
        /// <summary>
        /// internal dispose method
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                    if (deleteOnDisposeFileList.Count > 0) {
                        foreach (string filename in deleteOnDisposeFileList) {
                            deleteFile(filename, isLocal);
                        }
                    }
                    if (local_s3Client != null) {
                        local_s3Client.Dispose();
                    }
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }
        /// <summary>
        /// Do not change or add Overridable to these methods. Put cleanup code in Dispose(ByVal disposing As Boolean).
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// distructor
        /// </summary>
        ~FileController() {
            Dispose(false);
        }
        #endregion
    }
}
