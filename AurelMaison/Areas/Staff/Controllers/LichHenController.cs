using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class LichHenController(LichHenService lich) : Controller
{
    public IActionResult Index(DateTime? ngay, string? trangThai)
    {
        ViewBag.Ngay = ngay?.ToString("yyyy-MM-dd"); ViewBag.TrangThai = trangThai;
        return View(lich.LayTheoNgay(ngay, trangThai));
    }
    [HttpPost]
    public IActionResult XuLy(string maLH, string thaoTac, string? lyDo)
    {
        var nv = User.MaNV() ?? "";
        var kq = thaoTac switch
        {
            "XacNhan" => lich.XacNhan(maLH, nv), "HoanTat" => lich.HoanTat(maLH, nv),
            "Huy" => lich.Huy(maLH, nv, lyDo ?? ""), _ => new KetQua { ThongBao = "Thao tác không hợp lệ." }
        };
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }
}
