using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using CCD_EV_ManageDB.Repositories;
using CCD_EV_ManageDB.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using PandaUtilities;
using System.Diagnostics;
using System.Reflection;

namespace CCD_EV_ManageDB.Models
{
    public class JobWorker
    {
        private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }

        private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        private readonly int DataTableLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DataTableLimit"]);

        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }
        private static string DB_REC { get { return System.Configuration.ConfigurationManager.AppSettings["DB_REC"]; } }
        private static string DB_CMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP"]; } }
        private static string DB_CMP_Arch { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP_Arch"]; } }
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }

        private MongoDBClient db = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);

        public void Execute()
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.FindRecentLaunched();

            if (String.IsNullOrEmpty(UBT.id)) return;

            string DBSquery = UBT.jmap.GetValue("DBSquery","").ToString();
            string DBSdb = UBT.jmap.GetValue("DBSdb","").ToString();
            string DBScol = UBT.jmap.GetValue("DBScol","").ToString();
            string DBSsortStr = UBT.jmap.GetValue("DBSsortStr", "").ToString();
            string DBSsortDirection = UBT.jmap.GetValue("DBSsortDirection", "").ToString();
            string nowstr = UBT.jmap.GetValue("nowStr","").ToString();
            string type = UBT.jmap.GetValue("type","").ToString();
            string DUT = UBT.jmap.GetValue("DUT", "").ToString();
            string vTableStr = UBT.jmap.GetValue("vTableStr", "").ToString();
            string debug = UBT.jmap.GetValue("debug", "").ToString();
            string baseUrl = UBT.jmap.GetValue("baseUrl", "").ToString();
            string FieldSlidesStr = UBT.jmap.GetValue("FieldSlidesStr", "").ToString();
            string proj = UBT.jmap.GetValue("proj", "").ToString();
            string phase = UBT.jmap.GetValue("phase", "").ToString();
            string mails = UBT.jmap.GetValue("mails", "").ToString();

            string sheets = UBT.jmap.GetValue("sheets", "").ToString();
            string fields = UBT.jmap.GetValue("fields", "").ToString();
            string TestNameField = UBT.jmap.GetValue("TestNameField", "").ToString();
            string template = UBT.jmap.GetValue("template", "").ToString();
            string protocol = UBT.jmap.GetValue("protocol", "").ToString();
            string group = UBT.jmap.GetValue("group", "").ToString();
            string visFields = UBT.jmap.GetValue("visFields", "").ToString();
            string CSR_DB = UBT.jmap.GetValue("CSR_DB", "").ToString();
            string exefilename = UBT.jmap.GetValue("exefilename", "").ToString();
            string outFileName = UBT.jmap.GetValue("outFileName", "").ToString();
            string line = UBT.jmap.GetValue("line", "").ToString();
            string username = UBT.jmap.GetValue("username", "").ToString();
            string ids_str = UBT.jmap.GetValue("ids_str", "").ToString();
            string table_div = UBT.jmap.GetValue("table_div", "").ToString();
            string Query = UBT.jmap.GetValue("Query", "").ToString();
            string table = UBT.jmap.GetValue("table", "").ToString();
            string FilterSessions = UBT.jmap.GetValue("FilterSessions", "").ToString();
            string _interface = UBT.jmap.GetValue("_interface", "").ToString();
            string LookUPTableName = UBT.jmap.GetValue("LookUPTableName", "").ToString();
            string FilePath = UBT.jmap.GetValue("FilePath", "").ToString();
            
            


            DBsearch DBS = new DBsearch();
            DBS.db = DBSdb;
            DBS.Query = DBSquery;
            DBS.col = DBScol;
            DBS.SortStr = DBSsortStr;
            DBS.SortDirection = DBSsortDirection;

            switch (type)
            {
                case "1":
                    ExecExportEditFileBG(DBS, nowstr, UBT.id);
                    break;
                case "2":
                    ExecRunReportBG(UBT.sourceID, DBS, nowstr, UBT.id, debug, FieldSlidesStr);
                    break;
                case "3":
                    ExecRunCompReportBG(DUT,DBSquery, DBScol, nowstr, UBT.id);
                    break;
                case "4":
                    ValidateCompReportBG(DUT, DBSquery, DBScol, nowstr, UBT.id, vTableStr, baseUrl);
                    break;
                case "5":
                    RunAllIndicators(nowstr,UBT.id,proj,phase,_interface,mails);
                    break;
                case "6":
                    ExportReportFileBG(DBS, nowstr, UBT.id, sheets, fields, TestNameField, template);
                    break;
                case "7":
                    ExecCycleReportBG(nowstr, UBT.id, protocol, proj, phase, CSR_DB, table, FilterSessions);
                    break;
                case "8":
                    ExportVisualizationFileBG(DBS, nowstr, UBT.id, group, visFields);
                    break;
                case "9":
                    PocExeCalling(UBT.id, outFileName, line, exefilename);
                    break;
                case "10":
                    SessionsManageListWorker(UBT.id, proj, phase);
                    break;
                case "11":
                    HotCornerStatsWorker(UBT.id);
                    break;
                case "12":
                    TestModeMarginReportWorker(UBT.id,_interface,DBSdb,proj,phase,nowstr);
                    break;
                case "13":
                    BoardsInventoryWorker(UBT.id, proj, phase);
                    break;
                case "14":
                    ExportReportFileCellQueriesBG(DBS, nowstr, UBT.id, sheets, fields, TestNameField, template);
                    break;
                case "15":
                    CreateSpecLimitLookup(UBT.id, table, proj, phase, LookUPTableName);
                        break;
                case "16":
                    CheckSpecTestNameLimitViolation(UBT.id, table, proj, phase);
                    break;
                case "17":
                    LoadLanFile(UBT.id, FilePath, table, proj, phase);
                    break;
                case "18":
                    RunLiveIndicators(nowstr, UBT.id, proj, phase);
                    break;     
                case "19":
                    CleanRecycleDB(nowstr, UBT.id);
                    break;
                case "20":
                    CheckSpecTestNameLimitViolationBatch(UBT.id, proj, phase);
                    break;
                case "21":
                    ExecRunCSRReportsBG(nowstr, username, UBT.id, ids_str, protocol, proj, phase, table_div, mails, Query);
                    break;
                default:
                    UBT.status="Error: task type is not supported";
                    MDBUserBGTask.SaveUserBGTask(UBT);
                    break;
            }

        }

        //can be depricated? used for POC of remote exe execution, (whne I could not call directly from service) see:
        //C:\Users\nshemer\Documents\Visual Studio 2010\Projects\BGTaskServuce4exe
        public void Execute4exe()
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.FindRecentLaunched4exe();

            if (String.IsNullOrEmpty(UBT.id)) return;

            string type = UBT.jmap.GetValue("type", "").ToString();
            string exefilename = UBT.jmap.GetValue("exefilename", "").ToString();
            string outFileName = UBT.jmap.GetValue("outFileName", "").ToString();
            string line = UBT.jmap.GetValue("line", "").ToString();

            switch (type)
            {
                case "1":
                    PocExeCalling(UBT.id, outFileName, line, exefilename);
                    break;
                default:
                    UBT.status = "Error: task type is not supported";
                    MDBUserBGTask.SaveUserBGTask(UBT);
                    break;
            }

        }


        public void ExecRunCompReportBG(string DUT, string _query, string templ, string nowstr, string key)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);
            string filename = "CompReport_" + DUT + "_" + nowstr + ".docx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(Path.Combine(ServerDir, "EVData", templ + ".docx"), filepath, true);

            CompWordReportHandler ReportHandler = new CompWordReportHandler();
            bool errorOccurred;
            string errorMsg;
            ReportHandler.ExecDemoReport(DUT, _query, filepath, out errorOccurred, out errorMsg);
            UBT.status = "Done";
            if (!String.IsNullOrEmpty(errorMsg)) UBT.status = "Error: " + errorMsg;
            UBT.result = filepath;//link?
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        public void ValidateCompReportBG(string DUT, string _query, string templ, string nowstr, string key, string vTableStr, string baseUrl)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);
            string filename = "CompReport_" + DUT + "_" + nowstr + ".docx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(Path.Combine(ServerDir, "EVData", templ + ".docx"), filepath, true);

            CompWordReportHandler ReportHandler = new CompWordReportHandler();
            bool errorOccurred;
            string errorMsg;
            List <BsonDocument> MissedResults = ReportHandler.Validate( _query, filepath, vTableStr,out errorOccurred, out errorMsg);
            System.IO.File.Delete(filepath);
            if (!String.IsNullOrEmpty(errorMsg)) UBT.status = "Error: " + errorMsg;

            if (MissedResults.Count > 0)
            {
                filename = "CompReportVal_" + DUT + "_" + nowstr + ".htm";
                filepath = Path.Combine(ServerDir, "tmp", filename);
                ReportHandler.writeValidationHTML(DUT, templ,vTableStr, MissedResults, filepath, baseUrl,out errorOccurred, out errorMsg);
                UBT.result = filepath;
                if (!String.IsNullOrEmpty(errorMsg)) UBT.status = "Error: " + errorMsg;
            }
            else
            {
                UBT.result2 = "Results are complete for DUT:" + DUT + " Template: " + templ + " Interfaces:" + vTableStr;
            }
            UBT.status = "Done";
            MDBUserBGTask.SaveUserBGTask(UBT);
        }
        public void ExecExportEditFileBG(DBsearch DBS, string nowStr, string key)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            string errMassage = "";
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptyExcel"];
            List<BsonDocument> results = new List<BsonDocument>();
            bool errorOccurred, success;
            int errorCode;
            String errorMsg;
            success = false;

            ExcelHandler ExlExport = new ExcelHandler();

            DBS.Skip = 0;
            DBS.Sort = new SortByBuilder();
            DBS.SortStr = "TestEnd";
            db.genericFind(DBS, 0);

            string filename = "ccd_export_" + DBS.col + "_" + nowStr + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(EmptyExcel, filepath, true);

            try
            {
                Boolean HiddenFieldsAdded = false;
                foreach (BsonDocument item in DBS.ResCursor)
                {
                    results.Add(item);
                    if (!HiddenFieldsAdded)
                    {
                        int fieldsNumber = DBS.Fields.Count;
                        foreach (BsonElement field in item) DBS.Fields.Add(field.Name);
                        if (DBS.Fields.Count > fieldsNumber) HiddenFieldsAdded = true;
                    }
                }
            }
            catch
            {
                UBT.status = "Error";
                UBT.result = "cannot export query:'" + DBS.Query + "' results'";
                MDBUserBGTask.SaveUserBGTask(UBT);
            }

            ExlExport.writeSheet(filepath, "Results", results, DBS.Fields.ToList<string>());

            if (DBS.db != DB_CMP_Arch) // Do not delete exported records in case of compArch DB
            {
                try
                {
                    foreach (BsonDocument item in results)
                    {
                        success = db.dbDelete(DBS, item.AsBsonDocument.GetElement("_id").Value.ToString(), false, out errorOccurred, out errorCode, out errorMsg);
                        if (!success) errMassage = "Error:All matched record for query:'" + DBS.Query + "' where exported to Excel, not all exported results where deleted from DB ";
                    }
                }
                catch
                {
                    UBT.status = "Error:All matched record for query:'" + DBS.Query + "' where exported to Excel, not all exported results where deleted from DB ";
                    MDBUserBGTask.SaveUserBGTask(UBT);
                    return;
                }
            }

            UBT.status = "Done";
            if (!String.IsNullOrEmpty(errMassage)) UBT.status += " " + errMassage;
            UBT.result = filepath;//link?
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        public void ExecRunReportBG(string _id, DBsearch DBS, string nowStr, string key, string debug, string FieldSlidesStr)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            string errMassage = "";
            bool needToWait = true;
            UBT.status = "In progress";
            PandaUtilities.JobStatus status = PandaUtilities.JobStatus.NOT_EXIST;
            MDBUserBGTask.SaveUserBGTask(UBT);

            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);

            //0. Create tmp dir, copy templates fie, create RawData excel file according to saved Query
            //string NowStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string dirpath = Path.Combine(ServerDir, "tmp", "rg_" + Report.RConfig.table + "_" + nowStr);
            try
            {
                Directory.CreateDirectory(dirpath);
                Directory.CreateDirectory(Path.Combine(dirpath, "RawData"));
                Directory.CreateDirectory(Path.Combine(dirpath, "Results"));
                if (Report.useMarginLimits)
                {
                    Directory.CreateDirectory(Path.Combine(dirpath, "MarginData"));
                }
                string RGTemplates = System.Configuration.ConfigurationManager.AppSettings["RGTemplates"];
                System.IO.File.Copy(RGTemplates, Path.Combine(dirpath, Path.GetFileName(RGTemplates)));

            }
            catch (Exception e)
            {
                UBT.status = "Errorwhile creating report tmp directory:" + e.Message;
                UBT.result = "";
                MDBUserBGTask.SaveUserBGTask(UBT);
                return;
            }
            //run current PRM query and create Excel of RawData in report directory
            try
            {
                DBS.Skip = 0;
                DBS.Sort = new SortByBuilder();
                if (string.IsNullOrEmpty(DBS.SortStr)) DBS.SortStr = "TestEnd";
                db.genericFind(DBS, 0);
                string rfilename = "ccd_report_" + DBS.col + "_" + nowStr + ".xlsx";
                string mfilename = "ccd_report_margin_" + DBS.col + "_" + nowStr + ".xlsx";
                string rfilepath = Path.Combine(dirpath, "RawData", rfilename);
                string mfilepath = Path.Combine(dirpath, "MarginData", mfilename);
                Report.ExportReportFile(DBS, rfilepath,mfilepath, debug, FieldSlidesStr);

                //1. create Config.xml accroding to saved Report
                string filename = "ccd_rg_config_" + Report.RConfig.table + "_" + nowStr + ".xml";
                string cfilepath = Path.Combine(dirpath, filename);
                XMLReportHandler XMLer = new XMLReportHandler();
                XMLer.CreateRGConfigXML(cfilepath, Report.RConfig, FieldSlidesStr,Report.useMarginLimits);

                //2. Limits.xlsx
                string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptyExcel"];
                string excelFile = Path.Combine(dirpath, "Limits_" + Report.RConfig.table + "_" + nowStr + ".xlsx");
                System.IO.File.Copy(EmptyExcel, excelFile, true);
                Report.buildLimitsExcel(excelFile, FieldSlidesStr);
                //3. ticket.xml
                string tfilename = "ticket_" + Report.RConfig.table + "_" + nowStr + ".xml";
                XMLer.CreateTicketXML(Path.Combine(dirpath, tfilename), excelFile, cfilepath, Report.Name, Report.RConfig);
                //4.put ticket on watched dir or activate client/server API (?)
                System.IO.File.Copy(Path.Combine(dirpath, tfilename), Path.Combine(ServerDir, "ticketsToRun", tfilename));
                //5. return to check page(?)
                UBT.result = dirpath;
                MDBUserBGTask.SaveUserBGTask(UBT);

                ////For testing on localhost (without service)
                //UBT.status = "DEBUG stopped";
                //MDBUserBGTask.SaveUserBGTask(UBT);
                //return;

                //new - use Panda API to determine if report finished
                string[] resultsDir = System.IO.Directory.GetDirectories(Path.Combine(dirpath, "Results"));
                int tries = 0;
                while ((tries < 150) && (resultsDir.Length==0))
                {
                    Thread.Sleep(2000);
                    tries++;
                    resultsDir = System.IO.Directory.GetDirectories(Path.Combine(dirpath, "Results"));
                }
                
                tries = 0;
                PandaQuery pquery = new PandaQuery();


                //while ((tries < 40) && (!UBT.checkIfReportCreated()))
                while ((tries < 150) && (resultsDir.Length > 0) && (needToWait))
                {

                    status = pquery.GetJobStatus(Path.GetFileName(resultsDir[0]));
                    Console.WriteLine(status.ToString());
                    if ((status == PandaUtilities.JobStatus.PROCESSING) || (status == PandaUtilities.JobStatus.SUBMITTED)
                        || (status == PandaUtilities.JobStatus.WAITING))
                    {
                        needToWait = true;
                    }
                    else
                    {
                        if (status != PandaUtilities.JobStatus.DONE) errMassage = " Panda status:" + status.ToString();
                        needToWait = false;
                    }
                    Thread.Sleep(2000);
                    tries++;
                }
                if (resultsDir.Length==0) throw (new Exception("Panda has not accept job"));
                //Report.ResultsPath = dirpath;
                //MDBReport.SaveUserTableReport(Report);
            }
            catch (Exception ex)
            {
                UBT.status = "Error:" + ex.Message;
                UBT.result = "";
                MDBUserBGTask.SaveUserBGTask(UBT);
                return;
            }

            UBT.status = "Done";
            if (needToWait) UBT.status = "Done Timeout; Panda status:" + status.ToString();
            if (!String.IsNullOrEmpty(errMassage)) UBT.status += " " + errMassage;
            UBT.result = dirpath;//link is built in presentation layer
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        public void RunAllIndicators(string nowStr, string key,string proj, string phase, string _interface, string mails)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            IndicatorDisplayResult DispResult = new IndicatorDisplayResult();
            IndicatorHandler IndHAndler = new IndicatorHandler();
            ExcelHandler ExclHandler = new ExcelHandler();

            List<Indicator> IndicatorsListTmp = new MDBIndicator().ReadProjectPhaseIndicators(proj, phase);
            List<Indicator> IndicatorsList = new List<Indicator>();
            // New: _interface
            if ((_interface == "ALL") || (_interface == "")) IndicatorsList = IndicatorsListTmp;
            else
            {
                foreach (Indicator Ind in IndicatorsListTmp)
                {
                    if (Ind._id.ToUpper().Contains(_interface.ToUpper())) IndicatorsList.Add(Ind);
                    else
                    {
                        if ((_interface == "CIO") && ((Ind._id.Contains("TBT3")) || (Ind._id.Contains("TBT4")))) IndicatorsList.Add(Ind);
                    }
                }
            }
            ////
            string EmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorsAllTemplateNew_" + proj + "_" + phase + ".xlsx");
            if (!System.IO.File.Exists(EmptyExcel)) EmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorsAllTemplateNew.xlsx");
            string filename = "Indicators_official_" + proj + "_" + phase + "_" + _interface + "_" + nowStr + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(EmptyExcel, filepath, true);

            ExclHandler.PrepareIndicatorsAllFile(filepath, DispResult);

            foreach (Indicator ind in IndicatorsList)
            {
                DispResult = IndHAndler.LaunchIndicator(ind._id,true);
                ExclHandler.writeIndicatorSheet(filepath, "Results", DispResult);
                //depricated - single detaile Excel
                ////New: create single detaile Excel for each table
                //string SEmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorSingleTemplate.xlsx");
                //string Sfilename = "Indicator_official_" + ind._id + "_" + nowStr + ".xlsx";
                //string Sfilepath = Path.Combine(ServerDir, "tmp", Sfilename);
                //System.IO.File.Copy(SEmptyExcel, Sfilepath, true);
                //ExclHandler.writeIndicatorSheet(Sfilepath, "Results", DispResult);
                //ExclHandler.writeSingleIndicatorDetailsSheet(Sfilepath, "Results", DispResult);
                ////
            }

            ExclHandler.ClearFutureWWIndicatorsAllFile(filepath, DispResult);

            string filename1 = "Indicators_acc_" + proj + "_" + phase + "_" + nowStr + ".xlsx";
            string filepath1 = Path.Combine(ServerDir, "tmp", filename1);
            if ((_interface == "ALL") || (_interface == ""))
            {
                System.IO.File.Copy(EmptyExcel, filepath1, true);

                ExclHandler.PrepareIndicatorsAllFile(filepath1, DispResult);

                foreach (Indicator ind in IndicatorsList)
                {
                    DispResult = IndHAndler.LaunchIndicator(ind._id, false);
                    ExclHandler.writeIndicatorSheet(filepath1, "Results", DispResult);
                    //depricated - single detaile Excel
                    ////New: create single detaile Excel for each table
                    //string SEmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorSingleTemplate.xlsx");
                    //string Sfilename = "Indicator_acc_" + ind._id + "_" + nowStr + ".xlsx";
                    //string Sfilepath = Path.Combine(ServerDir, "tmp", Sfilename);
                    //System.IO.File.Copy(SEmptyExcel, Sfilepath, true);
                    //ExclHandler.writeIndicatorSheet(Sfilepath, "Results", DispResult);
                    //ExclHandler.writeSingleIndicatorDetailsSheet(Sfilepath, "Results", DispResult);
                    ////
                }
            }

            if (System.IO.File.Exists(filepath))
            {
                UBT.status = "Done";
                UBT.result = filepath;
            }
            else
            {
                UBT.status = "Error";
            }
            MDBUserBGTask.SaveUserBGTask(UBT);
            //mail
            string Host = System.Configuration.ConfigurationManager.AppSettings["mailHost"];
            string sender = System.Configuration.ConfigurationManager.AppSettings["mailSender"];
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            //client.Host = "iil-out.intel.com";
            client.Host = Host;

            foreach (string address in mails.Split(','))
            {
                if (!string.IsNullOrEmpty(address))
                {          
                    //mail.To.Add(new MailAddress(address));
                    MailMessage mail = new MailMessage(sender,address);
                    mail.Subject = "Weekly indicators: " + CCDUtilities.getCurWeek();
                    mail.Body = "Weekly indicators: " + CCDUtilities.getCurWeek();
                    mail.Attachments.Add(new Attachment(filepath));
                    if ((_interface == "ALL") || (_interface == "")) mail.Attachments.Add(new Attachment(filepath1));
                    client.Send(mail);
                }
            }
        }


        public void ExportReportFileBG(DBsearch DBS, string nowStr, string key, string sheets, string fields, string TestNameField, string template)
        {

            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            string errMassage = "";
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptyExcel"];

            Dictionary<string, List<BsonDocument>> dictionary =
                    new Dictionary<string, List<BsonDocument>>();
            Dictionary<string, HashSet<string>> dictionaryFields = new Dictionary<string, HashSet<string>>();

            List<string> PNA_FileNames = new List<string>();

            ExcelHandler ExlExport = new ExcelHandler();

            DBS.Skip = 0;
            DBS.Sort = new SortByBuilder();
            DBS.SortStr = "TestEnd";
            if (fields == "all")
            {
                DBS.FieldsStr = "";
            }

            db.genericFind(DBS, 0);

            string filename = "ccd_report_" + DBS.col + "_" + nowStr + ".xlsx";

            //New: DV_Analysis
            if (template == "DV_TxFFE_exportRepTmpl")
            {
                sheets = "DV_TxFFE";
            }
            //New: Patch for Vitaly
            if ((template == "CIO_RX_exportRepTmpl") || (template == "DV_TxFFE_exportRepTmpl"))
            {
                BsonDocument doc = null;
                foreach (BsonDocument item in DBS.ResCursor) { doc = item; break; }
                //ccd_report_CIO_Rounded_Gen3_Rx_{Active|Passive}Host_Skew_Unitid_BoardNumber_Date.xlsx
                //"CIO_Rounded_Gen3_Rx_Active_PC084_FF025_Short125_0208"
                //Build desired name from first result
                string _TestName = doc.GetValue("SpecTestName", "").ToString();
                if (_TestName.ToUpper().Contains("ACTIVE")) _TestName = "Active";
                if (_TestName.ToUpper().Contains("PASSIVE")) _TestName = "Passive";
                string _Host = doc.GetValue("HostName", "").ToString().ToUpper();
                _Host = _Host.Replace("CCD", "");
                _Host = _Host.Replace("-IT", "");
                string _Skew = doc.GetValue("Skew", "").ToString();
                string _UnitId = doc.GetValue("UnitId", "").ToString();
                string _BoardNumber = doc.GetValue("BoardNumber", "").ToString();
                filename = DBS.col + "_" + _TestName + "_" + _Host + "_" + _Skew + _UnitId + "_" + _BoardNumber + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";
            }
            if ((template.Contains("CIO")) && (template.Contains("JTOL_Margin")))
            {
                filename = "ccd_report_CIO_JTOL_Margin_" + nowStr + ".xlsx";
            }
            //
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            if (string.IsNullOrEmpty(template))
            {
                System.IO.File.Copy(EmptyExcel, filepath, true);
            }
            else
            {
                System.IO.File.Copy(Path.Combine(ServerDir, "EVData", template + ".xlsx"), filepath, true);
            }
            if (sheets == "tests")
            {
                try
                {
                    foreach (BsonDocument item in DBS.ResCursor)
                    {
                        string TestName = item.GetValue(TestNameField, "noTestName").ToString();
                        if (!dictionary.ContainsKey(TestName))
                        {
                            dictionary.Add(TestName, new List<BsonDocument>());
                            dictionaryFields.Add(TestName, new HashSet<string>());
                        }
                        dictionary[TestName].Add(item);
                        //fields
                        foreach (BsonElement field in item)
                        {
                            if ((field.Name.Length > 6) && (field.Name.Substring(0, 6) == "HIDDEN")) continue;
                            else dictionaryFields[TestName].Add(field.Name);
                        }
                    }
                }
                catch
                {
                    UBT.status = "Error";
                    UBT.result = "ERROR: cannot export query:'" + DBS.Query + "' results'";
                    MDBUserBGTask.SaveUserBGTask(UBT);
                    return;
                }

                if (dictionary.Count > 100)
                {
                    UBT.status = "Error";
                    UBT.result = "ERROR: cannot exceed 100 sheets limit, choose another separator then:\"" + TestNameField + "\" or refine query";
                    MDBUserBGTask.SaveUserBGTask(UBT);
                    return ;

                }
                foreach (var pair in dictionary) ExlExport.writeSheet(filepath, pair.Key, pair.Value, dictionaryFields[pair.Key].ToList<string>());

            }

            if (sheets == "one")
            {
                List<BsonDocument> results = new List<BsonDocument>();
                HashSet<string> AllFields = new HashSet<string>();
                try
                {
                    foreach (BsonDocument item in DBS.ResCursor)
                    {
                        if (string.IsNullOrEmpty(template))
                        {
                            results.Add(item);
                            foreach (BsonElement field in item) AllFields.Add(field.Name);
                        }
                        else
                        {
                            //New: for RL
                            if ((template.StartsWith("CIO_RL_exportRepTmpl")) || (template.StartsWith("PCIe_RL_exportRepTmpl")) || (template.StartsWith("DP_RL_exportRepTmpl")))
                            {
                                if (!string.IsNullOrEmpty(item.GetValue("FILE_PNA_Result", "").ToString()))
                                {
                                    bool createSpec = true;
                                    string PNA_FileNoLane = Regex.Replace(Path.GetFileNameWithoutExtension(item.GetValue("FILE_PNA_Result", "").ToString()), @"L\d", "");
                                    if (PNA_FileNames.Contains(PNA_FileNoLane)) createSpec = false;
                                    else PNA_FileNames.Add(PNA_FileNoLane);

                                    //parse CSV and create BsonDocument for each line
                                    results.AddRange(ExlExport.parsePNA_CSV(template, item.GetValue("FILE_PNA_Result", "").ToString(), item, createSpec));
                                }
                            }
                            else
                                if (!item.GetValue("_id", "").ToString().ToUpper().StartsWith("RESET")) results.Add(item);
                        }
                    }
                }
                catch (Exception e)
                {
                    UBT.status = "Error";
                    UBT.result = "ERROR: cannot export query:'" + DBS.Query + "' results'";
                    MDBUserBGTask.SaveUserBGTask(UBT);
                    return;
                }
                if (string.IsNullOrEmpty(template))
                {
                    ExlExport.writeSheet(filepath, "Results", results, DBS.Fields.ToList<string>());
                }
                else
                {
                    string sheetName = "Results";
                    int fieldsLine = 2;

                    if ((template.StartsWith("CIO_RL_exportRepTmpl")) || (template.StartsWith("PCIe_RL_exportRepTmpl")) || (template.StartsWith("DP_RL_exportRepTmpl")))
                    { sheetName = "Data"; fieldsLine = 1; }

                    if ((template.Contains("CIO")) && (template.Contains("JTOL_Margin")))
                    { 
                        sheetName = "Results_from_DB"; fieldsLine = 1;
                        results = new TestModeMarginal_ReportHandler().CIOReport(DBS);
                    }

                    ExlExport.writeFieldsHeaderTmplSheet(filepath, sheetName, results, fieldsLine);

                    //New graph range update
                    if (template.Contains("Graph"))
                    {
                        ExcelChartsHandler ExlChartHandler = new ExcelChartsHandler();
                        ExlChartHandler.updateGraphRange(filepath, template, results.Count + fieldsLine);
                    }
                    ////
                    ////New pivot range update
                    //if (template.Contains("Pivot"))
                    //{
                    //    ExcelChartsHandler ExlChartHandler = new ExcelChartsHandler();
                    //    if (((template.Contains("CIO")) && (template.Contains("Margin"))) || ((template.Contains("TBT")) && (template.Contains("Margin"))))
                    //    {
                    //        ExlChartHandler.updatePivothRange(filepath, "CIO_RL_Tables", "Results from DB", results.Count + fieldsLine);
                    //    }
                        
                    //}
                    ////

                }

            }

            ////////////////////////////
            //  New: DV_TxFFE
            ///////////////////
            if (sheets == "DV_TxFFE")
            {
                List<string> DVFields = new List<string> { "Post", "Pre", "Preshoot", "Deemphasis", "Preset", "Corner", "Port", "Lane", "Place", "Key" };
                try
                {
                    foreach (BsonDocument item in DBS.ResCursor)
                    {
                        if (!string.IsNullOrEmpty(item.GetValue("FILE_DV_TxFFE", "").ToString()))
                        {
                            string resultFileName = item.GetValue("FILE_DV_TxFFE", "").ToString();
                            if (resultFileName.EndsWith("xls"))
                            {
                                bool errorOccurred, success;
                                int errorCode;
                                String errorMsg;
                                if (ExlExport.convertXlsCSVToXlsx(resultFileName))
                                {
                                    string[,] values = new string[1, 2] { { "FILE_DV_TxFFE", resultFileName + "x" } };
                                    success = db.dbupdate(item.GetValue("_id").ToString(), values, out errorOccurred, out errorCode, out errorMsg);
                                    item.Set("FILE_DV_TxFFE", resultFileName + "x");
                                }
                                //TODO: error handling
                            }
                            //TODO: error handling
                            Dictionary<string, List<BsonDocument>> dictionaryTmp = ExlExport.DV_Analyse_accumulate(dictionary, item);
                            dictionary = dictionaryTmp;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UBT.status = "Error";
                    UBT.result = "ERROR: cannot export query:'" + DBS.Query + "' results' exception:" + ex.Message;
                    MDBUserBGTask.SaveUserBGTask(UBT);
                    return;
                }

                foreach (var pair in dictionary) ExlExport.writeSheet(filepath, pair.Key, pair.Value, DVFields);

                //TODO: 2nd phase analasyis per each sheet
                (new DV_AnalysisHandler()).DV_Analyse_Summary(filepath);


            }
            ///////////////////////////////

            UBT.status = "Done";
            if (!String.IsNullOrEmpty(errMassage)) UBT.status += " " + errMassage;
            UBT.result = filepath;//link?
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        public void ExecCycleReportBG(string nowstr, string key, string protocol, string proj, string phase, string CSR_DB, string table, string FilterSessions)
        {
            bool success = false;
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);
            string msg = "";
            string filepath = "";
            SessionsManageDisplay SMDisp = new SessionManageHandler().GetSessionsManageDisplay(protocol, proj, phase, FilterSessions);
            if (SMDisp.Conf.tables.Count == 0)
            {
                UBT.status = "Error";
                UBT.result = "ERROR: No configuration is defined for: " + protocol + " " + proj + " " + phase; ;
                MDBUserBGTask.SaveUserBGTask(UBT);
                return;
            }
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            
            if (SMDisp.Conf.useNewCSR == "Y")
            {
                bool wasCSRLookUpUpdated = CPH.UpdateCyclePlanCSRLookUP();
                CycleStatusHandlerNew cycleStatusHandler = new CycleStatusHandlerNew();
                success = cycleStatusHandler.CycleStatusReportExec(SMDisp, out msg, out filepath, CSR_DB, table);
            }
            else
            {
                CycleStatusHandler cycleStatusHandler = new CycleStatusHandler();
                success = cycleStatusHandler.CycleStatusReportExec(SMDisp, out msg, out filepath, CSR_DB, table);
            }

            UBT.status = "Done";
            if ((!success) || (!String.IsNullOrEmpty(msg))) UBT.status = "Error: " + msg;
            else UBT.result = filepath;//link?
            MDBUserBGTask.SaveUserBGTask(UBT);
        }


        public void ExportVisualizationFileBG(DBsearch DBS, string nowStr, string key, string group, string visFields)
        {
            List<string> txtList = new List<string>();
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            string errMassage = "";
            bool success = true;
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptyExcel"];

            Dictionary<string, List<BsonDocument>> dictionary =
                    new Dictionary<string, List<BsonDocument>>();
            Dictionary<string, HashSet<string>> dictionaryFields = new Dictionary<string, HashSet<string>>();

            ExcelHandler ExlExport = new ExcelHandler();

            DBS.Skip = 0;
            DBS.Sort = new SortByBuilder();
            DBS.SortStr = "TestEnd";
            db.genericFind(DBS, 0);

            string filename = "ccd_visualization_" + DBS.col + "_" + nowStr + ".csv";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            txtList.Add("group," + visFields);
            File.AppendAllLines(filepath, txtList);
            List<string> fields = new List<string>(visFields.Split(','));
            foreach (BsonDocument doc in DBS.ResCursor)
            {
                //TODO: error handling (if value is missing o not numeric)
                txtList.Clear();
                txtList.Add(doc.GetValue(group, "").ToString() + ",");
                foreach (string f in fields) txtList[0]+=(doc.GetValue(f, "").ToString() + ",");
                txtList[0] = txtList[0].TrimEnd(',');
                File.AppendAllLines(filepath, txtList);
            }

            UBT.status = "Done";
            if ((!success) || (!String.IsNullOrEmpty(errMassage))) UBT.status = "Error: " + errMassage;
            else UBT.result = filepath;//link?
            MDBUserBGTask.SaveUserBGTask(UBT);

        }


        public void PocExeCalling(string key, string outFileName, string line, string exefilename)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            string errMassage = "";
            bool success = true;
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            //string filename = @"C:\tmp\ConsoleApplication1.exe";
            string cParams = '"' + Path.Combine( @"C:\", "tmp", outFileName) + '"' + " " + '"' + line + '"';
            try
            {
                //var proc = System.Diagnostics.Process.Start(exefilename, cParams);
                ProcessStartInfo info = new ProcessStartInfo(exefilename, cParams);
                info.UseShellExecute = false;
                info.RedirectStandardError = true;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.CreateNoWindow = true;
                info.ErrorDialog = false;
                info.WindowStyle = ProcessWindowStyle.Hidden;

                Process process = Process.Start(info);
                process.WaitForExit();
            }
            catch (Exception e)
            {
                errMassage = e.Message;
                success = false;
            }

            UBT.status = "Done";
            if ((!success) || (!String.IsNullOrEmpty(errMassage))) UBT.status = "Error: " + errMassage;
            else UBT.result = "Done";//link?
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        public void ExecRunCSRReportsBG(string nowStr, string username, string key, string ids_str, string Protocol, string proj, string phase, string table_div, string mails, string Query)
        {
            List<string> reportsIds = new List<string>(ids_str.Split(','));
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            string errMassage = "";
            bool success = true;
            UBT.status = "In progress 0/" + reportsIds.Count.ToString();
            MDBUserBGTask.SaveUserBGTask(UBT);


            //////////////////////////////////////////////////
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            string mailMessage = "";
            int cnt = 0;
            foreach (string _id in new List<string>(ids_str.Split(',')))
            {
                UserTableReport Report = MDBReport.ReadUserTableReport(_id);
                mailMessage += "Launching report: \"" + Report.Name + "\"";
                DBsearch DBS = new DBsearch();
                string displayDB = "PRM";
                DBS.db = DB_PRM;
                DBS.col = Report.RConfig.table;
                DBS.Query = Query;


                //Report Launch validation
                List<BsonDocument> MissedResults = Report.LaunchValidate(DBS.Query, displayDB, "");
                if (MissedResults.Count > 0)
                {
                    errMassage += " could not launch report: \"" + Report.Name + "\" cause data is missing.";
                    mailMessage += " could not launch report: \"" + Report.Name + "\" cause data is missing.";
                    //foreach (BsonDocument doc in MissedResults)
                    //{
                    //    errMassage += "\"" + doc.GetValue("TestName", "").ToString() + "-" + doc.GetValue("Field", "") + "\" ; ";
                    //}
                    continue;
                }

                //Save query in report as last launched report, do we want it?
                //Report.Search = new DBsearchSave(DBS);
                //MDBReport.SaveUserTableReport(Report);

                string nowStr2 = DateTime.Now.ToString("yyyyMMddHHmmss");
                string key2 = username + nowStr2;
                MDBUserBGTask MDBUserBGTaskRec = new MDBUserBGTask();

                string ExcelDebug = "N";

                BsonDocument jmap = new BsonDocument();
                jmap.Add("DBSquery", DBS.Query);
                jmap.Add("DBSdb", DBS.db);
                jmap.Add("DBScol", DBS.col);
                jmap.Add("DBSsortStr", DBS.SortStr);
                jmap.Add("DBSsortDirection", DBS.SortDirection);
                jmap.Add("nowStr", nowStr2);
                jmap.Add("key", key2);
                jmap.Add("debug", ExcelDebug);
                jmap.Add("type", "2");
                jmap.Add("FieldSlidesStr", "");

                //MDBUserBGTaskRec.SaveUserBGTask(new UserBGTask(key2, "report_" + Report.Name + " " + "CSR launch" + " " + nowStr2, Report._id, jmap));//unmark and comment from here

                //Already in BG, so handling it directly and waiting:
                UserBGTask _task = new UserBGTask(key2, "report_" + Report.Name + " " + "(CSR launch)" + " " + nowStr2, Report._id, jmap);
                _task.status = "BG Launched";

                MDBUserBGTaskRec.SaveUserBGTask(_task);

                JobWorker worker = new JobWorker();
                worker.ExecRunReportBG(Report._id, DBS, nowStr, _task.id, ExcelDebug, "");
                UserBGTask UBTtmp = MDBUserBGTask.ReadUserBGTasks(key2);
                mailMessage += " Finished. Status = " + UBTtmp.status + " \n";
                cnt++;
                UBT.status = "In progress 0/" + cnt;
                MDBUserBGTask.SaveUserBGTask(UBT);
            }
            ///////////////////////////////////////////////////

            UBT.status = "Done";
            if ((!success) || (!String.IsNullOrEmpty(errMassage))) UBT.status = "Error: " + errMassage;
            else UBT.result = "";// 7/7?
            MDBUserBGTask.SaveUserBGTask(UBT);

            //mail
            string Host = System.Configuration.ConfigurationManager.AppSettings["mailHost"];
            string sender = System.Configuration.ConfigurationManager.AppSettings["mailSender"];
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            //client.Host = "iil-out.intel.com";
            client.Host = Host;

            foreach (string address in mails.Split(','))
            {
                if (!string.IsNullOrEmpty(address))
                {
                    //mail.To.Add(new MailAddress(address));
                    MailMessage mail = new MailMessage(sender, address);
                    mail.Subject = UBT.desc ;
                    mail.Body = mailMessage;
                    //mail.Attachments.Add(new Attachment(filepath));
                    //mail.Attachments.Add(new Attachment(filepath1));
                    client.Send(mail);
                }
            }

        }

        //SessionsManage tool
        public void SessionsManageListWorker(string key, string proj, string phase)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            SessionManageHandler SMH = new SessionManageHandler();
            foreach (string protocol in (new List<string>(System.Configuration.ConfigurationManager.AppSettings["Protocols"].Split(','))))
            {
                SessionsManageDisplay SMDisp = SMH.GetSessionsManageDisplay(protocol, proj, phase,"ALL");
                if (SMDisp.Conf.RebuildFieldsData == "Y")
                {
                    SMH.RebuildFieldsDataOfProtocolConf(SMDisp.Conf);
                }
            }

            UBT.status = "Done";
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        //HotCornerStats tool
        public void HotCornerStatsWorker(string key)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            HotCornerStatsHandler HCSH = new HotCornerStatsHandler();
            string message = "";
            HCSH.LoadCornerSetTimesFromExecutions(out message);
            UBT.status = "Done";
            UBT.result = message;
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        //TestModeMarginReport
        public void TestModeMarginReportWorker(string key,string _interface, string db_str, string project, string phase, string nowStr)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            TestModeMarginal_ReportHandler_Depricated reportHandler = new TestModeMarginal_ReportHandler_Depricated();
            string message = "";
            string filepath = reportHandler.GenerateReport(_interface, db_str, project, phase, nowStr);
            UBT.status = "Done";
            UBT.result = message;
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        //BoardsInventory tool 
        public void BoardsInventoryWorker(string key, string proj, string phase)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);
            MDBBoard db = new MDBBoard();
            db.MDBFind("EQ;Project;" + proj + ";EQ;Phase;" + phase + ";Matches;Name;/^./", "AND", "DES", "_id");

            UBT.status = "Done";
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        public void ExportReportFileCellQueriesBG(DBsearch DBS, string nowStr, string key, string sheets, string fields, string TestNameField, string template)
        {

            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            string errMassage = "";
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptyExcel"];

            Dictionary<string, List<BsonDocument>> dictionary =
                    new Dictionary<string, List<BsonDocument>>();
            Dictionary<string, HashSet<string>> dictionaryFields = new Dictionary<string, HashSet<string>>();

            ExcelHandler ExlExport = new ExcelHandler();

            DBS.Skip = 0;
            DBS.Sort = new SortByBuilder();
            DBS.SortStr = "TestEnd";
            if (fields == "all")
            {
                DBS.FieldsStr = "";
            }

            db.genericFind(DBS, 0);

            string filename = "ccd_report_" + DBS.col + "_" + nowStr + ".xlsx";
            if (template == "Power_Consumption_1UnitSummary_exportRepCellQueryTmpl")
            {
                BsonDocument doc = null;
                foreach (BsonDocument item in DBS.ResCursor) { doc = item; break; }
                string _UnitId = doc.GetValue("UnitId", "").ToString();
                filename = "Power_Consumption_1UnitSummary_" + _UnitId + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";
            }

            if (template == "SVR_Efficiency_Summary_exportRepCellQueryTmpl")
            {
                BsonDocument doc = null;
                foreach (BsonDocument item in DBS.ResCursor) { doc = item; break; }
                string _UnitId = doc.GetValue("UnitId", "").ToString();
                string _BoardId = doc.GetValue("BoardNumber", "").ToString();
                string _Skew = doc.GetValue("Skew", "").ToString();
                filename = "SVR_Efficiency_" +_Skew + "_Board_"+ _BoardId + "_UNIT_" + _UnitId + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";
            }

            string filepath = Path.Combine(ServerDir, "tmp", filename);
            if (string.IsNullOrEmpty(template))
            {
                System.IO.File.Copy(EmptyExcel, filepath, true);
            }
            else
            {
                System.IO.File.Copy(Path.Combine(ServerDir, "EVData", template + ".xlsx"), filepath, true);
            }
            //copied from old export. TODO - in case of some sheets, need to produce from current results set (?) list of base queries, unit (?) skew(?)
            //TODO; add configuration/pararmeters (XML, in Excel template, parameters from web) that may help to define this
            //if (sheets == "tests")
            //{
            //    try
            //    {
            //        foreach (BsonDocument item in DBS.ResCursor)
            //        {
            //            string TestName = item.GetValue(TestNameField, "noTestName").ToString();
            //            if (!dictionary.ContainsKey(TestName))
            //            {
            //                dictionary.Add(TestName, new List<BsonDocument>());
            //                dictionaryFields.Add(TestName, new HashSet<string>());
            //            }
            //            dictionary[TestName].Add(item);
            //            //fields
            //            foreach (BsonElement field in item)
            //            {
            //                if ((field.Name.Length > 6) && (field.Name.Substring(0, 6) == "HIDDEN")) continue;
            //                else dictionaryFields[TestName].Add(field.Name);
            //            }
            //        }
            //    }
            //    catch
            //    {
            //        UBT.status = "Error";
            //        UBT.result = "ERROR: cannot export query:'" + DBS.Query + "' results'";
            //        MDBUserBGTask.SaveUserBGTask(UBT);
            //        return;
            //    }

            //    if (dictionary.Count > 100)
            //    {
            //        UBT.status = "Error";
            //        UBT.result = "ERROR: cannot exceed 100 sheets limit, choose another separator then:\"" + TestNameField + "\" or refine query";
            //        MDBUserBGTask.SaveUserBGTask(UBT);
            //        return;

            //    }
            //    foreach (var pair in dictionary) ExlExport.writeSheet(filepath, pair.Key, pair.Value, dictionaryFields[pair.Key].ToList<string>());

            //}

            if (sheets == "one")
            {
                string emptySheetName = "";
                List<string> sheetNames = new List<string>();
                sheetNames.Add(emptySheetName);
                if (template == "Power_SVR_code2values_10_ALL_units_exportRepCellQueryTmpl_PreProcess")
                {
                    sheetNames = ExlExport.getSheetNames(filepath);
                }
                if (template.Contains("PreProcess"))
                {
                    foreach (string sheetName in sheetNames) ExlExport.writeSheetLoadCellQueriesPreProcess(filepath, sheetName, DBS.Query, DBS.col, DBS.db, template);
                }

                foreach (string sheetName in sheetNames) ExlExport.writeSheetLoadCellQueries(filepath, sheetName, DBS.Query, DBS.col, DBS.db);
            }


            UBT.status = "Done";
            if (!String.IsNullOrEmpty(errMassage)) UBT.status += " " + errMassage;
            UBT.result = filepath;//link?
            MDBUserBGTask.SaveUserBGTask(UBT);
        }


        public void CreateSpecLimitLookup(string key, string table, string project, string phase, string LookUPTableName)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);
            MDBIndicator MDBIndicatorObj = new MDBIndicator();
            MDBADMReferenceTable MDBRef = new MDBADMReferenceTable();
            MDBSpecLimitLookUP MDBSpecLimitLookUPObj = new MDBSpecLimitLookUP(LookUPTableName);
            Indicator Ind = MDBIndicatorObj.ReadIndicator(table, project, phase);
            ADMReferenceTable specTable = MDBRef.ReadADMReferenceTable(Ind.spec);
            SpecLimitsConflictsHandler SLCHandler = new SpecLimitsConflictsHandler(Path.Combine(ServerDir, "EVData", "SpecLimitsConflictsIgnore" + ".xml"));
            foreach (FieldSlide slide in specTable.RConfig.FieldsSlides)
            {
                if (slide.type == General.ReportSlideType.limits.ToString())
                {
                    //fields: usl,lsl,actualfield name, spec id, [WW end (wws + 7*wn)]no need anymore...
                    BsonDocument Doc = new BsonDocument { { "_id", table + "_" + project + "_" + phase + "_" + slide.test } };
                    Doc.Add("usl", slide.usl);
                    Doc.Add("lsl", slide.lsl);
                    Doc.Add("field", slide.column);
                    if (SLCHandler.SkipSpecTestNameCheck(Ind.spec, slide.test)) Doc.Add("skip", "Y");
                    MDBSpecLimitLookUPObj.Save(Doc);
                }
            }

            UBT.status = "Done";
            MDBUserBGTask.SaveUserBGTask(UBT);

        }

        public void CheckSpecTestNameLimitViolation(string key, string table, string project, string phase)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);
            SpecLimitsConflictsHandler SLCHandler = new SpecLimitsConflictsHandler(Path.Combine(ServerDir, "EVData", "SpecLimitsConflictsIgnore" + ".xml"));
            string message = "";
            string success = "";
            success = SLCHandler.CheckSpecTestNameLimitViolation(table, project, phase,null, out message);
            if (success.ToUpper() == "ERROR") UBT.status = "Error";
            else UBT.status = "Done";
            UBT.result = message;
            MDBUserBGTask.SaveUserBGTask(UBT);
            return;
        }

        public void LoadLanFile(string key, string FilePath, string table, string project, string phase)
        {
            string Message = "";
            string errMessage = "";
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);
            FXVLLoader Floader = new FXVLLoader();
            if (Floader.LoadLANFile(out errMessage, out Message, FilePath, project, phase, table))
            {
                UBT.status = "Done";
                UBT.result = "File was loaded succesfully " + Message;
            }
            else UBT.status = "Error: cannot load Excel file " + errMessage;

            MDBUserBGTask.SaveUserBGTask(UBT);
            return;
        }

        public void RunLiveIndicators(string nowStr, string key, string proj, string phase)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);

            IndicatorDisplayResult DispResultPRM = new IndicatorDisplayResult();
            IndicatorDisplayResult DispResultTMP = new IndicatorDisplayResult();
            IndicatorDisplayResult DispResult = new IndicatorDisplayResult();
            IndicatorHandler IndHAndler = new IndicatorHandler();
            ExcelHandler ExclHandler = new ExcelHandler();

            List<Indicator> IndicatorsList = new MDBIndicator().ReadProjectPhaseIndicators(proj, phase);
            string EmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorsAllTemplateNew_" + proj + "_" + phase + ".xlsx");
            if (!System.IO.File.Exists(EmptyExcel)) EmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorsAllTemplateNew.xlsx");
            string filename = "Indicators_official_" + proj + "_" + phase + "_live_" + nowStr + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(EmptyExcel, filepath, true);

            ExclHandler.PrepareIndicatorsAllFile(filepath, DispResult);

            foreach (Indicator ind in IndicatorsList)
            {
                DispResultPRM = IndHAndler.LaunchIndicator(ind._id, true);
                DispResultTMP = IndHAndler.LaunchIndicator(ind._id, true, false, "TMP");
                if (DispResultTMP.Res.weeks.Count > 0) DispResult = IndHAndler.MergeDispResults(ind._id, DispResultPRM, DispResultTMP);
                else DispResult = DispResultPRM;
                ExclHandler.writeIndicatorSheet(filepath, "Results", DispResult);
            }

            ExclHandler.ClearFutureWWIndicatorsAllFile(filepath, DispResult);
            ExcelChartsHandler ExlChartHandler = new ExcelChartsHandler();
            string imgPath = "";
            Dictionary<string, int> graphs = new Dictionary<string, int>();
            graphs.Add("Summary by Area", 1);
            graphs.Add("Graph", 2);
            if (proj == "FXVL") graphs["Graph"] = 1;
            ExlChartHandler.exportSheetGraphToImage(filepath, graphs, "_"+proj+"_"+phase, out imgPath);
            //ExlChartHandler.exportSheetGraphToImage(filepath, "Graph", 2, "Graph_" + proj + "_" + phase, out imgPath);


            if (System.IO.File.Exists(filepath))
            {
                UBT.status = "Done";
                UBT.result = filepath;
            }
            else
            {
                UBT.status = "Error";
            }
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        public void CleanRecycleDB(string nowStr, string key)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);
            if (db.CleanRecycleDB())
            {
                UBT.status = "Done";
            }
            else
            {
                UBT.status = "Error";
            }
            MDBUserBGTask.SaveUserBGTask(UBT);
        }

        public void CheckSpecTestNameLimitViolationBatch(string key, string project, string phase)
        {
            MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
            UserBGTask UBT = MDBUserBGTask.ReadUserBGTasks(key);
            UBT.status = "In progress";
            MDBUserBGTask.SaveUserBGTask(UBT);
            SpecLimitsConflictsHandler SLCHandler = new SpecLimitsConflictsHandler(Path.Combine(ServerDir, "EVData", "SpecLimitsConflictsIgnore" + ".xml"));
            string message = "";
            string success = "";
            MDBIndicator MDBIndicatorObj = new MDBIndicator();
            //Indicator Ind = MDBIndicatorObj.ReadIndicator(table + "_" + project + "_" + phase);
            foreach (Indicator Ind in MDBIndicatorObj.ReadProjectPhaseIndicators(project, phase))
            {
                success = SLCHandler.CheckSpecTestNameLimitViolation(Ind.table, project, phase, UBT.jmap.GetValue("prevLaunch","0").ToString(), out message);
                if (success.ToUpper() == "ERROR") 
                    {
                        UBT.status = "Error";
                        UBT.result +=" " + message;
                }
                else if (UBT.status!= "Error") UBT.status = "Done";
            }
            MDBUserBGTask.SaveUserBGTask(UBT);
            return;
        }
    }
}