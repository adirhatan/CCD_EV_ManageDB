using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;

namespace CCD_EV_ManageDB.Helpers
{
    public class SessionDBHandler
    {
        private static string connString { get { return System.Configuration.ConfigurationManager.AppSettings["ASPStateDBconnString"]; } }

        public SessionDBHandler()
        {

        }
        public void FixSessionsDB()
        {
            //SqlConnection myConnection = new SqlConnection("server=haisql023;" +
            //               "Trusted_Connection=yes;" +
            //               "database=CCDEVASPState; " +
            //               "User ID=CCDEVState_User; "+
            //               "Password=CCDEV; " +
            //               "connection timeout=30");
            //Server=haisql023\haisql023;Database=CCDEVASPState;User ID=CCDEVState_User;Password=CCDEV"
            SqlConnection myConnection = new SqlConnection(connString);
            string cmdtxt = @"USE [CCDEVASPState]
                            GO
                            DECLARE @return_value int
                            EXEC    @return_value = [dbo].[CreateTempTables]
                            SELECT  'Return Value' = @return_value
                            GO
                            ";
            SqlCommand command = new SqlCommand(cmdtxt, myConnection);
            try
            {
                myConnection.Open();
                command.ExecuteNonQuery();
                myConnection.Close();
            }
            catch (SqlException ex)
            {
                
            }
        }

    }
}