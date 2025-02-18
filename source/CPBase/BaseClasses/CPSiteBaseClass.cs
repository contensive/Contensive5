
using System;

namespace Contensive.BaseClasses {
    //
    //====================================================================================================
    /// <summary>
    /// Application settings and methods
    /// </summary>
    public abstract class CPSiteBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// The html platform version for this site.
        /// replaces the site property html platform version
        /// htmlPlatform 0-4 is Bootstrap 4.
        /// htmlPlaform 5 is Bootstrap 5.
        /// </summary>
        public abstract int htmlPlatformVersion { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The application name
        /// </summary>
        public abstract string Name { get; }
        //
        //====================================================================================================
        /// <summary>
        /// remove the property
        /// </summary>
        /// <param name="key"></param>
        public abstract void ClearProperty(string key);
        //
        //====================================================================================================
        /// <summary>
        /// set a site-wide property. Read back with cp.site.GetText(), .getBoolean(), etc
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, string value);
        /// <summary>
        /// set a site-wide property. Read back with cp.site.GetText(), .getBoolean(), etc
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, bool value);
        /// <summary>
        /// set a site-wide property. Read back with cp.site.GetText(), .getBoolean(), etc
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, DateTime value);
        /// <summary>
        /// set a site-wide property. Read back with cp.site.GetText(), .getBoolean(), etc
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, int value);
        /// <summary>
        /// set a site-wide property. Read back with cp.site.GetText(), .getBoolean(), etc
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, double value);
        //
        //====================================================================================================
        /// <summary>
        /// Read a site property as a string. If the key is not set, sets and returns the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract string GetText(string key, string defaultValue);
        /// <summary>
        /// Read a site property as a string
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string GetText(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Read a site property as a boolean. If the key is not set, sets and returns the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key, bool defaultValue);
        /// <summary>
        /// Read a site property as a boolean.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Read a site property as a date. If the key is not set, sets and returns the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key, DateTime defaultValue);
        /// <summary>
        /// Read a site property as a date.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Read a site property as an integer. If the key is not set, sets and returns the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key, int defaultValue);
        /// <summary>
        /// Read a site property as an integer.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Read a site property as a double. If the key is not set, sets and returns the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key);
        /// <summary>
        /// Read a site property as a double.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key, double defaultValue);
        //
        //====================================================================================================
        /// <summary>
        /// The primary domain name for the application. Used for email links, monitoring, etc.
        /// </summary>
        public abstract string DomainPrimary { get; }
        //
        //====================================================================================================
        /// <summary>
        /// For a webpage hit, this is the current domain used, otherwise it is the primary domain.
        /// </summary>
        public abstract string Domain { get; }
        //
        //====================================================================================================
        /// <summary>
        /// A complete list of all domains supported.
        /// </summary>
        public abstract string DomainList { get; }
        //
        //====================================================================================================
        /// <summary>
        /// For websites, the default script page.
        /// </summary>
        public abstract string PageDefault { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Deprecated. Use LogActivity(subject, degtails, userId)
        /// </summary>
        /// <param name="details"></param>
        /// <param name="userId"></param>
        /// <param name="ignoreId"></param>
        [Obsolete("Use LogActivity(message, user)", false)] public abstract void LogActivity(string details, int userId, int ignoreId);
        //
        //====================================================================================================
        /// <summary>
        /// Add completed activity for online visit by current user, subject saved as message
        /// </summary>
        /// <param name="subject">A brief name for the activity to be shown in a list of other activities. Typically 'Call', 'Message', 'Meeting', 'Online Form', etc.</param>
        public abstract int  AddActivity(string subject);
        //
        //====================================================================================================
        /// <summary>
        /// Add completed activity for online visit by current user
        /// </summary>
        /// <param name="subject">A brief name for the activity to be shown in a list of other activities. Typically 'Call', 'Message', 'Meeting', 'Online Form', etc.</param>
        /// <param name="detailsText">The details of the activity, non-html plain text.</param>
        public abstract int  AddActivity(string subject, string detailsText);
        //
        //====================================================================================================
        /// <summary>
        /// Add completed activity for online visit by the selected user
        /// </summary>
        /// <param name="subject">A brief name for the activity to be shown in a list of other activities. Typically 'Call', 'Message', 'Meeting', 'Online Form', etc.</param>
        /// <param name="detailsText">The details of the activity, non-html plain text.</param>
        /// <param name="activityUserId"></param>
        public abstract int AddActivity(string subject, string detailsText, int activityUserId);
        //
        //====================================================================================================
        /// <summary>
        /// Add completed activity for specific activity type, by the selected user
        /// </summary>
        /// <param name="subject">A brief name for the activity to be shown in a list of other activities. Typically 'Call', 'Message', 'Meeting', 'Online Form', etc.</param>
        /// <param name="detailsText">The details of the activity, non-html plain text.</param>
        /// <param name="activityUserId"></param>
        /// <param name="typeId">see ActivityLogTypeEnum, 1=visit online, 2=email-to, 3=email-from, 4=call-to, 5=call-from, 6=text-to, 7=text-from, 8=meeting-video, 9=meeting-in-person</param>
        public abstract int AddActivity(string subject, string detailsText, int activityUserId, int typeId);
        //
        //====================================================================================================
        /// <summary>
        /// Add a schedule, non-completed activity for specific activity type, by the selected user
        /// </summary>
        /// <param name="subject">A brief name for the activity to be shown in a list of other activities. Typically 'Call', 'Message', 'Meeting', 'Online Form', etc.</param>
        /// <param name="detailsText">The details of the activity, non-html plain text.</param>
        /// <param name="activityUserId"></param>
        /// <param name="typeId">see ActivityLogTypeEnum, 1=online visit, 2=online purchase, 3=email-to, 4=email-from, 5=call-to, 6=call-from, 7=text-to, 8=text-from, 9=meeting-video, 10=meeting-in-person, </param>
        /// <param name="dateScheduled"></param>
        /// <param name="duration"></param>
        /// <param name="scheduledStaffId"></param>
        public abstract int AddActivity(string subject, string detailsText, int activityUserId, int typeId, DateTime dateScheduled, int duration, int scheduledStaffId);
        //
        //====================================================================================================
        /// <summary>
        /// Add a schedule, non-completed activity online-visit activity, by the selected user
        /// </summary>
        /// <param name="subject">A brief name for the activity to be shown in a list of other activities. Typically 'Call', 'Message', 'Meeting', 'Online Form', etc.</param>
        /// <param name="detailsText">The details of the activity, non-html plain text.</param>
        /// <param name="activityUserId"></param>
        /// <param name="dateScheduled"></param>
        /// <param name="duration"></param>
        /// <param name="scheduledStaffId"></param>
        [Obsolete("Use overload with activity-type", false)] 
        public abstract int AddActivity(string subject, string detailsText, int activityUserId, DateTime dateScheduled, int duration, int scheduledStaffId);
        //
        //====================================================================================================
        /// <summary>
        /// Report an error, logging it and sending notifications as the app is confirgured. Does not rethrow the error.
        /// </summary>
        /// <param name="message"></param>
        public abstract void ErrorReport(string message);
        /// <summary>
        /// Report an error, logging it and sending notifications as the app is confirgured. Does not rethrow the error.
        /// </summary>
        /// <param name="Ex"></param>
        public abstract void ErrorReport(System.Exception Ex);
        /// <summary>
        /// Report an error, logging it and sending notifications as the app is confirgured. Does not rethrow the error.
        /// </summary>
        /// <param name="Ex"></param>
        /// <param name="message"></param>
        public abstract void ErrorReport(System.Exception Ex, string message);
        //
        //====================================================================================================
        /// <summary>
        /// When debugging is true, add this message and timestamp to the debug trace.
        /// </summary>
        /// <param name="message"></param>
        public abstract void TestPoint(string message);
        //
        //====================================================================================================
        /// <summary>
        /// Add a site warning that must be removed manually. 
        /// Log a message to the site-warning log, which displays a Warning indicator in the admin site. 
        /// This should only be for issues related to content and settings that an admin user can address. 
        /// To log code issues, use cp.log. To set the server alarm, use cp.site.LogAlarm() .
        /// </summary>
        /// <param name="name">A short general description of the problem</param>
        /// <param name="description">A more detailed description that might include instructions how to fix the issue.</param>
        public abstract void SetSiteWarning(string name, string description);
        //
        //====================================================================================================
        /// <summary>
        /// Add or remove a site warning based on the boolean argument. 
        /// Log a message to the site-warning log, which displays a Warning indicator in the admin site. 
        /// This should only be for issues related to content and settings that an admin user can address. 
        /// To log code issues, use cp.log. To set the server alarm, use cp.site.LogAlarm() .
        /// </summary>
        /// <param name="name">A short general description of the problem</param>
        /// <param name="description">A more detailed description that might include instructions how to fix the issue.</param>
        /// <param name="addRemove">if true, the warning is added, else the warning is removed</param>
        public abstract void SetSiteWarning(string name, string description, bool addRemove);
        //
        //====================================================================================================
        /// <summary>
        /// Log an Alarm. Alarms are saved in the \Contensive\Alarms folder, and will automatically cause the server alarm to fail.
        /// </summary>
        /// <param name="cause"></param>
        public abstract void LogAlarm(string cause);
        //
        //====================================================================================================
        /// <summary>
        /// Add a link alias record for this page.
        /// </summary>
        /// <param name="linkAlias">The link alias to add.</param>
        /// <param name="pageId">the id of the page to be displayed by this link alias</param>
        /// <param name="queryStringSuffix">The query string to be added to the url that may effect how addons on the page render.</param>
        public abstract void AddLinkAlias(string linkAlias, int pageId, string queryStringSuffix);
        /// <summary>
        /// Add a link alias record for this page.
        /// </summary>
        /// <param name="linkAlias"></param>
        /// <param name="pageId"></param>
        public abstract void AddLinkAlias(string linkAlias, int pageId);
        //
        //====================================================================================================
        /// <summary>
        /// An addon can throw an Event that then executes other addons that bind to that event in their record. 
        /// For example, ecommerce throws an event 'fulfillment'. An item like a giftcard can be emailed on fulfillment which occurs differently on different types of accounts.
        /// </summary>
        /// <param name="eventNameIdOrGuid"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use ThrowEvent(int), ThrowEventByName(string), or ThrowEventByGuid(string)",false)] public abstract string ThrowEvent(string eventNameIdOrGuid);
        public abstract string ThrowEvent(int  eventId );
        public abstract string ThrowEventByName(string eventName);
        public abstract string ThrowEventByGuid(string eventGuid);
        //
        //====================================================================================================
        // deprecated
        //
        /// <summary>
        /// deprecated. Use CP.Http.CdnFilePathPrefix or CP.Http.CdnFilepathPrefixAbsolute
        /// </summary>
        [Obsolete("Use CP.Http.CdnFilePathPrefix or CP.Http.CdnFilepathPrefixAbsolute", false)]        public abstract string FilePath { get; }
        //
        /// <summary>
        /// deprecated. Use CP.Addon.InstallCollectionFile()
        /// </summary>
        /// <param name="privatePathFilename"></param>
        /// <param name="returnUserError"></param>
        /// <returns></returns>
        [Obsolete("Use CP.Addon.InstallCollectionFile()", false)]        public abstract bool installCollectionFile(string privatePathFilename, ref string returnUserError);
        //
        /// <summary>
        /// Use CP.Addon.InstallCollectionFromLibrary()
        /// </summary>
        /// <param name="collectionGuid"></param>
        /// <param name="returnUserError"></param>
        /// <returns></returns>
        [Obsolete("Use CP.Addon.InstallCollectionFromLibrary()", false)]        public abstract bool installCollectionFromLibrary(string collectionGuid, ref string returnUserError);
        /// <summary>
        /// Use correct defaultValue type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use correct defaultValue type",true)]        public abstract bool GetBoolean(string key, string defaultValue);
        /// <summary>
        /// Use correct defaultValue type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use correct defaultValue type", false)]        public abstract DateTime GetDate(string key, string defaultValue);
        /// <summary>
        /// Use correct defaultValue type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use correct defaultValue type", false)]        public abstract int GetInteger(string key, string defaultValue);
        /// <summary>
        /// Use correct defaultValue type
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use correct defaultValue type", false)]        public abstract double GetNumber(string key, string defaultValue);
        /// <summary>
        /// Use GetText()
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [Obsolete("Use GetText()", false)]        public abstract string GetProperty(string key, string value);
        /// <summary>
        /// Use GetText()
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Obsolete("Use GetText()", false)]        public abstract string GetProperty(string key);
        /// <summary>
        /// Deprecated
        /// </summary>
        [Obsolete("Deprecated", false)]        public abstract bool MultiDomainMode { get; }
        /// <summary>
        /// Deprecated, please use cp.cdnFiles, cp.privateFiles, cp.WwwFiles, or cp.TempFiles instead.
        /// </summary>
        [Obsolete("Deprecated, please use cp.cdnFiles, cp.privateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]        public abstract string PhysicalFilePath { get; }
        /// <summary>
        /// Deprecated, please use cp.cdnFiles, cp.privateFiles, cp.WwwFiles, or cp.TempFiles instead.
        /// </summary>
        [Obsolete("Deprecated, please use cp.cdnFiles, cp.privateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]        public abstract string PhysicalInstallPath { get; }
        /// <summary>
        /// Deprecated, please use cp.cdnFiles, cp.privateFiles, cp.WwwFiles, or cp.TempFiles instead.
        /// </summary>
        [Obsolete("Deprecated, please use cp.cdnFiles, cp.privateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]        public abstract string PhysicalWWWPath { get; }
        /// <summary>
        /// Deprecated
        /// </summary>
        [Obsolete("Deprecated", false)]        public abstract bool TrapErrors { get; }
        /// <summary>
        /// Deprecated. This was the url path to the application for virtually hosted sites. Should be a blank.
        /// </summary>
        [Obsolete("Deprecated. This was the url path to the application for virtually hosted sites. Should be a blank.", false)]        public abstract string AppPath { get; }
        /// <summary>
        /// Deprecated. This was the url path to the application for virtually hosted sites. Should be a blank.
        /// </summary>
        [Obsolete("Deprecated. This was the url path to the application for virtually hosted sites. Should be a blank.", false)]        public abstract string AppRootPath { get; }
        /// <summary>
        /// Deprecated. This was a slash followed by the application name.
        /// </summary>
        [Obsolete("Deprecated. This was a slash followed by the application name.", false)]        public abstract string VirtualPath { get; }
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <returns></returns>
        [Obsolete("Deprecated.", false)]        public abstract bool IsTesting();
        /// <summary>
        /// Use CP.Utils.ExportCsv()
        /// </summary>
        /// <param name="command"></param>
        /// <param name="SQL"></param>
        /// <param name="exportName"></param>
        /// <param name="filename"></param>
        [Obsolete("Use CP.Utils.ExportCsv()", false)]        public abstract void RequestTask(string command, string SQL, string exportName, string filename);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated.", false)]        public abstract int LandingPageId(string domainName);
        /// <summary>
        /// Deprecated
        /// </summary>
        /// <returns></returns>
        [Obsolete("Deprecated.", false)]        public abstract int LandingPageId();
        /// <summary>
        /// Use CP.Utils.EncodeAppRootPath()
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        [Obsolete("Use CP.Utils.EncodeAppRootPath()", false)]        public abstract string EncodeAppRootPath(string link);
        //
        //====================================================================================================
        /// <summary>
        /// Log a message to the site-warning log.The site warning log lets admins know there are issues they need to resolve. This is not code bugs, but content or setting relalted issues.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="typeOfWarningKey"></param>
        /// <param name="instanceKey"></param>
        [Obsolete("Use CP.Site.SetSiteWarning()", false)] public abstract void LogWarning(string name, string description, string typeOfWarningKey, string instanceKey);
    }
}
