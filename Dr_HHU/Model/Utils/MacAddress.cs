using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Dr_HHU.Model.Utils
{
    public static class MacAddress
    {
        public static PhysicalAddress MAC { get; private set; }

        public static void Initialize(NetworkInterface i)
        {
            MAC = i.GetPhysicalAddress();
        }

        public static void Initialize(long Addr)
        {
            byte[] p = new byte[6];
            p[5] = (byte)(Addr & 0xFF);
            Addr >>= 8;
            p[4] = (byte)(Addr & 0xFF);
            Addr >>= 8;
            p[3] = (byte)(Addr & 0xFF);
            Addr >>= 8;
            p[2] = (byte)(Addr & 0xFF);
            Addr >>= 8;
            p[1] = (byte)(Addr & 0xFF);
            Addr >>= 8;
            p[0] = (byte)(Addr & 0xFF);
            MAC = new PhysicalAddress(p);
        }
    }
}
