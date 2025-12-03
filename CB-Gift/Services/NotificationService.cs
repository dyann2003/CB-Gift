using CB_Gift.Data;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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
            // 1. Save to DB
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

            // 2. Push SignalR
            var userGroupName = $"user_{userId}";

            await _hubContext.Clients.Group(userGroupName).SendAsync(
                "NewNotification",
                notification
            );
        }

        public async Task<List<Notification>> GetForUserAsync(string userId, int pageIndex, int pageSize)
        {
            // Validate để tránh lỗi nếu FE truyền số âm
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize <= 0) pageSize = 10;

            return await _context.Notifications
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt) // Sắp xếp mới nhất lên đầu
                .Skip((pageIndex - 1) * pageSize)      // Bỏ qua (Trang hiện tại - 1) * Số lượng
                .Take(pageSize)                        // Chỉ lấy đúng số lượng yêu cầu
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int id, string userId)
        {
            var n = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (n != null)
            {
                n.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            // Tìm tất cả thông báo chưa đọc của user đó
            var unreadNotifications = await _context.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                // Cập nhật trạng thái
                foreach (var noti in unreadNotifications)
                {
                    noti.IsRead = true;
                }

                // Lưu DB
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> CountUnreadForUserAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }
    }
}
