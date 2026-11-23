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
    public class MDBCyclePlanCSRLookUP : MDBClient
    {
        public MDBCyclePlanCSRLookUP(string LookUPTableName)
            : base("DB_PRM", LookUPTableName)
        { }

        public MDBCyclePlanCSRLookUP(string protocol, string project, string phase, string table)
            : base("DB_PRM", "CSRLookUP_" + protocol + "_" + project  + "_" + phase + "_" + table)
        { }

        public BsonDocument Read(string _id)
        {
            //Unit+specTestName +corner+port+lane
            return base.MDBFindOne("EQ;_id;" + _id, "AND", "ASC", "_id");
        }

        public BsonDocument Read (string Unit, string specTestName,string corner, string port, string lane)
        {
            //Unit+specTestName +corner+port+lane
            return base.MDBFindOne("EQ;_id;" + Unit + "_" + specTestName + "_CORNER_" + corner + "_" + port + "_" + lane, "AND", "ASC", "_id");
        }


        public bool Save(BsonDocument result)
        {
            return base.dbUpdate(result);
        }

        public bool isUpdated()
        {
            BsonDocument doc = base.MDBFindOne("EQ;_id;isUpdated", "AND", "ASC", "_id");
            if (doc==null) return false;
            if (doc.GetValue("val", "").ToString() == "U") //table is being built right now, need to wait
            {
                bool need2wait = true;
                int cnt = 0;
                while (need2wait)
                {
                    Thread.Sleep(3000);
                    cnt++;
                    doc = base.MDBFindOne("EQ;_id;isUpdated", "AND", "ASC", "_id");
                    if (doc.GetValue("val", "").ToString() == "Y") need2wait = false;
                    if (cnt > 40) need2wait = false;
                }
            }
            return (doc.GetValue("val", "").ToString() == "Y");
        }


        public void DropCollection()
        {
            base.collection.Drop();
        }
    }
}