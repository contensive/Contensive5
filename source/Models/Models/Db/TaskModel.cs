
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
        /// Legacy field, no longer used. The addon result file is stored via downloadPathFilename instead.
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
        /// <summary>
        /// error/failure message. Null means success when dateCompleted is set.
        /// </summary>
        public string failMessage { get; set; }
        /// <summary>
        /// cdn path/filename for a downloadable result file, if applicable
        /// </summary>
        public string downloadPathFilename { get; set; }
        /// <summary>
        /// id of the user who requested the task
        /// </summary>
        public int requestedBy { get; set; }
    }
}
