
using Newtonsoft.Json;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// Tracks resource files installed by a collection. Enables cleanup on uninstall and orphan removal on upgrade.
    /// </summary>
    public class ResourceManifestModel {
        //
        private const string manifestFilename = "resourceManifest.json";
        //
        public string collectionGuid { get; set; }
        public string collectionName { get; set; }
        public DateTime installedDate { get; set; }
        public List<ResourceManifestEntry> resources { get; set; } = new List<ResourceManifestEntry>();
        /// <summary>
        /// Folders created by zip extraction. Tracked so they can be removed on uninstall if empty.
        /// Each entry has a type (www, private, cdn) and the folder path.
        /// </summary>
        public List<ResourceManifestFolderEntry> folders { get; set; } = new List<ResourceManifestFolderEntry>();
        //
        /// <summary>
        /// A single resource file entry in the manifest
        /// </summary>
        public class ResourceManifestEntry {
            /// <summary>
            /// The file system the file was copied to: www, private, or cdn
            /// </summary>
            public string type { get; set; }
            /// <summary>
            /// The full relative path within that file system
            /// </summary>
            public string destinationPath { get; set; }
        }
        //
        /// <summary>
        /// A folder created by zip extraction
        /// </summary>
        public class ResourceManifestFolderEntry {
            /// <summary>
            /// The file system: www, private, or cdn
            /// </summary>
            public string type { get; set; }
            /// <summary>
            /// The folder path within that file system
            /// </summary>
            public string folderPath { get; set; }
        }
        //
        /// <summary>
        /// Save the manifest to the collection version folder as resourceManifest.json
        /// </summary>
        public static void save(CoreController core, string collectionVersionFolder, ResourceManifestModel manifest) {
            string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            core.privateFiles.saveFile(collectionVersionFolder + manifestFilename, json);
        }
        //
        /// <summary>
        /// Load the manifest from the collection version folder. Returns null if not found.
        /// </summary>
        public static ResourceManifestModel load(CoreController core, string collectionVersionFolder) {
            string pathFilename = collectionVersionFolder + manifestFilename;
            if (!core.privateFiles.fileExists(pathFilename)) { return null; }
            string json = core.privateFiles.readFileText(pathFilename);
            if (string.IsNullOrWhiteSpace(json)) { return null; }
            var result = JsonConvert.DeserializeObject<ResourceManifestModel>(json);
            if (result != null && result.folders == null) { result.folders = new List<ResourceManifestFolderEntry>(); }
            return result;
        }
        //
        /// <summary>
        /// Recursively collect all files under a path in the given file system, adding them to the manifest resources list.
        /// Also collects all subfolders into the manifest folders list.
        /// </summary>
        public static void addFilesAndFoldersRecursively(FileController fileSystem, string basePath, string type, ResourceManifestModel manifest) {
            string normalizedPath = FileController.normalizeDosPath(basePath);
            foreach (var file in fileSystem.getFileList(normalizedPath)) {
                manifest.resources.Add(new ResourceManifestEntry { type = type, destinationPath = normalizedPath + file.Name });
            }
            foreach (var folder in fileSystem.getFolderList(normalizedPath)) {
                string subFolderPath = normalizedPath + folder.Name + "\\";
                manifest.folders.Add(new ResourceManifestFolderEntry { type = type, folderPath = subFolderPath });
                addFilesAndFoldersRecursively(fileSystem, subFolderPath, type, manifest);
            }
        }
    }
}
