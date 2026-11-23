using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;

namespace CCD_EV_ManageDB.Models
{
    public class Inventory
    {
        public string platform { get; set; }
        public string project { get; set; }
        public string Vendor { get; set; }
        public string UnitsNumber { get; set; }
        public string ReceptionDate { get; set; }
        public string BIOS { get; set; }
        public string NVM { get; set; }
        public string LastUpdateDate { get; set; }
        public string FinishDate { get; set; }
        public string Ports { get; set; }
        public string DevType { get; set; }
        public bool HDMISupport { get; set; }
        public string Preset10G0 { get; set; }
        public string Preset10G1 { get; set; }
        public string Preset10G2 { get; set; }
        public string Preset10G3 { get; set; }
        public string Preset10GAMA0 { get; set; }
        public string Preset10GAMA1 { get; set; }
        public string Preset10GAMA2 { get; set; }
        public string Preset10GAMA3 { get; set; }
        public string Preset20G0 { get; set; }
        public string Preset20G1 { get; set; }
        public string Preset20G2 { get; set; }
        public string Preset20G3 { get; set; }
        public string USB2Amp { get; set; }
        public List<string> Reports { get; set; }
        public List<InvSaved> SavedItems { get; set; }
        public List<string> UnitIds { get; set; }
        public List<string> PFinishDates { get; set; }

        public Inventory()
        {
            FinishDate = "0";
            Reports = new List<string>();
            SavedItems = new List<InvSaved>();
            UnitIds = new List<string>();
            PFinishDates = new List<string>();
        }

        public Inventory(string lplatform, string lproject, string lVendor, string lUnitsNumber, string lReceptionDate, string lBIOS, string lNMV, string lLastUpdateDate, string lFinishDate, string lPorts, string lDevType, bool lHDMISupport, string lPreset10G0, string lPreset10G1, string lPreset10G2, string lPreset10G3, string lPreset10GAMA0, string lPreset10GAMA1, string lPreset10GAMA2, string lPreset10GAMA3, string lPreset20G0, string lPreset20G1, string lPreset20G2, string lPreset20G3, string lUSB2Amp)
        {
            platform = lplatform;
            project = lproject;
            Vendor = lVendor;
            UnitsNumber = lUnitsNumber;
            ReceptionDate = lReceptionDate;
            BIOS = lBIOS;
            NVM = lNMV;
            LastUpdateDate = lLastUpdateDate;
            FinishDate = lFinishDate;
            Ports = lPorts;
            DevType = lDevType;
            HDMISupport = lHDMISupport;
            Preset10G0 = lPreset10G0;
            Preset10G1 = lPreset10G1;
            Preset10G2 = lPreset10G2;
            Preset10G3 = lPreset10G3;
            Preset10GAMA0 = lPreset10GAMA0;
            Preset10GAMA1 = lPreset10GAMA1;
            Preset10GAMA2 = lPreset10GAMA2;
            Preset10GAMA3 = lPreset10GAMA3;
            Preset20G0 = lPreset20G0;
            Preset20G1 = lPreset20G1;
            Preset20G2 = lPreset20G2;
            Preset20G3 = lPreset20G3;
            USB2Amp = lUSB2Amp;
            SavedItems = new List<InvSaved>();
            Reports = new List<string>();
            UnitIds = new List<string>();
            PFinishDates = new List<string>();
            
        }
        public Inventory(BsonDocument doc)
        {
            BIOS = (doc.GetValue("BIOS", "").ToString());
            FinishDate = (doc.GetValue("FinishDate", "").ToString());
            LastUpdateDate = (doc.GetValue("LastUpdateDate", "").ToString());
            NVM = (doc.GetValue("NVM", "").ToString());
            platform = (doc.GetValue("_id", "").ToString());
            project = (doc.GetValue("project", "").ToString());
            ReceptionDate = (doc.GetValue("ReceptionDate", "").ToString());
            UnitsNumber = (doc.GetValue("UnitsNumber", "").ToString());
            Vendor = (doc.GetValue("Vendor", "").ToString());
            Ports = (doc.GetValue("Ports", "").ToString());
            DevType = (doc.GetValue("DevType", "").ToString());
            HDMISupport = (doc.GetValue("HDMISupport", "").ToBoolean());
            Preset10G0 = (doc.GetValue("Preset10G0", "").ToString());
            Preset10G1 = (doc.GetValue("Preset10G1", "").ToString());
            Preset10G2 = (doc.GetValue("Preset10G2", "").ToString());
            Preset10G3 = (doc.GetValue("Preset10G3", "").ToString());
            Preset10GAMA0 = (doc.GetValue("Preset10GAMA0", "").ToString());
            Preset10GAMA1 = (doc.GetValue("Preset10GAMA1", "").ToString());
            Preset10GAMA2 = (doc.GetValue("Preset10GAMA2", "").ToString());
            Preset10GAMA3 = (doc.GetValue("Preset10GAMA3", "").ToString());
            Preset20G0 = (doc.GetValue("Preset20G0", "").ToString());
            Preset20G1 = (doc.GetValue("Preset20G1", "").ToString());
            Preset20G2 = (doc.GetValue("Preset20G2", "").ToString());
            Preset20G3 = (doc.GetValue("Preset20G3", "").ToString());
            USB2Amp = (doc.GetValue("USB2Amp", "").ToString());
            Reports = new List<string>();
            if (doc.Contains("Reports"))
            {
                BsonArray rArray = doc.GetElement("Reports").Value.AsBsonArray;
                foreach (string report in rArray)
                {
                    Reports.Add(report);
                }
            }
            SavedItems = new List<InvSaved>();
            BsonArray bArray = doc.GetElement("SavedItems").Value.AsBsonArray;
            foreach (BsonDocument item in bArray)
            {
                InvSaved IS = new InvSaved(item.GetValue("BIOS").ToString(), item.GetValue("NVM").ToString(), item.GetValue("UpdateDate").ToString());
                SavedItems.Add(IS);
            }
            UnitIds = new List<string>();
            if (doc.Contains("UnitIds"))
            {
                BsonArray uArray = doc.GetElement("UnitIds").Value.AsBsonArray;
                foreach (string UnitId in uArray)
                {
                    UnitIds.Add(UnitId);
                }
            }
            PFinishDates = new List<string>();
            if (doc.Contains("PFinishDates"))
            {
                BsonArray pfArray = doc.GetElement("PFinishDates").Value.AsBsonArray;
                foreach (string PFinishDate in pfArray)
                {
                    PFinishDates.Add(PFinishDate);
                }
            }
        }

        public BsonDocument toBsonDocument()
        {
            BsonDocument InvDoc = new BsonDocument { { "_id", platform } };
            InvDoc.Add("project", project);
            InvDoc.Add("BIOS", BIOS);
            InvDoc.Add("FinishDate", Convert.ToDouble(FinishDate));
            InvDoc.Add("LastUpdateDate", Convert.ToDouble(LastUpdateDate));
            InvDoc.Add("NVM", NVM);
            InvDoc.Add("ReceptionDate", Convert.ToDouble(ReceptionDate));
            InvDoc.Add("UnitsNumber", UnitsNumber);
            InvDoc.Add("Vendor", Vendor);
            InvDoc.Add("Ports", Ports);
            InvDoc.Add("DevType", DevType);
            InvDoc.Add("HDMISupport", HDMISupport);
            InvDoc.Add("Preset10G0", Preset10G0);
            InvDoc.Add("Preset10G1", Preset10G1);
            InvDoc.Add("Preset10G2", Preset10G2);
            InvDoc.Add("Preset10G3", Preset10G3);
            InvDoc.Add("Preset10GAMA0", Preset10GAMA0);
            InvDoc.Add("Preset10GAMA1", Preset10GAMA1);
            InvDoc.Add("Preset10GAMA2", Preset10GAMA2);
            InvDoc.Add("Preset10GAMA3", Preset10GAMA3);
            InvDoc.Add("Preset20G0", Preset20G0);
            InvDoc.Add("Preset20G1", Preset20G1);
            InvDoc.Add("Preset20G2", Preset20G2);
            InvDoc.Add("Preset20G3", Preset20G3);
            InvDoc.Add("USB2Amp", USB2Amp);
            
            BsonArray rArray = new BsonArray();
            foreach (string report in Reports)
            {
                rArray.Add(report);
            }
            InvDoc.Add("Reports", rArray);

            BsonArray bArray = new BsonArray();
            foreach (var term in SavedItems)
            {
                bArray.Add(term.ToBsonDocument());
            }
            InvDoc.Add("SavedItems", bArray);
            BsonArray uArray = new BsonArray();

            foreach (string UnitId in UnitIds)
            {
                uArray.Add(UnitId);
            }
            InvDoc.Add("UnitIds", uArray);

            BsonArray pfArray = new BsonArray();
            foreach (string PFinishDate in PFinishDates)
            {
                pfArray.Add(PFinishDate);
            }
            InvDoc.Add("PFinishDates", pfArray);
            return InvDoc;
        }
        public HashSet<string> GetInventoryFields()
        {
            HashSet<string> Fields = new HashSet<string>(new string[] { "_id","Project","Vendor", "BIOS", "FinishDate", "LastUpdateDate", "NVM", "ReceptionDate", "UnitsNumber" ,"DevType"});
            return Fields;
        }

    }

    public class InvSaved
    {
        public string BIOS { get; set; }
        public string NVM { get; set; }
        public string UpdateDate { get; set; }

        public InvSaved(string _BIOS, string _NVM, string _UpdateDate)
        {
            BIOS = _BIOS;
            NVM = _NVM;
            UpdateDate = _UpdateDate;
        }
    }

    public static class BsonDocumentInventoryExtension
    {
        // This is the extension method. 
        // The first parameter takes the "this" modifier
        // and specifies the type for which the method is defined. 

        public static Inventory BsonDocumentToInventory(this BsonDocument doc)
        {
            Inventory Inv = new Inventory(doc);
            return Inv;
        }
    }
}