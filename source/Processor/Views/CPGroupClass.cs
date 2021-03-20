
using System;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;

namespace Contensive.Processor {
    /// <summary>
    /// group interface
    /// </summary>
    public class CPGroupClass : BaseClasses.CPGroupBaseClass, IDisposable {
        //
        private Contensive.Processor.Controllers.CoreController core;
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
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
        public override void Add(string groupName) => GroupController.add(core, groupName);
        //
        //====================================================================================================
        /// <summary>
        /// Add a group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="groupCaption"></param>
        public override void Add(string groupName, string groupCaption) => GroupController.add(core, groupName, groupCaption);
        //
        //====================================================================================================
        /// <summary>
        /// Add current user to a group
        /// </summary>
        /// <param name="groupNameOrGuid"></param>
        public override void AddUser(string groupNameOrGuid) => GroupController.addUser(core, groupNameOrGuid, core.session.user.id, DateTime.MinValue);
        //
        //====================================================================================================
        /// <summary>
        /// Add current user to a group
        /// </summary>
        /// <param name="groupId"></param>
        public override void AddUser(int groupId) => GroupController.addUser(core, groupId.ToString(), core.session.user.id, DateTime.MinValue);
        //
        //====================================================================================================
        //
        public override void AddUser(string GroupNameIdOrGuid, int UserId) => GroupController.addUser(core, GroupNameIdOrGuid, UserId, DateTime.MinValue);
        //
        //====================================================================================================
        //
        public override void AddUser(string GroupNameIdOrGuid, int UserId, DateTime DateExpires) => GroupController.addUser(core, GroupNameIdOrGuid, UserId, DateExpires);
        //
        //====================================================================================================
        //
        public override void AddUser(int GroupId, int UserId) => GroupController.addUser(core, GroupId, UserId, DateTime.MinValue);
        //
        //====================================================================================================
        //
        public override void AddUser(int GroupId, int UserId, DateTime DateExpires) => GroupController.addUser(core, GroupId.ToString(), UserId, DateExpires);
        //
        //====================================================================================================
        //
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
        //
        public override void Delete(int GroupId) => GroupModel.delete<GroupModel>(core.cpParent, GroupId);
        //
        //====================================================================================================
        //
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
        //
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
        public override string GetName(int GroupId)
            => DbBaseModel.getRecordName<GroupModel>(core.cpParent, GroupId);
        //
        //====================================================================================================
        //
        public override void RemoveUser(string GroupNameIdOrGuid, int removeUserId) {
            int groupId = GetId(GroupNameIdOrGuid);
            int userId = removeUserId;
            if (groupId != 0) {
                if (userId == 0) {
                    GroupController.removeUser(core, groupId);
                } else {
                    GroupController.removeUser(core, groupId, userId);
                }
            }
        }
        //
        //====================================================================================================
        //
        public override void RemoveUser(string GroupNameIdOrGuid) => RemoveUser(GroupNameIdOrGuid, 0);
        //
        //====================================================================================================
        /// <summary>
        /// debug log entry
        /// </summary>
        /// <param name="message"></param>
        private void appendDebugLog(string message)
            => Logger.Debug(LogController.getMessageLine(cp.core, message, false));
        //
        //====================================================================================================
        //
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPGroupClass() {
            Dispose(false);
        }
        protected virtual void Dispose(bool disposing_group) {
            if (!this.disposed_group) {
                appendDebugLog(".dispose, dereference cp, main, csv");
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
        protected bool disposed_group;
        #endregion
    }
}