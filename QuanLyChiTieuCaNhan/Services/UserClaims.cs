using System.Security.Claims;
namespace QuanLyChiTieuCaNhan.Services;
public static class UserClaims
{
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
            ? id : throw new UnauthorizedAccessException("Không có tài khoản hợp lệ.");
}
