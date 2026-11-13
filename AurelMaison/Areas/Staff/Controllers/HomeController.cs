using AurelMaison.Models;
using AurelMaison.Services;
using AurelMaison.Areas.Staff.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class HomeController(DonHangService donHang, LichHenService lich) : Controller
{
    public IActionResult Index()
    {
        var ds = donHang.LayDanhSach();
        var now = DateTime.Now;
        return View(new TongQuanStaff
        {
            ChoXacNhan = ds.Count(x => (string?)x.Element("TrangThai") == "ChoXacNhan"),
            DonMoi = ds.Where(x => (string?)x.Element("TrangThai") == "ChoXacNhan").Take(5).ToList(),
            LichHomNay = lich.LayTheoNgay(now),
            DoanhSoThang = ds.Where(x => (string?)x.Element("TrangThai") == "HoanTat"
                && (string?)x.Element("MaNVPhuTrach") == User.MaNV()
                && ((DateTime)x.Element("NgayHoanTat")!).Year == now.Year
                && ((DateTime)x.Element("NgayHoanTat")!).Month == now.Month).Sum(x => (decimal)x.Element("TongTien")!)
        });
    }
}
