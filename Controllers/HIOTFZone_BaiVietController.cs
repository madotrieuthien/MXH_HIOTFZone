using MXH_HIOTFZone.Hubs;
using MXH_HIOTFZone.Models;
using MXH_HIOTFZone.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MXH_HIOTFZone.Controllers
{
    public class HIOTFZone_BaiVietController : Controller
    {
        // GET: HIOTFZone_BaiViet
        

        MXH_MiniEntities db = new MXH_MiniEntities();
        ThongBaoService tbService;

        public HIOTFZone_BaiVietController()
        {
            tbService = new ThongBaoService();
        }
        // GET: Form tạo bài đăng
        public ActionResult TaoBaiViet()
            {
                return View();
            }

          
        // POST: TaoBaiViet
        [HttpPost]
        public ActionResult TaoBaiViet(string NoiDung, HttpPostedFileBase AnhBaiDang)
        {
            // Kiểm tra đăng nhập
            if (Session["NguoiDungID"] == null)
                return RedirectToAction("DangNhap", "HIOTFZone_TaiKhoan");

            int userId = (int)Session["NguoiDungID"];

            // 1. Tạo bài đăng mới
            BaiDang bai = new BaiDang
            {
                NguoiDungID = userId,
                NoiDung = NoiDung,
                NgayTao = DateTime.Now
            };

            // 2. Xử lý ảnh nếu có
            if (AnhBaiDang != null && AnhBaiDang.ContentLength > 0)
            {
                string folderPath = Server.MapPath("~/Images/BaiViet/");

                // Tạo thư mục nếu chưa có
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Tạo tên file duy nhất
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AnhBaiDang.FileName);
                string filePath = Path.Combine(folderPath, fileName);

                // Lưu ảnh vào thư mục
                AnhBaiDang.SaveAs(filePath);

                // Lưu tên file vào cột AnhUrl của BaiDang
                bai.AnhUrl = fileName;
            }

            // 3. Lưu bài đăng vào database
            db.BaiDangs.Add(bai);
            db.SaveChanges();

            // 4. Chuyển về trang chính (NewsFeed)
            return RedirectToAction("Index", "HIOTFZone");
        }

        [HttpPost]
        public JsonResult ToggleLike(int baiDangId)
        {
            if (Session["NguoiDungID"] == null)
                return Json(new { success = false, message = "Chưa đăng nhập" });

            int userId = (int)Session["NguoiDungID"];
            var baiDang = db.BaiDangs.Find(baiDangId);
            if (baiDang == null)
                return Json(new { success = false, message = "Bài viết không tồn tại" });

            var thich = db.ThichBaiDangs
                .FirstOrDefault(x => x.BaiDangID == baiDangId && x.NguoiDungID == userId);

            bool liked;

            if (thich == null)
            {
                // ✅ LIKE
                db.ThichBaiDangs.Add(new ThichBaiDang
                {
                    BaiDangID = baiDangId,
                    NguoiDungID = userId,
                    NgayTao = DateTime.Now
                });
                liked = true;

                // ✅ CHỈ THÔNG BÁO KHI LIKE
                if (baiDang.NguoiDungID != userId)
                {
                    tbService.TaoThongBao(
                        userId,
                        baiDang.NguoiDungID,
                        "LIKE",
                        $"{GetUserName(userId)} đã thích bài viết của bạn",
                        $"/HIOTFZone_BaiViet/ChiTiet/{baiDangId}"
                    );
                }
            }
            else
            {
                // ✅ UNLIKE → KHÔNG THÔNG BÁO
                db.ThichBaiDangs.Remove(thich);
                liked = false;
            }

            db.SaveChanges();

            // ✅ Realtime update
            int likeCount = db.ThichBaiDangs.Count(x => x.BaiDangID == baiDangId);
            var context = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager
                .GetHubContext<HIOTFZone_BaiDangHub>();
            context.Clients.All.updateLike(baiDangId, likeCount);

            return Json(new { success = true, liked, likeCount });
        }



        // Hàm lấy tên user
        private string GetUserName(int userId)
        {
            var user = db.NguoiDungs.Find(userId);
            return user != null ? user.TenNguoiDung : "Người dùng";
        }

        public ActionResult ChiTiet(int id)
        {
            // Tìm bài đăng theo ID
            // Include NguoiDung để lấy tên/avatar tác giả
            // Include ThichBaiDangs để kiểm tra xem mình like chưa
            var baiviet = db.BaiDangs
                            .Include("NguoiDung")
                            .Include("ThichBaiDangs")
                            .FirstOrDefault(b => b.BaiDangID == id);

            if (baiviet == null)
            {
                return HttpNotFound("Bài viết không tồn tại hoặc đã bị xóa.");
            }

            return View(baiviet);
        }
        [HttpPost]
        public ActionResult GuiBinhLuan(int baiDangId, string noiDung)
        {
            if (Session["NguoiDungID"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" });
            }

            if (string.IsNullOrWhiteSpace(noiDung))
            {
                return Json(new { success = false, message = "Nội dung không được để trống!" });
            }

            int userId = (int)Session["NguoiDungID"];

            // 1. Lưu vào Database
            var bl = new MXH_HIOTFZone.Models.BinhLuan
            {
                BaiDangID = baiDangId,
                NguoiDungID = userId,
                NoiDung = noiDung,
                NgayTao = DateTime.Now
            };
            db.BinhLuans.Add(bl);
            db.SaveChanges();

            // 2. Lấy thông tin người bình luận để trả về Client hiển thị ngay
            var user = db.NguoiDungs.Find(userId);
            string avatarUrl = "/Images/Avatar/" + (user.Avatar ?? "default.png");

            // 3. Gửi SignalR (Realtime)
            var hubContext = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<MXH_HIOTFZone.Hubs.HIOTFZone_BaiDangHub>();
            hubContext.Clients.All.addNewComment(baiDangId, user.TenNguoiDung, avatarUrl, noiDung);

            // 4. Tạo thông báo (Optional - Nếu muốn)
            var baiDang = db.BaiDangs.Find(baiDangId);
            if (baiDang.NguoiDungID != userId)
            {
                MXH_HIOTFZone.Services.ThongBaoService tbService = new MXH_HIOTFZone.Services.ThongBaoService();
                tbService.TaoThongBao(userId, baiDang.NguoiDungID, "COMMENT", $"{GetUserName(userId)} đã bình luận bài viết của bạn", $"/HIOTFZone_BaiViet/ChiTiet/{baiDangId}");
            }

            return Json(new { success = true });
        }
    }
}