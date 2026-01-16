using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MXH_HIOTFZone.Models;
using System.Web.Mvc;

namespace MXH_HIOTFZone.Controllers
{
    public class HIOTFZone_TimKiemController : Controller
    {
        MXH_MiniEntities db = new MXH_MiniEntities();

        public ActionResult KetQua(string keyword = "")
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                ViewBag.Keyword = keyword;
                return View(new List<BaiDang>());
            }

            var ds = db.BaiDangs
                .Where(b => b.NoiDung.Contains(keyword)
                         || b.NguoiDung.TenNguoiDung.Contains(keyword))
                .OrderByDescending(b => b.NgayTao)
                .ToList();

            ViewBag.Keyword = keyword;

            return View(ds);
        }
    }
}