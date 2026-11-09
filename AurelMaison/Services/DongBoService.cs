using AurelMaison.Models;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace AurelMaison.Services;

public class LoiKiemTra
{
    public string Bang { get; set; } = string.Empty;
    public int? Dong { get; set; }
    public string NoiDung { get; set; } = string.Empty;

    /// <summary>Cảnh báo (ví dụ bảng chưa có XSD) không làm kết quả kiểm tra thất bại.</summary>
    public bool LaCanhBao { get; set; }
}

public class KetQuaKiemTra
{
    public List<LoiKiemTra> DanhSach { get; set; } = [];
    public Dictionary<string, int> SoDong { get; set; } = [];
    public bool HopLe => DanhSach.All(l => l.LaCanhBao);
    public int SoLoi => DanhSach.Count(l => !l.LaCanhBao);

    public string TomTat(int toiDa = 5) => string.Join("; ", DanhSach.Where(l => !l.LaCanhBao).Take(toiDa)
        .Select(l => $"{l.Bang}{(l.Dong != null ? $" dòng {l.Dong}" : "")}: {l.NoiDung}"));
}

public class DongDoiChieu
{
    public string Bang { get; set; } = string.Empty;
    public int SoDongXml { get; set; }
    public int SoDongSql { get; set; }
    public List<string> ChiCoTrongXml { get; set; } = [];
    public List<string> ChiCoTrongSql { get; set; } = [];

    /// <summary>Cùng khóa nhưng khác giá trị cột.</summary>
    public List<string> KhacNoiDung { get; set; } = [];

    public bool Khop => SoDongXml == SoDongSql && ChiCoTrongXml.Count == 0 && ChiCoTrongSql.Count == 0 && KhacNoiDung.Count == 0;
}

public class KetQuaDongBo : KetQua
{
    public List<(string Bang, int Xml, int Sql)> SoDong { get; set; } = [];
    public string? BanSaoLuu { get; set; }
    public KetQuaKiemTra? KiemTra { get; set; }
}

public class BanSaoLuu
{
    public string Ten { get; set; } = string.Empty;
    public DateTime ThoiGian { get; set; }
    public int SoFile { get; set; }
}

/// <summary>
/// XML là dữ liệu vận hành; SQL Server chỉ để khởi tạo (SQL → XML) và đồng bộ (XML → SQL).
/// Kiểm tra toàn bộ, sao lưu, phục hồi, xuất/nạp SQL, đối chiếu. Mọi thao tác ghi đều chạy trong khóa của XmlHelper.
/// </summary>
public class DongBoService(IConfiguration cauHinh)
{
    private record KhoaNgoai(string Cot, string BangCha);
    private record DinhNghiaBang(string Ten, string[] Khoa, KhoaNgoai[] KhoaNgoai, string[][] DuyNhat);

    /// <summary>12 bảng theo thứ tự khóa ngoại: nạp xuôi, xóa ngược. Khớp với QuanLyBanDongHo.sql.</summary>
    private static readonly DinhNghiaBang[] CacBang =
    [
        new("TaiKhoan", ["MaTK"], [], [["Email"]]),
        new("KhachHang", ["MaKH"], [new("MaTK", "TaiKhoan")], [["MaTK"]]),
        new("NhanVien", ["MaNV"], [new("MaTK", "TaiKhoan")], [["MaTK"]]),
        new("HangSanXuat", ["MaHang"], [], [["TenHang"]]),
        new("DongHo", ["MaMau"], [new("MaHang", "HangSanXuat")], []),
        new("PhieuNhap", ["MaPN"], [new("MaNV", "NhanVien")], []),
        new("ChiTietDongHo", ["Serial"], [new("MaMau", "DongHo"), new("MaPN", "PhieuNhap")], []),
        new("LichHen", ["MaLH"], [new("MaKH", "KhachHang"), new("MaMau", "DongHo"), new("MaNVXuLy", "NhanVien")], []),
        new("DonHang", ["MaDon"], [new("MaKH", "KhachHang"), new("MaNVLap", "NhanVien"), new("MaNVPhuTrach", "NhanVien")], []),
        new("ChiTietDonHang", ["MaDon", "STT"], [new("MaDon", "DonHang"), new("MaMau", "DongHo"), new("Serial", "ChiTietDongHo")], []),
        new("HoaDon", ["MaHD"], [new("MaDon", "DonHang"), new("MaNV", "NhanVien")], [["MaDon"]]),
        new("PhieuBaoHanh", ["MaPBH"], [new("MaDon", "DonHang"), new("Serial", "ChiTietDongHo"), new("MaKH", "KhachHang"), new("MaNVLap", "NhanVien")], [["MaDon", "Serial"]])
    ];

    private static readonly Regex TenThuMucHopLe = new(@"^\d{8}_\d{6}(_\d+)?$", RegexOptions.Compiled);

    public static IReadOnlyList<string> DanhSachBang { get; } = CacBang.Select(b => b.Ten).ToList();

    private static string ThuMucXml => Path.Combine(XmlHelper.ThuMucAppData, "Xml");
    private static string ThuMucSaoLuu => Path.Combine(XmlHelper.ThuMucAppData, "Backup");
    private static string ThuMucKiemTra => Path.Combine(XmlHelper.ThuMucAppData, "KiemTra");

    private string ChuoiKetNoi => cauHinh.GetConnectionString("QuanLyBanDongHo")
        ?? throw new InvalidOperationException("Thiếu ConnectionStrings:QuanLyBanDongHo trong appsettings.json.");

    // ===================== Kiểm tra =====================

    /// <summary>Dữ liệu đang chạy: đủ 12 file, đúng XSD, không trùng khóa, mọi tham chiếu tồn tại.</summary>
    public KetQuaKiemTra KiemTraToanBo() => XmlHelper.ChayTrongKhoa(() => KiemTraThuMuc(ThuMucXml));

    private static KetQuaKiemTra KiemTraThuMuc(string thuMuc)
    {
        var kq = new KetQuaKiemTra();
        var docs = new Dictionary<string, XDocument>();

        foreach (var b in CacBang)
        {
            var file = Path.Combine(thuMuc, b.Ten + ".xml");
            if (!File.Exists(file))
            {
                kq.DanhSach.Add(new LoiKiemTra { Bang = b.Ten, NoiDung = "Thiếu file " + b.Ten + ".xml." });
                continue;
            }
            XDocument doc;
            try
            {
                doc = XmlHelper.DocFile(file);
            }
            catch (XmlException e)
            {
                kq.DanhSach.Add(new LoiKiemTra { Bang = b.Ten, Dong = e.LineNumber, NoiDung = "XML không đọc được: " + e.Message });
                continue;
            }
            kq.DanhSach.AddRange(KiemTraMotBang(b, doc));
            docs[b.Ten] = doc;
            kq.SoDong[b.Ten] = doc.Root?.Elements(b.Ten).Count() ?? 0;
        }

        // Tham chiếu: chỉ khi file bảng cha đọc được.
        var khoaTheoBang = docs.ToDictionary(
            p => p.Key,
            p => p.Value.Root!.Elements(p.Key).Select(e => (string?)e.Element(CacBang.First(b => b.Ten == p.Key).Khoa[0]) ?? "").ToHashSet());
        foreach (var b in CacBang.Where(b => docs.ContainsKey(b.Ten)))
        {
            foreach (var dong in docs[b.Ten].Root!.Elements(b.Ten))
            {
                foreach (var fk in b.KhoaNgoai)
                {
                    var giaTri = (string?)dong.Element(fk.Cot);
                    if (string.IsNullOrEmpty(giaTri) || !khoaTheoBang.TryGetValue(fk.BangCha, out var cha) || cha.Contains(giaTri))
                    {
                        continue;
                    }
                    kq.DanhSach.Add(new LoiKiemTra
                    {
                        Bang = b.Ten,
                        Dong = SoDong(dong),
                        NoiDung = $"{fk.Cot} = {giaTri} không có trong {fk.BangCha}."
                    });
                }
            }
        }
        return kq;
    }

    /// <summary>Thử một file XML tải lên với XSD và khóa chính của bảng tenBang, không ghi gì.</summary>
    public List<LoiKiemTra> KiemTraFile(string tenBang, Stream luong)
    {
        var b = CacBang.FirstOrDefault(x => x.Ten == tenBang);
        if (b == null)
        {
            return [new LoiKiemTra { Bang = tenBang, NoiDung = "Không có bảng này." }];
        }
        try
        {
            return KiemTraMotBang(b, XmlHelper.DocFile(luong));
        }
        catch (XmlException e)
        {
            return [new LoiKiemTra { Bang = tenBang, Dong = e.LineNumber, NoiDung = "XML không đọc được: " + e.Message }];
        }
    }

    private static List<LoiKiemTra> KiemTraMotBang(DinhNghiaBang b, XDocument doc)
    {
        var loi = new List<LoiKiemTra>();
        if (doc.Root?.Name.LocalName != "DanhSach" + b.Ten)
        {
            loi.Add(new LoiKiemTra { Bang = b.Ten, NoiDung = $"Phần tử gốc phải là <DanhSach{b.Ten}>." });
            return loi;
        }

        if (XmlHelper.CoXsd(b.Ten))
        {
            loi.AddRange(XmlHelper.KiemTraXsd(b.Ten, doc).Select(l => TachDong(b.Ten, l)));
        }
        else
        {
            loi.Add(new LoiKiemTra { Bang = b.Ten, LaCanhBao = true, NoiDung = "Chưa có XSD, chỉ kiểm tra khóa và tham chiếu." });
            foreach (var la in doc.Root.Elements().Where(e => e.Name.LocalName != b.Ten))
            {
                loi.Add(new LoiKiemTra { Bang = b.Ten, Dong = SoDong(la), NoiDung = $"Phần tử <{la.Name.LocalName}> không phải <{b.Ten}>." });
            }
        }

        // Khóa chính và cột duy nhất (kể cả bảng chưa có XSD).
        foreach (var cot in new[] { b.Khoa }.Concat(b.DuyNhat))
        {
            var laKhoaChinh = cot == b.Khoa;
            var daGap = new HashSet<string>();
            foreach (var dong in doc.Root.Elements(b.Ten))
            {
                var giaTri = cot.Select(c => (string?)dong.Element(c)).ToArray();
                if (giaTri.Any(string.IsNullOrEmpty))
                {
                    if (laKhoaChinh)
                    {
                        loi.Add(new LoiKiemTra { Bang = b.Ten, Dong = SoDong(dong), NoiDung = $"Thiếu khóa chính {string.Join(", ", cot)}." });
                    }
                    continue;
                }
                var khoa = string.Join(" | ", giaTri.Select(g => cot.Length == 1 && b.Ten == "TaiKhoan" ? g!.ToLowerInvariant() : g));
                if (!daGap.Add(khoa))
                {
                    loi.Add(new LoiKiemTra
                    {
                        Bang = b.Ten,
                        Dong = SoDong(dong),
                        NoiDung = $"Trùng {(laKhoaChinh ? "khóa chính" : "giá trị duy nhất")} {string.Join(", ", cot)} = {khoa}."
                    });
                }
            }
        }
        return loi;
    }

    // ===================== Sao lưu / phục hồi =====================

    /// <summary>Chép App_Data/Xml sang App_Data/Backup/yyyyMMdd_HHmmss trong khóa. Ma = tên bản sao lưu.</summary>
    public KetQua SaoLuu() => XmlHelper.ChayTrongKhoa(() =>
    {
        try
        {
            var ten = TenThuMucMoi(ThuMucSaoLuu);
            var dich = Path.Combine(ThuMucSaoLuu, ten);
            Directory.CreateDirectory(dich);
            foreach (var bang in DanhSachBang)
            {
                var nguon = Path.Combine(ThuMucXml, bang + ".xml");
                if (File.Exists(nguon))
                {
                    File.Copy(nguon, Path.Combine(dich, bang + ".xml"));
                }
            }
            return new KetQua { ThanhCong = true, Ma = ten, ThongBao = $"Đã sao lưu vào Backup/{ten}." };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new KetQua { ThongBao = "Sao lưu lỗi: " + e.Message };
        }
    });

    public List<BanSaoLuu> LayDanhSachSaoLuu() => LietKe(ThuMucSaoLuu);

    public List<BanSaoLuu> LayDanhSachKiemTra() => LietKe(ThuMucKiemTra);

    /// <summary>Kiểm tra bản sao lưu → sao lưu bản hiện tại → chép đè, tất cả trong khóa.</summary>
    public KetQua PhucHoi(string tenBanSaoLuu)
    {
        if (!TenThuMucHopLe.IsMatch(tenBanSaoLuu ?? string.Empty) || !Directory.Exists(Path.Combine(ThuMucSaoLuu, tenBanSaoLuu!)))
        {
            return new KetQua { ThongBao = "Không tìm thấy bản sao lưu." };
        }
        var nguon = Path.Combine(ThuMucSaoLuu, tenBanSaoLuu!);

        return XmlHelper.ChayTrongKhoa(() =>
        {
            var kt = KiemTraThuMuc(nguon);
            if (!kt.HopLe)
            {
                return new KetQua { ThongBao = $"Bản sao lưu {tenBanSaoLuu} không hợp lệ, không phục hồi: {kt.TomTat()}" };
            }
            var sl = SaoLuu();
            if (!sl.ThanhCong)
            {
                return sl;
            }
            var loi = ChepDe(nguon, sl.Ma);
            return loi != null
                ? new KetQua { ThongBao = $"Phục hồi lỗi: {loi}" }
                : new KetQua { ThanhCong = true, Ma = tenBanSaoLuu!, ThongBao = $"Đã phục hồi {tenBanSaoLuu}. Bản trước khi phục hồi ở Backup/{sl.Ma}." };
        });
    }

    // ===================== XML → SQL =====================

    /// <summary>
    /// Sao lưu → kiểm tra toàn bộ → một SqlTransaction: xóa 12 bảng theo thứ tự ngược khóa ngoại, nạp lại theo thứ tự xuôi,
    /// đếm số dòng từng bảng khớp XML → commit. Lỗi thì rollback, SQL giữ nguyên.
    /// </summary>
    public KetQuaDongBo XmlVaoSql() => XmlHelper.ChayTrongKhoa(() =>
    {
        var sl = SaoLuu();
        if (!sl.ThanhCong)
        {
            return new KetQuaDongBo { ThongBao = sl.ThongBao };
        }
        var kt = KiemTraThuMuc(ThuMucXml);
        if (!kt.HopLe)
        {
            return new KetQuaDongBo { BanSaoLuu = sl.Ma, KiemTra = kt, ThongBao = $"XML có {kt.SoLoi} lỗi, chưa đồng bộ: {kt.TomTat()}" };
        }

        var ketQua = new KetQuaDongBo { BanSaoLuu = sl.Ma, KiemTra = kt };
        string bangDangNap = "", viTri = "";
        try
        {
            using var ketNoi = new SqlConnection(ChuoiKetNoi);
            ketNoi.Open();
            using var gd = ketNoi.BeginTransaction();
            try
            {
                var kieuCot = DocKieuCot(ketNoi, gd);

                foreach (var b in CacBang.Reverse())
                {
                    bangDangNap = b.Ten;
                    using var xoa = new SqlCommand($"DELETE FROM dbo.[{b.Ten}]", ketNoi, gd);
                    xoa.ExecuteNonQuery();
                }

                foreach (var b in CacBang)
                {
                    bangDangNap = b.Ten;
                    if (!kieuCot.TryGetValue(b.Ten, out var cotSql))
                    {
                        throw new InvalidOperationException($"SQL không có bảng dbo.{b.Ten}.");
                    }
                    var dsDong = XmlHelper.DocFile(Path.Combine(ThuMucXml, b.Ten + ".xml")).Root!.Elements(b.Ten).ToList();
                    foreach (var dong in dsDong)
                    {
                        viTri = $" dòng {SoDong(dong)}";
                        using var lenh = new SqlCommand { Connection = ketNoi, Transaction = gd };
                        var cot = dong.Elements().ToList();
                        for (var i = 0; i < cot.Count; i++)
                        {
                            var ten = cot[i].Name.LocalName;
                            if (!cotSql.TryGetValue(ten, out var kieu))
                            {
                                throw new InvalidOperationException($"Cột {ten} không có trong bảng SQL.");
                            }
                            lenh.Parameters.AddWithValue("@p" + i, ChuyenSangSql(kieu, cot[i].Value, ten));
                        }
                        lenh.CommandText = $"INSERT INTO dbo.[{b.Ten}] ({string.Join(", ", cot.Select(c => $"[{c.Name.LocalName}]"))}) " +
                                           $"VALUES ({string.Join(", ", cot.Select((_, i) => "@p" + i))})";
                        lenh.ExecuteNonQuery();
                    }
                    viTri = "";

                    using var dem = new SqlCommand($"SELECT COUNT(*) FROM dbo.[{b.Ten}]", ketNoi, gd);
                    var soSql = (int)dem.ExecuteScalar()!;
                    ketQua.SoDong.Add((b.Ten, dsDong.Count, soSql));
                    if (soSql != dsDong.Count)
                    {
                        throw new InvalidOperationException($"Số dòng lệch: XML {dsDong.Count}, SQL {soSql}.");
                    }
                }
                gd.Commit();
            }
            catch
            {
                gd.Rollback();
                throw;
            }
        }
        catch (Exception e) when (e is SqlException or InvalidOperationException or FormatException or OverflowException or IOException or XmlException)
        {
            ketQua.SoDong.Clear();
            ketQua.ThongBao = string.IsNullOrEmpty(bangDangNap)
                ? $"Không kết nối được SQL Server: {e.Message}"
                : $"Lỗi ở bảng {bangDangNap}{viTri}: {e.Message} Đã hoàn tác, SQL giữ nguyên.";
            return ketQua;
        }

        ketQua.ThanhCong = true;
        ketQua.ThongBao = $"Đã đồng bộ {ketQua.SoDong.Sum(s => s.Sql)} dòng của 12 bảng từ XML vào SQL. Sao lưu trước đó: Backup/{sl.Ma}.";
        return ketQua;
    });

    // ===================== SQL → XML =====================

    /// <summary>
    /// Xuất 12 bảng SQL ra App_Data/KiemTra/&lt;thời điểm&gt;. khoiTao = true: kiểm tra bản xuất, sao lưu XML hiện tại
    /// rồi chép đè vào App_Data/Xml. Ma = tên thư mục xuất.
    /// </summary>
    public KetQua SqlRaXml(bool khoiTao)
    {
        string ten;
        string thuMuc;
        try
        {
            Directory.CreateDirectory(ThuMucKiemTra);
            ten = TenThuMucMoi(ThuMucKiemTra);
            thuMuc = Path.Combine(ThuMucKiemTra, ten);
            Directory.CreateDirectory(thuMuc);

            using var ketNoi = new SqlConnection(ChuoiKetNoi);
            ketNoi.Open();
            var kieuCot = DocKieuCot(ketNoi, null);
            foreach (var b in CacBang)
            {
                using var lenh = new SqlCommand($"SELECT * FROM dbo.[{b.Ten}] ORDER BY {string.Join(", ", b.Khoa.Select(k => $"[{k}]"))}", ketNoi);
                using var doc = lenh.ExecuteReader();
                var goc = new XElement("DanhSach" + b.Ten);
                while (doc.Read())
                {
                    var dong = new XElement(b.Ten);
                    for (var i = 0; i < doc.FieldCount; i++)
                    {
                        if (doc.IsDBNull(i))
                        {
                            continue; // NULL → không có phần tử, giống FOR XML ... ELEMENTS
                        }
                        var tenCot = doc.GetName(i);
                        dong.Add(new XElement(tenCot, ChuyenSangXml(kieuCot[b.Ten][tenCot], doc.GetValue(i))));
                    }
                    goc.Add(dong);
                }
                XmlHelper.GhiFile(new XDocument(new XDeclaration("1.0", "utf-8", null), goc), Path.Combine(thuMuc, b.Ten + ".xml"));
            }
        }
        catch (Exception e) when (e is SqlException or InvalidOperationException or IOException)
        {
            return new KetQua { ThongBao = "Xuất SQL lỗi: " + e.Message };
        }

        if (!khoiTao)
        {
            return new KetQua { ThanhCong = true, Ma = ten, ThongBao = $"Đã xuất SQL ra KiemTra/{ten}." };
        }

        return XmlHelper.ChayTrongKhoa(() =>
        {
            var kt = KiemTraThuMuc(thuMuc);
            if (!kt.HopLe)
            {
                return new KetQua { Ma = ten, ThongBao = $"Dữ liệu SQL không qua kiểm tra, chưa ghi đè XML: {kt.TomTat()}" };
            }
            var sl = SaoLuu();
            if (!sl.ThanhCong)
            {
                return sl;
            }
            var loi = ChepDe(thuMuc, sl.Ma);
            return loi != null
                ? new KetQua { Ma = ten, ThongBao = $"Khởi tạo lỗi: {loi}" }
                : new KetQua { ThanhCong = true, Ma = ten, ThongBao = $"Đã khởi tạo XML từ SQL. XML cũ đã sao lưu ở Backup/{sl.Ma}." };
        });
    }

    // ===================== Đối chiếu =====================

    /// <summary>So số dòng, danh sách khóa và nội dung từng dòng giữa XML đang chạy và một bản xuất từ SQL.</summary>
    public List<DongDoiChieu> DoiChieu(string tenThuMucKiemTra)
    {
        if (!TenThuMucHopLe.IsMatch(tenThuMucKiemTra ?? string.Empty))
        {
            return [];
        }
        var thuMuc = Path.Combine(ThuMucKiemTra, tenThuMucKiemTra!);
        var ketQua = new List<DongDoiChieu>();
        foreach (var b in CacBang)
        {
            var xml = TheoKhoa(b, XmlHelper.Load(b.Ten));
            var file = Path.Combine(thuMuc, b.Ten + ".xml");
            var sql = File.Exists(file) ? TheoKhoa(b, XmlHelper.DocFile(file)) : [];
            ketQua.Add(new DongDoiChieu
            {
                Bang = b.Ten,
                SoDongXml = xml.Count,
                SoDongSql = sql.Count,
                ChiCoTrongXml = xml.Keys.Except(sql.Keys).OrderBy(k => k).ToList(),
                ChiCoTrongSql = sql.Keys.Except(xml.Keys).OrderBy(k => k).ToList(),
                KhacNoiDung = xml.Keys.Intersect(sql.Keys).Where(k => xml[k] != sql[k]).OrderBy(k => k).ToList()
            });
        }
        return ketQua;
    }

    /// <summary>Số đơn và doanh thu trong view vw_DoanhThuHoanTat, để so với trang Báo cáo sau khi đồng bộ.</summary>
    public (int SoDon, decimal DoanhThu)? DoanhThuSql(out string? loi)
    {
        loi = null;
        try
        {
            using var ketNoi = new SqlConnection(ChuoiKetNoi);
            ketNoi.Open();
            using var lenh = new SqlCommand("SELECT COUNT(DISTINCT MaDon), ISNULL(SUM(DonGia), 0) FROM dbo.vw_DoanhThuHoanTat", ketNoi);
            using var doc = lenh.ExecuteReader();
            doc.Read();
            return (doc.GetInt32(0), doc.GetDecimal(1));
        }
        catch (Exception e) when (e is SqlException or InvalidOperationException)
        {
            loi = e.Message;
            return null;
        }
    }

    // ===================== Nội bộ =====================

    private static Dictionary<string, Dictionary<string, string>> DocKieuCot(SqlConnection ketNoi, SqlTransaction? gd)
    {
        using var lenh = new SqlCommand(
            "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo'", ketNoi, gd);
        using var doc = lenh.ExecuteReader();
        var kq = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        while (doc.Read())
        {
            var bang = doc.GetString(0);
            if (!kq.TryGetValue(bang, out var cot))
            {
                kq[bang] = cot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            cot[doc.GetString(1)] = doc.GetString(2);
        }
        return kq;
    }

    private static object ChuyenSangSql(string kieu, string giaTri, string tenCot) => kieu switch
    {
        "bit" => giaTri switch
        {
            "true" or "1" => true,
            "false" or "0" => false,
            _ => throw new FormatException($"Cột {tenCot}: '{giaTri}' không phải true/false.")
        },
        "int" => int.Parse(giaTri, CultureInfo.InvariantCulture),
        "bigint" => long.Parse(giaTri, CultureInfo.InvariantCulture),
        "decimal" or "numeric" or "money" => decimal.Parse(giaTri, NumberStyles.Number, CultureInfo.InvariantCulture),
        "date" => DateTime.Parse(giaTri, CultureInfo.InvariantCulture).Date,
        "datetime" or "datetime2" or "smalldatetime" => DateTime.Parse(giaTri, CultureInfo.InvariantCulture),
        _ => giaTri
    };

    private static string ChuyenSangXml(string kieu, object giaTri) => giaTri switch
    {
        bool b => b ? "true" : "false",
        DateTime d when kieu == "date" => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime d => d.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => giaTri.ToString() ?? string.Empty
    };

    /// <summary>Khóa → nội dung chuẩn hóa (các cột sắp theo tên) để so hai bản.</summary>
    private static Dictionary<string, string> TheoKhoa(DinhNghiaBang b, XDocument doc)
    {
        var kq = new Dictionary<string, string>();
        foreach (var dong in doc.Root?.Elements(b.Ten) ?? [])
        {
            var khoa = string.Join(" | ", b.Khoa.Select(k => (string?)dong.Element(k) ?? ""));
            kq[khoa] = string.Join("\n", dong.Elements().OrderBy(e => e.Name.LocalName, StringComparer.Ordinal)
                .Select(e => e.Name.LocalName + "=" + e.Value));
        }
        return kq;
    }

    /// <summary>
    /// Chép 12 file từ thư mục nguồn vào App_Data/Xml, gọi trong khóa. Chép hết ra file tạm trước rồi mới thay;
    /// thay hỏng giữa chừng thì chép lại bản sao lưu banSaoLuu để không để XML nửa cũ nửa mới. Trả lỗi hoặc null.
    /// </summary>
    private static string? ChepDe(string nguon, string banSaoLuu)
    {
        var tmp = DanhSachBang.ToDictionary(b => b, b => Path.Combine(ThuMucXml, b + ".xml.tmp"));
        try
        {
            foreach (var bang in DanhSachBang)
            {
                File.Copy(Path.Combine(nguon, bang + ".xml"), tmp[bang], overwrite: true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            XoaTam(tmp.Values);
            return "Không chép được file nguồn, XML giữ nguyên: " + e.Message;
        }

        try
        {
            foreach (var bang in DanhSachBang)
            {
                ThayFile(tmp[bang], Path.Combine(ThuMucXml, bang + ".xml"));
            }
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            XoaTam(tmp.Values);
            var thuMucSaoLuu = Path.Combine(ThuMucSaoLuu, banSaoLuu);
            try
            {
                foreach (var bang in DanhSachBang)
                {
                    File.Copy(Path.Combine(thuMucSaoLuu, bang + ".xml"), Path.Combine(ThuMucXml, bang + ".xml"), overwrite: true);
                }
                return "Ghi đè lỗi giữa chừng, đã trả lại XML từ Backup/" + banSaoLuu + ": " + e.Message;
            }
            catch (Exception e2) when (e2 is IOException or UnauthorizedAccessException)
            {
                return $"Ghi đè lỗi ({e.Message}) và không tự trả lại được ({e2.Message}). Hãy Phục hồi Backup/{banSaoLuu}.";
            }
        }
    }

    /// <summary>Thử lại vài lần: trình quét virus/lập chỉ mục có thể giữ file vừa ghi trong chốc lát.</summary>
    private static void ThayFile(string tmp, string dich)
    {
        for (var lan = 1; ; lan++)
        {
            try
            {
                if (File.Exists(dich))
                {
                    File.Replace(tmp, dich, null);
                }
                else
                {
                    File.Move(tmp, dich);
                }
                return;
            }
            catch (Exception e) when (lan < 5 && e is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(100 * lan);
            }
        }
    }

    private static void XoaTam(IEnumerable<string> dsFile)
    {
        foreach (var f in dsFile)
        {
            try { File.Delete(f); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }

    private static List<BanSaoLuu> LietKe(string thuMuc) =>
        !Directory.Exists(thuMuc) ? [] :
        Directory.GetDirectories(thuMuc)
            .Select(d => new DirectoryInfo(d))
            .Where(d => TenThuMucHopLe.IsMatch(d.Name))
            .Select(d => new BanSaoLuu
            {
                Ten = d.Name,
                ThoiGian = DateTime.TryParseExact(d.Name[..15], "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : d.CreationTime,
                SoFile = d.GetFiles("*.xml").Length
            })
            .OrderByDescending(b => b.Ten)
            .ToList();

    private static string TenThuMucMoi(string cha)
    {
        var goc = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var ten = goc;
        for (var i = 2; Directory.Exists(Path.Combine(cha, ten)); i++)
        {
            ten = $"{goc}_{i}";
        }
        return ten;
    }

    private static int? SoDong(XElement e) => e is IXmlLineInfo v && v.HasLineInfo() ? v.LineNumber : null;

    private static LoiKiemTra TachDong(string bang, string thongBao)
    {
        var m = Regex.Match(thongBao, @"^Dòng (\d+): (.*)$", RegexOptions.Singleline);
        return m.Success
            ? new LoiKiemTra { Bang = bang, Dong = int.Parse(m.Groups[1].Value), NoiDung = m.Groups[2].Value }
            : new LoiKiemTra { Bang = bang, NoiDung = thongBao };
    }
}
