using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;

namespace CCD_EV_ManageDB.Models
{
    public class ExcelChartsHandler
    {
        private void releaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception ex)
            {
                obj = null;
                //MessageBox.Show("Exception Occured while releasing object " + ex.ToString());
            }
            //finally
            //{
            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //}
        }


        public void writeIndicatorSheet(string excelFileName, string sheetName, IndicatorDisplayResult IndDispResults)
        {
            const string graphTitle = "Graph Title";
            const string xAxis = "Time";
            const string yAxis = "Value";

            // Open Excel and get first worksheet.

            var excelApplication = new Excel.Application();
            var workbooks = excelApplication.Workbooks;
            var workbook = workbooks.Open(excelFileName); // Fixed

            Excel.Range GraphRange = null;

            foreach (Excel.Worksheet worksheet in workbook.Worksheets)
            {
                
                if (worksheet.Name == "EV TEST")
                {
                    GraphRange = worksheet.get_Range("A100", "L108");
                    //GraphRange.Value = "Fixed";
                }
                if (worksheet.Name == sheetName)
                {
                    // Add chart.
                    //var chart = worksheet.ChartObjects();//as Microsoft.Office.Interop.Excel.ChartObject;
                    Excel.ChartObject chart = (Excel.ChartObject)worksheet.ChartObjects(1);
                    //chart.Activate(); //???

                    //var chartObject = charts.Add(60, 10, 300, 300) as
                    //    Microsoft.Office.Interop.Excel.ChartObject;
                    //var chart = chartObject.Chart;
                    //var chart = charts.Item(0); //as Microsoft.Office.Interop.Excel.Chart;

                    // Set chart range.
                    //var range = worksheet.get_Range("A100", "K108");
                    chart.Chart.SetSourceData(GraphRange);
                    //Export Image example:
                    //chart.Chart.Export(Path.GetDirectoryName(excelFileName) + @"\" + chart.Name + ".png", "PNG", false);

                    //chart.SetSourceData(range);

                    //// Set chart properties.
                    //chart.ChartType = Microsoft.Office.Interop.Excel.XlChartType.xl3DColumnClustered;
                    //chart.ChartWizard(Source: range,
                    //    Title: graphTitle,
                    //    CategoryTitle: xAxis,
                    //    ValueTitle: yAxis);

                    // Save.
                    break;
                }
            }
                



            workbook.Save();

            workbook.Close();
            workbooks.Close();
            excelApplication.Quit();

            releaseObject(workbook);
            releaseObject(workbooks);
            releaseObject(excelApplication);

        }

        //public void writeIndicatorSheet(string excelFileName, string sheetName, IndicatorDisplayResult IndDispResults)
        //{
        //    const string graphTitle = "Graph Title";
        //    const string xAxis = "Time";
        //    const string yAxis = "Value";

        //    // Open Excel and get first worksheet.
        //    var application = new Excel.Application();
        //    var workbook = application.Workbooks.Open(excelFileName);

        //    var worksheet = workbook.Worksheets[1] as
        //        Microsoft.Office.Interop.Excel.Worksheet;

        //    // Add chart.
        //    var charts = worksheet.ChartObjects() as
        //        Microsoft.Office.Interop.Excel.ChartObjects;
        //    var chartObject = charts.Add(60, 10, 300, 300) as
        //        Microsoft.Office.Interop.Excel.ChartObject;
        //    var chart = chartObject.Chart;

        //    // Set chart range.
        //    var range = worksheet.get_Range("A1", "M5");
        //    chart.SetSourceData(range);

        //    // Set chart properties.
        //    chart.ChartType = Microsoft.Office.Interop.Excel.XlChartType.xl3DColumnClustered;
        //    chart.ChartWizard(Source: range,
        //        Title: graphTitle,
        //        CategoryTitle: xAxis,
        //        ValueTitle: yAxis);

        //    // Save.

        //    workbook.Save();
        //    workbook.Close();
        //    application.Quit();

        //    releaseObject(worksheet);
        //    releaseObject(workbook);
        //    releaseObject(application);

        //}


            //    Excel.Application xlApp ;
            //    Excel.Workbook xlWorkBook ;
            //    Excel.Worksheet xlWorkSheet ;
            //    object misValue = System.Reflection.Missing.Value;

            //    xlApp = new Excel.Application();
            //    xlWorkBook = xlApp.Workbooks.Add(misValue);
            //    xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            //    //add data 
            //    xlWorkSheet.Cells[1, 1] = "";
            //    xlWorkSheet.Cells[1, 2] = "Student1";
            //    xlWorkSheet.Cells[1, 3] = "Student2";
            //    xlWorkSheet.Cells[1, 4] = "Student3";

            //    xlWorkSheet.Cells[2, 1] = "Term1";
            //    xlWorkSheet.Cells[2, 2] = "80";
            //    xlWorkSheet.Cells[2, 3] = "65";
            //    xlWorkSheet.Cells[2, 4] = "45";

            //    xlWorkSheet.Cells[3, 1] = "Term2";
            //    xlWorkSheet.Cells[3, 2] = "78";
            //    xlWorkSheet.Cells[3, 3] = "72";
            //    xlWorkSheet.Cells[3, 4] = "60";

            //    xlWorkSheet.Cells[4, 1] = "Term3";
            //    xlWorkSheet.Cells[4, 2] = "82";
            //    xlWorkSheet.Cells[4, 3] = "80";
            //    xlWorkSheet.Cells[4, 4] = "65";

            //    xlWorkSheet.Cells[5, 1] = "Term4";
            //    xlWorkSheet.Cells[5, 2] = "75";
            //    xlWorkSheet.Cells[5, 3] = "82";
            //    xlWorkSheet.Cells[5, 4] = "68";

            //    Excel.Range chartRange ; 

            //    Excel.ChartObjects xlCharts = (Excel.ChartObjects)xlWorkSheet.ChartObjects(Type.Missing);
            //    Excel.ChartObject myChart = (Excel.ChartObject)xlCharts.Add(10, 80, 300, 250);
            //    Excel.Chart chartPage = myChart.Chart;

            //    chartRange = xlWorkSheet.get_Range("A1", "d5");
            //    chartPage.SetSourceData(chartRange, misValue);
            //    chartPage.ChartType = Excel.XlChartType.xlColumnClustered; 

            //    //xlWorkBook.SaveAs("csharp.net-informations.xls", Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
            //    xlWorkBook.SaveAs(excelFileName, Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
            //    xlWorkBook.Close(true, misValue, misValue);
            //    xlApp.Quit();

            //    releaseObject(xlWorkSheet);
            //    releaseObject(xlWorkBook);
            //    releaseObject(xlApp);

            //    //MessageBox.Show("Excel file created , you can find the file c:\\csharp.net-informations.xls");
            //}

            public void updateGraphRange(string excelFileName, string template, int RangeCNT)
            {

                // Open Excel and get first worksheet.

                var excelApplication = new Excel.Application();
                var workbooks = excelApplication.Workbooks;
                var workbook = workbooks.Open(excelFileName); // Fixed

                Excel.Range GraphRange1 = null;
                //Excel.Range GraphRange2 = null;

                string sheetName="Results";
                if (template.StartsWith("HotCorner")) sheetName="Graph";

                foreach (Excel.Worksheet worksheet in workbook.Worksheets)
                {

                    if (worksheet.Name == "Results")
                    {
                        if (template.StartsWith("HotCorner")) GraphRange1 = worksheet.get_Range("X2", "AB"+ RangeCNT.ToString());
                        if (template.StartsWith("Power_Profiling")) GraphRange1 = worksheet.get_Range("N2", "N"+ RangeCNT.ToString());
                        //if (template.StartsWith("Power_Profiling")) GraphRange2 = worksheet.get_Range("N2", "N"+ RangeCNT.ToString());
                        if (template.StartsWith("HotCorner"))
                        {
                            foreach (Excel.Range cell in GraphRange1.Cells)
                            {
                                if (string.IsNullOrEmpty(Convert.ToString(cell.Cells[1, 1].value2))) cell.Cells[1, 1].value2 = 0;
                            }
                        }
                    }
                    if (worksheet.Name == sheetName)
                    {
                        Excel.ChartObject chart = worksheet.ChartObjects(1);
                        if (template.StartsWith("HotCorner"))  chart.Chart.SetSourceData(GraphRange1);
                        if (template.StartsWith("Power_Profiling") && (chart.Chart.ChartTitle.Caption.StartsWith("Theta"))) chart.Chart.SetSourceData(GraphRange1);
                        //if (template.StartsWith("Power_Profiling") && (chart.Chart.ChartTitle.Caption.StartsWith("SomeethingElse"))) chart.Chart.SetSourceData(GraphRange2);

                        //Throws exception if chart 2 not exists
                        //chart = worksheet.ChartObjects(2);
                        //if (chart != null)
                        //{
                        //    if (template.StartsWith("Power_Profiling") && (chart.Chart.ChartTitle.Caption.StartsWith("Theta"))) chart.Chart.SetSourceData(GraphRange1);
                        //    //if (template.StartsWith("Power_Profiling") && (chart.Chart.ChartTitle.Caption.StartsWith("SomeethingElse"))) chart.Chart.SetSourceData(GraphRange2);
                        //}
                        
                        break;
                    }
                }

                workbook.Save();

                workbook.Close();
                workbooks.Close();
                excelApplication.Quit();

                releaseObject(workbook);
                releaseObject(workbooks);
                releaseObject(excelApplication);

            }

            public void updatePivothRange(string excelFileName, string PivotSheetName, string ResultsSheetName,int  RangeCNT)
            {

                // Open Excel and get first worksheet.

                var excelApplication = new Excel.Application();
                var workbooks = excelApplication.Workbooks;
                var workbook = workbooks.Open(excelFileName); // Fixed


                foreach (Excel.Worksheet worksheet in workbook.Worksheets)
                {

                    if (worksheet.Name == PivotSheetName)
                    {
                        Excel.PivotTable pivot = worksheet.PivotTables(1);
                        //Results from DB!R1C1:R101C119
                        // Row number = mumber of results, Col number - fixed in template (number of fields)
                        pivot.SourceData = ResultsSheetName + "!R1C1:R" + RangeCNT.ToString() + "C119";
                        pivot.RefreshTable();
                        pivot = worksheet.PivotTables(2);
                        //Results from DB!R1C1:R101C119
                        // Row number = mumber of results, Col number - fixed in template (number of fields)
                        pivot.SourceData = ResultsSheetName + "!R1C1:R" + RangeCNT.ToString() + "C119";
                        pivot.RefreshTable();
                        break;
                    }
                }

                workbook.Save();

                workbook.Close();
                workbooks.Close();
                excelApplication.Quit();

                releaseObject(workbook);
                releaseObject(workbooks);
                releaseObject(excelApplication);

            }

            public void exportSheetGraphToImage(string excelFileName, Dictionary<string, int> graphs, string imgNameSuffix, out string imgPath)
            {
                imgPath = "";
                  try
                    {
                        _exportSheetGraphToImage(excelFileName, graphs, imgNameSuffix, out imgPath);
                    }
                    finally
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
            }
            public void _exportSheetGraphToImage(string excelFileName, Dictionary<string,int>graphs, string imgNameSuffix, out string imgPath)
            {

                // Open Excel and get first worksheet.

                var excelApplication = new Excel.Application();
                var workbooks = excelApplication.Workbooks;
                var workbook = workbooks.Open(excelFileName); // Fixed

                imgPath = "";
                try
                {
                    foreach (var graphPair in graphs)
                    {
                        string sheetName = graphPair.Key;
                        int graphID = graphPair.Value;
                        foreach (Excel.Worksheet worksheet in workbook.Worksheets)
                        {
                            if (worksheet.Name == sheetName)
                            {

                                Excel.ChartObject chart = (Excel.ChartObject)worksheet.ChartObjects(graphID);
                                //Export Image example:
                                chart.Chart.Export(Path.GetDirectoryName(excelFileName) + @"\" + sheetName.Replace(" ", "") + imgNameSuffix + ".png", "PNG", false);
                                imgPath = Path.GetDirectoryName(excelFileName) + @"\" + sheetName.Replace(" ", "") + imgNameSuffix + ".png";
                                break;
                            }
                        }
                    }




                    //workbook.Save();

                    workbook.Close(0);
                    workbooks.Close();
                    excelApplication.Quit();
                }
                finally
                {

                    releaseObject(workbook);
                    releaseObject(workbooks);
                    releaseObject(excelApplication);
                }

            }

        }

    }
