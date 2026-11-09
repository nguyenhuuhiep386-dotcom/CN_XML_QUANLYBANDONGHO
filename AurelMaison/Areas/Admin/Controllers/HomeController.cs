using AurelMaison.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class HomeController : Controller
{
    public IActionResult Index() => View("ChuaTrienKhai");
}
