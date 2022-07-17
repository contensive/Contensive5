
using Contensive.BaseClasses;
using Contensive.BaseModels;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using static Contensive.Processor.Constants;
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
        internal CPClass cpParent { get; set; }
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
        private RouteMapModel _routeMap;
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
        /// Common property holding all AWS credentials. These data include hooks for legacy credential strategies.
        /// </summary>
        public AwsCredentialsModel awsCredentials {
            get {
                if (_awsCredentials == null) {
                    _awsCredentials = new AwsCredentialsModel(this);
                }
                return _awsCredentials;
            }
        }
        private AwsCredentialsModel _awsCredentials = null;
        //
        //===================================================================================================
        /// <summary>
        /// An instance of the sessionController, populated for the current session (user state, visit state, etc)
        /// </summary>
        public SessionController session { get; set; }
        //
        //===================================================================================================
        /// <summary>
        /// if true, the session model (user, viewing, visit, visitor, properties) will be deleted on dispose)
        /// </summary>
        public bool deleteSessionOnExit { get; set; }
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
        public Random random { get; set; } = new Random();
        //
        //===================================================================================================
        /// <summary>
        /// set the datetime to be used for dateTimeNowMockable. set to null to use the actual datetime.now.
        /// </summary>
        public void mockDateTimeNow(DateTime? mockNow) {
            _mockNow = mockNow;
        }
        private DateTime? _mockNow;
        //
        //===================================================================================================
        /// <summary>
        /// Use as the current DAteTime (now) sitewide. If true, this method returns that value.
        /// </summary>
        public DateTime dateTimeNowMockable {
            get {
                if (_mockNow != null) {
                    return (DateTime)_mockNow;
                }
                GetSystemTimePreciseAsFileTime(out long fileTime);
                return DateTimeOffset.FromFileTime(fileTime).DateTime;
                //return DateTime.Now;
            }
        }
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        static extern void GetSystemTimePreciseAsFileTime(out long filetime);
        //
        //===================================================================================================
        /// <summary>
        /// todo - remove this simple wrapper
        /// </summary>
        public string sqlDateTimeMockable {
            get {
                return DbController.encodeSQLDate(dateTimeNowMockable);
            }
        }
        //
        //===================================================================================================
        /// <summary>
        /// Set true and email send adds all email to mockEmailList
        /// </summary>
        public bool mockEmail { get; set; }
        //
        //===================================================================================================
        /// <summary>
        /// In email mock mode, this list includes all emails sent
        /// </summary>
        public List<MockEmailClass> mockEmailList { get; set; } = new List<MockEmailClass>();
        //
        //===================================================================================================
        /// <summary>
        /// Set true and email send adds all text messages to mockTextMessageList
        /// </summary>
        public bool mockTextMessages { get; set; }
        //
        //===================================================================================================
        /// <summary>
        /// In text message mock mode, this list includes all text messages sent
        /// </summary>
        public List<MockTextMessageClass> mockTextMessageList { get; set; } = new List<MockTextMessageClass>();
        //
        //===================================================================================================
        /// <summary>
        /// Dictionary of cdef, index by name
        /// </summary>
        internal Dictionary<string, Models.Domain.ContentMetadataModel> metaDataDictionary { get; set; }
        //
        //===================================================================================================
        /// <summary>
        /// Dictionary of tableschema, index by name
        /// </summary>
        internal Dictionary<string, Models.Domain.TableSchemaModel> tableSchemaDictionary { get; set; }
        //
        //===================================================================================================
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
        internal Dictionary<string, int> _contentNameIdDictionary;
        //
        //===================================================================================================
        /// <summary>
        /// assembly files to skip
        /// </summary>
        internal List<string> assemblyList_NonAddonsInstalled { get; set; } = new List<string> {
            "\\aws.logger.core.dll",
            "\\awssdk.cloudwatchlogs.dll",
            "\\awssdk.core.dll",
            "\\awssdk.s3.dll",
            "\\awssdk.simpleemail.dll",
            "\\awssdk.simplenotificationservice.dll",
            "\\awssdk.sqs.dll",
            "\\clearscript.dll",
            "\\clearscriptv8.dll",
            "\\clearscript.windows.dll",
            "\\clearscriptv8.win-x64.dll",
            "\\clearscriptv8.win-x86.dll",
            "\\contensivedbmodels.dll",
            "\\cpbase.dll",
            "\\icsharpcode.sharpziplib.dll",
            "\\microsoft.bcl.asyncinterfaces.dll",
            "\\microsoft.web.administration.dll",
            "\\microsoft.win32.administration.dll",
            "\\newtonsoft.json.dll",
            "\\nlog.aws.logger.dll",
            "\\nlog.dll",
            "\\nuglify.dll",
            "\\nustache.core.dll",
            "\\pipelines.sockts.unofficial.dll",
            "\\sixlabors.imagesharp.dll",
            "\\stackexchange.redis.dll",
            "\\stubble.core.dll",
            "\\system.buffers.dll",
            "\\system.collection.immutable.dll",
            "\\system.diagnostics.diagnosticsource.dll",
            "\\system.diagnostics.eventlog.dll",
            "\\system.diagnostics.performancecounter.dll",
            "\\system.io.pipelines.dll",
            "\\system.memory.dll",
            "\\system.numerics.vectors.dll",
            "\\system.reflection.typeextensions.dll",
            "\\system.runtime.compilerservices.unsafe.dll",
            "\\system.security.principal.windows.dll",
            "\\system.serviceprocess.servicecontroller.dll",
            "\\system.threading.channels.dll",
            "\\system.threading.tasks.extensions.dll",
            "\\system.data.entity.design.dll",
            "\\system.data.entity.dll"
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
                //
                // -- if remote-mode collections.xml file is updated, invalidate cache
                if (!privateFiles.localFileStale(AddonController.getPrivateFilesAddonPath() + "Collections.xml")) {
                    _assemblyFileDict = cache.getObject<Dictionary<string, AssemblyFileDetails>>(AssemblyFileDictCacheName);
                }
                if (_assemblyFileDict == null) {
                    _assemblyFileDict = new Dictionary<string, AssemblyFileDetails>();
                }
                return _assemblyFileDict;
            }
        }
        //
        //===================================================================================================
        /// <summary>
        /// list of assemblies found to be addons. used to speed up execution
        /// </summary>
        public void assemblyList_AddonsFound_save() {
            var dependentKeyList = new List<CacheKeyHashClass> {
                cpParent.core.cache.createTableDependencyKeyHash(AddonModel.tableMetadata.tableNameLower),
                cpParent.core.cache.createTableDependencyKeyHash(AddonCollectionModel.tableMetadata.tableNameLower)
            };
            cache.storeObject(AssemblyFileDictCacheName, _assemblyFileDict, dependentKeyList);
        }
        private Dictionary<string, AssemblyFileDetails> _assemblyFileDict;
        private const string AssemblyFileDictCacheName = "assemblyFileDict";
        //
        //===================================================================================================
        /// <summary>
        /// List of datasources. The default datasourse is the first entry, and is populated from the initialization configuration. Additional datasources come from the datasources content in the primary datasourse.
        /// </summary>
        public Dictionary<string, DataSourceModel> dataSourceDictionary {
            get {
                if (_dataSources == null) {
                    _dataSources = DataSourceModel.getNameDict(this.cpParent);
                }
                return _dataSources;
            }
        }
        private Dictionary<string, DataSourceModel> _dataSources;
        //
        //===================================================================================================
        /// <summary>
        /// cp.doc - the primary document being constructed
        /// </summary>
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
        /// <summary>
        /// 
        /// </summary>
        public HtmlController html {
            get {
                if (_html == null) {
                    _html = new HtmlController(this);
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
        public DocPropertiesModel docProperties {
            get {
                if (_docProperties == null) {
                    _docProperties = new DocPropertiesModel(this);
                }
                return _docProperties;
            }
        }
        private DocPropertiesModel _docProperties;
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
        private SitePropertiesController _siteProperties;
        //
        //===================================================================================================
        /// <summary>
        /// persistent object with methods to control the web server features
        /// </summary>
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
        private FileController _appRootFiles;
        //
        //===================================================================================================
        //
        public FileController tempFiles {
            get {
                if (_tmpFiles == null) {
                    //
                    // local server -- everything is ephemeral
                    _tmpFiles = new FileController(this, appConfig.localTempPath);
                }
                return _tmpFiles;
            }
        }
        private FileController _tmpFiles;
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
        private FileController _privateFiles;
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
                    _programDataFiles = new FileController(this, "D:\\Contensive\\");
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
                        _programDataFiles = new FileController(this, drive.Name + "Contensive\\");
                        return _programDataFiles;
                    }
                }
                //
                // -- legacy, use c:\program data\contensive, deprecated because all application data should be stored on one drive for portability, and decrease backup costs
                string legacyProgramFilesFolder = FileController.normalizeDosPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) + "Contensive\\";
                if (System.IO.Directory.Exists(legacyProgramFilesFolder)) {
                    _programDataFiles = new FileController(this, legacyProgramFilesFolder);
                    return _programDataFiles;
                }
                //
                // -- not found
                if (driveNameList.Contains("D:\\")) {
                    //
                    // -- d-drive exists, create a new folder in d:\contensive
                    _programDataFiles = new FileController(this, FileController.normalizeDosPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) + "Contensive\\");
                    return _programDataFiles;
                }
                //
                // -- no d-drive exists, create a new folder in c:\contensive
                System.IO.Directory.CreateDirectory("C:\\");
                _programDataFiles = new FileController(this, FileController.normalizeDosPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) + "Contensive\\");
                return _programDataFiles;
            }
        }
        private FileController _programDataFiles;
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
                        if (!executePath.ToLowerInvariant().IndexOf("\\git\\").Equals(-1)) {
                            //
                            //  -- save if not in developer execution path
                            serverConfig.programFilesPath = executePath;
                            LogController.logWarn(this, "serverConfig.ProgramFilesPath is blank. Current executable path includes \\git\\ so development environment set.");
                        } else {
                            //
                            //  -- developer, fake a path
                            serverConfig.programFilesPath = "c:\\Program Files (x86)\\Contensive\\";
                            LogController.logWarn(this, "serverConfig.ProgramFilesPath is blank. Current executable path does NOT includes \\git\\ so assumed program files path environment set.");
                        }
                        serverConfig.save(this);
                    }
                    //
                    // -- always local
                    _programFiles = new FileController(this, serverConfig.programFilesPath);
                }
                return _programFiles;
            }
        }
        private FileController _programFiles;
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
        private FileController _cdnFiles;
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
                    if (_addonCacheNonPersistent == null  || _addonCacheNonPersistent.isEmpty) {
                        _addonCacheNonPersistent = new AddonCacheModel(this);
                        cache.storeObject(cacheName_addonCachePersistent, _addonCacheNonPersistent);
                    }
                }
                return _addonCacheNonPersistent;
            }
        }
        private AddonCacheModel _addonCacheNonPersistent;
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
        /// The current domain. If not set, an empty domain model is returned
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
        private DomainModel _domains;
        /// <summary>
        /// domains configured for this app. keys are lowercase
        /// </summary>
        public Dictionary<string, DomainModel> domainDictionary { get; set; }
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
        private Controllers.CacheController _cache;
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
            coreController_Initialize(cp, null, null, false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for app, non-Internet use. coreClass is the primary object internally, created by cp.
        /// In this mode, user, visit and visitor or empty objects.
        /// To create a session.user, call session.verifyUser()
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="appName"></param>
        /// <remarks></remarks>
        public CoreController(CPClass cp, string appName) {
            coreController_Initialize(cp, appName, null, false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for a web request/response environment. coreClass is the primary object internally, created by cp.
        /// </summary>
        public CoreController(CPClass cp, string appName, HttpContextModel httpContext) {
            try {
                coreController_Initialize(cp, appName, httpContext, true);
            } catch (Exception ex) {
                LogController.logShortLine("CoreController constructor-4, exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Fatal);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor common tasks.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="allowVisit"></param>
        /// <param name="httpContext"></param>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        private void coreController_Initialize(CPClass cp, string appName, HttpContextModel httpContext, bool allowVisit) {
            try {

                this.cpParent = cp;
                this.cpParent.core = this;
                //
                // -- clear mock datetime
                _mockNow = null;
                //
                LogController.log(this, "coreController_Initialize, enter", BaseClasses.CPLogBaseClass.LogLevel.Trace);
                //
                metaDataDictionary = new Dictionary<string, ContentMetadataModel>();
                tableSchemaDictionary = null;
                //
                serverConfig = ServerConfigModel.getObject(this);
                serverConfig.defaultDataSourceType = ServerConfigBaseModel.DataSourceTypeEnum.sqlServer;
                doc.continueProcessing = true;
                appConfig = null;
                if (!string.IsNullOrEmpty(appName)) {
                    //
                    // -- initialize application
                    appConfig = AppConfigModel.getObject(this, serverConfig, appName);
                    if (!appConfig.enabled) {
                        //
                        // -- app is provided, but not enabled, exit
                        return;
                    }
                }
                if (httpContext != null) {
                    //
                    // -- initialize http context
                    webServer.httpContext = httpContext;
                    webServer.initHttpContext();
                }
                //
                // -- initialize document
                doc.docGuid = GenericController.getGUID();
                doc.allowDebugLog = true;
                doc.profileStartTime = dateTimeNowMockable;
                doc.visitPropertyAllowDebugging = true;
                //
                // -- allow exception reporing
                doc.blockExceptionReporting = false;
                //
                // -- session
                deleteSessionOnExit = (httpContext == null);
                if (appConfig == null) {
                    //
                    // -- server mode, there is no application
                    session = SessionController.create(this, false);
                } else if (appConfig.appStatus != AppConfigModel.AppStatusEnum.ok) {
                    //
                    // -- application is not ready, might be error, or in maintainence mode
                    session = SessionController.create(this, false);
                } else {
                    //
                    // -- initialize session
                    session = SessionController.create(this, allowVisit && siteProperties.allowVisitTracking);
                    //
                    // -- debug defaults on, so if not on, set it off and clear what was collected
                    doc.visitPropertyAllowDebugging = visitProperty.getBoolean("AllowDebugging");
                    if (!doc.visitPropertyAllowDebugging) {
                        doc.testPointMessage = "";
                    }
                }
            } catch (Exception ex) {
                LogController.logShortLine("CoreController coreController_Initialize, exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Fatal);
                throw;
            }
        }
        //
        //===================================================================================================
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
        /// version for core assembly
        /// </summary>
        /// <remarks></remarks>
        public static string codeVersion() {
            Type myType = typeof(CoreController);
            Assembly myAssembly = Assembly.GetAssembly(myType);
            AssemblyName myAssemblyname = myAssembly.GetName();
            Version myVersion = myAssemblyname.Version;
            return myVersion.Major.ToString("0") + "." + myVersion.Minor.ToString("0") + "." + myVersion.Build.ToString("0") + "." + myVersion.Revision.ToString("0");
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
        //====================================================================================================
        /// <summary>
        /// dotnet pluralize and singularize.
        /// </summary>
        public PluralizationService pluralizationService {
            get {
                if (_pluralizationService == null) {
                    _pluralizationService = PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
                }
                return _pluralizationService;
            }
        }
        private PluralizationService _pluralizationService;

        #region  IDisposable Support 
        //
        //====================================================================================================
        /// <summary>
        /// dispose pattern
        /// </summary>
        protected bool disposed;
        //
        //====================================================================================================
        /// <summary>
        /// dispose pattern
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            string hint = "10";
            try {
                if (!this.disposed) {
                    hint = "20";
                    this.disposed = true;
                    if (disposing) {
                        hint = "30";
                        //
                        // -- Block all output from underlying routines
                        doc.blockExceptionReporting = true;
                        doc.continueProcessing = false;
                        //
                        // -- content server object is valid
                        if ((session?.visit != null) && (serverConfig != null) && (appConfig != null) && (appConfig.appStatus == AppConfigBaseModel.AppStatusEnum.ok)) {
                            hint = "40";
                            if (deleteSessionOnExit) {
                                if (session != null) {
                                    if ((session.visit != null) && (session.visit.id > 0)) {
                                        //
                                        // -- delete visit
                                        visitProperty.deleteAll(session.user.id);
                                        DbBaseModel.delete<VisitModel>(cpParent, session.visit.id);
                                        //
                                        // -- delete viewing
                                        DbBaseModel.deleteRows<ViewingModel>(cpParent, "(visitId=" + session.visit.id + ")");
                                    }
                                    if ((session.visitor != null) && (session.visitor.id > 0)) {
                                        //
                                        // -- delete visitor
                                        visitorProperty.deleteAll(session.user.id);
                                        DbBaseModel.delete<VisitorModel>(cpParent, session.visit.id);
                                    }
                                }
                            }
                            hint = "50";
                            if (session?.visit !=null && !deleteSessionOnExit && siteProperties.allowVisitTracking) {
                                hint = "51";
                                //
                                // If visit tracking, save the viewing record
                                //
                                string ViewingName = ((string)(session.visit.id + "." + session.visit.pageVisits)).left(10);
                                int PageId = 0;
                                if (_doc != null) {
                                    if (doc?.pageController?.page != null) {
                                        PageId = doc.pageController.page.id;
                                    }
                                }
                                hint = "52";
                                //
                                // -- convert requestForm to a name=value string for Db storage
                                string requestFormSerialized = GenericController.convertNameValueDictToREquestString(webServer.requestForm);
                                string pagetitle = "";
                                if (!doc.htmlMetaContent_TitleList.Count.Equals(0)) {
                                    pagetitle = doc.htmlMetaContent_TitleList[0].content;
                                }
                                hint = "53";
                                string sql = "insert into ccviewings ("
                                    + "Name,VisitId,MemberID,Host,Path,Page,QueryString,Form,Referer,DateAdded,StateOK,pagetime,Active,RecordID,ExcludeFromAnalytics,pagetitle,ccguid"
                                    + ")values("
                                    + " " + DbController.encodeSQLText(ViewingName)
                                    + "," + session.visit.id.ToString()
                                    + "," + session.user.id.ToString()
                                    + "," + DbController.encodeSQLText(webServer.requestDomain)
                                    + "," + DbController.encodeSQLText(webServer.requestPath)
                                    + "," + DbController.encodeSQLText(webServer.requestPage)
                                    + "," + DbController.encodeSQLText(webServer.requestQueryString.left(255))
                                    + "," + DbController.encodeSQLText(requestFormSerialized.left(255))
                                    + "," + DbController.encodeSQLText(webServer.requestReferrer.left(255))
                                    + "," + DbController.encodeSQLDate(doc.profileStartTime)
                                    + "," + DbController.encodeSQLBoolean(session.visitStateOk)
                                    + "," + doc.appStopWatch.ElapsedMilliseconds.ToString()
                                    + ",1"
                                    + "," + PageId.ToString()
                                    + "," + DbController.encodeSQLBoolean(webServer.pageExcludeFromAnalytics)
                                    + "," + DbController.encodeSQLText(pagetitle)
                                    + "," + DbController.encodeSQLText(doc.docGuid);
                                sql += ");";
                                hint = "54";
                                db.executeNonQuery(sql);
                            }
                        }
                        hint = "60";
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
                        if (_db != null) {
                            _db.Dispose();
                            _db = null;
                        }
                        //
                        _siteProperties = null;
                        _domains = null;
                        _docProperties = null;
                        _webServer = null;
                        _visitProperty = null;
                        _visitorProperty = null;
                        _userProperty = null;
                    }
                    //
                    // cleanup non-managed objects
                    //
                }
            } catch (Exception ex) {
                logger.Error(ex, "CoreController.Dispose. hint [" + hint + "]");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// dispose pattern
        /// </summary>
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// dispose pattern
        /// </summary>
        ~CoreController() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);
        }
        #endregion
    }
}
