
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using NLog;
using System;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor {
    public class CPUserClass : BaseClasses.CPUserBaseClass, IDisposable {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        //=======================================================================================================
        /// <summary>
        /// Clear a property
        /// </summary>
        /// <param name="key"></param>
        public override void ClearProperty(string key) {
            cp.core.userProperty.clearProperty(key);
        }
        //
        //====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="coreObj"></param>
        /// <param name="cp"></param>
        public CPUserClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return authenticated user's email
        /// </summary>
        public override string Email {
            get {
                if (cp?.core?.session?.user == null) { return ""; }
                return cp.core.session.user.email;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return the id of the user that matches these credentials. No authentication is performed. Password is ignored in no-password mode (preferences setting)
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override int GetIdByLogin(string username, string password) {
            if (cp?.core?.session == null) { return 0; }
            string userErrorMessage = "";
            return AuthController.preflightAuthentication_returnUserId(cp.core, cp.core.session, username, password, false, ref userErrorMessage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Returns the id of the user in the current session context. If 0, this action will create a user.
        /// This trigger allows sessions with guest detection disabled that will enable if used.
        /// </summary>
        public override int Id {
            get {
                if (cp?.core?.session?.user == null) { return 0; }
                if (cp.core.session.user.id != 0) { return cp.core.session.user.id; }
                cp.core.session.verifyUser();
                return cp.core.session.user.id;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Returns the id of the user in the current session context. If 0, does NOT create a user for the session
        /// </summary>
        public override int IdInSession {
            get {
                if (cp?.core?.session?.user == null) { return 0; }
                return cp.core.session.user.id;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Checks if the current user is authenticated and has the admin role.
        /// </summary>
        public override bool IsAdmin {
            get {
                if (cp?.core?.session == null) { return false; }
                return cp.core.session.isAuthenticatedAdmin();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Checks if the current user is authenticated and is advanced editting.
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public override bool IsAdvancedEditing(string contentName) {
            if (cp?.core?.session == null) { return false; }
            return cp.core.session.isAdvancedEditing();
        }
        //
        //====================================================================================================
        //
        public override bool IsAdvancedEditing() => IsAdvancedEditing("");
        //
        //====================================================================================================
        /// <summary>
        /// Is the current user authenticated
        /// </summary>
        public override bool IsAuthenticated {
            get {
                if (cp?.core?.session == null) { return false; }
                return cp.core.session.isAuthenticated;
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsContentManager() {
            if (cp?.core?.session == null) { return false; }
            return cp.core.session.isAuthenticatedContentManager();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Is the current user authenticated and a content manager for the specified content.
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public override bool IsContentManager(string contentName) {
            if (cp?.core?.session == null) { return false; }
            return cp.core.session.isAuthenticatedContentManager(contentName);
        }
        //
        //====================================================================================================
        //
        public override bool IsDeveloper {
            get {
                if (cp?.core?.session == null) { return false; }
                return cp.core.session.isAuthenticatedDeveloper();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsEditing(string contentName) {
            if (cp?.core?.session == null) { return false; }
            return cp.core.session.isEditing(contentName);
        }
        //
        //====================================================================================================
        //
        public override bool IsEditing() {
            if (cp?.core?.session == null) { return false; }
            return cp.core.session.isEditing();
        }
        //
        //====================================================================================================
        //
        [Obsolete("Use IsEditing()", false)]
        public override bool IsEditingAnything {
            get {
                if (cp?.core?.session == null) { return false; }
                return cp.core.session.isEditing();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsTemplateEditing {
            get {
                if (cp?.core?.session == null) { return false; }
                return cp.core.session.isTemplateEditing();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsPageBuilderEditing {
            get {
                if (cp?.core?.session == null) { return false; }
                return cp.core.session.isPageBuilderEditing();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsDebugging {
            get {
                if (cp?.core?.session == null) { return false; }
                return cp.core.session.isDebugging();
            }
        }
        //
        //
        //====================================================================================================
        //
        public override bool IsGuest {
            get {
                if (cp?.core?.session == null) { return true; }
                return cp.core.session.isGuest();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if the specified user is in the specified group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public override bool IsInGroup(string groupName, int userId) {
            try {
                if (cp?.Group == null) { return false; }
                int groupId = cp.Group.GetId(groupName);
                if (groupId == 0) {
                    return false;
                }
                return IsInGroupList(groupId.ToString(), userId);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if the current session user is authenticated and is in the current group
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public override bool IsInGroup(string groupName) {
            if (!IsAuthenticated) { return false; }
            return IsInGroup(groupName, IdInSession);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if the specified user is in the specified group
        /// </summary>
        /// <param name="groupIDList"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public override bool IsInGroupList(string groupIDList, int userId) {
            return GroupController.isInGroupList(cp.core, userId, true, groupIDList, false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if the current session user is authenticated and is in the current group list
        /// </summary>
        /// <param name="groupIDList"></param>
        /// <returns></returns>
        public override bool IsInGroupList(string groupIDList) {
            if (!IsAuthenticated) { return false; }
            return IsInGroupList(groupIDList, IdInSession);
        }
        //
        //====================================================================================================
        //
        [Obsolete("deprecated", true)]
        public override bool IsMember {
            get {
                if (cp?.core?.session == null) { return false; }
                return cp.core.session.isAuthenticatedMember();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsQuickEditing(string ignore) {
            if (cp?.core?.session == null) { return false; }
            return cp.core.session.isPageBuilderEditing();
        }
        //
        //====================================================================================================
        //
        public override bool IsRecognized {
            get {
                if (cp?.core?.session == null) { return false; }
                return cp.core.session.isRecognized();
            }
        }
        //
        //====================================================================================================
        //
        [Obsolete("deprecated", true)]
        public override bool IsWorkflowRendering {
            get {
                return false;
            }
        }
        //
        //====================================================================================================
        //
        public override string Language {
            get {
                if (cp?.core?.session == null) { return string.Empty; }
                if (cp.core.session.userLanguage != null) {
                    return cp.core.session.userLanguage.name;
                }
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        //
        public override int LanguageID {
            get {
                if (cp?.core?.session == null) { return 0; }
                return cp.core.session.user.languageId;
            }
        }
        //
        //====================================================================================================
        //
        public override bool Login(string username, string password, bool setAutoLogin) {
            string userErrorMessage = "";
            return AuthController.login(cp.core, cp.core.session, username, password, setAutoLogin, ref userErrorMessage);
        }
        public override bool Login(string username, string password) {
            string userErrorMessage = "";
            return AuthController.login(cp.core, cp.core.session, username, password, false, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override bool LoginByID(int userId) {
            return AuthController.loginById(cp.core, cp.core.session, userId, false);
        }
        //
        //====================================================================================================
        //
        public override bool LoginByID(int userId, bool setAutoLogin) {
            return AuthController.loginById(cp.core, cp.core.session, userId, setAutoLogin);
        }
        //
        //====================================================================================================
        //
        public override bool LoginIsOK(string username, string password) {
            string userErrorMessage = "";
            return !AuthController.preflightAuthentication_returnUserId(cp.core, cp.core.session, username, password, false, ref userErrorMessage).Equals(0);
        }
        //
        //====================================================================================================
        //
        public override void Logout() {
            AuthController.logout(cp.core, cp.core.session);
        }
        //
        //====================================================================================================
        //
        public override string Name {
            get {
                if (cp?.core?.session?.user == null) { return ""; }
                return cp.core.session.user.name;
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsNew {
            get {
                if (cp?.core?.session?.visit == null) { return true; }
                return cp.core.session.visit.memberNew;
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsNewLoginOK(string username, string password) {
            if (cp?.core?.session == null) { return false; }
            string errorMessage = "";
            int errorCode = 0;
            return cp.core.session.isNewCredentialOK(cp.User.Id, username, password, ref errorMessage, ref errorCode);
        }
        //
        //====================================================================================================
        //
        public override bool IsNewLoginOK(int userId, string username, string password, ref string errorMessage) {
            if (cp?.core?.session == null) {
                errorMessage = "There is a problem understanding the request";
                return false;
            }
            int errorCode = 0;
            return cp.core.session.isNewCredentialOK(userId, username, password, ref errorMessage, ref errorCode);
        }
        //
        //====================================================================================================
        //
        public override int OrganizationID {
            get {
                if (cp?.core?.session?.user == null) { return 0; }
                return cp.core.session.user.organizationId;
            }
        }
        //
        //====================================================================================================
        //
        public override bool Recognize(int userID) {
            return AuthController.recognizeById(cp.core, cp.core.session, userID);
        }
        //
        //====================================================================================================
        //
        public override string Username {
            get {
                if (cp?.core?.session == null) { return ""; }
                return cp.core.session.user.username;
            }
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, string value)
            => cp.core.userProperty.setProperty(key, value);
        //
        public override void SetProperty(string PropertyName, string Value, int TargetMemberId)
            => cp.core.userProperty.setProperty(PropertyName, Value, TargetMemberId);
        //
        public override void SetProperty(string key, int value)
            => cp.core.userProperty.setProperty(key, value);
        //
        public override void SetProperty(string PropertyName, int Value, int TargetMemberId)
            => cp.core.userProperty.setProperty(PropertyName, Value, TargetMemberId);
        //
        public override void SetProperty(string key, double value)
            => cp.core.userProperty.setProperty(key, value);
        //
        public override void SetProperty(string PropertyName, double Value, int TargetMemberId)
            => cp.core.userProperty.setProperty(PropertyName, Value, TargetMemberId);
        //
        public override void SetProperty(string key, bool value)
            => cp.core.userProperty.setProperty(key, value);
        //
        public override void SetProperty(string PropertyName, bool Value, int TargetMemberId)
            => cp.core.userProperty.setProperty(PropertyName, Value, TargetMemberId);
        //
        public override void SetProperty(string key, DateTime value)
            => cp.core.userProperty.setProperty(key, value);
        //
        public override void SetProperty(string PropertyName, DateTime Value, int TargetMemberId)
            => cp.core.userProperty.setProperty(PropertyName, Value, TargetMemberId);
        //
        //=======================================================================================================
        //
        public override bool GetBoolean(string key) => cp.core.userProperty.getBoolean(key);
        public override bool GetBoolean(string key, bool defaultValue) => cp.core.userProperty.getBoolean(key, defaultValue);
        //
        //=======================================================================================================
        //
        public override DateTime GetDate(string key) => cp.core.userProperty.getDate(key);
        public override DateTime GetDate(string key, DateTime defaultValue) => cp.core.userProperty.getDate(key, defaultValue);
        //
        //=======================================================================================================
        //
        public override int GetInteger(string key) => cp.core.userProperty.getInteger(key);
        public override int GetInteger(string key, int defaultValue) => cp.core.userProperty.getInteger(key, defaultValue);
        //
        //=======================================================================================================
        //
        public override double GetNumber(string key) => cp.core.userProperty.getNumber(key);
        public override double GetNumber(string key, double defaultValue) => cp.core.userProperty.getNumber(key, defaultValue);
        //
        //=======================================================================================================
        //
        public override string GetText(string key) => cp.core.userProperty.getText(key);
        public override string GetText(string key, string defaultValue) => cp.core.userProperty.getText(key, defaultValue);
        //
        //=======================================================================================================
        //
        public override T GetObject<T>(string key) {
            return cp.core.userProperty.getObject<T>(key);
        }
        //
        //=======================================================================================================
        //
        public override bool SetPassword(string password, ref string userErrorMessage) {
            return AuthController.trySetPassword(cp, password, ref userErrorMessage);
        }
        //
        //=======================================================================================================
        //
        public override bool SetPassword(string password, int userId, ref string userErrorMessage) {
            PersonModel user = DbBaseModel.create<PersonModel>(cp, userId);
            return AuthController.trySetPassword(cp, password, user, ref userErrorMessage);
        }
        //
        //=======================================================================================================
        //
        public override bool SetPassword(string password) {
            string userErrorMessage = "";
            return AuthController.trySetPassword(cp, password, ref userErrorMessage);
        }
        //
        //=======================================================================================================
        //
        public override bool SetPassword(string password, int userId) {
            PersonModel user = DbBaseModel.create<PersonModel>(cp, userId);
            return AuthController.trySetPassword(cp, password, user);
        }
        //
        //====================================================================================================
        // todo  obsolete
        //
        public override void Track() {
            //
            // -- reading the id causes forces the user to be tracked. private nonsense to defeat compile warning
            ignore_localId = Id;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private int ignore_localId;
        //
        //====================================================================================================
        // deprecated methods
        //
        [Obsolete("deprecated", true)]
        public override double GetNumber(string key, string defaultValue) => cp.core.userProperty.getNumber(key, encodeNumber(defaultValue));
        //
        [Obsolete("deprecated", true)]
        public override int GetInteger(string key, string defaultValue) => cp.core.userProperty.getInteger(key, encodeInteger(defaultValue));
        //
        [Obsolete("deprecated", true)]
        public override DateTime GetDate(string key, string defaultValue) => cp.core.userProperty.getDate(key, encodeDate(defaultValue));
        //
        [Obsolete("deprecated", true)]
        public override bool GetBoolean(string key, string defaultValue) => cp.core.userProperty.getBoolean(key, encodeBoolean(defaultValue));
        //
        [Obsolete("Use IsEditing", true)]
        public override bool IsAuthoring(string contentName) => cp.core.session.isEditing(contentName);
        //
        [Obsolete("Use LoginById(integer) instead", false)]
        public override bool LoginByID(string RecordID, bool SetAutoLogin = false) {
            return AuthController.authenticateById(cp.core, cp.core.session, encodeInteger(RecordID));
        }
        //
        //=======================================================================================================
        //
        [Obsolete("Use Get with correct type", false)]
        public override string GetProperty(string PropertyName, string DefaultValue = "", int TargetMemberId = 0) {
            if (TargetMemberId == 0) {
                return cp.core.userProperty.getText(PropertyName, DefaultValue);
            } else {
                return cp.core.userProperty.getText(PropertyName, DefaultValue, TargetMemberId);
            }
        }
        //
        [Obsolete("Use Get with correct type", false)]
        public override string GetProperty(string PropertyName, string DefaultValue) {
            return cp.core.userProperty.getText(PropertyName, DefaultValue);
        }
        //
        [Obsolete("Use Get with correct type", false)]
        public override string GetProperty(string PropertyName) {
            return cp.core.userProperty.getText(PropertyName);
        }
        //
        [Obsolete("deprecated", false)]
        public override string Password {
            get {
                return cp.core.session.user.password;
            }
        }
        //
        //====================================================================================================
        //
        #region  IDisposable Support 
        //
        //====================================================================================================
        //
        protected virtual void Dispose(bool disposing_user) {
            if (!this.disposed_user) {
                if (disposing_user) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_user = true;
        }
        protected bool disposed_user;
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CPUserClass() {
            Dispose(false);
        }
        #endregion
    }

}