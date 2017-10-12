using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace SecretNest.Web.Proxy
{
    static class Operators
    {
        static BlockingCollection<Lazy<Operator>> collection = new BlockingCollection<Lazy<Operator>>();
        static Operators()
        {
            int count = Properties.Settings.Default.ThreadLimit;
            string clientCertificateSubjectName = Properties.Settings.Default.ClientCertificateSubjectName;
            if (string.IsNullOrWhiteSpace(clientCertificateSubjectName))
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(new Lazy<Operator>(() => new OperatorWithoutCert()));
                }
            }
            else
            {
                bool useMachineStore = Properties.Settings.Default.UseMachineStore;
                X509Store store;
                if (useMachineStore)
                {
                    store = new X509Store(StoreLocation.LocalMachine);
                }
                else
                {
                    store = new X509Store(StoreLocation.CurrentUser);
                }
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectName, clientCertificateSubjectName, true);
                store.Close();
                store.Dispose();
                if (certs.Count > 0)
                {
                    X509Certificate cert = certs[0];

                    for (int i = 0; i < count; i++)
                    {
                        collection.Add(new Lazy<Operator>(() => new OperatorWithCert(cert)));
                    }
                }
                else
                {
                    throw new InvalidOperationException("Client certification cannot be found.");
                }
            }
        }


        public static Lazy<Operator> GetOne()
        {
            return collection.Take();
        }

        public static void PutOne(Lazy<Operator> item)
        {
            collection.Add(item);
        }
    }
}