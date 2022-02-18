
namespace Contensive.BaseClasses {
    /// <summary>
    /// text messaging (SNS) layer. Provide properties and methods to abstract the service for addons.
    /// </summary>
    public abstract class CPSMSBaseClass {
        //
        //==========================================================================================
        /// <summary>
        /// Send an text message with SMS to the phone numnber.
        /// </summary>
        /// <param name="smsPhoneNumber"></param>
        /// <param name="smsMessage"></param>
        /// <returns></returns>
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
    }
}

