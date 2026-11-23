using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using System.Text.RegularExpressions;

namespace CCD_EV_ManageDB.Repositories
{
    public class MDBCompReportLines : MDBClient
    {
        public MDBCompReportLines()
            : base("DB_CMP", "CompReportLines")
        { }

        public Boolean SaveCompReportLine(DBTmpl Line)
        {
            base.MDBSaveRec(Line.toBsonDocument());
            return true;
        }

        public Boolean DeleteCompReportLine(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public DBTmpl ReadCompReportLine(string id)
        {
            DBTmpl Line = new DBTmpl();
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) Line = new DBTmpl(rec);
            return Line;
        }



        public List<DBTmpl> ReadAllCompReportLines()
        {
            List<DBTmpl> lines = new List<DBTmpl>();
            MongoCursor Cur;

            Cur = base.MDBReadAllMatchedIDRecs("/^./");
            foreach (BsonDocument doc in Cur)
            {
                lines.Add(new DBTmpl(doc));
            }
            return lines;

        }

        public List<DBTmpl> ReadMatchedIDQeuryCompReportLines(string idQuery)
        {
            List<DBTmpl> lines = new List<DBTmpl>();
            MongoCursor Cur;

            Cur = base.MDBReadAllMatchedIDRecs(idQuery);
            foreach (BsonDocument doc in Cur)
            {
                lines.Add(new DBTmpl(doc));
            }
            return lines;

        }

        public Dictionary<string, DBTmpl> GetCompReportLinesDictionary()
        {
            Dictionary<string, DBTmpl> LinesDictionary = new Dictionary<string, DBTmpl>();
            List<DBTmpl> lines = ReadAllCompReportLines();
            foreach (DBTmpl l in lines) LinesDictionary.Add(l.name, l);
            return LinesDictionary;
        }

        public bool CopyLinesPA2PB()
        {
            bool success = true;
            MongoCursor Cur;

            Cur = base.MDBReadAllMatchedIDRecs("/^(DP-|DPpp-|USB31-RX-|USB20-TX-).*-PA$/");
            foreach (BsonDocument doc in Cur)
            {
                DBTmpl Line = new DBTmpl(doc);
                //1. replicate id with -PB suffix
                //2. create new  DBTmpl with new id and change suffix ";Matches;Port;/PA/i" to ";Matches;Port;/PB/i" in query field
                Line.name = Regex.Replace(Line.name,@"^(.*)(-PA)$","$1"+"-PB");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/PA/i", @"Matches;Port;/PB/i");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/pa/i", @"Matches;Port;/pb/i");
                SaveCompReportLine(Line);
            }
            //In DP replicate PA tp SRC0
            Cur = base.MDBReadAllMatchedIDRecs("/^(DP-).*-PA$/");
            foreach (BsonDocument doc in Cur)
            {
                DBTmpl Line = new DBTmpl(doc);
                //1. replicate id with -SRC0 suffix
                //2. create new  DBTmpl with new id and change suffix ";Matches;Port;/PA/i" to ";Matches;Port;/PB/i" in query field
                Line.name = Regex.Replace(Line.name, @"^(.*)(-PA)$", "$1" + "-SRC0");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/PA/i", @"Matches;Port;/SRC0/i");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/pa/i", @"Matches;Port;/SRC0/i");
                SaveCompReportLine(Line);
            }
            Cur = base.MDBReadAllMatchedIDRecs("/^(USB31-TX-).*-PA$/");
            foreach (BsonDocument doc in Cur)
            {
                DBTmpl Line = new DBTmpl(doc);
                //1. replicate id with -PB suffix
                //2. create new  DBTmpl with new id and change suffix ";Matches;Port;/PA/i" to ";Matches;Port;/PB/i" in query field
                //3. replace results to l2-l3
                Line.name = Regex.Replace(Line.name, @"^(.*)(-PA)$", "$1" + "-PB");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/PA/i", @"Matches;Port;/PB/i");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/pa/i", @"Matches;Port;/pb/i");
                Line.results = "L2-L3";
                SaveCompReportLine(Line);
            }
            Cur = base.MDBReadAllMatchedIDRecs("/^TBT_TX_.*_1P$/");
            foreach (BsonDocument doc in Cur)
            {
                DBTmpl Line = new DBTmpl(doc);
                //1. replicate id with -1P suffix
                //2. create new  DBTmpl with new id and change results field from "L0-L1" to "L0-L3"
                Line.name = Regex.Replace(Line.name, @"^(.*)(_1P)$", "$1" + "_2P");
                Line.results = "L0-L3";
                SaveCompReportLine(Line);
            }
            Cur = base.MDBReadAllMatchedIDRecs("/^(TBT_TX_).*_PA$/");
            foreach (BsonDocument doc in Cur)
            {
                DBTmpl Line = new DBTmpl(doc);
                //1. replicate id with -PB suffix
                //2. create new  DBTmpl with new id and change suffix ";Matches;Port;/PA/i" to ";Matches;Port;/PB/i" in query field
                Line.name = Regex.Replace(Line.name, @"^(.*)(_PA)$", "$1" + "_PB");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/PA/i", @"Matches;Port;/PB/i");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/pa/i", @"Matches;Port;/pb/i");
                SaveCompReportLine(Line);
            }
            Cur = base.MDBReadAllMatchedIDRecs("/^(TBT-RX-|TBT-RL-).*-1P$/");
            foreach (BsonDocument doc in Cur)
            {
                DBTmpl Line = new DBTmpl(doc);
                //1. replicate id with -1P suffix
                //2. create new  DBTmpl with new id and change results field from "L0-L1" to "L0-L3"
                Line.name = Regex.Replace(Line.name, @"^(.*)(-1P)$", "$1" + "-2P");
                Line.results = "L0-L3";
                SaveCompReportLine(Line);
            }
            Cur = base.MDBReadAllMatchedIDRecs("/^(TBT-RX-|TBT-RL-).*-PA$/");
            foreach (BsonDocument doc in Cur)
            {
                DBTmpl Line = new DBTmpl(doc);
                //1. replicate id with -PB suffix
                //2. create new  DBTmpl with new id and change suffix ";Matches;Port;/PA/i" to ";Matches;Port;/PB/i" in query field
                Line.name = Regex.Replace(Line.name, @"^(.*)(-PA)$", "$1" + "-PB");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/PA/i", @"Matches;Port;/PB/i");
                Line.query = Regex.Replace(Line.query, @"Matches;Port;/pa/i", @"Matches;Port;/pb/i");
                SaveCompReportLine(Line);
            }
            return success;
        }
    }

    public class DBTmpl
    {
        public string name { get; set; }
        public string table { get; set; }
        public string query { get; set; }
        public string field1 { get; set; }
        public string field2 { get; set; }
        public string field3 { get; set; }
        public string disp1 { get; set; }
        public string disp2 { get; set; }
        public string disp3 { get; set; }
        public string results { get; set; }

        public DBTmpl()
        { }

        public DBTmpl(string lName, string _query, string tableName, string Field1, string Field2, string Field3, string _disp1, string _disp2, string _disp3,string _results)
        {
            name = lName;
            table = tableName;
            query = _query;
            field1 = Field1;
            field2 = Field2;
            field3 = Field3;
            disp1 = _disp1;
            disp2 = _disp2;
            disp3 = _disp3;
            results = _results;
        }

        public DBTmpl(BsonDocument doc)
        { 
            name = doc.GetValue("_id","").ToString();
            table = doc.GetValue("table", "").ToString();
            query = doc.GetValue("query","").ToString();
            field1 = doc.GetValue("field1","").ToString();
            field2 = doc.GetValue("field2","").ToString();
            field3 = doc.GetValue("field3", "").ToString();
            disp1 = doc.GetValue("disp1","").ToString();
            disp2 = doc.GetValue("disp2","").ToString();
            disp3 = doc.GetValue("disp3", "").ToString();
            results = doc.GetValue("results","").ToString();
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument sLine = new BsonDocument { { "_id", name } };
            sLine.Add("query", query);
            sLine.Add("table", table);
            sLine.Add("field1", field1);
            sLine.Add("field2", field2);
            sLine.Add("field3", field3);
            sLine.Add("disp1", disp1);
            sLine.Add("disp2", disp2);
            sLine.Add("disp3", disp3);
            sLine.Add("results", results);
            return sLine;
        }
    }
}