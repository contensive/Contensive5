using Contensive.BaseClasses;
using System;
using System.Data;
using System.Reflection;
using System.Text;
using static Contensive.BaseClasses.LayoutBuilder.LayoutBuilderBaseClass;
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
                layoutBuilder.allowDownloadButton = true;
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
                layoutBuilder.allowDownloadButton = true;
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
                layoutBuilder.columnCaptionClass = $"{AfwStyles.afwWidth20px} {AfwStyles.afwTextAlignCenter}";
                layoutBuilder.columnCellClass = $"{AfwStyles.afwWidth20px} {AfwStyles.afwTextAlignCenter}";
                layoutBuilder.columnDownloadable = false;
                layoutBuilder.columnVisible = true;
                layoutBuilder.columnSortable = false;
                // 
                layoutBuilder.addColumn();
                layoutBuilder.columnCaption = "ID";
                layoutBuilder.columnCaptionClass = $"{AfwStyles.afwWidth100px} {AfwStyles.afwTextAlignCenter}";
                layoutBuilder.columnCellClass = $"{AfwStyles.afwWidth100px} {AfwStyles.afwTextAlignCenter}";
                layoutBuilder.columnDownloadable = true;
                layoutBuilder.columnVisible = true;
                layoutBuilder.columnSortable = true;
                // 
                layoutBuilder.addColumn();
                layoutBuilder.columnCaption = "Date Added";
                layoutBuilder.columnCaptionClass = $"{AfwStyles.afwWidth200px} {AfwStyles.afwTextAlignCenter}";
                layoutBuilder.columnCellClass = $"{AfwStyles.afwWidth200px} {AfwStyles.afwTextAlignLeft}";
                layoutBuilder.columnDownloadable = true;
                layoutBuilder.columnVisible = true;
                layoutBuilder.columnSortable = true;
                // 
                layoutBuilder.addColumn();
                layoutBuilder.columnCaption = "Name";
                layoutBuilder.columnCaptionClass = $"{AfwStyles.afwTextAlignLeft}";
                layoutBuilder.columnCellClass = $"{AfwStyles.afwTextAlignLeft}";
                layoutBuilder.columnDownloadable = true;
                layoutBuilder.columnVisible = true;
                layoutBuilder.columnSortable = true;
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
                string sql = $"select id,dateAdded,name from ccmembers where {sqlWhere}";
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
                            layoutBuilder.setCell(cp.Utils.EncodeDate(row["dateAdded"]));
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
                // -- if this layout appears in a portal and as a subsection of a portal feature (like account), set the portalSubNavTitleList
                layoutBuilder.portalSubNavTitleList.Add("Subnav Title for layoutBuilderList");
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

