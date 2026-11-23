using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq; 
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using OfficeOpenXml;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class CycleStatusReportExcelBuilder
    {
        public string ServerDir { get { return System.Configuration.ConfigurationManager.AppSettings["ServerDir"]; } }
        Dictionary<string, Int32> SkewNBoardLookUP;

        public SessionsManageProtocolConf SMPConf;
        CyclePlanMetaData CPMetaData;
        //some CyclePlan objects with needed data
        //...
        public Unit LogicUnit;
        public List<string> CSR_excelPorts { get; set; }
        public List<string> Corners { get; set; }
        public List<string> ExistUnits { get; set; }


        public CycleStatusReportExcelBuilder(SessionsManageProtocolConf _SMPConf, CyclePlanMetaData _CPMetaData)
        {
            //copied from "CycleStatusReport" constructor, reference??

            SMPConf = _SMPConf;
            CPMetaData = _CPMetaData;
            Corners = new List<string>(new string[] { "HVHT", "HVLT", "NOMN", "LVHT", "LVLT" });
            if (SMPConf.corners.Count > 0) Corners = new List<string>(SMPConf.corners);
            //some CyclePlan objects with needed data (??)
            ExistUnits = new List<string>();
            foreach (Unit _unit in SMPConf.units) if (_unit.ToString().EndsWith("1")) ExistUnits.Add(_unit.ToString());

        }

        //hard coded basic POC tht creates new line- Just for reference
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
                //worksheet.Cells[1, 6, 1, 20].Merge = true; equivalen to line above?

                package.Save();

            }
        }

        //1. choose correct template (?) (maybe it will done in constructor?)
        //2. crate all neede lines, with correct names
        //2.5 merge first cell from start index to last ??
        //3. create all needed columns (?)
        public string pocBuildFromScratch(string selectedTable, out string message)
        {
            message = "";
            int TableDivStart = 6;
            int TableDivLinesCnt = 0;
            int TableDivCnt = 0;
            int TableCnt = 0;
            string fileName = "";

            fileName = getExcelTemplateForProtocol_Table(SMPConf.protocol, "", out message);
            if (!String.IsNullOrEmpty(message)) return (fileName);

            foreach (string table in SMPConf.tables)
            {
                //////////not ready yet, not sure it needs to be inside this loop, TODO:flow of seperated CSR per table
                if ((SMPConf.separateCSRperTable == "Y") && (table != selectedTable)) continue;
                if ((SMPConf.separateCSRperTable == "Y") && (table==selectedTable))
                {
                    fileName = getExcelTemplateForProtocol_Table(SMPConf.protocol, table, out message);
                }
                /////////
                TableCnt++;
                if (TableCnt > 1)
                {
                    //Create new table section, and calc indexes (?) same as new tableDiv section
                    int currTableDivStart = TableDivStart;
                    TableDivStart += 5 + (TableDivLinesCnt * Corners.Count) + 1;
                    pocCreateDivTable(fileName, TableDivStart, "<Table_Div>", "<first>", currTableDivStart);
                }

                TableDivCnt = 0;
                foreach (var TDV in SMPConf.table_divs[table])
                {
                    TableDivCnt++;
                    if (TableDivCnt > 1)
                    {
                        //Create new tableDiv section, and calc indexes (?)
                        int currTableDivStart = TableDivStart;
                        TableDivStart += 5 + (TableDivLinesCnt * Corners.Count) + 1;
                        pocCreateDivTable(fileName, TableDivStart, TDV.Key, "<first>", currTableDivStart);
                    }
                    else
                    {
                        //replace <Table_Div> in template with TDV.Key (?)
                        replaceCellConstantInSheet(fileName, 0, "<Table_Div>", TDV.Key.ToString());
                    }
                    TableDivLinesCnt = 0;
                    foreach (var TDL in TDV.Value.TableDivLines)
                    {
                        TableDivLinesCnt++;
                        //calc indexes
                        if (TableDivLinesCnt == 1)
                        {
                            //Just replace the name with TDL.Key.ToString()
                            replaceCellConstantInSheet(fileName, 0, "<first>", TDL.Key.ToString());
                        }
                        else
                        {
                            // TODO: replace '1' with correct index
                            pocCreateDivTableLine(fileName, TableDivStart + 5 + (TableDivLinesCnt - 1) * Corners.Count, TDL.Key.ToString(), TableDivStart, TableDivStart + 5);
                        }

                    }
                }
            }
            //Add units
            //pocCopyUnit(fileName, 10, 8);
            //pocCopyUnit(fileName, 11, 9);
           
            foreach (Unit _allUnit in SMPConf.units)
            {
                if (!ExistUnits.Contains(_allUnit.ToString()))
                {
                    //handle "Pos" or "Ref" = -1 ??
                    int startIndex = 4 + ((SMPConf.excelPorts.Count) * getNewUnitPos(_allUnit.ToString()));
                    int refIndex = 4 + ((SMPConf.excelPorts.Count) * getNewUnitRef(_allUnit.ToString()));
                    pocCopyUnit(fileName, startIndex, refIndex); //TODO: copy "Meta" values, merge needed cells (like in Div)
                    pushNewUnit(_allUnit.ToString());
                    ////Megre needed cells in Skew line (?)
                    //pocMergeSkewCells(fileName);
                 }
            }
            //Megre needed cells in Skew line (?)
            pocMergeSkewCells(fileName);
            //New: add Conf XML
            pocWriteConfSheet(fileName, CreateConfXML());

            return (Path.GetFileName(fileName));

        }

        // not sure if need to implement: (in order to keep colors?? and other small fixes they may do )
        //1. Calculate lines/cols to remove/add
        //2. remove all needed lines, 
        //3. crate all neede lines, with correct names
        //3.5 merge first cell from start index to last ??
        //4. remove all needed columns
        //5. create all needed columns (?)
        public void pocUpdateExistExcel()
        {
            foreach (string table in SMPConf.tables)
            {
                foreach (var TDV in SMPConf.table_divs[table])
                {
                    foreach (var TDL in TDV.Value.TableDivLines)
                    {

                    }
                }
            }
        }

        public void pocCreateDivTableLine(string excelFileName, int newLineBeginIndex, string newTableDivLineName, int tableDivStartIndex, int line2CopyFrom)
        { 
            FileInfo newFile = new FileInfo(excelFileName);

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                worksheet.InsertRow(newLineBeginIndex, Corners.Count, line2CopyFrom);
                //string rangeSTR = "";
                //worksheet.Cells["A16:A20"].Merge = true;
                //rangeSTR = "A" + newLineBeginIndex.ToString() + ":" + "A" + (newLineBeginIndex + Corners.Count - 1).ToString();
                worksheet.Cells[getRangeSTR("A", newLineBeginIndex, "A", (newLineBeginIndex + Corners.Count - 1))].Merge = true;
                //worksheet.Cells["B16:B20"].Merge = true;
                //rangeSTR = "B" + newLineBeginIndex.ToString() + ":" + "B" + (newLineBeginIndex + Corners.Count - 1).ToString();
                worksheet.Cells[getRangeSTR("B", newLineBeginIndex, "B", (newLineBeginIndex + Corners.Count - 1))].Merge = true;
                worksheet.Cells[newLineBeginIndex, 2].Value = newTableDivLineName;
                int cornerCnt = 0;
                foreach (string cornerName in Corners)
                {
                    worksheet.Cells[newLineBeginIndex + cornerCnt, 3].Value = cornerName;
                    cornerCnt++;
                }
                //worksheet.Cells["A6:A20"].Merge = true;
                //rangeSTR = "A" + tableDivStartIndex.ToString() + ":" + "A" + (newLineBeginIndex + Corners.Count - 1).ToString();
                worksheet.Cells[getRangeSTR("A", tableDivStartIndex, "A", (newLineBeginIndex + Corners.Count - 1))].Merge = true;
                // in case of insert a line in middle of exist lines, not sure how to handle this merge.(currently works ok for adding last line always)
                package.Save();

            }

        }

        public void pocCreateDivTable(string excelFileName, int newLineBeginIndex, string newTableDivName, string newTableDivLineName, int refTableDivStartIndex)
        {
            FileInfo newFile = new FileInfo(excelFileName);

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                //New section beginning
                for (int i = 0; i < 5; i++)
                {
                    worksheet.InsertRow(newLineBeginIndex + i, 1, refTableDivStartIndex + i);
                    if (SMPConf.excelPorts[0]!="NONE")
                    {
                        //Merge "Meta" cells
                        if (worksheet.Cells[refTableDivStartIndex + i, 2].Value != null) worksheet.Cells[newLineBeginIndex + i, 2, newLineBeginIndex + i, 2 + 1].Merge = true;
                        int step = SMPConf.excelPorts.Count;
                        for (int j = 4; j < 100; j += step)
                        {
                            if (worksheet.Cells[refTableDivStartIndex + i, j].Value != null) worksheet.Cells[newLineBeginIndex + i, j, newLineBeginIndex + i, j + step -1].Merge = true;
                        }
                    }
                    //copy values
                    for (int j = 2; j < 100; j++) if (worksheet.Cells[refTableDivStartIndex + i, j].Value != null) worksheet.Cells[newLineBeginIndex + i, j].Value = worksheet.Cells[refTableDivStartIndex + i, j].Value;
                    worksheet.Cells[getRangeSTR("B", newLineBeginIndex+i, "C", newLineBeginIndex + i)].Merge = true;
                }
                worksheet.Cells[getRangeSTR("A", newLineBeginIndex, "A", (newLineBeginIndex + 4))].Merge = true;
                worksheet.Cells[newLineBeginIndex, 1].Value = newTableDivName;
                worksheet.InsertRow(newLineBeginIndex + 5, 1, newLineBeginIndex - 1); //Seperator/EOF line
                package.Save();
            }

            pocCreateDivTableLine(excelFileName, newLineBeginIndex + 5, newTableDivLineName, newLineBeginIndex, refTableDivStartIndex + 5);
        }


        public void pocCopyUnit(string excelFileName, int newColBeginIndex, int refColStartIndex)
        {
            if ((newColBeginIndex < 0) || (refColStartIndex < 0)) return;
            FileInfo newFile = new FileInfo(excelFileName);

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                for (int i = 0; i < SMPConf.excelPorts.Count; i++)  worksheet.InsertColumn(newColBeginIndex +i, 1, refColStartIndex +i);
                //Merge "Meta" cells
                //copy values
                for (int j = 3; j < 143; j++)
                {
                    if (worksheet.Cells[j, refColStartIndex].Value == null) continue;
                    if (worksheet.Cells[j, refColStartIndex, j, refColStartIndex + SMPConf.excelPorts.Count - 1].Merge == true)
                    {
                        worksheet.Cells[j, newColBeginIndex, j, newColBeginIndex + SMPConf.excelPorts.Count - 1].Merge = true;
                        worksheet.Cells[j, newColBeginIndex].Value = worksheet.Cells[j, refColStartIndex].Value;
                    }
                    else
                    {
                        for (int i = 0; i < SMPConf.excelPorts.Count ; i++) worksheet.Cells[j, newColBeginIndex + i].Value = worksheet.Cells[j, refColStartIndex + i].Value;
                    }
                }
                package.Save();
            }

        }

        public void pocMergeSkewCells(string excelFileName)
        {
            FileInfo newFile = new FileInfo(excelFileName);

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                List<string> typ1Units = new List<string>();
                foreach (Unit _unit in SMPConf.units) if ((_unit.skew=="TYP") && (_unit.num=="1")) typ1Units.Add(_unit.ToString());
                int UnitnumOfCells = SMPConf.excelPorts.Count * typ1Units.Count;

                //worksheet.Cells["H3:K3"].Merge = true;
                //worksheet.Cells["L3:O3"].Merge = true;
                string unitsStr = "";
                foreach (string unitStr in ExistUnits)
                {
                    unitsStr += unitStr + ",";
                }
                for (int j = 4; j < 143; j++)
                {
                    if (worksheet.Cells[3, j].Value == null) continue;
                    if (worksheet.Cells[3, j, 3, j + SkewNBoardLookUP["Skew"] -1 ].Merge == true) continue;
                    if (!unitsStr.Contains(worksheet.Cells[3, j].Value.ToString())) continue;
                    worksheet.Cells[3, j, 3, j + SkewNBoardLookUP["Skew"] - 1].Merge = true;

                }
                int BoardsNum = SkewNBoardLookUP.Count - 1;
                int currBoardCnt = 1;
                for (int j = 4; j < 143; j++)
                {
                    if (currBoardCnt > BoardsNum) currBoardCnt = 1;
                    if (!SkewNBoardLookUP.ContainsKey("Board" + currBoardCnt.ToString())) continue;
                    if (worksheet.Cells[4, j].Value == null) continue;
                    if (worksheet.Cells[4, j].Value.ToString().ToUpper().StartsWith("EXEC")) break; 
                    if (worksheet.Cells[4, j, 4, j + SkewNBoardLookUP["Board" + currBoardCnt.ToString()] - 1].Merge == true)
                    {
                        currBoardCnt++;
                        continue;
                    }
                    worksheet.Cells[4, j, 4, j + SkewNBoardLookUP["Board"+currBoardCnt.ToString()] - 1].Merge = true;
                    currBoardCnt++;

                }
                package.Save();
            }

        }


        //TODO - implement logic, currently hard coded
        public string getExcelTemplateForProtocol_Table(string protocol, string table, out string message)
        {
            message = "";
            string EmptyExcel = System.Configuration.ConfigurationManager.AppSettings["EmptySRExcel"];
            string ServerDir = System.Configuration.ConfigurationManager.AppSettings["ServerDir"];

            List<string>BoardTypesList = this.CPMetaData.GetBoardTypesList(table);
            List<string>SetUpTypesList = this.CPMetaData.GetSetUpTypesList(table);
            //Template name emptyCR_newTmpl_<N>BoardType_<N>SetupType_<N>Port example:
            //emptyCR_newTmpl_1BoardType_2SetupType_1Port.xlsx
            string template = "emptyCR_newTmpl_" + BoardTypesList.Count.ToString() + "BoardType_" + SetUpTypesList.Count.ToString() + "SetupType_" + SMPConf.excelPorts.Count.ToString() + "Port";
            //Use proje specific template if existst: emptyCR_newTmpl_2BoardType_4SetupType_2Port_HADAS.xlsx
            string templateProj = template + "_" + SMPConf.proj;
            if (File.Exists(Path.Combine(Path.GetDirectoryName(EmptyExcel), Path.GetFileNameWithoutExtension(EmptyExcel).Replace("emptySR", templateProj) + Path.GetExtension(EmptyExcel)))) template = templateProj;

            string newEmptyExcel = Path.Combine(Path.GetDirectoryName(EmptyExcel), Path.GetFileNameWithoutExtension(EmptyExcel).Replace("emptySR", template) + Path.GetExtension(EmptyExcel));
            if (!File.Exists(newEmptyExcel))
            {
                message = "Cannot find matched template,\""+template+"\", report System Admin";
                return "";
            }
            string filename = "emptyCR_" + protocol + "_" + SMPConf.proj + "_" + SMPConf.phase + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            if (SMPConf.separateCSRperTable == "Y") filename = "emptyCR_" + protocol + "_" + SMPConf.proj + "_" + SMPConf.phase + "_" + table + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            string filepath = Path.Combine(ServerDir, "tmp", filename);
            System.IO.File.Copy(newEmptyExcel, filepath, true);
            //prepare lookup dictionary of Skew cell length and Board configuration - lines 3,4 in Excel template
            prepareSkewLookUp(filepath);
            //prepare template, replace constants with actual values
            replaceCellConstantInSheet(filepath, 0, "<Interface>", SMPConf.protocol);
            replaceConstantInSheetName(filepath, 0, "<Interface>", SMPConf.protocol);
            replaceCellConstantInSheet(filepath, 0, "<Project>", SMPConf.proj);
            replaceConstantInSheetName(filepath, 0, "<Project>", SMPConf.proj);
            replaceCellConstantInSheet(filepath, 0, "<Phase>", SMPConf.phase);
            int cnt=1;
            foreach (string BoardType in BoardTypesList)
            {
                replaceCellConstantInSheet(filepath, 0, "<BoardType"+cnt.ToString()+">",BoardType );
                cnt++;
            }
            cnt = 1;
            foreach (string SetUpType in SetUpTypesList)
            {
                replaceCellConstantInSheet(filepath, 0, "<SetUpType" + cnt.ToString() + ">", SetUpType);
                cnt++;
            }
            cnt = 1;
            foreach (string port in SMPConf.excelPorts)
            {
                replaceCellConstantInSheet(filepath, 0, "<Port" + cnt.ToString() + ">", port);
                cnt++;
            }
            
            //TODO: prepare 2nd sheet - "Sessions", is it in use?
            //  need to duplicate table section for CIO
            //  need to replace template constants, rename
            replaceCellConstantInSheet(filepath, 1, "<Interface>", SMPConf.protocol);
            replaceConstantInSheetName(filepath, 1, "<Interface>", SMPConf.protocol);
            replaceCellConstantInSheet(filepath, 1, "<Project>", SMPConf.proj);
            replaceConstantInSheetName(filepath, 1, "<Project>", SMPConf.proj);
            replaceCellConstantInSheet(filepath, 1, "<Phase>", SMPConf.phase);
            string tableName = table;
            if (String.IsNullOrEmpty(tableName)) tableName = SMPConf.table_divs.First().Key;
            replaceCellConstantInSheet(filepath, 1, "<Table>", tableName);
            if ((SMPConf.separateCSRperTable!="Y") && (SMPConf.table_divs.Count > 1))
            {
                int tableCnt = 0;
                foreach (string _tableName in SMPConf.table_divs.Keys)
                {
                    tableCnt++;
                    if (tableCnt > 1) addSessionsTable(filepath, _tableName, tableCnt);
                }
            }

            return filepath;
        }
        private void addSessionsTable(string filepath,string tableName, int tableCnt)
        {
            //Copy paste "sessions" table; find its size, searching "Total Results" string and add 1 for separatore line
            FileInfo newFile = new FileInfo(filepath);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(1);
                bool notFound = true;
                int cnt = 1;
                int TotalResults = 0;
                while (notFound)
                {
                    cnt++;
                    if ((worksheet.Cells[cnt, 2].Value != null) && (worksheet.Cells[cnt, 2].Value.ToString() == "Total Results"))
                    {
                        TotalResults = cnt;
                        notFound = false;
                    }
                    if (cnt > 10000) notFound = false;
                }
                if (TotalResults == 0) TotalResults = 36;//assuming sessions table is from 1-36 (???)
                int lineHeaderRemove = 0;
                lineHeaderRemove = (tableCnt -2)*3;

                ExcelRangeBase s = worksheet.Cells[(TotalResults+1) * (tableCnt - 1) + 1-lineHeaderRemove , 1];
                worksheet.Cells[getRangeSTR("A", 4, "M", (TotalResults + 1))].Copy(s);
                worksheet.Cells[s.Address].Value = tableName;
                package.Save();
            }
        }
        
        private string getRangeSTR(string S_col, int S_ind, string T_col, int T_ind)
        {
            return S_col + S_ind.ToString() + ":" + T_col + T_ind.ToString();
        }


        private void replaceCellConstantInSheet(string excelFileName, int sheetIndex, string cellExistVal, string cellNewVal)
        {
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(sheetIndex);
                //if (!String.IsNullOrEmpty(sheetName)) worksheet = package.Workbook.Worksheets[sheetName]; //depricated - using sheetIndex instead
                var query3 = (from cell in worksheet.Cells["0:0"]
                              where cell.Value != null && cell.Value.ToString().Contains(cellExistVal)
                              select cell);
                foreach (var cell in query3)
                {
                    cell.Value = cell.Value.ToString().Replace(cellExistVal, cellNewVal);
                }
                //worksheet.Cells.AutoFitColumns(0);  //autofit columns for all cells
                package.Save();
            }
        }

        private void replaceConstantInSheetName(string excelFileName, int sheetIndex, string ExistVal, string NewVal)
        {
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(sheetIndex);
                worksheet.Name = worksheet.Name.Replace(ExistVal, NewVal);
                package.Save();
            }
        }
        //assuming Units are ordered same skew 1,2,3
        private int getNewUnitPos(string NewUnit)
        {
            string matchedExistUnit = Regex.Replace(NewUnit,@"\d+","1");
            string prefix = Regex.Replace(NewUnit,@"\d+","");
            prefix = prefix.TrimEnd('_');
            int ind = ExistUnits.IndexOf(matchedExistUnit);
            while ((ind<ExistUnits.Count) && (ExistUnits[ind].StartsWith(prefix.Substring(0,1)))) ind++;
            return ind;
        }

        private int getNewUnitRef(string NewUnit)
        {
            string matchedExistUnit = Regex.Replace(NewUnit, @"\d+", "1");
            return ExistUnits.IndexOf(matchedExistUnit);
        }

        //assuming Units are ordered same skew 1,2,3
        private void pushNewUnit(string NewUnit)
        {
            int pos = getNewUnitPos(NewUnit);
            List<string> tmpExistUnits  = new List<string>();
            for (int i = 0; i < pos; i++) tmpExistUnits.Add(ExistUnits[i]);
            tmpExistUnits.Add(NewUnit);
            for (int i = pos; i < ExistUnits.Count; i++) tmpExistUnits.Add(ExistUnits[i]);
            ExistUnits = tmpExistUnits;
        }


        private XElement CreateConfXML()
        {
            // Create doc with root node
            XElement ProtocolConf = new XElement("ProtocolConf");
            XDocument doc = new XDocument(ProtocolConf);
            ProtocolConf.Add(new XElement("protocol", SMPConf.protocol));
            ProtocolConf.Add(new XElement("phase", SMPConf.phase));
            ProtocolConf.Add(new XElement("proj", SMPConf.proj));
            foreach (string table in this.SMPConf.tables)
            {
                ProtocolConf.Add(new XElement("table", table));
            }
            int cntFields = 0;
            foreach (string field in this.SMPConf.fields)
            {
                XElement fieldElem = new XElement("field",field);
                fieldElem.Add(new XAttribute("alias", this.SMPConf.fieldsAliases[cntFields]));
                ProtocolConf.Add(fieldElem);
                cntFields++;
            }

            if (SMPConf.corners.Count > 0) ProtocolConf.Add(createStrCommaDelXElementOfList(SMPConf.corners,"corners"));
            ProtocolConf.Add(createStrCommaDelXElementOfList(SMPConf.ports, "ports"));
            ProtocolConf.Add(createStrCommaDelXElementOfList(SMPConf.excelPorts, "excelPorts"));
            ProtocolConf.Add(createStrCommaDelXElementOfList(SMPConf.lanes, "lanes"));
            ProtocolConf.Add(new XElement("SRTemplate", SMPConf.SRTemplate));
            if (!String.IsNullOrEmpty(SMPConf.separateCSRperTable)) ProtocolConf.Add(new XElement("separateCSRperTable", SMPConf.separateCSRperTable));
            if (!String.IsNullOrEmpty(SMPConf.useNewCSR)) ProtocolConf.Add(new XElement("useNewCSR", SMPConf.useNewCSR));
            
            foreach (var TableTDVpair in SMPConf.table_divs)
            {
                foreach (var TDVpair in TableTDVpair.Value)
                {
                    XElement table_div = new XElement("table_div");
                    table_div.Add(new XAttribute("tdname", TDVpair.Key));
                    table_div.Add(new XAttribute("tname", TDVpair.Value.table_name));
                    table_div.Add(new XAttribute("SRreportId", TDVpair.Value.SRreportId));
                    foreach (var TDLpair in TDVpair.Value.TableDivLines)
                    {
                        XElement table_div_line = new XElement("table_div_line");
                        table_div_line.Add(new XAttribute("dname", TDLpair.Key));
                        table_div_line.Add(new XAttribute("trials", TDLpair.Value.trials));
                        table_div_line.Add(new XAttribute("DivNumOfTests", TDLpair.Value.numOfTests));
                        foreach (TableDivField TDField in TDLpair.Value.tableDivFields)
                        {
                            XElement table_div_field = new XElement("table_div_field", createStrCommaDelOfList(TDField.values));
                            table_div_field.Add(new XAttribute("fname", TDField.name));
                            table_div_field.Add(new XAttribute("condition", TDField.condition));
                            table_div_field.Add(new XAttribute("isSpecSubSet", TDField.isSpecSubSet.ToString()));
                            table_div_line.Add(table_div_field);
                        }
                        table_div.Add(table_div_line);
                    }
                    ProtocolConf.Add(table_div);
                }
            }

            XElement units = new XElement("units");
            foreach (Unit unit in SMPConf.units)
            {
                XElement unitElem = new XElement("unit");
                unitElem.Add(new XElement("skew", unit.skew));
                unitElem.Add(new XElement("type", unit.type));
                unitElem.Add(new XElement("setup", unit.setup));
                unitElem.Add(new XElement("num", unit.num));
                units.Add(unitElem);
            }
            ProtocolConf.Add(units);


            foreach (Mask mask in SMPConf.masks)
            {
                XElement maskElem = new XElement("mask");
                maskElem.Add(new XElement("rule", mask.RuleItem+"=" + mask.RuleAction));
                maskElem.Add(new XElement("exclude", mask.ExcludeItem+"=" + mask.ExcludeDetails));
                if (!String.IsNullOrEmpty(mask.table)) maskElem.Add(new XElement("table", mask.table));
                ProtocolConf.Add(maskElem);
            }

            return ProtocolConf;
        }

        private XElement createStrCommaDelXElementOfList(List<string> ListObj ,string XElementName)
        {
            string Str = "";
            foreach (string s in ListObj)
            {
                Str += s + ",";
            }
           return (new XElement(XElementName, Str.TrimEnd(',')));
        }

        private string createStrCommaDelOfList(List<string> ListObj)
        {
            string Str = "";
            foreach (string s in ListObj)
            {
                Str += s + ",";
            }
           return  Str.TrimEnd(',');
        }

        public void pocWriteConfSheet(string excelFileName, XElement ConfXML)
        {
            FileInfo newFile = new FileInfo(excelFileName);

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(2);
                worksheet.Cells[1, 1].Value = ConfXML.ToString();
                package.Save();
            }

        }

        public XElement pocReadConfSheet(string excelFileName)
        {
            FileInfo newFile = new FileInfo(excelFileName);

            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(2);
                XDocument doc = XDocument.Parse(worksheet.Cells[1, 1].Value.ToString());
                XElement ProtocolConf = doc.Root;
                return ProtocolConf;
            }

        }

        private void prepareSkewLookUp(string excelFileName)
        {
            if (SkewNBoardLookUP == null) SkewNBoardLookUP = new Dictionary<string, int>();
            FileInfo newFile = new FileInfo(excelFileName);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.ElementAt(0);
                //go over cells in line 3 from col 4 till null and put value in "Skew"
                string firstSkew = worksheet.Cells[3, 4].Value.ToString();
                int SkewLenCnt = 1;
                string nextSkew=null;
                while (string.IsNullOrEmpty(nextSkew))
                {
                    SkewLenCnt++;
                    if (worksheet.Cells[3, 4 + SkewLenCnt].Value != null) nextSkew = worksheet.Cells[3, 4 + SkewLenCnt].Value.ToString();
                }
                SkewNBoardLookUP.Add("Skew", SkewLenCnt);
                //go over cells in line 4 from col 4 till Skew ends, and count boards lengths into Board1, Board2 etc
                string board = worksheet.Cells[4, 4].Value.ToString();
                int BoardsCnt = 1;
                int lastBoardInd=0;
                for (int i = 1; i <= SkewLenCnt; i++)
                {
                    if (worksheet.Cells[4, 4 + i].Value != null)
                    {
                        SkewNBoardLookUP.Add("Board" + BoardsCnt.ToString(), i - lastBoardInd);
                        lastBoardInd = i;
                        BoardsCnt++;
                    }
                }
                if (lastBoardInd < SkewLenCnt) SkewNBoardLookUP.Add("Board" + BoardsCnt.ToString(), SkewLenCnt - lastBoardInd);

            }
        }
    }

} 