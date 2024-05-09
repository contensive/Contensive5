
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    //
    public class EditModalModel {
        //
        public  EditModalModel(CoreController core, ContentMetadataModel contentMetadata, int recordId, bool allowCut, string recordName, string customCaption) {
            dialogCaption = contentMetadata.name;
            anythingElseYouNeedForDialog = "";
            editTag = "";
            isEditing = !core.session.isEditing();
            leftFields = [];
            rightFields = [];
            carouselTestimonials = [];
        }
        public string dialogCaption { get; }
        public string anythingElseYouNeedForDialog { get; }
        public string editTag { get; }
        public bool isEditing { get; }
        public List<EditModalModel_Leftfield> leftFields { get; }
        public EditModalModel_Rightfield[] rightFields { get; }
        public EditModalModel_Carouseltestimonial[] carouselTestimonials { get; }
    }

    public class EditModalModel_Leftfield {
        public EditModalModel_Leftfield(CoreController core, ContentMetadataModel contentMetadata, int recordId, bool allowCut, string recordName, string customCaption) {

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
        public EditModalModel_Rightfield_Rightsectionaccordion[] rightSectionAccordion { get; }
    }

    public class EditModalModel_Rightfield_Rightsectionaccordion {
        public string currentValue { get; }
        public string placeholder { get; }
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
        public string id { get; }
        public object isHelp { get; }
        public string caption { get; }
        public bool isChecked { get; }
        public bool isRequired { get; }
        public bool isReadOnly { get; }
        public string help { get; }
        public string htmlName { get; }
    }

    public class EditModalModel_Carouseltestimonial {
        public int testimonialNumber { get; }
        public string activeTestimonial { get; }
        public EditModalModel_Carouseltestimonial_Star[] stars { get; }
        public string text { get; }
        public string userName { get; }
        public string avatar { get; }
        public string imageAlt { get; }
    }

    public class EditModalModel_Carouseltestimonial_Star {
        public bool isFullStar { get; }
        public bool isEmptyStar { get; }
        public bool isHalfStar { get; }
    }

}