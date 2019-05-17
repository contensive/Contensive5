﻿
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
//
namespace Contensive.Processor.Models.Db {
    public class GroupModel : DbBaseModel {
        //
        //====================================================================================================
        //-- const
        public const string contentName = "groups";
        public const string contentTableNameLowerCase = "ccgroups";
        public const string contentDataSource = "default";
        public const bool nameFieldIsUnique = true;
        //
        //====================================================================================================
        // -- instance properties
        public bool allowBulkEmail { get; set; }
        public string caption { get; set; }        
        public string copyFilename { get; set; }
        public bool publicJoin { get; set; }
        // 
        //====================================================================================================
        public static GroupModel addEmpty(CoreController core) {
            return addEmpty<GroupModel>(core);
        }
        //
        //====================================================================================================
        public static GroupModel addDefault(CoreController core, Domain.ContentMetadataModel metaData) {
            return addDefault<GroupModel>(core, metaData);
        }
        //
        //====================================================================================================
        public static GroupModel addDefault(CoreController core, ref List<string> callersCacheNameList, Domain.ContentMetadataModel metaData) {
            return addDefault<GroupModel>(core, metaData, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static GroupModel create(CoreController core, int recordId) {
            return create<GroupModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static GroupModel create(CoreController core, int recordId, ref List<string> callersCacheNameList) {
            return create<GroupModel>(core, recordId, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static GroupModel create(CoreController core, string recordGuid) {
            return create<GroupModel>(core, recordGuid);
        }
        //
        //====================================================================================================
        public static GroupModel create(CoreController core, string recordGuid, ref List<string> callersCacheNameList) {
            return create<GroupModel>(core, recordGuid, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public static GroupModel createByUniqueName(CoreController core, string recordName) {
            return createByUniqueName<GroupModel>(core, recordName);
        }
        //
        //====================================================================================================
        public static GroupModel createByUniqueName(CoreController core, string recordName, ref List<string> callersCacheNameList) {
            return createByUniqueName<GroupModel>(core, recordName, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        public new void save(CoreController core, bool asyncSave = false) { base.save(core, asyncSave); }
        //
        //====================================================================================================
        public static void delete(CoreController core, int recordId) {
            delete<GroupModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static void delete(CoreController core, string ccGuid) {
            delete<GroupModel>(core, ccGuid);
        }
        //
        //====================================================================================================
        public static List<GroupModel> createList(CoreController core, string sqlCriteria, string sqlOrderBy, List<string> callersCacheNameList) {
            return createList<GroupModel>(core, sqlCriteria, sqlOrderBy, callersCacheNameList);
        }
        //
        //====================================================================================================
        public static List<GroupModel> createList(CoreController core, string sqlCriteria, string sqlOrderBy) {
            return createList<GroupModel>(core, sqlCriteria, sqlOrderBy);
        }
        //
        //====================================================================================================
        public static List<GroupModel> createList(CoreController core, string sqlCriteria) {
            return createList<GroupModel>(core, sqlCriteria);
        }
        //
        //====================================================================================================
        public static void invalidateRecordCache(CoreController core, int recordId) {
            invalidateCacheOfRecord<GroupModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static string getRecordName(CoreController core, int recordId) {
            return DbBaseModel.getRecordName<GroupModel>(core, recordId);
        }
        //
        //====================================================================================================
        public static string getRecordName(CoreController core, string ccGuid) {
            return DbBaseModel.getRecordName<GroupModel>(core, ccGuid);
        }
        //
        //====================================================================================================
        public static int getRecordId(CoreController core, string ccGuid) {
            return DbBaseModel.getRecordId<GroupModel>(core, ccGuid);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return a cache key used to represent the table. ONLY used for invalidation. Add this as a dependent key if you want that key cleared when ANY record in the table is changed.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string getTableInvalidationKey(CoreController core) {
            return getTableCacheKey<GroupModel>(core);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Verify a group exists by name. If so, verify the caption. If not create the group.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupName"></param>
        /// <param name="groupCaption"></param>
        /// <returns></returns>
        public static GroupModel verify(CoreController core, string groupName, string groupCaption) {
            Models.Db.GroupModel result = null;
            try {
                result = Models.Db.GroupModel.createByUniqueName(core, groupName);
                if (result != null) {
                    if (result.caption != groupCaption) {
                        result.caption = groupCaption;
                        result.save(core);
                    }
                } else {
                    var groupMetaData = Models.Domain.ContentMetadataModel.createByUniqueName(core, "groups");
                    result = Models.Db.GroupModel.addDefault(core, groupMetaData);
                    result.name = groupName;
                    result.caption = groupCaption;
                    result.save(core);
                }
            } catch (Exception ex) {
                throw (ex);
            }
            return result;
        }
    }
}
