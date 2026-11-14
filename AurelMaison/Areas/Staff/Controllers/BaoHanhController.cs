using AurelMaison.Areas.Staff.Models;
using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class BaoHanhController(BaoHanhService baoHanh, DonHangService donHang) : Controller
{
    public IActionResult Index(string? maDon) => View(new BaoHanhViewModel
    {
        DonHoanTat = donHang.LayDanhSach(trangThai: "HoanTat"),
        Don = string.IsNullOrEmpty(maDon) ? null : donHang.LayChiTiet(maDon), Phieu = baoHanh.LayDanhSach()
    });
    [HttpPost]
    public IActionResult LapPhieu(string maDon, string serial)
    {
        var kq = baoHanh.LapPhieu(maDon, serial, User.MaNV() ?? "");
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao + (kq.ThanhCong ? " Mã phiếu: " + kq.Ma : "");
        return RedirectToAction(nameof(Index), new { maDon });
    }
}
