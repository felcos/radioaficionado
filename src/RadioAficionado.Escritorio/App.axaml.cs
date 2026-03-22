using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Escritorio.Vistas;

namespace RadioAficionado.Escritorio;

/// <summary>
/// Aplicación principal de Avalonia con inyección de dependencias.
/// </summary>
public class App : Application
{
    /// <summary>
    /// Contenedor de servicios de la aplicación.
    /// </summary>
    public static ServiceProvider? Servicios { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection coleccion = new ServiceCollection();
        ConfigurarServicios(coleccion);
        Servicios = coleccion.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime escritorio)
        {
            escritorio.MainWindow = new VentanaPrincipal();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigurarServicios(IServiceCollection servicios)
    {
        // TODO: Registrar servicios de dominio, aplicación e infraestructura
    }
}
