
namespace Contensive.Models.Db {
    //
    public class VisitSummaryModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("visit summary", "ccvisitsummary", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// field properties
        /// </summary>
        public int authenticatedVisits { get; set; }
        public int aveTimeOnSite { get; set; }
        public int botVisits { get; set; }
        public int dateNumber { get; set; }
        public int mobileVisits { get; set; }
        public int newVisitorVisits { get; set; }
        public int noCookieVisits { get; set; }
        public int pagesViewed { get; set; }
        public int singlePageVisits { get; set; }
        public int timeDuration { get; set; }
        public int timeNumber { get; set; }
        public int visits { get; set; }
    }
}
