using AurelMaison.Models;
using AurelMaison.Services;
using AurelMaison.Services.CustomerDemo;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

XmlHelper.KhoiTao(builder.Environment.ContentRootPath);
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<KhoService>();
builder.Services.AddSingleton<DanhMucService>();
builder.Services.AddSingleton<QuanLyTaiKhoanService>();
builder.Services.AddSingleton<BaoCaoService>();
builder.Services.AddSingleton<KhachHangService>();
builder.Services.AddSingleton<DongBoService>();

// Customer adapters call Phương's real account, profile, catalog and stock services.
builder.Services.AddSingleton<CustomerGroupAdapter>();
builder.Services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<CustomerGroupAdapter>());
builder.Services.AddSingleton<IKhachHangService>(sp => sp.GetRequiredService<CustomerGroupAdapter>());
builder.Services.AddSingleton<IDanhMucService>(sp => sp.GetRequiredService<CustomerGroupAdapter>());
builder.Services.AddSingleton<IKhoService>(sp => sp.GetRequiredService<CustomerGroupAdapter>());
// Nguyên will replace the provisional online-order adapter at integration.
builder.Services.AddSingleton<IDonHangService, DemoDonHangService>();
builder.Services.AddScoped<GioHangService>();
builder.Services.AddSingleton<LichHenService>();
builder.Services.AddHttpClient<WarrantyClient>(client => client.Timeout = TimeSpan.FromSeconds(8))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = Security.CookieName;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Đọc lại tài khoản ở mỗi yêu cầu: bị khóa hoặc đổi vai trò thì đăng xuất ngay.
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var tk = ctx.HttpContext.RequestServices.GetRequiredService<AuthService>()
                .LayTaiKhoan(ctx.Principal?.MaTK());
            if (tk == null || tk.TrangThai != TrangThaiTaiKhoan.HoatDong || tk.VaiTro != ctx.Principal!.VaiTro())
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };

        // /api trả mã lỗi thay vì chuyển sang trang đăng nhập HTML.
        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else
            {
                ctx.Response.Redirect(ctx.RedirectUri);
            }
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            }
            else
            {
                ctx.Response.Redirect(ctx.RedirectUri);
            }
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// Giỏ hàng chỉ lưu trong Session.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
