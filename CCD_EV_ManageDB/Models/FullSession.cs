using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCD_EV_ManageDB.Models
{
    [Serializable()]
    public class FullSession
    {
        public DBsearchSave TmpSearch { get; set; }
        public DBsearchSave PrmSearch { get; set; }
        public DBsearchSave CmpSearch { get; set; }
        public DBsearchSave InvSearch { get; set; }
        public DBsearchSave CalibrationSearch { get; set; }
        public DBsearchSave BoardSearch { get; set; }
        public string LastDBViewed { get; set; }
        public string ReportDB { get; set; }
        public int QuearyDays { get; set; }
        public string LastSessionManageProj { get; set; }
        public string LastSessionManagePhase { get; set; }
        public string LastSessionManageProtocol { get; set; }
        public string LastCyclePlanProj { get; set; }
        public string LastCyclePlanPhase { get; set; }
        public string LastCyclePlanProtocol { get; set; }
        public string LastCyclePlanShowSum { get; set; }


    public FullSession()
        {
            TmpSearch = new DBsearchSave();
            PrmSearch = new DBsearchSave();
            CmpSearch = new DBsearchSave();
            InvSearch = new DBsearchSave();
            CalibrationSearch = new DBsearchSave();
            BoardSearch = new DBsearchSave();
            ReportDB = "PRM";
            QuearyDays = -1;
            LastCyclePlanShowSum = "Y";
        }
    }
}