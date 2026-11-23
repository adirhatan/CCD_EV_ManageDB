using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace CCD_EV_ManageDB.Repositories
{
    public class MDBUserTableReport : MDBClient
    {
        public MDBUserTableReport()
            : base("DB_PRM", "UserTableReport")
        { }

        public Boolean SaveUserTableReport(UserTableReport Report)
        {
            BsonDocument sReport;

            sReport = new BsonDocument { { "_id", Report._id } };
            sReport.Add("name", Report.Name);
            sReport.Add("TestName", Report.TestName);
            sReport.Add("RConfig", Report.RConfig.toBsonDocument());
            sReport.Add("ResultsPath", Report.ResultsPath);
            sReport.Add("Search", Report.Search.ToBsonDocument());
            sReport.Add("useMarginLimits", Report.useMarginLimits);
            MDBSaveRec(sReport);
            return true;
        }

        public string CopyAndSaveUserTableReport(string username, UserTableReport Report, string table)
        {
            string name = Report.Name + "_copy";
            if (string.IsNullOrEmpty(table)) table = Report.RConfig.table;
            if (Report.RConfig.table != table) name = Report.Name +"_" + table + "_copy";
            
            UserTableReport CReport = new UserTableReport(username, name, table);
            CReport.RConfig = Report.RConfig;
            CReport.TestName = Report.TestName;
            CReport.RConfig.table = table;
            if (this.SaveUserTableReport(CReport)) return CReport._id;
            return null;
        }

        public UserTableReport ReadUserTableReport(string id)
        {
            UserTableReport Report = new UserTableReport();
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null)
            {
                Report._id = id;
                Report.Name = rec.GetValue("name", "").ToString();
                Report.TestName = rec.GetValue("TestName", "").ToString();
                Report.RConfig = rec.GetValue("RConfig").AsBsonDocument.BsonDocumentToReportConfig();
                Report.ResultsPath = rec.GetValue("ResultsPath", "").ToString();
                Report.Search = BsonSerializer.Deserialize<DBsearchSave>(rec.GetElement("Search").Value.ToBsonDocument());
                Report.useMarginLimits = rec.GetValue("useMarginLimits", "").ToBoolean();
            }
            return Report;
        }

        public Boolean DeleteUserTableReport(string id)
        {
            return MDBDeleteID(id);
        }

        public Boolean RenameUserReport(string id, string name)
        {
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null)
            {
                rec.Set("name", name);
                return MDBSaveRec(rec);
            }
            return false;
        }

        public MongoCursor ReadCursorAllUserTableReports(string username)
        {
            MongoCursor Cur = MDBReadAllMatchedIDRecs(username+"2");
            return Cur;
        }

        public Dictionary<string,HashSet<string>> GetDictionaryAllUsersTableReports()
        {
            Dictionary<string, HashSet<string>> UsersReports = new Dictionary<string, HashSet<string>>();
            //MongoCursor Cur = MDBReadAllMatchedIDRecs(username);
            MongoCursor Cur = collection.FindAll();
            SortByBuilder sort = new SortByBuilder(); ;
            sort.Ascending(new string[] { "_id" });
            Cur.SetSortOrder(sort);
            foreach (BsonDocument rec in Cur)
            {
                UserTableReport Report = new UserTableReport();
                Report._id = rec.GetValue("_id", "").ToString();
                Report.Name = rec.GetValue("name", "").ToString();
                Report.TestName = rec.GetValue("TestName", "").ToString();
                Report.RConfig = rec.GetValue("RConfig").AsBsonDocument.BsonDocumentToReportConfig();
                string userNameAndTable = Regex.Replace(Report._id,@"\d*","") + "_" + Report.RConfig.table;
                if (!UsersReports.ContainsKey(userNameAndTable)) UsersReports.Add(userNameAndTable, new HashSet<string>());
                UsersReports[userNameAndTable].Add(Report.Name);
            }
            return UsersReports;
        }

        public UserTableReport ReadUserTableReport(string UserName,string table,string ReportName)
        {
            UserTableReport Report = new UserTableReport();
            string query = "";
            query += "Matches;_id;/^" + UserName + "/;";
            query += "EQ;name;" + ReportName;
            MongoCursor Cur = base.MDBFind(query, "AND", "Des", "TestEnd");
            foreach (BsonDocument rec in Cur)
            {
                if (rec != null) 
                {
                    Report._id = rec.GetValue("_id", "").ToString(); 
                    Report.Name = rec.GetValue("name", "").ToString();
                    Report.TestName = rec.GetValue("TestName", "").ToString();
                    Report.RConfig = rec.GetValue("RConfig").AsBsonDocument.BsonDocumentToReportConfig();
                    Report.ResultsPath = rec.GetValue("ResultsPath", "").ToString();
                    Report.Search = BsonSerializer.Deserialize<DBsearchSave>(rec.GetElement("Search").Value.ToBsonDocument());
                    Report.useMarginLimits = rec.GetValue("useMarginLimits", "").ToBoolean();
                }
                if (Report.RConfig.table == table) return Report;
            }
            return Report;
        }
    }
}