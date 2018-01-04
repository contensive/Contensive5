﻿
using System;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Contensive.Core;
using Contensive.Core.Models.Entity;
using Contensive.Core.Controllers;
using static Contensive.Core.Controllers.genericController;
using static Contensive.Core.constants;
//
namespace Contensive.Core.Models.Entity {
    public class groupEmailModel : baseModel {
        //
        //====================================================================================================
        //-- const
        public const string contentName = "group email";
        public const string contentTableName = "ccemail";
        private const string contentDataSource = "default";
        //
        //====================================================================================================
        // -- instance properties
        public bool AddLinkEID { get; set; }
        public bool AllowSpamFooter { get; set; }
        public bool BlockSiteStyles { get; set; }
        public string CopyFilename { get; set; }
        public int EmailTemplateID { get; set; }
        public string FromAddress { get; set; }
        public string InlineStyles { get; set; }
        public DateTime LastSendTestDate { get; set; }
        public DateTime ScheduleDate { get; set; }
        public bool Sent { get; set; }
        public string StylesFilename { get; set; }
        public string Subject { get; set; }
        public bool Submitted { get; set; }
        public int TestMemberID { get; set; }
        //
        //====================================================================================================
        public static groupEmailModel add(coreClass cpCore) {
            return add<groupEmailModel>(cpCore);
        }
        //
        //====================================================================================================
        public static groupEmailModel add(coreClass cpCore, ref List<string> callersCacheNameList) {
            return add<groupEmailModel>(cpCore, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static groupEmailModel create(coreClass cpCore, int recordId) {
            return create<groupEmailModel>(cpCore, recordId);
        }
        //
        //====================================================================================================
        public static groupEmailModel create(coreClass cpCore, int recordId, ref List<string> callersCacheNameList) {
            return create<groupEmailModel>(cpCore, recordId, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static groupEmailModel create(coreClass cpCore, string recordGuid) {
            return create<groupEmailModel>(cpCore, recordGuid);
        }
        //
        //====================================================================================================
        public static groupEmailModel create(coreClass cpCore, string recordGuid, ref List<string> callersCacheNameList) {
            return create<groupEmailModel>(cpCore, recordGuid, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static groupEmailModel createByName(coreClass cpCore, string recordName) {
            return createByName<groupEmailModel>(cpCore, recordName);
        }
        //
        //====================================================================================================
        public static groupEmailModel createByName(coreClass cpCore, string recordName, ref List<string> callersCacheNameList) {
            return createByName<groupEmailModel>(cpCore, recordName, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public new void save(coreClass cpCore) {
            base.save(cpCore);
        }
        //
        //====================================================================================================
        public static void delete(coreClass cpCore, int recordId) {
            delete<groupEmailModel>(cpCore, recordId);
        }
        //
        //====================================================================================================
        public static void delete(coreClass cpCore, string ccGuid) {
            delete<groupEmailModel>(cpCore, ccGuid);
        }
        //
        //====================================================================================================
        public static List<groupEmailModel> createList(coreClass cpCore, string sqlCriteria, string sqlOrderBy, List<string> callersCacheNameList) {
            return createList<groupEmailModel>(cpCore, sqlCriteria, sqlOrderBy, callersCacheNameList);
        }
        //
        //====================================================================================================
        public static List<groupEmailModel> createList(coreClass cpCore, string sqlCriteria, string sqlOrderBy) {
            return createList<groupEmailModel>(cpCore, sqlCriteria, sqlOrderBy);
        }
        //
        //====================================================================================================
        public static List<groupEmailModel> createList(coreClass cpCore, string sqlCriteria) {
            return createList<groupEmailModel>(cpCore, sqlCriteria);
        }
        //
        //====================================================================================================
        public void invalidatePrimaryCache(coreClass cpCore, int recordId) {
            invalidateCacheSingleRecord<groupEmailModel>(cpCore, recordId);
        }
        //
        //====================================================================================================
        public static string getRecordName(coreClass cpcore, int recordId) {
            return baseModel.getRecordName<groupEmailModel>(cpcore, recordId);
        }
        //
        //====================================================================================================
        public static string getRecordName(coreClass cpcore, string ccGuid) {
            return baseModel.getRecordName<groupEmailModel>(cpcore, ccGuid);
        }
        //
        //====================================================================================================
        public static int getRecordId(coreClass cpcore, string ccGuid) {
            return baseModel.getRecordId<groupEmailModel>(cpcore, ccGuid);
        }
        //
        //====================================================================================================
        public static groupEmailModel createDefault(coreClass cpcore) {
            return createDefault<groupEmailModel>(cpcore);
        }
    }
}
