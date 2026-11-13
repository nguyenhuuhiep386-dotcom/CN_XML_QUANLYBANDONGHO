using System.Xml.Linq;
using AurelMaison.Models;
using AurelMaison.Services;

namespace AurelMaison.Areas.Staff.Models;

public class DanhSachDonViewModel
{
    public List<XElement> Don { get; set; } = [];
    public string? TuKhoa { get; set; }
    public string? TrangThai { get; set; }
    public string? NguonDon { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
}
public class ChiTietDonViewModel
{
    public XElement Don { get; set; } = new("DonHang");
    public Dictionary<string, List<string>> SerialTheoMau { get; set; } = [];
}
public class TaiQuayViewModel
{
    public string? SoDienThoai { get; set; }
    public KhachHang? Khach { get; set; }
    public List<ChiecDongHo> Chiec { get; set; } = [];
    public Dictionary<string, decimal> GiaTheoMau { get; set; } = [];
}
public class BaoHanhViewModel
{
    public List<XElement> DonHoanTat { get; set; } = [];
    public XElement? Don { get; set; }
    public List<XElement> Phieu { get; set; } = [];
}
public class TongQuanStaff
{
    public int ChoXacNhan { get; set; }
    public decimal DoanhSoThang { get; set; }
    public List<XElement> DonMoi { get; set; } = [];
    public List<XElement> LichHomNay { get; set; } = [];
}
public static class StaffHienThi
{
    public static string Text(XElement e, string cot) => (string?)e.Element(cot) ?? "";
    public static string TrangThai(string? ma) => ma switch
    {
        "ChoXacNhan" => "Chờ xác nhận", "DaXacNhan" => "Đã xác nhận", "DangGiao" => "Đang giao",
        "HoanTat" => "Hoàn tất", "DaHuy" => "Đã hủy", _ => ma ?? ""
    };
}
