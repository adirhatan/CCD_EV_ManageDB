using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using System.IO;

namespace CCD_EV_ManageDB.Helpers
{
    public static class CCDUtilities
    {
        public static string getAdjacentSundayDate(string _Sdate)
        {
            Calendar myCal = CultureInfo.InvariantCulture.Calendar;
            string Sdate = _Sdate;
            switch (myCal.GetDayOfWeek(new DateTime(Convert.ToInt32(_Sdate.Substring(0, 4)), Convert.ToInt32(_Sdate.Substring(4, 2)), Convert.ToInt32(_Sdate.Substring(6, 2)))))
            {
                case DayOfWeek.Sunday:
                    Sdate = _Sdate;
                    break;
                case DayOfWeek.Monday:
                    Sdate = (new DateTime(Convert.ToInt32(_Sdate.Substring(0, 4)), Convert.ToInt32(_Sdate.Substring(4, 2)), Convert.ToInt32(_Sdate.Substring(6, 2)))).AddDays(-1D).ToString("yyyyMMdd");
                    break;
                case DayOfWeek.Tuesday:
                    Sdate = (new DateTime(Convert.ToInt32(_Sdate.Substring(0, 4)), Convert.ToInt32(_Sdate.Substring(4, 2)), Convert.ToInt32(_Sdate.Substring(6, 2)))).AddDays(-2D).ToString("yyyyMMdd");
                    break;
                case DayOfWeek.Wednesday:
                    Sdate = (new DateTime(Convert.ToInt32(_Sdate.Substring(0, 4)), Convert.ToInt32(_Sdate.Substring(4, 2)), Convert.ToInt32(_Sdate.Substring(6, 2)))).AddDays(-3D).ToString("yyyyMMdd");
                    break;
                case DayOfWeek.Thursday:
                    Sdate = (new DateTime(Convert.ToInt32(_Sdate.Substring(0, 4)), Convert.ToInt32(_Sdate.Substring(4, 2)), Convert.ToInt32(_Sdate.Substring(6, 2)))).AddDays(-4D).ToString("yyyyMMdd");
                    break;
                case DayOfWeek.Friday:
                    Sdate = (new DateTime(Convert.ToInt32(_Sdate.Substring(0, 4)), Convert.ToInt32(_Sdate.Substring(4, 2)), Convert.ToInt32(_Sdate.Substring(6, 2)))).AddDays(-5D).ToString("yyyyMMdd");
                    break;
                case DayOfWeek.Saturday:
                    Sdate = (new DateTime(Convert.ToInt32(_Sdate.Substring(0, 4)), Convert.ToInt32(_Sdate.Substring(4, 2)), Convert.ToInt32(_Sdate.Substring(6, 2)))).AddDays(-6D).ToString("yyyyMMdd");
                    break;
            }
            return Sdate;
        }

        public static int calcTndWeek(DateTime IndStartDate, string ResultDate)
        {
            int week = 0;
            DateTime ResDate = (new DateTime(Convert.ToInt32(ResultDate.Substring(0, 4)), Convert.ToInt32(ResultDate.Substring(4, 2)), Convert.ToInt32(ResultDate.Substring(6, 2))));
            ResDate = ResDate.AddDays(-1L);// subtruct a day to include Sunday in prevoius week;
            int days = (ResDate.Subtract(IndStartDate)).Days ;
            week = days / 7;
            return week;
        }

        public static string getCurWeek()
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = DateTime.Now.AddDays(-1); // subtruct a day to include Sunday in prevoius week;
            
            Calendar cal = dfi.Calendar;
            return "ww " + cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek).ToString();
        }

        public static DateTime String2Date(string _Sdate)
        {
            return (new DateTime(Convert.ToInt32(_Sdate.Substring(0, 4)), Convert.ToInt32(_Sdate.Substring(4, 2)), Convert.ToInt32(_Sdate.Substring(6, 2))));
        }

        public static bool CopyFileSaveBackup(string sourceFile, string targetDir, string targetFileName, out string message)
        {
            message = "";
            string targetFileNameNoExtension = Path.GetFileNameWithoutExtension(targetFileName);
            string extension = Path.GetExtension(targetFileName);
            string targetFilePath = Path.Combine(targetDir, targetFileName);
            bool curFileWasRenamed = false;
            if (File.Exists(targetFilePath))
            {
                int i = 0;
                string backupName = "";
                while ((!curFileWasRenamed) && (i < 100))
                {
                    i++;
                    if (i < 10) backupName = (Path.Combine(targetDir, targetFileNameNoExtension + "_" + DateTime.Now.ToString("yyyyMMdd") + "_0" + i.ToString() + extension));
                    else backupName = (Path.Combine(targetDir, targetFileNameNoExtension + "_" + DateTime.Now.ToString("yyyyMMdd") + "_" + i.ToString() +  extension));
                    if (!File.Exists(backupName))
                    {
                        File.Copy(targetFilePath, backupName);
                        if (File.Exists(backupName)) curFileWasRenamed = true;
                    }
                }
                if (!curFileWasRenamed)
                {
                    message = "Error: colud not create backup for " + targetFilePath;
                    return false;
                }
            }

            try
            {
                File.Copy(sourceFile, targetFilePath,true);
            }
            catch (Exception e)
            {
                message = e.Message;
                return false;
            }
            return true;
        }
    }
}