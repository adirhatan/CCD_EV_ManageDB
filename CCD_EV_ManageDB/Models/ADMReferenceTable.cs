using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq; 
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using CCD_EV_ManageDB.Repositories;
using OfficeOpenXml;

namespace CCD_EV_ManageDB.Models
{
    public class ADMReferenceTable
    {
        public string _id { get; set; }
        public string Protocol { get; set; }
        public string PVersion { get; set; }
        public ReportConfig RConfig { get; set; }

        public ADMReferenceTable()
        {
            RConfig = new ReportConfig("");
        }

        public ADMReferenceTable(string _Protocol, string _PVersion)
        {
            _id = _Protocol + "_" + _PVersion;
            Protocol = _Protocol;
            PVersion = _PVersion;
            RConfig = new ReportConfig(_Protocol);
        }




        public bool LoadExcelLimits(string excelFileName)
        {
           try
           {
                FileInfo newFile = new FileInfo(excelFileName);
                using (ExcelPackage package = new ExcelPackage(newFile))
                {
                    foreach (ExcelWorksheet displayWorksheet in package.Workbook.Worksheets)
                    {
                        int rowCnt = 1;
                        while (rowCnt < displayWorksheet.Dimension.End.Row)
                        {
                                rowCnt++;
                                RConfig.FieldsSlides.Add(new FieldSlide(General.ReportSlideType.limits.ToString(), "", Convert.ToString(displayWorksheet.Cells[rowCnt, 1].Value), Convert.ToString(displayWorksheet.Cells[rowCnt, 2].Value), "", Convert.ToString(displayWorksheet.Cells[rowCnt, 3].Value),
                                    Convert.ToString(displayWorksheet.Cells[rowCnt, 4].Value), null, Convert.ToString(displayWorksheet.Cells[rowCnt, 5].Value), Convert.ToString(displayWorksheet.Cells[rowCnt, 6].Value)));
                        }
                    }
                }
           }
           catch (Exception e)
           {
               return (false);
           }
            return (true);
        }

        public bool CreateSpecExcelFromMappingFile(string table, string OutfilePath, ref string errMsg)
        {
            //    //Reads XML and produces the following: (TODO: read from reference/report in DB)
            //    AdvDetailsFieldsVals.Add(" Total Jitter Test (Low BitRate)", new Dictionary<string, List<string>>());
            //    AdvDetailsFieldsVals[" Total Jitter Test (Low BitRate)"].Add("Level", new List<string>(new string[] { "Swing 0", "Swing 1", "Swing 2" }));
            //    AdvDetailsFieldsVals[" Total Jitter Test (Low BitRate)"].Add("BitRate", new List<string>(new string[] { "1.62 Gbps" }));
            //    AdvDetailsFieldsValsPermutations.Add(" Total Jitter Test (Low BitRate)", new List<string>(new string[] { "Swing 0 1.62 Gbps", "Swing 1 1.62 Gbps", "Swing 2 1.62 Gbps" }));

            Dictionary<string, Dictionary<string, List<string>>> AdvDetailsFieldsValsFilter = new Dictionary<string, Dictionary<string, List<string>>>();
            Dictionary<string, Dictionary<string, List <string>>> AdvDetailsFieldsVals = new Dictionary<string,Dictionary<string,List<string>>>();
            Dictionary<string, List<string>> AdvDetailsFieldsValsPermutations = new Dictionary<string,List<string>>();
            Dictionary<string, string> SpecName = new Dictionary<string, string>();
            errMsg = "";
            string MapFilePath = new MDBAppRemoteMap().ReadAppRemoteMapFilePath(table);
            if (!File.Exists(MapFilePath))
            {
                errMsg = "Could not find map file:" + MapFilePath;
                return false;
            }
            XDocument doc = XDocument.Load(new MDBAppRemoteMap().ReadAppRemoteMapFilePath(table));
            //XElement dbElem = doc.Root.Element(db);
            XElement dbElem = doc.Root;
            foreach (XElement testElem in dbElem.Elements("Test"))
            {
                foreach (XElement SpecNameElem in testElem.Elements("SpecTestName"))
                {
                    SpecName.Add(testElem.Attribute("name").Value,SpecNameElem.Value);
                }
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
                permHelper[fieldsNum - 1] = 1;
                List<int> fieldsValsNumArr = new List<int>();
                int permutationsNum = 1;
                foreach (var field in a.Value)
                {
                    fieldsValsNumArr.Add(field.Value.Count());
                    permutationsNum *= field.Value.Count();
                }
                for (int cnt = fieldsNum - 1; cnt > 0; cnt--)
                {
                    permHelper[cnt - 1] = permHelper[cnt] * fieldsValsNumArr[cnt];
                }
                List<string> permutations = new List<string>(new string[permutationsNum]);
                for (int i = 0; i < permutationsNum; i++) permutations[i] = "";
                for (int i = 0; i < fieldsNum; i++)
                {
                    int cnt = 0;
                    int ind = 0;
                    for (int j = 0; j < permutationsNum; j++)
                    {
                        if (string.IsNullOrEmpty(permutations[j])) permutations[j] += a.Value[a.Value.Keys.ElementAt(i)].ElementAt(ind);
                        else permutations[j] += "_" + a.Value[a.Value.Keys.ElementAt(i)].ElementAt(ind);
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
                            int cnt = 0;
                            foreach (string val in filter.Value) if (perm.Contains(val)) cnt++;
                            if (cnt == filter.Value.Count()) valid = false;
                        }
                        if (valid) tPermutations.Add(perm);
                    }
                    permutations = tPermutations;
                }
                AdvDetailsFieldsValsPermutations.Add(a.Key, permutations);
            }
            //build Excel
            try
            {
                FileInfo newFile = new FileInfo(OutfilePath);
                using (ExcelPackage package = new ExcelPackage(newFile))
                {
                    //ExcelWorksheet displayWorksheet = package.Workbook.Worksheets.Add("Sheet1");
                    foreach (ExcelWorksheet displayWorksheet in package.Workbook.Worksheets)
                    {
                        int rowCnt = 1;
                        foreach (var pair in AdvDetailsFieldsValsPermutations)
                        {
                            foreach (string perm in pair.Value)
                            {
                                rowCnt++;
                                displayWorksheet.Cells[rowCnt, 1].Value = SpecName[pair.Key] + "_" + perm;
                            }
                        }
                    }
                    package.Save();
                }
            }
            catch (Exception e)
            {
                errMsg = e.Message;
                return (false);
            }
            return (true);

        }

        public List<string> GetSpecTestNames()
        {
            List<string> SpecTestNames = new List<string>();
            foreach (FieldSlide slide in this.RConfig.FieldsSlides) SpecTestNames.Add(slide.test);
            return SpecTestNames;
        }

        public FieldSlide GetSpecTestNameLimits(string _SpecTedtName)
        {
            return RConfig.GetFieldSlideBySpec(_SpecTedtName);
        }

    }

    public static class BsonDocumentADMExtension
    {
        // This is the extension method. 
        // The first parameter takes the "this" modifier
        // and specifies the type for which the method is defined. 
        public static ADMReferenceTable BsonDocumentToADMReferenceTable(this BsonDocument doc)
        {
            ADMReferenceTable Ref = new ADMReferenceTable();

            Ref._id = doc.GetValue("_id", "").ToString(); ;
            Ref.RConfig = doc.GetValue("RConfig").AsBsonDocument.BsonDocumentToReportConfig();
            Ref.Protocol = doc.GetValue("Protocol", "").ToString();
            Ref.PVersion = doc.GetValue("PVersion", "").ToString();
            return Ref;
        }
    }

}

