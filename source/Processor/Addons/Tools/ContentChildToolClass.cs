
using System;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using NLog;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class ContentChildToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get(((CPClass)cpBase));
        }
        //
        //=============================================================================
        // Create a child content
        //=============================================================================
        //
        public static  string get(CPClass cp) {
            string result = "";
            try {
                //
                bool IsEmptyList = false;
                int ParentContentId = 0;
                string ChildContentName = "";
                int ChildContentId = 0;
                bool AddAdminMenuEntry = false;
                StringBuilderLegacyController Content = new StringBuilderLegacyController();
                string FieldValue = null;
                bool NewGroup = false;
                int GroupId = 0;
                string NewGroupName = "";
                string Button = null;
                string Caption = null;
                string Description = "";
                string ButtonList = "";
                bool BlockForm = false;
                //
                Button = cp.core.docProperties.getText(RequestNameButton);
                if (Button == ButtonCancel) {
                    //
                    //
                    //
                    return cp.core.webServer.redirect("/" + cp.core.appConfig.adminRoute, "GetContentChildTool, Cancel Button Pressed");
                } else if (!cp.core.session.isAuthenticatedAdmin()) {
                    //
                    //
                    //
                    ButtonList = ButtonCancel;
                    Content.add(AdminUIController.getFormBodyAdminOnly());
                } else {
                    //
                    if (Button != ButtonOK) {
                        //
                        // Load defaults
                        //
                        ParentContentId = cp.core.docProperties.getInteger("ParentContentID");
                        if (ParentContentId == 0) {
                            ParentContentId = ContentMetadataModel.getContentId(cp.core, "Page Content");
                        }
                        AddAdminMenuEntry = true;
                        GroupId = 0;
                    } else {
                        //
                        // Process input
                        //
                        ParentContentId = cp.core.docProperties.getInteger("ParentContentID");
                        var parentContentMetadata = ContentMetadataModel.create(cp.core, ParentContentId);
                        ChildContentName = cp.core.docProperties.getText("ChildContentName");
                        AddAdminMenuEntry = cp.core.docProperties.getBoolean("AddAdminMenuEntry");
                        GroupId = cp.core.docProperties.getInteger("GroupID");
                        NewGroup = cp.core.docProperties.getBoolean("NewGroup");
                        NewGroupName = cp.core.docProperties.getText("NewGroupName");
                        //
                        if ((parentContentMetadata == null) || (string.IsNullOrEmpty(ChildContentName))) {
                            Processor.Controllers.ErrorController.addUserError(cp.core, "You must select a parent and provide a child name.");
                        } else {
                            //
                            // Create Definition
                            //
                            Description = Description + "<div>&nbsp;</div>"
                                + "<div>Creating content [" + ChildContentName + "] from [" + parentContentMetadata.name + "]</div>";
                            var childContentMetadata = parentContentMetadata.createContentChild(cp.core, ChildContentName, cp.core.session.user.id);

                            ChildContentId = ContentMetadataModel.getContentId(cp.core, ChildContentName);
                            //
                            // Create Group and Rule
                            //
                            if (NewGroup && (!string.IsNullOrEmpty(NewGroupName))) {
                                using (var csData = new CsModel(cp.core)) {
                                    csData.open("Groups", "name=" + DbController.encodeSQLText(NewGroupName));
                                    if (csData.ok()) {
                                        Description = Description + "<div>Group [" + NewGroupName + "] already exists, using existing group.</div>";
                                        GroupId = csData.getInteger("ID");
                                    } else {
                                        Description = Description + "<div>Creating new group [" + NewGroupName + "]</div>";
                                        csData.close();
                                        csData.insert("Groups");
                                        if (csData.ok()) {
                                            GroupId = csData.getInteger("ID");
                                            csData.set("Name", NewGroupName);
                                            csData.set("Caption", NewGroupName);
                                        }
                                    }
                                }
                            }
                            if (GroupId != 0) {
                                using (var csData = new CsModel(cp.core)) {
                                    csData.insert("Group Rules");
                                    if (csData.ok()) {
                                        Description = Description + "<div>Assigning group [" + MetadataController.getRecordName(cp.core, "Groups", GroupId) + "] to edit content [" + ChildContentName + "].</div>";
                                        csData.set("GroupID", GroupId);
                                        csData.set("ContentID", ChildContentId);
                                    }
                                }
                            }
                            //
                            // Add Admin Menu Entry
                            //
                            if (AddAdminMenuEntry) {
                                //
                                // Add Navigator entries
                            }
                            //
                            Description = Description + "<div>&nbsp;</div>"
                                + "<div>Your new content is ready. <a href=\"?" + rnAdminForm + "=22\">Click here</a> to create another Content Definition, or hit [Cancel] to return to the main menu.</div>";
                            ButtonList = ButtonCancel;
                            BlockForm = true;
                        }
                        cp.core.cacheRuntime.clear();
                        cp.core.cache.invalidateAll();
                    }
                    //
                    // Get the form
                    //
                    if (!BlockForm) {
                        string tableBody = "";
                        //
                        FieldValue = "<select size=\"1\" name=\"ParentContentID\" ID=\"\"><option value=\"\">Select One</option>";
                        FieldValue = FieldValue + GetContentChildTool_Options(cp, 0, ParentContentId);
                        FieldValue = FieldValue + "</select>";
                        tableBody += AdminUIController.getEditRow(cp.core, FieldValue, "Parent Content Name", "", false, false, "");
                        //
                        FieldValue = Controllers.HtmlController.inputText_Legacy(cp.core, "ChildContentName", ChildContentName, 1, 40);
                        tableBody += AdminUIController.getEditRow(cp.core, FieldValue, "New Child Content Name", "", false, false, "");
                        //
                        FieldValue = ""
                            + Controllers.HtmlController.inputRadio("NewGroup", false.ToString(), NewGroup.ToString()) + cp.core.html.selectFromContent("GroupID", GroupId, "Groups", "", "", "", ref IsEmptyList) + "(Select a current group)"
                            + "<br>" + Controllers.HtmlController.inputRadio("NewGroup", true.ToString(), NewGroup.ToString()) + Controllers.HtmlController.inputText_Legacy(cp.core, "NewGroupName", NewGroupName) + "(Create a new group)";
                        tableBody += AdminUIController.getEditRow(cp.core, FieldValue, "Content Manager Group", "", false, false, "");
                        //
                        Content.add(AdminUIController.editTable(tableBody));
                        Content.add("</td></tr>" + kmaEndTable);
                        //
                        ButtonList = ButtonOK + "," + ButtonCancel;
                    }
                    //Content.add(Controllers.HtmlController.inputHidden(rnAdminSourceForm, AdminFormContentChildTool));
                }
                //
                Caption = "Create Child Table Metadata";
                Description = "<div>This tool is used to create child table metadata (child content definitions) that help segregate your content into authorable segments.</div>" + Description;
                result = AdminUIController.getToolBody(cp.core, Caption, ButtonList, "", false, false, Description, "", 0, Content.text);
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
            }
            return result;
        }
        //
        //=============================================================================
        // Create a child content
        //=============================================================================
        //
        private static string GetContentChildTool_Options(CPClass cp, int ParentId, int DefaultValue) {
            string returnOptions = "";
            try {
                //
                string SQL = null;
                int RecordId = 0;
                string RecordName = null;
                //
                if (ParentId == 0) {
                    SQL = "select Name, ID from ccContent where ((ParentID<1)or(Parentid is null)) and (AllowContentChildTool<>0);";
                } else {
                    SQL = "select Name, ID from ccContent where ParentID=" + ParentId + " and (AllowContentChildTool<>0) and not (allowcontentchildtool is null);";
                }
                using (var csData = new CsModel(cp.core)) {
                    csData.openSql(SQL);
                    while (csData.ok()) {
                        RecordName = csData.getText("Name");
                        RecordId = csData.getInteger("ID");
                        if (RecordId == DefaultValue) {
                            returnOptions = returnOptions + "<option value=\"" + RecordId + "\" selected>" + csData.getText("name") + "</option>";
                        } else {
                            returnOptions = returnOptions + "<option value=\"" + RecordId + "\" >" + csData.getText("name") + "</option>";
                        }
                        returnOptions = returnOptions + GetContentChildTool_Options(cp, RecordId, DefaultValue);
                        csData.goNext();
                    }
                    csData.close();
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
            return returnOptions;
        }
        //
    }
}

