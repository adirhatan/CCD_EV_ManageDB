using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Dynamic;
using System.Text;
using System.Web.Script.Serialization;

namespace CCD_EV_ManageDB.Helpers
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlString IncludeNGon(this HtmlHelper helper, string @namespace = "ngon")
        {
            var viewData = helper.ViewContext.ViewData;
            if (viewData == null)
            {
                return MvcHtmlString.Empty;
            }
 
            var ngon = viewData["NGon"] as ExpandoObject;
            if (ngon == null)
            {
                throw new InvalidOperationException("Cannot find NGon in ViewBag. Did you remember to add the global NGonActionFilterAttribute?");
            }
 
            var tag = new TagBuilder("script");
            tag.Attributes.Add(new KeyValuePair<string, string>("type", "text/javascript"));
            var builder = new StringBuilder();
            builder.AppendFormat("window.{0}={{}};", @namespace);
 
            //var serializer = new JavaScriptSerializer();
            var serializer = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 100 };
            foreach (var prop in ngon)
            {
                builder.AppendFormat("{0}.{1}={2};", @namespace, prop.Key, helper.Raw(serializer.Serialize(prop.Value)));
            }
 
            tag.InnerHtml = builder.ToString();
            return new HtmlString(tag.ToString());
        }
    }
}