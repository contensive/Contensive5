﻿
namespace Contensive.CPBase.BaseModels {
    /// <summary>
    /// Attributes for html form
    /// </summary>
    public class HtmlAttributesForm : HtmlAttributesGlobal {
        /// <summary>
        /// Specifies the character encodings that are to be used for the form submission. A space-separated list of one or more character encodings that are to be used for the form submission.
        /// Common values: UTF-8 - Character encoding for Unicode, ISO-8859-1 - Character encoding for the Latin alphabet.
        /// </summary>
        public string acceptcharset;
        /// <summary>
        /// Specifies where to send the form-data when a form is submitted
        /// </summary>
        public string action;
        /// <summary>
        /// Specifies whether a form should have autocomplete on or off
        /// </summary>
        public bool autocomplete;
        /// <summary>
        /// Specifies how the form-data should be encoded when submitting it to the server (only for method="post")
        /// </summary>
        public HtmlEncTypeEnum enctype;
        /// <summary>
        /// Specifies the HTTP method to use when sending form-data
        /// </summary>
        public HtmlMethodEnum method;
        /// <summary>
        /// Specifies the name of a form
        /// </summary>
        public string name;
        /// <summary>
        /// Specifies that the form should not be validated when submitted
        /// </summary>
        public bool novalidate;
        /// <summary>
        /// Specifies where to display the response that is received after submitting the form
        /// </summary>
        public HtmlAttributeTarget target;
        /// <summary>
        /// possible values for html attribute target
        /// </summary>
        public enum HtmlAttributeTarget {
            /// <summary>
            /// 
            /// </summary>
            none,
            /// <summary>
            /// 
            /// </summary>
            _blank,
            /// <summary>
            /// 
            /// </summary>
            _self,
            /// <summary>
            /// 
            /// </summary>
            _parent,
            /// <summary>
            /// 
            /// </summary>
            _top
        }
        /// <summary>
        /// values for html form encodetype
        /// </summary>
        public enum HtmlEncTypeEnum {
            /// <summary>
            /// 
            /// </summary>
            none,
            /// <summary>
            /// 
            /// </summary>
            application_x_www_form_urlencoded,
            /// <summary>
            /// 
            /// </summary>
            multipart_form_data,
            /// <summary>
            /// 
            /// </summary>
            text_plain
        }
        /// <summary>
        /// values for html form method
        /// </summary>
        public enum HtmlMethodEnum {
            /// <summary>
            /// 
            /// </summary>
            post,
            /// <summary>
            /// 
            /// </summary>
            get
        }
    }
}
