using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class UserBGTask
    {
        public string id { get; set; }
        public string desc { get; set; }
        public string status { get; set; }
        public string result { get; set; }
        public string result2 { get; set; }
        public string result3 { get; set; }
        public string result4 { get; set; }
        public string sourceID { get; set; }
        public BsonDocument jmap { get; set; }

        public UserBGTask()
        {
            jmap = new BsonDocument();
        }

        public UserBGTask(string _id, string _desc)
        {
            id = _id;
            desc = _desc;
            status = "Launched";
            result = "";
            result2 = "";
            result3= "";
            result4 = "";
            sourceID = "";
            jmap = new BsonDocument();
        }
        public UserBGTask(string _id, string _desc,string _sourceID)
        {
            id = _id;
            desc = _desc;
            status = "Launched";
            result = "";
            result2 = "";
            result3= "";
            result4 = "";
            sourceID = _sourceID;
            jmap = new BsonDocument();

        }

        public UserBGTask(string _id, string _desc, string _sourceID,BsonDocument _jmap)
        {
            id = _id;
            desc = _desc;
            status = "Launched";
            result = "";
            result2 = "";
            result3 = "";
            result4 = "";
            sourceID = _sourceID;
            jmap = _jmap;

        }

        public bool checkIfReportCreated()
        {
            if (string.IsNullOrEmpty(result4))
            {
                string marginDir = Path.Combine(result, "MarginData");
                if (System.IO.Directory.Exists(marginDir))
                {
                    string[] marginFiles = System.IO.Directory.GetFiles(marginDir);
                    if (marginFiles.Length > 0) result4 = marginFiles[0];
                }
            }
            if (!System.IO.Directory.Exists(result)) return false;
            string[] resultsDir = System.IO.Directory.GetDirectories(Path.Combine(result, "Results"));
            if (resultsDir.Length == 0) return false;
            string[] resultsFiles = System.IO.Directory.GetFiles(resultsDir[0]);
            if (resultsFiles.Length == 0) return false;
            if (resultsFiles.Length > 0) result2 = Path.Combine(result, "Results",resultsFiles[0]);
            if (resultsFiles.Length > 1) result3 = Path.Combine(result, "Results", resultsFiles[1]);
            return true;
        }
    }
}