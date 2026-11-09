using AurelMaison.Models;
using AurelMaison.Services;

namespace AurelMaison.Areas.Admin.Models;

// products · Đồng hồ & kho
public class DongHoIndexVM
{
    public List<MauDongHo> DanhSach { get; set; } = [];
    public List<HangSanXuat> Hang { get; set; } = [];
    public string? TuKhoa { get; set; }
    public string? MaHang { get; set; }
    public string? TrangThai { get; set; }

    /// <summary>Hãng đang mở để sửa trong khung hãng; null = khung đang ở chế độ thêm.</summary>
    public HangSanXuat? HangDangSua { get; set; }
}

// editproduct · Thêm / sửa đồng hồ
public class SuaDongHoVM
{
    public MauDongHo Mau { get; set; } = new();
    public bool LaMoi { get; set; }
    public List<HangSanXuat> Hang { get; set; } = [];
}

// inventory · Serial & nhập kho
public class NhapKhoForm
{
    public string? MaMau { get; set; }
    public string? GhiChu { get; set; }
    public List<DongNhapKho> Dong { get; set; } = [];
}

public class KhoIndexVM
{
    public List<ChiecDongHo> DanhSach { get; set; } = [];
    public List<MauDongHo> Mau { get; set; } = [];
    public string? MaMau { get; set; }
    public string? TrangThai { get; set; }
    public Dictionary<string, int> TheoTrangThai { get; set; } = [];
    public NhapKhoForm Form { get; set; } = new();
}

// stockok · Đã ghi nhận nhập kho
public class DaNhapVM
{
    public PhieuNhap Phieu { get; set; } = new();
    public List<ChiecDongHo> Chiec { get; set; } = [];
    public string MaMau { get; set; } = string.Empty;
    public string TenMau { get; set; } = string.Empty;
    public int SoConHangMoi { get; set; }
}

// xmldata · Dữ liệu XML
public class DuLieuVM
{
    public List<BanSaoLuu> SaoLuu { get; set; } = [];
    public List<BanSaoLuu> BanXuat { get; set; } = [];

    public KetQuaKiemTra? KiemTra { get; set; }

    public string? TenBangThu { get; set; }
    public string? TenFileThu { get; set; }
    public List<LoiKiemTra>? KiemTraFile { get; set; }

    public KetQuaDongBo? DongBo { get; set; }

    public string? TenBanXuat { get; set; }
    public List<DongDoiChieu>? DoiChieu { get; set; }

    /// <summary>So doanh thu: trang Báo cáo (XML) với view vw_DoanhThuHoanTat (SQL).</summary>
    public bool CoSoDoanhThu { get; set; }
    public int SoDonXml { get; set; }
    public decimal DoanhThuXml { get; set; }
    public (int SoDon, decimal DoanhThu)? DoanhThuSql { get; set; }
    public string? LoiSql { get; set; }
}

// users · Tài khoản & phân quyền
public class TaoNhanVienForm
{
    public string? HoTen { get; set; }
    public string? Email { get; set; }
    public string? MatKhauTam { get; set; }
    public string? SoDienThoai { get; set; }
    public string? ChucVu { get; set; }
    public string VaiTro { get; set; } = AurelMaison.Models.VaiTro.NhanVien;
}

public class TaiKhoanIndexVM
{
    public List<DongTaiKhoan> DanhSach { get; set; } = [];
    public string? VaiTro { get; set; }
    public DongTaiKhoan? DangChon { get; set; }
    public string? MaTKDangDung { get; set; }
    public TaoNhanVienForm Tao { get; set; } = new();
}
