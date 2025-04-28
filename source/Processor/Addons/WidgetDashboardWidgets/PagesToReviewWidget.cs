using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class PagesToReviewWidget : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                int months = cp.Doc.GetInteger("widgetFilter");
                return new DashboardWidgetNumberModel() {
                    widgetName = "Pages To Review",
                    width = 1,
                    number = PageContentModel.getPagesToReviewCount(cp, months).ToString(),
                    subhead = "Pages To Review",
                    description = $"Pages not reviewed in the past {months} {(months==1 ? "month" : "months")}. Click to Review.",
                    refreshSeconds = 0,
                    widgetType = WidgetTypeEnum.number,
                    url = $"{cp.Site.GetText("adminurl")}?addonguid=%7B6A54A051-6CF6-4D8C-823D-C37741DD072A%7D",
                    filterOptions = new List<DashboardWidgetBaseModel_FilterOptions>() {
                        new DashboardWidgetBaseModel_FilterOptions() {
                            filterCaption = "1 Month",
                            filterValue = "1",
                            filterActive = (months == 1)
                        },
                        new DashboardWidgetBaseModel_FilterOptions() {
                            filterCaption = "3 Months",
                            filterValue = "3",
                            filterActive = (months == 3)
                        },
                        new DashboardWidgetBaseModel_FilterOptions() {
                            filterCaption = "1 Year",
                            filterValue = "12",
                            filterActive = (months == 12)
                        }
                    }
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
