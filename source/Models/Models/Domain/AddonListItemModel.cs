
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
//
namespace Contensive.Processor.Models {
    /// <summary>
    /// The structure of an included addon list, stored in page and template records
    /// get and put remote methods in Processor addons
    /// </summary>
    [System.Serializable]
    public class AddonListItemModel {
        /// <summary>
        /// The guid of the design block addon that is in this position
        /// </summary>
        public string designBlockTypeGuid;
        /// <summary>
        /// The name of the design block to be used for non-rendered mode
        /// </summary>
        public string designBlockTypeName;
        /// <summary>
        /// the Guid of the data instance in this position
        /// </summary>
        public string instanceGuid;
        /// <summary>
        /// todo - create view model separate from domain model because UI mode might need it
        /// Not saved in DB, use only for view. 
        /// </summary>
        public string renderedHtml;
        /// <summary>
        /// Assets added to the html document during html rendering
        /// </summary>
        public AddonAssetsModel renderedAssets;
        /// <summary>
        /// If this design block is structural, it contains one or more addon lists
        /// </summary>
        public List<AddonListColumnItemModel> columns;
        /// <summary>
        /// If the addon has a settings record, this is the admin edit url to be used on the UI
        /// </summary>
        public string settingsEditUrl;
        /// <summary>
        /// if the addon can be edited, this is the url to the admin sits
        /// </summary>
        public string addonEditUrl;
        //
        //====================================================================================================
        /// <summary>
        /// Render the html,css,js for an addonListItem
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="addonListItem"></param>
        /// <param name="executeContext"></param>
        public static string render(CPBaseClass cp, AddonListItemModel addonListItem, CPUtilsBaseClass.addonExecuteContext executeContext) {
            if (addonListItem == null) {
                return string.Empty;
            }
            cp.Doc.SetProperty("instanceId", addonListItem.instanceGuid);
            return cp.Addon.Execute(addonListItem.designBlockTypeGuid, executeContext) ?? string.Empty;
        }
        //
        //====================================================================================================
        /// <summary>
        /// render all the renderHtml, css, js nodes in an addon list
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="addonList"></param>
        /// <param name="executeContext"></param>
        public static string render(CPBaseClass cp, List<AddonListItemModel> addonList, CPUtilsBaseClass.addonExecuteContext executeContext) {
            if (addonList == null) {
                return string.Empty;
            }
            bool isEditing = cp.User.IsEditing();
            var result = new StringBuilder();
            int addonIndex = 0;
            foreach (var addon in addonList) {
                var addonHtml = render(cp, addon, executeContext);
                if (isEditing && string.IsNullOrWhiteSpace(addonHtml)) {
                    //
                    // -- in edit mode, widget should always show something visible
                    // -- check if addon exists to distinguish "not found" from "empty render"
                    var addonRecord = DbBaseModel.create<AddonModel>(cp, addon.designBlockTypeGuid);
                    string widgetName = WebUtility.HtmlEncode(string.IsNullOrEmpty(addon.designBlockTypeName) ? "Unknown Widget" : addon.designBlockTypeName);
                    string widgetGuid = WebUtility.HtmlEncode(addon.designBlockTypeGuid ?? "");
                    string instanceGuid = WebUtility.HtmlEncode(addon.instanceGuid ?? "");
                    int pageId = cp.Doc.PageId;
                    string errorMsg = (addonRecord == null)
                        ? $"<strong>Widget Not Found:</strong> \"{widgetName}\" &mdash; the addon may have been deleted or its collection uninstalled."
                        : $"<strong>Widget Rendered Empty:</strong> \"{widgetName}\" &mdash; the addon returned no content. It may have encountered an error.";
                    string errorStyle = (addonRecord == null)
                        ? "background-color:#f8d7da;border-color:#f5c6cb;color:#721c24;"
                        : "background-color:#fff3cd;border-color:#ffc107;color:#856404;";
                    addonHtml = $"<div style=\"padding:16px;margin:8px 0;{errorStyle}border:1px solid;border-radius:4px;font-family:sans-serif;font-size:14px;\">"
                        + errorMsg
                        + $" <span style=\"font-size:0.85em;color:#6c757d;\">({widgetGuid})</span>"
                        + $"<form method=\"post\" action=\"/saveeditmodal\" style=\"display:inline;margin-left:12px;\">"
                        + $"<input type=\"hidden\" name=\"button\" value=\"deleteWidget\" />"
                        + $"<input type=\"hidden\" name=\"contentGuid\" value=\"{{02203F58-0921-4F75-A089-0EAD6E8FEB4F}}\" />"
                        + $"<input type=\"hidden\" name=\"recordGuid\" value=\"{instanceGuid}\" />"
                        + $"<input type=\"hidden\" name=\"pageid\" value=\"{pageId}\" />"
                        + $"<button type=\"submit\" style=\"background:#dc3545;color:white;border:none;padding:4px 12px;border-radius:3px;cursor:pointer;font-size:0.9em;\""
                        + $" onclick=\"return confirm('Remove this widget from the page?');\">Remove Widget</button>"
                        + $"</form>"
                        + $"</div>";
                }
                if (addon.columns != null) {
                    int colPtr = 1;
                    foreach (var column in addon.columns) {
                        string replaceTarget = $"<!-- column-{colPtr} -->";
                        string renderedColumn = render(cp, column.addonList, executeContext);
                        if (isEditing) {
                            renderedColumn = $"<div class=\"pb-column\" data-pb-column-index=\"{colPtr - 1}\" data-pb-parent-instance-guid=\"{addon.instanceGuid}\">{renderedColumn}</div>";
                        }
                        addonHtml = addonHtml.Replace(replaceTarget, renderedColumn);
                        colPtr++;
                    }
                }
                if (isEditing) {
                    result.Append($"<div class=\"pb-page-widget\" data-pb-addon-index=\"{addonIndex}\" data-pb-instance-guid=\"{addon.instanceGuid}\">{addonHtml}</div>");
                } else {
                    result.Append(addonHtml);
                }
                addonIndex++;
            }
            return result.ToString();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Render for editing the html,css,js for an addonListItem
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="addonListItem"></param>
        /// <param name="Addon"></param>
        /// <param name="executeContext"></param>
        public static void renderEdit(CPBaseClass cp, AddonListItemModel addonListItem, AddonModel Addon, CPUtilsBaseClass.addonExecuteContext executeContext) {
            int assetSkipCnt = cp.Doc.HtmlAssetList.Count;
            if (Addon != null) {
                //
                // -- edit place holder. Use if addon cannot or should not render in editor
                if (string.IsNullOrEmpty(Addon.editPlaceholderHtml)) {
                    addonListItem.renderedHtml = render(cp, addonListItem, executeContext);
                } else {
                    addonListItem.renderedHtml = Addon.editPlaceholderHtml;
                }
                addonListItem.renderedAssets = new AddonAssetsModel();
                string styleslink = Addon.stylesLinkHref;
                if (!string.IsNullOrEmpty(styleslink)) {
                    addonListItem.renderedAssets.headStylesheetLinks.Add(styleslink);
                }
                if (!string.IsNullOrEmpty(Addon.stylesFilename.filename)) {
                    addonListItem.renderedAssets.headStyles.Add(Addon.stylesFilename.filename);
                }
                //
                // -- settings edit icon
                int contentId = Addon.instanceSettingPrimaryContentId ?? 0;
                if (!contentId.Equals(0)) {
                    string contentName = cp.Content.GetRecordName("content", contentId);
                    if (!string.IsNullOrEmpty(contentName)) {
                        using (CPCSBaseClass csRecord = cp.CSNew()) {
                            if (csRecord.Open(contentName, "ccguid=" + cp.Db.EncodeSQLText(addonListItem.instanceGuid), "id", true, "id")) {
                                addonListItem.settingsEditUrl = getAdminEditUrl(cp, contentId, addonListItem.instanceGuid);
                            }
                        }
                    }
                }
                //
                // -- addon edit icon
                if (!Addon.blockEditTools) {
                    addonListItem.addonEditUrl = "";
                }
            } else {
                //
                // -- addon not found in database, show error in edit mode
                string widgetName = WebUtility.HtmlEncode(string.IsNullOrEmpty(addonListItem.designBlockTypeName) ? "Unknown Widget" : addonListItem.designBlockTypeName);
                string widgetGuid = WebUtility.HtmlEncode(addonListItem.designBlockTypeGuid ?? "");
                addonListItem.renderedHtml = "<div style=\"padding:16px;margin:8px 0;background-color:#f8d7da;border:1px solid #f5c6cb;border-radius:4px;color:#721c24;font-family:sans-serif;font-size:14px;\">"
                    + $"<strong>Widget Not Found:</strong> \"{widgetName}\" &mdash; the addon may have been deleted or its collection uninstalled."
                    + $" <span style=\"font-size:0.85em;color:#6c757d;\">({widgetGuid})</span>"
                    + "</div>";
                addonListItem.renderedAssets = new AddonAssetsModel();
            }
            //
            // -- if addon existed but rendered empty, show a warning in edit mode
            if (Addon != null && string.IsNullOrWhiteSpace(addonListItem.renderedHtml)) {
                string widgetName = WebUtility.HtmlEncode(string.IsNullOrEmpty(Addon.name) ? "Unknown Widget" : Addon.name);
                addonListItem.renderedHtml = "<div style=\"padding:16px;margin:8px 0;background-color:#fff3cd;border:1px solid #ffc107;border-radius:4px;color:#856404;font-family:sans-serif;font-size:14px;\">"
                    + $"<strong>Widget Rendered Empty:</strong> \"{widgetName}\" &mdash; the addon returned no content. It may have encountered an error."
                    + "</div>";
            }
            foreach (var asset in cp.Doc.HtmlAssetList.Skip(assetSkipCnt)) {
                string key = ((asset.assetType == CPDocBaseClass.HtmlAssetTypeEnum.script) ? "js" : "css");
                key += ((asset.inHead) ? "-head" : "-body");
                key += ((asset.isLink) ? "-link" : "-inline");
                switch (key) {
                    case "js-head-link":
                        addonListItem.renderedAssets.headJsLinks.Add(asset.content);
                        break;
                    case "js-head-inline":
                        addonListItem.renderedAssets.headJs.Add(asset.content);
                        break;
                    case "js-body-link":
                        addonListItem.renderedAssets.bodyJsLinks.Add(asset.content);
                        break;
                    case "js-body-inline":
                        addonListItem.renderedAssets.bodyJs.Add(asset.content);
                        break;
                    case "css-head-link":
                    case "css-body-link":
                        addonListItem.renderedAssets.headStylesheetLinks.Add(asset.content);
                        break;
                    case "css-head-inline":
                    case "css-body-inline":
                        addonListItem.renderedAssets.headStyles.Add(asset.content);
                        break;
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// render for editing all the renderHtml, css, js nodes in an addon list
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="addonList"></param>
        /// <param name="executeContext"></param>
        public static void renderEdit(CPBaseClass cp, List<AddonListItemModel> addonList, CPUtilsBaseClass.addonExecuteContext executeContext) {
            if (addonList == null) {
                return;
            }
            foreach (var addonListItem in addonList) {
                if (addonListItem.columns != null) {
                    foreach (var column in addonListItem.columns) {
                        renderEdit(cp, column.addonList, executeContext);
                    }
                }
                //
                // -- open addon model, depend on model caching not public static because persistence of the appDomain is too long and will not invalidate
                AddonModel addon = DbBaseModel.create<AddonModel>(cp, addonListItem.designBlockTypeGuid);
                //var addon = AddonCacheAsideController.createAddon(cp, addonListItem.designBlockTypeGuid);
                renderEdit(cp, addonListItem, addon, executeContext);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// transverse the addonlist and delete the instance specified. return true if found, false if not found
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="addonList"></param>
        /// <param name="instanceGuid"></param>
        /// <returns></returns>
        public static bool deleteInstance(CPBaseClass cp, List<AddonListItemModel> addonList, string instanceGuid) {
            if (addonList == null) {
                return false;
            }
            foreach (var addon in addonList) {
                if (addon.columns != null) {
                    foreach (var column in addon.columns) {
                        if (deleteInstance(cp, column.addonList, instanceGuid)) { return true; }
                    }
                }
                if (addon.instanceGuid == instanceGuid) {
                    addonList.Remove(addon);
                    return true;
                }
            }
            return false;
        }
        //
        // ==========================================================================================
        /// <summary>
        /// clean up the addonlist delivered from the UI (remove renderedhtml, update addonName, etc)
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="addonList"></param>
        /// <returns></returns>
        public static void normalizeAddonList(CPBaseClass cp, List<AddonListItemModel> addonList) {
            try {
                if (addonList == null) {
                    return;
                }
                foreach (var addon in addonList) {
                    addon.renderedHtml = string.Empty;
                    addon.renderedAssets = new AddonAssetsModel();
                    //
                    // -- move to model cache because static property persistence is the appDomain and there is no invalidation, so it takes an iisreset to invalidate
                    // -- to document -- this was originally a model open and it was changed to the static cache.
                    var addonRecord = DbBaseModel.create<AddonModel>(cp, addon.designBlockTypeGuid);
                    //var addonRecord = AddonCacheAsideController.createAddon(cp, addon.designBlockTypeGuid);
                    if (addonRecord != null) {
                        addon.designBlockTypeName = addonRecord.name;
                    }
                    if (addon.columns != null) {
                        foreach (var column in addon.columns) {
                            normalizeAddonList(cp, column.addonList);
                        }
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// referenced from processor.project
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="contentId"></param>
        /// <param name="instanceGuid"></param>
        /// <returns></returns>
        private static string getAdminEditUrl(CPBaseClass cp, int contentId, string instanceGuid) {
            return $"/{cp.GetAppConfig(cp.Site.Name).adminRoute}?af=4&aa=2&ad=1&cid={contentId}&guid={instanceGuid}";
        }

    }
    /// <summary>
    /// Assets added to the html document during html rendering
    /// </summary>
    [System.Serializable]
    public class AddonAssetsModel {
        /// <summary>
        /// css styles added to the head
        /// </summary>
        public List<string> headStyles = new List<string>();
        //
        public List<string> headStylesheetLinks = new List<string>();
        //
        public List<string> headJs = new List<string>();
        //
        public List<string> headJsLinks = new List<string>();
        //
        public List<string> bodyJs = new List<string>();
        //
        public List<string> bodyJsLinks = new List<string>();
    }
    [System.Serializable]
    public class AddonListColumnItemModel {
        /// <summary>
        /// the integer width of a column, where the row totals 12
        /// </summary>
        public int col;
        /// <summary>
        /// optional class that represents the width of the column
        /// </summary>
        public string className;
        /// <summary>
        /// Each column contains an addon list. This extra object layer was created to make it more convenient for the UI javascript
        /// </summary>
        public List<AddonListItemModel> addonList;
    }
    ////
    ////====================================================================================================
    ///// <summary>
    ///// This is a static property cache, so it's persistence is the appDomain and there is no invalidation. It is fast, but addon updates require an iisreset
    ///// Removed for now.
    ///// Original name from PageBuilder collection was just CacheAside
    ///// </summary>
    //public static class AddonCacheAsideController {
    //    //
    //    public static Dictionary<string, AddonModel> AddonCacheAside = new Dictionary<string, AddonModel>();
    //    //
    //    public static AddonModel createAddon(CPBaseClass cp, string addonGuid) {
    //        if (AddonCacheAside.ContainsKey(addonGuid)) {
    //            return AddonCacheAside[addonGuid];
    //        }
    //        AddonModel addon = DbBaseModel.create<AddonModel>(cp, addonGuid);
    //        AddonCacheAside.Add(addonGuid, addon);
    //        return addon;
    //    }

    //}
}