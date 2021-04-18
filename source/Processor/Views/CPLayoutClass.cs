
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor {
    //
    //====================================================================================================
    /// <summary>
    /// Manage Layouts
    /// </summary>
    public class CPLayoutClass : BaseClasses.CPLayoutBaseClass {
        //
        private CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPLayoutClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get layout field of layout record. Return empty string if not found.
        /// </summary>
        /// <param name="layoutid"></param>
        /// <returns></returns>
        public override string GetLayout(int layoutid) {
            return LayoutController.getLayout(cp, layoutid);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get layout field of layout record. Return empty string if not found.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <returns></returns>
        public override string GetLayout(string layoutGuid) {
            return LayoutController.getLayout(cp, layoutGuid);
        }
        //
        //====================================================================================================
        /// <summary>
        /// get a layout  from the layout record, create the record from layoutCdnPathFilename if invalid
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <returns></returns>
        public override string GetLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutCdnPathFilename) {
            return LayoutController.getLayout(cp, layoutGuid, defaultLayoutName, defaultLayoutCdnPathFilename);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a layout by its name. If there are duplicates, return the first by id. Not recommeded, use Guid. For compatibility only
        /// </summary>
        /// <param name="layoutName"></param>
        /// <returns></returns>
        public override string GetLayoutByName(string layoutName) {
            return LayoutController.getLayoutByName(cp, layoutName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a layout by its name. If there are duplicates, return the first by id. Not recommeded, use Guid. For compatibility only
        /// </summary>
        /// <param name="layoutName"></param>
        /// <param name="defaultLayout"></param>
        /// <returns></returns>
        public override string GetLayoutByName(string layoutName, string defaultLayout) {
            return LayoutController.getLayoutByName(cp, layoutName, defaultLayout);
        }
    }
}