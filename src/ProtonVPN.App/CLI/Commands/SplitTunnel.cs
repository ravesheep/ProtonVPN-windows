using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProtonVPN.Common;
using ProtonVPN.Common.Extensions;
using ProtonVPN.Common.OS.Net;
using ProtonVPN.Core.Service.Vpn;
using ProtonVPN.Core.Settings;
using ProtonVPN.Core.Settings.Contracts;
using ProtonVPN.Settings;
using ProtonVPN.Settings.SplitTunneling;

namespace ProtonVPN.CLI.Commands
{
    public class SplitTunnel
    {
        // Split tunneling
        //  --SplitTunnel state on|off
        //  --SplitTunnel mode include|exclude
        //  --SplitTunnel app add {path} [name]
        //  --SplitTunnel app remove|enable|disable {path|name}
        //  --SplitTunnel ip add|remove|enable|disable {ip}
        public static async Task HandleSplitTunnelAsync(IReadOnlyList<string> cParams, IAppSettings appSettings, IVpnManager vpnManager,
            AppListViewModel apps, SplitTunnelingIpListViewModel ips)
        {
            if (cParams.Count < 2)
            {
                return;
            }

            SplitTunnelMode splitTunnelMode = appSettings.SplitTunnelMode;

            string subCommand = cParams[0].ToLower();
            string action = cParams[1].ToLower();
            bool reconnectRequired = false;

            // --SplitTunnel state on|off
            if (subCommand.Equals("state"))
            {
                if (action.Equals("on") && !appSettings.SplitTunnelingEnabled)
                {
                    appSettings.SplitTunnelingEnabled = true;
                    reconnectRequired = true;
                }

                else if (action.Equals("off") && appSettings.SplitTunnelingEnabled)
                {
                    appSettings.SplitTunnelingEnabled = false;
                    reconnectRequired = true;
                }
            }

            // --SplitTunnel mode include|exclude
            else if (subCommand.Equals("mode"))
            {
                if (action.Equals("include") && splitTunnelMode != SplitTunnelMode.Permit)
                {
                    appSettings.SplitTunnelMode = SplitTunnelMode.Permit;
                    reconnectRequired = true;
                }

                else if (action.Equals("exclude") && splitTunnelMode != SplitTunnelMode.Block)
                {
                    appSettings.SplitTunnelMode = SplitTunnelMode.Block;
                    reconnectRequired = true;
                }
            }

            // --SplitTunnel app add|remove|enable|disable {path|name}
            else if (subCommand.Equals("app"))
            {
                if (cParams.Count < 3 || splitTunnelMode == SplitTunnelMode.Disabled)
                {
                    return;
                }

                string pathOrName = cParams[2].ToLower();
                bool shouldEnable = false;

                switch (action)
                {
                    // --SplitTunnel app add {path} [name]
                    case "add":
                        {
                            string name = cParams.Count > 3 ? cParams[3] : null;

                            SplitTunnelingApp[] savedApps = splitTunnelMode == SplitTunnelMode.Permit ? appSettings.SplitTunnelingAllowApps
                                : appSettings.SplitTunnelingBlockApps;

                            if (!IsValidAppPath(pathOrName) || savedApps.Any(app => app.Path.ToLower().Equals(pathOrName)))
                            {
                                return;
                            }

                            SplitTunnelingApp newApp = new()
                            {
                                Enabled = true,
                                Path = pathOrName,
                                Name = name ?? AppName(pathOrName)
                            };

                            if (splitTunnelMode == SplitTunnelMode.Permit)
                            {
                                appSettings.SplitTunnelingAllowApps = savedApps.Append(newApp).ToArray();
                            }

                            else
                            {
                                appSettings.SplitTunnelingBlockApps = savedApps.Append(newApp).ToArray();
                            }

                            reconnectRequired = true;
                            break;
                        }

                    // --SplitTunnel app remove {path|name}
                    case "remove":
                        {
                            SplitTunnelingApp[] savedApps = splitTunnelMode == SplitTunnelMode.Permit ? appSettings.SplitTunnelingAllowApps
                                : appSettings.SplitTunnelingBlockApps;

                            SplitTunnelingApp[] newArray = savedApps
                                .Where(app => !app.Path.ToLower().Equals(pathOrName) && !app.Name.ToLower().Equals(pathOrName))
                                .ToArray();

                            if (splitTunnelMode == SplitTunnelMode.Permit)
                            {
                                appSettings.SplitTunnelingAllowApps = newArray;
                            }

                            else
                            {
                                appSettings.SplitTunnelingBlockApps = newArray;
                            }

                            if (newArray.Length != savedApps.Length)
                            {
                                reconnectRequired = true;
                            }
                            break;
                        }

                    // --SplitTunnel app enable {path|name}
                    case "enable":
                        {
                            shouldEnable = true;
                            goto case "disable";
                        }

                    // --SplitTunnel app disable {path|name}
                    case "disable":
                        {
                            foreach (SelectableItemWrapper<AppViewModel> item in apps.Items)
                            {
                                SplitTunnelingApp app = item.Item.DataSource();

                                if (app.Path.ToLower().Equals(pathOrName) || app.Name.ToLower().Equals(pathOrName))
                                {
                                    if (item.Selected != shouldEnable)
                                    {
                                        item.Selected = shouldEnable;
                                        reconnectRequired = true;
                                    }
                                }
                            }
                            break;
                        }
                }
            }

            // --SplitTunnel ip add|remove|enable|disable {ip}
            else if (subCommand.Equals("ip"))
            {
                if (cParams.Count < 3)
                {
                    return;
                }

                if (splitTunnelMode == SplitTunnelMode.Disabled)
                {
                    return;
                }

                IpContract[] ipList = splitTunnelMode == SplitTunnelMode.Permit ? appSettings.SplitTunnelIncludeIps
                    : appSettings.SplitTunnelExcludeIps;

                IpContract ip = new()
                {
                    Enabled = true,
                    Ip = new NetworkAddress(cParams[2]).GetCidrString()
                };

                bool shouldEnable = false;

                switch (action)
                {
                    // --SplitTunnel ip add {ip}
                    case "add":
                        {
                            if (ipList.Any(item => item.Ip == ip.Ip))
                            {
                                return;
                            }

                            IpContract[] newArray = ipList.Append(ip).ToArray();

                            if (splitTunnelMode == SplitTunnelMode.Permit)
                            {
                                appSettings.SplitTunnelIncludeIps = newArray;
                            }

                            else
                            {
                                appSettings.SplitTunnelExcludeIps = newArray;
                            }

                            reconnectRequired = true;
                            break;
                        }

                    // --SplitTunnel ip remove {ip}
                    case "remove":
                        {
                            IpContract[] newArray = ipList.Where(item => item.Ip != ip.Ip).ToArray();

                            if (newArray.Length == ipList.Length)
                            {
                                return;
                            }

                            if (splitTunnelMode == SplitTunnelMode.Permit)
                            {
                                appSettings.SplitTunnelIncludeIps = newArray;
                            }

                            else
                            {
                                appSettings.SplitTunnelExcludeIps = newArray;
                            }

                            reconnectRequired = true;
                            break;
                        }

                    // --SplitTunnel ip enable {ip}
                    case "enable":
                        {
                            shouldEnable = true;
                            goto case "disable";
                        }

                    // --SplitTunnel ip disable {ip}
                    case "disable":
                        {
                            IEnumerable<SelectableItemWrapper<IpViewModel>> results = ips.Items.Where(item => item.Item.Ip.Equals(ip.Ip));

                            foreach (SelectableItemWrapper<IpViewModel> item in results)
                            {
                                if (item.Selected != shouldEnable)
                                {
                                    item.Selected = shouldEnable;
                                    reconnectRequired = true;
                                }
                            }
                            break;
                        }
                }
            }

            if (reconnectRequired)
            {
                await vpnManager.ReconnectAsync();
            }
        }

        private static bool IsValidAppPath(string path)
        {
            try
            {
                return !string.IsNullOrEmpty(path)
                    && Path.IsPathRooted(path)
                    && string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(path);
            }
            catch (ArgumentException) { }

            return false;
        }

        private static string AppName(string appPath)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(appPath);
                string name = versionInfo.FileDescription?.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    name = versionInfo.ProductName?.Trim();
                }

                if (string.IsNullOrEmpty(name))
                {
                    name = Path.GetFileNameWithoutExtension(appPath).Trim();
                }

                return name.GetFirstChars(200);
            }
            catch (FileNotFoundException) { }
            catch (ArgumentException) { }

            return string.Empty;
        }
    }
}
