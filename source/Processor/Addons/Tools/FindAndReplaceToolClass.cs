
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class FindAndReplaceToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get(((CPClass)cpBase).core);
        }
        //
        //=============================================================================
        // Find and Replace launch tool
        //=============================================================================
        //
        public static string get(CoreController core) {
            string result = "";
            try {
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                //
                Stream.add(AdminUIController.getHeaderTitleDescription("Find and Replace", "This tool runs a find and replace operation on content throughout the site."));
                //
                // Process the form
                //
                string Button = core.docProperties.getText("button");
                bool IsDeveloper = core.session.isAuthenticatedDeveloper();
                int RowPtr = 0;
                string CDefList = "";
                string FindText = "";
                string ReplaceText = "";
                string lcName = null;
                if (Button == ButtonFindAndReplace) {
                    int RowCnt = core.docProperties.getInteger("CDefRowCnt");
                    if (RowCnt > 0) {
                        for (RowPtr = 0; RowPtr < RowCnt; RowPtr++) {
                            if (core.docProperties.getBoolean("Cdef" + RowPtr)) {
                                lcName = GenericController.toLCase(core.docProperties.getText("CDefName" + RowPtr));
                                if (IsDeveloper || (lcName == "page content") || (lcName == "copy content") || (lcName == "page templates")) {
                                    CDefList = CDefList + "," + lcName;
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(CDefList)) {
                            CDefList = CDefList.Substring(1);
                        }
                        FindText = core.docProperties.getText("FindText");
                        ReplaceText = core.docProperties.getText("ReplaceText");
                        var cmdDetail = new TaskModel.CmdDetailClass {
                            addonId = 0,
                            addonName = "GetForm_FindAndReplace",
                            args = new System.Collections.Generic.Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
                                { "app", core.appConfig.name },
                                { "FindText", FindText },
                                { "ReplaceText", ReplaceText }, 
                                { "CDefNameList", CDefList } 
                            }
                        };
                        TaskSchedulerController.addTaskToQueue(core, cmdDetail, false);
                        Stream.add("Find and Replace has been requested for content definitions [" + CDefList + "], finding [" + FindText + "] and replacing with [" + ReplaceText + "]");
                    }
                } else {
                    CDefList = "Page Content,Copy Content,Page Templates";
                    FindText = "";
                    ReplaceText = "";
                }
                //
                // Display form
                //
                int FindRows = core.docProperties.getInteger("SQLRows");
                if (FindRows == 0) {
                    FindRows = core.userProperty.getInteger("FindAndReplaceFindRows", 1);
                } else {
                    core.userProperty.setProperty("FindAndReplaceFindRows", FindRows.ToString());
                }
                int ReplaceRows = core.docProperties.getInteger("ReplaceRows");
                if (ReplaceRows == 0) {
                    ReplaceRows = core.userProperty.getInteger("FindAndReplaceReplaceRows", 1);
                } else {
                    core.userProperty.setProperty("FindAndReplaceReplaceRows", ReplaceRows.ToString());
                }
                //
                Stream.add("<div>Find</div>");
                Stream.add("<TEXTAREA NAME=\"FindText\" ROWS=\"" + FindRows + "\" ID=\"FindText\" STYLE=\"width: 800px;\">" + FindText + "</TEXTAREA>");
                Stream.add("&nbsp;<INPUT TYPE=\"Text\" TabIndex=-1 NAME=\"FindTextRows\" SIZE=\"3\" VALUE=\"" + FindRows + "\" ID=\"\"  onchange=\"FindText.rows=FindTextRows.value; return true\"> Rows");
                Stream.add("<br><br>");
                //
                Stream.add("<div>Replace it with</div>");
                Stream.add("<TEXTAREA NAME=\"ReplaceText\" ROWS=\"" + ReplaceRows + "\" ID=\"ReplaceText\" STYLE=\"width: 800px;\">" + ReplaceText + "</TEXTAREA>");
                Stream.add("&nbsp;<INPUT TYPE=\"Text\" TabIndex=-1 NAME=\"ReplaceTextRows\" SIZE=\"3\" VALUE=\"" + ReplaceRows + "\" ID=\"\"  onchange=\"ReplaceText.rows=ReplaceTextRows.value; return true\"> Rows");
                Stream.add("<br><br>");
                string TopHalf = "";
                string BottomHalf = "";
                //
                using (var csData = new CsModel(core)) {
                    csData.open("Content");
                    while (csData.ok()) {
                        string RecordName = csData.getText("Name");
                        lcName = GenericController.toLCase(RecordName);
                        if (IsDeveloper || (lcName == "page content") || (lcName == "copy content") || (lcName == "page templates")) {
                            int RecordId = csData.getInteger("ID");
                            if (GenericController.strInstr(1, "," + CDefList + ",", "," + RecordName + ",") != 0) {
                                TopHalf = TopHalf + "<div>" + HtmlController.checkbox("Cdef" + RowPtr, true) + HtmlController.inputHidden("CDefName" + RowPtr, RecordName) + "&nbsp;" + csData.getText("Name") + "</div>";
                            } else {
                                BottomHalf = BottomHalf + "<div>" + HtmlController.checkbox("Cdef" + RowPtr, false) + HtmlController.inputHidden("CDefName" + RowPtr, RecordName) + "&nbsp;" + csData.getText("Name") + "</div>";
                            }
                        }
                        csData.goNext();
                        RowPtr += 1;
                    }
                }
                Stream.add(TopHalf + BottomHalf + HtmlController.inputHidden("CDefRowCnt", RowPtr));
                //
                result = AdminUIController.getToolForm(core, Stream.text, ButtonCancel + "," + ButtonFindAndReplace);
            } catch (Exception ex) {
                LogControllerX.logError(core, ex);
            }
            return result;
        }
        //
    }
}

