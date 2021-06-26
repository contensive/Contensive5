
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// helper methods to support processes, properties and methods used throughout
    /// </summary>
    public abstract class CPUtilsBaseClass {
        /// <summary>
        /// When an addon is executed, this is a description of the context of the method. Used to support features like html-comments only appropriate for some context
        /// </summary>
        public enum addonContext {
            /// <summary>
            /// Addon placed on a page.
            /// </summary>
			ContextPage = 1,
            /// <summary>
            /// Addon run by the admin site addon, to be displayed in the dashboard space
            /// </summary>
			ContextAdmin = 2,
            /// <summary>
            /// Addon placed on a template
            /// </summary>
			ContextTemplate = 3,
            /// <summary>
            /// Addon executed when an email is being rendered for an individual
            /// </summary>
			ContextEmail = 4,
            /// <summary>
            /// Addon executed as a remote method and is expected to return html (as opposed to JSON)
            /// </summary>
			ContextRemoteMethodHtml = 5,
            /// <summary>
            /// Addon executed because when a new visit is created. The return is ignored.
            /// </summary>
			ContextOnNewVisit = 6,
            /// <summary>
            /// Addon executed right before the body end html tag. The return is placed in the html
            /// </summary>
			ContextOnPageEnd = 7,
            /// <summary>
            /// Addon executed right after the open body tag. The return is placed in the html.
            /// </summary>
			ContextOnPageStart = 8,
            /// <summary>
            /// Addon executed because it is set as the editor for a content field type. It reads details from the doc and creates an html edit tag(s).
            /// </summary>
			ContextEditor = 9,
            /// <summary>
            /// deprecated
            /// </summary>
			ContextHelpUser = 10,
            /// <summary>
            /// deprecated
            /// </summary>
			ContextHelpAdmin = 11,
            /// <summary>
            /// deprecated
            /// </summary>
			ContextHelpDeveloper = 12,
            /// <summary>
            /// Addon executed by admin site when a content record is changed. Reads details from doc properties and act on the change
            /// </summary>
			ContextOnContentChange = 13,
            /// <summary>
            /// Executes when the html page is complete. Can modify the html document in core.docBody
            /// </summary>
			ContextFilter = 14,
            /// <summary>
            /// Return the addon's return, add artifacts like css to document.
            /// </summary>
			ContextSimple = 15,
            /// <summary>
            /// Executes right after the body start. Return is placed in the html document
            /// </summary>
			ContextOnBodyStart = 16,
            /// <summary>
            /// Executes right before the end body. Return is placed in the html body
            /// </summary>
			ContextOnBodyEnd = 17,
            /// <summary>
            /// Executes as a remote method. If return is a string, it is returned. If the return is any other type, it is serialized to JSON.
            /// </summary>
			ContextRemoteMethodJson = 18
        }
        /// <summary>
        /// When an addon is executed from content with {% JSON %}, this object describes the record in which the command was executed
        /// </summary>
        public class addonExecuteHostRecordContext {
            /// <summary>
            /// the content for the record (metadata that describes the table)
            /// </summary>
            public string contentName { get; set; }
            /// <summary>
            /// record's id
            /// </summary>
            public int recordId { get; set; }
            /// <summary>
            /// field name
            /// </summary>
            public string fieldName { get; set; }
        }
        /// <summary>
        /// When an addon is execute, this object describes the environment in which the addon will be used.
        /// </summary>
        public class addonExecuteContext {
            /// <summary>
            /// This caption is used if the addon cannot be executed.
            /// </summary>
            /// <returns></returns>
            public string errorContextMessage { get; set; }
            /// <summary>
            /// select enumeration option the describes the environment in which the addon is being executed (in an email, on a page, as a remote method, etc)
            /// </summary>
            /// <returns></returns>
            public addonContext addonType { get; set; } = addonContext.ContextSimple;
            /// <summary>
            /// Optional. If the addon is run from a page, it includes an instanceGuid which can be used by addon programming to locate date for this instance.
            /// </summary>
            /// <returns></returns>
            public string instanceGuid { get; set; } = "";
            /// <summary>
            /// Optional. Name value pairs added to the document environment during execution so they be read by addon programming during and after execution with cp.doc.getText(), etc.
            /// </summary>
            /// <returns></returns>
            public Dictionary<string, string> argumentKeyValuePairs { get; set; } = new Dictionary<string, string>();
            /// <summary>
            /// Optional. If this addon is run automatically because it was included in content, this is the contentName, recordId and fieldName of the record that held that content.
            /// </summary>
            /// <returns></returns>
            public addonExecuteHostRecordContext hostRecord { get; set; } = new addonExecuteHostRecordContext();
            /// <summary>
            /// Optional. If included, this is the id value of a record in the Wrappers content and that wrapper will be added to the addon return result.
            /// </summary>
            /// <returns></returns>
            public int wrapperID { get; set; }
            /// <summary>
            /// Optional. If included, the addon will be wrapped with a div and this will be the html Id value of the div. May be used to customize the resulting html styles.
            /// </summary>
            /// <returns></returns>
            public string cssContainerId { get; set; } = "";
            /// <summary>
            /// Optional. If included, the addon will be wrapped with a div and this will be the html class value of the div. May be used to customize the resulting html styles.
            /// </summary>
            /// <returns></returns>
            public string cssContainerClass { get; set; } = "";
            /// <summary>
            /// Optional. If true, this addon is called because it was a dependancy, and can only be called once within a document.
            /// </summary>
            /// <returns></returns>
            public bool isIncludeAddon { get; set; }
            /// <summary>
            /// Optional. If set true, the addon being called will be delivered as ah html document, with head, body and html tags. This forces the addon's htmlDocument setting.
            /// </summary>
            /// <returns></returns>
            public bool forceHtmlDocument { get; set; }
            /// <summary>
            /// When true, the environment is run from the task subsystem, without a UI. Assemblies from base collection run from program files. Addon return is ignored.
            /// </summary>
            public bool backgroundProcess { get; set; }
            /// <summary>
            /// When true, an addon's javascript will be put in the head. This also forces javascript for all dependant addons to the head.
            /// </summary>
            public bool forceJavascriptToHead { get; set; }
            //
            // todo - deprecate personalizationPeopleId - execute against the current session context
            /// <summary>
            /// Deprecated.  execute against the current session context
            /// </summary>
            /// <returns></returns>
            public int personalizationPeopleId { get; set; }
            //
            // todo - deprecate personalizationPeopleId - execute against the current session context
            /// <summary>
            /// Deprecated.  execute against the current session context
            /// </summary>
            /// <returns></returns>
            public bool personalizationAuthenticated { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Authentication token can be used to authenticate the user with the request "eid=token". The default expiration is 24 hours.
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public abstract string GetAuthenticationToken(int UserId);
        /// <summary>
        /// Authentication token can be used to authenticate the user with the request "eid=token".
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="Expiration"></param>
        /// <returns></returns>
        public abstract string GetAuthenticationToken(int UserId, DateTime Expiration);
        //
        //====================================================================================================
        /// <summary>
        /// Create a log entry type "Log"
        /// </summary>
        /// <param name="logText"></param>
        public abstract void AppendLog(string logText);
        //
        //====================================================================================================
        /// <summary>
        /// Return a text approximation of html content
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public abstract string ConvertHTML2Text(string Source);
        //
        //====================================================================================================
        /// <summary>
        /// Return an html approximation of text content
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract string ConvertText2HTML(string source);
        //
        //====================================================================================================
        /// <summary>
        /// Return a new guid in system's format {...}
        /// </summary>
        /// <returns></returns>
        public abstract string CreateGuid();
        //
        //====================================================================================================
        /// <summary>
        /// html content can be stored with embedded addons. Use this routine to prepare the content returnedfrom a wysiwyg editor. (addons converted to images)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract string DecodeHtmlFromWysiwygEditor(string source);
        //
        //====================================================================================================
        /// <summary>
        /// Decodes a querystring response argument (key or value) you would expect to see within a querystring (key1=value1&amp;amp;key2=value2)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract string DecodeResponseVariable(string source);
        //
        //====================================================================================================
        /// <summary>
        /// convert a file link (like /ccLibraryFiles/imageFilename/000001/this.png) to a full URL
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        public abstract string EncodeAppRootPath(string link);
        //
        //====================================================================================================
        /// <summary>
        /// Convert to boolean. If no valid rendition exists, returns false. (accepts true, yes, on, any +number)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public abstract bool EncodeBoolean(object expression);
        //
        //====================================================================================================
        /// <summary>
        /// html content can be stored with embedded addons. This routine renders the content for display on a website.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="contextContentName"></param>
        /// <param name="contextRecordID"></param>
        /// <param name="wrapperID"></param>
        /// <returns></returns>
        public abstract string EncodeContentForWeb(string source, string contextContentName = "", int contextRecordID = 0, int wrapperID = 0);
        //
        //====================================================================================================
        /// <summary>
        /// Convert to DateTime. If no valid rendition exists, returns default( DateTime ) = 1/1/0001 12:00:00AM.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public abstract DateTime EncodeDate(object expression);
        //
        //====================================================================================================
        /// <summary>
        /// html content can be stored with embedded addons. Use this routine to prepare the content for a wysiwyg editor.  (addons converted to images)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract string EncodeHtmlForWysiwygEditor(string source);
        //
        //====================================================================================================
        /// <summary>
        /// Convert to integer. If no valid rendition exists, returns 0.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public abstract int EncodeInteger(object expression);
        //
        //====================================================================================================
        /// <summary>
        /// Convert to double. If no valid rendition exists, returns 0.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public abstract double EncodeNumber(object expression);
        //
        //====================================================================================================
        /// <summary>
        /// Encodes a querystring response argument (key or value) you would expect to see within a querystring (key1=value1&amp;key2=value2)
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public abstract string EncodeRequestVariable(string Source);
        //
        //====================================================================================================
        /// <summary>
        /// Convert to string. If no valid rendition exists, returns empty.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public abstract string EncodeText(object expression);
        //
        //====================================================================================================
        /// <summary>
        /// Run an SQL query on the default datasource and save the data in a CSV file in the filename provided to a record in the downloads table.
        /// </summary>
        /// <param name="SQL">The query to run, selecting the data to be exported.</param>
        /// <param name="exportName">The name of the export data, used in the downloads table</param>
        /// <param name="filename">The filename for the export, saved in the downloads table</param>
        public abstract void ExportCsv(string SQL, string exportName, string filename);
        //
        //====================================================================================================
        /// <summary>
        /// get a value from a key=value delimited string. ex keyValueDelimitedString (a=b&amp;amp;c=4), keys ( a and c ), delimiter ( &amp;amp; ), values ( b and 4 )
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keyValueDelimitedString"></param>
        /// <param name="defaultValue"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public abstract string GetArgument(string key, string keyValueDelimitedString, string defaultValue, string delimiter);
        /// <summary>
        /// get a value from a key=value delimited string. ex keyValueDelimitedString (a=b&amp;amp;c=4), keys ( a and c ), delimiter ( &amp;amp; ), values ( b and 4 )
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keyValueDelimitedString"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract string GetArgument(string key, string keyValueDelimitedString, string defaultValue);
        /// <summary>
        /// get a value from a key=value delimited string. ex keyValueDelimitedString (a=b&amp;amp;c=4), keys ( a and c ), delimiter ( &amp;amp; ), values ( b and 4 )
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keyValueDelimitedString"></param>
        /// <returns></returns>
        public abstract string GetArgument(string key, string keyValueDelimitedString);
        //
        //====================================================================================================
        /// <summary>
        /// Return the filename part of a path (after the left-most slash), either Unix or Dos
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <returns></returns>
        public abstract string GetFilename(string pathFilename);
        //
        //====================================================================================================
        /// <summary>
        /// returns a 1-based index into the comma delimited ListOfItems where Item is found. 
        /// </summary>
        /// <param name="itemToFind"></param>
        /// <param name="commaDelimitedListOfItems"></param>
        /// <returns></returns>
        public abstract int GetListIndex(string itemToFind, string commaDelimitedListOfItems);
        //
        //====================================================================================================
        /// <summary>
        /// Return a random integer
        /// </summary>
        /// <returns></returns>
        public abstract int GetRandomInteger();
        //
        //====================================================================================================
        /// <summary>
        /// returns true if the string is a guid (start/end with brace, char count, check dashes
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public abstract bool isGuid(string guid);
        //
        //====================================================================================================
        /// <summary>
        /// For websites, run an iisreset. Call from a remote method that detects the server restoration and recovers
        /// </summary>
        public abstract void IISReset();
        //
        //====================================================================================================
        /// <summary>
        /// return true if the itemToFind is in the delimitedString
        /// </summary>
        /// <param name="delimitedString"></param>
        /// <param name="itemToFind"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public abstract bool IsInDelimitedString(string delimitedString, string itemToFind, string delimiter);
        //
        //====================================================================================================
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="addIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, string value, bool addIfMissing);
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, string value);
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, int value);
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="addIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, int value, bool addIfMissing);
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, double value);
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="addIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, double value, bool addIfMissing);
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, bool value);
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="addIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, bool value, bool addIfMissing);
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, DateTime value);
        /// <summary>
        /// Update or add a key=value pair within a url like "/path/page?key=value&amp;key=value"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="addIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyLinkQueryString(string url, string key, DateTime value, bool addIfMissing);
        //
        //====================================================================================================
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <param name="AddIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, string QueryValue, bool AddIfMissing);
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, string QueryValue);
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, int QueryValue);
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <param name="AddIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, int QueryValue, bool AddIfMissing);
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, double QueryValue);
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <param name="AddIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, double QueryValue, bool AddIfMissing);
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, bool QueryValue);
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <param name="AddIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, bool QueryValue, bool AddIfMissing);
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, DateTime QueryValue);
        /// <summary>
        /// Update or add a key=value pair within key value pair string like "key=value&amp;key=value"
        /// </summary>
        /// <param name="WorkingQuery"></param>
        /// <param name="QueryName"></param>
        /// <param name="QueryValue"></param>
        /// <param name="AddIfMissing"></param>
        /// <returns></returns>
        public abstract string ModifyQueryString(string WorkingQuery, string QueryName, DateTime QueryValue, bool AddIfMissing);
        //
        //====================================================================================================
        /// <summary>
        /// return the components of a url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="return_protocol"></param>
        /// <param name="return_domain"></param>
        /// <param name="return_path"></param>
        /// <param name="return_page"></param>
        /// <param name="return_queryString"></param>
        public abstract void SeparateURL(string url, ref string return_protocol, ref string return_domain, ref string return_path, ref string return_page, ref string return_queryString);
        /// <summary>
        /// return the components of a url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="return_protocol"></param>
        /// <param name="return_domain"></param>
        /// <param name="return_port"></param>
        /// <param name="return_path"></param>
        /// <param name="return_page"></param>
        /// <param name="return_queryString"></param>
        public abstract void SeparateURL(string url, ref string return_protocol, ref string return_domain, ref string return_port, ref string return_path, ref string return_page, ref string return_queryString);
        //
        //====================================================================================================
        /// <summary>
        /// returns the result of a Split, except it honors quoted text, if a quote is found, it is assumed to also be a delimiter ( 'this"that"theother' = 'this "that" theother' )
        /// </summary>
        /// <param name="wordList"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public abstract object SplitDelimited(string wordList, string delimiter);
        //
        //====================================================================================================
        /// <summary>
        /// Method used to convert application data from DotNet DirectoryInfo object to internal legacy 4.1 Parse string.
        /// </summary>
        /// <param name="DirectoryInfo"></param>
        /// <returns></returns>
        public abstract string Upgrade51ConvertDirectoryInfoArrayToParseString(List<CPFileSystemBaseClass.FolderDetail> DirectoryInfo);
        //
        //====================================================================================================
        /// <summary>
        /// Method used to convert application data from DotNet DirectoryInfo object to internal legacy 4.1 Parse string.
        /// </summary>
        /// <param name="DirectoryInfo"></param>
        /// <returns></returns>
        public abstract string Upgrade51ConvertFileInfoArrayToParseString(List<CPFileSystemBaseClass.FileDetail> DirectoryInfo);
        //
        //====================================================================================================
        /// <summary>
        /// wrapped dotnet namespace for use in scripting
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract string EncodeUrl(string source);
        //
        //====================================================================================================
        /// <summary>
        /// wrapped dotnet namespace for use in scripting
        /// </summary>
        /// <param name="Url"></param>
        /// <returns></returns>
        public abstract string DecodeUrl(string Url);
        //
        //====================================================================================================
        /// <summary>
        /// wrapped dotnet namespace for use in scripting
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract string EncodeHTML(string source);
        //
        //====================================================================================================
        /// <summary>
        ///Returns current DateTime.Now if cp.Mock.DateTime is not set, else returns Mock.DateTime
        /// </summary>
        /// <returns></returns>
        public abstract DateTime GetDateTimeMockable();
        //
        //====================================================================================================
        /// <summary>
        /// wrapped dotnet namespace for use in scripting
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract string DecodeHTML(string source);
        //
        //====================================================================================================
        // deprecated
        /// <summary>
        /// Deprecated, use AppendLog
        /// </summary>
        /// <param name="Text"></param>
        [Obsolete("Deprecated, use AppendLog", false)] public abstract void AppendLogFile(string Text);
        /// <summary>
        /// Deprecated, file logging is no longer supported. Use AppendLog(message) to log Info level messages
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="logText"></param>
        [Obsolete("Deprecated, file logging is no longer supported. Use AppendLog(message) to log Info level messages", false)] public abstract void AppendLog(string pathFilename, string logText);
        /// <summary>
        /// Deprecated. use cp.addon.execute()
        /// </summary>
        /// <param name="idGuidOrName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. use cp.addon.execute()", false)] public abstract string ExecuteAddon(string idGuidOrName);
        /// <summary>
        /// Deprecated. use cp.addon.execute() and manage the wrapper manually.
        /// </summary>
        /// <param name="IdGuidOrName"></param>
        /// <param name="WrapperId"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. use cp.addon.execute() and manage the wrapper manually.", false)] public abstract string ExecuteAddon(string IdGuidOrName, int WrapperId);
        /// <summary>
        /// Deprecated. use cp.addon.execute()
        /// </summary>
        /// <param name="IdGuidOrName"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. use cp.addon.execute()", false)] public abstract string ExecuteAddon(string IdGuidOrName, addonContext context);
        /// <summary>
        /// Deprecated. use cp.addon.executeAsProcess()
        /// </summary>
        /// <param name="IdGuidOrName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. use cp.addon.executeAsProcess()", false)] public abstract string ExecuteAddonAsProcess(string IdGuidOrName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="ServerHost"></param>
        /// <param name="ServerVirtualPath"></param>
        /// <returns></returns>
        [Obsolete("Deprecated", false)] public abstract string ConvertLinkToShortLink(string URL, string ServerHost, string ServerVirtualPath);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="PathPagePrefix"></param>
        /// <returns></returns>
        [Obsolete("Deprecated", false)] public abstract string ConvertShortLinkToLink(string URL, string PathPagePrefix);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="GMTDate"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use native methods to convert date formats.", false)] public abstract DateTime DecodeGMTDate(string GMTDate);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="isNewApp"></param>
        [Obsolete("Installation upgrade through the cp interface is deprecated. Please use the command line tool.", false)]        
        public abstract void Upgrade(bool isNewApp);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <returns></returns>
        [Obsolete("Deprecated", false)] public abstract string GetPleaseWaitEnd();
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <returns></returns>
        [Obsolete("Deprecated", false)] public abstract string GetPleaseWaitStart();
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [Obsolete("Use System.Web.HttpUtility.JavaScriptStringEncode()", false)] public abstract string EncodeJavascript(string source);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        [Obsolete("Encode each key value first with EncodeResponseVariable(), then assemble them into the querystring.", false)] public abstract string EncodeQueryString(string Source);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="Date0"></param>
        /// <param name="Date1"></param>
        /// <returns></returns>
        [Obsolete("Deprecated", false)] public abstract DateTime GetFirstNonZeroDate(DateTime Date0, DateTime Date1);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="Integer0"></param>
        /// <param name="Integer1"></param>
        /// <returns></returns>
        [Obsolete("Deprecated", false)] public abstract int GetFirstNonZeroInteger(int Integer0, int Integer1);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="DigitCount"></param>
        /// <returns></returns>
        [Obsolete("Use string.PadRight() and string.PadLeft()", false)] public abstract string GetIntegerString(int Value, int DigitCount);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="Body"></param>
        /// <returns></returns>
        [Obsolete("Use new StringReader(str).ReadLine()", false)] public abstract string GetLine(string Body);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use Process.GetCurrentProcess().Id", false)] public abstract int GetProcessID();
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="SourceURL"></param>
        /// <param name="Protocol"></param>
        /// <param name="Host"></param>
        /// <param name="Port"></param>
        /// <param name="Path"></param>
        /// <param name="Page"></param>
        /// <param name="QueryString"></param>
        [Obsolete("Use SeparateUrl", false)] public abstract void ParseURL(string SourceURL, ref string Protocol, ref string Host, ref string Port, ref string Path, ref string Page, ref string QueryString);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="timeMSec"></param>
        [Obsolete("Use System.Threading.Thread.Sleep()", false)] public abstract void Sleep(int timeMSec);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, some server audits fail if Md5 use detected.")] public abstract string hashMd5(string source);
        /// <summary>
        /// Install an addon collection file asynchonously. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        /// <param name="privateFile"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use CP.Addon.InstallCollectionFile().", false)] public abstract int installCollectionFromFile(string privateFile);
        /// <summary>
        /// Install all addon collections in a folder asynchonously. Optionally delete the folder. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        /// <param name="privateFolder"></param>
        /// <param name="deleteFolderWhenDone"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use CP.Addon.InstallCollectionFile().", false)] public abstract int installCollectionsFromFolder(string privateFolder, bool deleteFolderWhenDone);
        /// <summary>
        /// Install all addon collections in a folder asynchonously. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        /// <param name="privateFolder"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use CP.Addon.InstallCollectionFile().", false)] public abstract int installCollectionsFromFolder(string privateFolder);
        /// <summary>
        /// Install an addon collections from the collection library asynchonously. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        /// <param name="collectionGuid"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use CP.Addon.InstallCollectionFromLibrary().", false)] public abstract int installCollectionFromLibrary(string collectionGuid);
        //====================================================================================================
        /// <summary>
        /// Install an addon collections from an endpoint asynchonously. The task is queued and the taskId is returned. Use cp.tasks.getTaskStatus to determine status
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use CP.Addon.InstallCollectionFromLink().", false)] public abstract int installCollectionFromLink(string link);
    }
}

