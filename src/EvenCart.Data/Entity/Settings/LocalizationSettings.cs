﻿#region License
// Copyright (c) Sojatia Infocrafts Private Limited.
// The following code is part of EvenCart eCommerce Software (https://evencart.co) Dual Licensed under the terms of
// 
// 1. GNU GPLv3 with additional terms (available to read at https://evencart.co/license)
// 2. EvenCart Proprietary License (available to read at https://evencart.co/license/commercial-license).
// 
// You can select one of the above two licenses according to your requirements. The usage of this code is
// subject to the terms of the license chosen by you.
#endregion

using EvenCart.Core.Config;

namespace EvenCart.Data.Entity.Settings
{
    public class LocalizationSettings : ISettingGroup
    {
        public bool AllowUserToSelectCurrency { get; set; }

        public bool AllowUserToSelectLanguage { get; set; }

        public int BaseCurrencyId { get; set; }

        public int PrimaryCurrencyId { get; set; }

        public string DefaultLanguage { get; set; }

        public string DefaultCurrencyRateProvider { get; set; }
    }
}