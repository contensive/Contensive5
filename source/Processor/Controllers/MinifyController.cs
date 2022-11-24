
using Contensive.Models.Db;
using System;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class MinifyController : IDisposable {
        //
        public static void minifyAddonStyles(CoreController core, AddonModel addon) {
            try {
                // -- minify css
                if (string.IsNullOrEmpty(addon.stylesFilename.content)) {
                    addon.minifyStylesFilename.content = "";
                } else {
                    addon.minifyStylesFilename.content = NUglify.Uglify.Css(addon.stylesFilename.content).Code;
                }
                // -- minify javascript
                if (string.IsNullOrEmpty(addon.jsFilename.content)) {
                    addon.minifyJsFilename.content = "";
                } else {
                    addon.minifyJsFilename.content = NUglify.Uglify.Css(addon.jsFilename.content).Code;
                }
                addon.save(core.cpParent);
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
        //
        //====================================================================================================
        //
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed;
        //
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~MinifyController() {
            Dispose(false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }
    //
}