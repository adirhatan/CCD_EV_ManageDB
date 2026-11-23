using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CCD_EV_ManageDB.Models
{
    public class MonitorData
    {
        public Dictionary<string, MongoCursor> MonitorResults = new Dictionary<string, MongoCursor>();
        public HashSet<string> Fields { get; set; }
        public List<MItem> Corners { get; set; }
        public List<MItem> Tests { get; set; }
        public List<MItem> Lanes { get; set; }

        public MonitorData()
        {
            MonitorResults = new Dictionary<string, MongoCursor>();
            Fields = new HashSet<string>();
            Corners = new List<MItem>();
            Tests = new List<MItem>();
            Lanes = new List<MItem>();
        }

        public void initSesisonVals(BsonDocument curExecutionitem)
        {
            foreach (string corner in curExecutionitem.GetElement("PlannedCorners").Value.ToString().Split(';'))
            {
                if (!String.IsNullOrEmpty(corner)) Corners.Add(new MItem(corner, "grey"));
            }
            foreach (string test in curExecutionitem.GetElement("PlannedTests").Value.ToString().Split(';'))
            {
                if (!String.IsNullOrEmpty(test)) Tests.Add(new MItem(test, "grey"));
            }
            foreach (string lane in curExecutionitem.GetElement("PlannedLanes").Value.ToString().Split(';'))
            {
                if (!String.IsNullOrEmpty(lane)) Lanes.Add(new MItem(lane, "grey"));
            }
        }
        public void prepareTable(BsonDocument curResultsItem, string status)
        {
            bool IsLastTest = false;
            bool IsLastLane = false;

            string lastLane = curResultsItem.GetValue("Lane", "").ToString();
            string lastTest = curResultsItem.GetValue("TestName", "").ToString();
            string lastCorner = curResultsItem.GetValue("Corner", "").ToString();

            if (Lanes.Last().Name == lastLane)
            {
                lastLane = Lanes.First().Name;
                IsLastLane = true;
            }
            else
            {
                int ind = 0;
                foreach (MItem lane in Lanes)
                {
                    if (lane.Name == lastLane)
                    {
                        lastLane = Lanes[ind + 1].Name;
                        break;
                    }
                    ind++;
                }
            }
                
            if (IsLastLane)
            {
                if (Tests.Last().Name == lastTest)
                {
                    lastTest = Tests.First().Name;
                    IsLastTest = true;
                }else{
                    int ind = 0;
                    foreach (MItem test in Tests)
                    {
                        if (test.Name == lastTest)
                        {
                            lastTest = Tests[ind + 1].Name;
                            break;
                        }
                        ind++;
                    }
                }
            }

            if (IsLastTest)
            {
                if (Corners.Last().Name == lastCorner)
                {
                    if (status.ToUpper() == "FINISHED")
                    {
                        //All green if "Finished"
                        lastCorner = "lastCorner";
                        lastTest = "lastTest";
                        lastLane = "lastLane";
                    }else{
                        //Last lane,test,corner should be yellow
                        lastTest = Tests.Last().Name;
                        lastLane = Lanes.Last().Name;
                    }

                }else{
                    int ind = 0;
                    foreach (MItem corner in Corners)
                    {
                        if (corner.Name == lastCorner)
                        {
                            lastCorner = Corners[ind + 1].Name;
                            break;
                        }
                        ind++;
                    }
                }
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

        //private class MItem
        //{
        //    public string Name { get; set; }
        //    public string Color { get; set; }

        //    public MItem(string N, string C)
        //    {
        //        Name = N;
        //        Color = C;
        //    }
        //}
    }

}