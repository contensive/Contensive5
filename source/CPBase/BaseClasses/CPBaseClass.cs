
using Contensive.BaseModels;
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    //
    //====================================================================================================
    /// <summary>
    /// CP - The object passed to an addon in the add-ons execute method. See the AddonBaseClass for details of the addon execute method.
    /// </summary>
    /// <remarks></remarks>
    public abstract class CPBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// The addon class handles access to an add-on's features. Use the Utils object to run an cpcore.addon. An instance of the Addon class is passed to the executing addon in the MyAddon object so it can access any features needed. See the CPAddonBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPAddonBaseClass Addon { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Classes and methods to create forms for the admin user interface
        /// </summary>
        public abstract CPAdminUIBaseClass AdminUI { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Return the configuration of the app name specified. Use 
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        public abstract AppConfigBaseModel GetAppConfig(string appName);
        //
        //====================================================================================================
        /// <summary>
        /// The current application
        /// </summary>
        /// <returns></returns>
        public abstract AppConfigBaseModel GetAppConfig();
        //
        //====================================================================================================
        /// <summary>
        /// A list of the names (keys) for all apps on this server groups
        /// </summary>
        public abstract List<string> GetAppNameList();
        //
        //====================================================================================================
        //
        //====================================================================================================
        /// <summary>
        /// Construct new Block object. See CPBlockBaseClass for Block Details
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPBlockBaseClass BlockNew();
        //
        //====================================================================================================
        /// <summary>
        /// The Cache objects handles caching. Use this class to save blocks of data you will use again. See CPCacheBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPCacheBaseClass Cache { get; }
        //
        //====================================================================================================
        /// <summary>
        /// read and write cdn files, like content uploads. Sites with static front-ends may put static files here.
        /// </summary>
        /// <returns></returns>
        public abstract CPFileSystemBaseClass CdnFiles { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Content class handles functions related to content meta such as determining the table used for a content definition, getting a recordid based on the name, or accessing the methods that control workflow publishing. See CPContentBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPContentBaseClass Content { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Construct new CS object. See CPCSBaseClass for CS object details 
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPCSBaseClass CSNew();
        //
        //====================================================================================================
        /// <summary>
        /// The Db object handles direct access to the Database. The ContentSet functions in the CPCSBaseClass are prefered for general use. See the CPDBBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPDateBaseClass Date { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Db object handles direct access to the Database. The ContentSet functions in the CPCSBaseClass are prefered for general use. See the CPDBBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPDbBaseClass Db { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Construct new Db object. CP.Db methods are for the default datasource. 
        /// Use this method to create a db object for other datasources. 
        /// See CPDbBaseClass for Db object details 
        /// </summary>
        /// <returns>Returns a new Db class. If the Datasource cannot be opened an exception is thrown.</returns>
        /// <remarks></remarks>
        public abstract CPDbBaseClass DbNew(string DataSourceName);
        //
        //====================================================================================================
        /// <summary>
        /// The Doc object handles features related to the document (page) being contructed in the current call. See CPDocBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPDocBaseClass Doc { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Email object handles email functions. See CPEmailBaseClass for more information.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPEmailBaseClass Email { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Legacy file object. Use FileCdn, FileAppRoot, FilePrivate and tempFiles.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Legacy file object. Use FileCdn, FileAppRoot, FilePrivate and tempFiles.")]
        public abstract CPFileBaseClass File { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Group Object accesses group features. Group Features generally associate people and roles. See CPGroupBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPGroupBaseClass Group { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The HTML class handles functions used to read and produce HTML elements. See CPHtmlBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPHtmlBaseClass Html { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The HTML class handles functions used to read and produce HTML elements. See CPHtmlBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPHtml5BaseClass Html5 { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Helper class for common http request methods. For saving http verbs to files, see the file object
        /// </summary>
        public abstract CPHttpBaseClass Http { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Image controller
        /// </summary>
        public abstract CPImageBaseClass Image { get; }
        //
        //====================================================================================================
        /// <summary>
        /// utilities for json
        /// </summary>
        public abstract CPJSONBaseClass JSON { get; }
        //
        //====================================================================================================
        /// <summary>
        /// manage application layout records.
        /// </summary>
        public abstract CPLayoutBaseClass Layout { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Log class manages server logs
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPLogBaseClass Log { get; }
        //
        //====================================================================================================
        /// <summary>
        /// read and write messages to a standard message queue
        /// </summary>
        public abstract CPMessageQueueBaseClass MessageQueue { get; }
        //
        //====================================================================================================
        /// <summary>
        /// publish to an mqtt broker
        /// </summary>
        public abstract CPMQTTBaseClass MQTT { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Manage mustache compatible template language operations
        /// </summary>
        public abstract CPMustacheBaseClass Mustache { get; }
        //
        //====================================================================================================
        /// <summary>
        /// read and write files not available to the Internet
        /// </summary>
        /// <returns></returns>
        public abstract CPFileSystemBaseClass PrivateFiles { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Request object handles data associated with the request from the visitor. See CPRequestBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPRequestBaseClass Request { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Response object handles the stream of data back to the visitor. See CPResponseBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPResponseBaseClass Response { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Server configuration
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract ServerConfigBaseModel ServerConfig { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Site Class handles features related to the current site. See CPSiteBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPSiteBaseClass Site { get; }
        //
        //====================================================================================================
        /// <summary>
        /// read and write files in a temporary location.
        /// </summary>
        /// <returns></returns>
        public abstract CPSecurityBaseClass Security { get; }
        //
        //====================================================================================================
        /// <summary>
        /// exposes base credentials and methods to get/set credentials
        /// </summary>
        public abstract CPSecretsBaseClass Secrets { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Text messaging services
        /// </summary>
        public abstract CPSMSBaseClass SMS { get; }
        //
        //====================================================================================================
        /// <summary>
        /// A local file system that automatically deletes files
        /// </summary>
        public abstract CPFileSystemBaseClass TempFiles { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The User Class handles details related to the user and its related people record. See CPUserBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPUserBaseClass User { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The UserError Class handles error handling for those conditions you want the user to know about or correct. For example an login error. See the CPUserErrorBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPUserErrorBaseClass UserError { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Visit Class handles details related to the visit. For instance it holds the number of pages hit so far and has methods for adding and modifying user defined visit properties. See CPVisitBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPVisitBaseClass Visit { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Visitor Class handles details related to the visitor. For instance it holds the browser type used by the visitor. See CPVisitorBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPVisitorBaseClass Visitor { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The Utils class handles basic utilities and other features not classified. See CPUtilsBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract CPUtilsBaseClass Utils { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Contensive version
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string Version { get; }
        //
        //====================================================================================================
        /// <summary>
        /// read and write files in the root folder of the application (appRoot, wwwRoot,htdocs,etc)
        /// </summary>
        /// <returns></returns>
        public abstract CPFileSystemBaseClass WwwFiles { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The MyAddon object is an instance of the Addon class created before an add-ons execute method is called. See CPAddonBaseClass for more details.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Legacy object. To determine addon properties of the current addon, use CP.Addon.id to create a model.", false)]
        public abstract CPAddonBaseClass MyAddon { get; }
        //
    }
}
