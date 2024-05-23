
using System;
using System.Collections.Generic;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Controllers;
using Contensive.BaseClasses;
using Contensive.Models.Db;
using NLog;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class ConfigureEditClass : AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get((CPClass)cpBase);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get the Configure Edit
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static string get(CPClass cp) {
            CoreController core = cp.core;
            try {
                KeyPtrController Index = new KeyPtrController();
                int ContentId = cp.Doc.GetInteger(RequestNameToolContentId);
                var contentMetadata = ContentMetadataModel.create(core, ContentId, true, true);
                int RecordCount = 0;
                int formFieldId = 0;
                string StatusMessage = "";
                string ErrorMessage = "";
                bool ReloadCDef = cp.Doc.GetBoolean("ReloadCDef");
                if (contentMetadata != null) {
                    string ToolButton = cp.Doc.GetText("Button");
                    //
                    if (!string.IsNullOrEmpty(ToolButton)) {
                        bool AllowContentAutoLoad = false;
                        if (ToolButton != ButtonCancel) {
                            //
                            // Save the form changes
                            //
                            AllowContentAutoLoad = cp.Site.GetBoolean("AllowContentAutoLoad", true);
                            cp.Site.SetProperty("AllowContentAutoLoad", "false");
                            //
                            // ----- Save the input
                            //
                            RecordCount = GenericController.encodeInteger(cp.Doc.GetInteger("dtfaRecordCount"));
                            if (RecordCount > 0) {
                                int RecordPointer = 0;
                                for (RecordPointer = 0; RecordPointer < RecordCount; RecordPointer++) {
                                    //
                                    string formFieldName = cp.Doc.GetText("dtfaName." + RecordPointer);
                                    CPContentBaseClass.FieldTypeIdEnum formFieldTypeId = (CPContentBaseClass.FieldTypeIdEnum)cp.Doc.GetInteger("dtfaType." + RecordPointer);
                                    formFieldId = GenericController.encodeInteger(cp.Doc.GetInteger("dtfaID." + RecordPointer));
                                    bool formFieldInherited = cp.Doc.GetBoolean("dtfaInherited." + RecordPointer);
                                    //
                                    // problem - looking for the name in the Db using the form's name, but it could have changed.
                                    // have to look field up by id
                                    //
                                    foreach (KeyValuePair<string, Processor.Models.Domain.ContentFieldMetadataModel> cdefFieldKvp in contentMetadata.fields) {
                                        if (cdefFieldKvp.Value.id == formFieldId) {
                                            //
                                            // Field was found in CDef
                                            //
                                            if (cdefFieldKvp.Value.inherited && (!formFieldInherited)) {
                                                //
                                                // Was inherited, but make a copy of the field
                                                //
                                                using (var CSTarget = new CsModel(core)) {
                                                    if (CSTarget.insert("Content Fields")) {
                                                        using (var CSSource = new CsModel(core)) {
                                                            if (CSSource.openRecord("Content Fields", formFieldId)) { CSSource.copyRecord(CSTarget); }
                                                        }
                                                        formFieldId = CSTarget.getInteger("ID");
                                                        CSTarget.set("ContentID", ContentId);
                                                    }
                                                    CSTarget.close();
                                                }
                                                ReloadCDef = true;
                                            } else if ((!cdefFieldKvp.Value.inherited) && (formFieldInherited)) {
                                                //
                                                // Was a field, make it inherit from it's parent
                                                MetadataController.deleteContentRecord(core, "Content Fields", formFieldId);
                                                ReloadCDef = true;
                                            } else if ((!cdefFieldKvp.Value.inherited) && (!formFieldInherited)) {
                                                //
                                                // not inherited, save the field values and mark for a reload
                                                //
                                                {
                                                    if (formFieldName.IndexOf(" ") != -1) {
                                                        //
                                                        // remoave spaces from new name
                                                        //
                                                        StatusMessage = StatusMessage + "<LI>Field [" + formFieldName + "] was renamed [" + GenericController.strReplace(formFieldName, " ", "") + "] because the field name can not include spaces.</LI>";
                                                        formFieldName = GenericController.strReplace(formFieldName, " ", "");
                                                    }
                                                    //
                                                    string SQL = null;
                                                    //
                                                    if ((!string.IsNullOrEmpty(formFieldName)) && ((int)formFieldTypeId != 0) && ((cdefFieldKvp.Value.nameLc == "") || (cdefFieldKvp.Value.fieldTypeId == 0))) {
                                                        //
                                                        // Create Db field, Field is good but was not before
                                                        //
                                                        core.db.createSQLTableField(contentMetadata.tableName, formFieldName, formFieldTypeId);
                                                        StatusMessage = StatusMessage + "<LI>Field [" + formFieldName + "] was saved to this content definition and a database field was created in [" + contentMetadata.tableName + "].</LI>";
                                                    } else if ((string.IsNullOrEmpty(formFieldName)) || ((int)formFieldTypeId == 0)) {
                                                        //
                                                        // name blank or type=0 - do nothing but tell them
                                                        //
                                                        if (string.IsNullOrEmpty(formFieldName) && ((int)formFieldTypeId == 0)) {
                                                            ErrorMessage += "<LI>Field number " + (RecordPointer + 1) + " was saved to this content definition but no database field was created because a name and field type are required.</LI>";
                                                        } else if (formFieldName == "unnamedfield" + formFieldId.ToString()) {
                                                            ErrorMessage += "<LI>Field number " + (RecordPointer + 1) + " was saved to this content definition but no database field was created because a field name is required.</LI>";
                                                        } else {
                                                            ErrorMessage += "<LI>Field [" + formFieldName + "] was saved to this content definition but no database field was created because a field type are required.</LI>";
                                                        }
                                                    } else if ((formFieldName == cdefFieldKvp.Value.nameLc) && (formFieldTypeId != cdefFieldKvp.Value.fieldTypeId)) {
                                                        //
                                                        // Field Type changed, must be done manually
                                                        //
                                                        ErrorMessage += "<LI>Field [" + formFieldName + "] changed type from [" + DbBaseModel.getRecordName<ContentFieldTypeModel>(core.cpParent, (int)cdefFieldKvp.Value.fieldTypeId) + "] to [" + DbBaseModel.getRecordName<ContentFieldTypeModel>(core.cpParent, (int)formFieldTypeId) + "]. This may have caused a problem converting content.</LI>";
                                                        int DataSourceTypeId = core.db.getDataSourceType();
                                                        switch (DataSourceTypeId) {
                                                            case DataSourceTypeODBCMySQL:
                                                                SQL = "alter table " + contentMetadata.tableName + " change " + cdefFieldKvp.Value.nameLc + " " + cdefFieldKvp.Value.nameLc + " " + core.db.getSQLAlterColumnType(formFieldTypeId) + ";";
                                                                break;
                                                            default:
                                                                SQL = "alter table " + contentMetadata.tableName + " alter column " + cdefFieldKvp.Value.nameLc + " " + core.db.getSQLAlterColumnType(formFieldTypeId) + ";";
                                                                break;
                                                        }
                                                        core.db.executeNonQuery(SQL);
                                                    }
                                                    SQL = "Update ccFields"
                                                        + " Set name=" + DbController.encodeSQLText(formFieldName)
                                                        + ",type=" + (int)formFieldTypeId
                                                        + ",caption=" + DbController.encodeSQLText(cp.Doc.GetText("dtfaCaption." + RecordPointer))
                                                        + ",DefaultValue=" + DbController.encodeSQLText(cp.Doc.GetText("dtfaDefaultValue." + RecordPointer))
                                                        + ",EditSortPriority=" + DbController.encodeSQLText(GenericController.encodeText(cp.Doc.GetInteger("dtfaEditSortPriority." + RecordPointer)))
                                                        + ",Active=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaActive." + RecordPointer))
                                                        + ",ReadOnly=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaReadOnly." + RecordPointer))
                                                        + ",Authorable=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaAuthorable." + RecordPointer))
                                                        + ",Required=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaRequired." + RecordPointer))
                                                        + ",UniqueName=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaUniqueName." + RecordPointer))
                                                        + ",TextBuffered=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaTextBuffered." + RecordPointer))
                                                        + ",Password=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaPassword." + RecordPointer))
                                                        + ",HTMLContent=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaHTMLContent." + RecordPointer))
                                                        + ",EditTab=" + DbController.encodeSQLText(cp.Doc.GetText("dtfaEditTab." + RecordPointer))
                                                        + ",Scramble=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaScramble." + RecordPointer)) + "";
                                                    if (core.session.isAuthenticatedAdmin()) {
                                                        SQL += ",adminonly=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaAdminOnly." + RecordPointer));
                                                    }
                                                    if (core.session.isAuthenticatedDeveloper()) {
                                                        SQL += ",DeveloperOnly=" + DbController.encodeSQLBoolean(cp.Doc.GetBoolean("dtfaDeveloperOnly." + RecordPointer));
                                                    }
                                                    SQL += " where ID=" + formFieldId;
                                                    core.db.executeNonQuery(SQL);
                                                    ReloadCDef = true;
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                            core.cache.invalidateAll();
                            core.cacheRuntime.clear();
                        }
                        if (ToolButton == ButtonAdd) {
                            //
                            // ----- Insert a blank Field
                            var defaultValues = ContentMetadataModel.getDefaultValueDict(core, ContentFieldModel.tableMetadata.contentName);
                            var field = ContentFieldModel.addDefault<ContentFieldModel>(core.cpParent, defaultValues);
                            field.name = "unnamedField" + field.id.ToString();
                            field.contentId = ContentId;
                            field.editSortPriority = 0;
                            field.save(core.cpParent);
                            ReloadCDef = true;
                        }
                        //
                        // ----- Button Reload CDef
                        if (ToolButton == ButtonSaveandInvalidateCache) {
                            core.cache.invalidateAll();
                            core.cacheRuntime.clear();
                        }
                        //
                        // ----- Restore Content Autoload site property
                        //
                        if (AllowContentAutoLoad) {
                            cp.Site.SetProperty("AllowContentAutoLoad", AllowContentAutoLoad.ToString());
                        }
                        //
                        // ----- Cancel or Save, reload CDef and go
                        //
                        if ((ToolButton == ButtonCancel) || (ToolButton == ButtonOK)) {
                            //
                            // ----- Exit back to menu
                            //
                            return core.webServer.redirect(core.appConfig.adminRoute, "Tool-ConfigureContentEdit, ok or cancel button, go to root.");
                        }
                    }
                }
                //
                //   Print Output
                string description = ""
                    + Controllers.HtmlController.p( "Use this tool to add or modify content definition fields and the underlying sql table fields.")
                    + ((ContentId.Equals(0)) ? "" : ""
                        + Controllers.HtmlController.ul(""
                            + Controllers.HtmlController.li(Controllers.HtmlController.a("Edit Content", "?aa=0&cid=3&id=" + ContentId + "&tx=&ad=0&asf=1&af=4", "nav-link btn btn-primary"), "nav-item mr-1 me-1")
                            + Controllers.HtmlController.li(Controllers.HtmlController.a("Edit Records", "?cid=" + ContentId, "nav-link btn btn-primary"), "nav-item mr-1 me-1")
                            + Controllers.HtmlController.li(Controllers.HtmlController.a("Select Different Fields", "?af=105", "nav-link btn btn-primary"), "nav-item mr-1 me-1")
                            , "nav")
                    );
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                Stream.add(AdminUIController.getHeaderTitleDescription("Edit Table Metadata", description));
                //
                // -- status of last operation
                if (!string.IsNullOrEmpty(StatusMessage)) {
                    Stream.add(AdminUIController.getToolFormRow(core, "<UL>" + StatusMessage + "</UL>"));
                }
                //
                // -- errors with last operations
                if (!string.IsNullOrEmpty(ErrorMessage)) {
                    Stream.add(Controllers.HtmlController.div("There was a problem saving these changes" + "<UL>" + ErrorMessage + "</UL>", "ccError"));
                }
                if (ReloadCDef) {
                    contentMetadata = Processor.Models.Domain.ContentMetadataModel.create(core, ContentId, true, true);
                }
                string ButtonList = ButtonCancel + "," + ButtonSelect;
                if (contentMetadata == null) {
                    //
                    // content tables that have edit forms to Configure
                    bool isEmptyList = false;
                    Stream.add(AdminUIController.getToolFormInputRow(core, "Select a Content Definition to Configure", AdminUIEditorController.getLookupContentEditor(core, RequestNameToolContentId, ContentId, ContentMetadataModel.getContentId(core, "Content"), ref isEmptyList, false, "", "", false, "")));
                } else {
                    //
                    // Configure edit form
                    Stream.add(Controllers.HtmlController.inputHidden(RequestNameToolContentId, ContentId));
                    Stream.add(core.html.getPanelTop());
                    ButtonList = ButtonCancel + "," + ButtonSave + "," + ButtonOK + "," + ButtonAdd;
                    //
                    // Get a new copy of the content definition
                    //
                    Stream.add(SpanClassAdminNormal + "<P><B>" + contentMetadata.name + "</b></P>");
                    Stream.add("<table border=\"0\" cellpadding=\"1\" cellspacing=\"1\" width=\"100%\">");
                    //
                    int ParentContentId = contentMetadata.parentId;
                    bool AllowCDefInherit = false;
                    Processor.Models.Domain.ContentMetadataModel ParentCDef = null;
                    if (ParentContentId == -1) {
                        AllowCDefInherit = false;
                    } else {
                        AllowCDefInherit = true;
                        string ParentContentName = MetadataController.getContentNameByID(core, ParentContentId);
                        ParentCDef = Processor.Models.Domain.ContentMetadataModel.create(core, ParentContentId, true, true);
                    }
                    bool NeedFootNote1 = false;
                    bool NeedFootNote2 = false;
                    if (contentMetadata.fields.Count > 0) {
                        //
                        // -- header row
                        Stream.add("<tr>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"center\"></td>");
                        if (!AllowCDefInherit) {
                            Stream.add("<td valign=\"bottom\" width=\"100\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b><br>Inherited*</b></span></td>");
                            NeedFootNote1 = true;
                        } else {
                            Stream.add("<td valign=\"bottom\" width=\"100\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b><br>Inherited</b></span></td>");
                        }
                        Stream.add("<td valign=\"bottom\" width=\"100\" class=\"ccPanelInput\" align=\"left\">" + SpanClassAdminSmall + "<b><br>Field</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"100\" class=\"ccPanelInput\" align=\"left\">" + SpanClassAdminSmall + "<b><br>Caption</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"100\" class=\"ccPanelInput\" align=\"left\">" + SpanClassAdminSmall + "<b><br>Edit Tab</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"100\" class=\"ccPanelInput\" align=\"left\">" + SpanClassAdminSmall + "<b><br>Default</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"left\">" + SpanClassAdminSmall + "<b><br>Type</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"left\">" + SpanClassAdminSmall + "<b>Edit<br>Order</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b><br>Active</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b>Read<br>Only</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b><br>Auth</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b><br>Req</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b><br>Unique</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b>Text<br>Buffer</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b><br>Pass</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "<b>Text<br>Scrm</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"left\">" + SpanClassAdminSmall + "<b><br>HTML</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"left\">" + SpanClassAdminSmall + "<b>Admin<br>Only</b></span></td>");
                        Stream.add("<td valign=\"bottom\" width=\"50\" class=\"ccPanelInput\" align=\"left\">" + SpanClassAdminSmall + "<b>Dev<br>Only</b></span></td>");
                        Stream.add("</tr>");
                        RecordCount = 0;
                        //
                        // Build a select template for Type
                        //
                        string TypeSelectTemplate = core.html.selectFromContent("menuname", -1, "Content Field Types", "", "unknown");
                        //
                        // Index the sort order
                        //
                        List<FieldSortClass> fieldList = new List<FieldSortClass>();
                        int FieldCount = contentMetadata.fields.Count;
                        foreach (var keyValuePair in contentMetadata.fields) {
                            FieldSortClass fieldSort = new FieldSortClass();
                            string sortOrder = "";
                            fieldSort.field = keyValuePair.Value;
                            sortOrder = "";
                            if (fieldSort.field.active) {
                                sortOrder += "0";
                            } else {
                                sortOrder += "1";
                            }
                            if (fieldSort.field.authorable) {
                                sortOrder += "0";
                            } else {
                                sortOrder += "1";
                            }
                            sortOrder += fieldSort.field.editTabName + getIntegerString(fieldSort.field.editSortPriority, 10) + getIntegerString(fieldSort.field.id, 10);
                            fieldSort.sort = sortOrder;
                            fieldList.Add(fieldSort);
                        }
                        fieldList.Sort((p1, p2) => p1.sort.CompareTo(p2.sort));
                        StringBuilderLegacyController StreamValidRows = new StringBuilderLegacyController();
                        var contentFieldsCdef = Processor.Models.Domain.ContentMetadataModel.createByUniqueName(core, "content fields");
                        foreach (FieldSortClass fieldsort in fieldList) {
                            StringBuilderLegacyController streamRow = new StringBuilderLegacyController();
                            bool rowValid = true;
                            //
                            // If Field has name and type, it is locked and can not be changed
                            //
                            bool FieldLocked = (fieldsort.field.nameLc != "") && (fieldsort.field.fieldTypeId != 0);
                            //
                            // put the menu into the current menu format
                            //
                            formFieldId = fieldsort.field.id;
                            streamRow.add(Controllers.HtmlController.inputHidden("dtfaID." + RecordCount, formFieldId));
                            streamRow.add("<tr>");
                            //
                            // edit button
                            //
                            streamRow.add("<td class=\"ccPanelInput\" align=\"left\">" + AdminUIEditButtonController.getEditTab(core, contentFieldsCdef.name, formFieldId) + "</td>");
                            //
                            // Inherited
                            //
                            if (!AllowCDefInherit) {
                                //
                                // no parent
                                //
                                streamRow.add("<td class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "False</span></td>");
                            } else if (fieldsort.field.inherited) {
                                //
                                // inherited property
                                //
                                streamRow.add("<td class=\"ccPanelInput\" align=\"center\">" + Controllers.HtmlController.checkbox("dtfaInherited." + RecordCount, fieldsort.field.inherited) + "</td>");
                            } else {
                                Processor.Models.Domain.ContentFieldMetadataModel parentField = null;
                                //
                                // CDef has a parent, but the field is non-inherited, test for a matching Parent Field
                                //
                                if (ParentCDef != null) {
                                    foreach (KeyValuePair<string, ContentFieldMetadataModel> kvp in ParentCDef.fields) {
                                        if (kvp.Value.nameLc == fieldsort.field.nameLc) {
                                            parentField = kvp.Value;
                                            break;
                                        }
                                    }
                                }
                                if (parentField == null) {
                                    streamRow.add("<td class=\"ccPanelInput\" align=\"center\">" + SpanClassAdminSmall + "False**</span></td>");
                                    NeedFootNote2 = true;
                                } else {
                                    streamRow.add("<td class=\"ccPanelInput\" align=\"center\">" + Controllers.HtmlController.checkbox("dtfaInherited." + RecordCount, fieldsort.field.inherited) + "</td>");
                                }
                            }
                            //
                            // name
                            //
                            bool tmpValue = string.IsNullOrEmpty(fieldsort.field.nameLc);
                            rowValid = rowValid && !tmpValue;
                            streamRow.add("<td class=\"ccPanelInput\" align=\"left\"><nobr>");
                            if (fieldsort.field.inherited) {
                                streamRow.add(SpanClassAdminSmall + fieldsort.field.nameLc + "&nbsp;</SPAN>");
                            } else if (FieldLocked) {
                                streamRow.add(SpanClassAdminSmall + fieldsort.field.nameLc + "&nbsp;</SPAN><input type=hidden name=dtfaName." + RecordCount + " value=\"" + fieldsort.field.nameLc + "\">");
                            } else {
                                streamRow.add(Controllers.HtmlController.inputText_Legacy(core, "dtfaName." + RecordCount, fieldsort.field.nameLc, 1, 10));
                            }
                            streamRow.add("</nobr></td>");
                            //
                            // caption
                            //
                            streamRow.add("<td class=\"ccPanelInput\" align=\"left\"><nobr>");
                            if (fieldsort.field.inherited) {
                                streamRow.add(SpanClassAdminSmall + fieldsort.field.caption + "</SPAN>");
                            } else {
                                streamRow.add(Controllers.HtmlController.inputText_Legacy(core, "dtfaCaption." + RecordCount, fieldsort.field.caption, 1, 10));
                            }
                            streamRow.add("</nobr></td>");
                            //
                            // Edit Tab
                            //
                            streamRow.add("<td class=\"ccPanelInput\" align=\"left\"><nobr>");
                            if (fieldsort.field.inherited) {
                                streamRow.add(SpanClassAdminSmall + fieldsort.field.editTabName + "</SPAN>");
                            } else {
                                streamRow.add(Controllers.HtmlController.inputText_Legacy(core, "dtfaEditTab." + RecordCount, fieldsort.field.editTabName, 1, 10));
                            }
                            streamRow.add("</nobr></td>");
                            //
                            // default
                            //
                            streamRow.add("<td class=\"ccPanelInput\" align=\"left\"><nobr>");
                            if (fieldsort.field.inherited) {
                                streamRow.add(SpanClassAdminSmall + GenericController.encodeText(fieldsort.field.defaultValue) + "</SPAN>");
                            } else {
                                streamRow.add(Controllers.HtmlController.inputText_Legacy(core, "dtfaDefaultValue." + RecordCount, GenericController.encodeText(fieldsort.field.defaultValue), 1, 10));
                            }
                            streamRow.add("</nobr></td>");
                            //
                            // type
                            //
                            rowValid = rowValid && (fieldsort.field.fieldTypeId > 0);
                            streamRow.add("<td class=\"ccPanelInput\" align=\"left\"><nobr>");
                            if (fieldsort.field.inherited) {
                                using (var csData = new CsModel(core)) {
                                    csData.openRecord("Content Field Types", (int)fieldsort.field.fieldTypeId);
                                    if (!csData.ok()) {
                                        streamRow.add(SpanClassAdminSmall + "Unknown[" + fieldsort.field.fieldTypeId + "]</SPAN>");
                                    } else {
                                        streamRow.add(SpanClassAdminSmall + csData.getText("Name") + "</SPAN>");
                                    }
                                }
                            } else if (FieldLocked) {
                                streamRow.add(DbBaseModel.getRecordName<ContentFieldTypeModel>(core.cpParent, (int)fieldsort.field.fieldTypeId) + Controllers.HtmlController.inputHidden("dtfaType." + RecordCount, (int)fieldsort.field.fieldTypeId));
                            } else {
                                string TypeSelect = TypeSelectTemplate;
                                TypeSelect = GenericController.strReplace(TypeSelect, "menuname", "dtfaType." + RecordCount, 1, 99, 1);
                                TypeSelect = GenericController.strReplace(TypeSelect, "=\"" + fieldsort.field.fieldTypeId + "\"", "=\"" + fieldsort.field.fieldTypeId + "\" selected", 1, 99, 1);
                                streamRow.add(TypeSelect);
                            }
                            streamRow.add("</nobr></td>");
                            //
                            // sort priority
                            //
                            streamRow.add("<td class=\"ccPanelInput\" align=\"left\"><nobr>");
                            if (fieldsort.field.inherited) {
                                streamRow.add(SpanClassAdminSmall + fieldsort.field.editSortPriority + "</SPAN>");
                            } else {
                                streamRow.add(Controllers.HtmlController.inputText_Legacy(core, "dtfaEditSortPriority." + RecordCount, fieldsort.field.editSortPriority.ToString(), 1, 10));
                            }
                            streamRow.add("</nobr></td>");
                            //
                            // active
                            //
                            streamRow.add(configureEdit_CheckBox("dtfaActive." + RecordCount, fieldsort.field.active, fieldsort.field.inherited));
                            //
                            // read only
                            //
                            streamRow.add(configureEdit_CheckBox("dtfaReadOnly." + RecordCount, fieldsort.field.readOnly, fieldsort.field.inherited));
                            //
                            // authorable
                            //
                            streamRow.add(configureEdit_CheckBox("dtfaAuthorable." + RecordCount, fieldsort.field.authorable, fieldsort.field.inherited));
                            //
                            // required
                            //
                            streamRow.add(configureEdit_CheckBox("dtfaRequired." + RecordCount, fieldsort.field.required, fieldsort.field.inherited));
                            //
                            // UniqueName
                            //
                            streamRow.add(configureEdit_CheckBox("dtfaUniqueName." + RecordCount, fieldsort.field.uniqueName, fieldsort.field.inherited));
                            //
                            // text buffered
                            //
                            streamRow.add(configureEdit_CheckBox("dtfaTextBuffered." + RecordCount, fieldsort.field.textBuffered, fieldsort.field.inherited));
                            //
                            // password
                            //
                            streamRow.add(configureEdit_CheckBox("dtfaPassword." + RecordCount, fieldsort.field.password, fieldsort.field.inherited));
                            //
                            // scramble
                            //
                            streamRow.add(configureEdit_CheckBox("dtfaScramble." + RecordCount, fieldsort.field.scramble, fieldsort.field.inherited));
                            //
                            // HTML Content
                            //
                            streamRow.add(configureEdit_CheckBox("dtfaHTMLContent." + RecordCount, fieldsort.field.htmlContent, fieldsort.field.inherited));
                            //
                            // Admin Only
                            //
                            if (core.session.isAuthenticatedAdmin()) {
                                streamRow.add(configureEdit_CheckBox("dtfaAdminOnly." + RecordCount, fieldsort.field.adminOnly, fieldsort.field.inherited));
                            }
                            //
                            // Developer Only
                            //
                            if (core.session.isAuthenticatedDeveloper()) {
                                streamRow.add(configureEdit_CheckBox("dtfaDeveloperOnly." + RecordCount, fieldsort.field.developerOnly, fieldsort.field.inherited));
                            }
                            //
                            streamRow.add("</tr>");
                            RecordCount = RecordCount + 1;
                            //
                            // rows are built - put the blank rows at the top
                            //
                            if (!rowValid) {
                                Stream.add(streamRow.text);
                            } else {
                                StreamValidRows.add(streamRow.text);
                            }
                        }
                        Stream.add(StreamValidRows.text);
                        Stream.add(Controllers.HtmlController.inputHidden("dtfaRecordCount", RecordCount));
                    }
                    Stream.add("</table>");
                    //
                    Stream.add(core.html.getPanelBottom());
                    if (NeedFootNote1) {
                        Stream.add("<br>*Field Inheritance is not allowed because this Content Definition has no parent.");
                    }
                    if (NeedFootNote2) {
                        Stream.add("<br>**This field can not be inherited because the Parent Content Definition does not have a field with the same name.");
                    }
                }
                Stream.add(Controllers.HtmlController.inputHidden("ReloadCDef", ReloadCDef));
                //
                // -- assemble form
                return AdminUIController.getToolForm(core, Stream.text, ButtonList);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return toolExceptionMessage;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// get checkbox
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="selected"></param>
        /// <param name="Inherited"></param>
        /// <returns></returns>
        public static string configureEdit_CheckBox(string htmlName, bool selected, bool Inherited) {
            string result = "<td class=\"ccPanelInput\" align=\"center\"><nobr>";
            if (Inherited) {
                result += SpanClassAdminSmall + getYesNo(selected) + "</SPAN>";
            } else {
                result += Controllers.HtmlController.checkbox(htmlName, selected);
            }
            return result + "</nobr></td>";
        }
        //
        //====================================================================================================
        /// <summary>
        /// domain class
        /// </summary>
        private class FieldSortClass {
            public string sort;
            public Processor.Models.Domain.ContentFieldMetadataModel field;
        }
    }
}

