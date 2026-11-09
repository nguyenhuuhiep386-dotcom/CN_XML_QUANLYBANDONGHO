using AurelMaison.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers.Admin;

[Authorize(Roles = VaiTro.Admin)]
[Route("Admin")]
public class AdminHomeController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("ChuaTrienKhai");
}
