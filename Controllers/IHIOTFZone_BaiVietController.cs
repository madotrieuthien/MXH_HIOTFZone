using System.Web;
using System.Web.Mvc;

namespace MXH_HIOTFZone.Controllers
{
    public interface IHIOTFZone_BaiVietController
    {
        ActionResult TaoBaiViet();
        ActionResult TaoBaiViet(string NoiDung, HttpPostedFileBase AnhBaiDang);
    }
}