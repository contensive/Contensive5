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
using Contensive.Core;
using Contensive.Core.Models.DbModels;
using Contensive.Core.Controllers;
using static Contensive.Core.Controllers.genericController;
using static Contensive.Core.constants;
using Contensive.Core.Models.Complex;
using Contensive.BaseClasses;
//
namespace Contensive.Core.Addons.Email {
    public class processEmailClass  : Contensive.BaseClasses.AddonBaseClass{
        //
        //====================================================================================================
        /// <summary>
        /// addon interface
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            string result = "";
            try {
                //
                // -- ok to cast cpbase to cp because they build from the same solution
                coreController core = ((CPClass)cp).core;
                emailController.procesQueue(core);
                //
                // Send Submitted Group Email (submitted, not sent, no conditions)
                //
                ProcessEmail_GroupEmail(core);
                //
                // Send Conditional Email - Offset days after Joining
                //
                DateTime EmailServiceLastCheck = (core.siteProperties.getDate("EmailServiceLastCheck"));
                core.siteProperties.setProperty("EmailServiceLastCheck", encodeText(DateTime.Now));
                bool IsNewHour = (DateTime.Now - EmailServiceLastCheck).TotalHours > 1;
                bool IsNewDay = EmailServiceLastCheck.Date != DateTime.Now.Date;
                ProcessEmail_ConditionalEmail(core, IsNewHour, IsNewDay);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
        //
        //====================================================================================================
        //   Process Group Email
        //
        private void ProcessEmail_GroupEmail(coreController core) {
            try {
                //
                //Dim siteStyles As String
                DateTime ScheduleDate = default(DateTime);
                string EmailCopy = null;
                string SQL = null;
                int CSEmail = 0;
                int CSPeople = 0;
                string SQLDateNow = null;
                int emailID = 0;
                string Criteria = null;
                int CSDrop = 0;
                int EmailDropID = 0;
                int PeopleID = 0;
                int ConfirmationMemberID = 0;
                string EmailSubject = null;
                string EmailStatusList = null;
                int EmailMemberID = 0;
                string SQLTablePeople = null;
                string SQLTableMemberRules = null;
                string SQLTableGroups = null;
                string BounceAddress = null;
                string EmailTemplate = null;
                string PrimaryLink = null;
                bool EmailAddLinkEID = false;
                int EmailTemplateID = 0;
                string EmailFrom = null;
                //
                SQLDateNow = core.db.encodeSQLDate(DateTime.Now);
                PrimaryLink = "http://" + core.appConfig.domainList[0];
                //
                // Open the email records
                //
                Criteria = "(ccemail.active<>0)"
                    + " and (ccemail.Sent=0)"
                    + " and (ccemail.submitted<>0)"
                    + " and ((ccemail.scheduledate is null)or(ccemail.scheduledate<" + SQLDateNow + "))"
                    + " and ((ccemail.ConditionID is null)OR(ccemail.ConditionID=0))"
                    + "";
                CSEmail = core.db.csOpen("Email", Criteria);
                if (core.db.csOk(CSEmail)) {
                    //
                    SQLTablePeople = cdefModel.getContentTablename(core, "People");
                    SQLTableMemberRules =  cdefModel.getContentTablename(core, "Member Rules");
                    SQLTableGroups = cdefModel.getContentTablename(core, "Groups");
                    BounceAddress = core.siteProperties.getText("EmailBounceAddress", "");
                    //siteStyles = core.html.html_getStyleSheet2(0, 0)
                    //
                    while (core.db.csOk(CSEmail)) {
                        emailID = core.db.csGetInteger(CSEmail, "ID");
                        EmailMemberID = core.db.csGetInteger(CSEmail, "ModifiedBy");
                        EmailTemplateID = core.db.csGetInteger(CSEmail, "EmailTemplateID");
                        EmailTemplate = GetEmailTemplate(core, EmailTemplateID);
                        EmailAddLinkEID = core.db.csGetBoolean(CSEmail, "AddLinkEID");
                        //exclusiveStyles = core.asv.csv_cs_getText(CSEmail, "exclusiveStyles")
                        EmailFrom = core.db.csGetText(CSEmail, "FromAddress");
                        EmailSubject = core.db.csGetText(CSEmail, "Subject");
                        //emailStyles = emailController.getStyles(emailID)
                        //
                        // Mark this email sent and go to the next
                        //
                        core.db.csSet(CSEmail, "sent", true);
                        core.db.csSave(CSEmail);
                        //
                        // Create Drop Record
                        //
                        CSDrop = core.db.csInsertRecord("Email Drops", EmailMemberID);
                        if (core.db.csOk(CSDrop)) {
                            EmailDropID = core.db.csGetInteger(CSDrop, "ID");
                            ScheduleDate = core.db.csGetDate(CSEmail, "ScheduleDate");
                            if (ScheduleDate < DateTime.Parse("1/1/2000")) {
                                ScheduleDate = DateTime.Parse("1/1/2000");
                            }
                            core.db.csSet(CSDrop, "Name", "Drop " + EmailDropID + " - Scheduled for " + ScheduleDate.ToString("") + " " + ScheduleDate.ToString(""));
                            core.db.csSet(CSDrop, "EmailID", emailID);
                            //Call core.asv.csv_SetCSField(CSDrop, "CreatedBy", EmailMemberID)
                        }
                        core.db.csClose(ref CSDrop);
                        //
                        // Select all people in the groups for this email
                        SQL = "select Distinct " + SQLTablePeople + ".ID as MemberID," + SQLTablePeople + ".email"
                            + " From ((((ccemail"
                            + " left join ccEmailGroups on ccEmailGroups.EmailID=ccEmail.ID)"
                            + " left join " + SQLTableGroups + " on " + SQLTableGroups + ".ID = ccEmailGroups.GroupID)"
                            + " left join " + SQLTableMemberRules + " on " + SQLTableGroups + ".ID = " + SQLTableMemberRules + ".GroupID)"
                            + " left join " + SQLTablePeople + " on " + SQLTablePeople + ".ID = " + SQLTableMemberRules + ".MemberID)"
                            + " Where (ccEmail.ID=" + emailID + ")"
                            + " and (" + SQLTableGroups + ".active<>0)"
                            + " and (" + SQLTableGroups + ".AllowBulkEmail<>0)"
                            + " and (" + SQLTablePeople + ".active<>0)"
                            + " and (" + SQLTablePeople + ".AllowBulkEmail<>0)"
                            + " and (" + SQLTablePeople + ".email<>'')"
                            + " and ((" + SQLTableMemberRules + ".DateExpires is null)or(" + SQLTableMemberRules + ".DateExpires>" + SQLDateNow + "))"
                            + " order by " + SQLTablePeople + ".email," + SQLTablePeople + ".id";
                        CSPeople = core.db.csOpenSql(SQL,"Default");
                        //
                        // Send the email to all selected people
                        //
                        string LastEmail = null;
                        string Email = null;
                        string PeopleName = null;
                        EmailStatusList = "";
                        LastEmail = "empty";
                        while (core.db.csOk(CSPeople)) {
                            PeopleID = core.db.csGetInteger(CSPeople, "MemberID");
                            Email = core.db.csGetText(CSPeople, "Email");
                            if (Email == LastEmail) {
                                PeopleName = core.db.getRecordName("people", PeopleID);
                                if (string.IsNullOrEmpty(PeopleName)) {
                                    PeopleName = "user #" + PeopleID;
                                }
                                EmailStatusList = EmailStatusList + "Not Sent to " + PeopleName + ", duplicate email address (" + Email + ")" + BR;
                            } else {
                                EmailStatusList = EmailStatusList + SendEmailRecord(core, PeopleID, emailID, DateTime.MinValue, EmailDropID, BounceAddress, EmailFrom, EmailTemplate, EmailFrom, EmailSubject, core.db.csGet(CSEmail, "CopyFilename"), core.db.csGetBoolean(CSEmail, "AllowSpamFooter"), core.db.csGetBoolean(CSEmail, "AddLinkEID"), "") + BR;
                                //EmailStatusList = EmailStatusList & SendEmailRecord( PeopleID, EmailID, 0, EmailDropID, BounceAddress, EmailFrom, EmailTemplate, EmailFrom, core.csv_cs_get(CSEmail, "Subject"), core.csv_cs_get(CSEmail, "CopyFilename"), core.csv_cs_getBoolean(CSEmail, "AllowSpamFooter"), core.csv_cs_getBoolean(CSEmail, "AddLinkEID"), "") & BR
                            }
                            LastEmail = Email;
                            core.db.csGoNext(CSPeople);
                        }
                        core.db.csClose(ref CSPeople);
                        //
                        // Send the confirmation
                        //
                        EmailCopy = core.db.csGet(CSEmail, "copyfilename");
                        ConfirmationMemberID = core.db.csGetInteger(CSEmail, "testmemberid");
                        SendConfirmationEmail(core, ConfirmationMemberID, EmailDropID, EmailTemplate, EmailAddLinkEID, PrimaryLink, EmailSubject, EmailCopy, "", EmailFrom, EmailStatusList);
                        //            CSPeople = core.asv.csOpenRecord("people", ConfirmationMemberID)
                        //            If core.asv.csv_IsCSOK(CSPeople) Then
                        //                ClickFlagQuery = RequestNameEmailClickFlag & "=" & EmailDropID & "&" & RequestNameEmailMemberID & "=" & ConfirmationMemberID
                        //                EmailTemplate = core.csv_EncodeContent(EmailTemplate, ConfirmationMemberID, -1, False, EmailAddLinkEID, True, True, False, True, ClickFlagQuery, PrimaryLink)
                        //                EmailSubject = core.csv_EncodeContent(core.csv_cs_get(CSEmail, "Subject"), ConfirmationMemberID, , True, False, False, False, False, True, , "http://" & GetPrimaryDomainName())
                        //                EmailBody = core.csv_EncodeContent(core.csv_cs_get(CSEmail, "CopyFilename"), ConfirmationMemberID, , False, EmailAddLinkEID, True, True, False, True, , "http://" & GetPrimaryDomainName())
                        //                'EmailFrom = core.csv_cs_get(CSEmail, "FromAddress")
                        //                Confirmation = "<HTML><Head>" _
                        //                    & "<Title>Email Confirmation</Title>" _
                        //                    & "<Base href=""http://" & GetPrimaryDomainName() & core.csv_RootPath & """>" _
                        //                    & emailStyles _
                        //                    & "</Head><BODY>" _
                        //                    & "The follow email has been sent" & BR & BR _
                        //                    & "Subject: " & EmailSubject & BR _
                        //                    & "From: " & EmailFrom & BR _
                        //                    & "Body" & BR _
                        //                    & "----------------------------------------------------------------------" & BR _
                        //                    & core.csv_MergeTemplate(EmailTemplate, EmailBody, ConfirmationMemberID) & BR _
                        //                    & "----------------------------------------------------------------------" & BR _
                        //                    & "--- email list ---" & BR _
                        //                    & EmailStatusList _
                        //                    & "--- end email list ---" & BR _
                        //                    & "</BODY></HTML>"
                        //                Confirmation = ConvertLinksToAbsolute(Confirmation, PrimaryLink & "/")
                        //                Call core.csv_SendEmail2(core.asv.csv_cs_getText(CSPeople, "Email"), EmailFrom, "Email Confirmation from " & GetPrimaryDomainName(), Confirmation, "", "", , True, True)
                        //                End If
                        //            Call core.asv.csv_CloseCS(CSPeople)
                        //
                        core.db.csGoNext(CSEmail);
                    }
                }
                core.db.csClose(ref CSEmail);
                //
                return;
            } catch (Exception ex) {
                core.handleException(ex);
            }
            //ErrorTrap:
            throw (new ApplicationException("Unexpected exception")); //core.handleLegacyError3(core.appConfig.name, "trap error", "App.EXEName", "ProcessEmailClass", "ProcessEmail_GroupEmail", Err.Number, Err.Source, Err.Description, True, True, "")
                                                                      //todo  TASK: Calls to the VB 'Err' function are not converted by Instant C#:
            //Microsoft.VisualBasic.Information.Err().Clear();
        }
        //
        //====================================================================================================
        //
        private void ProcessEmail_ConditionalEmail(coreController core, bool IsNewHour, bool IsNewDay) {
            try {
                //
                //
                bool EmailAddLinkEID = false;
                string EmailSubject = null;
                string EmailCopy = null;
                string EmailStatus = null;
                string SQL = null;
                int CSEmailBig = 0;
                int CSEmail = 0;
                int emailID = 0;
                int EmailDropID = 0;
                int ConfirmationMemberID = 0;
                string SQLTablePeople = null;
                string SQLTableMemberRules = null;
                string SQLTableGroups = null;
                string BounceAddress = null;
                int EmailTemplateID = 0;
                string EmailTemplate = null;
                string FieldList = null;
                string FromAddress = null;
                int EmailMemberID = 0;
                DateTime EmailDateExpires = default(DateTime);
                DateTime rightNow = default(DateTime);
                DateTime rightNowDate = default(DateTime);
                string SQLDateNow = null;
                int dataSourceType = 0;
                string sqlDateTest = null;
                //
                dataSourceType = core.db.getDataSourceType("default");
                SQLTablePeople = cdefModel.getContentTablename(core, "People");
                SQLTableMemberRules = cdefModel.getContentTablename(core, "Member Rules");
                SQLTableGroups = cdefModel.getContentTablename(core, "Groups");
                BounceAddress = core.siteProperties.getText("EmailBounceAddress", "");
                // siteStyles = core.html.html_getStyleSheet2(0, 0)
                //
                rightNow = DateTime.Now;
                rightNowDate = rightNow.Date;
                SQLDateNow = core.db.encodeSQLDate(DateTime.Now);
                //
                // Send Conditional Email - Offset days after Joining
                //   sends email between the condition period date and date +1. if a conditional email is setup and there are already
                //   peope in the group, they do not get the email if they are past the one day threshhold.
                //   To keep them from only getting one, the log is used for the one day.
                //   Housekeep logs far > 1 day
                //
                if (IsNewDay) {
                    FieldList = "ccEmail.TestMemberID AS TestMemberID,ccEmail.ID as EmailID," + SQLTablePeople + ".ID AS MemberID, " + SQLTableMemberRules + ".DateExpires AS DateExpires,ccEmail.BlockSiteStyles,ccEmail.stylesFilename";
                    if (dataSourceType == DataSourceTypeODBCSQLServer) {
                        sqlDateTest = ""
                            + " AND (CAST(" + SQLTableMemberRules + ".DateAdded as datetime)+ccEmail.ConditionPeriod < " + SQLDateNow + ")"
                            + " AND (CAST(" + SQLTableMemberRules + ".DateAdded as datetime)+ccEmail.ConditionPeriod+1.0 > " + SQLDateNow + ")"
                            + "";
                    } else {
                        sqlDateTest = ""
                            + " AND (" + SQLTableMemberRules + ".DateAdded+ccEmail.ConditionPeriod < " + SQLDateNow + ")"
                            + " AND (" + SQLTableMemberRules + ".DateAdded+ccEmail.ConditionPeriod+1.0 > " + SQLDateNow + ")"
                            + "";
                    }
                    SQL = "SELECT Distinct " + FieldList + " FROM ((((ccEmail"
                        + " LEFT JOIN ccEmailGroups ON ccEmail.ID = ccEmailGroups.EmailID)"
                        + " LEFT JOIN " + SQLTableGroups + " ON ccEmailGroups.GroupID = " + SQLTableGroups + ".ID)"
                        + " LEFT JOIN " + SQLTableMemberRules + " ON " + SQLTableGroups + ".ID = " + SQLTableMemberRules + ".GroupID)"
                        + " LEFT JOIN " + SQLTablePeople + " ON " + SQLTableMemberRules + ".MemberID = " + SQLTablePeople + ".ID)"
                        + " Where (ccEmail.id Is Not Null)"
                        + sqlDateTest + " AND (ccEmail.ConditionExpireDate > " + SQLDateNow + " OR ccEmail.ConditionExpireDate IS NULL)"
                        + " AND (ccEmail.ScheduleDate < " + SQLDateNow + " OR ccEmail.ScheduleDate IS NULL)"
                        + " AND (ccEmail.Submitted <> 0)"
                        + " AND (ccEmail.ConditionID = 2)"
                        + " AND (ccEmail.ConditionPeriod IS NOT NULL)"
                        + " AND (" + SQLTableGroups + ".Active <> 0)"
                        + " AND (" + SQLTableGroups + ".AllowBulkEmail <> 0)"
                        + " AND (" + SQLTablePeople + ".ID IS NOT NULL)"
                        + " AND (" + SQLTablePeople + ".Active <> 0)"
                        + " AND (" + SQLTablePeople + ".AllowBulkEmail <> 0)"
                        + " AND (ccEmail.ID Not In (Select ccEmailLog.EmailID from ccEmailLog where ccEmailLog.MemberID=" + SQLTablePeople + ".ID))";
                    CSEmailBig = core.db.csOpenSql(SQL,"Default");
                    while (core.db.csOk(CSEmailBig)) {
                        emailID = core.db.csGetInteger(CSEmailBig, "EmailID");
                        EmailMemberID = core.db.csGetInteger(CSEmailBig, "MemberID");
                        EmailDateExpires = core.db.csGetDate(CSEmailBig, "DateExpires");
                        CSEmail = core.db.csOpenContentRecord("Conditional Email", emailID);
                        if (core.db.csOk(CSEmail)) {
                            EmailTemplateID = core.db.csGetInteger(CSEmail, "EmailTemplateID");
                            EmailTemplate = GetEmailTemplate(core, EmailTemplateID);
                            FromAddress = core.db.csGetText(CSEmail, "FromAddress");
                            ConfirmationMemberID = core.db.csGetInteger(CSEmail, "testmemberid");
                            EmailAddLinkEID = core.db.csGetBoolean(CSEmail, "AddLinkEID");
                            EmailSubject = core.db.csGet(CSEmail, "Subject");
                            EmailCopy = core.db.csGet(CSEmail, "CopyFilename");
                            //emailStyles = emailController.getStyles(emailID)
                            EmailStatus = SendEmailRecord(core, EmailMemberID, emailID, EmailDateExpires, 0, BounceAddress, FromAddress, EmailTemplate, FromAddress, EmailSubject, EmailCopy, core.db.csGetBoolean(CSEmail, "AllowSpamFooter"), EmailAddLinkEID, "");
                            //EmailStatus = SendEmailRecord( EmailMemberID, EmailID, EmailDateExpires, 0, BounceAddress, FromAddress, EmailTemplate, FromAddress, EmailSubject, EmailCopy, core.csv_cs_getBoolean(CSEmail, "AllowSpamFooter"), EmailAddLinkEID, EmailInlineStyles)
                            SendConfirmationEmail(core, ConfirmationMemberID, EmailDropID, EmailTemplate, EmailAddLinkEID, "", EmailSubject, EmailCopy, "", FromAddress, EmailStatus + "<BR>");
                        }
                        core.db.csClose(ref CSEmail);
                        core.db.csGoNext(CSEmailBig);
                    }
                    core.db.csClose(ref CSEmailBig);
                }
                //
                // Send Conditional Email - Offset days Before Expiration
                //
                if (IsNewDay) {
                    FieldList = "ccEmail.TestMemberID AS TestMemberID,ccEmail.ID AS EmailID, " + SQLTablePeople + ".ID AS MemberID, " + SQLTableMemberRules + ".DateExpires AS DateExpires,ccEmail.BlockSiteStyles,ccEmail.stylesFilename";
                    if (dataSourceType == DataSourceTypeODBCSQLServer) {
                        sqlDateTest = ""
                            + " AND (CAST(" + SQLTableMemberRules + ".DateExpires as datetime)-ccEmail.ConditionPeriod > " + SQLDateNow + ")"
                            + " AND (CAST(" + SQLTableMemberRules + ".DateExpires as datetime)-ccEmail.ConditionPeriod-1.0 < " + SQLDateNow + ")"
                            + "";
                    } else {
                        sqlDateTest = ""
                            + " AND (" + SQLTableMemberRules + ".DateExpires-ccEmail.ConditionPeriod > " + SQLDateNow + ")"
                            + " AND (" + SQLTableMemberRules + ".DateExpires-ccEmail.ConditionPeriod-1.0 < " + SQLDateNow + ")"
                            + "";
                    }
                    SQL = "SELECT DISTINCT " + FieldList + " FROM ((((ccEmail"
                        + " LEFT JOIN ccEmailGroups ON ccEmail.ID = ccEmailGroups.EmailID)"
                        + " LEFT JOIN " + SQLTableGroups + " ON ccEmailGroups.GroupID = " + SQLTableGroups + ".ID)"
                        + " LEFT JOIN " + SQLTableMemberRules + " ON " + SQLTableGroups + ".ID = " + SQLTableMemberRules + ".GroupID)"
                        + " LEFT JOIN " + SQLTablePeople + " ON " + SQLTableMemberRules + ".MemberID = " + SQLTablePeople + ".ID)"
                        + " Where (ccEmail.id Is Not Null)"
                        + sqlDateTest + " AND (ccEmail.ConditionExpireDate > " + SQLDateNow + " OR ccEmail.ConditionExpireDate IS NULL)"
                        + " AND (ccEmail.ScheduleDate < " + SQLDateNow + " OR ccEmail.ScheduleDate IS NULL)"
                        + " AND (ccEmail.Submitted <> 0)"
                        + " AND (ccEmail.ConditionID = 1)"
                        + " AND (ccEmail.ConditionPeriod IS NOT NULL)"
                        + " AND (" + SQLTableGroups + ".Active <> 0)"
                        + " AND (" + SQLTableGroups + ".AllowBulkEmail <> 0)"
                        + " AND (" + SQLTablePeople + ".ID IS NOT NULL)"
                        + " AND (" + SQLTablePeople + ".Active <> 0)"
                        + " AND (" + SQLTablePeople + ".AllowBulkEmail <> 0)"
                        + " AND (ccEmail.ID Not In (Select ccEmailLog.EmailID from ccEmailLog where ccEmailLog.MemberID=" + SQLTablePeople + ".ID))";
                    CSEmailBig = core.db.csOpenSql(SQL,"Default");
                    while (core.db.csOk(CSEmailBig)) {
                        emailID = core.db.csGetInteger(CSEmailBig, "EmailID");
                        EmailMemberID = core.db.csGetInteger(CSEmailBig, "MemberID");
                        EmailDateExpires = core.db.csGetDate(CSEmailBig, "DateExpires");
                        CSEmail = core.db.csOpenContentRecord("Conditional Email", emailID);
                        if (core.db.csOk(CSEmail)) {
                            EmailTemplateID = core.db.csGetInteger(CSEmail, "EmailTemplateID");
                            EmailTemplate = GetEmailTemplate(core, EmailTemplateID);
                            FromAddress = core.db.csGetText(CSEmail, "FromAddress");
                            ConfirmationMemberID = core.db.csGetInteger(CSEmail, "testmemberid");
                            EmailAddLinkEID = core.db.csGetBoolean(CSEmail, "AddLinkEID");
                            EmailSubject = core.db.csGet(CSEmail, "Subject");
                            EmailCopy = core.db.csGet(CSEmail, "CopyFilename");
                            //emailStyles = emailController.getStyles(emailID)
                            EmailStatus = SendEmailRecord(core, EmailMemberID, emailID, EmailDateExpires, 0, BounceAddress, FromAddress, EmailTemplate, FromAddress, core.db.csGet(CSEmail, "Subject"), core.db.csGet(CSEmail, "CopyFilename"), core.db.csGetBoolean(CSEmail, "AllowSpamFooter"), core.db.csGetBoolean(CSEmail, "AddLinkEID"), "");
                            //EmailStatus = SendEmailRecord( EmailMemberID, EmailID, EmailDateExpires, 0, BounceAddress, FromAddress, EmailTemplate, FromAddress, core.csv_cs_get(CSEmail, "Subject"), core.csv_cs_get(CSEmail, "CopyFilename"), core.csv_cs_getBoolean(CSEmail, "AllowSpamFooter"), core.csv_cs_getBoolean(CSEmail, "AddLinkEID"), EmailInlineStyles)
                            SendConfirmationEmail(core, ConfirmationMemberID, EmailDropID, EmailTemplate, EmailAddLinkEID, "", EmailSubject, EmailCopy, "", FromAddress, EmailStatus + "<BR>");
                        }
                        core.db.csClose(ref CSEmail);
                        core.db.csGoNext(CSEmailBig);
                    }
                    core.db.csClose(ref CSEmailBig);
                }
                //
                // Send Conditional Email - Birthday
                //
                if (IsNewDay) {
                    FieldList = "ccEmail.TestMemberID AS TestMemberID,ccEmail.ID AS EmailID, " + SQLTablePeople + ".ID AS MemberID, " + SQLTableMemberRules + ".DateExpires AS DateExpires,ccEmail.BlockSiteStyles,ccEmail.stylesFilename";
                    SQL = "SELECT DISTINCT " + FieldList + " FROM ((((ccEmail"
                        + " LEFT JOIN ccEmailGroups ON ccEmail.ID = ccEmailGroups.EmailID)"
                        + " LEFT JOIN " + SQLTableGroups + " ON ccEmailGroups.GroupID = " + SQLTableGroups + ".ID)"
                        + " LEFT JOIN " + SQLTableMemberRules + " ON " + SQLTableGroups + ".ID = " + SQLTableMemberRules + ".GroupID)"
                        + " LEFT JOIN " + SQLTablePeople + " ON " + SQLTableMemberRules + ".MemberID = " + SQLTablePeople + ".ID)"
                        + " Where (ccEmail.id Is Not Null)"
                        + " AND (ccEmail.ConditionExpireDate > " + SQLDateNow + " OR ccEmail.ConditionExpireDate IS NULL)"
                        + " AND (ccEmail.ScheduleDate < " + SQLDateNow + " OR ccEmail.ScheduleDate IS NULL)"
                        + " AND (ccEmail.Submitted <> 0)"
                        + " AND (ccEmail.ConditionID = 3)"
                        + " AND (" + SQLTableGroups + ".Active <> 0)"
                        + " AND (" + SQLTableGroups + ".AllowBulkEmail <> 0)"
                        + " AND ((" + SQLTableMemberRules + ".DateExpires is null)or(" + SQLTableMemberRules + ".DateExpires > " + SQLDateNow + "))"
                        + " AND (" + SQLTablePeople + ".ID IS NOT NULL)"
                        + " AND (" + SQLTablePeople + ".Active <> 0)"
                        + " AND (" + SQLTablePeople + ".AllowBulkEmail <> 0)"
                        + " AND (" + SQLTablePeople + ".BirthdayMonth=" + DateTime.Now.Month + ")"
                        + " AND (" + SQLTablePeople + ".BirthdayDay=" + DateTime.Now.Day + ")"
                        + " AND (ccEmail.ID Not In (Select ccEmailLog.EmailID from ccEmailLog where ccEmailLog.MemberID=" + SQLTablePeople + ".ID and ccEmailLog.DateAdded>=" + core.db.encodeSQLDate(DateTime.Now.Date) + "))";
                    CSEmailBig = core.db.csOpenSql(SQL,"Default");
                    while (core.db.csOk(CSEmailBig)) {
                        emailID = core.db.csGetInteger(CSEmailBig, "EmailID");
                        EmailMemberID = core.db.csGetInteger(CSEmailBig, "MemberID");
                        EmailDateExpires = core.db.csGetDate(CSEmailBig, "DateExpires");
                        CSEmail = core.db.csOpenContentRecord("Conditional Email", emailID);
                        if (core.db.csOk(CSEmail)) {
                            EmailTemplateID = core.db.csGetInteger(CSEmail, "EmailTemplateID");
                            EmailTemplate = GetEmailTemplate(core, EmailTemplateID);
                            FromAddress = core.db.csGetText(CSEmail, "FromAddress");
                            ConfirmationMemberID = core.db.csGetInteger(CSEmail, "testmemberid");
                            EmailAddLinkEID = core.db.csGetBoolean(CSEmail, "AddLinkEID");
                            EmailSubject = core.db.csGet(CSEmail, "Subject");
                            EmailCopy = core.db.csGet(CSEmail, "CopyFilename");
                            //emailStyles = emailController.getStyles(emailID)
                            EmailStatus = SendEmailRecord(core, EmailMemberID, emailID, EmailDateExpires, 0, BounceAddress, FromAddress, EmailTemplate, FromAddress, core.db.csGet(CSEmail, "Subject"), core.db.csGet(CSEmail, "CopyFilename"), core.db.csGetBoolean(CSEmail, "AllowSpamFooter"), core.db.csGetBoolean(CSEmail, "AddLinkEID"), "");
                            //EmailStatus = SendEmailRecord( EmailMemberID, EmailID, EmailDateExpires, 0, BounceAddress, FromAddress, EmailTemplate, FromAddress, core.csv_cs_get(CSEmail, "Subject"), core.csv_cs_get(CSEmail, "CopyFilename"), core.csv_cs_getBoolean(CSEmail, "AllowSpamFooter"), core.csv_cs_getBoolean(CSEmail, "AddLinkEID"), EmailInlineStyles)
                            SendConfirmationEmail(core, ConfirmationMemberID, EmailDropID, EmailTemplate, EmailAddLinkEID, "", EmailSubject, EmailCopy, "", FromAddress, EmailStatus + "<BR>");
                        }
                        core.db.csClose(ref CSEmail);
                        core.db.csGoNext(CSEmailBig);
                    }
                    core.db.csClose(ref CSEmailBig);
                }
                //
                return;
            } catch (Exception ex) {
                core.handleException(ex);
            }
            //ErrorTrap:
            throw (new ApplicationException("Unexpected exception")); //core.handleLegacyError3(core.appConfig.name, "trap error", "App.EXEName", "ProcessEmailClass", "ProcessEmail_ConditionalEmail", Err.Number, Err.Source, Err.Description, True, True, "")
                                                                      //todo  TASK: Calls to the VB 'Err' function are not converted by Instant C#:
            //Microsoft.VisualBasic.Information.Err().Clear();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send email to a memberid
        /// </summary>
        /// <param name="MemberID"></param>
        /// <param name="emailID"></param>
        /// <param name="DateBlockExpires"></param>
        /// <param name="EmailDropID"></param>
        /// <param name="BounceAddress"></param>
        /// <param name="ReplyToAddress"></param>
        /// <param name="EmailTemplate"></param>
        /// <param name="FromAddress"></param>
        /// <param name="EmailSubject"></param>
        /// <param name="EmailBody"></param>
        /// <param name="AllowSpamFooter"></param>
        /// <param name="EmailAllowLinkEID"></param>
        /// <param name="emailStyles"></param>
        /// <returns>OK if successful, else returns user error.</returns>
        private string SendEmailRecord(coreController core, int MemberID, int emailID, DateTime DateBlockExpires, int EmailDropID, string BounceAddress, string ReplyToAddress, string EmailTemplate, string FromAddress, string EmailSubject, string EmailBody, bool AllowSpamFooter, bool EmailAllowLinkEID, string emailStyles) {
            string returnStatus = "";
            int CSPeople = 0;
            int CSLog = 0;
            try {
                //
                //Dim CS as integer
                //Dim EmailFrom As String
                //Dim HTMLHead As String
                string ServerPageDefault = null;
                string EmailToName = null;
                string ClickFlagQuery = null;
                string EmailStatus = null;
                //Dim FieldList As String
                //Dim InlineStyles As String
                string emailWorkingStyles = null;
                string RootURL = null;
                string protocolHostLink = null;
                string ToAddress = null;
                //Dim ToAddressName As String
                string EmailBodyEncoded = null;
                string EmailSubjectEncoded = null;
                //
                string errorMessage = "";
                string EmailTemplateEncoded = "";
                string OpenTriggerCode = "";
                //
                EmailBodyEncoded = EmailBody;
                EmailSubjectEncoded = EmailSubject;
                //buildversion = core.app.dataBuildVersion
                CSLog = core.db.csInsertRecord("Email Log", 0);
                if (core.db.csOk(CSLog)) {
                    core.db.csSet(CSLog, "Name", "Sent " + encodeText(DateTime.Now));
                    core.db.csSet(CSLog, "EmailDropID", EmailDropID);
                    core.db.csSet(CSLog, "EmailID", emailID);
                    core.db.csSet(CSLog, "MemberID", MemberID);
                    core.db.csSet(CSLog, "LogType", EmailLogTypeDrop);
                    core.db.csSet(CSLog, "DateBlockExpires", DateBlockExpires);
                    core.db.csSet(CSLog, "SendStatus", "Send attempted but not completed");
                    if (true) {
                        core.db.csSet(CSLog, "fromaddress", FromAddress);
                        core.db.csSet(CSLog, "Subject", EmailSubject);
                    }
                    core.db.csSave(CSLog);
                    //
                    // Get the Template
                    //
                    protocolHostLink = "http://" + GetPrimaryDomainName(core);
                    //
                    // Get the Member
                    //
                    CSPeople = core.db.csOpenContentRecord("People", MemberID, 0, false, false, "Email,Name");
                    if (core.db.csOk(CSPeople)) {
                        ToAddress = core.db.csGet(CSPeople, "Email");
                        EmailToName = core.db.csGet(CSPeople, "Name");
                        ServerPageDefault = core.siteProperties.getText(siteproperty_serverPageDefault_name, siteproperty_serverPageDefault_defaultValue);
                        RootURL = protocolHostLink + requestAppRootPath;
                        if (EmailDropID != 0) {
                            switch (core.siteProperties.getInteger("GroupEmailOpenTriggerMethod", 0)) {
                                case 1:
                                    OpenTriggerCode = "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + RootURL + ServerPageDefault + "?" + RequestNameEmailOpenCssFlag + "=" + EmailDropID + "&" + rnEmailMemberID + "=#member_id#\">";
                                    break;
                                default:
                                    OpenTriggerCode = "<img src=\"" + RootURL + ServerPageDefault + "?" + rnEmailOpenFlag + "=" + EmailDropID + "&" + rnEmailMemberID + "=#member_id#\">";
                                    break;
                            }
                        }
                        //
                        emailWorkingStyles = emailStyles;
                        emailWorkingStyles = genericController.vbReplace(emailWorkingStyles, StyleSheetStart, StyleSheetStart + "<!-- ", 1, 99, 1);
                        emailWorkingStyles = genericController.vbReplace(emailWorkingStyles, StyleSheetEnd, " // -->" + StyleSheetEnd, 1, 99, 1);
                        //
                        // Create the clickflag to be added to all anchors
                        //
                        ClickFlagQuery = rnEmailClickFlag + "=" + EmailDropID + "&" + rnEmailMemberID + "=" + MemberID;
                        //
                        // Encode body and subject
                        //
                        //EmailBodyEncoded = contentCmdController.executeContentCommands(core, EmailBodyEncoded, CPUtilsClass.addonContext.ContextEmail, MemberID, true, ref errorMessage);
                        EmailBodyEncoded = activeContentController.renderHtmlForEmail(core, EmailBodyEncoded, MemberID, ClickFlagQuery);
                        //EmailBodyEncoded = core.html.convertActiveContent_internal(EmailBodyEncoded, MemberID, "", 0, 0, False, EmailAllowLinkEID, True, True, False, True, ClickFlagQuery, PrimaryLink, True, 0, "", CPUtilsClass.addonContext.ContextEmail, True, Nothing, False)
                        //EmailBodyEncoded = core.csv_EncodeContent8(Nothing, EmailBodyEncoded, MemberID, "", 0, 0, False, EmailAllowLinkEID, True, True, False, True, ClickFlagQuery, PrimaryLink, True, "", 0, "", True, CPUtilsClass.addonContext.contextEmail)
                        //
                        //EmailSubjectEncoded = contentCmdController.executeContentCommands(core, EmailSubjectEncoded, CPUtilsClass.addonContext.ContextEmail, MemberID, true, ref errorMessage);
                        EmailSubjectEncoded = activeContentController.renderHtmlForEmail(core, EmailSubjectEncoded, MemberID, ClickFlagQuery);
                        //EmailSubjectEncoded = core.html.convertActiveContent_internal(EmailSubjectEncoded, MemberID, "", 0, 0, True, False, False, False, False, True, "", PrimaryLink, True, 0, "", CPUtilsClass.addonContext.ContextEmail, True, Nothing, False)
                        //EmailSubjectEncoded = core.csv_EncodeContent8(Nothing, EmailSubjectEncoded, MemberID, "", 0, 0, True, False, False, False, False, True, "", PrimaryLink, True, "", 0, "", True, CPUtilsClass.addonContext.contextEmail)
                        //
                        // Encode/Merge Template
                        //
                        if (string.IsNullOrEmpty(EmailTemplate)) {
                            //
                            // create 20px padding template
                            //
                            EmailBodyEncoded = "<div style=\"padding:10px;\">" + EmailBodyEncoded + "</div>";
                        } else {
                            //
                            // use provided template
                            //
                            //EmailTemplateEncoded = contentCmdController.executeContentCommands(core, EmailTemplateEncoded, CPUtilsClass.addonContext.ContextEmail, MemberID, true, ref errorMessage);
                            EmailTemplateEncoded = activeContentController.renderHtmlForEmail(core, EmailTemplate, MemberID, ClickFlagQuery);
                            //EmailTemplateEncoded = core.html.convertActiveContent_internal(EmailTemplate, MemberID, "", 0, 0, False, EmailAllowLinkEID, True, True, False, True, ClickFlagQuery, protocolHostLink, True, 0, "", CPUtilsClass.addonContext.ContextEmail, True, Nothing, False)
                            //EmailTemplateEncoded = core.csv_EncodeContent8(Nothing, EmailTemplate, MemberID, "", 0, 0, False, EmailAllowLinkEID, True, True, False, True, ClickFlagQuery, PrimaryLink, True, "", 0, "", True, CPUtilsClass.addonContext.contextEmail)
                            //EmailTemplateEncoded = core.csv_encodecontent8(Nothing, EmailTemplate, MemberID, "", 0, 0, False, EmailAllowLinkEID, True, True, False, True, ClickFlagQuery, PrimaryLink, True, "", 0, ContentPlaceHolder, True, CPUtilsClass.addonContext.contextemail)
                            if (genericController.vbInstr(1, EmailTemplateEncoded, fpoContentBox) != 0) {
                                EmailBodyEncoded = genericController.vbReplace(EmailTemplateEncoded, fpoContentBox, EmailBodyEncoded);
                            } else {
                                EmailBodyEncoded = EmailTemplateEncoded + "<div style=\"padding:10px;\">" + EmailBodyEncoded + "</div>";
                            }
                            //                If genericController.vbInstr(1, EmailTemplateEncoded, ContentPlaceHolder) <> 0 Then
                            //                    EmailBodyEncoded = genericController.vbReplace(EmailTemplateEncoded, ContentPlaceHolder, EmailBodyEncoded)
                            //                Else
                            //                    EmailBodyEncoded = EmailTemplateEncoded & "<div style=""padding:10px;"">" & EmailBodyEncoded & "</div>"
                            //                End If
                        }
                        //
                        // Spam Footer under template
                        // remove the marker for any other place in the email then add it as needed
                        //
                        EmailBodyEncoded = genericController.vbReplace(EmailBodyEncoded, rnEmailBlockRecipientEmail, "", 1, 99, 1);
                        if (AllowSpamFooter) {
                            //
                            // non-authorable, default true - leave it as an option in case there is an important exception
                            //
                            EmailBodyEncoded = EmailBodyEncoded + "<div style=\"padding:10px;\">" + GetLinkedText("<a href=\"" + RootURL + ServerPageDefault + "?" + rnEmailBlockRecipientEmail + "=#member_email#&" + rnEmailBlockRequestDropID + "=" + EmailDropID + "\">", core.siteProperties.getText("EmailSpamFooter", DefaultSpamFooter)) + "</div>";
                        }
                        //
                        // open trigger under footer (so it does not shake as the image comes in)
                        //
                        EmailBodyEncoded = EmailBodyEncoded + OpenTriggerCode;
                        EmailBodyEncoded = genericController.vbReplace(EmailBodyEncoded, "#member_id#", MemberID);
                        EmailBodyEncoded = genericController.vbReplace(EmailBodyEncoded, "#member_email#", ToAddress);
                        //
                        // Now convert URLS to absolute
                        //
                        EmailBodyEncoded = ConvertLinksToAbsolute(EmailBodyEncoded, RootURL);
                        //
                        EmailBodyEncoded = ""
                            + "<HTML>"
                            + "<Head>"
                            + "<Title>" + EmailSubjectEncoded + "</Title>"
                            + "<Base href=\"" + RootURL + "\">"
                            + "</Head>"
                            + "<BODY class=ccBodyEmail>"
                            + "<Base href=\"" + RootURL + "\">"
                            + emailWorkingStyles + EmailBodyEncoded + "</BODY>"
                            + "</HTML>";
                        //
                        // Send
                        //
                        emailController.sendAdHoc(core, ToAddress, FromAddress, EmailSubjectEncoded, EmailBodyEncoded, BounceAddress, ReplyToAddress, "", true, true, 0, ref EmailStatus);
                        if (string.IsNullOrEmpty(EmailStatus)) {
                            EmailStatus = "ok";
                        }
                        returnStatus = returnStatus + "Send to " + EmailToName + " at " + ToAddress + ", Status = " + EmailStatus;
                        //
                        // ----- Log the send
                        //
                        core.db.csSet(CSLog, "SendStatus", EmailStatus);
                        if (true) {
                            core.db.csSet(CSLog, "toaddress", ToAddress);
                        }
                        core.db.csSave(CSLog);
                    }
                    //Call core.app.closeCS(CSPeople)
                }
                //Call core.app.closeCS(CSLog)
            } catch (Exception) {
                throw (new ApplicationException("Unexpected exception")); 
            } finally {
                core.db.csClose(ref CSPeople);
                core.db.csClose(ref CSLog);
            }

            return returnStatus;
        }
        //
        //====================================================================================================
        //
        private string GetPrimaryDomainName(coreController core) {
            return core.appConfig.domainList[0];
        }
        //
        //====================================================================================================
        //
        private string GetEmailTemplate(coreController core, int EmailTemplateID) {
            string tempGetEmailTemplate = "";
            try {
                //
                // Get the Template
                //
                if (EmailTemplateID != 0) {
                    int CS = core.db.csOpenContentRecord("Email Templates", EmailTemplateID, 0, false, false, "BodyHTML");
                    if (core.db.csOk(CS)) {
                        tempGetEmailTemplate = core.db.csGet(CS, "BodyHTML");
                    }
                    core.db.csClose(ref CS);
                }
            } catch (Exception ex) {
                core.handleException(ex);
                throw (new ApplicationException("Unexpected exception")); //core.handleLegacyError3(core.appConfig.name, "trap error", "App.EXEName", "ProcessEmailClass", "GetEmailTemplate", Err.Number, Err.Source, Err.Description, True, True, "")
            }
            return tempGetEmailTemplate;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send confirmation email 
        /// </summary>
        /// <param name="ConfirmationMemberID"></param>
        /// <param name="EmailDropID"></param>
        /// <param name="EmailTemplate"></param>
        /// <param name="EmailAllowLinkEID"></param>
        /// <param name="PrimaryLink"></param>
        /// <param name="EmailSubject"></param>
        /// <param name="emailBody"></param>
        /// <param name="emailStyles"></param>
        /// <param name="EmailFrom"></param>
        /// <param name="EmailStatusList"></param>
        private void SendConfirmationEmail(coreController core, int ConfirmationMemberID, int EmailDropID, string EmailTemplate, bool EmailAllowLinkEID, string PrimaryLink, string EmailSubject, string emailBody, string emailStyles, string EmailFrom, string EmailStatusList) {
            try {
                personModel person = personModel.create(core, ConfirmationMemberID);
                if ( person != null ) {
                    string ConfirmBody = ""
                        + "The follow email has been sent." 
                        + BR 
                        + BR + "If this email includes personalization, each email sent was personalized to its recipient. This confirmation has been personalized to you." 
                        + BR 
                        + BR + "Subject: " + EmailSubject 
                        + BR + "From: " 
                        + EmailFrom 
                        + BR + "Body" 
                        + BR + "----------------------------------------------------------------------" 
                        + BR + emailBody
                        + BR + "----------------------------------------------------------------------" 
                        + BR + "--- recipient list ---" 
                        + BR + EmailStatusList 
                        + "--- end of list ---" 
                        + BR;
                    string queryStringForLinkAppend = rnEmailClickFlag + "=" + EmailDropID + "&" + rnEmailMemberID + "=" + person.id;
                    string sendStatus = "";
                    emailController.sendPerson(core, person, EmailFrom, "Email confirmation from " + GetPrimaryDomainName(core), ConfirmBody,true, true, EmailDropID, EmailTemplate, EmailAllowLinkEID, ref sendStatus, queryStringForLinkAppend);
                }
            } catch (Exception ex) {
                core.handleException(ex);
                throw;
            }
        }
    }
}