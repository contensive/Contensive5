﻿//
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Addons.Primitives {
    public class DownloadClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- Active Download hook
                LibraryFilesModel file = LibraryFilesModel.create<LibraryFilesModel>(core.cpParent, core.docProperties.getText(rnDownloadFileGuid));
                if (file != null) {
                    //
                    // -- lookup record and set clicks
                    if (file != null) {
                        file.clicks += 1;
                        file.save(core.cpParent);
                        if (file.filename != "") {
                            //
                            // -- create log entry
                            LibraryFileLogModel log = DbBaseModel.addEmpty<LibraryFileLogModel>(core.cpParent);
                            string logName = core.dateTimeNowMockable + " user [#" + core.session.user.name + ", " + core.session.user.name + "]";
                            if (log != null) {
                                log.name = logName;
                                log.fileId = file.id;
                                log.visitId = core.session.visit.id;
                                log.memberId = core.session.user.id;
                                log.fromUrl = core.webServer.requestPageReferer;
                                log.save(core.cpParent);
                            }
                            //
                            LogController.addActivityCompleted(core, "File download", logName + ", downloaded file [" + file.name + "]", core.session.user.id, (int)ActivityLogModel.ActivityLogTypeEnum.OnlineVisit);
                            //
                            // -- and go
                            string link = GenericController.getCdnFileLink(core, file.filename);
                            return core.webServer.redirect(link, "Redirecting because the active download request variable is set to a valid Library Files record..");
                        }
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}
