
using Contensive.BaseClasses;
using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Models.Domain;
using System;
using System.Linq;
using NLog;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using Amazon.SimpleEmail;
using System.Data;

namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// create editorRows. 
    /// The editorRow is the editor plus the caption, help and layout that can be sta
    /// </summary>
    public static class EditorRowClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// get the editorRow for this field.
        /// The editorRow is the editor plus the caption, help and layout that can be sta
        /// </summary>
        /// <param name="core"></param>
        /// <param name="field"></param>
        /// <param name="adminData"></param>
        /// <param name="editorEnv"></param>
        /// <returns></returns>
        public static string getEditorRow(CoreController core, ContentFieldMetadataModel field, AdminDataModel adminData, EditorEnvironmentModel editorEnv) {
            int hint = 0;
            string hintField = field.nameLc;
            //
            // -- get current value
            if (!adminData.editRecord.fieldsLc.ContainsKey(field.nameLc)) {
                //
                // -- field not found in edit record. Report and exit with blank editor
                logger.Error($"{core.logCommonMessage}", new GenericException("getEditorRow, field [" + field.nameLc + "] not found in editRecord collection for content [" + adminData.adminContent.name + "]"));
                return "<!-- no editor row available because field not found -->";
            }
            //
            object fieldValueObject = adminData.editRecord.fieldsLc[field.nameLc].value_content;
            string fieldValue_text = encodeText(fieldValueObject);
            try {
                EditorRequest request = new() {
                    contentName = adminData.adminContent.name,
                    contentId = adminData.adminContent.id,
                    field = field,
                    tableName = adminData.adminContent.tableName,
                    fieldTypeEditors = core.cacheRuntime.fieldEditorAddonList,
                    editRecordId = adminData.editRecord.id,
                    editViewTitleSuffix = adminData.editViewTitleSuffix,
                    isContentRootPage = editorEnv.isContentRootPage,
                    record_readOnly = editorEnv.record_readOnly,
                    editorAddonListJSON = editorEnv.editorAddonListJSON,
                    styleList = editorEnv.styleList,
                    styleOptionList = editorEnv.styleOptionList,
                    formFieldList = editorEnv.formFieldList,
                    editRecordContentControlId = adminData.editRecord.contentControlId,
                    editRecordNameLc = adminData.editRecord.nameLc,
                    currentValue = fieldValue_text,
                    fields = adminData.adminContent.fields,
                    htmlName = field.nameLc
                };
                editorResponse response = getEditor(core, request);
                //
                // -- restore updated request arguments
                editorEnv.formFieldList = request.formFieldList;
                //
                // assemble the editor row
                return AdminUIController.getEditRow(core, response.editorString, response.fieldCaption, field.helpDefault, field.required, false, response.fieldHtmlId, response.editorWrapperSyle);
            } catch (Exception ex) {
                logger.Error($"{core.logCommonMessage}", ex, "getEditorRow, hint[" + hint + "], field [" + hintField + "]");
                throw;
            }
        }
        public class EditorRequest {
            public ContentFieldMetadataModel field;
            public Dictionary<string, ContentFieldMetadataModel> fields;
            public string contentName;
            public int contentId;
            public string tableName;
            public List<FieldTypeEditorAddonModel> fieldTypeEditors;
            //public Models.EditRecordModel editRecord;
            public int editRecordId;
            public string editRecordNameLc;
            public int editRecordContentControlId;
            public string editViewTitleSuffix;
            public bool isContentRootPage;
            public bool record_readOnly;
            public string editorAddonListJSON;
            public string styleList;
            public string styleOptionList;
            public string formFieldList;
            public string currentValue;
            public string htmlName;
        }

        public class editorResponse {
            public string content;
            public string fieldHtmlId;
            public string fieldCaption;
            public string editorWrapperSyle;
            public string editorString;
        }

        public static editorResponse getEditor(CoreController core, EditorRequest request) {
            try {
                //
                editorResponse response = new() {
                    content = "",
                    editorString = "",
                    editorWrapperSyle = "",
                    fieldCaption = "",
                    fieldHtmlId = ""
                };
                ////
                //// -- get current value
                //if (!request.editRecord.fieldsLc.ContainsKey(request.field.nameLc)) {
                //    //
                //    // -- field not found in edit record. Report and exit with blank editor
                //    logger.Error($"{core.logCommonMessage}", new GenericException("getEditorRow, field [" + request.field.nameLc + "] not found in editRecord collection for content [" + request.contentName + "]"));
                //    return new editorResponse {
                //        editorString = "<!-- no editor row available because field not found -->"
                //    };
                //}
                ////
                //object fieldValueObject = request.editRecord.fieldsLc[request.field.nameLc].value_content;
                //string fieldValue_text = encodeText(fieldValueObject);
                //
                string whyReadOnlyMsg = "";
                int fieldRows = 1;
                response.fieldHtmlId = request.field.nameLc + request.field.id.ToString();
                response.fieldCaption = request.field.caption;
                if (request.field.uniqueName) {
                    //
                    response.fieldCaption = "&nbsp;**" + response.fieldCaption;
                } else {
                    //
                    if (request.field.nameLc.ToLowerInvariant() == "email") {
                        if ((request.tableName.ToLowerInvariant() == "ccmembers") && ((core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin, false)))) {
                            response.fieldCaption = "&nbsp;***" + response.fieldCaption;
                        }
                    }
                }
                //
                if (request.field.required) {
                    response.fieldCaption = "&nbsp;*" + response.fieldCaption;
                }
                //adminData.formInputCount += 1;
                bool fieldForceReadOnly = false;
                //
                // Read only Special Cases
                if (request.isContentRootPage) {
                    //
                    // -- page content metadata, these are the special fields
                    switch (toLCase(request.field.nameLc)) {
                        case "active": {
                                //
                                // if active, it is read only -- if inactive, let them set it active.
                                fieldForceReadOnly = encodeBoolean(request.currentValue);
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
                                request.currentValue = "1";
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
                //
                // Special Case - ccemail table Alloweid should be disabled if siteproperty AllowLinkLogin is false
                //
                if (toLCase(request.tableName) == "ccemail" && toLCase(request.field.nameLc) == "allowlinkeid") {
                    if (!(core.siteProperties.getBoolean("AllowLinkLogin", true))) {
                        request.currentValue = "0";
                        fieldForceReadOnly = true;
                        request.currentValue = "0";
                    }
                }
                //
                // -- determine custom editor addon
                AddonModel editorAddon = null;
                if (!string.IsNullOrEmpty(request.field.editorAddonGuid)) {
                    //
                    // -- set editor from field
                    editorAddon = core.cacheRuntime.addonCache.create(request.field.editorAddonGuid);
                }
                var fieldEditor = request.fieldTypeEditors.Find(x => (x.fieldTypeId == (int)request.field.fieldTypeId));
                if (fieldEditor != null) {
                    //
                    // -- set editor from field type
                    int fieldTypeDefaultEditorAddonId = (int)fieldEditor.editorAddonId;
                    editorAddon = core.cacheRuntime.addonCache.create(fieldTypeDefaultEditorAddonId);
                }
                //
                // -- create editor: custom, read-only, editable
                bool useEditorAddon = false;
                bool editorReadOnly = (request.record_readOnly || request.field.readOnly || (request.editRecordId != 0 && request.field.notEditable) || (fieldForceReadOnly));
                if (editorAddon != null) {
                    //
                    //--------------------------------------------------------------------------------------------
                    // ----- Custom Editor
                    //--------------------------------------------------------------------------------------------
                    //
                    core.docProperties.setProperty("editorRecordId", request.editRecordId);
                    core.docProperties.setProperty("editorName", request.htmlName);
                    core.docProperties.setProperty("editorValue", request.currentValue);
                    core.docProperties.setProperty("editorFieldId", request.field.id);
                    core.docProperties.setProperty("editorFieldType", (int)request.field.fieldTypeId);
                    core.docProperties.setProperty("editorReadOnly", editorReadOnly);
                    core.docProperties.setProperty("editorWidth", "");
                    core.docProperties.setProperty("editorHeight", "");
                    if (request.field.fieldTypeId.isOneOf(CPContentBaseClass.FieldTypeIdEnum.HTML, CPContentBaseClass.FieldTypeIdEnum.HTMLCode, CPContentBaseClass.FieldTypeIdEnum.FileHTML, CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode)) {
                        //
                        // include html related arguments
                        core.docProperties.setProperty("editorAllowActiveContent", "1");
                        core.docProperties.setProperty("editorAddonList", request.editorAddonListJSON);
                        core.docProperties.setProperty("editorStyles", request.styleList);
                        core.docProperties.setProperty("editorStyleOptions", request.styleOptionList);
                    }
                    response.editorString = core.addon.execute(editorAddon, new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                        addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextEditor,
                        errorContextMessage = "field editor id:" + editorAddon.id
                    });
                    useEditorAddon = !string.IsNullOrEmpty(response.editorString);
                    if (useEditorAddon) {
                        //
                        // -- editor worked
                        request.formFieldList += "," + request.field.nameLc;
                    } else {
                        //
                        // -- editor failed, determine if it is missing (or inactive). If missing, remove it from the members preferences
                        using var csData = new CsModel(core);
                        if (!csData.openSql("select id from ccaggregatefunctions where id=" + editorAddon.id)) {
                            //
                            // -- missing, not just inactive
                            response.editorString = "";
                            //
                            // load user's editor preferences to fieldEditorPreferences() - this is the editor this user has picked when there are >1
                            //   fieldId:addonId,fieldId:addonId,etc
                            //   with custom FancyBox form in edit window with button "set editor preference"
                            //   this button causes a 'refresh' action, reloads fields with stream without save
                            //
                            string tmpList = core.userProperty.getText("editorPreferencesForContent:" + request.contentId, "");
                            int PosStart = strInstr(1, "," + tmpList, "," + request.field.id + ":");
                            if (PosStart > 0) {
                                int PosEnd = strInstr(PosStart + 1, "," + tmpList, ",");
                                if (PosEnd == 0) {
                                    tmpList = tmpList.left(PosStart - 1);
                                } else {
                                    tmpList = tmpList.left(PosStart - 1) + tmpList.Substring(PosEnd - 1);
                                }
                                core.userProperty.setProperty("editorPreferencesForContent:" + request.contentId, tmpList);
                            }
                        }
                    }
                }
                //
                // -- style for editor wrapper used to limit the width of some editors like integer
                response.editorWrapperSyle = "";
                if (!useEditorAddon) {
                    bool IsEmptyList = false;
                    //
                    // -- if custom editor not used or if it failed
                    string field_LookupContentSqlFilter = request.field.LookupContentSqlFilter;
                    if (!string.IsNullOrEmpty(field_LookupContentSqlFilter)) {
                        int pos0 = field_LookupContentSqlFilter.IndexOf('{');
                        int pos1 = field_LookupContentSqlFilter.IndexOf('}');
                        if (pos0 >= 0 && pos1 > (pos0 + 1)) {
                            //
                            // -- this lookup query criteria includes {fieldname} arguments that are manually replaced before running the query.
                            // -- cache a name/value list for this record
                            //
                            string contentLookupFieldName = field_LookupContentSqlFilter.Substring(pos0 + 1, pos1 - pos0 - 1).ToLower();
                            if (!request.fields.ContainsKey(contentLookupFieldName)) {
                                // -- field not found, remove entire filter
                                logger.Warn($"{core.logCommonMessage},Admin Edit View, LookupContentSqlFilter contains mustache replacement but field was not found, content [{request.contentName}], field [{request.field.nameLc}], LookupContentSqlFilter [{request.field.LookupContentSqlFilter}]");
                                field_LookupContentSqlFilter = "";
                            } else {
                                // -- replace field name with field value
                                DataTable dt = core.db.executeQuery($"select {contentLookupFieldName} from {request.tableName} where id={request.editRecordId}");
                                if (dt?.Rows != null && dt.Rows.Count == 1) {
                                    field_LookupContentSqlFilter.replace($"{{{contentLookupFieldName}}}", encodeText(dt.Rows[0][0]), StringComparison.CurrentCultureIgnoreCase);
                                }
                            }
                        }
                    }
                    //
                    if (request.field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Redirect) {
                        //
                        // ----- Default Editor, Redirect fields (the same for normal/readonly/spelling)

                        response.editorString = AdminUIEditorController.getRedirectEditor(core, request.field, request.contentId, request.editViewTitleSuffix, request.editRecordNameLc, request.editRecordId, request.editRecordContentControlId, request.currentValue, editorReadOnly, response.fieldHtmlId, request.field.required);
                    } else if (editorReadOnly) {
                        //
                        //--------------------------------------------------------------------------------------------
                        // ----- Display fields as read only
                        //--------------------------------------------------------------------------------------------
                        //
                        if (!string.IsNullOrEmpty(whyReadOnlyMsg)) {
                            whyReadOnlyMsg = "<span class=\"ccDisabledReason\">" + whyReadOnlyMsg + "</span>";
                        }
                        switch (request.field.fieldTypeId) {
                            case CPContentBaseClass.FieldTypeIdEnum.Text:
                            case CPContentBaseClass.FieldTypeIdEnum.Link:
                            case CPContentBaseClass.FieldTypeIdEnum.ResourceLink: {
                                    //
                                    // ----- Text Type
                                    response.editorString += AdminUIEditorController.getTextEditor(core, request.field.nameLc, request.currentValue, editorReadOnly, response.fieldHtmlId);
                                    request.formFieldList += "," + request.field.nameLc;
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                    //
                                    // ----- Boolean ReadOnly
                                    response.editorString += AdminUIEditorController.getBooleanEditor(core, request.field.nameLc, encodeBoolean(request.currentValue), editorReadOnly, response.fieldHtmlId);
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                    //
                                    // ----- Lookup, readonly
                                    if (request.field.lookupContentId != 0) {
                                        response.editorString = AdminUIEditorController.getLookupContentEditor(core, request.field.nameLc, encodeInteger(request.currentValue), request.field.lookupContentId, ref IsEmptyList, editorReadOnly, response.fieldHtmlId, whyReadOnlyMsg, request.field.required, field_LookupContentSqlFilter);
                                        request.formFieldList += "," + request.field.nameLc;
                                        response.editorWrapperSyle = "max-width:400px";
                                    } else if (!string.IsNullOrEmpty(request.field.lookupList)) {
                                        response.editorString = AdminUIEditorController.getLookupListEditor(core, request.field.nameLc, encodeInteger(request.currentValue), request.field.lookupList.Split(',').ToList(), editorReadOnly, response.fieldHtmlId, whyReadOnlyMsg, request.field.required);
                                        request.formFieldList += "," + request.field.nameLc;
                                        response.editorWrapperSyle = "max-width:400px";
                                    } else {
                                        //
                                        // -- log exception but dont throw
                                        logger.Warn($"{core.logCommonMessage}", new GenericException("Field [" + request.contentName + "." + request.field.nameLc + "] is a Lookup field, but no LookupContent or LookupList has been configured"));
                                        response.editorString += "[Selection not configured]";
                                    }
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                    //
                                    // ----- date, readonly
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString = AdminUIEditorController.getDateTimeEditor(core, request.field.nameLc, encodeDate(request.currentValue), editorReadOnly, response.fieldHtmlId, request.field.required);
                                    response.editorWrapperSyle = "max-width:500px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                    //
                                    // ----- Member Select ReadOnly
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString = AdminUIEditorController.getMemberSelectEditor(core, request.field.nameLc, encodeInteger(request.currentValue), request.field.memberSelectGroupId_get(core, request.contentName, request.field.nameLc), request.field.memberSelectGroupName_get(core), editorReadOnly, response.fieldHtmlId, request.field.required, whyReadOnlyMsg);
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                    //
                                    //   Placeholder
                                    response.editorString = AdminUIEditorController.getManyToManyEditor(core, request.field, "field" + request.field.id, request.currentValue, request.editRecordId, editorReadOnly, whyReadOnlyMsg);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Currency: {
                                    //
                                    // ----- Currency ReadOnly
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString += (HtmlController.inputCurrency(core, request.field.nameLc, encodeNumber(request.currentValue), response.fieldHtmlId, "text form-control", editorReadOnly, false));
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                    //
                                    // ----- double/number/float
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString += (HtmlController.inputNumber(core, request.field.nameLc, encodeNumber(request.currentValue), response.fieldHtmlId, "text form-control", editorReadOnly, false));
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                            case CPContentBaseClass.FieldTypeIdEnum.Integer: {
                                    //
                                    // ----- Others that simply print
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString += (HtmlController.inputInteger(core, request.field.nameLc, encodeInteger(request.currentValue), response.fieldHtmlId, "text form-control", editorReadOnly, false));
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                    //
                                    // edit html as html (see the code)
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString += HtmlController.inputHidden(request.field.nameLc, request.currentValue);
                                    fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".RowHeight", 10));
                                    response.editorString += HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, fieldRows, -1, response.fieldHtmlId, false, editorReadOnly, "form-control");
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.HTML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTML: {
                                    //
                                    // ----- HTML types readonly
                                    if (request.field.htmlContent) {
                                        //
                                        // edit html as html (see the code)
                                        request.formFieldList += "," + request.field.nameLc;
                                        response.editorString += HtmlController.inputHidden(request.field.nameLc, request.currentValue);
                                        fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".RowHeight", 10));
                                        response.editorString += HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, fieldRows, -1, response.fieldHtmlId, false, editorReadOnly, "form-control");
                                    } else {
                                        //
                                        // edit html as wysiwyg readonly
                                        request.formFieldList += "," + request.field.nameLc;
                                        response.editorString += AdminUIEditorController.getHtmlEditor(core, request.field.nameLc, request.currentValue, request.editorAddonListJSON, request.styleList, request.styleOptionList, editorReadOnly, response.fieldHtmlId);
                                    }
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.LongText:
                            case CPContentBaseClass.FieldTypeIdEnum.FileText: {
                                    //
                                    // ----- LongText, TextFile
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString += HtmlController.inputHidden(request.field.nameLc, request.currentValue);
                                    fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".RowHeight", 10));
                                    response.editorString += HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, fieldRows, -1, response.fieldHtmlId, false, editorReadOnly, " form-control");
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.File: {
                                    //
                                    // ----- File ReadOnly
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString = AdminUIEditorController.getFileEditor(core, request.field.nameLc, request.currentValue, request.field.readOnly, response.fieldHtmlId, request.field.required, whyReadOnlyMsg);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                    //
                                    // ----- Image ReadOnly
                                    request.formFieldList += "," + request.field.nameLc;
                                    string imageAltSizes = "";
                                    string resizedImageUrl = ImageController.resizeAndCropNoTypeChange(core, request.currentValue, 80, 80, ref imageAltSizes, out bool _);
                                    response.editorString = AdminUIEditorController.getImageEditor(core, request.field.nameLc, request.currentValue, resizedImageUrl, request.field.readOnly, response.fieldHtmlId);
                                    break;
                                }
                            default: {
                                    //
                                    // ----- Legacy text type -- not used unless something was missed
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString += HtmlController.inputHidden(request.field.nameLc, request.currentValue);
                                    if (request.field.password) {
                                        //
                                        // Password forces simple text box
                                        response.editorString += HtmlController.inputText_Legacy(core, request.field.nameLc, "*****", 0, 0, response.fieldHtmlId, true, true, "password form-control");
                                    } else if (!request.field.htmlContent) {
                                        //
                                        // not HTML capable, textarea with resizing
                                        if ((request.field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Text) && (request.currentValue.IndexOf("\n") == -1) && (request.currentValue.Length < 40)) {
                                            //
                                            // text field shorter then 40 characters without a CR
                                            response.editorString += HtmlController.inputText_Legacy(core, request.field.nameLc, request.currentValue, 1, 0, response.fieldHtmlId, false, true, "text form-control");
                                        } else {
                                            //
                                            // longer text data, or text that contains a CR
                                            response.editorString += HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, 10, -1, response.fieldHtmlId, false, true, " form-control");
                                        }
                                    } else if (request.field.htmlContent) {
                                        //
                                        // HTMLContent true, and prefered
                                        fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".PixelHeight", 500));
                                        response.editorString += core.html.getFormInputHTML(request.field.nameLc, request.currentValue, "500", "", false, true, request.editorAddonListJSON, request.styleList, request.styleOptionList);
                                        response.editorString = "<div style=\"width:95%\">" + response.editorString + "</div>";
                                    } else {
                                        //
                                        // HTMLContent true, but text editor selected
                                        fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".RowHeight", 10));
                                        response.editorString += HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, fieldRows, -1, response.fieldHtmlId, false, editorReadOnly);
                                    }
                                    break;
                                }
                        }
                    } else {
                        //
                        // -- Not Read Only - Display fields as form elements to be modified
                        switch (request.field.fieldTypeId) {
                            case CPContentBaseClass.FieldTypeIdEnum.Text: {
                                    //
                                    // ----- Text Type
                                    if (request.field.password) {
                                        response.editorString += AdminUIEditorController.getPasswordEditor(core, request.field.nameLc, request.currentValue, false, response.fieldHtmlId);
                                    } else {
                                        response.editorString += AdminUIEditorController.getTextEditor(core, request.field.nameLc, request.currentValue, false, response.fieldHtmlId);
                                    }
                                    request.formFieldList += "," + request.field.nameLc;
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                    //
                                    // ----- Boolean
                                    response.editorString += AdminUIEditorController.getBooleanEditor(core, request.field.nameLc, encodeBoolean(request.currentValue), false, response.fieldHtmlId);
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                    //
                                    // ----- Lookup
                                    if (!request.field.lookupContentId.Equals(0)) {
                                        response.editorString = AdminUIEditorController.getLookupContentEditor(core, request.field.nameLc, encodeInteger(request.currentValue), request.field.lookupContentId, ref IsEmptyList, request.field.readOnly, response.fieldHtmlId, whyReadOnlyMsg, request.field.required, field_LookupContentSqlFilter);
                                        request.formFieldList += "," + request.field.nameLc;
                                        response.editorWrapperSyle = "max-width:400px";
                                    } else if (!string.IsNullOrEmpty(request.field.lookupList)) {
                                        response.editorString = AdminUIEditorController.getLookupListEditor(core, request.field.nameLc, encodeInteger(request.currentValue), request.field.lookupList.Split(',').ToList(), request.field.readOnly, response.fieldHtmlId, whyReadOnlyMsg, request.field.required);
                                        request.formFieldList += "," + request.field.nameLc;
                                        response.editorWrapperSyle = "max-width:400px";
                                    } else {
                                        //
                                        // -- log exception but dont throw
                                        logger.Warn($"{core.logCommonMessage}", new GenericException("Field [" + request.contentName + "." + request.field.nameLc + "] is a Lookup field, but no LookupContent or LookupList has been configured"));
                                        response.editorString += "[Selection not configured]";
                                    }
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                    //
                                    // ----- Date
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString = AdminUIEditorController.getDateTimeEditor(core, request.field.nameLc, encodeDate(request.currentValue), request.field.readOnly, response.fieldHtmlId, request.field.required);
                                    response.editorWrapperSyle = "max-width:500px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                    //
                                    // ----- Member Select
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString = AdminUIEditorController.getMemberSelectEditor(core, request.field.nameLc, encodeInteger(request.currentValue), request.field.memberSelectGroupId_get(core, request.contentName, request.field.nameLc), request.field.memberSelectGroupName_get(core), request.field.readOnly, response.fieldHtmlId, request.field.required, whyReadOnlyMsg);
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                    //
                                    //   Placeholder
                                    response.editorString = AdminUIEditorController.getManyToManyEditor(core, request.field, "field" + request.field.id, request.currentValue, request.editRecordId, false, whyReadOnlyMsg);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.File: {
                                    //
                                    // ----- File
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString = AdminUIEditorController.getFileEditor(core, request.field.nameLc, request.currentValue, request.field.readOnly, response.fieldHtmlId, request.field.required, whyReadOnlyMsg);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                    //
                                    // ----- Image ReadOnly
                                    request.formFieldList += "," + request.field.nameLc;
                                    string imageAltSizes = "";
                                    string resizedImageUrl = ImageController.resizeAndCropNoTypeChange(core, request.currentValue, 80, 80, ref imageAltSizes, out bool _);
                                    response.editorString = AdminUIEditorController.getImageEditor(core, request.field.nameLc, request.currentValue, resizedImageUrl, request.field.readOnly, response.fieldHtmlId);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Currency: {
                                    //
                                    // ----- currency
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString += AdminUIEditorController.getCurrencyEditor(core, request.field.nameLc, encodeNumberNullable(request.currentValue), request.field.readOnly, response.fieldHtmlId, request.field.required, whyReadOnlyMsg);
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                    //
                                    // ----- double/number/float
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString += AdminUIEditorController.getNumberEditor(core, request.field.nameLc, encodeNumberNullable(request.currentValue), request.field.readOnly, response.fieldHtmlId, request.field.required, whyReadOnlyMsg);
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                            case CPContentBaseClass.FieldTypeIdEnum.Integer: {
                                    //
                                    // ----- Others that simply print
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString += (HtmlController.inputInteger(core, request.field.nameLc, encodeIntegerNullable(request.currentValue), response.fieldHtmlId, "text form-control", editorReadOnly, false));
                                    response.editorWrapperSyle = "max-width:400px";
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.Link: {
                                    //
                                    // ----- Link (href value
                                    //
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString = AdminUIEditorController.getLinkEditor(core, request.field.nameLc, request.currentValue, editorReadOnly, response.fieldHtmlId, request.field.required);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.ResourceLink: {
                                    //
                                    // ----- Resource Link (src value)
                                    //
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString = AdminUIEditorController.getLinkEditor(core, request.field.nameLc, request.currentValue, editorReadOnly, response.fieldHtmlId, request.field.required);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                    //
                                    // View the content as Html, not wysiwyg
                                    request.formFieldList += "," + request.field.nameLc;
                                    response.editorString = AdminUIEditorController.getHtmlCodeEditor(core, request.field.nameLc, request.currentValue, editorReadOnly, response.fieldHtmlId);
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.HTML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTML: {
                                    //
                                    // content is html
                                    request.formFieldList += "," + request.field.nameLc;
                                    if (request.field.htmlContent) {
                                        //
                                        // View the content as Html, not wysiwyg
                                        response.editorString = AdminUIEditorController.getHtmlCodeEditor(core, request.field.nameLc, request.currentValue, editorReadOnly, response.fieldHtmlId);
                                    } else {
                                        //
                                        // wysiwyg editor
                                        response.editorString = AdminUIEditorController.getHtmlEditor(core, request.field.nameLc, request.currentValue, request.editorAddonListJSON, request.styleList, request.styleOptionList, editorReadOnly, response.fieldHtmlId);
                                    }
                                    //
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.LongText:
                            case CPContentBaseClass.FieldTypeIdEnum.FileText: {
                                    //
                                    // -- Long Text, use text editor
                                    request.formFieldList += "," + request.field.nameLc;
                                    fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".RowHeight", 10));
                                    response.editorString = HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, fieldRows, -1, response.fieldHtmlId, false, false, "text form-control");
                                    //
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileCSS: {
                                    //
                                    // ----- CSS field
                                    request.formFieldList += "," + request.field.nameLc;
                                    fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".RowHeight", 10));
                                    response.editorString = HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, fieldRows, -1, response.fieldHtmlId, false, false, "styles form-control");
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileJavascript: {
                                    //
                                    // ----- Javascript field
                                    request.formFieldList += "," + request.field.nameLc;
                                    fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".RowHeight", 10));
                                    response.editorString = HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, fieldRows, -1, response.fieldHtmlId, false, false, "text form-control");
                                    //
                                    break;
                                }
                            case CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                                    //
                                    // ----- xml field
                                    request.formFieldList += "," + request.field.nameLc;
                                    fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".RowHeight", 10));
                                    response.editorString = HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, fieldRows, -1, response.fieldHtmlId, false, false, "text form-control");
                                    //
                                    break;
                                }
                            default: {
                                    //
                                    // ----- Legacy text type -- not used unless something was missed
                                    //
                                    request.formFieldList += "," + request.field.nameLc;
                                    if (request.field.password) {
                                        //
                                        // Password forces simple text box
                                        response.editorString = HtmlController.inputText_Legacy(core, request.field.nameLc, request.currentValue, -1, -1, response.fieldHtmlId, true, false, "password form-control");
                                    } else if (!request.field.htmlContent) {
                                        //
                                        // not HTML capable, textarea with resizing
                                        //
                                        if ((request.field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Text) && (request.currentValue.IndexOf("\n", StringComparison.InvariantCulture) == -1) && (request.currentValue.Length < 40)) {
                                            //
                                            // text field shorter then 40 characters without a CR
                                            //
                                            response.editorString = HtmlController.inputText_Legacy(core, request.field.nameLc, request.currentValue, 1, -1, response.fieldHtmlId, false, false, "text form-control");
                                        } else {
                                            //
                                            // longer text data, or text that contains a CR
                                            //
                                            response.editorString = HtmlController.inputTextarea(core, request.field.nameLc, request.currentValue, 10, -1, response.fieldHtmlId, false, false, "text form-control");
                                        }
                                    } else if (request.field.htmlContent) {
                                        //
                                        // HTMLContent true, and prefered
                                        //
                                        if (string.IsNullOrEmpty(request.currentValue)) {
                                            //
                                            // editor needs a starting p tag to setup correctly
                                            //
                                            request.currentValue = HTMLEditorDefaultCopyNoCr;
                                        }
                                        fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".PixelHeight", 500));
                                        response.editorString += core.html.getFormInputHTML(request.field.nameLc, request.currentValue, "500", "", false, true, request.editorAddonListJSON, request.styleList, request.styleOptionList);
                                        response.editorString = "<div style=\"width:95%\">" + response.editorString + "</div>";
                                    } else {
                                        //
                                        // HTMLContent true, but text editor selected
                                        fieldRows = (core.userProperty.getInteger(request.contentName + "." + request.field.nameLc + ".RowHeight", 10));
                                        response.editorString = HtmlController.inputTextarea(core, request.field.nameLc, HtmlController.encodeHtml(request.currentValue), fieldRows, -1, response.fieldHtmlId, false, false, "text");
                                    }
                                    break;
                                }
                        }
                    }
                }
                return response;
            } catch (Exception ex) {
                core.cpParent.Site.ErrorReport(ex);
                throw;
            }

        }
    }
    public class EditorEnvironmentModel {
        public bool isContentRootPage { get; set; }
        public bool record_readOnly { get; set; }
        public string editorAddonListJSON { get; set; }
        public string styleList { get; set; }
        public string styleOptionList { get; set; }
        public bool allowHelpMsgCustom { get; set; }
        public string formFieldList { get; set; }
    }
}
