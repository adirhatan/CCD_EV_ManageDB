using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Globalization;
using CCD_EV_ManageDB.Models;
using CCD_EV_ManageDB.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class IndicatorResults
    {
        public string _id { get; set; }
        public string table { get; set; }
        public string project { get; set; }
        public string phase { get; set; }
        public string spec { get; set; }
        public string wws { get; set; }
        public string wn { get; set; }
        public List<IndWeekSum> weeks { get; set; }

        public IndicatorResults()
        {
            weeks = new List<IndWeekSum>();
        }

        public IndicatorResults(Indicator Ind)
        {
            _id = Ind._id;
            table = Ind.table;
            project = Ind.project;
            phase = Ind.phase;
            spec = Ind.spec;
            wws = Ind.wws;
            wn = Ind.wn;
            weeks = new List<IndWeekSum>();
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = new DateTime(Convert.ToInt32(wws.Substring(0, 4)), Convert.ToInt32(wws.Substring(4, 2)), Convert.ToInt32(wws.Substring(6, 2)));
            Calendar cal = dfi.Calendar;

            for (int weekCnt = 0; weekCnt < Convert.ToInt32(wn); weekCnt++)
            {
                weeks.Add(new IndWeekSum(cal.GetWeekOfYear(date1.AddDays(Convert.ToDouble(weekCnt * 7)), dfi.CalendarWeekRule, dfi.FirstDayOfWeek).ToString(), date1.AddDays(Convert.ToDouble(weekCnt * 7)).ToString("yyyyMMdd")));
                weeks[weekCnt].plannedTests = Ind.GetWeekPlannedOrigTests(weekCnt);
                weeks[weekCnt].plannedTestsAc = weeks[weekCnt].plannedTests;
            }
        }
    }

    public class IndWeekSum
    {
        public string Name { get; set; }
        public string Sdate { get; set; }
        //No. of planned tests - weekly, this week only
        public long plannedTests { get; set; }
        //No. of planned tests - accumulative , sum all planned
        public long plannedTestsAc { get; set; }
        //No. of tests executed  - weekly, this week only
        public long executedTests { get; set; }
        //No. of tests executed  - accumulative, sum all executed
        public long executedTestsAc { get; set; }
        //No. of passed tests  - accumulative, sum all executed passed (query?)
        public long passedTestsAc { get; set; }
        //No. of failed tests  - accumulative
        public long failedTestsAc { get; set; }
        //No. of blocked tests  - accumulative, ??
        public long blockedTestsAc { get; set; }
        //No. of pending tests  - accumulative, planned - executed
        public long pendingTestsAc { get; set; }
        //Coverage % - executes VS planned
        public double CoverageAc { get; set; }

        public IndWeekSum(string _Name, string _Sdate)
        {
            Name = _Name;
            Sdate = CCDUtilities.getAdjacentSundayDate(_Sdate);
        }
    }
}