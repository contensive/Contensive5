
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.AdminSite {
    public class GroupRuleEditor {
        //
        //========================================================================
        //
        public static string get(CoreController core, AdminDataModel adminData) {
            string result = null;
            try {
                string editorRow = "";
                editorRow = AdminUIEditorController.getGroupRuleEditor(core, adminData);
                result = AdminUIController.getEditPanel(core, true, "Group Membership", "", editorRow);
                adminData.editSectionPanelCount += 1;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        public class GroupRuleEditorRowModel {
            public string idHidden;
            public string checkboxInput;
            public string groupCaption;
            public string expiresInput;
            public string roleInput;
            public string relatedButtonList;
        }
        public class GroupRuleEditorModel {
            public string listCaption;
            public string helpText;
            public List<GroupRuleEditorRowModel> rowList;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
