
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Processor.Exceptions;
using System.Collections.Generic;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor.Addons.AdminSite {
    public class ToolCustomReports {
        //
        //========================================================================
        //
        public static string get(CoreController core) {
            try {
                //
                string Button = null;
                string SQL = null;
                string RQS = null;
                int PageSize = 0;
                int PageNumber = 0;
                int TopCount = 0;
                int RowPointer = 0;
                int DataRowCount = 0;
                string PreTableCopy = "";
                string PostTableCopy = "";
                int ColumnPtr = 0;
                string[] ColCaption = null;
                string[] ColAlign = null;
                string[] ColWidth = null;
                string[,] Cells = null;
                string AdminURL = null;
                int RowCnt = 0;
                int RowPtr = 0;
                int ContentId = 0;
                string Format = null;
                string Name = null;
                string title = null;
                string Description = null;
                string ButtonCommaListLeft = null;
                string ButtonCommaListRight = null;
                int ContentPadding = 0;
                string ContentSummary = "";
                StringBuilderLegacyController Tab0 = new StringBuilderLegacyController();
                StringBuilderLegacyController Tab1 = new StringBuilderLegacyController();
                string Content = "";
                string SQLFieldName = null;
                var adminMenu = new EditTabModel();
                //
                const int ColumnCnt = 4;
                //
                Button = core.docProperties.getText(RequestNameButton);
                ContentId = core.docProperties.getInteger("ContentID");
                Format = core.docProperties.getText("Format");
                //
                title = "Custom Report Manager";
                Description = "Custom Reports are a way for you to create a snapshot of data to view or download. To request a report, select the Custom Reports tab, check the report(s) you want, and click the [Request Download] Button. When your report is ready, it will be available in the <a href=\"?" + rnAdminForm + "=30\">Download Manager</a>. To create a new custom report, select the Request New Report tab, enter a name and SQL statement, and click the Apply button.";
                ContentPadding = 0;
                ButtonCommaListLeft = ButtonCancel + "," + ButtonDelete + "," + ButtonRequestDownload;
                ButtonCommaListRight = "";
                SQLFieldName = "SQLQuery";
                //
                if (!core.session.isAuthenticatedAdmin()) {
                    //
                    // Must be a developer
                    //
                    Description = Description + "You can not access the Custom Report Manager because your account is not configured as an administrator.";
                } else {
                    //
                    // Process Requests
                    //
                    if (!string.IsNullOrEmpty(Button)) {
                        switch (Button) {
                            case ButtonCancel:
                                return core.webServer.redirect("/" + core.appConfig.adminRoute, "CustomReports, Cancel Button Pressed");
                            case ButtonDelete:
                                RowCnt = core.docProperties.getInteger("RowCnt");
                                if (RowCnt > 0) {
                                    for (RowPtr = 0; RowPtr < RowCnt; RowPtr++) {
                                        if (core.docProperties.getBoolean("Row" + RowPtr)) {
                                            MetadataController.deleteContentRecord(core, "Custom Reports", core.docProperties.getInteger("RowID" + RowPtr));
                                        }
                                    }
                                }
                                break;
                            case ButtonRequestDownload:
                            case ButtonApply:
                                //
                                Name = core.docProperties.getText("name");
                                SQL = core.docProperties.getText(SQLFieldName);
                                if (!string.IsNullOrEmpty(Name) || !string.IsNullOrEmpty(SQL)) {
                                    if ((string.IsNullOrEmpty(Name)) || (string.IsNullOrEmpty(SQL))) {
                                        Processor.Controllers.ErrorController.addUserError(core, "A name and SQL Query are required to save a new custom report.");
                                    } else {
                                        int customReportId = 0;
                                        using (var csData = new CsModel(core)) {
                                            csData.insert("Custom Reports");
                                            if (csData.ok()) {
                                                customReportId = csData.getInteger("id");
                                                csData.set("Name", Name);
                                                csData.set(SQLFieldName, SQL);
                                            }
                                            csData.close();
                                        }
                                        requestDownload(core, customReportId);
                                    }
                                }
                                //
                                RowCnt = core.docProperties.getInteger("RowCnt");
                                if (RowCnt > 0) {
                                    for (RowPtr = 0; RowPtr < RowCnt; RowPtr++) {
                                        if (core.docProperties.getBoolean("Row" + RowPtr)) {
                                            int customReportId =core.docProperties.getInteger("RowID" + RowPtr);
                                            using (var csData = new CsModel(core)) {
                                                csData.openRecord("Custom Reports", customReportId);
                                                if (csData.ok()) {
                                                    SQL = csData.getText(SQLFieldName);
                                                    Name = csData.getText("Name");
                                                }
                                            }
                                            requestDownload(core, customReportId);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    //
                    // Build Tab0
                    //
                    Tab0.add("<p>The following is a list of available custom reports.</p>");
                    //
                    RQS = core.doc.refreshQueryString;
                    PageSize = core.docProperties.getInteger(RequestNamePageSize);
                    if (PageSize == 0) {
                        PageSize = 50;
                    }
                    PageNumber = core.docProperties.getInteger(RequestNamePageNumber);
                    if (PageNumber == 0) {
                        PageNumber = 1;
                    }
                    AdminURL = "/" + core.appConfig.adminRoute;
                    TopCount = PageNumber * PageSize;
                    //
                    // Setup Headings
                    //
                    ColCaption = new string[ColumnCnt + 1];
                    ColAlign = new string[ColumnCnt + 1];
                    ColWidth = new string[ColumnCnt + 1];
                    Cells = new string[PageSize + 1, ColumnCnt + 1];
                    //
                    ColCaption[ColumnPtr] = "Select<br><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=10 height=1>";
                    ColAlign[ColumnPtr] = "center";
                    ColWidth[ColumnPtr] = "10";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    ColCaption[ColumnPtr] = "Name";
                    ColAlign[ColumnPtr] = "left";
                    ColWidth[ColumnPtr] = "100%";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    ColCaption[ColumnPtr] = "Created By<br><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=100 height=1>";
                    ColAlign[ColumnPtr] = "left";
                    ColWidth[ColumnPtr] = "100";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    ColCaption[ColumnPtr] = "Date Created<br><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=150 height=1>";
                    ColAlign[ColumnPtr] = "left";
                    ColWidth[ColumnPtr] = "150";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    //   Get Data
                    //
                    using (var csData = new CsModel(core)) {
                        RowPointer = 0;
                        if (!csData.open("Custom Reports")) {
                            Cells[0, 1] = "There are no custom reports defined";
                            RowPointer = 1;
                        } else {
                            DataRowCount = csData.getRowCount();
                            while (csData.ok() && (RowPointer < PageSize)) {
                                int customReportId = csData.getInteger("ID");
                                Cells[RowPointer, 0] = HtmlController.checkbox("Row" + RowPointer) + HtmlController.inputHidden("RowID" + RowPointer, customReportId);
                                Cells[RowPointer, 1] = csData.getText("name");
                                Cells[RowPointer, 2] = csData.getText("CreatedBy");
                                Cells[RowPointer, 3] = csData.getDate("DateAdded").ToShortDateString();
                                RowPointer = RowPointer + 1;
                                csData.goNext();
                            }
                        }
                        csData.close();
                    }
                    string Cell = null;
                    Tab0.add(HtmlController.inputHidden("RowCnt", RowPointer));
                    Cell = AdminUIController.getReport(core, RowPointer, ColCaption, ColAlign, ColWidth, Cells, PageSize, PageNumber, PreTableCopy, PostTableCopy, DataRowCount, "ccPanel");
                    Tab0.add("<div>" + Cell + "</div>");
                    //
                    // Build RequestContent Form
                    //
                    Tab1.add("<p>Use this form to create a new custom report. Enter the SQL Query for the report, and a name that will be used as a caption.</p>");
                    //
                    Tab1.add("<table border=\"0\" cellpadding=\"3\" cellspacing=\"0\" width=\"100%\">");
                    //
                    Tab1.add("<tr>");
                    Tab1.add("<td align=right>Name</td>");
                    Tab1.add("<td>" + HtmlController.inputText_Legacy(core, "Name", "", 1, 40) + "</td>");
                    Tab1.add("</tr>");
                    //
                    Tab1.add("<tr>");
                    Tab1.add("<td align=right>SQL Query</td>");
                    Tab1.add("<td>" + HtmlController.inputText_Legacy(core, SQLFieldName, "", 8, 40) + "</td>");
                    Tab1.add("</tr>");
                    //
                    Tab1.add("<tr><td width=\"120\"><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"120\" height=\"1\"></td><td width=\"100%\">&nbsp;</td></tr></table>");
                    //
                    // Build and add tabs
                    //
                    adminMenu.addEntry("Custom&nbsp;Reports", Tab0.text, "ccAdminTab");
                    adminMenu.addEntry("Request&nbsp;New&nbsp;Report", Tab1.text, "ccAdminTab");
                    Content = adminMenu.getTabs(core);
                    //
                }
                //
                core.html.addTitle("Custom Reports", "Custom Reports");
                //
                return AdminUIController.getToolBody(core, title, ButtonCommaListLeft, ButtonCommaListRight, true, true, Description, ContentSummary, ContentPadding, Content);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return toolExceptionMessage;
            }
        }
        //
        //========================================================================
        //
        public static void requestDownload( CoreController core, int customReportId ) {
            //
            // Request the download
            //
            var customReport = DbBaseModel.create<CustomReportModel>(core.cpParent, customReportId);
            if ( customReport != null) {
                var ExportCSVAddon = DbBaseModel.create<AddonModel>(core.cpParent, addonGuidExportCSV);
                if (ExportCSVAddon == null) {
                    LogController.logError(core, new GenericException("ExportCSV addon not found. Task could not be added to task queue."));
                } else {
                    var docProperties = new Dictionary<string, string> {
                                                { "sql", customReport.sqlQuery },
                                                { "datasource", "default" }
                                            };
                    var cmdDetail = new TaskModel.CmdDetailClass {
                        addonId = ExportCSVAddon.id,
                        addonName = ExportCSVAddon.name,
                        args = docProperties
                    };
                    string ExportFilename = "CustomReport_" + customReport.id.ToString("00000000") + ".csv";
                    string reportName = customReport.name ?? "#" + customReport.id;
                    TaskSchedulerController.addTaskToQueue(core, cmdDetail, false, "Custom Report, " + reportName, ExportFilename);
                }
            }
        }


    }
}
