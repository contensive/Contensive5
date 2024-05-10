using Contensive.Processor.Controllers;
using System.Collections.Generic;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    //
    public class EditModalModel {
        //
        public EditModalModel(CoreController core, ContentMetadataModel contentMetadata, int recordId, bool allowCut, string recordName, string customCaption) {
            dialogCaption = string.IsNullOrEmpty(customCaption) ? "Edit" : customCaption;
            adminEditUrl = AdminUIEditButtonController.getRecordEditUrl(core, contentMetadata.id, recordId);
            isEditing = !core.session.isEditing();
            leftFields = getFieldList(core, contentMetadata);
            rightFields = [];
        }
        public string dialogCaption { get; }
        public string adminEditUrl { get; }
        public bool isEditing { get; }
        public List<EditModalModel_FieldListItem> leftFields { get; }
        public EditModalModel_Rightfield[] rightFields { get; }
        //
        private static List<EditModalModel_FieldListItem> getFieldList(CoreController core, ContentMetadataModel contentMetadata) {
            List<EditModalModel_FieldListItem> result = new();
            foreach (KeyValuePair<string, ContentFieldMetadataModel> fieldKvp in contentMetadata.fields) {
                string fieldName = fieldKvp.Key;
                ContentFieldMetadataModel field = fieldKvp.Value;
                if (field.authorable) {
                    result.Add(new EditModalModel_FieldListItem(field));
                }
            }
            return result;
        }
    }

    public class EditModalModel_FieldListItem {
        //
        /// <summary>
        /// constructor
        /// </summary>
        public EditModalModel_FieldListItem(ContentFieldMetadataModel field) {
            htmlName = $"field-{field.id}-name";
            caption = field.caption;
            help = field.helpMessage;
            currentValue = field.defaultValue;
            isHelp = !string.IsNullOrEmpty(field.helpMessage);
            isRequired = field.required;
            isReadOnly = field.readOnly;
            isInteger = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Integer;
            isText = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Text;
            isTextLong = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.LongText) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileText);
            isBoolean = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Boolean;
            isDate = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Date;
            isFile = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.File || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileCSS) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileJavascript) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileXML);
            isSelect = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Lookup) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.MemberSelect);
            isCurrency = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Currency;
            isImage = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileImage;
            isFloat = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Float;
            isCheckboxList = field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.ManyToMany;
            isLink = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.Link) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.ResourceLink);
            isHtml = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.HTML) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileHTML);
            isHtmlCode = (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.HTMLCode) || (field.fieldTypeId == BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode);
            textMaxLength = (isText ? 255 : isTextLong ? 65353 : 0);
            numberMin = 0;
            numberMax = 2147483647;
            imageDeleteName = $"field-{field.id}-delete";
            placeholder = $"{field.caption}";
            id = $"field-{field.id}";
            isChecked = isBoolean ? GenericController.encodeBoolean(field.defaultValue) : false;
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
        public string id { get; }
        public bool isChecked { get; }
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