using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using CCD_EV_ManageDB.Repositories;
using System.Security.Principal;

namespace CCD_EV_ManageDB.Models.Security
{
    public class CCD_EV_RolesProvider : RoleProvider
    {
        private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }

        private MongoDBClient db = new MongoDBClient(dbAdress, DB_PRM, "users");

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            //string[] roles = new string[] { "CCD_EV" };
            string[] roles = db.UserDBRoles(username);
            return (roles);
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            foreach (string r in db.UserDBRoles(username)) if (r == roleName) return true;
            return false;
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}