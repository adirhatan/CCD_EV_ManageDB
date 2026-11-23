using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace CCD_EV_ManageDB.Repositories
{
    public class MDBInventory : MDBClient
    {
        public MDBInventory()
            : base("DB_CMP", "Inventory")
        { }

        public MDBInventory(string db)
            : base(db, "Inventory")
        { }

        public Boolean SaveInventory(Inventory inv)
        {
            base.MDBSaveRec(inv.toBsonDocument());
            return true;
        }

        public Boolean DeleteInventory(string id)
        {
            base.MDBDeleteID(id);
            return true;
        }

        public Inventory ReadInventory(string id)
        {
            Inventory Inv = new Inventory();
            if (String.IsNullOrWhiteSpace(id)) return Inv;
            BsonDocument rec = collection.FindOneById(id);
            if (rec != null) Inv = rec.BsonDocumentToInventory();
            return Inv;
        }



        public List<Inventory> ReadAllInventory()
        {
            List<Inventory> Inventorylines = new List<Inventory>();
            MongoCursor Cur;

            Cur = base.MDBReadAllMatchedIDRecs("/^./");
            foreach (BsonDocument doc in Cur)
            {
                Inventorylines.Add(new Inventory(doc));
            }
            return Inventorylines;

        }

        public string[] ReadAllInventoryPlatforms()
        {
            List<string> InventoryPlatforms = new List<string>();
            MongoCursor Cur;

            Cur = base.MDBReadAllMatchedIDRecs("/^./");
            foreach (BsonDocument doc in Cur)
            {
                if (!(doc.GetValue("FinishDate").ToDouble() > 0))
                InventoryPlatforms.Add(doc.GetValue("_id").ToString());
            }
            return InventoryPlatforms.ToArray();

        }

        public Dictionary<string, HashSet<string>> getDictionaryAllInventoryPlatformsBIOS()
        {
            Inventory curInv;
            MongoCursor Cur = null;
            //Cur = this.MDBFind("Matches;_id;/^./", "AND", "DES", "TestEnd");
            //Cur.Limit = 10000;
            Cur = base.MDBReadAllMatchedIDRecs("/^./");

            Dictionary<string, HashSet<string>> dictionaryBioses = new Dictionary<string, HashSet<string>>();
            try
            {
                foreach (BsonDocument doc in Cur)
                {
                    if (!(doc.GetValue("FinishDate").ToDouble() > 0))
                    {
                        HashSet<string> BIOSES = new HashSet<string>();
                        curInv = doc.BsonDocumentToInventory();
                        BIOSES.Add(curInv.BIOS);
                        foreach (InvSaved curSavedInv in curInv.SavedItems)
                            if (!BIOSES.Contains(curSavedInv.BIOS))
                            {
                                BIOSES.Add(curSavedInv.BIOS);
                            }
                        dictionaryBioses.Add(curInv.platform, BIOSES);
                    }
                }
            }
            catch
            {
                return null;
            }
            return dictionaryBioses;
        }

        public Dictionary<string, HashSet<string>> getDictionaryAllInventoryPlatformsNVM()
        {
            Inventory curInv;
            MongoCursor Cur = null;
            //Cur = this.MDBFind("Matches;_id;/^./", "AND", "DES", "TestEnd");
            //Cur.Limit = 10000;
            Cur = base.MDBReadAllMatchedIDRecs("/^./");

            Dictionary<string, HashSet<string>> dictionaryNvms = new Dictionary<string, HashSet<string>>();
            try
            {
                foreach (BsonDocument doc in Cur)
                {
                    if (!(doc.GetValue("FinishDate").ToDouble() > 0))
                    {
                        HashSet<string> NVMS = new HashSet<string>();
                        curInv = doc.BsonDocumentToInventory();
                        NVMS.Add(curInv.NVM);
                        foreach (InvSaved curSavedInv in curInv.SavedItems)
                            if (!NVMS.Contains(curSavedInv.NVM))
                            {
                                NVMS.Add(curSavedInv.NVM);
                            }
                        dictionaryNvms.Add(curInv.platform, NVMS);
                    }
                }
            }
            catch
            {
                return null;
            }
            return dictionaryNvms;
        }

        public Dictionary<string, HashSet<string>> getDictionaryAllInventoryPlatformsUnitIDS()
        {
            Inventory curInv;
            MongoCursor Cur = null;
            //Cur = this.MDBFind("Matches;_id;/^./", "AND", "DES", "TestEnd");
            //Cur.Limit = 10000;
            Cur = base.MDBReadAllMatchedIDRecs("/^./");

            Dictionary<string, HashSet<string>> dictionaryUnitIDS = new Dictionary<string, HashSet<string>>();
            try
            {
                foreach (BsonDocument doc in Cur)
                {
                    if (!(doc.GetValue("FinishDate").ToDouble() > 0))
                    {
                        HashSet<string> UnitIDS = new HashSet<string>();
                        curInv = doc.BsonDocumentToInventory();
                        foreach (string unitID in curInv.UnitIds)
                            UnitIDS.Add(unitID);
                        dictionaryUnitIDS.Add(curInv.platform, UnitIDS);
                    }
                }
            }
            catch
            {
                return null;
            }
            return dictionaryUnitIDS;
        }
    }
    //    public class Inventory
    //{
    //    public string platform { get; set; }
    //    public string UnitsNumber { get; set; }
    //    public string ReceptionDate { get; set; }
    //    public string BIOS { get; set; }
    //    public string NVM { get; set; }
    //    public string LastUpdateDate { get; set; }
    //    public string FinishDate { get; set; }

    //    public Inventory()
    //    { }

    //    public Inventory(BsonDocument doc)
    //    {
    //        BIOS = (doc.GetValue("BIOS", "").ToString());
    //        FinishDate = (doc.GetValue("FinishDate", "").ToString());
    //        LastUpdateDate = (doc.GetValue("LastUpdateDate", "").ToString());
    //        NVM = (doc.GetValue("NVM", "").ToString());
    //        platform = (doc.GetValue("platform", "").ToString());
    //        ReceptionDate = (doc.GetValue("ReceptionDate", "").ToString());
    //        UnitsNumber = (doc.GetValue("UnitsNumber", "").ToString());
    //    }

    //    public BsonDocument toBsonDocument()
    //    {
    //        BsonDocument InvDoc = new BsonDocument { { "_id", platform } };
    //        InvDoc.Add("BIOS", BIOS);
    //        InvDoc.Add("FinishDate", FinishDate);
    //        InvDoc.Add("LastUpdateDate", LastUpdateDate);
    //        InvDoc.Add("NVM", NVM);
    //        InvDoc.Add("ReceptionDate", ReceptionDate);
    //        InvDoc.Add("UnitsNumber", UnitsNumber);
    //        return InvDoc;
    //    }

    //}

    //public static class BsonDocumentInventoryExtension
    //{
    //    // This is the extension method. 
    //    // The first parameter takes the "this" modifier
    //    // and specifies the type for which the method is defined. 

    //    public static Inventory BsonDocumentToInventory(this BsonDocument doc)
    //    {
    //        Inventory Inv = new Inventory(doc);
    //        //Inv.BIOS = (doc.GetValue("BIOS", "").ToString());
    //        //Inv.FinishDate = (doc.GetValue("FinishDate", "").ToString());
    //        //Inv.LastUpdateDate = (doc.GetValue("LastUpdateDate", "").ToString());
    //        //Inv.NVM = (doc.GetValue("NVM", "").ToString());
    //        //Inv.platform = (doc.GetValue("platform", "").ToString());
    //        //Inv.ReceptionDate = (doc.GetValue("ReceptionDate", "").ToString());
    //        //Inv.UnitsNumber = (doc.GetValue("UnitsNumber", "").ToString());
    //        return Inv;
    //    }
    //}
}