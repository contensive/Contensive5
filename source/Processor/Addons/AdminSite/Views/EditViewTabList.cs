
using Contensive.Processor.Controllers;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// Create tabs in edit view
    /// </summary>
    public static class EditViewTabList {
        //
        // ====================================================================================================
        /// <summary>
        /// Get tabs saved
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <param name="editorEnv"></param>
        /// <param name="RecordID"></param>
        /// <param name="ContentID"></param>
        /// <param name="EditTab"></param>
        /// <returns></returns>
        public static string getTab(CoreController core, AdminDataModel adminData, EditorEnvironmentModel editorEnv, int RecordID, int ContentID, string EditTab) {
            string returnHtml = "";
            int hint = 0;
            try {
                //
                // ----- Open the panel
                if (adminData.adminContent.fields.Count <= 0) {
                    hint = 10;
                    //
                    // There are no visible fiels, return empty
                    LogController.logError(core, new GenericException("There is no metadata for this field."));
                } else {
                    hint = 20;
                    //
                    // ----- Build an index to sort the fields by EditSortOrder
                    Dictionary<string, ContentFieldMetadataModel> sortingFields = new();
                    hint = 30;
                    foreach (var keyValuePair in adminData.adminContent.fields) {
                        hint = 40;
                        ContentFieldMetadataModel field = keyValuePair.Value;
                        hint = 50;
                        if (field.editTabName.ToLowerInvariant() == EditTab.ToLowerInvariant()) {
                            hint = 60;
                            if (AdminDataModel.isVisibleUserField(core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, adminData.adminContent.tableName)) {
                                hint = 70;
                                string AlphaSort = GenericController.getIntegerString(field.editSortPriority, 10) + "-" + GenericController.getIntegerString(field.id, 10);
                                hint = 80;
                                sortingFields.Add(AlphaSort, field);
                            }
                        }
                    }
                    hint = 90;
                    //
                    // ----- display the record fields
                    bool AllowHelpIcon = core.visitProperty.getBoolean("AllowHelpIcon");
                    StringBuilderLegacyController resultBody = new();
                    bool needUniqueEmailMessage = false;
                    hint = 100;
                    foreach (var kvp in sortingFields) {
                        hint = 110;
                        ContentFieldMetadataModel field = kvp.Value;
                        hint = 120;
                        string editorRow = EditorRowClass.getEditorRow(core, field, adminData, editorEnv);
                        hint = 130;
                        resultBody.add("<tr><td colspan=2>" + editorRow + "</td></tr>");
                    }
                    hint = 140;
                    //
                    // ----- add the *Required Fields footer
                    resultBody.add("<tr><td colspan=2 style=\"padding-top:10px;font-size:70%\"><div>* Field is required.</div><div>** Field must be unique.</div>");
                    if (needUniqueEmailMessage) {
                        resultBody.add("<div>*** Field must be unique because this site allows login by email.</div>");
                    }
                    resultBody.add("</td></tr>");
                    hint = 150;
                    //
                    // ----- close the panel
                    returnHtml = AdminUIController.getEditPanel(core, false, "", "", AdminUIController.editTable(resultBody.text));
                    adminData.editSectionPanelCount += 1;
                    resultBody = null;
                    hint = 160;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex, "hint [" + hint + "]");
                throw;
            }
            return returnHtml;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Create all the tabs for the edit form
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <param name="editTabs"></param>
        /// <param name="editorEnv"></param>
        public static void addContentTabs(CoreController core, AdminDataModel adminData, EditTabModel editTabs, EditorEnvironmentModel editorEnv) {
            try {
                // todo
                string IDList = "";
                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
                    IDList = IDList + "," + field.id;
                }
                if (!string.IsNullOrEmpty(IDList)) {
                    IDList = IDList.Substring(1);
                }
                //
                DataTable dt = core.db.executeQuery("select fieldid,helpdefault,helpcustom from ccfieldhelp where fieldid in (" + IDList + ") order by fieldid,id");
                string[,] fieldHelpArray = core.db.convertDataTabletoArray(dt);
                int HelpCnt = 0;
                int[] HelpIDCache = { };
                string[] helpDefaultCache = { };
                string[] HelpCustomCache = { };
                KeyPtrController helpIdIndex = new KeyPtrController();
                if (fieldHelpArray.GetLength(0) > 0) {
                    HelpCnt = fieldHelpArray.GetUpperBound(1) + 1;
                    HelpIDCache = new int[HelpCnt + 1];
                    helpDefaultCache = new string[HelpCnt + 1];
                    HelpCustomCache = new string[HelpCnt + 1];
                    int fieldId = -1;
                    int HelpPtr = 0;
                    for (HelpPtr = 0; HelpPtr < HelpCnt; HelpPtr++) {
                        fieldId = GenericController.encodeInteger(fieldHelpArray[0, HelpPtr]);
                        int LastFieldId = 0;
                        if (fieldId != LastFieldId) {
                            LastFieldId = fieldId;
                            HelpIDCache[HelpPtr] = fieldId;
                            helpIdIndex.setPtr(fieldId.ToString(), HelpPtr);
                            helpDefaultCache[HelpPtr] = GenericController.encodeText(fieldHelpArray[1, HelpPtr]);
                            HelpCustomCache[HelpPtr] = GenericController.encodeText(fieldHelpArray[2, HelpPtr]);
                        }
                    }
                    editorEnv.allowHelpMsgCustom = true;
                }
                //
                List<string> TabsFound = new List<string>();
                foreach (KeyValuePair<string, ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                    ContentFieldMetadataModel field = keyValuePair.Value;
                    if ((!field.editTabName.ToLowerInvariant().Equals("control info")) && (field.authorable) && (field.active) && (!TabsFound.Contains(field.editTabName.ToLowerInvariant()))) {
                        TabsFound.Add(field.editTabName.ToLowerInvariant());
                        string editTabCaption = field.editTabName;
                        if (string.IsNullOrEmpty(editTabCaption)) { editTabCaption = "Details"; }
                        string tabContent = getTab(core, adminData, editorEnv, adminData.editRecord.id, adminData.adminContent.id, field.editTabName);
                        if (!string.IsNullOrEmpty(tabContent)) {
                            addCustomTab(core, editTabs, editTabCaption, tabContent);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// add a tab to the list of tabs
        /// </summary>
        /// <param name="core"></param>
        /// <param name="editTabs"></param>
        /// <param name="Caption"></param>
        /// <param name="Content"></param>
        /// <returns></returns>
        public static void addCustomTab(CoreController core, EditTabModel editTabs, string Caption, string Content) {
            try {
                if (string.IsNullOrEmpty(Content)) { return; }
                editTabs.addEntry(Caption.Replace(" ", "&nbsp;"), "", "", Content, false, "ccAdminTab");
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
    }
}
