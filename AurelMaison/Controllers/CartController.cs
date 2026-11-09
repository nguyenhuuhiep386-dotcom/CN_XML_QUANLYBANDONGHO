using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

public class CartController : Controller
{
    public IActionResult Index() => View("ChuaTrienKhai");
}
