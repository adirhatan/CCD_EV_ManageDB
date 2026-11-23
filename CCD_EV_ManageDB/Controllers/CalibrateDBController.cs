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
    [LogActionFilter]
    public class CalibrateDBController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ManageDBController));
        private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }

        private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        private readonly int DataTableLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DataTableLimit"]);

        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }
        private static string DB_CMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP"]; } }
        private static string DB_CMP_Arch { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP_Arch"]; } }

        //private MongoDBClient db = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);

        private List<string> lop = new List<string>(new string[] { "EQ", "Matches", "NotMatches", "In", "NotIn", "GT", "LT" });
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }
        public string[] MongoTables { get { return System.Configuration.ConfigurationManager.AppSettings["MongoTables"].Split(','); } }
        public string[] CompTables { get { return System.Configuration.ConfigurationManager.AppSettings["CertificationTables"].Split(','); } }
        public string[] ProjectsList { get { return System.Configuration.ConfigurationManager.AppSettings["ProjectsList"].Split(','); } }
        public string[] PhasesList { get { return System.Configuration.ConfigurationManager.AppSettings["PhasesList"].Split(','); } }

        //
        // GET: /InventoryDB/

        public ActionResult Index()
        {
            return RedirectToAction("CalibrateLines");
        }

        public ActionResult CalibrationLinesClear(DBsearch obj, string arcdb)
        {
            DBsearch DBS = new DBsearch();
            DBS.col = "Calibration";
            DBS.db = DB_PRM;
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CalibrationSearch = new DBsearchSave(DBS);
            return RedirectToAction("CalibrationLines");
        }

        public ActionResult CalibrationLines(DBsearch obj, string arcdb)
        {
            DBsearch DBS = new DBsearch();
            DBS.col = "Calibration";
            DBS.db = DB_PRM;
            if (!string.IsNullOrWhiteSpace(arcdb) && (arcdb.ToUpper() == "TRUE")) DBS.db = DB_CMP_Arch;
            ViewBag.q0list = lop;

            if (obj.Query == null)
            {

                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    (((FullSession)System.Web.HttpContext.Current.Session["FS"]).CalibrationSearch.Query != null)
                    && (((FullSession)System.Web.HttpContext.Current.Session["FS"]).CalibrationSearch.db == DBS.db))
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CalibrationSearch.getNewDBS();
                }
                else
                {
                obj.Query = "Matches;_id;/^./";
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
            MDBCalibration db = new MDBCalibration();
            DBS.ResCursor = db.MDBFind(DBS.Query, DBS.SearchOP, DBS.SortDirection, DBS.SortStr);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            DBS.TableFields = (new Calibration()).GetCalibrationFields();
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).CalibrationSearch = new DBsearchSave(DBS);
            return View("CalibrationLines", DBS);
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult CreateLine()
        {
            return View("EditLine", new Calibration());
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult EditLine(string _id)
        {
            List<string> reflist = (new MDBADMReferenceTable()).ReadListAllADMReferenceTablesIDS();
            reflist.Insert(0, "");
            ViewBag.reflist = reflist;
            ViewBag.projlist = new List<string>(ProjectsList);
            ViewBag.phaselist = new List<string>(PhasesList);


            Calibration line = (new MDBCalibration()).Read(_id);

            return View("EditLine", line);
        }

        
        [Authorize(Roles = "CCD_EV")]
        public ActionResult EditLineExec(string _id, string _Spec,string _CTS,string _Station,string _Project,string _Phase,string _Rate,string _Setup,string _Interface,string _ExecutionDate,string _File,string _User,string _ExpirationDays)
        {
            double number;

            if ((!Double.TryParse(_ExecutionDate, out number)) || (number > (double)25001231) || (number < (double)20190101))
            {
                TempData["message"] = "Reception Date should be YYYYMMDD format";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }
            if ((!Double.TryParse(_ExpirationDays, out number)))
            {
                TempData["message"] = "Expiration Days should be number (defualt is 30)";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }

            if ((string.IsNullOrEmpty(_Spec)) || (string.IsNullOrEmpty(_CTS)) || (string.IsNullOrEmpty(_Station)) || (string.IsNullOrEmpty(_Rate)) || (string.IsNullOrEmpty(_Setup)) || (string.IsNullOrEmpty(_User)))
            {
                TempData["message"] = "All fields are mandatory";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }

            MDBCalibration mdbCalibration = new MDBCalibration();
            Calibration newCal = new Calibration(_Spec, _CTS, _Station, _Project, _Phase, _Rate, _Setup, _Interface, _ExecutionDate, _File, _User, _ExpirationDays);
            if (!String.IsNullOrWhiteSpace(_id) && (_id != newCal.id))
            {
                TempData["message"] = "You cannot change exist item name, please delete item and recreate one";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }
            else
            {
                //in case of new calibration, check if we already have one with this id 
                if (String.IsNullOrWhiteSpace(_id))
                {
                    Calibration tCal = mdbCalibration.Read(newCal.id);
                    if (tCal.id == newCal.id)
                    {
                        TempData["message"] = "You cannot create calibration '" + newCal.id + "' because it is already exists in DB";
                        return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
                    }
                }

                ////update exist calibration - if we need to save prev items?? (I think we save all "prev" calibration as non valid...)
                //else
                //{
                //    Inventory lastInv = mdbInventory.ReadInventory(_id);
                //    if ((lBIOS != lastInv.BIOS) || (lNMV != lastInv.NVM))
                //    {
                //        lastInv.SavedItems.Add(new InvSaved(lastInv.BIOS, lastInv.NVM, DateTime.Now.ToString("yyyyMMdd")));
                //    }
                //    newInv.SavedItems = lastInv.SavedItems;
                //    newInv.Reports = lastInv.Reports;
                //    newInv.UnitIds = lastInv.UnitIds;
                //    newInv.PFinishDates = lastInv.PFinishDates;
                //}
            }
            if (!mdbCalibration.Save(newCal))
            {
                TempData["message"] = "Edit calibration failed due to internal error, please try again later";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }

            return RedirectToAction("CalibrationLines");
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult EditLineFile(string _id, HttpPostedFileBase file)
        {
            string fileName = "";
            string path = "";

            if ((file != null) && (file.ContentLength > 0))
            {
                fileName = Path.GetFileName(file.FileName);
                path = Path.Combine(ServerDir, "Calibrations", "Files", _id);
                try
                {
                    Directory.CreateDirectory(path);
                    path = Path.Combine(path, fileName);
                    file.SaveAs(path);
                }
                catch (Exception e)
                {
                    TempData["message"] = "Error: cannot save Report file " + e.Message;
                    return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
                }
            }
            else
            {
                TempData["message"] = "You must choose a file to save.";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }
            MDBCalibration mdbCalibration = new MDBCalibration();
            Calibration Cal = mdbCalibration.Read(_id);
            if (Cal.File != path)
            {
                Cal.File = path;
                mdbCalibration.Save(Cal);
            }

            return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
        }


        [Authorize(Roles = "CCD_EV")]
        public ActionResult DeleteLine(string _id)
        {
            if (!(new MDBCalibration()).Delete(_id)) TempData["message"] = "Delete calibration failed due to internal error, please try again later";
            return RedirectToAction("CalibrationLines");
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult ValidationLineUpdate(string _id, string v)
        {
            MDBCalibration mdbCalibration = new MDBCalibration();
            Calibration existCal = mdbCalibration.Read(_id);
            if (String.IsNullOrWhiteSpace(existCal.id))
            {
                TempData["message"] = "Can not find Calibration ID:" + _id;
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }
             if (v=="false") existCal.Valid = false; //TODO: update all results used this validation to "Exclude Indicator" ot do it in batch
             if (v == "true") existCal.Valid = true; // needed?
            if (!(new MDBCalibration()).Save(existCal)) TempData["message"] = "Invalidate calibration failed due to internal error, please try again later";
            return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
        }

        [Authorize(Roles = "CCD_EV")]
        public ActionResult ViewCalibrationFile(string filepath, string _id , string Source)
        {
            if (System.IO.File.Exists(filepath)) return new FilePathResult(filepath, "application/vnd.openxmlformats-officedocument.wordprocessingml.document") { FileDownloadName = Path.GetFileName(filepath) };
            else
            {
                TempData["message"] = "ERROR: cannot find file " + Path.GetFileName(filepath);
                return RedirectToAction(Source, new RouteValueDictionary(new { _id = _id }));
            }
        }

    }
}
