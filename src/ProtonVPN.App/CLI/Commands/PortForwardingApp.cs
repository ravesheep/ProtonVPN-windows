using System.Collections.Generic;
using System.Threading.Tasks;
using ProtonVPN.Core.Settings;

namespace ProtonVPN.CLI.Commands
{
    public class PortForwardingApp
    {
        // Port Forwarding App
        //  --PortForwardingApp disabled|qBittorrent|custom
        public static Task HandlePortForwardingApp(IReadOnlyList<string> cParams, IAppSettings appSettings)
        {
            if (cParams.Count == 0)
            {
                return Task.CompletedTask;
            }

            Core.PortForwarding.PortForwardingApp app;
            string query = cParams[0].ToLower();

            switch (query)
            {
                case "disabled":
                    app = Core.PortForwarding.PortForwardingApp.Disabled;
                    break;

                case "qbittorrent":
                    app = Core.PortForwarding.PortForwardingApp.qBittorrent;
                    break;

                case "custom":
                    app = Core.PortForwarding.PortForwardingApp.Custom;
                    break;

                default:
                    return Task.CompletedTask;
            }

            appSettings.PortForwardingApp = app;
            return Task.CompletedTask;
        }
    }
}
