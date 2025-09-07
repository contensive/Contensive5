using Amazon.SimpleEmail;
using Contensive.BaseModels;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
//
// todo implement #nullable enable
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
        /// <summary>
        /// message to be added to log messages, created once during core construction.
        /// doc cache if session is valid
        /// </summary>
        public string logCommonMessage {
            get {
                if (_logCommonMessage != null && userId != null && visitId != null) return _logCommonMessage;
                userId = session?.user?.id;
                visitId = session?.visit?.id;
                _logCommonMessage = $"" +
                    $"app[{((appConfig?.name == null) ? "no-app" : appConfig.name)}]" +
                    $",doc[{(doc?.docGuid == null ? "unset" : doc.docGuid)}]" +
                    $",user[{(session?.user?.name == null ? "0" : userId.ToString() + ":" + session.user.name)}]" +
                    $",visit[{(visitId == null ? "0" : visitId.ToString())}]" +
                    $",thread[{Environment.CurrentManagedThreadId:000}]" +
                    $",url[{((webServer == null) ? "non-web" : string.IsNullOrEmpty(webServer.requestPathPage) ? "empty" : webServer.requestPathPage)}]";
                return _logCommonMessage;
            }
        }
        private string _logCommonMessage;
        private int? userId;
        private int? visitId;
        //
        /// <summary>
        /// message for logs, curly braces escaped to be used in structured logging
        /// </summary>
        public string logCpommonMessage_forStructuredLogging {
            get {
                if (_logCpommonMessage_forStructuredLogging != null && userId != null && visitId != null) return _logCpommonMessage_forStructuredLogging;
                _logCpommonMessage_forStructuredLogging= logCommonMessage.replace("{", "{{", StringComparison.CurrentCulture).replace("}", "}}",StringComparison.CurrentCulture);
                return _logCpommonMessage_forStructuredLogging;
            }
        }
        private string _logCpommonMessage_forStructuredLogging;
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
        //
        /// <summary>
        /// hold ses client for scope of core
        /// </summary>
        public AmazonSimpleEmailServiceClient sesClient {
            get {
                if (sesClient_local != null) { return sesClient_local; }
                sesClient_local = AwsSesController.getSesClient(this);
                return sesClient_local;
            }
        }
        private AmazonSimpleEmailServiceClient sesClient_local;
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
        public WebServerController webServer;
        //
        //===================================================================================================
        //
        /// <summary>
        /// www file system.
        /// testing pattern to block null ref return
        /// </summary>
        public FileController wwwFiles {
            get {
                if (_wwwFiles != null) { return _wwwFiles; }
                if (appConfig == null || !appConfig.enabled) {
                    if (appConfig == null) { throw new Exceptions.GenericException("Access to www filesystem is disabled because appConfig is not valid."); }
                    throw new Exceptions.GenericException("Access to www filesystem is disabled because this app is disabled.");
                }
                _wwwFiles = new FileController(this, serverConfig.isLocalFileSystem, appConfig.localWwwPath, appConfig.remoteWwwPath);
                return _wwwFiles;
            }
        }
        private FileController _wwwFiles;
        //
        //===================================================================================================
        //
        public FileController tempFiles {
            get {
                if (_tempFiles != null) { return _tempFiles; }
                if (appConfig == null || !appConfig.enabled) {
                    if (appConfig == null) { throw new Exceptions.GenericException("Access to temp filesystem is disabled because appConfig is not valid."); }
                    throw new Exceptions.GenericException("Access to temp filesystem is disabled because this app is disabled.");
                }
                _tempFiles = new FileController(this, appConfig.localTempPath);
                return _tempFiles;
            }
        }
        private FileController _tempFiles;
        //
        //===================================================================================================
        //
        public FileController privateFiles {
            get {
                if (_privateFiles != null) { return _privateFiles; }
                if (appConfig == null || !appConfig.enabled) {
                    if (appConfig == null) { throw new Exceptions.GenericException("Access to private filesystem is disabled because appConfig is not valid."); }
                    throw new Exceptions.GenericException("Access to private filesystem is disabled because this app is disabled.");
                }
                _privateFiles = new FileController(this, serverConfig.isLocalFileSystem, appConfig.localPrivatePath, appConfig.remotePrivatePath);
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
                    System.IO.Directory.CreateDirectory("d:\\Contensive");
                    _programDataFiles = new FileController(this, "d:\\Contensive\\");
                    return _programDataFiles;
                }
                //
                // -- no d-drive exists, create a new folder in c:\contensive
                System.IO.Directory.CreateDirectory("c:\\Contensive");
                _programDataFiles = new FileController(this, "c:\\Contensive\\");
                return _programDataFiles;
            }
        }
        private FileController _programDataFiles;
        //
        //===================================================================================================
        //
        public FileController programFiles {
            get {
                if (_programFiles != null) { return _programFiles; }
                string executePath = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
                if (executePath.ToLowerInvariant().IndexOf("\\git\\") > -1) {
                    //
                    //  -- developer execution, do not use saved path, use the git path but do not save
                    logger.Warn($"{this.logCommonMessage},Execution path includes GIT, use it and do not update serverConfig.ProgramFilesPath.");
                    _programFiles = new FileController(this, executePath);
                    return _programFiles;
                }
                if (!string.IsNullOrEmpty(serverConfig.programFilesPath) && System.IO.File.Exists(serverConfig.programFilesPath + "Processor.dll")) {
                    //
                    // -- use saved path if it exists
                    _programFiles = new FileController(this, serverConfig.programFilesPath);
                    return _programFiles;
                }
                //
                //  -- developer, fake a path
                logger.Warn($"{this.logCommonMessage},serverConfig.ProgramFilesPath is blank. Current executable path does NOT includes \\git\\ so assumed program files path environment set.");
                if (System.IO.File.Exists("c:\\Program Files\\Contensive\\Processor.dll")) {
                    serverConfig.programFilesPath = "c:\\Program Files\\Contensive\\";
                } else {
                    serverConfig.programFilesPath = "c:\\Program Files (x86)\\Contensive\\";
                }
                serverConfig.save(this);
                _programFiles = new FileController(this, serverConfig.programFilesPath);
                return _programFiles;
            }
        }
        private FileController _programFiles;
        //
        //===================================================================================================
        //
        public FileController cdnFiles {
            get {
                if (_cdnFiles != null) { return _cdnFiles; }
                if (appConfig == null || !appConfig.enabled) {
                    if (appConfig == null) { throw new Exceptions.GenericException("Access to cdn filesystem is disabled because appConfig is not valid."); }
                    throw new Exceptions.GenericException("Access to cdn filesystem is disabled because this app is disabled.");
                }
                _cdnFiles = new FileController(this, serverConfig.isLocalFileSystem, appConfig.localFilesPath, appConfig.remoteFilePath);
                return _cdnFiles;
            }
        }
        private FileController _cdnFiles;
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
                if (_domain == null) {
                    _domain = new DomainModel {
                        name = "*",
                        defaultRouteId = siteProperties.defaultRouteId
                    };
                }
                return _domain;
            }
            set {
                _domain = value;
            }
        }
        private DomainModel _domain;
        //
        //===================================================================================================
        /// <summary>
        /// cache controller
        /// </summary>
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
        /// cache store controller
        /// store frequently accessed records.
        /// </summary>
        public CacheRuntimeController cacheRuntime {
            get {
                if (_cacheStore == null) {
                    _cacheStore = new CacheRuntimeController(this);
                }
                return _cacheStore;
            }
        }
        private CacheRuntimeController _cacheStore;
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
                if (_dbServer == null) {
                    _dbServer = new DbServerController(this);
                }
                return _dbServer;
            }
        }
        private DbServerController _dbServer;
        //
        //===================================================================================================
        /// <summary>
        /// secrets
        /// </summary>
        public SecretsModel secrets {
            get {
                if (_secrets == null) {
                    _secrets = new SecretsModel(this);
                }
                return _secrets;
            }
        }
        private SecretsModel _secrets;
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for cluster use.
        /// </summary>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        public CoreController(CPClass cp) : this(cp, null, null, false) { }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for app, non-Internet use. coreClass is the primary object internally, created by cp.
        /// Sessions are disabled, so user, visit and visitor are empty objects. To create a session.user, call session.verifyUser()
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="appName"></param>
        /// <remarks></remarks>
        public CoreController(CPClass cp, string appName) : this(cp, appName, null, false) { }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for app, non-Internet use. coreClass is the primary object internally, created by cp.
        /// If allowSession true, sessions are valid.
        /// If allowSession false, session objects cp.user, cp.visit and cp.visitor are empty. Use to increase performance when sessions are not needed. Properties are shared with all calls. To create a session.user, call session.verifyUser()
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="appName"></param>
        /// <remarks></remarks>
        public CoreController(CPClass cp, string appName, bool allowSession) : this(cp, appName, null, allowSession) { }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for a web request/response environment. coreClass is the primary object internally, created by cp.
        /// </summary>
        public CoreController(CPClass cp, string appName, HttpContextModel httpContext) : this(cp, appName, httpContext, true) { }
        //
        //====================================================================================================
        /// <summary>
        /// coreClass constructor for a web request/response environment, if you need to block visit tracking programmatically. coreClass is the primary object internally, created by cp.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="appName"></param>
        /// <param name="httpContext"></param>
        /// <param name="allowVisit"></param>
        public CoreController(CPClass cp, string appName, HttpContextModel httpContext, bool allowVisit) {
            try {
                //
                cpParent = cp;
                cpParent.core = this;
                cacheRuntime.metaDataDictionary = [];
                cacheRuntime.tableSchemaDictionary = [];
                //
                // -- clear mock datetime
                _mockNow = null;
                //
                logger.Trace($"{cp.core.logCommonMessage},coreController_Initialize, enter");

                //
                serverConfig = ServerConfigModel.create(this);
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
                webServer = new WebServerController(this, httpContext);
                //
                // -- initialize document
                doc.allowDebugLog = true;
                doc.profileStartTime = dateTimeNowMockable;
                doc.visitPropertyAllowDebugging = false;
                doc.blockExceptionReporting = false;
                //
                // -- session
                //
                deleteSessionOnExit = (httpContext == null);
                if (appConfig == null) {
                    //
                    // -- server mode, there is no application
                    session = new(this, false);
                } else if (appConfig.appStatus != AppConfigModel.AppStatusEnum.ok) {
                    //
                    // -- application is not ready, might be error, or in maintainence mode
                    session = new(this, false);
                } else {
                    //
                    // -- initialize session
                    session = new(this, allowVisit && siteProperties.allowVisitTracking);
                    //
                    // -- authentication event if appName is valid
                    //
                    if (string.IsNullOrEmpty(appName)) { return; }
                    //
                    string UserErrorMessage = "";
                    if (!AuthEventController.processAuthenticationDefaultEvent(this, ref UserErrorMessage)) {
                        _ = EventController.throwEventByName(this, "authentication event");
                    }
                }
            } catch (Exception ex) {
                LogController.logShortLine("CoreController constructor-4, exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Fatal);
                throw;
            }
        }
        //
        //===================================================================================================
        /// <summary>
        /// clear the addon cache, the persistent routeMap, and the non-persistent RouteMap
        /// </summary>
        public void routeMapRebuild() {
            // 
            // -- clear cache and local routeMap
            cache.invalidate(RouteMapModel.cacheNameRouteMap);
            _routeMap = null;
            //
            // -- no idea why we added the addon clear
            cacheRuntime.clear();
            //
            // -- rebuild the map
            _ = routeMap;
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
        protected virtual void dispose(bool disposing) {
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
                            if (session?.visit != null && !deleteSessionOnExit && siteProperties.allowVisitTracking) {
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
                                    + " " + DbController.encodeSQLText(ViewingName.substringSafe(0, 240))
                                    + "," + session.visit.id.ToString()
                                    + "," + session.user.id.ToString()
                                    + "," + DbController.encodeSQLText(webServer.requestDomain.substringSafe(0, 240))
                                    + "," + DbController.encodeSQLText(webServer.requestPath.substringSafe(0, 240))
                                    + "," + DbController.encodeSQLText(webServer.requestPage.substringSafe(0, 240))
                                    + "," + DbController.encodeSQLText(webServer.requestQueryString.substringSafe(0, 240))
                                    + "," + DbController.encodeSQLText(requestFormSerialized.substringSafe(0, 240))
                                    + "," + DbController.encodeSQLText(webServer.requestReferrer.substringSafe(0, 240))
                                    + "," + DbController.encodeSQLDate(doc.profileStartTime)
                                    + "," + DbController.encodeSQLBoolean(session.visitStateOk)
                                    + "," + doc.appStopWatch.ElapsedMilliseconds.ToString()
                                    + ",1"
                                    + "," + PageId.ToString()
                                    + "," + DbController.encodeSQLBoolean(webServer.pageExcludeFromAnalytics)
                                    + "," + DbController.encodeSQLText(pagetitle.substringSafe(0, 240))
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
                        if (sesClient_local != null) {
                            sesClient_local.Dispose();
                        }
                        //
                        _siteProperties = null;
                        _domain = null;
                        _docProperties = null;
                        webServer = null;
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
        private static NLog.Logger logger { get; } = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// dispose pattern
        /// </summary>
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// dispose pattern
        /// </summary>
        ~CoreController() {
            // do not add code here. Use the Dispose(disposing) overload
            dispose(false);
        }
        #endregion
    }
}
