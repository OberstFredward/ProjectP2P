using System.Net;
using System.Net.Sockets;

namespace ProjectP2P
{
    class TcpListenerAdapted : TcpListener //Aufgrund des TcpListerner.active -> protected [Tolle Idee Microsoft! -.-]
    {
            public TcpListenerAdapted(IPEndPoint localEP) : base(localEP) //Weiterleitung an die Basiskonstruktoren
            {
            }

            public TcpListenerAdapted(IPAddress localaddr, int port) : base(localaddr, port)
            {
            }

            public new bool Active //die .active Eigenschaft mit .Active zurückgeben
            {
                get { return base.Active; }
            }
        }
}
