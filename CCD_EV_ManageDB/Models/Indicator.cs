using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq; 
using System.Web;
using System.Globalization;
using CCD_EV_ManageDB.Helpers;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class Indicator
    {
        public string _id { get; set; }
        public string table { get; set; }
        public string project { get; set; }
        public string phase { get; set; }
        public string spec { get; set; }
        public string wws { get; set; }
        public string wn { get; set; }
        public string lanes { get; set; }

        public List<IndTest> tests { get; set; }
        public List<string> NVMs { get; set; }

        public Indicator()
        {
            tests = new List<IndTest>();
            NVMs = new List<string>();
        }

        public Indicator(string _table, string _project, string _phase, ADMReferenceTable _spec,string _wws, string _wn, string _lanes)
        {
            _id = _table + "_" + _project + "_" + _phase;
            table = _table;
            project = _project;
            phase = _phase;
            spec = _spec._id;
            wws = CCDUtilities.getAdjacentSundayDate(_wws);
            wn = _wn;
            lanes = _lanes;
            tests = new List<IndTest>();
            foreach (FieldSlide fs in _spec.RConfig.FieldsSlides)
            {
                tests.Add(new IndTest(fs.test, fs.column, 0, wws, wn));
            }
            NVMs = new List<string>();
        }


        public Indicator(BsonDocument doc)
        {
            _id = (doc.GetValue("_id", "").ToString());
            table = (doc.GetValue("table", "").ToString());
            project = (doc.GetValue("project", "").ToString());
            phase = (doc.GetValue("phase", "").ToString());
            spec = (doc.GetValue("spec", "").ToString());
            wws = (doc.GetValue("wws", "").ToString());
            wn = (doc.GetValue("wn", "").ToString());
            lanes = (doc.GetValue("lanes", "").ToString());
            tests = new List<IndTest>();
            if (doc.Contains("Tests"))
            {
                BsonArray uArray = doc.GetElement("Tests").Value.AsBsonArray;
                foreach (BsonDocument test in uArray)
                {
                    //TODO: "BsonDocumentIndTest"
                    IndTest indTest = new IndTest(test);
                    tests.Add(indTest);
                }
            }

            NVMs = new List<string>();
            if (doc.Contains("NVMs"))
            {
                BsonArray nArray = doc.GetElement("NVMs").Value.AsBsonArray;
                foreach (string NVM in nArray)
                {
                    NVMs.Add(NVM);
                }
            }

        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument IndDoc = new BsonDocument { { "_id", _id } };
            IndDoc.Add("table", table);
            IndDoc.Add("project", project);
            IndDoc.Add("phase", phase);
            IndDoc.Add("spec", spec);
            IndDoc.Add("wws", wws);
            IndDoc.Add("wn", wn);
            IndDoc.Add("lanes", lanes);
            BsonArray uArray = new BsonArray();
            foreach (IndTest test in tests)
            {
                uArray.Add(test.toBsonDocument());
            }
            IndDoc.Add("Tests", uArray);
            BsonArray nArray = new BsonArray();

            foreach (string NVM in NVMs)
            {
                nArray.Add(NVM);
            }
            IndDoc.Add("NVMs", nArray);

            return IndDoc;
        }

        public bool AddWeeks(int wn)
        {
            WorkWeek ww = tests[0].WorkWeeks[tests[0].WorkWeeks.Count - 1];
            DateTime date1 = new DateTime(Convert.ToInt32(ww.Sdate.Substring(0, 4)), Convert.ToInt32(ww.Sdate.Substring(4, 2)), Convert.ToInt32(ww.Sdate.Substring(6, 2)));
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            Calendar cal = dfi.Calendar;
            foreach (IndTest test in tests)
            {
                for (int i = 1; i <= wn; i++)
                {
                    test.WorkWeeks.Add(new WorkWeek((cal.GetWeekOfYear(date1.AddDays(Convert.ToDouble(i * 7)), dfi.CalendarWeekRule, dfi.FirstDayOfWeek).ToString()), (date1.AddDays(Convert.ToDouble(i * 7)).ToString("yyyyMMdd"))));
                }
                for (int i = 0; i > wn; i--)
                {
                    test.WorkWeeks.RemoveAt(test.WorkWeeks.Count - 1);
                }
            }
            this.wn = ((Convert.ToInt32(this.wn))+wn).ToString();
            return true;
        }

        public long GetWeekPlannedOrigTests(int week)
        {
            long PlannedTest = 0L;
            foreach (IndTest test in this.tests) PlannedTest += test.WorkWeeks[week].PlannedOrig;
            return PlannedTest;
        }

        public bool UpdateIndicatorSpec(ADMReferenceTable _spec)
        {
            //In case of old indicator, do not change it
            DateTime wwe = new DateTime(Convert.ToInt32(this.wws.Substring(0, 4)), Convert.ToInt32(this.wws.Substring(4, 2)), Convert.ToInt32(this.wws.Substring(6, 2)));
            if (DateTime.Now>wwe.AddDays(Convert.ToDouble(7 * Convert.ToInt32(this.wn)))) return false;

            bool updated = false;
            List<String> IndTestsNames = new List<String>();
            List<String> SpecTestsNames = new List<String>();
            foreach (IndTest tmpIndTest in tests) IndTestsNames.Add(tmpIndTest.SpecTestName);

            // Check a case where test was added to Spec
            foreach (FieldSlide fs in _spec.RConfig.FieldsSlides)
            {
                SpecTestsNames.Add(fs.test);
                if (!IndTestsNames.Contains(fs.test))
                {
                    tests.Add(new IndTest(fs.test, fs.column, 0, wws, wn));
                    updated = true;
                }
            }
            // Check a case were test was removed from Spec
            foreach (String IndTestsName in IndTestsNames)
            {
                if (!SpecTestsNames.Contains(IndTestsName))
                {
                    List <IndTest> tmpIndTests = new List<IndTest>();
                    foreach (IndTest tmpIndTest in tests) if (tmpIndTest.SpecTestName != IndTestsName) tmpIndTests.Add(tmpIndTest);
                    tests = tmpIndTests;
                    updated = true;
                }
            }
            return updated;
        }

        public Dictionary<string, string> getSpecTestNamesSum()
        {
            Dictionary<string, string> SpecTestNamesSum = new Dictionary<string,string>();
            int sumAll = 0;
            foreach (IndTest indtest in this.tests)
            {
                int sum = 0;
                foreach (WorkWeek ww in indtest.WorkWeeks) sum += ww.PlannedOrig;
                SpecTestNamesSum.Add(indtest.SpecTestName,sum.ToString());
                sumAll += sum;
            }
            SpecTestNamesSum.Add("sum", sumAll.ToString());
            return SpecTestNamesSum;
        }

    }
    public class IndTest
    {
        public string SpecTestName { get; set; }
        public string Field { get; set; }
        public string Comment { get; set; }
        public int UnitsNum { get; set; }
        public int Corners { get; set; }
        public List<WorkWeek> WorkWeeks;

        public IndTest(string _SpecTestName, string _Field, int _UnitsNum, string wws, string wn)
        {
            SpecTestName = _SpecTestName;
            Field = _Field;
            UnitsNum = _UnitsNum;
            WorkWeeks = new List<WorkWeek>();
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = new DateTime(Convert.ToInt32(wws.Substring(0, 4)), Convert.ToInt32(wws.Substring(4, 2)), Convert.ToInt32(wws.Substring(6, 2)));
            Calendar cal = dfi.Calendar;
            for (int weekCnt = 0; weekCnt < Convert.ToInt32(wn); weekCnt++)
            {
                WorkWeeks.Add(new WorkWeek(cal.GetWeekOfYear(date1.AddDays(Convert.ToDouble(weekCnt * 7)), dfi.CalendarWeekRule, dfi.FirstDayOfWeek).ToString(), date1.AddDays(Convert.ToDouble(weekCnt * 7)).ToString("yyyyMMdd")));
            }

        }

        public IndTest(BsonDocument Doc)
        {
            SpecTestName = Doc.GetValue("SpecTestName","").ToString();
            Field = Doc.GetValue("Field","").ToString();
            Comment = Doc.GetValue("Comment", "").ToString();
            UnitsNum = Doc.GetValue("UnitsNum", 0).ToInt32();
            WorkWeeks = new List<WorkWeek>();
            if (Doc.Contains("WorkWeeks"))
            {
                BsonArray WWArray = Doc.GetElement("WorkWeeks").Value.AsBsonArray;
                foreach (BsonDocument ww in WWArray)
                {
                    WorkWeeks.Add(new WorkWeek(ww));
                }
            }
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument ();
            Doc.Add("SpecTestName", SpecTestName);
            Doc.Add("Field", Field);
            Doc.Add("Comment", Comment);
            Doc.Add("UnitsNum", UnitsNum);
            Doc.Add("Corners", Corners);
            BsonArray WWArray = new BsonArray();
            foreach (WorkWeek ww in WorkWeeks)
            {
                WWArray.Add(ww.ToBsonDocument());
            }
            Doc.Add("WorkWeeks", WWArray);
            return Doc;
        }
    }
    public class WorkWeek
    {
        public string Name { get; set; }
        public string Sdate { get; set; }
        public int PlannedOrig { get; set; }
        public int PlannedUpd { get; set; }
        public long Executed { get; set; }
        public long Passed { get; set; }
        public bool Blocked { get; set; }

        public WorkWeek(string _Name,string _Sdate)
        {
            Name = _Name;
            Sdate = CCDUtilities.getAdjacentSundayDate(_Sdate);
            //Calendar myCal = CultureInfo.InvariantCulture.Calendar;
            //switch (myCal.GetDayOfWeek(new DateTime(Convert.ToInt32(_Sdate.Substring(0,4)),Convert.ToInt32(_Sdate.Substring(4,2)),Convert.ToInt32(_Sdate.Substring(6,2)))))
            //{
            //    case DayOfWeek.Sunday:
            //        Sdate = _Sdate;
            //        break;
            //    case DayOfWeek.Monday:
            //        Sdate = (Convert.ToInt32(_Sdate)-1).ToString();
            //        break;
            //    case DayOfWeek.Tuesday:
            //        Sdate = (Convert.ToInt32(_Sdate)-2).ToString();
            //        break;
            //    case DayOfWeek.Wednesday:
            //        Sdate = (Convert.ToInt32(_Sdate)-3).ToString();
            //        break;
            //    case DayOfWeek.Thursday:
            //        Sdate = (Convert.ToInt32(_Sdate)-4).ToString();
            //        break;
            //    case DayOfWeek.Friday:
            //        Sdate = (Convert.ToInt32(_Sdate)-5).ToString();
            //        break;
            //    case DayOfWeek.Saturday:
            //        Sdate = (Convert.ToInt32(_Sdate)-6).ToString();
            //        break;
            //}
        }

        public WorkWeek(BsonDocument Doc)
        {
            Name = Doc.GetValue("Name", "").ToString();
            Sdate = Doc.GetValue("Sdate", "").ToString();
            PlannedOrig = Doc.GetValue("PlannedOrig", 0).ToInt32();
            PlannedUpd = Doc.GetValue("PlannedUpd", 0).ToInt32();
            Blocked = Doc.GetValue("Blocked", false).ToBoolean();

        }
    }
    public static class BsonDocumentIndicatorExtension
    {
        // This is the extension method. 
        // The first parameter takes the "this" modifier
        // and specifies the type for which the method is defined. 
        public static Indicator BsonDocumentToIndicator(this BsonDocument doc)
        {
            Indicator Ind = new Indicator(doc);
            return Ind;
        }
    }



}