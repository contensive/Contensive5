
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using Contensive.BaseClasses;
using NLog;

namespace Contensive.Processor {
    /// <summary>
    /// The implementation of the CPCSBaseClass interface. Base interface is exposed to addons, and this implementation is passed during run-time
    /// </summary>
    public class CPCSClass : CPCSBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// dependancies
        /// </summary>
        private readonly CPClass cp;
        /// <summary>
        /// The dataset backing up this object
        /// </summary>
        private CsModel cs;
        //
        //====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        public CPCSClass(CPBaseClass cp) {
            this.cp = (CPClass)cp;
            cs = new CsModel(this.cp.core);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Insert a record, leaving the dataset open in this object. Call cs.close() to close the data
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        public override bool Insert(string contentName) {
            try {
                return cs.insert(contentName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="selectFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <returns></returns>
        public override bool OpenRecord(string contentName, int recordId, string selectFieldList, bool activeOnly) {
            try {
                if (!cs.openRecord(contentName, recordId, selectFieldList)) { return false; }
                if (!activeOnly || cs.getBoolean("active")) { return true; }
                cs.close();
                return false;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public override bool OpenRecord(string contentName, int recordId)
            => OpenRecord(contentName, recordId, "", true);
        //
        //====================================================================================================
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordId"></param>
        /// <param name="selectFieldList"></param>
        /// <returns></returns>
        public override bool OpenRecord(string contentName, int recordId, string selectFieldList)
            => OpenRecord(contentName, recordId, selectFieldList, true);
        //
        //====================================================================================================
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <param name="selectFieldList"></param>
        /// <param name="activeOnly"></param>
        /// <returns></returns>
        public override bool OpenRecord(string contentName, string recordGuid, string selectFieldList, bool activeOnly) {
            try {
                if (!cs.openRecord(contentName, recordGuid, selectFieldList)) { return false; }
                if (!activeOnly || cs.getBoolean("active")) { return true; }
                cs.close();
                return false;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }

        }
        //
        //====================================================================================================
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <param name="selectFieldList"></param>
        /// <returns></returns>
        public override bool OpenRecord(string contentName, string recordGuid, string selectFieldList)
                    => OpenRecord(contentName, recordGuid, selectFieldList, true);
        //
        //====================================================================================================
        /// <summary>
        /// Open dataset for the record specified
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="recordGuid"></param>
        /// <returns></returns>
        public override bool OpenRecord(string contentName, string recordGuid)
            => OpenRecord(contentName, recordGuid, "", true);
        //
        //====================================================================================================
        //
        public override bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly, string selectFieldList, int pageSize, int pageNumber) {
            try {
                return cs.open(contentName, sqlCriteria, sortFieldList, activeOnly, 0, selectFieldList, pageSize, pageNumber);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //
        //====================================================================================================
        //
        public override bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly, string selectFieldList, int pageSize)
            => Open(contentName, sqlCriteria, sortFieldList, activeOnly, selectFieldList, pageSize, 1);
        //
        //====================================================================================================
        //
        public override bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly, string selectFieldList)
            => Open(contentName, sqlCriteria, sortFieldList, activeOnly, selectFieldList, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly)
            => Open(contentName, sqlCriteria, sortFieldList, activeOnly, "", DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool Open(string contentName, string sqlCriteria, string sortFieldList)
            => Open(contentName, sqlCriteria, sortFieldList, true, "", DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool Open(string contentName, string sqlCriteria)
            => Open(contentName, sqlCriteria, "", true, "", DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool Open(string contentName)
            => Open(contentName, "", "", true, "", DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize, int pageNumber) {
            try {
                return cs.openGroupUsers(groupList, sqlCriteria, sortFieldList, activeOnly, pageSize, pageNumber);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize)
            => OpenGroupUsers(groupList, sqlCriteria, sortFieldList, activeOnly, pageSize, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly)
            => OpenGroupUsers(groupList, sqlCriteria, sortFieldList, activeOnly, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList)
            => OpenGroupUsers(groupList, sqlCriteria, sortFieldList, true, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(List<string> groupList, string sqlCriteria)
            => OpenGroupUsers(groupList, sqlCriteria, "", true, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(List<string> groupList)
            => OpenGroupUsers(groupList, "", "", true, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(string groupName, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize, int pageNumber)
            => OpenGroupUsers(new List<string> { groupName }, sqlCriteria, sortFieldList, activeOnly, pageSize, pageNumber);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(string groupName, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize)
            => OpenGroupUsers(new List<string> { groupName }, sqlCriteria, sortFieldList, activeOnly, pageSize, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(string groupName, string sqlCriteria, string sortFieldList, bool activeOnly)
            => OpenGroupUsers(new List<string> { groupName }, sqlCriteria, sortFieldList, activeOnly, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(string groupName, string sqlCriteria, string sortFieldList)
            => OpenGroupUsers(new List<string> { groupName }, sqlCriteria, sortFieldList, true, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(string groupName, string sqlCriteria)
            => OpenGroupUsers(new List<string> { groupName }, sqlCriteria, "", true, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenGroupUsers(string groupName)
            => OpenGroupUsers(new List<string> { groupName }, "", "", true, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenSQL(string sql, string dataSourcename, int pageSize, int pageNumber) {
            try {
                if (((string.IsNullOrEmpty(sql)) || (sql.ToLowerInvariant() == "default")) && (!string.IsNullOrEmpty(dataSourcename)) && (dataSourcename.ToLowerInvariant() != "default")) {
                    //
                    // -- arguments reversed from legacy api mistake, datasource has the query, sql has the datasource
                    logger.Warn($"{cp.core.logCommonMessage},Call to cs with arguments reversed, datasource [" + dataSourcename + "], sql [" + sql + "]");
                    return cs.openSql(dataSourcename, sql, pageSize, pageNumber);
                }
                return cs.openSql(sql, dataSourcename, pageSize, pageNumber);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override bool OpenSQL(string sql, string dataSourcename, int pageSize)
            => OpenSQL(sql, dataSourcename, pageSize, 1);
        //
        //====================================================================================================
        //
        public override bool OpenSQL(string sql, string dataSourcename)
            => OpenSQL(sql, dataSourcename, DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override bool OpenSQL(string sql)
            => OpenSQL(sql, "", DbController.sqlPageSizeDefault, 1);
        //
        //====================================================================================================
        //
        public override void Close() {
            try {
                cs.close();
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override object GetFormInput(string contentName, string fieldName, int height, int width, string htmlId) {
            return cp.core.html.inputCs(cs, contentName, fieldName, height, width, htmlId);
        }
        public override object GetFormInput(string contentName, string fieldName, int height, int width) {
            return cp.core.html.inputCs(cs, contentName, fieldName, height, width);
        }
        public override object GetFormInput(string contentName, string fieldName, int height) {
            return cp.core.html.inputCs(cs, contentName, fieldName, height);
        }
        public override object GetFormInput(string contentName, string fieldName) {
            return cp.core.html.inputCs(cs, contentName, fieldName);
        }
        //
        //====================================================================================================
        //
        public override void Delete() {
            try {
                cs.deleteRecord();
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override bool FieldOK(string fieldName) {
            try {
                return cs.isFieldSupported(fieldName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override void GoFirst() {
            try {
                cs.goFirst(false);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override string GetAddLink(string presetNameValueList, bool allowPaste) {
            try {
                return cs.getRecordAddLink(presetNameValueList, allowPaste);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        public override string GetAddLink(string presetNameValueList)
            => GetAddLink(presetNameValueList, false);
        //
        public override string GetAddLink()
            => GetAddLink("", false);
        //
        //====================================================================================================
        //
        public override bool GetBoolean(string FieldName) {
            try {
                return cs.getBoolean(FieldName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override DateTime GetDate(string FieldName) {
            try {
                return cs.getDate(FieldName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override string GetEditLink(bool allowCut) {
            try {
                return cs.getRecordEditLink(allowCut);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override string GetEditLink() => GetEditLink(false);
        //
        //====================================================================================================
        //
        public override string GetEditWrapper(string innerHtml) {
            return EditUIController.getEditWrapper(cp.core, "", GetEditLink() + innerHtml);
        }
        //
        //====================================================================================================
        //
        public override string GetFilename(string fieldName, string originalFilename, string contentName) {
            try {
                return cs.getFilename(fieldName, originalFilename, contentName, 0);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        public override string GetFilename(string fieldName, string originalFilename) {
            try {
                return cs.getFilename(fieldName, originalFilename);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }

        public override string GetFilename(string fieldName) {
            try {
                return cs.getFilename(fieldName, "");
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override int GetInteger(string FieldName) {
            try {
                return cs.getInteger(FieldName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override double GetNumber(string FieldName) {
            try {
                return cs.getNumber(FieldName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override int GetRowCount() {
            try {
                return cs.getRowCount();
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override string GetSQL() {
            try {
                return cs.getSql();
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override string GetText(string fieldName) {
            try {
                return cs.getText(fieldName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override string GetHtml(string fieldName) {
            try {
                return cs.getText(fieldName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override void GoNext() {
            try {
                cs.goNext();
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override bool NextOK() {
            try {
                cs.goNext();
                return cs.ok();
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override bool OK() {
            try {
                return cs.ok();
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override void Save() {
            try {
                cs.save();
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override void SetField(string fieldName, object fieldValue) {
            try {
                if (fieldValue is string fieldString) {
                    cs.set(fieldName, fieldString);
                    return;
                }
                int? fieldInt = fieldValue as int?;
                if (fieldInt != null) {
                    cs.set(fieldName, fieldInt.GetValueOrDefault());
                    return;
                }
                bool? fieldBool = fieldValue as bool?;
                if (fieldBool != null) {
                    cs.set(fieldName, fieldBool.GetValueOrDefault());
                    return;
                }
                DateTime? fieldDate = fieldValue as DateTime?;
                if (fieldDate != null) {
                    cs.set(fieldName, fieldDate.GetValueOrDefault());
                    return;
                }
                cs.set(fieldName, fieldValue.ToString());
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        public override void SetField(string FieldName, int FieldValue) {
            cs.set(FieldName, FieldValue);
        }
        //
        public override void SetField(string FieldName, bool FieldValue) {
            cs.set(FieldName, FieldValue);
        }
        //
        public override void SetField(string FieldName, DateTime FieldValue) {
            cs.set(FieldName, FieldValue);
        }
        //
        public override void SetField(string FieldName, String FieldValue) {
            cs.set(FieldName, FieldValue);
        }
        //
        //====================================================================================================
        //
        public override void SetFormInput(string fieldName, string requestName) {
            try {
                cs.setFormInput(cp.core, fieldName, requestName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        public override void SetFormInput(string fieldName)
            => SetFormInput(fieldName, fieldName);
        //
        //====================================================================================================
        /// <summary>
        /// Return the value in the field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public override string GetValue(string fieldName) {
            return cs.getValueStoredInDbField(fieldName);
        }
        //
        //====================================================================================================
        //
        [Obsolete("Use OpenGroupUsers()", false)]
        public override bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria, string SortFieldList, bool ActiveOnly, int PageSize, int PageNumber) {
            List<string> groupList = new List<string>();
            groupList.AddRange(GroupCommaList.Split(','));
            return OpenGroupUsers(groupList, SQLCriteria, SortFieldList, ActiveOnly, PageSize, PageNumber);
        }
        //
        [Obsolete("Use OpenGroupUsers()", false)]
        public override bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria, string SortFieldList, bool ActiveOnly, int PageSize) {
            List<string> groupList = new List<string>();
            groupList.AddRange(GroupCommaList.Split(','));
            return OpenGroupUsers(groupList, SQLCriteria, SortFieldList, ActiveOnly, PageSize);
        }
        //
        [Obsolete("Use OpenGroupUsers()", false)]
        public override bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria, string SortFieldList, bool ActiveOnly) {
            List<string> groupList = new List<string>();
            groupList.AddRange(GroupCommaList.Split(','));
            return OpenGroupUsers(groupList, SQLCriteria, SortFieldList, ActiveOnly);
        }
        //
        [Obsolete("Use OpenGroupUsers()", false)]
        public override bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria, string SortFieldList) {
            List<string> groupList = new List<string>();
            groupList.AddRange(GroupCommaList.Split(','));
            return OpenGroupUsers(groupList, SQLCriteria, SortFieldList);
        }
        //
        [Obsolete("Use OpenGroupUsers()", false)]
        public override bool OpenGroupListUsers(string GroupCommaList, string SQLCriteria) {
            List<string> groupList = new List<string>();
            groupList.AddRange(GroupCommaList.Split(','));
            return OpenGroupUsers(groupList, SQLCriteria);
        }
        //
        [Obsolete("Use OpenGroupUsers()", false)]
        public override bool OpenGroupListUsers(string GroupCommaList) {
            List<string> groupList = new List<string>();
            groupList.AddRange(GroupCommaList.Split(','));
            return OpenGroupUsers(groupList);
        }
        //
        [Obsolete("Use GetFormInput(string,string,int,int,string)", false)]
        public override object GetFormInput(string contentName, string fieldName, string height, string width, string htmlId)
            => GetFormInput(contentName, fieldName, GenericController.getInteger(height), GenericController.getInteger(width), htmlId);
        //
        [Obsolete("Use GetFormInput(string,string,int,int)", false)]
        public override object GetFormInput(string contentName, string fieldName, string height, string width)
            => GetFormInput(contentName, fieldName, GenericController.getInteger(height), GenericController.getInteger(width));
        //
        //
        [Obsolete("Use GetFormInput(string,string,int)", false)]
        public override object GetFormInput(string contentName, string fieldName, string height)
            => GetFormInput(contentName, fieldName, GenericController.getInteger(height));
        //
        [Obsolete("Use SetField for all field types that store data in files (textfile, cssfile, etc)", false)]
        public override void SetFile(string FieldName, string Copy, string ContentName) {
            try {
                cs.setTextFile(FieldName, Copy, ContentName);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        [Obsolete("Use OpenSql()", false)]
        public override bool OpenSQL2(string sql, string DataSourcename, int PageSize, int PageNumber) {
            return cs.openSql(sql, DataSourcename, PageSize, PageNumber);
        }
        //
        [Obsolete("Use OpenSql()", false)]
        public override bool OpenSQL2(string sql, string DataSourcename, int PageSize) {
            return cs.openSql(sql, DataSourcename, PageSize);
        }
        //
        [Obsolete("Use OpenSql()", false)]
        public override bool OpenSQL2(string sql, string DataSourcename) {
            return cs.openSql(sql, DataSourcename);
        }
        //
        [Obsolete("Use OpenSql()", false)]
        public override bool OpenSQL2(string sql) {
            return cs.openSql(sql);
        }
        //
        [Obsolete("Use getText for a fields copy and getFilename for the fields filename if the field stores copy in a file", false)]
        public override string GetTextFile(string FieldName) {
            return cs.getText(FieldName);
        }
        //
        //====================================================================================================
        // Disposable
        //
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public sealed override void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        // dispose
        //
        protected virtual void Dispose(bool disposing_cs) {
            if (!this.disposed_cs) {
                if (disposing_cs) {
                    //
                    // ----- call .dispose for managed objects
                    if (cs != null) {
                        cs.Dispose();
                        cs = null;
                    }
                }
                //
                // ----- release unmanaged resources
                //
            }
            this.disposed_cs = true;
        }

        ~CPCSClass() {
            Dispose(false);
        }
        protected bool disposed_cs;
        #endregion
    }

}

