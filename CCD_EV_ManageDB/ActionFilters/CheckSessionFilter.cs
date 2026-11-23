using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace CCD_EV_ManageDB.ActionFilters
{
    public class CheckSessionFilter :  ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) 
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary {{ "Controller", "ManageDB" },
                                      { "Action", "Index" } });
            }

            base.OnActionExecuting(filterContext);
        }
    }
}