
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// CP.CS - The secondary interface to execute queries on a sql database. Use dbModels when possible (does not use cdef metadata). To run queries, use executeQuery, executeNonQuery and executeNonQueryAsync.
    /// </summary>
    /// <remarks></remarks>
    public abstract class CPCSBaseClass : IDisposable {
        // todo cs.openRecord -- very important as it will use the cdef model + dbmodels so it will be cached - cs.open() cannot be cached.
        // todo in collection file, in cdef for each text field, include a maxtextlength that will be used throughout to prevent db truncation
        //
        //====================================================================================================
        /// <summary>
        /// Insert a record, leaving the dataset open in this object. Call cs.close() to close the data
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool Insert(string contentName);
        //
        //====================================================================================================
        /// <summary>
        /// Opens a record set with the record specified by the recordId
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="selectFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool OpenRecord(string contentName, int recordId, string selectFieldList, bool activeOnly);
        /// <summary>
        /// Opens a record set with the record specified by the recordId
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="selectFieldList"></param>
        /// <returns></returns>
        public abstract bool OpenRecord(string contentName, int recordId, string selectFieldList);
        /// <summary>
        /// Opens a record set with the record specified by the recordId
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public abstract bool OpenRecord(string contentName, int recordId);
        //
        //====================================================================================================
        /// <summary>
        /// Opens a record set with the record specified by the recordGuid
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <param name="selectFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <returns></returns>
        public abstract bool OpenRecord(string contentName, string recordGuid, string selectFieldList, bool activeOnly);
        /// <summary>
        /// Opens a record set with the record specified by the recordGuid
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <param name="selectFieldList"></param>
        /// <returns></returns>
        public abstract bool OpenRecord(string contentName, string recordGuid, string selectFieldList);
        /// <summary>
        /// Opens a record set with the record specified by the recordGuid
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <returns></returns>
        public abstract bool OpenRecord(string contentName, string recordGuid);
        //
        //====================================================================================================
        /// <summary>
        /// Opens a record set with the records specified by the sqlCriteria
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="selectFieldList"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber">1-based page number</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly, string selectFieldList, int pageSize, int pageNumber);
        /// <summary>
        /// Opens a record set with the records specified by the sqlCriteria
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="selectFieldList"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public abstract bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly, string selectFieldList, int pageSize);
        /// <summary>
        /// Opens a record set with the records specified by the sqlCriteria
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="selectFieldList"></param>
        /// <returns></returns>
        public abstract bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly, string selectFieldList);
        /// <summary>
        /// Opens a record set with the records specified by the sqlCriteria
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <returns></returns>
        public abstract bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly);
        /// <summary>
        /// Opens a record set with the records specified by the sqlCriteria
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <returns></returns>
        public abstract bool Open(string contentName, string sqlCriteria, string sortFieldList);
        /// <summary>
        /// Opens a record set with the records specified by the sqlCriteria
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="sqlCriteria"></param>
        /// <returns></returns>
        public abstract bool Open(string contentName, string sqlCriteria);
        /// <summary>
        /// Opens a record set
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public abstract bool Open(string contentName);
        //
        //====================================================================================================
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber">1-based page number</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool OpenGroupUsers(string groupName, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize, int pageNumber);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(string groupName, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(string groupName, string sqlCriteria, string sortFieldList, bool activeOnly);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(string groupName, string sqlCriteria, string sortFieldList);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="sqlCriteria"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(string groupName, string sqlCriteria);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(string groupName);
        //
        //====================================================================================================
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber">1-based page number</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool OpenGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize, int pageNumber);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sortFieldList"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="sqlCriteria"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(List<string> groupList, string sqlCriteria);
        /// <summary>
        /// Opens a record set with user records that are in a Group
        /// </summary>
        /// <param name="groupList"></param>
        /// <returns></returns>
        public abstract bool OpenGroupUsers(List<string> groupList);
        //
        //====================================================================================================
        /// <summary>
        /// Opens a record set based on an sql statement. The result is paged based on the pageSize and pageNumber parameters. The pageNumber is 1-based.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourcename"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber">1-based page number</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool OpenSQL(string sql, string dataSourcename, int pageSize, int pageNumber);
        /// <summary>
        /// Opens a record set based on an sql statement
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourcename"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public abstract bool OpenSQL(string sql, string dataSourcename, int pageSize);
        /// <summary>
        /// Opens a record set based on an sql statement
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dataSourcename"></param>
        /// <returns></returns>
        public abstract bool OpenSQL(string sql, string dataSourcename);
        /// <summary>
        /// Opens a record set based on an sql statement
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public abstract bool OpenSQL(string sql);
        //
        //====================================================================================================
        /// <summary>
        ///  Closes an open record set
        /// </summary>
        /// <remarks></remarks>
        public abstract void Close();
        //
        //====================================================================================================
        /// <summary>
        /// Returns a form input element based on a content field definition
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="fieldName"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract object GetFormInput(string contentName, string fieldName, int height, int width, string htmlId);
        public abstract object GetFormInput(string contentName, string fieldName, int height, int width);
        public abstract object GetFormInput(string contentName, string fieldName, int height);
        public abstract object GetFormInput(string contentName, string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Deletes the current row
        /// </summary>
        /// <remarks></remarks>
        public abstract void Delete();
        //
        //====================================================================================================
        /// <summary>
        /// Returns true if the given field is valid for this record set
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool FieldOK(string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Move to the first record in the current record set
        /// </summary>
        /// <remarks></remarks>
        public abstract void GoFirst();
        //
        //====================================================================================================
        /// <summary>
        /// Returns an icon linked to the add function in the admin site for this content
        /// </summary>
        /// <param name="presetNameValueList"></param>
        /// <param name="allowPaste"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetAddLink(string presetNameValueList, bool allowPaste);
        public abstract string GetAddLink(string presetNameValueList);
        public abstract string GetAddLink();
        //
        //====================================================================================================
        /// <summary>
        /// Returns the field value cast as a boolean
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool GetBoolean(string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the field value cast as a date
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract DateTime GetDate(string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// get a system edit link
        /// </summary>
        /// <param name="allowCut"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetEditLink(bool allowCut);
        /// <summary>
        /// get a system edit link
        /// </summary>
        /// <returns></returns>
        public abstract string GetEditLink();
        //
        //====================================================================================================
        /// <summary>
        /// wrap content in system editing region style with an edit link
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetEditWrapper(string innerHtml);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the filename for the field, if a filename is related to the field type. Use this call to create the appropriate filename when a new file is added. The filename with the appropriate path is created or returned. This file and path is relative to the site's content file path and does not include a leading slash. To use this file in a URL, prefix with cp.Http.CdnFilePathPrefix.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="originalFilename"></param>
        /// <param name="contentName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetFilename(string fieldName, string originalFilename, string contentName);
        public abstract string GetFilename(string fieldName, string originalFilename);
        public abstract string GetFilename(string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the field value cast as an integer
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract int GetInteger(string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the field value cast as a number (double)
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract double GetNumber(string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the number of rows in the result.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract int GetRowCount();
        //
        //====================================================================================================
        /// <summary>
        /// returns the query used to generate the results
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetSQL();
        //
        //====================================================================================================
        /// <summary>
        /// Returns the result and converts it to a text type. For field types that store text in files, the text is returned instead of the filename. These include textfile, cssfile, javascriptfile. For file types that do not contain text, the filename is returned. These include filetype and imagefiletype.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetText(string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the result of getText() after verifying it's content is valid for use in Html content. If the field is a fieldTypeHtml the content is returned without conversion. If the field is any other type, the content is HtmlEncoded first (> converted to &gt;, etc)
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetHtml(string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Move to the next record in a result set.
        /// </summary>
        /// <remarks></remarks>
        public abstract void GoNext();
        //
        //====================================================================================================
        /// <summary>
        /// Move to the next record in a result set and return true if the row is valid.
        /// </summary>
        /// <remarks></remarks>
        public abstract bool NextOK();
        //
        //====================================================================================================
        /// <summary>
        /// Returns true if there is valid data in the current row of the result set.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract bool OK();
        //
        //====================================================================================================
        /// <summary>
        /// Forces a save of any changes made to the current row. A save occurs automatically when the content set is closed or when it moves to another row.
        /// </summary>
        /// <remarks></remarks>
        public abstract void Save();
        //
        //====================================================================================================
        /// <summary>
        /// Sets a value in a field of the current row.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        /// <remarks></remarks>
        public abstract void SetField(string fieldName, object fieldValue);
        //
        //====================================================================================================
        /// <summary>
        /// Sets a value in a field of the current row.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public abstract void SetField(string fieldName, string fieldValue);
        //
        //====================================================================================================
        /// <summary>
        /// Sets a value in a field of the current row.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public abstract void SetField(string fieldName, int fieldValue);
        //
        //====================================================================================================
        /// <summary>
        /// Sets a value in a field of the current row.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public abstract void SetField(string fieldName, bool fieldValue);
        //
        //====================================================================================================
        /// <summary>
        /// Sets a value in a field of the current row.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public abstract void SetField(string fieldName, DateTime fieldValue);
        //
        //====================================================================================================
        /// <summary>
        /// Processes a value from the incoming request to a field in the current row.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="requestName"></param>
        /// <remarks></remarks>
        public abstract void SetFormInput(string fieldName, string requestName);
        public abstract void SetFormInput(string fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Return the value directly from the field, without the conversions associated with GetText().
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public abstract string GetValue(string fieldName);
        //
        //====================================================================================================
        // Deprecated
        //
        [Obsolete("Use OpenGroupUsers instead. The GroupCommaList is a comma delimited list of groups and cannot handle group names that include a comma.", false)]
        public abstract bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria, string SortFieldList, bool ActiveOnly, int PageSize, int PageNumber);
        //
        [Obsolete("Use OpenGroupUsers instead. The GroupCommaList is a comma delimited list of groups and cannot handle group names that include a comma.", false)]
        public abstract bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria, string SortFieldList, bool ActiveOnly, int PageSize);
        //
        [Obsolete("Use OpenGroupUsers instead. The GroupCommaList is a comma delimited list of groups and cannot handle group names that include a comma.", false)]
        public abstract bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria, string SortFieldList, bool ActiveOnly);
        //
        [Obsolete("Use OpenGroupUsers instead. The GroupCommaList is a comma delimited list of groups and cannot handle group names that include a comma.", false)]
        public abstract bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria, string SortFieldList);
        //
        [Obsolete("Use OpenGroupUsers instead. The GroupCommaList is a comma delimited list of groups and cannot handle group names that include a comma.", false)]
        public abstract bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria);
        //
        [Obsolete("Use OpenGroupUsers instead. The GroupCommaList is a comma delimited list of groups and cannot handle group names that include a comma.", false)]
        public abstract bool OpenGroupListUsers(string GroupCommaList);
        //
        [Obsolete("Use SetField for all field types that store data in files (textfile, cssfile, etc)", true)]
        public abstract void SetFile(string FieldName, string Copy, string ContentName);
        //
        [Obsolete("Use getText to get copy, getFilename to get file.", false)]
        public abstract string GetTextFile(string FieldName);
        //
        [Obsolete("Use OpenSql", false)]
        public abstract bool OpenSQL2(string SQL, string DataSourcename, int PageSize, int PageNumber);
        //
        [Obsolete("Use OpenSql", false)]
        public abstract bool OpenSQL2(string SQL, string DataSourcename, int PageSize);
        //
        [Obsolete("Use OpenSql", false)]
        public abstract bool OpenSQL2(string SQL, string DataSourcename);
        //
        [Obsolete("Use OpenSql", false)]
        public abstract bool OpenSQL2(string SQL);
        //
        [Obsolete("Use GetFormInput(string,string,int,int,string)", false)]
        public abstract object GetFormInput(string contentName, string fieldName, string height, string width, string htmlId);
        //
        [Obsolete("Use GetFormInput(string,string,int,int)", false)]
        public abstract object GetFormInput(string contentName, string fieldName, string height, string width);
        //
        [Obsolete("Use GetFormInput(string,string,int)", false)]
        public abstract object GetFormInput(string contentName, string fieldName, string height);
        //
        //====================================================================================================
        /// <summary>
        /// support IDisposable
        /// </summary>
        public abstract void Dispose();
    }

}

