
using System;
using Contensive.Processor.Controllers;
using Contensive.Models.Db;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using System.Collections.Generic;
using System.Text;
using NLog;
using Contensive.BaseClasses;
using Contensive.Processor.Models.Domain;
//
namespace Contensive.Processor.Addons.PageManager {
    /// <summary>
    /// addon interface
    /// </summary>
    public class GetHiddenChildPageList : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// pageManager addon interface
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core; 
                result = getHiddenChildPageList(core );
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// list out all child pages not previously listed
        /// </summary>
        /// <param name="core"></param>
        /// <param name="result"></param>
        private static string getHiddenChildPageList(CoreController core ) {
            CPBaseClass cp = core.cpParent;
            try {
                //
                // ----- Hidden Child pages. Pages in this list do not appear on the page. This is an admin editing tool to let admins see all pages not associated to a list.
                if (!core.session.isEditing()) {
                    return "";
                }
                cp.Doc.SetProperty("instanceid", Constants.guidHiddenChildPageList);
                return cp.Addon.Execute(guidChildPageListAddon);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        //
        //   Creates the child page list used by PageContent
        //
        //   RequestedListName is the name of the ChildList (ActiveContent Child Page List)
        //       ----- New
        //       {CHILDPAGELIST} = the listname for the Hidden list at the bottom of all page content, same as "", "Hidden", "NONE"
        //       RequestedListName = "", same as "Hidden", same as "NONE"
        //           prints Hidden list (child pages that have not printed so far (Hidden list))
        //       AllowChildListDisplay - if false, no Child Page List is displayed, but authoring tags are still there
        //       Changed to friend, not public
        //       ----- Old
        //       "NONE" returns child pages with no RequestedListName
        //       "" same as "NONE"
        //       "Hidden" returns all child pages that have not been printed on this page
        //           - uses ChildPageListTracking to track what has been seen
        //=============================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="requestedListName"></param>
        /// <param name="contentName"></param>
        /// <param name="parentPageID"></param>
        /// <param name="allowChildListDisplay"></param>
        /// <param name="ArchivePages"></param>
        /// <returns></returns>
        public static string getHiddexnChildPageList(CoreController core, string requestedListName, string contentName, int parentPageID, bool allowChildListDisplay, bool ArchivePages = false) {
            try {
                if (string.IsNullOrEmpty(contentName)) { contentName = PageContentModel.tableMetadata.contentName; }
                string UcaseRequestedListName = toUCase(requestedListName);
                if ((UcaseRequestedListName == "NONE") || (UcaseRequestedListName == "ORPHAN") || (UcaseRequestedListName == "HIDDEN") || (UcaseRequestedListName == "{CHILDPAGELIST}")) {
                    UcaseRequestedListName = "";
                }
                string archiveLink = core.webServer.requestPathPage;
                archiveLink = convertLinkToShortLink(archiveLink, core.webServer.requestDomain, core.appConfig.cdnFileUrl);
                archiveLink = encodeVirtualPath(archiveLink, core.appConfig.cdnFileUrl, appRootPath, core.webServer.requestDomain);
                string sqlCriteria = "(parentId=" + parentPageID + ")" + ((string.IsNullOrWhiteSpace(UcaseRequestedListName)) ? "" : "and(parentListName=" + DbController.encodeSQLText(UcaseRequestedListName) + ")");
                List<PageContentModel> childPageList = DbBaseModel.createList<PageContentModel>(core.cpParent, sqlCriteria, "sortOrder");
                var inactiveList = new StringBuilder();
                var activeList = new StringBuilder();
                bool isAuthoring = core.session.isEditing(contentName);
                int ChildListCount = 0;
                if (childPageList.Count > 0) {
                    string currentPageChildPageIdList = core.cpParent.Doc.GetText("Current Page Child PageId List", "0");
                    string testPageIdList = "," + currentPageChildPageIdList + ",";
                    foreach (PageContentModel childPage in childPageList) {
                        if (!testPageIdList.Contains("," + childPage.id + ",")) {
                            currentPageChildPageIdList += "," + childPage.id;
                        }
                        string PageLink = PageManagerController.getPageLink(core, childPage.id, "", true, false);
                        string pageMenuHeadline = childPage.menuHeadline;
                        if (string.IsNullOrEmpty(pageMenuHeadline)) {
                            pageMenuHeadline = childPage.name.Trim(' ');
                            if (string.IsNullOrEmpty(pageMenuHeadline)) {
                                pageMenuHeadline = "Related Page";
                            }
                        }
                        string pageEditLink = "";
                        if (isAuthoring) {
                            pageEditLink = EditUIController.getEditIcon(core,contentName, childPage.id);
                        }
                        //
                        string link = PageLink;
                        if (ArchivePages) {
                            link = GenericController.modifyLinkQuery(archiveLink, rnPageId, encodeText(childPage.id), true);
                        }
                        bool blockContentComposite = false;
                        if (childPage.blockContent || childPage.blockPage) {
                            blockContentComposite = !PageManagerController.allowThroughPageBlock(core, childPage.id);
                        }
                        string LinkedText = GenericController.getLinkedText("<a href=\"" + HtmlController.encodeHtml(link) + "\">", pageMenuHeadline);
                        if ((string.IsNullOrEmpty(UcaseRequestedListName)) && (childPage.parentListName != "") && (!isAuthoring)) {
                            //
                            // ----- Requested Hidden list, and this record is in a named list, and not editing, do not display
                            //
                        } else if ((string.IsNullOrEmpty(UcaseRequestedListName)) && (childPage.parentListName != "")) {
                            //
                            // -- child page has a parentListName but this request does not
                            if (!core.doc.pageController.childPageIdsListed.Contains(childPage.id)) {
                                //
                                // -- child page has not yet displays, if editing show it as an Hidden page
                                if (isAuthoring) {
                                    string editWrapperClass = "ccEditWrapper";
                                    //if (!core.siteProperties.allowEditModal) { editWrapperClass = "ccEditWrapper"; }
                                    inactiveList.Append("" +
                                        $"<li name=\"page{childPage.id}\" name=\"page{childPage.id}\"  id=\"page{childPage.id}\" class=\"{editWrapperClass} ccListItemNoBullet\">" +
                                        pageEditLink +
                                        "[from missing child page list '" + childPage.parentListName + "': " + LinkedText + "]" +
                                        "</li>");
                                }
                            }
                        } else if ((string.IsNullOrEmpty(UcaseRequestedListName)) && (!allowChildListDisplay) && (!isAuthoring)) {
                            //
                            // ----- Requested Hidden List, Not AllowChildListDisplay, not Authoring, do not display
                            //
                        } else if ((!string.IsNullOrEmpty(UcaseRequestedListName)) && (UcaseRequestedListName != GenericController.toUCase(childPage.parentListName))) {
                            //
                            // ----- requested named list and wrong RequestedListName, do not display
                            //
                        } else if (!childPage.allowInChildLists) {
                            //
                            // ----- Allow in Child Page Lists is false, display hint to authors
                            //
                            if (isAuthoring) {
                                string editWrapperClass = "ccEditWrapper";
                                //if (!core.siteProperties.allowEditModal) { editWrapperClass = "ccEditWrapper"; }
                                inactiveList.Append("" +
                                    $"<li name=\"page{childPage.id}\"  id=\"page{childPage.id}\" class=\"{editWrapperClass} ccListItemNoBullet\">" +
                                    pageEditLink +
                                    $"[Hidden (Allow in Child Lists is not checked): {LinkedText}]" +
                                    "</li>");
                            }
                        } else if (!childPage.active) {
                            //
                            // ----- Not active record, display hint if authoring
                            if (isAuthoring) {
                                string editWrapperClass = "ccEditWrapper";
                                //if (!core.siteProperties.allowEditModal) { editWrapperClass = "ccEditWrapper"; }
                                inactiveList.Append($"<li name=\"page{childPage.id}\"  id=\"page{childPage.id}\" class=\"{editWrapperClass} ccListItemNoBullet\">");
                                inactiveList.Append(pageEditLink);
                                inactiveList.Append("[Hidden (Inactive): " + LinkedText + "]");
                                inactiveList.Append("</li>");
                            }
                        } else if ((childPage.pubDate != DateTime.MinValue) && (childPage.pubDate > core.doc.profileStartTime)) {
                            //
                            // ----- Child page has not been published
                            //
                            if (isAuthoring) {
                                string editWrapperClass = "ccEditWrapper";
                                //if (!core.siteProperties.allowEditModal) { editWrapperClass = "ccEditWrapper"; }
                                inactiveList.Append($"<li name=\"page{childPage.id}\"  id=\"page{childPage.id}\" class=\"{editWrapperClass} ccListItemNoBullet\">");
                                inactiveList.Append(pageEditLink);
                                inactiveList.Append("[Hidden (To be published " + childPage.pubDate + "): " + LinkedText + "]");
                                inactiveList.Append("</li>");
                            }
                        } else if ((childPage.dateExpires != DateTime.MinValue) && (childPage.dateExpires < core.doc.profileStartTime)) {
                            //
                            // ----- Child page has expired
                            //
                            if (isAuthoring) {
                                string editWrapperClass = "ccEditWrapper";
                                //if (!core.siteProperties.allowEditModal) { editWrapperClass = "ccEditWrapper"; }
                                inactiveList.Append($"<li name=\"page{childPage.id}\"  id=\"page{childPage.id}\" class=\"{editWrapperClass} ccListItemNoBullet\">");
                                inactiveList.Append(pageEditLink);
                                inactiveList.Append("[Hidden (Expired " + childPage.dateExpires + "): " + LinkedText + "]");
                                inactiveList.Append("</li>");
                            }
                        } else {
                            //
                            // ----- display list (and authoring links)
                            //
                            if (isAuthoring) {
                                activeList.Append($"<li name=\"page{childPage.id}\"  id=\"page{childPage.id}\" class=\"ccEditWrapper ccListItem allowSort\">");
                                //
                                // -- edit icon and text
                                activeList.Append($"{pageEditLink}&nbsp;{LinkedText}");
                                //
                                // -- content block notes
                                if (childPage.blockContent) { activeList.Append("&nbsp;[Content Blocked]"); }
                                if (childPage.blockPage) { activeList.Append("&nbsp;[Page Blocked]"); }
                                //
                                // -- drag-drop icon
                                activeList.Append(HtmlController.div(iconGrip, "ccListItemDragHandle float-end"));
                            } else {
                                activeList.Append($"<li name=\"page{childPage.id}\"  id=\"page{childPage.id}\" class=\"ccListItem allowSort\">{LinkedText}");
                            }
                            //
                            // include overview
                            // if AllowBrief is false, BriefFilename is not loaded
                            //
                            if (!string.IsNullOrEmpty( childPage.briefFilename.filename) && childPage.allowBrief) {
                                string brief = encodeText(core.cdnFiles.readFileText(childPage.briefFilename.filename)).Trim(' ');
                                if (!string.IsNullOrEmpty(brief)) {
                                    activeList.Append("<div class=\"ccListCopy\">" + brief + "</div>");
                                }
                            }
                            activeList.Append("</li>");
                            //
                            // -- add child page to childPagesListed list
                            if (!core.doc.pageController.childPageIdsListed.Contains(childPage.id)) { 
                                core.doc.pageController.childPageIdsListed.Add(childPage.id); 
                            }
                            ChildListCount += 1;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(currentPageChildPageIdList)) {
                        core.cpParent.Doc.SetProperty("Current Page Child PageId List", currentPageChildPageIdList);
                    }
                }
                //
                // ----- Add Link
                //
                if (!ArchivePages && isAuthoring) {
                    string editWrapperClass = "";
                    if (!core.siteProperties.allowEditModal) { editWrapperClass = "ccEditWrapper"; }
                    foreach (var AddLink in EditUIController.getAddTabList(core, contentName, "parentid=" + parentPageID + ",ParentListName=" + UcaseRequestedListName, true)) {
                        if (!string.IsNullOrEmpty(AddLink)) { 
                            inactiveList.Append($"<li class=\"{editWrapperClass} ccListItemNoBullet\">" + AddLink + "</LI>"); 
                        }
                    }
                }
                //
                // ----- If there is a list, add the list start and list end
                //
                string result = activeList + inactiveList.ToString();
                if (!string.IsNullOrEmpty(result)) {
                    result = "<ul id=\"childPageList_" + parentPageID + "_" + requestedListName + "\" class=\"ccChildList\">" + result + "</ul>";
                }
                if ((!string.IsNullOrEmpty(UcaseRequestedListName)) && (ChildListCount == 0) && isAuthoring) {
                    result = "[Child Page List with no pages]</p><p>" + result;
                }
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
    }
}
