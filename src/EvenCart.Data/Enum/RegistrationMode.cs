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

using System.ComponentModel;

namespace EvenCart.Data.Enum
{
    public enum RegistrationMode
    {
        [Description("Immediate")]
        Immediate,
        [Description("With activation email to the user")]
        WithActivationEmail,
        [Description("Manually approve the registered user")]
        ManualApproval,
        [Description("Only users with invitation link will be allowed to register")]
        InviteOnly,
        [Description("Disable Registrations")]
        Disabled
    }
}