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
    public class MDBBsonDoc : MDBClient
    {
        public MDBBsonDoc(string db, string table)
            : base(db, table)
        { }

        public Boolean SaveRec(BsonDocument rec)
        {
            return base.MDBSaveRec(rec);
        }

        public Boolean DeleteRec(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public BsonDocument ReadRec(string id)
        {
            BsonDocument rec = new BsonDocument();
            if (String.IsNullOrWhiteSpace(id)) return rec;
            rec = collection.FindOneById(id);
            return rec;
        }

    }
}