
using System;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Methods and Properties for Groups
    /// </summary>
    public abstract class CPGroupBaseClass {
        //
        //==========================================================================================
        /// <summary>
        /// Add a new group
        /// </summary>
        /// <param name="groupName"></param>
        public abstract void Add(string groupName);
        //
        //==========================================================================================
        /// <summary>
        /// Add a new group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="groupCaption"></param>
        public abstract void Add(string groupName, string groupCaption);
        //
        //==========================================================================================
        /// <summary>
        /// Add the current user to a group.
        /// </summary>
        /// <param name="groupId"></param>
        public abstract void AddUser(int groupId);
        //
        //==========================================================================================
        /// <summary>
        /// Add the current user to a group. If argument is numeric, record is referenced by Id. If argument is guid, record is referenced by ccGuid. Otherwise argument is name.
        /// </summary>
        /// <param name="groupNameIdOrGuid"></param>
        public abstract void AddUser(string groupNameIdOrGuid);
        //
        //==========================================================================================
        /// <summary>
        /// Add a user to a group. If argument is numeric, record is referenced by Id. If argument is guid, record is referenced by ccGuid. Otherwise argument is name.
        /// </summary>
        /// <param name="groupNameIdOrGuid"></param>
        /// <param name="userId"></param>
        public abstract void AddUser(string groupNameIdOrGuid, int userId);
        //
        //==========================================================================================
        /// <summary>
        /// Add a user to a group. If argument is numeric, record is referenced by Id. If argument is guid, record is referenced by ccGuid. Otherwise argument is name.
        /// </summary>
        /// <param name="groupNameIdOrGuid"></param>
        /// <param name="userId"></param>
        /// <param name="dateExpires"></param>
        public abstract void AddUser(string groupNameIdOrGuid, int userId, DateTime dateExpires);
        //
        //==========================================================================================
        /// <summary>
        /// Add a user to a group.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="userId"></param>
        public abstract void AddUser(int groupId, int userId);
        //
        //==========================================================================================
        /// <summary>
        /// Add a user to a group.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="userId"></param>
        /// <param name="dateExpires"></param>
        public abstract void AddUser(int groupId, int userId, DateTime dateExpires);
        //
        //==========================================================================================
        /// <summary>
        /// Delete a group. If argument is numeric, record is referenced by Id. If argument is guid, record is referenced by ccGuid. Otherwise argument is name.
        /// </summary>
        /// <param name="groupNameIdOrGuid"></param>
        public abstract void Delete(string groupNameIdOrGuid);
        //
        //==========================================================================================
        /// <summary>
        /// Delete a group.
        /// </summary>
        /// <param name="groupId"></param>
        public abstract void Delete(int groupId);
        //
        //==========================================================================================
        /// <summary>
        /// Get a group Id. If argument is guid, record is referenced by ccGuid. Otherwise argument is name.
        /// </summary>
        /// <param name="groupNameOrGuid"></param>
        /// <returns></returns>
        public abstract int GetId(string groupNameOrGuid);
        //
        //==========================================================================================
        /// <summary>
        /// Get a group name. If argument is numeric, record is referenced by Id. Otherwise record is referenced by ccGuid.
        /// </summary>
        /// <param name="GroupIdOrGuid"></param>
        /// <returns></returns>
        public abstract string GetName(string GroupIdOrGuid);
        //
        //==========================================================================================
        /// <summary>
        /// Get a group Name
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public abstract string GetName(int groupId);
        //
        //==========================================================================================
        /// <summary>
        /// Remove the current user from a group
        /// </summary>
        /// <param name="groupNameOrGuid"></param>
        public abstract void RemoveUser(string groupNameOrGuid);
        //
        //==========================================================================================
        /// <summary>
        /// Remove the current user from a group
        /// </summary>
        /// <param name="groupId"></param>
        public abstract void RemoveUser(int groupId);
        //
        //==========================================================================================
        /// <summary>
        /// Remove a user from a group
        /// </summary>
        /// <param name="groupNameOrGuid"></param>
        /// <param name="userId"></param>
        public abstract void RemoveUser(string groupNameOrGuid, int userId);
        //
        //==========================================================================================
        /// <summary>
        /// Remove a user from a group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="userId"></param>
        public abstract void RemoveUser(int groupId, int userId);
        //
        //==========================================================================================
        /// <summary>
        /// Verify a group exists, create it if it does not
        /// </summary>
        /// <param name="groupGuid"></param>
        /// <param name="groupName"></param>
        public abstract void verifyGroup(string groupGuid, string groupName);
        //
        //==========================================================================================
        /// <summary>
        /// Verify a group exists, create it if it does not
        /// </summary>
        /// <param name="groupGuid"></param>
        /// <param name="groupName"></param>
        /// <param name="groupCaption"></param>
        public abstract void verifyGroup(string groupGuid, string groupName, string groupCaption);
        //
        //==========================================================================================
        /// <summary>
        /// Verify a group exists, create it if it does not
        /// </summary>
        /// <param name="groupGuid"></param>
        public abstract bool exists(string groupGuid);
        //
        //====================================================================================================
        // deprecated
        //
    }
}

