using Amazon.SimpleEmail;
using Contensive.BaseClasses;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using NUglify.JavaScript.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Contensive.Processor.Models.Domain {
    public class EditModalModel_Field {
        //
        /// <summary>
        /// constructor
        /// </summary>
        public EditModalModel_Field(CoreController core,  ContentFieldMetadataModel field, string currentValue, int editRecordId, Dictionary<string, ContentFieldMetadataModel> fields, ContentMetadataModel contentMetaData, string editModalSn, bool isHidden) {
            htmlName = $"field{field.id}";
            caption = field.caption;
            help = field.helpMessage;
            this.currentValue = currentValue;
            isHelp = !string.IsNullOrEmpty(field.helpMessage);
            isRequired = field.required;
            isReadOnly = field.readOnly;
            //
            this.isHidden = isHidden;
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
            isJavaScript = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileJavaScript;
            isXML = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileXML;
            isRedirect = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Redirect;
            //
            // -- cache this or pass from calling method
            List<FieldTypeEditorAddonModel> fieldTypeDefaultEditors = core.cacheRuntime.fieldEditorAddonList;
            //
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
            fieldId = $"{editModalSn}{field.id}";
            isChecked = isBoolean && GenericController.encodeBoolean(currentValue);
            sort = field.editSortPriority;
            selectOptionList = !isSelect ? "" : getSelectOptionList(core, field, currentValue, contentMetaData);
            imageUrl = !isImage ? "" : string.IsNullOrEmpty(currentValue) ? "/img/picturePlaceholder.jpg" : core.cpParent.Http.CdnFilePathPrefixAbsolute + currentValue;
            fileUrl = !isFile && !isImage ? "" : string.IsNullOrEmpty(currentValue) ? "" : core.cpParent.Http.CdnFilePathPrefixAbsolute + currentValue;
            fileName = !isFile && !isImage ? "" : string.IsNullOrEmpty(currentValue) ? "" : Path.GetFileName(currentValue);
        }
        /// <summary>
        /// if true, the field is a hidden and should just include the htmlName and currentvalue
        /// </summary>
        public bool isHidden { get; }
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
        // ====================================================================================================
        //
        /// <summary>
        /// create the select input option list for lookup field types using AdminUI, and remove the select wrapper
        /// </summary>
        /// <param name="core"></param>
        /// <param name="field"></param>
        /// <param name="fieldValueObject"></param>
        /// <returns></returns>
        public static string getSelectOptionList(CoreController core, ContentFieldMetadataModel field, string fieldValueObject, ContentMetadataModel contentMetaData) {
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
            if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.MemberSelect) {
                int groupId = field.memberSelectGroupId_get(core, contentMetaData.name, field.nameLc);
                string groupName = core.cpParent.Content.GetRecordName("groups", groupId);
                EditorString = AdminUIEditorController.getMemberSelectEditor(core, field.nameLc, GenericController.encodeInteger(fieldValueObject), groupId, groupName, field.readOnly, fieldHtmlId, field.required, whyReadOnlyMsg);
                EditorString = removeOptionsFromSelect(EditorString);
                return EditorString;
            }
            return EditorString;
        }
        //
        // ====================================================================================================
        //
        /// <summary>
        /// return the options from a select tag
        /// </summary>
        /// <param name="selectTag"></param>
        /// <returns></returns>
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
        //
        // ====================================================================================================
        //
        /// <summary>
        /// Get the list of fields to be edited. 
        /// Fields in the main "details" tab that are not in the rightFields list.
        /// Exclude fields that are in the rightGroups
        /// Add hiddens for fields that are one of the presets but are not included in left and right lists
        /// </summary>
        /// <param name="core"></param>
        /// <param name="currentRecordCs"></param>
        /// <param name="contentMetadata"></param>
        /// <param name="presetNameValuePairs">comma separated list of name=value pairs to prepopulate</param>
        /// <param name="editModalSn">a unique string for the current editor (edit tag plus modal)</param>
        /// <returns></returns>
        public static List<EditModalModel_Field> getLeftFields(CoreController core, CPCSBaseClass currentRecordCs, ContentMetadataModel contentMetadata, string presetNameValuePairs, string editModalSn, List<EditModalModel_RightGroup> rightGroups) {
            List<EditModalModel_Field> result = [];
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
                //
                // -- search rightGroups for this field, if found, skip it
                if (rightGroups.Find( (x) => x.rightGroupFields.Find((x) => x.fileName == fieldName)!=null ) !=null) { continue; }
                //
                if (string.IsNullOrEmpty(field.editTabName) && AdminDataModel.isVisibleUserField(core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, contentMetadata.tableName)) {
                    string currentValue = "";
                    if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.ManyToMany || field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect) {
                        // 
                        // -- these field types have no value
                    } else {
                        //
                        // -- prepopulated values
                        if (prepopulateValue.ContainsKey(fieldName.ToLowerInvariant())) {
                            switch (field.fieldTypeId) {
                                case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                        DateTime? dateValue = GenericController.encodeDate(prepopulateValue[fieldName.ToLowerInvariant()]);
                                        currentValue = GenericController.encodeDateIsoString(dateValue);
                                        break;
                                    }
                                default: {
                                        currentValue = prepopulateValue[fieldName.ToLowerInvariant()];
                                        break;
                                    }
                            }
                        } else if (currentRecordCs.OK()) {
                            //
                            // -- current record value
                            switch (field.fieldTypeId) {
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                                case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                case CPContentBaseClass.FieldTypeIdEnum.FileJavaScript:
                                case CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                                        //
                                        // -- file type fields read the filename, but save the content
                                        currentValue = currentRecordCs.GetValue(field.nameLc);
                                        currentValue = core.cpParent.CdnFiles.Read(currentValue);
                                        break;
                                    }
                                case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                        DateTime? dateValue = currentRecordCs.GetDate(field.nameLc);
                                        currentValue = GenericController.encodeDateIsoString(dateValue);
                                        break;
                                    }
                                default: {
                                        currentValue = currentRecordCs.GetValue(field.nameLc);
                                        break;
                                    }
                            }
                        }
                    }
                    result.Add(new EditModalModel_Field(core, field, currentValue, editRecordId, contentMetadata.fields, contentMetadata, editModalSn, false));
                } else if (prepopulateValue.ContainsKey(fieldName.ToLowerInvariant())) {
                    //
                    // -- else add a hidden for the prepopulate value
                    result.Add(new EditModalModel_Field(core, field, prepopulateValue[fieldName.ToLowerInvariant()], editRecordId, contentMetadata.fields, contentMetadata, editModalSn, true));
                } else if (editRecordId == 0 && field.required) {
                    //
                    // -- if new record (add), and field is required, and not otherwise added, include it in the list
                    result.Add(new EditModalModel_Field(core, field, prepopulateValue[fieldName.ToLowerInvariant()], editRecordId, contentMetadata.fields, contentMetadata, editModalSn, false));
                }
            }
            List<EditModalModel_Field> sortedResult = result.OrderBy(o => o.sort).ToList();
            return result;
        }
    }

}
