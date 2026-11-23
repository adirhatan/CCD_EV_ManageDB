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
    public class MDBBoard : MDBClient
    {
        public MDBBoard()
            : base("DB_PRM", "Boards")
        { }


        public Boolean Save(Board board)
        {
            base.MDBSaveRec(board.toBsonDocument());
            return true;
        }

        public Boolean Delete(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public Board Read(string id)
        {
            Board board = new Board();
            if (String.IsNullOrWhiteSpace(id)) return board;
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) board = new Board(rec);
            return board;
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