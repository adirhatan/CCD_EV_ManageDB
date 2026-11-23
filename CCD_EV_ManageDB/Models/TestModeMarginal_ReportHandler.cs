using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class TestModeMarginal_ReportHandler
    {



        public List<BsonDocument> CIOReport(DBsearch DBS)
        {
            //	PJ_Value - 5 rows, each hold the values: 1/2/10/50/100
            List <int> calVals = new List<int>(new int[] {1,2,10,50,100 });
            List<string> testNames = new List<string>();
            List<string> BertIPs = new List<string>();
            //  Margin_PJ_Value – Values from “PJ_Cal_Val” (PJ_Cal_Val contains 5 results per 5 PJ’s in section j.2. above per DB_Table per TestName. All data is uploaded to the DB)

            MDBStationResults MDBStationResult = new MDBStationResults(DBS.db, DBS.col);

            
            List<BsonDocument> results = new List<BsonDocument>();
            MongoCursor ResCursor;

            List<string> tables = new List<string>(new string[] { "CIO_Rounded_Gen2_Rx", "CIO_Rounded_Gen3_Rx", "TBT3_Legacy_Gen2_Rx", "TBT3_Legacy_Gen3_Rx" });
            //List<string> tables = new List<string>(new string[] { "CIO_Rounded_Gen2_Rx" });
            foreach (string table in tables)
            {
                MDBStationResult = new MDBStationResults(DBS.db, table);
                testNames.Clear();
                foreach (BsonValue tmpTestName in MDBStationResult.MDBFindDistinct(DBS.Query, "AND", "ASC", "_id", "TestName")) testNames.Add(tmpTestName.ToString());
                BertIPs.Clear();
                foreach (BsonValue tmpBertIP in MDBStationResult.MDBFindDistinct(DBS.Query, "AND", "ASC", "_id", "BERT_IP_SN")) BertIPs.Add(tmpBertIP.ToString());
                //Bit_Rate_BERT
                //Result_Origin   Test/Calibration


                foreach (int cal_pj in calVals)
                {
                    foreach (string test in testNames)
                    {
                        foreach (string Bert_IP in BertIPs)
                        {
                            BsonDocument calResultOrig = MDBStationResult.MDBFindOne(DBS.Query + ";EQ;Run;Long;EQ;TestName;" + test + ";EQ;PJ_value;" + cal_pj.ToString() + ";EQ;BERT_IP_SN;" +  Bert_IP, "AND", "DES", "TestEnd");
                            if (calResultOrig != null)
                            {
                                string BertType = calResultOrig.GetValue("BERT_Type", "").ToString();
                                BsonDocument calResult = new BsonDocument();
                                calResult.Set("PJ_value", cal_pj);
                                calResult.Set("Margin_PJ_Value", calResultOrig.GetValue("PJ_Cal_Val", ""));
                                calResult.Set("Result_Origin", "Spec");
                                calResult.Set("Bit_Rate_Bert", table + "_" + BertType + "_" + Bert_IP);
                                results.Add(calResult);
                            }
                        }
                    }
                }
                ResCursor = MDBStationResult.MDBFind(DBS.Query + ";EQ;Run;Long", "AND", "DES", "TestEnd");
                foreach (BsonDocument item in ResCursor)
                {
                    if (!item.GetValue("_id", "").ToString().ToUpper().StartsWith("RESET"))
                    {
                        string BertType = item.GetValue("BERT_Type", "").ToString();
                        string BertIP = item.GetValue("BERT_IP_SN", "").ToString();
                        item.Set("Result_Origin", "Test");
                        item.Set("Bit_Rate_Bert", table + "_" + BertType + "_" + BertIP);
                        results.Add(item);
                    }
                }
            }

            return results;
        }
    }
}