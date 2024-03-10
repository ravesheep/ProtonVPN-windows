using System.Collections.Generic;
using System.Threading.Tasks;
using ProtonVPN.Common.KillSwitch;
using ProtonVPN.Core.Settings;

namespace ProtonVPN.CLI.Commands
{
    public class KillSwitch
    {
        // Kill Switch
        //  --KillSwitch off|on|permanent
        public static Task HandleKillSwitch(IReadOnlyList<string> cParams, IAppSettings appSettings)
        {
            if (cParams.Count == 0)
            {
                return Task.CompletedTask;
            }

            KillSwitchMode mode;
            string query = cParams[0].ToLower();

            switch (query)
            {
                case "off":
                    mode = KillSwitchMode.Off;
                    break;
                case "on":
                    mode = KillSwitchMode.Soft;
                    break;
                case "permanent":
                    mode = KillSwitchMode.Hard;
                    break;
                default:
                    return Task.CompletedTask;
            }

            appSettings.KillSwitchMode = mode;
            return Task.CompletedTask;
        }
    }
}
