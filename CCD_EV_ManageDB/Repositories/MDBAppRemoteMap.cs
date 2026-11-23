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
    public class MDBAppRemoteMap : MDBClient
    {
        public MDBAppRemoteMap()
            : base("DB_TMP", "AppRemoteMapp")
        { }

        public Boolean SaveAppRemoteMap(BsonDocument Doc)
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

        public Boolean SaveAppRemoteMapRec(string id, string filePath)
        {
            BsonDocument Doc;

            Doc = new BsonDocument { { "_id", id } };
            Doc.Add("filePath", filePath);
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

        public string ReadAppRemoteMapFilePath(string tableName) 
        {

            string query = "";
            query = "EQ;_id;" + tableName ;
            BsonDocument res = MDBFindOne(query, "AND", "Des", "_id");
            if (res != null) return res.GetValue("filePath", "").ToString();
            return "";

        }

    }
}