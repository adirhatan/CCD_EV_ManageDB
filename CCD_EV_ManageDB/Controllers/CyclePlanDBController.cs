using System;
using System.Collections.Generic;
using System.Linq;
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

namespace CCD_EV_ManageDB.Controllers
{
    public class CyclePlanDBController : Controller
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

        private static string DefaultProtocol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultProtocol"]; } }
        //
        // GET: /ACP/

        public ActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult Main(string protocol, string proj, string phase)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            ViewBag.collist = new List<string>(MongoTables);
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;


            return View();
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult DefineMetaData(string protocol, string proj, string phase)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            List<string> tablelist = CPH.filterTablesPerProtocol(new List<string>(MongoTables)); 
            tablelist.Insert(0, "");
            ViewBag.collist = tablelist;
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;
            List<string> speclist = CPH.filterTablesPerProtocol((new MDBADMReferenceTable()).ReadListAllADMReferenceTablesIDS()); 
            speclist.Insert(0, "");
            ViewBag.speclist = speclist;

            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            if (CPMetaData.protocol != protocol) CPMetaData = new CyclePlanMetaData(protocol, proj, phase);//TODO add missing info: WW, WS etc

            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = DateTime.Now;
            Calendar cal = dfi.Calendar;
            int curWeek = cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            Dictionary<string, string> weeks = new Dictionary<string, string>();
            for (int weekCnt = -10; weekCnt < 20; weekCnt++)
            {
                string weekTmp = date1.AddDays(Convert.ToDouble(weekCnt * 7)).ToString("yyyyMMdd");
                weeks.Add(cal.GetWeekOfYear(date1.AddDays(Convert.ToDouble(weekCnt * 7)), dfi.CalendarWeekRule, dfi.FirstDayOfWeek).ToString(), CCDUtilities.getAdjacentSundayDate(weekTmp));
            }
            ViewBag.workweeks = weeks;
            List<string> weekNumbers = new List<string>(new string[] { "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", });
            ViewBag.weekNumbers = weekNumbers;

            CyclePlanMetaData DefCPMetaData = MDBCPMetaData.Read(DefaultProtocol, proj, phase);
            ViewBag.wn = DefCPMetaData.wn;
            ViewBag.wws = DefCPMetaData.wws;
            ViewBag.defProtocol = DefaultProtocol;

            return View(CPMetaData);
        }
        
        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult DefineMetaData_copy_config_from(string protocol, string proj, string phase, string copy_from_proj, string copy_from_phase)
        {
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData refCPMetaData = MDBCPMetaData.Read(protocol, copy_from_proj, copy_from_phase);
            if ((refCPMetaData.protocol != protocol) || (refCPMetaData.IndicatorsMeta.Count==0)) 
            {
                TempData["message"] = "Error: Cycle Plan Meta Data of:" + copy_from_proj + " " + copy_from_phase + " does not exist";
                return RedirectToAction("DefineMetaData");
            }
            CyclePlanMetaData CPMetaData = new CyclePlanMetaData(protocol, proj, phase);
            CPMetaData.IndicatorsMeta = new Dictionary<string, CPTableMetaData>(refCPMetaData.IndicatorsMeta);
            MDBCPMetaData.Save(CPMetaData);

            return RedirectToAction("DefineMetaData");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult DefineMetaDataAddTable(string protocol, string proj, string phase, string col, string spec,string BoardTypeVals, string SetupTypeVals , double resAvgTime)
        {
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol,proj,phase);
            if (CPMetaData.protocol != protocol) CPMetaData = new CyclePlanMetaData(protocol, proj, phase);//TODO add missing info: WW, WS etc
            if (CPMetaData.IndicatorsMeta.ContainsKey(col))
            {
                TempData["message"] = "Error: Cycle Plan  " + CPMetaData._id + " already contains table " + col;
                return RedirectToAction("DefineMetaData");
            }
            else CPMetaData.IndicatorsMeta.Add(col, new CPTableMetaData(spec,resAvgTime,SetupTypeVals,BoardTypeVals));
            MDBCPMetaData.Save(CPMetaData);

            return RedirectToAction("DefineMetaData");
            //return RedirectToAction("DefineMetaData", new RouteValueDictionary(new { proj = proj, phase = phase }));

        }

        [HttpPost]
        public ActionResult DefineMetaDataUpdateResAvgTime(string protocol, string proj, string phase, string col, double resAvgTime)
        {
            bool s;
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol,proj,phase);
            if (CPMetaData.protocol != protocol) Response.Write("Update Failed:" + resAvgTime.ToString());
            if (!CPMetaData.IndicatorsMeta.ContainsKey(col)) Response.Write("Update Failed:" + resAvgTime.ToString());
            CPMetaData.IndicatorsMeta[col].resAvgTime = resAvgTime;
            s= MDBCPMetaData.Save(CPMetaData);
            if (s) Response.Write(resAvgTime.ToString());
            else Response.Write("Update Failed:" + resAvgTime.ToString());
            return null;
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult DefineMetaDataDelTable(string protocol, string proj, string phase, string col)
        {
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            if (CPMetaData.protocol != protocol) CPMetaData = new CyclePlanMetaData(protocol, proj, phase);//TODO add missing info: WW, WS etc
            if (CPMetaData.IndicatorsMeta.ContainsKey(col))
            {
                //CPMetaData.IndicatorsMeta.Remove(col);
                //MDBCPMetaData.Save(CPMetaData);
                CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
                string message = "";
                if (CPH.DeleteTableInMetaData(col, out message)) TempData["messageInfo"] = "Deleting of table \"" + col + "\" is done" ;
                else TempData["message"] = "Error: an error accured during deleting of table \"" + col + "\":" + message;
                return RedirectToAction("DefineMetaData");
            }
            return RedirectToAction("DefineMetaData");
            //return RedirectToAction("DefineMetaData", new RouteValueDictionary(new { proj = proj, phase = phase }));

        }

        
        [Authorize(Roles = "CCD_EV_ADM")]
        public ActionResult DefineMetaDataDefWWS(string protocol, string proj, string phase, string wws, string wn)
        {
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol,proj,phase);
            if (CPMetaData.protocol != protocol) CPMetaData = new CyclePlanMetaData(protocol, proj, phase);
            CPMetaData.wn = wn;
            CPMetaData.wws = wws;
            MDBCPMetaData.Save(CPMetaData);

            return RedirectToAction("DefineMetaData");
            //return RedirectToAction("DefineMetaData", new RouteValueDictionary(new { proj = proj, phase = phase }));

        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CreateSpecSubSets(string protocol, string proj, string phase, string col)
        {
            //TODO: some of those beginning lines are redundent
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            List<string> tablelist = CPH.filterTablesPerProtocol(new List<string>(MongoTables)); 
            tablelist.Insert(0, "");
            ViewBag.collist = tablelist;
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            if ((CPMetaData==null) || (string.IsNullOrEmpty(CPMetaData._id)) || (CPMetaData.IndicatorsMeta.Count==0))
            {
                TempData["message"] = "Error: you must define at least one table in 'Define Meta Data' in order to create Spec Sub Sets";
                return RedirectToAction("Main");
            }
            ViewBag.IndicatorsMetalist = CPMetaData.IndicatorsMeta;
            List<string> speclist = CPH.filterTablesPerProtocol((new MDBADMReferenceTable()).ReadListAllADMReferenceTablesIDS()); 
            speclist.Insert(0, "");
            ViewBag.speclist = speclist;

            MDBCyclePlanSpecSubSet MDBCPlanSpecSubSet = new MDBCyclePlanSpecSubSet();
            List<CyclePlanSpecSubSet> CyclePlanSpecSubSets = MDBCPlanSpecSubSet.ReadProjectList(protocol, proj, phase);
            if (string.IsNullOrEmpty(col))
            {
                if (CPMetaData.IndicatorsMeta.Count > 0) col = CPMetaData.IndicatorsMeta.ElementAt(0).Key;
            }
            ViewBag.selectedSpecTestslist = CPH.GetSpecTestNamesListForTable(col);
            ViewBag.selectedCol = col;
            ViewBag.selectedSpec = CPMetaData.IndicatorsMeta[col].spec;
            return View(CyclePlanSpecSubSets);
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CreateSpecSubSets_copy_config_from(string protocol, string proj, string phase, string copy_from_proj, string copy_from_phase)
        {
            //TODO: move logic to CyclePlanHandler.cs, define Constractor of CyclePlanSpecSubSet(string protocol, string proj, string phase, CyclePlanSpecSubSet ref)
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData refCPMetaData = MDBCPMetaData.Read(protocol, copy_from_proj, copy_from_phase);
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);

            if (!CPMetaData.CompareTablesAndSpecs(refCPMetaData))
            {
                TempData["message"] = "Error: Cycle Plan Meta Data of:" + copy_from_proj + " " + copy_from_phase + " contains different tables or specs, can not copy from it";
                return RedirectToAction("CreateSpecSubSets");
            }
            MDBCyclePlanSpecSubSet MDBCPlanSpecSubSet = new MDBCyclePlanSpecSubSet();
            foreach (CyclePlanSpecSubSet CPSpecSubSet in MDBCPlanSpecSubSet.ReadProjectList(protocol, copy_from_proj, copy_from_phase))
            {
                CyclePlanSpecSubSet CPSpecSubSetNew = new CyclePlanSpecSubSet(protocol, proj, phase, CPSpecSubSet);
                MDBCPlanSpecSubSet.Save(CPSpecSubSetNew);
            }
            return RedirectToAction("CreateSpecSubSets");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CreateSpecSubSetsAddSubSet(string protocol, string proj, string phase, string col, string spec,string SubSetName, string Fields)
        {
            //CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase); see if needed
            ADMReferenceTable specRef = (new MDBADMReferenceTable().ReadADMReferenceTable(spec));
            if (string.IsNullOrEmpty(specRef._id))
            {
                TempData["message"] = "Error: could not find Spec:" + spec;
                return RedirectToAction("CreateSpecSubSets");
            }

            MDBCyclePlanSpecSubSet MDBCPlanSpecSubSet = new MDBCyclePlanSpecSubSet();
            CyclePlanSpecSubSet CPSpecSubSetNew = new CyclePlanSpecSubSet(protocol, proj, phase, col, SubSetName, spec);
            CyclePlanSpecSubSet CPSpecSubSetExist = MDBCPlanSpecSubSet.Read(protocol, proj, phase,col,SubSetName);
            if (CPSpecSubSetNew._id == CPSpecSubSetExist._id)
            {
                TempData["message"] = "Error: SpecSubSet  " + CPSpecSubSetNew._id + " already exists  ";
                return RedirectToAction("CreateSpecSubSets");

            }
            CPSpecSubSetNew.allTests = specRef.RConfig.FieldsSlides.Count.ToString();
            foreach (string SpecTestName in (new List<string>(Fields.Split(',')))) 
            {
                CPSpecSubSetNew.SpecTestNames.Add(SpecTestName);
            }
            MDBCPlanSpecSubSet.Save(CPSpecSubSetNew);
            return RedirectToAction("CreateSpecSubSets");
            //return RedirectToAction("DefineMetaData", new RouteValueDictionary(new { proj = proj, phase = phase }));

        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CreateSpecSubSetsEditSubSet(string protocol, string proj, string phase, string id)
        {
            MDBCyclePlanSpecSubSet MDBCPlanSpecSubSet = new MDBCyclePlanSpecSubSet();
            CyclePlanSpecSubSet CPSpecSubSet = MDBCPlanSpecSubSet.Read(id);
            ADMReferenceTable specRef = (new MDBADMReferenceTable().ReadADMReferenceTable(CPSpecSubSet.Spec));
            TempData["EditedSubset_id"] = CPSpecSubSet._id;
            TempData["editedSpecTestslist"] = specRef.GetSpecTestNames();
            return RedirectToAction("CreateSpecSubSets");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CreateSpecSubSetsEditExecSubSet(string id, string SubSetName, string FieldsEdit)
        {
            MDBCyclePlanSpecSubSet MDBCPlanSpecSubSet = new MDBCyclePlanSpecSubSet();
            CyclePlanSpecSubSet CPSpecSubSet = MDBCPlanSpecSubSet.Read(id);
            // create "New" for new id
            CyclePlanSpecSubSet CPSpecSubSetNew = new CyclePlanSpecSubSet(CPSpecSubSet.protocol, CPSpecSubSet.project, CPSpecSubSet.phase, CPSpecSubSet.Table, SubSetName, CPSpecSubSet.Spec);
            CPSpecSubSet._id = CPSpecSubSetNew._id;
            CPSpecSubSet.Name = SubSetName;
            CPSpecSubSet.SpecTestNames.Clear();
            foreach (string SpecTestName in (new List<string>(FieldsEdit.Split(','))))
            {
                CPSpecSubSet.SpecTestNames.Add(SpecTestName);
            }
            MDBCPlanSpecSubSet.Delete(id);
            MDBCPlanSpecSubSet.Save(CPSpecSubSet);
            CyclePlanHandler CPH = new CyclePlanHandler(CPSpecSubSet.protocol, CPSpecSubSet.project, CPSpecSubSet.phase);
            CPH.UpdateSpecSubSetIDInProjectUnits(CPSpecSubSet, id);
            return RedirectToAction("CreateSpecSubSets");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CreateSpecSubSetsDelSubSet(string id)
        {
            MDBCyclePlanSpecSubSet MDBCPlanSpecSubSet = new MDBCyclePlanSpecSubSet();
            CyclePlanSpecSubSet deletedCPSpecSubSet =  MDBCPlanSpecSubSet.Read(id);
            MDBCPlanSpecSubSet.Delete(id);
            CyclePlanHandler CPH = new CyclePlanHandler(deletedCPSpecSubSet.protocol, deletedCPSpecSubSet.project, deletedCPSpecSubSet.phase);
            CPH.UpdateDelSpecSubSetIDInProjectUnits(id, deletedCPSpecSubSet.Table);
            return RedirectToAction("CreateSpecSubSets");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CreateCoverageRules(string protocol, string proj, string phase, string col)
        {
            //TODO: some of those beginning lines are redundent
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            List<string> tablelist = CPH.filterTablesPerProtocol(new List<string>(MongoTables)); 
            tablelist.Insert(0, "");
            ViewBag.collist = tablelist;
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            if ((CPMetaData == null) || (string.IsNullOrEmpty(CPMetaData._id)) || (CPMetaData.IndicatorsMeta.Count == 0))
            {
                TempData["message"] = "Error: you must define at least one table in 'Define Meta Data' in order to create Coverage rules";
                return RedirectToAction("Main");
            }
            ViewBag.IndicatorsMetalist = CPMetaData.IndicatorsMeta;
            List<string> speclist = CPH.filterTablesPerProtocol((new MDBADMReferenceTable()).ReadListAllADMReferenceTablesIDS()); 
            speclist.Insert(0, "");
            ViewBag.speclist = speclist;

            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
            List<CyclePlanCoverageRule> CyclePlanCoverageRules = MDBCPCoverageRule.ReadProjectList(protocol, proj, phase);
            if (string.IsNullOrEmpty(col))
            {
                if (CPMetaData.IndicatorsMeta.Count > 0) col = CPMetaData.IndicatorsMeta.ElementAt(0).Key;
            }
            ViewBag.selectedCol = col;
            ViewBag.SetupTypes = CPMetaData.IndicatorsMeta[col].SetupTypes;

            Dictionary<string, HashSet<string>> dictionarySetupTypes;
            dictionarySetupTypes = new Dictionary<string, HashSet<string>>();
            foreach (KeyValuePair<string,CPTableMetaData> pair in CPMetaData.IndicatorsMeta)
            {
                if (!dictionarySetupTypes.ContainsKey(pair.Key)) dictionarySetupTypes.Add(pair.Key, new HashSet<string>(pair.Value.SetupTypes));
            }

            ViewBag.NGon.dictionarySetupTypes = dictionarySetupTypes;

            
            //need to delete after HTML clean SpecSubSet dialogs
            ViewBag.selectedSpecTestslist = new List<string>();
            ///

            return View(CyclePlanCoverageRules);
        }

        public ActionResult CreateCoverageRules_copy_config_from(string protocol, string proj, string phase, string copy_from_proj, string copy_from_phase)
        {
            //TODO: move logic to CyclePlanHandler.cs, define Constractor of CyclePlanSpecSubSet(string protocol, string proj, string phase, CyclePlanSpecSubSet ref)
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData refCPMetaData = MDBCPMetaData.Read(protocol, copy_from_proj, copy_from_phase);
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);

            if (!CPMetaData.CompareTablesAndSpecs(refCPMetaData))
            {
                TempData["message"] = "Error: Cycle Plan Meta Data of:" + copy_from_proj + " " + copy_from_phase + " contains different tables or specs, can not copy from it";
                return RedirectToAction("CreateCoverageRules");
            }
            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
            foreach (CyclePlanCoverageRule CPCoverageRule in MDBCPCoverageRule.ReadProjectList(protocol, copy_from_proj, copy_from_phase))
            {
                CyclePlanCoverageRule CPCoverageRuleNew = new CyclePlanCoverageRule(protocol, proj, phase, CPCoverageRule);
                MDBCPCoverageRule.Save(CPCoverageRuleNew);
            }
            return RedirectToAction("CreateCoverageRules");
        }

        public ActionResult CreateCoverageRulesAdd(string protocol, string proj, string phase, string col, string name, string corners, string ports, string lanes, string trials, string SetUpType)
        {
            CyclePlanCoverageRule CPCoverageRule = new CyclePlanCoverageRule();
            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
            try
            {
                CPCoverageRule = new CyclePlanCoverageRule(protocol, proj, phase, col, name, corners, lanes, ports, trials, SetUpType);
            }
            catch (Exception e)
            {
                TempData["message"] = "Error: Rule contains ileagal data: " + e.Message;
                return RedirectToAction("CreateCoverageRules");
            }


            CyclePlanCoverageRule CPCoverageRuleExist = MDBCPCoverageRule.Read(protocol, proj, phase, col, name);
            if (CPCoverageRule._id == CPCoverageRuleExist._id)
            {
                TempData["message"] = "Error: Coverage Rule  " + CPCoverageRuleExist._id + " already exists  ";
                return RedirectToAction("CreateCoverageRules");

            }
            MDBCPCoverageRule.Save(CPCoverageRule);
            return RedirectToAction("CreateCoverageRules");
            //return RedirectToAction("DefineMetaData", new RouteValueDictionary(new { proj = proj, phase = phase }));

        }

        public ActionResult CreateCoverageRulesEditExec(string id, string name, string corners, string ports, string lanes, string trials, string SetUpType)
        {
            CyclePlanCoverageRule CPCoverageRuleNew = new CyclePlanCoverageRule();
            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();

            CyclePlanCoverageRule CPCoverageRuleExist = MDBCPCoverageRule.Read(id);
            if (CPCoverageRuleExist._id != id)
            {
                TempData["message"] = "Error: Coverage Rule  " + id + " does not exists  ";
                return RedirectToAction("CreateCoverageRules");

            }
            try
            {
                CPCoverageRuleNew = new CyclePlanCoverageRule(CPCoverageRuleExist.protocol, CPCoverageRuleExist.project, CPCoverageRuleExist.phase, CPCoverageRuleExist.Table, name, corners, lanes, ports, trials, SetUpType);
            }
            catch (Exception e)
            {
                TempData["message"] = "Error: Rule contains ileagal data: " + e.Message;
                return RedirectToAction("CreateCoverageRules");
            }
            MDBCPCoverageRule.Delete(id);
            MDBCPCoverageRule.Save(CPCoverageRuleNew);
            CyclePlanHandler CPH = new CyclePlanHandler(CPCoverageRuleNew.protocol, CPCoverageRuleNew.project, CPCoverageRuleNew.phase);
            CPH.UpdateCoverageRuleIDInProjectUnits(CPCoverageRuleNew, id);
            return RedirectToAction("CreateCoverageRules");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CreateCoverageRulesDel(string id)
        {
            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
            CyclePlanCoverageRule deletedCoverageRule =  MDBCPCoverageRule.Read(id);
            MDBCPCoverageRule.Delete(id);
            CyclePlanHandler CPH = new CyclePlanHandler(deletedCoverageRule.protocol, deletedCoverageRule.project, deletedCoverageRule.phase);
            CPH.UpdateDelCoverageRuleIDInProjectUnits(id, deletedCoverageRule.Table);
            return RedirectToAction("CreateCoverageRules");
        }


        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult AssignProjectUnits(string protocol, string proj, string phase, string col, string _ShowSum)
        {
            //TODO: some of those beginning lines are redundent
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            if (string.IsNullOrEmpty(_ShowSum)) _ShowSum = FS.LastCyclePlanShowSum;
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            List<string> tablelist = CPH.filterTablesPerProtocol(new List<string>(MongoTables)); 
            tablelist.Insert(0, "");
            ViewBag.collist = tablelist;
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);

            if (_ShowSum == "Y")
            {
                ViewBag.ShowSum = "Y";
                ViewBag.ShowSumRequest = "N";
            }
            else
            {
                ViewBag.ShowSum = "N";
                ViewBag.ShowSumRequest = "Y";
            }
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanShowSum = protocol;
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            if ((CPMetaData == null) || (string.IsNullOrEmpty(CPMetaData._id)) || (CPMetaData.IndicatorsMeta.Count == 0))
            {
                TempData["message"] = "Error: you must define at least one table in 'Define Meta Data' , one SpecSubSet , and one Coverage Rule in order to assign Project Unit";
                return RedirectToAction("Main");
            }
            ViewBag.IndicatorsMetalist = CPMetaData.IndicatorsMeta;
            List<string> speclist = CPH.filterTablesPerProtocol((new MDBADMReferenceTable()).ReadListAllADMReferenceTablesIDS()); 
            speclist.Insert(0, "");
            ViewBag.speclist = speclist;

            AssignProjectUnitDisp Model = new AssignProjectUnitDisp();
            //Need to fill according to selected col):
            Model.CPMetaData = CPMetaData;
            //Model.CyclePlanProjectUnitsDisp;
            //Model.CyclePlanSpecSubSets;
            if (string.IsNullOrEmpty(col))
            {
                if (CPMetaData.IndicatorsMeta.Count > 0) col = CPMetaData.IndicatorsMeta.ElementAt(0).Key;
            }
            ViewBag.selectedCol = col;
            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
            Model.CyclePlanCoverageRules = MDBCPCoverageRule.ReadProjectTableList(protocol, proj, phase,col);
            if (Model.CyclePlanCoverageRules.Count == 0)
            {
                TempData["message"] = "Error: you must define at least one Coverage Rule in order to assign Project Unit";
                return RedirectToAction("Main");
            }
            foreach (CyclePlanCoverageRule CoverageRule in MDBCPCoverageRule.ReadProjectList(protocol, proj, phase))
            {
                if (!Model.CoverageRulesNames.ContainsKey(CoverageRule._id)) Model.CoverageRulesNames.Add(CoverageRule._id, CoverageRule.Name);
                if (!Model.CoverageRulesSums.ContainsKey(CoverageRule._id)) Model.CoverageRulesSums.Add(CoverageRule._id, (CoverageRule.corners.Count*CoverageRule.lanes.Count*CoverageRule.ports.Count*CoverageRule.trials).ToString());
            }

            MDBCyclePlanSpecSubSet MDBCPSpecSubSet = new MDBCyclePlanSpecSubSet();
            Model.CyclePlanSpecSubSets = MDBCPSpecSubSet.ReadProjectTableList(protocol, proj, phase, col);
            if (Model.CyclePlanSpecSubSets.Count == 0)
            {
                TempData["message"] = "Error: you must define at least one Spec Sub Set in order to assign Project Unit";
                return RedirectToAction("Main");
            }
            foreach (CyclePlanSpecSubSet SubSetSpec in MDBCPSpecSubSet.ReadProjectList(protocol, proj, phase))
            {
                if (!Model.SpecSubSetsNames.ContainsKey(SubSetSpec._id)) Model.SpecSubSetsNames.Add(SubSetSpec._id, SubSetSpec.Name);
                if (!Model.SpecSubSetsNumbers.ContainsKey(SubSetSpec._id)) Model.SpecSubSetsNumbers.Add(SubSetSpec._id, SubSetSpec.SpecTestNames.Count.ToString());
            }

            Model.CyclePlanProjectUnitsDisp = new List<CyclePlanProjectUnitDisp>();
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            foreach (CyclePlanProjectUnit CPProjectUnit in MDBCPProjectUnit.ReadProjectList(protocol, proj, phase))
            {
                Model.CyclePlanProjectUnits.Add(CPProjectUnit);
                Model.CyclePlanProjectUnitsDisp.AddRange(CPProjectUnit.GetCyclePlanProjectUnitDispList());
            }

            ViewBag.SetupTypes = CPMetaData.IndicatorsMeta[col].SetupTypes;
            ViewBag.BoardTypes = CPMetaData.IndicatorsMeta[col].BoardTypes;
            Dictionary<string, HashSet<string>> dictionarySpecSubSets;
            dictionarySpecSubSets = new Dictionary<string, HashSet<string>>();
            Dictionary<string, HashSet<string>> dictionarySpecSubSetsNames;
            dictionarySpecSubSetsNames = new Dictionary<string, HashSet<string>>();
            foreach (CyclePlanSpecSubSet SubSetSpec in MDBCPSpecSubSet.ReadProjectList(protocol, proj, phase))
            {
                if (!dictionarySpecSubSets.ContainsKey(SubSetSpec.Table)) dictionarySpecSubSets.Add(SubSetSpec.Table, new HashSet<string>());
                if (!dictionarySpecSubSets[SubSetSpec.Table].Contains(SubSetSpec._id)) dictionarySpecSubSets[SubSetSpec.Table].Add(SubSetSpec._id);
                if (!dictionarySpecSubSetsNames.ContainsKey(SubSetSpec.Table)) dictionarySpecSubSetsNames.Add(SubSetSpec.Table, new HashSet<string>());
                if (!dictionarySpecSubSetsNames[SubSetSpec.Table].Contains(SubSetSpec.Name)) dictionarySpecSubSetsNames[SubSetSpec.Table].Add(SubSetSpec.Name);
            }

            ViewBag.NGon.dictionarySpecSubSets = dictionarySpecSubSets;
            ViewBag.NGon.dictionarySpecSubSetsNames = dictionarySpecSubSetsNames;

            Dictionary<string, HashSet<string>> dictionaryCoverageRules;
            dictionaryCoverageRules = new Dictionary<string, HashSet<string>>();
            Dictionary<string, HashSet<string>> dictionaryCoverageRulesNames;
            dictionaryCoverageRulesNames = new Dictionary<string, HashSet<string>>();
            foreach (CyclePlanCoverageRule CoverageRule in MDBCPCoverageRule.ReadProjectList(protocol, proj, phase))
            {
                if (!dictionaryCoverageRules.ContainsKey(CoverageRule.Table)) dictionaryCoverageRules.Add(CoverageRule.Table, new HashSet<string>());
                if (!dictionaryCoverageRules[CoverageRule.Table].Contains(CoverageRule._id)) dictionaryCoverageRules[CoverageRule.Table].Add(CoverageRule._id);
                if (!dictionaryCoverageRulesNames.ContainsKey(CoverageRule.Table)) dictionaryCoverageRulesNames.Add(CoverageRule.Table, new HashSet<string>());
                if (!dictionaryCoverageRulesNames[CoverageRule.Table].Contains(CoverageRule.Name)) dictionaryCoverageRulesNames[CoverageRule.Table].Add(CoverageRule.Name);
            }

            ViewBag.NGon.dictionaryCoverageRules = dictionaryCoverageRules;
            ViewBag.NGon.dictionaryCoverageRulesNames = dictionaryCoverageRulesNames;

            Dictionary<string, string> dictionaryCopy_SourceId = new Dictionary<string, string>();
            Dictionary<string, string> dictionaryCopy_Skew = new Dictionary<string, string>();
            foreach (CyclePlanProjectUnit CPPU in Model.CyclePlanProjectUnits)
            {
                dictionaryCopy_SourceId.Add(CPPU._id, CPPU.LogicUnit);
                //Crystal_MR_C0_Crystal1_TYP_N_H_1,TYP_N_H_1
                string tmpSkweKey = CPPU.protocol + "_" + CPPU.project + "_" + CPPU.phase + "_" + CPPU.Table + "_" + CPPU.Skew + "_" + CPPU.LogicUnitNumber;
                string tmpSkwe = CPPU.Skew + "_" + CPPU.LogicUnitNumber;
                if (!dictionaryCopy_Skew.ContainsKey(tmpSkweKey)) dictionaryCopy_Skew.Add(tmpSkweKey, tmpSkwe);
            }
            ViewBag.NGon.dictionaryCopy_SourceId = dictionaryCopy_SourceId;
            ViewBag.NGon.dictionaryCopy_Skew = dictionaryCopy_Skew;

            CyclePlanMetaData CPMetaDataDefault = MDBCPMetaData.Read(DefaultProtocol, proj, phase);
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = new DateTime(Convert.ToInt32(CPMetaDataDefault.wws.Substring(0, 4)), Convert.ToInt32(CPMetaDataDefault.wws.Substring(4, 2)), Convert.ToInt32(CPMetaDataDefault.wws.Substring(6, 2)));
            Calendar cal = dfi.Calendar;
            int curWeek = cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            Dictionary<string, string> weeks = new Dictionary<string, string>();
            for (int weekCnt = 0; weekCnt < Convert.ToInt32(CPMetaDataDefault.wn); weekCnt++)
            {
                string weekTmp = date1.AddDays(Convert.ToDouble(weekCnt * 7)).ToString("yyyyMMdd");
                weeks.Add(cal.GetWeekOfYear(date1.AddDays(Convert.ToDouble(weekCnt * 7)), dfi.CalendarWeekRule, dfi.FirstDayOfWeek).ToString(), CCDUtilities.getAdjacentSundayDate(weekTmp));
                if (weekCnt == 0) ViewBag.firstWeek = weekTmp;
            }
            ViewBag.workweeks = weeks;
            string validateMessage = "";
            if (!Model.Validate(out validateMessage))
            {
                TempData["message"] = "Error: " + validateMessage + " ,you must define at least one SpecSubSet , and one Coverage Rule for each Table in order to assign Project Unit";
                return RedirectToAction("Main");
            }

            //need to delete after HTML clean SpecSubSet dialogs
            ViewBag.selectedSpecTestslist = new List<string>();
            ///

            return View(Model);
        }

        public ActionResult AssignProjectUnits_copy_config_from(string protocol, string proj, string phase, string copy_from_proj, string copy_from_phase)
        {
            //TODO: move logic to CyclePlanHandler.cs, define Constractor of CyclePlanSpecSubSet(string protocol, string proj, string phase, CyclePlanSpecSubSet ref)
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData refCPMetaData = MDBCPMetaData.Read(protocol, copy_from_proj, copy_from_phase);
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);

            if (!CPMetaData.CompareTablesAndSpecs(refCPMetaData))
            {
                TempData["message"] = "Error: Cycle Plan Meta Data of:" + copy_from_proj + " " + copy_from_phase + " contains different tables or specs, can not copy from it";
                return RedirectToAction("AssignProjectUnits");
            }

            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            if (!CPH.ValidateAssignProjectUnits_copy_config_from(copy_from_proj, copy_from_phase))
            {
                TempData["message"] = "Error: Cycle Plan SpecSubSets/CoverageRules of: " + copy_from_proj + " " + copy_from_phase + " contains different names, cannot copy from it";
                return RedirectToAction("AssignProjectUnits");
            }
            //
            CyclePlanMetaData CPMetaDataDefault = MDBCPMetaData.Read(DefaultProtocol, proj, phase);

            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            foreach (CyclePlanProjectUnit CPProjectUnit in MDBCPProjectUnit.ReadProjectList(protocol, copy_from_proj, copy_from_phase))
            {
                CyclePlanProjectUnit CPProjectUnitNew = new CyclePlanProjectUnit(protocol, proj, phase, CPProjectUnit, CPMetaDataDefault.wws);
                MDBCPProjectUnit.Save(CPProjectUnitNew);
            }
            return RedirectToAction("AssignProjectUnits");
        }

        public ActionResult AssignProjectUnitsAdd(string protocol, string proj, string phase, string col, string Skew, string LogicUnitNumber, string BoardType, string SetUpType,string stationsNum, string SpecSubSetID, string CoverageRuleID, string wws)
        {
            CyclePlanProjectUnit CPProjectUnit = new CyclePlanProjectUnit();
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            try
            {
                CPProjectUnit = new CyclePlanProjectUnit(protocol, proj, phase, col, Skew, BoardType, SetUpType, LogicUnitNumber,stationsNum, SpecSubSetID, CoverageRuleID, wws);
                //CPProjectUnit = new CyclePlanProjectUnit(protocol, proj, phase, col, Skew, LogicUnitNumber,BoardType, SetUpType, SpecSubSetID, CoverageRuleID, "");
            }
            catch (Exception e)
            {
                TempData["message"] = "Error: Unit contains ileagal data: " + e.Message;
                return RedirectToAction("AssignProjectUnits");
            }


            CyclePlanProjectUnit CPProjectUnitExist = MDBCPProjectUnit.Read(protocol, proj, phase, col, Skew,LogicUnitNumber, BoardType, SetUpType);
            if (CPProjectUnitExist._id == CPProjectUnit._id)
            {
                if (CPProjectUnitExist.AddPlan(SpecSubSetID, CoverageRuleID, wws,stationsNum)) MDBCPProjectUnit.Save(CPProjectUnitExist) ;
                else TempData["message"] = "Error: Unit with requested spec subset and coverage rull:  " + CPProjectUnitExist._id + " already exists  ";
                return RedirectToAction("AssignProjectUnits");

            }
            MDBCPProjectUnit.Save(CPProjectUnit);
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            CPH.commitNeedToUpdate();
            return RedirectToAction("AssignProjectUnits");
            //return RedirectToAction("DefineMetaData", new RouteValueDictionary(new { proj = proj, phase = phase }));

        }

        public ActionResult AssignProjectUnitsCopyUnit(string protocol, string proj, string phase, string col,string SourceId, string Skew, string LogicUnitNumber, string BoardType, string SetUpType)
        {
            CyclePlanProjectUnit CPProjectUnit = new CyclePlanProjectUnit();
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();

            CPProjectUnit = new CyclePlanProjectUnit(protocol, proj, phase, col, Skew, BoardType, SetUpType, LogicUnitNumber, "1", "", "", "");

            CyclePlanProjectUnit CPProjectUnitExist = MDBCPProjectUnit.Read(protocol, proj, phase, col, Skew, LogicUnitNumber, BoardType, SetUpType);
            if (CPProjectUnitExist._id == CPProjectUnit._id)
            {
                TempData["message"] = "Error: Target Unit requested already exists: " + CPProjectUnit._id + " , you can copy to non exist target units only.";
                return RedirectToAction("AssignProjectUnits");

            }
            CyclePlanProjectUnit CPProjectUnitSource = MDBCPProjectUnit.Read(SourceId);
            if (CPProjectUnitSource._id != SourceId)
            {
                TempData["message"] = "Error: Source Unit requested does not exist" + SourceId;
                return RedirectToAction("AssignProjectUnits");
            }
            CPProjectUnit.plans = new Dictionary<string, Plan>(CPProjectUnitSource.plans);
            MDBCPProjectUnit.Save(CPProjectUnit);
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            CPH.commitNeedToUpdate();
            return RedirectToAction("AssignProjectUnits");
        }

        public ActionResult AssignProjectUnitsCopySkew(string protocol, string proj, string phase, string col, string SourceSkewStr, string TargetSkew, string TargetLogicUnitNumber)
        {
            string SourceSkew = SourceSkewStr.Split('_')[0];
            string SourceLogicUnitNumber = SourceSkewStr.Split('_')[1];
            CyclePlanProjectUnit CPProjectUnit = new CyclePlanProjectUnit();
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();

            //id example: Crystal_MR_C0_Crystal1_FF_N_L_1
            foreach (CyclePlanProjectUnit CPProjectUnitExist in MDBCPProjectUnit.ReadProjectTableList(protocol, proj, phase, col))
            {
                if ((CPProjectUnitExist.Skew == TargetSkew) && (CPProjectUnitExist.LogicUnitNumber == TargetLogicUnitNumber))
                {
                    TempData["message"] = "Error: Target Skew  already contains units: " + CPProjectUnitExist._id + " , you can copy to non exist target Skews only.";
                    return RedirectToAction("AssignProjectUnits");

                }
            }
            foreach (CyclePlanProjectUnit CPProjectUnitSource in MDBCPProjectUnit.ReadProjectTableList(protocol, proj, phase, col))
            {
                if ((CPProjectUnitSource.Skew == SourceSkew) && (CPProjectUnitSource.LogicUnitNumber == SourceLogicUnitNumber))
                {
                    CPProjectUnit = new CyclePlanProjectUnit(protocol, proj, phase, col, TargetSkew, CPProjectUnitSource.BoardType, CPProjectUnitSource.SetupType, TargetLogicUnitNumber, "1", "", "", "");
                    CPProjectUnit.plans = new Dictionary<string, Plan>(CPProjectUnitSource.plans);
                    MDBCPProjectUnit.Save(CPProjectUnit);
                }
            }

            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            CPH.commitNeedToUpdate();
            return RedirectToAction("AssignProjectUnits");
        }

        public ActionResult AssignProjectUnitsEditExec(string id, string Skew, string LogicUnitNumber, string BoardType, string SetUpType, string stationsNum, string SpecSubSetID, string SpecSubSetIDorig, string CoverageRuleID, string CoverageRuleIDorig, string wws)
        {
            CyclePlanProjectUnit CPProjectUnitNew = new CyclePlanProjectUnit();
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            CyclePlanProjectUnit CPProjectUnitExist = MDBCPProjectUnit.Read(id);
            if (CPProjectUnitExist._id != id)
            {
                TempData["message"] = "Error: can not find project unit " + id;
                return RedirectToAction("AssignProjectUnits");
            }
            try
            {
                CPProjectUnitNew = new CyclePlanProjectUnit(CPProjectUnitExist.protocol, CPProjectUnitExist.project, CPProjectUnitExist.phase, CPProjectUnitExist.Table, Skew, BoardType, SetUpType, LogicUnitNumber,stationsNum, SpecSubSetID, CoverageRuleID, wws);
            }
            catch (Exception e)
            {
                TempData["message"] = "Error: Unit contains ileagal data: " + e.Message;
                return RedirectToAction("AssignProjectUnits");
            }

            MDBCPProjectUnit.Delete(id);
            foreach (var existPlanPair in CPProjectUnitExist.plans)
            {
                if (existPlanPair.Key != (new Plan(SpecSubSetIDorig, CoverageRuleIDorig, "",""))._id) CPProjectUnitNew.AddPlan(existPlanPair.Value);
            }

            if (!MDBCPProjectUnit.Save(CPProjectUnitNew))
            {
                TempData["message"] = "Error: could not save project unit:  " + CPProjectUnitNew._id;
                return RedirectToAction("AssignProjectUnits");
            }
            CyclePlanHandler CPH = new CyclePlanHandler(CPProjectUnitExist.protocol, CPProjectUnitExist.project, CPProjectUnitExist.phase);
            CPH.commitNeedToUpdate();

            return RedirectToAction("AssignProjectUnits");

        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult AssignProjectUnitsDelPlan(string id, string SpecSubSetID, string CoverageRuleID)
        {
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            CyclePlanProjectUnit CPProjectUnitExist = MDBCPProjectUnit.Read(id);
            CPProjectUnitExist.removePlan(new Plan(SpecSubSetID,CoverageRuleID, "", ""));
            if (CPProjectUnitExist.plans.Count == 0) MDBCPProjectUnit.Delete(id);
            else MDBCPProjectUnit.Save(CPProjectUnitExist);
            CyclePlanHandler CPH = new CyclePlanHandler(CPProjectUnitExist.protocol, CPProjectUnitExist.project, CPProjectUnitExist.phase);
            CPH.commitNeedToUpdate();
            return RedirectToAction("AssignProjectUnits");
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult ValidateProject(string protocol, string proj, string phase)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            List<string> tablelist = CPH.filterTablesPerProtocol(new List<string>(MongoTables)); 
            tablelist.Insert(0, "");
            ViewBag.collist = tablelist;
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;

            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            //if (CPMetaData.protocol != protocol) CPMetaData = new CyclePlanMetaData(protocol, proj, phase);//TODO handle erro case
            List<string> projectTables = new List<string>(CPMetaData.IndicatorsMeta.Keys);
            ViewBag.ProjectColList = projectTables;
            return View();
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult PlanProject(string protocol, string proj, string phase)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            List<string> tablelist = CPH.filterTablesPerProtocol(new List<string>(MongoTables));
            tablelist.Insert(0, "");
            ViewBag.collist = tablelist;
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;

            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            //if (CPMetaData.protocol != protocol) CPMetaData = new CyclePlanMetaData(protocol, proj, phase);//TODO handle erro case
            List<string> projectTables = new List<string>(CPMetaData.IndicatorsMeta.Keys);
            ViewBag.ProjectColList = projectTables;
            return View();
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult PlanIndicator(string protocol, string proj, string phase,string col, string numWWperUnit)
        {
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            Indicator Ind = CPH.GetPlannedIndicator(col, numWWperUnit);
            ViewBag.protocol = protocol;
            MDBIndicator MDBIndicator = new MDBIndicator();
            Indicator IndExist = MDBIndicator.ReadIndicator(Ind._id);
            if (IndExist._id == Ind._id) ViewBag.IndExists = "Y";
            Dictionary<string, string> SpecTestNamesSum = Ind.getSpecTestNamesSum();
            ViewBag.SpecTestNamesSum = SpecTestNamesSum;
            ViewBag.numWWperUnit = numWWperUnit;
            return View(Ind);
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult PlanIndicatorExportIndToExcel(string protocol, string proj, string phase, string col, string numWWperUnit)
        {
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            Indicator Ind = CPH.GetPlannedIndicator(col, numWWperUnit);

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
            string EmptyExcel = Path.Combine(ServerDir, "EVData", "IndicatorSingleTemplate.xlsx"); //We can use another template here
            string filename = "Indicator_" + Ind._id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);

            System.IO.File.Copy(EmptyExcel, filepath, true);
            IndicatorDisplayResult DispResult = new IndicatorDisplayResult();
            MDBIndicator MDBIndicator = new MDBIndicator();
            Indicator IndExist = MDBIndicator.ReadIndicator(Ind._id);
            if (IndExist._id == Ind._id) ViewBag.IndExists = "Y";
            //Create TMP indicator for exporting to Excel, delete it at the end
            Ind._id = System.IO.Path.GetFileNameWithoutExtension(filename);
            MDBIndicator.SaveIndicator(Ind);
            if (!string.IsNullOrEmpty(Ind._id)) DispResult = new IndicatorHandler().LaunchIndicator(Ind._id, true,true);
            MDBIndicator.DeleteIndicator(Ind._id);
            (new ExcelHandler()).writeIndicatorSheet(filepath, "Results", DispResult);
            (new ExcelHandler()).writeSingleIndicatorDetailsSheet(filepath, "Results", DispResult);
            ViewBag.filename = filename;
            Dictionary<string, string> SpecTestNamesSum = Ind.getSpecTestNamesSum();
            ViewBag.SpecTestNamesSum = SpecTestNamesSum;
            return View("PlanIndicator", Ind);
        }


        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CSR_Table_Configure(string protocol, string proj, string phase, string selectedTable, string ExportedFileNAme)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);

            List<string> tablelist = CPH.filterTablesPerProtocol(new List<string>(MongoTables));
            tablelist.Insert(0, "");
            ViewBag.collist = tablelist;
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;

            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            if ((CPMetaData == null) || (string.IsNullOrEmpty(CPMetaData._id)) || (CPMetaData.IndicatorsMeta.Count == 0))
            {
                //TODO: improve message, need to define many things...
                TempData["message"] = "Error: you must define at least one table in 'Define Meta Data' in order to configure SM/CSR";
                return RedirectToAction("Main");
            }
            //need to have updated LookUP since trials number and tests number of Div line, udated from there
            bool wasCSRLookUpUpdated = CPH.UpdateCyclePlanCSRLookUP(); //need to see it is working well in foreground (or must done BG)
            List<string> projectTables = new List<string>(CPMetaData.IndicatorsMeta.Keys);
            ViewBag.ProjectColList = projectTables;
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);
            SessionsManageProtocolConf SMPConfCurrent = MDBSMconf.Read(protocol, proj, phase);
            if (!SMPConf.ImpEquals(SMPConfCurrent)) TempData["messageInfo"] = "Notice: Configuration of NEW CSR is different than active CSR";

            var TablesNotInProject = SMPConf.table_divs.Keys.Except(SMPConf.tables).ToList();
            if (TablesNotInProject.Any())
            {
                string add_table = "";
                foreach (string table in TablesNotInProject) add_table += table + ",";
                TempData["message"] = "Error: CSR Table Configuration contains the following table \"" + add_table + "\" which does not defined in SM/CSR configuration; Delete those lines or restore Table definition";
            }
            //TODO: see if this partial initializtion makes sense
            //write this in SessionManageHandler
            if (SMPConf.tables.Count == 0)
            {
                SMPConf.protocol = protocol;
                SMPConf.proj = proj;
                SMPConf.phase = phase;
                SMPConf.tables = new List<string>(CPMetaData.IndicatorsMeta.Keys);
                foreach (string table in SMPConf.tables) SMPConf.table_divs.Add(table, new Dictionary<string, TableDiv>());
                //MDBSMconf.Save(SMPConf);
            }

            foreach (string table in SMPConf.tables)
            {
                if (!SMPConf.table_divs.ContainsKey(table))SMPConf.table_divs.Add(table, new Dictionary<string, TableDiv>());
            }



            //?????? not efficient update of "trials"...

            foreach (string table in SMPConf.tables)
            {
                foreach (var pair in SMPConf.table_divs[table])
                {
                    foreach (var tdl in pair.Value.TableDivLines)
                    {
                        bool isSpecSubSetFileds = true ;
                        foreach (TableDivField tdf in tdl.Value.tableDivFields) 
                        {
                            if (!tdf.isSpecSubSet)
                            {
                                isSpecSubSetFileds = false;
                                break;
                            }
                        }
                        if (isSpecSubSetFileds) tdl.Value.trials = CPH.GetTrialsOfSpecTestNameCSRLookUP(table, tdl.Value.tableDivFields[0].values[0]);
                    }
                }
            }
            
            //??????

            Dictionary<string, HashSet<string>> dictionaryDivLines;
            dictionaryDivLines = new Dictionary<string, HashSet<string>>();
            Dictionary<string, string> dictionaryTablesOfDivs;
            dictionaryTablesOfDivs = new Dictionary<string, string>();
            Dictionary<string, string> UserReportsNames = new Dictionary<string, string>();
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            foreach (string table in SMPConf.tables)
            {
                HashSet<string> divs = new HashSet<string>();
                foreach (var pair in SMPConf.table_divs[table])
                {
                    if (!dictionaryDivLines.ContainsKey(pair.Key)) dictionaryDivLines.Add(pair.Key, new HashSet<string>(pair.Value.TableDivLines.Keys));
                    if (!dictionaryTablesOfDivs.ContainsKey(pair.Key)) dictionaryTablesOfDivs.Add(pair.Key, table);
                    //pair.Value.SRreportId
                    UserTableReport ReportTmp = MDBReport.ReadUserTableReport(pair.Value.SRreportId);
                    if ((ReportTmp._id == pair.Value.SRreportId) && (!UserReportsNames.ContainsKey(pair.Value.SRreportId))) UserReportsNames.Add(ReportTmp._id, ReportTmp.Name);
                    foreach (string id in pair.Value.reportIds)
                    {
                        if (String.IsNullOrEmpty(id)) continue;
                        ReportTmp = MDBReport.ReadUserTableReport(id);
                        if ((ReportTmp._id == id) && (!UserReportsNames.ContainsKey(id))) UserReportsNames.Add(ReportTmp._id, ReportTmp.Name);
                    }

                }
                //dictionaryDivLines.Add("new_"+ table, divs);
            }
            dictionaryDivLines.Add("new", new HashSet<string>(dictionaryDivLines.Keys));
            ViewBag.NGon.dictionaryDivLines = dictionaryDivLines;
            ViewBag.NGon.dictionaryTablesOfDivs = dictionaryTablesOfDivs; 

            //////New: prepare values for "Div Line select specs Dialog"
            if (String.IsNullOrEmpty(selectedTable)) selectedTable = SMPConf.tables[0];
            ViewBag.selectedSpecTestslist = CPH.GetSpecTestNamesListForTable(selectedTable);
            ViewBag.selectedCol = selectedTable;
            ViewBag.selectedSpec = CPMetaData.IndicatorsMeta[selectedTable].spec;

            Dictionary<string, HashSet<string>> dictionarySpecs;
            dictionarySpecs = new Dictionary<string, HashSet<string>>();
            foreach (string table in SMPConf.tables)
            {
                dictionarySpecs.Add(table, new HashSet<string>(CPH.GetSpecTestNamesListForTable(table)));
            }
            ViewBag.NGon.dictionarySpecs = dictionarySpecs;
            //New: Report assigning
            
            string username = User.Identity.Name;
            //string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
            MongoCursor Cur = MDBReport.ReadCursorAllUserTableReports(username);
            Dictionary<string, HashSet<string>> UserReports = new Dictionary<string, HashSet<string>>();
            
            foreach (BsonDocument rec in Cur)
            {
                UserTableReport Report = new UserTableReport();
                Report._id = rec.GetValue("_id", "").ToString();
                Report.Name = rec.GetValue("name", "").ToString();
                Report.RConfig = rec.GetValue("RConfig").AsBsonDocument.BsonDocumentToReportConfig();
                if (SMPConf.tables.Contains(Report.RConfig.table))
                {
                    if (!UserReports.ContainsKey(Report.RConfig.table)) UserReports.Add(Report.RConfig.table, new HashSet<string>());
                    UserReports[Report.RConfig.table].Add(Report._id);
                    if (!UserReportsNames.ContainsKey(Report._id)) UserReportsNames.Add(Report._id, Report.Name);
                }
            }
            ViewBag.NGon.DictionaryUserTableReports = UserReports;
            
            ViewBag.NGon.DictionaryUserReportsNames = UserReportsNames;

            if (!String.IsNullOrEmpty(ExportedFileNAme)) ViewBag.ExportedFileNAme = ExportedFileNAme;

            return View(SMPConf);
        }

        public ActionResult CSR_copy_config_from(string protocol, string proj, string phase, string copy_from_proj, string copy_from_phase, string sourceAction)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf newSMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);
            if (newSMPConf.table_divs.Count>0)
            {
                TempData["message"] = "Error: Configuration already exist for: \"" + protocol + " " + proj + " " + phase + "\"";
                return RedirectToAction(sourceAction);
            }
            SessionsManageProtocolConf refSMPConf = MDBSMconf.Read(protocol, copy_from_proj, copy_from_phase);
            if (refSMPConf.table_divs.Count == 0)
            {
                TempData["message"] = "Error: Reference Configuration does not exist: \"" + protocol + " " + copy_from_proj + " " + copy_from_phase + "\"";
                return RedirectToAction(sourceAction);
            }
            //Verify tables in ref conf are defined in this project
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            foreach (string table in refSMPConf.tables) if (!CPMetaData.IndicatorsMeta.Keys.Contains(table))
                {
                    TempData["message"] = "Error: Reference Configuration contains the following table \"" + table + "\" which does not defined in current project";
                    return RedirectToAction(sourceAction);
                }


            newSMPConf = refSMPConf;
            foreach (string table in CPMetaData.IndicatorsMeta.Keys) if (!newSMPConf.tables.Contains(table)) newSMPConf.tables.Add(table);
            newSMPConf.proj = proj;
            newSMPConf.phase = phase;
            newSMPConf.ActiveFileNames.Clear();
            MDBSMconf.SaveTMP(newSMPConf);

            return RedirectToAction(sourceAction);

        }



        public ActionResult CSR_reset_config(string protocol, string proj, string phase, string copy_from_proj, string copy_from_phase, string sourceAction)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            MDBSMconf.DeleteTMP(protocol, proj, phase);
            SessionsManageProtocolConf SMPConf = MDBSMconf.Read(protocol, proj, phase);
            SessionsManageProtocolConf SMPConftmp = new SessionsManageProtocolConf();
            if (SMPConf.protocol == protocol)
            {
                SMPConftmp.protocol = protocol;
                SMPConftmp.phase = phase;
                SMPConftmp.proj = proj;
                SMPConftmp.tables = new List<string>(SMPConf.tables);
                SMPConftmp.fields = new List<string>(SMPConf.fields);
                SMPConftmp.fieldsAliases = new List<string>(SMPConf.fieldsAliases);
                SMPConftmp.corners = new List<string>(SMPConf.corners);
                SMPConftmp.ports = new List<string>(SMPConf.ports);
                SMPConftmp.lanes = new List<string>(SMPConf.lanes);
                SMPConftmp.units = new List<Unit>(SMPConf.units);
                SMPConftmp.separateCSRperTable = SMPConf.separateCSRperTable;
                SMPConftmp.SRTemplate = SMPConf.SRTemplate;
                MDBSMconf.SaveTMP(SMPConftmp);
            }

            if (!String.IsNullOrEmpty(copy_from_proj) && (!String.IsNullOrEmpty(copy_from_phase))) return RedirectToAction("CSR_copy_config_from", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, copy_from_proj = copy_from_proj, copy_from_phase = copy_from_phase, sourceAction = sourceAction }));
            return RedirectToAction(sourceAction);

        }

        public ActionResult CSR_Table_Div_Line_Add(string protocol, string proj, string phase, string col, string DivName, string newDivName, string newDivTable, string DivLinePos, string DivLineName, string trials, string NumOfTests, string Fields)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);
            //TODO: see if this partial initializtion makes sense
            //write this in SessionManageHandler
            if (SMPConf.tables.Count == 0)
            {
                SMPConf.protocol = protocol;
                SMPConf.proj = proj;
                SMPConf.phase = phase;
                MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
                CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
                SMPConf.tables = new List<string>(CPMetaData.IndicatorsMeta.Keys);
                //foreach (string table in SMPConf.tables) SMPConf.table_divs.Add(table, new Dictionary<string, TableDiv>());
                //MDBSMconf.Save(SMPConf);
            }
            if ((DivName=="new") && (String.IsNullOrEmpty(newDivName)))
            {
                TempData["message"] = "Error: New Div Name is empty";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = col }));
            }

            //Not sure what it was meant to check
            //if ((SMPConf.table_divs.Count>0) && (!SMPConf.table_divs.ContainsKey(col)))
            //{
            //    TempData["message"] = "Error: DB Table value sent:\"" + col + "\" does not exist in Conf of project";
            //    return RedirectToAction("ConfigureSM_CSR");
            //}

            //if ((SMPConf.table_divs.Count == 0) && (!SMPConf.table_divs.ContainsKey(col))) SMPConf.table_divs.Add(col, new Dictionary<string, TableDiv>());
            foreach (string table in SMPConf.tables)
            {
                if (!SMPConf.table_divs.ContainsKey(table)) SMPConf.table_divs.Add(table, new Dictionary<string, TableDiv>());
            }

            if ((DivName == "new") && (!String.IsNullOrEmpty(newDivName))) DivName = newDivName;



            
            TableDivLine newDivLine = new TableDivLine(DivLineName, new List<TableDivField>());

            if (!String.IsNullOrEmpty(Fields))
            {
                int specCnt = 0;
                CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
                foreach (string SpecTestName in (new List<string>(Fields.Split(','))))
                {
                    TableDivField tdSpecField = new TableDivField("SpecTestName", SpecTestName, true);
                    newDivLine.tableDivFields.Add(tdSpecField);
                    if (specCnt == 0) newDivLine.trials = CPH.GetTrialsOfSpecTestNameCSRLookUP(col, SpecTestName); //TODO, Error in case of more than 1 trials num?
                    specCnt++;
                }
                newDivLine.numOfTests = specCnt;
            }


            //TODO:(?) if fields type of "isSpecSubSet" are in use, no "SpecTestName" fields are allowed 
            //field1,condition1,values1 (1=>5)
            for (int i = 1; i < 6; i++)
            {
                string field = Request.Form["field" + i.ToString()];
                if (String.IsNullOrEmpty(field)) break;
                string condition = Request.Form["condition" + i.ToString()];
                string values = Request.Form["values" + i.ToString()];
                TableDivField tdField = new TableDivField();
                tdField.name = field;
                tdField.values = new List<string>(values.Split(','));
                tdField.condition = condition;
                newDivLine.tableDivFields.Add(tdField);
                //assigning here, since we know we have "additinal conditions"
                newDivLine.trials = Convert.ToInt32(trials);
                newDivLine.numOfTests = Convert.ToInt32(NumOfTests);
            }
            if (newDivLine.tableDivFields.Count == 0)
            {
                TempData["message"] = "Error: 1 Field, Condition and Values is mandatory";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = col }));
            }
            if ((newDivLine.getSpecTestNamesOfSpecSubSet().Count>0) && (newDivLine.isDivLineWithAddCondoition_SpecTestName()))
            {
                TempData["message"] = "Error: You cannot use both - SpecTestName SubSet && SpecTestName additional definition";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = col }));
            }


            TableDiv ExistTableDiv = new TableDiv();

            if (!SMPConf.table_divs[col].ContainsKey(DivName))
            {
                //TODO: add Div
                //SMPConf.table_divs[col].Add(DivName, new TableDiv(DivName,col));
                SMPConf.InsertTableDiv(col, new TableDiv(DivName, col), DivLinePos);
            }
            foreach (var tmpTDV in SMPConf.table_divs[col].Values)
            {
                if ((tmpTDV.TableDivLines.ContainsKey(newDivLine.name)))
                {
                    TempData["message"] = "Error: Div Line Name:" + newDivLine.name + " already exists, please use another name";
                    //return RedirectToAction("CSR_Table_Configure");
                    return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = col }));
                }
            }
            SMPConf.table_divs[col][DivName].InsertDivLine(newDivLine, DivLinePos);
          
            MDBSMconf.SaveTMP(SMPConf);

            //return RedirectToAction("CSR_Table_Configure");
            return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = col }));

        }

       
        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CSR_Table_Div_Line_Del(string protocol, string proj, string phase, string table, string DivName, string DivLineName)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);
            if (!(SMPConf.table_divs.ContainsKey(table)) || (!SMPConf.table_divs[table].ContainsKey(DivName)))
            {
                TempData["message"] = "Error: Div Line \"" + DivName + " " + DivLineName + "\" does not exist";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }
            SMPConf.table_divs[table][DivName].RemoveDivLine(DivLineName);
            if (SMPConf.table_divs[table][DivName].TableDivLines.Count==0) SMPConf.table_divs[table].Remove(DivName);
            MDBSMconf.SaveTMP(SMPConf);
            //return RedirectToAction("CSR_Table_Configure");
            return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
        }

        [Authorize(Roles = "CCD_EV_ADV")]
        public ActionResult CSR_Table_BuildCSRExcel(string protocol, string proj, string phase, string selectedTable)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            //TODO ...
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);
            if (SMPConf.excelPorts.Count==0)
            {
                TempData["message"] = "ERROR: cannot create Excel file, \"Excel Ports\" is empty, please define (fill \"NONE\" if you do not need ports resolution)";
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = selectedTable }));
            }
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            CycleStatusReportExcelBuilder CSRBuilder = new CycleStatusReportExcelBuilder(SMPConf, CPMetaData);
            string message = "";
            string filename = CSRBuilder.pocBuildFromScratch(selectedTable,out message);
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            if (System.IO.File.Exists(filepath))
            {
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj,selectedTable = selectedTable, ExportedFileNAme = filename }));
            }
            else
            {
                TempData["message"] = "ERROR: cannot create Excel file, " + message;
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = selectedTable }));
            }
        }

        public ActionResult CSR_Table_Div_Line_EditExec(string protocol, string proj, string phase, string table, string DivName, string DivLineName, string origDivLineName, string trials, string NumOfTests, string FieldsEdit)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);

            if ((SMPConf.table_divs.Count > 0) && (!SMPConf.table_divs.ContainsKey(table)))
            {
                TempData["message"] = "Error: DB Table value sent:\"" + table + "\" does not exist in Conf of project";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }

            if ((SMPConf.table_divs.Count == 0) && (!SMPConf.table_divs.ContainsKey(table))) SMPConf.table_divs.Add(table, new Dictionary<string, TableDiv>());

            
            TableDivLine newDivLine = new TableDivLine(DivLineName, new List<TableDivField>());
            //TODO? move this merge code to handler
            TableDivLine origDivLine = SMPConf.table_divs[table][DivName].TableDivLines[origDivLineName];
            if (!String.IsNullOrEmpty(FieldsEdit))
            {
                int specCnt = 0;
                CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
                foreach (string SpecTestName in (new List<string>(FieldsEdit.Split(','))))
                {
                    TableDivField tdSpecField = new TableDivField("SpecTestName", SpecTestName, true);
                    newDivLine.tableDivFields.Add(tdSpecField);
                    if (specCnt == 0) newDivLine.trials = CPH.GetTrialsOfSpecTestNameCSRLookUP(table, SpecTestName); //TODO, Error in case of more than 1 trials num?
                    specCnt++;
                }
                newDivLine.numOfTests = specCnt;

                
                ////No need for that since interface is unified
                //foreach (TableDivField eTDF in origDivLine.tableDivFields)
                //{
                //    if (!eTDF.isSpecSubSet) newDivLine.tableDivFields.Add(eTDF);
                //}
            }
            //No need for that since interface is unified
            //else
            //{
            //    foreach (TableDivField eTDF in origDivLine.tableDivFields)
            //    {
            //        if (eTDF.isSpecSubSet) newDivLine.tableDivFields.Add(eTDF);
            //    }
            //}

            //field1,condition1,values1 (1=>5)
            for (int i = 1; i < 6; i++)
            {
                string field = Request.Form["field" + i.ToString()];
                if (String.IsNullOrEmpty(field)) break;
                string condition = Request.Form["condition" + i.ToString()];
                string values = Request.Form["values" + i.ToString()];
                TableDivField tdField = new TableDivField();
                tdField.name = field;
                tdField.values = new List<string>(values.Split(','));
                tdField.condition = condition;
                newDivLine.tableDivFields.Add(tdField);
                //assigning here, since we know we have "additinal conditions"
                newDivLine.trials = Convert.ToInt32(trials);
                newDivLine.numOfTests = Convert.ToInt32(NumOfTests);
            }
            if (newDivLine.tableDivFields.Count == 0)
            {
                TempData["message"] = "Error: 1 Field, Condition and Values is mandatory";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }

            if ((newDivLine.getSpecTestNamesOfSpecSubSet().Count > 0) && (newDivLine.isDivLineWithAddCondoition_SpecTestName()))
            {
                TempData["message"] = "Error: You cannot use both - SpecTestName SubSet && SpecTestName additional definition";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }

            if (newDivLine.name != origDivLineName) 
            {
                foreach (var tmpTDV in SMPConf.table_divs[table].Values)
                    {
                        if (tmpTDV.TableDivLines.ContainsKey(newDivLine.name))
                        {
                            TempData["message"] = "Error: Div Line Name:" + newDivLine.name + " already exists, please use another name" ;
                            //return RedirectToAction("CSR_Table_Configure");
                            return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
                        }
                    }   
            }

            SMPConf.table_divs[table][DivName].updateDivLine(newDivLine, origDivLineName);
            SMPConf.useNewCSR = "Y";
            MDBSMconf.SaveTMP(SMPConf);

            //return RedirectToAction("CSR_Table_Configure");
            return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
        }


        public ActionResult CSR_Table_Div_Line_Order(string protocol, string proj, string phase, string table, string DivName, string DivLineName, string direction)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);

            if ((SMPConf.table_divs.Count > 0) && (!SMPConf.table_divs.ContainsKey(table)))
            {
                TempData["message"] = "Error: DB Table value sent:\"" + table + "\" does not exist in Conf of project";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }

            if (!SMPConf.table_divs[table].ContainsKey(DivName))
            {
                TempData["message"] = "Error: Div value sent:\"" + DivName + "\" does not exist in Conf of project";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }

            if (direction == "up") SMPConf.table_divs[table][DivName].orderUpDivLine(DivLineName);
            if (direction == "down") SMPConf.table_divs[table][DivName].orderDownDivLine(DivLineName);

            MDBSMconf.SaveTMP(SMPConf);

            //return RedirectToAction("CSR_Table_Configure");
            return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
        }

        public ActionResult CSR_Table_Div_Order(string protocol, string proj, string phase, string table, string DivName, string direction)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);

            if ((SMPConf.table_divs.Count > 0) && (!SMPConf.table_divs.ContainsKey(table)))
            {
                TempData["message"] = "Error: DB Table value sent:\"" + table + "\" does not exist in Conf of project";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }

            if (!SMPConf.table_divs[table].ContainsKey(DivName))
            {
                TempData["message"] = "Error: Div value sent:\"" + DivName + "\" does not exist in Conf of project";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }

            if (direction == "up") SMPConf.orderUpDiv(table, DivName);
            if (direction == "down") SMPConf.orderDownDiv(table, DivName);

            MDBSMconf.SaveTMP(SMPConf);

            //return RedirectToAction("CSR_Table_Configure");
            return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
        }

        public ActionResult CSR_Table_Div_UpdateReportsExec(string protocol, string proj, string phase, string table, string DivName, string SelectSRreportID, string newPRMreportIDs)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);

            if ((SMPConf.table_divs.Count > 0) && (!SMPConf.table_divs.ContainsKey(table)))
            {
                TempData["message"] = "Error: DB Table value sent:\"" + table + "\" does not exist in Conf of project";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }

            if (!SMPConf.table_divs[table].ContainsKey(DivName))
            {
                TempData["message"] = "Error: Div value sent:\"" + DivName + "\" does not exist in Conf of project";
                //return RedirectToAction("CSR_Table_Configure");
                return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
            }

            if (!String.IsNullOrEmpty(SelectSRreportID))SMPConf.table_divs[table][DivName].SRreportId = SelectSRreportID.Replace("delete","");

            List<string> ListnewPRMreportIDs = new List<string>(newPRMreportIDs.Split(','));
            int newPRMreportIDsCNT = 0;
            foreach (string r in new List<string>(newPRMreportIDs.Split(',')))
            {
                if (!String.IsNullOrEmpty(r))
                {
                    if (newPRMreportIDsCNT < SMPConf.table_divs[table][DivName].reportIds.Count) SMPConf.table_divs[table][DivName].reportIds[newPRMreportIDsCNT] = r.Replace("delete", "");
                    else SMPConf.table_divs[table][DivName].reportIds.Add(r.Replace("delete", ""));
                }
                newPRMreportIDsCNT++;
            }

            List<string> reportIdsTMP = new List <string> (SMPConf.table_divs[table][DivName].reportIds);
            SMPConf.table_divs[table][DivName].reportIds.Clear();
            foreach (string r in reportIdsTMP) if (!String.IsNullOrEmpty(r)) SMPConf.table_divs[table][DivName].reportIds.Add(r);

            MDBSMconf.SaveTMP(SMPConf);

            //return RedirectToAction("CSR_Table_Configure");
            return RedirectToAction("CSR_Table_Configure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj, selectedTable = table }));
        }


        public ActionResult ConfigureSM_CSR(string protocol, string proj, string phase, string selectedTable)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);

            List<string> tablelist = CPH.filterTablesPerProtocol(new List<string>(MongoTables));
            tablelist.Insert(0, "");
            ViewBag.collist = tablelist;
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            if ((CPMetaData == null) || (string.IsNullOrEmpty(CPMetaData._id)) || (CPMetaData.IndicatorsMeta.Count == 0))
            {
                //TODO: improve message, need to define many things...
                TempData["message"] = "Error: you must define at least one table in 'Define Meta Data' in order to configure SM/CSR";
                return RedirectToAction("Main");
            }
            List<string> projectTables = new List<string>(CPMetaData.IndicatorsMeta.Keys);
            ViewBag.ProjectColList = projectTables;
            ViewBag.selectedCol = selectedTable;

            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.Read(protocol, proj, phase);
            var TablesNotInProject = SMPConf.tables.Except(CPMetaData.IndicatorsMeta.Keys).ToList();
            if (TablesNotInProject.Any())
            {
                string add_table = "";
                foreach (string table in TablesNotInProject) add_table += table + ",";
                ViewBag.TablesWarning = "Warning: SM/CSR Configuration contains the following table \"" + add_table + "\" which does not defined in current project";
            }

            var secondNotFirst = CPH.GetPortsOfProject().Except(SMPConf.ports).ToList();
            if (secondNotFirst.Any()) 
            {
                string add_ports = "";
                foreach (string p in secondNotFirst) add_ports += p + ",";
                ViewBag.PortsWarning = "Warning: Coverage Rules contatins additional ports:" + add_ports + " ; "; 
            }

            secondNotFirst = CPH.GetLanesOfProject().Except(SMPConf.lanes).ToList();
            if (secondNotFirst.Any())
            {
                string add_lanes = "";
                foreach (string l in secondNotFirst) add_lanes += l + ",";
                ViewBag.LanesWarning = "Warning: Actice Coverage Rules contatins additional lanes:" + add_lanes + " (CSR target numbers will be wrong!!!)";
            }

            List<string> ConfUnitsStrings = new List<string>();
            foreach (Unit _unit in SMPConf.units) ConfUnitsStrings.Add(_unit.ToString());
            secondNotFirst = CPH.GetUnitsOfProject().Except(ConfUnitsStrings).ToList();

            if (secondNotFirst.Any())
            {
                string add_units = "";
                foreach (string u in secondNotFirst) add_units += u + ",";
                ViewBag.UnitsWarning += "Warning: Project contatins additional units:" + add_units + " (CSR will not show those units)";
            }

            var FirstNotsecond = ConfUnitsStrings.Except(CPH.GetUnitsOfProject()).ToList();

            if (FirstNotsecond.Any())
            {
                string add_units = "";
                foreach (string u in FirstNotsecond) add_units += u + ",";
                ViewBag.UnitsWarning += "Warning: SM/CSR configuration contatins units not exist in Project:" + add_units;
            }


            return View(SMPConf);
        }


        public ActionResult ConfigureSM_CSRexec(string protocol, string proj, string phase, string tableNew1,string ports, string lanes,  string units, string separateCSRperTable)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.Read(protocol, proj, phase);
            SessionsManageProtocolConf SMPconfSaved = MDBSMconf.Read(protocol, proj, phase);
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            List<string> projectTables = new List<string>(CPMetaData.IndicatorsMeta.Keys);
            if (string.IsNullOrEmpty(SMPConf.protocol))
            {
                SMPConf.protocol = protocol;
                SMPConf.proj = proj;
                SMPConf.phase = phase;
                foreach (string table in CPMetaData.IndicatorsMeta.Keys) if (!SMPConf.tables.Contains(table)) SMPConf.tables.Add(table);
            }
            SMPConf.tables.Clear();
            for (int i = 0; i < 20; i++)
            {
                string table = Request.Form["smTable" + i.ToString()];
                if (String.IsNullOrEmpty(table)) break;
                if (projectTables.Contains(table)) SMPConf.tables.Add(table);
                else
                {
                    TempData["message"] += "Error: Project does not contain table \"" + table + '"';
                    return RedirectToAction("ConfigureSM_CSR");
                }
            }
            if (!String.IsNullOrEmpty(tableNew1))
            {
                if (SMPConf.tables.Contains(tableNew1))
                {
                    TempData["message"] += "Error: SM/CSR configuration already contain table \"" + tableNew1 +'"';
                    return RedirectToAction("ConfigureSM_CSR");
                }
                SMPConf.tables.Add(tableNew1);
            }
            if (SMPConf.tables.Count == 0)
            {
                TempData["message"] += "Error: SM/CSR configuration must contain 1 table ";
                return RedirectToAction("ConfigureSM_CSR");
            }
            ////
            SMPConf.fields.Clear();
            SMPConf.fieldsAliases.Clear();
            for (int i = 0; i < 20; i++)
            {
                string field = Request.Form["field" + i.ToString()];
                if (String.IsNullOrEmpty(field)) break;
                string fieldA = Request.Form["fieldA" + i.ToString()];
                SMPConf.fields.Add(field);
                if (!String.IsNullOrEmpty(fieldA)) SMPConf.fieldsAliases.Add(fieldA);
                else SMPConf.fieldsAliases.Add(field);
            }
            for (int i = 1; i < 4; i++)
            {
                string field = Request.Form["fieldNew" + i.ToString()];
                if (String.IsNullOrEmpty(field)) break;
                string fieldA = Request.Form["fieldNewA" + i.ToString()];
                SMPConf.fields.Add(field);
                if (!String.IsNullOrEmpty(fieldA)) SMPConf.fieldsAliases.Add(fieldA);
                else SMPConf.fieldsAliases.Add(field);
            }
            SMPConf.separateCSRperTable = separateCSRperTable;
            SMPConf.ports.Clear();
            foreach (string port in (new List<string>(ports.Split(','))))
            {
                if (!String.IsNullOrEmpty(port)) SMPConf.ports.Add(port.ToUpper());
            }
            SMPConf.lanes.Clear();
            foreach (string lane in (new List<string>(lanes.Split(','))))
            {
                if (!String.IsNullOrEmpty(lane)) SMPConf.lanes.Add(lane.ToUpper());
            }
            //New: auto update of SMPConf.SRTemplate
            SMPConf.SRTemplate = "P";
            foreach (string p in SMPConf.ports)
            {
                if (p.StartsWith("P")) SMPConf.SRTemplate += p.Replace("P","");
                if (p == "SRC0") SMPConf.SRTemplate += "0";
            }

            if ((SMPConf.lanes.Count == 1) && (SMPConf.lanes[0] == "NA")) SMPConf.SRTemplate += "LNA";
            if (SMPConf.lanes.Count == 2) SMPConf.SRTemplate += "L01";
            if (SMPConf.lanes.Count == 4) SMPConf.SRTemplate += "L03";

            //in PCIE CSR we put lanes in "ports" 
            if (SMPConf.protocol.StartsWith("PCIE"))
            {
                SMPConf.SRTemplate = "NPL";
                foreach (string p in SMPConf.ports)
                {
                    if (p.StartsWith("L")) SMPConf.SRTemplate += p.Replace("L", "");
                }
            }
            //
            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
            string newEmptyExcel = Path.Combine(Path.GetDirectoryName(EmptyExcel), Path.GetFileNameWithoutExtension(EmptyExcel).Replace("emptySR", "emptySR" + SMPConf.SRTemplate) + Path.GetExtension(EmptyExcel));
            if (!System.IO.File.Exists(newEmptyExcel)) SMPConf.SRTemplate = "NPL0";
            //
            SMPConf.units.Clear();
            //SortedList<string, Unit> tmpUnits = new SortedList<string, Unit>();
            
            foreach (string unitStr in (new List<string>(units.Split(','))))
            {
                if (!String.IsNullOrEmpty(unitStr))
                {
                    Unit tmpUnit = new Unit(unitStr);
                    if (tmpUnit.ToString() != unitStr)
                    {
                        TempData["message"] += "Error: unit string is not in correct format:" + unitStr;
                        RedirectToAction("ConfigureSM_CSR");
                    }
                    //tmpUnits.Add(tmpUnit.ToString(), tmpUnit);
                    SMPConf.units.Add(tmpUnit);
                }
            }
            SMPConf.units.Sort();

            //New: Dynamic fields
            string SMPconfSavedFields = "";
            foreach (string f in SMPconfSaved.fields) SMPconfSavedFields += f + ";";
            string SMPconfFields = "";
            foreach (string f in SMPConf.fields) SMPconfFields += f + ";";
            //if (SMPconfSavedFields != SMPconfFields) RebuildFieldsDataOfProtocolConf(SMPconf);  //need to be done in Background
            if (SMPconfSavedFields != SMPconfFields) SMPConf.RebuildFieldsData = "Y";

            MDBSMconf.SaveDoNotOverWriteTimestamp_ActiveFileNames(SMPConf);
            SessionsManageProtocolConf SMPConftmp = MDBSMconf.ReadTMP(protocol, proj, phase);
            if (string.IsNullOrEmpty(SMPConftmp.protocol))
            {
                SMPConftmp.protocol = protocol;
                SMPConftmp.proj = proj;
                SMPConftmp.phase = phase;
            }
            if (SMPConftmp.protocol == protocol)
            {
                SMPConftmp.tables = new List<string>(SMPConf.tables);
                SMPConftmp.fields = new List<string>(SMPConf.fields);
                SMPConftmp.fieldsAliases = new List<string>(SMPConf.fieldsAliases);
                SMPConftmp.ports = new List<string>(SMPConf.ports);
                SMPConftmp.lanes = new List<string>(SMPConf.lanes);
                SMPConftmp.units = new List<Unit>(SMPConf.units);
                SMPConftmp.separateCSRperTable = SMPConf.separateCSRperTable;
                SMPConftmp.SRTemplate = SMPConf.SRTemplate;
                MDBSMconf.SaveTMP(SMPConftmp);
            }
            return RedirectToAction("ConfigureSM_CSR");
        }

        public ActionResult ConfigureSM_copy_config_from(string protocol, string proj, string phase, string copy_from_proj, string copy_from_phase)
        {
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);

            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf newSMPConf = MDBSMconf.Read(protocol, proj, phase);
            if (newSMPConf.table_divs.Count > 0)
            {
                TempData["message"] = "Error: Configuration already exist for: \"" + protocol + " " + proj + " " + phase + "\"";
                return RedirectToAction("ConfigureSM_CSR");
            }
            SessionsManageProtocolConf refSMPConf = MDBSMconf.Read(protocol, copy_from_proj, copy_from_phase);
            if (refSMPConf.table_divs.Count == 0)
            {
                if ((copy_from_proj != proj) || (copy_from_phase != phase))
                {
                    TempData["message"] = "Error: Reference Configuration does not exist: \"" + protocol + " " + copy_from_proj + " " + copy_from_phase + "\"";
                    return RedirectToAction("ConfigureSM_CSR");
                }
                else
                {
                    //Initialize "refSMPConf" of current project with cycle plan values
                    refSMPConf.protocol = protocol;
                    refSMPConf.proj = proj;
                    refSMPConf.phase = phase;
                    foreach (string table in CPMetaData.IndicatorsMeta.Keys) if (!refSMPConf.tables.Contains(table)) refSMPConf.tables.Add(table);
                    TempData["messageInfo"] = "Notice: Reference Configuration does not exist: \"" + protocol + " " + copy_from_proj + " " + copy_from_phase + "\" Initialized tables, please fill all fields and save";
                }
            }

            //Verify tables in ref conf are defined in this project

            foreach (string table in refSMPConf.tables) if (!CPMetaData.IndicatorsMeta.Keys.Contains(table))
                {
                    TempData["message"] = "Error: Reference Configuration contains the following table \"" + table + "\" which does not defined in current project";
                    return RedirectToAction("ConfigureSM_CSR");
                }

            newSMPConf = refSMPConf;
            foreach (string table in CPMetaData.IndicatorsMeta.Keys) if (!newSMPConf.tables.Contains(table)) newSMPConf.tables.Add(table);

            newSMPConf = refSMPConf;
            newSMPConf.proj = proj;
            newSMPConf.phase = phase;
            newSMPConf.ActiveFileNames.Clear();
            MDBSMconf.SaveDoNotOverWriteTimestamp_ActiveFileNames(newSMPConf);
            SessionsManageProtocolConf SMPConftmp = MDBSMconf.ReadTMP(protocol, proj, phase);
            if (string.IsNullOrEmpty(SMPConftmp.protocol))
            {
                SMPConftmp.protocol = protocol;
                SMPConftmp.proj = proj;
                SMPConftmp.phase = phase;
            }
            if (SMPConftmp.protocol == protocol)
            {
                SMPConftmp.tables = new List<string>(newSMPConf.tables);
                SMPConftmp.fields = new List<string>(newSMPConf.fields);
                SMPConftmp.fieldsAliases = new List<string>(newSMPConf.fieldsAliases);
                SMPConftmp.ports = new List<string>(newSMPConf.ports);
                SMPConftmp.lanes = new List<string>(newSMPConf.lanes);
                SMPConftmp.units = new List<Unit>(newSMPConf.units);
                SMPConftmp.separateCSRperTable = newSMPConf.separateCSRperTable;
                MDBSMconf.SaveTMP(SMPConftmp);
            }

            return RedirectToAction("ConfigureSM_CSR");

        }

        public ActionResult CSR_Template_Deploy(string protocol, string proj, string phase)
        {
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            FullSession FS = (FullSession)System.Web.HttpContext.Current.Session["FS"];
            if (string.IsNullOrEmpty(proj)) proj = FS.LastCyclePlanProj;
            if (string.IsNullOrEmpty(proj)) proj = ProjectsList[0];
            if (string.IsNullOrEmpty(phase)) phase = FS.LastCyclePlanPhase;
            if (string.IsNullOrEmpty(phase)) phase = PhasesList[0];
            if (string.IsNullOrEmpty(protocol)) protocol = FS.LastCyclePlanProtocol;
            if (string.IsNullOrEmpty(protocol)) protocol = ProtocolsList[0];
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);

            List<string> tablelist = CPH.filterTablesPerProtocol(new List<string>(MongoTables));
            tablelist.Insert(0, "");
            ViewBag.collist = tablelist;
            ViewBag.proj = proj;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phase = phase;
            ViewBag.phaselist = new List<string>(PhasesList);
            ViewBag.protocol = protocol;
            ViewBag.protocollist = new List<string>(ProtocolsList);
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProj = proj;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanPhase = phase;
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).LastCyclePlanProtocol = protocol;

            //Not sure if needed..
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            if ((CPMetaData == null) || (string.IsNullOrEmpty(CPMetaData._id)) || (CPMetaData.IndicatorsMeta.Count == 0))
            {
                //TODO: improve message, need to define many things...
                TempData["message"] = "Error: you must define at least one table in 'Define Meta Data' in order to configure SM/CSR";
                return RedirectToAction("Main");
            }

            SessionsManageProtocolConf SMPConf = (new MDBSessionsManageConf()).Read(protocol, proj, phase);
            string Interface = protocol.Split('_')[0];
            if (Interface.StartsWith("USB")) Interface = "USB";
            ViewBag.Interface = Interface;
            if (SMPConf.separateCSRperTable == "Y") ViewBag.tables = new List<string>(SMPConf.tables);
            else ViewBag.tables = new List<string>();
            string FileName = "";
            List<string> currTemplates = new List<string>();
            if (SMPConf.separateCSRperTable != "Y")
            {
                ViewBag.TemplateFiles = new List<string>(new CycleStatusHandler().GetCSRExcelTemplates(protocol, proj, phase, "", out FileName));
                currTemplates.Add(SMPConf.getActiveFileName(FileName));
                ViewBag.currTemplates = currTemplates;
                ViewBag.TemplateFileNoTable = FileName;
            }
            else
            {
                List<string> TemplateFiles = new List<string>();
                foreach (string table in SMPConf.tables)
                {
                    TemplateFiles.AddRange(new List<string>(new CycleStatusHandler().GetCSRExcelTemplates(protocol, proj, phase, table, out FileName)));
                    currTemplates.Add(SMPConf.getActiveFileName(FileName));
                    ViewBag.TemplateFileNoTable = Path.GetFileNameWithoutExtension(FileName).Replace(table, "");
                }
                ViewBag.TemplateFiles = TemplateFiles;
                ViewBag.currTemplates = currTemplates;
            }

            return View();
        }


        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSR_TemplateDownloadFile(string protocol, string proj, string phase, string filename)
        {
            string mimeSuffix = "application/vnd.ms-excel";
            if (Path.GetExtension(filename) == "xml") mimeSuffix = "text/xml";
            string filepath = Path.Combine(ServerDir, "EVData", filename);
            if (System.IO.File.Exists(filepath))
            {
                ViewBag.ExportedFileName = filepath;
                return new FilePathResult(filepath, mimeSuffix) { FileDownloadName = filename };
            }
            else
            {
                TempData["message"] = "ERROR: cannot find file " + filename;
                return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSR_TemplateUploadFile(string protocol, string proj, string phase, string table, HttpPostedFileBase file)
        {
            string fileName = "";
            string path = "";
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
                    return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
                }
            }
            else
            {
                TempData["message"] = "Error: you must choose a file to upload";
                return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }
            if (Path.GetExtension(fileName) == ".xlsx") isEXL = true;
            if (!isEXL)
            {
                TempData["message"] = "Error: file type is not supported, must be [xlsx] not:" + fileName;
                return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }

            

            string FileName = "";
            string message = "";
            CycleStatusHandler SCRHandler = new CycleStatusHandler();
            string XMLMessage = "";
            if (isEXL)
            {
                SCRHandler.GetCSRExcelTemplates(protocol, proj, phase, table, out FileName);
            }

            if (!Path.GetFileNameWithoutExtension(fileName).StartsWith(Path.GetFileNameWithoutExtension(FileName)))
            {
                TempData["message"] = "Error: The file  - " + file.FileName + " does not have a valid prefix. Valid prefix: " + Path.GetFileNameWithoutExtension(FileName);
                return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }

            //"separateCSRperTable" flow: save Conf in order to restore other table(s)
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConfPrev = MDBSMconf.Read(protocol, proj, phase);

            SessionManageHandler SMHandler = new SessionManageHandler();
            //TODO: add conf check (? maybe not a must since XML is auto created)
            //if (!SMHandler.CheckSessionsManageConf(path, out XMLMessage))
            //{
            //    TempData["message"] = "Error:" + XMLMessage;
            //    return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            //}

            if (!SMHandler.pocLoadExcelTemplateSMProtocolConfToDB(path))      
            {
                TempData["message"] = "Error: cannot load conf to DB";
                return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }
            ////"separateCSRperTable" flow: restore other table(s) from saved Conf
            //if (SMPConfPrev.separateCSRperTable == "Y")
            //{
            //    SessionsManageProtocolConf SMPConf = MDBSessionsManageConfObj.Read(protocol, proj, phase);
            //    foreach (var TDVpair in SMPConfPrev.table_divs)
            //    {
            //        if (TDVpair.Key == table) continue;
            //        if (!SMPConf.table_divs.ContainsKey(TDVpair.Key)) SMPConf.table_divs.Add(TDVpair.Key, TDVpair.Value);
            //        else SMPConf.table_divs[TDVpair.Key] = TDVpair.Value;
            //    }
            //    MDBSessionsManageConfObj.SaveDoNotOverWriteTimestamp_ActiveFileNames(SMPConf);
            //}
            else XMLMessage = " conf was loaded to DB";


            savedInEVDATA = CCDUtilities.CopyFileSaveBackup(path, Path.Combine(ServerDir, "EVData"), Path.GetFileName(path), out message);
            if (savedInEVDATA)
            {
                TempData["messageInfo"] = "File " + fileName + " saved successfully;" + XMLMessage;

                //MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
                SessionsManageProtocolConf SMPConf = MDBSMconf.Read(protocol, proj, phase);
                SMPConf.updateActiveFileName(FileName, fileName);
                //"separateCSRperTable" flow: restore other table(s) from saved Conf
                if (SMPConfPrev.separateCSRperTable == "Y")
                {
                    foreach (var TDVpair in SMPConfPrev.table_divs)
                    {
                        if (TDVpair.Key == table) continue;
                        if (!SMPConf.table_divs.ContainsKey(TDVpair.Key)) SMPConf.table_divs.Add(TDVpair.Key, TDVpair.Value);
                        else SMPConf.table_divs[TDVpair.Key] = TDVpair.Value;
                    }
                }
                MDBSMconf.SaveDoNotOverWriteTimestamp(SMPConf);
            }

            else TempData["message"] = "Error while saving file:" + message + XMLMessage;
            return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));

        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSR_TemplateMakeFileActive(string protocol, string proj, string phase, string table, string fileName)
        {
            string path = Path.Combine(ServerDir, "EVData", fileName);

            bool isEXL = false;

            if (Path.GetExtension(fileName) == ".xlsx") isEXL = true;

            string FileName = "";
            CycleStatusHandler SCRHandler = new CycleStatusHandler();
            string XMLMessage = "";
            if (isEXL)
            {
                SCRHandler.GetCSRExcelTemplates(protocol, proj, phase, table, out FileName);
            }
            else
            {
                TempData["message"] = "Error: The file  - " + fileName + " does not have a valid prefix. Valid prefix: " + Path.GetFileNameWithoutExtension(FileName);
                return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }

            //"separateCSRperTable" flow: save Conf in order to restore other table(s)
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConfPrev = MDBSMconf.Read(protocol, proj, phase);

            SessionManageHandler SMHandler = new SessionManageHandler();
            //TODO: add conf check (? maybe not a must since XML is auto created)
            //if (!SMHandler.CheckSessionsManageConf(path, out XMLMessage))
            //{
            //    TempData["message"] = "Error:" + XMLMessage;
            //    return RedirectToAction("CSRConfigure", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            //}
            if (!SMHandler.pocLoadExcelTemplateSMProtocolConfToDB(path))  
            {
                TempData["message"] = "Error: cannot load conf to DB";
                return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
            }
            ////"separateCSRperTable" flow: restore other table(s) from saved Conf
            //if (SMPConfPrev.separateCSRperTable == "Y")
            //{
            //    SessionsManageProtocolConf SMPConf = MDBSessionsManageConfObj.Read(protocol, proj, phase);
            //    foreach (var TDVpair in SMPConfPrev.table_divs)
            //    {
            //        if (TDVpair.Key == table) continue;
            //        if (!SMPConf.table_divs.ContainsKey(TDVpair.Key)) SMPConf.table_divs.Add(TDVpair.Key, TDVpair.Value);
            //        else SMPConf.table_divs[TDVpair.Key] = TDVpair.Value;
            //    }
            //    MDBSessionsManageConfObj.SaveDoNotOverWriteTimestamp_ActiveFileNames(SMPConf);
            //}
            else XMLMessage = " conf was loaded to DB;";



            TempData["messageInfo"] = "File " + fileName + " is now active;" + XMLMessage;

            //MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.Read(protocol, proj, phase);
            SMPConf.updateActiveFileName(FileName, fileName);
            //"separateCSRperTable" flow: restore other table(s) from saved Conf
            if (SMPConfPrev.separateCSRperTable == "Y")
            {
                foreach (var TDVpair in SMPConfPrev.table_divs)
                {
                    if (TDVpair.Key == table) continue;
                    if (!SMPConf.table_divs.ContainsKey(TDVpair.Key)) SMPConf.table_divs.Add(TDVpair.Key, TDVpair.Value);
                    else SMPConf.table_divs[TDVpair.Key] = TDVpair.Value;
                }
            }
            MDBSMconf.SaveDoNotOverWriteTimestamp(SMPConf);

            return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
        }
        [Authorize(Roles = "CCD_EV")]
        public ActionResult CSR_TemplateDeleteFile(string protocol, string proj, string phase, string filename)
        {
            System.IO.File.Delete(Path.Combine(ServerDir, "EVData", filename));
            return RedirectToAction("CSR_Template_Deploy", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
        }

        [HttpPost]
        public ActionResult update_excelPorts_ajax(string protocol, string proj, string phase, string excelPorts)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageProtocolConf SMPConf = MDBSMconf.ReadTMP(protocol, proj, phase);
            if (String.IsNullOrEmpty(SMPConf.protocol))
            {
                Response.Write("New CSR conf is empty; Excel Ports assigning failed:'" + excelPorts + "'");
                return null;
            }
            SMPConf.excelPorts.Clear();
            foreach (string eport in (new List<string>(excelPorts.Split(','))))
            {
                if (!String.IsNullOrEmpty(eport)) SMPConf.excelPorts.Add(eport.ToUpper());
            }
           
            //report rename reference:
            bool s;
            s = MDBSMconf.SaveTMP(SMPConf);
            if (s) Response.Write("Excel Ports assigning succeeded:'" + excelPorts + "'");
            else Response.Write("Excel Ports assigning failed:'" + excelPorts + "'");
            return null;
        }

        
        [HttpPost]
        public ActionResult SearchDupSpecTestNameAssigned(string protocol, string proj, string phase)
        {
            string header = "The follwing duplicates were found:\n";
            List<string> messagesAll = new List<string>();
            List<string> messages = new List<string>();
            CyclePlanHandler CPH = new CyclePlanHandler(protocol, proj, phase);
            CPH.CreateCyclePlanCSRLookUP(true);
            
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, proj, phase);
            foreach (string table in CPMetaData.IndicatorsMeta.Keys)
            {
                messages.Clear();
                MDBCyclePlanCSRLookUP MDBCP_CSRLookUP = new MDBCyclePlanCSRLookUP(protocol, proj, phase, table);
                MongoCursor Cur = MDBCP_CSRLookUP.MDBFind("GT;duplicates;0", "AND", "ASC", "_id");
                foreach (BsonDocument Doc in Cur) messages.Add(Doc.GetValue("_id").ToString());
                if (messages.Count > 0)
                {
                    messagesAll.Add("Table: " + table);
                    messagesAll.AddRange(messages);
                }
            }
            if (messagesAll.Count > 0)
            {
                string reportFileName = Path.Combine(ServerDir, "tmp", "validateCyclePlan_" + protocol + "_" + proj + "_" + phase + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
                System.IO.File.AppendAllText(reportFileName, header);
                System.IO.File.AppendAllLines(reportFileName, messagesAll);
                ViewBag.ExportedFileNAme = reportFileName;
                return new FilePathResult(reportFileName, "text/xml") { FileDownloadName = reportFileName };
            }
            TempData["messageInfo"] = "Configuration is well, no duplicates were found ";
            return RedirectToAction("ValidateProject", new RouteValueDictionary(new { protocol = protocol, phase = phase, proj = proj }));
        }
        // GET: /ManageDB/test

        public ActionResult test()
        {
            return View();
        }

        public ActionResult test1()
        {
            throw new NotImplementedException();
            return View();
        }

    }
}
