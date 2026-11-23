using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq; 
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class CycleStatusReportNew
    {
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }

        protected Dictionary<string, Dictionary<string,string>> StatusResults; //buckets in Excel
        protected Dictionary<string, Dictionary<string,List<string>>> StatusResultsFullPass;// buckets for full resolution /port/lane/trials in order to validate duplicate results
        protected Dictionary<string, Dictionary<string, List<string>>> StatusResultsFullFail;// buckets for full resolution /port/lane/trials in order to validate duplicate results
        protected Dictionary<string, Dictionary<string, int>> StatusResultsFullAll;// buckets for full resolution /port/lane/trials in order to count all results

        //protected Dictionary<string, List<string>> SpecTestNamesWithMissingTrials;// New:Missing info for Status Report
        //protected Dictionary<string, List<string>> SpecTestNamesWithFailedTrials;// New:Missing info for Status Report
        protected Dictionary<string, Dictionary<string,string>> SpecTestNamesWithMissingTrials;// New:Missing info for Status Report
        protected Dictionary<string, Dictionary<string,string>> SpecTestNamesWithFailedTrials;// New:Missing info for Status Report

        protected int ProgressContainerFull;// New: calculate progress
        protected int ProgressContainerExecuted;// New: calculate progress

        public List<string> Corners { get; set; }
        public Dictionary<string, List<string>> Stations { get; set; }
        public Dictionary<string, List<string>> FWversions { get; set; }
        public Dictionary<string, List<string>> Boards { get; set; }
        public SessionsManageProtocolConf SMPConf;
        public string table;
        public Unit LogicUnit;
        public List<string> CSR_excelPorts { get; set; }

        MDBCyclePlanCSRLookUP MDBCP_CSRLookUP;


        public CycleStatusReportNew(SessionsManageProtocolConf _SMPConf, string _table, Unit _LogicUnit)
        {
            SMPConf = _SMPConf;
            table = _table;
            LogicUnit = _LogicUnit;
            StatusResults = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>();
            StatusResultsFullPass = new Dictionary<string, Dictionary<string, List<string>>>();
            StatusResultsFullFail = new Dictionary<string,Dictionary<string,List<string>>>();
            StatusResultsFullAll = new Dictionary<string, Dictionary<string, int>>();
            //SpecTestNamesWithMissingTrials = new Dictionary<string, List<string>>();
            //SpecTestNamesWithFailedTrials = new Dictionary<string, List<string>>();
            SpecTestNamesWithMissingTrials = new Dictionary<string, Dictionary<string, string>>();
            SpecTestNamesWithFailedTrials = new Dictionary<string, Dictionary<string, string>>();
            Corners = new List<string>(new string[] { "HVHT", "HVLT", "NOMN", "LVHT", "LVLT" });
            if (SMPConf.corners.Count > 0) Corners = new List<string>(SMPConf.corners);
            Stations = new Dictionary<string, List<string>>();
            FWversions = new Dictionary<string, List<string>>();
            Boards = new Dictionary<string, List<string>>();
            Dictionary<string,string> ExcelPortsDict = new Dictionary<string,string>();
            //Apply "mask" logic on Excel Ports, in DP_TX RDRV/Multifunctions shares same config but Multifunction doe snot use SRC0
            List<string> ExcelPortsExcluded = new List<string>();
            foreach (Mask mask in SMPConf.masks)
            {
                if ((mask.table == "ALL") || (this.table == mask.table)) ExcelPortsExcluded.AddRange(mask.ExcludePorts(LogicUnit, ""));
            }
            CSR_excelPorts = new List<string>();
            CSR_excelPorts.AddRange(SMPConf.excelPorts);
            foreach (string s in ExcelPortsExcluded) if (CSR_excelPorts.Contains(s)) CSR_excelPorts.Remove(s);
            //foreach (string s in SMPConf.excelPorts) ExcelPortsDict.Add(s,"NA");
            foreach (string s in CSR_excelPorts) ExcelPortsDict.Add(s, "NA");

            foreach (var TDV in SMPConf.table_divs[table])
            {
                foreach (var TDL in TDV.Value.TableDivLines)
                {
                    if (!StatusResults.ContainsKey(TDL.Key + "-" + "HVHT"))
                    {
                        foreach (string c in Corners) StatusResults.Add(TDL.Key + "-" + c, new Dictionary<string, string>(ExcelPortsDict));
                        foreach (string c in Corners)
                        {
                            StatusResultsFullPass.Add(TDL.Key + "-" + c, new Dictionary<string, List<string>>());
                            StatusResultsFullFail.Add(TDL.Key + "-" + c, new Dictionary<string, List<string>>());
                            StatusResultsFullAll.Add(TDL.Key + "-" + c, new Dictionary<string, int>());
                            foreach (string s in SMPConf.ports)
                            {
                                foreach (string l in SMPConf.lanes)
                                {
                                    StatusResultsFullPass[TDL.Key + "-" + c].Add(s + "-" + l, new List<string>());
                                    StatusResultsFullFail[TDL.Key + "-" + c].Add(s + "-" + l, new List<string>());
                                    StatusResultsFullAll[TDL.Key + "-" + c].Add(s + "-" + l,0);
                                }
                            }
                        }
                    }
                }
            }

            ProgressContainerFull = 0;
            ProgressContainerExecuted = 0;

            MDBCP_CSRLookUP = new MDBCyclePlanCSRLookUP(SMPConf.protocol, SMPConf.proj, SMPConf.phase, _table);


        }
        public int calcNumOfFullResultsCyclePlan(int trialNum, int divLineTestsNum, string divName, string divLine_name, string corner_name)
        {
            //Currently supports both old & new mconfiguration methods
            //New method - for each SpecTestName in subset, looks for exist records in CSRLookup. (doesn't need divLineTestsNum nor trialNum, gets it from CSRLookup)
            //Old Methos - old func, multiples lanes*ports*trialNum*divLineTestsNum, excludes ports/lanes according to masks logic
            List<string> divLineSpecTestNames = SMPConf.table_divs[table][divName].TableDivLines[divLine_name].getSpecTestNamesOfSpecSubSet();
            long divLineTestsNumL = 0;
            int trialNumLookUp = 0;
            foreach (string spec in divLineSpecTestNames)
            {
                string query = "Matches;_id;/^"+LogicUnit.ToString() + "_" + spec + "_CORNER_" + corner_name + "/";
                //Using LookUp to count all LogicUnit*Corenr*Ports*Lanes valid permutations, instead of using masks
                MongoCursor Cur = MDBCP_CSRLookUP.MDBFind(query, "AND", "ASC", "_id");
                divLineTestsNumL += Cur.Count();
                if (trialNumLookUp == 0)
                {
                    BsonDocument tmpLookUpDoc = MDBCP_CSRLookUP.MDBFindOne(query, "AND", "ASC", "_id");
                    if (tmpLookUpDoc != null)
                    {
                        trialNumLookUp = tmpLookUpDoc.GetValue("trials", 0).ToInt32();
                    }
                }
            }

            //using "New Method" (need to make sure this is reliable desicion)
            if (divLineSpecTestNames.Count > 0)
            {
                int num = Convert.ToInt32(divLineTestsNumL);
                //Using "New Method" with additional fields conditions - needs trials number defined by user per Excel cell
                if (SMPConf.table_divs[table][divName].TableDivLines[divLine_name].isDivLineWithAddCondoition()) num = num * trialNum;
                //Using "New Method" without additional fields conditions - using trials number defined in coverage rule, asuming equals for all SpecTestNames in same cell
                else num = num * trialNumLookUp;
                return num;
            }
                //using "Old method"
            else
            {
                return calcNumOfFullResults(trialNum, divLineTestsNum, divLine_name);
            }

        }


        public int calcNumOfFullResults(int trialNum, int divLineTestsNum, string divLine_name)
        {
            if (SMPConf.masks.Count > 0) return calcNumOfFullResultsMasks(trialNum, divLineTestsNum, divLine_name);
            int num = 1;
            num=num*SMPConf.lanes.Count;
            num=num*SMPConf.ports.Count;
            num = num * trialNum;
            num = num * divLineTestsNum;
            return num;
        }
        public int calcNumOfPortResults(int trialNum, int divLineTestsNum, string divLine_name, string port)
        {
            if (SMPConf.masks.Count > 0) return calcNumOfPortResultsMasks(trialNum, divLineTestsNum, divLine_name, port);
            int num = 1;
            num = num*SMPConf.lanes.Count;
            num = num * trialNum;
            num = num * divLineTestsNum;
            return num;
        }

        public int calcNumOfPortResultsCyclePlan(int trialNum, int divLineTestsNum, string divName, string divLine_name, string corner_name, string port)
        {
            //Currently supports both old & new mconfiguration methods
            //New method - for each SpecTestName in subset, looks for exist records in CSRLookup. (doesn't need divLineTestsNum nor trialNum, gets it from CSRLookup)
            //Old Methos - old func, multiples lanes*ports*trialNum*divLineTestsNum, excludes ports/lanes according to masks logic
            List<string> divLineSpecTestNames = SMPConf.table_divs[table][divName].TableDivLines[divLine_name].getSpecTestNamesOfSpecSubSet();
            long divLineTestsNumL = 0;
            int trialNumLookUp = 0;
            foreach (string spec in divLineSpecTestNames)
            {
                string query = "Matches;_id;/^" + LogicUnit.ToString() + "_" + spec + "_CORNER_" + corner_name + "_" + port + "/";
                MongoCursor Cur = MDBCP_CSRLookUP.MDBFind(query, "AND", "ASC", "_id");
                divLineTestsNumL += Cur.Count();
                if (trialNumLookUp == 0)
                {
                    BsonDocument tmpLookUpDoc = MDBCP_CSRLookUP.MDBFindOne(query, "AND", "ASC", "_id");
                    if (tmpLookUpDoc != null)
                    {
                        trialNumLookUp = tmpLookUpDoc.GetValue("trials", 0).ToInt32();
                    }
                }
            }

            //using "New Method" (need to make sure this is reliable desicion)
            if (divLineSpecTestNames.Count > 0)
            {
                int num = Convert.ToInt32(divLineTestsNumL);
                //Using "New Method" with additional fields conditions - needs trials number defined by user per Excel cell
                if (SMPConf.table_divs[table][divName].TableDivLines[divLine_name].isDivLineWithAddCondoition()) num = num * trialNum;
                //Using "New Method" without additional fields conditions - using trials number defined in coverage rule, asuming equals for all SpecTestNames in same cell
                else num = num * trialNumLookUp;
                return num;
            }
            //using "Old method"
            else
            {
                return calcNumOfPortResults(trialNum, divLineTestsNum, divLine_name, port);
            }

        }

        public int calcNumOfFullResultsMasks(int trialNum, int divLineTestsNum, string divLine_name)
        {
            int num = 0;
            foreach (string _port in SMPConf.ports)
            {
                num += calcNumOfPortResultsMasks(trialNum, divLineTestsNum,divLine_name, _port);
            }
            return num;
        }

        public int calcNumOfPortResultsMasks(int trialNum, int divLineTestsNum, string divLine_name, string port)
        {
            int num = 1;
            List<string> PortsExcluded = new List<string>();
            foreach (Mask mask in SMPConf.masks)
            {
                if ((mask.table == "ALL") || (this.table == mask.table)) PortsExcluded.AddRange(mask.ExcludePorts(LogicUnit, divLine_name));
            }
            if (PortsExcluded.Contains(port)) return 0;
            List<string> LanesExcluded = new List<string>();
            foreach (Mask mask in SMPConf.masks)
            {
                if ((mask.table == "ALL") || (this.table == mask.table)) LanesExcluded.AddRange(mask.ExcludeLanes(LogicUnit, divLine_name, port));
            }
            int lanesCount = 0;
            foreach (string _lane in SMPConf.lanes)
            {
                if (!LanesExcluded.Contains(_lane)) lanesCount++;
            }
            num = num * lanesCount;
            num = num * trialNum;
            num = num * divLineTestsNum;
            return num;
        }

        private int getTrialNumLookup(string divName, string divLine_name, string corner_name)
        {
            List<string> divLineSpecTestNames = SMPConf.table_divs[table][divName].TableDivLines[divLine_name].getSpecTestNamesOfSpecSubSet();
            int trialNumLookUp = 0;
            foreach (string spec in divLineSpecTestNames)
            {
                string query = "Matches;_id;/^" + LogicUnit.ToString() + "_" + spec + "_CORNER_" + corner_name + "/";
                if (trialNumLookUp != 0) break;
                else
                {
                    BsonDocument tmpLookUpDoc = MDBCP_CSRLookUP.MDBFindOne(query, "AND", "ASC", "_id");
                    if (tmpLookUpDoc != null)
                    {
                        trialNumLookUp = tmpLookUpDoc.GetValue("trials", 0).ToInt32();
                    }
                }
            }
            return trialNumLookUp;
        }

        public void AnalyzeResults(MongoCursor Results)
        {
            string corner;
            string port;
            string lane;
            string status;
            string table_divLine_name;
            int divLine_trials;
            int divLine_numOfTests;
            string station;
            string FWverion;
            string board;
            string specTestName;

            if (Results == null) return; // In case we filter DB to TMP or PRM only it will be caled with null cursor

            SpecTestNamesWithMissingTrials.Clear(); //Since Analyze is called twice TMP/PRM just the last time matters
            SpecTestNamesWithFailedTrials.Clear();  //Since Analyze is called twice TMP/PRM just the last time matters
            ProgressContainerFull = 0; //Since Analyze is called twice TMP/PRM just the last time matters
            ProgressContainerExecuted = 0; //Since Analyze is called twice TMP/PRM just the last time matters

            foreach (BsonDocument result in Results)
            {
                table_divLine_name = "";
                divLine_trials = 1;
                divLine_numOfTests = 1;
                corner = result.GetValue("Corner", "").ToString().ToUpper();
                if (corner.Length > 4) corner = corner.Substring(0, 4);
                port = result.GetValue("Port", "").ToString().ToUpper();
                lane = result.GetValue("Lane", "").ToString().ToUpper();
                //for PCIE switch between lane value to port
                if (this.table.StartsWith("PCIE"))
                {
                    port = lane;
                    lane = "NA";
                }
                status = result.GetValue("TestStatus", "NA").ToString().ToUpper();
                station = result.GetValue("HostName", "").ToString().ToUpper();
                FWverion = "Rev" + result.GetValue("NVM_REV", "").ToString().ToUpper() + " Pre" + result.GetValue("NVM_Pre", "").ToString().ToUpper() + " " + result.GetValue("FW version", "").ToString().ToUpper();
                board = result.GetValue("BoardNumber", "").ToString() + "_Unit_" + result.GetValue("UnitId", "").ToString();
                specTestName = result.GetValue("SpecTestName", "NA").ToString();

                //foreach (var TDL in SMPConf.table_divs[table].TableDivLines)
                foreach (var TDV in SMPConf.table_divs[table])
                {
                    foreach (var TDL in TDV.Value.TableDivLines)
                    {
                        bool allFieldsFound = true;
                        if (!TDL.Value.isDivLineWithAddCondoition()) allFieldsFound = false;
                        bool isSpecSubSetFieldEquals = false;
                        foreach (TableDivField TDF in TDL.Value.tableDivFields)
                        {
                            if (!TDF.isSpecSubSet)
                            {
                                string field = result.GetValue(TDF.name, "").ToString();
                                if (TDF.condition == "equals")
                                {
                                    if (!TDF.values.Contains(field))
                                    {
                                        allFieldsFound = false;
                                    }
                                }
                                if (TDF.condition == "contains")
                                {
                                    foreach (string v in TDF.values) if (!field.Contains(v)) allFieldsFound = false;
                                }
                                if (TDF.condition == "notContains")
                                {
                                    foreach (string v in TDF.values) if (field.Contains(v)) allFieldsFound = false;
                                }
                            }
                            if (TDF.isSpecSubSet)
                            {
                                //allFieldsFound = false;
                                string field = result.GetValue(TDF.name, "").ToString();
                                if (TDF.condition == "equals")
                                {
                                    if (TDF.values[0]==field)
                                    {
                                        isSpecSubSetFieldEquals = true;
                                    }
                                }
                            }
                        }
                        //Handle case of mixed conditions: isSpecSubSet field and manual field(s)
                        if ((TDL.Value.isDivLineWithMixedCondoitions()) && ((!isSpecSubSetFieldEquals) || (!allFieldsFound))) continue;

                        //Cases of isSpecSubSet conditions only or manual conditions only
                        if ((allFieldsFound) || (isSpecSubSetFieldEquals))
                        {
                            table_divLine_name = TDL.Key;
                            if (allFieldsFound) divLine_trials = TDL.Value.trials;
                            if ((isSpecSubSetFieldEquals) && (!TDL.Value.isDivLineWithMixedCondoitions())) divLine_trials = getTrialNumLookup(TDV.Value.name, TDL.Value.name, corner);
                            if (TDL.Value.setup_trials.Count > 0)
                            {
                                if (TDL.Value.setup_trials.ContainsKey(this.LogicUnit.setup)) divLine_trials = TDL.Value.setup_trials[this.LogicUnit.setup];
                            }
                            divLine_numOfTests = TDL.Value.numOfTests;
                            if (TDL.Value.setup_numOfTests.Count > 0)
                            {
                                if (TDL.Value.setup_numOfTests.ContainsKey(this.LogicUnit.setup)) divLine_numOfTests = TDL.Value.setup_numOfTests[this.LogicUnit.setup];
                            }
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(table_divLine_name))
                    {
                        if (!Stations.ContainsKey(TDV.Key)) Stations.Add(TDV.Key, new List<string>());
                        if (!Stations[TDV.Key].Contains(station)) Stations[TDV.Key].Add(station);
                        if (!FWversions.ContainsKey(TDV.Key)) FWversions.Add(TDV.Key, new List<string>());
                        if (!FWversions[TDV.Key].Contains(FWverion)) FWversions[TDV.Key].Add(FWverion);
                        if (!Boards.ContainsKey(TDV.Key)) Boards.Add(TDV.Key, new List<string>());
                        if (!Boards[TDV.Key].Contains(board)) Boards[TDV.Key].Add(board);
                        break;
                    }
                    
                }

                string key = table_divLine_name + "-" + corner;
                string fullKey = port + "-" + lane;
                string excelKey = port;
                if (SMPConf.excelPorts.Count == 1) excelKey = SMPConf.excelPorts[0];

                if (!(StatusResults.ContainsKey(key))) continue;
                if (!(StatusResults[key].ContainsKey(excelKey))) continue;
                //New: check if it is a valid result permutation according to Cycle Plane definitions
                BsonDocument LookUPDoc = this.MDBCP_CSRLookUP.Read(this.LogicUnit.ToString(), specTestName, corner, port, lane);
                if ((LookUPDoc==null) || (String.IsNullOrEmpty(LookUPDoc.GetValue("_id","").ToString()))) continue;

                //Count result
                //how to count duplicate? (currently do not count duplicats)

                if (status == "PASS")
                {
                    if (StatusResultsFullPass[key].ContainsKey(fullKey) && (StatusResultsFullPass[key][fullKey].Count() < divLine_trials * divLine_numOfTests)) //trials added
                    {
                        int cnt = 0;
                        foreach (string t in StatusResultsFullPass[key][fullKey]) if (t == specTestName) cnt++;
                        if (cnt < divLine_trials) StatusResultsFullPass[key][fullKey].Add(specTestName);
                    }
                }
                if (status == "FAIL")
                {
                    if ((StatusResultsFullFail[key].ContainsKey(fullKey) && StatusResultsFullFail[key][fullKey].Count() < divLine_trials * divLine_numOfTests))  //trials added
                    {
                        int fcnt = 0;
                        foreach (string t in StatusResultsFullFail[key][fullKey]) if (t == specTestName) fcnt++;
                        if (fcnt < divLine_trials) StatusResultsFullFail[key][fullKey].Add(specTestName);
                    }
                }

                if (StatusResultsFullAll[key].ContainsKey(fullKey))
                {
                    StatusResultsFullAll[key][fullKey] += 1;
                }
            }

            //summarize results
            foreach (string key in StatusResults.Keys)
            {
                Dictionary<string, string> resultLine = StatusResults[key];
                string divLine_name = key.Split('-')[0];
                string corner_name = key.Split('-')[1];
                int trialNum=1;
                int divLineTestsNum = 1;
                bool difLineFound = false;
                string divName = "";
                foreach (var TDV in SMPConf.table_divs[table])
                {
                    foreach (var TDL in TDV.Value.TableDivLines)
                    {
                        if (TDL.Key == divLine_name)
                        {
                            trialNum = TDL.Value.trials;
                            if (TDL.Value.setup_trials.Count > 0)
                            {
                                if (TDL.Value.setup_trials.ContainsKey(this.LogicUnit.setup)) trialNum = TDL.Value.setup_trials[this.LogicUnit.setup];
                            }
                            divLineTestsNum = TDL.Value.numOfTests;
                            if (TDL.Value.setup_numOfTests.Count > 0)
                            {
                                if (TDL.Value.setup_numOfTests.ContainsKey(this.LogicUnit.setup)) divLineTestsNum = TDL.Value.setup_numOfTests[this.LogicUnit.setup];
                            }
                            difLineFound = true;
                            break;
                        }
                    }
                    if (difLineFound)
                    {
                        divName = TDV.Key;
                        break;
                    }
                }

                //foreach (string excelPort in SMPConf.excelPorts)
                foreach (string excelPort in CSR_excelPorts)  
                {
                        int sum;
                        sum = 0;
                        int fsum;
                        fsum = 0;
                        int allSum;
                        allSum = 0;

                        if (resultLine.Count == 1)
                        {
                            foreach (var cntEntry in StatusResultsFullPass[key]) sum += cntEntry.Value.Count();
                            foreach (var cntEntry in StatusResultsFullFail[key]) fsum += cntEntry.Value.Count();
                            foreach (var cntEntry in StatusResultsFullAll[key]) allSum += cntEntry.Value;
                            int NumOfFullResults = calcNumOfFullResultsCyclePlan(trialNum, divLineTestsNum, divName ,divLine_name,corner_name);
                            if ((sum == NumOfFullResults) && (fsum == 0))
                            {
                                StatusResults[key][excelPort] = "Done";
                                if (allSum > NumOfFullResults) StatusResults[key][excelPort] += " " + allSum + "/" + NumOfFullResults.ToString();
                                if (sum == 0)
                                {
                                    StatusResults[key][excelPort] = "None";
                                }
                            }
                            if ((sum < NumOfFullResults) && (sum > 0)) StatusResults[key][excelPort] = "InComp " + sum.ToString() + "/" + NumOfFullResults.ToString();
                            if (fsum > 0) StatusResults[key][excelPort] = "Fail " + fsum.ToString() + "/" + NumOfFullResults.ToString();
                            if (((fsum + sum) > NumOfFullResults) && (fsum > 0)) 
                                StatusResults[key][excelPort] = "Review " + sum.ToString() + "/" + NumOfFullResults.ToString() + " ; " + fsum.ToString() + "/" + NumOfFullResults.ToString();
                            //accumulate progress
                            ProgressContainerFull += NumOfFullResults;
                            if (allSum > NumOfFullResults) ProgressContainerExecuted += NumOfFullResults;
                            else ProgressContainerExecuted += allSum;
                        }
                        else
                        {
                            foreach (var cntEntry in StatusResultsFullPass[key])
                            {
                                if (cntEntry.Key.Split('-')[0] == excelPort) sum += cntEntry.Value.Count();
                            }
                            foreach (var cntEntry in StatusResultsFullFail[key])
                            {
                                if (cntEntry.Key.Split('-')[0] == excelPort) fsum += cntEntry.Value.Count();
                            }

                            foreach (var cntEntry in StatusResultsFullAll[key])
                            {
                                if (cntEntry.Key.Split('-')[0] == excelPort) allSum += cntEntry.Value;
                            }

                            int NumOfPortResults = calcNumOfPortResultsCyclePlan(trialNum, divLineTestsNum, divName, divLine_name, corner_name, excelPort);
                            if ((sum == NumOfPortResults) && (fsum == 0))
                            {
                                StatusResults[key][excelPort] = "Done";
                                if (allSum > NumOfPortResults) StatusResults[key][excelPort] += " " + allSum + "/" + NumOfPortResults;
                                if (sum == 0)
                                {
                                    StatusResults[key][excelPort] = "None";
                                }
                            }
                            if ((sum < NumOfPortResults) && (sum > 0)) StatusResults[key][excelPort] = "InComp " + sum.ToString() + "/" + NumOfPortResults.ToString();
                            if (fsum > 0) StatusResults[key][excelPort] = "Fail " + fsum.ToString() + "/" + NumOfPortResults.ToString();
                            if (((fsum + sum) > NumOfPortResults) && (fsum > 0)) StatusResults[key][excelPort] = "Review " +
                                 sum.ToString() + "/" + NumOfPortResults.ToString() + " ; " + fsum.ToString() + "/" + NumOfPortResults.ToString();
                            //accumulate progress
                            ProgressContainerFull += NumOfPortResults;
                            if (allSum > NumOfPortResults) ProgressContainerExecuted += NumOfPortResults;
                            else ProgressContainerExecuted += allSum;
                        }
                        addCellToSpecTestNamesWithMissingOrFailedTrials(key, excelPort, trialNum);
                }

            }


        }


        public  Dictionary<string, Dictionary<string, string>> GetStatusResults()
        {
            return StatusResults;
        }

        public int GetProgressContainerFull()
        {
            return ProgressContainerFull;
        }

        public int GetProgressContainerExecuted()
        {
            return ProgressContainerExecuted;
        }

        //public Dictionary<string, List<string>> GetSpecTestNamesWithMissingTrials()
        public Dictionary<string, Dictionary<string,string>> GetSpecTestNamesWithMissingTrials()
        {
            return SpecTestNamesWithMissingTrials;
        }

        //public Dictionary<string, List<string>> GetSpecTestNamesWithFailedTrials()
        public Dictionary<string, Dictionary<string,string>> GetSpecTestNamesWithFailedTrials()
        {
            return SpecTestNamesWithFailedTrials;
        }

        public string getLinkToResults(string key,string portResultsKey, string portResultsValue,string baseQuery)
        {
            string link = "";
            //String strPathAndQuery = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
            //String strUrl = System.Web.HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");
            string strUrl = System.Configuration.ConfigurationManager.AppSettings["webHost"];
            //if (_db == System.Configuration.ConfigurationManager.AppSettings["DB_PRM"])
            //{
            link = strUrl + "ManageDB/SearchPRM?db=" + System.Configuration.ConfigurationManager.AppSettings["DB_PRM"] + "&col=" + table + "&Query=" + baseQuery;
            //}
            //else
            //{
            link = strUrl + "ManageDB/SearchTMP?db=" + System.Configuration.ConfigurationManager.AppSettings["DB_TMP"] + "&col=" + table + "&Query=" + baseQuery;
            //}
            //key format:div +  "-" + corner
            string[] terms = key.Split('-');
            if (terms.Length > 1) link += ";EQ;Corner;" + terms[terms.Length-1] ;
            //for PCIE switch between lane value to port
            string portLinkStr = "Port";
            if (this.table.StartsWith("PCIE")) portLinkStr = "Lane";
            if (portResultsKey != "NONE") link += ";Matches;"+ portLinkStr +";/" + portResultsKey + "/i";
            string div_line_name = terms[0];
            if (terms.Length > 2) div_line_name = key.Replace("-" + terms[terms.Length - 1], "");

            foreach (var TDV in SMPConf.table_divs[table])
            {
                //if (SMPConf.table_divs[table].TableDivLines.ContainsKey(div_line_name))
                if (TDV.Value.TableDivLines.ContainsKey(div_line_name))
                {
                    //Build link for "isSpecSubSet": "In;SpecTestName;Spec1,spec2,Spec3"
                    string SpecValues = "";
                    //Build link for "Additional Fields": "EQ;SetUp;setup1,Contains,Format;Ghz1M" etc
                    foreach (TableDivField TDF in TDV.Value.TableDivLines[div_line_name].tableDivFields)
                    {
                        if (TDF.isSpecSubSet)
                        {
                            SpecValues += TDF.values[0] + ",";
                            continue;
                        }
                        if (TDF.condition == "equals")
                        {
                            link += ";In;" + TDF.name + ";";
                            foreach (string val in TDF.values)
                            {
                                link += val + ",";
                            }
                            link = link.TrimEnd(',');
                        }
                        if (TDF.condition == "contains")
                        {
                            foreach (string val in TDF.values)
                            {
                                link += ";Matches;" + TDF.name + ";" + val ;
                            }
                        }
                        if (TDF.condition == "notContains")
                        {
                            foreach (string val in TDF.values)
                            {
                                link += ";NotMatches;" + TDF.name + ";" + val;
                            }
                        }
                    }
                    SpecValues = SpecValues.TrimEnd(',');
                    if (!String.IsNullOrEmpty(SpecValues))
                    {
                        link += ";In;SpecTestName;" + SpecValues;
                    }
                    
                }
            }
            return link;
        }

        public string getLogicUnitLinkToResults(string baseQuery)
        {
            string link = "";
            //String strPathAndQuery = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
            //String strUrl = System.Web.HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");
            string strUrl = System.Configuration.ConfigurationManager.AppSettings["webHost"];
            //if (_db == System.Configuration.ConfigurationManager.AppSettings["DB_PRM"])
            //{
            link = strUrl + "ManageDB/SearchPRM?db=" + System.Configuration.ConfigurationManager.AppSettings["DB_PRM"] + "&col=" + table + "&Query=" + baseQuery;
            //}
            //else
            //{
            link = strUrl + "ManageDB/SearchTMP?db=" + System.Configuration.ConfigurationManager.AppSettings["DB_TMP"] + "&col=" + table + "&Query=" + baseQuery;
            //}

            return link;
        }

        public string getInfoString(List<string> _list)
        {
            string o = " ";
            foreach (string  s in _list) o += s + " ; ";
            return o;
        }

        private void addCellToSpecTestNamesWithMissingOrFailedTrials(string key,string excelPort, int trialNum)
        {
            if ((trialNum == 1) || (StatusResults[key][excelPort].StartsWith("Done")) || (StatusResults[key][excelPort].StartsWith("Review")))
            {
                return;
            }
            Dictionary<string, string> resultLine = StatusResults[key];
            string[] terms = key.Split('-');
            string corner = terms[terms.Length-1];

            //Dictionary<string, List<string>> SpecTestNamesWithFailedTrialsTmp = new Dictionary<string, List<string>>();
            //Dictionary<string, List<string>> SpecTestNamesWithMissingTrialsTmp = new Dictionary<string, List<string>>();
            Dictionary<string, Dictionary<string, string>> SpecTestNamesWithFailedTrialsTmp = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, string>> SpecTestNamesWithMissingTrialsTmp = new Dictionary<string, Dictionary<string, string>>();


            foreach (var FailEntry in StatusResultsFullFail[key])
            {
                List<string> Specs = FailEntry.Value;
                Dictionary<string, int> SpectCnt = new Dictionary<string, int>();
                foreach (string s in Specs)
                {
                    if (!SpectCnt.ContainsKey(s)) SpectCnt.Add(s, 0);
                    SpectCnt[s]++;
                }
                foreach (var pair in SpectCnt)
                {
                    //if (!SpecTestNamesWithFailedTrialsTmp.ContainsKey(s)) SpecTestNamesWithFailedTrialsTmp.Add(s, new List<string>());
                    if (!SpecTestNamesWithFailedTrialsTmp.ContainsKey(pair.Key)) SpecTestNamesWithFailedTrialsTmp.Add(pair.Key, new Dictionary<string, string>());
                    //if (!SpecTestNamesWithFailedTrialsTmp[s].Contains(corner + "-" + FailEntry.Key)) SpecTestNamesWithFailedTrialsTmp[s].Add(corner + "-" + FailEntry.Key);
                    if (!SpecTestNamesWithFailedTrialsTmp[pair.Key].ContainsKey(corner + "-" + FailEntry.Key)) SpecTestNamesWithFailedTrialsTmp[pair.Key].Add(corner + "-" + FailEntry.Key, pair.Value.ToString() + "/" + trialNum.ToString());
                }
            }

            foreach (var PassEntry in StatusResultsFullPass[key])
            {
                List<string> Specs = PassEntry.Value;
                Dictionary<string, int> SpectCnt = new Dictionary<string, int>();
                foreach (string s in Specs)
                {
                    if (!SpectCnt.ContainsKey(s)) SpectCnt.Add(s,0);
                    SpectCnt[s]++;
                }
                foreach (var pair in SpectCnt)
                {
                    if (pair.Value < trialNum)
                    {
                       
                        //if (!SpecTestNamesWithMissingTrialsTmp.ContainsKey(pair.Key)) SpecTestNamesWithMissingTrialsTmp.Add(pair.Key, new List<string>());
                        if (!SpecTestNamesWithMissingTrialsTmp.ContainsKey(pair.Key)) SpecTestNamesWithMissingTrialsTmp.Add(pair.Key, new Dictionary<string, string>()); ;
                        //if (!SpecTestNamesWithMissingTrialsTmp[pair.Key].Contains(corner + "-" + PassEntry.Key)) SpecTestNamesWithMissingTrialsTmp[pair.Key].Add(corner + "-" + PassEntry.Key);
                        if (!SpecTestNamesWithMissingTrialsTmp[pair.Key].ContainsKey(corner + "-" + PassEntry.Key)) SpecTestNamesWithMissingTrialsTmp[pair.Key].Add(corner + "-" + PassEntry.Key, pair.Value.ToString() + "/" + trialNum.ToString());
                    }
                }
            }


            foreach (var p in SpecTestNamesWithFailedTrialsTmp)
            {
                if (!SpecTestNamesWithFailedTrials.ContainsKey(p.Key)) SpecTestNamesWithFailedTrials.Add(p.Key, p.Value);
                //foreach (string s in p.Value)
                foreach (var s in p.Value)
                {
                    //if (!SpecTestNamesWithFailedTrials[p.Key].Contains(s)) SpecTestNamesWithFailedTrials[p.Key].Add(s);
                    if (!SpecTestNamesWithFailedTrials[p.Key].ContainsKey(s.Key)) SpecTestNamesWithFailedTrials[p.Key].Add(s.Key,s.Value);
                }
            }

            foreach (var p in SpecTestNamesWithMissingTrialsTmp) 
            {
                if (!SpecTestNamesWithMissingTrials.ContainsKey(p.Key)) SpecTestNamesWithMissingTrials.Add(p.Key, p.Value);
                //foreach (string s in p.Value)
                foreach (var s in p.Value)
                {
                    //if (!SpecTestNamesWithMissingTrials[p.Key].Contains(s)) SpecTestNamesWithMissingTrials[p.Key].Add(s);
                    if (!SpecTestNamesWithMissingTrials[p.Key].ContainsKey(s.Key)) SpecTestNamesWithMissingTrials[p.Key].Add(s.Key, s.Value);
                }
            }


        }

    }


}