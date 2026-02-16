
using Contensive.BaseClasses;
using Contensive.BaseModels;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using NLog;

namespace Contensive.Processor {
    /// <summary>
    /// The main class created to run an application
    /// </summary>
    public class CPClass : CPBaseClass, IDisposable {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// The processor class is an api wrapper around the CoreController
        /// </summary>
        public CoreController core { get; set; }
        //
        //=========================================================================================================
        /// <summary>
        /// constructor for server use. No application context will be available. Use to create new apps or iterate through apps.
        /// </summary>
        /// <remarks></remarks>
        public CPClass() {
            core = new CoreController(this);
        }
        //
        //=========================================================================================================
        /// <summary>
        /// constructor for non-Internet app use. Sessions are disabled. Use CPClass(appName,allowSessions) to enable sessions
        /// </summary>
        /// <remarks></remarks>
        public CPClass(string appName) {
            core = new CoreController(this, appName);
        }
        //
        //=========================================================================================================
        /// <summary>
        /// constructor for non-Internet app use.
        /// If allowSession true, sessions are valid.
        /// If allowSession false, session objects cp.user, cp.visit and cp.visitor are empty. Use to increase performance when sessions are not needed. Properties are shared with all calls. To create a session.user, call session.verifyUser()
        /// </summary>
        /// <remarks></remarks>
        public CPClass(string appName, bool allowSession) {
            core = new CoreController(this, appName, allowSession);
        }
        //
        //=========================================================================================================
        /// <summary>
        /// constructor for iis site use. Configuration read from programdata json
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="appName"></param>
        /// <remarks></remarks>
        public CPClass(string appName, HttpContextModel httpContext) {
            core = new CoreController(this, appName, httpContext);
        }
        //
        //=========================================================================================================
        /// <summary>
        /// return ok if the application is running correctly. Use statusMessage to display the status
        /// </summary>
        public AppConfigBaseModel.AppStatusEnum status {
            get {
                return core.appConfig.appStatus;
            }
        }
        //
        //=========================================================================================================
        /// <summary>
        /// return a message that can be used to display status
        /// </summary>
        public string statusMessage {
            get {
                return GenericController.getApplicationStatusMessage(core.appConfig.appStatus);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns true if the server config file is valid (currently only requires a valid db)
        /// </summary>
        /// <returns></returns>
        public bool serverOk {
            get {
                if (core?.serverConfig == null) { return false; }
                return !string.IsNullOrEmpty(core.secrets.defaultDataSourceAddress);
            }
        }
        //
        //=========================================================================================================
        /// <summary>
        /// returns true if the current application has status set OK (not disabled)
        /// </summary>
        public bool appOk {
            get {
                if ((core?.serverConfig == null) || (core?.appConfig == null)) { return false; }
                if (core.appConfig.appStatus == AppConfigBaseModel.AppStatusEnum.ok && core.appConfig.enabled) { return true; }
                return false;
            }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Executes a specific route. The route can be a remote method, link alias, admin route, etc.
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public string executeRoute(string route) 
            => RouteController.executeRoute(core, route);
        //
        //==========================================================================================
        /// <summary>
        /// Executes the default route set in the admin settings is used.
        /// </summary>
        /// <returns></returns>
        public string executeRoute() 
            => RouteController.executeRoute(core, "");
        //
        //====================================================================================================
        /// <summary>
        /// executes an addon with the name or guid provided, in the context specified.
        /// </summary>
        /// <param name="addonNameOrGuid"></param>
        /// <param name="addonContext"></param>
        /// <returns></returns>
        public string executeAddon(string addonNameOrGuid, CPUtilsBaseClass.addonContext addonContext = CPUtilsBaseClass.addonContext.ContextSimple) {
            try {
                if (core?.appConfig?.enabled == null || !core.appConfig.enabled) {
                    if (core == null) { throw new GenericException("cp.executeAddon failed because coreController null"); }
                    if (core?.appConfig == null) { throw new GenericException("cp.executeAddon failed because core.appConfig null"); }
                    logger.Debug($"{core.logCommonMessage},cp.executeAddon returned empty because application [" + core.appConfig.name + "] is marked inactive in config.json");
                    return string.Empty;
                }
                if (GuidController.isGuid(addonNameOrGuid)) {
                    //
                    // -- call by guid
                    AddonModel addonByGuid = core.cacheRuntime.addonCache.create(addonNameOrGuid);
                    if (addonByGuid == null) {
                        throw new GenericException("Addon [" + addonNameOrGuid + "] could not be found.");
                    } else {
                        return core.addon.execute(addonByGuid, new CPUtilsBaseClass.addonExecuteContext {
                            addonType = addonContext,
                            errorContextMessage = "external call to execute addon [" + addonNameOrGuid + "]"
                        });
                    }
                }
                AddonModel addonByName = core.cacheRuntime.addonCache.createByUniqueName(addonNameOrGuid);
                if (addonByName != null) {
                    //
                    // -- call by name
                    return core.addon.execute(addonByName, new CPUtilsBaseClass.addonExecuteContext {
                        addonType = addonContext,
                        errorContextMessage = "external call to execute addon [" + addonNameOrGuid + "]"
                    });
                } else if (addonNameOrGuid.isNumeric()) {
                    //
                    // -- compatibility - call by id
                    return executeAddon(GenericController.getInteger(addonNameOrGuid), addonContext);
                } else {
                    throw new GenericException("Addon [" + addonNameOrGuid + "] could not be found.");
                }
            } catch (Exception ex) {
                Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// executes an addon with the id provided, in the context specified.
        /// </summary>
        /// <param name="addonId"></param>
        /// <param name="addonContext"></param>
        /// <returns></returns>
        public string executeAddon(int addonId, CPUtilsBaseClass.addonContext addonContext = CPUtilsBaseClass.addonContext.ContextSimple) {
            try {
                if (core?.appConfig?.enabled == null || !core.appConfig.enabled) {
                    if (core == null) { throw new GenericException("cp.executeAddon failed because coreController null"); }
                    if (core?.appConfig == null) { throw new GenericException("cp.executeAddon failed because core.appConfig null"); }
                    logger.Debug($"{core.logCommonMessage},cp.executeAddon returned empty because application [" + core.appConfig.name + "] is marked inactive in config.json");
                    return string.Empty;
                }
                AddonModel addon = core.cacheRuntime.addonCache.create(addonId);
                if (addon == null) {
                    throw new GenericException("Addon [#" + addonId.ToString() + "] could not be found.");
                } else {
                    return core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                        addonType = addonContext,
                        errorContextMessage = "external call to execute addon [" + addonId + "]"
                    });
                }
            } catch (Exception ex) {
                Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //=========================================================================================================
        /// <summary>
        /// Create a new block object, used to manipulate html elements using htmlClass and htmlId. Alternatively create a block object with its constructor.
        /// </summary>
        /// <returns></returns>
        public override CPBlockBaseClass BlockNew() {
            return new CPBlockClass(this);
        }
        //
        //=========================================================================================================
        /// <summary>
        /// Create a new data set object used to run queries and open tables with soft table names (determined run-time). Alternatively create a data set object with its constructor
        /// </summary>
        /// <returns></returns>
        public override CPCSBaseClass CSNew() {
            return new CPCSClass(this);
        }
        //
        //=========================================================================================================
        /// <summary>
        /// Create a datasource. The default datasource is CP.Db
        /// </summary>
        /// <param name="DataSourceName"></param>
        /// <returns></returns>
        public override CPDbBaseClass DbNew(string DataSourceName) {
            return new CPDbClass(this, DataSourceName);
        }
        //
        //=========================================================================================================
        /// <summary>
        /// system version
        /// </summary>
        public override string Version {
            get {
                return CoreController.codeVersion();
            }
        }
        //
        //=========================================================================================================
        /// <summary>
        /// expose the user error object
        /// </summary>
        public override CPUserErrorBaseClass UserError {
            get {
                if (_userErrorObj == null) {
                    _userErrorObj = new CPUserErrorClass(this);
                }
                return _userErrorObj;
            }
        }
        private CPUserErrorClass _userErrorObj;
        //
        //=========================================================================================================
        /// <summary>
        /// expose the user object
        /// </summary>
        public override CPUserBaseClass User {
            get {
                if (_userObj == null) {
                    if(core?.session is not null ) {
                        core.session.verifyUser();
                    }
                    _userObj = new CPUserClass(this);
                }
                return _userObj;
            }
        }
        private CPUserClass _userObj;
        //
        //=========================================================================================================
        /// <summary>
        /// expose object for managing addons
        /// </summary>
        public override CPAddonBaseClass Addon {
            get {
                if (_addonObj == null) {
                    _addonObj = new CPAddonClass(this);
                }
                return _addonObj;
            }
        }
        private CPAddonClass _addonObj;
        //
        //=========================================================================================================
        /// <summary>
        /// expose object for managing cache. This cache designed to cache Db record objects and Domain Objects
        /// </summary>
        public override CPCacheBaseClass Cache {
            get {
                if (_cacheObj == null) {
                    _cacheObj = new CPCacheClass(this);
                }
                return _cacheObj;
            }
        }
        private CPCacheClass _cacheObj;
        //
        //=========================================================================================================
        /// <summary>
        /// expose an object for managing content
        /// </summary>
        public override CPContentBaseClass Content {
            get {
                if (_contentObj == null) {
                    _contentObj = new CPContentClass(this);
                }
                return _contentObj;
            }
        }
        private CPContentClass _contentObj;
        //
        //=========================================================================================================
        /// <summary>
        /// manage time for user's, the primary business location and secondary locations
        /// </summary>
        public override CPDateBaseClass Date {
            get {
                if (_Date != null) { return _Date; }
                _Date = new CPDateClass(this);
                return _Date;
            }
        }
        private CPDateClass _Date;
        //
        //=========================================================================================================
        /// <summary>
        /// Properties and methods helpful in access the database
        /// </summary>
        public override CPDbBaseClass Db {
            get {
                if (_dbObj == null) {
                    _dbObj = new CPDbClass(this, "");
                }
                return _dbObj;
            }
        }
        private CPDbClass _dbObj;
        //
        //=========================================================================================================
        /// <summary>
        /// Properties and methods helpful in creating a return document. 
        /// </summary>
        public override CPDocBaseClass Doc {
            get {
                if (_docObj == null) {
                    _docObj = new CPDocClass(this);
                }
                return _docObj;
            }
        }
        private CPDocClass _docObj;
        //
        //====================================================================================================
        /// <summary>
        /// Properties and methods helpful in managing email
        /// </summary>
        public override CPEmailBaseClass Email {
            get {
                if (_emailObj == null) {
                    _emailObj = new CPEmailClass(this);
                }
                return _emailObj;
            }
        }
        private CPEmailClass _emailObj;
        //
        //====================================================================================================
        /// <summary>
        /// Legacy method that provides access the current application server. AS of v5, access is limited to that provided by FilePrivate, wwwRoot, temp and cdnFiles
        /// </summary>
        [Obsolete("deprecated", true)]
        public override CPFileBaseClass File {
            get {
                if (_fileObj == null) {
                    _fileObj = new CPFileClass(this);
                }
                return _fileObj;
            }
        }
        private CPFileClass _fileObj;
        //
        //====================================================================================================
        /// <summary>
        /// Properties and methods helpful in managing groups
        /// </summary>
        public override CPGroupBaseClass Group {
            get {
                if (_groupObj == null) {
                    _groupObj = new CPGroupClass(this);
                }
                return _groupObj;
            }
        }
        private CPGroupClass _groupObj;
        //
        //====================================================================================================
        /// <summary>
        /// Properties and methods helpful in creating html documents
        /// </summary>
        public override CPHtmlBaseClass Html {
            get {
                if (_htmlObj == null) {
                    _htmlObj = new CPHtmlClass(this);
                }
                return _htmlObj;
            }
        }
        private CPHtmlClass _htmlObj;
        //
        //====================================================================================================
        /// <summary>
        /// Properties and methods helpful in creating html documents
        /// </summary>
        public override CPHtml5BaseClass Html5 {
            get {
                if (_html5Obj == null) {
                    _html5Obj = new CPHtml5Class(this);
                }
                return _html5Obj;
            }
        }
        private CPHtml5Class _html5Obj;
        //
        //====================================================================================================
        /// <summary>
        /// http helper class
        /// </summary>
        public override CPHttpBaseClass Http {
            get {
                if (_HttpObj == null) {
                    _HttpObj = new CPHttpClass(this);
                }
                return _HttpObj;
            }
        }
        private CPHttpClass _HttpObj;
        //
        //=========================================================================================================
        //
        public override CPImageBaseClass Image {
            get {
                if (_Image == null) {
                    _Image = new CPImageClass(this);
                }
                return _Image;
            }
        }
        private CPImageClass _Image;

        //
        //====================================================================================================
        /// <summary>
        /// Json serialize/deserialize
        /// </summary>
        public override CPJSONBaseClass JSON {
            get {
                if (_jsonObj == null) {
                    _jsonObj = new CPJSONClass();
                }
                return _jsonObj;
            }
        }
        private CPJSONBaseClass _jsonObj;
        //
        //====================================================================================================
        /// <summary>
        /// Return a persistent object for this functionality
        /// </summary>
        public override CPLogBaseClass Log {
            get {
                if (logObj == null) {
                    logObj = new CPLogClass(this);
                }
                return logObj;
            }
        }
        private CPLogClass logObj;
        //
        //====================================================================================================
        /// <summary>
        /// Return a persistent object for this functionality
        /// </summary>
        public override CPLayoutBaseClass Layout {
            get {
                if (layoutObj == null) {
                    layoutObj = new CPLayoutClass(this);
                }
                return layoutObj;
            }
        }
        private CPLayoutClass layoutObj;
        //
        //====================================================================================================
        /// <summary>
        /// Return a persistent object for this functionality
        /// </summary>
        public override CPMessageQueueBaseClass MessageQueue {
            get {
                if (MessageQueueObj == null) {
                    MessageQueueObj = new CPMessageQueueClass(this);
                }
                return MessageQueueObj;
            }
        }
        private CPMessageQueueClass MessageQueueObj;
        //
        //====================================================================================================
        /// <summary>
        /// MQTT interface
        /// </summary>
        public override CPMQTTBaseClass MQTT {
            get {
                if (MQTT_local == null) {
                    MQTT_local = new CPMQTTClass(this);
                }
                return MQTT_local;
            }
        }
        private CPMQTTBaseClass MQTT_local;
        //
        //====================================================================================================
        /// <summary>
        /// Return a persistent object for this functionality
        /// </summary>
        public override CPMustacheBaseClass Mustache {
            get {
                if (mustacheObj == null) {
                    mustacheObj = new CPMustacheClass(this);
                }
                return mustacheObj;
            }
        }
        private CPMustacheClass mustacheObj;
        //
        //====================================================================================================
        /// <summary>
        /// Return a persistent object for this functionality
        /// </summary>
        [Obsolete("Deprecated. To access addon details of the addon running, create a model with the cp.addon.id", true)]
        public override CPAddonBaseClass MyAddon {
            get {
                return Addon;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Object that provides access to the application request, typically a webserver request.
        /// </summary>
        public override CPRequestBaseClass Request {
            get {
                if (_requestObj == null) {
                    _requestObj = new CPRequestClass(this);
                }
                return _requestObj;
            }
        }
        private CPRequestClass _requestObj;
        //
        //====================================================================================================
        /// <summary>
        /// Object that provides access to the application response, typically a webserver response.
        /// </summary>
        public override CPResponseBaseClass Response {
            get {
                if (_responseObj == null) {
                    _responseObj = new CPResponseClass(this);
                }
                return _responseObj;
            }
        }
        private CPResponseClass _responseObj;
        //
        //====================================================================================================
        /// <summary>
        /// An object that includes properties and methods that descript the application
        /// </summary>
        public override CPSiteBaseClass Site {
            get {
                if (_siteObj == null) {
                    _siteObj = new CPSiteClass(this);
                }
                return _siteObj;
            }
        }
        private CPSiteClass _siteObj;
        //
        //====================================================================================================
        /// <summary>
        /// Text Messaging interface
        /// </summary>
        public override CPSMSBaseClass SMS {
            get {
                if (_smsObj == null) {
                    _smsObj = new CPSMSClass(this);
                }
                return _smsObj;
            }
        }
        private CPSMSClass _smsObj;
        //
        //====================================================================================================
        /// <summary>
        /// An object that provides basic methods helpful is application execute.
        /// </summary>
        public override CPUtilsBaseClass Utils {
            get {
                if (_utilsObj == null) {
                    _utilsObj = new CPUtilsClass(this);
                }
                return _utilsObj;
            }
        }
        private CPUtilsClass _utilsObj;
        //
        //====================================================================================================
        /// <summary>
        /// An object that represents the visit. A visit is typically used for Internet applications and represents a sequence of route hits
        /// </summary>
        public override CPVisitBaseClass Visit {
            get {
                if (_visitObj == null) {
                    _visitObj = new CPVisitClass(this);
                }
                return _visitObj;
            }
        }
        private CPVisitClass _visitObj;
        //
        //====================================================================================================
        /// <summary>
        /// An object that represents the visitor. The visitor is typically used for Internet applications and represents a sequence of visits
        /// </summary>
        public override CPVisitorBaseClass Visitor {
            get {
                if (_visitorObj == null) {
                    _visitorObj = new CPVisitorClass(this);
                }
                return _visitorObj;
            }
        }
        private CPVisitorClass _visitorObj;
        //
        //====================================================================================================
        /// <summary>
        /// The route map is a dictionary of route names and route details that tell how to execute the route
        /// </summary>
        public RouteMapModel routeMap {
            get {
                return core.routeMap;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Temporary file storarge
        /// </summary>
        public override CPFileSystemBaseClass TempFiles {
            get {
                if (_FileTemp == null) {
                    _FileTemp = new CPFileSystemClass(core.tempFiles);
                }
                return _FileTemp;
            }
        }
        private CPFileSystemClass _FileTemp;
        //
        //====================================================================================================
        /// <summary>
        /// A file object with access to the domain's primary web root files. This is typically where design files are stored, like styles sheets, js, etc.
        /// </summary>
        public override CPFileSystemBaseClass WwwFiles {
            get {
                if (_FileAppRoot == null) {
                    _FileAppRoot = new CPFileSystemClass(core.wwwFiles);
                }
                return _FileAppRoot;
            }
        }
        private CPFileSystemClass _FileAppRoot;
        //
        //====================================================================================================
        /// <summary>
        /// Access to private files for the application. Private files are not available online.
        /// </summary>
        public override CPFileSystemBaseClass PrivateFiles {
            get {
                if (_PrivateFiles == null) {
                    _PrivateFiles = new CPFileSystemClass(core.privateFiles);
                }
                return _PrivateFiles;
            }
        }
        private CPFileSystemClass _PrivateFiles;
        //
        //=========================================================================================================
        /// <summary>
        /// expose object for managing content delivery files. This is a publically accessable location that holds content contributed. If remote file mode, this is an AWS S3 bucket
        /// </summary>
        public override CPFileSystemBaseClass CdnFiles {
            get {
                if (_CdnFiles == null) {
                    _CdnFiles = new CPFileSystemClass(core.cdnFiles);
                }
                return _CdnFiles;
            }
        }
        private CPFileSystemClass _CdnFiles;

        //
        //=========================================================================================================
        //
        public override CPAdminUIBaseClass AdminUI {
            get {
                if (_AdminUI == null) {
                    _AdminUI = new CPAdminUIClass(core);
                }
                return _AdminUI;
            }
        }
        private CPAdminUIBaseClass _AdminUI;
        //
        //=========================================================================================================
        /// <summary>
        /// return the server config object which exposes connectivity information for addons that need it
        /// </summary>
        public override ServerConfigBaseModel ServerConfig => core.serverConfig;
        //
        //=========================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public override CPSecurityBaseClass Security {
            get {
                if (_Security == null) {
                    _Security = new CPSecurityClass(core);
                }
                return _Security;
            }
        }
        private CPSecurityBaseClass _Security;
        //
        //=========================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public override CPSecretsBaseClass Secrets {
            get {
                if (_Secrets == null) {
                    _Secrets = new CPSecretsClass(core);
                }
                return _Secrets;
            }
        }
        private CPSecretsBaseClass _Secrets;
        //
        //=========================================================================================================
        /// <summary>
        /// Gets a list of string with all the applications for this server
        /// </summary>
        /// <returns></returns>
        public override List<string> GetAppNameList() {
            var result = new List<string>();
            foreach (var app in core.serverConfig.apps) {
                result.Add(app.Key);
            }
            return result;
        }
        //
        //=========================================================================================================asdfasdf
        /// <summary>
        /// Get the configuration object for the specified application. Returns null if not found.
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        public override AppConfigBaseModel GetAppConfig(string appName) {
            if (!core.serverConfig.apps.ContainsKey(appName)) { return null; }
            return core.serverConfig.apps[appName];
        }
        //
        //=========================================================================================================
        /// <summary>
        /// Get the configuration object for the current application.
        /// </summary>
        /// <returns></returns>
        public override AppConfigBaseModel GetAppConfig() {
            return core.appConfig;
        }
        //
        //=========================================================================================================
        // deprecated
        //
        [Obsolete("Use cp.Doc.SetProperty.", false)]
        public void addVar(string key, string value) {
            try {
                if (!string.IsNullOrEmpty(key)) {
                    this.Doc.SetProperty(key, value);
                }
            } catch (Exception ex) {
                Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        /// <summary>
        /// MS recommended interface
        /// </summary>
        protected bool disposed_cp;
        /// <summary>
        /// MS recommended interface
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// MS recommended interface
        /// </summary>
        ~CPClass() {
            Dispose(false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing_cp"></param>
        protected virtual void Dispose(bool disposing_cp) {
            if (!this.disposed_cp) {
                this.disposed_cp = true;
                if (disposing_cp) {
                    //
                    // call .dispose for managed objects
                    //
                    if (_dbObj != null) {
                        _dbObj.Dispose();
                    }
                    if (_docObj != null) {
                        _docObj.Dispose();
                    }
                    if (_emailObj != null) {
                        _emailObj.Dispose();
                    }
                    if (_fileObj != null) {
                        _fileObj.Dispose();
                    }
                    if (_groupObj != null) {
                        _groupObj.Dispose();
                    }
                    if (_htmlObj != null) {
                        _htmlObj.Dispose();
                    }
                    if (_requestObj != null) {
                        _requestObj.Dispose();
                    }
                    if (_responseObj != null) {
                        _responseObj.Dispose();
                    }
                    if (_siteObj != null) {
                        _siteObj.Dispose();
                    }
                    if (_smsObj != null) {
                        _smsObj.Dispose();
                    }
                    if (_userObj != null) {
                        _userObj.Dispose();
                    }
                    if (_utilsObj != null) {
                        _utilsObj.Dispose();
                    }
                    if (_visitObj != null) {
                        _visitObj.Dispose();
                    }
                    if (_visitorObj != null) {
                        _visitorObj.Dispose();
                    }
                    if (_CdnFiles != null) {
                        _CdnFiles.Dispose();
                    }
                    if (_FileAppRoot != null) {
                        _FileAppRoot.Dispose();
                    }
                    if (_PrivateFiles != null) {
                        _PrivateFiles.Dispose();
                    }
                    if (core != null) {
                        core.Dispose();
                    }
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }

}