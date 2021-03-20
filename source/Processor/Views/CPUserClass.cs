
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor {
    public class CPUserClass : BaseClasses.CPUserBaseClass, IDisposable {
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
            return cp.core.session.getUserIdForUsernameCredentials(username, password, false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Returns the id of the user in the current session context. If 0, this action will create a user.
        /// This trigger allows sessions with guest detection disabled that will enable if used.
        /// </summary>
        public override int Id {
            get {
                if (cp.core.session.user.id==0) {
                    cp.core.session.verifyUser();
                }
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
                return (cp.core.session.isAuthenticated);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Is the current user authenticated and a content manager for the specified content.
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public override bool IsContentManager(string contentName) {
            return cp.core.session.isAuthenticatedContentManager(contentName);
        }
        //
        //====================================================================================================
        //
        public override bool IsDeveloper {
            get {
                return cp.core.session.isAuthenticatedDeveloper();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsEditing(string contentName) {
            return cp.core.session.isEditing(contentName);
        }
        //
        //====================================================================================================
        //
        public override bool IsEditingAnything {
            get {
                return cp.core.session.isEditing();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsTemplateEditing {
            get {
                return cp.core.session.isTemplateEditing();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsPageBuilderEditing {
            get {
                return cp.core.session.isPageBuilderEditing();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsDebugging {
            get {
                return cp.core.session.isDebugging();
            }
        }
        //
        //
        //====================================================================================================
        //
        public override bool IsGuest {
            get {
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
                int groupId = cp.Group.GetId(groupName);
                if (groupId == 0) {
                    return false;
                }
                return IsInGroupList(groupId.ToString(), userId);
            } catch (Exception ex) {
                LogController.logError(cp.core,ex);
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
            return IsInGroup(groupName, Id);
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
            return IsInGroupList(groupIDList, Id);
        }
        //
        //====================================================================================================
        //
        [Obsolete("deprecated",true)]
        public override bool IsMember {
            get {
                return cp.core.session.isAuthenticatedMember();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsQuickEditing(string contentName) {
            return cp.core.session.isQuickEditing(contentName);
        }
        //
        //====================================================================================================
        //
        public override bool IsRecognized {
            get {
                return cp.core.session.isRecognized();
            }
        }
        //
        //====================================================================================================
        //
        [Obsolete("deprecated",true)]
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
                return cp.core.session.user.languageId;
            }
        }
        //
        //====================================================================================================
        //
        public override bool Login(string usernameOrEmail, string password, bool setAutoLogin) {
            return cp.core.session.authenticate(usernameOrEmail, password, setAutoLogin);
        }
        public override bool Login(string usernameOrEmail, string password) 
            => Login(usernameOrEmail, password, false);
        //
        //====================================================================================================
        //
        public override bool LoginByID(int userId) {
            return cp.core.session.authenticateById(userId, cp.core.session);
        }
        //
        //====================================================================================================
        //
        public override bool LoginByID(int userId, bool setAutoLogin) {
            bool result = cp.core.session.authenticateById(userId, cp.core.session);
            if (result) {
                cp.core.session.user.autoLogin = setAutoLogin;
                cp.core.session.user.save(cp);
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public override bool LoginIsOK(string usernameOrEmail, string password) {
            return cp.core.session.isLoginOK( usernameOrEmail, password);
        }
        //
        //====================================================================================================
        //
        public override void Logout()  {
            cp.core.session.logout();
        }
        //
        //====================================================================================================
        //
        public override string Name {
            get {
                return cp.core.session.user.name;
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsNew {
            get {
                return cp.core.session.visit.memberNew;
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsNewLoginOK(string username, string password) {
            string errorMessage = "";
            int errorCode = 0;
            return cp.core.session.isNewCredentialOK(username, password, ref errorMessage, ref errorCode);
        }
        //
        //====================================================================================================
        //
        public override int OrganizationID {
            get {
                return cp.core.session.user.organizationId;
            }
        }
        //
        //====================================================================================================
        //
        public override bool Recognize(int userID) {
            return SessionController.recognizeById(cp.core, userID, cp.core.session);
        }
        //
        //====================================================================================================
        //
        public override string Username {
            get {
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
        //====================================================================================================
        // todo  obsolete
        //
        public override void Track()   {
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
        [Obsolete("deprecated",true)]
        public override double GetNumber(string key, string defaultValue) => cp.core.userProperty.getNumber(key, encodeNumber(defaultValue));
        //
        [Obsolete("deprecated",true)]
        public override int GetInteger(string key, string defaultValue) => cp.core.userProperty.getInteger(key, encodeInteger(defaultValue));
        //
        [Obsolete("deprecated",true)]
        public override DateTime GetDate(string key, string defaultValue) => cp.core.userProperty.getDate(key, encodeDate(defaultValue));
        //
        [Obsolete("deprecated",true)]
        public override bool GetBoolean(string key, string defaultValue) => cp.core.userProperty.getBoolean(key, encodeBoolean(defaultValue));
        //
        [Obsolete("Use IsEditing",true)]
        public override bool IsAuthoring(string contentName) => cp.core.session.isEditing(contentName);
        //
        [Obsolete("Use IsContentManager( Page Content )", false)]
        public override bool IsContentManager() => IsContentManager("Page Content");
        //
        [Obsolete("Use LoginById(integer) instead", false)]
        public override bool LoginByID(string RecordID, bool SetAutoLogin = false) {
            return cp.core.session.authenticateById(encodeInteger(RecordID), cp.core.session);
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
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPUserClass()  {
            Dispose(false);
        }
        #endregion
    }

}