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
    public class MDBCyclePlanProjectUnit : MDBClient
    {
        public MDBCyclePlanProjectUnit()
            : base("DB_PRM", "CyclePlanProjectUnits")
        { }

        public MDBCyclePlanProjectUnit(string db)
            : base(db, "CyclePlanProjectUnits")
        { }

        public Boolean Save(CyclePlanProjectUnit CPProjectUnit)
        {
            base.MDBSaveRec(CPProjectUnit.toBsonDocument());
            return true;
        }

        public Boolean Delete(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public CyclePlanProjectUnit Read(string id)
        {
            CyclePlanProjectUnit CPProjectUnit = new CyclePlanProjectUnit();
            if (String.IsNullOrWhiteSpace(id)) return CPProjectUnit;
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) CPProjectUnit = new CyclePlanProjectUnit(rec);
            return CPProjectUnit;
        }

        //if we use table/proj/phase as id
        public CyclePlanProjectUnit Read(string _protocol, string _proj, string _phase, string _Table, string _Skew, string _LogicUnitNumber,string _BoardType, string _SetupType)
        {
            string LogicUnit = _Skew + "_" + _BoardType.ToUpper().Substring(0, 1) + "_" + _SetupType.ToUpper().Substring(0, 1) + "_" + _LogicUnitNumber;
            return Read(_protocol + "_" + _proj + "_" + _phase + "_" + _Table + "_" + LogicUnit);
        }

        public List<CyclePlanProjectUnit> ReadProjectList(string protocol, string proj, string phase)
        {
            List<CyclePlanProjectUnit> CPProjectUnits = new List<CyclePlanProjectUnit>();
            string query = "Matches;_id;/" + protocol + "_" + proj + "_" + phase + "/";
            MongoCursor Cur = base.MDBFind(query, "AND", "ASC", "_id");
            foreach (BsonDocument rec in Cur) CPProjectUnits.Add(new CyclePlanProjectUnit(rec));
            return (CPProjectUnits);
        }

        public List<CyclePlanProjectUnit> ReadProjectTableList(string protocol, string proj, string phase , string Table)
        {
            List<CyclePlanProjectUnit> CPProjectUnits = new List<CyclePlanProjectUnit>();
            string query = "Matches;_id;/" + protocol + "_" + proj + "_" + phase + '_' + Table + "/;EQ;Table;" + Table;
            MongoCursor Cur = base.MDBFind(query, "AND", "ASC", "_id");
            foreach (BsonDocument rec in Cur) CPProjectUnits.Add(new CyclePlanProjectUnit(rec));
            return (CPProjectUnits);
        }


    }

}