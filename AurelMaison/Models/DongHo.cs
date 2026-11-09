using System.Globalization;
using System.Xml.Linq;

namespace AurelMaison.Models;

/// <summary>Một dòng DongHo.xml (mẫu đồng hồ), kèm tên hãng và số chiếc còn hàng khi đọc ra.</summary>
public class MauDongHo
{
    public string MaMau { get; set; } = string.Empty;
    public string TenMau { get; set; } = string.Empty;
    public string MaHang { get; set; } = string.Empty;
    public decimal Gia { get; set; }
    public string LoaiMay { get; set; } = string.Empty;
    public decimal KichThuoc { get; set; }
    public string ChatLieu { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public int ThoiHanBaoHanh { get; set; }
    public string TrangThai { get; set; } = TrangThaiMau.DangBan;

    // Không lưu trong DongHo.xml.
    public string TenHang { get; set; } = string.Empty;
    public int SoConHang { get; set; }

    public static MauDongHo TuXml(XElement e) => new()
    {
        MaMau = (string?)e.Element("MaMau") ?? string.Empty,
        TenMau = (string?)e.Element("TenMau") ?? string.Empty,
        MaHang = (string?)e.Element("MaHang") ?? string.Empty,
        Gia = SoThapPhan(e.Element("Gia")),
        LoaiMay = (string?)e.Element("LoaiMay") ?? string.Empty,
        KichThuoc = SoThapPhan(e.Element("KichThuoc")),
        ChatLieu = (string?)e.Element("ChatLieu") ?? string.Empty,
        MoTa = (string?)e.Element("MoTa"),
        HinhAnh = (string?)e.Element("HinhAnh"),
        ThoiHanBaoHanh = int.TryParse((string?)e.Element("ThoiHanBaoHanh"), out var bh) ? bh : 0,
        TrangThai = (string?)e.Element("TrangThai") ?? TrangThaiMau.DangBan
    };

    /// <summary>Phần tử theo đúng thứ tự cột SQL; cột rỗng thì bỏ (NULL).</summary>
    public XElement ToXml() => new("DongHo",
        new XElement("MaMau", MaMau),
        new XElement("TenMau", TenMau),
        new XElement("MaHang", MaHang),
        new XElement("Gia", Gia.ToString("0", CultureInfo.InvariantCulture)),
        new XElement("LoaiMay", LoaiMay),
        new XElement("KichThuoc", KichThuoc.ToString("0.0", CultureInfo.InvariantCulture)),
        new XElement("ChatLieu", ChatLieu),
        string.IsNullOrEmpty(MoTa) ? null : new XElement("MoTa", MoTa),
        string.IsNullOrEmpty(HinhAnh) ? null : new XElement("HinhAnh", HinhAnh),
        new XElement("ThoiHanBaoHanh", ThoiHanBaoHanh),
        new XElement("TrangThai", TrangThai));

    internal static decimal SoThapPhan(XElement? e) =>
        decimal.TryParse((string?)e, NumberStyles.Number, CultureInfo.InvariantCulture, out var so) ? so : 0;
}

public class HangSanXuat
{
    public string MaHang { get; set; } = string.Empty;
    public string TenHang { get; set; } = string.Empty;
    public string? QuocGia { get; set; }
    public string? MoTa { get; set; }

    /// <summary>Số mẫu DongHo thuộc hãng (mọi trạng thái); lớn hơn 0 thì không xóa được.</summary>
    public int SoMau { get; set; }

    public static HangSanXuat TuXml(XElement e) => new()
    {
        MaHang = (string?)e.Element("MaHang") ?? string.Empty,
        TenHang = (string?)e.Element("TenHang") ?? string.Empty,
        QuocGia = (string?)e.Element("QuocGia"),
        MoTa = (string?)e.Element("MoTa")
    };
}

/// <summary>Một dòng ChiTietDongHo.xml: một chiếc đồng hồ cụ thể, định danh bằng serial.</summary>
public class ChiecDongHo
{
    public string Serial { get; set; } = string.Empty;
    public string MaMau { get; set; } = string.Empty;
    public string MaPN { get; set; } = string.Empty;
    public string TinhTrang { get; set; } = TinhTrangChiec.Moi;
    public bool CoHop { get; set; }
    public bool CoGiayTo { get; set; }
    public string TrangThai { get; set; } = TrangThaiChiec.ConHang;

    public string TenMau { get; set; } = string.Empty;

    public static ChiecDongHo TuXml(XElement e) => new()
    {
        Serial = (string?)e.Element("Serial") ?? string.Empty,
        MaMau = (string?)e.Element("MaMau") ?? string.Empty,
        MaPN = (string?)e.Element("MaPN") ?? string.Empty,
        TinhTrang = (string?)e.Element("TinhTrang") ?? TinhTrangChiec.Moi,
        CoHop = (string?)e.Element("CoHop") == "true",
        CoGiayTo = (string?)e.Element("CoGiayTo") == "true",
        TrangThai = (string?)e.Element("TrangThai") ?? TrangThaiChiec.ConHang
    };
}

/// <summary>Một dòng trên form nhập kho.</summary>
public class DongNhapKho
{
    public string? Serial { get; set; }
    public string TinhTrang { get; set; } = TinhTrangChiec.Moi;
    public bool CoHop { get; set; } = true;
    public bool CoGiayTo { get; set; } = true;
}

public class PhieuNhap
{
    public string MaPN { get; set; } = string.Empty;
    public DateTime NgayNhap { get; set; }
    public string MaNV { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}
