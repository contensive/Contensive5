
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Image methods
    /// </summary>
    public abstract class CPImageBaseClass {
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized and cropped to best fit the hole. 
        /// The image is never scaled up. If the hold is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.
        /// imageAltSizeList is a list of comma separated strings. the first string is the original filename. The rest are the sizes already created and expected to be in the cdn folder, in the format width + "x" + height. 
        /// </summary>
        /// <param name="imagePathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        /// <param name="imageAltSizeList"></param>
        public abstract string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized and cropped to best fit the hole. 
        /// The image is never scaled up. If the hold is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.  
        /// </summary>
        /// <param name="imagePathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        public abstract string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized and cropped to best fit the hole, in .webP format.
        /// The image is never scaled up. If the hold is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.  
        /// imageAltSizeList is a list of comma separated strings. the first string is the original filename. The rest are the sizes already created and expected to be in the cdn folder, in the format width + "x" + height. 
        /// </summary>
        /// <param name="imagePathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        /// <param name="imageAltSizeList"></param>
        public abstract string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized and cropped to best fit the hole, in .webP format. 
        /// The image is never scaled up. If the hold is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.  
        /// </summary>
        /// <param name="imagePathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        public abstract string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //====================================================================================================
        // deprecated
        //
    }
}

