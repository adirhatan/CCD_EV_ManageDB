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
    public class StatusReport
    {
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }

        protected Dictionary<string, Dictionary<string,string>> StatusResults;
        protected Dictionary<string, Dictionary<string, string>> resultAdvDetails;
        protected Dictionary<string, Dictionary<string, List <string>>> AdvDetailsFieldsVals;
        protected Dictionary<string, List<string>> AdvDetailsFieldsValsPermutations;
        //protected Dictionary<string,bool> CornerFailedResults;
        protected Dictionary<string, List<string>> CornerFailedResultsMeasures;
        protected Dictionary<string, List<string>> CornerFailedTjSuspicious;
        protected Dictionary<string, string> TestStandTestNames;
        protected TestNameLookUP TestNameLookUPObj;
        protected bool TestNameLookUPWasRead;


        protected string TestName;
        string corner;
        string port;
        string lane;
        string testName;
        bool JoinFieldToKey;
        public List<string> Lanes { get; set; }
        public List<string> Ports { get; set; }
        public List<string> Corners { get; set; }
        public bool detailed;
        string proj;
        string isRetimer = "false";
        string spec;
        string table;

        public StatusReport(string reportId, string _Lanes, bool _JoinFieldToKey, bool mesureSummary)
        {
            JoinFieldToKey = _JoinFieldToKey;
            StatusResults = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>();
            resultAdvDetails = new Dictionary<string, Dictionary<string, string>>();
            AdvDetailsFieldsVals = new Dictionary<string, Dictionary<string, List<string>>>();
            AdvDetailsFieldsValsPermutations = new Dictionary<string,List<string>>();
            //CornerFailedResults = new Dictionary<string, bool>();
            CornerFailedResultsMeasures = new Dictionary<string, List<string>>();
            CornerFailedTjSuspicious = new Dictionary<string, List<string>>();
            TestStandTestNames = new Dictionary<string, string>();
            UserTableReport R = (new MDBUserTableReport()).ReadUserTableReport(reportId);
            //New XML file initializtion. TODO read from reference table
            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
            detailed = false;
            if (File.Exists(Path.Combine(Path.GetDirectoryName(EmptyExcel), R.RConfig.table + "_SR.xml")))
            {
                detailed = true;
                readAdvDetailedFile(Path.Combine(Path.GetDirectoryName(EmptyExcel), R.RConfig.table + "_SR.xml"));
            }
            //Min Max importatnt values
            if (mesureSummary)
            {
                R.RConfig.FieldsSlides.Add(new FieldSlide("", "", "Temperature Tj Max", "", "", "", "", "", ""));
                R.RConfig.FieldsSlides.Add(new FieldSlide("", "", "Temperature Amb Max", "", "", "", "", "", ""));
                R.RConfig.FieldsSlides.Add(new FieldSlide("", "", "Temperature Tj Min", "", "", "", "", "", ""));
                R.RConfig.FieldsSlides.Add(new FieldSlide("", "", "Temperature Amb Min", "", "", "", "", "", ""));
                R.RConfig.FieldsSlides.Add(new FieldSlide("", "", "SVR ToSet", "", "", "", "", "", ""));
                R.RConfig.FieldsSlides.Add(new FieldSlide("", "", "SVR Max", "", "", "", "", "", ""));
                R.RConfig.FieldsSlides.Add(new FieldSlide("", "", "SVR Min", "", "", "", "", "", ""));
                R.RConfig.FieldsSlides.Add(new FieldSlide("", "", "V3_3 Max", "", "", "", "", "", ""));
                R.RConfig.FieldsSlides.Add(new FieldSlide("", "", "V3_3 Min", "", "", "", "", "", ""));
            }
            //
            foreach (FieldSlide FS in R.RConfig.FieldsSlides)
            {
                if (!JoinFieldToKey) 
                {
                    if (!StatusResults.ContainsKey(FS.test))StatusResults.Add(FS.test, new Dictionary<string,string>());
                    if (!resultAdvDetails.ContainsKey(FS.test)) resultAdvDetails.Add(FS.test, new Dictionary<string, string>());
                }
                if (JoinFieldToKey)
                {
                    if (!StatusResults.ContainsKey(FS.test + "_" + FS.column)) StatusResults.Add(FS.test + "_" + FS.column, new Dictionary<string, string>());
                    if (!resultAdvDetails.ContainsKey(FS.test + "_" + FS.column)) resultAdvDetails.Add(FS.test + "_" + FS.column, new Dictionary<string, string>());
                }
                
            }
            TestName = R.TestName;
            TitleItem Ti = R.RConfig.TitleItems[R.RConfig.TitleItems.Count - 1];
            if (Ti.content.StartsWith("Spec:")) spec = Ti.content.Replace("Spec:","").Trim();
            table = R.RConfig.table;
            TestNameLookUPWasRead = false;
            //TODO take Lanes/Ports from Report definition
            switch (_Lanes)
            {
                case "PABL01":
                    Lanes = new List<string>(new string[] { "L0", "L1" });
                    Ports = new List<string>(new string[] { "PA", "PB" });
                    break;
                case "PCDL01":
                    Lanes = new List<string>(new string[] { "L0", "L1" });
                    Ports = new List<string>(new string[] { "PC", "PD" });
                    break;
                case "PABEL01":
                    Lanes = new List<string>(new string[] { "L0", "L1" });
                    Ports = new List<string>(new string[] { "PA", "PB","PE" });
                    break;
                case "PABCDL01":
                    Lanes = new List<string>(new string[] { "L0", "L1" });
                    Ports = new List<string>(new string[] { "PA", "PB", "PC", "PD" });
                    break;
                case "PABCDEL01":
                    Lanes = new List<string>(new string[] { "L0", "L1" });
                    Ports = new List<string>(new string[] { "PA", "PB", "PC", "PD", "PE" });
                    break;
                case "PABL03":
                    Lanes = new List<string>(new string[] { "L0", "L1", "L2", "L3" });
                    Ports = new List<string>(new string[] { "PA", "PB" });
                    break;
                case "PAB0L03":
                    Lanes = new List<string>(new string[] { "L0", "L1", "L2", "L3" });
                    Ports = new List<string>(new string[] { "PA", "PB","SRC0" });
                    break;
                case "PABCDL03":
                    Lanes = new List<string>(new string[] { "L0", "L1", "L2", "L3" });
                    Ports = new List<string>(new string[] { "PA", "PB", "PC", "PD" });
                    break;
                case "PABCDEL03":
                    Lanes = new List<string>(new string[] { "L0", "L1", "L2", "L3" });
                    Ports = new List<string>(new string[] { "PA", "PB", "PC", "PD", "PE" });
                    break;
                case "PABLNA":
                    Lanes = new List<string>(new string[] { "NA" });
                    Ports = new List<string>(new string[] { "PA", "PB" });
                    break;
                case "PA_SA_DB_SB_DLNA":
                    Lanes = new List<string>(new string[] { "NA" });
                    Ports = new List<string>(new string[] { "PA_S", "PA_D", "PB_S", "PB_D"});
                    break;
                case "PAB0LNA":
                    Lanes = new List<string>(new string[] { "NA" });
                    Ports = new List<string>(new string[] { "PA", "PB", "SRC0" });
                    break;
                case "PABELNA":
                    Lanes = new List<string>(new string[] { "NA" });
                    Ports = new List<string>(new string[] { "PA", "PB", "PE" });
                    break;
                case "PABCDELNA":
                    Lanes = new List<string>(new string[] { "NA" });
                    Ports = new List<string>(new string[] {"PA", "PB", "PC", "PD", "PE" });
                    break;
                case "NPL03":
                    Lanes = new List<string>(new string[] { "L0", "L1", "L2", "L3" });
                    Ports = new List<string>();
                    break;
                case "NPL0":
                    Lanes = new List<string>(new string[] { "L0"});
                    Ports = new List<string>();
                    break;
                case "NPL07":
                    Lanes = new List<string>(new string[] { "L0", "L1", "L2", "L3", "L4", "L5", "L6", "L7" });
                    Ports = new List<string>();
                    break;
                default:
                    break;
            }


            Corners = new List<string>(new string[] { "HVHT", "HVLT", "NOMN", "LVHT", "LVLT" });
            foreach (KeyValuePair<string, Dictionary<string, string>> entry in StatusResults)
            {
                foreach (string _corner in Corners)
                {
                    if (Ports.Count > 0)
                    {
                        foreach (string _port in Ports)
                        {
                            foreach (string _lane in Lanes) 
                            {
                                entry.Value.Add(_corner + "-" + _port + "-" + _lane, "NA");
                                if (AdvDetailsFieldsValsPermutations.ContainsKey(entry.Key))
                                {
                                    foreach (string permutation in AdvDetailsFieldsValsPermutations[entry.Key])
                                    {
                                        resultAdvDetails[entry.Key].Add(_corner + "-" + _port + "-" + _lane + "-" + permutation, "NA");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string _lane in Lanes)
                        {
                            entry.Value.Add(_corner + "-" + _lane, "NA");
                            if (AdvDetailsFieldsValsPermutations.ContainsKey(entry.Key))
                            {
                                foreach (string permutation in AdvDetailsFieldsValsPermutations[entry.Key])
                                {
                                    resultAdvDetails[entry.Key].Add(_corner + "-" + _lane + "-" + permutation, "NA");
                                }
                            }
                        }
                    }
                }

            }

        }
        public void AnalyzeResults(List<BsonDocument> Results)
        {
            //if (JoinFieldToKey)
            //{
            //TODO: go over Results, and copy results taht is used more than once, remove specific fields so the result will be used for the next test:
            // Case1 20G EyeMask Test_Region1Hits,Case1 20G EyeMask Test_Region1Hits,Case1 20G EyeMask Test_Region1Hits,Case1 20G EyeMask Test_EyeHeightMin,
            // Case1 20G EyeMask Test_EyeWidthMin - duplicate "Case1 20G EyeMask Test" result and remove Region1Hits,Region2Hits... this way results will be
            // analayzed for all report lines.
            // Maybe need to prepare the information about the report duplicate needs before?
            //}
            foreach (BsonDocument result in Results)
            {
                if (string.IsNullOrEmpty(proj))
                {
                    proj = result.GetValue("ProjectName","").ToString();
                    isRetimer = "false";
                    if ( (result.GetValue("TestName","").ToString().ToUpper().Contains("RETIMER")) ||
                         (result.GetValue("SpecTestName","").ToString().ToUpper().Contains("RETIMER")) ||
                         (result.GetValue("Mode","").ToString().ToUpper().Contains("RETIMER")) )
                    {
                        isRetimer = "true";
                    }
                }
                corner = result.GetValue("Corner", "").ToString().Substring(0, 4).ToUpper();
                port = result.GetValue("Port", "").ToString().ToUpper();
                lane = result.GetValue("Lane", "").ToString().ToUpper();
                testName = result.GetValue(TestName, "").ToString();

                //new for Custom Execution
                if (!TestStandTestNames.ContainsKey(testName)) TestStandTestNames.Add(testName, result.GetValue("TestName", "").ToString());

                if (JoinFieldToKey)
                {
                    foreach (string _key in StatusResults.Keys)
                    {
                        if ((_key.StartsWith(testName)) && (result.GetValue(_key.Remove(0, testName.Length + 1),"")!=""))
                        {
                            testName = _key;
                            break;
                        }
                    }
                }
                string key = corner + "-" + port + "-" + lane;
                if (Ports.Count == 0) key = corner + "-" + lane;

                //?????
                if (!(StatusResults.ContainsKey(testName))) continue;
                if (!(StatusResults[testName].ContainsKey(key))) continue;

                if (StatusResults[testName][key] == "NA")
                {
                    StatusResults[testName][key] = result.GetValue("TestStatus", "NA").ToString().ToUpper();
                    string _margin = result.GetValue("Margin", 100).ToString();
                    double _marginD;
                    if ((!string.IsNullOrWhiteSpace(_margin)) && Double.TryParse(_margin, out _marginD) &&
                        (Convert.ToDouble(_margin) >= 0) && 
                        (Convert.ToDouble(_margin) <= 2)) StatusResults[testName][key] = "MARGINAL";
                }
                else
                {
                    if (StatusResults[testName][key] != result.GetValue("TestStatus", "NA").ToString().ToUpper())
                    {
                        StatusResults[testName][key] = "MULTI";
                    }
                }


                //Min Max importatnt values
                if (StatusResults.ContainsKey("Temperature Tj Max"))
                {
                    if ((StatusResults["Temperature Tj Max"][key] == "NA") ||
                        ((Convert.ToDouble(StatusResults["Temperature Tj Max"][key])) < result.GetValue("Tjunc", "0").ToDouble()))
                        StatusResults["Temperature Tj Max"][key] = result.GetValue("Tjunc", "0").ToString();
                    if ((StatusResults["Temperature Tj Min"][key] == "NA") ||
                        ((Convert.ToDouble(StatusResults["Temperature Tj Min"][key])) > result.GetValue("Tjunc", "0").ToDouble()))
                        StatusResults["Temperature Tj Min"][key] = result.GetValue("Tjunc", "0").ToString();
                    if ((StatusResults["Temperature Amb Max"][key] == "NA") ||
                        ((Convert.ToDouble(StatusResults["Temperature Amb Max"][key])) < result.GetValue("Tamb", "0").ToDouble()))
                        StatusResults["Temperature Amb Max"][key] = result.GetValue("Tamb", "0").ToString();
                    if ((StatusResults["Temperature Amb Min"][key] == "NA") ||
                        ((Convert.ToDouble(StatusResults["Temperature Amb Min"][key])) > result.GetValue("Tamb", "0").ToDouble()))
                        StatusResults["Temperature Amb Min"][key] = result.GetValue("Tamb", "0").ToString();
                    if ((StatusResults["SVR ToSet"][key] == "NA") ||
                        ((Convert.ToDouble(StatusResults["SVR ToSet"][key])) < result.GetValue("SVRVoltageToSet", "0").ToDouble()))
                        StatusResults["SVR ToSet"][key] = result.GetValue("SVRVoltageToSet", "0").ToString();
                    if ((StatusResults["SVR Max"][key] == "NA") ||
                        ((Convert.ToDouble(StatusResults["SVR Max"][key])) < result.GetValue("V1_05", "0").ToDouble()))
                        StatusResults["SVR Max"][key] = result.GetValue("V1_05", "0").ToString();
                    if ((StatusResults["SVR Min"][key] == "NA") ||
                        ((Convert.ToDouble(StatusResults["SVR Min"][key])) > result.GetValue("V1_05", "0").ToDouble()))
                        StatusResults["SVR Min"][key] = result.GetValue("V1_05", "0").ToString();
                    if ((StatusResults["V3_3 Max"][key] == "NA") ||
                        ((Convert.ToDouble(StatusResults["V3_3 Max"][key])) < result.GetValue("V3_3", "0").ToDouble()))
                        StatusResults["V3_3 Max"][key] = result.GetValue("V3_3", "0").ToString();
                    if ((StatusResults["V3_3 Min"][key] == "NA") ||
                        ((Convert.ToDouble(StatusResults["V3_3 Min"][key])) > result.GetValue("V3_3", "0").ToDouble()))
                        StatusResults["V3_3 Min"][key] = result.GetValue("V3_3", "0").ToString();
                }
                //

                //TODO - add uniqeness validation of Skew/phase etc...??

                if (!(AdvDetailsFieldsVals.ContainsKey(testName))) continue;
                string advKey = "";
                foreach (string field in AdvDetailsFieldsVals[testName].Keys) advKey += result.GetValue(field, "NA").ToString() + " ";
                if (!(resultAdvDetails[testName].ContainsKey(key + "-" + advKey.TrimEnd()))) continue;
                if (resultAdvDetails[testName][key + "-" + advKey.TrimEnd()] == "NA")
                {
                    resultAdvDetails[testName][key + "-" + advKey.TrimEnd()] = result.GetValue("TestStatus", "NA").ToString().ToUpper();
                }
                else
                {
                    if (resultAdvDetails[testName][key + "-" + advKey.TrimEnd()] != result.GetValue("TestStatus", "NA").ToString().ToUpper())
                    {
                        resultAdvDetails[testName][key + "-" + advKey.TrimEnd()] = "MULTI";
                    }
                }
            }
            //new; mark corner failed: build dictionary
            BuildMarkCornerFailedResults();

        }

        void BuildMarkCornerFailedResults()
        {
            if (!StatusResults.ContainsKey("Temperature Tj Max")) return;

            calcCornerfailSingeltonFactory cCornerfailFactory = new calcCornerfailSingeltonFactory();
            calcCornerfail calcObj = null;
            //calcCornerfailTmpr  calcObjTmpr = new calcCornerfailTmpr();
            //calcCornerfailSVR  calcObjSVR = new calcCornerfailSVR();
            //calcCornerfailV3  calcObjV3 = new calcCornerfailV3();


            //TODO: read thresholds from configuration
            Dictionary<string, Dictionary<string, double>> CornerFailedThresholds = new Dictionary<string,Dictionary<string,double>>();
            Dictionary<string, Corner> MarkCornerFailConf = this.readMarkCornerFailConf(proj, isRetimer);
            
            List<string> Measures = new List<string>(new string[] { "Temperature Tj Max","Temperature Tj Min","Temperature Amb Max","Temperature Amb Min","SVR Max","SVR Min","V3_3 Max","V3_3 Min"});
            //List<string> Measures = new List<string>(new string[] { "Temperature Tj Max", "Temperature Tj Min", "Temperature Amb Max", "Temperature Amb Min", "V3_3 Max", "V3_3 Min" });
            foreach (string _corner in Corners)
            {
                if (!CornerFailedThresholds.ContainsKey(_corner)) CornerFailedThresholds.Add(_corner, new Dictionary<string, double>());
                foreach (string measure in Measures) 
                {
                    calcObj = cCornerfailFactory.getCalcCornerfail(measure);
                    if (MarkCornerFailConf.ContainsKey(_corner))
                    {
                        CornerFailedThresholds[_corner].Add(measure, calcObj.calcThreshold(measure, MarkCornerFailConf[_corner]));
                    }
                    else
                    {
                        CornerFailedThresholds[_corner].Add(measure, 999);
                    }
                }
            }

            foreach (string measure in Measures)
            {
                calcObj = cCornerfailFactory.getCalcCornerfail(measure);
                var measureResultDictionary = StatusResults[measure];
                foreach (KeyValuePair<string, string> CornerVal in measureResultDictionary)
                {
                    double goldenVal = CornerFailedThresholds[CornerVal.Key.Substring(0, 4)][measure];
                    //SVR from result
                    if (measure.Contains("SVR"))
                    {
                        string resSvr = "0";
                        if (StatusResults["SVR ToSet"].ContainsKey(CornerVal.Key)) resSvr = StatusResults["SVR ToSet"][CornerVal.Key];
                        if (resSvr == "NA") resSvr = "0";
                        Corner tmpCorner = new Corner();
                        tmpCorner.svr = Convert.ToDouble(resSvr);
                        goldenVal = calcObj.calcThreshold(measure, tmpCorner);
                    }
                    //create flag to skip AMB check in Hot corner
                    //For Hot corner, check AMB only if: a)there was Tj failure or b)there was Tj suspicios (more than 100 delta)
                    bool skipAMBcheck = false;
                    if (CornerVal.Key.Substring(2, 2).Equals("HT") && (measure.Contains("Amb"))) skipAMBcheck = true;
                    if (CornerVal.Key.Substring(2, 2).Equals("HT") && (CornerFailedResultsMeasures.ContainsKey(CornerVal.Key)))
                    {
                        foreach (string _msr in CornerFailedResultsMeasures[CornerVal.Key])
                        {
                            if (_msr.Contains("Tj ")) skipAMBcheck = false;
                            break;
                        }
                    }
                    if ((CornerVal.Value != "NA") && (CornerVal.Key.Substring(2, 2).Equals("HT")) && (measure.Contains("Tj ")))
                    {
                        if (calcObj.isTJSuspicious(measure, goldenVal, Convert.ToDouble(CornerVal.Value)))
                        {
                            if (!CornerFailedTjSuspicious.ContainsKey(CornerVal.Key)) CornerFailedTjSuspicious.Add(CornerVal.Key, new List<string>());
                            CornerFailedTjSuspicious[CornerVal.Key].Add(measure);
                        }
                    }
                    if (CornerVal.Key.Substring(2, 2).Equals("HT") && (measure.Contains("Amb")) && (CornerFailedTjSuspicious.ContainsKey(CornerVal.Key))) skipAMBcheck = false;
                    //END OF CREATING FLAG
                    //if ((CornerVal.Value!="NA") && (calcObj.isFailForValue(measure, goldenVal, Convert.ToDouble(CornerVal.Value))))
                    if ((!skipAMBcheck) && (CornerVal.Value != "NA") && (calcObj.isFailForValue(measure, goldenVal, Convert.ToDouble(CornerVal.Value))))
                    {
                        if (!CornerFailedResultsMeasures.ContainsKey(CornerVal.Key)) CornerFailedResultsMeasures.Add(CornerVal.Key, new List<string>());
                        CornerFailedResultsMeasures[CornerVal.Key].Add(measure);
                    }
                }
            }

        }

        Dictionary<string, Corner> readMarkCornerFailConf(string proj, string isRetimer)
        {
            Dictionary<string, Corner> corners = new Dictionary<string, Corner>();
            
            try
            {
                XDocument doc = XDocument.Load(Path.Combine(ServerDir, "EVData", "MarkCornersFailConf.xml"));
                XElement dbElem = doc.Root;
                foreach (XElement CornersConf in dbElem.Elements("CornersConf"))
                {
                    if ((CornersConf.Attribute("proj").Value != proj) || (CornersConf.Attribute("isRetimer").Value != isRetimer)) continue;
                    foreach (XElement confCorner in CornersConf.Elements("corner"))
                    {
                        Corner tmpCorner = new Corner();
                        tmpCorner.name = confCorner.Element("name").Value.Trim();
                        tmpCorner.svr = Convert.ToDouble(confCorner.Element("svr").Value.Trim());
                        tmpCorner.v3 = Convert.ToDouble(confCorner.Element("v3").Value.Trim());
                        tmpCorner.tmpr = Convert.ToDouble(confCorner.Element("tmpr").Value.Trim());
                        corners.Add(tmpCorner.name,tmpCorner);
                    }
                }
            }
            catch (Exception e)
            {
                //
                return corners;
            }
            return corners;
        }

        public bool IsCornerFail(string _key)
        {
            //if (CornerFailedResults.ContainsKey(_key)) return CornerFailedResults[_key];
            if (CornerFailedResultsMeasures.ContainsKey(_key)) return true;
            return false;
        }
        public bool IsCornerFailMeasure(string _key,string _measure)
        {
            if ((CornerFailedResultsMeasures.ContainsKey(_key)) && (CornerFailedResultsMeasures[_key].Contains(_measure))) return true;
            return false;
        }
        public bool IsCornerFailForCSR(string _CSRkey, string Port)
        {
            string _key;
            List<string> PortsList = new List<string>();
            if (Port == "NA") PortsList = this.Ports;
            else PortsList.Add(Port);
            foreach (string _port in PortsList)
            {
                foreach (string _lane in this.Lanes)
                {
                    _key = _CSRkey.Split('-')[1] + "-" +  _port + "-" + _lane;
                    //
                    //Special fix for PCIE, they do not use Ports in SR, just lanes
                    if (_port.Equals(_lane)) _key = _CSRkey.Split('-')[1] + "-" + _port;
                    //
                    if (CornerFailedResultsMeasures.ContainsKey(_key)) return true;
                }
            }
            return false;
        }
        public  Dictionary<string, Dictionary<string, string>> GetStatusResults()
        {
            return StatusResults;
        }

        public Dictionary<string, Dictionary<string, string>> GetAdvResultsDetails()
        {
            return resultAdvDetails;
        }

        public string GetTestStandTestName(string key)
        {
            if (!TestNameLookUPWasRead)
            {
                TestNameLookUPWasRead = true;
                BsonDocument tmp = (new MDBTestNameLookUP()).Read(proj, table, spec);
                if (tmp != null) TestNameLookUPObj = new TestNameLookUP(tmp);
            }
            if (TestStandTestNames.ContainsKey(key)) return TestStandTestNames[key];
            else
            {
                if ((TestNameLookUPObj != null) && TestNameLookUPObj.LookUP.ContainsKey(key)) return TestNameLookUPObj.LookUP[key];
            }
            return "Missing TestName";
        }
            

        public string getLinkToResults(string _testName, string cornerName, BsonDocument FirstResult, string col, List<string> SeperatorFields,string _db, string baseQuery)
        {
            string link = "";
            //String strPathAndQuery = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
            //String strUrl = System.Web.HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");
            string strUrl = System.Configuration.ConfigurationManager.AppSettings["webHost"];
            string testName = _testName;
            if (JoinFieldToKey)
            {
                int _ind = testName.LastIndexOf('_');
                testName = testName.Substring(0,_ind);
            }
            //link = strUrl + "ManageDB/SearchTMP?db=test&col=" + col + "&Query=EQ;SessionStart;" + FirstResult.GetValue("SessionStart", "") + ";EQ;" + TestName + ";" + testName;
            if (_db == System.Configuration.ConfigurationManager.AppSettings["DB_PRM"])
            {
                link = strUrl + "ManageDB/SearchPRM?db=" + _db + "&col=" + col + "&Query=" + baseQuery + ";";
            }
            else
            {
                link = strUrl + "ManageDB/SearchTMP?db=" + _db + "&col=" + col + "&Query=" + baseQuery + ";";
            }
            foreach (string field in SeperatorFields)
            {
                link += "EQ;" + field + ";" + FirstResult.GetValue(field, "") + ";";
            }
            link += "EQ;" + TestName + ";" + testName;
            if (JoinFieldToKey)
            {
                link += ";ExistNotEmpty;" + _testName.Remove(0, testName.Length + 1) + ";";
            }
            if (Ports.Count > 0)
            {
                //name format:_corner + "-" + _port + "-" + _lane
                string[] terms = cornerName.Split('-');
                if (terms.Length == 3) link += ";Matches;Corner;" + terms[0] + ";Matches;Port;/" + terms[1] + "/i;" + "Matches;Lane;/" + terms[2] + "/i";
            }
            else
            {
                //name format:_corner "-" + _lane
                string[] terms = cornerName.Split('-');
                if (terms.Length == 2) link += ";Matches;Corner;" + terms[0] + ";Matches;Lane;/" + terms[1] + "/i";
            }
            return link;
        }

        public string getLinkToMissingDetailedResults(string _testName, string cornerName, BsonDocument FirstResult, string col, List<string> SeperatorFields, string _db,string permsLink)
        {
            string link = "";
            string testDetails = "";
            String strPathAndQuery = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
            String strUrl = System.Web.HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");
            link = strUrl + "ManageDB/StatusReportDisplayMissDetails?permutations=" + permsLink;
            if (AdvDetailsFieldsVals.ContainsKey(_testName))
            {
                foreach (string field in AdvDetailsFieldsVals[_testName].Keys)
                {
                    link += "&field=" + field;
                }
            }
            testDetails += "Test name:" + _testName + " "; 
            foreach (string field in SeperatorFields)
            {
                testDetails += field + ";" + FirstResult.GetValue(field, "") + "; ";
            }
            link += "&testDetails="+ testDetails;
            return link;
        }

        private void readAdvDetailedFile(string filePath)
        {
            //    //Reads XML and produces the following: (TODO: read from reference/report in DB)
            //    AdvDetailsFieldsVals.Add(" Total Jitter Test (Low BitRate)", new Dictionary<string, List<string>>());
            //    AdvDetailsFieldsVals[" Total Jitter Test (Low BitRate)"].Add("Level", new List<string>(new string[] { "Swing 0", "Swing 1", "Swing 2" }));
            //    AdvDetailsFieldsVals[" Total Jitter Test (Low BitRate)"].Add("BitRate", new List<string>(new string[] { "1.62 Gbps" }));
            //    AdvDetailsFieldsValsPermutations.Add(" Total Jitter Test (Low BitRate)", new List<string>(new string[] { "Swing 0 1.62 Gbps", "Swing 1 1.62 Gbps", "Swing 2 1.62 Gbps" }));

            Dictionary<string, Dictionary<string, List<string>>> AdvDetailsFieldsValsFilter = new Dictionary<string, Dictionary<string, List<string>>>();
            XDocument doc = XDocument.Load(filePath);
            //XElement dbElem = doc.Root.Element(db);
            XElement dbElem = doc.Root;
            foreach (XElement testElem in dbElem.Elements("Test"))
            {
                AdvDetailsFieldsVals.Add(testElem.Attribute("name").Value, new Dictionary<string, List<string>>());
                foreach (XElement fieldElem in testElem.Elements("Field"))
                {
                    AdvDetailsFieldsVals[testElem.Attribute("name").Value].Add(fieldElem.Attribute("name").Value, new List<string>());
                    //IndexFields.Add(fieldElem.Attribute("name").Value);
                    foreach (XElement valElem in fieldElem.Elements("Value"))
                    {
                        AdvDetailsFieldsVals[testElem.Attribute("name").Value][fieldElem.Attribute("name").Value].Add(valElem.Value);
                    }
                }
                foreach (XElement FilterElem in testElem.Elements("Filter"))
                {
                    if (!AdvDetailsFieldsValsFilter.ContainsKey(testElem.Attribute("name").Value)) 
                            AdvDetailsFieldsValsFilter.Add(testElem.Attribute("name").Value, new Dictionary<string, List<string>>());

                    AdvDetailsFieldsValsFilter[testElem.Attribute("name").Value].Add(FilterElem.Attribute("name").Value, new List<string>());
                    foreach (XElement valElem in FilterElem.Elements("Value"))
                    {
                        AdvDetailsFieldsValsFilter[testElem.Attribute("name").Value][FilterElem.Attribute("name").Value].Add(valElem.Value);
                    }
                }
            }
            //build permutations
            foreach (var a in AdvDetailsFieldsVals)
            {
                int fieldsNum = a.Value.Count();
                List<int> permHelper = new List<int>(new int[fieldsNum]);
                permHelper.Insert(fieldsNum - 1, 1);
                permHelper[fieldsNum-1] = 1;
                List<int> fieldsValsNumArr = new List<int>();
                int permutationsNum = 1;
                foreach (var field in a.Value)
                {
                    fieldsValsNumArr.Add(field.Value.Count());
                    permutationsNum*=field.Value.Count();
                }
                for (int cnt = fieldsNum -1; cnt > 0; cnt--)
                {
                    permHelper[cnt-1] = permHelper[cnt] * fieldsValsNumArr[cnt];
                }
                List <string> permutations = new List<string>(new string [permutationsNum]);
                for (int i = 0; i < permutationsNum;i++) permutations[i] = "";
                for (int i = 0; i < fieldsNum; i++)
                {
                    int cnt = 0;
                    int ind = 0;
                    for (int j = 0; j < permutationsNum; j++)
                    {
                        permutations[j] += a.Value[a.Value.Keys.ElementAt(i)].ElementAt(ind) + " ";
                        cnt++;
                        if (cnt >= permHelper[i])
                        {
                            cnt = 0;
                            ind++;
                        }
                        if (ind >= fieldsValsNumArr[i]) ind = 0;
                    }
                }
                for (int i = 0; i < permutationsNum; i++) permutations[i] = permutations[i].TrimEnd();
                //Filter
                if (AdvDetailsFieldsValsFilter.ContainsKey(a.Key))
                {
                    List<string> tPermutations = new List<string>();
                    foreach (string perm in permutations)
                    {
                        bool valid = true;
                        foreach (var filter in AdvDetailsFieldsValsFilter[a.Key])
                        {
                            int cnt=0;
                            foreach (string val in filter.Value) if (perm.Contains(val)) cnt++;
                            if (cnt == filter.Value.Count()) valid = false;
                        }
                        if (valid) tPermutations.Add(perm);
                    }
                    permutations = tPermutations;
                }
                AdvDetailsFieldsValsPermutations.Add(a.Key, permutations);
            }

        }
    }

    public class Corner
    {
        public string name;
        public double tmpr;
        public double svr;
        public double v3;
    }

    public class calcCornerfailSingeltonFactory
    {
        private static calcCornerfailTmpr calcCornerfailTmprObj;
        private static calcCornerfailSVR calcCornerfailSVRObj;
        private static calcCornerfailV3 calcCornerfailV3Obj;

        public calcCornerfailSingeltonFactory()
        {
            calcCornerfailTmprObj = new calcCornerfailTmpr();
            calcCornerfailSVRObj = new calcCornerfailSVR();
            calcCornerfailV3Obj = new calcCornerfailV3();
        }
        public calcCornerfail getCalcCornerfail(string _measure)
        {
            if (_measure.Contains("Temperature")) return calcCornerfailTmprObj;
            if (_measure.Contains("SVR")) return calcCornerfailSVRObj;
            if (_measure.Contains("V3_3")) return calcCornerfailV3Obj;
            return null;
        }
    }
    public abstract class calcCornerfail
    {
        public virtual bool isFailForValue(string _measure, double goldenVal, double resVal)
        {
            if (goldenVal == 999) return false;
            if ((_measure.Contains("Min")) && (goldenVal > resVal)) return true;
            if ((_measure.Contains("Max")) && (resVal > goldenVal)) return true;
            return false;
        }
        public virtual double calcThreshold(string measure, Corner cornerConf)
        {
            return 999;
        }
        public virtual bool isTJSuspicious(string _measure, double goldenVal, double resVal)
        {
            if (!(_measure.Contains("Tj ")) || (goldenVal == 999)) return false;
            if ((_measure.Contains("Tj ")) && (Math.Abs(goldenVal - resVal) > 100)) return true;
            return false;
        }
    }

    class calcCornerfailTmpr: calcCornerfail
    {
        public override double calcThreshold(string measure, Corner cornerConf)
        {
            if ((measure.Contains("Amb")) && (cornerConf.name.Contains("HT"))) //return 999; in order to ignore...
            {
                if (measure.Contains("Min")) return cornerConf.tmpr + 15;
                if (measure.Contains("Max")) return cornerConf.tmpr + 45;
            }
            if ((measure.Contains("Tj ")) && (cornerConf.name.Contains("LT"))) return 999;
            if ((measure.Contains("Tj ")) && (cornerConf.name.Contains("NOMN"))) return 999;
            if (measure.Contains("Min")) return cornerConf.tmpr - 5;
            if (measure.Contains("Max")) return cornerConf.tmpr + 5;
            return 999;
        }

        public override bool isFailForValue(string _measure, double goldenVal, double resVal)
        {
            if (goldenVal == 999) return false;
            //Case of JTAG failuer, measered SVR instead of Diode; Amb threshold will give indication
            if ((_measure.Contains("Tj ")) && (Math.Abs(goldenVal-resVal)>100)) return false;
            if ((_measure.Contains("Min")) && (goldenVal > resVal)) return true;
            if ((_measure.Contains("Tj ")) && (_measure.Contains("Max")) ) return false;
            if ((_measure.Contains("Max")) && (resVal > goldenVal)) return true;
            return false;
        }

    }

    class calcCornerfailSVR : calcCornerfail
    {
        public override double calcThreshold(string measure, Corner cornerConf)
        {
            if (cornerConf.svr == 0) return 999; //couldn't read SVR ToSet from results - ignore
            // +- 0.04
            if (measure.Contains("Min")) return cornerConf.svr - 0.04;
            if (measure.Contains("Max")) return cornerConf.svr + 0.04;
            return 999;
        }
    }

    class calcCornerfailV3 : calcCornerfail
    {
        public override double calcThreshold(string measure, Corner cornerConf)
        {
            //+-5%
            if (measure.Contains("Min")) return cornerConf.v3 * 95 / 100;
            if (measure.Contains("Max")) return cornerConf.v3 * 105 / 100;
            return 999;
        }
    }

}