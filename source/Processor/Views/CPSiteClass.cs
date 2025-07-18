﻿using Amazon.SecretsManager.Extensions.Caching;
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;

namespace Contensive.Processor {
    //
    public class CPSiteClass : BaseClasses.CPSiteBaseClass, IDisposable {
        //
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        //
        public CPSiteClass(CPClass cp) {
            this.cp = cp;
        }
        ////
        ////=======================================================================================================
        ///// <summary>
        ///// get a secret value
        ///// if useSecretManager, get value from secretManager
        ///// else get from matching property, if not match, from local secrets store
        ///// </summary>
        ///// <param name="secretName"></param>
        //public override string GetSecret(string secretName) {
        //    if (cp.core.serverConfig.useSecretManager) {
        //        //
        //        // -- use secret manager
        //        return SecretManagerController.getSecret(cp.core, secretName);
        //    }
        //    //
        //    // -- use local secrets and secret properties
        //    switch (secretName) {
        //        case "defaultdatasourceaddress": {
        //                return cp.core.serverConfig.defaultDataSourceAddress;
        //            }
        //        case "defaultdatasourceusername": {
        //                return cp.core.serverConfig.defaultDataSourceUsername;
        //            }
        //        case "defaultdatasourcepassword": {
        //                return cp.core.serverConfig.defaultDataSourcePassword;
        //            }
        //        case "defaultdatasourcetype": {
        //                return cp.core.serverConfig.defaultDataSourceType.ToString();
        //            }
        //        case "defaultdatasourcesecure": {
        //                return cp.core.serverConfig.defaultDataSourceSecure.ToString();
        //            }
        //        case "awsaccesskey": {
        //                return cp.core.serverConfig.awsAccessKey;
        //            }
        //        case "awssecretaccesskey": {
        //                return cp.core.serverConfig.awsSecretAccessKey;
        //            }
        //        case "awsregionname": {
        //                return cp.core.serverConfig.awsRegionName;
        //            }
        //        case "awsbucketname": {
        //                return cp.core.serverConfig.awsBucketName;
        //            }
        //        default: {
        //                //
        //                // -- use local secrets, didnt match property
        //                BaseModels.NameValueBaseModel result = cp.core.appConfig.secrets.Find(x => x.name.ToLowerInvariant() == secretName.ToLowerInvariant());
        //                if (result == null) { return ""; }
        //                return result.value;
        //            }
        //    }
        //}
        ////
        ////=======================================================================================================
        ///// <summary>
        ///// set a secret value
        ///// if useSecretManager, set it there
        ///// else if property match, set the match
        ///// else set in local secrets
        ///// </summary>
        ///// <param name="secretName"></param>
        //public override void SetSecret(string secretName, string secretValue) {
        //    if (cp.core.serverConfig.useSecretManager) {
        //        //
        //        // -- use secret manager
        //        SecretManagerController.setSecret(cp.core, secretName, secretValue);
        //        return;
        //    }
        //    SecretManagerController.setSecret(cp.core, secretName, secretValue);
        //    return;
        //    ////
        //    //// -- use local configuration
        //    //switch (secretName) {
        //    //    case "defaultdatasourceaddress": {
        //    //            cp.core.serverConfig.defaultDataSourceAddress = secretValue;
        //    //            break;
        //    //        }
        //    //    case "defaultdatasourceusername": {
        //    //            cp.core.serverConfig.defaultDataSourceUsername = secretValue;
        //    //            break;
        //    //        }
        //    //    case "defaultdatasourcepassword": {
        //    //            cp.core.serverConfig.defaultDataSourcePassword = secretValue;
        //    //            break;
        //    //        }
        //    //    case "defaultdatasourcetype": {
        //    //            cp.core.serverConfig.defaultDataSourceType = (BaseModels.ServerConfigBaseModel.DataSourceTypeEnum)cp.Utils.EncodeInteger(secretValue);
        //    //            break;
        //    //        }
        //    //    case "defaultdatasourcesecure": {
        //    //            cp.core.serverConfig.defaultDataSourceSecure = cp.Utils.EncodeBoolean(secretValue);
        //    //            break;
        //    //        }
        //    //    case "awsaccesskey": {
        //    //            cp.core.serverConfig.awsAccessKey = secretValue;
        //    //            break;
        //    //        }
        //    //    case "awssecretaccesskey": {
        //    //            cp.core.serverConfig.awsSecretAccessKey = secretValue;
        //    //            break;
        //    //        }
        //    //    case "awsregionname": {
        //    //            cp.core.serverConfig.awsRegionName = secretValue;
        //    //            break;
        //    //        }
        //    //    case "awsbucketname": {
        //    //            cp.core.serverConfig.awsBucketName = secretValue;
        //    //            break;
        //    //        }
        //    //    default: {
        //    //            SecretManagerController.setSecret(cp.core, secretName, secretValue);
        //    //            break;
        //    //        }
        //    //}
        //}
        //
        //=======================================================================================================
        /// <summary>
        /// Clear a property
        /// </summary>
        /// <param name="key"></param>
        public override void ClearProperty(string key) {
            cp.core.siteProperties.clearProperty(key);
        }
        //
        //====================================================================================================
        //
        public override int htmlPlatformVersion {
            get {
                return cp.core.siteProperties.htmlPlatformVersion;
            }
        }
        //
        //====================================================================================================
        //
        public override string Name {
            get {
                return cp.core.appConfig.name;
            }
        }
        //
        //====================================================================================================
        //
        public override void AddLinkAlias(string linkAliasPhrase, int pageId, string queryStringSuffix) => LinkAliasController.addLinkAlias(cp.core, linkAliasPhrase, pageId, queryStringSuffix);
        //
        public override void AddLinkAlias(string linkAliasPhrase, int pageId) => LinkAliasController.addLinkAlias(cp.core, linkAliasPhrase, pageId, "");
        //
        //====================================================================================================
        //
        public override void SetProperty(string key, string value) {
            cp.core.siteProperties.setProperty(key, value);
        }
        //
        //====================================================================================================
        //
        public override void SetProperty(string key, bool value) {
            cp.core.siteProperties.setProperty(key, value);
        }
        //
        //====================================================================================================
        //
        public override void SetProperty(string key, DateTime value) {
            cp.core.siteProperties.setProperty(key, value);
        }
        //
        //====================================================================================================
        //
        public override void SetProperty(string key, int value) {
            cp.core.siteProperties.setProperty(key, value);
        }
        //
        //====================================================================================================
        //
        public override void SetProperty(string key, double value) {
            cp.core.siteProperties.setProperty(key, value);
        }
        //
        //====================================================================================================
        //
        public override bool GetBoolean(string key, bool defaultValue) {
            return cp.core.siteProperties.getBoolean(key, defaultValue);
        }
        public override bool GetBoolean(string key) {
            return cp.core.siteProperties.getBoolean(key, default);
        }
        //
        //====================================================================================================
        //
        public override DateTime GetDate(string key, DateTime DefaultValue) {
            return cp.core.siteProperties.getDate(key, DefaultValue);
        }
        public override DateTime GetDate(string key) {
            return cp.core.siteProperties.getDate(key, default);
        }
        //
        //====================================================================================================
        //
        public override int GetInteger(string key, int DefaultValue) {
            return cp.core.siteProperties.getInteger(key, DefaultValue);
        }
        public override int GetInteger(string key) {
            return cp.core.siteProperties.getInteger(key, default);
        }
        //
        //====================================================================================================
        //
        public override double GetNumber(string key, double DefaultValue) {
            return cp.core.siteProperties.getNumber(key, DefaultValue);
        }
        public override double GetNumber(string key) {
            return cp.core.siteProperties.getNumber(key, default);
        }
        //
        //====================================================================================================
        //
        public override string GetText(string key, string DefaultValue) {
            return cp.core.siteProperties.getText(key, DefaultValue);
        }
        public override string GetText(string key) {
            return cp.core.siteProperties.getText(key, default);
        }
        //
        //====================================================================================================
        //
        public override string DomainPrimary {
            get {
                string tempDomainPrimary = null;
                tempDomainPrimary = "";
                if (cp.core.appConfig.domainList.Count > 0) {
                    tempDomainPrimary = cp.core.appConfig.domainList[0];
                }
                return tempDomainPrimary;
            }
        }
        //
        //====================================================================================================
        //
        public override string Domain {
            get {
                return cp.core.webServer.requestDomain;
            }
        }
        //
        //====================================================================================================
        //
        public override string DomainList {
            get {
                return string.Join(",", cp.core.appConfig.domainList);
            }
        }
        //
        //====================================================================================================
        //
        public override string PageDefault {
            get {
                return cp.core.siteProperties.serverPageDefault;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="activityDetails"></param>
        /// <param name="userID"></param>
        /// <param name="organizationId"></param>
        public override int AddActivity(string subject) {
            return LogController.addActivityCompletedVisit(cp.core, subject, subject);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="detailsText"></param>
        /// <param name="userID"></param>
        /// <param name="organizationId"></param>
        public override int AddActivity(string subject, string detailsText) {
            return LogController.addActivityCompletedVisit(cp.core, subject, detailsText);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="detailsText"></param>
        /// <param name="activityUserId"></param>
        /// <param name="organizationId"></param>
        public override int AddActivity(string subject, string detailsText, int activityUserId) {
            return LogController.addActivityCompletedVisit(cp.core, subject, detailsText, activityUserId);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="detailsText"></param>
        /// <param name="activityUserId"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override int AddActivity(string subject, string detailsText, int activityUserId, int typeId) {
            return LogController.addActivityCompleted(cp.core, subject, detailsText, activityUserId, typeId);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="detailsText"></param>
        /// <param name="activityUserID"></param>
        /// <param name="dateScheduled"></param>
        /// <param name="duration"></param>
        /// <param name="scheduledStaffId"></param>
        [Obsolete("Use overload with activity-type", false)]
        public override int AddActivity(string subject, string detailsText, int activityUserID, DateTime dateScheduled, int duration, int scheduledStaffId) {
            return LogController.addActivityScheduled(cp.core, subject, detailsText, activityUserID, dateScheduled, duration, scheduledStaffId);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="detailsText"></param>
        /// <param name="activityUserId"></param>
        /// <param name="typeId"></param>
        /// <param name="dateScheduled"></param>
        /// <param name="duration"></param>
        /// <param name="scheduledStaffId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override int AddActivity(string subject, string detailsText, int activityUserId, int typeId, DateTime dateScheduled, int duration, int scheduledStaffId) {
            return LogController.addActivityScheduled(cp.core, subject, detailsText, activityUserId, typeId, dateScheduled, duration, scheduledStaffId);
        }

        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="activityMessage"></param>
        /// <param name="activityUserID"></param>
        /// <param name="legacyOrganizationId"></param>
        [Obsolete]
        public override void LogActivity(string activityMessage, int activityUserID, int legacyOrganizationId) {
            LogController.addActivityCompletedVisit(cp.core, "", activityMessage, activityUserID);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="typeOfWarningKey"></param>
        /// <param name="instanceKey"></param>
        public override void SetSiteWarning(string name, string description) {
            LogController.setSiteWarning(cp.core, name, description, true);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="typeOfWarningKey"></param>
        /// <param name="instanceKey"></param>
        /// <param name="addRemove">if true add the warning, else remove it</param>
        public override void SetSiteWarning(string name, string description, bool addRemove) {
            LogController.setSiteWarning(cp.core, name, description, addRemove);
        }


        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cause"></param>
        public override void LogAlarm(string cause) {
            LogController.logAlarm(cp.core, cause);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public override void ErrorReport(string message) {
            LogController.log(cp.core, message, BaseClasses.CPLogBaseClass.LogLevel.Error);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        public override void ErrorReport(System.Exception ex, string message) {
            LogController.log(cp.core, message + ", exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        public override void ErrorReport(System.Exception ex) {
            LogController.log(cp.core, "exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Debug log entry
        /// </summary>
        /// <param name="message"></param>
        public override void TestPoint(string message)
            => Logger.Trace(cp.core.logCommonMessage + "," + message);
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventNameIdOrGuid"></param>
        /// <returns></returns>
        public override string ThrowEventByName(string eventName) {
            //
            cp.Log.Trace($"CPSiteClass.throwEventByName(string [{eventName}]) enter");
            //
            return EventController.throwEventByName(cp.core, eventName);
        }
        //
        //====================================================================================================
        //
        [Obsolete("Deprecated. Use ThrowEventByName. Events are created and called only by name. Do not reference ids or guids.", false)]
        public override string ThrowEvent(string eventNameIdOrGuid) {
            //
            cp.Log.Trace($"CPSiteClass.ThrowEvent(string [{eventNameIdOrGuid}]) enter");
            //
            if (eventNameIdOrGuid.isNumeric()) {
                //
                // -- int
                AddonEventModel addonEvent = DbBaseModel.create<AddonEventModel>(cp, cp.Utils.EncodeInteger(eventNameIdOrGuid));
                if (addonEvent == null) { return ""; }
                return ThrowEventByName(addonEvent.name);
            } else if (GuidController.isGuid(eventNameIdOrGuid)) {
                //
                // -- guid
                AddonEventModel addonEvent = DbBaseModel.create<AddonEventModel>(cp, eventNameIdOrGuid);
                if (addonEvent == null) { return ""; }
                return ThrowEventByName(addonEvent.name);
            } else {
                //
                // -- name
                return EventController.throwEventByName(cp.core, eventNameIdOrGuid);
            }
        }
        //
        //====================================================================================================
        //
        [Obsolete("Deprecated. Use ThrowEventByName. Events are created and called only by name. Do not reference ids or guids.", false)]
        public override string ThrowEvent(int eventId) {
            //
            cp.Log.Trace($"CPSiteClass.ThrowEvent(int [{eventId}]) enter");
            //
            AddonEventModel addonEvent = DbBaseModel.create<AddonEventModel>(cp, eventId);
            if (addonEvent == null) { return ""; }
            return ThrowEventByName(addonEvent.name);
        }
        //
        //====================================================================================================
        //
        [Obsolete("Deprecated. Use ThrowEventByName. Events are created and called only by name. Do not reference ids or guids.", false)]
        public override string ThrowEventByGuid(string eventGuid) {
            //
            cp.Log.Trace($"CPSiteClass.ThrowEventByGuid(string [{eventGuid}]) enter");
            //
            AddonEventModel addonEvent = DbBaseModel.create<AddonEventModel>(cp, eventGuid);
            if (addonEvent == null) { return ""; }
            return ThrowEventByName(addonEvent.name);
        }
        //
        //====================================================================================================
        // Deprecated
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Deprecated.", false)]
        public override bool MultiDomainMode { get { return false; } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use GetText()", false)]
        public override string GetProperty(string propertyName, string DefaultValue) {
            return cp.core.siteProperties.getText(propertyName, DefaultValue);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        [Obsolete("Use GetText()", false)]
        public override string GetProperty(string propertyName) {
            return cp.core.siteProperties.getText(propertyName, "");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use methods with matching types", false)]
        public override bool GetBoolean(string propertyName, string DefaultValue) {
            return cp.core.siteProperties.getBoolean(propertyName, GenericController.encodeBoolean(DefaultValue));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use methods with matching types", false)]
        public override DateTime GetDate(string propertyName, string DefaultValue) {
            return cp.core.siteProperties.getDate(propertyName, GenericController.encodeDate(DefaultValue));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use methods with matching types", false)]
        public override int GetInteger(string propertyName, string DefaultValue) {
            return cp.core.siteProperties.getInteger(propertyName, GenericController.encodeInteger(DefaultValue));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        [Obsolete("Deprecated.", false)]
        public override double GetNumber(string propertyName, string DefaultValue) {
            return cp.core.siteProperties.getNumber(propertyName, GenericController.encodeNumber(DefaultValue));
        }
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Deprecated, please use cp.cdnFiles, cp.privateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override string PhysicalFilePath {
            get {
                return cp.core.cdnFiles.localAbsRootPath;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Deprecated, please use cp.cdnFiles, cp.privateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override string PhysicalInstallPath {
            get {
                return cp.core.privateFiles.localAbsRootPath;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Deprecated, please use cp.cdnFiles, cp.privateFiles, cp.WwwFiles, or cp.TempFiles instead.", false)]
        public override string PhysicalWWWPath {
            get {
                return cp.core.wwwFiles.localAbsRootPath;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Deprecated.", false)]
        public override bool TrapErrors {
            get {
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Deprecated.", false)]
        public override string AppPath {
            get {
                return AppRootPath;
            }
        }
        //
        [Obsolete("Deprecated.", false)]
        public override string AppRootPath {
            get {
                return appRootPath;
            }
        }
        //
        [Obsolete("Deprecated.", false)]
        public override string VirtualPath {
            get {
                return "/" + cp.core.appConfig.name;
            }
        }
        //
        [Obsolete("Deprecated.", false)]
        public override bool IsTesting() { return false; }
        //
        [Obsolete("Use GetInteger(LandingPageID)", false)]
        public override int LandingPageId() {
            return GetInteger("LandingPageID", 0);
        }
        //
        [Obsolete("Use GetInteger(LandingPageID)", false)]
        public override int LandingPageId(string domainName) {
            if (string.IsNullOrWhiteSpace(domainName)) { return GetInteger("LandingPageID", 0); }
            var domain = DbBaseModel.createByUniqueName<DomainModel>(cp, domainName);
            if (domain == null) { return GetInteger("LandingPageID", 0); }
            return domain.rootPageId;
        }
        //
        [Obsolete("Use CP.Utils.ExportCsv()", false)]
        public override void RequestTask(string command, string sql, string exportName, string filename) {
            cp.Utils.ExportCsv(sql, exportName, filename);
        }
        //
        [Obsolete("Use CP.Addon.InstallCollectionFile()", false)]
        public override bool installCollectionFile(string privatePathFilename, ref string returnUserError)
            => cp.Addon.InstallCollectionFile(privatePathFilename, ref returnUserError);
        //
        [Obsolete("Use CP.Utils.InstallCollectionFromLibrary()", false)]
        public override bool installCollectionFromLibrary(string collectionGuid, ref string returnUserError)
            => cp.Addon.InstallCollectionFromLibrary(collectionGuid, ref returnUserError);
        //
        [Obsolete("Use CP.Utils.EncodeAppRootPath()", false)]
        public override string EncodeAppRootPath(string link)
            => cp.Utils.EncodeAppRootPath(link);
        //
        //====================================================================================================
        //
        [Obsolete("Use CP.Http.CdnFilePathPrefix or CP.Http.CdnFilePathPrefixAbsolute", false)]
        public override string FilePath {
            get {
                return cp.core.appConfig.cdnFileUrl;
            }
        }
        [Obsolete("Use CP.Site.AddAdminWarning", false)]
        public override void LogWarning(string name, string description, string typeOfWarningKey, string instanceKey) {
            LogController.setSiteWarning(cp.core, name, description, true);
        }
        //
        //
        #region  IDisposable Support 
        //
        //====================================================================================================
        //
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
        protected bool disposed_site;
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPSiteClass() {
            Dispose(false);
        }
        #endregion
    }
}