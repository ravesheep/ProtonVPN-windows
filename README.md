# A fork of Proton VPN Windows app

This fork adds:
- Basic command-line (CLI) support
- Automatic updates to the forwarded port in qBittorrent
  - Can also send the port via command-line to other apps or scripts

## qBittorrent

Every time ProtonVPN's forwarded port number changes (e.g. when connecting to another server), it will notify qBittorrent of the change and update it.  
This can be done via qBittorrent's web UI or command-line.  
This behavior can be tweaked in the settings and should match your qBittorrent settings (e.g: is authentication enabled via username/password?).

⚠️ As port forwarding is a paid feature, free users may not see these settings in the app.

## Other apps

You can set a command to run whenever the port changes and use %protonPort% or !protonPort! to get the current port.  
You can set another command to run whenever changing the app setting (e.g. to free the previous port number or close the app).

A few examples of commands that can be used:
```bat
:: Running a batch file (use %protonPort% inside your script)
"C:\Users\...\script.bat"

:: ...or you can pass the port as an argument (use %1 inside your script)
"C:\Users\...\script.bat" !protonPort!

:: You will also likely want to change the working directory
cd "C:\Users\...\" & script.bat

:: Running a Node script
node "C:\Users\...\" !protonPort!
cd "C:\Users\...\" & node . !protonPort!
```

## Command-line (CLI)

Because there is currently no separate CLI application, commands do not output any text or value (see [#4](https://github.com/ravesheep/ProtonVPN-windows/issues/4)).  
Arguments are accepted by both the `ProtonVPN.Launcher` and `ProtonVPN` executable.  
Here's all currently supported commands:

```sh
# Logs into the given account
ProtonVPN.Launcher --Login {username} {password} [2fa]

# Logs out from the current account
ProtonVPN.Launcher --Logout

# Sets the protocol to use for the VPN connection
ProtonVPN.Launcher --Protocol smart|wireguard|udp|tcp

# Enables or disables kill switch
ProtonVPN.Launcher --KillSwitch on|off|permanent

# Enables or disables secure core
ProtonVPN.Launcher --SecureCore on|off

# Enables or disables port forwarding
ProtonVPN.Launcher --PortForwarding on|off

# Sets the selected port forwarding app
ProtonVPN.Launcher --PortForwardingApp disabled|qBittorrent|custom

# Enables or disables split tunneling
ProtonVPN.Launcher --SplitTunnel state on|off

# Sets split tunneling to include or exclude apps and IPs
ProtonVPN.Launcher --SplitTunnel mode include|exclude

# Adds or removes the given app or IP address to the split tunneling list
ProtonVPN.Launcher --SplitTunnel app add {path} [name]
ProtonVPN.Launcher --SplitTunnel app remove {path|name}
ProtonVPN.Launcher --SplitTunnel ip add|remove {ip}

# Enables or disables the given app or IP on the split tunneling list
ProtonVPN.Launcher --SplitTunnel app enable|disable {path|name}
ProtonVPN.Launcher --SplitTunnel ip enable|disable {ip}

# Quick connect
ProtonVPN.Launcher --Connect

# Connects using a profile
ProtonVPN.Launcher --Connect profile {name}|quick|fastest|random

# Connects to a server given its name (e.g. US-FL#53)
ProtonVPN.Launcher --Connect server {name}

# Connects to a server in the given country (and city, if specified) with given features,
# or the previous server's features
ProtonVPN.Launcher --Connect server {countryCode} [SecureCore|Tor|P2P|B2B]
ProtonVPN.Launcher --Connect server {countryCode} {city} [SecureCore|Tor|P2P|B2B]

# Disconnects from the current server
ProtonVPN.Launcher --Disconnect
```

## Updating

Updating can be safely done from within the app, as it will download updates of this fork instead of the official version.

Downloads are available on the [Github repository](https://github.com/ravesheep/ProtonVPN-windows/releases/latest).


## License
Original ProtonVPN app and code:  
Copyright (c) 2023 Proton AG

See [LICENSE](LICENSE)
