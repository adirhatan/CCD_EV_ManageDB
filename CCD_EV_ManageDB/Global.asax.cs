using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using CCD_EV_ManageDB.Models.Security;
using CCD_EV_ManageDB.Controllers;
using CCD_EV_ManageDB.Helpers;
using log4net;
using NGon;

namespace CCD_EV_ManageDB
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
   

    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MvcApplication));

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            //filters.Add(new HandleErrorAttribute());
            filters.Add(new CustomHandleErrorAttribute());
       }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "ManageDB", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {

            AreaRegistration.RegisterAllAreas();
            
            GlobalFilters.Filters.Add(new NGonActionFilterAttribute());


            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
            //BGJobSchedulerInd.Start();

            log4net.Config.XmlConfigurator.Configure();

        }
        protected void FormsAuthentication_OnAuthenticate(Object sender, FormsAuthenticationEventArgs e)
        {
            if (FormsAuthentication.CookiesSupported == true)
            {
                if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
                {
                    try
                    {
                        //let us take out the username now                
                        string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
                        CCD_EV_RolesProvider rp = new CCD_EV_RolesProvider();

                        //Let us set the Pricipal with our user specific details
                        e.User = new System.Security.Principal.GenericPrincipal(
                          new System.Security.Principal.GenericIdentity(username, "Forms"), rp.GetRolesForUser(username));
                        _log.Info("In OnAuthenticate user:" + username + " roles:" + rp.GetRolesForUser(username).Length);
                    }
                    catch (Exception ex)
                    {
                        //somehting went wrong
                        _log.Info("In OnAuthenticate error:" + ex.ToString());
                    }
                }
            }
        }

        //protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
        //{
        //    if (FormsAuthentication.CookiesSupported == true)
        //    {
        //        if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
        //        {
        //            try
        //            {
        //                //let us take out the username now
        //                string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
        //                CCD_EV_RolesProvider rp = new CCD_EV_RolesProvider();
        //                //Let us set the Pricipal with our user specific details
        //                HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(
        //                  new System.Security.Principal.GenericIdentity(username, "Forms"), rp.GetRolesForUser(username));
        //            }
        //            catch (Exception)
        //            {
        //                //somehting went wrong
        //            }
        //        }
        //    }
        //}

        protected void Application_Error(object sender, EventArgs e)
        {
            var httpContext = ((MvcApplication)sender).Context;
            var currentController = " ";
            var currentAction = " ";
            var currentRouteData = RouteTable.Routes.GetRouteData(new HttpContextWrapper(httpContext));

            if (currentRouteData != null)
            {
                if (currentRouteData.Values["controller"] != null && !String.IsNullOrEmpty(currentRouteData.Values["controller"].ToString()))
                {
                    currentController = currentRouteData.Values["controller"].ToString();
                }

                if (currentRouteData.Values["action"] != null && !String.IsNullOrEmpty(currentRouteData.Values["action"].ToString()))
                {
                    currentAction = currentRouteData.Values["action"].ToString();
                }
            }

            var ex = Server.GetLastError();
            var controller = new ErrorController();
            var routeData = new RouteData();
            var action = "Index";

            if (ex is HttpException)
            {
                var httpEx = ex as HttpException;

                switch (httpEx.GetHttpCode())
                {
                    case 404:
                        action = "NotFound";
                        break;

                    case 401:
                        action = "AccessDenied";
                        break;
                }
            }

            httpContext.ClearError();
            httpContext.Response.Clear();
            httpContext.Response.StatusCode = ex is HttpException ? ((HttpException)ex).GetHttpCode() : 500;
            httpContext.Response.TrySkipIisCustomErrors = true;

            routeData.Values["controller"] = "Error";
            routeData.Values["action"] = action;

            controller.ViewData.Model = new HandleErrorInfo(ex, currentController, currentAction);
            ((IController)controller).Execute(new RequestContext(new HttpContextWrapper(httpContext), routeData));
        }

        //adde due to sso problem, coming from microsoft app, soloution found in (section starts with "Here is a solution for C# ASP.NET"):
        //https://stackoverflow.com/questions/2653626/why-are-cookies-unrecognized-when-a-link-is-clicked-from-an-external-source-i-e
        private static string MSUserAgentsRegex = @"[^\w](Word|Excel|PowerPoint|ms-office)([^\w]|\z)";
        protected void Application_OnPostAuthenticateRequest(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(Request.UserAgent, MSUserAgentsRegex))
            {
                Response.Write("<html><head><meta http-equiv='refresh' content='0'/></head><body></body></html>");
                Response.End();
            }
        }
    }
}