using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using RadioAficionado.Aplicacion;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Infraestructura;
using RadioAficionado.Infraestructura.Postgres;
using RadioAficionado.Nativo.ModosDigitales;
using RadioAficionado.IA;
using RadioAficionado.Web.Autenticacion;
using RadioAficionado.Web.Data;
using RadioAficionado.Web.Hubs;
using RadioAficionado.Web.Middleware;
using RadioAficionado.Web.Servicios;

// Configurar Serilog
string plantillaLog =
    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: plantillaLog)
    .WriteTo.File("logs/radioaficionado-web-.log", rollingInterval: RollingInterval.Day, outputTemplate: plantillaLog)
    .CreateLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Registrar Serilog.ILogger para componentes que lo inyectan directamente
    builder.Services.AddSingleton(Log.Logger);

    // CORS — permitir que la app de escritorio se comunique con la API
    builder.Services.AddCors(opciones =>
    {
        opciones.AddPolicy("PermitirEscritorio", politica =>
        {
            politica.WithOrigins(
                       "http://localhost:5200",
                       "https://localhost:5200",
                       "https://ham.felcos.es")
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    });

    // Servicios
    builder.Services.AddControllersWithViews();
    builder.Services.AddSignalR();
    builder.Services.AgregarCapaDeAplicacion();
    builder.Services.AgregarCapaDeInfraestructura();
    builder.Services.AgregarModosDigitales();
    builder.Services.AgregarCapaDeIa();

    // Servicios de tunelado remoto
    builder.Services.AddScoped<IServicioApiKeys, ServicioApiKeys>();
    builder.Services.AddSingleton<RegistroServiciosConectados>();
    builder.Services.AddSingleton<ControladorTimeoutPtt>();
    builder.Services.AddSingleton<MetricasConexion>();

    // PostgreSQL — cadena de conexión desde configuración (obligatoria)
    string cadenaConexion = builder.Configuration.GetConnectionString("RadioAficionado")
        ?? throw new InvalidOperationException(
            "La cadena de conexión 'RadioAficionado' no está configurada en appsettings.json. " +
            "Añade una sección ConnectionStrings con la clave 'RadioAficionado'.");
    builder.Services.AgregarPostgres(cadenaConexion);

    // ASP.NET Identity — contexto separado para no afectar la app de escritorio
    builder.Services.AddDbContext<ContextoIdentidadRadioAficionado>(opciones =>
    {
        opciones.UseNpgsql(cadenaConexion, npgsqlOpciones =>
            npgsqlOpciones.MigrationsAssembly("RadioAficionado.Infraestructura.Postgres"));
    });

    builder.Services.AddIdentity<UsuarioRadio, IdentityRole>(opciones =>
    {
        // Contraseña: razonable sin ser molesto
        opciones.Password.RequiredLength = 8;
        opciones.Password.RequireDigit = true;
        opciones.Password.RequireLowercase = true;
        opciones.Password.RequireUppercase = true;
        opciones.Password.RequireNonAlphanumeric = false;
        opciones.Password.RequiredUniqueChars = 4;

        // Bloqueo por intentos fallidos
        opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        opciones.Lockout.MaxFailedAccessAttempts = 5;
        opciones.Lockout.AllowedForNewUsers = true;

        // Usuario
        opciones.User.RequireUniqueEmail = true;
        opciones.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789/";

        // No requerir confirmación de email por ahora
        opciones.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ContextoIdentidadRadioAficionado>()
    .AddDefaultTokenProviders();

    // Esquema de autenticacion por clave de API (para el servicio local)
    builder.Services.AddAuthentication()
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.NombreEsquema, null);

    // Configurar cookie de autenticación
    builder.Services.ConfigureApplicationCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/IniciarSesion";
        opciones.LogoutPath = "/Cuenta/CerrarSesion";
        opciones.AccessDeniedPath = "/Cuenta/IniciarSesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opciones.ExpireTimeSpan = TimeSpan.FromDays(14);
        opciones.SlidingExpiration = true;
    });

    WebApplication app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Inicio/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseMiddleware<RateLimitingMiddleware>();
    app.UseRouting();
    app.UseCors("PermitirEscritorio");
    app.UseAuthentication();
    app.UseAuthorization();

    // Correlation ID: adjunta el identificador de traza de cada request a los logs (Art. 17)
    app.Use(async (contexto, siguiente) =>
    {
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", contexto.TraceIdentifier))
        {
            await siguiente();
        }
    });

    app.UseSerilogRequestLogging();

    app.MapControllers();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Inicio}/{action=Index}/{id?}");

    // Hubs de SignalR para tunelado remoto
    app.MapHub<HubTunelServicio>("/hubs/tunel-servicio");
    app.MapHub<HubRelayRig>("/hubs/relay-rig");

    // Endpoint de metricas (solo en desarrollo)
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/api/metricas", (MetricasConexion metricas) => Results.Ok(metricas.ObtenerSnapshot()));
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación web terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
