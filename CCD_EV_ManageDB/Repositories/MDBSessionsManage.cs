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
    public class MDBSessionsManage : MDBClient
    {
        public MDBSessionsManage()
            : base("DB_PRM", "SessionsManage")
        { }

        public MDBSessionsManage(string db)
            : base(db, "SessionsManage")
        { }

        public Boolean Save(SessionsManage SM)
        {
            base.MDBSaveRec(SM.toBsonDocument());
            return true;
        }

        public Boolean Delete(string id)
        {
            ObjectId _id = new ObjectId(id);
            base.MDBDeleteID(_id);
            return true;
        }

        public Boolean Delete(ObjectId id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public SessionsManage Read(string id)
        {
            SessionsManage SM = new SessionsManage();
            if (String.IsNullOrWhiteSpace(id)) return SM;
            ObjectId _id = new ObjectId(id);
            BsonDocument rec = collection.FindOneById(_id);
            if (rec != null) SM = new SessionsManage(rec);
            return SM;
        }

        public List<SessionsManage> ReadTableProjectPhaseSessions(string table, string proj, string phase)
        {
            List<SessionsManage> SMList = new List<SessionsManage>();
            MongoCursor Cur;
            string phase_query = ";EQ;phase;" + phase;
            if ((proj == "HBR") && (phase == "A0")) phase_query = ";In;phase;A0,A1";
            Cur = base.MDBFind("EQ;project;" + proj + phase_query + ";EQ;table;" + table, "AND", "ASC", "_id");
            foreach (BsonDocument doc in Cur)
            {
                SMList.Add(new SessionsManage(doc));
            }
            return SMList;

        }

        public List<SessionsManage> ReadSameBoardSessions(string _id)
        {
            List<SessionsManage> SMList = new List<SessionsManage>();
            MongoCursor Cur;

            SessionsManage SM = this.Read(_id);

            string phase_query = ";EQ;phase;" + SM.phase;
            if ((SM.project == "HBR") && (SM.phase == "A0")) phase_query = ";In;phase;A0,A1";
            Cur = base.MDBFind("EQ;project;" + SM.project + phase_query + ";EQ;table;" + SM.table + ";EQ;board_id;" + SM.board_id, "AND", "ASC", "_id");
            foreach (BsonDocument doc in Cur)
            {
                SessionsManage targetSessoin = new SessionsManage(doc);
                if (SM.ImpEqualsExceptValidatoin(targetSessoin))
                    SMList.Add(targetSessoin);
            }
            return SMList;

        }


        //To be used from TMP_DB
        public List<string> ReadTMPTableProjectPhaseSessions(string table, string proj, string phase)
        {
            SessionsManage SM_TMP = new SessionsManage();
            List<SessionsManage> SMList = new List<SessionsManage>();
            string phase_query = ";EQ;phase;" + phase;
            if ((proj == "HBR") && (phase == "A0")) phase_query = ";In;phase;A0,A1";
            BsonDocument doc = base.MDBFindOne("EQ;project;" + proj + phase_query + ";EQ;table;" + table, "AND", "ASC", "_id");
            if (doc !=null) SM_TMP = new SessionsManage(doc);
            return SM_TMP.Sessions;
        }

        public Boolean SaveTMPRecExecutionSessionsWithNoResults(string table, string proj, string phase, List<string> SList)
        {
            SessionsManage SM_TMP = new SessionsManage();
            string phase_query = ";EQ;phase;" + phase;
            if ((proj == "HBR") && (phase == "A0")) phase_query = ";In;phase;A0,A1";
            BsonDocument doc = base.MDBFindOne("EQ;project;" + proj + phase_query + ";EQ;table;" + table, "AND", "ASC", "_id");           
            if (doc != null) SM_TMP._id = doc.GetValue("_id").AsObjectId;
            SM_TMP.table = table;
            SM_TMP.project = proj;
            SM_TMP.phase = phase;
            SM_TMP.Sessions = SList;
            base.MDBSaveRec(SM_TMP.toBsonDocument());
            return true;
        }

    }

}