using Amazon.Runtime.Internal.Transform;
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
    public class EditModalModel_RightGroup {
        /// <summary>
        /// unique id for this group
        /// </summary>
        public string groupId { get; set; }
        public string htmlName { get; set; }
        public string caption { get; set; }
        public string help { get; set; }
        public bool isHelp { get; set; }
        public List<EditModalModel_Field> rightGroupFields { get; set; }
        ////
        //public static EditModalModel_RightGroup getContainerStyles(CoreController core, ContentMetadataModel contentMetadata, Dictionary<string, ContentFieldMetadataModel> styleFields, string editModalSn, CPCSBaseClass currentRecordCs) {
        //    int recordId = currentRecordCs.OK() ? currentRecordCs.GetInteger("id") : 0;
        //    string help = "";
        //    EditModalModel_RightGroup result = new() {
        //        groupId = GenericController.getRandomString(4),
        //        htmlName = "",
        //        caption = "Container Styles",
        //        help = help,
        //        isHelp = string.IsNullOrEmpty(help),
        //        rightGroupFields = []
        //    };
        //    if (styleFields.ContainsKey("padtop")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("padtop") : "0";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["padtop"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("padright")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("padright") : "0";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["padright"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("padbottom")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("padbottom") : "0";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["padbottom"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("padleft")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("padleft") : "0";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["padleft"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("asfullbleed")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("asfullbleed") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["asfullbleed"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("backgroundimagefilename")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("backgroundimagefilename") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["backgroundimagefilename"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("styleheight")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("styleheight") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["styleheight"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    return result;
        //}
        ////
        //// ====================================================================================================
        ////
        //public static EditModalModel_RightGroup getButtonFields(CoreController core, ContentMetadataModel contentMetadata, Dictionary<string, ContentFieldMetadataModel> styleFields, string editModalSn, CPCSBaseClass currentRecordCs) {
        //    int recordId = currentRecordCs.OK() ? currentRecordCs.GetInteger("id") : 0;
        //    string help = "";
        //    EditModalModel_RightGroup result = new() {
        //        groupId = GenericController.getRandomString(4),
        //        htmlName = "",
        //        caption = "Button Styles",
        //        help = help,
        //        isHelp = string.IsNullOrEmpty(help),
        //        rightGroupFields = []
        //    };
        //    if (styleFields.ContainsKey("buttontext")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("buttontext") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["buttontext"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("buttonurl")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("buttonurl") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["buttonurl"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("btnstyleselector")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("btnstyleselector") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["btnstyleselector"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    return result;
        //}
        ////
        //// ====================================================================================================
        ///// <summary>
        ///// find all edit groups, then loop through them and call getEditGroup()
        ///// </summary>
        ///// <param name="core"></param>
        ///// <param name="contentMetadata"></param>
        ///// <param name="fields"></param>
        ///// <param name="editModalSn"></param>
        ///// <param name="currentRecordCs"></param>
        ///// <returns></returns>
        //public static EditModalModel_RightGroup getEditGroups(CoreController core, ContentMetadataModel contentMetadata, Dictionary<string, ContentFieldMetadataModel> fields, string editModalSn, CPCSBaseClass currentRecordCs) {
        //    //
        //    // -- 
        //    Dictionary<string, List<ContentFieldMetadataModel>> fieldsByGroupName = [];
        //    foreach (KeyValuePair<string, ContentFieldMetadataModel> fieldNVP in fields) {
        //        ContentFieldMetadataModel field = fieldNVP.Value;
        //        if (!string.IsNullOrEmpty(field.editGroupName) && !fieldsByGroupName.ContainsKey(field.editGroupName)) {
        //            fieldsByGroupName.Add(field.editGroupName, field);
        //        }
        //    }
        //    //
        //    if (fieldsByGroupName.Count == 0) { return fieldsByGroupName; }
        //}
        ////
        //// ====================================================================================================
        ////
        //public static EditModalModel_RightGroup getEditGroupFields(CoreController core, ContentMetadataModel contentMetadata, Dictionary<string, ContentFieldMetadataModel> editGroupFields, string editModalSn, CPCSBaseClass currentRecordCs) {
        //    int recordId = currentRecordCs.OK() ? currentRecordCs.GetInteger("id") : 0;
        //    string help = "";
        //    EditModalModel_RightGroup result = new() {
        //        groupId = GenericController.getRandomString(4),
        //        htmlName = "",
        //        caption = "Button Styles",
        //        help = help,
        //        isHelp = string.IsNullOrEmpty(help),
        //        rightGroupFields = []
        //    };
        //    if (editGroupFields.ContainsKey("buttontext")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("buttontext") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, editGroupFields["buttontext"], currentValue, recordId, editGroupFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (editGroupFields.ContainsKey("buttonurl")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("buttonurl") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, editGroupFields["buttonurl"], currentValue, recordId, editGroupFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (editGroupFields.ContainsKey("btnstyleselector")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("btnstyleselector") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, editGroupFields["btnstyleselector"], currentValue, recordId, editGroupFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    return result;
        //}
        //
        // ====================================================================================================
        //
        /// <summary>
        /// Get the list of fields to be edited
        /// </summary>
        /// <param name="core"></param>
        /// <param name="currentRecordCs"></param>
        /// <param name="contentMetadata"></param>
        /// <param name="presetNameValuePairs">comma separated list of name=value pairs to prepopulate</param>
        /// <param name="editModalSn">a unique string for the current editor (edit tag plus modal)</param>
        /// <returns></returns>
        public static List<EditModalModel_RightGroup> getRightGroups(CoreController core, CPCSBaseClass currentRecordCs, ContentMetadataModel contentMetadata, string presetNameValuePairs, string editModalSn) {
            List<EditModalModel_RightGroup> result = [];
            Dictionary<string, string> prepopulateValue = [];
            int recordId = currentRecordCs.OK() ? currentRecordCs.GetInteger("id") : 0;
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
            // -- get a list of groups with all group fields in it
            Dictionary<string, List<ContentFieldMetadataModel>> fieldsByGroupName = [];
            foreach (KeyValuePair<string, ContentFieldMetadataModel> fieldNVP in contentMetadata.fields) {
                ContentFieldMetadataModel field = fieldNVP.Value;
                // -- create new group with an empty list of fields
                if (!string.IsNullOrEmpty(field.editGroupName)) {
                    if (!fieldsByGroupName.ContainsKey(field.editGroupName)) {
                        fieldsByGroupName.Add(field.editGroupName, []);
                    }
                    // -- add this field to the group
                    fieldsByGroupName[field.editGroupName].Add(field);
                }
            }
            //
            if (fieldsByGroupName.Count == 0) { return []; }
            //
            // -- go through the list of groups and add RightGroups to the result
            foreach (var groupName in fieldsByGroupName.Keys) {
                EditModalModel_RightGroup rightGroup = new() {
                    groupId = GenericController.getRandomString(4),
                    htmlName = "",
                    caption = groupName,
                    help = "",
                    isHelp = string.IsNullOrEmpty(""),
                    rightGroupFields = []
                };
                foreach (var field in fieldsByGroupName[groupName]) {
                    string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText(field.nameLc) : "";
                    rightGroup.rightGroupFields.Add(new EditModalModel_Field(core, field, currentValue, recordId, contentMetadata.fields, contentMetadata, editModalSn, false));
                }
                result.Add(rightGroup);
            }
            return result;
        }
        ////
        //// ====================================================================================================
        ////
        //public static EditModalModel_RightGroup addRightGroups_Fieldf(CoreController core, ContentMetadataModel contentMetadata, Dictionary<string, ContentFieldMetadataModel> styleFields, string editModalSn, CPCSBaseClass currentRecordCs) {
        //    int recordId = currentRecordCs.OK() ? currentRecordCs.GetInteger("id") : 0;
        //    string help = "";
        //    EditModalModel_RightGroup result = new() {
        //        groupId = GenericController.getRandomString(4),
        //        htmlName = "",
        //        caption = "Button Styles",
        //        help = help,
        //        isHelp = string.IsNullOrEmpty(help),
        //        rightGroupFields = []
        //    };
        //    if (styleFields.ContainsKey("buttontext")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("buttontext") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["buttontext"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("buttonurl")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("buttonurl") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["buttonurl"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    if (styleFields.ContainsKey("btnstyleselector")) {
        //        string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("btnstyleselector") : "";
        //        result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["btnstyleselector"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
        //    }
        //    //
        //    return result;
        //}



        ////
        //// -- filter the styles fields
        //Dictionary<string, ContentFieldMetadataModel> styleFields = [];
        //foreach (KeyValuePair<string, ContentFieldMetadataModel> field in contentMetadata.fields) {
        //    if (field.Value.editTabName.ToLower().Equals("styles")) {
        //        styleFields.Add(field.Key, field.Value);
        //    }
        //}
        ////
        //// -- style group
        //var stylegroup = getContainerStyles(core, contentMetadata, styleFields, editModalSn, currentRecordCs);
        //if (stylegroup != null && stylegroup.rightGroupFields.Count > 0) {
        //    result.Add(stylegroup);
        //}
        //// -- buttons without groups
        //var buttonGroup = getButtonFields(core, contentMetadata, contentMetadata.fields, editModalSn, currentRecordCs);
        //if (buttonGroup != null && buttonGroup.rightGroupFields.Count > 0) {
        //    result.Add(buttonGroup);
        //}
        //
        //return result;
    }

}

