
using System;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class CreateChildContentDefinitionClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return getForm_CreateChildContent(((CPClass)cpBase).core);
        }
        //
        //=============================================================================
        // Create a child content
        //=============================================================================
        //
        private string getForm_CreateChildContent(CoreController core) {
            string result = "";
            try {
                int ParentContentId = 0;
                string ChildContentName = "";
                bool AddAdminMenuEntry = false;
                StringBuilderLegacyController Stream = new();
                string ButtonList = ButtonCancel + "," + ButtonRun;
                //
                Stream.add(AdminUIController.getHeaderTitleDescription("Create a Child Content from a Content Definition", "This tool creates a Content Definition based on another Content Definition."));
                //
                //   print out the submit form
                if (core.docProperties.getText("Button") != "") {
                    //
                    // Process input
                    //
                    ParentContentId = core.docProperties.getInteger("ParentContentID");
                    var parentContentMetadata = ContentMetadataModel.create(core, ParentContentId);
                    ChildContentName = core.docProperties.getText("ChildContentName");
                    AddAdminMenuEntry = core.docProperties.getBoolean("AddAdminMenuEntry");
                    //
                    Stream.add(SpanClassAdminSmall);
                    if ((parentContentMetadata == null) || (string.IsNullOrEmpty(ChildContentName))) {
                        Stream.add("<p>You must select a parent and provide a child name.</p>");
                    } else {
                        //
                        // Create Definition
                        //
                        Stream.add("<P>Creating content [" + ChildContentName + "] from [" + parentContentMetadata + "]");
                        var childContentMetadata = parentContentMetadata.createContentChild(core, ChildContentName, core.session.user.id);
                        //
                        Stream.add("<br>Reloading Content Definitions...");
                        core.cache.invalidateAll();
                        core.cacheRuntime.clear();
                        Stream.add("<br>Finished</P>");
                    }
                    Stream.add("</SPAN>");
                }
                Stream.add(SpanClassAdminNormal);
                //
                Stream.add("Parent Content Name<br>");
                Stream.add(core.html.selectFromContent("ParentContentID", ParentContentId, "Content", "").replace("<select ", "<select class=\"form-control\" ",StringComparison.InvariantCultureIgnoreCase));
                Stream.add("<br><br>");
                //
                Stream.add("Child Content Name<br>");
                Stream.add(HtmlController.inputText_Legacy(core, "ChildContentName", ChildContentName, 1, 40,"",false,false,"form-control"));
                Stream.add("<br><br>");
                //
                Stream.add("Add Admin Menu Entry under Parent's Menu Entry<br>");
                Stream.add(HtmlController.checkbox("AddAdminMenuEntry", AddAdminMenuEntry));
                Stream.add("<br><br>");
                //
                Stream.add("</SPAN>");
                //
                result = AdminUIController.getToolForm(core, Stream.text, ButtonList);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
    }
}

