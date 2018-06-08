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
using Contensive.Processor;
using Contensive.Processor.Models.DbModels;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.genericController;
using static Contensive.Processor.constants;
//
namespace Contensive.Processor.Models.DbModels {
    public class GroupRuleModel : baseModel {
        //
        //====================================================================================================
        //-- const
        public const string contentName = "group rules";
        public const string contentTableName = "ccgrouprules";
        private const string contentDataSource = "default"; 
        //
        //====================================================================================================
        // -- instance properties
        public bool AllowAdd { get; set; }
        public bool AllowDelete { get; set; }
        
        public int ContentID { get; set; }
        public int GroupID { get; set; }
        //
        //====================================================================================================
        public static GroupRuleModel add(coreController core) {
            return add<GroupRuleModel>(core);
        }
        //
        //====================================================================================================
        public static GroupRuleModel add(coreController core, ref List<string> callersCacheNameList) {
            return add<GroupRuleModel>(core, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static GroupRuleModel create(coreController core, int recordId) {
            return create<GroupRuleModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static GroupRuleModel create(coreController core, int recordId, ref List<string> callersCacheNameList) {
            return create<GroupRuleModel>(core, recordId, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static GroupRuleModel create(coreController core, string recordGuid) {
            return create<GroupRuleModel>(core, recordGuid);
        }
        //
        //====================================================================================================
        public static GroupRuleModel create(coreController core, string recordGuid, ref List<string> callersCacheNameList) {
            return create<GroupRuleModel>(core, recordGuid, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static GroupRuleModel createByName(coreController core, string recordName) {
            return createByName<GroupRuleModel>(core, recordName);
        }
        //
        //====================================================================================================
        public static GroupRuleModel createByName(coreController core, string recordName, ref List<string> callersCacheNameList) {
            return createByName<GroupRuleModel>(core, recordName, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public new void save(coreController core) {
            base.save(core);
        }
        //
        //====================================================================================================
        public static void delete(coreController core, int recordId) {
            delete<GroupRuleModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static void delete(coreController core, string ccGuid) {
            delete<GroupRuleModel>(core, ccGuid);
        }
        //
        //====================================================================================================
        public static List<GroupRuleModel> createList(coreController core, string sqlCriteria, string sqlOrderBy, List<string> callersCacheNameList) {
            return createList<GroupRuleModel>(core, sqlCriteria, sqlOrderBy, callersCacheNameList);
        }
        //
        //====================================================================================================
        public static List<GroupRuleModel> createList(coreController core, string sqlCriteria, string sqlOrderBy) {
            return createList<GroupRuleModel>(core, sqlCriteria, sqlOrderBy);
        }
        //
        //====================================================================================================
        public static List<GroupRuleModel> createList(coreController core, string sqlCriteria) {
            return createList<GroupRuleModel>(core, sqlCriteria);
        }
        //
        //====================================================================================================
        public void invalidatePrimaryCache(coreController core, int recordId) {
            invalidateCacheSingleRecord<GroupRuleModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static string getRecordName(coreController core, int recordId) {
            return baseModel.getRecordName<GroupRuleModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static string getRecordName(coreController core, string ccGuid) {
            return baseModel.getRecordName<GroupRuleModel>(core, ccGuid);
        }
        //
        //====================================================================================================
        public static int getRecordId(coreController core, string ccGuid) {
            return baseModel.getRecordId<GroupRuleModel>(core, ccGuid);
        }
        //
        //====================================================================================================
        public static GroupRuleModel createDefault(coreController core) {
            return createDefault<GroupRuleModel>(core);
        }
    }
}
