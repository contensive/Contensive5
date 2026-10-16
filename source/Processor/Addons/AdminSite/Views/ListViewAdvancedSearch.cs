
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
        public static string get(CPClass cp, CoreController core, AdminDataModel adminData) {
            try {
                Controllers.EditRefererStackController.initializeRefererStack(core);
                //
                GridConfigClass gridConfig = new(core, adminData);
                //
                // -- collect field metadata into parallel arrays (preserves original field pointer indices for form naming)
                int FieldPtr = 0;
                int FieldSize = 100;
                var FieldNames = new string[FieldSize + 1];
                var FieldCaption = new string[FieldSize + 1];
                var fieldId = new int[FieldSize + 1];
                var fieldTypeId = new CPContentBaseClass.FieldTypeIdEnum[FieldSize + 1];
                var FieldValue = new string[FieldSize + 1];
                var FieldMatchOptions = new int[FieldSize + 1];
                var FieldLookupContentName = new string[FieldSize + 1];
                var FieldLookupList = new string[FieldSize + 1];
                //
                var fieldsThatAllowNotAuthorable = new List<string> { "id", "dateadded", "createdby", "modifieddate", "modifiedby", "ccguid", "contentcontrolid", "sortorder", "active" };
                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
                    if (!field.active) { continue; }
                    if (!field.authorable && !fieldsThatAllowNotAuthorable.Contains(field.nameLc, StringComparer.OrdinalIgnoreCase)) { continue; }
                    if (FieldPtr >= FieldSize) {
                        FieldSize += 100;
                        Array.Resize(ref FieldNames, FieldSize + 1);
                        Array.Resize(ref FieldCaption, FieldSize + 1);
                        Array.Resize(ref fieldId, FieldSize + 1);
                        Array.Resize(ref fieldTypeId, FieldSize + 1);
                        Array.Resize(ref FieldValue, FieldSize + 1);
                        Array.Resize(ref FieldMatchOptions, FieldSize + 1);
                        Array.Resize(ref FieldLookupContentName, FieldSize + 1);
                        Array.Resize(ref FieldLookupList, FieldSize + 1);
                    }
                    string fieldName = GenericController.toLCase(field.nameLc);
                    FieldNames[FieldPtr] = fieldName;
                    FieldCaption[FieldPtr] = field.caption;
                    fieldId[FieldPtr] = field.id;
                    fieldTypeId[FieldPtr] = field.fieldTypeId;
                    if (fieldTypeId[FieldPtr] == CPContentBaseClass.FieldTypeIdEnum.Lookup) {
                        int contentId = field.lookupContentId;
                        if (contentId > 0) {
                            FieldLookupContentName[FieldPtr] = MetadataController.getContentNameByID(core, contentId);
                        }
                        FieldLookupList[FieldPtr] = field.lookupList;
                    }
                    if (gridConfig.findWords.ContainsKey(fieldName)) {
                        FieldValue[FieldPtr] = gridConfig.findWords[fieldName].Value;
                        FieldMatchOptions[FieldPtr] = (int)gridConfig.findWords[fieldName].MatchOption;
                    }
                    FieldPtr += 1;
                }
                int FieldCnt = FieldPtr;
                //
                // -- sort fields alphabetically by caption
                var sortedIndices = new int[FieldCnt];
                for (int i = 0; i < FieldCnt; i++) { sortedIndices[i] = i; }
                Array.Sort(sortedIndices, (a, b) => string.Compare(FieldCaption[a] ?? "", FieldCaption[b] ?? "", StringComparison.OrdinalIgnoreCase));
                //
                // -- build view model for the Mustache body layout
                var viewModel = new AdvancedSearchViewModel();
                for (int sortIdx = 0; sortIdx < FieldCnt; sortIdx++) {
                    int ptr = sortedIndices[sortIdx];
                    int matchOption = FieldMatchOptions[ptr];
                    string radioName = $"FieldMatch{ptr}";
                    var fieldVm = new AdvancedSearchFieldViewModel {
                        caption = FieldCaption[ptr]
                    };
                    switch (fieldTypeId[ptr]) {
                        case CPContentBaseClass.FieldTypeIdEnum.Date:
                            //
                            // -- Date: ignore, empty, not empty, =, >, <
                            fieldVm.radioOptions = new List<AdvancedSearchRadioOption> {
                                newRadio(radioName, FindWordMatchEnum.MatchIgnore, matchOption, "ignore"),
                                newRadio(radioName, FindWordMatchEnum.MatchEmpty, matchOption, "empty"),
                                newRadio(radioName, FindWordMatchEnum.MatchNotEmpty, matchOption, "not empty"),
                                newRadio(radioName, FindWordMatchEnum.MatchEquals, matchOption, "="),
                                newRadio(radioName, FindWordMatchEnum.MatchGreaterThan, matchOption, "&gt;"),
                                newRadio(radioName, FindWordMatchEnum.MatchLessThan, matchOption, "&lt;")
                            };
                            fieldVm.hasValueInput = true;
                            fieldVm.valueInputHtml = HtmlController.inputDate(core, $"fieldvalue{ptr}", getDate(FieldValue[ptr]));
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Currency:
                        case CPContentBaseClass.FieldTypeIdEnum.Float:
                        case CPContentBaseClass.FieldTypeIdEnum.Integer:
                        case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                            //
                            // -- Numeric: ignore, empty, not empty, =, >, <
                            string numericRadioId = $"n{ptr}";
                            fieldVm.radioOptions = new List<AdvancedSearchRadioOption> {
                                newRadio(radioName, FindWordMatchEnum.MatchIgnore, matchOption, "ignore"),
                                newRadio(radioName, FindWordMatchEnum.MatchEmpty, matchOption, "empty"),
                                newRadio(radioName, FindWordMatchEnum.MatchNotEmpty, matchOption, "not empty"),
                                newRadio(radioName, FindWordMatchEnum.matchincludes, matchOption, "=", numericRadioId),
                                newRadio(radioName, FindWordMatchEnum.MatchGreaterThan, matchOption, "&gt;"),
                                newRadio(radioName, FindWordMatchEnum.MatchLessThan, matchOption, "&lt;")
                            };
                            fieldVm.hasValueInput = true;
                            fieldVm.valueInputHtml = HtmlController.inputText_Legacy(core, $"fieldvalue{ptr}", FieldValue[ptr], 1, 5, "", false, false, "form-control ccAdvSearchText")
                                .Replace(">", $" onFocus=\"var e=getElementById('{numericRadioId}');e.checked=1;\">");
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.File:
                        case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                            //
                            // -- File: ignore, empty, not empty
                            fieldVm.radioOptions = new List<AdvancedSearchRadioOption> {
                                newRadio(radioName, FindWordMatchEnum.MatchIgnore, matchOption, "ignore"),
                                newRadio(radioName, FindWordMatchEnum.MatchEmpty, matchOption, "empty"),
                                newRadio(radioName, FindWordMatchEnum.MatchNotEmpty, matchOption, "not empty")
                            };
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Boolean:
                            //
                            // -- Boolean: ignore, true, false
                            fieldVm.radioOptions = new List<AdvancedSearchRadioOption> {
                                newRadio(radioName, FindWordMatchEnum.MatchIgnore, matchOption, "ignore"),
                                newRadio(radioName, FindWordMatchEnum.MatchTrue, matchOption, "true"),
                                newRadio(radioName, FindWordMatchEnum.MatchFalse, matchOption, "false")
                            };
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Text:
                        case CPContentBaseClass.FieldTypeIdEnum.LongText:
                        case CPContentBaseClass.FieldTypeIdEnum.HTML:
                        case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                        case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                        case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                        case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                        case CPContentBaseClass.FieldTypeIdEnum.FileJavaScript:
                        case CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                                //
                                // -- Text: ignore, empty, not empty, includes
                                string textRadioId = $"t{ptr}";
                                fieldVm.radioOptions = new List<AdvancedSearchRadioOption> {
                                    newRadio(radioName, FindWordMatchEnum.MatchIgnore, matchOption, "ignore"),
                                    newRadio(radioName, FindWordMatchEnum.MatchEmpty, matchOption, "empty"),
                                    newRadio(radioName, FindWordMatchEnum.MatchNotEmpty, matchOption, "not empty"),
                                    newRadio(radioName, FindWordMatchEnum.matchincludes, matchOption, "includes", textRadioId)
                                };
                                fieldVm.hasValueInput = true;
                                fieldVm.valueInputHtml = HtmlController.inputText_Legacy(core, $"fieldvalue{ptr}", FieldValue[ptr], 1, 5, "", false, false, "form-control ccAdvSearchText")
                                    .Replace(">", $" onFocus=\"var e=getElementById('{textRadioId}');e.checked=1;\">");
                                break;
                            }
                        case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                        case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                //
                                // -- Lookup: ignore, empty, not empty, includes
                                string lookupRadioId = $"t{ptr}";
                                fieldVm.radioOptions = new List<AdvancedSearchRadioOption> {
                                    newRadio(radioName, FindWordMatchEnum.MatchIgnore, matchOption, "ignore"),
                                    newRadio(radioName, FindWordMatchEnum.MatchEmpty, matchOption, "empty"),
                                    newRadio(radioName, FindWordMatchEnum.MatchNotEmpty, matchOption, "not empty"),
                                    newRadio(radioName, FindWordMatchEnum.matchincludes, matchOption, "includes", lookupRadioId)
                                };
                                fieldVm.hasValueInput = true;
                                fieldVm.valueInputHtml = HtmlController.inputText_Legacy(core, $"fieldvalue{ptr}", FieldValue[ptr], 1, 5, "", false, false, "form-control ccAdvSearchText")
                                    .Replace(">", $" onFocus=\"var e=getElementById('{lookupRadioId}');e.checked=1;\">");
                                break;
                            }
                        default:
                            continue;
                    }
                    viewModel.fields.Add(fieldVm);
                }
                //
                // -- render the body from the Mustache layout
                string bodyLayout = cp.Layout.GetLayout(
                    Constants.guidLayoutAdvancedSearchBody,
                    Constants.nameLayoutAdvancedSearchBody,
                    Constants.pathFilenameLayoutAdvancedSearchBody
                );
                string bodyHtml = cp.Mustache.Render(bodyLayout, viewModel);
                //
                // -- build the page using LayoutBuilder
                var layoutBuilder = cp.AdminUI.CreateLayoutBuilder();
                layoutBuilder.title = $"{adminData.adminContent.name} Advanced Search";
                layoutBuilder.description = "Enter criteria for each field to identify and select your results. The results of a search will have to have all of the criteria you enter.";
                layoutBuilder.isOuterContainer = true;
                layoutBuilder.body = bodyHtml;
                //
                // -- add form buttons
                layoutBuilder.addFormButton(ButtonCancel);
                layoutBuilder.addFormButton(ButtonSearch);
                layoutBuilder.addClientButton("Reset", "", "btnAdvSearchReset");
                //
                // -- add hidden fields for form posting
                layoutBuilder.addFormHidden("fieldcnt", FieldCnt);
                layoutBuilder.addFormHidden(rnAdminSourceForm, AdminFormList_AdvancedSearch);
                for (int sortIdx = 0; sortIdx < FieldCnt; sortIdx++) {
                    int ptr = sortedIndices[sortIdx];
                    layoutBuilder.addFormHidden($"fieldname{ptr}", FieldNames[ptr]);
                }
                //
                core.html.addTitle($"{adminData.adminContent.name} Advanced Search", "admin advanced search");
                return layoutBuilder.getHtml();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //=================================================================================
        /// <summary>
        /// Create a radio option view model entry
        /// </summary>
        private static AdvancedSearchRadioOption newRadio(string radioName, FindWordMatchEnum matchEnum, int currentMatchOption, string label, string radioId = "") {
            return new AdvancedSearchRadioOption {
                name = radioName,
                value = ((int)matchEnum).ToString(),
                isChecked = (int)matchEnum == currentMatchOption,
                label = label,
                radioId = radioId
            };
        }
    }
}
