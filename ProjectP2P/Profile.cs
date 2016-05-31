using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ProjectP2P
{
    public class Profile
    {
        //------------------Eigenschaften----------------------------
        //Profile-Objekt Attribute -> Werden im Konstruktor festgelegt, nur auslesbar (readonly)
        internal readonly string externIPv6;
        internal readonly string externIPv4;
        internal readonly string localIPv4;
        internal readonly string localIPv6;
        internal readonly int id;
        //Hilfsvariabeln statisch
        internal static bool InternetConnection;
        //-------------------Konstruktoren----------------------------
        public Profile()
        {
            CheckInternetConnection();
            GetLocalIPs(out localIPv6, out localIPv4);
            GetExternalIPs(out externIPv6, out externIPv4);
            CreateID(out id);
        }
        //---------------statische Methoden (zum Setzen der Attribute)-----------------------
        private static void CreateID(out int id)
        {
            id = new Random().Next(1000, 9999);
        }
        public static bool CheckInternetConnection()
        {
            try
            {
                using (Ping pingCheckIp = new Ping()) //'using' führt sofort die pingCheckIp.Dispose() Methode nach dem verlassen des Blockes aus -> Garbage Collector kann Speicherplatz wieder freimachen
                {
                    PingReply reply = pingCheckIp.Send("icanhazip.com", 1000, new byte[32], new PingOptions());
                    if (reply.Status == IPStatus.Success) //Ist der PingStatus erfolgreich? 
                    {
                        InternetConnection = true;
                        return true;
                    }
                    else
                    {
                        InternetConnection = false;
                        return false;
                    }
                }
            }
            catch
            {
                InternetConnection = false;
                return false;
            }
        }

        private static void GetExternalIPs(out string externIPv6, out string externIPv4)
        {
            if (InternetConnection)
            {
                try
                {
                    externIPv6 = new WebClient().DownloadString(@"http://ipv6.icanhazip.com/").Trim();
                }
                catch
                {
                    externIPv6 = "";
                }
                try
                {
                    externIPv4 = new WebClient().DownloadString(@"http://ipv4.icanhazip.com/").Trim();
                }
                catch
                {
                    externIPv4 = "";
                }
            }
            else
            {
                externIPv4 = "";
                externIPv6 = "";
            }
        }

        private static void GetLocalIPs(out string localIPv6, out string localIPv4)
        {
            localIPv4 = "";
            localIPv6 = "";
            string hostName = Dns.GetHostName();
            for (int i = 0; i < Dns.GetHostEntry(hostName).AddressList.Length; i++)
            {
                if (Dns.GetHostEntry(hostName).AddressList[i].IsIPv6LinkLocal == true)
                {
                    localIPv6 = Dns.GetHostEntry(hostName).AddressList[i].ToString();
                }
                else if (Dns.GetHostEntry(hostName).AddressList[i].IsIPv6LinkLocal == false)
                {
                    localIPv4 = Dns.GetHostEntry(hostName).AddressList[i].ToString();
                }
            }
        }

        public static bool CheckPortOpenness()
        {
            return false;
        }
    }
}
