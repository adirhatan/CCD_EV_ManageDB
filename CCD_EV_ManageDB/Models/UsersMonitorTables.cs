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
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class UsersMonitorTables
    {
        public string _id { get; set; }
        public string Name { get; set; }
        public List<MTable> MTables { get; set; }

        public UsersMonitorTables()
        {
            _id = "";
            Name = "";
            MTables = new List<MTable>();
        }

        public UsersMonitorTables(string username, string name)
        {
            _id = username + DateTime.Now.ToString("yyyyMMddHHmmss");
            Name = name;
            MTables = new List<MTable>();
        }

        public void addTable(string _TN, string _HN,string _DB, int order)
        {
            List<MTable> NewMTables = new List<MTable>();
            order = order - 1;
            int cnt = 0;
            foreach (MTable Table in MTables)
            {
                cnt++;
                if (order == cnt) NewMTables.Add(new MTable(_TN,_HN,_DB));
                NewMTables.Add(Table);
            }
            if (order > cnt) NewMTables.Add(new MTable(_TN, _HN,_DB));
            MTables = NewMTables;
        }
    }

    public class MTable
    {
        public string TName { get; set; }
        public string HostName { get; set; }
        public string DBName { get; set; }

        public MTable(string TN, string HN, string DB)
        {
            TName = TN;
            HostName = HN;
            DBName = DB;
        }
    }
}