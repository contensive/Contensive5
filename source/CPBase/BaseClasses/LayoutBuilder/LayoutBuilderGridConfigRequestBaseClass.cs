//using System;
//using System.Collections.Generic;

//namespace Contensive.BaseClasses.LayoutBuilder {
//    /// <summary>
//    /// layout split right and left, right larger
//    /// </summary>
//    public abstract class LayoutBuilderGridConfigRequestBaseClass {
//        /// <summary>
//        /// The number of rows per page to display by default
//        /// </summary>
//        public abstract int defaultRecordsPerPage { get; set; }
//        /// <summary>
//        /// grid customizations can be made by users, and are stored in either user properties or visit properties. This prefix is used to identify the properties.
//        /// This string is the cache name that identifies this use of the grid system. 
//        /// For example, if this grid system is for the account list, the gridSavePrefix might the "AccountList"
//        /// </summary>
//        public abstract string gridPropertiesSaveName { get; set; }
//        /// <summary>
//        /// The list of fields that can be sorted by the user
//        /// </summary>
//        public abstract List<string> sortableFields { get; set; }

//    }
//}
