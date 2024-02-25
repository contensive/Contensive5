
using Contensive.BaseClasses;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class ImageControllerTests {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_LandscapeToSquare_ScaleDown_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, true, false, false, true);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_PortraitToSquare_ScaleDown_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, true, true, false, true);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_LandscapeToSquare_ScaleDown_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, false, false, false, true);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_PortraitToSquare_ScaleDown_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, false, true, false, true);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_LandscapeToSquare_ScaleDown_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, true, false, true, true);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_PortraitToSquare_ScaleDown_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, true, true, true, true);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_LandscapeToSquare_ScaleDown_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, false, false, true, true);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_PortraitToSquare_ScaleDown_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, false, true, true, true);
            }
        }







        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_LandscapeToSquare_ScaleUp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, true, false, false, false);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_PortraitToSquare_ScaleUp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, true, true, false, false);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_LandscapeToSquare_ScaleUp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, false, false, false, false);
                //
                // pad is not tranparent
                //
                Assert.Fail();
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_PortraitToSquare_ScaleUp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, false, true, false, false);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_LandscapeToSquare_ScaleUp_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, true, false, true, false);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_PortraitToSquare_ScaleUp_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, true, true, true, false);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_LandscapeToSquare_ScaleUp_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, false, false, true, false);
                //
                // pad is not tranparent
                //
                Assert.Fail();
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_PortraitToSquare_ScaleUp_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                resizeTestmethod(cp, false, true, true, false);
            }
        }





        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeTestmethod(CPClass cp, bool cropOrPad, bool portraitOrLandscape, bool toWebP, bool resizeDown) {

            // arrange
            int finalWidth;
            int finalHeight;
            if (resizeDown) {
                 finalWidth = 300;
                 finalHeight = 300;
            } else {
                 finalWidth = 3000;
                 finalHeight = 3000;
            }
            string srcImageCdnPathFilename_unix;
            string dstImageCdnPathFilename_expected;
            if (portraitOrLandscape) {
                // -- portrait
                srcImageCdnPathFilename_unix = "designblocks/img/SampleTextAndImage.png";
                if (cropOrPad) {
                    // -- crop
                    if (toWebP) {
                        // -- crop webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/SampleTextAndImage-{finalWidth}x{finalHeight}.webp";
                    } else {
                        // -- crop non-webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/SampleTextAndImage-{finalWidth}x{finalHeight}.png";
                    }
                } else {
                    if (toWebP) {
                        // -- pad webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/SampleTextAndImage-pad-{finalWidth}x{finalHeight}.webp";
                    } else {
                        // -- pad non-webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/SampleTextAndImage-pad-{finalWidth}x{finalHeight}.png";
                    }
                }
            } else {
                // -- landscpae
                srcImageCdnPathFilename_unix = "designblocks/img/CarouselSample.png";
                //srcImageCdnPathFilename_unix = "designblocks/img/CarouselSample.png";
                if (cropOrPad) {
                    // -- crop
                    if (toWebP) {
                        // -- crop webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/CarouselSample-{finalWidth}x{finalHeight}.webp";
                    } else {
                        // -- crop non-webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/CarouselSample-{finalWidth}x{finalHeight}.png";
                    }
                } else {
                    if (toWebP) {
                        // -- pad webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/CarouselSample-pad-{finalWidth}x{finalHeight}.webp";
                    } else {
                        // -- pad non-webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/CarouselSample-pad-{finalWidth}x{finalHeight}.png";
                    }
                }
            }
            //
            // -- delete image if it exists
            cp.CdnFiles.DeleteFile(dstImageCdnPathFilename_expected);
            //
            // -- clear all cach
            cp.Cache.InvalidateAll();
            //
            // -- verify files (test dos and unix filenames)
            // -- upload any changes
            cp.CdnFiles.CopyLocalToRemote(srcImageCdnPathFilename_unix);
            Assert.IsTrue(cp.CdnFiles.FileExists(srcImageCdnPathFilename_unix));
            Assert.IsFalse(cp.CdnFiles.FileExists(dstImageCdnPathFilename_expected));
            //
            // act
            string dstImageCdnPathFilename_unix_returned;
            if (cropOrPad) {
                // -- crop
                if (toWebP) {
                    // -- crop webp
                    dstImageCdnPathFilename_unix_returned = ImageController.resizeAndCropWebP(cp.core, srcImageCdnPathFilename_unix, finalWidth, finalHeight, []);
                } else {
                    // -- crop non-webp
                    dstImageCdnPathFilename_unix_returned = ImageController.resizeAndCrop(cp.core, srcImageCdnPathFilename_unix, finalWidth, finalHeight, []);
                }
            } else {
                if (toWebP) {
                    // -- pad webp
                    dstImageCdnPathFilename_unix_returned = ImageController.resizeAndPadWebP(cp.core, srcImageCdnPathFilename_unix, finalWidth, finalHeight, []);
                } else {
                    // -- pad non-webp
                    dstImageCdnPathFilename_unix_returned = ImageController.resizeAndPad(cp.core, srcImageCdnPathFilename_unix, finalWidth, finalHeight, []);
                }
            }
            //
            // assert
            Assert.AreEqual(dstImageCdnPathFilename_expected, dstImageCdnPathFilename_unix_returned);
            //
            // -- cleanup
            cp.CdnFiles.DeleteFile(dstImageCdnPathFilename_expected);
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void png_resize_src_dos_unix_slash() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string srcImageCdnPathFilename_dos = "designblocks\\img\\CarouselSample.png";
                string srcImageCdnPathFilename_unix = "designblocks/img/CarouselSample.png";
                string dstImageCdnPathFilename_expected = "designblocks/img/CarouselSample-50x50.png";
                //
                // -- delete image if it exists
                cp.CdnFiles.DeleteFile(dstImageCdnPathFilename_expected);
                //
                // -- clear all cach
                cp.Cache.InvalidateAll();
                //
                // -- verify files (test dos and unix filenames)
                Assert.IsTrue(cp.CdnFiles.FileExists(srcImageCdnPathFilename_dos));
                Assert.IsTrue(cp.CdnFiles.FileExists(srcImageCdnPathFilename_unix));
                Assert.IsFalse(cp.CdnFiles.FileExists(dstImageCdnPathFilename_expected));
                //
                // act - convert png, 1920x600 to 50x50
                string dstImageCdnPathFilename_dos_returned = ImageController.resizeAndCrop(cp.core, srcImageCdnPathFilename_dos, 50, 50, []);
                string dstImageCdnPathFilename_unix_returned = ImageController.resizeAndCrop(cp.core, srcImageCdnPathFilename_unix, 50, 50, []);
                //
                // assert
                Assert.AreEqual(dstImageCdnPathFilename_expected, dstImageCdnPathFilename_dos_returned);
                Assert.AreEqual(dstImageCdnPathFilename_expected, dstImageCdnPathFilename_unix_returned);
                //
                // -- cleanup
                cp.CdnFiles.DeleteFile(dstImageCdnPathFilename_expected);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void bad_fileExtension() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string srcImageCdnPathFilename_dos = "designblocks\\img\\CarouselSample.bad";
                string dstImageCdnPathFilename_expected = "designblocks/img/CarouselSample.bad";
                //
                // -- cannot delete expected
                //
                // -- clear all cach
                cp.Cache.InvalidateAll();
                //
                // act - convert 1920x600 to 50x50
                string dstImageCdnPathFilename_dos_returned = ImageController.resizeAndCrop(cp.core, srcImageCdnPathFilename_dos, 50, 50, new System.Collections.Generic.List<string>());
                //
                // assert
                Assert.AreEqual(dstImageCdnPathFilename_expected, dstImageCdnPathFilename_dos_returned);
            }
        }

    }
}