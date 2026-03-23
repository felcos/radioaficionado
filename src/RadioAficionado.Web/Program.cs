using Serilog;
using RadioAficionado.Aplicacion;
using RadioAficionado.Infraestructura;
using RadioAficionado.Infraestructura.Postgres;

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

    // Servicios
    builder.Services.AddControllersWithViews();
    builder.Services.AgregarCapaDeAplicacion();
    builder.Services.AgregarCapaDeInfraestructura();

    // PostgreSQL — cadena de conexión desde configuración, con valor por defecto para desarrollo
    string cadenaConexion = builder.Configuration.GetConnectionString("RadioAficionado")
        ?? "Host=localhost;Database=radioaficionado;Username=postgres;Password=postgres";
    builder.Services.AgregarPostgres(cadenaConexion);

    WebApplication app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Inicio/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();
    app.UseSerilogRequestLogging();

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
