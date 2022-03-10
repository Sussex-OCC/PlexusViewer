﻿using Sussex.Lhcra.Plexus.Viewer.Domain.Interfaces;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Sussex.Lhcra.Plexus.Viewer.UI.Helpers
{
    public class IpAddressProvider : IIpAddressProvider
    {
        public string GetClientIpAddress()
        {
            var hostName = Dns.GetHostName();
            var ipAddresses = Dns.GetHostAddresses(hostName);

            var ipAddressV4 = ipAddresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)?.ToString();
            var ipAddressV6 = ipAddresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6)?.ToString();

            return ipAddressV4 ?? ipAddressV6;
        }

        public string GetHostIpAddress()
        {
            var hostName = Dns.GetHostName();
            var ipAddresses = Dns.GetHostAddresses(hostName);

            var ipAddressV4 = ipAddresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)?.ToString();
            var ipAddressV6 = ipAddresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6)?.ToString();

            return ipAddressV4 ?? ipAddressV6;
        }
    }
}
