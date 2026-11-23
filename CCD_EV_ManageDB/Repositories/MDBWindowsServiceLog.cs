using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace CCD_EV_ManageDB.Repositories
{
    public class MDBWindowsServiceLog : MDBClient
    {
        public MDBWindowsServiceLog()
            : base("DB_TMP", "WindowsServiceLog")
        { }

        public Boolean SaveWindowsServiceLogBsonDoc(BsonDocument Doc)
        {
            try
            {
                MDBSaveRec(Doc);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public Boolean SaveWindowsServiceLogRec(string id, string status, string message)
        {
            BsonDocument Doc;

            Doc = new BsonDocument { { "_id", id } };
            Doc.Add("status", status);
            Doc.Add("message", message);
            try
            {
                MDBSaveRec(Doc);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

    }
}