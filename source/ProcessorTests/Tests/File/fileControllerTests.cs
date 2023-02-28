
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class FileControllerTests {
        //
        //====================================================================================================
        /// <summary>
        /// </summary>
        [TestMethod()]
        public void isValidPathFilename_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    //
                    {
                        Assert.IsFalse(cp.CdnFiles.isValidPathFilename(""));
                        Assert.IsFalse(cp.CdnFiles.isValidPathFilename(null));
                        //
                        Assert.IsTrue(cp.CdnFiles.isValidPathFilename("a.html"));
                        Assert.IsTrue(cp.CdnFiles.isValidPathFilename("a/b/c/d.html"));
                        Assert.IsTrue(cp.CdnFiles.isValidPathFilename("a\\b\\c\\d.html"));
                        //
                        Assert.IsTrue(cp.CdnFiles.isValidPathFilename(cp.CdnFiles.PhysicalFilePath + "a/b/c/d.html"));
                        Assert.IsTrue(cp.CdnFiles.isValidPathFilename(cp.CdnFiles.PhysicalFilePath + "a\\b\\c\\d.html"));
                        Assert.IsFalse(cp.CdnFiles.isValidPathFilename("c:\\tmp\\" + "a/b/c/d.html"));
                        //
                        Assert.IsFalse(cp.CdnFiles.isValidPathFilename("a*.html"));
                        Assert.IsFalse(cp.CdnFiles.isValidPathFilename("a!.html"));
                        Assert.IsFalse(cp.CdnFiles.isValidPathFilename("a@.html"));
                    }
                    //
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// </summary>
        [TestMethod()]
        public void encodeDosFilename_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    {
                        // -- simple path name
                        string okSrc = "tests\\test" + cp.Utils.GetRandomInteger() + ".txt";
                        string okDst = FileController.encodeDosPathFilename(okSrc);
                        Assert.AreEqual(okSrc, okDst);
                    }
                    {
                        // -- mulitple path name
                        string okSrc = "0\\1\\2\\3\\4\\5\\test" + cp.Utils.GetRandomInteger() + ".txt";
                        string okDst = FileController.encodeDosPathFilename(okSrc);
                        Assert.AreEqual(okSrc, okDst);
                    }
                    {
                        // -- wrong slash
                        string srcBad = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                        string dstBad = FileController.encodeDosPathFilename(srcBad);
                        Assert.AreNotEqual(srcBad, dstBad);
                    }
                    {
                        // -- simple bad characters
                        string srcBad = "<test>" + cp.Utils.GetRandomInteger() + ".txt";
                        string dstBad = FileController.encodeDosPathFilename(srcBad);
                        Assert.AreNotEqual(srcBad, dstBad);
                    }
                    //
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// </summary>
        [TestMethod()]
        public void encodeUnixFilename_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    {
                        // -- simple path name
                        string okSrc = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                        string okDst = FileController.encodeUnixPathFilename(okSrc);
                        Assert.AreEqual(okSrc, okDst);
                    }
                    {
                        // -- mulitple path name
                        string okSrc = "0/1/2/3/4/5/test" + cp.Utils.GetRandomInteger() + ".txt";
                        string okDst = FileController.encodeUnixPathFilename(okSrc);
                        Assert.AreEqual(okSrc, okDst);
                    }
                    {
                        // -- wrong slash
                        string srcBad = "tests\\test" + cp.Utils.GetRandomInteger() + ".txt";
                        string dstBad = FileController.encodeUnixPathFilename(srcBad);
                        Assert.AreNotEqual(srcBad, dstBad);
                    }
                    {
                        // -- simple bad characters
                        string srcBad = "<test>" + cp.Utils.GetRandomInteger() + ".txt";
                        string dstBad = FileController.encodeUnixPathFilename(srcBad);
                        Assert.AreNotEqual(srcBad, dstBad);
                    }
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// test
        /// </summary>
        [TestMethod()]
        public void joinPath_Test() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                // act
                //
                {
                    string result = cp.core.cdnFiles.joinPath("a/b/", "c/d/e.txt");
                    Assert.AreEqual(@"a\b\c\d\e.txt", result);
                }
                {
                    string result = cp.core.cdnFiles.joinPath("a/b", "c/d/e.txt");
                    Assert.AreEqual(@"a\b\c\d\e.txt", result);
                }
                {
                    string result = cp.core.cdnFiles.joinPath("a/b/", "/c/d/e.txt");
                    Assert.AreEqual(@"a\b\c\d\e.txt", result);
                }
                {
                    string result = cp.core.cdnFiles.joinPath("/a//b//", "//c//d//e.txt");
                    Assert.AreEqual(@"a\b\c\d\e.txt", result);
                }
                {
                    string result = cp.core.cdnFiles.joinPath(@"a\b\\", @"\c\d\\e.txt");
                    Assert.AreEqual(@"a\b\c\d\e.txt", result);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// readFileText in local mode
        /// </summary>
        [TestMethod()]
        public void readFileText_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    string fileContent = "12345";
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    cp.core.cdnFiles.saveFile(pathFilename, fileContent, true);
                    string resultContent = cp.core.cdnFiles.readFileText(pathFilename, true);
                    cp.core.cdnFiles.deleteFile(pathFilename, true);
                    Assert.AreEqual(fileContent, resultContent);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// readfiletest is remote mode
        /// </summary>
        [TestMethod()]
        public void readFileText_Remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent = "12345";
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    cp.core.cdnFiles.saveFile(pathFilename, fileContent, false);
                    string resultContent = cp.core.cdnFiles.readFileText(pathFilename, false);
                    cp.core.cdnFiles.deleteFile(pathFilename, false);
                    Assert.AreEqual(fileContent, resultContent);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// readfiletest is remote mode
        /// </summary>
        [TestMethod()]
        public void readFileBinary_Remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    byte[] fileContent = Encoding.ASCII.GetBytes("12345");
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    cp.core.cdnFiles.saveFile(pathFilename, fileContent, false);
                    byte[] resultContent = cp.core.cdnFiles.readFileBinary(pathFilename, false);
                    cp.core.cdnFiles.deleteFile(pathFilename, false);
                    // assert
                    Assert.AreEqual(fileContent.Length, resultContent.Length);
                    Assert.IsTrue(fileContent.SequenceEqual(resultContent));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// append a file in remote mode
        /// </summary>
        [TestMethod()]
        public void appendFile_Text_Remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent1 = "12345";
                    string fileContent2 = "67890";
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    cp.core.cdnFiles.appendFile(pathFilename, fileContent1, false);
                    cp.core.cdnFiles.appendFile(pathFilename, fileContent2, false);
                    string resultContent = cp.core.cdnFiles.readFileText(pathFilename, false);
                    cp.core.cdnFiles.deleteFile(pathFilename, false);
                    Assert.AreEqual(fileContent1 + fileContent2, resultContent);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// append a file in local mode
        /// </summary>
        [TestMethod()]
        public void appendFile_Text_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent1 = "12345";
                    string fileContent2 = "67890";
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    cp.core.cdnFiles.appendFile(pathFilename, fileContent1, true);
                    cp.core.cdnFiles.appendFile(pathFilename, fileContent2, true);
                    string resultContent = cp.core.cdnFiles.readFileText(pathFilename, true);
                    cp.core.cdnFiles.deleteFile(pathFilename, true);
                    Assert.AreEqual(fileContent1 + fileContent2, resultContent);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// append a file in remote mode
        /// </summary>
        [TestMethod()]
        public void createPath_Remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string parentPath = "tests/";
                    string folder = "test folder " + cp.Utils.GetRandomInteger();
                    string path = parentPath + folder + "/";
                    cp.core.cdnFiles.createPath(path, false);
                    var folderList = cp.core.cdnFiles.getFolderList(parentPath, false);
                    cp.core.cdnFiles.deleteFolder(path, false);
                    bool folderFound = false;
                    foreach (var pathTest in folderList) {
                        if (!pathTest.Name.Equals(folder)) { continue; }
                        folderFound = true;
                        break;
                    }
                    Assert.IsTrue(folderFound);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// append a file in local mode
        /// </summary>
        [TestMethod()]
        public void createPath_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string parentPath = "tests/";
                    string folder = "test folder " + cp.Utils.GetRandomInteger();
                    string path = parentPath + folder + "/";
                    cp.core.cdnFiles.createPath(path, true);
                    var folderList = cp.core.cdnFiles.getFolderList(parentPath, true);
                    cp.core.cdnFiles.deleteFolder(path, true);
                    bool folderFound = false;
                    foreach (var pathTest in folderList) {
                        if (!pathTest.Name.Equals(folder)) { continue; }
                        folderFound = true;
                        break;
                    }
                    Assert.IsTrue(folderFound);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete a file in local mode
        /// </summary>
        [TestMethod()]
        public void deleteFile_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent = cp.Utils.GetRandomInteger().ToString();
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    cp.core.cdnFiles.saveFile(pathFilename, fileContent, true);
                    //
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                    //
                    cp.core.cdnFiles.deleteFile(pathFilename, true);
                    //
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete a file in remote mode
        /// </summary>
        [TestMethod()]
        public void deleteFile_Remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent = cp.Utils.GetRandomInteger().ToString();
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    //
                    // -- save remote file
                    cp.core.cdnFiles.saveFile(pathFilename, fileContent, false);
                    //
                    // -- file exists both local and remote
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, false));
                    //
                    // -- delete local file
                    cp.core.cdnFiles.deleteFile(pathFilename, true);
                    //
                    // -- file deleted local, but remote is pk, and local is ok after remote is read
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, false));
                    Assert.AreEqual(fileContent, cp.core.cdnFiles.readFileText(pathFilename, false));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, true));
                    //
                    // -- delete remote file
                    cp.core.cdnFiles.deleteFile(pathFilename, false);
                    //
                    // -- local and remote deleted
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete a folder recursively in remote mode
        /// </summary>
        [TestMethod()]
        public void deleteFolder_Remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent = cp.Utils.GetRandomInteger().ToString();
                    string testPath = "tests/testfolder" + cp.Utils.GetRandomInteger() + "/";
                    string pathFilename1 = testPath + "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string pathFilename2 = testPath + "test" + cp.Utils.GetRandomInteger() + ".txt";
                    cp.core.cdnFiles.saveFile(pathFilename1, fileContent, false);
                    cp.core.cdnFiles.saveFile(pathFilename2, fileContent, false);
                    //
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename2, true));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename2, false));
                    //
                    cp.core.cdnFiles.deleteFolder(testPath, false);
                    //
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete a folder recursively in remote mode
        /// </summary>
        [TestMethod()]
        public void deleteFolder_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent = cp.Utils.GetRandomInteger().ToString();
                    string testPath = "tests/testfolder" + cp.Utils.GetRandomInteger() + "/";
                    string pathFilename1 = testPath + "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string pathFilename2 = testPath + "test" + cp.Utils.GetRandomInteger() + ".txt";
                    cp.core.cdnFiles.saveFile(pathFilename1, fileContent, true);
                    cp.core.cdnFiles.saveFile(pathFilename2, fileContent, true);
                    //
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename2, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, false));
                    //
                    cp.core.cdnFiles.deleteFolder(testPath, true);
                    //
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// rename a file
        /// </summary>
        [TestMethod()]
        public void renameFile_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent = cp.Utils.GetRandomInteger().ToString();
                    string testPath = "tests/testfolder" + cp.Utils.GetRandomInteger() + "/";
                    string filename1 = "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string filename2 = "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string pathFilename1 = testPath + filename1;
                    string pathFilename2 = testPath + filename2;
                    //
                    // -- save filename1 to local
                    cp.core.cdnFiles.saveFile(pathFilename1, fileContent, true);
                    //
                    // -- it exists local, not remote
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    //
                    // rename local
                    cp.core.cdnFiles.renameFile(pathFilename1, filename2, true);
                    //
                    // -- new name exists local, not remote
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename2, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, false));
                    //
                    // -- old not local or remote
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    //
                    // --read newname and it matches content written to original file
                    Assert.AreEqual(fileContent, cp.core.cdnFiles.readFileText(pathFilename2, true));
                    //
                    // -- delete new filename, both are gone
                    cp.core.cdnFiles.deleteFile(pathFilename2, true);
                    //
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, false));
                    //
                    // -- cleanup test folder
                    cp.core.cdnFiles.deleteFolder(testPath, true);
                    Assert.IsFalse(cp.core.cdnFiles.pathExists(testPath, true));
                    Assert.IsFalse(cp.core.cdnFiles.pathExists(testPath, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// rename a file remote filesystem
        /// </summary>
        [TestMethod()]
        public void renameFile_Remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent = cp.Utils.GetRandomInteger().ToString();
                    string testPath = "tests/testfolder" + cp.Utils.GetRandomInteger() + "/";
                    string filename1 = "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string filename2 = "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string pathFilename1 = testPath + filename1;
                    string pathFilename2 = testPath + filename2;
                    //
                    // -- save filename1 to remote
                    cp.core.cdnFiles.saveFile(pathFilename1, fileContent, false);
                    //
                    // -- it exists local, and remote
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    //
                    // rename remote
                    cp.core.cdnFiles.renameFile(pathFilename1, filename2, false);
                    //
                    // -- new name exists local, and remote
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename2, true));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename2, false));
                    //
                    // -- old not local or remote
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    //
                    // --read remote newname and it matches content written to original file
                    Assert.AreEqual(fileContent, cp.core.cdnFiles.readFileText(pathFilename2, false));
                    //
                    // -- delete new filename, both are gone
                    cp.core.cdnFiles.deleteFile(pathFilename2, false);
                    //
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename1, false));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename2, false));
                    //
                    // -- cleanup test folder
                    cp.core.cdnFiles.deleteFolder(testPath, false);
                    Assert.IsFalse(cp.core.cdnFiles.pathExists(testPath, true));
                    Assert.IsFalse(cp.core.cdnFiles.pathExists(testPath, false));
                }
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// save file binary remote filesystem
        /// </summary>
        [TestMethod()]
        public void saveFile_Binary_Remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    byte[] fileContent = Encoding.ASCII.GetBytes("12345");
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    //
                    // -- save file
                    cp.core.cdnFiles.saveFile(pathFilename, fileContent, false);
                    //
                    // -- remote and local exists
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, false));
                    //
                    // -- read back from remote
                    byte[] resultContent = cp.core.cdnFiles.readFileBinary(pathFilename, false);
                    Assert.AreEqual(fileContent.Length, resultContent.Length);
                    Assert.IsTrue(fileContent.SequenceEqual(resultContent));
                    //
                    // -- clean up the files
                    cp.core.cdnFiles.deleteFile(pathFilename, false);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// save file binary local filesystem
        /// </summary>
        [TestMethod()]
        public void saveFile_Binary_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    byte[] fileContent = Encoding.ASCII.GetBytes("12345");
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    //
                    // -- save local file
                    cp.core.cdnFiles.saveFile(pathFilename, fileContent, true);
                    //
                    // -- local exists, not remote
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                    //
                    // -- read back from local
                    byte[] resultContent = cp.core.cdnFiles.readFileBinary(pathFilename, true);
                    Assert.AreEqual(fileContent.Length, resultContent.Length);
                    Assert.IsTrue(fileContent.SequenceEqual(resultContent));
                    //
                    // -- clean up the files
                    cp.core.cdnFiles.deleteFile(pathFilename, true);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// save file text remote filesystem
        /// </summary>
        [TestMethod()]
        public void saveFile_Text_Remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent = "12345";
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    //
                    // -- save file
                    cp.core.cdnFiles.saveFile(pathFilename, fileContent, false);
                    //
                    // -- remote and local exists
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, false));
                    //
                    // -- read back from remote
                    string resultContent = cp.core.cdnFiles.readFileText(pathFilename, false);
                    Assert.AreEqual(fileContent.Length, resultContent.Length);
                    Assert.IsTrue(fileContent.Equals(resultContent));
                    //
                    // -- clean up the files
                    cp.core.cdnFiles.deleteFile(pathFilename, false);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// save file text local filesystem
        /// </summary>
        [TestMethod()]
        public void saveFile_Text_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string fileContent = "12345";
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    //
                    // -- save file remote
                    cp.core.cdnFiles.saveFile(pathFilename, fileContent, true);
                    //
                    // -- local exists, remote !exists
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                    //
                    // -- read back from local
                    string resultContent = cp.core.cdnFiles.readFileText(pathFilename, true);
                    Assert.AreEqual(fileContent.Length, resultContent.Length);
                    Assert.IsTrue(fileContent.Equals(resultContent));
                    //
                    // -- clean up the files
                    cp.core.cdnFiles.deleteFile(pathFilename, true);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// saveHttpRequestToFile local test
        /// </summary>
        [TestMethod()]
        public void saveHttpRequestToFile_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    //
                    // -- save file remote
                    cp.core.cdnFiles.saveHttpRequestToFile("http://www.google.com", pathFilename, true);
                    //
                    // -- local exists, remote !exists
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                    //
                    // -- read back from local
                    string resultContent = cp.core.cdnFiles.readFileText(pathFilename, true);
                    Assert.AreNotEqual(0, resultContent.Length);
                    //
                    // -- clean up the files
                    cp.core.cdnFiles.deleteFile(pathFilename, true);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// saveHttpRequestToFile remote test
        /// </summary>
        [TestMethod()]
        public void saveHttpRequestToFile_remote_Test() {
            using (CPClass cp = new(testAppName)) {
                {
                    if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                    string pathFilename = "tests/test" + cp.Utils.GetRandomInteger() + ".txt";
                    //
                    // -- save file remote
                    cp.core.cdnFiles.saveHttpRequestToFile("http://www.google.com", pathFilename, false);
                    //
                    // -- local exists, remote exists
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsTrue(cp.core.cdnFiles.fileExists(pathFilename, false));
                    //
                    // -- read back from remote
                    string resultContent = cp.core.cdnFiles.readFileText(pathFilename, false);
                    Assert.AreNotEqual(0, resultContent.Length);
                    //
                    // -- clean up the files
                    cp.core.cdnFiles.deleteFile(pathFilename, false);
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, true));
                    Assert.IsFalse(cp.core.cdnFiles.fileExists(pathFilename, false));
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// saveHttpRequestToFile remote test
        /// </summary>
        [TestMethod()]
        public void splitDosPathFilename_Test() {
            using (CPClass cp = new(testAppName)) {
                if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                {
                    string path = @"tests\";
                    string filename = "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string resultPath = "";
                    string resultFilename = "";
                    string pathFilename = path + filename;
                    cp.core.cdnFiles.splitDosPathFilename(pathFilename, ref resultPath, ref resultFilename);
                    Assert.AreEqual(path, resultPath);
                    Assert.AreEqual(filename, resultFilename);
                }
                {
                    string path = @"tests/";
                    string filename = "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string resultPath = "";
                    string resultFilename = "";
                    string pathFilename = path + filename;
                    cp.core.cdnFiles.splitDosPathFilename(pathFilename, ref resultPath, ref resultFilename);
                    Assert.AreEqual(@"tests\", resultPath);
                    Assert.AreEqual(filename, resultFilename);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// saveHttpRequestToFile remote test
        /// </summary>
        [TestMethod()]
        public void unzipFile_zipPath_Local_Test() {
            using (CPClass cp = new(testAppName)) {
                if (cp.core.cdnFiles.isLocal) { Assert.Fail("test application [" + testAppName + "] must be configured as a remote filesystem."); }
                {
                    string fileContent = cp.Utils.GetRandomInteger().ToString();
                    string srcPath = "tests/srcZipfolder" + cp.Utils.GetRandomInteger() + "/";
                    string filename1 = "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string filename2 = "test" + cp.Utils.GetRandomInteger() + ".txt";
                    string pathFilename1 = srcPath + filename1;
                    string pathFilename2 = srcPath + filename2;
                    cp.core.cdnFiles.saveFile(pathFilename1, fileContent, true);
                    cp.core.cdnFiles.saveFile(pathFilename2, fileContent, true);
                    //
                    string dstPath = "tests/dstZipfolder/";
                    string zipPathFilename = dstPath + "test.zip";
                    cp.core.cdnFiles.zipPath(zipPathFilename, srcPath);
                    //
                    cp.core.cdnFiles.unzipFile(zipPathFilename);
                    //
                    Assert.AreEqual(cp.core.cdnFiles.readFileText(srcPath + filename1, true), cp.core.cdnFiles.readFileText(dstPath + filename1, true));
                    //
                    // -- cleanup
                    cp.core.cdnFiles.deleteFolder(srcPath);
                    cp.core.cdnFiles.deleteFolder(dstPath);
                    Assert.IsFalse(cp.core.cdnFiles.pathExists(srcPath));
                    Assert.IsFalse(cp.core.cdnFiles.pathExists(dstPath));
                }
            }
        }

    }
}