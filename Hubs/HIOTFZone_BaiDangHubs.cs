using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace MXH_HIOTFZone.Hubs
{
    public class HIOTFZone_BaiDangHub : Hub
    {
        // Gọi từ client để broadcast số like mới
        public void LikeUpdated(int baiDangId, int likeCount)
        {
            // Gửi cho tất cả client update số like của bài viết
            Clients.All.updateLike(baiDangId, likeCount);
        }
    }
}