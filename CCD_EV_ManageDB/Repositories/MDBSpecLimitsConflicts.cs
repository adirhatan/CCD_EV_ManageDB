using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Threading;
using System.Globalization;
using System.Reflection;

namespace CCD_EV_ManageDB.Repositories
{
    public class MDBSpecLimitsConflicts : MDBClient
    {
        public MDBSpecLimitsConflicts()
            : base("DB_TMP", "SpecLimitsConflictsFound")
        { }

        public BsonDocument Read (string _id)
        {
            return base.MDBFindOne("EQ;_id;" + _id, "AND", "ASC", "_id");
        }

        public List<BsonValue> ReadTableConflictsSessionsList(string table, string project, string phase)
        {
            List<BsonValue> SessionsList = new List<BsonValue>();

            SessionsList = base.MDBFindDistinct("EQ;project;" + project + ";EQ;phase;" + phase + ";EQ;table;" + table + ";EQ;type;Conflict", "AND", "ASC", "_id", "SessionStart");
            return SessionsList;
        }

        //currenrly not in use:
        public MongoCursor ReadTableProjectPhaseConflicts(string table, string project, string phase)
        {
            MongoCursor Cur = base.MDBFind("EQ;project;" + project + ";EQ;phase;" + phase + ";EQ;table;" + table, "AND", "ASC", "_id");
            return Cur;
        }

        public bool Save(BsonDocument result)
        {
            return base.dbUpdate(result);
        }

        public bool Delete (string _id)
        {
            return base.MDBDeleteID(_id);
        }



    }
}