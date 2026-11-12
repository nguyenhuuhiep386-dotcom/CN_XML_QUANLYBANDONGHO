using System.Text.RegularExpressions;
using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

[Authorize(Roles = Security.Customer)]
public sealed class WarrantyController(WarrantyClient client) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? maPhieu, CancellationToken cancellationToken)
    {
        if (maPhieu is null) return View(new TraCuuVm(""));
        string ma = maPhieu.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(ma, @"^BH[0-9]{8}$"))
            return View(new TraCuuVm(ma, Loi: "Nhập mã phiếu theo dạng BH20260012."));
        var response = await client.Tim(ma, Request.Cookies[Security.CookieName], cancellationToken);
        if (response.Status == 401)
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", new { maPhieu = ma }) });
        return response.Status switch
        {
            200 => View(new TraCuuVm(ma, response.Phieu)),
            403 => View(new TraCuuVm(ma, Loi: "Bạn không có quyền xem phiếu bảo hành này.")),
            501 => View(new TraCuuVm(ma, Loi: "API bảo hành chưa được tích hợp trên nhánh này.")),
            404 => View(new TraCuuVm(ma, Loi: "Không tìm thấy mã phiếu bảo hành.")),
            _ => View(new TraCuuVm(ma, Loi: "Dịch vụ bảo hành chưa phản hồi. Vui lòng thử lại."))
        };
    }
}
