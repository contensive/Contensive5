using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers.EditControls {
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
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        public class GroupRuleEditorRowModel {
            public string idHidden;
            public string checkboxInput;
            public string radioInput;
            public string groupCaption;
            public string expiresInput;
            public string roleInput;
            public string relatedButtonList;
        }
        public class ExclusiveSetSectionModel {
            public string setLabel;
            public string noneRadioInput;
            public string hiddenFields;
            public string expiresInput;
            public string roleInput;
            public List<GroupRuleEditorRowModel> rowList;
        }
        public class GroupRuleEditorModel {
            public string listCaption;
            public string helpText;
            public bool hasExclusiveSets;
            public List<ExclusiveSetSectionModel> exclusiveSetList;
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
