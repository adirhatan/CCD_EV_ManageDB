using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class ReportData
    {
        public UserTableReport Report { get; set; }
        public List<string> Templates { get; set; }
        public HashSet<string> Tests { get; set; }
        public HashSet<string> TableFields { get; set; }
        public Dictionary<string, HashSet<string>> TestsFields { get; set; }
        public DBsearchSave DBQuery { get; set; }
        public string script { get; set; }

        public ReportData(UserTableReport R, List<string> _Templates, HashSet<string> _Tests ,Dictionary<string, HashSet<string>> _TestsFields,DBsearchSave _Query)
        {
            Report = R;
            Templates = _Templates;
            Tests = _Tests;
            TableFields = (new MDBTablesFields()).ReadTableFields(Report.RConfig.table, General.TableType.Dev);
            TestsFields = _TestsFields;
            DBQuery = _Query;
        }
    }
}