using Amazon.SimpleEmail;
using Contensive.BaseClasses;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using Contensive.Processor.Controllers.EditControls;
using NUglify.JavaScript.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Contensive.Processor.Models.Domain {
    public class EditModalViewModel_Field {
        //
        /// <summary>
        /// constructor
        /// </summary>
        public EditModalViewModel_Field(CoreController core, ContentFieldMetadataModel field, string currentValue, int editRecordId, Dictionary<string, ContentFieldMetadataModel> fields, ContentMetadataModel contentMetaData, string editModalSn, bool isHidden, Dictionary<int, int> FieldTypeEditorAddons) {
            try {
                id = field.id;
                htmlName = $"field{field.id}";
                caption = field.caption;
                help = field.helpMessage;
                this.currentValue = currentValue;
                isHelp = !string.IsNullOrEmpty(field.helpMessage);
                isRequired = field.required;
                isReadOnly = field.readOnly;
                //
                if (isHidden) {
                    //
                    // -- if this is a hidden exit now
                    this.isHidden = isHidden;
                    return;
                }
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
                if (!string.IsNullOrEmpty(field.editorAddonGuid) || FieldTypeEditorAddons.ContainsKey((int)field.fieldTypeId)) {
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
                }
                textMaxLength = isText ? 255 : (isTextLong ? 65353 : ((isHtml || isHtmlCode) ? 65535 : 255));
                numberMin = 0;
                numberMax = 2147483647;
                fieldClearName = $"field{field.id}delete";
                placeholder = $"{field.caption}";
                fieldId = $"{editModalSn}{field.id}";
                isChecked = isBoolean && GenericController.encodeBoolean(currentValue);
                sort = field.editSortPriority;
                selectOptionList = !isSelect ? "" : getSelectOptionList(core, field, currentValue, contentMetaData);
                imageUrl = !isImage ? "" : string.IsNullOrEmpty(currentValue) ? "/baseassets/picturePlaceholder.jpg" : core.cpParent.Http.CdnFilePathPrefixAbsolute + currentValue;
                fileUrl = !isFile && !isImage ? "" : string.IsNullOrEmpty(currentValue) ? "" : core.cpParent.Http.CdnFilePathPrefixAbsolute + currentValue;
                fileName = !isFile && !isImage ? "" : string.IsNullOrEmpty(currentValue) ? "" : Path.GetFileName(currentValue);
                redirectUrl = isRedirect ? field.redirectPath : "";
            } catch (Exception ex) {
                LogController.log(core, "exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                throw;
            }
        }
        /// <summary>
        /// if true, the field is a hidden and should just include the htmlName and currentvalue
        /// </summary>
        public int id { get; }
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
        /// for image types, this is the image currently loaded, or the default image /baseassets/picturePlaceholder.jpg
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
        /// <summary>
        /// if isRedirect, and this redirectpath is not null, the editor is an anchor tag to this url
        /// </summary>
        public string redirectUrl { get; }
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
        /// <param name="presetQSNameValues">comma separated list of name=value pairs to prepopulate</param>
        /// <param name="editModalSn">a unique string for the current editor (edit tag plus modal)</param>
        /// <returns></returns>
        public static List<EditModalViewModel_Field> getLeftFields(CoreController core, CPCSBaseClass currentRecordCs, ContentMetadataModel contentMetadata, List<string> presetQSNameValues, string editModalSn, List<EditModalViewModel_RightGroup> rightGroups, Dictionary<int, int> fieldTypeEditorAddons) {
            try {
                List<EditModalViewModel_Field> result = [];
                Dictionary<string, string> prepopulateValue = [];
                if (presetQSNameValues.Count>0) {
                    //
                    // -- create dictionary of name/values that should be prepopulated during an add
                    foreach (var keyValuePair in presetQSNameValues) {
                        if (!string.IsNullOrEmpty(keyValuePair)) {
                            string[] keyValue = keyValuePair.Split('=');
                            string keyName = GenericController.decodeResponseVariable(keyValue[0]).ToLowerInvariant();
                            if (keyValue.Length == 2 && !prepopulateValue.ContainsKey(keyName)) {
                                prepopulateValue.Add(keyName, GenericController.decodeResponseVariable(keyValue[1]).ToLowerInvariant());
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
                    string fieldNameLC = fieldKvp.Key.ToLowerInvariant();
                    //
                    // -- search rightGroups for this field, if found, skip it
                    if (rightGroups.Find((x) => x.rightGroupFields.Find((x) => x.id == field.id) != null) != null) { continue; }
                    //
                    if (string.IsNullOrEmpty(field.editTabName) && AdminDataModel.isVisibleUserField_EditModal(core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, contentMetadata.tableName)) {
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
                        result.Add(new EditModalViewModel_Field(core, field, currentValue, editRecordId, contentMetadata.fields, contentMetadata, editModalSn, false, fieldTypeEditorAddons));
                    } else if (prepopulateValue.ContainsKey(fieldName.ToLowerInvariant())) {
                        //
                        // -- else add a hidden for the prepopulate value
                        result.Add(new EditModalViewModel_Field(core, field, prepopulateValue[fieldName.ToLowerInvariant()], editRecordId, contentMetadata.fields, contentMetadata, editModalSn, true, fieldTypeEditorAddons));
                    } else if (editRecordId == 0 && field.required) {
                        //
                        // -- if new record (add), and field is required, and not otherwise added, include it in the list
                        result.Add(new EditModalViewModel_Field(core, field, field.defaultValue, editRecordId, contentMetadata.fields, contentMetadata, editModalSn, false, fieldTypeEditorAddons));
                        //result.Add(new EditModalViewModel_Field(core, field, prepopulateValue[fieldName.ToLowerInvariant()], editRecordId, contentMetadata.fields, contentMetadata, editModalSn, false, fieldTypeEditorAddons));
                    }
                }
                List<EditModalViewModel_Field> sortedResult = result.OrderBy(o => o.sort).ToList();
                return result;
            } catch (Exception ex) {
                LogController.log(core, "exception [" + ex + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                throw;
            }
        }
    }

}
