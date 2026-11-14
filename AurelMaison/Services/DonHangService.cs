using AurelMaison.Models;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace AurelMaison.Services;

public partial class DonHangService(DanhMucService danhMuc, KhoService kho)
{
    public KetQuaTaoDon TaoDonOnline(string maKH, IEnumerable<MucDatHang>? gio,
        ThongTinNhanHang? nhan) => XmlHelper.ChayTrongKhoa(() =>
    {
        try
        {
            var muc = gio?.ToList() ?? [];
            if (muc.Count == 0 || muc.Any(x => x == null || string.IsNullOrWhiteSpace(x.MaMau) || x.SoLuong <= 0))
                return Loi("Giỏ hàng phải có sản phẩm và số lượng lớn hơn 0.");
            if (nhan == null || string.IsNullOrWhiteSpace(nhan.TenNguoiNhan) || string.IsNullOrWhiteSpace(nhan.SoDienThoai))
                return Loi("Nhập họ tên và số điện thoại người nhận.");
            if (nhan.HinhThucNhan is not ("GiaoTanNoi" or "NhanTaiShowroom"))
                return Loi("Hình thức nhận hàng không hợp lệ.");
            if (nhan.HinhThucNhan == "GiaoTanNoi" && string.IsNullOrWhiteSpace(nhan.DiaChiNhan)
                || nhan.HinhThucNhan == "NhanTaiShowroom" && string.IsNullOrWhiteSpace(nhan.Showroom))
                return Loi("Nhập địa chỉ giao hàng hoặc showroom nhận hàng.");
            if (string.IsNullOrWhiteSpace(maKH) || XmlHelper.TimTheoMa("KhachHang", "MaKH", maKH) == null)
                return Loi("Không tìm thấy khách hàng.");

            var don = XmlHelper.Load("DonHang");
            var chiTiet = XmlHelper.Load("ChiTietDonHang");
            var donCu = new XDocument(don);
            var ma = XmlHelper.SinhMa("AM", "DonHang", "MaDon", coNam: true);
            var stt = 0;
            decimal tong = 0;
            foreach (var nhom in muc.GroupBy(x => x.MaMau))
            {
                var soLuong = nhom.Sum(x => (long)x.SoLuong);
                var mau = danhMuc.LayChiTiet(nhom.Key);
                if (mau == null || mau.TrangThai != TrangThaiMau.DangBan || mau.Gia <= 0)
                    return Loi($"Mẫu {nhom.Key} không còn được bán.");
                if (soLuong > kho.DemConHang(nhom.Key))
                    return Loi($"Mẫu {nhom.Key} không đủ hàng.");
                for (long i = 0; i < soLuong; i++)
                    chiTiet.Root!.Add(new XElement("ChiTietDonHang",
                        new XElement("MaDon", ma), new XElement("STT", ++stt),
                        new XElement("MaMau", mau.MaMau), new XElement("TenMau", mau.TenMau),
                        new XElement("DonGia", mau.Gia), new XElement("ThoiHanBaoHanh", mau.ThoiHanBaoHanh)));
                tong += mau.Gia * soLuong;
            }
            don.Root!.Add(new XElement("DonHang",
                new XElement("MaDon", ma), new XElement("MaKH", maKH), new XElement("NguonDon", "Online"),
                new XElement("NgayDat", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                new XElement("TenNguoiNhan", nhan.TenNguoiNhan.Trim()), new XElement("SoDienThoai", nhan.SoDienThoai.Trim()),
                new XElement("HinhThucNhan", nhan.HinhThucNhan),
                nhan.HinhThucNhan == "GiaoTanNoi" ? new XElement("DiaChiNhan", nhan.DiaChiNhan!.Trim()) : new XElement("Showroom", nhan.Showroom!.Trim()),
                new XElement("TongTien", tong), new XElement("TrangThai", "ChoXacNhan")));

            var loi = XmlHelper.KiemTraXsd("DonHang", don).Concat(XmlHelper.KiemTraXsd("ChiTietDonHang", chiTiet)).ToList();
            if (loi.Count > 0) return Loi(string.Join("; ", loi));
            var kq = XmlHelper.Save("DonHang", don);
            if (!kq.ThanhCong) return Loi(kq.ThongBao);
            kq = XmlHelper.Save("ChiTietDonHang", chiTiet);
            if (!kq.ThanhCong)
            {
                var phucHoi = XmlHelper.Save("DonHang", donCu);
                return Loi(kq.ThongBao + (phucHoi.ThanhCong ? "" : " Không phục hồi được đơn: " + phucHoi.ThongBao));
            }
            return new KetQuaTaoDon { ThanhCong = true, Ma = ma, TongTien = tong, ThongBao = "Đã tạo đơn hàng." };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or XmlException or XmlSchemaException or OverflowException)
        {
            return Loi("Không tạo được đơn hàng: " + e.Message);
        }
    });

    public List<XElement> LayTheoKhach(string maKH) => XmlHelper.ChayTrongKhoa(() =>
        XmlHelper.Load("DonHang").Root!.Elements("DonHang")
            .Where(x => !string.IsNullOrWhiteSpace(maKH) && (string?)x.Element("MaKH") == maKH)
            .OrderByDescending(x => (string?)x.Element("NgayDat")).Select(x => new XElement(x)).ToList());

    // Bắt buộc truyền mã khách từ phiên; không cho khách đọc đơn của người khác.
    public XElement? LayChiTietTheoKhach(string maDon, string maKH) => XmlHelper.ChayTrongKhoa(() =>
    {
        var don = LayTheoKhach(maKH).FirstOrDefault(x => (string?)x.Element("MaDon") == maDon);
        if (don == null) return null;
        don.Add(new XElement("ChiTiet", XmlHelper.Load("ChiTietDonHang").Root!.Elements("ChiTietDonHang")
            .Where(x => (string?)x.Element("MaDon") == maDon)));
        return don;
    });

    private static KetQuaTaoDon Loi(string thongBao) => new() { ThongBao = thongBao };
}
