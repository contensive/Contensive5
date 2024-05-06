
using Contensive.BaseClasses;
using Contensive.CPBase.BaseModels;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.BaseClasses.CPContentBaseClass;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Tools used to assemble html document elements. This is not a storage for assembling a document (see docController)
    /// </summary>
    public class HtmlController {
        //
        private readonly CoreController core;
        //
        // ====================================================================================================
        /// <summary>
        /// tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public static string a(string innerHtml, HtmlAttributesA attributes) {
            StringBuilder result = new StringBuilder("<a");
            result.Append((string.IsNullOrWhiteSpace(attributes.download)) ? "" : "" + " download=\"" + encodeHtml(attributes.download) + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.href)) ? "" : "" + " href=\"" + encodeHtml(attributes.href) + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.hreflang)) ? "" : "" + " href=\"" + encodeHtml(attributes.hreflang) + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.media)) ? "" : "" + " media=\"" + encodeHtml(attributes.media) + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ping)) ? "" : "" + " ping=\"" + encodeHtml(attributes.ping) + "\"");
            switch (attributes.referrerpolicy) {
                case HtmlAttributesA.HtmlAttributeReferrerPolicy.no_referrer: {
                        result.Append(" referrerpolicy=\"no-referrer\"");
                        break;
                    }
                case HtmlAttributesA.HtmlAttributeReferrerPolicy.no_referrer_when_downgrade: {
                        result.Append(" referrerpolicy=\"no-referrer-when-downgrade\"");
                        break;
                    }
                case HtmlAttributesA.HtmlAttributeReferrerPolicy.origin: {
                        result.Append(" referrerpolicy=\"origin\"");
                        break;
                    }
                case HtmlAttributesA.HtmlAttributeReferrerPolicy.origin_when_cross_origin: {
                        result.Append(" referrerpolicy=\"origin-when-cross-origin\"");
                        break;
                    }
                case HtmlAttributesA.HtmlAttributeReferrerPolicy.unsafe_url: {
                        result.Append(" referrerpolicy=\"unsafe-url\"");
                        break;
                    }
                default: {
                        break;
                    }
            }
            result.Append((attributes.rel.Equals(HtmlAttributesA.HtmlAttributeRel.none)) ? "" : "" + " ping=\"" + encodeHtml(attributes.rel.ToString()) + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.target)) ? "" : "" + " ping=\"" + encodeHtml(attributes.target) + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.type)) ? "" : "" + " ping=\"" + encodeHtml(attributes.type) + "\"");
            result.Append(getHtmlAttributesGlobal(attributes));
            return result + ">" + innerHtml + "</a>";
        }
        /// <summary>
        /// tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="href"></param>
        /// <returns></returns>
        public static string a(string innerHtml, string href) => a(innerHtml, href, "", "", "", "");
        /// <summary>
        /// tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="href"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public static string a(string innerHtml, string href, string htmlClass) => a(innerHtml, href, htmlClass, "", "", "");
        /// <summary>
        /// tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="href"></param>
        /// <param name="htmlClass"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public static string a(string innerHtml, string href, string htmlClass, string htmlId) => a(innerHtml, href, htmlClass, htmlId, "", "");
        /// <summary>
        /// tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="href"></param>
        /// <param name="htmlClass"></param>
        /// <param name="htmlId"></param>
        /// <param name="tabIndex"></param>
        /// <returns></returns>
        public static string a(string innerHtml, string href, string htmlClass, string htmlId, string tabIndex) => a(innerHtml, href, htmlClass, htmlId, tabIndex, "");
        /// <summary>
        /// tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="href"></param>
        /// <param name="htmlClass"></param>
        /// <param name="htmlId"></param>
        /// <param name="tabIndex"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string a(string innerHtml, string href, string htmlClass, string htmlId, string tabIndex, string target) {
            var tag = new StringBuilder("<a");
            if (!String.IsNullOrWhiteSpace(href)) { tag.Append(" href=\"").Append(href).Append("\""); }
            if (!String.IsNullOrWhiteSpace(htmlClass)) { tag.Append(" class=\"").Append(htmlClass).Append("\""); }
            if (!String.IsNullOrWhiteSpace(htmlId)) { tag.Append(" id=\"").Append(htmlId).Append("\""); }
            if (!String.IsNullOrWhiteSpace(tabIndex)) { tag.Append(" tabindex=\"").Append(tabIndex).Append("\""); }
            if (!String.IsNullOrWhiteSpace(target)) { tag.Append(" target=\"").Append(target).Append("\""); }
            tag.Append(">").Append(innerHtml).Append("</a>");
            return tag.ToString();
        }
        //
        //====================================================================================================
        /// <summary>
        /// convert html to text, simulating whitespace
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceHtml"></param>
        /// <returns></returns>
        public static string convertHtmlToText(CoreController core, string sourceHtml) {
            if (string.IsNullOrEmpty(sourceHtml)) { return ""; }
            if (!sourceHtml.Contains("<")) { return sourceHtml; }
            return NUglify.Uglify.HtmlToText("<div>" + sourceHtml + "</div>", NUglify.Html.HtmlToTextOptions.KeepStructure).Code.Trim();
        }
        //
        //====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core"></param>
        /// <remarks></remarks>
        public HtmlController(CoreController core) {
            this.core = core;
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="AllowLogin"></param>
        /// <param name="AllowTools"></param>
        /// <returns></returns>
        public string getHtmlBodyEnd(bool AllowLogin, bool AllowTools) {
            List<string> result = new();
            try {
                if (AllowLogin) {
                    result.Add(getLoginLink());
                }
                //
                // -- body Javascript
                bool allowDebugging = core.visitProperty.getBoolean("AllowDebugging");
                var scriptOnLoad = new List<string>();
                foreach (var asset in core.doc.htmlAssetList.FindAll((a) => ((a.assetType == CPDocBaseClass.HtmlAssetTypeEnum.script) || (a.assetType == CPDocBaseClass.HtmlAssetTypeEnum.scriptOnLoad)) && (!a.inHead) && (!string.IsNullOrEmpty(a.content)))) {
                    if (string.IsNullOrEmpty(asset.content)) { continue; }
                    if (!string.IsNullOrEmpty(asset.addedByMessage) && allowDebugging) {
                        result.Add(Environment.NewLine + "<!-- from " + asset.addedByMessage + " -->");
                    }
                    if (asset.assetType == CPDocBaseClass.HtmlAssetTypeEnum.scriptOnLoad) {
                        scriptOnLoad.Add(asset.content + ";");
                    }
                    if (!asset.isLink) {
                        result.Add(Environment.NewLine + "<script Language=\"JavaScript\" type=\"text/javascript\">" + asset.content + "</script>");
                    } else {
                        if (asset.content.Trim().Substring(0, 1) == "<") {
                            result.Add(Environment.NewLine + asset.content);
                        } else {
                            result.Add(Environment.NewLine + "<script type=\"text/javascript\" src=\"" + asset.content + "\"></script>");
                        }
                    }
                }
                if (scriptOnLoad.Count > 0) {
                    result.Add(""
                        + Environment.NewLine + "<script Language=\"JavaScript\" type=\"text/javascript\">"
                        + "function ready(callback){"
                            + "if (document.readyState!='loading') callback(); "
                            + "else if (document.addEventListener) document.addEventListener('DOMContentLoaded', callback); "
                            + "else document.attachEvent('onreadystatechange', function(){"
                                + "if (document.readyState=='complete') callback();"
                            + "});"
                        + "} ready(function(){" + string.Join(Environment.NewLine, scriptOnLoad) + Environment.NewLine + "});"
                        + "</script>");

                }
                //
                // -- Include any other close page
                if (core.doc.htmlForEndOfBody != "") {
                    result.Add(core.doc.htmlForEndOfBody);
                }
                if (core.doc.testPointMessage != "") {
                    result.Add("<div class=\"ccTestPointMessageCon\">" + core.doc.testPointMessage + "</div>");
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return string.Join("\r", result);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Select list from a content
        /// </summary>
        /// <param name="MenuName"></param>
        /// <param name="CurrentValue"></param>
        /// <param name="ContentName"></param>
        /// <param name="Criteria"></param>
        /// <param name="NoneCaption"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public string selectFromContent(string MenuName, int CurrentValue, string ContentName, string Criteria = "", string NoneCaption = "", string htmlId = "") {
            bool tempVar = false;
            return selectFromContent(MenuName, CurrentValue, ContentName, Criteria, NoneCaption, htmlId, ref tempVar, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Select list from a content
        /// </summary>
        /// <param name="MenuName"></param>
        /// <param name="CurrentValue"></param>
        /// <param name="ContentName"></param>
        /// <param name="lookupContentEditor"></param>
        /// <param name="NoneCaption"></param>
        /// <param name="htmlId"></param>
        /// <param name="return_IsEmptyList"></param>
        /// <param name="HtmlClass"></param>
        /// <returns></returns>
        public string selectFromContent(string MenuName, int CurrentValue, string ContentName, string lookupContentEditor, string NoneCaption, string htmlId, ref bool return_IsEmptyList, string HtmlClass = "") {
            string result = "";
            try {
                const string MenuNameFPO = "<MenuName>";
                const string NoneCaptionFPO = "<NoneCaption>";
                string LcaseCriteria = toLCase(lookupContentEditor);
                return_IsEmptyList = true;
                if (string.IsNullOrEmpty(MenuName)) { return $"Selection html name cannot be blank."; }
                if (string.IsNullOrEmpty(ContentName)) { return $"Selection content cannot be blank."; }
                //
                string CurrentValueText = CurrentValue.ToString();
                string SelectRaw = "";
                foreach (CacheInputSelectClass inputSelect in core.doc.inputSelectCache) {
                    if ((inputSelect.contentName == ContentName) && (inputSelect.criteria == null) && (inputSelect.currentValue == CurrentValueText)) {
                        SelectRaw = inputSelect.selectRaw;
                        return_IsEmptyList = false;
                        break;
                    }
                }
                //
                if (string.IsNullOrEmpty(SelectRaw)) {
                    //
                    // Build the SelectRaw, Test selection size
                    ContentMetadataModel metaData = ContentMetadataModel.createByUniqueName(core, ContentName);
                    if(metaData is null ) {
                        return $"Selection content [{ContentName}] is not valid.";
                    }
                    string ContentControlCriteria = metaData.legacyContentControlCriteria;
                    string SQL = "select count(*) as cnt from " + metaData.tableName + " where " + ContentControlCriteria;
                    if (!string.IsNullOrEmpty(null)) {
                        SQL += " and " + null;
                    }
                    DataTable dt = core.db.executeQuery(SQL);
                    int RowCnt = 0;
                    if (dt.Rows.Count > 0) {
                        RowCnt = GenericController.encodeInteger(dt.Rows[0]["cnt"]);
                    }
                    int RowMax = 0;
                    if (RowCnt == 0) {
                        RowMax = -1;
                    } else {
                        return_IsEmptyList = false;
                        RowMax = RowCnt - 1;
                    }
                    //
                    if (RowCnt > core.siteProperties.selectFieldLimit) {
                        //
                        // -- Selection is too big
                        ErrorController.addUserError(core, "The drop down list for " + ContentName + " called " + MenuName + " is too long to display. The site administrator has been notified and the problem will be resolved shortly. To fix this issue temporarily, go to the admin tab of the Preferences page and set the Select Field Limit larger than " + RowCnt + ".");
                        logger.Error($"{core.logCommonMessage}", new Exception("Error creating select list from content [" + ContentName + "] called [" + MenuName + "]. Selection of [" + RowCnt + "] records exceeds [" + core.siteProperties.selectFieldLimit + "], the current Site Property SelectFieldLimit."));
                        result += inputHidden(MenuNameFPO, CurrentValue);
                        if (CurrentValue == 0) {
                            result = inputText_Legacy(core, MenuName, "0");
                        } else {
                            using (var csData = new CsModel(core)) {
                                if (csData.openRecord(ContentName, CurrentValue)) {
                                    result = csData.getText("name") + "&nbsp;";
                                }
                            }
                        }
                        result += "(Selection is too large to display option list)";
                    } else {
                        //
                        // -- Generate Drop Down Field Names
                        string DropDownFieldList = metaData.dropDownFieldList;
                        if (string.IsNullOrEmpty(DropDownFieldList)) { DropDownFieldList = "NAME"; }
                        int DropDownFieldCount = 0;
                        string CharAllowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                        int DropDownFieldListLength = DropDownFieldList.Length;
                        string[] DropDownDelimiter = { };
                        string[] DropDownFieldName = { };
                        string DropDownPreField = "";
                        string FieldName = "";
                        int CharPointer = 0;
                        string SortFieldList = "";
                        for (CharPointer = 1; CharPointer <= DropDownFieldListLength; CharPointer++) {
                            string CharTest = DropDownFieldList.Substring(CharPointer - 1, 1);
                            if (GenericController.strInstr(1, CharAllowed, CharTest) == 0) {
                                //
                                // Character not allowed, delimit Field name here
                                //
                                if (!string.IsNullOrEmpty(FieldName)) {
                                    //
                                    // ----- main_Get new Field Name and save it
                                    //
                                    if (string.IsNullOrEmpty(SortFieldList)) {
                                        SortFieldList = FieldName;
                                    }
                                    Array.Resize(ref DropDownFieldName, DropDownFieldCount + 1);
                                    Array.Resize(ref DropDownDelimiter, DropDownFieldCount + 1);
                                    DropDownFieldName[DropDownFieldCount] = FieldName;
                                    DropDownDelimiter[DropDownFieldCount] = CharTest;
                                    DropDownFieldCount = DropDownFieldCount + 1;
                                    FieldName = "";
                                } else {
                                    //
                                    // ----- Save Field Delimiter
                                    //
                                    if (DropDownFieldCount == 0) {
                                        //
                                        // ----- Before any field, add to DropDownPreField
                                        //
                                        DropDownPreField = DropDownPreField + CharTest;
                                    } else {
                                        //
                                        // ----- after a field, add to last DropDownDelimiter
                                        //
                                        DropDownDelimiter[DropDownFieldCount - 1] = DropDownDelimiter[DropDownFieldCount - 1] + CharTest;
                                    }
                                }
                            } else {
                                //
                                // Character Allowed, Put character into fieldname and continue
                                //
                                FieldName = FieldName + CharTest;
                            }
                        }
                        if (!string.IsNullOrEmpty(FieldName)) {
                            if (string.IsNullOrEmpty(SortFieldList)) {
                                SortFieldList = FieldName;
                            }
                            Array.Resize(ref DropDownFieldName, DropDownFieldCount + 1);
                            Array.Resize(ref DropDownDelimiter, DropDownFieldCount + 1);
                            DropDownFieldName[DropDownFieldCount] = FieldName;
                            DropDownDelimiter[DropDownFieldCount] = "";
                            DropDownFieldCount = DropDownFieldCount + 1;
                        }
                        if (DropDownFieldCount == 0) {
                            logger.Error($"{core.logCommonMessage}", new Exception("No drop down field names found for content [" + ContentName + "]."));
                        } else {
                            int[] DropDownFieldPointer = new int[DropDownFieldCount];
                            string SelectFields = "ID";
                            int Ptr = 0;
                            for (Ptr = 0; Ptr < DropDownFieldCount; Ptr++) {
                                SelectFields = SelectFields + "," + DropDownFieldName[Ptr];
                            }
                            //
                            // ----- Start select box
                            //
                            string TagId = "";
                            if (!string.IsNullOrEmpty(htmlId)) {
                                TagId = " ID=\"" + htmlId + "\"";
                            }
                            StringBuilderLegacyController FastString = new StringBuilderLegacyController();
                            FastString.add("<select size=\"1\" name=\"" + MenuNameFPO + "\"" + TagId + ">");
                            FastString.add("<option value=\"\">" + NoneCaptionFPO + "</option>");
                            //
                            // ----- select values
                            using (var csData = new CsModel(core)) {
                                if (csData.open(ContentName, lookupContentEditor, SortFieldList, false, 0, SelectFields)) {
                                    string[,] RowsArray = csData.getRows();
                                    string[] RowFieldArray = csData.getSelectFieldList().Split(',');
                                    int ColumnMax = RowsArray.GetUpperBound(0);
                                    RowMax = RowsArray.GetUpperBound(1);
                                    //
                                    // -- setup IDFieldPointer
                                    string UcaseFieldName = "ID";
                                    int IDFieldPointer = 0;
                                    int ColumnPointer = 0;
                                    for (ColumnPointer = 0; ColumnPointer <= ColumnMax; ColumnPointer++) {
                                        if (UcaseFieldName == GenericController.toUCase(RowFieldArray[ColumnPointer])) {
                                            IDFieldPointer = ColumnPointer;
                                            break;
                                        }
                                    }
                                    //
                                    // setup DropDownFieldPointer()
                                    //
                                    for (var FieldPointer = 0; FieldPointer < DropDownFieldCount; FieldPointer++) {
                                        UcaseFieldName = GenericController.toUCase(DropDownFieldName[FieldPointer]);
                                        for (ColumnPointer = 0; ColumnPointer <= ColumnMax; ColumnPointer++) {
                                            if (UcaseFieldName == GenericController.toUCase(RowFieldArray[ColumnPointer])) {
                                                DropDownFieldPointer[FieldPointer] = ColumnPointer;
                                                break;
                                            }
                                        }
                                    }
                                    //
                                    // output select
                                    //
                                    bool SelectedFound = false;
                                    string Copy = null;
                                    int RowPointer = 0;
                                    int RecordID = 0;
                                    for (RowPointer = 0; RowPointer <= RowMax; RowPointer++) {
                                        RecordID = GenericController.encodeInteger(RowsArray[IDFieldPointer, RowPointer]);
                                        Copy = DropDownPreField;
                                        for (var FieldPointer = 0; FieldPointer < DropDownFieldCount; FieldPointer++) {
                                            Copy += RowsArray[DropDownFieldPointer[FieldPointer], RowPointer] + DropDownDelimiter[FieldPointer];
                                        }
                                        if (string.IsNullOrEmpty(Copy)) {
                                            Copy = "no name";
                                        }
                                        FastString.add(Environment.NewLine + "<option value=\"" + RecordID + "\" ");
                                        if (RecordID == CurrentValue) {
                                            FastString.add("selected");
                                            SelectedFound = true;
                                        }
                                        if (core.siteProperties.selectFieldWidthLimit != 0) {
                                            if (Copy.Length > core.siteProperties.selectFieldWidthLimit) {
                                                Copy = Copy.left(core.siteProperties.selectFieldWidthLimit) + "...+";
                                            }
                                        }
                                        FastString.add(">" + HtmlController.encodeHtml(Copy) + "</option>");
                                    }
                                    if (!SelectedFound && (CurrentValue != 0)) {
                                        csData.close();
                                        if (!string.IsNullOrEmpty(lookupContentEditor)) {
                                            lookupContentEditor = lookupContentEditor + "and";
                                        }
                                        lookupContentEditor = lookupContentEditor + "(id=" + GenericController.encodeInteger(CurrentValue) + ")";
                                        if (csData.open(ContentName, lookupContentEditor, SortFieldList, false, 0, SelectFields)) {
                                            RowsArray = csData.getRows();
                                            RowFieldArray = csData.getSelectFieldList().Split(',');
                                            RowMax = RowsArray.GetUpperBound(1);
                                            ColumnMax = RowsArray.GetUpperBound(0);
                                            RecordID = GenericController.encodeInteger(RowsArray[IDFieldPointer, 0]);
                                            Copy = DropDownPreField;
                                            for (var FieldPointer = 0; FieldPointer < DropDownFieldCount; FieldPointer++) {
                                                Copy += RowsArray[DropDownFieldPointer[FieldPointer], 0] + DropDownDelimiter[FieldPointer];
                                            }
                                            if (string.IsNullOrEmpty(Copy)) {
                                                Copy = "no name";
                                            }
                                            FastString.add(Environment.NewLine + "<option value=\"" + RecordID + "\" selected");
                                            SelectedFound = true;
                                            if (core.siteProperties.selectFieldWidthLimit != 0) {
                                                if (Copy.Length > core.siteProperties.selectFieldWidthLimit) {
                                                    Copy = Copy.left(core.siteProperties.selectFieldWidthLimit) + "...+";
                                                }
                                            }
                                            FastString.add(">" + HtmlController.encodeHtml(Copy) + "</option>");
                                        }
                                    }
                                }
                            }
                            FastString.add("</select>");
                            SelectRaw = FastString.text;
                        }
                    }
                    //
                    // Save the SelectRaw
                    //
                    if (!return_IsEmptyList) {
                        core.doc.inputSelectCache.Add(new CacheInputSelectClass {
                            contentName = ContentName,
                            criteria = lookupContentEditor,
                            currentValue = CurrentValue.ToString(),
                            selectRaw = SelectRaw
                        });
                    }
                }
                //
                SelectRaw = GenericController.strReplace(SelectRaw, MenuNameFPO, MenuName);
                SelectRaw = GenericController.strReplace(SelectRaw, NoneCaptionFPO, NoneCaption);
                if (!string.IsNullOrEmpty(HtmlClass)) {
                    SelectRaw = GenericController.strReplace(SelectRaw, "<select ", "<select class=\"" + HtmlClass + "\"");
                }
                result = SelectRaw;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public string selectUserFromGroup(string MenuName, int currentValue, int GroupID, string ignore = "", string noneCaption = "Select", string HtmlId = "", string HtmlClass = "") {
            string result = "";
            try {
                StringBuilderLegacyController FastString = new StringBuilderLegacyController();
                //
                const string MenuNameFPO = "<MenuName>";
                const string NoneCaptionFPO = "<NoneCaption>";
                //
                string iMenuName = GenericController.encodeText(MenuName);
                currentValue = GenericController.encodeInteger(currentValue);
                noneCaption = GenericController.encodeEmpty(noneCaption, "Select One");
                string sqlCriteria = "";
                //
                string SelectRaw = "";
                foreach (CacheInputSelectClass cacheInputSelect in core.doc.inputSelectCache) {
                    if ((cacheInputSelect.contentName == "Group:" + GroupID) && (cacheInputSelect.criteria == sqlCriteria) && (GenericController.encodeInteger(cacheInputSelect.currentValue) == currentValue)) {
                        SelectRaw = cacheInputSelect.selectRaw;
                        break;
                    }
                }
                //
                //
                //
                if (string.IsNullOrEmpty(SelectRaw)) {
                    //
                    // Build the SelectRaw
                    // Test selection size
                    //
                    int RowMax = 0;
                    string SQL = "select count(*) as cnt"
                        + " from ccMemberRules R"
                        + " inner join ccMembers P on R.memberId=P.ID"
                        + " where (P.active<>0)"
                        + " and (R.GroupID=" + GroupID + ")";
                    using (var csData = new CsModel(core)) {
                        csData.openSql(SQL);
                        if (csData.ok()) {
                            RowMax = RowMax + csData.getInteger("cnt");
                        }
                    }
                    if (RowMax > core.siteProperties.selectFieldLimit) {
                        //
                        // Selection is too big
                        //
                        logger.Error($"{core.logCommonMessage}", new Exception("While building a group members list for group [" + GroupController.getGroupName(core, GroupID) + "], too many rows were selected. [" + RowMax + "] records exceeds [" + core.siteProperties.selectFieldLimit + "], the current Site Property app.SiteProperty_SelectFieldLimit."));
                        result += inputHidden(MenuNameFPO, currentValue);
                        if (currentValue != 0) {
                            using (var csData = new CsModel(core)) {
                                if (csData.openRecord("people", currentValue)) {
                                    result = csData.getText("name") + "&nbsp;";
                                }
                            }
                        }
                        result += "(Selection is too large to display)";
                    } else {
                        //
                        // ----- Generate Drop Down Field Names
                        //
                        string DropDownFieldList = "name";
                        var peopleMetaData = ContentMetadataModel.createByUniqueName(core, "people");
                        if (peopleMetaData != null) DropDownFieldList = peopleMetaData.dropDownFieldList;
                        int DropDownFieldCount = 0;
                        string CharAllowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                        string DropDownPreField = "";
                        string FieldName = "";
                        string SortFieldList = "";
                        string[] DropDownFieldName = { };
                        string[] DropDownDelimiter = { };
                        for (int CharPointer = 1; CharPointer <= DropDownFieldList.Length; CharPointer++) {
                            string CharTest = DropDownFieldList.Substring(CharPointer - 1, 1);
                            if (GenericController.strInstr(1, CharAllowed, CharTest) == 0) {
                                //
                                // Character not allowed, delimit Field name here
                                //
                                if (!string.IsNullOrEmpty(FieldName)) {
                                    //
                                    // ----- main_Get new Field Name and save it
                                    //
                                    if (string.IsNullOrEmpty(SortFieldList)) {
                                        SortFieldList = FieldName;
                                    }
                                    Array.Resize(ref DropDownFieldName, DropDownFieldCount + 1);
                                    Array.Resize(ref DropDownDelimiter, DropDownFieldCount + 1);
                                    DropDownFieldName[DropDownFieldCount] = FieldName;
                                    DropDownDelimiter[DropDownFieldCount] = CharTest;
                                    DropDownFieldCount = DropDownFieldCount + 1;
                                    FieldName = "";
                                } else {
                                    //
                                    // ----- Save Field Delimiter
                                    //
                                    if (DropDownFieldCount == 0) {
                                        //
                                        // ----- Before any field, add to DropDownPreField
                                        //
                                        DropDownPreField = DropDownPreField + CharTest;
                                    } else {
                                        //
                                        // ----- after a field, add to last DropDownDelimiter
                                        //
                                        DropDownDelimiter[DropDownFieldCount - 1] = DropDownDelimiter[DropDownFieldCount - 1] + CharTest;
                                    }
                                }
                            } else {
                                //
                                // Character Allowed, Put character into fieldname and continue
                                //
                                FieldName = FieldName + CharTest;
                            }
                        }
                        if (!string.IsNullOrEmpty(FieldName)) {
                            if (string.IsNullOrEmpty(SortFieldList)) {
                                SortFieldList = FieldName;
                            }
                            Array.Resize(ref DropDownFieldName, DropDownFieldCount + 1);
                            Array.Resize(ref DropDownDelimiter, DropDownFieldCount + 1);
                            DropDownFieldName[DropDownFieldCount] = FieldName;
                            DropDownDelimiter[DropDownFieldCount] = "";
                            DropDownFieldCount = DropDownFieldCount + 1;
                        }
                        if (DropDownFieldCount == 0) {
                            logger.Error($"{core.logCommonMessage}", new Exception("No drop down field names found for content [" + GroupID + "]."));
                        } else {
                            int[] DropDownFieldPointer = new int[DropDownFieldCount];
                            string SelectFields = "P.ID";
                            for (int Ptr = 0; Ptr < DropDownFieldCount; Ptr++) {
                                SelectFields = SelectFields + ",P." + DropDownFieldName[Ptr];
                            }
                            //
                            // ----- Start select box
                            //
                            string TagClass = "";
                            if (GenericController.encodeEmpty(HtmlClass, "") != "") {
                                TagClass = " Class=\"" + GenericController.encodeEmpty(HtmlClass, "") + "\"";
                            }
                            //
                            string TagId = "";
                            if (GenericController.encodeEmpty(HtmlId, "") != "") {
                                TagId = " ID=\"" + GenericController.encodeEmpty(HtmlId, "") + "\"";
                            }
                            //
                            FastString.add("<select size=\"1\" name=\"" + MenuNameFPO + "\"" + TagId + TagClass + ">");
                            FastString.add("<option value=\"\">" + NoneCaptionFPO + "</option>");
                            //
                            // ----- select values
                            //
                            if (string.IsNullOrEmpty(SortFieldList)) {
                                SortFieldList = "name";
                            }
                            using (var csData = new CsModel(core)) {
                                SQL = "select " + SelectFields + " from ccMemberRules R"
                                    + " inner join ccMembers P on R.memberId=P.ID"
                                    + " where (R.GroupID=" + GroupID + ")"
                                    + " and((R.DateExpires is null)or(R.DateExpires>" + DbController.encodeSQLDate(core.dateTimeNowMockable) + "))"
                                    + " and(P.active<>0)"
                                    + " order by P." + SortFieldList;
                                if (csData.openSql(SQL)) {
                                    string[,] RowsArray = csData.getRows();
                                    string[] RowFieldArray = csData.getSelectFieldList().Split(',');
                                    RowMax = RowsArray.GetUpperBound(1);
                                    int ColumnMax = RowsArray.GetUpperBound(0);
                                    //
                                    // setup IDFieldPointer
                                    //
                                    string UcaseFieldName = "ID";
                                    int IDFieldPointer = 0;
                                    for (int ColumnPointer = 0; ColumnPointer <= ColumnMax; ColumnPointer++) {
                                        if (UcaseFieldName == GenericController.toUCase(RowFieldArray[ColumnPointer])) {
                                            IDFieldPointer = ColumnPointer;
                                            break;
                                        }
                                    }
                                    //
                                    // setup DropDownFieldPointer()
                                    //
                                    for (var FieldPointer = 0; FieldPointer < DropDownFieldCount; FieldPointer++) {
                                        UcaseFieldName = GenericController.toUCase(DropDownFieldName[FieldPointer]);
                                        for (int ColumnPointer = 0; ColumnPointer <= ColumnMax; ColumnPointer++) {
                                            if (UcaseFieldName == GenericController.toUCase(RowFieldArray[ColumnPointer])) {
                                                DropDownFieldPointer[FieldPointer] = ColumnPointer;
                                                break;
                                            }
                                        }
                                    }
                                    //
                                    // output select
                                    //
                                    int LastRecordId = -1;
                                    for (int RowPointer = 0; RowPointer <= RowMax; RowPointer++) {
                                        int RecordID = GenericController.encodeInteger(RowsArray[IDFieldPointer, RowPointer]);
                                        if (RecordID != LastRecordId) {
                                            string Copy = DropDownPreField;
                                            for (var FieldPointer = 0; FieldPointer < DropDownFieldCount; FieldPointer++) {
                                                Copy += RowsArray[DropDownFieldPointer[FieldPointer], RowPointer] + DropDownDelimiter[FieldPointer];
                                            }
                                            if (string.IsNullOrEmpty(Copy)) {
                                                Copy = "no name";
                                            }
                                            FastString.add(Environment.NewLine + "<option value=\"" + RecordID + "\" ");
                                            if (RecordID == currentValue) {
                                                FastString.add("selected");
                                            }
                                            if (core.siteProperties.selectFieldWidthLimit != 0) {
                                                if (Copy.Length > core.siteProperties.selectFieldWidthLimit) {
                                                    Copy = Copy.left(core.siteProperties.selectFieldWidthLimit) + "...+";
                                                }
                                            }
                                            FastString.add(">" + Copy + "</option>");
                                            LastRecordId = RecordID;
                                        }
                                    }
                                }
                            }
                            FastString.add("</select>");
                            SelectRaw = FastString.text;
                        }
                    }
                    //
                    // Save the SelectRaw
                    //
                    core.doc.inputSelectCache.Add(new CacheInputSelectClass {
                        contentName = "Group:" + GroupID,
                        criteria = sqlCriteria,
                        currentValue = currentValue.ToString(),
                        selectRaw = SelectRaw
                    });
                }
                //
                SelectRaw = GenericController.strReplace(SelectRaw, MenuNameFPO, iMenuName);
                SelectRaw = GenericController.strReplace(SelectRaw, NoneCaptionFPO, noneCaption);
                result = SelectRaw;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create a select list from a comma separated list, returns an index into the list list, starting at 1, if an element is blank (,) no option is created
        /// </summary>
        /// <param name="MenuName"></param>
        /// <param name="CurrentOneBaseKey"></param>
        /// <param name="SelectList"></param>
        /// <param name="NoneCaption"></param>
        /// <param name="htmlId"></param>
        /// <param name="HtmlClass"></param>
        /// <returns></returns>
        public static string selectFromList(CoreController core, string MenuName, int CurrentOneBaseKey, List<string> lookupList, string NoneCaption, string htmlId, string HtmlClass = "") {
            try {
                var FastString = new StringBuilder();
                FastString.Append("<select size=1");
                if (!string.IsNullOrEmpty(htmlId)) FastString.Append(" id=\"" + htmlId + "\"");
                if (!string.IsNullOrEmpty(HtmlClass)) FastString.Append(" class=\"" + HtmlClass + "\"");
                if (!string.IsNullOrEmpty(MenuName)) FastString.Append(" name=\"" + MenuName + "\"");
                if (!string.IsNullOrEmpty(NoneCaption)) {
                    FastString.Append("><option value=\"\">" + NoneCaption + "</option>");
                } else {
                    FastString.Append("><option value=\"\">Select One</option>");
                }
                //
                // ----- sort values
                var lookupDict = lookupList.Select((s, i) => new { s, i }).ToDictionary(x => x.i + 1, x => x.s);
                var sortedLookupDict = lookupDict.OrderBy(x => x.Value);
                //
                // ----- select values
                foreach (KeyValuePair<int, string> lookup in sortedLookupDict) {
                    if (!string.IsNullOrWhiteSpace(lookup.Value)) {
                        string selected = CurrentOneBaseKey.Equals(lookup.Key) ? " selected" : "";
                        FastString.Append("<option value=\"" + lookup.Key + "\"" + selected + ">" + lookup.Value + "</option>");
                    }
                }
                FastString.Append("</select>");
                return FastString.ToString();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        public static string selectFromList(CoreController core, string MenuName, string CurrentValue, List<string> lookups, string NoneCaption, string htmlId, string HtmlClass = "") {
            int zeroBaseIndex = lookups.FindIndex(x => x.Equals(CurrentValue, StringComparison.InvariantCultureIgnoreCase));
            return selectFromList(core, MenuName, zeroBaseIndex + 1, lookups, NoneCaption, htmlId, HtmlClass);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Display an icon with a link to the login form/cclib.net/admin area
        /// </summary>
        /// <returns></returns>
        public string getLoginLink() {
            string result = "";
            try {
                //
                string Link = null;
                string IconFilename = null;
                //
                if (core.siteProperties.allowLoginIcon) {
                    result += "<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\">";
                    result += "<tr><td align=\"right\">";
                    if (core.session.isAuthenticatedContentManager()) {
                        result += "<a href=\"" + HtmlController.encodeHtml("/" + core.appConfig.adminRoute) + "\" target=\"_blank\">";
                    } else {
                        Link = core.webServer.requestPage + "?" + core.doc.refreshQueryString;
                        Link = GenericController.modifyLinkQuery(Link, RequestNameHardCodedPage, HardCodedPageLogin, true);
                        result += "<a href=\"" + HtmlController.encodeHtml(Link) + "\" >";
                    }
                    IconFilename = core.siteProperties.loginIconFilename;
                    if (GenericController.toLCase(IconFilename.left(7)) != "" + cdnPrefix + "") {
                        IconFilename = GenericController.getCdnFileLink(core, IconFilename);
                    }
                    // original  "<img alt=\"Login\" src=\"" + IconFilename + "\" border=\"0\" >"
                    result += HtmlController.img(IconFilename, "Login");
                    result += "</A>";
                    result += "</td></tr></table>";
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns a multipart form, required for file uploads
        /// </summary>
        /// <param name="core"></param>
        /// <param name="innerHtml"></param>
        /// <param name="actionQueryString"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlClass"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public static string formMultipart(CoreController core, string innerHtml, string actionQueryString, string htmlName, string htmlClass, string htmlId) {
            return form(core, innerHtml, new HtmlAttributesForm {
                action = (string.IsNullOrEmpty(actionQueryString)) ? "" : "?" + actionQueryString,
                method = HtmlAttributesForm.HtmlMethodEnum.post,
                enctype = HtmlAttributesForm.HtmlEncTypeEnum.multipart_form_data,
                name = htmlName,
                @class = htmlClass,
                id = htmlId
            });
        }
        //        
        public static string formMultipart(CoreController core, string innerHtml, string actionQueryString, string htmlName, string htmlClass) {
            return form(core, innerHtml, new HtmlAttributesForm {
                action = (string.IsNullOrEmpty(actionQueryString)) ? "" : "?" + actionQueryString,
                method = HtmlAttributesForm.HtmlMethodEnum.post,
                enctype = HtmlAttributesForm.HtmlEncTypeEnum.multipart_form_data,
                name = htmlName,
                @class = htmlClass
            });
        }
        //        
        public static string formMultipart(CoreController core, string innerHtml, string actionQueryString, string htmlName) {
            return form(core, innerHtml, new HtmlAttributesForm {
                action = (string.IsNullOrEmpty(actionQueryString)) ? "" : "?" + actionQueryString,
                method = HtmlAttributesForm.HtmlMethodEnum.post,
                enctype = HtmlAttributesForm.HtmlEncTypeEnum.multipart_form_data,
                name = htmlName
            });
        }
        //        
        public static string formMultipart(CoreController core, string innerHtml, string actionQueryString) {
            return form(core, innerHtml, new HtmlAttributesForm {
                action = (string.IsNullOrEmpty(actionQueryString)) ? "" : "?" + actionQueryString,
                method = HtmlAttributesForm.HtmlMethodEnum.post,
                enctype = HtmlAttributesForm.HtmlEncTypeEnum.multipart_form_data
            });
        }
        //        
        public static string formMultipart(CoreController core, string innerHtml) {
            return form(core, innerHtml, new HtmlAttributesForm {
                method = HtmlAttributesForm.HtmlMethodEnum.post,
                enctype = HtmlAttributesForm.HtmlEncTypeEnum.multipart_form_data
            });
        }
        //
        //====================================================================================================
        //
        public static string getHtmlAttributesGlobal(HtmlAttributesGlobal attributes) {
            var result = new StringBuilder();
            result.Append((string.IsNullOrWhiteSpace(attributes.accesskey)) ? "" : "" + " accesskey=\"" + attributes.accesskey + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.@class)) ? "" : "" + " class=\"" + attributes.@class + "\"");
            result.Append((!attributes.contenteditable) ? "" : "" + " contenteditable=\"true\"");
            if ((attributes.data != null) && (!attributes.data.Count.Equals(0))) {
                var dataAttr = new StringBuilder();
                foreach (var keyValue in attributes.data) { dataAttr.Append(" data-" + keyValue.Key + "=\"" + keyValue.Value + "\""); }
                result.Append(dataAttr);
            }
            result.Append((string.IsNullOrWhiteSpace(attributes.dir)) ? "" : "" + " dir=\"" + attributes.dir + "\"");
            result.Append((!attributes.draggable) ? "" : "" + " draggable=\"true\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.dropzone)) ? "" : "" + " dropzone=\"" + attributes.dropzone + "\"");
            result.Append((!attributes.hidden) ? "" : "" + " hidden");
            result.Append((string.IsNullOrWhiteSpace(attributes.id)) ? "" : "" + " id=\"" + attributes.id + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.lang)) ? "" : "" + " lang=\"" + attributes.lang + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onabort)) ? "" : "" + " onabort=\"" + attributes.onabort + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onafterprint)) ? "" : "" + " onafterprint=\"" + attributes.onafterprint + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onbeforeprint)) ? "" : "" + " onbeforeprint=\"" + attributes.onbeforeprint + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onbeforeunload)) ? "" : "" + " onbeforeunload=\"" + attributes.onbeforeunload + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onblur)) ? "" : "" + " onblur=\"" + attributes.onblur + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.oncanplay)) ? "" : "" + " oncanplay=\"" + attributes.oncanplay + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.oncanplaythrough)) ? "" : "" + " oncanplaythrough=\"" + attributes.oncanplaythrough + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onchange)) ? "" : "" + " onchange=\"" + attributes.onchange + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onclick)) ? "" : "" + " onclick=\"" + attributes.onclick + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.oncontextmenu)) ? "" : "" + " oncontextmenu=\"" + attributes.oncontextmenu + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.oncopy)) ? "" : "" + " oncopy=\"" + attributes.oncopy + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.oncuechange)) ? "" : "" + " oncuechange=\"" + attributes.oncuechange + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.oncut)) ? "" : "" + " oncut=\"" + attributes.oncut + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ondblclick)) ? "" : "" + " ondblclick=\"" + attributes.ondblclick + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ondrag)) ? "" : "" + " ondrag=\"" + attributes.ondrag + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ondragend)) ? "" : "" + " ondragend=\"" + attributes.ondragend + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ondragenter)) ? "" : "" + " ondragenter=\"" + attributes.ondragenter + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ondragleave)) ? "" : "" + " ondragleave=\"" + attributes.ondragleave + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ondragover)) ? "" : "" + " ondragover=\"" + attributes.ondragover + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ondragstart)) ? "" : "" + " ondragstart=\"" + attributes.ondragstart + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ondrop)) ? "" : "" + " ondrop=\"" + attributes.ondrop + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ondurationchange)) ? "" : "" + " ondurationchange=\"" + attributes.ondurationchange + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onemptied)) ? "" : "" + " onemptied=\"" + attributes.onemptied + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onended)) ? "" : "" + " onended=\"" + attributes.onended + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onerror)) ? "" : "" + " onerror=\"" + attributes.onerror + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onfocus)) ? "" : "" + " onfocus=\"" + attributes.onfocus + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onhashchange)) ? "" : "" + " onhashchange=\"" + attributes.onhashchange + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.oninput)) ? "" : "" + " oninput=\"" + attributes.oninput + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.oninvalid)) ? "" : "" + " oninvalid=\"" + attributes.oninvalid + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onkeydown)) ? "" : "" + " onkeydown=\"" + attributes.onkeydown + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onkeypress)) ? "" : "" + " onkeypress=\"" + attributes.onkeypress + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onkeyup)) ? "" : "" + " onkeyup=\"" + attributes.onkeyup + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onload)) ? "" : "" + " onload=\"" + attributes.onload + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onloadeddata)) ? "" : "" + " onloadeddata=\"" + attributes.onloadeddata + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onloadedmetadata)) ? "" : "" + " onloadedmetadata=\"" + attributes.onloadedmetadata + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onloadstart)) ? "" : "" + " onloadstart=\"" + attributes.onloadstart + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onmessage)) ? "" : "" + " onmessage=\"" + attributes.onmessage + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onmousedown)) ? "" : "" + " onmousedown=\"" + attributes.onmousedown + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onmousemove)) ? "" : "" + " onmousemove=\"" + attributes.onmousemove + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onmouseout)) ? "" : "" + " onmouseout=\"" + attributes.onmouseout + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onmouseover)) ? "" : "" + " onmouseover=\"" + attributes.onmouseover + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onmouseup)) ? "" : "" + " onmouseup=\"" + attributes.onmouseup + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onmousewheel)) ? "" : "" + " onmousewheel=\"" + attributes.onmousewheel + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onoffline)) ? "" : "" + " onoffline=\"" + attributes.onoffline + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ononline)) ? "" : "" + " ononline=\"" + attributes.ononline + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onpagehide)) ? "" : "" + " onpagehide=\"" + attributes.onpagehide + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onpageshow)) ? "" : "" + " onpageshow=\"" + attributes.onpageshow + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onpaste)) ? "" : "" + " onpaste=\"" + attributes.onpaste + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onpause)) ? "" : "" + " onpause=\"" + attributes.onpause + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onplay)) ? "" : "" + " onplay=\"" + attributes.onplay + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onplaying)) ? "" : "" + " onplaying=\"" + attributes.onplaying + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onpopstate)) ? "" : "" + " onpopstate=\"" + attributes.onpopstate + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onprogress)) ? "" : "" + " onprogress=\"" + attributes.onprogress + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onratechange)) ? "" : "" + " onratechange=\"" + attributes.onratechange + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onreset)) ? "" : "" + " onreset=\"" + attributes.onreset + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onresize)) ? "" : "" + " onresize=\"" + attributes.onresize + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onscroll)) ? "" : "" + " onscroll=\"" + attributes.onscroll + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onsearch)) ? "" : "" + " onsearch=\"" + attributes.onsearch + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onseeked)) ? "" : "" + " onseeked=\"" + attributes.onseeked + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onseeking)) ? "" : "" + " onseeking=\"" + attributes.onseeking + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onselect)) ? "" : "" + " onselect=\"" + attributes.onselect + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onstalled)) ? "" : "" + " onstalled=\"" + attributes.onstalled + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onstorage)) ? "" : "" + " onstorage=\"" + attributes.onstorage + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onsubmit)) ? "" : "" + " onsubmit=\"" + attributes.onsubmit + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onsuspend)) ? "" : "" + " onsuspend=\"" + attributes.onsuspend + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.ontimeupdate)) ? "" : "" + " ontimeupdate=\"" + attributes.ontimeupdate + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onunload)) ? "" : "" + " onunload=\"" + attributes.onunload + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onvolumechange)) ? "" : "" + " onvolumechange=\"" + attributes.onvolumechange + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onwaiting)) ? "" : "" + " onwaiting=\"" + attributes.onwaiting + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.onwheel)) ? "" : "" + " onwheel=\"" + attributes.onwheel + "\"");
            result.Append((!attributes.spellcheck) ? "" : "" + " spellcheck");
            result.Append((string.IsNullOrWhiteSpace(attributes.style)) ? "" : "" + " style=\"" + attributes.style + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.tabindex)) ? "" : "" + " tabindex=\"" + attributes.tabindex + "\"");
            result.Append((string.IsNullOrWhiteSpace(attributes.title)) ? "" : "" + " title=\"" + attributes.title + "\"");
            result.Append((!attributes.translate) ? "" : "" + " translate");
            return result.ToString();
        }
        //
        //====================================================================================================
        //
        public static string form(CoreController core, string innerHtml, HtmlAttributesForm attributes) {
            StringBuilder result = new("<form");
            result.Append((string.IsNullOrWhiteSpace(attributes.acceptcharset)) ? "" : $" accept-charset=\"{attributes.acceptcharset}\"");
            result.Append($" action=\"{(string.IsNullOrWhiteSpace(attributes.action) ? "?" + core.doc.refreshQueryString : attributes.action)}\"");
            result.Append((!attributes.autocomplete) ? "" : " autocomplete=\"on\"");
            switch (attributes.enctype) {
                case HtmlAttributesForm.HtmlEncTypeEnum.application_x_www_form_urlencoded: {
                        result.Append(" enctype=\"application/x-www-form-urlencoded\"");
                        break;
                    }
                case HtmlAttributesForm.HtmlEncTypeEnum.text_plain: {
                        result.Append(" enctype=\"text/plain\"");
                        break;
                    }
                default: {
                        //
                        // -- none and multipart, use multi-part -- should be the default so a simple form call works with file uploads
                        result.Append(" enctype=\"multipart/form-data\"");
                        break;
                    }
            }
            if (!attributes.enctype.Equals(HtmlAttributesForm.HtmlEncTypeEnum.none)) {
            }
            result.Append($" method={(attributes.method.Equals(HtmlAttributesForm.HtmlMethodEnum.get) ? "\"get\"" : "\"post\"")}");
            result.Append((string.IsNullOrWhiteSpace(attributes.name)) ? "" : $" name=\"{attributes.name}\"");
            result.Append((!attributes.novalidate) ? "" : " novalidate");
            result.Append((attributes.target.Equals(HtmlAttributesForm.HtmlAttributeTarget.none)) ? "" : $" enctype=\"{attributes.target}\"");
            result.Append(getHtmlAttributesGlobal(attributes));
            return $"{result}>{innerHtml}</form>";
        }
        //
        public static string form(CoreController core, string innerHtml, string actionQueryString, string htmlName, string htmlClass, string htmlId) {
            return form(core, innerHtml, new HtmlAttributesForm {
                action = string.IsNullOrEmpty(actionQueryString) ? "" : (actionQueryString.Substring(0, 1).Equals("?") ? "" : "?") + actionQueryString,
                style = "display: inline",
                name = htmlName,
                @class = htmlClass,
                id = htmlId,
                method = HtmlAttributesForm.HtmlMethodEnum.post
            });
        }
        //
        public static string form(CoreController core, string innerHtml, string actionQueryString, string htmlName) {
            return form(core, innerHtml, new HtmlAttributesForm {
                action = string.IsNullOrEmpty(actionQueryString) ? "" : (actionQueryString.Substring(0, 1).Equals("?") ? "" : "?") + actionQueryString,
                style = "display: inline",
                name = htmlName,
                method = HtmlAttributesForm.HtmlMethodEnum.post
            });
        }
        //
        public static string form(CoreController core, string innerHtml, string actionQueryString) {
            return form(core, innerHtml, new HtmlAttributesForm {
                action = string.IsNullOrEmpty(actionQueryString) ? "" : (actionQueryString.Substring(0, 1).Equals("?") ? "" : "?") + actionQueryString,
                style = "display: inline",
                method = HtmlAttributesForm.HtmlMethodEnum.post
            });
        }
        //
        public static string form(CoreController core, string innerHtml) {
            return form(core, innerHtml, new HtmlAttributesForm {
                style = "display: inline",
                method = HtmlAttributesForm.HtmlMethodEnum.post
            });
        }
        //
        //====================================================================================================
        //
        public static string inputText_Legacy(CoreController core, string htmlName, string defaultValue = "", int heightRows = 1, int widthCharacters = 20, string htmlId = "", bool passwordField = false, bool readOnly = false, string htmlClass = "", int maxLength = -1, bool disabled = false, string placeholder = "", bool required = false) {
            string result = "";
            try {
                if ((heightRows > 1) && !passwordField) {
                    result = inputTextarea(core, htmlName, defaultValue, heightRows, widthCharacters, htmlId, true, readOnly, htmlClass, disabled, maxLength);
                } else {
                    defaultValue = HtmlController.encodeHtml(defaultValue);
                    // todo replace concat with stringbuilder
                    string attrList = " name=\"" + htmlName + "\"";
                    attrList += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
                    attrList += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
                    attrList += (!readOnly) ? "" : " readonly";
                    attrList += (!disabled) ? "" : " disabled";
                    attrList += (maxLength <= 0) ? "" : " maxlength=" + maxLength.ToString();
                    attrList += (string.IsNullOrEmpty(placeholder)) ? "" : " placeholder=\"" + placeholder + "\"";
                    attrList += (!required) ? "" : " required";
                    if (passwordField) {
                        attrList += (widthCharacters <= 0) ? " size=\"" + core.siteProperties.defaultFormInputWidth.ToString() + "\"" : " size=\"" + widthCharacters.ToString() + "\"";
                        result = "<input type=\"password\" value=\"" + defaultValue + "\"" + attrList + ">";
                    } else {
                        attrList += (widthCharacters <= 0) ? " size=\"" + core.siteProperties.defaultFormInputWidth.ToString() + "\"" : " size=\"" + widthCharacters.ToString() + "\"";
                        result = "<input TYPE=\"Text\" value=\"" + defaultValue + "\"" + attrList + ">";
                    }
                    core.doc.formInputTextCnt += 1;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public static string inputText(CoreController core, string htmlName, string defaultValue = "", string htmlClass = "", string htmlId = "", bool readOnly = false, int maxLength = -1, bool disabled = false, string placeholder = "", bool required = false) {
            string result = "";
            try {
                defaultValue = HtmlController.encodeHtml(defaultValue);
                // todo replace concat with stringbuilder
                string attrList = " name=\"" + htmlName + "\"";
                attrList += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
                attrList += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
                attrList += (!readOnly) ? "" : " readonly";
                attrList += (!disabled) ? "" : " disabled";
                attrList += (maxLength <= 0) ? "" : " maxlength=" + maxLength.ToString();
                attrList += (string.IsNullOrEmpty(placeholder)) ? "" : " placeholder=\"" + placeholder + "\"";
                attrList += (!required) ? "" : " required";
                result = "<input TYPE=\"Text\" value=\"" + defaultValue + "\"" + attrList + ">";
                core.doc.formInputTextCnt += 1;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// HTML Form text input (or text area), added disabled case
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="defaultValue"></param>
        /// <param name="heightRows"></param>
        /// <param name="widthCharacters"></param>
        /// <param name="htmlId"></param>
        /// <param name="ignore"></param>
        /// <param name="disabled"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public static string inputTextarea(CoreController core, string htmlName, string defaultValue = "", int heightRows = 4, int widthCharacters = -1, string htmlId = "", bool ignore = false, bool readOnly = false, string htmlClass = "", bool disabled = false, int maxLength = 0, bool required = false) {
            string result = "";
            try {
                defaultValue = HtmlController.encodeHtml(defaultValue);
                string attrList = " name=\"" + htmlName + "\"";
                attrList += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
                attrList += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
                attrList += (!readOnly) ? "" : " readonly";
                attrList += (!disabled) ? "" : " disabled";
                attrList += (maxLength <= 0) ? "" : " maxlength=" + maxLength.ToString();
                attrList += (widthCharacters == -1) ? "" : " cols=" + widthCharacters.ToString();
                attrList += " rows=" + heightRows.ToString();
                attrList += (!required) ? "" : " required";
                result = "<textarea" + attrList + ">" + defaultValue + "</textarea>";
                core.doc.formInputTextCnt += 1;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public static string inputDate(CoreController core, string htmlName, DateTime? htmlValue, string Width = "", string htmlId = "", string htmlClass = "", bool readOnly = false, bool required = false, bool disabled = false) {
            string result = "";
            try {
                core.doc.formInputTextCnt += 1;
                core.doc.inputDateCnt = core.doc.inputDateCnt + 1;
                result = "<input type=\"date\"  name=\"" + HtmlController.encodeHtml(htmlName) + "\"";
                if ((htmlValue != null) && (htmlValue > DateTime.MinValue)) result += " value=\"" + ((DateTime)htmlValue).ToString("o", CultureInfo.InvariantCulture).Substring(0, 10) + "\"";
                result += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
                result += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
                result += (!readOnly) ? "" : " readonly";
                result += (!disabled) ? "" : " disabled";
                result += (!required) ? "" : " required";
                result += ">";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public static string inputTime(CoreController core, string htmlName, DateTime? htmlValue, string htmlId = "", string htmlClass = "", bool readOnly = false, bool required = false, bool disabled = false) {
            string result = "";
            try {
                result += "<input type=\"time\"  name=\"" + HtmlController.encodeHtml(htmlName) + "\" step=\"1\"";
                if ((htmlValue != null) && (htmlValue > DateTime.MinValue)) result += " value=\"" + ((DateTime)htmlValue).ToString("o", CultureInfo.InvariantCulture).Substring(11, 12) + "\"";
                result += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
                result += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
                result += (!readOnly) ? "" : " readonly";
                result += (!disabled) ? "" : " disabled";
                result += (!required) ? "" : " required";
                result += ">";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// HTML Form file upload input
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlId"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public static string inputFile(string htmlName, string htmlId, string htmlClass) {
            string result = "<input type=\"file\" name=\"" + htmlName + "\"";
            result += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
            result += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
            return result + ">";
        }
        //
        public static string inputFile(string htmlName, string htmlId) => inputFile(htmlName, htmlId, "");
        //
        public static string inputFile(string htmlName) => inputFile(htmlName, "", "");
        //
        //====================================================================================================
        /// <summary>
        /// Radio. checked when htmlValue=currentValue. Value always included
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="CurrentValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public static string inputRadio(string htmlName, string htmlValue, string CurrentValue, string htmlId, string htmlClass) {
            string result = "<input type=radio name=\"" + htmlName + "\" value=\"" + htmlValue + "\"";
            result += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
            result += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
            result += (htmlValue.ToLower().Equals(CurrentValue.ToLower())) ? " checked" : "";
            return result + ">";
        }
        //
        public static string inputRadio(string htmlName, string htmlValue, string CurrentValue, string htmlId) => inputRadio(htmlName, htmlValue, CurrentValue, htmlId, "");
        //
        public static string inputRadio(string htmlName, string htmlValue, string CurrentValue) => inputRadio(htmlName, htmlValue, CurrentValue, "", "");
        //
        public static string inputRadio(string htmlName, int htmlValue, int CurrentValue, string htmlId, string htmlClass) {
            string result = "<input type=radio name=\"" + htmlName + "\" value=\"" + htmlValue + "\"";
            result += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
            result += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
            result += (htmlValue != CurrentValue) ? "" : " checked";
            return result + ">";
        }
        //
        public static string inputRadio(string htmlName, int htmlValue, int CurrentValue, string htmlId) => inputRadio(htmlName, htmlValue, CurrentValue, htmlId, "");
        //
        public static string inputRadio(string htmlName, int htmlValue, int CurrentValue) => inputRadio(htmlName, htmlValue, CurrentValue, "", "");
        //
        //====================================================================================================
        //
        public static string checkbox(string TagName, string DefaultValue) {
            return checkbox(TagName, GenericController.encodeBoolean(DefaultValue));
        }
        //
        //====================================================================================================
        //
        public static string checkbox(string htmlName, bool selected = false, string htmlId = "", bool disabled = false, string htmlClass = "", bool readOnly = false, string htmlValue = "1", string label = "", bool required = false) {
            string result = "<input type=\"checkbox\" name=\"" + htmlName + "\" value=\"" + htmlValue + "\"";
            if (readOnly && !selected) {
                result += " disabled";
            } else if (readOnly) {
                result += " disabled checked";
            } else if (selected) {
                result += " checked";
            }
            if (!string.IsNullOrWhiteSpace(htmlId)) {
                result += " id=\"" + htmlId + "\"";
            }
            if (!string.IsNullOrWhiteSpace(htmlClass)) {
                result += " class=\"" + htmlClass + "\"";
            }
            result += (!required) ? "" : " required";
            result += ">";
            if (!string.IsNullOrWhiteSpace(label)) {
                result = div("<label>" + result + "&nbsp;" + label + "</label>", "checkbox");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return an html input element for the field in the current dataset
        /// </summary>
        /// <param name="cs"></param>
        /// <param name="contentName"></param>
        /// <param name="fieldName"></param>
        /// <param name="Height"></param>
        /// <param name="Width"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public string inputCs(CsModel cs, string contentName, string fieldName, int Height = 1, int Width = 40, string htmlId = "") {
            string returnResult = "";
            try {
                bool fieldFound = false;
                var contentMetadata = ContentMetadataModel.createByUniqueName(core, contentName);
                string FieldValueVariant = "";
                CPContentBaseClass.FieldTypeIdEnum fieldTypeId = 0;
                bool FieldReadOnly = false;
                bool FieldPassword = false;
                int FieldLookupContentId = 0;
                int FieldMemberSelectGroupId = 0;
                bool FieldHTMLContent = false;
                string FieldLookupList = "";
                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in contentMetadata.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
                    if (field.nameLc == fieldName.ToLower(CultureInfo.InvariantCulture)) {
                        FieldValueVariant = field.defaultValue;
                        fieldTypeId = field.fieldTypeId;
                        FieldReadOnly = field.readOnly;
                        string FieldCaption = field.caption;
                        FieldPassword = field.password;
                        FieldHTMLContent = field.htmlContent;
                        FieldLookupContentId = field.lookupContentId;
                        FieldLookupList = field.lookupList;
                        FieldMemberSelectGroupId = field.memberSelectGroupId_get(core, contentName, fieldName);
                        fieldFound = true;
                        break;
                    }
                }
                if (!fieldFound) {
                    logger.Error($"{core.logCommonMessage}", new Exception("Field [" + fieldName + "] was not found in Content Definition [" + contentName + "]"));
                } else {
                    //
                    // main_Get the current value if the record was found
                    //
                    if (cs.ok()) { FieldValueVariant = cs.getValueStoredInDbField(fieldName); }
                    string FieldValueText = null;
                    //
                    if (FieldPassword) {
                        //
                        // Handle Password Fields
                        //
                        FieldValueText = GenericController.encodeText(FieldValueVariant);
                        returnResult = inputText_Legacy(core, fieldName, FieldValueText, Height, Width, "", true);
                    } else {
                        string FieldLookupContentName = null;
                        int FieldValueInteger = 0;
                        //
                        // Non Password field by fieldtype
                        //
                        switch (fieldTypeId) {
                            case CPContentBaseClass.FieldTypeIdEnum.HTML:
                            case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                                FieldValueText = GenericController.encodeText(FieldValueVariant);
                                if (FieldReadOnly) {
                                    returnResult = FieldValueText;
                                } else {
                                    returnResult = getFormInputHTML(fieldName, FieldValueText, "", Width.ToString());
                                }
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                                //
                                // html files, read from cdnFiles and use html editor
                                //
                                FieldValueText = GenericController.encodeText(FieldValueVariant);
                                if (!string.IsNullOrEmpty(FieldValueText)) {
                                    FieldValueText = core.cdnFiles.readFileText(FieldValueText);
                                }
                                if (FieldReadOnly) {
                                    returnResult = FieldValueText;
                                } else {
                                    returnResult = getFormInputHTML(fieldName, FieldValueText, "", Width.ToString());
                                }
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                //
                                // text cdnFiles files, read from cdnFiles and use text editor
                                //
                                FieldValueText = GenericController.encodeText(FieldValueVariant);
                                if (!string.IsNullOrEmpty(FieldValueText)) {
                                    FieldValueText = core.cdnFiles.readFileText(FieldValueText);
                                }
                                if (FieldReadOnly) {
                                    returnResult = FieldValueText;
                                } else {
                                    returnResult = inputText_Legacy(core, fieldName, FieldValueText, Height, Width);
                                }
                                //
                                // text public files, read from core.cdnFiles and use text editor
                                //
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                            case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                            case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                                FieldValueText = GenericController.encodeText(FieldValueVariant);
                                if (!string.IsNullOrEmpty(FieldValueText)) {
                                    FieldValueText = core.cdnFiles.readFileText(FieldValueText);
                                }
                                if (FieldReadOnly) {
                                    returnResult = FieldValueText;
                                } else {
                                    returnResult = inputText_Legacy(core, fieldName, FieldValueText, Height, Width);
                                }
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.Boolean:
                                if (FieldReadOnly) {
                                    returnResult = GenericController.encodeText(GenericController.encodeBoolean(FieldValueVariant));
                                } else {
                                    returnResult = checkbox(fieldName, GenericController.encodeBoolean(FieldValueVariant));
                                }
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                                returnResult = GenericController.encodeText(GenericController.encodeNumber(FieldValueVariant));
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.Float:
                            case CPContentBaseClass.FieldTypeIdEnum.Currency:
                            case CPContentBaseClass.FieldTypeIdEnum.Integer:
                                FieldValueVariant = GenericController.encodeNumber(FieldValueVariant).ToString();
                                if (FieldReadOnly) {
                                    returnResult = GenericController.encodeText(FieldValueVariant);
                                } else {
                                    returnResult = inputText_Legacy(core, fieldName, GenericController.encodeText(FieldValueVariant), Height, Width);
                                }
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.File:
                                FieldValueText = GenericController.encodeText(FieldValueVariant);
                                if (FieldReadOnly) {
                                    returnResult = FieldValueText;
                                } else {
                                    returnResult = FieldValueText + "<br>change: " + inputFile(fieldName, GenericController.encodeText(FieldValueVariant));
                                }
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                                FieldValueText = GenericController.encodeText(FieldValueVariant);
                                if (FieldReadOnly) {
                                    returnResult = FieldValueText;
                                } else {
                                    returnResult = "<img src=\"" + GenericController.getCdnFileLink(core, FieldValueText) + "\"><br>change: " + inputFile(fieldName, GenericController.encodeText(FieldValueVariant));
                                }
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                                FieldValueInteger = GenericController.encodeInteger(FieldValueVariant);
                                FieldLookupContentName = MetadataController.getContentNameByID(core, FieldLookupContentId);
                                if (!string.IsNullOrEmpty(FieldLookupContentName)) {
                                    //
                                    // Lookup into Content
                                    //
                                    if (FieldReadOnly) {
                                        using (CsModel csLookup = new CsModel(core)) {
                                            if (cs.openRecord(FieldLookupContentName, FieldValueInteger)) {
                                                returnResult = csLookup.getTextEncoded("name");
                                            }
                                        }
                                    } else {
                                        bool IsEmptyList = false;
                                        returnResult = selectFromContent(fieldName, FieldValueInteger, FieldLookupContentName, "", "", "", ref IsEmptyList);
                                    }
                                } else if (!string.IsNullOrEmpty(FieldLookupList)) {
                                    //
                                    // Lookup into LookupList
                                    //
                                    returnResult = selectFromList(core, fieldName, FieldValueInteger, FieldLookupList.Split(',').ToList(), "", "");
                                } else {
                                    //
                                    // Just call it text
                                    //
                                    returnResult = inputText_Legacy(core, fieldName, FieldValueInteger.ToString(), Height, Width);
                                }
                                break;
                            case CPContentBaseClass.FieldTypeIdEnum.MemberSelect:
                                FieldValueInteger = GenericController.encodeInteger(FieldValueVariant);
                                returnResult = selectUserFromGroup(fieldName, FieldValueInteger, FieldMemberSelectGroupId);
                                break;
                            default:
                                FieldValueText = GenericController.encodeText(FieldValueVariant);
                                if (FieldReadOnly) {
                                    returnResult = FieldValueText;
                                } else {
                                    if (FieldHTMLContent) {
                                        returnResult = getFormInputHTML(fieldName, FieldValueText, Height.ToString(), Width.ToString(), FieldReadOnly, false);
                                    } else {
                                        returnResult = inputText_Legacy(core, fieldName, FieldValueText, Height, Width);
                                    }
                                }
                                break;
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnResult;
        }
        //
        //====================================================================================================
        //
        public static string inputSubmit(string htmlValue, string htmlName = "button", string htmlId = "", string onClick = "", bool disabled = false, string htmlClass = "") {
            string attrList = "<input type=submit";
            attrList += (string.IsNullOrEmpty(htmlName)) ? "" : " name=\"" + htmlName + "\"";
            attrList += (string.IsNullOrEmpty(htmlValue)) ? "" : " value=\"" + htmlValue + "\"";
            attrList += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
            attrList += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
            attrList += (string.IsNullOrEmpty(onClick)) ? "" : " onclick=\"" + onClick + "\"";
            attrList += (!disabled) ? "" : " disabled";
            return attrList + ">";
        }
        //
        //====================================================================================================
        //
        public static string inputHidden(string htmlName, string htmlValue, string htmlClass, string htmlId) {
            string attrList = "<input type=hidden";
            attrList += (string.IsNullOrEmpty(htmlName)) ? "" : " name=\"" + encodeHtml(htmlName) + "\"";
            attrList += (string.IsNullOrEmpty(htmlValue)) ? "" : " value=\"" + encodeHtml(htmlValue) + "\"";
            attrList += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + encodeHtml(htmlId) + "\"";
            attrList += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + encodeHtml(htmlClass) + "\"";
            return attrList + ">";
        }
        public static string inputHidden(string name, string value) => inputHidden(name, value, "", "");
        public static string inputHidden(string name, string value, string htmlClass) => inputHidden(name, value, htmlClass, "");
        //
        public static string inputHidden(string TagName, bool TagValue) => inputHidden(TagName, TagValue.ToString());
        public static string inputHidden(string TagName, bool TagValue, string htmlClass) => inputHidden(TagName, TagValue.ToString(), htmlClass);
        public static string inputHidden(string TagName, bool TagValue, string htmlClass, string htmlId) => inputHidden(TagName, TagValue.ToString(), htmlClass, htmlId);
        //
        public static string inputHidden(string TagName, int TagValue) => inputHidden(TagName, TagValue.ToString());
        public static string inputHidden(string TagName, int TagValue, string htmlClass) => inputHidden(TagName, TagValue.ToString(), htmlClass);
        public static string inputHidden(string TagName, int TagValue, string htmlClass, string htmlId) => inputHidden(TagName, TagValue.ToString(), htmlClass, htmlId);
        //
        public static string inputHidden(string TagName, double TagValue) => inputHidden(TagName, TagValue.ToString());
        public static string inputHidden(string TagName, double TagValue, string htmlClass) => inputHidden(TagName, TagValue.ToString(), htmlClass);
        public static string inputHidden(string TagName, double TagValue, string htmlClass, string htmlId) => inputHidden(TagName, TagValue.ToString(), htmlClass, htmlId);
        //
        public static string inputHidden(string TagName, DateTime TagValue) => inputHidden(TagName, TagValue.ToString());
        public static string inputHidden(string TagName, DateTime TagValue, string htmlClass) => inputHidden(TagName, TagValue.ToString(), htmlClass);
        public static string inputHidden(string TagName, DateTime TagValue, string htmlClass, string htmlId) => inputHidden(TagName, TagValue.ToString(), htmlClass, htmlId);
        //
        //====================================================================================================
        //
        public void javascriptAddEvent(string HtmlId, string DOMEvent, string Javascript) {
            string JSCodeAsString = Javascript;
            JSCodeAsString = GenericController.strReplace(JSCodeAsString, "'", "'+\"'\"+'");
            JSCodeAsString = GenericController.strReplace(JSCodeAsString, Environment.NewLine, "\\n");
            JSCodeAsString = GenericController.strReplace(JSCodeAsString, "\r", "\\n");
            JSCodeAsString = GenericController.strReplace(JSCodeAsString, "\n", "\\n");
            JSCodeAsString = "'" + JSCodeAsString + "'";
            addScriptCode_onLoad("cj.addListener(document.getElementById('" + HtmlId + "'),'" + DOMEvent + "',function(){eval(" + JSCodeAsString + ")})", "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns a json format list of all addons that can be placed into the contentType provided (page, mail). Cached in doc scope
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public string getWysiwygAddonList(CPHtml5BaseClass.EditorContentType contentType) {
            string result = "";
            try {
                if (core.doc.wysiwygAddonList.ContainsKey(contentType)) {
                    result = core.doc.wysiwygAddonList[contentType];
                } else {
                    //
                    // -- AC Tags, Would like to replace these with Add-ons eventually
                    int ItemsSize = 100;
                    string[] ItemsHtmlId = new string[101];
                    string[] ItemsJson = new string[101];
                    int ItemsCnt = 0;
                    var Index = new KeyPtrController();
                    //
                    // -- AC StartBlockText
                    string IconIDControlString = "AC," + ACTypeAggregateFunction + ",0,Block Text,";
                    string IconImg = AddonController.getAddonIconImg("/" + core.appConfig.adminRoute, 0, 0, 0, true, IconIDControlString, "", core.appConfig.cdnFileUrl, "Text Block Start", "Block text to all except selected groups starting at this point", "", 0);
                    IconImg = GenericController.encodeJavascriptStringSingleQuote(IconImg);
                    ItemsHtmlId[ItemsCnt] = "['Block Text','{%{\"Block Text\":{\"Group\":\"EnterGroupName\"}}%}']";
                    ItemsJson[ItemsCnt] = "['Block Text','" + IconImg + "']";
                    Index.setPtr("Block Text", ItemsCnt);
                    ItemsCnt += 1;
                    //
                    // AC EndBlockText
                    //
                    IconIDControlString = "AC," + ACTypeAggregateFunction + ",0,Block Text End,";
                    IconImg = AddonController.getAddonIconImg("/" + core.appConfig.adminRoute, 0, 0, 0, true, IconIDControlString, "", core.appConfig.cdnFileUrl, "Text Block End", "End of text block", "", 0);
                    IconImg = GenericController.encodeJavascriptStringSingleQuote(IconImg);
                    ItemsHtmlId[ItemsCnt] = "['Block Text End','{%{\"Block Text End\"%}']";
                    ItemsJson[ItemsCnt] = "['Block Text End','" + IconImg + "']";
                    Index.setPtr("Block Text", ItemsCnt);
                    ItemsCnt += 1;
                    //
                    if ((contentType == CPHtml5BaseClass.EditorContentType.contentTypeEmail) || (contentType == CPHtml5BaseClass.EditorContentType.contentTypeEmailTemplate)) {
                        //
                        // ----- Email Only AC tags
                        //
                        // Editing Email Body or Templates - Since Email can not process Add-ons, it main_Gets the legacy AC tags for now
                        //
                        // Personalization Tag
                        //
                        string selectOptions = "";
                        var peopleMetaData = ContentMetadataModel.createByUniqueName(core, "people");
                        if (peopleMetaData != null) selectOptions = string.Join("|", peopleMetaData.selectList);
                        IconIDControlString = "AC,PERSONALIZATION,0,Personalization,field=[" + selectOptions + "]";
                        IconImg = AddonController.getAddonIconImg("/" + core.appConfig.adminRoute, 0, 0, 0, true, IconIDControlString, "", core.appConfig.cdnFileUrl, "Any Personalization Field", "Renders as any Personalization Field", "", 0);
                        IconImg = GenericController.encodeJavascriptStringSingleQuote(IconImg);
                        ItemsHtmlId[ItemsCnt] = "['Personalization','" + IconImg + "']";
                        ItemsJson[ItemsCnt] = "['Personalization','{%{\"Personalization\":{\"name\":\"firstName\"}}%}']";
                        Index.setPtr("Personalization", ItemsCnt);
                        ItemsCnt += 1;
                        //
                        if (contentType == CPHtml5BaseClass.EditorContentType.contentTypeEmailTemplate) {
                            //
                            // Editing Email Templates
                            //   This is a special case
                            //   Email content processing can not process add-ons, and PageContentBox and TextBox are needed
                            //   So I added the old AC Tag into the menu for this case
                            //   Need a more consistant solution later
                            //
                            IconIDControlString = "AC," + ACTypeTemplateContent + ",0,Template Content,";
                            IconImg = AddonController.getAddonIconImg("/" + core.appConfig.adminRoute, 52, 64, 0, false, IconIDControlString, "" + cdnPrefix + "images/ACTemplateContentIcon.gif", core.appConfig.cdnFileUrl, "Content Box", "Renders as the content for a template", "", 0);
                            IconImg = GenericController.encodeJavascriptStringSingleQuote(IconImg);
                            ItemsHtmlId[ItemsCnt] = "['Content Box','" + IconImg + "']";
                            ItemsJson[ItemsCnt] = "['Content Box','{%{\"ContentBox\"%}']";
                            Index.setPtr("Content Box", ItemsCnt);
                            ItemsCnt += 1;
                            //
                            IconIDControlString = "AC," + ACTypeTemplateText + ",0,Template Text,Name=Default";
                            IconImg = AddonController.getAddonIconImg("/" + core.appConfig.adminRoute, 52, 52, 0, false, IconIDControlString, "" + cdnPrefix + "images/ACTemplateTextIcon.gif", core.appConfig.cdnFileUrl, "Template Text", "Renders as a template text block", "", 0);
                            IconImg = encodeJavascriptStringSingleQuote(IconImg);
                            ItemsHtmlId[ItemsCnt] = "['Template Text','" + IconImg + "']";
                            ItemsJson[ItemsCnt] = "['Template Text','{%{\"TemplateText\"%}']";
                            Index.setPtr("Template Text", ItemsCnt);
                            ItemsCnt += 1;
                        }
                    }
                    //
                    // -- addons
                    string Criteria = "(1=1)";
                    if (contentType == CPHtml5BaseClass.EditorContentType.contentTypeEmail) {
                        //
                        // select only addons with email placement (dont need to check main_version bc if email, must be >4.0.325
                        //
                        Criteria = Criteria + "and(email<>0)";
                    } else {
                        if (contentType == CPHtml5BaseClass.EditorContentType.contentTypeWeb) {
                            //
                            // Non Templates
                            Criteria = Criteria + "and(content<>0)";
                        } else {
                            //
                            // Templates
                            Criteria = Criteria + "and(template<>0)";
                        }
                    }
                    string AddonContentName = AddonModel.tableMetadata.contentName;
                    string SelectList = "Name,Link,ID,ArgumentList,IconFilename,IconWidth,IconHeight,IconSprites,IsInline,ccguid";
                    using (var csData = new CsModel(core)) {
                        if (csData.open(AddonContentName, Criteria, "Name,ID", false, 0, SelectList)) {
                            string LastAddonName = "";
                            while (csData.ok()) {
                                string addonGuid = csData.getText("ccguid");
                                string addonName = encodeText(csData.getText("name")).Trim(' ');
                                if (!string.IsNullOrEmpty(addonName) && (addonName != LastAddonName)) {
                                    //
                                    // Icon (fieldtyperesourcelink)
                                    //
                                    bool IsInline = csData.getBoolean("IsInline");
                                    string IconFilename = csData.getText("Iconfilename");
                                    int IconWidth = 0;
                                    int IconHeight = 0;
                                    int IconSprites = 0;
                                    if (!string.IsNullOrEmpty(IconFilename)) {
                                        IconWidth = csData.getInteger("IconWidth");
                                        IconHeight = csData.getInteger("IconHeight");
                                        IconSprites = csData.getInteger("IconSprites");
                                    }
                                    //
                                    // Calculate DefaultAddonOption_String
                                    //
                                    string ArgumentList = csData.getText("ArgumentList").Trim(' ');
                                    string jsonCommand = "";
                                    string defaultAddonOptions = AddonController.getDefaultAddonOptions(core, ArgumentList, addonGuid, IsInline, addonName, ref jsonCommand);
                                    defaultAddonOptions = encodeHtml(defaultAddonOptions);
                                    LastAddonName = addonName;
                                    IconIDControlString = "AC,AGGREGATEFUNCTION,0," + addonName + "," + defaultAddonOptions + "," + addonGuid;
                                    IconImg = AddonController.getAddonIconImg("/" + core.appConfig.adminRoute, IconWidth, IconHeight, IconSprites, IsInline, IconIDControlString, IconFilename, core.appConfig.cdnFileUrl, addonName, "Rendered as the Add-on [" + addonName + "]", "", 0);
                                    ItemsHtmlId[ItemsCnt] = "['" + encodeJavascriptStringSingleQuote(addonName) + "','" + encodeJavascriptStringSingleQuote(IconImg) + "']";
                                    ItemsJson[ItemsCnt] = "['" + encodeJavascriptStringSingleQuote(addonName) + "','" + encodeJavascriptStringSingleQuote(jsonCommand) + "']";
                                    Index.setPtr(addonName, ItemsCnt);
                                    ItemsCnt += 1;
                                    if (ItemsCnt >= ItemsSize) {
                                        ItemsSize = ItemsSize + 100;
                                        Array.Resize(ref ItemsHtmlId, ItemsSize + 1);
                                        Array.Resize(ref ItemsJson, ItemsSize + 1);
                                    }
                                }
                                csData.goNext();

                            }
                        }
                    }
                    //
                    // Build output sting in alphabetical order by name
                    //
                    int ItemsPtr = Index.getFirstPtr();
                    int LoopPtr = 0;
                    bool useJson = core.siteProperties.getBoolean("wysiwyg clips use JSON commands", true);
                    while ((ItemsPtr >= 0) && (LoopPtr < ItemsCnt)) {
                        result += Environment.NewLine + "," + ((useJson) ? ItemsJson[ItemsPtr] : ItemsHtmlId[ItemsPtr]);
                        int PtrTest = Index.getNextPtr();
                        if (PtrTest < 0) {
                            break;
                        } else {
                            ItemsPtr = PtrTest;
                        }
                        LoopPtr += 1;
                    }
                    if (!string.IsNullOrEmpty(result)) {
                        result = "[" + result.Substring(3) + "]";
                    }
                    //
                    core.doc.wysiwygAddonList.Add(contentType, result);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// replace string new-line with html break
        /// </summary>
        public static string convertNewLineToHtmlBreak(string text) {
            if (string.IsNullOrWhiteSpace(text)) { return ""; }
            return text.Replace(windowsNewLine, "<br>").Replace(unixNewLine, "<br>").Replace(macNewLine, "<br>");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Convert an HTML source to a text equivelent. converts CRLF to br, encodes reserved HTML characters to their equivalent
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string convertTextToHtml(string text) {
            return convertNewLineToHtmlBreak(encodeHtml(text));
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get Addon Selector
        ///
        ///   The addon selector is the string sent out with the content in edit-mode. In the editor, it is converted by javascript
        ///   to the popup window that selects instance options. It is in this format:
        ///
        ///   Select (creates a list of names in a select box, returns the selected name)
        ///       name=currentvalue[optionname0:optionvalue0|optionname1:optionvalue1|...]
        ///   CheckBox (creates a list of names in checkboxes, and returns the selected names)
        /// </summary>
        /// <param name="SrcOptionName"></param>
        /// <param name="InstanceOptionValue_AddonEncoded"></param>
        /// <param name="SrcOptionValueSelector"></param>
        /// <returns></returns>
        public string getAddonSelector(string SrcOptionName, string InstanceOptionValue_AddonEncoded, string SrcOptionValueSelector) {
            string result = "";
            try {
                //
                const string ACFunctionList = "List";
                const string ACFunctionList1 = "selectname";
                const string ACFunctionList2 = "listname";
                const string ACFunctionList3 = "selectcontentname";
                const string ACFunctionListId = "ListID";
                const string ACFunctionListFields = "ListFields";
                string SrcSelector = SrcOptionValueSelector.Trim(' ');
                //
                string SrcSelectorInner = SrcSelector;
                int PosLeft = GenericController.strInstr(1, SrcSelector, "[");
                string SrcSelectorSuffix = "";
                if (PosLeft != 0) {
                    int PosRight = GenericController.strInstr(1, SrcSelector, "]");
                    if (PosRight != 0) {
                        if (PosRight < SrcSelector.Length) {
                            SrcSelectorSuffix = SrcSelector.Substring(PosRight);
                        }
                        SrcSelector = (SrcSelector.Substring(PosLeft - 1, PosRight - PosLeft + 1)).Trim(' ');
                        SrcSelectorInner = (SrcSelector.Substring(1, SrcSelector.Length - 2)).Trim(' ');
                    }
                }
                //
                // Break SrcSelectorInner up into individual choices to detect functions
                //
                string list = "";
                int Pos = 0;
                if (!string.IsNullOrEmpty(SrcSelectorInner)) {
                    string[] Choices = SrcSelectorInner.Split('|');
                    int ChoiceCnt = Choices.GetUpperBound(0) + 1;
                    int Ptr = 0;
                    for (Ptr = 0; Ptr < ChoiceCnt; Ptr++) {
                        bool IsContentList = false;
                        bool IsListField = false;
                        //
                        // List Function (and all the indecision that went along with it)
                        //
                        bool IncludeID = false;
                        int FnLen = 0;
                        string Choice = Choices[Ptr];
                        if (Pos == 0) {
                            Pos = GenericController.strInstr(1, Choice, ACFunctionList1 + "(", 1);
                            if (Pos > 0) {
                                IsContentList = true;
                                IncludeID = false;
                                FnLen = ACFunctionList1.Length;
                            }
                        }
                        if (Pos == 0) {
                            Pos = GenericController.strInstr(1, Choice, ACFunctionList2 + "(", 1);
                            if (Pos > 0) {
                                IsContentList = true;
                                IncludeID = false;
                                FnLen = ACFunctionList2.Length;
                            }
                        }
                        if (Pos == 0) {
                            Pos = GenericController.strInstr(1, Choice, ACFunctionList3 + "(", 1);
                            if (Pos > 0) {
                                IsContentList = true;
                                IncludeID = false;
                                FnLen = ACFunctionList3.Length;
                            }
                        }
                        if (Pos == 0) {
                            Pos = GenericController.strInstr(1, Choice, ACFunctionListId + "(", 1);
                            if (Pos > 0) {
                                IsContentList = true;
                                IncludeID = true;
                                FnLen = ACFunctionListId.Length;
                            }
                        }
                        if (Pos == 0) {
                            Pos = GenericController.strInstr(1, Choice, ACFunctionList + "(", 1);
                            if (Pos > 0) {
                                IsContentList = true;
                                IncludeID = false;
                                FnLen = ACFunctionList.Length;
                            }
                        }
                        if (Pos == 0) {
                            Pos = GenericController.strInstr(1, Choice, ACFunctionListFields + "(", 1);
                            if (Pos > 0) {
                                IsListField = true;
                                IncludeID = false;
                                FnLen = ACFunctionListFields.Length;
                            }
                        }
                        //
                        if (Pos > 0) {
                            //
                            string FnArgList = (Choice.Substring((Pos + FnLen) - 1)).Trim(' ');
                            string ContentName = "";
                            string ContentCriteria = "";
                            if ((FnArgList.left(1) == "(") && (FnArgList.Substring(FnArgList.Length - 1) == ")")) {
                                //
                                // set ContentName and ContentCriteria from argument list
                                //
                                FnArgList = FnArgList.Substring(1, FnArgList.Length - 2);
                                string[] FnArgs = GenericController.splitDelimited(FnArgList, ",");
                                int FnArgCnt = FnArgs.GetUpperBound(0) + 1;
                                if (FnArgCnt > 0) {
                                    ContentName = FnArgs[0].Trim(' ');
                                    if ((ContentName.left(1) == "\"") && (ContentName.Substring(ContentName.Length - 1) == "\"")) {
                                        ContentName = (ContentName.Substring(1, ContentName.Length - 2)).Trim(' ');
                                    } else if ((ContentName.left(1) == "'") && (ContentName.Substring(ContentName.Length - 1) == "'")) {
                                        ContentName = (ContentName.Substring(1, ContentName.Length - 2)).Trim(' ');
                                    }
                                }
                                if (FnArgCnt > 1) {
                                    ContentCriteria = FnArgs[1].Trim(' ');
                                    if ((ContentCriteria.left(1) == "\"") && (ContentCriteria.Substring(ContentCriteria.Length - 1) == "\"")) {
                                        ContentCriteria = (ContentCriteria.Substring(1, ContentCriteria.Length - 2)).Trim(' ');
                                    } else if ((ContentCriteria.left(1) == "'") && (ContentCriteria.Substring(ContentCriteria.Length - 1) == "'")) {
                                        ContentCriteria = (ContentCriteria.Substring(1, ContentCriteria.Length - 2)).Trim(' ');
                                    }
                                }
                            }
                            using (var csData = new CsModel(core)) {
                                if (IsContentList) {
                                    //
                                    // ContentList - Open the Content and build the options from the names
                                    csData.open(ContentName, ContentCriteria, "name", true, 0, "ID,Name");
                                } else if (IsListField) {
                                    //
                                    //
                                    // ListField
                                    int CId = ContentMetadataModel.getContentId(core, ContentName);
                                    csData.open("Content Fields", "Contentid=" + CId, "name", true, 0, "ID,Name");
                                }

                                if (csData.ok()) {
                                    object[,] Cell = csData.getRows();
                                    int RowCnt = Cell.GetUpperBound(1) + 1;
                                    int RowPtr = 0;
                                    for (RowPtr = 0; RowPtr < RowCnt; RowPtr++) {
                                        //
                                        string RecordName = GenericController.encodeText(Cell[1, RowPtr]);
                                        RecordName = GenericController.strReplace(RecordName, Environment.NewLine, " ");
                                        int RecordID = GenericController.encodeInteger(Cell[0, RowPtr]);
                                        if (string.IsNullOrEmpty(RecordName)) {
                                            RecordName = "record " + RecordID;
                                        } else if (RecordName.Length > 50) {
                                            RecordName = RecordName.left(50) + "...";
                                        }
                                        RecordName = GenericController.encodeNvaArgument(RecordName);
                                        list = list + "|" + RecordName;
                                        if (IncludeID) {
                                            list = list + ":" + RecordID;
                                        }
                                    }
                                }
                            }
                        } else {
                            //
                            // choice is not a function, just add the choice back to the list
                            //
                            list = list + "|" + Choices[Ptr];
                        }
                    }
                    if (!string.IsNullOrEmpty(list)) {
                        list = list.Substring(1);
                    }
                }
                //
                // Build output string
                //
                result = HtmlController.encodeHtml(GenericController.encodeNvaArgument(SrcOptionName)) + "=";
                if (!string.IsNullOrEmpty(InstanceOptionValue_AddonEncoded)) {
                    result += HtmlController.encodeHtml(InstanceOptionValue_AddonEncoded);
                }
                if (string.IsNullOrEmpty(SrcSelectorSuffix) && string.IsNullOrEmpty(list)) {
                    //
                    // empty list with no suffix, return with name=value
                    //
                } else if (GenericController.toLCase(SrcSelectorSuffix) == "resourcelink") {
                    //
                    // resource link, exit with empty list
                    //
                    result += "[]ResourceLink";
                } else {
                    //
                    //
                    //
                    result += "[" + list + "]" + SrcSelectorSuffix;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// getFormInputHTML
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="DefaultValue"></param>
        /// <param name="styleHeight"></param>
        /// <param name="styleWidth"></param>
        /// <param name="readOnlyfield"></param>
        /// <param name="allowActiveContent"></param>
        /// <param name="addonListJSON"></param>
        /// <param name="styleList"></param>
        /// <param name="styleOptionList"></param>
        /// <param name="allowResourceLibrary"></param>
        /// <returns></returns>
        public string getFormInputHTML(string htmlName, string DefaultValue = "", string styleHeight = "", string styleWidth = "", bool readOnlyfield = false, bool allowActiveContent = false, string addonListJSON = "", string styleList = "", string styleOptionList = "", bool allowResourceLibrary = false) {
            string returnHtml = "";
            try {
                var fieldEditorAddonList = EditorController.getFieldEditorAddonList(core);
                Contensive.Processor.Addons.AdminSite.FieldTypeEditorAddonModel fieldEditor = fieldEditorAddonList.Find(x => x.fieldTypeId == (int)CPContentBaseClass.FieldTypeIdEnum.HTML);
                int FieldEditorAddonId = 0;
                if (fieldEditor != null) {
                    FieldEditorAddonId = fieldEditor.editorAddonId;
                }
                if (FieldEditorAddonId == 0) {
                    //
                    //    use default wysiwyg
                    returnHtml = inputTextarea(core, htmlName, DefaultValue);
                } else {
                    // todo ----- move this (defaulteditor override) to AdminUI
                    //
                    // use addon editor
                    Dictionary<string, string> arguments = new Dictionary<string, string> {
                        { "editorName", htmlName },
                        { "editorValue", DefaultValue },
                        { "editorFieldType", ((int)CPContentBaseClass.FieldTypeIdEnum.HTML).ToString() },
                        { "editorReadOnly", readOnlyfield.ToString() },
                        { "editorWidth", styleWidth },
                        { "editorHeight", styleHeight },
                        { "editorAllowResourceLibrary", allowResourceLibrary.ToString() },
                        { "editorAllowActiveContent", allowActiveContent.ToString() },
                        { "editorAddonList", addonListJSON },
                        { "editorStyles", styleList },
                        { "editorStyleOptions", styleOptionList }
                    };
                    var addon = core.cacheRuntime.addonCache.create(FieldEditorAddonId);
                    returnHtml = core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                        addonType = CPUtilsBaseClass.addonContext.ContextEditor,
                        argumentKeyValuePairs = arguments,
                        errorContextMessage = "calling editor addon for text field type, addon [" + FieldEditorAddonId + "]"
                    });
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnHtml;
        }
        //
        //====================================================================================================
        /// <summary>
        /// process requests from the legacy tool panel at the bottom of the page, and the replacement tool-bar at the top.
        /// Removed login processing and legacy tool panel
        /// </summary>
        public void processFormToolsPanel() {
            try {
                //
                // ----- Read in and save the Member profile values from the tools panel
                //
                if (core.session.user.id > 0) {
                    if (core.doc.userErrorList.Count.Equals(0)) {
                        string legacyFormSn = core.docProperties.getText("ccformsn");
                        string Button = core.docProperties.getText(legacyFormSn + "mb");
                        switch (Button) {
                            case ButtonLogout: {
                                    //
                                    // Logout - This can only come from the Horizonal Tool Bar
                                    //
                                    AuthenticationController.logout(core, core.session);
                                }
                                break;
                            case ButtonLogin: {
                                    //
                                    // Login - This can only come from the Horizonal Tool Bar
                                    //
                                    string requestUsername = core.cpParent.Doc.GetText(legacyFormSn + "username");
                                    string requestPassword = core.cpParent.Doc.GetText(legacyFormSn + "password");
                                    bool passwordRequestValid = core.cpParent.Doc.IsProperty(legacyFormSn + "password");
                                    LoginController.processLoginPage_Default(core, requestUsername, requestPassword, passwordRequestValid);
                                }
                                break;
                            case ButtonApply: {
                                    //
                                    // ----- AllowAdminLinks
                                    //
                                    core.visitProperty.setProperty("AllowEditing", encodeText(core.docProperties.getBoolean(legacyFormSn + "AllowEditing")));
                                    //
                                    // ----- Quick Editor
                                    //
                                    core.visitProperty.setProperty("AllowQuickEditor", encodeText(core.docProperties.getBoolean(legacyFormSn + "AllowQuickEditor")));
                                    //
                                    // ----- Advanced Editor
                                    //
                                    core.visitProperty.setProperty("AllowAdvancedEditor", encodeText(core.docProperties.getBoolean(legacyFormSn + "AllowAdvancedEditor")));
                                    //
                                    // ----- developer Only parts
                                    //
                                    core.visitProperty.setProperty("AllowDebugging", encodeText(core.docProperties.getBoolean(legacyFormSn + "AllowDebugging")));
                                }
                                break;
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //====================================================================================================
        //
        public void processAddonSettingsEditor() {
            //
            string constructor = null;
            bool ParseOK = false;
            int PosNameStart = 0;
            int PosNameEnd = 0;
            string AddonName = null;
            int OptionPtr = 0;
            string ArgValueAddonEncoded = null;
            int OptionCnt = 0;
            bool needToClearCache = false;
            string[] ConstructorSplit = null;
            int Ptr = 0;
            string[] Arg = null;
            string ArgName = null;
            string ArgValue = null;
            string AddonOptionConstructor = "";
            string addonOption_String = "";
            CPContentBaseClass.FieldTypeIdEnum fieldType = 0;
            string Copy = "";
            int RecordID = 0;
            string FieldName = null;
            string ACInstanceId = null;
            string ContentName = null;
            int PosACInstanceId = 0;
            int PosStart = 0;
            int PosIDStart = 0;
            int PosIDEnd = 0;
            //
            ContentName = core.docProperties.getText("ContentName");
            RecordID = core.docProperties.getInteger("RecordID");
            FieldName = core.docProperties.getText("FieldName");
            ACInstanceId = core.docProperties.getText("ACInstanceID");
            bool FoundAddon = false;
            if (ACInstanceId == PageChildListInstanceId) {
                //
                // ----- Page Content Child List Add-on
                //
                if (RecordID != 0) {
                    AddonModel addon = core.cacheRuntime.addonCache.create(addonGuidChildList);
                    if (addon != null) {
                        FoundAddon = true;
                        AddonOptionConstructor = addon.argumentList;
                        AddonOptionConstructor = GenericController.strReplace(AddonOptionConstructor, Environment.NewLine, "\r");
                        AddonOptionConstructor = GenericController.strReplace(AddonOptionConstructor, "\n", "\r");
                        AddonOptionConstructor = GenericController.strReplace(AddonOptionConstructor, "\r", Environment.NewLine);
                        {
                            if (!string.IsNullOrEmpty(AddonOptionConstructor)) {
                                AddonOptionConstructor = AddonOptionConstructor + Environment.NewLine;
                            }
                            if (addon.isInline) {
                                AddonOptionConstructor = AddonOptionConstructor + AddonOptionConstructor_Inline;
                            } else {
                                AddonOptionConstructor = AddonOptionConstructor + AddonOptionConstructor_Block;
                            }
                        }

                        ConstructorSplit = GenericController.stringSplit(AddonOptionConstructor, Environment.NewLine);
                        AddonOptionConstructor = "";
                        //
                        // main_Get all responses from current Argument List and build new addonOption_String
                        //
                        for (Ptr = 0; Ptr <= ConstructorSplit.GetUpperBound(0); Ptr++) {
                            Arg = ConstructorSplit[Ptr].Split('=');
                            ArgName = Arg[0];
                            OptionCnt = core.docProperties.getInteger(ArgName + "CheckBoxCnt");
                            if (OptionCnt > 0) {
                                ArgValueAddonEncoded = "";
                                for (OptionPtr = 0; OptionPtr < OptionCnt; OptionPtr++) {
                                    ArgValue = core.docProperties.getText(ArgName + OptionPtr);
                                    if (!string.IsNullOrEmpty(ArgValue)) {
                                        ArgValueAddonEncoded = ArgValueAddonEncoded + "," + GenericController.encodeNvaArgument(ArgValue);
                                    }
                                }
                                if (!string.IsNullOrEmpty(ArgValueAddonEncoded)) {
                                    ArgValueAddonEncoded = ArgValueAddonEncoded.Substring(1);
                                }
                            } else {
                                ArgValue = core.docProperties.getText(ArgName);
                                ArgValueAddonEncoded = GenericController.encodeNvaArgument(ArgValue);
                            }
                            addonOption_String = addonOption_String + "&" + GenericController.encodeNvaArgument(ArgName) + "=" + ArgValueAddonEncoded;
                        }
                        if (!string.IsNullOrEmpty(addonOption_String)) {
                            addonOption_String = addonOption_String.Substring(1);
                        }

                    }
                    core.db.executeNonQuery("update ccpagecontent set ChildListInstanceOptions=" + DbController.encodeSQLText(addonOption_String) + " where id=" + RecordID);
                    needToClearCache = true;
                }
            } else if ((ACInstanceId == "-2") && (!string.IsNullOrEmpty(FieldName))) {
                //
                // ----- Admin Addon, ACInstanceID=-2, FieldName=AddonName
                //
                AddonName = FieldName;
                FoundAddon = false;
                AddonModel addon = core.cacheRuntime.addonCache.createByUniqueName(AddonName);
                if (addon != null) {
                    FoundAddon = true;
                    AddonOptionConstructor = addon.argumentList;
                    AddonOptionConstructor = GenericController.strReplace(AddonOptionConstructor, Environment.NewLine, "\r");
                    AddonOptionConstructor = GenericController.strReplace(AddonOptionConstructor, "\n", "\r");
                    AddonOptionConstructor = GenericController.strReplace(AddonOptionConstructor, "\r", Environment.NewLine);
                    if (!string.IsNullOrEmpty(AddonOptionConstructor)) {
                        AddonOptionConstructor = AddonOptionConstructor + Environment.NewLine;
                    }
                    if (GenericController.encodeBoolean(addon.isInline)) {
                        AddonOptionConstructor = AddonOptionConstructor + AddonOptionConstructor_Inline;
                    } else {
                        AddonOptionConstructor = AddonOptionConstructor + AddonOptionConstructor_Block;
                    }
                }
                if (!FoundAddon) {
                    //
                    // Hardcoded Addons
                    //
                    switch (GenericController.toLCase(AddonName)) {
                        case "block text":
                            FoundAddon = true;
                            AddonOptionConstructor = AddonOptionConstructor_ForBlockText;
                            break;
                        case "":
                            break;
                    }
                }
                if (FoundAddon) {
                    ConstructorSplit = GenericController.stringSplit(AddonOptionConstructor, Environment.NewLine);
                    addonOption_String = "";
                    //
                    // main_Get all responses from current Argument List
                    //
                    for (Ptr = 0; Ptr <= ConstructorSplit.GetUpperBound(0); Ptr++) {
                        string nvp = ConstructorSplit[Ptr].Trim(' ');
                        if (!string.IsNullOrEmpty(nvp)) {
                            Arg = ConstructorSplit[Ptr].Split('=');
                            ArgName = Arg[0];
                            OptionCnt = core.docProperties.getInteger(ArgName + "CheckBoxCnt");
                            if (OptionCnt > 0) {
                                ArgValueAddonEncoded = "";
                                for (OptionPtr = 0; OptionPtr < OptionCnt; OptionPtr++) {
                                    ArgValue = core.docProperties.getText(ArgName + OptionPtr);
                                    if (!string.IsNullOrEmpty(ArgValue)) {
                                        ArgValueAddonEncoded = ArgValueAddonEncoded + "," + GenericController.encodeNvaArgument(ArgValue);
                                    }
                                }
                                if (!string.IsNullOrEmpty(ArgValueAddonEncoded)) {
                                    ArgValueAddonEncoded = ArgValueAddonEncoded.Substring(1);
                                }
                            } else {
                                ArgValue = core.docProperties.getText(ArgName);
                                ArgValueAddonEncoded = GenericController.encodeNvaArgument(ArgValue);
                            }
                            addonOption_String = addonOption_String + "&" + GenericController.encodeNvaArgument(ArgName) + "=" + ArgValueAddonEncoded;
                        }
                    }
                    if (!string.IsNullOrEmpty(addonOption_String)) {
                        addonOption_String = addonOption_String.Substring(1);
                    }
                    core.userProperty.setProperty("Addon [" + AddonName + "] Options", addonOption_String);
                    needToClearCache = true;
                }
            } else if (string.IsNullOrEmpty(ContentName) || RecordID == 0) {
                //
                // ----- Public Site call, must have contentname and recordid
                //
                logger.Error($"{core.logCommonMessage}", new Exception("invalid content [" + ContentName + "], RecordID [" + RecordID + "]"));
            } else {
                //
                // ----- Normal Content Edit - find instance in the content
                //
                using (var csData = new CsModel(core)) {
                    if (!csData.openRecord(ContentName, RecordID)) {
                        logger.Error($"{core.logCommonMessage}", new Exception("No record found with content [" + ContentName + "] and RecordID [" + RecordID + "]"));
                    } else {
                        if (!string.IsNullOrEmpty(FieldName)) {
                            //
                            // Field is given, find the position
                            //
                            Copy = csData.getText(FieldName);
                            PosACInstanceId = GenericController.strInstr(1, Copy, "=\"" + ACInstanceId + "\" ", 1);
                        } else {
                            //
                            // Find the field, then find the position
                            //
                            FieldName = csData.getFirstFieldName();
                            while (!string.IsNullOrEmpty(FieldName)) {
                                fieldType = csData.getFieldTypeId(FieldName);
                                switch (fieldType) {
                                    case CPContentBaseClass.FieldTypeIdEnum.LongText:
                                    case CPContentBaseClass.FieldTypeIdEnum.Text:
                                    case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                    case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                    case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                                    case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                                    case CPContentBaseClass.FieldTypeIdEnum.HTML:
                                    case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                                    case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                    case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                                        Copy = csData.getText(FieldName);
                                        PosACInstanceId = GenericController.strInstr(1, Copy, "ACInstanceID=\"" + ACInstanceId + "\"", 1);
                                        if (PosACInstanceId != 0) {
                                            //
                                            // found the instance
                                            PosACInstanceId = PosACInstanceId + 13;
                                            goto ExitLabel1;
                                        }
                                        break;
                                }
                                FieldName = csData.getNextFieldName();
                            }
                        ExitLabel1:;
                        }
                        //
                        // Parse out the Addon Name
                        //
                        if (PosACInstanceId == 0) {
                            logger.Error($"{core.logCommonMessage}", new Exception("AC Instance [" + ACInstanceId + "] not found in record with content [" + ContentName + "] and RecordID [" + RecordID + "]"));
                        } else {
                            Copy = ContentRenderController.updateLibraryFilesInHtmlContent(core, Copy);
                            ParseOK = false;
                            PosStart = Copy.LastIndexOf("<ac ", PosACInstanceId - 1, System.StringComparison.OrdinalIgnoreCase) + 1;
                            if (PosStart != 0) {
                                //
                                // main_Get Addon Name to lookup Addon and main_Get most recent Argument List
                                //
                                PosNameStart = GenericController.strInstr(PosStart, Copy, " name=", 1);
                                if (PosNameStart != 0) {
                                    PosNameStart = PosNameStart + 7;
                                    PosNameEnd = GenericController.strInstr(PosNameStart, Copy, "\"");
                                    if (PosNameEnd != 0) {
                                        AddonName = Copy.Substring(PosNameStart - 1, PosNameEnd - PosNameStart);
                                        //????? test this
                                        FoundAddon = false;
                                        AddonModel embeddedAddon = core.cacheRuntime.addonCache.createByUniqueName(AddonName);
                                        if (embeddedAddon != null) {
                                            FoundAddon = true;
                                            AddonOptionConstructor = GenericController.encodeText(embeddedAddon.argumentList);
                                            AddonOptionConstructor = GenericController.strReplace(AddonOptionConstructor, Environment.NewLine, "\r");
                                            AddonOptionConstructor = GenericController.strReplace(AddonOptionConstructor, "\n", "\r");
                                            AddonOptionConstructor = GenericController.strReplace(AddonOptionConstructor, "\r", Environment.NewLine);
                                            if (!string.IsNullOrEmpty(AddonOptionConstructor)) {
                                                AddonOptionConstructor = AddonOptionConstructor + Environment.NewLine;
                                            }
                                            if (GenericController.encodeBoolean(embeddedAddon.isInline)) {
                                                AddonOptionConstructor = AddonOptionConstructor + AddonOptionConstructor_Inline;
                                            } else {
                                                AddonOptionConstructor = AddonOptionConstructor + AddonOptionConstructor_Block;
                                            }
                                        } else {
                                            //
                                            // -- Hardcoded Addons
                                            switch (GenericController.toLCase(AddonName)) {
                                                case "block text":
                                                    FoundAddon = true;
                                                    AddonOptionConstructor = AddonOptionConstructor_ForBlockText;
                                                    break;
                                                case "":
                                                    break;
                                            }
                                        }
                                        if (FoundAddon) {
                                            ConstructorSplit = GenericController.stringSplit(AddonOptionConstructor, Environment.NewLine);
                                            addonOption_String = "";
                                            //
                                            // main_Get all responses from current Argument List
                                            //
                                            for (Ptr = 0; Ptr <= ConstructorSplit.GetUpperBound(0); Ptr++) {
                                                constructor = ConstructorSplit[Ptr];
                                                if (!string.IsNullOrEmpty(constructor)) {
                                                    Arg = constructor.Split('=');
                                                    ArgName = Arg[0];
                                                    OptionCnt = core.docProperties.getInteger(ArgName + "CheckBoxCnt");
                                                    if (OptionCnt > 0) {
                                                        ArgValueAddonEncoded = "";
                                                        for (OptionPtr = 0; OptionPtr < OptionCnt; OptionPtr++) {
                                                            ArgValue = core.docProperties.getText(ArgName + OptionPtr);
                                                            if (!string.IsNullOrEmpty(ArgValue)) {
                                                                ArgValueAddonEncoded = ArgValueAddonEncoded + "," + GenericController.encodeNvaArgument(ArgValue);
                                                            }
                                                        }
                                                        if (!string.IsNullOrEmpty(ArgValueAddonEncoded)) {
                                                            ArgValueAddonEncoded = ArgValueAddonEncoded.Substring(1);
                                                        }
                                                    } else {
                                                        ArgValue = core.docProperties.getText(ArgName);
                                                        ArgValueAddonEncoded = GenericController.encodeNvaArgument(ArgValue);
                                                    }

                                                    addonOption_String = addonOption_String + "&" + GenericController.encodeNvaArgument(ArgName) + "=" + ArgValueAddonEncoded;
                                                }
                                            }
                                            if (!string.IsNullOrEmpty(addonOption_String)) {
                                                addonOption_String = addonOption_String.Substring(1);
                                            }
                                        }
                                    }
                                }
                                //
                                // Replace the new querystring into the AC tag in the content
                                //
                                PosIDStart = GenericController.strInstr(PosStart, Copy, " querystring=", 1);
                                if (PosIDStart != 0) {
                                    PosIDStart = PosIDStart + 14;
                                    if (PosIDStart != 0) {
                                        PosIDEnd = GenericController.strInstr(PosIDStart, Copy, "\"");
                                        if (PosIDEnd != 0) {
                                            ParseOK = true;
                                            Copy = Copy.left(PosIDStart - 1) + HtmlController.encodeHtml(addonOption_String) + Copy.Substring(PosIDEnd - 1);
                                            csData.set(FieldName, Copy);
                                            needToClearCache = true;
                                        }
                                    }
                                }
                            }
                            if (!ParseOK) {
                                logger.Error($"{core.logCommonMessage}", new Exception("There was a problem parsing AC Instance [" + ACInstanceId + "] record with content [" + ContentName + "] and RecordID [" + RecordID + "]"));
                            }
                        }
                    }
                }
            }
            if (needToClearCache) {
                //
                // Clear Caches
                //
                if (!string.IsNullOrEmpty(ContentName)) {
                    string contentTablename = MetadataController.getContentTablename(core, ContentName);
                    core.cache.invalidateTableDependencyKey(contentTablename);
                }
            }
        }
        //
        //====================================================================================================
        //
        public void processHelpBubbleEditor() {
            //
            string SQL = null;
            string HelpBubbleId = null;
            string[] IDSplit = null;
            int RecordID = 0;
            string HelpCaption = null;
            string HelpMessage = null;
            //
            HelpBubbleId = core.docProperties.getText("HelpBubbleID");
            IDSplit = HelpBubbleId.Split('-');
            switch (GenericController.toLCase(IDSplit[0])) {
                case "userfield":
                    //
                    // main_Get the id of the field, and save the input as the caption and help
                    //
                    if (IDSplit.GetUpperBound(0) > 0) {
                        RecordID = GenericController.encodeInteger(IDSplit[1]);
                        if (RecordID > 0) {
                            HelpCaption = core.docProperties.getText("helpcaption");
                            HelpMessage = core.docProperties.getText("helptext");
                            SQL = "update ccfields set caption=" + DbController.encodeSQLText(HelpCaption) + ",HelpMessage=" + DbController.encodeSQLText(HelpMessage) + " where id=" + RecordID;
                            core.db.executeNonQuery(SQL);
                            core.cache.invalidateAll();
                            core.cacheRuntime.clear();
                        }
                    }
                    break;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string getCheckList(string htmlNamePrefix, string PrimaryContentName, int PrimaryRecordID, string SecondaryContentName, string RulesContentName, string RulesPrimaryFieldname, string RulesSecondaryFieldName) {
            return getCheckList(htmlNamePrefix, PrimaryContentName, PrimaryRecordID, SecondaryContentName, RulesContentName, RulesPrimaryFieldname, RulesSecondaryFieldName, "", "", false, false, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string getCheckList(string htmlNamePrefix, string PrimaryContentName, int PrimaryRecordID, string SecondaryContentName, string RulesContentName, string RulesPrimaryFieldname, string RulesSecondaryFieldName, string SecondaryContentSelectCriteria) {
            return getCheckList(htmlNamePrefix, PrimaryContentName, PrimaryRecordID, SecondaryContentName, RulesContentName, RulesPrimaryFieldname, RulesSecondaryFieldName, SecondaryContentSelectCriteria, "", false, false, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string getCheckList(string htmlNamePrefix, string PrimaryContentName, int PrimaryRecordID, string SecondaryContentName, string RulesContentName, string RulesPrimaryFieldname, string RulesSecondaryFieldName, string SecondaryContentSelectCriteria, string CaptionFieldName) {
            return getCheckList(htmlNamePrefix, PrimaryContentName, PrimaryRecordID, SecondaryContentName, RulesContentName, RulesPrimaryFieldname, RulesSecondaryFieldName, SecondaryContentSelectCriteria, CaptionFieldName, false, false, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string getCheckList(string htmlNamePrefix, string PrimaryContentName, int PrimaryRecordID, string SecondaryContentName, string RulesContentName, string RulesPrimaryFieldname, string RulesSecondaryFieldName, string SecondaryContentSelectCriteria, string CaptionFieldName, bool readOnlyfield) {
            return getCheckList(htmlNamePrefix, PrimaryContentName, PrimaryRecordID, SecondaryContentName, RulesContentName, RulesPrimaryFieldname, RulesSecondaryFieldName, SecondaryContentSelectCriteria, CaptionFieldName, readOnlyfield, false, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// list of checkbox options based on a standard set of rules.
        ///   IncludeContentFolderDivs
        ///       When true, the list of options (checkboxes) are grouped by ContentFolder and wrapped in a Div with ID="ContentFolder99"
        ///   For instance, list out a options of all public groups, with the ones checked that this member belongs to
        ///       PrimaryContentName = "People"
        ///       PrimaryRecordId = MemberID
        ///       SecondaryContentName = "Groups"
        ///       SecondaryContentSelectCriteria = "ccGroups.PublicJoin not equal 0"
        ///       RulesContentName = "Member Rules"
        ///       RulesPrimaryFieldName = "MemberID"
        ///       RulesSecondaryFieldName = "GroupID"
        /// </summary>
        /// <param name="htmlNamePrefix"></param>
        /// <param name="primaryContentName"></param>
        /// <param name="primaryRecordID"></param>
        /// <param name="secondaryContentName"></param>
        /// <param name="rulesContentName"></param>
        /// <param name="rulesPrimaryFieldname"></param>
        /// <param name="rulesSecondaryFieldName"></param>
        /// <param name="secondaryContentSelectCriteria"></param>
        /// <param name="captionFieldName"></param>
        /// <param name="readOnlyfield"></param>
        /// <param name="IncludeContentFolderDivs"></param>
        /// <param name="defaultSecondaryIDList"></param>
        /// <returns></returns>
        public string getCheckList(string htmlNamePrefix, string primaryContentName, int primaryRecordID, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectCriteria, string captionFieldName, bool readOnlyfield, bool IncludeContentFolderDivs, string defaultSecondaryIDList) {
            try {
                StringBuilder result = new("");
                //
                // ----- argument validate
                captionFieldName = encodeEmpty(captionFieldName, "name");
                if (string.IsNullOrEmpty(primaryContentName) || string.IsNullOrEmpty(secondaryContentName) || string.IsNullOrEmpty(rulesContentName) || string.IsNullOrEmpty(rulesPrimaryFieldname) || string.IsNullOrEmpty(rulesSecondaryFieldName)) {
                    logger.Error($"{core.logCommonMessage}", new Exception("Creating checklist, all required fields were not supplied, Caption=[" + captionFieldName + "], PrimaryContentName=[" + primaryContentName + "], SecondaryContentName=[" + secondaryContentName + "], RulesContentName=[" + rulesContentName + "], RulesPrimaryFieldName=[" + rulesPrimaryFieldname + "], RulesSecondaryFieldName=[" + rulesSecondaryFieldName + "]"));
                    return "[Checklist not configured]";
                }
                var secondaryMeta = ContentMetadataModel.createByUniqueName(core, secondaryContentName);
                if (secondaryMeta == null) {
                    logger.Error($"{core.logCommonMessage}", new Exception("Creating checklist, secondary content was not valid, Caption=[" + captionFieldName + "], PrimaryContentName=[" + primaryContentName + "], SecondaryContentName=[" + secondaryContentName + "], RulesContentName=[" + rulesContentName + "], RulesPrimaryFieldName=[" + rulesPrimaryFieldname + "], RulesSecondaryFieldName=[" + rulesSecondaryFieldName + "]"));
                    return "[Checklist not configured]";
                }
                var rulesMeta = ContentMetadataModel.createByUniqueName(core, rulesContentName);
                if (rulesMeta == null) {
                    logger.Error($"{core.logCommonMessage}", new Exception("Creating checklist, rule content was not valid, Caption=[" + captionFieldName + "], PrimaryContentName=[" + primaryContentName + "], SecondaryContentName=[" + secondaryContentName + "], RulesContentName=[" + rulesContentName + "], RulesPrimaryFieldName=[" + rulesPrimaryFieldname + "], RulesSecondaryFieldName=[" + rulesSecondaryFieldName + "]"));
                    return "[Checklist not configured]";
                }
                //
                // -- build rule fields
                List<ContentFieldMetadataModel> ruleEditFields = getRuleEditFields(rulesMeta, rulesPrimaryFieldname, rulesSecondaryFieldName);
                //
                // -- columns, up to 4 supported, if rules=0, class=col-12, if rules=1, class=col-6, rules=2, class=col-4
                int colCnt = (ruleEditFields.Count > 3 ? 4 : ruleEditFields.Count + 1);
                string colClass = "col-" + Convert.ToInt32(12 / colCnt).ToString();
                //
                // -- get contentcontrolid list
                var contentControlIdList = new List<int>();
                if (secondaryMeta.parentId <= 0) {
                    //
                    // -- if content has no parent, include all contentcontrolid==0
                    contentControlIdList.Add(0);
                }
                contentControlIdList.Add(secondaryMeta.id);
                contentControlIdList.AddRange(secondaryMeta.childIdList(core));
                string singularPrefixHtmlEncoded = HtmlController.encodeHtml(GenericController.getSingular_Sortof(secondaryContentName)) + "&nbsp;";
                //
                if ((!string.IsNullOrEmpty(secondaryMeta.tableName)) && (!string.IsNullOrEmpty(rulesMeta.tableName))) {
                    string jsLegacy = "var OldFolder" + core.doc.checkListCnt + ";";
                    Dictionary<int, RuleIdSecondaryIdModel> secondaryIdDict = new();
                    if (primaryRecordID == 0) {
                        //
                        // -- New record, prepopulate with defaults from DefaultSecondaryIDList
                        foreach (var idtext in defaultSecondaryIDList.Split(',')) {
                            int id = encodeInteger(idtext);
                            if (id == 0) { continue; }
                            if (secondaryIdDict.ContainsKey(id)) { continue; }
                            secondaryIdDict.Add(id, new RuleIdSecondaryIdModel() { ruleId = id, secondaryId = 0 });
                        }
                    } else {
                        //
                        // ----- Determine which secondary records are associated by a rule
                        // ----- (exclude new record issue ID=0)
                        string sql = ""
                            + "SELECT "
                                + secondaryMeta.tableName + ".id AS secondaryId," + rulesMeta.tableName + ".id as ruleId"
                            + " FROM "
                                + secondaryMeta.tableName + " LEFT JOIN"
                                + " " + rulesMeta.tableName + " ON " + secondaryMeta.tableName + ".Id = " + rulesMeta.tableName + "." + rulesSecondaryFieldName
                            + " WHERE "
                                + " (" + rulesMeta.tableName + "." + rulesPrimaryFieldname + "=" + primaryRecordID + ")"
                                + " AND (" + rulesMeta.tableName + ".Active<>0)"
                                + " AND (" + secondaryMeta.tableName + ".Active<>0)"
                                + " And (" + secondaryMeta.tableName + ".ContentControlID IN (" + string.Join(",", contentControlIdList) + "))"
                                + (!string.IsNullOrEmpty(secondaryContentSelectCriteria) ? "AND(" + secondaryContentSelectCriteria + ")" : "");
                        using DataTable dt = core.cpParent.Db.ExecuteQuery(sql);
                        if (dt?.Rows != null) {
                            foreach (DataRow row in dt.Rows) {
                                int secondaryId = GenericController.encodeInteger(row["secondaryId"]);
                                if (secondaryIdDict.ContainsKey(secondaryId)) { continue; }
                                secondaryIdDict.Add(secondaryId, new RuleIdSecondaryIdModel() { secondaryId = secondaryId, ruleId = GenericController.encodeInteger(row["ruleId"]) });
                            }
                        }
                    }
                    //
                    // -- add column headers if there ruleFields
                    if (ruleEditFields.Count > 0) {
                        //
                        // -- build a bootstrap row
                        result.Append("<div class=\"row pb-1\">");
                        //
                        // -- first column has no label
                        result.Append("<div class=\"" + colClass + "\">&nbsp;</div>");
                        //
                        // -- columns from rule
                        foreach (var ruleField in ruleEditFields) {
                            result.Append("<div class=\"" + colClass + "\">" + ruleField.caption + "</div>");
                        }
                        //
                        // -- end of row
                        result.Append("</div>");
                    }
                    //
                    // -- Gather all the Secondary Records, sorted by ContentName
                    string sqlSecondaryRecords = ""
                        + "select "
                            + secondaryMeta.tableName + ".id, "
                            + secondaryMeta.tableName + "." + captionFieldName + " as OptionCaption, "
                            + secondaryMeta.tableName + ".name AS OptionName, "
                            + secondaryMeta.tableName + ".SortOrder"
                        + " from "
                            + secondaryMeta.tableName + " where (1=1)" + ((!string.IsNullOrEmpty(secondaryContentSelectCriteria)) ? "AND(" + secondaryContentSelectCriteria + ")" : "")
                        + " group by "
                            + secondaryMeta.tableName + ".id, "
                            + secondaryMeta.tableName + "." + captionFieldName + ", "
                            + secondaryMeta.tableName + ".name, "
                            + secondaryMeta.tableName + ".SortOrder"
                        + " order by "
                            + secondaryMeta.tableName + "." + captionFieldName;
                    using (var csData = new CsModel(core)) {
                        if (!csData.openSql(sqlSecondaryRecords)) {
                            result.Append("(No choices are available.)");
                        } else {
                            int checkBoxPtr = 0;
                            bool CanSeeHiddenFields = core.session.isAuthenticatedDeveloper();
                            string DivName = htmlNamePrefix + ".All";
                            bool isAdmin = !core.webServer.requestPathPage.IndexOf(core.siteProperties.getText("adminUrl"), System.StringComparison.OrdinalIgnoreCase).Equals(-1);
                            string editLinkTemplate = !isAdmin ? "" : AdminUIEditButtonController.getRecordEditAnchorTag(core, secondaryMeta, -1, "", "");
                            while (csData.ok()) {
                                string OptionName = csData.getText("OptionName");
                                if ((OptionName.left(1) != "_") || CanSeeHiddenFields) {
                                    //
                                    // Current checkbox is visible
                                    int secondaryId = csData.getInteger("ID");
                                    string OptionCaption = csData.getText("OptionCaption");
                                    if (string.IsNullOrEmpty(OptionCaption)) {
                                        OptionCaption = OptionName;
                                    }
                                    string optionCaptionHtmlEncoded = (!isAdmin ? "" : "&nbsp;&nbsp;" + editLinkTemplate.Replace("-1", secondaryId.ToString()));
                                    if (string.IsNullOrEmpty(OptionCaption)) {
                                        optionCaptionHtmlEncoded += "&nbsp;" + singularPrefixHtmlEncoded + secondaryId;
                                    } else {
                                        optionCaptionHtmlEncoded += "&nbsp;" + encodeHtml(OptionCaption);
                                    }
                                    bool ruleFound = secondaryIdDict.ContainsKey(secondaryId);
                                    //
                                    // -- build a bootstrap row
                                    result.Append("<div class=\"row pb-1\">");
                                    //
                                    // -- first column is checkbox and label
                                    result.Append("<div class=\"" + colClass + "\">");
                                    result.Append("<input type=hidden name=\"" + htmlNamePrefix + "." + checkBoxPtr + ".id\" value=" + secondaryId + ">");
                                    if (readOnlyfield && !ruleFound) {
                                        //
                                        // -- unchecked, disabled
                                        result.Append("<div class=\"checkbox\"><label><input type=checkbox disabled>" + optionCaptionHtmlEncoded + "</label></div>");
                                    } else if (readOnlyfield) {
                                        //
                                        // -- checked, disabled
                                        result.Append("<div class=\"checkbox\"><label><input type=checkbox disabled checked>" + optionCaptionHtmlEncoded + "</label></div>");
                                        result.Append("<input type=\"hidden\" name=\"" + htmlNamePrefix + "." + checkBoxPtr + ".id\" value=" + secondaryId + ">");
                                    } else if (ruleFound) {
                                        //
                                        // -- checked
                                        result.Append("<div class=\"checkbox\"><label><input type=checkbox name=\"" + htmlNamePrefix + "." + checkBoxPtr + "\" value=\"1\" checked>" + optionCaptionHtmlEncoded + "</label></div>");
                                    } else {
                                        //
                                        // -- unchecked
                                        result.Append("<div class=\"checkbox\"><label><input type=\"checkbox\" name=\"" + htmlNamePrefix + "." + checkBoxPtr + "\" value=\"1\">" + optionCaptionHtmlEncoded + "</label></div>");
                                    }
                                    result.Append("</div>");
                                    //
                                    // -- include additional columns from rules
                                    using (CPCSBaseClass ruleCs = core.cpParent.CSNew()) {
                                        if (ruleFound) {
                                            ruleCs.OpenRecord(rulesMeta.name, secondaryIdDict[secondaryId].ruleId);
                                        }
                                        foreach (var ruleField in ruleEditFields) {
                                            result.Append("<div class=\"" + colClass + "\">");
                                            //
                                            string htmlNameRuleField = htmlNamePrefix + "." + checkBoxPtr + "." + ruleField.nameLc;
                                            switch (ruleField.fieldTypeId) {
                                                case FieldTypeIdEnum.Text: {
                                                        string htmlValue = (!ruleCs.OK() || string.IsNullOrEmpty(ruleCs.GetText(ruleField.nameLc)) ? "" : ruleCs.GetText(ruleField.nameLc));
                                                        result.Append(inputText(core, htmlNameRuleField, htmlValue));
                                                        break;
                                                    }
                                                case FieldTypeIdEnum.Date: {
                                                        DateTime? htmlValue = (!ruleCs.OK() || string.IsNullOrEmpty(ruleCs.GetText(ruleField.nameLc)) ? null : ruleCs.GetDate(ruleField.nameLc));
                                                        result.Append(inputDate(core, htmlNameRuleField, htmlValue));
                                                        break;
                                                    }
                                                case FieldTypeIdEnum.Currency:
                                                case FieldTypeIdEnum.Float: {
                                                        double? htmlValue = (!ruleCs.OK() || string.IsNullOrEmpty(ruleCs.GetText(ruleField.nameLc)) ? null : ruleCs.GetNumber(ruleField.nameLc));
                                                        result.Append(inputNumber(core, htmlNameRuleField, htmlValue));
                                                        break;
                                                    }
                                                case FieldTypeIdEnum.Integer: {
                                                        int? htmlValue = (!ruleCs.OK() || string.IsNullOrEmpty(ruleCs.GetText(ruleField.nameLc)) ? null : ruleCs.GetInteger(ruleField.nameLc));
                                                        result.Append(inputInteger(core, htmlNameRuleField, htmlValue));
                                                        break;
                                                    }
                                                case FieldTypeIdEnum.Boolean: {
                                                        bool htmlValue = (!ruleCs.OK() || string.IsNullOrEmpty(ruleCs.GetText(ruleField.nameLc)) ? false : ruleCs.GetBoolean(ruleField.nameLc));
                                                        result.Append(checkbox(htmlNameRuleField, ruleCs.OK() ? (ruleCs.GetBoolean(ruleField.nameLc) ? "1" : "0") : null));
                                                        break;
                                                    }
                                            }
                                            //
                                            result.Append("</div>");
                                        }
                                    }
                                    //
                                    // -- end of row
                                    result.Append("</div>");
                                    checkBoxPtr++;
                                }
                                csData.goNext();
                            }
                            result.Append(inputHidden(htmlNamePrefix + ".RowCount", checkBoxPtr));
                        }
                    }
                    addScriptCode(jsLegacy, "CheckList Categories");
                }
                core.doc.checkListCnt = core.doc.checkListCnt + 1;
                return result.ToString();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="stylePanel"></param>
        /// <param name="styleHilite"></param>
        /// <param name="styleShadow"></param>
        /// <param name="width"></param>
        /// <param name="padding"></param>
        /// <param name="heightMin"></param>
        /// <returns></returns>
        public string getPanel(string content, string stylePanel, string styleHilite, string styleShadow, string width, int padding, int heightMin) {
            string ContentPanelWidth = "";
            string contentPanelWidthStyle = "";
            if (width.isNumeric()) {
                ContentPanelWidth = (int.Parse(width) - 2).ToString();
                contentPanelWidthStyle = ContentPanelWidth + "px";
            } else {
                ContentPanelWidth = "100%";
                contentPanelWidthStyle = ContentPanelWidth;
            }
            //
            string s0 = ""
                + "<td style=\"padding:" + padding + "px;vertical-align:top\" class=\"" + stylePanel + "\">"
                + content
                + "</td>"
                + "";
            //
            string s1 = ""
                + "<tr>"
                + s0
                + "</tr>"
                + "";
            string s2 = ""
                + "<table style=\"width:" + contentPanelWidthStyle + ";border:0px;\" class=\"" + stylePanel + "\" cellspacing=\"0\">"
                + s1
                + "</table>"
                + "";
            string s3 = ""
                + "<td colspan=\"3\" width=\"" + ContentPanelWidth + "\" valign=\"top\" align=\"left\" class=\"" + stylePanel + "\">"
                + s2
                + "</td>"
                + "";
            string s4 = ""
                + "<tr>"
                + s3
                + "</tr>"
                + "";
            string result = ""
                + "<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"" + width + "\" class=\"" + stylePanel + "\">"
                + s4
                + "</table>"
                + "";
            return result;
        }
        //
        public string getPanel(string content) => getPanel(content, "ccPanel", "ccPanelHilite", "ccPanelShadow", "100%", 5, 1);
        //
        public string getPanel(string content, string stylePanel, string styleHilite, string styleShadow, string width, int padding)
            => getPanel(content, stylePanel, styleHilite, styleShadow, width, padding, 1);
        //
        //====================================================================================================
        //
        public string getPanelHeader(string RightSideMessage) {
            return AdminUIController.getHeader(core, "", RightSideMessage, "");
        }
        //
        //====================================================================================================
        //
        public string getPanelTop(string StylePanel = "", string StyleHilite = "", string StyleShadow = "", string Width = "", string Padding = "", string HeightMin = "") {
            string MyWidth = encodeEmpty(Width, "100%");
            string MyPadding = encodeEmpty(Padding, "5");
            string ContentPanelWidth = (MyWidth.isNumeric()) ? (int.Parse(MyWidth) - 2).ToString() : "100%";
            return ""
                + "\r<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"" + MyWidth + "\">"
                + cr2 + "<tr>"
                + cr3 + "<td colspan=\"3\" width=\"" + ContentPanelWidth + "\" valign=\"top\" align=\"left\">"
                + cr4 + "<table border=\"0\" cellpadding=\"" + MyPadding + "\" cellspacing=\"0\" width=\"" + ContentPanelWidth + "\">"
                + cr5 + "<tr>"
                + cr6 + "<td valign=\"top\">";
        }
        //
        //====================================================================================================
        //
        public string getPanelBottom() {
            return ""
                + cr6 + "</td>"
                + cr5 + "</tr>"
                + cr4 + "</table>"
                + cr3 + "</td>"
                + cr2 + "</tr>"
                + "\r</table>";
        }
        //
        //====================================================================================================
        //
        public string getPanelButtons(string leftButtonCommaList, string rightButtonCommaList) {
            string leftButtonHtml = (string.IsNullOrWhiteSpace(leftButtonCommaList)) ? "" : AdminUIController.getButtonHtmlFromCommaList(core, leftButtonCommaList, true, true, RequestNameButton);
            string rightButtonHtml = (string.IsNullOrWhiteSpace(rightButtonCommaList)) ? "" : AdminUIController.getButtonHtmlFromCommaList(core, rightButtonCommaList, false, false, RequestNameButton);
            return AdminUIController.getSectionButtonBar(core, leftButtonHtml, rightButtonHtml);
        }
        //
        public string getPanelButtons(string leftButtonCommaList) => getPanelButtons(leftButtonCommaList, "");
        //
        //====================================================================================================
        //
        public string getPanelInput(string PanelContent, string PanelWidth = "", string PanelHeightMin = "1") {
            return getPanel(PanelContent, "ccPanelInput", "ccPanelShadow", "ccPanelHilite", PanelWidth, 2, GenericController.encodeInteger(PanelHeightMin));
        }
        //
        //=================================================================================================================
        /// <summary>
        /// return value from name/value special form
        /// </summary>
        /// <param name="OptionName"></param>
        /// <param name="addonOptionString"></param>
        /// <returns></returns>
        public static string getAddonOptionStringValue(string OptionName, string addonOptionString) {
            string result = GenericController.getSimpleNameValue(OptionName, addonOptionString, "", "&");
            int Pos = GenericController.strInstr(1, result, "[");
            if (Pos > 0) {
                result = result.left(Pos - 1);
            }
            return encodeText(GenericController.decodeNvaArgument(result)).Trim(' ');
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create the full html doc from the accumulated elements
        /// </summary>
        /// <param name="htmlBody"></param>
        /// <param name="htmlBodyTag"></param>
        /// <param name="allowLogin"></param>
        /// <param name="allowTools"></param>
        /// <param name="blockNonContentExtras"></param>
        /// <param name="isAdminSite"></param>
        /// <returns></returns>
        public string getHtmlDoc(string htmlBody, string htmlBodyTag, bool allowLogin = true, bool allowTools = true) {
            string result = "";
            try {
                string htmlHead = getHtmlHead();
                string htmlBeforeEndOfBody = getHtmlBodyEnd(allowLogin, allowTools);
                //
                // -- add user errors that were not handled during page process
                if (!core.doc.userErrorList.Count.Equals(0)) {
                    htmlBody = HtmlController.div(ErrorController.getUserError(core), "ccAdminMsg") + htmlBody;
                }
                //
                // -- assemble html document
                result = ""
                    + core.siteProperties.docTypeDeclaration
                    + Environment.NewLine + "<html lang=\"en-US\">"
                      + Environment.NewLine + "<head>"
                    + Environment.NewLine + htmlHead
                    + Environment.NewLine + "</head>"
                    + Environment.NewLine + htmlBodyTag + htmlBody + htmlBeforeEndOfBody + Environment.NewLine + "</body>"
                    + Environment.NewLine + "</html>"
                    + "";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public string getHtmlHead() {
            List<string> headList = [];
            try {
                //
                // -- meta content
                bool allowDebug = core.doc.visitPropertyAllowDebugging;
                if (core.doc.htmlMetaContent_TitleList.Count > 0) {
                    string content = "";
                    string addedByMessage = "";
                    foreach (var asset in core.doc.htmlMetaContent_TitleList.FindAll((a) => !string.IsNullOrWhiteSpace(a.content))) {
                        string testContent = content + ", " + asset.content;
                        if (!string.IsNullOrWhiteSpace(content) && testContent.Length > 60) { break; }
                        //
                        // -- allow if resulting title length <= 60
                        content = testContent;
                        if (allowDebug && !string.IsNullOrWhiteSpace(asset.addedByMessage) && !addedByMessage.Contains(asset.addedByMessage)) {
                            addedByMessage += asset.addedByMessage + " ";
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(content)) {
                        if (!string.IsNullOrWhiteSpace(addedByMessage)) { headList.Add(getAddedByComment(addedByMessage)); }
                        headList.Add("<title>" + HtmlController.encodeHtml(content.Substring(2)) + "</title>");
                    }
                }
                if (core.doc.htmlMetaContent_KeyWordList.Count > 0) {
                    string content = "";
                    string addedByMessage = "";
                    foreach (var asset in core.doc.htmlMetaContent_KeyWordList.FindAll((a) => !string.IsNullOrWhiteSpace(a.content))) {
                        content += "," + asset.content;
                        if (allowDebug && !string.IsNullOrWhiteSpace(asset.addedByMessage) && !addedByMessage.Contains(asset.addedByMessage)) {
                            addedByMessage += asset.addedByMessage + " ";
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(content)) {
                        if (!string.IsNullOrWhiteSpace(addedByMessage)) { headList.Add(getAddedByComment(addedByMessage)); }
                        headList.Add("<meta name=\"keywords\" content=\"" + HtmlController.encodeHtml(content.Substring(1)) + "\" >");
                    }
                }
                if (core.doc.htmlMetaContent_Description.Count > 0) {
                    string content = "";
                    foreach (var asset in core.doc.htmlMetaContent_Description.FindAll((a) => !string.IsNullOrWhiteSpace(a.content))) {
                        if (allowDebug && !string.IsNullOrWhiteSpace(asset.addedByMessage)) {
                            headList.Add(getAddedByComment(asset.addedByMessage));
                        }
                        content += "," + asset.content;
                    }
                    headList.Add("<meta name=\"description\" content=\"" + HtmlController.encodeHtml(content.Substring(1)) + "\" >");
                }
                //
                // -- favicon
                string VirtualFilename = core.siteProperties.getText("faviconfilename");
                switch (Path.GetExtension(VirtualFilename).ToLowerInvariant()) {
                    case ".ico":
                        if (allowDebug) { headList.Add(getAddedByComment("site setting favicon upload")); };
                        headList.Add("<link rel=\"icon\" type=\"image/vnd.microsoft.icon\" href=\"" + GenericController.getCdnFileLink(core, VirtualFilename) + "\" >");
                        break;
                    case ".png":
                        if (allowDebug) { headList.Add(getAddedByComment("site setting favicon upload")); };
                        headList.Add("<link rel=\"icon\" type=\"image/png\" href=\"" + GenericController.getCdnFileLink(core, VirtualFilename) + "\" >");
                        break;
                    case ".gif":
                        if (allowDebug) { headList.Add(getAddedByComment("site setting favicon upload")); };
                        headList.Add("<link rel=\"icon\" type=\"image/gif\" href=\"" + GenericController.getCdnFileLink(core, VirtualFilename) + "\" >");
                        break;
                    case ".jpg":
                        if (allowDebug) { headList.Add(getAddedByComment("site setting favicon upload")); };
                        headList.Add("<link rel=\"icon\" type=\"image/jpg\" href=\"" + GenericController.getCdnFileLink(core, VirtualFilename) + "\" >");
                        break;
                }
                headList.Add("<meta name=\"generator\" content=\"Contensive\">");
                //
                // -- no-follow
                if (core.doc.noFollow) {
                    headList.Add("<meta name=\"robots\" content=\"nofollow\" >");
                    headList.Add("<meta name=\"robots\" content=\"noindex\" >");
                    headList.Add("<meta name=\"mssmarttagspreventparsing\" content=\"true\" >");
                }
                //
                // -- css and js
                // -- only select assets with .inHead, which includes those whose depencies are .inHead
                if (core.doc.htmlAssetList.Count > 0) {
                    List<string> headScriptList = new List<string>();
                    List<string> styleList = new List<string>();
                    foreach (var asset in core.doc.htmlAssetList.FindAll((CPDocBaseClass.HtmlAssetClass item) => (item.inHead))) {
                        if (string.IsNullOrEmpty(asset.content)) { continue; }
                        if (asset.assetType.Equals(CPDocBaseClass.HtmlAssetTypeEnum.style)) {
                            if (allowDebug && !string.IsNullOrWhiteSpace(asset.addedByMessage)) {
                                styleList.Add(getAddedByComment(asset.addedByMessage));
                            }
                            if (asset.isLink) {
                                if (asset.content.Trim().Substring(0, 1) == "<") {
                                    styleList.Add(asset.content);
                                } else {
                                    styleList.Add("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + asset.content + "\" >");
                                }
                            } else {
                                styleList.Add("<style>" + asset.content + "</style>");
                            }
                        } else if (asset.assetType.Equals(CPDocBaseClass.HtmlAssetTypeEnum.script)) {
                            if (allowDebug && !string.IsNullOrWhiteSpace(asset.addedByMessage)) {
                                headScriptList.Add(getAddedByComment(asset.addedByMessage));
                            }
                            if (asset.isLink) {
                                if (asset.content.Trim().Substring(0, 1) == "<") {
                                    headScriptList.Add(asset.content);
                                } else {
                                    headScriptList.Add("<script type=\"text/javascript\" src=\"" + asset.content + "\"></script>");
                                }
                            } else {
                                headScriptList.Add("<script type=\"text/javascript\">" + asset.content + "</script>");
                            }
                        }
                    }
                    headList.AddRange(styleList);
                    headList.AddRange(headScriptList);
                }
                //
                // -- other head tags - always last
                foreach (var asset in core.doc.htmlMetaContent_OtherTags.FindAll((a) => (!string.IsNullOrEmpty(a.content)))) {
                    if (allowDebug && !string.IsNullOrWhiteSpace(asset.addedByMessage)) {
                        headList.Add(getAddedByComment(asset.addedByMessage, "Other Head Tags"));
                    }
                    headList.Add(asset.content);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return string.Join(Environment.NewLine, headList);
        }
        //
        //====================================================================================================
        //
        public void addScriptCode_onLoad(string code, string addedByMessage) {
            try {
                if (!string.IsNullOrEmpty(code)) {
                    core.doc.htmlAssetList.Add(new CPDocBaseClass.HtmlAssetClass {
                        assetType = CPDocBaseClass.HtmlAssetTypeEnum.scriptOnLoad,
                        addedByMessage = addedByMessage,
                        isLink = false,
                        content = code
                    });
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //====================================================================================================
        //
        public void addScriptCode(string code, string addedByMessage, bool forceHead = false, int sourceAddonId = 0) {
            try {
                if (string.IsNullOrWhiteSpace(code)) { return; }
                string codeNormalized = removeScriptTag(code);
                //
                CPDocBaseClass.HtmlAssetClass asset = core.doc.htmlAssetList.Find(t => (t.content == codeNormalized) && (!t.isLink));
                if (asset != null) {
                    //
                    // already in list, just mark it forceHead
                    asset.inHead = asset.inHead || forceHead;
                    return;
                }
                //
                // add to list
                core.doc.htmlAssetList.Add(new CPDocBaseClass.HtmlAssetClass {
                    assetType = CPDocBaseClass.HtmlAssetTypeEnum.script,
                    inHead = forceHead,
                    addedByMessage = addedByMessage,
                    isLink = false,
                    content = codeNormalized,
                    sourceAddonId = sourceAddonId
                });
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //=========================================================================================================
        /// <summary>
        /// Add a javascript link to the document
        /// </summary>
        /// <param name="scriptUrl">Link to the document. Should start with either 'http', or '/'</param>
        /// <param name="addedByMessage">message displayed in debug mode</param>
        /// <param name="forceHead">if true, this document tag goes in the head, else at the end of body</param>
        /// <param name="sourceAddonId">optional, the addon that supplied this javascript</param>
        public void addScriptLinkSrc(string scriptUrl, string addedByMessage, bool forceHead, int sourceAddonId) {
            try {
                if (string.IsNullOrEmpty(scriptUrl)) { return; }
                //
                // -- normalize scriptUrl
                string scriptUrlNormalized = scriptUrl.Trim();
                if (scriptUrlNormalized.Substring(0, 1) != "<") {
                    scriptUrlNormalized = scriptUrlNormalized.replace(@"\", "/", StringComparison.InvariantCultureIgnoreCase);
                    if (!scriptUrlNormalized.StartsWith("/") && !scriptUrlNormalized.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)) {
                        //
                        // -- case where link was relative to the current path. Does not work because URLs are not folders. Assume relative to root
                        scriptUrlNormalized = "/" + scriptUrlNormalized;
                    }
                }
                //
                // -- block duplicates
                CPDocBaseClass.HtmlAssetClass asset = core.doc.htmlAssetList.Find(t => (t.content == scriptUrlNormalized) && t.isLink);
                if (asset != null) {
                    //
                    // already in list, just mark it forceHead
                    asset.inHead = asset.inHead || forceHead;
                    return;
                }
                //
                // add to list
                core.doc.htmlAssetList.Add(new CPDocBaseClass.HtmlAssetClass {
                    assetType = CPDocBaseClass.HtmlAssetTypeEnum.script,
                    addedByMessage = addedByMessage,
                    isLink = true,
                    inHead = forceHead,
                    content = scriptUrlNormalized,
                    sourceAddonId = sourceAddonId
                });
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //=========================================================================================================
        /// <summary>
        /// Add a javascript link to the document
        /// </summary>
        /// <param name="scriptUrl">Link to the document. Should start with either 'http', or '/'</param>
        /// <param name="addedByMessage">message displayed in debug mode</param>
        public void addScriptLinkSrc(string scriptUrl, string addedByMessage) => addScriptLinkSrc(scriptUrl, addedByMessage, false, 0);
        //
        //=========================================================================================================
        /// <summary>
        /// Add a javascript link to the document
        /// </summary>
        /// <param name="scriptUrl">Link to the document. Should start with either 'http', or '/'</param>
        /// <param name="addedByMessage">message displayed in debug mode</param>
        /// <param name="forceHead">if true, this document tag goes in the head, else at the end of body</param>
        public void addScriptLinkSrc(string scriptUrl, string addedByMessage, bool forceHead) => addScriptLinkSrc(scriptUrl, addedByMessage, forceHead, 0);
        //
        //=========================================================================================================
        //
        public void addTitle(string pageTitle, string addedByMessage) {
            try {
                if (!string.IsNullOrEmpty(pageTitle.Trim())) {
                    core.doc.htmlMetaContent_TitleList.Add(new HtmlMetaClass {
                        addedByMessage = addedByMessage,
                        content = pageTitle
                    });
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //=========================================================================================================
        //
        public void addMetaDescription(string MetaDescription, string addedByMessage) {
            try {
                if (!string.IsNullOrEmpty(MetaDescription.Trim())) {
                    core.doc.htmlMetaContent_Description.Add(new HtmlMetaClass {
                        addedByMessage = addedByMessage,
                        content = MetaDescription
                    });
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        public void addMetaDescription(string MetaDescription) => addMetaDescription(MetaDescription, "");
        //
        //=========================================================================================================
        /// <summary>
        /// add a link to the head tag for a remote stylesheet
        /// </summary>
        /// <param name="styleSheetUrlNormalized">link, must start with either "/" or "http" or a "/" is added. This is because designers often create layouts without using a server by opening the files in the filesystem, and it is path relative.</param>
        /// <param name="addedByMessage">Displayed in debug mode</param>
        public void addStyleLink(string styleSheetUrlNormalized, string addedByMessage) {
            try {
                if (string.IsNullOrEmpty(styleSheetUrlNormalized)) { return; }
                string link = styleSheetUrlNormalized.Trim();
                if (link.Substring(0, 1) != "<") {
                    link = link.replace(@"\", "/", StringComparison.InvariantCultureIgnoreCase);
                    if (!link.StartsWith("/") && !link.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)) {
                        //
                        // -- case where link was relative to the current path. Does not work because URLs are not folders. Assume relative to root
                        link = "/" + link;
                    }
                }
                //
                // -- block duplicates
                CPDocBaseClass.HtmlAssetClass asset = core.doc.htmlAssetList.Find(t => (t.content == styleSheetUrlNormalized) && t.isLink);
                if (asset != null) { return; }
                //
                core.doc.htmlAssetList.Add(new CPDocBaseClass.HtmlAssetClass {
                    addedByMessage = addedByMessage,
                    assetType = CPDocBaseClass.HtmlAssetTypeEnum.style,
                    inHead = true,
                    isLink = true,
                    content = link
                });
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //=========================================================================================================
        //
        public void addMetaKeywordList(string metaKeywordList, string addedByMessage) {
            try {
                foreach (string keyword in metaKeywordList.Split(',')) {
                    if (!string.IsNullOrEmpty(keyword)) {
                        core.doc.htmlMetaContent_KeyWordList.Add(new HtmlMetaClass {
                            addedByMessage = addedByMessage,
                            content = keyword
                        });
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        public void addMetaKeywordList(string MetaKeywordList) => addMetaKeywordList(MetaKeywordList, "");
        //
        //=========================================================================================================
        //
        public void addHeadTag(string headTag, string addedByMessage) {
            try {
                if (!string.IsNullOrWhiteSpace(headTag)) {
                    core.doc.htmlMetaContent_OtherTags.Add(new HtmlMetaClass {
                        addedByMessage = addedByMessage,
                        content = headTag
                    });
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        public void addHeadTag(string headTag) => addHeadTag(headTag, "");
        //
        //============================================================================
        //
        public string getContentCopy(string CopyName, string DefaultContent, int personalizationPeopleId, bool AllowEditWrapper, bool personalizationIsAuthenticated) {
            string returnCopy = "";
            try {
                int RecordID = 0;
                int contactPeopleId = 0;
                //
                // honestly, not sure what to do with 'return_ErrorMessage'
                using (var csData = new CsModel(core)) {
                    if (!csData.open("copy content", "Name=" + DbController.encodeSQLText(CopyName), "ID", true, 0, "Name,ID,Copy,modifiedBy")) {
                        csData.close();
                        csData.insert("copy content");
                        if (csData.ok()) {
                            RecordID = csData.getInteger("ID");
                            csData.set("name", CopyName);
                            csData.set("copy", GenericController.encodeText(DefaultContent));
                            csData.save();
                            //   Call WorkflowController.publishEdit("copy content", RecordID)
                        }
                    }
                    if (csData.ok()) {
                        RecordID = csData.getInteger("ID");
                        contactPeopleId = csData.getInteger("modifiedBy");
                        returnCopy = csData.getText("Copy");
                        returnCopy = ContentRenderController.renderHtmlForWeb(core, returnCopy, "copy content", RecordID, personalizationPeopleId, "", 0, CPUtilsBaseClass.addonContext.ContextPage);
                        //
                        {
                            if (core.session.isEditing()) {
                                returnCopy = csData.getRecordEditLink(false) + returnCopy;
                                if (AllowEditWrapper) {
                                    returnCopy = AdminUIEditButtonController.getEditWrapper(core, returnCopy);
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnCopy;
        }
        //
        // ====================================================================================================
        //
        public void setContentCopy(string copyName, string content) {
            using (var csData = new CsModel(core)) {
                csData.open("Copy Content", "name=" + DbController.encodeSQLText(copyName));
                if (!csData.ok()) {
                    csData.insert("Copy Content");
                }
                csData.set("name", copyName);
                csData.set("Copy", content);
            }
        }
        //
        //====================================================================================================
        //
        public string getResourceLibrary(string RootFolderName, bool AllowSelectResource, string SelectResourceEditorName, string SelectLinkObjectName, bool AllowGroupAdd) {
            string addonGuidResourceLibrary = "{564EF3F5-9673-4212-A692-0942DD51FF1A}";
            Dictionary<string, string> arguments = new Dictionary<string, string> {
                { "RootFolderName", RootFolderName },
                { "AllowSelectResource", AllowSelectResource.ToString() },
                { "SelectResourceEditorName", SelectResourceEditorName },
                { "SelectLinkObjectName", SelectLinkObjectName },
                { "AllowGroupAdd", AllowGroupAdd.ToString() }
            };
            return core.addon.execute(core.cacheRuntime.addonCache.create(addonGuidResourceLibrary), new CPUtilsBaseClass.addonExecuteContext {
                addonType = CPUtilsBaseClass.addonContext.ContextAdmin,
                argumentKeyValuePairs = arguments,
                errorContextMessage = "calling resource library addon [" + addonGuidResourceLibrary + "] from internal method"
            });
        }
        //
        //====================================================================================================
        /// <summary>
        /// process the input of a checklist, making changes to rule records
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="primaryContentName"></param>
        /// <param name="primaryRecordID"></param>
        /// <param name="secondaryContentName"></param>
        /// <param name="rulesContentName"></param>
        /// <param name="rulesPrimaryFieldname"></param>
        /// <param name="rulesSecondaryFieldName"></param>
        public void processCheckList(string tagName, string primaryContentName, string primaryRecordID, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName) {
            int rowCnt = core.docProperties.getInteger(tagName + ".RowCount");
            bool RuleContentChanged = false;
            if (rowCnt > 0) {
                //
                // -- get additional rule fields 
                var ruleMetadata = ContentMetadataModel.createByUniqueName(core, rulesContentName);
                if (ruleMetadata == null) {
                    logger.Warn($"{core.logCommonMessage}, processCheckList called and ruleContentName not found [" + rulesContentName + "]");
                    return;
                }
                var ruleEditFields = getRuleEditFields(ruleMetadata, rulesPrimaryFieldname, rulesSecondaryFieldName);
                //
                var secondaryMetadata = ContentMetadataModel.createByUniqueName(core, secondaryContentName);
                if (secondaryMetadata == null) {
                    logger.Warn($"{core.logCommonMessage}, processCheckList called and secondaryContentName not found [" + secondaryContentName + "]");
                    return;
                }
                //
                // Go through each checkbox and check for a rule
                string dupRuleIdList = "";
                string rulesTablename = MetadataController.getContentTablename(core, rulesContentName);
                string SQL = "select " + rulesSecondaryFieldName + ",id from " + rulesTablename + " where (" + rulesPrimaryFieldname + "=" + primaryRecordID + ")and(active<>0) order by " + rulesSecondaryFieldName;
                DataTable currentRules = core.db.executeQuery(SQL);
                int currentRulesCnt = currentRules.Rows.Count;
                for (int rowPtr = 0; rowPtr < rowCnt; rowPtr++) {
                    //
                    // ----- Read Response
                    int secondaryRecordId = core.docProperties.getInteger(tagName + "." + rowPtr + ".ID");
                    bool ruleNeeded = core.docProperties.getBoolean(tagName + "." + rowPtr);
                    //
                    // ----- find existing rule in datatable
                    bool ruleFound = false;
                    int ruleId = 0;
                    int testRecordIDLast = 0;
                    for (int ptr = 0; ptr < currentRulesCnt; ptr++) {
                        int testRecordId = encodeInteger(currentRules.Rows[ptr][0]);
                        if (testRecordId == 0) {
                            //
                            // skip
                        } else if (testRecordId == secondaryRecordId) {
                            //
                            // hit
                            ruleFound = true;
                            ruleId = encodeInteger(currentRules.Rows[ptr][1]);
                            break;
                        } else if (testRecordId == testRecordIDLast) {
                            //
                            // dup
                            dupRuleIdList = dupRuleIdList + "," + encodeInteger(currentRules.Rows[ptr][1]);
                            currentRules.Rows[ptr][0] = 0;
                        }
                        testRecordIDLast = testRecordId;
                    }
                    if (ruleNeeded && !ruleFound) {
                        if (!ruleFound) {
                            //
                            // No record exists, and one is needed                        
                            using (var csData = new CsModel(core)) {
                                if (csData.insert(rulesContentName)) {
                                    csData.set("Active", ruleNeeded);
                                    csData.set(rulesPrimaryFieldname, primaryRecordID);
                                    csData.set(rulesSecondaryFieldName, secondaryRecordId);
                                    ruleId = csData.getInteger("id");
                                }
                            }
                        }
                        RuleContentChanged = true;
                    } else if (!ruleNeeded && ruleFound) {
                        //
                        // Record exists and it is not needed
                        SQL = "delete from " + rulesTablename + " where id=" + ruleId;
                        core.db.executeNonQuery(SQL);
                        RuleContentChanged = true;
                    }
                    if (ruleNeeded && ruleId > 0 & ruleEditFields.Count > 0) {
                        string delimiter = "";
                        string updateSql = "update " + ruleMetadata.tableName + " set ";
                        foreach (var ruleField in ruleEditFields) {
                            updateSql += delimiter + ruleField.nameLc + "=";
                            string requestName = tagName + "." + rowPtr + "." + ruleField.nameLc;
                            if (string.IsNullOrEmpty(core.docProperties.getText(requestName))) {
                                //
                                // -- blank saves null in all cases
                                updateSql += "null";
                            } else {
                                switch (ruleField.fieldTypeId) {
                                    case FieldTypeIdEnum.Text: {
                                            updateSql += DbController.encodeSQLText(core.docProperties.getText(requestName));
                                            break;
                                        }
                                    case FieldTypeIdEnum.Date: {
                                            updateSql += DbController.encodeSQLDate(core.docProperties.getDate(requestName));
                                            break;
                                        }
                                    case FieldTypeIdEnum.Currency:
                                    case FieldTypeIdEnum.Float: {
                                            updateSql += DbController.encodeSQLNumber(core.docProperties.getNumber(requestName));
                                            break;
                                        }
                                    case FieldTypeIdEnum.Integer: {
                                            updateSql += DbController.encodeSQLNumber(core.docProperties.getInteger(requestName));
                                            break;
                                        }
                                    case FieldTypeIdEnum.Boolean: {
                                            updateSql += DbController.encodeSQLBoolean(core.docProperties.getBoolean(requestName));
                                            break;
                                        }
                                }
                            }
                            delimiter = ",";
                        }
                        updateSql += " where id=" + ruleId;
                        core.db.executeNonQuery(updateSql);
                    }
                }
                //
                // delete dups
                if (!string.IsNullOrEmpty(dupRuleIdList)) {
                    SQL = "delete from " + rulesTablename + " where id in (" + dupRuleIdList.Substring(1) + ")";
                    core.db.executeNonQuery(SQL);
                    RuleContentChanged = true;
                }
            }
            if (RuleContentChanged) {
                string tablename = MetadataController.getContentTablename(core, rulesContentName);
                core.cache.invalidateTableDependencyKey(tablename);
            }
        }
        //
        //====================================================================================================
        //
        public static string genericBlockTag(string TagName, string InnerHtml) => genericBlockTag(TagName, InnerHtml, "", "", "", "");
        //
        public static string genericBlockTag(string TagName, string InnerHtml, string HtmlClass) => genericBlockTag(TagName, InnerHtml, HtmlClass, "", "", "");
        //
        public static string genericBlockTag(string TagName, string InnerHtml, string HtmlClass, string HtmlId) => genericBlockTag(TagName, InnerHtml, HtmlClass, HtmlId, "", "");
        //
        //====================================================================================================
        /// <summary>
        /// create an html block tag like div
        /// </summary>
        /// <param name="TagName"></param>
        /// <param name="InnerHtml"></param>
        /// <param name="HtmlName"></param>
        /// <param name="HtmlClass"></param>
        /// <param name="HtmlId"></param>
        /// <returns></returns>
        public static string genericBlockTag(string TagName, string InnerHtml, string HtmlClass, string HtmlId, string HtmlName, string inlineStyle) {
            var result = new StringBuilder("<");
            result.Append(TagName.Trim());
            result.Append(!string.IsNullOrEmpty(HtmlName) ? " name=\"" + HtmlName + "\"" : "");
            result.Append(!string.IsNullOrEmpty(HtmlClass) ? " class=\"" + HtmlClass + "\"" : "");
            result.Append(!string.IsNullOrEmpty(HtmlId) ? " id=\"" + HtmlId + "\"" : "");
            result.Append(!string.IsNullOrEmpty(inlineStyle) ? " style=\"" + inlineStyle + "\"" : "");
            result.Append(">");
            result.Append(InnerHtml);
            result.Append("</");
            result.Append(TagName.Trim());
            result.Append(">");
            return result.ToString();
        }
        //
        //====================================================================================================
        //
        public static string li(string innerHtml) => genericBlockTag("li", innerHtml, "", "", "", "");
        //
        public static string li(string innerHtml, string htmlClass) => genericBlockTag("li", innerHtml, htmlClass, "", "", "");
        //
        public static string li(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("li", innerHtml, htmlClass, htmlId, "", "");
        //
        public static string li(string innerHtml, string htmlClass, string htmlId, string style) => genericBlockTag("li", innerHtml, htmlClass, htmlId, "", style);
        //
        //====================================================================================================
        //
        public static string ol(string innerHtml) => genericBlockTag("ol", innerHtml, "", "", "", "");
        //
        public static string ol(string innerHtml, string htmlClass) => genericBlockTag("ol", innerHtml, htmlClass, "", "", "");
        //
        public static string ol(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("ol", innerHtml, htmlClass, htmlId, "", "");
        //
        public static string ol(string innerHtml, string htmlClass, string htmlId, string style) => genericBlockTag("ol", innerHtml, htmlClass, htmlId, "", style);
        //
        //====================================================================================================
        //
        public static string ul(string innerHtml) => genericBlockTag("ul", innerHtml, "", "", "", "");
        //
        public static string ul(string innerHtml, string htmlClass) => genericBlockTag("ul", innerHtml, htmlClass, "", "", "");
        //
        public static string ul(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("ul", innerHtml, htmlClass, htmlId, "", "");
        //
        public static string ul(string innerHtml, string htmlClass, string htmlId, string style) => genericBlockTag("ul", innerHtml, htmlClass, htmlId, "", style);
        //
        //====================================================================================================
        //
        public static string section(string innerHtml) => genericBlockTag("section", innerHtml, "", "", "", "");
        //
        public static string section(string innerHtml, string htmlClass) => genericBlockTag("section", innerHtml, htmlClass, "", "", "");
        //
        public static string section(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("section", innerHtml, htmlClass, htmlId, "", "");
        //
        public static string section(string innerHtml, string htmlClass, string htmlId, string style) => genericBlockTag("section", innerHtml, htmlClass, htmlId, "", style);
        //
        //====================================================================================================
        //
        public static string div(string innerHtml) => genericBlockTag("div", innerHtml, "", "", "", "");
        //
        public static string div(string innerHtml, string htmlClass) => genericBlockTag("div", innerHtml, htmlClass, "", "", "");
        //
        public static string div(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("div", innerHtml, htmlClass, htmlId, "", "");
        //
        public static string div(string innerHtml, string htmlClass, string htmlId, string style) => genericBlockTag("div", innerHtml, htmlClass, htmlId, "", style);
        //
        //====================================================================================================
        //
        public static string p(string innerHtml) => genericBlockTag("p", innerHtml, "", "", "", "");
        //
        public static string p(string innerHtml, string htmlClass) => genericBlockTag("p", innerHtml, htmlClass, "", "", "");
        //
        public static string p(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("p", innerHtml, htmlClass, htmlId, "", "");
        //
        public static string p(string innerHtml, string htmlClass, string htmlId, string style) => genericBlockTag("p", innerHtml, htmlClass, htmlId, "", style);
        //
        //====================================================================================================
        //
        public static string h1(string innerHtml) => genericBlockTag("h1", innerHtml, "", "", "", "");
        //
        public static string h1(string innerHtml, string htmlClass) => genericBlockTag("h1", innerHtml, htmlClass, "", "", "");
        //
        public static string h1(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("h1", innerHtml, htmlClass, htmlId, "", "");
        //
        //====================================================================================================
        //
        public static string h2(string innerHtml) => genericBlockTag("h2", innerHtml, "", "", "", "");
        //
        public static string h2(string innerHtml, string htmlClass) => genericBlockTag("h2", innerHtml, htmlClass, "", "", "");
        //
        public static string h2(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("h2", innerHtml, htmlClass, htmlId, "", "");
        //
        //====================================================================================================
        //
        public static string h3(string innerHtml) => genericBlockTag("h3", innerHtml, "", "", "", "");
        //
        public static string h3(string innerHtml, string htmlClass) => genericBlockTag("h3", innerHtml, htmlClass, "", "", "");
        //
        public static string h3(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("h3", innerHtml, htmlClass, htmlId, "", "");
        //
        //====================================================================================================
        //
        public static string h4(string innerHtml) => genericBlockTag("h4", innerHtml, "", "", "", "");
        //
        public static string h4(string innerHtml, string htmlClass) => genericBlockTag("h4", innerHtml, htmlClass, "", "", "");
        //
        public static string h4(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("h4", innerHtml, htmlClass, htmlId, "", "");
        //
        //====================================================================================================
        //
        public static string h5(string innerHtml) => genericBlockTag("h5", innerHtml, "", "", "", "");
        //
        public static string h5(string innerHtml, string htmlClass) => genericBlockTag("h5", innerHtml, htmlClass, "", "", "");
        //
        public static string h5(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("h5", innerHtml, htmlClass, htmlId, "", "");
        //
        //====================================================================================================
        //
        public static string h6(string innerHtml) => genericBlockTag("h6", innerHtml, "", "", "", "");
        //
        public static string h6(string innerHtml, string htmlClass) => genericBlockTag("h6", innerHtml, htmlClass, "", "", "");
        //
        public static string h6(string innerHtml, string htmlClass, string htmlId) => genericBlockTag("h6", innerHtml, htmlClass, htmlId, "", "");
        //
        //====================================================================================================
        //
        public static string label(string innerHtml, string forHtmlId = "", string htmlClass = "", string htmlId = "") {
            string s = "";
            if (!string.IsNullOrEmpty(forHtmlId)) {
                s += " for=\"" + forHtmlId + "\"";
            }
            if (!string.IsNullOrEmpty(htmlClass)) {
                s += " class=\"" + htmlClass + "\"";
            }
            if (!string.IsNullOrEmpty(htmlId)) {
                s += " id=\"" + htmlId + "\"";
            }
            return "<label " + s + ">" + innerHtml + "</label>";
        }
        //
        //====================================================================================================
        //
        public static string strong(string innerHtml, string htmlClass = "", string htmlId = "") {
            string s = "";
            if (!string.IsNullOrEmpty(htmlClass)) {
                s += " class=\"" + htmlClass + "\"";
            }
            if (!string.IsNullOrEmpty(htmlId)) {
                s += " id=\"" + htmlId + "\"";
            }
            return "<strong " + s + ">" + innerHtml + "</strong>";
        }
        //
        // ====================================================================================================
        /// <summary>
        /// img tag
        /// </summary>
        /// <param name="src"></param>
        /// <param name="alt"></param>
        /// <param name="widthPx"></param>
        /// <param name="heightPx"></param>
        /// <returns></returns>
        public static string img(string src, string alt, int widthPx, int heightPx, string htmlClass) {
            var result = new StringBuilder("<img");
            result.Append(!string.IsNullOrEmpty(src) ? " src=\"" + src + "\"" : "");
            result.Append(!string.IsNullOrEmpty(alt) ? " alt=\"" + alt + "\"" : "");
            result.Append(!widthPx.Equals(0) ? " width=\"" + widthPx.ToString() + "\"" : "");
            result.Append(!heightPx.Equals(0) ? " height=\"" + heightPx.ToString() + "\"" : "");
            result.Append(!string.IsNullOrEmpty(htmlClass) ? " class=\"" + htmlClass + "\"" : "");
            result.Append(">");
            return result.ToString();
        }
        //
        public static string img(string src, string alt, int widthPx, int heightPx) => img(src, alt, widthPx, heightPx, "");
        //
        public static string img(string src, string alt, int widthPx) => img(src, alt, widthPx, 0, "");
        //
        public static string img(string src, string alt) => img(src, alt, 0, 0, "");
        //
        public static string img(string src) => img(src, "", 0, 0, "");
        //
        // ====================================================================================================
        /// <summary>
        /// create html span inline tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public static string genericInlineTag(string htmlTag, string innerHtml, string htmlClass, string htmlId) => "<" + htmlTag + " class=\"" + htmlClass + "\" id=\"" + htmlId + "\">" + innerHtml + "</" + htmlTag + ">";
        public static string genericInlineTag(string htmlTag, string innerHtml, string htmlClass) => "<" + htmlTag + " class=\"" + htmlClass + "\">" + innerHtml + "</" + htmlTag + ">";
        public static string genericInlineTag(string htmlTag, string innerHtml) => "<" + htmlTag + ">" + innerHtml + "</" + htmlTag + ">";
        //
        // ====================================================================================================
        /// <summary>
        /// create html span inline tag
        /// </summary>
        /// <returns></returns>
        public static string span(string innerHtml, string htmlClass, string htmlId) { return genericInlineTag("span", innerHtml, htmlClass, htmlId); }
        public static string span(string innerHtml, string htmlClass) { return genericInlineTag("span", innerHtml, htmlClass); }
        public static string span(string innerHtml) { return genericInlineTag("span", innerHtml); }
        //
        // ====================================================================================================
        /// <summary>
        /// create html small inline tag
        /// </summary>
        /// <returns></returns>
        public static string small(string innerHtml, string htmlClass, string htmlId) { return genericInlineTag("small", innerHtml, htmlClass, htmlId); }
        public static string small(string innerHtml, string htmlClass) { return genericInlineTag("small", innerHtml, htmlClass); }
        public static string small(string innerHtml) { return genericInlineTag("small", innerHtml); }
        //
        // ====================================================================================================
        /// <summary>
        /// create a table start tag
        /// </summary>
        /// <param name="cellpadding"></param>
        /// <param name="cellspacing"></param>
        /// <param name="border"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public static string tableStart(int cellpadding, int cellspacing, int border, string htmlClass = "") {
            return "<table border=\"" + border + "\" cellpadding=\"" + cellpadding + "\" cellspacing=\"" + cellspacing + "\" class=\"" + htmlClass + "\" width=\"100%\">";
        }
        //
        // ====================================================================================================
        /// <summary>
        /// return a row start (tr tag)
        /// </summary>
        /// <returns></returns>
        public static string tableRowStart() {
            return "<tr>";
        }
        //
        // ====================================================================================================
        /// <summary>
        /// create a td tag (without /td)
        /// </summary>
        /// <param name="Width"></param>
        /// <param name="ColSpan"></param>
        /// <param name="EvenRow"></param>
        /// <param name="Align"></param>
        /// <param name="BGColor"></param>
        /// <returns></returns>
        public static string tableCellStart(string Width = "", int ColSpan = 0, bool EvenRow = false, string Align = "", string BGColor = "") {
            string result = "";
            if (!string.IsNullOrEmpty(Width)) {
                result += " width=\"" + Width + "\"";
            }
            if (!string.IsNullOrEmpty(BGColor)) {
                result += " bgcolor=\"" + BGColor + "\"";
            } else if (EvenRow) {
                result += " class=\"ccPanelRowEven\"";
            } else {
                result += " class=\"ccPanelRowOdd\"";
            }
            if (ColSpan != 0) {
                result += " colspan=\"" + ColSpan + "\"";
            }
            if (!string.IsNullOrEmpty(Align)) {
                result += " align=\"" + Align + "\"";
            }
            return "<td" + result + ">";
        }
        //
        // ====================================================================================================
        /// <summary>
        /// create a table cell <td>content</td>
        /// </summary>
        /// <param name="Copy"></param>
        /// <param name="Width"></param>
        /// <param name="ColSpan"></param>
        /// <param name="EvenRow"></param>
        /// <param name="Align"></param>
        /// <param name="BGColor"></param>
        /// <returns></returns>
        public static string td(string Copy, string Width = "", int ColSpan = 0, bool EvenRow = false, string Align = "", string BGColor = "") {
            return tableCellStart(Width, ColSpan, EvenRow, Align, BGColor) + Copy + tableCellEnd;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// create a <tr><td>content</td></tr>
        /// </summary>
        /// <param name="Cell"></param>
        /// <param name="ColSpan"></param>
        /// <param name="EvenRow"></param>
        /// <returns></returns>
        public static string tableRow(string Cell, int ColSpan = 0, bool EvenRow = false) {
            return tableRowStart() + td(Cell, "100%", ColSpan, EvenRow) + kmaEndTableRow;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// convert html entities in a string
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static string encodeHtml(string Source) {
            return WebUtility.HtmlEncode(Source);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// decodeHtml, Convert HTML entity strings to their text equiv
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static string decodeHtml(string Source) {
            return WebUtility.HtmlDecode(Source);
        }
        //
        // ====================================================================================================
        //
        public static string style(string innerStyleSheet) {
            var tag = new StringBuilder("<style type=\"text/css\">");
            tag.Append(innerStyleSheet).Append("</style>");
            return tag.ToString();
        }
        //
        // ====================================================================================================
        //
        public static string indent(string sourceHtml, int tabCnt) {
            string result = "";
            //
            //   Indent every line by 1 tab
            int posStart = GenericController.strInstr(1, sourceHtml, "<![CDATA[", 1);
            if (posStart == 0) {
                //
                // no cdata
                //
                posStart = GenericController.strInstr(1, sourceHtml, "<textarea", 1);
                if (posStart == 0) {
                    //
                    // no textarea
                    //
                    if (tabCnt > 0 && tabCnt < 99) {
                        result = sourceHtml.Replace(Environment.NewLine, Environment.NewLine + new string(Convert.ToChar("\t"), tabCnt));
                    } else {
                        result = sourceHtml.Replace(Environment.NewLine, Environment.NewLine + "\t");
                    }
                } else {
                    //
                    // text area found, isolate it and indent before and after
                    //
                    int posEnd = GenericController.strInstr(posStart, sourceHtml, "</textarea>", 1);
                    string pre = sourceHtml.left(posStart - 1);
                    string post = "";
                    string target = "";
                    if (posEnd == 0) {
                        target = sourceHtml.Substring(posStart - 1);
                    } else {
                        target = sourceHtml.Substring(posStart - 1, posEnd - posStart + ((string)("</textarea>")).Length);
                        post = sourceHtml.Substring((posEnd + ((string)("</textarea>")).Length) - 1);
                    }
                    result = indent(pre, tabCnt) + target + indent(post, tabCnt);
                }
            } else {
                //
                // cdata found, isolate it and indent before and after
                //
                int posEnd = GenericController.strInstr(posStart, sourceHtml, "]]>", 1);
                string pre = sourceHtml.left(posStart - 1);
                string post = "";
                string target = "";
                if (posEnd == 0) {
                    target = sourceHtml.Substring(posStart - 1);
                } else {
                    target = sourceHtml.Substring(posStart - 1, posEnd - posStart + ((string)("]]>")).Length);
                    post = sourceHtml.Substring(posEnd + 2);
                }
                result = indent(pre, tabCnt) + target + indent(post, tabCnt);
            }
            return result;
        }
        //
        public static string indent(string sourceHtml) => indent(sourceHtml, 1);
        //
        //====================================================================================================
        //
        public static string inputCurrency(CoreController core, string htmlName, Double? htmlValue, string htmlId = "", string htmlClass = "", bool readOnly = false, bool required = false, bool disabled = false) {
            string result = "";
            try {
                core.doc.formInputTextCnt += 1;
                core.doc.inputDateCnt = core.doc.inputDateCnt + 1;
                result = "<input type=\"number\" step=\"any\" name=\"" + encodeHtml(htmlName) + "\"";
                if ((htmlValue != null)) result += " value=\"" + encodeNumber(htmlValue).ToString("F2") + "\"";
                result += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
                result += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
                result += (!readOnly) ? "" : " readonly";
                result += (!disabled) ? "" : " disabled";
                result += (!required) ? "" : " required";
                result += ">";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public static string inputNumber(CoreController core, string htmlName, Double? htmlValue, string htmlId = "", string htmlClass = "", bool readOnly = false, bool required = false, bool disabled = false) {
            string result = "";
            try {
                // {0:s}
                // yyyy-MM-dd
                core.doc.formInputTextCnt += 1;
                core.doc.inputDateCnt = core.doc.inputDateCnt + 1;
                result = "<input type=\"number\" step=\"any\"  name=\"" + HtmlController.encodeHtml(htmlName) + "\"";
                if ((htmlValue != null)) result += " value=\"" + htmlValue + "\"";
                result += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
                result += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
                result += (!readOnly) ? "" : " readonly";
                result += (!disabled) ? "" : " disabled";
                result += (!required) ? "" : " required";
                result += ">";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public static string inputInteger(CoreController core, string htmlName, int? htmlValue, string htmlId = "", string htmlClass = "", bool readOnly = false, bool required = false, bool disabled = false) {
            string result = "";
            try {
                // {0:s}
                // yyyy-MM-dd
                core.doc.formInputTextCnt += 1;
                core.doc.inputDateCnt = core.doc.inputDateCnt + 1;
                result = "<input type=\"number\" step=\"1\" name=\"" + HtmlController.encodeHtml(htmlName) + "\"";
                if ((htmlValue != null)) result += " value=\"" + htmlValue + "\"";
                result += (string.IsNullOrEmpty(htmlId)) ? "" : " id=\"" + htmlId + "\"";
                result += (string.IsNullOrEmpty(htmlClass)) ? "" : " class=\"" + htmlClass + "\"";
                result += (!readOnly) ? "" : " readonly";
                result += (!disabled) ? "" : " disabled";
                result += (!required) ? "" : " required";
                result += ">";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        // ====================================================================================================
        //
        public static string adminHint(CoreController core, string innerHtml) {
            if (core.session.isEditing() || core.session.user.admin || core.session.user.developer) {
                return ""
                    + "<div class=\"ccHintWrapper\">"
                        + "<div  class=\"ccHintWrapperContent\">"
                        + "<b>Administrator</b>"
                        + "<br>"
                        + "<br>" + GenericController.encodeText(innerHtml) + "</div>"
                    + "</div>";
            }
            return string.Empty;
        }
        //
        // ====================================================================================================
        //
        public static string scriptCode(CoreController core, string code) {
            try {
                if (string.IsNullOrWhiteSpace(code)) { return string.Empty; }
                return ""
                    + "\n<script>"
                    + "\n" + code
                    + "\n</script>";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        // ====================================================================================================
        //
        public static string script(CoreController core, string src) {
            try {
                if (string.IsNullOrWhiteSpace(src)) { return string.Empty; }
                return "<script src=\"" + src + "\"></script>";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
        //========================================================================================================
        /// <summary>
        /// Convert the href and src links in html content to full urls that include the protocol and domain 
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <param name="urlProtocolDomainSlash"></param>
        /// <returns></returns>
        public static string convertLinksToAbsolute(string htmlContent, string urlProtocolDomainSlash) {
            string result = htmlContent;
            result = result.Replace(" href=\"", " href=\"/");
            result = result.Replace(" href=\"/http", " href=\"http");
            result = result.Replace(" href=\"/mailto", " href=\"mailto");
            result = result.Replace(" href=\"/../", " href=\"" + urlProtocolDomainSlash);
            result = result.Replace(" href=\"/./", " href=\"" + urlProtocolDomainSlash);
            result = result.Replace(" href=\"//", " href=\"" + urlProtocolDomainSlash);
            result = result.Replace(" href=\"/?", " href=\"" + urlProtocolDomainSlash + "?");
            result = result.Replace(" href=\"/", " href=\"" + urlProtocolDomainSlash);
            //
            result = result.Replace(" href=", " href=/");
            result = result.Replace(" href=/\"", " href=\"");
            result = result.Replace(" href=/http", " href=http");
            result = result.Replace(" href=//", " href=" + urlProtocolDomainSlash);
            result = result.Replace(" href=/?", " href=" + urlProtocolDomainSlash + "?");
            result = result.Replace(" href=/", " href=" + urlProtocolDomainSlash);
            //
            result = result.Replace(" src=\"", " src=\"/");
            result = result.Replace(" src=\"/http", " src=\"http");
            result = result.Replace(" src=\"/../", " src=\"" + urlProtocolDomainSlash);
            result = result.Replace(" src=\"/./", " src=\"" + urlProtocolDomainSlash);
            result = result.Replace(" src=\"//", " src=\"" + urlProtocolDomainSlash);
            result = result.Replace(" src=\"/?", " src=\"" + urlProtocolDomainSlash + "?");
            result = result.Replace(" src=\"/", " src=\"" + urlProtocolDomainSlash);
            //
            result = result.Replace(" src=", " src=/");
            result = result.Replace(" src=/\"", " src=\"");
            result = result.Replace(" src=/http", " src=http");
            result = result.Replace(" src=/../", " src=" + urlProtocolDomainSlash);
            result = result.Replace(" src=/./", " src=" + urlProtocolDomainSlash);
            result = result.Replace(" src=//", " src=" + urlProtocolDomainSlash);
            result = result.Replace(" src=/?", " src=" + urlProtocolDomainSlash + "?");
            result = result.Replace(" src=/", " src=" + urlProtocolDomainSlash);
            return result;
        }
        //
        //====================================================================================================
        //
        private static List<ContentFieldMetadataModel> getRuleEditFields(ContentMetadataModel rulesMeta, string rulesPrimaryFieldname, string rulesSecondaryFieldName) {
            var allowedFieldTypes = new List<FieldTypeIdEnum> { FieldTypeIdEnum.Boolean, FieldTypeIdEnum.Currency, FieldTypeIdEnum.Date, FieldTypeIdEnum.Float, FieldTypeIdEnum.Integer, FieldTypeIdEnum.Text, FieldTypeIdEnum.Link };
            var blockFieldNames = new List<string> { "id", "name", "sortorder", "active", "dateadded", "createdby", "modifieddate", "modifiedby", "contentcontrolid", "ccguid", "createkey" };
            List<ContentFieldMetadataModel> ruleFields = new();
            foreach (var field in rulesMeta.fields) {
                if (allowedFieldTypes.Contains(field.Value.fieldTypeId) && !blockFieldNames.Contains(field.Value.nameLc) && field.Value.active && field.Value.authorable && field.Value.nameLc != rulesPrimaryFieldname && field.Value.nameLc != rulesSecondaryFieldName) {
                    ruleFields.Add(field.Value);
                }
            }
            return ruleFields;
        }
        //
        //====================================================================================================
        //
        private static string getAddedByComment(string addedByMessage, string addedBySuffix) {
            return Environment.NewLine + "<!-- added by " + HtmlController.encodeHtml(addedByMessage) + ", " + addedBySuffix + " -->";
        }
        //
        //====================================================================================================
        //
        private static string getAddedByComment(string addedByMessage) {
            return Environment.NewLine + "<!-- added by " + HtmlController.encodeHtml(addedByMessage) + " -->";
        }

        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static NLog.Logger logger { get; } = NLog.LogManager.GetCurrentClassLogger();
    }
}
