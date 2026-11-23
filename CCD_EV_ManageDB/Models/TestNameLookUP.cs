using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class TestNameLookUP
    {
        public string _id;
        public string project;
        public string table;
        public string spec;
        public Dictionary<string, string> LookUP;

        public TestNameLookUP()
        {
            LookUP = new Dictionary<string, string>();
        }

        public TestNameLookUP(string _project, string _table, string _spec)
        {
            project = _project;
            table = _table;
            spec = _spec;
            _id = project + "_" + table + "_" + spec;
            LookUP = new Dictionary<string, string>();
        }

        public TestNameLookUP(BsonDocument doc)
        {
            _id = doc.GetValue("_id", "").ToString();
            table = (doc.GetValue("table", "").ToString());
            project = (doc.GetValue("project", "").ToString());
            spec = (doc.GetValue("spec", "").ToString());
            LookUP = new Dictionary<string, string>();

            if (doc.Contains("LookUP"))
            {
                BsonArray lArray = doc.GetElement("LookUP").Value.AsBsonArray;
                foreach (BsonDocument item in lArray)
                {
                    LookUP.Add(item.GetValue("_id", "").ToString(), item.GetValue("TestName", "").ToString());
                }
            }
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", _id } };
            Doc.Add("table", table);
            Doc.Add("project", project);
            Doc.Add("spec", spec);

            BsonArray lArray = new BsonArray();
            foreach (var pair in LookUP)
            {
                BsonDocument item = new BsonDocument { { "_id", pair.Key } };
                item.Add("TestName", pair.Value);
                lArray.Add(item);
            }
            Doc.Add("LookUP", lArray);

            return Doc;
        }
    }
}