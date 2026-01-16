using MXH_HIOTFZone.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MXH_HIOTFZone.Controllers
{

    public class TaoCuocTroChuyenController : Controller
    {
        MXH_MiniEntities db = new MXH_MiniEntities();
        // GET: TaoCuocTroChuyen
        // 1) Hiện View tạo cuộc trò chuyện riêng tư
        public ActionResult TaoCuocTroChuyenRieng()
        {
            int userId = (int)Session["NguoiDungID"];

            // Lấy danh sách bạn bè
            var dsBanBe = db.BanBes
                .Where(b => b.NguoiDungID1 == userId || b.NguoiDungID2 == userId)
                .Select(b => b.NguoiDungID1 == userId ? b.NguoiDungID2 : b.NguoiDungID1)
                .Distinct()
                .ToList();

            // Lấy thông tin người dùng
            var dsNguoiDung = db.NguoiDungs
                .Where(u => dsBanBe.Contains(u.NguoiDungID))
                .ToList();

            return View(dsNguoiDung);
        }



        // 2) Xử lý khi người dùng chọn bạn và xác nhận
        [HttpPost]
        public JsonResult CreatePrivateChat(int friendId)
        {
            if (Session["NguoiDungID"] == null)
                return Json(new { success = false, error = "Bạn chưa đăng nhập" });

            int userId = (int)Session["NguoiDungID"];

            // === KIỂM TRA ĐÃ CÓ CUỘC TRÒ CHUYỆN RIÊNG TƯ GIỮA 2 NGƯỜI CHƯA ===
            var existing = (from ct in db.CuocTroChuyens
                            where ct.Loai == "Private"
                            join tv1 in db.ThanhVienCuocTroChuyens on ct.CuocTroChuyenID equals tv1.CuocTroChuyenID
                            join tv2 in db.ThanhVienCuocTroChuyens on ct.CuocTroChuyenID equals tv2.CuocTroChuyenID
                            where tv1.NguoiDungID == userId && tv2.NguoiDungID == friendId
                            select ct).FirstOrDefault();

            if (existing != null)
            {
                // Đã có phòng → chuyển sang chat
                return Json(new
                {
                    success = true,
                    chatUrl = Url.Action("Chat", "HIOTFZone_Chat") + "?id=" + existing.CuocTroChuyenID
                });
            }

            // === KHÔNG CÓ → TẠO PHÒNG CHAT MỚI ===
            CuocTroChuyen newRoom = new CuocTroChuyen()
            {
                TenCuocTroChuyen = "",
                Loai = "Private"
            };

            db.CuocTroChuyens.Add(newRoom);
            db.SaveChanges(); // Sau SaveChanges sẽ có ID phòng

            int roomId = newRoom.CuocTroChuyenID;

            // Thêm thành viên (người dùng)
            db.ThanhVienCuocTroChuyens.Add(new ThanhVienCuocTroChuyen()
            {
                CuocTroChuyenID = roomId,
                NguoiDungID = userId
            });

            // Thêm thành viên (bạn được chọn)
            db.ThanhVienCuocTroChuyens.Add(new ThanhVienCuocTroChuyen()
            {
                CuocTroChuyenID = roomId,
                NguoiDungID = friendId
            });

            db.SaveChanges();

            // Trả về URL phòng mới
            return Json(new
            {
                success = true,
                chatUrl = Url.Action("Chat", "HIOTFZone_Chat") + "?id=" + roomId
            });
        }


        // GET: Tạo group chat
        public ActionResult TaoCuocTroChuyenGroup()
        {
            int userId = (int)Session["NguoiDungID"];

            // Lấy danh sách bạn bè
            var dsBanBe = db.BanBes
                .Where(b => b.NguoiDungID1 == userId || b.NguoiDungID2 == userId)
                .Select(b => b.NguoiDungID1 == userId ? b.NguoiDungID2 : b.NguoiDungID1)
                .Distinct()
                .ToList();

            // Lấy thông tin người dùng
            var dsNguoiDung = db.NguoiDungs
                .Where(u => dsBanBe.Contains(u.NguoiDungID))
                .ToList();

            return View(dsNguoiDung);
        }

        // POST: Tạo group chat
        [HttpPost]
        public ActionResult TaoCuocTroChuyenGroup(int[] selectedUserIds, string groupName)
        {
            int userId = (int)Session["NguoiDungID"];

            if (selectedUserIds == null || selectedUserIds.Length < 3)
                return Json(new { success = false, error = "Phải chọn tối thiểu 3 người để tạo group!" });

            try
            {
                // Tạo cuộc trò chuyện mới
                var chat = new CuocTroChuyen
                {
                    Loai = "Group",
                    TenCuocTroChuyen = groupName,
                    NgayTao = DateTime.Now
                };
                db.CuocTroChuyens.Add(chat);
                db.SaveChanges();

                // Thêm người tạo group
                db.ThanhVienCuocTroChuyens.Add(new ThanhVienCuocTroChuyen
                {
                    CuocTroChuyenID = chat.CuocTroChuyenID,
                    NguoiDungID = userId
                });

                // Thêm các thành viên được chọn
                foreach (var id in selectedUserIds)
                {
                    db.ThanhVienCuocTroChuyens.Add(new ThanhVienCuocTroChuyen
                    {
                        CuocTroChuyenID = chat.CuocTroChuyenID,
                        NguoiDungID = id
                    });
                }

                db.SaveChanges();

                return Json(new { success = true, chatUrl = Url.Action("Chat", "HIOTFZone_Chat", new { id = chat.CuocTroChuyenID }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}