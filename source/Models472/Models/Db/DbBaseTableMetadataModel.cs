using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Models.Db {
    //
    //====================================================================================================
    // each inheriting class has to declare a static constructor for these fields
    // there are fields so they can be destinguished from the properties
    // should be a single static readonly field of a class that contains these public fields
    //
    public class DbBaseTableMetadataModel {
        public string contentName { get; }
        public string tableNameLower { get; }
        public string dataSourceName { get; }
        public bool nameFieldIsUnique { get; }
        //
        public DbBaseTableMetadataModel(string contentName, string tableName, string dataSource, bool nameFieldIsUnique) {
            this.contentName = contentName;
            this.tableNameLower = tableName.ToLower();
            this.dataSourceName = dataSource;
            this.nameFieldIsUnique = nameFieldIsUnique;
        }
        //
        public DbBaseTableMetadataModel(string contentName, string tableName, string dataSource) {
            this.contentName = contentName;
            this.tableNameLower = tableName.ToLower();
            this.dataSourceName = dataSource;
            this.nameFieldIsUnique = false;
        }
        //
        public DbBaseTableMetadataModel(string contentName, string tableName) {
            this.contentName = contentName;
            this.tableNameLower = tableName.ToLower();
            this.dataSourceName = "default";
            this.nameFieldIsUnique = false;
        }
        //
        public DbBaseTableMetadataModel(string tableName) {
            this.contentName = tableName.ToLower();
            this.tableNameLower = tableName.ToLower();
            this.dataSourceName = "default";
            this.nameFieldIsUnique = false;
        }
    }

}
