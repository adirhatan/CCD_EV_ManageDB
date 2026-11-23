using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCD_EV_ManageDB.Models
{
    public class IndicatorDisplayResult
    {
        public Indicator Ind { get; set; }
        public IndicatorResults Res { get; set; }

        public IndicatorDisplayResult()
        {
            Ind = new Indicator();
            Res = new IndicatorResults();
        }
    }
}