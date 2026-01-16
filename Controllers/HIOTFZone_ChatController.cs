using MXH_HIOTFZone.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MXH_HIOTFZone.Controllers
{
    public class HIOTFZone_ChatController : Controller
    {
        MXH_MiniEntities db = new MXH_MiniEntities();
        // GET: HIOTFZone_Chat
        public ActionResult CuocTroChuyen()
        {
            if (Session["NguoiDungID"] == null)
                return RedirectToAction("DangNhap", "HIOTFZone_User");
            return View();
        }

        public ActionResult Chat(int id)
        {
            int currentUserId = (int)Session["NguoiDungID"]; // Lấy ID người hiện tại từ session

            // Lấy thông tin người còn lại nếu Private
            var chatInfo = (from tv in db.ThanhVienCuocTroChuyens
                            join nd in db.NguoiDungs on tv.NguoiDungID equals nd.NguoiDungID
                            join ct in db.CuocTroChuyens on tv.CuocTroChuyenID equals ct.CuocTroChuyenID
                            where tv.CuocTroChuyenID == id
                                  && tv.NguoiDungID != currentUserId
                                  && ct.Loai == "Private"
                            select new
                            {
                                TenHienThi = nd.TenNguoiDung,
                                NguoiConLaiID = nd.NguoiDungID,
                                Avatar = nd.Avatar,
                                TrangThai = nd.TrangThaiOnline
                            }).FirstOrDefault();

            var chatGroup = (from ct in db.CuocTroChuyens
                             where ct.CuocTroChuyenID == id && ct.Loai == "Group"
                             select new
                             {
                                 TenGroup = ct.TenCuocTroChuyen
                             }).FirstOrDefault();


            ViewBag.CuocTroChuyenID = id;
            ViewBag.TenHienThi = chatInfo?.TenHienThi ?? chatGroup.TenGroup;
            ViewBag.NguoiConLaiID = chatInfo?.NguoiConLaiID;
            ViewBag.Avatar = chatInfo?.Avatar ?? "Group.jpg";
            ViewBag.Loai = chatInfo != null ? "Private" : "Group";
            ViewBag.TrangThai = chatInfo?.TrangThai;

            return View();
        }

        public ActionResult jLayDanhSachChat()
        {
            if (Session["NguoiDungID"] == null)
                return Json(new { loi = "Chưa đăng nhập" }, JsonRequestBehavior.AllowGet);

            int userId = (int)Session["NguoiDungID"];

            var ds = (from tv in db.ThanhVienCuocTroChuyens
                      where tv.NguoiDungID == userId

                      join ct in db.CuocTroChuyens on tv.CuocTroChuyenID equals ct.CuocTroChuyenID

                      // Lấy tin nhắn mới nhất
                      let lastMsg = db.TinNhans
                          .Where(t => t.CuocTroChuyenID == ct.CuocTroChuyenID)
                          .OrderByDescending(t => t.ThoiGianGui)
                          .FirstOrDefault()

                      // Lấy thành viên còn lại
                      let other = db.ThanhVienCuocTroChuyens
                          .Where(t => t.CuocTroChuyenID == ct.CuocTroChuyenID && t.NguoiDungID != userId)
                          .Select(s => s.NguoiDung)
                          .FirstOrDefault()



                      select new
                      {
                          CuocTroChuyenID = ct.CuocTroChuyenID,
                          Loai = ct.Loai,
                          TenCuocTroChuyen = ct.Loai == "Private"
                                ? other.TenNguoiDung
                                : ct.TenCuocTroChuyen,

                          Avatar = ct.Loai == "Private"
                                ? other.Avatar
                                : "group.png",

                          TinNhanCuoi = lastMsg != null ? lastMsg.NoiDungTin : "",
                          ThoiGian = lastMsg != null ? lastMsg.ThoiGianGui : (DateTime?)null,
                          NguoiGuiID = lastMsg != null ? lastMsg.NguoiGuiID : (int?)null,
                          TenNguoiGui = lastMsg != null ? lastMsg.NguoiDung.TenNguoiDung : "",
                          TrangThaiOnline = other != null ? other.TrangThaiOnline : (bool?)null


                      })
                      .OrderByDescending(x => x.ThoiGian)
                      .ToList();

            return Json(ds, JsonRequestBehavior.AllowGet);
        }

        public JsonResult jLayTinNhan(int id)
        {
            // Lấy ID người đang đăng nhập
            int userId = (int)Session["NguoiDungID"];

            // Lấy tin nhắn theo cuộc trò chuyện
            var ds = db.TinNhans
                .Where(t => t.CuocTroChuyenID == id)
                .OrderBy(t => t.ThoiGianGui)   // tin cũ → tin mới
                .Select(t => new
                {
                    TinNhanID = t.TinNhanID,
                    FromMe = (t.NguoiGuiID == userId),  // true = tin của mình
                    NguoiGuiID = t.NguoiGuiID,
                    NoiDung = t.NoiDungTin,
                    ThoiGian = t.ThoiGianGui

                })
                .ToList();

            // Trả JSON cho JavaScript
            return Json(ds, JsonRequestBehavior.AllowGet);
        }


        // POST: Xóa Group
        [HttpPost]
        public JsonResult XoaGroup(int id)
        {
            try
            {
                var group = db.CuocTroChuyens.FirstOrDefault(c => c.CuocTroChuyenID == id && c.Loai == "Group");
                if (group == null) return Json(new { success = false, error = "Nhóm không tồn tại!" });

                // Xóa các thành viên (cascade đã tự động nếu FK ON DELETE CASCADE)
                db.CuocTroChuyens.Remove(group);
                db.SaveChanges();

                return Json(new { success = true, redirectUrl = Url.Action("CuocTroChuyen", "HIOTFZone_Chat") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: Đổi tên Group
        [HttpPost]
        public JsonResult DoiTenGroup(int id, string newName)
        {
            try
            {
                var group = db.CuocTroChuyens.FirstOrDefault(c => c.CuocTroChuyenID == id && c.Loai == "Group");
                if (group == null) return Json(new { success = false, error = "Nhóm không tồn tại!" });

                group.TenCuocTroChuyen = newName;
                db.SaveChanges();

                return Json(new { success = true, redirectUrl = Url.Action("CuocTroChuyen", "HIOTFZone_Chat") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


        public ActionResult TimKiem(string keyword = "")
        {
            // Lấy ID người dùng hiện tại từ session để loại khỏi kết quả
            int? myID = Session["NguoiDungID"] as int?;

            // Lọc danh sách người dùng, loại bỏ chính bản thân
            var query = db.NguoiDungs.AsQueryable();
            if (myID != null)
                query = query.Where(u => u.NguoiDungID != myID);

            // Nếu có keyword, lọc theo tên
            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(u => u.TenNguoiDung.ToLower().Contains(kw));
            }

            // Chuyển sang List và trả về View
            var ds = query.ToList();
            return View(ds);
        }

    }
}