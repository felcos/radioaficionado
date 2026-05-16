using Android.App;
using Android.Content;
using Android.OS;

namespace RadioAficionado.Mobile.Android;

/// <summary>
/// Actividad de splash screen que se muestra mientras la aplicación carga.
/// Redirige automáticamente a la actividad principal de Avalonia.
/// </summary>
[Activity(
    Label = "RadioAficionado",
    Theme = "@style/MyTheme.Splash",
    MainLauncher = true,
    NoHistory = true)]
public class SplashActivity : Activity
{
    /// <summary>
    /// Inicia la actividad principal inmediatamente al crear el splash.
    /// </summary>
    /// <param name="savedInstanceState">Estado guardado de la instancia.</param>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Intent? intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
        StartActivity(intent);
        Finish();
    }
}
