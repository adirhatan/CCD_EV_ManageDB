using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CCD_EV_ManageDB.Models
{
    public class ReportConfig
    {
        public string table { get; set; }
        public List<TitleItem> TitleItems { get; set; }
        public List<FieldSlide> FieldsSlides { get; set; }
        public List<ImageDetails> ImagesDetails { get; set; }

        public ReportConfig(string tn)
        {
            table = tn;
            TitleItems = new List<TitleItem>();
            FieldsSlides = new List<FieldSlide>();
            ImagesDetails = new List<ImageDetails>();
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument tmp = new BsonDocument();
            tmp.Add("table", table);
            BsonArray bArray1 = new BsonArray();
            BsonArray bArray2 = new BsonArray();
            BsonArray bArray3 = new BsonArray();
            List<string> imageDetailsIDs = new List<string>();

            foreach (var term in FieldsSlides)
            {
                bArray1.Add(term.ToBsonDocument());
                if (!String.IsNullOrWhiteSpace(term.imageDetailsID)) imageDetailsIDs.Add(term.imageDetailsID);
            }
            tmp.Add("FieldsSlides", bArray1);
            foreach (var term in TitleItems)
            {
                bArray2.Add(term.ToBsonDocument());
            }
            tmp.Add("TitleItems", bArray2);
            foreach (ImageDetails term in ImagesDetails)
            {
                if (imageDetailsIDs.Contains(term.imageDetailsID)) bArray3.Add(term.ToBsonDocument());
            }
            tmp.Add("ImagesDetails", bArray3);
            return tmp;
        }

        //public void fromBsonDocument(BsonDocument doc)
        //{
        //    BsonArray bArray = doc.GetElement("FieldsSlides").Value.AsBsonArray;
        //    foreach (BsonDocument item in bArray)
        //    {
        //        FieldSlide fs = new FieldSlide(item.GetValue("templateKey").ToString(), item.GetValue("test").ToString(), item.GetValue("column").ToString());
        //        FieldsSlides.Add(fs);
        //    }
        //    bArray.Clear();
        //    bArray = doc.GetElement("FieldsSlides").Value.AsBsonArray;
        //    foreach (BsonDocument item in bArray)
        //    {
        //        TitleItem ti = new TitleItem(item.GetValue("top").ToString(), item.GetValue("height").ToString(), item.GetValue("fontsize").ToString(),item.GetValue("content").ToString());
        //        TitleItems.Add(ti);
        //    }
        //}
        public FieldSlide GetImageSlide(string _ImageSlideID)
        {
            foreach (var slide in this.FieldsSlides) if (slide.imageDetailsID == _ImageSlideID) return slide;
            return (new FieldSlide("", "", "", "", "", "", "", "",""));

        }

        private void initImagesDetailsForSlide(string ImageDetilsID)
        {
            //TODO: error handling
            foreach (ImageDetails item in this.ImagesDetails)
            {
                if (item.imageDetailsID == ImageDetilsID) return;
            }
            this.ImagesDetails.Add(new ImageDetails(GetImageSlide(ImageDetilsID), 1));
            this.ImagesDetails.Add(new ImageDetails(GetImageSlide(ImageDetilsID), 2));
            this.ImagesDetails.Add(new ImageDetails(GetImageSlide(ImageDetilsID), 3));
            this.ImagesDetails.Add(new ImageDetails(GetImageSlide(ImageDetilsID), 4));
        }
        public List<ImageDetails> GetSlideImageDetails(string ImageDetilsID)
        {
            //parse number of images from template and refers images <= to this number
            string tmp = Regex.Match(GetImageSlide(ImageDetilsID).templateKey, @"\d").Value;
            int numOfImages = 0;
            int currentLastImage = 0;
            Int32.TryParse(tmp, out numOfImages);
            List<ImageDetails> _ImageDetails = new List<ImageDetails>();
            initImagesDetailsForSlide(ImageDetilsID);
            foreach (ImageDetails item in this.ImagesDetails)
            {
                if ((item.imageDetailsID == ImageDetilsID) && (item.order <= numOfImages))
                {
                    _ImageDetails.Add(item);
                    if (item.order > currentLastImage) currentLastImage = item.order;
                }
            }
            for (int cnt = currentLastImage + 1; cnt <= numOfImages; cnt++) _ImageDetails.Add(new ImageDetails(GetImageSlide(ImageDetilsID), cnt));
            return _ImageDetails.OrderBy(o => o.order).ToList();

        }

        public FieldSlide GetFieldSlideBySpec(string _SpecName)
        {
            foreach (var slide in this.FieldsSlides) if (slide.type == General.ReportSlideType.limits.ToString() && (slide.test==_SpecName)) return slide;
            return (new FieldSlide("", "", "", "", "", "", "", "", ""));

        }

       
    }

    public class TitleItem
    {
        public string top { get; set; }
        public string left { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public string fontSize { get; set; }
        public string foreground { get; set; }
        public bool isMultiLine { get; set; }
        public string content { get; set; }

        public TitleItem(string t, string h,string fs, string c)
        {
            top = t;
            height = h;
            fontSize = fs;
            content = c;
        }
    }

    public class FieldSlide
    {
        public string type { get; set; }
        public string templateKey { get; set; }
        public string test { get; set; }
        public string column { get; set; }
        public string text { get; set; }
        public string specRef { get; set; }
        public string lsl { get; set; }
        public string usl { get; set; }
        private string _imageDetailsID;
        public string imageDetailsID 
        {
            get
            {
                return _imageDetailsID;
            }
            set 
            {
                if ((String.IsNullOrWhiteSpace(value)) && (type==General.ReportSlideType.image.ToString())) _imageDetailsID = ObjectId.GenerateNewId().ToString();
                else _imageDetailsID = value;
            } 
        }
        public string units { get; set; }
        //public ImageDetails[] ii;
        public string margin { get; set; }

        public FieldSlide(string typ, string tk, string t, string c, string _text, string _lsl, string _usl, string _imageDetailsID, string _units)
        {
            templateKey = tk;
            test = t;
            column = c;
            lsl = _lsl;
            usl = _usl;
            type = typ;
            text = _text;
            imageDetailsID = _imageDetailsID;
            units = _units;
            //ii = new ImageDetails [] 
            //{
            //    new ImageDetails (1,"","","","",""),
            //    new ImageDetails (1,"","","","",""),
            //    new ImageDetails (1,"","","","",""),
            //    new ImageDetails (1,"","","","","")
            //};


        }
        public FieldSlide(string typ, string tk, string t, string c, string _text, string _lsl, string _usl, string _imageDetailsID, string _units,decimal _margin)
        {
            templateKey = tk;
            test = t;
            column = c;
            lsl = _lsl;
            usl = _usl;
            type = typ;
            text = _text;
            imageDetailsID = _imageDetailsID;
            units = _units;
            margin = _margin.ToString();
            //if (margin == "0") margin = "20";

        }
        public FieldSlide(string typ, string tk, string t, string c, string _text, string _lsl, string _usl, string _imageDetailsID, string _units, string _specRef)
        {
            templateKey = tk;
            test = t;
            column = c;
            lsl = _lsl;
            usl = _usl;
            type = typ;
            text = _text;
            specRef = _specRef;
            imageDetailsID = _imageDetailsID;
            units = _units;
        }

        public FieldSlide(string typ, string tk, string t, string c, string _text, string _lsl, string _usl, string _imageDetailsID, string _units, string _specRef, string _margin)
        {
            templateKey = tk;
            test = t;
            column = c;
            lsl = _lsl;
            usl = _usl;
            type = typ;
            text = _text;
            specRef = _specRef;
            imageDetailsID = _imageDetailsID;
            units = _units;
            margin = _margin;
        }

        public string GetPassFailbyLimits(string value)
        {
            if (string.IsNullOrEmpty(value)) return "None";
            if ( (!string.IsNullOrEmpty(usl)) && (ConvertToDec(value) > ConvertToDec(usl))) return "Fail";
            if ((!string.IsNullOrEmpty(lsl)) && (ConvertToDec(value) < ConvertToDec(lsl))) return "Fail";
            return "Pass";
        }
        public string getMarginForReportConf()
        {
            string m = "";
            decimal cal_margin = 0;
            if (!String.IsNullOrEmpty(usl) && !String.IsNullOrEmpty(lsl)) cal_margin = (ConvertToDec(usl) - ConvertToDec(lsl)) * ConvertToDec(margin) / 100;
            if (!String.IsNullOrEmpty(usl) && String.IsNullOrEmpty(lsl)) cal_margin = Math.Abs(ConvertToDec(usl) * ConvertToDec(margin) / 100);
            if (String.IsNullOrEmpty(usl) && !String.IsNullOrEmpty(lsl)) cal_margin = Math.Abs(ConvertToDec(lsl) * ConvertToDec(margin) / 100);
           
            if (cal_margin != 0) m = cal_margin.ToString();
            return m;
        }

        private Decimal ConvertToDec(string val)
        {
            decimal decVal = 0;
            try
            {
                if (val.ToUpper().Contains("E")) decVal = Decimal.Parse(val, System.Globalization.NumberStyles.Float);
                else decVal = Convert.ToDecimal(val);
            }
            catch (Exception e)
            {
                return 0;
            }
            return decVal;
        }

        public static string GetLimit31TestName(string table, string _TestName)
        {
            if (table.StartsWith("DP"))
            {
                string bitrate = "";
                string swing = "";
                string emphasis = "";
                string ppm = "";
                string step = "";
                string cable = "";
                string noSSC = "";
                _TestName = Regex.Replace(_TestName, "\\W", "");
                _TestName = Regex.Replace(_TestName, "_", "");
                //example: VTX TRANSITION BIT OUTPUT RATIO_5.4 Gbps_Pre-emphasis 1_Swing 0
                if (Regex.IsMatch(_TestName, @"\d*Gbps"))
                {
                    Match m = Regex.Match(_TestName, @"(\d*)Gbps");
                    bitrate = "BR" + m.Groups[1].Value;
                }
                if (Regex.IsMatch(_TestName, @"Swing\d*"))
                {
                    Match m = Regex.Match(_TestName, @"Swing(\d*)");
                    swing = "SW" + m.Groups[1].Value;
                }
                if (Regex.IsMatch(_TestName, @"emphasis\d*"))
                {
                    Match m = Regex.Match(_TestName, @"emphasis(\d*)");
                    emphasis = "EM" + m.Groups[1].Value;
                }
                if (Regex.IsMatch(_TestName, @"cable"))
                {
                    cable = "nc";
                }
                if (Regex.IsMatch(_TestName, @"NoSSC"))
                {
                    noSSC = "ns";
                }
                //example: ClockSwitchMinus600ppm54Gbpsmax10step   - parse step,ppm (after punctuation removed)
                if (Regex.IsMatch(_TestName, @"\d*ppm"))
                {
                    Match m = Regex.Match(_TestName, @"(\d\d)\d*ppm");
                    ppm = "P" + m.Groups[1].Value;
                }
                if (Regex.IsMatch(_TestName, @"\d\dstep"))
                {
                    Match m = Regex.Match(_TestName, @"(\d\d)step");
                    step = "ST" + m.Groups[1].Value;
                }
                if ((_TestName.Length > 31) && (!string.IsNullOrEmpty(bitrate) || !string.IsNullOrEmpty(swing) || !string.IsNullOrEmpty(emphasis) || !string.IsNullOrEmpty(cable) || !string.IsNullOrEmpty(noSSC)))
                {
                    _TestName = _TestName.Substring(0, 16) + cable + noSSC + ppm +  bitrate + swing + emphasis + step;
                }
                if ((_TestName.Length > 31) && (string.IsNullOrEmpty(bitrate)) && (string.IsNullOrEmpty(swing)) && (string.IsNullOrEmpty(emphasis)) && (string.IsNullOrEmpty(cable)) && (string.IsNullOrEmpty(noSSC)))
                {
                    _TestName = _TestName.Substring(0, 16) + _TestName.Substring(_TestName.Length - 15, 15);
                }
                return _TestName;
            }
            if (table.StartsWith("LAN_IEEE"))
            {
                //cannot execute: _TestName = Regex.Replace(_TestName, "\\W", ""); since -/+ characters are needed
                _TestName = Regex.Replace(_TestName, "_", "");
                _TestName = Regex.Replace(_TestName, "/", "");
                if (_TestName.Length > 31) _TestName = _TestName.Substring(0, 15) + _TestName.Substring(_TestName.Length - 16, 16);
                return _TestName;
            }

            _TestName = Regex.Replace(_TestName, "\\W", "");
            _TestName = Regex.Replace(_TestName, "_", "");
            if (_TestName.Length > 31) _TestName = _TestName.Substring(0, 15) + _TestName.Substring(_TestName.Length - 16, 16);
            return _TestName;

        }

    }


    public class ImageDetails
    {
        //public string _id { get; set; }
        public int order { get; set; }
        public string field { get; set; }
        public string query { get; set; }
        public string text { get; set; }
        public string test { get; set; }
        public string imageDetailsID { get; set; }

        public ImageDetails(int _order, string _field, string _query, string _text, string _test, string _imageDetailsID )
        {
            //_id = id;
            //if (String.IsNullOrWhiteSpace(_id)) _id = ObjectId.GenerateNewId().ToString();
            order = _order;
            field = _field;
            query = _query;
            text = _text;
            test = _test;
            imageDetailsID = _imageDetailsID;

        }
        public ImageDetails( string _imageDetailsID)
        {
             //_id = ObjectId.GenerateNewId().ToString();
            order = 1;
            field = "";
            query = "";
            text = "";
            test = "";
            imageDetailsID = _imageDetailsID;

        }

        public ImageDetails(FieldSlide FS,int _order)
        {
            //_id = id;
            //if (String.IsNullOrWhiteSpace(_id)) _id = ObjectId.GenerateNewId().ToString();
            order = _order;
            field = FS.column;
            query = "";
            text = "";
            test = FS.test;
            imageDetailsID = FS.imageDetailsID;

        }
    }

    public static class BsonDocumentRCExtension
    {
        // This is the extension method. 
        // The first parameter takes the "this" modifier
        // and specifies the type for which the method is defined. 
        public static ReportConfig BsonDocumentToReportConfig(this BsonDocument doc)
        {
            //Dictionary<string, FieldSlide> SlidesImageDetailsIDs = new Dictionary<string, FieldSlide>();
            //HashSet<string> imageDetailsIDsHash = new HashSet<string>();
            ReportConfig RC = new ReportConfig(doc.GetValue("table", "").ToString());

            BsonArray bArray = doc.GetElement("FieldsSlides").Value.AsBsonArray;
            foreach (BsonDocument item in bArray)
            {
                FieldSlide fs = new FieldSlide(item.GetValue("type", General.ReportSlideType.limits.ToString()).ToString(), item.GetValue("templateKey").ToString(), item.GetValue("test").ToString(), item.GetValue("column").ToString(), item.GetValue("text", "").ToString(), item.GetValue("lsl", "").ToString(), item.GetValue("usl", "").ToString(), item.GetValue("imageDetailsID", " ").ToString(), item.GetValue("units", "").ToString(), item.GetValue("specRef", "").ToString(), item.GetValue("margin", "").ToString());
                RC.FieldsSlides.Add(fs);
              //  if (!String.IsNullOrWhiteSpace(fs.imageDetailsID.ToString()) && (fs.imageDetailsID != BsonNull.Value) && (!SlidesImageDetailsIDs.Keys.Contains("fs.imageDetailsID"))) SlidesImageDetailsIDs.Add(fs.imageDetailsID,fs);
            }
            BsonArray bArray2 = doc.GetElement("TitleItems").Value.AsBsonArray;
            foreach (BsonDocument item in bArray2)
            {
                TitleItem ti = new TitleItem(item.GetValue("top").ToString(), item.GetValue("height").ToString(), item.GetValue("fontSize").ToString(), item.GetValue("content").ToString());
                RC.TitleItems.Add(ti);
            }
            if (doc.Contains("ImagesDetails"))
            {
                BsonArray bArray3 = doc.GetElement("ImagesDetails").Value.AsBsonArray;
                foreach (BsonDocument item in bArray3)
                {
                    ImageDetails ID = new ImageDetails(item.GetValue("order", 0).ToInt32(), item.GetValue("field", "").ToString(), item.GetValue("query", "").ToString(), item.GetValue("text", "").ToString(), item.GetValue("test", "").ToString(), item.GetValue("imageDetailsID", "").ToString());
                    RC.ImagesDetails.Add(ID);
                    //imageDetailsIDsHash.Add(ID.imageDetailsID);
                }
            }
            ////new: recognize new image slides with no related imageDetails records and add imageDetails record for them
            //foreach (string ID in imageDetailsIDsHash) if (SlidesImageDetailsIDs.Keys.Contains("ID")) SlidesImageDetailsIDs.Remove("ID");
            //foreach (var IDslide in SlidesImageDetailsIDs) RC.ImagesDetails.Add(new ImageDetails(IDslide.Value));
            
            return RC;
        }
    }

}