namespace CB_Gift.Services.IService
{

        public interface IEmailSender
        {
            Task SendAsync(string toEmail, string subject, string body);
        }
    
}
