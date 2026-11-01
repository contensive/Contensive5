using Contensive.BaseModels;
using System;

namespace Contensive.BaseClasses {
    /// <summary>
    /// text messaging layer. Provide properties and methods to abstract the service for addons.
    /// </summary>
    public abstract class CPSMSBaseClass {
        //
        //==========================================================================================
        /// <summary>
        /// Send one text message immediately. The phone number is normalized to E.164, the block list is applied, the message is logged in the Text Message Log, 
        /// and the provider message id is returned. Does not throw for expected failures, check result.success and result.userError.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public abstract SmsSendResultModel SendText(SmsSendRequestModel request);
        //
        //==========================================================================================
        /// <summary>
        /// Send an text message with SMS to the phone numnber.
        /// </summary>
        /// <param name="smsPhoneNumber"></param>
        /// <param name="smsMessage"></param>
        /// <returns></returns>
        [Obsolete("Use SendText(SmsSendRequestModel), which returns the send status, the provider message id and an error message.", false)]
        public abstract bool Send(string smsPhoneNumber, string smsMessage);
        //
        //==========================================================================================
        /// <summary>
        /// Send s system text message
        /// </summary>
        /// <param name="systemTextMessageGuid"></param>
        /// <param name="additionalCopy"></param>
        /// <param name="additionalUserID"></param>
        /// <returns></returns>
        public abstract bool SendSystem(string systemTextMessageGuid, string additionalCopy, int additionalUserID);
        //
        //==========================================================================================
        /// <summary>
        /// Send an SMS text message to a user by their user id. Returns false if the user has no cell phone on file.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="smsMessage"></param>
        /// <param name="userError"></param>
        /// <returns></returns>
        [Obsolete("Use SendText(SmsSendRequestModel) with toMemberId set, which returns the send status, the provider message id and an error message.", false)]
        public abstract bool SendUser(int userId, string smsMessage, ref string userError);
        //
        //==========================================================================================
        /// <summary>
        /// True if text messaging is configured and turned on for this site. If false, SendText returns status Disabled.
        /// </summary>
        public abstract bool IsEnabled { get; }
        //
        //==========================================================================================
        /// <summary>
        /// Convert a phone number in any format to E.164 (for example +17033039974) using the site's default country code. Returns an empty string if the number is not valid.
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public abstract string NormalizePhone(string phone);
        //
        //==========================================================================================
        /// <summary>
        /// True if the phone number is on the site's Text Message Block List. The phone can be in any format. An invalid phone is not blocked.
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public abstract bool IsBlocked(string phone);
        //
        //==========================================================================================
        /// <summary>
        /// Get the character count, segment count and encoding (GSM-7 or UCS-2) of a message body.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public abstract SmsSegmentInfoModel GetSegmentInfo(string body);
    }
}
