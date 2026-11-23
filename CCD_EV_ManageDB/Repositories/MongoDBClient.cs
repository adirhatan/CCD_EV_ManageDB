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
using System.Text.RegularExpressions;

namespace CCD_EV_ManageDB.Repositories
{
    public class MongoDBClient
    {
        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }
        private static string DB_REC { get { return System.Configuration.ConfigurationManager.AppSettings["DB_REC"]; } }

        private MongoClient client;
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection<BsonDocument> collection;
        private string dbaddr;
        private string dbname;
        private string colname;

        public MongoDBClient(string dbaddr, string dbname, string colname)
        {
            this.dbaddr = dbaddr;
            this.dbname = dbname;
            this.colname = colname;


            client = new MongoClient(dbaddr);
            server = client.GetServer();
            database = server.GetDatabase(dbname);
            collection = database.GetCollection<BsonDocument>(colname);

            try
            {
                BsonDocument rectest = new BsonDocument { { "test", "test" } };
                collection.Insert(rectest);
                BsonElement id_elem = rectest.GetElement("_id");
                BsonDocument recExist = collection.FindOneById(id_elem.Value);
                //var query = Query.EQ("_id", id_elem.Value);
                collection.Remove(Query.EQ("_id", id_elem.Value));
            }
            catch (Exception e)
            {
                if (e is MongoConnectionException || e is WriteConcernException)
                {
                    //do something
                }

                throw;
            }
        }


        public bool dbInsert(string id, string[,] values, out bool errorOccurred, out int errorCode, out String errorMsg)
        {
            errorOccurred = false;
            errorCode = 0;
            errorMsg = String.Empty;

            BsonDocument recIn = buildBsonDoc(id, values);
            try
            {
                //collection.Insert(recIn);
                collection.Save(recIn);
            }
            catch (Exception e)
            {
                //can not connect DB 
                if (e is MongoConnectionException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }

                if (e is WriteConcernException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }
            }
            return true;
        }

        public bool dbupdate(string id, string[,] values, out bool errorOccurred, out int errorCode, out String errorMsg)
        {
            errorOccurred = false;
            errorCode = 0;
            errorMsg = String.Empty;
            try
            {
                BsonDocument recIn = buildBsonDoc(id, values);
                BsonDocument recExist = collection.FindOneById(id);
                if (recExist == null)
                {
                    //throw (new Exception("Cannot update, id " + id + " does not exist"));
                    recExist = recIn;
                }
                else
                {
                    foreach (BsonElement element in recIn.Elements)
                    {
                        if (recExist.Contains(element.Name)) recExist.Set(element.Name, element.Value);
                        else recExist.Add(element);
                    }
                }
                collection.Save(recExist);
                return true;
            }
            catch (Exception e)
            {
                //can not connect DB 
                if (e is MongoConnectionException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }

                if (e is WriteConcernException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }
            }
            return true;
        }

        private BsonDocument buildBsonDoc(string id, string[,] values)
        {
            BsonDocument recIn = new BsonDocument { { "_id", id } };
            recIn.AllowDuplicateNames = false;

            int x = 0;
            string fName = null;
            BsonValue BVal = null;
            //need to find a beter way to iterate the array and ignore/top when  empty cells 
            foreach (string str in values)
            {
                if (x == 0)
                {
                    fName = str;
                    x = 1;
                }
                else
                {
                    double d;
                    if ((fName != "UnitId") && (fName != "BoardNumber") && (double.TryParse(str, out d))) BVal = d;
                    else BVal = str;
                    x = 0;
                    if (!fName.Equals(""))
                    {
                        if (recIn.Contains(fName)) recIn.Set(fName, BVal);
                        else recIn.Add(fName, BVal);
                    }
                }
            }
            return recIn;
        }

        public void disconnectDB(out bool errorOccurred, out int errorCode, out String errorMsg)
        {
            errorOccurred = false;
            errorCode = 0;
            errorMsg = String.Empty;
            try
            {
                server.Disconnect();
            }
            catch (Exception e)
            {
                errorOccurred = true;
                errorMsg = e.Message;
            }
        }

        public void disconnectDB()
        {
            server.Disconnect();
        }

        public bool dbDelete(DBsearch DBS, string id, bool delFiles, out bool errorOccurred, out int errorCode, out String errorMsg)
        {

            this.dbname = DBS.db;
            this.colname = DBS.col;

            database = server.GetDatabase(DBS.db);
            collection = database.GetCollection<BsonDocument>(DBS.col);

            errorOccurred = false;
            errorCode = 0;
            errorMsg = String.Empty;

            try
            {
                BsonDocument recExist = collection.FindOneById(id);
                if (recExist != null)
                {
                    MongoCursor ResCursor = collection.Find(Query.EQ("_id", new BsonString(id)));

                    if (delFiles)
                    {
                        foreach (BsonDocument item in ResCursor)
                        {
                            foreach (BsonElement field in item)
                            {
                                if ((field.Name.Length > 4) && (field.Name.Substring(0, 4) == "FILE") ||
                                    (field.Name.Length > 6) && (field.Name.Substring(0, 6) == "ATTACH"))
                                {
                                    try
                                    {
                                        if (field.Value != null) File.Delete(field.Value.ToString());
                                    }
                                    catch (Exception ex)
                                    {
                                        //can not delete file, do nothing
                                    }
                                }
                            }
                        }
                    }
                    WriteConcernResult res = collection.Remove(Query.EQ("_id", new BsonString(id)));
                }
                else
                {
                    //do something? record already deleted or wrong id was sent
                    return false;
                }
            }
            catch (Exception e)
            {
                //can not connect DB 
                if (e is MongoConnectionException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }

                if (e is WriteConcernException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }
            }
            return true;
        }
        string MDBNormalizeMatches(string query)
        {
            query = query.Replace("(", @"\(").Replace(")", @"\)");
            return query;
        }
        public void genericFind(DBsearch DBS, int limit)
        {
            this.dbname = DBS.db;
            this.colname = DBS.col;

            database = server.GetDatabase(DBS.db);
            collection = database.GetCollection<BsonDocument>(DBS.col);

            DBS.QueryTriplets.Clear();
            DBS.Sessions.Clear();
            DBS.uSession = null;
            DBS.Fields.Clear();

            System.Text.StringBuilder res = new System.Text.StringBuilder();
            var qList = new List<IMongoQuery>();
            String[] qarr = DBS.Query.Split(';');
            for (int i = 0; i < qarr.Length; i += 3)
            {
                DBS.QueryTriplets.Add((qarr[i]) + ";" + (qarr[i + 1]) + ";" + (qarr[i + 2]));
            }
            foreach (string triplet in DBS.QueryTriplets)
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
                        if ((Double.TryParse(tarr[2].Trim(), out v)) && (tarr[1] != "UnitId") && (tarr[1] != "BoardNumber"))
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
                }
            }
            switch (DBS.SearchOP)
            {
                case "AND":
                    var query = Query.And(qList);
                    DBS.ResCursor = collection.Find(query);
                    break;
                case "OR":
                    query = Query.Or(qList);
                    DBS.ResCursor = collection.Find(query);
                    break;
                default:
                    DBS.SearchOP = "AND";
                    query = Query.And(qList);
                    DBS.ResCursor = collection.Find(query);
                    break;

            }
            if (DBS.SortDirection == "ASC") DBS.Sort.Ascending(new string[] { DBS.SortStr });
            else DBS.Sort.Descending(new string[] { DBS.SortStr });
            DBS.ResCursor.SetSortOrder(DBS.Sort);
            DBS.ResCursor.SetSkip(DBS.Skip);
            DBS.Fields.Add(DBS.SortStr);

            if ((DBS.FieldsStr != null) && (DBS.FieldsStr != "") && (DBS.FieldsInc))
            {
                List<String> tmpFields = new List<string>(DBS.FieldsStr.Split(';'));
                if (!tmpFields.Contains(DBS.SortStr)) tmpFields.Add(DBS.SortStr);
                DBS.ResCursor.SetFields(Fields.Include(tmpFields.ToArray()));
            }
            if ((DBS.FieldsStr != null) && (DBS.FieldsStr != "") && (DBS.FieldsExc)) DBS.ResCursor.SetFields(Fields.Exclude(DBS.FieldsStr.Split(';')));

            //new
            MongoCursor L_ResCursor = DBS.ResCursor.Clone((new BsonDocument()).GetType());
            DBS.ResCursor.SetLimit(limit);

            foreach (BsonDocument item in L_ResCursor)
            {
                DBS.Sessions.Add(DBS.GetSesFromDBRec(item));
                foreach (BsonElement field in item)
                {
                    if ((field.Name.Length > 6) && (field.Name.Substring(0, 6) == "HIDDEN")) continue;
                    else DBS.Fields.Add(field.Name);
                }
            }

            //order fields if selected
            if ((DBS.FieldsStr != null) && (DBS.FieldsStr != "") && (DBS.FieldsInc))
            {
                List<String> tmpFields = new List<string>(DBS.FieldsStr.Split(';'));
                HashSet<string> tmpFields1 = new HashSet<string>();
                tmpFields1.Add(DBS.SortStr);
                foreach (string f in tmpFields) tmpFields1.Add(f);
                DBS.Fields = tmpFields1;
            }

        }

        public void setDBandCol(string dbname, string colname, DBsearch DBS)
        {
            this.dbname = dbname;
            this.colname = colname;

            database = server.GetDatabase(dbname);
            collection = database.GetCollection<BsonDocument>(colname);

            DBS.Sessions.Clear();
            DBS.Fields.Clear();
            DBS.ResCursor = collection.FindAll();
            if (DBS.SortDirection == "ASC") DBS.Sort.Ascending(new string[] { DBS.SortStr });
            else DBS.Sort.Descending(new string[] { DBS.SortStr });
            DBS.ResCursor.SetSortOrder(DBS.Sort);

            MongoCursor L_ResCursor = DBS.ResCursor.Clone((new BsonDocument()).GetType());

            L_ResCursor.SetSortOrder(DBS.Sort);
            //new TODO:correct limit
            DBS.ResCursor.SetLimit(100);

            int cnt = 0;
            foreach (BsonDocument item in L_ResCursor)
            {
                cnt++;
                try
                {
                    DBS.Sessions.Add(DBS.GetSesFromDBRec(item));
                }
                catch (Exception e)
                {
                    //if e  
                    continue;
                }
                //Avoid scanning full collection, stop building default sessions and fields lists after 50 sessions or 5000 results
                if ((DBS.Sessions.Count > 100) || (cnt > 10000)) break;
                foreach (BsonElement field in item)
                {
                    if ((field.Name.Length > 6) && (field.Name.Substring(0, 6) == "HIDDEN")) continue;
                    else DBS.Fields.Add(field.Name);
                }
            }

        }

        public bool fixLoadToPRM()
        {
            MongoCursor L_ResCursor;
            foreach (string colName in database.GetCollectionNames())
            {
                collection = database.GetCollection<BsonDocument>(colName);
                L_ResCursor = collection.Find(Query.EQ("ProjectName", "TR"));
                foreach (BsonDocument item in L_ResCursor)
                {
                    string SessionStart = item.GetValue("HIDDEN_SessionStart", "0").ToString();
                    Double SessionStartD = Convert.ToDouble(SessionStart);


                    if (string.IsNullOrEmpty((item.GetValue("HIDDEN_LoadToPRM", "").ToString())))
                    {
                        item.Set("HIDDEN_LoadToPRM", SessionStartD);
                    }
                    if (string.IsNullOrEmpty((item.GetValue("Indicator", "").ToString())))
                    {
                        item.Set("Indicator", "Y");
                    }
                    try
                    {
                        collection.Save(item);
                    }
                    catch (Exception e)
                    {
                        //if e  
                        continue;
                    }
                }
            }
            return true;

        }

        public bool fixLoadToPRM_Shavuot()
        {
            MongoCursor L_ResCursor;
            var qList = new List<IMongoQuery>();
            qList.Add(Query.EQ("ProjectName", "BB"));
            qList.Add(Query.LT("HIDDEN_LoadToPRM", new BsonDouble(Convert.ToDouble(20180522000000))));
            qList.Add(Query.GT("HIDDEN_LoadToPRM", new BsonDouble(Convert.ToDouble(20180521000000))));
            foreach (string colName in database.GetCollectionNames())
            {
                collection = database.GetCollection<BsonDocument>(colName);
                L_ResCursor = collection.Find(Query.And(qList));
                foreach (BsonDocument item in L_ResCursor)
                {
                    string SessionStart = item.GetValue("HIDDEN_SessionStart", "0").ToString();
                    Double SessionStartD = Convert.ToDouble(SessionStart);
                    item.Set("HIDDEN_LoadToPRM", SessionStartD);

                    try
                    {
                        collection.Save(item);
                    }
                    catch (Exception e)
                    {
                        //if e  
                        continue;
                    }
                }
            }
            return true;

        }
        public Boolean loadToPRM(DBsearch DBS)
        {
            //TODO: Error handling
            MongoDatabase Pdatabase = server.GetDatabase(DB_PRM);
            MongoCollection<BsonDocument> Pcollection = Pdatabase.GetCollection<BsonDocument>(DBS.col);
            string nowstr = DateTime.Now.ToString("yyyyMMddHHmmss");
            Double nowstrDbl = Convert.ToDouble(nowstr);


            foreach (BsonDocument item in DBS.ResCursor)
            {
                try
                {
                    if (IsRecExist(item, DB_PRM)) return false;
                }
                catch (Exception e)
                {
                    //if e  
                    continue;
                }
            }
            foreach (BsonDocument item in DBS.ResCursor)
            {

                if (string.IsNullOrEmpty((item.GetValue("HIDDEN_LoadToPRM", "").ToString())))
                {
                    item.Set("HIDDEN_LoadToPRM", nowstrDbl);
                }
                if (string.IsNullOrEmpty((item.GetValue("Indicator", "").ToString())))
                {
                    item.Set("Indicator", "Y");
                }
                try
                {
                    Pcollection.Save(item);
                }
                catch (Exception e)
                {
                    //if e  
                    continue;
                }
                if (IsRecExist(item, DB_PRM)) collection.Remove(Query.EQ("_id", item.GetElement("_id").Value));
            }
            return true;
        }
        
        public Boolean moveToREC(DBsearch DBS)
        {
            //TODO: Error handling
            MongoDatabase Rdatabase = server.GetDatabase(DB_REC);
            MongoCollection<BsonDocument> Rcollection = Rdatabase.GetCollection<BsonDocument>(DBS.col);
            string nowstr = DateTime.Now.ToString("yyyyMMddHHmmss");
            Double nowstrDbl = Convert.ToDouble(nowstr);


            foreach (BsonDocument item in DBS.ResCursor)
            {
                try
                {
                    if (IsRecExist(item, DB_REC)) return false;
                }
                catch (Exception e)
                {
                    //if e  
                    continue;
                }
            }
            foreach (BsonDocument item in DBS.ResCursor)
            {

                if (string.IsNullOrEmpty((item.GetValue("HIDDEN_MoveToREC", "").ToString())))
                {
                    item.Set("HIDDEN_MoveToREC", nowstrDbl);
                }
                //if (string.IsNullOrEmpty((item.GetValue("Indicator", "").ToString())))
                //{
                //    item.Set("Indicator", "Y");
                //}
                try
                {
                    Rcollection.Save(item);
                }
                catch (Exception e)
                {
                    //if e  
                    continue;
                }
                if (IsRecExist(item, DB_REC)) collection.Remove(Query.EQ("_id", item.GetElement("_id").Value));
            }
            return true;
        }

        public bool moveFromREC2TMP(DBsearch DBS)
        {
            //TODO: Error handling
            MongoDatabase Rdatabase = server.GetDatabase(DB_TMP);
            MongoCollection<BsonDocument> Rcollection = Rdatabase.GetCollection<BsonDocument>(DBS.col);
            string nowstr = DateTime.Now.ToString("yyyyMMddHHmmss");
            Double nowstrDbl = Convert.ToDouble(nowstr);


            foreach (BsonDocument item in DBS.ResCursor)
            {
                try
                {
                    if (IsRecExist(item, DB_TMP)) return false;
                }
                catch (Exception e)
                {
                    //if e  
                    continue;
                }
            }
            foreach (BsonDocument item in DBS.ResCursor)
            {

                item.Remove("HIDDEN_MoveToREC");

                try
                {
                    Rcollection.Save(item);
                }
                catch (Exception e)
                {
                    //if e  
                    continue;
                }
                if (IsRecExist(item, DB_TMP)) collection.Remove(Query.EQ("_id", item.GetElement("_id").Value));
            }
            return true;
        }

        public bool dbMoveToREC(DBsearch DBS, string id, bool delFiles, out bool errorOccurred, out int errorCode, out String errorMsg)
        {

            this.dbname = DBS.db;
            this.colname = DBS.col;

            database = server.GetDatabase(DBS.db);
            collection = database.GetCollection<BsonDocument>(DBS.col);
            DBsearch tmpDBS = DBS;

            errorOccurred = false;
            errorCode = 0;
            errorMsg = String.Empty;

            try
            {
                BsonDocument recExist = collection.FindOneById(id);
                if (recExist != null)
                {
                    DBS.ResCursor = collection.Find(Query.EQ("_id", new BsonString(id)));
                    errorOccurred = this.moveToREC(DBS);
                }
                else
                {
                    //do something? record already deleted or wrong id was sent
                    return false;
                }
            }
            catch (Exception e)
            {
                //can not connect DB 
                if (e is MongoConnectionException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }

                if (e is WriteConcernException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }
            }
            return true;
        }

        public bool dbRestoreToTMP(DBsearch DBS, string id, bool delFiles, out bool errorOccurred, out int errorCode, out String errorMsg)
        {

            this.dbname = DBS.db;
            this.colname = DBS.col;

            database = server.GetDatabase(DBS.db);
            collection = database.GetCollection<BsonDocument>(DBS.col);
            DBsearch tmpDBS = DBS;

            errorOccurred = false;
            errorCode = 0;
            errorMsg = String.Empty;

            try
            {
                BsonDocument recExist = collection.FindOneById(id);
                if (recExist != null)
                {
                    DBS.ResCursor = collection.Find(Query.EQ("_id", new BsonString(id)));
                    errorOccurred = this.moveFromREC2TMP(DBS);
                }
                else
                {
                    //do something? record already deleted or wrong id was sent
                    return false;
                }
            }
            catch (Exception e)
            {
                //can not connect DB 
                if (e is MongoConnectionException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }

                if (e is WriteConcernException)
                {
                    errorOccurred = true;
                    errorMsg = e.Message;
                    //errorCode = e.ErrorCode;
                    return false;
                }
            }
            return true;
        }
        public Boolean IsRecExist(BsonDocument rec, string DB)
        {
            MongoDatabase Ndatabase = server.GetDatabase(DB);
            MongoCollection<BsonDocument> Ncollection = Ndatabase.GetCollection<BsonDocument>(colname);

            BsonDocument recExist = null;
            recExist = Ncollection.FindOneById(rec.GetElement("_id").Value);
            if (recExist != null) return true;
            return false;
        }

        public string[] UserDBRoles(string userId)
        {
            List<string> rolesList = new List<string>();
            BsonDocument userDBrec = collection.FindOneById(new BsonString(userId));


            foreach (BsonElement elem in userDBrec)
            {
                if ((elem.Name.ToString().Length > 4) && (elem.Name.ToString().Substring(0, 4) == "role")) rolesList.Add(elem.Value.ToString());
            }

            return (rolesList.ToArray());
        }

        public void AddUserDBRole(string userId, string role)
        {
            database = server.GetDatabase(DB_PRM);
            collection = database.GetCollection<BsonDocument>("users");

            BsonDocument userDBrec = collection.FindOneById(new BsonString(userId));
            if (userDBrec == null)
            {
                var document = new BsonDocument(true);
                document.Add("_id", userId);
                document.Add("role", role);
                document.Add("role", "DefRole");
                collection.Insert(document);
            }
            else
            {
                List<string> rolesList = new List<string>();
                foreach (BsonElement elem in userDBrec)
                {
                    if (elem.Name == "role") rolesList.Add(elem.Value.ToString());
                }
                if (!rolesList.Contains(role)) collection.Save(userDBrec.Add("role", role));
            }
        }

        public List<userCCDEV> GetAllUsers()
        {
            List<userCCDEV> users = new List<userCCDEV>();
            database = server.GetDatabase(DB_PRM);
            collection = database.GetCollection<BsonDocument>("users");
            MongoCursor ResCursor = collection.FindAll();
            foreach (BsonDocument doc in ResCursor)
            {
                userCCDEV user = new userCCDEV();
                foreach (BsonElement elem in doc)
                {
                    if ((elem.Name.ToString().Length > 4) && (elem.Name.ToString().Substring(0, 4) == "role")) user.roles.Add(elem.Value.ToString());
                    if (elem.Name == "_id") user.id = elem.Value.ToString();
                }
                users.Add(user);
            }
            return (users);

        }
        public Boolean ValidateUser(string username, string password)
        {
            database = server.GetDatabase(DB_PRM);
            collection = database.GetCollection<BsonDocument>("users");
            try
            {
                BsonDocument userDBrec = collection.FindOneById(new BsonString(username));
                if (userDBrec == null) return false;
                if (password == userDBrec.GetElement("pass").Value.ToString()) return true;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }

        }
        public Boolean SaveUserQuery(string username, string qName, DBsearch DBS)
        {
            var qList = new List<IMongoQuery>();
            BsonDocument sQuery;
            DBsearchSave saveDBS;
            string id;

            database = server.GetDatabase(DB_PRM);
            collection = database.GetCollection<BsonDocument>("usersQueris");

            id = username + DateTime.Now.ToString("yyyyMMddHHmmss");

            sQuery = new BsonDocument { { "_id", id } };

            sQuery.Add("name", qName);
            saveDBS = new DBsearchSave(DBS);
            sQuery.Add("DBS", saveDBS.ToBsonDocument());
            collection.Save(sQuery);
            return true;
        }

        public DBsearch ReadUserQuery(string _id)
        {
            SortByBuilder sort = new SortByBuilder(); ;
            DBsearchSave saveDBS = new DBsearchSave();
            BsonDocument sQuery;


            database = server.GetDatabase(DB_PRM);
            collection = database.GetCollection<BsonDocument>("usersQueris");
            try
            {
                sQuery = collection.FindOneById(_id);
            }
            catch (Exception e)
            {
                return null;
            }
            if (sQuery != null)
            {
                saveDBS = BsonSerializer.Deserialize<DBsearchSave>(sQuery.GetElement("DBS").Value.ToBsonDocument());
                DBsearch DBS = saveDBS.getNewDBS();
                return DBS;
            }
            else return null;
        }

        public bool DeleteUserQuery(string _id)
        {
            database = server.GetDatabase(DB_PRM);
            collection = database.GetCollection<BsonDocument>("usersQueris");
            try
            {
                WriteConcernResult res = collection.Remove(Query.EQ("_id", new BsonString(_id)));
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public bool RenameUserQuery(string _id, string qName)
        {
            BsonDocument sQuery;


            database = server.GetDatabase(DB_PRM);
            collection = database.GetCollection<BsonDocument>("usersQueris");
            try
            {
                sQuery = collection.FindOne(Query.EQ("_id", new BsonString(_id)));
            }
            catch (Exception e)
            {
                return false;
            }
            if (sQuery != null)
            {
                sQuery.Set("name", qName);
                collection.Save(sQuery);
                return true;
            }
            else return false;
        }

        public MongoCursor ReadUserQueries(string username)
        {
            var qList = new List<IMongoQuery>();
            SortByBuilder sort = new SortByBuilder(); ;
            DBsearchSave saveDBS = new DBsearchSave();
            MongoCursor Cur;

            qList.Add(Query.Matches("_id", username));
            database = server.GetDatabase(DB_PRM);
            collection = database.GetCollection<BsonDocument>("usersQueris");
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


        public void setDBandCol(string dbname, string colname)
        {
            this.dbname = dbname;
            this.colname = colname;

            database = server.GetDatabase(dbname);
            collection = database.GetCollection<BsonDocument>(colname);
        }

        public Boolean dbInsert(BsonDocument rec)
        {
            WriteConcernResult Action = collection.Insert(rec);
            return Action.Ok;
        }


        public void fixHiddenFields(string dbname)
        {
            BsonValue BVal = null;
            double d;

            database = server.GetDatabase(dbname);


            foreach (string colName in database.GetCollectionNames())
            {
                collection = database.GetCollection<BsonDocument>(colName);


                MongoCursor L_ResCursor = collection.FindAll();

                foreach (BsonDocument item in L_ResCursor)
                {
                    bool save = false;
                    string testStart = item.GetValue("TestStart", "").ToString();
                    string h_testStart = item.GetValue("HIDDEN_TestStart", "").ToString();
                    string testEnd = item.GetValue("TestEnd", "").ToString();
                    string h_testEnd = item.GetValue("HIDDEN_TestEnd", "").ToString();
                    string sessionStart = item.GetValue("SessionStart", "").ToString();
                    string h_sessionStart = item.GetValue("HIDDEN_SessionStart", "").ToString();

                    if ((!String.IsNullOrEmpty(testStart)) && (String.IsNullOrEmpty(h_testStart)))
                    {
                        h_testStart = testStart.Replace("_", "");
                        if (double.TryParse(h_testStart, out d)) BVal = d;
                        else BVal = h_testStart;
                        item.Set("HIDDEN_TestStart", BVal);
                        save = true;
                    }
                    if ((!String.IsNullOrEmpty(testEnd)) && (String.IsNullOrEmpty(h_testEnd)))
                    {
                        h_testEnd = testEnd.Replace("_", "");
                        if (double.TryParse(h_testEnd, out d)) BVal = d;
                        else BVal = h_testEnd;
                        item.Set("HIDDEN_TestEnd", BVal);
                        save = true;
                    }
                    if ((!String.IsNullOrEmpty(sessionStart)) && (String.IsNullOrEmpty(h_sessionStart)))
                    {
                        h_sessionStart = sessionStart.Replace("_", "");
                        if (double.TryParse(h_sessionStart, out d)) BVal = d;
                        else BVal = h_sessionStart;
                        item.Set("HIDDEN_SessionStart", BVal);
                        save = true;
                    }

                    if (save) collection.Save(item);
                }
            }

        }

        public void fixNumericFields(string dbname)
        {

            database = server.GetDatabase(dbname);
            //List<string> tables = new List<string>();
            //tables.Add("Test_DP_src0");
            //foreach (string colName in tables)
            foreach (string colName in database.GetCollectionNames())
            {
                if (colName.Substring(0, 4) != "PCIE") continue;
                collection = database.GetCollection<BsonDocument>(colName);


                MongoCursor L_ResCursor = collection.FindAll();

                foreach (BsonDocument item in L_ResCursor)
                {
                    bool save = false;
                    BsonValue bval = item.GetValue("UnitId", "");
                    string UnitIdStr = item.GetValue("UnitId", "").ToString();

                    if (bval.IsDouble)
                    {
                        item.Set("UnitId", UnitIdStr);
                        save = true;
                    }

                    bval = item.GetValue("BoardNumber", "");
                    string BoardNumber = item.GetValue("BoardNumber", "").ToString();

                    if (bval.IsDouble)
                    {
                        item.Set("BoardNumber", BoardNumber);
                        save = true;
                    }

                    bval = item.GetValue("UnitSerialNumber", "");
                    string UnitSStr = item.GetValue("UnitSerialNumber", "").ToString();

                    if (bval.IsDouble)
                    {
                        item.Set("UnitSerialNumber", UnitSStr);
                        save = true;
                    }
                    if (save) collection.Save(item);
                }
            }

        }

        public void fixInAppTestName(string dbname, string colName)
        {
            database = server.GetDatabase(dbname);


            collection = database.GetCollection<BsonDocument>(colName);


            MongoCursor L_ResCursor = collection.FindAll();

            foreach (BsonDocument item in L_ResCursor)
            {
                bool save = false;
                string InAppTestName = item.GetValue("InAppTestName", "").ToString();
                string fixedInAppTestName = Regex.Replace(InAppTestName, @"^Lane \d \-", "");
                if (fixedInAppTestName != InAppTestName)
                {
                    item.Set("InAppTestName", fixedInAppTestName);
                    item.Set("InAppTestNameOrig", InAppTestName);
                    collection.Save(item);
                }

                ////Add spcae due to regex mistake  //string fixedInAppTestName = Regex.Replace(InAppTestName, @"^Lane \d \- ", "");
                //if (InAppTestName.Substring(0, 1) != " ")
                //{
                //    string fixedInAppTestName = " " + InAppTestName;
                //    item.Set("InAppTestName", fixedInAppTestName);
                //    collection.Save(item);
                //}

                if (save) collection.Save(item);
            }


        }


        public int ChangeAttachmentsPath(string dbname, string project, string sourcePathPrefix, string targetPathPrefix)
            {
                int cnt = 0;
                database = server.GetDatabase(dbname);
                //List<string> tables = new List<string>(System.Configuration.ConfigurationManager.AppSettings["MongoTables"].Split(','));
                //for debug. specific tables:
                List<string> tables = new List<string>();

            //TR moved to backup 2020 08
            //tables.Add("TBT3_Legacy_Gen3_Tx");
            //tables.Add("PCIE-RL");
            //tables.Add("USB2.0_TX");
            tables.Add("USB3.1_GEN1_GEN2_RX");
            tables.Add("USB3.1_GEN1_GEN2_TX");

            foreach (string colName in tables)
                //foreach (string colName in database.GetCollectionNames()) //doesn't work for some reason???
                {
                    collection = database.GetCollection<BsonDocument>(colName);
                    MongoCursor L_ResCursor = collection.Find(Query.EQ("ProjectName", project));

                    foreach (BsonDocument item in L_ResCursor)
                    {
                        bool save = false;
                        foreach (BsonElement element in item.Elements)
                        {
                            if ((element.Name.ToUpper().StartsWith("FILE")) || (element.Name.ToUpper().StartsWith("ATTACH")))
                            {
                                string path = element.Value.ToString();
                                if (path.StartsWith(sourcePathPrefix))
                                {
                                    path = path.Replace(sourcePathPrefix,targetPathPrefix);
                                    item.Set(element.Name,path);
                                    save = true;
                                }   
                            }
                        }
                        if (save)
                        {
                            collection.Save(item);
                            cnt++;
                        }
                    }
                }
                return cnt;
            }

        public bool CleanRecycleDB()
        {
            MongoDatabase rec_database = server.GetDatabase(DB_REC);
            List<string> tables = new List<string>(System.Configuration.ConfigurationManager.AppSettings["MongoTables"].Split(','));
            DateTime now = DateTime.Now;
            string nowStr = now.ToString("yyyyMMddHHmmss");
            foreach (string colName in tables)
            {
                try
                {
                    MongoCollection<BsonDocument> rec_collection = rec_database.GetCollection<BsonDocument>(colName);
                    if (rec_collection == null) continue;
                    foreach (BsonDocument doc in rec_collection.FindAll())
                    {
                        string docTimeStr = doc.GetValue("HIDDEN_MoveToREC", "").ToString();
                        if (string.IsNullOrEmpty(docTimeStr)) continue;
                        DateTime docTime = new DateTime(Convert.ToInt32(docTimeStr.Substring(0, 4)), Convert.ToInt32(docTimeStr.Substring(4, 2)), Convert.ToInt32(docTimeStr.Substring(6, 2)));
                        if ((now.Subtract(docTime).TotalDays > 29))
                        {
                            foreach (BsonElement field in doc)
                            {
                                if ((field.Name.Length > 4) && (field.Name.Substring(0, 4) == "FILE") ||
                                    (field.Name.Length > 6) && (field.Name.Substring(0, 6) == "ATTACH"))
                                {
                                    try
                                    {
                                        if (field.Value != null) File.Delete(field.Value.ToString());
                                    }
                                    catch (Exception ex)
                                    {
                                        //can not delete file, do nothing
                                    }
                                }
                            }
                            rec_collection.Remove(Query.EQ("_id", doc.GetElement("_id").Value));
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e is MongoConnectionException || e is WriteConcernException)
                    {
                        //do something
                    }

                    return false;
                }

            }
            return true;
        }

    }
}