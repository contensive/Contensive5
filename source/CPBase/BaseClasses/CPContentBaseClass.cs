
using System;
using Contensive.CPBase.BaseModels;

namespace Contensive.BaseClasses {
    public abstract class CPContentBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Track the latest modified date
        /// </summary>
        public abstract LastestDateTrackerBaseModel LatestContentModifiedDate { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Field types. This enum replaces FileTypeIdEnug.
        /// </summary>
        public enum FieldTypeIdEnum  {
            /// <summary>
            /// (int32) Positive and negative integer 32-bit values, -2,147,483,648 to 2,147,483,647
            /// </summary>
            Integer = 1,
            /// <summary>
            /// (string) Literal strings stored up to 255 characters.
            /// </summary>
            Text = 2,
            /// <summary>
            /// (string) Literal strings stored up to 65535 characters.
            /// </summary>
            LongText = 3,
            /// <summary>
            /// (bool) true or false. Stored in Db as integer, 0=false, 1=true. Tested as null|0 = false oterwise true
            /// </summary>
            Boolean = 4,
            /// <summary>
            /// (date)
            /// </summary>
            Date = 5,
            /// <summary>
            /// Uploaded file of any type. Path is created specifically for the path it is created in and stored in the db as an varchar(255)
            /// </summary>
            File = 6,
            /// <summary>
            /// (int) A foreign key to the primary key (id) of another table
            /// </summary>
            Lookup = 7,
            /// <summary>
            /// no value stored. This field holds metadata that creates a forward link on the editing page for many-to-many fields
            /// </summary>
            Redirect = 8,
            /// <summary>
            /// (double) usd currency
            /// </summary>
            Currency = 9,
            /// <summary>
            /// (string, varchar(255)), stores the path and filename of a content file holding text
            /// </summary>
            FileText = 10,
            /// <summary>
            /// Uploaded image file. Path is created specifically for the path it is created in and stored in the db as an varchar(255)
            /// </summary>
            FileImage = 11,
            /// <summary>
            /// (double)
            /// </summary>
            Float = 12,
            /// <summary>
            /// (integer). Creates automatically incrementing integer
            /// </summary>
            AutoIdIncrement = 13,
            /// <summary>
            /// no value stored. This field hold metadata that creates a checkbox list for a many-to-many relationship
            /// </summary>
            ManyToMany = 14,
            /// <summary>
            /// (int) Holds a lookup id for the ccmembers table limited by those in a group definied in the metadata
            /// </summary>
            MemberSelect = 15,
            /// <summary>
            /// (string, varchar(255)), holds a path and filename to a css content file
            /// </summary>
            FileCSS = 16,
            /// <summary>
            /// (string, varchar(255)), holds a path and filename to an xml content file
            /// </summary>
            FileXML = 17,
            /// <summary>
            /// (string, varchar(255)), holds a path and filename to a javascript content file
            /// </summary>
            FileJavascript = 18,
            /// <summary>
            /// 
            /// </summary>
            Link = 19,
            /// <summary>
            /// (string, varchar(255)), holds a url
            /// </summary>
            ResourceLink = 20,
            /// <summary>
            /// (string, varchar(max)), holds text content that represents html
            /// </summary>
            HTML = 21,
            /// <summary>
            /// (string, varchar(255)), holds a path and filename to a content file with html content
            /// </summary>
            FileHTML = 22,
            /// <summary>
            /// (string, varchar(max)), holds text content that represents html and edited as html code
            /// </summary>
            HTMLCode = 23,
            /// <summary>
            /// (string, varchar(255)), holds a path and filename to a content file with html content and edited as html code
            /// </summary>
            FileHTMLCode = 24
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get the string from the 'Copy Content' record based on it's name. If the record does not exist it is created with the default value provided.
        /// </summary>
        /// <param name="CopyName"></param>
        /// <param name="DefaultContent"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetCopy(string CopyName, string DefaultContent);
        /// <summary>
        /// Get the string from the 'Copy Content' record based on it's name. If the record does not exist it is created with the default value provided.
        /// </summary>
        /// <param name="CopyName"></param>
        /// <returns></returns>
        public abstract string GetCopy(string CopyName);
        //
        //====================================================================================================
        /// <summary>
        /// Get the string from the 'Copy Content' record based on it's name. If the record does not exist it is created with the default value provided.
        /// </summary>
        /// <param name="CopyName"></param>
        /// <param name="DefaultContent"></param>
        /// <param name="personalizationPeopleId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetCopy(string CopyName, string DefaultContent, int personalizationPeopleId);
        //
        //====================================================================================================
        /// <summary>
        /// Set a string in a 'Copy Content' record. The record will be created or modified.
        /// </summary>
        /// <param name="CopyName"></param>
        /// <param name="Content"></param>
        /// <remarks></remarks>
        public abstract void SetCopy(string CopyName, string Content);
        //
        //====================================================================================================
        /// <summary>
        /// Get an icon linked to the administration site which adds a new record to the content.
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="presetNameValueList">A comma delimited list of name=value pairs. Each name is a field name and the value is used to prepopulate the new record.</param>
        /// <param name="allowPaste">If true and the content supports cut-paste from the public site, the returned string will include a cut icon.</param>
        /// <param name="isEditing">If false, this call returns nothing. Set it true if IsEdiing( contentname ) is true.</param>
        /// <param name="includechildContent">If true, return a crlf delimited list of add tags, including add links for child content</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetAddLink(string contentName, string presetNameValueList, bool allowPaste, bool isEditing, bool includechildContent);
        /// <summary>
        /// Get an icon linked to the administration site which adds a new record to the content.
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="presetNameValueList">A comma delimited list of name=value pairs. Each name is a field name and the value is used to prepopulate the new record.</param>
        /// <param name="allowPaste">If true and the content supports cut-paste from the public site, the returned string will include a cut icon.</param>
        /// <param name="isEditing">If false, this call returns nothing. Set it true if IsEdiing( contentname ) is true.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetAddLink(string contentName, string presetNameValueList, bool allowPaste, bool isEditing);
        /// <summary>
        /// Get an icon linked to the administration site which adds a new record to the content.
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="presetNameValueList"></param>
        /// <returns></returns>
        public abstract string GetAddLink(string contentName, string presetNameValueList);
        /// <summary>
        /// Get an icon linked to the administration site which adds a new record to the content.
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public abstract string GetAddLink(string contentName);
        /// <summary>
        /// Get an icon linked to the administration site which adds a new record to the content.
        /// </summary>
        /// <param name="contentId"></param>
        /// <param name="presetNameValueList"></param>
        /// <param name="includeChildContent">If true, return a crlf delimited list of add tags, including add links for child content</param>
        /// <returns></returns>
        public abstract string GetAddLink(int contentId, string presetNameValueList, bool includeChildContent);
        /// <summary>
        /// Get an icon linked to the administration site which adds a new record to the content.
        /// </summary>
        /// <param name="contentId"></param>
        /// <param name="presetNameValueList"></param>
        /// <returns></returns>
        public abstract string GetAddLink(int contentId, string presetNameValueList);
        /// <summary>
        /// Get an icon linked to the administration site which adds a new record to the content.
        /// </summary>
        /// <param name="contentId"></param>
        /// <returns></returns>
        public abstract string GetAddLink(int contentId);
        //
        //====================================================================================================
        /// <summary>
        /// Returns an SQL compatible where-clause which includes all the contentcontentid values allowed for this content name.
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetContentControlCriteria(string contentName);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the content id given its name
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract int GetID(string ContentName);
        //
        //====================================================================================================
        /// <summary>
        /// Return the contentName from its id
        /// </summary>
        /// <param name="contentId"></param>
        /// <returns></returns>
        public abstract string GetName(int contentId);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the datasource name of the content given.
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetDataSource(string ContentName);
        //
        //====================================================================================================
        /// <summary>
        /// Get the system edit link
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="allowCut">If true and the content allows cut and paste, and cut icon will be included in the return string.</param>
        /// <param name="recordLabel">Used as a caption for the label</param>
        /// <param name="IsEditing">If false, this call returns nothing. Set it true if IsEdiing( contentname ) is true.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetEditLink(string contentName, string recordId, bool allowCut, string recordLabel, bool IsEditing);
        /// <summary>
        /// Get the system edit link
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public abstract string GetEditLink(string contentName, int recordId);
        /// <summary>
        /// Get the system edit link
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <returns></returns>
        public abstract string GetEditLink(string contentName, string recordGuid);
        /// <summary>
        /// Get the system edit link
        /// </summary>
        /// <param name="contentId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public abstract string GetEditLink(int contentId, int recordId);
        /// <summary>
        /// Get the system edit link
        /// </summary>
        /// <param name="contentId"></param>
        /// <param name="recordGuid"></param>
        /// <returns></returns>
        public abstract string GetEditLink(int contentId, string recordGuid);
        //
        //====================================================================================================
        /// <summary>
        /// wrap content in system editing region style.
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <returns></returns>
        public abstract string GetEditWrapper(string innerHtml);
        /// <summary>
        /// wrap content in system editing region style with an edit link.
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public abstract string GetEditWrapper(string innerHtml, string contentName, int recordId);
        /// <summary>
        /// wrap content in system editing region style with an edit link.
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <returns></returns>
        public abstract string GetEditWrapper(string innerHtml, string contentName, string recordGuid);
        /// <summary>
        /// wrap content in system editing region style with an edit link.
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="contentId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public abstract string GetEditWrapper(string innerHtml, int contentId, int recordId);
        /// <summary>
        /// wrap content in system editing region style with an edit link.
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="contentId"></param>
        /// <param name="recordGuid"></param>
        /// <returns></returns>
        public abstract string GetEditWrapper(string innerHtml, int contentId, string recordGuid);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the primary link alias for the record id and querystringsuffix. If no link alias exists, it defaultvalue is returned.
        /// </summary>
        /// <param name="PageID"></param>
        /// <param name="QueryStringSuffix">In the case where an add-on is on the page, there may be many unique documents possible from the one pageid. Each possible variation is determined by values in the querystring added by the cpcore.addon. These name=value pairs in Querystring format are used to identify additional link aliases.</param>
        /// <param name="DefaultLink">If no link alias is found, this value is returned.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetLinkAliasByPageID(int PageID, string QueryStringSuffix, string DefaultLink);
        //
        //====================================================================================================
        /// <summary>
        /// Return the appropriate link for a page.
        /// </summary>
        /// <param name="PageID"></param>
        /// <param name="QueryStringSuffix">If a link alias exists, this is used to lookup the correct alias. See GetLinkAliasByPageID for details. In other cases, this is added to the querystring.</param>
        /// <param name="AllowLinkAlias"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetPageLink(int PageID, string QueryStringSuffix, bool AllowLinkAlias);
        /// <summary>
        /// Return the appropriate link for a page.
        /// </summary>
        /// <param name="PageID"></param>
        /// <param name="QueryStringSuffix"></param>
        /// <returns></returns>
        public abstract string GetPageLink(int PageID, string QueryStringSuffix);
        /// <summary>
        /// Return the appropriate link for a page.
        /// </summary>
        /// <param name="PageID"></param>
        /// <returns></returns>
        public abstract string GetPageLink(int PageID);
        //
        //====================================================================================================
        /// <summary>
        /// Return a record's ID given it's name. If duplicates exist, the first one ordered by ID is returned.
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract int GetRecordID(string ContentName, string RecordName);
        //
        //====================================================================================================
        /// <summary>
        /// Return a records name given it's ID.
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordID"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetRecordName(string ContentName, int RecordID);
        //
        //====================================================================================================
        /// <summary>
        /// Get the table used for a content definition.
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetTable(string ContentName);
        //
        //====================================================================================================
        /// <summary>
        /// Used to test if a field exists in a content definition
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="FieldName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool IsField(string ContentName, string FieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Returns true if the record is currently being edited.
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordID"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool IsLocked(string ContentName, string RecordID);
        //
        //====================================================================================================
        /// <summary>
        /// Returns true if the childcontentid is a child of the parentcontentid
        /// </summary>
        /// <param name="ChildContentID"></param>
        /// <param name="ParentContentID"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool IsChildContent(string ChildContentID, string ParentContentID);
        //
        //====================================================================================================
        /// <summary>
        /// Inserts a record and returns the Id for the record
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
		public abstract int AddRecord(string ContentName);
        //
        //====================================================================================================
        /// <summary>
        /// Insert a record and set its name. REturn the id of the record.
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="recordName"></param>
        /// <returns></returns>
		public abstract int AddRecord(string ContentName, string recordName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a new content with sqlTablename and default fields on the default datasource. After the call, modify the content with the database model - Models.Db.ContentModel.create( cp, id )
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="sqlTableName"></param>
        /// <param name="dataSource"></param>
        /// <returns></returns>
		public abstract int AddContent(string ContentName, string sqlTableName, string dataSource);
        //
        //====================================================================================================
        /// <summary>
        /// Create a new content with sqlTablename and default fields. After the call, modify the content with the database model - Models.Db.ContentModel.create( cp, id )
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="sqlTableName"></param>
        /// <returns></returns>
        public abstract int AddContent(string ContentName, string sqlTableName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a new content with default fields. sqlTablename created from contentName. After the call, modify the content with the database model - Models.Db.ContentModel.create( cp, id )
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        public abstract int AddContent(string ContentName);
        //
        //====================================================================================================
        /// <summary>
        /// Create a new field in an existing content, return the fieldid
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="FieldName"></param>
        /// <param name="FieldType"></param>
        /// <returns></returns>
        public abstract int AddContentField(string ContentName, string FieldName, int FieldType);
        /// <summary>
        /// Create a new field in an existing content, return the fieldid
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="FieldName"></param>
        /// <param name="fieldTypeId"></param>
        /// <returns></returns>
        public abstract int AddContentField(string ContentName, string FieldName, FieldTypeIdEnum fieldTypeId);
        //
        //====================================================================================================
        /// <summary>
        /// Delete a content from the system, sqlTable is left intact. Use db.DeleteTable to drop the table
        /// </summary>
        /// <param name="ContentName"></param>
        /// <remarks></remarks>
        public abstract void DeleteContent(string ContentName);
        //
        //====================================================================================================
        /// <summary>
        /// Delete records based from a table based on a content name and SQL criteria.
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="SQLCriteria"></param>
        /// <remarks></remarks>
        public abstract void Delete(string ContentName, string SQLCriteria);
        //
        //====================================================================================================
        /// <summary>
        /// Returns a linked icon to the admin list page for the content
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetListLink(string ContentName);
        //
        //====================================================================================================
        /// <summary>
        /// Return the recordId in the ccTables table for this table
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
		public abstract int GetTableID(string tableName);
        //
        //====================================================================================================
        // deprecated
        //
        /// <summary>
        /// Deprecated, use CP.Layout.GetLayoutByName()
        /// </summary>
        /// <param name="layoutName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Deprecated, use CP.Layout.GetLayoutByName()", false)]
        public abstract string getLayout(string layoutName);
        //
        /// <summary>
        /// Deprecated, use CP.Layout.GetLayoutByName()
        /// </summary>
        /// <param name="layoutName"></param>
        /// <param name="defaultLayout"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Deprecated, use CP.Layout.GetLayoutByName()", false)] 
        public abstract string getLayout(string layoutName, string defaultLayout);
        //
        /// <summary>
        /// Returns the html layout field of a layout record
        /// </summary>
        /// <param name="layoutId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Deprecated, use CP.Layout.GetLayout()", false)]
        public abstract string GetLayout(int layoutId);
        //
        /// <summary>
        /// Deprecated, template link is not supported
        /// </summary>
        /// <param name="TemplateID"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, template link is not supported", false)]
        public abstract string GetTemplateLink(int TemplateID);
        //
        /// <summary>
        /// workflow editing is deprecated
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        [Obsolete("workflow editing is deprecated", false)]
        public abstract bool IsWorkflow(string ContentName);
        //
        /// <summary>
        /// workflow editing is deprecated
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordID"></param>
        [Obsolete("workflow editing is deprecated", false)]
        public abstract void PublishEdit(string ContentName, int RecordID);
        //
        /// <summary>
        /// workflow editing is deprecated
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordID"></param>
        [Obsolete("workflow editing is deprecated", false)]
        public abstract void SubmitEdit(string ContentName, int RecordID);
        //
        /// <summary>
        /// workflow editing is deprecated
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordId"></param>
        [Obsolete("workflow editing is deprecated", false)]
        public abstract void AbortEdit(string ContentName, int RecordId);
        //
        /// <summary>
        /// workflow editing is deprecated
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="RecordId"></param>
        [Obsolete("workflow editing is deprecated", false)]
        public abstract void ApproveEdit(string ContentName, int RecordId);
        //
        /// <summary>
        /// Deprecated, use AddRecord(string)
        /// </summary>
        /// <param name="ContentName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use AddRecord(string)", false)]
        public abstract int AddRecord(object ContentName);
        //
        /// <summary>
        /// Deprecated. Use models to access record fields
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated. Use models to access record fields", false)]
        public abstract string GetProperty(string ContentName, string PropertyName);
        //
        /// <summary>
        /// Deprecated, use GetFieldMeta and use the property
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="FieldName"></param>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use GetFieldMeta and use the property", false)]
        public abstract string GetFieldProperty(string ContentName, string FieldName, string PropertyName);
        //
        /// <summary>
        /// Deprecated, use method with FieldTypeIdEnum
        /// </summary>
        /// <param name="ContentName"></param>
        /// <param name="FieldName"></param>
        /// <param name="fileTypeEnum"></param>
        /// <returns></returns>
        [Obsolete("Deprecated, use method with FieldTypeIdEnum", false)]
        public abstract int AddContentField(string ContentName, string FieldName, fileTypeIdEnum fileTypeEnum);
        //
        public enum GoogleVisualizationStatusEnum {
            OK = 1,
            warning = 2,
            ErrorStatus = 3
        }
        //
        //====================================================================================================
        /// <summary>
        /// Obsolete. Use FieldTypeIdEnum
        /// </summary>
        [Obsolete("deprecated, Use FieldTypeIdEnum", false)]
        public enum fileTypeIdEnum {
            Integer = 1,
            Text = 2,
            LongText = 3,
            Boolean = 4,
            Date = 5,
            File = 6,
            Lookup = 7,
            Redirect = 8,
            Currency = 9,
            FileText = 10,
            FileImage = 11,
            Float = 12,
            AutoIdIncrement = 13,
            ManyToMany = 14,
            MemberSelect = 15,
            FileCSS = 16,
            FileXML = 17,
            FileJavascript = 18,
            Link = 19,
            ResourceLink = 20,
            HTML = 21,
            FileHTML = 22
        }
    }

}

