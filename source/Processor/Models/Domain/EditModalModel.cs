﻿using Amazon.SimpleEmail;
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
            dialogCaption = string.IsNullOrEmpty(customCaption) ? "Edit" : customCaption;
            adminEditUrl = AdminUIEditButtonController.getEditUrl(core, contentMetadata.id, recordId);
            isEditing = !core.session.isEditing();
            leftFields = getFieldList(core, contentMetadata, recordId, presetNameValuePairs);
            rightFields = [];
            this.recordId = recordId;
            contentGuid = contentMetadata.guid;
            editId = GenericController.getRandomString(5);
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
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="contentMetadata"></param>
        /// <param name="recordId"></param>
        /// <param name="presetNameValuePairs">comma separated list of name=value pairs to prepopulate</param>
        /// <returns></returns>
        private static List<EditModalModel_FieldListItem> getFieldList(CoreController core, ContentMetadataModel contentMetadata, int recordId, string presetNameValuePairs) {
            List<EditModalModel_FieldListItem> result = [];
            Dictionary<string, string> prepopulateValue = [];
            if (!string.IsNullOrEmpty(presetNameValuePairs)) {
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
            // -- create cs pointing to current record
            using (CPCSBaseClass cs = core.cpParent.CSNew()) {
                if (recordId > 0) { cs.OpenRecord(contentMetadata.name, recordId); }
                foreach (KeyValuePair<string, ContentFieldMetadataModel> fieldKvp in contentMetadata.fields) {
                    string fieldName = fieldKvp.Key;
                    ContentFieldMetadataModel field = fieldKvp.Value;
                    if (field.authorable && string.IsNullOrEmpty(field.editTabName)) {
                        string currentValue = "";
                        if (prepopulateValue.ContainsKey(fieldName.ToLowerInvariant())) {
                            currentValue = prepopulateValue[fieldName.ToLowerInvariant()];
                        } else if (cs.OK()) {
                            currentValue = cs.GetText(field.nameLc);
                        }
                        result.Add(new EditModalModel_FieldListItem(field, currentValue));
                    } else if (prepopulateValue.ContainsKey(fieldName.ToLowerInvariant())) {
                        //
                        // -- else add a hidden for the prepopulate value
                        result.Add(new EditModalModel_FieldListItem(field, prepopulateValue[fieldName.ToLowerInvariant()]));
                    }
                }
            }
            List<EditModalModel_FieldListItem> sortedResult = result.OrderBy(o => o.sort).ToList();
            return result;
        }
    }

    public class EditModalModel_FieldListItem {
        //
        /// <summary>
        /// constructor
        /// </summary>
        public EditModalModel_FieldListItem(ContentFieldMetadataModel field, string currentValue) {
            htmlName = $"field-{field.nameLc}";
            caption = field.caption;
            help = field.helpMessage;
            this.currentValue = currentValue;
            isHelp = !string.IsNullOrEmpty(field.helpMessage);
            isRequired = field.required;
            isReadOnly = field.readOnly;
            //
            isFile = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.File || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileCSS) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileJavascript) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileXML);
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
            textMaxLength = isText ? 255 : (isTextLong ? 65353 : ((isHtml || isHtmlCode) ? 65535 : 255));
            numberMin = 0;
            numberMax = 2147483647;
            imageDeleteName = $"field-{field.id}-delete";
            placeholder = $"{field.caption}";
            fieldId = $"field-{field.id}";
            isChecked = isBoolean && GenericController.encodeBoolean(field.defaultValue);
            sort = field.editSortPriority;
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
        public bool isCheckboxList { get; }
        public bool isLink { get; }
        public bool isHtml { get; }
        public bool isHtmlCode { get; }
        public int textMaxLength { get; }
        public int numberMin { get; }
        public int numberMax { get; }
        public string imageDeleteName { get; }
        public string placeholder { get; }
        public string fieldId { get; }
        public bool isChecked { get; }
        public int sort { get; }
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