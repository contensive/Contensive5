
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Methods to send and manage email.
    /// </summary>
    public abstract class CPEmailBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Returns the site's default email from address
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string fromAddressDefault { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Sends an email to an email address. Return false if the email could not be sent
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        /// <remarks></remarks>
        public abstract void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage);
        /// <summary>
        /// Sends an email to an email address. Return false if the email could not be sent
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        public abstract void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML);
        /// <summary>
        /// Sends an email to an email address. Return false if the email could not be sent
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        public abstract void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately);
        /// <summary>
        /// Sends an email to an email address. Return false if the email could not be sent
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public abstract void send(string toAddress, string fromAddress, string subject, string body);
        //
        //====================================================================================================
        /// <summary>
        /// Sends an email that includes all the form elements in the current webpage response.
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        /// <remarks></remarks>
        public abstract void sendForm(string toAddress, string fromAddress, string subject, ref string adminErrorMessage);
        /// <summary>
        /// Sends an email that includes all the form elements in the current webpage response.
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        public abstract void sendForm(string toAddress, string fromAddress, string subject);
        //
        //====================================================================================================
        /// <summary>
        /// Send a list of usernames and passwords to the account(s) that include the given email address. If false, the email could not be sent.
        /// </summary>
        /// <param name="userEmailAddress"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        /// <remarks></remarks>
        public abstract void sendPassword(string userEmailAddress, ref string adminErrorMessage);
        /// <summary>
        /// Send a list of usernames and passwords to the account(s) that include the given email address. If false, the email could not be sent.
        /// </summary>
        /// <param name="userEmailAddress"></param>
        public abstract void sendPassword(string userEmailAddress);
        //
        //====================================================================================================
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailName"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        /// <remarks></remarks>
        public abstract void sendSystem(string emailName, string additionalCopy, int additionalUserID, ref string adminErrorMessage);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailName"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        public abstract void sendSystem(string emailName, string additionalCopy, int additionalUserID);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailName"></param>
        /// <param name="additionalCopy"></param>
        public abstract void sendSystem(string emailName, string additionalCopy);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailName"></param>
        public abstract void sendSystem(string emailName);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailId"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        public abstract void sendSystem(int emailId, string additionalCopy, int additionalUserID, ref string adminErrorMessage);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailId"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        public abstract void sendSystem(int emailId, string additionalCopy, int additionalUserID);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailId"></param>
        /// <param name="additionalCopy"></param>
        public abstract void sendSystem(int emailId, string additionalCopy);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailId"></param>
        public abstract void sendSystem(int emailId);
        //
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailName"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        /// <param name="sendImmediately"></param>
        /// <remarks></remarks>
        public abstract void sendSystem(string emailName, string additionalCopy, int additionalUserID, ref string adminErrorMessage, bool sendImmediately);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailName"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendSystem(string emailName, string additionalCopy, int additionalUserID, bool sendImmediately );
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailName"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendSystem(string emailName, string additionalCopy, bool sendImmediately);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailName"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendSystem(string emailName, bool sendImmediately);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailId"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        /// <param name="sendImmediately"></param>
        public abstract void sendSystem(int emailId, string additionalCopy, int additionalUserID, ref string adminErrorMessage, bool sendImmediately);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailId"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendSystem(int emailId, string additionalCopy, int additionalUserID, bool sendImmediately);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailId"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendSystem(int emailId, string additionalCopy, bool sendImmediately);
        /// <summary>
        /// Send a system email record. If the EmailIdOrName field contains a number, it is assumed first to be an Id. If false, the email could not be sent
        /// </summary>
        /// <param name="emailId"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendSystem(int emailId, bool sendImmediately);
        //
        //====================================================================================================
        /// <summary>
        /// Sends an email to everyone in a group. (legacy support: if groupName is a valid guid it is assumed to be)
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        /// <remarks></remarks>
        public abstract void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage);
        /// <summary>
        /// Sends an email to everyone in a group. (legacy support: if groupName is a valid guid it is assumed to be)
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        public abstract void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML);
        /// <summary>
        /// Sends an email to everyone in a group. (legacy support: if groupName is a valid guid it is assumed to be)
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately);
        /// <summary>
        /// Sends an email to everyone in a group. (legacy support: if groupName is a valid guid it is assumed to be)
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public abstract void sendGroup(string groupName, string fromAddress, string subject, string body);
        /// <summary>
        /// Sends an email to everyone in a group.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        public abstract void sendGroup(int groupId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage);
        /// <summary>
        /// Sends an email to everyone in a group.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        public abstract void sendGroup(int groupId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML);
        /// <summary>
        /// Sends an email to everyone in a group.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendGroup(int groupId, string fromAddress, string subject, string body, bool sendImmediately);
        /// <summary>
        /// Sends an email to everyone in a group.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public abstract void sendGroup(int groupId, string fromAddress, string subject, string body);
        /// <summary>
        /// Sends an email to everyone in a list of groups.
        /// </summary>
        /// <param name="groupNameList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        //
        public abstract void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage);
        /// <summary>
        /// Sends an email to everyone in a list of groups.
        /// </summary>
        /// <param name="groupNameList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        public abstract void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML);
        /// <summary>
        /// Sends an email to everyone in a list of groups.
        /// </summary>
        /// <param name="groupNameList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body, bool sendImmediately);
        /// <summary>
        /// Sends an email to everyone in a list of groups.
        /// </summary>
        /// <param name="groupNameList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public abstract void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body);
        /// <summary>
        /// Sends an email to everyone in a list of groups.
        /// </summary>
        /// <param name="groupIdList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        public abstract void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage);
        /// <summary>
        /// Sends an email to everyone in a list of groups.
        /// </summary>
        /// <param name="groupIdList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        public abstract void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML);
        /// <summary>
        /// Sends an email to everyone in a list of groups.
        /// </summary>
        /// <param name="groupIdList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body, bool sendImmediately);
        /// <summary>
        /// Sends an email to everyone in a list of groups.
        /// </summary>
        /// <param name="groupIdList"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public abstract void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body);
        //
        //====================================================================================================
        /// <summary>
        /// Send an email a user.
        /// </summary>
        /// <param name="toUserId"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        /// <param name="adminErrorMessage">If not empty, this is an error message appropriate for an administrator, not a public user.</param>
        /// <remarks></remarks>
        public abstract void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage);
        /// <summary>
        /// Send an email a user.
        /// </summary>
        /// <param name="toUserId"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        public abstract void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML);
        /// <summary>
        /// Send an email a user.
        /// </summary>
        /// <param name="toUserId"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        public abstract void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately);
        /// <summary>
        /// Send an email a user.
        /// </summary>
        /// <param name="toUserId"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public abstract void sendUser(int toUserId, string fromAddress, string subject, string body);
        /// <summary>
        /// Send an email a user.
        /// </summary>
        /// <param name="toAddress"></param>
        /// <returns></returns>
        public abstract bool validateEmail(string toAddress);
        /// <summary>
        /// Validate a user email
        /// </summary>
        /// <param name="toUserId"></param>
        /// <returns></returns>
        public abstract bool validateUserEmail(int toUserId);
        //
        //====================================================================================================
        // deprecated
        //
        /// <summary>
        /// use setUser with argument int toUserId
        /// </summary>
        /// <param name="ToUserID"></param>
        /// <param name="FromAddress"></param>
        /// <param name="Subject"></param>
        /// <param name="Body"></param>
        /// <param name="SendImmediately"></param>
        /// <param name="BodyIsHTML"></param>
        [Obsolete("use setUser with argument int toUserId", false)] public abstract void sendUser(string ToUserID, string FromAddress, string Subject, string Body, bool SendImmediately, bool BodyIsHTML);
        /// <summary>
        /// use setUser with argument int toUserId
        /// </summary>
        /// <param name="ToUserID"></param>
        /// <param name="FromAddress"></param>
        /// <param name="Subject"></param>
        /// <param name="Body"></param>
        /// <param name="SendImmediately"></param>
        [Obsolete("use setUser with argument int toUserId", false)] public abstract void sendUser(string ToUserID, string FromAddress, string Subject, string Body, bool SendImmediately);
        /// <summary>
        /// use setUser with argument int toUserId
        /// </summary>
        /// <param name="ToUserID"></param>
        /// <param name="FromAddress"></param>
        /// <param name="Subject"></param>
        /// <param name="Body"></param>
        [Obsolete("use setUser with argument int toUserId", false)] public abstract void sendUser(string ToUserID, string FromAddress, string Subject, string Body);
    }
}

