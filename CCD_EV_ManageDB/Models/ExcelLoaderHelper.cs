using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace CCD_EV_ManageDB.Models
{


    public class ExcelLoaderHelper
    {
        //List<string> mandatoryFields = new List<string>(new string[] { "_id", "ProjectName", "BoardNumber", "SessionStart", "Phase", "Skew", "OPName", "UnitId", "NVM_REV", "Port", "SessionStart", "TestStart", "TestEnd", "Corner", "Lane", "V1_05", "A1_05", "V3_3", "A3_3", "Tjunc", "Tamb", "Retry" });

        //Mandatory for copy to SD
        //List<string> mandatoryFields = new List<string>(new string[] { "_id", "ProjectName", "SessionStart" });
        List<string> mandatoryFields = new List<string>(System.Configuration.ConfigurationManager.AppSettings["VitalFields"].Split(','));

        public Boolean validate(BsonDocument item)
        {
            BsonValue BVal = null;
            double d;
            string testStart = item.GetValue("TestStart", "").ToString();
            string h_testStart = item.GetValue("HIDDEN_TestStart", "").ToString();
            string testEnd = item.GetValue("TestEnd", "").ToString();
            string h_testEnd = item.GetValue("HIDDEN_TestEnd", "").ToString();
            string sessionStart = item.GetValue("SessionStart", "").ToString();
            string h_sessionStart = item.GetValue("HIDDEN_SessionStart", "").ToString();

            if ((!String.IsNullOrEmpty(testStart)) && (String.IsNullOrEmpty(h_testStart)))
            {
                h_testStart = testStart.Replace("_", "");
                if (double.TryParse(h_testStart, out d)) BVal = d;
                else BVal = h_testStart;
                item.Set("HIDDEN_TestStart", BVal);
            }
            if ((!String.IsNullOrEmpty(testEnd)) && (String.IsNullOrEmpty(h_testEnd)))
            {
                h_testEnd = testEnd.Replace("_", "");
                if (double.TryParse(h_testEnd, out d)) BVal = d;
                else BVal = h_testEnd;
                item.Set("HIDDEN_TestEnd", BVal);
            }
            if ((!String.IsNullOrEmpty(sessionStart)) && (String.IsNullOrEmpty(h_sessionStart)))
            {
                h_sessionStart = sessionStart.Replace("_", "");
                if (double.TryParse(h_sessionStart, out d)) BVal = d;
                else BVal = h_sessionStart;
                item.Set("HIDDEN_SessionStart", BVal);
            }
            foreach (string M_Field in mandatoryFields)
            {
                BsonElement field = null;
                try
                {
                    field = item.GetElement(M_Field);
                }
                catch (Exception ex)
                {
                    return false;
                }
                if ((field == null) || (field.Value == null) || (field.Value.ToString() == ""))
                {
                    return false;
                }
            }
            return true;
        }
        public Boolean AreFilesExist(BsonDocument item)
        {
            foreach (BsonElement field in item)
            {
                if (((field.Name.Length > 3) && (field.Name.Substring(0, 4) == "FILE")) ||
                    ((field.Name.Length > 5) && (field.Name.Substring(0, 6) == "ATTACH")))
                    if ((field.Value != null) && (field.Value.ToString() != "") && !(System.IO.File.Exists(field.Value.ToString()))) return false;
            }
            return true;
        }
    }
}
