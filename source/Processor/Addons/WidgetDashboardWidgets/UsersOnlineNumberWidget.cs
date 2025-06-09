using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using System;
using System.Data;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class UsersOnlineNumberWidget : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                return new DashboardWidgetNumberModel() {
                    widgetName = "Users Online",
                    width = 1,
                    number = getUsersOnline(cp).ToString(),
                    subhead = "Users Online",
                    description = "The number of users online over the past 30 minutes",
                    refreshSeconds = 5,
                    widgetType = WidgetTypeEnum.number,
                    url = $"{cp.Site.GetText("adminurl")}?addonguid={Constants.guidUsersOnlineReportAddon}",
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }

        }
        //
        public static int getUsersOnline(CPBaseClass cp) {
            using DataTable dt = cp.Db.ExecuteQuery($"select count(*) as cnt from ccvisits where (lastVisitTime >{cp.Db.EncodeSQLDate(DateTime.Now.AddMinutes(-30))})");
            if (dt?.Rows != null) {
                return cp.Utils.EncodeInteger(dt.Rows[0]["cnt"]);
            }
            return 0;
        }
    }
}
