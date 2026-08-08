
using Contensive.BaseClasses;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Merges multiple addon CSS files into a single file to reduce render-blocking resources.
    /// When enabled via the "Allow CSS Merge" site property, all non-deferred addon CSS link assets
    /// are combined into one file keyed by addon IDs. Deferred CSS loads asynchronously using the
    /// media-swap technique. When "Allow CSS Purge" is also enabled, unused CSS rules are removed
    /// per page and inlined as a style tag.
    /// </summary>
    public static class CssMergeController {
        //
        //====================================================================================================
        /// <summary>
        /// Process style assets and return HTML tags. When CSS merge is enabled, mergeable addon CSS
        /// is combined into a single file. When CSS purge is also enabled, unused rules are removed
        /// and the result is inlined. Deferred CSS uses media="print" onload swap technique.
        /// Non-addon CSS passes through unchanged.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="styleAssets">All style assets with inHead==true</param>
        /// <param name="allowDebug">If true, add debug comments showing which addon added each asset</param>
        /// <param name="htmlBody">The full HTML body content, used for CSS purge to identify used selectors</param>
        /// <param name="allowCssPurge">If true, purge unused CSS rules and inline the result</param>
        /// <returns>List of HTML tags to add to the head</returns>
        public static List<string> getMergedStyleTags(CoreController core, List<CPDocBaseClass.HtmlAssetClass> styleAssets, bool allowDebug, string htmlBody, bool allowCssPurge) {
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
                    string mergedCss = "";
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
                        mergedCss = cssBuilder.ToString();
                        if (!string.IsNullOrEmpty(mergedCss)) {
                            core.cdnFiles.saveFile(mergedFilename, mergedCss);
                        }
                    }
                    //
                    // -- purge or link
                    if (allowCssPurge && !string.IsNullOrEmpty(htmlBody)) {
                        //
                        // -- check cache first
                        string htmlBodyHash = computeHash(htmlBody);
                        string cacheKey = $"cssPurge-{idString}-{htmlBodyHash}";
                        string purgedCss = core.cache.getText(cacheKey);
                        if (string.IsNullOrEmpty(purgedCss)) {
                            //
                            // -- cache miss, read the merged CSS and purge
                            if (string.IsNullOrEmpty(mergedCss)) {
                                mergedCss = core.cdnFiles.readFileText(mergedFilename);
                            }
                            if (!string.IsNullOrEmpty(mergedCss)) {
                                purgedCss = purgeCss(mergedCss, htmlBody);
                                //
                                // -- store in cache with dependency on addons table
                                if (!string.IsNullOrEmpty(purgedCss)) {
                                    core.cache.storeObject(cacheKey, purgedCss);
                                }
                            }
                        }
                        //
                        // -- emit inline style tag with purged CSS
                        if (!string.IsNullOrEmpty(purgedCss)) {
                            if (allowDebug) {
                                result.Add(getAddedByComment($"CSS Merge+Purge ({mergeableAssets.Count} stylesheets from addons: {idString})"));
                            }
                            result.Add($"<style>{purgedCss}</style>");
                        }
                    } else {
                        //
                        // -- no purge, emit a single link tag for the merged file
                        if (allowDebug) {
                            result.Add(getAddedByComment($"CSS Merge ({mergeableAssets.Count} stylesheets from addons: {idString})"));
                        }
                        string mergedUrl = GenericController.getCdnFileLink(core, mergedFilename);
                        result.Add($"<link rel=\"stylesheet\" type=\"text/css\" href=\"{mergedUrl}\" >");
                    }
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
        /// Purge unused CSS rules by comparing selectors against identifiers found in the HTML body.
        /// Splits CSS into top-level blocks using brace counting to preserve original text exactly
        /// (ExCSS ToCss() drops vendor-prefixed and unrecognized properties). Selectors are analyzed
        /// with regex to decide keep/drop.
        /// </summary>
        private static string purgeCss(string css, string htmlBody) {
            try {
                //
                // -- extract used identifiers from HTML
                var usedIdentifiers = extractUsedIdentifiers(htmlBody);
                //
                // -- split CSS into top-level blocks and filter
                var blocks = splitCssBlocks(css);
                var purgedBuilder = new StringBuilder();
                foreach (var block in blocks) {
                    if (shouldKeepBlock(block, usedIdentifiers)) {
                        purgedBuilder.AppendLine(block);
                    }
                }
                return purgedBuilder.ToString();
            } catch (Exception ex) {
                logger.Error(ex, "Error purging CSS, returning unpurged CSS");
                return css;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Split CSS text into top-level blocks by tracking brace depth.
        /// Each block is a complete rule (style rule, @media, @font-face, etc.) with its original text preserved.
        /// Also captures top-level statements without braces (like @import, @charset).
        /// </summary>
        private static List<string> splitCssBlocks(string css) {
            var blocks = new List<string>();
            int depth = 0;
            int blockStart = -1;
            bool inString = false;
            char stringChar = '"';
            bool inComment = false;
            //
            // -- skip leading whitespace to find first meaningful content
            int i = 0;
            while (i < css.Length) {
                //
                // -- handle comments
                if (!inString && !inComment && i + 1 < css.Length && css[i] == '/' && css[i + 1] == '*') {
                    if (blockStart < 0) { blockStart = i; }
                    inComment = true;
                    i += 2;
                    continue;
                }
                if (inComment) {
                    if (i + 1 < css.Length && css[i] == '*' && css[i + 1] == '/') {
                        inComment = false;
                        i += 2;
                        //
                        // -- if this comment is a standalone top-level block (addon comment), emit it
                        if (depth == 0 && blockStart >= 0) {
                            string commentBlock = css.Substring(blockStart, i - blockStart).Trim();
                            if (!string.IsNullOrEmpty(commentBlock)) {
                                blocks.Add(commentBlock);
                            }
                            blockStart = -1;
                        }
                    } else {
                        i++;
                    }
                    continue;
                }
                //
                // -- handle strings
                if (!inComment && (css[i] == '"' || css[i] == '\'')) {
                    if (!inString) {
                        inString = true;
                        stringChar = css[i];
                    } else if (css[i] == stringChar && (i == 0 || css[i - 1] != '\\')) {
                        inString = false;
                    }
                    i++;
                    continue;
                }
                if (inString) { i++; continue; }
                //
                // -- track braces
                if (css[i] == '{') {
                    if (depth == 0 && blockStart < 0) {
                        // -- scan backwards to find the start of the selector/at-rule
                        blockStart = i;
                        while (blockStart > 0 && css[blockStart - 1] != '}' && css[blockStart - 1] != '/' && (blockStart <= 1 || css[blockStart - 2] != '*' || css[blockStart - 1] != '/')) {
                            blockStart--;
                        }
                        // -- skip whitespace at the start
                        while (blockStart < i && char.IsWhiteSpace(css[blockStart])) {
                            blockStart++;
                        }
                    }
                    depth++;
                } else if (css[i] == '}') {
                    depth--;
                    if (depth == 0 && blockStart >= 0) {
                        string block = css.Substring(blockStart, i - blockStart + 1).Trim();
                        if (!string.IsNullOrEmpty(block)) {
                            blocks.Add(block);
                        }
                        blockStart = -1;
                    }
                }
                i++;
            }
            return blocks;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Determine if a CSS block (original text) should be kept based on used identifiers.
        /// Examines the text before the first '{' to identify the rule type and selector.
        /// </summary>
        private static bool shouldKeepBlock(string block, HashSet<string> usedIdentifiers) {
            //
            // -- comments: always keep
            if (block.TrimStart().StartsWith("/*")) { return true; }
            //
            int bracePos = block.IndexOf('{');
            if (bracePos < 0) {
                //
                // -- no braces (e.g. @import, @charset): always keep
                return true;
            }
            string header = block.Substring(0, bracePos).Trim();
            //
            // -- always keep @font-face
            if (header.StartsWith("@font-face", StringComparison.OrdinalIgnoreCase)) { return true; }
            //
            // -- always keep @keyframes
            if (header.StartsWith("@keyframes", StringComparison.OrdinalIgnoreCase) || header.StartsWith("@-webkit-keyframes", StringComparison.OrdinalIgnoreCase)) { return true; }
            //
            // -- always keep @import
            if (header.StartsWith("@import", StringComparison.OrdinalIgnoreCase)) { return true; }
            //
            // -- always keep @page
            if (header.StartsWith("@page", StringComparison.OrdinalIgnoreCase)) { return true; }
            //
            // -- always keep @namespace
            if (header.StartsWith("@namespace", StringComparison.OrdinalIgnoreCase)) { return true; }
            //
            // -- always keep @charset
            if (header.StartsWith("@charset", StringComparison.OrdinalIgnoreCase)) { return true; }
            //
            // -- @media and @supports: extract inner rules and filter them
            if (header.StartsWith("@media", StringComparison.OrdinalIgnoreCase) || header.StartsWith("@supports", StringComparison.OrdinalIgnoreCase)) {
                //
                // -- extract the content between the outermost braces
                int innerStart = bracePos + 1;
                int innerEnd = block.LastIndexOf('}');
                if (innerEnd <= innerStart) { return true; }
                string innerCss = block.Substring(innerStart, innerEnd - innerStart);
                //
                // -- recursively filter inner blocks
                var innerBlocks = splitCssBlocks(innerCss);
                var keptBlocks = new List<string>();
                foreach (var innerBlock in innerBlocks) {
                    if (shouldKeepBlock(innerBlock, usedIdentifiers)) {
                        keptBlocks.Add(innerBlock);
                    }
                }
                //
                // -- keep the @media/@supports block only if it has kept rules
                return keptBlocks.Count > 0;
            }
            //
            // -- style rule: header is the selector, check against used identifiers
            return selectorMatchesUsedIdentifiers(header, usedIdentifiers);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Extract all CSS-relevant identifiers from the HTML body: element names, class names,
        /// IDs, attribute names, and potential class references from inline JavaScript.
        /// </summary>
        private static HashSet<string> extractUsedIdentifiers(string htmlBody) {
            var identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            //
            // -- add framework safelist (Bootstrap, Font Awesome, common dynamic patterns)
            addFrameworkSafelist(identifiers);
            //
            // -- parse HTML with HtmlAgilityPack
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlBody);
            //
            // -- walk all nodes
            foreach (var node in htmlDoc.DocumentNode.DescendantsAndSelf()) {
                if (node.NodeType != HtmlNodeType.Element) { continue; }
                //
                // -- element/tag name
                identifiers.Add(node.Name);
                //
                // -- all attribute names (for attribute selectors like [type], [data-toggle])
                foreach (var attr in node.Attributes) {
                    identifiers.Add(attr.Name);
                    //
                    // -- class names from class attribute
                    if (attr.Name.Equals("class", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(attr.Value)) {
                        foreach (var className in attr.Value.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) {
                            identifiers.Add(className);
                        }
                    }
                    //
                    // -- ID attribute
                    if (attr.Name.Equals("id", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(attr.Value)) {
                        identifiers.Add(attr.Value);
                    }
                }
            }
            //
            // -- scan inline scripts and event handlers for potential class/ID references
            extractJsReferences(htmlDoc, identifiers);
            //
            return identifiers;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Scan script tags and on* event handler attributes for string literals
        /// that might be CSS class names used dynamically in JavaScript.
        /// </summary>
        private static void extractJsReferences(HtmlDocument htmlDoc, HashSet<string> identifiers) {
            //
            // -- collect all JS content: script tag bodies and on* attributes
            var jsContent = new StringBuilder();
            foreach (var node in htmlDoc.DocumentNode.DescendantsAndSelf()) {
                if (node.NodeType != HtmlNodeType.Element) { continue; }
                //
                // -- script tag content
                if (node.Name.Equals("script", StringComparison.OrdinalIgnoreCase)) {
                    string scriptContent = node.InnerText;
                    if (!string.IsNullOrEmpty(scriptContent)) {
                        jsContent.AppendLine(scriptContent);
                    }
                }
                //
                // -- on* event handler attributes
                foreach (var attr in node.Attributes) {
                    if (attr.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(attr.Value)) {
                        jsContent.AppendLine(attr.Value);
                    }
                }
            }
            //
            // -- extract string literals from JS that look like CSS class names
            if (jsContent.Length > 0) {
                string js = jsContent.ToString();
                //
                // -- match single and double quoted strings
                var stringPattern = new Regex(@"[""']([a-zA-Z][a-zA-Z0-9_-]*(?:\s+[a-zA-Z][a-zA-Z0-9_-]*)*)[""']");
                foreach (Match match in stringPattern.Matches(js)) {
                    string value = match.Groups[1].Value;
                    foreach (var token in value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) {
                        if (token.Length > 1 && Regex.IsMatch(token, @"^[a-zA-Z][a-zA-Z0-9_-]*$")) {
                            identifiers.Add(token);
                        }
                    }
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add framework safelist entries — classes and patterns commonly added by JavaScript
        /// at runtime that won't appear in the static HTML but must be preserved.
        /// </summary>
        private static void addFrameworkSafelist(HashSet<string> identifiers) {
            //
            // -- Bootstrap dynamic classes
            var bootstrapClasses = new[] {
                "show", "hide", "fade", "collapse", "collapsing",
                "modal-open", "modal-backdrop", "modal-dialog-scrollable",
                "active", "disabled", "open", "close", "in", "out",
                "visible", "invisible", "dropdown-menu", "dropdown-toggle",
                "tooltip", "tooltip-inner", "tooltip-arrow",
                "popover", "popover-header", "popover-body",
                "carousel", "carousel-item", "carousel-item-next", "carousel-item-prev",
                "offcanvas", "offcanvas-backdrop",
                "accordion", "accordion-collapse",
                "tab-pane", "nav-link", "nav-item",
                "alert-dismissible",
                "was-validated", "is-valid", "is-invalid",
                "sticky-top", "fixed-top", "fixed-bottom"
            };
            foreach (var cls in bootstrapClasses) {
                identifiers.Add(cls);
            }
            //
            // -- Font Awesome core classes (base classes only, individual icons are purged)
            var fontAwesomeCore = new[] {
                "fa", "fas", "far", "fab", "fal", "fad",
                "fa-solid", "fa-regular", "fa-brands", "fa-light", "fa-duotone",
                "fa-fw", "fa-lg", "fa-2x", "fa-3x", "fa-4x", "fa-5x",
                "fa-spin", "fa-pulse", "fa-flip-horizontal", "fa-flip-vertical",
                "fa-rotate-90", "fa-rotate-180", "fa-rotate-270",
                "fa-stack", "fa-stack-1x", "fa-stack-2x", "fa-inverse"
            };
            foreach (var cls in fontAwesomeCore) {
                identifiers.Add(cls);
            }
        }
        //
        //====================================================================================================
        //
        //====================================================================================================
        /// <summary>
        /// Check if a CSS selector references any identifier found in the HTML.
        /// Universal selectors (*, html, body, :root) are always kept.
        /// Pseudo-classes and pseudo-elements are always kept.
        /// </summary>
        private static bool selectorMatchesUsedIdentifiers(string selectorText, HashSet<string> usedIdentifiers) {
            if (string.IsNullOrEmpty(selectorText)) { return true; }
            //
            // -- always keep universal selectors
            string trimmed = selectorText.Trim();
            if (trimmed == "*") { return true; }
            //
            // -- split compound selectors (comma-separated) and keep if any part matches
            var selectors = selectorText.Split(',');
            foreach (var selector in selectors) {
                if (singleSelectorMatches(selector.Trim(), usedIdentifiers)) {
                    return true;
                }
            }
            return false;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Check if a single (non-compound) CSS selector matches any used identifier.
        /// Extracts class names, IDs, element names, and attribute names from the selector.
        /// </summary>
        private static bool singleSelectorMatches(string selector, HashSet<string> usedIdentifiers) {
            if (string.IsNullOrEmpty(selector)) { return true; }
            //
            // -- always keep selectors targeting html, body, :root, *
            if (selector == "*" || selector == "html" || selector == "body" || selector == ":root") {
                return true;
            }
            //
            // -- extract class names (.className)
            var classPattern = new Regex(@"\.([a-zA-Z_][a-zA-Z0-9_-]*)");
            var classMatches = classPattern.Matches(selector);
            if (classMatches.Count > 0) {
                foreach (Match match in classMatches) {
                    string className = match.Groups[1].Value;
                    //
                    // -- keep if this class is used in the HTML or matches dynamic patterns
                    if (usedIdentifiers.Contains(className)) { return true; }
                    if (isDynamicPattern(className)) { return true; }
                }
            }
            //
            // -- extract IDs (#idName)
            var idPattern = new Regex(@"#([a-zA-Z_][a-zA-Z0-9_-]*)");
            var idMatches = idPattern.Matches(selector);
            if (idMatches.Count > 0) {
                foreach (Match match in idMatches) {
                    if (usedIdentifiers.Contains(match.Groups[1].Value)) { return true; }
                }
            }
            //
            // -- extract element names (div, span, a, etc.)
            var elementPattern = new Regex(@"(?:^|[\s>+~])([a-zA-Z][a-zA-Z0-9]*)");
            var elementMatches = elementPattern.Matches(selector);
            if (elementMatches.Count > 0) {
                bool hasOnlyElements = classMatches.Count == 0 && idMatches.Count == 0;
                if (hasOnlyElements) {
                    // -- selector uses only element names (e.g. "div p"), keep if any element is used
                    foreach (Match match in elementMatches) {
                        if (usedIdentifiers.Contains(match.Groups[1].Value)) { return true; }
                    }
                }
            }
            //
            // -- extract attribute selectors ([type="text"], [data-toggle])
            var attrPattern = new Regex(@"\[([a-zA-Z_][a-zA-Z0-9_-]*)");
            foreach (Match match in attrPattern.Matches(selector)) {
                if (usedIdentifiers.Contains(match.Groups[1].Value)) { return true; }
            }
            //
            // -- if the selector is only pseudo-classes/elements with no other identifiers, keep it
            if (classMatches.Count == 0 && idMatches.Count == 0 && selector.Contains(":")) {
                return true;
            }
            //
            return false;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Check if a class name matches common dynamic patterns that should always be kept.
        /// </summary>
        private static bool isDynamicPattern(string className) {
            return className.StartsWith("js-", StringComparison.OrdinalIgnoreCase)
                || className.StartsWith("is-", StringComparison.OrdinalIgnoreCase)
                || className.StartsWith("has-", StringComparison.OrdinalIgnoreCase)
                || className.StartsWith("was-", StringComparison.OrdinalIgnoreCase);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Compute a short hash of a string for use as a cache key component.
        /// </summary>
        private static string computeHash(string input) {
            using (var sha = SHA256.Create()) {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                // -- use first 8 bytes (16 hex chars) for a short but collision-resistant key
                var sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++) {
                    sb.Append(bytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
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
                string cssContent = "";
                string baseUrl = "";
                //
                // -- strip cdn prefix to get the relative path in cdnFiles
                if (!string.IsNullOrEmpty(cdnFileUrl) && url.StartsWith(cdnFileUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    string relativePath = url.Substring(cdnFileUrl.Length);
                    cssContent = core.cdnFiles.readFileText(relativePath);
                    baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
                }
                //
                // -- root-relative URL, try wwwFiles
                else if (url.StartsWith("/") && !url.StartsWith("//")) {
                    cssContent = core.wwwFiles.readFileText(url.Substring(1));
                    baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
                }
                //
                // -- external URL (http:, https:, //), fetch via HTTP
                else if (url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("//")) {
                    string fetchUrl = url.StartsWith("//") ? $"https:{url}" : url;
                    var http = new HttpController();
                    cssContent = http.getURL(fetchUrl);
                    baseUrl = fetchUrl.Substring(0, fetchUrl.LastIndexOf('/') + 1);
                }
                //
                // -- rewrite relative url() references to absolute
                if (!string.IsNullOrEmpty(cssContent) && !string.IsNullOrEmpty(baseUrl)) {
                    cssContent = rewriteCssUrls(cssContent, baseUrl);
                }
                return cssContent;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}, error reading CSS for addon {asset.sourceAddonId}, url [{asset.content}]");
                return "";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Rewrite relative url() references in CSS to absolute URLs.
        /// Handles url(../path), url(./path), url(path), and quoted variants.
        /// Skips urls that are already absolute (http:, https:, //, data:).
        /// </summary>
        private static string rewriteCssUrls(string css, string baseUrl) {
            // -- match url(...) with optional quotes
            return Regex.Replace(css, @"url\(\s*([""']?)(.+?)\1\s*\)", (match) => {
                string quote = match.Groups[1].Value;
                string originalUrl = match.Groups[2].Value;
                //
                // -- skip absolute urls and data urls
                if (originalUrl.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)
                    || originalUrl.StartsWith("//")
                    || originalUrl.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase)
                    || originalUrl.StartsWith("#")) {
                    return match.Value;
                }
                //
                // -- resolve relative url against the base
                try {
                    var baseUri = new Uri(baseUrl);
                    var resolved = new Uri(baseUri, originalUrl);
                    return $"url({quote}{resolved.AbsoluteUri}{quote})";
                } catch {
                    return match.Value;
                }
            });
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
