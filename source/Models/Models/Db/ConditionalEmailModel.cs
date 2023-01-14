
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
        /// 
        /// </summary>
        public int conditionId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string subject { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool submitted { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int testMemberId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool toAll { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int conditionPeriod { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DbBaseModel.FieldTypeTextFile copyFilename { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool addLinkEId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool allowSpamFooter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool blockSiteStyles { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? conditionExpireDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int emailTemplateId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int emailWizardId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string fromAddress { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string inlineStyles { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? lastSendTestDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? scheduleDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool sent { get; set; }
        /// <summary>
        /// 
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
