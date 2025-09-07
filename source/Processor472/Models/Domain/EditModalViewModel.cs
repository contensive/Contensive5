using Amazon.SimpleEmail;
using Contensive.BaseClasses;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Db;
using NUglify.JavaScript.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    //
    public class EditModalViewModel {
        //
        /// <summary>
        /// if true, the edit tag is included in the modal
        /// </summary>
        public bool includeEditTag {
            get {
                return !string.IsNullOrWhiteSpace(recordGuid);
            }
        }
        //
        /// <summary>
        /// if true, the add tag is included in the modal
        /// </summary>
        public bool includeAddTag {
            get {
                return string.IsNullOrWhiteSpace(recordGuid);
            }
        }
        //
        /// <summary>
        /// modal caption
        /// </summary>
        public string dialogCaption {
            get {
                return string.IsNullOrEmpty(customCaption) ? $"Edit {recordName}" : customCaption;
            }
        }
        //
        /// <summary>
        /// the link to the admin site advance edit page
        /// </summary>
        public string adminEditUrl {
            get {
                return EditUIController.getEditUrl(core, contentMetadata.id, recordGuid);
            }
        }
        //
        /// <summary>
        /// true if this user is in edit mode
        /// </summary>
        public bool isEditing {
            get {
                return !core.session.isEditing();
            }
        }
        //
        /// <summary>
        /// The edit modal list of fields for the left
        /// </summary>
        public List<EditModalViewModel_Field> leftFields {
            get {
                setContentData();
                return contentData_leftFields;
            }
        }
        //
        /// <summary>
        /// the edit modal list of groups of fields for the right
        /// </summary>
        public List<EditModalViewModel_RightGroup> rightGroups {
            get {
                setContentData();
                return contentData_rightGroups;
            }
        }
        //
        /// <summary>
        /// true if there are right groups in the edit modal
        /// </summary>
        public bool hasRightGroups {
            get {
                setContentData();
                return contentData_rightGroups.Count > 0;
            }
        }
        //
        /// <summary>
        /// 
        /// </summary>
        public string recordGuid { get; }
        //
        /// <summary>
        /// the guid of hte content for the record being edited
        /// </summary>
        public string contentGuid {
            get {
                return contentMetadata.guid;
            }
        }
        //
        /// <summary>
        /// value that is unique for each editor instance. Use to prevent collisions when the same addon is used multiple times
        /// </summary>
        public string editModalSn {
            get {
                if (_editModalSn != null) { return _editModalSn; }
                _editModalSn = GenericController.getRandomString(5);
                return _editModalSn;
            }
        }
        private string _editModalSn = null;
        //
        /// <summary>
        /// for the add item layout, this is the name added to the "Add New {{addItemName}}"
        /// It is the singular of the content name
        /// </summary>
        public string contentItemName {
            get {
                return GenericController.getSingular_Sortof(core, contentMetadata.name);
            }
        }
        //
        /// <summary>
        /// allows the delete button.
        /// </summary>
        public bool allowDeleteWidget {
            get {
                setContentData();
                return contentData_AllowDeleteWidget;
            }
        }
        /// <summary>
        /// if true, the delete data button appears. Clicking it deletes this data record
        /// </summary>
        public bool allowDeleteData {
            get {
                setContentData();
                return contentData_allowDeleteData;
            }
        }
        //
        /// <summary>
        /// if delete-widget, this is the page to be updated
        /// </summary>
        public int pageId {
            get {
                return core.doc.pageController.page.id;
            }
        }
        //
        /// <summary>
        /// adds the paste link to add tab
        /// </summary>
        public bool isAllowPaste { get; }
        //
        /// <summary>
        /// adds the cut link to edit tab
        /// </summary>
        public bool isAllowCut { get; }
        //
        public string presetNameValueQS { 
            get {
                return string.Join("&", presetQSNameValues);
            }
        }
        //
        //
        // ====================================================================================================
        // -- privates
        // 
        private string recordName { get; }
        //
        private string customCaption { get; }
        //
        CoreController core { get; }
        //
        private ContentMetadataModel contentMetadata { get; }
        //
        public List<string> presetQSNameValues { get; } = [];
        //
        // ====================================================================================================
        //
        public static bool isFieldInModal(CoreController core, ContentFieldMetadataModel field, ContentMetadataModel contentMetadata) {
            return (string.IsNullOrEmpty(field.editTabName) || !string.IsNullOrEmpty(field.editGroupName)) && AdminDataModel.isVisibleUserField_EditModal(core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, contentMetadata.tableName);
        }
        //
        // ====================================================================================================
        //
        private void setContentData() {
            if(contentDataLoaded) { return; }
            //
            contentDataLoaded=true;
            //
            // -- determine if any fields use custom editors
            Dictionary<int,int> fieldTypeEditorAddons = ContentFieldModel.getFieldTypeEditorAddons(core);
            //
            using (CPCSBaseClass currentRecordCs = core.cpParent.CSNew()) {
                if (!string.IsNullOrWhiteSpace(recordGuid)) {
                    currentRecordCs.OpenRecord(contentMetadata.name, recordGuid);
                }
                contentData_rightGroups = EditModalViewModel_RightGroup.getRightGroups(core, currentRecordCs, contentMetadata, presetQSNameValues, editModalSn, fieldTypeEditorAddons);
                if(!currentRecordCs.OK()) {
                    //
                    // -- add new record
                    contentData_allowDeleteData = false;
                    contentData_AllowDeleteWidget = false;
                } else {
                    //
                    // -- edit record
                    if (!string.IsNullOrEmpty(instanceId) && currentRecordCs.OK() && currentRecordCs.GetText("CCGUID") == instanceId) {
                        // -- is widget
                        contentData_allowDeleteData = false;
                        contentData_AllowDeleteWidget = true;
                    } else {
                        // -- not widget
                        contentData_allowDeleteData = true;
                        contentData_AllowDeleteWidget = false;
                    }
                }
                contentData_leftFields = EditModalViewModel_Field.getLeftFields(core, currentRecordCs, contentMetadata, presetQSNameValues, editModalSn, contentData_rightGroups, fieldTypeEditorAddons);
            }
        }
        private bool contentDataLoaded { get; set; } = false;
        private string instanceId;
        private List<EditModalViewModel_RightGroup> contentData_rightGroups;
        private List<EditModalViewModel_Field> contentData_leftFields;
        private bool contentData_AllowDeleteWidget;
        private bool contentData_allowDeleteData;
        //
        // ====================================================================================================
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentMetadata"></param>
        /// <param name="recordId"></param>
        /// <param name="allowCut"></param>
        /// <param name="recordName"></param>
        /// <param name="customCaption"></param>
        /// <param name="presetQSNameValues">Comma separated list of field name=value to prepopulate fields. Add hiddens if these fields are not visible in the edit.</param>
        public EditModalViewModel(CoreController core, ContentMetadataModel contentMetadata, string recordGuid, bool allowCut, string recordName, string customCaption, List<string> presetQSNameValues) {
            //
            // -- move this to arguments, it is view
            this.instanceId = core.docProperties.getText("instanceId");
            //
            // -- store privates
            this.core = core;
            this.contentMetadata = contentMetadata;
            this.recordGuid = recordGuid;
            this.presetQSNameValues = presetQSNameValues;
            this.recordName = recordName;
            this.customCaption = customCaption;
            this.isAllowCut = allowCut;
            // todo
            this.isAllowPaste = false;
        }
    }
}