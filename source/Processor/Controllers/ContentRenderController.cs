
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// interpret dynamic elements with content including <AC></AC> tags and {% {} %} JSON-based content commands.
    /// </summary>
    public static class ContentRenderController {
        //
        //  active content:
        //      1) addons dropped into wysiwyg editor
        //              processed here
        //      2) json formatted content commands
        //              contentCommandController called (see contentcommandcontroller for syntax and details
        //
        //====================================================================================================
        /// <summary>
        /// render addLinkAuthToAllLinks, ActiveFormatting, ActiveImages and ActiveEditIcons. 
        /// 1) addLinkAuthToAllLinks adds a link authentication querystring to all anchor tags pointed to this application's domains.
        /// 2) ActiveFormatting converts AC type="" /AC tags into thier rendered equvalent.
        /// 3) ActiveImages ?
        /// 4) ActiveEditIcons: if true, it converts AC type="" /AC tags into img tags with instance properties encoded
        ///
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceHtmlContent">The html source to be parsed.</param>
        /// <param name="peopleId">The user to whom this rendering will be targeted</param>
        /// <param name="ContextContentName">If this content is from a DbModel, this is the content name.</param>
        /// <param name="ContextRecordID">If this content is from a DbModel, this is the record id.</param>
        /// <param name="addLinkAuthenticationToAllLinks">If true, link authentication is added to all anchor tags</param>
        /// <param name="ignore"></param>
        /// <param name="encodeACResourceLibraryImages">To be deprecated: this was a way to store only a reference to library images in the content, then replace with img tag while rendering</param>
        /// <param name="encodeForWysiwygEditor">When true, active content (and addons?) are converted to images for the editor. process</param>
        /// <param name="EncodeNonCachableTags">to be deprecated: some tags could be cached and some not, this was a way to divide them.</param>
        /// <param name="queryStringToAppendToAllLinks">If provided, this querystring will be added to all anchor tags that link back to the domains for this application</param>
        /// <param name="protocolHost">The protocol plus domain desired if encoding Resource Library Images or encoding for the Wysiwyg editor</param>
        /// <param name="IsEmailContent">If true, this rendering is for an email.</param>
        /// <param name="AdminURL"></param>
        /// <param name="deprecated_personalizationIsAuthenticated"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string renderContent_ACTags_AnchorTags(CoreController core, string sourceHtmlContent, PersonModel userNullable, string ContextContentName, int ContextRecordID, bool addLinkAuthenticationToAllLinks, bool ignore, bool encodeACResourceLibraryImages, bool encodeForWysiwygEditor, bool EncodeNonCachableTags, string queryStringToAppendToAllLinks, string protocolHost, bool IsEmailContent, string AdminURL, bool deprecated_personalizationIsAuthenticated, CPUtilsBaseClass.addonContext context) {
            string result = sourceHtmlContent;
            try {
                //
                // Fixup Anchor Query (additional AddonOptionString pairs to add to the end)
                string AnchorQuery = "";
                if (addLinkAuthenticationToAllLinks && userNullable != null) {
                    AnchorQuery += "&eid=" + encodeURL(SecurityController.encodeToken(core, userNullable.id, core.dateTimeNowMockable.AddDays(30)));
                }
                //
                if (!string.IsNullOrEmpty(queryStringToAppendToAllLinks)) {
                    AnchorQuery += "&" + queryStringToAppendToAllLinks;
                }
                //
                if (!string.IsNullOrEmpty(AnchorQuery)) {
                    AnchorQuery = AnchorQuery.Substring(1);
                }
                //
                // Test early if this needs to run at all
                bool ProcessACTags = (EncodeNonCachableTags || encodeACResourceLibraryImages || encodeForWysiwygEditor) && (result.IndexOf("<AC ", System.StringComparison.OrdinalIgnoreCase) != -1);
                bool ProcessAnchorTags = (!string.IsNullOrEmpty(AnchorQuery)) && (result.IndexOf("<A ", System.StringComparison.OrdinalIgnoreCase) != -1);
                if ((!string.IsNullOrEmpty(result)) && (ProcessAnchorTags || ProcessACTags)) {
                    //
                    // ----- Load the Active Elements
                    //
                    HtmlParserController KmaHTML = new HtmlParserController(core);
                    KmaHTML.load(result);
                    StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                    int ElementPointer = 0;
                    if (KmaHTML.elementCount > 0) {
                        ElementPointer = 0;
                        result = "";
                        string serverFilePath = protocolHost + "/" + core.appConfig.name + "/files/";
                        while (ElementPointer < KmaHTML.elementCount) {
                            string Copy = KmaHTML.text(ElementPointer).ToString();
                            if (KmaHTML.isTag(ElementPointer)) {
                                string ElementTag = GenericController.toUCase(KmaHTML.tagName(ElementPointer));
                                string ACName = KmaHTML.elementAttribute(ElementPointer, "NAME");
                                string ACType = "";
                                int NotUsedId = 0;
                                string addonOptionString = null;
                                string AddonOptionStringHTMLEncoded = null;
                                string ACInstanceId = null;
                                switch (ElementTag) {
                                    case "A": {
                                            if (!string.IsNullOrEmpty(AnchorQuery)) {
                                                //
                                                // ----- Add ?eid=0000 to all anchors back to the same site so emails
                                                //       can be sent that will automatically log the person in when they
                                                //       arrive.
                                                //
                                                int AttributeCount = KmaHTML.elementAttributeCount(ElementPointer);
                                                if (AttributeCount > 0) {
                                                    Copy = "<A ";
                                                    for (int AttributePointer = 0; AttributePointer < AttributeCount; AttributePointer++) {
                                                        string attrName = KmaHTML.elementAttributeName(ElementPointer, AttributePointer);
                                                        string attrValue = KmaHTML.elementAttributeValue(ElementPointer, AttributePointer);
                                                        if (attrName.ToLower() == "href") {
                                                            string linkDomain = "";
                                                            int Pos = GenericController.strInstr(1, attrValue, "://");
                                                            if (Pos > 0) {
                                                                linkDomain = attrValue;
                                                                linkDomain = linkDomain.Substring(Pos + 2);
                                                                Pos = GenericController.strInstr(1, linkDomain, "/");
                                                                if (Pos > 0) {
                                                                    linkDomain = linkDomain.left(Pos - 1);
                                                                }
                                                            }
                                                            {
                                                                //
                                                                // -- add to all links because it is difficult/impossible to trap every case. downside is we will link to other sites
                                                                if (attrValue.Substring(attrValue.Length - 1) == "?") {
                                                                    //
                                                                    // Ends in a questionmark, must be Dwayne (?)
                                                                    //
                                                                    attrValue += AnchorQuery;
                                                                } else if (GenericController.strInstr(1, attrValue, "mailto:", 1) != 0) {
                                                                    //
                                                                    // catch mailto
                                                                    //
                                                                } else if (GenericController.strInstr(1, attrValue, "?") == 0) {
                                                                    //
                                                                    // No questionmark there, add it
                                                                    //
                                                                    attrValue = attrValue + "?" + AnchorQuery;
                                                                } else {
                                                                    //
                                                                    // Questionmark somewhere, add new value with amp;
                                                                    //
                                                                    attrValue = attrValue + "&" + AnchorQuery;
                                                                }
                                                            }
                                                        }
                                                        Copy += " " + attrName + "=\"" + attrValue + "\"";
                                                    }
                                                    Copy += ">";
                                                }
                                            }
                                            break;
                                        }
                                    case "AC": {
                                            //
                                            // ----- decode all AC tags
                                            //
                                            ACType = KmaHTML.elementAttribute(ElementPointer, "TYPE");
                                            ACInstanceId = KmaHTML.elementAttribute(ElementPointer, "ACINSTANCEID");
                                            string ACGuid = KmaHTML.elementAttribute(ElementPointer, "GUID");
                                            switch (ACType.ToUpper()) {
                                                case ACTypeAggregateFunction: {
                                                        //
                                                        // -- Add-on
                                                        NotUsedId = 0;
                                                        AddonOptionStringHTMLEncoded = KmaHTML.elementAttribute(ElementPointer, "QUERYSTRING");
                                                        addonOptionString = HtmlController.decodeHtml(AddonOptionStringHTMLEncoded);
                                                        if (IsEmailContent) {
                                                            //
                                                            // -- Addon for email
                                                            if (EncodeNonCachableTags) {
                                                                switch (GenericController.toLCase(ACName)) {
                                                                    case "block text": {
                                                                            //
                                                                            // -- start block text
                                                                            Copy = "";
                                                                            string GroupIDList = HtmlController.getAddonOptionStringValue("AllowGroups", addonOptionString);
                                                                            if (userNullable!=null && !GroupController.isInGroupList(core, userNullable.id, true, GroupIDList, true)) {
                                                                                //
                                                                                // Block content if not allowed
                                                                                //
                                                                                ElementPointer += 1;
                                                                                while (ElementPointer < KmaHTML.elementCount) {
                                                                                    ElementTag = GenericController.toUCase(KmaHTML.tagName(ElementPointer));
                                                                                    if (ElementTag == "AC") {
                                                                                        ACType = GenericController.toUCase(KmaHTML.elementAttribute(ElementPointer, "TYPE"));
                                                                                        if (ACType == ACTypeAggregateFunction) {
                                                                                            if (GenericController.toLCase(KmaHTML.elementAttribute(ElementPointer, "name")) == "block text end") {
                                                                                                break;
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                    ElementPointer += 1;
                                                                                }
                                                                            }
                                                                            break;
                                                                        }
                                                                    case "block text end": {
                                                                            //
                                                                            // -- end block text
                                                                            Copy = "";
                                                                            break;
                                                                        }
                                                                    default: {
                                                                            //
                                                                            // -- addons
                                                                            CPUtilsBaseClass.addonExecuteContext executeContext = new CPUtilsBaseClass.addonExecuteContext {
                                                                                addonType = CPUtilsBaseClass.addonContext.ContextEmail,
                                                                                cssContainerClass = "",
                                                                                cssContainerId = "",
                                                                                hostRecord = new CPUtilsBaseClass.addonExecuteHostRecordContext {
                                                                                    contentName = ContextContentName,
                                                                                    fieldName = "",
                                                                                    recordId = ContextRecordID
                                                                                },
                                                                                argumentKeyValuePairs = GenericController.convertQSNVAArgumentstoDocPropertiesList(core, AddonOptionStringHTMLEncoded),
                                                                                instanceGuid = ACInstanceId,
                                                                                errorContextMessage = "rendering addon found in active content within an email"
                                                                            };
                                                                            AddonModel addon = core.cacheStore.addonCache.createByUniqueName(ACName);
                                                                            Copy = core.addon.execute(addon, executeContext);
                                                                            break;
                                                                        }
                                                                }
                                                            }
                                                        } else {
                                                            //
                                                            // Addon - for web
                                                            //

                                                            if (encodeForWysiwygEditor) {
                                                                //
                                                                // Get IconFilename, update the optionstring, and execute optionstring replacement functions
                                                                //
                                                                string AddonContentName = AddonModel.tableMetadata.contentName;
                                                                string SelectList = "Name,Link,ID,ArgumentList,ObjectProgramID,IconFilename,IconWidth,IconHeight,IconSprites,IsInline,ccGuid";
                                                                int IconWidth = 0;
                                                                int IconHeight = 0;
                                                                int IconSprites = 0;
                                                                string IconAlt = "";
                                                                string IconTitle = "";
                                                                bool AddonIsInline = false;
                                                                string SrcOptionList = "";
                                                                string IconFilename = "";
                                                                using (var csData = new CsModel(core)) {
                                                                    string Criteria = "";
                                                                    if (!string.IsNullOrEmpty(ACGuid)) {
                                                                        Criteria = "ccguid=" + DbController.encodeSQLText(ACGuid);
                                                                    } else {
                                                                        Criteria = "name=" + DbController.encodeSQLText(ACName.ToUpper());
                                                                    }
                                                                    if (csData.open(AddonContentName, Criteria, "Name,ID", false, 0, SelectList)) {
                                                                        IconFilename = csData.getText("IconFilename");
                                                                        SrcOptionList = csData.getText("ArgumentList");
                                                                        IconWidth = csData.getInteger("IconWidth");
                                                                        IconHeight = csData.getInteger("IconHeight");
                                                                        IconSprites = csData.getInteger("IconSprites");
                                                                        AddonIsInline = csData.getBoolean("IsInline");
                                                                        ACGuid = csData.getText("ccGuid");
                                                                        IconAlt = ACName;
                                                                        IconTitle = "Rendered as the Add-on [" + ACName + "]";
                                                                    } else {
                                                                        switch (GenericController.toLCase(ACName)) {
                                                                            case "block text": {
                                                                                    IconFilename = "";
                                                                                    SrcOptionList = AddonOptionConstructor_ForBlockText;
                                                                                    IconWidth = 0;
                                                                                    IconHeight = 0;
                                                                                    IconSprites = 0;
                                                                                    AddonIsInline = true;
                                                                                    ACGuid = "";
                                                                                    break;
                                                                                }
                                                                            case "block text end": {
                                                                                    IconFilename = "";
                                                                                    SrcOptionList = "";
                                                                                    IconWidth = 0;
                                                                                    IconHeight = 0;
                                                                                    IconSprites = 0;
                                                                                    AddonIsInline = true;
                                                                                    ACGuid = "";
                                                                                    break;
                                                                                }
                                                                            default: {
                                                                                    IconFilename = "";
                                                                                    SrcOptionList = "";
                                                                                    IconWidth = 0;
                                                                                    IconHeight = 0;
                                                                                    IconSprites = 0;
                                                                                    AddonIsInline = false;
                                                                                    IconAlt = "Unknown Add-on [" + ACName + "]";
                                                                                    IconTitle = "Unknown Add-on [" + ACName + "]";
                                                                                    ACGuid = "";
                                                                                    break;
                                                                                }
                                                                        }
                                                                    }
                                                                    csData.close();
                                                                }
                                                                //
                                                                // Build AddonOptionStringHTMLEncoded from SrcOptionList (for names), itself (for current settings), and SrcOptionList (for select options)
                                                                //
                                                                if (SrcOptionList.IndexOf("wrapper", System.StringComparison.OrdinalIgnoreCase) == -1) {
                                                                    if (AddonIsInline) {
                                                                        SrcOptionList = SrcOptionList + Environment.NewLine + AddonOptionConstructor_Inline;
                                                                    } else {
                                                                        SrcOptionList = SrcOptionList + Environment.NewLine + AddonOptionConstructor_Block;
                                                                    }
                                                                }
                                                                string ResultOptionListHTMLEncoded = "";
                                                                if (!string.IsNullOrEmpty(SrcOptionList)) {
                                                                    ResultOptionListHTMLEncoded = "";
                                                                    SrcOptionList = GenericController.strReplace(SrcOptionList, Environment.NewLine, "\r");
                                                                    SrcOptionList = GenericController.strReplace(SrcOptionList, "\n", "\r");
                                                                    string[] SrcOptions = GenericController.stringSplit(SrcOptionList, "\r");
                                                                    for (int Ptr = 0; Ptr <= SrcOptions.GetUpperBound(0); Ptr++) {
                                                                        string SrcOptionName = SrcOptions[Ptr];
                                                                        int loopPtr2 = 0;

                                                                        while ((SrcOptionName.Length > 1) && (SrcOptionName.left(1) == "\t") && (loopPtr2 < 100)) {
                                                                            SrcOptionName = SrcOptionName.Substring(1);
                                                                            loopPtr2 += 1;
                                                                        }
                                                                        string SrcOptionValueSelector = "";
                                                                        string SrcOptionSelector = "";
                                                                        int Pos = GenericController.strInstr(1, SrcOptionName, "=");
                                                                        if (Pos > 0) {
                                                                            SrcOptionValueSelector = SrcOptionName.Substring(Pos);
                                                                            SrcOptionName = SrcOptionName.left(Pos - 1);
                                                                            SrcOptionSelector = "";
                                                                            Pos = GenericController.strInstr(1, SrcOptionValueSelector, "[");
                                                                            if (Pos != 0) {
                                                                                SrcOptionSelector = SrcOptionValueSelector.Substring(Pos - 1);
                                                                            }
                                                                        }
                                                                        // all Src and Instance vars are already encoded correctly
                                                                        if (!string.IsNullOrEmpty(SrcOptionName)) {
                                                                            // since AddonOptionString is encoded, InstanceOptionValue will be also
                                                                            string InstanceOptionValue = HtmlController.getAddonOptionStringValue(SrcOptionName, addonOptionString);
                                                                            string ResultOptionSelector = core.html.getAddonSelector(SrcOptionName, GenericController.encodeNvaArgument(InstanceOptionValue), SrcOptionSelector);
                                                                            ResultOptionListHTMLEncoded = ResultOptionListHTMLEncoded + "&" + ResultOptionSelector;
                                                                        }
                                                                    }
                                                                    if (!string.IsNullOrEmpty(ResultOptionListHTMLEncoded)) {
                                                                        ResultOptionListHTMLEncoded = ResultOptionListHTMLEncoded.Substring(1);
                                                                    }
                                                                }
                                                                string ACNameCaption = GenericController.strReplace(ACName, "\"", "");
                                                                ACNameCaption = HtmlController.encodeHtml(ACNameCaption);
                                                                string IDControlString = "AC," + ACType + "," + NotUsedId + "," + GenericController.encodeNvaArgument(ACName) + "," + ResultOptionListHTMLEncoded + "," + ACGuid;
                                                                Copy = AddonController.getAddonIconImg(AdminURL, IconWidth, IconHeight, IconSprites, AddonIsInline, IDControlString, IconFilename, serverFilePath, IconAlt, IconTitle, ACInstanceId, 0);
                                                            } else if (EncodeNonCachableTags) {
                                                                //
                                                                // Add-on Experiment - move all processing to the Webclient
                                                                // just pass the name and arguments back in th FPO
                                                                // HTML encode and quote the name and AddonOptionString
                                                                //
                                                                Copy = ""
                                                                + ""
                                                                + "<!-- ADDON "
                                                                + "\"" + ACName + "\""
                                                                + ",\"" + AddonOptionStringHTMLEncoded + "\""
                                                                + ",\"" + ACInstanceId + "\""
                                                                + ",\"" + ACGuid + "\""
                                                                + " -->"
                                                                + "";
                                                            }
                                                            //
                                                        }
                                                        break;
                                                    }
                                                case ACTypeTemplateContent: {
                                                        //
                                                        // ----- Create Template Content
                                                        AddonOptionStringHTMLEncoded = "";
                                                        addonOptionString = "";
                                                        NotUsedId = 0;
                                                        if (encodeForWysiwygEditor) {
                                                            //
                                                            string IconIDControlString = "AC," + ACType + "," + NotUsedId + "," + ACName + "," + AddonOptionStringHTMLEncoded;
                                                            Copy = AddonController.getAddonIconImg(AdminURL, 52, 64, 0, false, IconIDControlString, "" + cdnPrefix + "images/ACTemplateContentIcon.gif", serverFilePath, "Template Page Content", "Renders as the Template Page Content", ACInstanceId, 0);
                                                        } else if (EncodeNonCachableTags) {
                                                            //
                                                            // Add in the Content
                                                            Copy = fpoContentBox;
                                                        }
                                                        break;
                                                    }
                                                default: {
                                                        // do nothing
                                                        break;
                                                    }
                                            }
                                            break;
                                        }
                                    default: {
                                            // do nothing;
                                            break;
                                        }
                                }
                            }
                            //
                            // ----- Output the results
                            //
                            Stream.add(Copy);
                            ElementPointer += 1;
                        }
                    }
                    result = Stream.text;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                // throw;
            }
            return result;
        }
        //
        //===================================================================================================
        /// <summary>
        /// Internal routine to render htmlContent.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceHtmlContent">The content to be rendered</param>
        /// <param name="userId">Personalize the content for this person</param>
        /// <param name="contextContentName">The type of content that the source content. (page content, email, page template etc)</param>
        /// <param name="contextRecordID">The recordid of the content </param>
        /// <param name="contextContactPeopleID"></param>
        /// <param name="convertHtmlToText">if true, the html source will be converted to plain text.</param>
        /// <param name="addLinkAuthToAllLinks"></param>
        /// <param name="encodeActiveFormatting"></param>
        /// <param name="encodeActiveImages"></param>
        /// <param name="encodeActiveEditIcons"></param>
        /// <param name="encodeActivePersonalization"></param>
        /// <param name="queryStringForLinkAppend"></param>
        /// <param name="protocolHostLink"></param>
        /// <param name="isEmailContent"></param>
        /// <param name="ignore_DefaultWrapperID"></param>
        /// <param name="ignore_TemplateCaseOnly_Content"></param>
        /// <param name="addonExecuteContext"></param>
        /// <param name="userIsAuthenticated"></param>
        /// <param name="nothingObject"></param>
        /// <param name="userIsEditing"></param>
        /// <returns></returns>
        //
        public static string renderContent(CoreController core, string sourceHtmlContent, PersonModel userNullable, string contextContentName, int contextRecordID, int contextContactPeopleID, bool convertHtmlToText, bool addLinkAuthToAllLinks, bool encodeActiveFormatting, bool encodeActiveImages, bool encodeActiveEditIcons, bool encodeActivePersonalization, string queryStringForLinkAppend, string protocolHostLink, bool isEmailContent, CPUtilsBaseClass.addonContext addonExecuteContext, bool userIsAuthenticated, bool userIsEditing) {
            string result = sourceHtmlContent;
            string hint = "0";
            try {
                if (string.IsNullOrEmpty(sourceHtmlContent)) { return result; }
                hint = "10";
                //
                hint = "20";
                //
                // -- resize images
                result = updateLibraryFilesInHtmlContent(core, result);
                //
                // -- Do Active Content Conversion
                hint = "30";
                if (addLinkAuthToAllLinks || encodeActiveFormatting || encodeActiveImages || encodeActiveEditIcons) {
                    string AdminURL = "/" + core.appConfig.adminRoute;
                    result = renderContent_ACTags_AnchorTags(core, result, userNullable, contextContentName, contextRecordID, addLinkAuthToAllLinks, encodeActiveFormatting, encodeActiveImages, encodeActiveEditIcons, encodeActivePersonalization, queryStringForLinkAppend, protocolHostLink, isEmailContent, AdminURL, userIsAuthenticated, addonExecuteContext);
                }
                //
                // -- Do Plain Text Conversion
                hint = "40";
                if (convertHtmlToText) {
                    result = HtmlController.convertHtmlToText(core, result);
                }
                //
                // Process Addons
                //   parse as <!-- Addon "Addon Name","OptionString" -->
                //   They are handled here because Addons are written against coreClass, not the Content Server class
                //   ...so Group Email can not process addons 8(
                //   Later, remove the csv routine that translates <ac to this, and process it directly right here
                //   Later, rewrite so addons call csv, not coreClass, so email processing can include addons
                // (2/16/2010) - move csv_EncodeContent to csv, or wait and move it all to CP
                //    eventually, everything should migrate to csv and/or cp to eliminate the coreClass dependancy
                //    and all add-ons run as processes the same as they run on pages, or as remote methods
                // (2/16/2010) - if <!-- AC --> has four arguments, the fourth is the addon guid
                //
                // todo - deprecate execute addons based on this comment system "<!-- addon"
                int LineStart = 0;
                try {
                    hint = "50";
                    const string StartFlag = "<!-- ADDON";
                    const string EndFlag = " -->";
                    if (result.IndexOf(StartFlag) != -1) {
                        hint = "51";
                        int LineEnd = 0;
                        while (result.IndexOf(StartFlag) != -1) {
                            hint = "52";
                            LineStart = GenericController.strInstr(1, result, StartFlag);
                            LineEnd = GenericController.strInstr(LineStart, result, EndFlag);
                            string Copy = "";
                            if (LineEnd == 0) {
                                LogController.logWarn(core, "csv_EncodeContent9, Addon could not be inserted into content because the HTML comment holding the position is not formated correctly");
                                break;
                            } else {
                                hint = "53";
                                string AddonName = "";
                                string addonOptionString = "";
                                string ACInstanceId = "";
                                string AddonGuid = "";
                                int copyLength = LineEnd - LineStart - 11;
                                if (copyLength <= 0) {
                                    //
                                    // -- nothing between start and end, someone added a comment <!-- ADDON -->
                                } else {
                                    hint = "54";
                                    Copy = result.Substring(LineStart + 10, copyLength);
                                    string[] ArgSplit = GenericController.splitDelimited(Copy, ",");
                                    int ArgCnt = ArgSplit.GetUpperBound(0) + 1;
                                    if (!string.IsNullOrEmpty(ArgSplit[0])) {
                                        hint = "55";
                                        AddonName = ArgSplit[0].Substring(1, ArgSplit[0].Length - 2);
                                        if (ArgCnt > 1) {
                                            if (!string.IsNullOrEmpty(ArgSplit[1])) {
                                                addonOptionString = ArgSplit[1].Substring(1, ArgSplit[1].Length - 2);
                                                addonOptionString = HtmlController.decodeHtml(addonOptionString.Trim(' '));
                                            }
                                            if (ArgCnt > 2) {
                                                if (!string.IsNullOrEmpty(ArgSplit[2])) {
                                                    ACInstanceId = ArgSplit[2].Substring(1, ArgSplit[2].Length - 2);
                                                }
                                                if (ArgCnt > 3) {
                                                    if (!string.IsNullOrEmpty(ArgSplit[3])) {
                                                        AddonGuid = ArgSplit[3].Substring(1, ArgSplit[3].Length - 2);
                                                    }
                                                }
                                            }
                                        }
                                        // dont have any way of getting fieldname yet
                                        hint = "56";
                                        CPUtilsBaseClass.addonExecuteContext executeContext = new CPUtilsBaseClass.addonExecuteContext {
                                            addonType = CPUtilsBaseClass.addonContext.ContextPage,
                                            cssContainerClass = "",
                                            cssContainerId = "",
                                            hostRecord = new CPUtilsBaseClass.addonExecuteHostRecordContext {
                                                contentName = contextContentName,
                                                fieldName = "",
                                                recordId = contextRecordID
                                            },
                                            instanceGuid = ACInstanceId,
                                            argumentKeyValuePairs = GenericController.convertQSNVAArgumentstoDocPropertiesList(core, addonOptionString),
                                            errorContextMessage = "rendering active content with guid [" + AddonGuid + "] or name [" + AddonName + "]"
                                        };
                                        hint = "57, AddonGuid [" + AddonGuid + "], AddonName [" + AddonName + "]";
                                        if (!string.IsNullOrEmpty(AddonGuid)) {
                                            Copy = core.addon.execute(core.cacheStore.addonCache.create(AddonGuid), executeContext);
                                        } else {
                                            Copy = core.addon.execute(core.cacheStore.addonCache.createByUniqueName( AddonName), executeContext);
                                        }
                                    }
                                }
                            }
                            hint = "58";
                            result = result.left(LineStart - 1) + Copy + result.Substring(LineEnd + 3);
                        }
                    }
                } catch (Exception ex) {
                    //
                    // -- handle error, but don't abort encode
                    LogController.logError(core, ex, "hint [" + hint + "]");
                }
                //
                // process out text block comments inserted by addons
                // remove all content between BlockTextStartMarker and the next BlockTextEndMarker, or end of copy
                // exception made for the content with just the startmarker because when the AC tag is replaced with
                // with the marker, encode content is called with the result, which is just the marker, and this
                // section will remove it
                //
                hint = "60";
                bool DoAnotherPass = false;
                if ((!userIsEditing) && (result != BlockTextStartMarker)) {
                    DoAnotherPass = true;
                    while ((result.IndexOf(BlockTextStartMarker, System.StringComparison.OrdinalIgnoreCase) != -1) && DoAnotherPass) {
                        LineStart = GenericController.strInstr(1, result, BlockTextStartMarker, 1);
                        if (LineStart == 0) {
                            DoAnotherPass = false;
                        } else {
                            int LineEnd = GenericController.strInstr(LineStart, result, BlockTextEndMarker, 1);
                            if (LineEnd <= 0) {
                                DoAnotherPass = false;
                                result = result.left(LineStart - 1);
                            } else {
                                LineEnd = GenericController.strInstr(LineEnd, result, " -->");
                                if (LineEnd <= 0) {
                                    DoAnotherPass = false;
                                } else {
                                    result = result.left(LineStart - 1) + result.Substring(LineEnd + 3);
                                }
                            }
                        }
                    }
                }
                if (userIsEditing) {
                    if (result.IndexOf("<!-- AFScript -->", System.StringComparison.OrdinalIgnoreCase) != -1) {
                        string Copy = AdminUIController.getEditWrapper(core, "Aggregate Script", "##MARKER##");
                        string[] Wrapper = GenericController.stringSplit(Copy, "##MARKER##");
                        result = GenericController.strReplace(result, "<!-- AFScript -->", Wrapper[0], 1, 99, 1);
                        result = GenericController.strReplace(result, "<!-- /AFScript -->", Wrapper[1], 1, 99, 1);
                    }
                    if (result.IndexOf("<!-- AFReplacement -->", System.StringComparison.OrdinalIgnoreCase) != -1) {
                        string Copy = AdminUIController.getEditWrapper(core, "Aggregate Replacement", "##MARKER##");
                        string[] Wrapper = GenericController.stringSplit(Copy, "##MARKER##");
                        result = GenericController.strReplace(result, "<!-- AFReplacement -->", Wrapper[0], 1, 99, 1);
                        result = GenericController.strReplace(result, "<!-- /AFReplacement -->", Wrapper[1], 1, 99, 1);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex, "hint [" + hint + "]");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// render active content for a web page
        /// </summary>
        /// <param name="core"></param>
        /// <param name="source"></param>
        /// <param name="contextContentName">optional, content from which the data being rendered originated (like 'Page Content')</param>
        /// <param name="ContextRecordID">optional, id of the record from which the data being rendered originated</param>
        /// <param name="deprecated_ContextContactPeopleID">optional, the id of the person who should be contacted for this content. If 0, uses current user.</param>
        /// <param name="ProtocolHostString">The protocol + domain to be used to build URLs if the content includes dynamically generated images (resource library active content) and the domain is different from where the content is being rendered already. Leave blank and the URL will start with a slash.</param>
        /// <param name="DefaultWrapperID">optional, if provided and addon is html on a page, the content will be wrapped in the wrapper indicated</param>
        /// <param name="addonContext">Where this addon is being executed, like as a process, or in an email, or on a page. If not provided page context is assumed (adding assets like js and css to document)</param>
        /// <returns></returns>
        public static string renderHtmlForWeb(CoreController core, string source, string contextContentName = "", int ContextRecordId = 0, int deprecated_ContextContactPeopleId = 0, string ProtocolHostString = "", int DefaultWrapperId = 0, CPUtilsBaseClass.addonContext addonContext = CPUtilsBaseClass.addonContext.ContextPage) {
            string result = ContentCmdController.executeContentCommands(core, source, CPUtilsBaseClass.addonContext.ContextAdmin);
            return renderContent(core, result, core.session.user, contextContentName, ContextRecordId, deprecated_ContextContactPeopleId, false, false, true, true, false, true, "", ProtocolHostString, false, addonContext, core.session.isAuthenticated, core.session.isEditing());
        }
        // 
        //====================================================================================================
        /// <summary>
        /// Decodes ActiveContent and EditIcons into AC tags.
        /// Detect IMG tags:
        /// - If IMG ID attribute is "AC,IMAGE,recordid", convert to AC Image tag
        /// - If IMG ID attribute is "AC,DOWNLOAD,recordid", convert to AC Download tag
        /// - If IMG ID attribute is "AC,ACType,ACFieldName,ACInstanceName,QueryStringArguments,AddonGuid", convert it to generic AC tag
        /// - ACInstanceID - used to identify an AC tag on a page. Each instance of an AC tag must havea unique ACinstanceID
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceHtmlContent"></param>
        /// <returns></returns>
        public static string processWysiwygResponseForSave(CoreController core, string sourceHtmlContent) {
            string result = sourceHtmlContent;
            try {
                if (!string.IsNullOrEmpty(result)) {
                    //
                    // leave this in to make sure old <acform tags are converted back, new editor deals with <form, so no more converting
                    result = GenericController.strReplace(result, "<ACFORM>", "<FORM>");
                    result = GenericController.strReplace(result, "<ACFORM ", "<FORM ");
                    result = GenericController.strReplace(result, "</ACFORM>", "</form>");
                    result = GenericController.strReplace(result, "</ACFORM ", "</FORM ");
                    HtmlParserController DHTML = new HtmlParserController(core);
                    if (DHTML.load(result)) {
                        result = "";
                        int ElementCount = DHTML.elementCount;
                        StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                        if (ElementCount > 0) {
                            //
                            // ----- Locate and replace IMG Edit icons with {$ {} %} notation
                            Stream = new StringBuilderLegacyController();
                            int ElementPointer = 0;
                            for (ElementPointer = 0; ElementPointer < ElementCount; ElementPointer++) {
                                string ElementText = DHTML.text(ElementPointer).ToString();
                                if (DHTML.isTag(ElementPointer)) {
                                    int AttributeCount = 0;
                                    switch (GenericController.toUCase(DHTML.tagName(ElementPointer))) {
                                        case "IMG": {
                                                AttributeCount = DHTML.elementAttributeCount(ElementPointer);
                                                if (AttributeCount > 0) {
                                                    string ImageId = DHTML.elementAttribute(ElementPointer, "id");
                                                    string ImageSrcOriginal = DHTML.elementAttribute(ElementPointer, "src");
                                                    string VirtualFilePathBad = core.appConfig.name + "/files/";
                                                    string serverFilePath = "/" + VirtualFilePathBad;
                                                    if (ImageSrcOriginal.ToLowerInvariant().left(VirtualFilePathBad.Length) == GenericController.toLCase(VirtualFilePathBad)) {
                                                        //
                                                        // if the image is from the virtual file path, but the editor did not include the root path, add it
                                                        //
                                                        ElementText = GenericController.strReplace(ElementText, VirtualFilePathBad, "/" + VirtualFilePathBad, 1, 99, 1);
                                                        ImageSrcOriginal = GenericController.strReplace(ImageSrcOriginal, VirtualFilePathBad, "/" + VirtualFilePathBad, 1, 99, 1);
                                                    }
                                                    string ImageSrc = HtmlController.decodeHtml(ImageSrcOriginal);
                                                    ImageSrc = decodeURL(ImageSrc);
                                                    //
                                                    // problem with this case is if the addon icon image is from another site.
                                                    // not sure how it happened, but I do not think the src of an addon edit icon
                                                    // should be able to prevent the addon from executing.
                                                    //
                                                    string ACIdentifier = "";
                                                    string ACType = "";
                                                    string ACFieldName = "";
                                                    string ACInstanceName = "";
                                                    string ACGuid = "";
                                                    int ImageIDArrayCount = 0;
                                                    string ACQueryString = "";
                                                    int Ptr = 0;
                                                    string[] ImageIDArray = Array.Empty<string>();
                                                    if (0 != GenericController.strInstr(1, ImageId, ",")) {
                                                        ImageIDArray = ImageId.Split(',');
                                                        ImageIDArrayCount = ImageIDArray.GetUpperBound(0) + 1;
                                                        if (ImageIDArrayCount > 5) {
                                                            for (Ptr = 5; Ptr < ImageIDArrayCount; Ptr++) {
                                                                ACGuid = ImageIDArray[Ptr];
                                                                if ((ACGuid.left(1) == "{") && (ACGuid.Substring(ACGuid.Length - 1) == "}")) {
                                                                    //
                                                                    // this element is the guid, go with it
                                                                    //
                                                                    break;
                                                                } else if ((string.IsNullOrEmpty(ACGuid)) && (Ptr == (ImageIDArrayCount - 1))) {
                                                                    //
                                                                    // this is the last element, leave it as the guid
                                                                    //
                                                                    break;
                                                                } else {
                                                                    //
                                                                    // not a valid guid, add it to element 4 and try the next
                                                                    //
                                                                    ImageIDArray[4] = ImageIDArray[4] + "," + ACGuid;
                                                                    ACGuid = "";
                                                                }
                                                            }
                                                        }
                                                        if (ImageIDArrayCount > 1) {
                                                            ACIdentifier = GenericController.toUCase(ImageIDArray[0]);
                                                            ACType = ImageIDArray[1];
                                                            if (ImageIDArrayCount > 2) {
                                                                ACFieldName = ImageIDArray[2];
                                                                if (ImageIDArrayCount > 3) {
                                                                    ACInstanceName = ImageIDArray[3];
                                                                    if (ImageIDArrayCount > 4) {
                                                                        ACQueryString = ImageIDArray[4];
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    int Pos = 0;
                                                    int recordId = 0;
                                                    string imageStyle = null;
                                                    if (ACIdentifier == "AC") {
                                                        {
                                                            {
                                                                //
                                                                // ----- Process AC Tag
                                                                //
                                                                string acInstanceID = DHTML.elementAttribute(ElementPointer, "ACINSTANCEID");
                                                                if (string.IsNullOrEmpty(acInstanceID)) {
                                                                    acInstanceID = GenericController.getGUID();
                                                                }
                                                                ElementText = "";
                                                                string QueryString = null;
                                                                string[] QSSplit = null;
                                                                int QSPtr = 0;
                                                                //----------------------------- change to ACType
                                                                switch (GenericController.toUCase(ACType)) {
                                                                    case "IMAGE": {
                                                                            //
                                                                            // ----- AC Image, Decode Active Images to Resource Library references
                                                                            //
                                                                            if (ImageIDArrayCount >= 4) {
                                                                                recordId = GenericController.encodeInteger(ACInstanceName);
                                                                                string ImageWidthText = DHTML.elementAttribute(ElementPointer, "WIDTH");
                                                                                string ImageHeightText = DHTML.elementAttribute(ElementPointer, "HEIGHT");
                                                                                string ImageAlt = HtmlController.encodeHtml(DHTML.elementAttribute(ElementPointer, "Alt"));
                                                                                int ImageVSpace = GenericController.encodeInteger(DHTML.elementAttribute(ElementPointer, "vspace"));
                                                                                int ImageHSpace = GenericController.encodeInteger(DHTML.elementAttribute(ElementPointer, "hspace"));
                                                                                string ImageAlign = DHTML.elementAttribute(ElementPointer, "Align");
                                                                                string ImageBorder = DHTML.elementAttribute(ElementPointer, "BORDER");
                                                                                string ImageLoop = DHTML.elementAttribute(ElementPointer, "LOOP");
                                                                                imageStyle = DHTML.elementAttribute(ElementPointer, "STYLE");

                                                                                if (!string.IsNullOrEmpty(imageStyle)) {
                                                                                    //
                                                                                    // ----- Process styles, which override attributes
                                                                                    //
                                                                                    string[] IMageStyleArray = imageStyle.Split(';');
                                                                                    int ImageStyleArrayCount = IMageStyleArray.GetUpperBound(0) + 1;
                                                                                    int ImageStyleArrayPointer = 0;
                                                                                    for (ImageStyleArrayPointer = 0; ImageStyleArrayPointer < ImageStyleArrayCount; ImageStyleArrayPointer++) {
                                                                                        string ImageStylePair = IMageStyleArray[ImageStyleArrayPointer].Trim(' ');
                                                                                        int PositionColon = GenericController.strInstr(1, ImageStylePair, ":");
                                                                                        if (PositionColon > 1) {
                                                                                            string ImageStylePairName = (ImageStylePair.left(PositionColon - 1)).Trim(' ');
                                                                                            string ImageStylePairValue = (ImageStylePair.Substring(PositionColon)).Trim(' ');
                                                                                            switch (GenericController.toUCase(ImageStylePairName)) {
                                                                                                case "WIDTH": {
                                                                                                        ImageStylePairValue = GenericController.strReplace(ImageStylePairValue, "px", "");
                                                                                                        ImageWidthText = ImageStylePairValue;
                                                                                                        break;
                                                                                                    }
                                                                                                case "HEIGHT": {
                                                                                                        ImageStylePairValue = GenericController.strReplace(ImageStylePairValue, "px", "");
                                                                                                        ImageHeightText = ImageStylePairValue;
                                                                                                        break;
                                                                                                    }
                                                                                                default: {
                                                                                                        // do nothing
                                                                                                        break;
                                                                                                    }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                                ElementText = "<AC type=\"IMAGE\" ACInstanceID=\"" + acInstanceID + "\" RecordID=\"" + recordId + "\" Style=\"" + imageStyle + "\" Width=\"" + ImageWidthText + "\" Height=\"" + ImageHeightText + "\" VSpace=\"" + ImageVSpace + "\" HSpace=\"" + ImageHSpace + "\" Alt=\"" + ImageAlt + "\" Align=\"" + ImageAlign + "\" Border=\"" + ImageBorder + "\" Loop=\"" + ImageLoop + "\">";
                                                                            }
                                                                            break;
                                                                        }
                                                                    case ACTypeAggregateFunction: {
                                                                            //
                                                                            // Function
                                                                            //
                                                                            QueryString = "";
                                                                            if (!string.IsNullOrEmpty(ACQueryString)) {
                                                                                //
                                                                                string QSHTMLEncoded = GenericController.encodeText(ACQueryString);
                                                                                QueryString = HtmlController.decodeHtml(QSHTMLEncoded);
                                                                                QSSplit = QueryString.Split('&');
                                                                                for (QSPtr = 0; QSPtr <= QSSplit.GetUpperBound(0); QSPtr++) {
                                                                                    Pos = GenericController.strInstr(1, QSSplit[QSPtr], "[");
                                                                                    if (Pos > 0) {
                                                                                        QSSplit[QSPtr] = QSSplit[QSPtr].left(Pos - 1);
                                                                                    }
                                                                                    QSSplit[QSPtr] = HtmlController.encodeHtml(QSSplit[QSPtr]);
                                                                                }
                                                                                QueryString = string.Join("&", QSSplit);
                                                                            }
                                                                            ElementText = "<AC type=\"" + ACType + "\" name=\"" + ACInstanceName + "\" ACInstanceID=\"" + acInstanceID + "\" querystring=\"" + QueryString + "\" guid=\"" + ACGuid + "\">";
                                                                            break;
                                                                        }
                                                                    case ACTypeTemplateContent:
                                                                    case ACTypeTemplateText: {
                                                                            //
                                                                            //
                                                                            //
                                                                            QueryString = "";
                                                                            if (ImageIDArrayCount > 4) {
                                                                                QueryString = GenericController.encodeText(ImageIDArray[4]);
                                                                                QSSplit = QueryString.Split('&');
                                                                                for (QSPtr = 0; QSPtr <= QSSplit.GetUpperBound(0); QSPtr++) {
                                                                                    QSSplit[QSPtr] = HtmlController.encodeHtml(QSSplit[QSPtr]);
                                                                                }
                                                                                QueryString = string.Join("&", QSSplit);

                                                                            }
                                                                            ElementText = "<AC type=\"" + ACType + "\" name=\"" + ACInstanceName + "\" ACInstanceID=\"" + acInstanceID + "\" querystring=\"" + QueryString + "\">";
                                                                            break;
                                                                        }
                                                                    default: {
                                                                            //
                                                                            // All others -- added querystring from element(4) to all others to cover the group access AC object
                                                                            //
                                                                            QueryString = "";
                                                                            if (ImageIDArrayCount > 4) {
                                                                                QueryString = GenericController.encodeText(ImageIDArray[4]);
                                                                                QueryString = HtmlController.decodeHtml(QueryString);
                                                                                QSSplit = QueryString.Split('&');
                                                                                for (QSPtr = 0; QSPtr <= QSSplit.GetUpperBound(0); QSPtr++) {
                                                                                    QSSplit[QSPtr] = HtmlController.encodeHtml(QSSplit[QSPtr]);
                                                                                }
                                                                                QueryString = string.Join("&", QSSplit);
                                                                            }
                                                                            ElementText = "<AC type=\"" + ACType + "\" name=\"" + ACInstanceName + "\" ACInstanceID=\"" + acInstanceID + "\" field=\"" + ACFieldName + "\" querystring=\"" + QueryString + "\">";
                                                                            break;
                                                                        }
                                                                }
                                                            }
                                                        }
                                                    } else if (GenericController.strInstr(1, ImageSrc, "cclibraryfiles", 1) != 0) {
                                                        bool ImageAllowSFResize = core.siteProperties.getBoolean("ImageAllowSFResize", true);
                                                        if (ImageAllowSFResize && true) {
                                                            //
                                                            // if it is a real image, check for resize
                                                            //
                                                            Pos = GenericController.strInstr(1, ImageSrc, "cclibraryfiles", 1);
                                                            if (Pos != 0) {
                                                                string ImageVirtualFilename = ImageSrc.Substring(Pos - 1);
                                                                string[] Paths = ImageVirtualFilename.Split('/');
                                                                if (Paths.GetUpperBound(0) > 2) {
                                                                    if (GenericController.toLCase(Paths[1]) == "filename") {
                                                                        recordId = GenericController.encodeInteger(Paths[2]);
                                                                        if (recordId != 0) {
                                                                            string ImageFilename = Paths[3];
                                                                            string ImageVirtualFilePath = GenericController.strReplace(ImageVirtualFilename, ImageFilename, "");
                                                                            Pos = ImageFilename.LastIndexOf(".", StringComparison.InvariantCulture) + 1;
                                                                            if (Pos > 0) {
                                                                                string ImageFilenameAltSize = "";
                                                                                string ImageFilenameExt = ImageFilename.Substring(Pos);
                                                                                string ImageFilenameNoExt = ImageFilename.left(Pos - 1);
                                                                                Pos = ImageFilenameNoExt.LastIndexOf("-", StringComparison.InvariantCulture) + 1;
                                                                                if (Pos > 0) {
                                                                                    //
                                                                                    // ImageAltSize should be set from the width and height of the img tag,
                                                                                    // NOT from the actual width and height of the image file
                                                                                    // NOT from the suffix of the image filename
                                                                                    // ImageFilenameAltSize is used when the image has been resized, then 'reset' was hit
                                                                                    //  on the properties dialog before the save. The width and height come from this suffix
                                                                                    //
                                                                                    ImageFilenameAltSize = ImageFilenameNoExt.Substring(Pos);
                                                                                    string[] SizeTest = ImageFilenameAltSize.Split('x');
                                                                                    if (SizeTest.GetUpperBound(0) != 1) {
                                                                                        ImageFilenameAltSize = "";
                                                                                    } else {
                                                                                        if ((SizeTest[0].isNumeric() && SizeTest[1].isNumeric())) {
                                                                                            ImageFilenameNoExt = ImageFilenameNoExt.left(Pos - 1);
                                                                                        } else {
                                                                                            ImageFilenameAltSize = "";
                                                                                        }
                                                                                    }
                                                                                }
                                                                                if (GenericController.strInstr(1, sfImageExtList, ImageFilenameExt, 1) != 0) {
                                                                                    //
                                                                                    // Determine ImageWidth and ImageHeight
                                                                                    //
                                                                                    imageStyle = DHTML.elementAttribute(ElementPointer, "style");
                                                                                    int ImageWidth = GenericController.encodeInteger(DHTML.elementAttribute(ElementPointer, "width"));
                                                                                    int ImageHeight = GenericController.encodeInteger(DHTML.elementAttribute(ElementPointer, "height"));
                                                                                    if (!string.IsNullOrEmpty(imageStyle)) {
                                                                                        string[] Styles = imageStyle.Split(';');
                                                                                        for (Ptr = 0; Ptr <= Styles.GetUpperBound(0); Ptr++) {
                                                                                            string[] Style = Styles[Ptr].Split(':');
                                                                                            if (Style.GetUpperBound(0) > 0) {
                                                                                                string StyleName = GenericController.toLCase(Style[0].Trim(' '));
                                                                                                string StyleValue = null;
                                                                                                int StyleValueInt = 0;
                                                                                                if (StyleName == "width") {
                                                                                                    StyleValue = GenericController.toLCase(Style[1].Trim(' '));
                                                                                                    StyleValue = GenericController.strReplace(StyleValue, "px", "");
                                                                                                    StyleValueInt = GenericController.encodeInteger(StyleValue);
                                                                                                    if (StyleValueInt > 0) {
                                                                                                        ImageWidth = StyleValueInt;
                                                                                                    }
                                                                                                } else if (StyleName == "height") {
                                                                                                    StyleValue = GenericController.toLCase(Style[1].Trim(' '));
                                                                                                    StyleValue = GenericController.strReplace(StyleValue, "px", "");
                                                                                                    StyleValueInt = GenericController.encodeInteger(StyleValue);
                                                                                                    if (StyleValueInt > 0) {
                                                                                                        ImageHeight = StyleValueInt;
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                    //
                                                                                    // Get the record values
                                                                                    //
                                                                                    LibraryFilesModel file = LibraryFilesModel.create<LibraryFilesModel>(core.cpParent, recordId);
                                                                                    if (file != null) {
                                                                                        string RecordVirtualFilename = file.filename;
                                                                                        int RecordWidth = file.width;
                                                                                        int RecordHeight = file.height;
                                                                                        string RecordAltSizeList = file.altSizeList;
                                                                                        string RecordFilename = RecordVirtualFilename;
                                                                                        Pos = RecordVirtualFilename.LastIndexOf("/", StringComparison.InvariantCulture) + 1;
                                                                                        if (Pos > 0) {
                                                                                            RecordFilename = RecordVirtualFilename.Substring(Pos);
                                                                                        }
                                                                                        string RecordFilenameExt = "";
                                                                                        string RecordFilenameNoExt = RecordFilename;
                                                                                        Pos = RecordFilenameNoExt.LastIndexOf(".", StringComparison.InvariantCulture) + 1;
                                                                                        if (Pos > 0) {
                                                                                            RecordFilenameExt = RecordFilenameNoExt.Substring(Pos);
                                                                                            RecordFilenameNoExt = RecordFilenameNoExt.left(Pos - 1);
                                                                                        }
                                                                                        //
                                                                                        // if recordwidth or height are missing, get them from the file
                                                                                        //
                                                                                        if (RecordWidth == 0 || RecordHeight == 0) {
                                                                                            using (var imageEditor = new ImageEditController()) {
                                                                                                if (imageEditor.load(ImageVirtualFilename, core.cdnFiles)) {
                                                                                                    file.width = imageEditor.width;
                                                                                                    file.height = imageEditor.height;
                                                                                                    file.save(core.cpParent);
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                        //
                                                                                        // continue only if we have record width and height
                                                                                        //
                                                                                        if (RecordWidth != 0 && RecordHeight != 0) {
                                                                                            //
                                                                                            // set ImageWidth and ImageHeight if one of them is missing
                                                                                            //
                                                                                            if ((ImageWidth == RecordWidth) && (ImageHeight == 0)) {
                                                                                                //
                                                                                                // Image only included width, set default height
                                                                                                //
                                                                                                ImageHeight = RecordHeight;
                                                                                            } else if ((ImageHeight == RecordHeight) && (ImageWidth == 0)) {
                                                                                                //
                                                                                                // Image only included height, set default width
                                                                                                //
                                                                                                ImageWidth = RecordWidth;
                                                                                            } else if ((ImageHeight == 0) && (ImageWidth == 0)) {
                                                                                                //
                                                                                                // Image has no width or height, default both
                                                                                                // This happens when you hit 'reset' on the image properties dialog
                                                                                                //
                                                                                                using (var imageEditor = new ImageEditController()) {
                                                                                                    if (imageEditor.load(ImageVirtualFilename, core.cdnFiles)) {
                                                                                                        ImageWidth = imageEditor.width;
                                                                                                        ImageHeight = imageEditor.height;
                                                                                                    }
                                                                                                }
                                                                                                if ((ImageHeight == 0) && (ImageWidth == 0) && (!string.IsNullOrEmpty(ImageFilenameAltSize))) {
                                                                                                    Pos = GenericController.strInstr(1, ImageFilenameAltSize, "x");
                                                                                                    if (Pos != 0) {
                                                                                                        ImageWidth = GenericController.encodeInteger(ImageFilenameAltSize.left(Pos - 1));
                                                                                                        ImageHeight = GenericController.encodeInteger(ImageFilenameAltSize.Substring(Pos));
                                                                                                    }
                                                                                                }
                                                                                                if (ImageHeight == 0 && ImageWidth == 0) {
                                                                                                    ImageHeight = RecordHeight;
                                                                                                    ImageWidth = RecordWidth;
                                                                                                }
                                                                                            }
                                                                                            //
                                                                                            // Set the ImageAltSize to what was requested from the img tag
                                                                                            // if the actual image is a few rounding-error pixels off does not matter
                                                                                            // if either is 0, let altsize be 0, set real value for image height/width
                                                                                            //
                                                                                            string ImageAltSize = ImageWidth + "x" + ImageHeight.ToString();
                                                                                            string NewImageFilename = null;
                                                                                            //
                                                                                            // determine if we are OK, or need to rebuild
                                                                                            //
                                                                                            if ((RecordVirtualFilename == (ImageVirtualFilePath + ImageFilename)) && ((RecordWidth == ImageWidth) || (RecordHeight == ImageHeight))) {
                                                                                                //
                                                                                                // OK
                                                                                                // this is the raw image
                                                                                                // image matches record, and the sizes are the same
                                                                                                //
                                                                                            } else if ((RecordVirtualFilename == ImageVirtualFilePath + ImageFilenameNoExt + "." + ImageFilenameExt) && (RecordAltSizeList.IndexOf(ImageAltSize, System.StringComparison.OrdinalIgnoreCase) != -1)) {
                                                                                                //
                                                                                                // OK
                                                                                                // resized image, and altsize is in the list - go with resized image name
                                                                                                //
                                                                                                NewImageFilename = ImageFilenameNoExt + "-" + ImageAltSize + "." + ImageFilenameExt;
                                                                                                // images included in email have spaces that must be converted to "%20" or they 404
                                                                                                string imageNewLink = (getCdnFileLink(core, ImageVirtualFilePath) + NewImageFilename).Replace(" ", "%20");
                                                                                                ElementText = GenericController.strReplace(ElementText, ImageSrcOriginal, HtmlController.encodeHtml(imageNewLink));
                                                                                            } else if ((RecordWidth < ImageWidth) || (RecordHeight < ImageHeight)) {
                                                                                                //
                                                                                                // OK
                                                                                                // reize image larger then original - go with it as is
                                                                                                //
                                                                                                // images included in email have spaces that must be converted to "%20" or they 404
                                                                                                string imageNewLink = GenericController.getCdnFileLink(core, RecordVirtualFilename).Replace(" ", "%20");
                                                                                                ElementText = GenericController.strReplace(ElementText, ImageSrcOriginal, HtmlController.encodeHtml(imageNewLink));
                                                                                            } else {
                                                                                                //
                                                                                                // resized image - create NewImageFilename (and add new alt size to the record)
                                                                                                //
                                                                                                if (RecordWidth == ImageWidth && RecordHeight == ImageHeight) {
                                                                                                    //
                                                                                                    // set back to Raw image untouched, use the record image filename
                                                                                                    //
                                                                                                } else {
                                                                                                    //
                                                                                                    // Raw image filename in content, but it is resized, switch to an alternate size
                                                                                                    //
                                                                                                    NewImageFilename = RecordFilename;
                                                                                                    if ((ImageWidth == 0) || (ImageHeight == 0)) {
                                                                                                        //
                                                                                                        // Alt image has not been built
                                                                                                        //
                                                                                                        using (var imageEditor = new ImageEditController()) {
                                                                                                            if (!imageEditor.load(RecordVirtualFilename, core.cdnFiles)) {
                                                                                                                //
                                                                                                                // image load failed, use raw filename
                                                                                                                //
                                                                                                                LogController.logWarn(core, new GenericException("ImageEditController failed to load filename [" + RecordVirtualFilename + "]"));
                                                                                                            } else {
                                                                                                                //
                                                                                                                //
                                                                                                                //
                                                                                                                RecordWidth = imageEditor.width;
                                                                                                                RecordHeight = imageEditor.height;
                                                                                                                if (ImageWidth == 0) {
                                                                                                                    //
                                                                                                                    //
                                                                                                                    //
                                                                                                                    imageEditor.height = ImageHeight;
                                                                                                                } else if (ImageHeight == 0) {
                                                                                                                    //
                                                                                                                    //
                                                                                                                    //
                                                                                                                    imageEditor.width = ImageWidth;
                                                                                                                } else if (RecordHeight == ImageHeight) {
                                                                                                                    //
                                                                                                                    // change the width
                                                                                                                    //
                                                                                                                    imageEditor.width = ImageWidth;
                                                                                                                } else {
                                                                                                                    //
                                                                                                                    // change the height
                                                                                                                    //
                                                                                                                    imageEditor.height = ImageHeight;
                                                                                                                }
                                                                                                                //
                                                                                                                // if resized only width or height, set the other
                                                                                                                //
                                                                                                                if (ImageWidth == 0) {
                                                                                                                    ImageWidth = imageEditor.width;
                                                                                                                    ImageAltSize = ImageWidth + "x" + ImageHeight.ToString();
                                                                                                                }
                                                                                                                if (ImageHeight == 0) {
                                                                                                                    ImageHeight = imageEditor.height;
                                                                                                                    ImageAltSize = ImageWidth + "x" + ImageHeight.ToString();
                                                                                                                }
                                                                                                                //
                                                                                                                // set HTML attributes so image properties will display
                                                                                                                //
                                                                                                                if (GenericController.strInstr(1, ElementText, "height=", 1) == 0) {
                                                                                                                    ElementText = GenericController.strReplace(ElementText, ">", " height=\"" + ImageHeight + "\">");
                                                                                                                }
                                                                                                                if (GenericController.strInstr(1, ElementText, "width=", 1) == 0) {
                                                                                                                    ElementText = GenericController.strReplace(ElementText, ">", " width=\"" + ImageWidth + "\">");
                                                                                                                }
                                                                                                                //
                                                                                                                // Save new file
                                                                                                                //
                                                                                                                NewImageFilename = RecordFilenameNoExt + "-" + ImageAltSize + "." + RecordFilenameExt;
                                                                                                                imageEditor.save(ImageVirtualFilePath + NewImageFilename, core.cdnFiles);
                                                                                                                //
                                                                                                                // Update image record
                                                                                                                //
                                                                                                                RecordAltSizeList = RecordAltSizeList + Environment.NewLine + ImageAltSize;
                                                                                                            }
                                                                                                        }

                                                                                                    }
                                                                                                    //
                                                                                                    // Change the image src to the AltSize
                                                                                                    string newImagePathFilename = getCdnFileLink(core, ImageVirtualFilePath) + NewImageFilename;
                                                                                                    ElementText = strReplace(ElementText, ImageSrcOriginal, HtmlController.encodeHtml(newImagePathFilename.Replace(" ", "%20")));
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                        file.save(core.cpParent);
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                break;
                                            }
                                        default: {
                                                // do nothing
                                                break;
                                            }
                                    }
                                }
                                Stream.add(ElementText);
                            }
                        }
                        result = Stream.text;
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
            //
        }
        //
        //================================================================================================================
        /// <summary>
        /// for html content, this routine optimizes images referenced in the html if they are from library file
        /// </summary>
        public static string updateLibraryFilesInHtmlContent(CoreController core, string htmlContent) {
            try {
                //
                // -- exit if nothing there or not allowed
                if (string.IsNullOrWhiteSpace(htmlContent)) { return htmlContent; }
                if (!core.siteProperties.imageAllowUpdate) { return htmlContent; }
                //
                // -- search for library files and update them
                int posLink = htmlContent.IndexOf(core.appConfig.cdnFileUrl + "cclibraryfiles", StringComparison.OrdinalIgnoreCase);
                if (posLink == -1) { return htmlContent; }
                //
                // ----- Process Resource Library Images (swap in most current file)
                // -- LibraryFileSegments = an array that have key 1 and on start with core.appConfig.cdnFileUrl (usually /appname/files/)
                List<string> libaryFileSegmentList = stringSplit(htmlContent, core.appConfig.cdnFileUrl).ToList();
                string htmlContentUpdated = libaryFileSegmentList.First();
                foreach (var libraryFileSegment in libaryFileSegmentList.Skip(1)) {
                    string htmlContentSegment = libraryFileSegment;
                    //
                    // Determine if this sement is in a tag (<img src="...">) or in content (&quot...&quote)
                    // For now, skip the ones in content
                    int TagPosEnd = GenericController.strInstr(1, htmlContentSegment, ">");
                    int TagPosStart = GenericController.strInstr(1, htmlContentSegment, "<");
                    if ((TagPosStart != 0) && (TagPosEnd < TagPosStart)) {
                        //
                        // break pathfilename off the quote to the end
                        int posQuote = htmlContentSegment.IndexOf("\"");
                        if (posQuote == -1) { continue; }
                        string htmlContentSegment_file = htmlContentSegment.Substring(0, posQuote);

                        htmlContentSegment_file = htmlContentSegment_file.replace("\\", "/", StringComparison.InvariantCultureIgnoreCase);

                        string[] libraryFileSplit = htmlContentSegment_file.Split('/');
                        if (libraryFileSplit.GetUpperBound(0) > 2) {
                            int libraryRecordId = encodeInteger(libraryFileSplit[2]);
                            if ((libraryFileSplit[0].ToLower(CultureInfo.InvariantCulture) == "cclibraryfiles") && (libraryFileSplit[1].ToLower(CultureInfo.InvariantCulture) == "filename") && (libraryRecordId != 0)) {
                                LibraryFilesModel file = LibraryFilesModel.create<LibraryFilesModel>(core.cpParent, libraryRecordId);
                                if ((file != null) && (htmlContentSegment_file != file.filename)) {
                                    htmlContentSegment_file = file.filename;
                                    htmlContentSegment_file = htmlContentSegment_file.replace("\\", "/", StringComparison.InvariantCultureIgnoreCase);
                                }
                                //
                                // -- special case. image URLs with spaces must be corrected to %20 so gmail will not convert ot plus
                                htmlContentSegment_file = htmlContentSegment_file.replace(" ", "%20", StringComparison.InvariantCultureIgnoreCase);
                            }
                        }
                        htmlContentSegment = htmlContentSegment_file + htmlContentSegment.Substring(posQuote);
                    }
                    htmlContentUpdated += core.appConfig.cdnFileUrl + htmlContentSegment;
                }
                return htmlContentUpdated;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return htmlContent;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Convert an active content field (html data stored with ac /ac html tags) to a wysiwyg editor request (html with edit icon img for ac /ac)
        /// </summary>
        public static string renderHtmlForWysiwygEditor(CoreController core, string editorValue) {
            return renderContent(core, editorValue, core.session.user, "", 0, 0, false, false, false, true, true, false, "", "", false, Contensive.BaseClasses.CPUtilsBaseClass.addonContext.ContextSimple, false, false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// (future) for remote methods that render in JSON
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Source"></param>
        /// <param name="ContextContentName"></param>
        /// <param name="ContextRecordID"></param>
        /// <param name="deprecated_ContextContactPeopleID"></param>
        /// <param name="ProtocolHostString"></param>
        /// <param name="DefaultWrapperID"></param>
        /// <param name="ignore_TemplateCaseOnly_Content"></param>
        /// <param name="addonContext"></param>
        /// <returns></returns>
        public static string renderJSONForRemoteMethod(CoreController core, string Source, string ContextContentName, int ContextRecordID, int deprecated_ContextContactPeopleID, string ProtocolHostString, CPUtilsBaseClass.addonContext addonContext) {
            if (core.siteProperties.beta200327_BlockCCmdForJSONRemoteMethods) { return Source; }
            string result = Source;
            result = ContentCmdController.executeContentCommands(core, result, CPUtilsBaseClass.addonContext.ContextAdmin);
            result = renderContent(core, result, core.session.user, ContextContentName, ContextRecordID, deprecated_ContextContactPeopleID, false, false, true, true, false, true, "", ProtocolHostString, false, addonContext, core.session.isAuthenticated, core.session.isEditing());
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// render active content for an email. if user is invalid, original source is returned
        /// </summary>
        /// <param name="core"></param>
        /// <param name="source"></param>
        /// <param name="recipientNullable">If null, skip personalization</param>
        /// <param name="queryStringForLinkAppend"></param>
        /// <returns></returns>
        public static string renderHtmlForEmail(CoreController core, string source, PersonModel recipientNullable, string queryStringForLinkAppend, bool addLinkAuthToAllLinks) {
            //
            // -- create new session context for this user and queue the email.
            using (CPClass cp = new(core.appConfig.name)) {
                //
                // -- if user is valid, attempt to authenticate, else leave as guest
                if (recipientNullable!= null && recipientNullable.id > 0) {
                    cp.core.session.authenticateById(recipientNullable.id, cp.core.session);
                }
                //
                string result = ContentCmdController.executeContentCommands(cp.core, source, CPUtilsBaseClass.addonContext.ContextEmail);
                result = renderContent(cp.core, result, recipientNullable, "", 0, 0, false, addLinkAuthToAllLinks, true, true, false, true, queryStringForLinkAppend, "", true, CPUtilsBaseClass.addonContext.ContextEmail, true, false);
                return result;
            };
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
