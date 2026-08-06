
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Merges multiple addon CSS files into a single file to reduce render-blocking resources.
    /// When enabled via the "Allow CSS Merge" site property, all non-deferred addon CSS link assets
    /// are combined into one file keyed by addon IDs. Deferred CSS loads asynchronously using the
    /// media-swap technique.
    /// </summary>
    public static class CssMergeController {
        //
        //====================================================================================================
        /// <summary>
        /// Process style assets and return HTML tags. When CSS merge is enabled, mergeable addon CSS
        /// is combined into a single file. Deferred CSS uses media="print" onload swap technique.
        /// Non-addon CSS passes through unchanged.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="styleAssets">All style assets with inHead==true</param>
        /// <param name="allowDebug">If true, add debug comments showing which addon added each asset</param>
        /// <returns>List of HTML tags to add to the head</returns>
        public static List<string> getMergedStyleTags(CoreController core, List<CPDocBaseClass.HtmlAssetClass> styleAssets, bool allowDebug) {
            var result = new List<string>();
            try {
                //
                // -- separate assets into three groups
                var mergeableAssets = new List<CPDocBaseClass.HtmlAssetClass>();
                var deferredAssets = new List<CPDocBaseClass.HtmlAssetClass>();
                var passthroughAssets = new List<CPDocBaseClass.HtmlAssetClass>();
                //
                foreach (var asset in styleAssets) {
                    if (string.IsNullOrEmpty(asset.content)) { continue; }
                    if (asset.cssDefer) {
                        deferredAssets.Add(asset);
                    } else if (asset.canBeMerged && asset.isLink && asset.sourceAddonId > 0) {
                        mergeableAssets.Add(asset);
                    } else {
                        passthroughAssets.Add(asset);
                    }
                }
                //
                // -- merged addon CSS: build a single file from all mergeable assets
                if (mergeableAssets.Count > 0) {
                    //
                    // -- build filename from sorted addon IDs
                    var addonIds = mergeableAssets.Select(a => a.sourceAddonId).Distinct().OrderBy(id => id).ToList();
                    string idString = string.Join("-", addonIds);
                    string mergedFilename = $"cssmerge/merged_{idString}.css";
                    //
                    // -- check if the merged file already exists
                    if (!core.cdnFiles.fileExists(mergedFilename)) {
                        //
                        // -- file does not exist, read and concatenate all CSS content
                        var cssBuilder = new StringBuilder();
                        string cdnFileUrl = core.appConfig.cdnFileUrl;
                        foreach (var asset in mergeableAssets) {
                            string cssContent = readCssFromAsset(core, asset, cdnFileUrl);
                            if (!string.IsNullOrEmpty(cssContent)) {
                                cssBuilder.AppendLine($"/* addon {asset.sourceAddonId} */");
                                cssBuilder.AppendLine(cssContent);
                            }
                        }
                        //
                        // -- save the merged file
                        string mergedCss = cssBuilder.ToString();
                        if (!string.IsNullOrEmpty(mergedCss)) {
                            core.cdnFiles.saveFile(mergedFilename, mergedCss);
                        }
                    }
                    //
                    // -- emit a single link tag for the merged file
                    if (allowDebug) {
                        result.Add(getAddedByComment($"CSS Merge ({mergeableAssets.Count} stylesheets from addons: {idString})"));
                    }
                    string mergedUrl = GenericController.getCdnFileLink(core, mergedFilename);
                    result.Add($"<link rel=\"stylesheet\" type=\"text/css\" href=\"{mergedUrl}\" >");
                }
                //
                // -- pass-through assets: emit as-is (same logic as original HtmlController)
                foreach (var asset in passthroughAssets) {
                    if (allowDebug && !string.IsNullOrWhiteSpace(asset.addedByMessage)) {
                        result.Add(getAddedByComment(asset.addedByMessage));
                    }
                    if (asset.isLink) {
                        if (asset.content.Trim().Substring(0, 1) == "<") {
                            result.Add(asset.content);
                        } else {
                            result.Add($"<link rel=\"stylesheet\" type=\"text/css\" href=\"{asset.content}\" >");
                        }
                    } else {
                        result.Add($"<style>{asset.content}</style>");
                    }
                }
                //
                // -- deferred assets: load async with media-swap technique
                foreach (var asset in deferredAssets) {
                    if (allowDebug && !string.IsNullOrWhiteSpace(asset.addedByMessage)) {
                        result.Add(getAddedByComment($"{asset.addedByMessage} (deferred)"));
                    }
                    string href = asset.content;
                    if (asset.isLink) {
                        if (href.Trim().Substring(0, 1) == "<") {
                            // -- already a full tag, can't easily defer it, pass through as-is
                            result.Add(href);
                        } else {
                            result.Add($"<link rel=\"stylesheet\" href=\"{href}\" media=\"print\" onload=\"this.media='all'\">");
                            result.Add($"<noscript><link rel=\"stylesheet\" href=\"{href}\"></noscript>");
                        }
                    } else {
                        // -- inline style, can't defer, pass through
                        result.Add($"<style>{asset.content}</style>");
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read CSS content from a style asset's link URL. The URL was set by AddonController via
        /// getCdnFileLink, so it either starts with the cdnFileUrl prefix (cdn file) or is root-relative.
        /// </summary>
        private static string readCssFromAsset(CoreController core, CPDocBaseClass.HtmlAssetClass asset, string cdnFileUrl) {
            try {
                string url = asset.content;
                if (string.IsNullOrEmpty(url)) { return ""; }
                //
                // -- strip cdn prefix to get the relative path in cdnFiles
                if (!string.IsNullOrEmpty(cdnFileUrl) && url.StartsWith(cdnFileUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    string relativePath = url.Substring(cdnFileUrl.Length);
                    return core.cdnFiles.readFileText(relativePath);
                }
                //
                // -- root-relative URL, try wwwFiles
                if (url.StartsWith("/")) {
                    return core.wwwFiles.readFileText(url.Substring(1));
                }
                //
                // -- can't resolve, return empty
                return "";
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}, error reading CSS for addon {asset.sourceAddonId}");
                return "";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate any merged CSS files that include the specified addon ID.
        /// Called when an addon's CSS is updated (e.g. during minification).
        /// </summary>
        public static void invalidateAddonCss(CoreController core, int addonId) {
            try {
                var fileList = core.cdnFiles.getFileList("cssmerge");
                if (fileList == null || fileList.Count == 0) { return; }
                //
                string addonIdStr = addonId.ToString();
                foreach (var file in fileList) {
                    if (!file.Name.StartsWith("merged_")) { continue; }
                    //
                    // -- extract the ID portion: "merged_5-12-27.css" -> "5-12-27"
                    string namePart = file.Name.Substring("merged_".Length);
                    int dotPos = namePart.LastIndexOf('.');
                    if (dotPos > 0) { namePart = namePart.Substring(0, dotPos); }
                    //
                    // -- check if this addon's ID is in the list
                    var ids = namePart.Split('-');
                    if (ids.Contains(addonIdStr)) {
                        core.cdnFiles.deleteFile($"cssmerge/{file.Name}");
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}, error invalidating CSS merge for addon {addonId}");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Format a debug comment for CSS merge
        /// </summary>
        private static string getAddedByComment(string message) {
            return $"<!-- {message} -->";
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
