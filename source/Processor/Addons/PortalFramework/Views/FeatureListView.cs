using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.PortalFramework.Models.Domain;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.PortalFramework.Views {
    public class FeatureListView {
        //====================================================================================================
        /// <summary>
        /// create a feature list box
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        public static string getFeatureList(CPBaseClass cp, PortalDataModel portalData, PortalDataFeatureModel feature, string frameRqs) {
            try {
                //string activeNavHeading = feature.heading;
                string items = "";
                foreach (KeyValuePair<string, PortalDataFeatureModel> kvp in portalData.featureList) {
                    PortalDataFeatureModel liFeature = kvp.Value;
                    if (liFeature.parentFeatureId == feature.id && liFeature.parentFeatureId != 0) {
                        string featureHeading = liFeature.heading;
                        if (string.IsNullOrEmpty(featureHeading)) {
                            featureHeading = liFeature.name;
                        }
                        if (liFeature.dataContentId != 0) {
                            //
                            // -- display content button if content is not developeronly, or if this is a developer
                            ContentModel featureContent = DbBaseModel.create<ContentModel>(cp, liFeature.dataContentId);
                            if (featureContent != null) {
                                if (!featureContent.developerOnly || cp.User.IsDeveloper) {
                                    string qs = frameRqs;
                                    qs = cp.Utils.ModifyQueryString(qs, "addonid", "", false);
                                    qs = cp.Utils.ModifyQueryString(qs, Constants.rnDstFeatureGuid, "", false);
                                    qs = cp.Utils.ModifyQueryString(qs, "cid", liFeature.dataContentId.ToString());
                                    items += "<li><a target=\"_blank\" href=\"?" + qs + "\">" + featureHeading + "</a></li>";
                                }
                            }
                        } else {
                            string qs = cp.Utils.ModifyQueryString(frameRqs, Constants.rnDstFeatureGuid, liFeature.guid);
                            items += "<li><a href=\"?" + qs + "\">" + featureHeading + "</a></li>";
                        }
                    }
                }
                var content = cp.AdminUI.CreateLayoutBuilder();
                content.title = feature.heading;
                content.body = "<ul class=\"afwButtonList\">" + items + "</ul>";
                return content.getHtml();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "portalClass.getFeatureList exception");
                throw;
            }
        }
    }
}