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
using System.Windows.Forms;
using System.Windows.Threading;

namespace ProjectP2P
{
    public class Profile
    {
        //------------------Eigenschaften----------------------------
        //Profile-Objekt Attribute -> Werden im Konstruktor festgelegt, nur auslesbar (readonly)
        internal string externIPv6;
        internal string externIPv4;
        internal string localIPv4;
        internal string localIPv6;
        internal readonly int id;
        //Hilfsvariabeln statisch
        internal static bool InternetConnection;
        //Neue Threads
        private static Task CheckInternetConnectionAndGetIPsTask;
        //Events
        public static EventHandler UpdateMainFormEvent;
        //Main Thread Dispatcher
        private readonly Dispatcher MainThreadDispatcher;
        //-------------------Konstruktoren----------------------------
        public Profile(Dispatcher mainThreadDispatcher)
        {
            externIPv4 = ""; //Falls keine Internetverbindung
            externIPv6 = "";
            this.MainThreadDispatcher = mainThreadDispatcher;
            CheckInternetConnectionAndGetIPsTask = new Task(() => CheckInternetConnectionAndGetIPs(out localIPv6,out localIPv4,out externIPv6,out externIPv4,MainThreadDispatcher));
            CheckInternetConnectionAndGetIPsTask.Start();
            CreateID(out id);
        }
        //---------------statische Methoden (zum Setzen der Attribute)-----------------------
        private static void CreateID(out int id)
        {
            id = new Random().Next(1000, 9999);
        }
        private static void CheckInternetConnectionAndGetIPs(out string localIPv6, out string localIPv4, out string externIPv6, out string externIPv4,Dispatcher MainThreadDispatcher)
        {
            try
            {
                using (Ping pingCheckIp = new Ping()) //'using' führt sofort die pingCheckIp.Dispose() Methode nach dem verlassen des Blockes aus -> Garbage Collector kann Speicherplatz wieder freimachen
                {
                    PingReply reply = pingCheckIp.Send("google.com", 1000, new byte[32], new PingOptions());
                    if (reply.Status == IPStatus.Success) //Ist der PingStatus erfolgreich? 
                    {
                        InternetConnection = true;
                    }
                    else
                    {
                        reply = pingCheckIp.Send("yahoo.com", 1000, new byte[32], new PingOptions());
                        if (reply.Status == IPStatus.Success) //Ist der PingStatus erfolgreich? 
                        {
                            InternetConnection = true;
                        }
                        else
                        {
                            InternetConnection = false;
                        }
                    }
                }
            }
            catch
            {
                InternetConnection = false;
            }
            //--------------------------getLocalIPs-----------------------
            localIPv4 = "";
            localIPv6 = "";

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                if (adapter.OperationalStatus == OperationalStatus.Up && (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet || adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)) //Ethernet (LAN) oder WLAN
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    foreach (var ip in properties.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork) localIPv4 = ip.Address.ToString();
                        if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6 && localIPv6 == "") localIPv6 = ip.Address.ToString();
                        long test = ip.AddressPreferredLifetime; 
                    }
                }
            }
            //-------------------------------getExternalIPs-----------------------
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
            MainThreadDispatcher.Invoke(() => UpdateMainFormEvent(null, new EventArgs()));
        }
    }
}
