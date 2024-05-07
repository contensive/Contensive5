
using System;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.BaseClasses;
using System.Linq;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class QuickEditController : IDisposable {
        //
        //=============================================================================
        /// <summary>
        /// editor for page content inline editing
        /// </summary>
        /// <param name="core"></param>
        /// <param name="rootPageId"></param>
        /// <param name="OrderByClause"></param>
        /// <param name="AllowPageList"></param>
        /// <param name="AllowReturnLink"></param>
        /// <param name="ArchivePages"></param>
        /// <param name="contactMemberID"></param>
        /// <param name="childListSortMethodId"></param>
        /// <param name="main_AllowChildListComposite"></param>
        /// <param name="ArchivePage"></param>
        /// <returns></returns>
        internal static string getQuickEditing(CoreController core) {
            string result = "";
            try {
                int childListSortMethodId = core.doc.pageController.page.childListSortMethodId;
                int contactMemberId = core.doc.pageController.page.contactMemberId;
                int rootPageId = core.doc.pageController.pageToRootList.Last().id;
                core.html.addStyleLink("" + cdnPrefix + "quickEditor/styles.css", "Quick Editor");
                //
                // -- First Active Record - Output Quick Editor form
                Models.Domain.ContentMetadataModel cdef = Models.Domain.ContentMetadataModel.createByUniqueName(core, PageContentModel.tableMetadata.contentName);
                var pageContentTable = DbBaseModel.create<TableModel>(core.cpParent, cdef.id);
                var editLock = WorkflowController.getEditLock(core, pageContentTable.id, core.doc.pageController.page.id);
                WorkflowController.recordWorkflowStatusClass authoringStatus = WorkflowController.getWorkflowStatus(core, PageContentModel.tableMetadata.contentName, core.doc.pageController.page.id);
                PermissionController.UserContentPermissions userContentPermissions = PermissionController.getUserContentPermissions(core, cdef);
                bool AllowMarkReviewed = DbBaseModel.containsField<PageContentModel>("DateReviewed");
                string OptionsPanelAuthoringStatus = core.session.getAuthoringStatusMessage( editLock.isEditLocked, editLock.editLockByMemberName, encodeDate(editLock.editLockExpiresDate));
                //
                // Set Editing Authoring Control
                //
                WorkflowController.setEditLock(core, pageContentTable.id, core.doc.pageController.page.id);
                //
                // SubPanel: Authoring Status
                //
                string leftButtonCommaList = "";
                leftButtonCommaList = leftButtonCommaList + "," + ButtonCancel;
                if (userContentPermissions.allowSave) {
                    leftButtonCommaList = leftButtonCommaList + "," + ButtonSave + "," + ButtonOK;
                }
                if (userContentPermissions.allowDelete && (core.doc.pageController.pageToRootList.Count == 1)) {
                    //
                    // -- allow delete and not root page
                    leftButtonCommaList = leftButtonCommaList + "," + ButtonDelete;
                }
                if (userContentPermissions.allowAdd) {
                    leftButtonCommaList = leftButtonCommaList + "," + ButtonAddChildPage;
                }
                int page_ParentId = 0;
                if ((page_ParentId != 0) && userContentPermissions.allowAdd) {
                    leftButtonCommaList = leftButtonCommaList + "," + ButtonAddSiblingPage;
                }
                if (AllowMarkReviewed) {
                    leftButtonCommaList = leftButtonCommaList + "," + ButtonMarkReviewed;
                }
                if (!string.IsNullOrEmpty(leftButtonCommaList)) {
                    leftButtonCommaList = leftButtonCommaList.Substring(1);
                    leftButtonCommaList = core.html.getPanelButtons(leftButtonCommaList);
                }
                if (!core.doc.userErrorList.Count.Equals(0)) {
                    result += ""
                        + "\r<tr>"
                        + cr2 + "<td colspan=2 class=\"qeRow\"><div class=\"qeHeadCon\">" + ErrorController.getUserError(core) + "</div></td>"
                        + "\r</tr>";
                }
                if (!userContentPermissions.allowSave) {
                    result += ""
                    + "\r<tr>"
                    + cr2 + "<td colspan=\"2\" class=\"qeRow\">" + getQuickEditingBody(core, PageContentModel.tableMetadata.contentName) + "</td>"
                    + "\r</tr>";
                } else {
                    result += ""
                    + "\r<tr>"
                    + cr2 + "<td colspan=\"2\" class=\"qeRow\">" + getQuickEditingBody(core, PageContentModel.tableMetadata.contentName) + "</td>"
                    + "\r</tr>";
                }
                result += "\r<tr>"
                    + cr2 + "<td class=\"qeRow qeLeft\" style=\"padding-top:10px;\">Name</td>"
                    + cr2 + "<td class=\"qeRow qeRight\">" + HtmlController.inputText_Legacy(core, "name", core.doc.pageController.page.name, 1, 0, "", false, !userContentPermissions.allowSave) + "</td>"
                    + "\r</tr>"
                    + "";
                string pageList = "&nbsp;(there are no parent pages)";
                //
                // ----- Parent pages
                //
                if (core.doc.pageController.pageToRootList.Count > 1) {
                    pageList = "<ul class=\"qeListUL\"><li class=\"qeListLI\">Current Page</li></ul>";
                    foreach (PageContentModel testPage in Enumerable.Reverse(core.doc.pageController.pageToRootList)) {
                        string pageCaption = testPage.name;
                        if (string.IsNullOrEmpty(pageCaption)) {
                            pageCaption = "no name #" + GenericController.encodeText(testPage.id);
                        }
                        pageCaption = "<a href=\"" + PageManagerController.getPageLink(core, testPage.id, "")  + "\">" + pageCaption + "</a>";
                        pageList = "<ul class=\"qeListUL\"><li class=\"qeListLI\">" + pageCaption + pageList + "</li></ul>";
                    }
                }
                result += ""
                + "\r<tr>"
                + cr2 + "<td class=\"qeRow qeLeft\" style=\"padding-top:26px;\">Parent Pages</td>"
                + cr2 + "<td class=\"qeRow qeRight\"><div class=\"qeListCon\">" + pageList + "</div></td>"
                + "\r</tr>";
                //
                // ----- Child pages
                //
                AddonModel addon = core.cacheRuntime.addonCache.create(addonGuidChildList);
                CPUtilsBaseClass.addonExecuteContext executeContext = new CPUtilsBaseClass.addonExecuteContext {
                    addonType = CPUtilsBaseClass.addonContext.ContextPage,
                    hostRecord = new CPUtilsBaseClass.addonExecuteHostRecordContext {
                        contentName = PageContentModel.tableMetadata.contentName,
                        fieldName = "",
                        recordId = core.doc.pageController.page.id
                    },
                    argumentKeyValuePairs = GenericController.convertQSNVAArgumentstoDocPropertiesList(core, core.doc.pageController.page.childListInstanceOptions),
                    instanceGuid = PageChildListInstanceId,
                    errorContextMessage = "calling child page addon in quick editing editor"
                };
                pageList = core.addon.execute(addon, executeContext);
                if (GenericController.strInstr(1, pageList, "<ul", 1) == 0) {
                    pageList = "(there are no child pages)";
                }
                result += "\r<tr>"
                    + cr2 + "<td class=\"qeRow qeLeft\" style=\"padding-top:36px;\">Child Pages</td>"
                    + cr2 + "<td class=\"qeRow qeRight\"><div class=\"qeListCon\">" + pageList + "</div></td>"
                    + "\r</tr>";
                result = ""
                    + "\r<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">"
                    + GenericController.nop(result) + "\r</table>";
                result = ""
                    + leftButtonCommaList + result + leftButtonCommaList;
                result = core.html.getPanel(result);
                //
                // Form Wrapper
                //
                result += ""
                    + HtmlController.inputHidden("Type", FormTypePageAuthoring)
                    + HtmlController.inputHidden("ID", core.doc.pageController.page.id)
                    + HtmlController.inputHidden("ContentName", PageContentModel.tableMetadata.contentName);
                result = HtmlController.formMultipart(core, result, core.webServer.requestQueryString, "", "ccForm");
                result = "<div class=\"ccCon\">" + result + "</div>";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //========================================================================
        //
        //========================================================================
        //
        internal static string getQuickEditingBody(CoreController core, string ContentName) {
            string pageCopy = core.doc.pageController.page.copyfilename.content;
            //
            // ----- Page Copy
            //
            int FieldRows = core.userProperty.getInteger(ContentName + ".copyFilename.PixelHeight", 500);
            if (FieldRows < 50) {
                FieldRows = 50;
                core.userProperty.setProperty(ContentName + ".copyFilename.PixelHeight", FieldRows);
            }
            //
            // At this point we do now know the the template so we can not main_Get the stylelist.
            // Put in main_fpo_QuickEditing to be replaced after template known
            //
            core.doc.quickEditCopy = pageCopy;
            return html_quickEdit_fpo;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed;
        //
        public void Dispose()  {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~QuickEditController()  {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);


        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }
    //
}