#if NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
//
namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Reads assembly metadata (target framework) from a DLL without loading it.
    /// Uses System.Reflection.Metadata + System.Reflection.PortableExecutable to
    /// inspect PE headers and custom attributes only — no assembly loading occurs.
    /// </summary>
    public static class AssemblyMetadataHelper {
        //
        /// <summary>
        /// Read the TargetFrameworkAttribute from a DLL without loading it.
        /// Returns the TFM string (e.g. ".NETFramework,Version=v4.8", ".NETStandard,Version=v2.0",
        /// ".NETCoreApp,Version=v9.0") or null if not found or on error.
        /// </summary>
        public static string GetTargetFramework(string dllPath) {
            try {
                using var stream = File.OpenRead(dllPath);
                using var peReader = new PEReader(stream);
                if (!peReader.HasMetadata) { return null; }
                var metadataReader = peReader.GetMetadataReader();
                foreach (var attrHandle in metadataReader.GetAssemblyDefinition().GetCustomAttributes()) {
                    var attr = metadataReader.GetCustomAttribute(attrHandle);
                    //
                    // -- resolve the attribute constructor to get the attribute type name
                    if (attr.Constructor.Kind == HandleKind.MemberReference) {
                        var memberRef = metadataReader.GetMemberReference((MemberReferenceHandle)attr.Constructor);
                        if (memberRef.Parent.Kind == HandleKind.TypeReference) {
                            var typeRef = metadataReader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);
                            string typeName = metadataReader.GetString(typeRef.Name);
                            if (typeName == "TargetFrameworkAttribute") {
                                //
                                // -- decode the fixed argument (the TFM string)
                                var blobReader = metadataReader.GetBlobReader(attr.Value);
                                // -- skip the 2-byte prolog (always 0x0001)
                                blobReader.ReadUInt16();
                                string tfm = blobReader.ReadSerializedString();
                                return tfm;
                            }
                        }
                    }
                }
            } catch {
                // -- file is not a valid PE/managed assembly, or I/O error
            }
            return null;
        }
        //
        /// <summary>
        /// Returns true if the DLL targets .NETFramework (not .NETStandard or .NETCoreApp).
        /// Returns false if the target framework cannot be determined.
        /// </summary>
        public static bool IsFrameworkOnly(string dllPath) {
            string tfm = GetTargetFramework(dllPath);
            if (string.IsNullOrEmpty(tfm)) { return false; }
            return tfm.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase);
        }
        //
        /// <summary>
        /// Read the list of referenced assembly names from a DLL without loading it.
        /// Uses the PE metadata AssemblyReference table. Returns an empty list on error.
        /// </summary>
        public static List<string> GetAssemblyReferences(string dllPath) {
            var references = new List<string>();
            try {
                using var stream = File.OpenRead(dllPath);
                using var peReader = new PEReader(stream);
                if (!peReader.HasMetadata) { return references; }
                var metadataReader = peReader.GetMetadataReader();
                foreach (var refHandle in metadataReader.AssemblyReferences) {
                    var asmRef = metadataReader.GetAssemblyReference(refHandle);
                    string refName = metadataReader.GetString(asmRef.Name);
                    if (!string.IsNullOrEmpty(refName)) {
                        references.Add(refName);
                    }
                }
            } catch {
                // -- file is not a valid PE/managed assembly, or I/O error
            }
            return references;
        }
    }
}
#endif
