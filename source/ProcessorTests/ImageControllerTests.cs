
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
                string dstImageCdnPathFilename_dos_returned = ImageController.getBestFit(cp.core, srcImageCdnPathFilename_dos, 50, 50, new System.Collections.Generic.List<string>());
                string dstImageCdnPathFilename_unix_returned = ImageController.getBestFit(cp.core, srcImageCdnPathFilename_unix, 50, 50, new System.Collections.Generic.List<string>());
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
                string dstImageCdnPathFilename_dos_returned = ImageController.getBestFit(cp.core, srcImageCdnPathFilename_dos, 50, 50, new System.Collections.Generic.List<string>());
                //
                // assert
                Assert.AreEqual(dstImageCdnPathFilename_expected, dstImageCdnPathFilename_dos_returned);
            }
        }

    }
}