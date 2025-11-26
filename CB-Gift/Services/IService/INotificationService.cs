using CB_Gift.Models;

namespace CB_Gift.Services.IService
{
    public interface INotificationService
    {
        Task CreateAndSendNotificationAsync(string userId, string message, string redirectUrl);
        Task<List<Notification>> GetForUserAsync(string userId, int top = 50);
        Task MarkAsReadAsync(int id, string userId);
        Task<int> CountUnreadForUserAsync(string userId);
    }
}
