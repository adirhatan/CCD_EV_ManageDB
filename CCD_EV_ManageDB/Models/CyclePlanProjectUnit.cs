using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using CCD_EV_ManageDB.Repositories;
using System.Reflection;

namespace CCD_EV_ManageDB.Models
{
    public class CyclePlanProjectUnit
    {
        //	Key:  Protocol + Project + Phase +table + Skew + Board + *SetupType 

        public string _id { get; set; }
        public string protocol { get; set; }
        public string project { get; set; }
        public string phase { get; set; }
        public string Table { get; set; }
        public string Skew { get; set; }
        public string LogicUnit { get; set; }
        public string SetupType { get; set; }
        public string BoardType { get; set; }
        public string LogicUnitNumber { get; set; }
        public Dictionary<string,Plan> plans { get; set; }

        


        public CyclePlanProjectUnit()
        {
            plans = new Dictionary<string, Plan>();
        }

        public CyclePlanProjectUnit(string _protocol, string _proj, string _phase, string _Table, string _Skew, string _BoardType, string _SetupType, string _LogicUnitNumber, string _stationsNum)
        {
            LogicUnit = _Skew + "_" + _BoardType.ToUpper().Substring(0, 1) + "_" + _SetupType.ToUpper().Substring(0, 1) + "_" + _LogicUnitNumber;
            //_id = _protocol + '_' + _proj + '_' + _phase + '_' + _Table + '_' + _Skew + '_' + _Board + '_' + _SetupType;
            _id = _protocol + '_' + _proj + '_' + _phase + '_' + _Table + '_' +LogicUnit;
            protocol = _protocol;
            project = _proj;
            phase = _phase;
            Table = _Table;
            Skew = _Skew;
            SetupType = _SetupType;
            BoardType = _BoardType;
            LogicUnitNumber = _LogicUnitNumber;
            plans = new Dictionary<string, Plan>();
        }

        public CyclePlanProjectUnit(string _protocol, string _proj, string _phase, string _Table, string _Skew, string _BoardType, string _SetupType, string _LogicUnitNumber, string _stationsNum, string _SpecSubSetID, string _CoverageRuleID, string _wws)
        {
            LogicUnit = _Skew + "_" + _BoardType.ToUpper().Substring(0, 1) + "_" + _SetupType.ToUpper().Substring(0, 1) + "_" + _LogicUnitNumber;
            //_id = _protocol + '_' + _proj + '_' + _phase + '_' + _Table + '_' + _Skew + '_' + _Board + '_' + _SetupType;
            _id = _protocol + '_' + _proj + '_' + _phase + '_' + _Table + '_' + LogicUnit;
            protocol = _protocol;
            project = _proj;
            phase = _phase;
            Table = _Table;
            Skew = _Skew;
            SetupType = _SetupType;
            BoardType = _BoardType;
            LogicUnitNumber = _LogicUnitNumber;
            plans = new Dictionary<string, Plan>();
            plans.Add((new Plan(_SpecSubSetID, _CoverageRuleID, _wws, _stationsNum))._id, new Plan(_SpecSubSetID, _CoverageRuleID, _wws, _stationsNum));
        }

        public CyclePlanProjectUnit(string _protocol, string _proj, string _phase, CyclePlanProjectUnit refCPProjectUnit, string _wws)
        {
            LogicUnit = refCPProjectUnit.Skew + "_" + refCPProjectUnit.BoardType.ToUpper().Substring(0, 1) + "_" + refCPProjectUnit.SetupType.ToUpper().Substring(0, 1) + "_" + refCPProjectUnit.LogicUnitNumber;
            //_id = _protocol + '_' + _proj + '_' + _phase + '_' + _Table + '_' + _Skew + '_' + _Board + '_' + _SetupType;
            _id = _protocol + '_' + _proj + '_' + _phase + '_' + refCPProjectUnit.Table + '_' + LogicUnit;
            protocol = _protocol;
            project = _proj;
            phase = _phase;
            Table = refCPProjectUnit.Table;
            Skew = refCPProjectUnit.Skew;
            SetupType = refCPProjectUnit.SetupType;
            BoardType = refCPProjectUnit.BoardType;
            LogicUnitNumber = refCPProjectUnit.LogicUnitNumber;
            plans = new Dictionary<string, Plan>();
            foreach (var planPair in refCPProjectUnit.plans)
            {
                string _SpecSubSetID = planPair.Value.SpecSubSetID.Replace(refCPProjectUnit.project,_proj).Replace(refCPProjectUnit.phase,_phase);
                string _CoverageRuleID = planPair.Value.CoverageRuleID.Replace(refCPProjectUnit.project,_proj).Replace(refCPProjectUnit.phase,_phase);
                Plan plan = new Plan(_SpecSubSetID, _CoverageRuleID, _wws, planPair.Value.stationsNum);
                plans.Add(plan._id, plan);
            }
            
        }

        public CyclePlanProjectUnit(BsonDocument doc)
        {
            _id = doc.GetValue("_id", "").ToString();
            protocol = (doc.GetValue("protocol", "").ToString());
            project = (doc.GetValue("project", "").ToString());
            phase = (doc.GetValue("phase", "").ToString());
            Table = (doc.GetValue("Table", "").ToString());
            Skew = (doc.GetValue("Skew", "").ToString());
            BoardType = (doc.GetValue("BoardType", "").ToString());
            SetupType = (doc.GetValue("SetupType", "").ToString());
            LogicUnitNumber = (doc.GetValue("LogicUnitNumber", "").ToString());
            LogicUnit = (doc.GetValue("LogicUnit", "").ToString());


            plans = new Dictionary<string, Plan>();
            if (doc.Contains("plans"))
            {
                BsonArray pArray = doc.GetElement("plans").Value.AsBsonArray;
                foreach (BsonDocument planDoc in pArray)
                {
                    plans.Add((planDoc.GetValue("_id", "").ToString()), new Plan(planDoc));
                }
            }

        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", _id } };
            Doc.Add("protocol", protocol);
            Doc.Add("project", project);
            Doc.Add("phase", phase);
            Doc.Add("Table", Table);
            Doc.Add("Skew", Skew);
            BsonArray pArray = new BsonArray();
            foreach (var planPair in plans)
            {
                pArray.Add(planPair.Value.toBsonDocument());
            }
            Doc.Add("plans", pArray);
            Doc.Add("BoardType", BoardType);
            Doc.Add("SetupType", SetupType);
            Doc.Add("LogicUnitNumber", LogicUnitNumber);
            Doc.Add("LogicUnit", LogicUnit);
            return Doc;
        }

        public bool AddPlan ( string _SpecSubSetID, string _CoverageRuleID, string _wws, string _stationsNum)
        {
            bool Success = true;
            if (plans.ContainsKey((new Plan(_SpecSubSetID, _CoverageRuleID, _wws, _stationsNum))._id)) return false;
            plans.Add((new Plan(_SpecSubSetID, _CoverageRuleID, _wws, _stationsNum)._id), new Plan(_SpecSubSetID, _CoverageRuleID, _wws, _stationsNum));
            return Success;

        }
        public bool AddPlan(Plan _plan)
        {
            bool Success = true;
            if (plans.ContainsKey(_plan._id)) return false;
            plans.Add((_plan._id), _plan);
            return Success;

        }
        public bool removePlan(string _SpecSubSetID, string _CoverageRuleID)
        {
            bool Success = true;
            if (!plans.ContainsKey((new Plan(_SpecSubSetID, _CoverageRuleID,"", ""))._id)) return true;
            Success  = plans.Remove((new Plan(_SpecSubSetID, _CoverageRuleID,"", ""))._id);
            return Success;

        }

        public bool removePlan(Plan oldPlan)
        {
            bool Success = true;
            if (!plans.ContainsKey((new Plan(oldPlan.SpecSubSetID, oldPlan.CoverageRuleID, "",""))._id)) return true;
            Success = plans.Remove((new Plan(oldPlan.SpecSubSetID, oldPlan.CoverageRuleID, "",""))._id);
            return Success;

        }

        public List<CyclePlanProjectUnitDisp> GetCyclePlanProjectUnitDispList()
        {
            List<CyclePlanProjectUnitDisp> DispUnits = new List<CyclePlanProjectUnitDisp>();
            foreach (var planPair in plans)
            {
                CyclePlanProjectUnitDisp tmp = new CyclePlanProjectUnitDisp();
                tmp._id = _id;
                tmp.phase = phase;
                tmp.plan = planPair.Value;
                tmp.project = project;
                tmp.protocol = protocol;
                tmp.BoardType = BoardType;
                tmp.SetupType = SetupType;
                tmp.LogicUnit = LogicUnit;
                tmp.LogicUnitNumber = LogicUnitNumber;
                tmp.Skew = Skew;
                tmp.Table = Table;
                DispUnits.Add(tmp);
            }
            return DispUnits;
        }

    }
    public class Plan
    {
        public string _id { get; set; }
        public string SpecSubSetID { get; set; }
        public string CoverageRuleID { get; set; }
        public string wws { get; set; }
        public string duration { get; set; }
        public string stationsNum { get; set; }

        public Plan(string _SpecSubSetID, string _CoverageRuleID, string _wws, string _stationsNum)
        {
            _id = _SpecSubSetID + '_' + _CoverageRuleID;
            SpecSubSetID = _SpecSubSetID;
            CoverageRuleID = _CoverageRuleID;
            wws = _wws;
            stationsNum = _stationsNum;
        }
        
        public Plan(BsonDocument doc)
        {
            _id = doc.GetValue("_id", "").ToString();
            SpecSubSetID = doc.GetValue("SpecSubSetID", "").ToString();
            CoverageRuleID = (doc.GetValue("CoverageRuleID", "").ToString());
            wws = (doc.GetValue("wws", "").ToString());
            duration = (doc.GetValue("duration", "").ToString());
            stationsNum = (doc.GetValue("stationsNum", "1").ToString());

        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", _id } };
            Doc.Add("SpecSubSetID", SpecSubSetID);
            Doc.Add("CoverageRuleID", CoverageRuleID);
            Doc.Add("wws", wws);
            Doc.Add("duration", duration);
            Doc.Add("stationsNum", stationsNum);
            return Doc;
        }

    }

    //container for display - page
    public class AssignProjectUnitDisp
    {
        //public List<CyclePlanProjectUnit> CyclePlanProjectUnits { get; set; }  - see if we need this
        public List<CyclePlanProjectUnit> CyclePlanProjectUnits { get; set; }
        public List<CyclePlanProjectUnitDisp> CyclePlanProjectUnitsDisp { get; set; }
        public List<CyclePlanSpecSubSet> CyclePlanSpecSubSets { get; set; }
        public List<CyclePlanCoverageRule> CyclePlanCoverageRules { get; set; }
        public CyclePlanMetaData CPMetaData { get; set; }
        public Dictionary<string, string> SpecSubSetsNames { get; set; }
        public Dictionary<string, string> SpecSubSetsNumbers { get; set; }
        public Dictionary<string, string> CoverageRulesNames { get; set; }
        public Dictionary<string, string> CoverageRulesSums { get; set; }

        public AssignProjectUnitDisp()
        {
            CyclePlanProjectUnits = new List<CyclePlanProjectUnit>();
            CyclePlanProjectUnitsDisp = new List<CyclePlanProjectUnitDisp>();
            CyclePlanSpecSubSets = new List<CyclePlanSpecSubSet>();
            CyclePlanCoverageRules = new List<CyclePlanCoverageRule>();
            CPMetaData = new CyclePlanMetaData();
            SpecSubSetsNames = new Dictionary<string,string>();
            SpecSubSetsNumbers = new Dictionary<string, string>();
            CoverageRulesNames = new Dictionary<string,string>();
            CoverageRulesSums = new Dictionary<string, string>();

        }

        public bool Validate(out string messgae)
        {
            messgae = "";

            foreach (string table in CPMetaData.IndicatorsMeta.Keys)
            {
                bool SpecSubSetExistsforTable = false;
                bool CoverageRuleExistsforTable = false;
                foreach (string SpecSubSetsName in SpecSubSetsNames.Keys) if (SpecSubSetsName.Contains(table)) SpecSubSetExistsforTable = true;
                foreach (string CoverageRuleName in CoverageRulesNames.Keys) if (CoverageRuleName.Contains(table)) CoverageRuleExistsforTable = true;
                if ((!SpecSubSetExistsforTable) || (!CoverageRuleExistsforTable))
                {
                    messgae = " SpecSubSet or Coverage Rule are not defined for table:" + table;
                    return false;
                }
            }
            return true;
        }

    }

    //container for display - line
    public class CyclePlanProjectUnitDisp
    {
        public string _id { get; set; }
        public string protocol { get; set; }
        public string project { get; set; }
        public string phase { get; set; }
        public string Table { get; set; }
        public string Skew { get; set; }
        public string LogicUnit { get; set; }
        public string LogicUnitNumber { get; set; }
        public string SetupType { get; set; }
        public string BoardType { get; set; }
        public string stationsNum { get; set; }
        public Plan plan { get; set; }
    }


   
}