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
    public class MDBCyclePlanMetaData : MDBClient
    {
        public MDBCyclePlanMetaData()
            : base("DB_PRM", "CyclePlanMetaData")
        { }

        public MDBCyclePlanMetaData(string db)
            : base(db, "CyclePlanMetaData")
        { }

        public Boolean Save(CyclePlanMetaData CPMetaData)
        {
            base.MDBSaveRec(CPMetaData.toBsonDocument());
            return true;
        }

        public Boolean Delete(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public CyclePlanMetaData Read(string id)
        {
            CyclePlanMetaData CPMetaData = new CyclePlanMetaData();
            if (String.IsNullOrWhiteSpace(id)) return CPMetaData;
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) CPMetaData = new CyclePlanMetaData(rec);
            return CPMetaData;
        }

        //if we use table/proj/phase as id
        public CyclePlanMetaData Read(string protocol, string proj, string phase)
        {
            return Read(protocol + "_" + proj + "_" + phase);
        }


    }

}