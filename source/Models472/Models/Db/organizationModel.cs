
namespace Contensive.Models.Db {
    //
    public class OrganizationModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("organizations", "organizations", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// A detailed description
        /// </summary>
        public FieldTypeTextFile copyFilename { get; set; }
        /// <summary>
        /// A brief description
        /// </summary>
        public FieldTypeTextFile briefFilename { get; set; }
        /// <summary>
        /// The user who is the primary contact for this organization
        /// </summary>
        public int contactMemberId { get; set; }
        /// <summary>
        /// Address Line 1
        /// </summary>
        public string address1 { get; set; }
        /// <summary>
        /// Address Line 2
        /// </summary>
        public string address2 { get; set; }
        /// <summary>
        /// Address city
        /// </summary>
        public string city { get; set; }
        /// <summary>
        /// The address state
        /// </summary>
        public string state { get; set; }
        /// <summary>
        /// the address zip
        /// </summary>
        public string zip { get; set; }
        /// <summary>
        /// The address country
        /// </summary>
        public string country { get; set; }
        /// <summary>
        /// A count that can be used for the number of times the organization page is viewd
        /// </summary>
        public int viewings { get; set; }
        /// <summary>
        /// can be used to count the number of clicks online
        /// </summary>
        public int clicks { get; set; }
        /// <summary>
        /// The email address that can be used for this organization
        /// </summary>
        public string email { get; set; }
        /// <summary>
        /// The fax that can be used for this organization
        /// </summary>
        public string fax { get; set; }
        /// <summary>
        /// The website url to be displayed publically
        /// </summary>
        public string web { get; set; }
        /// <summary>
        /// The link that can be used to click
        /// </summary>
        public string link { get; set; }
        /// <summary>
        /// The contact-us phone for the organization
        /// </summary>
        public string phone { get; set; }
        /// <summary>
        /// The organization imge in larger format for banners, etc
        /// </summary>
        public FieldTypeFile imageFilename { get; set; }
        /// <summary>
        /// An organiation image in smaller format for logo
        /// </summary>
        public FieldTypeFile thumbNailFilename { get; set; }
    }
}
