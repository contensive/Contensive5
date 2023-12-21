
using System;
using static Contensive.Processor.Controllers.GenericController;
using System.Collections.Generic;
using System.Linq;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Controllers {
    /// <summary>
    /// workflow methods. Workflow editing is deprecated. This class handles edit locking
    /// </summary>
    public class WorkflowController : IDisposable {
        public enum AuthoringControls {
            /// <summary>
            /// record is being edted
            /// </summary>
            Editing = 1,
            /// <summary>
            /// record workflow deprecated
            /// </summary>
            Submitted = 2,
            /// <summary>
            /// record workflow deprecated
            /// </summary>
            Approved = 3,
            /// <summary>
            /// record workflow deprecated
            /// </summary>
            Modified = 4
        }
        //
        //==========================================================================================
        /// <summary>
        /// create the key (contentrecordkey) for the authoring controls table. 
        /// </summary>
        /// <param name="tableId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static string getTableRecordKey(int tableId, int recordId) => DbControllerX.encodeSQLText(tableId.ToString() + "/" + recordId.ToString());
        //
        //==========================================================================================
        /// <summary>
        /// create sql criteria for all authoring control records related to a tablerecordkey
        /// </summary>
        /// <param name="dateTimeMockable"></param>
        /// <param name="tableRecordKey"></param>
        /// <returns></returns>
        public static string getAuthoringControlCriteria(string tableRecordKey, DateTime dateTimeMockable)
            => "(contentRecordKey=" + tableRecordKey + ")and((DateExpires>" + DbControllerX.encodeSQLDate(dateTimeMockable) + ")or(DateExpires Is null))";
        //
        //==========================================================================================
        /// <summary>
        /// create sql criteria for a specific type of authoring control records related to a table/record
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="tableRecordKey"></param>
        /// <param name="ControlType"></param>
        /// <returns></returns>
        public static string getAuthoringControlCriteria(string tableRecordKey, AuthoringControls ControlType, DateTime dateTimeMockable)
            => "(controltype=" + (int)ControlType + ")and(" + getAuthoringControlCriteria(tableRecordKey, dateTimeMockable) + ")";
        //
        //=====================================================================================================
        /// <summary>
        /// Get the query to test the authoring control table if a record is locked
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        private static string getAuthoringControlCriteria(CoreController core, string contentName, int recordId)
            => getAuthoringControlCriteria(core, Models.Domain.ContentMetadataModel.createByUniqueName(core, contentName), recordId);
        //
        //=====================================================================================================
        /// <summary>
        /// Get the query to test the authoring control table if a record is locked
        /// </summary>
        /// <param name="cdef"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        private static string getAuthoringControlCriteria(CoreController core, Models.Domain.ContentMetadataModel cdef, int recordId) {
            if (cdef == null) { return "(1=0)"; }
            var table = DbBaseModel.createByUniqueName<TableModel>(core.cpParent, cdef.tableName);
            if (table == null) { return "(1=0)"; }
            return getAuthoringControlCriteria(getTableRecordKey(table.id, recordId),core.dateTimeNowMockable);
        }
        //
        //=================================================================================
        /// <summary>
        /// Get a record lock status. If session.user is the lock holder, returns unlocked
        /// </summary>
        /// <param name="core"></param>
        /// <param name="recordId"></param>
        /// <param name="tableId"></param>
        /// <returns></returns>
        public static editLockClass getEditLock(CoreController core, int tableId, int recordId) {
            try {
                var table = DbBaseModel.create<TableModel>(core.cpParent, tableId);
                if (table != null) {
                    //
                    // -- get the edit control for this record (not by this person) with the oldest expiration date
                    string criteria = "(createdby<>" + core.session.user.id + ")and" + getAuthoringControlCriteria(getTableRecordKey(table.id, recordId), AuthoringControls.Editing, core.dateTimeNowMockable);
                    var authoringControlList = DbBaseModel.createList<AuthoringControlModel>(core.cpParent, criteria, "dateexpires desc");
                    if (authoringControlList.Count > 0) {
                        var person = DbBaseModel.create<PersonModel>(core.cpParent, GenericController.encodeInteger(authoringControlList.First().createdBy));
                        return new editLockClass {
                            isEditLocked = true,
                            editLockExpiresDate = authoringControlList.First().dateExpires,
                            editLockByMemberId = (person == null) ? 0 : person.id,
                            editLockByMemberName = (person == null) ? "" : person.name
                        };
                    }
                };
            } catch (Exception ex) {
                LogControllerX.logError(core, ex);
            }
            return new editLockClass { isEditLocked = false };
        }
        //
        //========================================================================
        /// <summary>
        /// Returns true if the record is locked to the current member
        /// </summary>
        /// <param name="core"></param>
        /// <param name="tableId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static bool isRecordLocked(CoreController core, int tableId, int recordId) => getEditLock(core, tableId, recordId).isEditLocked;
        //
        //========================================================================
        /// <summary>
        /// Clears the edit lock for this record
        /// </summary>
        /// <param name="core"></param>
        /// <param name="tableId"></param>
        /// <param name="recordId"></param>
        public static void clearEditLock(CoreController core, int tableId, int recordId) {
            string criteria = "(contentRecordKey=" + getTableRecordKey(tableId, recordId) + ")";
            DbBaseModel.deleteRows<AuthoringControlModel>(core.cpParent, criteria);
        }
        //
        //=======================================================================
        /// <summary>
        /// Sets the edit lock for this record
        /// </summary>
        /// <param name="core"></param>
        /// <param name="tableId"></param>
        /// <param name="recordId"></param>
        public static void setEditLock(CoreController core, int tableId, int recordId) => setEditLock(core, tableId, recordId, core.session.user.id);
        //
        //=================================================================================
        /// <summary>
        /// Set a record locked
        /// </summary>
        /// <param name="core"></param>
        /// <param name="tableId"></param>
        /// <param name="recordId"></param>
        /// <param name="userId"></param>
        public static void setEditLock(CoreController core, int tableId, int recordId, int userId) {
            string contentRecordKey = getTableRecordKey(tableId, recordId);
            var editLockList = DbBaseModel.createList<AuthoringControlModel>(core.cpParent, "(contentRecordKey=" + contentRecordKey + ")");
            var editLock = (editLockList.Count > 0) ? editLockList.First() : DbBaseModel.addEmpty<AuthoringControlModel>(core.cpParent, userId);
            editLock.contentRecordKey = contentRecordKey;
            editLock.controlType = (int)AuthoringControls.Editing;
            editLock.createdBy = userId;
            editLock.dateAdded = core.dateTimeNowMockable;
            editLock.save(core.cpParent, userId);
        }
        //
        //=====================================================================================================
        /// <summary>
        /// the Approved Authoring Control
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordID"></param>
        /// <returns></returns>
        public static recordWorkflowStatusClass getWorkflowStatus(CoreController core, string ContentName, int RecordID) {
            recordWorkflowStatusClass result = new recordWorkflowStatusClass {
                isEditLocked = false,
                editLockByMemberName = "",
                editLockExpiresDate = DateTime.MinValue,
                workflowSubmittedMemberName = "",
                isWorkflowModified = false,
                workflowModifiedByMemberName = "",
                workflowModifiedDate = DateTime.MinValue,
                isWorkflowSubmitted = false,
                workflowSubmittedDate = DateTime.MinValue,
                isWorkflowApproved = false,
                workflowApprovedMemberName = "",
                workflowApprovedDate = DateTime.MinValue,
                isWorkflowDeleted = false,
                isWorkflowInserted = false
            };
            try {
                if (RecordID > 0) {
                    //
                    // Get Workflow Locks
                    Models.Domain.ContentMetadataModel CDef = Models.Domain.ContentMetadataModel.createByUniqueName(core, ContentName);
                    if (CDef.id > 0) {
                        var nameDict = new Dictionary<int, string>();
                        foreach (var recordLock in DbBaseModel.createList<AuthoringControlModel>(core.cpParent, getAuthoringControlCriteria(core, ContentName, RecordID))) {
                            int createdBy = GenericController.encodeInteger(recordLock.createdBy);
                            switch ((AuthoringControls)recordLock.controlType) {
                                case AuthoringControls.Editing:
                                    if (!result.isEditLocked) {
                                        result.isEditLocked = true;
                                        result.editLockExpiresDate = recordLock.dateAdded;
                                        if (nameDict.ContainsKey(createdBy)) {
                                            result.editLockByMemberName = nameDict[createdBy];
                                        } else {
                                            result.editLockByMemberName = DbBaseModel.getRecordName<PersonModel>(core.cpParent, createdBy);
                                            nameDict.Add(createdBy, result.workflowModifiedByMemberName);
                                        }
                                    }
                                    break;
                                case AuthoringControls.Modified:
                                    if (!result.isWorkflowModified) {
                                        result.isWorkflowModified = true;
                                        result.workflowSubmittedDate = recordLock.dateAdded;
                                        if (nameDict.ContainsKey(createdBy)) {
                                            result.workflowModifiedByMemberName = nameDict[createdBy];
                                        } else {
                                            result.workflowModifiedByMemberName = DbBaseModel.getRecordName<PersonModel>(core.cpParent, createdBy);
                                            nameDict.Add(createdBy, result.workflowModifiedByMemberName);
                                        }
                                    }
                                    break;
                                case AuthoringControls.Submitted:
                                    if (!result.isWorkflowSubmitted) {
                                        result.isWorkflowSubmitted = true;
                                        result.workflowModifiedDate = recordLock.dateAdded;
                                        if (nameDict.ContainsKey(createdBy)) {
                                            result.workflowSubmittedMemberName = nameDict[createdBy];
                                        } else {
                                            result.workflowSubmittedMemberName = DbBaseModel.getRecordName<PersonModel>(core.cpParent, createdBy);
                                            nameDict.Add(createdBy, result.workflowSubmittedMemberName);
                                        }
                                    }
                                    break;
                                case AuthoringControls.Approved:
                                    if (!result.isWorkflowApproved) {
                                        result.isWorkflowApproved = true;
                                        result.workflowApprovedDate = encodeDate(recordLock.dateAdded);
                                        if (nameDict.ContainsKey(createdBy)) {
                                            result.workflowApprovedMemberName = nameDict[createdBy];
                                        } else {
                                            result.workflowApprovedMemberName = DbBaseModel.getRecordName<PersonModel>(core.cpParent, createdBy);
                                            nameDict.Add(createdBy, result.workflowSubmittedMemberName);
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogControllerX.logError(core, ex);
                throw;
            }
            return result;
        }
        //
        //=================================================================================
        /// <summary>
        /// The workflow and edit locking status of the record.
        /// </summary>
        public class recordWorkflowStatusClass {
            /// <summary>
            /// The record is locked because it is being edited
            /// </summary>
            public bool isEditLocked { get; set; }
            /// <summary>
            /// The record is being editing by this user
            /// </summary>
            public string editLockByMemberName { get; set; }
            /// <summary>
            /// The date and time when the record will be released from editing lock
            /// </summary>
            public DateTime? editLockExpiresDate { get; set; }
            /// <summary>
            /// For deprected workflow editing. This user has submitted this record for publication
            /// </summary>
            public string workflowSubmittedMemberName { get; set; }
            /// <summary>
            /// For deprected workflow editing. This user has approved this record for publication
            /// </summary>
            public string workflowApprovedMemberName { get; set; }
            /// <summary>
            /// For deprected workflow editing. This user last saved changes to this record
            /// </summary>
            public string workflowModifiedByMemberName { get; set; }
            /// <summary>
            /// For deprected workflow editing. The record is locked because it has been submitted for publication
            /// </summary>
            public bool isWorkflowSubmitted { get; set; }
            /// <summary>
            /// For deprected workflow editing. The record is locked because it has been approved for publication
            /// </summary>
            public bool isWorkflowApproved { get; set; }
            /// <summary>
            /// For deprected workflow editing. The record has been modified
            /// </summary>
            public bool isWorkflowModified { get; set; }
            /// <summary>
            /// For deprected workflow editing. The date and time when the record was modified
            /// </summary>
            public DateTime? workflowModifiedDate { get; set; }
            /// <summary>
            /// For deprected workflow editing. The date and time when the record was submitted for publishing
            /// </summary>
            public DateTime? workflowSubmittedDate { get; set; }
            /// <summary>
            /// For deprected workflow editing. The date and time when the record was approved for publishing
            /// </summary>
            public DateTime workflowApprovedDate { get; set; }
            /// <summary>
            /// For deprected workflow editing.  The record has been inserted but not published
            /// </summary>
            public bool isWorkflowInserted { get; set; }
            /// <summary>
            /// For deprected workflow editing. The record has been deleted but not published
            /// </summary>
            public bool isWorkflowDeleted { get; set; }
        }

        //
        //=================================================================================
        /// <summary>
        /// The workflow and edit locking status of the record.
        /// </summary>
        public class editLockClass {
            /// <summary>
            /// The record is locked because it is being edited
            /// </summary>
            public bool isEditLocked { get; set; }
            /// <summary>
            /// The record is being editing by this user
            /// </summary>
            public int editLockByMemberId { get; set; }
            /// <summary>
            /// The record is being editing by this user
            /// </summary>
            public string editLockByMemberName { get; set; }
            /// <summary>
            /// The date and time when the record will be released from editing lock
            /// </summary>
            public DateTime? editLockExpiresDate { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //==========================================================================================
        #region  IDisposable Support 
        protected bool disposed;
        //
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    //
                    // ----- call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~WorkflowController()  {
            Dispose(false);


        }
        #endregion
    }
}

