
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Exceptions;
using Contensive.Models.Db;

namespace Contensive.Processor.Addons.AdminSite {
    public class ListViewExport {
        //
        //=============================================================================
        //   Export the Admin List form results
        //=============================================================================
        //
        public static string get(CoreController core, AdminDataModel adminData) {
            string result = "";
            try {
                //
                bool AllowContentAccess = false;
                string ButtonCommaList = "";
                string ExportName = null;
                string Description = null;
                string Content = "";
                string Button = null;
                int RecordLimit = 0;
                int recordCnt = 0;
                string sqlFieldList = "";
                string SQLFrom = "";
                string SQLWhere = "";
                string SQLOrderBy = "";
                bool IsLimitedToSubContent = false;
                string ContentAccessLimitMessage = "";
                Dictionary<string, bool> FieldUsedInColumns = new Dictionary<string, bool>();
                Dictionary<string, bool> IsLookupFieldValid = new Dictionary<string, bool>();
                IndexConfigClass IndexConfig = null;
                string SQL = null;
                bool IsRecordLimitSet = false;
                string RecordLimitText = null;
                var cacheNameList = new List<string>();
                DataSourceModel datasource = DataSourceModel.create(core.cpParent, adminData.adminContent.dataSourceId, ref cacheNameList);
                //
                // ----- Process Input
                //
                Button = core.docProperties.getText("Button");
                if (Button == ButtonCancelAll) {
                    //
                    // Cancel out to the main page
                    //
                    return core.webServer.redirect("?", "CancelAll button pressed on Index Export");
                } else if (Button != ButtonCancel) {
                    //
                    // get content access rights
                    //
                    var userContentPermissions = PermissionController.getUserContentPermissions(core, adminData.adminContent);
                    if (!userContentPermissions.allowEdit) {
                        //
                        // You must be a content manager of this content to use this tool
                        //
                        Content = ""
                            + "<p>You must be a content manager of " + adminData.adminContent.name + " to use this tool. Hit Cancel to return to main admin page.</p>"
                            + HtmlController.inputHidden(RequestNameAdminSubForm, AdminFormIndex_SubFormExport) + "";
                        ButtonCommaList = ButtonCancelAll;
                    } else {
                        IsRecordLimitSet = false;
                        if (string.IsNullOrEmpty(Button)) {
                            //
                            // Set Defaults
                            //
                            ExportName = "";
                            RecordLimit = 0;
                            RecordLimitText = "";
                        } else {
                            ExportName = core.docProperties.getText("ExportName");
                            RecordLimitText = core.docProperties.getText("RecordLimit");
                            if (!string.IsNullOrEmpty(RecordLimitText)) {
                                IsRecordLimitSet = true;
                                RecordLimit = GenericController.encodeInteger(RecordLimitText);
                            }
                        }
                        if (string.IsNullOrEmpty(ExportName)) {
                            ExportName = adminData.adminContent.name + " export for " + core.session.user.name;
                        }
                        //
                        // Get the SQL parts
                        //
                        IndexConfig = IndexConfigClass.get(core, adminData);
                        ListView.setIndexSQL(core, adminData, IndexConfig, ref AllowContentAccess, ref sqlFieldList, ref SQLFrom, ref SQLWhere, ref SQLOrderBy, ref IsLimitedToSubContent, ref ContentAccessLimitMessage, ref FieldUsedInColumns, IsLookupFieldValid);
                        if (!AllowContentAccess) {
                            //
                            // This should be caught with check earlier, but since I added this, and I never make mistakes, I will leave this in case there is a mistake in the earlier code
                            //
                            Processor.Controllers.ErrorController.addUserError(core, "Your account does not have access to any records in '" + adminData.adminContent.name + "'.");
                        } else {
                            //
                            // Get the total record count
                            //
                            SQL = "select count(" + adminData.adminContent.tableName + ".ID) as cnt from " + SQLFrom + " where " + SQLWhere;
                            using (var csData = new CsModel(core)) {
                                csData.openSql(SQL, datasource.name);
                                if (csData.ok()) {
                                    recordCnt = csData.getInteger("cnt");
                                }
                            }
                            //
                            // Build the SQL
                            //
                            SQL = "select";
                            if (IsRecordLimitSet && (datasource.dbTypeId != DataSourceTypeODBCMySQL)) {
                                SQL += " Top " + RecordLimit;
                            }
                            SQL += " " + adminData.adminContent.tableName + ".* From " + SQLFrom + " WHERE " + SQLWhere;
                            if (!string.IsNullOrEmpty(SQLOrderBy)) {
                                SQL += " Order By" + SQLOrderBy;
                            }
                            if (IsRecordLimitSet && (datasource.dbTypeId == DataSourceTypeODBCMySQL)) {
                                SQL += " Limit " + RecordLimit;
                            }
                            //
                            // Assumble the SQL
                            //
                            if (recordCnt == 0) {
                                //
                                // There are no records to request
                                //
                                Content = ""
                                    + "<p>This selection has no records. Hit Cancel to return to the " + adminData.adminContent.name + " list page.</p>"
                                    + HtmlController.inputHidden(RequestNameAdminSubForm, AdminFormIndex_SubFormExport) + "";
                                ButtonCommaList = ButtonCancel;
                            } else if (Button == ButtonRequestDownload) {
                                //
                                // Request the download
                                //
                                var ExportCSVAddon = core.cacheStore.addonCache.create(addonGuidExportCSV);
                                if (ExportCSVAddon == null) {
                                    LogController.logError(core, new GenericException("ExportCSV addon not found. Task could not be added to task queue."));
                                } else {
                                    var docProperties = new Dictionary<string, string> {
                                                { "sql", SQL },
                                                { "datasource", "default" }
                                            };
                                    var cmdDetail = new TaskModel.CmdDetailClass {
                                        addonId = ExportCSVAddon.id,
                                        addonName = ExportCSVAddon.name,
                                        args = docProperties
                                    };
                                    TaskSchedulerController.addTaskToQueue(core, cmdDetail, false, ExportName, "export_" + adminData.adminContent.name.Replace(" ","_") + ".csv");
                                }
                                //
                                Content = ""
                                    + "<p>Your export has been requested and will be available shortly in the <a href=\"?" + rnAdminForm + "=" + AdminFormDownloads + "\">Download Manager</a>. Hit Cancel to return to the " + adminData.adminContent.name + " list page.</p>"
                                    + HtmlController.inputHidden(RequestNameAdminSubForm, AdminFormIndex_SubFormExport) + "";
                                //
                                ButtonCommaList = ButtonCancel;
                            } else {
                                //
                                // no button or refresh button, Ask are you sure
                                //
                                Content += HtmlController.div(
                                    HtmlController.label("Export Name", "export-name")
                                    + HtmlController.inputText(core, "ExportName", ExportName, "form-control", "export-name")
                                    , "form-group");
                                Content += HtmlController.div(
                                    HtmlController.label("Records Found", "records-found")
                                    + HtmlController.inputText(core, "RecordCnt", recordCnt.ToString(), "form-control", "records-found",true)
                                    , "form-group");
                                Content += HtmlController.div(
                                    HtmlController.label("Record Limit", "record-limit")
                                    + HtmlController.inputText(core, "RecordLimit", RecordLimitText, "form-control", "record-limit")
                                    , "form-group");
                                if (core.session.isAuthenticatedDeveloper()) {
                                    Content += HtmlController.div(
                                         HtmlController.label("Results SQL", "export-query")
                                         + HtmlController.inputTextarea(core, "sql", SQL, 4,-1,"export-query",false,false, "form-control")
                                         , "form-group");
                                }
                                //
                                Content = ""
                                    //+ "\r<style>"
                                    //+ cr2 + ".exportTblCaption {width:100px;}"
                                    //+ cr2 + ".exportTblInput {}"
                                    //+ "\r</style>"
                                    + Content + HtmlController.inputHidden(RequestNameAdminSubForm, AdminFormIndex_SubFormExport) + "";
                                ButtonCommaList = ButtonCancel + "," + ButtonRequestDownload;
                                if (core.session.isAuthenticatedDeveloper()) {
                                    ButtonCommaList = ButtonCommaList + "," + ButtonRefresh;
                                }
                            }
                        }
                    }
                    //
                    Description = "<p>This tool creates an export of the current admin list page results. If you would like to download the current results, select a format and press OK. Your search results will be submitted for export. Your download will be ready shortly in the download manager. To exit without requesting an output, hit Cancel.</p>";
                    result = AdminUIController.getToolBody(core, adminData.adminContent.name + " Export", ButtonCommaList, "", false, false, Description, "", 10, Content);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
    }
}
