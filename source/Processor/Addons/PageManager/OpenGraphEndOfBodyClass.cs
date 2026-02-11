using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Windows.Input;
using System.Xml.Linq;
using Twilio.TwiML.Voice;
//
namespace Contensive.Processor.Addons.PageManager {
    public class OpenGraphEndOfBodyClass : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                bool isAdminUrl = cp.Request.PathPage.Equals(cp.Site.GetText("adminurl"), StringComparison.OrdinalIgnoreCase);
                if (isAdminUrl) { return ""; }
                //
                // Add standard Open Graph meta tags
                AddTag(cp, "fb:admins", "facebook admin Id list");
                AddTag(cp, "og:title", "Open Graph Title");
                AddTag(cp, "og:type", "Open Graph Content Type");
                AddTag(cp, "og:url", "Open Graph URL");
                AddTag(cp, "og:site_name", "Open Graph Site Name");
                AddTag(cp, "og:description", "Open Graph Description");

                // Get Open Graph image with fallback logic
                string ogImage = cp.Doc.GetText("Open Graph Image", "");
                if (string.IsNullOrWhiteSpace(ogImage)) {
                    ogImage = cp.Site.GetText("OpenGraphDefaultImageFilename", "");

                    if (string.IsNullOrWhiteSpace(ogImage)) {
                        // legacy name fallback for backward compatibility
                        ogImage = cp.Site.GetText("pageImageFilename", "");
                    }

                    if (!string.IsNullOrWhiteSpace(ogImage)) {
                        // Prepend the CDN file path to create absolute URL
                        ogImage = cp.Http.CdnFilePathPrefixAbsolute + ogImage;
                    }
                }
                if (!string.IsNullOrWhiteSpace(ogImage)) {
                    cp.Doc.AddHeadTag($"<meta property=\"og:image\" content=\"{ogImage}\"/>");
                }

                // Add Twitter Card meta tags
                cp.Doc.AddHeadTag("<meta property=\"twitter:card\" content=\"summary\"/>");
                AddTag(cp, "twitter:description", "Open Graph Description");
                AddTag(cp, "twitter:title", "Open Graph Title");
                AddTag(cp, "twitter:url", "Open Graph URL");

                // Add Twitter image tag (reusing the Open Graph image)
                if (!string.IsNullOrWhiteSpace(ogImage)) {
                    cp.Doc.AddHeadTag($"<meta property=\"twitter:image\" content=\"{ogImage}\"/>");
                }
                return "";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "";
            }
        }

        /// <summary>
        /// Adds a meta tag to the document head with the specified Open Graph property name and value from document or site properties
        /// </summary>
        /// <param name="cp">The CPBaseClass instance</param>
        /// <param name="ogName">The Open Graph property name (e.g., "og:title", "twitter:description")</param>
        /// <param name="key">The property key to retrieve the value from document or site properties</param>
        private void AddTag(CPBaseClass cp, string ogName, string key) {

            // Try to get value from document properties first, If not found in document, try site properties
            string ogValue = cp.Doc.GetText(key, "");
            if (string.IsNullOrWhiteSpace(ogValue)) {
                ogValue = cp.Site.GetText(key, "");
            }

            // Only add the tag if we have a value
            if (string.IsNullOrWhiteSpace(ogValue)) {
                return;
            }
            string encodedValue = cp.Utils.EncodeHTML(ogValue);
            cp.Doc.AddHeadTag($"<meta property=\"{ogName}\" content=\"{encodedValue}\"/>");
        }
    }
}