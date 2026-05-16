using Avalonia;
using Avalonia.iOS;
using Foundation;
using RadioAficionado.Mobile;

namespace RadioAficionado.Mobile.iOS;

/// <summary>
/// Delegado de la aplicación iOS que hospeda la aplicación Avalonia.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<App>
{
    /// <summary>
    /// Configura la instancia de Avalonia con los parámetros específicos de iOS.
    /// </summary>
    /// <param name="builder">Builder de la aplicación Avalonia.</param>
    /// <returns>AppBuilder configurado.</returns>
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder);
    }
}
