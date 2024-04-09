using System;
using System.IO;

namespace DefaultSite {
    public class WindowsTempFileController {
        public static string createTmpFile() {
            string fileName = string.Empty;

            try {
                fileName = Path.GetTempFileName();
                var fileInfo = new FileInfo(fileName);
                fileInfo.Attributes = FileAttributes.Temporary;
                Console.WriteLine("TEMP file created at: " + fileName);
            } catch (Exception ex) {
                Console.WriteLine("Unable to create TEMP file or set its attributes: " + ex.Message);
            }

            return fileName;
        }

        public static void updateTmpFile(string tmpFile, string content) {
            try {
                var streamWriter = File.AppendText(tmpFile);
                streamWriter.Write(content);
                streamWriter.Flush();
                streamWriter.Close();
                Console.WriteLine("TEMP file updated.");
            } catch (Exception ex) {
                Console.WriteLine("Error writing to TEMP file: " + ex.Message);
            }
        }

        public static void readTmpFile(string tmpFile) {
            try {
                var myReader = File.OpenText(tmpFile);
                Console.WriteLine("TEMP file contents: " + myReader.ReadToEnd());
                myReader.Close();
            } catch (Exception ex) {
                Console.WriteLine("Error reading TEMP file: " + ex.Message);
            }
        }

        public static void deleteTmpFile(string tmpFile) {
            try {

                if (File.Exists(tmpFile)) {
                    File.Delete(tmpFile);
                    Console.WriteLine("TEMP file deleted.");
                }

            } catch (Exception ex) {
                Console.WriteLine("Error deleteing TEMP file: " + ex.Message);
            }
        }
    }
}