namespace CB_Gift.Services.IService
{
    public interface INotificationService
    {
        Task CreateAndSendNotificationAsync(string userId, string message, string redirectUrl);
    }
}
