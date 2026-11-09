using System.Security.Cryptography;
using System.Text;

namespace AurelMaison.Services;

/// <summary>Mật khẩu lưu dạng SHA256(mật khẩu + Salt), mã hóa UTF-8, xuất hex chữ thường.</summary>
public static class MatKhauHelper
{
    public static string TaoSalt() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    public static string Bam(string matKhau, string salt) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(matKhau + salt))).ToLowerInvariant();

    public static bool KiemTra(string matKhau, string salt, string hashDaLuu) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Bam(matKhau, salt)),
            Encoding.UTF8.GetBytes(hashDaLuu.ToLowerInvariant()));
}
