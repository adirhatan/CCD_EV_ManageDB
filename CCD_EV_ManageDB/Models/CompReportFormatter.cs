using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;

namespace CCD_EV_ManageDB.Models
{
    public class CompReportFormatter
    {
        public string decPoint4(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.0000}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }

        public string decPoint3(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.000}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
        public string decPoint2(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.00}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
        public string Ber(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.00}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
        public string Pass0(string val)
        {
            string fval = val;
            if (val.ToUpper() == "PASS") fval = "0";
            return fval;
        }
        public string MarginTek(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.00}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
        public string Sec2pSdecP3(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                Double tmp = Convert.ToDouble(val);
                tmp = tmp * 1000000000000;
                val = tmp.ToString();
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.000}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
        public string Sec2nSdecP3(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                Double tmp = Convert.ToDouble(val);
                tmp = tmp * 1000000000;
                val = tmp.ToString();
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.000}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
        public string Sec2micSdecP3(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                Double tmp = Convert.ToDouble(val);
                tmp = tmp * 1000000;
                val = tmp.ToString();
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.000}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
        public string U2mUdecP3(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                Double tmp = Convert.ToDouble(val);
                tmp = tmp * 1000;
                val = tmp.ToString();
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.000}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
        public string U2mUdecP0(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                Double tmp = Convert.ToDouble(val);
                tmp = tmp * 1000;
                val = tmp.ToString();
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
        public string U2KUdecP3(string val)
        {
            string fval = "";
            if (val == "NA") return val;
            try
            {
                Double tmp = Convert.ToDouble(val);
                tmp = tmp / 1000;
                val = tmp.ToString();
                fval = String.Format(CultureInfo.InvariantCulture, "{0:0.000}", Convert.ToDouble(val));
            }
            catch
            {
                fval = val;
            }

            return fval;
        }
    }
}