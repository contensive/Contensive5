using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite {
    public class AdminNav {
        public static string get(CPClass cp) {
            try {
                if (cp.Site.GetBoolean("Admin Nav Portal Beta", false)) {
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
                throw;
            }
        }
    }
    //
    public class AdminNavViewModel {
        public readonly  List<AdminNavViewModel_Portal> portals;
        public AdminNavViewModel(CPBaseClass cp) {
            portals = [];
            string currentPortalGuid = cp.Doc.GetText("setPortalGuid");
            string baseUrl = $"{cp.Site.GetText("adminurl")}?addonGuid=%7BA1BCA00C-2657-42A1-8F98-BED1E5A42025%7D";
            //
            // -- get the list of portals
            string sql = "select name,ccguid from ccPortals where active>0 order by name";
            using (var dt = cp.Db.ExecuteQuery(sql)) {
                if (dt == null) { return; }
                if (dt.Rows.Count == 0) { return; }
                //
                // -- get the list of portals
                foreach (DataRow dr in dt.Rows) {
                    portals.Add(new AdminNavViewModel_Portal() {
                        name = GenericController.encodeText(dr["name"]),
                        url = $"{baseUrl}&setPortalGuid={GenericController.encodeText(dr["ccguid"])}",
                        active = currentPortalGuid.Equals(GenericController.encodeText(dr["ccguid"]), StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
        }
    }
    //
    public class AdminNavViewModel_Portal { 
        public string name { get; set; }
        public string url { get; set; }
        public bool active { get; set; } = true;
    }
}
