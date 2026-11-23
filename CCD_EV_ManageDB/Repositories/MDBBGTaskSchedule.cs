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
    public class MDBBGTaskSchedule : MDBClient
    {
        public MDBBGTaskSchedule()
            : base("DB_PRM", "BGTasksSchedule")
        { }

        public Boolean SaveBGTaskSchedule(BGTaskSchedule BGTS)
        {
            BsonDocument Doc;

            Doc = BGTS.toBsonDocument();
            return MDBSaveRec(Doc);
        }

        public BGTaskSchedule ReadBGTaskSchedule(string id)
        {
            BGTaskSchedule BGTS = new BGTaskSchedule();
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null)
            {
                BGTS = new BGTaskSchedule(rec);
            }
            return BGTS;
        }

        public List<BGTaskSchedule> ReadAllBGTasksSchedule()
        {
            List<BGTaskSchedule> BGTasksSchedList = new List<BGTaskSchedule>();
            MongoCursor Cur = MDBReadAllMatchedIDRecs("/.*/");

            foreach (BsonDocument rec in Cur)
            {
                BGTaskSchedule BGTS = new BGTaskSchedule(rec);
                BGTasksSchedList.Add(BGTS);
            }

            return BGTasksSchedList;
        }
    }
}