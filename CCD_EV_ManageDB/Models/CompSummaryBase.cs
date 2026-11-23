using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.IO;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class CompSummaryBase
    {

        public string GetResultsSumHtml(string _id,string NVM,bool Arch)
        {
            Inventory inv;
            if (!Arch) inv = (new MDBInventory()).ReadInventory(_id);
            else inv = (new MDBInventory("DB_CMP_Arch")).ReadInventory(_id);
            string dbAlias = "DB_CMP";
            if (Arch) dbAlias = "DB_CMP_Arch";
            string sum = "<p>"+ _id + " NVM - " + NVM + "</p>";
            Dictionary<string, int[]> sResults = new Dictionary<string, int[]>();
            string NVMquery = ";EQ;NVM_REV;" + NVM;
            if ((string.IsNullOrWhiteSpace(NVM)) || (NVM.ToUpper()=="ALL")) NVMquery = "";
            if (inv.Ports == "1P")
            {
                sResults.Add("TBT_TX 20G", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "20G", "1P", "TBT_TX", dbAlias));
                sResults.Add("TBT_TX 10G", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "10G", "1P", "TBT_TX", dbAlias));
                sResults.Add("TBT_RX 20G", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "20G", "1P", "TBT_RX", dbAlias));
                sResults.Add("TBT_RX 10G", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "10G", "1P", "TBT_RX", dbAlias));
                sResults.Add("TBT_RL", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "1P", "TBT_RL", dbAlias));
                sResults.Add("DP_TX PA", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PA", "DP_TX", dbAlias));
                sResults.Add("USB3p1_TX PA", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PA", "USB3p1_TX", dbAlias));
                sResults.Add("USB3p1_RX PA", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PA", "USB3p1_RX", dbAlias));
                sResults.Add("USB2p0_TX PA", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PA", "USB2p0_TX", dbAlias));
                sResults.Add("HDMI1p4_TX", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "", "HDMI1p4_TX", dbAlias));
            }
            else
            {
                sResults.Add("TBT_TX 20G", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "20G", "2P", "TBT_TX", dbAlias));
                sResults.Add("TBT_TX 10G", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "10G", "2P", "TBT_TX", dbAlias));
                sResults.Add("TBT_RX 20G", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "20G", "2P", "TBT_RX", dbAlias));
                sResults.Add("TBT_RX 10G", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "10G", "2P", "TBT_RX", dbAlias));
                sResults.Add("TBT_RL", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "2P", "TBT_RL", dbAlias));
                sResults.Add("DP_TX PA", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PA", "DP_TX", dbAlias));
                sResults.Add("DP_TX PB", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PB", "DP_TX", dbAlias));
                sResults.Add("USB3p1_TX PA", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PA", "USB3p1_TX", dbAlias));
                sResults.Add("USB3p1_TX PB", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PB", "USB3p1_TX", dbAlias));
                sResults.Add("USB3p1_RX PA", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PA", "USB3p1_RX", dbAlias));
                sResults.Add("USB3p1_RX PB", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PB", "USB3p1_RX", dbAlias));
                sResults.Add("USB2p0_TX PA", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PA", "USB2p0_TX", dbAlias));
                sResults.Add("USB2p0_TX PB", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "PB", "USB2p0_TX", dbAlias));
                sResults.Add("HDMI1p4_TX", GetResultsSumm("EQ;BoardNumber;" + _id + NVMquery, "", "", "HDMI1p4_TX", dbAlias));
            }
            //if DualPort: sResults.Add("DP_TX PA", GetResultsSumm("EQ;BoardNumber;" + _id, "", "PA", "DP_TX"));
            sum += buildHTML(sResults);
            return sum;
        }

        public int[] GetResultsSumm(string query, string gen, string port,string _dbTable,string db)
        {
            string queryPrefix="";
            int[] resultsSummary = new int[] { 0, 0, 0, 0 };
            MDBCompReportLines mdbCompReportLines = new MDBCompReportLines();
            MDBStationResults stationResults;
            switch (_dbTable)
            {
                case "TBT_TX":
                    queryPrefix="TBT_TX_";
                    stationResults = new MDBStationResults(db, "TBT_TX");
                    break;
                case "TBT_RX":
                    queryPrefix = "TBT-RX-";
                    stationResults = new MDBStationResults(db, "TBT_RX");
                    break;
                case "TBT_RL":
                    queryPrefix = "TBT-RL-";
                    stationResults = new MDBStationResults(db, "TBT_RL");
                    break;
                case "DP_TX":
                    queryPrefix="DP-";
                    stationResults = new MDBStationResults(db, "DP_TX");
                    break;
                case "USB3p1_TX":
                    queryPrefix = "USB31-TX-";
                    stationResults = new MDBStationResults(db, "USB3p1_TX");
                    break;
                case "USB3p1_RX":
                    queryPrefix = "USB31-RX-";
                    stationResults = new MDBStationResults(db, "USB3p1_RX");
                    break;
                case "USB2p0_TX":
                    queryPrefix = "USB20-TX-";
                    stationResults = new MDBStationResults(db, "USB2p0_TX");
                    break;
                case "HDMI1p4_TX":
                    queryPrefix = "HDMI1p4-.[^M]";
                    stationResults = new MDBStationResults(db, "HDMI1p4_TX");
                    break;
                default:
                    queryPrefix = "DP-";
                    stationResults = new MDBStationResults(db, "DP_TX");
                    break;
            }
            List<DBTmpl> lines = mdbCompReportLines.ReadMatchedIDQeuryCompReportLines("/^"+ queryPrefix + gen + ".*" + port + "$/");
            foreach (DBTmpl lineTmpl in lines)
            {
                List<string> results = stationResults.FindCompLine(query, lineTmpl);
                switch (results[0])
                {
                    case "Pass":
                        resultsSummary[0]++;
                        break;
                    case "Fail":
                        resultsSummary[1]++;
                        break;
                    case "NA":
                        resultsSummary[2]++;
                        break;
                    case "Multi":
                        resultsSummary[3]++;
                        break;
                }
            }

            return resultsSummary;
        }

        public string buildHTML(Dictionary<string, int[]> sResults)
        {

            StringWriter stringWriter = new StringWriter();
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "display");
                    writer.RenderBeginTag(HtmlTextWriterTag.Table);
                    writer.RenderBeginTag(HtmlTextWriterTag.Thead);
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    foreach (string col in new string[] {"", "Pass", "Fail", "NA", "Multi", "All" })
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        writer.Write(col + " &nbsp;");
                        writer.RenderEndTag();
                    }
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                    writer.RenderBeginTag(HtmlTextWriterTag.Tbody);
                    foreach (var pair in sResults)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            writer.Write(pair.Key);
                            writer.RenderEndTag();
                            for (int i = 0; i < 4; i++)
                            {
                                string style = "font-weight: bold;color:";
                                switch (i)
                                {
                                    case 0:
                                        style += "green";
                                        break;
                                    case 1:
                                        style += "red";
                                        break;
                                    case 2:
                                        style += "orange";
                                        break;
                                    case 3:
                                        style += "blue";
                                        break;
                                }
                                if (pair.Value[i]>0) writer.AddAttribute(HtmlTextWriterAttribute.Style, style);
                                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                writer.Write(pair.Value[i].ToString());
                                writer.RenderEndTag();
                            }
                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            writer.Write(pair.Value.Sum().ToString());
                            writer.RenderEndTag();
                        writer.RenderEndTag();
                    }
                    
                    writer.RenderEndTag();
                    writer.RenderEndTag();
            }
                return stringWriter.ToString();
            }

    }
}