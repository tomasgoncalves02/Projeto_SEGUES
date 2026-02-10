using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.User.Controllers;

public class UserController : Controller
{
    // GET
    public IActionResult Index()
    {
        // TODO: Profile
        //redirect to previous page
        return Redirect(Request.Headers["Referer"].ToString());
        //return View();
    }
}