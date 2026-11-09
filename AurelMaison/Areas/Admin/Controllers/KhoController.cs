using AurelMaison.Areas.Admin.Models;
using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Admin.Controllers;

/// <summary>inventory · Serial &amp; nhập kho và stockok · Đã ghi nhận nhập kho.</summary>
[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class KhoController(KhoService kho, DanhMucService danhMuc) : Controller
{
    private const int SoDongMacDinh = 3;

    public IActionResult Index(string? maMau, string? trangThai) =>
        View(TaoVM(maMau, trangThai, new NhapKhoForm { MaMau = maMau }));

    [HttpPost]
    public IActionResult NhapKho(NhapKhoForm form)
    {
        var kq = kho.NhapKho(form.MaMau ?? string.Empty, User.MaNV() ?? string.Empty, form.GhiChu, form.Dong);
        if (!kq.ThanhCong)
        {
            ViewData["Loi"] = kq.ThongBao;
            return View("Index", TaoVM(form.MaMau, null, form));
        }
        return RedirectToAction(nameof(DaNhap), new { id = kq.Ma });
    }

    public IActionResult DaNhap(string id)
    {
        var phieu = kho.LayPhieuNhap(id);
        if (phieu == null)
        {
            TempData["Loi"] = $"Không tìm thấy phiếu nhập {id}.";
            return RedirectToAction(nameof(Index));
        }
        var chiec = kho.LayDanhSachChiec(maPN: id);
        var maMau = chiec.FirstOrDefault()?.MaMau ?? string.Empty;
        return View(new DaNhapVM
        {
            Phieu = phieu,
            Chiec = chiec,
            MaMau = maMau,
            TenMau = chiec.FirstOrDefault()?.TenMau ?? string.Empty,
            SoConHangMoi = kho.DemConHang(maMau)
        });
    }

    private KhoIndexVM TaoVM(string? maMau, string? trangThai, NhapKhoForm form)
    {
        while (form.Dong.Count < SoDongMacDinh)
        {
            form.Dong.Add(new DongNhapKho());
        }
        return new KhoIndexVM
        {
            DanhSach = kho.LayDanhSachChiec(maMau, trangThai),
            Mau = danhMuc.LayDanhSachQuanTri(),
            MaMau = maMau,
            TrangThai = trangThai,
            TheoTrangThai = kho.DemTheoTrangThai(),
            Form = form
        };
    }
}
