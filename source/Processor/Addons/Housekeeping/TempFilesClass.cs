﻿
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;

namespace Contensive.Addons.Housekeeping {
    //
    public class TempFilesClass {

        //====================================================================================================
        /// <summary>
        /// delete all files over 1 hour old
        /// </summary>
        /// <param name="core"></param>
        public static void deleteFiles(CoreController core) {
            try {
                deleteFiles(core, "\\");

            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete all files over 1 hour old from the current path, recursive
        /// </summary>
        /// <param name="core"></param>
        /// <param name="path"></param>
        public static void deleteFiles(CoreController core, string path) {
            try {
                foreach (var folder in core.tempFiles.getFolderList(path)) {
                    deleteFiles(core, path + folder.Name + "\\");
                }
                foreach (var file in core.tempFiles.getFileList(path)) {
                    if (encodeDate(file.DateCreated).AddHours(1) < DateTime.Now) {
                        core.tempFiles.deleteFile(path + file.Name);
                    }
                }

            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }

    }
}