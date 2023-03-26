using HtmlAgilityPack;

namespace Contensive.Processor {
    namespace Controllers {
        // 
        // ====================================================================================================
        /// <summary>
        /// process delete (see tool form for details)
        /// </summary>
        public static class DataDeleteController {
            //
            public static void process(HtmlDocument htmlDoc) {
                //
                // -- legacy class
                {
                    string xPath = "//*[contains(@class,'mustache-delete')]";
                    HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                    if (nodeList != null) {
                        foreach (HtmlNode node in nodeList) {
                            node.ParentNode.RemoveChild(node);
                        }
                    }
                }
                //
                // -- data attribute
                {
                    string xPath = "//*[@data-delete]";
                    HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                    if (nodeList != null) {
                        foreach (HtmlNode node in nodeList) {
                            node.ParentNode.RemoveChild(node);
                        }
                    }
                }
            }
        }
    }
}
