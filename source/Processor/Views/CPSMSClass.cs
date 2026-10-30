
using Contensive.BaseModels;
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
        /// Send one text message immediately and return the result with the provider message id. See TextMessageController.sendImmediate for the pipeline.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override SmsSendResultModel SendText(SmsSendRequestModel request) {
            try {
                if (request == null) {
                    return new SmsSendResultModel { success = false, status = SmsSendStatusEnum.Failed, userError = "The text message request is empty." };
                }
                return TextMessageController.sendImmediate(cp.core, new TextMessageSendRequest {
                    textBody = request.body,
                    toPhone = request.toPhone,
                    attempts = 0,
                    systemTextMessageId = request.systemTextMessageId,
                    groupTextMessageId = 0,
                    toMemberId = request.toMemberId,
                    sentByMemberId = request.sentByMemberId,
                    source = request.source,
                    trackLinks = request.trackLinks,
                    isTest = request.isTest
                }, true);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return new SmsSendResultModel { success = false, status = SmsSendStatusEnum.Failed, userError = "Unexpected error sending the text message." };
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send a text message. Returns false if message was not sent. Wrapper over SendText.
        /// </summary>
        /// <param name="smsPhoneNumber"></param>
        /// <param name="smsMessage"></param>
        /// <returns></returns>
        [Obsolete("Use SendText(SmsSendRequestModel), which returns the send status, the provider message id and an error message.", false)]
        public override bool Send(string smsPhoneNumber, string smsMessage) {
            return SendText(new SmsSendRequestModel {
                toPhone = smsPhoneNumber,
                body = smsMessage,
                source = "cp.SMS.Send"
            }).success;
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
        public override bool SendSystem(string systemTextMessageGuid, string additionalCopy, int additionalUserID)
        {
            try
            {
                SystemTextMessageModel textMessage = DbBaseModel.create<SystemTextMessageModel>(cp, systemTextMessageGuid);
                if (textMessage == null)
                {
                    textMessage = DbBaseModel.verify<SystemTextMessageModel>(cp, systemTextMessageGuid);
                    textMessage.body = "";
                    textMessage.name = $"Text Message {textMessage.id}, created {textMessage.dateAdded}, for guid {systemTextMessageGuid}";
                    textMessage.save(cp);
                }
                string userErrorMessage = "";
                return TextMessageController.queueSystemTextMessage(cp.core, textMessage, additionalCopy, additionalUserID, ref userErrorMessage);
            }
            catch (Exception ex)
            {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send a text message to a user by their user id. Returns false with a userError if the user has no cell phone. Wrapper over SendText.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="smsMessage"></param>
        /// <param name="userError"></param>
        /// <returns></returns>
        [Obsolete("Use SendText(SmsSendRequestModel) with toMemberId set, which returns the send status, the provider message id and an error message.", false)]
        public override bool SendUser(int userId, string smsMessage, ref string userError) {
            try {
                string cellPhone = PersonModel.getCellPhone(cp, userId);
                if (string.IsNullOrWhiteSpace(cellPhone)) {
                    userError = $"User [{userId}] does not have a cell phone on file.";
                    return false;
                }
                SmsSendResultModel result = SendText(new SmsSendRequestModel {
                    toPhone = cellPhone,
                    toMemberId = userId,
                    body = smsMessage,
                    source = "cp.SMS.SendUser"
                });
                if (!result.success) { userError = result.userError; }
                return result.success;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// True if text messaging is configured and on for this site.
        /// </summary>
        public override bool IsEnabled => SmsController.isEnabled(cp.core);
        //
        //====================================================================================================
        /// <summary>
        /// Convert a phone number to E.164 with the site's default country code. Empty string if not valid.
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public override string NormalizePhone(string phone) {
            return SmsPhoneController.toE164(phone, TextMessageController.getDefaultCountryCode(cp.core));
        }
        //
        //====================================================================================================
        /// <summary>
        /// True if the phone is on the Text Message Block List. An invalid phone is not blocked.
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public override bool IsBlocked(string phone) {
            try {
                string e164 = NormalizePhone(phone);
                if (string.IsNullOrEmpty(e164)) { return false; }
                return TextMessageBlockListModel.isBlocked(cp, e164);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The character count, segment count and encoding of a message body.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public override SmsSegmentInfoModel GetSegmentInfo(string body) {
            return SmsSegmentController.getInfo(body);
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