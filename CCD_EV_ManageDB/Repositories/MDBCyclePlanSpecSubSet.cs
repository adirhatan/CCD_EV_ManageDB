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
    public class MDBCyclePlanSpecSubSet : MDBClient
    {
        public MDBCyclePlanSpecSubSet()
            : base("DB_PRM", "CyclePlanSpecSubSets")
        { }

        public MDBCyclePlanSpecSubSet(string db)
            : base(db, "CyclePlanSpecSubSets")
        { }

        public Boolean Save(CyclePlanSpecSubSet CPSpecSubSet)
        {
            base.MDBSaveRec(CPSpecSubSet.toBsonDocument());
            return true;
        }

        public Boolean Delete(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public CyclePlanSpecSubSet Read(string id)
        {
            CyclePlanSpecSubSet CPSpecSubSet = new CyclePlanSpecSubSet();
            if (String.IsNullOrWhiteSpace(id)) return CPSpecSubSet;
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) CPSpecSubSet = new CyclePlanSpecSubSet(rec);
            return CPSpecSubSet;
        }

        //if we use table/proj/phase as id
        public CyclePlanSpecSubSet Read(string protocol, string proj, string phase, string table, string name)
        {
            return Read(protocol + "_" + proj + "_" + phase + "_" + table + "_" + name);
        }

        public List<CyclePlanSpecSubSet> ReadProjectList(string protocol, string proj, string phase)
        {
            List<CyclePlanSpecSubSet> CyclePlanSpecSubSets = new List<CyclePlanSpecSubSet>();
            string query = "Matches;_id;/" + protocol + "_" + proj + "_" + phase + "/";
            MongoCursor Cur = base.MDBFind(query, "AND", "ASC", "_id");
            foreach (BsonDocument rec in Cur) CyclePlanSpecSubSets.Add(new CyclePlanSpecSubSet(rec));
            return (CyclePlanSpecSubSets);
        }

        public List<CyclePlanSpecSubSet> ReadProjectTableList(string protocol, string proj, string phase, string table)
        {
            List<CyclePlanSpecSubSet> CyclePlanSpecSubSets = new List<CyclePlanSpecSubSet>();
            string query = "Matches;_id;/" + protocol + "_" + proj + "_" + phase + '_' + table + "/;EQ;Table;" + table;
            MongoCursor Cur = base.MDBFind(query, "AND", "ASC", "_id");
            foreach (BsonDocument rec in Cur) CyclePlanSpecSubSets.Add(new CyclePlanSpecSubSet(rec));
            return (CyclePlanSpecSubSets);
        }


    }

}