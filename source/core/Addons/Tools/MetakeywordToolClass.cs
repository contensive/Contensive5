﻿
using System;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Contensive.Processor;
using Contensive.Processor.Models.DbModels;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.genericController;
using static Contensive.Processor.constants;
//
namespace Contensive.Addons.Tools {
    //
    public class MetakeywordToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return GetForm_MetaKeywordTool((CPClass)cpBase);
        }
        //
        //========================================================================
        // Tool to enter multiple Meta Keywords
        //========================================================================
        //
        private string GetForm_MetaKeywordTool( CPClass cp) {
            string tempGetForm_MetaKeywordTool = "";
                coreController core = cp.core;
            try {
                stringBuilderLegacyController Content = new stringBuilderLegacyController();
                string Copy = null;
                string Button = null;
                string Description = null;
                string ButtonList = null;
                string KeywordList = null;
                //
                Button = core.docProperties.getText(RequestNameButton);
                if (Button == ButtonCancel) {
                    //
                    // Cancel just exits with no content
                    //
                    return tempGetForm_MetaKeywordTool;
                } else if (!core.session.isAuthenticatedAdmin(core)) {
                    //
                    // Not Admin Error
                    //
                    ButtonList = ButtonCancel;
                    Content.Add(adminUIController.GetFormBodyAdminOnly());
                } else {
                    Content.Add(adminUIController.EditTableOpen);
                    //
                    // Process Requests
                    //
                    switch (Button) {
                        case ButtonSave:
                        case ButtonOK:
                            //
                            string[] Keywords = null;
                            string Keyword = null;
                            int Cnt = 0;
                            int Ptr = 0;
                            DataTable dt = null;
                            int CS = 0;
                            KeywordList = core.docProperties.getText("KeywordList");
                            if (!string.IsNullOrEmpty(KeywordList)) {
                                KeywordList = genericController.vbReplace(KeywordList, "\r\n", ",");
                                Keywords = KeywordList.Split(',');
                                Cnt = Keywords.GetUpperBound(0) + 1;
                                for (Ptr = 0; Ptr < Cnt; Ptr++) {
                                    Keyword = Keywords[Ptr].Trim(' ');
                                    if (!string.IsNullOrEmpty(Keyword)) {
                                        //Dim dt As DataTable

                                        dt = core.db.executeQuery("select top 1 ID from ccMetaKeywords where name=" + core.db.encodeSQLText(Keyword));
                                        if (dt.Rows.Count == 0) {
                                            CS = core.db.csInsertRecord("Meta Keywords");
                                            if (core.db.csOk(CS)) {
                                                core.db.csSet(CS, "name", Keyword);
                                            }
                                            core.db.csClose(ref CS);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                    if (Button == ButtonOK) {
                        //
                        // Exit on OK or cancel
                        //
                        return tempGetForm_MetaKeywordTool;
                    }
                    //
                    // KeywordList
                    //
                    Copy = htmlController.inputTextarea(core, "KeywordList", "", 10);
                    Copy += "<div>Paste your Meta Keywords into this text box, separated by either commas or enter keys. When you hit Save or OK, Meta Keyword records will be made out of each word. These can then be checked on any content page.</div>";
                    Content.Add(adminUIController.getEditRowLegacy(core, Copy, "Paste Meta Keywords", "", false, false, ""));
                    //
                    // Buttons
                    //
                    ButtonList = ButtonCancel + "," + ButtonSave + "," + ButtonOK;
                    //
                    // Close Tables
                    //
                    Content.Add(adminUIController.EditTableClose);
                    Content.Add(htmlController.inputHidden(rnAdminSourceForm, AdminFormSecurityControl));
                }
                //
                Description = "Use this tool to enter multiple Meta Keywords";
                tempGetForm_MetaKeywordTool = adminUIController.getBody(core, "Meta Keyword Entry Tool", ButtonList, "", true, true, Description, "", 0, Content.Text);
                Content = null;
                //
                ///Dim th as integer: Exit Function
                //
                // ----- Error Trap
                //
            } catch (Exception ex) {
                logController.handleError(core, ex);
            }
            return tempGetForm_MetaKeywordTool;
        }
    }
}
