using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class TestModeMarginal_ReportHandler_Depricated
    {
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }


        public string GenerateReport(string _interface, string db_str, string project, string phase, string nowStr)
        {
            //prepare file path (or get it?)
            string EmptyExcel = Path.Combine(ServerDir, "EVData", "JTOL_Margin_Tmpl_" + _interface + ".xlsx");
            //if (!System.IO.File.Exists(EmptyExcel)) EmptyExcel = //TODO Error handling
            string filename = "JTOL_Margin_Report_" + _interface + "_" + project + "_" + phase + "_" + "_" + nowStr + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(EmptyExcel, filepath, true);

            TestModeMarginalReport_Base dataObj;
            switch (_interface)
            {
                case "CIO":
                    dataObj = new TestModeMarginalReport_CIO();
                    break;
                default:
                    return filepath;            
                    break;
            }
            dataObj.BuildReport(filepath, db_str, project, phase);
            return filepath;
        }

    }

    public class TestModeMarginal_UnitData
    {
        //TODO - need to see if it is generic for all interfaces, shoul hold one sheet data
        public string unit;
        public List<BsonDocument> Graph1;
        public List<BsonDocument> Graph2;
        public List<BsonDocument> Graph3;
        public List<BsonDocument> Graph4;

        public TestModeMarginal_UnitData()
        {
            Graph1 = new List<BsonDocument>();
            Graph2 = new List<BsonDocument>();
            Graph3 = new List<BsonDocument>();
            Graph4 = new List<BsonDocument>();
        }
        public TestModeMarginal_UnitData(string _unit)
        {
            unit = _unit;
            Graph1 = new List<BsonDocument>();
            Graph2 = new List<BsonDocument>();
            Graph3 = new List<BsonDocument>();
            Graph4 = new List<BsonDocument>();
        }

        public double getMarginResult(string GraphNumStr, string cornerName, string SJ)
        {
            List<BsonDocument> Graph;
            switch (GraphNumStr)
            {
                case "1":
                    Graph = this.Graph1;
                    break;
                case "2":
                    Graph = this.Graph2;
                    break;
                case "3":
                    Graph = this.Graph3;
                    break;
                case "4":
                    Graph = this.Graph4;
                    break;
                default:
                    return -999999;
            }
            double result = -999999;
            Double _SJ;
            if (!Double.TryParse(SJ, out _SJ)) return result;
            foreach (BsonDocument doc in Graph)
            {
                if ((doc.GetValue("Corner").ToString() == cornerName) && (doc.GetValue("SJ").ToDouble() == Convert.ToDouble(SJ)))
                {
                    result = doc.GetValue("Margin Test Result").ToDouble();
                    break;
                }
            }
            return result;
        }
    }

    public abstract class TestModeMarginalReport_Base
    {
        public MongoCursor AllResultsCur;
        public List<string> UnitsList;

        public TestModeMarginalReport_Base()
        {
            UnitsList = new List<string>();
        }

        public virtual void GenQuery(string db_str,string project, string phase){}

        public virtual void  buildUnitsList(){}

        public virtual TestModeMarginal_UnitData GetDataForUnit(string db_str,string Unit)
        {
            TestModeMarginal_UnitData dummy = new TestModeMarginal_UnitData();
            return dummy;
        }

        public virtual void BuildExcelSheet(string filePath, TestModeMarginal_UnitData data){}

        protected void BuildGraph(ExcelWorksheet worksheet, TestModeMarginal_UnitData data, string graphNumStr, int firstCornerLine, int firstCornerCol, int titleLine, string titlePlaceHolder)
        {
            var tmp = worksheet.Cells[titleLine, firstCornerCol].Value;
            if (!tmp.ToString().StartsWith(titlePlaceHolder)) return;
            //TODO put Skew/ Lane Port values
            worksheet.Cells[titleLine, firstCornerCol].Value = tmp.ToString().Replace(titlePlaceHolder, "");
            //TODO MetaData in cell(I,8)

            int cornerCnt = 1;
            int dataStart = firstCornerLine + 1;
            string cornerName = "";
            tmp = worksheet.Cells[firstCornerLine, firstCornerCol].Value;
            if (!tmp.ToString().ToUpper().StartsWith("CORNER")) return;
            tmp = worksheet.Cells[dataStart, firstCornerCol].Value;
            cornerName = tmp.ToString().ToUpper();
            int i = 0;
            while (cornerCnt < 6)
            {
                tmp = worksheet.Cells[dataStart + i, firstCornerCol + 1].Value;
                if ((tmp == null) || (string.IsNullOrWhiteSpace(tmp.ToString())))
                {
                    if (cornerCnt == 5) break;
                    i++;
                    continue;
                }
                //assign data from  "data"
                Double marginResult = data.getMarginResult(graphNumStr, cornerName, tmp.ToString());
                if (marginResult > -999999) worksheet.Cells[dataStart + i, firstCornerCol + 3].Value = marginResult;
                tmp = worksheet.Cells[dataStart + i, firstCornerCol].Value;
                if ((tmp != null) && (tmp.ToString().ToUpper().StartsWith("CORNER")))
                {
                    cornerCnt++;
                    tmp = worksheet.Cells[dataStart + i + 1, firstCornerCol].Value;
                    cornerName = tmp.ToString().ToUpper();
                }
                i++;
            }

        }


        public void BuildReport(string filePath, string db_str, string project, string phase)
        {
            GenQuery(db_str, project, phase);
            buildUnitsList();
            foreach (string unit in this.UnitsList)
            {
                TestModeMarginal_UnitData udata = GetDataForUnit(db_str,unit);
                BuildExcelSheet(filePath, udata);
            }
        }

    }

    public class TestModeMarginalReport_CIO: TestModeMarginalReport_Base
    {
        public MongoCursor AllResultsCurCIO_Gen2;
        public MongoCursor AllResultsCurCIO_Gen3;
        public MongoCursor AllResultsCurTBT3_Gen2;
        public MongoCursor AllResultsCurTBT3_Gen3;


        public override void GenQuery(string db_str, string project, string phase)
        {
            //TODO: DB_TMP or PRM parameter?
            MDBStationResults MDBStationResult = new MDBStationResults(db_str, "CIO_Rounded_Gen2_Rx");
            //TODO: Query should be based on Spec with marginal suffix, and project phase
            AllResultsCurCIO_Gen2 = MDBStationResult.MDBFind("EQ;SessionStart;20190926_130849;EQ;Run;Long", "AND", "DES", "TestEnd");
            MDBStationResult = new MDBStationResults(db_str, "CIO_Rounded_Gen3_Rx");
            AllResultsCurCIO_Gen3 = MDBStationResult.MDBFind("EQ;SessionStart;20190926_131405;EQ;Run;Long", "AND", "DES", "TestEnd");
            //MDBStationResult = new MDBStationResults("TMP_DB", "TBT3_Legacy_Gen2_Rx");
            //AllResultsCurTBT3_Gen2 = MDBStationResult.MDBFind("EQ;SessionStart;20190926_130849;EQ;Run;Long", "AND", "DES", "TestEnd");
            //MDBStationResult = new MDBStationResults("TMP_DB", "TBT3_Legacy_Gen3_Rx");
            //AllResultsCurTBT3_Gen3 = MDBStationResult.MDBFind("EQ;SessionStart;20190926_130849;EQ;Run;Long", "AND", "DES", "TestEnd");
        }

        public override void  buildUnitsList()
        {
            foreach (BsonDocument result in AllResultsCurCIO_Gen2)
            {
                //if (!UnitsList.Contains(result.GetValue("BoardNumber", "").ToString())) UnitsList.Add(result.GetValue("BoardNumber", "").ToString());
                if (!UnitsList.Contains(result.GetValue("Skew", "").ToString())) UnitsList.Add(result.GetValue("Skew", "").ToString());
            }
            //foreach (BsonDocument result in AllResultsCurCIO_Gen3)
            //{
            //    if (!UnitsList.Contains(result.GetValue("BoardNumber", "").ToString())) UnitsList.Add(result.GetValue("BoardNumber", "").ToString());
            //}
        //    foreach (BsonDocument result in AllResultsCurTBT3_Gen2)
        //    {
        //        if (!UnitsList.Contains(result.GetValue("BoardNumber", "").ToString())) UnitsList.Add(result.GetValue("BoardNumber", "").ToString());
        //    }
        //    foreach (BsonDocument result in AllResultsCurTBT3_Gen3)
        //    {
        //        if (!UnitsList.Contains(result.GetValue("BoardNumber", "").ToString())) UnitsList.Add(result.GetValue("BoardNumber", "").ToString());
        //    }
        }
        public override TestModeMarginal_UnitData  GetDataForUnit(string db_str, string Unit)
        {
            TestModeMarginal_UnitData data = new TestModeMarginal_UnitData(Unit);
            MongoCursor UnitResultsCur;
            MDBStationResults MDBStationResult = new MDBStationResults(db_str, "CIO_Rounded_Gen2_Rx");
            //TODO query; make sure all have same Skew/Port/Lane (??)
            //UnitResultsCur = MDBStationResult.MDBFind("EQ;BoardNumber;" + Unit + ";EQ;SessionStart;20190926_130849;EQ;Run;Long", "AND", "DES", "TestEnd");
            UnitResultsCur = MDBStationResult.MDBFind("EQ;Skew;" + Unit + ";EQ;SessionStart;20190926_130849;EQ;Run;Long", "AND", "DES", "TestEnd");
            foreach (BsonDocument result in UnitResultsCur)
            {
                //Put data in "data" object
                data.Graph1.Add(MapFields(result));
            }

            MDBStationResult = new MDBStationResults(db_str, "CIO_Rounded_Gen3_Rx");
            //TODO query; make sure all have same Skew/Port/Lane (??)
            //UnitResultsCur = MDBStationResult.MDBFind("EQ;BoardNumber;" + Unit + ";EQ;SessionStart;20190926_131405;EQ;Run;Long", "AND", "DES", "TestEnd");
            UnitResultsCur = MDBStationResult.MDBFind("EQ;Skew;" + Unit + ";EQ;SessionStart;20190926_131405;EQ;Run;Long", "AND", "DES", "TestEnd");
            foreach (BsonDocument result in UnitResultsCur)
            {
                //Put data in "data" object
                data.Graph2.Add(MapFields(result));
            }

            //MDBStationResult = new MDBStationResults("TMP_DB", "TBT3_Legacy_Gen2_Rx");
            ////TODO query; make sure all have same Skew/Port/Lane (??)
            //UnitResultsCur = MDBStationResult.MDBFind("EQ;BoardNumber" + Unit + ";EQ;SessionStart;20190926_130849;EQ;Run;Long", "AND", "DES", "TestEnd");
            //foreach (BsonDocument result in UnitResultsCur)
            //{
            //    //Put data in "data" object
            //    data.Graph3.Add(MapFields(result));
            //}

            //MDBStationResult = new MDBStationResults("TMP_DB", "TBT3_Legacy_Gen3_Rx");
            ////TODO query; make sure all have same Skew/Port/Lane (??)
            //UnitResultsCur = MDBStationResult.MDBFind("EQ;BoardNumber" + Unit + ";EQ;SessionStart;20190926_130849;EQ;Run;Long", "AND", "DES", "TestEnd");
            //foreach (BsonDocument result in UnitResultsCur)
            //{
            //    //Put data in "data" object
            //    data.Graph4.Add(MapFields(result));
            //}
            
            return data;
        }

        private BsonDocument MapFields(BsonDocument result)
        {
            BsonDocument tmpResult = result.DeepClone().AsBsonDocument;
            //Add mapped fields
            tmpResult.Set("SJ", result.GetValue("PJ_value"));
            //Test_Rx_Passive_MarginTest_MR_A0_TYP_233_BoardLong_233_NOMN_pa_L0_StressSignal0.246_PJ_100MHz_Run_Long.txt	
            string marginResult = Regex.Replace(result.GetValue("FILE").ToString(), ".*StressSignal", "");
            marginResult = Regex.Replace(marginResult, "_PJ_.*", "");
            tmpResult.Set("Margin Test Result", marginResult);
            return tmpResult;
        }

        public override void BuildExcelSheet(string filePath, TestModeMarginal_UnitData data)
        {


            FileInfo newFile = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[1]; //Dupliucate Sheet from first (?)
                worksheet = package.Workbook.Worksheets.Copy(worksheet.Name, data.unit);

                ////B6 - graph1placeholder ; V6graph2placeholder B(6+2)1st corner; B6 + 5 corners + 2 = graph3placeholder ; V6 + 5 corners + 2 = graph4placeholder
                BuildGraph(worksheet, data, "1", 8, 2, 6, "graph1placeholder");
                BuildGraph(worksheet, data, "2", 8, 22, 6, "graph2placeholder");
                BuildGraph(worksheet, data, "1", 45, 2, 43, "graph3placeholder");
                BuildGraph(worksheet, data, "2", 45, 22, 43, "graph4placeholder");


                foreach (ExcelChart chart in worksheet.Drawings.OfType<ExcelChart>())
                {
                    foreach (var seriesRef in chart.ChartXml.GetElementsByTagName("c:f").OfType<XmlElement>())
                    {
                        seriesRef.InnerText = seriesRef.InnerText.Replace("'CIO Tmpl'!", data.unit + "!");
                    }
                }
                package.Save();

            }

        }

    }
}