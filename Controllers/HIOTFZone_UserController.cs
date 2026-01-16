using HIOTFZone.Helpers;
using MXH_HIOTFZone.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace MXH_HIOTFZone.Controllers
{
    public class HIOTFZone_UserController : Controller
    {
        MXH_MiniEntities db = new MXH_MiniEntities();
        // GET: HIOTFZone_User
        public ActionResult DangNhap()
        {
            return View();
        }

        // POST: Đăng nhập
        [HttpPost]
        public ActionResult DangNhap(string Email, string MatKhau)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(MatKhau))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ thông tin";
                return View();
            }

            string hashedPassword = PasswordHelper.HashPassword(MatKhau);

            var user = db.NguoiDungs.FirstOrDefault(u => u.Email == Email && u.MatKhauHash == hashedPassword );

            if (user != null)
            {
                if(user.TrangThaiTaiKhoan == true)
                {
                    ViewBag.Error = "Tài khoản của bạn đã bị khóa.";
                    return View();
                }
                // Cập nhật trạng thái online
                user.TrangThaiOnline = true;
                db.SaveChanges();

                // Lưu thông tin user vào session
                Session["NguoiDungID"] = user.NguoiDungID;
                Session["TenNguoiDung"] = user.TenNguoiDung;
                Session["Avatar"] = user.Avatar;
                Session["Email"] = user.Email;
                return RedirectToAction("Index", "HIOTFZone");
            }
            else
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng";
                return View();
            }
        }

        //---------------------------------------------------------------------------------------
        private string LayConGiap(DateTime? ngaySinh)
        {
            if (ngaySinh == null) return "Unknown.jpg";

            int namSinh = ngaySinh.Value.Year;
            int canChi = (namSinh - 4) % 12;
            switch (canChi)
            {
                case 0: return "Chuot.jpg";
                case 1: return "Trau.jpg";
                case 2: return "Ho.jpg";
                case 3: return "Meo.jpg";
                case 4: return "Rong.jpg";
                case 5: return "Ran.jpg";
                case 6: return "Ngua.jpg";
                case 7: return "De.jpg";
                case 8: return "Khi.jpg";
                case 9: return "Ga.jpg";
                case 10: return "Cho.jpg";
                case 11: return "Lon.jpg";
                default: return "Unknown.jpg";
            }
        }



        //-------------------------------------------------------------------------------------


        //Đăng ký tài khoản
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangKy(string TenKH, string GioiTinh, DateTime? NgaySinh, string DiaChi,
                           string Email, string MatKhau, string XacNhanMatKhau)
        {
            if (string.IsNullOrEmpty(TenKH) || string.IsNullOrEmpty(Email) ||
                string.IsNullOrEmpty(MatKhau) || string.IsNullOrEmpty(XacNhanMatKhau))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ thông tin bắt buộc.";
                return View();
            }

            if (MatKhau != XacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu và xác nhận mật khẩu không khớp.";
                return View();
            }

            if (db.NguoiDungs.Any(u => u.Email == Email))
            {
                ViewBag.Error = "Email đã được sử dụng.";
                return View();
            }

            try
            {
                string hashedPassword = PasswordHelper.HashPassword(MatKhau);

                NguoiDung newUser = new NguoiDung
                {
                    TenNguoiDung = TenKH,
                    Email = Email,
                    MatKhauHash = hashedPassword,
                    GioiTinh = GioiTinh,
                    NgaySinh = NgaySinh,
                    DiaChi = DiaChi,
                    NgayTao = DateTime.Now
                };

                db.NguoiDungs.Add(newUser);
                db.SaveChanges(); // để có NguoiDungID
                // Gửi email thông báo đăng ký thành công
                GuiMailThongBaoDangKy(Email, TenKH);

                // --- Tạo thư mục riêng cho user ---
                //string rootFolder = Server.MapPath("~/NguoiDung");
                //if (!Directory.Exists(rootFolder))
                //    Directory.CreateDirectory(rootFolder);

                //string userFolder = Path.Combine(rootFolder, newUser.NguoiDungID.ToString());
                //Directory.CreateDirectory(userFolder);

                //string avatarFolder = Path.Combine(userFolder, "Avatar");
                //Directory.CreateDirectory(avatarFolder);
                //Directory.CreateDirectory(Path.Combine(userFolder, "AnhBaiViet"));
                //Directory.CreateDirectory(Path.Combine(userFolder, "AnhBia"));

                // --- Gán avatar mặc định dựa theo tuổi/con giáp ---
                string defaultAvatar = LayConGiap(NgaySinh); // ví dụ trả về "Trau.png"

                // Copy file avatar mặc định vào thư mục của user
                string sourceFile = Server.MapPath("~/Images/Avatar/" + defaultAvatar);
                //string destFile = Path.Combine(avatarFolder, defaultAvatar);
                //if (System.IO.File.Exists(sourceFile))
                //{
                //    System.IO.File.Copy(sourceFile, destFile, true);
                //}

                // Lưu đường dẫn avatar vào DB (có thể lưu tên file)
                newUser.Avatar = defaultAvatar;
                db.SaveChanges();


                ViewBag.Success = "Đăng ký thành công!";
                return RedirectToAction("DangNhap", "HIOTFZone_User");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        public ActionResult DangXuat()
        {
            // Lấy ID user từ session
            int? id = Session["NguoiDungID"] as int?;

            if (id != null)
            {
                var user = db.NguoiDungs.FirstOrDefault(u => u.NguoiDungID == id );
                if (user != null)
                {

                    user.TrangThaiOnline = false;
                    db.SaveChanges();
                }
            }
            Session.Clear();
            return RedirectToAction("DangNhap", "HIOTFZone_User");
        }
        private void GuiMailThongBaoDangKy(string email, string hoTen)
        {
            string fromEmail = "2324802010127@student.tdmu.edu.vn";      // đổi thành email thật
            string fromPassword = "ajfh bzvc cmvn iakw";       // app password (không phải mật khẩu Gmail thường)
            string subject = "Xác nhận đăng ký tài khoản - Mạng xã hội Mini HIOTFZone";
            string body = $"Xin chào {hoTen},\n\nBạn đã đăng ký tài khoản thành công tại Mạng xã hội Mini HIOTFZone.\nChúc bạn trải nghiệm vui vẻ!\n\nTrân trọng,\nĐội ngũ HIOTFZone.";

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(fromEmail, "Mạng xã hội Mini HIOTFZone");
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