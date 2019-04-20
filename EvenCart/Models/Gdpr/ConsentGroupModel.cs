﻿using System.Collections.Generic;
using EvenCart.Infrastructure.Mvc.Models;

namespace EvenCart.Models.Gdpr
{
    public class ConsentGroupModel : FoundationEntityModel
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public IList<ConsentModel> Consents { get; set; }
    }
}