
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
                cp.Doc.SetProperty("SMS Phone Number", smsPhoneNumber);
                cp.Doc.SetProperty("SMS Message", smsMessage);
                return cp.Utils.EncodeBoolean(cp.Addon.Execute("{D7A7810E-ECF6-4ABE-A1C0-2188C2AD42AC}"));
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
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        //====================================================================================================
        /// <summary>
        /// destructor to call dispose
        /// </summary>
        ~CPSMSClass()  {
            Dispose(false);
        }
        #endregion
    }
}