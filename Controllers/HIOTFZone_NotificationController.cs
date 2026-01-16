using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MXH_HIOTFZone.Models;
using MXH_HIOTFZone.Services;

namespace MXH_HIOTFZone.Controllers
{
    public class HIOTFZone_NotificationController : Controller
    {
        private MXH_MiniEntities db = new MXH_MiniEntities();
        private ThongBaoService tbService;

        public HIOTFZone_NotificationController()
        {
            tbService = new ThongBaoService();
        }

        // API trả số thông báo chưa đọc (dùng cho badge)
        [HttpGet]
        public JsonResult UnreadCount()
        {
            if (Session["NguoiDungID"] == null)
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);

            int userId = (int)Session["NguoiDungID"];
            int count = db.ThongBaos.Count(n => n.NguoiNhanID == userId && (n.DaDoc == false || n.DaDoc == null));
            return Json(new { count = count }, JsonRequestBehavior.AllowGet);
        }

        // Trang danh sách thông báo
        public ActionResult Index()
        {
            if (Session["NguoiDungID"] == null)
                return RedirectToAction("DangNhap", "HIOTFZone_TaiKhoan");

            int userId = (int)Session["NguoiDungID"];

            // Lấy tất cả thông báo, sắp xếp mới nhất
            var list = db.ThongBaos
                         .Where(n => n.NguoiNhanID == userId)
                         .OrderByDescending(n => n.NgayTao)
                         .ToList();

            // Đánh dấu đã đọc những thông báo chưa đọc
            var unread = list.Where(n => n.DaDoc == false || n.DaDoc == null).ToList();
            if (unread.Any())
            {
                foreach (var n in unread)
                    n.DaDoc = true;

                db.SaveChanges();
            }

            return View(list);
        }
        public ActionResult GetNotificationPopup()
        {
            if (Session["NguoiDungID"] == null) return Content("");

            int userId = (int)Session["NguoiDungID"];

            var list = db.ThongBaos
                         // --- DÒNG QUAN TRỌNG NHẤT ---
                         // Bạn cần include bảng Người dùng tương ứng với NguoiGuiID.
                         // Trong EF, nếu có 2 khóa ngoại, thường nó sẽ đặt tên là NguoiDung1 (hoặc NguoiDung)
                         .Include("NguoiDung1")
                         // -----------------------------
                         .Where(n => n.NguoiNhanID == userId)
                         .OrderByDescending(n => n.NgayTao)
                         .Take(10)
                         .ToList();

            return PartialView("_NotificationListPartial", list);
        }

        // 2. API Đánh dấu đã đọc (Gọi khi mở popup)
        [HttpPost]
        public JsonResult MarkAsRead()
        {
            if (Session["NguoiDungID"] != null)
            {
                int userId = (int)Session["NguoiDungID"];
                var unread = db.ThongBaos.Where(n => n.NguoiNhanID == userId && (n.DaDoc == false || n.DaDoc == null)).ToList();

                if (unread.Any())
                {
                    foreach (var item in unread)
                    {
                        item.DaDoc = true;
                    }
                    db.SaveChanges();
                }
            }
            return Json(new { success = true });
        }

    }
}