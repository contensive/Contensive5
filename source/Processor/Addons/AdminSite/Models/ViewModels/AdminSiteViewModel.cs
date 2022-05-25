
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.Tools;
using Contensive.Processor.Controllers;
using Contensive.Processor.Exceptions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Addons.AdminSite {
    //
    //====================================================================================================
    //
    // -- admin site data model
    public class AdminSiteViewModel {
        /// <summary>
        /// callback added during construct
        /// </summary>
        private readonly CPClass cp;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        public AdminSiteViewModel(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        public List<RecentItem> recentList {
            get {
                if (localRecentList != null) { return localRecentList; }
                if (cp.User.Id == 0) return new List<RecentItem>();
                //
                // -- read from cache, invidate if an admin click isnt found in recent table
                string cacheKey = cp.Cache.CreateKey("admin-recent-List-" + cp.User.Id);
                localRecentList = cp.Cache.GetObject<List<RecentItem>>(cacheKey);
                if (localRecentList != null) return localRecentList;
                localRecentList = new List<RecentItem>();
                //
                //
                using (DataTable dt = cp.Db.ExecuteQuery("select top 20 name,href from ccAdminRecents where userId=" + cp.User.Id + " order by modifiedDate desc")) {
                    if (dt?.Rows != null) {
                        foreach (DataRow dr in dt.Rows) {
                            localRecentList.Add(new RecentItem {
                             recentHref = cp.Utils.EncodeText(dr["href"]),
                             recentName = cp.Utils.EncodeText( dr["name"])
                            });
                        }
                    }
                }
                localRecentList.Sort((a, b) => a.recentName.CompareTo(b.recentName));
                //
                string depKey = cp.Cache.CreateTableDependencyKey(AdminRecentModel.tableMetadata.tableNameLower);
                cp.Cache.Store(cacheKey, localRecentList, depKey);
                return localRecentList;
            }
        }
        private List<RecentItem> localRecentList = null;
        //
        //====================================================================================================
        public bool hasRecentList {
            get {
                return recentList.Count > 0;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// current user
        /// </summary>
        private PersonModel user {
            get {
                if (user_local != null) return user_local;
                user_local = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                return user_local;
            }
        }
        private PersonModel user_local = null;
        //
        //====================================================================================================
        /// <summary>
        /// current organization, or null if not valie
        /// </summary>
        private OrganizationModel userOrganization {
            get {
                if (userOrganization_local != null) return userOrganization_local;
                userOrganization_local = DbBaseModel.create<OrganizationModel>(cp, cp.User.OrganizationID);
                return userOrganization_local;
            }
        }
        private OrganizationModel userOrganization_local = null;
        //
        //====================================================================================================
        /// <summary>
        /// current user's initials from firstname and lastname, else first 2 characters of name
        /// </summary>
        public string userInitials {
            get {
                string initials = user.firstName.substringSafe(0, 1) + user.lastName.substringSafe(0, 1);
                if (!string.IsNullOrEmpty(initials)) { return initials; }
                initials = user.name.substringSafe(0, 2);
                return initials;
            }
        }
        //
        //====================================================================================================
        //
        public string userEmail {
            get {
                return user.email;
            }
        }
        //
        //====================================================================================================
        //
        public string userOrganizationName {
            get {
                return (userOrganization != null ? userOrganization.name : "");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// users firstname+lastname, else name
        /// </summary>
        public string userFullName {
            get {
                string fullName = user.firstName + ' ' + user.lastName;
                if (!string.IsNullOrEmpty(fullName.Trim())) { return fullName; }
                return user.name;
            }
        }
        /// <summary>
        /// exceptions to display at top of admin site
        /// </summary>
        public string adminExceptions {
            get {
                return cp.core.session.user.developer ? ErrorController.getDocExceptionHtmlList(cp.core) : "";
            }
        }
        /// <summary>
        /// navigator
        /// </summary>
        public string adminNav {
            get {
                return cp.core.addon.execute(DbBaseModel.create<AddonModel>(cp, AdminNavigatorGuid), new CPUtilsBaseClass.addonExecuteContext {
                    addonType = CPUtilsBaseClass.addonContext.ContextAdmin,
                    errorContextMessage = "executing Admin Navigator in Admin"
                });
            }
        }
        /// <summary>
        /// content
        /// </summary>
        public string adminContent {
            get {
                return AdminContentController.getAdminContent(cp);
            }
        }
        /// <summary>
        /// footer
        /// </summary>
        public string adminFooter { get; set; }
        /// <summary>
        /// header left side (typically "administration site")
        /// </summary>
        public string leftSideMessage {
            get {
                return cp.Site.GetText("AdminHeaderHTML", "Administration Site");
            }
        }
        /// <summary>
        /// header brand (typicall the site name)
        /// </summary>
        public string navBrand {
            get {
                return cp.Site.Name;
            }
        }
        /// <summary>
        /// header text on the right side to the left, typically user name 
        /// </summary>
        public string rightSideMessage {
            get {
                return cp.Html5.A(cp.User.Name, new CPBase.BaseModels.HtmlAttributesA {
                    href = "?af=4&cid=" + cp.Content.GetID("people") + "&id=" + cp.User.Id
                });
            }
        }
        /// <summary>
        /// header nav, typcially login/logout buttons
        /// </summary>
        public string rightSideNavHtml {
            get {
                return ""
                    + "<form class=\"form-inline\" method=post action=\"?method=logout\">"
                    + "<button class=\"btn btn-warning btn-sm ml-2\" type=\"submit\">Logout</button>"
                    + "</form>";
            }
        }
    }
    //
    public class RecentItem {
        public string recentHref { get; set; }
        public string recentName { get; set; }
    }
}

