using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;

namespace CCD_EV_ManageDB.Models
{

    public class Calibration
    {
        public string id { get; set; }
        public string Spec { get; set; }
        public string CTS { get; set; }
        public string Station { get; set; }
        public string Project { get; set; }
        public string Phase { get; set; }
        public string Rate { get; set; }
        public string Setup { get; set; }
        public string Interface { get; set; }
        public string ExecutionDate { get; set; }
        public string File { get; set; }
        public string User { get; set; }
        public string ExpirationDays { get; set; }
        public bool Valid { get; set; }
        public bool Expired { get; set; }



        public Calibration(string _Spec,string _CTS,string _Station,string _Project,string _Phase,string _Rate,string _Setup,string _Interface,string _ExecutionDate,string _File,string _User,string _ExpirationDays)
        {
            id = _Spec + "_" + _CTS + "_" + _Station + "_" + _Project + "_" + _Phase + "_" + _Rate + "_" + _Setup;
            Spec = _Spec;
            CTS = _CTS;
            Station = _Station;
            Project = _Project;
            Phase = _Phase;
            Rate = _Rate;
            Setup = _Setup;
            Interface = _Interface;
            ExecutionDate = _ExecutionDate;
            File = _File;
            User = _User;
            ExpirationDays =_ExpirationDays;
            Valid = true;
            Expired = false;
            
        }
        public Calibration()
        {
            ExpirationDays = "30";
            ExecutionDate = DateTime.Now.ToString("yyyyMMdd");
            Valid = true;
            Expired = false;
        }
        public Calibration(BsonDocument doc)
        {
            id = (doc.GetValue("_id", "").ToString());
            Spec = (doc.GetValue("Spec", "").ToString());
            CTS = (doc.GetValue("CTS", "").ToString());
            Station = (doc.GetValue("Station", "").ToString());
            Project = (doc.GetValue("Project", "").ToString());
            Phase = (doc.GetValue("Phase", "").ToString());
            Rate = (doc.GetValue("Rate", "").ToString());
            Setup = (doc.GetValue("Setup", "").ToString());
            Interface = (doc.GetValue("Interface", "").ToString());
            ExecutionDate = (doc.GetValue("ExecutionDate", "").ToString());
            File = (doc.GetValue("File", "").ToString());
            User = (doc.GetValue("User", "").ToString());
            ExpirationDays = (doc.GetValue("ExpirationDays", "").ToString());
            Valid = (doc.GetValue("Valid", "").ToBoolean());
            Expired = (doc.GetValue("Expired", "").ToBoolean());

        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", id } };
            Doc.Add("Spec", Spec);
            Doc.Add("CTS", CTS);
            Doc.Add("Station", Station);
            Doc.Add("Project", Project);
            Doc.Add("Phase", Phase);
            Doc.Add("Rate", Rate);
            Doc.Add("Setup", Setup);
            Doc.Add("Interface", Interface);
            Doc.Add("ExecutionDate", Convert.ToDouble(ExecutionDate));
            Doc.Add("File", File);
            Doc.Add("User", User);
            Doc.Add("ExpirationDays", ExpirationDays);
            Doc.Add("Valid", Valid);
            Doc.Add("Expired", Expired);
            return Doc;
        }
        public HashSet<string> GetCalibrationFields()
        {
            HashSet<string> Fields = new HashSet<string>(new string[] { "_id", "Spec", "CTS", "Station", "Project", "Phase", "Setup", "Interface", "User", "ExecutionDate","Valid","Expired"});
            return Fields;
        }

    }

}