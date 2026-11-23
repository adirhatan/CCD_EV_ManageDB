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
    public class MDBSpecLimitLookUP : MDBClient
    {
        public MDBSpecLimitLookUP(string LookUPTableName)
            : base("DB_TMP", LookUPTableName)
        { }

        public BsonDocument Read (string table, string project,string phase, string specTestName)
        {
            return base.MDBFindOne("EQ;_id;" + table + "_" + project + "_" + table + "_" + phase + "_" + specTestName, "AND", "ASC", "_id");
        }


        public bool Save(BsonDocument result)
        {
            return base.dbUpdate(result);
        }
    }
}