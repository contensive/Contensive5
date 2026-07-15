#if NET
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
//
namespace Contensive.Processor.Controllers {
    /// <summary>
    /// An isolated AssemblyLoadContext for a single addon execution.
    /// Loads the addon's own DLL and its co-located dependencies from the addon folder.
    /// Shared types (CPBase.dll, Processor.dll, ContensiveDbModels.dll) resolve from the
    /// default context so that cross-context casts (e.g. (AddonBaseClass)instance) succeed.
    /// </summary>
    internal class AddonAssemblyLoadContext : AssemblyLoadContext {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string _addonDirectory;
        private readonly string[] _sharedAssemblyNames;
        //
        /// <param name="addonDirectory">The folder containing the addon DLL and its dependency DLLs.</param>
        /// <param name="sharedAssemblyNames">Assembly simple names (case-insensitive) that must come from the default context.</param>
        public AddonAssemblyLoadContext(string addonDirectory, string[] sharedAssemblyNames)
            : base(isCollectible: false) {
            _addonDirectory = addonDirectory;
            _sharedAssemblyNames = sharedAssemblyNames;
        }
        //
        /// <summary>
        /// Called by the runtime when the addon references a dependency not yet loaded in this context.
        /// Shared assemblies return null so they resolve from the default (host) context.
        /// Other assemblies are loaded from the addon directory if present.
        /// </summary>
        protected override Assembly Load(AssemblyName assemblyName) {
            //
            // -- shared assemblies: return null to let the default context supply them
            foreach (string shared in _sharedAssemblyNames) {
                if (string.Equals(assemblyName.Name, shared, StringComparison.OrdinalIgnoreCase)) {
                    return null;
                }
            }
            //
            // -- System.* and Microsoft.* assemblies: prefer the default context version if loaded.
            // -- Addon folders often contain older Framework-era versions that are incompatible
            // -- with .NET 9 (e.g. System.Data.SqlClient with struct layout issues).
            // -- However, some Microsoft.* packages are addon-specific NuGet dependencies that
            // -- don't exist in the runtime (e.g. Microsoft.Bcl.AsyncInterfaces). If the default
            // -- context doesn't have it, let the addon's copy load in the isolated context.
            if (assemblyName.Name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
                || assemblyName.Name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)) {
                //
                // -- check if the default context already has this assembly loaded
                Assembly defaultLoaded = Array.Find(
                    AppDomain.CurrentDomain.GetAssemblies(),
                    a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase)
                );
                if (defaultLoaded != null) {
                    return null;
                }
                //
                // -- not in default context — load from addon folder if available
                string msCandidate = Path.Combine(_addonDirectory, $"{assemblyName.Name}.dll");
                if (File.Exists(msCandidate)) {
                    return LoadFromAssemblyPath(msCandidate);
                }
                return null;
            }
            //
            // -- check the addon directory for a matching DLL
            string candidatePath = Path.Combine(_addonDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(candidatePath)) {
                var loaded = LoadFromAssemblyPath(candidatePath);
                logger.Warn($"AddonAssemblyLoadContext, resolved [{assemblyName.Name}] from addon folder [{candidatePath}], version [{loaded.GetName().Version}]");
                return loaded;
            }
            //
            // -- not in addon folder: fall through to default context
            logger.Warn($"AddonAssemblyLoadContext, [{assemblyName.Name}] not found in addon folder [{_addonDirectory}], falling through to default context");
            return null;
        }
    }
}
#endif
