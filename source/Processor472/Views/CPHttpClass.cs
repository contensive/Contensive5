
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Contensive.BaseClasses;
using Contensive.BaseModels;
using Contensive.Processor.Controllers;

namespace Contensive.Processor {
    public class CPHttpClass : BaseClasses.CPHttpBaseClass {
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        // ====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cpParent"></param>
        //
        public CPHttpClass(CPClass cpParent) {
            cp = cpParent;
        }
        //
        // ====================================================================================================
        //
        public override string CdnFilePathPrefix {
            get {
                return HttpController.getCdnFilePathPrefix(cp.core);
            }
        }
        //
        // ====================================================================================================
        //
        public override string CdnFilePathPrefixAbsolute {
            get {
                return HttpController.getCdnFilePathPrefixAbsolute(cp.core);
            }
        }
        //
        // ====================================================================================================
        //
        public override string WebAddressProtocolDomain {
            get {
                return HttpController.getWebAddressProtocolDomain(cp.core);
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Simple http get of a url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public override string Get(string url) {
            HttpController httpRequest = new HttpController();
            return httpRequest.getURL(url);
        }
        //
        // ====================================================================================================
        //
        public override string Post(string url, System.Collections.Specialized.NameValueCollection requestArguments) {
            HttpController httpRequest = new HttpController();
            return httpRequest.postUrl(url, requestArguments);
        }
        //
        // ====================================================================================================
        //
        public override string Post(string url) {
            HttpController httpRequest = new HttpController();
            return httpRequest.postUrl(url);
        }
    }
}