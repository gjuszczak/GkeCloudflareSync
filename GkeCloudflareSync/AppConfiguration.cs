using System;
using System.Collections.Generic;
using System.Text;

namespace GkeCloudflareSync
{
    public class AppConfiguration
    {
        public string Domain { get; set; }
        public string Hostname { get; set; }
        public string CloudflareApiToken { get; set; }

        public string ExternalIpNodeAddressType { get; set; }
    }
}
