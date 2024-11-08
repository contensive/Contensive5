using Contensive.BaseClasses;
using Contensive.Models.Db;
using HtmlAgilityPack;
using NUglify.JavaScript.Syntax;
using System.Collections.Generic;

namespace Contensive.Processor {
    namespace Controllers {
        // 
        // ====================================================================================================
        /// <summary>
        /// locate the data-layout="layoutname" attributes and return the inner html that matches the layoutnamefilter
        /// </summary>
        public static class DataLayoutController {
            //
            /// <summary>
            /// limit the htmlDoc to the single layout requested in the filterName.
            /// If the layout contains a data-layout with this name, that node is returned.
            /// If the layout node is not found, the entire layout is returned
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="htmlDoc"></param>
            /// <param name="userMessageList"></param>
            /// <param name="layoutNameFilter"></param>
            public static void processFilter(CPBaseClass cp, HtmlDocument htmlDoc, ref List<string> userMessageList, string layoutNameFilter) {
                //
                // -- data attribute
                {
                    string xPath = "//*[@data-layout]";
                    HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                    if (nodeList != null) {
                        foreach (HtmlNode node in nodeList) {
                            string layoutRecordName = node.Attributes["data-layout"]?.Value;
                            node.Attributes.Remove("data-layout");
                            //
                            // -- body found, set the htmlDoc to the body
                            var layoutDoc = new HtmlDocument();
                            layoutDoc.LoadHtml( node.InnerHtml );
                            //
                            // -- process the layout 
                            DataDeleteController.process(layoutDoc);
                            MustacheVariableController.process(layoutDoc);
                            MustacheSectionController.process(layoutDoc);
                            MustacheTruthyController.process(layoutDoc);
                            MustacheInvertedSectionController.process(layoutDoc);
                            MustacheValueController.process(layoutDoc);
                            DataAddonController.process(cp, layoutDoc);
                            //
                            // -- save the alyout
                            LayoutModel layout = null;
                            if ((layout == null) && !string.IsNullOrWhiteSpace(layoutRecordName)) {
                                layout = DbBaseModel.createByUniqueName<LayoutModel>(cp, layoutRecordName);
                                if (layout == null) {
                                    layout = DbBaseModel.addDefault<LayoutModel>(cp);
                                    layout.name = layoutRecordName;
                                }
                                if(cp.Site.htmlPlatformVersion == 5) {
                                    layout.layoutPlatform5.content = HtmlController.unwrapMustacheAttributes(layoutDoc.DocumentNode.OuterHtml);
                                } else {
                                    layout.layout.content = HtmlController.unwrapMustacheAttributes(layoutDoc.DocumentNode.OuterHtml);
                                }
                                layout.save(cp);
                                userMessageList.Add("Saved Layout '" + layoutRecordName + "' from data-layout attribute.");
                            }
                        }
                    }
                    //
                    // -- the layout was not found, return the entire layout
                }
            }
        }
    }
}
