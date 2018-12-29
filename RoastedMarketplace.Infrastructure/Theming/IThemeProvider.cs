﻿namespace RoastedMarketplace.Infrastructure.Theming
{
    public interface IThemeProvider
    {
        ThemeInfo GetActiveTheme();

        string GetThemePath(string themeName);
    }
}