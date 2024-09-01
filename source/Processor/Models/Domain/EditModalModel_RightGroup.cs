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
        //
        public static EditModalModel_RightGroup getContainerStyles(CoreController core, ContentMetadataModel contentMetadata, Dictionary<string, ContentFieldMetadataModel> styleFields, string editModalSn, CPCSBaseClass currentRecordCs) {
            int recordId = currentRecordCs.OK() ? currentRecordCs.GetInteger("id") : 0;
            string help = "";
            EditModalModel_RightGroup result = new() {
                groupId = GenericController.getRandomString(4),
                htmlName = "",
                caption = "Container Styles",
                help = help,
                isHelp = string.IsNullOrEmpty(help),
                rightGroupFields = []
            };
            if (styleFields.ContainsKey("padtop")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("padtop") : "0";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["padtop"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            if (styleFields.ContainsKey("padright")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("padright") : "0";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["padright"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            if (styleFields.ContainsKey("padbottom")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("padbottom") : "0";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["padbottom"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            if (styleFields.ContainsKey("padleft")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("padleft") : "0";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["padleft"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            if (styleFields.ContainsKey("asfullbleed")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("asfullbleed") : "";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["asfullbleed"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            if (styleFields.ContainsKey("backgroundimagefilename")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("backgroundimagefilename") : "";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["backgroundimagefilename"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            if (styleFields.ContainsKey("styleheight")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("styleheight") : "";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["styleheight"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            return result;
        }
        //
        public static EditModalModel_RightGroup getButtonFields(CoreController core, ContentMetadataModel contentMetadata, Dictionary<string, ContentFieldMetadataModel> styleFields, string editModalSn, CPCSBaseClass currentRecordCs) {
            int recordId = currentRecordCs.OK() ? currentRecordCs.GetInteger("id") : 0;
            string help = "";
            EditModalModel_RightGroup result = new() {
                groupId = GenericController.getRandomString(4),
                htmlName = "",
                caption = "Button Styles",
                help = help,
                isHelp = string.IsNullOrEmpty(help),
                rightGroupFields = []
            };
            if (styleFields.ContainsKey("buttontext")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("buttontext") : "";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["buttontext"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            if (styleFields.ContainsKey("buttonurl")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("buttonurl") : "";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["buttonurl"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            if (styleFields.ContainsKey("btnstyleselector")) {
                string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText("btnstyleselector") : "";
                result.rightGroupFields.Add(new EditModalModel_Field(core, styleFields["btnstyleselector"], currentValue, recordId, styleFields, contentMetadata, editModalSn, false));
            }
            //
            return result;
        }
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
            // -- filter the styles fields
            Dictionary<string, ContentFieldMetadataModel> styleFields = [];
            foreach (KeyValuePair<string, ContentFieldMetadataModel> field in contentMetadata.fields) {
                if (field.Value.editTabName.ToLower().Equals("styles")) {
                    styleFields.Add(field.Key, field.Value);
                }
            }
            //
            // -- style group
            var stylegroup = getContainerStyles(core, contentMetadata, styleFields, editModalSn, currentRecordCs);
            if (stylegroup != null && stylegroup.rightGroupFields.Count > 0) {
                result.Add(stylegroup);
            }
            // -- buttons without groups
            var buttonGroup = getButtonFields(core, contentMetadata, contentMetadata.fields, editModalSn, currentRecordCs);
            if (buttonGroup != null && buttonGroup.rightGroupFields.Count > 0) {
                result.Add(buttonGroup);
            }
            //
            return result;
        }

    }
}
