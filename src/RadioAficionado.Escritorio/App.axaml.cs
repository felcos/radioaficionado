using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using RadioAficionado.Aplicacion;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura;
using RadioAficionado.Infraestructura.Sqlite;
using RadioAficionado.Nativo.Audio;
using RadioAficionado.Nativo.Rig;
using RadioAficionado.Nativo.Rotador;
using RadioAficionado.Escritorio.ViewModels;
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

    /// <summary>
    /// Inicializa los recursos XAML de la aplicación.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Configura Serilog, registra todos los servicios en el contenedor de DI
    /// y lanza la ventana principal.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        // Configurar Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/radioaficionado-.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

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

        // Servicios nativos
        servicios.AddSingleton<IControlRig>(sp =>
        {
            ClienteRigctld cliente = new ClienteRigctld();
            return cliente;
        });

        servicios.AddSingleton<IControlRotador>(sp =>
        {
            ClienteRotctld cliente = new ClienteRotctld();
            return cliente;
        });

        servicios.AddSingleton<IAudioPipeline, PipelineAudioNAudio>();

        // ViewModels
        servicios.AddTransient<VentanaPrincipalViewModel>();
        servicios.AddTransient<PanelRigViewModel>();
        servicios.AddTransient<PanelMensajesViewModel>();
        servicios.AddTransient<PanelRegistroQsoViewModel>();
        servicios.AddTransient<PanelLogbookViewModel>();
        servicios.AddTransient<PanelDxClusterViewModel>();
        servicios.AddTransient<ConfiguracionViewModel>();
    }
}
