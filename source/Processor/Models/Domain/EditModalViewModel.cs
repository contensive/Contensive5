using Amazon.SimpleEmail;
using Contensive.BaseClasses;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentMetadata"></param>
        /// <param name="recordId"></param>
        /// <param name="allowCut"></param>
        /// <param name="recordName"></param>
        /// <param name="customCaption"></param>
        /// <param name="presetNameValuePairs">Comma separated list of field name=value to prepopulate fields. Add hiddens if these fields are not visible in the edit.</param>
        public EditModalViewModel(CoreController core, ContentMetadataModel contentMetadata, int recordId, bool allowCut, string recordName, string customCaption, string presetNameValuePairs) {
            using (CPCSBaseClass currentRecordCs = core.cpParent.CSNew()) {
                includeEditTag = !recordId.Equals(0);
                includeAddTag = !includeEditTag;
                if (recordId > 0) { 
                    currentRecordCs.OpenRecord(contentMetadata.name, recordId); 
                }
                editModalSn = GenericController.getRandomString(5);
                dialogCaption = string.IsNullOrEmpty(customCaption) ? $"Edit {recordName}" : customCaption;
                adminEditUrl = EditUIController.getEditUrl(core, contentMetadata.id, recordId);
                isEditing = !core.session.isEditing();
                this.recordId = recordId;
                contentGuid = contentMetadata.guid;
                contentItemName = GenericController.getSingular_Sortof(core, contentMetadata.name);
                pageId = core.doc.pageController.page.id;
                string instanceId = core.docProperties.getText("instanceId");
                if (!string.IsNullOrEmpty(instanceId) && currentRecordCs.OK() && currentRecordCs.GetText("CCGUID") == instanceId) {
                    // -- is widget
                    rightGroups = EditModalViewModel_RightGroup.getRightGroups(core, currentRecordCs, contentMetadata, presetNameValuePairs, editModalSn);
                    hasRightGroups = rightGroups.Count() > 0;
                    allowDeleteData = false;
                    allowDeleteWidget = true;
                } else {
                    // -- not widget
                    allowDeleteData = true;
                    allowDeleteWidget = false;
                    rightGroups = [];

                }
                leftFields = EditModalViewModel_Field.getLeftFields(core, currentRecordCs, contentMetadata, presetNameValuePairs, editModalSn, rightGroups);
            }
        }
        /// <summary>
        /// if true, the edit tag is included in the modal
        /// </summary>
        public bool includeEditTag { get; set; }
        /// <summary>
        /// if true, the add tag is included in the modal
        /// </summary>
        public bool includeAddTag { get; set; }
        //
        public string dialogCaption { get; }
        //
        public string adminEditUrl { get; }
        //
        public bool isEditing { get; }
        //
        public List<EditModalViewModel_Field> leftFields { get; }
        //
        public List<EditModalViewModel_RightGroup> rightGroups { get; }
        //
        public bool hasRightGroups { get; }
        //
        public int recordId { get; }
        //
        public string contentGuid { get; }
        /// <summary>
        /// value that is unique for each editor instance. Use to prevent collisions when the same addon is used multiple times
        /// </summary>
        public string editModalSn { get; }
        /// <summary>
        /// for the add item layout, this is the name added to the "Add New {{addItemName}}"
        /// It is the singular of the content name
        /// </summary>
        public string contentItemName { get; }
        /// <summary>
        /// True if this addon is executed from the pagemanagers addonList.
        /// Detect this if the instanceId is guid, and it matches the current records guid.
        /// if true, the delete widget button appears. 
        /// Clicking this button removes this widget from the pages addon list
        /// </summary>
        public bool allowDeleteWidget { get; }
        /// <summary>
        /// if true, the delete data button appears. Clicking it deletes this data record
        /// </summary>
        public bool allowDeleteData { get; }
        /// <summary>
        /// if delete-widget, this is the page to be updated
        /// </summary>
        public int pageId { get; }
        //
        public static bool isFieldInModal(CoreController core, ContentFieldMetadataModel field, ContentMetadataModel contentMetadata) {
            return (string.IsNullOrEmpty(field.editTabName) && AdminDataModel.isVisibleUserField_EditModal(core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, contentMetadata.tableName));
        }
    }
}