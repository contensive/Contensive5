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
    public class tableModel : baseModel {
        //
        //====================================================================================================
        //-- const
        public const string contentName = "tables"; //<------ set content name
        public const string contentTableName = "ccTables"; //<------ set to tablename for the primary content (used for cache names)
        private const string contentDataSource = "default"; //<----- set to datasource if not default
        //
        //====================================================================================================
        // -- instance properties
        public int DataSourceID { get; set; }
        //
        //====================================================================================================
        public static tableModel add(CoreController core) {
            return add<tableModel>(core);
        }
        //
        //====================================================================================================
        public static tableModel add(CoreController core, ref List<string> callersCacheNameList) {
            return add<tableModel>(core, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static tableModel create(CoreController core, int recordId) {
            return create<tableModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static tableModel create(CoreController core, int recordId, ref List<string> callersCacheNameList) {
            return create<tableModel>(core, recordId, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static tableModel create(CoreController core, string recordGuid) {
            return create<tableModel>(core, recordGuid);
        }
        //
        //====================================================================================================
        public static tableModel create(CoreController core, string recordGuid, ref List<string> callersCacheNameList) {
            return create<tableModel>(core, recordGuid, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static tableModel createByName(CoreController core, string recordName) {
            return createByName<tableModel>(core, recordName);
        }
        //
        //====================================================================================================
        public static tableModel createByName(CoreController core, string recordName, ref List<string> callersCacheNameList) {
            return createByName<tableModel>(core, recordName, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public new void save(CoreController core) {
            base.save(core);
        }
        //
        //====================================================================================================
        public static void delete(CoreController core, int recordId) {
            delete<tableModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static void delete(CoreController core, string ccGuid) {
            delete<tableModel>(core, ccGuid);
        }
        //
        //====================================================================================================
        public static List<tableModel> createList(CoreController core, string sqlCriteria, string sqlOrderBy, List<string> callersCacheNameList) {
            return createList<tableModel>(core, sqlCriteria, sqlOrderBy, callersCacheNameList);
        }
        //
        //====================================================================================================
        public static List<tableModel> createList(CoreController core, string sqlCriteria, string sqlOrderBy) {
            return createList<tableModel>(core, sqlCriteria, sqlOrderBy);
        }
        //
        //====================================================================================================
        public static List<tableModel> createList(CoreController core, string sqlCriteria) {
            return createList<tableModel>(core, sqlCriteria);
        }
        //
        //====================================================================================================
        public void invalidatePrimaryCache(CoreController core, int recordId) {
            invalidateCacheSingleRecord<tableModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static string getRecordName(CoreController core, int recordId) {
            return baseModel.getRecordName<tableModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static string getRecordName(CoreController core, string ccGuid) {
            return baseModel.getRecordName<tableModel>(core, ccGuid);
        }
        //
        //====================================================================================================
        public static int getRecordId(CoreController core, string ccGuid) {
            return baseModel.getRecordId<tableModel>(core, ccGuid);
        }
        //
        //====================================================================================================
        public static tableModel createDefault(CoreController core) {
            return createDefault<tableModel>(core);
        }
    }
}
