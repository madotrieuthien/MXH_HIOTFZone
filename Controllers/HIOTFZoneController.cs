using MXH_HIOTFZone.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace MXH_HIOTFZone.Controllers
{
    public class HIOTFZoneController : Controller
    {
        MXH_MiniEntities db = new MXH_MiniEntities();
        // GET: HIOTFZone
        public ActionResult Index()

        {
            
            if (Session["NguoiDungID"] == null)
            {
                return RedirectToAction("DangNhap", "HIOTFZone_User");
            }
            else
            {
                int userId = (int)Session["NguoiDungID"];

                // Lấy danh sách ID bạn bè đã chấp nhận
                var friendIds = db.BanBes
                    .Where(b =>
                        (b.NguoiDungID1 == userId || b.NguoiDungID2 == userId))
                    .Select(b => b.NguoiDungID1 == userId ? b.NguoiDungID2 : b.NguoiDungID1)
                    .ToList();

                // Nếu muốn tính cả bài đăng của chính mình
                friendIds.Add(userId);

                // Lấy bài đăng của bạn bè
                var dsBaiDang = db.BaiDangs
                    .Where(b => friendIds.Contains(b.NguoiDungID))
                    .OrderByDescending(b => b.NgayTao)
                    .ToList();


                return View(dsBaiDang);
            }

        }


        public ActionResult Admin()
        {
            if(Session["AdminID"] == null)
            {
                return RedirectToAction("DangNhap", "HIOTFZone_Admin");
            }
            return View();
        }
    }
}