using Amazon.Runtime.Internal.Util;
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite {
    public class AdminNav {
        public static string get(CPClass cp) {
            try {
                if (cp.Site.GetBoolean("Admin Nav Widget Dashboard", true)) {
                    //
                    // -- beta portal nav
                    string layout = cp.Layout.GetLayout(layoutAdminSidebarGuid, layoutAdminSidebarName, layoutAdminSidebarCdnPathFilename, layoutAdminSidebarCdnPathFilename);
                    return cp.Mustache.Render(layout, new AdminNavViewModel(cp));
                } else {
                    //
                    // -- legacy admin nav
                    return cp.core.addon.execute(cp.core.cacheRuntime.addonCache.create(AdminNavigatorGuid), new CPUtilsBaseClass.addonExecuteContext {
                        addonType = CPUtilsBaseClass.addonContext.ContextAdmin,
                        errorContextMessage = "executing Admin Navigator in Admin"
                    });
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "";
                // -- swallow
            }
        }
    }
    //
    public class AdminNavViewModel {
        //
        public readonly string adminUrl;
        /// <summary>
        /// the current url si the admin url
        /// </summary>
        public readonly bool isAdminUrl;
        //
        public readonly List<AdminNavViewModel_Portal> portals;
        //
        public readonly AdminNavViewModel_PublicDomains publicDomains;
        //
        public AdminNavViewModel(CPBaseClass cp) {
            try {
                adminUrl = cp.Site.GetText("adminurl");
                isAdminUrl = cp.Request.PathPage.Equals(adminUrl, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(cp.Request.QueryString) && string.IsNullOrEmpty(cp.Request.Form);
                portals = [];
                string currentPortalGuid = cp.Doc.GetText("setPortalGuid");
                int currentPortalId = cp.Doc.GetInteger("setPortalId");
                string baseUrl = $"{cp.Site.GetText("adminurl")}?addonGuid=%7BA1BCA00C-2657-42A1-8F98-BED1E5A42025%7D";
                //
                // -- get the list of portals
                string iconFieldName = cp.Utils.versionIsOlder(cp.Site.GetText("BuildVersion"), "25.6.20.1") ? "'' as icon" : "icon";
                string sql = $"select id,name,ccguid,{iconFieldName} from ccPortals where active>0 order by name";
                using (var dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt == null || dt.Rows.Count == 0) { return; }
                    //
                    // -- get the list of portals
                    foreach (DataRow dr in dt.Rows) {
                        // -- add blank if icon is blank, or the http-prefixed icon url if present
                        string icon = GenericController.getText(dr["icon"]);
                        portals.Add(new AdminNavViewModel_Portal() {
                            name = GenericController.getText(dr["name"]),
                            url = $"{baseUrl}&setPortalGuid={GenericController.getText(dr["ccguid"])}",
                            active = currentPortalGuid.Equals(GenericController.getText(dr["ccguid"]), StringComparison.OrdinalIgnoreCase) || currentPortalId.Equals(GenericController.getInteger(dr["id"])),
                            icon = string.IsNullOrEmpty(icon) ? "" : cp.Http.CdnFilePathPrefix + icon
                        });
                    }
                }
                //
                // -- list of domains
                // -- 0/1=normal, 2=forward to url, 3=forward to replacement domain
                using (var dt = cp.Db.ExecuteQuery($@"select name,typeId from ccdomains where active>0 order by name")) {
                    if (dt?.Rows != null && dt.Rows.Count > 0) {
                        publicDomains = new AdminNavViewModel_PublicDomains() {
                            primaries = [],
                            secondaries = [],
                            forwards = []
                        };
                        List<string> primaryDomainList = cp.GetAppConfig().domainList.Select(s => s.ToLower()).ToList();
                        foreach (DataRow dr in dt.Rows) {
                            string domainName = GenericController.getText(dr["name"]);
                            int typeId = cp.Utils.EncodeInteger(dr["typeId"]);
                            if (typeId <= 1) {
                                if (primaryDomainList.Contains(domainName.ToLower())) {
                                    publicDomains.primaries.Add(domainName);
                                } else {
                                    publicDomains.secondaries.Add(domainName);
                                }
                            } else if (typeId == 2 || typeId == 3) {
                                publicDomains.forwards.Add(domainName);
                            }
                        }
                    }

                }
            } catch (Exception ex) {
                logger.Error(ex);
                // -- swallow
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
    //
    public class AdminNavViewModel_Portal {
        public string name { get; set; }
        public string url { get; set; }
        public bool active { get; set; } = true;
        public string icon { get; set; }
    }
    //
    public class AdminNavViewModel_PublicDomains {
        public bool hasPrimaries { get { return (primaries != null) && (primaries.Count > 0); } }
        public List<string> primaries { get; set; }
        public bool hasSecondaries { get { return (secondaries != null) && (secondaries.Count > 0); } }
        public List<string> secondaries { get; set; }
        public bool hasForwards { get { return (forwards != null) && (forwards.Count > 0); } }
        public List<string> forwards { get; set; }

    }
}
