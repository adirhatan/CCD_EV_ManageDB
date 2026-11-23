using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace CCD_EV_ManageDB.Repositories
{
    public class MDBExecutions : MDBClient
    {
        public MDBExecutions(string db)
            : base(db, "Executions")
        { }

        public BsonDocument ReadLastSysExecutionForTable(string table, string host)
        {
            table = table.Replace("+", @"\+");
            BsonDocument Cur = this.MDBFindOne("Matches;_id;/" + table + "_" + host + "/;In;Status;Start,Finished,Error", "AND", "DES", "TimeStamp");
            return Cur;
        }

        public BsonDocument ReadLastUserExecutionForTable(string table, string host)
        {
            table = table.Replace("+", @"\+");
            BsonDocument Cur = this.MDBFindOne("Matches;_id;/" + table + "_" + host + "/;In;Status;INFO,WARNING", "AND", "DES", "TimeStamp");
            return Cur;
        }

        public MongoCursor ReadCursorForSession(string table, string session)
        {
            MongoCursor ExecCur = null;
            table = table.Replace("+", @"\+");
            ExecCur = this.MDBFind("Matches;_id;/" + table + "/;EQ;SessionStart;" + session, "AND", "DES", "_id");
            return ExecCur;
        }

        public MongoCursor ReadCursorForSessions(string table, string sessions)
        {
            MongoCursor ExecCur = null;
            table = table.Replace("+", @"\+");
            ExecCur = this.MDBFind("Matches;_id;/" + table + "/;In;SessionStart;" + sessions, "AND", "DES", "_id");
            return ExecCur;
        }

        public MongoCursor ReadCursorLastSession(string table, string host)
        {
            string session = "";
            MongoCursor ExecCur = null;
            BsonDocument curExecutionitem = this.ReadLastSysExecutionForTable(table, host);
            if (curExecutionitem != null) session = curExecutionitem.GetElement("SessionStart").Value.ToString();
            table = table.Replace("+",@"\+");
            ExecCur = this.MDBFind("Matches;_id;/" + table + "_" + host + "/;EQ;SessionStart;" + session, "AND", "DES", "_id");
            return ExecCur;
        }

        public BsonDocument ReadLastSysExecutionForBoard(string Project, string Phase, string BoardName)
        {
            BsonDocument Cur = this.MDBFindOne("EQ;ProjectName;" + Project + ";EQ;Phase;" + Phase + ";EQ;BoardNumber;" + BoardName + ";In;Status;Start,Finished,Error", "AND", "DES", "TimeStamp");
            return Cur;
        }
    }
}