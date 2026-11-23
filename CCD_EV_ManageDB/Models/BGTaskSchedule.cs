using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class BGTaskSchedule
    {
        public string id { get; set; }
        public string desc { get; set; }
        public string user { get; set; }
        public string frequency { get; set; }
        public string day { get; set; }
        public string hour { get; set; }
        public long nextLaunch { get; set; }
        public long lastLaunch { get; set; }
        public BsonDocument jmap { get; set; }

        public BGTaskSchedule()
        {
            jmap = new BsonDocument();
        }

        public BGTaskSchedule(string _id, string _desc)
        {
            id = _id;
            desc = _desc;
            jmap = new BsonDocument();
        }

        public BGTaskSchedule(BsonDocument doc)
        {
            id = doc.GetValue("_id", "").ToString();
            desc = doc.GetValue("desc", "").ToString();
            user = doc.GetValue("user", "").ToString();
            desc = doc.GetValue("desc", "").ToString();
            frequency = doc.GetValue("frequency", "").ToString();
            day = doc.GetValue("day", "").ToString();
            hour = doc.GetValue("hour", "").ToString();
            nextLaunch = doc.GetValue("nextLaunch", "").ToInt64();
            lastLaunch = doc.GetValue("nextLaunch", "").ToInt64();
            jmap = doc.GetValue("jmap", new BsonDocument()).AsBsonDocument;
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument tmp = new BsonDocument();
            tmp.Add("_id", id);
            tmp.Add("desc", desc);
            tmp.Add("user", user);
            tmp.Add("frequency", frequency);
            tmp.Add("day", day);
            tmp.Add("hour", hour);
            tmp.Add("nextLaunch", nextLaunch);
            tmp.Add("lastLaunch", lastLaunch);
            tmp.Add("jmap", jmap);
            return tmp;
        }

        public Boolean IsLaunchTime(DateTime now)
        {
            if (Convert.ToInt64(now.ToString("yyyyMMddHHmmss")) < nextLaunch) return false;
            Calendar myCal = CultureInfo.InvariantCulture.Calendar;
            if (frequency == General.Frequency.weekly.ToString())
            {
                if ((myCal.GetDayOfWeek(DateTime.Now).ToString() == day) && (myCal.GetHour(DateTime.Now) == Convert.ToInt32(hour))) return true;
            }
            if (frequency == General.Frequency.daily.ToString())
            {
                if ((myCal.GetHour(DateTime.Now) == Convert.ToInt32(hour))) return true;
            }
            if (frequency == General.Frequency.minutely.ToString())
            {
                if (Convert.ToInt64(now.ToString("yyyyMMddHHmmss")) > nextLaunch) return true;
            }
            return false;
        }

        public void UpdateLaunchTimeExecuted(DateTime now)
        {
            Calendar myCal = CultureInfo.InvariantCulture.Calendar;
            jmap.Set("prevLaunch", lastLaunch);
            lastLaunch = Convert.ToInt64(now.ToString("yyyyMMddHHmmss"));
            
            if (frequency == General.Frequency.weekly.ToString())
            {
                nextLaunch=Convert.ToInt64(myCal.AddWeeks(now,1).ToString("yyyyMMddHHmmss"));
            }
            if (frequency == General.Frequency.daily.ToString())
            {
                nextLaunch = Convert.ToInt64(myCal.AddDays(now, 1).ToString("yyyyMMddHHmmss"));
            }
            if (frequency == General.Frequency.monthly.ToString())
            {
                nextLaunch = Convert.ToInt64(myCal.AddMonths(now, 1).ToString("yyyyMMddHHmmss"));
            }
            if (frequency == General.Frequency.minutely.ToString())
            {
                nextLaunch = Convert.ToInt64(myCal.AddMinutes(now, Convert.ToInt32(this.hour)).ToString("yyyyMMddHHmmss"));
            }
            

        }

        public void UpdateJmap(string nowStr, string key,string type, string proj, string phase, string mails)
        {
            jmap.Set("nowStr", nowStr);
            jmap.Set("key", key);
            jmap.Set("type", type);
            jmap.Set("proj", proj);
            jmap.Set("phase", phase);
            jmap.Set("mails", mails);
        }

        public void UpdateJmap(string nowStr, string key)
        {
            jmap.Set("nowStr", nowStr);
            jmap.Set("key", key);
        }


    }

}