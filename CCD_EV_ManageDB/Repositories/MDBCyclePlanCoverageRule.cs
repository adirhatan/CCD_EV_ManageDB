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
    public class MDBCyclePlanCoverageRule : MDBClient
    {
        public MDBCyclePlanCoverageRule()
            : base("DB_PRM", "CyclePlanCoverageRules")
        { }

        public MDBCyclePlanCoverageRule(string db)
            : base(db, "CyclePlanCoverageRules")
        { }

        public Boolean Save(CyclePlanCoverageRule CPCoverageRule)
        {
            base.MDBSaveRec(CPCoverageRule.toBsonDocument());
            return true;
        }

        public Boolean Delete(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public CyclePlanCoverageRule Read(string id)
        {
            CyclePlanCoverageRule CPCoverageRule = new CyclePlanCoverageRule();
            if (String.IsNullOrWhiteSpace(id)) return CPCoverageRule;
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) CPCoverageRule = new CyclePlanCoverageRule(rec);
            return CPCoverageRule;
        }

        //if we use table/proj/phase as id
        public CyclePlanCoverageRule Read(string protocol, string proj, string phase, string table, string name)
        {
            return Read(protocol + "_" + proj + "_" + phase + "_" + table + "_" + name);
        }

        public List<CyclePlanCoverageRule> ReadProjectList(string protocol, string proj, string phase)
        {
            List<CyclePlanCoverageRule> CyclePlanCoverageRules = new List<CyclePlanCoverageRule>();
            string query = "Matches;_id;/" + protocol + "_" + proj + "_" + phase + "/";
            MongoCursor Cur = base.MDBFind(query, "AND", "ASC", "_id");
            foreach (BsonDocument rec in Cur) CyclePlanCoverageRules.Add(new CyclePlanCoverageRule(rec));
            return (CyclePlanCoverageRules);
        }

        public List<CyclePlanCoverageRule> ReadProjectTableList(string protocol, string proj, string phase, string table)
        {
            List<CyclePlanCoverageRule> CyclePlanCoverageRules = new List<CyclePlanCoverageRule>();
            string query = "Matches;_id;/" + protocol + "_" + proj + "_" + phase + '_' + table +"/;EQ;Table;"+table;
            MongoCursor Cur = base.MDBFind(query, "AND", "ASC", "_id");
            foreach (BsonDocument rec in Cur) CyclePlanCoverageRules.Add(new CyclePlanCoverageRule(rec));
            return (CyclePlanCoverageRules);
        }


    }

}