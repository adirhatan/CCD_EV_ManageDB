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
    public class MDBTablesFields : MDBClient
    {
        public MDBTablesFields()
            : base("DB_TMP", "TablesFields")
        { }

        public HashSet<string> ReadTableFields(string table,General.TableType type)
        {
            HashSet<string> Fields = new HashSet<string>();
            BsonDocument FieldsDoc = collection.FindOneById(type.ToString() + "_" + table);
            if ((FieldsDoc != null) && (FieldsDoc.GetElement("date").Value.ToString() == (DateTime.Now.ToString("yyyyMMdd"))))
            {
                foreach (string field in FieldsDoc.GetElement("Fields").Value.AsBsonArray)
                {
                    Fields.Add(field);
                }
            }
            else
            {
                if (type == General.TableType.Dev)
                {
                    Fields = (new MDBStationResults("DB_TMP", table)).getTableFields();
                    if (Fields.Count == 0) Fields = (new MDBStationResults("DB_PRM", table)).getTableFields();
                }
                if (type == General.TableType.Comp)
                {
                    Fields = (new MDBStationResults("DB_CMP", table)).getTableFields();
                }

                SaveTableFields(table, type, Fields);
            }

            return Fields;
        }

        public Boolean SaveTableFields(string id,General.TableType type, HashSet<string> fields)
        {
            BsonDocument TableFields;

            if ((fields.Count == 0) && (type == General.TableType.Dev)) fields = (new MDBStationResults("DB_PRM", id)).getTableFields();

            TableFields = new BsonDocument { { "_id", type.ToString() + "_" + id } };
            TableFields.Add("date", DateTime.Now.ToString("yyyyMMdd"));
            BsonArray bArray = new BsonArray();
            foreach (string  field in fields)
            {
                bArray.Add(field);
            }
            TableFields.Add("Fields", bArray);
            MDBSaveRec(TableFields);
            return true;
        }

        public Dictionary<string, HashSet<string>> GetTablesFieldsDictionary(General.TableType type,string[] tables)
        {
            Dictionary<string, HashSet<string>> dictionaryFields = new Dictionary<string, HashSet<string>>();
            foreach (string table in tables)
            {
                dictionaryFields.Add(table, ReadTableFields(table, type));
            }
            return dictionaryFields;
        }
    }
}