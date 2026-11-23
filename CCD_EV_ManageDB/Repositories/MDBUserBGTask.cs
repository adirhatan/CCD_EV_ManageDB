using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace CCD_EV_ManageDB.Repositories
{
    public class MDBUserBGTask : MDBClient
    {
        public MDBUserBGTask()
            : base("DB_PRM", "UsersBGTasks")
        { }

        public Boolean SaveUserBGTask(UserBGTask UBT)
        {
            BsonDocument Doc;

            Doc = new BsonDocument { { "_id", UBT.id } };
            Doc.Add("desc", UBT.desc);
            Doc.Add("status", UBT.status);
            Doc.Add("result", UBT.result);
            Doc.Add("result2", UBT.result2);
            Doc.Add("result3", UBT.result3);
            Doc.Add("result4", UBT.result4);
            Doc.Add("sourceID", UBT.sourceID);
            Doc.Add("jmap", UBT.jmap);
            MDBSaveRec(Doc);
            return true;
        }

        public Boolean DeleteUserBGTask(string id)
        {
            return MDBDeleteID(id);
        }

        public UserBGTask ReadUserBGTasks(string id)
        {
            UserBGTask UBT = new UserBGTask();
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null)
            {
                UBT = BsonToObj(rec);
            }
            return UBT;
        }

        public List<UserBGTask> ReadAllUserBGTasks(string username)
        {
            List<UserBGTask> UserBGTasksList = new List<UserBGTask>();
            MongoCursor Cur = MDBReadAllMatchedIDRecs(username+"2");

            foreach (BsonDocument rec in Cur)
            {
                UserBGTask UBT = BsonToObj(rec);
                UserBGTasksList.Add(UBT);
            }

            return UserBGTasksList;
        }

        public MongoCursor ReadCursorAllUserBGTasks(string username)
        {
            SortByBuilder sort = new SortByBuilder();
            MongoCursor Cur = MDBReadAllMatchedIDRecs(username+"2");
            sort.Descending(new string[] { "_id" });            
            Cur.SetSortOrder(sort);
            return Cur;
        }

        public MongoCursor ReadCursorUserBGTasksBySourceID(string username,string SourceID)
        {
            string query = "Matches;_id;" + username + "2;EQ;sourceID;" + SourceID;
            MongoCursor Cur = MDBFind(query, "AND", "Des", "_id");
            return Cur;
        }

        public UserBGTask ReadLastBGTasksBySourceID(string username, string SourceID)
        {
            UserBGTask UBT = new UserBGTask();
            string query = "Matches;_id;" + username + "2;EQ;sourceID;" + SourceID;
            BsonDocument rec = MDBFindOne(query, "AND", "Des", "_id");
            if (rec != null)
            {
                UBT = BsonToObj(rec);
            }
            return UBT;
        }

        private UserBGTask BsonToObj(BsonDocument rec)
        {
            UserBGTask UBT = new UserBGTask();
            UBT.id = rec.GetValue("_id", "").ToString(); ;
            UBT.desc = rec.GetValue("desc", "").ToString();
            UBT.status = rec.GetValue("status", "").ToString();
            UBT.result = rec.GetValue("result", "").ToString();
            UBT.result2 = rec.GetValue("result2", "").ToString();
            UBT.result3 = rec.GetValue("result3", "").ToString();
            UBT.result4 = rec.GetValue("result4", "").ToString();
            UBT.sourceID = rec.GetValue("sourceID", "").ToString();
            if (!String.IsNullOrEmpty(rec.GetValue("jmap", "").ToString())) UBT.jmap = rec.GetValue("jmap", "").AsBsonDocument;
            return UBT;
        }


        public UserBGTask FindRecentLaunched()
        {

            UserBGTask UBT = new UserBGTask();
            string query = "EQ;status;Launched";
            //string query = "EQ;status;CompReport";
            BsonDocument rec = MDBFindOne(query, "AND", "Des", "_id");
            if (rec != null)
            {
                UBT = BsonToObj(rec);
            }
            return UBT;
        }

        public UserBGTask FindRecentLaunched4exe()
        {

            UserBGTask UBT = new UserBGTask();
            string query = "EQ;status;Launched4exe";
            //string query = "EQ;status;CompReport";
            BsonDocument rec = MDBFindOne(query, "AND", "Des", "_id");
            if (rec != null)
            {
                UBT = BsonToObj(rec);
            }
            return UBT;
        }

        public long FindRecentInProgress()
        {

            UserBGTask UBT = new UserBGTask();
            string query = "EQ;status;In progress";
            MongoCursor cur = MDBFind(query, "AND", "Des", "_id");
            return cur.Count();
        }

        public bool DeleteOldUserBGTasks(long date, ref string errMsg)
        {
            try
            {
                MongoCursor Cur = collection.FindAll();
                foreach (BsonDocument rec in Cur)
                {
                    string id = rec.GetValue("_id", "").ToString();
                    long recDate = Convert.ToInt64(Regex.Replace(id, @"\D*", ""));
                    if (recDate < date) MDBDeleteID(id);
                }
            }
            catch (Exception e)
            {
                errMsg = e.Message;
                return false;
            }
            return true;
        }
    }
}