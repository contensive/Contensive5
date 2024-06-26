﻿//
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Data;
//
namespace Contensive.Processor.Addons.ExportSql {
    //
    public class ExportCsvClass : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// execute an sql command on a given datasource and save the result as csv in a cdn file
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                logger.Trace($"{core.logCommonMessage},ExportCsvClass.execute, sql [" + cp.Doc.GetText("sql") + "]");
                //
                string dataSource = cp.Doc.GetText("datasource");
                using ( var db = cp.DbNew(dataSource)) {
                    //
                    // -- no way to know how big this is. 30 minute timeout
                    db.SQLTimeout = 1800;
                    using (DataTable dt = db.ExecuteQuery(cp.Doc.GetText("sql"))) {
                        string result = dt.toCsv();
                        //
                        logger.Trace($"{core.logCommonMessage},ExportCsvClass.execute, result [" + (result.Length>100 ? result.Substring(0,100) : result) + "]");
                        //
                        return result;
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}
