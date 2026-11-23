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
    public class MDBTestNameLookUP : MDBClient
    {
        public MDBTestNameLookUP()
            : base("DB_TMP", "TestNameLookUP")
        { }

        public BsonDocument Read (string project,string table, string spec)
        {
            return base.MDBFindOne("EQ;_id;" + project + "_" + table + "_" + spec,"AND","ASC","_id");
        }


        public bool Save(BsonDocument result)
        {
            return base.dbUpdate(result);
        }
    }
}