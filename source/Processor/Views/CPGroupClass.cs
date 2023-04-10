
using System;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;

namespace Contensive.Processor {
    /// <summary>
    /// group interface
    /// </summary>
    public class CPGroupClass : BaseClasses.CPGroupBaseClass, IDisposable {
        //
        private readonly CoreController core;
        //
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cpParent"></param>
        public CPGroupClass(CPClass cpParent) {
            cp = cpParent;
            core = cp.core;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a group
        /// </summary>
        /// <param name="groupName"></param>
        public override void Add(string groupName)
            => GroupController.add(core, groupName);
        //
        //====================================================================================================
        /// <summary>
        /// Add a group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="groupCaption"></param>
        public override void Add(string groupName, string groupCaption)
            => GroupController.add(core, groupName, groupCaption);
        //
        //====================================================================================================
        /// <summary>
        /// Add current user to a group
        /// </summary>
        /// <param name="groupNameOrGuid"></param>
        public override void AddUser(string groupNameOrGuid)
            => GroupController.addUser(core, groupNameOrGuid, core.session.user.id, DateTime.MinValue);
        //
        //====================================================================================================
        /// <summary>
        /// Add current user to a group
        /// </summary>
        /// <param name="groupId"></param>
        public override void AddUser(int groupId)
            => GroupController.addUser(core, groupId.ToString(), core.session.user.id, DateTime.MinValue);
        //
        //====================================================================================================
        /// <summary>
        /// Add user to a group
        /// </summary>
        /// <param name="GroupNameIdOrGuid"></param>
        /// <param name="UserId"></param>
        public override void AddUser(string GroupNameIdOrGuid, int UserId)
            => GroupController.addUser(core, GroupNameIdOrGuid, UserId, DateTime.MinValue);
        //
        //====================================================================================================
        /// <summary>
        /// Add user to a group
        /// </summary>
        /// <param name="GroupNameIdOrGuid"></param>
        /// <param name="UserId"></param>
        /// <param name="DateExpires"></param>
        public override void AddUser(string GroupNameIdOrGuid, int UserId, DateTime DateExpires)
            => GroupController.addUser(core, GroupNameIdOrGuid, UserId, DateExpires);
        //
        //====================================================================================================
        /// <summary>
        /// Add user to a group
        /// </summary>
        /// <param name="GroupId"></param>
        /// <param name="UserId"></param>
        public override void AddUser(int GroupId, int UserId)
            => GroupController.addUser(core, GroupId, UserId, DateTime.MinValue);
        //
        //====================================================================================================
        /// <summary>
        /// Add user to a group
        /// </summary>
        /// <param name="GroupId"></param>
        /// <param name="UserId"></param>
        /// <param name="DateExpires"></param>
        public override void AddUser(int GroupId, int UserId, DateTime DateExpires)
            => GroupController.addUser(core, GroupId.ToString(), UserId, DateExpires);
        //
        //====================================================================================================
        /// <summary>
        /// Delete group
        /// </summary>
        /// <param name="GroupNameIdOrGuid"></param>
        public override void Delete(string GroupNameIdOrGuid) {
            if (GenericController.isGuid(GroupNameIdOrGuid)) {
                //
                // -- guid
                GroupModel.delete<GroupModel>(core.cpParent, GroupNameIdOrGuid);
                return;
            }
            if (GroupNameIdOrGuid.isNumeric()) {
                //
                // -- id
                GroupModel.delete<GroupModel>(core.cpParent, GenericController.encodeInteger(GroupNameIdOrGuid));
                return;
            }
            //
            // -- name
            GroupModel.deleteRows<GroupModel>(core.cpParent, "(name=" + DbController.encodeSQLText(GroupNameIdOrGuid) + ")");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Delete group
        /// </summary>
        /// <param name="GroupId"></param>
        public override void Delete(int GroupId) => DbBaseModel.delete<GroupModel>(core.cpParent, GroupId);
        //
        //====================================================================================================
        /// <summary>
        /// Get group id
        /// </summary>
        /// <param name="GroupNameOrGuid"></param>
        /// <returns></returns>
        public override int GetId(string GroupNameOrGuid) {
            GroupModel group;
            if (GenericController.isGuid(GroupNameOrGuid)) {
                group = DbBaseModel.create<GroupModel>(cp, GroupNameOrGuid);
            } else {
                group = DbBaseModel.createByUniqueName<GroupModel>(cp, GroupNameOrGuid);
            }
            if (group != null) { return group.id; }
            return 0;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get group name
        /// </summary>
        /// <param name="GroupIdOrGuid"></param>
        /// <returns></returns>
        public override string GetName(string GroupIdOrGuid) {
            if (GroupIdOrGuid.isNumeric()) {
                //
                // id
                return DbBaseModel.getRecordName<GroupModel>(core.cpParent, GenericController.encodeInteger(GroupIdOrGuid));
            } else {
                //
                // guid
                return DbBaseModel.getRecordName<GroupModel>(core.cpParent, GroupIdOrGuid);
            }
        }
        /// <summary>G
        /// Get group name
        /// </summary>
        /// <param name="GroupId"></param>
        /// <returns></returns>
        public override string GetName(int GroupId)
            => DbBaseModel.getRecordName<GroupModel>(core.cpParent, GroupId);
        //
        //====================================================================================================
        /// <summary>
        /// remove user from group. If userId is 0, the current user is removed
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="userId"></param>
        public override void RemoveUser(int groupId, int userId) {
            if (groupId == 0) { return; }
            if (userId == 0) {
                GroupController.removeUser(core, groupId);
                return;
            }
            GroupController.removeUser(core, groupId, userId);
        }
        //
        //====================================================================================================
        /// <summary>
        /// remove user from group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="userId"></param>
        public override void RemoveUser(string GroupNameOrGuid, int userId) {
            int groupId = GetId(GroupNameOrGuid);
            RemoveUser(groupId, userId);
        }
        //
        //====================================================================================================
        /// <summary>
        /// remove current user from group
        /// </summary>
        /// <param name="GroupNameOrGuid"></param>
        public override void RemoveUser(string GroupNameOrGuid) => RemoveUser(GroupNameOrGuid, 0);
        //
        //====================================================================================================
        /// <summary>
        /// remove current user from group
        /// </summary>
        /// <param name="GroupNameOrGuid"></param>
        public override void RemoveUser(int groupId) => RemoveUser(groupId, 0);
        //
        //====================================================================================================
        /// <summary>
        /// if group does not exists by guid, create group with caption matching name
        /// </summary>
        /// <param name="groupGuid"></param>
        /// <param name="groupName"></param>
        public override void verifyGroup(string groupGuid, string groupName) {
            if (string.IsNullOrEmpty(groupGuid)) return;
            if (exists(groupGuid)) { return; }
            if (string.IsNullOrEmpty(groupName)) return;
            verifyGroup(groupGuid, groupName, groupName);
            return;
        }
        //
        //====================================================================================================
        /// <summary>
        /// if group does not exists by guid, create group
        /// </summary>
        /// <param name="groupGuid"></param>
        /// <param name="groupName"></param>
        /// <param name="groupCaption"></param>
        public override void verifyGroup(string groupGuid, string groupName, string groupCaption) {
            if (string.IsNullOrEmpty(groupGuid)) return;
            if (exists(groupGuid)) { return; }
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(groupName)) return;
            var group = DbBaseModel.addDefault<GroupModel>(cp);
            group.ccguid = groupGuid;
            group.name = groupName;
            group.caption = groupCaption;
            group.save(cp);
            return;
        }
        //
        //====================================================================================================
        /// <summary>
        /// if group exists by guid, return true, else false
        /// </summary>
        /// <param name="groupGuid"></param>
        /// <returns></returns>
        public override bool exists(string groupGuid) {
            if (string.IsNullOrEmpty(groupGuid)) return false;
            return DbBaseModel.getCount<GroupModel>(cp, $"ccguid={groupGuid}") > 0;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        //
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        /// <summary>
        /// Dispose pattern
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose pattern
        /// </summary>
        ~CPGroupClass() {
            Dispose(false);
        }
        /// <summary>
        /// Dispose pattern
        /// </summary>
        /// <param name="disposing_group"></param>
        protected virtual void Dispose(bool disposing_group) {
            if (!this.disposed_group) {
                if (disposing_group) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_group = true;
        }
        /// <summary>
        /// Dispose 
        /// </summary>
        protected bool disposed_group;
        #endregion
    }
}