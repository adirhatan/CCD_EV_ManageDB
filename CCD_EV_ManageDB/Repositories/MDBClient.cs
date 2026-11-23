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
    public class MDBClient
    {
  
        protected static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        protected static string DB_PRM_Arch { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM_Arch"]; } }
        protected static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }
        private static string DB_CMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP"]; } }
        private static string DB_CMP_Arch { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP_Arch"]; } }
        protected static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }

        protected MongoClient client;
        protected MongoServer server;
        protected MongoDatabase database;
        protected MongoCollection<BsonDocument> collection;
        protected string dbname;
        protected string colname;


        //Usage: (<"DB_PRM/DB_TMP">,<colname>)
        protected MDBClient(string _dbname, string _colname)
        {
            if ((_dbname=="DB_PRM") || (_dbname==DB_PRM)) dbname = DB_PRM;
            if ((_dbname == "DB_PRM_Arch") || (_dbname == DB_PRM_Arch)) dbname = DB_PRM_Arch;
            if ((_dbname == "DB_TMP") || (_dbname == DB_TMP)) dbname = DB_TMP;
            if ((_dbname == "DB_CMP") || (_dbname == DB_CMP)) dbname = DB_CMP;
            if ((_dbname == "DB_CMP_Arch") || (_dbname == DB_CMP_Arch)) dbname = DB_CMP_Arch;
            colname = _colname;
            
            client = new MongoClient(dbAdress);
            server = client.GetServer();
            database = server.GetDatabase(dbname);
            collection = database.GetCollection<BsonDocument>(colname);
        }

        protected bool MDBSaveRec(BsonDocument rec)
        {
            WriteConcernResult Action = collection.Save(rec);
            return Action.Ok;
        }

        protected bool MDBDeleteID(BsonValue id)
        {
            WriteConcernResult Action = collection.Remove(Query.EQ("_id", id));
            return Action.Ok;
        }

        protected MongoCursor MDBReadAllMatchedIDRecs(string id)
        {
            var qList = new List<IMongoQuery>();
            SortByBuilder sort = new SortByBuilder(); ;
            MongoCursor Cur;

            qList.Add(Query.Matches("_id", id));
            sort.Ascending(new string[] { "_id" });
            try
            {
                var query = Query.And(qList);
                Cur = collection.Find(query);
                Cur.SetSortOrder(sort);
            }
            catch (Exception e)
            {
                return null;
            }
            return Cur;
        }

        string MDBNormalizeMatches(string query)
        {
            query = query.Replace("(", @"\(").Replace(")", @"\)");
            return query;
        }

        public MongoCursor MDBFind(string query, string SearchOP, string SortDirection, string SortStr)
        {
            List<string> QueryTriplets =new List<string>();
            System.Text.StringBuilder res = new System.Text.StringBuilder();
            MongoCursor ResCursor = null;
            var qList = new List<IMongoQuery>();
            SortByBuilder Sort = new SortByBuilder();

            String[] qarr = query.Split(';');
            for (int i = 0; i < qarr.Length; i += 3)
            {
                QueryTriplets.Add((qarr[i]) + ";" + (qarr[i + 1]) + ";" + (qarr[i + 2]));
            }
            foreach (string triplet in QueryTriplets)
            {
                String[] tarr = triplet.Split(';');
                switch (tarr[0])
                {
                    case "LT":
                        if ((tarr[1] == "SessionStart") || (tarr[1] == "TestStart") || (tarr[1] == "TestEnd"))
                        {
                            string tmp = (tarr[2] + "00000000000000").Substring(0, 14);
                            qList.Add(Query.LT("HIDDEN_" + tarr[1], new BsonDouble(Convert.ToDouble(tmp))));
                        }
                        else qList.Add(Query.LT(tarr[1], new BsonDouble(Convert.ToDouble(tarr[2]))));
                        break;
                    case "GT":
                        if ((tarr[1] == "SessionStart") || (tarr[1] == "TestStart") || (tarr[1] == "TestEnd"))
                        {
                            string tmp = (tarr[2] + "00000000000000").Substring(0, 14);
                            qList.Add(Query.GT("HIDDEN_" + tarr[1], new BsonDouble(Convert.ToDouble(tmp))));
                        }
                        else qList.Add(Query.GT(tarr[1], new BsonDouble(Convert.ToDouble(tarr[2]))));
                        break;
                    case "Matches":
                        qList.Add(Query.Matches(tarr[1], MDBNormalizeMatches(tarr[2])));
                        break;
                    case "NotMatches":
                        qList.Add(Query.Not(Query.Matches(tarr[1], MDBNormalizeMatches(tarr[2]))));
                        break;
                    case "EQ":
                        Double v;
                        if ((Double.TryParse(tarr[2].Trim(), out v)) && (tarr[1] != "UnitId") && (tarr[1] != "BoardNumber") && (tarr[1] != "board_id"))
                        {
                            qList.Add(Query.EQ(tarr[1], new BsonDouble(v)));
                        }
                        else qList.Add(Query.EQ(tarr[1], tarr[2]));
                        break;
                    case "ExistNotEmpty":
                        qList.Add(Query.Exists(tarr[1]));
                        qList.Add(Query.NE(tarr[1], ""));
                        break;
                    case "In":
                        List<BsonString> lin = new List<BsonString>();
                        foreach (string s in tarr[2].Split(',')) lin.Add(new BsonString(s));
                        qList.Add(Query.In(tarr[1], lin));
                        break;
                    case "NotIn":
                        List<BsonString> lnin = new List<BsonString>();
                        foreach (string s in tarr[2].Split(',')) lnin.Add(new BsonString(s));
                        qList.Add(Query.NotIn(tarr[1], lnin));
                        break;
                    case "Exists":
                        qList.Add(Query.Exists(tarr[1]));
                        break;
                }
            }
            switch (SearchOP)
            {
                case "AND":
                    var _query = Query.And(qList);
                    ResCursor = collection.Find(_query);
                    break;
                case "OR":
                    _query = Query.Or(qList);
                    ResCursor = collection.Find(_query);
                    break;
                default:
                    SearchOP = "AND";
                    _query = Query.And(qList);
                    ResCursor = collection.Find(_query);
                    break;

            }
            if (SortDirection == "ASC") Sort.Ascending(new string[] { SortStr });
            else Sort.Descending(new string[] { SortStr });
            ResCursor.SetSortOrder(Sort);
            return ResCursor;
        }

        public BsonDocument MDBFindOne(string query, string SearchOP, string SortDirection, string SortStr)
        {
            BsonDocument result = null;
            MongoCursor ResCursor = this.MDBFind(query, SearchOP, SortDirection, SortStr);
            ResCursor.SetLimit(1);
            foreach (BsonDocument doc in ResCursor) result = doc;
            return result;
        }

        public List<BsonValue>  MDBFindDistinct(string query, string SearchOP, string SortDirection, string SortStr, string field)
        {
            List<BsonValue> results = new List<BsonValue>();
            List<string> QueryTriplets = new List<string>();
            System.Text.StringBuilder res = new System.Text.StringBuilder();
            var qList = new List<IMongoQuery>();
            SortByBuilder Sort = new SortByBuilder();

            String[] qarr = query.Split(';');
            for (int i = 0; i < qarr.Length; i += 3)
            {
                QueryTriplets.Add((qarr[i]) + ";" + (qarr[i + 1]) + ";" + (qarr[i + 2]));
            }
            foreach (string triplet in QueryTriplets)
            {
                String[] tarr = triplet.Split(';');
                switch (tarr[0])
                {
                    case "LT":
                        if ((tarr[1] == "SessionStart") || (tarr[1] == "TestStart") || (tarr[1] == "TestEnd"))
                        {
                            string tmp = (tarr[2] + "00000000000000").Substring(0, 14);
                            qList.Add(Query.LT("HIDDEN_" + tarr[1], new BsonDouble(Convert.ToDouble(tmp))));
                        }
                        else qList.Add(Query.LT(tarr[1], new BsonDouble(Convert.ToDouble(tarr[2]))));
                        break;
                    case "GT":
                        if ((tarr[1] == "SessionStart") || (tarr[1] == "TestStart") || (tarr[1] == "TestEnd"))
                        {
                            string tmp = (tarr[2] + "00000000000000").Substring(0, 14);
                            qList.Add(Query.GT("HIDDEN_" + tarr[1], new BsonDouble(Convert.ToDouble(tmp))));
                        }
                        else qList.Add(Query.GT(tarr[1], new BsonDouble(Convert.ToDouble(tarr[2]))));
                        break;
                    case "Matches":
                        qList.Add(Query.Matches(tarr[1], tarr[2]));
                        break;
                    case "NotMatches":
                        qList.Add(Query.Not(Query.Matches(tarr[1], tarr[2])));
                        break;
                    case "EQ":
                        Double v;
                        if ((Double.TryParse(tarr[2].Trim(), out v)) && (tarr[1] != "UnitId") && (tarr[1] != "BoardNumber") && (tarr[1] != "board_id"))
                        {
                            qList.Add(Query.EQ(tarr[1], new BsonDouble(v)));
                        }
                        else qList.Add(Query.EQ(tarr[1], tarr[2]));
                        break;
                    case "ExistNotEmpty":
                        qList.Add(Query.Exists(tarr[1]));
                        qList.Add(Query.NE(tarr[1], ""));
                        break;
                    case "In":
                        List<BsonString> lin = new List<BsonString>();
                        foreach (string s in tarr[2].Split(',')) lin.Add(new BsonString(s));
                        qList.Add(Query.In(tarr[1], lin));
                        break;
                    case "NotIn":
                        List<BsonString> lnin = new List<BsonString>();
                        foreach (string s in tarr[2].Split(',')) lnin.Add(new BsonString(s));
                        qList.Add(Query.NotIn(tarr[1], lnin));
                        break;
                    case "Exists":
                        qList.Add(Query.Exists(tarr[1]));
                        break;
                }
            }
            switch (SearchOP)
            {
                case "AND":
                    var _query = Query.And(qList);
                    results = collection.Distinct<BsonValue>(field, _query).ToList<BsonValue>();
                    break;
                case "OR":
                    _query = Query.Or(qList);
                    results = collection.Distinct<BsonValue>(field, _query).ToList<BsonValue>();
                    break;
                default:
                    SearchOP = "AND";
                    _query = Query.And(qList);
                    results = collection.Distinct<BsonValue>(field, _query).ToList<BsonValue>();
                    break;
            }
            //if (SortDirection == "ASC") Sort.Ascending(new string[] { SortStr });
            //else Sort.Descending(new string[] { SortStr });
            //ResCursor.SetSortOrder(Sort);
            return results;
        }

        protected Boolean MDBMoveCurToDB(MongoCursor ResCursor, string newDB)
        {
            //TODO: Error handling
            MongoDatabase Pdatabase = server.GetDatabase(newDB);
            MongoCollection<BsonDocument> Pcollection = Pdatabase.GetCollection<BsonDocument>(ResCursor.Collection.Name);

            foreach (BsonDocument item in ResCursor)
            {
                try
                {
                    if (IsRecExist(item, newDB)) return false;
                }
                catch (Exception e)
                {
                    //if e  
                    continue;
                }
            }
            foreach (BsonDocument item in ResCursor)
            {
                try
                {
                    Pcollection.Save(item);
                }
                catch (Exception e)
                {
                    //if e  
                    continue;
                }
                if (IsRecExist(item, newDB)) collection.Remove(Query.EQ("_id", item.GetElement("_id").Value));
            }
            return true;
        }
        protected Boolean IsRecExist(BsonDocument rec, string DB)
        {
            MongoDatabase Ndatabase = server.GetDatabase(DB);
            MongoCollection<BsonDocument> Ncollection = Ndatabase.GetCollection<BsonDocument>(colname);

            BsonDocument recExist = null;
            recExist = Ncollection.FindOneById(rec.GetElement("_id").Value);
            if (recExist != null) return true;
            return false;
        }

        protected Boolean dbUpdate(BsonDocument rec)
        {
            WriteConcernResult Action = collection.Save(rec);
            return Action.Ok;
        }


    }
}