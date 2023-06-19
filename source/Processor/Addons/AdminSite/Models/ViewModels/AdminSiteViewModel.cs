﻿
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
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
        //====================================================================================================
        //
        /// <summary>
        /// create the hardcoded list of 6 icons that appear at the top-right of the admin page
        /// </summary>
        public List<NavItemList> navList { 
            get {
                if (_navList != null) { return _navList; }
                if (cp.User.Id == 0) { return new List<NavItemList>(); }
                if (!cp.User.IsAdmin && !cp.User.IsContentManager()) { return new List<NavItemList>(); }
                //
                // -- read from cache, invidate if an admin click isnt found in recent table
                string cacheKey = cp.Cache.CreateKey($"admin-navlist");
                _navList = cp.Cache.GetObject<List<NavItemList>>(cacheKey);
                if (_navList != null) { return _navList; }
                //
                _navList = new() {
                    new NavItemList {
                        listName = "Design",
                        listIcon = "fas fa-paint-brush fa-lg",
                        listItemList = getNavItemsByType(NavTypeIdEnum.design)
                    },
                    new NavItemList {
                        listName = "Comm",
                        listIcon = "fas fa-comment-alt fa-lg",
                        listItemList = getNavItemsByType(NavTypeIdEnum.comm)
                    },
                    new NavItemList {
                        listName = "Reports",
                        listIcon = "fas fa-chart-pie fa-lg",
                        listItemList = getNavItemsByType(NavTypeIdEnum.report)
                    },
                    new NavItemList {
                        listName = "Tools",
                        listIcon = "fas fa-wrench fa-lg",
                        listItemList = getNavItemsByType(NavTypeIdEnum.tool)
                    },
                    new NavItemList {
                        listName = "Settings",
                        listIcon = "fas fa-cog fa-lg",
                        listItemList = getNavItemsByType(NavTypeIdEnum.setting)
                    },
                    new NavItemList {
                        listName = "User",
                        listIcon = "fas fa-user fa-lg",
                        listItemList = navProfileList
                    }
                };

                //
                string depKey = cp.Cache.CreateTableDependencyKey(AddonModel.tableMetadata.tableNameLower);
                cp.Cache.Store(cacheKey, _navList, depKey);
                return _navList;
            }
        }
        private List<NavItemList> _navList = null;
        //
        //====================================================================================================
        //
        /// <summary>
        /// get nav list for addons for one addon TypeId (include both addons and content)
        /// </summary>
        /// <param name="navTypeId"></param>
        /// <param name="localListCache"></param>
        /// <returns></returns>
        public List<NavItem> getNavItemsByType(NavTypeIdEnum navTypeId) {
            try {
                List<NavItem> localListCache = new();
                using (DataTable dt = cp.Db.ExecuteQuery(Properties.Resources.sqlGetNavItemByType.replace("{navTypeId}", ((int)navTypeId).ToString(),System.StringComparison.CurrentCultureIgnoreCase ))) {
                    if (dt?.Rows != null) {
                        string categoryNameLast = "";
                        foreach (DataRow dr in dt.Rows) {
                            string categoryName = cp.Utils.EncodeText(dr["categoryName"]);
                            categoryName = string.IsNullOrEmpty(categoryName) ? "" : categoryName;
                            if (!string.IsNullOrEmpty(categoryName) && categoryName != categoryNameLast) {
                                //
                                // -- create category header for this entry
                                localListCache.Add(new NavItem {
                                    navDivider = true
                                });
                                    localListCache.Add(new NavItem {
                                        navItemName = categoryName.replace(".", " > ", System.StringComparison.InvariantCultureIgnoreCase),
                                        isCategory = true
                                    });
                                categoryNameLast = categoryName;
                            }
                            //
                            // -- create entry
                            string entryName = cp.Utils.EncodeText(dr["name"]);
                            entryName = string.IsNullOrEmpty(entryName) ? "" : entryName;
                            //
                            string navItemHref = "";
                            int contentId = cp.Utils.EncodeInteger(dr["contentid"]);
                            if (contentId == 0) {
                                //
                                // -- addon
                                navItemHref = cp.GetAppConfig().adminRoute + "?addonguid=" + encodeURL(cp.Utils.EncodeText(dr["ccguid"]));
                            } else {
                                //
                                // -- data
                                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + contentId;
                            }
                            localListCache.Add(new NavItem {
                                navItemHref = navItemHref,
                                navItemName = entryName,
                                isCategory = false
                            });
                        }
                    }
                }
                return localListCache;
            } catch (System.Exception ex) {
                cp.Site.ErrorReport(ex, "getNavItemsByType");
                return new();
            }
        }
        //
        //====================================================================================================
        //
        /// <summary>
        /// hardcoded profile nav item in admin site top-right
        /// </summary>
        public List<NavItem> navProfileList {
            get {
                if (_navProfileList !=null) { return _navProfileList; }
                //
                string cacheKey = cp.Cache.CreateKey($"admin-nav-profileList-user{cp.User.Id}");
                _navProfileList = cp.Cache.GetObject<List<NavItem>>(cacheKey);
                if (_navProfileList != null) { return _navProfileList; }
                //
                string orgName = DbBaseModel.getRecordName<OrganizationModel>(cp, cp.User.OrganizationID);
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
                if (!string.IsNullOrEmpty(orgName)) {
                    navList.Add(new NavItem {
                        navItemName = orgName,
                        navItemHref = "?af=4&cid=" + cp.Content.GetID("organizations") + "&id=" + cp.User.OrganizationID
                    });
                }
                navList.Add(new NavItem {
                    navDivider = true
                });
                navList.Add(new NavItem {
                    navItemName = "Groups",
                    navItemHref = "?cid=" + cp.Content.GetID("groups")
                });
                navList.Add(new NavItem {
                    navItemName = "Organizations",
                    navItemHref = "?cid=" + cp.Content.GetID("organizations")
                });
                navList.Add(new NavItem {
                    navItemName = "People",
                    navItemHref = "?cid=" + cp.Content.GetID("people")
                });
                List<string> cacheKeyList = new List<string> {
                    cp.Cache.CreateTableDependencyKey(OrganizationModel.tableMetadata.tableNameLower)
                };
                cp.Cache.Store(cacheKey, _navProfileList, DateTime.Now, cacheKeyList);
                return navList;
            }
        }
        private List<NavItem> _navProfileList=null;
        ////
        ////====================================================================================================
        ////
        ////
        //public List<NavItem> navCommList {
        //    get {
        //        if (navCommList_local != null) { return navCommList_local; }
        //        if (cp.User.Id == 0) { return new List<NavItem>(); }
        //        if (!cp.User.IsAdmin && !cp.User.IsContentManager()) { return new List<NavItem>(); }
        //        //
        //        // -- read from cache, invidate if an admin click isnt found in recent table
        //        string cacheKey = cp.Cache.CreateKey("admin-nav-comm-list");
        //        navCommList_local = cp.Cache.GetObject<List<NavItem>>(cacheKey);
        //        if (navCommList_local != null) { return navCommList_local; }
        //        //
        //        navCommList_local = new List<NavItem> {
        //            //
        //            // -- consider selecting with navagator entry content
        //            //
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Group Email"),
        //                navItemName = "Group Email"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("System Email"),
        //                navItemName = "System Email"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Conditional Email"),
        //                navItemName = "Conditional Email"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Email Bounce List"),
        //                navItemName = "Email Bounce List"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Email Log"),
        //                navItemName = "Email Log"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Email Templates"),
        //                navItemName = "Email Templates"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?addonguid=" + encodeURL(addonGuidEmailDropReport),
        //                navItemName = cp.Utils.EncodeText("Email Drop Report")
        //            },
        //            //
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Group Text Messages"),
        //                navItemName = "Group Text Messages"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("System Text Messages"),
        //                navItemName = "System Text Messages"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Text Message Log"),
        //                navItemName = "Text Message Log"
        //            }
        //        };
        //        //
        //        // -- add the addons and content marked
        //        navCommList_local.AddRange(getNavItemsByType(NavTypeIdEnum.comm, ref _navToolsList));
        //        //
        //        // -- alpha sort
        //        navCommList_local.Sort((a, b) => a.navItemName.CompareTo(b.navItemName));
        //        //
        //        string depKey = cp.Cache.CreateTableDependencyKey(AddonModel.tableMetadata.tableNameLower);
        //        cp.Cache.Store(cacheKey, navCommList_local, depKey);
        //        return navCommList_local;
        //    }
        //}
        //private List<NavItem> navCommList_local;
        ////
        //public List<NavItem> navDesignList {
        //    get {
        //        if (navDesignList_local != null) { return navDesignList_local; }
        //        if (cp.User.Id == 0) { return new List<NavItem>(); }
        //        if (!cp.User.IsAdmin && !cp.User.IsContentManager()) { return new List<NavItem>(); }
        //        //
        //        // -- read from cache, invidate if an admin click isnt found in recent table
        //        string cacheKey = cp.Cache.CreateKey("admin-nav-design-list");
        //        navDesignList_local = cp.Cache.GetObject<List<NavItem>>(cacheKey);
        //        if (navDesignList_local != null) { return navDesignList_local; }
        //        navDesignList_local = new List<NavItem> {
        //            //
        //            // -- consider selecting with navagator entry content
        //            //
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Layouts"),
        //                navItemName = "Layouts"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Menus"),
        //                navItemName = "Menus"
        //            },
        //            new NavItem {
        //                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + cp.Content.GetID("Page Templates"),
        //                navItemName = "Templates"
        //            }
        //        };
        //        //
        //        // -- add the addons and content marked
        //        navDesignList_local.AddRange(getNavItemsByType(NavTypeIdEnum.design, ref _navToolsList));
        //        //
        //        // -- alpha sort
        //        navDesignList_local.Sort((a, b) => a.navItemName.CompareTo(b.navItemName));
        //        //
        //        string depKey = cp.Cache.CreateTableDependencyKey(AddonModel.tableMetadata.tableNameLower);
        //        cp.Cache.Store(cacheKey, navDesignList_local, depKey);
        //        return navDesignList_local;
        //    }
        //}
        //private List<NavItem> navDesignList_local;
        //
        public List<NavItem> navDomainList {
            get {
                if (navDomainList_Local != null) { return navDomainList_Local; }
                if (cp.User.Id == 0) { return new List<NavItem>(); }
                if (!cp.User.IsAdmin && !cp.User.IsContentManager()) { return new List<NavItem>(); }
                //
                navDomainList_Local = new List<NavItem>();
                foreach (DomainModel domain in DbBaseModel.createList<DomainModel>(cp, "(name<>'*')")) {
                    string navItemHref = domain.name.Contains("://") ? domain.name : "https://" + domain.name;
                    navDomainList_Local.Add(new NavItem {
                        navDivider = false,
                        navItemHref = navItemHref,
                        navItemName = navItemHref
                    });
                }
                return navDomainList_Local;
            }
        }
        private List<NavItem> navDomainList_Local = null;
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
                if (localRecentList != null) { return localRecentList; }
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
                if (user_local != null) { return user_local; }
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
                if (userOrganization_local != null) { return userOrganization_local; }
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
                return cp.core.addon.execute(cp.core.cacheRuntime.addonCache.create(AdminNavigatorGuid), new CPUtilsBaseClass.addonExecuteContext {
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
        /// <summary>
        /// if true, a divider is added and name/href ignored
        /// </summary>
        public bool navDivider { get; set; }
        /// <summary>
        /// if true, this navItemName is a category, boldeed and without link
        /// </summary>
        public bool isCategory { get; set; }
    }
    //
    public class NavItemList {
        public string listName { get; set; }
        public string listIcon { get; set; }
        public List<NavItem> listItemList { get; set; }
    }
}

