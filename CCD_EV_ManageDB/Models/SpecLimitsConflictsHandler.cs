using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using CCD_EV_ManageDB.Repositories;
using CCD_EV_ManageDB.Helpers;

namespace CCD_EV_ManageDB.Models
{
    public class SpecLimitsConflictsHandler
    {
        private Dictionary<string, SpecConflictsExceptions> SpecConflictsExceptionsDict;

        public SpecLimitsConflictsHandler(string filePath)
        {
            SpecConflictsExceptionsDict = new Dictionary<string, SpecConflictsExceptions>();
            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement dbElem = doc.Root;
                
                foreach (XElement Spec in dbElem.Elements("Spec"))
                {
                    SpecConflictsExceptionsDict.Add(Spec.Attribute("name").Value, (new SpecConflictsExceptions()));
                    foreach (XElement SpecTestNameSkip in Spec.Elements("SpecTestNameSkip"))
                    {
                        SpecConflictsExceptionsDict[Spec.Attribute("name").Value].SpecTestNamesSkip.Add(SpecTestNameSkip.Value);
                    }
                }
            }
            catch (Exception e)
            {
                //
            }
        }

        public bool SkipSpecTestNameCheck(string Spec, string SpecTestName)
        {
            if (!SpecConflictsExceptionsDict.ContainsKey(Spec)) return false;
            return SpecConflictsExceptionsDict[Spec].SkipSpecTestNameCheck(SpecTestName);
        }

        ////Depricated
        //public bool isConflictSpecialSpecLimitCheck(string Spec, string SpecTestName, string value, string usl, string lsl, string TestStatus)
        //{
        //    if (!SpecConflictsExceptionsDict.ContainsKey(Spec)) return false;
        //    return  (SpecConflictsExceptionsDict[Spec].SpecialCheck(Spec, SpecTestName, value, usl, lsl, TestStatus) ) ;
        //}


        ///////
        public string CheckSpecTestNameLimitViolation(string table, string project, string phase, string prevLaunch ,out string message)
        {
            string success = "Success";
            message = "";
            string messageNone = "No new coflicts were found";
            string messageFound = "Found following number of conficts in ";
            int numConfilcts = 0;
            MDBIndicator MDBIndicatorObj = new MDBIndicator();
            Indicator Ind = MDBIndicatorObj.ReadIndicator(table + "_" + project + "_" + phase);
            if ((Ind == null) || (string.IsNullOrEmpty(Ind.spec)))
            {
                message = "Error: No Indicator is defined for " + table + "_" + project + "_" + phase + " cannot execute scan";
                success = "Error";
                return success; 
            }

            try
            {
                MDBStationResults MDBCol = new MDBStationResults("DB_TMP", table);
                MongoCursor Cur = null;
                string query = "EQ;ProjectName;" + project + ";EQ;Phase;" + phase;
                if (!string.IsNullOrEmpty(prevLaunch))
                {
                    query += ";GT;TestEnd;" + prevLaunch;
                }
                Cur = MDBCol.MDBFind(query, "AND", "DES", "_id");
                numConfilcts = ScanCheckSpecTestNameLimitViolation4Cursor(Ind.spec, Cur, table, project, phase);
                message = messageNone;
                if (numConfilcts > 0) message = messageFound +"TMP:" + numConfilcts.ToString();

                MDBCol = new MDBStationResults("DB_PRM", table);
                Cur = null;
                Cur = MDBCol.MDBFind(query, "AND", "DES", "_id");
                numConfilcts = ScanCheckSpecTestNameLimitViolation4Cursor(Ind.spec, Cur, table, project, phase);
                if (numConfilcts > 0)
                {
                    if (message == messageNone) message = messageFound + "PRM:" + numConfilcts.ToString();
                    else message += " ; " + messageFound + "PRM:" + numConfilcts.ToString();
                }
            }
            catch (Exception e)
            {
                success = "Error";
                message = "ERROR: an exception occured during search for conflicts:" + e.Message;
                return success;
            }
            success = "Done";
            return success;
        }

        private int ScanCheckSpecTestNameLimitViolation4Cursor(string _Spec, MongoCursor Cur, string table, string project, string phase)
        {
            MDBADMReferenceTable MDBADMReferenceTableOBJ = new MDBADMReferenceTable();
            ADMReferenceTable SpecRef = MDBADMReferenceTableOBJ.ReadADMReferenceTable(_Spec);
            MDBSpecLimitsConflicts MDBSpecLimitsConflictsObj = new MDBSpecLimitsConflicts();
            string _SpecTestName = "";
            int numConfilcts = 0;
            foreach (BsonDocument result in Cur)
            {
                _SpecTestName = result.GetValue("SpecTestName", "").ToString();
                if (!SkipSpecTestNameCheck(_Spec, _SpecTestName))
                {
                    FieldSlide fs = SpecRef.GetSpecTestNameLimits(_SpecTestName);
                    string value = result.GetValue(fs.column, "").ToString();
                    string status = fs.GetPassFailbyLimits(value);
                    bool limits = false;
                    if (!string.IsNullOrEmpty(fs.usl) || !string.IsNullOrEmpty(fs.lsl)) limits = true;
                    if ((limits) && (status.ToUpper() != result.GetValue("TestStatus", "").ToString().ToUpper()))
                        //&&  SLCHandler.isConflictSpecialSpecLimitCheck(Ind.spec,_SpecTestName,value,fs.usl,fs.lsl,status))  //Depricated
                    {
                        BsonDocument doc = MDBSpecLimitsConflictsObj.Read(result.GetValue("_id", "").ToString());
                        if (doc == null)
                        {
                            //new conflict
                            BsonDocument Doc = new BsonDocument { { "_id", result.GetValue("_id", "").ToString() } };
                            Doc.Add("usl", fs.usl);
                            Doc.Add("lsl", fs.lsl);
                            Doc.Add("field", fs.column);
                            Doc.Add("value", value);
                            Doc.Add("TestStatus", result.GetValue("TestStatus", "").ToString());
                            Doc.Add("SpecLimitsStatus", status);
                            Doc.Add("SpecTestName", _SpecTestName);
                            Doc.Add("table", table);
                            Doc.Add("project", project);
                            Doc.Add("phase", phase);
                            Doc.Add("SessionStart", result.GetValue("SessionStart", "").ToString());
                            Doc.Add("type", "Conflict");
                            MDBSpecLimitsConflictsObj.Save(Doc);
                            if (limits) numConfilcts++;
                        }
                    }
                }

            }
            return numConfilcts;
        }
        ///////
    }

    public class SpecConflictsExceptions
    {
        public List<string> SpecTestNamesSkip;
        //public Dictionary<string, SpecialSpecTestNameCheck> SpecTestNamesSpecial; //Depricated
        public SpecConflictsExceptions()
        {
            SpecTestNamesSkip = new List<string>();
            //Depricated
            //SpecTestNamesSpecial = new Dictionary<string, SpecialSpecTestNameCheck>();
        }

        public bool SkipSpecTestNameCheck(string SpecTestName)
        {
            if (SpecTestNamesSkip.Count == 0) return false;
            if (SpecTestNamesSkip.Contains("ALL")) return true;
            if (SpecTestNamesSkip.Contains(SpecTestName)) return true;
            return false;
        }

        //Depricated
        //public bool SpecialCheck(string Spec, string SpecTestName, string value, string usl, string lsl, string TestStatus)
        //{
        //    if (SpecTestNamesSpecial.Count == 0) return true;
        //    if (!SpecTestNamesSpecial.ContainsKey(SpecTestName)) return true;
        //    //TODO
        //    switch (SpecTestNamesSpecial[SpecTestName].Condition)
        //    {
        //        case "val_eq_usl":
        //            if ((value == usl) && (TestStatus == SpecTestNamesSpecial[SpecTestName].TestStatus)) return false;
        //            else return true;
        //        break;
        //    }
        //    return true;
        //}

        //public class SpecialSpecTestNameCheck
        //{
        //    public string Condition;
        //    public string TestStatus;
        //}

    }   
}