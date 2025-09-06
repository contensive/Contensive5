
using System;

namespace Contensive.BaseClasses {
    /// <summary>
    /// properties and methods that relate to the current user identity.
    /// </summary>
    public abstract class CPUserBaseClass {        
        //
        //====================================================================================================
        /// <summary>
        /// remove the property
        /// </summary>
        /// <param name="key"></param>
        public abstract void ClearProperty(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Get a user's id from their username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public abstract int GetIdByLogin(string username, string password);
        //
        //====================================================================================================
        /// <summary>
        /// If track users is disabled, this method begins tracking the current user
        /// </summary>
        public abstract void Track();
        //
        //====================================================================================================
        /// <summary>
        /// is the current user advance editing the indicated content
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public abstract bool IsAdvancedEditing(string contentName);
        //
        //====================================================================================================
        /// <summary>
        /// is the current user advanced editing any content
        /// </summary>
        /// <returns></returns>
        public abstract bool IsAdvancedEditing();
        //
        //====================================================================================================
        /// <summary>
        /// is the current user authetnicated
        /// </summary>
        public abstract bool IsAuthenticated { get; }
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is editing anything
        /// </summary>
        /// <returns></returns>
        public abstract bool IsEditing();
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is editing the content specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public abstract bool IsEditing(string contentName);
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is editing any content
        /// </summary>
        [Obsolete("Use IsEditing()", false)] public abstract bool IsEditingAnything { get; }
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is editing the template
        /// </summary>
        public abstract bool IsTemplateEditing { get; }
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is an administrator and editing with Page Builder (creating addon lists for pages)
        /// </summary>
        public abstract bool IsPageBuilderEditing { get; }
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is a developer and has turned on debugging
        /// </summary>
        public abstract bool IsDebugging { get; }
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is not authenticated and not recognized. 
        /// </summary>
        public abstract bool IsGuest { get; }
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user has turned on quick editing
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public abstract bool IsQuickEditing(string contentName);
        //
        //====================================================================================================
        /// <summary>
        ///  true if the current user is recognized from their visitor record, but has not logged in as that identity
        /// </summary>
        public abstract bool IsRecognized { get; }
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is not recognized and not authenticated
        /// </summary>
        public abstract bool IsNew { get; }
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is authenticated and their identity is administrator role (checkbox in people record)
        /// </summary>
        public abstract bool IsAdmin { get; }
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is authenticated and their identity is content manager role
        /// </summary>
        /// <returns></returns>
        public abstract bool IsContentManager();
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is authenticated and their identity is content manager role (in a content manager group)
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public abstract bool IsContentManager(string contentName);
        //
        //====================================================================================================
        /// <summary>
        /// true if the current user is authenticated and their identity is the developer role (checkbox in people record)
        /// </summary>
        public abstract bool IsDeveloper { get; }
        //
        //====================================================================================================
        //
        public abstract bool IsInGroup(string groupName, int userId);
        /// <summary>
        /// true if the specified user is authenticted and in the specified group
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public abstract bool IsInGroup(string groupName);
        //
        //====================================================================================================
        /// <summary>
        /// true if the specified user is authenticted and in one of the specified groups. 
        /// </summary>
        /// <param name="groupIdCommaList">A comma delimited list of one or more group Ids</param>
        /// <param name="checkUserID"></param>
        /// <returns></returns>
        public abstract bool IsInGroupList(string groupIdCommaList, int checkUserID);
        /// <summary>
        /// true if the current user is authenticted and in one of the specified groups. 
        /// </summary>
        /// <param name="groupIdCommaList"></param>
        /// <returns></returns>
        public abstract bool IsInGroupList(string groupIdCommaList);
        //
        //====================================================================================================
        /// <summary>
        /// Associate the current visit session to the visitor session, but leave the user not authenticated. This changes the user to isRecognized=true, isGuest=false, isAuthenticated=false
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public abstract bool Recognize(int userID);
        //
        //====================================================================================================
        /// <summary>
        /// Autheticate the current user to the credentials provided. If the site property AllowEmailLogin is true, this method treats the first argument as either email or username. Duplicates and email=username matches are not allowed.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="setAutoLogin">If true, and allowed on the site, the user will be automatically logged in by their visitor cookie on future visits. Not available to admin or devloper roles.</param>
        /// <returns></returns>
        public abstract bool Login(string username, string password, bool setAutoLogin);
        /// <summary>
        /// Autheticate the current user to the credentials provided. If the site property AllowEmailLogin is true, this method treats the first argument as either email or username. Duplicates and email=username matches are not allowed.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public abstract bool Login(string username, string password);
        //
        //====================================================================================================
        /// <summary>
        /// Autheticate the current user to the identity id provided.
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public abstract bool LoginByID(int recordId);
        //
        //====================================================================================================
        /// <summary>
        /// Autheticate the current user to the identity id provided. If the site property AllowEmailLogin is true, this method treats the first argument as either email or username. Duplicates and email=username matches are not allowed.
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="setAutoLogin"></param>
        /// <returns></returns>
        public abstract bool LoginByID(int recordId, bool setAutoLogin);
        //
        //====================================================================================================
        /// <summary>
        /// returns true if the credentials provided are valid for a user in the system.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public abstract bool LoginIsOK(string username, string password);
        //
        //====================================================================================================
        /// <summary>
        /// logout the current user
        /// </summary>
        public abstract void Logout();
        //
        //====================================================================================================
        /// <summary>
        /// returns true if the credentials are valid, and not currently in user (the current user can use them)
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public abstract bool IsNewLoginOK(string username, string password);
        //
        //====================================================================================================
        /// <summary>
        /// returns true if the credentials are valid, and not currently in user (the current user can use them)
        /// </summary>
        /// <param name="userId">The id of the user who wants to update their username and password</param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="errorMessage">if the result is false, this is a user compatible message that explains how they can fix the issue</param>
        /// <returns></returns>
        public abstract bool IsNewLoginOK(int userId, string username, string password, ref string errorMessage);
        //
        //====================================================================================================
        /// <summary>
        /// The language relfected in the browser metadata
        /// </summary>
        public abstract string Language { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The id of the language relfected in the browser metadata
        /// </summary>
        public abstract int LanguageID { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If autheticated, the current user's email 
        /// </summary>
        public abstract string Email { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The user's id. If session tracking is disable, requesting the id will create a new guest user.
        /// </summary>
        public abstract int Id { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The user's id. If session tracking is disable, requesting the id will return 0.
        /// </summary>
        public abstract int IdInSession { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If autheticated, the current user's name
        /// </summary>
        public abstract string Name { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If autheticated, the current user's organization
        /// </summary>
        public abstract int OrganizationID { get; }
        //
        //====================================================================================================
        /// <summary>
        /// If autheticated, the current user's username
        /// </summary>
        public abstract string Username { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string GetText(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key. If the key doesn't exist, save and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract string GetText(string key, string defaultValue);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key. If the key doesn't exist, save and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key, bool defaultValue);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key. If the key doesn't exist, save and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key, DateTime defaultValue);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key. If the key doesn't exist, save and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key, int defaultValue);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key. If the key doesn't exist, save and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key, double defaultValue);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract T GetObject<T>(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Set a text user property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, string value);
        //
        //====================================================================================================
        /// <summary>
        /// Set a text user property for a specific user
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        public abstract void SetProperty(string key, string value, int userId);
        //
        //====================================================================================================
        /// <summary>
        /// Set a user property for this type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, int value);
        //
        //====================================================================================================
        /// <summary>
        /// Set an integer user property for a specific user
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        public abstract void SetProperty(string key, int value, int userId);
        //
        //====================================================================================================
        /// <summary>
        /// Set a user property for this type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, double value);
        //
        //====================================================================================================
        /// <summary>
        /// Set a number (double) user property for a specific user
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        public abstract void SetProperty(string key, double value, int userId);
        //
        //====================================================================================================
        /// <summary>
        /// Set a user property for this type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, bool value);
        //
        //====================================================================================================
        /// <summary>
        /// Set a boolean user property for a specific user
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        public abstract void SetProperty(string key, bool value, int userId);
        //
        //====================================================================================================
        /// <summary>
        /// Set a user property for this type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, DateTime value);
        //
        //====================================================================================================
        /// <summary>
        /// Set a datetime user property for a specific user
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="userId"></param>
        public abstract void SetProperty(string key, DateTime value, int userId);
        //
        //====================================================================================================
        /// <summary>
        /// Set the current user's password. If in plain-text password mode, the password field is populated, else the passwordHash field is populated.
        /// Return true if the password meets password policies.
        /// </summary>
        /// <param name="password"></param>
        public abstract bool SetPassword(string password);
        //
        //====================================================================================================
        /// <summary>
        /// Set the current user's password. If in plain-text password mode, the password field is populated, else the passwordHash field is populated.
        /// Return true if the password meets password policies.
        /// If error, return a user appropriate error message.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="userErrorMessage"></param>
        public abstract bool SetPassword(string password, ref string userErrorMessage);
        //
        //====================================================================================================
        /// <summary>
        /// Set the specific user's password. If in plain-text password mode, the password field is populated, else the passwordHash field is populated.
        /// Return true if the password meets password policies.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="userId"></param>
        public abstract bool SetPassword(string password, int userId);
        //
        //====================================================================================================
        /// <summary>
        /// Set the specific user's password. If in plain-text password mode, the password field is populated, else the passwordHash field is populated.
        /// Return true if the password meets password policies.
        /// If error, return a user appropriate error message.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="userId"></param>
        /// <param name="userErrorMessage"></param>
        public abstract bool SetPassword(string password, int userId, ref string userErrorMessage);
        //
        //====================================================================================================
        //
        // -- deprecated
        //
        [Obsolete("Use LoginById(integer) instead", false)]
        public abstract bool LoginByID(string RecordID, bool SetAutoLogin = false);
        //
        [Obsolete("correct default type", false)]
        public abstract bool GetBoolean(string PropertyName, string DefaultValue);
        //
        [Obsolete("correct default type", false)]
        public abstract DateTime GetDate(string PropertyName, string DefaultValue);
        //
        [Obsolete("correct default type", false)]
        public abstract int GetInteger(string PropertyName, string DefaultValue);
        //
        [Obsolete("correct default type", false)]
        public abstract double GetNumber(string PropertyName, string DefaultValue);
        //
        [Obsolete("Use IsEditing()",true)]
        public abstract bool IsAuthoring(string contentName);
        //
        [Obsolete("deprecated", false)]
        public abstract bool IsWorkflowRendering { get; }
        //
        [Obsolete("Deprecated. Use another method to differentiate types of users, like groups, select lists, etc.", false)]
        public abstract bool IsMember { get; }
        //
        [Obsolete("Deprecated.", false)]
        public abstract string Password { get; }
        //
        [Obsolete("Use the Get method with the correct type.", false)]
        public abstract string GetProperty(string key, string defaultValue, int targetVisitId);
        //
        [Obsolete("Use the Get method with the correct type.", false)]
        public abstract string GetProperty(string key, string defaultValue);
        //
        [Obsolete("Use the Get method with the correct type.", false)]
        public abstract string GetProperty(string key);
    }
}

