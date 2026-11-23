using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCD_EV_ManageDB.Models
{
    public class userCCDEV
    {
        public string id { get; set; }
        public List<string> roles { get; set; }
        public userCCDEV()
        {
            roles = new List<string>();
        }
    }

}