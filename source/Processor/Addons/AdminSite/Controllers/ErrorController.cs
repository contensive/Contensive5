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
using Contensive.Processor.Models.Db;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Models.Domain;
using Contensive.Addons.Tools;
using static Contensive.Processor.AdminUIController;
//
namespace Contensive.Addons.AdminSite {
    public class ErrorController {
        //
        //===========================================================================
        //
        public static string get( CoreController core, string UserError, string DeveloperError) {
            string result = "";
            try {
                //
                if (!string.IsNullOrEmpty(DeveloperError)) {
                    throw (new Exception("error [" + DeveloperError + "], user error [" + UserError + "]"));
                }
                if (!string.IsNullOrEmpty(UserError)) {
                    Processor.Controllers.ErrorController.addUserError(core, UserError);
                    result = AdminDataModel.AdminFormErrorOpen + Processor.Controllers.ErrorController.getUserError(core) + AdminDataModel.AdminFormErrorClose;
                }
                //
            } catch (Exception ex) {
                LogController.handleError(core, ex);
            }
            return result;
        }
    }
}
