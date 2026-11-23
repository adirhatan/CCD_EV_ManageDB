using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using CCD_EV_ManageDB.Repositories;
using System.Reflection;

namespace CCD_EV_ManageDB.Models
{
    public class CyclePlanSpecSubSet
    {
        public string _id { get; set; }
        public string protocol { get; set; }
        public string project { get; set; }
        public string phase { get; set; }
        public string Table { get; set; }
        public string Name { get; set; }
        public string Spec { get; set; }
        public List<string> SpecTestNames { get; set; }
        public string allTests { get; set; }


        public CyclePlanSpecSubSet()
        {
            SpecTestNames = new List<string>();
        }

        public CyclePlanSpecSubSet(string _protocol, string _proj, string _phase, string _Table, string _Name, string _Spec)
        {
            _id = _protocol + '_' + _proj + '_' + _phase + '_' + _Table + '_' + _Name;
            protocol = _protocol;
            project = _proj;
            phase = _phase;
            Table = _Table;
            Name = _Name;
            Spec = _Spec;
            SpecTestNames = new List<string>();

        }

        public CyclePlanSpecSubSet(BsonDocument doc)
        {
            _id = doc.GetValue("_id", "").ToString();
            protocol = (doc.GetValue("protocol", "").ToString());
            project = (doc.GetValue("project", "").ToString());
            phase = (doc.GetValue("phase", "").ToString());
            Table = (doc.GetValue("Table", "").ToString());
            Name = (doc.GetValue("Name", "").ToString());
            Spec = (doc.GetValue("Spec", "").ToString());
            BsonArray lArray = doc.GetElement("SpecTestNames").Value.AsBsonArray;
            SpecTestNames = new List<string>();
            foreach (BsonValue val in lArray) SpecTestNames.Add(val.ToString());
            allTests = (doc.GetValue("allTests", "").ToString());
        }

        public CyclePlanSpecSubSet(string _protocol, string _proj, string _phase, CyclePlanSpecSubSet refCPSpecSubSet)
        {
            _id = _protocol + '_' + _proj + '_' + _phase + '_' + refCPSpecSubSet.Table + '_' + refCPSpecSubSet.Name;
            protocol = _protocol;
            project = _proj;
            phase = _phase;
            Table = refCPSpecSubSet.Table;
            Name = refCPSpecSubSet.Name;
            Spec = refCPSpecSubSet.Spec;
            SpecTestNames = new List<string>(refCPSpecSubSet.SpecTestNames);
            allTests = refCPSpecSubSet.allTests;
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", _id } };
            Doc.Add("protocol", protocol);
            Doc.Add("project", project);
            Doc.Add("phase", phase);
            Doc.Add("Table", Table);
            Doc.Add("Name", Name);
            Doc.Add("Spec", Spec);
            BsonArray lArray = new BsonArray(SpecTestNames);
            Doc.Add("SpecTestNames", lArray);
            Doc.Add("allTests", allTests);
            return Doc;
        }


    }


   
}