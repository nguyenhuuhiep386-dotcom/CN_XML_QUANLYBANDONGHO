using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace AurelMaison.Controllers.Api;

/// <summary>Web Service tra cứu bảo hành, trả về XML. Chờ PhieuBaoHanh.xml từ script CSDL.</summary>
[ApiController]
[Route("api/baohanh")]
public class BaoHanhApiController : ControllerBase
{
    [HttpGet("{serial}")]
    public ContentResult TraCuu(string serial)
    {
        var ketQua = new XDocument(
            new XElement("KetQuaTraCuu",
                new XElement("Serial", serial),
                new XElement("ThongBao", "Chưa triển khai")));

        return new ContentResult
        {
            Content = ketQua.ToString(),
            ContentType = "application/xml; charset=utf-8",
            StatusCode = StatusCodes.Status501NotImplemented
        };
    }
}
