
using System;
using System.Collections.Generic;
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
        /// list view advanced search
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <returns></returns>
        public static string get(CPClass cp, CoreController core, AdminDataModel adminData) {
            string returnForm = "";
            try {
                //
                string SearchValue = null;
                FindWordMatchEnum MatchOption = 0;
                int FormFieldPtr = 0;
                int FormFieldCnt = 0;
                ContentMetadataModel CDef = null;
                string FieldName = null;
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                int FieldPtr = 0;
                bool RowEven = false;
                string RQS = null;
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
                int RowPointer = 0;
                string LeftButtons = "";
                string ButtonBar = null;
                string Title = null;
                string TitleBar = null;
                string Content = null;

                //
                // Process last form
                //
                string Button = core.docProperties.getText("button");
                GridConfigClass gridConfig = null;
                if (!string.IsNullOrEmpty(Button)) {
                    switch (Button) {
                        case ButtonSearch:
                            gridConfig = new(core, adminData);
                            FormFieldCnt = core.docProperties.getInteger("fieldcnt");
                            if (FormFieldCnt > 0) {
                                for (FormFieldPtr = 0; FormFieldPtr < FormFieldCnt; FormFieldPtr++) {
                                    FieldName = GenericController.toLCase(core.docProperties.getText("fieldname" + FormFieldPtr));
                                    MatchOption = (FindWordMatchEnum)core.docProperties.getInteger("FieldMatch" + FormFieldPtr);
                                    switch (MatchOption) {
                                        case FindWordMatchEnum.MatchEquals:
                                        case FindWordMatchEnum.MatchGreaterThan:
                                        case FindWordMatchEnum.matchincludes:
                                        case FindWordMatchEnum.MatchLessThan:
                                            SearchValue = core.docProperties.getText("FieldValue" + FormFieldPtr);
                                            break;
                                        default:
                                            SearchValue = "";
                                            break;
                                    }
                                    if (!gridConfig.findWords.ContainsKey(FieldName)) {
                                        //
                                        // fieldname not found, save if not FindWordMatchEnum.MatchIgnore
                                        //
                                        if (MatchOption != FindWordMatchEnum.MatchIgnore) {
                                            gridConfig.findWords.Add(FieldName, new GridConfigFindWordClass {
                                                Name = FieldName,
                                                MatchOption = MatchOption,
                                                Value = SearchValue
                                            });
                                        }
                                    } else {
                                        //
                                        // fieldname was found
                                        //
                                        gridConfig.findWords[FieldName].MatchOption = MatchOption;
                                        gridConfig.findWords[FieldName].Value = SearchValue;
                                    }
                                }
                            }
                            AdminContentController.setIndexSQL_SaveIndexConfig(cp, core, gridConfig);
                            return string.Empty;
                        case ButtonCancel:
                            return string.Empty;
                    }
                }
                gridConfig = new(core, adminData);
                Button = "CriteriaSelect";
                RQS = core.doc.refreshQueryString;
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
                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
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
                // Add headers to stream
                //
                returnForm = returnForm + "<table border=0 width=100% cellspacing=0 cellpadding=4>";
                //
                RowPointer = 0;
                for (FieldPtr = 0; FieldPtr < FieldCnt; FieldPtr++) {
                    returnForm = returnForm + HtmlController.inputHidden("fieldname" + FieldPtr, FieldNames[FieldPtr]);
                    RowEven = ((RowPointer % 2) == 0);
                    FieldMatchOption = FieldMatchOptions[FieldPtr];
                    switch (fieldTypeId[FieldPtr]) {
                        case CPContentBaseClass.FieldTypeIdEnum.Date:
                            //
                            // Date

                            returnForm = returnForm + "<tr>"
                                + "<td class=\"ccAdminEditCaption align-middle p-2\">" + FieldCaption[FieldPtr] + "</td>"
                                + "<td class=\"ccAdminEditField p-2\">"
                                + "<div style=\"display:block;float:left;width:800px;\">"
                                + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "m-2") + "ignore</div>"
                                + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "empty</div>"
                                + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "not&nbsp;empty</div>"
                                + "<div style=\"display:block;float:left;width:50px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEquals).ToString(), FieldMatchOption.ToString(), "", "m-2") + "=</div>"
                                + "<div style=\"display:block;float:left;width:50px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchGreaterThan).ToString(), FieldMatchOption.ToString(), "", "m-2") + "&gt;</div>"
                                + "<div style=\"display:block;float:left;width:50px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchLessThan).ToString(), FieldMatchOption.ToString(), "", "m-2") + "&lt;</div>"
                                + "<div style=\"display:block;float:left;width:300px;\">" + HtmlController.inputDate(core, "fieldvalue" + FieldPtr, encodeDate(FieldValue[FieldPtr])).Replace(">", " onFocus=\"ccAdvSearchText\">") + "</div>"
                                + "</div>"
                                + "</td>"
                                + "</tr>";
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Currency:
                        case CPContentBaseClass.FieldTypeIdEnum.Float:
                        case CPContentBaseClass.FieldTypeIdEnum.Integer:
                        case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                            //
                            // -- Numeric - changed FindWordMatchEnum.MatchEquals to MatchInclude to be compatible with Find Search
                            returnForm = returnForm + "<tr>"
                            + "<td class=\"ccAdminEditCaption align-middle p-2\">" + FieldCaption[FieldPtr] + "</td>"
                            + "<td class=\"ccAdminEditField p-2\">"
                            + "<div style=\"display:block;float:left;width:800px;\">"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "m-2") + "ignore</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "empty</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "not&nbsp;empty</div>"
                            + "<div style=\"display:block;float:left;width:50px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.matchincludes).ToString(), FieldMatchOption.ToString(), "n" + FieldPtr) + "=</div>"
                            + "<div style=\"display:block;float:left;width:50px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchGreaterThan).ToString(), FieldMatchOption.ToString(), "", "m-2") + "&gt;</div>"
                            + "<div style=\"display:block;float:left;width:50px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchLessThan).ToString(), FieldMatchOption.ToString(), "", "m-2") + "&lt;</div>"
                            + "<div style=\"display:block;float:left;width:300px;\">" + HtmlController.inputText_Legacy(core, "fieldvalue" + FieldPtr, FieldValue[FieldPtr], 1, 5, "", false, false, "ccAdvSearchText").Replace(">", " onFocus=\"var e=getElementById('n" + FieldPtr + "');e.checked=1;\">") + "</div>"
                            + "</div>"
                            + "</td>"
                            + "</tr>";
                            RowPointer += 1;
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.File:
                        case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                            //
                            // File
                            //
                            returnForm = returnForm + "<tr>"
                            + "<td class=\"ccAdminEditCaption align-middle p-2\">" + FieldCaption[FieldPtr] + "</td>"
                            + "<td class=\"ccAdminEditField p-2\">"
                            + "<div style=\"display:block;float:left;width:800px;\">"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "m-2") + "ignore</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "empty</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "not&nbsp;empty</div>"
                            + "</div>"
                            + "</td>"
                            + "</tr>";
                            RowPointer = RowPointer + 1;
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Boolean:
                            //
                            // Boolean
                            //
                            returnForm = returnForm + "<tr>"
                            + "<td class=\"ccAdminEditCaption align-middle p-2\">" + FieldCaption[FieldPtr] + "</td>"
                            + "<td class=\"ccAdminEditField p-2\">"
                            + "<div style=\"display:block;float:left;width:800px;\">"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "m-2") + "ignore</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchTrue).ToString(), FieldMatchOption.ToString(), "", "m-2") + "true</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchFalse).ToString(), FieldMatchOption.ToString(), "", "m-2") + "false</div>"
                            + "</div>"
                            + "</td>"
                            + "</tr>";
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
                            // Text
                            //
                            returnForm = returnForm + "<tr>"
                            + "<td class=\"ccAdminEditCaption align-middle p-2\">" + FieldCaption[FieldPtr] + "</td>"
                            + "<td class=\"ccAdminEditField p-2\">"
                            + "<div style=\"display:block;float:left;width:800px;\">"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "m-2") + "ignore</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "empty</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "not&nbsp;empty</div>"
                            + "<div style=\"display:block;float:left;width:150px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.matchincludes).ToString(), FieldMatchOption.ToString(), "t" + FieldPtr, "m-2") + "includes</div>"
                            + "<div style=\"display:block;float:left;width:300px;\">" + HtmlController.inputText_Legacy(core, "fieldvalue" + FieldPtr, FieldValue[FieldPtr], 1, 5, "", false, false, "ccAdvSearchText").Replace(">", " onFocus=\"var e=getElementById('t" + FieldPtr + "');e.checked=1;\">") + "</div>"
                            + "</div>"
                            + "</td>"
                            + "</tr>";
                            RowPointer = RowPointer + 1;
                            break;
                        case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                        case CPContentBaseClass.FieldTypeIdEnum.MemberSelect:
                            //
                            // Lookup
                            returnForm = returnForm + "<tr>"
                            + "<td class=\"ccAdminEditCaption align-middle p-2\">" + FieldCaption[FieldPtr] + "</td>"
                            + "<td class=\"ccAdminEditField p-2\">"
                            + "<div style=\"display:block;float:left;width:800px;\">"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchIgnore).ToString(), FieldMatchOption.ToString(), "", "m-2") + "ignore</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "empty</div>"
                            + "<div style=\"display:block;float:left;width:100px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.MatchNotEmpty).ToString(), FieldMatchOption.ToString(), "", "m-2") + "not&nbsp;empty</div>"
                            + "<div style=\"display:block;float:left;width:150px;\">" + HtmlController.inputRadio("FieldMatch" + FieldPtr, ((int)FindWordMatchEnum.matchincludes).ToString(), FieldMatchOption.ToString(), "t" + FieldPtr, "m-2") + "includes</div>"
                            + "<div style=\"display:block;float:left;width:300px;\">" + HtmlController.inputText_Legacy(core, "fieldvalue" + FieldPtr, FieldValue[FieldPtr], 1, 5, "", false, false, "ccAdvSearchText").Replace(">", " onFocus=\"var e=getElementById('t" + FieldPtr + "'); e.checked= 1;\">") + "</div>"
                            + "</div>"
                            + "</td>"
                            + "</tr>";
                            RowPointer = RowPointer + 1;
                            break;
                    }
                }
                returnForm = returnForm + HtmlController.tableRowStart();
                returnForm = returnForm + HtmlController.tableCellStart("120", 1, RowEven, "right") + "<img src=" + cdnPrefix + "images/spacer.gif width=120 height=1></td>";
                returnForm = returnForm + HtmlController.tableCellStart("99%", 1, RowEven, "left") + "<img src=" + cdnPrefix + "images/spacer.gif width=1 height=1></td>";
                returnForm = returnForm + kmaEndTableRow;
                returnForm = returnForm + "</table>";
                Content = returnForm;
                //
                // Assemble LiveWindowTable
                Stream.add(ButtonBar);
                Stream.add(TitleBar);
                Stream.add(Content);
                Stream.add(ButtonBar);
                Stream.add("<input type=hidden name=fieldcnt VALUE=" + FieldCnt + ">");
                Stream.add("<input type=hidden name=" + RequestNameAdminSubForm + " VALUE=" + AdminFormList_AdvancedSearch + ">");
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
