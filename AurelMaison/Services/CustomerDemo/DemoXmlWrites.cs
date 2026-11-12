using System.Xml.Linq;
using AurelMaison.Models;

namespace AurelMaison.Services.CustomerDemo;

internal static class DemoXmlWrites
{
    // Dùng bên trong ChayTrongKhoa, kiểm tra mọi tài liệu trước khi ghi đơn.
    internal static KetQua GhiNhieu(params (string Ten, XDocument Doc)[] files)
    {
        foreach (var (name, doc) in files)
        {
            var errors = XmlHelper.KiemTraXsd(name, doc);
            if (errors.Count > 0) return CustomerResult.Loi(string.Join("; ", errors));
        }

        var originals = files.ToDictionary(f => f.Ten, f => XmlHelper.Load(f.Ten));
        var written = new List<string>();
        foreach (var (name, doc) in files)
        {
            var result = XmlHelper.Save(name, doc);
            if (!result.ThanhCong)
            {
                foreach (var previous in written.AsEnumerable().Reverse())
                {
                    var restore = XmlHelper.Save(previous, originals[previous]);
                    if (!restore.ThanhCong)
                        return CustomerResult.Loi(result.ThongBao + " Phục hồi " + previous + " thất bại: " + restore.ThongBao);
                }
                return result;
            }
            written.Add(name);
        }
        return CustomerResult.Ok("Đã ghi đơn.");
    }
}
