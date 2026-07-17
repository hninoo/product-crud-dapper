using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductCrud.Web.Models;

namespace ProductCrud.Web.Controllers;

public sealed class HomeController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
