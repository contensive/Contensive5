using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Processor.Models.Domain.EditControl {
    /// <summary>
    /// model used to hold the autocomplete edit-control settings in the UI, passed back to the server tokenized for the end point
    /// </summary>
    internal class EditControlAutocompleteSettingsModel {
        /// <summary>
        /// the content to search in
        /// </summary>
        public string content { get; set; }
        /// <summary>
        /// the sqlCriteria to apply to the content for filtering.
        /// IMPORTANT: it must include the term filter, where the trm will be find-replaced for the sting '{term}'
        /// </summary>
        public string sqlCriteria { get; set; }
        /// <summary>
        /// when creating the select lookup list, this is the name of the field in the 'content' that is used for the label, visible to users. The default is name, if this is blank.
        /// </summary>
        public string nameField { get; set; }
        /// <summary>
        /// when creating the select lookup list, this is the name of the field in the 'content' that is used for the value, not visible to users. The default is id if this is blank.
        /// </summary>
        public string idField { get; set; }
        /// <summary>
        /// when creating the select lookup list, this is the list of fields used to sort the result. The default is nameField,idField if this is blank.
        /// </summary>
        public string sortFieldList { get; set; }   

    }
}
