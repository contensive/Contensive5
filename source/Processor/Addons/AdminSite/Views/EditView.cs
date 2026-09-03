//
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.AdminSite.Models;
using Contensive.Processor.Controllers;
using Contensive.Processor.Controllers.EditControls;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Addons.AdminSite {
    public static class EditView {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
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
                // -- Setup Edit Referer Stack
                // -- The stack tracks return URLs so OK/Cancel navigates back through edit levels.
                // -- On first entry (no stack in request), capture from HTTP Referer header.
                // -- On form posts (stack already in request), preserve the existing stack.
                string editRefererStackEncoded = core.docProperties.getText(RequestNameEditRefererStack);
                string editReferer;
                if (!string.IsNullOrEmpty(editRefererStackEncoded)) {
                    //
                    // -- stack was passed explicitly (form post or carried from parent), preserve it
                    editReferer = Controllers.EditRefererStackController.peek(editRefererStackEncoded);
                } else {
                    //
                    // -- no stack in request, capture from legacy EditReferer or HTTP Referer header
                    editReferer = core.docProperties.getText(RequestNameEditReferer);
                    if (string.IsNullOrEmpty(editReferer)) {
                        editReferer = core.webServer.requestReferer;
                    }
                    if (!string.IsNullOrEmpty(editReferer)) {
                        //
                        // -- if referer includes AdminWarningMsg, remove it -- this edit may fix the problem
                        int pos = editReferer.IndexOf("AdminWarningMsg=", StringComparison.CurrentCulture);
                        if (pos >= 0) {
                            editReferer = editReferer.left(pos - 2);
                        }
                    }
                    editRefererStackEncoded = Controllers.EditRefererStackController.push("", editReferer);
                }
                core.doc.addRefreshQueryString(RequestNameEditRefererStack, editRefererStackEncoded);
                //
                // -- backward compat: also set legacy EditReferer
                core.doc.addRefreshQueryString(RequestNameEditReferer, editReferer);
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
                    isContentRootPage = adminData.adminContent.tableName.ToLowerInvariant().Equals(PageContentModel.tableMetadata.tableNameLower) && (adminData.editRecord.parentId == 0) && (adminData.editRecord.id != 0),
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
                                submitted = getBoolean(adminData.editRecord.fieldsLc["submitted"].value_content);
                                sent = getBoolean(adminData.editRecord.fieldsLc["sent"].value_content);
                                LastSendTestDate = getDate(adminData.editRecord.fieldsLc["lastsendtestdate"].value_content);
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
                                LastSendTestDate = GenericController.getDate(adminData.editRecord.fieldsLc["lastsendtestdate"].value_content);
                            }
                            if (adminData.adminContent.id.Equals(ContentMetadataModel.getContentId(core, "System Email"))) {
                                //
                                // System Email
                                emailSubmitted = false;
                                editButtonBarInfo.allowSave = (userContentPermissions.allowSave && adminData.editRecord.allowUserSave && (!emailSubmitted) && (!emailSent));
                                editButtonBarInfo.allowSendTest = ((!emailSubmitted) && (!emailSent));
                            } else if (adminData.adminContent.id.Equals(ContentMetadataModel.getContentId(core, "Conditional Email"))) {
                                //
                                // Conditional Email
                                emailSubmitted = false;
                                editorEnv.record_readOnly = adminData.editRecord.userReadOnly || emailSubmitted;
                                if (adminData.editRecord.id != 0) {
                                    if (adminData.editRecord.fieldsLc.ContainsKey("submitted")) { emailSubmitted = GenericController.getBoolean(adminData.editRecord.fieldsLc["submitted"].value_content); }
                                }
                                editButtonBarInfo.allowActivate = !emailSubmitted && ((LastSendTestDate != DateTime.MinValue) || AllowEmailSendWithoutTest);
                                editButtonBarInfo.allowDeactivate = emailSubmitted;
                                editButtonBarInfo.allowSave = userContentPermissions.allowSave && adminData.editRecord.allowUserSave && !emailSubmitted;
                                editButtonBarInfo.allowSendTest = !emailSubmitted;
                            } else {
                                //
                                // Group Email
                                if (adminData.editRecord.id != 0) {
                                    emailSubmitted = getBoolean(adminData.editRecord.fieldsLc["submitted"].value_content);
                                    emailSent = getBoolean(adminData.editRecord.fieldsLc["sent"].value_content);
                                }
                                editButtonBarInfo.allowSave = !emailSubmitted && (userContentPermissions.allowSave && adminData.editRecord.allowUserSave);
                                editButtonBarInfo.allowSend = !emailSubmitted && ((LastSendTestDate != DateTime.MinValue) || AllowEmailSendWithoutTest);
                                editButtonBarInfo.allowSendTest = !emailSubmitted;
                                editorEnv.record_readOnly = adminData.editRecord.userReadOnly || emailSubmitted || emailSent;
                            }
                            break;
                        }
                    case "ccmembers": {
                            //
                            // -- People / Members: show Create Bearer Token button for existing records
                            if (adminData.editRecord.id != 0) {
                                editButtonBarInfo.allowCreateBearerToken = true;
                                editButtonBarInfo.peopleRecordId = adminData.editRecord.id;
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
                    recordLockExpiresDate = getDate(adminData.editRecord.editLock.editLockExpiresDate),
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
                        EditViewTabList.addCustomTab(core, editTabs, "Page Urls", LinkAliasEditor.getForm_Edit_PageUrls(core, adminData, adminData.editRecord.userReadOnly));
                    }
                    EditViewTabList.addCustomTab(core, editTabs, "Control&nbsp;Info", EditViewTabControlInfo.get(core, adminData, editorEnv));
                    Stream.add(editTabs.getTabs(core));
                }
                Stream.add(editSectionButtonBar);
                Stream.add(HtmlController.inputHidden("FormFieldList", editorEnv.formFieldList));
                //
                // -- People record: inject JS for the Create Bearer Token button
                if (editButtonBarInfo.allowCreateBearerToken) {
                    core.html.addScriptCode(@"
function contensiveCreateBearerToken(userId) {
    fetch('/createBearerToken?userId=' + userId, { method: 'POST' })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (data.success) {
                var overlay = document.createElement('div');
                overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.5);z-index:10000;display:flex;align-items:center;justify-content:center;';
                var box = document.createElement('div');
                box.style.cssText = 'background:#fff;border-radius:8px;padding:24px;max-width:600px;width:90%;box-shadow:0 4px 20px rgba(0,0,0,0.3);font-family:sans-serif;';
                box.innerHTML = '<h3 style=""margin:0 0 8px"">Bearer Token Created</h3>'
                    + '<p style=""margin:0 0 12px;color:#666"">Expires ' + data.expires + '. Use as: Authorization: Bearer &lt;token&gt;</p>'
                    + '<div style=""display:flex;gap:8px;align-items:center;"">'
                    + '<input id=""ccBearerTokenValue"" type=""text"" readonly value=""' + data.token + '"" style=""flex:1;padding:8px;font-family:monospace;font-size:13px;border:1px solid #ccc;border-radius:4px;"">'
                    + '<button id=""ccBearerTokenCopy"" style=""padding:8px 16px;background:#0d6efd;color:#fff;border:none;border-radius:4px;cursor:pointer;white-space:nowrap;"">Copy</button>'
                    + '</div>'
                    + '<div style=""text-align:right;margin-top:16px;"">'
                    + '<button id=""ccBearerTokenClose"" style=""padding:6px 20px;background:#6c757d;color:#fff;border:none;border-radius:4px;cursor:pointer;"">Close</button>'
                    + '</div>';
                overlay.appendChild(box);
                document.body.appendChild(overlay);
                var tokenInput = document.getElementById('ccBearerTokenValue');
                var copyBtn = document.getElementById('ccBearerTokenCopy');
                var closeBtn = document.getElementById('ccBearerTokenClose');
                copyBtn.addEventListener('click', function() {
                    tokenInput.select();
                    navigator.clipboard.writeText(tokenInput.value).then(function() {
                        copyBtn.textContent = 'Copied!';
                        copyBtn.style.background = '#198754';
                        setTimeout(function() { copyBtn.textContent = 'Copy'; copyBtn.style.background = '#0d6efd'; }, 2000);
                    });
                });
                closeBtn.addEventListener('click', function() { document.body.removeChild(overlay); });
                overlay.addEventListener('click', function(e) { if (e.target === overlay) { document.body.removeChild(overlay); } });
            } else {
                alert('Error creating bearer token: ' + data.message);
            }
        })
        .catch(function(err) { alert('Request failed: ' + err); });
}", "createBearerToken");
                }
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
                logger.Error(ex, $"{core.logCommonMessage}");
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
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //


    }
}
