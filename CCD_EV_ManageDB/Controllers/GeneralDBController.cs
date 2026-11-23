using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq; 
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
//using System.Collections.Specialized;
using CCD_EV_ManageDB.Models;
using CCD_EV_ManageDB.Repositories;
using CCD_EV_ManageDB.ActionFilters;
using CCD_EV_ManageDB.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Web.Security;
using System.Threading;
using System.Threading.Tasks;

namespace CCD_EV_ManageDB.Controllers
{

    [LogActionFilter]

    public class GeneralDBController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ManageDBController));
        private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }

        private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        private readonly int DataTableLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DataTableLimit"]);

        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }

        private MongoDBClient db_tmp = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);
        private MongoDBClient db_prm = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);
        private List<string> lop = new List<string>(new string[] { "EQ", "Matches", "NotMatches", "In", "NotIn", "ExistNotEmpty", "GT", "LT" });

        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }
        public string[] MongoTables { get { return System.Configuration.ConfigurationManager.AppSettings["MongoTables"].Split(','); } }
        public string[] CompTables { get { return System.Configuration.ConfigurationManager.AppSettings["CertificationTables"].Split(','); } }
        public string[] ProjectsList { get { return System.Configuration.ConfigurationManager.AppSettings["ProjectsList"].Split(','); } }
        public string[] PhasesList { get { return System.Configuration.ConfigurationManager.AppSettings["PhasesList"].Split(','); } }
        public string[] ProtocolsList { get { return System.Configuration.ConfigurationManager.AppSettings["Protocols"].Split(','); } }


        //
        // GET: /AdminDB/

        public ActionResult Index()
        {
            ViewBag.collist = new List<string>(MongoTables);
            return View();
        }

        public ActionResult ViewProjectsList()
        {

            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.projlist = new List<string>(ProjectsList);


            return View();
        }

        [Authorize(Roles = "CCD_EV_ADM")]
        public ActionResult ProjectListAction(string action, string col, string proj)
        {
            if (action == "archive")
            {
                if (!(new MDBStationResults("DB_PRM", col)).MoveResToPRMArch(proj))
                {
                    TempData["message"] = "An error occured while archiving table:" + col + " of project:" + proj;
                }
                else
                {
                    TempData["message"] = "Table:" + col + " of project:" + proj + " is now archived";
                }
            }
            if (action == "dearchive")
            {
                if (!(new MDBStationResults("DB_PRM_Arch", col)).MoveResFromPRMArch(proj))
                {
                    TempData["message"] = "An error occured while de-archiving table:" + col + " of project:" + proj;
                }
                else
                {
                    TempData["message"] = "Table:" + col + " of project:" + proj + " is now back in PRM DB";
                }
            }
            
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.projlist = new List<string>(ProjectsList);
            return View("ViewProjectsList");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult ADMRefTables()
        {

            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.Users = ((new MDBUsers()).ReadAllUsers());
            ViewBag.Users.Sort();
            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();

            MongoCursor Cur = MDBRef.ReadCursorAllADMReferenceTables();

            Dictionary<string, HashSet<string>> AllUsersTableReports = (new MDBUserTableReport()).GetDictionaryAllUsersTableReports();
            if (AllUsersTableReports == null)
            {
                TempData["message"] = "ERROR: cannot read table UserTableRport";
            }

            ViewBag.NGon.DictionaryAllUsersTableReports = AllUsersTableReports;
            ViewBag.UsersTablesReports = AllUsersTableReports.Keys;

            return View("ADMRefTables", Cur);
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult CreateRefTable(HttpPostedFileBase file, string _Protocol, string _PVersion)
        {
            //New: Excel file handling - Start
            string fileName = "";
            string path = "";
            if ((file != null) && (file.ContentLength > 0))
            {
                // extract only the fielname
                fileName = Path.GetFileName(file.FileName);
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
                    return RedirectToAction("ADMRefTables");
                }
            }
            if ((string.IsNullOrWhiteSpace(_Protocol)) || (string.IsNullOrWhiteSpace(_PVersion)))
            {
                TempData["message"] = "Please fill: Protocol & Version";
                return RedirectToAction("ADMRefTables");
            }

            ADMReferenceTable Ref = new ADMReferenceTable(_Protocol, _PVersion);
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (!Ref.LoadExcelLimits(path))
                {
                    TempData["message"] = "Error: cannot load Excel file properly";
                    return RedirectToAction("ADMRefTables");
                }
            }
            else Ref.RConfig.FieldsSlides.Add(new FieldSlide(General.ReportSlideType.limits.ToString(),"initial", "initial","", "", "", "",null,"",""));
            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();
            MDBRef.SaveADMReferenceTable(Ref);

            return RedirectToAction("ADMRefTables");
        }

        public ActionResult CopyReportToUser(string UserTable, string ReportName, string UserId)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            string userName = Regex.Replace(UserTable, "_.*", "");
            string tableName = Regex.Replace(UserTable, userName + "_", "");
            UserTableReport SourceReport = MDBReport.ReadUserTableReport(userName, tableName, ReportName);
            if ((SourceReport == null) || (String.IsNullOrWhiteSpace(SourceReport.Name)))
            {
                TempData["message"] = "Error: Can not find Report:" + UserTable + " " + ReportName;
                return RedirectToAction("ADMRefTables");
            }
            string TargetName = SourceReport.Name + "_From_" + Regex.Replace(SourceReport._id, @"\d*", "") + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            UserTableReport TargetReport = new UserTableReport(UserId, TargetName, SourceReport);

            if (!MDBReport.SaveUserTableReport(TargetReport))
            {
                TempData["message"] = "Error: Can not copy Report:" + UserTable + " " + ReportName;
            }
            else
            {
                TempData["messageInfo"] = "Report:" + UserTable + " " + ReportName +" was copied succesfully to user:" + UserId;
            }
            return RedirectToAction("CopyReportOfUser", "AdminDB");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult CreateRefTableFromReport(string _Protocol, string _PVersion,string UserTable, string ReportName, string ReportId)
        {
            ViewBag.collist = new List<string>(MongoTables);
            if ((string.IsNullOrWhiteSpace(_Protocol)) || (string.IsNullOrWhiteSpace(_PVersion)))
            {
                TempData["message"] = "Please fill: Protocol & Version";
                return RedirectToAction("ADMRefTables");
            }

            //UserTableReport report = (new MDBUserTableReport()).ReadUserTableReport(ReportId);
            string userName = Regex.Replace(UserTable,"_.*","");
            string tableName = Regex.Replace(UserTable,userName+"_","");
            UserTableReport report = (new MDBUserTableReport()).ReadUserTableReport(userName, tableName, ReportName);
            if ((report == null) || (String.IsNullOrWhiteSpace(report.Name)))
            {
                TempData["message"] = "Error: Can not find Report ID:" + ReportId;
                return RedirectToAction("ADMRefTables");
            }

            ADMReferenceTable Ref = new ADMReferenceTable(_Protocol, _PVersion);
            //Ref.RConfig.FieldsSlides.Add(new FieldSlide("initial", "initial", "", "", ""));
            Ref.RConfig = report.RConfig;
            //exclude image slides, take limits slides only
            List<FieldSlide> tmpList = new List<FieldSlide>();
            foreach (FieldSlide FS in Ref.RConfig.FieldsSlides)
            {
                if (FS.type==General.ReportSlideType.limits.ToString()) tmpList.Add(FS);
            }
            Ref.RConfig.FieldsSlides = tmpList;
            Ref.RConfig.ImagesDetails.Clear();
            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();
            MDBRef.SaveADMReferenceTable(Ref);

            return RedirectToAction("ADMRefTables");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult EditRefTable(string _id, bool fromCopy)
        {
            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();
            ADMReferenceTable Ref= MDBRef.ReadADMReferenceTable(_id);
            if (fromCopy) ViewBag.collist = new List<string>(MongoTables);
            else ViewBag.collist = null;

            return View("EditRefTable", Ref);
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult ExportExcelRefTable(string _id)
        {
            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();
            ADMReferenceTable Ref = MDBRef.ReadADMReferenceTable(_id);

                return View("EditRefTable", Ref);
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult DeleteRef(string _id)
        {
            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();
            bool s = MDBRef.DeleteADMReferenceTable(_id);
            if (!s)
            {
                ADMReferenceTable Ref = MDBRef.ReadADMReferenceTable(_id);
                TempData["message"] = "Deletion of report " + Ref._id + "failed please try again later";
            }
            return RedirectToAction("ADMRefTables");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult SaveRef(string id,string _Protocol, string _PVersion, string FieldsSlidesStr)
        {
            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();
            ADMReferenceTable Ref = MDBRef.ReadADMReferenceTable(id);
            Ref.RConfig.FieldsSlides.Clear();

            String[] FSarr = FieldsSlidesStr.Split(';');
            for (int i = 0; i < FSarr.Length; i += 7)
            {
                //TODO: client validation
                if (!String.IsNullOrEmpty(FSarr[i + 3]))
                    try
                    {
                        Convert.ToDouble(FSarr[i + 3]);
                    }
                    catch (Exception ex)
                    {
                        TempData["message"] = "Save of report " + Ref._id + " failed, limits must be numeric or empty in slide:" + FSarr[i];
                        return RedirectToAction("EditRefTable", new RouteValueDictionary(new { _id = id, fromCopy = false }));
                    }
                if (!String.IsNullOrEmpty(FSarr[i + 2]))
                    try
                    {
                        Convert.ToDouble(FSarr[i + 2]);
                    }
                    catch (Exception ex)
                    {
                        TempData["message"] = "Save of report " + Ref._id + " failed, limits must be numeric or empty in slide:" + FSarr[i];
                        return RedirectToAction("EditRefTable", new RouteValueDictionary(new { _id = id, fromCopy = false }));
                    }
                if ((!String.IsNullOrEmpty(FSarr[i + 2])) && (!String.IsNullOrEmpty(FSarr[i + 3])) &&
                    (Convert.ToDouble(FSarr[i + 3])) < (Convert.ToDouble(FSarr[i + 2])))
                {
                    TempData["message"] = "Save of report " + Ref._id + " failed, upper limit must be greater than lower in slide:" + FSarr[i];
                    return RedirectToAction("EditRefTable", new RouteValueDictionary(new { _id = id, fromCopy = false }));
                }

                Ref.RConfig.FieldsSlides.Add(new FieldSlide(General.ReportSlideType.limits.ToString(), "", FSarr[i], FSarr[i + 1], "", FSarr[i + 2], FSarr[i + 3], null, FSarr[i + 4], FSarr[i + 5].Replace("||", ";"), FSarr[i + 6]));
            }

            bool s = MDBRef.SaveADMReferenceTable(Ref);
            if (!s)
            {
                TempData["message"] = "Save of report " + Ref._id + " failed please try again later";
            }
            return RedirectToAction("EditRefTable", new RouteValueDictionary(new { _id = id, fromCopy = false }));
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult CopyRef(string _id,string _Protocol, string _PVersion)
        {
            if ((string.IsNullOrWhiteSpace(_Protocol)) || (string.IsNullOrWhiteSpace(_PVersion)))
            {
                TempData["message"] = "Please fill: Protocol & Version";
                return RedirectToAction("ADMRefTables");
            }

            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();
            ADMReferenceTable Ref = MDBRef.ReadADMReferenceTable(_id);
            string c_id = MDBRef.CopyAndSaveADMReferenceTable(_Protocol,_PVersion,Ref);
            if (c_id == null)
            {
                TempData["message"] = "ERROR: cannot copy ref";
                 return RedirectToAction("ADMRefTables");
            }

            return RedirectToAction("EditRefTable", new RouteValueDictionary(new { _id = c_id, fromCopy = true }));
        }

        //Indicators
        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult IndicatorsList(string proj, string phase)
        {
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            List<Indicator> IndicatorsList = new MDBIndicator().ReadProjectPhaseIndicators(proj, phase);
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);


            if (Roles.IsUserInRole("CCD_EV_ADM"))
            {
                List<string> speclist = (new MDBADMReferenceTable()).ReadListAllADMReferenceTablesIDS();
                speclist.Insert(0, "");
                DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
                DateTime date1 = DateTime.Now;
                Calendar cal = dfi.Calendar;
                int curWeek = cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
                Dictionary<string, string> weeks = new Dictionary<string, string>();
                for (int weekCnt = -10; weekCnt < 20; weekCnt++)
                {
                    weeks.Add(cal.GetWeekOfYear(date1.AddDays(Convert.ToDouble(weekCnt * 7)), dfi.CalendarWeekRule, dfi.FirstDayOfWeek).ToString(), date1.AddDays(Convert.ToDouble(weekCnt * 7)).ToString("dd/MM/yyyy"));
                }
                ViewBag.workweeks = weeks;
                List<string> weekNumbers = new List<string>(new string[] { "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" });
                ViewBag.weekNumbers = weekNumbers;
                //if (IndicatorsList.Count > 0)
                //{
                //    date1 = CCDUtilities.String2Date(IndicatorsList[0].wws);
                //    weeks.Clear();
                //    weeks.Add((cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek)).ToString(), date1.ToString("dd/MM/yyyy"));
                //    weekNumbers.Clear();
                //    weekNumbers.Add(IndicatorsList[0].wn);

                //    ViewBag.weekNumbers = weekNumbers;
                //    ViewBag.workweeks = weeks;
                //}
                return View("IndicatorsListADM", IndicatorsList);
            }
            return View(IndicatorsList);
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult IndicatorsCopyAllProjPhase(string s_proj, string s_phase, string t_proj, string t_phase, string wws, string wn)
        {
            string[] dateArr = wws.Split('/');

            if (dateArr.Length == 3)
            {
                DateTime date1 = new DateTime(Convert.ToInt32(dateArr[2]), Convert.ToInt32(dateArr[1]), Convert.ToInt32(dateArr[0]));
                wws = date1.ToString("yyyyMMdd");
            }
            else
            {
                TempData["messageInfo"] = "Error:work week date is not in correct format";
                return RedirectToAction("IndicatorsList", new RouteValueDictionary(new { proj = t_proj, phase = t_phase }));
            }

            if ((s_proj == t_proj) && (s_phase == t_phase))
            {
                TempData["messageInfo"] = "Cannot copy Indicatos to same project phase";
                return RedirectToAction("IndicatorsList", new RouteValueDictionary(new { proj = t_proj, phase = t_phase }));
            }
            MDBIndicator MDBIndicatorObj = new MDBIndicator();
            List<Indicator> IndicatorsList = MDBIndicatorObj.ReadProjectPhaseIndicators(s_proj, s_phase);
            foreach (Indicator Ind in IndicatorsList)
            {
                Indicator t_Ind = new Indicator(Ind.table, t_proj, t_phase, (new MDBADMReferenceTable().ReadADMReferenceTable(Ind.spec)), wws, wn, Ind.lanes);
                MDBIndicatorObj.SaveIndicator(t_Ind);
            }
            return RedirectToAction("IndicatorsList", new RouteValueDictionary(new { proj = t_proj, phase = t_phase }));
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult IndicatorsListAction(string action, string col, string proj, string phase, string spec, string wws, string wn, string lanes)
        {
            MDBIndicator mdbInd = new MDBIndicator();
            
            if (action == "initInd")
            {
                string[] dateArr = wws.Split('/');
                
                if (dateArr.Length == 3)
                {
                    DateTime date1 = new DateTime(Convert.ToInt32(dateArr[2]), Convert.ToInt32(dateArr[1]), Convert.ToInt32(dateArr[0]));
                    wws = date1.ToString("yyyyMMdd");
                }
                else
                {
                    TempData["messageInfo"] = "Error:work week date is not in correct format";
                    return View("IndicatorsList");
                }

                Indicator indNew = new Indicator(col, proj, phase, new MDBADMReferenceTable().ReadADMReferenceTable(spec),wws, wn, lanes);
                mdbInd.SaveIndicator(indNew);
                return RedirectToAction("IndicatorEdit", new RouteValueDictionary(new { _id = indNew._id }));

            }
            //Depricated
            //if (action=="editInd")
            //{
            //    Indicator ind = mdbInd.ReadIndicator(col, proj, phase);
            //    if (string.IsNullOrEmpty(ind._id))
            //    {
            //        return RedirectToAction("IndicatorInit", new RouteValueDictionary(new { col = col, proj = proj, phase = phase }));
            //    }
            //    return RedirectToAction("IndicatorEdit", new RouteValueDictionary(new { _id = ind._id }));
            //}
            //Depricated
            //if (action == "launchInd")
            //{
            //    Indicator Ind = new MDBIndicator().ReadIndicator(col,proj,phase);
            //    //string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptyExcel"];
            //    string EmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorTemplate.xlsx");
            //    string filename = "Indicator_" + Ind._id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            //    string filepath = Path.Combine(ServerDir, "tmp", filename);

            //    System.IO.File.Copy(EmptyExcel, filepath, true);
            //    IndicatorDisplayResult DispResult = new IndicatorDisplayResult();
            //    if (!string.IsNullOrEmpty(Ind._id)) DispResult = new IndicatorHandler().LaunchIndicator(Ind._id);
            //    (new ExcelHandler()).writeIndicatorSheet(filepath, "Results", DispResult);
                
            //    ////Add chart:
            //    //filepath = Path.Combine(ServerDir, "EVData", "IndicatorTemplate.xlsx");
            //    //(new ExcelChartsHandler()).writeIndicatorSheet(filepath, "Graph", DispResult);
                
            //    return View("IndicatorResult", DispResult);
            //}

            //ViewBag.collist = new List<string>(MongoTables);
            //ViewBag.projlist = new List<string>(ProjectsList);
            //ViewBag.phaselist = new List<string>(PhasesList);
            //return View("IndicatorsList");
            return RedirectToAction("IndicatorsList", new RouteValueDictionary(new { proj = proj, phase = phase }));
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult IndicatorsLaunchAll(string proj, string phase)
        {
            IndicatorDisplayResult DispResult = new IndicatorDisplayResult();
            IndicatorHandler IndHAndler = new IndicatorHandler();
            ExcelHandler ExclHandler = new ExcelHandler();

            List <Indicator> IndicatorsList = new MDBIndicator().ReadProjectPhaseIndicators(proj, phase);
            string EmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorsAllTemplateNew_" + proj + "_" + phase +".xlsx");
            if (!System.IO.File.Exists(EmptyExcel)) EmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorsAllTemplateNew.xlsx");
            string filename = "Indicators_Official_" + proj + "_" + phase + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(EmptyExcel, filepath, true);

            ExclHandler.PrepareIndicatorsAllFile(filepath, DispResult);

            foreach (Indicator ind in IndicatorsList)
            {
                DispResult = IndHAndler.LaunchIndicator(ind._id,true);
                ExclHandler.writeIndicatorSheet(filepath, "Results", DispResult);
            }

            //Switch WW53 to WW1
            //ExclHandler.replaceCellConstantInSheet(filepath, "EV TEST", "ww 53", "ww 1");

            if (System.IO.File.Exists(filepath)) return new FilePathResult(filepath, "application/vnd.ms-excel") { FileDownloadName = Path.GetFileName(filename) };
            else
            {
                TempData["message"] = "ERROR: cannot find file " + filename;
            }

            return RedirectToAction("IndicatorsList", new RouteValueDictionary(new { proj = proj, phase = phase }));
        }

        //
        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult IndicatorsLaunchAllBG(string proj, string phase, string _interface)
        {
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();

            BsonDocument jmap = new BsonDocument();
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("type", "5");
            jmap.Add("proj", proj);
            jmap.Add("phase", phase);
            jmap.Add("_interface", _interface);
            string sender = System.Configuration.ConfigurationManager.AppSettings["mailSender"];
            if ((_interface=="") || (_interface=="ALL")) jmap.Add("mails", sender);


            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "Indicators_" + proj + "_" + phase + "_" + nowStr, null, jmap));//unmark and comment from here

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "Indicators_" + proj + "_" + phase + "_" + nowStr, null, jmap);
            //_task.status = "Report";

            //MDBUserBGTaskRec.SaveUserBGTask(_task);

            //JobWorker worker = new JobWorker();
            //worker.RunAllIndicators(nowStr, _task.id, proj, phase,_interface, "naty.shemer@intel.com");
            return RedirectToAction("MyBGTasks", "ManageDB");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult IndicatorsCreateSchedule(string proj, string phase, string frequency, string day, string hour, string mails)
        {
            //add user as parameter
            //add mail addresses
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBBGTaskSchedule MDBBGTS = new MDBBGTaskSchedule();
            BGTaskSchedule BGTS = new BGTaskSchedule(username + "_indicator_" + proj + "_" + phase, "Indicator_" + proj + "_" + phase);
            BGTS.frequency = frequency;
            BGTS.day = day;
            BGTS.hour = hour;
            BGTS.user = username;
            
            BGTS.UpdateJmap("", "", "5",proj, phase,mails);
            if (MDBBGTS.SaveBGTaskSchedule(BGTS)) TempData["messageInfo"] = "Scedule has been created";
            else TempData["message"] = "Error:could not create schedule";
            return RedirectToAction("JobsSchedulerADM", new RouteValueDictionary(new { proj = proj, phase = phase }));
            //JobsSchedulerADM
        }
        //

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult IndicatorInit(string col, string proj, string phase)
        {
            Indicator ind = (new MDBIndicator()).ReadIndicator(col, proj, phase);
            if (!string.IsNullOrEmpty(ind._id))
            {
                return RedirectToAction("IndicatorEdit", new RouteValueDictionary(new { _id = ind._id }));
            }
            
            List<string> speclist = (new MDBADMReferenceTable()).ReadListAllADMReferenceTablesIDS();
            speclist.Insert(0, "");
            ViewBag.col = col;
            ViewBag.proj = proj;
            ViewBag.phase = phase;
            ViewBag.speclist = speclist;
            //TODO: real ww list, List<string> ww = new List<string> (new string[] {"01","02"});
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = DateTime.Now;
            Calendar cal = dfi.Calendar;
            int curWeek = cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            Dictionary<string, string> weeks = new Dictionary<string, string>();
            for (int weekCnt = -10; weekCnt < 20; weekCnt++)
            {
                weeks.Add(cal.GetWeekOfYear(date1.AddDays(Convert.ToDouble(weekCnt * 7)), dfi.CalendarWeekRule, dfi.FirstDayOfWeek).ToString(), date1.AddDays(Convert.ToDouble(weekCnt * 7)).ToString("dd/MM/yyyy"));
            }
            ViewBag.workweeks = weeks;
            //New
            //                <option value="10">10</option>
            //    <option value="11">11</option>
            List <string> weekNumbers =  new List<string>(new string[]{"10","11","12","13","14","15","16","17","18","19","20"});
            ViewBag.weekNumbers = weekNumbers;
            List<Indicator> ProjectIndicators = (new MDBIndicator()).ReadProjectPhaseIndicators(proj, phase);
            if (ProjectIndicators.Count > 0)
            {
                date1 = CCDUtilities.String2Date(ProjectIndicators[0].wws);
                weeks.Clear();
                weeks.Add((cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek)).ToString(), date1.ToString("dd/MM/yyyy"));
                weekNumbers.Clear();
                weekNumbers.Add(ProjectIndicators[0].wn);

                ViewBag.weekNumbers = weekNumbers;
                ViewBag.workweeks = weeks;
            }
            return View();
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult IndicatorEdit(string _id)
        {
            Indicator ind = new MDBIndicator().ReadIndicator(_id);
            if (ind.UpdateIndicatorSpec((new MDBADMReferenceTable()).ReadADMReferenceTable(ind.spec))) new MDBIndicator().SaveIndicator(ind);
            List<string> speclist = (new MDBADMReferenceTable()).ReadListAllADMReferenceTablesIDS();
            speclist.Insert(0, "");
            ViewBag.speclist = speclist;
            return View("IndicatorEdit", ind);
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult IndicatorDelete(string _id)
        {
            Indicator ind = new MDBIndicator().ReadIndicator(_id);
            bool s = new MDBIndicator().DeleteIndicator(_id);
            return RedirectToAction("IndicatorsList", new RouteValueDictionary(new { proj = ind.project, phase = ind.phase }));
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult IndicatorLaunch(string _id,string official)
        {
            Indicator Ind = new MDBIndicator().ReadIndicator(_id);
            //string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptyExcel"];
            string EmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorSingleTemplate.xlsx");
            string filename = "Indicator_" + Ind._id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);

            System.IO.File.Copy(EmptyExcel, filepath, true);
            IndicatorDisplayResult DispResult = new IndicatorDisplayResult();
            if (!string.IsNullOrEmpty(Ind._id)) DispResult = new IndicatorHandler().LaunchIndicator(Ind._id, (official=="Y"));

            (new ExcelHandler()).writeIndicatorSheet(filepath, "Results", DispResult);
            (new ExcelHandler()).writeSingleIndicatorDetailsSheet(filepath, "Results", DispResult);

            ////Add chart:
            //filepath = Path.Combine(ServerDir, "EVData", "IndicatorTemplate.xlsx");
            //(new ExcelChartsHandler()).writeIndicatorSheet(filepath, "Graph", DispResult);

            ////Return Excel file:
            //if (System.IO.File.Exists(filepath)) return new FilePathResult(filepath, "application/vnd.ms-excel") { FileDownloadName = Path.GetFileName(filename) };
            //else
            //{
            //    TempData["message"] = "ERROR: cannot find file " + filename;
            //}


            //
            ViewBag.filename = filename;
            ViewBag.official = "Official";
            if (official == "N") ViewBag.official = "Accumulative";
            return View("IndicatorResult", DispResult);
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        [CheckPermissionFilter]
        public ActionResult IndicatorEditExec(string _id, string spec, string proj, string phase)
        {
            MDBIndicator mdbIndicator = new MDBIndicator();
            Indicator Ind = mdbIndicator.ReadIndicator(_id);
            Ind.spec = spec;

            foreach (string key in Request.Form.AllKeys)
            {
                var queryValue = Request.Form[key];
                if (Regex.IsMatch(key, @"^u\d*"))
                {
                    Ind.tests[Convert.ToInt32(key.Substring(1))].UnitsNum = Convert.ToInt32(queryValue);
                }
                if (Regex.IsMatch(key, @"^c\d+"))
                {
                    Ind.tests[Convert.ToInt32(key.Substring(1))].Comment = queryValue;
                }
                if (Regex.IsMatch(key, @"w\d*_\d*"))
                {
                    Match m = Regex.Match(key, @"w(\d*)_(\d*)");
                    Ind.tests[Convert.ToInt32(m.Groups[1].Value)].WorkWeeks[Convert.ToInt32(m.Groups[2].Value)].PlannedOrig = Convert.ToInt32(queryValue);
                }
                //New Blocked
                if (Regex.IsMatch(key, @"wb\d*_\d*"))
                {
                    Match m = Regex.Match(key, @"wb(\d*)_(\d*)");
                    Ind.tests[Convert.ToInt32(m.Groups[1].Value)].WorkWeeks[Convert.ToInt32(m.Groups[2].Value)].Blocked = queryValue.StartsWith("true");
                }

            }

            if (mdbIndicator.SaveIndicator(Ind)) TempData["messageInfo"] = "Indicator " + _id + " was saved";
            else TempData["message"] = "Error: could not save Indicator " + _id ;
            return RedirectToAction("IndicatorsList", new RouteValueDictionary(new { proj = proj, phase = phase }));

        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult IndicatorsAddWeeksAll(string proj, string phase, int wn)
        {
            MDBIndicator DBInd = new MDBIndicator();
            List<Indicator> IndicatorsList = new MDBIndicator().ReadProjectPhaseIndicators(proj, phase);


            foreach (Indicator ind in IndicatorsList)
            {
                if (ind.AddWeeks(wn)) DBInd.SaveIndicator(ind);
            }

            return RedirectToAction("IndicatorsList", new RouteValueDictionary(new { proj = proj, phase = phase }));
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult IndicatorsLog(DBsearch obj)
        {
            //table lines are commented, maybe we want to add built in table filter later...
            //string table;

            if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
            {
                obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
                obj.Query = "Matches;_id;/^./";
                obj.SortStr = "date";
                obj.SortDirection = "DES";
            }
            else
            {
                obj.db = DB_PRM;
                obj.col = DefaultCol;
                obj.SortStr = "date";
                obj.SortDirection = "DES";
            }

            ViewBag.q0list = lop;

            if (string.IsNullOrEmpty(obj.Query)) obj.Query = "Matches;_id;/^./";
            DBsearch DBS = new DBsearch();
            //table = obj.col;
            DBS.col = "IndicatorsUserLog";
            DBS.db = obj.db;
            //table = table.Replace("+", @"\+");
            //DBS.Query = "Matches;_id;/" + table + "/;";
            //DBS.Query += obj.Query;
            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.FieldsStr = obj.FieldsStr;
            DBS.FieldsIncExc = obj.FieldsIncExc;
            if (obj.FieldsIncExc == "Exc")
            {
                DBS.FieldsExc = DBS.FieldsExc = true;
                DBS.FieldsInc = false;
            }
            //DBS.AddCoreFields();
            if (!String.IsNullOrEmpty(obj.SortStr)) TempData["JumpToBottom"] = "yes";
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "_id";
            DBS.Skip = obj.Skip;
            DBS.jumpBottom = obj.jumpBottom;
            //(new MongoDBClient(dbAdress, DB_TMP, DefaultCol)).genericFind(DBS, DataTableLimit);
            DBS.ResCursor = (new MDBStationResults(DB_PRM,"IndicatorsUserLog")).MDBFind(DBS.Query,"AND",DBS.SortDirection,DBS.SortStr);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            foreach (string field in DBS.Fields) DBS.TableFields.Add(field);
            DBS.TableFields = new HashSet<string>(new string[] { "_id", "table", "ProjectName", "Phase", "user", "Action", "comment", "date", "resultsNum", "query" });
            //DBS.Fields = new HashSet<string>(new string[] { "_id", "table", "query", "user", "comment", "Action", "date", "resultsNum", "ProjectName", "Phase" });
            String[] qarr = DBS.Query.Split(';');
            for (int i = 0; i < qarr.Length; i += 3)
            {
                DBS.QueryTriplets.Add((qarr[i]) + ";" + (qarr[i + 1]) + ";" + (qarr[i + 2]));
            }
            //DBS.col = table;
            DBS.Query = obj.Query;
            //DBS.QueryTriplets.RemoveAt(0);
            return View("IndicatorsLog", DBS);

        }


        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult loadIndicatorPlanning(string _id, HttpPostedFileBase file)
        {
            string message = "";
            string fileName = "";
            string path = "";
            if ((file != null) && (file.ContentLength > 0))
            {
                fileName = Path.GetFileName(file.FileName);
                path = Path.Combine(ServerDir, "uploads", fileName);
                try
                {
                    file.SaveAs(path);
                }
                catch (Exception e)
                {
                    TempData["message"] = "Error: cannot save file " + e.Message;
                    return RedirectToAction("IndicatorEdit", new RouteValueDictionary(new { _id = _id }));
                }
            }
            else
            {
                TempData["message"] = "Error: you must choose a file to upload";
                return RedirectToAction("IndicatorEdit", new RouteValueDictionary(new { _id = _id }));
            }

            MDBIndicator mdbIndicator = new MDBIndicator();
            Indicator Ind = mdbIndicator.ReadIndicator(_id);
            Indicator IndFromFile = (new ExcelHandler()).readSingleIndicatorDetailsSheet(path, Ind, out message);
            if (!string.IsNullOrEmpty(message))
            {
                TempData["message"] = "Error while loading Excel:" + message;
                return RedirectToAction("IndicatorEdit", new RouteValueDictionary(new { _id = _id }));
            }
            if (mdbIndicator.SaveIndicator(IndFromFile)) TempData["messageInfo"] = "Indicator " + _id + " was saved";
            else TempData["message"] = "Error: could not save Indicator " + _id;
            return RedirectToAction("IndicatorEdit", new RouteValueDictionary(new { _id = _id }));

        }
        //JobsSchedulerADM
        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult JobsSchedulerADM(string proj, string phase)
        {
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            List<Indicator> IndicatorsList = new MDBIndicator().ReadProjectPhaseIndicators(proj, phase);
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);

            return View();
        }

        //TestJobsScheduler
        //maybe test specific job? when I have more than once...
        public ActionResult TestJobsScheduler()
        {
            JobScheduler scheduler = new JobScheduler();
            scheduler.Execute();
            TempData["messageInfo"] = "Scheduler executed";
            return RedirectToAction("JobsSchedulerADM");
        }

      
        //SessionsManage tool
        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult SessionsManageList(string protocol,string proj, string phase, string FilterSessions, string _UnifyNVM)
        {

            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastSessionManageProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastSessionManagePhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastSessionManageProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = "";
            if (string.IsNullOrEmpty(FilterSessions)) FilterSessions = "Valid_Pending";
            //SessionsManageDisplay SMDisp = new SessionManageHandler().GetSessionsManageDisplay(protocol, proj, phase,FilterSessions);
            SessionManageHandler SM = new SessionManageHandler();
            SessionsManageDisplay SMDisp = SM.GetSessionsManageDisplay(protocol, proj, phase, FilterSessions);
            if (_UnifyNVM == "Y")
            {
                SM.UnifyNVMSessions(SMDisp);
                ViewBag.UnifyNVM = "Y";
                ViewBag.NVMrequest = "N";
            }
            else
            {
                ViewBag.UnifyNVM = "N";
                ViewBag.NVMrequest = "Y";
            }
            if (SMDisp.Conf.tables.Count == 0) TempData["messageInfo"] = "No configuration is defined for: "+ protocol + " " + proj + " " + phase;
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManagePhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProtocol = protocol;


            //New permission check start:
            List<DBpermission> permissions = new List<DBpermission>();
            MDBUsers MDBUsersObj = new MDBUsers();
            if (User.Identity.IsAuthenticated) permissions = (MDBUsersObj.getPermissions(User.Identity.Name));
            if ((!HttpContext.User.Identity.IsAuthenticated) || ((HttpContext.User.Identity.IsAuthenticated) && (HttpContext.User.IsInRole("CCD_EV_GB"))) ||
                (User.IsInRole("CCD_EV") && (!string.IsNullOrEmpty(permissions[0]._interface))))
            {
                string userName = "guest";
                if (HttpContext.User.Identity.IsAuthenticated) userName = HttpContext.User.Identity.Name;
                if (!(new DBpermissionsHandler()).CheckSMpermission(protocol, proj, phase, userName))
                {
                    System.Web.HttpContext.Current.Session["FS"] = new FullSession();
                    TempData["message"] = "Permission error:can not access Session Manage for protocol \"" + protocol + "\" project \"" + proj + "\" phase \"" + phase + "\"";
                    return View(new SessionsManageDisplay("", "", ""));
                }
            }
            //New permission check end:
            return View(SMDisp);

        }
        //SessionsManage Report Missing sessions tool
        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult ReportMissingSessionsInSM(string protocol, string proj, string phase, string FilterSessions, string _UnifyNVM)
        {

            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastSessionManageProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastSessionManagePhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastSessionManageProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            if (string.IsNullOrEmpty(FilterSessions)) FilterSessions = "Valid_Pending";
            //SessionsManageDisplay SMDisp = new SessionManageHandler().GetSessionsManageDisplay(protocol, proj, phase,FilterSessions);
            SessionManageHandler SM = new SessionManageHandler();
            SessionsManageDisplay SMDisp = SM.ReportMissingSessionsInSM(protocol, proj, phase, FilterSessions);
            if (_UnifyNVM == "Y")
            {
                SM.UnifyNVMSessions(SMDisp);
                ViewBag.UnifyNVM = "Y";
                ViewBag.NVMrequest = "N";
            }
            else
            {
                ViewBag.UnifyNVM = "N";
                ViewBag.NVMrequest = "Y";
            }
            if (SMDisp.Conf.tables.Count == 0) TempData["messageInfo"] = "No configuration is defined for: " + protocol + " " + proj + " " + phase;
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ViewBag.DB_TMP = DB_TMP;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManagePhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProtocol = protocol;


            //New permission check start:
            List<DBpermission> permissions = new List<DBpermission>();
            MDBUsers MDBUsersObj = new MDBUsers();
            if (User.Identity.IsAuthenticated) permissions = (MDBUsersObj.getPermissions(User.Identity.Name));
            if ((!HttpContext.User.Identity.IsAuthenticated) || ((HttpContext.User.Identity.IsAuthenticated) && (HttpContext.User.IsInRole("CCD_EV_GB"))) ||
                (User.IsInRole("CCD_EV") && (!string.IsNullOrEmpty(permissions[0]._interface))))
            {
                string userName = "guest";
                if (HttpContext.User.Identity.IsAuthenticated) userName = HttpContext.User.Identity.Name;
                if (!(new DBpermissionsHandler()).CheckSMpermission(protocol, proj, phase, userName))
                {
                    System.Web.HttpContext.Current.Session["FS"] = new FullSession();
                    TempData["message"] = "Permission error:can not access Session Manage for protocol \"" + protocol + "\" project \"" + proj + "\" phase \"" + phase + "\"";
                    return View(new SessionsManageDisplay("", "", ""));
                }
            }
            //New permission check end:
            return View(SMDisp);

        }


        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSRConfigure(string protocol, string proj, string phase)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastSessionManageProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastSessionManagePhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastSessionManageProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            SessionsManageDisplay SMDisp = new SessionManageHandler().GetSessionsManageDisplay(protocol, proj, phase, "ALL");
            if (SMDisp.Conf.tables.Count == 0) TempData["messageInfo"] = "No configuration is defined for: " + protocol + " " + proj + " " + phase;
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            string Interface = protocol.Split('_')[0];
            if (Interface.StartsWith("USB")) Interface = "USB";
            ViewBag.Interface = Interface;
            if (SMDisp.Conf.separateCSRperTable=="Y") ViewBag.tables = new List<string>(SMDisp.Conf.tables);
            else ViewBag.tables = new List<string>();
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManagePhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProtocol = protocol;

            string FileName = "";
            List<string> currTemplates = new List<string>();
            if (SMDisp.Conf.separateCSRperTable != "Y")
            {
                ViewBag.TemplateFiles = new List<string>(new CycleStatusHandler().GetCSRExcelTemplates(protocol, proj, phase, "", out FileName));
                currTemplates.Add(SMDisp.Conf.getActiveFileName(FileName));
                ViewBag.currTemplates = currTemplates;
                ViewBag.TemplateFileNoTable = FileName;
            }
            else
            {
                List<string> TemplateFiles = new List<string>();
                foreach (string table in SMDisp.Conf.tables)
                {
                    TemplateFiles.AddRange(new List<string>(new CycleStatusHandler().GetCSRExcelTemplates(protocol, proj, phase, table, out FileName)));
                    currTemplates.Add(SMDisp.Conf.getActiveFileName(FileName));
                    ViewBag.TemplateFileNoTable = Path.GetFileNameWithoutExtension(FileName).Replace(table, "");
                }
                ViewBag.TemplateFiles = TemplateFiles;
                ViewBag.currTemplates = currTemplates;
            }

            ViewBag.ConfigFiles = new List<string>(new CycleStatusHandler().GetCSRXMLConfigFiles(protocol, proj, phase, "", out FileName));
            ViewBag.currConfigfile = SMDisp.Conf.getActiveFileName(FileName) ;
            TempData.Remove("ExportedFileNAme");
            //New: Report assigning
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            MongoCursor Cur = MDBReport.ReadCursorAllUserTableReports(username);
            Dictionary<string, HashSet<string>> UserReports = new Dictionary<string, HashSet<string>>();
            foreach (BsonDocument rec in Cur)
            {
                UserTableReport Report = new UserTableReport();
                Report._id = rec.GetValue("_id", "").ToString();
                Report.Name = rec.GetValue("name", "").ToString();
                Report.RConfig = rec.GetValue("RConfig").AsBsonDocument.BsonDocumentToReportConfig();
                //string userNameAndTable = Regex.Replace(Report._id, @"\d*", "") + "_" + Report.RConfig.table;
                if (!UserReports.ContainsKey(Report.RConfig.table)) UserReports.Add(Report.RConfig.table, new HashSet<string>());
                UserReports[Report.RConfig.table].Add(Report.Name);
            }
            ViewBag.DictionaryUserTableReports = UserReports;
            return View(SMDisp);

        }
        
        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult ExecCycleReportBG(string protocol, string proj, string phase, string CSR_DB, string table, string FilterSessions)
        {
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            BsonDocument jmap = new BsonDocument();
            jmap.Add("protocol", protocol);
            jmap.Add("proj", proj);
            jmap.Add("phase", phase);
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("CSR_DB", CSR_DB);
            jmap.Add("table", table);
            jmap.Add("FilterSessions", FilterSessions);
            jmap.Add("type", "7");
            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "cycleStatusReport_" + protocol + "_" + proj + "_" + phase + nowStr + ".xlsx", "", jmap));

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "cycleStatusReport_" + protocol + "_" + proj + "_" + phase + "_" + nowStr + ".xlsx", "", jmap);
            //_task.status = "Export";
            //MDBUserBGTaskRec.SaveUserBGTask(_task);
            //JobWorker worker = new JobWorker();
            //worker.ExecCycleReportBG(nowStr, key, protocol, proj, phase, CSR_DB, table, FilterSessions);

            return RedirectToAction("MyBGTasks", "ManageDB");
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult ValidateCycleReportBG(string protocol, string proj, string phase, string CSR_DB, string table)
        {
            SessionsManageDisplay SMDisp = new SessionManageHandler().GetSessionsManageDisplay(protocol, proj, phase,"ALL");
            CycleStatusHandler cycleStatusHandler = new CycleStatusHandler();
            string validateReport = cycleStatusHandler.validateTemplateFileVSConf(SMDisp, table);
            if (!string.IsNullOrEmpty(validateReport) && (System.IO.File.Exists(validateReport)))
            {
                //TempData["ExportedFileNAme"] = validateReport;
                ViewBag.ExportedFileNAme = validateReport;
                return new FilePathResult(validateReport, "text/xml") { FileDownloadName = validateReport };
            }
            return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult SessionsManageEdit(string protocol, string proj, string phase, string _id)
        {
            //TODO: move relevant logic to SessionManageHandler/SessionManage/MDBSessionManage
            SessionsManageProtocolConf Conf = (new MDBSessionsManageConf()).Read(protocol, proj, phase);
            MDBSessionsManage MDBS = new MDBSessionsManage();

            List<SessionsManage> SMList =  MDBS.ReadSameBoardSessions(_id);
            foreach (var value in Enum.GetValues(typeof(General.SessionValidationStatus)))
            {
                bool found = false;
                foreach (SessionsManage SM in SMList) if (SM.validation.Equals((General.SessionValidationStatus)value) == true) found = true;
                if (!found)
                {
                    SessionsManage SMTemp = new SessionsManage(SMList.First().toBsonDocument());
                    SMTemp._id = new ObjectId();
                    SMTemp.Sessions.Clear();
                    SMTemp.comment = "";
                    SMTemp.validation = (General.SessionValidationStatus)value;
                    SMList.Add(SMTemp);
                }
            }
            List<string>logicUnitsOptional = new List<string>();
            string tmpLogicUnit = SMList.First().logicUnit;
            //depricated: create a reduced list of possible logic units - switched with full list
            //if (tmpLogicUnit.IndexOf("??") > 0)
            //{
            //    tmpLogicUnit = tmpLogicUnit.Substring(0, tmpLogicUnit.IndexOf("??") - 1);
            //    logicUnitsOptional.Add("");
            //}
            //else
            //{
            //    if (tmpLogicUnit.Length > 1) tmpLogicUnit = tmpLogicUnit.Substring(0, tmpLogicUnit.Length - 2);
            //    else tmpLogicUnit = SMList.First().Data.GetValue("Skew", "??").ToString();
            //}
            //foreach (string l in Conf.GetLogicUnitsList())
            //{
            //    if (l.StartsWith(tmpLogicUnit)) logicUnitsOptional.Add(l);
            //}
            //if (logicUnitsOptional.Count == 0) logicUnitsOptional.Add("");
            //if ((logicUnitsOptional.Count == 1) && (logicUnitsOptional[0] == "")) logicUnitsOptional.AddRange(Conf.GetLogicUnitsList());
            if (tmpLogicUnit.Contains("?")) logicUnitsOptional.Add(tmpLogicUnit);
            logicUnitsOptional.AddRange(Conf.GetLogicUnitsList());
            ViewBag.logicUnitsOptional = logicUnitsOptional;
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManagePhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProtocol = protocol;
            //SMList should contain Same board ID * validation options
            return View(SMList);
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult SessionsManageEditExec(string protocol, string proj, string phase, string _id, string logicUnit)
        {

            if (string.IsNullOrEmpty(logicUnit))
            {
                TempData["message"] = "You must assign LogicUnit value before saving any changes";
                return RedirectToAction("SessionsManageEdit", new RouteValueDictionary(new { _id = _id, protocol = protocol, phase = phase, proj = proj }));
            }
            //TODO: move relevant logic to SessionManageHandler/SessionManage/MDBSessionManage

            MDBSessionsManage MDBS = new MDBSessionsManage();
            //prepare dictionary of validation:Session
            Dictionary<string, SessionsManage> SessionsValidationKeys = new SessionManageHandler().getEditExecSessionsDictionary(_id);
            foreach (string key in Request.Form.AllKeys)
            {
                var queryValue = Request.Form[key];
                if ( Regex.IsMatch(key, @"Sessions_\w*"))
                {
                    Match m = Regex.Match(key, @"Sessions_(\w*)");
                    string tmpQueryValue = queryValue.Replace(" ","");
                    tmpQueryValue = tmpQueryValue.Replace("\r", "");
                    tmpQueryValue = tmpQueryValue.Replace("\n", "");
                    tmpQueryValue = tmpQueryValue.TrimEnd(',');
                    List<string> tmpSessionsList = new List<string>();
                    if (!string.IsNullOrEmpty(tmpQueryValue)) tmpSessionsList = new List<string>(tmpQueryValue.Split(','));
                    if (SessionsValidationKeys.ContainsKey(m.Groups[1].Value))
                    {
                        SessionsValidationKeys[m.Groups[1].Value].Sessions = tmpSessionsList;
                        SessionsValidationKeys[m.Groups[1].Value].logicUnit = logicUnit;
                    }
                }

                if (Regex.IsMatch(key, @"Comment_\w*"))
                {
                    Match m = Regex.Match(key, @"Comment_(\w*)");
                    if (SessionsValidationKeys.ContainsKey(m.Groups[1].Value)) SessionsValidationKeys[m.Groups[1].Value].comment = queryValue;
                }

            }
            
            //verification??


            foreach (var pair in SessionsValidationKeys)
            {
                if (pair.Value.Sessions.Count>0) MDBS.Save(pair.Value);
                else MDBS.Delete(pair.Value._id);
            }
            //add parameters
            if ((proj == "HBR") && (phase == "A1")) phase = "A0"; //Special fix for HBR A0,A1 cycle
            return RedirectToAction("SessionsManageList", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj =proj}));
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult SessionsManageCreateSession(string SessionStart, string protocol, string proj, string phase, string table, string _db, string infodoc)
        {
            //TODO: move relevant logic to SessionManageHandler/SessionManage/MDBSessionManage
            SessionsManageProtocolConf Conf = (new MDBSessionsManageConf()).Read(protocol, proj, phase);
            string _id = (new SessionManageHandler()).CreateSMforMissingSession(SessionStart, protocol, proj, phase, table);
            MDBSessionsManage MDBS = new MDBSessionsManage();


            List<SessionsManage> SMList = MDBS.ReadSameBoardSessions(_id);
            foreach (var value in Enum.GetValues(typeof(General.SessionValidationStatus)))
            {
                bool found = false;
                foreach (SessionsManage SM in SMList) if (SM.validation.Equals((General.SessionValidationStatus)value) == true) found = true;
                if (!found)
                {
                    SessionsManage SMTemp = new SessionsManage(SMList.First().toBsonDocument());
                    SMTemp._id = new ObjectId();
                    SMTemp.Sessions.Clear();
                    SMTemp.comment = "";
                    SMTemp.validation = (General.SessionValidationStatus)value;
                    SMList.Add(SMTemp);
                }
            }

            string infodocMessage = '"' + SessionStart + '"' + " was added to: ";
            foreach (SessionsManage SM in SMList)
            {
                if (SM.validation == General.SessionValidationStatus.Pending_review)
                {
                    if (!infodoc.Contains("\"TMP results\" : NumberLong(0)")) 
                    {
                        if (!SM.Sessions.Contains(SessionStart)) SM.Sessions.Add(SessionStart);
                        infodocMessage += " Pending_review;";
                    }

                    if (infodoc.Contains("\"TMP results\" : NumberLong(0)"))
                    {
                        if (SM.Sessions.Contains(SessionStart)) SM.Sessions.Remove(SessionStart);
                    }
                }
                if (SM.validation == General.SessionValidationStatus.PRM_not_valid)
                {
                    if (!infodoc.Contains("\"PRM results exc\" : NumberLong(0)"))
                    {
                        SM.Sessions.Add(SessionStart);
                        infodocMessage += " PRM_not_valid;";
                    }
                }
                if (SM.validation == General.SessionValidationStatus.PRM_valid)
                {
                    if (!infodoc.Contains("\"PRM results inc\" : NumberLong(0)"))
                    {
                        SM.Sessions.Add(SessionStart);
                        infodocMessage += " PRM_valid;";
                    }
                }
            }

            infodocMessage += " Press save to apply (Pending_Review is already created)";
            List<string> logicUnitsOptional = new List<string>();
            string tmpLogicUnit = SMList.First().logicUnit;
            if (tmpLogicUnit.Contains("?")) logicUnitsOptional.Add(tmpLogicUnit);
            logicUnitsOptional.AddRange(Conf.GetLogicUnitsList());
            ViewBag.logicUnitsOptional = logicUnitsOptional;
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ViewBag.infodocMessage = infodocMessage;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManagePhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProtocol = protocol;
            return View("SessionsManageEdit",SMList);
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult SessionsManageAddSession(string protocol, string proj, string phase, string _id, string SessionStart,string infodoc)
        {
            //TODO: move relevant logic to SessionManageHandler/SessionManage/MDBSessionManage
            SessionsManageProtocolConf Conf = (new MDBSessionsManageConf()).Read(protocol, proj, phase);
            MDBSessionsManage MDBS = new MDBSessionsManage();

            List<SessionsManage> SMList = MDBS.ReadSameBoardSessions(_id);

            foreach (var value in Enum.GetValues(typeof(General.SessionValidationStatus)))
            {
                bool found = false;
                foreach (SessionsManage SM in SMList) if (SM.validation.Equals((General.SessionValidationStatus)value) == true) found = true;
                if (!found)
                {
                    SessionsManage SMTemp = new SessionsManage(SMList.First().toBsonDocument());
                    SMTemp._id = new ObjectId();
                    SMTemp.Sessions.Clear();
                    SMTemp.comment = "";
                    SMTemp.validation = (General.SessionValidationStatus)value;
                    SMList.Add(SMTemp);
                }
            }
            string infodocMessage = '"' + SessionStart + '"' + " was added to: ";
            foreach (SessionsManage SM in SMList)
            {
                if (SM.validation == General.SessionValidationStatus.Pending_review)
                {
                    if (!infodoc.Contains("\"TMP results\" : NumberLong(0)"))
                    {
                        SM.Sessions.Add(SessionStart);
                        infodocMessage += " Pending_review;";
                    }
                }
                if (SM.validation == General.SessionValidationStatus.PRM_not_valid)
                {
                    if (!infodoc.Contains("\"PRM results exc\" : NumberLong(0)"))
                    {
                        SM.Sessions.Add(SessionStart);
                        infodocMessage += " PRM_not_valid;";
                    }
                }
                if (SM.validation == General.SessionValidationStatus.PRM_valid)
                {
                    if (!infodoc.Contains("\"PRM results inc\" : NumberLong(0)"))
                    {
                        SM.Sessions.Add(SessionStart);
                        infodocMessage += " PRM_valid;";
                    }
                }
            }

            infodocMessage += " Press save to apply";

            List<string> logicUnitsOptional = new List<string>();
            string tmpLogicUnit = SMList.First().logicUnit;
            if (tmpLogicUnit.Contains("?")) logicUnitsOptional.Add(tmpLogicUnit);
            logicUnitsOptional.AddRange(Conf.GetLogicUnitsList());
            ViewBag.logicUnitsOptional = logicUnitsOptional;
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ViewBag.infodocMessage = infodocMessage;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManagePhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastSessionManageProtocol = protocol;
            //SMList should contain Same board ID * validation options
            return View("SessionsManageEdit",SMList);
        }
        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult SessionsManageQueryDB(string _id)
        {
            //TODO: move relevant logic to SessionManageHandler/SessionManage/MDBSessionManage
            MDBSessionsManage MDBS = new MDBSessionsManage();

            SessionsManage SM = MDBS.Read(_id);
            string _db = DB_TMP;
            string action = "SearchTMP";
            string sessionsStr = "";
            string _query = "EQ;ProjectName;" + SM.project + ";EQ;Phase;"+ SM.phase + ";In;SessionStart;";
            if ((SM.validation == General.SessionValidationStatus.PRM_not_valid) || (SM.validation == General.SessionValidationStatus.PRM_valid))
            {
                _db = DB_PRM;
                action = "SearchPRM";
                if (SM.validation == General.SessionValidationStatus.PRM_valid) _query = "EQ;Indicator;Y;" + _query;
                else _query = "EQ;Indicator;N;" + _query;
            }
            foreach (string session in SM.Sessions)
            {
                if (string.IsNullOrWhiteSpace(sessionsStr)) sessionsStr = session;
                else sessionsStr += "," + session;
            }

            return RedirectToAction(action, "ManageDB", new RouteValueDictionary(new { db = _db, col = SM.table, Query = _query + sessionsStr }));
        }

        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult ValidatePendingItems(FormCollection formCollection,string protocol, string proj, string phase)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastSessionManageProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastSessionManagePhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastSessionManageProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];

            MDBSessionsManage MDBS = new MDBSessionsManage();
            foreach (var key in formCollection.AllKeys)
            {
                if (key.ToString().StartsWith("_cb"))
                {
                    string _id = (key.ToString().Substring(3));
                    SessionsManage SMPending = MDBS.Read(_id);
                    if (SMPending.logicUnit.Contains("??"))
                    {
                        TempData["message"] = "You must assign LogicUnit for all PendingReview items you want to validate";
                        return RedirectToAction("SessionsManageList", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
                    }
                }
            }

            foreach (var key in formCollection.AllKeys)
            {
                if (key.ToString().StartsWith("_cb"))
                {
                    string _id = (key.ToString().Substring(3));
                    SessionsManage SMPending = MDBS.Read(_id);
                    SessionsManage SMTemp = null;
                    List<SessionsManage> SMList = MDBS.ReadSameBoardSessions(_id);
                    foreach (SessionsManage SM in SMList) if (SM.validation == General.SessionValidationStatus.TMP_valid) SMTemp = SM;
                    if (SMTemp==null)
                    {
                        SMTemp = new SessionsManage(SMList.First().toBsonDocument());
                        SMTemp._id = new ObjectId();
                        SMTemp.Sessions.Clear();
                        SMTemp.comment = "";
                        SMTemp.validation = General.SessionValidationStatus.TMP_valid;
                    }
                    SMTemp.Sessions.AddRange(SMPending.Sessions);
                    MDBS.Save(SMTemp);
                    MDBS.Delete(SMPending._id);
                }
            }

            return RedirectToAction("SessionsManageList", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));

        }

        [Authorize(Roles = "CCD_EV,CCD_EV_GB")]
        public ActionResult CSRLaunchReports(string protocol, string proj, string phase, string table_name, string table_div)
        {
            List<UserTableReport> Reports = new List<UserTableReport>();
            SessionsManageProtocolConf Conf = (new MDBSessionsManageConf().Read(protocol, proj, phase));
            MDBUserTableReport MDBUserTableReportObj = new MDBUserTableReport();
            if (table_div.ToUpper() == "ALL")
            {
                table_name = "ALL";
                foreach (var TDVal in Conf.table_divs.Values)
                {
                    foreach (TableDiv TD in TDVal.Values)
                    {
                        Reports.Add(MDBUserTableReportObj.ReadUserTableReport(TD.SRreportId));
                        foreach (string reportId in TD.reportIds)
                        {
                            Reports.Add(MDBUserTableReportObj.ReadUserTableReport(reportId));
                        }
                    }
                }
            }
            else
            {
                if (Conf.table_divs.ContainsKey(table_name))
                {
                   if(Conf.table_divs[table_name].ContainsKey(table_div))
                   {
                       Reports.Add(MDBUserTableReportObj.ReadUserTableReport(Conf.table_divs[table_name][table_div].SRreportId));
                       foreach (string reportId in Conf.table_divs[table_name][table_div].reportIds)
                       {
                           Reports.Add(MDBUserTableReportObj.ReadUserTableReport(reportId));
                       }
                   }
                }
            }
            ViewBag.protocol = protocol;
            ViewBag.proj = proj;
            ViewBag.phase = phase;
            ViewBag.table_div = table_div;
            ViewBag.table = table_name;
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            ViewBag.Mail = (new MDBUsers()).getMail(username); 

            return View(Reports);
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSRConfigDownloadFile(string protocol, string proj, string phase, string filename)
        {
            string mimeSuffix = "application/vnd.ms-excel";
            if (Path.GetExtension(filename) == "xml") mimeSuffix = "text/xml";
            string filepath = Path.Combine(ServerDir, "EVData", filename);
            if (System.IO.File.Exists(filepath))
            {
                TempData["ExportedFileNAme"] = filepath; 
                return new FilePathResult(filepath, mimeSuffix) { FileDownloadName = filename };
            }
            else
            {
                TempData["message"] = "ERROR: cannot find file " + filename;
                return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSRConfigUploadFile(string protocol, string proj, string phase, string table, HttpPostedFileBase file)
        {
            string fileName = "";
            string path = "";
            bool isXML = false;
            bool isEXL = false;
            bool savedInEVDATA = false;
            if ((file != null) && (file.ContentLength > 0))
            {
                fileName = Path.GetFileName(file.FileName);
                path = Path.Combine(ServerDir, "uploads", fileName);
                try
                {
                    file.SaveAs(path);
                }
                catch (Exception e)
                {
                    TempData["message"] = "Error: cannot save file " + e.Message;
                    return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
                }
            }
            else
            {
                TempData["message"] = "Error: you must choose a file to upload";
                return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }

            if (Path.GetExtension(fileName) == ".xml") isXML = true;
            if (Path.GetExtension(fileName) == ".xlsx") isEXL = true;
            if ((!isXML) && (!isEXL))
            {
                TempData["message"] = "Error: file type is not supported, must be [xml,xlsx] not:" + fileName;
                return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }

            string FileName = "";
            string message = "";
            CycleStatusHandler SCRHandler = new CycleStatusHandler();
            string XMLMessage = "";
            if (isEXL) 
            {
                SCRHandler.GetCSRExcelTemplates(protocol, proj, phase, table, out FileName);
            }
            if (isXML)
            {
                SCRHandler.GetCSRXMLConfigFiles(protocol, proj, phase, "", out FileName);
            }

            if (!Path.GetFileNameWithoutExtension(fileName).StartsWith(Path.GetFileNameWithoutExtension(FileName)))
            {
                TempData["message"] = "Error: The file  - " + file.FileName + " does not have a valid prefix. Valid prefix: " + Path.GetFileNameWithoutExtension(FileName);
                return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }

            if (isXML)
            {
                SessionManageHandler SMHandler = new SessionManageHandler();
                if (!SMHandler.CheckSessionsManageConf(path, out XMLMessage))
                {
                    TempData["message"] = "Error:" + XMLMessage;
                    return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
                }
                if (!SMHandler.loadSessionsManageConfToDB(path))
                {
                    TempData["message"] = "Error: cannot load XML conf to DB";
                    return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
                }
                else XMLMessage = " new XML was loaded to DB";
            }

            savedInEVDATA = CCDUtilities.CopyFileSaveBackup(path, Path.Combine(ServerDir, "EVData"), Path.GetFileName(path), out message);
            if (savedInEVDATA)
            {
                TempData["messageInfo"] = "File " + fileName + " saved successfully";
                
                MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
                SessionsManageProtocolConf SMPConf = MDBSMconf.Read(protocol, proj, phase);
                SMPConf.updateActiveFileName(FileName,fileName);
                MDBSMconf.SaveDoNotOverWriteTimestamp(SMPConf);
            }

            else TempData["message"] = "Error while saving file:" + message + XMLMessage;
            return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));

        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSRConfigMakeFileActive(string protocol, string proj, string phase, string table, string fileName)
        {
            string path = Path.Combine(ServerDir, "EVData", fileName);
            bool isXML = false;
            bool isEXL = false;

            if (Path.GetExtension(fileName) == ".xml") isXML = true;
            if (Path.GetExtension(fileName) == ".xlsx") isEXL = true;

            string FileName = "";
            CycleStatusHandler SCRHandler = new CycleStatusHandler();
            string XMLMessage = "";
            if (isEXL)
            {
                SCRHandler.GetCSRExcelTemplates(protocol, proj, phase, table, out FileName);
            }
            if (isXML)
            {
                SCRHandler.GetCSRXMLConfigFiles(protocol, proj, phase, "", out FileName);
            }

            if (isXML)
            {
                SessionManageHandler SMHandler = new SessionManageHandler();
                if (!SMHandler.CheckSessionsManageConf(path, out XMLMessage))
                {
                    TempData["message"] = "Error:" + XMLMessage;
                    return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
                }
                if (!SMHandler.loadSessionsManageConfToDB(path))
                {
                    TempData["message"] = "Error: cannot load XML conf to DB";
                    return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
                }
                else XMLMessage = " new XML was loaded to DB";
            }


            TempData["messageInfo"] = "File " + fileName + " is now active";

            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.Read(protocol, proj, phase);
            SMPConf.updateActiveFileName(FileName, fileName);
            MDBSMconf.SaveDoNotOverWriteTimestamp(SMPConf);

            return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSRConfigCopyConfig(string Interface,string _protocol,  string proj, string phase, string t_proj, string t_phase)
        {
            string message = "";
            string errMessage = "";
            bool success;
            SessionManageHandler SMH = new SessionManageHandler();
            CycleStatusHandler SCRHandler = new CycleStatusHandler();
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();

            List <string> protocols = SMH.GetSessionsManageConfProtocols(Path.Combine(ServerDir, "EVData", "SessionsManageConf_" + Interface + "_" + proj + "_" + phase + ".xml"));
            if (protocols.Count == 0)
            {
                TempData["message"] = "Can not find XML file: " + Path.Combine(ServerDir, "EVData", "SessionsManageConf_" + Interface + "_" + proj + "_" + phase + ".xml");
                return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = _protocol, phase = phase, proj = proj }));
            }
            SessionsManageDisplay SMDispTarget = SMH.GetSessionsManageDisplay(protocols[0], t_proj, t_phase, "ALL");
            if (SMDispTarget.Conf.tables.Count != 0)
            {
                TempData["message"] = "Configuration is already defined for: " + protocols[0] + " " + t_proj + " " + t_phase;
                return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = _protocol, phase = phase, proj = proj }));
            }
            foreach (string protocol in protocols)
            {
                success = SMH.CSRConfigCopyConfig(protocol, proj, phase, t_proj, t_phase, out message);
                if (!success)
                {
                    errMessage += message;
                }
            }
            if (!string.IsNullOrEmpty(errMessage)) TempData["message"] = errMessage;
            else TempData["messageInfo"] = "Interface was created successfully: " + Interface + "_" + t_proj + "_" + t_phase;
            return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = _protocol, phase = phase, proj = proj }));
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSRConfigDeleteFile(string protocol, string proj, string phase, string filename)
        {
            System.IO.File.Delete(Path.Combine(ServerDir,"EVData", filename));
            return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSRConfigAssignReports(string protocol, string proj, string phase)
        {
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            Dictionary<string, string> SRreports = new Dictionary<string, string>();
            Dictionary<string, List<string>> PRMreports = new Dictionary<string, List<string>>();
            foreach (string key in Request.Form.AllKeys)
            {
                var queryValue = Request.Form[key];
                if (Regex.IsMatch(key, @"SRreport_.*"))
                {
                    Match m = Regex.Match(key, @"SRreport_(.*)");
                    string reportName = queryValue;
                    string tableDivName = m.Groups[1].Value;
                    if (!string.IsNullOrEmpty(reportName)) SRreports.Add(tableDivName, reportName);
                }
                if (Regex.IsMatch(key, @"PRMreport\d*_.*"))
                {
                    Match m = Regex.Match(key, @"PRMreport\d*_(.*)");
                    string reportName = queryValue;
                    string tableDivName = m.Groups[1].Value;
                    if (!string.IsNullOrEmpty(reportName) && (reportName.Length>1)) 
                    {
                        if (!PRMreports.ContainsKey(tableDivName)) PRMreports.Add(tableDivName,new List<string>());
                        PRMreports[tableDivName].Add(reportName);
                    }
                }
            }
            //update XML with SRreports and load to DB
            SessionManageHandler SMHandler = new SessionManageHandler();
            string message;
            if (!SMHandler.CSRAssignReports(protocol, proj, phase, SRreports,PRMreports,out message)) TempData["message"] = message ;
            else TempData["messageInfo"] = "Reports were assigned successfully";

            return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
        }


        [Authorize(Roles = "CCD_EV")]
        public ActionResult SessionsManageCreateSchedule(string proj, string phase, string frequency, string day, string hour, string mails)
        {
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBBGTaskSchedule MDBBGTS = new MDBBGTaskSchedule();
            BGTaskSchedule BGTS = new BGTaskSchedule(username + "_SessionsManage_" + proj + "_" + phase, "SessionsManage_" + proj + "_" + phase);
            BGTS.frequency = frequency;
            BGTS.day = day;
            BGTS.hour = hour;
            BGTS.user = username;

            BGTS.UpdateJmap("", "", "10", proj, phase, mails);
            if (MDBBGTS.SaveBGTaskSchedule(BGTS)) TempData["messageInfo"] = "Scedule has been created";
            else TempData["message"] = "Error:could not create schedule";
            return RedirectToAction("JobsSchedulerADM", new RouteValueDictionary(new { proj = proj, phase = phase }));
        }
        //

        [Authorize(Roles = "CCD_EV")]
        public ActionResult HotCornersStatsCreateSchedule(string proj, string phase, string frequency, string day, string hour, string mails)
        {
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBBGTaskSchedule MDBBGTS = new MDBBGTaskSchedule();
            BGTaskSchedule BGTS = new BGTaskSchedule(username + "_HotCornerStats_" + proj + "_" + phase, "HotCornerStats_" + proj + "_" + phase);
            BGTS.frequency = frequency;
            BGTS.day = day;
            BGTS.hour = hour;
            BGTS.user = username;

            BGTS.UpdateJmap("", "", "11", proj, phase, mails);
            if (MDBBGTS.SaveBGTaskSchedule(BGTS)) TempData["messageInfo"] = "Scedule has been created";
            else TempData["message"] = "Error:could not create schedule";
            return RedirectToAction("JobsSchedulerADM", new RouteValueDictionary(new { proj = proj, phase = phase }));
        }
        //

        public ActionResult ViewSpecLimitsConflicts(string proj, string phase, string table)
        {

            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.projlist = new List<string>(ProjectsList);

            DBsearch DBS = new DBsearch();
            DBS.Query = "EQ;project;" + proj + ";EQ;phase;" + phase ;
            DBS.SortStr = "SessionStart";
            DBS.SortDirection = "DES";
            DBS.col = table;
            DBS.db = DB_TMP;
            
            //return View();
            return RedirectToAction("SearchSpecLimitsConflicts", DBS);
        }

        public ActionResult SearchSpecLimitsConflicts(DBsearch obj)
        {

            //if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
            //    ((FullSession)System.Web.HttpContext.Current.Session["FS"]).PrmSearch.col != null)
            //{
            //    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).TmpSearch.getNewDBS();
            //    obj.Query = "Matches;_id;/^./";
            //    obj.SortStr = "date";
            //    obj.SortDirection = "DES";
            //}
            //else
            //{
            if (obj==null)
            {
                obj = new DBsearch();
                obj.db = DB_TMP;
                obj.col = DefaultCol;
                obj.SortStr = "SessionStart";
                obj.SortDirection = "DES";
            }

            ViewBag.q0list = lop;

            if (string.IsNullOrEmpty(obj.Query)) obj.Query = "Matches;_id;/^./";
            DBsearch DBS = new DBsearch();
            //table = obj.col;
            DBS.col = obj.col;
            DBS.db = obj.db;
            //table = table.Replace("+", @"\+");
            //DBS.Query = "Matches;_id;/" + table + "/;";
            //DBS.Query += obj.Query;
            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.FieldsStr = obj.FieldsStr;
            DBS.FieldsIncExc = obj.FieldsIncExc;
            if (obj.FieldsIncExc == "Exc")
            {
                DBS.FieldsExc = DBS.FieldsExc = true;
                DBS.FieldsInc = false;
            }
            //DBS.AddCoreFields();
            if (!String.IsNullOrEmpty(obj.SortStr)) TempData["JumpToBottom"] = "yes";
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "SessionStart";
            DBS.Skip = obj.Skip;
            DBS.jumpBottom = obj.jumpBottom;
            //(new MongoDBClient(dbAdress, DB_TMP, DefaultCol)).genericFind(DBS, DataTableLimit);
            string queryFilterWarn = ";EQ;type;Conflict";
            if (Roles.IsUserInRole("CCD_EV_ADM")) queryFilterWarn = "";
            DBS.ResCursor = (new MDBSpecLimitsConflicts()).MDBFind(DBS.Query + ";EQ;table;" + DBS.col + queryFilterWarn, "AND", DBS.SortDirection, DBS.SortStr);
            MongoCursor L_ResCursor = DBS.ResCursor.Clone((new BsonDocument()).GetType());

            foreach (BsonDocument item in L_ResCursor)
            {
                DBS.Sessions.Add(DBS.GetSesFromSpecOnflictsRec(item));
            }
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            foreach (string field in DBS.Fields) DBS.TableFields.Add(field);
            DBS.TableFields = new HashSet<string>(new string[] { "_id", "table", "project", "phase", "SessionStart", "type","SpecTestName", "SpecLimitsStatus", "TestStatus","usl","lsl","field","value" });
            String[] qarr = DBS.Query.Split(';');
            for (int i = 0; i < qarr.Length; i += 3)
            {
                DBS.QueryTriplets.Add((qarr[i]) + ";" + (qarr[i + 1]) + ";" + (qarr[i + 2]));
            }
            //DBS.col = table;
            DBS.Query = obj.Query;
            //DBS.QueryTriplets.RemoveAt(0);
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phaselist = new List<string>(PhasesList);
            return View(DBS);

        }

        [HttpPost]
        public ActionResult QuerySpecLimitsConflictSessions(string userids, string col, string _db)
        {
            DBsearch DBS;
            DBS = new DBsearch();

            DBS.Query = "In;SessionStart;" + userids;
            DBS.SortStr = "SessionStart";
            DBS.SortDirection = "DES";
            DBS.col = col;
            if (_db == "TMP") DBS.db = DB_TMP;
            if (_db == "PRM") DBS.db = DB_PRM;
            DBS.SearchOP = "AND";

            return RedirectToAction ("SearchSpecLimitsConflicts",DBS);
        }

        public ActionResult DeleteSpecLimitsConflictRecs(string query, string col)
        {
            bool success;

            MDBSpecLimitsConflicts db_obj = new MDBSpecLimitsConflicts();

            success = false;
            DBsearch DBS = new DBsearch();
            DBS.Query = query;
            DBS.SortStr = "SessionStart";
            DBS.SortDirection = "DES";
            DBS.col = col;
            DBS.db = DB_TMP;
            DBS.Skip = 0;
            DBS.Sort = new SortByBuilder();
            DBS.ResCursor = db_obj.MDBFind(DBS.Query + ";EQ;table;" + DBS.col, "AND", DBS.SortDirection, DBS.SortStr);

            TempData["message"] = "All matched record for query:'" + DBS.Query + "' where deleted from DB ";
            try
            {
                foreach (BsonDocument item in DBS.ResCursor)
                {
                    success = db_obj.Delete(item.GetValue("_id", "").ToString());
                }
                if (!success) TempData["message"] = "ERROR: Not all matched record for query:'" + DBS.Query + "' where deleted from DB ";
            }
            catch
            {
                TempData["message"] = "ERROR: Not all matched record for query:'" + DBS.Query + "' where not deleted from DB ";
            }

            return RedirectToAction("SearchSpecLimitsConflicts",DBS);
        }

        public ActionResult DeleteSpecLimitsConflictSessions(string userids, string col)
        {

            string query = "In;SessionStart;" + userids;


            return RedirectToAction("DeleteSpecLimitsConflictRecs", new RouteValueDictionary(new { query = query, col = col }));
        }

        public ActionResult CheckSpecLimitsConflicts(string project, string phase, string table)
        {
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            BsonDocument jmap = new BsonDocument();
            jmap.Add("table", table);
            jmap.Add("proj", project);
            jmap.Add("phase", phase);
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("type", "16");
            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "Check Spec Limit Violation " + table + " " + project + " " + phase, "", jmap));

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "Check Spec Limit Violation "+ table + " " + project + " " + phase, "", jmap);
            //_task.status = "Export";
            //MDBUserBGTaskRec.SaveUserBGTask(_task);
            //JobWorker worker = new JobWorker();
            //worker.CheckSpecTestNameLimitViolation(key,table,project,phase);

            return RedirectToAction("MyBGTasks", "ManageDB");
        }
    }
}

