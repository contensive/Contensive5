//
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Controllers.GenericController;
using Contensive.BaseClasses;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
using System;
using System.Globalization;
using Contensive.Processor.Addons.AdminSite.Models;

namespace Contensive.Processor.Addons.AdminSite {
    public static class EditView {
        //
        // ====================================================================================================
        /// <summary>
        /// Create the tabs for editing a record
        /// </summary>
        /// <param name="adminData.content"></param>
        /// <param name="editRecord"></param>
        /// <returns></returns>
        public static string get(CoreController core, AdminDataModel adminData) {
            try {
                //
                if ((!core.doc.userErrorList.Count.Equals(0)) && adminData.editRecord.loaded) {
                    //
                    // block load if there was a user error and it is already loaded (assume error was from response )
                } else if (adminData.adminContent.id <= 0) {
                    //
                    // Invalid Content
                    Processor.Controllers.ErrorController.addUserError(core, "There was a problem identifying the content you requested. Please return to the previous form and verify your selection.");
                    return "";
                } else if (adminData.editRecord.loaded && !adminData.editRecord.saved) {
                    //
                    //   File types need to be reloaded from the Db, because...
                    //       LoadDb - sets them to the path-page
                    //       LoadResponse - sets the blank if no change, filename if there is an upload
                    //       SaveEditRecord - if blank, no change. If a filename it saves the uploaded file
                    //       GetForm_Edit - expects the Db value to be in EditRecordValueVariants (path-page)
                    //
                    // xx This was added to bypass the load for the editrefresh case (reload the response so the editor preference can change)
                    // xx  I do not know why the following section says "reload even if it is loaded", but lets try this
                    //
                    foreach (var keyValuePair in adminData.adminContent.fields) {
                        ContentFieldMetadataModel field = keyValuePair.Value;
                        if ((keyValuePair.Value.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.File) || (keyValuePair.Value.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.FileImage)) {
                            adminData.editRecord.fieldsLc[field.nameLc].value_content = adminData.editRecord.fieldsLc[field.nameLc].value_storedInDb;
                        }
                    }
                } else {
                    //
                    // otherwise, load the record, even if it was loaded during a previous form process
                    EditRecordModel.loadEditRecord(core, true,  adminData);
                }
                if (!AdminDataModel.userHasContentAccess(core, ((adminData.editRecord.contentControlId.Equals(0)) ? adminData.adminContent.id : adminData.editRecord.contentControlId))) {
                    Processor.Controllers.ErrorController.addUserError(core, "Your account on this system does not have access rights to edit this content.");
                    return "";
                }
                //
                // Setup Edit Referer
                string EditReferer = core.docProperties.getText(RequestNameEditReferer);
                if (string.IsNullOrEmpty(EditReferer)) {
                    EditReferer = core.webServer.requestReferer;
                    if (!string.IsNullOrEmpty(EditReferer)) {
                        //
                        // special case - if you are coming from the advanced search, go back to the list page
                        EditReferer = GenericController.strReplace(EditReferer, "&af=39", "");
                        //
                        // if referer includes AdminWarningMsg (admin hint message), remove it -- this edit may fix the problem
                        int Pos = EditReferer.IndexOf("AdminWarningMsg=", StringComparison.CurrentCulture);
                        if (Pos >= 0) {
                            EditReferer = EditReferer.left(Pos - 2);
                        }
                    }
                }
                core.doc.addRefreshQueryString(RequestNameEditReferer, EditReferer);
                //
                // load user's editor preferences to fieldEditorPreferences() - this is the editor this user has picked when there are >1
                //   fieldId:addonId,fieldId:addonId,etc
                //   with custom FancyBox form in edit window with button "set editor preference"
                //   this button causes a 'refresh' action, reloads fields with stream without save
                //
                //
                // ----- determine contentType for editor
                //
                CPHtml5BaseClass.EditorContentType contentType;
                if (GenericController.toLCase(adminData.adminContent.name) == "email templates") {
                    contentType = CPHtml5BaseClass.EditorContentType.contentTypeEmailTemplate;
                } else if (GenericController.toLCase(adminData.adminContent.tableName) == "cctemplates") {
                    contentType = CPHtml5BaseClass.EditorContentType.contentTypeWebTemplate;
                } else if (GenericController.toLCase(adminData.adminContent.tableName) == "ccemail") {
                    contentType = CPHtml5BaseClass.EditorContentType.contentTypeEmail;
                } else {
                    contentType = CPHtml5BaseClass.EditorContentType.contentTypeWeb;
                }

                EditorEnvironmentModel editorEnv = new() {
                    allowHelpMsgCustom = false,
                    editorAddonListJSON = core.html.getWysiwygAddonList(contentType),
                    isRootPage = adminData.adminContent.tableName.ToLowerInvariant().Equals(PageContentModel.tableMetadata.tableNameLower) && (adminData.editRecord.parentId == 0) && (adminData.editRecord.id != 0),
                    needUniqueEmailMessage = false,
                    record_readOnly = adminData.editRecord.userReadOnly,
                    styleList = "",
                    styleOptionList = "",
                    formFieldList = ""
                };
                //
                // ----- determine access details
                var userContentPermissions = PermissionController.getUserContentPermissions(core, adminData.adminContent);
                bool allowDelete = adminData.adminContent.allowDelete && userContentPermissions.allowDelete && (adminData.editRecord.id != 0);
                bool allowAdd = adminData.adminContent.allowAdd && userContentPermissions.allowAdd;
                var editButtonBarInfo = new EditButtonBarInfoClass(core, adminData, allowDelete, true, userContentPermissions.allowSave, allowAdd);
                //
                string adminContentTableNameLc = adminData.adminContent.tableName.ToLowerInvariant();
                bool allowLinkAlias = adminContentTableNameLc.Equals(PageContentModel.tableMetadata.tableNameLower);
                bool allowPeopleGroups = adminContentTableNameLc.Equals(PersonModel.tableMetadata.tableNameLower); ;
                //
                // -- determine buttons
                // -- these customizations should be included in the content definition
                switch (adminContentTableNameLc) {
                    case "ccsystemtextmessages": {
                            //
                            // System SMS
                            bool submitted = false;
                            bool sent = false;
                            if (adminData.editRecord.id != 0) {
                                if (encodeInteger(adminData.editRecord.fieldsLc["testmemberid"].value_content) == 0) {
                                    adminData.editRecord.fieldsLc["testmemberid"].value_content = core.session.user.id;
                                }
                            }
                            editButtonBarInfo.allowSave = userContentPermissions.allowSave && adminData.editRecord.allowUserSave && (!submitted) && (!sent);
                            editButtonBarInfo.allowSendTest = (!submitted) && (!sent);
                            break;
                        }
                    case "ccgrouptextmessages": {
                            //
                            // System SMS
                            bool submitted = false;
                            bool sent = false;
                            DateTime LastSendTestDate = DateTime.MinValue;
                            bool allowSendWithoutTest = core.siteProperties.getBoolean("AllowEmailSendWithoutTest", false);
                            if (adminData.editRecord.id != 0) {
                                submitted = encodeBoolean(adminData.editRecord.fieldsLc["submitted"].value_content);
                                sent = encodeBoolean(adminData.editRecord.fieldsLc["sent"].value_content);
                                LastSendTestDate = encodeDate(adminData.editRecord.fieldsLc["lastsendtestdate"].value_content);
                            }
                            editButtonBarInfo.allowSave = !submitted && (userContentPermissions.allowSave && adminData.editRecord.allowUserSave);
                            editButtonBarInfo.allowSend = !submitted && ((LastSendTestDate != DateTime.MinValue) || allowSendWithoutTest);
                            editButtonBarInfo.allowSendTest = !submitted;
                            editorEnv.record_readOnly = adminData.editRecord.userReadOnly || submitted || sent;
                            break;
                        }
                    case "ccemail": {
                            //
                            // -- email
                            bool emailSubmitted = false;
                            bool emailSent = false;
                            DateTime LastSendTestDate = DateTime.MinValue;
                            bool AllowEmailSendWithoutTest = (core.siteProperties.getBoolean("AllowEmailSendWithoutTest", false));
                            if (adminData.editRecord.fieldsLc.ContainsKey("lastsendtestdate")) {
                                LastSendTestDate = GenericController.encodeDate(adminData.editRecord.fieldsLc["lastsendtestdate"].value_content);
                            }
                            if (adminData.adminContent.id.Equals(ContentMetadataModel.getContentId(core, "System Email"))) {
                                //
                                // System Email
                                emailSubmitted = false;
                                if (adminData.editRecord.id != 0) {
                                    if (adminData.editRecord.fieldsLc.ContainsKey("testmemberid")) {
                                        if (encodeInteger(adminData.editRecord.fieldsLc["testmemberid"].value_content) == 0) {
                                            adminData.editRecord.fieldsLc["testmemberid"].value_content = core.session.user.id;
                                        }
                                    }
                                }
                                editButtonBarInfo.allowSave = (userContentPermissions.allowSave && adminData.editRecord.allowUserSave && (!emailSubmitted) && (!emailSent));
                                editButtonBarInfo.allowSendTest = ((!emailSubmitted) && (!emailSent));
                            } else if (adminData.adminContent.id.Equals(ContentMetadataModel.getContentId(core, "Conditional Email"))) {
                                //
                                // Conditional Email
                                emailSubmitted = false;
                                editorEnv.record_readOnly = adminData.editRecord.userReadOnly || emailSubmitted;
                                if (adminData.editRecord.id != 0) {
                                    if (adminData.editRecord.fieldsLc.ContainsKey("submitted")) { emailSubmitted = GenericController.encodeBoolean(adminData.editRecord.fieldsLc["submitted"].value_content); }
                                }
                                editButtonBarInfo.allowActivate = !emailSubmitted && ((LastSendTestDate != DateTime.MinValue) || AllowEmailSendWithoutTest);
                                editButtonBarInfo.allowDeactivate = emailSubmitted;
                                editButtonBarInfo.allowSave = userContentPermissions.allowSave && adminData.editRecord.allowUserSave && !emailSubmitted;
                                editButtonBarInfo.allowSendTest = !emailSubmitted;
                            } else {
                                //
                                // Group Email
                                if (adminData.editRecord.id != 0) {
                                    emailSubmitted = encodeBoolean(adminData.editRecord.fieldsLc["submitted"].value_content);
                                    emailSent = encodeBoolean(adminData.editRecord.fieldsLc["sent"].value_content);
                                }
                                editButtonBarInfo.allowSave = !emailSubmitted && (userContentPermissions.allowSave && adminData.editRecord.allowUserSave);
                                editButtonBarInfo.allowSend = !emailSubmitted && ((LastSendTestDate != DateTime.MinValue) || AllowEmailSendWithoutTest);
                                editButtonBarInfo.allowSendTest = !emailSubmitted;
                                editorEnv.record_readOnly = adminData.editRecord.userReadOnly || emailSubmitted || emailSent;
                            }
                            break;
                        }
                    case "ccpagecontent": {
                            //
                            // -- Page Content
                            editButtonBarInfo.allowMarkReviewed = true;
                            editButtonBarInfo.isPageContent = true;
                            editButtonBarInfo.hasChildRecords = true;
                            allowLinkAlias = true;
                            break;
                        }
                    default: {
                            //
                            // All other tables (User definined)
                            var pageContentMetadata = ContentMetadataModel.createByUniqueName(core, "page content");
                            editButtonBarInfo.isPageContent = pageContentMetadata.isParentOf(core, adminData.adminContent.id);
                            editButtonBarInfo.hasChildRecords = adminData.adminContent.containsField(core, "parentid");
                            editButtonBarInfo.allowMarkReviewed = core.db.isSQLTableField(adminData.adminContent.tableName, "DateReviewed");
                            break;
                        }
                }
                //
                // Print common form elements
                var Stream = new StringBuilderLegacyController();
                Stream.add("\r<input type=\"hidden\" name=\"fieldEditorPreference\" id=\"fieldEditorPreference\" value=\"\">");
                string editSectionButtonBar = AdminUIController.getSectionButtonBarForEdit(core, editButtonBarInfo);
                Stream.add(editSectionButtonBar);
                var headerInfo = new RecordEditHeaderInfoClass {
                    recordId = adminData.editRecord.id,
                    recordLockById = adminData.editRecord.editLock.editLockByMemberId,
                    recordLockExpiresDate = encodeDate(adminData.editRecord.editLock.editLockExpiresDate),
                    recordName = adminData.editRecord.nameLc
                };
                string titleBarDetails = AdminUIController.getEditForm_TitleBarDetails(core, adminData, headerInfo);
                Stream.add(AdminUIController.getSectionHeader(core, "", titleBarDetails));
                {
                    var editTabs = new EditTabModel();
                    EditViewTabList.addContentTabs(core, adminData, editTabs, editorEnv);
                    if (allowPeopleGroups) {
                        EditViewTabList.addCustomTab(core, editTabs, "Groups", GroupRuleEditor.get(core, adminData));
                    }
                    if (allowLinkAlias) {
                        EditViewTabList.addCustomTab(core, editTabs, "Link Aliases", LinkAliasEditor.getForm_Edit_LinkAliases(core, adminData, adminData.editRecord.userReadOnly));
                    }
                    EditViewTabList.addCustomTab(core, editTabs, "Control&nbsp;Info", EditViewTabControlInfo.get(core, adminData, editorEnv));
                    Stream.add(editTabs.getTabs(core));
                }
                Stream.add(editSectionButtonBar);
                Stream.add(HtmlController.inputHidden("FormFieldList", editorEnv.formFieldList));
                //
                // -- update page title
                if (adminData.editRecord.id == 0) {
                    core.html.addTitle("Add " + adminData.adminContent.name, "admin edit view");
                } else if (string.IsNullOrEmpty(adminData.editRecord.nameLc)) {
                    core.html.addTitle("Edit #" + adminData.editRecord.id + " in " + adminData.editRecord.contentControlId_Name, "admin edit view");
                } else {
                    core.html.addTitle("Edit " + adminData.editRecord.nameLc + " in " + adminData.editRecord.contentControlId_Name, "admin edit view");
                }
                return wrapForm(core, Stream.text, adminData, AdminFormEdit);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        //
        private static string wrapForm(CoreController core, string innerHtml, AdminDataModel adminData, int AdminFormID) {
            try {
                core.html.addScriptCode("var docLoaded=false", "Form loader");
                core.html.addScriptCode_onLoad("docLoaded=true;", "Form loader");
                string result = Environment.NewLine + "<!-- block --><div class=\"d-none\"><input type=password name=\"password_block\" value=\"\"><input type=text name=\"username_block\" value=\"\"></div><!-- end block -->";
                result += Environment.NewLine + "<input TYPE=\"hidden\" NAME=\"" + rnAdminSourceForm + "\" VALUE=\"" + AdminFormID + "\">";
                result += Environment.NewLine + "<input TYPE=\"hidden\" NAME=\"" + RequestNameTitleExtension + "\" VALUE=\"" + adminData.editViewTitleSuffix + "\">";
                result += Environment.NewLine + "<input TYPE=\"hidden\" NAME=\"FormEmptyFieldList\" ID=\"FormEmptyFieldList\" VALUE=\",\">";
                result += innerHtml;
                return HtmlController.form(core, result, new CPBase.BaseModels.HtmlAttributesForm {
                    onsubmit = "cj.admin.saveEmptyFieldList('FormEmptyFieldList')",
                    autocomplete = false
                });
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //


    }
}
