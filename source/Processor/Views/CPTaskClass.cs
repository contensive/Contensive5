
using Contensive.BaseClasses;
using Contensive.BaseModels;
using Contensive.Models.Db;

namespace Contensive.Processor {
    /// <summary>
    /// Implements CPTaskBaseClass. Provides background task status queries.
    /// </summary>
    public class CPTaskClass : CPTaskBaseClass {
        //
        private readonly CPClass cp;
        //
        public CPTaskClass(CPClass cp) => this.cp = cp;
        //
        /// <summary>
        /// Get the status of a background task by its id.
        /// Returns null if the task id is not found.
        /// </summary>
        public override TaskStatusModel GetStatus(int taskId) {
            if (taskId <= 0) { return null; }
            var task = DbBaseModel.create<TaskModel>(cp, taskId);
            if (task == null) { return null; }
            return new TaskStatusModel {
                dateStarted = task.dateStarted,
                dateCompleted = task.dateCompleted,
                failMessage = task.failMessage,
                downloadPathFilename = task.downloadPathFilename
            };
        }
    }
}
