
using Contensive.BaseClasses;
using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using static Contensive.Processor.Controllers.GenericController;
namespace Contensive.Processor.Addons.AdminSite.Models {
    /// <summary>
    /// 
    /// </summary>
    public class EditRecordModel {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        public Dictionary<string, EditRecordFieldModel> fieldsLc = new(StringComparer.InvariantCultureIgnoreCase);
        /// <summary>
        /// ID field of edit record (Record to be edited)
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// ParentID field of edit record (Record to be edited)
        /// </summary>
        public int parentId { get; set; }
        /// <summary>
        /// name field of edit record
        /// </summary>
        public string nameLc { get; set; }
        /// <summary>
        /// active field of the edit record
        /// </summary>
        public bool active { get; set; }
        /// <summary>
        /// ContentControlID of the edit record
        /// </summary>
        public int contentControlId { get; set; }
        /// <summary>
        /// denormalized name from contentControlId property
        /// </summary>
        public string contentControlId_Name { get; set; }
        /// <summary>
        /// Used for Content Watch Link Label if default
        /// </summary>
        public string menuHeadline { get; set; }
        /// <summary>
        /// Used for control section display
        /// </summary>
        public DateTime modifiedDate { get; set; }
        public PersonModel modifiedBy { get; set; }
        public DateTime dateAdded { get; set; }
        public PersonModel createdBy { get; set; }
        /// <summary>
        /// true/false - set true when the field array values are loaded
        /// </summary>
        public bool loaded { get; set; }
        /// <summary>
        /// true if edit record was saved during this page
        /// </summary>
        public bool saved { get; set; }
        /// <summary>
        /// set if this record can not be edited, for various reasons
        /// </summary>
        public bool userReadOnly { get; set; }
        /// <summary>
        /// true means the edit record has been deleted
        /// </summary>
        public bool isDeleted { get; set; }
        /// <summary>
        /// set if Workflow authoring insert
        /// </summary>
        public bool isInserted { get; set; }
        /// <summary>
        /// record has been modified since last published
        /// </summary>
        public bool isModified { get; set; }
        /// <summary>
        /// member who first edited the record
        /// </summary>
        public string lockModifiedName { get; set; }
        /// <summary>
        /// Date when member modified record
        /// </summary>
        public DateTime lockModifiedDate { get; set; }
        /// <summary>
        /// set if a submit Lock, even if the current user is admin
        /// </summary>
        public bool submitLock { get; set; }
        /// <summary>
        /// member who submitted the record
        /// </summary>
        public string submittedName { get; set; }
        /// <summary>
        /// Date when record was submitted
        /// </summary>
        public DateTime submittedDate { get; set; }
        /// <summary>
        /// set if an approve Lock
        /// </summary>
        public bool approveLock { get; set; }
        /// <summary>
        /// member who approved the record
        /// </summary>
        public string approvedName { get; set; }
        /// <summary>
        /// Date when record was approved
        /// </summary>
        public DateTime approvedDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        private bool allowUserAdd;
        /// <summary>
        /// This user can add records to this content
        /// </summary>
        public bool getAllowUserAdd() {
            return allowUserAdd;
        }
        /// <summary>
        /// This user can add records to this content
        /// </summary>
        public void setAllowUserAdd(bool value) {
            allowUserAdd = value;
        }
        /// <summary>
        /// This user can save the current record
        /// </summary>
        public bool allowUserSave { get; set; }
        /// <summary>
        /// This user can delete the current record
        /// </summary>
        public bool allowUserDelete { get; set; }
        /// <summary>
        /// set if an edit Lock by anyone else besides the current user
        /// </summary>
        public WorkflowController.editLockClass editLock { get; set; }
        //
        //========================================================================
        //
        public static void SaveEditRecord(CPClass cp, AdminDataModel adminData) {
            try {
                int SaveCCIDValue = 0;
                if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                    //
                    // -- If There is an error, block the save
                    adminData.admin_Action = Constants.AdminActionNop;
                } else if (!cp.core.session.isAuthenticatedContentManager(adminData.adminContent.name)) {
                    //
                    // -- must be content manager
                } else if (adminData.editRecord.userReadOnly) {
                    //
                    // -- read only block
                } else {
                    //
                    // -- Record will be saved, create a new one if this is an add
                    bool NewRecord = false;
                    bool recordChanged = false;
                    using (var csData = new CsModel(cp.core)) {
                        if (adminData.editRecord.id == 0) {
                            NewRecord = true;
                            recordChanged = true;
                            csData.insert(adminData.adminContent.name);
                        } else {
                            NewRecord = false;
                            csData.openRecord(adminData.adminContent.name, adminData.editRecord.id);
                        }
                        if (!csData.ok()) {
                            //
                            // ----- Error: new record could not be created
                            //
                            if (NewRecord) {
                                //
                                // Could not insert record
                                //
                                logger.Error($"{cp.core.logCommonMessage}", new GenericException("A new record could not be inserted for content [" + adminData.adminContent.name + "]. Verify the Database table and field DateAdded, CreateKey, and ID."));
                            } else {
                                //
                                // Could not locate record you requested
                                //
                                logger.Error($"{cp.core.logCommonMessage}", new GenericException("The record you requested (ID=" + adminData.editRecord.id + ") could not be found for content [" + adminData.adminContent.name + "]"));
                            }
                        } else {
                            //
                            // ----- Get the ID of the current record
                            //
                            adminData.editRecord.id = csData.getInteger("ID");
                            //
                            // ----- Create the update sql
                            //
                            foreach (var keyValuePair in adminData.adminContent.fields) {
                                ContentFieldMetadataModel field = keyValuePair.Value;
                                EditRecordFieldModel editRecordField = adminData.editRecord.fieldsLc[field.nameLc];
                                object fieldValueObject = editRecordField.value_content;
                                string FieldValueText = GenericController.encodeText(fieldValueObject);
                                string fieldName = field.nameLc;
                                string UcaseFieldName = GenericController.toUCase(fieldName);
                                //
                                // ----- Handle special case fields
                                //
                                switch (UcaseFieldName) {
                                    case "NAME": {
                                            //
                                            // -- update cache for next view
                                            adminData.editRecord.nameLc = GenericController.encodeText(fieldValueObject);
                                            break;
                                        }
                                    case "SORTORDER": {
                                            //
                                            if (csData.getText(fieldName) != FieldValueText) {
                                                recordChanged = true;
                                                csData.set(fieldName, FieldValueText);
                                            }
                                            break;
                                        }
                                    case "CCGUID": {
                                            if (NewRecord && string.IsNullOrEmpty(FieldValueText)) {
                                                //
                                                // if new record and edit form returns empty, preserve the guid used to create the record.
                                            } else {
                                                //
                                                // save the value in the request
                                                if (csData.getText(fieldName) != FieldValueText) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, FieldValueText);
                                                }
                                            }
                                            break;
                                        }
                                    case "CONTENTCONTROLID": {
                                            if (csData.getInteger(fieldName) != encodeInteger(fieldValueObject)) {
                                                SaveCCIDValue = encodeInteger(fieldValueObject);
                                                recordChanged = true;
                                            }
                                            break;
                                        }
                                    case "ACTIVE": {
                                            if (csData.getBoolean(fieldName) != encodeBoolean(fieldValueObject)) {
                                                recordChanged = true;
                                                csData.set(fieldName, encodeBoolean(fieldValueObject));
                                            }
                                            break;
                                        }
                                    //
                                    // -- these are deprecated, but still in use
                                    case "DATEEXPIRES": {
                                            //
                                            // ----- make sure content watch expires before content expires
                                            //
                                            if (!isNull(fieldValueObject) && isDate(fieldValueObject)) {
                                                DateTime saveValue = encodeDate(fieldValueObject);
                                                if (adminData.contentWatchExpires <= DateTime.MinValue) {
                                                    adminData.contentWatchExpires = saveValue;
                                                } else if (adminData.contentWatchExpires > saveValue) {
                                                    adminData.contentWatchExpires = saveValue;
                                                }
                                            }
                                            //
                                            break;
                                        }
                                    //
                                    // -- these are deprecated, but still in use
                                    case "DATEARCHIVE": {
                                            //
                                            // ----- make sure content watch expires before content archives
                                            //
                                            if (!isNull(fieldValueObject) && isDate(fieldValueObject)) {
                                                DateTime saveValue = GenericController.encodeDate(fieldValueObject);
                                                if ((adminData.contentWatchExpires) <= DateTime.MinValue) {
                                                    adminData.contentWatchExpires = saveValue;
                                                } else if (adminData.contentWatchExpires > saveValue) {
                                                    adminData.contentWatchExpires = saveValue;
                                                }
                                            }
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                //
                                // ----- Put the field in the SQL to be saved
                                //
                                if (AdminDataModel.isVisibleUserField(cp.core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, adminData.adminContent.tableName) && (NewRecord || (!field.readOnly)) && (NewRecord || (!field.notEditable))) {
                                    //
                                    // ----- save the value by field type
                                    //
                                    switch (field.fieldTypeId) {
                                        case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                                        case CPContentBaseClass.FieldTypeIdEnum.Redirect: {
                                                //
                                                // do nothing with these
                                                //
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.File:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                                //
                                                // filenames, upload to cdnFiles
                                                //
                                                if (cp.core.docProperties.getBoolean(fieldName + ".DeleteFlag")) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, "");
                                                }
                                                //
                                                // -- find the uploaded file in the request.files 
                                                csData.setFormInput(cp.core, fieldName, fieldName);
                                                recordChanged = true;
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                                //
                                                // boolean
                                                //
                                                bool saveValue = GenericController.encodeBoolean(fieldValueObject);
                                                if (csData.getBoolean(fieldName) != saveValue) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, saveValue);
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                        case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                                //
                                                // Floating pointer numbers, allow nullable
                                                if (string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, null);
                                                } else if (encodeText(fieldValueObject) != csData.getText(fieldName)) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, encodeNumber(fieldValueObject));
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                                //
                                                // Date
                                                //
                                                if (cp.core.docProperties.getBoolean(fieldName + ".clearFlag") || string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
                                                    //
                                                    // -- blank field, or clear checkbox checked
                                                    recordChanged = true;
                                                    csData.set(fieldName, null);
                                                } else if (encodeDate(fieldValueObject) != csData.getDate(fieldName)) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, encodeDate(fieldValueObject));
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Integer:
                                        case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                                //
                                                // Integers, allow nullable
                                                if (string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, null);
                                                } else if (encodeInteger(fieldValueObject) != csData.getInteger(fieldName)) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, encodeInteger(fieldValueObject));
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.LongText:
                                        case CPContentBaseClass.FieldTypeIdEnum.Text:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileJavaScript:
                                        case CPContentBaseClass.FieldTypeIdEnum.HTML:
                                        case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                                //
                                                // Text
                                                //
                                                string saveValue = GenericController.encodeText(fieldValueObject);
                                                if (csData.getText(fieldName) != saveValue) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, saveValue);
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                                //
                                                // Many to Many checklist
                                                cp.core.html.processCheckList("field" + field.id, MetadataController.getContentNameByID(cp.core, field.contentId), adminData.editRecord.id, MetadataController.getContentNameByID(cp.core, field.manyToManyContentId), MetadataController.getContentNameByID(cp.core, field.manyToManyRuleContentId), field.manyToManyRulePrimaryField, field.manyToManyRuleSecondaryField);
                                                break;
                                            }
                                        default: {
                                                //
                                                // Unknown other types
                                                string saveValue = GenericController.encodeText(fieldValueObject);
                                                recordChanged = true;
                                                csData.set(UcaseFieldName, saveValue);
                                                break;
                                            }
                                    }
                                }
                                //
                                // -- put any changes back in array for the next page to display
                                editRecordField.value_content = fieldValueObject;
                            }
                            if (recordChanged) {
                                //
                                // -- clear cache
                                cp.core.cache.invalidateRecordKey(adminData.editRecord.id, adminData.adminContent.tableName);
                                cp.core.cache.invalidateTableDependencyKey( adminData.adminContent.tableName);
                            }
                            //
                            // ----- clear/set authoring controls
                            var contentTable = DbBaseModel.createByUniqueName<TableModel>(cp, adminData.adminContent.tableName);
                            if (contentTable != null) WorkflowController.clearEditLock(cp.core, contentTable.id, adminData.editRecord.id);
                            //
                            // ----- if admin content is changed, reload the adminContext.content data in case this is a save, and not an OK
                            if (recordChanged && SaveCCIDValue != 0) {
                                adminData.adminContent.setContentControlId(cp.core, adminData.editRecord.id, SaveCCIDValue);
                                adminData.editRecord.contentControlId_Name = MetadataController.getContentNameByID(cp.core, SaveCCIDValue);
                                adminData.adminContent = ContentMetadataModel.createByUniqueName(cp.core, adminData.editRecord.contentControlId_Name);
                                adminData.adminContent.id = adminData.adminContent.id;
                                adminData.adminContent.name = adminData.adminContent.name;
                            }
                        }
                    }
                    adminData.editRecord.saved = true;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Load Array, Get defaults if no record ID, Then load in any response elements
        /// </summary>
        /// <param name="core"></param>
        /// <param name="CheckUserErrors"></param>
        public static void loadEditRecord(CoreController core, bool CheckUserErrors, AdminDataModel adminData) {
            try {
                // todo refactor out
                EditRecordModel editRecord = adminData.editRecord;
                ContentMetadataModel adminContent = adminData.adminContent;
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
                        EditRecordModel.loadEditRecord_Default(core, adminData);
                        EditRecordModel.loadEditRecord_WherePairs(core, adminData);
                    } else {
                        //
                        // ----- Load the Live Record specified
                        //
                        EditRecordModel.loadEditRecord_Dbase(core, adminData, CheckUserErrors);
                        EditRecordModel.loadEditRecord_WherePairs(core, adminData);
                    }
                    //
                    // ----- Capture core fields needed for processing
                    //
                    editRecord.menuHeadline = "";
                    if (editRecord.fieldsLc.ContainsKey("menuheadline")) {
                        editRecord.menuHeadline = GenericController.encodeText(editRecord.fieldsLc["menuheadline"].value_content);
                    }
                    //
                    if (editRecord.fieldsLc.ContainsKey("name")) {
                        editRecord.nameLc = GenericController.encodeText(editRecord.fieldsLc["name"].value_content);
                    }
                    //
                    if (editRecord.fieldsLc.ContainsKey("active")) {
                        editRecord.active = GenericController.encodeBoolean(editRecord.fieldsLc["active"].value_content);
                    }
                    //
                    if (editRecord.fieldsLc.ContainsKey("contentcontrolid")) {
                        editRecord.contentControlId = GenericController.encodeInteger(editRecord.fieldsLc["contentcontrolid"].value_content);
                    }
                    //
                    if (editRecord.fieldsLc.ContainsKey("parentid")) {
                        editRecord.parentId = GenericController.encodeInteger(editRecord.fieldsLc["parentid"].value_content);
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
                                if (GenericController.encodeBoolean(editRecord.fieldsLc["developer"].value_content)) {
                                    editRecord.userReadOnly = true;
                                    Processor.Controllers.ErrorController.addUserError(core, "You do not have access rights To edit this record.");
                                    adminData.blockEditForm = true;
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
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Load both Live and Edit Record values from definition defaults
        /// </summary>
        /// <param name="core"></param>
        public static void loadEditRecord_Default(CoreController core, AdminDataModel adminData) {
            try {
                ContentMetadataModel adminContent = adminData.adminContent;
                EditRecordModel editRecord = adminData.editRecord;
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
                                    editRecord.fieldsLc[field.nameLc].value_content = encodeInteger(defaultValue);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Currency:
                            case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                    //
                                    editRecord.fieldsLc[field.nameLc].value_content = encodeNumber(defaultValue);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                    //
                                    editRecord.fieldsLc[field.nameLc].value_content = encodeBoolean(defaultValue);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                    //
                                    editRecord.fieldsLc[field.nameLc].value_content = encodeDate(defaultValue);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                    DefaultValueText = encodeText(field.defaultValue);
                                    if (!string.IsNullOrEmpty(DefaultValueText)) {
                                        if (DefaultValueText.isNumeric()) {
                                            editRecord.fieldsLc[field.nameLc].value_content = DefaultValueText;
                                        } else {
                                            if (field.lookupContentId != 0) {
                                                LookupContentName = MetadataController.getContentNameByID(core, field.lookupContentId);
                                                if (!string.IsNullOrEmpty(LookupContentName)) {
                                                    editRecord.fieldsLc[field.nameLc].value_content = MetadataController.getRecordIdByUniqueName(core, LookupContentName, DefaultValueText);
                                                }
                                            } else if (field.lookupList != "") {
                                                UCaseDefaultValueText = toUCase(DefaultValueText);
                                                lookups = field.lookupList.Split(',');
                                                for (Ptr = 0; Ptr <= lookups.GetUpperBound(0); Ptr++) {
                                                    if (UCaseDefaultValueText == toUCase(lookups[Ptr])) {
                                                        editRecord.fieldsLc[field.nameLc].value_content = Ptr + 1;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                            case CPContentBaseClass.FieldTypeIdEnum.FileJavaScript:
                            case CPContentBaseClass.FieldTypeIdEnum.FileText:
                            case CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                                    //
                                    // todo -- convert to internal storage for filename and content, like modes
                                    //
                                    // create filename and save file, put filename in raw data
                                    //string pathFilename = FileController.getVirtualRecordUnixPathFilename(adminContent.tableName, field.nameLc, editRecord.id, field.fieldTypeId);
                                    //core.cdnFiles.saveFile(pathFilename, defaultValue);
                                    //editRecord.fieldsLc[field.nameLc].value = pathFilename;
                                    editRecord.fieldsLc[field.nameLc].value_content = GenericController.encodeText(defaultValue);
                                    break;
                                }
                            default: {
                                    //
                                    editRecord.fieldsLc[field.nameLc].value_content = GenericController.encodeText(defaultValue);
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
                                editRecord.fieldsLc[field.nameLc].value_content = core.session.user.id;
                                break;
                            }
                        case "CREATEDBY": {
                                editRecord.fieldsLc[field.nameLc].value_content = core.session.user.id;
                                break;
                            }
                        case "CONTENTCONTROLID": {
                                editRecord.fieldsLc[field.nameLc].value_content = adminContent.id;
                                break;
                            }
                        default: {
                                // do nothing
                                break;
                            }
                    }
                    editRecord.fieldsLc[field.nameLc].value_storedInDb = editRecord.fieldsLc[field.nameLc].value_content;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Load both Live and Edit Record values from definition defaults
        /// </summary>
        /// <param name="core"></param>
        public static void loadEditRecord_WherePairs(CoreController core, AdminDataModel adminData) {
            try {
                ContentMetadataModel adminContent = adminData.adminContent;
                EditRecordModel editRecord = adminData.editRecord;
                Dictionary<string, string> wherePair = adminData.wherePair;
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
                                        editRecord.fieldsLc[field.nameLc].value_content = GenericController.encodeInteger(wherePair[field.nameLc]);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                        //
                                        editRecord.fieldsLc[field.nameLc].value_content = GenericController.encodeNumber(wherePair[field.nameLc]);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                        //
                                        editRecord.fieldsLc[field.nameLc].value_content = GenericController.encodeBoolean(wherePair[field.nameLc]);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                        //
                                        editRecord.fieldsLc[field.nameLc].value_content = GenericController.encodeDate(wherePair[field.nameLc]);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                        //
                                        // Many to Many can capture a list of ID values representing the 'secondary' values in the Many-To-Many Rules table
                                        //
                                        editRecord.fieldsLc[field.nameLc].value_content = wherePair[field.nameLc];
                                        break;
                                    }
                                default: {
                                        //
                                        editRecord.fieldsLc[field.nameLc].value_content = wherePair[field.nameLc];
                                        break;
                                    }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Load Records from the database
        /// </summary>
        /// <param name="core"></param>
        /// <param name="CheckUserErrors"></param>
        public static void loadEditRecord_Dbase(CoreController core, AdminDataModel adminData, bool CheckUserErrors) {
            try {
                EditRecordModel editRecord = adminData.editRecord;
                ContentMetadataModel adminContent = adminData.adminContent;
                bool blockEditForm = adminData.blockEditForm;
                //
                //
                object valueStoredInDb = null;
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
                    logger.Error($"{core.logCommonMessage}", new GenericException("AdminClass.LoadEditRecord_Dbase, No content definition was found For Content ID [" + editRecord.id + "]."));
                } else if (string.IsNullOrEmpty(adminContent.name)) {
                    //
                    // ----- Error: no content name
                    //
                    blockEditForm = true;
                    Processor.Controllers.ErrorController.addUserError(core, "No content definition could be found For ContentID [" + adminContent.id + "]. This could be a menu Error. Please contact your application developer For more assistance.");
                    logger.Error($"{core.logCommonMessage}", new GenericException("AdminClass.LoadEditRecord_Dbase, No content definition For ContentID [" + adminContent.id + "] could be found."));
                } else if (adminContent.tableName == "") {
                    //
                    // ----- Error: no content table
                    //
                    blockEditForm = true;
                    Processor.Controllers.ErrorController.addUserError(core, "The content definition [" + adminContent.name + "] is not associated With a valid database table. Please contact your application developer For more assistance.");
                    logger.Error($"{core.logCommonMessage}", new GenericException("AdminClass.LoadEditRecord_Dbase, No content definition For ContentID [" + adminContent.id + "] could be found."));
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
                    logger.Error($"{core.logCommonMessage}", new GenericException("AdminClass.LoadEditRecord_Dbase, Content [" + adminContent.name + "] has no fields defined."));
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
                                        valueStoredInDb = "";
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                                case CPContentBaseClass.FieldTypeIdEnum.FileJavaScript:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                        valueStoredInDb = csData.getText(adminContentcontent.nameLc);
                                        break;
                                    }
                                default: {
                                        valueStoredInDb = csData.getValueStoredInDbField(adminContentcontent.nameLc);
                                        break;
                                    }
                            }
                            //
                            // Check for required and null case loading error
                            //
                            if (CheckUserErrors && adminContentcontent.required && (GenericController.isNull(valueStoredInDb))) {
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
                                    valueStoredInDb = adminContentcontent.defaultValue;
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
                            editRecordField.value_storedInDb = valueStoredInDb;
                            editRecordField.value_content = valueStoredInDb;
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Read the Form into the fields array
        /// </summary>
        /// <param name="core"></param>
        public static void loadEditRecord_Request(CoreController core, AdminDataModel adminData) {
            try {
                ContentMetadataModel adminContent = adminData.adminContent;
                EditRecordModel editRecord = adminData.editRecord;
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
                if (editRecord.allowAdminFieldCheck(core) && (FormFieldLcListToBeLoaded.Count == 0)) {
                    //
                    // The field list was not returned
                    Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The error is [no field list].");
                } else if (editRecord.allowAdminFieldCheck(core) && (FormEmptyFieldLcList.Count == 0)) {
                    //
                    // The field list was not returned
                    Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The error is [no empty field list].");
                } else {
                    //
                    // fixup the string so it can be reduced by each field found, leaving and empty string if all correct
                    //
                    foreach (var keyValuePair in adminContent.fields) {
                        ContentFieldMetadataModel field = keyValuePair.Value;
                        loadEditRecord_RequestField(core, adminData, field, FormFieldLcListToBeLoaded, FormEmptyFieldLcList);
                    }
                    //
                    // If there are any form fields that were no loaded, flag the error now
                    //
                    if (editRecord.allowAdminFieldCheck(core) && (FormFieldLcListToBeLoaded.Count > 0)) {
                        Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The following fields were not found [" + string.Join(",", FormFieldLcListToBeLoaded) + "].");
                        throw (new GenericException("Unexpected exception"));
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
        public static void loadEditRecord_RequestField(CoreController core, AdminDataModel adminData, ContentFieldMetadataModel field, List<string> FormFieldLcListToBeLoaded, List<string> FormEmptyFieldLcList) {
            try {
                EditRecordModel editRecord = adminData.editRecord;
                ContentMetadataModel adminContent = adminData.adminContent;
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
                        if (editRecord.allowAdminFieldCheck(core)) {
                            if (!core.docProperties.containsKey(field.nameLc.ToUpper())) {
                                if (!(!core.doc.userErrorList.Count.Equals(0))) {
                                    //
                                    // Add user error only for the first missing field
                                    Processor.Controllers.ErrorController.addUserError(core, "There has been an Error reading the response from your browser. Please Try again, taking care not to submit the page until your browser has finished loading. If this Error occurs again, please report this problem To your site administrator. The first Error was [" + field.nameLc + " not found]. There may have been others.");
                                }
                                throw (new GenericException("Unexpected exception"));
                            }
                        }
                        if (GenericController.encodeInteger(ResponseFieldValueText) != GenericController.encodeInteger(editRecord.fieldsLc[field.nameLc].value_content)) {
                            //
                            // new value
                            editRecord.fieldsLc[field.nameLc].value_content = ResponseFieldValueText;
                            ResponseFieldIsEmpty = false;
                        }
                        break;
                    case "active":
                        //
                        // anyone can change active
                        if (editRecord.allowAdminFieldCheck(core) && (!InResponse) && (!InEmptyFieldList)) {
                            Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The error is [" + field.nameLc + " not found].");
                            return;
                        }
                        bool responseValue = core.docProperties.getBoolean(field.nameLc);
                        if (!responseValue.Equals(encodeBoolean(editRecord.fieldsLc[field.nameLc].value_content))) {
                            //
                            // new value
                            editRecord.fieldsLc[field.nameLc].value_content = responseValue;
                            ResponseFieldIsEmpty = false;
                        }
                        break;
                    case "sortorder":
                    case "ccguid":
                        //
                        // -- anyone can change
                        InEmptyFieldList = FormEmptyFieldLcList.Contains(field.nameLc);
                        InResponse = core.docProperties.containsKey(field.nameLc);
                        if (editRecord.allowAdminFieldCheck(core)) {
                            if ((!InResponse) && (!InEmptyFieldList)) {
                                Processor.Controllers.ErrorController.addUserError(core, "There has been an error reading the response from your browser. Please try your change again. If this error occurs again, please report this problem To your site administrator. The error is [" + field.nameLc + " not found].");
                                return;
                            }
                        }
                        if (ResponseFieldValueText != encodeText(editRecord.fieldsLc[field.nameLc].value_content)) {
                            //
                            // new value
                            editRecord.fieldsLc[field.nameLc].value_content = ResponseFieldValueText;
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
                        } else if (editRecord.allowAdminFieldCheck(core) && (!InResponse) && (!InEmptyFieldList)) {
                            //
                            // Was sent out non-blank, and no response back, flag error and leave the current value to a retry
                            string errorMessage = "There has been an error reading the response from your browser. The field[" + field.caption + "]" + TabCopy + " was missing. Please try your change again. If this error happens repeatedly, please report this problem to your site administrator.";
                            ErrorController.addUserError(core, errorMessage);
                            logger.Error($"{core.logCommonMessage}", new GenericException(errorMessage));
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
                                            if ((lcaseCopy == Processor.Constants.HTMLEditorDefaultCopyNoCr) || (lcaseCopy == Processor.Constants.HTMLEditorDefaultCopyNoCr2)) {
                                                //
                                                // if the editor was left blank, remote the default copy
                                                //
                                                ResponseFieldValueText = "";
                                            } else {
                                                if (GenericController.strInstr(1, ResponseFieldValueText, Processor.Constants.HTMLEditorDefaultCopyStartMark) != 0) {
                                                    //
                                                    // if the default copy was editing, remote the markers
                                                    //
                                                    ResponseFieldValueText = GenericController.strReplace(ResponseFieldValueText, Processor.Constants.HTMLEditorDefaultCopyStartMark, "");
                                                    ResponseFieldValueText = GenericController.strReplace(ResponseFieldValueText, Processor.Constants.HTMLEditorDefaultCopyEndMark, "");
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
                                    blockDuplicateEmail = (core.siteProperties.getBoolean( Contensive.Processor.Constants.sitePropertyName_AllowEmailLogin, false));
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
                    editRecord.fieldsLc[field.nameLc].value_content = ResponseFieldValueText;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
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
    }
}
