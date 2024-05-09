using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    //
    public static class AdminUIEditButtonController {
        //
        //===================================================================================================
        /// <summary>
        /// Wrap an edit region in a dotted border
        /// </summary>
        /// <param name="core"></param>
        /// <param name="caption"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string getEditWrapper(CoreController core, string caption, string content) {
            if (!core.session.isEditing()) { return content; }
            string result = HtmlController.div(content, "ccEditWrapperContent");
            if (!string.IsNullOrEmpty(caption)) { result = HtmlController.div(caption, "ccEditWrapperCaption") + result; }
            return HtmlController.div(result, "ccEditWrapper", "editWrapper" + core.doc.editWrapperCnt++);
        }
        //
        //===================================================================================================
        /// <summary>
        /// Wrap an edit region in a dotted border
        /// </summary>
        /// <param name="core"></param>
        /// <param name="innerHtml"></param>
        /// <returns></returns>
        public static string getEditWrapper(CoreController core, string innerHtml)
            => getEditWrapper(core, "", innerHtml);
        //
        //===================================================================================================
        /// <summary>
        /// Wrap an edit region in a dotted border
        /// </summary>
        /// <param name="core"></param>
        /// <param name="innerHtml"></param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getEditWrapper(CoreController core, string innerHtml, string contentName, int recordId)
            => getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, contentName, recordId, false) + innerHtml);
        //
        public static string getEditWrapper(CoreController core, string innerHtml, string contentName, int recordId, string customCaption)
            => getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, contentName, recordId, false, "", customCaption) + innerHtml);
        //
        //===================================================================================================
        /// <summary>
        /// Wrap an edit region in a dotted border
        /// </summary>
        /// <param name="core"></param>
        /// <param name="innerHtml"></param>
        /// <param name="contentId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getEditWrapper(CoreController core, string innerHtml, int contentId, int recordId) {
            var metadata = ContentMetadataModel.create(core, contentId);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordId, false, "", "") + innerHtml);
        }
        public static string getEditWrapper(CoreController core, string innerHtml, int contentId, int recordId, string customCaption) {
            var metadata = ContentMetadataModel.create(core, contentId);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordId, false, "", customCaption) + innerHtml);
        }
        //
        //===================================================================================================
        //
        public static string getEditWrapper(CoreController core, string innerHtml, string contentName, string recordGuid) {
            var metadata = ContentMetadataModel.createByUniqueName(core, contentName);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordGuid) + innerHtml);
        }
        //
        public static string getEditWrapper(CoreController core, string innerHtml, string contentName, string recordGuid, string customCaption) {
            var metadata = ContentMetadataModel.createByUniqueName(core, contentName);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordGuid, customCaption) + innerHtml);
        }
        //
        //===================================================================================================
        //
        public static string getEditWrapper(CoreController core, string innerHtml, int contentId, string recordGuid) {
            var metadata = ContentMetadataModel.create(core, contentId);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordGuid) + innerHtml);
        }
        //
        public static string getEditWrapper(CoreController core, string innerHtml, int contentId, string recordGuid, string customCaption) {
            var metadata = ContentMetadataModel.create(core, contentId);
            return getEditWrapper(core, "", getRecordEditAndCutAnchorTag(core, metadata, recordGuid, customCaption) + innerHtml);
        }
        //
        //====================================================================================================
        //
        public static string getRecordEditAndCutAnchorTag(CoreController core, ContentMetadataModel contentMetadata, int recordId, bool allowCut, string recordName, string customCaption) {
            try {
                if (core.siteProperties.allowEditModel) {
                    //
                    // -- legacy edit tag
                    if (!core.session.isEditing()) { return string.Empty; }
                    if (contentMetadata == null) { throw new GenericException("contentMetadata null."); }
                    var editSegmentList = new List<string> {
                        getRecordEditSegment(core, contentMetadata, recordId, recordName, customCaption)
                    };
                    if (allowCut) {
                        string WorkingLink = GenericController.modifyLinkQuery(core.webServer.requestPage + "?" + core.doc.refreshQueryString, rnPageCut, GenericController.encodeText(contentMetadata.id) + "." + GenericController.encodeText(recordId), true);
                        editSegmentList.Add("<a class=\"ccRecordCutLink\" TabIndex=\"-1\" href=\"" + HtmlController.encodeHtml(WorkingLink) + "\">&nbsp;" + iconContentCut.Replace("content cut", getEditSegmentRecordCaption(core, "Cut", contentMetadata.name, customCaption)) + "</a>");
                    }
                    return getRecordEditAnchorTag(core, editSegmentList);
                } else {
                    //
                    // -- edit modal
                    string layout = LayoutController.getLayout(core.cpParent, layoutEditModelGuid, defaultEditModelLayoutName, defaultEditModelLayoutCdnPathFilename, platform5LayoutCdnPathFilename);
                    object dataSet = new EditModalModel( core,  contentMetadata,  recordId,  allowCut,  recordName,  customCaption);
                    string result = MustacheController.renderStringToString("", dataSet);
                    return result;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create edit and cut anchor tag
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="allowCut"></param>
        /// <param name="recordName"></param>
        /// <param name="customCaption"></param>
        /// <returns></returns>
        public static string getRecordEditAndCutAnchorTag(CoreController core, string contentName, int recordId, bool allowCut, string recordName, string customCaption) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (string.IsNullOrWhiteSpace(contentName)) { throw (new GenericException("ContentName [" + contentName + "] is invalid")); }
                var contentMetadata = ContentMetadataModel.createByUniqueName(core, contentName);
                if (contentMetadata == null) { throw new GenericException("ContentName [" + contentName + "], but no content metadata found with this name."); }
                return getRecordEditAndCutAnchorTag(core, contentMetadata, recordId, allowCut, recordName, customCaption);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        public static string getRecordEditAndCutAnchorTag(CoreController core, string contentName, int recordId, bool allowCut, string recordName)
            => getRecordEditAndCutAnchorTag(core, contentName, recordId, allowCut, recordName, "");
        //
        //====================================================================================================
        /// <summary>
        /// create UI edit record link
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordID"></param>
        /// <param name="AllowCut"></param>
        /// <returns></returns>
        public static string getRecordEditAndCutAnchorTag(CoreController core, string ContentName, int RecordID, bool AllowCut) {
            return getRecordEditAndCutAnchorTag(core, ContentName, RecordID, AllowCut, "");
        }
        //
        public static string getRecordEditAnchorTag(CoreController core, string ContentName, int RecordID) {
            return getRecordEditAndCutAnchorTag(core, ContentName, RecordID, false, "");
        }
        //
        public static string getRecordEditAnchorTag(CoreController core, string ContentName, int RecordID, string recordName) {
            return getRecordEditAndCutAnchorTag(core, ContentName, RecordID, false, recordName);
        }
        //
        //====================================================================================================
        //
        public static string getRecordEditAndCutAnchorTag(CoreController core, ContentMetadataModel contentMetadata, string recordGuid) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (contentMetadata == null) { throw new GenericException("contentMetadata null."); }
                var editSegmentList = new List<string> {
                    getRecordEditSegment(core, contentMetadata, recordGuid)
                };
                return getRecordEditAnchorTag(core, editSegmentList);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        public static string getRecordEditAndCutAnchorTag(CoreController core, ContentMetadataModel contentMetadata, string recordGuid, string customCaption) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (contentMetadata == null) { throw new GenericException("contentMetadata null."); }
                var editSegmentList = new List<string> {
                    getRecordEditSegment(core, contentMetadata, recordGuid)
                };
                return getRecordEditAnchorTag(core, editSegmentList);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return caption for edit, cut, paste links
        /// </summary>
        /// <param name="verb">edit, cut, paste, etc</param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="recordName"></param>
        /// <param name="customCaption">if blank, create a new caption.</param>
        /// <returns></returns>
        public static string getEditSegmentRecordCaption(CoreController core, string verb, string contentName, string customCaption) {
            if (!string.IsNullOrEmpty(customCaption)) { return customCaption; }
            string result = verb + "&nbsp;";
            switch (contentName.ToLower(CultureInfo.InvariantCulture)) {
                case "page content": {
                        result += "Page";
                        break;
                    }
                case "page templates": {
                        result += "Template";
                        break;
                    }
                default: {
                        result += core.pluralizationService.Singularize(contentName);
                        break;
                    }
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the edit url
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getRecordEditUrl(CoreController core, int contentId, int recordId)
            => $"/{core.appConfig.adminRoute}?af=4&aa=2&ad=1&cid={contentId}&id={recordId}";
        //
        public static string getRecordEditUrl(CoreController core, int contentId, string recordGuid)
            => $"/{core.appConfig.adminRoute}?af=4&aa=2&ad=1&cid={contentId}&guid={recordGuid}";
        //
        //====================================================================================================
        /// <summary>
        /// return an addon edit link
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cdef"></param>
        /// <param name="addonId"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static string getAddonEditAnchorTag(CoreController core, int addonId, string caption) {
            var cdef = ContentMetadataModel.createByUniqueName(core, "add-ons");
            if (cdef == null) { return string.Empty; }
            return HtmlController.a(iconAddon_Green + ((string.IsNullOrWhiteSpace(caption)) ? "" : "&nbsp;" + caption), getRecordEditUrl(core, cdef.id, addonId), "ccAddonEditLink");
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns an addon edit link to be included getEditLink wrapper
        /// </summary>
        /// <param name="core"></param>
        /// <param name="addonId"></param>
        /// <param name="addonName"></param>
        /// <returns></returns>
        public static string getAddonEditSegment(CoreController core, int addonId, string addonName) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (addonId < 1) { throw (new GenericException("RecordID [" + addonId + "] is invalid")); }
                return getAddonEditAnchorTag(core, addonId, "Edit Add-on" + ((string.IsNullOrEmpty(addonName)) ? "" : " '" + addonName + "'"));
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns a record edit link to be included getEditLink wrapper
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="recordName"></param>
        /// <returns></returns>
        public static string getRecordEditSegment(CoreController core, string contentName, int recordId, string recordName, string customCaption) {
            try {
                if (!core.session.isEditing()) { return string.Empty; }
                if (string.IsNullOrWhiteSpace(contentName)) { throw (new GenericException("ContentName [" + contentName + "] is invalid")); }
                var contentMetadata = ContentMetadataModel.createByUniqueName(core, contentName);
                if (contentMetadata == null) { throw new GenericException("getRecordEditLink called with contentName [" + contentName + "], but no content metadata found with this name."); }
                return getRecordEditSegment(core, contentMetadata, recordId, recordName, customCaption);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns a record edit link to be included getEditLink wrapper
        /// </summary>
        public static string getRecordEditSegment(CoreController core, ContentMetadataModel contentMetadata, int recordId, string recordName, string customCaption)
            => getRecordEditAnchorTag(core, contentMetadata, recordId, getEditSegmentRecordCaption(core, "Edit", contentMetadata.name, customCaption));
        //
        //====================================================================================================
        /// <summary>
        /// returns a record edit link to be included getEditLink wrapper
        /// </summary>
        public static string getRecordEditSegment(CoreController core, ContentMetadataModel contentMetadata, string recordGuid)
            => getRecordEditAnchorTag(core, contentMetadata, recordGuid);
        //
        public static string getRecordEditSegment(CoreController core, string contentName, int recordID)
            => getRecordEditSegment(core, contentName, recordID, "", "");
        //
        //====================================================================================================
        /// <summary>
        /// An edit link (or left justified pill) is composed of a sequence of edit links (content edit links, cut, paste, addon edit) plus an endcap
        /// </summary>
        /// <param name="core"></param>
        /// <param name="editSegmentList"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(CoreController core, List<string> editSegmentList) {
            if (!core.session.isEditing()) { return string.Empty; }
            string result = string.Join("", editSegmentList) + HtmlController.div("&nbsp;", "ccEditLinkEndCap");
            return HtmlController.div(result, "ccRecordLinkCon");
        }
        //
        //====================================================================================================
        /// <summary>
        /// An edit link (or left justified pill) is composed of a sequence of edit links (content edit links, cut, paste, addon edit) plus an endcap
        /// </summary>
        /// <param name="core"></param>
        /// <param name="editSegmentList"></param>
        /// <returns></returns>
        public static string getAddonEditAnchorTag(CoreController core, List<string> editSegmentList) {
            if (!core.session.isAdvancedEditing()) { return string.Empty; }
            string result = string.Join("", editSegmentList) + HtmlController.div("&nbsp;", "ccEditLinkEndCap");
            return HtmlController.div(result, "ccAddonEditLinkCon");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return a list of add links for the content plus all valid subbordinate content
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentName"></param>
        /// <param name="presetNameValueList"></param>
        /// <param name="allowPaste"></param>
        /// <param name="allowUserAdd"></param>
        /// <returns></returns>
        public static List<string> getRecordAddAnchorTag(CoreController core, string contentName, string presetNameValueList, bool allowPaste, bool allowUserAdd, bool includeChildContent) {
            try {
                List<string> result = new List<string>();
                if (!allowUserAdd) { return result; }
                if (string.IsNullOrWhiteSpace(contentName)) { throw (new GenericException("ContentName [" + contentName + "] is invalid")); }
                //
                // -- convert older QS format to command delimited format
                presetNameValueList = presetNameValueList.Replace("&", ",");
                var content = DbBaseModel.createByUniqueName<ContentModel>(core.cpParent, contentName);
                result.AddRange(getRecordAddAnchorTag_GetChildContentLinks(core, content, presetNameValueList, includeChildContent, new List<int>()));
                //
                // -- Add in the paste entry, if needed
                if (!allowPaste) { return result; }
                string ClipBoard = core.visitProperty.getText("Clipboard", "");
                if (string.IsNullOrEmpty(ClipBoard)) { return result; }
                int Position = GenericController.strInstr(1, ClipBoard, ".");
                if (Position == 0) { return result; }
                string[] ClipBoardArray = ClipBoard.Split('.');
                if (ClipBoardArray.GetUpperBound(0) == 0) { return result; }
                int ClipboardContentId = GenericController.encodeInteger(ClipBoardArray[0]);
                int ClipChildRecordId = GenericController.encodeInteger(ClipBoardArray[1]);
                if (content.isParentOf<ContentModel>(core.cpParent, ClipboardContentId)) {
                    int ParentId = 0;
                    if (GenericController.strInstr(1, presetNameValueList, "PARENTID=", 1) != 0) {
                        //
                        // must test for main_IsChildRecord
                        //
                        string BufferString = presetNameValueList;
                        BufferString = BufferString.Replace("(", "");
                        BufferString = BufferString.Replace(")", "");
                        BufferString = BufferString.Replace(",", "&");
                        ParentId = encodeInteger(GenericController.main_GetNameValue_Internal(core, BufferString, "Parentid"));
                    }
                    if ((ParentId != 0) && (!DbBaseModel.isChildOf<PageContentModel>(core.cpParent, ParentId, 0, new List<int>()))) {
                        //
                        // Can not paste as child of itself
                        string PasteLink = core.webServer.requestPage + "?" + core.doc.refreshQueryString;
                        PasteLink = GenericController.modifyLinkQuery(PasteLink, RequestNamePaste, "1", true);
                        PasteLink = GenericController.modifyLinkQuery(PasteLink, rnPasteParentContentId, content.id.ToString(), true);
                        PasteLink = GenericController.modifyLinkQuery(PasteLink, rnPasteParentRecordId, ParentId.ToString(), true);
                        PasteLink = GenericController.modifyLinkQuery(PasteLink, RequestNamePasteFieldList, presetNameValueList, true);
                        string pasteLinkAnchor = HtmlController.a(iconContentPaste_Green + "&nbsp;Paste Record", PasteLink, "ccRecordPasteLink", "", "-1");
                        result.Add(HtmlController.div(pasteLinkAnchor + HtmlController.div("&nbsp;", "ccEditLinkEndCap"), "ccRecordLinkCon"));
                    }
                }
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return new List<string>();
            }
        }
        //
        public static List<string> getRecordAddAnchorTag(CoreController core, string ContentName, string PresetNameValueList, bool AllowPaste) => getRecordAddAnchorTag(core, ContentName, PresetNameValueList, AllowPaste, core.session.isEditing(ContentName), false);
        //
        public static List<string> getRecordAddAnchorTag(CoreController core, string ContentName, string PresetNameValueList) => getRecordAddAnchorTag(core, ContentName, PresetNameValueList, false, core.session.isEditing(ContentName), false);
        //
        //====================================================================================================
        /// <summary>
        /// return a list of record add links. If not includechildContent, the list has 1 entry, else it also includes links for all the child
        /// </summary>
        /// <param name="core"></param>
        /// <param name="content"></param>
        /// <param name="PresetNameValueList"></param>
        /// <param name="usedContentIdList"></param>
        /// <param name="MenuName"></param>
        /// <param name="ParentMenuName"></param>
        /// <returns></returns>
        private static List<string> getRecordAddAnchorTag_GetChildContentLinks(CoreController core, ContentModel content, string PresetNameValueList, bool includeChildContent, List<int> usedContentIdList) {
            var result = new List<string>();
            string Link = "";
            if (content != null) {
                if (usedContentIdList.Contains(content.id)) {
                    throw (new ArgumentException("result , Content Child [" + content.name + "] is one of its own parents", nameof(usedContentIdList)));
                } else {
                    usedContentIdList.Add(content.id);
                    //
                    // -- Determine if use has access
                    bool userHasAccess = false;
                    bool contentAllowAdd = false;
                    bool groupRulesAllowAdd = false;
                    DateTime memberRulesDateExpires;
                    bool memberRulesAllow = false;
                    if (core.session.isAuthenticatedAdmin()) {
                        //
                        // Entry was found
                        userHasAccess = true;
                        contentAllowAdd = true;
                        groupRulesAllowAdd = true;
                        memberRulesAllow = true;
                    } else {
                        //
                        // non-admin member, first check if they have access and main_Get true markers
                        //
                        using var csData = new CsModel(core);
                        string sql = "SELECT ccContent.ID as ContentID, ccContent.AllowAdd as ContentAllowAdd, ccGroupRules.AllowAdd as GroupRulesAllowAdd, ccMemberRules.DateExpires as MemberRulesDateExpires"
                            + " FROM (((ccContent"
                                + " LEFT JOIN ccGroupRules ON ccGroupRules.ContentID=ccContent.ID)"
                                + " LEFT JOIN ccgroups ON ccGroupRules.GroupID=ccgroups.ID)"
                                + " LEFT JOIN ccMemberRules ON ccgroups.ID=ccMemberRules.GroupID)"
                                + " LEFT JOIN ccMembers ON ccMemberRules.memberId=ccMembers.ID"
                            + " WHERE ("
                            + " (ccContent.id=" + content.id + ")"
                            + " AND(ccContent.active<>0)"
                            + " AND(ccGroupRules.active<>0)"
                            + " AND(ccMemberRules.active<>0)"
                            + " AND((ccMemberRules.DateExpires is Null)or(ccMemberRules.DateExpires>" + DbController.encodeSQLDate(core.doc.profileStartTime) + "))"
                            + " AND(ccgroups.active<>0)"
                            + " AND(ccMembers.active<>0)"
                            + " AND(ccMembers.ID=" + core.session.user.id + ")"
                            + " );";
                        csData.openSql(sql);
                        if (csData.ok()) {
                            //
                            // ----- Entry was found, member has some kind of access
                            //
                            userHasAccess = true;
                            contentAllowAdd = content.allowAdd;
                            groupRulesAllowAdd = csData.getBoolean("GroupRulesAllowAdd");
                            memberRulesDateExpires = csData.getDate("MemberRulesDateExpires");
                            memberRulesAllow = false;
                            if (memberRulesDateExpires == DateTime.MinValue) {
                                memberRulesAllow = true;
                            } else if (memberRulesDateExpires > core.doc.profileStartTime) {
                                memberRulesAllow = true;
                            }
                        } else {
                            //
                            // ----- No entry found, this member does not have access, just main_Get ContentID
                            //
                            userHasAccess = true;
                            contentAllowAdd = false;
                            groupRulesAllowAdd = false;
                            memberRulesAllow = false;
                        }
                    }
                    if (userHasAccess) {
                        //
                        // Add the Menu Entry* to the current menu (MenuName)
                        //
                        Link = "";
                        if (contentAllowAdd && groupRulesAllowAdd && memberRulesAllow) {
                            Link = "/" + core.appConfig.adminRoute + "?cid=" + content.id + "&af=4&aa=2&ad=1";
                            if (!string.IsNullOrEmpty(PresetNameValueList)) {
                                Link = Link + "&wc=" + GenericController.encodeRequestVariable(PresetNameValueList);
                            }
                        }
                        string shortName;
                        switch (content.name.ToLower()) {
                            case "page content": {
                                    shortName = "Page";
                                    break;
                                }
                            default: {
                                    shortName = core.pluralizationService.Singularize(content.name);
                                    break;
                                }
                        }
                        result.Add(HtmlController.div(HtmlController.a(iconAdd_Green + "&nbsp;Add " + shortName, Link, "ccRecordAddLink", "", "-1") + HtmlController.div("&nbsp;", "ccEditLinkEndCap"), "ccRecordLinkCon"));
                        //
                        // -- exit now if no child content
                        if (!includeChildContent) { return result; }
                        //
                        // Create child submenu if Child Entries found
                        var childList = DbBaseModel.createList<ContentModel>(core.cpParent, "ParentID=" + content.id);
                        if (childList.Count > 0) {
                            //
                            // ----- Create the ChildPanel with all Children found
                            foreach (var child in childList) {
                                result.AddRange(getRecordAddAnchorTag_GetChildContentLinks(core, child, PresetNameValueList, includeChildContent, usedContentIdList));
                            }
                        }
                    }
                }
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get record add tag
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cdef"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef)
            => getRecordEditAnchorTag(core, cdef, 0, "");
        //
        //====================================================================================================
        /// <summary>
        /// get record edit tag based on recordId
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cdef"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, int recordId)
            => getRecordEditAnchorTag(core, cdef, recordId, "");
        //
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, int recordId, string caption)
            => getRecordEditAnchorTag(getRecordEditUrl(core, cdef.id, recordId), caption, "ccRecordEditLink");
        //
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, int recordId, string caption, string htmlClass)
            => getRecordEditAnchorTag(getRecordEditUrl(core, cdef.id, recordId), caption, htmlClass);
        //
        //====================================================================================================
        /// <summary>
        /// get record edit tag based on guid
        /// </summary>
        /// <param name="core"></param>
        /// <param name="cdef"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, string recordGuid)
            => getRecordEditAnchorTag(core, cdef, recordGuid, "");
        //
        public static string getRecordEditAnchorTag(CoreController core, ContentMetadataModel cdef, string recordGuid, string caption)
            => getRecordEditAnchorTag(getRecordEditUrl(core, cdef.id, recordGuid), caption, "ccRecordEditLink");
        //
        //====================================================================================================
        /// <summary>
        /// UI Edit Record, with url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="caption"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public static string getRecordEditAnchorTag(string url, string caption, string htmlClass)
            => HtmlController.a(iconEdit_Green + ((string.IsNullOrWhiteSpace(caption)) ? "" : "&nbsp;" + caption), url, htmlClass);
        //
        public static string getRecordEditAnchorTag(string url, string caption)
            => getRecordEditAnchorTag(url, caption, "");
        //
        public static string getRecordEditAnchorTag(string url)
            => getRecordEditAnchorTag(url, "", "");
        //
        //====================================================================================================
        /// <summary>
        /// Wrap the content in a common wrapper if authoring is enabled
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string getAdminHintWrapper(CoreController core, string content) {
            return core.session.isEditing() ? content : string.Empty;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static NLog.Logger logger { get; } = NLog.LogManager.GetCurrentClassLogger();
    }
}
