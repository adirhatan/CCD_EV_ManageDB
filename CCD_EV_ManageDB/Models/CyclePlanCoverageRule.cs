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
    public class CyclePlanCoverageRule
    {
        public string _id { get; set; }
        public string protocol { get; set; }
        public string project { get; set; }
        public string phase { get; set; }
        public string Table { get; set; }
        public string Name { get; set; }
        public List<string> corners { get; set; }
        public List<string> ports { get; set; }
        public List<string> lanes { get; set; }
        public int trials { get; set; }
        public string SetupType { get; set; }


        public CyclePlanCoverageRule()
        {
            corners = new List<string>();
            ports = new List<string>();
            lanes = new List<string>();
        }

        public CyclePlanCoverageRule(string _protocol, string _proj, string _phase, string _Table, string _Name)
        {
            _id = _protocol + '_' + _proj + '_' + _phase + '_' + _Table + '_' + _Name;
            protocol = _protocol;
            project = _proj;
            phase = _phase;
            Table = _Table;
            Name = _Name;
            corners = new List<string>();
            ports = new List<string>();
            lanes = new List<string>();

        }

        public CyclePlanCoverageRule(string _protocol, string _proj, string _phase, string _Table, string _Name, string cornersStr, string lanesStr, string portsStr, string _trials, string _SetupType)
        {
            _id = _protocol + '_' + _proj + '_' + _phase + '_' + _Table + '_' + _Name;
            protocol = _protocol;
            project = _proj;
            phase = _phase;
            Table = _Table;
            Name = _Name;
            corners = new List<string>(cornersStr.Split(','));
            lanes = new List<string>(lanesStr.Split(','));
            ports = new List<string>(portsStr.Split(','));
            trials = Convert.ToInt32(_trials);
            SetupType = _SetupType;
        }

        public CyclePlanCoverageRule(string _protocol, string _proj, string _phase, CyclePlanCoverageRule refCPCoverageRule)
        {
            _id = _protocol + '_' + _proj + '_' + _phase + '_' + refCPCoverageRule.Table + '_' + refCPCoverageRule.Name;
            protocol = _protocol;
            project = _proj;
            phase = _phase;
            Table = refCPCoverageRule.Table;
            Name = refCPCoverageRule.Name;
            corners = new List<string>(refCPCoverageRule.corners);
            lanes = new List<string>(refCPCoverageRule.lanes);
            ports = new List<string>(refCPCoverageRule.ports);
            trials = refCPCoverageRule.trials;
            SetupType = refCPCoverageRule.SetupType;
        }
        public CyclePlanCoverageRule(BsonDocument doc)
        {
            _id = doc.GetValue("_id", "").ToString();
            protocol = (doc.GetValue("protocol", "").ToString());
            project = (doc.GetValue("project", "").ToString());
            phase = (doc.GetValue("phase", "").ToString());
            Table = (doc.GetValue("Table", "").ToString());
            Name = (doc.GetValue("Name", "").ToString());
            corners = new List<string>();
            if (doc.Contains("corners"))
            {
                BsonArray cArray = doc.GetElement("corners").Value.AsBsonArray;
                foreach (string corner in cArray)
                {
                    corners.Add(corner);
                }
            }

            ports = new List<string>();
            if (doc.Contains("ports"))
            {
                BsonArray pArray = doc.GetElement("ports").Value.AsBsonArray;
                foreach (string port in pArray)
                {
                    ports.Add(port);
                }
            }

            lanes = new List<string>();
            if (doc.Contains("lanes"))
            {
                BsonArray lArray = doc.GetElement("lanes").Value.AsBsonArray;
                foreach (string lane in lArray)
                {
                    lanes.Add(lane);
                }
            }
            trials = (doc.GetValue("trials","1").ToInt32());
            SetupType = (doc.GetValue("SetupType", "").ToString());
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", _id } };
            Doc.Add("protocol", protocol);
            Doc.Add("project", project);
            Doc.Add("phase", phase);
            Doc.Add("Table", Table);
            Doc.Add("Name", Name);
            BsonArray cArray = new BsonArray();
            foreach (string corner in corners)
            {
                cArray.Add(corner);
            }
            if (corners.Count > 0) Doc.Add("corners", cArray);

            BsonArray pArray = new BsonArray();
            foreach (string port in ports)
            {
                pArray.Add(port);
            }
            Doc.Add("ports", pArray);

            BsonArray lArray = new BsonArray();
            foreach (string lane in lanes)
            {
                lArray.Add(lane);
            }
            Doc.Add("lanes", lArray);
            Doc.Add("trials", trials);
            Doc.Add("SetupType", SetupType);
            return Doc;
        }


    }


   
}