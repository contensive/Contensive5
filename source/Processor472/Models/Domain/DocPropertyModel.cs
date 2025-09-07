
//
using Contensive.Processor.Controllers;

namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// Objects contained in docuument properties
    /// </summary>
    public class DocPropertyModel {
        /// <summary>
        /// The name of hte property (key)
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// the value of the property, represented as a string
        /// </summary>
        public string value { get; set; }
        /// <summary>
        /// The name=value pair (denormalized data for performance only)
        /// </summary>
        public string nameValue { get; set; }
        /// <summary>
        /// for file types, the dosAbsPathfilename
        /// </summary>
        public string windowsTempfilename { get; set; }
        /// <summary>
        /// for file types, the size
        /// </summary>
        public int fileSize { get; set; }
        /// <summary>
        /// todo remote -- not used
        /// </summary>
        public string fileType { get; set; }
        /// <summary>
        /// type type of property, see DocPropertyTypesEnum
        /// </summary>
        public DocPropertyTypesEnum propertyType { get; set; }
        /// <summary>
        /// type of doc property
        /// </summary>
        public enum DocPropertyTypesEnum {
            /// <summary>
            /// read from input context, server variables (iis this is from the webserver)
            /// </summary>
            serverVariable = 1,
            /// <summary>
            /// read from input context, http headers
            /// </summary>
            header = 2,
            /// <summary>
            /// read from input context, decoded httpcontext form
            /// </summary>
            form = 3,
            /// <summary>
            /// read from input context, represents a file
            /// </summary>
            file = 4,
            /// <summary>
            /// read from input context, querystring
            /// </summary>
            queryString = 5,
            /// <summary>
            /// values set without context, within code
            /// </summary>
            userDefined = 6
        }
    }
}