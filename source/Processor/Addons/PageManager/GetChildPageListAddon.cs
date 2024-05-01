
using System;
using Contensive.Processor.Controllers;
using Contensive.Models.Db;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using System.Collections.Generic;
using System.Text;
//
namespace Contensive.Processor.Addons.PageManager {
    /// <summary>
    /// addon interface
    /// </summary>
    public class GetChildPageList : Contensive.BaseClasses.AddonBaseClass {
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
                string listName = cp.Doc.GetText("instanceId");
                if ( string.IsNullOrWhiteSpace(listName)) {
                    listName = cp.Doc.GetText("List Name");
                }
                result = getChildPageList(core, listName, PageContentModel.tableMetadata.contentName, core.doc.pageController.page.id, true);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
        //
        //====================================================================================================
        //
        //   Creates the child page list used by PageContent
        //
        //   RequestedListName is the name of the ChildList (ActiveContent Child Page List)
        //       ----- New
        //       {CHILDPAGELIST} = the listname for the orphan list at the bottom of all page content, same as "", "ORPHAN", "NONE"
        //       RequestedListName = "", same as "ORPHAN", same as "NONE"
        //           prints orphan list (child pages that have not printed so far (orphan list))
        //       AllowChildListDisplay - if false, no Child Page List is displayed, but authoring tags are still there
        //       Changed to friend, not public
        //       ----- Old
        //       "NONE" returns child pages with no RequestedListName
        //       "" same as "NONE"
        //       "ORPHAN" returns all child pages that have not been printed on this page
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
        public static string getChildPageList(CoreController core, string requestedListName, string contentName, int parentPageID, bool allowChildListDisplay, bool ArchivePages = false) {
            try {
                if (string.IsNullOrEmpty(contentName)) { contentName = PageContentModel.tableMetadata.contentName; }
                string UcaseRequestedListName = toUCase(requestedListName);
                if ((UcaseRequestedListName == "NONE") || (UcaseRequestedListName == "ORPHAN") || (UcaseRequestedListName == "{CHILDPAGELIST}")) {
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
                        if (core.session.isEditing(contentName)) {
                            pageEditLink = AdminUIEditButtonController.getRecordEditAndCutAnchorTag(core, contentName, childPage.id, true, childPage.name);
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
                            // ----- Requested orphan list, and this record is in a named list, and not editing, do not display
                            //
                        } else if ((string.IsNullOrEmpty(UcaseRequestedListName)) && (childPage.parentListName != "")) {
                            //
                            // -- child page has a parentListName but this request does not
                            if (!core.doc.pageController.childPageIdsListed.Contains(childPage.id)) {
                                //
                                // -- child page has not yet displays, if editing show it as an orphan page
                                if (isAuthoring) {
                                    inactiveList.Append("\r<li name=\"page" + childPage.id + "\" name=\"page" + childPage.id + "\"  id=\"page" + childPage.id + "\" class=\"ccEditWrapper ccListItemNoBullet\">");
                                    inactiveList.Append(pageEditLink);
                                    inactiveList.Append("[from missing child page list '" + childPage.parentListName + "': " + LinkedText + "]");
                                    inactiveList.Append("</li>");
                                }
                            }
                        } else if ((string.IsNullOrEmpty(UcaseRequestedListName)) && (!allowChildListDisplay) && (!isAuthoring)) {
                            //
                            // ----- Requested orphan List, Not AllowChildListDisplay, not Authoring, do not display
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
                                inactiveList.Append("\r<li name=\"page" + childPage.id + "\"  id=\"page" + childPage.id + "\" class=\"ccEditWrapper ccListItemNoBullet\">");
                                inactiveList.Append(pageEditLink);
                                inactiveList.Append("[Hidden (Allow in Child Lists is not checked): " + LinkedText + "]");
                                inactiveList.Append("</li>");
                            }
                        } else if (!childPage.active) {
                            //
                            // ----- Not active record, display hint if authoring
                            //
                            if (isAuthoring) {
                                inactiveList.Append("\r<li name=\"page" + childPage.id + "\"  id=\"page" + childPage.id + "\" class=\"ccEditWrapper ccListItemNoBullet\">");
                                inactiveList.Append(pageEditLink);
                                inactiveList.Append("[Hidden (Inactive): " + LinkedText + "]");
                                inactiveList.Append("</li>");
                            }
                        } else if ((childPage.pubDate != DateTime.MinValue) && (childPage.pubDate > core.doc.profileStartTime)) {
                            //
                            // ----- Child page has not been published
                            //
                            if (isAuthoring) {
                                inactiveList.Append("\r<li name=\"page" + childPage.id + "\"  id=\"page" + childPage.id + "\" class=\"ccEditWrapper ccListItemNoBullet\">");
                                inactiveList.Append(pageEditLink);
                                inactiveList.Append("[Hidden (To be published " + childPage.pubDate + "): " + LinkedText + "]");
                                inactiveList.Append("</li>");
                            }
                        } else if ((childPage.dateExpires != DateTime.MinValue) && (childPage.dateExpires < core.doc.profileStartTime)) {
                            //
                            // ----- Child page has expired
                            //
                            if (isAuthoring) {
                                inactiveList.Append("\r<li name=\"page" + childPage.id + "\"  id=\"page" + childPage.id + "\" class=\"ccEditWrapper ccListItemNoBullet\">");
                                inactiveList.Append(pageEditLink);
                                inactiveList.Append("[Hidden (Expired " + childPage.dateExpires + "): " + LinkedText + "]");
                                inactiveList.Append("</li>");
                            }
                        } else {
                            //
                            // ----- display list (and authoring links)
                            //
                            if (isAuthoring) {
                                activeList.Append("\r<li name=\"page" + childPage.id + "\"  id=\"page" + childPage.id + "\" class=\"ccEditWrapper ccListItem allowSort\">");
                                if (!string.IsNullOrEmpty(pageEditLink)) { activeList.Append(HtmlController.div(iconGrip, "ccListItemDragHandle") + pageEditLink + "&nbsp;"); }
                                activeList.Append(LinkedText);
                                //
                                // include authoring mark for content block
                                //
                                if (childPage.blockContent) {
                                    activeList.Append("&nbsp;[Content Blocked]");
                                }
                                if (childPage.blockPage) {
                                    activeList.Append("&nbsp;[Page Blocked]");
                                }
                            } else {
                                activeList.Append("\r<li name=\"page" + childPage.id + "\"  id=\"page" + childPage.id + "\" class=\"ccListItem allowSort\">");
                                activeList.Append(LinkedText);
                            }
                            //
                            // include overview
                            // if AllowBrief is false, BriefFilename is not loaded
                            //
                            if ((childPage.briefFilename.filename != "") && (childPage.allowBrief)) {
                                string Brief = encodeText(core.cdnFiles.readFileText(childPage.briefFilename.filename)).Trim(' ');
                                if (!string.IsNullOrEmpty(Brief)) {
                                    activeList.Append("<div class=\"ccListCopy\">" + Brief + "</div>");
                                }
                            }
                            activeList.Append("</li>");
                            //
                            // -- add child page to childPagesListed list
                            if (!core.doc.pageController.childPageIdsListed.Contains(childPage.id)) { core.doc.pageController.childPageIdsListed.Add(childPage.id); }
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
                    foreach (var AddLink in AdminUIEditButtonController.getRecordAddAnchorTag(core, contentName, "parentid=" + parentPageID + ",ParentListName=" + UcaseRequestedListName, true)) {
                        if (!string.IsNullOrEmpty(AddLink)) { inactiveList.Append("\r<li class=\"ccEditWrapper ccListItemNoBullet\">" + AddLink + "</LI>"); }
                    }
                }
                //
                // ----- If there is a list, add the list start and list end
                //
                string result = activeList + inactiveList.ToString();
                if (!string.IsNullOrEmpty(result)) {
                    result = "\r<ul id=\"childPageList_" + parentPageID + "_" + requestedListName + "\" class=\"ccChildList\">" + result + "\r</ul>";
                }
                if ((!string.IsNullOrEmpty(UcaseRequestedListName)) && (ChildListCount == 0) && isAuthoring) {
                    result = "[Child Page List with no pages]</p><p>" + result;
                }
                return result;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
        //
    }
}
