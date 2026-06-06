#if NET
using System;
using System.Collections.Generic;
using System.IO;
using Contensive.Processor.Models.Domain;
//
namespace Contensive.Processor.Controllers {
    //
    /// <summary>
    /// Scans installed addon collection folders for DLLs that target .NET Framework only.
    /// These DLLs may not work in the .NET 9 Core environment and need to be recompiled
    /// targeting netstandard2.0 or net9.0.
    /// </summary>
    public static class AddonCompatibilityScanner {
        //
        /// <summary>
        /// Severity level for a compatibility issue.
        /// High = references assemblies known to be unsupported in .NET Core.
        /// Low  = targets .NET Framework but does not reference known-unsupported assemblies
        ///        (likely works via the compatibility shim).
        /// </summary>
        public enum CompatibilitySeverity {
            Low,
            High
        }
        //
        /// <summary>
        /// Assembly name prefixes known to be unsupported or heavily broken in .NET Core / .NET 9.
        /// If a Framework DLL references any assembly matching these prefixes, it gets High severity.
        /// </summary>
        private static readonly string[] UnsupportedAssemblyPrefixes = new[] {
            "System.Web",
            "System.Runtime.Remoting",
            "System.EnterpriseServices",
            "System.Messaging",
            "System.ServiceModel",
            "System.Windows.Forms",
            "System.Drawing",
            "System.Data.SqlClient"
        };
        //
        public class CompatibilityIssue {
            public string CollectionName;
            public string CollectionGuid;
            public string DllFileName;
            public string TargetFramework;
            /// <summary>
            /// High if the DLL references known-unsupported assemblies; Low otherwise.
            /// </summary>
            public CompatibilitySeverity Severity;
            /// <summary>
            /// The specific unsupported assembly references found (empty for Low severity).
            /// </summary>
            public List<string> UnsupportedReferences = new List<string>();
        }
        //
        /// <summary>
        /// Scan all installed addon collection folders for Framework-only DLLs.
        /// Skips known host assemblies and System.*/Microsoft.* dependencies.
        /// Returns a list of issues found.
        /// </summary>
        public static List<CompatibilityIssue> ScanAddonCollections(CoreController core) {
            var issues = new List<CompatibilityIssue>();
            try {
                //
                // -- get the list of installed collections from Collections.xml
                var collectionList = new List<CollectionLibraryModel>();
                string errorMessage = "";
                if (!CollectionFolderModel.getCollectionFolderConfigCollectionList(core, ref collectionList, ref errorMessage)) {
                    return issues;
                }
                //
                // -- known host assemblies to skip (these are not addon code)
                var skipAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "CPBase", "Processor", "ContensiveDbModels"
                };
                //
                foreach (var collection in collectionList) {
                    if (string.IsNullOrEmpty(collection.path)) { continue; }
                    string collectionFolder = AddonController.getPrivateFilesAddonPath() + collection.path;
                    string absFolder = core.privateFiles.joinPath(core.privateFiles.localAbsRootPath, collectionFolder);
                    if (!Directory.Exists(absFolder)) { continue; }
                    //
                    foreach (string dllPath in Directory.GetFiles(absFolder, "*.dll")) {
                        string fileName = Path.GetFileNameWithoutExtension(dllPath);
                        //
                        // -- skip host assemblies
                        if (skipAssemblies.Contains(fileName)) { continue; }
                        //
                        // -- skip System.* and Microsoft.* dependencies (addon author can't control these)
                        if (fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)) { continue; }
                        if (fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)) { continue; }
                        //
                        string tfm = AssemblyMetadataHelper.GetTargetFramework(dllPath);
                        if (!string.IsNullOrEmpty(tfm) && tfm.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase)) {
                            var unsupported = FindUnsupportedReferences(dllPath);
                            issues.Add(new CompatibilityIssue {
                                CollectionName = collection.name ?? "(unknown)",
                                CollectionGuid = collection.guid ?? "",
                                DllFileName = Path.GetFileName(dllPath),
                                TargetFramework = tfm,
                                Severity = unsupported.Count > 0 ? CompatibilitySeverity.High : CompatibilitySeverity.Low,
                                UnsupportedReferences = unsupported
                            });
                        }
                    }
                }
            } catch (Exception) {
                // -- best-effort scan, do not let it interrupt the upgrade
            }
            return issues;
        }
        //
        /// <summary>
        /// Check a DLL's assembly references against the known-unsupported list.
        /// Returns the list of matched unsupported assembly names.
        /// </summary>
        private static List<string> FindUnsupportedReferences(string dllPath) {
            var matched = new List<string>();
            var references = AssemblyMetadataHelper.GetAssemblyReferences(dllPath);
            foreach (string refName in references) {
                foreach (string prefix in UnsupportedAssemblyPrefixes) {
                    if (refName.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                        || refName.StartsWith($"{prefix}.", StringComparison.OrdinalIgnoreCase)) {
                        matched.Add(refName);
                        break;
                    }
                }
            }
            return matched;
        }
    }
}
#endif
