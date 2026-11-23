using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;

namespace CCD_EV_ManageDB.Models
{
    [Serializable()]
    public class DBsearchSave
    {
        public string db { get; set; }  
        public string col { get; set; }
        public string Query { get; set; }
        public string FieldsStr { get; set; }
        public string FieldsIncExc { get; set; }
        public bool FieldsInc { get; set; }
        public bool FieldsExc { get; set; }
        public string SortStr { get; set; }
        public string SortDirection { get; set; }

        public DBsearchSave(DBsearch DBS)
        {
            db = DBS.db;
            col = DBS.col;
            Query = DBS.Query;
            FieldsStr = DBS.FieldsStr;
            FieldsIncExc = DBS.FieldsIncExc;
            FieldsInc = DBS.FieldsInc;
            FieldsExc = DBS.FieldsExc;
            SortStr = DBS.SortStr;
            SortDirection = DBS.SortDirection;
        }

        public DBsearchSave()
        {
            // TODO: Complete member initialization
        }

        public DBsearch getNewDBS()
        {
            DBsearch DBS = new DBsearch();
            DBS.db = db;
            DBS.col = col;
            DBS.Query = Query;
            DBS.FieldsStr = FieldsStr;
            DBS.FieldsIncExc = FieldsIncExc;
            DBS.FieldsInc = FieldsInc;
            DBS.FieldsExc = FieldsExc;
            DBS.SortStr = SortStr;
            DBS.SortDirection = SortDirection;
            return DBS;
        }

    }
    public static class BsonDocumentDBSExtension
    {
        // This is the extension method. 
        // The first parameter takes the "this" modifier
        // and specifies the type for which the method is defined. 

        public static DBsearchSave BsonDocumentToDBsearchSave(this BsonDocument doc)
        {
            DBsearchSave DBSave = new DBsearchSave();
            DBSave.db = (doc.GetValue("db", "").ToString());
            DBSave.col = (doc.GetValue("col", "").ToString());
            DBSave.Query = (doc.GetValue("Query", "").ToString());
            DBSave.FieldsStr = (doc.GetValue("FieldsStr", "").ToString());
            DBSave.FieldsIncExc = (doc.GetValue("FieldsIncExc", "").ToString());
            DBSave.FieldsInc = (doc.GetValue("FieldsInc", false).ToBoolean());
            DBSave.FieldsExc = (doc.GetValue("FieldsExc", false).ToBoolean());
            DBSave.SortStr = (doc.GetValue("SortStr", "").ToString());
            DBSave.SortDirection = (doc.GetValue("SortDirection", "").ToString());
            return DBSave;
        }
    }
}