using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace CCD_EV_ManageDB.Models
{
    public class DBsearch
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
        public SessionToPRM uSession { get; set; }
        public int Skip { get; set; }
        public bool jumpBottom { get; set; }
        //
        public string SearchOP { get; set; } 
        //
        public List<string> QueryTriplets { get; set; }
        public HashSet<string> Fields { get; set; }
        public HashSet<string> TableFields { get; set; }
        public HashSet<SessionToPRM> Sessions { get; set; }
        public MongoCursor ResCursor { get; set; }
        public SortByBuilder Sort { get; set; }
    
    public DBsearch()
        {
            QueryTriplets = new List<string>();
            Fields = new HashSet<string>();
            TableFields = new HashSet<string>();
            Sessions = new HashSet<SessionToPRM>();
            Sort = new SortByBuilder();
            FieldsStr = "";
            FieldsInc = true;
            jumpBottom = true;
        }
    public void ClearObjs()
    {
        QueryTriplets = new List<string>();
        Fields = new HashSet<string>();
        TableFields = new HashSet<string>();
        Sessions = new HashSet<SessionToPRM>();
        Sort = new SortByBuilder();
        FieldsStr = "";
        FieldsInc = true;
        jumpBottom = true;
    }

    //Add SessionStart,ProjectName,UnitId TODO:take hardcoded configuration
    public void AddCoreFields()
    {
        if ((FieldsStr == null) || (FieldsStr == "")) return;
        string vfields = null;
        HashSet<string> fields = new HashSet<string>(FieldsStr.Split(';'));
        if (FieldsInc)
        {
            fields.Add("_id");
            fields.Add("SessionStart");
            fields.Add("ProjectName");
            fields.Add("UnitId");
        }
        if (FieldsExc)
        {
            fields.Remove("SessionStart");
            fields.Remove("ProjectName");
            fields.Remove("UnitId");
            fields.Remove("_id");
        }
        foreach (string item in fields)
        {
            if (vfields == null) vfields = item;
            else vfields = vfields + ";" + item;
        }
        FieldsStr = vfields;
      }

    public SessionToPRM GetSesFromDBRec(BsonDocument item)
    {
        SessionToPRM ses = new SessionToPRM();
        try
        {
            ses.SessionStart = item.GetElement("SessionStart").Value.ToString();
            ses.ProjectName = item.GetElement("ProjectName").Value.ToString();
            ses.UnitId = item.GetElement("UnitId").Value.ToString();
        }
        catch
        {
            //continue;
        }

        return ses;
    }

    public SessionToPRM GetSesFromSpecOnflictsRec(BsonDocument item)
    {
        SessionToPRM ses = new SessionToPRM();
        try
        {
            ses.SessionStart = item.GetElement("SessionStart").Value.ToString();
            ses.ProjectName = item.GetElement("project").Value.ToString();
            ses.UnitId = item.GetElement("UnitId").Value.ToString();
        }
        catch
        {
            //continue;
        }

        return ses;
    }
    }
}