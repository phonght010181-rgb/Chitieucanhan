using QuanLyChiTieuCaNhan.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChiTieuCaNhan.Models;
using QuanLyChiTieuCaNhan.ViewModels;
using QuanLyChiTieuCaNhan.Services;

namespace QuanLyChiTieuCaNhan.Controllers;
public class AccountController(AppDbContext db, ILogger<AccountController> logger) : Controller
{
    private const string ResetUserKey = "PasswordReset.UserId";
    private const string ResetCredentialKey = "PasswordReset.Credential";
    [AllowAnonymous, HttpGet]
    public IActionResult Register() => User.Identity.IsAuthenticated ? RedirectToAction("Index", "Dashboard") : View();

    [AllowAnonymous, HttpPost]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        model.Email = model.Email.Trim();
        if (db.Users.Any(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng");
            return View(model);
        }
        using var transaction = db.Database.BeginTransaction();
        try
        {
            var user = new User { FullName = model.FullName, Email = model.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, 12) };
            db.Users.Add(user);
            db.SaveChanges();
            db.Wallets.Add(new Wallet { UserId = user.UserId, Name = "Tiền mặt", WalletType = 1, IsDefault = true });
            DbInitializer.SeedUserCategories(db, user.UserId);
            transaction.Commit();
            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }
        catch (DbUpdateException ex)
        {
            transaction.Rollback();
            logger.LogError(ex, "Registration failed");
            ModelState.AddModelError("", "Không thể đăng ký. Vui lòng kiểm tra thông tin và thử lại.");
            return View(model);
        }
    }
    [AllowAnonymous, HttpGet]
    public IActionResult Login()
    {
        if (Request.Query.ContainsKey("ReturnUrl")) return RedirectToAction(nameof(Login));
        return User.Identity.IsAuthenticated ? RedirectToAction("Index", "Dashboard") : View();
    }
    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = db.Users.FirstOrDefault(u => u.Email == model.Email.Trim() && !u.IsDeleted);
        if (user == null) ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
        else if (user.IsLocked) ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng khôi phục mật khẩu hoặc liên hệ quản trị viên.");
        else if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            user.LoginFailedCount++;
            user.IsLocked = user.LoginFailedCount >= 5;
            try { db.SaveChanges(); }
            catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Thông tin đăng nhập vừa thay đổi. Vui lòng thử lại."); return View(model); }
            ModelState.AddModelError("", user.IsLocked ? "Tài khoản đã bị khóa sau 5 lần đăng nhập sai." : $"Email hoặc mật khẩu không đúng. Còn {5 - user.LoginFailedCount} lần thử.");
        }
        else
        {
            user.LoginFailedCount = 0;
            try { db.SaveChanges(); }
            catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Thông tin đăng nhập vừa thay đổi. Vui lòng thử lại."); return View(model); }
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), new Claim(ClaimTypes.Name, user.Email), new Claim(ClaimTypes.Role, user.Role), new Claim("FullName", user.FullName), new Claim("CredentialVersion", PasswordResetService.Fingerprint(user.PasswordHash)) };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return RedirectToAction("Index", "Dashboard");
        }
        return View(model);
    }
    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }
    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View();
    [Authorize, HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = db.Users.Single(u => u.UserId == User.GetUserId());
        if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, user.PasswordHash))
        {
            ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không đúng");
            return View(model);
        }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, 12);
        db.SaveChanges();
        await HttpContext.SignOutAsync();
        TempData["Success"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
        return RedirectToAction("Login");
    }
    [AllowAnonymous, HttpGet]
    public IActionResult ForgotPassword()
    {
        ClearPasswordReset();
        return View();
    }
    [AllowAnonymous, HttpPost]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        ClearPasswordReset();
        if (!ModelState.IsValid) return View(model);
        var user = db.Users.FirstOrDefault(u => u.Email == model.Email.Trim() && !u.IsDeleted);
        if (user == null)
        {
            ModelState.AddModelError("Email", "Email chưa được đăng ký.");
            return View(model);
        }
        TempData[ResetUserKey] = user.UserId.ToString();
        TempData[ResetCredentialKey] = PasswordResetService.Fingerprint(user.PasswordHash);
        return RedirectToAction(nameof(ResetPassword));
    }
    [AllowAnonymous, HttpGet]
    public IActionResult ResetPassword()
    {
        var user = GetPasswordResetUser();
        if (user == null) return RedirectToAction(nameof(ForgotPassword));
        return View(new ResetPasswordViewModel { Email = user.Email });
    }
    [AllowAnonymous, HttpPost]
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        var user = GetPasswordResetUser();
        if (user == null) return RedirectToAction(nameof(ForgotPassword));
        // Use the account selected in protected TempData, never the posted email.
        model.Email = user.Email;
        ModelState.Remove(nameof(model.Email));
        if (!ModelState.IsValid) return View(model);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, 12);
        user.IsLocked = false;
        user.LoginFailedCount = 0;
        try { db.SaveChanges(); }
        catch (DbUpdateConcurrencyException)
        {
            ClearPasswordReset();
            TempData["Error"] = "Tài khoản vừa thay đổi. Vui lòng nhập lại email.";
            return RedirectToAction(nameof(ForgotPassword));
        }
        ClearPasswordReset();
        TempData["Success"] = "Khôi phục mật khẩu thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    private User GetPasswordResetUser()
    {
        if (!int.TryParse(TempData.Peek(ResetUserKey) as string, out var userId)) return null;
        var user = db.Users.FirstOrDefault(u => u.UserId == userId && !u.IsDeleted);
        if (user != null && TempData.Peek(ResetCredentialKey) as string == PasswordResetService.Fingerprint(user.PasswordHash))
            return user;
        ClearPasswordReset();
        return null;
    }

    private void ClearPasswordReset()
    {
        TempData.Remove(ResetUserKey);
        TempData.Remove(ResetCredentialKey);
    }
}
