using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ProjectP2P
{
    class PartnerProfile
    {
        //Eingschaften
        internal string IPv4{ get; set; }
        internal string IPv6{ get; set; }
        internal int Id{ get; set; }

        internal bool IsLocalIPv4 { get; private set; }
        internal bool IsLocalIPv6 { get; private set; }

        public PartnerProfile(string IPv6, string IPv4, string ID)
        {
            this.IPv6 = IPv6;
            this.IPv4 = IPv4;
            try
            {
                this.Id = Convert.ToInt32(ID);
            }
            catch
            {
                this.Id = 0000; //ID "0" = Fehler
            }
            if (MainWindow.CheckIpAdress(IPv4))
            {
                IsLocalIPv4 = false;
            }
            else
            {
                IsLocalIPv4 = true;
            }
            if (MainWindow.CheckIpAdress(IPv6))
            {
                IsLocalIPv6 = false;
            }
            else
            {
                IsLocalIPv6 = true;
            }
        }
    }
}
