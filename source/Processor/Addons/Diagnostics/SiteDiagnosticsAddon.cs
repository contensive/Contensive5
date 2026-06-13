
using System;
using System.Text;
using System.Data;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using Newtonsoft.Json;
//
namespace Contensive.Processor.Addons.Diagnostics {
    //
    public class SiteDiagnosticsAddon : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Returns OK on success
        /// + available drive space
        /// + log size
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                var result = new StringBuilder();
                var core = ((CPClass)cp).core;
                //
                // test default data connection
                try {
                    using (var csData = new CsModel(core)) {
                        int recordId = 0;
                        if (csData.insert("Properties")) {
                            recordId = csData.getInteger("ID");
                        }
                        if (recordId == 0) {
                            return "ERROR, Failed to insert record in default data source.";
                        } else {
                            MetadataController.deleteContentRecord(core, "Properties", recordId);
                        }
                    }
                } catch (Exception exDb) {
                    return "ERROR, exception occured during default data source record insert, [" + exDb + "].";
                }
                result.AppendLine("ok, database connection passed.");
                //
                // -- test for taskscheduler not running
                if (DbBaseModel.createList<AddonModel>(core.cpParent, "(ProcessNextRun<" + DbController.encodeSQLDate(core.dateTimeNowMockable.AddHours(-1)) + ")").Count > 0) {
                    return "ERROR, there are process addons unexecuted for over 1 hour. TaskScheduler may not be enabled, or no server is running the Contensive Task Service.";
                }
                if (DbBaseModel.createList<TaskModel>(core.cpParent, "(dateCompleted is null)and(dateStarted<" + DbController.encodeSQLDate(core.dateTimeNowMockable.AddHours(-1)) + ")").Count > 0) {
                    return "ERROR, there are tasks that have been executing for over 1 hour. The Task Runner Server may have stopped.";
                }
                result.AppendLine("ok, taskscheduler running.");
                //
                // -- test for taskrunner not running
                if (DbBaseModel.createList<TaskModel>(core.cpParent, "(dateCompleted is null)and(dateStarted is null)").Count > 100) {
                    return "ERROR, there are over 100 task waiting to be execute. The Task Runner Server may have stopped.";
                }
                result.AppendLine("ok, taskrunner running.");
                //
                // -- verify the email process is running.
                if (cp.Site.GetDate("EmailServiceLastCheck") < core.dateTimeNowMockable.AddHours(-1)) {
                    return "ERROR, Email process has not executed for over 1 hour.";
                }
                result.AppendLine("ok, email process running.");
                //
                // -- verify SMS provider is configured (1=Twilio, 2=AWS)
                int smsProviderId = cp.Site.GetInteger("SMS Provider Id", 0);
                if (smsProviderId == 0) {
                    return "ERROR, SMS provider is not configured. Set 'SMS Provider Id' in site settings (1=Twilio, 2=AWS).";
                }
                result.AppendLine("ok, SMS provider configured.");
                //
                // -- verify the default root user does not exist with a well-known password or no expiration
                var rootUserList = PersonModel.createList<PersonModel>(cp, $"((username='root')and(password='{Constants.defaultRootUserPassword_deprecated}')and(active>0))");
                if (rootUserList.Count > 0) {
                    return "ERROR, active root user found with default password. Change the root user password or deactivate the account.";
                }
                //
                // -- meta data test- lookup field without lookup set
                string sql = "select c.id as contentid, c.name as contentName, f.* from ccfields f left join ccContent c on c.id = f.LookupContentID where f.Type = 7 and c.id is null and f.LookupContentID > 0 and f.Active > 0 and f.Authorable > 0";
                using (DataTable dt = core.db.executeQuery(sql)) {
                    if (!dt.Rows.Count.Equals(0)) {
                        string badFieldList = "";
                        foreach (DataRow row in dt.Rows) {
                            badFieldList += "," + row["contentName"] + "." + row["name"].ToString();
                        }
                        return "ERROR, the following field(s) are configured as lookup, but the field's lookup-content is not set [" + badFieldList.Substring(1) + "].";
                    }
                }
                //
                // -- metadata test - many to many setup
                sql = "select f.id,f.name as fieldName,f.ManyToManyContentID, f.ManyToManyRuleContentID, f.ManyToManyRulePrimaryField, f.ManyToManyRuleSecondaryField"
                    + " ,pc.name as primaryContentName"
                    + " , sc.name as secondaryContentName"
                    + " , r.name as ruleContentName"
                    + " , rp.name as PrimaryContentField"
                    + " , rs.name as SecondaryContentField"
                    + " from ccfields f"
                    + " left join cccontent sc on sc.id = f.ManyToManyContentID"
                    + " left join cccontent pc on pc.id = f.contentid"
                    + " left join cccontent r on r.id = f.ManyToManyRuleContentID"
                    + " left join ccfields rp on (rp.name = f.ManyToManyRulePrimaryField)and(rp.ContentID = r.id)"
                    + " left join ccfields rs on(rs.name = f.ManyToManyRuleSecondaryField)and(rs.ContentID = r.id)"
                    + " where"
                    + " (f.type = 14)and(f.Authorable > 0)and(f.active > 0)"
                    + " and((1 = 0)or(sc.id is null)or(pc.id is null)or(r.id is null)or(rp.id is null)or(rs.id is null))";
                using (DataTable dt = core.db.executeQuery(sql)) {
                    if (!dt.Rows.Count.Equals(0)) {
                        string badFieldList = "";
                        foreach (DataRow row in dt.Rows) {
                            badFieldList += "," + row["primaryContentName"] + "." + row["fieldName"].ToString();
                        }
                        return "ERROR, the following field(s) are configured as many-to-many, but the field's many-to-many metadata is not set [" + badFieldList.Substring(1) + "].";
                    }
                }
                // -- verify site warnings with alarm set
                var warningList = DbBaseModel.createList<SiteWarningModel>(cp, "((alarm>0)and(active>0))");
                if (warningList.Count > 0) {
                    return $"ERROR, [{warningList.Count}] Site Warning(s) with set alarm true.";
                }
                //
                // -- check server diagnostics status (Windows updates, domain bindings, etc.)
                string serverDiagnosticsStatusJson = cp.Site.GetText("ServerDiagnosticsStatus");
                if (string.IsNullOrEmpty(serverDiagnosticsStatusJson)) {
                    return $"ERROR, Server diagnostics status not available. The serverdiagnostic command must be run with elevated permissions from the task scheduler. See README.txt for details.";
                }
                try {
                    var diagnosticsStatus = JsonConvert.DeserializeObject<ServerDiagnosticsStatusModel>(serverDiagnosticsStatusJson);
                    if (diagnosticsStatus == null) {
                        return $"ERROR, Server diagnostics status is null. The serverdiagnostic command must be run with elevated permissions from the task scheduler. See README.txt for details.";
                    }

                    // -- check drive space
                    if (!diagnosticsStatus.driveSpaceValid) {
                        return $"ERROR, {diagnosticsStatus.driveSpaceErrorMessage}";
                    }
                    result.AppendLine("ok, drive space check passed");
                    //
                    // -- check log files
                    if (!diagnosticsStatus.logFilesValid) {
                        return $"ERROR, {diagnosticsStatus.logFilesErrorMessage}";
                    }
                    result.AppendLine("ok, log files check passed");
                    //
                    // -- check alarms
                    if (!diagnosticsStatus.alarmsValid) {
                        return $"ERROR, {diagnosticsStatus.alarmsErrorMessage}";
                    }
                    result.AppendLine("ok, alarm folder check passed");
                    //
                    // -- check domain bindings
                    if (!diagnosticsStatus.domainBindingsValid) {
                        return $"ERROR, {diagnosticsStatus.domainBindingsErrorMessage}";
                    }
                    result.AppendLine("ok, all domain bindings valid");
                    //
                    // -- check TLS configuration
                    if (!diagnosticsStatus.tlsValid) {
                        return $"ERROR, {diagnosticsStatus.tlsErrorMessage}";
                    }
                    result.AppendLine("ok, TLS configuration check passed");
                    //
                    // -- check Windows updates
                    if (!diagnosticsStatus.windowsUpdateCheckSuccessful) {
                        result.AppendLine($"warning, Windows update check failed: {diagnosticsStatus.windowsUpdateErrorMessage}");
                    } else if (diagnosticsStatus.windowsUpdateCount > 0) {
                        TimeSpan timeSinceCheck = core.dateTimeNowMockable - diagnosticsStatus.lastCheckDate;
                        if (timeSinceCheck.TotalDays > 7) {
                            return $"ERROR, {diagnosticsStatus.windowsUpdateCount} Windows update(s) pending for over 1 week (last checked {timeSinceCheck.TotalDays:F0} days ago).";
                        }
                        result.AppendLine($"warning, {diagnosticsStatus.windowsUpdateCount} Windows update(s) pending (last checked: {diagnosticsStatus.lastCheckDate:yyyy-MM-dd HH:mm})");
                    } else {
                        result.AppendLine($"ok, no Windows updates pending (last checked: {diagnosticsStatus.lastCheckDate:yyyy-MM-dd HH:mm})");
                    }

                } catch (Exception exDiagnosticsStatus) {
                    return $"ERROR, could not parse server diagnostics status: {exDiagnosticsStatus.Message}";
                }
                //
                return "ok, all server diagnostics passed" + Environment.NewLine + result.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return $"ERROR, unexpected exception during diagnostics [{ex.Message}]";
            }
        }
    }
}
