using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtonVPN.Core.PortForwarding
{
    public enum TorrentAppMode
    {
        WebUI,
        CommandLine
    }

    public enum PortForwardingApp
    {
        Disabled,
        qBittorrent,
        Custom
    }
}
