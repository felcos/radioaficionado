using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace RadioAficionado.Mobile.Android;

/// <summary>
/// Actividad principal de Android que hospeda la aplicación Avalonia.
/// </summary>
[Activity(
    Label = "RadioAficionado",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation |
                           ConfigChanges.ScreenSize |
                           ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
