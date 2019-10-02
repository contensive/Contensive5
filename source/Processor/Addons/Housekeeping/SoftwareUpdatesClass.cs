﻿
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using System.Xml;

namespace Contensive.Addons.Housekeeping {
    //
    public class SoftwareUpdatesClass {
        //====================================================================================================
        //
        public static bool DownloadAndInstall(CoreController core) {
            bool loadOK = true;
            try {
                XmlDocument Doc = null;
                string URL = null;
                string Copy = null;
                //
                Doc = new XmlDocument();
                URL = "http://support.contensive.com/GetUpdates?iv=" + core.codeVersion();
                loadOK = true;
                Doc.Load(URL);
                if ((Doc.DocumentElement.Name.ToLowerInvariant() == GenericController.vbLCase("ContensiveUpdate")) && (Doc.DocumentElement.ChildNodes.Count != 0)) {
                    foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                        Copy = CDefSection.InnerText;
                        switch (GenericController.vbLCase(CDefSection.Name)) {
                            case "mastervisitnamelist":
                                //
                                // Read in the interfaces and save to Add-ons
                                core.privateFiles.saveFile("config\\VisitNameList.txt", Copy);
                                break;
                            case "masteremailbouncefilters":
                                //
                                // save the updated filters file
                                core.privateFiles.saveFile("config\\EmailBounceFilters.txt", Copy);
                                break;
                            case "mastermobilebrowserlist":
                                //
                                // save the updated filters file
                                //
                                core.privateFiles.saveFile("config\\MobileBrowserList.txt", Copy);
                                break;
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return loadOK;
        }
        //
    }
}