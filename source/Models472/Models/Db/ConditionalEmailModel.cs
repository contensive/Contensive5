
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// Send emails based on a group condition
    /// </summary>
    public class ConditionalEmailModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("conditional email", "ccemail", "default", false);
        //
        //====================================================================================================
        //
        /// <summary>
        /// Lookup 1-based, 
        /// 1=Condition period (days) before expiration from Condition Groups,
        /// 2=Condition period (days) after joining Condition Groups
        /// </summary>
        public int conditionId { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public string subject { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public bool submitted { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public int testMemberId { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public bool toAll { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public int conditionPeriod { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public DbBaseModel.FieldTypeTextFile copyFilename { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public bool addLinkEId { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public bool allowSpamFooter { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public bool blockSiteStyles { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public DateTime? conditionExpireDate { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public int emailTemplateId { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public int emailWizardId { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public string fromAddress { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public string inlineStyles { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public DateTime? lastSendTestDate { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public DateTime? scheduleDate { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public bool sent { get; set; }
        /// <summary>
        /// Conditional Email
        /// </summary>
        public DbBaseModel.FieldTypeCSSFile stylesFilename { get; set; }
        /// <summary>
        /// id of addon used to personalize email when it is sent. 
        /// Addon returns an object with public properties used with mustache to personalize.
        /// Addon will be run in an environment with the recipient as the session user
        /// </summary>
        public int personalizeAddonId { get; set; }
    }
    /// <summary>
    /// define the process for sending the email
    /// </summary>
    public enum ConditionEmailConditionId {
        /// <summary>
        /// Send email before the expiration from a group
        /// </summary>
        DaysBeforeExpiration = 1,
        /// <summary>
        /// dend the email after joining a grooup
        /// </summary>
        DaysAfterJoining = 2
    }

}
