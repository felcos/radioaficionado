using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Configuracion;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Infraestructura.Configuracion;

/// <summary>
/// Implementación de <see cref="IServicioConfiguracion"/> que persiste la configuración
/// en un archivo JSON dentro de la carpeta de datos del usuario.
/// Ruta: %APPDATA%/RadioAficionado/configuracion.json (Windows)
///       ~/.config/RadioAficionado/configuracion.json (Linux/Mac)
/// </summary>
public sealed class ServicioConfiguracionJson : IServicioConfiguracion
{
    private readonly string _rutaArchivo;
    private readonly ILogger<ServicioConfiguracionJson> _logger;

    private static readonly JsonSerializerOptions _opcionesJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Crea una nueva instancia del servicio de configuración JSON.
    /// </summary>
    /// <param name="logger">Logger para registrar operaciones y errores.</param>
    public ServicioConfiguracionJson(ILogger<ServicioConfiguracionJson> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        string carpetaDatos = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string carpetaApp = Path.Combine(carpetaDatos, "RadioAficionado");
        _rutaArchivo = Path.Combine(carpetaApp, "configuracion.json");
    }

    /// <summary>
    /// Crea una nueva instancia del servicio de configuración JSON con ruta personalizada.
    /// Útil para tests.
    /// </summary>
    /// <param name="rutaArchivo">Ruta completa al archivo de configuración.</param>
    /// <param name="logger">Logger para registrar operaciones y errores.</param>
    public ServicioConfiguracionJson(string rutaArchivo, ILogger<ServicioConfiguracionJson> logger)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            throw new ArgumentException("La ruta del archivo no puede ser nula ni vacía.", nameof(rutaArchivo));
        }

        _rutaArchivo = rutaArchivo;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ConfiguracionCompleta> CargarAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_rutaArchivo))
        {
            _logger.LogInformation("Archivo de configuración no encontrado en {Ruta}. Se usarán valores por defecto.", _rutaArchivo);
            return new ConfiguracionCompleta();
        }

        try
        {
            string json = await File.ReadAllTextAsync(_rutaArchivo, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Archivo de configuración vacío en {Ruta}. Se usarán valores por defecto.", _rutaArchivo);
                return new ConfiguracionCompleta();
            }

            ConfiguracionCompleta? configuracion = JsonSerializer.Deserialize<ConfiguracionCompleta>(json, _opcionesJson);

            if (configuracion is null)
            {
                _logger.LogWarning("No se pudo deserializar la configuración de {Ruta}. Se usarán valores por defecto.", _rutaArchivo);
                return new ConfiguracionCompleta();
            }

            _logger.LogInformation("Configuración cargada exitosamente desde {Ruta}.", _rutaArchivo);
            return configuracion;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error al deserializar la configuración de {Ruta}. Se usarán valores por defecto.", _rutaArchivo);
            return new ConfiguracionCompleta();
        }
    }

    /// <inheritdoc />
    public async Task GuardarAsync(ConfiguracionCompleta configuracion, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        string? directorio = Path.GetDirectoryName(_rutaArchivo);

        if (!string.IsNullOrWhiteSpace(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
            _logger.LogInformation("Directorio de configuración creado: {Directorio}.", directorio);
        }

        string json = JsonSerializer.Serialize(configuracion, _opcionesJson);
        await File.WriteAllTextAsync(_rutaArchivo, json, ct).ConfigureAwait(false);

        _logger.LogInformation("Configuración guardada exitosamente en {Ruta}.", _rutaArchivo);
    }
}
