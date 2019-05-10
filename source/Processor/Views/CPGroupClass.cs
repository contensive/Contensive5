﻿
using System;
using Contensive.Processor.Models.Db;
using Contensive.Processor.Controllers;

namespace Contensive.Processor {
    //
    // comVisible to be activeScript compatible
    //
    //[ComVisible(true), Microsoft.VisualBasic.ComClass(CPGroupClass.ClassId, CPGroupClass.InterfaceId, CPGroupClass.EventsId)]
    public class CPGroupClass : BaseClasses.CPGroupBaseClass, IDisposable {
        //
        #region COM GUIDs
        public const string ClassId = "C0000B6E-5ABF-4C67-9F22-EF52D73FC54B";
        public const string InterfaceId = "DB74B7D9-73BE-40C1-B488-ACC098E8B9C1";
        public const string EventsId = "B9E3C450-CDC4-4590-8BCD-FEDDF7338D4B";
        #endregion
        //
        private Contensive.Processor.Controllers.CoreController core;
        private CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cpParent"></param>
        public CPGroupClass(CPClass cpParent) : base() {
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
            if ( GenericController.isGuid(GroupNameIdOrGuid )) {
                GroupModel.delete(core, GroupNameIdOrGuid);
                return;
            }
            if ( GroupNameIdOrGuid.IsNumeric()) {
                GroupModel.delete(core, GenericController.encodeInteger( GroupNameIdOrGuid));
                return;
            }
            GroupModel.delete(core, "name=" + DbController.encodeSQLText(GroupNameIdOrGuid));
        }
        //
        //====================================================================================================
        //
        public override void Delete(int GroupId) => GroupModel.delete(core, GroupId);
        //
        //====================================================================================================
        //
        public override int GetId(string GroupNameOrGuid) {
            GroupModel group;
            if (GenericController.isGuid(GroupNameOrGuid)) {
                group = GroupModel.create(cp.core, GroupNameOrGuid);
            } else {
                group = GroupModel.createByUniqueName(cp.core, GroupNameOrGuid);
            }
            if (group != null) { return group.id; }
            return 0;
        }
        //
        //====================================================================================================
        //
        public override string GetName(string GroupIdOrGuid) {
            if (GroupIdOrGuid.IsNumeric()) {
                return DbBaseModel.getRecordName<GroupModel>(core, GenericController.encodeInteger(GroupIdOrGuid));
            } else {
                return DbBaseModel.getRecordName<GroupModel>(core, GroupIdOrGuid);
            }
        }
        public override string GetName(int GroupId) 
            => DbBaseModel.getRecordName<GroupModel>(core, GroupId);
        //
        //====================================================================================================
        //
        public override void RemoveUser(string GroupNameIdOrGuid, int removeUserId) {
            int groupID = GetId(GroupNameIdOrGuid);
            int userId = removeUserId;
            if (groupID != 0) {
                if (userId == 0) {
                    cp.Content.Delete("Member Rules", "((memberid=" + cp.User.Id.ToString() + ")and(groupid=" + groupID.ToString() + "))");
                } else {
                    cp.Content.Delete("Member Rules", "((memberid=" + removeUserId.ToString() + ")and(groupid=" + groupID.ToString() + "))");
                }
            }
        }
        //
        //====================================================================================================
        //
        public override void RemoveUser(string GroupNameIdOrGuid) => RemoveUser(GroupNameIdOrGuid, 0);
        //
        //====================================================================================================
        //
        private void appendDebugLog(string copy) => LogController.logDebug(core, copy);
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
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                appendDebugLog(".dispose, dereference cp, main, csv");
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }
        protected bool disposed = false;
        #endregion
    }
}