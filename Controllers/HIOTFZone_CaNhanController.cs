using MXH_HIOTFZone.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using HIOTFZone.Helpers;


namespace MXH_HIOTFZone.Controllers
{
    public class HIOTFZone_CaNhanController : Controller
    {
        MXH_MiniEntities db = new MXH_MiniEntities();
        // GET: HIOTFZone_CaNhan
        public ActionResult TrangCaNhan()
        {
            // Lấy giá trị Session trước
            int userId = 0;
            if (Session["NguoiDungID"] != null)
            {
                userId = (int)Session["NguoiDungID"];
            }
            else
            {
                // Xử lý khi chưa đăng nhập
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // Truy vấn với giá trị đã lấy
            var thongTinCaNhan = db.NguoiDungs
                                   .SingleOrDefault(u => u.NguoiDungID == userId);

            return View(thongTinCaNhan);
        }

        // GET: Chỉnh sửa thông tin
        public ActionResult ChinhSuaThongTin()
        {
            if (Session["NguoiDungID"] == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            int userId = (int)Session["NguoiDungID"];
            var user = db.NguoiDungs.SingleOrDefault(u => u.NguoiDungID == userId);

            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Chỉnh sửa thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaThongTin(NguoiDung model, HttpPostedFileBase Avatar)
        {
            if (Session["NguoiDungID"] == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            int userId = (int)Session["NguoiDungID"];
            var user = db.NguoiDungs.SingleOrDefault(u => u.NguoiDungID == userId);

            if (user == null)
                return HttpNotFound();

            // Danh sách avatar mặc định
            string[] defaultAvatars = new string[] {
                "Chuot.jpg", "Trau.jpg", "Ho.jpg", "Meo.jpg",
                "Rong.jpg", "Ran.jpg", "Ngua.jpg", "De.jpg",
                "Khi.jpg", "Ga.jpg", "Cho.jpg", "Lon.jpg"
            };

            // Cập nhật các trường có thể chỉnh sửa
            user.TenNguoiDung = model.TenNguoiDung;
            user.GioiTinh = model.GioiTinh;
            user.NgaySinh = model.NgaySinh;
            user.DiaChi = model.DiaChi;

            // Xử lý avatar
            if (Avatar != null && Avatar.ContentLength > 0)
            {
                string uploadPath = Server.MapPath("~/Images/Avatar/");

                // Xóa avatar cũ nếu không phải mặc định
                if (!string.IsNullOrEmpty(user.Avatar) && !defaultAvatars.Contains(user.Avatar))
                {
                    string oldFile = Path.Combine(uploadPath, user.Avatar);
                    if (System.IO.File.Exists(oldFile))
                    {
                        System.IO.File.Delete(oldFile);
                    }
                }

                // Lưu avatar mới
                string fileName = Path.GetFileName(Avatar.FileName);
                string fullPath = Path.Combine(uploadPath, fileName);

                // Nếu tên file trùng, có thể thêm timestamp để tránh ghi đè
                if (System.IO.File.Exists(fullPath))
                {
                    string ext = Path.GetExtension(fileName);
                    string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                    fileName = $"{nameOnly}_{DateTime.Now.Ticks}{ext}";
                    fullPath = Path.Combine(uploadPath, fileName);
                }

                Avatar.SaveAs(fullPath);
                user.Avatar = fileName; // lưu vào SQL
            }

            db.SaveChanges();
            Session["NguoiDungID"] = user.NguoiDungID;
            Session["TenNguoiDung"] = user.TenNguoiDung;
            Session["Avatar"] = user.Avatar;
            Session["Email"] = user.Email;

            TempData["ThongBao"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("ChinhSuaThongTin");
        }


        //-----------------------
        //QUẢN LÝ BÀI VIẾT CÁ NHÂN

        // GET: Quản lý bài viết của người dùng
        public ActionResult QuanLyBaiViet()
        {
            if (Session["NguoiDungID"] == null)
                return RedirectToAction("DangNhap", "HIOTFZone_User");

            int userId = (int)Session["NguoiDungID"];
            var dsBaiDang = db.BaiDangs
                               .Where(b => b.NguoiDungID == userId)
                               .OrderByDescending(b => b.NgayTao)
                               .ToList();

            ViewBag.BaiDang = dsBaiDang;
            return View();
        }

        // POST: Xóa bài viết
        [HttpPost]
        public ActionResult XoaBaiDang(int BaiDangID)
        {
            if (Session["NguoiDungID"] == null)
                return RedirectToAction("DangNhap", "HIOTFZone_User");

            int userId = (int)Session["NguoiDungID"];
            var bai = db.BaiDangs.FirstOrDefault(b => b.BaiDangID == BaiDangID && b.NguoiDungID == userId);

            if (bai != null)
            {
                // Xóa ảnh trong thư mục nếu có
                if (!string.IsNullOrEmpty(bai.AnhUrl))
                {
                    string filePath = Server.MapPath("~/Images/BaiViet/" + bai.AnhUrl);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                db.BaiDangs.Remove(bai);
                db.SaveChanges();
            }

            return RedirectToAction("QuanLyBaiViet");
        }
        //-----------------------


        //ĐỔI MẬT KHẨU

        // GET: Hiển thị form đổi mật khẩu
        // GET: Hiển thị form đổi mật khẩu
        public ActionResult DoiMatKhau()
        {
            if (Session["NguoiDungID"] == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            int userId = (int)Session["NguoiDungID"];
            var user = db.NguoiDungs.SingleOrDefault(u => u.NguoiDungID == userId);

            if (user == null)
                return HttpNotFound();

            return View(user); // Truyền model NguoiDung
        }

        // POST: Xử lý đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiMatKhau(FormCollection form)
        {
            if (Session["NguoiDungID"] == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            int userId = (int)Session["NguoiDungID"];
            var user = db.NguoiDungs.SingleOrDefault(u => u.NguoiDungID == userId);

            if (user == null)
            {
                TempData["ThongBao"] = "Người dùng không tồn tại.";
                return RedirectToAction("DoiMatKhau");
            }

            // Lấy dữ liệu từ form
            string matKhauCu = form["MatKhauCu"];
            string matKhauMoi = form["MatKhauMoi"];
            string xacNhanMatKhauMoi = form["XacNhanMatKhauMoi"];

            // Kiểm tra mật khẩu cũ
            if (user.MatKhauHash != PasswordHelper.HashPassword(matKhauCu))
            {
                TempData["ThongBao"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("DoiMatKhau");
            }

            // Kiểm tra xác nhận mật khẩu mới
            if (matKhauMoi != xacNhanMatKhauMoi)
            {
                TempData["ThongBao"] = "Xác nhận mật khẩu mới không trùng khớp.";
                return RedirectToAction("DoiMatKhau");
            }

            // Lưu mật khẩu mới
            user.MatKhauHash = PasswordHelper.HashPassword(matKhauMoi);
            db.SaveChanges();

            TempData["ThongBao"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("DoiMatKhau");
        }

    }
}