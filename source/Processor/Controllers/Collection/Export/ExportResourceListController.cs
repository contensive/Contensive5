
using Contensive.BaseClasses;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// methods to export resources for an addon collection
    /// </summary>
    public static class ExportResourceListController {
        // 
        // ====================================================================================================
        /// <summary>
        /// return a list of files to export.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="fileCrlfList"></param>
        /// <returns></returns>
        public static List<string> getUnixPathFilenameList(CPBaseClass cp, string fileCrlfList) {
            try {
                var result = new List<string>();
                foreach (var pathFilename in fileCrlfList.Split(System.Environment.NewLine.ToCharArray())) {
                    if (!string.IsNullOrEmpty(pathFilename)) {
                        string savePathFilename = pathFilename.Replace(@"\", "/");
                        if (!result.Contains(savePathFilename)) {
                            result.Add(savePathFilename);
                        }
                    }
                }
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }

        // 
        // ====================================================================================================
        /// <summary>
        /// Get collection file list of xml Resource nodes.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="execFileList"></param>
        /// <param name="CollectionGuid"></param>
        /// <param name="tempPathFileList"></param>
        /// <param name="tempExportPath"></param>
        /// <returns></returns>
        public static string getResourceNodeList(CPBaseClass cp, List<string> execFileList, string CollectionGuid, List<string> tempPathFileList, string tempExportPath) {
            try {
                string nodeList = "";
                foreach (var PathFilename in execFileList) {
                    if (!PathFilename.Length.Equals(0)) {
                        string fixedPathFilename = PathFilename.Replace(@"\", "/");
                        string path = "";
                        string filename = fixedPathFilename;
                        int pos = fixedPathFilename.LastIndexOf("/");
                        if (pos >= 0) {
                            filename = fixedPathFilename.Substring(pos + 1);
                            path = fixedPathFilename.Substring(0, pos);
                        }
                        string CollectionPath = "";
                        DateTime LastChangeDate = default;
                        ExportController.getLocalCollectionArgs(cp, CollectionGuid, ref CollectionPath, ref LastChangeDate);
                        if (!CollectionPath.Length.Equals(0)) {
                            CollectionPath += @"\";
                        }
                        string AddonPath = @"addons\";
                        // AddFilename = AddonPath & CollectionPath & Filename
                        cp.PrivateFiles.Copy(AddonPath + CollectionPath + filename, tempExportPath + filename, cp.TempFiles);
                        if (!tempPathFileList.Contains(tempExportPath + filename)) {
                            tempPathFileList.Add(tempExportPath + filename);
                            nodeList = nodeList + System.Environment.NewLine + "\t" + "<Resource name=\"" + System.Net.WebUtility.HtmlEncode(filename) + "\" type=\"executable\" path=\"" + System.Net.WebUtility.HtmlEncode(path) + "\" />";
                        }
                    }

                }
                return nodeList;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return string.Empty;
            }
        }
        // 
        // ====================================================================================================
        // 
        public static string getResourceNodeList(CPBaseClass cp, string execFileList, string CollectionGuid, List<string> tempPathFileList, string tempExportPath) {
            try {
                string nodeList = "";
                if (!execFileList.Length.Equals(0)) {
                    DateTime LastChangeDate = default;
                    // 
                    // There are executable files to include in the collection
                    // If installed, source path is collectionpath, if not installed, collectionpath will be empty
                    // and file will be sourced right from addon path
                    // 
                    string CollectionPath = "";
                    ExportController.getLocalCollectionArgs(cp, CollectionGuid, ref CollectionPath, ref LastChangeDate);
                    if (!CollectionPath.Length.Equals(0)) {
                        CollectionPath += @"\";
                    }
                    string[] Files = execFileList.Split(new[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    for (int Ptr = 0; Ptr < Files.Length; Ptr++) {
                        string PathFilename = Files[Ptr];
                        if (!PathFilename.Length.Equals(0)) {
                            PathFilename = PathFilename.Replace(@"\", "/");
                            string Path = "";
                            string Filename = PathFilename;
                            int Pos = PathFilename.LastIndexOf("/");
                            if (Pos >= 0) {
                                Filename = PathFilename.Substring(Pos + 1);
                                Path = PathFilename.Substring(0, Pos);
                            }
                            string ManualFilename = "";
                            if (Filename.ToLower() != ManualFilename.ToLower()) {
                                string AddonPath = @"addons\";
                                // AddFilename = AddonPath & CollectionPath & Filename
                                cp.PrivateFiles.Copy(AddonPath + CollectionPath + Filename, tempExportPath + Filename, cp.TempFiles);
                                if (!tempPathFileList.Contains(tempExportPath + Filename)) {
                                    tempPathFileList.Add(tempExportPath + Filename);
                                    nodeList = nodeList + System.Environment.NewLine + "\t" + "<Resource name=\"" + System.Net.WebUtility.HtmlEncode(Filename) + "\" type=\"executable\" path=\"" + System.Net.WebUtility.HtmlEncode(Path) + "\" />";
                                }
                            }
                        }
                    }
                }
                return nodeList;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}

