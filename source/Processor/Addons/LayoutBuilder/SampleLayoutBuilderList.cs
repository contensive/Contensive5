using Contensive.BaseClasses;
using System;
using System.Data;
using System.Reflection;
using System.Text;
//
namespace Contensive.Processor.Addons.LayoutBuilder {
    /// <summary>
    /// Sample Layout Builder
    /// </summary>
    public class SampleLayoutBuilderList : AddonBaseClass {
        //
        private const string buttonCancel = "Cancel";
        private const string buttonSuccess = "Create Success";
        private const string buttonFail = "Create Fail";
        private const string buttonWarn = "Create Warning";
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                // 
                // -- authenticate/authorize
                if (!cp.User.IsAdmin) { return "You do not have permission."; }
                //
                // -- create the layout
                var layoutBuilder = cp.AdminUI.CreateLayoutBuilderList();
                layoutBuilder.title = "Layout Builder List Sample";
                layoutBuilder.description = "Description added before body created.";
                layoutBuilder.callbackAddonGuid = "{7E5A82B3-AE24-44E4-B9F3-3459FAFC8679}";
                layoutBuilder.portalSubNavTitle = "";
                layoutBuilder.csvDownloadFilename = "CSVDownloadFile.csv";
                layoutBuilder.addCsvDownloadCurrentPage = true;
                layoutBuilder.includeBodyColor = true;
                layoutBuilder.includeBodyPadding = true;
                layoutBuilder.includeForm = true;
                layoutBuilder.isOuterContainer = true;
                layoutBuilder.htmlAfterBody = "";
                layoutBuilder.htmlBeforeBody = "";
                layoutBuilder.htmlLeftOfBody = "";
                layoutBuilder.paginationPageSizeDefault = 50;
                layoutBuilder.recordCount = 0;
                layoutBuilder.infoMessage = "";
                layoutBuilder.successMessage = "";
                layoutBuilder.warningMessage = "";
                layoutBuilder.failMessage = "";
                ////
                //// -- validate arguments
                //int personId = cp.Doc.GetInteger("personId");
                //var people = Contensive.Models.Db.DbBaseModel.create<Contensive.Models.Db.PersonModel>(cp, personId);
                //if (people is null) {
                //    // 
                //    // -- bad person
                //    layoutBuilder.warningMessage = "This person is not valid";
                //    return layoutBuilder.getHtml();
                //}
                //
                // -- process request
                string button = cp.Doc.GetText("button");
                string sampleText = cp.Doc.GetText("sampleText");
                if (button == buttonCancel) {
                    //
                    // -- cancel button, return empty. Empty is detected and the caller should respond accordingly
                    return "";
                }
                if (button == buttonSuccess) {
                    //
                    // -- Success Example
                    layoutBuilder.successMessage = "This message is displayed on success.";
                    //
                    // -- set the csv download filename creates a link at the top of the form for a download
                    // -- ?? how does this get populated
                    layoutBuilder.csvDownloadFilename = "SampleLayoutBuilder.csv";
                    cp.CdnFiles.Save(layoutBuilder.csvDownloadFilename, "a,b,c,d,e\n1,2,3,4,5");
                } else if (button == buttonWarn) {
                    //
                    // -- Warn example
                    layoutBuilder.warningMessage = "This is a warning message.";
                } else if (button == buttonFail) {
                    //
                    // -- Fail example
                    layoutBuilder.failMessage = "This message is displayed on error.";
                }
                //
                // -- setup filters
                bool filterAdminOnly = cp.Doc.GetBoolean("filterAdminOnly");
                // 
                // -- setup the rqs so portal links in account section are correct, and form comes back here
                cp.Doc.AddRefreshQueryString("someValueId", 10);
                // 
                layoutBuilder.columnCaption = "<input type=\"CheckBox\" name=\"abNotesRowHead\" value=\"0\" id=\"abSelectNotesAllNone\">";
                layoutBuilder.columnCaptionClass = "afwWidth20px";
                layoutBuilder.columnCellClass = "afwWidth20px";
                // 
                layoutBuilder.addColumn();
                layoutBuilder.columnCaption = "Date Added";
                layoutBuilder.columnCaptionClass = "afwWidth200px";
                layoutBuilder.columnCellClass = "afwWidth200px";
                // 
                layoutBuilder.addColumn();
                layoutBuilder.columnCaption = "ID";
                layoutBuilder.columnCaptionClass = "afwWidth200px";
                layoutBuilder.columnCellClass = "afwWidth200px";
                // 
                layoutBuilder.addColumn();
                layoutBuilder.columnCaption = "Name";
                layoutBuilder.columnCaptionClass = "";
                layoutBuilder.columnCellClass = "";
                //
                // -- sql where clause
                string sqlWhere = $"(1=1)";
                sqlWhere += filterAdminOnly ? $" and(admin>0)" : "";
                sqlWhere += string.IsNullOrEmpty(layoutBuilder.sqlSearchTerm) ? "" : $" and(name like {cp.Db.EncodeSQLTextLike(layoutBuilder.sqlSearchTerm)})";
                //
                // -- determine total records in data (not just the page)
                string sqlCount = $"select count(*) from ccMembers where {sqlWhere}";
                using (DataTable dt = cp.Db.ExecuteQuery(sqlCount)) {
                    if (dt?.Rows != null && dt.Rows.Count == 1) {
                        layoutBuilder.recordCount = cp.Utils.EncodeInteger(dt.Rows[0][0]);
                    }
                }
                //
                // -- sql for the page of data
                string sql = $"select id,name from ccmembers where {sqlWhere}";
                //
                // -- sort
                sql += string.IsNullOrEmpty(layoutBuilder.sqlOrderBy) ? " order by name" : $" order by {layoutBuilder.sqlOrderBy}";
                //
                // -- Limit
                sql += $" OFFSET {(layoutBuilder.paginationPageNumber - 1) * layoutBuilder.paginationPageSize} ROWS FETCH NEXT {layoutBuilder.paginationPageSize} ROWS ONLY";
                //
                int rowPtr = 0;
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            layoutBuilder.addRow();
                            if (filterAdminOnly && cp.Utils.EncodeBoolean(row["alert"])) { layoutBuilder.addRowClass("abNoteAlertCustom"); }
                            // 
                            layoutBuilder.setCell(cp.Html.CheckBox("abNotesRow" + rowPtr).Replace(">", " class=\"abSelectNoteCheckbox\">") + cp.Html5.Hidden("abNotesRowId" + rowPtr, cp.Utils.EncodeInteger(row["id"]).ToString()));
                            // 
                            layoutBuilder.setCell(cp.Utils.EncodeInteger(row["id"]));
                            // 
                            layoutBuilder.setCell(cp.Utils.EncodeText(row["name"]));
                            //
                            rowPtr += 1;
                        }
                    }
                }
                //
                // -- setup form last to include values created during rendering
                layoutBuilder.description = "Description text added after body created.";
                //
                // -- if this layout appears in a portal and as a subsection of a portal feature (like account), set the portalSubNavTitle
                layoutBuilder.portalSubNavTitle = "Subnav Title for layoutBuilderList";
                //
                layoutBuilder.htmlAfterBody = $"" +
                    $"<style type=\"text/css\">" +
                        $".abNoteAlertCustom {{ background-color: {cp.Site.GetText("abActivatedAlertsBackgroundColor", "#ffff00")} !important;" +
                        $"color: {cp.Site.GetText("abActivatedAlertsFontColor", "#000000")} !important; }}" +
                    $"</style>";
                //
                // -- add submit buttons
                layoutBuilder.addFormButton(buttonCancel);
                layoutBuilder.addFormButton(buttonSuccess);
                layoutBuilder.addFormButton(buttonWarn);
                layoutBuilder.addFormButton(buttonFail);
                //
                // -- add link buttons
                layoutBuilder.addLinkButton("Link Button", "https://www.google.com");
                //
                // -- add hiddens, like subformId, or accountId, etc
                layoutBuilder.addFormHidden("rowCountIfNeeded", rowPtr);
                //
                // -- return the form
                return layoutBuilder.getHtml();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}

