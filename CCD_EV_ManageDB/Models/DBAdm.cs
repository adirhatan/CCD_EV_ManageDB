using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCD_EV_ManageDB.Models
{
    public class DBAdm
    {
        public List<userCCDEV> users { get; set; }
        
        public DBAdm()
        {
            users = new List<userCCDEV>();
        }

    }
}