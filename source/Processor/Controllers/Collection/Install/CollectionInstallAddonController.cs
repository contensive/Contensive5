
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Reflection;
using System.Text;
using System.Xml;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    //
    // install = means everything nessesary
    // buildfolder = means download and build out site
    //
    // todo: rework how adds are installed, this change can be done after weave launch
    // - current addon folder is called local addon folder and not in shared environment /local/addons
    // - add a node to the (local) collection.xml with last collection installation datetime (files added after this starts install)
    // - in private files, new folder with zip files to install /private/collectionInstall
    // - local server checks the list and runs install on new zips, if remote file system, download and install
    // - addon manager just copies zip file into the /private/collectionInstall folder
    //
    // todo -- To make it easy to add code to a site, be able to upload DLL files. Get the class names, find the collection and install in the correct collection folder
    //
    // todo -- Even in collection files, auto discover DLL file classes and create addons out of them. Create/update collections, create collection xml and install.
    //
    //====================================================================================================
    /// <summary>
    /// install addon collections.
    /// </summary>
    public static class CollectionInstallAddonController {
        //
        //======================================================================================================
        //
        public static void installNode(CoreController core, XmlNode AddonNode, string AddonGuidFieldName, int CollectionID, ref bool return_UpgradeOK, ref ErrorReturnModel return_ErrorMessage, ref bool collectionIncludesDiagnosticAddons) {
            // todo - return bool
            return_UpgradeOK = true;
            try {
                string Basename = GenericController.toLCase(AddonNode.Name);
                if ((Basename == "page") || (Basename == "process") || (Basename == "addon") || (Basename == "add-on")) {
                    string addonName = XmlController.getXMLAttribute(core, AddonNode, "name", "No Name");
                    if (string.IsNullOrEmpty(addonName)) {
                        addonName = "No Name";
                    }
                    string addonGuid = XmlController.getXMLAttribute(core,  AddonNode, "guid", addonName);
                    if (string.IsNullOrEmpty(addonGuid)) {
                        addonGuid = addonName;
                    }
                    string navTypeName = XmlController.getXMLAttribute(core,  AddonNode, "type", "");
                    int navTypeId = getListIndex(navTypeName, navTypeIDList);
                    if (navTypeId == 0) {
                        navTypeId = NavTypeIDAddon;
                    }
                    int addonId = 0;
                    using (var cs = new CsModel(core)) {
                        string Criteria = "(" + AddonGuidFieldName + "=" + DbController.encodeSQLText(addonGuid) + ")";
                        cs.open(AddonModel.tableMetadata.contentName, Criteria, "", false);
                        if (cs.ok()) {
                            //
                            // Update the Addon
                            //
                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", UpgradeAppFromLocalCollection, GUID match with existing Add-on, Updating Add-on [" + addonName + "], Guid [" + addonGuid + "]");
                        } else {
                            //
                            // not found by GUID - search name against name to update legacy Add-ons
                            //
                            cs.close();
                            Criteria = "(name=" + DbController.encodeSQLText(addonName) + ")and(" + AddonGuidFieldName + " is null)";
                            cs.open(AddonModel.tableMetadata.contentName, Criteria, "", false);
                            if (cs.ok()) {
                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", UpgradeAppFromLocalCollection, Add-on name matched an existing Add-on that has no GUID, Updating legacy Aggregate Function to Add-on [" + addonName + "], Guid [" + addonGuid + "]");
                            }
                        }
                        if (!cs.ok()) {
                            //
                            // not found by GUID or by name, Insert a new addon
                            //
                            cs.close();
                            cs.insert(AddonModel.tableMetadata.contentName);
                            if (cs.ok()) {
                                LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", UpgradeAppFromLocalCollection, Creating new Add-on [" + addonName + "], Guid [" + addonGuid + "]");
                            }
                        }
                        if (!cs.ok()) {
                            //
                            // Could not create new Add-on
                            //
                            LogController.logInfo(core, MethodInfo.GetCurrentMethod().Name + ", UpgradeAppFromLocalCollection, Add-on could not be created, skipping Add-on [" + addonName + "], Guid [" + addonGuid + "]");
                        } else {
                            addonId = cs.getInteger("ID");
                            MetadataController.deleteContentRecords(core, "Add-on Include Rules", "addonid=" + addonId);
                            MetadataController.deleteContentRecords(core, "Add-on Content Trigger Rules", "addonid=" + addonId);
                            //
                            cs.set("collectionid", CollectionID);
                            cs.set(AddonGuidFieldName, addonGuid);
                            cs.set("name", addonName);
                            cs.set("navTypeId", navTypeId);
                            var ArgumentList = new StringBuilder();
                            var StyleSheet = new StringBuilder();
                            if (AddonNode.ChildNodes.Count > 0) {
                                foreach (XmlNode Addonfield in AddonNode.ChildNodes) {
                                    if (Addonfield is not XmlComment) {
                                        XmlNode PageInterface = Addonfield;
                                        string fieldName = null;
                                        string FieldValue = "";
                                        switch (GenericController.toLCase(Addonfield.Name)) {
                                            case "activexdll": {
                                                    //
                                                    // This is handled in BuildLocalCollectionFolder
                                                    //
                                                    break;
                                                }
                                            case "editors": {
                                                    //
                                                    // list of editors
                                                    //
                                                    foreach (XmlNode TriggerNode in Addonfield.ChildNodes) {
                                                        //
                                                        int fieldTypeId = 0;
                                                        string fieldType = null;
                                                        switch (GenericController.toLCase(TriggerNode.Name)) {
                                                            case "type": {
                                                                    fieldType = TriggerNode.InnerText;
                                                                    fieldTypeId = MetadataController.getRecordIdByUniqueName(core, "Content Field Types", fieldType);
                                                                    if (fieldTypeId > 0) {
                                                                        using (var CS2 = new CsModel(core)) {
                                                                            Criteria = "(addonid=" + addonId + ")and(contentfieldTypeID=" + fieldTypeId + ")";
                                                                            CS2.open("Add-on Content Field Type Rules", Criteria);
                                                                            if (!CS2.ok()) {
                                                                                CS2.insert("Add-on Content Field Type Rules");
                                                                            }
                                                                            if (CS2.ok()) {
                                                                                CS2.set("addonid", addonId);
                                                                                CS2.set("contentfieldTypeID", fieldTypeId);
                                                                            }
                                                                        }
                                                                    }
                                                                    break;
                                                                }
                                                            default: {
                                                                    // do nothing
                                                                    break;
                                                                }
                                                        }
                                                    }
                                                    break;
                                                }
                                            case "processtriggers": {
                                                    //
                                                    // list of events that trigger a process run for this addon
                                                    //
                                                    foreach (XmlNode TriggerNode in Addonfield.ChildNodes) {
                                                        switch (GenericController.toLCase(TriggerNode.Name)) {
                                                            case "contentchange": {
                                                                    int TriggerContentId = 0;
                                                                    string ContentNameorGuid = TriggerNode.InnerText;
                                                                    if (string.IsNullOrEmpty(ContentNameorGuid)) {
                                                                        ContentNameorGuid = XmlController.getXMLAttribute(core,  TriggerNode, "guid", "");
                                                                        if (string.IsNullOrEmpty(ContentNameorGuid)) {
                                                                            ContentNameorGuid = XmlController.getXMLAttribute(core,  TriggerNode, "name", "");
                                                                        }
                                                                    }
                                                                    using (var CS2 = new CsModel(core)) {
                                                                        Criteria = "(ccguid=" + DbController.encodeSQLText(ContentNameorGuid) + ")";
                                                                        CS2.open("Content", Criteria);
                                                                        if (!CS2.ok()) {
                                                                            Criteria = "(ccguid is null)and(name=" + DbController.encodeSQLText(ContentNameorGuid) + ")";
                                                                            CS2.open("content", Criteria);
                                                                        }
                                                                        if (CS2.ok()) {
                                                                            TriggerContentId = CS2.getInteger("ID");
                                                                        }
                                                                    }
                                                                    if (TriggerContentId != 0) {
                                                                        using (var CS2 = new CsModel(core)) {
                                                                            Criteria = "(addonid=" + addonId + ")and(contentid=" + TriggerContentId + ")";
                                                                            CS2.open("Add-on Content Trigger Rules", Criteria);
                                                                            if (!CS2.ok()) {
                                                                                CS2.insert("Add-on Content Trigger Rules");
                                                                                if (CS2.ok()) {
                                                                                    CS2.set("addonid", addonId);
                                                                                    CS2.set("contentid", TriggerContentId);
                                                                                }
                                                                            }
                                                                            CS2.close();
                                                                        }
                                                                    }
                                                                    break;
                                                                }
                                                            default: {
                                                                    // do nothing
                                                                    break;
                                                                }
                                                        }
                                                    }
                                                    break;
                                                }
                                            case "scripting": {
                                                    //
                                                    // include add-ons - NOTE - import collections must be run before interfaces
                                                    // when importing a collectin that will be used for an include
                                                    int scriptinglanguageid = (int)AddonController.ScriptLanguages.VBScript;
                                                    string ScriptingLanguage = XmlController.getXMLAttribute(core, Addonfield, "language", "").ToLowerInvariant();
                                                    if (ScriptingLanguage.Equals("javascript") || ScriptingLanguage.Equals("jscript")) {
                                                        scriptinglanguageid = (int)AddonController.ScriptLanguages.Javascript;
                                                    }
                                                    cs.set("scriptinglanguageid", scriptinglanguageid);
                                                    string ScriptingEntryPoint = XmlController.getXMLAttribute(core,  Addonfield, "entrypoint", "");
                                                    cs.set("ScriptingEntryPoint", ScriptingEntryPoint);
                                                    int ScriptingTimeout = GenericController.encodeInteger(XmlController.getXMLAttribute(core, Addonfield, "timeout", "5000"));
                                                    cs.set("ScriptingTimeout", ScriptingTimeout);
                                                    string ScriptingCode = "";
                                                    foreach (XmlNode ScriptingNode in Addonfield.ChildNodes) {
                                                        switch (GenericController.toLCase(ScriptingNode.Name)) {
                                                            case "code": {
                                                                    ScriptingCode += ScriptingNode.InnerText;
                                                                    break;
                                                                }
                                                            default: {
                                                                    // do nothing
                                                                    break;
                                                                }
                                                        }
                                                    }
                                                    cs.set("ScriptingCode", ScriptingCode);
                                                    break;
                                                }
                                            case "navigator": {
                                                    //
                                                    // create a navigator entry with a parent set to this
                                                    //
                                                    cs.save();
                                                    string menuNameSpace = XmlController.getXMLAttribute(core,  Addonfield, "NameSpace", "");
                                                    if (!string.IsNullOrEmpty(menuNameSpace)) {
                                                        string NavIconTypeString = XmlController.getXMLAttribute(core,  Addonfield, "type", "");
                                                        if (string.IsNullOrEmpty(NavIconTypeString)) {
                                                            NavIconTypeString = "Addon";
                                                        }
                                                        BuildController.verifyNavigatorEntry(core, new MetadataMiniCollectionModel.MiniCollectionMenuModel {
                                                            menuNameSpace = menuNameSpace,
                                                            name = addonName,
                                                            adminOnly = false,
                                                            developerOnly = false,
                                                            newWindow = false,
                                                            active = true,
                                                            addonName = addonName,
                                                            navIconType = NavIconTypeString,
                                                            navIconTitle = addonName
                                                        }, CollectionID);
                                                    }
                                                    break;
                                                }
                                            case "argument":
                                            case "argumentlist": {
                                                    //
                                                    // multiple argumentlist elements are concatinated with crlf
                                                    ArgumentList.Append(Addonfield.InnerText.Trim(' ') + Environment.NewLine);
                                                    break;
                                                }
                                            case "style": {
                                                    //
                                                    // import exclusive style
                                                    //
                                                    string NodeName = XmlController.getXMLAttribute(core,  Addonfield, "name", "");
                                                    string NewValue = encodeText(Addonfield.InnerText).Trim(' ');
                                                    if (NewValue.left(1) != "{") {
                                                        NewValue = "{" + NewValue;
                                                    }
                                                    if (NewValue.Substring(NewValue.Length - 1) != "}") {
                                                        NewValue += "}";
                                                    }
                                                    StyleSheet.Append(NodeName + " " + NewValue + Environment.NewLine);
                                                    break;
                                                }
                                            case "stylesheet":
                                            case "styles": {
                                                    //
                                                    // import exclusive stylesheet if more then whitespace
                                                    //
                                                    string test = Addonfield.InnerText;
                                                    test = strReplace(test, " ", "");
                                                    test = strReplace(test, "\r", "");
                                                    test = strReplace(test, "\n", "");
                                                    test = strReplace(test, "\t", "");
                                                    if (!string.IsNullOrEmpty(test)) {
                                                        StyleSheet.Append(Addonfield.InnerText + Environment.NewLine);
                                                    }
                                                    break;
                                                }
                                            case "template":
                                            case "content":
                                            case "admin": {
                                                    //
                                                    // these add-ons will be "non-developer only" in navigation
                                                    //
                                                    fieldName = Addonfield.Name;
                                                    FieldValue = Addonfield.InnerText;
                                                    if (!cs.isFieldSupported(fieldName)) {
                                                        //
                                                        // Bad field name - need to report it somehow
                                                        //
                                                    } else {
                                                        cs.set(fieldName, FieldValue);
                                                        if (GenericController.encodeBoolean(Addonfield.InnerText)) {
                                                            //
                                                            // if template, admin or content - let non-developers have navigator entry
                                                            //
                                                        }
                                                    }
                                                    break;
                                                }
                                            case "icon": {
                                                    //
                                                    // icon
                                                    //
                                                    FieldValue = XmlController.getXMLAttribute(core,  Addonfield, "link", "");
                                                    if (!string.IsNullOrEmpty(FieldValue)) {
                                                        //
                                                        // Icons can be either in the root of the website or in content files
                                                        //
                                                        FieldValue = GenericController.strReplace(FieldValue, "\\", "/"); // make it a link, not a file
                                                        if (GenericController.strInstr(1, FieldValue, "://") != 0) {
                                                            //
                                                            // the link is an absolute URL, leave it link this
                                                            //
                                                        } else {
                                                            if (FieldValue.left(1) != "/") {
                                                                //
                                                                // make sure it starts with a slash to be consistance
                                                                //
                                                                FieldValue = "/" + FieldValue;
                                                            }
                                                            if (FieldValue.left(17) == "/contensivefiles/") {
                                                                //
                                                                // in content files, start link without the slash
                                                                //
                                                                FieldValue = FieldValue.Substring(17);
                                                            }
                                                        }
                                                        cs.set("IconFilename", FieldValue);
                                                        {
                                                            cs.set("IconWidth", GenericController.encodeInteger(XmlController.getXMLAttribute(core,  Addonfield, "width", "0")));
                                                            cs.set("IconHeight", GenericController.encodeInteger(XmlController.getXMLAttribute(core,  Addonfield, "height", "0")));
                                                            cs.set("IconSprites", GenericController.encodeInteger(XmlController.getXMLAttribute(core,  Addonfield, "sprites", "0")));
                                                        }
                                                    }
                                                    break;
                                                }
                                            case "includeaddon":
                                            case "includeadd-on":
                                            case "include addon":
                                            case "include add-on": {
                                                    //
                                                    // processed in phase2 of this routine, after all the add-ons are installed
                                                    //
                                                    break;
                                                }
                                            case "form": {
                                                    //
                                                    // The value of this node is the xml instructions to create a form. Take then
                                                    //   entire node, children and all, and save them in the formxml field.
                                                    //   this replaces the settings add-on type, and soo to be report add-on types as well.
                                                    //   this removes the ccsettingpages and settingcollectionrules, etc.
                                                    //
                                                    {
                                                        cs.set("formxml", Addonfield.InnerXml);
                                                    }
                                                    break;
                                                }
                                            case "javascript":
                                            case "javascriptinhead": {
                                                    //
                                                    // these all translate to JSFilename
                                                    //
                                                    fieldName = "jsfilename";
                                                    cs.set(fieldName, Addonfield.InnerText);

                                                    break;
                                                }
                                            case "iniframe": {
                                                    //
                                                    // typo - field is inframe
                                                    //
                                                    fieldName = "inframe";
                                                    cs.set(fieldName, Addonfield.InnerText);
                                                    break;
                                                }
                                            case "diagnostic": {
                                                    bool fieldValue = encodeBoolean(Addonfield.InnerText);
                                                    cs.set("diagnostic", fieldValue);
                                                    collectionIncludesDiagnosticAddons = collectionIncludesDiagnosticAddons || fieldValue;
                                                    break;
                                                }
                                            case "category": {
                                                    if (!string.IsNullOrWhiteSpace(Addonfield.InnerText)) {
                                                        AddonCategoryModel category = DbBaseModel.createByUniqueName<AddonCategoryModel>(core.cpParent, Addonfield.InnerText);
                                                        if (category == null) {
                                                            category = DbBaseModel.addDefault<AddonCategoryModel>(core.cpParent);
                                                            category.name = Addonfield.InnerText;
                                                            category.save(core.cpParent);
                                                        }
                                                        cs.set("addonCategoryId", category.id);
                                                    }
                                                    break;
                                                }
                                            case "instancesettingprimarycontentid": {
                                                    int lookupContentId = 0;
                                                    if (!string.IsNullOrWhiteSpace(Addonfield.InnerText)) {
                                                        ContentModel lookupContent = DbBaseModel.createByUniqueName<ContentModel>(core.cpParent, Addonfield.InnerText);
                                                        if (lookupContent != null) {
                                                            lookupContentId = lookupContent.id;
                                                        }
                                                    }
                                                    cs.set("instancesettingprimarycontentid", lookupContentId);
                                                    break;
                                                }
                                            default: {
                                                    //
                                                    // All the other fields should match the Db fields
                                                    //
                                                    fieldName = Addonfield.Name;
                                                    FieldValue = Addonfield.InnerText;
                                                    if (!cs.isFieldSupported(fieldName)) {
                                                        //
                                                        // Bad field name - need to report it somehow
                                                        //
                                                        LogController.logWarn(core, new InvalidOperationException("bad field found [" + fieldName + "], in addon node [" + addonName + "], of collection [" + MetadataController.getRecordName(core, "add-on collections", CollectionID) + "]"));
                                                    } else {
                                                        cs.set(fieldName, FieldValue);
                                                    }
                                                    break;
                                                }
                                        }
                                    }
                                }
                            }
                            cs.set("ArgumentList", ArgumentList.ToString());
                            cs.set("StylesFilename", StyleSheet.ToString());
                        }
                        cs.close();
                        //
                        // -- style and js minification
                        if (addonId > 0) {
                            var addon = DbBaseModel.create<AddonModel>(core.cpParent,addonId);
                            if (addon != null) {
                                MinifyController.minifyAddon(core, addon);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
