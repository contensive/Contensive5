
using Contensive.BaseClasses;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// 
    /// </summary>
    public class XmlController {
        /// <summary>
        /// 
        /// </summary>
        private readonly CPClass cp;
        /// <summary>
        /// This should match the Lookup List in the NavIconType field in the Navigator Entry content definition
        /// </summary>
        public string navIconTypeList { get; set; } = "Custom,Advanced,Content,Folder,Email,User,Report,Setting,Tool,Record,Addon,help";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorInteger { get; set; } = "Integer";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorText { get; set; } = "Text";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorLongText { get; set; } = "LongText";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorBoolean { get; set; } = "Boolean";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorDate { get; set; } = "Date";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorFile { get; set; } = "File";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorLookup { get; set; } = "Lookup";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorRedirect { get; set; } = "Redirect";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorCurrency { get; set; } = "Currency";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorImage { get; set; } = "Image";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorFloat { get; set; } = "Float";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorManyToMany { get; set; } = "ManyToMany";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorTextFile { get; set; } = "TextFile";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorCSSFile { get; set; } = "CSSFile";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorXMLFile { get; set; } = "XMLFile";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorJavaScriptFile { get; set; } = "JavascriptFile";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorLink { get; set; } = "Link";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorResourceLink { get; set; } = "ResourceLink";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorMemberSelect { get; set; } = "MemberSelect";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorHTML { get; set; } = "HTML";
        /// <summary>
        /// 
        /// </summary>
        public string fieldDescriptorHTMLFile { get; set; } = "HTMLFile";
        // 
        // ====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        /// <remarks></remarks>
        public XmlController(CPClass cp) {
            this.cp = cp;
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        private class TableClass {
            public string tableName { get; set; }
            public string dataSourceName { get; set; }
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        public string getXMLContentDefinition(string ContentName) {
            try {
                bool IncludeBaseFields = false;
                // 
                string ContentSelectList = ""
                    + " id,name,active,adminonly,allowadd"
                    + ",allowcalendarevents,allowcontentchildtool,allowcontenttracking,allowdelete,allowmetacontent"
                    + ",allowtopicrules,AllowWorkflowAuthoring,AuthoringTableID"
                    + ",ContentTableID,DefaultSortMethodID,DeveloperOnly,DropDownFieldList"
                    + ",EditorGroupID,ParentID,ccGuid,IsBaseContent"
                    + ",IconLink,IconHeight,IconWidth,IconSprites"
                    + ",NavTypeID,AddonCategoryId";

                string FieldSelectList = ""
                    + "f.ID,f.Name,f.contentid,f.Active,f.AdminOnly,f.Authorable,f.Caption,f.DeveloperOnly,f.EditSortPriority,f.Type,f.HTMLContent"
                    + ",f.IndexColumn,f.IndexSortDirection,f.IndexSortPriority,f.RedirectID,f.RedirectPath,f.Required"
                    + ",f.TextBuffered,f.UniqueName,f.DefaultValue,f.RSSTitleField,f.RSSDescriptionField,f.MemberSelectGroupID"
                    + ",f.EditTab,f.Scramble,f.LookupList,f.NotEditable,f.Password,f.readonly,f.ManyToManyRulePrimaryField"
                    + ",f.ManyToManyRuleSecondaryField,'' as HelpMessageDeprecated,f.ModifiedBy,f.IsBaseField,f.LookupContentID"
                    + ",f.RedirectContentID,f.ManyToManyContentID,f.ManyToManyRuleContentID"
                    + ",h.helpdefault,h.helpcustom,f.IndexWidth,f.editorAddonId,a.ccguid as editorAddonGuid"
                    + $"{(!versionIsOlder(cp.core.siteProperties.dataBuildVersion, "24.8.26.0") ? ",editGroup" : ",'' as editGroup")}";
                // 
                int FieldCnt = 0;
                string fieldName;
                int fieldContentID;
                int lastFieldID;
                int RecordID;
                string RecordName;
                int AuthoringTableID;
                string HelpDefault;
                int HelpCnt;
                int fieldId;
                string fieldType;
                int ContentTableID;
                string TableName;
                string DataSourceName;
                int DefaultSortMethodID;
                string DefaultSortMethod;
                int EditorGroupID;
                string EditorGroupName;
                int ParentID;
                string ParentName;
                int ContentID = 0;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                string iContentName;
                string sql;
                bool FoundMenuTable = false;
                //
                CPCSBaseClass cs = cp.CSNew();
                // 
                iContentName = ContentName;
                if (!string.IsNullOrEmpty(iContentName)) {
                    sql = "select id from cccontent where name=" + cp.Db.EncodeSQLText(iContentName);
                    if (cs.OpenSQL(sql)) { ContentID = cs.GetInteger("id"); }
                    cs.Close();
                }
                if (!string.IsNullOrEmpty(iContentName) && (ContentID == 0)) {
                    return string.Empty;
                }
                {
                    // 
                    // Build table lookup
                    // 
                    Dictionary<int, TableClass> tables = new Dictionary<int, TableClass>();
                    sql = "select T.ID,T.Name as TableName,D.Name as DataSourceName from ccTables T Left Join ccDataSources D on D.ID=T.DataSourceID";
                    if (cs.OpenSQL(sql)) {
                        do {
                            TableClass table = new TableClass();
                            table.tableName = cs.GetText("TableName");
                            table.dataSourceName = cs.GetText("DataSourceName");
                            tables.Add(cs.GetInteger("id"), table);
                            cs.GoNext();
                        }
                        while (cs.OK());
                    }
                    cs.Close();
                    // 
                    // 
                    // Build SortMethod lookup
                    // 
                    Dictionary<int, string> sorts = new Dictionary<int, string>();
                    sql = "select ID,Name from ccSortMethods";
                    if (cs.OpenSQL(sql)) {
                        do {
                            sorts.Add(cs.GetInteger("id"), cs.GetText("name"));
                            cs.GoNext();
                        }
                        while (cs.OK());
                    }
                    cs.Close();
                    // 
                    // Build groups lookup
                    // 
                    Dictionary<int, string> groups = new Dictionary<int, string>();
                    sql = "select ID,Name from ccGroups";
                    if (cs.OpenSQL(sql)) {
                        do {
                            groups.Add(cs.GetInteger("id"), cs.GetText("name"));
                            cs.GoNext();
                        }
                        while (cs.OK());
                    }
                    cs.Close();
                    // 
                    // Build Content lookup
                    // 
                    sql = "select id,name from ccContent";
                    Dictionary<int, string> contents = new Dictionary<int, string>();
                    if (cs.OpenSQL(sql)) {
                        do {
                            contents.Add(cs.GetInteger("id"), cs.GetText("name"));
                            cs.GoNext();
                        }
                        while (cs.OK());
                    }
                    cs.Close();
                    // 
                    if (ContentID != 0) {
                        sql = "select " + ContentSelectList + " from ccContent where (id=" + ContentID + ")and(contenttableid is not null)and(contentcontrolid is not null) order by id";
                    } else {
                        sql = "select " + ContentSelectList + " from ccContent where (name<>'')and(name is not null)and(contenttableid is not null)and(contentcontrolid is not null) order by id";
                    }
                    using (CPCSBaseClass contentCs = cp.CSNew()) {
                        if (contentCs.OpenSQL(sql)) {
                            // 
                            // ----- <cdef>
                            // 
                            bool IsBaseContent = (contentCs.GetBoolean("isBaseContent"));
                            iContentName = GetRSXMLAttribute(contentCs, "Name");
                            ContentID = (contentCs.GetInteger("ID"));
                            sb.Append(System.Environment.NewLine + "\t<CDef");
                            sb.Append($" Name=\"{iContentName}\"");
                            if ((!IsBaseContent) || IncludeBaseFields) {
                                sb.Append($" Active=\"{GetRSXMLAttribute(contentCs, "Active")}\"");
                                sb.Append($" AdminOnly=\"{GetRSXMLAttribute(contentCs, "AdminOnly")}\"");
                                sb.Append($" AllowAdd=\"{GetRSXMLAttribute(contentCs, "AllowAdd")}\"");
                                sb.Append($" AllowCalendarEvents=\"{GetRSXMLAttribute(contentCs, "AllowCalendarEvents")}\"");
                                sb.Append($" AllowContentChildTool=\"{GetRSXMLAttribute(contentCs, "AllowContentChildTool")}\"");
                                sb.Append($" AllowContentTracking=\"{GetRSXMLAttribute(contentCs, "AllowContentTracking")}\"");
                                sb.Append($" AllowDelete=\"{GetRSXMLAttribute(contentCs, "AllowDelete")}\"");
                                sb.Append($" AllowMetaContent=\"{GetRSXMLAttribute(contentCs, "AllowMetaContent")}\"");
                                sb.Append($" AllowTopicRules=\"{GetRSXMLAttribute(contentCs, "AllowTopicRules")}\"");
                                //
                                int key = contentCs.GetInteger("NavTypeId");
                                string value = ContentController.getLookupListText(key, "Other,Report,Setting,Tool,Comm,Design,Content,System");
                                sb.Append($" NavTypeId=\"{value}\"");
                                sb.Append($" AddonCategoryId=\"{GetRSXMLAttribute(contentCs, "AddonCategoryId")}\"");
                                //
                                //sb.Append($" AllowWorkflowAuthoring=\"{GetRSXMLAttribute(contentCs, "AllowWorkflowAuthoring")}\"");
                                // 
                                AuthoringTableID = (contentCs.GetInteger("AuthoringTableID"));
                                TableName = "";
                                DataSourceName = "";
                                if (tables.ContainsKey(AuthoringTableID)) {
                                    TableName = tables[AuthoringTableID].tableName;
                                    DataSourceName = tables[AuthoringTableID].dataSourceName;
                                }
                                if (DataSourceName == "")
                                    DataSourceName = "Default";
                                if (Strings.UCase(TableName) == "CCMENUENTRIES")
                                    FoundMenuTable = true;
                                sb.Append($" AuthoringDataSourceName=\"{EncodeXMLattribute(DataSourceName)}\"");
                                sb.Append($" AuthoringTableName=\"{EncodeXMLattribute(TableName)}\"");
                                // 
                                ContentTableID = (contentCs.GetInteger("ContentTableID"));
                                if (ContentTableID != AuthoringTableID) {
                                    if (ContentTableID != 0) {
                                        TableName = "";
                                        DataSourceName = "";
                                        if (tables.ContainsKey(ContentTableID)) {
                                            TableName = tables[ContentTableID].tableName;
                                            DataSourceName = tables[ContentTableID].dataSourceName;
                                            if (DataSourceName == "")
                                                DataSourceName = "Default";
                                        }
                                    }
                                }
                                sb.Append($" ContentDataSourceName=\"{EncodeXMLattribute(DataSourceName)}\"");
                                sb.Append($" ContentTableName=\"{EncodeXMLattribute(TableName)}\"");
                                // 
                                DefaultSortMethod = "";
                                DefaultSortMethodID = (contentCs.GetInteger("DefaultSortMethodID"));
                                if (sorts.ContainsKey(DefaultSortMethodID)) { DefaultSortMethod = sorts[DefaultSortMethodID]; }
                                sb.Append($" DefaultSortMethod=\"{EncodeXMLattribute(DefaultSortMethod)}\"");
                                // 
                                sb.Append($" DeveloperOnly=\"{GetRSXMLAttribute(contentCs, "DeveloperOnly")}\"");
                                sb.Append($" DropDownFieldList=\"{GetRSXMLAttribute(contentCs, "DropDownFieldList")}\"");
                                // 
                                EditorGroupName = "";
                                EditorGroupID = (contentCs.GetInteger("EditorGroupID"));
                                if ((groups.ContainsKey(EditorGroupID)))
                                    EditorGroupName = groups[EditorGroupID];
                                sb.Append($" EditorGroupName=\"{EncodeXMLattribute(EditorGroupName)}\"");
                                // 
                                ParentName = "";
                                ParentID = contentCs.GetInteger("ParentID");
                                if (contents.ContainsKey(ParentID))
                                    ParentName = contents[ParentID];
                                sb.Append($" Parent=\"{EncodeXMLattribute(ParentName)}\"");
                                // 
                                sb.Append($" IconLink=\"{GetRSXMLAttribute(contentCs, "IconLink")}\"");
                                sb.Append($" IconHeight=\"{GetRSXMLAttribute(contentCs, "IconHeight")}\"");
                                sb.Append($" IconWidth=\"{GetRSXMLAttribute(contentCs, "IconWidth")}\"");
                                sb.Append($" IconSprites=\"{GetRSXMLAttribute(contentCs, "IconSprites")}\"");
                            }
                            sb.Append($" Guid=\"{GetRSXMLAttribute(contentCs, "ccGuid")}\"");
                            sb.Append($" >");
                            // 
                            // create output
                            // 
                            sql = $"select {FieldSelectList} from ccfields f left join ccfieldhelp h on h.fieldid=f.id left join ccaggregatefunctions a on a.id=f.editorAddonId where (f.Type<>0)";
                            if (ContentID != 0) { sql += $"and(f.contentid={ContentID})"; }
                            if (!IncludeBaseFields) { sql += " and ((f.IsBaseField is null)or(f.IsBaseField=0))"; }
                            sql += " order by f.contentid,f.editTab,f.editSortPriority,f.id";
                            using (CPCSBaseClass fieldsCs = cp.CSNew()) {
                                if (fieldsCs.OpenSQL(sql)) {
                                    fieldId = 0;
                                    do {
                                        lastFieldID = fieldId;
                                        fieldId = fieldsCs.GetInteger("ID");
                                        fieldName = fieldsCs.GetText("Name");
                                        fieldContentID = fieldsCs.GetInteger("contentid");
                                        if (fieldContentID > ContentID)
                                            break;
                                        else if ((fieldContentID == ContentID) && (fieldId != lastFieldID)) {
                                            if (IncludeBaseFields || (Strings.InStr(1, ",id,ContentCategoryID,dateadded,createdby,modifiedby,EditBlank,EditArchive,EditSourceID,ContentControlID,CreateKey,ModifiedDate,ccguid,", "," + fieldName + ",", CompareMethod.Text) == 0)) {
                                                int memberSelectGroupId = fieldsCs.GetInteger("MemberSelectGroupID");
                                                string memberSelectGroup = memberSelectGroupId <= 0 ? "" : cp.Group.GetName(memberSelectGroupId.ToString());
                                                sb.Append(System.Environment.NewLine + "\t\t<Field");
                                                fieldType = csv_GetFieldDescriptorByType(fieldsCs.GetInteger("Type"));
                                                sb.Append($" Name=\"{fieldName}\"");
                                                sb.Append($" Caption=\"{fieldsCs.GetText("Caption")}\"");
                                                sb.Append($" EditTab=\"{fieldsCs.GetText("EditTab")}\"");
                                                sb.Append($" EditGroup=\"{fieldsCs.GetText("EditGroup")}\"");
                                                sb.Append($" FieldType=\"{fieldType}\"");
                                                sb.Append($" Authorable=\"{getTrueFalse( fieldsCs.GetBoolean("Authorable"))}\"");
                                                sb.Append($" EditSortPriority=\"{fieldsCs.GetText("EditSortPriority")}\"");
                                                sb.Append($" DefaultValue=\"{fieldsCs.GetText("DefaultValue")}\"");
                                                sb.Append($" Active=\"{getTrueFalse( fieldsCs.GetBoolean("Active"))}\"");
                                                sb.Append($" AdminOnly=\"{getTrueFalse( fieldsCs.GetBoolean("AdminOnly"))}\"");
                                                sb.Append($" DeveloperOnly=\"{getTrueFalse( fieldsCs.GetBoolean("DeveloperOnly"))}\"");
                                                sb.Append($" HTMLContent=\"{getTrueFalse( fieldsCs.GetBoolean("HTMLContent"))}\"");
                                                sb.Append($" Required=\"{getTrueFalse( fieldsCs.GetBoolean("Required"))}\"");
                                                sb.Append($" TextBuffered=\"{getTrueFalse( fieldsCs.GetBoolean("TextBuffered"))}\"");
                                                sb.Append($" UniqueName=\"{getTrueFalse( fieldsCs.GetBoolean("UniqueName"))}\"");
                                                sb.Append($" MemberSelectGroup=\"{memberSelectGroup}\"");
                                                sb.Append($" Scramble=\"{getTrueFalse( fieldsCs.GetBoolean("Scramble"))}\"");
                                                sb.Append($" LookupList=\"{fieldsCs.GetText("LookupList")}\"");
                                                sb.Append($" NotEditable=\"{getTrueFalse( fieldsCs.GetBoolean("NotEditable"))}\"");
                                                sb.Append($" Password=\"{getTrueFalse( fieldsCs.GetBoolean("Password"))}\"");
                                                sb.Append($" ReadOnly=\"{getTrueFalse( fieldsCs.GetBoolean("ReadOnly"))}\"");
                                                sb.Append($" ManyToManyRulePrimaryField=\"{fieldsCs.GetText("ManyToManyRulePrimaryField")}\"");
                                                sb.Append($" ManyToManyRuleSecondaryField=\"{fieldsCs.GetText("ManyToManyRuleSecondaryField")}\"");
                                                sb.Append($" IndexColumn=\"{fieldsCs.GetText("IndexColumn")}\"");
                                                sb.Append($" IndexSortDirection=\"{fieldsCs.GetText("IndexSortDirection")}\"");
                                                sb.Append($" IndexSortOrder=\"{fieldsCs.GetText("IndexSortPriority")}\"");
                                                sb.Append($" IndexWidth=\"{fieldsCs.GetText("IndexWidth")}\"");
                                                sb.Append($" RedirectID=\"{fieldsCs.GetText("RedirectID")}\"");
                                                sb.Append($" RedirectPath=\"{fieldsCs.GetText("RedirectPath")}\"");
                                                sb.Append($" IsBaseField=\"{getTrueFalse( fieldsCs.GetBoolean("IsBaseField"))}\"");
                                                sb.Append($" EditorAddonId=\"{fieldsCs.GetText("editorAddonGuid")}\"");
                                                // 
                                                RecordName = "";
                                                RecordID = fieldsCs.GetInteger("LookupContentID");
                                                if ((contents.ContainsKey(RecordID)))
                                                    RecordName = contents[RecordID];
                                                sb.Append($" LookupContent=\"{System.Net.WebUtility.HtmlEncode(RecordName)}\"");
                                                // 
                                                RecordName = "";
                                                RecordID = fieldsCs.GetInteger("RedirectContentID");
                                                if ((contents.ContainsKey(RecordID)))
                                                    RecordName = contents[RecordID];
                                                sb.Append($" RedirectContent=\"{System.Net.WebUtility.HtmlEncode(RecordName)}\"");
                                                // 
                                                RecordName = "";
                                                RecordID = fieldsCs.GetInteger("ManyToManyContentID");
                                                if ((contents.ContainsKey(RecordID)))
                                                    RecordName = contents[RecordID];
                                                sb.Append($" ManyToManyContent=\"{System.Net.WebUtility.HtmlEncode(RecordName)}\"");
                                                // 
                                                RecordName = "";
                                                RecordID = fieldsCs.GetInteger("ManyToManyRuleContentID");
                                                if ((contents.ContainsKey(RecordID)))
                                                    RecordName = contents[RecordID];
                                                sb.Append($" ManyToManyRuleContent=\"{System.Net.WebUtility.HtmlEncode(RecordName)}\"");
                                                // 
                                                sb.Append($" >");
                                                // 
                                                HelpCnt = 0;
                                                HelpDefault = fieldsCs.GetText("helpcustom");
                                                if (string.IsNullOrEmpty(HelpDefault)) {
                                                    HelpDefault = fieldsCs.GetText("helpdefault");
                                                }
                                                    if (!string.IsNullOrEmpty(HelpDefault)) {
                                                        sb.Append(System.Environment.NewLine + $"\t\t\t<HelpDefault>{EncodeCData(HelpDefault)}</HelpDefault>");
                                                    HelpCnt += 1;
                                                }
                                                if (HelpCnt > 0) {
                                                    sb.Append(System.Environment.NewLine + "\t\t");
                                                }
                                                sb.Append($"</Field>");
                                            }
                                            FieldCnt += 1;
                                        }
                                        fieldsCs.GoNext();
                                    }
                                    while (fieldsCs.OK());
                                }
                                fieldsCs.Close();
                            }
                            if (FieldCnt > 0) {
                                sb.Append(Environment.NewLine + "\t");
                            }
                            sb.Append("</CDef>");
                        }
                        contentCs.Close();
                    }
                    if (string.IsNullOrEmpty(ContentName)) {
                        // 
                        // Add other areas of the CDef file
                        // 
                        sb.Append(GetXMLContentDefinition_SQLIndexes());
                        if (FoundMenuTable) {
                            sb.Append(GetXMLContentDefinition_AdminMenus());
                        }
                    }
                    const string ApplicationCollectionGuid = "{C58A76E2-248B-4DE8-BF9C-849A960F79C6}";
                    const string CollectionFileRootNode = "collection";
                    return "<" + CollectionFileRootNode + $" name=\"Application\" guid=\"{ApplicationCollectionGuid}\">{sb}{System.Environment.NewLine}</{CollectionFileRootNode}>";
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetXMLContentDefinition3");
                return string.Empty;
            }
        }
        // 
        // ========================================================================
        // ----- Save the admin menus to CDef AdminMenu tags
        // ========================================================================
        // 
        private string GetXMLContentDefinition_SQLIndexes() {
            try {
                string DataSourceName;
                string TableName;
                // 
                string IndexFields = "";
                string IndexList = "";
                string IndexName;
                string[] ListRows;
                string[] ListRowSplit;
                string SQL;
                CPCSBaseClass cs = cp.CSNew();
                int Ptr;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                // 
                SQL = "select D.name as DataSourceName,T.name as TableName"
                    + " from cctables T left join ccDataSources d on D.ID=T.DataSourceID"
                    + " where t.active<>0";
                if (cs.OpenSQL(SQL)) {
                    do {
                        DataSourceName = cs.GetText("DataSourceName");
                        TableName = cs.GetText("TableName");
                        // 
                        // need a solution for this
                        // 
                        // IndexList = cpCore.app.csv_GetSQLIndexList(DataSourceName, TableName)
                        // 
                        if (IndexList != "") {
                            ListRows = Strings.Split(IndexList, System.Environment.NewLine);
                            IndexName = "";
                            for (Ptr = 0; Ptr <= Information.UBound(ListRows) + 1; Ptr++) {
                                if (Ptr <= Information.UBound(ListRows))
                                    // 
                                    // ListRowSplit has the indexname and field for this index
                                    // 
                                    ListRowSplit = Strings.Split(ListRows[Ptr], ",");
                                else
                                    // 
                                    // one past the last row, ListRowSplit gets a dummy entry to force the output of the last line
                                    // 
                                    ListRowSplit = Strings.Split("-,-", ",");
                                if (Information.UBound(ListRowSplit) > 0) {
                                    if (ListRowSplit[0] != "") {
                                        if (IndexName == "") {
                                            // 
                                            // first line of the first index description
                                            // 
                                            IndexName = ListRowSplit[0];
                                            IndexFields = ListRowSplit[1];
                                        } else if (IndexName == ListRowSplit[0])
                                            // 
                                            // next line of the index description
                                            // 
                                            IndexFields = IndexFields + "," + ListRowSplit[1];
                                        else {
                                            // 
                                            // first line of a new index description
                                            // save previous line
                                            // 
                                            if (IndexName != "" & IndexFields != "") {
                                                sb.Append("<SQLIndex");
                                                sb.Append(" Indexname=\"" + EncodeXMLattribute(IndexName) + "\"");
                                                sb.Append(" DataSourceName=\"" + EncodeXMLattribute(DataSourceName) + "\"");
                                                sb.Append(" TableName=\"" + EncodeXMLattribute(TableName) + "\"");
                                                sb.Append(" FieldNameList=\"" + EncodeXMLattribute(IndexFields) + "\"");
                                                sb.Append("></SQLIndex>" + System.Environment.NewLine);
                                            }
                                            // 
                                            IndexName = ListRowSplit[0];
                                            IndexFields = ListRowSplit[1];
                                        }
                                    }
                                }
                            }
                        }

                        cs.GoNext();
                    }
                    while (cs.OK());
                }
                cs.Close();
                return sb.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetXMLContentDefinition3");
                return string.Empty;
            }
        }
        // 
        // ========================================================================
        // ----- Save the admin menus to CDef AdminMenu tags
        // ========================================================================
        // 
        private string GetXMLContentDefinition_AdminMenus() {
            return GetXMLContentDefinition_AdminMenus_MenuEntries()
                + GetXMLContentDefinition_AdminMenus_NavigatorEntries();
        }
        // 
        // ========================================================================
        // ----- Save the admin menus to CDef AdminMenu tags
        // ========================================================================
        // 
        private string GetXMLContentDefinition_AdminMenus_NavigatorEntries() {
            try {
                int NavIconType;
                string NavIconTitle;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                CPCSBaseClass dt = cp.CSNew();
                string menuNameSpace;
                string RecordName;
                int ParentID;
                int MenuContentID;
                string[] SplitArray;
                int SplitIndex;
                // 
                // ****************************** if cdef not loaded, this fails
                // 
                MenuContentID = cp.Content.GetRecordID("Content", "Navigator Entries");
                if (dt.OpenSQL("select * from ccMenuEntries where (contentcontrolid=" + MenuContentID + ")and(name<>'')")) {
                    NavIconType = 0;
                    NavIconTitle = "";
                    do {
                        RecordName = (dt.GetText("Name"));
                        ParentID = (dt.GetInteger("ParentID"));
                        menuNameSpace = getMenuNameSpace(ParentID, "");
                        sb.Append("<NavigatorEntry Name=\"" + EncodeXMLattribute(RecordName) + "\"");
                        sb.Append(" NameSpace=\"" + menuNameSpace + "\"");
                        sb.Append(" LinkPage=\"" + GetRSXMLAttribute(dt, "LinkPage") + "\"");
                        sb.Append(" ContentName=\"" + GetRSXMLLookupAttribute(dt, "ContentID", "ccContent") + "\"");
                        sb.Append(" AdminOnly=\"" + GetRSXMLAttribute(dt, "AdminOnly") + "\"");
                        sb.Append(" DeveloperOnly=\"" + GetRSXMLAttribute(dt, "DeveloperOnly") + "\"");
                        sb.Append(" NewWindow=\"" + GetRSXMLAttribute(dt, "NewWindow") + "\"");
                        sb.Append(" Active=\"" + GetRSXMLAttribute(dt, "Active") + "\"");
                        sb.Append(" AddonName=\"" + GetRSXMLLookupAttribute(dt, "AddonID", "ccAggregateFunctions") + "\"");
                        sb.Append(" SortOrder=\"" + GetRSXMLAttribute(dt, "SortOrder") + "\"");
                        NavIconType = cp.Utils.EncodeInteger(GetRSXMLAttribute(dt, "NavIconType"));
                        NavIconTitle = GetRSXMLAttribute(dt, "NavIconTitle");
                        sb.Append(" NavIconTitle=\"" + NavIconTitle + "\"");
                        SplitArray = Strings.Split(navIconTypeList + ",help", ",");
                        SplitIndex = NavIconType - 1;
                        if ((SplitIndex >= 0) & (SplitIndex <= Information.UBound(SplitArray))) { sb.Append(" NavIconType=\"" + SplitArray[SplitIndex] + "\""); }
                        sb.Append(" guid=\"" + GetRSXMLAttribute(dt, "ccGuid") + "\"");
                        // 
                        sb.Append("></NavigatorEntry>" + System.Environment.NewLine);
                        dt.GoNext();
                    }
                    while (dt.OK());
                }
                return sb.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetXMLContentDefinition3");
                return string.Empty;
            }
        }
        // 
        // ========================================================================
        // ----- Save the admin menus to CDef AdminMenu tags
        // ========================================================================
        // 
        private string GetXMLContentDefinition_AdminMenus_MenuEntries() {
            try {
                StringBuilder sb = new StringBuilder();
                CPCSBaseClass dr = cp.CSNew();
                string RecordName;
                int MenuContentID;
                // 
                MenuContentID = cp.Content.GetRecordID("Content", "Menu Entries");
                if ((dr.OpenSQL("select * from ccMenuEntries where (contentcontrolid=" + MenuContentID + ")and(name<>'')"))) {
                    do {
                        RecordName = (dr.GetText("Name"));
                        sb.Append("<MenuEntry Name=\"" + EncodeXMLattribute(RecordName) + "\"");
                        sb.Append(" ParentName=\"" + GetRSXMLLookupAttribute(dr, "ParentID", "ccMenuEntries") + "\"");
                        sb.Append(" LinkPage=\"" + GetRSXMLAttribute(dr, "LinkPage") + "\"");
                        sb.Append(" ContentName=\"" + GetRSXMLLookupAttribute(dr, "ContentID", "ccContent") + "\"");
                        sb.Append(" AdminOnly=\"" + GetRSXMLAttribute(dr, "AdminOnly") + "\"");
                        sb.Append(" DeveloperOnly=\"" + GetRSXMLAttribute(dr, "DeveloperOnly") + "\"");
                        sb.Append(" NewWindow=\"" + GetRSXMLAttribute(dr, "NewWindow") + "\"");
                        sb.Append(" Active=\"" + GetRSXMLAttribute(dr, "Active") + "\"");
                        if (true)
                            sb.Append(" AddonName=\"" + GetRSXMLLookupAttribute(dr, "AddonID", "ccAggregateFunctions") + "\"");
                        sb.Append("/>" + System.Environment.NewLine);
                        dr.GoNext();
                    }
                    while (dr.OK());
                }
                dr.Close();
                return sb.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetXMLContentDefinition3");
                return string.Empty;

            }
        }
        // 
        // ========================================================================
        // 
        // ========================================================================
        // 
        private string GetXMLContentDefinition_AggregateFunctions() {
            try {
                CPCSBaseClass rs = cp.CSNew();
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                // 
                if ((rs.OpenSQL("select * from ccAggregateFunctions"))) {
                    do {
                        sb.Append("<Addon Name=\"" + GetRSXMLAttribute(rs, "Name") + "\"");
                        sb.Append(" Link=\"" + GetRSXMLAttribute(rs, "Link") + "\"");
                        sb.Append(" ArgumentList=\"" + GetRSXMLAttribute(rs, "ArgumentList") + "\"");
                        sb.Append(" SortOrder=\"" + GetRSXMLAttribute(rs, "SortOrder") + "\"");
                        sb.Append(" >");
                        sb.Append(GetRSXMLAttribute(rs, "Copy"));
                        sb.Append("</Addon>" + System.Environment.NewLine);
                        rs.GoNext();
                    }
                    while (rs.OK());
                }
                return sb.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetXMLContentDefinition3");
                return string.Empty;

            }
        }
        // 
        // 
        // 
        private string EncodeXMLattribute(string Source) {
            string result = System.Net.WebUtility.HtmlEncode(Source);
            result = Strings.Replace(result, System.Environment.NewLine, " ");
            result = Strings.Replace(result, "\n", "");
            result = Strings.Replace(result, "\r", "");
            return result;
        }
        // 
        // 
        // 
        private string GetTableRecordName(string TableName, int RecordID) {
            try {
                using (CPCSBaseClass dt = cp.CSNew()) {
                    if (RecordID != 0 & TableName != "") {
                        if (dt.OpenSQL("select Name from " + TableName + " where ID=" + RecordID))
                            return dt.GetText("name");
                        dt.Close();
                    }
                }
                return string.Empty;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetXMLContentDefinition3");
                return string.Empty;
            }
        }
        // 
        // 
        // 
        private string GetRSXMLAttribute(CPCSBaseClass dr, string FieldName) {
            return EncodeXMLattribute((dr.GetText(FieldName)));
        }
        // 
        // 
        // 
        private string GetRSXMLLookupAttribute(CPCSBaseClass dr, string FieldName, string TableName) {
            return EncodeXMLattribute(GetTableRecordName(TableName, (dr.GetInteger(FieldName))));
        }
        // 
        // 
        // 
        private string getMenuNameSpace(int RecordID, string UsedIDString) {
            string result = "";
            try {
                CPCSBaseClass rs = cp.CSNew();
                int ParentID;
                string RecordName = "";
                string ParentSpace = "";
                // 
                if (RecordID != 0) {
                    if (Strings.InStr(1, "," + UsedIDString + ",", "," + RecordID + ",", CompareMethod.Text) != 0) {
                        cp.Site.ErrorReport("Circular reference found in UsedIDString [" + UsedIDString + "] getting ccMenuEntries namespace for recordid [" + RecordID + "]");
                        result = "";
                    } else {
                        UsedIDString = UsedIDString + "," + RecordID;
                        ParentID = 0;
                        if (RecordID != 0) {
                            if ((rs.OpenSQL("select Name,ParentID from ccMenuEntries where ID=" + RecordID))) {
                                ParentID = rs.GetInteger("parentid");
                                RecordName = rs.GetText("name");
                            }
                            rs.Close();
                        }
                        if (RecordName != "") {
                            if (ParentID == RecordID) {
                                // 
                                // circular reference
                                // 
                                cp.Site.ErrorReport("Circular reference found (ParentID=RecordID) getting ccMenuEntries namespace for recordid [" + RecordID + "]");
                                result = "";
                            } else {
                                if (ParentID != 0)
                                    // 
                                    // get next parent
                                    // 
                                    ParentSpace = getMenuNameSpace(ParentID, UsedIDString);
                                if (ParentSpace != "")
                                    result = ParentSpace + "." + RecordName;
                                else
                                    result = RecordName;
                            }
                        } else
                            result = "";
                    }
                }
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetXMLContentDefinition3");
                return string.Empty;
            }
        }
        // 
        // ========================================================================
        // ----- Get FieldDescritor from FieldType
        // ========================================================================
        // 
        public string csv_GetFieldDescriptorByType(int fieldType) {
            string result = "";
            result = "";
            try {
                switch (fieldType) {
                    case (int)CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                            result = fieldDescriptorBoolean;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.Currency: {
                            result = fieldDescriptorCurrency;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.Date: {
                            result = fieldDescriptorDate;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.File: {
                            result = fieldDescriptorFile;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.Float: {
                            result = fieldDescriptorFloat;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                            result = fieldDescriptorImage;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.Link: {
                            result = fieldDescriptorLink;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.ResourceLink: {
                            result = fieldDescriptorResourceLink;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.Integer: {
                            result = fieldDescriptorInteger;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.LongText: {
                            result = fieldDescriptorLongText;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                            result = fieldDescriptorLookup;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.MemberSelect: {
                            result = fieldDescriptorMemberSelect;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.Redirect: {
                            result = fieldDescriptorRedirect;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                            result = fieldDescriptorManyToMany;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.FileText: {
                            result = fieldDescriptorTextFile;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.FileCSS: {
                            result = fieldDescriptorCSSFile;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                            result = fieldDescriptorXMLFile;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.FileJavaScript: {
                            result = fieldDescriptorJavaScriptFile;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.Text: {
                            result = fieldDescriptorText;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.HTML: {
                            result = fieldDescriptorHTML;
                            break;
                        }

                    case (int)CPContentBaseClass.FieldTypeIdEnum.FileHTML: {
                            result = fieldDescriptorHTMLFile;
                            break;
                        }

                    default: {
                            if (fieldType == (int)CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement)
                                result = "AutoIncrement";
                            else if (fieldType == (int)CPContentBaseClass.FieldTypeIdEnum.MemberSelect)
                                result = "MemberSelect";
                            else
                                // 
                                // If field type is ignored, call it a text field
                                // 
                                result = fieldDescriptorText;
                            break;
                        }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetXMLContentDefinition3");
            }
            return result;
        }
        // 
        // ====================================================================================================
        private string EncodeCData(string Source) {
            if (string.IsNullOrWhiteSpace(Source)) { return string.Empty; }
            return "<![CDATA[" + Strings.Replace(Source, "]]>", "]]]]><![CDATA[>") + "]]>";
        }
        //
        //====================================================================================================
        /// <summary>
        ///  Get an XML nodes attribute based on its name
        /// </summary>
        public static string getXMLAttribute(CoreController core, XmlNode Node, string Name, string DefaultIfNotFound) {
            try {
                XmlNode ResultNode = Node.Attributes.GetNamedItem(Name);
                if (ResultNode != null) { return ResultNode.Value; }
                string UcaseName = Name.ToUpperInvariant();
                foreach (XmlAttribute NodeAttribute in Node.Attributes) {
                    if (NodeAttribute.Name.ToUpperInvariant() == UcaseName) { return NodeAttribute.Value; }
                }
                return DefaultIfNotFound;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// get attribute
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Found"></param>
        /// <param name="Node"></param>
        /// <param name="Name"></param>
        /// <param name="DefaultIfNotFound"></param>
        /// <returns></returns>
        public static bool getXMLAttributeBoolean(CoreController core, XmlNode Node, string Name, bool DefaultIfNotFound) {
            return encodeBoolean(getXMLAttribute(core, Node, Name, encodeText(DefaultIfNotFound)));
        }
        //
        //====================================================================================================
        /// <summary>
        /// get attribute, return 0 if not found
        /// </summary>
        /// <param name="core"></param>
        /// <param name="Found"></param>
        /// <param name="Node"></param>
        /// <param name="Name"></param>
        /// <param name="DefaultIfNotFound"></param>
        /// <returns></returns>
        public static int getXMLAttributeInteger(CoreController core, XmlNode Node, string Name, int DefaultIfNotFound) {
            return encodeInteger(getXMLAttribute(core, Node, Name, DefaultIfNotFound.ToString()));
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
