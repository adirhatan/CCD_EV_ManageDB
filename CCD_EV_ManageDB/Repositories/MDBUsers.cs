using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CCD_EV_ManageDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace CCD_EV_ManageDB.Repositories
{
    public class MDBUsers : MDBClient
    {
        public MDBUsers()
            : base("DB_PRM", "users")
        { }

        public Boolean validateUser(string username, string password)
        {
            try
            {
                BsonDocument userDBrec = collection.FindOneById(new BsonString(username));
                if (userDBrec == null) return false;
                if ((StringCipher.Encrypt(password, "") == userDBrec.GetElement("pass").Value.ToString()) || (password == userDBrec.GetElement("pass").Value.ToString()))return true;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public BsonDocument ReadUser(string username)
        {
            try
            {
                BsonDocument userDBrec = collection.FindOneById(new BsonString(username));
                if (userDBrec == null) return null;
                return userDBrec;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public Boolean ChangePasswd(string username, string oldPass,string newPass)
        {
            if (validateUser(username,oldPass))
            {
                try
                {
                    BsonDocument userDBrec = ReadUser(username);
                    userDBrec.Set("pass", StringCipher.Encrypt(newPass, ""));
                    base.MDBSaveRec(userDBrec);
                }
                catch (Exception e)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public List<string> ReadAllUsers()
        {
            List<string> users = new List<string>();
            foreach (BsonDocument user in collection.FindAll()) users.Add(user.GetValue("_id").ToString());
            return users;
        }

        public int getQueryDays(string username)
        {
            int queryDays = 7;
            BsonDocument userDBrec;
            try
            {
                userDBrec = collection.FindOneById(new BsonString(username));
            }
            catch (Exception e)
            {
                return queryDays;
            }
            if (userDBrec == null) return queryDays;
            return userDBrec.GetValue("QueryDays", "7").ToInt32();
        }

        public bool setQueryDays(string username, int queryDays)
        {
            BsonDocument userDBrec = collection.FindOneById(new BsonString(username));

            if (userDBrec != null)
            {
                try
                {
                    userDBrec.Set("QueryDays", queryDays);
                    base.MDBSaveRec(userDBrec);
                }
                catch (Exception e)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public string getMail(string username)
        {
            string mailAd = "";
            BsonDocument userDBrec;
            try
            {
                userDBrec = collection.FindOneById(new BsonString(username));
            }
            catch (Exception e)
            {
                return mailAd;
            }
            if (userDBrec == null) return mailAd;
            return userDBrec.GetValue("mail", "").ToString();
        }

        public bool setMail(string username, string mailAd)
        {
            BsonDocument userDBrec = collection.FindOneById(new BsonString(username));

            if (userDBrec != null)
            {
                try
                {
                    userDBrec.Set("mail", mailAd);
                    base.MDBSaveRec(userDBrec);
                }
                catch (Exception e)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public List<DBpermission> getPermissions(string username)
        {
            List<DBpermission> Permissions = new List<DBpermission>();
            List<DBpermission> DummyPermissions = new List<DBpermission>();
            DummyPermissions.Add( new DBpermission("Interface","Project","Phase"));
            BsonDocument userDBrec;
            try
            {
                userDBrec = collection.FindOneById(new BsonString(username));
            }
            catch (Exception e)
            {
                return DummyPermissions;
            }
            if (userDBrec == null) return DummyPermissions;
            foreach (string permission in userDBrec.GetValue("permissions", "").ToString().Split(','))
            {
                Permissions.Add(new DBpermission(permission));
            }
            return Permissions;

        }
        


    }

        public static class StringCipher
    {
        // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
        // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        private static readonly byte[] initVectorBytes = Encoding.ASCII.GetBytes("tu89geji340t89u2");

        // This constant is used to determine the keysize of the encryption algorithm.
        private const int keysize = 256;

        public static string Encrypt(string plainText, string passPhrase)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                byte[] cipherTextBytes = memoryStream.ToArray();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            using(PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);
                using(RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using(ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                    {
                        using(MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using(CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }
    }


}