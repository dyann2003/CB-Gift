using Microsoft.AspNetCore.SignalR;
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
            // Gửi thông báo đến client vừa kết nối
            await Clients.Caller.SendAsync("ReceiveMessage", "Hệ thống", "Chào mừng bạn đã kết nối!");
            await base.OnConnectedAsync();
        }

        // Phương thức để tham gia vào một nhóm cụ thể (ví dụ: theo dõi đơn hàng)
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            // Gửi thông báo cho client trong nhóm đó
            await Clients.Group(groupName).SendAsync("ReceiveMessage", "Hệ thống", $"Bạn đã tham gia vào nhóm {groupName}.");
        }
        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }

        public async Task LeaveOrderGroup(string orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }
    }
}