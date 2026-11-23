using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Word = Microsoft.Office.Interop.Word;
using System.IO;
using System.Text.RegularExpressions;
using CCD_EV_ManageDB.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Web.UI;
using System.Web.Routing;

namespace CCD_EV_ManageDB.Models
{
    public class CompWordReportHandler
    {
        public CompWordReportHandler()
        {
            //MDBStationResults MDBStationResults = new MDBStationResults("DB_CMP", Report.RConfig.table);
        }
        public void ExecDemoReport(string DUT, string query, string file, out bool errorOccurred, out string errorMsg) 
        {

            errorOccurred = false;
            errorMsg = String.Empty;

            object missing = Type.Missing;
            Word._Application objApp;
            Word._Document objDoc;

            objApp = new Word.Application();
            objApp.Visible = false;
            objDoc = objApp.Documents.Open(file);


            //replace something in the header:
            foreach (Word.Section section in objDoc.Sections)
            {
                Word.Range headerRange = section.Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range;
                Word.Find findObject = headerRange.Find;
                findObject.ClearFormatting();
                findObject.Text = "DBrpl-DUT-DBend";
                findObject.Replacement.ClearFormatting();
                findObject.Replacement.Text = DUT;
                object replaceAll = Word.WdReplace.wdReplaceAll;
                findObject.Execute(ref missing, ref missing, ref missing, ref missing, ref missing,
                        ref missing, ref missing, ref missing, ref missing, ref missing,
                        ref replaceAll, ref missing, ref missing, ref missing, ref missing);
            }


            try
            {


                //////////////////////////////////////////////
                //NEW: Pictures try
                Word.InlineShape shape;
                Word.Range Prng = objDoc.Content;
                Prng.SpellingChecked = true;
                Prng.NoProofing = 1;

                Dictionary<string, DBTmpl> DBTemplates = (new MDBCompReportLines()).GetCompReportLinesDictionary();
                foreach (Word.Table table in objDoc.Tables)
                {
                    //Take table from text in the doc
                    if (!table.Cell(1, 1).Range.Text.ToString().StartsWith("DBimg")) continue;
                    string pattern = @"(DBimg-)(.*)(-DBend)(.*)";
                    Match match = Regex.Match(table.Cell(1, 1).Range.Text.ToString(), pattern);
                    //string dbTable = match.Groups[2].Value;
                    string tmp = match.Groups[4].Value;
                    tmp = Regex.Replace(tmp, @"\r", ""); // remove line feed \r since Range.text assignment adds l line feed (?)
                    table.Cell(1, 1).Range.Text = tmp;
                    //MDBStationResults MDBStationResults = new MDBStationResults("DB_CMP", dbTable);
                    //--------------------------------------------------------------

                    //string pattern = @"(DBrpl-)(.*)(-DBend)(.*)";
                    //Match match;
                    //Take table from DB templates
                    MDBStationResults MDBStationResults = null;
                    string dbTable = "";
                    DBTmpl lineTmpl;

                    int _rows = table.Rows.Count;
                    for (int r = 1; r < _rows + 1; r++)
                    {
                        int _cols = table.Rows[r].Cells.Count;
                        if ((!String.IsNullOrWhiteSpace(table.Cell(r, 1).Range.Text.ToString())) && (table.Cell(r, 1).Range.Text.ToString().StartsWith("DBimg")) && (_cols == 2))
                        {
                            for (int i = 1; i < 3; i++)
                            {
                                match = Regex.Match(table.Cell(r, i).Range.Text.ToString(), pattern);
                                if (DBTemplates.ContainsKey(match.Groups[2].Value))
                                {
                                    lineTmpl = DBTemplates[match.Groups[2].Value];
                                    if (dbTable != lineTmpl.table)
                                    {
                                        dbTable = lineTmpl.table;
                                        MDBStationResults = new MDBStationResults("DB_CMP", dbTable);
                                    }

                                    string imgPath = MDBStationResults.FindCompImg(query, lineTmpl);
                                    if (String.IsNullOrWhiteSpace(imgPath)) continue;

                                    Prng.Find.ClearFormatting();
                                    Prng.Find.Forward = true;
                                    Prng.Find.Text = match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value;

                                    Prng.Find.Execute();

                                    while (Prng.Find.Found)
                                    {
                                        Prng.Text = String.Empty;
                                        Prng.Select();
                                        //shape = objApp.Selection.InlineShapes.AddPicture(@"C:\Users\nshemer\Documents\CCD\Work\Compliance\eye2.jpg");
                                        shape = objApp.Selection.InlineShapes.AddPicture(imgPath);
                                        shape.Width = 264;
                                        shape.Height = 155;
                                        Prng.Find.Execute();
                                    }
                                    ///////////////////
                                    ////table.Cell(r, i).Range.Delete();
                                    //table.Cell(r, i).Range.Text = String.Empty;
                                    //table.Cell(r, i).Range.Select();
                                    ////string img = MDBStationResults.FindCompIMG(query, lineTmpl);
                                    ////shape = objApp.Selection.InlineShapes.AddPicture(img);
                                    //shape = objApp.Selection.InlineShapes.AddPicture(@"C:\Users\nshemer\Documents\CCD\Work\Compliance\eye2.jpg");
                                    //shape.Width = 264;
                                    //shape.Height = 155;
                                }
                            }
                        }
                    }
                }
                /////////////////////////////////////////////
                //Dictionary<string, DBTmpl> DBTemplates = (new MDBCompReportLines()).GetCompReportLinesDictionary();
                foreach (Word.Table table in objDoc.Tables)
                {
                    //Take table from text in the doc
                    if (!table.Cell(1, 1).Range.Text.ToString().StartsWith("DBrpl")) continue;
                    string pattern = @"(DBrpl-)(.*)(-DBend)(.*)";
                    Match match = Regex.Match(table.Cell(1, 1).Range.Text.ToString(), pattern);
                    //string dbTable = match.Groups[2].Value;
                    string tmp = match.Groups[4].Value;
                    tmp = Regex.Replace(tmp, @"\r", ""); // remove line feed \r since Range.text assignment adds l line feed (?)
                    table.Cell(1, 1).Range.Text = tmp;
                    //MDBStationResults MDBStationResults = new MDBStationResults("DB_CMP", dbTable);
                    //--------------------------------------------------------------

                    //string pattern = @"(DBrpl-)(.*)(-DBend)(.*)";
                    //Match match;
                    //Take table from DB templates
                    MDBStationResults MDBStationResults = null;
                    string dbTable = "";
                    DBTmpl lineTmpl;

                    int _rows = table.Rows.Count;
                    for (int r = 1; r < _rows + 1; r++)
                    {
                        int _cols = table.Rows[r].Cells.Count;
                        if ((!String.IsNullOrWhiteSpace(table.Cell(r, 2).Range.Text.ToString())) && (table.Cell(r, 2).Range.Text.ToString().StartsWith("DBrpl")))
                        {
                            match = Regex.Match(table.Cell(r, 2).Range.Text.ToString(), pattern);
                            if (DBTemplates.ContainsKey(match.Groups[2].Value))
                            {
                                tmp = match.Groups[4].Value;
                                tmp = Regex.Replace(tmp, @"(.*)\r", "$1"); // remove line feed \r since Range.text assignment adds l line feed (?)
                                table.Cell(r, 2).Range.Text = tmp.TrimStart();
                                lineTmpl = DBTemplates[match.Groups[2].Value];
                                if (dbTable != lineTmpl.table)
                                {
                                    dbTable = lineTmpl.table;
                                    MDBStationResults = new MDBStationResults("DB_CMP", dbTable);
                                }
                            }
                            else continue;
                            //pull results and produce output according to specific line logic
                            List<string> results = MDBStationResults.FindCompLine(query, lineTmpl);
                            table.Cell(r, 4).Range.Text = results[0];
                            table.Cell(r, 4).Range.Font.ColorIndex = Word.WdColorIndex.wdBrightGreen;
                            if (results[0] == "Fail") table.Cell(r, 4).Range.Font.ColorIndex = Word.WdColorIndex.wdRed;
                            int cnt = 1;
                            int ColsToDisp = results.Count + 4;
                            for (int c = 5; c < ColsToDisp; c++)
                            {

                                table.Cell(r, c).Range.Text = results[cnt];
                                if (results[0] == "Fail") table.Cell(r, c).Range.Font.ColorIndex = Word.WdColorIndex.wdRed;
                                cnt++;
                            }
                        }
                    }
                }


                /////////New Presets start ////////////
                /////////////////////////////////////////////
                foreach (Word.Table table in objDoc.Tables)
                {
                    //Take table from text in the doc
                    if (!table.Cell(1, 1).Range.Text.ToString().StartsWith("DBpreset")) continue;
                    string pattern = @"(DBpreset-)(.*)(-DBend)(.*)";
                    Match match = Regex.Match(table.Cell(1, 1).Range.Text.ToString(), pattern);
                    string tmp = match.Groups[4].Value;
                    tmp = Regex.Replace(tmp, @"\r", ""); // remove line feed \r since Range.text assignment adds l line feed (?)
                    table.Cell(1, 1).Range.Text = tmp;

                    //Take table from DB templates
                    BsonDocument InvDoc = new MDBInventory().ReadInventory(DUT).toBsonDocument();


                    int _rows = table.Rows.Count;
                    pattern = @"(DBrpl-)(.*)(-DBend)(.*)";
                    for (int r = 2; r < _rows + 1; r++)
                    {
                        int _cols = table.Rows[r].Cells.Count;
                        for (int c = 2; c < _cols + 1; c++)
                        {
                            if ((!String.IsNullOrWhiteSpace(table.Cell(r, c).Range.Text.ToString())) && (table.Cell(r, c).Range.Text.ToString().StartsWith("DBrpl")))
                            {
                                match = Regex.Match(table.Cell(r, c).Range.Text.ToString(), pattern);
                                table.Cell(r, c).Range.Text = InvDoc.GetValue((match.Groups[2].Value), "").ToString();
                            }
                        }
                    }
                }
                /////////New Presets end////////////
                //find & replace in the table range example:
                //Word.Range Trng = objDoc.Tables[6].Range;
                //Trng.Find.ClearFormatting();
                //Trng.Find.Execute("kuku");

                //cell.Range.Text = "kuku";
                //cell.Range.Select();


                //find & replace example:
                //int intFound = 0;
                //Word.Range rng = objDoc.Content;

                //rng.Find.ClearFormatting();
                //rng.Find.Forward = true;
                //rng.Find.Text = "find me";

                //rng.Find.Execute();

                //while (rng.Find.Found)
                //{
                //    rng.Text = "Replaced";
                //    rng.Select();
                //    intFound++;
                //    rng.Find.Execute();
                //}

                ////Add text at the end example:
                //object objEndOfDocFlag = "\\endofdoc"; /* \endofdoc is a predefined bookmark */
                //Word.Range objWordRng = objDoc.Bookmarks.get_Item(ref objEndOfDocFlag).Range; //go to end of document
                //objWordRng.InsertAfter("THIS IS THE SIMPLE WORD DEMO : THANKS YOU.");

                objDoc.SpellingChecked = true;
                objDoc.GrammarChecked = true;
                objDoc.ShowGrammaticalErrors = false;
                objDoc.ShowSpellingErrors = false;
                
                objApp.Documents.Save();
                objApp.Documents.Close();
                objApp.Quit();
            }
            catch (Exception e)
            {
                objApp.Documents.Close();
                objApp.Quit();
                errorOccurred = true;
                errorMsg = e.Message;
            }

        }

        public List<BsonDocument> Validate(string query, string templateFile ,string interfacesStr, out bool errorOccurred, out string errorMsg)
        {

            errorOccurred = false;
            errorMsg = String.Empty;

            List<BsonDocument> MissingResults = new List<BsonDocument>();

            object missing = Type.Missing;
            Word._Application objApp;
            Word._Document objDoc;

            objApp = new Word.Application();
            objApp.Visible = false;
            objDoc = objApp.Documents.Open(templateFile);

            try
            {

                Dictionary<string, DBTmpl> DBTemplates = (new MDBCompReportLines()).GetCompReportLinesDictionary();
                foreach (Word.Table table in objDoc.Tables)
                {
                    //Take table from text in the doc
                    if (!table.Cell(1, 1).Range.Text.ToString().StartsWith("DBimg")) continue;
                    string pattern = @"(DBimg-)(.*)(-DBend)(.*)";

                    //new: filter requested interfaces:
                    //string interfacesStr = "DP_TX,USB3p1_TX";//TODO get as input...
                    List<string>InterfacesList= new List<string>(interfacesStr.Split(','));
                    Match tmatch = Regex.Match(table.Cell(1, 1).Range.Text.ToString(), pattern);
                    string dbTable = tmatch.Groups[2].Value;
                    if (!InterfacesList.Contains(dbTable)) continue;
                    

                    Match match;// = Regex.Match(table.Cell(1, 1).Range.Text.ToString(), pattern)
                    string tmp;// = match.Groups[4].Value;
                    //tmp = Regex.Replace(tmp, @"\r", ""); // remove line feed \r since Range.text assignment adds l line feed (?)
                    //table.Cell(1, 1).Range.Text = tmp;

                    MDBStationResults MDBStationResults = null;
                    //string dbTable = "";
                    dbTable = "";
                    DBTmpl lineTmpl;

                    int _rows = table.Rows.Count;
                    for (int r = 1; r < _rows + 1; r++)
                    {
                        int _cols = table.Rows[r].Cells.Count;
                        if ((!String.IsNullOrWhiteSpace(table.Cell(r, 1).Range.Text.ToString())) && (table.Cell(r, 1).Range.Text.ToString().StartsWith("DBimg")) && (_cols == 2))
                        {
                            for (int i = 1; i < 3; i++)
                            {
                                match = Regex.Match(table.Cell(r, i).Range.Text.ToString(), pattern);
                                if (DBTemplates.ContainsKey(match.Groups[2].Value))
                                {
                                    lineTmpl = DBTemplates[match.Groups[2].Value];
                                    if (dbTable != lineTmpl.table)
                                    {
                                        dbTable = lineTmpl.table;
                                        MDBStationResults = new MDBStationResults("DB_CMP", dbTable);
                                    }

                                    BsonDocument doc = MDBStationResults.ValidateCompImg(query, lineTmpl);
                                    if (doc != null) MissingResults.Add(doc);;
                                }
                                else
                                {
                                    //report Error Template not defined
                                    BsonDocument doc = new BsonDocument();
                                    doc.Set("Template", match.Groups[2].Value);
                                    doc.Set("Desc", match.Groups[4].Value);
                                    doc.Set("Problem", "Template is not defined in DB");
                                    MissingResults.Add(doc);
                                }
                            }
                        }
                    }
                }

                foreach (Word.Table table in objDoc.Tables)
                {
                    if (!table.Cell(1, 1).Range.Text.ToString().StartsWith("DBrpl")) continue;
                    string pattern = @"(DBrpl-)(.*)(-DBend)(.*)";
                    //new: filter requested interfaces:
                    //string interfacesStr = "DP_TX,USB3p1_TX,DPpp_TX";//TODO get as input...
                    List<string> InterfacesList = new List<string>(interfacesStr.Split(','));
                    Match tmatch = Regex.Match(table.Cell(1, 1).Range.Text.ToString(), pattern);
                    string dbTable = tmatch.Groups[2].Value;
                    if (!InterfacesList.Contains(dbTable)) continue;

                    Match match;// = Regex.Match(table.Cell(1, 1).Range.Text.ToString(), pattern);

                    MDBStationResults MDBStationResults = null;
                    //string dbTable = "";
                    dbTable = "";
                    DBTmpl lineTmpl;

                    int _rows = table.Rows.Count;
                    for (int r = 1; r < _rows + 1; r++)
                    {
                        int _cols = table.Rows[r].Cells.Count;
                        if ((!String.IsNullOrWhiteSpace(table.Cell(r, 2).Range.Text.ToString())) && (table.Cell(r, 2).Range.Text.ToString().StartsWith("DBrpl")))
                        {
                            match = Regex.Match(table.Cell(r, 2).Range.Text.ToString(), pattern);
                            if (DBTemplates.ContainsKey(match.Groups[2].Value))
                            {
                                //check NA/Multi..
                                lineTmpl = DBTemplates[match.Groups[2].Value];
                                if (dbTable != lineTmpl.table)
                                {
                                    dbTable = lineTmpl.table;
                                    MDBStationResults = new MDBStationResults("DB_CMP", dbTable);
                                }

                                List<BsonDocument> valResults = MDBStationResults.ValidateCompLine(query, lineTmpl);//each result: Field,Item,Problem,Query
                                if (valResults == null) continue;
                                else
                                {
                                    //report Error NA/Multi
                                    foreach (BsonDocument valREsult in valResults)
                                    {
                                        valREsult.Set("Number", table.Cell(r, 1).Range.Text.ToString());
                                        valREsult.Set("Template", match.Groups[2].Value);
                                        valREsult.Set("Desc", match.Groups[4].Value);
                                        MissingResults.Add(valREsult);
                                    }
                                }
                            }
                            else
                            {
                                //report Error Template not defined
                                BsonDocument doc = new BsonDocument();
                                doc.Set("Number", table.Cell(r, 1).Range.Text.ToString());
                                doc.Set("Template", match.Groups[2].Value);
                                doc.Set("Desc", match.Groups[4].Value);
                                doc.Set("Problem", "Template is not defined in DB");
                                MissingResults.Add(doc);

                            }
                        }
                    }
                }
            }


            catch (Exception e)
            {
                errorOccurred = true;
                errorMsg = e.Message;
            }
            finally
            {
                objApp.Documents.Close();
                objApp.Quit();
            }
           return MissingResults;
        }

        public void writeValidationHTML(string DUT, string templ ,string vTableStr, List<BsonDocument> results, string filePath,string baseUrl, out bool errorOccurred, out string errorMsg)
        {
            errorOccurred = false;
            errorMsg = String.Empty;

            StringWriter stringWriter = new StringWriter();
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Html);
                writer.RenderBeginTag(HtmlTextWriterTag.Head);
                writer.AddAttribute(HtmlTextWriterAttribute.Href, "http://ccd-ev-data-int.iil.intel.com/Content/Site.css");
                writer.AddAttribute(HtmlTextWriterAttribute.Rel, "stylesheet");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/css");
                writer.RenderBeginTag(HtmlTextWriterTag.Link);
                writer.RenderEndTag();
                writer.RenderEndTag();
                writer.RenderBeginTag(HtmlTextWriterTag.Body);
                if (results.Count > 0)
                {
                    //<p>Following missing results for UnitId: @ViewBag.UnitID Template: @ViewBag.Template (@MissedResults.Count results)</p>
                    writer.RenderBeginTag(HtmlTextWriterTag.P);
                    //writer.Write("Following missing results for : "+ DUT + " Template: " +templ+ " Interfaces: " + vTableStr + " ("+results.Count+" results)");
                    writer.Write("Following missing results for : ");
                    writer.RenderBeginTag(HtmlTextWriterTag.B);
                    writer.Write(DUT);
                    writer.RenderEndTag();
                    writer.Write(" Template: ");
                    writer.RenderBeginTag(HtmlTextWriterTag.B);
                    writer.Write(templ);
                    writer.RenderEndTag();
                    writer.Write(" Interfaces: ");
                    writer.RenderBeginTag(HtmlTextWriterTag.B);
                    writer.Write(vTableStr);
                    writer.RenderEndTag();
                    writer.Write(" (" + results.Count + " results)");
                    writer.RenderEndTag();
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "display");
                    writer.RenderBeginTag(HtmlTextWriterTag.Table);
                    writer.RenderBeginTag(HtmlTextWriterTag.Thead);
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    foreach (string col in new string[] { "Number", "Desc", "Template", "Field", "Item", "Problem", "Query" })
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        writer.Write(col);
                        writer.RenderEndTag();
                    }
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                    writer.RenderBeginTag(HtmlTextWriterTag.Tbody);
                    foreach (BsonDocument doc in results)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        foreach (string col in new string[] { "Number", "Desc", "Template", "Field", "Item", "Problem", "Query" })
                        {
                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            if (col == "Query")
                            {
                                string link = baseUrl + "ManageDB/SearchCOMP?db=comp&col=" + doc.GetValue("Table", "") + "&Query=" + doc.GetValue(col, "");
                                writer.AddAttribute(HtmlTextWriterAttribute.Href, link);
                                writer.AddAttribute(HtmlTextWriterAttribute.Target, "query");
                                writer.RenderBeginTag(HtmlTextWriterTag.A);
                                writer.Write(doc.GetValue(col, ""));
                                writer.RenderEndTag();
                            }
                            else writer.Write(doc.GetValue(col, ""));
                        }
                        writer.RenderEndTag();
                    }
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                }
                else
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.P);
                    writer.Write("Results are complete for : " + DUT + " Template:" + templ );
                    writer.RenderEndTag();
                }
                writer.RenderEndTag();
                writer.RenderEndTag();
                System.IO.File.WriteAllText(filePath, stringWriter.ToString()); 
            }


        }


    }


}