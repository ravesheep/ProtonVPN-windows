using System.Collections.Generic;
using System.Threading.Tasks;
using ProtonVPN.Core.Service.Vpn;
using ProtonVPN.Core.Settings;

namespace ProtonVPN.CLI.Commands
{
    public class Protocol
    {
        // Protocol
        //  --Protocol smart|wireguard|udp|tcp
        public static async Task HandleProtocolAsync(IReadOnlyList<string> cParams, IAppSettings appSettings, IVpnManager vpnManager)
        {
            if (cParams.Count == 0)
            {
                return;
            }

            string ovpnProtocol = cParams[0].ToLower().Equals("smart") ? "auto" : cParams[0].ToLower();

            if (!ovpnProtocol.Equals("auto") && !ovpnProtocol.Equals("wireguard") && !ovpnProtocol.Equals("udp") && !ovpnProtocol.Equals("tcp"))
            {
                return;
            }

            if (ovpnProtocol != appSettings.OvpnProtocol)
            {
                appSettings.OvpnProtocol = ovpnProtocol;
                await vpnManager.ReconnectAsync();
            }
        }
    }
}
