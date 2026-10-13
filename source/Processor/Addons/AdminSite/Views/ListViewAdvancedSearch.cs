
using System;
using System.Collections.Generic;
using System.Linq;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Models.Domain;
using Contensive.BaseClasses;
using NLog;

namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// list view advanced search
    /// </summary>
    public static class ListViewAdvancedSearch {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //=================================================================================
        /// <summary>
        /// Process search criteria from the advanced search form and save to the grid config.
        /// Called from ProcessFormController when the Search button is pressed.
        /// </summary>
        public static void processSearchCriteria(CoreController core, AdminDataModel adminData) {
            var gridConfig = new GridConfigClass(core, adminData);
            int formFieldCnt = core.docProperties.getInteger("fieldcnt");
            if (formFieldCnt > 0) {
                for (int formFieldPtr = 0; formFieldPtr < formFieldCnt; formFieldPtr++) {
                    string fieldName = GenericController.toLCase(core.docProperties.getText("fieldname" + formFieldPtr));
                    var matchOption = (FindWordMatchEnum)core.docProperties.getInteger("FieldMatch" + formFieldPtr);
                    string searchValue;
                    switch (matchOption) {
                        case FindWordMatchEnum.MatchEquals:
                        case FindWordMatchEnum.MatchGreaterThan:
                        case FindWordMatchEnum.matchincludes:
                        case FindWordMatchEnum.MatchLessThan:
                            searchValue = core.docProperties.getText("FieldValue" + formFieldPtr);
                            break;
                        default:
                            searchValue = "";
                            break;
                    }
                    if (!gridConfig.findWords.ContainsKey(fieldName)) {
                        if (matchOption != FindWordMatchEnum.MatchIgnore) {
                            gridConfig.findWords.Add(fieldName, new GridConfigFindWordClass {
                                Name = fieldName,
                                MatchOption = matchOption,
                                Value = searchValue
                            });
                        }
                    } else {
                        gridConfig.findWords[fieldName].MatchOption = matchOption;
                        gridConfig.findWords[fieldName].Value = searchValue;
                    }
                }
            }
            AdminContentController.setIndexSQL_SaveIndexConfig(core.cpParent, core, gridConfig);
        }
        //
        //=================================================================================
        /// <summary>
        /// list view advanced search
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <returns></returns>
        public static string get(CPClass cp, CoreController core, AdminDataModel adminData) {
            string returnForm = "";
            try {
                Controllers.EditRefererStackController.initializeRefererStack(core);
                //
                ContentMetadataModel CDef = null;
                string FieldName = null;
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                int FieldPtr = 0;
                string[] FieldNames = { };
                string[] FieldCaption = { };
                int[] fieldId = null;
                CPContentBaseClass.FieldTypeIdEnum[] fieldTypeId = { };
                string[] FieldValue = { };
                int[] FieldMatchOptions = { };
                int FieldMatchOption = 0;
                string[] FieldLookupContentName = { };
                string[] FieldLookupList = { };
                int ContentId = 0;
                int FieldCnt = 0;
                int FieldSize = 0;
                string LeftButtons = "";
                string ButtonBar = null;
                string Title = null;
                string TitleBar = null;
                string Content = null;
                //
                GridConfigClass gridConfig = new(core, adminData);
                //
                // ----- ButtonBar
                //
                LeftButtons += AdminUIController.getButtonPrimary(ButtonCancel);
                LeftButtons += AdminUIController.getButtonPrimary(ButtonSearch);
                ButtonBar = AdminUIController.getSectionButtonBar(core, LeftButtons, "");
                //
                // ----- TitleBar
                //
                Title = adminData.adminContent.name;
                Title = Title + " Advanced Search";
                string TitleDescription = "<div>Enter criteria for each field to identify and select your results. The results of a search will have to have all of the criteria you enter.</div>";
                TitleBar = AdminUIController.getSectionHeader(core, Title, TitleDescription);
                //
                // ----- List out all fields
                //
                CDef = ContentMetadataModel.createByUniqueName(core, adminData.adminContent.name);
                FieldSize = 100;
                Array.Resize(ref FieldNames, FieldSize + 1);
                Array.Resize(ref FieldCaption, FieldSize + 1);
                Array.Resize(ref fieldId, FieldSize + 1);
                Array.Resize(ref fieldTypeId, FieldSize + 1);
                Array.Resize(ref FieldValue, FieldSize + 1);
                Array.Resize(ref FieldMatchOptions, FieldSize + 1);
                Array.Resize(ref FieldLookupContentName, FieldSize + 1);
                Array.Resize(ref FieldLookupList, FieldSize + 1);
                var fieldsThatAllowNotAuthorable = new List<string> { "id", "dateadded", "createdby", "modifieddate", "modifiedby", "ccguid", "contentcontrolid", "sortorder", "active" };
                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
                    //
                    // skip fields that are not active or not authorable (unless they are system fields that are allowed)
                    if (!field.active) {
                        continue;
                    }
                    if (!field.authorable && !fieldsThatAllowNotAuthorable.Contains(field.nameLc, StringComparer.OrdinalIgnoreCase)) {
                        continue;
                    }
                    if (FieldPtr >= FieldSize) {
                        FieldSize = FieldSize + 100;
                        Array.Resize(ref FieldNames, FieldSize + 1);
                        Array.Resize(ref FieldCaption, FieldSize + 1);
                        Array.Resize(ref fieldId, FieldSize + 1);
                        Array.Resize(ref fieldTypeId, FieldSize + 1);
                        Array.Resize(ref FieldValue, FieldSize + 1);
                        Array.Resize(ref FieldMatchOptions, FieldSize + 1);
                        Array.Resize(ref FieldLookupContentName, FieldSize + 1);
                        Array.Resize(ref FieldLookupList, FieldSize + 1);
                    }
                    FieldName = GenericController.toLCase(field.nameLc);
                    FieldNames[FieldPtr] = FieldName;
                    FieldCaption[FieldPtr] = field.caption;
                    fieldId[FieldPtr] = field.id;
                    fieldTypeId[FieldPtr] = field.fieldTypeId;
                    if (fieldTypeId[FieldPtr] == CPContentBaseClass.FieldTypeIdEnum.Lookup) {
                        ContentId = field.lookupContentId;
                        if (ContentId > 0) {
                            FieldLookupContentName[FieldPtr] = MetadataController.getContentNameByID(core, ContentId);
                        }
                        FieldLookupList[FieldPtr] = field.lookupList;
                    }
                    //
                    // set prepoplate value from indexconfig
                    //
                    if (gridConfig.findWords.ContainsKey(FieldName)) {
                        FieldValue[FieldPtr] = gridConfig.findWords[FieldName].Value;
                        FieldMatchOptions[FieldPtr] = (int)gridConfig.findWords[FieldName].MatchOption;
                    }
                    FieldPtr += 1;
                }
                FieldCnt = FieldPtr;
                //
                // -- sort fields alphabetically by caption
                var sortedIndices = new int[FieldCnt];
                for (int i = 0; i < FieldCnt; i++) { sortedIndices[i] = i; }
                Array.Sort(sortedIndices, (a, b) => string.Compare(FieldCaption[a] ?? "", FieldCaption[b] ?? "", StringComparison.OrdinalIgnoreCase));
                //
                // -- build rows using getEditRow pattern (caption on top, indented content below)
                for (int sortIdx = 0; sortIdx < FieldCnt; sortIdx++) {
                    FieldPtr = sortedIndices[sortIdx];
                    returnForm += HtmlController.inputHidden("fieldname" + FieldPtr, FieldNames[FieldPtr]);
                    FieldMatchOption = FieldMatchOptions[FieldPtr];
                    string radioOptions = "";
                    string valueInput = "";
                    switch (fieldTypeId[FieldPtr]) {
                        case CPContentBaseClass.FieldTypeIdEnum.Date:
                            //
                            // -- Date
                            radioOptions = ""
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "me-1")} ignore</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} empty</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} not empty</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEquals).ToString(), FieldMatchOption.ToString(), "", "me-1")} =</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchGreaterThan).ToString(), FieldMatchOption.ToString(), "", "me-1")} &gt;</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchLessThan).ToString(), FieldMatchOption.ToString(), "", "me-1")} &lt;</label>";
                            valueInput = HtmlController.inputDate(core, "fieldvalue" + FieldPtr, getDate(FieldValue[FieldPtr]));
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Currency:
                        case CPContentBaseClass.FieldTypeIdEnum.Float:
                        case CPContentBaseClass.FieldTypeIdEnum.Integer:
                        case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                            //
                            // -- Numeric
                            radioOptions = ""
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "me-1")} ignore</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} empty</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} not empty</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.matchincludes).ToString(), FieldMatchOption.ToString(), $"n{FieldPtr}", "me-1")} =</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchGreaterThan).ToString(), FieldMatchOption.ToString(), "", "me-1")} &gt;</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchLessThan).ToString(), FieldMatchOption.ToString(), "", "me-1")} &lt;</label>";
                            valueInput = HtmlController.inputText_Legacy(core, "fieldvalue" + FieldPtr, FieldValue[FieldPtr], 1, 5, "", false, false, "ccAdvSearchText").Replace(">", $" onFocus=\"var e=getElementById('n{FieldPtr}');e.checked=1;\">");
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.File:
                        case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                            //
                            // -- File
                            radioOptions = ""
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "me-1")} ignore</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} empty</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} not empty</label>";
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Boolean:
                            //
                            // -- Boolean
                            radioOptions = ""
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "me-1")} ignore</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchTrue).ToString(), FieldMatchOption.ToString(), "", "me-1")} true</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchFalse).ToString(), FieldMatchOption.ToString(), "", "me-1")} false</label>";
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Text:
                        case CPContentBaseClass.FieldTypeIdEnum.LongText:
                        case CPContentBaseClass.FieldTypeIdEnum.HTML:
                        case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                        case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                        case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                        case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                        case CPContentBaseClass.FieldTypeIdEnum.FileJavaScript:
                        case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                            //
                            // -- Text
                            radioOptions = ""
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "me-1")} ignore</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} empty</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} not empty</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.matchincludes).ToString(), FieldMatchOption.ToString(), $"t{FieldPtr}", "me-1")} includes</label>";
                            valueInput = HtmlController.inputText_Legacy(core, "fieldvalue" + FieldPtr, FieldValue[FieldPtr], 1, 5, "", false, false, "ccAdvSearchText").Replace(">", $" onFocus=\"var e=getElementById('t{FieldPtr}');e.checked=1;\">");
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                        case CPContentBaseClass.FieldTypeIdEnum.MemberSelect:
                            //
                            // -- Lookup
                            radioOptions = ""
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "me-1")} ignore</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} empty</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "me-1")} not empty</label>"
                                + $"<label class=\"d-inline-flex align-items-center me-3\">{HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.matchincludes).ToString(), FieldMatchOption.ToString(), $"t{FieldPtr}", "me-1")} includes</label>";
                            valueInput = HtmlController.inputText_Legacy(core, "fieldvalue" + FieldPtr, FieldValue[FieldPtr], 1, 5, "", false, false, "ccAdvSearchText").Replace(">", $" onFocus=\"var e=getElementById('t{FieldPtr}');e.checked=1;\">");
                            break;
                        default:
                            continue;
                    }
                    string editorHtml = $"<div class=\"d-flex flex-wrap align-items-center mb-1\">{radioOptions}</div>";
                    if (!string.IsNullOrEmpty(valueInput)) {
                        editorHtml += $"<div class=\"mt-1\" style=\"max-width:300px;\">{valueInput}</div>";
                    }
                    returnForm += AdminUIController.getEditRow(core, editorHtml, FieldCaption[FieldPtr], "", false);
                }
                Content = returnForm;
                //
                // Assemble LiveWindowTable
                Stream.add(ButtonBar);
                Stream.add(TitleBar);
                Stream.add(Content);
                Stream.add(ButtonBar);
                Stream.add("<input type=hidden name=fieldcnt VALUE=" + FieldCnt + ">");
                Stream.add(HtmlController.inputHidden(rnAdminSourceForm, AdminFormList_AdvancedSearch));
                returnForm = HtmlController.form(core, Stream.text);
                core.html.addTitle(adminData.adminContent.name + " Advanced Search", "admin advanced search" );
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnForm;
        }
    }
}
