
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using System;

namespace Contensive.Processor {
    /// <summary>
    /// Text Messaging interface
    /// </summary>
    public class CPSMSClass : BaseClasses.CPSMSBaseClass, IDisposable {
        /// <summary>
        /// internal store
        /// </summary>
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPSMSClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send a text message. Returns false if message was not sent.
        /// </summary>
        /// <param name="smsPhoneNumber"></param>
        /// <param name="smsMessage"></param>
        /// <returns></returns>
        public override bool Send(string smsPhoneNumber, string smsMessage) {
            try {
                string userError = "";
                return TextMessageController.sendImmediate(cp.core, new TextMessageSendRequest {
                    textBody = smsMessage,
                    toPhone = smsPhoneNumber,
                    attempts = 0,
                    systemTextMessageId = 0,
                    groupTextMessageId = 0,
                    toMemberId = 0
                }, ref userError, "Send to " + smsPhoneNumber);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send a system Text Message. Return true on success else there was an error.
        /// </summary>
        /// <param name="systemTextMessageGuid"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        /// <returns></returns>
        public override bool SendSystem(string systemTextMessageGuid, string additionalCopy, int additionalUserID) {
            try {
                SystemTextMessageModel textMessage = DbBaseModel.create<SystemTextMessageModel>(cp, systemTextMessageGuid);
                string userErrorMessage = "";
                return TextMessageController.queueSystemTextMessage(cp.core, textMessage, additionalCopy, additionalUserID, ref userErrorMessage);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        #region  IDisposable Support 
        //
        //====================================================================================================
        /// <summary>
        /// dispose called from dispose() method only
        /// </summary>
        /// <param name="disposing_site"></param>
        protected virtual void Dispose(bool disposing_site) {
            if (!this.disposed_site) {
                if (disposing_site) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
            }
            this.disposed_site = true;
        }
        //
        //====================================================================================================
        /// <summary>
        /// flag to mark object disposes
        /// </summary>
        protected bool disposed_site;
        //
        //====================================================================================================
        /// <summary>
        /// Do not change or add Overridable to these methods..  Put cleanup code in Dispose(ByVal disposing As Boolean).
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        //====================================================================================================
        /// <summary>
        /// destructor to call dispose
        /// </summary>
        ~CPSMSClass() {
            Dispose(false);
        }
        #endregion
    }
}