using System.Windows;

namespace MotorDebugStudio.Services;

public static class ThemeManager
{
    private const string DarkThemePath = "Themes/Theme.Dark.xaml";
    private const string LightThemePath = "Themes/Theme.Light.xaml";

    public static void ApplyTheme(bool useDarkTheme)
    {
        var appResources = Application.Current.Resources.MergedDictionaries;
        if (appResources.Count == 0)
        {
            return;
        }

        appResources[0] = new ResourceDictionary
        {
            Source = new Uri(useDarkTheme ? DarkThemePath : LightThemePath, UriKind.Relative)
        };
    }
}
