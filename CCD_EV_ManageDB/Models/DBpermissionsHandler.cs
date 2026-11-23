using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class DBpermissionsHandler
    {
        string[] ProjectsList { get { return System.Configuration.ConfigurationManager.AppSettings["ProjectsList"].Split(','); } }
        string[] PhasesList { get { return System.Configuration.ConfigurationManager.AppSettings["PhasesList"].Split(','); } }

        public bool CheckSearchPermission(string query, string col, string userName, out string message)
        {
            message = "";
            //EQ;ProjectName;GR;EQ;Phase;A0
            string proj = "";
            string phase = "";
            List<DBpermission> permissions = (new MDBUsers()).getPermissions(userName);
            if (Regex.IsMatch(query, @"EQ;ProjectName;\w*;\w*"))
            {
                Match m = Regex.Match(query, @"EQ;ProjectName;(\w*);.*");
                proj = m.Groups[1].Value;
            }
            if (Regex.IsMatch(query, @"EQ;Phase;\w\w"))
            {
                Match m = Regex.Match(query, @"EQ;Phase;(\w\w)");
                phase = m.Groups[1].Value;
            }
            foreach (DBpermission permission in permissions)
            {
                if (permission.checkPermission(col,proj,phase)) return true;
            }
            message = "Permission error: can not access table \"" + col + "\" project \"" + proj + "\" phase \"" + phase + "\"";
            return false;
        }


        public string getPermissionQueryForTable(string col, string userName)
        {
            string permissionQuery = "";
            List<DBpermission> permissions = (new MDBUsers()).getPermissions(userName);
            foreach (DBpermission permission in permissions)
            {
                if (permission.checkColPermissionOnly(col))
                {
                    permissionQuery = "EQ;ProjectName;";
                    if (permission.project != "ALL") permissionQuery += permission.project;
                    else  permissionQuery += ProjectsList[0];
                    permissionQuery += ";EQ;Phase;";
                    if (permission.phase != "ALL") permissionQuery += permission.phase;
                    else permissionQuery += PhasesList[0];
                    return permissionQuery;
                }
            }
            return "";
        }

        public string getProjectPhaseQueryFromSessionObj(DBsearch obj)
        {
            if (obj == null) return "";
            string proj = "";
            string phase = "";
            if (Regex.IsMatch(obj.Query, @"EQ;ProjectName;\w*;\w*"))
            {
                Match m = Regex.Match(obj.Query, @"EQ;ProjectName;(\w*);.*");
                proj = m.Groups[1].Value;
            }
            if (Regex.IsMatch(obj.Query, @"EQ;Phase;\w\w"))
            {
                Match m = Regex.Match(obj.Query, @"EQ;Phase;(\w\w)");
                phase = m.Groups[1].Value;
            }
            if ((!string.IsNullOrEmpty(proj)) && (!string.IsNullOrEmpty(phase))) return "EQ;ProjectName;"+proj+";EQ;Phase;" + phase;
            return "";
        }

        public bool CheckSMpermission(string protocol, string proj, string phase, string userName)
        {
            List<DBpermission> permissions = (new MDBUsers()).getPermissions(userName);

            foreach (DBpermission permission in permissions)
            {
                if (permission.checkPermission(protocol, proj, phase)) return true;
            }
            return false;
        }
    }
}