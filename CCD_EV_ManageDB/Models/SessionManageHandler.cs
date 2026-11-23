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
using OfficeOpenXml;

namespace CCD_EV_ManageDB.Models
{
    public class SessionManageHandler
    {
        private static string SpecLimitsConflictsWarn { get { return System.Configuration.ConfigurationManager.AppSettings["SpecLimitsConflictsWarn"]; } }
        //
        Dictionary<string, List<string>> SessionStart_SMDictionary; // key: SessionStart  value: List of SM _id's that holds this SessionStart (Validation types)
        string SessionStart_SM_table;
        string SessionStart_SM_proj;
        string SessionStart_SM_phase;
        Dictionary<string, BsonDocument> SessionStartInfoDictionary; // key: SessionStart  value: Bson document with Info about results - Comment, Number of results (TMP/PRM_valid/PRM_not valid/ALL ?), Pass/Fail etc
        string SessionStartInfo_table;
        string SessionStartInfo_proj;
        string SessionStartInfo_phase;


        public SessionManageHandler()
        {
            SessionStart_SMDictionary = new Dictionary<string, List<string>>();
            SessionStartInfoDictionary = new Dictionary<string, BsonDocument>();
        }
        public bool loadSessionsManageConfToDB(string filePath)
        {

            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            
            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement dbElem = doc.Root;
                foreach (XElement ProtocolConf in dbElem.Elements("ProtocolConf"))
                {
                    if (!loadSessionManageProtocolConfToDB(ProtocolConf)) return false;
                }
            }
            catch (Exception e)
            {
                //
                return false;
            }

            return true;

        }
        public bool loadSessionManageProtocolConfToDB(XElement ProtocolConf)
        {
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            try
            {
                SessionsManageProtocolConf SMPconf = new SessionsManageProtocolConf();
                SMPconf.protocol = ProtocolConf.Element("protocol").Value.Trim();
                SMPconf.phase = ProtocolConf.Element("phase").Value.Trim();
                SMPconf.proj = ProtocolConf.Element("proj").Value.Trim();
                foreach (XElement table in ProtocolConf.Elements("table"))
                {
                    SMPconf.tables.Add(table.Value.Trim());
                }
                foreach (XElement fieldElem in ProtocolConf.Elements("field"))
                {
                    SMPconf.fields.Add(fieldElem.Value.Trim());
                    if (fieldElem.HasAttributes) SMPconf.fieldsAliases.Add(fieldElem.Attribute("alias").Value.Trim());
                    else SMPconf.fieldsAliases.Add(fieldElem.Value.Trim());
                }

                if (ProtocolConf.Element("corners") != null)
                {
                    SMPconf.corners = new List<string>(ProtocolConf.Element("corners").Value.Split(','));
                }
                SMPconf.ports = new List<string>(ProtocolConf.Element("ports").Value.Split(','));
                SMPconf.excelPorts = new List<string>(ProtocolConf.Element("excelPorts").Value.Split(','));
                SMPconf.lanes = new List<string>(ProtocolConf.Element("lanes").Value.Split(','));
                SMPconf.SRTemplate = ProtocolConf.Element("SRTemplate").Value.Trim();
                if (ProtocolConf.Element("separateCSRperTable") != null) SMPconf.separateCSRperTable = ProtocolConf.Element("separateCSRperTable").Value.ToUpper().Trim();
                if (ProtocolConf.Element("useNewCSR") != null) SMPconf.useNewCSR = ProtocolConf.Element("useNewCSR").Value.ToUpper().Trim();
                

                foreach (XElement table_div in ProtocolConf.Elements("table_div"))
                {
                    TableDiv tmpTableDiv = new TableDiv();
                    if (table_div.HasAttributes)
                    {
                        tmpTableDiv.name = (table_div.Attribute("tdname").Value.Trim());
                        tmpTableDiv.table_name = (table_div.Attribute("tname").Value.Trim());
                        if (table_div.Attribute("SRreportId") != null) tmpTableDiv.SRreportId = table_div.Attribute("SRreportId").Value.Trim();
                    }
                    foreach (XElement reportId in table_div.Elements("reportId"))
                    {
                        tmpTableDiv.reportIds.Add(reportId.Value);
                    }
                    foreach (XElement TableDivLine in table_div.Elements("table_div_line"))
                    {
                        TableDivLine tmpTableDivLine = new TableDivLine();
                        if (TableDivLine.HasAttributes)
                        {
                            tmpTableDivLine.name = (TableDivLine.Attribute("dname").Value.Trim());
                            tmpTableDivLine.trials = Convert.ToInt32(TableDivLine.Attribute("trials").Value.Trim());
                            tmpTableDivLine.numOfTests = Convert.ToInt32(TableDivLine.Attribute("DivNumOfTests").Value.Trim());
                        }
                        ///
                        tmpTableDivLine.setup_trials = new Dictionary<string, int>();
                        foreach (XElement setup_trials in TableDivLine.Elements("setup_trials"))
                        {
                            if (setup_trials.HasAttributes)
                            {
                                tmpTableDivLine.setup_trials.Add(setup_trials.Attribute("setup").Value.Trim(), Convert.ToInt32(setup_trials.Attribute("trials").Value.Trim()));
                            }
                        }
                        tmpTableDivLine.setup_numOfTests = new Dictionary<string, int>();
                        foreach (XElement setup_numOfTests in TableDivLine.Elements("setup_numOfTests"))
                        {
                            if (setup_numOfTests.HasAttributes)
                            {
                                tmpTableDivLine.setup_numOfTests.Add(setup_numOfTests.Attribute("setup").Value.Trim(), Convert.ToInt32(setup_numOfTests.Attribute("tests").Value.Trim()));
                            }
                        }
                        ///
                        foreach (XElement TableDivField in TableDivLine.Elements("table_div_field"))
                        {
                            TableDivField tmpTableDivField = new TableDivField();
                            if (TableDivField.HasAttributes)
                            {
                                tmpTableDivField.name = (TableDivField.Attribute("fname").Value.Trim());
                                tmpTableDivField.condition = (TableDivField.Attribute("condition").Value.Trim());
                                if (TableDivField.Attribute("isSpecSubSet") != null)
                                {
                                    if (TableDivField.Attribute("isSpecSubSet").Value.ToUpper() == "TRUE") tmpTableDivField.isSpecSubSet = true;
                                }
                            }
                            tmpTableDivField.values = new List<string>(TableDivField.Value.Split(','));
                            tmpTableDivLine.tableDivFields.Add(tmpTableDivField);
                        }
                        tmpTableDiv.TableDivLines.Add(tmpTableDivLine.name, tmpTableDivLine);
                    }

                    if (!SMPconf.table_divs.ContainsKey(tmpTableDiv.table_name)) SMPconf.table_divs.Add(tmpTableDiv.table_name, new Dictionary<string, TableDiv>());
                    SMPconf.table_divs[tmpTableDiv.table_name].Add(tmpTableDiv.name, tmpTableDiv);
                }

                foreach (XElement units in ProtocolConf.Elements("units"))
                {
                    foreach (XElement unit in units.Elements("unit"))
                    {
                        Unit tmpUnit = new Unit();
                        tmpUnit.skew = unit.Element("skew").Value.Trim();
                        tmpUnit.type = unit.Element("type").Value.Trim();
                        tmpUnit.setup = unit.Element("setup").Value.Trim();
                        tmpUnit.num = unit.Element("num").Value.Trim();
                        SMPconf.units.Add(tmpUnit);
                    }
                }

                foreach (XElement mask in ProtocolConf.Elements("mask"))
                {
                    Mask tmpMask = new Mask();
                    tmpMask.RuleItem = mask.Element("rule").Value.Split('=')[0].Trim();
                    tmpMask.RuleAction = mask.Element("rule").Value.Split('=')[1].Trim();
                    tmpMask.ExcludeItem = mask.Element("exclude").Value.Split('=')[0].Trim();
                    tmpMask.ExcludeDetails = mask.Element("exclude").Value.Split('=')[1].Trim();
                    if (mask.Element("table") != null) tmpMask.table = mask.Element("table").Value.Trim();
                    SMPconf.masks.Add(tmpMask);
                }

                //New: Dynamic fields
                SessionsManageProtocolConf SMPconfSaved = MDBSMconf.Read(SMPconf.protocol, SMPconf.proj, SMPconf.phase);
                string SMPconfSavedFields = "";
                foreach (string f in SMPconfSaved.fields) SMPconfSavedFields += f + ";";
                string SMPconfFields = "";
                foreach (string f in SMPconf.fields) SMPconfFields += f + ";";
                //if (SMPconfSavedFields != SMPconfFields) RebuildFieldsDataOfProtocolConf(SMPconf);  //need to be done in Background
                if (SMPconfSavedFields != SMPconfFields) SMPconf.RebuildFieldsData = "Y";

                MDBSMconf.SaveDoNotOverWriteTimestamp_ActiveFileNames(SMPconf);
            }
            catch (Exception e)
            {
                //
                return false;
            }

            return true;
        }

        public bool pocLoadExcelTemplateSMProtocolConfToDB(string excelFileName)
        {
            FileInfo newFile = new FileInfo(excelFileName);

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(2);
                int cnt = 1;
                string confXml = "";
                while (worksheet.Cells[cnt, 1].Value != null)
                {
                    confXml += worksheet.Cells[cnt, 1].Value.ToString();
                    cnt++;
                }
                XDocument doc = XDocument.Parse(confXml);
                //XDocument doc = XDocument.Parse(worksheet.Cells[1, 1].Value.ToString());
                XElement ProtocolConf = doc.Root;
                return  loadSessionManageProtocolConfToDB(ProtocolConf);
            }

        }

        public bool CheckSessionsManageConf(string filePath, out string Message)
        {
            Message = "";
            List<SessionsManageProtocolConf> SMPconfList = new List<SessionsManageProtocolConf>();
            Dictionary<string,string> TablesPerProtokol = new Dictionary<string,string>(); //Key: table_proj_phase Value: protocol

            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement dbElem = doc.Root;
                foreach (XElement ProtocolConf in dbElem.Elements("ProtocolConf"))
                {
                    SessionsManageProtocolConf SMPconf = new SessionsManageProtocolConf();
                    SMPconf.protocol = ProtocolConf.Element("protocol").Value.Trim();
                    SMPconf.phase = ProtocolConf.Element("phase").Value.Trim();
                    SMPconf.proj = ProtocolConf.Element("proj").Value.Trim();
                    foreach (XElement table in ProtocolConf.Elements("table"))
                    {
                        SMPconf.tables.Add(table.Value.Trim());
                    }

                    SMPconfList.Add(SMPconf);
                }
                foreach ( SessionsManageProtocolConf TmpConf in SMPconfList)
                {
                    foreach (string table in TmpConf.tables)
                    {
                        string key = table + "_" + TmpConf.proj + "_" + TmpConf.phase;
                        if (!TablesPerProtokol.ContainsKey(key)) TablesPerProtokol.Add(key,TmpConf.protocol);
                        else 
                        {
                            Message = "Protocols:" + TmpConf.protocol + " and " + TablesPerProtokol[key] + " both contains - " + key;
                            return false;
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                //
                Message = e.Message;
                return false;
            }

            return true;

        }


        public SessionsManageDisplay GetSessionsManageDisplay(string protocol,string proj, string phase,string _FilterSessions )
        {
            MDBSessionsManage MDBS = new MDBSessionsManage();
            MDBSessionsManage MDBS_TMP_DB = new MDBSessionsManage("DB_TMP");
            SessionsManageDisplay SMD = new SessionsManageDisplay(protocol,proj, phase);
            SMD.FilterSessionsView = _FilterSessions;
            MDBSessionsManageConf MDBSconf = new MDBSessionsManageConf();
            MDBExecutions MDBExecution = new MDBExecutions("DB_TMP");

            SMD.Conf = MDBSconf.Read(protocol, proj, phase);
            //if no conf was defined for this protocol-project-phase
            if (SMD.Conf.tables.Count()==0) return SMD;


            //New 06 Feb 2020 lock mechanism - return not updated values
            int MinutesfromLastLock = 10;
            if (SMD.Conf.locktimestamp.ToString().Length > 12)
            {

                int locktsYear = Convert.ToInt32(SMD.Conf.locktimestamp.ToString().Substring(0, 4));
                int locktsMonth = Convert.ToInt32(SMD.Conf.locktimestamp.ToString().Substring(4, 2));
                int locktsDay = Convert.ToInt32(SMD.Conf.locktimestamp.ToString().Substring(6, 2));
                int locktsHour = Convert.ToInt32(SMD.Conf.locktimestamp.ToString().Substring(8, 2));
                int locktsMin = Convert.ToInt32(SMD.Conf.locktimestamp.ToString().Substring(10, 2));
                int locktsSec = Convert.ToInt32(SMD.Conf.locktimestamp.ToString().Substring(12, 2));
                DateTime LockTSdate = new DateTime(locktsYear, locktsMonth, locktsDay,locktsHour,locktsMin, locktsSec);
                MinutesfromLastLock = (int)DateTime.Now.Subtract(LockTSdate).TotalMinutes;
            }

            if ((SMD.Conf.locktimestamp > 0) && (MinutesfromLastLock < 10))
            {
                foreach (string table in SMD.Conf.tables)
                {
                    SessionsManageOfTable SMT = new SessionsManageOfTable(table);
                    //Load all exist SessionManage records
                    SMT.Sessions = MDBS.ReadTableProjectPhaseSessions(table, proj, phase);
                    BuildSessionStartInfoDictionary(table, proj, phase);
                    SMT.SessionStartInfoDictionary = new Dictionary<string, BsonDocument>(this.SessionStartInfoDictionary);
                    SMD.SessionsManageOfTables.Add(SMT);
                }
                return SMD;
            }

            SMD.Conf.locktimestamp = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmss"));
            MDBSconf.Save(SMD.Conf);
            //

            int tsYear = Convert.ToInt32(SMD.Conf.timestamp.ToString().Substring(0, 4));
            int tsMonth = Convert.ToInt32(SMD.Conf.timestamp.ToString().Substring(4, 2));
            int tsDay = Convert.ToInt32(SMD.Conf.timestamp.ToString().Substring(6, 2));
            DateTime TSdate = new DateTime(tsYear, tsMonth, tsDay);
            

            foreach (string table in SMD.Conf.tables)
            {
                SessionsManageOfTable SMT = new SessionsManageOfTable(table);
                //Load all exist SessionManage records
                SMT.Sessions = MDBS.ReadTableProjectPhaseSessions(table, proj, phase);
                List<string> alreadyExistSessions = new List<string>();
                foreach (SessionsManage existSM in SMT.Sessions)
                {
                    foreach (string existSession in existSM.Sessions) if (!alreadyExistSessions.Contains(existSession)) alreadyExistSessions.Add(existSession);
                }

                //Search all sessions in results DB newer than timestamp
                MDBStationResults MDB_Staion = new MDBStationResults("DB_TMP", table);
                //special fix for HBR A0+A1 in same cycle
                string phase_query = ";EQ;Phase;" + phase;
                if ((proj == "HBR") && (phase == "A0")) phase_query = ";In;Phase;A0,A1";
                MongoCursor mCur = MDB_Staion.MDBFind("GT;SessionStart;" + SMD.Conf.timestamp.ToString() + ";EQ;ProjectName;" + proj + phase_query, "AND", "DES", "TestEnd");
                HashSet<string> Sessions = new HashSet<string>();
                HashSet<string> ESessions = new HashSet<string>();
                
                 foreach (BsonDocument Doc in mCur)
                {
                    string session = Doc.GetElement("SessionStart").Value.ToString();
                    if ((!alreadyExistSessions.Contains(session)) && (!Sessions.Contains(session))) Sessions.Add(session);
                }

                ////////// Test New  ///////////////////
                //Search for all sessions in Execution table since from timestamp, that are not in DB results yet:
                 MongoCursor eCur = MDBExecution.MDBFind("Matches;_id;/^" + table + "/;GT;SessionStart;" + (SMD.Conf.timestamp - 1000000).ToString() + ";EQ;ProjectName;" + proj + phase_query + ";EQ;Status;Start", "AND", "DES", "TestEnd");

                foreach (BsonDocument Doc in eCur)
                {
                    string session = Doc.GetElement("SessionStart").Value.ToString();
                    if ((!Sessions.Contains(session)) && (!alreadyExistSessions.Contains(session))) ESessions.Add(session);
                }
                MongoCursor eCur2 = MDBExecution.MDBFind("Matches;_id;/^" + table + "/;GT;TestStart;" + (SMD.Conf.timestamp - 1000000).ToString() + ";EQ;ProjectName;" + proj + phase_query + ";EQ;Status;Start", "AND", "DES", "TestEnd");
                foreach (BsonDocument Doc in eCur2)
                {
                    string session = Doc.GetElement("SessionStart").Value.ToString();
                    if ((!Sessions.Contains(session)) && (!alreadyExistSessions.Contains(session)) && !ESessions.Contains(session)) ESessions.Add(session);
                }
                //Read last record from TMP SessionsManage
                List <string> TmpSessions = MDBS_TMP_DB.ReadTMPTableProjectPhaseSessions(table, proj, phase);
                //check if there are sessions in DB results, if yes - add them to "Sessions" and remove 
                //from TMP, if no check period threshold and keep it in TMP record
                foreach (string tSession in TmpSessions)
                {
                    if (MDB_Staion.MDBFindOne("EQ;SessionStart;" + tSession, "AND", "DES", "TestEnd") != null)
                    {
                        if ((!Sessions.Contains(tSession)) && (!alreadyExistSessions.Contains(tSession))) Sessions.Add(tSession);
                    }
                    else
                    {
                        DateTime tSessionDate = (new DateTime(Convert.ToInt32(tSession.Substring(0, 4)), Convert.ToInt32(tSession.Substring(4, 2)), Convert.ToInt32(tSession.Substring(6, 2))));
                        if (((TSdate.Subtract(tSessionDate)).Days < 5) && (!Sessions.Contains(tSession)) && (!ESessions.Contains(tSession)))//check threshold, and verify it is not saved already
                            ESessions.Add(tSession);
                    }
                }

                MDBS_TMP_DB.SaveTMPRecExecutionSessionsWithNoResults(table, proj, phase, ESessions.ToList<string>());
                ////////// End Test:  ///////////////////


                //Gather new sessions of same board
                //Merge into exist SessionManage records and create SessionManage record for new boards
                foreach (string session in Sessions)
                {
                    //New: check if need to ignore/Tmp_not_Valid this session
                    BsonDocument ExecutionRec = MDBExecution.MDBFindOne("Matches;_id;/^" + table + "/;EQ;SessionStart;" + session + ";EQ;Status;Start", "AND", "DES", "TestEnd");
                    //New: if need to ignore  this session
                    if ((ExecutionRec != null) && (ExecutionRec.GetValue("Debug_Session","")=="True")) continue;

                    SessionsManage SM = new SessionsManage();
                    //SM = SM.GetSessionsManageFromDBResult(MDB_Staion.MDBFindOne("EQ;SessionStart;" + session, "AND", "DES", "TestEnd"), table, SMD.Conf.fields);
                    SM = SM.GetSessionsManageFromDBResult(MDB_Staion.MDBFindOne("EQ;SessionStart;" + session, "AND", "DES", "TestEnd"), table, SMD.Conf.fields, SMD.Conf);
                    //New: if need to classify "Tmp_not_Valid" this session
                    //if ((ExecutionRec != null) && (ExecutionRec.GetValue("Debug_Session", "") == "True")) SM.validation = General.SessionValidationStatus.TMP_not_valid;
                    AddNewSessionToList(SMT.Sessions, SM);
                }
                foreach (SessionsManage Session in SMT.Sessions) MDBS.Save(Session);
                //Build BuildSessionStartInfoDictionary
                BuildSessionStartInfoDictionary(table, proj, phase);
                SMT.SessionStartInfoDictionary = new Dictionary<string, BsonDocument>(this.SessionStartInfoDictionary);
                //Add SpecLimitsConflictsFound
                if (SpecLimitsConflictsWarn == "Y")
                {
                    List<BsonValue> SessionsConflicts = (new MDBSpecLimitsConflicts()).ReadTableConflictsSessionsList(table, proj, phase);
                    
                    foreach (BsonValue sessionConflict in SessionsConflicts)
                    {
                        if ((SMT.SessionStartInfoDictionary.ContainsKey(sessionConflict.ToString())) && (IsSessionStartValidStatus(sessionConflict.ToString()))) SMT.SpecLimitsConflictSessions.Add(sessionConflict.ToString());
                    }
                }
                SMD.SessionsManageOfTables.Add(SMT);
            }
            SMD.Conf.timestamp = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmss"));
            SMD.Conf.locktimestamp = Convert.ToInt64(0);
            MDBSconf.Save(SMD.Conf);
            return SMD;
        }


        //This method is for new sessions
        void AddNewSessionToList(List<SessionsManage> SessionsList, SessionsManage NewSession)
        {
            int ind = 0;
            bool found = false;
            bool SearchLogicUnit = true;
            if (!NewSession.logicUnit.EndsWith("??")) SearchLogicUnit = false;
            foreach (SessionsManage Session in SessionsList)
            {
                if ((SearchLogicUnit) && (!Session.logicUnit.Contains("??")))
                {
                    if ((Session.board_id == NewSession.board_id) && (Session.unit_id == NewSession.unit_id) && (Session.logicUnit.StartsWith(NewSession.logicUnit.Substring(0, NewSession.logicUnit.Length - 2))))
                    {
                        NewSession.logicUnit = Session.logicUnit;
                        SearchLogicUnit = false;
                    }
                }

                if (Session.ImpEquals(NewSession))
                {
                    Session.Sessions.AddRange(NewSession.Sessions);
                    //New: merge Data 
                    foreach (string f in Session.FieldsList)
                    {
                        string SessionField = Session.Data.GetValue(f, "").ToString();
                        string NewSessionField = NewSession.Data.GetValue(f, "").ToString();
                        if (((!string.IsNullOrEmpty(NewSessionField)) && (!SessionField.Contains(NewSessionField))))
                        {
                            SessionField += ";" + NewSessionField;
                            Session.Data.Set(f, SessionField);
                        }
                    }
                    //Check real merge:
                    //Session.Sessions = Session.Sessions.Union(NewSession.Sessions).ToList<string>();
                    SessionsList[ind] = Session;
                    found = true;
                    break;
                }
                ind++;
            }
            if (!found)
            {
                SessionsList.Add(NewSession);
            }
        }

        public void UnifyNVMSessions(SessionsManageDisplay SMD)
        {
            foreach (SessionsManageOfTable SMT in SMD.SessionsManageOfTables)
            {
                List<SessionsManage> SessionsListNVMSplitted = new List<SessionsManage>(SMT.Sessions);
                SMT.Sessions.Clear();
                foreach (SessionsManage SM in SessionsListNVMSplitted) AddNewSessionToListUnifyNVM(SMT.Sessions, SM);
                //SMD.SessionsManageOfTables.Add(SMT);
            }
        }
        void AddNewSessionToListUnifyNVM(List<SessionsManage> SessionsList, SessionsManage NewSession)
        {
            int ind = 0;
            bool found = false;
            bool SearchLogicUnit = true;
            if (!NewSession.logicUnit.EndsWith("??")) SearchLogicUnit = false;
            foreach (SessionsManage Session in SessionsList)
            {
                if ((SearchLogicUnit) && (!Session.logicUnit.Contains("??")))
                {
                    if ((Session.board_id == NewSession.board_id) && (Session.unit_id == NewSession.unit_id) && (Session.logicUnit.StartsWith(NewSession.logicUnit.Substring(0, NewSession.logicUnit.Length - 2))))
                    {
                        NewSession.logicUnit = Session.logicUnit;
                        SearchLogicUnit = false;
                    }
                }

                if (Session.ImpEqualsExceptNVM(NewSession))
                {
                    Session.Sessions.AddRange(NewSession.Sessions);
                    //New: merge Data 
                    if (!Session.NVM.Contains(NewSession.NVM)) Session.NVM += ";" + NewSession.NVM;
                    foreach (string f in Session.FieldsList)
                    {
                        string SessionField = Session.Data.GetValue(f, "").ToString();
                        string NewSessionField = NewSession.Data.GetValue(f, "").ToString();
                        if (((!string.IsNullOrEmpty(NewSessionField)) && (!SessionField.Contains(NewSessionField))))
                        {
                            SessionField += ";" + NewSessionField;
                            Session.Data.Set(f, SessionField);
                        }
                    }
                    //Check real merge:
                    //Session.Sessions = Session.Sessions.Union(NewSession.Sessions).ToList<string>();
                    SessionsList[ind] = Session;
                    found = true;
                    break;
                }
                ind++;
            }
            if (!found)
            {
                SessionsList.Add(NewSession);
            }
        }

        public List<SessionsManage> getEditSessionsList(string _id)
        {
            MDBSessionsManage MDBS = new MDBSessionsManage();
            List<SessionsManage> SMList = MDBS.ReadSameBoardSessions(_id);
            foreach (var value in Enum.GetValues(typeof(General.SessionValidationStatus)))
            {
                bool found = false;
                foreach (SessionsManage SM in SMList) if (SM.validation.Equals((General.SessionValidationStatus)value) == true) found = true;
                if (!found)
                {
                    SessionsManage SMTemp = new SessionsManage(SMList.First().toBsonDocument());
                    SMTemp._id = ObjectId.GenerateNewId();
                    SMTemp.Sessions.Clear();
                    SMTemp.validation = (General.SessionValidationStatus)value;
                    SMList.Add(SMTemp);
                }
            }
            return SMList;
        }

        public Dictionary<string, SessionsManage> getEditExecSessionsDictionary(string _id)
        {
            Dictionary<string, SessionsManage> SessionsValidationKeys = new Dictionary<string, SessionsManage>();
            List<SessionsManage> SMList = this.getEditSessionsList(_id);
            foreach (SessionsManage SM in SMList) SessionsValidationKeys.Add(SM.validation.ToString(), SM);
            return SessionsValidationKeys;
        }

        public void LoadToPRMSessionManageUpdate(string sessionsStr,string table)
        {
            MDBSessionsManage MDBS = new MDBSessionsManage();
            MDBStationResults MDBStationResultsObj = new MDBStationResults("DB_PRM", table);
            List<string> SessionsList = new List<string>(sessionsStr.Split(','));
            SessionsManage SMTmp_Valid = new SessionsManage();
            BsonDocument Result = MDBStationResultsObj.MDBFindOne("EQ;SessionStart;" + SessionsList[0],"AND", "ASC", "_id");
            
            foreach (string session in SessionsList)
            {
                bool inPRM_valid = false;
                List<SessionsManage> SessionsManageList1 = MDBS.ReadTableProjectPhaseSessions(table, Result.GetValue("ProjectName", "").ToString(), Result.GetValue("Phase", "").ToString());
                List<SessionsManage> SessionsManageList2 = new List<SessionsManage>();
                foreach (SessionsManage SM in SessionsManageList1)
                {
                    if ((SM.validation == General.SessionValidationStatus.TMP_valid) && (SM.Sessions.Contains(session)))
                    {
                        SMTmp_Valid = SM;
                        SessionsManageList2 = MDBS.ReadSameBoardSessions(SM._id.ToString());
                        SM.Sessions.Remove(session);
                        if (SM.Sessions.Count>0) MDBS.Save(SM);
                        else MDBS.Delete(SM._id);
                    }
                }
                foreach (SessionsManage SM in SessionsManageList2)
                {
                    if ((SM.validation == General.SessionValidationStatus.PRM_valid) )
                    {
                        if (!SM.Sessions.Contains(session))
                        {
                            SM.Sessions.Add(session);
                        }
                        MDBS.Save(SM);
                        inPRM_valid = true;
                    }
                }
                //old version: if (!inPRM_valid)
                //fixed to avoid empty SM records
                if ((!inPRM_valid) && (!string.IsNullOrEmpty(SMTmp_Valid.table)))
                {
                    SessionsManage SMTemp = new SessionsManage(SMTmp_Valid.toBsonDocument());
                    SMTemp._id = ObjectId.GenerateNewId();
                    SMTemp.Sessions.Clear();
                    SMTemp.Sessions.Add(session);
                    SMTemp.validation = General.SessionValidationStatus.PRM_valid;
                    MDBS.Save(SMTemp);
                }
                
            }
        }

        public void IndicatorsIncExcSessionManageUpdate(string table, string query, HashSet<string> SessionsEffected, string Include, string comment)
        {   
            //for each session - if number of results equals to all results - remove session from PRM_valid, 
            //if less - split 
            //OR build a set of session, than for each session, search for results with INC=Y in order to decide if split or not
            MDBStationResults MDBStationResultsObj = new MDBStationResults("DB_PRM", table);
            string fsession = SessionsEffected.First();
            BsonDocument Result = MDBStationResultsObj.MDBFindOne("EQ;SessionStart;" + fsession, "AND", "ASC", "_id");
            this.BuildSessionStart_SMDictionary(table, Result.GetValue("ProjectName","").ToString(), Result.GetValue("Phase","").ToString());

            //default values for handling Exclude case(Include=N)
            string IndicatorQuery = "Y";
            General.SessionValidationStatus validationToAdd = General.SessionValidationStatus.PRM_not_valid;
            General.SessionValidationStatus validationToRemove = General.SessionValidationStatus.PRM_valid;

            if (Include == "Y")
            {
                IndicatorQuery = "N";
                validationToAdd = General.SessionValidationStatus.PRM_valid;
                validationToRemove = General.SessionValidationStatus.PRM_not_valid;
            }
            foreach (string session in SessionsEffected)
            {
                AddSessionToSM(session, validationToAdd,comment); 
                MongoCursor cur = MDBStationResultsObj.MDBFind("EQ;SessionStart;" + session + ";EQ;Indicator;" + IndicatorQuery, "AND", "ASC", "_id");
                if (cur.Count() == 0) 
                {
                    RemoveSessionFromSM(session, validationToRemove);
                }
             }


        }

        public void BuildSessionStart_SMDictionary(string table, string proj, string phase)
        {
            if ((SessionStart_SM_table == table) && (SessionStart_SM_proj == proj) && ( SessionStart_SM_phase ==phase) && (SessionStart_SMDictionary.Count > 0)) return;
            SessionStart_SMDictionary.Clear();
            SessionStart_SM_table = table;
            SessionStart_SM_proj = proj;
            SessionStart_SM_phase = phase;
            MDBSessionsManage MDBS = new MDBSessionsManage();
            foreach (SessionsManage SM in MDBS.ReadTableProjectPhaseSessions(table, proj, phase))
            {
                foreach (string session in SM.Sessions)
                {
                    if (this.SessionStart_SMDictionary.ContainsKey(session)) SessionStart_SMDictionary[session].Add(SM._id.ToString());
                    else SessionStart_SMDictionary.Add(session,SM._id.ToString().Split(' ').ToList<string>());
                }
            }

        }

        public void RemoveSessionFromSM(string session, General.SessionValidationStatus status)
        {
            //assuming BuildSessionStartDictionary was called!!
            if (!SessionStart_SMDictionary.ContainsKey(session)) return;
            MDBSessionsManage MDBS = new MDBSessionsManage();
            foreach (string id in SessionStart_SMDictionary[session])
            {
                SessionsManage SM = MDBS.Read(id);
                if (SM.validation == status) SM.Sessions.Remove(session);
                if (SM.Sessions.Count == 0) MDBS.Delete(id);
                else MDBS.Save(SM);
            }
        }

        public void AddSessionToSM(string session, General.SessionValidationStatus status, string comment)
        {
            //assuming BuildSessionStartDictionary was called!!
            if (!SessionStart_SMDictionary.ContainsKey(session)) return;
            MDBSessionsManage MDBS = new MDBSessionsManage();
            bool found = false;
            List<SessionsManage> SMList = MDBS.ReadSameBoardSessions(SessionStart_SMDictionary[session].First());
            foreach (SessionsManage SM in SMList)
            {
                if (SM.validation == status)
                {
                    if (!SM.Sessions.Contains(session))
                    {
                        SM.Sessions.Add(session);
                        MDBS.Save(SM);
                    }
                    found = true;
                }              
            }
            if (!found)
            {
                SessionsManage SMTemp = new SessionsManage(MDBS.Read(SessionStart_SMDictionary[session].First()).toBsonDocument());
                SMTemp._id = ObjectId.GenerateNewId();
                SMTemp.Sessions.Clear();
                SMTemp.Sessions.Add(session);
                SMTemp.validation = status;
                SMTemp.comment = comment;
                MDBS.Save(SMTemp);
                SessionStart_SMDictionary[session].Add(SMTemp._id.ToString());
            }
        }

        public void BuildSessionStartInfoDictionary(string table, string proj, string phase)
        {
            if ((SessionStartInfo_table == table) && (SessionStartInfo_proj == proj) && (SessionStartInfo_phase == phase) && (SessionStartInfoDictionary.Count > 0)) return;
            SessionStartInfoDictionary.Clear();
            MDBStationResults MDBStationResultsTMPObj = new MDBStationResults("DB_TMP", table);
            MDBStationResults MDBStationResultsPRMObj = new MDBStationResults("DB_PRM", table);
            SessionStartInfo_table = table;
            SessionStartInfo_proj = proj;
            SessionStartInfo_phase = phase;
            this.BuildSessionStart_SMDictionary(table, proj, phase);
            foreach (string SessionStart in SessionStart_SMDictionary.Keys)
            {
                //TODO: change "BuildSessionStartInfoDictionary" to use  BuildSessionInfoDoc()

                BsonDocument result = new BsonDocument();
                //query DB for needed info and build Bson document with Info about results - Comment, Number of results (TMP/PRM_valid/PRM_not valid/ALL), Pass/Fail 
                MongoCursor curTmp = MDBStationResultsTMPObj.MDBFind("EQ;SessionStart;" + SessionStart , "AND", "ASC", "_id");
                if (curTmp.Count() > 0) result = MDBStationResultsTMPObj.MDBFindOne("EQ;SessionStart;" + SessionStart, "AND", "ASC", "_id");
                MongoCursor curPrmInc = MDBStationResultsPRMObj.MDBFind("EQ;SessionStart;" + SessionStart + ";EQ;Indicator;Y", "AND", "ASC", "_id");
                if ((String.IsNullOrEmpty(result.GetValue("SessionStart","").ToString())) && (curPrmInc.Count() > 0)) 
                        result = MDBStationResultsPRMObj.MDBFindOne("EQ;SessionStart;" + SessionStart + ";EQ;Indicator;Y", "AND", "ASC", "_id");
                MongoCursor curPrmExc = MDBStationResultsPRMObj.MDBFind("EQ;SessionStart;" + SessionStart + ";EQ;Indicator;N", "AND", "ASC", "_id");
                if ((String.IsNullOrEmpty(result.GetValue("SessionStart", "").ToString())) && (curPrmExc.Count() > 0)) 
                        result = MDBStationResultsPRMObj.MDBFindOne("EQ;SessionStart;" + SessionStart + ";EQ;Indicator;N", "AND", "ASC", "_id");
                BsonDocument InfoDoc = new BsonDocument();
                if (result == null) continue;
                InfoDoc.Set("UserComment", result.GetValue("UserComment", ""));
                InfoDoc.Set("TMP results", curTmp.Count());
                InfoDoc.Set("PRM results inc", curPrmInc.Count());
                InfoDoc.Set("PRM results exc", curPrmExc.Count());
                MongoCursor curTmpSta = MDBStationResultsTMPObj.MDBFind("EQ;SessionStart;" + SessionStart + ";Matches;TestStatus;/^fail/i", "AND", "ASC", "_id");
                if (curTmpSta.Count() > 0) InfoDoc.Set("TMP results status", "Fail");
                else InfoDoc.Set("TMP results status", "Pass");
                MongoCursor curPrmIncSta = MDBStationResultsPRMObj.MDBFind("EQ;SessionStart;" + SessionStart + ";Matches;TestStatus;/^fail/i;EQ;Indicator;Y", "AND", "ASC", "_id");
                if (curPrmIncSta.Count() > 0) InfoDoc.Set("PRM results inc status", "Fail");
                else InfoDoc.Set("PRM results inc status", "Pass");
                MongoCursor curPrmExcSta = MDBStationResultsPRMObj.MDBFind("EQ;SessionStart;" + SessionStart + ";Matches;TestStatus;/^fail/i;EQ;Indicator;N", "AND", "ASC", "_id");
                if (curPrmExcSta.Count() > 0) InfoDoc.Set("PRM results exc status", "Fail");
                else InfoDoc.Set("PRM results exc status", "Pass");
                
                SessionStartInfoDictionary.Add(SessionStart, InfoDoc);
            }

        }

        BsonDocument BuildSessionInfoDoc(string table, string SessionStart)
        {
            //TODO: call BuildSessionStartInfoDictionary() OR BuildSessionInfoDoc()  - change "BuildSessionStartInfoDictionary" to use  BuildSessionInfoDoc()

            MDBStationResults MDBStationResultsTMPObj = new MDBStationResults("DB_TMP", table);
            MDBStationResults MDBStationResultsPRMObj = new MDBStationResults("DB_PRM", table);
            BsonDocument result = new BsonDocument();
            //query DB for needed info and build Bson document with Info about results - Comment, Number of results (TMP/PRM_valid/PRM_not valid/ALL), Pass/Fail 
            MongoCursor curTmp = MDBStationResultsTMPObj.MDBFind("EQ;SessionStart;" + SessionStart, "AND", "ASC", "_id");
            if (curTmp.Count() > 0) result = MDBStationResultsTMPObj.MDBFindOne("EQ;SessionStart;" + SessionStart, "AND", "ASC", "_id");
            MongoCursor curPrmInc = MDBStationResultsPRMObj.MDBFind("EQ;SessionStart;" + SessionStart + ";EQ;Indicator;Y", "AND", "ASC", "_id");
            if ((String.IsNullOrEmpty(result.GetValue("SessionStart", "").ToString())) && (curPrmInc.Count() > 0))
                result = MDBStationResultsPRMObj.MDBFindOne("EQ;SessionStart;" + SessionStart + ";EQ;Indicator;Y", "AND", "ASC", "_id");
            MongoCursor curPrmExc = MDBStationResultsPRMObj.MDBFind("EQ;SessionStart;" + SessionStart + ";EQ;Indicator;N", "AND", "ASC", "_id");
            if ((String.IsNullOrEmpty(result.GetValue("SessionStart", "").ToString())) && (curPrmExc.Count() > 0))
                result = MDBStationResultsPRMObj.MDBFindOne("EQ;SessionStart;" + SessionStart + ";EQ;Indicator;N", "AND", "ASC", "_id");
            BsonDocument InfoDoc = new BsonDocument();
            if (result == null) return InfoDoc;
            InfoDoc.Set("UserComment", result.GetValue("UserComment", ""));
            InfoDoc.Set("TMP results", curTmp.Count());
            InfoDoc.Set("PRM results inc", curPrmInc.Count());
            InfoDoc.Set("PRM results exc", curPrmExc.Count());
            MongoCursor curTmpSta = MDBStationResultsTMPObj.MDBFind("EQ;SessionStart;" + SessionStart + ";Matches;TestStatus;/^fail/i", "AND", "ASC", "_id");
            if (curTmpSta.Count() > 0) InfoDoc.Set("TMP results status", "Fail");
            else InfoDoc.Set("TMP results status", "Pass");
            MongoCursor curPrmIncSta = MDBStationResultsPRMObj.MDBFind("EQ;SessionStart;" + SessionStart + ";Matches;TestStatus;/^fail/i;EQ;Indicator;Y", "AND", "ASC", "_id");
            if (curPrmIncSta.Count() > 0) InfoDoc.Set("PRM results inc status", "Fail");
            else InfoDoc.Set("PRM results inc status", "Pass");
            MongoCursor curPrmExcSta = MDBStationResultsPRMObj.MDBFind("EQ;SessionStart;" + SessionStart + ";Matches;TestStatus;/^fail/i;EQ;Indicator;N", "AND", "ASC", "_id");
            if (curPrmExcSta.Count() > 0) InfoDoc.Set("PRM results exc status", "Fail");
            else InfoDoc.Set("PRM results exc status", "Pass");
            return InfoDoc;
        }


        public void RebuildFieldsDataOfProtocolConf(SessionsManageProtocolConf Conf)
        {
            //string protocol, string proj, string phase
            MDBSessionsManage MDBS = new MDBSessionsManage();

            foreach (string table in Conf.tables)
            {
                SessionsManageOfTable SMT = new SessionsManageOfTable(table);
                MDBStationResults MDB_Station = new MDBStationResults("DB_TMP", table);
                MDBStationResults MDB_StationTMP = new MDBStationResults("DB_TMP", table);
                MDBStationResults MDB_StationPRM = new MDBStationResults("DB_PRM", table);
                //Load all exist SessionManage records
                SMT.Sessions = MDBS.ReadTableProjectPhaseSessions(table, Conf.proj, Conf.phase);
                foreach (SessionsManage SM in SMT.Sessions)
                {
                    if (SM.validation.ToString().Contains("PRM")) MDB_Station = MDB_StationPRM;
                    else MDB_Station = MDB_StationTMP;
                    SM.FieldsList = new List<string>(Conf.fields);
                    SM.Data = new BsonDocument();
                    foreach (string f in SM.FieldsList)
                    {
                        foreach (string session in SM.Sessions)
                        {
                            BsonDocument item = MDB_Station.MDBFindOne("EQ;SessionStart;" + session, "AND", "DES", "TestEnd");
                            if (item == null) continue;
                            string newField = item.GetValue(f, "").ToString();
                            string existField = SM.Data.GetValue(f, "").ToString();
                            if ((!string.IsNullOrEmpty(newField)) && (!existField.Contains(newField))) 
                            {
                                if (!string.IsNullOrEmpty(existField)) existField += " ; ";
                                existField +=  newField;
                                SM.Data.Set(f, existField);
                            }
                        }
                        MDBS.Save(SM);
                    }
                }
            }
            MDBSessionsManageConf MDBSessionsManageConfObj = new MDBSessionsManageConf();
            Conf.RebuildFieldsData = "N";
            MDBSessionsManageConfObj.SaveDoNotOverWriteTimestamp_ActiveFileNames(Conf);
            
        }



        public List<string> GetSessionsManageConfProtocols(string filePath)
        {
            List<string> SessionsManageConfProtocols = new List<string>();

            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement dbElem = doc.Root;
                foreach (XElement ProtocolConf in dbElem.Elements("ProtocolConf"))
                {
                    SessionsManageConfProtocols.Add(ProtocolConf.Element("protocol").Value.Trim());
                }

            }
            catch (Exception e)
            {
                //TODO??
            }

            return SessionsManageConfProtocols;

        }

        public bool CSRConfigCopyConfig(string protocol, string proj, string phase, string t_proj, string t_phase, out string message)
        {
            string ServerDir = System.Configuration.ConfigurationManager.AppSettings["ServerDir"];
            bool savedInEVDATA = false;
            bool confInDB = false;

            string FileName = "";
            message = "";
            CycleStatusHandler SCRHandler = new CycleStatusHandler();
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();

            SessionsManageDisplay SMDispTarget = new SessionManageHandler().GetSessionsManageDisplay(protocol, t_proj, t_phase, "ALL");
            if (SMDispTarget.Conf.tables.Count != 0)
            {
                confInDB = true;
            }

            SessionsManageDisplay SMDisp = GetSessionsManageDisplay(protocol, proj, phase, "ALL");

            List<string> currTemplates = new List<string>();
            if (SMDisp.Conf.separateCSRperTable != "Y")
            {
                SCRHandler.GetCSRExcelTemplates(protocol, proj, phase, "", out FileName);
                currTemplates.Add(SMDisp.Conf.getActiveFileName(FileName));
            }
            else
            {
                foreach (string table in SMDisp.Conf.tables)
                {
                    SCRHandler.GetCSRExcelTemplates(protocol, proj, phase, table, out FileName);
                    currTemplates.Add(SMDisp.Conf.getActiveFileName(FileName));
                }
            }
            
            foreach (string ExcelFile in currTemplates)
            {
                string t_ExcelFile = ExcelFile.Replace("_" + proj + "_", "_" + t_proj + "_").Replace("_" + phase + ".", "_" + t_phase + ".");
                t_ExcelFile = t_ExcelFile.Replace("_" + phase + "_", "_" + t_phase + "_");
                savedInEVDATA = CCDUtilities.CopyFileSaveBackup(Path.Combine(ServerDir, "EVData", ExcelFile), Path.Combine(ServerDir, "EVData"), t_ExcelFile, out message);
                if (!savedInEVDATA) message += "Error while saving file:" + t_ExcelFile + " - " + message + ";";
            }

            if (!confInDB)
            {
                SCRHandler.GetCSRXMLConfigFiles(protocol, proj, phase, "", out FileName);
                FileName = SMDisp.Conf.getActiveFileName(FileName);

                string t_XMLFile = FileName.Replace("_" + proj + "_", "_" + t_proj + "_").Replace("_" + phase + ".", "_" + t_phase + ".");
                t_XMLFile = t_XMLFile.Replace("_" + phase + "_", "_" + t_phase + "_");

                savedInEVDATA = CCDUtilities.CopyFileSaveBackup(Path.Combine(ServerDir, "EVData", FileName), Path.Combine(ServerDir, "EVData"), t_XMLFile, out message);
                if (savedInEVDATA)
                {
                    List<string> XMLcontent = new List<string>(System.IO.File.ReadAllLines(Path.Combine(ServerDir, "EVData", t_XMLFile)));
                    List<string> XMLcontentNew = new List<string>();
                    foreach (string line in XMLcontent)
                    {
                        if (line.Contains("<proj>")) XMLcontentNew.Add(line.Replace(proj, t_proj));
                        if (line.Contains("<phase>")) XMLcontentNew.Add(line.Replace(phase, t_phase));
                        if ((!line.Contains("<proj>")) && (!line.Contains("<phase>"))) XMLcontentNew.Add(line);
                    }
                    System.IO.File.WriteAllLines(Path.Combine(ServerDir, "EVData", t_XMLFile), XMLcontentNew);
                    if (!loadSessionsManageConfToDB(Path.Combine(ServerDir, "EVData", t_XMLFile)))
                    {
                        message += "Error: cannot load XML conf to DB";
                        return false;
                    }
                }
            }

            //update active files
            currTemplates.Clear();
            SessionsManageProtocolConf SMPConf = MDBSMconf.Read(protocol, t_proj, t_phase);
            if (SMPConf.separateCSRperTable != "Y")
            {
                currTemplates = SCRHandler.GetCSRExcelTemplates(protocol, t_proj, t_phase, "", out FileName);
                SMPConf.updateActiveFileName(FileName, currTemplates[0]);
            }
            else
            {
                foreach (string table in SMDisp.Conf.tables)
                {
                    currTemplates = SCRHandler.GetCSRExcelTemplates(protocol, t_proj, t_phase, table, out FileName);
                    SMPConf.updateActiveFileName(FileName, currTemplates[0]);
                }
            }
            currTemplates = SCRHandler.GetCSRXMLConfigFiles(protocol, t_proj, t_phase, "", out FileName);
            SMPConf.updateActiveFileName(FileName, currTemplates[0]);
            MDBSMconf.SaveDoNotOverWriteTimestamp(SMPConf);
            
            return true;
        }

        public bool CSRAssignReports(string protocol, string proj, string phase, Dictionary<string, string> SRreports, Dictionary<string, List<string>> PRMreports, out string message)
        {
            string ServerDir = System.Configuration.ConfigurationManager.AppSettings["ServerDir"];

            string FileName = "";
            message = "";
            CycleStatusHandler SCRHandler = new CycleStatusHandler();
            MDBSessionsManageConf MDBSMconf = new MDBSessionsManageConf();
            SessionsManageDisplay SMDisp = GetSessionsManageDisplay(protocol, proj, phase, "ALL");

            SCRHandler.GetCSRXMLConfigFiles(protocol, proj, phase, "", out FileName);
            FileName = SMDisp.Conf.getActiveFileName(FileName);

            List<string> XMLcontent = new List<string>(System.IO.File.ReadAllLines(Path.Combine(ServerDir, "EVData", FileName)));
            List<string> XMLcontentNew = new List<string>();
            bool ProtocolFlag = false;
            foreach (string line in XMLcontent)
            {
                string tline = line; 
                if (line.Contains("<protocol>" + protocol + "</protocol>")) ProtocolFlag = true;
                if ((ProtocolFlag) && (line.Contains("<table_div ")) )
                {
                    foreach (var pair in SRreports)
                    {
                        if (line.Contains("tdname=\"" + pair.Key + "\""))
                        {
                            if (line.Contains("SRreportId=\""))
                            {
                                tline = Regex.Replace(line, @"SRreportId=.*\u0022", "SRreportId=\"" + pair.Value + "\"");
                            }
                            else
                            {
                                tline = Regex.Replace(line, @">", " SRreportId=\"" + pair.Value + "\">");
                            }
                        }
                    }
                    XMLcontentNew.Add(tline);
                    tline = "<reportId>";
                    foreach (var pair in PRMreports)
                    {
                        if (line.Contains("tdname=\"" + pair.Key + "\""))
                        {
                            //foreach (string reportId in (List<string>)pair.Value) XMLcontentNew.Add("\t\t\t<reportId>"+reportId+"</reportId>");
                            List<string> Ids = pair.Value;
                            foreach (string reportId in Ids) XMLcontentNew.Add("\t\t\t<reportId>" + reportId + "</reportId>");
                        }
                    }
                }
                if (!tline.Contains("<reportId>")) XMLcontentNew.Add(tline);
            }
            System.IO.File.WriteAllLines(Path.Combine(ServerDir, "EVData", FileName), XMLcontentNew);
            if (!loadSessionsManageConfToDB(Path.Combine(ServerDir, "EVData", FileName)))
            {
                message += "Error: cannot load XML conf to DB";
                return false;
            }            

            return true;
        }


        //New SM report: exist sessions in DB but not in SM
        public SessionsManageDisplay ReportMissingSessionsInSM(string protocol, string proj, string phase, string _FilterSessions)
        {
            SessionsManageDisplay SMD = GetSessionsManageDisplay (protocol, proj, phase, _FilterSessions);
            foreach (SessionsManageOfTable SMT in SMD.SessionsManageOfTables)
            //foreach (string table in SMD.Conf.tables)
            {
                Dictionary<string, BsonDocument> SessionStartInfoDictionary; // key: SessionStart  value: Bson document with Info about results - Comment, Number of results (TMP/PRM_valid/PRM_not valid/ALL ?), Pass/Fail etc
                SessionStartInfoDictionary = new Dictionary<string, BsonDocument>();

                //Search all sessions in results DB newer than timestamp
                MDBStationResults tMDB_Staion = new MDBStationResults("DB_TMP", SMT.table);
                string phase_query = ";EQ;Phase;" + phase;
                if ((proj == "HBR") && (phase == "A0")) phase_query = ";In;Phase;A0,A1";
                MongoCursor tmpCur = tMDB_Staion.MDBFind("EQ;ProjectName;" + proj + phase_query, "AND", "DES", "TestEnd");
                foreach (BsonDocument Doc in tmpCur)
                {
                    string session = Doc.GetElement("SessionStart").Value.ToString();
                    if ((!SMT.SessionStartInfoDictionary.ContainsKey(session)) &&  (!SMT.MissingSessionsInSMDictionary.ContainsKey(session))) 
                    {
                        BsonDocument tmp = BuildSessionInfoDoc(SMT.table, session);
                        SessionsManage SM = (new SessionsManage()).GetSessionsManageFromDBResult(tMDB_Staion.MDBFindOne("EQ;SessionStart;" + session, "AND", "DES", "TestEnd"), SMT.table, SMD.Conf.fields, SMD.Conf);
                        if (tmp != null)
                        {
                            SMT.MissingSessionsInSMDictionary.Add(session, tmp);
                            SMT.MissingSessions.Add(SM);
                        }
                    }
                }
                MDBStationResults pMDB_Staion = new MDBStationResults("DB_PRM", SMT.table);

                MongoCursor prmCur = pMDB_Staion.MDBFind("EQ;ProjectName;" + proj + phase_query, "AND", "DES", "TestEnd");
                foreach (BsonDocument Doc in prmCur)
                {
                    string session = Doc.GetElement("SessionStart").Value.ToString();
                    if ((!SMT.SessionStartInfoDictionary.ContainsKey(session)) && (!SMT.MissingSessionsInSMDictionary.ContainsKey(session)))
                    {
                        BsonDocument tmp = BuildSessionInfoDoc(SMT.table, session);
                        SessionsManage SM = (new SessionsManage()).GetSessionsManageFromDBResult(pMDB_Staion.MDBFindOne("EQ;SessionStart;" + session, "AND", "DES", "TestEnd"), SMT.table, SMD.Conf.fields, SMD.Conf);
                        if (tmp != null)
                        {
                            SMT.MissingSessionsInSMDictionary.Add(session, tmp);
                            SMT.MissingSessions.Add(SM);
                        }
                    }
                }

                //Search Exist SM that may contain missing sessions
                foreach (SessionsManage MissingSM in SMT.MissingSessions)
                {
                    foreach (SessionsManage ExistSM in SMT.Sessions)
                    {
                        if (MissingSM.ImpEqualsExceptValidatoin(ExistSM))
                        {
                            MissingSM.comment = ExistSM._id.ToString();
                            break;
                        }
                    }
                }

            }
            return SMD;
        }

        public string CreateSMforMissingSession(string SessionStart, string protocol, string proj, string phase, string table)
        {
            SessionsManage SM = new SessionsManage();
            SessionsManageProtocolConf Conf = (new MDBSessionsManageConf()).Read(protocol, proj, phase);
            MDBSessionsManage MDBS = new MDBSessionsManage();
            MDBStationResults tMDB_Staion = new MDBStationResults("DB_TMP",table);
            BsonDocument tmpDoc = tMDB_Staion.MDBFindOne("EQ;SessionStart;" + SessionStart, "AND", "DES", "TestEnd");
            if ((tmpDoc == null) || (string.IsNullOrEmpty(tmpDoc.GetValue("_id").ToString())))
            {
                tMDB_Staion = new MDBStationResults("DB_PRM", table);
                tmpDoc = tMDB_Staion.MDBFindOne("EQ;SessionStart;" + SessionStart, "AND", "DES", "TestEnd");
            }
            if ((tmpDoc != null) && (!string.IsNullOrEmpty(tmpDoc.GetValue("_id").ToString())))
            {
                SM = (new SessionsManage()).GetSessionsManageFromDBResult(tmpDoc, table, Conf.fields, Conf);
                MDBS.Save(SM);
            }
            return(SM._id.ToString());
        }

        public bool IsSessionStartValidStatus(string sessionStart)
        {
            
            if (!SessionStart_SMDictionary.ContainsKey(sessionStart)) return false;
            MDBSessionsManage MDBS = new MDBSessionsManage();
            foreach (string id in SessionStart_SMDictionary[sessionStart])
            {
                SessionsManage SM = MDBS.Read(id);
                if ((SM.validation == General.SessionValidationStatus.Pending_review) || (SM.validation == General.SessionValidationStatus.PRM_valid) 
                    || (SM.validation == General.SessionValidationStatus.TMP_valid)) return true;
            }
            return false;
        }
    }
}