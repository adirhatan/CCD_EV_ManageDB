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

namespace CCD_EV_ManageDB.Models
{
    public class StatusReportHandler
    {
        public bool StatusReportExec(string _id,string filepath, MongoCursor ResCur,string baseQuery, string Lanes, List<string> SeperatorFields,bool _JoinFieldKey, bool mesureSummary, out string errorMsg)
        {
            errorMsg = "";
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(_id);
            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
            
            //TODO: Take from report sheets separators
            //string SeperatorField = "UnitId";
            Dictionary<string, List<BsonDocument>> dictionary = new Dictionary<string, List<BsonDocument>>();

            try
            {
                foreach (BsonDocument item in ResCur)
                {
                    //string TestName = item.GetValue(SeperatorField, "noSeperator").ToString();
                    string SheetName = GetResultSheetName(item, SeperatorFields, Report.TestName);
                    if (!dictionary.ContainsKey(SheetName))
                    {
                        dictionary.Add(SheetName, new List<BsonDocument>());
                    }
                    dictionary[SheetName].Add(item);
                    //fields
                    foreach (BsonElement field in item)
                    {
                        if ((field.Name.Length > 6) && (field.Name.Substring(0, 6) == "HIDDEN")) continue;
                    }
                }
            }
            catch
            {
                errorMsg = "ERROR: cannot export query:";
                return false;
            }

            if (dictionary.Count > 100)
            {
                errorMsg = "ERROR: cannot exceed 100 sheets limit, choose another separators then:\"" + SeperatorFields.ToString() + "\" or refine query:";
                return false;
            }
            foreach (var pair in dictionary) writeSheet(filepath, pair.Key, Lanes, _JoinFieldKey, mesureSummary, pair.Value, Report, SeperatorFields, ResCur.Database.Name, baseQuery);
                       
            return true;
        }

        public void writeSheet(string excelFileName, string sheetName, string Lanes, bool _JoinFieldKey, bool mesureSummary, List<BsonDocument> records, UserTableReport Report, List<string> SeperatorFields, string _db, string baseQuery)
        {

            //TODO: take report format from Report and use Factory?
            StatusReport SR = new StatusReport(Report._id, Lanes, _JoinFieldKey, mesureSummary);
            SR.AnalyzeResults(records);
            Dictionary<string, Dictionary<string, string>> statusResults = SR.GetStatusResults();
            Dictionary<string, Dictionary<string, string>> advResultsDetails = SR.GetAdvResultsDetails();
            FileInfo newFile = new FileInfo(excelFileName);
            List<string> CustomExecLines = new List<string>();
            List<string> CustomExecLinesF = new List<string>();
            string CustomExecFile = Path.Combine(Path.GetDirectoryName(excelFileName),Path.GetFileNameWithoutExtension(excelFileName)+ ".txt");
            string CustomExecFileF = Path.Combine(Path.GetDirectoryName(excelFileName),Path.GetFileNameWithoutExtension(excelFileName) + "_Failed.txt");

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                if (sheetName.Length>32) sheetName=sheetName.Substring(0,32);
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Copy("Sheet1", sheetName);

                worksheet.Cells[1, 1].Value = Report.RConfig.table;
                worksheet.Cells[2, 2].Value = GetResultSheetHeader(records.ElementAt(0));


                int colIndex = 1;
                int lineIndex = 4;
                foreach (var pair in statusResults)
                {
                    lineIndex++;
                    colIndex = 1;

                    worksheet.Cells[lineIndex, colIndex].Value = pair.Key;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Transparent);
                    worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                    colIndex++;
                    foreach (var col in pair.Value)
                    {
                        worksheet.Cells[lineIndex, colIndex].Hyperlink = new Uri(SR.getLinkToResults(pair.Key, col.Key, records[0], Report.RConfig.table,SeperatorFields,_db, baseQuery));
                        //worksheet.Cells[lineIndex, colIndex].Formula = "HYPERLINK(\"" + SR.getLinkToResults(pair.Key, col.Key, records[0], Report.RConfig.table,SeperatorFields,_db) + "\",\"" + col.Value + "\")";
                        worksheet.Cells[lineIndex, colIndex].Value = col.Value;
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                        worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                        switch (col.Value)
                        { 
                            case "NA":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                                CustomExecLines.Add(GetCustomExecLine(col.Key, SR.GetTestStandTestName(pair.Key), "STATUS-NA", pair.Key.Replace(' ','@')));// Some SpecTestName contains spaces...
                                break;
                            case "PASS":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Green);
                                break;
                            case "FAIL":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                CustomExecLines.Add(GetCustomExecLine(col.Key, SR.GetTestStandTestName(pair.Key), "STATUS-FAIL", pair.Key.Replace(' ', '@')));
                                break;
                            case "MULTI":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                                break;
                            case "MARGINAL":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Orange);
                                break;
                            default:
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);
                                break;

                        }
                        if (SR.IsCornerFail(col.Key))
                        {
                            switch (col.Value)
                            {
                                case "NA":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.LightGray);
                                    break;
                                case "PASS":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.Green);
                                    break;
                                case "FAIL":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.Red);
                                    break;
                                case "MULTI":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.LightBlue);
                                    break;
                                case "MARGINAL":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.Orange);
                                    break;
                                default:
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Lavender);
                                    break;

                            }
                            if (SR.IsCornerFailMeasure(col.Key,pair.Key)) worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                        }  
                        
                        //New: Add detailes column
                        //check logic of the list: 
                        if (SR.detailed)
                        {
                            colIndex++;
                            string completenessVal = "NA";
                            string permsLink = "";
                            foreach (var perm in advResultsDetails[pair.Key])
                            {
                                if (!perm.Key.StartsWith(col.Key)) continue;
                                permsLink += perm.Key + ";" + perm.Value + ";";
                                if ((perm.Value == "PASS") && (completenessVal == "NA")) completenessVal = "Full";
                                if ((perm.Value == "NA") && (completenessVal == "Full")) completenessVal = "Partial";
                            }
                            worksheet.Cells[lineIndex, colIndex].Hyperlink = new Uri(SR.getLinkToMissingDetailedResults(pair.Key, col.Key, records[0], Report.RConfig.table, SeperatorFields, _db,permsLink));
                            worksheet.Cells[lineIndex, colIndex].Value = completenessVal;
                            worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                            worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                            switch (completenessVal)
                            {
                                case "NA":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                                    break;
                                case "Full":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Green);
                                    break;
                                case "Partial":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                    break;
                                default:
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);
                                    break;
                            }
                            if (advResultsDetails[pair.Key].Values.Count == 0)
                            {
                                worksheet.Cells[lineIndex, colIndex].Value = "OK";
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                            }
                        }
                        //TODO: FILE ??
                        colIndex++;
                    }
                }


                worksheet.Cells.AutoFitColumns(0);  //Autofit columns for all cells

                package.Save();

                //File.AppendAllLines(CustomExecFile, CustomExecLines);
                Dictionary<string, List<string>> DPCustomExecLines = new Dictionary<string, List<string>>();
                foreach (string line in CustomExecLines)
                {
                    List<string> tmpList = new List<string>(line.Split(' '));
                    if (!DPCustomExecLines.ContainsKey(tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4]))
                    {
                        DPCustomExecLines.Add(tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4], new List<string>());
                        DPCustomExecLines[tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4]].Add(tmpList[5].Replace('@',' '));
                    }
                    else
                    {
                        if (!DPCustomExecLines[tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4]].Contains(tmpList[5].Replace('@', ' ')))
                            DPCustomExecLines[tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4]].Add(tmpList[5].Replace('@', ' '));
                    }

                }
                CustomExecLinesF.Clear();
                foreach (var pair in DPCustomExecLines)
                {
                    string spec = "";
                    foreach (string s in pair.Value) spec += s + ",";
                    spec = spec.TrimEnd(',');
                    CustomExecLinesF.Add(pair.Key.Replace(',',' ') + " " + spec);
                }

                File.AppendAllLines(CustomExecFile, CustomExecLinesF);
                //File.AppendAllLines(CustomExecFileF, CustomExecLinesF);

            }
        }


        //public void writeSheetCSR(string excelFileName, string sheetName, string Lanes, bool mesureSummary, List<BsonDocument> records, UserTableReport Report, List<string> SeperatorFields, string _db, string baseQuery, Dictionary<string, List<string>> SpecTestNamesWithFailesTrials, Dictionary<string, List<string>> SpecTestNamesWithMissingTrials)
        public StatusReport writeSheetCSR(string excelFileName, string sheetName, string Lanes, bool mesureSummary, List<BsonDocument> records, UserTableReport Report, List<string> SeperatorFields, string _db, string baseQuery, Dictionary<string, Dictionary<string, string>> SpecTestNamesWithFailesTrials, Dictionary<string, Dictionary<string, string>> SpecTestNamesWithMissingTrials)
        {

            StatusReport SR = new StatusReport(Report._id, Lanes, false, mesureSummary);
            SR.AnalyzeResults(records);
            Dictionary<string, Dictionary<string, string>> statusResults = SR.GetStatusResults();
            Dictionary<string, Dictionary<string, string>> advResultsDetails = SR.GetAdvResultsDetails();
            FileInfo newFile = new FileInfo(excelFileName);
            List<string> CustomExecLines = new List<string>();
            List<string> CustomExecLinesF = new List<string>();
            string CustomExecFile = Path.Combine(Path.GetDirectoryName(excelFileName), Path.GetFileNameWithoutExtension(excelFileName) + ".txt");
            string CustomExecFileF = Path.Combine(Path.GetDirectoryName(excelFileName), Path.GetFileNameWithoutExtension(excelFileName) + "_Failed.txt");

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                if (sheetName.Length > 32) sheetName = sheetName.Substring(0, 32);
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Copy("Sheet1", sheetName);

                worksheet.Cells[1, 1].Value = Report.RConfig.table;
                worksheet.Cells[2, 2].Value = GetResultSheetHeader(records.ElementAt(0));


                int colIndex = 1;
                int lineIndex = 4;
                foreach (var pair in statusResults)
                {
                    lineIndex++;
                    colIndex = 1;

                    worksheet.Cells[lineIndex, colIndex].Value = pair.Key;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Transparent);
                    worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                    colIndex++;
                    foreach (var col in pair.Value)
                    {
                        string trialsEnhancedVal = CheckMissingOrFailedTrials(col.Value, pair.Key, col.Key, SpecTestNamesWithFailesTrials, SpecTestNamesWithMissingTrials);

                        worksheet.Cells[lineIndex, colIndex].Hyperlink = new Uri(SR.getLinkToResults(pair.Key, col.Key, records[0], Report.RConfig.table, SeperatorFields, _db, baseQuery));
                        //worksheet.Cells[lineIndex, colIndex].Value = col.Value;
                        worksheet.Cells[lineIndex, colIndex].Value = trialsEnhancedVal;
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                        worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                        //switch (col.Value)
                        string pref_trialsEnhancedVal = trialsEnhancedVal.Split(' ')[0];
                        switch (pref_trialsEnhancedVal)
                        {
                            case "NA":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                                CustomExecLines.Add(GetCustomExecLine(col.Key, SR.GetTestStandTestName(pair.Key), "STATUS-NA", pair.Key.Replace(' ', '@')));// Some SpecTestName contains spaces...
                                break;
                            case "PASS":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Green);
                                break;
                            case "FAIL":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                CustomExecLines.Add(GetCustomExecLine(col.Key, SR.GetTestStandTestName(pair.Key), "STATUS-FAIL", pair.Key.Replace(' ', '@')));
                                break;
                            case "MULTI":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                                break;
                            case "MARGINAL":
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Orange);
                                break;
                            default:
                                worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);
                                break;

                        }
                        if (SR.IsCornerFail(col.Key))
                        {
                            switch (col.Value)
                            {
                                case "NA":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.LightGray);
                                    break;
                                case "PASS":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.Green);
                                    break;
                                case "FAIL":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.Red);
                                    break;
                                case "MULTI":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.LightBlue);
                                    break;
                                case "MARGINAL":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.Orange);
                                    break;
                                default:
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Lavender);
                                    break;

                            }
                            if (SR.IsCornerFailMeasure(col.Key, pair.Key)) worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                        }

                        colIndex++;
                    }
                }


                worksheet.Cells.AutoFitColumns(0);  //Autofit columns for all cells

                package.Save();

                Dictionary<string, List<string>> DPCustomExecLines = new Dictionary<string, List<string>>();
                foreach (string line in CustomExecLines)
                {
                    List<string> tmpList = new List<string>(line.Split(' '));
                    if (!DPCustomExecLines.ContainsKey(tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4]))
                    {
                        DPCustomExecLines.Add(tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4], new List<string>());
                        DPCustomExecLines[tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4]].Add(tmpList[5].Replace('@', ' '));
                    }
                    else
                    {
                        if (!DPCustomExecLines[tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4]].Contains(tmpList[5].Replace('@', ' ')))
                            DPCustomExecLines[tmpList[0] + "," + tmpList[1] + "," + tmpList[2] + "," + tmpList[3] + "," + tmpList[4]].Add(tmpList[5].Replace('@', ' '));
                    }

                }
                CustomExecLinesF.Clear();
                foreach (var pair in DPCustomExecLines)
                {
                    string spec = "";
                    foreach (string s in pair.Value) spec += s + ",";
                    spec = spec.TrimEnd(',');
                    CustomExecLinesF.Add(pair.Key.Replace(',', ' ') + " " + spec);
                }

                File.AppendAllLines(CustomExecFile, CustomExecLinesF);

            }

            return SR;
        }

        //private string CheckMissingOrFailedTrials(string val, string specKey, string cornePortLaneKey, Dictionary<string, List<string>> SpecTestNamesWithFailesTrials, Dictionary<string, List<string>>  SpecTestNamesWithMissingTrials)
        private string CheckMissingOrFailedTrials(string val, string specKey, string cornePortLaneKey, Dictionary<string, Dictionary<string,string>> SpecTestNamesWithFailesTrials, Dictionary<string, Dictionary<string, string>> SpecTestNamesWithMissingTrials)
        {
            //if ((SpecTestNamesWithFailesTrials.ContainsKey(specKey)) && SpecTestNamesWithFailesTrials[specKey].Contains(cornePortLaneKey)) return "FAIL";
            if ((SpecTestNamesWithFailesTrials.ContainsKey(specKey)) && SpecTestNamesWithFailesTrials[specKey].ContainsKey(cornePortLaneKey)) return "FAIL" + " " + SpecTestNamesWithFailesTrials[specKey][cornePortLaneKey];
            //if ((SpecTestNamesWithMissingTrials.ContainsKey(specKey)) && SpecTestNamesWithMissingTrials[specKey].Contains(cornePortLaneKey)) return "NA";
            if ((SpecTestNamesWithMissingTrials.ContainsKey(specKey)) && SpecTestNamesWithMissingTrials[specKey].ContainsKey(cornePortLaneKey)) return "NA" + " " + SpecTestNamesWithMissingTrials[specKey][cornePortLaneKey];
            return val;
        }

        private string GetCustomExecLine(string colKey, string TestStandTestName, string status, string SpecTestName)
        {
            //HVHT-PA-L0=>  HVHT L0 pa Test_1
            List<string> cornerDetails = new List<string>(colKey.Split('-'));
            string CustomExec = "";
            if (cornerDetails.Count() > 2)
            {
                CustomExec = cornerDetails[0] + " " + cornerDetails[2] + " " + cornerDetails[1] + " " + TestStandTestName + " " + status + " " + SpecTestName;
            }
            else
            {
                if (cornerDetails[1].StartsWith("L")) CustomExec = cornerDetails[0] + " NA " + cornerDetails[1] + " " + TestStandTestName + " " + status + " " + SpecTestName;
                if (cornerDetails[1].StartsWith("P")) CustomExec = cornerDetails[0] + " " + cornerDetails[1] + " NA" + TestStandTestName + " " + status + " " + SpecTestName;
            }
            return CustomExec;
        }

        private string GetResultSheetName(BsonDocument item, List<string> SeperatorFields,string RTestName)
        {
            string ResultSheetName = "";
            foreach (string SeperatorField in SeperatorFields)
            {
                string val="";
                if (SeperatorField == "ReportTestName") val = item.GetValue(RTestName, "noSeperator").ToString();
                else val = item.GetValue(SeperatorField, "noSeperator").ToString();
                if (ResultSheetName == "") ResultSheetName = val;
                else ResultSheetName += "_" + val;
            }
            return ResultSheetName;
        }

        private string GetResultSheetHeader(BsonDocument item )
        {
            List<string> HeaderFields = new List<string>(new string[] { "ProjectName", "Phase", "Skew", "BoardNumber", "WORD_Unit", "UnitId", "WORD_UserComment", "UserComment" });
            string ResultSheetHeader = "";
            foreach (string Field in HeaderFields)
            {
                string val ="";
                if (Field.StartsWith("WORD_")) val = Field.Substring(5, Field.Length - 5);
                else val = item.GetValue(Field, Field+"_missing").ToString();
                if (ResultSheetHeader == "") ResultSheetHeader = val;
                else ResultSheetHeader += "_" + val;
            }
            return ResultSheetHeader;
        }

    }
}