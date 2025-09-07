
using Contensive.CPBase.BaseModels;
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Models.Domain {
    public class LastestDateTracker : LastestDateTrackerBaseModel {
        //
        private DateTime latestDate = DateTime.MinValue;
        //
        //====================================================================================================
        //
        public override DateTime Get() {
            return latestDate;
        }

        public override void Reset() {
            latestDate = DateTime.MinValue;
        }

        public override void Track(DateTime? modifiedDate) {
            int hint = 1;
            try {
                if (modifiedDate == null) { return; }
                DateTime workingDate = (DateTime)modifiedDate;
                latestDate = (workingDate.CompareTo(latestDate).Equals(1)) ? workingDate : latestDate;
            } catch (Exception ex) {
                throw new InvalidOperationException("hint [" + hint + "]", ex);
            }
        }
    }
}