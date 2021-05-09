
using Contensive.Processor.Controllers;
using System;
using System.Xml;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Software updates
    /// </summary>
    public static class SoftwareUpdatesClass {
        //====================================================================================================
        /// <summary>
        /// Daily, download software updates. This is a legacy process but is preserved to force
        /// and update if a patch is required.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static bool downloadAndInstall(CoreController core) {
            try {
                bool loadOK = true;
                //
                LogController.logInfo(core, "Housekeep, download and install");
                //
                var Doc = new XmlDocument { XmlResolver = null };
                string URL = "http://support.contensive.com/GetUpdates?iv=" + CoreController.codeVersion();
                Doc.Load(URL);
                if ((Doc.DocumentElement.Name.ToLowerInvariant() == GenericController.toLCase("ContensiveUpdate")) && (Doc.DocumentElement.ChildNodes.Count != 0)) {
                    foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                        string Copy = CDefSection.InnerText;
                        switch (GenericController.toLCase(CDefSection.Name)) {
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
                return loadOK;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                LogController.logAlarm(core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
        //
    }
}