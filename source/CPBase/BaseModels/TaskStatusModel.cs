
using System;

namespace Contensive.BaseModels {
    /// <summary>
    /// Status of a background task. State is derived from datetime fields and failMessage.
    /// Queued: dateStarted null. Running: dateStarted set, dateCompleted null.
    /// Completed (success): dateCompleted set, failMessage null.
    /// Completed (failed): dateCompleted set, failMessage set.
    /// </summary>
    public class TaskStatusModel {
        /// <summary>
        /// when the task started executing. null = queued
        /// </summary>
        public DateTime? dateStarted { get; set; }
        /// <summary>
        /// when the task completed. null = not yet completed
        /// </summary>
        public DateTime? dateCompleted { get; set; }
        /// <summary>
        /// error message if the task failed. null = success (when dateCompleted is set)
        /// </summary>
        public string failMessage { get; set; }
        /// <summary>
        /// cdn path/filename for a downloadable result file, if applicable
        /// </summary>
        public string downloadPathFilename { get; set; }
        //
        // -- derived convenience properties
        //
        /// <summary>
        /// true if the task is queued and has not started
        /// </summary>
        public bool isQueued => dateStarted == null;
        /// <summary>
        /// true if the task is currently running
        /// </summary>
        public bool isRunning => dateStarted != null && dateCompleted == null;
        /// <summary>
        /// true if the task completed successfully
        /// </summary>
        public bool isCompleted => dateCompleted != null && string.IsNullOrEmpty(failMessage);
        /// <summary>
        /// true if the task completed with a failure
        /// </summary>
        public bool isFailed => dateCompleted != null && !string.IsNullOrEmpty(failMessage);
    }
}
