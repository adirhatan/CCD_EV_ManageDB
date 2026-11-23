using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using CCD_EV_ManageDB.Models;
using CCD_EV_ManageDB.Repositories;
using CCD_EV_ManageDB.ActionFilters;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace CCD_EV_ManageDB.Models
{
    public class HotCornerStatsHandler
    {

        public void LoadCornerSetTimesFromExecutions(out string message)
        {
            //Take last date from "CornerSetTimes" table?
            Decimal NewStatsCnt = 0;
            MDBExecutions mdbExecutions = new MDBExecutions("DB_TMP");
            MDBStationResults mdbStationResults = new MDBStationResults("DB_TMP", "HotCornerStats");
            BsonDocument lastResult = mdbStationResults.MDBFindOne("Matches;_id;/^./", "AND", "DES", "_id");
            string lastResultSession = "20200404_000000";
            if (lastResult != null) lastResultSession = lastResult.GetValue("SessionStart", "20200404_000000").ToString();
            lastResultSession = lastResultSession.Substring(0, 8);
            MongoCursor Cur = mdbExecutions.MDBFind("GT;SessionStart;" + lastResultSession + ";EQ;Status;Info;Matches;Comment;Hot time measure", "AND", "ASC", "_id");
            foreach (BsonDocument executionResult in Cur)
            {
                string session = executionResult.GetValue("SessionStart", "").ToString();
                string executionResult_id = executionResult.GetValue("_id", "").ToString();
                string table = Regex.Replace(executionResult_id, @"_ccd.*", "");
                string corner = executionResult.GetValue("Corner", "").ToString();
                string statsKey = session + "_" + table + "_" + corner;
                BsonDocument StatsRec = mdbStationResults.MDBFindOne("EQ;_id;" + statsKey, "AND", "DES", "_id");
                if (StatsRec == null)
                {
                    StatsRec = BuildNewStatsRec(executionResult, statsKey, table);
                    NewStatsCnt++;
                }
                else StatsRec = updateStatsRec(executionResult, StatsRec);
                mdbStationResults.Save(StatsRec);
            }
            message = "Load corner set times from Executions id done," + NewStatsCnt + " records were found";
        }

        private BsonDocument BuildNewStatsRec(BsonDocument executionResult, string _id, string table)
        {
            //List <string> fieldsList = new List<string>(new string[] { "TestName", "ProjectName", "BoardNumber", "Phase", "Skew" });
            TimeSpan measure = calcTimeDiff(executionResult.GetValue("TimeStampStart", "").ToString(), executionResult.GetValue("TimeStampEnd", "").ToString());
            Double time = measure.Minutes + (Double)measure.Seconds / 60;
            // CornerSetFullTime, CornerSetTmprTime, TjTuneFullTime, TjTuneTmprTime
            string comment = executionResult.GetValue("Comment", "").ToString();
            string TjTune = executionResult.GetValue("TjTune", "").ToString();
            string FieldName = "CornerSetTmprTime";
            if (comment.Contains("INFRA_CornerSet Hot time measure")) FieldName = "CornerSetFullTime";
            if (comment.Contains("GEN_Temperature_TjTune Hot time measure")) FieldName = "TjTuneFullTime";
            if (TjTune.Contains("True")) FieldName = "TjTuneTmprTime";
            BsonDocument result = executionResult.DeepClone() as BsonDocument;
            result.Set("_id", _id);
            result.Add("table", table);
            result.Add(FieldName, time);
            result.Remove("TimeStampStart");
            result.Remove("TimeStampEnd");
            //result.Remove("TimeStamp");
            result.Remove("TestStart");
            result.Remove("TjTune");
            result.Remove("Comment");
            result.Remove("Status");
            return result;
        }

        private BsonDocument updateStatsRec(BsonDocument executionResult, BsonDocument StatsRec)
        {
            TimeSpan measure = calcTimeDiff(executionResult.GetValue("TimeStampStart", "").ToString(), executionResult.GetValue("TimeStampEnd", "").ToString());
            Double time = measure.Minutes + (Double)measure.Seconds / 60;
            string comment = executionResult.GetValue("Comment", "").ToString();
            string TjTune = executionResult.GetValue("TjTune", "").ToString();
            string FieldName = "CornerSetTmprTime";
            if (comment.Contains("INFRA_CornerSet Hot time measure")) FieldName = "CornerSetFullTime";
            if (comment.Contains("GEN_Temperature_TjTune Hot time measure")) FieldName = "TjTuneFullTime";
            if (TjTune.Contains("True")) FieldName = "TjTuneTmprTime";
            BsonDocument result = StatsRec.DeepClone() as BsonDocument;
            result.Set(FieldName, time);
            //CornerPlusTjTune
            if ((result.Contains("CornerSetFullTime")) && (result.Contains("TjTuneFullTime"))) result.Set("CornerPlusTjTune", result.GetValue("CornerSetFullTime").ToDouble() + result.GetValue("TjTuneFullTime").ToDouble());
            return result;
        }

        private TimeSpan calcTimeDiff(string TimeStampStart, string TimeStampEnd)
        {
            DateTime start = new DateTime(Convert.ToInt32(TimeStampStart.Substring(0, 4)), Convert.ToInt32(TimeStampStart.Substring(4, 2)), Convert.ToInt32(TimeStampStart.Substring(6, 2)), Convert.ToInt32(TimeStampStart.Substring(9, 2)), Convert.ToInt32(TimeStampStart.Substring(11, 2)), Convert.ToInt32(TimeStampStart.Substring(13, 2)));
            DateTime end = new DateTime(Convert.ToInt32(TimeStampEnd.Substring(0, 4)), Convert.ToInt32(TimeStampEnd.Substring(4, 2)), Convert.ToInt32(TimeStampEnd.Substring(6, 2)), Convert.ToInt32(TimeStampEnd.Substring(9, 2)), Convert.ToInt32(TimeStampEnd.Substring(11, 2)), Convert.ToInt32(TimeStampEnd.Substring(13, 2)));
            return (end.Subtract(start));
        }
    }
}