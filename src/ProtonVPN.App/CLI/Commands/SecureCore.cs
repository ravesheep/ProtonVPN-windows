using System.Collections.Generic;
using System.Threading.Tasks;
using ProtonVPN.Core.Service.Vpn;
using ProtonVPN.Core.Settings;

namespace ProtonVPN.CLI.Commands
{
    public class SecureCore
    {
        // Secure Core
        //  --SecureCore on|off
        public static async Task HandleSecureCoreAsync(IReadOnlyList<string> cParams, IAppSettings appSettings, IVpnManager vpnManager)
        {
            if (cParams.Count == 0)
            {
                return;
            }

            string mode = cParams[0].ToLower();

            if (mode.Equals("on") && !appSettings.SecureCore)
            {
                appSettings.SecureCore = true;
                appSettings.PortForwardingEnabled = false;
                await vpnManager.ReconnectAsync();
            }

            else if (mode.Equals("off") && appSettings.SecureCore)
            {
                appSettings.SecureCore = false;
                await vpnManager.ReconnectAsync();
            }
        }
    }
}
