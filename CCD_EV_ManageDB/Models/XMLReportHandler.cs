using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq; 
using System.Web;
using System.IO;
using System.Text.RegularExpressions;

namespace CCD_EV_ManageDB.Models
{
    public class XMLReportHandler
    {
        private static string RGTemplates { get { return System.Configuration.ConfigurationManager.AppSettings["RGTemplates"]; } }
        private static string RGPluginVer { get { return System.Configuration.ConfigurationManager.AppSettings["RGPluginVer"]; } }

        public XMLReportHandler()
        {
            
        }
        public List<string> ParseTemplatesXML()
        {
            List<string> Templates = new List<string>();
            XDocument doc = XDocument.Load(RGTemplates);
            foreach (XElement xe in doc.Descendants("Template"))
            {
                Templates.Add(xe.Attribute("key").Value);
            }
            return Templates;
        }

        public HashSet<string> ParseTemplateColsXML(string tmpl)
        {
            HashSet<string> Cols = new HashSet<string>();
            XDocument doc = XDocument.Load(RGTemplates);

            foreach (XElement tmplElem in doc.Descendants("Template"))
            {
                if (tmplElem.Attribute("key").Value == tmpl)
                {
                    foreach (XElement colElem in tmplElem.Descendants())
                    {
                        if (colElem.Name=="Column") Cols.Add(colElem.Value);
                    }
                }              
            }
            return Cols;
        }

        public void CreateRGConfigXML(string path, ReportConfig RC, string FieldSlidesStr, bool useMarginLimit)
        {
            List<string> FieldSlidesList = new List<string>();
            if (!string.IsNullOrEmpty(FieldSlidesStr))
            {
                FieldSlidesList = new List<string>(FieldSlidesStr.Split(','));
            }
            XDocument doc = new XDocument(
                new XElement("ReportConfig"));

                //new XAttribute("name", "value"),
            XElement Resources = new XElement("Resources");
            XElement ExternalTemplate = new XElement("ExternalTemplate");
            //XAttribute src = new XAttribute("src", @"\\ccdpc076-it\c$\RG\rg_templates_Naty.xml");
            string RGTemplates = System.Configuration.ConfigurationManager.AppSettings["RGTemplates"];
            XAttribute src = new XAttribute("src",Path.Combine(Path.GetDirectoryName(path), Path.GetFileName(RGTemplates)));
            ExternalTemplate.Add(src);
            Resources.Add(ExternalTemplate);
            doc.Root.Add(Resources);

            // Create a root node
            XElement ReportContent = new XElement("ReportContent");
            XElement tSlide = new XElement("Slide");
            tSlide.Add(new XAttribute("type", "GENERAL"));
            foreach (TitleItem ti in RC.TitleItems)
            {
                XElement TextBlock = new XElement("TextBlock",ti.content);
                TextBlock.Add(new XAttribute("top", ti.top));
                TextBlock.Add(new XAttribute("left", "50"));
                TextBlock.Add(new XAttribute("width", "600"));
                TextBlock.Add(new XAttribute("height", ti.height));
                TextBlock.Add(new XAttribute("fontSize", ti.fontSize));
                TextBlock.Add(new XAttribute("foreground", "Black"));
                TextBlock.Add(new XAttribute("alignment", "left"));
                TextBlock.Add(new XAttribute("fontFamily", "Calibri"));
                TextBlock.Add(new XAttribute("isMultiLine", "True"));
                tSlide.Add(TextBlock);
            }
            ReportContent.Add(tSlide);
            XElement sSlide = new XElement("Slide");
            sSlide.Add(new XAttribute("type", "SUMMARY"));
            sSlide.Add(new XElement("Title", "Summary"));
            ReportContent.Add(sSlide);
            int Fcnt = 0;
            foreach (FieldSlide fs in RC.FieldsSlides)
            {
                if ((FieldSlidesList.Count == 0) || (FieldSlidesList.Contains(Fcnt.ToString())))
                {
                    XElement Slide = new XElement("Slide");
                    Slide.Add(new XAttribute("type", "GENERAL"));
                    Slide.Add(new XAttribute("templateKey", fs.templateKey));
                    if (fs.type == General.ReportSlideType.limits.ToString())
                    {
                        XElement Source = new XElement("Source");
                        Source.Add(new XAttribute("id", "1"));
                        string _TestName = fs.test;
                        //31 excle limit
                        if (_TestName.Length > 15) _TestName = FieldSlide.GetLimit31TestName(RC.table, _TestName);
                        else
                        {
                            _TestName = Regex.Replace(_TestName, "\\W", "");
                            _TestName = Regex.Replace(_TestName, "_", "");
                        }
                        Source.Add(new XAttribute("test", _TestName));
                        Source.Add(new XAttribute("column", fs.column));
                        if (string.IsNullOrWhiteSpace(fs.text)) Source.Add(new XAttribute("nameInReport", fs.test + " - " + fs.column));
                        else Source.Add(new XAttribute("nameInReport", fs.text));
                        //New: Margin
                        if ((useMarginLimit) && (!string.IsNullOrEmpty(fs.getMarginForReportConf())))
                        {
                            Source.Add(new XAttribute("margin", fs.getMarginForReportConf()));
                        }
                        Slide.Add(Source);
                    }
                    if (fs.type == General.ReportSlideType.image.ToString())
                    {
                        //Images header
                        XElement Source0 = new XElement("Source");
                        Source0.Add(new XAttribute("id", "0"));
                        string _TestName = fs.test;
                        //31 excle limit
                        if (_TestName.Length > 15) _TestName = FieldSlide.GetLimit31TestName(RC.table, _TestName);
                        else
                        {
                            _TestName = Regex.Replace(_TestName, "\\W", "");
                            _TestName = Regex.Replace(_TestName, "_", "");
                        }
                        Source0.Add(new XAttribute("test", _TestName));
                        Source0.Add(new XAttribute("column", fs.column));
                        if (string.IsNullOrWhiteSpace(fs.text)) Source0.Add(new XAttribute("text", _TestName + " - Images"));
                        else Source0.Add(new XAttribute("text", fs.text));
                        Slide.Add(Source0);
                        foreach (ImageDetails imgDetails in RC.GetSlideImageDetails(fs.imageDetailsID))
                        {
                            XElement Source = new XElement("Source");
                            Source.Add(new XAttribute("id", imgDetails.order.ToString()));
                            Source.Add(new XAttribute("test", _TestName));
                            Source.Add(new XAttribute("column", imgDetails.field + imgDetails.order.ToString()));
                            if (string.IsNullOrWhiteSpace(imgDetails.text)) Source.Add(new XAttribute("text", fs.test + " - " + imgDetails.field + imgDetails.order.ToString()));
                            else Source.Add(new XAttribute("text", imgDetails.text));
                            Slide.Add(Source);
                        }
                    }
                    ReportContent.Add(Slide);
                }
                Fcnt++;
            }
           
            doc.Root.Add(ReportContent);
            doc.Save(path);
        }

        public void CreateTicketXML(string _ticketPath,string _LimitsFilePath,string _ConfigFilePath,string _ReportFileName,ReportConfig RC)
        {
            string dirPath = Path.GetDirectoryName(_ticketPath);
            // Create a root node
            XDocument doc = new XDocument(
                new XElement("Ticket"));

            XElement Plugin = new XElement("Plugin");
            Plugin.Add(new XAttribute("name", "ReportGenerator"));
            Plugin.Add(new XAttribute("version", RGPluginVer));
            XElement PluginInput = new XElement("PluginInput");
            XElement ReportGeneratorInput = new XElement("ReportGeneratorInput");
            ReportGeneratorInput.Add(new XElement("RawDataPath", Path.Combine(dirPath, "RawData")));
            ReportGeneratorInput.Add(new XElement("LimitsFilePath",_LimitsFilePath));
            ReportGeneratorInput.Add(new XElement("ConfigFilePath",_ConfigFilePath));
		    ReportGeneratorInput.Add(new XElement("DestinationFolder",Path.Combine(dirPath,"Results")));
            ReportGeneratorInput.Add(new XElement("ReportFileName",_ReportFileName));
            PluginInput.Add(ReportGeneratorInput);
            Plugin.Add(PluginInput);
            doc.Root.Add(Plugin);
            doc.Save(_ticketPath);
        }

 

    }
}