using System.Collections.Generic;
using System.Threading.Tasks;
using ProtonVPN.Core.Service.Vpn;
using ProtonVPN.Core.Settings;

namespace ProtonVPN.CLI.Commands
{
    public class PortForwarding
    {
        // Port forwarding
        //  --PortForwarding on|off
        public static async Task HandlePortForwarding(IReadOnlyList<string> cParams, IAppSettings appSettings, IVpnManager vpnManager)
        {

            if (cParams.Count == 0)
            {
                return;
            }

            string mode = cParams[0].ToLower();

            if (mode.Equals("on"))
            {
                appSettings.PortForwardingEnabled = true;

                if (appSettings.SecureCore)
                {
                    appSettings.SecureCore = false;
                    await vpnManager.ReconnectAsync();
                }
            }

            else if (mode.Equals("off"))
            {
                appSettings.PortForwardingEnabled = false;
            }

            return;
        }

    }
}
