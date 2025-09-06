

using Contensive.BaseModels;
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Helper methods to perform common http requests
    /// </summary>
    public abstract class CPHttpBaseClass {
        //
        // ====================================================================================================
        /// <summary>
        /// Get url to a string. Use file objects to save to file
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public abstract string Get(string url);
        //
        // ====================================================================================================
        /// <summary>
        /// Post key/values to  a url. Use file objects to save to file
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestArguments"></param>
        /// <returns></returns>
        public abstract string Post(string url, System.Collections.Specialized.NameValueCollection requestArguments);
        //
        // ====================================================================================================
        /// <summary>
        /// post entity to a url. Use file objects to save to file
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public abstract string Post(string url);
        //
        //====================================================================================================
        /// <summary>
        /// The prefix used with database file field types to create a file link. Use to create links used on the website. For links in resources outside the websie like email, RSS, etc use FilePathAbsolute().
        /// For example, the file record may contain cctablename\imagefilename\000000000001\myPhoto.jpg,
        /// this method might return /files/mysite/ if the file system is local (files on the webserver),
        /// or it might return https://myCdnSource.com/publicfiles/ if the application is setup for remote files, and this is the cdn
        /// </summary>
        public abstract string CdnFilePathPrefix { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The prefix added to database file-field types to create a file link. Use to create links in resources outside the website like email and rss.
        /// For example, the file record may contain cctablename\imagefilename\000000000001\myPhoto.jpg,
        /// this method might return https://www.mywebsite.com/files/mysite/ if the filesystem is local (file on the webserver),
        /// or it might return https://myCdnSource.com/publicfiles/ if the application is setup for remote files, and this is the cdn.
        /// For local filesystems, the protocol is always https, and the domain comes from the domain in the server's config file, or can be overridden with the site property ""
        /// </summary>
        public abstract string CdnFilePathPrefixAbsolute { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The prefered protocol and domain to be used to call the website (or application server). 
        /// This value returns https:// + the primary domain name configured in the serverr appconfig.json 
        /// This can be overridden with the site property webAddressProtocolDomain on the Preferences page
        /// </summary>
        public abstract string WebAddressProtocolDomain { get; }
    }
}

