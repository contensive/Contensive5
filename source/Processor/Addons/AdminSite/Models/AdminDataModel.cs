
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.AdminSite.Models;
using Contensive.Processor.Controllers;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// object that contains the context for the admin site, like recordsPerPage, etc. Should eventually include the loadContext and be its own document
    /// </summary>
    public class AdminDataModel {
        //
        //====================================================================================================
        //
        private CoreController core { get; }
        //
        //====================================================================================================
        /// <summary>
        /// the content metadata being viewed. Typically set with the query cid=123
        /// </summary>
        public ContentMetadataModel adminContent { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// the record being edited, typically set with query cid=123,id=123,af=4
        /// </summary>
        public EditRecordModel editRecord { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The form that submitted that the button to process
        /// </summary>
        public int srcFormId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Value returned from a submit button, process into action/form
        /// </summary>
        public string srcFormButton { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// the next form requested (the get). typically set with af=123
        /// </summary>
        public int dstFormId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// true if there was an error loading the edit record - use to block the edit form
        /// </summary>
        public bool blockEditForm { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The action to be performed before the next form
        /// </summary>
        public int admin_Action { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// for passing where clause values from page to page, key should be lowercase
        /// </summary>
        internal Dictionary<string, string> wherePair { get; set; } = new Dictionary<string, string>();
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int listViewRecordTop { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int listViewRecordsPerPage { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// String that adds on to the end of the title
        /// </summary>
        public string editViewTitleSuffix { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// this is a hidden on the edit form. The popup editor preferences sets this hidden and submits
        /// </summary>
        public string fieldEditorPreference { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// flag set that shows the rest are valid
        /// </summary>
        public bool contentWatchLoaded { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int contentWatchRecordID { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string contentWatchLink { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int contentWatchClicks { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string contentWatchLinkLabel { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public DateTime contentWatchExpires { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// list of all ContentWatchLists for this Content, read from response, then later saved to Rules
        /// </summary>
        public int[] contentWatchListID {
            get {
                return _ContentWatchListID;
            }
            set {
                _ContentWatchListID = value;
            }
        }
        private int[] _ContentWatchListID;
        //
        //====================================================================================================
        /// <summary>
        /// size of ContentWatchListID() array
        /// </summary>
        public int contentWatchListIDSize { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// number of valid entries in ContentWatchListID()
        /// </summary>
        public int contentWatchListIDCount { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Count of Buttons in use
        /// </summary>
        public int buttonObjectCount { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public bool userAllowContentEdit { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// used to generate labels for form input
        /// </summary>
        public int formInputCount { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int editSectionPanelCount { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string OpenLiveWindowTable = "<div ID=\"LiveWindowTable\">";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string CloseLiveWindowTable = "</div>";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string AdminFormErrorOpen = "<table border=\"0\" cellpadding=\"20\" cellspacing=\"0\" width=\"100%\"><tr><td align=\"left\">";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string AdminFormErrorClose = "</td></tr></table>";
        //
        //====================================================================================================
        /// <summary>
        /// these were defined different in csv
        /// </summary>
        public enum NodeTypeEnum {
            NodeTypeEntry = 0,
            NodeTypeCollection = 1,
            NodeTypeAddon = 2,
            NodeTypeContent = 3
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string IndexConfigPrefix = "IndexConfig:";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public bool allowAdminFieldCheck(CoreController core) {
            if (AllowAdminFieldCheck_Local == null) {
                AllowAdminFieldCheck_Local = core.siteProperties.getBoolean("AllowAdminFieldCheck", true);
            }
            return (bool)AllowAdminFieldCheck_Local;
        }
        private bool? AllowAdminFieldCheck_Local = null;
        //
        //====================================================================================================
        /// <summary>
        /// Read in Whats New values if present, Field values must be loaded
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminInfo"></param>
        public void loadContentTrackingDataBase(CoreController core) {
            try {
                int ContentID = 0;
                //
                // ----- check if admin record is present
                //
                if ((editRecord.id != 0) && (adminContent.allowContentTracking)) {
                    //
                    // ----- Open the content watch record for this content record
                    //
                    ContentID = ((editRecord.contentControlId.Equals(0)) ? adminContent.id : editRecord.contentControlId);
                    using var csData = new CsModel(core); csData.open("Content Watch", "(ContentID=" + DbController.encodeSQLNumber(ContentID) + ")AND(RecordID=" + DbController.encodeSQLNumber(editRecord.id) + ")");
                    if (csData.ok()) {
                        contentWatchLoaded = true;
                        contentWatchRecordID = (csData.getInteger("ID"));
                        contentWatchLink = (csData.getText("Link"));
                        contentWatchClicks = (csData.getInteger("Clicks"));
                        contentWatchLinkLabel = (csData.getText("LinkLabel"));
                        contentWatchExpires = (csData.getDate("WhatsNewDateExpires"));
                        csData.close();
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //========================================================================
        /// <summary>
        /// Read in Whats New values if present
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminContext"></param>
        public void loadContentTrackingResponse(CoreController core) {
            try {
                int RecordId = 0;
                contentWatchListIDCount = 0;
                if ((core.docProperties.getText("WhatsNewResponse") != "") && (adminContent.allowContentTracking)) {
                    //
                    // ----- set single fields
                    //
                    contentWatchLinkLabel = core.docProperties.getText("ContentWatchLinkLabel");
                    contentWatchExpires = core.docProperties.getDate("ContentWatchExpires");
                    //
                    // ----- Update ContentWatchListRules for all checked boxes
                    //
                    using var csData = new CsModel(core); csData.open("Content Watch Lists");
                    while (csData.ok()) {
                        RecordId = (csData.getInteger("ID"));
                        if (core.docProperties.getBoolean("ContentWatchList." + RecordId)) {
                            if (contentWatchListIDCount >= contentWatchListIDSize) {
                                contentWatchListIDSize += 50;
                                Array.Resize(ref _ContentWatchListID, contentWatchListIDSize);
                            }
                            contentWatchListID[contentWatchListIDCount] = RecordId;
                            contentWatchListIDCount += 1;
                        }
                        csData.goNext();
                    }
                    csData.close();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="AdminOnly"></param>
        /// <param name="DeveloperOnly"></param>
        /// <param name="Active"></param>
        /// <param name="Authorable"></param>
        /// <param name="Name"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public static bool isVisibleUserField(CoreController core, bool AdminOnly, bool DeveloperOnly, bool Active, bool Authorable, string Name, string TableName) {
            bool tempIsVisibleUserField = false;
            try {
                bool HasEditRights = false;
                //
                tempIsVisibleUserField = false;
                if ((TableName.ToLowerInvariant() == "ccpagecontent") && (Name.ToLowerInvariant() == "linkalias")) {
                    //
                    // ccpagecontent.linkalias is a control field that is not in control tab
                    //
                } else {
                    switch (GenericController.toUCase(Name)) {
                        case "ACTIVE":
                        case "ID":
                        case "CONTENTCONTROLID":
                        case "CREATEDBY":
                        case "DATEADDED":
                        case "MODIFIEDBY":
                        case "MODIFIEDDATE":
                        case "CREATEKEY":
                        case "CCGUID": {
                                //
                                // ----- control fields are not editable user fields
                                //
                                break;
                            }
                        default: {
                                //
                                // ----- test access
                                //
                                HasEditRights = true;
                                if (AdminOnly || DeveloperOnly) {
                                    //
                                    // field has some kind of restriction
                                    //
                                    if (!core.session.user.developer) {
                                        if (!core.session.user.admin) {
                                            //
                                            // you are not admin
                                            //
                                            HasEditRights = false;
                                        } else if (DeveloperOnly) {
                                            //
                                            // you are admin, and the record is developer
                                            //
                                            HasEditRights = false;
                                        }
                                    }
                                }
                                if ((HasEditRights) && (Active) && (Authorable)) {
                                    tempIsVisibleUserField = true;
                                }
                                break;
                            }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempIsVisibleUserField;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Test Content Access -- return based on Admin/Developer/MemberRules, if developer, let all through, if admin, block if table is developeronly 
        /// if member, run blocking query (which also traps adminonly and developer only), if blockin query has a null RecordID, this member gets everything
        /// if not null recordid in blocking query, use RecordIDs in result for Where clause on this lookup
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ContentID"></param>
        /// <returns></returns>
        public static bool userHasContentAccess(CoreController core, int ContentID) {
            try {
                if (core.session.isAuthenticatedAdmin()) { return true; }
                //
                ContentMetadataModel cdef = ContentMetadataModel.create(core, ContentID);
                if (cdef != null) {
                    return core.session.isAuthenticatedContentManager(cdef.name);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return false;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// constructor - load the context for the admin site, controlled by request inputs like rnContent (cid) and rnRecordId (id)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="request"></param>
        public AdminDataModel(CoreController core, AdminDataRequest request) {
            try {
                this.core = core;
                //
                // adminContext.content init
                if (request.contentId != 0) {
                    adminContent = ContentMetadataModel.create(core, request.contentId);
                    if (adminContent == null) {
                        adminContent = new ContentMetadataModel {
                            id = 0
                        };
                        Processor.Controllers.ErrorController.addUserError(core, "There is no content with the requested id [" + request.contentId + "]");
                    }
                }
                if (adminContent == null) {
                    adminContent = new ContentMetadataModel();
                }
                //
                // determine user rights to this content
                userAllowContentEdit = true;
                if (!core.session.isAuthenticatedAdmin()) {
                    if (adminContent.id > 0) {
                        userAllowContentEdit = userHasContentAccess(core, adminContent.id);
                    }
                }
                //
                // editRecord init
                //
                editRecord = new EditRecordModel {
                    loaded = false
                };
                if (userAllowContentEdit && !adminContent.id.Equals(0)) {
                    int requestedRecordId = request.id;
                    if (!requestedRecordId.Equals(0)) {
                        using var csData = new CsModel(core); csData.openRecord(adminContent.name, requestedRecordId, "id,contentControlId");
                        if (csData.ok()) {
                            editRecord.id = csData.getInteger("id");
                            //
                            // -- if this record is within a sub-content, reload adminContent
                            int recordContentId = csData.getInteger("contentControlId");
                            if ((recordContentId > 0) && (recordContentId != adminContent.id)) {
                                adminContent = ContentMetadataModel.create(core, recordContentId);
                            }
                        }
                        csData.close();
                    }
                    if (editRecord.id.Equals(0)) {
                        string requestedGuid = request.guid;
                        if (!string.IsNullOrWhiteSpace(requestedGuid) && isGuid(requestedGuid)) {
                            using var csData = new CsModel(core); csData.open(adminContent.name, "(ccguid=" + DbController.encodeSQLText(requestedGuid) + ")", "id", false, core.session.user.id, "id,contentControlId");
                            if (csData.ok()) {
                                editRecord.id = csData.getInteger("id");
                                //
                                // -- if this record is within a sub-content, reload adminContent
                                int recordContentId = csData.getInteger("contentControlId");
                                if ((recordContentId > 0) && (recordContentId != adminContent.id)) {
                                    adminContent = ContentMetadataModel.create(core, recordContentId);
                                }
                            }
                            csData.close();
                        }
                    }
                }
                //
                // Other page control fields
                //
                editViewTitleSuffix = request.titleExtension;
                listViewRecordTop = request.recordTop;
                listViewRecordsPerPage = request.recordsPerPage;
                if (listViewRecordsPerPage == 0) {
                    listViewRecordsPerPage = Constants.RecordsPerPageDefault;
                }
                //
                // Read WherePairCount
                int wherePairCount = 0;
                foreach (var nvp in request.wherePairDict) {
                    wherePair.Add(nvp.Key.ToLowerInvariant(), nvp.Value);
                    core.doc.addRefreshQueryString("wl" + wherePairCount, GenericController.encodeRequestVariable(nvp.Key));
                    core.doc.addRefreshQueryString("wr" + wherePairCount, GenericController.encodeRequestVariable(nvp.Value));
                    wherePairCount += 1;
                }
                //
                // ----- Other
                //
                admin_Action = request.adminAction;
                srcFormId = request.adminSourceForm;
                dstFormId = request.adminForm;
                srcFormButton = request.adminButton;
                //
                // ----- convert fieldEditorPreference change to a refresh action
                //
                if (adminContent.id != 0) {
                    fieldEditorPreference = request.fieldEditorPreference;
                    if (fieldEditorPreference != "") {
                        //
                        // Editor Preference change attempt. Set new preference and set this as a refresh
                        //
                        srcFormButton = "";
                        admin_Action = Constants.AdminActionEditRefresh;
                        dstFormId = AdminFormEdit;
                        int Pos = GenericController.strInstr(1, fieldEditorPreference, ":");
                        if (Pos > 0) {
                            int fieldEditorFieldId = GenericController.encodeInteger(fieldEditorPreference.left(Pos - 1));
                            int fieldEditorAddonId = GenericController.encodeInteger(fieldEditorPreference.Substring(Pos));
                            if (fieldEditorFieldId != 0) {
                                bool editorOk = true;
                                string SQL = "select id from ccfields where (active<>0) and id=" + fieldEditorFieldId;
                                DataTable dtTest = core.db.executeQuery(SQL);
                                if (dtTest.Rows.Count == 0) {
                                    editorOk = false;
                                }
                                if (editorOk && (fieldEditorAddonId != 0)) {
                                    SQL = "select id from ccaggregatefunctions where (active<>0) and id=" + fieldEditorAddonId;
                                    dtTest = core.db.executeQuery(SQL);
                                    if (dtTest.Rows.Count == 0) {
                                        editorOk = false;
                                    }
                                }
                                if (editorOk) {
                                    string Key = "editorPreferencesForContent:" + adminContent.id;
                                    //
                                    string editorpreferences = core.userProperty.getText(Key, "");
                                    if (!string.IsNullOrEmpty(editorpreferences)) {
                                        //
                                        // remove current preferences for this field
                                        //
                                        string[] Parts = ("," + editorpreferences).Split(new[] { "," + fieldEditorFieldId + ":" }, StringSplitOptions.None);
                                        int Cnt = Parts.GetUpperBound(0) + 1;
                                        if (Cnt > 0) {
                                            int Ptr = 0;
                                            for (Ptr = 1; Ptr < Cnt; Ptr++) {
                                                Pos = GenericController.strInstr(1, Parts[Ptr], ",");
                                                if (Pos == 0) {
                                                    Parts[Ptr] = "";
                                                } else if (Pos > 0) {
                                                    Parts[Ptr] = Parts[Ptr].Substring(Pos);
                                                }
                                            }
                                        }
                                        editorpreferences = string.Join("", Parts);
                                    }
                                    editorpreferences = editorpreferences + "," + fieldEditorFieldId + ":" + fieldEditorAddonId;
                                    core.userProperty.setProperty(Key, editorpreferences);
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        // ====================================================================================================
        /// <summary>
        /// Load Array, Get defaults if no record ID, Then load in any response elements
        /// </summary>
        /// <param name="core"></param>
        /// <param name="CheckUserErrors"></param>
        public void loadEditRecord(CoreController core, bool CheckUserErrors) {
            try {
                // todo refactor out
                if (string.IsNullOrEmpty(adminContent.name)) {
                    //
                    // Can not load edit record because bad content definition
                    //
                    if (adminContent.id == 0) {
                        throw (new Exception("The record can not be edited because no content definition was specified."));
                    } else {
                        throw (new Exception("The record can not be edited because a content definition For ID [" + adminContent.id + "] was not found."));
                    }
                } else {
                    //
                    if (editRecord.id == 0) {
                        //
                        // ----- New record, just load defaults
                        //
                        loadEditRecord_Default(core);
                        loadEditRecord_WherePairs(core);
                    } else {
                        //
                        // ----- Load the Live Record specified
                        //
                        loadEditRecord_Dbase(core, CheckUserErrors);
                        loadEditRecord_WherePairs(core);
                    }
                    //
                    // ----- Capture core fields needed for processing
                    //
                    editRecord.menuHeadline = "";
                    if (editRecord.fieldsLc.ContainsKey("menuheadline")) {
                        editRecord.menuHeadline = GenericController.encodeText(editRecord.fieldsLc["menuheadline"].value);
                    }
                    //
                    if (editRecord.fieldsLc.ContainsKey("name")) {
                        editRecord.nameLc = GenericController.encodeText(editRecord.fieldsLc["name"].value);
                    }
                    //
                    if (editRecord.fieldsLc.ContainsKey("active")) {
                        editRecord.active = GenericController.encodeBoolean(editRecord.fieldsLc["active"].value);
                    }
                    //
                    if (editRecord.fieldsLc.ContainsKey("contentcontrolid")) {
                        editRecord.contentControlId = GenericController.encodeInteger(editRecord.fieldsLc["contentcontrolid"].value);
                    }
                    //
                    if (editRecord.fieldsLc.ContainsKey("parentid")) {
                        editRecord.parentId = GenericController.encodeInteger(editRecord.fieldsLc["parentid"].value);
                    }
                    //
                    // ----- Set the local global copy of Edit Record Locks
                    var table = TableModel.createByContentName(core.cpParent, adminContent.name);
                    WorkflowController.recordWorkflowStatusClass authoringStatus = WorkflowController.getWorkflowStatus(core, adminContent.name, editRecord.id);
                    editRecord.editLock = WorkflowController.getEditLock(core, table.id, editRecord.id);
                    editRecord.submitLock = authoringStatus.isWorkflowSubmitted;
                    editRecord.submittedName = authoringStatus.workflowSubmittedMemberName;
                    editRecord.submittedDate = encodeDate(authoringStatus.workflowSubmittedDate);
                    editRecord.approveLock = authoringStatus.isWorkflowApproved;
                    editRecord.approvedName = authoringStatus.workflowApprovedMemberName;
                    editRecord.approvedDate = authoringStatus.workflowApprovedDate;
                    editRecord.isInserted = authoringStatus.isWorkflowInserted;
                    editRecord.isDeleted = authoringStatus.isWorkflowDeleted;
                    editRecord.isModified = authoringStatus.isWorkflowModified;
                    editRecord.lockModifiedName = authoringStatus.workflowModifiedByMemberName;
                    editRecord.lockModifiedDate = encodeDate(authoringStatus.workflowModifiedDate);
                    //
                    // ----- Set flags used to determine the Authoring State
                    PermissionController.UserContentPermissions userPermissions = PermissionController.getUserContentPermissions(core, adminContent);
                    editRecord.setAllowUserAdd(userPermissions.allowAdd);
                    editRecord.allowUserSave = userPermissions.allowSave;
                    editRecord.allowUserDelete = userPermissions.allowDelete;
                    //
                    // ----- Set Read Only: for edit lock
                    //
                    editRecord.userReadOnly |= editRecord.editLock.isEditLocked;
                    //
                    // ----- Set Read Only: if non-developer tries to edit a developer record
                    //
                    if (GenericController.toUCase(adminContent.tableName) == GenericController.toUCase("ccMembers")) {
                        if (!core.session.isAuthenticatedDeveloper()) {
                            if (editRecord.fieldsLc.ContainsKey("developer")) {
                                if (GenericController.encodeBoolean(editRecord.fieldsLc["developer"].value)) {
                                    editRecord.userReadOnly = true;
                                    Processor.Controllers.ErrorController.addUserError(core, "You do not have access rights To edit this record.");
                                    blockEditForm = true;
                                }
                            }
                        }
                    }
                    //
                    // ----- Now make sure this record is locked from anyone else
                    //
                    if (!(editRecord.userReadOnly)) {
                        WorkflowController.setEditLock(core, table.id, editRecord.id);
                    }
                    editRecord.loaded = true;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Load Array, Get defaults if no record ID, Then load in any response elements, no user errors
        /// </summary>
        /// <param name="core"></param>
        public void loadEditRecord(CoreController core) => loadEditRecord(core, false);
        //
        // ====================================================================================================
        /// <summary>
        /// Load both Live and Edit Record values from definition defaults
        /// </summary>
        /// <param name="core"></param>
        public void loadEditRecord_Default(CoreController core) {
            try {
                //
                string DefaultValueText = null;
                string LookupContentName = null;
                string UCaseDefaultValueText = null;
                string[] lookups = null;
                int Ptr = 0;
                string defaultValue = null;
                EditRecordFieldModel editRecordField = null;
                ContentFieldMetadataModel field = null;
                editRecord.active = true;
                editRecord.contentControlId = adminContent.id;
                editRecord.contentControlId_Name = adminContent.name;
                editRecord.editLock = new WorkflowController.editLockClass { editLockByMemberId = 0, editLockByMemberName = "", editLockExpiresDate = DateTime.MinValue, isEditLocked = false };
                editRecord.loaded = false;
                editRecord.saved = false;
                foreach (var keyValuePair in adminContent.fields) {
                    field = keyValuePair.Value;
                    if (!(editRecord.fieldsLc.ContainsKey(field.nameLc))) {
                        editRecordField = new EditRecordFieldModel();
                        editRecord.fieldsLc.Add(field.nameLc, editRecordField);
                    }
                    defaultValue = field.defaultValue;
                    if (field.active && !string.IsNullOrEmpty(defaultValue)) {
                        switch (field.fieldTypeId) {
                            case CPContentBaseClass.FieldTypeIdEnum.Integer:
                            case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                            case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                    //
                                    editRecord.fieldsLc[field.nameLc].value = encodeInteger(defaultValue);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Currency:
                            case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                    //
                                    editRecord.fieldsLc[field.nameLc].value = encodeNumber(defaultValue);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                    //
                                    editRecord.fieldsLc[field.nameLc].value = encodeBoolean(defaultValue);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                    //
                                    editRecord.fieldsLc[field.nameLc].value = encodeDate(defaultValue);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                    DefaultValueText = encodeText(field.defaultValue);
                                    if (!string.IsNullOrEmpty(DefaultValueText)) {
                                        if (DefaultValueText.isNumeric()) {
                                            editRecord.fieldsLc[field.nameLc].value = DefaultValueText;
                                        } else {
                                            if (field.lookupContentId != 0) {
                                                LookupContentName = MetadataController.getContentNameByID(core, field.lookupContentId);
                                                if (!string.IsNullOrEmpty(LookupContentName)) {
                                                    editRecord.fieldsLc[field.nameLc].value = MetadataController.getRecordIdByUniqueName(core, LookupContentName, DefaultValueText);
                                                }
                                            } else if (field.lookupList != "") {
                                                UCaseDefaultValueText = toUCase(DefaultValueText);
                                                lookups = field.lookupList.Split(',');
                                                for (Ptr = 0; Ptr <= lookups.GetUpperBound(0); Ptr++) {
                                                    if (UCaseDefaultValueText == toUCase(lookups[Ptr])) {
                                                        editRecord.fieldsLc[field.nameLc].value = Ptr + 1;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            default: {
                                    //
                                    editRecord.fieldsLc[field.nameLc].value = GenericController.encodeText(defaultValue);
                                    break;
                                }
                        }
                    }
                    //
                    // process reserved fields (set defaults just makes it look good)
                    // (also, this presets readonly/devonly/adminonly fields not set to member)
                    //
                    switch (GenericController.toUCase(field.nameLc)) {
                        case "MODIFIEDBY": {
                                editRecord.fieldsLc[field.nameLc].value = core.session.user.id;
                                break;
                            }
                        case "CREATEDBY": {
                                editRecord.fieldsLc[field.nameLc].value = core.session.user.id;
                                break;
                            }
                        case "CONTENTCONTROLID": {
                                editRecord.fieldsLc[field.nameLc].value = adminContent.id;
                                break;
                            }
                        default: {
                                // do nothing
                                break;
                            }
                    }
                    editRecord.fieldsLc[field.nameLc].dbValue = editRecord.fieldsLc[field.nameLc].value;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Load both Live and Edit Record values from definition defaults
        /// </summary>
        /// <param name="core"></param>
        public void loadEditRecord_WherePairs(CoreController core) {
            try {
                // todo refactor out
                if (!wherePair.Count.Equals(0)) {
                    foreach (var keyValuePair in adminContent.fields) {
                        ContentFieldMetadataModel field = keyValuePair.Value;
                        if (field.active && wherePair.ContainsKey(field.nameLc)) {
                            switch (field.fieldTypeId) {
                                case CPContentBaseClass.FieldTypeIdEnum.Integer:
                                case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                                case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement: {
                                        //
                                        editRecord.fieldsLc[field.nameLc].value = GenericController.encodeInteger(wherePair[field.nameLc]);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                        //
                                        editRecord.fieldsLc[field.nameLc].value = GenericController.encodeNumber(wherePair[field.nameLc]);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                        //
                                        editRecord.fieldsLc[field.nameLc].value = GenericController.encodeBoolean(wherePair[field.nameLc]);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                        //
                                        editRecord.fieldsLc[field.nameLc].value = GenericController.encodeDate(wherePair[field.nameLc]);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                        //
                                        // Many to Many can capture a list of ID values representing the 'secondary' values in the Many-To-Many Rules table
                                        //
                                        editRecord.fieldsLc[field.nameLc].value = wherePair[field.nameLc];
                                        break;
                                    }
                                default: {
                                        //
                                        editRecord.fieldsLc[field.nameLc].value = wherePair[field.nameLc];
                                        break;
                                    }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Load Records from the database
        /// </summary>
        /// <param name="core"></param>
        /// <param name="CheckUserErrors"></param>
        public void loadEditRecord_Dbase(CoreController core, bool CheckUserErrors) {
            try {
                //
                //
                object DBValueVariant = null;
                //
                // ----- test for content problem
                //
                if (editRecord.id == 0) {
                    //
                    // ----- Skip load, this is a new record
                    //
                } else if (adminContent.id == 0) {
                    //
                    // ----- Error: no content ID
                    //
                    blockEditForm = true;
                    Processor.Controllers.ErrorController.addUserError(core, "No content definition was found For Content ID [" + editRecord.id + "]. Please contact your application developer For more assistance.");
                    LogController.logError(core, new GenericException("AdminClass.LoadEditRecord_Dbase, No content definition was found For Content ID [" + editRecord.id + "]."));
                } else if (string.IsNullOrEmpty(adminContent.name)) {
                    //
                    // ----- Error: no content name
                    //
                    blockEditForm = true;
                    Processor.Controllers.ErrorController.addUserError(core, "No content definition could be found For ContentID [" + adminContent.id + "]. This could be a menu Error. Please contact your application developer For more assistance.");
                    LogController.logError(core, new GenericException("AdminClass.LoadEditRecord_Dbase, No content definition For ContentID [" + adminContent.id + "] could be found."));
                } else if (adminContent.tableName == "") {
                    //
                    // ----- Error: no content table
                    //
                    blockEditForm = true;
                    Processor.Controllers.ErrorController.addUserError(core, "The content definition [" + adminContent.name + "] is not associated With a valid database table. Please contact your application developer For more assistance.");
                    LogController.logError(core, new GenericException("AdminClass.LoadEditRecord_Dbase, No content definition For ContentID [" + adminContent.id + "] could be found."));
                    //
                    // move block to the edit and listing pages - to handle content editor cases - so they can edit 'pages', and just get the records they are allowed
                    //
                    //    ElseIf Not UserAllowContentEdit Then
                    //        '
                    //        ' ----- Error: load blocked by UserAllowContentEdit
                    //        '
                    //        BlockEditForm = True
                    //        Call core.htmldoc.main_AddUserError("Your account On this system does not have access rights To edit this content.")
                    //        Call HandleInternalError("AdminClass.LoadEditRecord_Dbase", "User does not have access To this content")
                } else if (adminContent.fields.Count == 0) {
                    //
                    // ----- Error: content definition is not complete
                    //
                    blockEditForm = true;
                    ErrorController.addUserError(core, "The content definition [" + adminContent.name + "] has no field records defined. Please contact your application developer For more assistance.");
                    LogController.logError(core, new GenericException("AdminClass.LoadEditRecord_Dbase, Content [" + adminContent.name + "] has no fields defined."));
                } else {
                    //
                    //   Open Content Sets with the data
                    //
                    using var csData = new CsModel(core);
                    csData.openRecord(adminContent.name, editRecord.id);
                    //
                    // store fieldvalues in RecordValuesVariant
                    //
                    if (!csData.ok()) {
                        //
                        //   Live or Edit records were not found
                        //
                        blockEditForm = true;
                        Processor.Controllers.ErrorController.addUserError(core, "The information you have requested could not be found. The record could have been deleted, Or there may be a system Error.");
                        // removed because it was throwing too many false positives (1/14/04 - tried to do it again)
                        // If a CM hits the edit tag for a deleted record, this is hit. It should not cause the Developers to spend hours running down.
                    } else {
                        foreach (var keyValuePair in adminContent.fields) {
                            ContentFieldMetadataModel adminContentcontent = keyValuePair.Value;
                            string fieldNameLc = adminContentcontent.nameLc;
                            EditRecordFieldModel editRecordField = null;
                            //
                            // set editRecord.field to editRecordField and set values
                            //
                            if (!editRecord.fieldsLc.ContainsKey(fieldNameLc)) {
                                editRecordField = new EditRecordFieldModel();
                                editRecord.fieldsLc.Add(fieldNameLc, editRecordField);
                            } else {
                                editRecordField = editRecord.fieldsLc[fieldNameLc];
                            }
                            //
                            // Load the current Database value
                            //
                            switch (adminContentcontent.fieldTypeId) {
                                case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                                case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                        DBValueVariant = "";
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                                case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                        DBValueVariant = csData.getText(adminContentcontent.nameLc);
                                        break;
                                    }
                                default: {
                                        DBValueVariant = csData.getRawData(adminContentcontent.nameLc);
                                        break;
                                    }
                            }
                            //
                            // Check for required and null case loading error
                            //
                            if (CheckUserErrors && adminContentcontent.required && (GenericController.isNull(DBValueVariant))) {
                                //
                                // if required and null
                                //
                                if (string.IsNullOrEmpty(adminContentcontent.defaultValue)) {
                                    //
                                    // default is null
                                    //
                                    if (adminContentcontent.editTabName == "") {
                                        Processor.Controllers.ErrorController.addUserError(core, "The value for [" + adminContentcontent.caption + "] was empty but is required. This must be set before you can save this record.");
                                    } else {
                                        Processor.Controllers.ErrorController.addUserError(core, "The value for [" + adminContentcontent.caption + "] in tab [" + adminContentcontent.editTabName + "] was empty but is required. This must be set before you can save this record.");
                                    }
                                } else {
                                    //
                                    // if required and null, set value to the default
                                    //
                                    DBValueVariant = adminContentcontent.defaultValue;
                                    if (adminContentcontent.editTabName == "") {
                                        Processor.Controllers.ErrorController.addUserError(core, "The value for [" + adminContentcontent.caption + "] was null but is required. The default value Is shown, And will be saved if you save this record.");
                                    } else {
                                        Processor.Controllers.ErrorController.addUserError(core, "The value for [" + adminContentcontent.caption + "] in tab [" + adminContentcontent.editTabName + "] was null but is required. The default value Is shown, And will be saved if you save this record.");
                                    }
                                }
                            }
                            //
                            // Save EditRecord values
                            //
                            switch (GenericController.toUCase(adminContentcontent.nameLc)) {
                                case "DATEADDED": {
                                        editRecord.dateAdded = csData.getDate(adminContentcontent.nameLc);
                                        break;
                                    }
                                case "MODIFIEDDATE": {
                                        editRecord.modifiedDate = csData.getDate(adminContentcontent.nameLc);
                                        break;
                                    }
                                case "CREATEDBY": {
                                        int createdByPersonId = csData.getInteger(adminContentcontent.nameLc);
                                        if (createdByPersonId == 0) {
                                            editRecord.createdBy = new PersonModel { name = "system" };
                                        } else {
                                            editRecord.createdBy = DbBaseModel.create<PersonModel>(core.cpParent, createdByPersonId);
                                            if (editRecord.createdBy == null) {
                                                editRecord.createdBy = new PersonModel { name = "deleted #" + createdByPersonId.ToString() };
                                            }
                                        }
                                        break;
                                    }
                                case "MODIFIEDBY": {
                                        int modifiedByPersonId = csData.getInteger(adminContentcontent.nameLc);
                                        if (modifiedByPersonId == 0) {
                                            editRecord.modifiedBy = new PersonModel { name = "system" };
                                        } else {
                                            editRecord.modifiedBy = DbBaseModel.create<PersonModel>(core.cpParent, modifiedByPersonId);
                                            if (editRecord.modifiedBy == null) {
                                                editRecord.modifiedBy = new PersonModel { name = "deleted #" + modifiedByPersonId.ToString() };
                                            }
                                        }
                                        break;
                                    }
                                case "ACTIVE": {
                                        editRecord.active = csData.getBoolean(adminContentcontent.nameLc);
                                        break;
                                    }
                                case "CONTENTCONTROLID": {
                                        editRecord.contentControlId = csData.getInteger(adminContentcontent.nameLc);
                                        if (editRecord.contentControlId.Equals(0)) {
                                            editRecord.contentControlId = adminContent.id;
                                        }
                                        editRecord.contentControlId_Name = MetadataController.getContentNameByID(core, editRecord.contentControlId);
                                        break;
                                    }
                                case "ID": {
                                        editRecord.id = csData.getInteger(adminContentcontent.nameLc);
                                        break;
                                    }
                                case "MENUHEADLINE": {
                                        editRecord.menuHeadline = csData.getText(adminContentcontent.nameLc);
                                        break;
                                    }
                                case "NAME": {
                                        editRecord.nameLc = csData.getText(adminContentcontent.nameLc);
                                        break;
                                    }
                                case "PARENTID": {
                                        editRecord.parentId = csData.getInteger(adminContentcontent.nameLc);
                                        break;
                                    }
                                default: {
                                        // do nothing
                                        break;
                                    }
                            }
                            //
                            editRecordField.dbValue = DBValueVariant;
                            editRecordField.value = DBValueVariant;
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Load Records from the database, no user error check
        /// </summary>
        /// <param name="core"></param>
        public void loadEditRecord_Dbase(CoreController core)
            => loadEditRecord_Dbase(core, false);
        //
        // ====================================================================================================
        /// <summary>
        /// Read the Form into the fields array
        /// </summary>
        /// <param name="core"></param>
        public void loadEditRecord_Request(CoreController core) {
            try {
                //
                // List of fields that were created for the form, and should be verified (starts and ends with a comma)
                var FormFieldLcListToBeLoaded = new List<string> { };
                string formFieldList = core.docProperties.getText("FormFieldList");
                if (!string.IsNullOrWhiteSpace(formFieldList)) {
                    FormFieldLcListToBeLoaded.AddRange(formFieldList.ToLowerInvariant().Split(','));
                    // -- remove possible front and end spaces
                    if (FormFieldLcListToBeLoaded.Contains("")) {
                        FormFieldLcListToBeLoaded.Remove("");
                        if (FormFieldLcListToBeLoaded.Contains("")) {
                            FormFieldLcListToBeLoaded.Remove("");
                        }
                    }
                }
                //
                // List of fields coming from the form that are empty -- and should not be in stream (starts and ends with a comma)
                var FormEmptyFieldLcList = new List<string> { };
                string emptyFieldList = core.docProperties.getText("FormEmptyFieldList");
                if (!string.IsNullOrWhiteSpace(emptyFieldList)) {
                    FormEmptyFieldLcList.AddRange(emptyFieldList.ToLowerInvariant().Split(','));
                    // -- remove possible front and end spaces
                    if (FormEmptyFieldLcList.Contains("")) {
                        FormEmptyFieldLcList.Remove("");
                        if (FormEmptyFieldLcList.Contains("")) {
                            FormEmptyFieldLcList.Remove("");
                        }
                    }
                }
                //
                if (allowAdminFieldCheck(core) && (FormFieldLcListToBeLoaded.Count == 0)) {
                    //
                    // The field list was not returned
                    Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The Error Is [no field list].");
                } else if (allowAdminFieldCheck(core) && (FormEmptyFieldLcList.Count == 0)) {
                    //
                    // The field list was not returned
                    Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The Error Is [no empty field list].");
                } else {
                    //
                    // fixup the string so it can be reduced by each field found, leaving and empty string if all correct
                    //
                    foreach (var keyValuePair in adminContent.fields) {
                        ContentFieldMetadataModel field = keyValuePair.Value;
                        loadEditRecord_RequestField(core, field, FormFieldLcListToBeLoaded, FormEmptyFieldLcList);
                    }
                    //
                    // If there are any form fields that were no loaded, flag the error now
                    //
                    if (allowAdminFieldCheck(core) && (FormFieldLcListToBeLoaded.Count > 0)) {
                        Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The following fields were not found [" + string.Join(",", FormFieldLcListToBeLoaded) + "].");
                        throw (new GenericException("Unexpected exception"));
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Read the Form into the fields array
        /// </summary>
        /// <param name="core"></param>
        /// <param name="field"></param>
        /// <param name="FormFieldLcListToBeLoaded"></param>
        /// <param name="FormEmptyFieldLcList"></param>
        public void loadEditRecord_RequestField(CoreController core, ContentFieldMetadataModel field, List<string> FormFieldLcListToBeLoaded, List<string> FormEmptyFieldLcList) {
            try {
                //
                // -- if field is not active, no change
                if (!field.active) { return; }
                //
                // -- if field was in request, set bool and remove it from list
                bool InLoadedFieldList = FormFieldLcListToBeLoaded.Contains(field.nameLc);
                if (InLoadedFieldList) {
                    FormFieldLcListToBeLoaded.Remove(field.nameLc);
                }
                //
                // -- if record is read only, exit now
                if (editRecord.userReadOnly) { return; }
                //
                // -- determine if the field value should be saved
                string ResponseFieldValueText = core.docProperties.getText(field.nameLc);
                string TabCopy = "";
                if (field.editTabName != "") {
                    TabCopy = " in the " + field.editTabName + " tab";
                }
                bool ResponseFieldValueIsOKToSave = true;
                bool InEmptyFieldList = FormEmptyFieldLcList.Contains(field.nameLc);
                bool InResponse = core.docProperties.containsKey(field.nameLc);
                bool ResponseFieldIsEmpty = string.IsNullOrEmpty(ResponseFieldValueText);
                //
                // -- process reserved fields
                switch (field.nameLc) {
                    case "contentcontrolid":
                        //
                        // -- admin can change contentcontrolid to any in the same table
                        if (allowAdminFieldCheck(core)) {
                            if (!core.docProperties.containsKey(field.nameLc.ToUpper())) {
                                if (!(!core.doc.userErrorList.Count.Equals(0))) {
                                    //
                                    // Add user error only for the first missing field
                                    Processor.Controllers.ErrorController.addUserError(core, "There has been an Error reading the response from your browser. Please Try again, taking care not to submit the page until your browser has finished loading. If this Error occurs again, please report this problem To your site administrator. The first Error was [" + field.nameLc + " not found]. There may have been others.");
                                }
                                throw (new GenericException("Unexpected exception"));
                            }
                        }
                        if (GenericController.encodeInteger(ResponseFieldValueText) != GenericController.encodeInteger(editRecord.fieldsLc[field.nameLc].value)) {
                            //
                            // new value
                            editRecord.fieldsLc[field.nameLc].value = ResponseFieldValueText;
                            ResponseFieldIsEmpty = false;
                        }
                        break;
                    case "active":
                        //
                        // anyone can change active
                        if (allowAdminFieldCheck(core) && (!InResponse) && (!InEmptyFieldList)) {
                            Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The error is [" + field.nameLc + " not found].");
                            return;
                        }
                        bool responseValue = core.docProperties.getBoolean(field.nameLc);
                        if (!responseValue.Equals(encodeBoolean(editRecord.fieldsLc[field.nameLc].value))) {
                            //
                            // new value
                            editRecord.fieldsLc[field.nameLc].value = responseValue;
                            ResponseFieldIsEmpty = false;
                        }
                        break;
                    case "ccguid":
                        //
                        // -- anyone can change
                        InEmptyFieldList = FormEmptyFieldLcList.Contains(field.nameLc);
                        InResponse = core.docProperties.containsKey(field.nameLc);
                        if (allowAdminFieldCheck(core)) {
                            if ((!InResponse) && (!InEmptyFieldList)) {
                                Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The Error Is [" + field.nameLc + " not found].");
                                return;
                            }
                        }
                        if (ResponseFieldValueText != encodeText(editRecord.fieldsLc[field.nameLc].value)) {
                            //
                            // new value
                            editRecord.fieldsLc[field.nameLc].value = ResponseFieldValueText;
                            ResponseFieldIsEmpty = false;
                        }
                        break;
                    case "id":
                    case "modifiedby":
                    case "modifieddate":
                    case "createdby":
                    case "dateadded":
                        //
                        // -----Control fields that cannot be edited
                        ResponseFieldValueIsOKToSave = false;
                        break;
                    default:
                        //
                        // ----- Read response for user fields
                        //       9/24/2009 - if fieldname is not in FormFieldListToBeLoaded, go with what is there (Db value or default value)
                        //
                        if (!field.authorable) {
                            //
                            // Is blocked from authoring, leave current value
                            //
                            ResponseFieldValueIsOKToSave = false;
                        } else if ((field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement) || (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect) || (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.ManyToMany)) {
                            //
                            // These fields types have no values to load, leave current value
                            // (many to many is handled during save)
                            //
                            ResponseFieldValueIsOKToSave = false;
                        } else if ((field.adminOnly) && (!core.session.isAuthenticatedAdmin())) {
                            //
                            // non-admin and admin only field, leave current value
                            //
                            ResponseFieldValueIsOKToSave = false;
                        } else if ((field.developerOnly) && (!core.session.isAuthenticatedDeveloper())) {
                            //
                            // non-developer and developer only field, leave current value
                            //
                            ResponseFieldValueIsOKToSave = false;
                        } else if ((field.readOnly) || (field.notEditable && (editRecord.id != 0))) {
                            //
                            // read only field, leave current
                            //
                            ResponseFieldValueIsOKToSave = false;
                        } else if (!InLoadedFieldList) {
                            //
                            // Was not sent out, so just go with the current value. Also, if the loaded field list is not returned, and the field is not returned, this is the bestwe can do.
                            ResponseFieldValueIsOKToSave = false;
                        } else if (allowAdminFieldCheck(core) && (!InResponse) && (!InEmptyFieldList)) {
                            //
                            // Was sent out non-blank, and no response back, flag error and leave the current value to a retry
                            string errorMessage = "There has been an error reading the response from your browser. The field[" + field.caption + "]" + TabCopy + " was missing. Please try your change again. If this error happens repeatedly, please report this problem to your site administrator.";
                            ErrorController.addUserError(core, errorMessage);
                            LogController.logError(core, new GenericException(errorMessage));
                            ResponseFieldValueIsOKToSave = false;
                        } else {
                            int EditorPixelHeight = 0;
                            int EditorRowHeight = 0;
                            //
                            // Test input value for valid data
                            //
                            switch (field.fieldTypeId) {
                                case CPContentBaseClass.FieldTypeIdEnum.Integer: {
                                        //
                                        // ----- Integer
                                        //
                                        ResponseFieldIsEmpty = ResponseFieldIsEmpty || (string.IsNullOrEmpty(ResponseFieldValueText));
                                        if (!ResponseFieldIsEmpty) {
                                            if (!ResponseFieldValueText.isNumeric()) {
                                                Processor.Controllers.ErrorController.addUserError(core, "The record cannot be saved because the field [" + field.caption + "]" + TabCopy + " must be a numeric value.");
                                                ResponseFieldValueIsOKToSave = false;
                                            }
                                        }
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                        //
                                        // ----- Floating point number
                                        //
                                        ResponseFieldIsEmpty = ResponseFieldIsEmpty || (string.IsNullOrEmpty(ResponseFieldValueText));
                                        if (!ResponseFieldIsEmpty) {
                                            if (!ResponseFieldValueText.isNumeric()) {
                                                Processor.Controllers.ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " must be a numeric value.");
                                                ResponseFieldValueIsOKToSave = false;
                                            }
                                        }
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                        //
                                        // ----- Must be a recordID
                                        //
                                        ResponseFieldIsEmpty = ResponseFieldIsEmpty || (string.IsNullOrEmpty(ResponseFieldValueText));
                                        if (!ResponseFieldIsEmpty) {
                                            if (!ResponseFieldValueText.isNumeric()) {
                                                ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " had an invalid selection.");
                                                ResponseFieldValueIsOKToSave = false;
                                            }
                                        }
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                        //
                                        // ----- Must be a Date value
                                        //
                                        ResponseFieldIsEmpty = ResponseFieldIsEmpty || (string.IsNullOrEmpty(ResponseFieldValueText));
                                        if (!ResponseFieldIsEmpty) {
                                            if (!GenericController.isDate(ResponseFieldValueText)) {
                                                ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " must be a date and/or time.");
                                                ResponseFieldValueIsOKToSave = false;
                                            }
                                        }
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                        //
                                        // ----- translate to boolean
                                        //
                                        ResponseFieldValueText = GenericController.encodeBoolean(ResponseFieldValueText).ToString();
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Link: {
                                        //
                                        // ----- Link field - if it starts with 'www.', add the http:// automatically
                                        //
                                        ResponseFieldValueText = GenericController.encodeText(ResponseFieldValueText);
                                        if (ResponseFieldValueText.ToLowerInvariant().left(4) == "www.") {
                                            ResponseFieldValueText = "http//" + ResponseFieldValueText;
                                        }
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.HTML:
                                case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                        //
                                        // ----- Html fields
                                        //
                                        EditorRowHeight = core.docProperties.getInteger(field.nameLc + "Rows");
                                        if (EditorRowHeight != 0) {
                                            core.userProperty.setProperty(adminContent.name + "." + field.nameLc + ".RowHeight", EditorRowHeight);
                                        }
                                        EditorPixelHeight = core.docProperties.getInteger(field.nameLc + "PixelHeight");
                                        if (EditorPixelHeight != 0) {
                                            core.userProperty.setProperty(adminContent.name + "." + field.nameLc + ".PixelHeight", EditorPixelHeight);
                                        }
                                        //
                                        if (!field.htmlContent) {
                                            string lcaseCopy = GenericController.toLCase(ResponseFieldValueText);
                                            lcaseCopy = GenericController.strReplace(lcaseCopy, "\r", "");
                                            lcaseCopy = GenericController.strReplace(lcaseCopy, "\n", "");
                                            lcaseCopy = lcaseCopy.Trim(' ');
                                            if ((lcaseCopy == HTMLEditorDefaultCopyNoCr) || (lcaseCopy == HTMLEditorDefaultCopyNoCr2)) {
                                                //
                                                // if the editor was left blank, remote the default copy
                                                //
                                                ResponseFieldValueText = "";
                                            } else {
                                                if (GenericController.strInstr(1, ResponseFieldValueText, HTMLEditorDefaultCopyStartMark) != 0) {
                                                    //
                                                    // if the default copy was editing, remote the markers
                                                    //
                                                    ResponseFieldValueText = GenericController.strReplace(ResponseFieldValueText, HTMLEditorDefaultCopyStartMark, "");
                                                    ResponseFieldValueText = GenericController.strReplace(ResponseFieldValueText, HTMLEditorDefaultCopyEndMark, "");
                                                }
                                                //
                                                // If the response is only white space, remove it
                                                // this is a fix for when Site Managers leave white space in the editor, and do not realize it
                                                //   then cannot fixgure out how to remove it
                                                //
                                                ResponseFieldValueText = ContentRenderController.processWysiwygResponseForSave(core, ResponseFieldValueText);
                                                if (string.IsNullOrEmpty(ResponseFieldValueText.ToLowerInvariant().Replace(' '.ToString(), "").Replace("&nbsp;", ""))) {
                                                    ResponseFieldValueText = "";
                                                }
                                            }
                                        }
                                        break;
                                    }
                                default: {
                                        //
                                        // ----- text types
                                        //
                                        EditorRowHeight = core.docProperties.getInteger(field.nameLc + "Rows");
                                        if (EditorRowHeight != 0) {
                                            core.userProperty.setProperty(adminContent.name + "." + field.nameLc + ".RowHeight", EditorRowHeight);
                                        }
                                        EditorPixelHeight = core.docProperties.getInteger(field.nameLc + "PixelHeight");
                                        if (EditorPixelHeight != 0) {
                                            core.userProperty.setProperty(adminContent.name + "." + field.nameLc + ".PixelHeight", EditorPixelHeight);
                                        }
                                        break;
                                    }
                            }
                            if (field.nameLc == "parentid") {
                                //
                                // check circular reference on all parentid fields
                                int ParentId = encodeInteger(ResponseFieldValueText);
                                int LoopPtr = 0;
                                List<int> usedIdList = new List<int> { editRecord.id };
                                const int LoopPtrMax = 100;
                                while ((LoopPtr < LoopPtrMax) && (ParentId != 0) && !usedIdList.Contains(ParentId)) {
                                    usedIdList.Add(ParentId);
                                    using (var csData = new CsModel(core)) {
                                        if (!csData.open(adminContent.name, "ID=" + ParentId.ToString(), "", true, 0, "ParentID")) {
                                            ParentId = 0;
                                            break;
                                        }
                                        ParentId = csData.getInteger("ParentID");
                                    }
                                    LoopPtr += 1;
                                }
                                if (LoopPtr == LoopPtrMax) {
                                    //
                                    // Too deep
                                    //
                                    Processor.Controllers.ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " creates a relationship between records that Is too large. Please limit the depth of this relationship to " + LoopPtrMax + " records.");
                                    ResponseFieldValueIsOKToSave = false;
                                } else if ((editRecord.id != 0) && (editRecord.id == ParentId)) {
                                    //
                                    // Reference to iteslf
                                    //
                                    Processor.Controllers.ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " contains a circular reference. This record points back to itself. This is not allowed.");
                                    ResponseFieldValueIsOKToSave = false;
                                } else if (ParentId != 0) {
                                    //
                                    // Circular reference
                                    //
                                    Processor.Controllers.ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " contains a circular reference. This field either points to other records which then point back to this record. This is not allowed.");
                                    ResponseFieldValueIsOKToSave = false;
                                }
                            }
                            if (field.textBuffered) {
                                //
                                // text buffering
                                //
                            }
                            if ((field.required) && (ResponseFieldIsEmpty)) {
                                //
                                // field is required and is not given
                                //
                                Processor.Controllers.ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " Is required but has no value.");
                                ResponseFieldValueIsOKToSave = false;
                            }
                            bool blockDuplicateUsername = false;
                            bool blockDuplicateEmail = false;
                            //
                            // special case - people records without Allowduplicateusername require username to be unique
                            //
                            if (GenericController.toLCase(adminContent.tableName) == "ccmembers") {
                                if (GenericController.toLCase(field.nameLc) == "username") {
                                    blockDuplicateUsername = !(core.siteProperties.getBoolean("allowduplicateusername", false));
                                }
                                if (GenericController.toLCase(field.nameLc) == "email") {
                                    blockDuplicateEmail = (core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin, false));
                                }
                            }
                            if ((blockDuplicateUsername || blockDuplicateEmail || field.uniqueName) && (!ResponseFieldIsEmpty)) {
                                //
                                // ----- Do the unique check for this field
                                //
                                string SQLUnique = "select id from " + adminContent.tableName + " where (" + field.nameLc + "=" + MetadataController.encodeSQL(ResponseFieldValueText, field.fieldTypeId) + ")and(" + adminContent.legacyContentControlCriteria + ")";
                                if (editRecord.id > 0) {
                                    //
                                    // --editing record
                                    SQLUnique = SQLUnique + "and(id<>" + editRecord.id + ")";
                                }
                                using var csData = new CsModel(core); csData.openSql(SQLUnique, adminContent.dataSourceName);
                                if (csData.ok()) {
                                    //
                                    // field is not unique, skip it and flag error
                                    //
                                    if (blockDuplicateUsername) {
                                        //
                                        //
                                        //
                                        Processor.Controllers.ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " must be unique and there Is another record with [" + ResponseFieldValueText + "]. This must be unique because the preference 'Allow Duplicate Usernames' is Not checked.");
                                    } else if (blockDuplicateEmail) {
                                        //
                                        //
                                        //
                                        Processor.Controllers.ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " must be unique and there is another record with [" + ResponseFieldValueText + "]. This must be unique because the preference 'Allow Email Login' is checked.");
                                    } else {
                                        //
                                        // non-workflow
                                        //
                                        Processor.Controllers.ErrorController.addUserError(core, "This record cannot be saved because the field [" + field.caption + "]" + TabCopy + " must be unique and there is another record with [" + ResponseFieldValueText + "].");
                                    }
                                    ResponseFieldValueIsOKToSave = false;
                                }
                            }
                        }
                        // end case
                        break;
                }
                //
                // Save response if it is valid
                //
                if (ResponseFieldValueIsOKToSave) {
                    editRecord.fieldsLc[field.nameLc].value = ResponseFieldValueText;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// addons used for editing, based on the field type
        /// </summary>
        public List<FieldTypeEditorAddonModel> fieldTypeEditors {
            get {
                if (_fieldTypeDefaultEditors == null) {
                    _fieldTypeDefaultEditors = EditorController.getFieldEditorAddonList(core);
                }
                return _fieldTypeDefaultEditors;
            }
        }
        private List<FieldTypeEditorAddonModel> _fieldTypeDefaultEditors;
        //
    }
    //
    //====================================================================================================
    /// <summary>
    /// method request object
    /// </summary>
    public class AdminDataRequest {
        /// <summary>
        /// content for edit or list
        /// </summary>
        public int contentId { get; set; }
        /// <summary>
        /// record id for edit
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// guid for edit
        /// </summary>
        public string guid { get; set; }
        /// <summary>
        /// prefix for record edit
        /// </summary>
        public string titleExtension { get; set; }
        /// <summary>
        /// the index to the top record
        /// </summary>
        public int recordTop { get; set; }
        /// <summary>
        /// records to display
        /// </summary>
        public int recordsPerPage { get; set; }
        /// <summary>
        /// name value pairs used for filters
        /// </summary>
        public Dictionary<string, string> wherePairDict { get; set; }
        /// <summary>
        /// action, like save, etc
        /// </summary>
        public int adminAction { get; set; }
        /// <summary>
        /// the form id of to originating form
        /// </summary>
        public int adminSourceForm { get; set; }
        /// <summary>
        /// the destination form
        /// </summary>
        public int adminForm { get; set; }
        /// <summary>
        /// the button pressed
        /// </summary>
        public string adminButton { get; set; }
        /// <summary>
        /// fieldEditorPreference
        /// </summary>
        public string fieldEditorPreference { get; set; }
    }
}
