using Contensive.BaseClasses;
using HtmlAgilityPack;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers {
    public static class DataAddonController {
        //
        public static void process(CPBaseClass cp, HtmlDocument htmlDoc) {
            string content = "";
            string addonName = "";
            {
                string xPath = "//*[contains(@class,'mustache-addon')]";
                HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                if (nodeList != null) {
                    foreach (HtmlNode node in nodeList) {
                        IEnumerable<string> classList = node.GetClasses();
                        if (classList != null) {
                            string lastClass = "";
                            foreach (string className in classList) {
                                if (lastClass.Equals("mustache-addon")) {
                                    addonName = className.Replace("_", " ");
                                    node.InnerHtml = "{% \"" + addonName + "\" %}";
                                    node.RemoveClass(className);
                                    node.RemoveClass("mustache-addon");
                                    break;
                                }
                                lastClass = className;
                            }
                        }
                        //
                        // -- if the addon does not exist, create it with the content removed
                        if (!string.IsNullOrEmpty(addonName) && !string.IsNullOrEmpty(content)) {
                            Contensive.Models.Db.AddonModel addon = Contensive.Models.Db.DbBaseModel.createByUniqueName<Contensive.Models.Db.AddonModel>(cp, addonName);
                            if (addon == null) {
                                addon = Contensive.Models.Db.DbBaseModel.addDefault<Contensive.Models.Db.AddonModel>(cp);
                                if (addon != null) {
                                    addon.name = addonName;
                                    addon.copyText = content;
                                    addon.save(cp);
                                }
                            }
                        }
                    }
                }
            }
            {
                //
                // -- data-mustach-addon (legacy)
                string xPath = "//*[@data-mustache-addon]";
                HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                if (nodeList != null) {
                    foreach (HtmlNode node in nodeList) {
                        addonName = node.Attributes["data-mustache-addon"]?.Value;
                        node.Attributes.Remove("data-mustache-addon");
                        node.InnerHtml = "{% \"" + addonName + "\" %}";




                        //
                        // -- if the addon does not exist, create it with the content removed
                        if (!string.IsNullOrEmpty(addonName) && !string.IsNullOrEmpty(content)) {
                            Contensive.Models.Db.AddonModel addon = Contensive.Models.Db.DbBaseModel.createByUniqueName<Contensive.Models.Db.AddonModel>(cp, addonName);
                            if (addon == null) {
                                addon = Contensive.Models.Db.DbBaseModel.addDefault<Contensive.Models.Db.AddonModel>(cp);
                                if (addon != null) {
                                    addon.name = addonName;
                                    addon.copyText = content;
                                    addon.save(cp);
                                }
                            }
                        }
                    }
                }
            }
            {
                //
                // -- data-mustach-addon
                string xPath = "//*[@data-addon]";
                HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                if (nodeList != null) {
                    foreach (HtmlNode node in nodeList) {
                        addonName = node.Attributes["data-addon"]?.Value;
                        node.Attributes.Remove("data-addon");
                        content = node.InnerHtml;
                        node.InnerHtml = "{% \"" + addonName + "\" %}";




                        //
                        // -- if the addon does not exist, create it with the content removed
                        if (!string.IsNullOrEmpty(addonName) && !string.IsNullOrEmpty(content)) {
                            Contensive.Models.Db.AddonModel addon = Contensive.Models.Db.DbBaseModel.createByUniqueName<Contensive.Models.Db.AddonModel>(cp, addonName);
                            if (addon == null) {
                                addon = Contensive.Models.Db.DbBaseModel.addDefault<Contensive.Models.Db.AddonModel>(cp);
                                if (addon != null) {
                                    addon.name = addonName;
                                    addon.copyText = content;
                                    addon.save(cp);
                                }
                            }
                        }
                    }
                }
            }



        }
    }
}