using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OfficeOpenXml;

namespace CCD_EV_ManageDB.Models
{
    class DV_AnalysisHandler
    {
        public string corner;
        public string port;
        public string lane;

        Dictionary<string, DVLimit> Limits;

        public DV_AnalysisHandler()
        {
            Limits = new Dictionary<string, DVLimit>();
        }

        public DV_AnalysisHandler(string _corner,string _port, string _lane)
        {
            corner = _corner;
            port = _port;
            lane = _lane;
            Limits = new Dictionary<string, DVLimit>();
        }

        public bool Analyse(string limits_excel_path, string results_excel_path, out string message)
        {
            message = string.Empty;
            if (!readLimits(limits_excel_path))
            {
                message = "Read limits excel failed";
                return false; //TODO error handling
            }
            try
            {
                FileInfo newFile = new FileInfo(results_excel_path);
                using (ExcelPackage package = new ExcelPackage(newFile))
                {
                    //Exit in case the file is already analysed
                    foreach (ExcelWorksheet Worksheet in package.Workbook.Worksheets)
                    {
                        if (Worksheet.Name.StartsWith("Preset")) return true;
                    }

                    ExcelWorksheet worksheetMain = package.Workbook.Worksheets.ElementAt(0);
                    //start presets loop here?
                    for (int presetInd = 0; presetInd < 16; presetInd++)
                    {

                        //calc preset0 only:
                        SortedList<double, DVResult> presetResults = new SortedList<double, DVResult>();
                        int lineIndex = 2;
                        while ((worksheetMain.Cells[lineIndex, 1].Value != null) && (!string.IsNullOrEmpty(worksheetMain.Cells[lineIndex, 1].Value.ToString())))
                        {
                            {
                                if ((worksheetMain.Cells[lineIndex, 5 + presetInd].Value != null) && (worksheetMain.Cells[lineIndex, 5 + presetInd].Value.ToString() == "Pass")) 
                                {
                                    DVResult tmpResult = new DVResult();
                                    tmpResult.Post = worksheetMain.Cells[lineIndex, 1].Value.ToString();
                                    tmpResult.Pre = worksheetMain.Cells[lineIndex, 2].Value.ToString();
                                    tmpResult.Preshoot = Convert.ToDouble(worksheetMain.Cells[lineIndex, 3].Value.ToString());
                                    tmpResult.Deemphasis = Convert.ToDouble(worksheetMain.Cells[lineIndex, 4].Value.ToString());
                                    tmpResult.Preset = worksheetMain.Cells[1, 5 + presetInd].Value.ToString();
                                    tmpResult.Corner = this.corner;
                                    tmpResult.Port = this.port;
                                    tmpResult.Lane = this.lane;
                                    tmpResult.calcRank(this.Limits);
                                    presetResults.Add(tmpResult.Grade, tmpResult);
                                }
                            }
                            lineIndex++;
                        }
                        // add sheet with ranked results
                        ExcelWorksheet worksheetTMP = package.Workbook.Worksheets.Add(worksheetMain.Cells[1, 5 + presetInd].Value.ToString());
                        worksheetTMP.Cells[1, 1].Value = "Post";
                        worksheetTMP.Cells[1, 2].Value = "Pre";
                        worksheetTMP.Cells[1, 3].Value = "Preshoot";
                        worksheetTMP.Cells[1, 4].Value = "Deemphasis";
                        worksheetTMP.Cells[1, 5].Value = "Preset";
                        worksheetTMP.Cells[1, 6].Value = "Corner";
                        worksheetTMP.Cells[1, 7].Value = "Lane";
                        worksheetTMP.Cells[1, 8].Value = "Port";
                        worksheetTMP.Cells[1, 9].Value = "Place";
                        worksheetTMP.Cells[1, 10].Value = "Key";
                        lineIndex = 2;
                        foreach (var pair in presetResults)
                        {
                            worksheetTMP.Cells[lineIndex, 1].Value = pair.Value.Post;
                            worksheetTMP.Cells[lineIndex, 2].Value = pair.Value.Pre;
                            worksheetTMP.Cells[lineIndex, 3].Value = pair.Value.Preshoot;
                            worksheetTMP.Cells[lineIndex, 4].Value = pair.Value.Deemphasis;
                            worksheetTMP.Cells[lineIndex, 5].Value = pair.Value.Preset;
                            worksheetTMP.Cells[lineIndex, 6].Value = pair.Value.Corner;
                            worksheetTMP.Cells[lineIndex, 7].Value = pair.Value.Lane;
                            worksheetTMP.Cells[lineIndex, 8].Value = pair.Value.Port;
                            worksheetTMP.Cells[lineIndex, 9].Value = lineIndex - 1;
                            worksheetTMP.Cells[lineIndex, 10].Value = pair.Value.Post + "_" + pair.Value.Pre;
                            lineIndex++;
                        }
                    }

                    package.Save();
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                return false;
            }
            return true;
        }

        bool readLimits (string limits_excel_path)
        {
            try
            {
                FileInfo newFile = new FileInfo(limits_excel_path);
                using (ExcelPackage package = new ExcelPackage(newFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                    int lineIndex = 1;
                    while (lineIndex<=17)
                    {
                        if ((worksheet.Cells[lineIndex, 1].Value != null) &&
                            (!string.IsNullOrEmpty(worksheet.Cells[lineIndex, 1].Value.ToString())) &&
                            (!worksheet.Cells[lineIndex, 1].Value.ToString().StartsWith("P")))
                        {
                            string key = "Preset" + worksheet.Cells[lineIndex, 1].Value.ToString();
                            Limits.Add(key, new DVLimit (Convert.ToDouble(worksheet.Cells[lineIndex, 2].Value.ToString()),Convert.ToDouble(worksheet.Cells[lineIndex, 3].Value.ToString())));
                        }
                        lineIndex++;
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public bool DV_Analyse_Summary(string filepath)
        {
            FileInfo newFile = new FileInfo(filepath);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                foreach (ExcelWorksheet worksheet in package.Workbook.Worksheets)
                {
                    if (!worksheet.Name.StartsWith("Preset")) continue;
                    Dictionary<string, List<DVResult>> Results = new Dictionary<string,List<DVResult>>(); //key=Pre_Post  val=list of results
                    Dictionary<string, HashSet<string>> CornerPortLane = new Dictionary<string, HashSet<string>>(); //Key - Pre_Post
                    Dictionary<string, int> SumOfPlace = new Dictionary<string, int>(); //Key - Pre_Post
                    int lineIndex = 2;
                    while ((worksheet.Cells[lineIndex, 1].Value != null) && (!string.IsNullOrEmpty(worksheet.Cells[lineIndex, 1].Value.ToString())))
                    {
                        string Key = worksheet.Cells[lineIndex, 10].Value.ToString();
                        DVResult result = new DVResult();

                        result.Post=worksheet.Cells[lineIndex, 1].Value.ToString();
                        result.Pre = worksheet.Cells[lineIndex, 2].Value.ToString();
                        //result.Preshoot=worksheet.Cells[lineIndex, 3].Value.ToString();
                        //result.Deemphasis=worksheet.Cells[lineIndex, 4].Value.ToString();
                        result.Preset=worksheet.Cells[lineIndex, 5].Value.ToString();
                        result.Corner=worksheet.Cells[lineIndex, 6].Value.ToString();
                        result.Lane=worksheet.Cells[lineIndex, 7].Value.ToString().ToUpper();
                        result.Port=worksheet.Cells[lineIndex, 8].Value.ToString().ToUpper();
                        result.Grade=Convert.ToDouble(worksheet.Cells[lineIndex, 9].Value.ToString());
                        if (!Results.ContainsKey(Key))
                        {
                            Results.Add(Key, new List<DVResult>());
                            CornerPortLane.Add(Key,new HashSet<string>());
                            SumOfPlace.Add(Key,0);
                        }
                        Results[Key].Add(result);
                        CornerPortLane[Key].Add(result.Corner + "_" + result.Port + "_" + result.Lane);
                        //No handling for duplicate results
                        SumOfPlace[Key] += (int)result.Grade;
                        lineIndex++;
                    }

                    SortedList<int, string> CornerPortLaneSummary = new SortedList<int, string>(new DuplicateKeyDescComparer<int>());
                    foreach (var pair in CornerPortLane)
                    {
                        CornerPortLaneSummary.Add(pair.Value.Count, pair.Key); // Order results , by #corne_lanes permutation
                    }

                    lineIndex = 1;
                    System.Drawing.Color color = System.Drawing.Color.LightGray;
                    worksheet.Cells[lineIndex, 11].Value = "Best Post_Pre:";
                    worksheet.Cells[lineIndex, 11].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, 11].Style.Fill.BackgroundColor.SetColor(color);
                    worksheet.Cells[lineIndex, 12].Value = "Num of pass corners_lanes:";
                    worksheet.Cells[lineIndex, 12].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, 12].Style.Fill.BackgroundColor.SetColor(color);
                    worksheet.Cells[lineIndex, 13].Value = "Place Sum:";
                    worksheet.Cells[lineIndex, 13].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[lineIndex, 13].Style.Fill.BackgroundColor.SetColor(color);

                    lineIndex++;

                    int LastNumOfPassCornersLanes = 0;
                    if (CornerPortLaneSummary.Count>0) LastNumOfPassCornersLanes = CornerPortLaneSummary.First().Key;
                    SortedList<int, string> SumOfPlaceCornerLanesSummary = new SortedList<int, string>(new DuplicateKeyComparer<int>());

                    foreach (var pair in CornerPortLaneSummary)
                    {
                        //Additional sort - build another sorted list for all results with same "Num of pass corners_lanes:", use "Grade Sum:" 
                        //     as key, and "Pass in most corners_lanes" as value, send to Excel when "Num of pass corners_lanes:" changes

                        if (pair.Key == LastNumOfPassCornersLanes) SumOfPlaceCornerLanesSummary.Add(SumOfPlace[pair.Value], pair.Value);
                        else
                        {
                            foreach (var _pair in SumOfPlaceCornerLanesSummary)
                            {
                                color = System.Drawing.Color.Gray;
                                if (lineIndex == 2) color = System.Drawing.Color.LightGreen;
                                if (lineIndex == 3) color = System.Drawing.Color.LightSkyBlue;
                                if (lineIndex == 4) color = System.Drawing.Color.LightPink;
                                //worksheet.Cells[lineIndex, 11].Value = "Pre_Post:";
                                worksheet.Cells[lineIndex, 11].Value = _pair.Value;
                                worksheet.Cells[lineIndex, 11].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                worksheet.Cells[lineIndex, 11].Style.Fill.BackgroundColor.SetColor(color);
                                //worksheet.Cells[lineIndex, 13].Value = "Num of pass corners_lanes:";
                                worksheet.Cells[lineIndex, 12].Value = LastNumOfPassCornersLanes;
                                worksheet.Cells[lineIndex, 12].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                worksheet.Cells[lineIndex, 12].Style.Fill.BackgroundColor.SetColor(color);
                                //worksheet.Cells[lineIndex, 15].Value = "Place Sum:";
                                worksheet.Cells[lineIndex, 13].Value = _pair.Key;
                                worksheet.Cells[lineIndex, 13].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                worksheet.Cells[lineIndex, 13].Style.Fill.BackgroundColor.SetColor(color);
                                lineIndex++;
                            }
                            LastNumOfPassCornersLanes = pair.Key;
                            SumOfPlaceCornerLanesSummary.Clear();
                            SumOfPlaceCornerLanesSummary.Add(SumOfPlace[pair.Value], pair.Value);
                        }
                   }
                    foreach (var _pair in SumOfPlaceCornerLanesSummary)
                    {
                        color = System.Drawing.Color.LightGray;
                        if (lineIndex == 2) color = System.Drawing.Color.LightGreen;
                        if (lineIndex == 3) color = System.Drawing.Color.LightSkyBlue;
                        if (lineIndex == 4) color = System.Drawing.Color.LightPink;
                        //worksheet.Cells[lineIndex, 11].Value = "Pre_Post:";
                        worksheet.Cells[lineIndex, 11].Value = _pair.Value;
                        worksheet.Cells[lineIndex, 11].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, 11].Style.Fill.BackgroundColor.SetColor(color);
                        //worksheet.Cells[lineIndex, 13].Value = "Num of pass corners_lanes:";
                        worksheet.Cells[lineIndex, 12].Value = LastNumOfPassCornersLanes;
                        worksheet.Cells[lineIndex, 12].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, 12].Style.Fill.BackgroundColor.SetColor(color);
                        //worksheet.Cells[lineIndex, 15].Value = "Place Sum:";
                        worksheet.Cells[lineIndex, 13].Value = _pair.Key;
                        worksheet.Cells[lineIndex, 13].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, 13].Style.Fill.BackgroundColor.SetColor(color);
                        lineIndex++;
                    }
                   
                    worksheet.Cells.AutoFitColumns(0);  //Autofit columns for all cells
                }

                package.Save();
            }
            return true;
        }
    }

    /// <summary>
    /// Comparer for comparing two keys, handling equality as beeing greater
    /// Use this Comparer e.g. with SortedLists or SortedDictionaries, that don't allow duplicate keys
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class DuplicateKeyComparer<TKey>
                    :
                 IComparer<TKey> where TKey : IComparable
    {
        #region IComparer<TKey> Members

        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1;   // Handle equality as beeing greater
            else
                return result;
        }

        #endregion
    }

    public class DuplicateKeyDescComparer<TKey>
                :
             IComparer<TKey> where TKey : IComparable
    {
        #region IComparer<TKey> Members

        public int Compare(TKey x, TKey y)
        {
            int result = y.CompareTo(x);

            if (result == 0)
                return 1;   // Handle equality as beeing greater
            else
                return result;
        }

        #endregion
    }

    class DVLimit
    {
        public double Preshoot;
        public double Deemphasis;

        public DVLimit(double P, double D)
        {
            Preshoot = P;
            Deemphasis = D;
        }
    }

    class DVResult
    {
        public double Preshoot;
        public double Deemphasis;
        public string Post;
        public string Pre;
        public string Corner;
        public string Port;
        public string Lane;
        public double Grade;
        public string Preset;


        public DVResult()
        {
        }

        public DVResult(string post, string pre, double preshoot, double deemphasis, string preset)
        {
            Preshoot = preshoot;
            Deemphasis = deemphasis;
            Post = post;
            Pre = pre;
            Preset = preset;
        }

        public void calcRank(Dictionary<string,DVLimit> limits)
        {
            Grade = Math.Abs(limits[this.Preset].Deemphasis - Deemphasis) + Math.Abs(limits[this.Preset].Preshoot - Preshoot);
        }
    }
}
