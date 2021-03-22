
using System;
using System.Collections.Generic;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Exceptions;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Site Properties
    /// </summary>
    public class SitePropertiesController {
        //
        private readonly CoreController core;
        //
        //====================================================================================================
        /// <summary>
        /// new
        /// </summary>
        /// <param name="core"></param>
        public SitePropertiesController(CoreController core) {
            this.core = core;
        }
        //
        //====================================================================================================
        /// <summary>
        /// clear a value from the database
        /// </summary>
        /// <param name="key"></param>
        public void clearProperty(string key) {
            if (string.IsNullOrWhiteSpace(key)) return;
            //
            // -- clear local cache
            setProperty(key, string.Empty);
            //
            // -- remove from db 
            core.db.executeNonQuery("Delete from ccsetup where (name=" + DbController.encodeSQLText(key) + ")");
        }
        //
        //====================================================================================================
        /// <summary>
        /// The site property for the system bounce address
        /// </summary>
        public string  emailBounceAddress {
            get {
                if (_emailBounceAddress == null) {
                    _emailBounceAddress = getText("EmailBounceAddress");
                }
                return Convert.ToString(_emailBounceAddress);
            }
        }
        private string _emailBounceAddress = null;
        //
        //====================================================================================================
        /// <summary>
        /// if true, add a login icon to the lower right corner
        /// </summary>
        public bool allowLoginIcon {
            get {
                if (_allowLoginIcon == null) {
                    _allowLoginIcon = getBoolean("AllowLoginIcon", false);
                }
                return Convert.ToBoolean(_allowLoginIcon);
            }
        }
        private bool? _allowLoginIcon = null;
        //
        //====================================================================================================
        /// <summary>
        /// The id of the addon to run with no specific route is found
        /// </summary>
        public int defaultRouteId {
            get {
                return getInteger(sitePropertyName_DefaultRouteAddonId);
            }
            set {
                setProperty(sitePropertyName_DefaultRouteAddonId, value);
            }
        }
        //
        //====================================================================================================
        //
        private bool dbNotReady {
            get {
                return (core.appConfig.appStatus != AppConfigModel.AppStatusEnum.ok);
            }
        }
        //
        //====================================================================================================
        //
        private int integerPropertyBase(string propertyName, int defaultValue, ref int? localStore) {
            if (dbNotReady) {
                //
                // -- Db not available yet, return default
                return defaultValue;
            } else if (localStore == null) {
                //
                // -- load local store 
                localStore = getInteger(propertyName, defaultValue);
            }
            return encodeInteger(localStore);
        }
        //
        //====================================================================================================
        //
        private bool booleanPropertyBase(string propertyName, bool defaultValue, ref bool? localStore) {
            if (dbNotReady) {
                //
                // -- Db not available yet, return default
                return defaultValue;
            } else if (localStore == null) {
                //
                // -- load local store 
                localStore = getBoolean(propertyName, defaultValue);
            }
            return encodeBoolean(localStore);
        }
        //
        //====================================================================================================
        //
        private string textPropertyBase(string propertyName, string defaultValue, ref string localStore) {
            if (dbNotReady) {
                //
                // -- Db not available yet, return default
                return defaultValue;
            } else if (localStore == null) {
                //
                // -- load local store 
                localStore = getText(propertyName, defaultValue);
            }
            return localStore;
        }
        //
        //====================================================================================================
        //
        internal int landingPageID {
            get {
                return integerPropertyBase("LandingPageID", 0, ref _landingPageId);
            }
        }
        private int? _landingPageId = null;
        //
        //====================================================================================================
        //
        internal bool trackGuestsWithoutCookies {
            get {
                return booleanPropertyBase("track guests without cookies", false, ref _trackGuestsWithoutCookies);
            }
        }
        private bool? _trackGuestsWithoutCookies = null;
        //
        //====================================================================================================
        //
        internal bool allowAutoLogin {
            get {
                return booleanPropertyBase("allowautologin", false, ref _AllowAutoLogin);
            }
        }
        private bool? _AllowAutoLogin = null;
        //
        //====================================================================================================
        //
        internal bool allowAutoRecognize {
            get {
                return booleanPropertyBase("allowautorecognize", false, ref _allowAutoRecognize);
            }
        }
        private bool? _allowAutoRecognize = null;
        //
        //====================================================================================================
        //
        internal int maxVisitLoginAttempts {
            get {
                return integerPropertyBase("maxVisitLoginAttempts", 20, ref _maxVisitLoginAttempts);
            }
        }
        private int? _maxVisitLoginAttempts = null;
        //
        //====================================================================================================
        //
        public string loginIconFilename {
            get {
                return textPropertyBase("LoginIconFilename", "" + cdnPrefix + "images/ccLibLogin.GIF", ref _LoginIconFilename);
            }
        }
        private string _LoginIconFilename = null;
        //
        //====================================================================================================
        //
        public bool allowVisitTracking {
            get {
                return booleanPropertyBase("allowVisitTracking", true, ref _allowVisitTracking);
            }
        }
        private bool? _allowVisitTracking;
        //
        //====================================================================================================
        /// <summary>
        /// trap errors (hide errors) - when true, errors will be logged and code resumes next. When false, errors are re-thrown
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool trapErrors {
            get {
                return booleanPropertyBase("TrapErrors", true, ref _trapErrors);
            }
        }
        private bool? _trapErrors = null;
        //
        //====================================================================================================
        //
        public string serverPageDefault {
            get {
                return textPropertyBase(sitePropertyName_ServerPageDefault, sitePropertyDefaultValue_ServerPageDefault, ref _ServerPageDefault_local);
            }
        }
        private string _ServerPageDefault_local = null;
        //
        //====================================================================================================
        //
        internal int defaultWrapperID {
            get {
                return integerPropertyBase("DefaultWrapperID", 0, ref _defaultWrapperId);
            }
        }
        private int? _defaultWrapperId = null;
        //
        //====================================================================================================
        /// <summary>
        /// allowLinkAlias
        /// </summary>
        /// <returns></returns>
        internal bool allowLinkAlias {
            get {
                return booleanPropertyBase("allowLinkAlias", true, ref _allowLinkAlias_Local);
            }
        }
        private bool? _allowLinkAlias_Local = null;
        //
        //====================================================================================================
        //
        public string docTypeDeclaration {
            get {
                return textPropertyBase("DocTypeDeclaration", DTDDefault, ref _docTypeDeclaration);
            }
        }
        private string _docTypeDeclaration = null;
        //
        //====================================================================================================
        //
        public bool useContentWatchLink {
            get {
                return booleanPropertyBase("UseContentWatchLink", false, ref _useContentWatchLink);
            }
        }
        private bool? _useContentWatchLink = null;
        //
        //====================================================================================================
        //
        public bool allowTestPointLogging {
            get {
                return booleanPropertyBase("AllowTestPointLogging", false, ref _allowTestPointLogging);
            }
        }
        private bool? _allowTestPointLogging = null;
        //
        //====================================================================================================
        //
        public int defaultFormInputWidth {
            get {
                return integerPropertyBase("DefaultFormInputWidth", 60, ref _defaultFormInputWidth);
            }
        }
        private int? _defaultFormInputWidth = null;
        //
        //====================================================================================================
        //
        public int selectFieldWidthLimit {
            get {
                return integerPropertyBase("SelectFieldWidthLimit", 200, ref _selectFieldWidthLimit);
            }
        }
        private int? _selectFieldWidthLimit = null;
        //
        //====================================================================================================
        //
        public int selectFieldLimit {
            get {
                return integerPropertyBase("SelectFieldLimit", 1000, ref _selectFieldLimit);
            }
        }
        private int? _selectFieldLimit = null;
        //
        //====================================================================================================
        //
        public int defaultFormInputTextHeight {
            get {
                return integerPropertyBase("DefaultFormInputTextHeight", 1, ref _defaultFormInputTextHeight);
            }
        }
        private int? _defaultFormInputTextHeight = null;
        //
        //====================================================================================================
        //
        public string emailAdmin {
            get {
                return textPropertyBase("EmailAdmin", "webmaster@" + core.webServer.requestDomain, ref _emailAdmin);
            }
        }
        private string _emailAdmin = null;
        //====================================================================================================
        //
        public bool imageAllowUpdate {
            get {
                return booleanPropertyBase("ImageAllowUpdate", true, ref _imageAllowUpdate);
            }
        }
        private bool? _imageAllowUpdate = null;
        //
        //
        //
        //========================================================================
        /// <summary>
        /// Set a site property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="Value"></param>
        public void setProperty(string propertyName, string Value) {
            try {
                if (dbNotReady) {
                    //
                    // -- cannot set property
                    throw new GenericException("Cannot set site property before Db is ready.");
                } else {
                    if (!string.IsNullOrEmpty(propertyName.Trim())) {
                        if (propertyName.ToLowerInvariant().Equals("adminurl")) {
                            //
                            // -- intercept adminUrl for compatibility, always use admin route instead
                        } else {
                            //
                            // -- set value in Db
                            string SQLNow = DbController.encodeSQLDate(core.dateTimeNowMockable);
                            string SQL = "UPDATE ccSetup Set FieldValue=" + DbController.encodeSQLText(Value) + ",ModifiedDate=" + SQLNow + " WHERE name=" + DbController.encodeSQLText(propertyName);
                            int recordsAffected = 0;
                            core.db.executeNonQuery(SQL, ref recordsAffected);
                            if (recordsAffected == 0) {
                                SQL = "INSERT INTO ccSetup (ACTIVE,CONTENTCONTROLID,NAME,FIELDVALUE,ModifiedDate,DateAdded)VALUES("
                            + DbController.SQLTrue + "," + DbController.encodeSQLNumber(ContentMetadataModel.getContentId(core, "site properties")) + "," + DbController.encodeSQLText(propertyName.ToUpper()) + "," + DbController.encodeSQLText(Value) + "," + SQLNow + "," + SQLNow + ");";
                                core.db.executeNonQuery(SQL);
                            }
                            //
                            // -- set simple lazy cache
                            string cacheName = getNameValueDictKey(propertyName);
                            if (nameValueDict.ContainsKey(cacheName)) {
                                nameValueDict.Remove(cacheName);
                            }
                            nameValueDict.Add(cacheName, Value);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// convert propertyname to dictionary key
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        internal string getNameValueDictKey(string propertyName) {
            return propertyName.Trim().ToLowerInvariant();
        }
        //
        //========================================================================
        /// <summary>
        /// Set a site property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="Value"></param>
        public void setProperty(string propertyName, bool Value) {
            if (Value) {
                setProperty(propertyName, "true");
            } else {
                setProperty(propertyName, "false");
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Set a site property from an integer
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="Value"></param>
        public void setProperty(string propertyName, int Value) {
            setProperty(propertyName, Value.ToString());
        }
        //
        //========================================================================
        /// <summary>
        /// Set a site property from a date 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="Value"></param>
        public void setProperty(string propertyName, DateTime Value) {
            setProperty(propertyName, Value.ToString());
        }
        //
        //========================================================================
        /// <summary>
        /// Set a site property from a date 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="Value"></param>
        public void setProperty(string propertyName, double Value) {
            setProperty(propertyName, Value.ToString());
        }
        //
        //========================================================================
        /// <summary>
        /// get site property without a cache check, return as text. If not found, set and return default value
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <param name="memberId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string getTextFromDb(string PropertyName, string DefaultValue, ref bool return_propertyFound) {
            try {
                string returnString = SitePropertyModel.getValue(core.cpParent, PropertyName, ref return_propertyFound);
                if (return_propertyFound) { return returnString; }
                //
                // -- proprety not found
                if (string.IsNullOrEmpty(DefaultValue)) { return string.Empty; }
                //
                // do not set - set may have to save, and save needs contentId, which now loads ondemand, which checks cache, which does a getSiteProperty.
                setProperty(PropertyName, DefaultValue);
                return_propertyFound = true;
                return DefaultValue;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get site property, return as text. If not found, set and return default value
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <param name="memberId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string getText(string propertyName, string DefaultValue) {
            try {
                if (dbNotReady) {
                    //
                    // -- if db not ready, return default 
                    return DefaultValue;
                }
                string cacheName = getNameValueDictKey(propertyName);
                if (cacheName.Equals("adminurl")) {
                    //
                    // -- adminRoute became an appConfig property
                    return "/" + core.appConfig.adminRoute;
                }
                if (string.IsNullOrEmpty(cacheName)) {
                    //
                    // -- return default if bad property name 
                    return DefaultValue;
                }
                //
                // -- test simple lazy cache
                if (nameValueDict.ContainsKey(cacheName)) {
                    //
                    // -- return property in memory cache
                    return nameValueDict[cacheName];
                }
                //
                // -- read db value
                bool propertyFound = false;
                string result = getTextFromDb(propertyName, DefaultValue, ref propertyFound);
                if (propertyFound) {
                    //
                    // -- found in Db, save in lazy cache in case it is repeated
                    if (nameValueDict.ContainsKey(cacheName)) {
                        nameValueDict.Remove(cacheName);
                    }
                    nameValueDict.Add(cacheName, result);
                    return result;
                }
                //
                // -- property not found in db, cache and return default
                nameValueDict.Add(cacheName, DefaultValue);
                setProperty(cacheName, DefaultValue);
                return DefaultValue;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// get site property and return string
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public string getText(string PropertyName) {
            return getText(PropertyName, string.Empty);
        }
        //
        //========================================================================
        /// <summary>
        /// get site property and return integer
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        public int getInteger(string PropertyName, int DefaultValue = 0) {
            return encodeInteger(getText(PropertyName, DefaultValue.ToString()));
        }
        //
        //========================================================================
        /// <summary>
        /// get site property and return double
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        public double getNumber(string PropertyName, double DefaultValue = 0) {
            return encodeNumber(getText(PropertyName, DefaultValue.ToString()));
        }
        //
        //========================================================================
        /// <summary>
        /// get site property and return boolean
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        public bool getBoolean(string PropertyName, bool DefaultValue = false) {
            return encodeBoolean(getText(PropertyName, DefaultValue.ToString()));
        }
        //
        //========================================================================
        /// <summary>
        /// get a site property as a date 
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        public DateTime getDate(string PropertyName, DateTime DefaultValue = default) {
            return encodeDate(getText(PropertyName, DefaultValue.ToString()));
        }
        //
        //====================================================================================================
        /// <summary>
        /// allowCache site property, not cached (to make it available to the cache process)
        /// </summary>
        /// <returns></returns>
        public bool allowCache_notCached {
            get {
                if (dbNotReady) {
                    return false;
                } else {
                    if (_allowCache_notCached == null) {
                        bool propertyFound = false;
                        _allowCache_notCached = GenericController.encodeBoolean(getTextFromDb("AllowBake", "0", ref propertyFound));
                    }
                    return encodeBoolean(_allowCache_notCached);
                }
            }
        }
        private bool? _allowCache_notCached = null;
        //
        //====================================================================================================
        /// <summary>
        /// The code version used to update the database last
        /// </summary>
        /// <returns></returns>
        public string dataBuildVersion {
            get {
                return textPropertyBase("BuildVersion", "", ref _buildVersion);
            }
            set {
                setProperty("BuildVersion", value);
                _buildVersion = null;
            }
        }
        private string _buildVersion = null;
        //
        //====================================================================================================
        /// <summary>
        /// Allow Legacy Scramble Fallback - if true, fields marked as scramble will first be descrambled with TwoWayEncoding. 
        /// If that fails the legacy descramble will be attempted
        /// </summary>
        /// <returns></returns>
        public bool allowLegacyDescrambleFallback {
            get {
                if (_allowLegacyDescrambleFallback == null) {
                    _allowLegacyDescrambleFallback = getBoolean("Allow Legacy Descramble Fallback");
                }
                return (bool)_allowLegacyDescrambleFallback;
            }
        }
        private bool? _allowLegacyDescrambleFallback = null;
        //
        //====================================================================================================
        //
        internal Dictionary<string, string> nameValueDict {
            get {
                if (dbNotReady) {
                    throw new GenericException("Cannot access site property collection if database is not ready.");
                } else {
                    if (_nameValueDict == null) {
                        _nameValueDict = SitePropertyModel.getNameValueDict(core.cpParent);
                    }
                }
                return _nameValueDict;
            }
        }
        private Dictionary<string, string> _nameValueDict = null;
        //
        //====================================================================================================
        /// <summary>
        /// While rendering page content, the legacy content from the page content record needs to be {%%} rendered, but newer addonList rendering should not be
        /// because it can contain user submitted data on forms, etc.
        /// This change moves the execution down to a lower level method, and conditions it on the type of content.
        /// </summary>
        public bool beta200327_BlockCCmdPostPageRender {
            get {
                if (_beta200327_BlockCCmdPostPageRender == null) {
                    _beta200327_BlockCCmdPostPageRender = getBoolean("Beta200327 block content cmd post page render", true);
                }
                return encodeBoolean(_beta200327_BlockCCmdPostPageRender);
            }
        }
        private bool? _beta200327_BlockCCmdPostPageRender = null;
        //
        //====================================================================================================
        /// <summary>
        /// After the execution of an addon, if the resulting content inludes {%%}, it could have come from
        /// user submitted data (contact us form). remove {%%} during addon execution post processing.
        /// *** turned off default because admin editors are addons and editing a copy block changes the content {% to {_%
        /// </summary>
        public bool beta200327_BlockCCmdCodeAfterAddonExec {
            get {
                if (_beta200327_BlockCCmdCodeAfterAddonExec == null) {
                    _beta200327_BlockCCmdCodeAfterAddonExec = getBoolean("Beta200327 block content cmd after addon execute", false);
                }
                return encodeBoolean(_beta200327_BlockCCmdCodeAfterAddonExec);
            }
        }
        private bool? _beta200327_BlockCCmdCodeAfterAddonExec = null;

        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public bool beta200327_BlockCCmdForJSONRemoteMethods {
            get {
                if (_beta200327_BlockCCmdForJSONRemoteMethods == null) {
                    _beta200327_BlockCCmdForJSONRemoteMethods = getBoolean("Beta200327 block content cmd For JSON Remote Methods", true);
                }
                return encodeBoolean(_beta200327_BlockCCmdForJSONRemoteMethods);
            }
        }
        private bool? _beta200327_BlockCCmdForJSONRemoteMethods = null;
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}