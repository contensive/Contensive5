﻿
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// Recover password form and process
    /// </summary>
    public static class PasswordRecoveryController {
        //
        //=============================================================================
        /// <summary>
        /// Send password recovery
        /// </summary>
        /// <returns></returns>
        public static string getPasswordRecoveryForm(CoreController core) {
            string returnResult = "";
            try {
                string QueryString = null;
                //
                if (core.siteProperties.getBoolean("allowPasswordEmail", true)) {
                    returnResult += Properties.Resources.defaultForgetPassword_html;
                    //
                    // write out all of the form input (except state) to hidden fields so they can be read after login
                    returnResult += HtmlController.inputHidden("Type", FormTypePasswordRecovery);
                    foreach (string formKey in core.docProperties.getKeyList()) {
                        var formValue = core.docProperties.getProperty(formKey);
                        if (formValue.propertyType == DocPropertyModel.DocPropertyTypesEnum.form) {
                            switch (toUCase(formValue.name)) {
                                case "S":
                                case "MA":
                                case "MB":
                                case "USERNAME":
                                case "PASSWORD":
                                case "EMAIL":
                                case "TYPE":
                                    break;
                                default:
                                    returnResult += HtmlController.inputHidden(formValue.name, formValue.value);
                                    break;
                            }
                        }
                    }
                    QueryString = core.doc.refreshQueryString;
                    QueryString = modifyQueryString(QueryString, "S", "");
                    QueryString = modifyQueryString(QueryString, "ccIPage", "");
                    returnResult = HtmlController.form(core, returnResult, QueryString);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnResult;
        }
        //
        //========================================================================
        /// <summary>
        /// processes a simple email password form that can be stacked into the login page
        /// </summary>
        /// <param name="core"></param>
        public static void processPasswordRecoveryForm(CoreController core, AuthTokenInfoModel authTokenInfo) {
            try {
                string returnUserMessage = "";
                string userEmail = core.docProperties.getText("email");
                if (string.IsNullOrEmpty(userEmail)) {
                    // -- no email entered
                    return;
                }
                List<PersonModel> users = DbBaseModel.createList<PersonModel>(core.cpParent, $"email={DbController.encodeSQLText(userEmail)}");
                if (users.Count != 1) {
                    // -- 0 or 2+ users found
                    return;
                }
                EmailController.trySendPasswordReset(core, users[0], authTokenInfo,  ref returnUserMessage);
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
