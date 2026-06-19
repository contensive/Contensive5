
using System;
using System.IO;
using System.Linq;
using Contensive.Processor;
using Contensive.Processor.Models.Domain;

namespace Contensive.CLI {
    static class MigrateWebrootCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--migratewebroot"
            + Environment.NewLine + "    Migrate a Core WebApi app to separate application binaries from"
            + Environment.NewLine + "    static files. Moves binaries to an app\\ folder and leaves static"
            + Environment.NewLine + "    files in www\\. Use -a to specify the application.";
        //
        // ====================================================================================================
        /// <summary>
        /// Migrate a Core WebApi app from overlapping paths (binaries + static files in www\)
        /// to separated paths (binaries in app\, static files in www\).
        /// </summary>
        public static void execute(CPClass cp, string appName) {
            if (string.IsNullOrEmpty(appName)) {
                Console.WriteLine("The --migratewebroot command requires an application name. Use -a appName --migratewebroot.");
                return;
            }
            if (!cp.core.serverConfig.apps.ContainsKey(appName)) {
                Console.WriteLine($"Application [{appName}] not found.");
                return;
            }
            var appConfig = (AppConfigModel)cp.core.serverConfig.apps[appName];
            if (!appConfig.enabled) {
                Console.WriteLine($"Application [{appName}] is disabled. Enable it first with --enable.");
                return;
            }
            //
            // -- validate this is a Core app with overlapping paths
            if (File.Exists(Path.Combine(appConfig.localWwwPath, "default.aspx"))) {
                Console.WriteLine($"Application [{appName}] is a Framework app. Migration is only for Core WebApi apps.");
                return;
            }
            if (!File.Exists(Path.Combine(appConfig.localWwwPath, "WebApi.dll"))) {
                Console.WriteLine($"Application [{appName}] does not have WebApi.dll in [{appConfig.localWwwPath}]. Nothing to migrate.");
                return;
            }
            if (!string.IsNullOrEmpty(appConfig.localAppPath)) {
                Console.WriteLine($"Application [{appName}] already has localAppPath set to [{appConfig.localAppPath}]. Already migrated.");
                return;
            }
            //
            // -- determine new app path by replacing trailing www\ with app\
            string wwwPath = appConfig.localWwwPath.TrimEnd('\\');
            string parentPath = Path.GetDirectoryName(wwwPath);
            string newAppPath = Path.Combine(parentPath, "app") + "\\";
            //
            Console.WriteLine($"Migrating [{appName}]:");
            Console.WriteLine($"  Application binaries: {appConfig.localWwwPath} -> {newAppPath}");
            Console.WriteLine($"  Static files remain:  {appConfig.localWwwPath}");
            Console.WriteLine();
            //
            // -- create the app folder
            NewAppCmd.setupDirectory(newAppPath);
            //
            // -- file patterns to move (application binaries and config)
            string[] fileExtensionsToMove = { ".dll", ".exe", ".pdb", ".deps.json", ".runtimeconfig.json" };
            string[] specificFilesToMove = { "web.config" };
            string[] fileGlobsToMove = { "appsettings*.json" };
            string[] directoriesToMove = { "runtimes", "logs", "webroot" };
            //
            // -- move matching files
            int movedFiles = 0;
            int movedDirs = 0;
            foreach (string file in Directory.GetFiles(appConfig.localWwwPath)) {
                string fileName = Path.GetFileName(file);
                bool shouldMove = fileExtensionsToMove.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    || specificFilesToMove.Any(f => fileName.Equals(f, StringComparison.OrdinalIgnoreCase))
                    || fileGlobsToMove.Any(glob => {
                        string pattern = glob.Replace("*", "");
                        string ext = Path.GetExtension(glob);
                        return fileName.StartsWith(pattern.Replace(ext, "").Replace("*", ""), StringComparison.OrdinalIgnoreCase)
                            && fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
                    });
                if (shouldMove) {
                    string destFile = Path.Combine(newAppPath, fileName);
                    File.Move(file, destFile);
                    movedFiles++;
                }
            }
            //
            // -- move matching directories
            foreach (string dirName in directoriesToMove) {
                string sourceDir = Path.Combine(appConfig.localWwwPath, dirName);
                if (Directory.Exists(sourceDir)) {
                    string destDir = Path.Combine(newAppPath, dirName);
                    Directory.Move(sourceDir, destDir);
                    movedDirs++;
                }
            }
            //
            // -- update appsettings.json in the new app path to point WebRootPath to localWwwPath
            string appSettingsPath = Path.Combine(newAppPath, "appsettings.json");
            if (File.Exists(appSettingsPath)) {
                string json = File.ReadAllText(appSettingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                using var stream = new MemoryStream();
                using (var writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions { Indented = true })) {
                    writer.WriteStartObject();
                    foreach (var prop in settings.EnumerateObject()) {
                        if (prop.Name == "Contensive") {
                            writer.WriteStartObject("Contensive");
                            foreach (var cprop in prop.Value.EnumerateObject()) {
                                if (cprop.Name != "WebRootPath") {
                                    cprop.WriteTo(writer);
                                }
                            }
                            writer.WriteString("WebRootPath", appConfig.localWwwPath);
                            writer.WriteEndObject();
                        } else {
                            prop.WriteTo(writer);
                        }
                    }
                    writer.WriteEndObject();
                }
                File.WriteAllText(appSettingsPath, System.Text.Encoding.UTF8.GetString(stream.ToArray()));
            }
            //
            // -- update config
            appConfig.localAppPath = newAppPath;
            cp.core.serverConfig.save(cp.core);
            //
            // -- update IIS site to point to the new app path
            using (CPClass appCp = new CPClass(appName)) {
                string primaryDomain = appConfig.domainList.Count > 0 ? appConfig.domainList[0] : appName;
                appCp.core.webServer.verifySite(appName, primaryDomain, newAppPath);
            }
            //
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Migration complete: {movedFiles} file(s) and {movedDirs} folder(s) moved.");
            Console.ResetColor();
            Console.WriteLine($"  localAppPath: {newAppPath}");
            Console.WriteLine($"  localWwwPath: {appConfig.localWwwPath}");
            Console.WriteLine($"  IIS site updated to [{newAppPath}]");
        }
    }
}
