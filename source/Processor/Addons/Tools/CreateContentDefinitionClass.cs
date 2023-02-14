
using System;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class CreateContentDefinitionClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get(((CPClass)cpBase).core);
        }
        //
        //====================================================================================================
        //
        public static string get(CoreController core ) {
            string result = "";
            try {
                //
                int ContentId = 0;
                string TableName = "";
                string ContentName = "";
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                string ButtonList = null;
                string Description = null;
                string Caption = null;
                int NavId = 0;
                int ParentNavId = 0;
                DataSourceModel datasource = DataSourceModel.create(core.cpParent, core.docProperties.getInteger("DataSourceID"));
                //
                ButtonList = ButtonCancel + "," + ButtonRun;
                Caption = "Create Content Definition";
                Description = "This tool creates a Content Definition. If the SQL table exists, it is used. If it does not exist, it is created. If records exist in the table with a blank ContentControlID, the ContentControlID will be populated from this new definition. A Navigator Menu entry will be added under Manage Site Content - Advanced.";
                //
                //   print out the submit form
                //
                if (core.docProperties.getText("Button") != "") {
                    //
                    // Process input
                    //
                    ContentName = core.docProperties.getText("ContentName");
                    TableName = core.docProperties.getText("TableName");
                    //
                    Stream.add(SpanClassAdminSmall);
                    Stream.add("<P>Creating content [" + ContentName + "] on table [" + TableName + "] on Datasource [" + datasource.name + "].</P>");
                    if ((!string.IsNullOrEmpty(ContentName)) && (!string.IsNullOrEmpty(TableName)) && (!string.IsNullOrEmpty(datasource.name))) {
                        using (var db = new DbController(core, datasource.name)) {
                            //
                            // -- create table and core fields, plus core indexes. Then during maintenance, remove unused indexes
                            db.createSQLTable(TableName);
                        }
                        ContentMetadataModel.createFromSQLTable(core, datasource, TableName, ContentName);
                        core.cache.invalidateAll();
                        core.clearMetaData();
                        ContentId = Processor.Models.Domain.ContentMetadataModel.getContentId(core, ContentName);
                        ParentNavId = MetadataController.getRecordIdByUniqueName(core, NavigatorEntryModel.tableMetadata.contentName, "Manage Site Content");
                        if (ParentNavId != 0) {
                            ParentNavId = 0;
                            using (var csSrc = new CsModel(core)) {
                                if (csSrc.open(NavigatorEntryModel.tableMetadata.contentName, "(name=" + DbController.encodeSQLText("Advanced") + ")and(parentid=" + ParentNavId + ")")) {
                                    ParentNavId = csSrc.getInteger("ID");
                                }
                            }
                            if (ParentNavId != 0) {
                                using (var csDest = new CsModel(core)) {
                                    csDest.open(NavigatorEntryModel.tableMetadata.contentName, "(name=" + DbController.encodeSQLText(ContentName) + ")and(parentid=" + NavId + ")");
                                    if (!csDest.ok()) {
                                        csDest.close();
                                        csDest.insert(NavigatorEntryModel.tableMetadata.contentName);
                                    }
                                    if (csDest.ok()) {
                                        csDest.set("name", ContentName);
                                        csDest.set("parentid", ParentNavId);
                                        csDest.set("contentid", ContentId);
                                    }
                                }
                            }
                        }
                        ContentId = ContentMetadataModel.getContentId(core, ContentName);
                        Stream.add("<P>Content Definition was created. An admin menu entry for this definition has been added under 'Site Content', and will be visible on the next page view. Use the [<a href=\"?af=105&ContentID=" + ContentId + "\">Edit Content Definition Fields</a>] tool to review and edit this definition's fields.</P>");
                    } else {
                        Stream.add("<P>Error, a required field is missing. Content not created.</P>");
                    }
                    Stream.add("</SPAN>");
                }
                Stream.add(SpanClassAdminNormal);
                Stream.add("Data Source<br>");
                Stream.add(core.html.selectFromContent("DataSourceID", datasource.id, "Data Sources", "", "Default").replace("<select ", "<select class=\"form-control\" ", StringComparison.InvariantCultureIgnoreCase));
                Stream.add("<br><br>");
                Stream.add("Content Name<br>");
                Stream.add(HtmlController.inputText_Legacy(core, "ContentName", ContentName, 1, 40,"",false,false,"form-control"));
                Stream.add("<br><br>");
                Stream.add("Table Name<br>");
                Stream.add(HtmlController.inputText_Legacy(core, "TableName", TableName, 1, 40, "", false, false, "form-control"));
                Stream.add("<br><br>");
                Stream.add("</SPAN>");
                result = AdminUIController.getToolBody(core, Caption, ButtonList, "", false, false, Description, "", 10, Stream.text);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
    }
}

