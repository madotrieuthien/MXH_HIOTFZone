using MXH_HIOTFZone.Models;
using MXH_HIOTFZone.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MXH_HIOTFZone.Controllers
{
    public class HIOTFZone_BanBeController : Controller
    {
        // GET: HIOTFZone_BanBe
        MXH_MiniEntities db = new MXH_MiniEntities();
        //tạo viewBanBe
        public ActionResult BanBe()
        {
            int? myID = Session["NguoiDungID"] as int?;

            // Lời mời kết bạn đang chờ
            var loiMoiRaw = (from lm in db.LoiMoiKetBans
                             join nd in db.NguoiDungs on lm.NguoiGuiID equals nd.NguoiDungID
                             where lm.NguoiNhanID == myID && lm.TrangThai == "Pending"
                             select new
                             {
                                 lm.ID,
                                 nd.NguoiDungID,
                                 nd.TenNguoiDung,
                                 nd.Avatar,
                                 nd.GioiTinh,
                                 nd.DiaChi
                             }).ToList();

            var dsNguoiGui = loiMoiRaw.Select(x => new NguoiDung
            {
                NguoiDungID = x.NguoiDungID,
                TenNguoiDung = x.TenNguoiDung,
                Avatar = x.Avatar,
                GioiTinh = x.GioiTinh,
                DiaChi = x.DiaChi
            }).ToList();

            ViewBag.LoiMoiID = loiMoiRaw.ToDictionary(x => x.NguoiDungID, x => x.ID);
            ViewBag.LoiMoi = dsNguoiGui;
            // Lấy danh sách Giới tính duy nhất
            ViewBag.Genders = db.NguoiDungs
                                .Select(u => u.GioiTinh)
                                .Distinct()
                                .ToList();

            // Lấy danh sách Địa chỉ duy nhất
            ViewBag.Addresses = db.NguoiDungs
                                  .Select(u => u.DiaChi)
                                  .Distinct()
                                  .ToList();

            var allUsers = db.NguoiDungs
    .Where(u => u.NguoiDungID != myID &&
           !db.LoiMoiKetBans.Any(x =>
               (x.NguoiGuiID == myID && x.NguoiNhanID == u.NguoiDungID) ||
               (x.NguoiGuiID == u.NguoiDungID && x.NguoiNhanID == myID)) &&
           !db.BanBes.Any(b =>
               (b.NguoiDungID1 == myID && b.NguoiDungID2 == u.NguoiDungID) ||
               (b.NguoiDungID2 == myID && b.NguoiDungID1 == u.NguoiDungID))
    ).ToList();

            // luôn gán, kể cả khi allUsers rỗng
            ViewBag.GoiYBanBe = allUsers ?? new List<NguoiDung>();



            var dsBanBe = db.BanBes
                .Where(x => x.NguoiDungID1 == myID || x.NguoiDungID2 == myID)
                .Select(x => x.NguoiDungID1 == myID ? x.NguoiDungID2 : x.NguoiDungID1)
                .Distinct()
                .ToList();

            var dsNguoiDung = db.NguoiDungs
                .Where(u => dsBanBe.Contains(u.NguoiDungID))
                .ToList();

            // Truyền vào ViewBag
            ViewBag.BanBe = dsNguoiDung;
            return View();
        }
        //tạo viewTimKiem
        public ActionResult TimKiem(string keyword = "", string gender = "", string location = "")
        {
            int? myID = Session["NguoiDungID"] as int?;
            var query = db.NguoiDungs.Where(u => u.NguoiDungID != myID);

            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(u =>
                    u.TenNguoiDung.ToLower().Contains(kw) ||
                    u.DiaChi.ToLower().Contains(kw));
            }

            if (!string.IsNullOrEmpty(gender))
                query = query.Where(u => u.GioiTinh == gender);
            if (!string.IsNullOrEmpty(location))
            {
                // loại bỏ "Tỉnh " nếu có trong value gửi từ form
                var locationValue = location.Replace("Tỉnh ", "").Trim();

                query = query.Where(u => u.DiaChi.Contains(locationValue));
            }


            var ds = query.ToList();
            return View(ds);
        }
        public ActionResult XoaBanBe(int id)
        {
            int myID = (int)Session["NguoiDungID"];

            var banBe = db.BanBes.FirstOrDefault(b =>
                (b.NguoiDungID1 == myID && b.NguoiDungID2 == id) ||
                (b.NguoiDungID2 == myID && b.NguoiDungID1 == id)
            );

            if (banBe != null)
            {
                db.BanBes.Remove(banBe);

                // Nếu muốn xóa luôn lời mời liên quan (tùy yêu cầu)
                var loiMoi = db.LoiMoiKetBans.FirstOrDefault(l =>
                    (l.NguoiGuiID == myID && l.NguoiNhanID == id) ||
                    (l.NguoiGuiID == id && l.NguoiNhanID == myID)
                );
                if (loiMoi != null)
                    db.LoiMoiKetBans.Remove(loiMoi);

                db.SaveChanges();
            }

            return RedirectToAction("BanBe");
        }
        public ActionResult GuiLoiMoiKetBan(int id)
        {
            int myID = (int)Session["NguoiDungID"];
            bool exists = db.LoiMoiKetBans.Any(x =>
                (x.NguoiGuiID == myID && x.NguoiNhanID == id) ||  // Mình đã gửi cho đối phương
                (x.NguoiGuiID == id && x.NguoiNhanID == myID));   // Đối phương đã gửi cho mình

            if (!exists)
            {
                db.LoiMoiKetBans.Add(new LoiMoiKetBan
                {
                    NguoiGuiID = myID,
                    NguoiNhanID = id,
                    TrangThai = "Pending",
                    NgayGuiKetBan = DateTime.Now
                });

                db.SaveChanges();
                // Sau khi add và save
                ThongBaoService tbService = new ThongBaoService();
                tbService.TaoThongBao(
                    myID,
                    id,
                    "FRIEND_REQUEST",
                    $"{GetUserName(myID)} đã gửi lời mời kết bạn",
                    "/HIOTFZone_BanBe/BanBe?tab=requests"
                );
            }
            return RedirectToAction("BanBe");
        }
        private string GetUserName(int userId)
        {
            var user = db.NguoiDungs.Find(userId);
            return user != null ? user.TenNguoiDung : "Người dùng";
        }
        public ActionResult ChapNhan(int id)
        {
            var loiMoi = db.LoiMoiKetBans
               .FirstOrDefault(x => x.ID == id
               && x.TrangThai == "Pending");

            if (loiMoi != null)
            {
                loiMoi.TrangThai = "Accepted";

                int a = loiMoi.NguoiGuiID;
                int b = loiMoi.NguoiNhanID;

                bool exists = db.BanBes.Any(x =>
                    (x.NguoiDungID1 == a &&
                    x.NguoiDungID2 == b) ||  // a → b
                    (x.NguoiDungID1 == b &&
                    x.NguoiDungID2 == a));   // b → a (2 chiều)

                if (!exists)
                {
                    db.BanBes.Add(new BanBe
                    {
                        NguoiDungID1 = a,        // ID người gửi
                        NguoiDungID2 = b,        // ID người nhận
                        NgayKetBan = DateTime.Now // Ngày giờ kết bạn
                    });
                }

                db.SaveChanges();
                ThongBaoService tbService = new ThongBaoService();
                tbService.TaoThongBao(
                    b,      // Ai chấp nhận
                    a,       // Ai nhận thông báo
                    "FRIEND_ACCEPT",
                    $"{GetUserName(b)} đã chấp nhận lời mời kết bạn của bạn",
                    $"/HIOTFZone_BanBe/TrangCaNhanBanBe/{b}"
                );
            }
            
            return RedirectToAction("BanBe");
        }
        public ActionResult TuChoi(int loiMoiID)
        {
            var loiMoi = db.LoiMoiKetBans.Find(loiMoiID);
            if (loiMoi != null)
            {
                db.LoiMoiKetBans.Remove(loiMoi);
                db.SaveChanges();
            }
            return RedirectToAction("BanBe");
        }
        //tạo viewTrangCaNhan
        public ActionResult TrangCaNhanBanBe(int id)
        {
            var user = db.NguoiDungs
                .Where(u => u.NguoiDungID == id)
                .FirstOrDefault();

            if (user == null)
                return HttpNotFound();
            ViewBag.DSBaiDang = db.BaiDangs
                .Where(b => b.NguoiDungID == id)  // lọc theo mã người đăng
                .OrderByDescending(b => b.NgayTao)   // sắp xếp bài mới nhất lên đầu
                .ToList();

            // Nếu avatar null hoặc rỗng, gán mặc định
            if (string.IsNullOrEmpty(user.Avatar))
                user.Avatar = "/Images/Avatar/Ga.jpg";

            return View(user);
        }
    }
}