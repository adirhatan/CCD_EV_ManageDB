using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCD_EV_ManageDB.Models
{
    public static class General
    {
        public enum TableType {None,Dev,Comp};
        public enum DBType { PRM, TMP, COMP };
        public enum BGTaskType { Export, DevReport, CompReport };
        public enum DeviceType { Host, Device };
        public enum ReportSlideType { limits, image };
        public enum Frequency { monthly, weekly, daily, minutely };
        public enum SessionValidationStatus { Pending_review,TMP_valid, TMP_not_valid, PRM_valid, PRM_not_valid };
        public enum FilterSessions { All,Valid_Pending };
    }
}