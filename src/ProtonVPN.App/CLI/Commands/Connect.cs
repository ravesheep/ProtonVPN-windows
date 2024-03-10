using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProtonVPN.Core.Profiles;
using ProtonVPN.Core.Servers.Specs;
using ProtonVPN.Core.Servers;
using ProtonVPN.Core.Service.Vpn;
using ProtonVPN.Core.Servers.Models;
using ProtonVPN.Core.Settings;
using System.Globalization;

namespace ProtonVPN.CLI.Commands
{
    public class Connect
    {
        // Connect
        //  --Connect
        //  --Connect profile {name}
        //  --Connect profile quick|fastest|random
        //  --Connect server {name} (e.g. US-FL#53)
        //  --Connect server {countryCode} [SecureCore|Tor|P2P|B2B]
        //  --Connect server {countryCode} {city} [SecureCore|Tor|P2P|B2B]
        public static async Task HandleConnectAsync(IReadOnlyList<string> cParams, IVpnManager vpnManager, IVpnConnector vpnConnector,
            IUserStorage userStorage, ServerManager serverManager, ProfileManager profileManager)
        {
            // --Connect
            if (cParams.Count == 0)
            {
                await vpnManager.QuickConnectAsync();
                return;
            }

            // Since profile|server require at least 1 more param (+ itself), we should return
            if (cParams.Count < 2)
            {
                return;
            }

            string subCommand = cParams[0].ToLower();

            // --Connect profile
            if (subCommand.Equals("profile"))
            {
                Profile profile;
                string profileName = cParams[1].ToLower();

                // --Connect profile quick
                if (profileName.Equals("quick"))
                {
                    await vpnManager.QuickConnectAsync();
                    return;
                }

                // --Connect profile fastest
                else if (profileName.Equals("fastest"))
                {
                    profile = await profileManager.GetFastestProfile();
                }

                // --Connect profile random
                else if (profileName.Equals("random"))
                {
                    profile = await profileManager.GetRandomProfileAsync();
                }

                // --Connect profile name
                else
                {
                    IReadOnlyList<Profile> profileList = await profileManager.GetProfiles();
                    profile = profileList.Single(v => v.Name.ToLower().Equals(profileName));
                }

                if (profile != null)
                {
                    await vpnManager.ConnectAsync(profile);
                }
            }

            // --Connect server
            else if (subCommand.Equals("server"))
            {
                string query = cParams[1];
                Server server = Server.Empty();

                // --Connect server name (i.e. US-FL#53)
                Server serverByName = serverManager.GetServer(new ServerByName(query.ToUpper()));

                if (serverByName != null)
                {
                    server = serverByName;
                }

                // --Connect server countryCode [SecureCore|Tor|P2P|B2B]
                // --Connect server countryCode city [SecureCore|Tor|P2P|B2B]
                else
                {
                    IReadOnlyCollection<Server> serverList;
                    Features features = (Features)vpnConnector.LastServer.Features;
                    string city = string.Empty;

                    // Both city and feature included, order is known
                    if (cParams.Count > 3)
                    {
                        city = cParams[2];
                        string feat = cParams[3].ToLower();

                        features = feat.Equals("securecore") ? Features.SecureCore : feat.Equals("tor") ? Features.Tor
                            : feat.Equals("p2p") ? Features.P2P : feat.Equals("b2b") ? Features.B2B : features;
                    }
                    // Check if cParams[2] is a city or feature
                    else if (cParams.Count > 2)
                    {
                        switch (cParams[2].ToLower())
                        {
                            case "securecore":
                                features = Features.SecureCore;
                                break;
                            case "tor":
                                features = Features.Tor;
                                break;
                            case "p2p":
                                features = Features.P2P;
                                break;
                            case "b2b":
                                features = Features.B2B;
                                break;
                            default:
                                city = cParams[2];
                                break;
                        }
                    }

                    ExitCountryServer byCountry = new(query.ToUpper());
                    ExactTierServer byTier = new(userStorage.GetUser().IsTierPlusOrHigher() ? ServerTiers.Plus : ServerTiers.Free);
                    ServerByFeatures byFeatures = new(features);

                    // City included
                    if (city != string.Empty)
                    {
                        city = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(city.ToLower());
                        ExitCityServer byCity = new(city);
                        serverList = serverManager.GetServers(byCountry & byCity & byTier & byFeatures);
                    }
                    // Only country code
                    else
                    {
                        serverList = serverManager.GetServers(byCountry & byTier & byFeatures);
                    }

                    if (serverList.Count > 0)
                    {
                        server = serverList.ElementAt(new Random().Next(serverList.Count));
                    }
                }

                // Not found
                if (server.IsEmpty())
                {
                    return;
                }

                Profile profile = new("")
                {
                    ServerId = server.Id
                };

                await vpnManager.ConnectAsync(profile);
            }
        }
    }
}
