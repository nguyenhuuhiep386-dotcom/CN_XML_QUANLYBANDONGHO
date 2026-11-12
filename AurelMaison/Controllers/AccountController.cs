using System.Security.Claims;
using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

public sealed class AccountController(IAuthService auth, IKhachHangService customers, LichHenService booking) : Controller
{
    [HttpGet]
    public IActionResult Register() => View(new DangKyForm());

    [HttpPost]
    public IActionResult Register(DangKyForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var result = auth.DangKy(form);
        if (!result.ThanhCong)
        {
            ModelState.AddModelError(nameof(form.Email), result.ThongBao);
            return View(form);
        }
        TempData["Message"] = result.ThongBao;
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new DangNhapForm { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(DangNhapForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var account = auth.DangNhap(form.Email, form.MatKhau);
        if (account is null)
        {
            ModelState.AddModelError("", "Email, mật khẩu không đúng hoặc tài khoản đã bị khóa.");
            return View(form);
        }

        if (User.Identity?.IsAuthenticated == true && User.MaTK() != account.MaTK) HttpContext.Session.Clear();
        var claims = new List<Claim> { new(CurrentUser.ClaimMaTK, account.MaTK), new(ClaimTypes.Name, account.HoTen),
            new(ClaimTypes.Role, account.Role) };
        if (account.MaKH is not null) claims.Add(new("MaKH", account.MaKH));
        if (account.MaNV is not null) claims.Add(new("MaNV", account.MaNV));
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        if (account.Role == Security.Staff) return RedirectToAction("Index", "Home", new { area = "Staff" });
        if (account.Role == Security.Admin) return RedirectToAction("Index", "Home", new { area = "Admin" });
        return Url.IsLocalUrl(form.ReturnUrl) ? LocalRedirect(form.ReturnUrl!) : RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [Authorize(Roles = Security.Customer), HttpGet]
    public IActionResult Index(string tab = "ho-so") => TaiKhoan(tab);

    [Authorize(Roles = Security.Customer), HttpPost]
    public IActionResult HoSo([Bind(Prefix = "HoSo")] HoSoForm form)
    {
        if (!ModelState.IsValid) return TaiKhoan("ho-so", form);
        var result = customers.CapNhatHoSo(User.MaKH()!, form);
        if (!result.ThanhCong)
        {
            ModelState.AddModelError("", result.ThongBao);
            return TaiKhoan("ho-so", form);
        }
        TempData["Message"] = result.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Security.Customer), HttpPost]
    public async Task<IActionResult> MatKhau([Bind(Prefix = "MatKhau")] DoiMatKhauForm form)
    {
        if (!ModelState.IsValid) return TaiKhoan("mat-khau");
        var result = auth.DoiMatKhau(User.MaTK()!, form.MatKhauCu, form.MatKhauMoi);
        if (!result.ThanhCong)
        {
            ModelState.AddModelError("", result.ThongBao);
            return TaiKhoan("mat-khau");
        }
        await HttpContext.SignOutAsync();
        TempData["Message"] = result.ThongBao;
        return RedirectToAction(nameof(Login));
    }

    private IActionResult TaiKhoan(string tab, HoSoForm? form = null)
    {
        if (tab is not ("ho-so" or "mat-khau" or "lich-hen")) return NotFound();
        var profile = customers.LayTheoTaiKhoan(User.MaTK()!);
        if (profile is null) return NotFound();
        return View("Index", new TaiKhoanVm(tab, form ?? new() { HoTen = profile.HoTen,
            SoDienThoai = profile.SoDienThoai, DiaChi = profile.DiaChi }, new(), booking.LayTheoKhach(profile.MaKH)));
    }
}
