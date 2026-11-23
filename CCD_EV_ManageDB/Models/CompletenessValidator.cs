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
    public class CompletenessValidator
    {
        public string UnitId { get; set; }
        public List<string> Lanes { get; set; }
        public List<string> Corners { get; set; }

        public CompletenessValidator()
        {
            Lanes = new List<string>();
            Corners = new List<string>(new string[] { "HVHT", "HVLT", "NOMN", "LVHT", "LVLT" });
        }
        public CompletenessValidator(string _UnitId, List<string> _Lanes)
        {
            UnitId = _UnitId;
            Lanes = _Lanes;
            Corners = new List<string>(new string[] { "HVHT", "HVLT", "NOMN", "LVHT", "LVLT" });

        }
        public CompletenessValidator(string _UnitId, List<string> _Lanes, List<string> _Corners)
        {
            UnitId = _UnitId;
            Lanes = _Lanes;
            Corners = _Corners;

        }

        public List<BsonDocument> Validate(string ReportID, string db, List<string> FieldSlides)
        {
            List<BsonDocument> MissingResults = new List<BsonDocument>();
            MDBUserTableReport MDBReport = new MDBUserTableReport();
            UserTableReport Report = MDBReport.ReadUserTableReport(ReportID);
            MDBStationResults MDBStationResults = new MDBStationResults("DB_" + db, Report.RConfig.table);
            foreach (string corner in Corners)
            {
                foreach (string lane in Lanes)
                {
                    int Fcnt = 0;
                    foreach (FieldSlide FS in Report.RConfig.FieldsSlides)
                    {
                        if (FieldSlides.Contains(Fcnt.ToString()))
                        {
                            //EQ;UnitId;1;EQ;Corner;MANUAL;EQ;Lane;NA;EQ;TestName;TestCSV
                            string query ="" ;
                            if (Report.RConfig.table=="PCIE-TX") query += "EQ;UnitSerialNumber;" + UnitId + ";";
                            else query += "EQ;UnitId;" + UnitId + ";";
                            query += "EQ;Corner;" + corner + ";";
                            query += "EQ;Lane;" + lane + ";";
                            query += "Exists;" + FS.column + "; ;";
                            query += "EQ;" + Report.TestName + ";" + FS.test;
                            BsonDocument res = MDBStationResults.MDBFindOne(query, "AND", "Des", "TestEnd");
                            if ((res == null) || (string.IsNullOrEmpty(res.GetValue(FS.column, "").ToString())))
                            {
                                BsonDocument MissedRes = new BsonDocument {
                                { "UnitId", UnitId },
                                { "Corner", corner },
                                { "Lane", lane },
                                { "TestName", FS.test },
                                { "Field", FS.column }
                            };
                                MissingResults.Add(MissedRes);
                            }
                        }
                        Fcnt++;
                    }
                }
            }

            return MissingResults;
        }

    }
}