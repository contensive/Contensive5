
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;

namespace Contensive.Models.Db {
    //
    public class GroupModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("groups", "ccgroups", "default", true);
        //
        //====================================================================================================
        public bool allowBulkEmail { get; set; }
        public string caption { get; set; }
        public DbBaseModel.FieldTypeTextFile copyFilename { get; set; }
        public bool publicJoin { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Verify a group exists by name. If so, verify the caption. If not create the group.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="groupName"></param>
        /// <param name="groupCaption"></param>
        /// <returns></returns>
        public static GroupModel verify(CPBaseClass cp, string groupName, string groupCaption, int userId, Dictionary<string,string> DefaultValues) {
            GroupModel result = null;
            try {
                result = createByUniqueName<GroupModel>(cp, groupName);
                if (result != null) {
                    if (result.caption != groupCaption) {
                        result.caption = groupCaption;
                        result.save(cp, userId);
                    }
                } else {
                    result = addDefault<GroupModel>(cp, DefaultValues, userId);
                    result.name = groupName;
                    result.caption = groupCaption;
                    result.save(cp, userId);
                }
            } catch (Exception) {
                throw;
            }
            return result;
        }

    }
}
