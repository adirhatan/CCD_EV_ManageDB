using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace CCD_EV_ManageDB.Models
{
    public class JobScheduler
    {

        //private static string DefaultCol { get { return System.Configuration.ConfigurationManager.AppSettings["DefaultCol"]; } }

        //private static string dbAdress { get { return System.Configuration.ConfigurationManager.AppSettings["DBAddress"]; } }
        //private readonly int DataTableLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DataTableLimit"]);

        //private static string DB_PRM { get { return System.Configuration.ConfigurationManager.AppSettings["DB_PRM"]; } }
        //private static string DB_TMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_TMP"]; } }
        //private static string DB_CMP { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP"]; } }
        //private static string DB_CMP_Arch { get { return System.Configuration.ConfigurationManager.AppSettings["DB_CMP_Arch"]; } }
        //public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }

        //private MongoDBClient db = new MongoDBClient(dbAdress, DB_TMP, DefaultCol);

        public void Execute()
        {
            DateTime nowDate;
            MDBBGTaskSchedule MDBBGTaskSchedule = new Repositories.MDBBGTaskSchedule();
            foreach (BGTaskSchedule BGTS in MDBBGTaskSchedule.ReadAllBGTasksSchedule())
            {
                nowDate = DateTime.Now;
                if (BGTS.IsLaunchTime(nowDate))
                {
                    MDBUserBGTask MDBUserBGTask = new MDBUserBGTask();
                    BGTS.UpdateJmap(nowDate.ToString("yyyyMMddHHmmss"), BGTS.user + nowDate.ToString("yyyyMMddHHmmss"));
                    UserBGTask UBT = new UserBGTask(BGTS.user + nowDate.ToString("yyyyMMddHHmmss"),BGTS.desc,"",BGTS.jmap);
                    MDBUserBGTask.SaveUserBGTask(UBT);
                    BGTS.UpdateLaunchTimeExecuted(nowDate);
                    MDBBGTaskSchedule.SaveBGTaskSchedule(BGTS);
                    System.Threading.Thread.Sleep(1000);//The key is in seconds resouloution, so wait to ensure valid keys
                }

            }


        }
    }
}