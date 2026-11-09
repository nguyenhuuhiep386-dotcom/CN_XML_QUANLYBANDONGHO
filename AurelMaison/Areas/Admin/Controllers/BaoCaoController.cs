using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Admin.Controllers;

/// <summary>reports · Báo cáo kinh doanh. Bản in đi qua BaoCaoDoanhThu.xslt.</summary>
[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class BaoCaoController(BaoCaoService baoCao) : Controller
{
    public IActionResult Index(DateTime? tuNgay, DateTime? denNgay)
    {
        var (tu, den) = KhoangNgay(tuNgay, denNgay);
        return View(baoCao.LapBaoCao(tu, den));
    }

    /// <summary>Dựng XDocument tổng hợp → XSLT → trang HTML có nút In.</summary>
    public IActionResult In(DateTime? tuNgay, DateTime? denNgay)
    {
        var (tu, den) = KhoangNgay(tuNgay, denNgay);
        var xml = BaoCaoService.TaoXml(baoCao.LapBaoCao(tu, den));
        return Content(XmlHelper.XuatHtml(xml, "BaoCaoDoanhThu"), "text/html; charset=utf-8");
    }

    /// <summary>Mặc định: từ đầu tháng hiện tại đến cuối tháng. Ngày đảo ngược thì đổi chỗ.</summary>
    private static (DateTime Tu, DateTime Den) KhoangNgay(DateTime? tuNgay, DateTime? denNgay)
    {
        var dauThang = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var tu = (tuNgay ?? dauThang).Date;
        var den = (denNgay ?? dauThang.AddMonths(1).AddDays(-1)).Date;
        return tu <= den ? (tu, den) : (den, tu);
    }
}
