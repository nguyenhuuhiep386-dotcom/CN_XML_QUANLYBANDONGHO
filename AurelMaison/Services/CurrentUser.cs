using System.Security.Claims;

namespace AurelMaison.Services;

/// <summary>
/// Đọc mã của người đang đăng nhập từ cookie. Mọi mã khách/nhân viên đều lấy từ đây,
/// không nhận từ form hay query.
/// </summary>
public static class CurrentUser
{
    public const string ClaimMaTK = "MaTK";
    public const string ClaimMaKH = "MaKH";
    public const string ClaimMaNV = "MaNV";

    public static string? MaTK(this ClaimsPrincipal user) => user.FindFirst(ClaimMaTK)?.Value;

    public static string? MaKH(this ClaimsPrincipal user) => user.FindFirst(ClaimMaKH)?.Value;

    public static string? MaNV(this ClaimsPrincipal user) => user.FindFirst(ClaimMaNV)?.Value;

    public static string? VaiTro(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value;

    public static string? HoTen(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Name)?.Value;
}
