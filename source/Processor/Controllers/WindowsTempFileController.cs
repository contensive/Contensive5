using System;
using System.IO;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Control files create and and managed by Windows Temp file system.
    /// https://www.daveoncsharp.com/2009/09/how-to-use-temporary-files-in-csharp/
    /// </summary>
    public static class WindowsTempFileController {
        //
        //====================================================================================================
        /// <summary>
        /// Cratea new windows temp file (typically in users folder). Creates 0 byte file
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string createFile(CoreController core) {
            try {
                string fileName = string.Empty;
                //
                // Get the full name of the newly created Temporary file. 
                // Note that the GetTempFileName() method actually creates
                // a 0-byte file and returns the name of the created file.
                fileName = Path.GetTempFileName();
                //
                // Create a FileInfo object to set the file's attributes
                FileInfo fileInfo = new FileInfo(fileName) {
                    //
                    // Set the Attribute property of this file to Temporary. 
                    // Although this is not completely necessary, the .NET Framework is able 
                    // to optimize the use of Temporary files by keeping them cached in memory.
                    Attributes = FileAttributes.Temporary
                };
                //
                return fileName;
            } catch (Exception ex) {
                string errMsg = "Error creating TEMP file or seting attributes";
                Logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// save text to the temp file
        /// </summary>
        /// <param name="dosAbspathFilename"></param>
        /// <param name="content"></param>
        /// <param name="core"></param>
        public static void updateFile(CoreController core, string dosAbspathFilename, string content) {
            try {
                //
                // Write to the temp file.
                StreamWriter streamWriter = File.AppendText(dosAbspathFilename);
                streamWriter.Write(content);
                streamWriter.Flush();
                streamWriter.Close();
            } catch (Exception ex) {
                string errMsg = "Error writing to TEMP file";
                Logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read the temp file
        /// </summary>
        /// <param name="core"></param>
        /// <param name="dosAbspathFilename"></param>
        public static void readFile(CoreController core, string dosAbspathFilename) {
            try {
                // Read from the temp file.
                StreamReader myReader = File.OpenText(dosAbspathFilename);
                Console.WriteLine("TEMP file contents: " + myReader.ReadToEnd());
                myReader.Close();
            } catch (Exception ex) {
                string errMsg = "Error reading TEMP file";
                Logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete a TEMP file
        /// </summary>
        /// <param name="core"></param>
        /// <param name="dosAbspathFilename"></param>
        public static void deleteFile(CoreController core, string dosAbspathFilename) {
            try {
                // Delete the temp file (if it exists)
                if (File.Exists(dosAbspathFilename)) {
                    File.Delete(dosAbspathFilename);
                    Console.WriteLine("TEMP file deleted.");
                }
            } catch (Exception ex) {
                string errMsg = "Error deleting TEMP file";
                Logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    }
}