
using Contensive.BaseClasses;
using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using static Contensive.Processor.Controllers.GenericController;
namespace Contensive.Processor.Addons.AdminSite.Models {
    /// <summary>
    /// 
    /// </summary>
    public class EditRecordModel {
        public Dictionary<string, EditRecordFieldModel> fieldsLc = new Dictionary<string, EditRecordFieldModel>();
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
                                LogController.logError(cp.core, new GenericException("A new record could not be inserted for content [" + adminData.adminContent.name + "]. Verify the Database table and field DateAdded, CreateKey, and ID."));
                            } else {
                                //
                                // Could not locate record you requested
                                //
                                LogController.logError(cp.core, new GenericException("The record you requested (ID=" + adminData.editRecord.id + ") could not be found for content [" + adminData.adminContent.name + "]"));
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
                                            adminData.editRecord.nameLc = GenericController.encodeText(fieldValueObject);
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
                                                if (string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
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
                                        case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
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
                                                cp.core.html.processCheckList("field" + field.id, MetadataController.getContentNameByID(cp.core, field.contentId), encodeText(adminData.editRecord.id), MetadataController.getContentNameByID(cp.core, field.manyToManyContentId), MetadataController.getContentNameByID(cp.core, field.manyToManyRuleContentId), field.manyToManyRulePrimaryField, field.manyToManyRuleSecondaryField);
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
                LogController.logError(cp.core, ex);
            }
        }
    }
}
