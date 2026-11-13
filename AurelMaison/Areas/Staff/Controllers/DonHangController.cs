using AurelMaison.Areas.Staff.Models;
using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class DonHangController(DonHangService donHang, KhoService kho, DanhMucService danhMuc, KhachHangService khach) : Controller
{
    [HttpGet]
    public IActionResult Index(DanhSachDonViewModel model)
    {
        model.Don = donHang.LayDanhSach(model.TuKhoa, model.TrangThai, model.NguonDon, model.TuNgay, model.DenNgay);
        return View(model);
    }
    [HttpGet]
    public IActionResult ChiTiet(string id)
    {
        var don = donHang.LayChiTiet(id);
        if (don == null) return NotFound();
        var model = new ChiTietDonViewModel { Don = don };
        foreach (var ma in don.Element("ChiTiet")!.Elements().Select(x => (string)x.Element("MaMau")!).Distinct())
            model.SerialTheoMau[ma] = kho.LaySerialConHang(ma);
        return View(model);
    }
    [HttpPost]
    public IActionResult XacNhan(string maDon, string[] serial) => KetQuaDon(donHang.XacNhan(maDon, serial, User.MaNV() ?? ""), maDon);
    [HttpPost]
    public IActionResult GiaoHang(string maDon) => KetQuaDon(donHang.GiaoHang(maDon, User.MaNV() ?? ""), maDon);
    [HttpPost]
    public IActionResult HoanTat(string maDon, string phuongThucTT) => KetQuaDon(donHang.HoanTat(maDon, User.MaNV() ?? "", phuongThucTT), maDon);
    [HttpPost]
    public IActionResult Huy(string maDon, string lyDo) => KetQuaDon(donHang.HuyBoiNhanVien(maDon, User.MaNV() ?? "", lyDo), maDon);

    [HttpGet("/Staff/HoaDon/{maHD}")]
    public IActionResult HoaDon(string maHD)
    {
        var html = donHang.XuatHoaDon(maHD);
        return html == null ? NotFound() : Content(html, "text/html; charset=utf-8");
    }
    [HttpGet]
    public IActionResult TaiQuay(string? soDienThoai) => View(new TaiQuayViewModel
    {
        SoDienThoai = soDienThoai, Khach = khach.TimTheoSoDienThoai(soDienThoai),
        Chiec = kho.LayDanhSachChiec(trangThai: "ConHang"),
        GiaTheoMau = danhMuc.LayDanhSach().ToDictionary(x => x.MaMau, x => x.Gia)
    });
    [HttpPost]
    public IActionResult ThemKhach(string hoTen, string soDienThoai, string? email, string? diaChi)
    {
        var kq = khach.ThemKhachTaiQuay(hoTen, soDienThoai, email, diaChi);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(TaiQuay), new { soDienThoai });
    }
    [HttpPost]
    public IActionResult TaoTaiQuay(string maKH, string[] serial, string phuongThucTT)
    {
        var kq = donHang.TaoDonTaiQuay(maKH, serial, User.MaNV() ?? "", phuongThucTT);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return kq.ThanhCong ? RedirectToAction(nameof(ChiTiet), new { id = kq.Ma })
            : RedirectToAction(nameof(TaiQuay), new { soDienThoai = khach.LayTheoMa(maKH)?.SoDienThoai });
    }
    private IActionResult KetQuaDon(KetQua kq, string ma)
    {
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id = ma });
    }
}
