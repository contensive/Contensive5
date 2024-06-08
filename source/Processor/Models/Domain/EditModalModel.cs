using Amazon.SecretsManager.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Contensive.BaseClasses;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mime;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    //
    public class EditModalModel {
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
        public EditModalModel(CoreController core, ContentMetadataModel contentMetadata, int recordId, bool allowCut, string recordName, string customCaption, string presetNameValuePairs) {
            using (CPCSBaseClass currentRecordCs = core.cpParent.CSNew()) {
                if (recordId > 0) { currentRecordCs.OpenRecord(contentMetadata.name, recordId); }
                dialogCaption = string.IsNullOrEmpty(customCaption) ? $"Edit {recordName}" : customCaption;
                adminEditUrl = AdminUIEditButtonController.getEditUrl(core, contentMetadata.id, recordId);
                isEditing = !core.session.isEditing();
                leftFields = getFieldList(core, currentRecordCs, contentMetadata, presetNameValuePairs);
                rightFields = [];
                this.recordId = recordId;
                contentGuid = contentMetadata.guid;
                editId = GenericController.getRandomString(5);
                contentItemName = GenericController.getSingular_Sortof(core, contentMetadata.name);
                pageId = core.doc.pageController.page.id;
                string instanceId = core.docProperties.getText("instanceId");
                bool isWidget = false;
                if (!string.IsNullOrEmpty(instanceId) && currentRecordCs.OK() && currentRecordCs.GetText("CCGUID") == instanceId) {
                    isWidget = true;
                }
                allowDeleteData = !isWidget;
                allowDeleteWidget = isWidget;
            }
        }
        //
        public string dialogCaption { get; }
        //
        public string adminEditUrl { get; }
        //
        public bool isEditing { get; }
        //
        public List<EditModalModel_FieldListItem> leftFields { get; }
        //
        public EditModalModel_Rightfield[] rightFields { get; }
        //
        public int recordId { get; }
        //
        public string contentGuid { get; }
        //
        public string editId { get; }
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
        /// <summary>
        /// Get the list of fields to be edited
        /// </summary>
        /// <param name="core"></param>
        /// <param name="currentRecordCs"></param>
        /// <param name="contentMetadata"></param>
        /// <param name="presetNameValuePairs">comma separated list of name=value pairs to prepopulate</param>
        /// <returns></returns>
        private static List<EditModalModel_FieldListItem> getFieldList(CoreController core, CPCSBaseClass currentRecordCs, ContentMetadataModel contentMetadata, string presetNameValuePairs) {
            List<EditModalModel_FieldListItem> result = [];
            Dictionary<string, string> prepopulateValue = [];
            if (!string.IsNullOrEmpty(presetNameValuePairs)) {
                //
                // -- create dictionary of name/values that should be prepopulated during an add
                foreach (var keyValuePair in presetNameValuePairs.Split(',')) {
                    if (!string.IsNullOrEmpty(keyValuePair)) {
                        string[] keyValue = keyValuePair.Split('=');
                        if (keyValue.Length == 2 && !prepopulateValue.ContainsKey(keyValue[0].ToLowerInvariant())) {
                            prepopulateValue.Add(keyValue[0].ToLowerInvariant(), keyValue[1]);
                        }
                    }
                }
            }
            //
            // -- iterate through all the fields in the content, adding the ones needed/allowed
            int editRecordId = currentRecordCs.OK() ? currentRecordCs.GetInteger("id") : 0;
            foreach (KeyValuePair<string, ContentFieldMetadataModel> fieldKvp in contentMetadata.fields) {
                string fieldName = fieldKvp.Key;
                ContentFieldMetadataModel field = fieldKvp.Value;
                if (string.IsNullOrEmpty(field.editTabName) && AdminDataModel.isVisibleUserField(core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, contentMetadata.tableName)) {
                    string currentValue = "";
                    if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.ManyToMany || field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect) {
                        // 
                        // -- these field types have no value
                    } else {
                        //
                        // -- capture current record value
                        if (prepopulateValue.ContainsKey(fieldName.ToLowerInvariant())) {
                            currentValue = prepopulateValue[fieldName.ToLowerInvariant()];
                        } else if (currentRecordCs.OK()) {
                            currentValue = currentRecordCs.GetValue(field.nameLc);
                            //
                            // -- file type fields read the filename, but save the content
                            switch (field.fieldTypeId) {
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                                case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                                case CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                                        currentValue = core.cpParent.CdnFiles.Read(currentValue);
                                        break;
                                    }
                            }
                        }
                    }
                    result.Add(new EditModalModel_FieldListItem(core, field, currentValue, editRecordId, contentMetadata.fields, contentMetadata));
                } else if (prepopulateValue.ContainsKey(fieldName.ToLowerInvariant())) {
                    //
                    // -- else add a hidden for the prepopulate value
                    result.Add(new EditModalModel_FieldListItem(core, field, prepopulateValue[fieldName.ToLowerInvariant()], editRecordId, contentMetadata.fields, contentMetadata));
                }
            }
            List<EditModalModel_FieldListItem> sortedResult = result.OrderBy(o => o.sort).ToList();
            return result;
        }
        //
        public static bool isFieldInModal(CoreController core, ContentFieldMetadataModel field, ContentMetadataModel contentMetadata) {
            return (string.IsNullOrEmpty(field.editTabName) && AdminDataModel.isVisibleUserField(core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, contentMetadata.tableName));
        }
    }

    public class EditModalModel_FieldListItem {
        //
        /// <summary>
        /// constructor
        /// </summary>
        public EditModalModel_FieldListItem(CoreController core, ContentFieldMetadataModel field, string currentValue, int editRecordId, Dictionary<string, ContentFieldMetadataModel> fields, ContentMetadataModel contentMetaData) {
            htmlName = $"field{field.id}";
            caption = field.caption;
            help = field.helpMessage;
            this.currentValue = currentValue;
            isHelp = !string.IsNullOrEmpty(field.helpMessage);
            isRequired = field.required;
            isReadOnly = field.readOnly;
            //
            isFile = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.File;
            isText = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Text;
            isTextLong = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.LongText) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileText);
            isInteger = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Integer;
            isDate = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Date;
            isHtml = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.HTML) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileHTML);
            isHtmlCode = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.HTMLCode) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode);
            isLink = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Link) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.ResourceLink);
            isCheckboxList = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.ManyToMany;
            isBoolean = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Boolean;
            isSelect = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Lookup) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.MemberSelect);
            isCurrency = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Currency;
            isImage = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileImage;
            isFloat = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Float;
            isCSS = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileCSS;
            isJavaScript = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileJavascript;
            isXML = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileXML;
            isRedirect = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Redirect;

            // -- cache this or pass from calling method
            List<FieldTypeEditorAddonModel> fieldTypeDefaultEditors = EditorController.getFieldEditorAddonList(core);




            // -- code editor
            EditorRowClass.editorResponse editorResponse = EditorRowClass.getEditor(core, new EditorRowClass.EditorRequest() {
                contentId = field.contentId,
                contentName = contentMetaData.name,
                editorAddonListJSON = core.html.getWysiwygAddonList(CPHtml5BaseClass.EditorContentType.contentTypeWeb),
                editRecordId = editRecordId,
                editViewTitleSuffix = "",
                field = field,
                fieldTypeEditors = fieldTypeDefaultEditors,
                formFieldList = null,
                isContentRootPage = false,
                record_readOnly = field.readOnly,
                styleList = "",
                styleOptionList = null,
                tableName = contentMetaData.tableName,
                editRecordNameLc = "",
                editRecordContentControlId = 0,
                currentValue = currentValue,
                fields = fields,
                htmlName = htmlName
            });
            customEditor = editorResponse.editorString;



            textMaxLength = isText ? 255 : (isTextLong ? 65353 : ((isHtml || isHtmlCode) ? 65535 : 255));
            numberMin = 0;
            numberMax = 2147483647;
            fieldClearName = $"field{field.id}delete";
            placeholder = $"{field.caption}";
            fieldId = $"field{field.id}";
            isChecked = isBoolean && GenericController.encodeBoolean(currentValue);
            sort = field.editSortPriority;
            if (isSelect) {
                selectOptionList = getSelectOptionList(core, field, currentValue);
            }
            imageUrl = !isImage ? "" : string.IsNullOrEmpty(currentValue) ? "/img/picturePlaceholder.jpg" : core.cpParent.Http.CdnFilePathPrefixAbsolute + currentValue;
            fileUrl = !isFile && !isImage ? "" : string.IsNullOrEmpty(currentValue) ? "" : core.cpParent.Http.CdnFilePathPrefixAbsolute + currentValue;
            fileName = !isFile && !isImage ? "" : string.IsNullOrEmpty(currentValue) ? "" : Path.GetFileName(currentValue);
        }

        public string htmlName { get; }
        public string caption { get; }
        public string help { get; }
        public string currentValue { get; }
        public bool isHelp { get; }
        public bool isRequired { get; }
        public bool isReadOnly { get; }
        public bool isInteger { get; }
        public bool isText { get; }
        public bool isTextLong { get; }
        public bool isBoolean { get; }
        public bool isDate { get; }
        public bool isFile { get; }
        public bool isSelect { get; }
        public bool isCurrency { get; }
        public bool isImage { get; }
        public bool isFloat { get; }
        public bool isCSS { get; }
        public bool isJavaScript { get; }
        public bool isXML { get; }
        public bool isCheckboxList { get; }
        public bool isLink { get; }
        public bool isHtml { get; }
        public bool isHtmlCode { get; }
        public bool isRedirect { get; }
        public int textMaxLength { get; }
        public int numberMin { get; }
        public int numberMax { get; }
        public string fieldClearName { get; }
        public string placeholder { get; }
        public string fieldId { get; }
        public bool isChecked { get; }
        public int sort { get; }
        public string selectOptionList { get; }
        /// <summary>
        /// for image types, this is the image currently loaded, or the default image /img/picturePlaceholder.jpg
        /// </summary>
        public string imageUrl { get; }
        /// <summary>
        /// AdminUI editor input
        /// </summary>
        public string customEditor { get; }
        /// <summary>
        /// if isFile or isImage, this is the download path to the file
        /// </summary>
        public string fileUrl { get; }
        /// <summary>
        /// if isFile or isImage, this is the name of the file without path
        /// </summary>
        public string fileName { get; }

        //
        /// <summary>
        /// create the select input option list for lookup field types using AdminUI, and remove the select wrapper
        /// </summary>
        /// <param name="core"></param>
        /// <param name="field"></param>
        /// <param name="fieldValueObject"></param>
        /// <returns></returns>
        public static string getSelectOptionList(CoreController core, ContentFieldMetadataModel field, string fieldValueObject) {
            string EditorString = "";
            bool IsEmptyList = false;
            string whyReadOnlyMsg = "";
            string fieldHtmlId = "not used";
            if (!field.lookupContentId.Equals(0)) {
                EditorString = AdminUIEditorController.getLookupContentEditor(core, field.nameLc, GenericController.encodeInteger(fieldValueObject), field.lookupContentId, ref IsEmptyList, field.readOnly, fieldHtmlId, whyReadOnlyMsg, field.required, field.LookupContentSqlFilter);
                EditorString = removeOptionsFromSelect(EditorString);
                return EditorString;
            }
            if (!string.IsNullOrEmpty(field.lookupList)) {
                EditorString = AdminUIEditorController.getLookupListEditor(core, field.nameLc, GenericController.encodeInteger(fieldValueObject), field.lookupList.Split(',').ToList(), field.readOnly, fieldHtmlId, whyReadOnlyMsg, field.required);
                EditorString = removeOptionsFromSelect(EditorString);
                return EditorString;
            }
            return EditorString;
        }
        //
        public static string removeOptionsFromSelect(string selectTag) {
            string result = selectTag;
            int pos = result.IndexOf("<option", 0, System.StringComparison.InvariantCultureIgnoreCase);
            if (pos < 0) { return ""; }
            result = result.Substring(pos);
            pos = result.IndexOf("</select", 0, System.StringComparison.InvariantCultureIgnoreCase);
            if (pos < 0) { return ""; }
            result = result.Substring(0, pos);
            return result;
        }
    }

    public class EditModalModel_Rightfield {
        public string id { get; }
        public string htmlName { get; }
        public string caption { get; }
        public string help { get; }
        public bool isHelp { get; }
        public List<EditModalModel_FieldListItem> rightSectionAccordion { get; }
    }
}