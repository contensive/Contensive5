﻿
using System;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Contensive.Exceptions;
using System.Net;
using Contensive.Models.Db;
using System.Collections.Generic;
using NLog;
using Contensive.Processor.Controllers.EditControls;

namespace Contensive.Processor.Addons.AdminSite {
    public class EditViewTabControlInfo {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //========================================================================
        /// <summary>
        /// Control edit tab
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <returns></returns>
        public static string get(CoreController core, AdminDataModel adminData, EditorEnvironmentModel editorEnv) {
            string result;
            var tabPanel = new StringBuilderLegacyController();
            try {
                bool disabled = false;
                //
                if (string.IsNullOrEmpty(adminData.adminContent.name)) {
                    //
                    // Content not found or not loaded
                    if (adminData.adminContent.id == 0) {
                        //
                        logger.Error($"{core.logCommonMessage}", new GenericException("No content definition was specified for this page"));
                        return HtmlController.p("No content was specified.");
                    } else {
                        //
                        // Content Definition was not specified
                        logger.Error($"{core.logCommonMessage}", new GenericException("The content definition specified for this page [" + adminData.adminContent.id + "] was not found"));
                        return HtmlController.p("No content was specified.");
                    }
                }
                //
                bool FieldRequired = false;
                //
                // -- fields with edittab set to control info, at top
                //
                List<string> TabsFound = [];
                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
                    string fieldTabName = field.editTabName.ToLowerInvariant();
                    if ((fieldTabName.Equals("control info") || fieldTabName.Equals("controlinfo")) && field.authorable && field.active) {
                        tabPanel.add(EditorRowClass.getEditorRow(core, field, adminData, editorEnv));
                    }
                }
                //
                // -- people-table-only, EID (Encoded ID)
                {
                    if (GenericController.toUCase(adminData.adminContent.tableName) == GenericController.toUCase("ccMembers")) {
                        string htmlId = "fieldGuid";
                        bool AllowEId = (core.siteProperties.getBoolean("AllowLinkLogin", true)) || (core.siteProperties.getBoolean("AllowLinkRecognize", true));
                        string fieldHelp = "This string is an authentication token that can be used in the URL for the next 15 minutes to log in as this user.";
                        string fieldEditor = "";
                        if (!AllowEId) {
                            fieldEditor = "(link login and link recognize are disabled in security preferences)";
                        } else if (adminData.editRecord.id == 0) {
                            fieldEditor = "(available after save)";
                        } else {
                            string eidQueryString = "eid=" + WebUtility.UrlEncode(SecurityController.encodeToken(core, adminData.editRecord.id, core.doc.profileStartTime.AddMinutes(15)));
                            string sampleUrl = core.webServer.requestProtocol + core.webServer.requestDomain + "/" + core.siteProperties.serverPageDefault + "?" + eidQueryString;
                            if (core.siteProperties.getBoolean("AllowLinkLogin", true)) {
                                fieldHelp = " If " + eidQueryString + " is added to a url querystring for this site, the user be logged in as this person.";
                            } else {
                                fieldHelp = " If " + eidQueryString + " is added to a url querystring for this site, the user be recognized in as this person, but not logged in.";
                            }
                            fieldHelp += " To enable, disable or modify this feature, use the security tab on the Preferences page.";
                            fieldHelp += "<br>For example: " + sampleUrl;
                            fieldEditor = AdminUIEditorController.getTextEditor(core, "ignore_eid", eidQueryString, true, htmlId);
                        }
                        tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "Member Link Login Querystring", fieldHelp, true, false, htmlId));
                    }
                }
                //
                // -- RecordID
                {
                    string fieldValue = (adminData.editRecord.id == 0) ? "(available after save)" : adminData.editRecord.id.ToString();
                    string fieldEditor = AdminUIEditorController.getTextEditor(core, "ignore", fieldValue, true, "");
                    string fieldHelp = "This is the unique number that identifies this record within this content.";
                    tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "Record Number", fieldHelp, true));
                }
                //
                // -- Active
                {
                    string htmlId = "fieldActive";
                    string fieldEditor = HtmlController.checkbox("active", adminData.editRecord.active, htmlId, disabled, "", adminData.editRecord.userReadOnly);
                    string fieldHelp = "When unchecked, add-ons can ignore this record as if it was temporarily deleted.";
                    tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "Active", fieldHelp, false, false, htmlId));
                }
                //
                // -- Sort Order
                {
                    bool fieldFound = adminData.editRecord.fieldsLc.ContainsKey("sortorder");
                    if(fieldFound) {
                        string fieldValue = "";
                        object fieldObject = adminData.editRecord.fieldsLc["sortorder"].value_content;
                        if (fieldObject != null) {
                            fieldValue = fieldObject.ToString();
                        }
                        string fieldEditor = AdminUIEditorController.getTextEditor(core, "sortOrder", fieldValue, false, "");
                        string fieldHelp = "Can be used to set the order of records displayed in lists. Is sorted alphabetically.";
                        tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "Sort Order", fieldHelp, false));
                    }
                }
                //
                // -- GUID
                {
                    string guidSetHtmlId = "guidSet" + GenericController.getRandomInteger().ToString();
                    string guidInputHtmlId = "guidInput" + GenericController.getRandomInteger().ToString();
                    string fieldValue = GenericController.encodeText(adminData.editRecord.fieldsLc["ccguid"].value_content);
                    string fieldEditor = "";
                    if (adminData.editRecord.userReadOnly) {
                        //
                        // -- readonly
                        fieldEditor = AdminUIEditorController.getTextEditor(core, "ignore", fieldValue, true, "");
                    } else if (string.IsNullOrEmpty(fieldValue)) {
                        //
                        // add a set button
                        string setButton = "<input id=\"" + guidSetHtmlId + "\" type=\"submit\" value=\"Set\" class=\"btn btn-primary btn-sm\">";
                        string setButtonWrapped = "<div class=\"input-group-append\">" + setButton + "</div>";
                        string inputCell = AdminUIEditorController.getTextEditor(core, "ccguid", "", false, guidInputHtmlId);
                        fieldEditor = HtmlController.div(inputCell + setButtonWrapped, "input-group");
                        string newGuid = GenericController.getGUID(true);
                        string onClickFn = "function(e){e.preventDefault();e.stopPropagation();$('#" + guidInputHtmlId + "').val('" + newGuid + "');}";
                        string script = "$('body').on('click','#" + guidSetHtmlId + "'," + onClickFn + ")";
                        core.html.addScriptCode(script, "Admin edit control-info-tab guid set button");
                    } else {
                        //
                        // field is read-only except for developers
                        fieldEditor = AdminUIEditorController.getTextEditor(core, "ccguid", fieldValue, !core.session.isAuthenticatedDeveloper(), guidInputHtmlId);
                    }
                    string FieldHelp = "This is a unique number that identifies this record globally. A GUID is not required, but when set it should never be changed. GUIDs are used to synchronize records. When empty, you can create a new guid. Only Developers can modify the guid.";
                    tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "GUID", FieldHelp, false, false, guidInputHtmlId));
                }
                //
                // ----- Controlling Content
                {
                    string HTMLFieldString = "";
                    string FieldHelp = "The content in which this record is stored. This is similar to a database table.";
                    ContentFieldMetadataModel field = null;
                    if (adminData.adminContent.fields.ContainsKey("contentcontrolid")) {
                        field = adminData.adminContent.fields["contentcontrolid"];
                        //
                        // if this record has a parent id, only include CDefs compatible with the parent record - otherwise get all for the table
                        FieldHelp = GenericController.encodeText(field.helpMessage);
                        FieldRequired = GenericController.encodeBoolean(field.required);
                        int FieldValueInteger = (adminData.editRecord.contentControlId.Equals(0)) ? adminData.adminContent.id : adminData.editRecord.contentControlId;
                        if (core.session.isAuthenticatedAdmin() && !string.IsNullOrEmpty(adminData.editRecord.contentControlId_Name)) {
                            //
                            // administrator, let them select any content compatible with the table
                            bool IsEmptyList = false;
                            string TableName2 = MetadataController.getContentTablename(core, adminData.editRecord.contentControlId_Name);
                            if (!string.IsNullOrEmpty(TableName2)) {
                                int tableId = MetadataController.getRecordIdByUniqueName(core, "Tables", TableName2);
                                int contentCId = MetadataController.getRecordIdByUniqueName(core, ContentModel.tableMetadata.contentName, ContentModel.tableMetadata.contentName);
                                if (tableId > 0 && contentCId > 0) {
                                    HTMLFieldString += AdminUIEditorController.getLookupContentEditor(core, "contentcontrolid", FieldValueInteger, contentCId, ref IsEmptyList, adminData.editRecord.userReadOnly, "", "", true, $"(ContentTableID={tableId})");
                                    FieldHelp = FieldHelp + " (Only administrators have access to this control. Changing the Controlling Content allows you to change who can author the record, as well as how it is edited.)";
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(HTMLFieldString)) {
                            //
                            // -- just display the name and a hidden with the value
                            HTMLFieldString = string.IsNullOrEmpty(adminData.editRecord.contentControlId_Name) ? adminData.adminContent.name : adminData.editRecord.contentControlId_Name;
                            HTMLFieldString += HtmlController.inputHidden("contentControlId", adminData.editRecord.contentControlId);
                        }
                        tabPanel.add(AdminUIController.getEditRow(core, HTMLFieldString, "Controlling Content", FieldHelp, FieldRequired, false, ""));
                    }
                }
                //
                // ----- Created By
                {
                    string FieldHelp = "The people account of the user who created this record.";
                    string fieldValue = "";
                    if (adminData.editRecord == null) {
                        fieldValue = "(not set)";
                    } else if (adminData.editRecord.id == 0) {
                        fieldValue = "(available after save)";
                    } else if (adminData.editRecord.createdBy == null) {
                        fieldValue = "(not set)";
                    } else {
                        int FieldValueInteger = adminData.editRecord.createdBy.id;
                        if (FieldValueInteger == 0) {
                            fieldValue = "(not set)";
                        } else {
                            using (var csData = new CsModel(core)) {
                                csData.open("people", "(id=" + FieldValueInteger + ")", "name,active", false);
                                if (!csData.ok()) {
                                    fieldValue = "#" + FieldValueInteger + ", (deleted)";
                                } else {
                                    fieldValue = "#" + FieldValueInteger + ", " + csData.getText("name");
                                    if (!csData.getBoolean("active")) {
                                        fieldValue += " (inactive)";
                                    }
                                }
                                csData.close();
                            }
                        }
                    }
                    string fieldEditor = AdminUIEditorController.getTextEditor(core, "ignore_createdBy", fieldValue, true, "");
                    tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "Created By", FieldHelp, FieldRequired, false, ""));
                }
                //
                // ----- Created Date
                {
                    string FieldHelp = "The date and time when this record was originally created.";
                    string fieldValue = "";
                    if (adminData.editRecord == null) {
                        fieldValue = "(not set)";
                    } else if (adminData.editRecord.id == 0) {
                        fieldValue = "(available after save)";
                    } else {
                        if (GenericController.encodeDateMinValue(adminData.editRecord.dateAdded) == DateTime.MinValue) {
                            fieldValue = "(not set)";
                        } else {
                            fieldValue = adminData.editRecord.dateAdded.ToString();
                        }
                    }
                    string fieldEditor = AdminUIEditorController.getTextEditor(core, "ignore_createdDate", fieldValue, true, "");
                    tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "Created Date", FieldHelp, FieldRequired, false, ""));
                }
                //
                // ----- Modified By
                {
                    string FieldHelp = "The people account of the last user who modified this record.";
                    string fieldValue = "";
                    if (adminData.editRecord == null) {
                        fieldValue = "(not set)";
                    } else if (adminData.editRecord.id == 0) {
                        fieldValue = "(available after save)";
                    } else if (adminData.editRecord.modifiedBy == null) {
                        fieldValue = "(not set)";
                    } else {
                        int FieldValueInteger = adminData.editRecord.modifiedBy.id;
                        if (FieldValueInteger == 0) {
                            fieldValue = "(not set)";
                        } else {
                            using (var csData = new CsModel(core)) {
                                csData.open("people", "(id=" + FieldValueInteger + ")", "name,active", false);
                                if (!csData.ok()) {
                                    fieldValue = "#" + FieldValueInteger + ", (deleted)";
                                } else {
                                    fieldValue = "#" + FieldValueInteger + ", " + csData.getText("name");
                                    if (!csData.getBoolean("active")) {
                                        fieldValue += " (inactive)";
                                    }
                                }
                                csData.close();
                            }
                        }
                    }
                    string fieldEditor = AdminUIEditorController.getTextEditor(core, "ignore_modifiedBy", fieldValue, true, "");
                    tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "Modified By", FieldHelp, FieldRequired, false, ""));
                }
                //
                // ----- Modified Date
                {
                    string FieldHelp = "The date and time when this record was last modified.";
                    string fieldValue = "";
                    if (adminData.editRecord == null) {
                        fieldValue = "(not set)";
                    } else if (adminData.editRecord.id == 0) {
                        fieldValue = "(available after save)";
                    } else {
                        if (GenericController.encodeDateMinValue(adminData.editRecord.modifiedDate) == DateTime.MinValue) {
                            fieldValue = "(not set)";
                        } else {
                            fieldValue = adminData.editRecord.modifiedDate.ToString();
                        }
                    }
                    string fieldEditor = AdminUIEditorController.getTextEditor(core, "ignore_modifiedBy", fieldValue, true, "");
                    tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "Modified Date", FieldHelp, false, false, ""));
                }
                string s = AdminUIController.editTable(tabPanel.text);
                result = AdminUIController.getEditPanel(core, true, "Control Information", "", s);
                adminData.editSectionPanelCount += 1;
                tabPanel = null;
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                string s = AdminUIController.editTable(tabPanel.text);
                result = AdminUIController.getEditPanel(core, true, "Control Information", "", $"<div>There was an error reading the data for this page. [{ex}]</div>{s}");
                adminData.editSectionPanelCount += 1;
                return result;
            }
        }
    }
}
