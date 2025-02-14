﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace TestesNFe
{
    public static class DigitalCertificateAgent
    {
        public static void AddToStore(this X509Certificate2 @this, StoreName storeName = StoreName.TrustedPublisher, StoreLocation storeLocation = StoreLocation.CurrentUser, OpenFlags openFlags = OpenFlags.MaxAllowed)
        {
            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(openFlags);
            store.Add(@this);
            store.Close();
        }

        public static X509Certificate2 ChooseCertificate()
        {
            X509Certificate2 certificate = null;

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            var collection = store.Certificates;
            var fcollection = collection.Find(X509FindType.FindByTimeValid, DateTime.Now, true);
            var scollection = X509Certificate2UI.SelectFromCollection(fcollection, "Certificados válidos:", "Selecione o certificado que deseja usar", X509SelectionFlag.SingleSelection);

            if (scollection.Count == 0)
                throw new Exception("Nenhum certificado foi selecionado!");

            certificate = scollection[0];
            if (certificate == null)
                throw new Exception("Nenhum certificado foi selecionado!");

            store.Close();
            return certificate;
        }

        public static string ConvertToBase64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }
        public static byte[] ConvertToBytes(string base64String)
        {
            return Convert.FromBase64String(base64String);
        }
        public static byte[] ConvertToBytes(this X509Certificate2 @this, X509ContentType certificateType = X509ContentType.Pfx)
        {
            return @this.Export(certificateType);
        }
        public static byte[] ConvertToBytes(this X509Certificate2 @this, string password, X509ContentType certificateType = X509ContentType.Pfx)
        {
            return @this.Export(certificateType, password);
        }

        public static X509Certificate2 FindBySerialNumber(string serialNumber)
        {
            X509Certificate2 certificate = null;

            if (string.IsNullOrEmpty(serialNumber))
                throw new Exception("Não foi possível localizar o certificado digital!");

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            foreach (var item in store.Certificates)
            {
                if (item.SerialNumber != null && item.SerialNumber.ToUpper().Equals(serialNumber.ToUpper(), StringComparison.InvariantCultureIgnoreCase))
                    certificate = item;
            }
            if (certificate == null)
                throw new Exception(string.Format("Certificado digital nº {0} não encontrado!", serialNumber.ToUpper()));

            store.Close();
            return certificate;
        }
        public static X509Certificate2 ImportFromBase64(string base64String)
        {
            var certicateInBytes = Convert.FromBase64String(base64String);
            var certificate = new X509Certificate2(certicateInBytes);
            AddToStore(certificate);
            return certificate;
        }
        public static X509Certificate2 ImportFromBase64(string base64String, string password)
        {
            var certicateInBytes = Convert.FromBase64String(base64String);
            var certificate = new X509Certificate2(certicateInBytes, password, X509KeyStorageFlags.Exportable);
            if (certificate.HasPrivateKey)
                AddToStore(certificate);
            return certificate;
        }
        public static X509Certificate2 ImportFromBytes(byte[] bytes)
        {
            var certificate = new X509Certificate2(bytes);
            AddToStore(certificate);
            return certificate;
        }

        public static bool IsValid(this X509Certificate2 @this)
        {
            return Convert.ToDateTime(@this.GetExpirationDateString()) > DateTime.Now;
        }

        public static List<X509Certificate2> ListOnlyValidCerticates(StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            try
            {
                ConcurrentBag<X509Certificate2> bag = new ConcurrentBag<X509Certificate2>();
                var store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                Parallel.ForEach(store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, true).OfType<X509Certificate2>(), certificate =>
                {
                    bag.Add(certificate);
                });
                store.Close();
                return bag?.OrderBy(ob => ob.NotAfter).ToList();
            }
            catch
            {
                return null;
            }
        }
    }
}
