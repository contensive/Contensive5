
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using Contensive.Processor.Controllers;
using NLog;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// Table Schema caching to speed up update
    /// </summary>
    //
    public class TableSchemaModel {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        public class ColumnSchemaModel {
            public string COLUMN_NAME;
            public string DATA_TYPE;
            public int DATETIME_PRECISION;
            public int CHARACTER_MAXIMUM_LENGTH;
        }
        //
        public class IndexSchemaModel {
            public string index_name;
            public string index_keys;
            public List<string> indexKeyList;
        }
        public string tableName { get; set; }
        public bool dirty { get; set; }
        public List<ColumnSchemaModel> columns { get; set; }
        //
        // list of all indexes, with the field it covers
        public List<IndexSchemaModel> indexes { get; set; }
        //
        //=================================================================================
        //
        public static TableSchemaModel getTableSchema(CoreController core, string TableName, string DataSourceName) {
            TableSchemaModel tableSchema = null;
            try {
                if ((!string.IsNullOrEmpty(DataSourceName)) && (DataSourceName != "-1") && (DataSourceName.ToLowerInvariant() != "default")) {
                    throw new NotImplementedException("alternate datasources not supported yet");
                } else {
                    if (!string.IsNullOrEmpty(TableName)) {
                        string lowerTablename = TableName.ToLowerInvariant();
                        bool isInCache = core.cacheRuntime.tableSchemaDictionary.TryGetValue(lowerTablename, out tableSchema);
                        bool buildCache = !isInCache;
                        if (isInCache) {
                            buildCache = tableSchema.dirty;
                        }
                        if (buildCache) {
                            //
                            // cache needs to be built
                            //
                            DataTable dt = core.db.getTableSchemaData(TableName);
                            bool isInDb = false;
                            if (dt.Rows.Count <= 0) {
                                tableSchema = null;
                            } else {
                                isInDb = true;
                                tableSchema = new Models.Domain.TableSchemaModel {
                                    columns = new List<ColumnSchemaModel>(),
                                    indexes = new List<IndexSchemaModel>(),
                                    tableName = lowerTablename
                                };
                                //
                                // load columns
                                //
                                dt = core.db.getColumnSchemaData(TableName);
                                if (dt.Rows.Count > 0) {
                                    foreach (DataRow row in dt.Rows) {
                                        tableSchema.columns.Add(new ColumnSchemaModel {
                                            COLUMN_NAME = GenericController.getText(row["COLUMN_NAME"]).ToLowerInvariant(),
                                            DATA_TYPE = GenericController.getText(row["DATA_TYPE"]).ToLowerInvariant(),
                                            CHARACTER_MAXIMUM_LENGTH = GenericController.getInteger(row["CHARACTER_MAXIMUM_LENGTH"]),
                                            DATETIME_PRECISION = GenericController.getInteger(row["DATETIME_PRECISION"])
                                        });
                                    }
                                }
                                //
                                // Load the index schema
                                //
                                dt = core.db.getIndexSchemaData(TableName);
                                if (dt.Rows.Count > 0) {
                                    foreach (DataRow row in dt.Rows) {
                                        string index_keys = GenericController.getText(row["index_keys"]).ToLowerInvariant();
                                        tableSchema.indexes.Add(new IndexSchemaModel {
                                            index_name = GenericController.getText(row["INDEX_NAME"]).ToLowerInvariant(),
                                            index_keys = index_keys,
                                            indexKeyList = index_keys.Split(',').Select(s => s.Trim()).ToList()
                                        });
                                    }
                                }
                            }
                            if (!isInDb && isInCache) {
                                //
                                // -- not in db but in cache -- remove from cache
                                core.cacheRuntime.tableSchemaDictionary.Remove(lowerTablename);
                            } else if (!isInDb && !isInCache) {
                                //
                                // -- not in Db, not in cache -- do nothing
                            } else if (isInDb && !isInCache) {
                                //
                                // -- in Db but not in cache -- add it to cache
                                core.cacheRuntime.tableSchemaDictionary.Add(lowerTablename, tableSchema);
                            } else {
                                //
                                // -- in Db and in cache -- update cache because this was a .dirty cache problem
                                core.cacheRuntime.tableSchemaDictionary[lowerTablename] = tableSchema;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return tableSchema;
        }
        //
        //====================================================================================================
        public static void tableSchemaListClear(CoreController core) {
            core.cacheRuntime.tableSchemaDictionary.Clear();
        }
    }

}
