
using Amazon;
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Security.Cryptography;
using System.Xml;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Software updates
    /// </summary>
    public static class SiteWarningsClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //====================================================================================================
        /// <summary>
        /// Daily, download software updates. This is a legacy process but is preserved to force
        /// and update if a patch is required.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static void setWarnings(HouseKeepEnvironmentModel env) {
            try {
                var cp = env.core.cpParent;
                //
                env.log("Housekeep, SiteWarningsClass");
                //
                // -- if setup for bs4/bs5, check templates and layouts
                bool isSiteBootrap5 = env.core.siteProperties.htmlPlatformVersion == 5;
                //
                // -- check for templates with bs4 properties
                foreach (var template in Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PageTemplateModel>(cp)) {
                    string testLayout = template.bodyHTML;
                    if (isSiteBootrap5) {
                        string errorMsg = includesBootstrap4(cp, testLayout);
                        if (!string.IsNullOrEmpty(errorMsg)) {
                            cp.Site.SetSiteWarning($"Bootstrap-4 styles found in Page Template for Bootstrap-5 site", $"Bootstrap-4 styles found on site set using Bootstrap-5, template [{template.id}, {template.name}], found bootstrap-4 style selectors [{errorMsg}]");
                        }
                    }
                    //
                    // -- check for default contensive content (facebook, etc) 
                    //
                    setWarnings_defaultContent(cp, testLayout, $"template [{template.id}, {template.name}]");
                }
                //
                // -- check for layouts with bs4 properties
                foreach (var layout in Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.LayoutModel>(cp)) {
                    string testLayout = isSiteBootrap5 ? layout.layoutPlatform5.content : "";
                    testLayout = string.IsNullOrEmpty(testLayout) ? layout.layout.content : testLayout;
                    if (isSiteBootrap5) {
                        string errorMsg = includesBootstrap4(cp, testLayout);
                        if (!string.IsNullOrEmpty(errorMsg)) {
                            cp.Site.SetSiteWarning($"Bootstrap-4 styles found in Layout for Bootstrap-5 site", $"Bootstrap-4 styles found on site set using Bootstrap-5, layout [{layout.id}, {layout.name}], found bootstrap-4 style selectors [{errorMsg}]");
                        }
                    }
                    //
                    // -- check for default contensive content (facebook, etc) 
                    //
                    setWarnings_defaultContent(cp, testLayout, $"layout [{layout.id}, {layout.name}]");
                }
                //
                // -- set warning of doctype is not <!DOCTYPE HTML>
                string docType = cp.core.siteProperties.docTypeDeclaration;
                if (!string.IsNullOrEmpty(docType) && docType.ToLowerInvariant() != "<!doctype html>") {
                    cp.Site.SetSiteWarning($"html doctype is not html5 doctype html", $"html doctype is not html5 doctype html. This effects how browsers display html and styles. To fix this, make sure all templates are html5 compatible, then set DocType Declaration to '<!DOCTYPE HTML>' in the Templates tab of Site Settings, available from the Settings icon in the admin site.");
                }
                //
                // -- check for duplicate Layout records by name
                setWarnings_duplicateRecords(cp, "cclayouts", "Layouts");
                //
                // -- check for duplicate Page Template records by name
                setWarnings_duplicateRecords(cp, "cctemplates", "Page Templates", "page templates");
                //
                // -- check for duplicate Email Template records by name
                setWarnings_duplicateRecords(cp, "cctemplates", "Email Templates", "email templates");
                //
                // -- check for valid default email from-address
                testEmailSiteProperty(cp, "EMAILADMIN");
                testEmailSiteProperty(cp, "EMAILFROMADDRESS");
                testEmailSiteProperty(cp, "EMAILPUBLISHSUBMITFROM");
                //
                return;
            } catch (Exception ex) {
                logger.Error(ex, $"{env.core.logCommonMessage}");
                LogController.logAlarm(env.core, "Housekeep, SiteWarningsClass, ex [" + ex + "]");
                throw;
            }
            //

        }
        //
        // ====================================================================================================
        //
        private static void testEmailSiteProperty(CPBaseClass cp, string propertyName ) {
            if (cp.Site.GetText(propertyName).containsCaseInsensative("@www") || string.IsNullOrEmpty(cp.Site.GetText(propertyName))) { 
                cp.Site.SetSiteWarning($"Invalid email site property [{propertyName}]", "Invalid email site property [{propertyName}]"); 
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// test an html code block for issues
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="sourceMsg"></param>
        /// <param name="testLayout"></param>
        private static void setWarnings_defaultContent(CPClass cp, string testLayout, string sourceMsg) {
            if (cp.Site.Name.ToLower() != "kmaintranet") {
                string testFor = "https://www.facebook.com/Contensive";
                if (testLayout.containsCaseInsensative(testFor)) {
                    cp.Site.SetSiteWarning($"default content found in layout", $"Default content [{testFor}] found, {sourceMsg}");
                }
                testFor = "https://twitter.com/Contensive";
                if (testLayout.containsCaseInsensative(testFor)) {
                    cp.Site.SetSiteWarning($"default content found in layout", $"Default content [{testFor}] found, {sourceMsg}");
                }
                testFor = "https://www.youtube.com/Contensive";
                if (testLayout.containsCaseInsensative(testFor)) {
                    cp.Site.SetSiteWarning($"default content found in layout", $"Default content [{testFor}] found, {sourceMsg}");
                }
            }
        }

        /// <summary>
        /// Check for duplicate records by name in a table. For shared tables (like cctemplates used by both
        /// Page Templates and Email Templates), pass the contentName to filter by content type.
        /// </summary>
        private static void setWarnings_duplicateRecords(CPClass cp, string tableName, string recordTypeLabel, string contentName = "") {
            string contentFilter = "";
            if (!string.IsNullOrEmpty(contentName)) {
                contentFilter = $" and r.contentControlId in (select id from ccContent where name={DbController.encodeSQLText(contentName)})";
            }
            string sql = $"select r.name, count(*) as dupeCount from {tableName} r where r.active>0{contentFilter} group by r.name having count(*)>1";
            using (System.Data.DataTable dt = cp.Db.ExecuteQuery(sql)) {
                if (dt?.Rows.Count > 0) {
                    foreach (System.Data.DataRow row in dt.Rows) {
                        string name = row["name"]?.ToString() ?? "";
                        int dupeCount = GenericController.getInteger(row["dupeCount"]);
                        cp.Site.SetSiteWarning(
                            $"Duplicate {recordTypeLabel} record",
                            $"There are [{dupeCount}] active {recordTypeLabel} records with the name [{name}]. Duplicate names can cause collection install conflicts. Remove or rename the duplicates."
                        );
                    }
                }
            }
        }
        //
        // ====================================================================================================
        //
        private static string includesBootstrap4(CPClass cp, string testLayout) {
            string errorMsg = "";
            if (string.IsNullOrWhiteSpace(testLayout)) { return errorMsg; }
            //
            if (testLayout.Contains("data-toggle")) { errorMsg += ",data-toggle"; };
            if (testLayout.Contains("data-target")) { errorMsg += ",data-target"; };
            if (testLayout.Contains(" pr-") || testLayout.Contains("\"pr-")) { errorMsg += ",pr-*"; };
            if (testLayout.Contains(" pl-") || testLayout.Contains("\"pl-")) { errorMsg += ",pl-*"; };
            if (testLayout.Contains(" mr-") || testLayout.Contains("\"mr-")) { errorMsg += ",mr-*"; };
            if (testLayout.Contains(" ml-") || testLayout.Contains("\"ml-")) { errorMsg += ",ml-*"; };
            if (testLayout.Contains(" left-") || testLayout.Contains("\"left-")) { errorMsg += ",left-*"; };
            if (testLayout.Contains(" right-") || testLayout.Contains("\"right-")) { errorMsg += ",right-*"; };
            if (testLayout.Contains("float-left")) { errorMsg += ",float-left"; };
            if (testLayout.Contains("float-right")) { errorMsg += ",float-right"; };
            if (testLayout.Contains("boarder-left")) { errorMsg += ",boarder-left"; };
            if (testLayout.Contains("boarder-right")) { errorMsg += ",boarder-right"; };
            if (testLayout.Contains("text-left")) { errorMsg += ",text-left"; };
            if (testLayout.Contains("text-right")) { errorMsg += ",text-right"; };
            return errorMsg;
        }
        //
    }
}