
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Manage layout records
    /// </summary>
    public abstract class CPLayoutBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Returns the html layout field of a layout record
        /// </summary>
        /// <param name="layoutName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetLayoutByName(string layoutName);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the html layout field of a layout record. If the record does not exist, it is created with the supplied default layout
        /// </summary>
        /// <param name="layoutName"></param>
        /// <param name="defaultLayout"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetLayoutByName(string layoutName, string defaultLayout);
        //
        //====================================================================================================
        /// <summary>
        /// get the layout by its id. If not found, returns blank. To determine record status use Layout Model instead.
        /// </summary>
        /// <param name="layoutId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetLayout(int layoutId);
        //
        //====================================================================================================
        /// <summary>
        /// get the layout by its guid. If not found, returns blank. To determine record status use Layout Model instead.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <returns></returns>
        public abstract string GetLayout(string layoutGuid);
        //
        //====================================================================================================
        /// <summary>
        /// returns a layout by Guid. If missing, the layout record is created with the default name and the layout file referenced in the cdn
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <returns></returns>
        public abstract string GetLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutCdnPathFilename);
    }
}
