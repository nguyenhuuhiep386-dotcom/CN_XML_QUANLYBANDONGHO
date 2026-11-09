using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Xsl;

namespace AurelMaison.Services;

/// <summary>Dữ liệu không khớp XSD nên không được ghi xuống file.</summary>
public sealed class XmlKhongHopLeException(string tenFile, IReadOnlyList<string> danhSachLoi)
    : Exception($"{tenFile}.xml không hợp lệ theo {tenFile}.xsd: {string.Join("; ", danhSachLoi)}")
{
    public IReadOnlyList<string> DanhSachLoi { get; } = danhSachLoi;
}

/// <summary>
/// Cổng đọc/ghi duy nhất tới App_Data/Xml. Tham số tenFile là tên không có đuôi, ví dụ "DongHo":
/// dữ liệu ở Xml/DongHo.xml, lược đồ ở Xsd/DongHo.xsd.
/// </summary>
public sealed class XmlHelper
{
    private static readonly Regex TenHopLe = new("^[A-Za-z][A-Za-z0-9]*$", RegexOptions.Compiled);

    private static readonly XmlReaderSettings CauHinhDoc = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null
    };

    private readonly string _thuMucXml;
    private readonly string _thuMucXsd;
    private readonly string _thuMucXslt;
    private readonly ConcurrentDictionary<string, object> _khoaTheoFile = new(StringComparer.OrdinalIgnoreCase);

    public XmlHelper(IWebHostEnvironment env)
    {
        var appData = Path.Combine(env.ContentRootPath, "App_Data");
        _thuMucXml = Path.Combine(appData, "Xml");
        _thuMucXsd = Path.Combine(appData, "Xsd");
        _thuMucXslt = Path.Combine(appData, "Xslt");
    }

    public XDocument Doc(string tenFile)
    {
        var duongDan = DuongDan(_thuMucXml, tenFile, ".xml");
        lock (Khoa(tenFile))
        {
            using var reader = XmlReader.Create(duongDan, CauHinhDoc);
            return XDocument.Load(reader);
        }
    }

    /// <summary>Trả về các lỗi XSD của tài liệu; danh sách rỗng nghĩa là hợp lệ.</summary>
    public IReadOnlyList<string> KiemTra(string tenFile, XDocument taiLieu)
    {
        var schemas = new XmlSchemaSet { XmlResolver = null };
        using (var reader = XmlReader.Create(DuongDan(_thuMucXsd, tenFile, ".xsd"), CauHinhDoc))
        {
            schemas.Add(null, reader);
        }
        schemas.Compile();

        // Gom cả cảnh báo: phần tử không có khai báo trong lược đồ chỉ sinh cảnh báo,
        // bỏ qua thì dữ liệu sai vẫn lọt.
        var danhSachLoi = new List<string>();
        taiLieu.Validate(schemas, (_, e) => danhSachLoi.Add(e.Message));
        return danhSachLoi;
    }

    /// <summary>Kiểm tra XSD rồi ghi đè file. Tài liệu sai thì ném lỗi và giữ nguyên file cũ.</summary>
    public void Ghi(string tenFile, XDocument taiLieu)
    {
        var duongDan = DuongDan(_thuMucXml, tenFile, ".xml");
        lock (Khoa(tenFile))
        {
            var danhSachLoi = KiemTra(tenFile, taiLieu);
            if (danhSachLoi.Count > 0)
            {
                throw new XmlKhongHopLeException(tenFile, danhSachLoi);
            }

            // Ghi ra file tạm rồi đổi tên để lỗi giữa chừng không làm hỏng file đang có.
            var fileTam = duongDan + ".tmp";
            try
            {
                var cauHinhGhi = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
                using (var writer = XmlWriter.Create(fileTam, cauHinhGhi))
                {
                    taiLieu.Save(writer);
                }
                File.Move(fileTam, duongDan, overwrite: true);
            }
            finally
            {
                File.Delete(fileTam);
            }
        }
    }

    /// <summary>Đọc, sửa và ghi trong cùng một khóa để hai yêu cầu đồng thời không đè mất thay đổi của nhau.</summary>
    public void CapNhat(string tenFile, Action<XDocument> thayDoi)
    {
        lock (Khoa(tenFile))
        {
            var taiLieu = Doc(tenFile);
            thayDoi(taiLieu);
            Ghi(tenFile, taiLieu);
        }
    }

    /// <summary>Áp dụng App_Data/Xslt/{tenXslt}.xslt lên tài liệu và trả về chuỗi HTML.</summary>
    public string XuatHtml(string tenXslt, XDocument taiLieu, IReadOnlyDictionary<string, object>? thamSo = null)
    {
        var xslt = new XslCompiledTransform();
        using (var reader = XmlReader.Create(DuongDan(_thuMucXslt, tenXslt, ".xslt"), CauHinhDoc))
        {
            xslt.Load(reader, new XsltSettings(enableDocumentFunction: false, enableScript: false), null);
        }

        var danhSachThamSo = new XsltArgumentList();
        foreach (var (ten, giaTri) in thamSo ?? new Dictionary<string, object>())
        {
            danhSachThamSo.AddParam(ten, string.Empty, giaTri);
        }

        using var ketQua = new StringWriter();
        using (var writer = XmlWriter.Create(ketQua, xslt.OutputSettings))
        using (var nguon = taiLieu.CreateReader())
        {
            xslt.Transform(nguon, danhSachThamSo, writer);
        }
        return ketQua.ToString();
    }

    private object Khoa(string tenFile) => _khoaTheoFile.GetOrAdd(tenFile, _ => new object());

    private static string DuongDan(string thuMuc, string ten, string duoi)
    {
        if (!TenHopLe.IsMatch(ten))
        {
            throw new ArgumentException($"Tên file không hợp lệ: '{ten}'.", nameof(ten));
        }
        return Path.Combine(thuMuc, ten + duoi);
    }
}
