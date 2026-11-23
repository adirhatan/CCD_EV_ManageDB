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

namespace CCD_EV_ManageDB.Controllers
{
    [LogActionFilter]
    public class MonitorDBController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ManageDBController));
        private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }

        private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        private readonly int DataTableLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DataTableLimit"]);

        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }

        private MongoDBClient db_tmp = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);
        private MongoDBClient db_prm = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);

        //private List<string> lop = new List<string>(new string[] { "EQ", "Matches", "In", "GT", "LT" });
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }
        public string[] MongoTables { get { return System.Configuration.ConfigurationManager.AppSettings["MongoTables"].Split(','); } }
        //
        // GET: /MonitorDB/

        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        public ActionResult Index()
        {
            return RedirectToAction("MyMonitorTables");
            //old depricated
            MonitorData MD = new MonitorData();
            foreach (string table in MongoTables)
            {
                DBsearch DBS = new DBsearch();
                DBS.db = DB_TMP;
                DBS.col = table;
                DBS.SortStr = "TestEnd";
                DBS.SortDirection = "DES";
                DateTime now = DateTime.Now.Date;
                //TODO Session of today? last 5 days?
                now = now.AddDays(-3);

                DBS.Query = "GT;TestEnd;"+ now.ToString("yyyyMMdd");
                DBS.FieldsStr = "TestEnd;_id;SessionStart;ProjectName;Phase;Skew;TestStart;Corner;BoardNumber;UnitId";
                DBS.FieldsInc = true;
                DBS.jumpBottom = false;
                db_tmp.genericFind(DBS, 5);
                MD.MonitorResults.Add(table, DBS.ResCursor);
            }
            MD.Fields = new HashSet<string>(new string[] { "TestEnd", "_id", "SessionStart", "ProjectName", "Phase", "Skew", "TestStart", "Corner", "BoardNumber", "UnitId" });
            return View(MD);
        }

        public ActionResult AjaxTest()
        {
            MonitorData MD = new MonitorData();
            //string table = "Test_DP_src0";
            foreach (string table in MongoTables)
            {

                DBsearch DBS = new DBsearch();
                DBS.db = DB_TMP;
                DBS.col = table;
                DBS.SortStr = "TestEnd";
                DBS.SortDirection = "DES";
                DateTime now = DateTime.Now.Date;
                //TODO Session of today? last 5 days?
                now = now.AddDays(-3);

                //DBS.Query = "GT;TestEnd;" + now.ToString("yyyyMMdd");
                DBS.Query = "Matches;_id;/^./";
                DBS.FieldsStr = "TestEnd;_id;SessionStart;ProjectName;Phase;Skew;TestStart;Corner;BoardNumber;UnitId";
                DBS.FieldsInc = true;
                DBS.jumpBottom = false;
                db_tmp.genericFind(DBS, 5);
                MD.MonitorResults.Add(table, DBS.ResCursor);
            }
            MD.Fields = new HashSet<string>(new string[] { "TestEnd", "_id", "SessionStart", "ProjectName", "Phase", "Skew", "TestStart", "Corner", "BoardNumber", "UnitId" });
            return View(MD);
        }

        public ActionResult AjaxData(string table)
        {
            MonitorData MD = new MonitorData();
            //string table = "Test_DP_src0";
                
                DBsearch DBS = new DBsearch();
                DBS.db = DB_TMP;
                DBS.col = table;
                DBS.SortStr = "TestEnd";
                DBS.SortDirection = "DES";
                DateTime now = DateTime.Now.Date;
                //TODO Session of today? last 5 days?
                now = now.AddDays(-3);

                //DBS.Query = "GT;TestEnd;" + now.ToString("yyyyMMdd");
                DBS.Query = "Matches;_id;/^./";
                DBS.FieldsStr = "TestEnd;_id;SessionStart;ProjectName;Phase;Skew;TestStart;Corner;BoardNumber;UnitId";
                DBS.FieldsInc = true;
                DBS.jumpBottom = false;
                db_tmp.genericFind(DBS, 5);
                string res = "{" + "\n" + "\"" + "data" + "\"" + ": [" + "\n";
                foreach (BsonDocument item in DBS.ResCursor)
                {
                    res += item.ToJson();
                    res += ",";
                }
                res = res.TrimEnd(new char[] {','});
                res += "]" + "\n" + "}";
                Response.Write(res);
                return null;
        }
        


        public ActionResult AjaxTable()
        {
            MonitorData MD = new MonitorData();
            return View(MD);
        }

        public ActionResult data()
        {
             string filepath = Path.Combine(ServerDir,"tmp","arrays.txt");
             if (System.IO.File.Exists(filepath)) return new FilePathResult(filepath, "text/plain");
             else return RedirectToAction("Index");
        }
        public ActionResult RunMonitorTables(string _id, string refresh)
        {
            VMonitorTables VMT = new VMonitorTables();
            string errMsg = "";
            VMT.loadUserMonitorTables(_id,out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                TempData["message"] = errMsg;
                return RedirectToAction("MyMonitorTables");
            }
            if (!string.IsNullOrWhiteSpace(refresh)) VMT.refresh = refresh;
            return View(VMT);
        }


        //public ActionResult Monitor(string tableName,string hostName)
        //{
        //    VMonitorTables VMT = new VMonitorTables();
        //    MDBExecutions MDB_Executions = new MDBExecutions();
        //    MonitorTable MT = new MonitorTable(tableName, hostName);
        //    string session = null;
        //    string status = null;
        //    MT.ExecutionRecords = MDB_Executions.ReadCursorLastSession(tableName, hostName);
        //    MT.initSesisonVals();
        //    BsonDocument curExecutionitem = MDB_Executions.ReadLastExecutionForTable(tableName, hostName);
        //    if (curExecutionitem != null)
        //    {
        //        session = curExecutionitem.GetElement("SessionStart").Value.ToString();
        //        status = curExecutionitem.GetElement("Status").Value.ToString();
        //    }
        //    MT.ExecutionCurStatus = status;
        //    MDBStationResults MDB_Staion = new MDBStationResults("DB_TMP", tableName);
        //    MT.TableCurResult = MDB_Staion.ReadLastResultForTable(hostName);
        //    MT.prepareTable();

        //    return View(MT);
        //}

        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        [HttpPost]
        public ActionResult loadMonitorTables(string mont, string name, string hostName,string dbName)
        {
            bool s;
            MDBUsersMonitorTables MDBUMont = new MDBUsersMonitorTables();
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            UsersMonitorTables UMont = new UsersMonitorTables(username, name);
            UMont.MTables.Add(new MTable(mont, hostName, "DB_TMP"));
            s = MDBUMont.SaveUserMonitorTables(UMont);
            TempData["message"] = "MonitorTables new Set:\"" + name + "\" was created. (" + mont + ";"+hostName+")";
            return RedirectToAction("MyMonitorTables");
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        public ActionResult AddExistMonitorTables(string _id,string mont, string hostName,string dbName)
        {
            //TODO add order
            bool s;
            MDBUsersMonitorTables MDBUMont = new MDBUsersMonitorTables();
            UsersMonitorTables UMont = MDBUMont.ReadUserMonitorTables(_id);
            UMont.MTables.Add(new MTable(mont, hostName, "DB_TMP"));
            s = MDBUMont.SaveUserMonitorTables(UMont);
            TempData["message"] = "MonitorTables Set:\"" + UMont.Name + "\" updated. added:(" + mont + ";" + hostName + ")";
            return RedirectToAction("MyMonitorTables");
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        public ActionResult MyMonitorTables()
        {
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.collist.Add("");
            MDBUsersMonitorTables MDBUMont = new MDBUsersMonitorTables();
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;

            MongoCursor Cur = MDBUMont.ReadCursorAllUserMonitorTables(username);

            return View("MyMonitorTables", Cur);
        }

        public ActionResult ViewAllMonitorTables()
        {
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.collist.Add("");
            MDBUsersMonitorTables MDBUMont = new MDBUsersMonitorTables();

            MongoCursor Cur = MDBUMont.ReadCursorAllUserMonitorTables("");

            return View("ViewAllMonitorTables", Cur);
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        public ActionResult DeleteMonitorTables(string _id)
        {
            MDBUsersMonitorTables MDB_UMT = new MDBUsersMonitorTables();
            bool s = MDB_UMT.DeleteUserMonitorTables(_id);
            if (!s)
            {
                UsersMonitorTables UMont = MDB_UMT.ReadUserMonitorTables(_id);
                TempData["message"] = "Deletion of set " + UMont.Name + " failed please try again later";
            }
            return RedirectToAction("MyMonitorTables");
        }


        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        [HttpPost]
        public ActionResult RenameMonitorTables(string _id, string name)
        {
            bool s;
            MDBUsersMonitorTables MDB_UMT = new MDBUsersMonitorTables();
            s = MDB_UMT.RenameMonitorSet(_id, name);
            if (s) Response.Write(name);
            else Response.Write("Rename Failed:" + name);
            return null;
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_ADV")]
        public ActionResult DeleteLastTable(string _id)
        {
            MDBUsersMonitorTables MDB_UMT = new MDBUsersMonitorTables();
            bool s = MDB_UMT.DeleteLastTable(_id);
            if (!s)
            {
                UsersMonitorTables UMont = MDB_UMT.ReadUserMonitorTables(_id);
                TempData["message"] = "Deletion of last table from set " + UMont.Name + " failed please try again later";
            }
            return RedirectToAction("MyMonitorTables");
        }
    }
}
