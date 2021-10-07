﻿/*
 * Copyright (c) 2021 Proton Technologies AG
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

using System;
using ProtonVPN.Core.Modals;
using ProtonVPN.Core.Models;
using ProtonVPN.Core.Settings;
using ProtonVPN.Core.Window.Popups;
using ProtonVPN.Windows.Popups.Upsell;

namespace ProtonVPN.Modals.Welcome
{
    public class WelcomeModalManager
    {
        private readonly Random _random = new();
        private readonly IAppSettings _appSettings;
        private readonly IUserStorage _userStorage;
        private readonly IPopupWindows _popupWindows;
        private readonly IModals _modals;

        public WelcomeModalManager(
            IAppSettings appSettings,
            IUserStorage userStorage,
            IPopupWindows popupWindows,
            IModals modals)
        {
            _appSettings = appSettings;
            _userStorage = userStorage;
            _popupWindows = popupWindows;
            _modals = modals;
        }

        public void Load()
        {
            User user = _userStorage.User();
            if (WelcomeModalHasToBeShown())
            {
                ShowWelcomeModal();
            }
            else if (!user.Paid() && !_userStorage.User().IsDelinquent())
            {
                ShowEnjoyModal();
            }
        }

        private void ShowEnjoyModal()
        {
            int randomNumber = _random.Next(0, 100);
            if (randomNumber >= 15)
            {
                return;
            }

            _popupWindows.Show<EnjoyingUpsellPopupViewModel>();
        }

        private void ShowWelcomeModal()
        {
            _modals.Show<WelcomeModalViewModel>();
            _appSettings.WelcomeModalShown = true;
        }

        private bool WelcomeModalHasToBeShown()
        {
            return !_appSettings.WelcomeModalShown;
        }
    }
}