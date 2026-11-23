using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
    [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
    [LogActionFilter]
    public class ReportDBController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ManageDBController));
        private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }

        private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        private readonly int DataTableLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DataTableLimit"]);

        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        private static string DB_PRM_Arch { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM_Arch"]; } }
        private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }

        private MongoDBClient db_tmp = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);
        private MongoDBClient db_prm = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);
        //private List<string> lop = new List<string>(new string[] { "EQ", "Matches", "In", "GT", "LT" });

        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }
        public string[] MongoTables { get { return System.Configuration.ConfigurationManager.AppSettings["MongoTables"].Split(','); } }
        //
        // GET: /ReportDB/

        public ActionResult Index()
        {
            //ViewBag.collist = new List<string>(MongoTables);
            //return View();
            return RedirectToAction("MyReports");
        }

        [Authorize(Roles = "CCD_EV")]
        [CheckPermissionFilter]
        public ActionResult CreateReport(string rName, string tableName, string tName, string refId, string rFromId, string pandaTemplate)
        {
            ViewBag.collist = new List<string>(MongoTables);
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;

            UserTableReport Report = new UserTableReport(username, rName, tableName);
            Report.TestName = tName;
            Report.RConfig.TitleItems.Add( new TitleItem("95","100","40","Alpine Ridge A0 Typ - socket\nAR PCIe DC \nElectrical Validation Results"));
            Report.RConfig.TitleItems.Add(new TitleItem("250", "30", "25", "WW37/2013"));
            Report.RConfig.TitleItems.Add(new TitleItem("290", "30", "25", "EV engineer: Amnon Zaideman"));
            Report.RConfig.TitleItems.Add(new TitleItem("370", "30", "15", "Note:\nThe current version of this document is\nintended for internal distribution only – WIP.\nPlease consult EM before copying/sending\ninformation from this document.\n"));
            Dictionary<string, HashSet<string>> dictionaryFields = (new MDBTablesTestsDictionary()).ReadTableTestsDictionary(Report.RConfig.table,General.TableType.Dev, Report.TestName);
            Report.RConfig.FieldsSlides.Add(new FieldSlide(General.ReportSlideType.limits.ToString(),"initial", dictionaryFields.ElementAt(0).Key, "","", "", "",null,""));
            //new
            if (!String.IsNullOrWhiteSpace(refId))
            {
                ADMReferenceTable refTable = (new MDBADMReferenceTable()).ReadADMReferenceTable(refId);
                Report.RConfig.TitleItems.Add(new TitleItem("330", "30", "15", "Spec:"+ refTable.Protocol + " " + refTable.PVersion));
                foreach (FieldSlide fs in refTable.RConfig.FieldsSlides)
                {
                    if (dictionaryFields.ContainsKey(fs.test))
                    {
                        Report.RConfig.FieldsSlides.RemoveAt(0);
                        break;
                    }
                }
                Report.RConfig.FieldsSlides.AddRange(refTable.RConfig.FieldsSlides);
                XMLReportHandler XMLer = new XMLReportHandler();
                List<string> Templates = XMLer.ParseTemplatesXML();
                if (string.IsNullOrEmpty(pandaTemplate)) pandaTemplate = Templates[0];
                foreach (FieldSlide fs in Report.RConfig.FieldsSlides)
                {
                    fs.templateKey = pandaTemplate;
                    if (string.IsNullOrEmpty(fs.margin)) fs.margin = "20";
                }
            }
            //
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            MDBReport.SaveUserTableReport(Report);

            //new
            //TODO: Add error handling
            if (!String.IsNullOrWhiteSpace(rFromId))
            {
                UserTableReport ReportFrom = MDBReport.ReadUserTableReport(rFromId);
                List<FieldSlide> tmpList = new List<FieldSlide>();

                foreach (FieldSlide FS in Report.RConfig.FieldsSlides)
                {
                    foreach (FieldSlide FSF in ReportFrom.RConfig.FieldsSlides)
                    {
                        if ((FS.test == FSF.test) && (FSF.column == FS.column)) FS.templateKey=FSF.templateKey;
                    }
                    tmpList.Add(FS);
                    foreach (FieldSlide FSF in ReportFrom.RConfig.FieldsSlides)
                    {
                        if ((FS.test == FSF.test) && (FSF.type == General.ReportSlideType.image.ToString())) tmpList.Add(FSF);
                    }
                }
                Report.RConfig.FieldsSlides = tmpList;
                Report.RConfig.ImagesDetails = ReportFrom.RConfig.ImagesDetails;
                MDBReport.SaveUserTableReport(Report);
            }


            return RedirectToAction("MyReports");
        }

        [Authorize(Roles = "CCD_EV")]
        [CheckPermissionFilter]
        public ActionResult CopyReport(string _id)
        {
            string tablestr = "";
            foreach (string Pkey in Request.Form.AllKeys)
            {
                var queryValue = Request.Form[Pkey];
                if (Regex.IsMatch(Pkey, @"table_.*"))
                {
                    Match m = Regex.Match(Pkey, @"table_(.*)");
                    if (queryValue.StartsWith("true")) tablestr += m.Groups[1].Value + ",";
                }

            }
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);
            if (string.IsNullOrEmpty(tablestr)) tablestr = Report.RConfig.table;
            tablestr = tablestr.TrimEnd(',');

            foreach (string table in (new List<string>(tablestr.Split(','))))
            {
                string c_id = MDBReport.CopyAndSaveUserTableReport(username, Report, table);
                if (c_id == null)
                {
                    TempData["message"] = "ERROR: cannot copy report";
                }
                System.Threading.Thread.Sleep(1000);//The key is in seconds resouloution, so wait to ensure valid keys
            }

            return RedirectToAction("MyReports");
        }

        public ActionResult CopyImagesFromReport(string _id, string _idFrom)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);
            UserTableReport ReportFrom = MDBReport.ReadUserTableReport(_idFrom);
            foreach (FieldSlide FS in Report.RConfig.FieldsSlides)
            {
                if (FS.type == General.ReportSlideType.image.ToString())
                {
                    TempData["message"] = "ERROR: must remove all images slides from current report before copying images";
                    return RedirectToAction("EditReport", new RouteValueDictionary(new { _id = _id, fromCopy = false }));
                }
            }
            List<FieldSlide> tmpList = new List<FieldSlide>();

            foreach (FieldSlide FS in Report.RConfig.FieldsSlides)
            {
                tmpList.Add(FS);
                int cnt = 0;
                foreach (FieldSlide FSF in ReportFrom.RConfig.FieldsSlides)
                {
                    if ((FS.test == FSF.test) && (FS.column==FSF.column))
                    {
                        if (cnt == ReportFrom.RConfig.FieldsSlides.Count - 1) break;
                        while(ReportFrom.RConfig.FieldsSlides.ElementAt(cnt++).type==General.ReportSlideType.image.ToString())
                            tmpList.Add(ReportFrom.RConfig.FieldsSlides.ElementAt(cnt));
                    }
                    cnt++;
                }
            }
            Report.RConfig.FieldsSlides = tmpList;
            Report.RConfig.ImagesDetails = ReportFrom.RConfig.ImagesDetails;

            return RedirectToAction("EditReport", new RouteValueDictionary(new { _id = _id, fromCopy = false }));
        }

        public ActionResult MyReports(string db)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null)
            {
                return RedirectToAction("Index","ManageDB");
            }
            DBsearchSave DBS = new DBsearchSave();
            string ReportDB = "PRM";
            //if (User.IsInRole("CCD_EV_GB")) db = "TMP"; //(Notice: overwrite parameter from html) dericated, allowed to search in PRM
            
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.Users = ((new MDBUsers()).ReadAllUsers());
            ViewBag.Users.Sort();
            List<string> reflist = (new MDBADMReferenceTable()).ReadListAllADMReferenceTablesIDS();
            reflist.Insert(0, "");
            ViewBag.reflist = reflist;
            XMLReportHandler XMLer = new XMLReportHandler();
            List<string> Templates = XMLer.ParseTemplatesXML();
            ViewBag.Templates = Templates;
           
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;

            MongoCursor Cur = MDBReport.ReadCursorAllUserTableReports(username);

            if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
            {
                DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch;
            }

            if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).ReportDB != null)
            {
                ReportDB = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).ReportDB;
            }

            if (!String.IsNullOrWhiteSpace(db)) ReportDB = db;
            if ((ReportDB != "PRM") && (ReportDB != "PRMArch"))
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch;
                }
            }
            
            ViewBag.db = ReportDB;
            ViewBag.Query = DBS.Query;
            ViewBag.col = DBS.col;
            if (((ReportDB == "PRM") && (DBS.db != null) && (DBS.db != DB_PRM)) || ((ReportDB == "PRMArch") && (DBS.db != null) && (DBS.db != DB_PRM_Arch)))
            {
                ViewBag.Query = "";
                ViewBag.col = "";
                TempData["message"] = "Please define query in " + ReportDB + " DB";
            }
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).ReportDB = ReportDB;
            return View("MyReports", Cur);
        }

        [Authorize(Roles = "CCD_EV")]
        [CheckPermissionFilter]
        public ActionResult EditReport(string _id,bool fromCopy)
        {    
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);
            if (fromCopy) ViewBag.collist = new List<string>(MongoTables);
            else ViewBag.collist = null;
            //populate list of tests and tests-fields
            Dictionary<string, HashSet<string>> dictionaryFields = (new MDBTablesTestsDictionary()).ReadTableTestsDictionary(Report.RConfig.table,General.TableType.Dev, Report.TestName);
            if (dictionaryFields==null)
            {
                TempData["message"] = "ERROR: cannot read table " + Report.RConfig.table + " results";
                return RedirectToAction("Index");
            }
            //New
            Report.addReportTests(dictionaryFields);
            HashSet<string> Tests = new HashSet<string>(dictionaryFields.Keys);
            
            //specific fix = empty test for some of Lev App stations
            Tests.Remove("");

            ViewBag.NGon.dictionaryFields = dictionaryFields;

            XMLReportHandler XMLer = new XMLReportHandler();
            List<string> Templates = XMLer.ParseTemplatesXML();

            ReportData RD = new ReportData(Report, Templates,Tests,dictionaryFields,new DBsearchSave());
            return View("EditReport",RD);
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult EditImageSlide(string _id, string ImageSlideID)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);

            //populate list of tests and tests-fields
            Dictionary<string, HashSet<string>> dictionaryFields = (new MDBTablesTestsDictionary()).ReadTableTestsDictionary(Report.RConfig.table, General.TableType.Dev, Report.TestName);
            if (dictionaryFields == null)
            {
                TempData["message"] = "ERROR: cannot read table " + Report.RConfig.table + " results";
                return RedirectToAction("Index");
            }
            Report.addReportTests(dictionaryFields);
            HashSet<string> Tests = new HashSet<string>(dictionaryFields.Keys);

            //specific fix = empty test for some of Lev App stations
            Tests.Remove("");

            ViewBag.NGon.dictionaryFields = dictionaryFields;

            XMLReportHandler XMLer = new XMLReportHandler();
            List<string> Templates = XMLer.ParseTemplatesXML();

            ReportSlideImageDetais RD = new ReportSlideImageDetais(Templates, Tests, dictionaryFields, Report.RConfig.GetSlideImageDetails(ImageSlideID), _id, ImageSlideID, Report.RConfig.GetImageSlide(ImageSlideID).templateKey, Report.RConfig.GetImageSlide(ImageSlideID).test, Report.RConfig.GetImageSlide(ImageSlideID).text);
            return View("EditImageSlide", RD);
        }

        public ActionResult DeleteReport(string _id)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            bool s = MDBReport.DeleteUserTableReport(_id);
            if (!s)
            {
                UserTableReport Report = MDBReport.ReadUserTableReport(_id);
                TempData["message"] = "Deletion of report " + Report.Name + " failed please try again later";
            }
            return RedirectToAction("MyReports");
        }

        //new

        public ActionResult RunPartialReport(string _id, string desc, string db, string[] debug)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);
            //verify current PRM search is for the requested report table
            DBsearch DBS = new DBsearch();
            if (string.IsNullOrEmpty(db)) db = "PRM";
            ViewBag.displayDB = db;
            if (db == "PRM")
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                }
            }
            if (db == "PRMArch")
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                }
            }
            if (db == "TMP")
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                }
            }
            if (db == "LST")
            {
                DBS = Report.Search.getNewDBS();
                if (DBS.db == DB_PRM) ViewBag.displayDB = "PRM";
                if (DBS.db == DB_PRM_Arch) ViewBag.displayDB = "PRMArch";
                if (DBS.db == DB_TMP) ViewBag.displayDB = "TMP";
            }
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            UserBGTask UBT = new MDBUserBGTask().ReadLastBGTasksBySourceID(username, _id);
            string FieldSlidesStr = UBT.jmap.GetValue("FieldSlidesStr", "").ToString();
            if (!string.IsNullOrEmpty(FieldSlidesStr)) ViewBag.FieldSlidesList = new List<string>(FieldSlidesStr.Split(','));
            else ViewBag.FieldSlidesList = new List<string>();
            ViewBag.db = db;
            ViewBag.Query = DBS.Query;
            ViewBag.col = DBS.col;
            

            return View("RunPartialReport", Report);
        }

        public ActionResult RunReportBG(string _id, string desc, string db, string[] debug, string Query)
        {
            //TODO complete: need a managment table: key,status, error (same col?) link when ready
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string FieldSlidesStr = "";

            //verify current PRM search is for the requested report table
            DBsearch DBS = new DBsearch();
            string displayDB = db;
            if (db == "PRM") 
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                }
            }
            if (db == "PRMArch")
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                }
            }
            if (db == "TMP")
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                }
            }

            foreach (string Pkey in Request.Form.AllKeys)
            {
                var queryValue = Request.Form[Pkey];
                if (Regex.IsMatch(Pkey, @"_cb\d*"))
                {
                    Match m = Regex.Match(Pkey, @"_cb(\d*)");
                    if (queryValue.StartsWith("true")) FieldSlidesStr += m.Groups[1].Value + ",";
                }

            }

            if (db == "LST")
            {
                DBS = Report.Search.getNewDBS();
                if (DBS.db == DB_PRM) displayDB = "PRM";
                if (DBS.db == DB_PRM_Arch) displayDB = "PRMArch";
                if (DBS.db == DB_TMP) displayDB = "TMP";
                if (string.IsNullOrEmpty(FieldSlidesStr))
                {
                    UserBGTask UBT = new MDBUserBGTask().ReadLastBGTasksBySourceID(username, _id);
                    FieldSlidesStr = UBT.jmap.GetValue("FieldSlidesStr", "").ToString();
                }
            }

            if (DBS.col != Report.RConfig.table)
            {
                TempData["message"] = "Error: current query table does not match requested report name-" + Report.Name + " table-" + Report.RConfig.table + "; navigate to Search PRM and execute search on report table, before running report";
                return RedirectToAction("MyReports");
            }

            //New: Report Launch validation - start
            List<BsonDocument> MissedResults = Report.LaunchValidate(DBS.Query, displayDB, FieldSlidesStr);
            if (MissedResults.Count > 0)
            {
                //TempData["message"] = "Error: the following slides have no results in the current query: ";
                //foreach (BsonDocument doc in MissedResults)
                //{
                //    TempData["message"] += "\"" + doc.GetValue("TestName", "").ToString() + "-" + doc.GetValue("Field", "") + "\" ; ";
                //}
                //return RedirectToAction("MyReports");
                //
                TempData["messageInfo"] = "Warning: Since some of the slides have no results in the current query, you must launch partial report; avialable slides are checked";
                List<string> FieldSlidesList = new List<string>();
                int Fcnt = 0;
                foreach (FieldSlide FS in Report.RConfig.FieldsSlides)
                {
                    //if (FS.type == General.ReportSlideType.limits.ToString())
                    //{
                        FieldSlidesList.Add(Fcnt.ToString());
                        Fcnt++;
                    //}
                }
                foreach (BsonDocument doc in MissedResults)
                {
                    if (FieldSlidesList.Contains(doc.GetValue("SlideNum", "").ToString())) FieldSlidesList.Remove(doc.GetValue("SlideNum", "").ToString());
                }
                ViewBag.FieldSlidesList = FieldSlidesList;
                ViewBag.db = db;
                ViewBag.Query = DBS.Query;
                ViewBag.col = DBS.col;
                return View("RunPartialReport", Report);
                //
            }
            //New: Report Launch validation - end
            Report.Search = new DBsearchSave(DBS);
            MDBReport.SaveUserTableReport(Report);

            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();

            string ExcelDebug = "N";
            if (debug != null) ExcelDebug = debug[0];

            BsonDocument jmap = new BsonDocument();
            jmap.Add("DBSquery", DBS.Query);
            jmap.Add("DBSdb", DBS.db);
            jmap.Add("DBScol", DBS.col);
            jmap.Add("DBSsortStr", DBS.SortStr);
            jmap.Add("DBSsortDirection", DBS.SortDirection);
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("debug", ExcelDebug);
            jmap.Add("type", "2");
            jmap.Add("FieldSlidesStr", FieldSlidesStr);

            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "report_" + Report.Name + " " + desc + " " + nowStr, Report._id, jmap));//unmark and comment from here

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "report_" + Report.Name + " " + desc + " " + nowStr, Report._id, jmap);
            //_task.status = "Report";

            //MDBUserBGTaskRec.SaveUserBGTask(_task);

            //JobWorker worker = new JobWorker();
            //worker.ExecRunReportBG(Report._id, DBS, nowStr, _task.id, ExcelDebug, FieldSlidesStr);

            return RedirectToAction("MyBGTasks","ManageDB");
        }

        public ActionResult RunCSRReportsBG(string ids_str, string Protocol, string proj, string phase, string table_div, string mails)
        {

            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string Query = "EQ;ProjectName;" + proj + ";EQ;Phase;" + phase + ";EQ;Indicator;Y";
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();

            BsonDocument jmap = new BsonDocument();

            jmap.Add("ids_str", ids_str);
            jmap.Add("Protocol", Protocol);
            jmap.Add("proj", proj);
            jmap.Add("phase", phase);
            jmap.Add("table_div", table_div);
            jmap.Add("mails", mails);
            jmap.Add("Query", Query);
            jmap.Add("username", username);
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("type", "10");

            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "CSR_reports_launch_" + Protocol + "_" + proj + "_" + phase + "_" + table_div + "_"+ nowStr, "", jmap));//unmark and comment from here

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "CSR_reports_launch_" + Protocol + "_" + proj + "_" + phase + "_" + table_div + "_" + nowStr, "", jmap);
            //_task.status = "Report";

            //MDBUserBGTaskRec.SaveUserBGTask(_task);

            //JobWorker worker = new JobWorker();
            //worker.ExecRunCSRReportsBG(nowStr,username,_task.id, ids_str,Protocol,proj,phase,table_div,mails,Query);

            return RedirectToAction("MyBGTasks", "ManageDB");
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult SaveReportChangeTab(string id, string col)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(id);
            Report.RConfig.table = col;
            MDBReport.SaveUserTableReport(Report);
            return RedirectToAction("EditReport", new RouteValueDictionary(new { _id = id ,fromCopy = false}));
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult SaveReportImageSlide(string id, string imageDetailsID, string template, string slideTest, string slideText, string TemplateChanged)
        {
            NameValueCollection nvc = Request.Form;
            string[] fields = Request.Form.GetValues("field");
            string[] queries = Request.Form.GetValues("query");
            string[] texts = Request.Form.GetValues("text");
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            
            UserTableReport Report = MDBReport.ReadUserTableReport(id);
            string message = "";
            if (!Report.updateImagesDetails(slideTest, slideText,imageDetailsID, template, fields, queries, texts, out message))
            {
                TempData["message"] = message;
                return RedirectToAction("EditReport", new RouteValueDictionary(new { _id = id, fromCopy = false }));
            }
            bool s = MDBReport.SaveUserTableReport(Report);
            if (!s)
            {
                TempData["message"] = "Save of report " + Report.Name + " failed please try again later";
            }
            if (string.IsNullOrWhiteSpace(TemplateChanged)) return RedirectToAction("EditReport", new RouteValueDictionary(new { _id = id, fromCopy = false }));
            else return RedirectToAction("EditImageSlide", new RouteValueDictionary(new { _id = id, ImageSlideID = imageDetailsID }));
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult SaveReport(string id, string FieldsSlidesStr, string TitleItemsStr, string ImageSlideID, IEnumerable<bool> useMarginLimits)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(id);
            string message = "";
            if (!Report.ParseSlidesStr(FieldsSlidesStr, TitleItemsStr, out message))
            {
                TempData["message"] = message;
                return RedirectToAction("EditReport", new RouteValueDictionary(new { _id = id, fromCopy = false }));
            }

            if (useMarginLimits != null && useMarginLimits.Count() == 2)
            {
                Report.useMarginLimits = true;
            }
            else
            {
                Report.useMarginLimits = false;
            }

            ////
            //Report.useMarginLimits = false;
            //var kuku = Request.Form.GetValues("useMarginLimits");
            //foreach (string Pkey in Request.Form.AllKeys)
            //{
            //    var queryValue = Request.Form[Pkey];
            //    if (Regex.IsMatch(Pkey, @"useMarginLimits"))
            //    {
            //        Match m = Regex.Match(Pkey, @"_cb(\d*)");
            //        if (queryValue.StartsWith("true")) Report.useMarginLimits = true;
            //    }

            //}
            //////
            //string _useMarginLimits = "N";
            //if (useMarginLimits != null) Report.useMarginLimits = useMarginLimits;
            //if (_useMarginLimits == "Y") Report.useMarginLimitsReport.useMarginLimits = true;
            //else Report.useMarginLimits = false;
            bool s = MDBReport.SaveUserTableReport(Report);
            if (!s)
            {
                TempData["message"] = "Save of report " + Report.Name + " failed please try again later";
            }
            if (string.IsNullOrWhiteSpace(ImageSlideID)) return RedirectToAction("EditReport", new RouteValueDictionary(new { _id = id, fromCopy = false }));
            else return RedirectToAction("EditImageSlide", new RouteValueDictionary(new { _id = id, ImageSlideID = ImageSlideID }));
        }

        [HttpPost]
        public ActionResult RenameReport(string _id, string name)
        {
            bool s;
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            s = MDBReport.RenameUserReport(_id, name);
            if (s) Response.Write(name);
            else Response.Write("Rename Failed:" + name);
            return null;
        }

        public ActionResult ValidateResCompReport(string _id, string db, string UnitId, string LanesStr, string FieldSlidesStr)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);
            if (string.IsNullOrEmpty(db)) db = "PRM";
            ViewBag.db = db;
            if (string.IsNullOrEmpty(LanesStr))  ViewBag.LanesStr = new List<string>();
            else ViewBag.LanesStr = new List<string>(LanesStr.Split(','));
            if (string.IsNullOrEmpty(FieldSlidesStr))  ViewBag.FieldSlidesStr = new List<string>();
            else ViewBag.FieldSlidesStr = new List<string>(FieldSlidesStr.Split(',')); 

            if(!string.IsNullOrEmpty(UnitId))
            {
                if (string.IsNullOrEmpty(LanesStr))
                {
                    TempData["message"]="Please check requested Lanes!!";
                    return View("ValidateResCompReport", Report);
                }
                if (string.IsNullOrEmpty(FieldSlidesStr))
                {
                    TempData["message"] = "Please check requested Fields!!";
                    return View("ValidateResCompReport", Report);
                }
                ViewBag.UnitID = UnitId;
                CompletenessValidator ReportCompV = new CompletenessValidator(UnitId,new List<string>(LanesStr.Split(',')));
                ViewBag.MissedResults = ReportCompV.Validate(_id, db, new List<string>(FieldSlidesStr.Split(',')));
            }

            return View("ValidateResCompReport", Report);
        }


        public ActionResult StatusReportExec(string _id, string Lanes, string JoinFieldKey, string db, string S1, string S2, string S3, string S4, string S5, string mesure)
        {

            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);
            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
            bool _JoinFieldKey = false;
            if (JoinFieldKey == "Y") _JoinFieldKey = true;

            //Temporay, TODO: read it from reference table \ report DB
            string detailed = "";
            if (System.IO.File.Exists(Path.Combine(Path.GetDirectoryName(EmptyExcel), Report.RConfig.table + "_SR.xml"))) detailed = "detailed";

            EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
            string newEmptyExcel = Path.Combine(Path.GetDirectoryName(EmptyExcel), Path.GetFileNameWithoutExtension(EmptyExcel).Replace("emptySR", "emptySR" + Lanes + detailed) + Path.GetExtension(EmptyExcel));
            EmptyExcel = newEmptyExcel;

            //verify current PRM search is for the requested report table
            //DBsearch PrmSearch = new DBsearch();
            DBsearch DBS = new DBsearch();
            if (db == "PRM")
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.getNewDBS();
                }
            }
            else
            {
                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.col != null)
                {
                    DBS = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                }
            }
            if (DBS.col != Report.RConfig.table)
            {
                TempData["message"] = "Error: current query table does not match requested report name-" + Report.Name + " table-" + Report.RConfig.table + "; navigate to Search TMP/PRM and execute search on report table, before running status report";
                return RedirectToAction("MyReports");
            }

            string filename = "ccd_statusReport_" + DBS.col + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(EmptyExcel, filepath, true);

            //TODO: Take from report sheets separators
            List<string> SeperatorFields = new List<string>();
            if (!string.IsNullOrWhiteSpace(S1)) SeperatorFields.Add(S1);
            if (!string.IsNullOrWhiteSpace(S2)) SeperatorFields.Add(S2);
            if (!string.IsNullOrWhiteSpace(S3)) SeperatorFields.Add(S3);
            if (!string.IsNullOrWhiteSpace(S4)) SeperatorFields.Add(S4);
            if (!string.IsNullOrWhiteSpace(S5)) SeperatorFields.Add(S5);
            //SeperatorFields.Add("BoardNumber");
            //SeperatorFields.Add("UnitId");
            Dictionary<string, List<BsonDocument>> dictionary = new Dictionary<string, List<BsonDocument>>();
            StatusReportHandler statusReportHandler = new StatusReportHandler();

            //new
            DBS.Skip = 0;
            DBS.Sort = new SortByBuilder();
            DBS.FieldsStr = "";

            string msg = "";
            db_tmp.genericFind(DBS, 0);
            bool success = statusReportHandler.StatusReportExec(_id, filepath,DBS.ResCursor,DBS.Query,Lanes, SeperatorFields,_JoinFieldKey, !string.IsNullOrEmpty(mesure), out msg);
            if (success)
            {
                TempData["messageInfo"] = "Status report for query:'" + DBS.Query + "' was exported to Excel ; Click the following to get Custom Execution input file:";
                //TODO: export file
                TempData["ExportedFileNAme"] = filepath;
                TempData["ExportedFileNAme2"] = filepath.Replace("xlsx", "txt");
                //return RedirectToAction("Index", "ManageDB"); OR load file in "MyReports"
            }
            else TempData["message"] = msg + DBS.Query;

            return RedirectToAction("MyReports");
        }

        public ActionResult SendReportToUser(string ReportId, string UserId)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport SourceReport = MDBReport.ReadUserTableReport(ReportId);
            if ((SourceReport == null) || (String.IsNullOrWhiteSpace(SourceReport.Name)))
            {
                TempData["message"] = "Error: Can not find Report:" + ReportId;
                return RedirectToAction("MyReports");
            }
            string TargetName = SourceReport.Name + "_From_" + Regex.Replace(SourceReport._id, @"\d*", "") + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            UserTableReport TargetReport = new UserTableReport(UserId, TargetName, SourceReport);

            if (!MDBReport.SaveUserTableReport(TargetReport))
            {
                TempData["message"] = "Error: Can not copy Report:" + SourceReport.Name;
            }
            else
            {
                TempData["message"] = "Report:" + SourceReport.Name + " was copied succesfully to user:" + UserId;
            }
            return RedirectToAction("MyReports");
        }

     }
}