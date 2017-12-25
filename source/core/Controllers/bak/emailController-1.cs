﻿

using System.Text.RegularExpressions;
// 
using Contensive.BaseClasses;
using Controllers;

using Models;
using Models.Context;
using Models.Entity;
// 

namespace Controllers {
    
    // 
    // ====================================================================================================
    // '' <summary>
    // '' static class controller
    // '' </summary>
    public class emailController : IDisposable {
        
        // 
        //  ----- constants
        // 
        // Private Const invalidationDaysDefault As Double = 365
        // 
        //  ----- objects constructed that must be disposed
        // 
        // Private cacheClient As Enyim.Caching.MemcachedClient
        // 
        //  ----- private instance storage
        // 
        private coreClass cpcore;
        
        // 
        //  Email Block List - these are people who have asked to not have email sent to them from this site
        //    Loaded ondemand by csv_GetEmailBlockList
        // 
        private string email_BlockList_Local {
        }
    }
}
EndSub// 
// ====================================================================================================
Endclass  {
}

    
    // 
    // ====================================================================================================
    // '' <summary>
    // '' Send Email
    // '' </summary>
    // '' <param name="ToAddress"></param>
    // '' <param name="FromAddress"></param>
    // '' <param name="SubjectMessage"></param>
    // '' <param name="BodyMessage"></param>
    // '' <param name="BounceAddress"></param>
    // '' <param name="ReplyToAddress"></param>
    // '' <param name="ResultLogFilename"></param>
    // '' <param name="isImmediate"></param>
    // '' <param name="isHTML"></param>
    // '' <param name="emailIdOrZeroForLog"></param>
    // '' <returns>OK if send is successful, otherwise returns the principle issue as a user error.</returns>
    public string send(string ToAddress, string FromAddress, string SubjectMessage, string BodyMessage, string BounceAddress, string ReplyToAddress, string ResultLogFilename, bool isImmediate, bool isHTML, int emailIdOrZeroForLog) {
        string returnStatus = "";
        try {
            // 
            string htmlBody;
            string rootUrl;
            smtpController EmailHandler = new smtpController(cpcore);
            string iResultLogPathPage;
            string WarningMsg = "";
            int CSLog;
            // 
            if ((ToAddress == "")) {
                //  block
            }
            else if ((((ToAddress.IndexOf("@", 0) + 1) 
                        == 0) 
                        || ((ToAddress.IndexOf(".", 0) + 1) 
                        == 0))) {
                //  block
            }
            else if ((FromAddress == "")) {
                //  block
            }
            else if ((((FromAddress.IndexOf("@", 0) + 1) 
                        == 0) 
                        || ((FromAddress.IndexOf(".", 0) + 1) 
                        == 0))) {
                //  block
            }
            else if ((0 != genericController.vbInstr(1, getBlockList, ("\r\n" 
                            + (ToAddress + "\r\n")), vbTextCompare))) {
                // 
                //  They are in the block list
                // 
                returnStatus = "Recipient has blocked this email";
            }
            else {
                // 
                iResultLogPathPage = ResultLogFilename;
                // 
                //  Test for from-address / to-address matches
                // 
                if ((genericController.vbLCase(FromAddress) == genericController.vbLCase(ToAddress))) {
                    FromAddress = cpcore.siteProperties.getText("EmailFromAddress", "");
                    if ((FromAddress == "")) {
                        // 
                        // 
                        // 
                        FromAddress = ToAddress;
                        WarningMsg = "The from-address matches the to-address. This email was sent, but may be blocked by spam filtering.";
                    }
                    else if ((genericController.vbLCase(FromAddress) == genericController.vbLCase(ToAddress))) {
                        // 
                        // 
                        // 
                        WarningMsg = "The from-address matches the to-address. This email was sent, but may be blocked by spam filtering.";
                    }
                    else {
                        // 
                        // 
                        // 
                        WarningMsg = ("The from-address matches the to-address. The from-address was changed to " 
                                    + (FromAddress + " to prevent it from being blocked by spam filtering."));
                    }
                    
                }
                
                // 
                if (isHTML) {
                    // 
                    //  Fix links for HTML send
                    // 
                    rootUrl = ("http://" 
                                + (cpcore.serverConfig.appConfig.domainList(0) + "/"));
                    BodyMessage = genericController.ConvertLinksToAbsolute(BodyMessage, rootUrl);
                    // 
                    //  compose body
                    // 
                    htmlBody = ("" + ("<html>" + ("<head>" + ("<Title>" 
                                + (SubjectMessage + ("</Title>" + ("<Base href=\"" 
                                + (rootUrl + ("\" >" + ("</head>" + ("<body class=\"ccBodyEmail\">" + ("<Base href=\"" 
                                + (rootUrl + ("\" >" 
                                + (BodyMessage + ("</body>" + "</html>"))))))))))))))));
                    returnStatus = EmailHandler.sendEmail5(ToAddress, FromAddress, SubjectMessage, BodyMessage, BounceAddress, ReplyToAddress, iResultLogPathPage, cpcore.siteProperties.getText("SMTPServer", "SMTP.YourServer.Com"), isImmediate, isHTML, "");
                }
                else {
                    returnStatus = EmailHandler.sendEmail5(ToAddress, FromAddress, SubjectMessage, BodyMessage, BounceAddress, ReplyToAddress, iResultLogPathPage, cpcore.siteProperties.getText("SMTPServer", "SMTP.YourServer.Com"), isImmediate, isHTML, "");
                }
                
                if ((returnStatus == "")) {
                    returnStatus = WarningMsg;
                }
                
                // 
                //  ----- Log the send
                // 
                if (true) {
                    CSLog = cpcore.db.csInsertRecord("Email Log", 0);
                    if (cpcore.db.csOk(CSLog)) {
                        cpcore.db.csSet(CSLog, "Name", ("System Email Send " + Now().ToString()));
                        cpcore.db.csSet(CSLog, "LogType", EmailLogTypeImmediateSend);
                        cpcore.db.csSet(CSLog, "SendStatus", returnStatus);
                        cpcore.db.csSet(CSLog, "toaddress", ToAddress);
                        cpcore.db.csSet(CSLog, "fromaddress", FromAddress);
                        cpcore.db.csSet(CSLog, "Subject", SubjectMessage);
                        if ((emailIdOrZeroForLog != 0)) {
                            cpcore.db.csSet(CSLog, "emailid", emailIdOrZeroForLog);
                        }
                        
                    }
                    
                    cpcore.db.csClose(CSLog);
                }
                
            }
            
        }
        catch (Exception ex) {
            cpCore.handleException(ex);
            throw;
        }
        
        return returnStatus;
    }
    
    //         '
    //         '
    //         '
    //         Public Function getStyles(ByVal EmailID As Integer) As String
    //             On Error GoTo ErrorTrap 'Const Tn = "getEmailStyles": 'Dim th as integer: th = profileLogMethodEnter(Tn)
    //             '
    //             getStyles = cpcore.html.html_getStyleSheet2(csv_contentTypeEnum.contentTypeEmail, 0, genericController.EncodeInteger(EmailID))
    //             If getStyles <> "" Then
    //                 getStyles = "" _
    //                     & vbCrLf & StyleSheetStart _
    //                     & vbCrLf & getStyles _
    //                     & vbCrLf & StyleSheetEnd
    //             End If
    //             '
    //             '
    //             Exit Function
    // ErrorTrap:
    //             cpCore.handleException(New Exception("Unexpected exception"))
    //         End Function
    //   
    // ========================================================================
    // '' <summary>
    // '' Send email to a memberId, returns ok if send is successful, otherwise returns the principle issue as a user error.
    // '' </summary>
    // '' <param name="personId"></param>
    // '' <param name="FromAddress"></param>
    // '' <param name="subject"></param>
    // '' <param name="Body"></param>
    // '' <param name="Immediate"></param>
    // '' <param name="HTML"></param>
    // '' <param name="emailIdOrZeroForLog"></param>
    // '' <param name="template"></param>
    // '' <param name="EmailAllowLinkEID"></param>
    // '' <returns> returns ok if send is successful, otherwise returns the principle issue as a user error</returns>
    public string sendPerson(int personId, string FromAddress, string subject, string Body, bool Immediate, bool HTML, int emailIdOrZeroForLog, string template, bool EmailAllowLinkEID) {
        string returnStatus = "";
        try {
            int CS;
            string ToAddress;
            string layoutError = "";
            string subjectEncoded;
            string bodyEncoded;
            string templateEncoded;
            // 
            subjectEncoded = subject;
            bodyEncoded = Body;
            templateEncoded = template;
            // 
            CS = cpcore.db.cs_openContentRecord("People", personId, ,, ,, "email");
            if (cpcore.db.csOk(CS)) {
                ToAddress = cpcore.db.csGetText(CS, "email").Trim();
                if ((ToAddress == "")) {
                    returnStatus = "The email was not sent because the to-address was blank.";
                }
                else if ((((ToAddress.IndexOf("@", 0) + 1) 
                            == 0) 
                            || ((ToAddress.IndexOf(".", 0) + 1) 
                            == 0))) {
                    returnStatus = ("The email was not sent because the to-address [" 
                                + (ToAddress + "] was not valid."));
                }
                else if ((FromAddress == "")) {
                    returnStatus = "The email was not sent because the from-address was blank.";
                }
                else if ((((FromAddress.IndexOf("@", 0) + 1) 
                            == 0) 
                            || ((FromAddress.IndexOf(".", 0) + 1) 
                            == 0))) {
                    returnStatus = ("The email was not sent because the from-address [" 
                                + (FromAddress + "] was not valid."));
                }
                else {
                    // 
                    //  encode subject
                    // 
                    subjectEncoded = cpcore.html.executeContentCommands(null, subjectEncoded, CPUtilsBaseClass.addonContext.ContextEmail, personId, true, layoutError);
                    subjectEncoded = cpcore.html.convertActiveContentToHtmlForEmailSend(subjectEncoded, personId, "");
                    // subjectEncoded = cpcore.html.convertActiveContent_internal(subjectEncoded, personId, "", 0, 0, False, EmailAllowLinkEID, True, True, False, True, "", "http://" & cpcore.serverConfig.appConfig.domainList(0), True, 0, "", CPUtilsBaseClass.addonContext.ContextEmail, True, Nothing, False)
                    // 
                    //  encode Body
                    // 
                    bodyEncoded = cpcore.html.executeContentCommands(null, bodyEncoded, CPUtilsBaseClass.addonContext.ContextEmail, personId, true, layoutError);
                    bodyEncoded = cpcore.html.convertActiveContentToHtmlForEmailSend(bodyEncoded, personId, "");
                    // bodyEncoded = cpcore.html.convertActiveContent_internal(bodyEncoded, personId, "", 0, 0, False, EmailAllowLinkEID, True, True, False, True, "", "http://" & cpcore.serverConfig.appConfig.domainList(0), True, 0, "", CPUtilsBaseClass.addonContext.ContextEmail, True, Nothing, False)
                    // 
                    //  encode template
                    // 
                    if ((templateEncoded != "")) {
                        templateEncoded = cpcore.html.executeContentCommands(null, templateEncoded, CPUtilsBaseClass.addonContext.ContextEmail, personId, true, layoutError);
                        templateEncoded = cpcore.html.convertActiveContentToHtmlForEmailSend(templateEncoded, personId, "");
                        // templateEncoded = cpcore.html.convertActiveContent_internal(templateEncoded, personId, "", 0, 0, False, EmailAllowLinkEID, True, True, False, True, "", "http://" & cpcore.serverConfig.appConfig.domainList(0), True, 0, "", CPUtilsBaseClass.addonContext.ContextEmail, True, Nothing, False)
                        // 
                        if (((templateEncoded.IndexOf(fpoContentBox, 0) + 1) 
                                    != 0)) {
                            bodyEncoded = genericController.vbReplace(templateEncoded, fpoContentBox, bodyEncoded);
                        }
                        else {
                            bodyEncoded = (templateEncoded + bodyEncoded);
                        }
                        
                    }
                    
                    bodyEncoded = genericController.vbReplace(bodyEncoded, "#member_id#", personId.ToString);
                    bodyEncoded = genericController.vbReplace(bodyEncoded, "#member_email#", ToAddress);
                    // 
                    returnStatus = send(ToAddress, FromAddress, subjectEncoded, bodyEncoded, "", "", "", Immediate, HTML, emailIdOrZeroForLog);
                }
                
            }
            
            cpcore.db.csClose(CS);
        }
        catch (Exception ex) {
            cpCore.handleException(ex);
            throw;
        }
        
        return returnStatus;
    }
    
    // 
    // ========================================================================
    //  Set the email sql for all members marked to receive the email
    //    Used to send the email and as body on the email test
    // ========================================================================
    // 
    public string getGroupSql(int EmailID) {
        return ("SELECT " + (" u.ID AS ID" + (" ,u.Name AS Name" + (" ,u.Email AS Email " + (" " + (" from " + (" (((ccMembers u" + (" left join ccMemberRules mr on mr.memberid=u.id)" + (" left join ccGroups g on g.id=mr.groupid)" + (" left join ccEmailGroups r on r.groupid=g.id)" + (" " + (" where " + (" (r.EmailID=1) " + (" and(r.Active<>0) " + (" and(g.Active<>0) " + (" and(g.AllowBulkEmail<>0) " + (" and(mr.Active<>0) " + (" and(u.Active<>0) " + (" and(u.AllowBulkEmail<>0)" + (" AND((mr.DateExpires is null)OR(mr.DateExpires>\'20161205 22:40:58:184\')) " + (" " + (" group by " + (" u.ID, u.Name, u.Email " + (" " + (" having ((u.Email Is Not Null) and(u.Email<>\'\')) " + (" " + (" order by u.Email,u.ID" + " ")))))))))))))))))))))))))));
    }
    
    // 
    //  ----- Need to test this and make it public
    // 
    //    This is what the admin site should call for both test and group email
    //    Making it public lets developers send email that administrators can control
    // 
    public string sendSystem(string EMailName, string AdditionalCopy, int AdditionalMemberIDOrZero) {
        // TODO: On Error GoTo Warning!!!: The statement is not translatable 
        // 'Dim th as integer : th = profileLogMethodEnter("SendSystemEmail")
        // 
        string returnString;
        bool isAdmin;
        int iAdditionalMemberID;
        string layoutError;
        string emailstyles;
        int EmailRecordID;
        int CSPeople;
        int CSEmail;
        int CSLog;
        string EmailToAddress;
        string EmailToName;
        string SQL;
        string EmailFrom;
        string EmailSubjectSource;
        string EmailBodySource;
        string ConfirmBody = String.Empty;
        bool EmailAllowLinkEID;
        int EmailToConfirmationMemberID;
        string EmailStatusMessage = String.Empty;
        int EMailToMemberID;
        string EmailSubject;
        string ClickFlagQuery;
        string EmailBody;
        string EmailStatus;
        string BounceAddress;
        string SelectList;
        int EMailTemplateID;
        string EmailTemplate;
        string EmailTemplateSource = String.Empty;
        int CS;
        bool isValid;
        // 
        returnString = "";
        iAdditionalMemberID = AdditionalMemberIDOrZero;
        // 
        if (true) {
            SelectList = "ID,TestMemberID,FromAddress,Subject,copyfilename,AddLinkEID,AllowSpamFooter,EmailTemplateID";
        }
        else {
            SelectList = "ID,TestMemberID,FromAddress,Subject,copyfilename,AddLinkEID,AllowSpamFooter,0 as EmailTemplateID";
        }
        
        CSEmail = cpcore.db.csOpen("System Email", ("name=" + cpcore.db.encodeSQLText(EMailName)), "ID", ,, ,, SelectList);
        if (!cpcore.db.csOk(CSEmail)) {
            // 
            //  ----- Email was not found
            // 
            cpcore.db.csClose(CSEmail);
            CSEmail = cpcore.db.csInsertRecord("System Email");
            cpcore.db.csSet(CSEmail, "name", EMailName);
            cpcore.db.csSet(CSEmail, "Subject", EMailName);
            cpcore.db.csSet(CSEmail, "FromAddress", cpcore.siteProperties.getText("EmailAdmin", ("webmaster@" + cpcore.serverConfig.appConfig.domainList(0))));
            // Call app.csv_SetCS(CSEmail, "caption", EmailName)
            cpcore.db.csClose(CSEmail);
            cpcore.handleException(new ApplicationException(("No system email was found with the name [" 
                                + (EMailName + "]. A new email blank was created but not sent."))));
        }
        else {
            // 
            //  --- collect values needed for send
            // 
            EmailRecordID = cpcore.db.csGetInteger(CSEmail, "ID");
            EmailToConfirmationMemberID = cpcore.db.csGetInteger(CSEmail, "testmemberid");
            EmailFrom = cpcore.db.csGetText(CSEmail, "FromAddress");
            EmailSubjectSource = cpcore.db.csGetText(CSEmail, "Subject");
            EmailBodySource = (cpcore.db.csGet(CSEmail, "copyfilename") + AdditionalCopy);
            EmailAllowLinkEID = cpcore.db.csGetBoolean(CSEmail, "AddLinkEID");
            BounceAddress = cpcore.siteProperties.getText("EmailBounceAddress", "");
            if ((BounceAddress == "")) {
                BounceAddress = EmailFrom;
            }
            
            EMailTemplateID = cpcore.db.csGetInteger(CSEmail, "EmailTemplateID");
            // 
            //  Get the Email Template
            // 
            if ((EMailTemplateID != 0)) {
                CS = cpcore.db.cs_openContentRecord("Email Templates", EMailTemplateID);
                if (cpcore.db.csOk(CS)) {
                    EmailTemplateSource = cpcore.db.csGet(CS, "BodyHTML");
                }
                
                cpcore.db.csClose(CS);
            }
            
            if ((EmailTemplateSource == "")) {
                EmailTemplateSource = "<div style=\"padding:10px\"><ac type=content></div>";
            }
            
            // 
            //  add styles to the template
            // 
            // emailstyles = getStyles(EmailRecordID)
            // EmailTemplateSource = emailstyles & EmailTemplateSource
            // 
            //  Spam Footer
            // 
            if (cpcore.db.csGetBoolean(CSEmail, "AllowSpamFooter")) {
                // 
                //  This field is default true, and non-authorable
                //  It will be true in all cases, except a possible unforseen exception
                // 
                EmailTemplateSource = (EmailTemplateSource + ("<div style=\"clear: both;padding:10px;\">" 
                            + (genericController.csv_GetLinkedText(("<a href=\"" 
                                + (genericController.encodeHTML(("http://" 
                                    + (cpcore.serverConfig.appConfig.domainList(0) + ("/" 
                                    + (cpcore.siteProperties.serverPageDefault + ("?" 
                                    + (rnEmailBlockRecipientEmail + "=#member_email#"))))))) + "\">")), cpcore.siteProperties.getText("EmailSpamFooter", DefaultSpamFooter)) + "</div>")));
            }
            
            // 
            //  --- Send message to the additional member
            // 
            if ((iAdditionalMemberID != 0)) {
                EmailStatusMessage = (EmailStatusMessage 
                            + (BR + ("Primary Recipient:" + BR)));
                CSPeople = cpcore.db.cs_openContentRecord("People", iAdditionalMemberID, ,, ,, "ID,Name,Email");
                if (cpcore.db.csOk(CSPeople)) {
                    EMailToMemberID = cpcore.db.csGetInteger(CSPeople, "ID");
                    EmailToName = cpcore.db.csGetText(CSPeople, "name");
                    EmailToAddress = cpcore.db.csGetText(CSPeople, "email");
                    if ((EmailToAddress == "")) {
                        EmailStatusMessage = (EmailStatusMessage + ("  Error: Not Sent to " 
                                    + (EmailToName + (" (people #" 
                                    + (EMailToMemberID + (") because their email address was blank." + BR))))));
                    }
                    else {
                        EmailStatus = sendPerson(iAdditionalMemberID, EmailFrom, EmailSubjectSource, EmailBodySource, false, true, EmailRecordID, EmailTemplateSource, EmailAllowLinkEID);
                        if ((EmailStatus == "")) {
                            EmailStatus = "ok";
                        }
                        
                        EmailStatusMessage = (EmailStatusMessage + ("  Sent to " 
                                    + (EmailToName + (" at " 
                                    + (EmailToAddress + (", Status = " 
                                    + (EmailStatus + BR)))))));
                    }
                    
                }
                
                cpcore.db.csClose(CSPeople);
            }
            
            // 
            //  --- Send message to everyone selected
            // 
            EmailStatusMessage = (EmailStatusMessage 
                        + (BR + ("Recipients in selected System Email groups:" + BR)));
            SQL = getGroupSql(EmailRecordID);
            CSPeople = cpcore.db.csOpenSql_rev("default", SQL);
            while (cpcore.db.csOk(CSPeople)) {
                EMailToMemberID = cpcore.db.csGetInteger(CSPeople, "ID");
                EmailToName = cpcore.db.csGetText(CSPeople, "name");
                EmailToAddress = cpcore.db.csGetText(CSPeople, "email");
                if ((EmailToAddress == "")) {
                    EmailStatusMessage = (EmailStatusMessage + ("  Not Sent to " 
                                + (EmailToName + (", people #" 
                                + (EMailToMemberID + (" because their email address was blank." + BR))))));
                }
                else {
                    EmailStatus = sendPerson(EMailToMemberID, EmailFrom, EmailSubjectSource, EmailBodySource, false, true, EmailRecordID, EmailTemplateSource, EmailAllowLinkEID);
                    if ((EmailStatus == "")) {
                        EmailStatus = "ok";
                    }
                    
                    EmailStatusMessage = (EmailStatusMessage + ("  Sent to " 
                                + (EmailToName + (" at " 
                                + (EmailToAddress + (", Status = " 
                                + (EmailStatus + BR)))))));
                    cpcore.db.csGoNext(CSPeople);
                }
                
            }
            
            cpcore.db.csClose(CSPeople);
            // 
            //  --- Send the completion message to the administrator
            // 
            if ((EmailToConfirmationMemberID == 0)) {
                //  AddUserError ("No confirmation email was sent because no confirmation member was selected")
            }
            else {
                // 
                //  get the confirmation info
                // 
                isValid = false;
                CSPeople = cpcore.db.cs_openContentRecord("people", EmailToConfirmationMemberID);
                if (cpcore.db.csOk(CSPeople)) {
                    isValid = cpcore.db.csGetBoolean(CSPeople, "active");
                    EMailToMemberID = cpcore.db.csGetInteger(CSPeople, "ID");
                    EmailToName = cpcore.db.csGetText(CSPeople, "name");
                    EmailToAddress = cpcore.db.csGetText(CSPeople, "email");
                    isAdmin = cpcore.db.csGetBoolean(CSPeople, "admin");
                }
                
                cpcore.db.csClose(CSPeople);
                // 
                if (!isValid) {
                    // returnString = "Administrator: The confirmation email was not sent because the confirmation email person is not selected or inactive, " & EmailStatus
                }
                else {
                    // 
                    //  Encode the body
                    // 
                    EmailBody = (EmailBodySource + "");
                    EmailTemplate = EmailTemplateSource;
                    // 
                    EmailSubject = EmailSubjectSource;
                    // 
                    ConfirmBody = (ConfirmBody + ("<div style=\"padding:10px;\">" + BR));
                    ConfirmBody = (ConfirmBody + ("The follow System Email was sent." + BR));
                    ConfirmBody = (ConfirmBody + ("" + BR));
                    ConfirmBody = (ConfirmBody + ("If this email includes personalization, each email sent was personalized to it\'s recipient. This conf" +
                    "irmation has been personalized to you." + BR));
                    ConfirmBody = (ConfirmBody + ("" + BR));
                    ConfirmBody = (ConfirmBody + ("Subject: " 
                                + (EmailSubject + BR)));
                    ConfirmBody = (ConfirmBody + ("From: " 
                                + (EmailFrom + BR)));
                    ConfirmBody = (ConfirmBody + ("Bounces return to: " 
                                + (BounceAddress + BR)));
                    ConfirmBody = (ConfirmBody + ("Body:" + BR));
                    ConfirmBody = (ConfirmBody + ("<div style=\"clear:all\">----------------------------------------------------------------------</div>" + BR));
                    ConfirmBody = (ConfirmBody 
                                + (EmailBody + BR));
                    ConfirmBody = (ConfirmBody + ("<div style=\"clear:all\">----------------------------------------------------------------------</div>" + BR));
                    ConfirmBody = (ConfirmBody + ("--- recipient list ---" + BR));
                    ConfirmBody = (ConfirmBody 
                                + (EmailStatusMessage + BR));
                    ConfirmBody = (ConfirmBody + ("--- end of list ---" + BR));
                    ConfirmBody = (ConfirmBody + "</div>");
                    EmailStatus = sendPerson(EmailToConfirmationMemberID, EmailFrom, ("System Email confirmation from " + cpcore.serverConfig.appConfig.domainList(0)), ConfirmBody, false, true, EmailRecordID, "", false);
                    if ((isAdmin 
                                && (EmailStatus != ""))) {
                        returnString = ("Administrator: There was a problem sending the confirmation email, " + EmailStatus);
                    }
                    
                }
                
            }
            
            // 
            //  ----- Done
            // 
            cpcore.db.csClose(CSPeople);
        }
        
        cpcore.db.csClose(CSEmail);
        // 
        sendSystem = returnString;
        // 
        // TODO: Exit Function: Warning!!! Need to return the value
        return;
        // 
        //  ----- Error Trap
        // 
    ErrorTrap:
        throw new applicationException("Unexpected exception");
        //  Call cpcore.handleLegacyError7("csv_SendSystemEmail", "Unexpected Trap")
    }
    
    // 
    // ========================================================================
    // '' <summary>
    // '' Send Email to address
    // '' </summary>
    // '' <param name="ToAddress"></param>
    // '' <param name="FromAddress"></param>
    // '' <param name="SubjectMessage"></param>
    // '' <param name="BodyMessage"></param>
    // '' <param name="optionalEmailIdForLog"></param>
    // '' <param name="Immediate"></param>
    // '' <param name="HTML"></param>
    // '' <returns>Returns OK if successful, otherwise returns user status</returns>
    public string send_Legacy(string ToAddress, string FromAddress, string SubjectMessage, string BodyMessage, int optionalEmailIdForLog, void =, void 0, bool Immediate, void =, void True, bool HTML, void =, void False) {
        string returnStatus = "";
        // Warning!!! Optional parameters not supported
        // Warning!!! Optional parameters not supported
        // Warning!!! Optional parameters not supported
        try {
            returnStatus = send(genericController.encodeText(ToAddress), genericController.encodeText(FromAddress), genericController.encodeText(SubjectMessage), genericController.encodeText(BodyMessage), "", "", "", Immediate, genericController.EncodeBoolean(HTML), genericController.EncodeInteger(optionalEmailIdForLog));
        }
        catch (Exception ex) {
            cpCore.handleException(ex);
            throw;
        }
        
        return returnStatus;
    }
    
    // 
    // ====================================================================================================
    // '' <summary>
    // '' send the confirmation email as a test
    // '' </summary>
    // '' <param name="EmailID"></param>
    // '' <param name="ConfirmationMemberID"></param>
    public void sendConfirmationTest(int EmailID, int ConfirmationMemberID) {
        try {
            string ConfirmFooter = String.Empty;
            int TotalCnt;
            int BlankCnt;
            int DupCnt;
            string DupList = String.Empty;
            int BadCnt;
            string BadList = String.Empty;
            int EmailLen;
            string LastEmail;
            string Emailtext;
            string LastDupEmail = String.Empty;
            string EmailLine;
            string TotalList = String.Empty;
            string EMailName;
            int EmailMemberID;
            int Posat;
            int PosDot;
            int CS;
            string EmailSubject;
            string EmailBody;
            string EmailTemplate;
            int EMailTemplateID;
            int CSTemplate;
            int CSPeople;
            string SQL;
            string EmailStatus;
            //  Dim emailstyles As String
            // 
            CS = cpcore.db.csOpenRecord("email", EmailID);
            if (!cpcore.db.csOk(CS)) {
                errorController.error_AddUserError(cpcore, "There was a problem sending the email confirmation. The email record could not be found.");
            }
            else {
                EmailSubject = cpcore.db.csGet(CS, "Subject");
                EmailBody = cpcore.db.csGet(CS, "copyFilename");
                // 
                //  merge in template
                // 
                EmailTemplate = "";
                EMailTemplateID = cpcore.db.csGetInteger(CS, "EmailTemplateID");
                if ((EMailTemplateID != 0)) {
                    CSTemplate = cpcore.db.csOpenRecord("Email Templates", EMailTemplateID, ,, "BodyHTML");
                    if (cpcore.db.csOk(CSTemplate)) {
                        EmailTemplate = cpcore.db.csGet(CSTemplate, "BodyHTML");
                    }
                    
                    cpcore.db.csClose(CSTemplate);
                }
                
                // 
                //  styles
                // 
                // emailstyles = getStyles(EmailID)
                // EmailBody = emailstyles & EmailBody
                // 
                //  spam footer
                // 
                if (cpcore.db.csGetBoolean(CS, "AllowSpamFooter")) {
                    // 
                    //  This field is default true, and non-authorable
                    //  It will be true in all cases, except a possible unforseen exception
                    // 
                    EmailBody = (EmailBody + ("<div style=\"clear:both;padding:10px;\">" 
                                + (genericController.csv_GetLinkedText(("<a href=\"" 
                                    + (genericController.encodeHTML((cpcore.webServer.requestProtocol 
                                        + (cpcore.webServer.requestDomain 
                                        + (requestAppRootPath 
                                        + (cpcore.siteProperties.serverPageDefault + ("?" 
                                        + (rnEmailBlockRecipientEmail + "=#member_email#"))))))) + "\">")), cpcore.siteProperties.getText("EmailSpamFooter", DefaultSpamFooter)) + "</div>")));
                    EmailBody = genericController.vbReplace(EmailBody, "#member_email#", "UserEmailAddress");
                }
                
                // 
                //  Confirm footer
                // 
                SQL = getGroupSql(EmailID);
                CSPeople = cpcore.db.csOpenSql(SQL);
                if (!cpcore.db.csOk(CSPeople)) {
                    errorController.error_AddUserError(cpcore, "There are no valid recipients of this email, other than the confirmation address. Either no groups or" +
                        " topics were selected, or those selections contain no people with both a valid email addresses and \'" +
                        "Allow Group Email\' enabled.");
                }
                else {
                    // TotalList = TotalList & "--- all recipients ---" & BR
                    LastEmail = "empty";
                    while (cpcore.db.csOk(CSPeople)) {
                        Emailtext = cpcore.db.csGet(CSPeople, "email");
                        EMailName = cpcore.db.csGet(CSPeople, "name");
                        EmailMemberID = cpcore.db.csGetInteger(CSPeople, "ID");
                        if ((EMailName == "")) {
                            EMailName = ("no name (member id " 
                                        + (EmailMemberID + ")"));
                        }
                        
                        EmailLine = (Emailtext + (" for " + EMailName));
                        if ((Emailtext == "")) {
                            BlankCnt = (BlankCnt + 1);
                        }
                        else if ((Emailtext == LastEmail)) {
                            DupCnt = (DupCnt + 1);
                            if ((Emailtext != LastDupEmail)) {
                                DupList = (DupList + ("<div class=i>" 
                                            + (Emailtext + ("</div>" + BR))));
                                LastDupEmail = Emailtext;
                            }
                            
                        }
                        
                        EmailLen = Emailtext.Length;
                        Posat = genericController.vbInstr(1, Emailtext, "@");
                        PosDot = InStrRev(Emailtext, ".");
                        if ((EmailLen < 6)) {
                            BadCnt = (BadCnt + 1);
                            BadList = (BadList 
                                        + (EmailLine + BR));
                        }
                        else if (((Posat < 2) 
                                    || (Posat 
                                    > (EmailLen - 4)))) {
                            BadCnt = (BadCnt + 1);
                            BadList = (BadList 
                                        + (EmailLine + BR));
                        }
                        else if (((PosDot < 4) 
                                    || (PosDot 
                                    > (EmailLen - 2)))) {
                            BadCnt = (BadCnt + 1);
                            BadList = (BadList 
                                        + (EmailLine + BR));
                        }
                        
                        TotalList = (TotalList 
                                    + (EmailLine + BR));
                        LastEmail = Emailtext;
                        TotalCnt = (TotalCnt + 1);
                        cpcore.db.csGoNext(CSPeople);
                    }
                    
                    // TotalList = TotalList & "--- end all recipients ---" & BR
                }
                
                cpcore.db.csClose(CSPeople);
                // 
                if ((DupCnt == 1)) {
                    errorController.error_AddUserError(cpcore, "There is 1 duplicate email address. See the test email for details.");
                    ConfirmFooter = (ConfirmFooter + (@"<div style=""clear:all"">WARNING: There is 1 duplicate email address. Only one email will be sent to each address. If the email includes personalization, or if you are using link authentication to automatically log in the user, you may want to correct duplicates to be sure the email is created correctly.<div style=""margin:20px;"">" 
                                + (DupList + "</div></div>")));
                }
                else if ((DupCnt > 1)) {
                    errorController.error_AddUserError(cpcore, ("There are " 
                                    + (DupCnt + " duplicate email addresses. See the test email for details")));
                    ConfirmFooter = (ConfirmFooter + ("<div style=\"clear:all\">WARNING: There are " 
                                + (DupCnt + (@" duplicate email addresses. Only one email will be sent to each address. If the email includes personalization, or if you are using link authentication to automatically log in the user, you may want to correct duplicates to be sure the email is created correctly.<div style=""margin:20px;"">" 
                                + (DupList + "</div></div>")))));
                }
                
                // 
                if ((BadCnt == 1)) {
                    errorController.error_AddUserError(cpcore, "There is 1 invalid email address. See the test email for details.");
                    ConfirmFooter = (ConfirmFooter + ("<div style=\"clear:all\">WARNING: There is 1 invalid email address<div style=\"margin:20px;\">" 
                                + (BadList + "</div></div>")));
                }
                else if ((BadCnt > 1)) {
                    errorController.error_AddUserError(cpcore, ("There are " 
                                    + (BadCnt + " invalid email addresses. See the test email for details")));
                    ConfirmFooter = (ConfirmFooter + ("<div style=\"clear:all\">WARNING: There are " 
                                + (BadCnt + (" invalid email addresses<div style=\"margin:20px;\">" 
                                + (BadList + "</div></div>")))));
                }
                
                // 
                if ((BlankCnt == 1)) {
                    errorController.error_AddUserError(cpcore, "There is 1 blank email address. See the test email for details");
                    ConfirmFooter = (ConfirmFooter + "<div style=\"clear:all\">WARNING: There is 1 blank email address.</div>");
                }
                else if ((BlankCnt > 1)) {
                    errorController.error_AddUserError(cpcore, ("There are " 
                                    + (DupCnt + " blank email addresses. See the test email for details.")));
                    ConfirmFooter = (ConfirmFooter + ("<div style=\"clear:all\">WARNING: There are " 
                                + (BlankCnt + " blank email addresses.</div>")));
                }
                
                // 
                if ((TotalCnt == 0)) {
                    ConfirmFooter = (ConfirmFooter + "<div style=\"clear:all\">WARNING: There are no recipients for this email.</div>");
                }
                else if ((TotalCnt == 1)) {
                    ConfirmFooter = (ConfirmFooter + ("<div style=\"clear:all\">There is 1 recipient<div style=\"margin:20px;\">" 
                                + (TotalList + "</div></div>")));
                }
                else {
                    ConfirmFooter = (ConfirmFooter + ("<div style=\"clear:all\">There are " 
                                + (TotalCnt + (" recipients<div style=\"margin:20px;\">" 
                                + (TotalList + "</div></div>")))));
                }
                
                // 
                if ((ConfirmationMemberID == 0)) {
                    errorController.error_AddUserError(cpcore, "No confirmation email was send because a Confirmation member is not selected");
                }
                else {
                    EmailBody = (EmailBody + ("<div style=\"clear:both;padding:10px;margin:10px;border:1px dashed #888;\">Administrator<br><br>" 
                                + (ConfirmFooter + "</div>")));
                    EmailStatus = sendPerson(ConfirmationMemberID, cpcore.db.csGetText(CS, "FromAddress"), EmailSubject, EmailBody, true, true, EmailID, EmailTemplate, false);
                    if ((EmailStatus != "ok")) {
                        errorController.error_AddUserError(cpcore, EmailStatus);
                    }
                    
                }
                
            }
            
            cpcore.db.csClose(CS);
        }
        catch (Exception ex) {
            cpCore.handleException(ex);
            throw;
        }
        
    }
    
    // 
    // ========================================================================
    //  main_SendFormEmail
    //    sends an email with the contents of a form
    // ========================================================================
    // 
    public void sendForm(string SendTo, string SendFrom, string SendSubject) {
        try {
            string Message = String.Empty;
            string iSendTo;
            string iSendFrom;
            string iSendSubject;
            // 
            iSendTo = genericController.encodeText(SendTo);
            iSendFrom = genericController.encodeText(SendFrom);
            iSendSubject = genericController.encodeText(SendSubject);
            // 
            if (((iSendTo.IndexOf("@") + 1) 
                        == 0)) {
                iSendTo = cpcore.siteProperties.getText("TrapEmail");
                iSendSubject = "EmailForm with bad Sendto address";
                Message = ("Subject: " + iSendSubject);
                Message = (Message + "\r\n");
            }
            
            Message = (Message + ("The form was submitted " 
                        + (cpCore.doc.profileStartTime + "\r\n")));
            Message = (Message + "\r\n");
            Message = (Message + ("All text fields are included, completed or not." + "\r\n"));
            Message = (Message + ("Only those checkboxes that are checked are included." + "\r\n"));
            Message = (Message + ("Entries are not in the order they appeared on the form." + "\r\n"));
            Message = (Message + "\r\n");
            foreach (string key in cpcore.docProperties.getKeyList) {
                // With...
                if (cpcore.docProperties.getProperty(key).IsForm) {
                    if ((genericController.vbUCase(cpcore.docProperties.getProperty(key).Value) == "ON")) {
                        Message = (Message 
                                    + (cpcore.docProperties.getProperty(key).Name + (": Yes" + ("\r\n" + "\r\n"))));
                    }
                    else {
                        Message = (Message 
                                    + (cpcore.docProperties.getProperty(key).Name + (": " 
                                    + (cpcore.docProperties.getProperty(key).Value + ("\r\n" + "\r\n")))));
                    }
                    
                }
                
            }
            
            // 
            send_Legacy(iSendTo, iSendFrom, iSendSubject, Message, ,, false, false);
        }
        catch (Exception ex) {
            cpcore.handleException(ex);
        }
        
    }
    
    // 
    // 
    // 
    public void sendGroup(string GroupList, string FromAddress, string subject, string Body, bool Immediate, bool HTML) {
        // TODO: On Error GoTo Warning!!!: The statement is not translatable 
        // 'Dim th as integer : th = profileLogMethodEnter("Proc00271")
        // 
        // If Not (true) Then Exit Sub
        // 
        string rootUrl;
        string MethodName;
        string[] Groups;
        int GroupCount;
        int GroupPointer;
        string iiGroupList;
        int ParsePosition;
        string iGroupList;
        string iFromAddress;
        string iSubjectSource;
        string iSubject;
        string iBodySource;
        string iBody;
        bool iImmediate;
        bool iHTML;
        string SQL;
        int CSPointer;
        int ToMemberID;
        // 
        MethodName = "main_SendGroupEmail";
        iGroupList = genericController.encodeText(GroupList);
        iFromAddress = genericController.encodeText(FromAddress);
        iSubjectSource = genericController.encodeText(subject);
        iBodySource = genericController.encodeText(Body);
        iImmediate = genericController.EncodeBoolean(Immediate);
        iHTML = genericController.EncodeBoolean(HTML);
        // 
        //  Fix links for HTML send - must do it now before encodehtml so eid links will attach
        // 
        rootUrl = ("http://" 
                    + (cpcore.webServer.requestDomain + requestAppRootPath));
        iBodySource = genericController.ConvertLinksToAbsolute(iBodySource, rootUrl);
        // 
        //  Build the list of groups
        // 
        if ((iGroupList != "")) {
            iiGroupList = iGroupList;
            while ((iiGroupList != "")) {
                object Preserve;
                Groups[GroupCount];
                ParsePosition = genericController.vbInstr(1, iiGroupList, ",");
                if ((ParsePosition == 0)) {
                    Groups[GroupCount] = iiGroupList;
                    iiGroupList = "";
                }
                else {
                    Groups[GroupCount] = iiGroupList.Substring(0, (ParsePosition - 1));
                    iiGroupList = iiGroupList.Substring(ParsePosition);
                }
                
                GroupCount = (GroupCount + 1);
            }
            
        }
        
        if ((GroupCount > 0)) {
            // 
            //  Build the SQL statement
            // 
            SQL = ("SELECT DISTINCT ccMembers.ID" + (" FROM (ccMembers LEFT JOIN ccMemberRules ON ccMembers.ID = ccMemberRules.MemberID) LEFT JOIN ccgroups" +
            " ON ccMemberRules.GroupID = ccgroups.ID" + (" WHERE (((ccMembers.Active)<>0) AND ((ccMembers.AllowBulkEmail)<>0) AND ((ccMemberRules.Active)<>0) A" +
            "ND ((ccgroups.Active)<>0) AND ((ccgroups.AllowBulkEmail)<>0)AND((ccMemberRules.DateExpires is null)O" +
            "R(ccMemberRules.DateExpires>" 
                        + (cpcore.db.encodeSQLDate(cpCore.doc.profileStartTime) + ")) AND ("))));
            for (GroupPointer = 0; (GroupPointer 
                        <= (GroupCount - 1)); GroupPointer++) {
                if ((GroupPointer == 0)) {
                    ("(ccgroups.Name=" 
                                + (cpcore.db.encodeSQLText(Groups[GroupPointer]) + ")"));
                }
                else {
                    ("OR(ccgroups.Name=" 
                                + (cpcore.db.encodeSQLText(Groups[GroupPointer]) + ")"));
                }
                
            }
            
            "));";
            CSPointer = cpcore.db.csOpenSql(SQL);
            while (cpcore.db.csOk(CSPointer)) {
                ToMemberID = genericController.EncodeInteger(cpcore.db.csGetInteger(CSPointer, "ID"));
                iSubject = iSubjectSource;
                iBody = iBodySource;
                // 
                //  send
                // 
                sendPerson(ToMemberID, iFromAddress, iSubject, iBody, iImmediate, iHTML, 0, "", false);
                cpcore.db.csGoNext(CSPointer);
            }
            
        }
        
        // 
        return;
        // 
        //  ----- Error Trap
        // 
    ErrorTrap:
        throw new applicationException("Unexpected exception");
        //  Call cpcore.handleLegacyError18(MethodName)
        // 
    }
    
    // 
    //  ----- Need to test this and make it public
    // 
    //    This is what the admin site should call for both test and group email
    //    Making it public lets developers send email that administrators can control
    // 
    public void sendSystem_Legacy(string EMailName, string AdditionalCopy, void =, void , int AdditionalMemberID, void =, void 0) {
        string EmailStatus;
        // Warning!!! Optional parameters not supported
        // Warning!!! Optional parameters not supported
        // 
        EmailStatus = sendSystem(genericController.encodeText(EMailName), genericController.encodeText(AdditionalCopy), genericController.EncodeInteger(AdditionalMemberID));
        if ((cpCore.doc.authContext.isAuthenticatedAdmin(cpcore) 
                    && (EmailStatus != ""))) {
            errorController.error_AddUserError(cpcore, ("Administrator: There was a problem sending the confirmation email, " + EmailStatus));
        }
        
        return;
    }
    
    // 
    // =============================================================================
    //  Send the Member his username and password
    // =============================================================================
    // 
    public bool sendPassword(string Email) {
        bool returnREsult = false;
        try {
            string sqlCriteria;
            string Message = "";
            int CS;
            string workingEmail;
            string FromAddress = "";
            string subject = "";
            bool allowEmailLogin;
            string Password;
            string Username;
            bool updateUser;
            int atPtr;
            int Index;
            string EMailName;
            bool usernameOK;
            int recordCnt;
            int Ptr;
            // 
            const string passwordChrs = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ012345678999999";
            const int passwordChrsLength = 62;
            // 
            workingEmail = genericController.encodeText(Email);
            // 
            returnREsult = false;
            if ((workingEmail == "")) {
                // hint = "110"
                errorController.error_AddUserError(cpcore, "Please enter your email address before requesting your username and password.");
            }
            else {
                // hint = "120"
                atPtr = genericController.vbInstr(1, workingEmail, "@");
                if ((atPtr < 2)) {
                    // 
                    //  email not valid
                    // 
                    // hint = "130"
                    errorController.error_AddUserError(cpcore, "Please enter a valid email address before requesting your username and password.");
                }
                else {
                    // hint = "140"
                    EMailName = vbMid(workingEmail, 1, (atPtr - 1));
                    // 
                    logController.logActivity2(cpcore, ("password request for email " + workingEmail), cpCore.doc.authContext.user.id, cpCore.doc.authContext.user.OrganizationID);
                    // 
                    allowEmailLogin = cpcore.siteProperties.getBoolean("allowEmailLogin", false);
                    recordCnt = 0;
                    sqlCriteria = ("(email=" 
                                + (cpcore.db.encodeSQLText(workingEmail) + ")"));
                    if (true) {
                        sqlCriteria = (sqlCriteria + ("and((dateExpires is null)or(dateExpires>" 
                                    + (cpcore.db.encodeSQLDate(DateTime.Now) + "))")));
                    }
                    
                    CS = cpcore.db.csOpen("People", sqlCriteria, "ID", "username,password", 1);
                    // TODO: Labeled Arguments not supported. Argument: 4 := 'SelectFieldList'
                    // TODO: Labeled Arguments not supported. Argument: 5 := 'PageSize'
                    if (!cpcore.db.csOk(CS)) {
                        // 
                        //  valid login account for this email not found
                        // 
                        if ((vbMid(workingEmail, (atPtr + 1)).ToLower() == "contensive.com")) {
                            // 
                            //  look for expired account to renew
                            // 
                            cpcore.db.csClose(CS);
                            CS = cpcore.db.csOpen("People", ("((email=" 
                                            + (cpcore.db.encodeSQLText(workingEmail) + "))")), "ID", 1);
                            // TODO: Labeled Arguments not supported. Argument: 4 := 'PageSize'
                            if (cpcore.db.csOk(CS)) {
                                // 
                                //  renew this old record
                                // 
                                // hint = "150"
                                cpcore.db.csSet(CS, "developer", "1");
                                cpcore.db.csSet(CS, "admin", "1");
                                cpcore.db.csSet(CS, "dateExpires", DateTime.Now.AddDays(7).Date.ToString());
                            }
                            else {
                                // 
                                //  inject support record
                                // 
                                // hint = "150"
                                cpcore.db.csClose(CS);
                                CS = cpcore.db.csInsertRecord("people");
                                cpcore.db.csSet(CS, "name", "Contensive Support");
                                cpcore.db.csSet(CS, "email", workingEmail);
                                cpcore.db.csSet(CS, "developer", "1");
                                cpcore.db.csSet(CS, "admin", "1");
                                cpcore.db.csSet(CS, "dateExpires", DateTime.Now.AddDays(7).Date.ToString());
                            }
                            
                            cpcore.db.csSave2(CS);
                        }
                        else {
                            // hint = "155"
                            errorController.error_AddUserError(cpcore, "No current user was found matching this email address. Please try again. ");
                        }
                        
                    }
                    
                    if (cpcore.db.csOk(CS)) {
                        // hint = "160"
                        FromAddress = cpcore.siteProperties.getText("EmailFromAddress", ("info@" + cpcore.webServer.requestDomain));
                        subject = ("Password Request at " + cpcore.webServer.requestDomain);
                        Message = "";
                        while (cpcore.db.csOk(CS)) {
                            // hint = "170"
                            updateUser = false;
                            if ((Message == "")) {
                                // hint = "180"
                                Message = ("This email was sent in reply to a request at " 
                                            + (cpcore.webServer.requestDomain + " for the username and password associated with this email address. "));
                                Message = (Message + ("If this request was made by you, please return to the login screen and use the following:" + "\r\n"));
                                Message = (Message + "\r\n");
                            }
                            else {
                                // hint = "190"
                                Message = (Message + "\r\n");
                                Message = (Message + ("Additional user accounts with the same email address: " + "\r\n"));
                            }
                            
                            // 
                            //  username
                            // 
                            // hint = "200"
                            Username = cpcore.db.csGetText(CS, "Username");
                            usernameOK = true;
                            if (!allowEmailLogin) {
                                // hint = "210"
                                if ((Username != Username.Trim())) {
                                    // hint = "220"
                                    Username = Username.Trim();
                                    updateUser = true;
                                }
                                
                                if ((Username == "")) {
                                    // hint = "230"
                                    // username = emailName & Int(Rnd() * 9999)
                                    usernameOK = false;
                                    Ptr = 0;
                                    while ((!usernameOK 
                                                && (Ptr < 100))) {
                                        Username = (EMailName + Int((Rnd() * 9999)));
                                        usernameOK = !cpCore.doc.authContext.isLoginOK(cpcore, Username, "test");
                                        Ptr = (Ptr + 1);
                                    }
                                    
                                    // hint = "250"
                                    if (usernameOK) {
                                        updateUser = true;
                                    }
                                    
                                }
                                
                                // hint = "260"
                                Message = (Message + (" username: " 
                                            + (Username + "\r\n")));
                            }
                            
                            // hint = "270"
                            if (usernameOK) {
                                // 
                                //  password
                                // 
                                // hint = "280"
                                Password = cpcore.db.csGetText(CS, "Password");
                                if ((Password.Trim() != Password)) {
                                    // hint = "290"
                                    Password = Password.Trim();
                                    updateUser = true;
                                }
                                
                                // hint = "300"
                                if ((Password == "")) {
                                    // hint = "310"
                                    for (Ptr = 0; (Ptr <= 8); Ptr++) {
                                        // hint = "320"
                                        Index = int.Parse((Rnd() * passwordChrsLength));
                                        Password = (Password + vbMid(passwordChrs, Index, 1));
                                    }
                                    
                                    // hint = "330"
                                    updateUser = true;
                                }
                                
                                // hint = "340"
                                Message = (Message + (" password: " 
                                            + (Password + "\r\n")));
                                returnREsult = true;
                                if (updateUser) {
                                    // hint = "350"
                                    cpcore.db.csSet(CS, "username", Username);
                                    cpcore.db.csSet(CS, "password", Password);
                                }
                                
                                recordCnt = (recordCnt + 1);
                            }
                            
                            cpcore.db.csGoNext(CS);
                        }
                        
                    }
                    
                }
                
            }
            
            // hint = "360"
            if (returnREsult) {
                cpcore.email.send_Legacy(workingEmail, FromAddress, subject, Message, 0, true, false);
                //     main_ClosePageHTML = main_ClosePageHTML & main_GetPopupMessage(app.publicFiles.ReadFile("ccLib\Popup\PasswordSent.htm"), 300, 300, "no")
            }
            
        }
        catch (Exception ex) {
            cpcore.handleException(ex);
            throw;
        }
        
        return returnREsult;
    }
    
    // 
    //  this class must implement System.IDisposable
    //  never throw an exception in dispose
    //  Do not change or add Overridable to these methods.
    //  Put cleanup code in Dispose(ByVal disposing As Boolean).
    // ====================================================================================================
    // 
    protected bool disposed = false;
    
    public DummyClass(coreClass cpCore) {
        this.cpcore = cpCore;
    }
    
    // 
    public void Dispose() {
        //  do not add code here. Use the Dispose(disposing) overload
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    // 
    protected override void Finalize() {
        //  do not add code here. Use the Dispose(disposing) overload
        Dispose(false);
        base.Finalize();
    }
    
    // 
    // ====================================================================================================
    // '' <summary>
    // '' dispose.
    // '' </summary>
    // '' <param name="disposing"></param>
    protected virtual void Dispose(bool disposing) {
        if (!this.disposed) {
            this.disposed = true;
            if (disposing) {
                // If (cacheClient IsNot Nothing) Then
                //     cacheClient.Dispose()
                // End If
            }
            
            // 
            //  cleanup non-managed objects
            // 
        }
        
    }