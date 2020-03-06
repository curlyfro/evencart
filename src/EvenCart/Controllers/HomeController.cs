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

using System.Threading.Tasks;
using EvenCart.Events;
using EvenCart.Infrastructure.Mvc;
using EvenCart.Infrastructure.Mvc.Attributes;
using EvenCart.Infrastructure.Routing;
using EvenCart.Models.Home;
using Microsoft.AspNetCore.Mvc;

namespace EvenCart.Controllers
{
    public class HomeController : FoundationController
    {
        [HttpGet("~/", Name = RouteNames.Home)]
        public async Task<IActionResult> Index()
        {
            return R.Success.Result;
        }

        [DualPost("~/contact-us", Name = RouteNames.ContactUs, OnlyApi = true)]
        [ValidateModelState(ModelType = typeof(ContactUsModel))]
        public IActionResult ContactUs(ContactUsModel requestModel)
        {
            RaiseEvent(NamedEvent.ContactUs, requestModel);
            return R.Success.Result;
        }

    }
}