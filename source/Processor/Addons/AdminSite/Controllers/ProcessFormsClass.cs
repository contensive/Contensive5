using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using System;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// 
    /// </summary>
    public static class ProcessFormController {
        //
        public  static void processForms(CPClass cp, AdminDataModel adminData) {
            try {
                if (adminData.srcFormId != 0) {
                    string EditorStyleRulesFilename = null;
                    switch (adminData.srcFormId) {
                        case AdminFormReports: {
                                //
                                // Reports form cancel button
                                //
                                switch (adminData.srcFormButton) {
                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.dstFormId = AdminFormRoot;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        case AdminFormQuickStats: {
                                switch (adminData.srcFormButton) {
                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.dstFormId = AdminFormRoot;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        case AdminFormPublishing: {
                                //
                                // Publish Form
                                //
                                switch (adminData.srcFormButton) {
                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.dstFormId = AdminFormRoot;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        case AdminFormIndex: {

                                switch (adminData.srcFormButton) {
                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.dstFormId = AdminFormRoot;
                                            adminData.adminContent = new ContentMetadataModel();
                                            break;
                                        }
                                    case ButtonClose: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.dstFormId = AdminFormRoot;
                                            adminData.adminContent = new ContentMetadataModel();
                                            break;
                                        }
                                    case ButtonAdd: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.dstFormId = AdminFormEdit;
                                            break;
                                        }

                                    case ButtonFind: {
                                            adminData.admin_Action = Constants.AdminActionFind;
                                            adminData.dstFormId = adminData.srcFormId;
                                            break;
                                        }

                                    case ButtonFirst: {
                                            adminData.listViewRecordTop = 0;
                                            adminData.dstFormId = adminData.srcFormId;
                                            break;
                                        }
                                    case ButtonPrevious: {
                                            adminData.listViewRecordTop -= adminData.listViewRecordsPerPage;
                                            if (adminData.listViewRecordTop < 0) {
                                                adminData.listViewRecordTop = 0;
                                            }
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.dstFormId = adminData.srcFormId;
                                            break;
                                        }

                                    case ButtonNext: {
                                            adminData.admin_Action = Constants.AdminActionNext;
                                            adminData.dstFormId = adminData.srcFormId;
                                            break;
                                        }

                                    case ButtonDelete: {
                                            adminData.admin_Action = Constants.AdminActionDeleteRows;
                                            adminData.dstFormId = adminData.srcFormId;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                // end case
                                break;

                            }
                        case AdminFormEdit: {
                                //
                                // Edit Form
                                //
                                string tableName = adminData.adminContent.tableName.ToLowerInvariant();
                                switch (adminData.srcFormButton) {
                                    case ButtonRefresh: {
                                            //
                                            // this is a test operation. need this so the user can set editor preferences without saving the record
                                            //   during refresh, the edit page is redrawn just was it was, but no save
                                            //
                                            adminData.admin_Action = Constants.AdminActionEditRefresh;
                                            adminData.dstFormId = AdminFormEdit;
                                            break;

                                        }

                                    case ButtonMarkReviewed: {
                                            adminData.admin_Action = Constants.AdminActionMarkReviewed;
                                            adminData.dstFormId = AdminFormIndex;
                                            adminData.allowRedirectToRefer = true;
                                            break;

                                        }

                                    case ButtonSaveandInvalidateCache: {
                                            adminData.admin_Action = Constants.AdminActionReloadCDef;
                                            adminData.dstFormId = AdminFormEdit;
                                            break;

                                        }

                                    case ButtonDelete: {
                                            adminData.admin_Action = Constants.AdminActionDelete;
                                            adminData.dstFormId = AdminFormIndex;
                                            adminData.allowRedirectToRefer = true;
                                            break;

                                        }

                                    case ButtonSave: {
                                            adminData.admin_Action = Constants.AdminActionSave;
                                            adminData.dstFormId = AdminFormEdit;
                                            break;

                                        }

                                    case ButtonSaveAddNew: {
                                            adminData.admin_Action = Constants.AdminActionSaveAddNew;
                                            adminData.dstFormId = AdminFormEdit;
                                            break;

                                        }

                                    case ButtonOK: {
                                            adminData.admin_Action = Constants.AdminActionSave;
                                            adminData.dstFormId = AdminFormIndex;
                                            adminData.allowRedirectToRefer = true;
                                            break;

                                        }

                                    case ButtonCancel: {
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.dstFormId = AdminFormIndex;
                                            adminData.allowRedirectToRefer = true;
                                            break;

                                        }
                                    case ButtonSendTest: {
                                            if (tableName == "ccemail") {
                                                //
                                                // -- Test email
                                                adminData.admin_Action = Constants.AdminActionSendEmailTest;
                                                adminData.dstFormId = AdminFormEdit;
                                                break;
                                            }
                                            if (tableName == "ccgrouptextmessages") {
                                                //
                                                // -- Test text message
                                                adminData.admin_Action = Constants.AdminActionSendTextMessageTest;
                                                adminData.dstFormId = AdminFormEdit;
                                                break;
                                            }
                                            if (tableName == "ccsystemtextmessages") {
                                                //
                                                // -- Test text message
                                                adminData.admin_Action = Constants.AdminActionSendTextMessageTest;
                                                adminData.dstFormId = AdminFormEdit;
                                                break;
                                            }
                                            break;
                                        }
                                    case ButtonSend: {
                                            if (tableName == "ccemail") {
                                                //
                                                // -- Send a Group Email
                                                adminData.admin_Action = Constants.AdminActionSendEmail;
                                                adminData.dstFormId = AdminFormEdit;
                                                break;
                                            }
                                            if (tableName == "ccgrouptextmessages") {
                                                //
                                                // -- Send a Group Text Message
                                                adminData.admin_Action = Constants.AdminActionSendTextMessage;
                                                adminData.dstFormId = AdminFormEdit;
                                                break;
                                            }
                                            adminData.admin_Action = Constants.AdminActionNop;
                                            adminData.dstFormId = AdminFormEdit;
                                            break;
                                        }

                                    case ButtonActivate: {
                                            //
                                            // Activate (submit) a conditional Email
                                            //
                                            adminData.admin_Action = Constants.AdminActionActivateEmail;
                                            adminData.dstFormId = AdminFormEdit;
                                            break;

                                        }
                                    case ButtonDeactivate: {
                                            //
                                            // Deactivate (clear submit) a conditional Email
                                            //
                                            adminData.admin_Action = Constants.AdminActionDeactivateEmail;
                                            adminData.dstFormId = AdminFormEdit;
                                            break;

                                        }
                                    case ButtonCreateDuplicate: {
                                            //
                                            // Create a Duplicate record (for email)
                                            //
                                            adminData.admin_Action = Constants.AdminActionDuplicate;
                                            adminData.dstFormId = AdminFormEdit;
                                            break;

                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        case AdminFormStyleEditor: {
                                //
                                // Process actions
                                //
                                switch (adminData.srcFormButton) {
                                    case ButtonSave:
                                    case ButtonOK: {
                                            //
                                            cp.core.siteProperties.setProperty("Allow CSS Reset", cp.core.docProperties.getBoolean(RequestNameAllowCSSReset));
                                            cp.core.cdnFiles.saveFile(DynamicStylesFilename, cp.core.docProperties.getText("StyleEditor"));
                                            if (cp.core.docProperties.getBoolean(RequestNameInlineStyles)) {
                                                //
                                                // Inline Styles
                                                //
                                                cp.core.siteProperties.setProperty("StylesheetSerialNumber", "0");
                                            } else {
                                                // mark to rebuild next fetch
                                                cp.core.siteProperties.setProperty("StylesheetSerialNumber", "-1");
                                            }
                                            //
                                            // delete all templateid based editorstylerule files, build on-demand
                                            //
                                            EditorStyleRulesFilename = GenericController.strReplace(EditorStyleRulesFilenamePattern, "$templateid$", "0", 1, 99, 1);
                                            cp.core.cdnFiles.deleteFile(EditorStyleRulesFilename);
                                            //
                                            using var csData = new CsModel(cp.core);
                                            csData.openSql("select id from cctemplates");
                                            while (csData.ok()) {
                                                EditorStyleRulesFilename = GenericController.strReplace(EditorStyleRulesFilenamePattern, "$templateid$", csData.getText("ID"), 1, 99, 1);
                                                cp.core.cdnFiles.deleteFile(EditorStyleRulesFilename);
                                                csData.goNext();
                                            }
                                            csData.close();
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                switch (adminData.srcFormButton) {
                                    case ButtonCancel:
                                    case ButtonOK: {
                                            //
                                            // Process redirects
                                            //
                                            adminData.dstFormId = AdminFormRoot;
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                break;
                            }
                        default: {
                                // end case
                                break;
                            }
                    }
                }
            } catch (Exception ex) {
                LogControllerX.logError(cp.core, ex);
                throw;
            }
        }
        //
    }
}
