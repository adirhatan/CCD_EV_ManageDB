using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.IO;
using System.Text; 
using CCD_EV_ManageDB.Models;
using CCD_EV_ManageDB.Repositories;
using CCD_EV_ManageDB.ActionFilters;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Web.Security;
using System.Threading;
using System.Threading.Tasks;
using CCD_EV_ManageDB.Helpers;


namespace CCD_EV_ManageDB.Controllers
{
    //[Authorize]
    [LogActionFilter]
    public class ManageDBController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ManageDBController));
        private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }
        private static string DefaultCompCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCompCol"]; } }

        private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        private readonly int  DataTableLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DataTableLimit"]);

        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        private static string DB_PRM_Arch { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM_Arch"]; } }
        private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }
        private static string DB_REC { get { return System.Configuration.ConfigurationManager.AppSettings["DB_REC"]; } }
        private static string DB_CMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP"]; } }
        private static string DB_CMP_Arch { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP_Arch"]; } }

        private MongoDBClient db = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);

        //private MongoDBClient db = new MongoDBClient("mongodb://localhost", "test", "powerConsResults");
        private List<string> lop = new List<string>(new string[] { "EQ","Matches","NotMatches","In","NotIn","ExistNotEmpty", "GT", "LT" });
        public string ServerDir{ get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; }  }
        public string[] MongoTables { get { return System.Configuration.ConfigurationManager.AppSettings["MongoTables"].Split(','); } }
        public string[] CompTables { get { return System.Configuration.ConfigurationManager.AppSettings["CertificationTables"].Split(','); } }
        public string[] ProjectsList { get { return System.Configuration.ConfigurationManager.AppSettings["ProjectsList"].Split(','); } }
        public string[] PhasesList { get { return System.Configuration.ConfigurationManager.AppSettings["PhasesList"].Split(','); } }
        
        //
        // GET: /ManageDB/

        public ActionResult Index()
        {
            //log.Debug("ManageDBController.Index was called");
            //log.Info("ManageDBController.Index was called");
            //return RedirectToAction("test1");
             DBsearch DBS;

             DBS = new DBsearch();

             if (System.Web.HttpContext.Current.Session["FS"] != null)
             {
                 if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRM")
                 return RedirectToAction("SearchPRM",((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS());
                 if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRMArch")
                     return RedirectToAction("SearchPRMArch", ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS());
                 if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "TMP")
                 return RedirectToAction("SearchTMP",((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS());
                 if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "CMP")
                 return RedirectToAction("SearchCOMP", ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS());
             }
             else 
             {
                System.Web.HttpContext.Current.Session["FS"] = new FullSession();
                return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "TMP", col = DefaultCol }));              
             }
            return RedirectToAction("SearchTMP",DBS);
        }
        [Authorize]
        public ActionResult Login()
        {
            return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult SaveQuery(string name,string _db)
        {
            bool s;
            DBsearch DBS = null;
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            if (String.IsNullOrEmpty(name)) name="DefaultName";
            if ((((FullSession)System.Web.HttpContext.Current.Session["FS"]) != null))
            {
                if (_db == DB_TMP) DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                if (_db == DB_PRM) DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                if (_db == DB_CMP) DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
                if (DBS != null)
                {

                    s = db.SaveUserQuery(username, name, DBS);
                    if (s) Response.Write("Query saved:" + name);
                    return null;
                }
            }
            Response.Write("Failed to save query:" + name);
            return null;
        }
        
        public ActionResult ReadSavedQuery(string _id)
        {
            DBsearch DBS = db.ReadUserQuery(_id);
            if (DBS != null)
            {
                if (DBS.db == DB_TMP) return RedirectToAction("SearchTMP", DBS);
                if (DBS.db == DB_PRM) return RedirectToAction("SearchPRM", DBS);
                if (DBS.db == DB_CMP) return RedirectToAction("SearchCOMP", DBS);
                return RedirectToAction("Index");
            }
            else return RedirectToAction("Index");
        }

        public ActionResult DeleteSavedQuery(string _id)
        {

            if (!db.DeleteUserQuery(_id)) TempData["message"] = "There was a problem deleting " + _id + "saved query";
            return RedirectToAction("MyQueries");
        }

        //public ActionResult popEditSavedQuery(string _id)
        //{
        //    DBsearchSave saveDBS = new DBsearchSave();
        //    DBsearch DBS = saveDBS.getNewDBS();
        //    return View(DBS);
        //}

        [HttpPost]
        public ActionResult RenameSavedQuery(string _id,string name)
        {
            bool s;
            s = db.RenameUserQuery(_id, name);
            if (s) Response.Write(name);
            else Response.Write("Rename Failed:" + name);
            return null;
        }

        public ActionResult SearchTMP(DBsearch obj)
        {
            if (obj.col == null)
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.col != null)
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                }
                else
                {
                    return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "TMP", col = DefaultCol }));
                    //return RedirectToAction("Index");
                }

            }
            //in case the session has gone, and submit was clicked on an opened page
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            //if (System.Web.HttpContext.Current.Session["FS"] == null) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "TMP", col = obj.col }));


            //New permission check start:
            List<DBpermission> permissions = new List<DBpermission>();
            MDBUsers MDBUsersObj = new MDBUsers();
            if (User.Identity.IsAuthenticated) permissions = (MDBUsersObj.getPermissions(User.Identity.Name));
            if ((!HttpContext.User.Identity.IsAuthenticated) || ((HttpContext.User.Identity.IsAuthenticated) && (HttpContext.User.IsInRole("CCD_EV_GB"))) ||
                (User.IsInRole("CCD_EV") && (!string.IsNullOrEmpty(permissions[0]._interface))))
            {
                string userName = "guest";
                string message = "";
                if (HttpContext.User.Identity.IsAuthenticated) userName = HttpContext.User.Identity.Name;
                if (!(new DBpermissionsHandler()).CheckSearchPermission(obj.Query, obj.col, userName, out message))
                {
                    System.Web.HttpContext.Current.Session["FS"] = new FullSession();
                    TempData["message"] = message; ;
                    return new RedirectToRouteResult(new RouteValueDictionary { { "Controller", "ManageDB" }, { "Action", "Index" } });
                }
            }
            //New permission check end:

            ViewBag.q0list = lop;
            ViewBag.collist = new List<string>(MongoTables);
            DBsearch DBS = new DBsearch();
            DBS.col = obj.col;
            DBS.db = obj.db;
            if (DBS.db != DB_TMP) DBS.db = DB_TMP;// ugly patch due to coming vi menu from Recycle DB (db was REC)
            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.FieldsStr = obj.FieldsStr;
            DBS.FieldsIncExc = obj.FieldsIncExc;
            if (obj.FieldsIncExc == "Exc")
            {
                DBS.FieldsExc = DBS.FieldsExc = true;
                DBS.FieldsInc = false;
            }
            DBS.AddCoreFields();
            if (!String.IsNullOrEmpty(obj.SortStr)) TempData["JumpToBottom"] = "yes";
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "_id";
            DBS.Skip = obj.Skip;
            DBS.jumpBottom = obj.jumpBottom;
            db.genericFind(DBS, DataTableLimit);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            DBS.TableFields = (new MDBTablesFields()).ReadTableFields(DBS.col, General.TableType.Dev);
            foreach (string field in DBS.Fields) DBS.TableFields.Add(field);


            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch = new DBsearchSave(DBS);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "TMP";

            if ((Roles.IsUserInRole("CCD_EV")) || (Roles.IsUserInRole("CCD_EV_ADV"))) return View("SearchTMP",DBS);
            //New, new page SearchTMPlp ?
            if (Roles.IsUserInRole("CCD_EV_GB")) return View("SearchTMP", DBS);
            return View("SearchTMPv", DBS);         

        }


        public ActionResult SearchREC(DBsearch obj)
        {
            if (obj.col == null)
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.col != null)
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                    obj.db = DB_REC;
                }
                else
                {
                    return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "REC", col = DefaultCol }));
                    //return RedirectToAction("Index");
                }

            }
            //in case the session has gone, and submit was clicked on an opened page
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            //if (System.Web.HttpContext.Current.Session["FS"] == null) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "TMP", col = obj.col }));


            //New permission check start:
            List<DBpermission> permissions = new List<DBpermission>();
            MDBUsers MDBUsersObj = new MDBUsers();
            if (User.Identity.IsAuthenticated) permissions = (MDBUsersObj.getPermissions(User.Identity.Name));
            if ((!HttpContext.User.Identity.IsAuthenticated) || ((HttpContext.User.Identity.IsAuthenticated) && (HttpContext.User.IsInRole("CCD_EV_GB"))) ||
                (User.IsInRole("CCD_EV") && (!string.IsNullOrEmpty(permissions[0]._interface))))
            {
                string userName = "guest";
                string message = "";
                if (HttpContext.User.Identity.IsAuthenticated) userName = HttpContext.User.Identity.Name;
                if (!(new DBpermissionsHandler()).CheckSearchPermission(obj.Query, obj.col, userName, out message))
                {
                    System.Web.HttpContext.Current.Session["FS"] = new FullSession();
                    TempData["message"] = message; ;
                    return new RedirectToRouteResult(new RouteValueDictionary { { "Controller", "ManageDB" }, { "Action", "Index" } });
                }
            }
            //New permission check end:

            ViewBag.q0list = lop;
            ViewBag.collist = new List<string>(MongoTables);
            DBsearch DBS = new DBsearch();
            DBS.col = obj.col;
            DBS.db = obj.db;
            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.FieldsStr = obj.FieldsStr;
            DBS.FieldsIncExc = obj.FieldsIncExc;
            if (obj.FieldsIncExc == "Exc")
            {
                DBS.FieldsExc = DBS.FieldsExc = true;
                DBS.FieldsInc = false;
            }
            DBS.AddCoreFields();
            if (!String.IsNullOrEmpty(obj.SortStr)) TempData["JumpToBottom"] = "yes";
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "_id";
            DBS.Skip = obj.Skip;
            DBS.jumpBottom = obj.jumpBottom;
            db.genericFind(DBS, DataTableLimit);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            DBS.TableFields = (new MDBTablesFields()).ReadTableFields(DBS.col, General.TableType.Dev);
            foreach (string field in DBS.Fields) DBS.TableFields.Add(field);


            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch = new DBsearchSave(DBS);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "TMP";

            if ((Roles.IsUserInRole("CCD_EV")) || (Roles.IsUserInRole("CCD_EV_ADV"))) return View("SearchREC", DBS);
            //New, new page SearchTMPlp ?
            if (Roles.IsUserInRole("CCD_EV_GB")) return View("SearchREC", DBS);
            return View("SearchTMPv", DBS);

        }

        [Authorize(Roles = "CCD_EV,CCD_EV_ADV,CCD_EV_GB")]
        public ActionResult SearchPRM(DBsearch obj)
        {
            if (obj.col == null)
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                    if (obj.db != DB_PRM) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "PRM", col = obj.col }));
                }
                else
                {
                    if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
                    return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "PRM", col = DefaultCol }));
                }

            }

            //in case the session has gone, and submit was clicked on an opened page
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            //if (System.Web.HttpContext.Current.Session["FS"] == null) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "PRM", col = obj.col }));

            //New permission check start:
            List<DBpermission> permissions = new List<DBpermission>();
            MDBUsers MDBUsersObj = new MDBUsers();
            if (User.Identity.IsAuthenticated) permissions = (MDBUsersObj.getPermissions(User.Identity.Name));
            if ((!HttpContext.User.Identity.IsAuthenticated) || ((HttpContext.User.Identity.IsAuthenticated) && (HttpContext.User.IsInRole("CCD_EV_GB"))) ||
                (User.IsInRole("CCD_EV") && (!string.IsNullOrEmpty(permissions[0]._interface))))
            {
                string userName = "guest";
                string message = "";
                if (HttpContext.User.Identity.IsAuthenticated) userName = HttpContext.User.Identity.Name;
                if (!(new DBpermissionsHandler()).CheckSearchPermission(obj.Query, obj.col, userName, out message))
                {
                    System.Web.HttpContext.Current.Session["FS"] = new FullSession();
                    TempData["message"] = message; ;
                    return new RedirectToRouteResult(new RouteValueDictionary { { "Controller", "ManageDB" }, { "Action", "SearchPRM" } });
                }
            }
            //New permission check end:

            ViewBag.q0list = lop;
            ViewBag.collist = new List<string>(MongoTables); 
            DBsearch DBS = new DBsearch();
            DBS.col = obj.col;
            DBS.db = obj.db;
            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.FieldsStr = obj.FieldsStr;
            DBS.FieldsIncExc = obj.FieldsIncExc;
            if (obj.FieldsIncExc == "Exc")
            {
                DBS.FieldsExc = DBS.FieldsExc = true;
                DBS.FieldsInc = false;
            }
            DBS.AddCoreFields();
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "_id";
            DBS.Skip = obj.Skip;
            DBS.jumpBottom = obj.jumpBottom;
            db.genericFind(DBS, DataTableLimit);
            DBS.TableFields = (new MDBTablesFields()).ReadTableFields(DBS.col, General.TableType.Dev);
            foreach (string field in DBS.Fields) DBS.TableFields.Add(field);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();

            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch = new DBsearchSave(DBS);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "PRM";

            if (Roles.IsUserInRole("CCD_EV_ADV")) return View("SearchPRM", DBS);
            if ((Roles.IsUserInRole("CCD_EV")) || (Roles.IsUserInRole("CCD_EV_ADV"))) return View("SearchPRM", DBS);
            //New, new page SearchTMPlp ?
            if (Roles.IsUserInRole("CCD_EV_GB")) return View("SearchPRM", DBS);
            return View("SearchPRMv", DBS);  

        }

        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        public ActionResult SearchPRMArch(DBsearch obj)
        {
            if (obj.col == null)
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                    if (obj.db != DB_PRM_Arch) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "PRM_Arch", col = obj.col }));
                }
                else
                {
                    if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
                    return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "PRM_Arch", col = DefaultCol }));
                }

            }

            //in case the session has gone, and submit was clicked on an opened page
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            //if (System.Web.HttpContext.Current.Session["FS"] == null) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "PRM", col = obj.col }));

            ViewBag.q0list = lop;
            ViewBag.collist = new List<string>(MongoTables);
            DBsearch DBS = new DBsearch();
            DBS.col = obj.col;
            DBS.db = obj.db;
            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.FieldsStr = obj.FieldsStr;
            DBS.FieldsIncExc = obj.FieldsIncExc;
            if (obj.FieldsIncExc == "Exc")
            {
                DBS.FieldsExc = DBS.FieldsExc = true;
                DBS.FieldsInc = false;
            }
            DBS.AddCoreFields();
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "_id";
            DBS.Skip = obj.Skip;
            DBS.jumpBottom = obj.jumpBottom;
            db.genericFind(DBS, DataTableLimit);
            DBS.TableFields = (new MDBTablesFields()).ReadTableFields(DBS.col, General.TableType.Dev);
            foreach (string field in DBS.Fields) DBS.TableFields.Add(field);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();

            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch = new DBsearchSave(DBS);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "PRMArch";

            //if (Roles.IsUserInRole("CCD_EV_ADV")) return View("SearchPRMArch", DBS);
            //return View("SearchPRMArchv", DBS);

            return View("SearchPRMArch", DBS);

        }

        public ActionResult SearchCOMP(DBsearch obj)
        {
            if (obj.col == null)
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.col != null)
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
                    if (obj.db != DB_CMP) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "CMP", col = obj.col }));
                }
                else
                {
                    //return RedirectToAction("Index"); DefaultCompCol
                    return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "CMP", col = DefaultCompCol }));
                }

            }
            //in case the session has gone, and submit was clicked on an opened page
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            //if (System.Web.HttpContext.Current.Session["FS"] == null) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "TMP", col = obj.col }));

            ViewBag.q0list = lop;
            ViewBag.collist = new List<string>(CompTables);
            DBsearch DBS = new DBsearch();
            DBS.col = obj.col;
            DBS.db = obj.db;
            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.FieldsStr = obj.FieldsStr;
            DBS.FieldsIncExc = obj.FieldsIncExc;
            if (obj.FieldsIncExc == "Exc")
            {
                DBS.FieldsExc = DBS.FieldsExc = true;
                DBS.FieldsInc = false;
            }
            DBS.AddCoreFields();
            if (!String.IsNullOrEmpty(obj.SortStr)) TempData["JumpToBottom"] = "yes";
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "_id";
            DBS.Skip = obj.Skip;
            DBS.jumpBottom = obj.jumpBottom;
            db.genericFind(DBS, DataTableLimit);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            DBS.TableFields = (new MDBTablesFields()).ReadTableFields(DBS.col, General.TableType.Comp);
            foreach (string field in DBS.Fields) DBS.TableFields.Add(field);


            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch = new DBsearchSave(DBS);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "CMP";

            if (Roles.IsUserInRole("CCD_EV_COMP")) return View("SearchCOMP", DBS);
            return View("SearchCOMPv", DBS);

        }

        public ActionResult SearchCOMPArch(DBsearch obj)
        {
            if (obj.col == null)
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.col != null)
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
                    if (obj.db != DB_CMP_Arch) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "CMP_Arch", col = obj.col }));
                }
                else
                {
                    //return RedirectToAction("Index"); DefaultCompCol
                    return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "CMP_Arch", col = DefaultCompCol }));
                }

            }
            //in case the session has gone, and submit was clicked on an opened page
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            //if (System.Web.HttpContext.Current.Session["FS"] == null) return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "TMP", col = obj.col }));

            ViewBag.q0list = lop;
            ViewBag.collist = new List<string>(CompTables);
            DBsearch DBS = new DBsearch();
            DBS.col = obj.col;
            DBS.db = obj.db;
            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.FieldsStr = obj.FieldsStr;
            DBS.FieldsIncExc = obj.FieldsIncExc;
            if (obj.FieldsIncExc == "Exc")
            {
                DBS.FieldsExc = DBS.FieldsExc = true;
                DBS.FieldsInc = false;
            }
            DBS.AddCoreFields();
            if (!String.IsNullOrEmpty(obj.SortStr)) TempData["JumpToBottom"] = "yes";
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "_id";
            DBS.Skip = obj.Skip;
            DBS.jumpBottom = obj.jumpBottom;
            db.genericFind(DBS, DataTableLimit);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            DBS.TableFields = (new MDBTablesFields()).ReadTableFields(DBS.col, General.TableType.Comp);
            foreach (string field in DBS.Fields) DBS.TableFields.Add(field);


            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch = new DBsearchSave(DBS);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "CMP";

            return View("SearchCOMPArch", DBS);

        }

        [CheckSessionFilter]
        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        [HttpPost]
        public ActionResult QuerySessions(string userids, string col,string _db)
        {
            DBsearch DBS;
            DBS = new DBsearch();

            DBS.Query = "In;SessionStart;" + userids;
            DBS.SortStr = "SessionStart";
            DBS.SortDirection = "DES";
            DBS.col = col;
            if (_db == "TMP") DBS.db = DB_TMP;
            if (_db == "REC") DBS.db = DB_REC;
            if (_db == "PRM") DBS.db = DB_PRM;
            if (_db == "CMP") DBS.db = DB_CMP;
            DBS.SearchOP = "AND";

            db.genericFind(DBS, DataTableLimit);
            if (_db == "TMP")
            {
                if (User.IsInRole("CCD_EV_GB"))
                {
                    DBsearch obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                    DBS.Query = (new DBpermissionsHandler()).getProjectPhaseQueryFromSessionObj(obj) + ';' + DBS.Query;
                }
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "TMP";
                return RedirectToAction("SearchTMP", DBS);
            }

            if (_db == "REC")
            {
                if (User.IsInRole("CCD_EV_GB"))
                {
                    DBsearch obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                    DBS.Query = (new DBpermissionsHandler()).getProjectPhaseQueryFromSessionObj(obj) + ';' + DBS.Query;
                }
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "TMP";
                return RedirectToAction("SearchREC", DBS);
            }

            if (_db == "PRM")
            {
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "PRM";
                return RedirectToAction("SearchPRM", DBS);
            }
            if (_db == "CMP")
            {
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "CMP";
                return RedirectToAction("SearchCOMP", DBS);
            }
            return RedirectToAction("Index");
        }

        [CheckSessionFilter]
        [Authorize(Roles = "CCD_EV_GB,CCD_EV,CCD_EV_ADV")]
        [HttpPost]
        public ActionResult DeleteSessions(string userids, string col, string _db)
        {
            DBsearch DBS;
            DBS = new DBsearch();

            DBS.Query = "In;SessionStart;" + userids;
            DBS.SortStr = "SessionStart";
            DBS.SortDirection = "DES";

            DBS.col = col;
            if (_db == "TMP") DBS.db = DB_TMP;
            if (_db == "CMP") DBS.db = DB_CMP;
            DBS.SearchOP = "AND";

            db.genericFind(DBS, DataTableLimit);
            if (_db == "TMP")
            {
                if (User.IsInRole("CCD_EV_GB"))
                {
                    DBsearch obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                    DBS.Query = (new DBpermissionsHandler()).getProjectPhaseQueryFromSessionObj(obj) + ';' + DBS.Query;
                }
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "TMP";
            }
            if (_db == "CMP")
            {
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "CMP";
            }
            return RedirectToAction("DeleteMatchedRecs");
        }

        [HttpPost]
        public ActionResult RestoreSessions(string userids, string col, string _db)
        {
            DBsearch DBS;
            DBS = new DBsearch();

            DBS.Query = "In;SessionStart;" + userids;
            DBS.SortStr = "SessionStart";
            DBS.SortDirection = "DES";

            DBS.col = col;
            if (_db == "TMP") DBS.db = DB_TMP;
            if (_db == "REC") DBS.db = DB_REC;
            if (_db == "CMP") DBS.db = DB_CMP;
            DBS.SearchOP = "AND";

            db.genericFind(DBS, DataTableLimit);
            if (_db == "TMP")
            {
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "TMP";
            }
            if (_db == "CMP")
            {
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "CMP";
            }
            return RedirectToAction("RestoreMatchedRecs");
        }

        [CheckSessionFilter]
        [CheckPermissionFilter]
        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        [HttpPost]
        public ActionResult LoadToPRM(string userids, string load, string col)
        {
            DBsearch DBS, obj;
            DBS = new DBsearch();


            //obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch;

            //DBS.Query = "In;SessionStart;" + ((FullSession)System.Web.HttpContext.Current.Session["FS"]).SessionsToPRM.ToString();
            DBS.Query = "In;SessionStart;" + userids;
            DBS.SortStr = "SessionStart";

            //DBS.col = obj.col;
            DBS.col = col;
            //DBS.db = obj.db;
            DBS.db = DB_TMP;
            DBS.SearchOP = "AND";

            db.genericFind(DBS,0);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch = new DBsearchSave(DBS);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "TMP";
            if (load == "yes")
            {
                Boolean success = db.loadToPRM(DBS);
                ////TODO: handle error case
                if (success)
                {
                    new SessionManageHandler().LoadToPRMSessionManageUpdate(userids,col);
                    TempData["message"] = "Sessions " + userids + " were loaded succesfully to perm DB  ";
                    return RedirectToAction("SwitchTable", new RouteValueDictionary(new { db = "TMP", col = DBS.col }));
                }
                else
                {
                    TempData["message"] = "An error occured while loading sessions: " + userids + " to perm DB, please try again  ";
                    //return RedirectToAction("SearchTMP", DBS); 
                    return View(DBS);
                }
            }
            else
            {
                TempData["message"] = "You are about to load sessions: " + userids + " to perm DB, click to load  ";
                TempData["sessions"] = userids;
            }
            return View(DBS);
            
        }

        //
        // GET: /ManageDB/test

        [CheckPermissionFilter]
        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        public ActionResult ExportEditFileBG()
        {
            DBsearch DBS = new DBsearch();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRM")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRMArch")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "TMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "CMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            BsonDocument jmap = new BsonDocument();
            jmap.Add("DBSquery", DBS.Query);
            jmap.Add("DBSdb", DBS.db);
            jmap.Add("DBScol", DBS.col);
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("type", "1");
            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "ccd_export_" + DBS.col + "_" + nowStr + ".xlsx", "", jmap));

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "ccd_export_" + DBS.col + "_" + nowStr + ".xlsx", "", jmap);
            //_task.status = "Export";
            //MDBUserBGTaskRec.SaveUserBGTask(_task);
            //JobWorker worker = new JobWorker();
            //worker.ExecExportEditFileBG(DBS, nowStr, _task.id);

            return RedirectToAction("MyBGTasks");
        }

        [CheckPermissionFilter]
        [Authorize(Roles = "CCD_EV_GB,CCD_EV,CCD_EV_ADV")]
        public ActionResult ExportReportFileBG(string sheets, string fields, string TestNameField, string template)
        {
            DBsearch DBS = new DBsearch();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRM")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRMArch")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "TMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "CMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            BsonDocument jmap = new BsonDocument();
            jmap.Add("DBSquery", DBS.Query);
            jmap.Add("DBSdb", DBS.db);
            jmap.Add("DBScol", DBS.col);
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("sheets", sheets);
            jmap.Add("fields", fields);
            jmap.Add("TestNameField", TestNameField);
            jmap.Add("template", template);
            jmap.Add("type", "6");
            //TODO: find better way to do this
            if (template.Contains("exportRepCellQueryTmpl")) jmap.Set("type", "14");
            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "ccd_export_report_" + DBS.col + "_" + nowStr + ".xlsx", "", jmap));

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "ccd_export_report_" + DBS.col + "_" + nowStr + ".xlsx", "", jmap);
            //_task.status = "Export";
            //MDBUserBGTaskRec.SaveUserBGTask(_task);
            //JobWorker worker = new JobWorker();
            //UserBGTask UBT = _task;
            //string DBSquery = UBT.jmap.GetValue("DBSquery", "").ToString();
            //string DBSdb = UBT.jmap.GetValue("DBSdb", "").ToString();
            //string DBScol = UBT.jmap.GetValue("DBScol", "").ToString();
            //string nowstr = UBT.jmap.GetValue("nowStr", "").ToString();
            //string type = UBT.jmap.GetValue("type", "").ToString();
            //string DUT = UBT.jmap.GetValue("DUT", "").ToString();
            //string vTableStr = UBT.jmap.GetValue("vTableStr", "").ToString();
            //string debug = UBT.jmap.GetValue("debug", "").ToString();
            //string baseUrl = UBT.jmap.GetValue("baseUrl", "").ToString();
            //string FieldSlidesStr = UBT.jmap.GetValue("FieldSlidesStr", "").ToString();
            //string proj = UBT.jmap.GetValue("proj", "").ToString();
            //string phase = UBT.jmap.GetValue("phase", "").ToString();
            //string mails = UBT.jmap.GetValue("mails", "").ToString();

            //string _sheets = UBT.jmap.GetValue("sheets", "").ToString();
            //string _fields = UBT.jmap.GetValue("fields", "").ToString();
            //string _TestNameField = UBT.jmap.GetValue("TestNameField", "").ToString();
            //string _template = UBT.jmap.GetValue("template", "").ToString();
            //DBsearch _DBS = new DBsearch();
            //_DBS.db = DBSdb;
            //_DBS.Query = DBSquery;
            //_DBS.col = DBScol;

            //if (!template.Contains("exportRepCellQueryTmpl")) worker.ExportReportFileBG(_DBS, nowStr, _task.id, _sheets, _fields, _TestNameField, _template);
            //else worker.ExportReportFileCellQueriesBG(_DBS, nowStr, _task.id, _sheets, _fields, _TestNameField, _template);

            return RedirectToAction("MyBGTasks");
        }


        public ActionResult ExportVisualizationFileBG(string group, string visFields)
        {
            DBsearch DBS = new DBsearch();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRM")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRMArch")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "TMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "CMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            BsonDocument jmap = new BsonDocument();
            jmap.Add("DBSquery", DBS.Query);
            jmap.Add("DBSdb", DBS.db);
            jmap.Add("DBScol", DBS.col);
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("group", group);
            jmap.Add("visFields", visFields);
            jmap.Add("type", "8");

            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "ccd_visualization_" + DBS.col + "_" + nowStr + ".csv", "", jmap));

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "ccd_visualization_" + DBS.col + "_" + nowStr + ".csv", "", jmap);
            //_task.status = "Export";
            //MDBUserBGTaskRec.SaveUserBGTask(_task);
            //JobWorker worker = new JobWorker();
            //UserBGTask UBT = _task;
            //string DBSquery = UBT.jmap.GetValue("DBSquery", "").ToString();
            //string DBSdb = UBT.jmap.GetValue("DBSdb", "").ToString();
            //string DBScol = UBT.jmap.GetValue("DBScol", "").ToString();
            //string nowstr = UBT.jmap.GetValue("nowStr", "").ToString();

            //DBsearch _DBS = new DBsearch();
            //_DBS.db = DBSdb;
            //_DBS.Query = DBSquery;
            //_DBS.col = DBScol;

            //worker.ExportVisualizationFileBG(_DBS, nowStr, _task.id, group,visFields);

            return RedirectToAction("MyBGTasks");
        }

        //
        // GET: /ManageDB/test

        public ActionResult test()
        {
            return View();
        }
        //
        // GET: /ManageDB/test1

        public ActionResult test1()
        {
           throw new NotImplementedException();
           return View();
        }

        //
        // GET: /ManageDB/DeleteMatchedRecs
        [CheckSessionFilter]
        [CheckPermissionFilter]
        [Authorize(Roles = "CCD_EV_GB,CCD_EV,CCD_EV_ADV")]
        public ActionResult DeleteMatchedRecs()
        {
            bool errorOccurred,success;
            int errorCode;
            String errorMsg;

            success = false;
            DBsearch DBS = new DBsearch();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRM")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "TMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "CMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
            //new
            DBS.Skip = 0;
            DBS.Sort = new SortByBuilder();
            db.genericFind(DBS, 0);

            TempData["message"] = "All matched record for query:'" + DBS.Query + "' where deleted from DB ";
            try
            {
                foreach (BsonDocument item in DBS.ResCursor)
                {
                    //success = db.dbDelete(DBS, item.AsBsonDocument.GetElement("_id").Value.ToString(),true, out errorOccurred, out errorCode, out errorMsg);
                    success = db.moveToREC(DBS);
                }
                if (!success) TempData["message"] = "ERROR: Not all matched record for query:'" + DBS.Query + "' where deleted from DB ";
            }
            catch
            {
                TempData["message"] = "ERROR: Not all matched record for query:'" + DBS.Query + "' where not deleted from DB ";
            }

            //return RedirectToAction("Search", DBS);
            return RedirectToAction("Index");
        }

        public ActionResult RestoreMatchedRecs()
        {
            bool errorOccurred, success;
            int errorCode;
            String errorMsg;

            success = false;
            DBsearch DBS = new DBsearch();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRM")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "TMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
            if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "CMP")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
            //new
            DBS.Skip = 0;
            DBS.Sort = new SortByBuilder();
            db.genericFind(DBS, 0);

            TempData["message"] = "All matched record for query:'" + DBS.Query + "' where resored to TMP DB ";
            try
            {
                foreach (BsonDocument item in DBS.ResCursor)
                {
                    //success = db.dbDelete(DBS, item.AsBsonDocument.GetElement("_id").Value.ToString(),true, out errorOccurred, out errorCode, out errorMsg);
                    success = db.moveFromREC2TMP(DBS);
                }
                if (!success) TempData["message"] = "ERROR: Not all matched record for query:'" + DBS.Query + "' where resored to TMP DB ";
            }
            catch
            {
                TempData["message"] = "ERROR: Not all matched record for query:'" + DBS.Query + "' where resored to TMP DB ";
            }

            return RedirectToAction("SearchREC", DBS);
        }

        //
        // POST: /ManageDB/Delete
        [CheckSessionFilter]
        [CheckPermissionFilter]
        [Authorize(Roles = "CCD_EV_GB,CCD_EV,CCD_EV_ADV")]
        [HttpPost]
        public ActionResult Delete(FormCollection formCollection)
        {
            bool errorOccurred,success;
            int errorCode;
            String errorMsg;

            try
            {
                DBsearch DBS = new DBsearch();
                if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRM")
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "TMP")
                   DBS= ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "CMP")
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();

                foreach (var key in formCollection.AllKeys)
                {
                    if (key.ToString().StartsWith("_id="))
                    {
                        //success = db.dbDelete(DBS, key.ToString().Substring(4),true, out errorOccurred, out errorCode, out errorMsg);
                        success = db.dbMoveToREC(DBS, key.ToString().Substring(4), true, out errorOccurred, out errorCode, out errorMsg);
                        
                    }
                }

                return RedirectToAction("Index");
              }
            catch
            {
                return View();
            }
        }

        //
        // POST: /ManageDB/Delete
        [CheckSessionFilter]
        [HttpPost]
        public ActionResult Restore(FormCollection formCollection)
        {
            bool errorOccurred, success;
            int errorCode;
            String errorMsg;

            try
            {
                DBsearch DBS = new DBsearch();
                if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRM")
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "TMP")
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "CMP")
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();

                foreach (var key in formCollection.AllKeys)
                {
                    if (key.ToString().StartsWith("_id="))
                    {
                        //success = db.dbDelete(DBS, key.ToString().Substring(4),true, out errorOccurred, out errorCode, out errorMsg);
                        success = db.dbRestoreToTMP(DBS, key.ToString().Substring(4), true, out errorOccurred, out errorCode, out errorMsg);

                    }
                }

                return RedirectToAction("SearchREC", DBS);
            }
            catch
            {
                return RedirectToAction("SearchREC");
            }
        }
        //
        // GET: /ManageDB/SwitchTable?db=db_logic_name&col=colname
        public ActionResult SwitchTable(string db,string col)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();

            int queryDays = 0;
            //using session for now, may use app cahche(?) or singelton
            DBsearch DBS = new DBsearch();
            if (db == "TMP") DBS.db = DB_TMP;
            if(db == "REC") DBS.db = DB_REC;
            if (db == "PRM") DBS.db = DB_PRM;
            if (db == "PRM_Arch") DBS.db = DB_PRM_Arch;
            if (db == "CMP") DBS.db = DB_CMP;
            if (db == "CMP_Arch") DBS.db = DB_CMP_Arch;
            DBS.SortStr = "TestEnd";
            if (col == "HotCornerStats") DBS.SortStr = "TimeStamp";
            DBS.SortDirection = "DES";
            this.db.setDBandCol(DBS.db, col, DBS);
            DBS.col = col;
            //DBS.Query = "Matches;_id;/^./";

            queryDays = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).QuearyDays;
            if ((queryDays == -1) && (User.Identity.IsAuthenticated))
            {
                {
                    string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
                    queryDays = new MDBUsers().getQueryDays(username);
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).QuearyDays = queryDays;
                }
            }
            if (queryDays > 0) DBS.Query = "GT;SessionStart;" + DateTime.Now.AddDays(-1 * queryDays).ToString("yyyyMMdd") + ";Matches;_id;/^./";
            else DBS.Query = "Matches;_id;/^./";

            if (User.IsInRole("CCD_EV_GB"))
            {
                DBS.Query = (new DBpermissionsHandler()).getPermissionQueryForTable(DBS.col, User.Identity.Name) + ';' + DBS.Query;
            }
            

            DBS.jumpBottom = false;


            if (db == "TMP")
            {
                //write fields to fields table in DB
                (new MDBTablesFields()).SaveTableFields(DBS.col,General.TableType.Dev, DBS.Fields);
                return RedirectToAction("SearchTMP", DBS);
            }
            if (db == "REC")
            {
                return RedirectToAction("SearchREC", DBS);
            }
            if (db == "PRM")
            {
                return RedirectToAction("SearchPRM", DBS);
            }
            if (db == "PRM_Arch")
            {
                return RedirectToAction("SearchPRMArch", DBS);
            }
            if (db == "CMP")
            {
                (new MDBTablesFields()).SaveTableFields(DBS.col, General.TableType.Comp, DBS.Fields);
                return RedirectToAction("SearchCOMP", DBS);
            }
            if (db == "CMP_Arch")
            {
                (new MDBTablesFields()).SaveTableFields(DBS.col, General.TableType.Comp, DBS.Fields);
                return RedirectToAction("SearchCOMPArch", DBS);
            }
            return RedirectToAction("Index");
        }

        //  
        // GET: /ManageDB/LoadFile
        [CheckSessionFilter]
        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult LoadFile(string _db)
        {
            DBsearch DBS = new DBsearch();
            DBS = new DBsearch();
            DBS.Query = "Matches;_id;/^./";
            DBS.SortStr = "TestEnd";
            DBS.SortDirection = "DES";

            if (_db == "TMP")
            {
                ViewBag.collist = new List<string>(MongoTables);

                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                }
                else
                {
                    DBS.db = DB_TMP;
                    DBS.col = DefaultCol;
                }
            }
            if (_db == "CMP")
            {
                ViewBag.collist = new List<string>(CompTables);

                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
                }
                else
                {
                    DBS.db = DB_CMP;
                    DBS.col = DefaultCompCol;
                }
            }
            FileLoad FL = new FileLoad();
            FL.col = DBS.col;
            FL.db = _db;
            return View(FL);
        }

        // This action handles the form POST and the upload
        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        [CheckPermissionFilter]
        [HttpPost]
        //public ActionResult LoadFile(HttpPostedFileBase file,string _db, string col, string Mfile)
        public ActionResult LoadFile(HttpPostedFileBase file,string _db, string col)
        {
            string fileName="";
            string path = "";
            List<BsonDocument> ResultsRecs = null;
            List<BsonDocument> ValidRecs = new List<BsonDocument>();
            List<BsonDocument> NonValidRecs = new List<BsonDocument>();
            List<BsonDocument> ExistRecs = new List<BsonDocument>();
            List<BsonDocument> NoFileRecs = new List<BsonDocument>();
            ExcelLoaderHelper Ehelper = new ExcelLoaderHelper();
            FileLoad FL = new FileLoad();
            FL.col = col;
            FL.db = _db;

            ViewBag.collist = new List<string>(MongoTables); 
            if (_db == "CMP")ViewBag.collist = new List<string>(CompTables);
            // Verify that the user selected a file
            //if ((file != null) && (file.ContentLength > 0) && (string.IsNullOrWhiteSpace(Mfile)))
            if ((file != null) && (file.ContentLength > 0))
            {
                // extract only the fielname
                fileName = Path.GetFileName(file.FileName);
                // store the file inside ~/App_Data/uploads folder
                //var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);
                path = Path.Combine(ServerDir,"uploads", fileName);
                try
                {
                    file.SaveAs(path);
                }
                catch (Exception e)
                {
                    TempData["message"] = "Error: cannot save Excel file " + e.Message;
                    return View(FL);
                }
            }

            //if (!string.IsNullOrWhiteSpace(Mfile))
            //{
            //    fileName = Mfile;
            //    path = Path.Combine(ServerDir, "uploads", fileName);
            //    if (!System.IO.File.Exists(path))
            //    {
            //        TempData["message"] = "Error: cannot find Excel file " + path;
            //        return View(FL);
            //    }
            //}
            if (string.IsNullOrWhiteSpace(path))
            {
                    TempData["message"] = "Error: cannot find Excel file details";
                    return View(FL);
            }
                ExcelHandler Eloader = new ExcelHandler();
                ResultsRecs = Eloader.ExcelToBsList(path);

                if (_db == "TMP")
                {
                    db.setDBandCol(DB_TMP, col);
                    foreach (BsonDocument item in ResultsRecs)
                    {
                        if (Ehelper.validate(item))
                        {
                            if ((db.IsRecExist(item, DB_TMP)) || (db.IsRecExist(item, DB_PRM)) || (db.IsRecExist(item, DB_PRM_Arch))) ExistRecs.Add(item);
                            else
                            {
                                if (Ehelper.AreFilesExist(item)) ValidRecs.Add(item);
                                else NoFileRecs.Add(item);
                            }
                        }
                        else NonValidRecs.Add(item);
                    }
                }
                if (_db == "CMP")
                {
                    db.setDBandCol(DB_CMP, col);
                    foreach (BsonDocument item in ResultsRecs)
                    {
                        if (Ehelper.validate(item))
                        {
                            if (db.IsRecExist(item, DB_CMP)) ExistRecs.Add(item);
                            else
                            {
                                if (Ehelper.AreFilesExist(item)) ValidRecs.Add(item);
                                else NoFileRecs.Add(item);
                            }
                        }
                        else NonValidRecs.Add(item);
                    }
                }
                //TODO: write detailed message to file on SD and display tha path in the web message
                TempData["message"] = "";
                if (NonValidRecs.Count > 0)
                {
                    TempData["message"] += "Error: Excel file contains the following " + NonValidRecs.Count + " non valid records\n";
                    log.Error("Error: Excel file " + fileName + "contains the following " + NonValidRecs.Count + " non valid records");
                    foreach (BsonDocument item in NonValidRecs)
                    { log.Error(item.GetElement("_id").ToString()); TempData["message"] += item.GetElement("_id").ToString() + "\n"; }
                }
                if (ExistRecs.Count > 0)
                {
                    TempData["message"] += "Error: Excel file contains the following " + ExistRecs.Count + " records, already exist in DB\n";
                    log.Error("Error: Excel file " + fileName + "contains the following " + ExistRecs.Count + " records, already exist in DB");
                    foreach (BsonDocument item in ExistRecs) { log.Error(item.GetElement("_id").ToString()); TempData["message"] += item.GetElement("_id").ToString() + "\n"; }
                }
                if (NoFileRecs.Count > 0)
                {
                    TempData["message"] += "Error: Excel file contains the following " + NoFileRecs.Count + "records, attachments files were not found\n";
                    log.Error("Error: Excel file " + fileName + "contains the following " + NoFileRecs.Count + " records, attachments files were not found");
                    foreach (BsonDocument item in NoFileRecs) { log.Error(item.GetElement("_id").ToString()); TempData["message"] += item.GetElement("_id").ToString() + "\n"; }
                }
                if ((ExistRecs.Count == 0) && (NonValidRecs.Count == 0) && (NoFileRecs.Count == 0))
                {
                    Boolean error = false;
                    foreach (BsonDocument item in ValidRecs)
                    {
                        if (!db.dbInsert(item))
                        {
                            TempData["message"] = "Error: could not save the following record to DB: " + item.ToString();
                            log.Error("Error: Excel file " + fileName + " could not save the following record to DB: " + item.ToString());
                            error = true;
                            break;
                        }
                    }
                    if (!error) TempData["message"]="Records were successfully loaded to DB.";
                }
            // redirect back to the index action to show the form once again
            //return RedirectToAction("LoadFile");

            return View(FL);
        }


        // GET: /ManageDB/LoadLANFile
        [CheckSessionFilter]
        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        public ActionResult LoadLANFile()
        {
            return View();
        }
        // 
        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        [HttpPost]
        //public ActionResult LoadFile(HttpPostedFileBase file,string _db, string col, string Mfile)
        public ActionResult LoadLANFile(HttpPostedFileBase file, string _db, string col,string project, string phase)
        {
            string fileName = "";
            string path = "";
            string errMessage = "";
            string Message = "";

            if ((file != null) && (file.ContentLength > 0))
            {
                // extract only the fielname
                fileName = Path.GetFileName(file.FileName);
                if ((fileName.ToUpper().Contains("BER")) && (col.Contains("TTL")) ||
                     (fileName.ToUpper().Contains("TTL")) && (col.Contains("BER")))
                {
                    TempData["message"] = "Error: filename \"" + fileName + "\" does not match requested table \"" + col + "\"";
                    return RedirectToAction("SearchPRM", "ManageDB");
                }
                // store the file inside ~/App_Data/uploads folder
                //var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);
                path = Path.Combine(ServerDir, "uploads", fileName);
                try
                {
                    file.SaveAs(path);
                }
                catch (Exception e)
                {
                    TempData["message"] = "Error: cannot save Excel file " + e.Message;
                    return RedirectToAction("SearchPRM", "ManageDB");
                }
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                TempData["message"] = "Error: cannot find Excel file details";
                return RedirectToAction("SearchPRM", "ManageDB");
            }
            
            //online load was too long, Depricated:
            //FXVLLoader Floader = new FXVLLoader();
            //if (Floader.LoadLANFile(out errMessage,out Message, path, project, phase, col)) TempData["messageInfo"] = "File was loaded succesfully " + Message;
            //else TempData["message"] = "Error: cannot load Excel file " + errMessage;
            //return RedirectToAction("SearchPRM", "ManageDB");
            
            //move it to BG
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            BsonDocument jmap = new BsonDocument();
            jmap.Add("FilePath", path);
            jmap.Add("proj", project);
            jmap.Add("phase", phase);
            jmap.Add("table", col);
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("type", "17");
            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "Load_LAN_File_" + fileName + "_" + nowStr, "", jmap));

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "Load_LAN_File_" + fileName + "_" + nowStr, "", jmap);
            //_task.status = "Load";
            //MDBUserBGTaskRec.SaveUserBGTask(_task);
            //JobWorker worker = new JobWorker();
            //worker.LoadLanFile(_task.id, path, col, project, phase);


            return RedirectToAction("MyBGTasks");
        }

        public ActionResult ViewExportFile(string filename)
        {
             string filepath = Path.Combine(ServerDir,"tmp",filename);
             if (System.IO.File.Exists(filepath)) return new FilePathResult(filepath, "application/vnd.ms-excel") { FileDownloadName = filename };
             else 
             {
                 TempData["message"] = "ERROR: cannot find file " + filename;
                  return RedirectToAction("Index");
             }
        }

        public ActionResult ViewExportBGFile(string filename)
        {
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            if (System.IO.File.Exists(filepath)) return new FilePathResult(filepath, "application/vnd.ms-excel") { FileDownloadName = Path.GetFileName(filename) };
            else
            {
                TempData["message"] = "ERROR: cannot find file " + filename;
                return RedirectToAction("MyBGTasks");
            }
        }

        public ActionResult MyQueries()
        {
            bool s;
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;

            MongoCursor Cur = db.ReadUserQueries(username);

            return View("MyQueries", Cur);
        }
        public ActionResult MyPreferences()
        {
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            ViewBag.QueryDays = (new MDBUsers()).getQueryDays(username).ToString();
            ViewBag.Mail = (new MDBUsers()).getMail(username); 
            return View();
        }
        public ActionResult PreferencesUpdate(string QueryDays, string mailAddress)
        {
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            if (!(new MDBUsers()).setQueryDays(username,  Int32.Parse(QueryDays)))
            {
                TempData["message"] = "ERROR: cannot update preferences;";
            }
            if (!(new MDBUsers()).setMail(username, mailAddress))
            {
                TempData["message"] = "ERROR: cannot update preferences;";
            }
            int queryDays = (new MDBUsers()).getQueryDays(username);
            ViewBag.QueryDays = queryDays.ToString();
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).QuearyDays = queryDays;
            ViewBag.Mail = (new MDBUsers()).getMail(username); 
            return View("MyPreferences");
        }
        public ActionResult MyBGTasks(string SourceID)
        {
            bool s;
            MongoCursor Cur;
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            List<UserBGTask> UserBGTasksList = MDBUserBGTaskRec.ReadAllUserBGTasks(username);
            //iterate results to check if PANDA created pdf/ppt
            foreach (UserBGTask item in UserBGTasksList)
            {
                if ((item.desc.Substring(0, 6) == "report") && (item.result != "") && ((item.result2 == "") || (item.result3 == "")))
                {
                    if (item.checkIfReportCreated()) MDBUserBGTaskRec.SaveUserBGTask(item);
                }
            }

            if (!String.IsNullOrEmpty(SourceID)) Cur = MDBUserBGTaskRec.ReadCursorUserBGTasksBySourceID(username, SourceID);
            else Cur = MDBUserBGTaskRec.ReadCursorAllUserBGTasks(username);
            return View("MyBGTasks", Cur);
        }

        public ActionResult DeleteBGTAsk(string _id)
        {
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            if (!MDBUserBGTaskRec.DeleteUserBGTask(_id)) TempData["message"] = "There was a problem deleting " + _id + "BGTask record";
            return RedirectToAction("MyBGTasks");
        }

        public ActionResult DeleteBGTasks(FormCollection formCollection)
        {
            bool success;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            foreach (var key in formCollection.AllKeys)
            {
                if (key.ToString().StartsWith("_cb"))
                {
                    success = MDBUserBGTaskRec.DeleteUserBGTask(key.ToString().Substring(3));
                }
            }
            return RedirectToAction("MyBGTasks");
        }

        public ActionResult StatusReportDisplayMissDetails(string permutations, string[] field, string testDetails)
        {
            ViewBag.testDetails = testDetails;
            List<string> Dperms = new List<string>();
            string[] perms = permutations.Split(';');
            for(int i=0;i<perms.Length-1;i+=2)
            {
                Dperms.Add(perms[i] + ";" + perms[i + 1]);
            }
            ViewBag.permutations = Dperms;
            ViewBag.fieldsArr = new List<string>();
            if (field!=null) ViewBag.fieldsArr = field;
            return View();
        }

        [CheckSessionFilter]
        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        [HttpPost]
        public ActionResult LogSessions(string userids, string col, string _db)
        {
            DBsearch DBS;
            DBS = new DBsearch();

            DBS.Query = "In;SessionStart;" + userids;
            DBS.SortStr = "SessionStart";
            DBS.SortDirection = "DES";

            DBS.col = col;
            if (_db == "TMP") DBS.db = DB_TMP;
            if (_db == "CMP") DBS.db = DB_CMP;
            DBS.SearchOP = "AND";

            db.genericFind(DBS, DataTableLimit);
            if (_db == "TMP")
            {
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "TMP";
                return RedirectToAction("ExecutionLogNew", DBS);
            }
            if (_db == "CMP")
            {
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch = new DBsearchSave(DBS);
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed = "CMP";
                return RedirectToAction("ExecutionLogNew", DBS);
            }
            return RedirectToAction("Index");
        }

        [CheckPermissionFilter]
        public ActionResult ExecutionLogNew(DBsearch obj,string colname, string logDB)
        {
            string table;
            if ((obj.col) == null && (logDB=="TMP"))
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.col != null)
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                    obj.Query="Matches;_id;/^./";
                    obj.SortStr = "_id";
                }
                else
                {
                    obj.db = DB_TMP;
                    obj.col = DefaultCol;
                }
                if (!string.IsNullOrEmpty(colname))
                {
                    obj.col = colname;
                }
            }
            if ((obj.col) == null && (logDB == "CMP"))
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.col != null)
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
                    obj.Query = "Matches;_id;/^./";
                    obj.SortStr = "_id";
                }
                else
                {
                    obj.db = DB_CMP;
                    obj.col = DefaultCompCol;
                }
                if (!string.IsNullOrEmpty(colname))
                {
                    obj.col = colname;
                }
            }

            ViewBag.q0list = lop;
            if ((logDB == "TMP") || (obj.db == DB_TMP))
            {
                ViewBag.logDB = "TMP";
                ViewBag.collist = new List<string>(MongoTables);
            }
            if ((logDB == "CMP") || (obj.db == DB_CMP))
            {
                ViewBag.logDB = "CMP";
                ViewBag.collist = new List<string>(CompTables);
            }
            if (string.IsNullOrEmpty(obj.Query)) obj.Query = "Matches;_id;/^./";
            DBsearch DBS = new DBsearch();
            table = obj.col;
            DBS.col = "Executions";
            DBS.db = obj.db;
            table = table.Replace("+", @"\+");
            DBS.Query = "Matches;_id;/" + table + "/;";
            DBS.Query += obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.FieldsStr = obj.FieldsStr;
            DBS.FieldsIncExc = obj.FieldsIncExc;
            if (obj.FieldsIncExc == "Exc")
            {
                DBS.FieldsExc = DBS.FieldsExc = true;
                DBS.FieldsInc = false;
            }
            DBS.AddCoreFields();
            if (!String.IsNullOrEmpty(obj.SortStr)) TempData["JumpToBottom"] = "yes";
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "_id";
            DBS.Skip = obj.Skip;
            DBS.jumpBottom = obj.jumpBottom;
            db.genericFind(DBS, DataTableLimit);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            foreach (string field in DBS.Fields) DBS.TableFields.Add(field);
            DBS.col = table;
            DBS.Query = obj.Query;
            DBS.QueryTriplets.RemoveAt(0);
            return View("ExecutionLogNew", DBS);

        }

        //
        // GET: /ManageDB/OfficialIndMatchedRecs
        [CheckSessionFilter]
        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        public ActionResult OfficialIndMatchedRecs(string Include, string comment)
        {
            HashSet<string> SessionsEffected = new HashSet<string>();
            BsonDocument firstResult = null;
            if (string.IsNullOrEmpty(comment))
            {
                TempData["message"] = "You must enter a comment to exclude/include results in official indicator ";
                return RedirectToAction("Index");
            }
            bool errorOccurred, success;
            int errorCode;
            String errorMsg;

            success = false;
            DBsearch DBS = new DBsearch();
            //if (((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastDBViewed == "PRM")
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
            //new
            DBS.Skip = 0;
            DBS.Sort = new SortByBuilder();
            db.genericFind(DBS, 0);

            TempData["messageInfo"] = "All matched record for query:'" + DBS.Query + "' updated to be included in official indicator ";
            if (Include=="N") TempData["messageInfo"] = "All matched record for query:'" + DBS.Query + "' updated to be excluded from official indicator ";
            try
            {
                foreach (BsonDocument item in DBS.ResCursor)
                {
                    if (firstResult == null) firstResult = item;
                    string[,] values = new string[1, 2] { { "Indicator", Include }};
                    success = db.dbupdate(item.GetValue("_id").ToString(), values, out errorOccurred, out errorCode, out errorMsg);
                    //New for SessionManage
                    if (!SessionsEffected.Contains(item.GetValue("SessionStart", "").ToString())) SessionsEffected.Add(item.GetValue("SessionStart", "").ToString());
                }
                if (!success) TempData["message"] = "ERROR: Not all matched record for query:'" + DBS.Query + "' where updated correctly ";
            }
            catch
            {
                TempData["message"] = "ERROR: Not all matched record for query:'" + DBS.Query + "' where updated correctly ";
            }

            //New for SessionManage
            try
            {
                new SessionManageHandler().IndicatorsIncExcSessionManageUpdate(DBS.col, DBS.Query, SessionsEffected, Include, comment);
            }
            catch
            {
                TempData["message"] = "ERROR: updating SessionsManage for query:'" + DBS.Query + "' encoutered an error ";
            }

            //TODO: add log record including: query, user,comment,date etc
            MDBBsonDoc MDBBsonDoc = new MDBBsonDoc("DB_PRM", "IndicatorsUserLog");
            BsonDocument doc = new BsonDocument();
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            doc.Set("_id",DBS.col + "_" + nowStr);//table + timestamp?
            doc.Set("table",DBS.col);
            doc.Set("query", DBS.Query);
            doc.Set("user", FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name);
            doc.Set("comment", comment);
            string action="Include";
            if (Include=="N") action="Exclude";
            doc.Set("Action", action);
            doc.Set("date", DateTime.Now.ToString("yyyyMMdd"));
            doc.Set("resultsNum", DBS.ResCursor.Count());
            doc.Set("ProjectName", firstResult.GetValue("ProjectName"));
            doc.Set("Phase", firstResult.GetValue("Phase"));
            MDBBsonDoc.SaveRec(doc);

            //return RedirectToAction("Search", DBS);
            return RedirectToAction("Index");
        }

        [CheckPermissionFilter]
        public ActionResult ShowLogicUnitLinks(string col, string Query, string PRMQuery, string SRFile, string Unit, string protocol, string proj, string phase)
        {
            String strPathAndQuery = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
            String strUrl = System.Web.HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");
            LogicUnitLinks Links = new LogicUnitLinks();
            Links.Protocol = protocol;
            Links.Project = proj;
            Links.Phase = phase;
            Links.Unit = Unit;
            Links.Table = col;
            if (string.IsNullOrEmpty(Query)) Links.TMPQuery = "";
            else Links.TMPQuery = strUrl + "ManageDB/SearchTMP?db=" + System.Configuration.ConfigurationManager.AppSettings["DB_TMP"] + "&col=" + col + "&Query=" + Query;
            if (string.IsNullOrEmpty(PRMQuery)) Links.PRMQuery = "";
            else Links.PRMQuery = strUrl + "ManageDB/SearchPRM?db=" + System.Configuration.ConfigurationManager.AppSettings["DB_PRM"] + "&col=" + col + "&Query=" + PRMQuery;
            Links.SRFile = SRFile;
            Links.CustomExecFile = SRFile.Replace(".xlsx", ".txt");
            return View(Links);
        }


        ////////////////

        public ActionResult BoardsLinesClear(DBsearch obj, string arcdb)
        {
            DBsearch DBS = new DBsearch();
            DBS.col = "Boards";
            DBS.db = DB_PRM;
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).BoardSearch = new DBsearchSave(DBS);
            return RedirectToAction("BoardsLines");
        }

        public ActionResult BoardsLines(DBsearch obj, string arcdb)
        {
            DBsearch DBS = new DBsearch();
            DBS.col = "Boards";
            DBS.db = DB_PRM;
            if (!string.IsNullOrWhiteSpace(arcdb) && (arcdb.ToUpper() == "TRUE")) DBS.db = DB_CMP_Arch;
            ViewBag.q0list = lop;

            if (obj.Query == null)
            {

                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    (((FullSession)System.Web.HttpContext.Current.Session["FS"]).BoardSearch.Query != null)
                    && (((FullSession)System.Web.HttpContext.Current.Session["FS"]).BoardSearch.db == DBS.db))
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).BoardSearch.getNewDBS();
                }
                else
                {
                    obj.Query = "EQ;Project;" + ProjectsList[0] + ";EQ;Phase;" + PhasesList[0] + ";Matches;Name;/^./";
                    obj.SearchOP = "AND";
                    obj.SortStr = "_id";
                    obj.SortDirection = "DES";
                }

            }

            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "_id";

            String[] qarr = DBS.Query.Split(';');
            for (int i = 0; i < qarr.Length; i += 3)
            {
                DBS.QueryTriplets.Add((qarr[i]) + ";" + (qarr[i + 1]) + ";" + (qarr[i + 2]));
            }
            MDBBoard db = new MDBBoard();
            DBS.ResCursor = db.MDBFind(DBS.Query, DBS.SearchOP, DBS.SortDirection, DBS.SortStr);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            DBS.TableFields = (new Board()).GetBoardFields();
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).BoardSearch = new DBsearchSave(DBS);
            MDBExecutions mdbExecutions = new MDBExecutions(DB_TMP);
            string timeStampStr;
            double timeStamp;
            foreach (BsonDocument doc in DBS.ResCursor)
            {
                Board line = new Board(doc);
                if (line.LastExecution != null) timeStampStr = line.LastExecution.GetValue("LastUpdated", 0).ToString();
                else timeStampStr = "0";
                timeStamp = Convert.ToDouble(timeStampStr);
                if (Convert.ToDouble(DateTime.Now.ToString("yyyyMMddHHmmss")) - timeStamp > 600)//Ten minutes from last update
                {
                    line.LastExecution = mdbExecutions.ReadLastSysExecutionForBoard(line.Project, line.Phase, line.Name);
                    if (line.LastExecution == null) line.LastExecution = new BsonDocument();
                    if ((string.IsNullOrEmpty(line.LastExecution.GetValue("Status", "").ToString())) || (line.LastExecution.GetValue("Status", "").ToString() == "Finished")) line.InUse = "N";
                    else
                    {
                        line.InUse = "Y";
                        if (Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd")) - Convert.ToInt32(line.LastExecution.GetValue("TimeStamp", 20150101).ToString().Substring(0, 8)) > 2) line.InUse = "N";
                    }
                    line.LastExecution.Set("LastUpdated", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    db.Save(line);
                }
            }
            return View("BoardsLines", DBS);
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult CreateBoardLine()
        {
            return View("EditBoardLine", new Board());
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult EditBoardLine(string _id)
        {

            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phaselist = new List<string>(PhasesList);

            Board line = (new MDBBoard()).Read(_id);            

            return View("EditBoardLine", line);
        }


        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult EditBoardLineExec(string _id, string _Name, string _Project, string _Phase, string _CommentText, string _CommentUser, string _UnitId, string _Skew, string _Fuse, string _Sigma, string _Interface)
        {

            if ((string.IsNullOrEmpty(_id) &&
                ((string.IsNullOrEmpty(_Name)) || (string.IsNullOrEmpty(_Project)) || (string.IsNullOrEmpty(_Phase)) || (string.IsNullOrEmpty(_UnitId)) || (string.IsNullOrEmpty(_Skew)) || (string.IsNullOrEmpty(_Sigma))
                    || (string.IsNullOrEmpty(_Interface)) || (string.IsNullOrEmpty(_CommentText)) || (string.IsNullOrEmpty(_CommentUser)))))
            {
                TempData["message"] = "Please fill all fields";
                return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
            }
            
            if ((string.IsNullOrEmpty(_id)) && ((_Interface=="Power") || (_Interface=="USB")) && (string.IsNullOrEmpty(_Fuse)))
            {
                TempData["message"] = "Please fill all fields, Fuse is mandatory for Power\\USB";
                return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
            }
            if ((!string.IsNullOrEmpty(_id) &&
                ((string.IsNullOrEmpty(_CommentText)) || (string.IsNullOrEmpty(_CommentUser)))))
            {
                TempData["message"] = "Please fill all fields";
                return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
            }


            MDBBoard mdbBoard = new MDBBoard();
            double _SigmaD,_FuseD;
            if (!Double.TryParse(_Sigma, out _SigmaD))
            {
                {
                    TempData["message"] = "Sigma must be numeric";
                    return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
                }
            }
            if (!Double.TryParse(_Fuse, out _FuseD))
            {
                TempData["message"] = "Fuse must be numeric";
                return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
            }
            Board newBoard = new Board(_Name, _Project, _Phase, _UnitId, _CommentText,_CommentUser,_Skew, _FuseD, _SigmaD, _Interface);

            //in case of new board, check if we already have one with this id 
            if (String.IsNullOrWhiteSpace(_id))
            {
                Board tBoard = mdbBoard.Read(newBoard.id);
                if (tBoard.id == newBoard.id)
                {
                    TempData["message"] = "You cannot create Board '" + newBoard.id + "' because it is already exists in DB";
                    return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
                }

                //check if we already have one with this Boardname or UnitId 
                MongoCursor cur = mdbBoard.MDBFind("EQ;Project;" + _Project + ";EQ;Phase;" + _Phase , "AND", "DES", "_id");
                foreach (BsonDocument doc in cur)
                {
                    Board eBoard = new Board(doc);
                    if ((eBoard.Name.ToUpper() == _Name.ToUpper()) && (eBoard.Valid=="Y"))
                    {
                        TempData["message"] = "You cannot create Board '" + newBoard.id + "' because board already exists in DB, need to delete or invalidate: " + eBoard.id;
                        return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
                    }
                    if ((eBoard.UnitId.ToUpper() == _UnitId.ToUpper()) && (eBoard.Valid=="Y") && (eBoard.Skew == _Skew))
                    {
                        TempData["message"] = "You cannot create Board '" + newBoard.id + "' because unit already exists in DB, need to delete or invalidate: "+ eBoard.id + " Skew: " + eBoard.Skew;
                        return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
                    }
                }
            }

            //update exist board
            else
            {
                Board tBoard = mdbBoard.Read(_id);
                if (!string.IsNullOrEmpty(tBoard.LastCommentText)) tBoard.SavedComments.Add(new Comment(tBoard.LastCommentText, tBoard.LastCommentUser, tBoard.LastCommentDate));
                tBoard.LastCommentDate = newBoard.LastCommentDate;
                tBoard.LastCommentText = newBoard.LastCommentText;
                tBoard.LastCommentUser = newBoard.LastCommentUser;
                if (!string.IsNullOrEmpty(_Sigma)) tBoard.Sigma = _SigmaD;
                if (!string.IsNullOrEmpty(_Fuse)) tBoard.Fuse = _FuseD;
                newBoard = tBoard;
            }
            if (!mdbBoard.Save(newBoard))
            {
                TempData["message"] = "Edit Board failed due to internal error, please try again later";
                return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
            }

            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            DBsearch DBS = new DBsearch();
            DBS.Query = "EQ;Project;" + _Project + ";EQ;Phase;" + _Phase + ";Matches;_id;/^./";
            DBS.SearchOP = "AND";
            DBS.SortStr = "_id";
            DBS.SortDirection = "DES";
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CalibrationSearch = new DBsearchSave(DBS);

            return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = newBoard.id }));
        }


        [Authorize(Roles = "CCD_EV")]
        public ActionResult DeleteBoardLine(string _id)
        {
            if (!(new MDBBoard()).Delete(_id)) TempData["message"] = "Delete board failed due to internal error, please try again later";
            return RedirectToAction("BoardsLines");
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult DeleteBoardLineComment(string _id)
        {
            MDBBoard mdbBoard = new MDBBoard();
            Board tBoard = mdbBoard.Read(_id);
            if (tBoard.SavedComments.Count > 0)
            {
                tBoard.LastCommentText = tBoard.SavedComments[tBoard.SavedComments.Count - 1].text;
                tBoard.LastCommentDate = tBoard.SavedComments[tBoard.SavedComments.Count - 1].commentDate;
                tBoard.LastCommentUser = tBoard.SavedComments[tBoard.SavedComments.Count - 1].user;
                tBoard.SavedComments.RemoveAt(tBoard.SavedComments.Count - 1);
            }
            else
            {
                tBoard.LastCommentText = "";
                tBoard.LastCommentDate = "";
                tBoard.LastCommentUser = "";
            }
            
            mdbBoard.Save(tBoard);
            return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult DeleteBoardLineOldComment(string _id, int desc)
        {
            MDBBoard mdbBoard = new MDBBoard();
            Board tBoard = mdbBoard.Read(_id);
            tBoard.SavedComments.RemoveAt(desc);
            mdbBoard.Save(tBoard);
            return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult ValidationBoardLineUpdate(string _id, string v, string _CommentText, string _CommentUser)
        {
            if ((!string.IsNullOrEmpty(_id) &&
                ((string.IsNullOrEmpty(_CommentText)) || (string.IsNullOrEmpty(_CommentUser)))))
            {
                TempData["message"] = "Please fill validate comment fields";
                return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
            }


            MDBBoard mdbBoard = new MDBBoard();
            Board existBoard = mdbBoard.Read(_id);
            if (String.IsNullOrWhiteSpace(existBoard.id))
            {
                TempData["message"] = "Can not find board ID:" + _id;
                return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
            }

            //In case of validate, check if we already have one with this Boardname 
            if (v == "true")
            {
                MongoCursor cur = mdbBoard.MDBFind("EQ;Project;" + existBoard.Project + ";EQ;Phase;" + existBoard.Phase, "AND", "DES", "_id");
                foreach (BsonDocument doc in cur)
                {
                    Board otherBoard = new Board(doc);
                    if ((otherBoard.Name.ToUpper() == existBoard.Name.ToUpper()) && (otherBoard.Valid == "Y") && (otherBoard.id != existBoard.id))
                    {
                        TempData["message"] = "You cannot validate Board '" + existBoard.id + "' because board already exists in DB, need to delete or invalidate: " + otherBoard.id;
                        return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
                    }
                }
            }


            if (!string.IsNullOrEmpty(existBoard.LastCommentText)) existBoard.SavedComments.Add(new Comment(existBoard.LastCommentText, existBoard.LastCommentUser, existBoard.LastCommentDate));
            existBoard.LastCommentText = _CommentText;
            existBoard.LastCommentUser = _CommentUser;
            existBoard.LastCommentDate = DateTime.Now.ToString("yyyyMMdd");
            if (v == "false") existBoard.Valid = "N"; 
            if (v == "true") existBoard.Valid = "Y";
            if (!(new MDBBoard()).Save(existBoard)) TempData["message"] = "Update board validation failed due to internal error, please try again later";
            return RedirectToAction("EditBoardLine", new RouteValueDictionary(new { _id = _id }));
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_LIVE")]
        public ActionResult LiveIndicatorsView()
        {          
            List <BGTaskSchedule> BGTaskScheduleList = (new MDBBGTaskSchedule()).ReadAllLiveIndicatorsBGTasksSchedule(); 
            return View(BGTaskScheduleList);
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_LIVE")]
        public ActionResult LiveIndicators(string proj, string phase)
        {
            ViewBag.proj = proj;
            ViewBag.phase = phase;
            ViewBag.date = System.IO.File.GetLastWriteTime(Path.Combine(ServerDir, "tmp", "Graph_" + proj + "_" + phase + ".png")).ToString("yyyyMMdd_HHmmss");
            return View();
        }
        [Authorize(Roles = "CCD_EV,CCD_EV_LIVE")]
        public ActionResult LiveIndicatorsRotation(int pos)
        {
            List<BGTaskSchedule> BGTaskScheduleList = (new MDBBGTaskSchedule()).ReadAllLiveIndicatorsBGTasksSchedule();
            BGTaskSchedule CurrBGTaskSchedule = BGTaskScheduleList.ElementAt(pos);
            string proj = CurrBGTaskSchedule.jmap.GetValue("proj").ToString();
            string phase = CurrBGTaskSchedule.jmap.GetValue("phase").ToString();
            ViewBag.proj = proj;
            ViewBag.phase = phase;
            ViewBag.date = System.IO.File.GetLastWriteTime(Path.Combine(ServerDir, "tmp", "Graph_" + proj + "_" + phase + ".png")).ToString("yyyyMMdd_HHmmss");
            pos += 1;
            if (pos + 1 > BGTaskScheduleList.Count) pos = 0;
            ViewBag.pos = pos;
            return View();
        }

        public ActionResult RenderImage(string name)
        {

            byte[] photoBack = System.IO.File.ReadAllBytes(Path.Combine(ServerDir, "tmp", name + ".png"));

            return File(photoBack, "image/png");
        }

    }

}
