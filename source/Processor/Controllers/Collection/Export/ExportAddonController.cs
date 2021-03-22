
using Contensive.BaseClasses;
using Microsoft.VisualBasic;
using System;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Export Addons to Collectionn file
    /// </summary>
    public static class ExportAddonController {
        // 
        // ====================================================================================================
        /// <summary>
        /// return an addon node
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="addonid"></param>
        /// <param name="Return_IncludeModuleGuidList"></param>
        /// <param name="Return_IncludeSharedStyleGuidList"></param>
        /// <returns></returns>
        public static string getAddonNode(CPBaseClass cp, int addonid, ref string Return_IncludeModuleGuidList, ref string Return_IncludeSharedStyleGuidList) {
            string result = "";
            try {
                using (CPCSBaseClass CS = cp.CSNew()) {
                    if (CS.OpenRecord("Add-ons", addonid)) {
                        string addonName = CS.GetText("name");
                        bool processRunOnce = CS.GetBoolean("ProcessRunOnce");
                        if (((Strings.LCase(addonName) == "oninstall") || ( Strings.LCase(addonName) == "_oninstall")))
                            processRunOnce = true;
                        // 
                        // content
                        result += ExportController.getNode( "Copy", CS.GetText("Copy"));
                        result += ExportController.getNode( "CopyText", CS.GetText("CopyText"));
                        // 
                        // DLL
                        //result += ExportController.getNode( "ActiveXProgramID", CS.GetText("objectprogramid"), true);
                        result += ExportController.getNode( "DotNetClass", CS.GetText("DotNetClass"));
                        // 
                        // Features
                        result += ExportController.getNode( "ArgumentList", CS.GetText("ArgumentList"));
                        result += ExportController.getNodeLookupContentName(cp, "instanceSettingPrimaryContentId", CS.GetInteger("instanceSettingPrimaryContentId"), "content");
                        result += ExportController.getNode( "AsAjax", CS.GetBoolean("AsAjax"));
                        result += ExportController.getNode( "Filter", CS.GetBoolean("Filter"));
                        result += ExportController.getNode( "Help", CS.GetText("Help"));
                        result += ExportController.getNode( "HelpLink", CS.GetText("HelpLink"));
                        result += System.Environment.NewLine + "\t" + "<Icon Link=\"" + CS.GetText("iconfilename") + "\" width=\"" + CS.GetInteger("iconWidth") + "\" height=\"" + CS.GetInteger("iconHeight") + "\" sprites=\"" + CS.GetInteger("iconSprites") + "\" />";
                        result += ExportController.getNode( "InIframe", CS.GetBoolean("InFrame"));
                        result += ExportController.getNode("BlockEditTools", CS.GetBoolean("BlockEditTools"));
                        result += ExportController.getNode( "AliasList", CS.GetText("aliasList"));
                        // 
                        // -- Form XML
                        result += ExportController.getNode( "FormXML", CS.GetText("FormXML"));
                        // 
                        // -- addon dependencies
                        using (CPCSBaseClass CS2 = cp.CSNew()) {
                            CS2.Open("Add-on Include Rules", "addonid=" + addonid);
                            while (CS2.OK()) {
                                int IncludedAddonID = CS2.GetInteger("IncludedAddonID");
                                using (CPCSBaseClass CS3 = cp.CSNew()) {
                                    CS3.Open("Add-ons", "ID=" + IncludedAddonID);
                                    if (CS3.OK()) {
                                        string Guid = CS3.GetText("ccGuid");
                                        if (Guid == "") {
                                            Guid = cp.Utils.CreateGuid();
                                            CS3.SetField("ccGuid", Guid);
                                        }
                                        result += System.Environment.NewLine + "\t" + "<IncludeAddon name=\"" + System.Net.WebUtility.HtmlEncode(CS3.GetText("name")) + "\" guid=\"" + Guid + "\"/>";
                                    }
                                    CS3.Close();
                                }
                                CS2.GoNext();
                            }
                            CS2.Close();
                        }
                        // 
                        // -- is inline/block
                        result += ExportController.getNode( "IsInline", CS.GetBoolean("IsInline"));
                        // 
                        // -- javascript (xmlnode may not match Db filename)
                        result += ExportController.getNode( "JavascriptInHead", CS.GetText("JSFilename"));
                        result += ExportController.getNode( "javascriptForceHead", CS.GetBoolean("javascriptForceHead"));
                        result += ExportController.getNode( "JSHeadScriptSrc", CS.GetText("JSHeadScriptSrc"));
                        // 
                        // -- javascript deprecated
                        result += ExportController.getNode( "JSBodyScriptSrc", CS.GetText("JSBodyScriptSrc"), true);
                        result += ExportController.getNode( "JavascriptBodyEnd", CS.GetText("JavascriptBodyEnd"), true);
                        result += ExportController.getNode( "JavascriptOnLoad", CS.GetText("JavascriptOnLoad"), true);
                        // 
                        // -- Placements
                        result += ExportController.getNode( "Content", CS.GetBoolean("Content"));
                        result += ExportController.getNode( "Template", CS.GetBoolean("Template"));
                        result += ExportController.getNode( "Email", CS.GetBoolean("Email"));
                        result += ExportController.getNode( "Admin", CS.GetBoolean("Admin"));
                        result += ExportController.getNode( "OnPageEndEvent", CS.GetBoolean("OnPageEndEvent"));
                        result += ExportController.getNode( "OnPageStartEvent", CS.GetBoolean("OnPageStartEvent"));
                        result += ExportController.getNode( "OnBodyStart", CS.GetBoolean("OnBodyStart"));
                        result += ExportController.getNode( "OnBodyEnd", CS.GetBoolean("OnBodyEnd"));
                        result += ExportController.getNode( "RemoteMethod", CS.GetBoolean("RemoteMethod"));
                        result += CS.FieldOK("Diagnostic") ? ExportController.getNode( "Diagnostic", CS.GetBoolean("Diagnostic")) : "";
                        //
                        // -- Presentation
                        result += ExportController.getNode("category", CS.GetText("addoncategoryid"));
                        // 
                        // -- Process
                        result += ExportController.getNode( "ProcessRunOnce", processRunOnce);
                        result += ExportController.getNode( "ProcessInterval", CS.GetInteger("ProcessInterval"));
                        // 
                        // Meta
                        // 
                        result += ExportController.getNode( "MetaDescription", CS.GetText("MetaDescription"));
                        result += ExportController.getNode( "OtherHeadTags", CS.GetText("OtherHeadTags"));
                        result += ExportController.getNode( "PageTitle", CS.GetText("PageTitle"));
                        result += ExportController.getNode( "RemoteAssetLink", CS.GetText("RemoteAssetLink"));
                        // 
                        // Styles
                        string Styles = "";
                        if (!CS.GetBoolean("BlockDefaultStyles"))
                            Styles = CS.GetText("StylesFilename").Trim();
                        string StylesTest = CS.GetText("CustomStylesFilename").Trim();
                        if (StylesTest != "") {
                            if (Styles != "")
                                Styles = Styles + System.Environment.NewLine + StylesTest;
                            else
                                Styles = StylesTest;
                        }
                        result += ExportController.getNode( "Styles", Styles);
                        result += ExportController.getNode( "styleslinkhref", CS.GetText("styleslinkhref"));
                        // 
                        // 
                        // Scripting
                        // 
                        string NodeInnerText = CS.GetText("ScriptingCode").Trim();
                        if (NodeInnerText != "")
                            NodeInnerText = System.Environment.NewLine + "\t" + "\t" + "<Code>" + ExportController.encodeCData(NodeInnerText) + "</Code>";
                        using (CPCSBaseClass CS2 = cp.CSNew()) {
                            CS2.Open("Add-on Scripting Module Rules", "addonid=" + addonid);
                            while (CS2.OK()) {
                                int ScriptingModuleID = CS2.GetInteger("ScriptingModuleID");
                                using (CPCSBaseClass CS3 = cp.CSNew()) {
                                    CS3.Open("Scripting Modules", "ID=" + ScriptingModuleID);
                                    if (CS3.OK()) {
                                        string Guid = CS3.GetText("ccGuid");
                                        if (Guid == "") {
                                            Guid = cp.Utils.CreateGuid();
                                            CS3.SetField("ccGuid", Guid);
                                        }
                                        Return_IncludeModuleGuidList = Return_IncludeModuleGuidList + System.Environment.NewLine + Guid;
                                        NodeInnerText = NodeInnerText + System.Environment.NewLine + "\t" + "\t" + "<IncludeModule name=\"" + System.Net.WebUtility.HtmlEncode(CS3.GetText("name")) + "\" guid=\"" + Guid + "\"/>";
                                    }
                                    CS3.Close();
                                }
                                CS2.GoNext();
                            }
                            CS2.Close();
                        }
                        if (NodeInnerText == "")
                            result += System.Environment.NewLine + "\t" + "<Scripting Language=\"" + CS.GetText("ScriptingLanguageID") + "\" EntryPoint=\"" + CS.GetText("ScriptingEntryPoint") + "\" Timeout=\"" + CS.GetText("ScriptingTimeout") + "\"/>";
                        else
                            result += System.Environment.NewLine + "\t" + "<Scripting Language=\"" + CS.GetText("ScriptingLanguageID") + "\" EntryPoint=\"" + CS.GetText("ScriptingEntryPoint") + "\" Timeout=\"" + CS.GetText("ScriptingTimeout") + "\">" + NodeInnerText + System.Environment.NewLine + "\t" + "</Scripting>";
                        // 
                        // Shared Styles
                        // 
                        using (CPCSBaseClass CS2 = cp.CSNew()) {
                            CS2.Open("Shared Styles Add-on Rules", "addonid=" + addonid);
                            while (CS2.OK()) {
                                int styleId = CS2.GetInteger("styleId");
                                using (CPCSBaseClass CS3 = cp.CSNew()) {
                                    CS3.Open("shared styles", "ID=" + styleId);
                                    if (CS3.OK()) {
                                        string Guid = CS3.GetText("ccGuid");
                                        if (Guid == "") {
                                            Guid = cp.Utils.CreateGuid();
                                            CS3.SetField("ccGuid", Guid);
                                        }
                                        Return_IncludeSharedStyleGuidList = Return_IncludeSharedStyleGuidList + System.Environment.NewLine + Guid;
                                        result += System.Environment.NewLine + "\t" + "<IncludeSharedStyle name=\"" + System.Net.WebUtility.HtmlEncode(CS3.GetText("name")) + "\" guid=\"" + Guid + "\"/>";
                                    }
                                    CS3.Close();
                                }
                                CS2.GoNext();
                            }
                            CS2.Close();
                        }
                        // 
                        // Process Triggers
                        // 
                        NodeInnerText = "";
                        using (CPCSBaseClass CS2 = cp.CSNew()) {
                            CS2.Open("Add-on Content Trigger Rules", "addonid=" + addonid);
                            while (CS2.OK()) {
                                int TriggerContentID = CS2.GetInteger("ContentID");
                                using (CPCSBaseClass CS3 = cp.CSNew()) {
                                    CS3.Open("content", "ID=" + TriggerContentID);
                                    if (CS3.OK()) {
                                        string Guid = CS3.GetText("ccGuid");
                                        if (Guid == "") {
                                            Guid = cp.Utils.CreateGuid();
                                            CS3.SetField("ccGuid", Guid);
                                        }
                                        NodeInnerText = NodeInnerText + System.Environment.NewLine + "\t" + "\t" + "<ContentChange name=\"" + System.Net.WebUtility.HtmlEncode(CS3.GetText("name")) + "\" guid=\"" + Guid + "\"/>";
                                    }
                                    CS3.Close();
                                }
                                CS2.GoNext();
                            }
                            CS2.Close();
                        }
                        if (NodeInnerText != "")
                            result += System.Environment.NewLine + "\t" + "<ProcessTriggers>" + NodeInnerText + System.Environment.NewLine + "\t" + "</ProcessTriggers>";
                        // 
                        // Editors
                        // 
                        if (cp.Content.IsField("Add-on Content Field Type Rules", "id")) {
                            NodeInnerText = "";
                            using (CPCSBaseClass CS2 = cp.CSNew()) {
                                CS2.Open("Add-on Content Field Type Rules", "addonid=" + addonid);
                                while (CS2.OK()) {
                                    int fieldTypeID = CS2.GetInteger("contentFieldTypeID");
                                    string fieldType = cp.Content.GetRecordName("Content Field Types", fieldTypeID);
                                    if (fieldType != "")
                                        NodeInnerText = NodeInnerText + System.Environment.NewLine + "\t" + "\t" + "<type>" + fieldType + "</type>";
                                    CS2.GoNext();
                                }
                                CS2.Close();
                            }
                            if (NodeInnerText != "")
                                result += System.Environment.NewLine + "\t" + "<Editors>" + NodeInnerText + System.Environment.NewLine + "\t" + "</Editors>";
                        }
                        // 
                        string addonGuid = CS.GetText("ccGuid");
                        if ((string.IsNullOrWhiteSpace(addonGuid))) {
                            addonGuid = cp.Utils.CreateGuid();
                            CS.SetField("ccGuid", addonGuid);
                        }
                        string NavType = CS.GetText("NavTypeID");
                        if ((NavType == ""))
                            NavType = "Add-on";
                        result = ""
                        + System.Environment.NewLine + "\t" + "<Addon name=\"" + System.Net.WebUtility.HtmlEncode(addonName) + "\" guid=\"" + addonGuid + "\" type=\"" + NavType + "\">"
                        + ExportController.tabIndent(cp, result)
                        + System.Environment.NewLine + "\t" + "</Addon>";
                    }
                    CS.Close();
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetAddonNode");
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
