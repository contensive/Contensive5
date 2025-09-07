
using System;
using System.Collections.Generic;

namespace Contensive.Models.Db {
    public class TaskModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("tasks", "cctasks", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// enum of all possible commands in task model
        /// </summary>
        public static class taskQueueCommandEnumModule {
            public const string runAddon = "runaddon";
        }
        /// <summary>
        /// model for cmdDetail field. Field contains a JSON serialization of this class
        /// </summary>
        public class CmdDetailClass {
            public int addonId;
            public string addonName;
            public Dictionary<string, string> args;
        }
        //
        //====================================================================================================
        /// <summary>
        /// JSON serialization of the cmdDetailClass containing information on how to run the task
        /// </summary>
        public string cmdDetail { get; set; }
        /// <summary>
        /// if non-0, the addon's result over-writes the content of the file referenced by the the download file-field. If this field is 0 the addon result goes to the filename field.
        /// These files are not deleted by housekeeping
        /// </summary>
        public int resultDownloadId { get; set; }
        /// <summary>
        /// if resultDownloadId is null or 0, and the addon return is not empty, the return is saved in a file referenced here.
        /// These files should be deleted in housekeep as the tasks are deleted.
        /// </summary>
        public DbBaseModel.FieldTypeTextFile filename { get; set; }
        /// <summary>
        /// datetime when the task is started
        /// </summary>
        public DateTime? dateStarted { get; set; }
        /// <summary>
        /// datetime when the task completes
        /// </summary>
        public DateTime? dateCompleted { get; set; }
        /// <summary>
        /// Timeout in sections for the task. zero means no timeout.
        /// </summary>
        public int timeout { get; set; }
        /// <summary>
        /// the guid of the runner that is assigned to handle this task. Empty if not assigned
        /// </summary>
        public string cmdRunner { get; set; }
    }
}
