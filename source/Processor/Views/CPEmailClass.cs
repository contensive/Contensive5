
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;

namespace Contensive.Processor {
    public class CPEmailClass : BaseClasses.CPEmailBaseClass, IDisposable {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        //==========================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        public CPEmailClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //==========================================================================================
        //
        public override string fromAddressDefault {
            get {
                return cp.core.siteProperties.getText("EMAILFROMADDRESS");
            }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Send email to an email address.
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="sendImmediately"></param>
        /// <param name="bodyIsHTML"></param>
        public override void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage) {
            try {
                // todo -- add personalization to text
                int personalizeAddonId = 0;
                EmailController.sendAdHocEmail(cp.core, "Ad Hoc email from api", 0, toAddress, fromAddress, subject, body, fromAddress, fromAddress, "", sendImmediately, bodyIsHTML, 0, ref adminErrorMessage, personalizeAddonId);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        public override void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string adminErrorMessage = "";
            send(toAddress, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage);
        }
        //
        public override void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately) {
            string adminErrorMessage = "";
            send(toAddress, fromAddress, subject, body, sendImmediately, true, ref adminErrorMessage);
        }
        //
        public override void send(string toAddress, string fromAddress, string subject, string body) {
            string adminErrorMessage = "";
            send(toAddress, fromAddress, subject, body, true, true, ref adminErrorMessage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send submitted form within an email
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="adminErrorMessage"></param>
        public override void sendForm(string toAddress, string fromAddress, string subject, ref string adminErrorMessage) {
            try {
                EmailController.trySendFormEmail(cp.core, toAddress, fromAddress, subject, out string errorMessage);
                adminErrorMessage = errorMessage;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        public override void sendForm(string toAddress, string fromAddress, string subject) {
            string adminErrorMessage = "";
            sendForm(toAddress, fromAddress, subject, ref adminErrorMessage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// if email not found, return
        /// if found, send the user an email with a token, and save the token in visit.
        /// </summary>
        /// <param name="userEmail"></param>
        /// <param name="userErrorMessage"></param>
        public override void sendPassword(string userEmail, ref string userErrorMessage) {
            if (string.IsNullOrEmpty(userEmail)) { return; }
            //
            foreach (PersonModel user in DbBaseModel.createList<PersonModel>(cp, $"email={DbController.encodeSQLText(userEmail)}")) {
                var authTokenInfo = new AuthTokenInfoModel(cp, user);
                AuthTokenInfoModel.setVisitProperty(cp, authTokenInfo);
                EmailController.trySendPasswordReset(cp.core, user, authTokenInfo, ref userErrorMessage);
            }
        }
        //
        public override void sendPassword(string userEmail) {
            string userErrorMessage = "";
            sendPassword(userEmail, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendSystem(string EmailName, string AdditionalCopy, int AdditionalUserID, ref string adminErrorMessage) {
            EmailController.trySendSystemEmail(cp.core, false, EmailName, AdditionalCopy, AdditionalUserID, ref adminErrorMessage);
        }
        //
        public override void sendSystem(string EmailName, string AdditionalCopy, int AdditionalUserID) {
            EmailController.trySendSystemEmail(cp.core, false, EmailName, AdditionalCopy, AdditionalUserID);
        }
        //
        public override void sendSystem(string EmailName, string AdditionalCopy) {
            EmailController.trySendSystemEmail(cp.core, false, EmailName, AdditionalCopy);
        }
        //
        public override void sendSystem(string EmailName) {
            EmailController.trySendSystemEmail(cp.core, false, EmailName);
        }
        //
        //====================================================================================================
        //
        public override void sendSystem(int emailId, string additionalCopy, int additionalUserID, ref string adminErrorMessage) {
            EmailController.trySendSystemEmail(cp.core, false, emailId, additionalCopy, additionalUserID, ref adminErrorMessage);
        }
        //
        public override void sendSystem(int emailId, string additionalCopy, int additionalUserID) {
            EmailController.trySendSystemEmail(cp.core, false, emailId, additionalCopy, additionalUserID);
        }
        //
        public override void sendSystem(int emailId, string additionalCopy) {
            EmailController.trySendSystemEmail(cp.core, false, emailId, additionalCopy);
        }
        //
        public override void sendSystem(int emailId) {
            EmailController.trySendSystemEmail(cp.core, false, emailId);
        }
        //
        //====================================================================================================
        //
        public override void sendSystem(string emailName, string additionalCopy, int additionalUserID, ref string adminErrorMessage, bool sendImmediately) {
            EmailController.trySendSystemEmail(cp.core, sendImmediately, emailName, additionalCopy, additionalUserID, ref adminErrorMessage);
            return;
        }

        public override void sendSystem(string emailName, string additionalCopy, int additionalUserID, bool sendImmediately) {
            EmailController.trySendSystemEmail(cp.core, sendImmediately, emailName, additionalCopy, additionalUserID);
        }

        public override void sendSystem(string emailName, string additionalCopy, bool sendImmediately) {
            EmailController.trySendSystemEmail(cp.core, sendImmediately, emailName, additionalCopy);
        }

        public override void sendSystem(string emailName, bool sendImmediately) {
            EmailController.trySendSystemEmail(cp.core, sendImmediately, emailName);
        }
        //
        //====================================================================================================
        //
        public override void sendSystem(int emailId, string additionalCopy, int additionalUserID, ref string adminErrorMessage, bool sendImmediately) {
            EmailController.trySendSystemEmail(cp.core, sendImmediately, emailId, additionalCopy, additionalUserID, ref adminErrorMessage);
        }

        public override void sendSystem(int emailId, string additionalCopy, int additionalUserID, bool sendImmediately) {
            EmailController.trySendSystemEmail(cp.core, sendImmediately, emailId, additionalCopy, additionalUserID);
        }

        public override void sendSystem(int emailId, string additionalCopy, bool sendImmediately) {
            EmailController.trySendSystemEmail(cp.core, sendImmediately, emailId, additionalCopy);
        }

        public override void sendSystem(int emailId, bool sendImmediately) {
            EmailController.trySendSystemEmail(cp.core, sendImmediately, emailId);
        }
        //
        //====================================================================================================
        //
        public override void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string adminErrorMessage) {
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, new List<string> { groupName }, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, new List<string> { groupName }, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, new List<string> { groupName }, fromAddress, subject, body, sendImmediately, true, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(string groupName, string fromAddress, string subject, string body) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, new List<string> { groupName }, fromAddress, subject, body, true, true, ref adminErrorMessage, personalizeAddonId);
        }
        //
        //====================================================================================================
        //
        public override void sendGroup(int groupId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string adminErrorMessage) {
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, new List<int> { groupId }, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(int groupId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, new List<int> { groupId }, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(int groupId, string fromAddress, string subject, string body, bool sendImmediately) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, new List<int> { groupId }, fromAddress, subject, body, sendImmediately, true, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(int groupId, string fromAddress, string subject, string body) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, new List<int> { groupId }, fromAddress, subject, body, true, true, ref adminErrorMessage, personalizeAddonId);
        }
        //
        //====================================================================================================
        //
        public override void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string adminErrorMessage) {
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, groupNameList, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, groupNameList, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body, bool sendImmediately) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, groupNameList, fromAddress, subject, body, sendImmediately, true, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, groupNameList, fromAddress, subject, body, true, true, ref adminErrorMessage, personalizeAddonId);
        }
        //
        //====================================================================================================
        //
        public override void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string adminErrorMessage) {
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, groupIdList, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, groupIdList, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body, bool sendImmediately) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, groupIdList, fromAddress, subject, body, sendImmediately, true, ref adminErrorMessage, personalizeAddonId);
        }
        //
        public override void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body) {
            string adminErrorMessage = "";
            int personalizeAddonId = 0;
            EmailController.trySendGroupEmail(cp.core, groupIdList, fromAddress, subject, body, true, true, ref adminErrorMessage, personalizeAddonId);
        }
        //
        //====================================================================================================
        //
        public override void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string adminErrorMessage) {
            PersonModel person = DbBaseModel.create<PersonModel>(cp, toUserId);
            if (person == null) {
                adminErrorMessage = "An email could not be sent because the user could not be located.";
                return;
            }
            int personalizeAddonId = 0;
            EmailController.trySendPersonEmail(cp.core, "Ad Hoc Email", person, fromAddress, subject, body, "", "", sendImmediately, bodyIsHtml, 0, "", false, ref adminErrorMessage, personalizeAddonId);
        }
        //
        //====================================================================================================
        //
        public override void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string adminErrorMessage = "";
            sendUser(toUserId, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref adminErrorMessage);

        }
        //
        //====================================================================================================
        //
        public override void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately) {
            string adminErrorMessage = "";
            sendUser(toUserId, fromAddress, subject, body, sendImmediately, true, ref adminErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendUser(int toUserId, string fromAddress, string subject, string body) {
            string adminErrorMessage = "";
            sendUser(toUserId, fromAddress, subject, body, true, true, ref adminErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override bool validateEmail(string toAddress) {
            return EmailController.verifyEmailAddress(cp.core, toAddress);
        }
        //
        //====================================================================================================
        //
        public override bool validateUserEmail(int toUserId) {
            var user = DbBaseModel.create<PersonModel>(cp, toUserId);
            return EmailController.verifyEmailAddress(cp.core, user.email);
        }
        //
        //====================================================================================================
        /// <summary>
        /// deprecated. Use the integer toUserId method
        /// </summary>
        /// <param name="toUserId"></param>
        /// <param name="FromAddress"></param>
        /// <param name="Subject"></param>
        /// <param name="Body"></param>
        /// <param name="SendImmediately"></param>
        /// <param name="BodyIsHTML"></param>
        [Obsolete()]
        public override void sendUser(string toUserId, string FromAddress, string Subject, string Body, bool SendImmediately, bool BodyIsHTML) {
            if (GenericController.encodeInteger(toUserId) <= 0) throw new ArgumentException("The To-User argument is not valid, [" + toUserId + "]");
            sendUser(GenericController.encodeInteger(toUserId), FromAddress, Subject, Body, SendImmediately, BodyIsHTML);
        }
        //
        //====================================================================================================
        /// <summary>
        /// deprecated. Use the integer toUserId method
        /// </summary>
        /// <param name="toUserId"></param>
        /// <param name="FromAddress"></param>
        /// <param name="Subject"></param>
        /// <param name="Body"></param>
        /// <param name="SendImmediately"></param>
        [Obsolete()]
        public override void sendUser(string toUserId, string FromAddress, string Subject, string Body, bool SendImmediately) {
            if (GenericController.encodeInteger(toUserId) <= 0) throw new ArgumentException("The To-User argument is not valid, [" + toUserId + "]");
            sendUser(GenericController.encodeInteger(toUserId), FromAddress, Subject, Body, SendImmediately);
        }
        //
        //====================================================================================================
        /// <summary>
        /// deprecated. Use the integer toUserId method
        /// </summary>
        /// <param name="toUserId"></param>
        /// <param name="FromAddress"></param>
        /// <param name="Subject"></param>
        /// <param name="Body"></param>
        [Obsolete()]
        public override void sendUser(string toUserId, string FromAddress, string Subject, string Body) {
            if (GenericController.encodeInteger(toUserId) <= 0) throw new ArgumentException("The To-User argument is not valid, [" + toUserId + "]");
            sendUser(GenericController.encodeInteger(toUserId), FromAddress, Subject, Body);
        }
        //
        //====================================================================================================
        //
        #region  IDisposable Support 
        //
        //==========================================================================================
        //
        protected virtual void dispose(bool disposing_email) {
            if (!this.disposed_email) {
                if (disposing_email) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_email = true;
        }
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CPEmailClass() {
            dispose(false);
        }
        protected bool disposed_email;
        #endregion
    }

}