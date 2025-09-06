
using System.Collections.Generic;

namespace Contensive.CPBase.BaseModels {
    /// <summary>
    /// Attributes avalable for all html5 elements
    /// </summary>
    public partial class HtmlAttributesGlobal {
        /// <summary>
        /// Specifies a shortcut key to activate/focus an element
        /// </summary>
        public string accesskey { get; set; }
        /// <summary>
        /// Specifies one or more classnames for an element (refers to a class in a style sheet)
        /// </summary>
        public string @class { get; set; }
        /// <summary>
        /// Specifies whether the content of an element is editable or not
        /// </summary>
        public bool contenteditable { get; set; }
        /// <summary>
        /// Used to store custom data private to the page or application
        /// </summary>
        public List<KeyValuePair<string, string>> data { get; set; }
        /// <summary>
        /// Specifies the text direction for the content in an element
        /// </summary>
        public string dir { get; set; }
        /// <summary>
        /// Specifies whether an element is draggable or not
        /// </summary>
        public bool draggable { get; set; }
        /// <summary>
        /// Specifies whether the dragged data is copied, moved, or linked, when dropped
        /// </summary>
        public string dropzone { get; set; }
        /// <summary>
        /// Specifies that an element is not yet, or is no longer, relevant
        /// </summary>
        public bool hidden { get; set; }
        /// <summary>
        /// Specifies a unique id for an element
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// Specifies the language of the element's content
        /// </summary>
        public string lang { get; set; }
        /// <summary>
        /// Specifies whether the element is to have its spelling and grammar checked or not
        /// </summary>
        public bool spellcheck { get; set; }
        /// <summary>
        /// Specifies an inline CSS style for an element
        /// </summary>
        public string style { get; set; }
        /// <summary>
        /// Specifies the tabbing order of an element
        /// </summary>
        public string tabindex { get; set; }
        /// <summary>
        /// Specifies extra information about an element
        /// </summary>
        public string title { get; set; }
        /// <summary>
        /// Specifies whether the content of an element should be translated or not
        /// </summary>
        public bool translate { get; set; }
    }
}
