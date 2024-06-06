
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.AdminUIController;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Addons.AdminSite {
    public static class ListView {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //========================================================================
        /// <summary>
        /// Print the index form, values and all creates a sql with leftjoins, and renames lookups as TableLookupxName where x is the TarGetFieldPtr of the field that is FieldTypeLookup
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <param name="IsEmailContent"></param>
        /// <returns></returns>
        public static string get(CPClass cp, CoreController core, AdminDataModel adminData) {
            string result;
            try {
                //
                // --- make sure required fields are present
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                if (adminData.adminContent.id == 0) {
                    //
                    // Bad content id
                    Stream.add(AdminErrorController.get(core, "This form requires a valid content definition, and one was not found for content ID [" + adminData.adminContent.id + "].", "No content definition was specified [ContentID=0]. Please contact your application developer for more assistance."));
                } else if (string.IsNullOrEmpty(adminData.adminContent.name)) {
                    //
                    // Bad content name
                    Stream.add(AdminErrorController.get(core, "No content definition could be found for ContentID [" + adminData.adminContent.id + "]. This could be a menu error. Please contact your application developer for more assistance.", "No content definition for ContentID [" + adminData.adminContent.id + "] could be found."));
                } else if (string.IsNullOrEmpty(adminData.adminContent.tableName)) {
                    //
                    // No tablename
                    Stream.add(AdminErrorController.get(core, "The content definition [" + adminData.adminContent.name + "] is not associated with a valid database table. Please contact your application developer for more assistance.", "Content [" + adminData.adminContent.name + "] ContentTablename is empty."));
                } else if (adminData.adminContent.fields.Count.Equals(0)) {
                    //
                    // No Fields
                    Stream.add(AdminErrorController.get(core, "This content [" + adminData.adminContent.name + "] cannot be accessed because it has no fields. Please contact your application developer for more assistance.", "Content [" + adminData.adminContent.name + "] has no field records."));
                } else if (adminData.adminContent.developerOnly && (!core.session.isAuthenticatedDeveloper())) {
                    //
                    // Developer Content and not developer
                    Stream.add(AdminErrorController.get(core, "Access to this content [" + adminData.adminContent.name + "] requires developer permissions. Please contact your application developer for more assistance.", "Content [" + adminData.adminContent.name + "] has no field records."));
                } else {
                    List<string> tmp = new List<string> { };
                    DataSourceModel datasource = DataSourceModel.create(core.cpParent, adminData.adminContent.dataSourceId, ref tmp);
                    //
                    // get access rights
                    var userContentPermissions = PermissionController.getUserContentPermissions(core, adminData.adminContent);
                    //
                    // detemine which subform to disaply
                    string Copy = "";
                    int SubForm = core.docProperties.getInteger(RequestNameAdminSubForm);
                    if (SubForm != 0) {
                        switch (SubForm) {
                            case AdminFormIndex_SubFormExport: {
                                    Copy = ListViewExport.get(core, adminData);
                                    break;
                                }
                            case AdminFormIndex_SubFormSetColumns: {
                                    Copy = ListViewSetColumns.get(cp, core, adminData);
                                    break;
                                }
                            case AdminFormIndex_SubFormAdvancedSearch: {
                                    Copy = ListViewAdvancedSearch.get(cp, core, adminData);
                                    break;
                                }
                            default: {
                                    // empty
                                    break;
                                }
                        }
                    }
                    Stream.add(Copy);
                    if (string.IsNullOrEmpty(Copy)) {
                        //
                        // If subforms return empty, go to parent form
                        //
                        // -- Load Index page customizations
                        GridConfigClass gridConfig = GridConfigClass.get(core, adminData);
                        setIndexSQL_ProcessIndexConfigRequests(core, adminData, ref gridConfig);
                        AdminContentController.setIndexSQL_SaveIndexConfig(cp, core, gridConfig);
                        //
                        // Get the SQL parts
                        bool AllowAccessToContent = false;
                        string ContentAccessLimitMessage = "";
                        bool IsLimitedToSubContent = false;
                        string sqlWhere = "";
                        string sqlOrderBy = "";
                        string sqlFieldList = "";
                        string sqlFrom = "";
                        Dictionary<string, bool> FieldUsedInColumns = new Dictionary<string, bool>(); // used to prevent select SQL from being sorted by a field that does not appear
                        Dictionary<string, bool> IsLookupFieldValid = new Dictionary<string, bool>();
                        setIndexSQL(core, adminData, gridConfig, ref AllowAccessToContent, ref sqlFieldList, ref sqlFrom, ref sqlWhere, ref sqlOrderBy, ref IsLimitedToSubContent, ref ContentAccessLimitMessage, ref FieldUsedInColumns, IsLookupFieldValid);
                        bool AllowAdd = adminData.adminContent.allowAdd && (!IsLimitedToSubContent) && (userContentPermissions.allowAdd);
                        bool allowDelete = (adminData.adminContent.allowDelete) && (userContentPermissions.allowDelete);
                        if ((!userContentPermissions.allowEdit) || (!AllowAccessToContent)) {
                            //
                            // two conditions should be the same -- but not time to check - This user does not have access to this content
                            ErrorController.addUserError(core, "Your account does not have access to any records in '" + adminData.adminContent.name + "'.");
                        } else {
                            //
                            // -- testupdate adminRecents
                            AdminRecentModel.insertAdminRecentContent(cp, cp.User.Id, adminData.adminContent.name, "?cid=" + adminData.adminContent.id);
                            //
                            // Get the total record count
                            string sql = "select count(" + adminData.adminContent.tableName + ".ID) as cnt from " + sqlFrom;
                            if (!string.IsNullOrEmpty(sqlWhere)) {
                                sql += " where " + sqlWhere;
                            }
                            int recordCnt = 0;
                            using (var csData = new CsModel(core)) {
                                if (csData.openSql(sql, datasource.name)) {
                                    recordCnt = csData.getInteger("cnt");
                                }
                            }
                            //
                            // Assumble the SQL
                            //
                            sql = "select";
                            if (datasource.dbTypeId != DataSourceTypeODBCMySQL) {
                                sql += " Top " + (gridConfig.recordTop + gridConfig.recordsPerPage);
                            }
                            sql += " " + sqlFieldList + " From " + sqlFrom;
                            if (!string.IsNullOrEmpty(sqlWhere)) {
                                sql += " WHERE " + sqlWhere;
                            }
                            if (!string.IsNullOrEmpty(sqlOrderBy)) {
                                sql += " Order By" + sqlOrderBy;
                            }
                            if (datasource.dbTypeId == DataSourceTypeODBCMySQL) {
                                sql += " Limit " + (gridConfig.recordTop + gridConfig.recordsPerPage);
                            }
                            //
                            // Refresh Query String
                            //
                            core.doc.addRefreshQueryString("tr", gridConfig.recordTop.ToString());
                            core.doc.addRefreshQueryString("asf", adminData.dstFormId.ToString());
                            core.doc.addRefreshQueryString("cid", adminData.adminContent.id.ToString());
                            core.doc.addRefreshQueryString(RequestNameTitleExtension, GenericController.encodeRequestVariable(adminData.editViewTitleSuffix));
                            int WhereCount = 0;
                            foreach (var kvp in adminData.wherePair) {
                                core.doc.addRefreshQueryString("wl" + WhereCount, kvp.Key);
                                core.doc.addRefreshQueryString("wr" + WhereCount, kvp.Value);
                                WhereCount++;
                            }
                            //
                            // ----- Filter Data Table
                            //
                            string IndexFilterContent = "";
                            string IndexFilterHead = "";
                            string IndexFilterJS = "";
                            //
                            // Filter Nav - if enabled, just add another cell to the row
                            if (core.visitProperty.getBoolean("IndexFilterOpen", false)) {
                                //
                                // Ajax Filter Open
                                //
                                IndexFilterHead = ""
                                    + Environment.NewLine + "<div class=\"ccHeaderCon\">"
                                    + Environment.NewLine + "<div id=\"IndexFilterHeCursorTypeEnum.ADOPENed\" class=\"opened\">"
                                    + "\r<table border=0 cellpadding=0 cellspacing=0 width=\"100%\"><tr>"
                                    + "\r<td valign=Middle class=\"left\">Filters</td>"
                                    + "\r<td valign=Middle class=\"right\"><a href=\"#\" onClick=\"CloseIndexFilter();return false\">" + iconClose_White + "</i></a></td>"
                                    + "\r</tr></table>"
                                    + Environment.NewLine + "</div>"
                                    + Environment.NewLine + "<div id=\"IndexFilterHeadClosed\" class=\"closed\" style=\"display:none;\">"
                                    + "\r<a href=\"#\" onClick=\"OpenIndexFilter();return false\">" + iconOpen_White + "</i></a>"
                                    + Environment.NewLine + "</div>"
                                    + Environment.NewLine + "</div>"
                                    + "";
                                IndexFilterContent = ""
                                    + Environment.NewLine + "<div class=\"ccContentCon\">"
                                    + Environment.NewLine + "<div id=\"IndexFilterContentOpened\" class=\"opened\">" + getForm_IndexFilterContent(core, adminData) + "<img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"200\" height=\"1\" style=\"clear:both\"></div>"
                                    + Environment.NewLine + "<div id=\"IndexFilterContentClosed\" class=\"closed\" style=\"display:none;\">" + adminIndexFilterClosedLabel + "</div>"
                                    + Environment.NewLine + "</div>";
                                IndexFilterJS = ""
                                    + Environment.NewLine + "<script Language=\"JavaScript\" type=\"text/javascript\">"
                                    + Environment.NewLine + "function CloseIndexFilter()  {SetDisplay('IndexFilterHeCursorTypeEnum.ADOPENed','none');SetDisplay('IndexFilterContentOpened','none');SetDisplay('IndexFilterHeadClosed','block');SetDisplay('IndexFilterContentClosed','block');cj.ajax.qs('" + RequestNameAjaxFunction + "=" + AjaxCloseIndexFilter + "','','')}"
                                    + Environment.NewLine + "function OpenIndexFilter()  {SetDisplay('IndexFilterHeCursorTypeEnum.ADOPENed','block');SetDisplay('IndexFilterContentOpened','block');SetDisplay('IndexFilterHeadClosed','none');SetDisplay('IndexFilterContentClosed','none');cj.ajax.qs('" + RequestNameAjaxFunction + "=" + AjaxOpenIndexFilter + "&cid=" + adminData.adminContent.id + "','','')}"
                                    + Environment.NewLine + "</script>";
                            } else {
                                //
                                // Ajax Filter Closed
                                //
                                IndexFilterHead = ""
                                    + Environment.NewLine + "<div class=\"ccHeaderCon\">"
                                    + Environment.NewLine + "<div id=\"IndexFilterHeCursorTypeEnum.ADOPENed\" class=\"opened\" style=\"display:none;\">"
                                    + "\r<table border=0 cellpadding=0 cellspacing=0 width=\"100%\"><tr>"
                                    + "\r<td valign=Middle class=\"left\">Filter</td>"
                                    + "\r<td valign=Middle class=\"right\"><a href=\"#\" onClick=\"CloseIndexFilter();return false\">" + iconClose_White + "</i></a></td>"
                                    + "\r</tr></table>"
                                    + Environment.NewLine + "</div>"
                                    + Environment.NewLine + "<div id=\"IndexFilterHeadClosed\" class=\"closed\">"
                                    + "\r<a href=\"#\" onClick=\"OpenIndexFilter();return false\">" + iconOpen_White + "</i></a>"
                                    + Environment.NewLine + "</div>"
                                    + Environment.NewLine + "</div>"
                                    + "";
                                IndexFilterContent = ""
                                    + Environment.NewLine + "<div class=\"ccContentCon\">"
                                    + Environment.NewLine + "<div id=\"IndexFilterContentOpened\" class=\"opened\" style=\"display:none;\"><div style=\"text-align:center;\"><img src=\"" + cdnPrefix + "images/ajax-loader-small.gif\" width=16 height=16></div></div>"
                                    + Environment.NewLine + "<div id=\"IndexFilterContentClosed\" class=\"closed\">" + adminIndexFilterClosedLabel + "</div>"
                                    + Environment.NewLine + "<div id=\"IndexFilterContentMinWidth\" style=\"display:none;\"><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=\"200\" height=\"1\" style=\"clear:both\"></div>"
                                    + Environment.NewLine + "</div>";
                                string AjaxQS = GenericController.modifyQueryString(core.doc.refreshQueryString, RequestNameAjaxFunction, AjaxOpenIndexFilterGetContent);
                                IndexFilterJS = ""
                                    + Environment.NewLine + "<script Language=\"JavaScript\" type=\"text/javascript\">"
                                    + Environment.NewLine + "var IndexFilterPop=false;"
                                    + Environment.NewLine + "function CloseIndexFilter()  {SetDisplay('IndexFilterHeCursorTypeEnum.ADOPENed','none');SetDisplay('IndexFilterHeadClosed','block');SetDisplay('IndexFilterContentOpened','none');SetDisplay('IndexFilterContentMinWidth','none');SetDisplay('IndexFilterContentClosed','block');cj.ajax.qs('" + RequestNameAjaxFunction + "=" + AjaxCloseIndexFilter + "','','')}"
                                    + Environment.NewLine + "function OpenIndexFilter()  {SetDisplay('IndexFilterHeCursorTypeEnum.ADOPENed','block');SetDisplay('IndexFilterHeadClosed','none');SetDisplay('IndexFilterContentOpened','block');SetDisplay('IndexFilterContentMinWidth','block');SetDisplay('IndexFilterContentClosed','none');if(!IndexFilterPop){cj.ajax.qs('" + AjaxQS + "','','IndexFilterContentOpened');IndexFilterPop=true;}else{cj.ajax.qs('" + RequestNameAjaxFunction + "=" + AjaxOpenIndexFilter + "','','');}}"
                                    + Environment.NewLine + "</script>";
                            }
                            //
                            // -- beta test moving grid to controller to be used for many-to-many data in teh redirect field types
                            string grid = ListGridController.get(core, adminData, gridConfig, userContentPermissions, sql, datasource, FieldUsedInColumns, IsLookupFieldValid);
                            string formContent = ""
                                + "<table ID=\"DataFilterTable\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"Background-Color:white;\">"
                                + "<tr>"
                                + "<td valign=top style=\"border-right:1px solid black;\" class=\"ccToolsCon\">" + IndexFilterJS + IndexFilterHead + IndexFilterContent + "</td>"
                                + "<td width=\"99%\" valign=top>" + grid + "</td>"
                                + "</tr>"
                                + "</table>";
                            //
                            // ----- ButtonBar
                            //
                            string ButtonBar = AdminUIController.getForm_Index_ButtonBar(core, AllowAdd, allowDelete, gridConfig.pageNumber, gridConfig.recordsPerPage, recordCnt, adminData.adminContent.name);
                            string titleRow = ListView.getForm_Index_Header(core, gridConfig, adminData.adminContent, recordCnt, ContentAccessLimitMessage);
                            //
                            // Assemble LiveWindowTable
                            //
                            Stream.add(ButtonBar);
                            Stream.add(AdminUIController.getSectionHeader(core, "", titleRow));
                            Stream.add(formContent);
                            Stream.add(ButtonBar);
                            Stream.add(HtmlController.inputHidden(rnAdminSourceForm, AdminFormIndex));
                            Stream.add(HtmlController.inputHidden("cid", adminData.adminContent.id));
                            Stream.add(HtmlController.inputHidden("indexGoToPage", ""));
                            Stream.add(HtmlController.inputHidden("Columncnt", gridConfig.columns.Count));
                            core.html.addTitle(adminData.adminContent.name, "admin list view");
                        }
                    }
                }
                result = HtmlController.form(core, Stream.text, "", "adminForm");
                //
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Title Bar for the index page
        /// </summary>
        /// <param name="core"></param>
        /// <param name="gridConfig"></param>
        /// <param name="adminContext.content"></param>
        /// <param name="recordCnt"></param>
        /// <param name="ContentAccessLimitMessage"></param>
        /// <returns></returns>
        public static string getForm_Index_Header(CoreController core, GridConfigClass gridConfig, ContentMetadataModel content, int recordCnt, string ContentAccessLimitMessage) {
            var filterLine = new StringBuilder();
            filterLine.Append((gridConfig.activeOnly) ? ", only active records" : "");
            string filterLastEdited = "";
            if (gridConfig.lastEditedByMe) {
                filterLastEdited = filterLastEdited + " by " + core.session.user.name;
            }
            if (gridConfig.lastEditedPast30Days) {
                filterLastEdited += " in the past 30 days";
            }
            if (gridConfig.lastEditedPast7Days) {
                filterLastEdited += " in the week";
            }
            if (gridConfig.lastEditedToday) {
                filterLastEdited += " today";
            }
            if (!string.IsNullOrEmpty(filterLastEdited)) {
                filterLine.Append(", last edited" + filterLastEdited);
            }
            foreach (var kvp in gridConfig.findWords) {
                IndexConfigFindWordClass findWord = kvp.Value;
                if (!string.IsNullOrEmpty(findWord.Name)) {
                    var fieldMeta = ContentMetadataModel.getField(core, content, findWord.Name);
                    if (fieldMeta != null) {
                        string FieldCaption = fieldMeta.caption;
                        switch (findWord.MatchOption) {
                            case FindWordMatchEnum.MatchFalse: {
                                    filterLine.Append(", " + FieldCaption + " is '" + findWord.Value + "' (false)");
                                    break;
                                }
                            case FindWordMatchEnum.MatchTrue: {
                                    filterLine.Append(", " + FieldCaption + " is '" + findWord.Value + "' (true)");
                                    break;
                                }
                            case FindWordMatchEnum.MatchEmpty: {
                                    filterLine.Append(", " + FieldCaption + " is empty");
                                    break;
                                }
                            case FindWordMatchEnum.MatchEquals: {
                                    filterLine.Append(", " + FieldCaption + " = " + findWord.Value);
                                    break;
                                }
                            case FindWordMatchEnum.MatchGreaterThan: {
                                    filterLine.Append(", " + FieldCaption + " > " + findWord.Value);
                                    break;
                                }
                            case FindWordMatchEnum.matchincludes: {
                                    filterLine.Append(", " + FieldCaption + " includes '" + findWord.Value + "'");
                                    break;
                                }
                            case FindWordMatchEnum.MatchLessThan: {
                                    filterLine.Append(", " + FieldCaption + " < " + findWord.Value + " ");
                                    break;
                                }
                            case FindWordMatchEnum.MatchNotEmpty: {
                                    filterLine.Append(", " + FieldCaption + " is not empty");
                                    break;
                                }
                            default: {
                                    // no match
                                    break;
                                }
                        }

                    }
                }
            }
            if (gridConfig.subCDefID > 0) {
                string ContentName = MetadataController.getContentNameByID(core, gridConfig.subCDefID);
                if (!string.IsNullOrEmpty(ContentName)) {
                    filterLine.Append(", in Sub-content '" + ContentName + "'");
                }
            }
            //
            // add groups to caption
            //
            if ((content.tableName.ToLowerInvariant() == "ccmembers") && (gridConfig.groupListCnt > 0)) {
                var groups = new List<string>();
                for (int ptr = 0; ptr < gridConfig.groupListCnt; ptr++) {
                    if (!string.IsNullOrEmpty(gridConfig.groupList[ptr])) {
                        groups.Add(gridConfig.groupList[ptr]);
                    }
                }
                if (groups.Count > 0) {
                    filterLine.Append(", in group(s) " + string.Join(",", groups));
                }
            }
            //
            // add sort details to caption
            //
            string sortLine = "";
            foreach (var kvp in gridConfig.sorts) {
                IndexConfigSortClass sort = kvp.Value;
                if (sort.direction > 0) {
                    sortLine = sortLine + ", then " + content.fields[sort.fieldName].caption;
                    if (sort.direction > 1) {
                        sortLine += " reverse";
                    }
                }
            }
            string pageNavigation = getForm_index_pageNavigation(core, gridConfig.pageNumber, gridConfig.recordsPerPage, recordCnt, content.name);
            //
            // ----- TitleBar
            //
            string Title = HtmlController.div("<strong>" + content.name + "</strong><div style=\"float:right;\">" + pageNavigation + "</div>");
            if (!filterLine.Length.Equals(0)) {
                string link = "/" + core.appConfig.adminRoute + "?cid=" + content.id + "&af=1&IndexFilterRemoveAll=1";
                Title += HtmlController.div(getDeleteLink(link) + "&nbsp;Filter: " + HtmlController.encodeHtml(filterLine.ToString().Substring(1)));
            }
            if (!string.IsNullOrEmpty(sortLine)) {
                string link = "/" + core.appConfig.adminRoute + "?cid=" + content.id + "&af=1&IndexSortRemoveAll=1";
                Title += HtmlController.div(getDeleteLink(link) + "&nbsp;Sort: " + HtmlController.encodeHtml(sortLine.Substring(6)));
            }
            if (!string.IsNullOrEmpty(ContentAccessLimitMessage)) {
                Title += "<div style=\"clear:both\">" + ContentAccessLimitMessage + "</div>";
            }
            return Title;
        }
        //   
        //========================================================================================
        /// <summary>
        /// Process request input on the gridConfig
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <param name="gridConfig"></param>
        public static void setIndexSQL_ProcessIndexConfigRequests(CoreController core, AdminDataModel adminData, ref GridConfigClass gridConfig) {
            try {
                if (!gridConfig.loaded) {
                    gridConfig = GridConfigClass.get(core, adminData);
                }
                //
                // ----- Page number
                string VarText = core.docProperties.getText("rt");
                if (!string.IsNullOrEmpty(VarText)) {
                    gridConfig.recordTop = GenericController.encodeInteger(VarText);
                }
                //
                VarText = core.docProperties.getText("RS");
                if (!string.IsNullOrEmpty(VarText)) {
                    gridConfig.recordsPerPage = GenericController.encodeInteger(VarText);
                }
                if (gridConfig.recordsPerPage <= 0) {
                    gridConfig.recordsPerPage = Constants.RecordsPerPageDefault;
                }
                gridConfig.pageNumber = encodeInteger(1 + Math.Floor(gridConfig.recordTop / (double)gridConfig.recordsPerPage));
                //
                //
                // ----- Process indexGoToPage value
                int TestInteger = core.docProperties.getInteger("indexGoToPage");
                if (TestInteger > 0) {
                    gridConfig.pageNumber = TestInteger;
                    gridConfig.recordTop = DbController.getStartRecord(gridConfig.recordsPerPage, gridConfig.pageNumber);
                } else {
                    //
                    // ----- Read filter changes and First/Next/Previous from form
                    string Button = core.docProperties.getText(RequestNameButton);
                    if (!string.IsNullOrEmpty(Button)) {
                        int ColumnCnt = 0;
                        switch (adminData.srcFormButton) {
                            case ButtonFirst:
                                //
                                // Force to first page
                                gridConfig.pageNumber = 1;
                                gridConfig.recordTop = DbController.getStartRecord(gridConfig.recordsPerPage, gridConfig.pageNumber);
                                break;
                            case ButtonNext:
                                //
                                // Go to next page
                                gridConfig.pageNumber += 1;
                                gridConfig.recordTop = DbController.getStartRecord(gridConfig.recordsPerPage, gridConfig.pageNumber);
                                break;
                            case ButtonPrevious:
                                //
                                // Go to previous page
                                gridConfig.pageNumber -= 1;
                                if (gridConfig.pageNumber <= 0) {
                                    gridConfig.pageNumber = 1;
                                }
                                gridConfig.recordTop = DbController.getStartRecord(gridConfig.recordsPerPage, gridConfig.pageNumber);
                                break;
                            case ButtonFind:
                                //
                                // Find (change search criteria and go to first page)
                                gridConfig.pageNumber = 1;
                                gridConfig.recordTop = DbController.getStartRecord(gridConfig.recordsPerPage, gridConfig.pageNumber);
                                ColumnCnt = core.docProperties.getInteger("ColumnCnt");
                                if (ColumnCnt > 0) {
                                    int ColumnPtr = 0;
                                    for (ColumnPtr = 0; ColumnPtr < ColumnCnt; ColumnPtr++) {
                                        string FindName = core.docProperties.getText("FindName" + ColumnPtr).ToLowerInvariant();
                                        if (!string.IsNullOrEmpty(FindName)) {
                                            if (adminData.adminContent.fields.ContainsKey(FindName.ToLowerInvariant())) {
                                                string FindValue = encodeText(core.docProperties.getText("FindValue" + ColumnPtr)).Trim(' ');
                                                if (string.IsNullOrEmpty(FindValue)) {
                                                    //
                                                    // -- find blank, if name in list, remove it
                                                    if (gridConfig.findWords.ContainsKey(FindName)) {
                                                        gridConfig.findWords.Remove(FindName);
                                                    }
                                                } else {
                                                    //
                                                    // -- nonblank find, store it
                                                    if (gridConfig.findWords.ContainsKey(FindName)) {
                                                        gridConfig.findWords.Remove(FindName);

                                                    }
                                                    ContentFieldMetadataModel field = adminData.adminContent.fields[FindName.ToLowerInvariant()];
                                                    var findWord = new IndexConfigFindWordClass {
                                                        Name = FindName,
                                                        Value = FindValue
                                                    };
                                                    switch (field.fieldTypeId) {
                                                        case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                                                        case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                                        case CPContentBaseClass.FieldTypeIdEnum.Float:
                                                        case CPContentBaseClass.FieldTypeIdEnum.Integer:
                                                        case CPContentBaseClass.FieldTypeIdEnum.MemberSelect:
                                                        case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                                                findWord.MatchOption = FindWordMatchEnum.MatchEquals;
                                                                break;
                                                            }
                                                        case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                                                if (encodeBoolean(FindValue)) {
                                                                    findWord.MatchOption = FindWordMatchEnum.MatchTrue;
                                                                } else {
                                                                    findWord.MatchOption = FindWordMatchEnum.MatchFalse;
                                                                }
                                                                break;
                                                            }
                                                        default: {
                                                                findWord.MatchOption = FindWordMatchEnum.matchincludes;
                                                                break;
                                                            }
                                                    }
                                                    gridConfig.findWords.Add(FindName, findWord);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            default:
                                // do nothing
                                break;
                        }
                    }
                }
                //
                // Process Filter form
                if (core.docProperties.getBoolean("IndexFilterRemoveAll")) {
                    //
                    // Remove all filters
                    gridConfig.findWords = new Dictionary<string, IndexConfigFindWordClass>();
                    gridConfig.groupListCnt = 0;
                    gridConfig.subCDefID = 0;
                    gridConfig.activeOnly = false;
                    gridConfig.lastEditedByMe = false;
                    gridConfig.lastEditedToday = false;
                    gridConfig.lastEditedPast7Days = false;
                    gridConfig.lastEditedPast30Days = false;
                } else {
                    int VarInteger;
                    //
                    // Add CDef
                    VarInteger = core.docProperties.getInteger("IndexFilterAddCDef");
                    if (VarInteger != 0) {
                        gridConfig.subCDefID = VarInteger;
                        gridConfig.pageNumber = 1;
                    }
                    //
                    // Remove CDef
                    VarInteger = core.docProperties.getInteger("IndexFilterRemoveCDef");
                    if (VarInteger != 0) {
                        gridConfig.subCDefID = 0;
                        gridConfig.pageNumber = 1;
                    }
                    //
                    // Add Groups
                    VarText = core.docProperties.getText("IndexFilterAddGroup").ToLowerInvariant();
                    int Ptr = 0;
                    if (!string.IsNullOrEmpty(VarText)) {
                        if (gridConfig.groupListCnt > 0) {
                            for (Ptr = 0; Ptr < gridConfig.groupListCnt; Ptr++) {
                                if (VarText == gridConfig.groupList[Ptr]) {
                                    break;
                                }
                            }
                        }
                        if ((Ptr == gridConfig.groupListCnt) && (gridConfig.groupListCnt < GridConfigClass.groupListCntMax)) {
                            gridConfig.groupList[gridConfig.groupListCnt] = VarText;
                            gridConfig.groupListCnt += 1;
                            gridConfig.pageNumber = 1;
                        }
                    }
                    //
                    // Remove Groups
                    VarText = core.docProperties.getText("IndexFilterRemoveGroup").ToLowerInvariant();
                    if (!string.IsNullOrEmpty(VarText)) {
                        if (gridConfig.groupListCnt > 0) {
                            for (Ptr = 0; Ptr < gridConfig.groupListCnt; Ptr++) {
                                if (gridConfig.groupList[Ptr] == VarText) {
                                    gridConfig.groupList[Ptr] = "";
                                    gridConfig.pageNumber = 1;
                                    break;
                                }
                            }
                        }
                    }
                    //
                    // Remove FindWords
                    VarText = core.docProperties.getText("IndexFilterRemoveFind").ToLowerInvariant();
                    if (!string.IsNullOrEmpty(VarText)) {
                        if (gridConfig.findWords.ContainsKey(VarText)) {
                            gridConfig.findWords.Remove(VarText);
                        }
                    }
                    //
                    // Read ActiveOnly
                    VarText = core.docProperties.getText("IndexFilterActiveOnly");
                    if (!string.IsNullOrEmpty(VarText)) {
                        gridConfig.activeOnly = GenericController.encodeBoolean(VarText);
                        gridConfig.pageNumber = 1;
                    }
                    //
                    // Read LastEditedByMe
                    VarText = core.docProperties.getText("IndexFilterLastEditedByMe");
                    if (!string.IsNullOrEmpty(VarText)) {
                        gridConfig.lastEditedByMe = GenericController.encodeBoolean(VarText);
                        gridConfig.pageNumber = 1;
                    }
                    //
                    // Last Edited Past 30 Days
                    VarText = core.docProperties.getText("IndexFilterLastEditedPast30Days");
                    if (!string.IsNullOrEmpty(VarText)) {
                        gridConfig.lastEditedPast30Days = GenericController.encodeBoolean(VarText);
                        gridConfig.lastEditedPast7Days = false;
                        gridConfig.lastEditedToday = false;
                        gridConfig.pageNumber = 1;
                    } else {
                        //
                        // Past 7 Days
                        VarText = core.docProperties.getText("IndexFilterLastEditedPast7Days");
                        if (!string.IsNullOrEmpty(VarText)) {
                            gridConfig.lastEditedPast30Days = false;
                            gridConfig.lastEditedPast7Days = GenericController.encodeBoolean(VarText);
                            gridConfig.lastEditedToday = false;
                            gridConfig.pageNumber = 1;
                        } else {
                            //
                            // Read LastEditedToday
                            VarText = core.docProperties.getText("IndexFilterLastEditedToday");
                            if (!string.IsNullOrEmpty(VarText)) {
                                gridConfig.lastEditedPast30Days = false;
                                gridConfig.lastEditedPast7Days = false;
                                gridConfig.lastEditedToday = GenericController.encodeBoolean(VarText);
                                gridConfig.pageNumber = 1;
                            }
                        }
                    }
                    //
                    // Read IndexFilterOpen
                    VarText = core.docProperties.getText("IndexFilterOpen");
                    if (!string.IsNullOrEmpty(VarText)) {
                        gridConfig.open = GenericController.encodeBoolean(VarText);
                        gridConfig.pageNumber = 1;
                    }
                    if (core.docProperties.getBoolean("IndexSortRemoveAll")) {
                        //
                        // Remove all filters
                        gridConfig.sorts = new Dictionary<string, IndexConfigSortClass>();
                    } else {
                        //
                        // SortField
                        string setSortField = core.docProperties.getText("SetSortField").ToLowerInvariant();
                        if (!string.IsNullOrEmpty(setSortField)) {
                            bool sortFound = gridConfig.sorts.ContainsKey(setSortField);
                            int sortDirection = core.docProperties.getInteger("SetSortDirection");
                            if (!sortFound) {
                                gridConfig.sorts.Add(setSortField, new IndexConfigSortClass {
                                    fieldName = setSortField,
                                    direction = 1,
                                    order = gridConfig.sorts.Count + 1
                                });
                            } else if (sortDirection > 0) {
                                gridConfig.sorts[setSortField].direction = sortDirection;
                            } else {
                                gridConfig.sorts.Remove(setSortField);
                                int sortOrder = 1;
                                foreach (var kvp in gridConfig.sorts) {
                                    kvp.Value.order = sortOrder++;
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //   
        //========================================================================================
        //
        public static void setIndexSQL(CoreController core, AdminDataModel adminData, GridConfigClass gridConfig, ref bool Return_AllowAccess, ref string return_sqlFieldList, ref string return_sqlFrom, ref string return_SQLWhere, ref string return_SQLOrderBy, ref bool return_IsLimitedToSubContent, ref string return_ContentAccessLimitMessage, ref Dictionary<string, bool> FieldUsedInColumns, Dictionary<string, bool> IsLookupFieldValid) {
            try {
                Return_AllowAccess = true;
                //
                // ----- Workflow Fields
                return_sqlFieldList += adminData.adminContent.tableName + ".ID";
                //
                // ----- From Clause - build joins for Lookup fields in columns, in the findwords, and in sorts
                return_sqlFrom = adminData.adminContent.tableName;
                int FieldPtr = 0;
                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
                    //
                    // quick fix for a replacement for the old fieldPtr (so multiple for loops will always use the same "table"+ptr string
                    FieldPtr = field.id;
                    bool IncludedInColumns = false;
                    bool IncludedInLeftJoin = false;
                    if (!IsLookupFieldValid.ContainsKey(field.nameLc)) {
                        IsLookupFieldValid.Add(field.nameLc, false);
                    }
                    if (!FieldUsedInColumns.ContainsKey(field.nameLc)) {
                        FieldUsedInColumns.Add(field.nameLc, false);
                    }
                    //
                    // test if this field is one of the columns we are displaying
                    IncludedInColumns = (gridConfig.columns.Find(x => (x.Name == field.nameLc)) != null);
                    //
                    // disallow IncludedInColumns if a non-supported field type
                    switch (field.fieldTypeId) {
                        case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                        case CPContentBaseClass.FieldTypeIdEnum.File:
                        case CPContentBaseClass.FieldTypeIdEnum.FileImage:
                        case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                        case CPContentBaseClass.FieldTypeIdEnum.LongText:
                        case CPContentBaseClass.FieldTypeIdEnum.ManyToMany:
                        case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                        case CPContentBaseClass.FieldTypeIdEnum.FileText:
                        case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                        case CPContentBaseClass.FieldTypeIdEnum.HTML:
                        case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                        case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                        case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                            IncludedInColumns = false;
                            break;
                    }
                    if ((field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.MemberSelect) || ((field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Lookup) && (field.lookupContentId != 0))) {
                        //
                        // This is a lookup field -- test if IncludedInLeftJoins
                        IncludedInLeftJoin = IncludedInColumns;
                        if (gridConfig.findWords.Count > 0) {
                            //
                            // test findwords
                            if (gridConfig.findWords.ContainsKey(field.nameLc)) {
                                if (gridConfig.findWords[field.nameLc].MatchOption != FindWordMatchEnum.MatchIgnore) {
                                    IncludedInLeftJoin = true;
                                }
                            }
                        }
                        if ((!IncludedInLeftJoin) && gridConfig.sorts.Count > 0) {
                            //
                            // test sorts
                            if (gridConfig.sorts.ContainsKey(field.nameLc.ToLowerInvariant())) {
                                IncludedInLeftJoin = true;
                            }
                        }
                        if (IncludedInLeftJoin) {
                            //
                            // include this lookup field
                            ContentMetadataModel lookupContentMetadata = null;
                            if (field.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.MemberSelect) {
                                lookupContentMetadata = ContentMetadataModel.createByUniqueName(core, "people");
                            } else {
                                lookupContentMetadata = ContentMetadataModel.create(core, field.lookupContentId);
                            }
                            if (lookupContentMetadata != null) {
                                FieldUsedInColumns[field.nameLc] = true;
                                IsLookupFieldValid[field.nameLc] = true;
                                return_sqlFieldList += ", LookupTable" + FieldPtr + ".Name AS LookupTable" + FieldPtr + "Name";
                                return_sqlFrom = "(" + return_sqlFrom + " LEFT JOIN " + lookupContentMetadata.tableName + " AS LookupTable" + FieldPtr + " ON " + adminData.adminContent.tableName + "." + field.nameLc + " = LookupTable" + FieldPtr + ".ID)";
                            }
                        }
                        //
                    }
                    if (IncludedInColumns) {
                        //
                        // This field is included in the columns, so include it in the select
                        return_sqlFieldList += " ," + adminData.adminContent.tableName + "." + field.nameLc;
                        FieldUsedInColumns[field.nameLc] = true;
                    }
                }
                StringBuilder sqlWhere = new StringBuilder();
                //
                // Sub CDef filter
                if (gridConfig.subCDefID > 0) {
                    var contentMetadata = Contensive.Processor.Models.Domain.ContentMetadataModel.create(core, gridConfig.subCDefID);
                    if (contentMetadata != null) { sqlWhere.Append("AND(" + contentMetadata.legacyContentControlCriteria + ")"); }
                }
                //
                // Return_sqlFrom and Where Clause for Groups filter
                int Ptr = 0;
                if (adminData.adminContent.tableName.ToLowerInvariant() == "ccmembers") {
                    if (gridConfig.groupListCnt > 0) {
                        for (Ptr = 0; Ptr < gridConfig.groupListCnt; Ptr++) {
                            string GroupName = gridConfig.groupList[Ptr];
                            if (!string.IsNullOrEmpty(GroupName)) {
                                int GroupID = MetadataController.getRecordIdByUniqueName(core, "Groups", GroupName);
                                if (GroupID == 0 && GroupName.isNumeric()) {
                                    GroupID = GenericController.encodeInteger(GroupName);
                                }
                                string groupTableAlias = "GroupFilter" + Ptr;
                                sqlWhere.Append("AND(" + groupTableAlias + ".GroupID=" + GroupID + ")and((" + groupTableAlias + ".dateExpires is null)or(" + groupTableAlias + ".dateExpires>" + core.sqlDateTimeMockable + "))");
                                return_sqlFrom = "(" + return_sqlFrom + " INNER JOIN ccMemberRules AS GroupFilter" + Ptr + " ON GroupFilter" + Ptr + ".memberId=ccMembers.ID)";
                            }
                        }
                    }
                }
                //
                // Add Name into Return_sqlFieldList
                return_sqlFieldList += " ," + adminData.adminContent.tableName + ".Name";
                //
                // paste sections together and do where clause
                if (AdminDataModel.userHasContentAccess(core, adminData.adminContent.id)) {
                    //
                    // This person can see all the records
                    sqlWhere.Append("AND(" + adminData.adminContent.legacyContentControlCriteria + ")");
                } else {
                    //
                    // Limit the Query to what they can see
                    return_IsLimitedToSubContent = true;
                    string SubQuery = "";
                    string list = adminData.adminContent.legacyContentControlCriteria;
                    adminData.adminContent.id = adminData.adminContent.id;
                    int SubContentCnt = 0;
                    if (!string.IsNullOrEmpty(list)) {
                        logger.Info($"{core.logCommonMessage},appendlog - adminContext.adminContext.content.contentControlCriteria=" + list);
                        string[] ListSplit = list.Split('=');
                        int Cnt = ListSplit.GetUpperBound(0) + 1;
                        if (Cnt > 0) {
                            for (Ptr = 0; Ptr < Cnt; Ptr++) {
                                int Pos = GenericController.strInstr(1, ListSplit[Ptr], ")");
                                if (Pos > 0) {
                                    int ContentId = GenericController.encodeInteger(ListSplit[Ptr].left(Pos - 1));
                                    if (ContentId > 0 && (ContentId != adminData.adminContent.id) && AdminDataModel.userHasContentAccess(core, ContentId)) {
                                        SubQuery = SubQuery + "OR(" + adminData.adminContent.tableName + ".ContentControlID=" + ContentId + ")";
                                        return_ContentAccessLimitMessage = return_ContentAccessLimitMessage + ", '<a href=\"?cid=" + ContentId + "\">" + MetadataController.getContentNameByID(core, ContentId) + "</a>'";
                                        string SubContactList = "";
                                        SubContactList += "," + ContentId;
                                        SubContentCnt += 1;
                                    }
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(SubQuery)) {
                        //
                        // Person has no access
                        Return_AllowAccess = false;
                        return;
                    } else {
                        sqlWhere.Append("AND(" + SubQuery.Substring(2) + ")");
                        return_ContentAccessLimitMessage = "Your access to " + adminData.adminContent.name + " is limited to Sub-content(s) " + return_ContentAccessLimitMessage.Substring(2);
                    }
                }
                //
                // Where Clause: Active Only
                //
                if (gridConfig.activeOnly) {
                    sqlWhere.Append("AND(" + adminData.adminContent.tableName + ".active<>0)");
                }
                //
                // Where Clause: edited by me
                if (gridConfig.lastEditedByMe) {
                    sqlWhere.Append("AND(" + adminData.adminContent.tableName + ".ModifiedBy=" + core.session.user.id + ")");
                }
                //
                // Where Clause: edited today
                if (gridConfig.lastEditedToday) {
                    sqlWhere.Append("AND(" + adminData.adminContent.tableName + ".ModifiedDate>=" + DbController.encodeSQLDate(core.doc.profileStartTime.Date) + ")");
                }
                //
                // Where Clause: edited past week
                if (gridConfig.lastEditedPast7Days) {
                    sqlWhere.Append("AND(" + adminData.adminContent.tableName + ".ModifiedDate>=" + DbController.encodeSQLDate(core.doc.profileStartTime.Date.AddDays(-7)) + ")");
                }
                //
                // Where Clause: edited past month
                if (gridConfig.lastEditedPast30Days) {
                    sqlWhere.Append("AND(" + adminData.adminContent.tableName + ".ModifiedDate>=" + DbController.encodeSQLDate(core.doc.profileStartTime.Date.AddDays(-30)) + ")");
                }
                //
                // Where Clause: Where Pairs
                if (adminData.adminContent.fields.Count > 0) {
                    foreach (var kvp in adminData.wherePair) {
                        if (adminData.adminContent.fields.ContainsKey(kvp.Key.ToLower())) {
                            var field = adminData.adminContent.fields[kvp.Key.ToLower()];
                            //
                            // found it, add it in the sql
                            string sqlValue = adminData.wherePair[field.nameLc];
                            if ((field.fieldTypeId != CPContentBaseClass.FieldTypeIdEnum.Currency) && (field.fieldTypeId != CPContentClass.FieldTypeIdEnum.Float) && (field.fieldTypeId != CPContentClass.FieldTypeIdEnum.Integer) && (field.fieldTypeId != CPContentClass.FieldTypeIdEnum.Lookup)) {
                                sqlValue = DbController.encodeSQLText(sqlValue);
                            }
                            sqlWhere.Append("and(" + adminData.adminContent.tableName + "." + field.nameLc + "=" + sqlValue + ")");
                            break;
                        }

                    }
                }
                //
                // Where Clause: findwords
                if (gridConfig.findWords.Count > 0) {
                    foreach (var kvp in gridConfig.findWords) {
                        IndexConfigFindWordClass findword = kvp.Value;
                        int FindMatchOption = (int)findword.MatchOption;
                        if (FindMatchOption != (int)FindWordMatchEnum.MatchIgnore) {
                            string FindWordNameLc = GenericController.toLCase(findword.Name);
                            string FindWordValue = findword.Value;
                            //
                            // Get FieldType
                            if (adminData.adminContent.fields.Count > 0) {
                                bool exitFor = false;
                                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                                    ContentFieldMetadataModel field = keyValuePair.Value;
                                    //
                                    // quick fix for a replacement for the old fieldPtr (so multiple for loops will always use the same "table"+ptr string
                                    FieldPtr = field.id;
                                    if (GenericController.toLCase(field.nameLc) == FindWordNameLc) {
                                        switch (field.fieldTypeId) {
                                            case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                                            case CPContentBaseClass.FieldTypeIdEnum.Integer: {
                                                    //
                                                    // integer
                                                    //
                                                    int FindWordValueInteger = GenericController.encodeInteger(FindWordValue);
                                                    switch (FindMatchOption) {
                                                        case (int)FindWordMatchEnum.MatchEmpty: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is null)");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchNotEmpty: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is not null)");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchEquals:
                                                        case (int)FindWordMatchEnum.matchincludes: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + "=" + DbController.encodeSQLNumber(FindWordValueInteger) + ")");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchGreaterThan: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + ">" + DbController.encodeSQLNumber(FindWordValueInteger) + ")");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchLessThan: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + "<" + DbController.encodeSQLNumber(FindWordValueInteger) + ")");
                                                                break;
                                                            }
                                                        default:
                                                            // do nothing
                                                            break;
                                                    }
                                                    exitFor = true;
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                            case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                                    //
                                                    // double
                                                    double FindWordValueDouble = GenericController.encodeNumber(FindWordValue);
                                                    switch (FindMatchOption) {
                                                        case (int)FindWordMatchEnum.MatchEmpty: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is null)");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchNotEmpty: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is not null)");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchGreaterThan: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + ">" + DbController.encodeSQLNumber(FindWordValueDouble) + ")");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchLessThan: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + "<" + DbController.encodeSQLNumber(FindWordValueDouble) + ")");
                                                                break;
                                                            }
                                                        default: {
                                                                // (int)FindWordMatchEnum.MatchEquals:
                                                                // (int)FindWordMatchEnum.matchincludes:
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + "=" + DbController.encodeSQLNumber(FindWordValueDouble) + ")");
                                                                break;
                                                            }
                                                    }
                                                    exitFor = true;
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.File:
                                            case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                                    //
                                                    // Date
                                                    switch (FindMatchOption) {
                                                        case (int)FindWordMatchEnum.MatchEmpty: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is null)");
                                                                break;
                                                            }
                                                        default: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is not null)");
                                                                break;
                                                            }
                                                    }
                                                    exitFor = true;
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                                    //
                                                    // Date
                                                    DateTime findDate = DateTime.MinValue;
                                                    if (GenericController.isDate(FindWordValue)) { findDate = DateTime.Parse(FindWordValue); }
                                                    switch (FindMatchOption) {
                                                        case (int)FindWordMatchEnum.MatchEmpty: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is null)");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchNotEmpty: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is not null)");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchGreaterThan: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + ">" + DbController.encodeSQLDate(findDate) + ")");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchLessThan: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + "<" + DbController.encodeSQLDate(findDate) + ")");
                                                                break;
                                                            }
                                                        default: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + "=" + DbController.encodeSQLDate(findDate) + ")");
                                                                break;
                                                            }
                                                    }
                                                    exitFor = true;
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                                            case CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                                                    //
                                                    // Lookup
                                                    if (IsLookupFieldValid[field.nameLc]) {
                                                        //
                                                        // Content Lookup
                                                        switch (FindMatchOption) {
                                                            case (int)FindWordMatchEnum.MatchEmpty: {
                                                                    sqlWhere.Append("AND(LookupTable" + FieldPtr + ".ID is null)");
                                                                    break;
                                                                }
                                                            case (int)FindWordMatchEnum.MatchNotEmpty: {
                                                                    sqlWhere.Append("AND(LookupTable" + FieldPtr + ".ID is not null)");
                                                                    break;
                                                                }
                                                            case (int)FindWordMatchEnum.MatchEquals: {
                                                                    sqlWhere.Append("AND(LookupTable" + FieldPtr + ".Name=" + DbController.encodeSQLText(FindWordValue) + ")");
                                                                    break;
                                                                }
                                                            case (int)FindWordMatchEnum.matchincludes: {
                                                                    sqlWhere.Append("AND(LookupTable" + FieldPtr + ".Name LIKE " + DbController.encodeSQLText("%" + FindWordValue + "%") + ")");
                                                                    break;
                                                                }
                                                            default:
                                                                // do nothing
                                                                break;
                                                        }
                                                    } else if (!string.IsNullOrEmpty(field.lookupList)) {
                                                        //
                                                        // LookupList
                                                        switch (FindMatchOption) {
                                                            case (int)FindWordMatchEnum.MatchEmpty: {
                                                                    sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is null)");
                                                                    break;
                                                                }
                                                            case (int)FindWordMatchEnum.MatchNotEmpty: {
                                                                    sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is not null)");
                                                                    break;
                                                                }
                                                            default: {
                                                                    string[] lookups = field.lookupList.ToLower(CultureInfo.InvariantCulture).Split(',');
                                                                    string LookupQuery = "(1=0)";
                                                                    //
                                                                    for (int LookupPtr = 0; LookupPtr <= lookups.GetUpperBound(0); LookupPtr++) {
                                                                        if (lookups[LookupPtr].Contains(FindWordValue.ToLower(CultureInfo.InvariantCulture))) {
                                                                            LookupQuery = LookupQuery + "OR(" + adminData.adminContent.tableName + "." + FindWordNameLc + "=" + DbController.encodeSQLNumber(LookupPtr + 1) + ")";
                                                                        }
                                                                    }
                                                                    if (!string.IsNullOrEmpty(LookupQuery)) {
                                                                        sqlWhere.Append("AND(" + LookupQuery + ")");
                                                                    }
                                                                    break;
                                                                }
                                                        }
                                                    }
                                                    exitFor = true;
                                                    break;
                                                }
                                            case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                                    //
                                                    // Boolean
                                                    switch (FindMatchOption) {
                                                        case (int)FindWordMatchEnum.matchincludes: {
                                                                if (GenericController.encodeBoolean(FindWordValue)) {
                                                                    sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + "<>0)");
                                                                } else {
                                                                    sqlWhere.Append("AND((" + adminData.adminContent.tableName + "." + FindWordNameLc + "=0)or(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is null))");
                                                                }
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchTrue: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + "<>0)");
                                                                break;
                                                            }
                                                        default: {
                                                                // FindWordMatchEnum.MatchFalse
                                                                sqlWhere.Append("AND((" + adminData.adminContent.tableName + "." + FindWordNameLc + "=0)or(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is null))");
                                                                break;
                                                            }
                                                    }
                                                    exitFor = true;
                                                    break;
                                                }
                                            default: {
                                                    //
                                                    // Text (and the rest)
                                                    switch (FindMatchOption) {
                                                        case (int)FindWordMatchEnum.MatchEmpty: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is null)");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.MatchNotEmpty: {
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " is not null)");
                                                                break;
                                                            }
                                                        case (int)FindWordMatchEnum.matchincludes: {
                                                                FindWordValue = DbController.encodeSQLText(FindWordValue);
                                                                FindWordValue = FindWordValue.Substring(1, FindWordValue.Length - 2);
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + " LIKE '%" + FindWordValue + "%')");
                                                                break;
                                                            }
                                                        default: {
                                                                // FindWordMatchEnum.MatchEquals
                                                                sqlWhere.Append("AND(" + adminData.adminContent.tableName + "." + FindWordNameLc + "=" + DbController.encodeSQLText(FindWordValue) + ")");
                                                                break;
                                                            }
                                                    }
                                                    exitFor = true;
                                                    break;
                                                }
                                        }
                                    }
                                    //
                                    // -- field found, no need to keep searching
                                    if (exitFor) { break; }
                                }
                            }
                        }
                    }
                }
                return_SQLWhere = sqlWhere.ToString().Substring(3);
                //
                // SQL Order by
                return_SQLOrderBy = "";
                string orderByDelim = " ";
                foreach (var kvp in gridConfig.sorts) {
                    IndexConfigSortClass sort = kvp.Value;
                    string SortFieldName = GenericController.toLCase(sort.fieldName);
                    //
                    // Get FieldType
                    if (adminData.adminContent.fields.ContainsKey(sort.fieldName)) {
                        var tempVar = adminData.adminContent.fields[sort.fieldName];
                        FieldPtr = tempVar.id; // quick fix for a replacement for the old fieldPtr (so multiple for loops will always use the same "table"+ptr string
                        if ((tempVar.fieldTypeId == CPContentBaseClass.FieldTypeIdEnum.Lookup) && IsLookupFieldValid[sort.fieldName]) {
                            return_SQLOrderBy += orderByDelim + "LookupTable" + FieldPtr + ".Name";
                        } else {
                            return_SQLOrderBy += orderByDelim + adminData.adminContent.tableName + "." + SortFieldName;
                        }
                    }
                    if (sort.direction > 1) {
                        return_SQLOrderBy += " Desc";
                    }
                    orderByDelim = ",";
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //==========================================================================================================================================
        /// <summary>
        /// Get index view filter content - remote method
        /// </summary>
        /// <param name="adminData.content"></param>
        /// <returns></returns>
        public static string getForm_IndexFilterContent(CoreController core, AdminDataModel adminData) {
            string returnContent = "";
            try {
                var gridConfig = GridConfigClass.get(core, adminData);
                string RQS = "cid=" + adminData.adminContent.id + "&af=1";
                string Link = string.Empty;
                string QS = string.Empty;
                int Ptr = 0;
                string SubFilterList = string.Empty;
                //
                //-------------------------------------------------------------------------------------
                // Remove filters
                //-------------------------------------------------------------------------------------
                //
                if ((gridConfig.subCDefID > 0) || (gridConfig.groupListCnt != 0) || (gridConfig.findWords.Count != 0) || gridConfig.activeOnly || gridConfig.lastEditedByMe || gridConfig.lastEditedToday || gridConfig.lastEditedPast7Days || gridConfig.lastEditedPast30Days) {
                    //
                    // Remove Filters
                    //
                    returnContent += "<div class=\"ccFilterHead\">Remove&nbsp;Filters</div>";
                    Link = "/" + core.appConfig.adminRoute + "?" + GenericController.modifyQueryString(RQS, "IndexFilterRemoveAll", "1");
                    returnContent += HtmlController.div(getDeleteLink(Link) + "&nbsp;Remove All", "ccFilterSubHead");
                    //
                    // Last Edited Edited by me
                    //
                    SubFilterList = "";
                    if (gridConfig.lastEditedByMe) {
                        Link = "/" + core.appConfig.adminRoute + "?" + GenericController.modifyQueryString(RQS, "IndexFilterLastEditedByMe", 0.ToString(), true);
                        SubFilterList += HtmlController.div(getDeleteLink(Link) + "&nbsp;By&nbsp;Me", "ccFilterIndent ccFilterList");
                    }
                    if (gridConfig.lastEditedToday) {
                        QS = RQS;
                        QS = GenericController.modifyQueryString(QS, "IndexFilterLastEditedToday", 0.ToString(), true);
                        Link = "/" + core.appConfig.adminRoute + "?" + QS;
                        SubFilterList += HtmlController.div(getDeleteLink(Link) + "&nbsp;Today", "ccFilterIndent ccFilterList");
                    }
                    if (gridConfig.lastEditedPast7Days) {
                        QS = RQS;
                        QS = GenericController.modifyQueryString(QS, "IndexFilterLastEditedPast7Days", 0.ToString(), true);
                        Link = "/" + core.appConfig.adminRoute + "?" + QS;
                        SubFilterList += HtmlController.div(getDeleteLink(Link) + "&nbsp;Past Week", "ccFilterIndent ccFilterList");
                    }
                    if (gridConfig.lastEditedPast30Days) {
                        QS = RQS;
                        QS = GenericController.modifyQueryString(QS, "IndexFilterLastEditedPast30Days", 0.ToString(), true);
                        Link = "/" + core.appConfig.adminRoute + "?" + QS;
                        SubFilterList += HtmlController.div(getDeleteLink(Link) + "&nbsp;Past 30 Days", "ccFilterIndent ccFilterList");
                    }
                    if (!string.IsNullOrEmpty(SubFilterList)) {
                        returnContent += "<div class=\"ccFilterSubHead\">Last&nbsp;Edited</div>" + SubFilterList;
                    }
                    //
                    // Sub Content definitions
                    //
                    string SubContentName = null;
                    SubFilterList = "";
                    if (gridConfig.subCDefID > 0) {
                        SubContentName = MetadataController.getContentNameByID(core, gridConfig.subCDefID);
                        QS = RQS;
                        QS = GenericController.modifyQueryString(QS, "IndexFilterRemoveCDef", encodeText(gridConfig.subCDefID));
                        Link = "/" + core.appConfig.adminRoute + "?" + QS;
                        SubFilterList += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + SubContentName + "", "ccFilterIndent");
                    }
                    if (!string.IsNullOrEmpty(SubFilterList)) {
                        returnContent += "<div class=\"ccFilterSubHead\">In Sub-content</div>" + SubFilterList;
                    }
                    //
                    // Group Filter List
                    //
                    string GroupName = null;
                    SubFilterList = "";
                    if (gridConfig.groupListCnt > 0) {
                        for (Ptr = 0; Ptr < gridConfig.groupListCnt; Ptr++) {
                            GroupName = gridConfig.groupList[Ptr];
                            if (!string.IsNullOrEmpty(gridConfig.groupList[Ptr])) {
                                if (GroupName.Length > 30) {
                                    GroupName = GroupName.left(15) + "..." + GroupName.Substring(GroupName.Length - 15);
                                }
                                QS = RQS;
                                QS = GenericController.modifyQueryString(QS, "IndexFilterRemoveGroup", gridConfig.groupList[Ptr]);
                                Link = "/" + core.appConfig.adminRoute + "?" + QS;
                                SubFilterList += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + GroupName + "", "ccFilterIndent");
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(SubFilterList)) {
                        returnContent += "<div class=\"ccFilterSubHead\">In Group(s)</div>" + SubFilterList;
                    }
                    //
                    // Other Filter List
                    //
                    SubFilterList = "";
                    if (gridConfig.activeOnly) {
                        QS = RQS;
                        QS = GenericController.modifyQueryString(QS, "IndexFilterActiveOnly", 0.ToString(), true);
                        Link = "/" + core.appConfig.adminRoute + "?" + QS;
                        SubFilterList += HtmlController.div(getDeleteLink(Link) + "&nbsp;Active&nbsp;Only", "ccFilterIndent ccFilterList");
                    }
                    if (!string.IsNullOrEmpty(SubFilterList)) {
                        returnContent += "<div class=\"ccFilterSubHead\">Other</div>" + SubFilterList;
                    }
                    //
                    // FindWords
                    //
                    foreach (var findWordKvp in gridConfig.findWords) {
                        IndexConfigFindWordClass findWord = findWordKvp.Value;
                        string fieldCaption = (!adminData.adminContent.fields.ContainsKey(findWord.Name.ToLower(CultureInfo.InvariantCulture))) ? findWord.Name : adminData.adminContent.fields[findWord.Name.ToLower(CultureInfo.InvariantCulture)].caption;
                        QS = RQS;
                        QS = GenericController.modifyQueryString(QS, "IndexFilterRemoveFind", findWord.Name);
                        Link = "/" + core.appConfig.adminRoute + "?" + QS;
                        switch (findWord.MatchOption) {
                            case FindWordMatchEnum.matchincludes:
                                returnContent += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + fieldCaption + "&nbsp;includes&nbsp;'" + findWord.Value + "'", "ccFilterIndent");
                                break;
                            case FindWordMatchEnum.MatchEmpty:
                                returnContent += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + fieldCaption + "&nbsp;is&nbsp;empty", "ccFilterIndent");
                                break;
                            case FindWordMatchEnum.MatchEquals:
                                returnContent += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + fieldCaption + "&nbsp;=&nbsp;'" + findWord.Value + "'", "ccFilterIndent");
                                break;
                            case FindWordMatchEnum.MatchFalse:
                                returnContent += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + fieldCaption + "&nbsp;is&nbsp;false", "ccFilterIndent");
                                break;
                            case FindWordMatchEnum.MatchGreaterThan:
                                returnContent += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + fieldCaption + "&nbsp;&gt;&nbsp;'" + findWord.Value + "'", "ccFilterIndent");
                                break;
                            case FindWordMatchEnum.MatchLessThan:
                                returnContent += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + fieldCaption + "&nbsp;&lt;&nbsp;'" + findWord.Value + "'", "ccFilterIndent");
                                break;
                            case FindWordMatchEnum.MatchNotEmpty:
                                returnContent += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + fieldCaption + "&nbsp;is&nbsp;not&nbsp;empty", "ccFilterIndent");
                                break;
                            case FindWordMatchEnum.MatchTrue:
                                returnContent += HtmlController.div(getDeleteLink(Link) + "&nbsp;" + fieldCaption + "&nbsp;is&nbsp;true", "ccFilterIndent");
                                break;
                            default:
                                // do nothing
                                break;
                        }
                    }
                    //
                    returnContent += "<div style=\"border-bottom:1px dotted #808080;\">&nbsp;</div>";
                }
                //
                //-------------------------------------------------------------------------------------
                // Add filters
                //-------------------------------------------------------------------------------------
                //
                returnContent += "<div class=\"ccFilterHead\">Add&nbsp;Filters</div>";
                //
                // Last Edited
                //
                SubFilterList = "";
                if (!gridConfig.lastEditedByMe) {
                    QS = RQS;
                    QS = GenericController.modifyQueryString(QS, "IndexFilterLastEditedByMe", "1", true);
                    Link = "/" + core.appConfig.adminRoute + "?" + QS;
                    SubFilterList = SubFilterList + "<div class=\"ccFilterIndent\"><a class=\"ccFilterLink\" href=\"" + Link + "\">By&nbsp;Me</a></div>";
                }
                if (!gridConfig.lastEditedToday) {
                    QS = RQS;
                    QS = GenericController.modifyQueryString(QS, "IndexFilterLastEditedToday", "1", true);
                    Link = "/" + core.appConfig.adminRoute + "?" + QS;
                    SubFilterList = SubFilterList + "<div class=\"ccFilterIndent\"><a class=\"ccFilterLink\" href=\"" + Link + "\">Today</a></div>";
                }
                if (!gridConfig.lastEditedPast7Days) {
                    QS = RQS;
                    QS = GenericController.modifyQueryString(QS, "IndexFilterLastEditedPast7Days", "1", true);
                    Link = "/" + core.appConfig.adminRoute + "?" + QS;
                    SubFilterList = SubFilterList + "<div class=\"ccFilterIndent\"><a class=\"ccFilterLink\" href=\"" + Link + "\">Past Week</a></div>";
                }
                if (!gridConfig.lastEditedPast30Days) {
                    QS = RQS;
                    QS = GenericController.modifyQueryString(QS, "IndexFilterLastEditedPast30Days", "1", true);
                    Link = "/" + core.appConfig.adminRoute + "?" + QS;
                    SubFilterList = SubFilterList + "<div class=\"ccFilterIndent\"><a class=\"ccFilterLink\" href=\"" + Link + "\">Past 30 Days</a></div>";
                }
                if (!string.IsNullOrEmpty(SubFilterList)) {
                    returnContent += "<div class=\"ccFilterSubHead\">Last&nbsp;Edited</div>" + SubFilterList;
                }
                //
                // Sub Content Definitions
                //
                SubFilterList = "";
                var contentList = ContentModel.createList<ContentModel>(core.cpParent, "(contenttableid in (select id from cctables where name=" + DbController.encodeSQLText(adminData.adminContent.tableName) + "))");
                string Caption = null;
                if (contentList.Count > 1) {
                    foreach (var subContent in contentList) {
                        Caption = "<span style=\"white-space:nowrap;\">" + subContent.name + "</span>";
                        QS = RQS;
                        QS = GenericController.modifyQueryString(QS, "IndexFilterAddCDef", subContent.id.ToString(), true);
                        Link = "/" + core.appConfig.adminRoute + "?" + QS;
                        SubFilterList = SubFilterList + "<div class=\"ccFilterIndent\"><a class=\"ccFilterLink\" href=\"" + Link + "\">" + Caption + "</a></div>";
                    }
                    returnContent += "<div class=\"ccFilterSubHead\">In Sub-content</div>" + SubFilterList;
                }
                //
                // people filters
                //
                SubFilterList = "";
                if (adminData.adminContent.tableName.ToLower(CultureInfo.InvariantCulture) == GenericController.toLCase("ccmembers")) {
                    using var csData = new CsModel(core);
                    csData.openSql(core.db.getSQLSelect("ccGroups", "ID,Caption,Name", "(active<>0)", "Caption,Name"));
                    while (csData.ok()) {
                        string Name = csData.getText("Name");
                        Ptr = 0;
                        if (gridConfig.groupListCnt > 0) {
                            for (Ptr = 0; Ptr < gridConfig.groupListCnt; Ptr++) {
                                if (Name == gridConfig.groupList[Ptr]) {
                                    break;
                                }
                            }
                        }
                        if (Ptr == gridConfig.groupListCnt) {
                            int RecordId = csData.getInteger("ID");
                            Caption = csData.getText("Caption");
                            if (string.IsNullOrEmpty(Caption)) {
                                Caption = Name;
                                if (string.IsNullOrEmpty(Caption)) {
                                    Caption = "Group " + RecordId;
                                }
                            }
                            if (Caption.Length > 30) {
                                Caption = Caption.left(15) + "..." + Caption.Substring(Caption.Length - 15);
                            }
                            Caption = "<span style=\"white-space:nowrap;\">" + Caption + "</span>";
                            QS = RQS;
                            if (!string.IsNullOrEmpty(Name.Trim(' '))) {
                                QS = GenericController.modifyQueryString(QS, "IndexFilterAddGroup", Name, true);
                            } else {
                                QS = GenericController.modifyQueryString(QS, "IndexFilterAddGroup", RecordId.ToString(), true);
                            }
                            Link = "/" + core.appConfig.adminRoute + "?" + QS;
                            SubFilterList = SubFilterList + "<div class=\"ccFilterIndent\"><a class=\"ccFilterLink\" href=\"" + Link + "\">" + Caption + "</a></div>";
                        }
                        csData.goNext();
                    }
                }
                if (!string.IsNullOrEmpty(SubFilterList)) {
                    returnContent += "<div class=\"ccFilterSubHead\">In Group(s)</div>" + SubFilterList;
                }
                //
                // Active Only
                //
                SubFilterList = "";
                if (!gridConfig.activeOnly) {
                    QS = RQS;
                    QS = GenericController.modifyQueryString(QS, "IndexFilterActiveOnly", "1", true);
                    Link = "/" + core.appConfig.adminRoute + "?" + QS;
                    SubFilterList = SubFilterList + "<div class=\"ccFilterIndent\"><a class=\"ccFilterLink\" href=\"" + Link + "\">Active&nbsp;Only</a></div>";
                }
                if (!string.IsNullOrEmpty(SubFilterList)) {
                    returnContent += "<div class=\"ccFilterSubHead\">Other</div>" + SubFilterList;
                }
                //
                returnContent += "<div style=\"border-bottom:1px dotted #808080;\">&nbsp;</div>";
                //
                // Advanced Search Link
                //
                QS = RQS;
                QS = GenericController.modifyQueryString(QS, RequestNameAdminSubForm, AdminFormIndex_SubFormAdvancedSearch, true);
                Link = "/" + core.appConfig.adminRoute + "?" + QS;
                returnContent += "<div class=\"ccFilterHead\"><a class=\"ccFilterLink\" href=\"" + Link + "\">Advanced&nbsp;Search</a></div>";
                //
                returnContent += "<div style=\"border-bottom:1px dotted #808080;\">&nbsp;</div>";
                //
                // Set Column Link
                //
                QS = RQS;
                QS = GenericController.modifyQueryString(QS, RequestNameAdminSubForm, AdminFormIndex_SubFormSetColumns, true);
                Link = "/" + core.appConfig.adminRoute + "?" + QS;
                returnContent += "<div class=\"ccFilterHead\"><a class=\"ccFilterLink\" href=\"" + Link + "\">Set&nbsp;Columns</a></div>";
                //
                returnContent += "<div style=\"border-bottom:1px dotted #808080;\">&nbsp;</div>";
                //
                // Import Link
                //
                QS = RQS;
                QS = GenericController.modifyQueryString(QS, rnAdminForm, AdminFormImportWizard, true);
                Link = "/" + core.appConfig.adminRoute + "?" + QS;
                returnContent += "<div class=\"ccFilterHead\"><a class=\"ccFilterLink\" href=\"" + Link + "\">Import</a></div>";
                //
                returnContent += "<div style=\"border-bottom:1px dotted #808080;\">&nbsp;</div>";
                //
                // Export Link
                //
                QS = RQS;
                QS = GenericController.modifyQueryString(QS, RequestNameAdminSubForm, AdminFormIndex_SubFormExport, true);
                Link = "/" + core.appConfig.adminRoute + "?" + QS;
                returnContent += "<div class=\"ccFilterHead\"><a class=\"ccFilterLink\" href=\"" + Link + "\">Export</a></div>";
                //
                returnContent += "<div style=\"border-bottom:1px dotted #808080;\">&nbsp;</div>";
                //
                returnContent = "<div style=\"padding-left:10px;padding-right:10px;\">" + returnContent + "</div>";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnContent;
        }
    }
}
