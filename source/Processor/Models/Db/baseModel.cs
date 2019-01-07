﻿
using System;
using System.Reflection;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using System.Data;
using Contensive.Processor.Exceptions;
using System.Text;
using Contensive.BaseClasses;
//
namespace Contensive.Processor.Models.Db {
    //
    //====================================================================================================
    // db model pattern
    //   new() - empty constructor to allow deserialization
    //   saveObject() - saves instance properties (nonstatic method)
    //   create() - loads instance properties and returns a model 
    //   delete() - deletes the record that matches the argument
    //   getObjectList() - a pattern for creating model lists.
    //   invalidateFIELDNAMEcache() - method to invalide the model cache. One per cache
    //
    //	1) set the primary content name in const cnPrimaryContent. avoid constants Like cnAddons used outside model
    //	2) find-And-replace "_blankModel" with the name for this model
    //	3) when adding model fields, add in three places: the Public Property, the saveObject(), the loadObject()
    //	4) when adding create() methods to support other fields/combinations of fields, 
    //       - add a secondary cache For that new create method argument in loadObjec()
    //       - add it to the injected cachename list in loadObject()
    //       - add an invalidate
    //
    // Model Caching
    //   caching applies to model objects only, not lists of models (for now)
    //       - this is because of the challenge of invalidating the list object when individual records are added or deleted
    //
    //   a model should have 1 primary cache object which stores the data and can have other secondary cacheObjects which do not hold data
    //    the cacheName of the 'primary' cacheObject for models and db records (cacheNamePrefix + ".id." + #id)
    //    'secondary' cacheName is (cacheNamePrefix + . + fieldName + . + #)
    //
    //   cacheobjects can be used to hold data (primary cacheobjects), or to hold only metadata (secondary cacheobjects)
    //       - primary cacheobjects are like 'personModel.id.99' that holds the model for id=99
    //           - it is primary because the .primaryobject is null
    //           - invalidationData. This cacheobject is invalid after this datetime
    //           - dependentobjectlist() - this object is invalid if any of those objects are invalid
    //       - secondary cachobjects are like 'person.ccguid.12345678'. It does not hold data, just a reference to the primary cacheobject
    //
    //   cacheNames spaces are replaced with underscores, so "addon collections" should be addon_collections
    //
    //   cacheNames that match content names are treated as caches of "any" record in the content, so invalidating "people" can be used to invalidate
    //       any non-specific cache in the people table, by including "people" as a dependant cachename. the "people" cachename should not clear
    //       specific people caches, like people.id.99, but can be used to clear lists of records like "staff_list_group"
    //       - this can be used as a fallback strategy to cache record lists: a remote method list can be cached with a dependancy on "add-ons".
    //       - models should always clear this content name cache entry on all cache clears
    //
    //   when a model is created, the code first attempts to read the model's cacheobject. if it fails, it builds it and saves the cache object and tags
    //       - when building the model, is writes object to the primary cacheobject, and writes all the secondaries to be used
    //       - when building the model, if a database record is opened, a dependantObject Tag is created for the tablename+'id'+id
    //       - when building the model, if another model is added, that model returns its cachenames in the cacheNameList to be added as dependentObjects
    //
    //
    public class DbModel {
        //
        //====================================================================================================
        //-- const must be set in derived clases
        //
        // -Public Const contentTableName As String = "" '<------ set to tablename for the primary content (used for cache names)
        // -Public Const contentDataSource As String = "" '<----- set to datasource if not default
        //
        //====================================================================================================
        //-- field types
        //
        public class FieldTypeFileBase {
            //
            // -- 
            // during load
            //   -- The filename is loaded into the model (blank or not). No content Is read from the file during load.
            //   -- the internalcore must be set
            //
            // during a cache load, the internalcore must be set
            //
            // content property read:
            //   -- If the filename Is blank, a blank Is returned
            //   -- if the filename exists, the content is read into the model and returned to the consumer
            //
            // content property written:
            //   -- content is stored in the model until save(). contentUpdated is set.
            //
            // filename property read: nothing special
            //
            // filename property written:
            //   -- contentUpdated set true if it was previously set (content was written), or if the content is not empty
            //
            // contentLoaded property means the content in the model is valid
            // contentUpdated property means the content needs to be saved on the next save
            //
            public string filename {
                set {
                    _filename = value;
                    //
                    // -- mark content updated if the content was updated, or if the content is not blank (so old content is written to the new updated filename)
                    contentUpdated = contentUpdated || (!string.IsNullOrEmpty(_content));
                }
                get {
                    return _filename;
                }
            }
            private string _filename = "";
            //
            // -- content in the file. loaded as needed, not during model create. 
            public string content {
                set {
                    _content = value;
                    contentUpdated = true;
                    contentLoaded = true;
                }
                get {
                    if (!contentLoaded) {
                        // todo if internalcore is not set, throw an error
                        if ((!string.IsNullOrEmpty(filename)) && (internalcore != null)) {
                            contentLoaded = true;
                            _content = internalcore.cdnFiles.readFileText(filename);
                        }
                    }
                    return _content;
                }
            }
            //
            // -- internal storage for content
            private string _content { get; set; } = "";
            //
            // -- When field is deserialized from cache, contentLoaded flag is used to deferentiate between unloaded content and blank conent.
            public bool contentLoaded { get; set; } = false;
            //
            // -- When content is updated, the model.save() writes the file
            public bool contentUpdated { get; set; } = false;
            //
            // -- set by load(). Used by field to read content from filename when needed
            public CoreController internalcore { get; set; } = null;
        }
        //
        public class FieldTypeTextFile : FieldTypeFileBase {
        }
        public class FieldTypeJavascriptFile : FieldTypeFileBase {
        }
        public class FieldTypeCSSFile : FieldTypeFileBase {
        }
        public class FieldTypeHTMLFile : FieldTypeFileBase {
        }
        //
        //====================================================================================================
        // -- instance properties
        /// <summary>
        /// identity integer, primary key for every table
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// name of the record used for lookup lists
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// optional guid, created automatically in the model
        /// </summary>
        public string ccguid { get; set; }
        /// <summary>
        /// optionally can be used to disable a record. Must be implemented in each query
        /// </summary>
        public bool active { get; set; }
        /// <summary>
        /// id of the metadata record in ccContent that controls the display and handing for this record
        /// </summary>
        public int contentControlID { get; set; }
        /// <summary>
        /// foreign key to ccmembers table, populated by admin when record added.
        /// </summary>
        public int createdBy { get; set; }
        /// <summary>
        /// used when creating new record
        /// </summary>
        public int createKey { get; set; }
        /// <summary>
        /// date record added, populated by admin when record added.
        /// </summary>
        public DateTime dateAdded { get; set; }
        /// <summary>
        /// foreign key to ccmembers table set to user who modified the record last in the admin site
        /// </summary>
        public int modifiedBy { get; set; }
        /// <summary>
        /// date when the record was last modified in the admin site
        /// </summary>
        public DateTime modifiedDate { get; set; }
        /// <summary>
        /// optionally used to sort recrods in the table
        /// </summary>
        public string sortOrder { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// return the name of the database table associated to the derived content
        /// </summary>
        /// <param name="derivedType"></param>
        /// <returns></returns>
        public static string derivedTableName(Type derivedType) {
            FieldInfo fieldInfo = derivedType.GetField("contentTableName");
            if (fieldInfo == null) {
                throw new GenericException("Class [" + derivedType.Name + "] must declare constant [contentTableName].");
            } else {
                return fieldInfo.GetRawConstantValue().ToString();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the name of the datasource assocated to the database table assocated to the derived content
        /// </summary>
        /// <param name="derivedType"></param>
        /// <returns></returns>
        public static string derivedDataSourceName(Type derivedType) {
            FieldInfo fieldInfo = derivedType.GetField("contentDataSource");
            if (fieldInfo == null) {
                throw new GenericException("Class [" + derivedType.Name + "] must declare public constant [contentDataSource].");
            } else {
                return fieldInfo.GetRawConstantValue().ToString();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns the boolean value of the constant nameIsUnique in the derived class. Setting true enables a name cache ptr.
        /// </summary>
        /// <param name="derivedType"></param>
        /// <returns></returns>
        public static bool derivedNameFieldIsUnique(Type derivedType) {
            FieldInfo fieldInfo = derivedType.GetField("nameFieldIsUnique");
            if (fieldInfo == null) {
                throw new GenericException("Class [" + derivedType.Name + "] must declare public constant [nameFieldIsUnique].");
            } else {
                return (bool)fieldInfo.GetRawConstantValue();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// simple constructor needed for deserialization
        /// </summary>
        public DbModel() {
            //
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a new recod to the db and open it. Starting a new model with this method will use the default values in Contensive metadata (active, contentcontrolid, etc).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <returns></returns>
        public static T addDefault<T>(CoreController core, Domain.MetaModel metaData) where T : DbModel {
            var callersCacheNameList = new List<string>();
            return addDefault<T>(core, metaData, ref callersCacheNameList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a new record to the db populated with default values from the content definition and return an object of it
        /// </summary>
        /// <param name="core"></param>
        /// <param name="callersCacheNameList"></param>
        /// <returns></returns>
        public static T addDefault<T>(CoreController core, Domain.MetaModel metaData, ref List<string> callersCacheNameList) where T : DbModel {
            T result = default(T);
            try {
                result = AddEmpty<T>(core);
                if (result != null) {
                    foreach (PropertyInfo modelProperty in result.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                        string propertyName = modelProperty.Name;
                        string propertyValue = "";
                        if (metaData.fields.ContainsKey(propertyName.ToLowerInvariant())) {
                            propertyValue = metaData.fields[propertyName.ToLowerInvariant()].defaultValue;
                            switch (propertyName.ToLowerInvariant()) {
                                case "specialcasefield":
                                    break;
                                default:
                                    switch (modelProperty.PropertyType.Name) {
                                        case "Int32": {
                                                modelProperty.SetValue(result, GenericController.encodeInteger(propertyValue), null);
                                                break;
                                            }
                                        case "Boolean": {
                                                modelProperty.SetValue(result, GenericController.encodeBoolean(propertyValue), null);
                                                break;
                                            }
                                        case "DateTime": {
                                                modelProperty.SetValue(result, GenericController.encodeDate(propertyValue), null);
                                                break;
                                            }
                                        case "Double": {
                                                modelProperty.SetValue(result, GenericController.encodeNumber(propertyValue), null);
                                                break;
                                            }
                                        case "String": {
                                                modelProperty.SetValue(result, propertyValue, null);
                                                break;
                                            }
                                        case "FieldTypeTextFile": {
                                                //
                                                // -- cdn files
                                                FieldTypeTextFile instanceFileType = new FieldTypeTextFile { filename = propertyValue };
                                                modelProperty.SetValue(result, instanceFileType);
                                                break;
                                            }
                                        case "FieldTypeJavascriptFile": {
                                                //
                                                // -- cdn files
                                                FieldTypeJavascriptFile instanceFileType = new FieldTypeJavascriptFile { filename = propertyValue };
                                                modelProperty.SetValue(result, instanceFileType);
                                                break;
                                            }
                                        case "FieldTypeCSSFile": {
                                                //
                                                // -- cdn files
                                                FieldTypeCSSFile instanceFileType = new FieldTypeCSSFile { filename = propertyValue };
                                                modelProperty.SetValue(result, instanceFileType);
                                                break;
                                            }
                                        case "FieldTypeHTMLFile": {
                                                //
                                                // -- private files
                                                FieldTypeHTMLFile instanceFileType = new FieldTypeHTMLFile { filename = propertyValue };
                                                modelProperty.SetValue(result, instanceFileType);
                                                break;
                                            }
                                        default: {
                                                modelProperty.SetValue(result, propertyValue, null);
                                                break;
                                            }
                                    }
                                    break;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a new empty record to the db and return an object of it.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="callersCacheNameList"></param>
        /// <returns></returns>
        public static T AddEmpty<T>(CoreController core) where T : DbModel {
            T result = default(T);
            try {
                if (core.serverConfig == null) {
                    //
                    // -- cannot use models without an application
                    LogController.handleError(core, new GenericException("Cannot use data models without a valid server configuration."));
                } else if (core.appConfig == null) {
                    //
                    // -- cannot use models without an application
                    LogController.handleError(core, new GenericException("Cannot use data models without a valid application configuration."));
                } else {
                    Type instanceType = typeof(T);
                    string tableName = derivedTableName(instanceType);
                    using (var db = new DbController(core, derivedDataSourceName(instanceType))) {
                        result = create<T>(core, db.insertTableRecordGetId(tableName, core.session.user.id));
                    }
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a new model with the data selected.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static T create<T>(CoreController core, int recordId) where T : DbModel {
            var tempVar = new List<string>();
            return create<T>(core, recordId, ref tempVar);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a new model with the data selected. All cacheNames related to the object will be added to the cacheNameList.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordId">The id of the record to be read into the new object</param>
        /// <param name="callersCacheNameList">Any cachenames effected by this record will be added to this list. If the method consumer creates a cache object, add these cachenames to its dependent cachename list.</param>
        public static T create<T>(CoreController core, int recordId, ref List<string> callersCacheNameList) where T : DbModel {
            T result = default(T);
            try {
                if (core.serverConfig == null) {
                    //
                    // -- cannot use models without an application
                    LogController.handleError(core, new GenericException("Cannot use data models without a valid server configuration."));
                } else if (core.appConfig == null) {
                    //
                    // -- cannot use models without an application
                    LogController.handleError(core, new GenericException("Cannot use data models without a valid application configuration."));
                } else {
                    if (recordId > 0) {
                        result = readRecordCache<T>(core, recordId);
                        if (result == null) {
                            using (var db = new DbController(core, derivedDataSourceName(typeof(T)))) {
                                using (var dt = db.executeQuery(getSelectSql<T>(core, null, "(id=" + recordId + ")", ""))) {
                                    if (dt != null) {
                                        if (dt.Rows.Count > 0) {
                                            result = loadRecord<T>(core, dt.Rows[0], ref callersCacheNameList);
                                        }
                                    }
                                }

                            }

                        }
                        //
                        // -- store core in all extended fields that need it (file fields so content read can happen on demand instead of at load)
                        if (result != null) {
                            foreach (PropertyInfo instanceProperty in result.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                                switch (instanceProperty.PropertyType.Name) {
                                    case "FieldTypeJavascriptFile": {
                                            FieldTypeJavascriptFile fileProperty = (FieldTypeJavascriptFile)instanceProperty.GetValue(result);
                                            fileProperty.internalcore = core;
                                            break;
                                        }
                                    case "FieldTypeCSSFile": {
                                            FieldTypeCSSFile fileProperty = (FieldTypeCSSFile)instanceProperty.GetValue(result);
                                            fileProperty.internalcore = core;
                                            break;
                                        }
                                    case "FieldTypeHTMLFile": {
                                            FieldTypeHTMLFile fileProperty = (FieldTypeHTMLFile)instanceProperty.GetValue(result);
                                            fileProperty.internalcore = core;
                                            break;
                                        }
                                    case "FieldTypeTextFile": {
                                            FieldTypeTextFile fileProperty = (FieldTypeTextFile)instanceProperty.GetValue(result);
                                            fileProperty.internalcore = core;
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// create an object from a record with matching ccGuid
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <param name="recordGuid"></param>
        /// <returns></returns>
        public static T create<T>(CoreController core, string recordGuid) where T : DbModel {
            var tempVar = new List<string>();
            return create<T>(core, recordGuid, ref tempVar);
        }
        //
        //====================================================================================================
        /// <summary>
        /// create an object from a record with a matching ccguid, add an object cache name to the argument list
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordGuid"></param>
        public static T create<T>(CoreController core, string recordGuid, ref List<string> callersCacheNameList) where T : DbModel {
            T result = default(T);
            try {
                if (core.serverConfig == null) {
                    //
                    // -- cannot use models without an application
                    LogController.handleError(core, new GenericException("Cannot use data models without a valid server configuration."));
                } else if (core.appConfig == null) {
                    //
                    // -- cannot use models without an application
                    LogController.handleError(core, new GenericException("Cannot use data models without a valid application configuration."));
                } else {
                    if (!string.IsNullOrEmpty(recordGuid)) {
                        result = readRecordCacheByGuidPtr<T>(core, recordGuid);
                        if (result == null) {
                            using (var db = new DbController(core, derivedDataSourceName(typeof(T)))) {
                                using (var dt = db.executeQuery(getSelectSql<T>(core, null, "(ccGuid=" + DbController.encodeSQLText(recordGuid) + ")", ""))) {
                                    if (dt != null) {
                                        if (dt.Rows.Count > 0) {
                                            result = loadRecord<T>(core, dt.Rows[0], ref callersCacheNameList);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// create an object from the first record found from a list created with matching name records, ordered by id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <param name="recordName"></param>
        /// <returns></returns>
        public static T createByUniqueName<T>(CoreController core, string recordName) where T : DbModel {
            var cacheNameList = new List<string>();
            return createByUniqueName<T>(core, recordName, ref cacheNameList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// create an object from the first record found from a list created with matching name records, ordered by id, add an object cache name to the argument list
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordName"></param>
        /// <param name="callersCacheNameList">method will add the cache name to this list.</param>
        public static T createByUniqueName<T>(CoreController core, string recordName, ref List<string> callersCacheNameList) where T : DbModel {
            T result = default(T);
            try {
                if (core.serverConfig == null) {
                    //
                    // -- cannot use models without an application
                    LogController.handleError(core, new GenericException("Cannot use data models without a valid server configuration."));
                } else if (core.appConfig == null) {
                    //
                    // -- cannot use models without an application
                    LogController.handleError(core, new GenericException("Cannot use data models without a valid application configuration."));
                } else {
                    if (!string.IsNullOrEmpty(recordName)) {
                        Type instanceType = typeof(T);
                        //
                        // -- if allowCache, then this subclass is for a content that has a unique name. read the name pointer
                        result = (derivedNameFieldIsUnique(instanceType)) ? readRecordCacheByUniqueNamePtr<T>(core, recordName) : null;
                        if (result == null) {
                            using (var db = new DbController(core, derivedDataSourceName(typeof(T)))) {
                                using (var dt = db.executeQuery(getSelectSql<T>(core, null, "(name=" + DbController.encodeSQLText(recordName) + ")", ""))) {
                                    if (dt != null) {
                                        if (dt.Rows.Count > 0) {
                                            result = loadRecord<T>(core, dt.Rows[0], ref callersCacheNameList);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// open an existing object
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="callersCacheKeyList"></param>
        private static T loadRecord<T>(CoreController core, DataRow row, ref List<string> callersCacheKeyList) where T : DbModel {
            T modelInstance = default(T);
            try {
                if (row != null) {
                    //filename = GenericController.encodeText(dt.Rows[0][instanceProperty.Name]);
                    Type instanceType = typeof(T);
                    string tableName = derivedTableName(instanceType);
                    int recordId = GenericController.encodeInteger(row["id"]);
                    modelInstance = (T)Activator.CreateInstance(instanceType);

                    foreach (PropertyInfo modelProperty in modelInstance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                        string propertyName = modelProperty.Name;
                        string propertyValue = row[propertyName].ToString();
                        switch (propertyName.ToLowerInvariant()) {
                            case "specialcasefield":
                                break;
                            default:
                                switch (modelProperty.PropertyType.Name) {
                                    case "Int32": {
                                            modelProperty.SetValue(modelInstance, GenericController.encodeInteger(propertyValue), null);
                                            break;
                                        }
                                    case "Boolean": {
                                            modelProperty.SetValue(modelInstance, GenericController.encodeBoolean(propertyValue), null);
                                            break;
                                        }
                                    case "DateTime": {
                                            modelProperty.SetValue(modelInstance, GenericController.encodeDate(propertyValue), null);
                                            break;
                                        }
                                    case "Double": {
                                            modelProperty.SetValue(modelInstance, GenericController.encodeNumber(propertyValue), null);
                                            break;
                                        }
                                    case "String": {
                                            modelProperty.SetValue(modelInstance, propertyValue, null);
                                            break;
                                        }
                                    case "FieldTypeTextFile": {
                                            //
                                            // -- cdn files
                                            FieldTypeTextFile instanceFileType = new FieldTypeTextFile {
                                                filename = propertyValue,
                                                internalcore = core
                                            };
                                            modelProperty.SetValue(modelInstance, instanceFileType);
                                            break;
                                        }
                                    case "FieldTypeJavascriptFile": {
                                            //
                                            // -- cdn files
                                            FieldTypeJavascriptFile instanceFileType = new FieldTypeJavascriptFile {
                                                filename = propertyValue,
                                                internalcore = core
                                            };
                                            modelProperty.SetValue(modelInstance, instanceFileType);
                                            break;
                                        }
                                    case "FieldTypeCSSFile": {
                                            //
                                            // -- cdn files
                                            FieldTypeCSSFile instanceFileType = new FieldTypeCSSFile {
                                                filename = propertyValue,
                                                internalcore = core
                                            };
                                            modelProperty.SetValue(modelInstance, instanceFileType);
                                            break;
                                        }
                                    case "FieldTypeHTMLFile": {
                                            //
                                            // -- private files
                                            FieldTypeHTMLFile instanceFileType = new FieldTypeHTMLFile {
                                                filename = propertyValue,
                                                internalcore = core
                                            };
                                            modelProperty.SetValue(modelInstance, instanceFileType);
                                            break;
                                        }
                                    default: {
                                            modelProperty.SetValue(modelInstance, propertyValue, null);
                                            break;
                                        }
                                }
                                break;
                        }
                    }

                    if (modelInstance != null) {
                        //
                        // -- set primary cache to the object created
                        // -- set secondary caches to the primary cache
                        // -- add all cachenames to the injected cachenamelist
                        if (modelInstance is DbModel baseInstance) {
                            string datasourceName = derivedDataSourceName(instanceType);
                            string cacheKey = CacheController.createCacheKey_forDbRecord(baseInstance.id, tableName, datasourceName);
                            callersCacheKeyList.Add(cacheKey);
                            core.cache.storeObject(cacheKey, modelInstance);
                            //
                            string cachePtr = CacheController.createCachePtr_forDbRecord_guid(baseInstance.ccguid, tableName, datasourceName);
                            core.cache.storePtr(cachePtr, cacheKey);
                            //
                            if (derivedNameFieldIsUnique(instanceType)) {
                                cachePtr = CacheController.createCachePtr_forDbRecord_uniqueName(baseInstance.name, tableName, datasourceName);
                                core.cache.storePtr(cachePtr, cacheKey);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
            return modelInstance;
        }
        //
        //====================================================================================================
        /// <summary>
        /// save the instance properties to a record with matching id. If id is not provided, a new record is created.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public int save(CoreController core, bool asyncSave = false) {
            try {
                if (isAppInvalid(core)) { return 0; }
                Type instanceType = this.GetType();
                string tableName = derivedTableName(instanceType);
                string datasourceName = derivedDataSourceName(instanceType);
                var sqlPairs = new SqlFieldListClass();
                using (var db = new DbController(core, derivedDataSourceName(instanceType))) {
                    foreach (PropertyInfo instanceProperty in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                        switch (instanceProperty.Name.ToLowerInvariant()) {
                            case "id":
                                break;
                            case "ccguid":
                                if (string.IsNullOrEmpty(ccguid)) { ccguid = GenericController.getGUID(); }
                                sqlPairs.add(instanceProperty.Name, DbController.encodeSQLText(instanceProperty.GetValue(this, null).ToString()));
                                break;
                            default:
                                CPContentBaseClass.fileTypeIdEnum  fieldTypeId = 0;
                                bool fileFieldContentUpdated = false;
                                FieldTypeFileBase fileProperty = null;
                                switch (instanceProperty.PropertyType.Name) {
                                    case "Int32":
                                        Int32 valueInt32 = (int)instanceProperty.GetValue(this);
                                        sqlPairs.add(instanceProperty.Name, DbController.encodeSQLNumber(valueInt32));
                                        break;
                                    case "Boolean":
                                        bool valueBool = (bool)instanceProperty.GetValue(this);
                                        sqlPairs.add(instanceProperty.Name, DbController.encodeSQLBoolean(valueBool));
                                        break;
                                    case "DateTime":
                                        DateTime valueDate = (DateTime)instanceProperty.GetValue(this, null);
                                        sqlPairs.add(instanceProperty.Name, DbController.encodeSQLDate(valueDate));
                                        break;
                                    case "Double":
                                        double valueDbl = (double)instanceProperty.GetValue(this);
                                        sqlPairs.add(instanceProperty.Name, DbController.encodeSQLNumber(valueDbl));
                                        break;
                                    case "FieldTypeTextFile": {
                                            fieldTypeId = CPContentBaseClass.fileTypeIdEnum.FileText;
                                            fileProperty = (FieldTypeTextFile)instanceProperty.GetValue(this);
                                        }
                                        break;
                                    case "FieldTypeJavascriptFile": {
                                            fieldTypeId = CPContentBaseClass.fileTypeIdEnum.FileJavascript;
                                            fileProperty = (FieldTypeJavascriptFile)instanceProperty.GetValue(this);
                                        }
                                        break;
                                    case "FieldTypeCSSFile": {
                                            fieldTypeId = CPContentBaseClass.fileTypeIdEnum.FileCSS;
                                            fileProperty = (FieldTypeCSSFile)instanceProperty.GetValue(this);
                                        }
                                        break;
                                    case "FieldTypeHTMLFile": {
                                            fieldTypeId = CPContentBaseClass.fileTypeIdEnum.FileHTML;
                                            fileProperty = (FieldTypeHTMLFile)instanceProperty.GetValue(this);
                                        }
                                        break;
                                    default:
                                        sqlPairs.add(instanceProperty.Name, DbController.encodeSQLText(instanceProperty.GetValue(this, null).ToString()));
                                        break;
                                }
                                if (!fieldTypeId.Equals(0)) {
                                    fileProperty.internalcore = core;
                                    PropertyInfo fileFieldContentUpdatedProperty = instanceProperty.PropertyType.GetProperty("contentUpdated");
                                    fileFieldContentUpdated = (bool)fileFieldContentUpdatedProperty.GetValue(fileProperty);
                                    if (fileFieldContentUpdated) {
                                        PropertyInfo fileFieldFilenameProperty = instanceProperty.PropertyType.GetProperty("filename");
                                        string fileFieldFilename = (string)fileFieldFilenameProperty.GetValue(fileProperty);
                                        if ((String.IsNullOrEmpty(fileFieldFilename)) && (id != 0)) {
                                            // 
                                            // -- if record exists and file property's filename is not set, get the filename from the Db
                                            using (System.Data.DataTable dt = db.executeQuery("select " + instanceProperty.Name + " from " + tableName + " where (id=" + id + ")")) {
                                                if (dt.Rows.Count > 0) {
                                                    fileFieldFilename = GenericController.encodeText(dt.Rows[0][instanceProperty.Name]);
                                                }
                                            }
                                        }
                                        PropertyInfo fileFieldContentProperty = instanceProperty.PropertyType.GetProperty("content");
                                        string fileFieldContent = (string)fileFieldContentProperty.GetValue(fileProperty);
                                        if ((string.IsNullOrEmpty(fileFieldContent)) && (!string.IsNullOrEmpty(fileFieldFilename))) {
                                            //
                                            // -- empty content and valid filename, delete the file and clear the filename
                                            sqlPairs.add(instanceProperty.Name, "");
                                            core.cdnFiles.deleteFile(fileFieldFilename);
                                            fileFieldFilenameProperty.SetValue(fileProperty, string.Empty);
                                        } else {
                                            //
                                            // -- save content
                                            if (string.IsNullOrEmpty(fileFieldFilename)) {
                                                fileFieldFilename = FileController.getVirtualRecordUnixPathFilename(tableName, instanceProperty.Name.ToLowerInvariant(), id, fieldTypeId);
                                                fileFieldFilenameProperty.SetValue(fileProperty, fileFieldFilename);
                                            }
                                            core.cdnFiles.saveFile(fileFieldFilename, fileFieldContent);
                                            sqlPairs.add(instanceProperty.Name, DbController.encodeSQLText(fileFieldFilename));
                                        }
                                    }

                                }
                                break;
                        }
                    }
                    if (sqlPairs.count > 0) {
                        if (id == 0) { id = db.insertTableRecordGetId(tableName); }
                        db.updateTableRecord(tableName, "(id=" + id.ToString() + ")", sqlPairs, asyncSave);
                    }
                }
                //
                // -- object is here, but the cache was invalidated, setting
                string cacheKey = CacheController.createCacheKey_forDbRecord(id, tableName, datasourceName);
                core.cache.storeObject(cacheKey, this);
                core.cache.storePtr(CacheController.createCachePtr_forDbRecord_guid(ccguid, tableName, datasourceName), cacheKey);
                if (derivedNameFieldIsUnique(instanceType)) core.cache.storePtr(CacheController.createCachePtr_forDbRecord_uniqueName(name, tableName, datasourceName), cacheKey);
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
            return id;
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete an existing database record by id
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordId"></param>
        public static void delete<T>(CoreController core, int recordId) where T : DbModel {
            try {
                if (isAppInvalid(core)) { return; }
                if (recordId <= 0) { return; }
                string dataSourceName = derivedDataSourceName(typeof(T));
                using (var db = new DbController(core, dataSourceName)) {
                    string tableName = derivedTableName(typeof(T));
                    db.deleteTableRecord(recordId, tableName);
                    core.cache.invalidate(CacheController.createCacheKey_forDbRecord(recordId, tableName, dataSourceName));
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete an existing database record by guid
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="guid"></param>
        public static void delete<T>(CoreController core, string guid) where T : DbModel {
            try {
                if (isAppInvalid(core)) { return; }
                if (string.IsNullOrEmpty(guid)) { return; }
                // todo change cache invalidate to key ptr, and we do not need to open the record first
                DbModel instance = create<DbModel>(core, guid);
                if (instance == null) { return; }
                invalidateRecordCache<T>(core, instance.id);
                using (var db = new DbController(core, derivedDataSourceName(typeof(T)))) {
                    db.deleteTableRecord(guid, derivedTableName(typeof(T)));
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// create a list of objects based on the sql criteria and sort order, and add a cache object to an argument
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="sqlCriteria"></param>
        /// <returns></returns>
        public static List<T> createList<T>(CoreController core, string sqlCriteria, string sqlOrderBy, List<string> callersCacheNameList) where T : DbModel {
            try {
                List<T> result = new List<T>();
                if (isAppInvalid(core)) { return result; }
                using (var db = new DbController(core, derivedDataSourceName(typeof(T)))) {
                    using (var dt = db.executeQuery(getSelectSql<T>(core, null, sqlCriteria, sqlOrderBy))) {
                        foreach (DataRow row in dt.Rows) {
                            T instance = loadRecord<T>(core, row, ref callersCacheNameList);
                            if (instance != null) { result.Add(instance); }
                        }
                    }
                }
                return result;
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// create a list of objects based on the sql criteria and sort order
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sqlOrderBy"></param>
        /// <returns></returns>
        public static List<T> createList<T>(CoreController core, string sqlCriteria, string sqlOrderBy) where T : DbModel => createList<T>(core, sqlCriteria, sqlOrderBy, new List<string> { });
        //
        //====================================================================================================
        /// <summary>
        /// create a list of objects based on the sql criteria
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <param name="sqlCriteria"></param>
        /// <returns></returns>
        public static List<T> createList<T>(CoreController core, string sqlCriteria) where T : DbModel => createList<T>(core, sqlCriteria, "id", new List<string> { });
        //
        public static List<T> createList<T>(CoreController core) where T : DbModel => createList<T>(core, "", "id", new List<string> { });
        //
        //====================================================================================================
        /// <summary>
        /// invalidate the cache entry for a record
        /// </summary>
        /// <param name="core"></param>
        /// <param name="recordId"></param>
        public static void invalidateRecordCache<T>(CoreController core, int recordId) where T : DbModel {
            try {
                if (isAppInvalid(core)) { return; }
                core.cache.invalidateDbRecord(recordId, derivedTableName(typeof(T)));
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// invalidate all cache entries for the table
        /// </summary>
        /// <param name="core"></param>
        /// <param name="recordId"></param>
        public static void invalidateTableCache<T>(CoreController core) where T : DbModel {
            try {
                if (isAppInvalid(core)) { return; }
                core.cache.invalidateAllKeysInTable(derivedTableName(typeof(T)));
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// get the name of the record, returning empty string if the record is null
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordId"></param>record
        /// <returns></returns>
        private static string getRecordName<T>(CoreController core, T record) where T : DbModel {
            return (record != null) ? record.name : string.Empty;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get the name of the record by it's id
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordId"></param>record
        /// <returns></returns>
        public static string getRecordName<T>(CoreController core, int recordId) where T : DbModel {
            try {
                return getRecordName<DbModel>(core, create<DbModel>(core, recordId));
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// get the name of the record by it's guid 
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="guid"></param>record
        /// <returns></returns>
        public static string getRecordName<T>(CoreController core, string guid) where T : DbModel {
            try {
                return getRecordName<DbModel>(core, create<DbModel>(core, guid));
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// get the id of the record by it's guid 
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="guid"></param>record
        /// <returns></returns>
        public static int getRecordId<T>(CoreController core, string guid) where T : DbModel {
            try {
                var record = create<DbModel>(core, guid);
                if (record != null) { return record.id; }
                return 0;
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create an empty model object, populating only control fields (guid, active, created/modified)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <returns></returns>
        public static T createEmpty<T>(CoreController core) where T : DbModel {
            try {
                T instance = (T)Activator.CreateInstance(typeof(T));
                if (isAppInvalid(core)) { return instance; }
                DateTime rightNow = DateTime.Now;
                instance.GetType().GetProperty("active", BindingFlags.Instance | BindingFlags.Public).SetValue(instance, 0, null);
                instance.GetType().GetProperty("ccguid", BindingFlags.Instance | BindingFlags.Public).SetValue(instance, GenericController.getGUID(), null);
                instance.GetType().GetProperty("dateadded", BindingFlags.Instance | BindingFlags.Public).SetValue(instance, rightNow, null);
                instance.GetType().GetProperty("modifieddate", BindingFlags.Instance | BindingFlags.Public).SetValue(instance, rightNow, null);
                instance.GetType().GetProperty("createdby", BindingFlags.Instance | BindingFlags.Public).SetValue(instance, core.session.user.id, null);
                instance.GetType().GetProperty("modifiedby", BindingFlags.Instance | BindingFlags.Public).SetValue(instance, core.session.user.id, null);
                return instance;
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read record cache by id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        private static T readRecordCache<T>(CoreController core, int recordId) where T : DbModel {
            T result = core.cache.getObject<T>(CacheController.createCacheKey_forDbRecord(recordId, derivedTableName(typeof(T)), derivedDataSourceName(typeof(T))));
            restoreCacheDataObjects(core, result);
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read a record cache by guid
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <param name="ccGuid"></param>
        /// <returns></returns>
        private static T readRecordCacheByGuidPtr<T>(CoreController core, string ccGuid) where T : DbModel {
            T result = core.cache.getObject<T>(CacheController.createCachePtr_forDbRecord_guid(ccGuid, derivedTableName(typeof(T)), derivedDataSourceName(typeof(T))));
            restoreCacheDataObjects(core, result);
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read a record cache using unique name (valid only if hasUniqueName is true)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        private static T readRecordCacheByUniqueNamePtr<T>(CoreController core, string uniqueName) where T : DbModel {
            T result = core.cache.getObject<T>(CacheController.createCachePtr_forDbRecord_uniqueName(uniqueName, derivedTableName(typeof(T)), derivedDataSourceName(typeof(T))));
            restoreCacheDataObjects(core, result);
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// After reading a cached Db object, go through instance properties and verify internal core objects populated
        /// </summary>
        private static void restoreCacheDataObjects<T>(CoreController core, T restoredInstance) {
            if (restoredInstance == null) { return; }
            foreach (PropertyInfo instanceProperty in restoredInstance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                // todo change test to is-subsclass-of-fieldCdnFile
                switch (instanceProperty.PropertyType.Name) {
                    case "FieldTypeTextFile":
                    case "FieldTypeCSSFile":
                    case "FieldTypeHTMLFile":
                    case "FieldTypeJavascriptFile":
                        FieldTypeFileBase fileProperty = (FieldTypeFileBase)instanceProperty.GetValue(restoredInstance);
                        fileProperty.internalcore = core;
                        break;
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// create an sql select for this model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <param name="fieldList"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="sqlOrderBy"></param>
        /// <returns></returns>
        public static string getSelectSql<T>(CoreController core, List<string> fieldList, string sqlCriteria, string sqlOrderBy) where T : DbModel {
            try {
                if (fieldList == null) {
                    fieldList = new List<string>();
                    T instance = (T)Activator.CreateInstance(typeof(T));
                    foreach (PropertyInfo modelProperty in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                        fieldList.Add(modelProperty.Name);
                    }
                }
                var sb = (new StringBuilder("select ")).Append(string.Join(",", fieldList.ToArray()))
                    .Append(" from ").Append(derivedTableName(typeof(T)))
                    .Append(" where (active>0)");
                if (!string.IsNullOrEmpty(sqlCriteria)) { sb.Append("and(" + sqlCriteria + ")"); }
                if (!string.IsNullOrEmpty(sqlOrderBy)) { sb.Append(" order by " + sqlOrderBy); }
                return sb.ToString();
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        public static string getSelectSql<T>(CoreController core, List<string> fieldList, string criteria) where T : DbModel => getSelectSql<T>(core, fieldList, criteria, null);
        //
        public static string getSelectSql<T>(CoreController core, List<string> fieldList) where T : DbModel => getSelectSql<T>(core, fieldList, null, null);
        //
        public static string getSelectSql<T>(CoreController core) where T : DbModel => getSelectSql<T>(core, null, null, null);
        //
        //====================================================================================================
        /// <summary>
        /// create the cache key for the table cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string getTableCacheKey<T>(CoreController core) {
            try {
                return CacheController.createCacheKey_forDbTable(derivedTableName(typeof(T)));
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Delete a selection of records
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="core"></param>
        /// <param name="sqlCriteria"></param>
        public static void deleteSelection<T>(CoreController core, string sqlCriteria) where T : DbModel {
            try {
                if (isAppInvalid(core)) { return; }
                if (string.IsNullOrEmpty(sqlCriteria)) { return; }
                using (var db = new DbController(core, derivedDataSourceName(typeof(T)))) {
                    db.deleteTableRecords(derivedTableName(typeof(T)), sqlCriteria);
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// if invalid application, log the error and return true
        /// </summary>
        /// <returns></returns>
        private static bool isAppInvalid(CoreController core) {
            if ((core.serverConfig != null) && (core.appConfig != null)) { return false; }
            LogController.handleError(core, new GenericException("Cannot use data models without a valid server and application configuration."));
            return true;
        }
    }
}
