﻿
using System;
using System.Reflection;
using System.Xml;
using System.Collections.Generic;

using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.BaseClasses;
using System.IO;
using System.Data;
using System.Linq;
using Contensive.Processor.Exceptions;
using Contensive.Addons.AdminSite.Controllers;
using Contensive.Processor.Models.Domain;
using static Newtonsoft.Json.JsonConvert;
using Contensive.Models.Db;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// run addons
    /// - first routine should be constructor
    /// - disposable region at end
    /// - if disposable is not needed add: not IDisposable - not contained classes that need to be disposed
    /// </summary>
    public class AddonController : IDisposable {
        //
        // ----- objects passed in constructor, do not dispose
        //
        private CoreController core;
        //
        public enum ScriptLanguages {
            VBScript = 1,
            Javascript = 2
        }
        //
        // ====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core"></param>
        public AddonController(CoreController core) : base() {
            this.core = core;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon because it is a dependency of another addon/page/template. A dependancy is only run once in a page.
        /// </summary>
        /// <param name="addonId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// 
        public string executeDependency(AddonModel addon, CPUtilsBaseClass.addonExecuteContext context) {
            bool saveContextIsIncludeAddon = context.isIncludeAddon;
            context.isIncludeAddon = true;
            string result = execute(addon, context);
            context.isIncludeAddon = saveContextIsIncludeAddon;
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute an addon by its guid
        /// </summary>
        /// <param name="addonGuid"></param>
        /// <param name="executeContext"></param>
        /// <returns></returns>
        public string execute(string addonGuid, CPUtilsBaseClass.addonExecuteContext executeContext) {
            var addon = DbBaseModel.create<AddonModel>(core.cpParent, addonGuid);
            if (addon == null) {
                //
                // -- addon not found
                LogController.logError(core, new ArgumentException("AddonExecute called without valid guid [" + addonGuid + "] from context [" + executeContext.errorContextMessage + "]."));
                return "";
            } else {
                return execute(addon, executeContext);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute addon
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="executeContext"></param>
        /// <returns></returns>
        public string execute(AddonModel addon, CPUtilsBaseClass.addonExecuteContext executeContext) {
            string result = "";
            string hint = "00";
            //
            // -- setup values that have to be in finalize
            bool rootLevelAddon = core.doc.addonRecursionDepth.Count.Equals(0);
            bool save_forceJavascriptToHead = executeContext.forceJavascriptToHead;
            long addonStart = core.doc.appStopWatch.ElapsedMilliseconds;
            //
            if (addon == null) {
                //
                // -- addon not found
                LogController.logWarn(core, new ArgumentException("AddonExecute called with null addon, executeContext [" + executeContext.errorContextMessage + "]."));
            } else {
                try {
                    hint = "01";
                    //
                    LogController.logInfo(core, "execute addon [" + addon.id + ", " + addon.name + "], context [" + executeContext.addonType + "], context [" + executeContext.errorContextMessage + "]");
                    //
                    // -- save the addon details in a fifo stack to popoff during exit. The top of the stack represents the addon being executed
                    core.doc.addonModelStack.Push(addon);
                    if (executeContext == null) {
                        //
                        // -- context not configured 
                        LogController.logError(core, new ArgumentException("The Add-on executeContext was not configured for addon [#" + addon.id + ", " + addon.name + "]."));
                    } else if (!string.IsNullOrEmpty(addon.objectProgramID)) {
                        //
                        // -- addons with activeX components are deprecated
                        string addonDescription = getAddonDescription(core, addon);
                        throw new GenericException("Addon is no longer supported because it contains an active-X component, add-on " + addonDescription + ".");
                    } else {
                        hint = "02";
                        //
                        // -- check for addon recursion beyond limit (addonRecursionLimit)
                        bool blockRecursion = false;
                        bool inRecursionList = core.doc.addonRecursionDepth.ContainsKey(addon.id);
                        if (inRecursionList) blockRecursion = (core.doc.addonRecursionDepth[addon.id] > addonRecursionLimit);
                        if (blockRecursion) {
                            //
                            // -- cannot call an addon within an addon
                            throw new GenericException("Addon recursion limit exceeded. An addon [#" + addon.id + ", " + addon.name + "] cannot be called by itself more than " + addonRecursionLimit + " times.");
                        } else {
                            hint = "03";
                            //
                            // -- track recursion and continue
                            if (!inRecursionList) {
                                core.doc.addonRecursionDepth.Add(addon.id, 1);
                            } else {
                                core.doc.addonRecursionDepth[addon.id] += 1;
                            }
                            //
                            // -- if executeContext.instanceGuid is set, save the current instanceId and set the context value. If not set, leave the current in place
                            string parentInstanceId = core.docProperties.getText("instanceId");
                            if (!string.IsNullOrWhiteSpace(executeContext.instanceGuid)) {
                                core.docProperties.setProperty("instanceId", executeContext.instanceGuid);
                            }
                            //
                            // -- if the addon's javascript is required in the head, set it in the executeContext now so it will propigate into the dependant addons as well
                            executeContext.forceJavascriptToHead = executeContext.forceJavascriptToHead || addon.javascriptForceHead;
                            //
                            // -- run included add-ons before their parent
                            foreach (var dependentAddon in core.addonCache.getDependsOnList(addon.id)) {
                                if (dependentAddon == null) {
                                    LogController.logError(core, new GenericException("Addon not found. An included addon of [" + addon.name + "] was not found. The included addon may have been deleted. Recreate or reinstall the missing addon, then reinstall [" + addon.name + "] or manually correct the included addon selection."));
                                } else {
                                    executeContext.errorContextMessage = "adding dependent addon [" + dependentAddon.name + "] for addon [" + addon.name + "] called within context [" + executeContext.errorContextMessage + "]";
                                    result += executeDependency(dependentAddon, executeContext);
                                }
                            }
                            //List<int> addonIncludeRuleList = core.doc.getAddonIncludeRuleList(addon.id);
                            //foreach ( int includedAddonID in addonIncludeRuleList) {
                            //    AddonModel dependentAddon = DbBaseModel.create<AddonModel>(core.cpInternal, includedAddonID);
                            //    if (dependentAddon == null) {
                            //        LogController.handleError(core, new GenericException("Addon not found. An included addon of [" + addon.name + "] was not found. The included addon may have been deleted. Recreate or reinstall the missing addon, then reinstall [" + addon.name + "] or manually correct the included addon selection."));
                            //    } else {
                            //        executeContext.errorContextMessage = "adding dependent addon [" + dependentAddon.name + "] for addon [" + addon.name + "] called within context [" + executeContext.errorContextMessage + "]";
                            //        result += executeDependency(dependentAddon, executeContext);
                            //    }
                            //}
                            //List<AddonIncludeRuleModel> addonIncludeRules = AddonIncludeRuleModel.createList(core, "(addonid=" + addon.id + ")");
                            //if (addonIncludeRules.Count > 0) {
                            //    string addonContextMessage = executeContext.errorContextMessage;
                            //    foreach (AddonIncludeRuleModel addonRule in addonIncludeRules) {
                            //        if (addonRule.includedAddonID > 0) {
                            //            AddonModel dependentAddon = DbBaseModel.create<AddonModel>(core.cpInternal, addonRule.includedAddonID);
                            //            if (dependentAddon == null) {
                            //                LogController.handleError(core, new GenericException("Addon not found. An included addon of [" + addon.name + "] was not found. The included addon may have been deleted. Recreate or reinstall the missing addon, then reinstall [" + addon.name + "] or manually correct the included addon selection."));
                            //            } else {
                            //                executeContext.errorContextMessage = "adding dependent addon [" + dependentAddon.name + "] for addon [" + addon.name + "] called within context [" + addonContextMessage + "]";
                            //                result += executeDependency(dependentAddon, executeContext);
                            //            }
                            //        }
                            //    }
                            //    executeContext.errorContextMessage = addonContextMessage;
                            //}
                            hint = "04";
                            //
                            // -- properties referenced multiple time 
                            bool allowAdvanceEditor = core.visitProperty.getBoolean("AllowAdvancedEditor");
                            //
                            // -- add addon record arguments to doc properties
                            if (!string.IsNullOrWhiteSpace(addon.argumentList)) {
                                foreach (var addon_argument in addon.argumentList.Replace(Environment.NewLine, "\r").Replace("\n", "\r").Split(Convert.ToChar("\r"))) {
                                    if (!string.IsNullOrEmpty(addon_argument)) {
                                        string[] nvp = addon_argument.Split('=');
                                        if (!string.IsNullOrEmpty(nvp[0])) {
                                            string nvpValue = "";
                                            if (nvp.Length > 1) {
                                                nvpValue = nvp[1];
                                            }
                                            if (nvpValue.IndexOf("[") >= 0) {
                                                nvpValue = nvpValue.Left(nvpValue.IndexOf("["));
                                            }
                                            core.docProperties.setProperty(nvp[0], nvpValue);
                                        }
                                    }
                                }
                            }
                            hint = "05";
                            //
                            // -- add instance properties to doc properties
                            string ContainerCssID = "";
                            string ContainerCssClass = "";
                            foreach (var kvp in executeContext.argumentKeyValuePairs) {
                                switch (kvp.Key.ToLowerInvariant()) {
                                    case "wrapper":
                                        executeContext.wrapperID = GenericController.encodeInteger(kvp.Value);
                                        break;
                                    case "as ajax":
                                        addon.asAjax = GenericController.encodeBoolean(kvp.Value);
                                        break;
                                    case "css container id":
                                        ContainerCssID = kvp.Value;
                                        break;
                                    case "css container class":
                                        ContainerCssClass = kvp.Value;
                                        break;
                                }
                                core.docProperties.setProperty(kvp.Key, kvp.Value);
                            }
                            hint = "06";
                            //
                            // Preprocess arguments into OptionsForCPVars, and set generic instance values wrapperid and asajax
                            if (addon.inFrame & (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodJson)) {
                                //
                                // -- inframe execution, deliver iframe with link back to remote method
                                hint = "07";
                                result = "TBD - inframe";
                                //Link = core.webServer.requestProtocol & core.webServer.requestDomain & requestAppRootPath & core.siteProperties.serverPageDefault
                                //If genericController.vbInstr(1, Link, "?") = 0 Then
                                //    Link = Link & "?"
                                //Else
                                //    Link = Link & "&"
                                //End If
                                //Link = Link _
                                //        & "nocache=" & Rnd() _
                                //        & "&HostContentName=" & EncodeRequestVariable(HostContentName) _
                                //        & "&HostRecordID=" & HostRecordID _
                                //        & "&remotemethodaddon=" & EncodeURL(addon.id.ToString) _
                                //        & "&optionstring=" & EncodeRequestVariable(WorkingOptionString) _
                                //        & ""
                                //FrameID = "frame" & GetRandomInteger(core)
                                //returnVal = "<iframe src=""" & Link & """ id=""" & FrameID & """ onload=""cj.setFrameHeight('" & FrameID & "');"" class=""ccAddonFrameCon"" frameborder=""0"" scrolling=""no"">This content is not visible because your browser does not support iframes</iframe>" _
                                //        & cr & "<script language=javascript type=""text/javascript"">" _
                                //        & cr & "// Safari and Opera need a kick-start." _
                                //        & cr & "var e=document.getElementById('" & FrameID & "');if(e){var iSource=e.src;e.src='';e.src = iSource;}" _
                                //        & cr & "</script>"
                            } else if (addon.asAjax & (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodJson)) {
                                //
                                // -- asajax execution, deliver div with ajax callback
                                //
                                hint = "08";
                                result = "TBD - asajax";
                                //-----------------------------------------------------------------
                                // AsAjax and this is NOT the callback - setup the ajax callback
                                // js,styles and other features from the addon record are added to the host page
                                // during the remote method, these are blocked, but if any are added during
                                //   DLL processing, they have to be handled
                                //-----------------------------------------------------------------
                                //
                                //If True Then
                                //    AsAjaxID = "asajax" & GetRandomInteger(core)
                                //    QS = "" _
                                //& RequestNameRemoteMethodAddon & "=" & EncodeRequestVariable(addon.id.ToString()) _
                                //& "&HostContentName=" & EncodeRequestVariable(HostContentName) _
                                //& "&HostRecordID=" & HostRecordID _
                                //& "&HostRQS=" & EncodeRequestVariable(core.doc.refreshQueryString) _
                                //& "&HostQS=" & EncodeRequestVariable(core.webServer.requestQueryString) _
                                //& "&optionstring=" & EncodeRequestVariable(WorkingOptionString) _
                                //& ""
                                //    '
                                //    ' -- exception made here. AsAjax is not used often, and this can create a QS too long
                                //    '& "&HostForm=" & EncodeRequestVariable(core.webServer.requestFormString) _
                                //    If IsInline Then
                                //        returnVal = cr & "<div ID=" & AsAjaxID & " Class=""ccAddonAjaxCon"" style=""display:inline;""><img src=""https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/images/ajax-loader-small.gif"" width=""16"" height=""16""></div>"
                                //    Else
                                //        returnVal = cr & "<div ID=" & AsAjaxID & " Class=""ccAddonAjaxCon""><img src=""https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/images/ajax-loader-small.gif"" width=""16"" height=""16""></div>"
                                //    End If
                                //    returnVal = returnVal _
                                //& cr & "<script Language=""javaScript"" type=""text/javascript"">" _
                                //& cr & "cj.ajax.qs('" & QS & "','','" & AsAjaxID & "');AdminNavPop=true;" _
                                //& cr & "</script>"
                                //    '
                                //    ' Problem - AsAjax addons must add styles, js and meta to the head
                                //    '   Adding them to the host page covers most cases, but sometimes the DLL itself
                                //    '   adds styles, etc during processing. These have to be added during the remote method processing.
                                //    '   appending the .innerHTML of the head works for FF, but ie blocks it.
                                //    '   using .createElement works in ie, but the tag system right now not written
                                //    '   to save links, etc, it is written to store the entire tag.
                                //    '   Also, OtherHeadTags can not be added this was.
                                //    '
                                //    ' Short Term Fix
                                //    '   For Ajax, Add javascript and style features to head of host page
                                //    '   Then during remotemethod, clear these strings before dll processing. Anything
                                //    '   that is added must have come from the dll. So far, the only addons we have that
                                //    '   do this load styles, so instead of putting in the the head (so ie fails), add styles inline.
                                //    '
                                //    '   This is because ie does not allow innerHTML updates to head tag
                                //    '   scripts and js could be handled with .createElement if only the links were saved, but
                                //    '   otherhead could not.
                                //    '   The case this does not cover is if the addon itself manually adds one of these entries.
                                //    '   In no case can ie handle the OtherHead, however, all the others can be done with .createElement.
                                //    ' Long Term Fix
                                //    '   Convert js, style, and meta tag system to use .createElement during remote method processing
                                //    '
                                //    Call core.html.doc_AddPagetitle2(PageTitle, AddedByName)
                                //    Call core.html.doc_addMetaDescription2(MetaDescription, AddedByName)
                                //    Call core.html.doc_addMetaKeywordList2(MetaKeywordList, AddedByName)
                                //    Call core.html.doc_AddHeadTag2(OtherHeadTags, AddedByName)
                                //    If Not blockJavascriptAndCss Then
                                //        '
                                //        ' add javascript and styles if it has not run already
                                //        '
                                //        Call core.html.addOnLoadJavascript(JSOnLoad, AddedByName)
                                //        Call core.html.addBodyJavascriptCode(JSBodyEnd, AddedByName)
                                //        Call core.html.addJavaScriptLinkHead(JSFilename, AddedByName)
                                //        If addon.StylesFilename.filename <> "" Then
                                //            Call core.html.addStyleLink(core.webServer.requestProtocol & core.webServer.requestDomain & genericController.getCdnFileLink(core, addon.StylesFilename.filename), addon.name & " default")
                                //        End If
                                //        'If CustomStylesFilename <> "" Then
                                //        '    Call core.html.addStyleLink(core.webServer.requestProtocol & core.webServer.requestDomain & genericController.getCdnFileLink(core, CustomStylesFilename), AddonName & " custom")
                                //        'End If
                                //    End If
                                //End If
                            } else {
                                //
                                //-----------------------------------------------------------------
                                // otherwise - produce the content from the addon
                                //   setup RQS as needed - RQS provides the querystring for add-ons to create links that return to the same page
                                //-----------------------------------------------------------------------------------------------------
                                //
                                hint = "09";
                                if (addon.inFrame && (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml)) {
                                    //
                                    // -- remote method called from inframe execution
                                    hint = "10";
                                    result = "TBD - remotemethod inframe";
                                    // Add-on setup for InFrame, running the call-back - this page must think it is just the remotemethod
                                    //If True Then
                                    //    Call core.doc.addRefreshQueryString(RequestNameRemoteMethodAddon, addon.id.ToString)
                                    //    Call core.doc.addRefreshQueryString("optionstring", WorkingOptionString)
                                    //End If
                                } else if (addon.asAjax && (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml)) {
                                    //
                                    // -- remotemethod called from asajax execution
                                    hint = "11";
                                    result = "TBD - remotemethod ajax";
                                    //
                                    // Add-on setup for AsAjax, running the call-back - put the referring page's QS as the RQS
                                    // restore form values
                                    //
                                    //If True Then
                                    //    QS = core.docProperties.getText("Hostform")
                                    //    If QS <> "" Then
                                    //        Call core.docProperties.addQueryString(QS)
                                    //    End If
                                    //    '
                                    //    ' restore refresh querystring values
                                    //    '
                                    //    QS = core.docProperties.getText("HostRQS")
                                    //    QSSplit = Split(QS, "&")
                                    //    For Ptr = 0 To UBound(QSSplit)
                                    //        NVPair = QSSplit[Ptr]
                                    //        If NVPair <> "" Then
                                    //            NVSplit = Split(NVPair, "=")
                                    //            If UBound(NVSplit) > 0 Then
                                    //                Call core.doc.addRefreshQueryString(NVSplit(0), NVSplit(1))
                                    //            End If
                                    //        End If
                                    //    Next
                                    //    '
                                    //    ' restore query string
                                    //    '
                                    //    QS = core.docProperties.getText("HostQS")
                                    //    Call core.docProperties.addQueryString(QS)
                                    //    '
                                    //    ' Clear the style,js and meta features that were delivered to the host page
                                    //    ' After processing, if these strings are not empty, they must have been added by the DLL
                                    //    '
                                    //    '
                                    //    JSOnLoad = ""
                                    //    JSBodyEnd = ""
                                    //    PageTitle = ""
                                    //    MetaDescription = ""
                                    //    MetaKeywordList = ""
                                    //    OtherHeadTags = ""
                                    //    addon.StylesFilename.filename = ""
                                    //    '  CustomStylesFilename = ""
                                    //End If
                                }
                                //
                                //-----------------------------------------------------------------
                                // Do replacements from Option String and Pick out WrapperID, and AsAjax
                                //-----------------------------------------------------------------
                                //
                                hint = "12";
                                string testString = (addon.copy + addon.copyText + addon.pageTitle + addon.metaDescription + addon.metaKeywordList + addon.otherHeadTags + addon.formXML).ToLowerInvariant();
                                if (!string.IsNullOrWhiteSpace(testString)) {
                                    foreach (var key in core.docProperties.getKeyList()) {
                                        if (testString.Contains(("$" + key + "$").ToLowerInvariant())) {
                                            string ReplaceSource = "$" + key + "$";
                                            string ReplaceValue = core.docProperties.getText(key);
                                            addon.copy = addon.copy.Replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                            addon.copyText = addon.copyText.Replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                            addon.pageTitle = addon.pageTitle.Replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                            addon.metaDescription = addon.metaDescription.Replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                            addon.metaKeywordList = addon.metaKeywordList.Replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                            addon.otherHeadTags = addon.otherHeadTags.Replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                            addon.formXML = addon.formXML.Replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                        }
                                    }
                                }
                                //
                                // -- text components
                                hint = "13";
                                string contentParts = addon.copyText + addon.copy;
                                if (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextEditor) {
                                    //
                                    // not editor, encode the content parts of the addon
                                    //
                                    switch (executeContext.addonType) {
                                        case CPUtilsBaseClass.addonContext.ContextEditor:
                                            contentParts = ActiveContentController.renderHtmlForWysiwygEditor(core, contentParts);
                                            break;
                                        case CPUtilsBaseClass.addonContext.ContextEmail:
                                            contentParts = ActiveContentController.renderHtmlForEmail(core, contentParts, core.session.user.id, "");
                                            break;
                                        case CPUtilsBaseClass.addonContext.ContextFilter:
                                        case CPUtilsBaseClass.addonContext.ContextOnBodyEnd:
                                        case CPUtilsBaseClass.addonContext.ContextOnBodyStart:
                                        case CPUtilsBaseClass.addonContext.ContextOnPageEnd:
                                        case CPUtilsBaseClass.addonContext.ContextOnPageStart:
                                        case CPUtilsBaseClass.addonContext.ContextPage:
                                        case CPUtilsBaseClass.addonContext.ContextTemplate:
                                        case CPUtilsBaseClass.addonContext.ContextAdmin:
                                        case CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml:
                                            contentParts = ActiveContentController.renderHtmlForWeb(core, contentParts, executeContext.hostRecord.contentName, executeContext.hostRecord.recordId, 0, "", 0, executeContext.addonType);
                                            break;
                                        case CPUtilsBaseClass.addonContext.ContextOnContentChange:
                                        case CPUtilsBaseClass.addonContext.ContextSimple:
                                            contentParts = ActiveContentController.renderHtmlForWeb(core, contentParts, "", 0, 0, "", 0, executeContext.addonType);
                                            break;
                                        case CPUtilsBaseClass.addonContext.ContextRemoteMethodJson:
                                            contentParts = ActiveContentController.renderJSONForRemoteMethod(core, contentParts, "", 0, 0, "", 0, "", executeContext.addonType);
                                            break;
                                        default:
                                            contentParts = ActiveContentController.renderHtmlForWeb(core, contentParts, "", 0, 0, "", 0, executeContext.addonType);
                                            break;
                                    }
                                }
                                result += contentParts;
                                //
                                // -- RemoteAssetLink
                                hint = "16";
                                if (addon.remoteAssetLink != "") {
                                    string RemoteAssetLink = addon.remoteAssetLink;
                                    if (RemoteAssetLink.IndexOf("://") < 0) {
                                        //
                                        // use request object to build link
                                        if (RemoteAssetLink.Left(1) == "/") {
                                            // asset starts with a slash, add to appRoot
                                            RemoteAssetLink = core.webServer.requestProtocol + core.webServer.requestDomain + RemoteAssetLink;
                                        } else {
                                            // asset is public files
                                            RemoteAssetLink = core.webServer.requestProtocol + core.webServer.requestDomain + core.appConfig.cdnFileUrl + RemoteAssetLink;
                                        }
                                    }
                                    int PosStart = 0;
                                    HttpRequestController kmaHTTP = new HttpRequestController();
                                    string RemoteAssetContent = kmaHTTP.getURL(ref RemoteAssetLink);
                                    int Pos = GenericController.vbInstr(1, RemoteAssetContent, "<body", 1);
                                    if (Pos > 0) {
                                        Pos = GenericController.vbInstr(Pos, RemoteAssetContent, ">");
                                        if (Pos > 0) {
                                            PosStart = Pos + 1;
                                            Pos = GenericController.vbInstr(Pos, RemoteAssetContent, "</body", 1);
                                            if (Pos > 0) {
                                                RemoteAssetContent = RemoteAssetContent.Substring(PosStart - 1, Pos - PosStart);
                                            }
                                        }
                                    }
                                    result += RemoteAssetContent;
                                }
                                //
                                // --  FormXML
                                hint = "17";
                                if (addon.formXML != "") {
                                    bool ExitAddonWithBlankResponse = false;
                                    result += execute_formContent(null, addon.formXML, ref ExitAddonWithBlankResponse, "addon [" + addon.name + "]");
                                    if (ExitAddonWithBlankResponse) {
                                        return string.Empty;
                                    }
                                }
                                //
                                // -- Script Callback
                                hint = "18";
                                if (addon.link != "") {
                                    string callBackLink = encodeVirtualPath(addon.link, core.appConfig.cdnFileUrl, appRootPath, core.webServer.requestDomain);
                                    foreach (var key in core.docProperties.getKeyList()) {
                                        callBackLink = modifyLinkQuery(callBackLink, encodeRequestVariable(key), encodeRequestVariable(core.docProperties.getText(key)), true);
                                    }
                                    foreach (var kvp in executeContext.argumentKeyValuePairs) {
                                        callBackLink = modifyLinkQuery(callBackLink, encodeRequestVariable(kvp.Key), encodeRequestVariable(core.docProperties.getText(kvp.Value)), true);
                                    }
                                    result += "<SCRIPT LANGUAGE=\"JAVASCRIPT\" SRC=\"" + callBackLink + "\"></SCRIPT>";
                                }
                                string AddedByName = addon.name + " addon";
                                //
                                // -- js head links
                                hint = "19";
                                if (addon.jsHeadScriptSrc != "") {
                                    core.html.addScriptLinkSrc(addon.jsHeadScriptSrc, AddedByName + " Javascript Head Src", (executeContext.forceJavascriptToHead || addon.javascriptForceHead), addon.id);
                                }
                                //
                                // -- js head code
                                hint = "20";
                                if (addon.jsFilename.filename != "") {
                                    string scriptFilename = GenericController.getCdnFileLink(core, addon.jsFilename.filename);
                                    //string scriptFilename = core.webServer.requestProtocol + core.webServer.requestDomain + genericController.getCdnFileLink(core, addon.JSFilename.filename);
                                    core.html.addScriptLinkSrc(scriptFilename, AddedByName + " Javascript Head Code", (executeContext.forceJavascriptToHead || addon.javascriptForceHead), addon.id);
                                }
                                //
                                // -- non-js html assets (styles,head tags), set flag to block duplicates 
                                hint = "21";
                                if (!core.doc.addonIdListRunInThisDoc.Contains(addon.id)) {
                                    core.doc.addonIdListRunInThisDoc.Add(addon.id);
                                    core.html.addTitle(addon.pageTitle, AddedByName);
                                    core.html.addMetaDescription(addon.metaDescription, AddedByName);
                                    core.html.addMetaKeywordList(addon.metaKeywordList, AddedByName);
                                    core.html.addHeadTag(addon.otherHeadTags, AddedByName);
                                    ////
                                    //// -- js body links
                                    //if (addon.JSBodyScriptSrc != "") {
                                    //    core.html.addScriptLink_Body(addon.JSBodyScriptSrc, AddedByName + " Javascript Body Src");
                                    //}
                                    ////
                                    //// -- js body code
                                    //core.html.addScriptCode_body(addon.JavaScriptBodyEnd, AddedByName + " Javascript Body Code");
                                    //
                                    // -- styles
                                    if (addon.stylesFilename.filename != "") {
                                        core.html.addStyleLink(GenericController.getCdnFileLink(core, addon.stylesFilename.filename), addon.name + " Stylesheet");
                                    }
                                    //
                                    // -- link to stylesheet
                                    if (addon.stylesLinkHref != "") {
                                        core.html.addStyleLink(addon.stylesLinkHref, addon.name + " Stylesheet Link");
                                    }
                                }
                                //
                                // -- Add Css containers
                                hint = "22";
                                if (!string.IsNullOrEmpty(ContainerCssID) || !string.IsNullOrEmpty(ContainerCssClass)) {
                                    if (addon.isInline) {
                                        result = "\r<span id=\"" + ContainerCssID + "\" class=\"" + ContainerCssClass + "\" style=\"display:inline;\">" + result + "</span>";
                                    } else {
                                        result = "\r<div id=\"" + ContainerCssID + "\" class=\"" + ContainerCssClass + "\">" + nop(result) + "\r</div>";
                                    }
                                }
                                //
                                // -- add scripting and dotnet code last, so if the execution adds javascript to the head, the code in the fields is first.
                                // -- Scripting code
                                hint = "14";
                                if (addon == null) { LogController.logError(core, new GenericException("AddonController.execute, addon became null at hint-14"), ""); }
                                if (addon.scriptingCode == null) { LogController.logError(core, new GenericException("AddonController.execute, addon.scriptCode is null at hint-14"), ""); }
                                if (addon.scriptingCode != "") {
                                    hint = "14.1";
                                    try {
                                        hint = "14.2";
                                        if (addon.scriptingLanguageID == (int)ScriptLanguages.Javascript) {
                                            hint = "14.3";
                                            result += execute_Script_JScript(ref addon);
                                        } else {
                                            hint = "14.4";
                                            result += execute_Script_VBScript(ref addon);
                                        }
                                    } catch (Exception ex) {
                                        hint = "14.5";
                                        string addonDescription = getAddonDescription(core, addon);
                                        throw new GenericException("There was an error executing the script component of Add-on [" + addonDescription + "]. The exception was [" + ex.ToString() + "]." + ((ex.InnerException != null) ? " There was an inner exception [" + ex.InnerException.Message + "]" : ""));
                                    }
                                }
                                //
                                // -- DotNet
                                hint = "15";
                                if (addon.dotNetClass != "") {
                                    result += execute_dotNetClass(executeContext, addon, AddonCollectionModel.create<AddonCollectionModel>(core.cpParent, addon.collectionID));
                                }

                            }
                            //
                            //   Add Wrappers to content
                            hint = "23";
                            if (addon.inFrame && (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml)) {
                                //
                                // -- iFrame content, framed in content, during the remote method call, add in the rest of the html page
                                hint = "24";
                                core.doc.setMetaContent(0, 0);
                                result = ""
                                    + core.siteProperties.docTypeDeclaration + Environment.NewLine + "<html>"
                                    + Environment.NewLine + "<head>"
                                    + core.html.getHtmlHead()
                                    + Environment.NewLine + "</head>"
                                    + Environment.NewLine + TemplateDefaultBodyTag
                                    + Environment.NewLine + "</body>"
                                    + Environment.NewLine + "</html>";
                            } else if (addon.asAjax && (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextRemoteMethodJson)) {
                                //
                                // -- as ajax content, AsAjax addon, during the Ajax callback, need to create an onload event that runs everything appended to onload within this content
                            } else if ((executeContext.addonType == CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml) || (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextRemoteMethodJson)) {
                                //
                                // -- non-ajax/non-Iframe remote method content (no wrapper)
                            } else if (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextEmail) {
                                //
                                // -- return Email context (no wrappers)
                            } else if (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextSimple) {
                                //
                                // -- add-on called by another add-on, subroutine style (no wrappers)
                            } else {
                                //
                                // -- Return all other types, Enable Edit Wrapper for Page Content edit mode
                                hint = "25";
                                bool IncludeEditWrapper = (!addon.blockEditTools) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextEditor) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextEmail) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodJson) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextSimple) && (!executeContext.isIncludeAddon);
                                if (IncludeEditWrapper) {
                                    IncludeEditWrapper = IncludeEditWrapper && (allowAdvanceEditor && ((executeContext.addonType == CPUtilsBaseClass.addonContext.ContextAdmin) || core.session.isEditing(executeContext.hostRecord.contentName)));
                                    if (IncludeEditWrapper) {
                                        //
                                        // Edit Icon
                                        var editSegmentList = new List<string>();
                                        editSegmentList.Add(AdminUIController.getAddonEditSegment(core, addon.id, addon.name));
                                        result = AdminUIController.getAddonEditLink(core, editSegmentList) + result;
                                        result = AdminUIController.getEditWrapper(core, result);
                                        //string EditWrapperHTMLID = "eWrapper" + core.doc.addonInstanceCnt;
                                        //string DialogList = "";
                                        //string HelpIcon = getHelpBubble(addon.id, addon.help, addon.collectionID, ref DialogList);
                                        //if (core.visitProperty.getBoolean("AllowAdvancedEditor")) {
                                        //    string addonArgumentListPassToBubbleEditor = ""; // comes from method in this class the generates it from addon and instance properites - lost it in the shuffle
                                        //    string AddonEditIcon = getIconSprite("", 0, "https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/images/tooledit.png", 22, 22, "Edit the " + addon.name + " Add-on", "Edit the " + addon.name + " Add-on", "", true, "");
                                        //    AddonEditIcon = "<a href=\"/" + core.appConfig.adminRoute + "?cid=" + Models.Domain.ContentMetadataModel.getContentId(core, AddonModel.contentName) + "&id=" + addon.id + "&af=4&aa=2&ad=1\" tabindex=\"-1\">" + AddonEditIcon + "</a>";
                                        //    string InstanceSettingsEditIcon = getInstanceBubble(addon.name, addonArgumentListPassToBubbleEditor, executeContext.hostRecord.contentName, executeContext.hostRecord.recordId, executeContext.hostRecord.fieldName, executeContext.instanceGuid, executeContext.addonType, ref DialogList);
                                        //    string HTMLViewerEditIcon = getHTMLViewerBubble(addon.id, "editWrapper" + core.doc.editWrapperCnt, ref DialogList);
                                        //    string SiteStylesEditIcon = ""; // ?????
                                        //    string ToolBar = InstanceSettingsEditIcon + AddonEditIcon + getAddonStylesBubble(addon.id, ref DialogList) + SiteStylesEditIcon + HTMLViewerEditIcon + HelpIcon;
                                        //    ToolBar = GenericController.vbReplace(ToolBar, "&nbsp;", "", 1, 99, 1);
                                        //    result = AdminUIController.getEditWrapper(core, "<div class=\"ccAddonEditTools\">" + ToolBar + "&nbsp;" + addon.name + DialogList + "</div>", result);
                                        //} else if (core.visitProperty.getBoolean("AllowEditing")) {
                                        //    result = AdminUIController.getEditWrapper(core, "<div class=\"ccAddonEditCaption\">" + addon.name + "&nbsp;" + HelpIcon + "</div>", result);
                                        //}
                                    }
                                }
                                //
                                // -- Add Comment wrapper, to help debugging except email, remote methods and admin (empty is used to detect no result)
                                hint = "26";
                                if (true && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextAdmin) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextEmail) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodJson) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextSimple)) {
                                    if (core.visitProperty.getBoolean("AllowDebugging")) {
                                        string AddonCommentName = GenericController.vbReplace(addon.name, "-->", "..>");
                                        if (addon.isInline) {
                                            result = "<!-- Add-on " + AddonCommentName + " -->" + result + "<!-- /Add-on " + AddonCommentName + " -->";
                                        } else {
                                            result = "\r<!-- Add-on " + AddonCommentName + " -->" + nop(result) + "\r<!-- /Add-on " + AddonCommentName + " -->";
                                        }
                                    }
                                }
                                //
                                // -- Add Design Wrapper
                                hint = "27";
                                if ((!string.IsNullOrEmpty(result)) && (!addon.isInline) && (executeContext.wrapperID > 0)) {
                                    result = addWrapperToResult(result, executeContext.wrapperID, "for Add-on " + addon.name);
                                }
                                // -- restore the parent's instanceId
                                hint = "28";
                                core.docProperties.setProperty("instanceId", parentInstanceId);
                            }
                            //
                            // -- unwind recursion count
                            hint = "29";
                            if (core.doc.addonRecursionDepth.ContainsKey(addon.id)) {
                                if (--core.doc.addonRecursionDepth[addon.id] <= 0) {
                                    core.doc.addonRecursionDepth.Remove(addon.id);
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    LogController.logError(core, ex, "hint [" + hint + "]");
                } finally {
                    //
                    // -- this completes the execute of this core.addon. remove it from the 'running' list
                    core.doc.addonInstanceCnt = core.doc.addonInstanceCnt + 1;
                    //
                    // -- restore the forceJavascriptToHead value of the caller
                    executeContext.forceJavascriptToHead = save_forceJavascriptToHead;
                    //
                    // -- if root level addon, and the addon is an html document, create the html document around it and uglify if not debugging
                    if ((executeContext.forceHtmlDocument) || ((rootLevelAddon) && (addon.htmlDocument))) {
                        result = core.html.getHtmlDoc(result, "<body>");
                        if ((!core.doc.visitPropertyAllowDebugging) && (core.siteProperties.getBoolean("Allow Html Minify", true))) {
                            result = NUglify.Uglify.Html(result).Code;
                        }
                    }
                    //
                    // -- pop modelstack and test point message
                    core.doc.addonModelStack.Pop();
                }
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute the xml part of an addon, return html
        /// </summary>
        /// <param name="nothingObject"></param>
        /// <param name="FormXML"></param>
        /// <param name="return_ExitAddonBlankWithResponse"></param>
        /// <returns></returns>
        private string execute_formContent(object nothingObject, string FormXML, ref bool return_ExitAddonBlankWithResponse, string contextErrorMessage) {
            string result = "";
            try {
                string Button = core.docProperties.getText(RequestNameButton);
                StringBuilderLegacyController Content = new StringBuilderLegacyController();
                string ButtonList = "";
                string Name = "";
                string Description = "";
                if (Button == ButtonCancel) {
                    //
                    // Cancel just exits with no content
                    return_ExitAddonBlankWithResponse = true;
                    return string.Empty;
                } else if (!core.session.isAuthenticatedAdmin()) {
                    //
                    // Not Admin Error
                    ButtonList = ButtonCancel;
                    Content.Add(AdminUIController.getFormBodyAdminOnly());
                } else {
                    {
                        bool loadOK = true;
                        XmlDocument Doc = new XmlDocument();
                        try {
                            Doc.LoadXml(FormXML);
                        } catch (Exception) {
                            ButtonList = ButtonCancel;
                            Content.Add("<div class=\"ccError\" style=\"margin:10px;padding:10px;background-color:white;\">There was a problem with the Setting Page you requested.</div>");
                            loadOK = false;
                        }
                        if (loadOK) {
                            //
                            // data is OK
                            //
                            if (GenericController.vbLCase(Doc.DocumentElement.Name) != "form") {
                                //
                                // error - Need a way to reach the user that submitted the file
                                //
                                ButtonList = ButtonCancel;
                                Content.Add("<div class=\"ccError\" style=\"margin:10px;padding:10px;background-color:white;\">There was a problem with the Setting Page you requested.</div>");
                            } else {
                                // todo - move locals
                                string fieldfilename = "";
                                string FieldValue = "";
                                bool IsFound = false;
                                string FieldName = "";
                                bool FieldReadOnly = false;
                                bool FieldHTML = false;
                                string fieldType = "";
                                //
                                // ----- Process Requests
                                //
                                if ((Button == ButtonSave) || (Button == ButtonOK)) {
                                    foreach (XmlNode SettingNode in Doc.DocumentElement.ChildNodes) {
                                        if (SettingNode.Name.ToLower() == "tab") {
                                            foreach (XmlNode TabNode in SettingNode.ChildNodes) {
                                                string Filename = null;
                                                string DefaultFilename = null;
                                                switch (TabNode.Name.ToLower()) {
                                                    case "siteproperty":
                                                        //
                                                        FieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                        FieldValue = core.docProperties.getText(FieldName);
                                                        fieldType = xml_GetAttribute(IsFound, TabNode, "type", "");
                                                        switch (GenericController.vbLCase(fieldType)) {
                                                            case "integer":
                                                                //
                                                                if (!string.IsNullOrEmpty(FieldValue)) {
                                                                    FieldValue = GenericController.encodeInteger(FieldValue).ToString();
                                                                }
                                                                core.siteProperties.setProperty(FieldName, FieldValue);
                                                                break;
                                                            case "boolean":
                                                                //
                                                                if (!string.IsNullOrEmpty(FieldValue)) {
                                                                    FieldValue = GenericController.encodeBoolean(FieldValue).ToString();
                                                                }
                                                                core.siteProperties.setProperty(FieldName, FieldValue);
                                                                break;
                                                            case "float":
                                                                //
                                                                if (!string.IsNullOrEmpty(FieldValue)) {
                                                                    FieldValue = encodeNumber(FieldValue).ToString();
                                                                }
                                                                core.siteProperties.setProperty(FieldName, FieldValue);
                                                                break;
                                                            case "date":
                                                                //
                                                                if (!string.IsNullOrEmpty(FieldValue)) {
                                                                    FieldValue = GenericController.encodeDate(FieldValue).ToString();
                                                                }
                                                                core.siteProperties.setProperty(FieldName, FieldValue);
                                                                break;
                                                            case "file":
                                                            case "imagefile":
                                                                //
                                                                if (core.docProperties.getBoolean(FieldName + ".DeleteFlag")) {
                                                                    core.siteProperties.setProperty(FieldName, "");
                                                                }
                                                                if (!string.IsNullOrEmpty(FieldValue)) {
                                                                    Filename = FieldValue;
                                                                    string VirtualFilePath = "Settings/" + FieldName + "/";
                                                                    core.cdnFiles.upload(FieldName, VirtualFilePath, ref Filename);
                                                                    core.siteProperties.setProperty(FieldName, VirtualFilePath + Filename);
                                                                }
                                                                break;
                                                            case "textfile":
                                                                //
                                                                DefaultFilename = "Settings/" + FieldName + ".txt";
                                                                Filename = core.siteProperties.getText(FieldName, DefaultFilename);
                                                                if (string.IsNullOrEmpty(Filename)) {
                                                                    Filename = DefaultFilename;
                                                                    core.siteProperties.setProperty(FieldName, DefaultFilename);
                                                                }
                                                                core.wwwFiles.saveFile(Filename, FieldValue);
                                                                break;
                                                            case "cssfile":
                                                                //
                                                                DefaultFilename = "Settings/" + FieldName + ".css";
                                                                Filename = core.siteProperties.getText(FieldName, DefaultFilename);
                                                                if (string.IsNullOrEmpty(Filename)) {
                                                                    Filename = DefaultFilename;
                                                                    core.siteProperties.setProperty(FieldName, DefaultFilename);
                                                                }
                                                                core.wwwFiles.saveFile(Filename, FieldValue);
                                                                break;
                                                            case "xmlfile":
                                                                //
                                                                DefaultFilename = "Settings/" + FieldName + ".xml";
                                                                Filename = core.siteProperties.getText(FieldName, DefaultFilename);
                                                                if (string.IsNullOrEmpty(Filename)) {
                                                                    Filename = DefaultFilename;
                                                                    core.siteProperties.setProperty(FieldName, DefaultFilename);
                                                                }
                                                                core.wwwFiles.saveFile(Filename, FieldValue);
                                                                break;
                                                            case "currency":
                                                                //
                                                                if (!string.IsNullOrEmpty(FieldValue)) {
                                                                    FieldValue = encodeNumber(FieldValue).ToString();
                                                                    FieldValue = String.Format("C", FieldValue);
                                                                }
                                                                core.siteProperties.setProperty(FieldName, FieldValue);
                                                                break;
                                                            case "link":
                                                                core.siteProperties.setProperty(FieldName, FieldValue);
                                                                break;
                                                            default:
                                                                core.siteProperties.setProperty(FieldName, FieldValue);
                                                                break;
                                                        }
                                                        break;
                                                    case "copycontent":
                                                        //
                                                        // A Copy Content block
                                                        //
                                                        FieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                        if (!FieldReadOnly) {
                                                            FieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                            FieldHTML = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "html", "false"));
                                                            if (FieldHTML) {
                                                                //
                                                                // treat html as active content for now.
                                                                //
                                                                FieldValue = core.docProperties.getRenderedActiveContent(FieldName);
                                                            } else {
                                                                FieldValue = core.docProperties.getText(FieldName);
                                                            }
                                                            using (var csData = new CsModel(core)) {
                                                                csData.open("Copy Content", "name=" + DbController.encodeSQLText(FieldName), "ID");
                                                                if (!csData.ok()) {
                                                                    csData.close();
                                                                    csData.insert("Copy Content");
                                                                }
                                                                if (csData.ok()) {
                                                                    csData.set("name", FieldName);
                                                                    //
                                                                    // Set copy
                                                                    //
                                                                    csData.set("copy", FieldValue);
                                                                    //
                                                                    // delete duplicates
                                                                    //
                                                                    csData.goNext();
                                                                    while (csData.ok()) {
                                                                        csData.deleteRecord();
                                                                        csData.goNext();
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        break;
                                                    case "filecontent":
                                                        //
                                                        // A File Content block
                                                        //
                                                        FieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                        if (!FieldReadOnly) {
                                                            FieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                            fieldfilename = xml_GetAttribute(IsFound, TabNode, "filename", "");
                                                            FieldValue = core.docProperties.getText(FieldName);
                                                            core.cdnFiles.saveFile(fieldfilename, FieldValue);
                                                        }
                                                        break;
                                                    case "dbquery":
                                                        //
                                                        // dbquery has no results to process
                                                        //
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (Button == ButtonOK) {
                                    //
                                    // Exit on OK or cancel
                                    //
                                    return_ExitAddonBlankWithResponse = true;
                                    return string.Empty;
                                }
                                //
                                // ----- Display Form
                                //
                                //Content.Add(AdminUIController.editTableOpen);
                                Name = xml_GetAttribute(IsFound, Doc.DocumentElement, "name", "");
                                int TabCnt = 0;
                                var adminMenu = new TabController();
                                foreach (XmlNode SettingNode in Doc.DocumentElement.ChildNodes) {
                                    string Copy = "";
                                    string TabName = "";
                                    string TabDescription = "";
                                    string TabHeading = "";
                                    StringBuilderLegacyController TabCell = null;
                                    switch (GenericController.vbLCase(SettingNode.Name)) {
                                        case "description":
                                            Description = SettingNode.InnerText;
                                            break;
                                        case "tab":
                                            TabCnt += 1;
                                            TabName = xml_GetAttribute(IsFound, SettingNode, "name", "");
                                            TabDescription = xml_GetAttribute(IsFound, SettingNode, "description", "");
                                            TabHeading = xml_GetAttribute(IsFound, SettingNode, "heading", "");
                                            TabCell = new StringBuilderLegacyController();
                                            foreach (XmlNode TabNode in SettingNode.ChildNodes) {
                                                int SQLPageSize = 0;
                                                int ErrorNumber = 0;
                                                string FieldDataSource = null;
                                                string FieldSQL = null;
                                                string FieldDescription = null;
                                                string FieldDefaultValue = null;
                                                string FieldCaption = null;
                                                switch (GenericController.vbLCase(TabNode.Name)) {
                                                    case "heading":
                                                        //
                                                        // Heading
                                                        FieldCaption = xml_GetAttribute(IsFound, TabNode, "caption", "");
                                                        TabCell.Add(AdminUIController.getEditSubheadRow(core, FieldCaption));
                                                        break;
                                                    case "siteproperty":
                                                        //
                                                        // Site property
                                                        FieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                        if (!string.IsNullOrEmpty(FieldName)) {
                                                            FieldCaption = xml_GetAttribute(IsFound, TabNode, "caption", "");
                                                            if (string.IsNullOrEmpty(FieldCaption)) {
                                                                FieldCaption = FieldName;
                                                            }
                                                            FieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                            FieldHTML = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "html", ""));
                                                            fieldType = xml_GetAttribute(IsFound, TabNode, "type", "");
                                                            string FieldSelector = xml_GetAttribute(IsFound, TabNode, "selector", "");
                                                            FieldDescription = xml_GetAttribute(IsFound, TabNode, "description", "");
                                                            string FieldAddon = xml_GetAttribute(IsFound, TabNode, "EditorAddon", "");
                                                            FieldDefaultValue = TabNode.InnerText;
                                                            FieldValue = core.siteProperties.getText(FieldName, FieldDefaultValue);
                                                            if (!string.IsNullOrEmpty(FieldAddon)) {
                                                                //
                                                                // Use Editor Addon
                                                                Dictionary<string, string> arguments = new Dictionary<string, string> {
                                                                    { "FieldName", FieldName },
                                                                    { "FieldValue", core.siteProperties.getText(FieldName, FieldDefaultValue) }
                                                                };
                                                                AddonModel addon = DbBaseModel.createByUniqueName<AddonModel>(core.cpParent, FieldAddon);
                                                                Copy = core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext() {
                                                                    addonType = CPUtilsBaseClass.addonContext.ContextAdmin,
                                                                    argumentKeyValuePairs = arguments,
                                                                    errorContextMessage = "executing field addon [" + FieldAddon + "] for " + contextErrorMessage
                                                                });
                                                            } else if (!string.IsNullOrEmpty(FieldSelector)) {
                                                                //
                                                                // Use Selector
                                                                Copy = AdminUIController.getDefaultEditor_SelectorString(core, FieldName, FieldValue, FieldSelector);
                                                            } else {
                                                                //
                                                                // Use default editor for each field type
                                                                switch (GenericController.vbLCase(fieldType)) {
                                                                    case "integer":
                                                                        //
                                                                        Copy = AdminUIController.getDefaultEditor_text(core, FieldName, FieldValue, FieldReadOnly);
                                                                        break;
                                                                    case "boolean":
                                                                        Copy = AdminUIController.getDefaultEditor_bool(core, FieldName, GenericController.encodeBoolean(FieldValue), FieldReadOnly);
                                                                        break;
                                                                    case "float":
                                                                        Copy = AdminUIController.getDefaultEditor_text(core, FieldName, FieldValue, FieldReadOnly);
                                                                        break;
                                                                    case "date":
                                                                        Copy = AdminUIController.getDefaultEditor_dateTime(core, FieldName, encodeDate(FieldValue), FieldReadOnly);
                                                                        break;
                                                                    case "file":
                                                                    case "imagefile":
                                                                        if (FieldReadOnly) {
                                                                            Copy = FieldValue + HtmlController.inputHidden(FieldName, FieldValue);
                                                                        } else {
                                                                            if (string.IsNullOrEmpty(FieldValue)) {
                                                                                Copy = core.html.inputFile(FieldName);
                                                                            } else {
                                                                                string NonEncodedLink = GenericController.getCdnFileLink(core, FieldValue);
                                                                                //NonEncodedLink = core.webServer.requestDomain + genericController.getCdnFileLink(core, FieldValue);
                                                                                string EncodedLink = encodeURL(NonEncodedLink);
                                                                                string FieldValuefilename = "";
                                                                                string FieldValuePath = "";
                                                                                core.privateFiles.splitDosPathFilename(FieldValue, ref FieldValuePath, ref FieldValuefilename);
                                                                                Copy = ""
                                                                                + "<a href=\"http://" + EncodedLink + "\" target=\"_blank\">[" + FieldValuefilename + "]</A>"
                                                                                + "&nbsp;&nbsp;&nbsp;Delete:&nbsp;" + HtmlController.checkbox(FieldName + ".DeleteFlag", false) + "&nbsp;&nbsp;&nbsp;Change:&nbsp;" + core.html.inputFile(FieldName);
                                                                            }
                                                                        }
                                                                        break;
                                                                    case "currency":
                                                                        //
                                                                        if (!string.IsNullOrEmpty(FieldValue)) {
                                                                            FieldValue = String.Format("C", FieldValue);
                                                                        }
                                                                        Copy = AdminUIController.getDefaultEditor_text(core, FieldName, FieldValue, FieldReadOnly);
                                                                        break;
                                                                    case "textfile":
                                                                        if (FieldReadOnly) {
                                                                            Copy = FieldValue + HtmlController.inputHidden(FieldName, FieldValue);
                                                                        } else {
                                                                            Copy = AdminUIController.getDefaultEditor_TextArea(core, FieldName, FieldValue, FieldReadOnly);
                                                                        }
                                                                        break;
                                                                    case "cssfile":
                                                                        Copy = AdminUIController.getDefaultEditor_TextArea(core, FieldName, FieldValue, FieldReadOnly);
                                                                        break;
                                                                    case "xmlfile":
                                                                        if (FieldReadOnly) {
                                                                            Copy = FieldValue + HtmlController.inputHidden(FieldName, FieldValue);
                                                                        } else {
                                                                            Copy = HtmlController.inputTextarea(core, FieldName, FieldValue, 5);
                                                                        }
                                                                        break;
                                                                    case "link":
                                                                        if (FieldReadOnly) {
                                                                            Copy = FieldValue + HtmlController.inputHidden(FieldName, FieldValue);
                                                                        } else {
                                                                            Copy = HtmlController.inputText_Legacy(core, FieldName, FieldValue);
                                                                        }
                                                                        break;
                                                                    default:
                                                                        //
                                                                        // text
                                                                        if (FieldHTML) {
                                                                            Copy = AdminUIController.getDefaultEditor_Html(core, FieldName, FieldValue, "", "", "", FieldReadOnly);
                                                                        } else {
                                                                            Copy = AdminUIController.getDefaultEditor_text(core, FieldName, FieldValue, FieldReadOnly);
                                                                        }
                                                                        break;
                                                                }
                                                            }
                                                            TabCell.Add(AdminUIController.getEditRowLegacy(core, Copy, FieldCaption, FieldDescription, false, false, ""));
                                                        }
                                                        break;
                                                    case "copycontent":
                                                        //
                                                        // Content Copy field
                                                        FieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                        if (!string.IsNullOrEmpty(FieldName)) {
                                                            FieldCaption = xml_GetAttribute(IsFound, TabNode, "caption", "");
                                                            if (string.IsNullOrEmpty(FieldCaption)) {
                                                                FieldCaption = FieldName;
                                                            }
                                                            FieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                            FieldDescription = xml_GetAttribute(IsFound, TabNode, "description", "");
                                                            FieldHTML = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "html", ""));
                                                            //
                                                            using (var csData = new CsModel(core)) {
                                                                csData.open("Copy Content", "Name=" + DbController.encodeSQLText(FieldName), "ID", false, 0, "id,name,Copy");
                                                                if (!csData.ok()) {
                                                                    csData.close();
                                                                    csData.insert("Copy Content");
                                                                    if (csData.ok()) {
                                                                        int RecordID = csData.getInteger("ID");
                                                                        csData.set("name", FieldName);
                                                                        csData.set("copy", GenericController.encodeText(TabNode.InnerText));
                                                                        csData.save();
                                                                    }
                                                                }
                                                                if (csData.ok()) {
                                                                    FieldValue = csData.getText("copy");
                                                                }
                                                            }
                                                            if (FieldHTML) {
                                                                Copy = AdminUIController.getDefaultEditor_Html(core, FieldName, FieldValue, "", "", "", FieldReadOnly);
                                                            } else {
                                                                Copy = AdminUIController.getDefaultEditor_text(core, FieldName, FieldValue, FieldReadOnly);
                                                            }
                                                            TabCell.Add(AdminUIController.getEditRowLegacy(core, Copy, FieldCaption, FieldDescription, false, false, ""));
                                                        }
                                                        break;
                                                    case "filecontent":
                                                        //
                                                        // Content from a flat file
                                                        //
                                                        FieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                        FieldCaption = xml_GetAttribute(IsFound, TabNode, "caption", "");
                                                        fieldfilename = xml_GetAttribute(IsFound, TabNode, "filename", "");
                                                        FieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                        FieldDescription = xml_GetAttribute(IsFound, TabNode, "description", "");
                                                        FieldDefaultValue = TabNode.InnerText;
                                                        FieldValue = FieldDefaultValue;
                                                        FieldHTML = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "html", ""));
                                                        if (!string.IsNullOrEmpty(fieldfilename)) {
                                                            if (core.cdnFiles.fileExists(fieldfilename)) {
                                                                FieldValue = core.cdnFiles.readFileText(fieldfilename);
                                                            }
                                                        }
                                                        if (FieldHTML) {
                                                            Copy = AdminUIController.getDefaultEditor_Html(core, FieldName, FieldValue, "", "", "", FieldReadOnly);
                                                            //Copy = core.html.getFormInputHTML(FieldName, FieldValue);
                                                        } else {
                                                            Copy = AdminUIController.getDefaultEditor_TextArea(core, FieldName, FieldValue, FieldReadOnly);
                                                            //Copy = htmlController.inputText(core, FieldName, FieldValue);
                                                        }
                                                        TabCell.Add(AdminUIController.getEditRowLegacy(core, Copy, FieldCaption, FieldDescription, false, false, ""));
                                                        break;
                                                    case "dbquery":
                                                    case "querydb":
                                                    case "query":
                                                    case "db":
                                                        //
                                                        // Display the output of a query
                                                        //
                                                        Copy = "";
                                                        FieldDataSource = xml_GetAttribute(IsFound, TabNode, "DataSourceName", "");
                                                        FieldSQL = TabNode.InnerText;
                                                        FieldCaption = xml_GetAttribute(IsFound, TabNode, "caption", "");
                                                        FieldDescription = xml_GetAttribute(IsFound, TabNode, "description", "");
                                                        SQLPageSize = GenericController.encodeInteger(xml_GetAttribute(IsFound, TabNode, "rowmax", ""));
                                                        if (SQLPageSize == 0) {
                                                            SQLPageSize = 100;
                                                        }
                                                        //
                                                        // Run the SQL
                                                        //
                                                        DataTable dt = null;
                                                        if (!string.IsNullOrEmpty(FieldSQL)) {
                                                            try {
                                                                using (var db = new DbController(core, FieldDataSource)) {
                                                                    dt = core.db.executeQuery(FieldSQL, 0, SQLPageSize);
                                                                }
                                                            } catch (Exception) {
                                                                ErrorNumber = 0;
                                                                loadOK = false;
                                                            }
                                                        }
                                                        if (dt != null) {
                                                            if (string.IsNullOrEmpty(FieldSQL)) {
                                                                //
                                                                // ----- Error
                                                                //
                                                                Copy = "No Result";
                                                            } else if (ErrorNumber != 0) {
                                                                //
                                                                // ----- Error
                                                                Copy = "Error: ";
                                                            } else if (!DbController.isDataTableOk(dt)) {
                                                                //
                                                                // ----- no result
                                                                //
                                                                Copy = "No Results";
                                                            } else if (dt.Rows.Count == 0) {
                                                                //
                                                                // ----- no result
                                                                //
                                                                Copy = "No Results";
                                                            } else {
                                                                //
                                                                // ----- print results
                                                                //
                                                                if (dt.Rows.Count > 0) {
                                                                    object[,] something = { { } };
                                                                    if (dt.Rows.Count == 1 && dt.Columns.Count == 1) {
                                                                        Copy = HtmlController.inputText_Legacy(core, "result", GenericController.encodeText(something[0, 0]), 0, 0, "", false, true);
                                                                    } else {
                                                                        foreach (DataRow dr in dt.Rows) {
                                                                            //
                                                                            //Const LoginMode_None = 1
                                                                            //Const LoginMode_AutoRecognize = 2
                                                                            //Const LoginMode_AutoLogin = 3
                                                                            //
                                                                            // Build headers
                                                                            //
                                                                            int FieldCount = dr.ItemArray.Length;
                                                                            Copy += ("\r<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"border-bottom:1px solid #444;border-right:1px solid #444;background-color:white;color:#444;\">");
                                                                            Copy += ("\r\t<tr>");
                                                                            foreach (DataColumn dc in dr.ItemArray) {
                                                                                Copy += ("\r\t\t<td class=\"ccadminsmall\" style=\"border-top:1px solid #444;border-left:1px solid #444;color:black;padding:2px;padding-top:4px;padding-bottom:4px;\">" + dr[dc].ToString() + "</td>");
                                                                            }
                                                                            Copy += ("\r\t</tr>");
                                                                            //
                                                                            // Build output table
                                                                            //
                                                                            string RowStart = null;
                                                                            string RowEnd = null;
                                                                            string ColumnStart = null;
                                                                            string ColumnEnd = null;
                                                                            RowStart = "\r\t<tr>";
                                                                            RowEnd = "\r\t</tr>";
                                                                            ColumnStart = "\r\t\t<td class=\"ccadminnormal\" style=\"border-top:1px solid #444;border-left:1px solid #444;background-color:white;color:#444;padding:2px\">";
                                                                            ColumnEnd = "</td>";
                                                                            int RowPointer = 0;
                                                                            int RowMax = 0;
                                                                            for (RowPointer = 0; RowPointer <= RowMax; RowPointer++) {
                                                                                Copy += (RowStart);
                                                                                int ColumnPointer = 0;
                                                                                int ColumnMax = 0;
                                                                                for (ColumnPointer = 0; ColumnPointer <= ColumnMax; ColumnPointer++) {
                                                                                    object CellData = something[ColumnPointer, RowPointer];
                                                                                    if (IsNull(CellData)) {
                                                                                        Copy += (ColumnStart + "[null]" + ColumnEnd);
                                                                                    } else if ((CellData == null)) {
                                                                                        Copy += (ColumnStart + "[empty]" + ColumnEnd);
                                                                                    } else if (Microsoft.VisualBasic.Information.IsArray(CellData)) {
                                                                                        Copy += ColumnStart + "[array]";
                                                                                        //Dim Cnt As Integer
                                                                                        //Cnt = UBound(CellData)
                                                                                        //Dim Ptr As Integer
                                                                                        //For Ptr = 0 To Cnt - 1
                                                                                        //    Copy = Copy & ("<br>(" & Ptr & ")&nbsp;[" & CellData[Ptr] & "]")
                                                                                        //Next
                                                                                        //Copy = Copy & (ColumnEnd)
                                                                                    } else if (GenericController.encodeText(CellData) == "") {
                                                                                        Copy += (ColumnStart + "[empty]" + ColumnEnd);
                                                                                    } else {
                                                                                        Copy += (ColumnStart + HtmlController.encodeHtml(GenericController.encodeText(CellData)) + ColumnEnd);
                                                                                    }
                                                                                }
                                                                                Copy += (RowEnd);
                                                                            }
                                                                            Copy += ("\r</table>");
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        TabCell.Add(AdminUIController.getEditRow(core, Copy, FieldCaption, FieldDescription, false, false, ""));
                                                        //TabCell.Add(adminUIController.getEditRowLegacy(core, Copy, FieldCaption, FieldDescription, false, false, ""));
                                                        break;
                                                }
                                            }
                                            Copy = AdminUIController.getEditPanel(core, true, TabHeading, TabDescription, AdminUIController.editTable(TabCell.Text));
                                            if (!string.IsNullOrEmpty(Copy)) {
                                                adminMenu.addEntry(TabName.Replace(" ", "&nbsp;"), Copy, "ccAdminTab");
                                            }
                                            //Content.Add( GetForm_Edit_AddTab(core,TabName, Copy, True))
                                            TabCell = null;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                //
                                // Buttons
                                //
                                ButtonList = ButtonCancel + "," + ButtonSave + "," + ButtonOK;
                                if (TabCnt > 0) { Content.Add(adminMenu.getTabs(core)); }
                            }
                        }
                    }
                }
                //
                result = AdminUIController.getToolBody(core, Name, ButtonList, "", true, true, Description, "", 0, Content.Text);
                Content = null;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute vb script
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="Code"></param>
        /// <param name="EntryPoint"></param>
        /// <param name="ScriptingTimeout"></param>
        /// <param name="ScriptName"></param>
        /// <returns></returns>
        private string execute_Script_VBScript(ref AddonModel addon) {
            string returnText = "";
            try {
                // todo - move locals
                var engine = new Microsoft.ClearScript.Windows.VBScriptEngine();
                string[] Args = { };
                string WorkingCode = addon.scriptingCode;
                string entryPoint = addon.scriptingEntryPoint;
                if (string.IsNullOrEmpty(entryPoint)) {
                    //
                    // -- compatibility mode, if no entry point given, if the code starts with "function myFuncton()" and add "call myFunction()"
                    int pos = WorkingCode.IndexOf("function", StringComparison.CurrentCultureIgnoreCase);
                    if (pos >= 0) {
                        entryPoint = WorkingCode.Substring(pos + 9);
                        pos = entryPoint.IndexOf("\r");
                        if (pos > 0) {
                            entryPoint = entryPoint.Substring(0, pos);
                        }
                        pos = entryPoint.IndexOf("\n");
                        if (pos > 0) {
                            entryPoint = entryPoint.Substring(0, pos);
                        }
                        pos = entryPoint.IndexOf("(");
                        if (pos > 0) {
                            entryPoint = entryPoint.Substring(0, pos);
                        }
                    }
                } else {
                    //
                    // -- etnry point provided, remove "()" if included and add to code
                    int pos = entryPoint.IndexOf("(");
                    if (pos > 0) {
                        entryPoint = entryPoint.Substring(0, pos);
                    }
                }
                try {
                    mainCsvScriptCompatibilityClass mainCsv = new mainCsvScriptCompatibilityClass(core);
                    engine.AddHostObject("ccLib", mainCsv);
                } catch (Exception) {
                    throw;
                }
                try {
                    engine.AddHostObject("cp", core.cpParent);
                } catch (Exception) {
                    throw;
                }
                try {
                    engine.Execute(WorkingCode);
                    object returnObj = engine.Evaluate(entryPoint);
                    if (returnObj != null) {
                        if (returnObj.GetType() == typeof(String)) {
                            returnText = (String)returnObj;
                        }
                    }
                } catch (Exception ex) {
                    string addonDescription = getAddonDescription(core, addon);
                    string errorMessage = "Error executing addon script, " + addonDescription;
                    throw new GenericException(errorMessage, ex);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnText;
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute jscript script
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="Code"></param>
        /// <param name="EntryPoint"></param>
        /// <param name="ScriptingTimeout"></param>
        /// <param name="ScriptName"></param>
        /// <returns></returns>
        private string execute_Script_JScript(ref AddonModel addon) {
            string returnText = "";
            try {
                // todo - move locals
                var engine = new Microsoft.ClearScript.Windows.JScriptEngine();
                string[] Args = { };
                string WorkingCode = addon.scriptingCode;
                //
                string entryPoint = addon.scriptingEntryPoint;
                if (string.IsNullOrEmpty(entryPoint)) {
                    //
                    // -- compatibility mode, if no entry point given, if the code starts with "function myFuncton()" and add "call myFunction()"
                    int pos = WorkingCode.IndexOf("function", StringComparison.CurrentCultureIgnoreCase);
                    if (pos >= 0) {
                        entryPoint = WorkingCode.Substring(pos + 9);
                        pos = entryPoint.IndexOf("\r");
                        if (pos > 0) {
                            entryPoint = entryPoint.Substring(0, pos);
                        }
                        pos = entryPoint.IndexOf("\n");
                        if (pos > 0) {
                            entryPoint = entryPoint.Substring(0, pos);
                        }
                        pos = entryPoint.IndexOf("(");
                        if (pos > 0) {
                            entryPoint = entryPoint.Substring(0, pos);
                        }
                    }
                } else {
                    //
                    // -- etnry point provided, remove "()" if included and add to code
                    int pos = entryPoint.IndexOf("(");
                    if (pos > 0) {
                        entryPoint = entryPoint.Substring(0, pos);
                    }
                }
                try {
                    mainCsvScriptCompatibilityClass mainCsv = new mainCsvScriptCompatibilityClass(core);
                    engine.AddHostObject("ccLib", mainCsv);
                } catch (Exception) {
                    throw;
                }
                try {
                    engine.AddHostObject("cp", core.cpParent);
                } catch (Exception) {
                    throw;
                }
                try {
                    engine.Execute(WorkingCode);
                    object returnObj = engine.Evaluate(entryPoint);
                    //object returnObj = engine.Evaluate(entryPoint);
                    if (returnObj != null) {
                        returnText = returnObj.ToString();
                        //if (returnObj.GetType() == typeof(String)) {
                        //    returnText = (String)returnObj;
                        //}
                    }
                } catch (Exception ex) {
                    string addonDescription = getAddonDescription(core, addon);
                    string errorMessage = "Error executing addon script, " + addonDescription;
                    throw new GenericException(errorMessage, ex);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnText;
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute dotnet code
        /// </summary>
        /// <param name="executeContext"></param>
        /// <param name="addon"></param>
        /// <param name="addonCollection"></param>
        /// <returns></returns>
        private string execute_dotNetClass(CPUtilsBaseClass.addonExecuteContext executeContext, AddonModel addon, AddonCollectionModel addonCollection) {
            string result = "";
            try {
                LogController.logTrace(core, "execute_assembly dotNetClass [" + addon.dotNetClass + "], enter");
                //
                // -- purpose is to have a repository where addons can be stored for web and non-web apps, and allow permissions to be installed with online upload
                string warningMessage = "The addon [" + addon.name + "] dotnet code could not be executed because no assembly was found with namespace [" + addon.dotNetClass + "].";
                if ((addonCollection == null) || (string.IsNullOrEmpty(addonCollection.ccguid))) { throw new GenericException(warningMessage + " The addon dotnet assembly could not be run because no collection is set, or the collection guid is empty."); }
                //
                // -- has addon been found before
                string assemblyFileDictKey = (addonCollection.ccguid + addon.dotNetClass).ToLower();
                if (core.assemblyList_AddonsFound.ContainsKey(assemblyFileDictKey)) {
                    return execute_dotNetClass_assembly(addon, core.assemblyList_AddonsFound[assemblyFileDictKey].pathFilename);
                }
                //
                // -- try to find addon in current executing path (built in addons)
                bool AddonFound = false;
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                string appPath = Path.GetDirectoryName(path);
                result = execute_dotNetClass_byPath(addon, assemblyFileDictKey, appPath, true, ref AddonFound);
                if (AddonFound) { return result; }
                //
                // -- try addon folder
                var collectionFolderConfig = CollectionFolderModel.getCollectionFolderConfig(core, addonCollection.ccguid);
                if (collectionFolderConfig == null) { throw new GenericException(warningMessage + ", not found in application path [" + appPath + "]. The collection path was not checked because the collection [" + addonCollection.name + "] was not found in the \\private\\addons\\Collections.xml file. Try re-installing the collection"); };
                if (string.IsNullOrEmpty(collectionFolderConfig.path)) { throw new GenericException(warningMessage + ", not found in application path [" + appPath + "]. The collection path was not checked because the path for collection [" + addonCollection.name + "] was not valid in the \\private\\addons\\Collections.xml file. Try re-installing the collection"); };
                string AddonPath = core.privateFiles.joinPath(getPrivateFilesAddonPath(), collectionFolderConfig.path);
                if (!core.privateFiles.pathExists_local(AddonPath)) { core.privateFiles.copyPathRemoteToLocal(AddonPath); }
                string appAddonPath = core.privateFiles.joinPath(core.privateFiles.localAbsRootPath, AddonPath);
                result = execute_dotNetClass_byPath(addon, assemblyFileDictKey, appAddonPath, false, ref AddonFound);
                if (!AddonFound) {
                    throw new GenericException(warningMessage + ", not found in application path [" + appPath + "] or collection path [" + appAddonPath + "].");
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            } finally {
                LogController.logTrace(core, "execute_assembly dotNetClass [" + addon.dotNetClass + "], exit");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute an assembly in a path
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="fullPath"></param>
        /// <param name="IsDevAssembliesFolder"></param>
        /// <param name="addonFound"></param>
        /// <returns></returns>
        private string execute_dotNetClass_byPath(AddonModel addon, string assemblyFileDictKey, string fullPath, bool IsDevAssembliesFolder, ref bool addonFound) {
            try {
                addonFound = false;
                if (!Directory.Exists(fullPath)) { return string.Empty; }
                foreach (var testPathFilename in Directory.GetFileSystemEntries(fullPath, "*.dll")) {
                    //
                    // -- tmp test skipping the non-addon list, depend on found-addon list instead
                    //if (core.assemblyList_NonAddonsFound.Contains(testPathFilename)) { continue; }
                    if (!string.IsNullOrEmpty(core.assemblyList_NonAddonsInstalled.Find(x => testPathFilename.ToLower().Right(x.Length) == x))) {
                        //
                        // -- this assembly is a non-addon installed file, block full path
                        core.assemblyList_NonAddonsFound.Add(testPathFilename);
                        continue;
                    };
                    string returnValue = execute_dotNetClass_assembly(addon, testPathFilename, ref addonFound);
                    if (addonFound) {
                        //
                        // -- addon found, save addonsFound list and return the addon result
                        if (!core.assemblyList_AddonsFound.ContainsKey(assemblyFileDictKey)) {
                            core.assemblyList_AddonsFound.Add(assemblyFileDictKey, new AssemblyFileDetails() {
                                pathFilename = testPathFilename,
                                path = ""
                            });
                            core.assemblyList_AddonsFound_save();
                        }
                        return returnValue;
                    }
                }
                return string.Empty;
            } catch (Exception ex) {
                //
                // -- this exception should interrupt the caller
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon assembly
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="assemblyPhysicalPrivatePathname"></param>
        /// <param name="fileIsValidAddonAssembly">The file was a valid assembly, just not the right onw</param>
        /// <param name="addonFound">If found, the search for the assembly can be abandoned</param>
        /// <returns></returns>
        private string execute_dotNetClass_assembly(AddonModel addon, string assemblyPhysicalPrivatePathname, ref bool addonFound) {
            string result = "";
            try {
                Assembly testAssembly = null;
                addonFound = false;
                try {
                    //
                    // -- "once an assembly is loaded into an appdomain, it's there for the life of the appdomain."
                    testAssembly = Assembly.LoadFrom(assemblyPhysicalPrivatePathname);
                } catch (System.BadImageFormatException) {
                    //
                    // -- file is not an assembly, return addonFound false
                    core.assemblyList_NonAddonsFound.Add(assemblyPhysicalPrivatePathname);
                    return string.Empty;
                } catch (Exception ex) {
                    //
                    // -- file is not an assembly, return addonFound false
                    LogController.logInfo(core, "Assembly.LoadFrom failure [" + assemblyPhysicalPrivatePathname + "], ex [" + ex.Message + "]");
                    return string.Empty;
                }
                //
                // -- assembly loaded, it is a proper assembly. Test if it is the one we are looking for (match class + baseclass)
                var typeMap = testAssembly.GetTypes().ToDictionary(t => t.FullName, t => t, StringComparer.OrdinalIgnoreCase);
                if (typeMap.TryGetValue(addon.dotNetClass, out Type addonType)) {
                    if ((addonType.IsPublic) && (!((addonType.Attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract)) && (addonType.BaseType != null)) {
                        //
                        // -- assembly is public, not abstract, based on a base type
                        if (addonType.BaseType.FullName != null) {
                            //
                            // -- assembly has a baseType fullname
                            addonFound = ((addonType.BaseType.FullName.ToLowerInvariant() == "addonbaseclass") || (addonType.BaseType.FullName.ToLowerInvariant() == "contensive.baseclasses.addonbaseclass"));
                        }
                    }
                }
                //
                // -- if not addon found, exit now
                if (!addonFound) { return string.Empty; }
                //
                // -- addon found, execute it
                AddonBaseClass AddonObj = null;
                try {
                    //
                    // -- Create an object from the Assembly
                    AddonObj = (AddonBaseClass)testAssembly.CreateInstance(addonType.FullName);
                } catch (ReflectionTypeLoadException ex) {
                    //
                    // -- exception thrown out of application bin folder when xunit library included -- ignore
                    LogController.logDebug(core, "Assembly ReflectionTypeLoadException, [" + assemblyPhysicalPrivatePathname + "], ex [" + ex.Message + "]");
                    core.assemblyList_NonAddonsFound.Add(assemblyPhysicalPrivatePathname);
                } catch (Exception ex) {
                    //
                    // -- problem loading types
                    LogController.logDebug(core, "Assembly exception, [" + assemblyPhysicalPrivatePathname + "], adding to assemblySkipList, ex [" + ex.Message + "]");
                    core.assemblyList_NonAddonsFound.Add(assemblyPhysicalPrivatePathname);
                    string detailedErrorMessage = "While locating assembly for addon [" + addon.name + "], there was an error loading types for assembly [" + assemblyPhysicalPrivatePathname + "]. This assembly was skipped and should be removed from the folder.";
                    throw new GenericException(detailedErrorMessage);
                }
                try {
                    //
                    // -- Call Execute
                    object AddonObjResult = AddonObj.Execute(core.cpParent);
                    if (AddonObjResult == null) return string.Empty;
                    if (AddonObjResult.GetType().ToString() == "System.String") { return (string)AddonObjResult; }
                    return SerializeObject(AddonObjResult);
                } catch (Exception ex) {
                    //
                    // -- error in the addon
                    LogController.logError(core, ex, "There was an error in the addon [" + addon.name + "]. It could not be executed because there was an error in the addon assembly [" + assemblyPhysicalPrivatePathname + "], in class [" + addonType.FullName.Trim().ToLowerInvariant() + "]. The error was [" + ex.ToString() + "]");
                    //Throw new GenericException(detailedErrorMessage)
                }
            } catch (Exception ex) {
                //
                // -- this exception should interrupt the caller
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================================
        /// <summary>
        /// Execute an addon assembly
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="assemblyPathname"></param>
        /// <returns></returns>
        private string execute_dotNetClass_assembly(AddonModel addon, string assemblyPathname) {
            bool mock1 = false;
            return execute_dotNetClass_assembly(addon, assemblyPathname, ref mock1);
        }
        //
        //====================================================================================================================
        /// <summary>
        /// execute an addon in the task service
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="arguments"></param>
        public void executeAsync(AddonModel addon, Dictionary<string, string> arguments) {
            try {
                if (addon == null) {
                    //
                    // -- addon not found
                    LogController.logError(core, "executeAsync called with null addon model.");
                } else {
                    //
                    // -- build arguments from the execute context on top of docProperties
                    var compositeArgs = new Dictionary<string, string>(arguments);
                    foreach (var key in core.docProperties.getKeyList()) {
                        if (!compositeArgs.ContainsKey(key)) { compositeArgs.Add(key, core.docProperties.getText(key)); }
                    }
                    var cmdDetail = new TaskModel.CmdDetailClass {
                        addonId = addon.id,
                        addonName = addon.name,
                        args = compositeArgs
                    };
                    TaskSchedulerController.addTaskToQueue(core, cmdDetail, false);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex, "executeAsync");
            }
        }
        //
        //====================================================================================================================
        /// <summary>
        /// execute an addon with the default context
        /// </summary>
        /// <param name="addon"></param>
        public void executeAsync(AddonModel addon) => executeAsync(addon, new Dictionary<string, string>());
        //
        //====================================================================================================================
        //
        public void executeAsync(string guid, string OptionString = "") {
            if (string.IsNullOrEmpty(guid)) { throw new ArgumentException("executeAsync called with invalid guid [" + guid + "]"); }
            var addon = DbBaseModel.create<AddonModel>(core.cpParent, guid);
            if (addon == null) { throw new ArgumentException("ExecuteAsync cannot find Addon for guid [" + guid + "]"); }
            executeAsync(addon, convertQSNVAArgumentstoDocPropertiesList(core, OptionString));
        }
        //
        //====================================================================================================================
        //
        public void executeAsyncByName(string name, string OptionString = "") {
            if (string.IsNullOrEmpty(name)) { throw new ArgumentException("executeAsyncByName called with invalid name [" + name + "]"); }
            var addon = DbBaseModel.createByUniqueName<AddonModel>(core.cpParent, name);
            if (addon == null) { throw new ArgumentException("executeAsyncByName cannot find Addon for name [" + name + "]"); }
            executeAsync(addon, convertQSNVAArgumentstoDocPropertiesList(core, OptionString));
        }
        //
        //===================================================================================================
        //   Build AddonOptionLists
        //
        //   On entry:
        //       AddonOptionConstructor = the addon-encoded version of the list that comes from the Addon Record
        //           It is crlf delimited and all escape characters converted
        //       AddonOptionString = addonencoded version of the list that comes from the HTML AC tag
        //           that means & delimited
        //
        //   On Exit:
        //       OptionString_ForObjectCall
        //               pass this string to the addon when it is run, crlf delimited name=value pair.
        //               This should include just the name=values pairs, with no selectors
        //               it should include names from both Addon and Instance
        //               If the Instance has a value, include it. Otherwise include Addon value
        //       AddonOptionExpandedConstructor = pass this to the bubble editor to create the the selectr
        //===================================================================================================
        //
        public void buildAddonOptionLists2(ref Dictionary<string, string> addonInstanceProperties, ref string addonArgumentListPassToBubbleEditor, string addonArgumentListFromRecord, Dictionary<string, string> instanceOptions, string InstanceID, bool IncludeSettingsBubbleOptions) {
            try {
                //
                int SavePtr = 0;
                string ConstructorValue = null;
                string ConstructorSelector = null;
                string ConstructorName = null;
                int ConstructorPtr = 0;
                string[] ConstructorNames = { };
                string[] ConstructorSelectors = { };
                string[] ConstructorValues = { };
                int ConstructorCnt = 0;
                if (!string.IsNullOrEmpty(addonArgumentListFromRecord)) {
                    //
                    string[] ConstructorNameValues = { };
                    //
                    // Initially Build Constructor from AddonOptions
                    //
                    ConstructorNameValues = GenericController.stringSplit(addonArgumentListFromRecord, Environment.NewLine);
                    ConstructorCnt = ConstructorNameValues.GetUpperBound(0) + 1;
                    ConstructorNames = new string[ConstructorCnt + 1];
                    ConstructorSelectors = new string[ConstructorCnt + 1];
                    ConstructorValues = new string[ConstructorCnt + 1];
                    string[] ConstructorTypes = new string[ConstructorCnt + 1];
                    SavePtr = 0;
                    for (ConstructorPtr = 0; ConstructorPtr < ConstructorCnt; ConstructorPtr++) {
                        ConstructorName = ConstructorNameValues[ConstructorPtr];
                        ConstructorSelector = "";
                        ConstructorValue = "";
                        int Pos = GenericController.vbInstr(1, ConstructorName, "=");
                        if (Pos > 1) {
                            ConstructorValue = ConstructorName.Substring(Pos);
                            ConstructorName = (ConstructorName.Left(Pos - 1)).Trim(' ');
                            Pos = GenericController.vbInstr(1, ConstructorValue, "[");
                            if (Pos > 0) {
                                ConstructorSelector = ConstructorValue.Substring(Pos - 1);
                                ConstructorValue = ConstructorValue.Left(Pos - 1);
                            }
                        }
                        if (!string.IsNullOrEmpty(ConstructorName)) {
                            //Pos = genericController.vbInstr(1, ConstructorName, ",")
                            //If Pos > 1 Then
                            //    ConstructorType = Mid(ConstructorName, Pos + 1)
                            //    ConstructorName = Left(ConstructorName, Pos - 1)
                            //End If

                            ConstructorNames[SavePtr] = ConstructorName;
                            ConstructorValues[SavePtr] = ConstructorValue;
                            ConstructorSelectors[SavePtr] = ConstructorSelector;
                            //ConstructorTypes(ConstructorPtr) = ConstructorType
                            SavePtr += 1;
                        }
                    }
                    ConstructorCnt = SavePtr;
                }
                //
                foreach (var kvp in instanceOptions) {
                    string InstanceName = kvp.Key;
                    string InstanceValue = kvp.Value;
                    if (!string.IsNullOrEmpty(InstanceName)) {
                        //
                        // if the name is not in the Constructor, add it
                        if (ConstructorCnt > 0) {
                            for (ConstructorPtr = 0; ConstructorPtr < ConstructorCnt; ConstructorPtr++) {
                                if (GenericController.vbLCase(InstanceName) == GenericController.vbLCase(ConstructorNames[ConstructorPtr])) {
                                    break;
                                }
                            }
                        }
                        if (ConstructorPtr >= ConstructorCnt) {
                            //
                            // not found, add this instance name and value to the Constructor values
                            //
                            Array.Resize(ref ConstructorNames, ConstructorCnt + 1);
                            Array.Resize(ref ConstructorValues, ConstructorCnt + 1);
                            Array.Resize(ref ConstructorSelectors, ConstructorCnt + 1);
                            ConstructorNames[ConstructorCnt] = InstanceName;
                            ConstructorValues[ConstructorCnt] = InstanceValue;
                            ConstructorCnt = ConstructorCnt + 1;
                        } else {
                            //
                            // found, set the ConstructorValue to the instance value
                            //
                            ConstructorValues[ConstructorPtr] = InstanceValue;
                        }
                        SavePtr += 1;
                    }
                }
                addonArgumentListPassToBubbleEditor = "";
                //
                // Build output strings from name and value found
                //
                for (ConstructorPtr = 0; ConstructorPtr < ConstructorCnt; ConstructorPtr++) {
                    ConstructorName = ConstructorNames[ConstructorPtr];
                    ConstructorValue = ConstructorValues[ConstructorPtr];
                    ConstructorSelector = ConstructorSelectors[ConstructorPtr];
                    addonInstanceProperties.Add(ConstructorName, ConstructorValue);
                    if (IncludeSettingsBubbleOptions) {
                        addonArgumentListPassToBubbleEditor = addonArgumentListPassToBubbleEditor + Environment.NewLine + core.html.getAddonSelector(ConstructorName, ConstructorValue, ConstructorSelector);
                    }
                }
                addonInstanceProperties.Add("InstanceID", InstanceID);
                if (!string.IsNullOrEmpty(addonArgumentListPassToBubbleEditor)) {
                    addonArgumentListPassToBubbleEditor = addonArgumentListPassToBubbleEditor.Substring(2);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //===================================================================================================
        //   Build AddonOptionLists
        //
        //   On entry:
        //       AddonOptionConstructor = the addon-encoded Version of the list that comes from the Addon Record
        //           It is line-delimited with &, and all escape characters converted
        //       InstanceOptionList = addonencoded Version of the list that comes from the HTML AC tag
        //           that means crlf line-delimited
        //
        //   On Exit:
        //       AddonOptionNameValueList
        //               pass this string to the addon when it is run, crlf delimited name=value pair.
        //               This should include just the name=values pairs, with no selectors
        //               it should include names from both Addon and Instance
        //               If the Instance has a value, include it. Otherwise include Addon value
        //       AddonOptionExpandedConstructor = pass this to the bubble editor to create the the selectr
        //===================================================================================================
        //
        public void buildAddonOptionLists(ref Dictionary<string, string> addonInstanceProperties, ref string addonArgumentListPassToBubbleEditor, string addonArgumentListFromRecord, Dictionary<string, string> InstanceOptionList, string InstanceID, bool IncludeEditWrapper) {
            buildAddonOptionLists2(ref addonInstanceProperties, ref addonArgumentListPassToBubbleEditor, addonArgumentListFromRecord, InstanceOptionList, InstanceID, IncludeEditWrapper);
        }
        //
        //====================================================================================================
        //
        public string getPrivateFilesAddonPath() {
            return "addons\\";
        }
        //
        //====================================================================================================
        //   Apply a wrapper to content
        // todo -- wrapper should be an addon !!!
        private string addWrapperToResult(string Content, int WrapperID, string WrapperSourceForComment = "") {
            string result = Content;
            try {
                string SelectFieldList = "name,copytext,javascriptonload,javascriptbodyend,stylesfilename,otherheadtags,JSFilename,targetString";
                using (var csData = new CsModel(core)) {
                    csData.openRecord("Wrappers", WrapperID, SelectFieldList);
                    if (csData.ok()) {
                        string Wrapper = csData.getText("copytext");
                        string wrapperName = csData.getText("name");
                        string TargetString = csData.getText("targetString");
                        //
                        string SourceComment = "wrapper " + wrapperName;
                        if (!string.IsNullOrEmpty(WrapperSourceForComment)) {
                            SourceComment = SourceComment + " for " + WrapperSourceForComment;
                        }
                        core.html.addScriptCode_onLoad(csData.getText("javascriptonload"), SourceComment);
                        core.html.addScriptCode(csData.getText("javascriptbodyend"), SourceComment);
                        core.html.addHeadTag(csData.getText("OtherHeadTags"), SourceComment);
                        //
                        string JSFilename = csData.getText("jsfilename");
                        if (!string.IsNullOrEmpty(JSFilename)) {
                            JSFilename = GenericController.getCdnFileLink(core, JSFilename);
                            core.html.addScriptLinkSrc(JSFilename, SourceComment);
                        }
                        string Copy = csData.getText("stylesfilename");
                        if (!string.IsNullOrEmpty(Copy)) {
                            if (GenericController.vbInstr(1, Copy, "://") != 0) {
                            } else if (Copy.Left(1) == "/") {
                            } else {
                                Copy = GenericController.getCdnFileLink(core, Copy);
                            }
                            core.html.addStyleLink(Copy, SourceComment);
                        }
                        //
                        if (!string.IsNullOrEmpty(Wrapper)) {
                            int Pos = GenericController.vbInstr(1, Wrapper, TargetString, 1);
                            if (Pos != 0) {
                                result = GenericController.vbReplace(Wrapper, TargetString, result, 1, 99, 1);
                            } else {
                                result = ""
                                    + "<!-- the selected wrapper does not include the Target String marker to locate the position of the content. -->"
                                    + Wrapper + result;
                            }
                        }
                    }
                    csData.close();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        //====================================================================================================
        // main_Get an XML nodes attribute based on its name
        //
        public string xml_GetAttribute(bool Found, XmlNode Node, string Name, string DefaultIfNotFound) {
            string result = DefaultIfNotFound;
            try {
                Found = false;
                XmlNode ResultNode = Node.Attributes.GetNamedItem(Name);
                if (ResultNode == null) {
                    string UcaseName = GenericController.vbUCase(Name);
                    foreach (XmlAttribute NodeAttribute in Node.Attributes) {
                        if (GenericController.vbUCase(NodeAttribute.Name) == UcaseName) {
                            result = NodeAttribute.Value;
                            Found = true;
                            break;
                        }
                    }
                } else {
                    result = ResultNode.Value;
                    Found = true;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get an option from an argument list
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ArgumentList"></param>
        /// <param name="AddonGuid"></param>
        /// <param name="IsInline"></param>
        /// <returns></returns>
        public static string getDefaultAddonOptions(CoreController core, string ArgumentList, string AddonGuid, bool IsInline) {
            ArgumentList = GenericController.vbReplace(ArgumentList, Environment.NewLine, "\r");
            ArgumentList = GenericController.vbReplace(ArgumentList, "\n", "\r");
            ArgumentList = GenericController.vbReplace(ArgumentList, "\r", Environment.NewLine);
            if (ArgumentList.IndexOf("wrapper", System.StringComparison.OrdinalIgnoreCase) == -1) {
                //
                // Add in default constructors, like wrapper
                if (!string.IsNullOrEmpty(ArgumentList)) {
                    ArgumentList += Environment.NewLine;
                }
                if (GenericController.vbLCase(AddonGuid) == GenericController.vbLCase(addonGuidContentBox)) {
                    ArgumentList += AddonOptionConstructor_BlockNoAjax;
                } else if (IsInline) {
                    ArgumentList += AddonOptionConstructor_Inline;
                } else {
                    ArgumentList += AddonOptionConstructor_Block;
                }
            }
            string result = "";
            if (!string.IsNullOrEmpty(ArgumentList)) {
                //
                // Argument list is present, translate from AddonConstructor to AddonOption format (see main_executeAddon for details)
                //
                string[] QuerySplit = GenericController.splitNewLine(ArgumentList);
                for (int Ptr = 0; Ptr <= QuerySplit.GetUpperBound(0); Ptr++) {
                    string NameValue = QuerySplit[Ptr];
                    if (!string.IsNullOrEmpty(NameValue)) {
                        //
                        // Execute list functions
                        //
                        string OptionName = "";
                        string OptionValue = "";
                        string OptionSelector = "";
                        //
                        // split on equal
                        //
                        NameValue = GenericController.vbReplace(NameValue, "\\=", Environment.NewLine);
                        int Pos = GenericController.vbInstr(1, NameValue, "=");
                        if (Pos == 0) {
                            OptionName = NameValue;
                        } else {
                            OptionName = NameValue.Left(Pos - 1);
                            OptionValue = NameValue.Substring(Pos);
                        }
                        OptionName = GenericController.vbReplace(OptionName, Environment.NewLine, "\\=");
                        OptionValue = GenericController.vbReplace(OptionValue, Environment.NewLine, "\\=");
                        //
                        // split optionvalue on [
                        //
                        OptionValue = GenericController.vbReplace(OptionValue, "\\[", Environment.NewLine);
                        Pos = GenericController.vbInstr(1, OptionValue, "[");
                        if (Pos != 0) {
                            OptionSelector = OptionValue.Substring(Pos - 1);
                            OptionValue = OptionValue.Left(Pos - 1);
                        }
                        OptionValue = GenericController.vbReplace(OptionValue, Environment.NewLine, "\\[");
                        OptionSelector = GenericController.vbReplace(OptionSelector, Environment.NewLine, "\\[");
                        //
                        // Decode AddonConstructor format
                        OptionName = GenericController.DecodeAddonConstructorArgument(OptionName);
                        OptionValue = GenericController.DecodeAddonConstructorArgument(OptionValue);
                        //
                        // Encode AddonOption format
                        OptionValue = GenericController.encodeNvaArgument(OptionValue);
                        //
                        // rejoin
                        string NameValuePair = core.html.getAddonSelector(OptionName, OptionValue, OptionSelector);
                        NameValuePair = GenericController.EncodeJavascriptStringSingleQuote(NameValuePair);
                        result += "&" + NameValuePair;
                        if (GenericController.vbInstr(1, NameValuePair, "=") == 0) {
                            result += "=";
                        }
                    }
                }
                if (!string.IsNullOrEmpty(result)) {
                    result = result.Substring(1);
                }
            }
            return result;
        }
        //
        //====================================================================================================
        //
        private string getAddonDescription(CoreController core, AddonModel addon) {
            string addonDescription = "[invalid addon]";
            if (addon != null) {
                string collectionName = "invalid collection or collection not set";
                AddonCollectionModel collection = AddonCollectionModel.create<AddonCollectionModel>(core.cpParent, addon.collectionID);
                if (collection != null) {
                    collectionName = collection.name;
                }
                addonDescription = "[#" + addon.id.ToString() + ", " + addon.name + "], collection [" + collectionName + "]";
            }
            return addonDescription;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Special case addon as it is a required core service. This method attempts the addon call and it if fails, calls the safe-mode version, tested for this build
        /// </summary>
        /// <returns></returns>
        public static string getAddonManager(CoreController core) {
            string result = "";
            try {
                bool AddonStatusOK = true;
                try {
                    AddonModel addon = DbBaseModel.create<AddonModel>(core.cpParent, addonGuidAddonManager);
                    if (addon != null) {
                        result = core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext() {
                            addonType = CPUtilsBaseClass.addonContext.ContextAdmin,
                            errorContextMessage = "calling addon manager guid for GetAddonManager method"
                        });
                    }
                } catch (Exception ex) {
                    LogController.logError(core, new Exception("Error calling ExecuteAddon with AddonManagerGuid, will attempt Safe Mode Addon Manager. Exception=[" + ex.ToString() + "]"));
                    AddonStatusOK = false;
                }
                if (string.IsNullOrEmpty(result)) {
                    LogController.logError(core, new Exception("AddonManager returned blank, calling Safe Mode Addon Manager."));
                    AddonStatusOK = false;
                }
                if (!AddonStatusOK) {
                    Addons.SafeAddonManager.AddonManagerClass AddonMan = new Addons.SafeAddonManager.AddonManagerClass(core);
                    result = AddonMan.getForm_SafeModeAddonManager();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Throw an addon event, which will call all addons registered to handle it
        /// </summary>
        /// <param name="eventNameIdOrGuid"></param>
        /// <returns></returns>
        public string throwEvent(string eventNameIdOrGuid) {
            string returnString = "";
            try {
                using (var cs = new CsModel(core)) {
                    string sql = "select e.id,c.addonId"
                        + " from (ccAddonEvents e"
                        + " left join ccAddonEventCatchers c on c.eventId=e.id)"
                        + " where ";
                    if (eventNameIdOrGuid.IsNumeric()) {
                        sql += "e.id=" + DbController.encodeSQLNumber(double.Parse(eventNameIdOrGuid));
                    } else if (GenericController.isGuid(eventNameIdOrGuid)) {
                        sql += "e.ccGuid=" + DbController.encodeSQLText(eventNameIdOrGuid);
                    } else {
                        sql += "e.name=" + DbController.encodeSQLText(eventNameIdOrGuid);
                    }
                    if (!cs.openSql(sql)) {
                        //
                        // event not found
                        if (eventNameIdOrGuid.IsNumeric()) {
                            //
                            // can not create an id
                        } else if (GenericController.isGuid(eventNameIdOrGuid)) {
                            //
                            // create event with Guid and id for name
                            cs.close();
                            cs.insert("add-on Events");
                            cs.set("ccguid", eventNameIdOrGuid);
                            cs.set("name", "Event " + cs.getInteger("id").ToString());
                        } else if (!string.IsNullOrEmpty(eventNameIdOrGuid)) {
                            //
                            // create event with name
                            cs.close();
                            cs.insert("add-on Events");
                            cs.set("name", eventNameIdOrGuid);
                        }
                    } else {
                        while (cs.ok()) {
                            int addonid = cs.getInteger("addonid");
                            if (addonid != 0) {
                                var addon = DbBaseModel.create<AddonModel>(core.cpParent, addonid);
                                returnString += core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                    addonType = CPUtilsBaseClass.addonContext.ContextSimple,
                                    errorContextMessage = "calling handler addon id [" + addonid + "] for event [" + eventNameIdOrGuid + "]"
                                });
                            }
                            cs.goNext();
                        }
                    }
                    cs.close();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return returnString;
        }
        //
        //========================================================================================================
        /// <summary>
        /// create an addon icon image for the desktop
        /// </summary>
        /// <param name="AdminURL"></param>
        /// <param name="IconWidth"></param>
        /// <param name="IconHeight"></param>
        /// <param name="IconSprites"></param>
        /// <param name="IconIsInline"></param>
        /// <param name="IconImgID"></param>
        /// <param name="IconFilename"></param>
        /// <param name="serverFilePath"></param>
        /// <param name="IconAlt"></param>
        /// <param name="IconTitle"></param>
        /// <param name="ACInstanceID"></param>
        /// <param name="IconSpriteColumn"></param>
        /// <returns></returns>
        public static string getAddonIconImg(string AdminURL, int IconWidth, int IconHeight, int IconSprites, bool IconIsInline, string IconImgID, string IconFilename, string serverFilePath, string IconAlt, string IconTitle, string ACInstanceID, int IconSpriteColumn) {
            string tempGetAddonIconImg = "";
            try {
                if (string.IsNullOrEmpty(IconAlt)) { IconAlt = "Add-on"; }
                if (string.IsNullOrEmpty(IconTitle)) { IconTitle = "Rendered as Add-on"; }
                if (string.IsNullOrEmpty(IconFilename)) {
                    //
                    // No icon given, use the default
                    if (IconIsInline) {
                        IconFilename = "https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/images/IconAddonInlineDefault.png";
                        IconWidth = 62;
                        IconHeight = 17;
                        IconSprites = 0;
                    } else {
                        IconFilename = "https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/images/IconAddonBlockDefault.png";
                        IconWidth = 57;
                        IconHeight = 59;
                        IconSprites = 4;
                    }
                } else if (vbInstr(1, IconFilename, "://") != 0) {
                    //
                    // icon is an Absolute URL - leave it
                    //
                } else if (IconFilename.Left(1) == "/") {
                    //
                    // icon is Root Relative, leave it
                    //
                } else {
                    //
                    // icon is a virtual file, add the serverfilepath
                    //
                    IconFilename = serverFilePath + IconFilename;
                }
                if ((IconWidth == 0) || (IconHeight == 0)) { IconSprites = 0; }
                if (IconSprites == 0) {
                    //
                    // just the icon
                    tempGetAddonIconImg = "<img"
                        + " border=0"
                        + " id=\"" + IconImgID + "\""
                        + " alt=\"" + IconAlt + "\""
                        + " title=\"" + IconTitle + "\""
                        + " src=\"" + IconFilename + "\"";
                    if (IconWidth != 0) { tempGetAddonIconImg += " width=\"" + IconWidth + "px\""; }
                    if (IconHeight != 0) { tempGetAddonIconImg += " height=\"" + IconHeight + "px\""; }
                    if (IconIsInline) {
                        tempGetAddonIconImg += " style=\"vertical-align:middle;display:inline;\" ";
                    } else {
                        tempGetAddonIconImg += " style=\"display:block\" ";
                    }
                    if (!string.IsNullOrEmpty(ACInstanceID)) { tempGetAddonIconImg += " ACInstanceID=\"" + ACInstanceID + "\""; }
                    tempGetAddonIconImg += ">";
                } else {
                    //
                    // Sprite Icon
                    tempGetAddonIconImg = getIconSprite(IconImgID, IconSpriteColumn, IconFilename, IconWidth, IconHeight, IconAlt, IconTitle, "", IconIsInline, ACInstanceID);
                }
            } catch (Exception) {
                throw;
            }
            return tempGetAddonIconImg;
        }
        //
        //========================================================================================================
        /// <summary>
        /// get addon sprite img
        /// </summary>
        /// <param name="TagID"></param>
        /// <param name="SpriteColumn"></param>
        /// <param name="IconSrc"></param>
        /// <param name="IconWidth"></param>
        /// <param name="IconHeight"></param>
        /// <param name="IconAlt"></param>
        /// <param name="IconTitle"></param>
        /// <param name="onDblClick"></param>
        /// <param name="IconIsInline"></param>
        /// <param name="ACInstanceID"></param>
        /// <returns></returns>
        public static string getIconSprite(string TagID, int SpriteColumn, string IconSrc, int IconWidth, int IconHeight, string IconAlt, string IconTitle, string onDblClick, bool IconIsInline, string ACInstanceID) {
            string tempGetIconSprite = "";
            try {
                tempGetIconSprite = "<img"
                    + " border=0"
                    + " id=\"" + TagID + "\""
                    + " onMouseOver=\"this.style.backgroundPosition='" + (-1 * SpriteColumn * IconWidth) + "px -" + (2 * IconHeight) + "px';\""
                    + " onMouseOut=\"this.style.backgroundPosition='" + (-1 * SpriteColumn * IconWidth) + "px 0px'\""
                    + " onDblClick=\"" + onDblClick + "\""
                    + " alt=\"" + IconAlt + "\""
                    + " title=\"" + IconTitle + "\""
                    + " src=\"https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/images/spacer.gif\"";
                string ImgStyle = "background:url(" + IconSrc + ") " + (-1 * SpriteColumn * IconWidth) + "px 0px no-repeat;";
                ImgStyle += "width:" + IconWidth + "px;";
                ImgStyle = ImgStyle + "height:" + IconHeight + "px;";
                if (IconIsInline) {
                    ImgStyle += "vertical-align:middle;display:inline;";
                } else {
                    ImgStyle += "display:block;";
                }
                if (!string.IsNullOrEmpty(ACInstanceID)) {
                    tempGetIconSprite += " ACInstanceID=\"" + ACInstanceID + "\"";
                }
                tempGetIconSprite += " style=\"" + ImgStyle + "\">";
            } catch (Exception) {
                throw;
            }
            return tempGetIconSprite;
        }
        //====================================================================================================
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed = false;
        //
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~AddonController() {
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
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }
}