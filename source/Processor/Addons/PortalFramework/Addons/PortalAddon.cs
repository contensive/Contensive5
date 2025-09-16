
using System;
using System.Collections.Generic;
using System.Linq;
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.PortalFramework.Models.Db;
using Contensive.Processor.Addons.PortalFramework.Models.Domain;
using Contensive.Processor.Addons.PortalFramework.Views;

namespace Contensive.Processor.Addons.PortalFramework.Addons {
    public class PortalAddon : AddonBaseClass {
        //====================================================================================================
        /// <summary>
        /// Addon interface. Run addon from doc property PortalGuid or PortalId (form, querystring, doc.setProperty())
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                //
                // get portal to display 
                string instanceId = cp.Doc.GetText("instanceid");
                string visitPropertyName = "portalForInstance" + instanceId;
                PortalModel portal = null;
                int portalId = cp.Doc.GetInteger(Constants.rnSetPortalId);
                if (!portalId.Equals(0)) { portal = DbBaseModel.create<PortalModel>(cp, portalId); }
                if (portal == null) {
                    //
                    // -- no setPortalId, try guid
                    string portalGuid = cp.Doc.GetText(Constants.rnSetPortalGuid);
                    if (!string.IsNullOrEmpty(portalGuid)) { portal = DbBaseModel.create<PortalModel>(cp, portalGuid); }
                    if (portal == null) {
                        //
                        // -- no setPortalGuid, try visit property
                        portalId = cp.Visit.GetInteger(visitPropertyName);
                        if (!portalId.Equals(0)) { portal = DbBaseModel.create<PortalModel>(cp, portalId); }
                        if (portal == null) {
                            //
                            // no visit property, try portal guid argument
                            portalGuid = cp.Doc.GetText("PortalGuid");
                            if (!string.IsNullOrEmpty(portalGuid)) { portal = DbBaseModel.create<PortalModel>(cp, portalGuid); }
                            if (portal == null) {
                                //
                                // try value from addon argument
                                portalId = cp.Doc.GetInteger("Portal");
                                if (!portalId.Equals(0)) { portal = DbBaseModel.create<PortalModel>(cp, portalId); }
                            }
                        }
                    }

                }
                cp.Doc.AddRefreshQueryString(Constants.rnSetPortalId, portal.id);
                cp.Visit.SetProperty(visitPropertyName, portal.id);
                return getPortalAddonHtml(cp, portal.id);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "portalClass exception");
                return "";
            }
        }
        //====================================================================================================
        /// <summary>
        /// get the portal html. portalId=0  returns the first order by id.
        /// </summary>
        /// <param name="CP"></param>
        /// <param name="portalSelectSqlCriteria"></param>
        /// <returns></returns>
        public string getPortalAddonHtml(CPBaseClass CP, int portalId) {
            try {
                if (!CP.User.IsAdmin) {
                    return Constants.blockedMessage;
                }
                PortalDataModel portalData = PortalDataModel.create(CP, portalId);
                //
                // -- dstFeature is the feature currently being viewed.
                string dstFeatureGuid = CP.Doc.GetText(Constants.rnDstFeatureGuid);
                //
                // -- Add Nav items
                PortalBuilderClass portalBuilder = new PortalBuilderClass();
                if (portalData.defaultFeature == null) {
                    //
                    // -- if no default feature, add'overview' to the nav and link to no feature, which will display dashboard
                    portalBuilder.addNav();
                    portalBuilder.navCaption = "Overview";
                    portalBuilder.navLink = CP.AdminUI.GetPortalFeatureLink(portalData.guid, "");
                }
                //
                // -- built top-level nav
                // -- features with no parent-feature
                bool foundActiveFeature = false;
                foreach (KeyValuePair<string, PortalDataFeatureModel> kvp in portalData.featureList) {
                    PortalDataFeatureModel feature = kvp.Value;
                    if (feature.parentFeatureId == 0) {
                        //
                        // -- feature has no parent, add to nav
                        var navFlyoutList = new List<PortalBuilderSubNavItemModel>();
                        if (feature.addonId == 0 && feature.dataContentId == 0) {
                            //
                            // -- parent feature has no addon or content, this is a non-feature that acts as a flyout with its child features
                            bool activeFeature = feature.guid == dstFeatureGuid;
                            foundActiveFeature = foundActiveFeature || activeFeature;
                            foreach (var subFeature in feature.subFeatureList) {
                                navFlyoutList.Add(new PortalBuilderSubNavItemModel {
                                    subActive = activeFeature,
                                    subCaption = string.IsNullOrEmpty(subFeature.heading) ? subFeature.name : subFeature.heading,
                                    subIsPortalLink = false,
                                    subLink = "?" + CP.Utils.ModifyQueryString(CP.Doc.RefreshQueryString, Constants.rnDstFeatureGuid, subFeature.guid),
                                    sublinkTarget = subFeature.dataContentId > 0 || !string.IsNullOrEmpty(subFeature.dataContentGuid) ? "_blank" : ""
                                });
                            }
                        }
                        portalBuilder.addNav(new PortalBuilderNavItemModel {
                            active = feature.guid == dstFeatureGuid,
                            caption = string.IsNullOrEmpty(feature.heading) ? feature.name : feature.heading,
                            isPortalLink = false,
                            link = "?" + CP.Utils.ModifyQueryString(CP.Doc.RefreshQueryString, Constants.rnDstFeatureGuid, feature.guid),
                            linkTarget = feature.dataContentId > 0 || !string.IsNullOrEmpty(feature.dataContentGuid) ? "_blank" : "",
                            navFlyoutList = navFlyoutList
                        });
                        portalBuilder.navCaption = string.IsNullOrEmpty(feature.heading) ? feature.name : feature.heading;
                        portalBuilder.navLink = "?" + CP.Utils.ModifyQueryString(CP.Doc.RefreshQueryString, Constants.rnDstFeatureGuid, feature.guid);
                    }
                }
                string body = "";
                string activeNavHeading = "";
                //
                // -- execute feature
                // -- if it returns empty, display default feature
                //
                if (portalData.featureList.ContainsKey(dstFeatureGuid)) {
                    //
                    // add feature guid to frameRqs so if the feature uses ajax, the featureGuid will be part of it
                    // add feature guid to rqs so if an addon is used that does not support frameRqs it will work
                    //
                    PortalDataFeatureModel dstDataFeature = portalData.featureList[dstFeatureGuid];
                    if (dstDataFeature.addonId != 0) {
                        //
                        // feature is an addon, execute it
                        //
                        CP.Doc.SetProperty(Constants.rnFrameRqs, CP.Doc.RefreshQueryString);
                        CP.Doc.AddRefreshQueryString(Constants.rnDstFeatureGuid, dstDataFeature.guid);
                        body = CP.Addon.Execute(dstDataFeature.addonId);
                        //
                        // -- portal title is a doc property set in each portal-builder that populates the title in the subnav.
                        portalBuilder.subNavTitleList.AddRange(CP.Doc.GetText("portalSubNavTitleList").Split('|'));
                        //
                    } else if (dstDataFeature.dataContentId != 0) {
                        //
                        // this is a data content feature -- should not be here, link should have taken them to the content
                        //
                        CP.Response.Redirect("?cid=" + dstDataFeature.dataContentId.ToString());
                        var content = CP.AdminUI.CreateLayoutBuilder();
                        content.title = string.IsNullOrEmpty(dstDataFeature.heading) ? dstDataFeature.name : dstDataFeature.heading;
                        content.body = "Redirecting to content";
                        body = content.getHtml();
                    } else {
                        //
                        // this is a feature list, display the feature list
                        //
                        body = FeatureListView.getFeatureList(CP, portalData, dstDataFeature, CP.Doc.RefreshQueryString);
                    }
                    ////
                    //// -- build the subnav, if the feature addon has sibling features, 
                    ////
                    //if (!string.IsNullOrEmpty(dstDataFeature.parentFeatureGuid)) {
                    //    if (portalData.featureList.ContainsKey(dstDataFeature.parentFeatureGuid)) {
                    //        PortalDataFeatureModel dstFeatureParent = portalData.featureList[dstDataFeature.parentFeatureGuid];
                    //        if (dstFeatureParent != null) {
                    //            if (dstFeatureParent.addonId > 0 || dstFeatureParent.dataContentId > 0) {
                    //                //
                    //                // -- if parent is content or addon features, show subnav (otherwise, show flyout)
                    //                var subFeatureList = dstFeatureParent.subFeatureList.OrderBy(p => p.sortOrder).ToList();
                    //                foreach (var dstFeatureSibling in subFeatureList) {
                    //                    bool activeFeature = dstFeatureSibling.guid == dstFeatureGuid;
                    //                    foundActiveFeature = foundActiveFeature || activeFeature;
                    //                    portalBuilder.addSubNav(new PortalBuilderSubNavItemModel {
                    //                        subActive = activeFeature,
                    //                        subCaption = string.IsNullOrEmpty(dstFeatureSibling.heading) ? dstFeatureSibling.name : dstFeatureSibling.heading,
                    //                        subIsPortalLink = false,
                    //                        subLink = "?" + CP.Utils.ModifyQueryString(CP.Doc.RefreshQueryString, Constants.rnDstFeatureGuid, dstFeatureSibling.guid),
                    //                        sublinkTarget = dstFeatureSibling.dataContentId > 0 || !string.IsNullOrEmpty(dstFeatureSibling.dataContentGuid) ? "_blank" : ""
                    //                    });
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    //
                    // -- build navs from the dstFeature, up to the top-nav
                    // -- then move the resulting navs to subnav, and childsubnavs
                    var targetDataFeature = dstDataFeature;
                    List<List<PortalBuilderSubNavItemModel>> subNavStack = [];
                    int loopLimit = 10;
                    int loopCnt = 0;
                    while (!string.IsNullOrEmpty(targetDataFeature.parentFeatureGuid)) {
                        if (loopCnt++ > loopLimit) {
                            CP.Site.ErrorReport("portalClass.getPortalAddonHtml infinite loop detected, limit reached");
                            break;
                        }
                        PortalDataFeatureModel targetDataParentFeature = portalData.featureList[targetDataFeature.parentFeatureGuid];
                        if ((targetDataParentFeature.addonId > 0 || targetDataParentFeature.dataContentId > 0) && (loopCnt>1 || targetDataParentFeature.subFeatureList.Count > 1)) {
                            //
                            // -- if parent is content or addon features, show subnav (otherwise, show flyout)
                            var navFeatures = new List<PortalBuilderSubNavItemModel>();
                            subNavStack.Add(navFeatures);

                            var subFeatureList = targetDataParentFeature.subFeatureList.OrderBy(p => p.sortOrder).ToList();
                            foreach (var dstFeatureSibling in subFeatureList) {
                                bool activeFeature = dstFeatureSibling.guid == targetDataFeature.guid;
                                foundActiveFeature = foundActiveFeature || activeFeature;
                                navFeatures.Add(new PortalBuilderSubNavItemModel {
                                    subActive = activeFeature,
                                    subCaption = string.IsNullOrEmpty(dstFeatureSibling.heading) ? dstFeatureSibling.name : dstFeatureSibling.heading,
                                    subIsPortalLink = false,
                                    subLink = "?" + CP.Utils.ModifyQueryString(CP.Doc.RefreshQueryString, Constants.rnDstFeatureGuid, dstFeatureSibling.guid),
                                    sublinkTarget = dstFeatureSibling.dataContentId > 0 || !string.IsNullOrEmpty(dstFeatureSibling.dataContentGuid) ? "_blank" : ""
                                });
                            }
                        }
                        targetDataFeature = targetDataParentFeature;
                    }
                    if (subNavStack.Count > 0) {
                        //
                        // -- if there is a nav stack, add the top nav to the subnav
                        //
                        subNavStack.Reverse();
                        portalBuilder.subNavItemList = subNavStack[0];
                        for (int i = 1; i < subNavStack.Count; i++) {
                            portalBuilder.childSubNavList.Add(new PortalBuilderClass.PortalBuilderChildSubNavListModel {
                                childSubNavItemList = subNavStack[i]
                            });
                        }
                    }
                    //
                    // -- set active heading
                    PortalDataFeatureModel dstFeatureRootFeature = PortalDataFeatureModel.getRootFeature(CP, dstDataFeature, portalData.featureList);
                    activeNavHeading = string.IsNullOrEmpty(dstFeatureRootFeature.heading) ? dstFeatureRootFeature.name : dstFeatureRootFeature.heading;
                    //
                    // -- if body not created, consider this a feature list
                    if (string.IsNullOrEmpty(body)) {
                        foreach (KeyValuePair<string, PortalDataFeatureModel> kvp in portalData.featureList) {
                            PortalDataFeatureModel parentFeature = kvp.Value;
                            if (parentFeature.id == dstDataFeature.parentFeatureId) {
                                //
                                // if feature returned empty and it is in a feature list, execute the feature list
                                body = FeatureListView.getFeatureList(CP, portalData, parentFeature, CP.Doc.RefreshQueryString);
                            }
                        }
                    }
                    //
                    // -- add pading
                    if (dstDataFeature.addPadding) {
                        body = CP.Html.div(body, "", "afwBodyPad", "");
                    }
                }
                if (string.IsNullOrEmpty(body)) {
                    //
                    // if the feature returns blank, run the default feature
                    //
                    if (portalData.defaultFeature != null) {
                        PortalDataFeatureModel feature = portalData.defaultFeature;
                        activeNavHeading = string.IsNullOrEmpty(feature.heading) ? feature.name : feature.heading;
                        CP.Doc.SetProperty(Constants.rnFrameRqs, CP.Doc.RefreshQueryString);
                        CP.Doc.AddRefreshQueryString(Constants.rnDstFeatureGuid, feature.guid);
                        body = CP.Addon.Execute(feature.addonId);
                        if (feature.addPadding) {
                            body = CP.Html.div(body, "", "afwBodyPad", "");
                        }
                    }
                    if (string.IsNullOrEmpty(body)) {
                        //
                        // -- if no default feature set, display dashboard for this portal
                        body = CP.AdminUI.GetWidgetDashboard(portalData.guid);
                        //LayoutBuilderSimple simple = new LayoutBuilderSimple {
                        //    body = "This portal feature has no content."
                        //};
                        //body = simple.getHtml(CP);
                    }
                }
                portalBuilder.setActiveNav(activeNavHeading);
                //
                //Assemble
                //
                portalBuilder.body = CP.Html.div(body, "", "", "afwBodyFrame");
                portalBuilder.title = portalData.name;
                portalBuilder.isOuterContainer = true;
                string returnHtml = portalBuilder.getHtml(CP);
                //
                // assemble body
                //
                //CP.Doc.AddHeadStyle(Properties.Resources.styles);
                CP.Doc.AddHeadJavascript("var afwFrameRqs='" + CP.Doc.RefreshQueryString + "';");
                //CP.Doc.AddHeadJavascript(Properties.Resources.javascript);
                //
                return returnHtml;
            } catch (Exception ex) {
                CP.Site.ErrorReport(ex, "portalClass");
                throw;
            }
        }
    }
}