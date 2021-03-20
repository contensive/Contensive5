
using System;
using Contensive.Processor.Controllers;
using System.Collections.Generic;
using Contensive.Models.Db;

namespace Contensive.Processor {
    public class CPEmailClass : BaseClasses.CPEmailBaseClass, IDisposable {
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
        public override void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string userErrorMessage) {
            try {
                EmailController.queueAdHocEmail(cp.core, "Ad Hoc email from api", 0, toAddress, fromAddress, subject, body, fromAddress, fromAddress, "", sendImmediately, bodyIsHTML,0 , ref userErrorMessage);
            } catch (Exception ex) {
                LogController.logError( cp.core,ex);
                throw;
            }
        }
        //
        public override void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml ) {
            string userErrorMessage = "";
            send(toAddress, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);
        }
        //
        public override void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately) {
            string userErrorMessage = "";
            send(toAddress, fromAddress, subject, body, sendImmediately, true, ref userErrorMessage);
        }
        //
        public override void send(string toAddress, string fromAddress, string subject, string body) {
            string userErrorMessage = "";
            send(toAddress, fromAddress, subject, body, true, true, ref userErrorMessage);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send submitted form within an email
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="fromAddress"></param>
        /// <param name="subject"></param>
        /// <param name="userErrorMessage"></param>
        public override void sendForm(string toAddress, string fromAddress, string subject, ref string userErrorMessage) {
            try {
                EmailController.tryQueueFormEmail(cp.core, toAddress, fromAddress, subject, out string errorMessage);
                userErrorMessage = errorMessage;
            } catch (Exception ex) {
                LogController.logError( cp.core,ex);
                throw;
            }
        }
        //
        public override void sendForm(string toAddress, string fromAddress, string subject) {
            string userErrorMessage = "";
            sendForm(toAddress, fromAddress, subject, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendPassword(string UserEmailAddress, ref string userErrorMessage) {
            PasswordRecoveryController.sendPassword(cp.core, UserEmailAddress, ref userErrorMessage);
        }
        //
        public override void sendPassword(string UserEmailAddress) {
            string userErrorMessage = "";
            sendPassword(UserEmailAddress, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendSystem(string EmailName, string AdditionalCopy, int AdditionalUserID, ref string userErrorMessage) {
            EmailController.tryQueueSystemEmail(cp.core, EmailName, AdditionalCopy, AdditionalUserID, ref userErrorMessage);
        }
        //
        public override void sendSystem(string EmailName, string AdditionalCopy, int AdditionalUserID) {
            EmailController.tryQueueSystemEmail(cp.core, EmailName, AdditionalCopy, AdditionalUserID);
        }
        //
        public override void sendSystem(string EmailName, string AdditionalCopy) {
            EmailController.tryQueueSystemEmail(cp.core, EmailName, AdditionalCopy);
        }
        //
        public override void sendSystem(string EmailName) {
            EmailController.tryQueueSystemEmail(cp.core, EmailName);
        }
        //
        //====================================================================================================
        //
        public override void sendSystem(int emailId, string additionalCopy, int additionalUserID, ref string userErrorMessage) {
            EmailController.tryQueueSystemEmail(cp.core, emailId, additionalCopy, additionalUserID);
        }
        //
        public override void sendSystem(int emailId, string additionalCopy, int additionalUserID) {
            EmailController.tryQueueSystemEmail(cp.core, emailId, additionalCopy, additionalUserID);
        }
        //
        public override void sendSystem(int emailId, string additionalCopy) {
            EmailController.tryQueueSystemEmail(cp.core, emailId, additionalCopy);
        }
        //
        public override void sendSystem(int emailId) {
            EmailController.tryQueueSystemEmail(cp.core, emailId);
        }
        //
        //====================================================================================================
        //
        public override void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string userErrorMessage) {
            EmailController.tryQueueGroupEmail(cp.core, new List<string> { groupName }, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);
        }
        //
        public override void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, new List<string> { groupName }, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);
        }
        //
        public override void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, new List<string> { groupName }, fromAddress, subject, body, sendImmediately, true, ref userErrorMessage);
        }
        //
        public override void sendGroup(string groupName, string fromAddress, string subject, string body) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, new List<string> { groupName }, fromAddress, subject, body, true, true, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendGroup(int groupId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string userErrorMessage) {
            EmailController.tryQueueGroupEmail(cp.core, new List<int> { groupId }, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);
        }
        //
        public override void sendGroup(int groupId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, new List<int> { groupId }, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);
        }
        //
        public override void sendGroup(int groupId, string fromAddress, string subject, string body, bool sendImmediately) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, new List<int> { groupId }, fromAddress, subject, body, sendImmediately, true, ref userErrorMessage);
        }
        //
        public override void sendGroup(int groupId, string fromAddress, string subject, string body) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, new List<int> { groupId }, fromAddress, subject, body, true, true, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string userErrorMessage) {
            EmailController.tryQueueGroupEmail(cp.core, groupNameList, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);
        }
        //
        public override void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, groupNameList, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);
        }
        //
        public override void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body, bool sendImmediately) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, groupNameList, fromAddress, subject, body, sendImmediately, true, ref userErrorMessage);
        }
        //
        public override void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, groupNameList, fromAddress, subject, body, true, true, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string userErrorMessage) {
            EmailController.tryQueueGroupEmail(cp.core, groupIdList, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);
        }
        //
        public override void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, groupIdList, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);
        }
        //
        public override void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body, bool sendImmediately) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, groupIdList, fromAddress, subject, body, sendImmediately, true, ref userErrorMessage);
        }
        //
        public override void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body) {
            string userErrorMessage = "";
            EmailController.tryQueueGroupEmail(cp.core, groupIdList, fromAddress, subject, body, true, true, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml, ref string userErrorMessage) {
            PersonModel person = DbBaseModel.create<PersonModel>(cp, toUserId);
            if ( person == null ) {
                userErrorMessage = "An email could not be sent because the user could not be located.";
                return;
            }
            EmailController.tryQueuePersonEmail(cp.core, "Ad Hoc Email", person, fromAddress, subject, body, "", "", sendImmediately, bodyIsHtml, 0,"",false, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHtml) {
            string userErrorMessage = "";
            sendUser(toUserId, fromAddress, subject, body, sendImmediately, bodyIsHtml, ref userErrorMessage);

        }
        //
        //====================================================================================================
        //
        public override void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately) {
            string userErrorMessage = "";
            sendUser(toUserId, fromAddress, subject, body, sendImmediately, true, ref userErrorMessage);
        }
        //
        //====================================================================================================
        //
        public override void sendUser(int toUserId, string fromAddress, string subject, string body) {
            string userErrorMessage = "";
            sendUser(toUserId, fromAddress, subject, body, true, true, ref userErrorMessage);
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
        [Obsolete()] public override void sendUser(string toUserId, string FromAddress, string Subject, string Body, bool SendImmediately, bool BodyIsHTML) {
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
        [Obsolete()] public override void sendUser(string toUserId, string FromAddress, string Subject, string Body, bool SendImmediately) {
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
        [Obsolete()] public override void sendUser(string toUserId, string FromAddress, string Subject, string Body) {
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
        protected virtual void Dispose(bool disposing_email) {
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
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPEmailClass()  {
            Dispose(false);
        }
        protected bool disposed_email;
        #endregion
    }

}