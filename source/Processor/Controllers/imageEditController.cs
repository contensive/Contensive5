
using System;
using System.Drawing;

namespace Contensive.Processor.Controllers {
    public class ImageEditController : IDisposable {
        private bool loaded = false;
        private string src = "";
        private System.Drawing.Image srcImage;
        private int setWidth = 0;
        private int setHeight = 0;
        //
        // dispose
        protected bool disposed;
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                    if (loaded) {
                        srcImage.Dispose();
                        srcImage = null;
                    }
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }
        //
        //
        //
        public bool load(string pathFilename, FileController fileSystem) {
            bool returnOk = false;
            try {
                fileSystem.copyFileRemoteToLocal(pathFilename);
                src = fileSystem.localAbsRootPath + pathFilename;
                if (System.IO.File.Exists(src)) {
                    srcImage = System.Drawing.Image.FromFile(src);
                    setWidth = srcImage.Width;
                    setHeight = srcImage.Height;
                    loaded = true;
                }
            } catch (Exception) {

            }
            return returnOk;
        }
        //
        //
        /// <summary>
        /// save the image to a file
        /// </summary>
        /// <param name="pathFilename"></param>
        /// <param name="fileSystem"></param>
        /// <returns></returns>
        public bool save(string pathFilename, FileController fileSystem) {
            bool returnOk = false;
            try {
                if (loaded) {
                    if (src == pathFilename) {
                        if (fileSystem.fileExists(src)) {
                            fileSystem.deleteFile(src);
                        }
                    }
                    Bitmap imgOutput = new Bitmap(srcImage, setWidth, setHeight);
                    System.Drawing.Imaging.ImageFormat imgFormat = srcImage.RawFormat;
                    imgOutput.Save(fileSystem.localAbsRootPath + pathFilename, imgFormat);
                    fileSystem.copyFileLocalToRemote(pathFilename);
                    imgOutput.Dispose();
                    returnOk = true;
                }
            } catch (Exception) {

            }
            return returnOk;
        }
        //
        //
        //
        public int width {
            get {
                return setWidth;
            }
            set {
                setWidth = value;
            }
        }
        //
        //
        //
        public int height {
            get {
                return setWidth;
            }
            set {
                setHeight = value;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //
        //=======================================================================================================
        //
        // IDisposable support
        //
        //=======================================================================================================
        //
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~ImageEditController()  {
            Dispose(false);
            
            
        }
        #endregion
    }
}