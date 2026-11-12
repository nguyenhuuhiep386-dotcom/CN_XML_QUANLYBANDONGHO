using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

[Authorize(Roles = Security.Customer)]
public sealed class BookingController(LichHenService booking, IKhachHangService customers, IDanhMucService catalog) : Controller
{
    [HttpGet]
    public IActionResult Index(string? maMau)
    {
        var profile = customers.LayTheoTaiKhoan(User.MaTK()!);
        if (profile is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(maMau) && catalog.LayChiTiet(maMau) is null) return NotFound();
        return BookingView(new() { MaMau = maMau, HoTen = profile.HoTen, SoDienThoai = profile.SoDienThoai ?? "" });
    }

    [HttpPost]
    public IActionResult Index(DatLichForm form)
    {
        if (!ModelState.IsValid) return BookingView(form);
        var result = booking.Tao(User.MaKH()!, form);
        if (result.ThanhCong) return RedirectToAction(nameof(Booked), new { id = result.Ma });
        ModelState.AddModelError("", result.ThongBao);
        return BookingView(form);
    }

    [HttpGet]
    public IActionResult Booked(string id)
    {
        var item = string.IsNullOrWhiteSpace(id) ? null : booking.LayChiTiet(id);
        if (item is null || item.MaKH != User.MaKH()) return NotFound();
        ViewData["TenMau"] = item.MaMau is { } code ? catalog.LayChiTiet(code)?.TenMau ?? code : "Tư vấn chung";
        return View(item);
    }

    private IActionResult BookingView(DatLichForm form)
    {
        ViewData["Mau"] = catalog.LayDanhSach(new(null, null, null, null, null, null));
        return View("Index", form);
    }
}
