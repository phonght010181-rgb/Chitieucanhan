using System.Security.Cryptography;
using System.Text;

namespace QuanLyChiTieuCaNhan.Services;

public static class PasswordResetService
{
    public static string Fingerprint(string hash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
}