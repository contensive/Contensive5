
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
        /// <summary>
        /// header text on the right side to the left, typically user name 
        /// </summary>
        public string linkToUserRecord {
            get {
                return cp.Html5.A(cp.User.Name, new CPBase.BaseModels.HtmlAttributesA {
                    href = "?af=4&cid=" + cp.Content.GetID("people") + "&id=" + cp.User.Id
                });
            }
        }
        //
        public List<NavItem> navProfileList {
            get {
                var navList = new List<NavItem> {
                    new NavItem {
                        navItemName = cp.User.Name,
                        navItemHref = "?af=4&cid=" + cp.Content.GetID("people") + "&id=" + cp.User.Id
                    },
                    new NavItem {
                        navItemName = "Logout",
                        navItemHref = "?method=logout"
                    },
                    new NavItem {
                        navItemName = "Impersonate",
                        navItemHref = "/impersonate"
                    }
                };
                return navList;
            }
        }
        //
        //====================================================================================================
        public List<NavItem> recentList {
            get {
                if (localRecentList != null) { return localRecentList; }
                if (cp.User.Id == 0) { return new List<NavItem>(); }
                //
                // -- read from cache, invidate if an admin click isnt found in recent table
                string cacheKey = cp.Cache.CreateKey("admin-recent-List-" + cp.User.Id);
                localRecentList = cp.Cache.GetObject<List<NavItem>>(cacheKey);
                if (localRecentList != null) return localRecentList;
                localRecentList = new List<NavItem>();
                //
                //
                using (DataTable dt = cp.Db.ExecuteQuery("select top 20 name,href from ccAdminRecents where userId=" + cp.User.Id + " order by modifiedDate desc")) {
                    if (dt?.Rows != null) {
                        foreach (DataRow dr in dt.Rows) {
                            localRecentList.Add(new NavItem {
                                navItemHref = cp.Utils.EncodeText(dr["href"]),
                                navItemName = cp.Utils.EncodeText(dr["name"])
                            });
                        }
                    }
                }
                localRecentList.Sort((a, b) => a.navItemName.CompareTo(b.navItemName));
                //
                string depKey = cp.Cache.CreateTableDependencyKey(AdminRecentModel.tableMetadata.tableNameLower);
                cp.Cache.Store(cacheKey, localRecentList, depKey);
                return localRecentList;
            }
        }
        private List<NavItem> localRecentList;
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
        private PersonModel user_local;
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
        private OrganizationModel userOrganization_local;
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
        //
        /// <summary>
        /// header caption (typically "administration site")
        /// </summary>
        public string adminHeaderCaption {
            get {
                return cp.Site.GetText("AdminHeaderHTML", "Administration Site");
            }
        }
        /// <summary>
        /// header nav, typcially login/logout buttons
        /// </summary>
        public string adminRoute {
            get {
                return cp.GetAppConfig().adminRoute;
            }
        }
        //
        // -- deprecated --
        //
        //
        /// <summary>
        /// header left side (typically "administration site")
        /// </summary>
        public string leftSideMessage {
            get {
                return adminHeaderCaption;
            }
        }
        //
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
        /// <summary>
        /// header brand (typicall the site name)
        /// </summary>
        public string navBrand {
            get {
                return cp.Site.Name;
            }
        }
    }
    //
    public class NavItem {
        public string navItemHref { get; set; }
        public string navItemName { get; set; }
    }
}

