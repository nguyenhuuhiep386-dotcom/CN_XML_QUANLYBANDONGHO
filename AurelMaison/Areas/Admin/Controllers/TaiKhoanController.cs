using AurelMaison.Areas.Admin.Models;
using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Admin.Controllers;

/// <summary>users · Tài khoản &amp; phân quyền và userok · Đã lưu quyền tài khoản.</summary>
[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class TaiKhoanController(QuanLyTaiKhoanService quanLy) : Controller
{
    public IActionResult Index(string? vaiTro, string? chon) => View(TaoVM(vaiTro, chon, new TaoNhanVienForm()));

    [HttpPost]
    public IActionResult CapNhat(string maTK, string vaiTro, string trangThai)
    {
        var kq = quanLy.CapNhat(maTK, vaiTro, trangThai, User.MaTK());
        if (!kq.ThanhCong)
        {
            TempData["Loi"] = kq.ThongBao;
            return RedirectToAction(nameof(Index), new { chon = maTK });
        }
        return RedirectToAction(nameof(DaLuu), new { id = maTK });
    }

    [HttpPost]
    public IActionResult TaoNhanVien(TaoNhanVienForm tao)
    {
        var kq = quanLy.TaoNhanVien(tao.HoTen ?? string.Empty, tao.Email ?? string.Empty, tao.MatKhauTam ?? string.Empty,
            tao.SoDienThoai, tao.ChucVu, tao.VaiTro);
        if (!kq.ThanhCong)
        {
            ViewData["Loi"] = kq.ThongBao;
            tao.MatKhauTam = null;
            return View("Index", TaoVM(null, null, tao));
        }
        return RedirectToAction(nameof(DaLuu), new { id = kq.Ma, moi = true });
    }

    public IActionResult DaLuu(string id, bool moi = false)
    {
        var tk = quanLy.LayMot(id);
        if (tk == null)
        {
            return RedirectToAction(nameof(Index));
        }
        ViewData["Moi"] = moi;
        return View(tk);
    }

    private TaiKhoanIndexVM TaoVM(string? vaiTro, string? chon, TaoNhanVienForm tao)
    {
        var tatCa = quanLy.LayDanhSach();
        return new TaiKhoanIndexVM
        {
            DanhSach = string.IsNullOrEmpty(vaiTro) ? tatCa : tatCa.Where(t => t.VaiTro == vaiTro).ToList(),
            VaiTro = vaiTro,
            DangChon = tatCa.FirstOrDefault(t => t.MaTK == chon),
            MaTKDangDung = User.MaTK(),
            Tao = tao
        };
    }
}
