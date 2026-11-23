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
    public class InventoryDBController : Controller
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
        //
        // GET: /InventoryDB/

        public ActionResult Index()
        {
            return RedirectToAction("InventoryLines");
        }

        public ActionResult InventoryLinesClear(DBsearch obj, string arcdb)
        {
            DBsearch DBS = new DBsearch();
            DBS.col = "Inventory";
            DBS.db = DB_CMP;
            if (!string.IsNullOrWhiteSpace(arcdb) && (arcdb.ToUpper() == "TRUE")) DBS.db = DB_CMP_Arch;
            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).InvSearch = new DBsearchSave(DBS);
            if (!string.IsNullOrWhiteSpace(arcdb) && (arcdb.ToUpper() == "TRUE")) return RedirectToAction("InventoryLines", new RouteValueDictionary(new { arcdb = "true" }));
            else return RedirectToAction("InventoryLines");
        }
        public ActionResult InventoryLines(DBsearch obj,string arcdb)
        {
            DBsearch DBS = new DBsearch();
            DBS.col = "Inventory";
            DBS.db = DB_CMP;
            if (!string.IsNullOrWhiteSpace(arcdb) && (arcdb.ToUpper() == "TRUE")) DBS.db = DB_CMP_Arch;
            ViewBag.q0list = lop;

            if (obj.Query == null)
            {

                if ((System.Web.HttpContext.Current.Session["FS"] != null) &&
                    (((FullSession)System.Web.HttpContext.Current.Session["FS"]).InvSearch.Query != null)
                    && (((FullSession)System.Web.HttpContext.Current.Session["FS"]).InvSearch.db == DBS.db))
                {
                    obj = ((FullSession)System.Web.HttpContext.Current.Session["FS"]).InvSearch.getNewDBS();
                }
                else
                {
                obj.Query = "Matches;_id;/^./";
                obj.SearchOP = "AND";
                obj.SortStr = "ReceptionDate";
                obj.SortDirection = "DES";
                }

            }

            DBS.Query = obj.Query;
            DBS.SearchOP = obj.SearchOP;
            DBS.SortStr = obj.SortStr;
            DBS.SortDirection = obj.SortDirection;
            if ((DBS.SortStr == null) || (DBS.SortStr == "")) DBS.SortStr = "ReceptionDate";
            
            String[] qarr = DBS.Query.Split(';');
            for (int i = 0; i < qarr.Length; i += 3)
            {
                DBS.QueryTriplets.Add((qarr[i]) + ";" + (qarr[i + 1]) + ";" + (qarr[i + 2]));
            }
            
 
            MDBInventory db = new MDBInventory();
            //New Arch support
            if (!string.IsNullOrWhiteSpace(arcdb) && (arcdb.ToUpper() == "TRUE")) db = new MDBInventory("DB_CMP_Arch");

            DBS.ResCursor = db.MDBFind(DBS.Query, DBS.SearchOP, DBS.SortDirection, DBS.SortStr);
            ViewBag.numberOfResults = DBS.ResCursor.Count().ToString();
            DBS.TableFields = (new Inventory()).GetInventoryFields();

            if (System.Web.HttpContext.Current.Session["FS"] == null) System.Web.HttpContext.Current.Session["FS"] = new FullSession();
            ((FullSession)System.Web.HttpContext.Current.Session["FS"]).InvSearch = new DBsearchSave(DBS);

            if (Roles.IsUserInRole("CCD_EV_COMP")) 
            {
                if ((!string.IsNullOrWhiteSpace(arcdb)) && (arcdb.ToUpper() == "TRUE")) return View("InventoryArchLines", DBS);
                else return View("InventoryLines", DBS);
            }

            if ((!string.IsNullOrWhiteSpace(arcdb)) && (arcdb.ToUpper() == "TRUE")) return View("InventoryArchLinesV", DBS);
            return View("InventoryLinesV", DBS);
        }

        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult CreateLine()
        {
            return View("EditLine", new Inventory());
        }

        //Depricated
        public ActionResult CopyLine(string _id)
        {
            MDBInventory dbLine = new MDBInventory();
            Inventory Inv = dbLine.ReadInventory(_id);
            if (String.IsNullOrWhiteSpace(Inv.platform))
            {
                TempData["message"] = "ERROR: cannot copy report";
                return RedirectToAction("InventoryLines");
            }
            string c_name = Inv.platform + "_copy";
            int cnt = 0;
            while (!String.IsNullOrWhiteSpace(dbLine.ReadInventory(c_name).platform))
            {
                cnt++;
                c_name = Inv.platform + "_copy" + cnt.ToString();
                if (cnt > 100)
                {
                    TempData["message"] = "ERROR: cannot copy Inventory";
                    return RedirectToAction("InventoryLines");
                }
            }
            Inv.platform = c_name;
            if (!dbLine.SaveInventory(Inv))
            {
                TempData["message"] = "ERROR: cannot copy inventory";
                return RedirectToAction("InventoryLines");
            }
            return RedirectToAction("InventoryLines");
        }
        //

        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult EditLine(string _id)
        {
            Inventory line = (new MDBInventory()).ReadInventory(_id);

            return View("EditLine", line);
        }

        public ActionResult DetailedLine(string _id)
        {
            Inventory line = (new MDBInventory()).ReadInventory(_id);

            return View("DetailedLine", line);
        }
        
        public ActionResult DetailedArchLine(string _id)
        {
            Inventory line = (new MDBInventory("DB_CMP_Arch")).ReadInventory(_id);

            return View("DetailedArchLine", line);
        }

        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult EditLineExec(string _id, string lplatform, string lproject, string lVendor, string lUnitsNumber, string lReceptionDate, string lBIOS, string lNMV, string lFinishDate, string lPorts, string lDevType, string[] HDMISupport, string lPreset10G0, string lPreset10G1, string lPreset10G2, string lPreset10G3, string lPreset10GAMA0, string lPreset10GAMA1, string lPreset10GAMA2, string lPreset10GAMA3, string lPreset20G0, string lPreset20G1, string lPreset20G2, string lPreset20G3, string lUSB2Amp, HttpPostedFileBase file)
        {
            double number;
            bool lHDMISupport = false;

            if ((!Double.TryParse(lReceptionDate, out number)) || (number > (double)25001231) || (number < (double)20150101))
            {
                TempData["message"] = "Reception Date should be YYYYMMDD format";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }
            if ((!Double.TryParse(lFinishDate, out number)) || (number > (double)25001231) || ((number < (double)20150101) && (number > (double)0)))
            {
                TempData["message"] = "Finish Date should be YYYYMMDD format or 0";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }

            if (HDMISupport != null) lHDMISupport = (HDMISupport[0] == "Y");

            MDBInventory mdbInventory = new MDBInventory();
            Inventory newInv = new Inventory(lplatform, lproject, lVendor, lUnitsNumber, lReceptionDate, lBIOS, lNMV, DateTime.Now.ToString("yyyyMMdd"), lFinishDate, lPorts, lDevType, lHDMISupport, lPreset10G0, lPreset10G1, lPreset10G2, lPreset10G3, lPreset10GAMA0, lPreset10GAMA1, lPreset10GAMA2, lPreset10GAMA3, lPreset20G0, lPreset20G1, lPreset20G2, lPreset20G3, lUSB2Amp);
            if (!String.IsNullOrWhiteSpace(_id) && (_id != lplatform))
            {
                TempData["message"] = "You cannot change exist item name, please delete item and recreate one";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }
            else
            {
                //in case of new platform, check if we already have one with this name in Inventory or ArchInventory
                if (String.IsNullOrWhiteSpace(_id))
                {
                    Inventory tInv = mdbInventory.ReadInventory(lplatform);
                    if (tInv.platform == lplatform)
                    {
                        TempData["message"] = "You cannot create platform '" + lplatform + "' because it is already exists in Inventory";
                        return View("EditLine", new Inventory());
                    }
                    MDBInventory mdbArchInventory = new MDBInventory("DB_CMP_Arch");
                    tInv = mdbArchInventory.ReadInventory(lplatform);
                    if (tInv.platform == lplatform)
                    {
                        TempData["message"] = "You cannot create platform '" + lplatform + "' because it is already exists in Archive Inventory";
                        return View("EditLine", new Inventory());
                    }
                }
                    
                //update exist platform
                else 
                {
                    Inventory lastInv = mdbInventory.ReadInventory(_id);
                    if ((lBIOS != lastInv.BIOS) || (lNMV != lastInv.NVM))
                    {
                        lastInv.SavedItems.Add(new InvSaved(lastInv.BIOS, lastInv.NVM, DateTime.Now.ToString("yyyyMMdd")));
                    }
                    newInv.SavedItems = lastInv.SavedItems;
                    newInv.Reports = lastInv.Reports;
                    newInv.UnitIds = lastInv.UnitIds;
                    newInv.PFinishDates = lastInv.PFinishDates;
                }
            }
            if (!mdbInventory.SaveInventory(newInv))
            {
                TempData["message"] = "Edit inventory failed due to internal error, please try again later";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }

            return RedirectToAction("InventoryLines");
        }

        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult EditLineReport(string _id,  HttpPostedFileBase file)
        {
            string fileName = "";
            string path = "";

            if ((file != null) && (file.ContentLength > 0))
            {
                fileName = Path.GetFileName(file.FileName);
                path = Path.Combine(ServerDir, "Certification", "Reports", _id);
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
                TempData["message"] = "You must choose a report file to save.";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }
            MDBInventory mdbInventory = new MDBInventory();
            Inventory lastInv = mdbInventory.ReadInventory(_id);
            if (!lastInv.Reports.Contains(path)) lastInv.Reports.Add(path);
            mdbInventory.SaveInventory(lastInv);

            return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
        }
        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult DeleteLineReport(string _id, string reportPath)
        {
            MDBInventory mdbInventory = new MDBInventory();
            Inventory lastInv = mdbInventory.ReadInventory(_id);


            if (reportPath != null) 
            {
                if (lastInv.Reports.Contains(reportPath)) lastInv.Reports.Remove(reportPath);
                //path = Path.Combine(ServerDir, "Certification", "Reports", _id);
                try
                {
                    System.IO.File.Delete(reportPath);
                }
                catch (Exception e)
                {
                    TempData["message"] = "Error: cannot delete Report file: " + Path.GetFileName(reportPath) + " " + e.Message;
                    return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
                }
            }
            else
            {
                TempData["message"] = "You must choose a report file to save.";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }

            mdbInventory.SaveInventory(lastInv);

            return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
        }
        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult EditLineUnitId(string _id, string UnitId)
        {
            if ((_id == null) || (String.IsNullOrWhiteSpace(UnitId)))
            {
                TempData["message"] = "Error: cannot add empty UnitId " ;
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }
            MDBInventory mdbInventory = new MDBInventory();
            Inventory lastInv = mdbInventory.ReadInventory(_id);
            if (!lastInv.UnitIds.Contains(UnitId)) lastInv.UnitIds.Add(UnitId);
            mdbInventory.SaveInventory(lastInv);

            return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
        }
        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult DeleteLineUnitId(string _id, string UnitId)
        {
            MDBInventory mdbInventory = new MDBInventory();
            Inventory lastInv = mdbInventory.ReadInventory(_id);


            if (UnitId != null)
            {
                if (lastInv.UnitIds.Contains(UnitId)) lastInv.UnitIds.Remove(UnitId);
                try
                {
                    System.IO.File.Delete(UnitId);
                }
                catch (Exception e)
                {
                    TempData["message"] = "Error: cannot delete UnitId: " + UnitId + " " + e.Message;
                    return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
                }
            }
            else
            {
                TempData["message"] = "You must choose UnitId to delete.";
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }

            mdbInventory.SaveInventory(lastInv);

            return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
        }
        //Try of request parsing: Form all parameters
        public ActionResult EditLineExecTry()
        {
            foreach (string key in Request.Form.AllKeys)
            {
                var queryValue = Request.Form[key];
            }
           
            return RedirectToAction("InventoryLines");
        }
        //End of Try



        [HttpPost]
        public ActionResult SummaryLine(string _id,string NVM, string IsArch)
        {
            bool Arch = (IsArch == "1");
            CompSummaryBase Summary = new CompSummaryBase();
            try
            {
                Response.Write(Summary.GetResultsSumHtml(_id, NVM, Arch));
            }
            catch (Exception e)
            {
                Response.Write("Error occured while Querying DB results summary for " + _id + " " + NVM + ":" + e.Message);
            }

            return null;
        }

        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult DeleteLine(string _id)
        {
            if (!(new MDBInventory()).DeleteInventory(_id)) TempData["message"] = "Delete inventory failed due to internal error, please try again later";
            return RedirectToAction("InventoryLines");
        }

        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult MoveToArch(string _id)
        {
            Inventory InvLine = (new MDBInventory()).ReadInventory(_id);
            string[] DBTables = CompTables;
            //DBTables = new string [] {"DP_cert_test"};
            foreach (string table in DBTables)
            {
                if (!(new MDBStationResults("DB_CMP", table)).MoveResToCompArch(_id))
                {
                    TempData["message"] = "An error occured while archiving platform " + '"' + _id + '"' + "table:" + table ;
                    return RedirectToAction("InventoryLines");
                }
            }
            //Save in DB_CMP_Arch and remove from Inventory
            if ((new MDBInventory("DB_CMP_Arch")).SaveInventory(InvLine))
            {
                if (!(new MDBInventory()).DeleteInventory(_id))
                {
                    TempData["message"] = "An error occured while deleting platform " + '"' + _id + '"' + " from Inventory table";
                    return RedirectToAction("InventoryLines");
                }

                TempData["message"] = "Inventory platform " + '"' + _id + '"' + " is now archived";
            }
            else TempData["message"] = "An error while saving Inventory platform line " + '"' + _id + '"' + " in archive, results were archived";

            return RedirectToAction("InventoryLines");
        }

        [Authorize(Roles = "CCD_EV_COMP")]
        public ActionResult MoveFromArch(string _id)
        {
            Inventory InvArchLine = (new MDBInventory("DB_CMP_Arch")).ReadInventory(_id);
            string[] DBTables = CompTables;
            //DBTables = new string[] { "DP_cert_test" };
            foreach (string table in DBTables)
            {
                if (!(new MDBStationResults("DB_CMP_Arch", table)).MoveResFromCompArch(_id))
                {
                    TempData["message"] = "An error occured while moving platform " + '"' + _id + '"' + "table:" + table;
                    return RedirectToAction("InventoryLines", new RouteValueDictionary(new { arcdb = "true" }));
                }
            }
            //Save in Inventory and remove from DB_CMP_Arch Inventory
            InvArchLine.PFinishDates.Add(InvArchLine.FinishDate);
            InvArchLine.PFinishDates.Sort();
            InvArchLine.FinishDate = "0";
            if ((new MDBInventory()).SaveInventory(InvArchLine))
            {
                if (!(new MDBInventory("DB_CMP_Arch")).DeleteInventory(_id))
                {
                    TempData["message"] = "An error occured while deleting platform " + '"' + _id + '"' + " from Archive, results were moved to active COMP db";
                    return RedirectToAction("InventoryLines", new RouteValueDictionary(new { arcdb = "true" }));
                }

                TempData["message"] = "Inventory platform " + '"' + _id + '"' + " is now active ";
            }
            else TempData["message"] = "An error while saving Inventory platform line " + '"' + _id + '"' + " as active, results were moved to active COMP db";

            return RedirectToAction("InventoryLines");
        }

        public ActionResult ViewReportFile(string filepath,string _id)
        {
            if (System.IO.File.Exists(filepath)) return new FilePathResult(filepath, "application/vnd.openxmlformats-officedocument.wordprocessingml.document") { FileDownloadName = Path.GetFileName(filepath) };
            else
            {
                TempData["message"] = "ERROR: cannot find file " + Path.GetFileName(filepath);
                return RedirectToAction("EditLine", new RouteValueDictionary(new { _id = _id }));
            }
        }

    }
}
