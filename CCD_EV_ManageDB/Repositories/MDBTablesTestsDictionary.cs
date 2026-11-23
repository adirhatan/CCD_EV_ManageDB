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
    public class MDBTablesTestsDictionary : MDBClient
    {
        public MDBTablesTestsDictionary()
            : base("DB_TMP", "TablesTestsDictionary")
        { }

        public Dictionary<string, HashSet<string>> ReadTableTestsDictionary(string table,General.TableType type,string TestField)
        {
            Dictionary<string, HashSet<string>> dictionaryFields = new Dictionary<string, HashSet<string>>();
            BsonDocument Doc = collection.FindOneById(type.ToString() + "_" + table + "_" + TestField);
            if ((Doc != null) && (Doc.GetElement("date").Value.ToString() == (DateTime.Now.ToString("yyyyMMdd"))))
            {
                foreach (BsonDocument Test  in Doc.GetElement("Tests").Value.AsBsonArray)
                {
                    HashSet<string> Fields = new HashSet<string>();
                    foreach (string field in Test.GetElement("TestFields").Value.AsBsonArray)
                    {
                        Fields.Add(field);
                    }
                    dictionaryFields.Add(Test.GetElement("_id").Value.ToString(), Fields);
                }
            }
            else
            {
                //Take values from both DB_PRM,DB_TMP
                dictionaryFields = (new MDBStationResults("DB_PRM", table)).getTableDictionaryTestsFields(TestField);
                Dictionary<string, HashSet<string>> dictionaryFieldsTmp = (new MDBStationResults("DB_TMP", table)).getTableDictionaryTestsFields(TestField);
                foreach (var pair in dictionaryFieldsTmp)
                {
                    if (dictionaryFields.ContainsKey(pair.Key)) foreach (string field in pair.Value) dictionaryFields[pair.Key].Add(field);
                    else dictionaryFields.Add(pair.Key, pair.Value);
                }
                if ((dictionaryFields.Count > 1) && (dictionaryFields.ContainsKey("noTestName"))) dictionaryFields.Remove("noTestName");
                SaveTableTestsDictionary(table, type, TestField, dictionaryFields);
            }

            return dictionaryFields;
        }

        public Boolean SaveTableTestsDictionary(string id,General.TableType type, string TestField, Dictionary<string, HashSet<string>> dictionaryFields)
        {
            BsonDocument Result;
            Result = new BsonDocument { { "_id", type.ToString() + "_" + id + "_" + TestField } };
            Result.Add("date", DateTime.Now.ToString("yyyyMMdd"));
            BsonArray bArray = new BsonArray();
            foreach (var pair in dictionaryFields)
            {
                BsonDocument Test;
                Test = new BsonDocument { { "_id", pair.Key } };
                Test.Add("TestFields",new BsonArray(pair.Value));
                bArray.Add(Test);
            }
            Result.Add("Tests", bArray);
            MDBSaveRec(Result);
            return true;
        }
    }
}