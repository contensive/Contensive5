
using Contensive.BaseClasses;
using System;
using System.Linq;

namespace Contensive.Models.Db {
    /// <summary>
    /// Auditing table to track each visit (=session)
    /// </summary>
    public class VisitModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("visits", "ccvisits", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// if true, this visit is from a bot
        /// </summary>
        public bool bot { get; set; }
        /// <summary>
        /// the browser string prsented by the client browser during the visit
        /// </summary>
        public string browser { get; set; }        
        /// <summary>
        /// true if cookie support verified (second hit)
        /// </summary>
        public bool cookieSupport { get; set; }
        /// <summary>
        /// flag can be used to exclude this visit from analytics
        /// </summary>
        public bool excludeFromAnalytics { get; set; }
        /// <summary>
        /// the refering page on the first hit from the visit
        /// </summary>
        public string http_referer { get; set; }
        /// <summary>
        /// The datetime of the last hit during the visit
        /// </summary>
        public DateTime? lastVisitTime { get; set; }
        /// <summary>
        /// count of login sequential unsuccessful attempts. Set to zero on successful login
        /// </summary>
        public int loginAttempts { get; set; }
        /// <summary>
        /// The people record associated with the visit. Can change during authentication
        /// </summary>
        public int memberId { get; set; }
        /// <summary>
        /// true if this visit created a new people record
        /// </summary>
        public bool memberNew { get; set; }
        /// <summary>
        /// browser indicates mobile
        /// </summary>
        public bool mobile { get; set; }
        /// <summary>
        /// number of hits in this visit
        /// </summary>
        public int pageVisits { get; set; }
        /// <summary>
        /// pathPage of referer
        /// </summary>
        public string refererPathPage { get; set; }
        /// <summary>
        /// request ip address
        /// </summary>
        public string remote_addr { get; set; }
        /// <summary>
        /// integer of start date
        /// </summary>
        public int startDateValue { get; set; }
        /// <summary>
        /// time portion of startDate
        /// </summary>
        public DateTime? startTime { get; set; }
        /// <summary>
        /// seconds between firsthit and lasthit
        /// </summary>
        public int timeToLastHit { get; set; }
        /// <summary>
        /// if true, this visit was authenticated to memberId
        /// </summary>
        public bool visitAuthenticated { get; set; }
        /// <summary>
        /// visitor for this visit
        /// </summary>
        public int visitorId { get; set; }
        /// <summary>
        /// if true, the visitor was created for this visit
        /// </summary>
        public bool visitorNew { get; set; }
        /// <summary>
        /// if true, the user on this visit is able to see age restricted content
        /// </summary>
        public bool CanSeeAgeRestrictedContent { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// return a visit object for the visitor's last visit before the provided id
        /// </summary>
        /// <param name="core"></param>
        /// <param name="visitId"></param>
        /// <param name="visitorId"></param>
        /// <returns></returns>
        public static VisitModel getLastVisitByVisitor(CPBaseClass cp, int visitId, int visitorId) {
            var visitList = DbBaseModel.createList<VisitModel>(cp, "(id<>" + visitId + ")and(VisitorID=" + visitorId + ")", "id desc");
            if ( visitList.Count>0) {
                return visitList.First();
            }
            return null;
        }
    }
}
