
using Contensive.BaseClasses;
using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Models.Domain;
using System;
using System.Linq;

namespace Contensive.Processor.Addons.AdminSite {
    public static class EditorRowClass {
        public static string getEditorRow(CoreController core, ContentFieldMetadataModel field, AdminDataModel adminData, EditorEnvironmentModel editorEnv) {
            int hint = 0;
            string hintField = field.nameLc;
            try {
                //
                // -- argument check
                Models.EditRecordModel editRecord = adminData.editRecord;
                if (!editRecord.fieldsLc.ContainsKey(field.nameLc)) {
                    //
                    // -- field not found in edit record. Report and exit with blank editor
                    LogController.logError(core, new GenericException("getEditorRow, field [" + field.nameLc + "] not found in editRecord collection for content [" + adminData.adminContent.name + "]"));
                    return "<!-- no editor row available because field not found -->";
                }
                //
                object fieldValueObject = editRecord.fieldsLc[field.nameLc].value;
                string fieldValue_text = encodeText(fieldValueObject);
                hint = 10;
                //
                string whyReadOnlyMsg = "";
                int fieldRows = 1;
                string fieldHtmlId = field.nameLc + field.id.ToString();
                string fieldCaption = field.caption;
                if (field.uniqueName) {
                    hint = 20;
                    //
                    fieldCaption = "&nbsp;**" + fieldCaption;
                } else {
                    hint = 30;
                    //
                    if (field.nameLc.ToLowerInvariant() == "email") {
                        if ((adminData.adminContent.tableName.ToLowerInvariant() == "ccmembers") && ((core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin, false)))) {
                            fieldCaption = "&nbsp;***" + fieldCaption;
                            editorEnv.needUniqueEmailMessage = true;
                        }
                    }
                }
                hint = 40;
                //
                if (field.required) {
                    fieldCaption = "&nbsp;*" + fieldCaption;
                }
                adminData.formInputCount += 1;
                bool fieldForceReadOnly = false;
                hint = 100;
                //
                // Read only Special Cases
                if (editorEnv.isRootPage) {
                    //
                    // -- page content metadata, these are the special fields
                    switch (GenericController.toLCase(field.nameLc)) {
                        case "active": {
                                //
                                // if active, it is read only -- if inactive, let them set it active.
                                fieldForceReadOnly = encodeBoolean(fieldValueObject);
                                if (fieldForceReadOnly) {
                                    whyReadOnlyMsg = "&nbsp;(disabled because you can not mark the landing page inactive)";
                                }
                                break;
                            }
                        case "dateexpires":
                        case "pubdate":
                        case "datearchive":
                        case "blocksection":
                        case "archiveparentid":
                        case "hidemenu": {
                                //
                                // These fields are read only on landing pages
                                fieldForceReadOnly = true;
                                whyReadOnlyMsg = "&nbsp;(disabled for the landing page)";
                                break;
                            }
                        case "allowinmenus":
                        case "allowinchildlists": {
                                fieldValueObject = "1";
                                fieldForceReadOnly = true;
                                whyReadOnlyMsg = "&nbsp;(disabled for root pages)";
                            }
                            break;
                        default: {
                                // do nothing
                                break;
                            }
                    }
                }
                hint = 200;
                //
                // Special Case - ccemail table Alloweid should be disabled if siteproperty AllowLinkLogin is false
                //
                if (GenericController.toLCase(adminData.adminContent.tableName) == "ccemail" && GenericController.toLCase(field.nameLc) == "allowlinkeid") {
                    if (!(core.siteProperties.getBoolean("AllowLinkLogin", true))) {
                        fieldValueObject = "0";
                        fieldForceReadOnly = true;
                        fieldValue_text = "0";
                    }
                }
                hint = 300;
                //
                // -- determine custom editor addon
                AddonModel editorAddon = null;
                if (!string.IsNullOrEmpty(field.editorAddonGuid)) {
                    //
                    // -- set editor from field
                    editorAddon = core.cacheStore.addonCache.create(field.editorAddonGuid);
                }
                var fieldEditor = adminData.fieldTypeEditors.Find(x => (x.fieldTypeId == (int)field.fieldTypeId));
                if (fieldEditor != null) {
                    //
                    // -- set editor from field type
                    int fieldTypeDefaultEditorAddonId = (int)fieldEditor.editorAddonId;
                    editorAddon = core.cacheStore.addonCache.create(fieldTypeDefaultEditorAddonId);
                }
                //
                // -- create editor: custom, read-only, editable
                bool useEditorAddon = false;
                string EditorString = "";
                bool editorReadOnly = (editorEnv.record_readOnly || field.readOnly || (editRecord.id != 0 && field.notEditable) || (fieldForceReadOnly));
                if (editorAddon != null) {
                    hint = 400;
                    //
                    //--------------------------------------------------------------------------------------------
                    // ----- Custom Editor
                    //--------------------------------------------------------------------------------------------
                    //
                    core.docProperties.setProperty("editorRecordId", editRecord.id);
                    core.docProperties.setProperty("editorName", field.nameLc);
                    core.docProperties.setProperty("editorValue", fieldValue_text);
                    core.docProperties.setProperty("editorFieldId", field.id);
                    core.docProperties.setProperty("editorFieldType", (int)field.fieldTypeId);
                    core.docProperties.setProperty("editorReadOnly", editorReadOnly);
                    core.docProperties.setProperty("editorWidth", "");
                    core.docProperties.setProperty("editorHeight", "");
                    if (field.fieldTypeId.isOneOf(CPContentBaseClass.FieldTypeIdEnum.HTML, CPContentBaseClass.FieldTypeIdEnum.HTMLCode, CPContentBaseClass.FieldTypeIdEnum.FileHTML, CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode)) {
                        //
                        // include html related arguments
                        core.docProperties.setProperty("editorAllowActiveContent", "1");
                        core.docProperties.setProperty("editorAddonList", editorEnv.editorAddonListJSON);
                        core.docProperties.setProperty("editorStyles", editorEnv.styleList);
                        core.docProperties.setProperty("editorStyleOptions", editorEnv.styleOptionList);
                    }
                    EditorString = core.addon.execute(editorAddon, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                        addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextEditor,
                        errorContextMessage = "field editor id:" + editorAddon.id
                    });
                    useEditorAddon = !string.IsNullOrEmpty(EditorString);
                    hint = 420;
                    if (useEditorAddon) {
                        //
                        // -- editor worked
                        editorEnv.formFieldList += "," + field.nameLc;
                    } else {
                        //
                        // -- editor failed, determine if it is missing (or inactive). If missing, remove it from the members preferences
                        using var csData = new CsModel(core);
                        if (!csData.openSql("select id from ccaggregatefunctions where id=" + editorAddon.id)) {
                            //
                            // -- missing, not just inactive
                            EditorString = "";
                            //
                            // load user's editor preferences to fieldEditorPreferences() - this is the editor this user has picked when there are >1
                            //   fieldId:addonId,fieldId:addonId,etc
                            //   with custom FancyBox form in edit window with button "set editor preference"
                            //   this button causes a 'refresh' action, reloads fields with stream without save
                            //
                            string tmpList = core.userProperty.getText("editorPreferencesForContent:" + adminData.adminContent.id, "");
                            int PosStart = GenericController.strInstr(1, "," + tmpList, "," + field.id + ":");
                            if (PosStart > 0) {
                                int PosEnd = GenericController.strInstr(PosStart + 1, "," + tmpList, ",");
                                if (PosEnd == 0) {
                                    tmpList = tmpList.left(PosStart - 1);
                                } else {
                                    tmpList = tmpList.left(PosStart - 1) + tmpList.Substring(PosEnd - 1);
                                }
                                core.userProperty.setProperty("editorPreferencesForContent:" + adminData.adminContent.id, tmpList);
                            }
                        }
                    }
                }
                hint = 500;
                //
                // -- style for editor wrapper used to limit the width of some editors like integer
                string editorWrapperSyle = "";
                if (!useEditorAddon) {
                    hint = 510;
                    bool IsEmptyList = false;
                    //
                    // if custom editor not used or if it failed
                    //
                    if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect) {
                        hint = 520;
                        //
                        // ----- Default Editor, Redirect fields (the same for normal/readonly/spelling)

                        EditorString = AdminUIEditorController.getRedirectEditor(core, field, adminData, editRecord, fieldValue_text, editorReadOnly, fieldHtmlId, field.required);
                    } else if (editorReadOnly) {
                        hint = 530;
                        //
                        //--------------------------------------------------------------------------------------------
                        // ----- Display fields as read only
                        //--------------------------------------------------------------------------------------------
                        //
                        if (!string.IsNullOrEmpty(whyReadOnlyMsg)) {
                            whyReadOnlyMsg = "<span class=\"ccDisabledReason\">" + whyReadOnlyMsg + "</span>";
                        }
                        switch (field.fieldTypeId) {
                            case CPContentBaseClass.FieldTypeIdEnum.Text:
                            case CPContentBaseClass.FieldTypeIdEnum.Link:
                            case CPContentBaseClass.FieldTypeIdEnum.ResourceLink: {
                                    //
                                    // ----- Text Type
                                    EditorString += AdminUIEditorController.getTextEditor(core, field.nameLc, fieldValue_text, editorReadOnly, fieldHtmlId);
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                    //
                                    // ----- Boolean ReadOnly
                                    EditorString += AdminUIEditorController.getBooleanEditor(core, field.nameLc, GenericController.encodeBoolean(fieldValueObject), editorReadOnly, fieldHtmlId);
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                    //
                                    // ----- Lookup, readonly
                                    if (field.lookupContentId != 0) {
                                        EditorString = AdminUIEditorController.getLookupContentEditor(core, field.nameLc, GenericController.encodeInteger(fieldValueObject), field.lookupContentId, ref IsEmptyList, editorReadOnly, fieldHtmlId, whyReadOnlyMsg, field.required, "");
                                        editorEnv.formFieldList += "," + field.nameLc;
                                        editorWrapperSyle = "max-width:400px";
                                    } else if (!string.IsNullOrEmpty(field.lookupList)) {
                                        EditorString = AdminUIEditorController.getLookupListEditor(core, field.nameLc, encodeInteger(fieldValueObject), field.lookupList.Split(',').ToList(), editorReadOnly, fieldHtmlId, whyReadOnlyMsg, field.required);
                                        editorEnv.formFieldList += "," + field.nameLc;
                                        editorWrapperSyle = "max-width:400px";
                                    } else {
                                        //
                                        // -- log exception but dont throw
                                        LogController.logWarn(core, new GenericException("Field [" + adminData.adminContent.name + "." + field.nameLc + "] is a Lookup field, but no LookupContent or LookupList has been configured"));
                                        EditorString += "[Selection not configured]";
                                    }
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                    //
                                    // ----- date, readonly
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString = AdminUIEditorController.getDateTimeEditor(core, field.nameLc, encodeDate(fieldValueObject), editorReadOnly, fieldHtmlId, field.required);
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                    //
                                    // ----- Member Select ReadOnly
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString = AdminUIEditorController.getMemberSelectEditor(core, field.nameLc, encodeInteger(fieldValueObject), field.memberSelectGroupId_get(core, adminData.adminContent.name, field.nameLc), field.memberSelectGroupName_get(core), editorReadOnly, fieldHtmlId, field.required, whyReadOnlyMsg);
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                    //
                                    //   Placeholder
                                    EditorString = AdminUIEditorController.getManyToManyEditor(core, field, "field" + field.id, fieldValue_text, editRecord.id, editorReadOnly, whyReadOnlyMsg);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Currency: {
                                    //
                                    // ----- Currency ReadOnly
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString += (HtmlController.inputCurrency(core, field.nameLc, encodeNumber(fieldValue_text), fieldHtmlId, "text form-control", editorReadOnly, false));
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                    //
                                    // ----- double/number/float
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString += (HtmlController.inputNumber(core, field.nameLc, encodeNumber(fieldValue_text), fieldHtmlId, "text form-control", editorReadOnly, false));
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                            case CPContentBaseClass.FieldTypeIdEnum.Integer: {
                                    //
                                    // ----- Others that simply print
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString += (HtmlController.inputInteger(core, field.nameLc, encodeInteger(fieldValue_text), fieldHtmlId, "text form-control", editorReadOnly, false));
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                    //
                                    // edit html as html (see the code)
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString += HtmlController.inputHidden(field.nameLc, fieldValue_text);
                                    fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".RowHeight", 10));
                                    EditorString += HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, fieldRows, -1, fieldHtmlId, false, editorReadOnly, "form-control");
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.HTML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTML: {
                                    //
                                    // ----- HTML types readonly
                                    if (field.htmlContent) {
                                        //
                                        // edit html as html (see the code)
                                        editorEnv.formFieldList += "," + field.nameLc;
                                        EditorString += HtmlController.inputHidden(field.nameLc, fieldValue_text);
                                        fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".RowHeight", 10));
                                        EditorString += HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, fieldRows, -1, fieldHtmlId, false, editorReadOnly, "form-control");
                                    } else {
                                        //
                                        // edit html as wysiwyg readonly
                                        editorEnv.formFieldList += "," + field.nameLc;
                                        EditorString += AdminUIEditorController.getHtmlEditor(core, field.nameLc, fieldValue_text, editorEnv.editorAddonListJSON, editorEnv.styleList, editorEnv.styleOptionList, editorReadOnly, fieldHtmlId);
                                    }
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.LongText:
                            case CPContentBaseClass.FieldTypeIdEnum.FileText: {
                                    //
                                    // ----- LongText, TextFile
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString += HtmlController.inputHidden(field.nameLc, fieldValue_text);
                                    fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".RowHeight", 10));
                                    EditorString += HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, fieldRows, -1, fieldHtmlId, false, editorReadOnly, " form-control");
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.File: {
                                    //
                                    // ----- File ReadOnly
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString = AdminUIEditorController.getFileEditor(core, field.nameLc, fieldValue_text, field.readOnly, fieldHtmlId, field.required, whyReadOnlyMsg);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                    //
                                    // ----- Image ReadOnly
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    string[] imageAltSizeList = { };
                                    string resizedImageUrl = ImageController.getBestFit(core, fieldValue_text, 80, 80, new System.Collections.Generic.List<string>());
                                    EditorString = AdminUIEditorController.getImageEditor(core, field.nameLc, fieldValue_text, resizedImageUrl, field.readOnly, fieldHtmlId);
                                    break;
                                }
                            default: {
                                    //
                                    // ----- Legacy text type -- not used unless something was missed
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString += HtmlController.inputHidden(field.nameLc, fieldValue_text);
                                    if (field.password) {
                                        //
                                        // Password forces simple text box
                                        EditorString += HtmlController.inputText_Legacy(core, field.nameLc, "*****", 0, 0, fieldHtmlId, true, true, "password form-control");
                                    } else if (!field.htmlContent) {
                                        //
                                        // not HTML capable, textarea with resizing
                                        if ((field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Text) && (fieldValue_text.IndexOf("\n") == -1) && (fieldValue_text.Length < 40)) {
                                            //
                                            // text field shorter then 40 characters without a CR
                                            EditorString += HtmlController.inputText_Legacy(core, field.nameLc, fieldValue_text, 1, 0, fieldHtmlId, false, true, "text form-control");
                                        } else {
                                            //
                                            // longer text data, or text that contains a CR
                                            EditorString += HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, 10, -1, fieldHtmlId, false, true, " form-control");
                                        }
                                    } else if (field.htmlContent) {
                                        //
                                        // HTMLContent true, and prefered
                                        fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".PixelHeight", 500));
                                        EditorString += core.html.getFormInputHTML(field.nameLc, fieldValue_text, "500", "", false, true, editorEnv.editorAddonListJSON, editorEnv.styleList, editorEnv.styleOptionList);
                                        EditorString = "<div style=\"width:95%\">" + EditorString + "</div>";
                                    } else {
                                        //
                                        // HTMLContent true, but text editor selected
                                        fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".RowHeight", 10));
                                        EditorString += HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, fieldRows, -1, fieldHtmlId, false, editorReadOnly);
                                    }
                                    break;
                                }
                        }
                    } else {
                        hint = 540;
                        //
                        // -- Not Read Only - Display fields as form elements to be modified
                        switch (field.fieldTypeId) {
                            case CPContentBaseClass.FieldTypeIdEnum.Text: {
                                    //
                                    // ----- Text Type
                                    if (field.password) {
                                        EditorString += AdminUIEditorController.getPasswordEditor(core, field.nameLc, fieldValue_text, false, fieldHtmlId);
                                    } else {
                                        EditorString += AdminUIEditorController.getTextEditor(core, field.nameLc, fieldValue_text, false, fieldHtmlId);
                                    }
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                    //
                                    // ----- Boolean
                                    EditorString += AdminUIEditorController.getBooleanEditor(core, field.nameLc, encodeBoolean(fieldValueObject), false, fieldHtmlId);
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                    //
                                    // ----- Lookup
                                    if (!field.lookupContentId.Equals(0)) {
                                        EditorString = AdminUIEditorController.getLookupContentEditor(core, field.nameLc, encodeInteger(fieldValueObject), field.lookupContentId, ref IsEmptyList, field.readOnly, fieldHtmlId, whyReadOnlyMsg, field.required, "");
                                        editorEnv.formFieldList += "," + field.nameLc;
                                        editorWrapperSyle = "max-width:400px";
                                    } else if (!string.IsNullOrEmpty(field.lookupList)) {
                                        EditorString = AdminUIEditorController.getLookupListEditor(core, field.nameLc, encodeInteger(fieldValueObject), field.lookupList.Split(',').ToList(), field.readOnly, fieldHtmlId, whyReadOnlyMsg, field.required);
                                        editorEnv.formFieldList += "," + field.nameLc;
                                        editorWrapperSyle = "max-width:400px";
                                    } else {
                                        //
                                        // -- log exception but dont throw
                                        LogController.logWarn(core, new GenericException("Field [" + adminData.adminContent.name + "." + field.nameLc + "] is a Lookup field, but no LookupContent or LookupList has been configured"));
                                        EditorString += "[Selection not configured]";
                                    }
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                    //
                                    // ----- Date
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString = AdminUIEditorController.getDateTimeEditor(core, field.nameLc, GenericController.encodeDate(fieldValueObject), field.readOnly, fieldHtmlId, field.required);
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                    //
                                    // ----- Member Select
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString = AdminUIEditorController.getMemberSelectEditor(core, field.nameLc, encodeInteger(fieldValueObject), field.memberSelectGroupId_get(core,adminData.adminContent.name, field.nameLc), field.memberSelectGroupName_get(core), field.readOnly, fieldHtmlId, field.required, whyReadOnlyMsg);
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                    //
                                    //   Placeholder
                                    EditorString = AdminUIEditorController.getManyToManyEditor(core, field, "field" + field.id, fieldValue_text, editRecord.id, false, whyReadOnlyMsg);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.File: {
                                    //
                                    // ----- File
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString = AdminUIEditorController.getFileEditor(core, field.nameLc, fieldValue_text, field.readOnly, fieldHtmlId, field.required, whyReadOnlyMsg);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                    //
                                    // ----- Image ReadOnly
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    string[] imageAltSizeList = { };
                                    string resizedImageUrl = ImageController.getBestFit(core, fieldValue_text, 80, 80, new System.Collections.Generic.List<string>());
                                    EditorString = AdminUIEditorController.getImageEditor(core, field.nameLc, fieldValue_text, resizedImageUrl, field.readOnly, fieldHtmlId);
                                    break;
                               }
                            case CPContentBaseClass.FieldTypeIdEnum.Currency: {
                                    //
                                    // ----- currency
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString += AdminUIEditorController.getCurrencyEditor(core, field.nameLc, encodeNumberNullable(fieldValueObject), field.readOnly, fieldHtmlId, field.required, whyReadOnlyMsg);
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                    //
                                    // ----- double/number/float
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString += AdminUIEditorController.getNumberEditor(core, field.nameLc, encodeNumberNullable(fieldValueObject), field.readOnly, fieldHtmlId, field.required, whyReadOnlyMsg);
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                            case CPContentBaseClass.FieldTypeIdEnum.Integer: {
                                    //
                                    // ----- Others that simply print
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString += (HtmlController.inputInteger(core, field.nameLc, encodeIntegerNullable(fieldValue_text), fieldHtmlId, "text form-control", editorReadOnly, false));
                                    editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Link: {
                                    //
                                    // ----- Link (href value
                                    //
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString = AdminUIEditorController.getLinkEditor(core, field.nameLc, fieldValue_text, editorReadOnly, fieldHtmlId, field.required);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.ResourceLink: {
                                    //
                                    // ----- Resource Link (src value)
                                    //
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString = AdminUIEditorController.getLinkEditor(core, field.nameLc, fieldValue_text, editorReadOnly, fieldHtmlId, field.required);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                    //
                                    // View the content as Html, not wysiwyg
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    EditorString = AdminUIEditorController.getHtmlCodeEditor(core, field.nameLc, fieldValue_text, editorReadOnly, fieldHtmlId);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.HTML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTML: {
                                    //
                                    // content is html
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    if (field.htmlContent) {
                                        //
                                        // View the content as Html, not wysiwyg
                                        EditorString = AdminUIEditorController.getHtmlCodeEditor(core, field.nameLc, fieldValue_text, editorReadOnly, fieldHtmlId);
                                    } else {
                                        //
                                        // wysiwyg editor
                                        EditorString = AdminUIEditorController.getHtmlEditor(core, field.nameLc, fieldValue_text, editorEnv.editorAddonListJSON, editorEnv.styleList, editorEnv.styleOptionList, editorReadOnly, fieldHtmlId);
                                    }
                                    //
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.LongText:
                            case CPContentBaseClass.FieldTypeIdEnum.FileText: {
                                    //
                                    // -- Long Text, use text editor
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".RowHeight", 10));
                                    EditorString = HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, fieldRows, -1, fieldHtmlId, false, false, "text form-control");
                                    //
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileCSS: {
                                    //
                                    // ----- CSS field
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".RowHeight", 10));
                                    EditorString = HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, fieldRows, -1, fieldHtmlId, false, false, "styles form-control");
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileJavascript: {
                                    //
                                    // ----- Javascript field
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".RowHeight", 10));
                                    EditorString = HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, fieldRows, -1, fieldHtmlId, false, false, "text form-control");
                                    //
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                                    //
                                    // ----- xml field
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".RowHeight", 10));
                                    EditorString = HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, fieldRows, -1, fieldHtmlId, false, false, "text form-control");
                                    //
                                    break;
                                }
                            default: {
                                    //
                                    // ----- Legacy text type -- not used unless something was missed
                                    //
                                    editorEnv.formFieldList += "," + field.nameLc;
                                    if (field.password) {
                                        //
                                        // Password forces simple text box
                                        EditorString = HtmlController.inputText_Legacy(core, field.nameLc, fieldValue_text, -1, -1, fieldHtmlId, true, false, "password form-control");
                                    } else if (!field.htmlContent) {
                                        //
                                        // not HTML capable, textarea with resizing
                                        //
                                        if ((field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Text) && (fieldValue_text.IndexOf("\n", StringComparison.InvariantCulture) == -1) && (fieldValue_text.Length < 40)) {
                                            //
                                            // text field shorter then 40 characters without a CR
                                            //
                                            EditorString = HtmlController.inputText_Legacy(core, field.nameLc, fieldValue_text, 1, -1, fieldHtmlId, false, false, "text form-control");
                                        } else {
                                            //
                                            // longer text data, or text that contains a CR
                                            //
                                            EditorString = HtmlController.inputTextarea(core, field.nameLc, fieldValue_text, 10, -1, fieldHtmlId, false, false, "text form-control");
                                        }
                                    } else if (field.htmlContent) {
                                        //
                                        // HTMLContent true, and prefered
                                        //
                                        if (string.IsNullOrEmpty(fieldValue_text)) {
                                            //
                                            // editor needs a starting p tag to setup correctly
                                            //
                                            fieldValue_text = HTMLEditorDefaultCopyNoCr;
                                        }
                                        fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".PixelHeight", 500));
                                        EditorString += core.html.getFormInputHTML(field.nameLc, fieldValue_text, "500", "", false, true, editorEnv.editorAddonListJSON, editorEnv.styleList, editorEnv.styleOptionList);
                                        EditorString = "<div style=\"width:95%\">" + EditorString + "</div>";
                                    } else {
                                        //
                                        // HTMLContent true, but text editor selected
                                        fieldRows = (core.userProperty.getInteger(adminData.adminContent.name + "." + field.nameLc + ".RowHeight", 10));
                                        EditorString = HtmlController.inputTextarea(core, field.nameLc, HtmlController.encodeHtml(fieldValue_text), fieldRows, -1, fieldHtmlId, false, false, "text");
                                    }
                                    break;
                                }
                        }
                    }
                }
                hint = 600;
                //
                // assemble the editor row
                return AdminUIController.getEditRow(core, EditorString, fieldCaption, field.helpDefault, field.required, false, fieldHtmlId, editorWrapperSyle);
            } catch (Exception ex) {
                LogController.logError(core, ex, "getEditorRow, hint[" + hint + "], field [" + hintField + "]");
                throw;
            }
        }
    }
    public class EditorEnvironmentModel {
        public bool needUniqueEmailMessage { get; set; }
        public bool isRootPage { get; set; }
        public bool record_readOnly { get; set; }
        public string editorAddonListJSON { get; set; }
        public string styleList { get; set; }
        public string styleOptionList { get; set; }
        public bool allowHelpMsgCustom { get; set; }
        public string formFieldList { get; set; }
    }
}
