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

using ProtonVPN.Logging.Contracts;
using ProtonVPN.ProcessCommunication.Common.Channels;
using ProtonVPN.ProcessCommunication.Common.Registration;
using ProtonVPN.ProcessCommunication.Contracts.Controllers;

namespace ProtonVPN.ProcessCommunication.App
{
    public static class FirstAppInstanceCaller
    {
        public static async Task HandleCommandAsync(string[] args)
        {
            if (args.Length == 0)
            {
                await OpenMainWindowAsync(new Uri("https://example.com"));
            }

            else if (Uri.TryCreate(string.Join(" ", args), UriKind.Absolute, out Uri uri))
            {
                await OpenMainWindowAsync(uri);
            }

            else
            {
                await SendCommandAsync(args);
            }
        }

        public static async Task OpenMainWindowAsync(Uri uri)
        {
            AppServerPortRegister appServerPortRegister = new(new NullLogger());
            int? appServerPort = appServerPortRegister.ReadOnce();

            if (appServerPort.HasValue && GrpcChannelWrapperFactory.IsPortValid(appServerPort.Value))
            {
                await CreateGrpcChannelAndSendOpenWindowCommandAsync(appServerPort.Value, uri);
            }
        }

        private static async Task CreateGrpcChannelAndSendOpenWindowCommandAsync(int appServerPort, Uri uri)
        {
            try
            {
                GrpcChannelWrapper grpcChannelWrapper = new(appServerPort);
                IAppController appController = grpcChannelWrapper.CreateService<IAppController>();

                await SendOpenWindowCommandAsync(appController, uri);
                await grpcChannelWrapper.ShutdownAsync();
            }
            catch
            {
            }
        }

        private static async Task SendOpenWindowCommandAsync(IAppController appController, Uri uri)
        {
            try
            {
                await appController.OpenWindow(uri);
            }
            catch
            {
            }
        }

        public static async Task SendCommandAsync(string[] args)
        {
            AppServerPortRegister appServerPortRegister = new(new NullLogger());
            int? appServerPort = appServerPortRegister.ReadOnce();

            if (appServerPort.HasValue && GrpcChannelWrapperFactory.IsPortValid(appServerPort.Value))
            {
                await CreateGrpcChannelAndSendCommandAsync(appServerPort.Value, args);
            }
        }

        private static async Task CreateGrpcChannelAndSendCommandAsync(int appServerPort, string[] args)
        {
            try
            {
                GrpcChannelWrapper grpcChannelWrapper = new(appServerPort);
                IAppController appController = grpcChannelWrapper.CreateService<IAppController>();

                await SendCommandAsync(appController, args);
                await grpcChannelWrapper.ShutdownAsync();
            }
            catch
            {
            }
        }

        private static async Task SendCommandAsync(IAppController appController, string[] args)
        {
            try
            {
                await appController.ReceiveCommand(args);
            }
            catch
            {
            }
        }
    }
}