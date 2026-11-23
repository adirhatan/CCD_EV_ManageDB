using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using CCD_EV_ManageDB.Repositories;
using CCD_EV_ManageDB.Helpers;

namespace CCD_EV_ManageDB.Models
{
    public class CyclePlanHandler
    {
        private static string DefaultProtocol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultProtocol"]; } }

        public string protocol { get; set; }
        public string project { get; set; }
        public string phase { get; set; }

        public CyclePlanHandler(string _protocol, string _project, string _phase)
        {
            protocol = _protocol;
            project = _project;
            phase = _phase;
        }
        public List<string> filterTablesPerProtocol(List<string> tablelist)
        {
            //TODO:
            //cannot use "CyclePlanMetaData" record, because there will be no option to add tables (after the first one the list will be filtered to one ?), will read "CyclePlanMetaData" tables in other places, not in Define
            // need to filter by prefix match, tx/rx match , TBT3/CIO/TBT4(?)
            string[] protocolDetails = protocol.Split('_');
            string TBT = "KUKU";
            if (protocolDetails[0]=="CIO") TBT = "TBT";
            List<string> filteredTableList = new List<string>();
            foreach (string table in tablelist)
            {
                if ((table.ToUpper().StartsWith(protocolDetails[0].ToUpper())) || table.ToUpper().StartsWith(TBT))
                {
                    bool toAdd = false;
                    if (protocolDetails.Length == 1) toAdd = true;
                    else
                    {
                        if (table.ToUpper().Contains(protocolDetails[1].ToUpper())) toAdd = true;
                    }
                    //if (protocol == "Power_Code2Voltage") toAdd = true; //problem with spec
                    if (toAdd) filteredTableList.Add(table);
                }
            }
            if (filteredTableList.Count == 0)
            {
                foreach (string table in tablelist)
                {
                    if ((table.ToUpper().StartsWith(protocolDetails[0].ToUpper())) || table.ToUpper().StartsWith(TBT)) filteredTableList.Add(table);
                }
            }

            return (filteredTableList);
        }

        public List<string> GetSpecTestNamesListForTable(string Table)
        {
            List<string> SpecTestNamesListForTable = new List<string>();
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, project, phase);
            if (CPMetaData.IndicatorsMeta.ContainsKey(Table))
            {
                MDBADMReferenceTable MDBADMReferenceTableOBJ = new MDBADMReferenceTable();
                ADMReferenceTable SpecRef =  MDBADMReferenceTableOBJ.ReadADMReferenceTable(CPMetaData.IndicatorsMeta[Table].spec);
                SpecTestNamesListForTable = SpecRef.GetSpecTestNames();
            }
            return SpecTestNamesListForTable;
        }

        public Indicator GetPlannedIndicator(string Table, string numWWperUnit)
        {
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, project, phase);
            CyclePlanMetaData CPMetaDataDefault = MDBCPMetaData.Read(DefaultProtocol, project, phase);  //TODO:See if we stay with this  (needed for wws wn)

            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();

            MDBCyclePlanSpecSubSet MDBCPSpecSubSet = new MDBCyclePlanSpecSubSet();

            MDBADMReferenceTable MDBADMReferenceTableOBJ = new MDBADMReferenceTable();
            ADMReferenceTable SpecRef = MDBADMReferenceTableOBJ.ReadADMReferenceTable(CPMetaData.IndicatorsMeta[Table].spec);

            Indicator ind = new Indicator(Table, project, phase, SpecRef, CPMetaDataDefault.wws, CPMetaDataDefault.wn, "");
            //...
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            foreach (CyclePlanProjectUnit CPProjectUnit in MDBCPProjectUnit.ReadProjectTableList(protocol, project, phase,Table)) 
            {
                foreach (var planPair in CPProjectUnit.plans)
                {
                    //use the following to add each test*coverage rule numbers, divide into all available weeks, starting in wws or default wws, 
                    // add unit (?)
                    //planPair.Value.SpecSubSetID
                    CyclePlanSpecSubSet SubSetSpec = MDBCPSpecSubSet.Read(planPair.Value.SpecSubSetID);
                    //planPair.Value.CoverageRuleID
                    CyclePlanCoverageRule CoverageRule = MDBCPCoverageRule.Read(planPair.Value.CoverageRuleID);
                    //planPair.Value.wws
                    int wn = Convert.ToInt32(CPMetaDataDefault.wn);

                    int actualSdate = Convert.ToInt32(CCDUtilities.getAdjacentSundayDate(CPMetaDataDefault.wws));
                    if (!string.IsNullOrEmpty(planPair.Value.wws))
                    {
                        //calc number of weeks (all weeks, start weeek?)
                        DateTime def_date = new DateTime(Convert.ToInt32(CPMetaDataDefault.wws.Substring(0, 4)), Convert.ToInt32(CPMetaDataDefault.wws.Substring(4, 2)), Convert.ToInt32(CPMetaDataDefault.wws.Substring(6, 2)));
                        DateTime plan_date = new DateTime(Convert.ToInt32(planPair.Value.wws.Substring(0, 4)), Convert.ToInt32(planPair.Value.wws.Substring(4, 2)), Convert.ToInt32(planPair.Value.wws.Substring(6, 2)));
                        int weeks_gap = (int)(plan_date.Subtract(def_date).Days / 7);
                        wn = wn - weeks_gap;
                        actualSdate = Convert.ToInt32(CCDUtilities.getAdjacentSundayDate(planPair.Value.wws));
                    }

                    int sum = CoverageRule.corners.Count * CoverageRule.lanes.Count * CoverageRule.ports.Count * CoverageRule.trials;
                    //if (string.IsNullOrEmpty(numWWperUnit))
                    //{
                        //Old calc, first week half, last week reminder
                        int firstWeeksum = (int)(sum / wn * 0.5);
                        int weekSum = (int)((sum - firstWeeksum) / (wn - 1));
                        int lastWeekSum = sum - firstWeeksum - (weekSum * (wn - 2));
                    //}
                    if (!string.IsNullOrEmpty(numWWperUnit))
                    {
                        wn = Convert.ToInt32(numWWperUnit);
                        //New calc, all weeks  the same, last week reminder
                        weekSum = (int)(sum / wn);
                        firstWeeksum = weekSum;
                        lastWeekSum = sum - (weekSum * (wn - 1));
                    }
                    
                    foreach (IndTest IndTestObj in ind.tests)
                    {
                        if (!SubSetSpec.SpecTestNames.Contains(IndTestObj.SpecTestName)) continue;
                        IndTestObj.UnitsNum++;
                        int weeksAssignedCnt = 0;
                        foreach (WorkWeek ww in IndTestObj.WorkWeeks)
                        {
                            if (Convert.ToInt32(ww.Sdate)<actualSdate) continue;
                            if (weeksAssignedCnt == 0) ww.PlannedOrig += firstWeeksum;
                            if (weeksAssignedCnt == (wn-1)) ww.PlannedOrig += lastWeekSum;
                            if ((weeksAssignedCnt > 0) && (weeksAssignedCnt < (wn-1))) ww.PlannedOrig += weekSum;
                            weeksAssignedCnt++;
                        }


                    }
                }
            }
            //
            return (ind);
        }

        public bool UpdateCoverageRuleIDInProjectUnits(CyclePlanCoverageRule newRule, string oldRuleID)
        {
            bool success = true;
            
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            foreach (CyclePlanProjectUnit CPProjectUnit in MDBCPProjectUnit.ReadProjectTableList(protocol, project, phase, newRule.Table))
            {
                List<string> keysToReplace = new List<string>();
                foreach (var planPair in CPProjectUnit.plans)
                {
                    if (planPair.Value.CoverageRuleID == oldRuleID) keysToReplace.Add(planPair.Key);

                }
                foreach (string key in keysToReplace)
                {
                    Plan tmpPlan = CPProjectUnit.plans[key];
                    CPProjectUnit.removePlan(tmpPlan);
                    Plan newPlan = new Plan(tmpPlan.SpecSubSetID, newRule._id, tmpPlan.wws, tmpPlan.stationsNum);
                    CPProjectUnit.AddPlan(newPlan);
                }
                MDBCPProjectUnit.Save(CPProjectUnit);
            }
            commitNeedToUpdate();
            //
            return (success);
        }

        public bool UpdateDelCoverageRuleIDInProjectUnits(string deletedRuleID, string col)
        {
            bool success = true;

            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            foreach (CyclePlanProjectUnit CPProjectUnit in MDBCPProjectUnit.ReadProjectTableList(protocol, project, phase, col))
            {
                List<string> keysToReplace = new List<string>();
                foreach (var planPair in CPProjectUnit.plans)
                {
                    if (planPair.Value.CoverageRuleID == deletedRuleID) keysToReplace.Add(planPair.Key);
                }
                foreach (string key in keysToReplace)
                {
                    Plan tmpPlan = CPProjectUnit.plans[key];
                    CPProjectUnit.removePlan(tmpPlan);
                    Plan newPlan = new Plan(tmpPlan.SpecSubSetID, "", tmpPlan.wws,tmpPlan.stationsNum);
                    CPProjectUnit.AddPlan(newPlan);
                }
                MDBCPProjectUnit.Save(CPProjectUnit);
            }
            commitNeedToUpdate();
            //
            return (success);
        }

        public bool UpdateSpecSubSetIDInProjectUnits(CyclePlanSpecSubSet newSpecSubSet, string oldSpecSubSetID)
        {
            bool success = true;

            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            foreach (CyclePlanProjectUnit CPProjectUnit in MDBCPProjectUnit.ReadProjectTableList(protocol, project, phase, newSpecSubSet.Table))
            {
                List<string> keysToReplace = new List<string>();
                foreach (var planPair in CPProjectUnit.plans)
                {
                    if (planPair.Value.SpecSubSetID == oldSpecSubSetID) keysToReplace.Add(planPair.Key);
                }
                foreach (string key in keysToReplace)
                {
                    Plan tmpPlan = CPProjectUnit.plans[key];
                    CPProjectUnit.removePlan(tmpPlan);
                    Plan newPlan = new Plan(newSpecSubSet._id, tmpPlan.CoverageRuleID, tmpPlan.wws,tmpPlan.stationsNum);
                    CPProjectUnit.AddPlan(newPlan);
                }
                MDBCPProjectUnit.Save(CPProjectUnit);
            }
            commitNeedToUpdate();
            //
            return (success);
        }

        public bool UpdateDelSpecSubSetIDInProjectUnits(string deletedSpecSubSet, string col)
        {
            bool success = true;

            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            foreach (CyclePlanProjectUnit CPProjectUnit in MDBCPProjectUnit.ReadProjectTableList(protocol, project, phase, col))
            {
                List<string> keysToReplace = new List<string>();
                foreach (var planPair in CPProjectUnit.plans)
                {
                    if (planPair.Value.SpecSubSetID == deletedSpecSubSet) keysToReplace.Add(planPair.Key);
                }
                foreach (string key in keysToReplace)
                {
                    Plan tmpPlan = CPProjectUnit.plans[key];
                    CPProjectUnit.removePlan(tmpPlan);
                    Plan newPlan = new Plan("",tmpPlan.CoverageRuleID, tmpPlan.wws,tmpPlan.stationsNum);
                    CPProjectUnit.AddPlan(newPlan);
                }
                MDBCPProjectUnit.Save(CPProjectUnit);
            }
            //
            commitNeedToUpdate();
            return (success);
        }

        public void commitNeedToUpdate()
        {
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, project, phase);
            foreach (string table in CPMetaData.IndicatorsMeta.Keys)
            {
                MDBCyclePlanCSRLookUP MDBCP_CSRLookUP = new MDBCyclePlanCSRLookUP(protocol, project, phase, table);
                BsonDocument tmpDoc = new BsonDocument { { "_id", "isUpdated" } };
                tmpDoc.Set("val", "N");
                MDBCP_CSRLookUP.Save(tmpDoc);
            }
        }

        public bool UpdateCyclePlanCSRLookUP()
        {
            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, project, phase);
            MDBCyclePlanCSRLookUP MDBCP_CSRLookUP = new MDBCyclePlanCSRLookUP(protocol, project, phase, CPMetaData.IndicatorsMeta.First().Key);
            if (!MDBCP_CSRLookUP.isUpdated())
            {
                CreateCyclePlanCSRLookUP(false);
                return true;
            }

            return false;
        }
        public bool CreateCyclePlanCSRLookUP(bool checkDuplicates)
        {
            bool success = true;

            MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
            CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, project, phase);
            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
            MDBCyclePlanSpecSubSet MDBCPSpecSubSet = new MDBCyclePlanSpecSubSet();
           
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            foreach (string table in CPMetaData.IndicatorsMeta.Keys)
            {
                MDBCyclePlanCSRLookUP MDBCP_CSRLookUP = new MDBCyclePlanCSRLookUP(protocol, project, phase, table);
                MDBCP_CSRLookUP.DropCollection();
                BsonDocument tmpDoc = new BsonDocument { { "_id", "isUpdated" } };
                tmpDoc.Add("val", "U");
                MDBCP_CSRLookUP.Save(tmpDoc);

                foreach (CyclePlanProjectUnit CPProjectUnit in MDBCPProjectUnit.ReadProjectTableList(protocol, project, phase, table))
                {
                    foreach (var planPair in CPProjectUnit.plans)
                    {
                        //planPair.Value.SpecSubSetID
                        CyclePlanSpecSubSet SubSetSpec = MDBCPSpecSubSet.Read(planPair.Value.SpecSubSetID);
                        //planPair.Value.CoverageRuleID
                        CyclePlanCoverageRule CoverageRule = MDBCPCoverageRule.Read(planPair.Value.CoverageRuleID);
                        foreach (string specTestName in SubSetSpec.SpecTestNames)
                        {
                            foreach (string corner in CoverageRule.corners)
                            {
                                foreach (string port in CoverageRule.ports)
                                {
                                    foreach (string lane in CoverageRule.lanes)
                                    {
                                        //Logic_Unit+specTestName +corner+port+lane
                                        BsonDocument Doc = new BsonDocument { { "_id", CPProjectUnit.LogicUnit + "_" + specTestName + "_CORNER_" + corner + "_" + port + "_" + lane } };
                                        Doc.Add("trials", CoverageRule.trials);
                                        if (!checkDuplicates) MDBCP_CSRLookUP.Save(Doc);
                                        else
                                        {
                                            BsonDocument existDoc = MDBCP_CSRLookUP.Read(Doc.GetValue("_id", "").ToString());
                                            if ((existDoc != null) && (existDoc.GetValue("_id", "").ToString()==Doc.GetValue("_id", "").ToString()))
                                            {
                                                Double dupNum = 1 + existDoc.GetValue("duplicates", 0).ToDouble();
                                                Doc.Set("duplicates",dupNum);
                                            }
                                            MDBCP_CSRLookUP.Save(Doc);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                tmpDoc.Set("val", "Y");
                MDBCP_CSRLookUP.Save(tmpDoc);
            }

            return success;
        }

        public int GetTrialsOfSpecTestNameCSRLookUP(string table, string specTestName)
        {
            MDBCyclePlanCSRLookUP MDBCP_CSRLookUP = new MDBCyclePlanCSRLookUP(protocol, project, phase,table);
            MDBCyclePlanProjectUnit MDBCyclePlanProjectUnitObj = new MDBCyclePlanProjectUnit();
            List<CyclePlanProjectUnit> ProjectTableUnits = MDBCyclePlanProjectUnitObj.ReadProjectTableList(protocol, project, phase, table);
            if (ProjectTableUnits.Count == 0) return 0;
            //Need to iterate all logic units in project since we do not know wich one is using this spec
            foreach (CyclePlanProjectUnit CPPUnit in ProjectTableUnits)
            {
                string logicUnit = CPPUnit.LogicUnit.ToString();
                string queary = "Matches;_id;" + logicUnit + "_" + specTestName + "_";
                BsonDocument Doc = MDBCP_CSRLookUP.MDBFindOne(queary, "AND", "ASC", "_id");
                if (Doc != null) return Doc.GetValue("trials", 0).ToInt32();
            }
            return 0;
        }

        public List<string> GetPortsOfProject()
        {

            List<string> ports = new List<string>();
            MDBCyclePlanProjectUnit MDBCPProjectUnit= new MDBCyclePlanProjectUnit();
            MongoCursor CurUnits = MDBCPProjectUnit.MDBFind("Matches;_id;/" + protocol + "_" + project + "_" + phase + "/", "AND", "ASC", "_id");
            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
            foreach (BsonDocument doc in CurUnits)
            {
                CyclePlanProjectUnit unit = new CyclePlanProjectUnit(doc);
                foreach (var planPair in unit.plans)
                {
                    CyclePlanCoverageRule rule = MDBCPCoverageRule.Read(planPair.Value.CoverageRuleID);
                    ports = new List<string>(ports.Union(rule.ports).Distinct());
                }
            }
            return ports;
        }

        public List<string> GetLanesOfProject()
        {

            List<string> lanes = new List<string>();
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            MongoCursor CurUnits = MDBCPProjectUnit.MDBFind("Matches;_id;/" + protocol + "_" + project + "_" + phase + "/", "AND", "ASC", "_id");
            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
            foreach (BsonDocument doc in CurUnits)
            {
                CyclePlanProjectUnit unit = new CyclePlanProjectUnit(doc);
                foreach (var planPair in unit.plans)
                {
                    CyclePlanCoverageRule rule = MDBCPCoverageRule.Read(planPair.Value.CoverageRuleID);
                    lanes = new List<string>(lanes.Union(rule.lanes).Distinct());
                }
            }
            return lanes;
        }

        public List<string> GetUnitsOfProject()
        {

            List<string> units = new List<string>();
            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            MongoCursor CurUnits = MDBCPProjectUnit.MDBFind("Matches;_id;/" + protocol + "_" + project + "_" + phase + "/", "AND", "ASC", "_id");
            foreach (BsonDocument doc in CurUnits)
            {
                CyclePlanProjectUnit unit = new CyclePlanProjectUnit(doc);
                units.Add(unit.LogicUnit.ToString());
            }
            return units;
        }

        public bool ValidateAssignProjectUnits_copy_config_from(string copy_from_proj, string copy_from_phase)
        {

            MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
            MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
            MDBCyclePlanSpecSubSet MDBCPSpecSubSet = new MDBCyclePlanSpecSubSet();
            foreach (CyclePlanProjectUnit CPProjectUnit in MDBCPProjectUnit.ReadProjectList(protocol, copy_from_proj, copy_from_phase))
            {
                CyclePlanProjectUnit CPProjectUnitNew = new CyclePlanProjectUnit(protocol, project, phase, CPProjectUnit,"");
                foreach (var pairPlan in CPProjectUnitNew.plans)
                {
                    CyclePlanSpecSubSet tmpSubSet = MDBCPSpecSubSet.Read(pairPlan.Value.SpecSubSetID);
                    if (tmpSubSet._id != pairPlan.Value.SpecSubSetID) return false;
                    CyclePlanCoverageRule tmpRule = MDBCPCoverageRule.Read(pairPlan.Value.CoverageRuleID);
                    if (tmpRule._id != pairPlan.Value.CoverageRuleID) return false;
                }

            }
            return true;
        }

        public bool DeleteTableInMetaData(string table, out string message)
        {
            message = "";
            try
            {
                MDBCyclePlanMetaData MDBCPMetaData = new MDBCyclePlanMetaData();
                CyclePlanMetaData CPMetaData = MDBCPMetaData.Read(protocol, project, phase);
                CPMetaData.IndicatorsMeta.Remove(table);
                MDBCPMetaData.Save(CPMetaData);

                MDBCyclePlanCoverageRule MDBCPCoverageRule = new MDBCyclePlanCoverageRule();
                foreach (CyclePlanCoverageRule rule in MDBCPCoverageRule.ReadProjectTableList(protocol, project, phase, table)) MDBCPCoverageRule.Delete(rule._id);

                MDBCyclePlanSpecSubSet MDBCPSpecSubSet = new MDBCyclePlanSpecSubSet();
                foreach (CyclePlanSpecSubSet subset in MDBCPSpecSubSet.ReadProjectTableList(protocol, project, phase, table)) MDBCPSpecSubSet.Delete(subset._id);

                MDBCyclePlanProjectUnit MDBCPProjectUnit = new MDBCyclePlanProjectUnit();
                foreach (CyclePlanProjectUnit unit in MDBCPProjectUnit.ReadProjectTableList(protocol, project, phase, table)) MDBCPProjectUnit.Delete(unit._id);

                MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
                SessionsManageProtocolConf SMPConf = MDBSMconf.Read(protocol, project, phase);
                if (SMPConf.tables.Contains(table)) SMPConf.tables.Remove(table);
                if (SMPConf.table_divs.ContainsKey(table)) SMPConf.table_divs.Remove(table);
                MDBSMconf.SaveDoNotOverWriteTimestamp_ActiveFileNames(SMPConf);
                SessionsManageProtocolConf SMPConfTmp = MDBSMconf.ReadTMP(protocol, project, phase);
                if (SMPConfTmp.tables.Contains(table)) SMPConfTmp.tables.Remove(table);
                if (SMPConfTmp.table_divs.ContainsKey(table)) SMPConfTmp.table_divs.Remove(table);
                MDBSMconf.SaveTMP(SMPConfTmp);

                MDBCyclePlanCSRLookUP MDBCP_CSRLookUP = new MDBCyclePlanCSRLookUP(protocol, project, phase, table);
                MDBCP_CSRLookUP.DropCollection();
            }
            catch (Exception e)
            {
                message = e.Message;
                return false;
            }


            return true;
        }
    }
}