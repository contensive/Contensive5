
using System;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite {
    public class ToolDownloads {
        //
        //========================================================================
        //
        //========================================================================
        //
        public static string get(CoreController core) {
            string tempGetForm_Downloads = null;
            try {
                //
                string Button = core.docProperties.getText(RequestNameButton);
                if (Button == ButtonCancel) {
                    return core.webServer.redirect("/" + core.appConfig.adminRoute, "Downloads, Cancel Button Pressed");
                }
                string ButtonListLeft = "";
                string ButtonListRight = "";
                string Content = "";
                //
                if (!core.session.isAuthenticatedAdmin()) {
                    //
                    // Must be a developer
                    //
                    ButtonListLeft = ButtonCancel;
                    ButtonListRight = "";
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
                                            DownloadModel.delete<DownloadModel>(core.cpParent, core.docProperties.getInteger("RowID" + RowPtr));
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
                    ColCaption[ColumnPtr] = "File";
                    ColAlign[ColumnPtr] = "Left";
                    ColWidth[ColumnPtr] = "100px";
                    ColumnPtr = ColumnPtr + 1;
                    //
                    //   Get Downloads available
                    //
                    int DataRowCount = 0;
                    var downloadList = DbBaseModel.createList<DownloadModel>(core.cpParent, "", "id desc", PageSize, PageNumber);
                    int RowPointer = 0;
                    if (downloadList.Count == 0) {
                        Cells[0, 1] = "There are no download requests";
                        RowPointer = 1;
                    } else {
                        RowPointer = 0;
                        DataRowCount = DbBaseModel.getCount<DownloadModel>(core.cpParent);
                        string LinkPrefix = "<a href=\"" + core.appConfig.cdnFileUrl;
                        string LinkSuffix = "\" target=_blank>Download</a>";
                        foreach (var download in downloadList) {
                            if (RowPointer >= PageSize) break;
                            var requestedBy = DbBaseModel.create<PersonModel>(core.cpParent, download.requestedBy);
                            Cells[RowPointer, 0] = HtmlController.checkbox("Row" + RowPointer) + HtmlController.inputHidden("RowID" + RowPointer, download.id);
                            Cells[RowPointer, 1] = download.name;
                            Cells[RowPointer, 2] = (requestedBy == null) ? "unknown" : requestedBy.name;
                            Cells[RowPointer, 3] = download.dateRequested.ToString();
                            if ( string.IsNullOrEmpty(download.resultMessage)) {
                                Cells[RowPointer, 4] = "\r\n<div id=\"pending" + RowPointer + "\">Pending <img src=\"https://s3.amazonaws.com/cdn.contensive.com/assets/20201227/images/ajax-loader-small.gif\" width=16 height=16></div>";
                            } else if (!string.IsNullOrEmpty(download.filename.filename)) {
                                Cells[RowPointer, 4] = "<div id=\"pending" + RowPointer + "\">" + LinkPrefix + download.filename.filename + LinkSuffix + "</div>";
                            } else {
                                Cells[RowPointer, 4] = "<div id=\"pending" + RowPointer + "\">error</div>";
                            }
                            RowPointer = RowPointer + 1;
                        }
                    }
                    StringBuilderLegacyController Tab0 = new StringBuilderLegacyController();
                    Tab0.add(HtmlController.inputHidden("RowCnt", RowPointer));
                    string PreTableCopy = "";
                    string PostTableCopy = "";
                    string Cell = AdminUIController.getReport(core, RowPointer, ColCaption, ColAlign, ColWidth, Cells, PageSize, PageNumber, PreTableCopy, PostTableCopy, DataRowCount, "ccPanel");
                    Tab0.add(Cell);
                    Content = Tab0.text;
                    ButtonListLeft = ButtonCancel + "," + ButtonRefresh + "," + ButtonDelete;
                    ButtonListRight = "";
                    Content = Content + HtmlController.inputHidden(rnAdminSourceForm, AdminFormDownloads);
                }
                //
                string Caption = "Download Manager";
                string Description = ""
    + "<p>The Download Manager holds all downloads requested from anywhere on the website. It also provides tools to request downloads from any Content.</p>"
    + "<p>To add a new download of any content in Contensive, click Export on the filter tab of the content listing page. To add a new download from a SQL statement, use Custom Reports under Reports on the Navigator.</p>";
                int ContentPadding = 0;
                string ContentSummary = "";
                tempGetForm_Downloads = AdminUIController.getToolBody(core, Caption, ButtonListLeft, ButtonListRight, true, true, Description, ContentSummary, ContentPadding, Content);
                //
                core.html.addTitle(Caption);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempGetForm_Downloads;
        }
    }
}
