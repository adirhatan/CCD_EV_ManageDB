using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using CCD_EV_ManageDB.Repositories;

namespace CCD_EV_ManageDB.Models
{
    public class ReportSlideImageDetais
    {
        public List<string> Templates { get; set; }
        public HashSet<string> Tests { get; set; }
        public Dictionary<string, HashSet<string>> TestsFields { get; set; }
        //all FieldSlide fields taht related to images:
        public List<ImageDetails> ImagesDetails;
        public string ReportID;
        public string slideTest;
        public string imageDetailsID;
        public string templateKey;
        public string slideText;

        public ReportSlideImageDetais(List<string> _Templates, HashSet<string> _Tests, Dictionary<string, HashSet<string>> _TestsFields, List<ImageDetails> _ImagesDetails,
                                        string _ReportID, string _imageDetailsID, string _templateKey, string _slideTest, string _slideText)
        {
            Templates = _Templates;
            Tests = _Tests;
            TestsFields = _TestsFields;
            ImagesDetails = _ImagesDetails;
            ReportID = _ReportID;
            imageDetailsID = _imageDetailsID;
            templateKey = _templateKey;
            slideTest = _slideTest;
            slideText = _slideText;

        }
    }
}