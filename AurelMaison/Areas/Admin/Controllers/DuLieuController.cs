using AurelMaison.Areas.Admin.Models;
using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Admin.Controllers;

/// <summary>xmldata · Dữ liệu XML: kiểm tra toàn bộ, thử XSD, XML ↔ SQL, sao lưu / phục hồi, đối chiếu.</summary>
[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class DuLieuController(DongBoService dongBo, BaoCaoService baoCao) : Controller
{
    private const long DungLuongFileToiDa = 5 * 1024 * 1024;

    public IActionResult Index(string? doiChieu)
    {
        var vm = TaoVM();
        if (!string.IsNullOrEmpty(doiChieu))
        {
            vm.TenBanXuat = doiChieu;
            vm.DoiChieu = dongBo.DoiChieu(doiChieu);
        }
        return View(vm);
    }

    [HttpPost]
    public IActionResult KiemTra()
    {
        var vm = TaoVM();
        vm.KiemTra = dongBo.KiemTraToanBo();
        return View("Index", vm);
    }

    [HttpPost]
    [RequestSizeLimit(DungLuongFileToiDa + 1024 * 1024)]
    public IActionResult KiemTraFile(string tenBang, IFormFile? file)
    {
        var vm = TaoVM();
        vm.TenBangThu = tenBang;
        if (file is not { Length: > 0 })
        {
            TempData["Loi"] = "Chọn một file XML để kiểm tra.";
            return RedirectToAction(nameof(Index));
        }
        if (file.Length > DungLuongFileToiDa)
        {
            TempData["Loi"] = "File tối đa 5 MB.";
            return RedirectToAction(nameof(Index));
        }
        vm.TenFileThu = file.FileName;
        using var luong = file.OpenReadStream();
        vm.KiemTraFile = dongBo.KiemTraFile(tenBang, luong);
        return View("Index", vm);
    }

    [HttpPost]
    public IActionResult SaoLuu()
    {
        var kq = dongBo.SaoLuu();
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult PhucHoi(string ten, bool xacNhan)
    {
        if (!xacNhan)
        {
            TempData["Loi"] = "Đánh dấu ô xác nhận trước khi phục hồi.";
            return RedirectToAction(nameof(Index));
        }
        var kq = dongBo.PhucHoi(ten);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult XmlVaoSql()
    {
        var vm = TaoVM();
        vm.DongBo = dongBo.XmlVaoSql();
        if (vm.DongBo.ThanhCong)
        {
            SoDoanhThu(vm);
        }
        vm.SaoLuu = dongBo.LayDanhSachSaoLuu();
        return View("Index", vm);
    }

    [HttpPost]
    public IActionResult SqlRaXml(bool khoiTao, bool xacNhan)
    {
        if (khoiTao && !xacNhan)
        {
            TempData["Loi"] = "Khởi tạo sẽ ghi đè toàn bộ XML đang chạy: đánh dấu ô xác nhận trước.";
            return RedirectToAction(nameof(Index));
        }
        var kq = dongBo.SqlRaXml(khoiTao);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        // Kiểm tra: mở luôn bảng đối chiếu với bản vừa xuất.
        return !khoiTao && kq.ThanhCong
            ? RedirectToAction(nameof(Index), new { doiChieu = kq.Ma })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult DoanhThu()
    {
        var vm = TaoVM();
        SoDoanhThu(vm);
        return View("Index", vm);
    }

    private DuLieuVM TaoVM() => new()
    {
        SaoLuu = dongBo.LayDanhSachSaoLuu(),
        BanXuat = dongBo.LayDanhSachKiemTra()
    };

    private void SoDoanhThu(DuLieuVM vm)
    {
        var bc = baoCao.LapBaoCao(new DateTime(2000, 1, 1), DateTime.Today.AddYears(10));
        vm.CoSoDoanhThu = true;
        vm.SoDonXml = bc.SoDon;
        vm.DoanhThuXml = bc.DoanhThu;
        vm.DoanhThuSql = dongBo.DoanhThuSql(out var loi);
        vm.LoiSql = loi;
    }
}
