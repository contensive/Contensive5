using System;
using Contensive.Models.Attributes;

namespace Contensive.Models.Db {
    /// <summary>
    /// Logs events related to user activity
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
        [ContentField(
            Name = "Link",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Link",
            DeveloperOnly = false,
            EditSortPriority = 1000,
            FieldType = "Link",
            HtmlContent = false,
            IndexColumn = 1,
            IndexSortDirection = 0,
            IndexSortOrder = 1,
            IndexWidth = 40,
            NotEditable = false,
            Password = false,
            ReadOnly = false,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "The url of the event (video meeting, etc)"
        )]
        public string link { get; set; }
        
        /// <summary>
        /// The user who the event affects. In the CRM, this is the lead/customer whose page this activity appears. The user who received the email.
        /// </summary>
        [ContentField(
            Name = "MemberId",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "User",
            DeveloperOnly = false,
            EditSortPriority = 1100,
            FieldType = "Lookup",
            HtmlContent = false,
            IndexColumn = 2,
            IndexSortDirection = 0,
            IndexSortOrder = 2,
            IndexWidth = 20,
            LookupContent = "People",
            NotEditable = false,
            Password = false,
            ReadOnly = false,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "The user who the event affects. In the CRM, this is the lead/customer whose page this activity appears."
        )]
        public int memberId { get; set; }
        
        /// <summary>
        /// see ActivityLogTypeEnum, 1=online visit, 2=online purchase, 3=email-to, 4=email-from, 5=call-to, 6=call-from, 7=text-to, 8=text-from, 9=meeting-video, 10=meeting-in-person
        /// </summary>
        [ContentField(
            Name = "TypeId",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Activity Type",
            DeveloperOnly = false,
            EditSortPriority = 1200,
            FieldType = "Lookup",
            HtmlContent = false,
            IndexColumn = 3,
            IndexSortDirection = 0,
            IndexSortOrder = 3,
            IndexWidth = 20,
            LookupList = "Online Visit,Online Purchase,Email To,Email From,Call To,Call From,Text To,Text From,Meeting Online,Meeting In Person,Contact Update",
            NotEditable = false,
            Password = false,
            ReadOnly = false,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "The type of activity: online visit, online purchase, email, call, text, meeting, or contact update"
        )]
        public int typeId { get; set; }
        
        /// <summary>
        /// plain text. a description of the event
        /// </summary>
        [ContentField(
            Name = "Message",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Message",
            DeveloperOnly = false,
            EditSortPriority = 1300,
            FieldType = "LongText",
            HtmlContent = false,
            IndexColumn = 4,
            IndexSortDirection = 0,
            IndexSortOrder = 4,
            IndexWidth = 40,
            NotEditable = false,
            Password = false,
            ReadOnly = false,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "A plain text description of the event"
        )]
        public string message { get; set; }
        
        /// <summary>
        /// For Tracking only. The visit session in which the event occurred
        /// </summary>
        [ContentField(
            Name = "VisitId",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Visit ID",
            DeveloperOnly = false,
            EditSortPriority = 1400,
            FieldType = "Integer",
            HtmlContent = false,
            IndexColumn = 99,
            IndexSortDirection = 0,
            IndexSortOrder = 0,
            IndexWidth = 0,
            NotEditable = false,
            Password = false,
            ReadOnly = true,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "For tracking only. The visit session in which the event occurred"
        )]
        public int visitId { get; set; }
        
        /// <summary>
        /// tracking. the visitor in effect when the event occurred
        /// </summary>
        [ContentField(
            Name = "VisitorId",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Visitor ID",
            DeveloperOnly = false,
            EditSortPriority = 1500,
            FieldType = "Integer",
            HtmlContent = false,
            IndexColumn = 99,
            IndexSortDirection = 0,
            IndexSortOrder = 0,
            IndexWidth = 0,
            NotEditable = false,
            Password = false,
            ReadOnly = true,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "The visitor in effect when the event occurred"
        )]
        public int visitorId { get; set; }
        
        /// <summary>
        /// for meetings, this is the person in the Staff group who is to meet. 
        /// </summary>
        [ContentField(
            Name = "ScheduledStaffId",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Scheduled Staff",
            DeveloperOnly = false,
            EditSortPriority = 1600,
            FieldType = "Lookup",
            HtmlContent = false,
            IndexColumn = 5,
            IndexSortDirection = 0,
            IndexSortOrder = 5,
            IndexWidth = 20,
            LookupContent = "People",
            NotEditable = false,
            Password = false,
            ReadOnly = false,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "For meetings, this is the person in the Staff group who is to meet"
        )]
        public int scheduledStaffId { get; set; }
        
        /// <summary>
        /// If this activity is a future scheduled event, this is the start of the event
        /// </summary>
        [ContentField(
            Name = "DateScheduled",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Date Scheduled",
            DeveloperOnly = false,
            EditSortPriority = 1700,
            FieldType = "Date",
            HtmlContent = false,
            IndexColumn = 6,
            IndexSortDirection = 0,
            IndexSortOrder = 6,
            IndexWidth = 15,
            NotEditable = false,
            Password = false,
            ReadOnly = false,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "If this activity is a future scheduled event, this is the start date and time of the event"
        )]
        public DateTime? dateScheduled { get; set; }
        
        /// <summary>
        /// If this activity is a scheduled event, this is when the activity was marked completed.
        /// Not necessarily the exact start or end time of the event.
        /// </summary>
        [ContentField(
            Name = "DateCompleted",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Date Completed",
            DeveloperOnly = false,
            EditSortPriority = 1800,
            FieldType = "Date",
            HtmlContent = false,
            IndexColumn = 7,
            IndexSortDirection = 0,
            IndexSortOrder = 7,
            IndexWidth = 15,
            NotEditable = false,
            Password = false,
            ReadOnly = false,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "When the activity was marked completed. Not necessarily the exact start or end time of the event"
        )]
        public DateTime? dateCompleted { get; set; }
        
        /// <summary>
        /// Date the activity was canceled
        /// </summary>
        [ContentField(
            Name = "DateCanceled",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Date Canceled",
            DeveloperOnly = false,
            EditSortPriority = 1900,
            FieldType = "Date",
            HtmlContent = false,
            IndexColumn = 8,
            IndexSortDirection = 0,
            IndexSortOrder = 8,
            IndexWidth = 15,
            NotEditable = false,
            Password = false,
            ReadOnly = false,
            Required = false,
            TextBuffered = false,
            HelpDefault = "Date the activity was canceled", 
            UniqueName = false
        )]
        public DateTime? dateCanceled { get; set; }
        
        /// <summary>
        /// If this activity is a future scheduled event, this is the duration of the event in minutes
        /// </summary>
        [ContentField(
            Name = "Duration",
            Active = true,
            AdminOnly = false,
            Authorable = true,
            Caption = "Duration (minutes)",
            DeveloperOnly = false,
            EditSortPriority = 2000,
            FieldType = "Integer",
            HtmlContent = false,
            IndexColumn = 9,
            IndexSortDirection = 0,
            IndexSortOrder = 9,
            IndexWidth = 10,
            NotEditable = false,
            Password = false,
            ReadOnly = false,
            Required = false,
            TextBuffered = false,
            UniqueName = false,
            HelpDefault = "If this activity is a future scheduled event, this is the duration of the event in minutes"
        )]
        public int duration { get; set; }
        //
        public enum ActivityLogTypeEnum {
            OnlineVisit = 1,
            OnlinePurchase = 2,
            EmailTo = 3,
            EmailFrom = 4,
            CallTo = 5,
            CallFrom = 6,
            TextTo = 7,
            TextFrom = 8,
            MeetingOnline = 9,
            MeetingInPerson = 10,
            ContactUpdate = 11
        }
    }
}
