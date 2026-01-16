using MXH_HIOTFZone.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace MXH_HIOTFZone.Controllers
{
    public class HIOTFZone_AdminController : Controller
    {
        MXH_MiniEntities db = new MXH_MiniEntities();
        // GET: HIOTFZone_Admin
        // GET: Admin/DangNhap
        public ActionResult DangNhap()
        {
            return View();
        }
        // POST: Admin/DangNhap
        [HttpPost]
        public ActionResult DangNhap(string TaiKhoan, string MatKhau)
        {
            if (string.IsNullOrEmpty(TaiKhoan) || string.IsNullOrEmpty(MatKhau))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            // Kiểm tra tài khoản và mật khẩu (không mã hóa)
            var admin = db.Admins.FirstOrDefault(a =>
                a.TaiKhoan == TaiKhoan &&
                a.MatKhau == MatKhau
            );

            if (admin != null)
            {
                // Lưu ID admin vào session
                Session["AdminID"] = admin.AdminID;
                Session["AdminHoTen"] = admin.HoTen;
                Session["AdminTaiKhoan"] = admin.TaiKhoan;

                return RedirectToAction("Admin", "HIOTFZOne");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
            return View();
        }

        // Đăng xuất
        public ActionResult DangXuat()
        {
            Session.Clear();
            return RedirectToAction("DangNhap");
        }

        private bool CheckAdmin()
        {
            return Session["AdminID"] != null;
        }

        // ==============================
        // 1. QUẢN LÝ TÀI KHOẢN NGƯỜI DÙNG
        // ==============================
        public ActionResult QuanLyTaiKhoan(string keyword, string status, string warning)
        {
            if (!CheckAdmin())
            {
                return RedirectToAction("DangNhap", "HIOTFZone_Admin");
            }

            var query = db.NguoiDungs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(u => u.TenNguoiDung.Contains(keyword));
            }

            if (!string.IsNullOrEmpty(status)) { 
                if(status == "active")
                {
                    query = query.Where(u => u.TrangThaiTaiKhoan != true);
                }   
                else if(status == "locked")
                {
                    query = query.Where(u => u.TrangThaiTaiKhoan == true);
                }    
            }

            if (!string.IsNullOrEmpty(warning)) {
                int wCount;
                if(int.TryParse(warning, out wCount))
                {
                    if(wCount >= 3)
                    {
                        query = query.Where(u => u.SoLanCanhCao >= 3);
                    }
                    else
                    {
                        query = query.Where(u => u.SoLanCanhCao == wCount);
                    }
                }    
            }
            var result = query.OrderByDescending(u => u.NgayTao).ToList();
            return View(result);
        }
        // Cảnh báo người dùng
        public ActionResult CanhBao(int id)
        {
            var nd = db.NguoiDungs.Find(id);
            if (nd != null)
            {
                nd.SoLanCanhCao += 1;
                db.SaveChanges();
                GuiMailCanhCao(nd.Email, nd.TenNguoiDung, nd.SoLanCanhCao);
            }

            return RedirectToAction("QuanLyTaiKhoan");
        }

        // Khóa tài khoản
        public ActionResult Khoa(int id)
        {
            var nd = db.NguoiDungs.Find(id);
            if (nd != null)
            {
                nd.TrangThaiTaiKhoan = true;
                db.SaveChanges();
                GuiMailKhoaTaiKhoan(nd.Email, nd.TenNguoiDung);
            }
            return RedirectToAction("QuanLyTaiKhoan");  
        }

        // Mở khóa tài khoản
        public ActionResult MoKhoa(int id)
        {  
            var nd = db.NguoiDungs.Find(id);
            if (nd != null)
            {
                nd.SoLanCanhCao = 0;
                nd.TrangThaiTaiKhoan = false;
                db.SaveChanges();
                GuiMailMoKhoaTaiKhoan(nd.Email, nd.TenNguoiDung);
            }
            return RedirectToAction("QuanLyTaiKhoan");
        }

        // ==============================
        // 2. QUẢN LÝ BÀI VIẾT
        // ==============================
        public ActionResult QuanLyBaiViet()
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "Admin");

            var ds = db.BaiDangs
                       .OrderByDescending(b => b.NgayTao)
                       .ToList();
            return View(ds);
        }

        // Xóa bài viết
        public ActionResult XoaBaiViet(int id)
        {
            var bai = db.BaiDangs.Find(id);
            if (bai != null)
            {
                db.BaiDangs.Remove(bai);
                db.SaveChanges();
            }

            return RedirectToAction("QuanLyBaiViet");
        }

        public ActionResult ChiTietBaiViet(int id)
        {
            var bai = db.BaiDangs
                .Include("NguoiDung")
                .Include("BinhLuans.NguoiDung")
                .FirstOrDefault(x => x.BaiDangID == id);

            if (bai == null)
                return HttpNotFound();

            return View(bai);
        }

        private void GuiMailCanhCao(string email, string hoTen, int lan)
        {
            string fromEmail = "2324802010127@student.tdmu.edu.vn";
            string fromPassword = "ajfh bzvc cmvn iakw";   // App password Gmail
            string subject = $"Cảnh cáo lần {lan} - Vi phạm quy tắc cộng đồng HIOTFZone";

            string body =
                $"Xin chào {hoTen},\n\n" +
                $"Tài khoản của bạn đã bị cảnh cáo **lần {lan}** vì vi phạm quy tắc cộng đồng.\n" +
                $"Vui lòng tuân thủ quy định để tránh bị khóa tài khoản.\n\n" +
                $"Trân trọng,\n" +
                $"Đội ngũ quản trị HIOTFZone.";

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(fromEmail, "HIOTFZone - Cảnh cáo vi phạm");
            mail.To.Add(email);
            mail.Subject = subject;
            mail.Body = body;

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
            smtp.EnableSsl = true;

            smtp.Send(mail);
        }
                private void GuiMailKhoaTaiKhoan(string email, string hoTen)
                {
                        string fromEmail = "2324802010127@student.tdmu.edu.vn";    
                        string fromPassword = "ajfh bzvc cmvn iakw";   // App password Gmail

                        string subject = "Tài khoản của bạn đã bị khóa - HIOTFZone";

                        string body =
                            $"Xin chào {hoTen},\n\n" +
                            $"Chúng tôi xin thông báo rằng tài khoản của bạn trên **Mạng xã hội Mini HIOTFZone** đã bị **khóa tạm thời/vĩnh viễn** do vi phạm quy tắc cộng đồng.\n\n" +
                            $"Vui lòng liên hệ đội ngũ hỗ trợ nếu bạn cho rằng đây là sự nhầm lẫn.\n" +
                            $"Email hỗ trợ: support@hiotfzone.com hoặc madotrieuthien@gmail.com\n\n" +
                            $"Trân trọng,\n" +
                            $"Đội ngũ quản trị HIOTFZone.";

                        MailMessage mail = new MailMessage();
                        mail.From = new MailAddress(fromEmail, "HIOTFZone - Quản trị hệ thống");
                        mail.To.Add(email);
                        mail.Subject = subject;
                        mail.Body = body;

                        SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                        smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                        smtp.EnableSsl = true;

                        smtp.Send(mail);
                 }
        private void GuiMailMoKhoaTaiKhoan(string email, string hoTen)
        {
            string fromEmail = "2324802010127@student.tdmu.edu.vn";
            string fromPassword = "ajfh bzvc cmvn iakw";   // App password Gmail

            string subject = "Tài khoản của bạn đã được mở khóa - HIOTFZone";

            string body =
                $"Xin chào {hoTen},\n\n" +
                $"Chúng tôi xin thông báo rằng tài khoản của bạn trên **Mạng xã hội Mini HIOTFZone** đã được **mở khóa** và bạn có thể đăng nhập lại bình thường.\n\n" +
                $"Nếu bạn cần hỗ trợ thêm, vui lòng liên hệ đội ngũ hỗ trợ.\n" +
                $"Email hỗ trợ: support@hiotfzone.com\n\n" +
                $"Chúc bạn trải nghiệm vui vẻ trở lại!\n\n" +
                $"Trân trọng,\n" +
                $"Đội ngũ quản trị HIOTFZone.";

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(fromEmail, "HIOTFZone - Quản trị hệ thống");
            mail.To.Add(email);
            mail.Subject = subject;
            mail.Body = body;

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
            smtp.EnableSsl = true;

            smtp.Send(mail);
        }

        
      

    }
}