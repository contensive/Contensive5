
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        /// <summary>
        /// the count of site warnings
        /// </summary>
        public string siteWarningCount {
            get {
                int count = DbBaseModel.getCount<SiteWarningModel>(cp, "");
                if (count == 0) { return ""; }
                return count.ToString();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// the link to site warnings
        /// </summary>
        public string siteWarningLink {
            get {
                if (string.IsNullOrEmpty(siteWarningCount)) { return ""; }
                return $"{cp.GetAppConfig().adminRoute}?cid={cp.Content.GetID("site warnings")}";
            }
        }
        //
        //====================================================================================================
        //
        /// <summary>
        /// create the hardcoded list of 6 icons that appear at the top-right of the admin page
        /// </summary>
        public List<CategoryNavItem> categoryNavList {
            get {
                if (_categoryList != null) { return _categoryList; }
                if (cp.User.Id == 0) { return new List<CategoryNavItem>(); }
                if (!cp.User.IsAdmin && !cp.User.IsContentManager()) { return new List<CategoryNavItem>(); }
                //
                // -- read from cache, invidate if an admin click isnt found in recent table
                string cacheKey = cp.Cache.CreateKey($"admin-categoryList");
                var cacheData = cp.Cache.GetObject<List<CategoryNavItem>>(cacheKey);
                if (cacheData != null) {
                    _categoryList = new();
                    _categoryList.AddRange(cacheData);
                    _categoryList.Add(new CategoryNavItem {
                        categoryName = "User",
                        categoryIcon = "fas fa-user fa-lg",
                        categoryNavColumnList = new List<CategoryNavColumn>() { navProfileCategoryList }
                    });
                    return _categoryList;
                }
                //
                cacheData = new() {
                    new CategoryNavItem {
                        categoryName = "Content",
                        categoryIcon = "fas fa-edit fa-lg",
                        categoryNavColumnList = getNavColumnList(NavTypeIdEnum.content)
                    },
                    new CategoryNavItem {
                        categoryName = "System",
                        categoryIcon = "fas fa-desktop fa-lg",
                        categoryNavColumnList = getNavColumnList(NavTypeIdEnum.system)
                    },
                    new CategoryNavItem {
                        categoryName = "Design",
                        categoryIcon = "fas fa-paint-brush fa-lg",
                        categoryNavColumnList = getNavColumnList(NavTypeIdEnum.design)
                    },
                    new CategoryNavItem {
                        categoryName = "Comm",
                        categoryIcon = "fas fa-comment-alt fa-lg",
                        categoryNavColumnList = getNavColumnList(NavTypeIdEnum.comm)
                    },
                    new CategoryNavItem {
                        categoryName = "Reports",
                        categoryIcon = "fas fa-chart-pie fa-lg",
                        categoryNavColumnList = getNavColumnList(NavTypeIdEnum.report)
                    },
                    new CategoryNavItem {
                        categoryName = "Tools",
                        categoryIcon = "fas fa-wrench fa-lg",
                        categoryNavColumnList = getNavColumnList(NavTypeIdEnum.tool)
                    },
                    new CategoryNavItem {
                        categoryName = "Settings",
                        categoryIcon = "fas fa-cog fa-lg",
                        categoryNavColumnList = getNavColumnList(NavTypeIdEnum.setting)
                    }
                };
                //
                string depKey = cp.Cache.CreateTableDependencyKey(AddonModel.tableMetadata.tableNameLower);
                cp.Cache.Store(cacheKey, cacheData, depKey);
                //
                _categoryList = new();
                _categoryList.AddRange(cacheData);
                _categoryList.Add(new CategoryNavItem {
                    categoryName = "User",
                    categoryIcon = "fas fa-user fa-lg",
                    categoryNavColumnList = new List<CategoryNavColumn>() { navProfileCategoryList }
                });
                return _categoryList;
            }
        }
        private List<CategoryNavItem> _categoryList = null;
        ////
        ////====================================================================================================
        ////
        ///// <summary>
        ///// create the hardcoded list of 6 icons that appear at the top-right of the admin page
        ///// </summary>
        //public List<NavItemList> navList {
        //    get {
        //        if (_navList != null) {
        //            _navList.Add(new NavItemList {
        //                listName = "User",
        //                listIcon = "fas fa-user fa-lg",
        //                listItemList = navProfileItemList
        //            });
        //            return _navList; 
        //        }
        //        if (cp.User.Id == 0) { return new List<NavItemList>(); }
        //        if (!cp.User.IsAdmin && !cp.User.IsContentManager()) { return new List<NavItemList>(); }
        //        //
        //        // -- read from cache, invidate if an admin click isnt found in recent table
        //        string cacheKey = cp.Cache.CreateKey($"admin-navlist");
        //        _navList = cp.Cache.GetObject<List<NavItemList>>(cacheKey);
        //        if (_navList != null) { return _navList; }
        //        //
        //        _navList = new() {
        //            new NavItemList {
        //                listName = "Design",
        //                listIcon = "fas fa-paint-brush fa-lg",
        //                listItemList = getNavItemsByType(NavTypeIdEnum.design)
        //            },
        //            new NavItemList {
        //                listName = "Comm",
        //                listIcon = "fas fa-comment-alt fa-lg",
        //                listItemList = getNavItemsByType(NavTypeIdEnum.comm)
        //            },
        //            new NavItemList {
        //                listName = "Reports",
        //                listIcon = "fas fa-chart-pie fa-lg",
        //                listItemList = getNavItemsByType(NavTypeIdEnum.report)
        //            },
        //            new NavItemList {
        //                listName = "Tools",
        //                listIcon = "fas fa-wrench fa-lg",
        //                listItemList = getNavItemsByType(NavTypeIdEnum.tool)
        //            },
        //            new NavItemList {
        //                listName = "Settings",
        //                listIcon = "fas fa-cog fa-lg",
        //                listItemList = getNavItemsByType(NavTypeIdEnum.setting)
        //            }                    
        //        };

        //        //
        //        string depKey = cp.Cache.CreateTableDependencyKey(AddonModel.tableMetadata.tableNameLower);
        //        cp.Cache.Store(cacheKey, _navList, depKey);
        //        _navList.Add(new NavItemList {
        //            listName = "User",
        //            listIcon = "fas fa-user fa-lg",
        //            listItemList = navProfileItemList
        //        });
        //        return _navList;
        //    }
        //}
        //private List<NavItemList> _navList = null;
        //
        //====================================================================================================
        //
        /// <summary>
        /// build navCategoryList
        /// </summary>
        /// <param name="navTypeId"></param>
        /// <param name="localListCache"></param>
        /// <returns></returns>
        public List<CategoryNavColumn> getNavColumnList(NavTypeIdEnum navTypeId) {
            try {
                //
                // -- create a column list with one column per category, move advanced to last
                List<CategoryNavColumn> columnList = new();
                using (DataTable dt = cp.Db.ExecuteQuery(Properties.Resources.sqlGetNavItemByType.replace("{navTypeId}", ((int)navTypeId).ToString(), System.StringComparison.CurrentCultureIgnoreCase))) {
                    if (dt?.Rows != null) {
                        string categoryNameLast = "";
                        CategoryNavColumn column = new() {
                            categoryNavColumnItemList = []
                        };
                        foreach (DataRow dr in dt.Rows) {
                            string categoryName = cp.Utils.EncodeText(dr["categoryName"]);
                            categoryName = string.IsNullOrEmpty(categoryName) ? "" : categoryName.replace(".", " ", System.StringComparison.InvariantCultureIgnoreCase);
                            string navHeaderName = "";
                            if (columnList.Count == 0 || categoryName != categoryNameLast) {
                                categoryNameLast = categoryName;
                                //
                                // -- new column
                                column = new() {
                                    categoryNavColumnItemList = []
                                };
                                columnList.Add(column);
                                navHeaderName = categoryName;
                            }
                            //
                            // -- create entry
                            int id = cp.Utils.EncodeInteger(dr["id"]);
                            string navItemHref = "";
                            string navItemDataDragId = "";
                            if (cp.Utils.EncodeBoolean(dr["isContent"])) {
                                //
                                // -- data
                                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + id;
                                navItemDataDragId = id > 0 ? $"c{id}" : "";
                            } else {
                                //
                                // -- addon
                                navItemHref = cp.GetAppConfig().adminRoute + "?addonguid=" + encodeURL(cp.Utils.EncodeText(dr["ccguid"]));
                                navItemDataDragId = id > 0 ? $"a{id}" : "";
                            }
                            //var navCategoryItem = new NavCategoryItem {
                            //    navItemName = cp.Utils.EncodeText(dr["name"]),
                            //    navItemDataDragId = navItemDataDragId,
                            //    navItemHref = (cp.Utils.EncodeBoolean(dr["isContent"]) ? "?cid=" + id : "?addonguid=" + encodeURL(cp.Utils.EncodeText(dr["ccguid"])))
                            //};
                            column.categoryNavColumnItemList.Add(new CategoryNavColumnItem {
                                navHeaderName = navHeaderName,
                                navItemName = cp.Utils.EncodeText(dr["name"]),
                                navItemDataDragId = navItemDataDragId,
                                navItemHref = (cp.Utils.EncodeBoolean(dr["isContent"]) ? "?cid=" + id : "?addonguid=" + encodeURL(cp.Utils.EncodeText(dr["ccguid"])))
                            });
                        }
                    }
                }
                //
                // -- if mulitple columns and the first columns header is blank, make is Misc
                if (columnList.Count > 1 && string.IsNullOrEmpty(columnList[0].categoryNavColumnItemList[0].navHeaderName)) {
                    columnList[0].categoryNavColumnItemList[0].navHeaderName = "Misc";
                }
                //
                // -- move any column with navColumnName "advanced" to the end of the columnlist
                List<CategoryNavColumn> columnList2 = [];
                foreach (var column in columnList.AsEnumerable().Reverse()) {
                    if (column.categoryNavColumnItemList.Count > 0) {
                        //
                        // -- move advanced to the end
                        if (column.categoryNavColumnItemList[0].navHeaderName.Equals("advanced", StringComparison.OrdinalIgnoreCase)) {
                            columnList2.Add(column);
                        } else {
                            columnList2.Insert(0, column);
                        }
                    }
                }
                if (columnList2.Count <= 4) {
                    return columnList2;
                }
                //
                // -- combine the shortest list with the one after it
                // -- if the last column is the shortest, move it left one and combine it with the one to the left.
                // -- over and over
                do {
                    // -- find the shortest column
                    int shortestColumnIndex = 0;
                    int shortestColumn = 9999;
                    foreach (var column in columnList2) {
                        if (column.categoryNavColumnItemList.Count < shortestColumn) {
                            shortestColumn = column.categoryNavColumnItemList.Count;
                            shortestColumnIndex = columnList2.IndexOf(column);
                        }
                    }
                    // -- if shortest is the last column, swap it with the next-to-last column
                    if (shortestColumnIndex == columnList2.Count - 1) {
                        //
                        // -- swap the last column with the one before it
                        var temp = columnList2[shortestColumnIndex];
                        columnList2[shortestColumnIndex] = columnList2[shortestColumnIndex - 1];
                        columnList2[shortestColumnIndex - 1] = temp;
                        shortestColumnIndex--;
                    }
                    // -- combine the shortest column with the next one
                    columnList2[shortestColumnIndex].categoryNavColumnItemList.AddRange(columnList2[shortestColumnIndex + 1].categoryNavColumnItemList);
                    columnList2.RemoveAt(shortestColumnIndex + 1);
                } while (columnList2.Count > 4);
                return columnList2;
            } catch (System.Exception ex) {
                cp.Site.ErrorReport(ex, "getNavCategoriesByType");
                return new();
            }
        }
        //
        //====================================================================================================
        //
        /// <summary>
        /// guild navItemList
        /// </summary>
        /// <param name="navTypeId"></param>
        /// <returns></returns>
        public List<NavItem> getNavItecmsByType(NavTypeIdEnum navTypeId) {
            try {
                List<NavItem> localListCache = new();
                using (DataTable dt = cp.Db.ExecuteQuery(Properties.Resources.sqlGetNavItemByType.replace("{navTypeId}", ((int)navTypeId).ToString(), System.StringComparison.CurrentCultureIgnoreCase))) {
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
                            int id = cp.Utils.EncodeInteger(dr["id"]);
                            string navItemDataDragId = "";
                            string navItemHref = "";
                            if (cp.Utils.EncodeBoolean(dr["isContent"])) {
                                //
                                // -- data
                                navItemHref = cp.GetAppConfig().adminRoute + "?cid=" + id;
                                navItemDataDragId = id > 0 ? $"c{id}" : "";
                            } else {
                                //
                                // -- addon
                                navItemHref = cp.GetAppConfig().adminRoute + "?addonguid=" + encodeURL(cp.Utils.EncodeText(dr["ccguid"]));
                                navItemDataDragId = id > 0 ? $"a{id}" : "";
                            }
                            localListCache.Add(new NavItem {
                                navItemHref = navItemHref,
                                navItemName = entryName,
                                isCategory = false,
                                navItemDataDragId = navItemDataDragId
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
        public CategoryNavColumn navProfileCategoryList {
            get {
                if (_navProfileCategoryList != null) { return _navProfileCategoryList; }
                //
                string cacheKey = cp.Cache.CreateKey($"admin-nav-category-profileList-user{cp.User.Id}");
                _navProfileCategoryList = cp.Cache.GetObject<CategoryNavColumn>(cacheKey);
                if (_navProfileCategoryList != null) { return _navProfileCategoryList; }
                //
                string orgName = DbBaseModel.getRecordName<OrganizationModel>(cp, cp.User.OrganizationID);
                int orgCid = cp.Content.GetID("organizations");
                int peopleCid = cp.Content.GetID("people");
                int groupCid = cp.Content.GetID("groups");
                _navProfileCategoryList = new CategoryNavColumn {
                    categoryNavColumnItemList = new List<CategoryNavColumnItem> {
                        new CategoryNavColumnItem {
                            navItemName = cp.User.Name,
                            navItemHref = "?af=4&cid=" + cp.Content.GetID("people") + "&id=" + cp.User.Id
                        },
                        new CategoryNavColumnItem {
                            navItemName = "Logout",
                            navItemHref = "?method=logout"
                        },
                        new CategoryNavColumnItem {
                            navItemName = "Impersonate",
                            navItemHref = "/impersonate"
                        },
                        new CategoryNavColumnItem {
                            navItemName = "Groups",
                            navItemHref = "?cid=" + groupCid,
                            navItemDataDragId = $"c{groupCid}"
                        },
                        new CategoryNavColumnItem {
                            navItemName = "Organizations",
                            navItemHref = "?cid=" + orgCid,
                            navItemDataDragId = $"c{orgCid}"
                        },
                        new CategoryNavColumnItem {
                            navItemName = "People",
                            navItemHref = "?cid=" + peopleCid,
                            navItemDataDragId = $"c{peopleCid}"
                        }
                     }
                };
                if (cp.User.IsDeveloper) {
                    //
                    // -- beta the Grid-Stack Dashboard
                    if (cp.Doc.IsProperty("dashbeta")) {
                        //
                        // -- change dashbeta setting
                        AddonModel addon = null;
                        if (cp.Doc.GetBoolean("dashbeta")) {
                            //
                            // -- turn on the dashbeta
                            addon = DbBaseModel.create<AddonModel>(cp, Contensive.Processor.Constants.addonGuidGridStackDemoDashboard);
                            cp.Site.SetProperty("Admin Nav Portal Beta", true);
                        } else {
                            //
                            // -- turn off the dashbeta
                            addon = DbBaseModel.create<AddonModel>(cp, Contensive.Processor.Constants.addonGuidDashboard);
                            cp.Site.SetProperty("Admin Nav Portal Beta", false);
                        }
                        if (addon is not null) {
                            cp.Site.SetProperty("ADMINROOTADDONID", addon.id);
                        }
                    }
                    int dashboardAddonid = cp.Site.GetInteger("ADMINROOTADDONID");
                    if (cp.Content.GetRecordGuid("add-ons", dashboardAddonid) == Contensive.Processor.Constants.addonGuidDashboard) {
                        //
                        // -- link to switch to beta
                        _navProfileCategoryList.categoryNavColumnItemList.Add(new CategoryNavColumnItem {
                            navItemName = "Try Beta Dashboard",
                            navItemHref = "?dashbeta=1",
                            navItemDataDragId = $""
                        });
                    } else {
                        //
                        // -- link to switch to icon-dashboard
                        _navProfileCategoryList.categoryNavColumnItemList.Add(new CategoryNavColumnItem {
                            navItemName = "Return to Icon Dash",
                            navItemHref = "?dashbeta=0",
                            navItemDataDragId = $""
                        });
                    }
                }
                List<string> cacheKeyList = [
                    cp.Cache.CreateTableDependencyKey(OrganizationModel.tableMetadata.tableNameLower)
                ];
                cp.Cache.Store(cacheKey, _navProfileCategoryList, DateTime.Now, cacheKeyList);
                return _navProfileCategoryList;
            }
        }

        private CategoryNavColumn _navProfileCategoryList = null;
        //
        //====================================================================================================
        //
        /// <summary>
        /// hardcoded profile nav item in admin site top-right
        /// </summary>
        public List<NavItem> navProfileItemList {
            get {
                if (_navProfileList != null) { return _navProfileList; }
                //
                string cacheKey = cp.Cache.CreateKey($"admin-nav-profileList-user{cp.User.Id}");
                _navProfileList = cp.Cache.GetObject<List<NavItem>>(cacheKey);
                if (_navProfileList != null) { return _navProfileList; }
                //
                string orgName = DbBaseModel.getRecordName<OrganizationModel>(cp, cp.User.OrganizationID);
                int peopleContentId = cp.Content.GetID("people");
                var navList = new List<NavItem> {
                    new NavItem {
                        navItemName = cp.User.Name,
                        navItemHref = $"?af=4&cid={peopleContentId}&id=" + cp.User.Id
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
                int orgContentId = cp.Content.GetID("organizations");
                if (!string.IsNullOrEmpty(orgName)) {
                    navList.Add(new NavItem {
                        navItemName = orgName,
                        navItemHref = $"?af=4&cid={orgContentId}&id=" + cp.User.OrganizationID
                    });
                }
                navList.Add(new NavItem {
                    navDivider = true
                });
                int groupContentId = cp.Content.GetID("organizations");
                navList.Add(new NavItem {
                    navItemName = "Groups",
                    navItemHref = $"?cid={groupContentId}",
                    navItemDataDragId = $"c{groupContentId}"
                });
                navList.Add(new NavItem {
                    navItemName = "Organizations",
                    navItemHref = $"?cid={orgContentId}",
                    navItemDataDragId = $"c{orgContentId}"
                });
                navList.Add(new NavItem {
                    navItemName = "People",
                    navItemHref = $"?cid={peopleContentId}",
                    navItemDataDragId = $"c{peopleContentId}"
                });
                List<string> cacheKeyList = new List<string> {
                    cp.Cache.CreateTableDependencyKey(OrganizationModel.tableMetadata.tableNameLower)
                };
                cp.Cache.Store(cacheKey, _navProfileList, DateTime.Now, cacheKeyList);
                return navList;
            }
        }
        private List<NavItem> _navProfileList = null;

        //
        public List<NavItem> navDomainList {
            get {
                if (navDomainList_Local != null) { return navDomainList_Local; }
                navDomainList_Local = [];
                //
                if (cp.User.Id == 0) { return []; }
                if (!cp.User.IsAdmin && !cp.User.IsContentManager()) { return []; }
                //
                foreach (string domainName in DomainModel.getPublicDomains(cp)) {
                    string navItemHref = domainName.Contains("://") ? domainName : (cp.Request.Secure ? "https://" : "http://") + domainName;
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
                using (DataTable dt = cp.Db.ExecuteQuery("select top 20 name,href,addonid,contentid from ccAdminRecents where userId=" + cp.User.Id + " order by modifiedDate desc")) {
                    if (dt?.Rows != null) {
                        foreach (DataRow dr in dt.Rows) {
                            int addonId = cp.Utils.EncodeInteger(dr["addonid"]);
                            int contentId = cp.Utils.EncodeInteger(dr["contentid"]);
                            localRecentList.Add(new NavItem {
                                navItemHref = cp.Utils.EncodeText(dr["href"]),
                                navItemName = cp.Utils.EncodeText(dr["name"]),
                                navItemDataDragId = addonId > 0 ? $"a{addonId}" : (contentId > 0 ? $"c{contentId}" : "")
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
                return AdminNav.get(cp);
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
    /// <summary>
    /// admin top-right nav icons construction
    /// alternative-2
    /// Create a list of categories that each contain a name and a list of items
    /// </summary>
    public class CategoryNavColumn {
        public List<CategoryNavColumnItem> categoryNavColumnItemList { get; set; }
    }
    public class CategoryNavColumnItem {
        /// <summary>
        /// creates a bold header in the column above this item. Originally at the top, but to make them stack it was moved into the item.
        /// </summary>
        public string navHeaderName { get; set; }
        /// <summary>
        /// href for this item, typically a link to the admin page for this item
        /// </summary>
        public string navItemHref { get; set; }
        /// <summary>
        /// item name as it appears in the list
        /// </summary>
        public string navItemName { get; set; }
        /// <summary>
        /// data-drag-id for this item, used by the drag and drop editor to identify this item
        /// </summary>
        public string navItemDataDragId { get; set; }
    }
    //
    public class CategoryNavItem {
        public string categoryName { get; set; }
        public string categoryIcon { get; set; }
        public List<CategoryNavColumn> categoryNavColumnList { get; set; }
    }
    //
    /// <summary>
    ///  admin top-right nav icons construction.
    ///  Alternative-1
    ///  Create a list of items that might be items and might be category entries
    /// </summary>
    public class NavItem {
        public string navItemHref { get; set; }
        public string navItemName { get; set; }
        public string navItemDataDragId { get; set; }
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

