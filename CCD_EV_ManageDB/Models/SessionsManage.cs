using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using CCD_EV_ManageDB.Repositories;
using System.Reflection;

namespace CCD_EV_ManageDB.Models
{
    public class SessionsManage
    {
        public ObjectId _id { get; set; }
        public string table { get; set; }
        public string project { get; set; }
        public string phase { get; set; }
        public string board_id { get; set; }
        public string unit_id { get; set; }
        public string NVM { get; set; }
        public string spec { get; set; } //? may take from indicator...
        public General.SessionValidationStatus validation { get; set; } 
        public double testsNumber { get; set; }
        public double timeStamp { get; set; }
        public List<string> FieldsList { get; set; }
        public BsonDocument Data;
        public List<string> Sessions { get; set; }
        public string comment { get; set; }
        public string logicUnit { get; set; }

        public SessionsManage()
        {
            _id = ObjectId.GenerateNewId();
            timeStamp = 0;
            FieldsList = new List<string>();
            Data = new BsonDocument();
            Sessions = new List<string>();
        }

        public SessionsManage(BsonDocument doc)
        {
            _id = doc.GetValue("_id", "").AsObjectId;
            table = (doc.GetValue("table", "").ToString());
            project = (doc.GetValue("project", "").ToString());
            phase = (doc.GetValue("phase", "").ToString());
            board_id = (doc.GetValue("board_id", "").ToString());
            unit_id = (doc.GetValue("unit_id", "").ToString());
            NVM = (doc.GetValue("NVM", "").ToString());
            spec = (doc.GetValue("spec", "").ToString());
            try
            {
                validation = (General.SessionValidationStatus)Enum.Parse(typeof(General.SessionValidationStatus), doc.GetValue("validation", "").ToString());
            }
            catch
            {
                validation = General.SessionValidationStatus.TMP_valid;
                //validation = General.SessionValidationStatus.PendingReview;
            }

            testsNumber = (doc.GetValue("testsNumber", "").ToDouble());
            timeStamp = (doc.GetValue("timeStamp", "").ToDouble());

            FieldsList = new List<string>();
            if (doc.Contains("FieldsList"))
            {
                BsonArray fArray = doc.GetElement("FieldsList").Value.AsBsonArray;
                foreach (string field in fArray)
                {
                    FieldsList.Add(field);
                }
            }
            Data = doc.GetElement("Data").Value.AsBsonDocument;

            Sessions = new List<string>();
            if (doc.Contains("Sessions"))
            {
                BsonArray sArray = doc.GetElement("Sessions").Value.AsBsonArray;
                foreach (string session in sArray)
                {
                    Sessions.Add(session);
                }
            }
            comment = (doc.GetValue("comment", "").ToString());
            logicUnit = (doc.GetValue("logicUnit", "").ToString());

        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", _id } };
            Doc.Add("table", table);
            Doc.Add("project", project);
            Doc.Add("phase", phase);
            Doc.Add("board_id", board_id);
            Doc.Add("unit_id", unit_id);
            Doc.Add("NVM", NVM);
            Doc.Add("spec", spec);
            Doc.Add("validation", validation.ToString());
            Doc.Add("testsNumber", testsNumber);
            Doc.Add("timeStamp", timeStamp);
            BsonArray fArray = new BsonArray();
            foreach (string field in FieldsList)
            {
                fArray.Add(field);
            }
            Doc.Add("FieldsList", fArray);
            Doc.Add("Data", Data);
            BsonArray sArray = new BsonArray();
            foreach (string session in Sessions)
            {
                sArray.Add(session);
            }
            Doc.Add("Sessions", sArray);
            Doc.Add("comment", comment);
            Doc.Add("logicUnit", logicUnit);


            return Doc;
        }

        public SessionsManage GetSessionsManageFromDBResult(BsonDocument item, string table, List<string> FieldsList, SessionsManageProtocolConf SMconf)
        {
            SessionsManage SM = new SessionsManage();
            try
            {
                SM.Sessions.Add(item.GetValue("SessionStart","").ToString());
                SM.project = item.GetValue("ProjectName", "").ToString();
                SM.phase = item.GetValue("Phase", "").ToString();
                SM.board_id = item.GetValue("BoardNumber", "").ToString();
                SM.unit_id = item.GetValue("UnitId", "").ToString();
                SM.NVM = item.GetValue("NVM_REV", "").ToString();
                SM.table = table;
                SM.FieldsList = FieldsList;
                foreach (string f in FieldsList)
                {
                    SM.Data.Add(f, item.GetValue(f,"").ToString());
                }

                //Hard codeed specific "sType" definition for several tables, may need configuration on future
                if (SM.isUSB_RxTable())
                {
                    string tmp = item.GetValue("SpecTestName", "").ToString().ToUpper();
                    if ((tmp.Contains("GEN2")) && (tmp.Contains("SHORT"))) SM.Data.Set("sType","GEN2_Short");
                    if ((tmp.Contains("GEN2")) && (tmp.Contains("LONG"))) SM.Data.Set("sType", "GEN2_Long");
                    if ((tmp.Contains("GEN1")) && (tmp.Contains("SHORT"))) SM.Data.Set("sType", "GEN1_Short");
                    if ((tmp.Contains("GEN1")) && (tmp.Contains("LONG"))) SM.Data.Set("sType", "GEN1_Long");
                    if (tmp.Contains("LFPS") || tmp.Contains("SCD_LBPM")) SM.Data.Set("sType", "LFPS_SCD_LBPM");
                    if (tmp.Contains("RXDETECT")) SM.Data.Set("sType", "RxDetect");
                }

                if (SM.isUSB_TxTable())
                {
                    string tmp = item.GetValue("TestName", "").ToString().ToUpper();
                    if ((tmp.Contains("GEN2")) && (tmp.Contains("SHORT"))) SM.Data.Set("sType", "GEN2_Short");
                    if ((tmp.Contains("GEN2")) && (tmp.Contains("LONG"))) SM.Data.Set("sType", "GEN2_Long");
                    if ((tmp.Contains("GEN1")) && (tmp.Contains("SHORT"))) SM.Data.Set("sType", "GEN1_Short");
                    if ((tmp.Contains("GEN1")) && (tmp.Contains("LONG"))) SM.Data.Set("sType", "GEN1_Long");
                    if ((tmp.Contains("GEN1")) && (!tmp.Contains("LONG"))) SM.Data.Set("sType", "GEN1_Short"); //additional tests GEN1 that are not LONG joined to GEN1_Short
                    if ((tmp.Contains("GEN2")) && (!tmp.Contains("LONG"))) SM.Data.Set("sType", "GEN2_Short"); //additional tests GEN2 that are not LONG joined to GEN2_Short
                    //if (tmp.Contains("JTF")) SM.Data.Set("sType", "JTF");
                    //if (tmp.Contains("CLOCK_SWITCH")) SM.Data.Set("sType", "Clock_Switch");
                    //if (tmp.Contains("TXRXDETECT")) SM.Data.Set("sType", "TxRxDetect");
                }

                if (SM.isCIO_RxTable())
                {
                    string tmp = item.GetValue("SpecTestName", "").ToString().ToUpper();
                    if (tmp.Contains("ACTIVE")) SM.Data.Set("sType", "Active");
                    if (tmp.Contains("PASSIVE")) SM.Data.Set("sType", "Passive");
                }


                if (SM.isCIO_Retimer())
                {
                    string tmp = item.GetValue("SpecTestName", "").ToString().ToUpper();
                    if (tmp.Contains("ACTIVE")) SM.Data.Set("sType", "Active");
                    if (tmp.Contains("TP2 ")) SM.Data.Set("sType", "Active");
                    if (tmp.Contains("TP3")) SM.Data.Set("sType", "Active");
                    if (tmp.Contains("JTF")) SM.Data.Set("sType", "Active");
                    if (tmp.Contains("PASSIVE")) SM.Data.Set("sType", "Passive");
                    if (tmp.Contains("OPTIC")) SM.Data.Set("sType", "Optic");
                    if (tmp.Contains("LRD")) SM.Data.Set("sType", "LRD");
                }


                if (SM.isCrystalTable())
                {
                    string tmp = item.GetValue("ESR", "").ToString();
                    SM.Data.Set("sType", tmp);
                }

                if (SM.isPowerConsTable())
                {
                    string tmp = item.GetValue("Chip/Solution", "").ToString().ToUpper();
                    SM.Data.Set("sType", tmp);
                }

                //New Logic Unit Assignment
                Unit tmpLogicUnit = new Unit();
                tmpLogicUnit.skew = SM.Data.GetValue("Skew", "").ToString();
                if ((SM.board_id.ToUpper().Contains("SHORT")) || (SM.board_id.ToUpper().Contains("LONG"))) tmpLogicUnit.type = SM.board_id.ToUpper().Substring(0, 1);
                //
                string firstType = SMconf.units[0].type;
                foreach (Unit tu in SMconf.units)
                {
                    if (tu.type!=firstType)
                    { 
                        firstType = "";
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(firstType)) tmpLogicUnit.type = firstType; //if we have only one board type, assign it
                //
                
                if (SM.isCIO_RxTable()) tmpLogicUnit.setup = SM.Data.GetValue("sType", "").ToString().Substring(0, 1);
                if (SM.isCIO_Retimer())
                {
                    tmpLogicUnit.setup = SM.Data.GetValue("sType", "").ToString().Substring(0, 1);
                }
                if ((SM.isUSB_RxTable()) || (SM.isUSB_TxTable()))
                {
                    if (SM.Data.GetValue("sType", "").ToString().ToUpper().Contains("SHORT")) tmpLogicUnit.type = "S";
                    if (SM.Data.GetValue("sType", "").ToString().ToUpper().Contains("LONG")) tmpLogicUnit.type = "L";
                }
                if ((SM.isUSB_RxTable()) || (SM.isUSB_TxTable())) tmpLogicUnit.setup = SM.Data.GetValue("Test Channel", "??").ToString().Substring(0, 1);
                if (SM.isPowerConsTable())
                {
                    tmpLogicUnit.type = "P";// always??
                    if (!string.IsNullOrEmpty(SM.Data.GetValue("sType", "").ToString())) tmpLogicUnit.setup = SM.Data.GetValue("sType", "").ToString().ToUpper().Substring(0, 1);
                }
                SM.logicUnit = CheckLogicUnitDefault(tmpLogicUnit, SMconf);

            }
            catch
            {
                SM.logicUnit = CheckLogicUnitDefault(new Unit(), SMconf);
                //continue;
            }

            return SM;
        }

        private string CheckLogicUnitDefault(Unit tmpLogicUnit, SessionsManageProtocolConf SMconf)
        {
            int cnt = 0;
            string UnitStr = "";
            if (string.IsNullOrWhiteSpace(tmpLogicUnit.skew))
            {
                UnitStr = "??_??_??_??";
                return UnitStr;
            }
            else UnitStr = tmpLogicUnit.skew;

            if (string.IsNullOrWhiteSpace(tmpLogicUnit.type))
            {
                UnitStr += "_??_??_??";
                return UnitStr;
            }
            else UnitStr += "_" + tmpLogicUnit.type;

            if (string.IsNullOrWhiteSpace(tmpLogicUnit.setup))
            {
                Unit defaultMatch = new Unit();
                foreach (Unit u in SMconf.units)
                {
                    if ((u.skew == tmpLogicUnit.skew) && (u.type == tmpLogicUnit.type))
                    {
                        defaultMatch = u;
                        cnt++;
                    }
                }
                if (cnt == 1) UnitStr = defaultMatch.ToString();
                else UnitStr += "_??_??";
                return UnitStr;
            }
            else UnitStr += "_" + tmpLogicUnit.setup;

            foreach (Unit u in SMconf.units)
            {
                if ((u.skew == tmpLogicUnit.skew) && (u.type == tmpLogicUnit.type) && (u.setup == tmpLogicUnit.setup)) cnt++;
            }
            if (cnt == 0) UnitStr += "_??"; // this case should not happen
            if (cnt == 1) UnitStr += "_1";
            if (cnt > 1) UnitStr += "_??"; // search for already assigned board will be done later

            return UnitStr;

        }

        public bool ImpEquals(SessionsManage targetSession)
        {
            if (this.validation != targetSession.validation) return false;
            return ImpEqualsExceptValidatoin(targetSession);
        }

        public bool ImpEqualsExceptValidatoin(SessionsManage targetSession)
        {
            if (this.board_id != targetSession.board_id) return false;
            if (this.unit_id != targetSession.unit_id) return false;
            if (this.NVM != targetSession.NVM) return false;
            if (this.Data.GetValue("sType", "").ToString() != targetSession.Data.GetValue("sType", "").ToString()) return false;
            return true;
        }

        public bool ImpEqualsExceptNVM(SessionsManage targetSession)
        {
            if (this.validation != targetSession.validation) return false;
            if (this.board_id != targetSession.board_id) return false;
            if (this.unit_id != targetSession.unit_id) return false;
            //if (this.NVM != targetSession.NVM) return false;
            if (this.Data.GetValue("sType", "").ToString() != targetSession.Data.GetValue("sType", "").ToString()) return false;
            return true;
        }

        bool isCIO_RxTable()
        {
            if ((this.table.StartsWith("CIO")) && (this.table.Contains("Rx"))) return true;
            if ((this.table.StartsWith("TBT")) && (this.table.Contains("Rx"))) return true;
            return false;
        }

        bool isCIO_Retimer()
        {
            if ((this.table.StartsWith("CIO")) && (this.table.ToUpper().Contains("RETIMER"))) return true;
            if ((this.table.StartsWith("TBT")) && (this.table.ToUpper().Contains("RETIMER"))) return true;
            return false;
        }

        bool isUSB_RxTable()
        {
            if ((this.table.StartsWith("USB")) && (this.table.Contains("_RX"))) return true;
            return false;
        }

        bool isUSB_TxTable()
        {
            if ((this.table.StartsWith("USB")) && (this.table.Contains("_TX"))) return true;
            return false;
        }
        bool isCrystalTable()
        {
            if (this.table.StartsWith("Crystal")) return true;
            return false;
        }
        bool isPowerConsTable()
        {
            if (this.table.StartsWith("Power_Consumption")) return true;
            return false;
        }
    }

    public class SessionsManageOfTable
    {
        public string table { get; set; }
        public List<SessionsManage> Sessions { get; set; }
        public Dictionary<string, BsonDocument> SessionStartInfoDictionary { get; set; } // key: SessionStart  value: Bson document with Info about results - Comment, Number of results (TMP/PRM_valid/PRM_not valid/ALL ?), Pass/Fail etc
        public List<SessionsManage> MissingSessions { get; set; }
        public Dictionary<string, BsonDocument> MissingSessionsInSMDictionary { get; set; } // key: SessionStart  value: Bson document with Info about results - Comment, Number of results (TMP/PRM_valid/PRM_not valid/ALL ?), Pass/Fail etc
        public List<string> SpecLimitsConflictSessions { get; set; }

        public SessionsManageOfTable(string table)
        {
            this.table = table;
            Sessions = new List<SessionsManage>();
            SessionStartInfoDictionary = new Dictionary<string, BsonDocument>();
            MissingSessions = new List<SessionsManage>();
            MissingSessionsInSMDictionary = new Dictionary<string, BsonDocument>();
            SpecLimitsConflictSessions = new List<string>();

        }
    }

    public class SessionsManageProtocolConf
    {
        public string proj { get; set; }
        public string phase { get; set; }
        public string protocol { get; set; }
        public List<string> tables { get; set; }
        public List<string> fields { get; set; }
        public List<string> fieldsAliases { get; set; }
        public double timestamp;
        public double locktimestamp;
        //for Cycle status:
        public List<string> corners { get; set; }
        public List<string> ports { get; set; }
        public List<string> excelPorts { get; set; }
        public List<string> lanes { get; set; }
        public string SRTemplate { get; set; }
        public string separateCSRperTable { get; set; }
        
        public Dictionary<string,Dictionary<string, TableDiv>> table_divs { get; set; } // key:DBtable value:Dictionary of TableDivs of this DBTable
        public List<Unit> units { get; set; }
        public Dictionary<string, string> ActiveFileNames { get; set; }
        public List<Mask> masks { get; set; }
        public string useNewCSR { get; set; }
        public string RebuildFieldsData { get; set; }

        public SessionsManageProtocolConf()
        {
            tables = new List<string>();
            fields = new List<string>();
            fieldsAliases = new List<string>();
            timestamp = Convert.ToDouble((DateTime.Now.AddDays(-7L)).ToString("yyyyMMddHHmmss"));
            locktimestamp = Convert.ToDouble(0);
            corners = new List<string>();
            ports = new List<string>();
            excelPorts = new List<string>();
            lanes = new List<string>();
            table_divs = new Dictionary<string, Dictionary<string, TableDiv>>();
            units = new List<Unit>();
            ActiveFileNames = new Dictionary<string, string>();
            masks = new List<Mask>();
        }

        public SessionsManageProtocolConf(BsonDocument doc)
        {
            proj = (doc.GetValue("proj", "").ToString());
            phase = (doc.GetValue("phase", "").ToString());
            protocol = (doc.GetValue("protocol", "").ToString()); 
            timestamp = (doc.GetValue("timestamp", 0).ToDouble());
            locktimestamp = (doc.GetValue("locktimestamp", 0).ToDouble());

            fields = new List<string>();
            if (doc.Contains("fields"))
            {
                BsonArray fArray = doc.GetElement("fields").Value.AsBsonArray;
                foreach (string field in fArray)
                {
                    fields.Add(field);
                }
            }

            fieldsAliases = new List<string>();
            if (doc.Contains("fieldsAliases"))
            {
                BsonArray faArray = doc.GetElement("fieldsAliases").Value.AsBsonArray;
                foreach (string field in faArray)
                {
                    fieldsAliases.Add(field);
                }
            }
            tables = new List<string>();
            if (doc.Contains("tables"))
            {
                BsonArray tArray = doc.GetElement("tables").Value.AsBsonArray;
                foreach (string session in tArray)
                {
                    tables.Add(session);
                }
            }

            corners = new List<string>();
            if (doc.Contains("corners"))
            {
                BsonArray cArray = doc.GetElement("corners").Value.AsBsonArray;
                foreach (string corner in cArray)
                {
                    corners.Add(corner);
                }
            }

            ports = new List<string>();
            if (doc.Contains("ports"))
            {
                BsonArray pArray = doc.GetElement("ports").Value.AsBsonArray;
                foreach (string port in pArray)
                {
                    ports.Add(port);
                }
            }

            excelPorts = new List<string>();
            if (doc.Contains("excelPorts"))
            {
                BsonArray eArray = doc.GetElement("excelPorts").Value.AsBsonArray;
                foreach (string excelPort in eArray)
                {
                    excelPorts.Add(excelPort);
                }
            }

            lanes = new List<string>();
            if (doc.Contains("lanes"))
            {
                BsonArray lArray = doc.GetElement("lanes").Value.AsBsonArray;
                foreach (string lane in lArray)
                {
                    lanes.Add(lane);
                }
            }

            SRTemplate = (doc.GetValue("SRTemplate", "").ToString());
            separateCSRperTable = (doc.GetValue("separateCSRperTable", "").ToString());

            table_divs = new Dictionary<string, Dictionary<string, TableDiv>>();
            if (doc.Contains("tableDivs"))
            {
                BsonArray tdArray = doc.GetElement("tableDivs").Value.AsBsonArray;
               
                
                foreach (BsonDocument td in tdArray)
                {
                    TableDiv tmp = new TableDiv(td);
                    if (!table_divs.ContainsKey(tmp.table_name)) table_divs.Add(tmp.table_name, new Dictionary<string, TableDiv>());
                    table_divs[tmp.table_name].Add(tmp.name, tmp);
                }
            }

            units = new List<Unit>();
            if (doc.Contains("units"))
            {
                BsonArray uArray = doc.GetElement("units").Value.AsBsonArray;
                foreach (BsonDocument unitDoc in uArray)
                {
                    units.Add(new Unit(unitDoc));
                }
            }

            ActiveFileNames = new Dictionary<string, string>();
            if (doc.Contains("ActiveFileNames"))
            {
                BsonArray acArray = doc.GetElement("ActiveFileNames").Value.AsBsonArray;
                foreach (BsonDocument ActiveFileName in acArray)
                {
                    ActiveFileNames.Add(ActiveFileName.GetValue("key").ToString(),ActiveFileName.GetValue("value").ToString());
                }
            }

            masks = new List<Mask>();
            if (doc.Contains("masks"))
            {
                BsonArray msArray = doc.GetElement("masks").Value.AsBsonArray;
                foreach (BsonDocument maskDoc in msArray)
                {
                    masks.Add(new Mask(maskDoc));
                }
            }

            useNewCSR = (doc.GetValue("useNewCSR", "").ToString());
            RebuildFieldsData = (doc.GetValue("RebuildFieldsData", "").ToString()); 

        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", protocol + '_' + proj + '_' + phase } };
            Doc.Add("proj", proj);
            Doc.Add("phase", phase);
            Doc.Add("protocol", protocol);
            Doc.Add("timestamp", timestamp);
            Doc.Add("locktimestamp", locktimestamp);
            BsonArray fArray = new BsonArray();
            foreach (string field in fields)
            {
                fArray.Add(field);
            }
            Doc.Add("fields", fArray);

            BsonArray faArray = new BsonArray();
            foreach (string field in fieldsAliases)
            {
                faArray.Add(field);
            }
            Doc.Add("fieldsAliases", faArray);

            BsonArray tArray = new BsonArray();
            foreach (string table in tables)
            {
                tArray.Add(table);
            }
            Doc.Add("tables", tArray);

            BsonArray cArray = new BsonArray();
            foreach (string corner in corners)
            {
                cArray.Add(corner);
            }
            if (corners.Count>0) Doc.Add("corners", cArray);

            BsonArray pArray = new BsonArray();
            foreach (string port in ports)
            {
                pArray.Add(port);
            }
            Doc.Add("ports", pArray);

            BsonArray eArray = new BsonArray();
            foreach (string excelPort in excelPorts)
            {
                eArray.Add(excelPort);
            }
            Doc.Add("excelPorts", eArray);

            BsonArray lArray = new BsonArray();
            foreach (string lane in lanes)
            {
                lArray.Add(lane);
            }
            Doc.Add("lanes", lArray);

            Doc.Add("SRTemplate", SRTemplate);
            Doc.Add("separateCSRperTable", separateCSRperTable);
            

            BsonArray tdArray = new BsonArray();
            foreach (KeyValuePair<string, Dictionary<string, TableDiv>> tdPair in table_divs)
            {
                foreach (KeyValuePair<string, TableDiv> td in tdPair.Value)
                {
                    tdArray.Add(td.Value.toBsonDocument());
                }
            }
            Doc.Add("tableDivs", tdArray);

            BsonArray uArray = new BsonArray();
            foreach ( Unit unit in units)
            {
                uArray.Add(unit.toBsonDocument());
            }
            Doc.Add("units", uArray);

            BsonArray acArray = new BsonArray();
            foreach (var ActiveFileName in ActiveFileNames)
            {
                BsonDocument tmpActiveFileName = new BsonDocument();
                tmpActiveFileName.Set("key", ActiveFileName.Key);
                tmpActiveFileName.Set("value", ActiveFileName.Value);
                acArray.Add(tmpActiveFileName);
            }
            Doc.Add("ActiveFileNames", acArray);

            BsonArray msArray = new BsonArray();
            foreach (Mask mask in masks)
            {
                msArray.Add(mask.toBsonDocument());
            }
            Doc.Add("masks", msArray);

            if (!String.IsNullOrEmpty(useNewCSR)) Doc.Add("useNewCSR", useNewCSR);
            if (!String.IsNullOrEmpty(RebuildFieldsData)) Doc.Add("RebuildFieldsData", RebuildFieldsData);
            

            return Doc;
        }

        public List<string> GetLogicUnitsList()
        {
            List<string> logicUnits = new List<string>();
            foreach (Unit u in this.units) logicUnits.Add(u.ToString());
            logicUnits.Sort();
            return logicUnits;
        }

        public string getActiveFileName(string FileName)
        {
            if (ActiveFileNames.ContainsKey(FileName)) return ActiveFileNames[FileName];
            else return FileName;
        }

        public void updateActiveFileName(string FileNameKey, string FileNameValue)
        {
            if (!ActiveFileNames.ContainsKey(FileNameKey)) ActiveFileNames.Add(FileNameKey, FileNameValue);
            else ActiveFileNames[FileNameKey] = FileNameValue;
        }

        public void InsertTableDiv(string col, TableDiv TDV, string pos)
        {
            if (table_divs[col].ContainsKey(TDV.name)) return;

            if (!table_divs[col].ContainsKey(pos))
            {
                table_divs[col].Add(TDV.name, TDV);
                return;
            }
            Dictionary<string, TableDiv> newTableDivs = new Dictionary<string, TableDiv>();
            foreach (var eTDV in table_divs[col])
            {
                newTableDivs.Add(eTDV.Key, eTDV.Value);
                if (eTDV.Key == pos) newTableDivs.Add(TDV.name, TDV);
            }
            table_divs[col] = newTableDivs;
        }

        //need to test
        public void orderUpDiv(string col,string Name)
        {
            if (!table_divs[col].ContainsKey(Name)) return;
            if (table_divs[col].Count == 1)
            {
                orderUpDivOfTable(col);
                return;
            }

            Dictionary<string, TableDiv> newTableDivs = new Dictionary<string, TableDiv>();
            string prevName = "";
            foreach (var eTDV in table_divs[col])
            {
                if ((eTDV.Key != Name) && (eTDV.Key != prevName)) prevName = eTDV.Key;
                if (eTDV.Key == Name) break;
            }
            if (String.IsNullOrEmpty(prevName)) return;
            foreach (var eTDV in table_divs[col])
            {
                if ((eTDV.Key != Name) && (eTDV.Key != prevName)) newTableDivs.Add(eTDV.Key, eTDV.Value);
                if (eTDV.Key == prevName)
                {
                    newTableDivs.Add(Name, table_divs[col][Name]);
                    newTableDivs.Add(prevName, table_divs[col][prevName]);
                }
            }
            table_divs[col] = newTableDivs;
        }
        public void orderUpDivOfTable(string col)
        {
            Dictionary<string, Dictionary<string, TableDiv>> newTable_divs = new Dictionary<string, Dictionary<string, TableDiv>>();
            string prevCol = "";
            foreach (var table_divsCol in table_divs.Keys)
            {
                if ((table_divsCol != col) && (table_divsCol != prevCol)) prevCol = table_divsCol;
                if (table_divsCol == col) break;
            }
            if (String.IsNullOrEmpty(prevCol)) return;
            foreach (var table_divsCol in table_divs.Keys)
            {
                if ((table_divsCol != col) && (table_divsCol != prevCol)) newTable_divs.Add(table_divsCol, table_divs[table_divsCol]);
                if (table_divsCol == prevCol)
                {
                    newTable_divs.Add(col, table_divs[col]);
                    newTable_divs.Add(prevCol, table_divs[prevCol]);
                }
            }
            table_divs = newTable_divs;
        }

        //need to test
        public void orderDownDiv(string col, string Name)
        {
            if (!table_divs[col].ContainsKey(Name)) return;
            if (table_divs[col].Count == 1)
            {
                orderDownDivOfTable(col);
                return;
            }
            Dictionary<string, TableDiv> newTableDivs = new Dictionary<string, TableDiv>();
            string nextName = "";
            bool nameReached = false;
            foreach (var eTDV in table_divs[col])
            {
                if (nameReached)
                {
                    nextName = eTDV.Key;
                    break;
                }
                if (eTDV.Key == Name) nameReached = true; ;
            }
            if (String.IsNullOrEmpty(nextName)) return;
            foreach (var eTDV in table_divs[col])
            {
                if ((eTDV.Key != Name) && (eTDV.Key != nextName)) newTableDivs.Add(eTDV.Key, eTDV.Value);
                if (eTDV.Key == nextName)
                {
                    newTableDivs.Add(nextName, table_divs[col][nextName]);
                    newTableDivs.Add(Name, table_divs[col][Name]);
                }
            }
            table_divs[col] = newTableDivs;
        }

        public void orderDownDivOfTable(string col)
        {
            Dictionary<string, Dictionary<string, TableDiv>> newTable_divs = new Dictionary<string, Dictionary<string, TableDiv>>();
            string nextCol = "";
            bool colReached = false;
            foreach (var table_divsCol in table_divs.Keys)
            {
                if (colReached)
                {
                    nextCol = table_divsCol;
                    break;
                }
                if (table_divsCol == col) colReached = true;
            }
            if (String.IsNullOrEmpty(nextCol)) return;
            foreach (var table_divsCol in table_divs.Keys)
            {
                if ((table_divsCol != col) && (table_divsCol != nextCol)) newTable_divs.Add(table_divsCol, table_divs[table_divsCol]);
                if (table_divsCol == nextCol)
                {
                    newTable_divs.Add(nextCol, table_divs[nextCol]);
                    newTable_divs.Add(col, table_divs[col]);
                }
            }
            table_divs = newTable_divs;
        }

        public bool ImpEquals(SessionsManageProtocolConf targetProtocol)
        {
            if (table_divs.Count!=targetProtocol.table_divs.Count) return false;
            foreach (var TDVPair in table_divs)
            {
                if (TDVPair.Value.Count != targetProtocol.table_divs[TDVPair.Key].Count) return false;
                int cnt=0;
                foreach (var TDVp in TDVPair.Value)
                {
                    if ((!targetProtocol.table_divs.ContainsKey(TDVPair.Key)) || 
                        (!targetProtocol.table_divs[TDVPair.Key].ContainsKey(TDVp.Key)) ||
                        //(!TDVp.Value.ImpEquals(targetProtocol.table_divs[TDVPair.Key][TDVp.Key]))) return false;
                        (!TDVp.Value.ImpEquals(targetProtocol.table_divs[TDVPair.Key].ElementAt(cnt).Value))) return false;
                    cnt++;
                }
            }
            return true;
        }


  
    }

    public class TableDiv
    {
        public string name { get; set; }
        public string table_name { get; set; }
        public string SRreportId { get; set; }
        public Dictionary<string,TableDivLine> TableDivLines { get; set; }
        public List<string> reportIds { get; set; }

        public TableDiv()
        {
            TableDivLines = new Dictionary<string,TableDivLine>();
            reportIds = new List<string>();
        }

        public TableDiv(string _name, string _table_name)
        {
            name = _name;
            table_name = _table_name;
            TableDivLines = new Dictionary<string,TableDivLine>();
            reportIds = new List<string>();
        }
        public TableDiv(BsonDocument doc)
        {
            name = (doc.GetValue("_id", "").ToString());
            table_name = (doc.GetValue("table_name", "").ToString());
            SRreportId = (doc.GetValue("SRreportId", "").ToString());
            TableDivLines = new Dictionary<string,TableDivLine>();
            if (doc.Contains("tableDivLines"))
            {
                BsonArray tArray = doc.GetElement("tableDivLines").Value.AsBsonArray;
                foreach (BsonDocument TableDivLineDoc in tArray)
                {
                    TableDivLine tmp = new TableDivLine(TableDivLineDoc);
                    TableDivLines.Add(tmp.name, tmp);
                }
            }
            reportIds = new List<string>();
            if (doc.Contains("reportIds"))
            {
                BsonArray rArray = doc.GetElement("reportIds").Value.AsBsonArray;
                foreach (string reportId in rArray)
                {
                    reportIds.Add(reportId);
                }
            }

        }
        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", name } };
            Doc.Add("table_name", table_name);
            Doc.Add("SRreportId", SRreportId);
            BsonArray tArray = new BsonArray();
            foreach (KeyValuePair<string, TableDivLine> TableDivLine in TableDivLines)
            {
                tArray.Add(TableDivLine.Value.toBsonDocument());
            }
            Doc.Add("tableDivLines", tArray);
            BsonArray rArray = new BsonArray();
            foreach (string reportId in reportIds)
            {
                rArray.Add(reportId);
            }
            Doc.Add("reportIds", rArray);
            return Doc;
        }

        public void InsertDivLine(TableDivLine TDL, string pos)
        {
            if (!TableDivLines.ContainsKey(pos))
            {
                TableDivLines.Add(TDL.name,TDL);
                return;
            }
            Dictionary<string,TableDivLine> newTableDivLines = new Dictionary<string,TableDivLine>();
            foreach (var eTDL in TableDivLines)
            {
                newTableDivLines.Add(eTDL.Key,eTDL.Value);
                if (eTDL.Key==pos) newTableDivLines.Add(TDL.name,TDL);
            }
            TableDivLines = newTableDivLines;
        }

        public void RemoveDivLine(string name)
        {
            if (TableDivLines.ContainsKey(name)) TableDivLines.Remove(name);
        }

        public void updateDivLine(TableDivLine TDL, string origName)
        {
            if (!TableDivLines.ContainsKey(origName))
            {
                return;
            }
            Dictionary<string,TableDivLine> newTableDivLines = new Dictionary<string,TableDivLine>();
            foreach (var eTDL in TableDivLines)
            {
                if (eTDL.Key == origName) newTableDivLines.Add(TDL.name, TDL);
                else newTableDivLines.Add(eTDL.Key,eTDL.Value);
            }
            TableDivLines = newTableDivLines;
        }

        public void orderUpDivLine(string Name)
        {
            if (!TableDivLines.ContainsKey(Name))
            {
                return;
            }
            Dictionary<string, TableDivLine> newTableDivLines = new Dictionary<string, TableDivLine>();
            string prevName = "";
            foreach (var eTDL in TableDivLines)
            {
                if ((eTDL.Key != Name) && (eTDL.Key != prevName)) prevName = eTDL.Key;
                if (eTDL.Key == Name) break;
            }
            if (String.IsNullOrEmpty(prevName)) return;
            foreach (var eTDL in TableDivLines)
            {
                if ((eTDL.Key != Name) && (eTDL.Key != prevName)) newTableDivLines.Add(eTDL.Key,eTDL.Value);
                if (eTDL.Key == prevName)
                {
                    newTableDivLines.Add(Name, TableDivLines[Name]);
                    newTableDivLines.Add(prevName, TableDivLines[prevName]);
                }
            }
            TableDivLines = newTableDivLines;
        }

        public void orderDownDivLine(string Name)
        {
            if (!TableDivLines.ContainsKey(Name))
            {
                return;
            }
            Dictionary<string, TableDivLine> newTableDivLines = new Dictionary<string, TableDivLine>();
            string nextName = "";
            bool nameReached = false;
            foreach (var eTDL in TableDivLines)
            {
                if (nameReached)
                {
                    nextName = eTDL.Key;
                    break;
                }
                if (eTDL.Key == Name) nameReached = true; ;
            }
            if (String.IsNullOrEmpty(nextName)) return;
            foreach (var eTDL in TableDivLines)
            {
                if ((eTDL.Key != Name) && (eTDL.Key != nextName)) newTableDivLines.Add(eTDL.Key, eTDL.Value);
                if (eTDL.Key == nextName)
                {
                    newTableDivLines.Add(nextName, TableDivLines[nextName]);
                    newTableDivLines.Add(Name, TableDivLines[Name]);
                }
            }
            TableDivLines = newTableDivLines;
        }

        public bool ImpEquals(TableDiv targetTableDiv)
        {
            if (this.name != targetTableDiv.name) return false;
            if (this.TableDivLines.Count != targetTableDiv.TableDivLines.Count) return false;
            int cnt = 0;
            foreach (var TDLPair in TableDivLines)
            {
                if ((!targetTableDiv.TableDivLines.ContainsKey(TDLPair.Key)) ||
                    (!TDLPair.Value.ImpEquals(targetTableDiv.TableDivLines.ElementAt(cnt).Value))) return false;
                cnt++;
            }
            return true;
        }
    }

    /// <summary>
    /// //
    /// </summary>
    public class TableDivLine
    {
        public string name { get; set; }
        public int trials { get; set; }
        public Dictionary<string,int> setup_trials {get; set;}
        public int numOfTests { get; set; }
        public Dictionary<string, int> setup_numOfTests { get; set; }
        public List<TableDivField> tableDivFields { get; set; }

        public TableDivLine()
        {
            trials = 1;
            numOfTests = 1;
            tableDivFields = new List<TableDivField>();
            setup_trials = new Dictionary<string, int>();
            setup_numOfTests = new Dictionary<string,int>();
        }

        public TableDivLine(string _name, List<TableDivField>_fields)
        {
            name = _name;
            trials = 1;
            numOfTests = 1;
            tableDivFields = new List<TableDivField>();
            setup_trials = new Dictionary<string, int>();
            setup_numOfTests = new Dictionary<string, int>();
        }


        public TableDivLine(BsonDocument doc)
        {
            name = (doc.GetValue("_id", "").ToString());
            trials = (doc.GetValue("trials", "1").ToInt32());
            numOfTests = (doc.GetValue("numOfTests", "1").ToInt32());
            tableDivFields = new List<TableDivField>();
            if (doc.Contains("tableDivFields"))
            {
                BsonArray fArray = doc.GetElement("tableDivFields").Value.AsBsonArray;
                foreach (BsonDocument tableDivField in fArray)
                {
                    tableDivFields.Add(new TableDivField(tableDivField));
                }
            }
            setup_trials = new Dictionary<string, int>();
            if (doc.Contains("setup_trials"))
            {
                BsonArray stArray = doc.GetElement("setup_trials").Value.AsBsonArray;
                foreach (BsonDocument setup_trial in stArray)
                {
                    setup_trials.Add(setup_trial.GetValue("key","").ToString(),setup_trial.GetValue("value",1).ToInt32());
                }
            }
            setup_numOfTests = new Dictionary<string, int>();
            if (doc.Contains("setup_numOfTests"))
            {
                BsonArray snArray = doc.GetElement("setup_numOfTests").Value.AsBsonArray;
                foreach (BsonDocument setup_numOfTest in snArray)
                {
                    setup_numOfTests.Add(setup_numOfTest.GetValue("key", "").ToString(), setup_numOfTest.GetValue("value", 1).ToInt32());
                }
            }
        }
        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument {{ "_id", name }};
            Doc.Add("trials", trials);
            Doc.Add("numOfTests", numOfTests);
            BsonArray fArray = new BsonArray();
            foreach (TableDivField tableDivField in tableDivFields)
            {
                fArray.Add(tableDivField.toBsonDocument());
            }
            Doc.Add("tableDivFields", fArray);
            BsonArray stArray = new BsonArray();
            foreach (var pair in setup_trials)
            {
                BsonDocument doc = new BsonDocument();
                doc.Set("key", pair.Key);
                doc.Set("value", pair.Value);
                stArray.Add(doc);
            }
            Doc.Add("setup_trials", stArray);
            BsonArray snArray = new BsonArray();
            foreach (var pair in setup_numOfTests)
            {
                BsonDocument doc = new BsonDocument();
                doc.Set("key", pair.Key);
                doc.Set("value", pair.Value);
                snArray.Add(doc);
            }
            Doc.Add("setup_numOfTests", snArray);
            return Doc;
        }
        public bool ImpEquals(TableDivLine targetTableline)
        {
            int cnt = 0;
            if (this.name != targetTableline.name) return false;
            if (this.numOfTests != targetTableline.numOfTests) return false;
            if (this.trials != targetTableline.trials) return false;
            if (this.tableDivFields.Count != targetTableline.tableDivFields.Count) return false;
            foreach (TableDivField TDF in tableDivFields)
            {
                if (!TDF.ImpEquals(targetTableline.tableDivFields[cnt])) return false;
                cnt++;
            }
            return true;
        }

        public List<string> getSpecTestNamesOfSpecSubSet()
        {
            //This method returns 0 in case SpecTestName SubSet is not in use. In such cases calc will be done in "Old method" 
            List<string> divLineSpecTestNames = new List<string>();
            foreach (TableDivField TDField in tableDivFields)
            {
                if (TDField.isSpecSubSet) divLineSpecTestNames.Add(TDField.values[0]);
            }
            return divLineSpecTestNames;
        }

        public bool isDivLineWithAddCondoition()
        {
            List<string> divLineSpecTestNames = new List<string>();
            foreach (TableDivField TDField in tableDivFields)
            {
                if (!TDField.isSpecSubSet) return true;
            }
            return false;
        }

        public bool isDivLineWithAddCondoition_SpecTestName()
        {
            List<string> divLineSpecTestNames = new List<string>();
            foreach (TableDivField TDField in tableDivFields)
            {
                if ((!TDField.isSpecSubSet) && (TDField.name=="SpecTestName"))return true;
            }
            return false;
        }

        public bool isDivLineWithMixedCondoitions()
        {
            List<string> divLineSpecTestNames = new List<string>();
            bool isSpecSubSetExist = false;
            foreach (TableDivField TDField in tableDivFields)
            {
                if (TDField.isSpecSubSet) isSpecSubSetExist =  true;
            }
            if ((isSpecSubSetExist) && (isDivLineWithAddCondoition())) return true;
            return false;
        }
    }

    public class TableDivField
    {
        public string name { get; set; }
        public string condition { get; set; }
        public List<string> values { get; set; }
        public bool isSpecSubSet { get; set; }

        public TableDivField()
        {
            values = new List<string>();
        }

        public TableDivField(string _name, string _val)
        {
            name = _name;
            condition = "equals";
            values = new List<string>();
            values.Add(_val);
        }

        public TableDivField(string _name, List<string> _values)
        {
            name = _name;
            condition = "equals";
            values = new List<string>(_values);
        }

        public TableDivField(string _name, string _val, bool _isSpecSubSet)
        {
            name = _name;
            condition = "equals";
            values = new List<string>();
            values.Add(_val);
            isSpecSubSet = _isSpecSubSet;
        }

        public TableDivField(BsonDocument doc)
        {
            name = (doc.GetValue("_id", "").ToString());
            condition = (doc.GetValue("condition", "").ToString());
            values = new List<string>();
            if (doc.Contains("values"))
            {
                BsonArray vArray = doc.GetElement("values").Value.AsBsonArray;
                foreach (string val in vArray)
                {
                    values.Add(val);
                }
            }
            isSpecSubSet = ((doc.GetValue("isSpecSubSet", "").ToString()=="Y"));
        }
        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument { { "_id", name } };
            Doc.Add("condition", condition);
            BsonArray vArray = new BsonArray();
            foreach (string val in values)
            {
                vArray.Add(val);
            }
            Doc.Add("values", vArray);
            if (isSpecSubSet) Doc.Add("isSpecSubSet", "Y");
            return Doc;
        }

        public bool ImpEquals(TableDivField targetTableField)
        {
            if (this.name != targetTableField.name) return false;
            if (this.condition != targetTableField.condition) return false;
            if (!this.values.SequenceEqual(targetTableField.values)) return false;
           

            return true;
        }

    }


    public class Unit : IComparable<Unit>
    {
        public string skew { get; set; }
        public string type { get; set; }
        public string setup { get; set; }
        public string num { get; set; }
        public int CompareTo(Unit other)
        {
            if ((skew == "TYP") && (other.skew == "SS"))
            {
                return -1;
            }
            if ((skew == "SS") && (other.skew == "TYP"))
            {
                return 1;
            }
            return (skew+num+type+setup).CompareTo(other.skew+other.num+other.type+other.setup);
        }

        public Unit() { }

        public override string ToString()
        {
            return skew+"_"+type+"_"+setup+"_"+num;
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument();
            Doc.Add("skew", skew);
            Doc.Add("type", type);
            Doc.Add("setup", setup);
            Doc.Add("num", num);
            return Doc;
        }
        public Unit(string unitStr)
        {
            List<string> tmp = new List<string>(unitStr.Split('_'));
            if (tmp.Count<4) return;
            skew = tmp[0];
            type = tmp[1];
            setup = tmp[2];
            num = tmp[3];
        }

        public Unit(BsonDocument doc)
        {
            skew = (doc.GetValue("skew", "").ToString());
            type = (doc.GetValue("type", "").ToString());
            setup = (doc.GetValue("setup", "").ToString());
            num = (doc.GetValue("num", "").ToString());
        }
        public bool IsShort()
        {
            if (type == "S") return true;
            return false;
        }
        public bool IsLong()
        {
            if (type == "L") return true;
            return false;
        }

    }

    public class SessionsManageDisplay
    {
        public SessionsManageProtocolConf Conf;
        public List<SessionsManageOfTable> SessionsManageOfTables { get; set; }
        public string FilterSessionsView;

        public SessionsManageDisplay(string protocol,string proj, string phase )
        {
            Conf = new SessionsManageProtocolConf();
            Conf.proj = proj;
            Conf.phase = phase;
            Conf.protocol = protocol;
            SessionsManageOfTables = new List<SessionsManageOfTable>();
        }
    }

    public class Mask
    {
        public string RuleItem { get; set; }
        public string RuleAction { get; set; }
        public string ExcludeItem { get; set; }
        public string ExcludeDetails { get; set; }
        public string table { get; set; }

        public Mask() 
        {
            table = "ALL";
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument Doc = new BsonDocument();
            Doc.Add("RuleItem", RuleItem);
            Doc.Add("RuleAction", RuleAction);
            Doc.Add("ExcludeItem", ExcludeItem);
            Doc.Add("ExcludeDetails", ExcludeDetails);
            Doc.Add("table", table);
            return Doc;
        }
        public Mask(BsonDocument doc)
        {
            RuleItem = (doc.GetValue("RuleItem", "").ToString());
            RuleAction = (doc.GetValue("RuleAction", "").ToString());
            ExcludeItem = (doc.GetValue("ExcludeItem", "").ToString());
            ExcludeDetails = (doc.GetValue("ExcludeDetails", "").ToString());
            table = (doc.GetValue("table", "ALL").ToString());
        }
        public List<string> ExcludePorts(Unit LogicUnit, string divLine_name)
        {
            List<string> PortsToExclude = new List<string>();
            if (ExcludeItem != "Port") return PortsToExclude;
            if (RuleItem == "Unit")
            {
                //specific code: (no reflection)
                //if ((RuleAction=="IsShort") && (LogicUnit.IsShort())) PortsToExclude=new List<string>(ExcludeDetails.Split(','));

                //changed to refelction, see "compReportFormatter" as reference
                MethodInfo method = typeof(Unit).GetMethod(RuleAction);
                if (method == null) return PortsToExclude; //RuleAction does not exist
                object[] methodArgs = { };
                if ((bool)method.Invoke(LogicUnit, methodArgs)) PortsToExclude = new List<string>(ExcludeDetails.Split(','));
            }

            if (RuleItem == "dname")
            {
                if (RuleAction != divLine_name) return PortsToExclude; 
                PortsToExclude = new List<string>(ExcludeDetails.Split(','));
            }
            return PortsToExclude;
        }
        public List<string> ExcludeLanes(Unit LogicUnit,string divLine_name, string port)
        {
            List<string> LanessToExclude = new List<string>();
            if (ExcludeItem != "Lane") return LanessToExclude;
            if (RuleItem == "Port")
            {
                if ((RuleAction.StartsWith("EQ;") && (RuleAction.Trim()=="EQ;"+port.Trim().ToUpper()))) LanessToExclude=new List<string>(ExcludeDetails.Split(','));
            }
            if (RuleItem == "Unit")
            {
                //specific code: (no reflection)
                //if ((RuleAction=="IsShort") && (LogicUnit.IsShort())) LanessToExclude=new List<string>(ExcludeDetails.Split(','));

                //changed to refelction, see "compReportFormatter" as reference
                MethodInfo method = typeof(Unit).GetMethod(RuleAction);
                if (method == null) return LanessToExclude; //RuleAction does not exist
                object[] methodArgs = { };
                if ((bool)method.Invoke(LogicUnit, methodArgs)) LanessToExclude = new List<string>(ExcludeDetails.Split(','));
            }
            if (RuleItem == "dname")
            {
                if (RuleAction != divLine_name) return LanessToExclude;
                LanessToExclude = new List<string>(ExcludeDetails.Split(','));
            }
            return LanessToExclude;
        }
    }

   
}