using Contensive.BaseClasses;
using Contensive.Processor.Models.Domain.EditControl;
using NLog;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.EditControls {
    //
    //====================================================================================================
    /// <summary>
    /// endpoint for edit control autocomplete
    /// uses jquery autocomplete
    /// Expects 2 request parameters:
    /// - token: an encrypted object that includes the autocomplete's settings
    /// - term: the term to search for
    /// </summary>
    public class EditControlAutocompleteRemote : AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// entry point for addon
        /// </summary>
        /// <param name="cpBase"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cpBase) {
            CPClass cp = (CPClass)cpBase;
            var result = new List<ResponseEditControlAutocompleteModel>();
            try {
                //
                // -- security is provided by the encrypted token, so no authentication is required, letting this editcontrl be used in public forms
                //
                // -- token passes from the control is an encrypted object that includes the autocomplete's settings
                string encryptedToken = cp.Doc.GetText("token");
                string settingsJson = cp.Security.DecryptTwoWay(encryptedToken);
                EditControlAutocompleteSettingsModel settings = cp.JSON.Deserialize<EditControlAutocompleteSettingsModel>(settingsJson);
                //
                // -- add content links
                string filterTerm = cp.Doc.GetText("term");
                if (string.IsNullOrEmpty(filterTerm)) { return result; }
                //
                string nameField = string.IsNullOrEmpty(settings.nameField) ? "name" : settings.nameField;
                string sqlcriteria = string.IsNullOrEmpty(settings.sqlCriteria) ? "" : $"({settings.sqlCriteria})and";
                sqlcriteria = $"{sqlcriteria}({nameField} like {cp.Db.EncodeSQLTextLike(filterTerm)})";
                using (CPCSBaseClass cs = cp.CSNew()) {
                    string idField = string.IsNullOrEmpty(settings.idField) ? "id" : settings.idField;
                    string sortFieldList = string.IsNullOrEmpty(settings.sortFieldList) ? $"{nameField},{idField}" : settings.sortFieldList;
                    if (cs.Open(settings.content, sqlcriteria, sortFieldList, true, $"{nameField},{idField}")) {
                        do {
                            result.Add(new ResponseEditControlAutocompleteModel {
                                label = cs.GetText(nameField),
                                value = cs.GetText(idField)
                            });
                            cs.GoNext();
                        } while (cs.OK());
                    }
                }
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
    }
    //
    // -- responseModel
    internal class ResponseEditControlAutocompleteModel {
        public string label { get; set; }
        public string value { get; set; }
        public string url { get; set; }
        public string type { get; set; }
    }
}
