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
    public class MDBIndicator : MDBClient
    {
        public MDBIndicator()
            : base("DB_PRM", "IndicatorsMetaData")
        { }

        public MDBIndicator(string db)
            : base(db, "IndicatorsMetaData")
        { }

        public Boolean SaveIndicator(Indicator ind)
        {
            base.MDBSaveRec(ind.toBsonDocument());
            return true;
        }

        public Boolean DeleteIndicator(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public Indicator ReadIndicator(string id)
        {
            Indicator Ind = new Indicator();
            if (String.IsNullOrWhiteSpace(id)) return Ind;
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) Ind = rec.BsonDocumentToIndicator();
            return Ind;
        }

        public Indicator ReadIndicator(string col,string proj, string phase)
        {
            return ReadIndicator(col + "_" + proj + "_" + phase);
        }

        public List<Indicator> ReadProjectPhaseIndicators(string proj, string phase)
        {
            List<Indicator> IndicatorList = new List<Indicator>();
            MongoCursor Cur;

            Cur = base.MDBFind("EQ;project;" + proj + ";EQ;phase;" + phase, "AND", "ASC", "_id");
            foreach (BsonDocument doc in Cur)
            {
                IndicatorList.Add(new Indicator(doc));
            }
            return IndicatorList;

        }

    }

}