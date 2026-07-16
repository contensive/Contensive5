
using Contensive.BaseClasses;
using System;
using System.Data;

namespace Contensive.Processor.Addons.Email {
    /// <summary>
    /// Email Block List tool for the Communicate portal.
    /// Shows all users whose email is blocked (bounce list or opt-out) with the ability to unblock.
    /// </summary>
    public class EmailBlockListAddon : AddonBaseClass {
        //
        public const string guidAddon = "{B198244B-7B4B-45F3-A52D-34EC2B807E58}";
        public const string guidPortalFeature = "{11074243-260D-403E-9765-8AAC22E01B5D}";
        private const string guidCommunicatePortal = "{e4d011e9-9f3b-4f7e-8ec3-f4fcc2a20455}";
        private const string guidEmailParentFeature = "{fef4be31-16af-48a7-89ae-2b8c45b9788a}";
        private const string buttonRemoveBlock = "Remove Block";
        private const string rnButton = "button";
        //
        // ====================================================================================================
        //
        public override object Execute(CPBaseClass cp) {
            try {
                if (!cp.User.IsAdmin) { return "You do not have permission to access this tool."; }
                processForm(cp);
                return getForm(cp);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Process form submissions (Remove Block button)
        /// </summary>
        private static void processForm(CPBaseClass cp) {
            try {
                if (!cp.Doc.IsProperty(rnButton)) { return; }
                string button = cp.Doc.GetText(rnButton);
                if ((button ?? "") != buttonRemoveBlock) { return; }
                //
                int rowCnt = cp.Doc.GetInteger("rowCnt");
                if (rowCnt <= 0) { return; }
                int unblockedCount = 0;
                for (int ptr = 0; ptr < rowCnt; ptr++) {
                    if (!cp.Doc.GetBoolean($"row{ptr}")) { continue; }
                    string source = cp.Doc.GetText($"rowSource{ptr}");
                    int rowId = cp.Doc.GetInteger($"rowId{ptr}");
                    string rowEmail = cp.Doc.GetText($"rowEmail{ptr}");
                    if (string.IsNullOrEmpty(source)) { continue; }
                    //
                    if (source == "bounce") {
                        //
                        // -- delete the bounce list record
                        if (rowId > 0) {
                            cp.Db.ExecuteNonQuery($"delete from EmailBounceList where id={rowId}");
                        }
                        //
                        // -- re-enable allowBulkEmail on person by email
                        if (!string.IsNullOrEmpty(rowEmail)) {
                            cp.Db.ExecuteNonQuery($"update ccMembers set allowBulkEmail=1 where email={cp.Db.EncodeSQLText(rowEmail)}");
                        }
                        unblockedCount++;
                    } else if (source == "optout") {
                        //
                        // -- re-enable allowBulkEmail on person by ID
                        if (rowId > 0) {
                            cp.Db.ExecuteNonQuery($"update ccMembers set allowBulkEmail=1 where id={rowId}");
                        }
                        unblockedCount++;
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Build the LayoutBuilder list
        /// </summary>
        private static string getForm(CPBaseClass cp) {
            try {
                if (!cp.Response.isOpen) { return ""; }
                //
                var layoutBuilder = cp.AdminUI.CreateLayoutBuilderList();
                //
                // -- define columns
                //
                // Row number
                layoutBuilder.columnCaption = "Row";
                layoutBuilder.columnCaptionClass = "afwWidth20px afwTextAlignCenter";
                layoutBuilder.columnCellClass = "afwTextAlignCenter";
                layoutBuilder.addColumn();
                //
                // Checkbox (select all in header)
                layoutBuilder.columnCaption = "<input type=\"checkbox\" id=\"ebSelectAllNone\">";
                layoutBuilder.columnCaptionClass = "afwWidth20px";
                layoutBuilder.columnCellClass = "afwTextAlignCenter";
                layoutBuilder.addColumn();
                //
                // Name
                layoutBuilder.columnCaption = "Name";
                layoutBuilder.columnCaptionClass = "afwTextAlignLeft";
                layoutBuilder.columnCellClass = "afwTextAlignLeft";
                layoutBuilder.columnSortable = false;
                layoutBuilder.addColumn();
                //
                // Email
                layoutBuilder.columnCaption = "Email";
                layoutBuilder.columnCaptionClass = "afwTextAlignLeft";
                layoutBuilder.columnCellClass = "afwTextAlignLeft";
                layoutBuilder.columnSortable = false;
                layoutBuilder.addColumn();
                //
                // Block Reason
                layoutBuilder.columnCaption = "Block Reason";
                layoutBuilder.columnCaptionClass = "afwTextAlignLeft";
                layoutBuilder.columnCellClass = "afwTextAlignLeft";
                layoutBuilder.columnSortable = false;
                layoutBuilder.addColumn();
                //
                // Date
                layoutBuilder.columnCaption = "Date";
                layoutBuilder.columnCaptionClass = "afwTextAlignLeft";
                layoutBuilder.columnCellClass = "afwTextAlignLeft";
                layoutBuilder.columnSortable = false;
                layoutBuilder.addColumn();
                //
                // -- build the base UNION query
                string sqlUnion = ""
                    + "select"
                    + " p.id as personId,"
                    + " p.name as personName,"
                    + " ISNULL(b.email, p.email) as email,"
                    + " 'Email bounce: ' + ISNULL(b.details,'(no details)') as blockReason,"
                    + " b.id as bounceId,"
                    + " b.dateAdded as blockDate,"
                    + " 'bounce' as blockSource"
                    + " from EmailBounceList b"
                    + " left join ccMembers p on p.email = b.email"
                    + " where (1=1)"
                    + " union all"
                    + " select"
                    + " p.id as personId,"
                    + " p.name as personName,"
                    + " p.email,"
                    + " 'User opted out of bulk email' as blockReason,"
                    + " 0 as bounceId,"
                    + " p.modifiedDate as blockDate,"
                    + " 'optout' as blockSource"
                    + " from ccMembers p"
                    + " where p.active <> 0"
                    + " and (p.allowBulkEmail = 0 or p.allowBulkEmail is null)"
                    + " and p.email is not null"
                    + " and p.email <> ''"
                    + " and p.email not in (select email from EmailBounceList where email is not null)";
                //
                // -- search filter
                string sqlWhere = "(1=1)";
                if (!string.IsNullOrEmpty(layoutBuilder.sqlSearchTerm)) {
                    string likeTerm = cp.Db.EncodeSQLTextLike(layoutBuilder.sqlSearchTerm);
                    sqlWhere += $" and (blocked.email like {likeTerm} or blocked.personName like {likeTerm})";
                }
                //
                // -- count query
                string sqlCount = $"select count(*) from ({sqlUnion}) as blocked where {sqlWhere}";
                using (DataTable dt = cp.Db.ExecuteQuery(sqlCount)) {
                    if (dt?.Rows != null && dt.Rows.Count == 1) {
                        layoutBuilder.recordCount = cp.Utils.EncodeInteger(dt.Rows[0][0]);
                    }
                }
                //
                // -- data query with pagination
                string sql = $"select * from ({sqlUnion}) as blocked where {sqlWhere}";
                sql += " order by blocked.email";
                sql += $" OFFSET {(layoutBuilder.paginationPageNumber - 1) * layoutBuilder.paginationPageSize} ROWS FETCH NEXT {layoutBuilder.paginationPageSize} ROWS ONLY";
                //
                // -- populate rows
                int rowPtr = 0;
                using (var csList = cp.CSNew()) {
                    if (csList.OpenSQL(sql)) {
                        int rowPtrStart = layoutBuilder.paginationPageSize * (layoutBuilder.paginationPageNumber - 1);
                        do {
                            int personId = csList.GetInteger("personId");
                            string personName = csList.GetText("personName");
                            string email = csList.GetText("email");
                            string blockReason = csList.GetText("blockReason");
                            DateTime blockDate = csList.GetDate("blockDate");
                            int bounceId = csList.GetInteger("bounceId");
                            string blockSource = csList.GetText("blockSource");
                            //
                            if (string.IsNullOrWhiteSpace(personName)) {
                                personName = "(no name)";
                            }
                            //
                            // -- checkbox with hidden fields
                            string rowSelect = cp.Html.CheckBox($"row{rowPtr}", false, "ebSelectCheckbox");
                            rowSelect += cp.Html5.Hidden($"rowId{rowPtr}", (blockSource == "bounce" ? bounceId : personId).ToString());
                            rowSelect += cp.Html5.Hidden($"rowSource{rowPtr}", blockSource);
                            rowSelect += cp.Html5.Hidden($"rowEmail{rowPtr}", email);
                            //
                            layoutBuilder.addRow();
                            layoutBuilder.setCell((rowPtrStart + rowPtr + 1).ToString());
                            layoutBuilder.setCell(rowSelect);
                            layoutBuilder.setCell(personName);
                            layoutBuilder.setCell(email);
                            layoutBuilder.setCell(blockReason);
                            layoutBuilder.setCell(blockDate > new DateTime(1900, 1, 1) ? blockDate.ToShortDateString() : "");
                            //
                            rowPtr++;
                            csList.GoNext();
                        } while (csList.OK());
                        csList.Close();
                    }
                }
                //
                // -- layout configuration
                layoutBuilder.title = "Email Block List";
                layoutBuilder.description = "This list shows all users whose email is currently blocked. There are two ways email can be blocked:"
                    + " (1) When a user clicks the unsubscribe link in an email, it unchecks the 'Allow Group Email' field on their people record. An admin can manually re-enable this."
                    + " (2) When an email is returned undelivered because the address is bad or blocked by the recipient's email provider, it is added to the Email Bounce List."
                    + " Bounces can be temporary (e.g., inbox full) or permanent (e.g., address does not exist)."
                    + " Select users and click 'Remove Block' to unblock them.";
                layoutBuilder.callbackAddonGuid = guidAddon;
                layoutBuilder.includeBodyColor = true;
                layoutBuilder.includeBodyPadding = true;
                layoutBuilder.includeForm = true;
                layoutBuilder.isOuterContainer = false;
                layoutBuilder.paginationPageSizeDefault = 50;
                layoutBuilder.allowDownloadButton = true;
                //
                // -- buttons
                layoutBuilder.addFormButton(buttonRemoveBlock, rnButton, "ebRemoveBlockButton");
                //
                // -- hidden fields
                layoutBuilder.addFormHidden("rowCnt", rowPtr);
                //
                // -- select all/none JavaScript
                cp.Doc.AddHeadJavascript(""
                    + "document.addEventListener('DOMContentLoaded',function(){"
                    + "document.body.addEventListener('click',function(e){"
                    + "if(e.target&&e.target.id==='ebSelectAllNone'){"
                    + "var cbs=document.querySelectorAll('.ebSelectCheckbox');"
                    + "for(var i=0;i<cbs.length;i++){cbs[i].checked=e.target.checked;}"
                    + "}"
                    + "});"
                    + "});");
                //
                return layoutBuilder.getHtml();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
