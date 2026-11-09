using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Admin.Controllers;

/// <summary>admin · Tổng quan hệ thống.</summary>
[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class HomeController(BaoCaoService baoCaoService) : Controller
{
    public IActionResult Index() => View(baoCaoService.TongQuan());
}
