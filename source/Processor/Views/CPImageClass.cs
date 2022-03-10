
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor {
    public class CPImageClass : BaseClasses.CPImageBaseClass {
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        // ====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cpParent"></param>
        //
        public CPImageClass(CPClass cpParent) {
            cp = cpParent;
        }
        //
        // ====================================================================================================
        //
        public override string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            return ImageController.getBestFit(cp.core, imagePathFilename, holeWidth, holeHeight, imageAltSizeList);
        }

        public override string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.getBestFit(cp.core, imagePathFilename, holeWidth, holeHeight);
        }

        public override string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            return ImageController.getBestFitWebP(cp.core, imagePathFilename, holeWidth, holeHeight, imageAltSizeList);
        }

        public override string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.getBestFitWebP(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
    }
}