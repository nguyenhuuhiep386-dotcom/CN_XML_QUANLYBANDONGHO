using AurelMaison.Models;
using System.Globalization;
using System.Xml.Linq;

namespace AurelMaison.Services;

public class DongBaoCaoMau
{
    public string MaMau { get; set; } = string.Empty;
    public string TenMau { get; set; } = string.Empty;
    public int SoChiec { get; set; }
    public decimal DoanhThu { get; set; }
}

public class DongBaoCaoNhanVien
{
    public string? MaNV { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public int SoDon { get; set; }
    public decimal DoanhThu { get; set; }

    /// <summary>Phần trăm trên tổng doanh thu kỳ, 0–100.</summary>
    public decimal TyTrong { get; set; }
}

public class BaoCaoDoanhThu
{
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public decimal DoanhThu { get; set; }
    public int SoChiec { get; set; }
    public int SoDon { get; set; }
    public decimal GiaTriTrungBinh { get; set; }
    public List<DongBaoCaoMau> TopMau { get; set; } = [];
    public List<DongBaoCaoNhanVien> TheoNhanVien { get; set; } = [];
}

public class TongQuanHeThong
{
    public DateTime Thang { get; set; }
    public decimal DoanhThuThang { get; set; }
    public int SoDonHoanTatThang { get; set; }
    public int SoDonDangXuLy { get; set; }
    public int SoMauDangBan { get; set; }
    public Dictionary<string, int> ChiecTheoTrangThai { get; set; } = [];
    public List<(string Nhan, decimal DoanhThu)> TheoTuan { get; set; } = [];
}

/// <summary>
/// Số liệu tính trực tiếp từ XML đang chạy bằng LINQ to XML. Doanh thu = tổng DonGia chi tiết của đơn HoanTat,
/// lọc theo NgayHoanTat; đơn DaHuy không tính.
/// </summary>
public class BaoCaoService(KhoService khoService)
{
    private record DonHoanTat(string MaDon, string? MaNV, DateTime NgayHoanTat, List<XElement> ChiTiet)
    {
        public decimal TongTien => ChiTiet.Sum(ct => MauDongHo.SoThapPhan(ct.Element("DonGia")));
    }

    public TongQuanHeThong TongQuan(DateTime? thang = null)
    {
        var dauThang = new DateTime((thang ?? DateTime.Now).Year, (thang ?? DateTime.Now).Month, 1);
        var cuoiThang = dauThang.AddMonths(1).AddTicks(-1);
        var donThang = DocDonHoanTat(dauThang, cuoiThang);

        var dangXuLy = new[] { TrangThaiDon.ChoXacNhan, TrangThaiDon.DaXacNhan, TrangThaiDon.DangGiao };
        var tq = new TongQuanHeThong
        {
            Thang = dauThang,
            DoanhThuThang = donThang.Sum(d => d.TongTien),
            SoDonHoanTatThang = donThang.Count,
            SoDonDangXuLy = XmlHelper.Load("DonHang").Root!.Elements("DonHang")
                .Count(e => dangXuLy.Contains((string?)e.Element("TrangThai"))),
            SoMauDangBan = XmlHelper.Load("DongHo").Root!.Elements("DongHo")
                .Count(e => (string?)e.Element("TrangThai") == TrangThaiMau.DangBan),
            ChiecTheoTrangThai = khoService.DemTheoTrangThai()
        };

        // Tuần 1–3: 7 ngày; tuần 4 gồm phần còn lại của tháng.
        var moc = new[] { 1, 8, 15, 22, cuoiThang.Day + 1 };
        for (var i = 0; i < 4; i++)
        {
            var tu = dauThang.AddDays(moc[i] - 1);
            var den = dauThang.AddDays(moc[i + 1] - 1);
            tq.TheoTuan.Add(($"Tuần {i + 1}", donThang.Where(d => d.NgayHoanTat >= tu && d.NgayHoanTat < den).Sum(d => d.TongTien)));
        }
        return tq;
    }

    public BaoCaoDoanhThu LapBaoCao(DateTime tuNgay, DateTime denNgay, int soMauTop = 5)
    {
        tuNgay = tuNgay.Date;
        denNgay = denNgay.Date;
        var don = DocDonHoanTat(tuNgay, denNgay.AddDays(1).AddTicks(-1));
        var tenNV = XmlHelper.Load("NhanVien").Root!.Elements("NhanVien")
            .ToDictionary(e => (string?)e.Element("MaNV") ?? string.Empty, e => (string?)e.Element("HoTen") ?? string.Empty);

        var bc = new BaoCaoDoanhThu
        {
            TuNgay = tuNgay,
            DenNgay = denNgay,
            DoanhThu = don.Sum(d => d.TongTien),
            SoChiec = don.Sum(d => d.ChiTiet.Count),
            SoDon = don.Count
        };
        bc.GiaTriTrungBinh = bc.SoDon == 0 ? 0 : decimal.Round(bc.DoanhThu / bc.SoDon, 0);

        bc.TopMau = don.SelectMany(d => d.ChiTiet)
            .GroupBy(ct => (string?)ct.Element("MaMau") ?? string.Empty)
            .Select(g => new DongBaoCaoMau
            {
                MaMau = g.Key,
                TenMau = (string?)g.First().Element("TenMau") ?? g.Key,
                SoChiec = g.Count(),
                DoanhThu = g.Sum(ct => MauDongHo.SoThapPhan(ct.Element("DonGia")))
            })
            .OrderByDescending(m => m.SoChiec).ThenByDescending(m => m.DoanhThu)
            .Take(soMauTop)
            .ToList();

        bc.TheoNhanVien = don
            .GroupBy(d => d.MaNV)
            .Select(g => new DongBaoCaoNhanVien
            {
                MaNV = g.Key,
                HoTen = g.Key == null ? "Chưa phân công" : tenNV.GetValueOrDefault(g.Key, g.Key),
                SoDon = g.Count(),
                DoanhThu = g.Sum(d => d.TongTien),
                TyTrong = bc.DoanhThu == 0 ? 0 : decimal.Round(g.Sum(d => d.TongTien) * 100 / bc.DoanhThu, 1)
            })
            .OrderByDescending(n => n.DoanhThu)
            .ToList();
        return bc;
    }

    /// <summary>XML tổng hợp làm đầu vào cho App_Data/Xslt/BaoCaoDoanhThu.xslt.</summary>
    public static XDocument TaoXml(BaoCaoDoanhThu bc)
    {
        static string So(decimal so) => so.ToString(CultureInfo.InvariantCulture);
        return new XDocument(
            new XElement("BaoCaoDoanhThu",
                new XAttribute("TuNgay", bc.TuNgay.ToString("yyyy-MM-dd")),
                new XAttribute("DenNgay", bc.DenNgay.ToString("yyyy-MM-dd")),
                new XAttribute("NgayLap", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                new XElement("TongHop",
                    new XElement("DoanhThu", So(bc.DoanhThu)),
                    new XElement("SoChiec", bc.SoChiec),
                    new XElement("SoDon", bc.SoDon),
                    new XElement("GiaTriTrungBinh", So(bc.GiaTriTrungBinh))),
                new XElement("MauBanChay", bc.TopMau.Select(m => new XElement("Mau",
                    new XElement("MaMau", m.MaMau),
                    new XElement("TenMau", m.TenMau),
                    new XElement("SoChiec", m.SoChiec),
                    new XElement("DoanhThu", So(m.DoanhThu))))),
                new XElement("DoanhSoNhanVien", bc.TheoNhanVien.Select(n => new XElement("NhanVien",
                    new XElement("MaNV", n.MaNV ?? string.Empty),
                    new XElement("HoTen", n.HoTen),
                    new XElement("SoDon", n.SoDon),
                    new XElement("DoanhThu", So(n.DoanhThu)),
                    new XElement("TyTrong", So(n.TyTrong)))))));
    }

    private static List<DonHoanTat> DocDonHoanTat(DateTime tu, DateTime den)
    {
        var chiTiet = XmlHelper.Load("ChiTietDonHang").Root!.Elements("ChiTietDonHang")
            .ToLookup(ct => (string?)ct.Element("MaDon") ?? string.Empty);

        return XmlHelper.Load("DonHang").Root!.Elements("DonHang")
            .Where(e => (string?)e.Element("TrangThai") == TrangThaiDon.HoanTat)
            .Select(e => new
            {
                MaDon = (string?)e.Element("MaDon") ?? string.Empty,
                MaNV = (string?)e.Element("MaNVPhuTrach"),
                Ngay = DateTime.TryParse((string?)e.Element("NgayHoanTat"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var n) ? n : (DateTime?)null
            })
            .Where(d => d.Ngay >= tu && d.Ngay <= den)
            .Select(d => new DonHoanTat(d.MaDon, string.IsNullOrEmpty(d.MaNV) ? null : d.MaNV, d.Ngay!.Value, chiTiet[d.MaDon].ToList()))
            .ToList();
    }
}
