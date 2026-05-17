using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using RadioAficionado.Aplicacion;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura;
using RadioAficionado.Infraestructura.Persistencia;
using RadioAficionado.Infraestructura.Sqlite;
using RadioAficionado.Nativo.Audio;
using RadioAficionado.Nativo.Dsp;
using RadioAficionado.Nativo.ModosDigitales;
using RadioAficionado.Nativo.Rig;
using RadioAficionado.Nativo.Rotador;
using RadioAficionado.Nativo.Sdr;
using RadioAficionado.IA;
using RadioAficionado.Servicio.Hubs;
using RadioAficionado.Servicio.Remoto;
using RadioAficionado.Servicio.Servicios;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/radioaficionado-servicio-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Registrar Serilog.ILogger para componentes que lo inyectan directamente
    builder.Services.AddSingleton(Log.Logger);

    // ------------------------------------------------------------------
    // Localizacion (i18n)
    // ------------------------------------------------------------------
    builder.Services.AddLocalization(opciones => opciones.ResourcesPath = "Resources");

    // ------------------------------------------------------------------
    // Servicios MVC + SignalR
    // ------------------------------------------------------------------
    builder.Services.AddControllersWithViews()
        .AddViewLocalization()
        .AddDataAnnotationsLocalization();
    builder.Services.AddSignalR();

    // ------------------------------------------------------------------
    // Capas de aplicacion e infraestructura (mismas que App.axaml.cs)
    // ------------------------------------------------------------------
    builder.Services.AgregarCapaDeAplicacion();
    builder.Services.AgregarCapaDeInfraestructura();
    builder.Services.AgregarSqlite();

    // ------------------------------------------------------------------
    // Servicios nativos (rig, audio, waterfall, DSP, SDR, modos digitales, IA)
    // ------------------------------------------------------------------
    builder.Services.AddSingleton<IControlRotador>(sp =>
    {
        ClienteRotctld cliente = new ClienteRotctld();
        return cliente;
    });

    builder.Services.AddSingleton<IAudioPipeline, PipelineAudioNAudio>();
    builder.Services.AddSingleton<IServicioWaterfall, ServicioWaterfall>();
    builder.Services.AgregarCapaDeSdr();
    builder.Services.AgregarModosDigitales();
    builder.Services.AgregarCapaDeIa();

    // ------------------------------------------------------------------
    // Servicio de estado de operacion (singleton - estado global del rig)
    // ------------------------------------------------------------------
    builder.Services.AddSingleton<ServicioEstadoOperacion>();
    builder.Services.AddSingleton<IServicioOperacionDigital, ServicioOperacionDigital>();
    builder.Services.AddHostedService<ServidorUdpWsjtx>();
    builder.Services.AddHostedService<ClienteDxClusterTelnet>();

    // ------------------------------------------------------------------
    // Conexion remota al servidor web (tunelado SignalR)
    // ------------------------------------------------------------------
    builder.Services.Configure<ConfiguracionRemoto>(
        builder.Configuration.GetSection("Remoto"));
    builder.Services.AddHostedService<ClienteRelaySignalR>();

    // ------------------------------------------------------------------
    // Health check para el lanzador WebView2
    // ------------------------------------------------------------------
    builder.Services.AddHealthChecks();

    // ------------------------------------------------------------------
    // Kestrel en puerto 5200 por defecto
    // ------------------------------------------------------------------
    builder.WebHost.UseUrls("http://localhost:5200");

    WebApplication app = builder.Build();

    // Aplicar migraciones SQLite
    try
    {
        using IServiceScope scope = app.Services.CreateScope();
        ContextoRadioAficionado contexto = scope.ServiceProvider
            .GetRequiredService<ContextoRadioAficionado>();
        contexto.Database.Migrate();
        Log.Information("Migraciones de base de datos aplicadas correctamente.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error al aplicar migraciones de base de datos.");
    }

    // Suscribir eventos del ServicioEstadoOperacion a SignalR
    ServicioEstadoOperacion estadoOperacion = app.Services.GetRequiredService<ServicioEstadoOperacion>();

    estadoOperacion.EstadoCambiado += (sender, dto) =>
    {
        IHubContext<HubRig, IClienteHubRig> hubRig =
            app.Services.GetRequiredService<IHubContext<HubRig, IClienteHubRig>>();
        _ = hubRig.Clients.All.RecibirEstadoRig(dto);
    };

    estadoOperacion.ConexionCambiada += (sender, args) =>
    {
        IHubContext<HubRig, IClienteHubRig> hubRig =
            app.Services.GetRequiredService<IHubContext<HubRig, IClienteHubRig>>();
        _ = hubRig.Clients.All.RecibirConexionCambiada(args.Conectado, args.Detalle);
    };

    // ------------------------------------------------------------------
    // Middleware
    // ------------------------------------------------------------------
    app.UseStaticFiles();
    app.UseRouting();

    // Localizacion: es (por defecto) y en, detectado por ruta
    CultureInfo culturaEs = new CultureInfo("es");
    CultureInfo culturaEn = new CultureInfo("en");
    List<CultureInfo> culturasSoportadas = new List<CultureInfo> { culturaEs, culturaEn };

    RequestLocalizationOptions opcionesLocalizacion = new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(culturaEs),
        SupportedCultures = culturasSoportadas,
        SupportedUICultures = culturasSoportadas
    };

    opcionesLocalizacion.RequestCultureProviders.Insert(0, new RouteDataRequestCultureProvider
    {
        RouteDataStringKey = "cultura",
        UIRouteDataStringKey = "cultura"
    });

    app.UseRequestLocalization(opcionesLocalizacion);

    app.UseSerilogRequestLogging();

    // ------------------------------------------------------------------
    // Endpoints
    // ------------------------------------------------------------------
    app.MapHealthChecks("/health");

    app.MapHub<HubRig>("/hubs/rig");
    app.MapHub<HubWaterfall>("/hubs/waterfall");
    app.MapHub<HubDecodificaciones>("/hubs/decodificaciones");
    app.MapHub<HubEstado>("/hubs/estado");

    // Ruta con prefijo de cultura: /es/Operacion/Index, /en/Operacion/Index
    app.MapControllerRoute(
        name: "localizada",
        pattern: "{cultura:regex(^(es|en)$)}/{controller=Operacion}/{action=Index}/{id?}");

    // Ruta sin prefijo de cultura (espanol por defecto)
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Operacion}/{action=Index}/{id?}",
        defaults: new { cultura = "es" });

    Log.Information("RadioAficionado.Servicio iniciado en http://localhost:5200");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "RadioAficionado.Servicio termino inesperadamente.");
}
finally
{
    Log.CloseAndFlush();
}
