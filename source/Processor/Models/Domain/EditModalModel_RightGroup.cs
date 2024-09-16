using Amazon.Runtime.Internal.Transform;
using Amazon.SimpleEmail;
using Contensive.BaseClasses;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using NUglify.JavaScript.Syntax;
using System;
using System.Collections;
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
            SortedDictionary<string, List<ContentFieldMetadataModel>> fieldsByGroupName = [];
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
                var sortedFields = fieldsByGroupName[groupName];
                sortedFields.Sort((x, y) => x.nameLc.CompareTo(y.nameLc));
                foreach (var field in sortedFields) {
                    string currentValue = currentRecordCs.OK() ? currentRecordCs.GetText(field.nameLc) : "";
                    rightGroup.rightGroupFields.Add(new EditModalModel_Field(core, field, currentValue, recordId, contentMetadata.fields, contentMetadata, editModalSn, false));
                    rightGroup.help += string.IsNullOrEmpty(field.helpMessage) ? "" : field.caption + ": " + field.helpMessage + ". \n";
                }
                rightGroup.isHelp = string.IsNullOrEmpty(rightGroup.help);
                result.Add(rightGroup);
            }
            return result;
        }
    }

}

