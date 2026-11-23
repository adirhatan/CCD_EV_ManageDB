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
using System.Reflection;

namespace CCD_EV_ManageDB.Controllers
{
    [Authorize(Roles = "CCD_EV_COMP")]
    [LogActionFilter]
    public class CompReportDBController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ManageDBController));
        private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }

        private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        private readonly int DataTableLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DataTableLimit"]);

        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }
        private static string DB_CMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP"]; } }

        //private MongoDBClient db = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);

        private List<string> lop = new List<string>(new string[] { "EQ", "Matches", "NotMatches", "In", "NotIn", "GT", "LT" });
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }
        public string[] MongoTables { get { return System.Configuration.ConfigurationManager.AppSettings["MongoTables"].Split(','); } }
        public string[] CompTables { get { return System.Configuration.ConfigurationManager.AppSettings["CertificationTables"].Split(','); } }
        //
        // GET: /CompReport/

        public ActionResult Index()
        {
            return RedirectToAction("CompReports");
        }


        public ActionResult CompReports()
        {
            MDBInventory mdbInventory = new MDBInventory();
            ViewBag.boardNumbers = new List<string>(mdbInventory.ReadAllInventoryPlatforms());
            Dictionary<string, HashSet<string>> dictionaryBioses = (mdbInventory.getDictionaryAllInventoryPlatformsBIOS());
            ViewBag.NGon.dictionaryBioses = dictionaryBioses;
            Dictionary<string, HashSet<string>> dictionaryNvms = (mdbInventory.getDictionaryAllInventoryPlatformsNVM());
            ViewBag.NGon.dictionaryNvms = dictionaryNvms;
            Dictionary<string, HashSet<string>> dictionaryUnitIDS = (mdbInventory.getDictionaryAllInventoryPlatformsUnitIDS());
            ViewBag.NGon.dictionaryUnitIDS = dictionaryUnitIDS;
            

            List<string> tNames = new List<string>();
            foreach (string tName in (Directory.GetFiles(Path.Combine(ServerDir, "EVData"), "CompTmpl*"))) tNames.Add(Path.GetFileNameWithoutExtension(tName));
            ViewBag.tmplates = tNames;
            ViewBag.collist = new List<string>(CompTables);
            ViewBag.defFieldsList = new List<string>((new MDBTablesFields()).ReadTableFields(CompTables[0],General.TableType.Comp));
            MethodInfo[] methodInfos = typeof(CompReportFormatter).GetMethods();
            List<string> dispMethods = new List<string>();
            dispMethods.Add("");
            foreach (MethodInfo mInf in methodInfos)
            {
                if (mInf.Name == "ToString") break;
                dispMethods.Add(mInf.Name);
            }
            ViewBag.disp = dispMethods;
            ViewBag.results = new List<string>(new string[] { "L0-L3", "PA-PB","S","D0-D2","CK-D0-D2","Clock","L0-L1","L2-L3","IMG" });
            if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.col != null)
            {
                DBsearch DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
                ViewBag.CurQueryDisplay = "Table:\"" + DBS.col + "\" Query:\"" + DBS.Query + "\"";
            }
            else
            {
                ViewBag.CurQueryDisplay = "";
            }

            //new
            Dictionary<string, string> ports = new Dictionary<string, string>();
            Dictionary<string, string> devTypes = new Dictionary<string, string>();
            foreach (string inv in ViewBag.boardNumbers)
            {
                ports.Add(inv, (mdbInventory.ReadInventory(inv)).Ports);
                devTypes.Add(inv, (mdbInventory.ReadInventory(inv)).DevType);
            }
            ViewBag.NGon.ports = ports;
            ViewBag.NGon.devTypes = devTypes;
            ViewBag.NGon.tmplH1 = "";
            ViewBag.NGon.tmplH2 = "";
            ViewBag.NGon.tmplD1 = "";
            ViewBag.NGon.tmplD2 = "";
            foreach (string tmpl in ViewBag.tmplates)
            {
                if ((tmpl.EndsWith("2P")) && (tmpl.ToUpper().Contains("HOST")) && (ViewBag.NGon.tmplH2 == "")) ViewBag.NGon.tmplH2 = tmpl;
                if ((tmpl.EndsWith("1P")) && (tmpl.ToUpper().Contains("HOST")) && (ViewBag.NGon.tmplH1 == "")) ViewBag.NGon.tmplH1 = tmpl;
                if ((tmpl.EndsWith("2P")) && (tmpl.ToUpper().Contains("DEVICE")) && (ViewBag.NGon.tmplD2 == "")) ViewBag.NGon.tmplD2 = tmpl;
                if ((tmpl.EndsWith("1P")) && (tmpl.ToUpper().Contains("DEVICE")) && (ViewBag.NGon.tmplD1 == "")) ViewBag.NGon.tmplD1 = tmpl;
            }
            //
            
            return View("CompReports",(new MDBCompReportLines()).ReadAllCompReportLines());
        }

        public ActionResult CompReportsEdit (string tab)
        {
            MDBInventory mdbInventory = new MDBInventory();
            ViewBag.boardNumbers = new List<string>(mdbInventory.ReadAllInventoryPlatforms());
            Dictionary<string, HashSet<string>> dictionaryBioses = (mdbInventory.getDictionaryAllInventoryPlatformsBIOS());
            ViewBag.NGon.dictionaryBioses = dictionaryBioses;
            Dictionary<string, HashSet<string>> dictionaryNvms = (mdbInventory.getDictionaryAllInventoryPlatformsNVM());
            ViewBag.NGon.dictionaryNvms = dictionaryNvms;
            Dictionary<string, HashSet<string>> dictionaryFields = (new MDBTablesFields()).GetTablesFieldsDictionary(General.TableType.Comp, CompTables);
            ViewBag.NGon.dictionaryFields = dictionaryFields;
            List<string> tNames = new List<string>();
            foreach (string tName in (Directory.GetFiles(Path.Combine(ServerDir, "EVData"), "CompTmpl*"))) tNames.Add(Path.GetFileNameWithoutExtension(tName));
            ViewBag.tmplates = tNames;
            ViewBag.collist = new List<string>(CompTables);
            ViewBag.defFieldsList = new List<string>((new MDBTablesFields()).ReadTableFields(CompTables[0], General.TableType.Comp));
            MethodInfo[] methodInfos = typeof(CompReportFormatter).GetMethods();
            List<string> dispMethods = new List<string>();
            dispMethods.Add("");
            foreach (MethodInfo mInf in methodInfos)
            {
                if (mInf.Name == "ToString") break;
                dispMethods.Add(mInf.Name);
            }
            ViewBag.disp = dispMethods;
            ViewBag.results = new List<string>(new string[] { "L0-L3", "PA-PB", "S", "D0-D2","CK-D0-D2", "Clock", "L0-L1", "L2-L3","IMG" });
            if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.col != null)
            {
                DBsearch DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
                ViewBag.CurQueryDisplay = "Table:\"" + DBS.col + "\" Query:\"" + DBS.Query + "\"";
            }
            else
            {
                ViewBag.CurQueryDisplay = "";
            }
            if (String.IsNullOrWhiteSpace(tab)) tab = "TBT_TX";
            ViewBag.tab = tab;
            return View("CompReportsEdit", (new MDBCompReportLines()).ReadMatchedIDQeuryCompReportLines("/^(" + tab + "|IMG_" + tab + ").*/"));
            //return View("CompReportsEdit", (new MDBCompReportLines()).ReadAllCompReportLines());
        }

        public ActionResult CreateLine(string lName, string lQuery, string tableName, string Field1, string Field2, string Field3, string disp1, string disp2,string disp3, string results,string tab)
        {
            if (!(new MDBCompReportLines()).SaveCompReportLine(new DBTmpl(lName, lQuery, tableName, Field1, Field2, Field3, disp1, disp2, disp3,results)))
            {
                TempData["message"]="Create new line failed due to internal error, please try again later";
            }
            //return RedirectToAction("CompReportsEdit");
            return RedirectToAction("CompReportsEdit", new RouteValueDictionary(new { tab = tab }));
        }
        public ActionResult CopyLine(string _id,string tab)
        {
            MDBCompReportLines dbLine = new MDBCompReportLines();
            DBTmpl line = dbLine.ReadCompReportLine(_id);           
            if (String.IsNullOrWhiteSpace(line.name))
            {
                TempData["message"] = "ERROR: cannot copy report";
                return RedirectToAction("CompReportsEdit", new RouteValueDictionary(new { tab = tab }));
            }
            string c_name = line.name + "_copy";
            int cnt = 0;
            while (!String.IsNullOrWhiteSpace(dbLine.ReadCompReportLine(c_name).name))
            {
                cnt++;
                c_name = line.name + "_copy" + cnt.ToString();
                if (cnt>100)
                {
                    TempData["message"] = "ERROR: cannot copy report";
                    return RedirectToAction("CompReportsEdit", new RouteValueDictionary(new { tab = tab }));
                }
            }
            line.name = c_name;
            if (!dbLine.SaveCompReportLine(line))
            {
                TempData["message"] = "ERROR: cannot copy report";
                return RedirectToAction("CompReports");
            }
            //return RedirectToAction("CompReportsEdit");
            return RedirectToAction("CompReportsEdit", new RouteValueDictionary(new { tab = tab }));
        }

        public ActionResult EditLine(string _id,string tab)
        {
            DBTmpl line = (new MDBCompReportLines()).ReadCompReportLine(_id);
            Dictionary<string, HashSet<string>> dictionaryFields = (new MDBTablesFields()).GetTablesFieldsDictionary(General.TableType.Comp, CompTables);
            ViewBag.NGon.dictionaryFields = dictionaryFields;
            ViewBag.collist = new List<string>(CompTables);
            ViewBag.defFieldsList = new List<string>((new MDBTablesFields()).ReadTableFields(line.table, General.TableType.Comp));
            ViewBag.tab = tab;
            MethodInfo[] methodInfos = typeof(CompReportFormatter).GetMethods();
            List<string> dispMethods = new List<string>();
            dispMethods.Add("");
            foreach (MethodInfo mInf in methodInfos)
            {
                if (mInf.Name == "ToString") break;
                dispMethods.Add(mInf.Name);
            }
            ViewBag.disp = dispMethods;
            ViewBag.results = new List<string>(new string[] { "L0-L3", "PA-PB", "S", "D0-D2","CK-D0-D2", "Clock", "L0-L1","L2-L3", "IMG" });
            if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.col != null)
            {
                DBsearch DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CmpSearch.getNewDBS();
                ViewBag.CurQueryDisplay = "Table:\"" + DBS.col + "\" Query:\"" + DBS.Query + "\"";
            }
            else
            {
                ViewBag.CurQueryDisplay = "";
            }
            return View("EditLine", line);
        }
        public ActionResult EditLineExec(string _id, string lName, string lQuery, string tableName, string Field1, string Field2,string Field3, string disp1, string disp2,string disp3, string results, string tab)
        {
            MDBCompReportLines mdbCompReportLines = new MDBCompReportLines();
            if (_id != lName) 
            {
                if (!mdbCompReportLines.DeleteCompReportLine(_id))
                {
                    TempData["message"]="Edit line failed due to internal error, please try again later";
                    return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
                }
            }
            if (!mdbCompReportLines.SaveCompReportLine(new DBTmpl(lName, lQuery, tableName, Field1, Field2, Field3,disp1, disp2, disp3,results)))
            {
                TempData["message"]="Edit line failed due to internal error, please try again later";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }

            return RedirectToAction("CompReportsEdit", new RouteValueDictionary(new { tab = tab }));
            //return RedirectToAction("CompReportsEdit");
        }

        public ActionResult RunTestEditLine(string _id,string unitId)
        {
            DBTmpl tmpl = (new MDBCompReportLines().ReadCompReportLine(_id));
            DBsearch DBS = new DBsearch();
            DBS.db = DB_CMP;
            DBS.col = tmpl.table;
            DBS.Query = "EQ;BoardNumber;" + unitId + ";" + tmpl.query;
            return RedirectToAction("SearchCOMP", "ManageDB",DBS);
        }


        public ActionResult DeleteLine(string _id,string tab)
        {
            if (!(new MDBCompReportLines()).DeleteCompReportLine(_id)) TempData["message"] = "Delete line failed due to internal error, please try again later";
            //return RedirectToAction("CompReportsEdit");
            return RedirectToAction("CompReportsEdit", new RouteValueDictionary(new { tab = tab }));
        }

        [HttpPost]
        public ActionResult RenameLine(string _id, string name)
        {
            DBTmpl line;
            bool s;
            MDBCompReportLines mdbCompReportLines = new MDBCompReportLines();
            line = mdbCompReportLines.ReadCompReportLine(_id);
            line.name = name;
            s = mdbCompReportLines.SaveCompReportLine(line);
            s = mdbCompReportLines.DeleteCompReportLine(_id);
            if (s) Response.Write(name);
            else Response.Write("Rename Failed:" + name);
            return null;
        }

        public ActionResult RunReportBG(string boardNumber, string BIOS, string NVM,string UnitID, string templ)
        {
            //TODO complete: need a managment table: key,status, error (same col?) link when ready
            if (String.IsNullOrWhiteSpace(boardNumber))
            {
                TempData["message"] = "Please supply UnitId in order to create report";
                return RedirectToAction("CompReports");
            }

            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();

            String query = "EQ;BoardNumber;" + boardNumber;
            if (!string.IsNullOrEmpty(BIOS)) query += ";EQ;BIOS;" + BIOS;
            if (!string.IsNullOrEmpty(NVM)) query += ";EQ;NVM_REV;" + NVM;
            if (!string.IsNullOrEmpty(UnitID)) query += ";EQ;UnitId;" + UnitID;

            BsonDocument jmap = new BsonDocument();
            jmap.Add("DBSquery", query);
            jmap.Add("DBScol", templ);
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("type", "3");
            jmap.Add("DUT", boardNumber);// this value will be written in the report header


            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "CompReport_" + boardNumber + " " + nowStr, "", jmap)); //unmark and comment from here

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "CompReport_" + boardNumber + "_" + nowStr, "", jmap);
            //_task.status = "CompReport";

            //MDBUserBGTaskRec.SaveUserBGTask(_task);

            //JobWorker worker = new JobWorker();
            //worker.ExecRunCompReportBG(boardNumber, query, templ, nowStr, _task.id);

            return RedirectToAction("MyBGTasks", "ManageDB");
        }


        public ActionResult ValidateCompReport(string boardNumber, string BIOS, string NVM, string UnitID,string templ, string[] vTable)
        {
            if (vTable == null)
            {
                TempData["message"] = "Please select interface/s to validate";
                return RedirectToAction("CompReports");
            }
            string vTableStr = "";
            foreach (string t in vTable) vTableStr += t + ",";
            vTableStr=vTableStr.Substring(0,vTableStr.Length-1);

            if (!string.IsNullOrEmpty(boardNumber))
            {
                ViewBag.UnitID = boardNumber;
                ViewBag.Template = templ;
                string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
                string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
                string key = username + nowStr;
                MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();

                String query = "EQ;BoardNumber;" + boardNumber;
                if (!string.IsNullOrEmpty(BIOS)) query += ";EQ;BIOS;" + BIOS;
                if (!string.IsNullOrEmpty(NVM)) query += ";EQ;NVM_REV;" + NVM;
                if (!string.IsNullOrEmpty(UnitID)) query += ";EQ;UnitId;" + UnitID;

                String strPathAndQuery = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
                String strUrl = System.Web.HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");

                BsonDocument jmap = new BsonDocument();
                jmap.Add("DBSquery", query);
                jmap.Add("DBScol", templ);
                jmap.Add("nowStr", nowStr);
                jmap.Add("key", key);
                jmap.Add("type", "4");
                jmap.Add("DUT", boardNumber);
                jmap.Add("vTableStr", vTableStr);
                jmap.Add("baseUrl", strUrl);

                MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "CompReportVal_" + boardNumber + " " + nowStr, "", jmap));  //unmark and comment from here

                ////For testing on localhost (without service)
                //UserBGTask _task = new UserBGTask(key, "CompReportVal_" + boardNumber + "_" + nowStr, "", jmap);
                //_task.status = "CompReport";

                //MDBUserBGTaskRec.SaveUserBGTask(_task);

                //JobWorker worker = new JobWorker();
                //worker.ValidateCompReportBG(boardNumber, query, templ, nowStr, _task.id, vTableStr, strUrl);

                return RedirectToAction("MyBGTasks", "ManageDB");
                
            }

            TempData["message"] = "Please enter BoardNumber to validate";
            return RedirectToAction("CompReports");
        }

        public ActionResult CopyLinesPA2PB()
        {
            MDBCompReportLines Lines = new MDBCompReportLines();
            if (!Lines.CopyLinesPA2PB()) TempData["message"] = "Copy lines PA to PB encountered an error";
            return RedirectToAction("CompReportsEdit");
        }


    }
}
