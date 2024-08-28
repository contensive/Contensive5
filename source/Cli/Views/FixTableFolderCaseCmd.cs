
using System;
using System.Collections.Generic;
using System.IO;
using Contensive.Processor;
using Contensive.BaseClasses;
using Contensive.Models.Db;

namespace Contensive.CLI {
    static class FixTableFolderCaseCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--fixtablefoldercase"
            + Environment.NewLine + "    For local file sites only. Rename all table folders (ccTablename/fieldName) and the filenames saved in the table fields to reflect lowecase table and fields names."
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// Filenames created from Db tables should always be tablename.ToLower() and fieldName.ToLower(). Go through all tables and correct both the fields and the folders
        /// </summary>
        /// <param name="appName"></param>
        public static void execute( CPClass cpServer, string appName) {
            try {
                //
                if (!cpServer.serverOk) {
                    Console.WriteLine("Server Configuration not loaded correctly. Please run --configure");
                    return;
                }
                if (!cpServer.core.serverConfig.apps.ContainsKey( appName )) {
                    Console.WriteLine("The application [" + appName + "] was not found in this server group.");
                    return;
                }
                if (!cpServer.core.serverConfig.isLocalFileSystem) {
                    Console.WriteLine("This server is NOT in localmode. renaming local folders can only be run in localmode.");
                    return;
                }
                using (var cp = new CPClass(appName)) {
                    Console.Write("\n\rRename all tablePathnames to lowercase table and fieldnames.");
                    var rootFolderList = cp.CdnFiles.FolderList("");
                    var contentList = DbBaseModel.createList<ContentModel>(cp, "");
                    var filenameinFieldTypeList = new List<CPContentBaseClass.FieldTypeIdEnum>() {
                        CPContentBaseClass.FieldTypeIdEnum.FileCSS,
                        CPContentBaseClass.FieldTypeIdEnum.FileHTML,
                        CPContentBaseClass.FieldTypeIdEnum.FileImage,
                        CPContentBaseClass.FieldTypeIdEnum.FileJavaScript,
                        CPContentBaseClass.FieldTypeIdEnum.FileText,
                        CPContentBaseClass.FieldTypeIdEnum.FileXML,
                        CPContentBaseClass.FieldTypeIdEnum.File
                    };
                    foreach ( var content in contentList ) {
                        Console.Write("\n\rContent [" + content.name + "].");
                        var contentMeta = Contensive.Processor.Models.Domain.ContentMetadataModel.create(cp.core, content.id);
                        if ( contentMeta?.tableName != null ) {
                            string tablename = contentMeta.tableName.ToLower();
                            //
                            // -- check root folder if it matches the tablename, verify case
                            foreach ( var rootFolder in rootFolderList ) {
                                if (rootFolder.Name.ToLower()==tablename ) {
                                    if (rootFolder.Name != tablename) {
                                        //
                                        // -- folder needs to be renamed
                                        Console.Write("\n\rCdn folder needs to be renamed, original [" + rootFolder.Name + "], corrected [" + tablename + "].");
                                        var dir = new DirectoryInfo( cp.CdnFiles.PhysicalFilePath + rootFolder.Name);
                                        dir.MoveTo(cp.CdnFiles.PhysicalFilePath + tablename + "_tmpRename");
                                        dir.MoveTo(cp.CdnFiles.PhysicalFilePath + tablename);
                                    }
                                    //
                                    // -- verify all subfolders that match a field are lowercase 
                                    var subFolderList = cp.CdnFiles.FolderList(rootFolder.Name);
                                    foreach ( var field in contentMeta.fields) {
                                        foreach (var subFolder in subFolderList) {
                                            if ( field.Value.nameLc == subFolder.Name.ToLower() ) {
                                                //
                                                // -- there is a folder named for this field, correct the folder case and rename all the filenames in the field
                                                if (field.Value.nameLc != subFolder.Name) {
                                                    //
                                                    // -- fieldname matches but case is wrong
                                                    Console.Write("\n\rCdn subfolder needs to be renamed, original [" + tablename + "\\" + subFolder.Name + "], corrected [" + tablename + "\\" + field.Value.nameLc + "].");
                                                    var dir = new DirectoryInfo(cp.CdnFiles.PhysicalFilePath + tablename + "\\" + subFolder.Name);
                                                    dir.MoveTo(cp.CdnFiles.PhysicalFilePath + tablename + "\\" + field.Value.nameLc + "_tmpRename");
                                                    dir.MoveTo(cp.CdnFiles.PhysicalFilePath + tablename + "\\" + field.Value.nameLc);
                                                }
                                                //
                                                // -- if this field type is a filename-in-field-type, run a sql server rename
                                                if ( filenameinFieldTypeList.Contains( field.Value.fieldTypeId )) {
                                                    string sql;
                                                    //
                                                    // -- fix case for tablename with correct slash
                                                    sql = "update " + tablename + " set " + field.Value.nameLc + " = REPLACE(CAST([" + field.Value.nameLc + "] as NVarchar(MAX)),'" + tablename + "/','" + tablename + "/') where(" + field.Value.nameLc + " like '" + tablename + "/%')";
                                                    cp.Db.ExecuteNonQuery(sql);
                                                    //
                                                    // -- fix case for tablename with wrong slash
                                                    sql = "update " + tablename + " set " + field.Value.nameLc + " = REPLACE(CAST([" + field.Value.nameLc + "] as NVarchar(MAX)),'" + tablename + "\\','" + tablename + "/') where(" + field.Value.nameLc + " like '" + tablename + "\\%')";
                                                    cp.Db.ExecuteNonQuery(sql);
                                                    //
                                                    // -- fix case for fieldname with correct slash
                                                    sql = "update " + tablename + " set " + field.Value.nameLc + " = REPLACE(CAST([" + field.Value.nameLc + "] as NVarchar(MAX)),'" + tablename + "/" + field.Value.nameLc + "/','" + tablename + "/" + field.Value.nameLc + "/') where(" + field.Value.nameLc + " like '" + tablename + "/" + field.Value.nameLc + "/%')";
                                                    cp.Db.ExecuteNonQuery(sql);
                                                    //
                                                    // -- fix case for fieldname with wrong slash
                                                    sql = "update " + tablename + " set " + field.Value.nameLc + " = REPLACE(CAST([" + field.Value.nameLc + "] as NVarchar(MAX)),'" + tablename + "/" + field.Value.nameLc + "\\','" + tablename + "/" + field.Value.nameLc + "/') where(" + field.Value.nameLc + " like '" + tablename + "/" + field.Value.nameLc + "\\%')";
                                                    cp.Db.ExecuteNonQuery(sql);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                //
                //
                //
            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
    }
}
