using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using CCD_EV_ManageDB.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CCD_EV_ManageDB.Models
{
    public class VMonitorTables
    {
        public List<MonitorTable> MTList { get; set; }
        //public HashSet<string> Fields { get; set; }
        public string Name { get; set; }
        public string refresh { get; set; }
        public string UMTId { get; set; }

        public VMonitorTables()
        {
            MTList = new List<MonitorTable>();
            //Fields = new HashSet<string>();
            //Fields.Add("SessionStart");
            //Fields.Add("BoardNumber");
            //Fields.Add("UnitId");
            ////Fields.Add("UnitSerialNumber");
            refresh = "Y";
        }

        public void loadUserMonitorTables(string UMTId,out string errMsg)
        {
            errMsg = "";
            MDBUsersMonitorTables MDB_UMT = new MDBUsersMonitorTables();
            UsersMonitorTables UMT = MDB_UMT.ReadUserMonitorTables(UMTId);
            //MDBExecutions MDB_Executions = new MDBExecutions();
            this.Name = UMT.Name;
            this.UMTId = UMTId;
            foreach (MTable mt in UMT.MTables)
            {
                MDBExecutions MDB_Executions = new MDBExecutions(mt.DBName);
                MonitorTable MT = new MonitorTable(mt.TName, mt.HostName);
                MT.ExecutionRecords = MDB_Executions.ReadCursorLastSession(mt.TName, mt.HostName);
                if (MT.ExecutionRecords.Count() == 0)
                {
                    errMsg = "MonitorTable: " + mt.TName + " Host:" + mt.HostName + " does not exist, please check (Host name is case sensitive)";
                    //return RedirectToAction("MyMonitorTables");
                }
                MT.initSesisonVals();
                BsonDocument curExecutionitem = MDB_Executions.ReadLastSysExecutionForTable(mt.TName, mt.HostName);
                if (curExecutionitem != null)
                {
                    MT.ExecutionStartSession = curExecutionitem.GetElement("SessionStart").Value.ToString();
                    MT.ExecutionCurStatus = curExecutionitem.GetElement("Status").Value.ToString();
                    MT.ExecutionComment = curExecutionitem.GetElement("Comment").Value.ToString();
                    MT.ExecutionTimeStamp = curExecutionitem.GetElement("TimeStamp").Value.ToString();
                    MT.ExecutionBoardNumber = curExecutionitem.GetElement("BoardNumber").Value.ToString();
                    MT.ExecutionUnitId = curExecutionitem.GetElement("UnitId").Value.ToString();
                    MT.ExecutionSkew = curExecutionitem.GetElement("Skew").Value.ToString();
                    MT.ExecutionProject = curExecutionitem.GetElement("ProjectName").Value.ToString();
                    MT.ExecutionPhase = curExecutionitem.GetElement("Phase").Value.ToString();
                    if (string.IsNullOrWhiteSpace(mt.TName))
                    {
                        mt.TName = Regex.Replace(curExecutionitem.GetElement("_id").Value.ToString(), "_" + mt.HostName + ".*", "");
                        MT.TName = mt.TName;
                    }
                }
                BsonDocument curExecutionLogitem = MDB_Executions.ReadLastUserExecutionForTable(mt.TName, mt.HostName);
                if (curExecutionLogitem != null)
                {
                    MT.ExecutionLogCurStatus = curExecutionLogitem.GetElement("Status").Value.ToString();
                    MT.ExecutionLogComment = curExecutionLogitem.GetElement("Comment").Value.ToString();
                    MT.ExecutionLogTimeStamp = curExecutionLogitem.GetElement("TimeStamp").Value.ToString();
                }
                //MDBStationResults MDB_Staion = new MDBStationResults("DB_TMP", mt.TName);
                 MDBStationResults MDB_Staion = new MDBStationResults(mt.DBName, mt.TName);
                //MT.TableCurResult = MDB_Staion.ReadLastResultForTable(mt.HostName);
                MT.TableCurResult = MDB_Staion.ReadLastResultForTableSession(mt.HostName,MT.ExecutionStartSession);
                if (MT.TableCurResult != null)
                {
                    long rtimeStamp = Int64.Parse(MT.TableCurResult.GetValue("TestEnd", "0").ToString().Replace("_", ""));
                    long EtimeStamp = Int64.Parse(MT.ExecutionTimeStamp.Replace("_", ""));
                    if ((rtimeStamp > EtimeStamp) && (MT.ExecutionCurStatus.ToUpper() == "ERROR")) { MT.ExecutionCurStatus = "Running-(last error:)"; }
                }
                MongoCursor mCur = MDB_Staion.MDBFind("EQ;SessionStart;" + MT.ExecutionStartSession + ";Matches;TestStatus;/Fail/i", "AND", "DES", "TestEnd");
                MT.ExecutionFails = mCur.Count();
                mCur = MDB_Staion.MDBFind("EQ;SessionStart;" + MT.ExecutionStartSession , "AND", "DES", "TestEnd");
                MT.ExecutionResultsNum = mCur.Count();
                MT.prepareTable();

                this.MTList.Add(MT);
            }
        }

    }
}