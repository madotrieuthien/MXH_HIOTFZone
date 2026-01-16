using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MXH_HIOTFZone.Hubs
{
    [HubName("chat")]
    public class HIOTFZone_Hub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }
        // Khi client kết nối lần đầu, gửi thông tin về client đó
        public void Connect(string name)
        {
            // Gọi phương thức connect(name) trên client hiện tại
            Clients.Caller.connect(name);
        }

        // Gửi tin nhắn đến tất cả client
        public void Message(string name, string message)
        {
            // Gọi phương thức Message(name, message) trên tất cả client
            Clients.All.Message(name, message);
        }

        // ==================== PHẦN CHAT THEO PHÒNG RIÊNG ====================

        // Tham gia 1 phòng
        public void JoinRoom(string roomName, string userName)
        {
            // Thêm connection hiện tại vào group tên roomName
            Groups.Add(Context.ConnectionId, roomName);

            // Thông báo cho tất cả người trong phòng về việc tham gia
            //Clients.Group(roomName).ReceiveMessage("System", $"{userName} đã tham gia phòng {roomName}");
        }

        // Rời phòng
        public void LeaveRoom(string roomName, string userName)
        {
            // Xóa connection hiện tại khỏi group
            Groups.Remove(Context.ConnectionId, roomName);

            // Thông báo cho tất cả người trong phòng về việc rời phòng
            Clients.Group(roomName).ReceiveMessage("System", $"{userName} đã rời phòng {roomName}");
        }

        // Gửi tin nhắn đến 1 phòng cụ thể
        public void SendMessageToRoom(string roomName, string userName, string message, string avatar)
        {

            // Gọi phương thức ReceiveMessage trên tất cả client trong group roomName
            Clients.Group(roomName).ReceiveMessage(userName, message, avatar);
        }
    }
}