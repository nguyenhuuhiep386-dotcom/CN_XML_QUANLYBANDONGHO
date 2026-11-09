using AurelMaison.Models;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Xsl;

namespace AurelMaison.Services;

/// <summary>
/// Cổng đọc/ghi duy nhất tới App_Data/Xml. tenBang là tên không có đuôi, ví dụ "DongHo":
/// dữ liệu ở Xml/DongHo.xml, lược đồ ở Xsd/DongHo.xsd. Mỗi dòng là phần tử cùng tên bảng.
/// </summary>
public static class XmlHelper
{
    private static readonly object _khoa = new();
    private static readonly Regex TenHopLe = new("^[A-Za-z][A-Za-z0-9]*$", RegexOptions.Compiled);

    private static readonly XmlReaderSettings CauHinhDoc = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null
    };

    private static string? _thuMucAppData;

    /// <summary>Gọi một lần lúc ứng dụng khởi động, trước mọi thao tác khác.</summary>
    public static void KhoiTao(string thuMucGoc) => _thuMucAppData = Path.Combine(thuMucGoc, "App_Data");

    /// <summary>
    /// Bọc trọn đọc → kiểm tra → sửa → ghi trong một khóa dùng chung toàn ứng dụng.
    /// Lock của C# cho phép lồng trên cùng luồng, nên gọi các hàm của XmlHelper và của dịch vụ khác
    /// bên trong khối này vẫn an toàn.
    /// </summary>
    public static T ChayTrongKhoa<T>(Func<T> thaoTac)
    {
        lock (_khoa)
        {
            return thaoTac();
        }
    }

    public static XDocument Load(string tenBang)
    {
        var duongDan = DuongDan("Xml", tenBang, ".xml");
        // Đọc cũng phải trong khóa: file đang mở để đọc sẽ làm File.Replace của luồng khác thất bại.
        lock (_khoa)
        {
            using var reader = XmlReader.Create(duongDan, CauHinhDoc);
            return XDocument.Load(reader, LoadOptions.SetLineInfo);
        }
    }

    /// <summary>
    /// Trả về các lỗi XSD; danh sách rỗng nghĩa là hợp lệ. Thao tác ghi hai file cần gọi hàm này cho
    /// cả hai trước khi Save file đầu tiên, vì khóa không làm việc ghi hai file thành nguyên tử.
    /// </summary>
    public static IReadOnlyList<string> KiemTraXsd(string tenBang, XDocument doc)
    {
        var schemas = new XmlSchemaSet { XmlResolver = null };
        using (var reader = XmlReader.Create(DuongDan("Xsd", tenBang, ".xsd"), CauHinhDoc))
        {
            schemas.Add(null, reader);
        }
        schemas.Compile();

        // Gom cả cảnh báo: phần tử không có khai báo trong lược đồ chỉ sinh cảnh báo,
        // bỏ qua thì dữ liệu sai vẫn lọt.
        var danhSachLoi = new List<string>();
        doc.Validate(schemas, (nguon, e) =>
            danhSachLoi.Add(nguon is IXmlLineInfo vitri && vitri.HasLineInfo()
                ? $"Dòng {vitri.LineNumber}: {e.Message}"
                : e.Message));
        return danhSachLoi;
    }

    /// <summary>Kiểm tra XSD rồi ghi đè file qua file tạm. Dữ liệu sai thì báo lỗi và giữ nguyên file cũ.</summary>
    public static KetQua Save(string tenBang, XDocument doc) => ThucHienGhi(() =>
    {
        var danhSachLoi = KiemTraXsd(tenBang, doc);
        if (danhSachLoi.Count > 0)
        {
            return ThatBai(string.Join("; ", danhSachLoi));
        }

        var file = DuongDan("Xml", tenBang, ".xml");
        var tmp = file + ".tmp";
        try
        {
            var cauHinhGhi = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
            using (var writer = XmlWriter.Create(tmp, cauHinhGhi))
            {
                doc.Save(writer);
            }

            if (File.Exists(file))
            {
                File.Replace(tmp, file, null);
            }
            else
            {
                File.Move(tmp, file);
            }
        }
        finally
        {
            File.Delete(tmp);
        }
        return new KetQua { ThanhCong = true };
    });

    public static XElement? TimTheoMa(string tenBang, string cotKhoa, string ma) =>
        TimDong(Load(tenBang), tenBang, cotKhoa, ma);

    public static KetQua Them(string tenBang, string cotKhoa, XElement dong) => ThucHienGhi(() =>
    {
        var ma = dong.Element(cotKhoa)?.Value;
        if (string.IsNullOrEmpty(ma))
        {
            return ThatBai($"Thiếu {cotKhoa}.");
        }

        var doc = Load(tenBang);
        if (TimDong(doc, tenBang, cotKhoa, ma) != null)
        {
            return ThatBai($"Mã {ma} đã tồn tại.");
        }

        doc.Root!.Add(dong);
        return VoiMa(Save(tenBang, doc), ma);
    });

    /// <summary>Sửa các cột của một dòng, ví dụ dong => dong.SetElementValue("Gia", 100). Gán null để bỏ cột.</summary>
    public static KetQua Sua(string tenBang, string cotKhoa, string ma, Action<XElement> thayDoi) => ThucHienGhi(() =>
    {
        var doc = Load(tenBang);
        var dong = TimDong(doc, tenBang, cotKhoa, ma);
        if (dong == null)
        {
            return ThatBai($"Không tìm thấy mã {ma}.");
        }

        thayDoi(dong);
        return VoiMa(Save(tenBang, doc), ma);
    });

    /// <summary>Không kiểm tra tham chiếu từ bảng khác; dịch vụ sở hữu bảng phải kiểm tra trước khi gọi.</summary>
    public static KetQua Xoa(string tenBang, string cotKhoa, string ma) => ThucHienGhi(() =>
    {
        var doc = Load(tenBang);
        var dong = TimDong(doc, tenBang, cotKhoa, ma);
        if (dong == null)
        {
            return ThatBai($"Không tìm thấy mã {ma}.");
        }

        dong.Remove();
        return VoiMa(Save(tenBang, doc), ma);
    });

    /// <summary>
    /// Sinh mã kế tiếp: KH004 (coNam = false), AM20260019 (coNam = true). Phải gọi trong cùng khối
    /// ChayTrongKhoa với lần ghi dòng mới, nếu không hai yêu cầu có thể nhận cùng một mã.
    /// </summary>
    public static string SinhMa(string tienTo, string tenBang, string cot, bool coNam = false)
    {
        var dauMa = coNam ? $"{tienTo}{DateTime.Now.Year}" : tienTo;
        var soLonNhat = Load(tenBang).Root!.Elements(tenBang)
            .Select(dong => dong.Element(cot)?.Value ?? string.Empty)
            .Where(ma => ma.StartsWith(dauMa, StringComparison.Ordinal))
            .Select(ma => int.TryParse(ma[dauMa.Length..], out var so) ? so : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"{dauMa}{(soLonNhat + 1).ToString(coNam ? "D4" : "D3")}";
    }

    /// <summary>Áp dụng App_Data/Xslt/{tenXslt}.xslt lên tài liệu và trả về chuỗi HTML.</summary>
    public static string XuatHtml(XDocument xml, string tenXslt)
    {
        var xslt = new XslCompiledTransform();
        using (var reader = XmlReader.Create(DuongDan("Xslt", tenXslt, ".xslt"), CauHinhDoc))
        {
            xslt.Load(reader, new XsltSettings(enableDocumentFunction: false, enableScript: false), null);
        }

        using var ketQua = new StringWriter();
        using (var writer = XmlWriter.Create(ketQua, xslt.OutputSettings))
        using (var nguon = xml.CreateReader())
        {
            xslt.Transform(nguon, writer);
        }
        return ketQua.ToString();
    }

    public static string XuatHtml(string tenBang, string tenXslt) => XuatHtml(Load(tenBang), tenXslt);

    private static XElement? TimDong(XDocument doc, string tenBang, string cotKhoa, string ma) =>
        doc.Root!.Elements(tenBang).FirstOrDefault(dong => dong.Element(cotKhoa)?.Value == ma);

    private static KetQua ThatBai(string thongBao) => new() { ThanhCong = false, ThongBao = thongBao };

    private static KetQua VoiMa(KetQua ketQua, string ma)
    {
        ketQua.Ma = ma;
        return ketQua;
    }

    // Hàm ghi trả KetQua thay vì ném lỗi ra giao diện: file khóa, thiếu XSD, XML hỏng đều thành thông báo.
    private static KetQua ThucHienGhi(Func<KetQua> thaoTac)
    {
        lock (_khoa)
        {
            try
            {
                return thaoTac();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or XmlException or XmlSchemaException)
            {
                return ThatBai(e.Message);
            }
        }
    }

    private static string DuongDan(string thuMuc, string ten, string duoi)
    {
        if (_thuMucAppData == null)
        {
            throw new InvalidOperationException("Chưa gọi XmlHelper.KhoiTao.");
        }
        if (!TenHopLe.IsMatch(ten))
        {
            throw new ArgumentException($"Tên file không hợp lệ: '{ten}'.", nameof(ten));
        }
        return Path.Combine(_thuMucAppData, thuMuc, ten + duoi);
    }
}
