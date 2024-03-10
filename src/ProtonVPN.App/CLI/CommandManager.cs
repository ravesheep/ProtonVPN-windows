using System.Threading.Tasks;
using ProtonVPN.CLI.Commands;
using ProtonVPN.Common.Cli;
using ProtonVPN.Core.Auth;
using ProtonVPN.Core.Profiles;
using ProtonVPN.Core.Servers;
using ProtonVPN.Core.Service.Vpn;
using ProtonVPN.Core.Settings;
using ProtonVPN.Settings.SplitTunneling;

namespace ProtonVPN.CLI
{
    public class CommandManager : ICommandAware
    {
        private readonly IAppSettings _appSettings;
        private readonly IVpnManager _vpnManager;
        private readonly IVpnConnector _vpnConnector;
        private readonly IUserStorage _userStorage;
        private readonly IUserAuthenticator _userAuthenticator;
        private readonly ServerManager _serverManager;
        private readonly ProfileManager _profileManager;
        private readonly AppListViewModel _apps;
        private readonly SplitTunnelingIpListViewModel _ips;

        public CommandManager(IAppSettings appSettings, IVpnManager vpnManager, IVpnConnector vpnConnector, IUserStorage userStorage,
            IUserAuthenticator userAuthenticator, ServerManager serverManager, ProfileManager profileManager,
            AppListViewModel apps, SplitTunnelingIpListViewModel ips)
        {
            _appSettings = appSettings;
            _vpnManager = vpnManager;
            _vpnConnector = vpnConnector;
            _userStorage = userStorage;
            _userAuthenticator = userAuthenticator;
            _serverManager = serverManager;
            _profileManager = profileManager;
            _apps = apps;
            _ips = ips;
        }

        public async Task OnCommandReceived(CommandEventArgs e)
        {
            CommandLineOption login = new("login", e.Args);
            CommandLineOption logout = new("logout", e.Args);
            CommandLineOption connect = new("connect", e.Args);
            CommandLineOption disconnect = new("disconnect", e.Args);
            CommandLineOption protocol = new("protocol", e.Args);
            CommandLineOption killSwitch = new("killswitch", e.Args);
            CommandLineOption secureCore = new("secureCore", e.Args);
            CommandLineOption portForwarding = new("portforwarding", e.Args);
            CommandLineOption portForwardingApp = new("portforwardingapp", e.Args);
            CommandLineOption splitTunnel = new("splittunnel", e.Args);

            if (logout.Exists())
            {
                await _userAuthenticator.LogoutAsync();
                return;
            }

            if (login.Exists())
            {
                await Commands.Login.HandleLoginAsync(login.Params(), _userAuthenticator);
            }

            if (protocol.Exists())
            {
                await Protocol.HandleProtocolAsync(protocol.Params(), _appSettings, _vpnManager);
            }

            if (killSwitch.Exists())
            {
                await KillSwitch.HandleKillSwitch(killSwitch.Params(), _appSettings);
            }

            if (secureCore.Exists())
            {
                await SecureCore.HandleSecureCoreAsync(secureCore.Params(), _appSettings, _vpnManager);
            }

            if (portForwarding.Exists())
            {
                await Commands.PortForwarding.HandlePortForwarding(portForwarding.Params(), _appSettings, _vpnManager);
            }

            if (portForwardingApp.Exists())
            {
                await PortForwardingApp.HandlePortForwardingApp(portForwardingApp.Params(), _appSettings);
            }

            if (splitTunnel.Exists())
            {
                await SplitTunnel.HandleSplitTunnelAsync(splitTunnel.Params(), _appSettings, _vpnManager, _apps, _ips);
            }

            if (connect.Exists())
            {
                await Connect.HandleConnectAsync(connect.Params(), _vpnManager, _vpnConnector, _userStorage, _serverManager, _profileManager);
            }

            else if (disconnect.Exists())
            {
                await _vpnManager.DisconnectAsync();
            }
        }
    }
}
