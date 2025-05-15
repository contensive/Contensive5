using Contensive.Processor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Models.Models.Domain {
    public class DashboardWidgetBarChartModel2 : DashboardWidgetBaseModel {

    }
    public class DashboardWidgetBarChartModel_DataSets {
        public string label { get; set; }
        public List<double> data { get; set; }
        public string backgroundColor { get; set; }
        public string borderColor { get; set; }
        public int borderWidth { get; set; }
    }
}
