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
using System.Net.Mail;
using CCD_EV_ManageDB.Models;
using CCD_EV_ManageDB.Repositories;
using CCD_EV_ManageDB.ActionFilters;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Web.Security;
using System.Threading;
using System.Threading.Tasks;
using MongoDBConsoleApplication1;

namespace CCD_EV_ManageDB.Controllers
{

    [Authorize(Roles = "CCD_EV_ADM")]
    [LogActionFilter]

    public class AdminDBController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ManageDBController));
        private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }

        private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        private readonly int DataTableLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DataTableLimit"]);

        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }

        private MongoDBClient db_tmp = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);
        private MongoDBClient db_prm = new MongoDBClient(dbAdress, DB_PRM, DefaultCol);

        //private List<string> lop = new List<string>(new string[] { "EQ", "Matches", "In", "GT", "LT" });
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }
        public string[] MongoTables { get { return System.Configuration.ConfigurationManager.AppSettings["MongoTables"].Split(','); } }
        public string[] CompTables { get { return System.Configuration.ConfigurationManager.AppSettings["CertificationTables"].Split(','); } }
        public string[] ProjectsList { get { return System.Configuration.ConfigurationManager.AppSettings["ProjectsList"].Split(','); } }
        public string[] PhasesList { get { return System.Configuration.ConfigurationManager.AppSettings["PhasesList"].Split(','); } }
        


        //
        // GET: /AdminDB/

        public ActionResult Index()
        {
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.compcollist = new List<string>(CompTables);
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phaselist = new List<string>(PhasesList);
            return View();
        }


        [HttpPost]
        public ActionResult FixHidden(string db)
        {
            db_tmp.fixHiddenFields(db);
            TempData["message"] = "Hidden fields of " + db + " tables are fixed";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult FixNumeric(string db)
        {
            db_tmp.fixNumericFields(db);
            TempData["message"] = "Numeric fields of " + db + " tables are fixed";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult FixLoadToPRM()
        {
            db_prm.fixLoadToPRM();
            TempData["message"] = "LoadToPRM field was added for PRM tables of TR project";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult IndexTable(string db,string table)
        {
            MDBStationResults resTable = new MDBStationResults("DB_" + db, table);
            //resTable.IndexAllFields();
            List<string> IndexFields = new List<string>();
            XDocument doc = XDocument.Load(Path.Combine(ServerDir, "EVData", "MongoTablesIndexes.xml"));
            XElement dbElem = doc.Root.Element(db);
            foreach (XElement tableElem in dbElem.Elements("Table"))
            {
                if (tableElem.Attribute("name").Value == table)
                {
                    foreach (XElement xe in tableElem.Elements("Field"))
                    {
                        IndexFields.Add(xe.Attribute("name").Value);
                    }
                    break;
                }
            }
                IndexFields.Add("TestEnd");
                resTable.IndexListOFFields(IndexFields);

            TempData["message"] = "Table:"+ table + " fields, has been indexed";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult IndexAllTables(string db)
        {
            List<string> IndexFields = new List<string>();
            XDocument doc = XDocument.Load(Path.Combine(ServerDir, "EVData", "MongoTablesIndexes.xml"));
            XElement dbElem = doc.Root.Element(db);

            string[] DBTables = MongoTables;
            if (db == "CMP")  DBTables = CompTables;

            foreach (string table in DBTables)
            {
                IndexFields.Clear();
                foreach (XElement tableElem in dbElem.Elements("Table"))
                {
                    if (tableElem.Attribute("name").Value == table)
                    {
                        foreach (XElement xe in tableElem.Elements("Field"))
                        {
                            IndexFields.Add(xe.Attribute("name").Value);
                        }
                        break;
                    }
                }
                if (!IndexFields.Contains("TestEnd")) IndexFields.Add("TestEnd");
                if (!IndexFields.Contains("SessionStart")) IndexFields.Add("SessionStart");
                
                MDBStationResults resTable = new MDBStationResults("DB_" + db, table);
                resTable.IndexListOFFields(IndexFields);

            }
            TempData["message"] = "Tables has been indexed";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult fixInAppTestName(string db, string table)
        {
            db_tmp.fixInAppTestName(db,table);
            TempData["message"] = "InAppTestName fields of " + db + ","  + table + " are fixed";
            return RedirectToAction("Index");
        }
        
        [HttpPost]
        public ActionResult LoadSpecMappingFile(HttpPostedFileBase file, string app, string table)
        {
            string path = "";
            path = Path.Combine(ServerDir, "EVData", table + "_appToSpec.xml");
            if (file == null)
            {
                TempData["message"] = "Error: please attach file";
                return RedirectToAction("Index");
            }
            try
            {
                file.SaveAs(path);
            }
            catch (Exception e)
            {
                TempData["message"] = "Error: cannot save mapping file as:" + path + " errMsg:" + e.Message;
                return RedirectToAction("Index");
            }
            if (new MDBAppRemoteMap().SaveAppRemoteMapRec(table, path)) TempData["messageInfo"] = "Mapping file was added in: " + path;
            else TempData["message"] = "Error: mapping file was not added properly for:\"" + table ;
            return RedirectToAction("Index");
        }
        [HttpPost]
        public ActionResult GetSpecExcelFromMappingFile(string app, string table)
        {
            string errMsg="";
            System.IO.File.Copy(Path.Combine(ServerDir, "EVData", "emptySpec.xlsx"), Path.Combine(ServerDir, "tmp", table + "_Spec.xlsx"),true);
            if (new ADMReferenceTable().CreateSpecExcelFromMappingFile(table, Path.Combine(ServerDir, "tmp", table + "_Spec.xlsx"), ref errMsg))
                TempData["message"] = "Excel file was created in:" + Path.Combine(ServerDir, "tmp", table + "_Spec.xlsx");
            else TempData["message"] = "Error: Excel file was not created for table:\"" + table + "\" ErrorMsg:" + errMsg;
            return RedirectToAction("Index");
        }

        public ActionResult CopyReportOfUser()
        {

            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.Users = ((new MDBUsers()).ReadAllUsers());
            ViewBag.Users.Sort();

            Dictionary<string, HashSet<string>> AllUsersTableReports = (new MDBUserTableReport()).GetDictionaryAllUsersTableReports();

            ViewBag.NGon.DictionaryAllUsersTableReports = AllUsersTableReports;
            ViewBag.UsersTablesReports = AllUsersTableReports.Keys;

            return View();
        }

        public ActionResult DeleteOldUserBGTasks()
        {
            string errMsg = "";

            if (new MDBUserBGTask().DeleteOldUserBGTasks(Convert.ToInt64(DateTime.Now.AddDays(-365).ToString("yyyyMMddHHmmss")), ref errMsg))
                TempData["messageInfo"] = "User BG tasks older than a year were deleted succesfully";
            else TempData["message"] = "Error: could not delete old user BG tasks, ErrorMsg:" + errMsg;

            return RedirectToAction("Index");
        }

        public ActionResult TestMail()
        {
            MailMessage mail = new MailMessage("naty.shemer@intel.com", "naty.shemer@intel.com");
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "iil-out.intel.com";
            mail.Subject = "this is a test email.";
            mail.Body = "this is my test email body";
            client.Send(mail);
            return RedirectToAction("Index");
        }

        public ActionResult TableMapSpecTestName(string colname, string db,string query)
        {
            bool errorOccurred;
            int errorCode;
            string errorMsg = null;
            int changedResults = 0;

            MongoDBUtilTS MDButilsForSpecMap = new MongoDBUtilTS(dbAdress, DB_TMP, colname, out errorOccurred, out errorCode, out errorMsg);
            MongoDBUtilTS MDButils = new MongoDBUtilTS(dbAdress, db, colname, out errorOccurred, out errorCode, out errorMsg);
            Dictionary<string, List<string>> SpecNameNFields = new XMLHandler().readSpecMappingFile(MDButilsForSpecMap.ReadRemoteApptoSpecFilePath(colname));
            MDBStationResults MDBCol = new MDBStationResults(db, colname);
            MongoCursor Cur = null;
            if (string.IsNullOrEmpty(query)) query = "Matches;_id;/.*/";
            Cur = MDBCol.MDBFind(query, "AND", "DES", "_id");
            List<BsonDocument> results = new List<BsonDocument>();
            string _SpecTestName = "";
            foreach (BsonDocument result in Cur)
            {
                results.Clear();
                results.Add(result);
                _SpecTestName = result.GetValue("SpecTestName", "").ToString() ;
                MDButils.mapTestFieldToSpecName(SpecNameNFields, "InAppTestName", results, errorMsg);
                if ((_SpecTestName) != result.GetValue("SpecTestName", "").ToString())
                {
                    MDBCol.Save(results[0]);
                    changedResults++;
                }
            }
            TempData["messageInfo"] = "SpecTestName mapped " + changedResults.ToString() + " results where changed";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult LoadSessionsManageConf(string Interafce, string proj, string phase)
        {
            SessionManageHandler SMHandler = new SessionManageHandler();
            string Message = "";
            string fileName;
            if (string.IsNullOrEmpty(Interafce)) fileName = Path.Combine(ServerDir, "EVData", "SessionsManageConf.xml");
            else fileName = Path.Combine(ServerDir, "EVData", "SessionsManageConf_" + Interafce + "_" + proj +"_" + phase +".xml");
            if (!SMHandler.CheckSessionsManageConf(fileName, out Message))
            {
                TempData["message"] = "Error:" + Message;
                return RedirectToAction("Index");
            }
            if (SMHandler.loadSessionsManageConfToDB(fileName))
                TempData["messageInfo"] = "SessionsManageConf was loaded successfully";
            else TempData["message"] = "Error: could not load SessionsManageConf" ;
            return RedirectToAction("Index");
        }

        public ActionResult ChangeAttachmentsPath(string dbname, string project, string sourcePathPrefix, string targetPathPrefix)
        {
            int cnt = db_tmp.ChangeAttachmentsPath(dbname, project, sourcePathPrefix, targetPathPrefix);
            TempData["message"] = "Path was changed for DB/project:" + dbname + "/" + project + ", " + cnt + " records where changed";
            return RedirectToAction("Index");
        }

        public ActionResult CreateTestNameLookUPForProject(string project,string phase)
        {
            MDBIndicator MDBIndicatorObj = new MDBIndicator();
            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();
            MDBTestNameLookUP MDBLookUpObj = new MDBTestNameLookUP();
            foreach (Indicator Ind in MDBIndicatorObj.ReadProjectPhaseIndicators(project, phase))
            {
                MDBStationResults MDBStationResultsPRMObj = new MDBStationResults("DB_PRM", Ind.table);
                MDBStationResults MDBStationResultsTMPObj = new MDBStationResults("DB_TMP", Ind.table);
                ADMReferenceTable specTable = MDBRef.ReadADMReferenceTable(Ind.spec);
                TestNameLookUP lookUPobj= new TestNameLookUP(project, Ind.table, specTable._id);

                foreach (FieldSlide slide in specTable.RConfig.FieldsSlides)
                {
                    string test = "Missing TestName";
                    BsonDocument result = MDBStationResultsPRMObj.MDBFindOne("EQ;SpecTestName;" + slide.test, "AND", "ASC", "_id");
                    if (result == null) result = MDBStationResultsTMPObj.MDBFindOne("EQ;SpecTestName;" + slide.test, "AND", "ASC", "_id");
                    if (result != null) test = result.GetValue("TestName", "Missing TestName").ToString(); 
                    if (!lookUPobj.LookUP.ContainsKey(slide.test)) lookUPobj.LookUP.Add(slide.test, test);
                }

                MDBLookUpObj.Save(lookUPobj.toBsonDocument());

            }
            return RedirectToAction("Index");
        }

        public ActionResult CreateProjectSpecsXMLforCustomExec(string project)
        {
            string CustomExecFile = Path.Combine(ServerDir,"tmp", project + "_ProjectSpecsXML_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
            MDBTestNameLookUP MDBLookUpObj = new MDBTestNameLookUP();
            MongoCursor Cur = MDBLookUpObj.MDBFind("Matches;_id;/^" + project + "/", "AND", "ASC", "_id");
            List<string> Lines = new List<string>();
            foreach (BsonDocument doc in Cur)
            {
                Lines.Clear();
                TestNameLookUP lookUPobj = new TestNameLookUP(doc);
                string tablePrefix = lookUPobj.table.Replace('-','_').Split('_')[0];
                tablePrefix=Regex.Replace(tablePrefix,@"\d*","");
                tablePrefix = tablePrefix.TrimEnd('.');
                if (tablePrefix.StartsWith("TBT")) tablePrefix="CIO";//only exception we have in table names?

                foreach (var spec in lookUPobj.LookUP)
                {
                    Lines.Add("<" + tablePrefix.ToLower() + ">" + spec.Key + "</" + tablePrefix.ToLower() + ">");
                }
                System.IO.File.AppendAllLines(CustomExecFile, Lines);
            }
            return RedirectToAction("Index");
        }

        public ActionResult PocExeCalling(string outFileName, string line)
        {
            //string filename = Path.Combine(ServerDir, "tmp", "ConsoleApplication1.exe");
            string filename = @"C:\tmp\ConsoleApplication1.exe";
            string cParams = '"' + Path.Combine(ServerDir, "tmp", outFileName) + '"' + " " + '"' + line + '"';
            var proc = System.Diagnostics.Process.Start(filename, cParams);
            return RedirectToAction("Index");
        }

        public ActionResult PocExeCallingBG(string outFileName, string line)
        {
            //string filename = Path.Combine(ServerDir, "tmp", "ConsoleApplication1.exe");
            string exefilename = @"C:\tmp\ConsoleApplication1.exe";
            string cParams = '"' + Path.Combine(ServerDir, "tmp", outFileName) + '"' + " " + '"' + line + '"';
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            BsonDocument jmap = new BsonDocument();
            jmap.Add("exefilename", exefilename);
            jmap.Add("outFileName", outFileName);
            jmap.Add("line", line);
            jmap.Add("key", key);
            jmap.Add("type", "9");
            UserBGTask _task = new UserBGTask(key, "PocExeCallingBG" + "_" + nowStr, "", jmap);
            //_task.status = "Launched4exe";
            MDBUserBGTaskRec.SaveUserBGTask(_task);
            //MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "PocExeCallingBG" + "_" + nowStr , "", jmap));

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "PocExeCallingBG" + "_" + nowStr, "", jmap);
            //MDBUserBGTaskRec.SaveUserBGTask(_task);
            //JobWorker worker = new JobWorker();
            //worker.PocExeCalling(key, outFileName, line, exefilename);

            return RedirectToAction("MyBGTasks", "ManageDB");
        }

        public ActionResult LoadCornerSetTimesFromExecutions()
        {
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            BsonDocument jmap = new BsonDocument();
            jmap.Add("type", "11");
            MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "HotCornerStatsBG", "", jmap));

            ////For testing on localhost (without service)
            //UserBGTask _task = new UserBGTask(key, "HotCornerStatsBG", "", jmap);
            //_task.status = "Export";
            //MDBUserBGTaskRec.SaveUserBGTask(_task);
            //JobWorker worker = new JobWorker();
            //worker.HotCornerStatsWorker(key);
            return RedirectToAction("MyBGTasks", "ManageDB");
        }

        public ActionResult LaunchTestModeMarginReport(string db_str, string _interface, string proj, string phase)
        {
            string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            string nowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string key = username + nowStr;
            MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();
            BsonDocument jmap = new BsonDocument();
            jmap.Add("type", "12");
            jmap.Add("nowStr", nowStr);
            jmap.Add("key", key);
            jmap.Add("proj", proj);
            jmap.Add("phase", phase);
            jmap.Add("_interface", _interface);
            jmap.Add("DBSdb",db_str);
            
            //MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key, "TestModeMarginReport", "", jmap));

            //For testing on localhost (without service)
            UserBGTask _task = new UserBGTask(key, "TestModeMarginReport", "", jmap);
            _task.status = "Export";
            MDBUserBGTaskRec.SaveUserBGTask(_task);
            JobWorker worker = new JobWorker();
            worker.TestModeMarginReportWorker(key, _interface, db_str, proj, phase, nowStr);
            return RedirectToAction("MyBGTasks", "ManageDB");
        }


    }
}
