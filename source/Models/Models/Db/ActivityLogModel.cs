
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// Logs events related to user actvity
    /// </summary>
    public class ActivityLogModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Activity Log", "ccActivityLog", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// The url of the event (video meeting, etc)
        /// </summary>
        public string link { get; set; }
        /// <summary>
        /// The user who the event affects. In the CRM, this is the lead/customer whose page this activity apears. The user who received the email.
        /// </summary>
        public int memberId { get; set; }
        /// <summary>
        /// see ActivityLogTypeEnum, 1=online visit, 2=online purchase, 3=email-to, 4=email-from, 5=call-to, 6=call-from, 7=text-to, 8=text-from, 9=meeting-video, 10=meeting-in-person
        /// </summary>
        public int typeId { get; set; }
        /// <summary>
        /// plain text. a description of the event
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// For Tracking only. The visit session in which the event occured
        /// </summary>
        public int visitId { get; set; }
        /// <summary>
        /// tracking. the visitor in effect when the event occured
        /// </summary>
        public int visitorId { get; set; }
        /// <summary>
        /// for meetings, this is the person in the staff group who is to meet. 
        /// </summary>
        public int scheduledStaffId { get; set; }
        /// <summary>
        /// If this activity is a future scheduled event, this is the start of the event
        /// </summary>
        public DateTime? dateScheduled { get; set; }
        /// <summary>
        /// If this activity is a scheduled event, this is when the activity was marked completed.
        /// Not necessarily the exact start or end time of the event.
        /// </summary>
        public DateTime? dateCompleted { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? dateCanceled { get; set; }
        /// <summary>
        /// If this activity is a future scheduled event, this is the duration of the event in minutes
        /// </summary>
        public int duration { get; set; }
        //
        public enum ActivityLogTypeEnum {
            OnlineVisit=1,
            OnlinePurchase=2,
            EmailTo = 3,
            EmailFrom = 4,
            CallTo = 5,
            CallFrom = 6,
            TextTo = 7,
            TextFrom = 8,
            MeetingOnline=9,
            MeetingInPerson = 10,
            ContactUpdate = 11
        }


    }
}
