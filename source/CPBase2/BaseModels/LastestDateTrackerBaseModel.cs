
using System;

namespace Contensive.CPBase.BaseModels {
    //
    //====================================================================================================
    /// <summary>
    /// Track a series of dates, saving only the latest
    /// </summary>
    public abstract class LastestDateTrackerBaseModel {
        /// <summary>
        /// Reset the Modified Date. Call before tracking the modified date.
        /// </summary>
        public abstract void Reset();
        /// <summary>
        /// include this modified date and save if it is the latest
        /// </summary>
        /// <param name="ModifiedDate"></param>
        public abstract void Track(DateTime? ModifiedDate);
        /// <summary>
        /// Get the latest date added to the tracker since the last reset
        /// </summary>
        /// <returns></returns>
        public abstract DateTime Get();
    }
}
