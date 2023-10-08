/*
 * Copyright (c) 2023 Proton AG
 *
 * This file is part of ProtonVPN.
 *
 * ProtonVPN is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ProtonVPN is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ProtonVPN.  If not, see <https://www.gnu.org/licenses/>.
 */

using ProtonVPN.Common.PortForwarding;
using ProtonVPN.Core.PortForwarding;
using ProtonVPN.Core.Settings;
using ProtonVPN.Notifications;
using ProtonVPN.Translations;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ProtonVPN.PortForwarding
{
    public class PortForwardingNotifier : IPortForwardingNotifier, IPortForwardingStateAware
    {
        private static readonly HttpClient _httpClient = new();
        private readonly INotificationSender _notificationSender;
        private readonly IAppSettings _appSettings;
        private readonly SemaphoreSlim _semaphore;

        private int _currentExternalPort;
        private bool _isCookieSet = false;

        public PortForwardingNotifier(INotificationSender notificationSender, IAppSettings appSettings)
        {
            _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };

            _notificationSender = notificationSender;
            _appSettings = appSettings;
            _semaphore = new SemaphoreSlim(1);
        }
        public void OnPortForwardingStateChanged(PortForwardingState state)
        {
            if (state?.MappedPort?.MappedPort?.ExternalPort == null)
            {
                _currentExternalPort = 0;
            }
            else if (state.MappedPort.MappedPort.ExternalPort != _currentExternalPort)
            {
                _currentExternalPort = state.MappedPort.MappedPort.ExternalPort;
                SendNotification(state.MappedPort.MappedPort.ExternalPort);

                if (_appSettings.PortForwardingApp == PortForwardingApp.Disabled)
                {
                    return;
                }

                if (_appSettings.PortForwardingApp == PortForwardingApp.qBittorrent)
                {
                    if (_appSettings.TorrentAppMode == TorrentAppMode.CommandLine)
                    {
                        UpdatePortViaCommandLine(state.MappedPort.MappedPort.ExternalPort);
                    }

                    else if (_appSettings.TorrentAppMode == TorrentAppMode.WebUI)
                    {
                        Task.Run(() => UpdatePortViaWebUIAsync(state.MappedPort.MappedPort.ExternalPort));
                    }
                }

                else if (_appSettings.PortForwardingApp == PortForwardingApp.Custom)
                {
                    if (!string.IsNullOrEmpty(_appSettings.PortForwardingCommand_On))
                    {
                        RunCustomCommand(_appSettings.PortForwardingCommand_On, state.MappedPort.MappedPort.ExternalPort);
                    }
                }
            }
        }

        private void SendNotification(int port)
        {
            if (_appSettings.PortForwardingNotificationsEnabled)
            {
                string notificationTitle =
                    $"{Translation.Get("PortForwarding_lbl_ActivePort")} {port}";
                string notificationDescription = Translation.Get("PortForwarding_lbl_Info");
                _notificationSender.Send(notificationTitle, notificationDescription);
            }
        }

        private static void RunCustomCommand(string command, int port)
        {
            string final = string.Format("SET protonPort={0}&", port);
            final += command;

            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/v /c " + final)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            System.Diagnostics.Process proc = new System.Diagnostics.Process
            {
                StartInfo = procStartInfo
            };

            proc.Start();
        }

        private async Task UpdatePortViaWebUIAsync(int port)
        {
            await _semaphore.WaitAsync();

            // Get cookie
            if (_appSettings.TorrentAppWebUIAuthRequired && !_isCookieSet)
            {
                for (int i = 0; i < 5; i++)
                {
                    // The port was changed, stop retrying
                    if (port != _currentExternalPort)
                    {
                        break;
                    }

                    _isCookieSet = await GetCookieAsync();

                    if (_isCookieSet)
                    {
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10 * (i + 1)));
                }

                if (!_isCookieSet)
                {
                    _semaphore.Release();
                    throw new Exception("Could not authenticate to the torrent app's WebUI.");
                }
            }

            // Update port
            bool IsPortUpdated = false;

            for (int i = 0; i < 5; i++)
            {
                // The port was changed, stop retrying
                if (port != _currentExternalPort)
                {
                    IsPortUpdated = true;
                    break;
                }

                IsPortUpdated = await WebUISendPortAsync(port);

                if (IsPortUpdated)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(10 * (i + 1)));
            }

            _semaphore.Release();

            if (!IsPortUpdated)
            {
                throw new Exception("Could not update port through the torrent app's WebUI.");
            }
        }

        private async Task<bool> PostWebUIRequestAsync(string apiPath, Dictionary<string, string> content, bool doGetCookie)
        {
            try
            {
                string url = $"http://localhost:{_appSettings.TorrentAppWebUIPort}{apiPath}";

                FormUrlEncodedContent urlContent = new(content);

                using HttpResponseMessage response = await _httpClient.PostAsync(url, urlContent);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                // Likely requires authentication, cookie unset or invalid
                else if (((int)response.StatusCode == 401 || (int)response.StatusCode == 403) && _appSettings.TorrentAppWebUIAuthRequired && doGetCookie)
                {
                    _isCookieSet = await GetCookieAsync();

                    // Retry, with a cookie that should be valid
                    if (_isCookieSet)
                    {
                        if (await PostWebUIRequestAsync(apiPath, content, false))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return false;
        }

        private async Task<bool> GetCookieAsync()
        {
            Dictionary<string, string> content = new()
                {
                    { "username", _appSettings.TorrentAppWebUIAuthUsername },
                    { "password", _appSettings.TorrentAppWebUIAuthPassword }
                };

            return await PostWebUIRequestAsync("/api/v2/auth/login", content, false);
        }

        private async Task<bool> WebUISendPortAsync(int port)
        {
            Dictionary<string, string> content = new()
            {
                { "json", $"{{ \"listen_port\": {port} }}" }
            };

            return await PostWebUIRequestAsync("/api/v2/app/setPreferences", content, true);
        }

        private static void UpdatePortViaCommandLine(int port)
        {
            string command = string.Format("FOR /L %x IN (1, 1, 10) DO taskkill /IM qbittorrent.exe & "
                + "tasklist | find /I \"qbittorrent.exe\" & "
                + "IF ERRORLEVEL 1 (start \"\" \"C:\\Program Files\\qBittorrent\\qbittorrent.exe\" --torrenting-port={0} & exit) "
                + "ELSE (timeout /T 2)"
                , port);

            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            System.Diagnostics.Process proc = new System.Diagnostics.Process
            {
                StartInfo = procStartInfo
            };

            proc.Start();
        }
    }
}
