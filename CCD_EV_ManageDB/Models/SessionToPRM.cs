using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCD_EV_ManageDB.Models
{
    public class SessionToPRM
    {
        public string SessionStart { get; set; }
        public string ProjectName { get; set; }
        public string UnitId { get; set; }

        public override bool Equals(object obj) { 
          var other = obj as SessionToPRM;
          if (other == null) {
            return false;
          }
          return SessionStart == other.SessionStart ;
        }
        public bool Equals(SessionToPRM s)
        {
            // If parameter is null return false:
            if ((object)s == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (SessionStart == s.SessionStart);
        }
        //add this code to class ThreeDPoint as defined previously
        //
        public static bool operator ==(SessionToPRM a, SessionToPRM b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.SessionStart == b.SessionStart;
        }

        public static bool operator !=(SessionToPRM a, SessionToPRM b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return SessionStart + "," + ProjectName + "," + UnitId;
        }

        public override int GetHashCode() 
        {
            return SessionStart.GetHashCode();
        }

        public SessionToPRM GetSesFromStr(string str)
        {
            SessionToPRM ses = new SessionToPRM();
            string[] arr = str.Split(',');
            if (arr.Length < 3) return null;
            ses.SessionStart = arr[0];
            ses.ProjectName = arr[1];
            ses.UnitId = arr[2];
            return ses;
        }
    }
}