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
    public class CyclePlanMetaData
    {
        public string _id { get; set; }
        public string protocol { get; set; }
        public string project { get; set; }
        public string phase { get; set; }
        public Dictionary<string, CPTableMetaData> IndicatorsMeta { get; set; } // Dictionary of table;Spec.id,eesAvgTime
        public string wws { get; set; }
        public string wn { get; set; }


        public CyclePlanMetaData()
        {
            IndicatorsMeta = new Dictionary<string, CPTableMetaData>();
        }

        public CyclePlanMetaData(string _protocol, string _proj, string _phase)
        {
            _id = _protocol + '_' + _proj + '_' + _phase;
            protocol = _protocol;
            project = _proj;
            phase = _phase;
            IndicatorsMeta = new Dictionary<string, CPTableMetaData>();
        }

        public CyclePlanMetaData(BsonDocument doc)
        {
            _id = doc.GetValue("_id", "").ToString();
            protocol = (doc.GetValue("protocol", "").ToString());
            project = (doc.GetValue("project", "").ToString());
            phase = (doc.GetValue("phase", "").ToString());

            IndicatorsMeta = new Dictionary<string, CPTableMetaData>();
            if (doc.Contains("IndicatorsMeta"))
            {
                BsonArray iArray = doc.GetElement("IndicatorsMeta").Value.AsBsonArray;
                foreach (BsonDocument metaDoc in iArray)
                {
                    CPTableMetaData tmpCPTableMetaData = new CPTableMetaData(metaDoc);
                    IndicatorsMeta.Add(metaDoc.GetValue("key").ToString(), tmpCPTableMetaData);
                }
            }
            wws = (doc.GetValue("wws", "").ToString());
            wn = (doc.GetValue("wn", "").ToString());

        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", _id } };
            Doc.Add("protocol", protocol);
            Doc.Add("project", project);
            Doc.Add("phase", phase);
            BsonArray iArray = new BsonArray();
            foreach (var IndicatorMeta in IndicatorsMeta)
            {
                BsonDocument tmpIndicatorsMeta = IndicatorMeta.Value.toBsonDocument();
                tmpIndicatorsMeta.Set("key", IndicatorMeta.Key);
                iArray.Add(tmpIndicatorsMeta);
            }
            Doc.Add("IndicatorsMeta", iArray);
            Doc.Add("wws", wws);
            Doc.Add("wn", wn);

            return Doc;
        }

        public List<string> GetBoardTypesList(string table)
        {
            CPTableMetaData CPTableMetaDataObj = null;
            if (String.IsNullOrEmpty(table)) CPTableMetaDataObj = IndicatorsMeta.First().Value;
            else CPTableMetaDataObj = IndicatorsMeta[table];
            return CPTableMetaDataObj.BoardTypes;
        }

        public List<string> GetSetUpTypesList(string table)
        {
            CPTableMetaData CPTableMetaDataObj = null;
            if (String.IsNullOrEmpty(table)) CPTableMetaDataObj = IndicatorsMeta.First().Value;
            else CPTableMetaDataObj = IndicatorsMeta[table];
            return CPTableMetaDataObj.SetupTypes;
        }

        public bool CompareTablesAndSpecs(CyclePlanMetaData refCPMetaData)
        {
            //In case ref contains other table or differernt spec returns false 
            //In case source contains another tabe returns true (will not make wong logic for copy ref content of sets/rulles/units etc)
            foreach (var pair in refCPMetaData.IndicatorsMeta)
            {
                if (!IndicatorsMeta.ContainsKey(pair.Key)) return false;
                if (IndicatorsMeta[pair.Key].spec!=pair.Value.spec) return false;
            }
            return true;
        }

    }

    public class CPTableMetaData
    {
        public string spec { get; set; }
        public double resAvgTime { get; set; }
        public List<string> SetupTypes { get; set; }
        public List<string> BoardTypes { get; set; }

        public CPTableMetaData(string _spec, double _resAvgTime, string _SetupTypesStr,string _BoardTypesStr)
        {
            spec = _spec;
            resAvgTime = _resAvgTime;
            SetupTypes = new List<string>(_SetupTypesStr.Split(','));
            BoardTypes = new List<string>(_BoardTypesStr.Split(','));
        }


        public CPTableMetaData(BsonDocument doc)
        {
            spec = (doc.GetValue("spec", "").ToString());
            resAvgTime = (doc.GetValue("resAvgTime", "0").ToDouble());
            SetupTypes = new List<string>();
            if (doc.Contains("SetupTypes"))
            {
                BsonArray sArray = doc.GetElement("SetupTypes").Value.AsBsonArray;
                foreach (string type in sArray)
                {
                    SetupTypes.Add(type);
                }
            }
            BoardTypes = new List<string>();
            if (doc.Contains("BoardTypes"))
            {
                BsonArray bArray = doc.GetElement("BoardTypes").Value.AsBsonArray;
                foreach (string type in bArray)
                {
                    BoardTypes.Add(type);
                }
            }
        }
        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument();
            Doc.Add("spec", spec);
            Doc.Add("resAvgTime", resAvgTime);
            BsonArray sArray = new BsonArray(SetupTypes);
            Doc.Add("SetupTypes", sArray);
            BsonArray bArray = new BsonArray(BoardTypes);
            Doc.Add("BoardTypes", bArray);
            return Doc;
        }
    }
   
}
