
using Contensive.BaseClasses;
using System;

namespace Contensive.Processor.Addons.CollectionInstall {
    /// <summary>
    /// Background task addon that installs a collection from the library by GUID.
    /// Called via cp.Addon.ExecuteAsProcess() with argument "collectionGuid".
    /// </summary>
    public class CollectionInstallTask : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                string collectionGuid = cp.Doc.GetText("collectionGuid");
                if (string.IsNullOrEmpty(collectionGuid)) {
                    cp.Site.ErrorReport($"CollectionInstallTask: collectionGuid argument is missing or empty.");
                    return "";
                }
                string errorMessage = "";
                bool result = cp.Addon.InstallCollectionFromLibrary(collectionGuid, ref errorMessage);
                if (!result) {
                    cp.Site.ErrorReport($"CollectionInstallTask: failed to install collection [{collectionGuid}], error [{errorMessage}]");
                }
                if (!string.IsNullOrEmpty(errorMessage)) {
                    cp.Utils.AppendLog($"CollectionInstallTask: collection [{collectionGuid}], message [{errorMessage}]");
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}
