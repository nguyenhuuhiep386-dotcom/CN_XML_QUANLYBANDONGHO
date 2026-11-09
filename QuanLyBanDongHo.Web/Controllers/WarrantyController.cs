using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

[Authorize]
public class WarrantyController : Controller
{
    public IActionResult Index() => View("ChuaTrienKhai");
}
