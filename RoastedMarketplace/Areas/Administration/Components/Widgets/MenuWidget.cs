﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RoastedMarketplace.Core.Plugins;
using RoastedMarketplace.Infrastructure.Helpers;
using RoastedMarketplace.Infrastructure.Mvc;
using RoastedMarketplace.Infrastructure.Mvc.Models;
using RoastedMarketplace.Infrastructure.ViewEngines.GlobalObjects;
using RoastedMarketplace.Services.Navigation;
using RoastedMarketplace.Services.Widgets;

namespace RoastedMarketplace.Areas.Administration.Components.Widgets
{
    [ViewComponent(Name = WidgetSystemName)]
    public class MenuWidget : FoundationComponent, IWidget
    {
        private const string WidgetSystemName = "Menu";
        private readonly IWidgetService _widgetService;
        private readonly IMenuService _menuService;
        public MenuWidget(IWidgetService widgetService, IMenuService menuService)
        {
            _widgetService = widgetService;
            _menuService = menuService;
        }

        public override IViewComponentResult Invoke(object data = null)
        {
            var dataAsDict = data as Dictionary<string, object>;
            if (dataAsDict == null)
                return R.Success.ComponentResult;
            var widgetId = dataAsDict["id"].ToString();
            var widgetSettings = _widgetService.LoadWidgetSettings<MenuWidgetSettings>(widgetId);
            var menu = _menuService.Get(widgetSettings.MenuId);
            if (menu == null)
                return R.Success.ComponentResult;
            var widgetNavigation = NavigationObject.GetNavigationImpl(menu.MenuItems, 0);
            return R.Success.With("title", widgetSettings.Title)
                .With("widgetNavigation", widgetNavigation)
                .With("widgetId", widgetId)
                .ComponentResult;
        }

        public string DisplayName { get; } = "Menu";

        public string SystemName { get; } = WidgetSystemName;

        public IList<string> WidgetZones { get; } = null;

        public bool HasConfiguration { get; } = true;

        public string ConfigurationUrl { get; } = null;

        public Type SettingsType { get; } = typeof(MenuWidgetSettings);

        public object GetViewObject(object settings)
        {
            var widgetSettings = settings as MenuWidgetSettings;
            var menus = _menuService.Get(x => true).ToList();
            var menusSelectList = SelectListHelper.GetSelectItemList(menus, x => x.Id, x => x.Name);
            return new
            {
                title = widgetSettings?.Title,
                menuId = widgetSettings?.MenuId,
                availableMenus = menusSelectList
            };
        }

        public class MenuWidgetSettings : WidgetSettingsModel
        {
            public string Title { get; set; }

            public int MenuId { get; set; }
        }
    }
}