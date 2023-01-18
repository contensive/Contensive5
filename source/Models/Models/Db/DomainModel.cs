
using Contensive.BaseClasses;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Models.Db {
    //
    public class DomainModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("domains", "ccdomains", "default", true);
        //
        //====================================================================================================
        /// <summary>
        /// the template used for this domain. Can be overridden by page
        /// </summary>
        public int defaultTemplateId { get; set; }
        /// <summary>
        /// forward traffic to this domain to another domain
        /// </summary>
        public int forwardDomainId { get; set; }
        /// <summary>
        /// forward traffic to this url
        /// </summary>
        public string forwardUrl { get; set; }
        /// <summary>
        /// set response header to noFollow for this domain
        /// </summary>
        public bool noFollow { get; set; }
        /// <summary>
        /// for this domain, display this page not found
        /// </summary>
        public int pageNotFoundPageId { get; set; }
        /// <summary>
        /// for this domain, the home/landing page
        /// </summary>
        public int rootPageId { get; set; }
        /// <summary>
        /// determines the type of response
        /// </summary>
        public int typeId { get; set; }
        /// <summary>
        /// true if this domain has received traffic
        /// </summary>
        public bool visited { get; set; }
        /// <summary>
        /// the default code to execute for this domain
        /// </summary>
        public int defaultRouteId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Return (do not save) a domain record with name = '*' that can be used when no domain is available.
        /// This domain will be used when no other matching domain is found
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="requestDomain"></param>
        /// <returns></returns>
        public static DomainModel getWildcardDomain(CPBaseClass cp) {
            //
            // -- attempt to get existing wildcard domain
            DomainModel domain = createByUniqueName<DomainModel>(cp, "*");
            if (domain != null) { return domain; }
            //
            // -- create new wildcard domain
            domain = DbBaseModel.addDefault<DomainModel>(cp);
            domain.name = "*";
            domain.rootPageId = 0;
            domain.noFollow = false;
            domain.typeId = 1;
            domain.visited = false;
            domain.forwardUrl = "";
            domain.defaultTemplateId = 0;
            domain.pageNotFoundPageId = 0;
            domain.forwardDomainId = 0;
            domain.defaultRouteId = cp.Site.GetInteger("DEFAULT ROUTE ADDONID");
            domain.save(cp);
            return domain;
        }
        //
        public enum DomainTypeEnum {
            Normal = 1,
            ForwardToUrl = 2,
            ForwardToReplacementDomain = 3
        }
    }
}
