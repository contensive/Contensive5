
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Contensive.BaseClasses {
    /// <summary>
    /// CP.Addon - The Addon class represents the instance of an add-on. To use this class, use its constructor and open an cpcore.addon. 
    /// Use these properties to retrieve it's configuration
    /// </summary>
    /// <remarks></remarks>
    public abstract class CPAddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in the default addon environment (type=simple, etc) and returns its resulting object. Generally addons return strings.
        /// </summary>
        /// <param name="addonGuid"></param>
        /// <returns></returns>
        public abstract string Execute(string addonGuid);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in the default addon environment (type=simple, etc) and returns its resulting object. Generally addons return strings. Pass arguments to the addon that can be read with cp.doc.get() methods.
        /// </summary>
        /// <param name="addonGuid">The guid of the addon to be executed.</param>
        /// <param name="argumentKeyValuePairs">The executing addon can read these arguments with methods like cp.doc.getText("key").</param>
        /// <returns></returns>
        public abstract string Execute(string addonGuid, Dictionary<string, string> argumentKeyValuePairs);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in the default addon environment (type=simple, etc) and returns its resulting object. Generally addons return strings. Provide details for the execution environment, such as argument key value pairs.
        /// </summary>
        /// <param name="addonGuid">The guid of the addon to be executed.</param>
        /// <param name="executeContext">The context where the addon is being executed (on a page, in an email, etc.). Typical is 'Simple' which blocks html comments.</param>
        /// <returns></returns>
        public abstract string Execute(string addonGuid, CPUtilsBaseClass.addonExecuteContext executeContext);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in the default addon environment (type=simple, etc) and returns its resulting object. Generally addons return strings.
        /// </summary>
        /// <param name="addonId">The id of the addon to be executed.</param>
        /// <returns></returns>
        public abstract string Execute(int addonId);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in the default addon environment (type=simple, etc) and returns its resulting object. Generally addons return strings. Pass arguments to the addon that can be read with cp.doc.get() methods.
        /// </summary>
        /// <param name="addonId">The id of the addon to be executed.</param>
        /// <param name="argumentKeyValuePairs">The executing addon can read these arguments with methods like cp.doc.getText("key").</param>
        /// <returns></returns>
        public abstract string Execute(int addonId, Dictionary<string, string> argumentKeyValuePairs);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in the default addon environment (type=simple, etc) and returns its resulting object. Generally addons return strings. Provide details for the execution environment, such as argument key value pairs.
        /// </summary>
        /// <param name="addonId">The id of the addon to be executed.</param>
        /// <param name="executeContext">The context where the addon is being executed (on a page, in an email, etc.). Typical is 'Simple' which blocks html comments.</param>
        /// <returns></returns>
        public abstract string Execute(int addonId, CPUtilsBaseClass.addonExecuteContext executeContext);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in the default addon environment (type=simple, etc) and returns its resulting object. Generally addons return strings.
        /// </summary>
        /// <param name="addonName">The name of the addon to be executed.</param>
        /// <returns></returns>
        public abstract string ExecuteByUniqueName(string addonName);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in the default addon environment (type=simple, etc) and returns its resulting object. Generally addons return strings. Pass arguments to the addon that can be read with cp.doc.get() methods.
        /// </summary>
        /// <param name="addonName">The name of the addon to be executed.</param>
        /// <param name="argumentKeyValuePairs">The executing addon can read these arguments with methods like cp.doc.getText("key").</param>
        /// <returns></returns>
        public abstract string ExecuteByUniqueName(string addonName, Dictionary<string, string> argumentKeyValuePairs);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in the default addon environment (type=simple, etc) and returns its resulting object. Generally addons return strings. Provide details for the execution environment, such as argument key value pairs.
        /// </summary>
        /// <param name="addonName">The name of the addon to be executed.</param>
        /// <param name="executeContext">The executing addon can read these arguments with methods like cp.doc.getText("key").</param>
        /// <returns></returns>
        public abstract string ExecuteByUniqueName(string addonName, CPUtilsBaseClass.addonExecuteContext executeContext);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in a background process with the current session environment.
        /// </summary>
        /// <param name="addonName">The name of the addon to be executed.</param>
        public abstract void ExecuteAsProcessByUniqueName(string addonName);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in a background process with the current session environment. Arguments can be passed that the addon can read with cp.doc.get methods.
        /// </summary>
        /// <param name="addonName">The name of the addon to be executed.</param>
        /// <param name="argumentKeyValuePairs">The executing addon can read these arguments with methods like cp.doc.getText("key").</param>
        public abstract void ExecuteAsProcessByUniqueName(string addonName, Dictionary<string, string> argumentKeyValuePairs);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in a background process with the current session environment.
        /// </summary>
        /// <param name="addonGuid">The guid of the addon to be executed.</param>
        public abstract void ExecuteAsProcess(string addonGuid);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in a background process with the current session environment. Arguments can be passed that the addon can read with cp.doc.get methods.
        /// </summary>
        /// <param name="addonGuid">The guid of the addon to be executed.</param>
        /// <param name="argumentKeyValuePairs">The executing addon can read these arguments with methods like cp.doc.getText("key").</param>
        public abstract void ExecuteAsProcess(string addonGuid, Dictionary<string, string> argumentKeyValuePairs);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in a background process with the current session environment.
        /// </summary>
        /// <param name="addonid">The id of the addon to be executed.</param>
        public abstract void ExecuteAsProcess(int addonid);
        //
        //====================================================================================================
        /// <summary>
        /// Execute an addon in a background process with the current session environment. Arguments can be passed that the addon can read with cp.doc.get methods.
        /// </summary>
        /// <param name="addonid">The id of the addon to be executed.</param>
        /// <param name="argumentKeyValuePairs"></param>
        public abstract void ExecuteAsProcess(int addonid, Dictionary<string, string> argumentKeyValuePairs);
        //
        //====================================================================================================
        /// <summary>
        /// Install addon collection from a file in private files
        /// </summary>
        /// <param name="tempPathFilename">The path and filename in the tempFiles store. A path starts with the folder name and ends with a slash (like myfolder\subfolder\)</param>
        /// <param name="returnUserError">If the installation is successful, this returns string.empty.</param>
        /// <returns></returns>
        public abstract bool InstallCollectionFile(string tempPathFilename, ref string returnUserError);
        //
        //====================================================================================================
        /// <summary>
        /// Install addon collection from a file in private files
        /// </summary>
        /// <param name="tempPathFilename">The path and filename in the tempFiles store. A path starts with the folder name and ends with a slash (like myfolder\subfolder\)</param>
        /// <returns></returns>
        public abstract int InstallCollectionFileAsync(string tempPathFilename);
        //
        //====================================================================================================
        /// <summary>
        /// Install addon collection by its guid from the addon collection library
        /// </summary>
        /// <param name="tempFolder">The path and filename in the tempFiles store. A path starts with the folder name and ends with a slash (like myfolder\subfolder\)</param>
        /// <param name="deleteFolderWhenDone">Delete the temp folder when it is no longer needed.</param>
        /// <param name="returnUserError">If the installation is successful, this returns string.empty.</param>
        /// <returns></returns>
        public abstract bool InstallCollectionsFromFolder(string tempFolder, bool deleteFolderWhenDone, ref string returnUserError);
        //
        //====================================================================================================
        /// <summary>
        /// Install addon collection by its guid from the addon collection library
        /// </summary>
        /// <param name="tempFolder">The path and filename in the tempFiles store. A path starts with the folder name and ends with a slash (like myfolder\subfolder\)</param>
        /// <param name="deleteFolderWhenDone">Delete the temp folder when it is no longer needed.</param>
        /// <returns></returns>
        public abstract int InstallCollectionsFromFolderAsync(string tempFolder, bool deleteFolderWhenDone);
        //
        //====================================================================================================
        /// <summary>
        /// Install addon collection by its guid from the addon collection library
        /// </summary>
        /// <param name="collectionGuid">The guid of the collection to be installed.</param>
        /// <param name="returnUserError">If the installation is successful, this returns string.empty.</param>
        /// <returns></returns>
        public abstract bool InstallCollectionFromLibrary(string collectionGuid, ref string returnUserError);
        //
        //====================================================================================================
        /// <summary>
        /// Install addon collection by its guid from the addon collection library
        /// </summary>
        /// <param name="collectionGuid">The guid of the collection to be installed.</param>
        /// <returns></returns>
        public abstract int InstallCollectionFromLibraryAsync(string collectionGuid);
        //
        //====================================================================================================
        /// <summary>
        /// Install addon collection from a link. This link should download a collection zip or xml file when requested.
        /// </summary>
        /// <param name="collectionFileLink">This link should download a collection zip or xml file when requested.</param>
        /// <param name="returnUserError">If the installation is successful, this returns string.empty.</param>
        /// <returns></returns>
        public abstract bool InstallCollectionFromLink(string collectionFileLink, ref string returnUserError);
        //
        //====================================================================================================
        /// <summary>
        /// Install addon collection by its guid from the addon collection library.
        /// </summary>
        /// <param name="collectionFileLink">This link should download a collection zip or xml file when requested.</param>
        /// <returns></returns>
        public abstract int InstallCollectionFromLinkAsync(string collectionFileLink);
        //
        //====================================================================================================
        /// <summary>
        /// Package a collection into a collection zip file and return a path to the zip file in the cdnFiles store.
        /// </summary>
        /// <param name="collectionId">The id of the collection to be exported.</param>
        /// <param name="collectionZipCdnPathFilename">A path in the cdnFiles store. (ex myfolder\subfolder\)</param>
        /// <param name="returnUserError">If the export is successful, this returns string.empty.</param>
        /// <returns></returns>
        public abstract bool ExportCollection(int collectionId, ref string collectionZipCdnPathFilename, ref string returnUserError);
        //
        //====================================================================================================
        /// <summary>
        /// Package a collection into a collection zip file and return a path to the zip file in the cdnFiles store.
        /// </summary>
        /// <param name="collectionGuid">The guid of the collection to be exported.</param>
        /// <param name="collectionZipCdnPathFilename">A path in the cdnFiles store. (ex myfolder\subfolder\)</param>
        /// <param name="returnUserError">If the export is successful, this returns string.empty.</param>
        /// <returns></returns>
        public abstract bool ExportCollection(string collectionGuid, ref string collectionZipCdnPathFilename, ref string returnUserError);
        //
        //====================================================================================================
        /// <summary>
        /// The id of the addon currently executing.
        /// </summary>
        public abstract int ID { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The guid of the addon currently executing.
        /// </summary>
        public abstract string ccGuid { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When true, this add-on is displayed on and can be used from the admin navigator.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool Admin { get; }
        //
        //====================================================================================================
        /// <summary>
        /// A crlf delimited list of name=value pairs. These pairs create an options dialog available to administrators in advance edit mode. When the addon is executed, the values selected are available through the cp.doc.var("name") method.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string ArgumentList { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When true, this addon returns the javascript code necessary to implement this object as ajax.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool AsAjax { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When true, the system only uses the custom styles field when building the page. This field is not updated with add-on updates.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string BlockDefaultStyles { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The ID local to this site of the collection which installed this cpcore.addon.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract int CollectionID { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When true, this addon can be placed in the content of pages.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool Content { get; }
        //
        //====================================================================================================
        /// <summary>
        /// text copy is added to the addon content during execution.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string Copy { get; }
        //
        //====================================================================================================
        /// <summary>
        /// text copy is added to the addon content during execution.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string CopyText { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Styles that are rendered on the page when the addon is executed. Custom styles are editable and are not modified when the add-on is updated.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string CustomStyles { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Styles that are included with the add-on and are updated when the add-on is updated. See BlockdefaultStyles to block these.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string DefaultStyles { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The add-on description is displayed in the addon manager
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string Description { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When present, the system calls the execute method of an objected created from this dot net class namespace.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string DotNetClass { get; }
        //
        //====================================================================================================
        /// <summary>
        /// This is an xml stucture that the system executes to create an admin form. See the support.contensive.com site for more details.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string FormXML { get; }
        //
        //====================================================================================================
        /// <summary>
        /// This copy is displayed when the help icon for this addon is clicked.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string Help { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If present, this link is displayed when the addon icon is clicked.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string HelpLink { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When present, this icon will be used when the add-on is displayed in the addon manager and when edited. The height, width and sprites must also be set.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string IconFilename { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The height in pixels of the icon referenced by the iconfilename.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract int IconHeight { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The number of images in the icon. There can be multiple images stacked top-to-bottom in the file. The first is the normal image. the second is the hover-over image. The third is the clicked image.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract int IconSprites { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The width of the icon referenced by the iconfilename
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract int IconWidth { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When true, this addon will be displayed in an html iframe.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool InFrame { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When true, the system will assume the addon returns html that is inline, as opposed to block. This is used to vary the edit icon behaviour.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool IsInline { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Javascript code that will be placed in the document right before the end-body tag. Do not include script tags.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string JavaScriptBodyEnd { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Javascript code that will be placed in the head of the document. Do no include script tags.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string JavascriptInHead { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Javascript that will be executed in the documents onload event.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string JavaScriptOnLoad { get; }
        //
        //====================================================================================================
        /// <summary>
        /// A URL to a webserver that returns javascript. This URL will be added as the src attribute of a script tag, and placed in the content where this Add-on is inserted. This URL can be to any server-side program on any server, provided it returns javascript.
        /// For instance, if you have a script page that returns javascript,put the URL of that page here. The addon can be dropped on any page and will execute the script. Your script can be from any site. This technique is used in widgets and avoids the security issues with ajaxing from another site.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string Link { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Text here will be added to the meta description section of the document head.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string MetaDescription { get; }
        //
        //====================================================================================================
        /// <summary>
        /// This is a comma or crlf delimited list of phrases that will be added to the document's meta keyword list
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string MetaKeywordList { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The name of the cpcore.addon.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string Name { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The type of navigator entry to be made. Choices are: Add-on,Report,Setting,Tool
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string NavIconType { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If present, this string will be used as an activex programid to create an object and call it's execute method.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string ObjectProgramID { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If true, this addon will be execute at the end of every page and its content added to right before the end-body tag
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool OnBodyEnd { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If true, this addon will be execute at the start of every page and it's content added to right after the body tag
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool OnBodyStart { get; }
        //
        //====================================================================================================
        /// <summary>
        /// if true, this add-on will be executed on every page and its content added right after the content box.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool OnContentEnd { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If true, this add-on will be executed on every page and its content added right before the content box
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool OnContentStart { get; }
        //
        //====================================================================================================
        [Obsolete("Deprecated. Use a model object instead", false)]
        public abstract bool Open(int AddonId);
        //
        //====================================================================================================
        [Obsolete("Deprecated. Use a model object instead", false)]
        public abstract bool Open(string AddonNameOrGuid);
        //
        //====================================================================================================
        /// <summary>
        /// All content in the field will be added directly, as-is to the document head.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string OtherHeadTags { get; }
        //
        //====================================================================================================
        /// <summary>
        /// All content in the field will be added to the documents title tag
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string PageTitle { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When present, this add-on will be executed stand-alone without a webpage periodically at this interval (in minutes).
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string ProcessInterval { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The next time this add-on is scheduled to run as a processs
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract DateTime ProcessNextRun { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Check true, this addon will be run once within the next minute as a stand-alone process.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool ProcessRunOnce { get; }
        //
        //====================================================================================================
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string RemoteAssetLink { get; }
        //
        //====================================================================================================
        /// <summary>
        /// if true, this add-on can be executed as a remote method. The name of the addon is used as the url.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool RemoteMethod { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When present, this text will be added to the robots.txt content for the site. This content is editable through the preferences page
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string RobotsTxt { get; }
        //
        //====================================================================================================
        /// <summary>
        /// When present, the first routine of this script will be executed when the add-on is executed and its return added to the add-ons return
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string ScriptCode { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If the ScriptCode has more than one routine and you want to run one other than the first, list is here.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string ScriptEntryPoint { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The script language selected for this script.
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string ScriptLanguage { get; }
        //
        //====================================================================================================
        /// <summary>
        /// A comma delimited list of the local id values of shared style record that will display with this add-on
        /// </summary>
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract string SharedStyles { get; }
        //
        //====================================================================================================
        [Obsolete("Deprecated. Use cp.addon.id or cp.addon.ccguid to create a model object and use its properties", false)]
        public abstract bool Template { get; }
    }
}
