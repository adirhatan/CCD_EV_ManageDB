using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;

namespace CCD_EV_ManageDB.Models
{

    public class Board
    {
        public string id { get; set; }
        public string Name { get; set; }
        public string Project { get; set; }
        public string Phase { get; set; }
        public string LastCommentText { get; set; }
        public string LastCommentUser { get; set; }
        public string LastCommentDate { get; set; }
        public string UnitId { get; set; }
        public List<Comment> SavedComments { get; set; }
        public BsonDocument LastExecution { get; set; }
        public string Valid { get; set; }
        public string InUse { get; set; }
        public string Skew { get; set; }
        public double Fuse { get; set; }
        public double Sigma { get; set; }
        public string Interface { get; set; }



        public Board(string _Name, string _Project, string _Phase, string _UnitId, string _LastCommentText, string _LastCommentUser, string _Skew, double _Fuse, double _Sigma, string _Interface)
        {
            id = _Name + "_" + _Project + "_" + _Phase + "_" + _UnitId;
            Name = _Name;
            UnitId = _UnitId;
            Project = _Project;
            Phase = _Phase;
            LastCommentText = _LastCommentText;
            LastCommentUser = _LastCommentUser;
            LastCommentDate = DateTime.Now.ToString("yyyyMMdd");
            SavedComments = new List<Comment>();
            Valid = "Y";
            InUse = "N";
            Skew = _Skew;
            Fuse = _Fuse;
            Sigma = _Sigma;
            Interface = _Interface;
            
        }
        public Board()
        {
            SavedComments = new List<Comment>();
            Valid = "Y";
            InUse = "N";
        }
        public Board(BsonDocument doc)
        {
            id = (doc.GetValue("_id", "").ToString());
            Name = (doc.GetValue("Name", "").ToString());
            Project = (doc.GetValue("Project", "").ToString());
            Phase = (doc.GetValue("Phase", "").ToString());
            UnitId = (doc.GetValue("UnitId", "").ToString());
            LastCommentText = doc.GetValue("LastCommentText", "").ToString();
            LastCommentUser = doc.GetValue("LastCommentUser", "").ToString();
            LastCommentDate = doc.GetValue("LastCommentDate", "").ToString();
            SavedComments = new List<Comment>();
            BsonArray sArray = doc.GetElement("SavedComments").Value.AsBsonArray;
            foreach (BsonDocument item in sArray)
            {
                Comment C = new Comment(item);
                SavedComments.Add(C);
            }
            LastExecution = doc.GetValue("LastExecution", new BsonDocument()).ToBsonDocument();
            Valid = (doc.GetValue("Valid", "Y").ToString());
            InUse = (doc.GetValue("InUse", "N").ToString());
            Skew = (doc.GetValue("Skew", "").ToString());
            Fuse = (doc.GetValue("Fuse", 0).ToDouble());
            Sigma = (doc.GetValue("Sigma", 0).ToDouble());
            Interface = (doc.GetValue("Interface", "").ToString());
       }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", id } };
            Doc.Add("Name", Name);
            Doc.Add("UnitId", UnitId);
            Doc.Add("Project", Project);
            Doc.Add("Phase", Phase);
            Doc.Add("LastCommentText", LastCommentText.ToString());
            Doc.Add("LastCommentUser", LastCommentUser.ToString());
            Doc.Add("LastCommentDate", LastCommentDate.ToString());
            BsonArray sArray = new BsonArray();
            foreach (var C in SavedComments)
            {
                sArray.Add(C.ToBsonDocument());
            }
            Doc.Add("SavedComments", sArray);
            Doc.Add("LastExecution", LastExecution);
            Doc.Add("Valid", Valid);
            Doc.Add("InUse", InUse);
            Doc.Add("Skew", Skew);
            Doc.Add("Sigma", Sigma);
            Doc.Add("Fuse", Fuse);
            Doc.Add("Interface", Interface);
            return Doc;
        }
        public HashSet<string> GetBoardFields()
        {
            HashSet<string> Fields = new HashSet<string>(new string[] { "Name","_id", "Project", "Phase", "UnitId", "LastCommentText", "LastCommentUser", "LastCommentDate","InUse","Skew","Fuse","Sigma","Interface" });
            return Fields;
        }

    }

    public class Comment
    {
        public string text { get; set; }
        public string user { get; set; }
        public string commentDate { get; set; }

        public Comment(string _text, string _user, string _Date)
        {
            text = _text;
            user = _user;
            commentDate = _Date;
        }

        public Comment(BsonDocument doc)
        {
            text = doc.GetValue("text", "").ToString();
            user = doc.GetValue("user", "").ToString();
            commentDate = doc.GetValue("commentDate", "").ToString();
        }
    }

}