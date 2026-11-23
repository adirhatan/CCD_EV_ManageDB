using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CCD_EV_ManageDB.Models;
using CCD_EV_ManageDB.Repositories;
using System.Text.RegularExpressions;

namespace CCD_EV_ManageDB.ActionFilters
{
    public class CheckPermissionFilter :  ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //New permission check start:
            string table = "";
            if (!checkPermission(filterContext ,out table))
            {
                if (filterContext.Controller.TempData["message"]==null)
                    filterContext.Controller.TempData.Add("message", "You are not permitted to make this action:" + filterContext.ActionDescriptor.ControllerDescriptor.ControllerName + "/" + filterContext.ActionDescriptor.ActionName + " for table:" + table);
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary {{ "Controller", "ManageDB" },
                                      { "Action", "Index" } });
            }

            base.OnActionExecuting(filterContext);
        }

        bool checkPermission(ActionExecutingContext filterContext, out string table)
        {
            table = " cannot extract table from request parameters";
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated) return false;
            MDBUsers MDBUsersObj = new MDBUsers();
            List<DBpermission> permissions = (MDBUsersObj.getPermissions(filterContext.HttpContext.User.Identity.Name));
            //SInce Default permission is false for non handled actions, try to give inormative message:
            if (filterContext.HttpContext.User.IsInRole("CCD_EV") && (string.IsNullOrEmpty(permissions[0]._interface))) return true;
            table += ";Yor are limited to \"" + permissions[0]._interface + "\" tables only";
            string action = filterContext.ActionDescriptor.ActionName;
            string controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            string _col = "NoSuchCol";
            string _proj = "ALL";
            string _phase = "ALL";
            string message = "";
            DBsearch DBS = new DBsearch();
            switch (controller)
            {
                case "ManageDB":
                    switch (action)
                    {
                        case "ExportReportFileBG":
                        case "ExportEditFileBG":
                        case "DeleteMatchedRecs":
                        case "Delete":
                            DBS = getDBSfromSession(filterContext);
                            table = DBS.col;
                            if (filterContext.HttpContext.User.IsInRole("CCD_EV_GB")) return ((new DBpermissionsHandler()).CheckSearchPermission(DBS.Query, DBS.col, filterContext.HttpContext.User.Identity.Name, out message));
                            if (filterContext.HttpContext.User.IsInRole("CCD_EV"))  return check(permissions, DBS.col, "ALL", "ALL");
                            break;
                        case "LoadFile":
                            if ((action == "LoadFile") && (filterContext.HttpContext.Request.HttpMethod == "GET"))
                            {
                                DBS = getDBSfromSession(filterContext);
                                table = DBS.col;
                                if (filterContext.HttpContext.User.IsInRole("CCD_EV_GB")) return ((new DBpermissionsHandler()).CheckSearchPermission(DBS.Query, DBS.col, filterContext.HttpContext.User.Identity.Name, out message));
                                if (filterContext.HttpContext.User.IsInRole("CCD_EV")) return check(permissions, DBS.col, "ALL", "ALL");
                            }

                            if ((action == "LoadFile") && (filterContext.HttpContext.Request.HttpMethod == "POST"))
                            {
                                _col = get_request_parameter(filterContext, "col");
                                table = _col;
                                return check(permissions, _col, _proj, _phase);
                            }

                            break;
                        case "LoadToPRM":
                        case "ShowLogicUnitLinks":
                            _col = get_request_parameter(filterContext, "col");
                            table = _col;
                            return check(permissions,_col,_proj,_phase);
                        case "ExecutionLogNew":
                            DBS = getDBSfromSession(filterContext);
                            _col = DBS.col;
                            //"colname" parameter if exists overwrites Session table
                            string tmpStr = get_request_parameter(filterContext, "colname");
                            if (!string.IsNullOrEmpty(tmpStr)) _col = tmpStr;
                            table = _col;
                            return check(permissions,_col,_proj,_phase);
                    }
                    break;
                case "GeneralDB":
                    switch (action)
                    {
                        case "EditRefTable":
                        case "CopyRef":
                        case "DeleteRef":
                        case "IndicatorEdit":
                        case "IndicatorLaunch":
                        case "IndicatorEditExec":
                            _col = getInterfacePrefixFromSTR(get_request_parameter(filterContext, "_id"));
                            if (!string.IsNullOrEmpty(_col)) table = _col;
                            return check(permissions,_col,_proj,_phase);
                        case "SaveRef":
                            _col = getInterfacePrefixFromSTR(get_request_parameter(filterContext, "id"));
                            if (!string.IsNullOrEmpty(_col)) table = _col;
                            return check(permissions, _col, _proj, _phase);
                        case "CreateRefTable":
                        case "CreateRefTableFromReport":
                            _col = getInterfacePrefixFromSTR(get_request_parameter(filterContext, "_Protocol"));
                            if (!string.IsNullOrEmpty(_col)) table = _col;
                            return check(permissions,_col,_proj,_phase);
                        case "IndicatorsLaunchAllBG":
                            _col = getInterfacePrefixFromSTR(get_request_parameter(filterContext, "_interface"));
                            if (!string.IsNullOrEmpty(_col)) table = _col;
                            return check(permissions,_col,_proj,_phase);
                    }
                    break;
                case "ReportDB":
                    string reportID = "";
                    switch (action)
                    {
                        case "EditReport":
                        case "CopyReport":
                            reportID = get_request_parameter(filterContext, "_id");
                            _col = getTableFromReportID(reportID);
                            if (!string.IsNullOrEmpty(_col)) table = _col;
                            return check(permissions, _col, _proj, _phase);
                            
                        case "CreateReport":
                            _col = get_request_parameter(filterContext, "tableName");
                            if (!string.IsNullOrEmpty(_col)) table = _col;
                            if (!check(permissions, _col, _proj, _phase)) return false;
                            string ref_interface =  getInterfacePrefixFromSTR(get_request_parameter(filterContext, "refId"));
                            if (string.IsNullOrEmpty(ref_interface)) return true;
                            table = "(Spec reference) " + ref_interface;
                            return check(permissions, ref_interface, _proj, _phase);
                    }

                    
                    break;
            }
            //Default return false for actions that are not specifically parsed 
            return false;
        }


        //for role CCD-EV-BG we check table;project ;phase
        //for role CCD-EV we just check interface (as table) proj = ALL, phase = ALL
        bool check(List<DBpermission> permissions, string col, string proj, string phase)
        {
            if (col.ToUpper().StartsWith("EMPTY")) return true;
            try
            {
                foreach (DBpermission permission in permissions)
                {
                    if (permission.checkPermission(col, proj, phase)) return true;
                }
            }
            catch (Exception e)
            {
                //TODO
            }
            return false;
        }

        DBsearch getDBSfromSession(ActionExecutingContext filterContext)
        {
            DBsearch DBS = new DBsearch();
            if ((FullSession)filterContext.HttpContext.Session["FS"] == null) return DBS;
            if (((FullSession)filterContext.HttpContext.Session["FS"]).LastDBViewed.StartsWith("PRM"))
                DBS = ((FullSession)filterContext.HttpContext.Session["FS"]).PrmSearch.getNewDBS();
            if (((FullSession)filterContext.HttpContext.Session["FS"]).LastDBViewed == "TMP")
                DBS = ((FullSession)filterContext.HttpContext.Session["FS"]).TmpSearch.getNewDBS();
            return DBS;
        }

        string get_request_parameter(ActionExecutingContext filterContext, string field)
        {
            if ((filterContext.HttpContext.Request.QueryString[field]) != null) return (filterContext.HttpContext.Request.QueryString[field].ToString());
            else
            {
                if (filterContext.HttpContext.Request.Form != null)
                {
                    foreach (string key in filterContext.HttpContext.Request.Form.AllKeys)
                    {
                        if (key == field)
                        {
                            return (filterContext.HttpContext.Request.Form[key].ToString());
                        }
                    }
                }
            }
            return "";
        }

        string getInterfacePrefixFromSTR(string _id)
        {
            if (Regex.IsMatch(_id, @"([A-Za-z]*)(.*)"))
            {
                Match m = Regex.Match(_id, @"([A-Za-z]*)(.*)");
                return (m.Groups[1].Value);
            }
            else return _id;
        }

        string getTableFromReportID(string _id)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);
            if (Report.RConfig == null) return "";
            return Report.RConfig.table;
        }
    }
}