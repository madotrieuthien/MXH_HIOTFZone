using System;
using MXH_HIOTFZone.Models;

namespace MXH_HIOTFZone.Services
{
    public class ThongBaoService
    {
        private MXH_MiniEntities db = new MXH_MiniEntities();

        /// <summary>
        /// Tạo thông báo
        /// </summary>
        /// <param name="nguoiGuiId">ID người thực hiện hành động</param>
        /// <param name="nguoiNhanId">ID người nhận</param>
        /// <param name="loaiThongBao">LIKE / KETBAN</param>
        /// <param name="noiDung">Nội dung thông báo</param>
        /// <param name="link">Link chuyển hướng</param>
        public void TaoThongBao(int nguoiGuiId, int nguoiNhanId, string loaiThongBao, string noiDung, string link)
        {
            // Không gửi thông báo cho chính mình
            if (nguoiGuiId == nguoiNhanId) return;

            ThongBao tb = new ThongBao
            {
                NguoiGuiID = nguoiGuiId,
                NguoiNhanID = nguoiNhanId,
                LoaiThongBao = loaiThongBao,
                NoiDung = noiDung,
                Link = link,
                DaDoc = false,
                NgayTao = DateTime.Now
            };

            db.ThongBaos.Add(tb); 
            db.SaveChanges();
        }
    }
}