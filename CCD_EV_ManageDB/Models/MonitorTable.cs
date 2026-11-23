using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CCD_EV_ManageDB.Models
{
    public class MonitorTable
    {
        public MongoCursor ExecutionRecords;
        public string ExecutionCurStatus;
        public string ExecutionComment;
        public string ExecutionTimeStamp;
        public string ExecutionStartSession;
        public string ExecutionBoardNumber;
        public string ExecutionUnitId;
        public string ExecutionProject;
        public string ExecutionPhase;
        public string ExecutionSkew;
        public long ExecutionFails;
        public long ExecutionResultsNum;
        public BsonDocument TableCurResult;
        public List<MItem> Corners { get; set; }
        public List<MItem> Tests { get; set; }
        public List<MItem> Ports { get; set; }
        public List<MItem> Lanes { get; set; }
        public string TName { get; set; }
        public string HostName { get; set; }
        public List<string> ExecutionFields { get; set; }
        public string ExecutionLogCurStatus;
        public string ExecutionLogComment;
        public string ExecutionLogTimeStamp;


        public MonitorTable(string _TName, string _HostName)
        {
            TName = _TName;
            HostName = _HostName;
            Corners = new List<MItem>();
            Tests = new List<MItem>();
            Ports = new List<MItem>();
            Lanes = new List<MItem>();
            ExecutionFields = new List<string>();
            ExecutionFails = 0;
            ExecutionResultsNum = 0;
        }

        //public MonitorTable(string _TName, string _HostName, MongoCursor _ExecutionRecords, string _ExecutionCurStatus, BsonDocument _TableCurResult)
        //{
        //    TName = _TName;
        //    HostName = _HostName;
        //    ExecutionRecords = _ExecutionRecords;
        //    ExecutionCurStatus = _ExecutionCurStatus;
        //    TableCurResult = _TableCurResult;
        //    Corners = new List<MItem>();
        //    Tests = new List<MItem>();
        //    Lanes = new List<MItem>();
        //    initSesisonVals();
        //    prepareTable();
        //}

        public void initSesisonVals()
        {
            BsonDocument curExecutionitem = null;
            MongoCursor ExecCur = ExecutionRecords.Clone((new BsonDocument()).GetType());
            foreach (BsonDocument item in ExecCur)
            {
                if (item.GetElement("Status").Value.ToString() == "Start")
                {
                    curExecutionitem = item;
                    foreach (BsonElement field in item) ExecutionFields.Add(field.Name);
                    break;
                }
            }

            if (curExecutionitem == null) return;

            foreach (string corner in curExecutionitem.GetElement("PlannedCorners").Value.ToString().Split(';'))
            {
                if (!String.IsNullOrEmpty(corner)) Corners.Add(new MItem(corner, "grey"));
            }
            foreach (string test in curExecutionitem.GetElement("PlannedTests").Value.ToString().Split(';'))
            {
                if (!String.IsNullOrEmpty(test)) Tests.Add(new MItem(test, "grey"));
            }
            foreach (string port in curExecutionitem.GetValue("PlannedPorts","").ToString().Split(';'))
            {
                if (!String.IsNullOrEmpty(port)) Ports.Add(new MItem(port, "grey"));
            }
            //
            if (Ports.Count == 0) Ports.Add(new MItem("<Port>", "grey"));
            //
            foreach (string lane in curExecutionitem.GetElement("PlannedLanes").Value.ToString().Split(';'))
            {
                if (!String.IsNullOrEmpty(lane)) Lanes.Add(new MItem(lane, "grey"));
            }
        }
        public void prepareTable()
        {
            if (TableCurResult == null) return;
            if ((Lanes.Count == 0) || (Ports.Count==0) || (Tests.Count == 0) || (Corners.Count == 0)) return;

            string lastLane = TableCurResult.GetValue("Lane", "").ToString();
            string lastPort = TableCurResult.GetValue("Port", "").ToString();
            string lastTest = TableCurResult.GetValue("TestName", "").ToString();
            string lastCorner = TableCurResult.GetValue("Corner", "").ToString();

            //Logic to promote lanes/tests/corners indicator to next step asumming once the result is written next step is being executed, but tests may write more than once
            //bool IsLastTest = false;
            //bool IsLastLane = false;
            //if (Lanes.Last().Name == lastLane)
            //{
            //    lastLane = Lanes.First().Name;
            //    IsLastLane = true;
            //}
            //else
            //{
            //    int ind = 0;
            //    foreach (MItem lane in Lanes)
            //    {
            //        if (lane.Name == lastLane)
            //        {
            //            lastLane = Lanes[ind + 1].Name;
            //            break;
            //        }
            //        ind++;
            //    }
            //}
                
            //if (IsLastLane)
            //{
            //    if (Tests.Last().Name == lastTest)
            //    {
            //        lastTest = Tests.First().Name;
            //        IsLastTest = true;
            //    }else{
            //        int ind = 0;
            //        foreach (MItem test in Tests)
            //        {
            //            if (test.Name == lastTest)
            //            {
            //                lastTest = Tests[ind + 1].Name;
            //                break;
            //            }
            //            ind++;
            //        }
            //    }
            //}

            //if (IsLastTest)
            //{
            //    if (Corners.Last().Name == lastCorner)
            //    {
            //        if (ExecutionCurStatus.ToUpper() == "FINISHED")
            //        {
            //            //All green if "Finished"
            //            lastCorner = "lastCorner";
            //            lastTest = "lastTest";
            //            lastLane = "lastLane";
            //        }else{
            //            //Last lane,test,corner should be yellow
            //            lastTest = Tests.Last().Name;
            //            lastLane = Lanes.Last().Name;
            //        }

            //    }else{
            //        int ind = 0;
            //        foreach (MItem corner in Corners)
            //        {
            //            if (corner.Name == lastCorner)
            //            {
            //                lastCorner = Corners[ind + 1].Name;
            //                break;
            //            }
            //            ind++;
            //        }
            //    }
            //}


            if (ExecutionCurStatus.ToUpper() == "FINISHED")
            {
                //All green if "Finished"
                lastCorner = "lastCorner";
                lastTest = "lastTest";
                lastPort = "lastPort";
                lastLane = "lastLane";
            }

            foreach (MItem corner in Corners)
            {
                if (lastCorner == corner.Name)
                {
                    corner.Color = "yellow";
                    break;
                }
                else
                {
                    corner.Color = "green";
                }
            }
            foreach (MItem test in Tests)
            {
                if (lastTest == test.Name)
                {
                    test.Color = "yellow";
                    break;
                }
                else
                {
                    test.Color = "green";
                }
            }
            foreach (MItem port in Ports)
            {
                if (lastPort == port.Name)
                {
                    port.Color = "yellow";
                    break;
                }
                else
                {
                    port.Color = "green";
                }
            }
            foreach (MItem lane in Lanes)
            {
                if (lastLane == lane.Name)
                {
                    lane.Color = "yellow";
                    break;
                }
                else
                {
                    lane.Color = "green";
                }
            }
        }

    }
    public class MItem
    {
        public string Name { get; set; }
        public string Color { get; set; }

        public MItem(string N, string C)
        {
            Name = N;
            Color = C;
        }
    }
}