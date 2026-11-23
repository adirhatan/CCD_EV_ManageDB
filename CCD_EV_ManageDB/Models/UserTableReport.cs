using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using CCD_EV_ManageDB.Repositories;
using System.Text.RegularExpressions;

namespace CCD_EV_ManageDB.Models
{
    public class UserTableReport
    {
        public string _id { get; set; }
        public string Name { get; set; }
        public string TestName { get; set; }
        public ReportConfig RConfig { get; set; }
        public DBsearchSave Search { get; set; }
        public string ResultsPath { get; set; }
        public bool useMarginLimits { get; set; }

        public UserTableReport()
        {
            RConfig = new ReportConfig("");
            Search = new DBsearchSave();
        }

        public UserTableReport(string username,string _name,string tn)
        {
            _id = username + DateTime.Now.ToString("yyyyMMddHHmmss");
            Name = _name;
            RConfig = new ReportConfig(tn);
            Search = new DBsearchSave();
        }

        public UserTableReport(string username, string _name, UserTableReport SourceReport)
        {
            _id = username + DateTime.Now.ToString("yyyyMMddHHmmss");
            Name = _name;
            RConfig = SourceReport.RConfig;
            TestName = SourceReport.TestName;
            Search = new DBsearchSave();
        }

        public bool buildLimitsExcel(string filePath, string FieldSlidesStr)
        {
            ExcelHandler ExlExport = new ExcelHandler();
            List<BsonDocument> limits = new List<BsonDocument>();
            List<string> limitsCols = new List<string>();
            limitsCols.Add("Test");
            limitsCols.Add("Result");
            limitsCols.Add("lsl");
            limitsCols.Add("usl");
            List<string> FieldSlidesList = new List<string>();
            if (!string.IsNullOrEmpty(FieldSlidesStr))
            {
                FieldSlidesList = new List<string>(FieldSlidesStr.Split(','));
            }
            int Fcnt = -1;
            foreach (FieldSlide fs in RConfig.FieldsSlides)
            {
                Fcnt++;
                if ((FieldSlidesList.Count == 0) || (FieldSlidesList.Contains(Fcnt.ToString())))
                {
                    if (fs.type == General.ReportSlideType.image.ToString()) continue;
                    if ((!String.IsNullOrEmpty(fs.lsl)) || (!String.IsNullOrEmpty(fs.usl)))
                    {
                        BsonDocument tmp = new BsonDocument();
                        string _TestName = fs.test;
                        //31 excle limit
                        if (_TestName.Length > 15) _TestName = FieldSlide.GetLimit31TestName(RConfig.table,_TestName);
                        else
                        {
                            _TestName = Regex.Replace(_TestName, "\\W", "");
                            _TestName = Regex.Replace(_TestName, "_", "");
                        }
                        tmp.Set("Test", _TestName);
                        tmp.Set("Result", fs.column);
                        tmp.Set("lsl", fs.lsl);
                        tmp.Set("usl", fs.usl);
                        limits.Add(tmp);
                    }
                }
            }
            ExlExport.writeSheet(filePath, "Limits", limits, limitsCols);
            return true;
        }

        public bool ExportReportFile(DBsearch DBS, string filePath, string margin_filePath, string debug, string FieldSlidesStr)
        {
            //new for margin start1
            Dictionary<string, ReportMargin> MarginsDictionary = new Dictionary<string, ReportMargin>();
            //new for margin end1

            XMLReportHandler XMLer = new XMLReportHandler();
            Dictionary<string, HashSet<string>> TemplatesCoreColumns = new Dictionary<string, HashSet<string>>();
            foreach (FieldSlide fs in this.RConfig.FieldsSlides)
            {
                if (TemplatesCoreColumns.ContainsKey(fs.test))
                {
                    foreach (string col in XMLer.ParseTemplateColsXML(fs.templateKey)) TemplatesCoreColumns[fs.test].Add(col);
                }
                else
                {
                    TemplatesCoreColumns.Add(fs.test, XMLer.ParseTemplateColsXML(fs.templateKey));
                }
            }


            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptyExcel"];
            string ServerDir  = System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; 

            Dictionary<string, List<BsonDocument>> dictionary =
                    new Dictionary<string, List<BsonDocument>>();
            Dictionary<string, HashSet<string>> dictionaryFields = new Dictionary<string, HashSet<string>>();

            ExcelHandler ExlExport = new ExcelHandler();

            MDBStationResults resultsDBS = new MDBStationResults(DBS.db, DBS.col);

            List<string> FieldSlidesList = new List<string>();

            if (!string.IsNullOrEmpty(FieldSlidesStr))
            {
                FieldSlidesList = new List<string>(FieldSlidesStr.Split(','));
            }
          
            System.IO.File.Copy(EmptyExcel, filePath, true);

            
            try
            {
                foreach (BsonDocument item in DBS.ResCursor)
                {
                    //
                    item.Set("SkewSigmaUnitId",item.GetValue("Skew","").ToString()+"_" + item.GetValue("ULTSkew","").ToString() + "Sigma_" + item.GetValue("UnitId","").ToString());
                    //
                    string _OrigTestName = item.GetValue(this.TestName, "noTestName").ToString();
                    string _TestName = item.GetValue(this.TestName, "noTestName").ToString();
                    //31 excle limit
                    if (_TestName.Length > 15) _TestName = FieldSlide.GetLimit31TestName(RConfig.table, _TestName);
                    else
                    {
                        _TestName = Regex.Replace(_TestName, "\\W", "");
                        _TestName = Regex.Replace(_TestName, "_", "");
                    }
                    item.Set(this.TestName, _TestName);
                    //new: remove string chars froreport values
                    int Fcnt = 0;
                    foreach (FieldSlide fs in this.RConfig.FieldsSlides)
                    {
                        if ((FieldSlidesList.Count==0) || (FieldSlidesList.Contains(Fcnt.ToString())))
                        {
                            if ((_OrigTestName == fs.test) && (fs.type == General.ReportSlideType.limits.ToString()))
                            {
                                string field = item.GetValue(fs.column, "").ToString();
                                string pattern = @"([\d\.\-\+eE]*) *[a-zA-Z]*";
                                string replacement = "$1";
                                string result = Regex.Replace(field, pattern, replacement);
                                if (field.ToUpper() == "PASS") result = "1";
                                if (field.ToUpper() == "FAIL") result = "0";
                                if (result != field) item.Set(fs.column, result);
                                //new for Report Margin start2
                                if (this.useMarginLimits)
                                {
                                    if (!MarginsDictionary.ContainsKey(_OrigTestName + " - " + fs.column))
                                    {
                                        //FieldSlide tmpFS = this.GetFieldSlideForTest(_OrigTestName);
                                        FieldSlide tmpFS = fs;
                                        if (!String.IsNullOrEmpty(tmpFS.test))
                                        {
                                            MarginsDictionary.Add(tmpFS.test + " - " + tmpFS.column, new ReportMargin(tmpFS.column, tmpFS.usl, tmpFS.lsl, tmpFS.margin));
                                        }
                                    }
                                    if (MarginsDictionary.ContainsKey(_OrigTestName + " - " + fs.column))
                                    {
                                        bool isDecVal = true;
                                        string strVal = item.GetValue(MarginsDictionary[_OrigTestName + " - " + fs.column].field, "").ToString().ToUpper();
                                        decimal decVal = 0;
                                        try
                                        {
                                            if (strVal.Contains("E-")) decVal = Decimal.Parse(strVal, System.Globalization.NumberStyles.Float);
                                            else decVal = Convert.ToDecimal(strVal);
                                        }
                                        catch (Exception e)
                                        {
                                            isDecVal = false;
                                        }
                                        if ((isDecVal) && (String.IsNullOrEmpty(MarginsDictionary[_OrigTestName + " - " + fs.column].max)))
                                        {
                                            MarginsDictionary[_OrigTestName + " - " + fs.column].max = decVal.ToString();
                                        }
                                        if ((isDecVal) && (Convert.ToDecimal(MarginsDictionary[_OrigTestName + " - " + fs.column].max) < decVal))
                                        {
                                            MarginsDictionary[_OrigTestName + " - " + fs.column].max = decVal.ToString();
                                        }
                                        if ((isDecVal) && (String.IsNullOrEmpty(MarginsDictionary[_OrigTestName + " - " + fs.column].min)))
                                        {
                                            MarginsDictionary[_OrigTestName + " - " + fs.column].min = decVal.ToString();
                                        }
                                        if ((isDecVal) && (Convert.ToDecimal(MarginsDictionary[_OrigTestName + " - " + fs.column].min) > decVal))
                                        {
                                            MarginsDictionary[_OrigTestName + " - " + fs.column].min = decVal.ToString();
                                        }

                                    }
                                }
                                //new for Report Margin end2
                            }
                            if ((_OrigTestName == fs.test) && (fs.type == General.ReportSlideType.image.ToString()))
                            {
                                ////old
                                //    string field = item.GetValue(fs.imageColumn, "").ToString();
                                //    item.Set("attachment"+fs.column, field);

                                //new:
                                foreach (ImageDetails imgDetails in this.RConfig.GetSlideImageDetails(fs.imageDetailsID))
                                {
                                    bool addAttach = true;
                                    if (!string.IsNullOrWhiteSpace(imgDetails.query))
                                    {
                                        //TODO: qury + doc id
                                        try
                                        {
                                            BsonDocument res = resultsDBS.MDBFindOne("EQ;_id;" + item.GetValue("_id").ToString() + ";" + imgDetails.query, "AND", "Des", "TestEnd");
                                            if (res == null) addAttach = false;
                                        }
                                        catch (Exception e)
                                        {
                                            addAttach = false;
                                            //we can add "error" jpg in this case...
                                        }

                                    }
                                    if (addAttach)
                                    {
                                        string field = item.GetValue(imgDetails.field, "").ToString();
                                        item.Set(imgDetails.field + imgDetails.order.ToString(), "1");
                                        item.Set("attachment" + imgDetails.field + imgDetails.order.ToString(), field);
                                    }
                                }
                            }
                        }
                        Fcnt++;
                    }
                    //new
                    if (!dictionary.ContainsKey(_TestName))
                    {
                        dictionary.Add(_TestName, new List<BsonDocument>());
                        dictionaryFields.Add(_TestName, new HashSet<string>());
                    }
                    //Commentes Due to internal memory problem - take only the fields we need see below
                    //dictionary[_TestName].Add(item);

                    //fields
                    if (debug.Substring(0, 1).ToUpper() == "Y")
                    {
                        //Take all fields excluding HIDDEN
                        foreach (BsonElement field in item)
                        {
                            if ((field.Name.Length > 6) && (field.Name.Substring(0, 6) == "HIDDEN")) continue;
                            else dictionaryFields[_TestName].Add(field.Name);
                        }
                    }
                    //else
                    //{
                        Fcnt = 0;
                        //New: take only the needed column for the test
                        foreach (FieldSlide fs in this.RConfig.FieldsSlides)
                        {
                            if ((FieldSlidesList.Count == 0) || (FieldSlidesList.Contains(Fcnt.ToString())))
                            {
                                if (_OrigTestName == fs.test)
                                {
                                    if (dictionaryFields[_TestName].Count == 0) dictionaryFields[_TestName].UnionWith(TemplatesCoreColumns[fs.test]);
                                    dictionaryFields[_TestName].Add(fs.column);
                                    ////old
                                    //if (fs.type == General.ReportSlideType.image.ToString()) dictionaryFields[_TestName].Add("attachment" + fs.column);
                                    //new
                                    if (fs.type == General.ReportSlideType.image.ToString())
                                    {
                                        foreach (ImageDetails imgDetails in this.RConfig.GetSlideImageDetails(fs.imageDetailsID))
                                        {
                                            dictionaryFields[_TestName].Add(imgDetails.field + imgDetails.order.ToString());
                                            dictionaryFields[_TestName].Add("attachment" + imgDetails.field + imgDetails.order.ToString());
                                        }
                                    }

                                }
                            }
                            Fcnt++;
                        }
                    //}

                    //Due to internal memory problem - take only the fields we need
                    BsonDocument tmp = new BsonDocument();
                    foreach (string col in dictionaryFields[_TestName].ToList<string>())
                    {
                        tmp.Set(col,item.GetValue(col, ""));
                    }
                    dictionary[_TestName].Add(tmp);
                }
            }
            catch (Exception e)
            {
                return false;
            }
            foreach (var pair in dictionary) 
                if (dictionaryFields[pair.Key].Count>0) ExlExport.writeSheet(filePath, pair.Key, pair.Value, dictionaryFields[pair.Key].ToList<string>());

            //new for margin start3
            if (this.useMarginLimits)
            {
                System.IO.File.Copy(Path.Combine(ServerDir, "EVData", "emptyReportMargin.xlsx"), margin_filePath, true);
                ExlExport.writeFieldsHeaderTmplSheet(margin_filePath, "Margins", GetCalculatedMargins(MarginsDictionary), 1);
            }
            //new for margin end3
            return true;
        }

        public bool ParseSlidesStr(string FieldsSlidesStr, string TitleItemsStr, out string message)
        {
            message = "";
            this.RConfig.FieldsSlides.Clear();
            this.RConfig.TitleItems.Clear();
            //TODO build Report...
            String[] TIarr = TitleItemsStr.Split(';');
            for (int i = 0; i < TIarr.Length; i += 4)
            {
                //TODO: client validation
                if ((String.IsNullOrEmpty(TIarr[i])) || (String.IsNullOrEmpty(TIarr[i + 1])) || (String.IsNullOrEmpty(TIarr[i + 2])) ||
                    (String.IsNullOrEmpty(TIarr[i] + 3)))
                {
                    message = "Save of report " + this.Name + " failed, Titles parameter cannot be empty";
                    return false;
                }
                this.RConfig.TitleItems.Add(new TitleItem(TIarr[i], TIarr[i + 1], TIarr[i + 2], TIarr[i + 3].Trim()));
            }
            String[] FSarr = FieldsSlidesStr.Split(';');
            for (int i = 0; i < FSarr.Length; i += 9)
            {
                if (FSarr[i + 8] == General.ReportSlideType.limits.ToString())
                {
                    //TODO: client validation
                    if (!String.IsNullOrEmpty(FSarr[i + 4]))
                        try
                        {
                            Convert.ToDouble(FSarr[i + 4]);
                        }
                        catch (Exception ex)
                        {
                            message = "Save of report " + this.Name + " failed, limits must be numeric or empty in slide:" + FSarr[i];
                            return false;
                        }
                    if (!String.IsNullOrEmpty(FSarr[i + 5]))
                        try
                        {
                            Convert.ToDouble(FSarr[i + 5]);
                        }
                        catch (Exception ex)
                        {
                            message = "Save of report " + this.Name + " failed, limits must be numeric or empty in slide:" + FSarr[i];
                            return false;
                        }
                    if ((!String.IsNullOrEmpty(FSarr[i + 4])) && (!String.IsNullOrEmpty(FSarr[i + 5])) &&
                        (Convert.ToDouble(FSarr[i + 5])) < (Convert.ToDouble(FSarr[i + 4])))
                    {
                        message = "Save of report " + this.Name + " failed, upper limit must be greater than lower in slide:" + FSarr[i];
                        return false;
                    }
                    if (!String.IsNullOrEmpty(FSarr[i + 7]))
                        try
                        {
                            Convert.ToDecimal(FSarr[i + 7]);
                        }
                        catch (Exception ex)
                        {
                            message = "Save of report " + this.Name + " failed, Margin limits must be numeric or empty in slide:" + FSarr[i];
                            return false;
                        }
                    if (String.IsNullOrEmpty(FSarr[i + 7])) FSarr[i + 7] = "20";
                    this.RConfig.FieldsSlides.Add(new FieldSlide(FSarr[i + 8], FSarr[i + 3], FSarr[i], FSarr[i + 1], FSarr[i + 2], FSarr[i + 4], FSarr[i + 5], null, FSarr[i + 6],Convert.ToDecimal(FSarr[i + 7])));
                }
                if (FSarr[i + 8] == General.ReportSlideType.image.ToString())
                {

                    this.RConfig.FieldsSlides.Add(new FieldSlide(FSarr[i + 8], FSarr[i + 3], FSarr[i], FSarr[i + 1], FSarr[i + 2], "", "", FSarr[i + 4],""));
                }

            }
            return true;
        }
        //public List<ImageDetails> GetSlideImageDetails(string ImageDetilsID)
        //{
        //    List<ImageDetails> _ImageDetails = new List<ImageDetails>();
        //    initImagesDetailsForSlide(ImageDetilsID);
        //    foreach (ImageDetails item in this.RConfig.ImagesDetails)
        //    {
        //        if (item.imageDetailsID == ImageDetilsID) _ImageDetails.Add(item);
        //    }
        //    return _ImageDetails.OrderBy(o => o.order).ToList();

        //}
        //private void initImagesDetailsForSlide(string ImageDetilsID)
        //{
        //    //TODO: error handling
        //    foreach (ImageDetails item in this.RConfig.ImagesDetails)
        //    {
        //        if (item.imageDetailsID == ImageDetilsID) return;
        //    }
        //    this.RConfig.ImagesDetails.Add(new ImageDetails(GetImageSlide(ImageDetilsID),1));
        //    this.RConfig.ImagesDetails.Add(new ImageDetails(GetImageSlide(ImageDetilsID), 2));
        //    this.RConfig.ImagesDetails.Add(new ImageDetails(GetImageSlide(ImageDetilsID), 3));
        //    this.RConfig.ImagesDetails.Add(new ImageDetails(GetImageSlide(ImageDetilsID), 4));
        //}

        //New TODO
        public bool updateImagesDetails(string slideTest,string slideText, string ImageDetilsID, string template,string []fields, string []queries,string []texts,out string message)
        {
            message = "";
            List<ImageDetails> tmpImageDetails = new List<ImageDetails>();
            foreach (ImageDetails item in this.RConfig.ImagesDetails)
            {
                if (item.imageDetailsID != ImageDetilsID) tmpImageDetails.Add(item);
            }
            this.RConfig.ImagesDetails = tmpImageDetails;
            //Add some other fields editing ? Template?
            int cnt = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                //Add Validation??
                cnt++;
                this.RConfig.ImagesDetails.Add(new ImageDetails(cnt, fields[i], queries[i], texts[i], slideTest, ImageDetilsID));
            }
            FieldSlide FS = this.RConfig.GetImageSlide(ImageDetilsID);
            FS.templateKey = template;
            FS.test = slideTest;
            FS.text = slideText;
            FS.column = fields[0];

            List<FieldSlide> tmp = new List<FieldSlide>();
            foreach (FieldSlide tmpFS in this.RConfig.FieldsSlides)
            {
                if (tmpFS.imageDetailsID == ImageDetilsID)
                {
                    tmpFS.templateKey = template;
                    tmpFS.test = slideTest;
                    tmpFS.text = slideText;
                    tmpFS.column = fields[0];
                }
                tmp.Add(tmpFS);
            }
            this.RConfig.FieldsSlides = tmp;
            return true;
        }

        //public FieldSlide GetImageSlide(string _ImageSlideID)
        //{
        //    foreach (var slide in this.RConfig.FieldsSlides) if (slide.imageDetailsID == _ImageSlideID) return slide;
        //    return (new FieldSlide("", "", "", "", "", "", "", ""));

        //}

        public void addReportTests(Dictionary<string, HashSet<string>> dictionaryFields)
        {
            foreach (FieldSlide fs in this.RConfig.FieldsSlides)
            {
                if (!dictionaryFields.ContainsKey(fs.test))
                { 
                    dictionaryFields.Add(fs.test, new HashSet<string>(new string[] { fs.column }));
                }
            }
            foreach (ImageDetails imd in this.RConfig.ImagesDetails)
            {
                if (dictionaryFields.ContainsKey(imd.test) && (!dictionaryFields[imd.test].Contains(imd.field)))
                {
                    dictionaryFields[imd.test].Add(imd.field);
                }
            }
        }

        public List<BsonDocument> LaunchValidate(string _query, string db, string FieldSlidesStr)
        {
            List<BsonDocument> MissingResults = new List<BsonDocument>();
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            //UserTableReport Report = MDBReport.ReadUserTableReport(ReportID);
            MDBStationResults MDBStationResults = new MDBStationResults("DB_" + db, RConfig.table);
            List<string> FieldSlidesList = new List<string>();
            if (!string.IsNullOrEmpty(FieldSlidesStr))
            {
                FieldSlidesList = new List<string>(FieldSlidesStr.Split(','));
            }

            int Fcnt = 0;
            foreach (FieldSlide FS in RConfig.FieldsSlides)
            {
                if ((FieldSlidesList.Count == 0) || (FieldSlidesList.Contains(Fcnt.ToString())))
                {
                    if (FS.type == General.ReportSlideType.limits.ToString())
                    {
                        string query = _query + ";";
                        //EQ;SpecTestName;TestCSV;ExistNotEmpty;PCIE_INFO_1K_PE_RESULTS;
                        query += "EQ;" + TestName + ";" + FS.test + ";";
                        query += "ExistNotEmpty;" + FS.column + ";";
                        BsonDocument res = MDBStationResults.MDBFindOne(query, "AND", "Des", "TestEnd");
                        if ((res == null) || (string.IsNullOrEmpty(res.GetValue(FS.column, "").ToString())))
                        {
                            BsonDocument MissedRes = new BsonDocument {
                                { "TestName", FS.test },
                                { "Field", FS.column },
                                { "SlideNum",Fcnt}
                                };
                            MissingResults.Add(MissedRes);
                        }
                    }
                    //if (FS.type == General.ReportSlideType.image.ToString())
                    //{
                    //    foreach (ImageDetails imgDetails in this.RConfig.GetSlideImageDetails(FS.imageDetailsID))
                    //    {
                    //        string query = _query + ";";
                    //        query += "EQ;" + TestName + ";" + FS.test + ";";
                    //        query += "ExistNotEmpty;" + imgDetails.field + ";";
                    //        if (!string.IsNullOrWhiteSpace(imgDetails.query))
                    //        {
                    //            query += ";" + imgDetails.query;
                    //        }
                    //        try
                    //        {
                    //            BsonDocument res = MDBStationResults.MDBFindOne(query, "AND", "Des", "TestEnd");
                    //            if (res == null)
                    //            {
                    //                BsonDocument MissedRes = new BsonDocument {
                    //                    { "TestName", FS.test },
                    //                    { "Field", imgDetails.field }
                    //                    };
                    //                MissingResults.Add(MissedRes);
                    //            }
                    //        }
                    //        catch (Exception e)
                    //        {
                    //            BsonDocument MissedRes = new BsonDocument {
                    //                    { "TestName", FS.test },
                    //                    { "Field", imgDetails.field }
                    //                    };
                    //            MissingResults.Add(MissedRes);
                    //        }

                    //    }
                    //}
                }
                Fcnt++;
            }
            return MissingResults;
        }
        //private string GetLimit31TestName(string _TestName)
        //{
        //    if (this.RConfig.table.StartsWith("DP"))
        //    {
        //        string bitrate = "";
        //        string swing = "";
        //        string emphasis = "";
        //        string cable = "";
        //        _TestName = Regex.Replace(_TestName, "\\W", "");
        //        _TestName = Regex.Replace(_TestName, "_", "");
        //        //example: VTX TRANSITION BIT OUTPUT RATIO_5.4 Gbps_Pre-emphasis 1_Swing 0
        //        if (Regex.IsMatch(_TestName, @"\d*Gbps"))
        //        {
        //            Match m = Regex.Match(_TestName, @"(\d*)Gbps");
        //            bitrate = m.Groups[1].Value;
        //        }
        //        if (Regex.IsMatch(_TestName, @"Swing\d*"))
        //        {
        //            Match m = Regex.Match(_TestName, @"Swing(\d*)");
        //            swing = m.Groups[1].Value;
        //        }
        //        if (Regex.IsMatch(_TestName, @"emphasis\d*"))
        //        {
        //            Match m = Regex.Match(_TestName, @"emphasis(\d*)");
        //            emphasis = m.Groups[1].Value;
        //        }
        //        if (Regex.IsMatch(_TestName, @"cable"))
        //        {
        //            cable = "nc";
        //        }
        //        if (_TestName.Length > 31) _TestName = _TestName.Substring(0, 16) + cable + "BR" + bitrate + "SW" + swing + "EM" + emphasis;
        //    }
        //    else
        //    {
        //        _TestName = Regex.Replace(_TestName, "\\W", "");
        //        _TestName = Regex.Replace(_TestName, "_", "");
        //        if (_TestName.Length > 31) _TestName = _TestName.Substring(0, 15) + _TestName.Substring(_TestName.Length - 16, 16);
        //    }
        //    return _TestName;

        //}

        private FieldSlide GetFieldSlideForTest(string TestName)
        {
            foreach (FieldSlide fs in this.RConfig.FieldsSlides)
            {
                if ((TestName == fs.test) && (fs.type == General.ReportSlideType.limits.ToString())) return fs;
            }
            return new FieldSlide("", "", "", "", "", "", "", "", "");
        }

         //new for margin
        public List<BsonDocument> GetCalculatedMargins(Dictionary<string, ReportMargin> MarginDictionary)
        {
            List<BsonDocument> resultsList = new List<BsonDocument>();
            foreach (var pair in MarginDictionary)
            {
                if (pair.Value.marginLimit == "0") continue;
                BsonDocument marginResult = new BsonDocument();
                marginResult.Set("Test Name", pair.Key);
                marginResult.Set("Minimum spec", pair.Value.lsl);
                marginResult.Set("Maximum spec", pair.Value.usl);
                marginResult.Set("Minimum result", pair.Value.min);
                marginResult.Set("Maximum result", pair.Value.max);
                marginResult.Set("Margin limit", pair.Value.marginLimit);

                //cal_max_spec
                if (!String.IsNullOrEmpty(pair.Value.usl) && !String.IsNullOrEmpty(pair.Value.lsl))
                {
                    decimal cal_max_spec = (ConvertToDec(pair.Value.usl) - ConvertToDec(pair.Value.max)) / (ConvertToDec(pair.Value.usl) - ConvertToDec(pair.Value.lsl)) * 100;
                    marginResult.Set("cal_max_spec", cal_max_spec.ToString());
                }
                //cal_max_spec with no min limit
                if (!String.IsNullOrEmpty(pair.Value.usl) && String.IsNullOrEmpty(pair.Value.lsl) && (ConvertToDec(pair.Value.usl)!=0))
                {
                    decimal cal_max_spec = (ConvertToDec(pair.Value.usl) - ConvertToDec(pair.Value.max)) / Math.Abs(ConvertToDec(pair.Value.usl))  * 100;
                    marginResult.Set("cal_max_spec", cal_max_spec.ToString());
                }
                //cal_min_spec
                if (!String.IsNullOrEmpty(pair.Value.usl) && !String.IsNullOrEmpty(pair.Value.lsl))
                {
                    decimal cal_min_spec = (ConvertToDec(pair.Value.min) - ConvertToDec(pair.Value.lsl)) / (ConvertToDec(pair.Value.usl) - ConvertToDec(pair.Value.lsl)) * 100;
                    marginResult.Set("cal_min_spec", cal_min_spec.ToString());
                }
                //cal_min_spec with no max limit
                if (String.IsNullOrEmpty(pair.Value.usl) && !String.IsNullOrEmpty(pair.Value.lsl) && (ConvertToDec(pair.Value.lsl)!=0))
                {
                    decimal cal_min_spec = (ConvertToDec(pair.Value.min) - ConvertToDec(pair.Value.lsl))/ Math.Abs(ConvertToDec(pair.Value.lsl)) * 100;
                    marginResult.Set("cal_min_spec", cal_min_spec.ToString());
                }

                bool cal_min_specExist = !string.IsNullOrEmpty(marginResult.GetValue("cal_min_spec", "").ToString());
                bool cal_max_specExist = !string.IsNullOrEmpty(marginResult.GetValue("cal_max_spec", "").ToString());
                decimal margin = 0;
                if (cal_min_specExist) margin = ConvertToDec(marginResult.GetValue("cal_min_spec", "0").ToString());
                if (cal_max_specExist) margin = ConvertToDec(marginResult.GetValue("cal_max_spec", "0").ToString());
                if ((cal_min_specExist) && (cal_max_specExist))
                {
                    margin = Math.Min(ConvertToDec(marginResult.GetValue("cal_min_spec", "0").ToString()), ConvertToDec(marginResult.GetValue("cal_max_spec", "0").ToString()));
                }
                marginResult.Set("Margin", margin.ToString());
                if ((margin != 0) && (margin < Convert.ToDecimal(pair.Value.marginLimit))) marginResult.Set("MarginFailed", "TRUE");
                resultsList.Add(marginResult);
            }
            return resultsList;
        }

        private Decimal ConvertToDec(string val)
        {
            decimal decVal = 0;
            try
            {
                if (val.ToUpper().Contains("E")) decVal = Decimal.Parse(val, System.Globalization.NumberStyles.Float);
                else decVal = Convert.ToDecimal(val);
            }
            catch (Exception e)
            {
                return 0;
            }
            return decVal;
        }

    }

    public class ReportMargin
    {
        public string min;
        public string max;
        public string field;
        public string usl;
        public string lsl;
        public string marginLimit;

        public ReportMargin(string _field, string _usl, string _lsl, string _mlimit)
        {
            field = _field;
            usl = _usl;
            lsl = _lsl;
            marginLimit = _mlimit;

        }

    }
}