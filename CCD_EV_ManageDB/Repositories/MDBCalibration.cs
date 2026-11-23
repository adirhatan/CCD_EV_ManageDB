using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace CCD_EV_ManageDB.Repositories
{
    public class MDBCalibration : MDBClient
    {
        public MDBCalibration()
            : base("DB_PRM", "Calibration")
        { }


        public Boolean Save(Calibration cal)
        {
            base.MDBSaveRec(cal.toBsonDocument());
            return true;
        }

        public Boolean Delete(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public Calibration Read(string id)
        {
            Calibration Cal = new Calibration();
            if (String.IsNullOrWhiteSpace(id)) return Cal;
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) Cal = new Calibration(rec);
            return Cal;
        }



        public List<Calibration> ReadAllCalibrations()
        {
            List<Calibration> CalibrationLines = new List<Calibration>();
            MongoCursor Cur;

            Cur = base.MDBReadAllMatchedIDRecs("/^./");
            foreach (BsonDocument doc in Cur)
            {
                CalibrationLines.Add(new Calibration(doc));
            }
            return CalibrationLines;

        }

        //public string[] ReadAllInventoryPlatforms()
        //{
        //    List<string> InventoryPlatforms = new List<string>();
        //    MongoCursor Cur;

        //    Cur = base.MDBReadAllMatchedIDRecs("/^./");
        //    foreach (BsonDocument doc in Cur)
        //    {
        //        if (!(doc.GetValue("FinishDate").ToDouble() > 0))
        //        InventoryPlatforms.Add(doc.GetValue("_id").ToString());
        //    }
        //    return InventoryPlatforms.ToArray();

        //}
    }

}