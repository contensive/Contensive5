
using System;
using System.Collections.Generic;
using Contensive.BaseClasses;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using NLog;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite {
    public class DownloadManagerAddon : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get(((CPClass)cpBase).core);
        }
        //
        //========================================================================
        //
        public static string get(CoreController core) {
            string result = null;
            try {
                LayoutBuilderBaseClass layout = core.cpParent.AdminUI.CreateLayoutBuilder();
                //
                string Button = core.docProperties.getText(RequestNameButton);
                if (Button == ButtonCancel) {
                    return core.webServer.redirect("/" + core.appConfig.adminRoute, "Downloads, Cancel Button Pressed");
                }
                string Content = "";
                //
                if (!core.session.isAuthenticatedAdmin()) {
                    //
                    // Must be admin
                    layout.addFormButton(ButtonCancel);
                    Content = Content + AdminUIController.getFormBodyAdminOnly();
                } else {
                    int ContentId = core.docProperties.getInteger("ContentID");
                    string Format = core.docProperties.getText("Format");
                    //
                    // Process Requests
                    //
                    if (!string.IsNullOrEmpty(Button)) {
                        int RowCnt = 0;
                        switch (Button) {
                            case ButtonDelete:
                                RowCnt = core.docProperties.getInteger("RowCnt");
                                if (RowCnt > 0) {
                                    int RowPtr = 0;
                                    for (RowPtr = 0; RowPtr < RowCnt; RowPtr++) {
                                        if (core.docProperties.getBoolean("Row" + RowPtr)) {
                                            int deleteId = core.docProperties.getInteger("RowID" + RowPtr);
                                            //
                                            // -- delete cdn file if present, then delete the task record
                                            var taskToDelete = DbBaseModel.create<TaskModel>(core.cpParent, deleteId);
                                            if (taskToDelete != null) {
                                                if (!string.IsNullOrEmpty(taskToDelete.downloadPathFilename)) {
                                                    try {
                                                        core.cdnFiles.deleteFile(taskToDelete.downloadPathFilename);
                                                    } catch (Exception exFile) {
                                                        logger.Warn(exFile, $"{core.logCommonMessage},DownloadManager, error deleting cdn file [{taskToDelete.downloadPathFilename}]");
                                                    }
                                                }
                                                DbBaseModel.delete<TaskModel>(core.cpParent, deleteId);
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    //
                    // Build Tab0
                    //
                    string RQS = core.doc.refreshQueryString;
                    int PageSize = core.docProperties.getInteger(RequestNamePageSize);
                    if (PageSize == 0) {
                        PageSize = 50;
                    }
                    int PageNumber = core.docProperties.getInteger(RequestNamePageNumber);
                    if (PageNumber == 0) {
                        PageNumber = 1;
                    }
                    string AdminURL = "/" + core.appConfig.adminRoute;
                    int TopCount = PageNumber * PageSize;
                    //
                    const int ColumnCnt = 5;
                    //
                    // Setup Headings
                    //
                    string[] ColCaption = new string[ColumnCnt + 1];
                    string[] ColAlign = new string[ColumnCnt + 1];
                    string[] ColWidth = new string[ColumnCnt + 1];
                    string[,] Cells = new string[PageSize + 1, ColumnCnt + 1];
                    int ColumnPtr = 0;
                    //
                    ColCaption[ColumnPtr] = "&nbsp;";
                    ColAlign[ColumnPtr] = "center";
                    ColWidth[ColumnPtr] = "10px";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    ColCaption[ColumnPtr] = "Name";
                    ColAlign[ColumnPtr] = "left";
                    ColWidth[ColumnPtr] = "";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    ColCaption[ColumnPtr] = "For";
                    ColAlign[ColumnPtr] = "left";
                    ColWidth[ColumnPtr] = "200px";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    ColCaption[ColumnPtr] = "Requested";
                    ColAlign[ColumnPtr] = "left";
                    ColWidth[ColumnPtr] = "200px";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    ColCaption[ColumnPtr] = "Status";
                    ColAlign[ColumnPtr] = "Left";
                    ColWidth[ColumnPtr] = "100px";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    //   Get download tasks (tasks with downloadPathFilename set)
                    //
                    int DataRowCount = 0;
                    var taskList = DbBaseModel.createList<TaskModel>(core.cpParent, "(downloadPathFilename is not null)and(downloadPathFilename<>'')", "id desc", PageSize, PageNumber);
                    int RowPointer = 0;
                    if (taskList.Count == 0) {
                        Cells[0, 1] = "There are no download requests";
                        RowPointer = 1;
                    } else {
                        RowPointer = 0;
                        //
                        // -- count total download tasks for pagination
                        using (var dt = core.db.executeQuery("select count(*) as cnt from cctasks where (downloadPathFilename is not null)and(downloadPathFilename<>'')")) {
                            if (dt.Rows.Count > 0) {
                                DataRowCount = GenericController.getInteger(dt.Rows[0]["cnt"]);
                            }
                        }
                        string LinkPrefix = "<a href=\"" + core.appConfig.cdnFileUrl;
                        string LinkSuffix = "\" target=_blank>Download</a>";
                        foreach (var task in taskList) {
                            if (RowPointer >= PageSize) { break; }
                            var requestedBy = DbBaseModel.create<PersonModel>(core.cpParent, task.requestedBy);
                            Cells[RowPointer, 0] = HtmlController.checkbox("Row" + RowPointer) + HtmlController.inputHidden("RowID" + RowPointer, task.id);
                            Cells[RowPointer, 1] = task.name;
                            Cells[RowPointer, 2] = (requestedBy == null) ? "unknown" : requestedBy.name;
                            Cells[RowPointer, 3] = (task.dateAdded ?? DateTime.MinValue).ToString();
                            //
                            // -- status display based on datetime fields
                            if (task.dateCompleted == null && task.dateStarted == null) {
                                //
                                // -- queued
                                Cells[RowPointer, 4] = $"<div id=\"pending{RowPointer}\">Queued</div>";
                            } else if (task.dateCompleted == null && task.dateStarted != null) {
                                //
                                // -- running (pending with spinner)
                                Cells[RowPointer, 4] = $"\r\n<div data-id=\"{task.id}\" class=\"downloadPending\" id=\"pending{RowPointer}\">Pending <img src=\"https://s3.amazonaws.com/cdn.contensive.com/assets/20201227/images/ajax-loader-small.gif\" width=16 height=16></div>";
                            } else if (task.dateCompleted != null && string.IsNullOrEmpty(task.failMessage)) {
                                //
                                // -- completed successfully, show download link
                                Cells[RowPointer, 4] = $"<div id=\"pending{RowPointer}\">{LinkPrefix}{task.downloadPathFilename}{LinkSuffix}</div>";
                            } else {
                                //
                                // -- completed with error
                                Cells[RowPointer, 4] = $"<div id=\"pending{RowPointer}\">Error: {HtmlController.encodeHtml(task.failMessage)}</div>";
                            }
                            RowPointer++;
                        }
                    }
                    StringBuilderLegacyController Tab0 = new StringBuilderLegacyController();
                    Tab0.add(HtmlController.inputHidden("RowCnt", RowPointer));
                    string PreTableCopy = "";
                    string PostTableCopy = "";
                    string Cell = AdminUIController.getReport(core, RowPointer, ColCaption, ColAlign, ColWidth, Cells, PageSize, PageNumber, PreTableCopy, PostTableCopy, DataRowCount, "ccPanel");
                    Tab0.add(Cell);
                    Content = Tab0.text;
                    //
                    layout.addFormButton(ButtonCancel);
                    layout.addFormButton(ButtonRefresh);
                    layout.addFormButton(ButtonDelete);
                }
                //
                layout.title = "Download Manager";
                layout.description = "<p>The Download Manager lists downloads requested from anywhere on the website. To add a new download of any content in Contensive, click Export on the filter tab of the content listing page. To add a new download from a SQL statement, use Custom Reports under Reports on the Navigator.</p>";
                layout.body = Content;
                //
                //
                result = layout.getHtml();
                result += $"<script>{core.cpParent.PrivateFiles.Read("downloadmanager/client.js")}</script>";
                //
                core.html.addTitle("Download Manager", "Download Manager");
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
    }
}
