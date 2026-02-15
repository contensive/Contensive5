using Contensive.AccountBilling.Controllers;
using Contensive.AccountBilling.Models.Domain;
using Contensive.AccountBilling.Views;
using Contensive.BaseClasses;
using Microsoft.VisualBasic;
using My.Resources;
using System;
using System.Data;
using System.Reflection;
using Views.AccountManager;

namespace Contensive.AccountBilling {
    // ====================================================================================================
    /// <summary>
    /// List of Orders
    /// </summary>
    /// <remarks></remarks>
    public class AccountManagerUsersAddon : AddonBaseClass {
        //
        public const string guidPortalFeature = "{EFFBFF82-B965-4078-AFF9-2F78F563E45B}";
        public const string guidAddon = "";
        public const string viewName = "UserList";
        //
        // ====================================================================================================
        //
        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- authenticate/authorize
                if (!cp.User.IsAdmin)
                    return SecurityController.getNotAuthorizedHtmlResponse(cp);
                //
                // -- validate portal environment
                if (!cp.AdminUI.EndpointContainsPortal())
                    return PortalController.RedirectToPortalFeature(cp, guidPortalFeature);
                //
                using var app = new ApplicationModel(cp);
                processForm(app);
                return app.cp.Response.isOpen ? getForm(app) : "";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                cp.Site.LogAlarm($"Ecommerce exception in critical process, ex {ex.ToString()}");
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// process ajax buttons and return to list
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        internal static void processForm(ApplicationModel app) {
            try {
                var cp = app.cp;
                //
                string button = cp.Doc.GetText(Constants.rnButton);
                //
                return;
            } catch (Exception ex) {
                app.cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Create the report html
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        internal static string getForm(ApplicationModel app) {
            try {
                CPBaseClass cp = app.cp;
                //
                // -- load filters
                //
                // -- init layoutbuilder
                var layoutBuilder = cp.AdminUI.CreateLayoutBuilderList();
                layoutBuilder.title = "Users";
                layoutBuilder.description = "Users represent individuals in accounts. They receive benefits from their account subscriptions";
                layoutBuilder.callbackAddonGuid = Constants.guidAddonUserList;
                layoutBuilder.includeForm = true;
                //
                // -- setup column headers
                layoutBuilder.columnCaption = "Row";
                layoutBuilder.columnCaptionClass = "afwWidth20px afwTextAlignCenter";
                layoutBuilder.columnCellClass = "afwWidth20px afwTextAlignCenter";
                //
                layoutBuilder.addColumn();
                layoutBuilder.columnCaption = "<input type=\"checkbox\" id=\"abSelectAccountAllNone\">";
                layoutBuilder.columnCaptionClass = "afwWidth20px afwTextAlignCenter";
                layoutBuilder.columnCellClass = "afwWidth20px afwTextAlignCenter";
                //
                layoutBuilder.addColumn();
                layoutBuilder.columnCaption = "User";
                layoutBuilder.columnCaptionClass = "afwTextAlignLeft";
                layoutBuilder.columnCellClass = "afwTextAlignLeft";
                //
                layoutBuilder.addColumn();
                layoutBuilder.columnCaption = "Account";
                layoutBuilder.columnCaptionClass = "afwTextAlignLeft";
                layoutBuilder.columnCellClass = "afwTextAlignLeft";
                //
                // -- sql where clause from filters
                string sqlWhere = "";
                sqlWhere += string.IsNullOrEmpty(layoutBuilder.sqlSearchTerm) ? "" : $" and((u.name like {cp.Db.EncodeSQLTextLike(layoutBuilder.sqlSearchTerm)})or(a.name like {cp.Db.EncodeSQLTextLike(layoutBuilder.sqlSearchTerm)}))  ";
                //
                // -- determine total records in data (not just the page)
                string sqlCount = @$"
                    select
                        count(*)
                    from
                        ccmembers u
                        left join mmMembershipPeopleRules mar on mar.memberId=u.id
                        left join abaccounts a on a.id=mar.accountid
                    where 1=1
                        and(a.id>0)
                        {sqlWhere}
                    ";
                using (DataTable dt = cp.Db.ExecuteQuery(sqlCount)) {
                    if (dt?.Rows != null && dt.Rows.Count == 1) {
                        layoutBuilder.recordCount = cp.Utils.EncodeInteger(dt.Rows[0][0]);
                    }
                }
                //
                // -- create select query
                string sql = @$"
                    select
	                    u.id as userId,u.name as userName,
	                    a.id as accountid,a.name as accountname
                    from
                        ccmembers u
                        left join mmMembershipPeopleRules mar on mar.memberId=u.id
                        left join abaccounts a on a.id=mar.accountid
                    where 1=1
                        and(a.id>0)
	                    {sqlWhere}
                    order by
                        {(string.IsNullOrEmpty(layoutBuilder.sqlOrderBy) ? "u.name" : layoutBuilder.sqlOrderBy)}
                    OFFSET
                        {(layoutBuilder.paginationPageNumber - 1) * layoutBuilder.paginationPageSize} ROWS FETCH NEXT {layoutBuilder.paginationPageSize} ROWS ONLY
                    ";
                //
                int rowPtr = 0;
                int peopleCid = cp.Content.GetID("people");
                string adminUrl = cp.Site.GetText("adminUrl");
                using (var csList = cp.CSNew()) {
                    if (csList.OpenSQL(sql)) {
                        do {
                            int userId = csList.GetInteger("userId");
                            layoutBuilder.addRow();
                            //
                            // -- row number
                            layoutBuilder.setCell((rowPtr + 1).ToString());
                            //
                            // -- checkbox
                            string rowSelect = cp.Html.CheckBox("row" + rowPtr, false, "abSelectAccountCheckbox");
                            rowSelect += cp.Html5.Hidden("rowId" + rowPtr, userId.ToString());
                            layoutBuilder.setCell(rowSelect);
                            //
                            // -- user name
                            string userName = csList.GetText("userName");
                            userName = string.IsNullOrEmpty(userName) ? "no name" : userName;
                            string userUrl = $"{adminUrl}?af=4&cid={peopleCid}&id={userId}";
                            layoutBuilder.setCell($"<a href=\"{userUrl}\">{userName}</a>");
                            //
                            // -- account name
                            string accountUrl = cp.AdminUI.GetPortalFeatureLink(Constants.guidPortalAccountManager,AccountEditAddon.guidPortalFeature) + $"&accountid={csList.GetInteger(Constants.rnAccountId)}";
                            layoutBuilder.setCell($"<a href=\"{accountUrl}\">{csList.GetText("accountName")}</a>");
                            //
                            rowPtr += 1;
                            csList.GoNext();
                        }
                        while (csList.OK());
                        csList.Close();
                    }
                }
                //
                // -- after-body
                layoutBuilder.htmlAfterBody = "";
                //
                // -- left of body (filters)
                //
                layoutBuilder.htmlLeftOfBody = "";
                //
                // -- buttons
                layoutBuilder.addFormButton(Constants.buttonCancel);
                //
                // -- hiddens
                layoutBuilder.addFormHidden(Constants.rnSrcFormId, Constants.formIdUserList.ToString());
                layoutBuilder.addFormHidden("rowCnt", rowPtr.ToString());
                //
                return layoutBuilder.getHtml();
            } catch (Exception ex) {
                //
                app.cp.Site.ErrorReport(ex);
                return app.cp.Html5.P("There was an error creating this page");
                throw;
            }
        }
    }
}
