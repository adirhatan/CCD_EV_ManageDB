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
    public class MDBADMReferenceTable : MDBClient
    {
        public MDBADMReferenceTable()
            : base("DB_PRM", "ADMReferenceTable")
        { }

        public Boolean SaveADMReferenceTable(ADMReferenceTable Ref)
        {
            BsonDocument sRef;

            sRef = new BsonDocument { { "_id", Ref._id } };
            sRef.Add("Protocol", Ref.Protocol);
            sRef.Add("PVersion", Ref.PVersion);
            sRef.Add("RConfig", Ref.RConfig.toBsonDocument());
            MDBSaveRec(sRef);
            return true;
        }

        public string CopyAndSaveADMReferenceTable(string _Protocol,string _PVresion, ADMReferenceTable Ref)
        {
            ADMReferenceTable CRef = new ADMReferenceTable(_Protocol, _PVresion);
            CRef.RConfig = Ref.RConfig;
            if (this.SaveADMReferenceTable(CRef)) return CRef._id;
            return null;
        }

        public ADMReferenceTable ReadADMReferenceTable(string id)
        {
            ADMReferenceTable Ref = new ADMReferenceTable();
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null)
            {
                Ref._id = id;
                Ref.RConfig = rec.GetValue("RConfig").AsBsonDocument.BsonDocumentToReportConfig();
                Ref.Protocol = rec.GetValue("Protocol", "").ToString();
                Ref.PVersion = rec.GetValue("PVersion", "").ToString();
            }
            return Ref;
        }

        public Boolean DeleteADMReferenceTable(string id)
        {
            return MDBDeleteID(id);
        }

        public MongoCursor ReadCursorAllADMReferenceTables()
        {
            MongoCursor Cur = MDBReadAllMatchedIDRecs("/^./");
            return Cur;
        }

        public List<string> ReadListAllADMReferenceTablesIDS()
        {
            List<string> IDS = new List<string>();
            MongoCursor Cur = MDBReadAllMatchedIDRecs("/^./");
            foreach (BsonDocument doc in Cur) IDS.Add(doc.GetValue("_id").ToString());
            return IDS;
        }

        public MongoCursor ReadCursorAllProtocolADMReferenceTables(string protocol)
        {
            MongoCursor Cur = MDBReadAllMatchedIDRecs("/^"+ protocol + "/");
            return Cur;
        }


    }
}