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
//
using System.Net.Mail;
using System.Net.Mime;
using System.IO;
//
namespace Contensive.Processor.Controllers {
    public class EmailSmtpController {
        //
        //====================================================================================================
        /// <summary>
        /// Send email by SMTP. return 'ok' if success, else return a user compatible error message
        /// </summary>
        public static bool send(CoreController core, EmailController.EmailClass email, ref string returnErrorMessage) {
            bool status = false;
            returnErrorMessage = "";
            try {
                string smtpServer = core.siteProperties.getText("SMTPServer", "127.0.0.1");
                SmtpClient client = new SmtpClient(smtpServer);
                MailMessage mailMessage = new MailMessage();
                MailAddress fromAddresses = new MailAddress(email.fromAddress.Trim());
                Attachment data = null;
                ContentDisposition disposition = null;
                ContentType mimeType = null;
                AlternateView alternate = null;
                //
                mailMessage.From = fromAddresses;
                mailMessage.To.Add(new MailAddress(email.toAddress.Trim()));
                mailMessage.Subject = email.subject;
                client.EnableSsl = false;
                client.UseDefaultCredentials = false;
                //
                if ((string.IsNullOrEmpty(email.textBody)) && (!string.IsNullOrEmpty(email.htmlBody))) {
                    //
                    // html only
                    mailMessage.Body = email.htmlBody;
                    mailMessage.IsBodyHtml = true;
                } else if ((!string.IsNullOrEmpty(email.textBody)) && (string.IsNullOrEmpty(email.htmlBody))) {
                    //
                    // text body only
                    mailMessage.Body = email.textBody;
                    mailMessage.IsBodyHtml = false;
                } else {
                    //
                    // both html and text
                    mailMessage.Body = email.textBody;
                    mailMessage.IsBodyHtml = false;
                    mimeType = new System.Net.Mime.ContentType("text/html");
                    alternate = AlternateView.CreateAlternateViewFromString(email.htmlBody, mimeType);
                    mailMessage.AlternateViews.Add(alternate);
                }
                //
                // Create  the file attachment for this e-mail message.
                //if (!string.IsNullOrEmpty(AttachmentFilename)) {
                //    data = new Attachment(AttachmentFilename, MediaTypeNames.Application.Octet);
                //    disposition = data.ContentDisposition;
                //    disposition.CreationDate = System.IO.File.GetCreationTime(AttachmentFilename);
                //    disposition.ModificationDate = System.IO.File.GetLastWriteTime(AttachmentFilename);
                //    disposition.ReadDate = System.IO.File.GetLastAccessTime(AttachmentFilename);
                //    mailMessage.Attachments.Add(data);
                //}
                if (core.mockEmail) {
                    //
                    // -- for unit tests, mock interface by adding email to core.mockSmptList
                    core.mockEmailList.Add(new CoreController.MockEmailClass() {
                        email = email
                    });
                    status = true;
                } else {
                    //
                    // -- send email
                    try {
                        LogController.logInfo(core, "sendSmtp, to [" + email.toAddress + "], from [" + email.fromAddress + "], subject [" + email.subject + "], BounceAddress [" + email.BounceAddress + "], replyTo [" + email.replyToAddress + "]");
                        client.Send(mailMessage);
                        status = true;
                    } catch (Exception ex) {
                        returnErrorMessage = "There was an error sending email [" + ex.ToString() + "]";
                        LogController.logError(core, returnErrorMessage);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, "There was an error configuring smtp server ex [" + ex.ToString() + "]");
                throw;
            }
            return status;
        }
    }
}