
using System;
using Contensive.Processor.Exceptions;
using System.Linq;
using static Contensive.Processor.Controllers.GenericController;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class GroupController : IDisposable {
        //
        //====================================================================================================
        /// <summary>
        /// Create a group, set the name and caption, return its Id. If the group already exists, the groups Id is returned.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static int add(CoreController core, string groupName, string groupCaption) {
            var defaultValues = ContentMetadataModel.getDefaultValueDict(core, GroupModel.tableMetadata.contentName);
            return GroupModel.verify(core.cpParent, groupName, groupCaption, 0, defaultValues).id;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create a group and return its Id. If the group already exists, the groups Id is returned. If the group cannot be added a 0 is returned.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="groupCaption"></param>
        /// <returns></returns>
        public static int add(CoreController core, string groupName) => add(core, groupName, groupName);
        //
        //====================================================================================================
        /// <summary>
        /// Add a user to a group with an expiration date
        /// </summary>
        /// <param name="core"></param>
        /// <param name="group"></param>
        /// <param name="user"></param>
        /// <param name="dateExpires"></param>
        public static void addUser(CoreController core, GroupModel group, PersonModel user, DateTime dateExpires) {
            try {
                var ruleList = DbBaseModel.createList<MemberRuleModel>(core.cpParent, "(MemberID=" + user.id.ToString() + ")and(GroupID=" + group.id.ToString() + ")");
                if (ruleList.Count == 0) {
                    // -- add new rule
                    var rule = DbBaseModel.addDefault<MemberRuleModel>(core.cpParent, Models.Domain.ContentMetadataModel.getDefaultValueDict(core, "groups"));
                    rule.groupId = group.id;
                    rule.memberId = user.id;
                    rule.dateExpires = dateExpires;
                    rule.save(core.cpParent);
                    return;
                }
                // at least one rule found, set expire date, delete the rest
                var ruleFirst = ruleList.First();
                if (ruleFirst.dateExpires != dateExpires) {
                    ruleFirst.dateExpires = dateExpires;
                    ruleFirst.save(core.cpParent);
                }
                if (ruleList.Count > 1) {
                    foreach (var rule in ruleList) {
                        if (!rule.Equals(ruleFirst)) DbBaseModel.delete<MemberRuleModel>(core.cpParent, rule.id);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a user to a group with no expiration date
        /// </summary>
        /// <param name="core"></param>
        /// <param name="group"></param>
        /// <param name="user"></param>
        public static void addUser(CoreController core, GroupModel group, PersonModel user) => addUser(core, group, user, DateTime.MinValue);
        //
        //====================================================================================================
        /// <summary>
        /// Add a user to a group. If gorupId doesnt exist, an error is logged.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupId"></param>
        /// <param name="userId"></param>
        /// <param name="dateExpires"></param>
        public static void addUser(CoreController core, int groupId, int userId, DateTime dateExpires) {
            var group = DbBaseModel.create<GroupModel>(core.cpParent, groupId);
            if (group == null) {
                //
                // -- inactive or invalid groupId
                //LogController.logError(core, new GenericException("addUser called with invalid groupId [" + groupId + "]"));
                return;
            }
            if (userId.Equals(0)) {
                //
                // -- default to keyboard user
                userId = core.session.user.id;
            }
            PersonModel user = DbBaseModel.create<PersonModel>(core.cpParent, userId);
            if (user == null) {
                //
                // -- inactive or invalid userId
                //LogController.logError(core, new GenericException("addUser called with invalid userId [" + userId + "]"));
                return;
            }
            addUser(core, group, user, dateExpires);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a user to a group. if group is missing and argument is name or guid, it is created.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupNameIdOrGuid"></param>
        /// <param name="userid"></param>
        /// <param name="dateExpires"></param>
        public static void addUser(CoreController core, string groupNameIdOrGuid, int userid, DateTime dateExpires) {
            GroupModel group = null;
            if (groupNameIdOrGuid.isNumeric()) {
                group = DbBaseModel.create<GroupModel>(core.cpParent, GenericController.encodeInteger(groupNameIdOrGuid));
                if (group == null) {
                    LogController.logError(core, new GenericException("addUser called with invalid groupId [" + groupNameIdOrGuid + "]"));
                    return;
                }
            } else if (GenericController.isGuid(groupNameIdOrGuid)) {
                group = DbBaseModel.create<GroupModel>(core.cpParent, groupNameIdOrGuid);
                if (group == null) {
                    var defaultValues = ContentMetadataModel.getDefaultValueDict(core, "groups");
                    group = DbBaseModel.addDefault<GroupModel>(core.cpParent, defaultValues);
                    group.ccguid = groupNameIdOrGuid;
                    group.name = groupNameIdOrGuid;
                    group.caption = groupNameIdOrGuid;
                    group.save(core.cpParent);
                }
            } else {
                group = DbBaseModel.createByUniqueName<GroupModel>(core.cpParent, groupNameIdOrGuid);
                if (group == null) {
                    group = DbBaseModel.addDefault<GroupModel>(core.cpParent, Models.Domain.ContentMetadataModel.getDefaultValueDict(core, "groups"));
                    group.ccguid = groupNameIdOrGuid;
                    group.name = groupNameIdOrGuid;
                    group.caption = groupNameIdOrGuid;
                    group.save(core.cpParent);
                }

            }
            var user = DbBaseModel.create<PersonModel>(core.cpParent, userid);
            if (user != null) { addUser(core, group, user, dateExpires); }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a user to a group. if group is missing and argument is name or guid, it is created.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupNameIdOrGuid"></param>
        /// <param name="userid"></param>
        public static void addUser(CoreController core, string groupNameIdOrGuid, int userid) => addUser(core, groupNameIdOrGuid, userid, DateTime.MinValue);
        //
        //====================================================================================================
        /// <summary>
        /// Add the current user to a group. if group is missing and argument is name or guid, it is created.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupNameIdOrGuid"></param>
        public static void addUser(CoreController core, string groupNameIdOrGuid) => addUser(core, groupNameIdOrGuid, core.session.user.id, DateTime.MinValue);
        //
        //====================================================================================================
        /// <summary>
        /// Get a group Id
        /// </summary>
        /// <param name="core"></param>
        /// <param name="GroupName"></param>
        /// <returns></returns>
        public static int getGroupId(CoreController core, string GroupName) {
            var group = DbBaseModel.createByUniqueName<GroupModel>(core.cpParent, GroupName);
            if (group != null) { return group.id; }
            return 0;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get a group Name
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public static string getGroupName(CoreController core, int groupId) {
            var group = DbBaseModel.create<GroupModel>(core.cpParent, groupId);
            if (group != null) { return group.name; }
            return String.Empty;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Remove a user from a group.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="group"></param>
        /// <param name="user"></param>
        public static void removeUser(CoreController core, GroupModel group, PersonModel user) {
            if ((group != null) && (user != null)) {
                MemberRuleModel.deleteRows<MemberRuleModel>(core.cpParent, "(MemberID=" + DbController.encodeSQLNumber(user.id) + ")AND(groupid=" + DbController.encodeSQLNumber(group.id) + ")");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Remove a user from a group. 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupName"></param>
        /// <param name="userId"></param>
        public static void removeUser(CoreController core, string groupName, int userId) {
            var group = DbBaseModel.createByUniqueName<GroupModel>(core.cpParent, groupName);
            if (group != null) {
                var user = DbBaseModel.create<PersonModel>(core.cpParent, userId);
                if (user != null) {
                    removeUser(core, group, user);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Remove the current user from a group
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupName"></param>
        public static void removeUser(CoreController core, string groupName) => removeUser(core, groupName, core.session.user.id);
        //
        //====================================================================================================
        /// <summary>
        /// Remove a user from a group. 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupId"></param>
        /// <param name="userId"></param>
        public static void removeUser(CoreController core, int groupId, int userId) {
            var group = DbBaseModel.create<GroupModel>(core.cpParent, groupId);
            if (group != null) {
                var user = DbBaseModel.create<PersonModel>(core.cpParent, userId);
                if (user != null) {
                    removeUser(core, group, user);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Remove the current user from a group
        /// </summary>
        /// <param name="core"></param>
        /// <param name="groupName"></param>
        public static void removeUser(CoreController core, int groupId) => removeUser(core, groupId, core.session.user.id);
        //
        //========================================================================
        /// <summary>
        /// Returns true if the user is an admin, or authenticated and in the group named
        /// </summary>
        /// <param name="core"></param>
        /// <param name="GroupName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        //
        public static bool isMemberOfGroup(CoreController core, string GroupName, int userId) {
            bool result = false;
            try {
                result = isInGroupList(core, "," + GroupController.getGroupId(core, GroupName), userId, true);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        public static bool isMemberOfGroup(CoreController core, string GroupName) => isMemberOfGroup(core, GroupName, core.session.user.id);
        //
        //========================================================================
        // ----- Returns true if the visitor is an admin, or authenticated and in the group list
        //========================================================================
        //
        public static bool isInGroupList(CoreController core, string GroupIDList, int checkMemberId = 0, bool adminReturnsTrue = false) {
            bool result = false;
            try {
                if (checkMemberId == 0) {
                    checkMemberId = core.session.user.id;
                }
                result = isInGroupList(core, checkMemberId, core.session.isAuthenticated, GroupIDList, adminReturnsTrue);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        //===============================================================================================================================
        //   Is Group Member of a GroupIDList
        //   admins are always returned true
        //===============================================================================================================================
        //
        public static bool isInGroupList(CoreController core, int MemberID, bool isAuthenticated, string GroupIDList) {
            return isInGroupList(core, MemberID, isAuthenticated, GroupIDList, true);
        }
        //
        //===============================================================================================================================
        //   Is Group Member of a GroupIDList
        //===============================================================================================================================
        //
        public static bool isInGroupList(CoreController core, int MemberID, bool isAuthenticated, string GroupIDList, bool adminReturnsTrue) {
            bool returnREsult = false;
            try {
                //
                string sql = null;
                string Criteria = null;
                string WorkingIDList = null;
                //
                returnREsult = false;
                if (isAuthenticated) {
                    WorkingIDList = GroupIDList;
                    WorkingIDList = GenericController.strReplace(WorkingIDList, " ", "");
                    while (GenericController.strInstr(1, WorkingIDList, ",,") != 0) {
                        WorkingIDList = GenericController.strReplace(WorkingIDList, ",,", ",");
                    }
                    if (!string.IsNullOrEmpty(WorkingIDList)) {
                        if (strMid(WorkingIDList, 1) == ",") {
                            if (strLen(WorkingIDList) <= 1) {
                                WorkingIDList = "";
                            } else {
                                WorkingIDList = strMid(WorkingIDList, 2);
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(WorkingIDList)) {
                        if (WorkingIDList.right(1) == ",") {
                            if (strLen(WorkingIDList) <= 1) {
                                WorkingIDList = "";
                            } else {
                                WorkingIDList = GenericController.strMid(WorkingIDList, 1, strLen(WorkingIDList) - 1);
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(WorkingIDList)) {
                        if (adminReturnsTrue) {
                            //
                            // check if memberid is admin
                            //
                            sql = "select top 1 m.id"
                                + " from ccmembers m"
                                + " where"
                                + " (m.id=" + MemberID + ")"
                                + " and(m.active<>0)"
                                + " and("
                                + " (m.admin<>0)"
                                + " or(m.developer<>0)"
                                + " )"
                                + " ";
                            using (var csData = new CsModel(core)) {
                                returnREsult = csData.openSql(sql);
                            }
                        }
                    } else {
                        //
                        // check if they are admin or in the group list
                        //
                        if (GenericController.strInstr(1, WorkingIDList, ",") != 0) {
                            Criteria = "r.GroupID in (" + WorkingIDList + ")";
                        } else {
                            Criteria = "r.GroupID=" + WorkingIDList;
                        }
                        Criteria = ""
                            + "(" + Criteria + ")"
                            + " and(r.id is not null)"
                            + " and((r.DateExpires is null)or(r.DateExpires>" + DbController.encodeSQLDate(core.dateTimeNowMockable) + "))"
                            + " ";
                        if (adminReturnsTrue) {
                            Criteria = "(" + Criteria + ")or(m.admin<>0)or(m.developer<>0)";
                        }
                        Criteria = ""
                            + "(" + Criteria + ")"
                            + " and(m.active<>0)"
                            + " and(m.id=" + MemberID + ")";
                        //
                        sql = "select top 1 m.id"
                            + " from ccmembers m"
                            + " left join ccMemberRules r on r.Memberid=m.id"
                            + " where" + Criteria;
                        using (var csData = new CsModel(core)) {
                            csData.openSql(sql);
                            returnREsult = csData.ok();
                        }
                    }
                }

            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnREsult;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed;
        //
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~GroupController() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }
    //
}