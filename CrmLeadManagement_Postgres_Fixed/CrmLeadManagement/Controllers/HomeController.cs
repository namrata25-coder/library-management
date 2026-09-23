using Microsoft.AspNetCore.Mvc;

namespace CrmLeadManagement.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("List", "Lead");
    }

    public IActionResult Error()
    {
        return Problem();
    }
}
