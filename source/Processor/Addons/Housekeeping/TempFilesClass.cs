
using Contensive.Processor.Controllers;
using System;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Addons.Housekeeping {
    /// <summary>
    /// Housekeep the temp file system
    /// </summary>
    public static class TempFilesClass {
        //
        //====================================================================================================
        /// <summary>
        /// delete all files over 1 hour old
        /// </summary>
        /// <param name="core"></param>
        public static void deleteFiles(HouseKeepEnvironmentModel env) {
            try {
                //
                env.log("Housekeep, delete temp files over 1 hour old");
                //
                deleteOldFilesReturnFilesRemaining(env, "\\");

            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
                LogControllerX.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete all files over 1 hour old from the current path, recursive
        /// return true of all files deleted so the folder can be deleted
        /// return false means there are files left
        /// </summary>
        /// <param name="core"></param>
        /// <param name="path"></param>
        public static bool deleteOldFilesReturnFilesRemaining(HouseKeepEnvironmentModel env, string path) {
            try {
                //
                // -- delete all the folders
                bool filesRemaining = false;
                foreach (var folder in env.core.tempFiles.getFolderList(path)) {
                    if (deleteOldFilesReturnFilesRemaining(env, path + folder.Name + "\\")) {
                        //
                        // -- the folder has files remaining
                        filesRemaining = true;
                        continue;
                    }
                    env.core.tempFiles.deleteFolder(path + folder.Name);
                }
                //
                // -- delete all the files
                foreach (var file in env.core.tempFiles.getFileList(path)) {
                    if (encodeDate(file.DateLastModified).AddHours(1) > env.core.dateTimeNowMockable) {
                        filesRemaining = true;
                        continue;
                    }
                    env.core.tempFiles.deleteFile(path + file.Name);
                }
                return filesRemaining;

            } catch (Exception ex) {
                LogControllerX.logError(env.core, ex);
                LogControllerX.logAlarm(env.core, "Housekeep, exception, ex [" + ex + "]");
                throw;
            }
        }

    }
}