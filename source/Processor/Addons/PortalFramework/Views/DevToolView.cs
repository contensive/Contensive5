
using Contensive.BaseClasses;
using Contensive.Processor.Addons.PortalFramework.Models.Domain;
using System;

namespace Contensive.Processor.Addons.PortalFramework.Views {
    public class DevToolView {
        //====================================================================================================
        /// <summary>
        /// get a portal feature available to developers that provides tool for creating portals
        /// </summary>
        /// <param name="CP"></param>
        /// <returns></returns>
        public static string getDevTool(CPBaseClass CP, PortalDataModel portalData, string frameRqs) {
            try {
                string section;
                //
                // this is a feature list, display the feature list
                //
                var content = CP.AdminUI.CreateLayoutBuilder();
                content.title = "Developer Tool";
                content.body = "";
                //
                // process snapshot tool
                //
                if (CP.Doc.GetText("button") == "Take Snapshot") {
                    CPCSBaseClass cs = CP.CSNew();
                    if (cs.Open("portals", "ccguid=" + CP.Db.EncodeSQLText(portalData.guid), "", true, "", 9999, 1)) {
                        string configJson = CP.JSON.Serialize(portalData);
                        cs.SetField("defaultConfigJson", configJson);
                    }
                    cs.Close();

                }
                //
                // output snapshot tool
                //
                section = "<h3>Portal Snapshot</h3>";
                section += "<p>Click the snapshot button to save the current features for this portal in the portal's default configuration field.</p>";
                section += CP.Html.Button("button", "Take Snapshot");
                section += "<p>Modify Portal and Portal Features data directly</p>";
                section += "<ul>";
                section += "<li><a href=\"?cid=" + CP.Content.GetID("Portals") + "\">Portals</a></li>";
                section += "<li><a href=\"?cid=" + CP.Content.GetID("Portal Features") + "\">Portal Features</a></li>";
                section += "<li><a href=\"?cid=" + CP.Content.GetID("Admin Framework Reports") + "\">Admin Framework Reports</a></li>";
                section += "<li><a href=\"?cid=" + CP.Content.GetID("Admin Framework Report Columns") + "\">Admin Framework Report Columns</a></li>";
                section += "</ul>";
                section = CP.Html.Form(section, "", "", "", frameRqs);
                content.body += section;
                //
                //
                //
                return content.getHtml();
            } catch (Exception ex) {
                CP.Site.ErrorReport(ex, "exception in loadPortal");
                throw;
            }
        }
    }
}