using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class CycleStatusHandler
    {
        Dictionary<string, int> Progress;

        public bool CycleStatusReportExec(SessionsManageDisplay SMDisp, out string errorMsg, out string filePathOut, string CSR_DB, string table)
        {
            Progress = new Dictionary<string, int>();
            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
            string ServerDir = System.Configuration.ConfigurationManager.AppSettings["ServerDir"];
            string newEmptyExcel = Path.Combine(Path.GetDirectoryName(EmptyExcel), Path.GetFileNameWithoutExtension(EmptyExcel).Replace("emptySR", "emptyCR" + "_" + SMDisp.Conf.protocol + "_" + SMDisp.Conf.proj + "_" + SMDisp.Conf.phase) + Path.GetExtension(EmptyExcel));
            if (!string.IsNullOrEmpty(table)) newEmptyExcel = Path.Combine(Path.GetDirectoryName(EmptyExcel), Path.GetFileNameWithoutExtension(EmptyExcel).Replace("emptySR", "emptyCR" + "_" + SMDisp.Conf.protocol + "_" + SMDisp.Conf.proj + "_" + SMDisp.Conf.phase + "_" + table) + Path.GetExtension(EmptyExcel));
            //New: Active file out of exist files:
            newEmptyExcel = SMDisp.Conf.getActiveFileName(Path.GetFileName(newEmptyExcel));
            newEmptyExcel = Path.Combine(Path.GetDirectoryName(EmptyExcel), newEmptyExcel);
            if (!File.Exists(newEmptyExcel))
            {
                errorMsg = "cannot find template file: " + newEmptyExcel;
                filePathOut = "";
                return false;
            }
            EmptyExcel = newEmptyExcel;
            string filename = "cycleStatusReport_" + SMDisp.Conf.protocol + "_" + SMDisp.Conf.proj + "_" + SMDisp.Conf.phase + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(EmptyExcel, filepath, true);
            errorMsg = "";
            filePathOut = filepath;
            foreach (SessionsManageOfTable SMT in SMDisp.SessionsManageOfTables)
            {
                //New for DP: enable Unify SM interface with separate CSR Excels:
                if ((!string.IsNullOrEmpty(table)) && (SMT.table!=table)) continue; 

                writeSessionsSheet(filepath, "Sessions", SMT, SMDisp.Conf, SMDisp.FilterSessionsView);

                Dictionary<string, List<string>> LogicUnit_SMDictionaryTMP = Build_LogicUnit_SMDictionary(SMT,"TMP"); // key: logicUnit  value: List of valid executed Sessions that belongs to this logicUnit                   
                if (CSR_DB.Contains("Pending")) LogicUnit_SMDictionaryTMP = Build_LogicUnit_SMDictionaryTMPWithPending(SMT, "TMP"); // key: logicUnit  value: List of valid executed Sessions that belongs to this logicUnit                   
                Dictionary<string, List<string>> LogicUnit_SMDictionaryPRM = Build_LogicUnit_SMDictionary(SMT, "PRM"); // key: logicUnit  value: List of valid executed Sessions that belongs to this logicUnit               
                foreach (Unit Unit in SMDisp.Conf.units)
                {
                    string query = "";
                    MongoCursor mCur = null;
                    if (CSR_DB.Contains("TMP"))
                    {
                        query = GetQueryForLogicUnitresults(SMDisp.Conf.proj, SMDisp.Conf.phase,LogicUnit_SMDictionaryTMP, Unit.ToString(), "TMP");
                        MDBStationResults MDB_Staion = new MDBStationResults("DB_TMP", SMT.table);
                        mCur = MDB_Staion.MDBFind(query, "AND", "DES", "TestEnd");
                    }
                    string queryPRM = "";
                    MongoCursor mCurPRM = null;
                    if (CSR_DB.Contains("PRM"))
                    {
                        queryPRM = GetQueryForLogicUnitresults(SMDisp.Conf.proj, SMDisp.Conf.phase, LogicUnit_SMDictionaryPRM, Unit.ToString(), "PRM");
                        MDBStationResults MDB_StaionPRM = new MDBStationResults("DB_PRM", SMT.table);
                        mCurPRM = MDB_StaionPRM.MDBFind(queryPRM, "AND", "DES", "TestEnd");
                    }
                    
                    writeTableLogicUnitCol(filepath, mCur,mCurPRM, Unit, SMT.table,SMDisp.Conf,query,queryPRM);
                }
            }
            writeProgress(filepath, SMDisp.Conf);
            return true;
        }

        public void writeTableLogicUnitCol(string excelFileName, MongoCursor ResCur, MongoCursor ResCurPRM, Unit LogicUnit, string table, SessionsManageProtocolConf SMPConf, string base_query, string base_queryPRM)
        {

            CycleStatusReport CSR = new CycleStatusReport(SMPConf, table, LogicUnit);
            CSR.AnalyzeResults(ResCur);
            CSR.AnalyzeResults(ResCurPRM);
            Dictionary<string, Dictionary<string, string>> statusResults = CSR.GetStatusResults();
            if (!Progress.ContainsKey(LogicUnit.skew + "_Full"))
            {
                Progress.Add(LogicUnit.skew + "_Full", 0);
                Progress.Add(LogicUnit.skew + "_Executed", 0);
            }
            Progress[LogicUnit.skew + "_Full"] += CSR.GetProgressContainerFull();
            Progress[LogicUnit.skew + "_Executed"] += CSR.GetProgressContainerExecuted();

            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                //debugging:
                //var tmpo = worksheet.Cells[1, 1].Value;
                //for (int i = 1; i < 100; i++)
                //{
                //    tmpo = worksheet.Cells[5, i].Value;
                //}

                //return;
                ////end
                var tmp = worksheet.Cells[1, 1].Value;
                worksheet.Cells[2, 1].Hyperlink = new Uri(getLaunchReportsLink(SMPConf.protocol, SMPConf.proj, SMPConf.phase, table, "ALL"));
                int skewCol = 0;
                int skweCnt = 1;
                int lineToCheck = 3;
                for (int i = 2; i < 100; i++)
                {
                    tmp = worksheet.Cells[lineToCheck, i].Value;
                    if ((tmp != null) && (tmp.ToString() == LogicUnit.skew) && (skweCnt==Convert.ToInt32(LogicUnit.num)))
                    {
                        skewCol = i;
                        break;
                    }
                    if ((tmp != null) && (tmp.ToString() == LogicUnit.skew)) skweCnt++;
                }

                int nextSkewCol = 100; //???? last populated cell?
                lineToCheck = 3;
                for (int i = skewCol + 1; i < 100; i++)
                {
                    tmp = worksheet.Cells[lineToCheck, i].Value;
                    //if ((tmp != null) && (tmp.ToString() != LogicUnit.skew))
                    if (tmp != null) 
                    {
                        nextSkewCol = i;
                        break;
                    }
                }

                int typeCol = 0;
                lineToCheck = 4;
                for (int i = skewCol; i < nextSkewCol; i++)
                {
                    tmp = worksheet.Cells[lineToCheck, i].Value;
                    if ((tmp != null) && (tmp.ToString().StartsWith(LogicUnit.type)))
                    {
                        typeCol = i;
                        break;
                    }
                }

                int setupCol = 0;
                lineToCheck = 6;
                for (int i = typeCol; i < nextSkewCol; i++)
                {
                    tmp = worksheet.Cells[lineToCheck, i].Value;
                    if ((tmp != null) && (tmp.ToString().StartsWith(LogicUnit.setup)))
                    {
                        setupCol = i;
                        break;
                    }
                }
                //worksheet.Cells[5, setupCol].Hyperlink = new Uri(CSR.getLogicUnitLinkToResults(base_query));
                
                foreach (var TDV in SMPConf.table_divs[table])
                {
                    int tableLine = 0;
                    for (int i = 2; i < 1000; i++)
                    {
                        tmp = worksheet.Cells[i, 1].Value;
                        //if ((tmp != null) && (tmp.ToString() == table))
                        if ((tmp != null) && (tmp.ToString() == TDV.Value.name))
                        {
                            tableLine = i;
                            worksheet.Cells[tableLine, 1].Hyperlink = new Uri(getLaunchReportsLink(SMPConf.protocol, SMPConf.proj, SMPConf.phase, table, TDV.Value.name));
                            break;
                        }
                    }
                    
                    //TODO: here ???
                    //build Status Report fo Logic unit (Per table div? onli if defferent table... different template (?)
                    /////////////// build status report start ///////////////////
                    List<BsonDocument> DocsForSR = new List<BsonDocument>();
                    if (ResCur!=null) foreach (BsonDocument doc in ResCur) DocsForSR.Add(doc);
                    if (ResCurPRM!=null) foreach (BsonDocument doc in ResCurPRM) DocsForSR.Add(doc);
                    MDBUserTableReport MDBReport = new MDBUserTableReport();
                    UserTableReport Report = MDBReport.ReadUserTableReport(TDV.Value.SRreportId); 
                    //UserTableReport Report = MDBReport.ReadUserTableReport("naty20190731160306"); 
                    string Lanes = SMPConf.SRTemplate;
                    string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
                    string newEmptyExcel = Path.Combine(Path.GetDirectoryName(EmptyExcel), Path.GetFileNameWithoutExtension(EmptyExcel).Replace("emptySR", "emptySR" + Lanes) + Path.GetExtension(EmptyExcel));
                    EmptyExcel = newEmptyExcel;
                    string SR_filename = excelFileName.Replace("cycleStatusReport", "CSR_" + LogicUnit.ToString() + "_" + table + "_StatusReport");
                    System.IO.File.Copy(newEmptyExcel, SR_filename, true);
                    List<string> Separators = new List<string>();
                    Separators.Add("Skew");
                    StatusReportHandler statusReportHandler = new StatusReportHandler();
                    StatusReport SR = null;
                    if ((DocsForSR.Count > 0) && (!string.IsNullOrEmpty(Report.Name))) 
                    {
                         SR = statusReportHandler.writeSheetCSR(SR_filename, "Skew", Lanes, true, DocsForSR, Report, Separators, "test", GetUnifiedQuery(base_query, base_queryPRM), CSR.GetSpecTestNamesWithFailedTrials(), CSR.GetSpecTestNamesWithMissingTrials());
                    }
                    /////////////// build status report end   ///////////////////
                    if (CSR.Stations.ContainsKey(TDV.Key)) worksheet.Cells[tableLine, setupCol].Hyperlink =
                        new Uri(getLogicUnitLinkToResults(SMPConf.protocol, SMPConf.proj, SMPConf.phase, table, base_query, base_queryPRM, SR_filename, LogicUnit.ToString())); //CSR.getLogicUnitLinkToResults(base_query)

                    if (CSR.Stations.ContainsKey(TDV.Key)) worksheet.Cells[tableLine + 1, setupCol].Value = CSR.getInfoString(CSR.Stations[TDV.Key]); //column index can be parsed
                    if (CSR.FWversions.ContainsKey(TDV.Key)) worksheet.Cells[tableLine + 2, setupCol].Value = CSR.getInfoString(CSR.FWversions[TDV.Key]); //column index can be parsed
                    if (CSR.Boards.ContainsKey(TDV.Key)) worksheet.Cells[tableLine + 3, setupCol].Value = CSR.getInfoString(CSR.Boards[TDV.Key]); //column index can be parsed
                    //

                    int colIndex = 1;
                    int lineIndex = tableLine + 5;
                    var dist = worksheet.Cells[lineIndex, 2].Value;
                    if (dist == null) return;//cannot parse excel TODO: formal Error message??
                    var next_dist = worksheet.Cells[lineIndex + 5, 2].Value;
                    var corner = worksheet.Cells[lineIndex, 3].Value;
                    lineIndex -= 1;
                    while (corner != null)
                    {
                        lineIndex++;
                        corner = worksheet.Cells[lineIndex, 3].Value;
                        if (corner == null) continue;
                        next_dist = worksheet.Cells[lineIndex, 2].Value;
                        if (next_dist != null) dist = next_dist;
                        string key = dist.ToString() + "-" + corner.ToString();
                        if (!statusResults.ContainsKey(key)) continue;
                        colIndex = setupCol - 1;
                        //foreach (string port in SMPConf.excelPorts)
                        foreach (var portResults in statusResults[key])
                        {
                            colIndex++;
                            worksheet.Cells[lineIndex, colIndex].Hyperlink = new Uri(CSR.getLinkToResults(key, portResults.Key, portResults.Value, GetUnifiedQuery(base_query,base_queryPRM)));
                            worksheet.Cells[lineIndex, colIndex].Value = portResults.Value;
                            worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                            worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 11;
                            string portResultsValue = portResults.Value.Split(' ')[0];
                            switch (portResultsValue)
                            {
                                case "NA":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                                    break;
                                case "None":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DarkGray);
                                    if (portResults.Value.Split(' ').Length > 1) worksheet.Cells[lineIndex, colIndex].AutoFitColumns(17);
                                    break;
                                case "Done":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                                    if (portResults.Value.Split(' ').Length > 1) worksheet.Cells[lineIndex, colIndex].AutoFitColumns(17);
                                    break;
                                case "Fail":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                    worksheet.Cells[lineIndex, colIndex].AutoFitColumns(17);
                                    break;
                                case "Review":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Pink);
                                    worksheet.Cells[lineIndex, colIndex].AutoFitColumns(17);
                                    break;
                                case "InComp":
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                                    worksheet.Cells[lineIndex, colIndex].AutoFitColumns(17);
                                    break;
                                default:
                                    worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);
                                    break;
                            }

                            if ((SR!=null) && (SR.IsCornerFailForCSR(key,portResults.Key)))
                            {
                                switch (portResultsValue)
                                {
                                    case "NA":
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.LightGray);
                                        break;
                                    case "Done":
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.Green);
                                        break;
                                    case "Fail":
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.Red);
                                        break;
                                    case "Review":
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.Pink);
                                        break;
                                    case "InComp":
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color1.SetColor(System.Drawing.Color.Lavender);
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.Gradient.Color2.SetColor(System.Drawing.Color.LightYellow);
                                        break;
                                    default:
                                        worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Lavender);
                                        break;

                                }
                            }
                            
                        }

                    }


                    //worksheet.Cells.AutoFitColumns(0);  //Autofit columns for all cells

                    package.Save();
                }

            }

            
        }


        public void writeSessionsSheet(string excelFileName, string sheetNameSubStr,SessionsManageOfTable SMT, SessionsManageProtocolConf SMPConf, string FilterSessions)
        {

            string table = SMT.table;
            int fieldsLine = 3;//hard coded
            List<string> sheetsNames = new List<string>();
            List<string> colNames = new List<string>();
            FileInfo newFile = new FileInfo(excelFileName);

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(1);
                //foreach (ExcelWorksheet tmpWorksheet in package.Workbook.Worksheets)
                //    if (tmpWorksheet.Name.Contains(sheetNameSubStr)) worksheet = package.Workbook.Worksheets[tmpWorksheet.Name];


                int colIndex = 3;

                while ((worksheet.Cells[fieldsLine, colIndex].Value != null) && (!string.IsNullOrEmpty(worksheet.Cells[fieldsLine, colIndex].Value.ToString())))
                {
                    colNames.Add(worksheet.Cells[fieldsLine, colIndex].Value.ToString());
                    colIndex++;
                }


                //TODO: find table section
                int tableLine = 0;
                var tmp = worksheet.Cells[1, 1].Value;
                for (int i = 4; i < 350; i++)
                {
                    tmp = worksheet.Cells[i, 1].Value;
                    if ((tmp != null) && (tmp.ToString() == table))
                    {
                        tableLine = i;
                        break;
                    }

                }
                int lineIndex=0;
                int nextSkewLine=0;
                foreach (SessionsManage SM in SMT.Sessions)
                {
                    if ((FilterSessions == "Valid_Pending") && ((SM.validation == General.SessionValidationStatus.TMP_not_valid) || (SM.validation == General.SessionValidationStatus.PRM_not_valid))) continue;
                    int ResultsSum = 0;
                    //TODO find Skew section and 1st free place (?? or ignore this and use LogicUnit field instaed??)
                    int skewLine = tableLine;
                    for (int i = tableLine; i < 350; i++)
                    {
                        tmp = worksheet.Cells[i, 2].Value;
                        //if ((tmp != null) && (tmp.ToString() == SM.logicUnit.Split('_')[0]))
                        if ((tmp != null) && (tmp.ToString() == SM.Data.GetValue("Skew","").ToString()))
                        {
                            skewLine = i;
                            break;
                        }
                    }
                    for (int i = skewLine; i < 350; i++)
                    {
                        tmp = worksheet.Cells[i, 2].Value;
                        //if ((tmp != null) && (tmp.ToString() == SM.logicUnit.Split('_')[0]))
                        if ((tmp != null) && (tmp.ToString() != SM.Data.GetValue("Skew", "").ToString()))
                        {
                            nextSkewLine = i;
                            break;
                        }
                    }
                    lineIndex=skewLine;
                    colIndex = 3;
                    tmp = worksheet.Cells[lineIndex, colIndex].Value;
                    while ((tmp !=null) && (lineIndex <nextSkewLine))
                    {
                        lineIndex++;
                        tmp = worksheet.Cells[lineIndex, colIndex].Value;
                    }
                    if (lineIndex == nextSkewLine)
                    {
                        worksheet.InsertRow(lineIndex, 5, skewLine);
                    }

                    foreach (string col in colNames)
                    {
                        switch (col)
                        {
                            case "Validation":
                                worksheet.Cells[lineIndex, colIndex].Value = SM.validation.ToString();
                                break;
                            case "LogicUnit":
                                worksheet.Cells[lineIndex, colIndex].Value = SM.logicUnit;
                                break;
                            case "Board ID":
                                worksheet.Cells[lineIndex, colIndex].Value = SM.board_id;
                                break;
                            case "Unit ID":
                                worksheet.Cells[lineIndex, colIndex].Value = SM.unit_id;
                                break;
                            case "NVM":
                                worksheet.Cells[lineIndex, colIndex].Value = SM.NVM;
                                break;
                            case "Sessions":
                                //TOD: add results numbers and fail/pss color indication (take code from SessionManageList.chtml)
                                string ss = "";
                                string resultsNum ="";
                                string status = "Pass";
                                string statusInd = "";

                                foreach (string session in SM.Sessions)
                                {
                                    if ((SM.validation == General.SessionValidationStatus.TMP_valid) || (SM.validation == General.SessionValidationStatus.TMP_not_valid) || (SM.validation == General.SessionValidationStatus.Pending_review))
                                    {
                                        resultsNum = SMT.SessionStartInfoDictionary[session].GetValue("TMP results", "").ToString();
                                        status = SMT.SessionStartInfoDictionary[session].GetValue("TMP results status", "").ToString();
                                    }
                                    if (SM.validation == General.SessionValidationStatus.PRM_valid) 
                                    {
                                        resultsNum = SMT.SessionStartInfoDictionary[session].GetValue("PRM results inc", "").ToString();
                                        status = SMT.SessionStartInfoDictionary[session].GetValue("PRM results inc status", "").ToString();
                                    }
                                    if (SM.validation == General.SessionValidationStatus.PRM_not_valid) 
                                    {
                                        resultsNum = SMT.SessionStartInfoDictionary[session].GetValue("PRM results exc", "").ToString();
                                        status = SMT.SessionStartInfoDictionary[session].GetValue("PRM results exc status", "").ToString();
                                    }
                                    if (status == "Fail")
                                    {
                                        worksheet.Cells[lineIndex, colIndex].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                                        //statusInd = " **Fail**";
                                    }
                                    else
                                    {
                                        statusInd = "";
                                    }
                                    ss += session + "(" + resultsNum + statusInd + "),";
                                    ResultsSum += Convert.ToInt32(resultsNum);
                                }

                                worksheet.Cells[lineIndex, colIndex].Value = ss.TrimEnd(',');
                                break;
                            case "Results":
                                worksheet.Cells[lineIndex, colIndex].Value = ResultsSum;
                                break;
                            case "Comments":
                                worksheet.Cells[lineIndex, colIndex].Value = SM.comment;
                                break;
                            default:
                                if (SM.FieldsList.Contains(col)) worksheet.Cells[lineIndex, colIndex].Value = SM.Data.GetValue(col, "").ToString();
                                if (SMPConf.fieldsAliases.Contains(col))
                                {
                                    worksheet.Cells[lineIndex, colIndex].Value = SM.Data.GetValue(SM.FieldsList[SMPConf.fieldsAliases.IndexOf(col)], "").ToString();
                                } 
                                break;
                        }
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[lineIndex, colIndex].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                        worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Transparent);
                        worksheet.Cells[lineIndex, colIndex].Style.Font.Size = 11;

                        if (SM.validation == General.SessionValidationStatus.Pending_review) worksheet.Cells[lineIndex, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Orange);
                        colIndex++;
                    }
                }
                //worksheet.Cells.AutoFitColumns(0);  //Autofit columns for all cells

                package.Save();

            }
        }

        public void writeProgress(string excelFileName, SessionsManageProtocolConf SMPConf)
        {
            FileInfo newFile = new FileInfo(excelFileName);
            int ALL_Full = 0;
            int ALL_Executed = 0;
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[1];

                var tmp = worksheet.Cells[1, 1].Value;
                int skewCol = 0;
                int lineToCheck = 2;
                for (int i = 2; i < 100; i++)
                {
                    tmp = worksheet.Cells[lineToCheck, i].Value;
                    if ((tmp != null) && (tmp.ToString() == SMPConf.units[0].skew))
                    {
                        skewCol = i;
                        break;
                    }
                }
                if (skewCol == 0) return;
                while (tmp != null)
                {
                    if (Progress.ContainsKey(tmp + "_Full"))
                    {
                        if ((worksheet.Cells[3, skewCol].Value==null) || (string.IsNullOrWhiteSpace(worksheet.Cells[3, skewCol].Value.ToString())))  worksheet.Cells[3, skewCol].Value = Progress[tmp + "_Full"];
                        ALL_Full += Progress[tmp + "_Full"];
                    }
                    if (Progress.ContainsKey(tmp + "_Executed"))
                    {
                        worksheet.Cells[4, skewCol].Value = Progress[tmp + "_Executed"];
                        //worksheet.Cells[5, skewCol].Value = (((double)Progress[tmp + "_Executed"] / (double)Progress[tmp + "_Full"])).ToString("0.##");
                        ALL_Executed += Progress[tmp + "_Executed"];
                    }
                    if ((string)tmp == "ALL") 
                    {
                        if ((worksheet.Cells[3, skewCol].Value==null) || (string.IsNullOrWhiteSpace(worksheet.Cells[3, skewCol].Value.ToString()))) worksheet.Cells[3, skewCol].Value = ALL_Full;
                        worksheet.Cells[4, skewCol].Value = ALL_Executed;
                        //worksheet.Cells[5, skewCol].Value = (((double)ALL_Executed / (double)ALL_Full)).ToString("0.##");
                    }
                    skewCol++;
                    tmp = worksheet.Cells[2, skewCol].Value;
                }
                package.Save();
            }
        }


        //New: Validate Excel template; 
        //TODO:
        // 1.traverse and check logic
        // 2. Add "Sessions sheet"

        public string validateTemplateFileVSConf(SessionsManageDisplay SMDisp, string table)
        {
            SessionsManageProtocolConf SMPConf = SMDisp.Conf;
            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
            string ServerDir = System.Configuration.ConfigurationManager.AppSettings["ServerDir"];
            string excelFileName;
            //string excelFileName = Path.Combine(Path.GetDirectoryName(EmptyExcel), Path.GetFileNameWithoutExtension(EmptyExcel).Replace("emptySR", "emptyCR" + "_" + SMPConf.protocol + "_" + SMPConf.proj + "_" + SMPConf.phase) + Path.GetExtension(EmptyExcel));
            //if (!string.IsNullOrEmpty(table)) excelFileName = Path.Combine(Path.GetDirectoryName(EmptyExcel), Path.GetFileNameWithoutExtension(EmptyExcel).Replace("emptySR", "emptyCR" + "_" + SMPConf.protocol + "_" + SMPConf.proj + "_" + SMPConf.phase + "_" + table) + Path.GetExtension(EmptyExcel));
            //New: Active file out of exist files:
            GetCSRExcelTemplates(SMPConf.protocol, SMPConf.proj, SMPConf.phase, table, out excelFileName);
            excelFileName = SMDisp.Conf.getActiveFileName(excelFileName);
            excelFileName = Path.Combine(Path.GetDirectoryName(EmptyExcel), excelFileName);

            List<string> Corners = new List<string>(new string[] { "HVHT", "HVLT", "NOMN", "LVHT", "LVLT" });
            if (SMPConf.corners.Count > 0) Corners = new List<string>(SMPConf.corners);

            List<string> messages = new List<string>();
            messages.Add ("Validation Report for:" + SMPConf.protocol + "_" + SMPConf.proj + "_" + SMPConf.phase + " Excel file:" + excelFileName);
            messages.Add(" ");
            List<string> tables = new List<string>();
            if (table != null) tables.Add(table);
            else
            {
                foreach (SessionsManageOfTable SMT in SMDisp.SessionsManageOfTables)
                {
                    tables.Add(SMT.table);
                }
            }

            try
            {
                FileInfo newFile = new FileInfo(excelFileName);
                using (ExcelPackage package = new ExcelPackage(newFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                    foreach (string _table in tables)
                    {
                        foreach (Unit unit in SMPConf.units)
                        {
                            Unit unitFound = new Unit();
                            var tmp = worksheet.Cells[1, 1].Value;
                            //worksheet.Cells[2, 1].Hyperlink = new Uri(getLaunchReportsLink(SMPConf.protocol, SMPConf.proj, SMPConf.phase, table, "ALL"));
                            int skewCol = 0;
                            int skweCnt = 1;
                            int lineToCheck = 3;
                            for (int i = 2; i < 100; i++)
                            {
                                tmp = worksheet.Cells[lineToCheck, i].Value;
                                if ((tmp != null) && (tmp.ToString() == unit.skew) && (skweCnt == Convert.ToInt32(unit.num)))
                                {
                                    skewCol = i;
                                    break;
                                }
                                if ((tmp != null) && (tmp.ToString() == unit.skew)) skweCnt++;
                            }

                            if (worksheet.Cells[lineToCheck, skewCol].Value != null) unitFound.skew = worksheet.Cells[lineToCheck, skewCol].Value.ToString();
                            unitFound.num = skweCnt.ToString();


                            int nextSkewCol = 100; //???? last populated cell?
                            lineToCheck = 3;
                            for (int i = skewCol + 1; i < 100; i++)
                            {
                                tmp = worksheet.Cells[lineToCheck, i].Value;
                                if ((tmp != null) && (tmp.ToString() != unit.skew))
                                    if (tmp != null)
                                    {
                                        nextSkewCol = i;
                                        break;
                                    }
                            }

                            int typeCol = 0;
                            lineToCheck = 4;
                            for (int i = skewCol; i < nextSkewCol; i++)
                            {
                                tmp = worksheet.Cells[lineToCheck, i].Value;
                                if ((tmp != null) && (tmp.ToString().StartsWith(unit.type)))
                                {
                                    typeCol = i;
                                    break;
                                }
                            }

                            if (worksheet.Cells[lineToCheck, typeCol].Value != null) unitFound.type = worksheet.Cells[lineToCheck, typeCol].Value.ToString();

                            int setupCol = 0;
                            lineToCheck = 6;
                            for (int i = typeCol; i < nextSkewCol; i++)
                            {
                                tmp = worksheet.Cells[lineToCheck, i].Value;
                                if ((tmp != null) && (tmp.ToString().StartsWith(unit.setup)))
                                {
                                    setupCol = i;
                                    break;
                                }
                            }

                            if (worksheet.Cells[lineToCheck, setupCol].Value != null) unitFound.setup = worksheet.Cells[lineToCheck, setupCol].Value.ToString();

                            messages.Add("OK: Table: " + _table + " Searched unit:" + unit.ToString() + "Found unit:" + unitFound.ToString());


                            foreach (var TDV in SMPConf.table_divs[_table])
                            {
                                int cornersCnt = 0;

                                int tableLine = 0;
                                for (int i = 2; i < 1000; i++)
                                {
                                    tmp = worksheet.Cells[i, 1].Value;
                                    if ((tmp != null) && (tmp.ToString() == TDV.Value.name))
                                    {
                                        tableLine = i;
                                        break;
                                    }
                                }


                                int colIndex = 1;
                                int lineIndex = tableLine + 5;
                                var dist = worksheet.Cells[lineIndex, 2].Value;
                                if (dist == null)
                                {
                                    messages.Add("Error: Configuration table div:" + TDV.Value.name + " does not have match in Excel");
                                    break;
                                }
                                if (!TDV.Value.TableDivLines.ContainsKey(dist.ToString())) messages.Add("Error: Excel table div:" + TDV.Value.name + " line:" + dist.ToString() + " does not have match in configuration");
                                var next_dist = worksheet.Cells[lineIndex + 5, 2].Value;
                                var corner = worksheet.Cells[lineIndex, 3].Value;
                                lineIndex -= 1;
                                while (corner != null)
                                {
                                    lineIndex++;
                                    corner = worksheet.Cells[lineIndex, 3].Value;
                                    if (corner == null) continue;
                                    next_dist = worksheet.Cells[lineIndex, 2].Value;
                                    if (next_dist != null) dist = next_dist;
                                    string key = dist.ToString() + "-" + corner.ToString();
                                    if (Corners.Contains(corner)) cornersCnt++;
                                    colIndex = setupCol - 1;

                                }
                                if (Corners.Count > cornersCnt) messages.Add("Error: could not find all corners for unit-" + unit.ToString() + " Table div-" + TDV.Key + "Table-" + _table);
                            }
                        }
                    }
                    package.Dispose();


                }
            }
            catch (Exception e)
            {
                messages.Add("Error: Internal error happaned:" + e.Message);
            }

            string reportFileName = Path.Combine(ServerDir,"tmp","validateCSR_" + SMDisp.Conf.protocol + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
            File.AppendAllLines(reportFileName, messages);
            return reportFileName;
        }

        private Dictionary<string, List<string>> Build_LogicUnit_SMDictionary(SessionsManageOfTable SMT, string DB)
        {
            Dictionary<string, List<string>> LogicUnit_SMDictionary = new Dictionary<string, List<string>>();
            foreach (SessionsManage SM in SMT.Sessions)
            {
                if (SM.validation.ToString().Contains(DB) && (SM.validation==General.SessionValidationStatus.PRM_valid || SM.validation==General.SessionValidationStatus.TMP_valid))
                {
                    if (LogicUnit_SMDictionary.ContainsKey(SM.logicUnit))LogicUnit_SMDictionary[SM.logicUnit].AddRange(SM.Sessions);
                    else LogicUnit_SMDictionary.Add(SM.logicUnit, new List<string>(SM.Sessions));
                }
            }
            return LogicUnit_SMDictionary;
        }

        private Dictionary<string, List<string>> Build_LogicUnit_SMDictionaryTMPWithPending(SessionsManageOfTable SMT, string DB)
        {
            Dictionary<string, List<string>> LogicUnit_SMDictionary = new Dictionary<string, List<string>>();
            foreach (SessionsManage SM in SMT.Sessions)
            {
                if (((SM.validation.ToString().Contains("Pending") ||SM.validation.ToString().Contains(DB)) && (SM.validation == General.SessionValidationStatus.Pending_review || SM.validation == General.SessionValidationStatus.TMP_valid)))
                {
                    if (LogicUnit_SMDictionary.ContainsKey(SM.logicUnit)) LogicUnit_SMDictionary[SM.logicUnit].AddRange(SM.Sessions);
                    else LogicUnit_SMDictionary.Add(SM.logicUnit, new List<string>(SM.Sessions));
                }
            }
            return LogicUnit_SMDictionary;
        }

        private string GetQueryForLogicUnitresults(string proj, string phase,Dictionary<string, List<string>> LogicUnit_SMDictionary, string LogicUnit, string dbStr)
        {
            string query = "EQ;ProjectName;"+proj+ ";EQ;Phase;"+phase+";In;SessionStart;";
            if (!LogicUnit_SMDictionary.ContainsKey(LogicUnit)) return query + "empty";
            foreach (string session in LogicUnit_SMDictionary[LogicUnit]) query += session + ",";
            query = query.TrimEnd(',');
            if (dbStr=="PRM") query += ";EQ;Indicator;Y";
            return query;
        }

        private string GetUnifiedQuery(string queryTMP, string queryPRM)    
        {
            if ((queryTMP.Contains("empty")) || (string.IsNullOrEmpty(queryTMP))) return queryPRM;
            if ((queryPRM.Contains("empty")) || (string.IsNullOrEmpty(queryPRM))) return queryTMP;
            //string tmpPRM = queryPRM.Replace("In;SessionStart;",",").Replace(";EQ;Indicator;Y","");
            string tmpPRM = Regex.Replace(queryPRM, @"EQ;ProjectName;.*;EQ;Phase;.*;In;SessionStart;", ",").Replace(";EQ;Indicator;Y", "");
            return queryTMP.Replace(";EQ;Indicator;Y", "") + tmpPRM + ";EQ;Indicator;Y";
        }

        private string getLogicUnitLinkToResults(string protocol, string proj, string phase, string table, string baseQuery, string baseQuaryPRM, string FilePath, string LogicUnit)
        {
            string link = "";
            //String strPathAndQuery = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
            //String strUrl = System.Web.HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");
            string strUrl = System.Configuration.ConfigurationManager.AppSettings["webHost"];
            //if (_db == System.Configuration.ConfigurationManager.AppSettings["DB_PRM"])
            //{
            //link = strUrl + "ManageDB/SearchPRM?db=" + System.Configuration.ConfigurationManager.AppSettings["DB_PRM"] + "&col=" + table + "&Query=" + baseQuery;
            //}
            //else
            //{
            //link = strUrl + "ManageDB/SearchTMP?db=" + System.Configuration.ConfigurationManager.AppSettings["DB_TMP"] + "&col=" + table + "&Query=" + baseQuery;
            //}

            

            link = strUrl + "ManageDB/ShowLogicUnitLinks?col=" + table + "&Query=" + baseQuery + "&PRMQuery=" + baseQuaryPRM + 
                "&SRFile=" + FilePath + "&Unit=" + LogicUnit + "&Protocol="+ protocol + "&proj=" + proj + "&phase=" + phase;

            return link;
        }
        private string getLaunchReportsLink(string protocol, string proj, string phase, string table, string table_div)
        {
            string link = "";
            string strUrl = System.Configuration.ConfigurationManager.AppSettings["webHost"];

            link = strUrl + "GeneralDB/CSRLaunchReports?table_name=" + table + "&table_div=" + table_div + "&Protocol=" + protocol + "&proj=" + proj + "&phase=" + phase;

            return link;
        }


        public List<string> GetCSRExcelTemplates(string protocol, string proj, string phase, string table, out string Filename)
        {
            List<string> files = new List<string>();
            Filename = "";
            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
            Filename = "emptyCR" + "_" + protocol + "_" + proj + "_" + phase + Path.GetExtension(EmptyExcel);
            //New: support "separateCSRperTable", use table
            if (!string.IsNullOrEmpty(table)) Filename = "emptyCR" + "_" + protocol + "_" + proj + "_" + phase + "_" + table + Path.GetExtension(EmptyExcel);
            DirectoryInfo inf = new DirectoryInfo(Path.GetDirectoryName(EmptyExcel));
            foreach (FileInfo f_inf in inf.GetFiles(Path.GetFileNameWithoutExtension(Filename) + "*"))
            {
                files.Add(f_inf.Name);
            }

            files.Sort();
            //files.Reverse();
            return files;

        }

        public List<string> GetCSRXMLConfigFiles(string protocol, string proj, string phase, string table, out string Filename)
        {
            List<string> files = new List<string>();
            Filename = "";
            string fileName;
            string ServerDir = System.Configuration.ConfigurationManager.AppSettings["ServerDir"];
            string Interface = protocol.Split('_')[0];
            if (Interface.StartsWith("USB")) Interface = "USB";
            fileName = Path.Combine(ServerDir, "EVData", "SessionsManageConf_" + Interface + "_" + proj + "_" + phase + ".xml");

            Filename = Path.GetFileName(fileName);
            //files.Add(Path.GetFileName(fileName));
            DirectoryInfo inf = new DirectoryInfo(Path.GetDirectoryName(fileName));
            foreach (FileInfo f_inf in inf.GetFiles(Path.GetFileNameWithoutExtension(fileName)+"*"))
            {
               files.Add(f_inf.Name);
            }

            files.Sort();
            //files.Reverse();
            return files;
        }

        public void pocExpandTemplateSheet(string excelFileName, string sheetNameSubStr, int line2copy, int col2Copy)
        {

            //int fieldsLine = 3;//hard coded
            List<string> sheetsNames = new List<string>();
            List<string> colNames = new List<string>();
            FileInfo newFile = new FileInfo(excelFileName);

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                //foreach (ExcelWorksheet tmpWorksheet in package.Workbook.Worksheets)
                //    if (tmpWorksheet.Name.Contains(sheetNameSubStr)) worksheet = package.Workbook.Worksheets[tmpWorksheet.Name];

                worksheet.InsertRow(line2copy, 5, 11);
                //worksheet.InsertColumn(col2Copy, 1, 4);
                worksheet.Cells["A16:A20"].Merge = true;
                worksheet.Cells["B16:B20"].Merge = true;
                worksheet.Cells["A6:A20"].Merge = true;

                package.Save();

            }
        }
    }
}