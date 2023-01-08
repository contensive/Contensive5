
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using Contensive.BaseClasses;
using Contensive.Processor.Exceptions;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;

namespace Contensive.Processor {
    //
    // ====================================================================================================
    //
    public class CPUtilsClass : BaseClasses.CPUtilsBaseClass, IDisposable {
        //
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        // ====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        public CPUtilsClass(CPClass cp) {
            this.cp = cp;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Returns core.dateTimeMockable unless MockDateTime is set
        /// </summary>
        /// <returns></returns>
        public override DateTime GetDateTimeMockable() {
            return cp.core.dateTimeNowMockable;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Authentication token can be used to authenticate the user with the request "eid=token". The default expiration is 24 hours.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public override string GetAuthenticationToken(int userId) {
            return SecurityController.encodeToken(cp.core, userId, cp.core.dateTimeNowMockable.AddDays(1));
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Authentication token can be used to authenticate the user with the request "eid=token".
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="expiration"></param>
        /// <returns></returns>
        public override string GetAuthenticationToken(int userId, DateTime expiration) {
            return SecurityController.encodeToken(cp.core, userId, expiration);
        }

        //
        // ====================================================================================================
        /// <summary>
        /// Return a text approximation of an Html document
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override string ConvertHTML2Text(string source) {
            return HtmlController.convertHtmlToText(cp.core, source);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// return an html approximation of a text document
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override string ConvertText2HTML(string source) {
            return HtmlController.convertTextToHtml(source);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Create a new guid in the systems format (registry format "{...}")
        /// </summary>
        /// <returns></returns>
        public override string CreateGuid()  {
            return GenericController.getGUID();
        }
        //
        // ====================================================================================================
        //
        public override string EncodeContentForWeb(string Source, string ContextContentName = "", int ContextRecordId = 0, int WrapperId = 0) {
            return ContentRenderController.renderHtmlForWeb(cp.core, Source, ContextContentName, ContextRecordId, 0, "", WrapperId, CPUtilsBaseClass.addonContext.ContextPage);
        }
        //
        // ====================================================================================================
        //
        public override void IISReset()  {
            {
                cp.core.webServer.reset();
            }
        }
        //
        // ====================================================================================================
        //
        public override int EncodeInteger(object expression) {
            return GenericController.encodeInteger(expression);
        }
        //
        // ====================================================================================================
        //
        public override double EncodeNumber(object expression) {
            return GenericController.encodeNumber(expression);
        }
        //
        // ====================================================================================================
        //
        public override string EncodeText(object expression) {
            return GenericController.encodeText(expression);
        }
        //
        // ====================================================================================================
        //
        public override bool EncodeBoolean(object expression) {
            return GenericController.encodeBoolean(expression);
        }
        //
        // ====================================================================================================
        //
        public override DateTime EncodeDate(object expression) {
            return GenericController.encodeDate(expression);
        }
        //
        // ====================================================================================================
        //
        public override void AppendLog(string Text) {
            LogController.logInfo(cp.core, Text);
        }
        //
        // ====================================================================================================
        //
        public override string DecodeResponseVariable(string Source) {
            return GenericController.decodeResponseVariable(Source);
        }
        //
        // ====================================================================================================
        //
        public override string EncodeRequestVariable(string Source) {
            return GenericController.encodeRequestVariable(Source);
        }
        //
        // ====================================================================================================
        //
        public override string GetArgument(string Name, string ArgumentString, string DefaultValue, string Delimiter) {
            return GenericController.getValueFromKeyValueString(Name, ArgumentString, DefaultValue, Delimiter);
        }
        public override string GetArgument(string Name, string ArgumentString, string DefaultValue) {
            return GenericController.getValueFromKeyValueString(Name, ArgumentString, DefaultValue, "");
        }
        public override string GetArgument(string Name, string ArgumentString) {
            return GenericController.getValueFromKeyValueString(Name, ArgumentString, "", "");
        }
        //
        // ====================================================================================================
        //
        public override string GetFilename(string PathFilename) {
            return FileController.getFilename(PathFilename);
        }
        //
        // ====================================================================================================
        //
        public override int GetListIndex(string Item, string ListOfItems) {
            return GenericController.getListIndex(Item, ListOfItems);
        }
        //
        // ====================================================================================================
        //
        public override int GetRandomInteger()  {
            return GenericController.getRandomInteger(cp.core);
        }
        //
        // ====================================================================================================
        //
        public override bool IsInDelimitedString(string DelimitedString, string TestString, string Delimiter) {
            return GenericController.isInDelimitedString(DelimitedString, TestString, Delimiter);
        }
        //
        // ====================================================================================================
        //
        public override string ModifyLinkQueryString(string url, string key, string value, bool addIfMissing) {
            return GenericController.modifyLinkQuery(url, key, value, addIfMissing);
        }

        public override string ModifyLinkQueryString(string url, string key, string value) {
            return GenericController.modifyLinkQuery(url, key, value, true);
        }

        public override string ModifyLinkQueryString(string url, string key, int value) {
            return GenericController.modifyLinkQuery(url, key, value.ToString(), true);
        }

        public override string ModifyLinkQueryString(string url, string key, int value, bool addIfMissing) {
            return GenericController.modifyLinkQuery(url, key, value.ToString(), addIfMissing);
        }

        public override string ModifyLinkQueryString(string url, string key, double value) {
            return GenericController.modifyLinkQuery(url, key, value.ToString(), true);
        }

        public override string ModifyLinkQueryString(string url, string key, double value, bool addIfMissing) {
            return GenericController.modifyLinkQuery(url, key, value.ToString(), addIfMissing);
        }

        public override string ModifyLinkQueryString(string url, string key, bool value) {
            return GenericController.modifyLinkQuery(url, key, value.ToString(), true);
        }

        public override string ModifyLinkQueryString(string url, string key, bool value, bool addIfMissing) {
            return GenericController.modifyLinkQuery(url, key, value.ToString(), addIfMissing);
        }

        public override string ModifyLinkQueryString(string url, string key, DateTime value) {
            return GenericController.modifyLinkQuery(url, key, value.ToString(), true);
        }

        public override string ModifyLinkQueryString(string url, string key, DateTime value, bool addIfMissing) {
            return GenericController.modifyLinkQuery(url, key, value.ToString(), addIfMissing);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, string QueryValue, bool AddIfMissing) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue, AddIfMissing);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, string QueryValue) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue, true);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, int QueryValue) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue, true);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, int QueryValue, bool AddIfMissing) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue, AddIfMissing);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, double QueryValue) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue.ToString(), true);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, double QueryValue, bool AddIfMissing) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue.ToString(), AddIfMissing);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, bool QueryValue) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue, true);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, bool QueryValue, bool AddIfMissing) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue, AddIfMissing);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, DateTime QueryValue) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue.ToString(), true);
        }

        public override string ModifyQueryString(string WorkingQuery, string QueryName, DateTime QueryValue, bool AddIfMissing) {
            return GenericController.modifyQueryString(WorkingQuery, QueryName, QueryValue.ToString(), AddIfMissing);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// seperate a url into its parts
        /// </summary>
        /// <param name="SourceURL"></param>
        /// <param name="Protocol"></param>
        /// <param name="Host"></param>
        /// <param name="Path"></param>
        /// <param name="Page"></param>
        /// <param name="QueryString"></param>
        public override void SeparateURL(string SourceURL, ref string Protocol, ref string Host, ref string Path, ref string Page, ref string QueryString) {
            GenericController.UrlDetailsClass urlDetails = GenericController.splitUrl(SourceURL);
            Protocol = urlDetails.protocol;
            Host = urlDetails.host;
            Path = string.Join("\"", urlDetails.pathSegments);
            Page = urlDetails.filename;
            QueryString = urlDetails.queryString;
        }
        public override void SeparateURL(string SourceURL, ref string Protocol, ref string Host, ref string port, ref string Path, ref string Page, ref string QueryString) {
            GenericController.UrlDetailsClass urlDetails = GenericController.splitUrl(SourceURL);
            Protocol = urlDetails.protocol;
            Host = urlDetails.host;
            port = urlDetails.port;
            Path = string.Join("\"", urlDetails.pathSegments);
            Page = urlDetails.filename;
            QueryString = urlDetails.queryString;
        }
        //
        // ====================================================================================================
        //
        public override object SplitDelimited(string WordList, string Delimiter) {
            return GenericController.splitDelimited(WordList, Delimiter);
        }
        //
        // ====================================================================================================
        //
        public override bool isGuid(string guid) {
            return GenericController.isGuid(guid);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Converts html content to the wysiwyg editor compatible format that includes edit icons for addons. Use this to convert the html content added to wysiwyg editors. Use EncodeHtmlFromWysiwygEditor() before saving back to Db.
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public override string EncodeHtmlForWysiwygEditor(string Source) {
            return ContentRenderController.renderHtmlForWysiwygEditor(cp.core, Source);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Converts html content from wysiwyg editors to be saved. See EncodeHtmlForWysiwygEditor() for more details.
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public override string DecodeHtmlFromWysiwygEditor(string Source) {
            return ContentRenderController.processWysiwygResponseForSave(cp.core, Source);
        }
        //
        //====================================================================================================
        //
        public override void ExportCsv(string sql, string exportName, string filename) {
            try {
                var ExportCSVAddon = DbBaseModel.create<AddonModel>(cp, addonGuidExportCSV);
                if (ExportCSVAddon == null) {
                    LogController.logError(cp.core, new GenericException("ExportCSV addon not found. Task could not be added to task queue."));
                } else {
                    var cmdDetail = new TaskModel.CmdDetailClass {
                        addonId = ExportCSVAddon.id,
                        addonName = ExportCSVAddon.name,
                        args = new Dictionary<string, string> {
                            { "sql", sql },
                            { "ExportName", exportName },
                            { "filename", filename }
                        }
                    };
                    TaskSchedulerController.addTaskToQueue(cp.core, cmdDetail, false, exportName, filename );
                }
            } catch (Exception) {
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert a file link (like /ccLibraryFiles/imageFilename/000001/this.png) to a full URL
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        public override string EncodeAppRootPath(string link) {
            return GenericController.encodeVirtualPath(GenericController.encodeText(link), cp.core.appConfig.cdnFileUrl, appRootPath, cp.core.webServer.requestDomain);
        }
        //
        //==============================================================================================================
        /// <summary>
        /// convert fileInfo array to parsable string [filename-attributes-creationTime-lastAccessTime-lastWriteTime-length-entension]
        /// </summary>
        /// <param name="FileInfo"></param>
        /// <returns></returns>
        public override string Upgrade51ConvertFileInfoArrayToParseString(List<CPFileSystemBaseClass.FileDetail> FileInfo) {
            return UpgradeController.upgrade51ConvertFileInfoArrayToParseString(FileInfo);
        }
        //
        //==============================================================================================================
        /// <summary>
        /// convert directoryInfo object to parsable string [filename-attributes-creationTime-lastAccessTime-lastWriteTime-extension]
        /// </summary>
        /// <param name="DirectoryInfo"></param>
        /// <returns></returns>
        public override string Upgrade51ConvertDirectoryInfoArrayToParseString(List<CPFileSystemBaseClass.FolderDetail> DirectoryInfo) {
            return UpgradeController.upgrade51ConvertDirectoryInfoArrayToParseString(DirectoryInfo);
        }
        //
        //====================================================================================================
        // deprecated
        //
        [Obsolete("Installation upgrade through the cp interface is deprecated. Please use the command line tool.", false)]
        public override void Upgrade(bool isNewApp) {
            try {
                throw new GenericException("Installation upgrade through the cp interface is deprecated. Please use the command line tool.");
                // Controllers.appBuilderController.upgrade(CP.core, isNewApp)
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        public override string DecodeUrl(string Url) {
            return GenericController.decodeURL(Url);
        }
        //
        public override string DecodeHTML(string Source) {
            return HtmlController.decodeHtml(Source);
        }
        //
        public override string EncodeHTML(string Source) {
            string returnValue = "";
            //
            if (!string.IsNullOrEmpty(Source)) {
                returnValue = HtmlController.encodeHtml(Source);
            }
            return returnValue;
        }
        //
        public override string EncodeUrl(string Source) {
            return GenericController.encodeURL(Source);
        }
        //
        [Obsolete("deprecated", false)]
        public override string GetPleaseWaitEnd()  {
            return cp.core.programFiles.readFileText("resources\\WaitPageClose.htm");
        }
        //
        [Obsolete("deprecated", false)]
        public override string GetPleaseWaitStart()  {
            return cp.core.programFiles.readFileText("Resources\\WaitPageOpen.htm");
        }
        //
        public string executeAddon(string addonIDGuidOrName, int wrapperId, addonContext context) {
            addonExecuteContext executeContext = new addonExecuteContext {
                addonType = context,
                instanceGuid = cp.core.docProperties.getText("instanceId"),
                wrapperID = wrapperId
            };
            if (addonIDGuidOrName.isNumeric()) {
                return (string)cp.Addon.Execute(EncodeInteger(addonIDGuidOrName), executeContext);
            } else if (isGuid(addonIDGuidOrName)) {
                return (string)cp.Addon.Execute(addonIDGuidOrName, executeContext);
            } else {
                return (string)cp.Addon.ExecuteByUniqueName(addonIDGuidOrName, executeContext);
            }
        }
        //
        [Obsolete("Deprecated, use cp.addon.Execute", false)]
        public override string ExecuteAddon(string addonIDGuidOrName) => executeAddon(addonIDGuidOrName, 0, addonContext.ContextPage);
        //
        [Obsolete("Deprecated, use cp.addon.Execute", false)]
        public override string ExecuteAddon(string addonIDGuidOrName, int WrapperId) => executeAddon(addonIDGuidOrName, WrapperId, addonContext.ContextPage);
        //
        [Obsolete("Deprecated, use cp.addon.Execute", false)]
        public override string ExecuteAddon(string addonIDGuidOrName, addonContext context) => executeAddon(addonIDGuidOrName, 0, context);
        //
        [Obsolete("Deprecated, use cp.addon.Execute", false)]
        public override string ExecuteAddonAsProcess(string addonIDGuidOrName) {
            try {
                AddonModel addon = null;
                if (addonIDGuidOrName.isNumeric()) {
                    addon = cp.core.addonCache.getAddonById(EncodeInteger(addonIDGuidOrName));
                } else if (GenericController.isGuid(addonIDGuidOrName)) {
                    addon = cp.core.addonCache.getAddonByGuid(addonIDGuidOrName);
                } else {
                    addon = cp.core.addonCache.getAddonByName(addonIDGuidOrName);
                }
                if (addon != null) {
                    cp.core.addon.executeAsProcess(addon);
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
            return string.Empty;
        }
        //
        [Obsolete("Deprecated, use AppendLog")]
        public override void AppendLogFile(string Text) {
            LogController.logInfo(cp.core, Text);
        }
        //
        [Obsolete("Deprecated, file logging is no longer supported. Use AppendLog(message) to log Info level messages")]
        public override void AppendLog(string pathFilename, string Text) {
            if ((!string.IsNullOrWhiteSpace(pathFilename)) && (!string.IsNullOrWhiteSpace(Text))) {
                pathFilename = FileController.convertToDosSlash(pathFilename);
                string[] parts = pathFilename.Split('\\');
                LogController.logInfo(cp.core, "legacy logFile: [" + pathFilename + "], " + Text);
            }
        }
        //
        [Obsolete("Deprecated", false)]
        public override string ConvertLinkToShortLink(string URL, string ServerHost, string ServerVirtualPath) {
            return GenericController.convertLinkToShortLink(URL, ServerHost, ServerVirtualPath);
        }
        //
        [Obsolete("Deprecated", false)]
        public override string ConvertShortLinkToLink(string url, string pathPagePrefix) {
            return GenericController.removeUrlPrefix(url, pathPagePrefix);
        }
        //
        [Obsolete("Deprecated. Use native methods to convert date formats.", false)]
        public override DateTime DecodeGMTDate(string GMTDate) {
            return GenericController.deprecatedDecodeGMTDate(GMTDate);
        }
        [Obsolete("Use SeparateURL(), true ")]
        public override void ParseURL(string url, ref string return_protocol, ref string return_domain, ref string return_port, ref string return_path, ref string return_page, ref string return_queryString) {
            SeparateURL(url, ref return_protocol, ref return_domain, ref return_port, ref return_path, ref return_queryString);
        }
        //
        [Obsolete("Deprecated.", false)]
        public override string EncodeJavascript(string Source) {
            return GenericController.encodeJavascriptStringSingleQuote(Source);
        }
        //
        [Obsolete("Deprecated.", false)]
        public override string EncodeQueryString(string Source) {
            return GenericController.encodeQueryString(Source);
        }
        //
        [Obsolete("Deprecated.", false)]
        public override DateTime GetFirstNonZeroDate(DateTime Date0, DateTime Date1) {
            return GenericController.getFirstNonZeroDate(Date0, Date1);
        }
        //
        [Obsolete("Deprecated.", false)]
        public override int GetFirstNonZeroInteger(int Integer0, int Integer1) {
            return GenericController.getFirstNonZeroInteger(Integer0, Integer1);
        }
        //
        [Obsolete("Deprecated.", false)]
        public override string GetIntegerString(int Value, int DigitCount) {
            return GenericController.getIntegerString(Value, DigitCount);
        }
        //
        [Obsolete("Deprecated.", false)]
        public override string GetLine(string Body) {
            return GenericController.getLine(ref Body);
        }
        //
        [Obsolete("Deprecated.", false)]
        public override int GetProcessID()  {
            return Process.GetCurrentProcess().Id;
        }
        //
        [Obsolete("Deprecated.", false)]
        public override void Sleep(int timeMSec) {
            System.Threading.Thread.Sleep(timeMSec);
        }
        //
        [Obsolete("Deprecated.", false)]
        public override string hashMd5(string source) {
            throw new NotImplementedException("hashMd5 not implemented");
        }
        // todo implement taskId return value, create cp.task object to track task status
        //====================================================================================================
        /// <summary>
        /// Install an addon collection file asynchonously. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        /// <param name="privateFile"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use cp.addon methods.", false)]
        public override int installCollectionFromFile(string privateFile) {
            string ignore = "";
            cp.Addon.InstallCollectionFile(privateFile, ref ignore);
            return 0;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Install all addon collections in a folder asynchonously. Optionally delete the folder. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        /// <param name="privateFolder"></param>
        /// <param name="deleteFolderWhenDone"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use cp.addon methods.", false)]
        public override int installCollectionsFromFolder(string privateFolder, bool deleteFolderWhenDone) {
            string ignore = "";
            cp.Addon.InstallCollectionsFromFolder(privateFolder, deleteFolderWhenDone, ref ignore);
            return 0;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Install all addon collections in a folder asynchonously. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        /// <param name="privateFolder"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use cp.addon methods.", false)]
        public override int installCollectionsFromFolder(string privateFolder) {
            string ignore = "";
            cp.Addon.InstallCollectionsFromFolder(privateFolder, false, ref ignore);
            return 0;
        }
        // todo implement taskId return value, create cp.task object to track task status
        //====================================================================================================
        /// <summary>
        /// Install an addon collections from the collection library asynchonously. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        [Obsolete("Deprecated, use cp.addon methods.", false)]
        public override int installCollectionFromLibrary(string collectionGuid) {
            string ignore = "";
            cp.Addon.InstallCollectionFromLibrary(collectionGuid, ref ignore);
            return 0;
        }
        // todo implement taskId return value, create cp.task object to track task status
        //====================================================================================================
        /// <summary>
        /// Install an addon collections from an endpoint asynchonously. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        /// <param name="privateFolder"></param>
        /// <param name="deleteFolderWhenDone"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use cp.addon methods.", false)]
        public override int installCollectionFromLink(string link) {
            string ignore = "";
            cp.Addon.InstallCollectionFromLink(link, ref ignore);
            return 0;
        }
        //
        // dispose
        //
        #region  IDisposable Support 
        //
        // ====================================================================================================
        // dispose
        protected virtual void Dispose(bool disposing_utils) {
            if (!this.dispose_utils) {
                if (disposing_utils) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.dispose_utils = true;
        }
        protected bool dispose_utils;
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CPUtilsClass()  {
            Dispose(false);
        }
        #endregion
    }

}