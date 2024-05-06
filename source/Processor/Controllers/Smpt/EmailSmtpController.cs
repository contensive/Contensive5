
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Net.Mail;
using System.Net.Mime;
//
namespace Contensive.Processor.Controllers {
    public class EmailSmtpController {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// Send email by SMTP. return 'ok' if success, else return a user compatible error message
        /// </summary>
        public static bool send(CoreController core, EmailSendRequest email, ref string returnErrorMessage) {
            bool status = false;
            returnErrorMessage = "";
            try {
                string smtpServer = core.siteProperties.getText("SMTPServer", "127.0.0.1");
                SmtpClient client = new(smtpServer) {
                    EnableSsl = false,
                    UseDefaultCredentials = false
                };
                MailAddress fromAddresses = new(email.fromAddress.Trim());
                MailMessage mailMessage = new MailMessage {
                    From = fromAddresses,
                    Subject = email.subject
                };
                mailMessage.To.Add(new MailAddress(email.toAddress.Trim()));
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
                    ContentType mimeType = new("text/html");
                    AlternateView alternate = AlternateView.CreateAlternateViewFromString(email.htmlBody, mimeType);
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
                        logger.Info($"{core.logCommonMessage},sendSmtp, to [" + email.toAddress + "], from [" + email.fromAddress + "], subject [" + email.subject + "], BounceAddress [" + email.bounceAddress + "], replyTo [" + email.replyToAddress + "]");
                        client.Send(mailMessage);
                        status = true;
                    } catch (Exception ex) {
                        returnErrorMessage = "There was an smtp error sending the email";
                        Logger.Error(ex, core.logCommonMessage + "," + returnErrorMessage);
                    }
                }
            } catch (Exception ex) {
                string errMsg = "There was an error configuring the smtp server";
                Logger.Error(ex, $"{core.logCommonMessage},{errMsg}");
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
