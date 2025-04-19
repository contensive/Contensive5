using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using System;
using System.Data;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class PagesToReviewWidget : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                return new DashboardWidgetNumberModel() {
                    widgetName = "Pages To Review",
                    width = 1,
                    number = PageContentModel.getPagesToReviewCount(cp).ToString(),
                    subhead = "Pages To Review",
                    description = "Pages not reviewed in the past 90 days. Click to Review.",
                    refreshSeconds = 0,
                    widgetType = WidgetTypeEnum.number,
                    url = $"{cp.Site.GetText("adminurl")}?addonguid=%7B6A54A051-6CF6-4D8C-823D-C37741DD072A%7D"
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
