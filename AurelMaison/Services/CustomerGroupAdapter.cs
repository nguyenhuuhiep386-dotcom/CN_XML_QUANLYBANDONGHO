using AurelMaison.Models;

namespace AurelMaison.Services;

/// <summary>Chuyển dữ liệu từ dịch vụ dùng chung sang view model của khu khách hàng.</summary>
public sealed class CustomerGroupAdapter(AuthService auth, KhachHangService customers,
    DanhMucService catalog, KhoService stock) : IAuthService, IKhachHangService, IDanhMucService, IKhoService
{
    public KetQua DangKy(DangKyForm form) => auth.DangKy(form.HoTen, form.Email, form.MatKhau);

    public CustomerIdentity? DangNhap(string email, string matKhau)
    {
        var account = auth.DangNhap(email, matKhau);
        return account is null ? null : new(account.MaTK, account.Email, account.VaiTro,
            account.MaKH, account.MaNV, account.HoTen);
    }

    public CustomerIdentity? LayTaiKhoan(string? maTK)
    {
        var account = auth.LayTaiKhoan(maTK);
        if (account is null || account.TrangThai != TrangThaiTaiKhoan.HoatDong) return null;
        var customer = customers.LayTheoTaiKhoan(account.MaTK);
        var employee = account.VaiTro is VaiTro.NhanVien or VaiTro.Admin
            ? XmlHelper.Load("NhanVien").Root!.Elements("NhanVien")
                .FirstOrDefault(row => (string?)row.Element("MaTK") == account.MaTK)
            : null;
        return new(account.MaTK, account.Email, account.VaiTro, customer?.MaKH,
            (string?)employee?.Element("MaNV"), customer?.HoTen ?? (string?)employee?.Element("HoTen") ?? account.Email);
    }

    public KetQua DoiMatKhau(string maTK, string cu, string moi) => auth.DoiMatKhau(maTK, cu, moi);

    public HoSo? LayTheoTaiKhoan(string maTK)
    {
        var customer = customers.LayTheoTaiKhoan(maTK);
        return customer is null ? null : new(customer.MaKH, customer.HoTen, customer.SoDienThoai, customer.DiaChi);
    }

    public KetQua CapNhatHoSo(string maKH, HoSoForm form) =>
        customers.CapNhatHoSo(maKH, form.HoTen, form.SoDienThoai, form.DiaChi);

    public IReadOnlyList<Hang> LayHang() => catalog.LayDanhSachHang().Select(h => new Hang(h.MaHang, h.TenHang)).ToList();

    public IReadOnlyList<DongHo> LayDanhSach(LocDongHo loc) =>
        catalog.LayDanhSach(loc.TuKhoa, loc.Hang, loc.GiaTu, loc.GiaDen, loc.LoaiMay, loc.ChatLieu).Select(Map).ToList();

    public DongHo? LayChiTiet(string maMau)
    {
        var model = catalog.LayChiTiet(maMau);
        return model?.TrangThai == TrangThaiMau.DangBan ? Map(model) : null;
    }

    public int DemConHang(string maMau) => stock.DemConHang(maMau);

    private static DongHo Map(MauDongHo model) => new(model.MaMau, model.TenMau, model.MaHang, model.TenHang,
        model.Gia, model.LoaiMay, model.KichThuoc, model.ChatLieu, model.MoTa ?? "", model.HinhAnh ?? "",
        model.ThoiHanBaoHanh, model.SoConHang);
}
