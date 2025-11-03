using CB_Gift.Data;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.SignalR;

namespace CB_Gift.Services
{
    public class NotificationService : INotificationService
    {
        private readonly CBGiftDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(CBGiftDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateAndSendNotificationAsync(string userId, string message, string redirectUrl)
        {
            // ✅ 1. Tạo và lưu thông báo vào Database
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                RedirectUrl = redirectUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // ✅ 2. Gửi thông báo real-time đến đúng người dùng
            var userGroupName = $"user_{userId}";
            await _hubContext.Clients.Group(userGroupName).SendAsync(
                "NewNotification",
                notification // Gửi đi object notification vừa tạo
            );
        }
    }
}
