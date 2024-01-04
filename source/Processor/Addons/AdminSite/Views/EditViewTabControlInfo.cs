
using System;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Contensive.Exceptions;
using System.Net;
using Contensive.Models.Db;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.AdminSite {
    public class EditViewTabControlInfo {
        //
        //========================================================================
        /// <summary>
        /// Control edit tab
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <returns></returns>
        public static string get(CoreController core, AdminDataModel adminData, EditorEnvironmentModel editorEnv) {
            // -- used in expection handling
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
                        LogController.logError(core, new GenericException("No content definition was specified for this page"));
                        return HtmlController.p("No content was specified.");
                    } else {
                        //
                        // Content Definition was not specified
                        LogController.logError(core, new GenericException("The content definition specified for this page [" + adminData.adminContent.id + "] was not found"));
                        return HtmlController.p("No content was specified.");
                    }
                }
                //
                // ----- Authoring status
                bool FieldRequired = false;


                List<string> TabsFound = new List<string>();
                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
                    if ((field.editTabName.ToLowerInvariant().Equals("control info")) && (field.authorable) && (field.active)) {
                        tabPanel.add(EditorRowClass.getEditorRow(core, field, adminData, editorEnv));
                    }
                }
                //
                // ----- RecordID
                {
                    string fieldValue = (adminData.editRecord.id == 0) ? "(available after save)" : adminData.editRecord.id.ToString();
                    string fieldEditor = AdminUIEditorController.getTextEditor(core, "ignore", fieldValue, true, "");
                    string fieldHelp = "This is the unique number that identifies this record within this content.";
                    tabPanel.add(AdminUIController.getEditRow(core, fieldEditor, "Record Number", fieldHelp, true, false, ""));
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
                // ----- EID (Encoded ID)
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
                        if (!core.session.isAuthenticatedAdmin()) {
                            HTMLFieldString = HTMLFieldString + HtmlController.inputHidden("contentControlId", FieldValueInteger);
                        } else {
                            string RecordContentName = adminData.editRecord.contentControlId_Name;
                            string TableName2 = MetadataController.getContentTablename(core, RecordContentName);
                            int TableId = MetadataController.getRecordIdByUniqueName(core, "Tables", TableName2);
                            //
                            // Test for parentid
                            int ParentId = 0;
                            bool ContentSupportsParentId = false;
                            if (adminData.editRecord.id > 0) {
                                using (var csData = new CsModel(core)) {
                                    if (csData.openRecord(RecordContentName, adminData.editRecord.id)) {
                                        ContentSupportsParentId = csData.isFieldSupported("ParentID");
                                        if (ContentSupportsParentId) {
                                            ParentId = csData.getInteger("ParentID");
                                        }
                                    }
                                }
                            }
                            bool IsEmptyList = false;
                            if (core.session.isAuthenticatedAdmin()) {
                                //
                                // administrator, and either ( no parentid or does not support it), let them select any content compatible with the table
                                string sqlFilter = "(ContentTableID=" + TableId + ")";
                                int contentCId = MetadataController.getRecordIdByUniqueName(core, ContentModel.tableMetadata.contentName, ContentModel.tableMetadata.contentName);
                                HTMLFieldString += AdminUIEditorController.getLookupContentEditor(core, "contentcontrolid", FieldValueInteger, contentCId, ref IsEmptyList, adminData.editRecord.userReadOnly, "", "", true, sqlFilter);
                                FieldHelp = FieldHelp + " (Only administrators have access to this control. Changing the Controlling Content allows you to change who can author the record, as well as how it is edited.)";
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(HTMLFieldString)) {
                        HTMLFieldString = adminData.editRecord.contentControlId_Name;
                    }
                    tabPanel.add(AdminUIController.getEditRow(core, HTMLFieldString, "Controlling Content", FieldHelp, FieldRequired, false, ""));
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
                LogController.logError(core, ex);
                string s = AdminUIController.editTable(tabPanel.text);
                result = AdminUIController.getEditPanel(core, true, "Control Information", "", $"<div>There was an error reading the data for this page. [{ex}]</div>{s}");
                adminData.editSectionPanelCount += 1;
                return result;
            }
        }
    }
}
