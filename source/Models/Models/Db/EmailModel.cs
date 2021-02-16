﻿
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// 
    /// </summary>
    public class EmailModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("email", "ccemail", "default", true);
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string addonList { get; set; }
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
        public bool sent { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DbBaseModel.FieldTypeCSSFile stylesFilename { get; set; }
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
        public DateTime? conditionExpireDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int conditionId { get; set; }
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
    }
}
