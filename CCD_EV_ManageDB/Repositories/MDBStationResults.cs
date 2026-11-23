using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Threading;
using System.Globalization;
using System.Reflection;

namespace CCD_EV_ManageDB.Repositories
{
    public class MDBStationResults : MDBClient
    {
        public MDBStationResults(string _db,string table)
            : base(_db, table)
        { }

        //public MongoCursor ReadCursorAllExecutionsTable(string table, string host)
        //{
        //    MongoCursor Cur = MDBFind("Matches;_id;/" + table +"_" + host, "AND", "DES", "TimeStamp");
        //    return Cur;
        //}

        public MongoCursor ReadCursorAllExecutionsTable(string table, string host)
        {
            MongoCursor Cur = this.MDBReadAllMatchedIDRecs(table + "_" + host);
            return Cur;
        }

        public BsonDocument ReadLastResultForTable(string host)
        {
            BsonDocument Cur = this.MDBFindOne("Matches;HostName;" + host, "AND", "DES", "TestEnd");
            return Cur;
        }

        public MongoCursor ReadCursorLastSession(string host)
        {
            string session = "";
            MongoCursor Cur = null;
            BsonDocument curResult = this.ReadLastResultForTable(host);
            if (curResult != null) session = curResult.GetElement("SessionStart").Value.ToString();
            Cur = this.MDBFind("Matches;HostName;" + host + ";EQ;SessionStart;" + session, "AND", "DES", "_id");
            return Cur;
        }

        public BsonDocument ReadLastResultForTableSession(string host, string session)
        {
            BsonDocument Cur = this.MDBFindOne("Matches;HostName;" + host + ";EQ;SessionStart;" + session, "AND", "DES", "TestEnd");
            return Cur;
        }

        public HashSet<string> getTableFields()
        {
            //TODO write/read to DB or cache
            HashSet<string> Fields = new HashSet<string>();
            MongoCursor L_ResCursor = MDBFind("Matches;_id;/^./", "AND", "DES", "TestEnd");

            int cnt = 0;
            foreach (BsonDocument item in L_ResCursor)
            {
                cnt++;
                //Avoid scanning full collection, stop building fields list after 50000 results
                if ((cnt > 10000)) break;
                foreach (BsonElement field in item)
                {
                    if ((field.Name.Length > 6) && (field.Name.Substring(0, 6) == "HIDDEN")) continue;
                    else Fields.Add(field.Name);
                }
            }

            Fields.Add("Indicator");
            return Fields;

        }

        public Dictionary<string, HashSet<string>> getTableDictionaryTestsFields(string TestField)
        {
            MongoCursor Cur = null;
            Cur = this.MDBFind("Matches;_id;/^./", "AND", "DES", "TestEnd");
            Cur.Limit=100000;

            HashSet<string> Tests = new HashSet<string>();
            Dictionary<string, HashSet<string>> dictionaryFields = new Dictionary<string, HashSet<string>>();
            try
            {
                foreach (BsonDocument item in Cur)
                {
                    string TestName = item.GetValue(TestField, "noTestName").ToString();
                    if (!Tests.Contains(TestName))
                    {
                        Tests.Add(TestName);
                        dictionaryFields.Add(TestName, new HashSet<string>());
                    }
                    //fields
                    foreach (BsonElement field in item)
                    {
                        if ((field.Name.Length > 6) && (field.Name.Substring(0, 6) == "HIDDEN")) continue;
                        else dictionaryFields[TestName].Add(field.Name);
                    }
                }
            }
            catch
            {
                return null;
            }
            return dictionaryFields;
        }

        public bool IndexAllFields()
        {
            HashSet<string> fields = this.getTableFields();
            foreach (string field in fields)
            {
                if (field == "TestEnd") continue;

                //this.collection.EnsureIndex(IndexKeys.Ascending(field));
                this.collection.EnsureIndex(IndexKeys.Ascending(field));
                this.collection.EnsureIndex(IndexKeys.Descending(field));
            }
            return true;
        }

        public bool IndexListOFFields(List<string> fields)
        {
            //HashSet<string> fields = this.getTableFields();
            foreach (string field in fields)
            {
                //if (field == "TestEnd") continue;

                this.collection.EnsureIndex(IndexKeys.Ascending(field));
                this.collection.EnsureIndex(IndexKeys.Descending(field));
            }
            return true;
        }

        public List<string> FindCompLine(string query, DBTmpl CompReportTmpl)
        {
            string[] items = { "" };
            string itemFName = "";
            List<string> PrefixList = new List<string>();
            List<string> SuffixList = new List<string>();
            List<string> ResultsList = new List<string>();
            switch (CompReportTmpl.results)
            {
                case "L0-L3":
                    items = new string[] { "L0", "L1", "L2", "L3" };
                    itemFName = "Lane";
                    break;
                case "L0-L1":
                    items = new string[] { "L0", "L1" };
                    itemFName = "Lane";
                    break;
                case "L2-L3":
                    items = new string[] { "L2", "L3" };
                    itemFName = "Lane";
                    break;
                case "PA-PB":
                    items = new string[] { "PA", "PB" };
                    itemFName = "Port";
                    break;
                case "D0-D2":
                    items = new string[] { "D0", "D1", "D2" };
                    itemFName = "Lane";
                    PrefixList.Add("-");
                    break;
                case "CK-D0-D2":
                    items = new string[] {"CK", "D0", "D1", "D2" };
                    itemFName = "Lane";
                    break;
                case "S":
                case "Clock":
                    items = new string[] { "" };
                    itemFName = "";
                    if (CompReportTmpl.results == "Clock")
                    {
                        SuffixList.Add("-");
                        SuffixList.Add("-");
                        SuffixList.Add("-");
                    }
                    break;
            }

            //ResultsList.AddRange(PrefixList);
            ResultsList.AddRange(FindCompLineItem(PrefixList,query, CompReportTmpl, items, itemFName));
            //ResultsList.AddRange(FindCompLineItem(query, CompReportTmpl, items, itemFName));
            ResultsList.AddRange(SuffixList);
            return ResultsList;
            //return FindCompLineItem(query, CompReportTmpl, items, itemFName);
        }

        public string FindCompImg(string query, DBTmpl CompReportTmpl)
        {
            BsonDocument doc;
            //string tmp = "";
            string result = "";
            if (CompReportTmpl.results == "IMG")
            {
                //doc = base.MDBFindOne(query + ";" + CompReportTmpl.query + ";NotMatches;" + CompReportTmpl.field1 + ";//", "AND", "DES", "TestEnd");
                doc = base.MDBFindOne(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field1 + ";", "AND", "DES", "TestEnd");
                if (doc != null) result = doc.GetValue(CompReportTmpl.field1, "").ToString();
                //TODO??- manipulte image path according to CompReportTmpl.disp1??
                //tmp = CompReportFormat(tmp, CompReportTmpl.disp1);
            }
            return result;
        }

        bool CompReportFormat(string val, string format, out string outVal)
        {
            outVal = val;
            if ((val == "NA") || (val == "Multi") || (String.IsNullOrWhiteSpace(format))) return true;
            try
            {
                CompReportFormatter instance = new CompReportFormatter();
                MethodInfo method = typeof(CompReportFormatter).GetMethod(format);
                if (method == null) return false; //format does not exist
                object[] methodArgs = { val };
                outVal = (string)method.Invoke(instance, methodArgs);
            }
            catch
            {
                return false;
            }
            
           return true;
        }

        //List<BsonDocument> valResults = MDBStationResults.ValidateCompLine(query, lineTmpl);//each result: Field,Item,Problem,Query
        public List<BsonDocument> ValidateCompLine(string query, DBTmpl CompReportTmpl)
        {
            string[] items = { "" };
            string itemFName = "";
            switch (CompReportTmpl.results)
            {
                case "L0-L3":
                    items = new string[] { "L0", "L1", "L2", "L3" };
                    itemFName = "Lane";
                    break;
                case "L0-L1":
                    items = new string[] { "L0", "L1" };
                    itemFName = "Lane";
                    break;
                case "L2-L3":
                    items = new string[] { "L2", "L3" };
                    itemFName = "Lane";
                    break;
                case "PA-PB":
                    items = new string[] { "PA", "PB" };
                    itemFName = "Port";
                    break;
                case "D0-D2":
                    items = new string[] { "D0", "D1","D2" };
                    itemFName = "Lane";
                    //itemFName = "Data Lane A"; // TODO make sure this is correct 
                    break;
                case "CK-D0-D2":
                    items = new string[] { "CK", "D0", "D1", "D2" };
                    itemFName = "Lane";
                    break;
                case "S":
                case "Clock":
                    items = new string[] { "" };
                    itemFName = "";
                    break;
            }
                return ValidateCompLineItem(query, CompReportTmpl, items, itemFName);
        }

        public BsonDocument ValidateCompImg(string query, DBTmpl CompReportTmpl)
        {
            BsonDocument doc= new BsonDocument();
            MongoCursor Cur;
            BsonDocument MissedRes = new BsonDocument {
                {"Template", CompReportTmpl.name},
                { "Field", CompReportTmpl.field1 },
                { "Item", "IMG" },
                { "Problem", "Multi" },
                { "Query", query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field1 + ";" },
                {"Table",CompReportTmpl.table }
            };
           
            if (CompReportTmpl.results == "IMG")
            {
                Cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field1 + ";", "AND", "DES", "TestEnd");
                if (Cur.Count() > 1)
                {
                    MissedRes.Set("Problem", "Multi");
                }
                if (Cur.Count() == 0)
                {
                    MissedRes.Set("Problem", "NA");
                }
                if (Cur.Count() == 1)
                {
                    foreach (BsonDocument d in Cur) doc = d;
                    string path = doc.GetValue(CompReportTmpl.field1, "").ToString();
                    if (String.IsNullOrWhiteSpace(path)) MissedRes.Set("Problem", "NA");
                    if (!File.Exists(path)) MissedRes.Set("Problem", "File does not exist:" + path);
                    else MissedRes = null;
                }
            }
            return MissedRes;
        }

        List<string> FindCompLineItem(List<string> prefixList, string query, DBTmpl CompReportTmpl, string[] items, string itemFName)
        //List<string> FindCompLineItem(string query, DBTmpl CompReportTmpl, string[] items, string itemFName)
        {
            BsonDocument doc;
            MongoCursor cur;
            string Status = "Pass";
            string tmp = "";
            string formattedTmp;
            List<string> results = new List<string>();
            results.Add("Fail");//default status value. will be updated later
            results.AddRange(prefixList);

            int i = 0;
            foreach (string item in items)
            {
                tmp = "NA";
                if (!String.IsNullOrWhiteSpace(itemFName))
                {
                    cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";Matches;" + itemFName + ";/" + item + "/i;ExistNotEmpty;" + CompReportTmpl.field1 + ";", "AND", "DES", "TestEnd");
                    doc = base.MDBFindOne(query + ";" + CompReportTmpl.query + ";Matches;" + itemFName + ";/" + item + "/i;ExistNotEmpty;" + CompReportTmpl.field1 + ";", "AND", "DES", "TestEnd");
                }
                else
                {
                    cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field1 + ";", "AND", "DES", "TestEnd");
                    doc = base.MDBFindOne(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field1 + ";", "AND", "DES", "TestEnd");
                }

                if (doc != null) tmp = doc.GetValue(CompReportTmpl.field1, "NA").ToString();
                //new: Multi
                if (cur.Count() > 1) tmp = "Multi";
                if (CompReportFormat(tmp, CompReportTmpl.disp1, out formattedTmp)) tmp = formattedTmp;
                results.Add(tmp);
                if ((doc != null) && (doc.GetValue("TestStatus", "NA").ToString() == "Fail")) Status = "Fail";
                if ((doc == null) && (Status  != "Fail")) Status = "NA";
                if ((doc != null) && (doc.GetValue("TestStatus", "NA").ToString() == "NA")) Status = "NA";
                //new: Multi
                if (cur.Count() > 1) Status = "Multi";

                if (!String.IsNullOrEmpty(CompReportTmpl.field2))
                {
                    tmp = "NA";
                    if (!String.IsNullOrWhiteSpace(itemFName))
                    {
                        cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";Matches;" + itemFName + ";/" + item + "/i;ExistNotEmpty;" + CompReportTmpl.field2 + ";", "AND", "DES", "TestEnd");
                        doc = base.MDBFindOne(query + ";" + CompReportTmpl.query + ";Matches;" + itemFName + ";/" + item + "/i;ExistNotEmpty;" + CompReportTmpl.field2 + ";", "AND", "DES", "TestEnd");
                    }
                    else
                    {
                        cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field2 + ";", "AND", "DES", "TestEnd");
                        doc = base.MDBFindOne(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field2 + ";", "AND", "DES", "TestEnd");
                    }
                    if (doc != null) tmp = doc.GetValue(CompReportTmpl.field2, "NA").ToString();
                    //new: Multi
                    if (cur.Count() > 1) tmp = "Multi";
                    if (CompReportFormat(tmp, CompReportTmpl.disp2, out formattedTmp)) tmp = formattedTmp;
                    results[i + 1] += Environment.NewLine + tmp;
                    if ((doc != null) && (doc.GetValue("TestStatus", "NA").ToString() == "Fail")) Status = "Fail";
                    if ((doc == null) && (Status != "Fail")) Status = "NA";
                    if ((doc != null) && (doc.GetValue("TestStatus", "NA").ToString() == "NA")) Status = "NA";
                    //new: Multi
                    if (cur.Count() > 1) Status = "Multi";
                }

                if (!String.IsNullOrEmpty(CompReportTmpl.field3))
                {
                    tmp = "NA";
                    if (!String.IsNullOrWhiteSpace(itemFName))
                    {
                        cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";Matches;" + itemFName + ";/" + item + "/i;ExistNotEmpty;" + CompReportTmpl.field3 + ";", "AND", "DES", "TestEnd");
                        doc = base.MDBFindOne(query + ";" + CompReportTmpl.query + ";Matches;" + itemFName + ";/" + item + "/i;ExistNotEmpty;" + CompReportTmpl.field3 + ";", "AND", "DES", "TestEnd");
                    }
                    else
                    {
                        cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field3 + ";", "AND", "DES", "TestEnd");
                        doc = base.MDBFindOne(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field3 + ";", "AND", "DES", "TestEnd");
                    }
                    if (doc != null) tmp = doc.GetValue(CompReportTmpl.field3, "NA").ToString();
                    //new: Multi
                    if (cur.Count() > 1) tmp = "Multi";
                    if (CompReportFormat(tmp, CompReportTmpl.disp3, out formattedTmp)) tmp = formattedTmp;
                    results[i + 1] += Environment.NewLine + tmp;
                    if ((doc != null) && (doc.GetValue("TestStatus", "NA").ToString() == "Fail")) Status = "Fail";
                    if ((doc == null) && (Status != "Fail")) Status = "NA";
                    if ((doc != null) && (doc.GetValue("TestStatus", "NA").ToString() == "NA")) Status = "NA";
                    //new: Multi
                    if (cur.Count() > 1) Status = "Multi";
                }
                i++;
            }

            foreach (string result in results) if ((result == "NA") && (results[0] == "Pass")) Status = "NA";
            results[0] = Status;
            return results;
            
        }





        List<BsonDocument> ValidateCompLineItem(string query, DBTmpl CompReportTmpl,string[] items, string itemFName)
        {
                List<BsonDocument> MissingResults = new List<BsonDocument>();
                BsonDocument doc = new BsonDocument();
                MongoCursor Cur;
                string tmp,ftmp = "";

                foreach (string item in items)
                {
                    tmp = "NA";
                    BsonDocument MissedRes = new BsonDocument {
                        {"Template", CompReportTmpl.name},
                        { "Field", CompReportTmpl.field1 },
                        { "Item", item },
                        { "Problem", "Multi" },
                        {"Table",CompReportTmpl.table }
                    };
                        if (!String.IsNullOrWhiteSpace(itemFName))
                        MissedRes.Set("Query", query + ";" + CompReportTmpl.query + ";EQ;"+ itemFName + ";" + item + ";ExistNotEmpty;" + CompReportTmpl.field1 + ";");
                        else  MissedRes.Set("Query", query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field1 + ";");
                    
                    if (!String.IsNullOrWhiteSpace(itemFName))
                    Cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";EQ;"+ itemFName + ";" + item + ";ExistNotEmpty;" + CompReportTmpl.field1 + ";", "AND", "DES", "TestEnd");
                    else
                    Cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field1 + ";", "AND", "DES", "TestEnd");

                    if (Cur.Count() > 1)
                    {
                        MissedRes.Set("Problem", "Multi");
                        MissingResults.Add(MissedRes);
                    }
                    if (Cur.Count() == 0)
                    {
                        MissedRes.Set("Problem", "NA");
                        MissingResults.Add(MissedRes);
                    }
                    if (Cur.Count() == 1)
                    {
                        foreach (BsonDocument d in Cur) doc = d;

                        if (doc != null) tmp = doc.GetValue(CompReportTmpl.field1, "NA").ToString();
                        if (tmp == "NA")
                        {
                            MissedRes.Set("Problem", "NA");
                            MissingResults.Add(MissedRes);
                        }
                        if (!CompReportFormat(tmp, CompReportTmpl.disp1,out ftmp))
                        {
                            MissedRes.Set("Problem", "Edit issue");
                            MissingResults.Add(MissedRes);
                        }
                        if ((doc != null) && (doc.GetValue("TestStatus", "NA").ToString() == "NA"))
                        {
                            MissedRes.Set("Problem", "NA");
                            MissedRes.Set("Field", "TestStatus");
                            MissingResults.Add(MissedRes);
                        }
                    }
                    if (!String.IsNullOrEmpty(CompReportTmpl.field2))
                    {
                        tmp = "NA";
                        MissedRes = new BsonDocument {
                            {"Template", CompReportTmpl.name},
                            { "Field", CompReportTmpl.field2 },
                            { "Item", item },
                            { "Problem", "Multi" },
                            {"Table",CompReportTmpl.table }
                        };
                        if (!String.IsNullOrWhiteSpace(itemFName))
                            MissedRes.Set("Query", query + ";" + CompReportTmpl.query + ";EQ;" + itemFName + ";" + item + ";ExistNotEmpty;" + CompReportTmpl.field2 + ";");
                        else MissedRes.Set("Query", query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field2 + ";");

                        if (!String.IsNullOrWhiteSpace(itemFName))
                            Cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";EQ;" + itemFName + ";" + item + ";ExistNotEmpty;" + CompReportTmpl.field2 + ";", "AND", "DES", "TestEnd");
                        else
                            Cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field2 + ";", "AND", "DES", "TestEnd");

                        if (Cur.Count() > 1)
                        {
                            MissedRes.Set("Problem", "Multi");
                            MissingResults.Add(MissedRes);
                        }
                        if (Cur.Count() == 0)
                        {
                            MissedRes.Set("Problem", "NA");
                            MissingResults.Add(MissedRes);
                        }
                        if (Cur.Count() == 1)
                        {
                            foreach (BsonDocument d in Cur) doc = d;

                            if (doc != null) tmp = doc.GetValue(CompReportTmpl.field2, "NA").ToString();
                            if (tmp == "NA")
                            {
                                MissedRes.Set("Problem", "NA");
                                MissingResults.Add(MissedRes);
                            }
                            if (!CompReportFormat(tmp, CompReportTmpl.disp2, out ftmp))
                            {
                                MissedRes.Set("Problem", "Edit issue");
                                MissingResults.Add(MissedRes);
                            }
                            if ((doc != null) && (doc.GetValue("TestStatus", "NA").ToString() == "NA"))
                            {
                                MissedRes.Set("Problem", "NA");
                                MissedRes.Set("Field", "TestStatus");
                                MissingResults.Add(MissedRes);
                            }
                        }
                    }

                    if (!String.IsNullOrEmpty(CompReportTmpl.field3))
                    {
                        tmp = "NA";
                        MissedRes = new BsonDocument {
                            {"Template", CompReportTmpl.name},
                            { "Field", CompReportTmpl.field3 },
                            { "Item", item },
                            { "Problem", "Multi" },
                            {"Table",CompReportTmpl.table }
                        };
                        if (!String.IsNullOrWhiteSpace(itemFName))
                            MissedRes.Set("Query", query + ";" + CompReportTmpl.query + ";EQ;" + itemFName + ";" + item + ";ExistNotEmpty;" + CompReportTmpl.field3 + ";");
                        else MissedRes.Set("Query", query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field3 + ";");

                        if (!String.IsNullOrWhiteSpace(itemFName))
                            Cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";EQ;" + itemFName + ";" + item + ";ExistNotEmpty;" + CompReportTmpl.field3 + ";", "AND", "DES", "TestEnd");
                        else
                            Cur = base.MDBFind(query + ";" + CompReportTmpl.query + ";ExistNotEmpty;" + CompReportTmpl.field3 + ";", "AND", "DES", "TestEnd");

                        if (Cur.Count() > 1)
                        {
                            MissedRes.Set("Problem", "Multi");
                            MissingResults.Add(MissedRes);
                        }
                        if (Cur.Count() == 0)
                        {
                            MissedRes.Set("Problem", "NA");
                            MissingResults.Add(MissedRes);
                        }
                        if (Cur.Count() == 1)
                        {
                            foreach (BsonDocument d in Cur) doc = d;

                            if (doc != null) tmp = doc.GetValue(CompReportTmpl.field3, "NA").ToString();
                            if (tmp == "NA")
                            {
                                MissedRes.Set("Problem", "NA");
                                MissingResults.Add(MissedRes);
                            }
                            if (!CompReportFormat(tmp, CompReportTmpl.disp3, out ftmp))
                            {
                                MissedRes.Set("Problem", "Edit issue");
                                MissingResults.Add(MissedRes);
                            }
                            if ((doc != null) && (doc.GetValue("TestStatus", "NA").ToString() == "NA"))
                            {
                                MissedRes.Set("Problem", "NA");
                                MissedRes.Set("Field", "TestStatus");
                                MissingResults.Add(MissedRes);
                            }
                        }
                    }
                }
                return MissingResults;
            }

        //new
        public bool MoveResToCompArch(string BoardNumber)
        {
            MongoCursor Cur = base.MDBFind("EQ;BoardNumber;" + BoardNumber, "AND", "DES", "_id");
            return (base.MDBMoveCurToDB(Cur, System.Configuration.ConfigurationManager.AppSettings["DB_CMP_Arch"]));
        }

        public bool MoveResFromCompArch(string BoardNumber)
        {
            MongoCursor Cur = base.MDBFind("EQ;BoardNumber;" + BoardNumber, "AND", "DES", "_id");
            return (base.MDBMoveCurToDB(Cur, System.Configuration.ConfigurationManager.AppSettings["DB_CMP"]));
        }

        public bool MoveResToPRMArch(string proj)
        {
            MongoCursor Cur = base.MDBFind("EQ;ProjectName;" + proj, "AND", "DES", "_id");
            return (base.MDBMoveCurToDB(Cur, System.Configuration.ConfigurationManager.AppSettings["DB_PRM_Arch"]));
        }

        public bool MoveResFromPRMArch(string proj)
        {
            MongoCursor Cur = base.MDBFind("EQ;ProjectName;" + proj, "AND", "DES", "_id");
            return (base.MDBMoveCurToDB(Cur, System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]));
        }

        public bool Save(BsonDocument result)
        {
            return base.dbUpdate(result);
        }

        public string FindExcelCellQuery(string baseQuery,string cellQuery)
            //cell_query=SpecTestName;EQ;$B$Line;Corner;EQ;Room;field=Chip[mw]
        {
            string val = "";
            int fInd = cellQuery.IndexOf(";field=");
            string _cellQuery = cellQuery.Substring(0, fInd);
            List <string> fieldNames = new List<string>(cellQuery.Substring(fInd + 7).Split(';'));
            if (!baseQuery.EndsWith(";")) baseQuery += ";";
            BsonDocument result = base.MDBFindOne(baseQuery + _cellQuery, "AND", "DES", "_id");
            if (result != null)
            {
                foreach (string fieldName in fieldNames)
                {
                    if (!string.IsNullOrEmpty(val)) val += "_";
                    val += result.GetValue(fieldName, "").ToString();
                }
            }
            return val;
        }
            //This is a special method for FXVL to copy <table>_102M data from original data excluding Cable_Length > 102M
        public Boolean CopyFXVL102MData()
        {
            if (!colname.Contains("_102M")) return false;
            var qList = new List<IMongoQuery>();
            List<BsonString> lin = new List<BsonString>();
            lin.Add(new BsonString("112M"));
            lin.Add(new BsonString("122M"));
            lin.Add(new BsonString("132M"));
            lin.Add(new BsonString("142M"));
            qList.Add(Query.NotIn("Cable_Length", lin));

            var source = database.GetCollection(colname.Replace("_102M",""));
            var _query = Query.And(qList);
            collection.RemoveAll();
            collection.InsertBatch(source.Find(_query));
            return true;
        }
    }
}