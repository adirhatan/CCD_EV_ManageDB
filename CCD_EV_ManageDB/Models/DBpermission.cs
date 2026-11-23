using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCD_EV_ManageDB.Models
{
    public class DBpermission
    {
        private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }

        public string _interface { get; set; }
        public string project { get; set; }
        public string phase { get; set; }

        public DBpermission(string _int, string _project , string _phase)
        {
            _interface = _int;
            project = _project;
            phase = _phase;
        }

        public DBpermission(string permissionStr)
        {
            List<string> strList = new List<string>(permissionStr.Split('_'));
            if (strList.Count < 3) return;
            _interface = strList[0];
            project = strList[1];
            phase = strList[2];
        }

        public bool checkPermission(string col, string _project, string _phase)
        {
            if (col == DefaultCol) return true;
            if (!col.Contains(_interface))
            {
                if (_interface != "CIO") return false;
                else if (((_interface == "CIO") && ((!col.Contains("TBT3")) && (!col.Contains("TBT4")))) &&
                           ((_interface == "CIO") && (col!="TBT"))) return false;
            }
            if (((project != _project) && (project != "ALL")) || ((phase != _phase) && (phase != "ALL"))) return false;
            return true;
        }

        public bool checkColPermissionOnly(string col)
        {
            if (col == DefaultCol) return true;
            if (!col.Contains(_interface))
            {
                if (_interface != "CIO") return false;
                else if ((_interface == "CIO") && ((!col.Contains("TBT3")) && (!col.Contains("TBT4")))) return false;
            }
            return true;
        }


    }
}