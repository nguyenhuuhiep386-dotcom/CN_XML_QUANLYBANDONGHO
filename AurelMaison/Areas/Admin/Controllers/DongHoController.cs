using AurelMaison.Areas.Admin.Models;
using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace AurelMaison.Areas.Admin.Controllers;

/// <summary>Dữ liệu form thêm/sửa mẫu. Giá và kích thước nhận dạng chuỗi rồi tự đọc bằng InvariantCulture.</summary>
public class MauDongHoForm
{
    public bool LaMoi { get; set; }
    public string? MaMau { get; set; }
    public string? TenMau { get; set; }
    public string? MaHang { get; set; }
    public string? Gia { get; set; }
    public string? LoaiMay { get; set; }
    public string? KichThuoc { get; set; }
    public string? ChatLieu { get; set; }
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public int ThoiHanBaoHanh { get; set; }
}

/// <summary>products · Đồng hồ &amp; kho và editproduct · Thêm / sửa đồng hồ, kèm khung hãng.</summary>
[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class DongHoController(DanhMucService danhMuc, IWebHostEnvironment env) : Controller
{
    private const long DungLuongAnhToiDa = 5 * 1024 * 1024;
    private static readonly string[] DuoiAnh = [".jpg", ".jpeg", ".png", ".webp"];

    public IActionResult Index(string? tuKhoa, string? maHang, string? trangThai, string? suaHang)
    {
        var hang = danhMuc.LayDanhSachHang();
        return View(new DongHoIndexVM
        {
            DanhSach = danhMuc.LayDanhSachQuanTri(tuKhoa, maHang, trangThai),
            Hang = hang,
            TuKhoa = tuKhoa,
            MaHang = maHang,
            TrangThai = trangThai,
            HangDangSua = hang.FirstOrDefault(h => h.MaHang == suaHang)
        });
    }

    public IActionResult Them() => View("Sua", new SuaDongHoVM
    {
        LaMoi = true,
        Mau = new MauDongHo { ThoiHanBaoHanh = 24 },
        Hang = danhMuc.LayDanhSachHang()
    });

    public IActionResult Sua(string id)
    {
        var mau = danhMuc.LayChiTiet(id);
        if (mau == null)
        {
            TempData["Loi"] = $"Không tìm thấy mẫu {id}.";
            return RedirectToAction(nameof(Index));
        }
        return View(new SuaDongHoVM { Mau = mau, Hang = danhMuc.LayDanhSachHang() });
    }

    [HttpPost]
    [RequestSizeLimit(DungLuongAnhToiDa + 1024 * 1024)]
    public async Task<IActionResult> Luu(MauDongHoForm form, IFormFile? anh)
    {
        var mau = new MauDongHo
        {
            MaMau = form.MaMau ?? string.Empty,
            TenMau = form.TenMau ?? string.Empty,
            MaHang = form.MaHang ?? string.Empty,
            LoaiMay = form.LoaiMay ?? string.Empty,
            ChatLieu = form.ChatLieu ?? string.Empty,
            MoTa = form.MoTa,
            HinhAnh = form.HinhAnh,
            ThoiHanBaoHanh = form.ThoiHanBaoHanh
        };

        string? loi = null;
        if (!decimal.TryParse(form.Gia?.Replace(".", "").Replace(",", "").Replace(" ", ""), NumberStyles.None, CultureInfo.InvariantCulture, out var gia))
        {
            loi = "Giá phải là số nguyên dương (đồng).";
        }
        if (!decimal.TryParse(form.KichThuoc?.Replace(",", "."), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var kichThuoc))
        {
            loi ??= "Kích thước phải là số, ví dụ 36 hoặc 39.8.";
        }
        mau.Gia = gia;
        mau.KichThuoc = kichThuoc;

        string? fileMoi = null;
        if (loi == null && anh is { Length: > 0 })
        {
            (fileMoi, loi) = await LuuAnh(anh);
            if (fileMoi != null)
            {
                mau.HinhAnh = "/images/" + fileMoi;
            }
        }

        var kq = loi == null ? danhMuc.LuuMau(mau, form.LaMoi) : new KetQua { ThongBao = loi };
        if (!kq.ThanhCong)
        {
            if (fileMoi != null)
            {
                System.IO.File.Delete(Path.Combine(env.WebRootPath, "images", fileMoi));
            }
            ViewData["Loi"] = kq.ThongBao;
            mau.HinhAnh = form.HinhAnh;
            return View("Sua", new SuaDongHoVM { LaMoi = form.LaMoi, Mau = mau, Hang = danhMuc.LayDanhSachHang() });
        }

        TempData["ThongBao"] = form.LaMoi ? $"Đã thêm mẫu {kq.Ma}." : $"Đã lưu mẫu {kq.Ma}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult DoiTrangThai(string id, string trangThai)
    {
        var kq = danhMuc.DoiTrangThaiMau(id, trangThai);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThanhCong
            ? $"Mẫu {id}: {HienThi.TrangThaiMau(trangThai).ToLowerInvariant()}."
            : kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    // ===== Hãng =====

    [HttpPost]
    public IActionResult LuuHang(string? maHang, string tenHang, string? quocGia, string? moTa)
    {
        var kq = string.IsNullOrEmpty(maHang)
            ? danhMuc.ThemHang(tenHang, quocGia, moTa)
            : danhMuc.SuaHang(maHang, tenHang, quocGia, moTa);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThanhCong ? $"Đã lưu hãng {kq.Ma}." : kq.ThongBao;
        return kq.ThanhCong || string.IsNullOrEmpty(maHang)
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Index), new { suaHang = maHang });
    }

    [HttpPost]
    public IActionResult XoaHang(string maHang)
    {
        var kq = danhMuc.XoaHang(maHang);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThanhCong ? $"Đã xóa hãng {maHang}." : kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Lưu ảnh vào wwwroot/images với tên mới; trả (tên file, null) hoặc (null, lỗi).</summary>
    private async Task<(string? TenFile, string? Loi)> LuuAnh(IFormFile anh)
    {
        var duoi = Path.GetExtension(anh.FileName).ToLowerInvariant();
        if (!DuoiAnh.Contains(duoi) || !anh.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return (null, "Ảnh phải là file JPG, PNG hoặc WEBP.");
        }
        if (anh.Length > DungLuongAnhToiDa)
        {
            return (null, "Ảnh tối đa 5 MB.");
        }

        var tenFile = $"dh_{DateTime.Now:yyyyMMddHHmmssfff}{duoi}";
        var thuMuc = Path.Combine(env.WebRootPath, "images");
        Directory.CreateDirectory(thuMuc);
        await using var luong = System.IO.File.Create(Path.Combine(thuMuc, tenFile));
        await anh.CopyToAsync(luong);
        return (tenFile, null);
    }
}
