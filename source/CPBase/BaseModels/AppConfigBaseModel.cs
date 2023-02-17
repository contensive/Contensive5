
using System;
using System.Collections.Generic;

namespace Contensive.BaseModels {
    //
    //====================================================================================================
    /// <summary>
    /// Configuration of an application
    /// - new() - to allow deserialization (so all methods must pass in cp)
    /// - shared getObject( cp, id ) - returns loaded model
    /// - saveObject( cp ) - saves instance properties, returns the record id
    /// </summary>
    public abstract  class AppConfigBaseModel {
        /// <summary>
        /// name for the app. Must be unique within the server group. Difficulate to change later.
        /// </summary>
        public abstract string name { get; set; }
        /// <summary>
        /// status used to signal that the app is ok. See AppStatusEnum for values
        /// </summary>
        public abstract AppStatusEnum appStatus { get; set; }
        /// <summary>
        /// when false, app throws exception
        /// </summary>
        public abstract bool enabled { get; set; }
        /// <summary>
        /// key used for all encoding, two=way and one-way encoding. 
        /// </summary>
        public abstract string privateKey { get; set; }
        /// <summary>
        /// if the privateKey decoding fails and this key is not blank, an attempt is made with this key on reads.
        /// When changing keys, put the old key here. For two-way encoding, read will use fallback, and written back primary.
        /// For one-way, if primary fails, attempt secondary.
        /// </summary>
        public abstract string privateKeyFallBack { get; set; }
        /// <summary>
        /// local abs path to wwwroot. Paths end in slash. (i.e. d:\inetpub\myApp\www\)
        /// </summary>
        public abstract string localWwwPath { get; set; }
        /// <summary>
        /// local file path to the content files. Paths end in slash. (i.e. d:\inetpub\myApp\files\)
        /// </summary>
        public abstract string localFilesPath { get; set; }
        /// <summary>
        /// local file path to the content files. Paths end in slash. (i.e. d:\inetpub\myApp\private\)
        /// </summary>
        public abstract string localPrivatePath { get; set; }
        /// <summary>
        /// temp file storage, files used by just one process, scope just during rendering life. Paths end in slash. (i.e. d:\inetpub\myApp\temp\)
        /// </summary>
        public abstract string localTempPath { get; set; }
        /// <summary>
        /// path within AWS S3 bucket where www files are stored
        /// </summary>
        public abstract string remoteWwwPath { get; set; }
        /// <summary>
        /// path within AWS S3 bucket where cdn files are stored.
        /// in some cases (like legacy), cdnFiles are in an iis virtual folder mapped to appRoot (like /appName/files/). Some cases this is a URL (http:\\cdn.domain.com pointing to s3)
        /// </summary>
        public abstract string remoteFilePath { get; set; }
        /// <summary>
        /// path within AWS S3 bucket where private files are stored.
        /// </summary>
        public abstract string remotePrivatePath { get; set; }
        /// <summary>
        /// url for cdn files (for upload files, etc). For local files is can be /appname/files/) for remote cdn, it includes protocol-host
        /// </summary>
        public abstract string cdnFileUrl { get; set; }
        /// <summary>
        /// set true to be included in server monitor testing
        /// </summary>
        public abstract bool allowSiteMonitor { get; set; }
        /// <summary>
        /// domain(s) for the app. primary domain is the first item in the list
        /// </summary>
        public abstract List<string> domainList { get; set; }
        /// <summary>
        /// route to admin site. The url pathpath that executes the addon site
        /// </summary>
        public abstract string adminRoute { get; set; }
        /// <summary>
        /// when exeecuting iis, this is the default page.
        /// </summary>
        public abstract string defaultPage { get; set; }
        /// <summary>
        /// if true, the command line delete cannot delete this app
        /// </summary>
        public abstract bool deleteProtection { get; set; }
        /// <summary>
        /// Emails per month. if 0 unlimited
        /// </summary>
        public abstract int emailLimit { get; set; }
        /// <summary>
        /// limit to members (accounts with membership, users, ?)
        /// </summary>
        public abstract int memberLimit { get; set; }
        /// <summary>
        /// limit to the number of content managers
        /// </summary>
        public abstract int adminLimit { get; set; }
        /// <summary>
        /// if serverconfig.useSecretManager if true, this is not used.
        /// if false, app secrets are stored here.
        /// values stored in secrets override properties in server and app config.
        /// </summary>
        public abstract List<NameValueBaseModel> secrets { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// status of the app in the appConfigModel. Only applies to the app loaded in the serverstatus.appconfig
        /// </summary>
        public enum AppModeEnum {
            /// <summary>
            /// app running normal
            /// </summary>
            normal = 0,
            /// <summary>
            /// app paused
            /// </summary>
            maintainence = 1
        }
        //
        //====================================================================================================
        /// <summary>
        /// status of the app in the appConfigModel. Only applies to the app loaded in the serverstatus.appconfig
        /// </summary>
        public enum AppStatusEnum {
            /// <summary>
            /// app status ok
            /// </summary>
            ok = 0,
            /// <summary>
            /// app status not ok
            /// </summary>
            maintenance = 1
        }
    }
}

