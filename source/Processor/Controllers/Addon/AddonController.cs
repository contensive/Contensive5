
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
using static Newtonsoft.Json.JsonConvert;

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
        private readonly CoreController core;
        /// <summary>
        /// possible scripting languanges
        /// </summary>
        public enum ScriptLanguages {
            /// <summary>
            /// vb scripting language
            /// </summary>
            VBScript = 1,
            /// <summary>
            /// javascript scripting language
            /// </summary>
            Javascript = 2
        }
        //
        // ====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core"></param>
        public AddonController(CoreController core) {
            this.core = core;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// convert the return of an addon executable (code or script) to string
        /// </summary>
        /// <param name="AddonObjResult"></param>
        /// <returns></returns>
        public static string convertAddonReturntoString(object AddonObjResult) {
            if (AddonObjResult == null) { return string.Empty; }
            if (AddonObjResult.GetType() == typeof(string)) { return (string)AddonObjResult; }
            if (AddonObjResult.GetType() == typeof(int)) { return ((int)AddonObjResult).ToString(CultureInfo.InvariantCulture); }
            if (AddonObjResult.GetType() == typeof(double)) { return ((double)AddonObjResult).ToString(CultureInfo.InvariantCulture); }
            if (AddonObjResult.GetType() == typeof(bool)) { return ((bool)AddonObjResult).ToString(CultureInfo.InvariantCulture); }
            if (AddonObjResult.GetType() == typeof(DateTime)) { return ((DateTime)AddonObjResult).ToString(CultureInfo.InvariantCulture); }
            //
            // -- all objects serialize to JSON
            string result = SerializeObject(AddonObjResult);
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// execute all onBodyStart addons
        /// </summary>
        /// <returns></returns>
        public string executeOnBodyStart() {
            var result = new StringBuilder();
            //
            // -- OnBodyStart add-ons
            foreach (AddonModel addon in core.cacheStore.addonCache.getOnBodyStartAddonList()) {
                var bodyStartContext = new CPUtilsBaseClass.addonExecuteContext {
                    addonType = CPUtilsBaseClass.addonContext.ContextOnBodyStart,
                    errorContextMessage = "calling onBodyStart addon [" + addon.name + "] in HtmlBodyTemplate"
                };
                result.Append(execute(addon, bodyStartContext));
            }
            return result.ToString();
        }
        //
        // ====================================================================================================
        /// <summary>
        /// execute all onbodyend addons, return the results
        /// </summary>
        /// <returns></returns>
        public string executeOnBodyEnd() {
            var result = new StringBuilder();
            //
            // -- OnBodyEnd add-ons
            foreach (var addon in core.cacheStore.addonCache.getOnBodyEndAddonList()) {
                var bodyEndContext = new CPUtilsBaseClass.addonExecuteContext {
                    addonType = CPUtilsBaseClass.addonContext.ContextFilter,
                    errorContextMessage = "calling onBodyEnd addon [" + addon.name + "] in HtmlBodyTemplate"
                };
                result.Append(execute(addon, bodyEndContext));
            }
            return result.ToString();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon because it is a dependency of another addon/page/template. A dependancy is only run once in a page.
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="executeContext"></param>
        /// <returns></returns>
        /// 
        public string executeDependency(AddonModel addon, CPUtilsBaseClass.addonExecuteContext executeContext) {
            //
            if (addon == null) {
                if (executeContext == null) {
                    LogController.logWarn(core, "executeDependency argument error, addon null, executeContext is null");
                    return "";
                }
                LogController.logWarn(core, "executeDependency argument error, addon null, executeContext [" + executeContext.errorContextMessage + "]");
                return "";
            }
            if (executeContext == null) { executeContext = new CPUtilsBaseClass.addonExecuteContext(); };
            //
            LogController.logDebug(core, "AddonController.executeDependency [" + addon.name + "], executeContext [" + executeContext.errorContextMessage + "]");
            //
            // -- no, cannot exit because for ex, if JQ has been added, then it is added again but the js needs to be in the head,
            // -- then it needs to run so it's js is moved to head.
            // -- x -- exit if this addon has already been executed
            // i f ( c o r e . d o c . a d d o n s E x e c u t e d . C o n t a i n s ( a d d o n . i d ) )   {   r e t u r n   " " ; }
            //
            // -- save current context
            var contextParent = new CPUtilsBaseClass.addonExecuteContext {
                forceHtmlDocument = executeContext.forceHtmlDocument,
                isDependency = executeContext.isDependency,
                wrapperID = executeContext.wrapperID
            };
            //
            // -- set dependency context
            executeContext.isDependency = true;
            executeContext.forceHtmlDocument = false;
            executeContext.wrapperID = 0;
            //
            // -- execute addon
            string result = execute(addon, executeContext);
            //
            // -- restore previous conext
            executeContext.wrapperID = contextParent.wrapperID;
            executeContext.forceHtmlDocument = contextParent.forceHtmlDocument;
            executeContext.isDependency = contextParent.isDependency;
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
            AddonModel addon = core.cacheStore.addonCache.create(addonGuid);
            if (addon == null) {
                if (executeContext == null) {
                    LogController.logWarn(core, "cp.addon.execute argument error, no addon found with addonGuid [" + addonGuid + "], executeContext is null");
                    return "";
                }
                LogController.logWarn(core, "cp.addon.execute argument error, no addon found with addonGuid [" + addonGuid + "], executeContext [" + executeContext.errorContextMessage + "]");
                return "";
            }
            return execute(addon, executeContext);
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute an addon by its id
        /// </summary>
        /// <param name="addonId"></param>
        /// <param name="executeContext"></param>
        /// <returns></returns>
        public string execute(int addonId, CPUtilsBaseClass.addonExecuteContext executeContext) {
            AddonModel addon = core.cacheStore.addonCache.create(addonId);
            if (addon == null) {
                if (executeContext == null) {
                    LogController.logWarn(core, "cp.addon.execute argument error, no addon found with addonId [" + addonId + "], executeContext is null");
                    return "";
                }
                LogController.logWarn(core, "cp.addon.execute argument error, no addon found with addonId [" + addonId + "], executeContext [" + executeContext.errorContextMessage + "]");
                return "";
            }
            return execute(addon, executeContext);
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute addon
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="executeContext">A description of the calling method/environment</param>
        /// <returns></returns>
        public string execute(AddonModel addon, CPUtilsBaseClass.addonExecuteContext executeContext) {
            int hint = 0;
            Stopwatch sw = new();
            sw.Start();
            //
            LogController.logDebug(core, "AddonController.execute [" + ((addon == null) ? "null" : addon.id.ToString() + ", " + addon.name) + "], context [" + (executeContext == null ? "null" : executeContext.addonType) + "], context [" + (executeContext == null ? "null" : executeContext.errorContextMessage) + "]");
            //
            // -- verify arguments
            if (addon == null) {
                if (executeContext == null) {
                    //
                    // -- no addon and no context
                    LogController.logWarn(core, new ArgumentException("AddonController.execute called with null addon and null executeContext."));
                    return "";
                }
                //
                // -- no addon
                LogController.logWarn(core, new ArgumentException("AddonController.execute called with null addon, executeContext [" + executeContext.errorContextMessage + "]."));
                return "";
            }
            if (executeContext == null) {
                //
                // -- no context
                LogController.logError(core, new ArgumentException("AddonController.execute call with invalid executeContext,  was not configured for addon [#" + addon.id + ", " + addon.name + "]."));
                return "";
            }
            //
            // -- test dependency-recursion (where a depends on b, and b depends on a)
            // -- block the second running on any dependency
            if (executeContext.isDependency && executeContext.dependencyRecursionTestStack.Contains(addon.id)) {
                //
                // -- error -- recursive addon dependencies. Exit now
                string recursionTraceMsg = "addon stack ";
                foreach (var addonInStack in core.doc.addonExecutionStack) {
                    recursionTraceMsg += "[" + addonInStack.id + ", " + addonInStack.name + "], called by ";
                }
                recursionTraceMsg = recursionTraceMsg.substringSafe(0, recursionTraceMsg.Length - 12);
                LogController.logError(core, new GenericException("AddonController.execute, Addon appears in its own recursion path, addon [" + addon.id + ", " + addon.name + "], recursion [" + recursionTraceMsg + "]"));
                return "";
            }
            //
            // -- test developer-recursion - if a developer inside and addon calls other addons, and that addon then calls the first
            // -- track how many times each addon is being executed with an array that includes the number of times each addon is executing
            // -- within one execution environment, an addon can only be currently executing 5 times.
            bool inDeveloperRecursionList = core.doc.addonDeveloperRecursionCount.ContainsKey(addon.id);
            if (inDeveloperRecursionList && (core.doc.addonDeveloperRecursionCount[addon.id] > addonRecursionLimit)) {
                //
                // -- error -- cannot call an addon within an addon
                string recursionTraceMsg = "addon stack ";
                foreach (var addonInStack in core.doc.addonExecutionStack) {
                    recursionTraceMsg += "[" + addonInStack.id + ", " + addonInStack.name + "], called by ";
                }
                recursionTraceMsg = recursionTraceMsg.substringSafe(0, recursionTraceMsg.Length - 12);
                LogController.logError(core, new GenericException("AddonController.execute, Addon appears in its own recursion path, addon [" + addon.id + ", " + addon.name + "], recursion [" + recursionTraceMsg + "]"));
                return "";
            }
            //
            // -- setup values that have to be within finalize
            var result = new StringBuilder();
            string parentInstanceId = core.docProperties.getText("instanceId");
            bool rootLevelAddon = core.doc.addonDeveloperRecursionCount.Count.Equals(0);
            try {
                hint = 1;
                //
                // -- test developer-recursion
                // -- save the addon details in a fifo stack to popoff during exit.
                // -- Used to track the current addon being executed
                core.doc.addonExecutionStack.Push(addon);
                executeContext.dependencyRecursionTestStack.Push(addon.id);
                // -- add all addons to the dependencyrecursionTestStack, only test them if they are dependencies - but block them even if they were first run as non-dependencies
                hint = 2;
                //
                // -- track recursion and continue
                if (!inDeveloperRecursionList) {
                    core.doc.addonDeveloperRecursionCount.Add(addon.id, 1);
                } else {
                    core.doc.addonDeveloperRecursionCount[addon.id] += 1;
                }
                //
                // -- execute dependencies, if the addon's javascript is required in the head, set it in the executeContext now so it will propigate into the dependant addons as well
                {
                    bool save_forceJavascriptToHead = executeContext.forceJavascriptToHead;
                    executeContext.forceJavascriptToHead = executeContext.forceJavascriptToHead || addon.javascriptForceHead;
                    //
                    // -- run included add-ons before their parent
                    foreach (var dependentAddon in core.cacheStore.addonCache.getDependsOnList(addon.id)) {
                        if (dependentAddon == null) {
                            LogController.logWarn(core, new GenericException("Dependent addon not found. An included addon of [" + addon.name + "] was not found. The included addon may have been deleted. Recreate or reinstall the missing addon, then reinstall [" + addon.name + "] or manually correct the included addon selection."));
                            continue;
                        }
                        executeContext.errorContextMessage = "adding dependent addon [" + dependentAddon.name + "] for addon [" + addon.name + "] called within context [" + executeContext.errorContextMessage + "]";
                        result.Append(executeDependency(dependentAddon, executeContext));
                    }
                    executeContext.forceJavascriptToHead = save_forceJavascriptToHead;
                    hint = 3;
                }
                //
                // -- Track that this addon has been executed (to provide dependency verification)
                bool isDependencyThatAlreadyRan = false;
                if (!core.doc.addonsExecuted.Contains(addon.id)) {
                    //
                    // -- addon not run yet this page, add it to addonsExecuted list
                    core.doc.addonsExecuted.Add(addon.id);
                } else {
                    // -- addon has aleady run once. If just a dependency, set flag to signal this is running just to check js-in-head and all else should be skipped
                    isDependencyThatAlreadyRan = executeContext.isDependency;
                }
                //
                if (!string.IsNullOrEmpty(addon.objectProgramId)) {
                    //
                    // -- addons with activeX components are deprecated
                    string addonDescription = getAddonDescription(core, addon);
                    throw new GenericException("Addon is no longer supported because it contains an active-X component, add-on " + addonDescription + ".");
                }

                //
                // -- if root level addon, and the addon is an html document, create the html document around it and uglify if not debugging
                if (executeContext.forceHtmlDocument || (rootLevelAddon && addon.htmlDocument)) {
                    //
                    // -- on body start addons (as long  as this addon is not onbodystart also
                    if (!addon.onBodyStart) {
                        result.Append(core.addon.executeOnBodyStart());
                    } else {
                        logger.Warn("Addon [" + addon.id + ", " + addon.name + "] is run as an Html Document addon but it is marked onBodyStart. onBodyStart was ignored.");
                    }
                }
                //
                // -- if executeContext.instanceGuid is set, save the current instanceId and set the context value. If not set, leave the current in place
                if (!string.IsNullOrWhiteSpace(executeContext.instanceGuid)) {
                    core.docProperties.setProperty("instanceId", executeContext.instanceGuid);
                }
                hint = 4;
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
                                if (nvpValue.IndexOf("[", StringComparison.InvariantCulture) >= 0) {
                                    nvpValue = nvpValue.left(nvpValue.IndexOf("[", StringComparison.InvariantCulture));
                                }
                                core.docProperties.setProperty(nvp[0], nvpValue);
                            }
                        }
                    }
                }
                hint = 5;
                //
                // -- add instance properties to doc properties
                string containerCssId = "";
                string ContainerCssClass = "";
                foreach (var kvp in executeContext.argumentKeyValuePairs) {
                    switch (kvp.Key.ToLowerInvariant()) {
                        case "wrapper": {
                                executeContext.wrapperID = GenericController.encodeInteger(kvp.Value);
                                break;
                            }
                        case "as ajax": {
                                addon.asAjax = GenericController.encodeBoolean(kvp.Value);
                                break;
                            }
                        case "css container id": {
                                containerCssId = kvp.Value;
                                break;
                            }
                        case "css container class": {
                                ContainerCssClass = kvp.Value;
                                break;
                            }
                        default: {
                                // do nothing
                                break;
                            }
                    }
                    core.docProperties.setProperty(kvp.Key, kvp.Value);
                }
                //
                // -- build content
                //
                string AddedByName = addon.name + " addon";
                if (!isDependencyThatAlreadyRan && addon.inFrame && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodJson)) {
                    //
                    // -- inframe execution, deliver iframe with link back to remote method. remote is intercepted in routing and execute is called with context ContextRemoteMethodHtml or ContextRemoteMethodJson
                    //
                    result.Append("TBD - inframe");
                } else if (!isDependencyThatAlreadyRan && addon.asAjax && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodJson)) {
                    //
                    // -- asajax execution, deliver div with ajax callback. remote is intercepted in routing and execute is called with context ContextRemoteMethodHtml or ContextRemoteMethodJson
                    //
                    result.Append("TBD - asajax");
                } else {
                    //
                    // -- standard addon content delivery
                    //
                    hint = 9;
                    if (addon.inFrame && (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml)) {
                        //
                        // -- remote method called from inframe execution
                        //
                        result.Append("TBD - remotemethod inframe");
                    } else if (addon.asAjax && (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml)) {
                        //
                        // -- remotemethod called from asajax execution. Add-on setup for AsAjax, running the call-back - put the referring page's QS as the RQS. restore form values
                        //
                        result.Append("TBD - remotemethod ajax");
                    }
                    hint = 10;
                    //
                    // -- js code
                    hint = 11;
                    bool validMinJs = core.siteProperties.allowMinify && !string.IsNullOrEmpty(addon.minifyJsFilename.filename);
                    if (validMinJs) {
                        string scriptCodeUrl = getCdnFileLink(core, addon.minifyJsFilename.filename);
                        core.html.addScriptLinkSrc(scriptCodeUrl, AddedByName + " Minify Javascript Head Code", (executeContext.forceJavascriptToHead || addon.javascriptForceHead), addon.id);
                    } else if (!string.IsNullOrEmpty(addon.jsFilename.filename)) {
                        string scriptCodeUrl = getCdnFileLink(core, addon.jsFilename.filename);
                        core.html.addScriptLinkSrc(scriptCodeUrl, AddedByName + " Javascript Head Code", (executeContext.forceJavascriptToHead || addon.javascriptForceHead), addon.id);
                    }
                    //
                    // -- js Url
                    string scriptUrl = AddonModel.getPlatformAsset(core.cpParent, addon.JSHeadScriptPlatform5Src, addon.jsHeadScriptSrc); //(core.siteProperties.htmlPlatformVersion == 5 && !string.IsNullOrEmpty(addon.JSHeadScriptPlatform5Src)) ? addon.JSHeadScriptPlatform5Src : addon.jsHeadScriptSrc;
                    if (!string.IsNullOrEmpty(scriptUrl)) {
                        if (validMinJs && AddonModel.isAssetUrlLocal(core.cpParent, scriptUrl)) {
                            // -- local js, was included in minified
                        } else {
                            core.html.addScriptLinkSrc(scriptUrl, AddedByName + " Javascript Head Src", (executeContext.forceJavascriptToHead || addon.javascriptForceHead), addon.id);
                        }
                    }
                    if (!isDependencyThatAlreadyRan) {
                        //
                        // -- generate return content
                        AddonContentSourceEnum contentSourceId = AddonContentSourceEnum.All;
                        if (addon.contentSourceId != null) { contentSourceId = (AddonContentSourceEnum)addon.contentSourceId; }
                        //
                        // -- content delivered.
                        hint = 12;
                        string addon_copy = addon.copy;
                        string addon_copyText = addon.copyText;
                        string addon_pageTitle = addon.pageTitle;
                        string addon_metaDescription = addon.metaDescription;
                        string addon_metaKeywordList = addon.metaKeywordList;
                        string addon_otherHeadTags = addon.otherHeadTags;
                        string addon_formXML = addon.formXML;
                        string testString = (addon_copy + addon_copyText + addon_pageTitle + addon_metaDescription + addon_metaKeywordList + addon_otherHeadTags + addon_formXML).ToLowerInvariant();
                        if (!string.IsNullOrWhiteSpace(testString)) {
                            foreach (var key in core.docProperties.getKeyList()) {
                                if (testString.Contains(("$" + key + "$").ToLowerInvariant())) {
                                    string ReplaceSource = "$" + key + "$";
                                    string ReplaceValue = core.docProperties.getText(key);
                                    addon_copy = addon_copy.replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                    addon_copyText = addon_copyText.replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                    addon_pageTitle = addon_pageTitle.replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                    addon_metaDescription = addon_metaDescription.replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                    addon_metaKeywordList = addon_metaKeywordList.replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                    addon_otherHeadTags = addon_otherHeadTags.replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                    addon_formXML = addon_formXML.replace(ReplaceSource, ReplaceValue, StringComparison.CurrentCultureIgnoreCase);
                                }
                            }
                        }
                        hint = 13;
                        //
                        // -- text components
                        string contentParts = "";
                        contentParts += (contentSourceId == AddonContentSourceEnum.All || contentSourceId == AddonContentSourceEnum.ContentText) ? addon_copyText : "";
                        contentParts += (contentSourceId == AddonContentSourceEnum.All || contentSourceId == AddonContentSourceEnum.ContentWysiwyg) ? addon_copy : "";
                        if (!string.IsNullOrEmpty(contentParts) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextEditor)) {
                            //
                            // not editor, encode the content parts of the addon
                            //
                            switch (executeContext.addonType) {
                                case CPUtilsBaseClass.addonContext.ContextEditor:
                                    contentParts = ContentRenderController.renderHtmlForWysiwygEditor(core, contentParts);
                                    break;
                                case CPUtilsBaseClass.addonContext.ContextEmail:
                                    contentParts = ContentRenderController.renderHtmlForEmail(core, contentParts, core.session.user, "", false);
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
                                    contentParts = ContentRenderController.renderHtmlForWeb(core, contentParts, executeContext.hostRecord.contentName, executeContext.hostRecord.recordId, 0, "", 0, executeContext.addonType);
                                    break;
                                case CPUtilsBaseClass.addonContext.ContextOnContentChange:
                                case CPUtilsBaseClass.addonContext.ContextSimple:
                                    contentParts = ContentRenderController.renderHtmlForWeb(core, contentParts, "", 0, 0, "", 0, executeContext.addonType);
                                    break;
                                case CPUtilsBaseClass.addonContext.ContextRemoteMethodJson:
                                    contentParts = ContentRenderController.renderJSONForRemoteMethod(core, contentParts, "", 0, 0, "", executeContext.addonType);
                                    break;
                                case CPUtilsBaseClass.addonContext.ContextOnNewVisit:
                                    break;
                                case CPUtilsBaseClass.addonContext.ContextHelpUser:
                                    break;
                                case CPUtilsBaseClass.addonContext.ContextHelpAdmin:
                                    break;
                                case CPUtilsBaseClass.addonContext.ContextHelpDeveloper:
                                    break;
                                default:
                                    contentParts = ContentRenderController.renderHtmlForWeb(core, contentParts, "", 0, 0, "", 0, executeContext.addonType);
                                    break;
                            }
                            result.Append(contentParts);
                        }
                        //
                        // -- RemoteAssetLink
                        hint = 16;
                        if (!string.IsNullOrEmpty(addon.remoteAssetLink) && (contentSourceId == AddonContentSourceEnum.All || contentSourceId == AddonContentSourceEnum.RemoteAsset)) {
                            string RemoteAssetLink = addon.remoteAssetLink;
                            if (RemoteAssetLink.IndexOf("://", StringComparison.InvariantCulture) < 0) {
                                //
                                // use request object to build link
                                if (RemoteAssetLink.left(1) == "/") {
                                    // asset starts with a slash, add to appRoot
                                    RemoteAssetLink = core.webServer.requestProtocol + core.webServer.requestDomain + RemoteAssetLink;
                                } else {
                                    // asset is public files
                                    RemoteAssetLink = core.webServer.requestProtocol + core.webServer.requestDomain + core.appConfig.cdnFileUrl + RemoteAssetLink;
                                }
                            }
                            int PosStart = 0;
                            HttpController kmaHTTP = new();
                            string RemoteAssetContent = kmaHTTP.getURL(RemoteAssetLink);
                            int Pos = GenericController.strInstr(1, RemoteAssetContent, "<body", 1);
                            if (Pos > 0) {
                                Pos = GenericController.strInstr(Pos, RemoteAssetContent, ">");
                                if (Pos > 0) {
                                    PosStart = Pos + 1;
                                    Pos = GenericController.strInstr(Pos, RemoteAssetContent, "</body", 1);
                                    if (Pos > 0) {
                                        RemoteAssetContent = RemoteAssetContent.Substring(PosStart - 1, Pos - PosStart);
                                    }
                                }
                            }
                            result.Append(RemoteAssetContent);
                        }
                        //
                        // --  FormXML
                        hint = 17;
                        if (!string.IsNullOrEmpty(addon_formXML) && (contentSourceId == AddonContentSourceEnum.All || contentSourceId == AddonContentSourceEnum.FormExecution)) {
                            bool ExitAddonWithBlankResponse = false;
                            result.Append(execute_formContent(addon_formXML, ref ExitAddonWithBlankResponse, "addon [" + addon.name + "]"));
                            if (ExitAddonWithBlankResponse) {
                                //
                                // -- exit early
                                return string.Empty;
                            }
                        }
                        //
                        // -- Script Callback
                        hint = 18;
                        if (!string.IsNullOrEmpty(addon.link)) {
                            string callBackLink = encodeVirtualPath(addon.link, core.appConfig.cdnFileUrl, appRootPath, core.webServer.requestDomain);
                            foreach (var key in core.docProperties.getKeyList()) {
                                callBackLink = modifyLinkQuery(callBackLink, encodeRequestVariable(key), encodeRequestVariable(core.docProperties.getText(key)), true);
                            }
                            foreach (var kvp in executeContext.argumentKeyValuePairs) {
                                callBackLink = modifyLinkQuery(callBackLink, encodeRequestVariable(kvp.Key), encodeRequestVariable(core.docProperties.getText(kvp.Value)), true);
                            }
                            result.Append("<SCRIPT LANGUAGE=\"JAVASCRIPT\" SRC=\"" + callBackLink + "\"></SCRIPT>");
                        }
                        //
                        // -- non-js html assets (styles,head tags), set flag to block duplicates 
                        hint = 21;
                        bool addMetaData = false;
                        if (!core.doc.addonIdList_AddedMetedata.Contains(addon.id)) {
                            core.doc.addonIdList_AddedMetedata.Add(addon.id);
                            addMetaData = true;
                            //
                            // -- styles (use minify version)
                            bool validMinCss = core.siteProperties.allowMinify && !string.IsNullOrEmpty(addon.minifyStylesFilename.filename);
                            if (validMinCss) {
                                // -- minified if allow and minified styles exists
                                core.html.addStyleLink(getCdnFileLink(core, addon.minifyStylesFilename.filename), addon.name + " Minified Stylesheet");
                            } else if (!string.IsNullOrEmpty(addon.stylesFilename.filename)) {
                                // -- non-minified if not allow, no minified styles, and rawStyles exist
                                core.html.addStyleLink(getCdnFileLink(core, addon.stylesFilename.filename), addon.name + " Stylesheet");
                            }
                            //
                            // -- link to stylesheet if not in minified styles. if allowmin and url starts with '/', it was in the min. 
                            string cssUrl = AddonModel.getPlatformAsset(core.cpParent, addon.StylesLinkPlatform5Href, addon.stylesLinkHref); // (core.siteProperties.htmlPlatformVersion == 5 && !string.IsNullOrEmpty(addon.StylesLinkPlatform5Href)) ? addon.StylesLinkPlatform5Href : addon.stylesLinkHref;
                            if (!string.IsNullOrEmpty(cssUrl)) {
                                if (validMinCss && AddonModel.isAssetUrlLocal(core.cpParent, cssUrl)) {  // (styleSheetUrl.Substring(0, 1) == "/" && styleSheetUrl.Substring(0, 2) != "//")) {
                                    // -- url is wrapped up in minified styles
                                } else {
                                    // -- style link is not allow-minified, or it does not begin with "/"
                                    core.html.addStyleLink(cssUrl, addon.name + " Stylesheet Link");
                                }
                            }
                        }
                        //
                        // -- add scripting and dotnet code last, so if the execution adds javascript to the head, the code in the fields is first.
                        // -- Scripting code
                        hint = 14;
                        if (!string.IsNullOrEmpty(addon.scriptingCode) && (contentSourceId == AddonContentSourceEnum.All || contentSourceId == AddonContentSourceEnum.ScriptingCodeExecution)) {
                            try {
                                if (addon.scriptingLanguageId == (int)ScriptLanguages.Javascript) {
                                    result.Append(AddonScriptController.execute_Script_JScript(core, ref addon));
                                } else {
                                    result.Append(AddonScriptController.execute_Script_VBScript(core, ref addon));
                                }
                            } catch (Exception ex) {
                                //
                                // -- exeption in outside code
                                LogController.logError(core, ex, "Exception in script component of addon [" + getAddonDescription(core, addon) + "]");
                            }
                        }
                        //
                        // -- DotNet
                        hint = 15;
                        if (!string.IsNullOrEmpty(addon.dotNetClass) && (contentSourceId == AddonContentSourceEnum.All || contentSourceId == AddonContentSourceEnum.DotNetCodeExecution)) {
                            try {
                                //
                                // -- executing outside code, swallow exceptions
                                result.Append(execute_dotNetClass(addon, AddonCollectionModel.create<AddonCollectionModel>(core.cpParent, addon.collectionId)));
                            } catch (Exception ex) {
                                //
                                // -- exeption in outside code
                                LogController.logError(core, ex, "Exception in dotnet component of addon [" + getAddonDescription(core, addon) + "]");
                            }
                        }
                        //
                        // -- add meta data elements after all code execution so metadata added by code appears first (more specific data before less specific data)
                        if (addMetaData) {
                            core.html.addTitle(addon_pageTitle, AddedByName);
                            core.html.addMetaDescription(addon_metaDescription, AddedByName);
                            core.html.addMetaKeywordList(addon_metaKeywordList, AddedByName);
                            core.html.addHeadTag(addon_otherHeadTags, AddedByName);
                        }
                    }
                }
                hint = 23;
                if (!isDependencyThatAlreadyRan) {
                    //
                    //   Add Wrappers to content
                    //
                    if (addon.inFrame && (executeContext.addonType == CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml)) {
                        //
                        // -- iFrame content, framed in content, during the remote method call, add in the rest of the html page
                        hint = 24;

                        result = new StringBuilder(""
                            + core.siteProperties.docTypeDeclaration
                            + Environment.NewLine + "<html lang=\"en-US\">"
                            + Environment.NewLine + "<head>"
                            + core.html.getHtmlHead()
                            + Environment.NewLine + "</head>"
                            + Environment.NewLine + TemplateDefaultBodyTag
                            + result.ToString()
                            + Environment.NewLine + "</body>"
                            + Environment.NewLine + "</html>"
                            );
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
                        // -- Add Css containers
                        hint = 22;
                        if (!string.IsNullOrEmpty(containerCssId) || !string.IsNullOrEmpty(ContainerCssClass)) {
                            if (addon.isInline) {
                                result = new StringBuilder("\r<span id=\"" + containerCssId + "\" class=\"" + ContainerCssClass + "\" style=\"display:inline;\">" + result.ToString() + "</span>");
                            } else {
                                result = new StringBuilder("\r<div id=\"" + containerCssId + "\" class=\"" + ContainerCssClass + "\">" + result.ToString() + "\r</div>");
                            }
                        }
                        //
                        // -- Return all other types, Enable Edit Wrapper for Page Content edit mode

                        hint = 25;
                        bool IncludeEditWrapper = (!addon.blockEditTools) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextEditor) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextEmail) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodJson) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextSimple) && (!executeContext.isDependency);
                        if (IncludeEditWrapper) {
                            IncludeEditWrapper = IncludeEditWrapper && (allowAdvanceEditor && ((executeContext.addonType == CPUtilsBaseClass.addonContext.ContextAdmin) || core.session.isEditing(executeContext.hostRecord.contentName)));
                            if (IncludeEditWrapper) {
                                //
                                // Edit Icon
                                var editSegmentList = new List<string> {
                                            AdminUIController.getAddonEditSegment(core, addon.id, addon.name)
                                        };
                                result = new StringBuilder(AdminUIController.getAddonEditAnchorTag(core, editSegmentList) + result.ToString());
                                result = new StringBuilder(AdminUIController.getEditWrapper(core, result.ToString()));
                            }
                        }
                        //
                        // -- Add Comment wrapper, to help debugging except email, remote methods and admin (empty is used to detect no result)
                        hint = 26;
                        if (true && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextAdmin) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextEmail) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodHtml) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextRemoteMethodJson) && (executeContext.addonType != CPUtilsBaseClass.addonContext.ContextSimple)) {
                            if (core.visitProperty.getBoolean("AllowDebugging")) {
                                string AddonCommentName = GenericController.strReplace(addon.name, "-->", "..>");
                                if (addon.isInline) {
                                    result = new StringBuilder("<!-- Add-on " + AddonCommentName + " -->" + result.ToString() + "<!-- /Add-on " + AddonCommentName + " -->");
                                } else {
                                    result = new StringBuilder("\r<!-- Add-on " + AddonCommentName + " -->" + result.ToString() + "\r<!-- /Add-on " + AddonCommentName + " -->");
                                }
                            }
                        }
                        //
                        // -- Add Design Wrapper
                        hint = 27;
                        if ((result.Length > 0) && (!addon.isInline) && (executeContext.wrapperID > 0)) {
                            result = new StringBuilder(addWrapperToResult(result.ToString(), executeContext.wrapperID, "for Add-on " + addon.name));
                        }
                        // -- restore the parent's instanceId
                        hint = 28;
                        core.docProperties.setProperty("instanceId", parentInstanceId);
                    }
                }
                //
                // -- unwind recursion count
                hint = 29;
                if (core.doc.addonDeveloperRecursionCount.ContainsKey(addon.id)) {
                    if (--core.doc.addonDeveloperRecursionCount[addon.id] <= 0) {
                        core.doc.addonDeveloperRecursionCount.Remove(addon.id);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex, "hint [" + hint + "]");
            } finally {
                //
                // -- this completes the execute of this core.addon. remove it from the 'running' list
                core.doc.addonInstanceCnt = core.doc.addonInstanceCnt + 1;
                //
                // -- restore the parent instanceid
                core.docProperties.setProperty("instanceId", parentInstanceId);
                //
                // -- html-only addons
                if (executeContext.forceHtmlDocument || addon.htmlDocument) {
                    //
                    // -- if the executed content includes content cmds, we cant guarantee it didnt come from user data
                    if (core.siteProperties.beta200327_BlockCCmdCodeAfterAddonExec) {
                        result = new StringBuilder(result.ToString().Replace("{%", "{_%").Replace("%}", "%_}"));
                    }
                    //
                    // -- if root level addon, and the addon is an html document, create the html document around it and uglify if not debugging
                    if (executeContext.forceHtmlDocument || (rootLevelAddon && addon.htmlDocument)) {
                        //
                        // -- on body end addons (as long  as this addon is not onbodystart also
                        if (!addon.onBodyEnd) {
                            //
                            // -- on body end addons
                            core.doc.body = result.ToString();
                            string addonResult = core.addon.executeOnBodyEnd();
                            result = new StringBuilder(core.doc.body + addonResult);
                        } else {
                            logger.Warn("Addon [" + addon.id + ", " + addon.name + "] is run as an Html Document addon but it is marked onBodyEnd. onBodyEnd was ignored.");
                        }
                        //
                        // -- create html document from returned body
                        result = new StringBuilder(core.html.getHtmlDoc(result.ToString(), "<body>"));
                        //
                        // -- minify results
                        if ((!core.doc.visitPropertyAllowDebugging) && (core.siteProperties.getBoolean("Allow Html Minify", true))) {
                            result = new StringBuilder(NUglify.Uglify.Html(result.ToString()).Code);
                        }
                    }
                }
                //
                // -- log remote method payload size
                if (addon.remoteMethod) {
                    StackFrame frame = new(1);
                    MethodBase method = frame.GetMethod();
                    LogController.logTrace(core, "addon [" + addon.id + "." + addon.name + "], method [" + method.ReflectedType.Name + "." + method.Name + "], visit [" + core.session.visit.id + "], payload [" + result.Length + "], time [" + sw.ElapsedMilliseconds + "ms]");
                }
                //
                // -- remove this addon from the recursion test stack
                // -- add all addons to the dependencyrecursionTestStack, only test them if they are dependencies - but block them even if they were first run as non-dependencies
                executeContext.dependencyRecursionTestStack.Pop();
                //
                // -- pop modelstack and test point message
                core.doc.addonExecutionStack.Pop();
            }
            return result.ToString();
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute the xml part of an addon, return html
        /// </summary>
        /// <param name="FormXML"></param>
        /// <param name="return_ExitAddonBlankWithResponse"></param>
        /// <param name="contextErrorMessage"></param>
        /// <returns></returns>
        private string execute_formContent(string FormXML, ref bool return_ExitAddonBlankWithResponse, string contextErrorMessage) {
            string result = "";
            try {
                string Button = core.docProperties.getText(RequestNameButton);
                StringBuilderLegacyController Content = new();
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
                    Content.add(AdminUIController.getFormBodyAdminOnly());
                } else {
                    {
                        bool loadOK = true;
                        XmlDocument Doc = new();
                        try {
                            Doc.LoadXml(FormXML);
                        } catch (Exception ex) {
                            LogController.logError(core, ex);
                            ButtonList = ButtonCancel;
                            Content.add("<div class=\"ccError\" style=\"margin:10px;padding:10px;background-color:white;\">There was a problem with the Setting Page you requested.</div>");
                            loadOK = false;
                        }
                        if (loadOK) {
                            //
                            // data is OK
                            //
                            if (GenericController.toLCase(Doc.DocumentElement.Name) != "form") {
                                //
                                // error - Need a way to reach the user that submitted the file
                                //
                                ButtonList = ButtonCancel;
                                Content.add("<div class=\"ccError\" style=\"margin:10px;padding:10px;background-color:white;\">There was a problem with the Setting Page you requested.</div>");
                            } else {
                                // todo - move locals
                                string fieldfilename = "";
                                string fieldValue = "";
                                bool IsFound = false;
                                string fieldName = "";
                                bool fieldReadOnly = false;
                                bool FieldHTML = false;
                                string fieldType = "";
                                //
                                // ----- Process Requests
                                //
                                if ((Button == ButtonSave) || (Button == ButtonOK)) {
                                    foreach (XmlNode SettingNode in Doc.DocumentElement.ChildNodes) {
                                        if (SettingNode.Name.ToLower(CultureInfo.InvariantCulture) == "tab") {
                                            foreach (XmlNode TabNode in SettingNode.ChildNodes) {
                                                string Filename = null;
                                                string DefaultFilename = null;
                                                switch (TabNode.Name.ToLower(CultureInfo.InvariantCulture)) {
                                                    case "siteproperty": {
                                                            //
                                                            fieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                            fieldValue = core.docProperties.getText(fieldName);
                                                            fieldType = xml_GetAttribute(IsFound, TabNode, "type", "");
                                                            switch (GenericController.toLCase(fieldType)) {
                                                                case "integer": {
                                                                        //
                                                                        if (!string.IsNullOrEmpty(fieldValue)) {
                                                                            fieldValue = GenericController.encodeInteger(fieldValue).ToString();
                                                                        }
                                                                        core.siteProperties.setProperty(fieldName, fieldValue);
                                                                        break;
                                                                    }
                                                                case "boolean": {
                                                                        //
                                                                        if (!string.IsNullOrEmpty(fieldValue)) {
                                                                            fieldValue = GenericController.encodeBoolean(fieldValue).ToString();
                                                                        }
                                                                        core.siteProperties.setProperty(fieldName, fieldValue);
                                                                        break;
                                                                    }
                                                                case "float": {
                                                                        //
                                                                        if (!string.IsNullOrEmpty(fieldValue)) {
                                                                            fieldValue = encodeNumber(fieldValue).ToString();
                                                                        }
                                                                        core.siteProperties.setProperty(fieldName, fieldValue);
                                                                        break;
                                                                    }
                                                                case "date": {
                                                                        //
                                                                        if (encodeDateMinValue(encodeDate(fieldValue)).Equals(DateTime.MinValue)) {
                                                                            //
                                                                            // -- value is not a valid date, save empty
                                                                            fieldValue = "";
                                                                        }
                                                                        core.siteProperties.setProperty(fieldName, fieldValue);
                                                                        break;
                                                                    }
                                                                case "file":
                                                                case "imagefile": {
                                                                        //
                                                                        if (core.docProperties.getBoolean(fieldName + ".DeleteFlag")) {
                                                                            core.siteProperties.setProperty(fieldName, "");
                                                                        }
                                                                        if (!string.IsNullOrEmpty(fieldValue)) {
                                                                            Filename = fieldValue;
                                                                            string VirtualFilePath = "settings/" + fieldName + "/";
                                                                            core.cdnFiles.upload(fieldName, VirtualFilePath, ref Filename);
                                                                            core.siteProperties.setProperty(fieldName, VirtualFilePath + Filename);
                                                                        }
                                                                        break;
                                                                    }
                                                                case "privatefile": {
                                                                        //
                                                                        if (core.docProperties.getBoolean(fieldName + ".DeleteFlag")) {
                                                                            core.siteProperties.setProperty(fieldName, "");
                                                                        }
                                                                        if (!string.IsNullOrEmpty(fieldValue)) {
                                                                            Filename = fieldValue;
                                                                            string VirtualFilePath = "settings/" + fieldName + "/";
                                                                            core.privateFiles.upload(fieldName, VirtualFilePath, ref Filename);
                                                                            core.siteProperties.setProperty(fieldName, VirtualFilePath + Filename);
                                                                        }
                                                                        break;
                                                                    }
                                                                case "textfile": {
                                                                        //
                                                                        DefaultFilename = "settings/" + fieldName + ".txt";
                                                                        Filename = core.siteProperties.getText(fieldName, DefaultFilename);
                                                                        if (string.IsNullOrEmpty(Filename)) {
                                                                            Filename = DefaultFilename;
                                                                            core.siteProperties.setProperty(fieldName, DefaultFilename);
                                                                        }
                                                                        core.privateFiles.saveFile(Filename, fieldValue);
                                                                        break;
                                                                    }
                                                                case "cssfile": {
                                                                        //
                                                                        // ** deprecate ** install css and js files as resources, use with addons, not settings (and not in wwwroot)
                                                                        DefaultFilename = "settings/" + fieldName + ".css";
                                                                        Filename = core.siteProperties.getText(fieldName, DefaultFilename);
                                                                        if (string.IsNullOrEmpty(Filename)) {
                                                                            Filename = DefaultFilename;
                                                                            core.siteProperties.setProperty(fieldName, DefaultFilename);
                                                                        }
                                                                        core.wwwFiles.saveFile(Filename, fieldValue);
                                                                        break;
                                                                    }
                                                                case "xmlfile": {
                                                                        //
                                                                        // ** deprecate ** install xml files as resources, not settings (and not in wwwroot)
                                                                        DefaultFilename = "settings/" + fieldName + ".xml";
                                                                        Filename = core.siteProperties.getText(fieldName, DefaultFilename);
                                                                        if (string.IsNullOrEmpty(Filename)) {
                                                                            Filename = DefaultFilename;
                                                                            core.siteProperties.setProperty(fieldName, DefaultFilename);
                                                                        }
                                                                        core.wwwFiles.saveFile(Filename, fieldValue);
                                                                        break;
                                                                    }
                                                                case "currency": {
                                                                        //
                                                                        if (!string.IsNullOrEmpty(fieldValue)) {
                                                                            fieldValue = encodeNumber(fieldValue).ToString();
                                                                            fieldValue = String.Format("C", fieldValue);
                                                                        }
                                                                        core.siteProperties.setProperty(fieldName, fieldValue);
                                                                        break;
                                                                    }
                                                                case "link": {
                                                                        core.siteProperties.setProperty(fieldName, fieldValue);
                                                                        break;
                                                                    }
                                                                default: {
                                                                        core.siteProperties.setProperty(fieldName, fieldValue);
                                                                        break;
                                                                    }
                                                            }
                                                            break;
                                                        }
                                                    case "copycontent": {
                                                            //
                                                            // A Copy Content block
                                                            //
                                                            fieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                            if (!fieldReadOnly) {
                                                                fieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                                FieldHTML = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "html", "false"));
                                                                if (FieldHTML) {
                                                                    //
                                                                    // treat html as active content for now.
                                                                    //
                                                                    fieldValue = core.docProperties.getTextFromWysiwygEditor(fieldName);
                                                                } else {
                                                                    fieldValue = core.docProperties.getText(fieldName);
                                                                }
                                                                using var csData = new CsModel(core);
                                                                csData.open("Copy Content", "name=" + DbController.encodeSQLText(fieldName), "ID");
                                                                if (!csData.ok()) {
                                                                    csData.close();
                                                                    csData.insert("Copy Content");
                                                                }
                                                                if (csData.ok()) {
                                                                    csData.set("name", fieldName);
                                                                    //
                                                                    // Set copy
                                                                    //
                                                                    csData.set("copy", fieldValue);
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
                                                            break;

                                                        }
                                                    case "filecontent": {
                                                            //
                                                            // A File Content block
                                                            //
                                                            fieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                            if (!fieldReadOnly) {
                                                                fieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                                fieldfilename = xml_GetAttribute(IsFound, TabNode, "filename", "");
                                                                fieldValue = core.docProperties.getText(fieldName);
                                                                core.cdnFiles.saveFile(fieldfilename, fieldValue);
                                                            }
                                                            break;
                                                        }
                                                    case "dbquery": {
                                                            //
                                                            // dbquery has no results to process
                                                            //
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
                                Name = xml_GetAttribute(IsFound, Doc.DocumentElement, "name", "");
                                int TabCnt = 0;
                                var adminMenu = new EditTabModel();
                                foreach (XmlNode SettingNode in Doc.DocumentElement.ChildNodes) {
                                    string Copy = "";
                                    string TabName = "";
                                    string TabDescription = "";
                                    string TabHeading = "";
                                    StringBuilderLegacyController TabCell = null;
                                    switch (GenericController.toLCase(SettingNode.Name)) {
                                        case "description": {
                                                Description = SettingNode.InnerText;
                                                break;
                                            }
                                        case "tab": {
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
                                                    switch (GenericController.toLCase(TabNode.Name)) {
                                                        case "heading": {
                                                                //
                                                                // Heading
                                                                FieldCaption = xml_GetAttribute(IsFound, TabNode, "caption", "");
                                                                TabCell.add(AdminUIController.getEditSubheadRow(core, FieldCaption));
                                                                break;
                                                            }
                                                        case "siteproperty": {
                                                                //
                                                                // Site property
                                                                fieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                                if (!string.IsNullOrEmpty(fieldName)) {
                                                                    FieldCaption = xml_GetAttribute(IsFound, TabNode, "caption", "");
                                                                    if (string.IsNullOrEmpty(FieldCaption)) {
                                                                        FieldCaption = fieldName;
                                                                    }
                                                                    fieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                                    FieldHTML = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "html", ""));
                                                                    fieldType = xml_GetAttribute(IsFound, TabNode, "type", "");
                                                                    string fieldSelector = xml_GetAttribute(IsFound, TabNode, "selector", "");
                                                                    FieldDescription = xml_GetAttribute(IsFound, TabNode, "description", "");
                                                                    string FieldAddon = xml_GetAttribute(IsFound, TabNode, "EditorAddon", "");
                                                                    FieldDefaultValue = TabNode.InnerText;
                                                                    fieldValue = core.siteProperties.getText(fieldName, FieldDefaultValue);
                                                                    if (!string.IsNullOrEmpty(FieldAddon)) {
                                                                        //
                                                                        // Use Editor Addon
                                                                        Dictionary<string, string> arguments = new Dictionary<string, string> {
                                                                            { "FieldName", fieldName },
                                                                            { "FieldValue", core.siteProperties.getText(fieldName, FieldDefaultValue) }
                                                                        };
                                                                        AddonModel addon = core.cacheStore.addonCache.createByUniqueName(FieldAddon);
                                                                        //if (addon == null) { addon = AddonModel.createByUniqueName(core.cpParent, FieldAddon); };
                                                                        Copy = core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                                                            addonType = CPUtilsBaseClass.addonContext.ContextAdmin,
                                                                            argumentKeyValuePairs = arguments,
                                                                            errorContextMessage = "executing field addon [" + FieldAddon + "] for " + contextErrorMessage
                                                                        });
                                                                    } else if (!string.IsNullOrEmpty(fieldSelector)) {
                                                                        //
                                                                        // Use Selector
                                                                        Copy = AdminUIEditorController.getSelectorStringEditor(core, fieldName, fieldValue, fieldSelector);
                                                                    } else {
                                                                        //
                                                                        // Use default editor for each field type
                                                                        switch (GenericController.toLCase(fieldType)) {
                                                                            case "integer": {
                                                                                    //
                                                                                    Copy = AdminUIEditorController.getIntegerEditor(core, fieldName, encodeIntegerNullable(fieldValue), fieldReadOnly, "", false, "");
                                                                                    break;
                                                                                }
                                                                            case "boolean": {
                                                                                    Copy = AdminUIEditorController.getBooleanEditor(core, fieldName, encodeBoolean(fieldValue), fieldReadOnly, "");
                                                                                    break;
                                                                                }
                                                                            case "double":
                                                                            case "number":
                                                                            case "float": {
                                                                                    Copy = AdminUIEditorController.getNumberEditor(core, fieldName, encodeNumberNullable(fieldValue), fieldReadOnly, "", false, "");
                                                                                    break;
                                                                                }
                                                                            case "date": {
                                                                                    Copy = AdminUIEditorController.getDateTimeEditor(core, fieldName, encodeDate(fieldValue), fieldReadOnly, "", false);
                                                                                    break;
                                                                                }
                                                                            case "file": {
                                                                                    Copy = AdminUIEditorController.getFileEditor(core, fieldName, fieldValue, fieldReadOnly, "", false, "");
                                                                                    break;
                                                                                }
                                                                            case "privatefile": {
                                                                                    Copy = AdminUIEditorController.getPrivateFileEditor(core, fieldName, fieldValue, fieldReadOnly, "", false, "");
                                                                                    break;
                                                                                }
                                                                            case "imagefile": {
                                                                                    Copy = AdminUIEditorController.getImageEditor(core, fieldName, fieldValue, fieldValue, fieldReadOnly, "");
                                                                                    break;
                                                                                }
                                                                            case "currency": {
                                                                                    //
                                                                                    Copy = AdminUIEditorController.getCurrencyEditor(core, fieldName, encodeNumber(fieldValue), fieldReadOnly, "", false, "");
                                                                                    break;
                                                                                }
                                                                            case "textfile": {
                                                                                    string fileValue = core.privateFiles.readFileText(fieldValue);
                                                                                    if (fieldReadOnly) {
                                                                                        Copy = fileValue + HtmlController.inputHidden(fieldName, fileValue);
                                                                                    } else {
                                                                                        Copy = AdminUIEditorController.getLongTextEditor(core, fieldName, fileValue, fieldReadOnly, "", false, "");
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "cssfile": {
                                                                                    string fileValue = core.privateFiles.readFileText(fieldValue);
                                                                                    if (fieldReadOnly) {
                                                                                        Copy = fieldValue + HtmlController.inputHidden(fieldName, fileValue);
                                                                                    } else {
                                                                                        Copy = AdminUIEditorController.getLongTextEditor(core, fieldName, fileValue, fieldReadOnly, "", false, "");
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "xmlfile": {
                                                                                    string fileValue = core.privateFiles.readFileText(fieldValue);
                                                                                    if (fieldReadOnly) {
                                                                                        Copy = fieldValue + HtmlController.inputHidden(fieldName, fileValue);
                                                                                    } else {
                                                                                        Copy = AdminUIEditorController.getLongTextEditor(core, fieldName, fileValue, fieldReadOnly, "", false, "");
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "link": {
                                                                                    if (fieldReadOnly) {
                                                                                        Copy = fieldValue + HtmlController.inputHidden(fieldName, fieldValue);
                                                                                    } else {
                                                                                        Copy = AdminUIEditorController.getTextEditor(core, fieldName, fieldValue, fieldReadOnly, "", false);
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            default: {
                                                                                    //
                                                                                    // text
                                                                                    if (FieldHTML) {
                                                                                        Copy = AdminUIEditorController.getHtmlEditor(core, fieldName, fieldValue, "", "", "", fieldReadOnly);
                                                                                    } else {
                                                                                        Copy = AdminUIEditorController.getTextEditor(core, fieldName, fieldValue, fieldReadOnly, "", false);
                                                                                    }
                                                                                    break;
                                                                                }
                                                                        }
                                                                    }
                                                                    TabCell.add(AdminUIController.getEditRowLegacy(core, Copy, FieldCaption, FieldDescription, false, false, ""));
                                                                }
                                                                break;
                                                            }
                                                        case "copycontent": {
                                                                //
                                                                // Content Copy field
                                                                fieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                                if (!string.IsNullOrEmpty(fieldName)) {
                                                                    FieldCaption = xml_GetAttribute(IsFound, TabNode, "caption", "");
                                                                    if (string.IsNullOrEmpty(FieldCaption)) {
                                                                        FieldCaption = fieldName;
                                                                    }
                                                                    fieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                                    FieldDescription = xml_GetAttribute(IsFound, TabNode, "description", "");
                                                                    FieldHTML = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "html", ""));
                                                                    //
                                                                    using (var csData = new CsModel(core)) {
                                                                        csData.open("Copy Content", "Name=" + DbController.encodeSQLText(fieldName), "ID", false, 0, "id,name,Copy");
                                                                        if (!csData.ok()) {
                                                                            csData.close();
                                                                            csData.insert("Copy Content");
                                                                            if (csData.ok()) {
                                                                                int RecordId = csData.getInteger("ID");
                                                                                csData.set("name", fieldName);
                                                                                csData.set("copy", GenericController.encodeText(TabNode.InnerText));
                                                                                csData.save();
                                                                            }
                                                                        }
                                                                        if (csData.ok()) {
                                                                            fieldValue = csData.getText("copy");
                                                                        }
                                                                    }
                                                                    if (FieldHTML) {
                                                                        Copy = AdminUIEditorController.getHtmlEditor(core, fieldName, fieldValue, "", "", "", fieldReadOnly);
                                                                    } else {
                                                                        Copy = AdminUIEditorController.getTextEditor(core, fieldName, fieldValue, fieldReadOnly);
                                                                    }
                                                                    TabCell.add(AdminUIController.getEditRowLegacy(core, Copy, FieldCaption, FieldDescription, false, false, ""));
                                                                }
                                                                break;
                                                            }
                                                        case "filecontent": {
                                                                //
                                                                // Content from a flat file
                                                                //
                                                                fieldName = xml_GetAttribute(IsFound, TabNode, "name", "");
                                                                FieldCaption = xml_GetAttribute(IsFound, TabNode, "caption", "");
                                                                fieldfilename = xml_GetAttribute(IsFound, TabNode, "filename", "");
                                                                fieldReadOnly = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "readonly", ""));
                                                                FieldDescription = xml_GetAttribute(IsFound, TabNode, "description", "");
                                                                FieldDefaultValue = TabNode.InnerText;
                                                                fieldValue = FieldDefaultValue;
                                                                FieldHTML = GenericController.encodeBoolean(xml_GetAttribute(IsFound, TabNode, "html", ""));
                                                                if (!string.IsNullOrEmpty(fieldfilename)) {
                                                                    if (core.cdnFiles.fileExists(fieldfilename)) {
                                                                        fieldValue = core.cdnFiles.readFileText(fieldfilename);
                                                                    }
                                                                }
                                                                if (FieldHTML) {
                                                                    Copy = AdminUIEditorController.getHtmlEditor(core, fieldName, fieldValue, "", "", "", fieldReadOnly);
                                                                } else {
                                                                    Copy = AdminUIEditorController.getLongTextEditor(core, fieldName, fieldValue, fieldReadOnly, "", false, "");
                                                                }
                                                                TabCell.add(AdminUIController.getEditRowLegacy(core, Copy, FieldCaption, FieldDescription, false, false, ""));
                                                                break;
                                                            }
                                                        case "dbquery":
                                                        case "querydb":
                                                        case "query":
                                                        case "db": {
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
                                                                        using var db = new DbController(core, FieldDataSource);
                                                                        dt = core.db.executeQuery(FieldSQL, 0, SQLPageSize);
                                                                    } catch (Exception ex) {
                                                                        LogController.logError(core, ex);
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
                                                                                    // Build headers
                                                                                    //
                                                                                    int FieldCount = dr.ItemArray.Length;
                                                                                    Copy += ("\r<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"border-bottom:1px solid #444;border-right:1px solid #444;background-color:white;color:#444;\">");
                                                                                    Copy += ("\r\t<tr>");
                                                                                    foreach (DataColumn dc in dr.ItemArray) {
                                                                                        Copy += ("\r\t\t<td class=\"ccadminsmall\" style=\"border-top:1px solid #444;border-left:1px solid #444;color:black;padding:2px;padding-top:4px;padding-bottom:4px;\">" + dr[dc] + "</td>");
                                                                                    }
                                                                                    Copy += ("\r\t</tr>");
                                                                                    //
                                                                                    // Build output table
                                                                                    //
                                                                                    string RowStart = "\r\t<tr>";
                                                                                    string RowEnd = "\r\t</tr>";
                                                                                    string ColumnStart = "\r\t\t<td class=\"ccadminnormal\" style=\"border-top:1px solid #444;border-left:1px solid #444;background-color:white;color:#444;padding:2px\">";
                                                                                    string ColumnEnd = "</td>";
                                                                                    int RowMax = 0;
                                                                                    for (int RowPointer = 0; RowPointer <= RowMax; RowPointer++) {
                                                                                        Copy += (RowStart);
                                                                                        int ColumnPointer = 0;
                                                                                        int ColumnMax = 0;
                                                                                        for (ColumnPointer = 0; ColumnPointer <= ColumnMax; ColumnPointer++) {
                                                                                            object CellData = something[ColumnPointer, RowPointer];
                                                                                            if (isNull(CellData)) {
                                                                                                Copy += (ColumnStart + "[null]" + ColumnEnd);
                                                                                            } else if ((CellData == null)) {
                                                                                                Copy += (ColumnStart + "[empty]" + ColumnEnd);
                                                                                            } else if (Microsoft.VisualBasic.Information.IsArray(CellData)) {
                                                                                                Copy += ColumnStart + "[array]";
                                                                                            } else if (string.IsNullOrEmpty(encodeText(CellData))) {
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
                                                                TabCell.add(AdminUIController.getEditRow(core, Copy, FieldCaption, FieldDescription, false, false, ""));
                                                                break;
                                                            }
                                                        default: {
                                                                break;
                                                            }
                                                    }
                                                }
                                                Copy = AdminUIController.getEditPanel(core, true, TabHeading, TabDescription, AdminUIController.editTable(TabCell.text));
                                                if (!string.IsNullOrEmpty(Copy)) {
                                                    adminMenu.addEntry(TabName.Replace(" ", "&nbsp;"), Copy, "ccAdminTab");
                                                }
                                                TabCell = null;
                                                break;
                                            }
                                        default: {
                                                break;
                                            }
                                    }
                                }
                                //
                                // Buttons
                                //
                                ButtonList = ButtonCancel + "," + ButtonSave + "," + ButtonOK;
                                if (TabCnt > 0) { Content.add(adminMenu.getTabs(core)); }
                            }
                        }
                    }
                }
                //
                result = AdminUIController.getToolBody(core, Name, ButtonList, "", true, true, Description, "", 0, Content.text);
                Content = null;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
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
        private string execute_dotNetClass(AddonModel addon, AddonCollectionModel addonCollection) {
            string result = "";
            try {
                LogController.logTrace(core, "execute_assembly dotNetClass [" + addon.dotNetClass + "], enter");
                //
                // -- purpose is to have a repository where addons can be stored for web and non-web apps, and allow permissions to be installed with online upload
                string warningMessage = "The addon [" + addon.name + "] dotnet code could not be executed because no assembly was found with namespace [" + addon.dotNetClass + "].";
                if ((addonCollection == null) || (string.IsNullOrEmpty(addonCollection.ccguid))) { throw new GenericException(warningMessage + " The addon dotnet assembly could not be run because no collection is set, or the collection guid is empty."); }
                //
                // -- has addon been found before
                string assemblyFileDictKey = (addonCollection.ccguid + addon.dotNetClass).ToLower(CultureInfo.InvariantCulture);
                if (core.cacheStore.assemblyFileDict.ContainsKey(assemblyFileDictKey)) {
                    return execute_dotNetClass_assembly(addon, core.cacheStore.assemblyFileDict[assemblyFileDictKey].pathFilename);
                }
                //
                // -- try to find addon in current executing path (built in addons)
                bool AddonFound = false;
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                string appPath = Path.GetDirectoryName(path);
                result = execute_dotNetClass_byPath(addon, assemblyFileDictKey, appPath, ref AddonFound);
                if (AddonFound) { return result; }
                //
                // -- try addon folder
                var collectionFolderConfig = CollectionFolderModel.getCollectionFolderConfig(core, addonCollection.ccguid);
                if (collectionFolderConfig == null) {
                    throw new GenericException(warningMessage + ", not found in application path [" + appPath + "]. The collection path was not checked because the collection [" + addonCollection.name + "] was not found in the \\private\\addons\\Collections.xml file. Try re-installing the collection");
                };
                if (string.IsNullOrEmpty(collectionFolderConfig.path)) {
                    throw new GenericException(warningMessage + ", not found in application path [" + appPath + "]. The collection path was not checked because the path for collection [" + addonCollection.name + "] was not valid in the \\private\\addons\\Collections.xml file. Try re-installing the collection");
                };
                string AddonPath = core.privateFiles.joinPath(getPrivateFilesAddonPath(), collectionFolderConfig.path);
                if (!core.privateFiles.pathExists_local(AddonPath)) { core.privateFiles.copyPathRemoteToLocal(AddonPath); }
                string appAddonPath = core.privateFiles.joinPath(core.privateFiles.localAbsRootPath, AddonPath);
                result = execute_dotNetClass_byPath(addon, assemblyFileDictKey, appAddonPath, ref AddonFound);
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
        /// <param name="dosAbsPath"></param>
        /// <param name="assemblyFileDictKey"></param>
        /// <param name="addonFound"></param>
        /// <returns></returns>
        private string execute_dotNetClass_byPath(AddonModel addon, string assemblyFileDictKey, string dosAbsPath, ref bool addonFound) {
            try {
                addonFound = false;
                if (!Directory.Exists(dosAbsPath)) { return string.Empty; }
                foreach (var testDosAbsPathFilename in Directory.GetFileSystemEntries(dosAbsPath, "*.dll")) {
                    //
                    // -- tmp test skipping the non-addon list, depend on found-addon list instead
                    if (!string.IsNullOrEmpty(core.assemblyList_NonAddonsInstalled.Find(x => testDosAbsPathFilename.ToLower(CultureInfo.InvariantCulture).right(x.Length) == x))) {
                        //
                        // -- this assembly is a non-addon installed file, block full path
                        continue;
                    };
                    string returnValue = execute_dotNetClass_assembly(addon, testDosAbsPathFilename, ref addonFound);
                    if (addonFound) {
                        //
                        // -- addon found, save addonsFound list and return the addon result
                        // -- lock to prevent change between containsKey and add
                        object lockObj = new object();
                        lock (lockObj) {
                            if (!core.cacheStore.assemblyFileDict.ContainsKey(assemblyFileDictKey)) {
                                core.cacheStore.assemblyFileDict.Add(assemblyFileDictKey, new AssemblyFileDetails {
                                    pathFilename = testDosAbsPathFilename,
                                    path = ""
                                });
                                core.cacheStore.assemblyFileDict_save();
                            }
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
        /// <param name="assemblyPrivateAbsPathFilename"></param>
        /// <param name="addonFound">If found, the search for the assembly can be abandoned</param>
        /// <returns></returns>
        private string execute_dotNetClass_assembly(AddonModel addon, string assemblyPrivateAbsPathFilename, ref bool addonFound) {
            try {
                //
                LogController.logTrace(core, "execute_dotNetClass_assembly, enter, [" + assemblyPrivateAbsPathFilename + "]");
                //
                Assembly testAssembly = null;
                addonFound = false;
                try {
                    //
                    // -- "once an assembly is loaded into an appdomain, it's there for the life of the appdomain."
                    testAssembly = Assembly.LoadFrom(assemblyPrivateAbsPathFilename);
                } catch (System.BadImageFormatException) {
                    //
                    // -- file is not an assembly, return addonFound false
                    //
                    LogController.logTrace(core, "execute_dotNetClass_assembly, 1, [" + assemblyPrivateAbsPathFilename + "]");
                    addonFound = false;
                    //
                    // -- MS says BadImageFormatException is how you detect non-assembly DLLs
                    string filename = Path.GetFileName(assemblyPrivateAbsPathFilename);
                    if (!string.IsNullOrWhiteSpace(filename)) { core.assemblyList_NonAddonsInstalled.Add("//" + filename); }
                    return string.Empty;
                } catch (Exception ex) {
                    //
                    // -- file is not an assembly, return addonFound false
                    //
                    LogController.logError(core, ex, "execute_dotNetClass_assembly, 2, [" + assemblyPrivateAbsPathFilename + "]");
                    addonFound = false;
                    return string.Empty;
                }
                //
                // -- assembly loaded, it is a proper assembly. Test if it is the one we are looking for (match class + baseclass)
                Type addonType = null;
                try {
                    //
                    // -- catch exceptions found (Select.Pdf.dll has two classes that differ by only case)
                    addonType = Array.Find<Type>(
                        testAssembly.GetTypes(),
                        testType => (testType.IsPublic)
                            && (testType.FullName.Equals(addon.dotNetClass, StringComparison.InvariantCultureIgnoreCase))
                            && ((testType.Attributes & TypeAttributes.Abstract) != TypeAttributes.Abstract)
                            && (testType.BaseType != null)
                            && (!string.IsNullOrEmpty(testType.BaseType.FullName)
                            && (testType.BaseType.FullName.Equals("contensive.baseclasses.addonbaseclass", StringComparison.InvariantCultureIgnoreCase)))
                        );
                    //
                    addonFound = (addonType != null);
                } catch (ArgumentException ex) {
                    //
                    // -- argument exception
                    LogController.logWarn(core, ex, "Argument Exception while enumerating classes for addon [" + addon.id + "," + addon.name + "], assembly [" + assemblyPrivateAbsPathFilename + "]. Typically these are when the assembly has two classes that differ only by the case of the class name or namespace.");
                    addonFound = false;
                    return string.Empty;
                } catch (ReflectionTypeLoadException ex) {
                    //
                    // -- loader error, one of the types listed could not be loaded. Could be bad or missing dependency
                    foreach (var loaderException in ex.LoaderExceptions) {
                        LogController.logError(core, ex, "Loader Exception [" + loaderException.Message + "] while enumerating classes for addon [" + addon.id + "," + addon.name + "],  assembly [" + assemblyPrivateAbsPathFilename + "]");
                    }
                    addonFound = false;
                    return string.Empty;
                } catch (Exception ex) {
                    //
                    // -- unknown error. Better to log and continue
                    LogController.logError(core, ex, "Exception while enumerating classes for addon [" + addon.id + "," + addon.name + "],  assembly [" + assemblyPrivateAbsPathFilename + "]");
                    addonFound = false;
                    return string.Empty;
                }
                //
                LogController.logTrace(core, "execute_dotNetClass_assembly, 4, [" + assemblyPrivateAbsPathFilename + "]");
                //
                // -- if not addon found, exit now
                if (!addonFound) { return string.Empty; }
                //
                // -- addon found, execute it
                AddonBaseClass AddonObj = null;
                try {
                    //
                    LogController.logTrace(core, "execute_dotNetClass_assembly, 5, [" + assemblyPrivateAbsPathFilename + "]");
                    //
                    // -- Create an object from the Assembly
                    AddonObj = (AddonBaseClass)testAssembly.CreateInstance(addonType.FullName);
                } catch (ReflectionTypeLoadException ex) {
                    //
                    // -- exception thrown out of application bin folder when xunit library included -- ignore
                    //
                    LogController.logWarn(core, "execute_dotNetClass_assembly, 6, Assembly matches addon pattern but caused a ReflectionTypeLoadException. Return blank. addon [" + addon.name + "], [" + assemblyPrivateAbsPathFilename + "], exception [" + ex + "]");
                    return string.Empty;
                } catch (Exception ex) {
                    //
                    // -- problem loading types
                    //
                    LogController.logWarn(core, "execute_dotNetClass_assembly, 6, Assembly matches addon pattern but caused a load exception. Return blank. addon [" + addon.name + "], [" + assemblyPrivateAbsPathFilename + "], exception [" + ex + "]");
                    return string.Empty;
                }
                try {
                    //
                    LogController.logTrace(core, "execute_dotNetClass_assembly, 8, [" + assemblyPrivateAbsPathFilename + "]");
                    //
                    // -- Call Execute
                    object AddonObjResult = AddonObj.Execute(core.cpParent);
                    return convertAddonReturntoString(AddonObjResult);
                } catch (Exception ex) {
                    //
                    // -- error in the addon
                    //
                    LogController.logError(core, ex, "execute_dotNetClass_assembly, 9, [" + assemblyPrivateAbsPathFilename + "]. There was an error in the addon [" + addon.name + "]. It could not be executed because there was an error in the addon assembly [" + assemblyPrivateAbsPathFilename + "], in class [" + addonType.FullName.Trim().ToLowerInvariant() + "]. The error was [" + ex + "]");
                    return string.Empty;
                }
            } catch (Exception ex) {
                //
                // -- this exception should interrupt the caller
                LogController.logError(core, ex);
                throw;
            }
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
        /// Execute the addon in a background process.
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="arguments"></param>
        public void executeAsProcess(AddonModel addon, Dictionary<string, string> arguments) {
            try {
                if (addon == null) {
                    //
                    // -- addon not found
                    string errMsg = "executeAsync called with null addon model.";
                    logger.Error(LogController.processLogMessage(core, errMsg, true));
                    return;
                }
                //
                // -- build arguments from the execute context on top of docProperties
                var compositeArgs = new Dictionary<string, string>(arguments);
                foreach (var key in core.docProperties.getKeyList()) {
                    if (!compositeArgs.ContainsKey(key)) { compositeArgs.Add(key, core.docProperties.getText(key)); }
                }
                TaskSchedulerController.addTaskToQueue(core, new TaskModel.CmdDetailClass {
                    addonId = addon.id,
                    addonName = addon.name,
                    args = compositeArgs
                }, false);
            } catch (Exception ex) {
                LogController.logError(core, ex, "executeAsync");
            }
        }
        //
        //====================================================================================================================
        /// <summary>
        /// Execute the addon in a background process.
        /// </summary>
        /// <param name="addon"></param>
        public void executeAsProcess(AddonModel addon) => executeAsProcess(addon, new Dictionary<string, string>());
        //
        //====================================================================================================================
        /// <summary>
        /// Execute the addon in a background process.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="OptionString"></param>
        public void executeAsProcess(string guid, string OptionString = "") {
            if (string.IsNullOrEmpty(guid)) { throw new ArgumentException("executeAsync called with invalid guid [" + guid + "]"); }
            var addon = core.cacheStore.addonCache.create(guid);
            if (addon == null) { throw new ArgumentException("ExecuteAsync cannot find Addon for guid [" + guid + "]"); }
            executeAsProcess(addon, convertQSNVAArgumentstoDocPropertiesList(core, OptionString));
        }
        //
        //====================================================================================================================
        /// <summary>
        /// Execute the addon in a background process.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="OptionString"></param>
        public void executeAsProcessByName(string name, string OptionString = "") {
            if (string.IsNullOrEmpty(name)) { throw new ArgumentException("executeAsyncByName called with invalid name [" + name + "]"); }
            AddonModel addon = core.cacheStore.addonCache.createByUniqueName(name);
            //var addon = AddonModel.createByUniqueName(core.cpParent, name);
            if (addon == null) { throw new ArgumentException("executeAsyncByName cannot find Addon for name [" + name + "]"); }
            executeAsProcess(addon, convertQSNVAArgumentstoDocPropertiesList(core, OptionString));
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
                string[] ConstructorNames = Array.Empty<string>();
                string[] ConstructorSelectors = Array.Empty<string>();
                string[] ConstructorValues = Array.Empty<string>();
                int ConstructorCnt = 0;
                if (!string.IsNullOrEmpty(addonArgumentListFromRecord)) {
                    //
                    string[] ConstructorNameValues = Array.Empty<string>();
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
                        int Pos = GenericController.strInstr(1, ConstructorName, "=");
                        if (Pos > 1) {
                            ConstructorValue = ConstructorName.Substring(Pos);
                            ConstructorName = (ConstructorName.left(Pos - 1)).Trim(' ');
                            Pos = GenericController.strInstr(1, ConstructorValue, "[");
                            if (Pos > 0) {
                                ConstructorSelector = ConstructorValue.Substring(Pos - 1);
                                ConstructorValue = ConstructorValue.left(Pos - 1);
                            }
                        }
                        if (!string.IsNullOrEmpty(ConstructorName)) {
                            ConstructorNames[SavePtr] = ConstructorName;
                            ConstructorValues[SavePtr] = ConstructorValue;
                            ConstructorSelectors[SavePtr] = ConstructorSelector;
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
                                if (GenericController.toLCase(InstanceName) == GenericController.toLCase(ConstructorNames[ConstructorPtr])) {
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
                            ConstructorCnt += 1;
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
        public static string getPrivateFilesAddonPath() {
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
                using var csData = new CsModel(core);
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
                    string styleSheetUrl = csData.getText("stylesfilename");
                    if (!string.IsNullOrEmpty(styleSheetUrl)) {
                        if (GenericController.strInstr(1, styleSheetUrl, "://").Equals(0) && (!styleSheetUrl.left(1).Equals("/"))) {
                            styleSheetUrl = GenericController.getCdnFileLink(core, styleSheetUrl);
                        }
                        core.html.addStyleLink(styleSheetUrl, SourceComment);
                    }
                    //
                    if (!string.IsNullOrEmpty(Wrapper)) {
                        int Pos = GenericController.strInstr(1, Wrapper, TargetString, 1);
                        if (Pos != 0) {
                            result = GenericController.strReplace(Wrapper, TargetString, result, 1, 99, 1);
                        } else {
                            result = ""
                                + "<!-- the selected wrapper does not include the Target String marker to locate the position of the content. -->"
                                + Wrapper + result;
                        }
                    }
                }
                csData.close();
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
                    string UcaseName = GenericController.toUCase(Name);
                    foreach (XmlAttribute NodeAttribute in Node.Attributes) {
                        if (GenericController.toUCase(NodeAttribute.Name) == UcaseName) {
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
        public static string getDefaultAddonOptions(CoreController core, string ArgumentList, string AddonGuid, bool IsInline, string AddonName, ref string jsonCommand) {
            var argList = new List<NameValueModel>();
            ArgumentList = GenericController.strReplace(ArgumentList, Environment.NewLine, "\r");
            ArgumentList = GenericController.strReplace(ArgumentList, "\n", "\r");
            ArgumentList = GenericController.strReplace(ArgumentList, "\r", Environment.NewLine);
            if (ArgumentList.IndexOf("wrapper", System.StringComparison.OrdinalIgnoreCase) == -1) {
                //
                // Add in default constructors, like wrapper
                if (!string.IsNullOrEmpty(ArgumentList)) {
                    ArgumentList += Environment.NewLine;
                }
                if (GenericController.toLCase(AddonGuid) == GenericController.toLCase(addonGuidContentBox)) {
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
                        string OptionValue = "";
                        string OptionSelector = "";
                        //
                        // split on equal
                        //
                        NameValue = GenericController.strReplace(NameValue, "\\=", Environment.NewLine);
                        int Pos = GenericController.strInstr(1, NameValue, "=");
                        //
                        // Execute list functions
                        //
                        string OptionName;
                        if (Pos == 0) {
                            OptionName = NameValue;
                        } else {
                            OptionName = NameValue.left(Pos - 1);
                            OptionValue = NameValue.Substring(Pos);
                        }
                        OptionName = GenericController.strReplace(OptionName, Environment.NewLine, "\\=");
                        OptionValue = GenericController.strReplace(OptionValue, Environment.NewLine, "\\=");
                        //
                        // split optionvalue on [
                        //
                        OptionValue = GenericController.strReplace(OptionValue, "\\[", Environment.NewLine);
                        Pos = GenericController.strInstr(1, OptionValue, "[");
                        if (Pos != 0) {
                            OptionSelector = OptionValue.Substring(Pos - 1);
                            OptionValue = OptionValue.left(Pos - 1);
                        }
                        OptionValue = GenericController.strReplace(OptionValue, Environment.NewLine, "\\[");
                        OptionSelector = GenericController.strReplace(OptionSelector, Environment.NewLine, "\\[");
                        //
                        // Decode AddonConstructor format
                        OptionName = GenericController.decodeAddonConstructorArgument(OptionName);
                        OptionValue = GenericController.decodeAddonConstructorArgument(OptionValue);
                        //
                        // -- add to json format
                        argList.Add(new NameValueModel {
                            name = OptionName,
                            value = OptionValue
                        });
                        //
                        // Encode AddonOption format
                        OptionValue = GenericController.encodeNvaArgument(OptionValue);
                        //
                        // rejoin
                        string NameValuePair = core.html.getAddonSelector(OptionName, OptionValue, OptionSelector);
                        NameValuePair = GenericController.encodeJavascriptStringSingleQuote(NameValuePair);
                        result += "&" + NameValuePair;
                        if (GenericController.strInstr(1, NameValuePair, "=") == 0) {
                            result += "=";
                        }
                    }
                }
                //
                // -- cleanup htmlId command
                if (!string.IsNullOrEmpty(result)) {
                    result = result.Substring(1);
                }
                //
                // -- create json command
                string jsonArgs = "";
                foreach (var arg in argList) {
                    if (!string.IsNullOrEmpty(arg.value)) {
                        jsonArgs += (string.IsNullOrEmpty(jsonArgs) ? "" : ",") + "{" + "\"" + encodeJavascriptStringSingleQuote(arg.name) + "\":\"" + encodeJavascriptStringSingleQuote(arg.value) + "\"" + "}";
                    }
                }
                if (string.IsNullOrEmpty(jsonArgs)) {
                    jsonCommand = "{%\"" + encodeJavascriptStringSingleQuote(AddonName) + "\"%}";
                } else {
                    jsonCommand = "{%{\"" + encodeJavascriptStringSingleQuote(AddonName) + "\":" + jsonArgs + "}%}";
                }
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public static string getAddonDescription(CoreController core, AddonModel addon) {
            string addonDescription = "[invalid addon]";
            if (addon != null) {
                string collectionName = "invalid collection or collection not set";
                AddonCollectionModel collection = AddonCollectionModel.create<AddonCollectionModel>(core.cpParent, addon.collectionId);
                if (collection != null) {
                    collectionName = collection.name;
                }
                addonDescription = "[#" + addon.id + ", " + addon.name + "], collection [" + collectionName + "]";
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
                    AddonModel addon = core.cacheStore.addonCache.create(addonGuidAddonManager);
                    if (addon != null) {
                        result = core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                            addonType = CPUtilsBaseClass.addonContext.ContextAdmin,
                            errorContextMessage = "calling addon manager guid for GetAddonManager method"
                        });
                    }
                } catch (Exception ex) {
                    LogController.logError(core, new Exception("Error calling ExecuteAddon with AddonManagerGuid, will attempt Safe Mode Addon Manager. Exception=[" + ex + "]"));
                    AddonStatusOK = false;
                }
                if (string.IsNullOrEmpty(result)) {
                    LogController.logError(core, new Exception("AddonManager returned blank, calling Safe Mode Addon Manager."));
                    AddonStatusOK = false;
                }
                if (!AddonStatusOK) {
                    Contensive.Processor.Addons.SafeAddonManager.AddonManagerClass AddonMan = new Contensive.Processor.Addons.SafeAddonManager.AddonManagerClass(core);
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
                using var cs = new CsModel(core);
                string sql = "select distinct c.addonId"
                    + " from ((ccAddonEvents e"
                    + " left join ccAddonEventCatchers c on c.eventId=e.id)"
                    + " left join ccAggregateFunctions a on a.id=c.addonid)"
                    + " where ";
                if (eventNameIdOrGuid.isNumeric()) {
                    sql += "e.id=" + DbController.encodeSQLNumber(double.Parse(eventNameIdOrGuid));
                } else if (GenericController.isGuid(eventNameIdOrGuid)) {
                    sql += "e.ccGuid=" + DbController.encodeSQLText(eventNameIdOrGuid);
                } else {
                    sql += "e.name=" + DbController.encodeSQLText(eventNameIdOrGuid);
                }
                sql += " order by c.addonid desc";
                if (!cs.openSql(sql)) {
                    //
                    // event not found
                    if (eventNameIdOrGuid.isNumeric()) {
                        //
                        // can not create an id
                    } else if (GenericController.isGuid(eventNameIdOrGuid)) {
                        //
                        // create event with Guid and id for name
                        using var cs2 = new CsModel(core);
                        cs2.insert("add-on Events");
                        cs2.set("ccguid", eventNameIdOrGuid);
                        cs2.set("name", "Event " + cs2.getInteger("id").ToString());
                    } else if (!string.IsNullOrEmpty(eventNameIdOrGuid)) {
                        //
                        // create event with name
                        using var cs3 = new CsModel(core);
                        cs3.insert("add-on Events");
                        cs3.set("name", eventNameIdOrGuid);
                    }
                } else {
                    //
                    // -- event found, check if there are addons to run
                    while (cs.ok()) {
                        int addonid = cs.getInteger("addonid");
                        if (addonid != 0) {
                            var addon = core.cacheStore.addonCache.create(addonid);
                            returnString += core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                addonType = CPUtilsBaseClass.addonContext.ContextSimple,
                                errorContextMessage = "calling handler addon id [" + addonid + "] for event [" + eventNameIdOrGuid + "]"
                            });
                        }
                        cs.goNext();
                    }
                }
                cs.close();
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
            if (string.IsNullOrEmpty(IconAlt)) { IconAlt = "Add-on"; }
            if (string.IsNullOrEmpty(IconTitle)) { IconTitle = "Rendered as Add-on"; }
            if (string.IsNullOrEmpty(IconFilename)) {
                //
                // No icon given, use the default
                if (IconIsInline) {
                    IconFilename = "" + cdnPrefix + "images/IconAddonInlineDefault.png";
                    IconWidth = 62;
                    IconHeight = 17;
                    IconSprites = 0;
                } else {
                    IconFilename = "" + cdnPrefix + "images/IconAddonBlockDefault.png";
                    IconWidth = 57;
                    IconHeight = 59;
                    IconSprites = 4;
                }
            } else if (strInstr(1, IconFilename, "://") != 0) {
                //
                // icon is an Absolute URL - leave it
                //
            } else if (IconFilename.left(1) == "/") {
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
            string result;
            if (IconSprites == 0) {
                //
                // just the icon
                result = "<img"
                    + " border=0"
                    + " id=\"" + IconImgID + "\""
                    + " alt=\"" + IconAlt + "\""
                    + " title=\"" + IconTitle + "\""
                    + " src=\"" + IconFilename + "\"";
                if (IconWidth != 0) { result += " width=\"" + IconWidth + "px\""; }
                if (IconHeight != 0) { result += " height=\"" + IconHeight + "px\""; }
                if (IconIsInline) {
                    result += " style=\"vertical-align:middle;display:inline;\" ";
                } else {
                    result += " style=\"display:block\" ";
                }
                if (!string.IsNullOrEmpty(ACInstanceID)) { result += " ACInstanceID=\"" + ACInstanceID + "\""; }
                result += ">";
            } else {
                //
                // Sprite Icon
                result = getIconSprite(IconImgID, IconSpriteColumn, IconFilename, IconWidth, IconHeight, IconAlt, IconTitle, "", IconIsInline, ACInstanceID);
            }
            return result;
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
            string result = "<img"
                + " border=0"
                + " id=\"" + TagID + "\""
                + " onMouseOver=\"this.style.backgroundPosition='" + -1 * SpriteColumn * IconWidth + "px -" + 2 * IconHeight + "px';\""
                + " onMouseOut=\"this.style.backgroundPosition='" + -1 * SpriteColumn * IconWidth + "px 0px'\""
                + " onDblClick=\"" + onDblClick + "\""
                + " alt=\"" + IconAlt + "\""
                + " title=\"" + IconTitle + "\""
                + " src=\"" + cdnPrefix + "images/spacer.gif\"";
            string ImgStyle = "background:url(" + IconSrc + ") " + (-1 * SpriteColumn * IconWidth) + "px 0px no-repeat;";
            ImgStyle += "width:" + IconWidth + "px;";
            ImgStyle = ImgStyle + "height:" + IconHeight + "px;";
            if (IconIsInline) {
                ImgStyle += "vertical-align:middle;display:inline;";
            } else {
                ImgStyle += "display:block;";
            }
            if (!string.IsNullOrEmpty(ACInstanceID)) {
                result += " ACInstanceID=\"" + ACInstanceID + "\"";
            }
            result += " style=\"" + ImgStyle + "\">";
            return result;
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
        protected bool disposed;
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
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static NLog.Logger logger { get; } = NLog.LogManager.GetCurrentClassLogger();
    }
}