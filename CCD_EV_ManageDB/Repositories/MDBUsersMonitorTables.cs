using System;
using System.IO;
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
    public class MDBUsersMonitorTables : MDBClient
    {

        public MDBUsersMonitorTables()
            : base("DB_PRM", "UsersMonitorTables")
        { }

        public Boolean SaveUserMonitorTables(UsersMonitorTables UMTab)
        {
            BsonDocument sTables;

            sTables = new BsonDocument { { "_id", UMTab._id } };
            sTables.Add("name", UMTab.Name);
            BsonArray bArray = new BsonArray();
            foreach (var term in UMTab.MTables)
            {
                bArray.Add(term.ToBsonDocument());
            }
            sTables.Add("mTables", bArray);
            MDBSaveRec(sTables);
            return true;
        }

        public Boolean DeleteUserMonitorTables(string id)
        {
            return MDBDeleteID(id);
        }

        public Boolean RenameMonitorSet(string id, string name)
        {
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null)
            {
                rec.Set("name", name);
                return MDBSaveRec(rec);
            }
            return false;
        }

        public Boolean DeleteLastTable(string id)
        {
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null)
            {
                BsonArray bArray = rec.GetElement("mTables").Value.AsBsonArray;
                if (bArray.Count() <= 1) return (DeleteUserMonitorTables(id));
                bArray.RemoveAt(bArray.Count() - 1);
                rec.Set("mTables", bArray);
                MDBSaveRec(rec);
                return true;
            }
            return false;
        }
        
        public UsersMonitorTables ReadUserMonitorTables(string id)
        {
            UsersMonitorTables UMTab = new UsersMonitorTables();
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null)
            {
                UMTab._id = id;
                UMTab.Name = rec.GetValue("name", "").ToString();
                foreach (BsonDocument item in rec.GetElement("mTables").Value.AsBsonArray)
                {
                    UMTab.MTables.Add(new MTable(item.GetValue("TName", "").ToString(), item.GetValue("HostName", "").ToString(), item.GetValue("DBName", "").ToString()));
                }
            }
            return UMTab;
        }

        public List<UsersMonitorTables> ReadAllUserMonitorTables(string username)
        {
            List<UsersMonitorTables> UserMonitorTablesList = new List<UsersMonitorTables>();
            MongoCursor Cur =  MDBReadAllMatchedIDRecs(username);

            foreach (BsonDocument rec in Cur)
            {
                UsersMonitorTables UMTab = new UsersMonitorTables();
                UMTab._id = rec.GetValue("_id", "").ToString(); ;
                UMTab.Name = rec.GetValue("name", "").ToString();
                foreach (BsonDocument item in rec.GetElement("mTables").Value.AsBsonArray)
                {
                    UMTab.MTables.Add(new MTable(item.GetValue("TName", "").ToString(), item.GetValue("HostName", "").ToString(), item.GetValue("DBName", "").ToString()));
                }
                UserMonitorTablesList.Add(UMTab);
            }

            return UserMonitorTablesList;
        }

        public MongoCursor ReadCursorAllUserMonitorTables(string username)
        {
            MongoCursor Cur = MDBReadAllMatchedIDRecs(username);
            return Cur;
        }


    }
    
}