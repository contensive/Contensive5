
using Contensive.BaseModels;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Background task management: queue tasks, check status.
    /// Access through cp.Task.
    /// </summary>
    public abstract class CPTaskBaseClass {
        /// <summary>
        /// Get the status of a background task by its id.
        /// Returns null if the task id is not found.
        /// </summary>
        public abstract TaskStatusModel GetStatus(int taskId);
    }
}
