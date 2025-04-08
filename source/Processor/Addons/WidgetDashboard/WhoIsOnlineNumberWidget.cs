using Contensive.BaseClasses;
using Contensive.Processor.Addons.WidgetDashboard.Models;
using System;
using System.Data;

namespace Contensive.Processor.Addons.WidgetDashboard {
    public class WhoIsOnlineNumberWidget : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                return new WidgetNumberModel() {
                    minWidth = 2,
                    minHeight = 2,
                    number = getUsersOnline(cp).ToString(),
                    subhead = "Users Online",
                    description = "The number of users online over the past 30 minutes",
                    refreshSeconds = 0
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }

        }
        //
        public static int getUsersOnline(CPBaseClass cp) {
            using DataTable dt = cp.Db.ExecuteQuery("select count(*) as cnt from ccvisits where (lastVisitTime > " + cp.Db.EncodeSQLDate(DateTime.Now.AddMinutes(-30)) + ")");
            if (dt?.Rows != null) {
                return cp.Utils.EncodeInteger(dt.Rows[0]["cnt"]);
            }
            return 0;
        }
    }
}
