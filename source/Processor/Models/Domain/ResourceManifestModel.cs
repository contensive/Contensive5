
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
        /// Unzip a file into a temp folder, then copy each extracted file to the destination one at a time.
        /// If the file does not already exist at the destination, add it to the manifest.
        /// Then copy the file to the destination regardless.
        /// </summary>
        public static void unzipToTempThenCopy(CoreController core, FileController dstFileSystem, string dstPath, string zipPathFilename, string type, ResourceManifestModel manifest) {
            string tempPath = $"installZip{GenericController.getRandomInteger()}\\";
            try {
                core.tempFiles.createPath(tempPath);
                //
                // -- copy the zip to temp and extract there
                dstFileSystem.copyFile(zipPathFilename, tempPath + System.IO.Path.GetFileName(zipPathFilename), core.tempFiles);
                core.tempFiles.unzipFile(tempPath + System.IO.Path.GetFileName(zipPathFilename));
                core.tempFiles.deleteFile(tempPath + System.IO.Path.GetFileName(zipPathFilename));
                //
                // -- iterate extracted files and copy to destination
                string normalizedDstPath = FileController.normalizeDosPath(dstPath);
                copyTempToDestRecursively(core, dstFileSystem, tempPath, normalizedDstPath, type, manifest);
            } finally {
                core.tempFiles.deleteFolder(tempPath);
            }
        }
        //
        /// <summary>
        /// Recursively copy files from temp folder to destination. Track new files in the manifest.
        /// </summary>
        public static void copyTempToDestRecursively(CoreController core, FileController dstFileSystem, string tempPath, string dstPath, string type, ResourceManifestModel manifest, bool alwaysAddToManifest = false) {
            string normalizedTempPath = FileController.normalizeDosPath(tempPath);
            string normalizedDstPath = FileController.normalizeDosPath(dstPath);
            foreach (var file in core.tempFiles.getFileList(normalizedTempPath)) {
                string dstFilePath = normalizedDstPath + file.Name;
                if (alwaysAddToManifest || !dstFileSystem.fileExists(dstFilePath)) {
                    manifest.resources.Add(new ResourceManifestEntry { type = type, destinationPath = dstFilePath });
                }
                core.tempFiles.copyFile(normalizedTempPath + file.Name, dstFilePath, dstFileSystem);
            }
            foreach (var folder in core.tempFiles.getFolderList(normalizedTempPath)) {
                if (string.IsNullOrEmpty(folder.Name)) { continue; }
                string subTempPath = normalizedTempPath + folder.Name + "\\";
                string subDstPath = normalizedDstPath + folder.Name + "\\";
                if (alwaysAddToManifest || !dstFileSystem.pathExists(subDstPath)) {
                    manifest.folders.Add(new ResourceManifestFolderEntry { type = type, folderPath = subDstPath });
                    if (!dstFileSystem.pathExists(subDstPath)) {
                        dstFileSystem.createPath(subDstPath);
                    }
                }
                copyTempToDestRecursively(core, dstFileSystem, subTempPath, subDstPath, type, manifest, alwaysAddToManifest);
            }
        }
    }
}
