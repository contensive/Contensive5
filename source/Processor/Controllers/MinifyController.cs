
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
        //====================================================================================================
        //
        public static void minifyAddon(CoreController core, AddonModel addon) {
            try {
                {
                    string cssRaw = "";
                    //
                    // -- css text box
                    if (!string.IsNullOrEmpty(addon.stylesFilename.content)) {
                        cssRaw += Environment.NewLine + addon.stylesFilename.content;
                    }
                    //
                    // -- css url
                    string cssUrl = AddonModel.getPlatformAsset(core.cpParent, addon.StylesLinkPlatform5Href, addon.stylesLinkHref);
                    if (AddonModel.isAssetUrlLocal(core.cpParent, cssUrl)) {
                        // -- detect if in www files, or cdn files by checking if it starts with the serverConfig cdn prefix
                        if (cssUrl.IndexOf(core.appConfig.cdnFileUrl) == 0) {
                            // -- url is in cdn files
                            cssRaw += Environment.NewLine + core.cdnFiles.readFileText(cssUrl.Substring(core.appConfig.cdnFileUrl.Length));
                        } else {
                            // -- url is in www files
                            cssRaw += Environment.NewLine + core.wwwFiles.readFileText(cssUrl);
                        }
                    }
                    addon.minifyStylesFilename.content = NUglify.Uglify.Css(cssRaw).Code;
                }
                {
                    //
                    // -- minify javascript
                    string jsRaw = "";
                    //
                    // -- js textbox
                    if (!string.IsNullOrEmpty(addon.jsFilename.content)) {
                        jsRaw += Environment.NewLine + addon.jsFilename.content;
                    }
                    //
                    // -- js Url
                    string jsUrl = AddonModel.getPlatformAsset(core.cpParent, addon.JSHeadScriptPlatform5Src, addon.jsHeadScriptSrc); // (core.siteProperties.htmlPlatformVersion == 5 && !string.IsNullOrEmpty(addon.JSHeadScriptPlatform5Src)) ? addon.JSHeadScriptPlatform5Src : addon.jsHeadScriptSrc;
                    jsUrl = jsUrl.Trim();
                    if (AddonModel.isAssetUrlLocal(core.cpParent, jsUrl)) {
                        if (jsUrl.IndexOf(core.appConfig.cdnFileUrl) == 0) {
                            // -- url is in cdn files
                            jsRaw += Environment.NewLine + core.cdnFiles.readFileText(jsUrl.Substring(core.appConfig.cdnFileUrl.Length));
                        } else {
                            // -- url is in www files
                            jsRaw += Environment.NewLine + core.wwwFiles.readFileText(jsUrl);
                        }
                    }
                    addon.minifyJsFilename.content = NUglify.Uglify.Js(jsRaw).Code;
                }
                //
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