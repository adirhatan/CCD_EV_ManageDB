using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCD_EV_ManageDB.Models
{
    public class FileLoad
    {
        public HttpPostedFileBase file { get; set; }
        public string path{ get; set; }
        public string col { get; set; }
        public string db { get; set; }
    }
}