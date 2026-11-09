using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

// Form đăng nhập tối giản để kiểm thử phân quyền; Hiệp làm giao diện login/register theo Figma.
public class AccountController(AuthService authService) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string matKhau, string? returnUrl = null)
    {
        var tt = authService.DangNhap(email, matKhau);
        if (tt == null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Email"] = email;
            ViewData["Loi"] = "Email hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.";
            return View();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            AuthService.TaoPrincipal(tt, CookieAuthenticationDefaults.AuthenticationScheme));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return tt.VaiTro switch
        {
            VaiTro.Admin => Redirect("/Admin"),
            VaiTro.NhanVien => Redirect("/Staff"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
