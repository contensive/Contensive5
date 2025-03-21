﻿using Amazon.Runtime.Internal.Transform;
using Amazon.SimpleEmail;
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using NUglify.JavaScript.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Contensive.Processor.Models.Domain {
    public class EditModalViewModel_RightGroup {
        /// <summary>
        /// unique id for this group
        /// </summary>
        public string groupId { get; set; }
        public string htmlName { get; set; }
        public string caption { get; set; }
        public string help { get; set; }
        public bool isHelp { get; set; }
        public List<EditModalViewModel_Field> rightGroupFields { get; set; }
        //
        // ====================================================================================================
        //
        /// <summary>
        /// Get the list of fields to be edited
        /// </summary>
        /// <param name="core"></param>
        /// <param name="currentRecordCs"></param>
        /// <param name="contentMetadata"></param>
        /// <param name="presetQSNameValues">comma separated list of name=value pairs to prepopulate</param>
        /// <param name="editModalSn">a unique string for the current editor (edit tag plus modal)</param>
        /// <returns></returns>
        public static List<EditModalViewModel_RightGroup> getRightGroups(CoreController core, CPCSBaseClass currentRecordCs, ContentMetadataModel contentMetadata, List<string> presetQSNameValues, string editModalSn, Dictionary<int, int> FieldTypeEditorAddons) {
            List<EditModalViewModel_RightGroup> result = [];
            Dictionary<string, string> prepopulateValue = [];
            int recordId = currentRecordCs.OK() ? currentRecordCs.GetInteger("id") : 0;
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
            // -- get a list of groups with all group fields in it
            SortedDictionary<string, List<ContentFieldMetadataModel>> fieldsByGroupName = [];
            foreach (KeyValuePair<string, ContentFieldMetadataModel> fieldNVP in contentMetadata.fields) {
                ContentFieldMetadataModel field = fieldNVP.Value;
                if (AdminDataModel.isVisibleUserField_EditModal(core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, contentMetadata.tableName)) {
                    if (!string.IsNullOrEmpty(field.editGroupName)) {
                        if (!fieldsByGroupName.ContainsKey(field.editGroupName)) {
                            fieldsByGroupName.Add(field.editGroupName, []);
                        }
                        // -- add this field to the group
                        fieldsByGroupName[field.editGroupName].Add(field);
                    }
                }
            }
            //
            if (fieldsByGroupName.Count == 0) { return []; }
            //
            // -- go through the list of groups and add RightGroups to the result
            foreach (var groupName in fieldsByGroupName.Keys) {
                EditModalViewModel_RightGroup rightGroup = new() {
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
                    rightGroup.rightGroupFields.Add(new EditModalViewModel_Field(core, field, currentValue, recordId, contentMetadata.fields, contentMetadata, editModalSn, false, FieldTypeEditorAddons));
                    rightGroup.help += string.IsNullOrEmpty(field.helpMessage) ? "" : field.caption + ": " + field.helpMessage + ". \n";
                }
                rightGroup.isHelp = string.IsNullOrEmpty(rightGroup.help);
                result.Add(rightGroup);
            }
            return result;
        }
    }

}

