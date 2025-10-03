using CB_Gift.Services.Email;
using System.Net;
using System.Net.Mail;

namespace CB_Gift.Services.Email
{

    public class SmtpEmailSender : IEmailSender
        {
            private readonly IConfiguration _config;
            public SmtpEmailSender(IConfiguration config)
            {
                _config = config;
            }

            public async Task SendAsync(string toEmail, string subject, string body)
            {
                var smtpServer = _config["Email:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var senderEmail = _config["Email:Sender"];
                var senderPassword = _config["Email:Password"];

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail!, "CB-Gift Support"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }
    
}
