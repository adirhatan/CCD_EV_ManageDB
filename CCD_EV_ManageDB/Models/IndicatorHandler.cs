using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Data;
using System.IO;
using OfficeOpenXml;
using CCD_EV_ManageDB.Repositories;
using CCD_EV_ManageDB.Helpers;

namespace CCD_EV_ManageDB.Models
{
    public class IndicatorHandler
    {
        SessionsManageDisplay SMDisp = null;

        public IndicatorDisplayResult LaunchIndicatorOld(string _id)
        {
            IndicatorDisplayResult DispResult = new IndicatorDisplayResult();
            Indicator Ind = new MDBIndicator().ReadIndicator(_id);
            IndicatorResults IR = new IndicatorResults(Ind);
            MDBStationResults StationResults = new MDBStationResults("DB_PRM", Ind.table);
            for (int weekCnt=0; weekCnt<IR.weeks.Count;weekCnt++)
            {
                int testCnt = 0;
                foreach (IndTest test in Ind.tests)
                {
                    WorkWeek ww = test.WorkWeeks[weekCnt];
                    //if (Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd")) < Convert.ToInt32(ww.Sdate)) break; //??
                    //No. of planned tests - weekly, this week only
                    IR.weeks[weekCnt].plannedTests += ww.PlannedOrig;
                    //No. of planned tests - accumulative , sum all planned
                    IR.weeks[weekCnt].plannedTestsAc += ww.PlannedOrig;
                    DateTime wwe = new DateTime(Convert.ToInt32(ww.Sdate.Substring(0, 4)), Convert.ToInt32(ww.Sdate.Substring(4, 2)), Convert.ToInt32(ww.Sdate.Substring(6, 2)));
                    string wwEnd = wwe.AddDays(Convert.ToDouble(7)).ToString("yyyyMMdd");
                    string query = "";
                    query += "EQ;SpecTestName;" + test.SpecTestName + ";";
                    query += "GT;SessionStart;" + ww.Sdate + ";";
                    query += "LT;SessionStart;" + wwEnd;
                    //query += "ExistNotEmpty;" + test.Field + "; ;";
                    MongoCursor resCur = StationResults.MDBFind(query, "AND", "Des", "TestEnd");
                    Ind.tests[testCnt].WorkWeeks[weekCnt].Executed = resCur.Count();
                    //No. of tests executed tests - accumulative , sum all planned
                    IR.weeks[weekCnt].executedTestsAc += resCur.Count();
                    //No. of tests executed tests - weekly
                    IR.weeks[weekCnt].executedTests += resCur.Count();
                    query += ";Matches;TestStatus;/^pass$/i";
                    resCur = StationResults.MDBFind(query, "AND", "Des", "TestEnd");
                    Ind.tests[testCnt].WorkWeeks[weekCnt].Passed = resCur.Count();
                    //No. of passed tests  - accumulative, sum all executed passed (query?)
                    IR.weeks[weekCnt].passedTestsAc += resCur.Count();
                    testCnt++;
                }
            }

            //Summary for central indicator Excel
            //No. of planned tests - accumulative , sum all planned
            //No. of tests executed  - accumulative, sum all executed
            //No. of passed tests  - accumulative, sum all executed passed (query?)
            //No. of failed tests  - accumulative
            //No. of blocked tests  - accumulative, ??
            //No. of pending tests  - accumulative, planned - executed
            //Coverage % - executes VS planned
            
            //foreach (IndWeekSum weekSum in IR.weeks)
            //{
            for (int weekCnt=0; weekCnt<IR.weeks.Count;weekCnt++)
            {
                //if (Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd")) < Convert.ToInt32(IR.weeks[weekCnt].Sdate)) break;//??
                
                //complete calculation of Acc vals by adding prevoise week val
                if (weekCnt > 0)
                {
                    IR.weeks[weekCnt].plannedTestsAc += IR.weeks[weekCnt-1].plannedTestsAc;
                    IR.weeks[weekCnt].executedTestsAc += IR.weeks[weekCnt-1].executedTestsAc;
                    IR.weeks[weekCnt].executedTests += IR.weeks[weekCnt-1].executedTests;
                    IR.weeks[weekCnt].passedTestsAc += IR.weeks[weekCnt-1].passedTestsAc;
                }
                //No. of failed tests  - accumulative
                IR.weeks[weekCnt].failedTestsAc = IR.weeks[weekCnt].executedTestsAc - IR.weeks[weekCnt].passedTestsAc;
                //No. of blocked tests  - accumulative, ??
               
                //No. of pending tests  - accumulative, planned - executed
                IR.weeks[weekCnt].pendingTestsAc = IR.weeks[weekCnt].plannedTestsAc - IR.weeks[weekCnt].executedTestsAc - IR.weeks[weekCnt].blockedTestsAc;
                if (IR.weeks[weekCnt].pendingTestsAc < 0) IR.weeks[weekCnt].pendingTestsAc = 0;
                //Coverage % - executes VS planned
                //fval = String.Format(CultureInfo.InvariantCulture, "{0:0.00}", Convert.ToDouble(val));  //string format 
                IR.weeks[weekCnt].CoverageAc = (Convert.ToDouble(IR.weeks[weekCnt].executedTestsAc) / Convert.ToDouble(IR.weeks[weekCnt].plannedTestsAc));

            }

            DispResult.Ind = Ind;
            DispResult.Res = IR;
            return DispResult;
        }

        public IndicatorDisplayResult LaunchIndicator(string _id, bool isOfficial)
        {
            return LaunchIndicator(_id, isOfficial, false, "PRM");
        }
        public IndicatorDisplayResult LaunchIndicator(string _id, bool isOfficial, bool planOnly)
        {
            return LaunchIndicator(_id, isOfficial, planOnly, "PRM");
        }
        public IndicatorDisplayResult LaunchIndicator(string _id, bool isOfficial, bool planOnly, string db)
        {
            //TODO: Create one query - query += "GT;SessionStart;" + ww.Sdate  of first date
            //      Than iterate results, and poulate each week, (can try to have "buckets" of each week)
            //      Need to change only the first part bely, Summary calculation part may stay the same...

            IndicatorDisplayResult DispResult = new IndicatorDisplayResult();
            Indicator Ind = new MDBIndicator().ReadIndicator(_id);
            IndicatorResults IR = new IndicatorResults(Ind);

            DateTime wwe = new DateTime(Convert.ToInt32(Ind.wws.Substring(0, 4)), Convert.ToInt32(Ind.wws.Substring(4, 2)), Convert.ToInt32(Ind.wws.Substring(6, 2)));
            string wwEnd = wwe.AddDays(Convert.ToDouble(7*Convert.ToInt32(Ind.wn))).ToString("yyyyMMdd"); //TODO: maybe another week needed??
            string query = "";
            MDBStationResults StationResults = null;

            if (db.ToUpper() == "PRM")
            {
                if (planOnly) query += "EQ;ProjectName;NotExists;"; //make query without any results
                if (isOfficial) query += "EQ;Indicator;Y;";
                query += "Matches;ProjectName;/" + Ind.project + "/i;";
                //Special fix for HBR A0 unified with A1:
                if ((Ind.project == "HBR") && (Ind.phase == "A0")) query += "Matches;Phase;/A[01]/i;";
                else query += "Matches;Phase;/" + Ind.phase + "/i;";
                query += "GT;HIDDEN_LoadToPRM;" + Ind.wws + "000000;";
                //query += "GT;SessionStart;" + Ind.wws + ";";
                query += "LT;HIDDEN_LoadToPRM;" + wwEnd + "235959";
                //query += "LT;SessionStart;" + wwEnd + "000000"; 
                StationResults = new MDBStationResults("DB_PRM", Ind.table);
            }
            if (db.ToUpper() == "TMP") 
            {
                query = getSMqueryTMP_Valid(Ind);
                if (string.IsNullOrEmpty(query)) return DispResult;
                StationResults = new MDBStationResults("DB_TMP", Ind.table);            
            }
            
            MongoCursor resCur = StationResults.MDBFind(query, "AND", "Des", "TestEnd");

            if ((db.ToUpper() == "TMP") && (resCur.Count() == 0)) return DispResult; 
            
            int weekCnt = 0;

            foreach (BsonDocument result in resCur)
            {
                if (db.ToUpper() == "PRM") weekCnt = CCDUtilities.calcTndWeek(wwe, result.GetValue("HIDDEN_LoadToPRM", "00000000").ToString().Substring(0, 8));
                if (db.ToUpper() == "TMP") weekCnt = CCDUtilities.calcTndWeek(wwe, result.GetValue("SessionStart", "00000000").ToString().Substring(0, 8));
                if (weekCnt < 0) continue;
                int testCnt = -1;
                foreach (IndTest test in Ind.tests)
                {
                    testCnt++;
                    if (result.GetValue("SpecTestName", "").ToString().Trim().ToUpper() != test.SpecTestName.Trim().ToUpper()) continue;
                    WorkWeek ww = test.WorkWeeks[weekCnt];

                    Ind.tests[testCnt].WorkWeeks[weekCnt].Executed++;
                    //No. of tests executed tests - accumulative , sum all planned
                    IR.weeks[weekCnt].executedTestsAc++;
                    //No. of tests executed tests - weekly
                    IR.weeks[weekCnt].executedTests++;
                    if (result.GetValue("TestStatus", "").ToString().ToUpper() == "PASS")
                    {
                        Ind.tests[testCnt].WorkWeeks[weekCnt].Passed++;
                        //No. of passed tests  - accumulative, sum all executed passed (query?)
                        IR.weeks[weekCnt].passedTestsAc++;
                    }
                    break;
                }
            }


            //Summary for central indicator Excel
            //No. of planned tests - accumulative , sum all planned
            //No. of tests executed  - accumulative, sum all executed
            //No. of passed tests  - accumulative, sum all executed passed (query?)
            //No. of failed tests  - accumulative
            //No. of blocked tests  - accumulative, ??
            //No. of pending tests  - accumulative, planned - executed
            //Coverage % - executes VS planned

            //foreach (IndWeekSum weekSum in IR.weeks)
            //{
            for (int weekCntSum = 0; weekCntSum < IR.weeks.Count; weekCntSum++)
            {
                //if (Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd")) < Convert.ToInt32(IR.weeks[weekCnt].Sdate)) break;//??

                //New Blocked
                foreach (IndTest iTest in Ind.tests)
                {
                    if (iTest.WorkWeeks[weekCntSum].Blocked) IR.weeks[weekCntSum].blockedTestsAc += iTest.WorkWeeks[weekCntSum].PlannedOrig;
                }

                //complete calculation of Acc vals by adding prevoise week val
                if (weekCntSum > 0)
                {
                    IR.weeks[weekCntSum].plannedTestsAc += IR.weeks[weekCntSum - 1].plannedTestsAc;
                    IR.weeks[weekCntSum].executedTestsAc += IR.weeks[weekCntSum - 1].executedTestsAc;
                    IR.weeks[weekCntSum].executedTests += IR.weeks[weekCntSum - 1].executedTests;
                    IR.weeks[weekCntSum].passedTestsAc += IR.weeks[weekCntSum - 1].passedTestsAc;
                    IR.weeks[weekCntSum].blockedTestsAc += IR.weeks[weekCntSum - 1].blockedTestsAc;
                }
                //No. of failed tests  - accumulative
                IR.weeks[weekCntSum].failedTestsAc = IR.weeks[weekCntSum].executedTestsAc - IR.weeks[weekCntSum].passedTestsAc;
                //No. of blocked tests  - accumulative, ??
                //No. of pending tests  - accumulative, planned - executed
                IR.weeks[weekCntSum].pendingTestsAc = IR.weeks[weekCntSum].plannedTestsAc - IR.weeks[weekCntSum].executedTestsAc;
                if (IR.weeks[weekCntSum].pendingTestsAc < 0) IR.weeks[weekCntSum].pendingTestsAc = 0;
                //Coverage % - executes VS planned
                //fval = String.Format(CultureInfo.InvariantCulture, "{0:0.00}", Convert.ToDouble(val));  //string format 
                IR.weeks[weekCntSum].CoverageAc = (Convert.ToDouble(IR.weeks[weekCntSum].executedTestsAc) / Convert.ToDouble(IR.weeks[weekCntSum].plannedTestsAc));

            }

            DispResult.Ind = Ind;
            DispResult.Res = IR;
            return DispResult;
        }

        string getSMqueryTMP_Valid(Indicator Ind)
        {
            string query = "";
            if (SMDisp == null)
            {
                SessionManageHandler SM = new SessionManageHandler();
                string protocol = Ind_Id2Protocol(Ind);
                if (string.IsNullOrEmpty(protocol)) return "";
                SMDisp = new SessionManageHandler().GetSessionsManageDisplay(protocol, Ind.project, Ind.phase, "Y");
                foreach (SessionsManageOfTable SMT in SMDisp.SessionsManageOfTables)
                {
                    if (SMT.table != Ind.table) continue;
                    foreach (SessionsManage _SM in SMT.Sessions)
                    {
                        if (_SM.validation == General.SessionValidationStatus.TMP_valid) foreach (string s in _SM.Sessions) query += s + ',';
                    }
                }
            }
            if (!string.IsNullOrEmpty(query)) query = "In;SessionStart;" + query.TrimEnd(',');
            return query;
        }
        private string Ind_Id2Protocol(Indicator Ind)
        {
            //<add key="Protocols" value="USB3_RX,USB3_TX,USB2_TX,CIO_RL,CIO_RX,CIO_TX,CIO_RETIMER,PCIE_DC,PCIE_PLL,PCIE_RX,PCIE_RL,PCIE_TX,Crystal,Power_Consumption,Power_Code2Voltage,Power_Integrity,Power_Profiling,DP_Common,DP_RX,DP_SBXX,DP_TX" />
            string protocol_prefix = "";
            if (Ind._id.ToUpper().StartsWith("DP")) protocol_prefix =  "DP";
            if (Ind._id.ToUpper().StartsWith("PCIE")) protocol_prefix = "PCIE";
            if (Ind._id.ToUpper().StartsWith("CIO")) protocol_prefix = "CIO";
            if (Ind._id.ToUpper().StartsWith("TBT")) protocol_prefix = "DP";
            if (Ind._id.ToUpper().StartsWith("POWER")) protocol_prefix = "Power";
            if (Ind._id.ToUpper().StartsWith("USB")) protocol_prefix = "USB";
            if (Ind._id.ToUpper().StartsWith("CRYSTAL")) protocol_prefix = "Crystal";
            MongoCursor Cur = new MDBCyclePlanMetaData().MDBFind("Matches;_id;/^" + protocol_prefix + ".*" + Ind.project + "_" + Ind.phase + "$/", "AND", "ASC", "_id");
            foreach (BsonDocument doc in Cur)
            {
                CyclePlanMetaData CPMetaData = new CyclePlanMetaData(doc);
                foreach (var pair in CPMetaData.IndicatorsMeta)
                {
                    if ((Ind.table == pair.Key) && (Ind.spec == pair.Value.spec)) return CPMetaData.protocol;
                }
            }
            return "";
        }

        public IndicatorDisplayResult MergeDispResults(string _id, IndicatorDisplayResult DispResultPRM, IndicatorDisplayResult DispResultTMP)
        {
            //TODO: Create one query - query += "GT;SessionStart;" + ww.Sdate  of first date
            //      Than iterate results, and poulate each week, (can try to have "buckets" of each week)
            //      Need to change only the first part bely, Summary calculation part may stay the same...

            IndicatorDisplayResult DispResult = new IndicatorDisplayResult();
            Indicator Ind = new MDBIndicator().ReadIndicator(_id);
            IndicatorResults IR = new IndicatorResults(Ind);

            DateTime wwe = new DateTime(Convert.ToInt32(Ind.wws.Substring(0, 4)), Convert.ToInt32(Ind.wws.Substring(4, 2)), Convert.ToInt32(Ind.wws.Substring(6, 2)));
            string wwEnd = wwe.AddDays(Convert.ToDouble(7 * Convert.ToInt32(Ind.wn))).ToString("yyyyMMdd"); //TODO: maybe another week needed??



            //Summary for central indicator Excel
            //No. of planned tests - accumulative , sum all planned
            //No. of tests executed  - accumulative, sum all executed
            //No. of passed tests  - accumulative, sum all executed passed (query?)
            //No. of failed tests  - accumulative
            //No. of blocked tests  - accumulative, ??
            //No. of pending tests  - accumulative, planned - executed
            //Coverage % - executes VS planned

            for (int weekCntSum = 0; weekCntSum < IR.weeks.Count; weekCntSum++)
            {

                IR.weeks[weekCntSum].plannedTestsAc = DispResultPRM.Res.weeks[weekCntSum].plannedTestsAc;
                IR.weeks[weekCntSum].executedTestsAc = DispResultPRM.Res.weeks[weekCntSum].executedTestsAc + DispResultTMP.Res.weeks[weekCntSum].executedTestsAc;
                IR.weeks[weekCntSum].executedTests = DispResultPRM.Res.weeks[weekCntSum].executedTests + DispResultTMP.Res.weeks[weekCntSum].executedTests;
                IR.weeks[weekCntSum].passedTestsAc = DispResultPRM.Res.weeks[weekCntSum].passedTestsAc + DispResultTMP.Res.weeks[weekCntSum].passedTestsAc;
                IR.weeks[weekCntSum].blockedTestsAc = DispResultPRM.Res.weeks[weekCntSum].blockedTestsAc;
                
                //No. of failed tests  - accumulative
                IR.weeks[weekCntSum].failedTestsAc = IR.weeks[weekCntSum].executedTestsAc - IR.weeks[weekCntSum].passedTestsAc;
                //No. of blocked tests  - accumulative, ??
                //No. of pending tests  - accumulative, planned - executed
                IR.weeks[weekCntSum].pendingTestsAc = IR.weeks[weekCntSum].plannedTestsAc - IR.weeks[weekCntSum].executedTestsAc;
                if (IR.weeks[weekCntSum].pendingTestsAc < 0) IR.weeks[weekCntSum].pendingTestsAc = 0;
                //Coverage % - executes VS planned
                //fval = String.Format(CultureInfo.InvariantCulture, "{0:0.00}", Convert.ToDouble(val));  //string format 
                IR.weeks[weekCntSum].CoverageAc = (Convert.ToDouble(IR.weeks[weekCntSum].executedTestsAc) / Convert.ToDouble(IR.weeks[weekCntSum].plannedTestsAc));

            }

            DispResult.Ind = Ind;
            DispResult.Res = IR;
            return DispResult;
        }

    }
}