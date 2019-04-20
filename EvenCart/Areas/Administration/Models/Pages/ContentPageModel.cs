﻿using System;
using EvenCart.Areas.Administration.Models.Users;
using EvenCart.Infrastructure.Mvc.Models;
using EvenCart.Infrastructure.Mvc.Validator;
using FluentValidation;

namespace EvenCart.Areas.Administration.Models.Pages
{
    public class ContentPageModel : FoundationEntityModel, IRequiresValidations<ContentPageModel>
    {
        public string Name { get; set; }

        public string Content { get; set; }

        public bool Published { get; set; }

        public bool Private { get; set; }

        public string Password { get; set; }

        public string SystemName { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime UpdatedOn { get; set; }

        public DateTime PublishedOn { get; set; }

        public int UserId { get; set; }


        #region Virtual Properties
        public SeoMetaModel SeoMeta { get; set; }

        public UserMiniModel User { get; set; }
        #endregion

        public void SetupValidationRules(ModelValidator<ContentPageModel> v)
        {
            v.RuleFor(x => x.Name).NotEmpty();
        }
    }
}