using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
//using Excel = Microsoft.Office.Interop.Excel;
using MongoDB.Bson;
using System.Data;
using System.IO;
using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Table.PivotTable;
using OfficeOpenXml.Drawing.Chart;
using CCD_EV_ManageDB.Helpers;
using CCD_EV_ManageDB.Repositories;
using CsvHelper;


namespace CCD_EV_ManageDB.Models
{
    public class ExcelHandler
    {
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }

        public List<BsonDocument> ExcelToBsList(string excelFileName)
        {
            List<string> colNames = new List<string>();
            List<BsonDocument> ResultsRecs = new List<BsonDocument>();

            int index = 0;
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                foreach (ExcelWorksheet displayWorksheet in package.Workbook.Worksheets)
                {
                    if (displayWorksheet.Name == "PANDA") continue;
                    colNames.Clear();
                    index = 1;
                    while (displayWorksheet.Cells[1, index].Value != null)
                    {
                        colNames.Add(Convert.ToString(displayWorksheet.Cells[1, index].Value));
                        index++;
                        //accumulate column names into array...
                    }

                    int rowCnt = 1;
                    while (rowCnt <= displayWorksheet.Dimension.End.Row)
                    {
                        BsonDocument result = new BsonDocument();
                        rowCnt++;
                        if (rowCnt == 1) continue;
                        if ((Convert.ToString(displayWorksheet.Cells[rowCnt, 1].Value) == null) || (Convert.ToString(displayWorksheet.Cells[rowCnt, 1].Value) == "")) continue;
                        for (int i = 1; i < index; i++)
                        {
                            string value = Convert.ToString(displayWorksheet.Cells[rowCnt, i].Value);
                            if (value != null)
                            {
                                BsonValue BVal = null;
                                double d;
                                if ((colNames.ElementAt(i - 1) != "UnitId") && (colNames.ElementAt(i - 1) != "BoardNumber") && (double.TryParse(value, out d))) BVal = d;
                                else BVal = value;
                                result.Add(colNames.ElementAt(i - 1), BVal);
                            }

                        }
                        ResultsRecs.Add(result);
                    }
                }
            }

            return (ResultsRecs);

        }

        public void writeSheet(string excelFileName, string sheetName, List<BsonDocument> records, List<string> colNames)
        {
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                if (worksheet.Name != "Sheet1") worksheet = package.Workbook.Worksheets.Add(sheetName);
                else worksheet.Name = sheetName;

                int colIndex = 1;
                foreach (string col in colNames)
                {
                    worksheet.Cells[1, colIndex].Value = col;
                    colIndex++;
                }

                int lineIndex = 1;
                foreach (BsonDocument rec in records)
                {
                    lineIndex++;
                    colIndex = 1;
                    foreach (string col in colNames)
                    {
                        worksheet.Cells[lineIndex, colIndex].Value = rec.GetValue(col, "");
                        //TODO: FILE ??
                        colIndex++;
                    }
                }
                worksheet.Cells.AutoFitColumns(0);  //Autofit columns for all cells

                package.Save();

            }
        }

        public void writeFieldsHeaderTmplSheet(string excelFileName, string sheetName, List<BsonDocument> records, int fieldsLine)
        {

            double d;
            List<string> sheetsNames = new List<string>();
            List<string> colNames = new List<string>();
            FileInfo newFile = new FileInfo(excelFileName);
            bool isReportMargin = false;
            if (excelFileName.Contains("MarginData")) isReportMargin = true;
            bool isHotCorner = false;
            if (excelFileName.Contains("HotCorner")) isHotCorner = true;
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                foreach (ExcelWorksheet tmpWorksheet in package.Workbook.Worksheets) sheetsNames.Add(tmpWorksheet.Name);
                if (sheetsNames.Contains(sheetName))
                {
                    worksheet = package.Workbook.Worksheets[sheetName];
                }
                else
                {
                    if (worksheet.Name != "Sheet1") worksheet = package.Workbook.Worksheets.Add(sheetName);
                    else worksheet.Name = sheetName;
                }

                int colIndex = 1;
                //foreach (string col in colNames)
                while ((worksheet.Cells[fieldsLine, colIndex].Value != null) && (!string.IsNullOrEmpty(worksheet.Cells[fieldsLine, colIndex].Value.ToString())))
                {
                    colNames.Add(worksheet.Cells[fieldsLine, colIndex].Value.ToString());
                    colIndex++;
                }

                int lineIndex = fieldsLine;
                foreach (BsonDocument rec in records)
                {
                    bool isPass = (rec.GetValue("TestStatus", "").ToString().ToUpper() == "PASS");
                    lineIndex++;
                    colIndex = 1;
                    foreach (string col in colNames)
                    {
                        if ((col != "UnitId") && (col != "BoardNumber") && (double.TryParse(rec.GetValue(col, "").ToString(), out d)))
                        {
                            worksheet.Cells[lineIndex, colIndex].Value = d;
                        }
                        else
                        {
                            worksheet.Cells[lineIndex, colIndex].Value = rec.GetValue(col, "");
                        }
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Transparent);
                        worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 11;
                        if ((!isPass) && (!isReportMargin) && (!isHotCorner)) worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                        if ((isReportMargin) && (col=="Margin") && (rec.GetValue("MarginFailed", "").ToString().ToUpper() == "TRUE")) worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Orange);
                        //TODO: FILE ??
                        colIndex++;
                    }
                }
                worksheet.Cells.AutoFitColumns(0);  //Autofit columns for all cells

                package.Save();

            }
        }

        //TODO: add needed parameters?
        public void refreshPivotSheet(string excelFileName, string sheetName, List<BsonDocument> records, int fieldsLine)
        {
            List<string> sheetsNames = new List<string>();
            List<string> colNames = new List<string>();
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                foreach (ExcelWorksheet tmpWorksheet in package.Workbook.Worksheets) sheetsNames.Add(tmpWorksheet.Name);
                if (sheetsNames.Contains(sheetName))
                {
                    worksheet = package.Workbook.Worksheets[sheetName];
                }

                var pivotTable = worksheet.PivotTables[0];
                //TODO:define datasource and refresh(?)  
                pivotTable.CacheDefinition.SourceRange = worksheet.Cells["A0:D29"];

                //Refresh using Interop...

                package.Save();

            }
        }

        public void writeIndicatorSheet(string excelFileName, string sheetName, IndicatorDisplayResult IndDispResults)
        {
            IndicatorResults IR = IndDispResults.Res;
            Indicator Ind = IndDispResults.Ind;
            FileInfo newFile = new FileInfo(excelFileName);
            long AllTestsNeed = 0L;
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                //if (worksheet.Name != "EV TEST") worksheet = package.Workbook.Worksheets.Add(sheetName);
                //else worksheet.Name = sheetName;

                int lineStartTableIndex = 1;
                bool StartTableIndexFound = false;
                while ((!StartTableIndexFound) && (lineStartTableIndex < worksheet.Dimension.End.Row))
                {
                    if ((worksheet.Cells[lineStartTableIndex, 1].Value != null) &&
                        ((worksheet.Cells[lineStartTableIndex, 1].Value.ToString() == "SingleIndicator") ||
                          (worksheet.Cells[lineStartTableIndex, 1].Value.ToString() == IR.table)))
                    {
                        StartTableIndexFound = true;
                        if (worksheet.Cells[lineStartTableIndex, 1].Value.ToString() == "SingleIndicator")
                            worksheet.Cells[lineStartTableIndex, 1].Value = IR.table;
                    }
                    else
                    {
                        lineStartTableIndex++;
                    }
                }
                if (lineStartTableIndex >= worksheet.Dimension.End.Row) return;
                //worksheet.Cells[1, 1].Value = "Total View Main";
                int colIndex = 2;
                //Initialize table with empty vals
                for (int ind = lineStartTableIndex; ind < lineStartTableIndex + 9; ind++)
                {
                    colIndex = 2;
                    while (colIndex <= worksheet.Dimension.End.Column)
                    {
                        worksheet.Cells[ind, colIndex].Value = "";
                        colIndex++;
                    }
                }

                colIndex = 2;
                foreach (IndWeekSum week in IR.weeks)
                {
                    worksheet.Cells[lineStartTableIndex, colIndex].Value = "ww " + week.Name;
                    colIndex++;
                }


                int lineIndex = lineStartTableIndex;
                lineIndex++;
                //worksheet.Cells[lineIndex, 1].Value = "No. of planned tests ";

                colIndex = 2;
                //Planned
                foreach (IndWeekSum week in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = week.plannedTestsAc;
                    AllTestsNeed = week.plannedTestsAc;
                    colIndex++;
                }
                lineIndex++;
                //worksheet.Cells[lineIndex, 1].Value = "No. of tests executed ";
                colIndex = 2;
                //executed
                foreach (IndWeekSum week in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = week.executedTestsAc;
                    colIndex++;
                }
                lineIndex++;
                //worksheet.Cells[lineIndex, 1].Value = "No. of passed tests ";
                colIndex = 2;
                //Passed
                foreach (IndWeekSum week in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = week.passedTestsAc;
                    colIndex++;
                }
                lineIndex++;
                //worksheet.Cells[lineIndex, 1].Value = "No. of failed tests ";
                colIndex = 2;
                //Failed
                foreach (IndWeekSum week in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = week.failedTestsAc;
                    colIndex++;
                }
                lineIndex++;
                //worksheet.Cells[lineIndex, 1].Value = "No. of blocked tests ";
                colIndex = 2;
                //blocked
                foreach (IndWeekSum week in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = week.blockedTestsAc;
                    colIndex++;
                }
                lineIndex++;
                //worksheet.Cells[lineIndex, 1].Value = "No. of pending tests";
                colIndex = 2;
                //Pending
                foreach (IndWeekSum week in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = week.pendingTestsAc;
                    colIndex++;
                }
                lineIndex++;
                //worksheet.Cells[lineIndex, 1].Value = "% Coverage ";
                colIndex = 2;
                //Coverage
                foreach (IndWeekSum week in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = week.CoverageAc;
                    colIndex++;
                }

                //TODO: "All test need to be done" from excel or calc? (last Planned coloumn...
                lineIndex++;
                //worksheet.Cells[lineIndex, 1].Value = "All test need to be done ";
                colIndex = 2;
                //All test need to be done
                worksheet.Cells[lineIndex, colIndex].Value = AllTestsNeed;

                worksheet.Cells.AutoFitColumns(0);  //Autofit columns for all cells

                package.Save();

            }
        }

        public void writeSingleIndicatorDetailsSheet(string excelFileName, string sheetName, IndicatorDisplayResult IndDispResults)
        {
            IndicatorResults IR = IndDispResults.Res;
            Indicator Ind = IndDispResults.Ind;
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(1);
                //if (worksheet.Name != "EV TEST") worksheet = package.Workbook.Worksheets.Add(sheetName);
                //else worksheet.Name = sheetName;

                int lineStartTableIndex = 1;
                bool StartTableIndexFound = false;
                while ((!StartTableIndexFound) && (lineStartTableIndex < worksheet.Dimension.End.Row))
                {
                    if ((worksheet.Cells[lineStartTableIndex, 1].Value != null) &&
                        ((worksheet.Cells[lineStartTableIndex, 1].Value.ToString() == "SingleIndicator") ||
                          (worksheet.Cells[lineStartTableIndex, 1].Value.ToString() == IR.table)))
                    {
                        StartTableIndexFound = true;
                        if (worksheet.Cells[lineStartTableIndex, 1].Value.ToString() == "SingleIndicator")
                            worksheet.Cells[lineStartTableIndex, 1].Value = IR.table;
                    }
                    else
                    {
                        lineStartTableIndex++;
                    }
                }
                if (lineStartTableIndex >= worksheet.Dimension.End.Row) return;

                //int colIndex = 2;
                ////Initialize table with empty vals
                //for (int ind = lineStartTableIndex; ind < lineStartTableIndex + 9; ind++)
                //{
                //    colIndex = 2;
                //    while (colIndex <= worksheet.Dimension.End.Column)
                //    {
                //        worksheet.Cells[ind, colIndex].Value = "";
                //        colIndex++;
                //    }
                //}

                int colIndex = 3;
                foreach (IndWeekSum week in IR.weeks)
                {
                    worksheet.Cells[lineStartTableIndex, colIndex].Value = "ww " + week.Name;
                    colIndex += 4;
                }


                int lineIndex = lineStartTableIndex;
                lineIndex++;

                foreach (IndTest test in Ind.tests)
                {
                    lineIndex++;
                    worksheet.Cells[lineIndex, 1].Value = test.SpecTestName;
                    worksheet.Cells[lineIndex, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, 1].Style.Font.Size = 8;
                    worksheet.Cells[lineIndex, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                    worksheet.Cells[lineIndex, 2].Value = test.UnitsNum;
                    worksheet.Cells[lineIndex, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, 2].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, 2].Style.Font.Size = 8;
                    worksheet.Cells[lineIndex, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    colIndex = 3;
                    foreach (var week in test.WorkWeeks)
                    {
                        worksheet.Cells[lineIndex, colIndex].Value = week.PlannedOrig;
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                        worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightPink);
                        colIndex++;
                        worksheet.Cells[lineIndex, colIndex].Value = week.Executed;
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                        worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSkyBlue);
                        colIndex++;
                        worksheet.Cells[lineIndex, colIndex].Value = week.Passed;
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                        worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSeaGreen);
                        if ((week.Executed - week.Passed) > 0L) worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                        colIndex++;
                        worksheet.Cells[lineIndex, colIndex].Value = week.Blocked.ToString().Substring(0, 1);
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                        worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                        if (week.Blocked) worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                        else worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                        colIndex++;
                    }
                }

                colIndex = 3;
                lineIndex++;
                worksheet.Cells[lineIndex, 1].Value = "Total: Planned/Exec/Pass/Blocked";
                worksheet.Cells[lineIndex, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[lineIndex, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                worksheet.Cells[lineIndex, 1].Style.Font.Size = 8;
                worksheet.Cells[lineIndex, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);

                foreach (var Sweek in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = Sweek.plannedTestsAc;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);
                    colIndex++;
                    worksheet.Cells[lineIndex, colIndex].Value = Sweek.executedTestsAc;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);
                    colIndex++;
                    worksheet.Cells[lineIndex, colIndex].Value = Sweek.passedTestsAc;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);
                    colIndex++;
                    //New Blocked
                    worksheet.Cells[lineIndex, colIndex].Value = Sweek.blockedTestsAc;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);
                    colIndex++;

                }

                colIndex = 3;
                lineIndex++;
                worksheet.Cells[lineIndex, 1].Value = "Coverage:";
                worksheet.Cells[lineIndex, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[lineIndex, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                worksheet.Cells[lineIndex, 1].Style.Font.Size = 8;
                worksheet.Cells[lineIndex, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSeaGreen);

                foreach (var Sweek in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = Sweek.CoverageAc.ToString("F3", CultureInfo.CurrentCulture);
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSeaGreen);
                    colIndex += 4;
                }

                colIndex = 3;
                lineIndex++;
                worksheet.Cells[lineIndex, 1].Value = "Pending:";
                worksheet.Cells[lineIndex, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[lineIndex, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                worksheet.Cells[lineIndex, 1].Style.Font.Size = 8;
                worksheet.Cells[lineIndex, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);

                foreach (var Sweek in IR.weeks)
                {
                    worksheet.Cells[lineIndex, colIndex].Value = Sweek.pendingTestsAc;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                    worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 8;
                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                    colIndex += 4;
                }


                worksheet.Cells.AutoFitColumns(0);  //Autofit columns for all cells

                package.Save();

            }
        }


        //go over the file and read instead of write, make sure to fill "Indicator" object or return it, as well as error message
        public Indicator readSingleIndicatorDetailsSheet(string excelFileName, Indicator _Ind, out string message)
        {
            message = "";
            Indicator Ind = new Indicator();
            Ind = _Ind;
            FileInfo newFile = new FileInfo(excelFileName);
            try
            {
                using (ExcelPackage package = new ExcelPackage(newFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(1);
                    //if (worksheet.Name != "EV TEST") worksheet = package.Workbook.Worksheets.Add(sheetName);
                    //else worksheet.Name = sheetName;

                    int lineStartTableIndex = 1;
                    bool StartTableIndexFound = false;
                    while ((!StartTableIndexFound) && (lineStartTableIndex < worksheet.Dimension.End.Row))
                    {
                        if ((worksheet.Cells[lineStartTableIndex, 1].Value != null) &&
                            ((worksheet.Cells[lineStartTableIndex, 1].Value.ToString() == "SingleIndicator") ||
                              (worksheet.Cells[lineStartTableIndex, 1].Value.ToString() == Ind.table)))
                        {
                            StartTableIndexFound = true;
                            if (worksheet.Cells[lineStartTableIndex, 1].Value.ToString() == "SingleIndicator")
                                worksheet.Cells[lineStartTableIndex, 1].Value = Ind.table;
                        }
                        else
                        {
                            lineStartTableIndex++;
                        }
                    }
                    if (lineStartTableIndex >= worksheet.Dimension.End.Row)
                    {
                        message = "Could not find tabel in cell 1:1";
                        return Ind;
                    }



                    int colIndex = 3;
                    foreach (WorkWeek week in Ind.tests[0].WorkWeeks)
                    {
                        if ((worksheet.Cells[lineStartTableIndex, colIndex].Value==null) || (worksheet.Cells[lineStartTableIndex, colIndex].Value.ToString() != "ww " + week.Name))
                        {
                            message = "Could not find ww" + week.Name + " in cell " + lineStartTableIndex.ToString() + ":" + colIndex.ToString();
                            return Ind;
                        }
                        colIndex += 4;
                    }


                    int lineIndex = lineStartTableIndex;
                    lineIndex++;

                    foreach (IndTest test in Ind.tests)
                    {
                        lineIndex++;
                        if (worksheet.Cells[lineIndex, 1].Value.ToString() != test.SpecTestName)
                        {
                            message = "Could not find SpecTestName:" + test.SpecTestName + " in cell " + lineIndex.ToString() + ":1";
                            return Ind;
                        }
                        //ne need to check units
                        int unitsNum;
                        if ((int.TryParse(worksheet.Cells[lineIndex, 2].Value.ToString(), out unitsNum))) test.UnitsNum = unitsNum;

                        colIndex = 3;
                        foreach (var week in test.WorkWeeks)
                        {
                            int val;
                            if ((worksheet.Cells[lineIndex, colIndex].Value==null) || (!int.TryParse(worksheet.Cells[lineIndex, colIndex].Value.ToString(), out val)))
                            {
                                message = "Could not read int planned value in cell " + lineIndex.ToString() + ":" + colIndex;
                                return Ind;
                            }
                            else week.PlannedOrig = val;

                            colIndex++;
                            //worksheet.Cells[lineIndex, colIndex].Value = week.Executed;
                            colIndex++;
                            //worksheet.Cells[lineIndex, colIndex].Value = week.Passed;
                            colIndex++;
                            //worksheet.Cells[lineIndex, colIndex].Value = week.Blocked.ToString().Substring(0, 1);
                            colIndex++;
                        }
                    }

                    package.Dispose();

                }
            }
            catch (Exception e)
            {
                message = e.Message;
                return Ind;
            }

            return Ind;

        }
        public void PrepareIndicatorsAllFile(string excelFileName, IndicatorDisplayResult IndDispResults)
        {
            IndicatorResults IR = IndDispResults.Res;
            Indicator Ind = IndDispResults.Ind;
            FileInfo newFile = new FileInfo(excelFileName);
            string WeekName = CCDUtilities.getCurWeek();
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                //if (worksheet.Name != "EV TEST") worksheet = package.Workbook.Worksheets.Add(sheetName);
                //else worksheet.Name = sheetName;

                int lineStartTableIndex = 1;
                bool StartTableIndexFound = false;
                while ((!StartTableIndexFound) && (lineStartTableIndex < worksheet.Dimension.End.Row))
                {
                    if ((worksheet.Cells[lineStartTableIndex, 1].Value != null) &&
                        ((worksheet.Cells[lineStartTableIndex, 1].Value.ToString().Trim() == "year:"))) StartTableIndexFound = true;
                    else
                    {
                        lineStartTableIndex++;
                    }

                }
                if (lineStartTableIndex >= worksheet.Dimension.End.Row) return;
                int colIndex = 2;

                while (colIndex <= worksheet.Dimension.End.Column)
                {
                    if (((worksheet.Cells[lineStartTableIndex, colIndex].Value != null)) &&
                        (worksheet.Cells[lineStartTableIndex, colIndex].Value.ToString().Trim() == "last updated WW:"))
                    {
                        worksheet.Cells[lineStartTableIndex, colIndex + 1].Value = WeekName;
                        break;
                    }
                    colIndex++;
                }

                package.Save();

            }
        }

        public void ClearFutureWWIndicatorsAllFile(string excelFileName, IndicatorDisplayResult IndDispResults)
        {
            //IndicatorResults IR = IndDispResults.Res;
            //Indicator Ind = IndDispResults.Ind;
            FileInfo newFile = new FileInfo(excelFileName);
            string WeekName = CCDUtilities.getCurWeek();
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                //if (worksheet.Name != "EV TEST") worksheet = package.Workbook.Worksheets.Add(sheetName);
                //else worksheet.Name = sheetName;

                int lineStartTableIndex = 1;
                bool StartTableIndexFound = false;
                while ((!StartTableIndexFound) && (lineStartTableIndex < worksheet.Dimension.End.Row))
                {
                    if ((worksheet.Cells[lineStartTableIndex, 1].Value != null) &&
                        ((worksheet.Cells[lineStartTableIndex, 1].Value.ToString().Trim() == "Total View Factorized"))) StartTableIndexFound = true;
                    else
                    {
                        lineStartTableIndex++;
                    }

                }
                if (lineStartTableIndex >= worksheet.Dimension.End.Row) return;
                int colIndex = 2;

                while (colIndex <= worksheet.Dimension.End.Column)
                {
                    if (((worksheet.Cells[lineStartTableIndex, colIndex].Value != null)) &&
                        (worksheet.Cells[lineStartTableIndex, colIndex].Value.ToString().Trim() == WeekName))
                    {
                        break;
                    }
                    colIndex++;
                }
                colIndex++;

                //clear future WW
                while (colIndex <= worksheet.Dimension.End.Column)
                {
                    if (worksheet.Cells[lineStartTableIndex + 2, colIndex].Value != null) worksheet.Cells[lineStartTableIndex + 2, colIndex].Value = null;
                    if (worksheet.Cells[lineStartTableIndex + 3, colIndex].Value != null) worksheet.Cells[lineStartTableIndex + 3, colIndex].Value = null;
                    if (worksheet.Cells[lineStartTableIndex + 4, colIndex].Value != null) worksheet.Cells[lineStartTableIndex + 4, colIndex].Value = null;
                    colIndex++;
                }

                package.Save();

            }
        }

        public void writeIndicatorChartSheet(string excelFileName, string sheetName, IndicatorDisplayResult IndDispResults)
        {
            IndicatorResults IR = IndDispResults.Res;
            Indicator Ind = IndDispResults.Ind;
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                foreach (ExcelWorksheet worksheet in package.Workbook.Worksheets)
                {
                    //ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);

                    //New: chart handling using EPPlus great API
                    foreach (var chart in worksheet.Drawings)
                    {
                        ExcelChart Chart = chart as ExcelChart;
                        worksheet.Drawings.Remove(Chart.Name);
                        foreach (ExcelChartSerie ser in Chart.Series)
                        {
                            ser.Series.Replace("$K", "$M");
                            //TODO: sereies.Series = ???
                            //write to log or TMPDATA and show in web page
                            // maybe create new series? and delete the old?
                            //try to create a new graph? copy exist one?
                        }

                    }

                    //ExcelChart Chart = new ExcelChart();
                }
                package.Save();

            }
        }


        public List<BsonDocument> parsePNA_CSV(string templeate, string csv_path, BsonDocument recIn, bool createSpec)
        {
            List<BsonDocument> results = new List<BsonDocument>();
            List<BsonDocument> resultsSpec = new List<BsonDocument>();

            List<string> colnames = new List<string>();
            int rowNum = 0;
            bool FieldsNames = false;
            int rowNumSummaryTitle = -10;

            System.IO.TextReader readFile = new StreamReader(csv_path);
            var parser = new CsvParser(readFile);
            while (true)
            {
                var values = parser.Read();
                if (values == null)
                {
                    break;
                }
                rowNum++;

                if (values[0].StartsWith("!")) continue;
                if (string.IsNullOrWhiteSpace(values[0].ToString())) continue;
                if (values[0].StartsWith("BEGIN"))
                {
                    FieldsNames = true; //next line is fields names
                    continue;
                }
                if (values[0].StartsWith("END")) break; //next line is fields names

                if (FieldsNames)
                {
                    rowNumSummaryTitle = rowNum;
                    colnames = new List<string>(values);
                    FieldsNames = false;
                    continue;
                }

                //handling a raw
                if ((colnames.Count > 0) && (rowNum > rowNumSummaryTitle) && (colnames.Count == values.Length))
                {
                    BsonDocument resultDoc = recIn.DeepClone().AsBsonDocument;
                    int colCounter = -1;
                    foreach (string col in colnames)
                    {
                        colCounter++;
                        if (string.IsNullOrWhiteSpace(col)) continue;
                        BsonValue BVal = null;
                        BVal = values[colCounter];
                        //TODO: real edit col names, or aliases. (this is quick & dirty replacing)
                        resultDoc.Add(col.Replace("Log Mag", ""), BVal);
                        if (col == "Freq(Hz)")
                        {
                            Double tmpGHz = Convert.ToDouble(resultDoc.GetValue("Freq(Hz)", "0").ToString());
                            resultDoc.Add("Freq (GHz)", String.Format("{0:0.00}", (tmpGHz / 1000000000)));
                        }
                    }
                    results.Add(resultDoc);
                    //DP duplicate result ; sdd22=>sdd11; lane# +1
                    if (templeate.StartsWith("DP"))
                    {
                        BsonDocument resultDocDPAdd = resultDoc.DeepClone().AsBsonDocument;
                        resultDocDPAdd.Set("Sdd11 (dB)", resultDoc.GetValue("Sdd22 (dB)", ""));
                        int tmpLane = Convert.ToInt32(resultDocDPAdd.GetValue("Lane", "00").ToString().Substring(1, 1));
                        tmpLane += 1;
                        resultDocDPAdd.Set("Lane", "L" + tmpLane.ToString());
                        results.Add(resultDocDPAdd);
                    }

                }
            }
            //determane PCIe rev for spec
            string PcieSpecRev = "PCIe_Base_3";
            string Project = recIn.GetValue("ProjectName", "").ToString();
            //PCIe_Base_2: FXVL
            //PCIe_Base_3: AR,TR,GR,MR
            //PCIe_Base_4: MSR1,MSR3,HADAS
            switch (Project)
            {
                case "FXVL":
                    PcieSpecRev = "PCIe_Base_2";
                    break;
                case "AR":
                case "TR":
                case "GR":
                case "MR":
                    PcieSpecRev = "PCIe_Base_3";
                    break;
                case "MSR1":
                case "MSR3":
                case "HADAS":
                    PcieSpecRev = "PCIe_Base_4";
                    break;
                default:
                    PcieSpecRev = "PCIe_Base_3";
                    break;
            }
            if (createSpec)
            {
                //duplicate results for spec
                foreach (BsonDocument result in results)
                {
                    BsonDocument resultDoc = result.DeepClone().AsBsonDocument;
                    Double tmpGHz = Convert.ToDouble(resultDoc.GetValue("Freq (GHz)", "0").ToString());
                    resultDoc.Set("Lane", "Spec");
                    //TODO add formula...
                    if (templeate.StartsWith("CIO"))
                    {
                        Double sdd = -8.5;
                        if ((tmpGHz > 3) && (tmpGHz <= 12))
                        {
                            sdd = -3.5 + 8.3 * Math.Log10(tmpGHz / 12);
                        }
                        resultDoc.Set("Sdd11 (dB)", sdd);
                        resultDoc.Set("Sdd22 (dB)", sdd);

                        Double scc = -6;
                        if ((tmpGHz > 2.5) && (tmpGHz <= 12))
                        {
                            scc = -3;
                        }
                        resultDoc.Set("Scc11 (dB)", scc);
                        resultDoc.Set("Scc22 (dB)", scc);

                    }

                    if (templeate.StartsWith("DP"))
                    {
                        Double sdd = -8.5;
                        if ((tmpGHz > 3) && (tmpGHz <= 12))
                        {
                            sdd = -3.5 + 8.3 * Math.Log10(tmpGHz / 12);
                        }
                        resultDoc.Set("Sdd11 (dB)", sdd);
                        resultDoc.Set("Sdd22 (dB)", sdd);
                    }

                    if (templeate.StartsWith("PCIe"))
                    {

                        Double sdd11 = -10;
                        Double sdd22 = -10;
                        Double scc11 = -6;
                        Double scc22 = -6;

                        if ((tmpGHz > 1.25) && (tmpGHz <= 2.5))
                        {
                            sdd11 = -8;
                            sdd22 = -8;
                        }
                        if (tmpGHz > 2.5)
                        {
                            if (PcieSpecRev == "PCIe_Base_4")
                            {
                                sdd11 = -6;
                                sdd22 = -6;
                            }
                            else
                            {
                                sdd11 = -4;
                                sdd22 = -5;
                            }

                            scc11 = -3;
                            scc22 = -3;
                        }
                        resultDoc.Set("Sdd11 (dB)", sdd11);
                        resultDoc.Set("Sdd22 (dB)", sdd22);
                        resultDoc.Set("Scc11 (dB)", scc11);
                        resultDoc.Set("Scc22 (dB)", scc22);
                    }

                    resultsSpec.Add(resultDoc);
                }
            }
            results.AddRange(resultsSpec);
            return results;
        }

        public Dictionary<string, List<BsonDocument>> DV_Analyse_accumulate(Dictionary<string, List<BsonDocument>> _dictionary, BsonDocument item)
        {
            Dictionary<string, List<BsonDocument>> dictionary = _dictionary;
            string message;
            DV_AnalysisHandler DV = new DV_AnalysisHandler(item.GetValue("Corner", "").ToString(), item.GetValue("Port", "").ToString(), item.GetValue("Lane", "").ToString());
            if (!DV.Analyse(Path.Combine(ServerDir, "EVData", "TxEq_Limits.xlsx"), item.GetValue("FILE_DV_TxFFE", "").ToString(), out message)) return dictionary;

            FileInfo newFile = new FileInfo(item.GetValue("FILE_DV_TxFFE", "").ToString());
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                foreach (ExcelWorksheet worksheet in package.Workbook.Worksheets)
                {
                    if (!worksheet.Name.StartsWith("Preset")) continue;

                    int lineIndex = 2;
                    while ((worksheet.Cells[lineIndex, 1].Value != null) && (!string.IsNullOrEmpty(worksheet.Cells[lineIndex, 1].Value.ToString())))
                    {
                        BsonDocument result = new BsonDocument();
                        result.Set("Post", worksheet.Cells[lineIndex, 1].Value.ToString());
                        result.Set("Pre", worksheet.Cells[lineIndex, 2].Value.ToString());
                        result.Set("Preshoot", worksheet.Cells[lineIndex, 3].Value.ToString());
                        result.Set("Deemphasis", worksheet.Cells[lineIndex, 4].Value.ToString());
                        result.Set("Preset", worksheet.Cells[lineIndex, 5].Value.ToString());
                        result.Set("Corner", worksheet.Cells[lineIndex, 6].Value.ToString());
                        result.Set("Lane", worksheet.Cells[lineIndex, 7].Value.ToString());
                        result.Set("Port", worksheet.Cells[lineIndex, 8].Value.ToString());
                        result.Set("Place", worksheet.Cells[lineIndex, 9].Value.ToString());
                        result.Set("Key", worksheet.Cells[lineIndex, 10].Value.ToString());
                        if (!dictionary.ContainsKey(worksheet.Name))
                        {
                            dictionary.Add(worksheet.Name, new List<BsonDocument>());
                        }
                        dictionary[worksheet.Name].Add(result);
                        lineIndex++;
                    }
                }
                return dictionary;
            }
        }

        public bool convertXlsCSVToXlsx(string resultFileName)
        {
            try
            {
                string csvFileName = resultFileName;//"FL_insurance_sample.csv";
                string excelFileName = resultFileName + "x"; //"FL_insurance_sample.xls";

                string worksheetsName = Path.GetFileNameWithoutExtension(resultFileName);

                bool firstRowIsHeader = false;

                var format = new ExcelTextFormat();
                format.Delimiter = '\t';
                //format.EOL = "\r";              // DEFAULT IS "\r\n";


                using (ExcelPackage package = new ExcelPackage(new FileInfo(excelFileName)))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(worksheetsName);
                    worksheet.Cells["A1"].LoadFromText(new FileInfo(csvFileName), format, OfficeOpenXml.Table.TableStyles.None, firstRowIsHeader);
                    package.Save();
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public void writeSheetLoadCellQueries(string excelFileName, string sheetName, string baseQuery, string table, string _DB)
        {
            MDBStationResults mdbResults = new MDBStationResults(_DB, table);
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                if (!String.IsNullOrEmpty(sheetName)) worksheet = package.Workbook.Worksheets[sheetName];
                var query3 = (from cell in worksheet.Cells["0:0"]
                              where cell.Value != null && cell.Value.ToString().StartsWith("cell_query")
                              select cell);
                foreach (var cell in query3)   
                {
                    cell.Value = mdbResults.FindExcelCellQuery(baseQuery,cell.Value.ToString().Replace("cell_query=",""));
                    if (string.IsNullOrEmpty(cell.Value.ToString())) cell.Value = "NA";
                    else
                    {
                        Double tmpDpuble;
                        if (Double.TryParse(cell.Value.ToString(), out tmpDpuble)) cell.Value=tmpDpuble;
                    }
                }
                //worksheet.Cells.AutoFitColumns(0);  //autofit columns for all cells
                package.Save();
            }
        }

        //hard coded pre process function to replace "ULT1/ULT2..." with actual ULT values from DB
        public void writeSheetLoadCellQueriesPreProcess(string excelFileName, string sheetName, string baseQuery, string table, string _DB, string template)
        {
            Dictionary<string, string> PreProcessValues = new Dictionary<string, string>();
            //TODO-  a better way to call specific PreProcess for each template
            if (template.StartsWith("Power_SVR_code2values_10_ALL_units")) PreProcessValues = Power_SVR_code2values_10_ALL_units_PreProcess(baseQuery, table, _DB);
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                if (!String.IsNullOrEmpty(sheetName)) worksheet = package.Workbook.Worksheets[sheetName];
                foreach (var _value in PreProcessValues)
                {
                    var query3 = (from cell in worksheet.Cells["0:0"]
                                  where cell.Value != null && cell.Value.ToString().Contains(_value.Key)
                                  select cell);
                    foreach (var cell in query3)
                    {
                        cell.Value = cell.Value.ToString().Replace(_value.Key,_value.Value);
                    }
                }
                package.Save();
            }
        }

        Dictionary<string,string> Power_SVR_code2values_10_ALL_units_PreProcess(string baseQuery, string table, string _DB)
        {
            Dictionary<string, string> PreProcessValues = new Dictionary<string, string>();
            MDBStationResults mdbResults = new MDBStationResults(_DB, table);
            int ULTcnt = 1;
            foreach (BsonValue ult in mdbResults.MDBFindDistinct(baseQuery,"AND","ASC","_id","ULT"))
            {
                PreProcessValues.Add("ULT" + ULTcnt.ToString("X").ToUpper(), ult.ToString());
                ULTcnt++;
            }
            return (PreProcessValues);
        }

        public List<string> getSheetNames(string excelFileName)
        {
            FileInfo newFile = new FileInfo(excelFileName);
            List<string> SheetNames = new List<string>();
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                foreach (ExcelWorksheet worksheet in package.Workbook.Worksheets) SheetNames.Add(worksheet.Name);
            }
            return SheetNames;
        }

        public void replaceCellConstantInSheet(string excelFileName, string sheetName, string cellExistVal, string cellNewVal)
        {
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                if (!String.IsNullOrEmpty(sheetName)) worksheet = package.Workbook.Worksheets[sheetName];
                var query3 = (from cell in worksheet.Cells["0:0"]
                              where cell.Value != null && cell.Value.ToString().StartsWith(cellExistVal)
                              select cell);
                foreach (var cell in query3)
                {
                    cell.Value = cellNewVal;
                }
                //worksheet.Cells.AutoFitColumns(0);  //autofit columns for all cells
                package.Save();
            }
        }

    }
}
