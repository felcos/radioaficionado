using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using RadioAficionado.Aplicacion;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Infraestructura;
using RadioAficionado.Infraestructura.Postgres;
using RadioAficionado.Nativo.ModosDigitales;
using RadioAficionado.IA;
using RadioAficionado.Web.Data;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/radioaficionado-web-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // CORS — permitir que la app de escritorio se comunique con la API
    builder.Services.AddCors(opciones =>
    {
        opciones.AddPolicy("PermitirEscritorio", politica =>
        {
            politica.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    });

    // Servicios
    builder.Services.AddControllersWithViews();
    builder.Services.AgregarCapaDeAplicacion();
    builder.Services.AgregarCapaDeInfraestructura();
    builder.Services.AgregarModosDigitales();
    builder.Services.AgregarCapaDeIa();

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
    app.UseRouting();
    app.UseCors("PermitirEscritorio");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSerilogRequestLogging();

    app.MapControllers();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Inicio}/{action=Index}/{id?}");

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
