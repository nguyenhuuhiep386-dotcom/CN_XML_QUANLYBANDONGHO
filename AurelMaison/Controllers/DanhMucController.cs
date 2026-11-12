using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

public sealed class DanhMucController(IDanhMucService catalog) : Controller
{
    public IActionResult Index(string? tuKhoa, string? hang, decimal? giaTu, decimal? giaDen, string? loaiMay, string? chatLieu)
    {
        var filters = new LocDongHo(tuKhoa, hang, giaTu, giaDen, loaiMay, chatLieu);
        if (giaTu < 0 || giaDen < 0 || (giaTu.HasValue && giaDen.HasValue && giaTu.Value > giaDen.Value))
            ModelState.AddModelError("", "Khoảng giá không hợp lệ.");
        var all = catalog.LayDanhSach(new(null, null, null, null, null, null));
        return View(new DanhMucVm(filters, ModelState.IsValid ? catalog.LayDanhSach(filters) : [],
            catalog.LayHang(), all.Select(x => x.LoaiMay).Distinct().Order().ToList(),
            all.Select(x => x.ChatLieu).Distinct().Order().ToList()));
    }

    public IActionResult ChiTiet(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || catalog.LayChiTiet(id) is not { } product) return NotFound();
        return View(product);
    }

    [HttpGet("/DanhMuc/Xml")]
    public IActionResult Xml() => Content(XmlHelper.XuatHtml(XmlHelper.Load("DongHo"), "DanhMucDongHo"), "text/html; charset=utf-8");
}
