
using System;
using System.Net.Mail;
using System.Net.Mime;
using Contensive.Processor.Models.Domain;
//
namespace Contensive.Processor.Controllers {
    public class EmailSmtpController {
        //
        //====================================================================================================
        /// <summary>
        /// Send email by SMTP. return 'ok' if success, else return a user compatible error message
        /// </summary>
        public static bool send(CoreController core, EmailSendDomainModel email, ref string returnErrorMessage) {
            bool status = false;
            returnErrorMessage = "";
            try {
                string smtpServer = core.siteProperties.getText("SMTPServer", "127.0.0.1");
                SmtpClient client = new SmtpClient(smtpServer);
                MailMessage mailMessage = new MailMessage();
                MailAddress fromAddresses = new MailAddress(email.fromAddress.Trim());
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
                if (core.mockEmail) {
                    //
                    // -- for unit tests, mock interface by adding email to core.mockSmptList
                    core.mockEmailList.Add(new MockEmailClass {
                        email = email
                    });
                    status = true;
                } else {
                    //
                    // -- send email
                    try {
                        LogController.logInfo(core, "sendSmtp, to [" + email.toAddress + "], from [" + email.fromAddress + "], subject [" + email.subject + "], BounceAddress [" + email.bounceAddress + "], replyTo [" + email.replyToAddress + "]");
                        client.Send(mailMessage);
                        status = true;
                    } catch (Exception ex) {
                        string errMsg = "There was an error sending email";
                        Logger.Error(ex, LogController.getMessageLine(core, errMsg, true));
                    }
                }
            } catch (Exception ex) {
                string errMsg = "There was an error configuring smtp server";
                Logger.Error(ex, LogController.getMessageLine(core, errMsg, true));
                throw;
            }
            return status;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
