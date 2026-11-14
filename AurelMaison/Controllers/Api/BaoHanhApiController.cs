using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace AurelMaison.Controllers.Api;

/// <summary>
/// Web Service tra cứu bảo hành, chỉ trả phiếu thuộc khách đang đăng nhập.
/// Chưa đăng nhập → 401, không phải KhachHang → 403 (cấu hình cookie trong Program.cs).
/// </summary>
[ApiController]
[Route("api/baohanh")]
[Authorize(Roles = VaiTro.KhachHang)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BaoHanhApiController(BaoHanhService baoHanh) : ControllerBase
{
    [HttpGet]
    public IActionResult DanhSach()
    {
        var maKH = User.MaKH();
        if (string.IsNullOrWhiteSpace(maKH)) return StatusCode(403);
        return XmlHelper.ChayTrongKhoa(() => Xml(new XElement("DanhSachPhieuBaoHanh",
            baoHanh.LayTheoKhach(maKH).Select(baoHanh.TaoPhanHoi))));
    }

    [HttpGet("{maPhieu}")]
    public IActionResult TraCuu(string maPhieu) => XmlHelper.ChayTrongKhoa<IActionResult>(() =>
    {
        var maKH = User.MaKH();
        if (string.IsNullOrWhiteSpace(maKH)) return StatusCode(403);
        var phieu = baoHanh.LayPhieu(maPhieu);
        if (phieu == null) return NotFound();
        if ((string?)phieu.Element("MaKH") != maKH) return StatusCode(403);
        return Xml(baoHanh.TaoPhanHoi(phieu));
    });

    private ContentResult Xml(XElement element) => Content(element.ToString(), "application/xml; charset=utf-8");
}
