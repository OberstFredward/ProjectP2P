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
            byte checkIPv4 = MainWindow.CheckIpAdress(IPv4);
            if (checkIPv4 == 0)
            {
                IsLocalIPv4 = true;
            }
            else if(checkIPv4 == 2)
            {
                IsLocalIPv4 = false;
            }
            byte checkIPv6 = MainWindow.CheckIpAdress(IPv6);
            if (checkIPv6 == 1)
            {
                IsLocalIPv6 = true;
            }
            else if(checkIPv6 == 3)
            {
                IsLocalIPv6 = false;
            }
        }
    }
}
