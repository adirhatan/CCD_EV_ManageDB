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

namespace CCD_EV_ManageDB.Models
{
    public class FXVLLoader
    {

        public bool LoadLANFile(out string errorMsg, out string message, string excelFileName, string project, string phase, string table)
        {
            errorMsg = "";
            message = "";
            int resultsLoaded = 0;
            int resultsChanged = 0;

            List<string> colNames = new List<string>();
            List<BsonDocument> ResultsRecs = new List<BsonDocument>();

            int index = 0;
            FileInfo newFile = new FileInfo(excelFileName);
            string nowStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Double now = Convert.ToDouble(DateTime.Now.ToString("yyyyMMddHHmmss"));
            int sheetNum = 0;
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                sheetNum++;   
                foreach (ExcelWorksheet displayWorksheet in package.Workbook.Worksheets)
                {
                    var tmp = displayWorksheet.Cells[1, 1].Value;
                    var tmp2 = displayWorksheet.Cells[1, 1].Value;
                    if (displayWorksheet.Name == "Summary") continue;
                    //TODO: Parse Metadata from excel beginning; add viatl fields needed - SessionStart and more (?) 
                    //
                    //line:4
                    //cols: 1  (,5,10)
                    //data: DUT_host: , LP_host:, Cable_Type:	
                    int MetaDataLine = 4;
                    int ColNamesLine = 15;
                    BsonDocument metaData = new BsonDocument();
                    for (int i = MetaDataLine; i < ColNamesLine; i++)
                    {
                        tmp = displayWorksheet.Cells[i, 1].Value;
                        tmp2 = displayWorksheet.Cells[i, 2].Value;
                        if ((tmp != null) && (tmp2!=null))
                        {
                            tmp = tmp.ToString().Replace('.','_').TrimEnd(':').TrimEnd(' ');
                            metaData.Add(tmp.ToString(), tmp2.ToString());
                        }

                        //need to format temprature
                        string tmpr = metaData.GetValue("DUT_Temp ( c )", "").ToString();
                        if (!string.IsNullOrEmpty(tmpr))
                        {
                            tmpr = Regex.Replace(tmpr,@"\D","");
                            metaData.Set("DUT_Temp ( c )", Convert.ToDouble(tmpr));
                        }
                    }

                    //Parse results
                    colNames.Clear();
                    index = 1;
                    //parse colname line starting with "Cable_Length"

                    for (int i = ColNamesLine; i < displayWorksheet.Dimension.End.Row; i++)
                    {
                        tmp = displayWorksheet.Cells[i, 1].Value;
                        if ((tmp != null) && (tmp.ToString().StartsWith("Cable_Length")))
                        {
                            ColNamesLine = i;
                            break;
                        }
                    }

                    while (displayWorksheet.Cells[ColNamesLine, index].Value != null)
                    {
                        string fieldName = Convert.ToString(displayWorksheet.Cells[ColNamesLine, index].Value).Replace("\n","");
                        if (fieldName.Contains("Pass/Fail")) fieldName = "TestStatus";
                        colNames.Add(fieldName);
                        index++;
                        //accumulate column names into array...
                    }



                    int rowCnt = ColNamesLine ;
                    string cell1 = "";
                    while (rowCnt < displayWorksheet.Dimension.End.Row)
                    {
                        BsonDocument result = metaData.DeepClone().AsBsonDocument;
                        rowCnt++;
                        if (rowCnt == 1) continue;
                        if ((Convert.ToString(displayWorksheet.Cells[rowCnt, 1].Value) != null) && (Convert.ToString(displayWorksheet.Cells[rowCnt, 1].Value) != "")) 
                        {cell1 = Convert.ToString(displayWorksheet.Cells[rowCnt, 1].Value);}
                        result.Add(colNames.ElementAt(0), cell1);
                        for (int i = 2; i < index; i++)
                        {
                            string value = Convert.ToString(displayWorksheet.Cells[rowCnt, i].Value);
                            if (value != null)
                            {
                                BsonValue BVal = null;
                                double d;
                                if ((colNames.ElementAt(i - 1) != "UnitId") && (colNames.ElementAt(i - 1) != "BoardNumber") && (double.TryParse(value, out d))) BVal = d;
                                else BVal = value;
                                result.Set(colNames.ElementAt(i - 1), BVal);
                            }

                        }
                        //TODO:
                        //add metadata parsed at beginning
                        now++;
                        result.Add("SessionStart", nowStr);
                        result.Add("HIDDEN_SessionStart", Convert.ToDouble(nowStr.Replace("_","")));
                        result.Add("TestStart", now.ToString().Substring(0,8) + "_" + now.ToString().Substring(8,6)) ;
                        result.Add("TestEnd", now.ToString().Substring(0, 8) + "_" + now.ToString().Substring(8, 6));
                        result.Add("HIDDEN_TestStart", now);
                        result.Add("HIDDEN_TestEnd", now);
                        result.Add("HIDDEN_LoadToPRM", now); //?? maybe elsewere
                        result.Add("Indicator", "Y"); //?? maybe elsewere
                        result.Add("ProjectName", project);
                        result.Add("Phase", phase);
                        result.Add("Corner", displayWorksheet.Name);


                        string SpecTestName = "Cable_Length_" + result.GetValue("Cable_Length", "").ToString() + "_Speed_" + result.GetValue("Speed", "").ToString() + "_" 
                            + result.GetValue("DUT_Master/Slave", "").ToString();
                        SpecTestName = SpecTestName.TrimEnd('_');
                        result.Add("SpecTestName", SpecTestName);
                        //add ID
                        Random rnd = new Random();
                        int rndSigma = rnd.Next(1, 10000);
                        string _id = "Test_WebLoadResults" + rndSigma.ToString() + "_" + result.GetValue("TestEnd", "").ToString();
                        result.Set("_id", _id);

                        //add SpecTestName
                        ResultsRecs.Add(result);
                    }

                    //Load new results to DB; in case of different new result, change exist to "Indicator N" and load new.
                    MDBStationResults MDBStationResultsObj = new MDBStationResults("DB_PRM", table);
                    string actualValue = "DUT_BER";
                    if (table == "LAN_TTL") actualValue = "TTL_avg";

                    foreach (BsonDocument doc in ResultsRecs)
                    {
                        //check if result actually was executed (val is not 0)
                        if ((string.IsNullOrEmpty(doc.GetValue(actualValue, "").ToString()))) continue;
                        //check if we already have it in DB
                        string query = "EQ;SpecTestName;" + doc.GetValue("SpecTestName", "") + ";EQ;Corner;" + doc.GetValue("Corner", "") + ";EQ;DUT_Sigma;" + doc.GetValue("DUT_Sigma", "") + ";EQ;Indicator;Y"; 
                        BsonDocument ExistResult = MDBStationResultsObj.MDBFindOne(query, "AND", "DES", "_id");
                        if ((ExistResult != null) && (!string.IsNullOrEmpty(ExistResult.GetValue("_id", "").ToString())))
                        {
                            //in case "TestStatus" was change but test was not re-executed (actual result is the same)
                            if (ExistResult.GetValue("TestStatus", "").ToString() != doc.GetValue("TestStatus", "").ToString())
                            {
                                if (ExistResult.GetValue(actualValue, "").ToString() == doc.GetValue(actualValue, "").ToString())
                                {
                                    ExistResult.Set("TestStatus", doc.GetValue("TestStatus", "").ToString());
                                    MDBStationResultsObj.Save(ExistResult);
                                    resultsChanged++;
                                }
                            }
                            //need to compare some vital fields to decide if it is same result or new result
                            if (ExistResult.GetValue(actualValue, "").ToString() == doc.GetValue(actualValue, "").ToString()) continue;
                            ExistResult.Set("Indicator", "N");
                            MDBStationResultsObj.Save(ExistResult);
                            MDBStationResultsObj.Save(doc);
                            resultsLoaded++;
                        }
                        else
                        {
                            MDBStationResultsObj.Save(doc);
                            resultsLoaded++;
                        }

                    }

                }

                message = resultsLoaded.ToString() + " results were loaded.";
                if (resultsChanged > 0) message += " " + resultsChanged.ToString() + " results were status changed.";
            }
            //New: Copy all data excluding Cable_Length > 102M, to <table>_102M table
            string table102 = table + "_102M";
            MDBStationResults MDBStationResultsObj102M = new MDBStationResults("DB_PRM", table102);
            MDBStationResultsObj102M.CopyFXVL102MData();
            //Enc Copy _102M
            return true;
        }


    }
}