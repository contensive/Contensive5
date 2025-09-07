
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// This model represents the basic fields used in every Contensive table. 
    /// It is the base model for both the GenericModel (when you supply the tablename) and all the specific Db Models
    /// </summary>
    //
    public class DbBaseFieldsModel {
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
        public int contentControlId { get; set; }
        /// <summary>
        /// foreign key to ccmembers table, populated by admin when record added.
        /// </summary>
        public int? createdBy { get; set; }
        /// <summary>
        /// used when creating new record
        /// </summary>
        public int createKey { get; set; }
        /// <summary>
        /// date record added, populated by admin when record added.
        /// </summary>
        public DateTime? dateAdded { get; set; }
        /// <summary>
        /// foreign key to ccmembers table set to user who modified the record last in the admin site
        /// </summary>
        public int? modifiedBy { get; set; }
        /// <summary>
        /// date when the record was last modified in the admin site
        /// </summary>
        public DateTime? modifiedDate { get; set; }
        /// <summary>
        /// optionally used to sort recrods in the table
        /// </summary>
        public string sortOrder { get; set; }
    }
}
