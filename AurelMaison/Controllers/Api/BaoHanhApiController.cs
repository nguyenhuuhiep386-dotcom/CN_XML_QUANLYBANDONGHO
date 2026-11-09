using AurelMaison.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace AurelMaison.Controllers.Api;

/// <summary>
/// Web Service tra cứu bảo hành, trả về XML (Nguyên viết phần thân).
/// Chưa đăng nhập → 401, không phải KhachHang → 403 (cấu hình cookie trong Program.cs).
/// </summary>
[ApiController]
[Route("api/baohanh")]
[Authorize(Roles = VaiTro.KhachHang)]
public class BaoHanhApiController : ControllerBase
{
    [HttpGet("{maPhieu}")]
    public ContentResult TraCuu(string maPhieu)
    {
        var ketQua = new XDocument(
            new XElement("KetQuaTraCuu",
                new XElement("MaPhieu", maPhieu),
                new XElement("ThongBao", "Chưa triển khai")));

        return new ContentResult
        {
            Content = ketQua.ToString(),
            ContentType = "application/xml; charset=utf-8",
            StatusCode = StatusCodes.Status501NotImplemented
        };
    }
}
