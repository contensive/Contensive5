
using System;
using System.Collections.Generic;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Exceptions;
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
        /// sampleBoolProperty
        /// </summary>
        public bool sampleBoolProperty {
            get {
                if (_sampleBoolProperty != null) { return (bool)_sampleBoolProperty; }
                _sampleBoolProperty = getBoolean(spSampleBoolProperty, true);
                return (bool)_sampleBoolProperty;
            }
            set {
                _sampleBoolProperty = value;
                setProperty(spSampleBoolProperty, value);
            }
        }
        private readonly string spSampleBoolProperty = "sampleBoolProperty";
        private bool? _sampleBoolProperty = null;
        //
        //====================================================================================================
        /// <summary>
        /// sampleIntProperty
        /// </summary>
        public int sampleIntProperty {
            get {
                if (_sampleIntProperty != null) { return (int)_sampleIntProperty; }
                _sampleIntProperty = getInteger(spSampleIntProperty, 99);
                return (int)_sampleIntProperty;
            }
            set {
                _sampleIntProperty = value;
                setProperty(spSampleIntProperty, value);
            }
        }
        private readonly string spSampleIntProperty = "sampleIntProperty";
        private int? _sampleIntProperty = null;
        //
        //====================================================================================================
        /// <summary>
        /// sampleTextProperty
        /// </summary>
        public string sampleTextProperty {
            get {
                if (_sampleTextProperty != null) { return _sampleTextProperty; }
                _sampleTextProperty = getText(spSampleTextProperty, "default");
                return (string)_sampleTextProperty;
            }
            set {
                _sampleTextProperty = value;
                setProperty(spSampleTextProperty, value);
            }
        }
        private readonly string spSampleTextProperty = "sampleTextProperty";
        private string _sampleTextProperty;
        //
        //====================================================================================================
        /// <summary>
        /// passwordAgeLockupDays - how many days old a password can be without being updated
        /// </summary>
        public int passwordAgeLockoutDays {
            get {
                if (_passwordAgeLockoutDays != null) { return (int)_passwordAgeLockoutDays; }
                _passwordAgeLockoutDays = getInteger(spPasswordAgeLockoutDays, 90);
                return (int)_passwordAgeLockoutDays;
            }
            set {
                _passwordAgeLockoutDays = value;
                setProperty(spPasswordAgeLockoutDays, value);
            }
        }
        private readonly string spPasswordAgeLockoutDays = "password age lockout days";
        private int? _passwordAgeLockoutDays = null;
        //
        //====================================================================================================
        /// <summary>
        /// passwordBlockUsedPasswordPeriod
        /// </summary>
        public int passwordBlockUsedPasswordPeriod {
            get {
                if (_passwordBlockUsedPasswordPeriod != null) { return (int)_passwordBlockUsedPasswordPeriod; }
                _passwordBlockUsedPasswordPeriod = getInteger(spPasswordBlockUsedPasswordPeriod, 30);
                return (int)_passwordBlockUsedPasswordPeriod;
            }
            set {
                _passwordBlockUsedPasswordPeriod = value;
                setProperty(spPasswordBlockUsedPasswordPeriod, value);
            }
        }
        private readonly string spPasswordBlockUsedPasswordPeriod = "password block used password period";
        private int? _passwordBlockUsedPasswordPeriod = null;
        //
        //====================================================================================================
        /// <summary>
        /// passwordRequiresSpecialCharacter
        /// </summary>
        public bool passwordRequiresSpecialCharacter {
            get {
                if (_passwordRequiresSpecialCharacter != null) { return (bool)_passwordRequiresSpecialCharacter; }
                _passwordRequiresSpecialCharacter = getBoolean(spPasswordRequiresSpecialCharacter, true);
                return (bool)_passwordRequiresSpecialCharacter;
            }
            set {
                _passwordRequiresSpecialCharacter = value;
                setProperty(spPasswordRequiresSpecialCharacter, value);
            }
        }
        private readonly string spPasswordRequiresSpecialCharacter = "password requires special character";
        private bool? _passwordRequiresSpecialCharacter = null;
        //
        //====================================================================================================
        /// <summary>
        /// passwordRequiresNumber
        /// </summary>
        public bool passwordRequiresNumber {
            get {
                if (_passwordRequiresNumber != null) { return (bool)_passwordRequiresNumber; }
                _passwordRequiresNumber = getBoolean(spPasswordRequiresNumber, true);
                return (bool)_passwordRequiresNumber;
            }
            set {
                _passwordRequiresNumber = value;
                setProperty(spPasswordRequiresNumber, value);
            }
        }
        private readonly string spPasswordRequiresNumber = "password requires number";
        private bool? _passwordRequiresNumber = null;
        //
        //====================================================================================================
        /// <summary>
        /// passwordRequiresUppercase
        /// </summary>
        public bool passwordRequiresUppercase {
            get {
                if (_passwordRequiresUppercase != null) { return (bool)_passwordRequiresUppercase; }
                _passwordRequiresUppercase = getBoolean(spPasswordRequiresUppercase, true);
                return (bool)_passwordRequiresUppercase;
            }
            set {
                _passwordRequiresUppercase = value;
                setProperty(spPasswordRequiresUppercase, value);
            }
        }
        private readonly string spPasswordRequiresUppercase = "password requires uppercase";
        private bool? _passwordRequiresUppercase = null;
        //
        //====================================================================================================
        /// <summary>
        /// passwordRequiresLowercase
        /// </summary>
        public bool passwordRequiresLowercase {
            get {
                if (_passwordRequiresLowercase != null) { return (bool)_passwordRequiresLowercase; }
                _passwordRequiresLowercase = getBoolean(spPasswordRequiresLowercase, true);
                return (bool)_passwordRequiresLowercase;
            }
            set {
                _passwordRequiresLowercase = value;
                setProperty(spPasswordRequiresLowercase, value);
            }
        }
        private readonly string spPasswordRequiresLowercase = "password requires lowercase";
        private bool? _passwordRequiresLowercase = null;
        //
        //====================================================================================================
        /// <summary>
        /// passwordMinLength
        /// </summary>
        public int passwordMinLength {
            get {
                if (_passwordMinLength != null) { return (int)_passwordMinLength; }
                _passwordMinLength = getInteger("password min length", 10);
                return (int)_passwordMinLength;
            }
            set {
                _passwordMinLength = value;
                setProperty("password min length", value);
            }
        }
        private int? _passwordMinLength = null;
        //
        //====================================================================================================
        //
        /// <summary>
        /// if true, the password is cleared when a password saved in a people record in admin and automatically converted to a passwordHash
        /// </summary>
        public bool clearAdminPasswordOnHash {
            get {
                if (_clearAdminPasswordOnHash != null) { return (bool)_clearAdminPasswordOnHash; }
                _clearAdminPasswordOnHash = getBoolean("clear admin password on hash", true);
                return (bool)_clearAdminPasswordOnHash;
            }
            set {
                _clearAdminPasswordOnHash = value;
                setProperty("clear admin password on hash", value);
            }
        }
        private bool? _clearAdminPasswordOnHash = null;
        //
        //====================================================================================================
        //
        /// <summary>
        /// temp property, test if this will work. if true, include a page settings button
        /// </summary>
        public bool allowPageSettingsEdit {
            get {
                if (_allowPageSettingsEdit != null) { return (bool)_allowPageSettingsEdit; }
                _allowPageSettingsEdit = getBoolean("allow page settings edit", false);
                return (bool)_allowPageSettingsEdit;
            }
        }
        private bool? _allowPageSettingsEdit;
        //
        /// <summary>
        /// if site property anonymousUserResponseID is set to 3 (redirect), this is the redirect destination
        /// </summary>
        public string anonymousUserResponseCopy {
            get {
                if (anonymousUserResponseCopy_local != null) { return anonymousUserResponseCopy_local; }
                anonymousUserResponseCopy_local = core.cpParent.Content.GetCopy(anonymousUserResponseCopy_copyName, defaultvalue);
                return anonymousUserResponseCopy_local;
            }
            set {
                core.cpParent.Content.SetCopy(anonymousUserResponseCopy_copyName, value);
            }
        }
        private const string defaultvalue = "<p style=\"text-align:center;margin-top:100px;\">This site is not available for anonymous access.</p>";
        private const string anonymousUserResponseCopy_copyName = "anonymousUserResponseCopy";
        private string anonymousUserResponseCopy_local;
        //
        /// <summary>
        /// if site property anonymousUserResponseID is set to 3 (redirect), this is the redirect destination
        /// </summary>
        public int loginPageId {
            get {
                if (loginPageId_local != null) { return (int)loginPageId_local; }
                loginPageId_local = getInteger(loginPageId_spName, 0);
                return (int)loginPageId_local;
            }
            set {
                loginPageId_local = value;
                setProperty(loginPageId_spName, (int)loginPageId_local);
            }
        }
        private const string loginPageId_spName = "loginpageid";
        private int? loginPageId_local;
        //
        /// <summary>
        /// Login Page AddonID - when not set, the default login is used for /admin and all authentication blocks
        /// when set, this addon is called to process authetication. On success, authetnicate and return EMPTY STRING. otherwise return a login form
        /// </summary>
        public int loginPageAddonId {
            get {
                if (loginPageAddonId_local != null) { return (int)loginPageAddonId_local; }
                loginPageAddonId_local = getInteger(loginPageAddonId_spName, 0);
                return (int)loginPageAddonId_local;
            }
            set {
                loginPageAddonId_local = value;
                setProperty(loginPageAddonId_spName, (int)loginPageAddonId_local);
            }
        }
        private const string loginPageAddonId_spName = "Login Page AddonID";
        private int? loginPageAddonId_local;
        //
        /// <summary>
        /// How to handle anonymouse access
        /// 0 = allow anonymouse access
        /// 1 = block with login form (addon site propery loginaddonid)
        /// 2 = block with login message (getcontentcopy with name = site property AnonymousUserResponseCopy)
        /// 3 = redirect to login page
        /// </summary>
        public int anonymousUserResponseID {
            get {
                if (anonymousUserResponseID_local != null) { return (int)anonymousUserResponseID_local; }
                anonymousUserResponseID_local = getInteger(anonymousUserResponseId_spName, 0);
                return (int)anonymousUserResponseID_local;
            }
            set {
                anonymousUserResponseID_local = value;
                setProperty(anonymousUserResponseId_spName, (int)anonymousUserResponseID_local);
            }
        }
        private const string anonymousUserResponseId_spName = "anonymousUserResponseID";
        private int? anonymousUserResponseID_local;
        //
        //====================================================================================================
        /// <summary>
        /// clear a value from the database
        /// </summary>
        /// <param name="key"></param>
        public void clearProperty(string key) {
            if (string.IsNullOrWhiteSpace(key)) { return; }
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
        /// for now this is just internal - a way for the /admin site to force htmlPlaformVersion to 4 no matter how the public site is set
        /// </summary>
        public int htmlPlatformOverride {
            set {
                _htmlPlatformVersion = value;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// A system flag used to coordinate resources into platforms. Specifically to let addons use both bootstrap 4 or 5.
        /// </summary>
        public int htmlPlatformVersion {
            get {
                if (_htmlPlatformVersion != null) { return (int)_htmlPlatformVersion; }
                _htmlPlatformVersion = getInteger("html platform version", 5);
                return (int)_htmlPlatformVersion;
            }
        }
        private int? _htmlPlatformVersion = null;
        //
        //====================================================================================================
        /// <summary>
        /// The site property for the system bounce address
        /// </summary>
        public string emailBounceAddress {
            get {
                // compound assignment -- if the value is null, make the assignment
                _emailBounceAddress ??= getText("EmailBounceAddress");
                return Convert.ToString(_emailBounceAddress);
            }
        }
        private string _emailBounceAddress = null;
        //
        //====================================================================================================
        //
        public string avatarDefaultPathFilename {
            get {
                return getPropertyBase("avatarDefaultPathFilename", "" + cdnPrefix + "baseAssets/avatarDefault.jpg", ref _avatarDefaultPathFilename);
            }
        }
        private string _avatarDefaultPathFilename = null;
        //
        //====================================================================================================
        /// <summary>
        /// The site property for the default system email from-address
        /// </summary>
        public string emailFromAddress {
            get {
                if (_emailFromAddress == null) {
                    _emailFromAddress = getText("emailFromAddress");
                }
                return Convert.ToString(_emailFromAddress);
            }
        }
        private string _emailFromAddress = null;
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
                if (core?.appConfig is null) { return true; }
                return (core.appConfig.appStatus != AppConfigModel.AppStatusEnum.ok);
            }
        }
        //
        //====================================================================================================
        //
        private int getPropertyBase(string propertyName, int defaultValue, ref int? localStore) {
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
        private bool getPropertyBase(string propertyName, bool defaultValue, ref bool? localStore) {
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
        private string getPropertyBase(string propertyName, string defaultValue, ref string localStore) {
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
                return getPropertyBase("LandingPageID", 0, ref _landingPageId);
            }
        }
        private int? _landingPageId = null;
        //
        //====================================================================================================
        //
        internal bool allowAutoLogin {
            get {
                return getPropertyBase("allowautologin", false, ref _AllowAutoLogin);
            }
        }
        private bool? _AllowAutoLogin = null;
        //
        //====================================================================================================
        //
        internal bool allowAutoRecognize {
            get {
                return getPropertyBase("allowautorecognize", false, ref _allowAutoRecognize);
            }
        }
        private bool? _allowAutoRecognize = null;
        //
        //====================================================================================================
        //
        public string loginIconFilename {
            get {
                return getPropertyBase("LoginIconFilename", "" + cdnPrefix + "images/ccLibLogin.GIF", ref _LoginIconFilename);
            }
        }
        private string _LoginIconFilename = null;
        //
        //====================================================================================================
        /// <summary>
        /// If disabled, do not automatically track user visits. Initialize user tracking if cp.user.id is referenced.
        /// </summary>
        public bool allowVisitTracking {
            get {
                // todo, new site with visit tracking false blocked sessions. when fixed, make default false
                return getPropertyBase("allowVisitTracking", true, ref _allowVisitTracking);
            }
        }
        private bool? _allowVisitTracking;
        //
        //====================================================================================================
        //
        public string serverPageDefault {
            get {
                return getPropertyBase(sitePropertyName_ServerPageDefault, sitePropertyDefaultValue_ServerPageDefault, ref _ServerPageDefault_local);
            }
        }
        private string _ServerPageDefault_local = null;
        //
        //====================================================================================================
        //
        internal int defaultWrapperID {
            get {
                return getPropertyBase("DefaultWrapperID", 0, ref _defaultWrapperId);
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
                return getPropertyBase("allowLinkAlias", true, ref _allowLinkAlias_Local);
            }
        }
        private bool? _allowLinkAlias_Local = null;
        //
        //====================================================================================================
        //
        public string docTypeDeclaration {
            get {
                return getPropertyBase("DocTypeDeclaration", DTDDefault, ref _docTypeDeclaration);
            }
        }
        private string _docTypeDeclaration = null;
        //
        //====================================================================================================
        //
        public bool useContentWatchLink {
            get {
                return getPropertyBase("UseContentWatchLink", false, ref _useContentWatchLink);
            }
        }
        private bool? _useContentWatchLink = null;
        //
        //====================================================================================================
        //
        public int defaultFormInputWidth {
            get {
                return getPropertyBase("DefaultFormInputWidth", 60, ref _defaultFormInputWidth);
            }
        }
        private int? _defaultFormInputWidth = null;
        //
        //====================================================================================================
        //
        public int selectFieldWidthLimit {
            get {
                return getPropertyBase("SelectFieldWidthLimit", 200, ref _selectFieldWidthLimit);
            }
        }
        private int? _selectFieldWidthLimit = null;
        //
        //====================================================================================================
        //
        public int selectFieldLimit {
            get {
                return getPropertyBase("SelectFieldLimit", 1000, ref _selectFieldLimit);
            }
        }
        private int? _selectFieldLimit = null;
        //
        //====================================================================================================
        //
        public int defaultFormInputTextHeight {
            get {
                return getPropertyBase("DefaultFormInputTextHeight", 1, ref _defaultFormInputTextHeight);
            }
        }
        private int? _defaultFormInputTextHeight = null;
        //
        //====================================================================================================
        //
        public string emailAdmin {
            get {
                return getPropertyBase("EmailAdmin", "webmaster@" + core.webServer.requestDomain, ref _emailAdmin);
            }
        }
        private string _emailAdmin = null;
        //====================================================================================================
        //
        public bool imageAllowUpdate {
            get {
                return getPropertyBase("ImageAllowUpdate", true, ref _imageAllowUpdate);
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
                            + "1,0," + DbController.encodeSQLText(propertyName.ToUpper()) + "," + DbController.encodeSQLText(Value) + "," + SQLNow + "," + SQLNow + ");";
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
                if (_allowCache_notCached != null) { return (bool)_allowCache_notCached; }
                // -- special case, allowCache referenced in nameValueDict load
                if (_nameValueDict != null) {
                    return getPropertyBase("AllowBake", true, ref _allowCache_notCached);
                }
                if (dbNotReady) { return false; }
                bool propertyFound = false;
                _allowCache_notCached = GenericController.encodeBoolean(getTextFromDb("AllowBake", "true", ref propertyFound));
                return (bool)_allowCache_notCached;
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
                return getPropertyBase("BuildVersion", "", ref _buildVersion);
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
                if (dbNotReady) { throw new GenericException("Cannot access site property collection if database is not ready."); }
                if (_nameValueDict != null) { return _nameValueDict; }
                _nameValueDict = SitePropertyModel.getNameValueDict(core.cpParent);
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
        public bool allowMinify {
            get {
                if (allowMinify_Local != null) { return (bool)allowMinify_Local; }
                allowMinify_Local = getBoolean("Allow Addon JS and CSS Minify", true);
                return (bool)allowMinify_Local;
            }
        }
        private bool? allowMinify_Local = null;
        //
        /// <summary>
        /// file returned from site property 'RobotsTxtFilename'
        /// </summary>
        public string robotsTxt {
            get {
                const string defaultFilename = "settings/RobotsTxtFilename.txt";
                //
                string result = core.privateFiles.readFileText(robotsTxtFilename);
                // -- legacy migrations
                if (result == "settings/RobotsTxtFilename.txt") { result = ""; }
                if (result == "Settings/RobotsTxtFilename.txt") { result = ""; }
                if (result == "config/RobotsTxtBase.txt") { result = ""; }
                if (string.IsNullOrEmpty(result)) {
                    //
                    // reset the filename and save default robots.txt
                    robotsTxtFilename = defaultFilename;
                    result = "User-agent: *\r\nDisallow: /admin/";
                    core.privateFiles.saveFile(robotsTxtFilename, result);
                }
                return result;
            }
        }
        //
        public string robotsTxtFilename {
            get {
                if (robotsTxtFilename_Local != null) { return robotsTxtFilename_Local; }
                robotsTxtFilename_Local = getText("RobotsTxtFilename", "settings/RobotsTxtFilename.txt");
                return (string)robotsTxtFilename_Local;
            }
            set {
                setProperty("RobotsTxtFilename", value);
            }
        }
        private string robotsTxtFilename_Local = null;
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}