
using System;
using System.Collections.Generic;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class PermissionController : IDisposable {
        //
        //====================================================================================================
        /// <summary>
        /// Permissions this user has for this content.
        /// </summary>
        public static UserContentPermissions getUserContentPermissions(CoreController core, ContentMetadataModel cdef) {
            var result = new UserContentPermissions {
                allowDelete = false,
                allowAdd = false,
                allowSave = false,
                allowEdit = false
            };
            try {
                if ((!core.session.isAuthenticated) || (cdef == null)) {
                    //
                    // -- exit with no rights
                    return result;
                }
                if (core.session.isAuthenticatedDeveloper()) {
                    //
                    // developers are always content managers
                    result.allowEdit = true;
                    result.allowSave = true;
                    result.allowAdd = cdef.allowAdd;
                    result.allowDelete = cdef.allowDelete;
                } else if (core.session.isAuthenticatedAdmin()) {
                    //
                    // admin is content manager if the CDef is not developer only
                    if (!cdef.developerOnly) {
                        result.allowEdit = true;
                        result.allowSave = true;
                        result.allowAdd = cdef.allowAdd;
                        result.allowDelete = cdef.allowDelete;
                    }
                } else {
                    //
                    // Authenticated and not admin or developer
                    result = getUserAuthoringPermissions_ContentManager(core, cdef, new List<int>());
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Checks if the member is a content manager for the specific content, Which includes transversing up the tree to find the next rule that applies. Member must be checked for authenticated and main_IsAdmin already
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentId"></param>
        /// <param name="returnAllowEdit"></param>
        /// <param name="returnAllowAdd"></param>
        /// <param name="returnAllowDelete"></param>
        /// <param name="usedContentIdList"></param>
        //========================================================================
        //
        private static UserContentPermissions getUserAuthoringPermissions_ContentManager(CoreController core, ContentMetadataModel cdef, List<int> usedContentIdList) {
            var result = new UserContentPermissions {
                allowAdd = false,
                allowDelete = false,
                allowEdit = false,
                allowSave = false
            };
            try {
                if (usedContentIdList.Contains(cdef.id)) {
                    //
                    // failed usedContentIdList test, this content id was in the child path
                    //
                    throw new ArgumentException("ContentID [" + cdef.id + "] was found to be in it's own parentid path.");
                } else if (cdef.id < 1) {
                    //
                    // ----- not a valid contentname
                    //
                } else if (core.doc.contentAccessRights_NotList.Contains(cdef.id)) {
                    //
                    // ----- was previously found to not be a Content Manager
                    //
                } else if (core.doc.contentAccessRights_List.Contains(cdef.id)) {
                    //
                    // ----- was previously found to be a Content Manager
                    //
                    result.allowEdit = true;
                    result.allowSave = true;
                    result.allowAdd = core.doc.contentAccessRights_AllowAddList.Contains(cdef.id);
                    result.allowDelete = core.doc.contentAccessRights_AllowDeleteList.Contains(cdef.id);
                } else {
                    //
                    // ----- Must test it
                    //
                    string SQL = "SELECT ccGroupRules.ContentID,allowAdd,allowDelete"
                    + " FROM ccGroupRules RIGHT JOIN ccMemberRules ON ccGroupRules.GroupId = ccMemberRules.GroupID"
                    + " WHERE ("
                        + " (ccMemberRules.memberId=" + DbController.encodeSQLNumber(core.session.user.id) + ")"
                        + " AND(ccMemberRules.active<>0)"
                        + " AND(ccGroupRules.active<>0)"
                        + " AND(ccGroupRules.ContentID=" + cdef.id + ")"
                        + " AND((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" + DbController.encodeSQLDate(core.doc.profileStartTime) + "))"
                        + ");";
                    using (var csData = new CsModel(core)) {
                        csData.openSql(SQL);
                        if (csData.ok()) {
                            result.allowEdit = true;
                            result.allowSave = true;
                            result.allowAdd = csData.getBoolean("allowAdd");
                            result.allowDelete = csData.getBoolean("allowDelete");
                        }
                    }
                    //
                    if (!result.allowEdit) {
                        //
                        // ----- Not a content manager for this one, check the parent
                        if (cdef.parentId > 0) {
                            var parentCdef = ContentMetadataModel.create(core, cdef.parentId);
                            usedContentIdList.Add(cdef.id);
                            getUserAuthoringPermissions_ContentManager(core, cdef, usedContentIdList);
                        }
                    }
                    if (result.allowEdit) {
                        //
                        // ----- Was found to be true
                        //
                        core.doc.contentAccessRights_List.Add(cdef.id);
                        if (result.allowEdit) {
                            core.doc.contentAccessRights_AllowAddList.Add(cdef.id);
                        }
                        if (result.allowDelete) {
                            core.doc.contentAccessRights_AllowDeleteList.Add(cdef.id);
                        }
                    } else {
                        //
                        // ----- Was found to be false
                        //
                        core.doc.contentAccessRights_NotList.Add(cdef.id);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return result;
        }
        //
        /// <summary>
        /// generic permissions a user might have with content
        /// </summary>
        public class UserContentPermissions {
            /// <summary>
            /// this user can add new records to this content
            /// </summary>
            public bool allowAdd { get; set; }
            /// <summary>
            /// this user can edit records in this content
            /// </summary>
            public bool allowEdit { get; set; }
            /// <summary>
            /// this user can save the record
            /// </summary>
            public bool allowSave { get; set; }
            /// <summary>
            /// this user can delete the record
            /// </summary>
            public bool allowDelete { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
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
        public void Dispose()  {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~PermissionController()  {
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