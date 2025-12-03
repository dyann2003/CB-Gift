using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // ✔ 1. Lấy danh sách thông báo của user hiện tại
        [HttpGet]
        // Mặc định: Trang 1, mỗi lần lấy 10 tin
        public async Task<IActionResult> GetMyNotifications([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            // Gọi service với tham số phân trang
            var list = await _notificationService.GetForUserAsync(userId, pageIndex, pageSize);

            return Ok(list);
        }

        // ✔ 2. Đánh dấu 1 thông báo là đã đọc
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok(new { message = "Marked as read" });
        }

        // ✔ 3. Tạo thông báo (cho test / tạo sự kiện)
        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _notificationService.CreateAndSendNotificationAsync(
                dto.UserId,
                dto.Message,
                dto.RedirectUrl ?? "/"
            );

            return Ok(new { message = "Notification created and sent" });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var count = await _notificationService.CountUnreadForUserAsync(userId);
            return Ok(new { unreadCount = count });
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }

        public class CreateNotificationDto
        {
            public string UserId { get; set; } = default!;
            public string Message { get; set; } = default!;
            public string? RedirectUrl { get; set; }
        }
    }
}
