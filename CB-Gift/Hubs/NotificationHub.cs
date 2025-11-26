using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CB_Gift.Hubs 
{
    public class NotificationHub : Hub
    {
        // Phương thức này có thể được gọi từ client
        // Ví dụ: gửi thông báo đến tất cả client
        public async Task SendMessageToAll(string user, string message)
        {
            // "ReceiveMessage" là tên sự kiện mà client sẽ lắng nghe
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // Phương thức này được gọi khi một client mới kết nối
        public override async Task OnConnectedAsync()
        {
            // Lấy UserId từ token xác thực của người dùng đang kết nối
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId) && Context.ConnectionId != null)
            {
                // Thêm kết nối này vào group của riêng user đó
                // Ví dụ group name: "user_xxxx-yyyy-zzzz"
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        // Phương thức để tham gia vào một nhóm cụ thể (ví dụ: theo dõi đơn hàng)
        public async Task JoinGroup(string groupName)
        {
            if (Context.ConnectionId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await Clients.Caller.SendAsync("ReceiveMessage", "Hệ thống", $"Bạn đã tham gia vào nhóm {groupName}.");
            }
        }

        public async Task JoinOrderGroup(string orderId)
        {
            if (Context.ConnectionId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }

        public async Task LeaveOrderGroup(string orderId)
        {
            if (Context.ConnectionId != null)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }
    }
}