
using System;
using System.IO;
using System.Net;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class EmailHippoController : IDisposable {
        //
        // ----- constants
        //
        /*
 *******************************************************************************************
 * Company:
 * © 2017, Email Hippo Limited. (http://emailhippo.com)
 *
 * File name:
 * API V3 - C# Example.cs
 *
 * Version:
 * 1.0.20170508.0
 *
 * Version control:
 * - 1.0.20170508.0 - initial release
 *
 * Date:
 * May 2017
 *
 * Description:
 * Demonstrates how to call a RESTful service @ //api.hippoapi.com/v3/more/json
 * using C#.
 *
 * This example requires a valid key to work correctly.
 *
 * License:
 * Apache 2.0 (https://www.apache.org/licenses/LICENSE-2.0)
 *******************************************************************************************
*/
        /// <summary>
        /// The api url.
        /// </summary>
        private const string ApiUrl = @"https://api.hippoapi.com/v3/more/json";
        /// <summary>
        /// 0 = ApiUrl
        /// 1 = API Key
        /// 2 = Email address to query
        /// </summary>
        private const string QueryFormatString = @"{0}/{1}/{2}";
        /// <summary>
        /// The your api key.
        /// </summary>
        private const string YourAPIKey = @"3A9124FB";
        //
        private static string ValidateEmail(CoreController core, string emailAddress) {
           var requestUrl = string.Format(QueryFormatString, ApiUrl, YourAPIKey, emailAddress);
            var myRequest = (HttpWebRequest)WebRequest.Create(requestUrl);
            using( WebResponse webResponse = myRequest.GetResponse() ) {
                try {
                    using (var reader = new StreamReader(webResponse.GetResponseStream())) {
                        return reader.ReadToEnd();
                    }
                } catch (Exception exception) {
                    LogController.logError(core, exception);
                    return string.Empty;
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed;
        //
        public void Dispose()  {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~EmailHippoController()  {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);


        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }
    //
}