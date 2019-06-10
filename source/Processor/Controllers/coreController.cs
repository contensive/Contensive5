﻿
using System;
using System.Reflection;
using Contensive.BaseClasses;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Models.Db;
using System.Collections.Generic;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
using System.Diagnostics;
using Contensive.Processor.Exceptions;
//
namespace Contensive.Processor.Controllers {
    //
    //===================================================================================================
    /// <summary>
    /// central object, passed for dependancy injection, provides access to persistent objects (document persistence/scope)
    /// </summary>
    public class CoreController : IDisposable {
        //
        //===================================================================================================
        /// <summary>
        /// a reference to the cp api interface that parents this object. CP is the api to addons, based on the abstract classes exposed to developers.
        /// </summary>
        internal CPClass cp_forAddonExecutionOnly { get; set; }
        //
        //===================================================================================================
        /// <summary>
        /// server configuration - this is the node's configuration, including everything needed to attach to resources required (db,cache,filesystem,etc)
        /// and the configuration of all applications within this group of servers. This file is shared between all servers in the group.
        /// </summary>
        public ServerConfigModel serverConfig { get; set; }
        //
        //===================================================================================================
        /// <summary>
        /// An instance of the sessionController, populated for the current session (user state, visit state, etc)
        /// </summary>
        public SessionController session;
        //
        //===================================================================================================
        // todo - this should be a pointer into the serverConfig
        /// <summary>
        /// The configuration for this app, a copy of the data in the serverconfig file
        /// </summary>
        public AppConfigModel appConfig { get; set; }
        //
        //===================================================================================================
        // todo move persistent objects to .doc (keeping of document scope persistence)
        /// <summary>
        /// rnd resource used during this scope
        /// </summary>
        public Random random = new Random();
        //
        //===================================================================================================
        /// <summary>
        /// Set true and email send adds all email to mockEmailList
        /// </summary>
        public bool mockEmail = false;
        public List<MockEmailClass> mockEmailList = new List<MockEmailClass>();
        //
        /// <summary>
        /// when enable, use MS trace logging. An attempt to stop file append permission issues
        /// </summary>
        public bool useNlog = true;
        //
        /// <summary>
        /// Dictionary of cdef, index by name
        /// </summary>
        internal Dictionary<string, Models.Domain.ContentMetadataModel> metaDataDictionary { get; set; }
        //
        /// <summary>
        /// Dictionary of tableschema, index by name
        /// </summary>
        internal Dictionary<string, Models.Domain.TableSchemaModel> tableSchemaDictionary { get; set; }
        //
        /// <summary>
        /// lookup contentId by contentName
        /// </summary>
        internal Dictionary<string, int> contentNameIdDictionary {
            get {
                if (_contentNameIdDictionary == null) {
                    _contentNameIdDictionary = new Dictionary<string, int>();
                }
                return _contentNameIdDictionary;
            }
        }
        internal Dictionary<string, int> _contentNameIdDictionary = null;
        //
        //===================================================================================================
        /// <summary>
        /// list of DLLs in the addon assembly path that are not adds. As they are discovered, they are added to this list
        /// and not loaded in the future. The me.dispose compares the list count to the loaded count and caches if different.
        /// </summary>
        /// <returns></returns>
        public List<string> assemblyList_NonAddonsFound {
            get {
                if (_assemblyList_NonAddonsFound == null) {
                    _assemblyList_NonAddonsFound = cache.getObject<List<string>>(cacheName_AssemblyList_NonAddonsFound);
                    if (_assemblyList_NonAddonsFound == null) {
                        _assemblyList_NonAddonsFound = new List<string>();
                    }
                    _assemblyList_NonAddonsFound_CountWhenLoaded = _assemblyList_NonAddonsFound.Count;
                }
                return _assemblyList_NonAddonsFound;
            }
        }
        private List<string> _assemblyList_NonAddonsFound;
        private int _assemblyList_NonAddonsFound_CountWhenLoaded;
        //
        // -- assembly files to skip
        public List<string> assemblyList_NonAddonsInstalled = new List<string>() {
            "\\cpbase.dll",
            "\\awssdk.core.dll",
            "\\awssdk.s3.dll",
            "\\clearscript.dll",
            "\\clearscriptv8-32.dll",
            "\\clearscriptv8-64.dll",
            "\\amazon.elasticachecluster.dll",
            "\\defaultsite.dll",
            "\\enyim.caching.dll",
            "\\icsharpcode.sharpziplib.dll",
            "\\microsoft.web.administration.dll",
            "\\newtonsoft.json.dll",
            "\\nlog.dll",
            "\\nuglify.dll",
            "\\nustache.core.dll",
            "\\v8-base-ia32.dll",
            "\\v8-ia32.dll",
            "\\v8-base-x64.dll",
            "\\v8-x64.dll"
        };
        //
        //===================================================================================================
        // todo move to class
        /// <summary>
        /// A dictionary of addon collection.namespace.class and the file assembly where it was found. Built during execution, stored in cache
        /// </summary>
        public Dictionary<string, AssemblyFileDetails> assemblyList_AddonsFound {
            get {
                if (_assemblyFileDict != null) { return _assemblyFileDict; }
                _assemblyFileDict = cache.getObject<Dictionary<string, AssemblyFileDetails>>(AssemblyFileDictCacheName);
                if (_assemblyFileDict == null) {
                    _assemblyFileDict = new Dictionary<string, AssemblyFileDetails>();
                }
                return _assemblyFileDict;
            }
        }
        public void assemblyList_AddonsFound_save() {
            var dependentKeyList = new List<string>();
            dependentKeyList.Add(CacheController.createCacheKey_LastRecordModifiedDate(AddonModel.contentTableNameLowerCase));
            dependentKeyList.Add(CacheController.createCacheKey_LastRecordModifiedDate(AddonCollectionModel.contentTableNameLowerCase));
            cache.storeObject(AssemblyFileDictCacheName, _assemblyFileDict, dependentKeyList);
        }
        private Dictionary<string, AssemblyFileDetails> _assemblyFileDict;
        private const string AssemblyFileDictCacheName = "assemblyFileDict";
        //
        //===================================================================================================
        //
        public Dictionary<string, DataSourceModel> dataSourceDictionary {
            get {
                if (_dataSources == null) {
                    _dataSources = DataSourceModel.getNameDict(this);
                }
                return _dataSources;
            }
        }
        private Dictionary<string, DataSourceModel> _dataSources = null;
        //
        //===================================================================================================
        //
        public DocController doc {
            get {
                if (_doc == null) {
                    _doc = new DocController(this);
                }
                return _doc;
            }
        }
        private DocController _doc;
        //
        //===================================================================================================
        //
        public Controllers.HtmlController html {
            get {
                if (_html == null) {
                    _html = new Controllers.HtmlController(this);
                }
                return _html;
            }
        }
        private Controllers.HtmlController _html;
        //
        //===================================================================================================
        //
        public Controllers.AddonController addon {
            get {
                if (_addon == null) {
                    _addon = new Controllers.AddonController(this);
                }
                return _addon;
            }
        }
        private Controllers.AddonController _addon;
        //
        //===================================================================================================
        //
        public PropertyModelClass userProperty {
            get {
                if (_userProperty == null) {
                    _userProperty = new PropertyModelClass(this, PropertyModelClass.PropertyTypeEnum.user);
                }
                return _userProperty;
            }
        }
        private PropertyModelClass _userProperty;
        //
        //===================================================================================================
        //
        public PropertyModelClass visitorProperty {
            get {
                if (_visitorProperty == null) {
                    _visitorProperty = new PropertyModelClass(this, PropertyModelClass.PropertyTypeEnum.visitor);
                }
                return _visitorProperty;
            }
        }
        private PropertyModelClass _visitorProperty;
        //
        //===================================================================================================
        //
        public PropertyModelClass visitProperty {
            get {
                if (_visitProperty == null) {
                    _visitProperty = new PropertyModelClass(this, PropertyModelClass.PropertyTypeEnum.visit);
                }
                return _visitProperty;
            }
        }
        private PropertyModelClass _visitProperty;
        //
        //===================================================================================================
        //
        public DocPropertyController docProperties {
            get {
                if (_docProperties == null) {
                    _docProperties = new DocPropertyController(this);
                }
                return _docProperties;
            }
        }
        private DocPropertyController _docProperties = null;
        //
        //===================================================================================================
        /// <summary>
        /// siteProperties object
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public SitePropertiesController siteProperties {
            get {
                if (_siteProperties == null) {
                    _siteProperties = new SitePropertiesController(this);
                }
                return _siteProperties;
            }
        }
        private SitePropertiesController _siteProperties = null;
        //
        //===================================================================================================
        //
        public WebServerController webServer {
            get {
                if (_webServer == null) {
                    _webServer = new WebServerController(this);
                }
                return _webServer;
            }
        }
        private WebServerController _webServer;
        //
        //===================================================================================================
        //
        public FileController wwwFiles {
            get {
                if (_appRootFiles == null) {
                    if (appConfig != null) {
                        if (appConfig.enabled) {
                            _appRootFiles = new FileController(this, serverConfig.isLocalFileSystem, appConfig.localWwwPath, appConfig.remoteWwwPath);
                        }
                    }
                }
                return _appRootFiles;
            }
        }
        private FileController _appRootFiles = null;
        //
        //===================================================================================================
        //
        public FileController tempFiles {
            get {
                if (_tmpFiles == null) {
                    //
                    // local server -- everything is ephemeral
                    _tmpFiles = new FileController(this, true, appConfig.localTempPath, "");
                }
                return _tmpFiles;
            }
        }
        private FileController _tmpFiles = null;
        //
        //===================================================================================================
        //
        public FileController privateFiles {
            get {
                if (_privateFiles == null) {
                    if (appConfig != null) {
                        if (appConfig.enabled) {
                            _privateFiles = new FileController(this, serverConfig.isLocalFileSystem, appConfig.localPrivatePath, appConfig.remotePrivatePath);
                        }
                    }
                }
                return _privateFiles;
            }
        }
        private FileController _privateFiles = null;
        //
        //===================================================================================================
        /// <summary>
        /// Progam data are data used by the system for operation, unrelated to application data, like config files and logs.
        /// ProgramData folder is the folder where the config.json file is stored. If it is not found, the default is returned, d:\Contensive or c:\Contensive
        /// The prefered location is d:\Contensive so the d-drive can be the data drive and the c-drive does not need to be backed up
        /// If files dont exist, it tries
        /// </summary>
        public FileController programDataFiles {
            get {
                if (_programDataFiles != null) { return _programDataFiles; }
                //
                // -- always local -- must be because this object is used to read serverConfig, before the object is valid
                if (System.IO.File.Exists("D:\\Contensive\\config.json")) {
                    //
                    // -- prefer D:\contensive
                    _programDataFiles = new FileController(this, true, "D:\\Contensive\\", "");
                    return _programDataFiles;
                }
                var driveNameList = new List<string>();
                //
                // -- next check each drive for a \contensive folder and go with it
                foreach (var drive in System.IO.DriveInfo.GetDrives()) {
                    if (drive.IsReady && (drive.DriveType == System.IO.DriveType.Fixed)) {
                        // drive.name looks like "C:\\"
                        driveNameList.Add(drive.Name);
                        if (!System.IO.File.Exists(drive.Name + "Contensive\\config.json")) { continue; }
                        _programDataFiles = new FileController(this, true, drive.Name + "Contensive\\", "");
                        return _programDataFiles;
                    }
                }
                //
                // -- legacy, use c:\program data\contensive, deprecated because all application data should be stored on one drive for portability, and decrease backup costs
                string legacyProgramFilesFolder = FileController.normalizeDosPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) + "Contensive\\";
                if (System.IO.Directory.Exists(legacyProgramFilesFolder)) {
                    _programDataFiles = new FileController(this, true, legacyProgramFilesFolder, "");
                    return _programDataFiles;
                }
                //
                // -- not found
                if ( driveNameList.Contains("D:\\")) {
                    //
                    // -- d-drive exists, create a new folder in d:\contensive
                    _programDataFiles = new FileController(this, true, FileController.normalizeDosPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) + "Contensive\\", "");
                    return _programDataFiles;
                }
                //
                // -- no d-drive exists, create a new folder in c:\contensive
                System.IO.Directory.CreateDirectory("C:\\");
                _programDataFiles = new FileController(this, true, FileController.normalizeDosPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) + "Contensive\\", "");
                return _programDataFiles;
            }
        }
        private FileController _programDataFiles = null;
        //
        //===================================================================================================
        //
        public FileController programFiles {
            get {
                if (_programFiles == null) {
                    if (String.IsNullOrEmpty(serverConfig.programFilesPath)) {
                        //
                        // -- dev environment, setup programfiles path 
                        string executePath = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
                        //string executePath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                        if (!executePath.ToLowerInvariant().IndexOf("\\git\\").Equals(-1)) {
                            //
                            //  -- save if not in developer execution path
                            serverConfig.programFilesPath = executePath;
                            LogController.logWarn(this, "serverConfig.ProgramFilesPath is blank. Current executable path includes \\git\\ so development environment set.");
                        } else {
                            //
                            //  -- developer, fake a path
                            serverConfig.programFilesPath = "c:\\Program Files (x86)\\kma\\Contensive5\\";
                            LogController.logWarn(this, "serverConfig.ProgramFilesPath is blank. Current executable path does NOT includes \\git\\ so assumed program files path environment set.");
                        }
                        serverConfig.save(this);
                    }
                    //
                    // -- always local
                    _programFiles = new FileController(this, true, serverConfig.programFilesPath, "");
                }
                return _programFiles;
            }
        }
        private FileController _programFiles = null;
        //
        //===================================================================================================
        //
        public FileController cdnFiles {
            get {
                if (_cdnFiles == null) {
                    if (appConfig != null) {
                        if (appConfig.enabled) {
                            _cdnFiles = new FileController(this, serverConfig.isLocalFileSystem, appConfig.localFilesPath, appConfig.remoteFilePath);
                        }
                    }
                }
                return _cdnFiles;
            }
        }
        private FileController _cdnFiles = null;
        //
        //===================================================================================================
        /// <summary>
        /// provide an addon cache object lazy populated from the Domain.addonCacheModel. This object provides an
        /// interface to lookup read addon data and common lists
        /// </summary>
        public AddonCacheModel addonCache {
            get {
                if (_addonCacheNonPersistent == null) {
                    _addonCacheNonPersistent = cache.getObject<AddonCacheModel>(cacheName_addonCachePersistent);
                    if (_addonCacheNonPersistent == null) {
                        _addonCacheNonPersistent = new AddonCacheModel(this);
                        cache.storeObject(cacheName_addonCachePersistent, _addonCacheNonPersistent);
                    }
                }
                return _addonCacheNonPersistent;
            }
        }
        private AddonCacheModel _addonCacheNonPersistent = null;
        /// <summary>
        /// method to clear the core instance of routeMap. Explained in routeMap.
        /// </summary>
        public void addonCacheClearNonPersistent() {
            _addonCacheNonPersistent = null;
        }
        /// <summary>
        /// method to clear the core instance of routeMap. Explained in routeMap.
        /// </summary>
        public void addonCacheClear() {
            cache.invalidate(cacheName_addonCachePersistent);
            _addonCacheNonPersistent = null;
        }
        //
        //===================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public DomainModel domain {
            get {
                if (_domains == null) {
                    _domains = new DomainModel();
                }
                return _domains;
            }
            set {
                _domains = value;
            }
        }
        private DomainModel _domains = null;
        /// <summary>
        /// domains configured for this app. keys are lowercase
        /// </summary>
        public Dictionary<string, DomainModel> domainDictionary;
        //
        //===================================================================================================
        public Controllers.CacheController cache {
            get {
                if (_cache == null) {
                    _cache = new Controllers.CacheController(this);
                }
                return _cache;
            }
        }
        private Controllers.CacheController _cache = null;
        //
        //===================================================================================================
        /// <summary>
        /// database datasource for the default datasource
        /// </summary>
        public DbController db {
            get {
                if (_db == null) {
                    _db = new DbController(this, "default");
                }
                return _db;
            }
        }
        private DbController _db;
        //
        //===================================================================================================
        /// <summary>
        /// db access to the server to add and query catalogs
        /// </summary>
        public DbServerController dbServer {
            get {
                if (_dbEngine == null) {
                    _dbEngine = new DbServerController(this);
                }
                return _dbEngine;
            }
        }
        private DbServerController _dbEngine;
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for cluster use.
        /// </summary>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        public CoreController(CPClass cp) {
            cp_forAddonExecutionOnly = cp;
            LogController.logRaw("CoreController constructor-0, enter", LogController.LogLevel.Trace);
            //
            metaDataDictionary = new Dictionary<string, ContentMetadataModel>();
            tableSchemaDictionary = null;
            //
            // -- create default auth objects for non-user methods, or until auth is available
            session = new SessionController(this);
            //
            serverConfig = ServerConfigModel.getObject(this);
            this.serverConfig.defaultDataSourceType = DataSourceModel.DataSourceTypeEnum.sqlServerNative;
            webServer.iisContext = null;
            constructorInitialize(false);
            LogController.logRaw("CoreController constructor-0, exit", LogController.LogLevel.Trace);
        }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for app, non-Internet use. coreClass is the primary object internally, created by cp.
        /// </summary>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        public CoreController(CPClass cp, string applicationName) : base() {
            this.cp_forAddonExecutionOnly = cp;
            LogController.logRaw("CoreController constructor-1, enter", LogController.LogLevel.Trace);
            //
            metaDataDictionary = new Dictionary<string, Models.Domain.ContentMetadataModel>();
            tableSchemaDictionary = null;
            //
            // -- create default auth objects for non-user methods, or until auth is available
            session = new SessionController(this);
            //
            serverConfig = ServerConfigModel.getObject(this);
            serverConfig.defaultDataSourceType = DataSourceModel.DataSourceTypeEnum.sqlServerNative;
            appConfig = AppConfigModel.getObject(this, serverConfig, applicationName);
            if (appConfig != null) {
                webServer.iisContext = null;
                constructorInitialize(false);
            }
            LogController.logRaw("CoreController constructor-1, exit", LogController.LogLevel.Trace);
        }
        //
        //====================================================================================================
        /// <summary>
        /// The route map is a dictionary of route names plus route details that tell how to execute the route.
        /// local instance is used to speed up multiple local requests.
        /// If during a page hit the route table entries are updated, cache is cleared and the instance object is cleared. When the next reference is 
        /// made the data is refreshed. At the end of every pageload, if the routemap updates it reloads the iis route table so if a new route
        /// is added and the next hit is to that method, it will be loaded.
        /// </summary>
        public RouteMapModel routeMap {
            get {
                if (_routeMap == null) {
                    _routeMap = RouteMapModel.create(this);
                }
                return _routeMap;
            }
        }
        private RouteMapModel _routeMap = null;
        /// <summary>
        /// clear the addon cache, the persistent routeMap, and the non-persistent RouteMap
        /// </summary>
        public void routeMapCacheClear() {
            // 
            addonCacheClear();
            Models.Domain.RouteMapModel.invalidateCache(this);
            _routeMap = null;
        }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for app, non-Internet use. coreClass is the primary object internally, created by cp.
        /// </summary>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        public CoreController(CPClass cp, string applicationName, ServerConfigModel serverConfig) : base() {
            cp_forAddonExecutionOnly = cp;
            LogController.logRaw("CoreController constructor-2, enter", LogController.LogLevel.Trace);
            //
            metaDataDictionary = new Dictionary<string, Models.Domain.ContentMetadataModel>();
            tableSchemaDictionary = null;
            //
            // -- create default auth objects for non-user methods, or until auth is available
            session = new SessionController(this);
            //
            this.serverConfig = serverConfig;
            this.serverConfig.defaultDataSourceType = DataSourceModel.DataSourceTypeEnum.sqlServerNative;
            appConfig = AppConfigModel.getObject(this, serverConfig, applicationName);
            appConfig.appStatus = AppConfigModel.AppStatusEnum.ok;
            webServer.iisContext = null;
            constructorInitialize(false);
            LogController.logRaw("CoreController constructor-2, exit", LogController.LogLevel.Trace);
        }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for app, non-Internet use. coreClass is the primary object internally, created by cp.
        /// </summary>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        public CoreController(CPClass cp, string applicationName, ServerConfigModel serverConfig, System.Web.HttpContext httpContext) : base() {
            this.cp_forAddonExecutionOnly = cp;
            LogController.logRaw("CoreController constructor-3, enter", LogController.LogLevel.Trace);
            //
            // -- create default auth objects for non-user methods, or until auth is available
            session = new SessionController(this);
            //
            this.serverConfig = serverConfig;
            this.serverConfig.defaultDataSourceType = DataSourceModel.DataSourceTypeEnum.sqlServerNative;
            appConfig = AppConfigModel.getObject(this, serverConfig, applicationName);
            this.appConfig.appStatus = AppConfigModel.AppStatusEnum.ok;
            webServer.initWebContext(httpContext);
            constructorInitialize(true);
            LogController.logRaw("CoreController constructor-3, exit", LogController.LogLevel.Trace);
        }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for a web request/response environment. coreClass is the primary object internally, created by cp.
        /// </summary>
        public CoreController(CPClass cp, string applicationName, System.Web.HttpContext httpContext) : base() {
            this.cp_forAddonExecutionOnly = cp;
            LogController.logRaw("CoreController constructor-4, enter", LogController.LogLevel.Trace);
            //
            metaDataDictionary = new Dictionary<string, Models.Domain.ContentMetadataModel>();
            tableSchemaDictionary = null;
            //
            // -- create default auth objects for non-user methods, or until auth is available
            session = new SessionController(this);
            //
            serverConfig = ServerConfigModel.getObject(this);
            serverConfig.defaultDataSourceType = DataSourceModel.DataSourceTypeEnum.sqlServerNative;
            appConfig = AppConfigModel.getObject(this, serverConfig, applicationName);
            if (appConfig != null) {
                webServer.initWebContext(httpContext);
                constructorInitialize(true);
            }
            LogController.logRaw("CoreController constructor-4, exit", LogController.LogLevel.Trace);
        }
        //
        /// <summary>
        /// coreClass constructor common tasks.
        /// </summary>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        private void constructorInitialize(bool allowVisit) {
            try {
                //
                doc.docGuid = GenericController.getGUID();
                doc.allowDebugLog = true;
                doc.profileStartTime = DateTime.Now;
                doc.visitPropertyAllowDebugging = true;
                //
                // -- attempt auth load
                if (appConfig == null) {
                    //
                    // -- server mode, there is no application
                    session = SessionController.create(this, false);
                } else if (appConfig.appStatus != AppConfigModel.AppStatusEnum.ok) {
                    //} else if ((appConfig.appMode != appConfigModel.appModeEnum.normal) || (appConfig.appStatus != appConfigModel.appStatusEnum.OK)) {
                    //
                    // -- application is not ready, might be error, or in maintainence mode
                    session = SessionController.create(this, false);
                } else {
                    session = SessionController.create(this, allowVisit && siteProperties.allowVisitTracking);
                    //
                    // -- debug defaults on, so if not on, set it off and clear what was collected
                    doc.visitPropertyAllowDebugging = visitProperty.getBoolean("AllowDebugging");
                    if (!doc.visitPropertyAllowDebugging) {
                        doc.testPointMessage = "";
                    }
                }
            } catch (Exception ex) {
                throw (ex);
            }
        }
        //
        //
        //====================================================================================================
        /// <summary>
        /// version for core assembly
        /// </summary>
        /// <remarks></remarks>
        public string codeVersion() {
            Type myType = typeof(CoreController);
            Assembly myAssembly = Assembly.GetAssembly(myType);
            AssemblyName myAssemblyname = myAssembly.GetName();
            Version myVersion = myAssemblyname.Version;
            return myVersion.Major.ToString("0") + "." + myVersion.Minor.ToString("00") + "." + myVersion.Build.ToString("00000000");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Clear all data from the metaData current instance. Next request will load from cache.
        /// </summary>
        public void clearMetaData() {
            if (metaDataDictionary != null) {
                metaDataDictionary.Clear();
            }
            if (tableSchemaDictionary != null) {
                tableSchemaDictionary.Clear();
            }
            contentNameIdDictionaryClear();
        }
        //
        //====================================================================================================
        //
        internal void contentNameIdDictionaryClear() {
            _contentNameIdDictionary = null;
        }
        //

        #region  IDisposable Support 
        //
        protected bool disposed = false;
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            LogController.logRaw("CoreController dispose, enter", LogController.LogLevel.Trace);
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                    // ----- Block all output from underlying routines
                    //
                    doc.blockExceptionReporting = true;
                    doc.continueProcessing = false;
                    //
                    // -- save assemblySkipList
                    if (_assemblyList_NonAddonsFound != null) {
                        if (_assemblyList_NonAddonsFound.Count > _assemblyList_NonAddonsFound_CountWhenLoaded) {
                            LogController.logRaw("CoreController dispose, save assemblySkipList to cache, _assemblySkipList.Count [" + _assemblyList_NonAddonsFound.Count + "], _assemblySkipList_CountWhenLoaded [" + _assemblyList_NonAddonsFound_CountWhenLoaded + "]", LogController.LogLevel.Trace);
                            cache.storeObject(cacheName_AssemblyList_NonAddonsFound, _assemblyList_NonAddonsFound);
                        }
                    }
                    //
                    // content server object is valid
                    //
                    if (serverConfig != null) {
                        if (appConfig != null) {
                            if (appConfig.appStatus == AppConfigModel.AppStatusEnum.ok) {
                                //if ((appConfig.appMode == appConfigModel.appModeEnum.normal) && (appConfig.appStatus == appConfigModel.appStatusEnum.OK))
                                if (siteProperties.allowVisitTracking) {
                                    //
                                    // If visit tracking, save the viewing record
                                    //
                                    string ViewingName = ((string)(session.visit.id + "." + session.visit.pageVisits)).Left(10);
                                    int PageID = 0;
                                    if (_doc != null) {
                                        if (doc.pageController.page != null) {
                                            PageID = doc.pageController.page.id;
                                        }
                                    }
                                    //
                                    // -- convert requestFormDict to a name=value string for Db storage
                                    string requestFormSerialized = GenericController.convertNameValueDictToREquestString(webServer.requestFormDict);
                                    string pagetitle = "";
                                    if (!doc.htmlMetaContent_TitleList.Count.Equals(0)) {
                                        pagetitle = doc.htmlMetaContent_TitleList[0].content;
                                    }
                                    string SQL = "insert into ccviewings ("
                                        + "Name,VisitId,MemberID,Host,Path,Page,QueryString,Form,Referer,DateAdded,StateOK,pagetime,Active,RecordID,ExcludeFromAnalytics,pagetitle"
                                        + ")values("
                                        + " " + DbController.encodeSQLText(ViewingName)
                                        + "," + session.visit.id.ToString()
                                        + "," + session.user.id.ToString()
                                        + "," + DbController.encodeSQLText(webServer.requestDomain)
                                        + "," + DbController.encodeSQLText(webServer.requestPath)
                                        + "," + DbController.encodeSQLText(webServer.requestPage)
                                        + "," + DbController.encodeSQLText(webServer.requestQueryString.Left(255))
                                        + "," + DbController.encodeSQLText(requestFormSerialized.Left(255))
                                        + "," + DbController.encodeSQLText(webServer.requestReferrer.Left(255))
                                        + "," + DbController.encodeSQLDate(doc.profileStartTime)
                                        + "," + DbController.encodeSQLBoolean(session.visitStateOk)
                                        + "," + doc.appStopWatch.ElapsedMilliseconds.ToString()
                                        + ",1"
                                        + "," + PageID.ToString()
                                        + "," + DbController.encodeSQLBoolean(webServer.pageExcludeFromAnalytics)
                                        + "," + DbController.encodeSQLText(pagetitle);
                                    SQL += ");";
                                    db.executeNonQueryAsync(SQL);
                                }
                            }
                        }
                    }
                    //
                    // ----- dispose objects created here
                    //
                    if (_addon != null) {
                        _addon.Dispose();
                        _addon = null;
                    }
                    //
                    if (_db != null) {
                        _db.Dispose();
                        _db = null;
                    }
                    //
                    if (_cache != null) {
                        _cache.Dispose();
                        _cache = null;
                    }
                    //
                    if (_siteProperties != null) {
                        // no dispose
                        //Call _siteProperties.Dispose()
                        _siteProperties = null;
                    }
                    //
                    //if (_json != null) {
                    //    // no dispose
                    //    //Call _json.Dispose()
                    //    _json = null;
                    //}
                    //
                    //If Not (_user Is Nothing) Then
                    //    ' no dispose
                    //    'Call _user.Dispose()
                    //    _user = Nothing
                    //End If
                    //
                    if (_domains != null) {
                        // no dispose
                        //Call _domains.Dispose()
                        _domains = null;
                    }
                    //
                    if (_docProperties != null) {
                        // no dispose
                        //Call _doc.Dispose()
                        _docProperties = null;
                    }
                    //
                    if (_webServer != null) {
                        // no dispose
                        //Call _webServer.Dispose()
                        _webServer = null;
                    }
                    //
                    //if (_menuFlyout != null) {
                    //    // no dispose
                    //    //Call _menuFlyout.Dispose()
                    //    _menuFlyout = null;
                    //}
                    //
                    if (_visitProperty != null) {
                        // no dispose
                        //Call _visitProperty.Dispose()
                        _visitProperty = null;
                    }
                    //
                    if (_visitorProperty != null) {
                        // no dispose
                        //Call _visitorProperty.Dispose()
                        _visitorProperty = null;
                    }
                    //
                    if (_userProperty != null) {
                        // no dispose
                        //Call _userProperty.Dispose()
                        _userProperty = null;
                    }
                    //
                    if (_db != null) {
                        _db.Dispose();
                        _db = null;
                    }
                }
                //
                // cleanup non-managed objects
                //
            }
            LogController.logRaw("CoreController dispose, exit", LogController.LogLevel.Trace);
        }
        //
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~CoreController() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);
        }
        #endregion
    }
}
