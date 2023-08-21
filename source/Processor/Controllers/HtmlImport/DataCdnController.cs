using Contensive.BaseClasses;
using HtmlAgilityPack;
using System;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// <img data-cdn="src" src="/image/this.png">
    /// changes the src from www-based to cdn-based. The image is expected to be loaded 
    /// </summary>
    public static class DataCdnController {
        /// <summary>
        /// find and process all data-cdn attributes. Convert the attribute named in the data-cdn to a cdn url
        /// </summary>
        /// <param name="htmlDoc"></param>
        public static void process(CPBaseClass cp, HtmlDocument htmlDoc) {
            //
            // -- data-cdn
            string xPath = "//*[@data-cdn]";
            HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
            if (nodeList != null) {
                foreach (HtmlNode node in nodeList) {
                    string attrName = node.Attributes["data-cdn"]?.Value;
                    node.Attributes.Remove("data-cdn");
                    if (attrName == null || attrName.Length == 0) { return; }
                    if (!node.Attributes.Contains(attrName)) { return; }
                    //
                    string srcUrl = node.Attributes[attrName]?.Value;
                    if (srcUrl == null || srcUrl.Length == 0) { return; }
                    //
                    node.Attributes.Remove(attrName);
                    string cdnPathPage = "";
                    if (srcUrl.Contains("://")) {
                        //
                        // -- srcUrl is absolute. dst is pathPage
                        Uri myUri = new(srcUrl);
                        cdnPathPage = cp.Http.CdnFilePathPrefix + myUri.PathAndQuery;
                        cp.CdnFiles.SaveHttpGet(srcUrl, cdnPathPage);
                        node.Attributes.Add(attrName, cdnPathPage);
                    } else {
                        //
                        // -- relative url, source is on wwwroot
                        if (srcUrl.Substring(0, 1) == "/") {
                            if (srcUrl.Length == 1) {
                                srcUrl = "";
                            } else {
                                srcUrl = srcUrl.Substring(1);
                            }
                        }
                        if(cp.WwwFiles.FileExists(srcUrl)) {
                            cp.WwwFiles.Copy(srcUrl, srcUrl, cp.CdnFiles);
                        }
                        node.Attributes.Add(attrName, cp.Http.CdnFilePathPrefixAbsolute + srcUrl);
                    }
                }
            }
        }
    }
}