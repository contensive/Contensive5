//
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Collections.Generic;
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
                //
                // -- get password field names to mask in the export
                string passwordFieldNamesCsv = cp.Doc.GetText("passwordFieldNames");
                var passwordFieldNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                if (!string.IsNullOrEmpty(passwordFieldNamesCsv)) {
                    foreach (string fieldName in passwordFieldNamesCsv.Split(',')) {
                        if (!string.IsNullOrWhiteSpace(fieldName)) {
                            passwordFieldNames.Add(fieldName.Trim());
                        }
                    }
                }
                using (var db = cp.DbNew(dataSource)) {
                    //
                    // -- no way to know how big this is. 30 minute timeout
                    db.SQLTimeout = 1800;
                    using (DataTable dt = db.ExecuteQuery(cp.Doc.GetText("sql"))) {
                        //
                        // -- mask password columns before converting to csv
                        if (passwordFieldNames.Count > 0) {
                            foreach (DataColumn col in dt.Columns) {
                                if (passwordFieldNames.Contains(col.ColumnName)) {
                                    foreach (DataRow row in dt.Rows) {
                                        row[col] = "****";
                                    }
                                }
                            }
                        }
                        string result = dt.toCsv();
                        //
                        logger.Trace($"{core.logCommonMessage},ExportCsvClass.execute, result [" + (result.Length > 100 ? result.Substring(0, 100) : result) + "]");
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
