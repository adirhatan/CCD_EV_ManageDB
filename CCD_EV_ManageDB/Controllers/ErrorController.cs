using System.Web.Mvc;

namespace CCD_EV_ManageDB.Controllers
{
  public class ErrorController : Controller
  {
    public ActionResult Index()
    {
      return View();
    }

    public ActionResult NotFound()
    {
      return View();
    }

    public ActionResult AccessDenied()
    {
      return View();
    }
  }
}
