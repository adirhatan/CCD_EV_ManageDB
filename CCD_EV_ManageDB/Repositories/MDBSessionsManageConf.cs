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
    public class MDBSessionsManageConf : MDBClient
    {
        public MDBSessionsManageConf()
            : base("DB_PRM", "SessionsManageConf")
        { }

        public MDBSessionsManageConf(string db)
            : base(db, "SessionsManageConf")
        { }

        public Boolean SaveDoNotOverWriteTimestamp(SessionsManageProtocolConf SMPconf)
        {
            SessionsManageProtocolConf existsSMPconf = Read(SMPconf.protocol, SMPconf.proj, SMPconf.phase);
            SMPconf.timestamp = existsSMPconf.timestamp;
            base.MDBSaveRec(SMPconf.toBsonDocument());
            return true;
        }

        public Boolean SaveDoNotOverWriteTimestamp_ActiveFileNames(SessionsManageProtocolConf SMPconf)
        {
            SessionsManageProtocolConf existsSMPconf = Read(SMPconf.protocol, SMPconf.proj, SMPconf.phase);
            SMPconf.timestamp = existsSMPconf.timestamp;
            SMPconf.ActiveFileNames = existsSMPconf.ActiveFileNames;
            base.MDBSaveRec(SMPconf.toBsonDocument());
            return true;
        }

        public Boolean Save(SessionsManageProtocolConf SMPconf)
        {
            base.MDBSaveRec(SMPconf.toBsonDocument());
            return true;
        }

        public Boolean SaveTMP(SessionsManageProtocolConf SMPconf)
        {
            SMPconf.useNewCSR = "Y";
            BsonDocument Doc = SMPconf.toBsonDocument();
            string _id = Doc.GetValue("_id", "").ToString();
            if (!_id.EndsWith("tmp")) _id += "_tmp";
            Doc.Set("_id", _id);
            base.MDBSaveRec(Doc);
            return true;
        }

        public Boolean Delete(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public Boolean DeleteTMP(string protocol, string proj, string phase)
        {
            base.MDBDeleteID(protocol + "_" + proj + "_" + phase + "_tmp");
            return true;
        }
        

        public SessionsManageProtocolConf Read(string id)
        {
            SessionsManageProtocolConf SMPconf = new SessionsManageProtocolConf();
            if (String.IsNullOrWhiteSpace(id)) return SMPconf;
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) SMPconf = new SessionsManageProtocolConf(rec);
            return SMPconf;
        }

        //if we use table/proj/phase as id
        public SessionsManageProtocolConf Read(string protocol, string proj, string phase)
        {
            return Read(protocol + "_" + proj + "_" + phase);
        }

        public SessionsManageProtocolConf ReadTMP(string protocol, string proj, string phase)
        {
            return Read(protocol + "_" + proj + "_" + phase + "_tmp");
        }


    }

}