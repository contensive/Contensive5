﻿
using System;
using Contensive.Processor.Models.Db;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using Contensive.Processor.Models.Domain;
//
namespace Contensive.Processor {
    public class CPUserClass : BaseClasses.CPUserBaseClass, IDisposable {
        //
        #region COM GUIDs
        public const string ClassId = "08EF64C6-9C51-4B32-84D9-0D3BDAF42A28";
        public const string InterfaceId = "B1A95B1F-A00D-4AC6-B3A0-B1619568C2EA";
        public const string EventsId = "DBE2B6CB-6339-4FFB-92D7-BE37AEA841CC";
        #endregion
        //
        //====================================================================================================
        /// <summary>
        /// dependencies
        /// </summary>
        private CPClass cp;
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
        /// authenticate to the provided credentials
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override int GetIdByLogin(string username, string password) {
            return cp.core.session.getUserIdForUsernameCredentials(username, password);
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
                    var user = PersonModel.addDefault(cp.core, ContentMetadataModel.createByUniqueName( cp.core, PersonModel.contentName));
                    user.createdByVisit = true;
                    user.save(cp.core);
                    SessionController.recognizeById(cp.core, user.id, ref cp.core.session);
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
        public override bool IsGuest {
            get {
                return cp.core.session.isGuest();
            }
        }
        //
        //====================================================================================================
        //
        public override bool IsInGroup(string groupName, int userId) {
            bool result = false;
            //
            try {
                int groupId = cp.Group.GetId(groupName);
                if (userId == 0) {
                    userId = Id;
                }
                if (groupId == 0) {
                    result = false;
                } else {
                    result = IsInGroupList(groupId.ToString(), userId);
                }
            } catch (Exception ex) {
                LogController.logError(cp.core,ex);
                result = false;
            }
            return result;
        }
        //
        public override bool IsInGroup(string groupName) => IsInGroup(groupName, cp.User.Id);
        //
        //====================================================================================================
        //
        public override bool IsInGroupList(string groupIDList, int userId) {
            bool result = false;
            //
            try {
                if (userId == 0) {
                    userId = Id;
                }
                result = GroupController.isInGroupList(cp.core, userId, IsAuthenticated, groupIDList, false);
            } catch (Exception ex) {
                LogController.logError(cp.core,ex);
                result = false;
            }
            return result;
            //
        }
        //
        //====================================================================================================
        //
        public override bool IsInGroupList(string groupIDList) => IsInGroupList(groupIDList, cp.User.Id);
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
                return cp.core.session.user.languageID;
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
                cp.core.session.user.save(cp.core);
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
        public override void Logout() {
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
                return cp.core.session.user.organizationID;
            }
        }
        //
        //====================================================================================================
        //
        public override bool Recognize(int userID) {
            return SessionController.recognizeById(cp.core, userID, ref cp.core.session);
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
        public override void SetProperty(string key, string value) => cp.core.userProperty.setProperty(key, value);
        //
        public override void SetProperty(string key, int value) => cp.core.userProperty.setProperty(key, value);
        //
        public override void SetProperty(string key, double value) => cp.core.userProperty.setProperty(key, value);
        //
        public override void SetProperty(string key, bool value) => cp.core.userProperty.setProperty(key, value);
        //
        public override void SetProperty(string key, DateTime value) => cp.core.userProperty.setProperty(key, value);
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
        //====================================================================================================
        // todo  obsolete
        //
        public override void Track() {
            int localId = Id;
        }
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
        [Obsolete("Deprecated.", false)]
        public override void SetProperty(string PropertyName, string Value, int TargetMemberId) {
            cp.core.userProperty.setProperty(PropertyName, Value, TargetMemberId);
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
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }
        protected bool disposed = false;
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