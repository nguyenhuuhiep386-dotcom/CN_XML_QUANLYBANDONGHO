using AurelMaison.Models;

namespace AurelMaison.Services;

public interface IAuthService
{
    KetQua DangKy(DangKyForm form);
    CustomerIdentity? DangNhap(string email, string matKhau);
    CustomerIdentity? LayTaiKhoan(string? maTK);
    KetQua DoiMatKhau(string maTK, string cu, string moi);
}

public interface IKhachHangService
{
    HoSo? LayTheoTaiKhoan(string maTK);
    KetQua CapNhatHoSo(string maKH, HoSoForm form);
}

public interface IDanhMucService
{
    IReadOnlyList<Hang> LayHang();
    IReadOnlyList<DongHo> LayDanhSach(LocDongHo loc);
    DongHo? LayChiTiet(string maMau);
}

public interface IKhoService
{
    int DemConHang(string maMau);
}

public interface IDonHangService
{
    KetQuaTaoDon TaoDonOnline(string maKH, IReadOnlyList<MucGio> gio, DatHangForm form);
    IReadOnlyList<DonHang> LayTheoKhach(string maKH);
    DonHang? LayChiTiet(string maDon, string maKH);
    KetQua Huy(string maDon, string maKH, string lyDo);
}
