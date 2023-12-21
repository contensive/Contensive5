
using System;
using Contensive.Processor;

using System.Text;
using System.IO;
using Contensive.Processor.Controllers;
using Contensive.Models.Db;
using System.Globalization;
using System.Data;
//
namespace Contensive.Processor.Addons.Diagnostics {
    //
    public class ServerDiagnosticClass : Contensive.BaseClasses.AddonBaseClass {
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
                // -- tmp, check for 10% free on C-drive and D-drive
                if (Directory.Exists(@"c:\")) {
                    DriveInfo driveTest = new DriveInfo("c");
                    double freeSpace = Math.Round(100.0 * (Convert.ToDouble(driveTest.AvailableFreeSpace) / Convert.ToDouble(driveTest.TotalSize)), 2);
                    if (freeSpace < 10) { return "ERROR, Drive-C does not have 10% free"; }
                    result.AppendLine("ok, drive-c free space [" + freeSpace + "%], [" + (driveTest.AvailableFreeSpace / (1024 * 1024)).ToString("F2", CultureInfo.InvariantCulture) + " MB]");
                }
                if (Directory.Exists(@"d:\")) {
                    DriveInfo driveTest = new DriveInfo("d");
                    double freeSpace = Math.Round( 100.0 * (Convert.ToDouble(driveTest.AvailableFreeSpace) / Convert.ToDouble(driveTest.TotalSize)), 2);
                    if (freeSpace < 10) { return "ERROR, Drive-D does not have 10% free"; }
                    result.AppendLine("ok, drive-D free space [" + freeSpace + "%], [" + (driveTest.AvailableFreeSpace / (1024 * 1024)).ToString("F2", CultureInfo.InvariantCulture) + " MB]");
                }
                //
                // -- log files under 1MB
                if (!core.programDataFiles.pathExists("Logs/")) {
                    core.programDataFiles.createPath("Logs/");
                }
                foreach (var fileDetail in core.programDataFiles.getFileList("Logs/")) {
                    if (fileDetail.Size > 1000000) { return "ERROR, log file size error [" + fileDetail.Name + "], size [" + fileDetail.Size + "]"; }
                }
                result.AppendLine("ok, all log files under 1 MB");
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
                if (DbBaseModel.createList<AddonModel>(core.cpParent, "(ProcessNextRun<" + DbControllerX.encodeSQLDate( core.dateTimeNowMockable.AddHours(-1)) + ")").Count > 0) {
                    return "ERROR, there are process addons unexecuted for over 1 hour. TaskScheduler may not be enabled, or no server is running the Contensive Task Service.";
                }
                if (DbBaseModel.createList<TaskModel>(core.cpParent, "(dateCompleted is null)and(dateStarted<" + DbControllerX.encodeSQLDate(core.dateTimeNowMockable.AddHours(-1)) + ")").Count > 0) {
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
                // -- last -- if alarm folder is not empty, fail diagnostic. Last so others can add an alarm entry
                if(!core.programDataFiles.pathExists("Alarms/")) {
                    core.programDataFiles.createPath("Alarms/");
                }
                foreach (var alarmFile in core.programDataFiles.getFileList("Alarms/")) {
                    return "ERROR, Alarm folder is not empty, [" + core.programDataFiles.readFileText("Alarms/" + alarmFile.Name) + "].";
                }
                // -- verify the default username=root, password=contensive is not present
                var rootUserList = PersonModel.createList<PersonModel>(cp, "((username='root')and(password='contensive')and(active>0))");
                if ( rootUserList.Count>0 ) {
                    return "ERROR, delete or inactive default user root/contensive.";
                }
                //
                // -- meta data test- lookup field without lookup set
                string sql = "select c.id as contentid, c.name as contentName, f.* from ccfields f left join ccContent c on c.id = f.LookupContentID where f.Type = 7 and c.id is null and f.LookupContentID > 0 and f.Active > 0 and f.Authorable > 0";
                using ( DataTable dt = core.db.executeQuery(sql)) {
                    if ( !dt.Rows.Count.Equals(0)) {
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
                return "ok, all server diagnostics passed" + Environment.NewLine + result.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "ERROR, unexpected exception during diagnostics";
            }
        }
    }
}
