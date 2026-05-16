using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using RadioAficionado.Aplicacion;
using RadioAficionado.Infraestructura;
using RadioAficionado.Infraestructura.Persistencia;
using RadioAficionado.Infraestructura.Sqlite;
using RadioAficionado.Mobile.ViewModels;
using RadioAficionado.Mobile.Vistas;

namespace RadioAficionado.Mobile;

/// <summary>
/// Aplicación principal de Avalonia Mobile con inyección de dependencias.
/// Registra únicamente los servicios relevantes para la versión móvil:
/// logbook, propagación, mapa y configuración. No registra servicios de
/// hardware (rig, audio, rotador) que no aplican en dispositivos móviles.
/// </summary>
public class App : Application
{
    /// <summary>
    /// Contenedor de servicios de la aplicación móvil.
    /// </summary>
    public static ServiceProvider? Servicios { get; private set; }

    /// <summary>
    /// Inicializa los recursos XAML de la aplicación.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Configura Serilog, registra todos los servicios en el contenedor de DI
    /// y lanza la vista principal móvil.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/radioaficionado-mobile-.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        ServiceCollection coleccion = new ServiceCollection();
        ConfigurarServicios(coleccion);
        Servicios = coleccion.BuildServiceProvider();

        AplicarMigraciones(Servicios);

        if (ApplicationLifetime is ISingleViewApplicationLifetime vistaUnica)
        {
            vistaUnica.MainView = new VentanaPrincipalMobile();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Aplica las migraciones pendientes de Entity Framework Core al iniciar la aplicación.
    /// Si la migración falla, registra el error y continúa (la app puede funcionar parcialmente).
    /// </summary>
    /// <param name="proveedor">Proveedor de servicios de la aplicación.</param>
    private static void AplicarMigraciones(ServiceProvider proveedor)
    {
        try
        {
            using IServiceScope scope = proveedor.CreateScope();
            ContextoRadioAficionado contexto = scope.ServiceProvider
                .GetRequiredService<ContextoRadioAficionado>();
            contexto.Database.Migrate();
            Log.Information("Migraciones de base de datos aplicadas correctamente en mobile.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al aplicar migraciones de base de datos al iniciar la aplicación móvil.");
        }
    }

    /// <summary>
    /// Registra los servicios necesarios para la versión móvil.
    /// Excluye servicios de hardware: IControlRig, IControlRotador, IAudioPipeline, IServicioWaterfall.
    /// </summary>
    /// <param name="servicios">Colección de servicios del contenedor de DI.</param>
    private static void ConfigurarServicios(IServiceCollection servicios)
    {
        // Logging
        servicios.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        // Capas de aplicación e infraestructura
        servicios.AgregarCapaDeAplicacion();
        servicios.AgregarCapaDeInfraestructura();
        servicios.AgregarSqlite();

        // ViewModels móviles
        servicios.AddTransient<VentanaPrincipalMobileViewModel>();
        servicios.AddTransient<PanelLogbookMobileViewModel>();
        servicios.AddTransient<PanelMapaMobileViewModel>();
        servicios.AddTransient<PanelPropagacionMobileViewModel>();
    }
}
