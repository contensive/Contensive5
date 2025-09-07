
using System;

namespace Contensive.Models.Db {
    //
    public class ImportWizardTaskModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Import Wizard Tasks", "importWizardTasks", "default", false);
        //
        //====================================================================================================
        //
        public DateTime dateCompleted { get; set; }
        public DateTime dateStarted { get; set; }
        public string importMapFilename { get; set; }
        public string notifyEmail { get; set; }
        public string resultMessage { get; set; }
        public string uploadFilename { get; set; }
    }
}
