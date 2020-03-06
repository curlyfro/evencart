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

using EvenCart.Areas.Administration.Models.Common;
using EvenCart.Data.Entity.Common;

namespace EvenCart.Areas.Administration.Factories.Common
{
    public class CustomLabelModelFactory : ICustomLabelModelFactory
    {
        public CustomLabelModel Create(CustomLabel entity)
        {
            return new CustomLabelModel()
            {
                Id = entity.Id,
                DisplayOrder = entity.DisplayOrder,
                LabelType = entity.Type,
                Text = entity.Text
            };
        }
    }
}