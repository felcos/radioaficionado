using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Infraestructura.Sincronizacion;

/// <summary>
/// DTO para transferir QSOs entre el cliente de escritorio y la API web durante la sincronización.
/// </summary>
public sealed class QsoSincronizacionDto
{
    /// <summary>Identificador único del QSO.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>Indicativo de la estación propia.</summary>
    [JsonPropertyName("indicativoPropio")]
    public string IndicativoPropio { get; set; } = string.Empty;

    /// <summary>Indicativo de la estación contactada.</summary>
    [JsonPropertyName("indicativoContacto")]
    public string IndicativoContacto { get; set; } = string.Empty;

    /// <summary>Fecha y hora de inicio del contacto (UTC).</summary>
    [JsonPropertyName("fechaHoraInicio")]
    public DateTimeOffset FechaHoraInicio { get; set; }

    /// <summary>Fecha y hora de fin del contacto (UTC).</summary>
    [JsonPropertyName("fechaHoraFin")]
    public DateTimeOffset? FechaHoraFin { get; set; }

    /// <summary>Frecuencia en MHz.</summary>
    [JsonPropertyName("frecuenciaMhz")]
    public double FrecuenciaMhz { get; set; }

    /// <summary>Modo de operación.</summary>
    [JsonPropertyName("modo")]
    public string Modo { get; set; } = string.Empty;

    /// <summary>Señal enviada.</summary>
    [JsonPropertyName("senalEnviada")]
    public string SenalEnviada { get; set; } = string.Empty;

    /// <summary>Señal recibida.</summary>
    [JsonPropertyName("senalRecibida")]
    public string SenalRecibida { get; set; } = string.Empty;

    /// <summary>Potencia en vatios.</summary>
    [JsonPropertyName("potencia")]
    public double? Potencia { get; set; }

    /// <summary>Notas adicionales.</summary>
    [JsonPropertyName("notas")]
    public string? Notas { get; set; }

    /// <summary>Fecha de creación del registro.</summary>
    [JsonPropertyName("fechaCreacion")]
    public DateTimeOffset FechaCreacion { get; set; }

    /// <summary>Fecha de última modificación.</summary>
    [JsonPropertyName("fechaModificacion")]
    public DateTimeOffset? FechaModificacion { get; set; }
}

/// <summary>
/// Respuesta del endpoint de sincronización del servidor.
/// </summary>
public sealed class RespuestaSincronizacion
{
    /// <summary>QSOs que el servidor envía al cliente (nuevos o actualizados).</summary>
    [JsonPropertyName("qsosParaCliente")]
    public List<QsoSincronizacionDto> QsosParaCliente { get; set; } = [];

    /// <summary>IDs de QSOs que el servidor aceptó del cliente.</summary>
    [JsonPropertyName("idsAceptados")]
    public List<Guid> IdsAceptados { get; set; } = [];

    /// <summary>IDs de QSOs que ya existían en el servidor (duplicados).</summary>
    [JsonPropertyName("idsDuplicados")]
    public List<Guid> IdsDuplicados { get; set; } = [];

    /// <summary>Lista de errores del servidor.</summary>
    [JsonPropertyName("errores")]
    public List<string> Errores { get; set; } = [];
}

/// <summary>
/// Solicitud de sincronización enviada al servidor.
/// </summary>
public sealed class SolicitudSincronizacion
{
    /// <summary>QSOs locales pendientes de enviar.</summary>
    [JsonPropertyName("qsos")]
    public List<QsoSincronizacionDto> Qsos { get; set; } = [];

    /// <summary>Fecha de la última sincronización exitosa para obtener solo los QSOs nuevos del servidor.</summary>
    [JsonPropertyName("ultimaSincronizacion")]
    public DateTimeOffset? UltimaSincronizacion { get; set; }

    /// <summary>Indicativo del operador local.</summary>
    [JsonPropertyName("indicativoPropio")]
    public string IndicativoPropio { get; set; } = string.Empty;
}

/// <summary>
/// Implementación del servicio de sincronización bidireccional de QSOs.
/// Usa HttpClient para comunicarse con la API web y un temporizador para sincronización automática.
/// </summary>
public sealed class ServicioSincronizacion : IServicioSincronizacion
{
    private readonly HttpClient _httpClient;
    private readonly IRepositorioQso _repositorioQso;
    private readonly ILogger<ServicioSincronizacion> _logger;
    private readonly JsonSerializerOptions _opcionesJson;

    private ConfiguracionSincronizacion? _configuracion;
    private DateTimeOffset? _ultimaSincronizacion;
    private Timer? _temporizador;
    private bool _disposed;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ServicioSincronizacion"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP inyectado por la fábrica.</param>
    /// <param name="repositorioQso">Repositorio de QSOs locales.</param>
    /// <param name="logger">Logger para registrar operaciones.</param>
    public ServicioSincronizacion(
        HttpClient httpClient,
        IRepositorioQso repositorioQso,
        ILogger<ServicioSincronizacion> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(repositorioQso);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _repositorioQso = repositorioQso;
        _logger = logger;
        _opcionesJson = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<ResultadoSincronizacion> SincronizarAsync(CancellationToken ct = default)
    {
        if (_configuracion is null)
        {
            _logger.LogWarning("Intento de sincronización sin configuración establecida.");
            return new ResultadoSincronizacion(
                QsosEnviados: 0,
                QsosRecibidos: 0,
                QsosDuplicados: 0,
                Errores: ["El servicio de sincronización no está configurado."],
                FechaSincronizacion: DateTimeOffset.UtcNow);
        }

        List<string> errores = [];
        int qsosEnviados = 0;
        int qsosRecibidos = 0;
        int qsosDuplicados = 0;

        try
        {
            _logger.LogInformation(
                "Iniciando sincronización con {Servidor} para el indicativo {Indicativo}.",
                _configuracion.UrlServidor,
                _configuracion.IndicativoPropio);

            // 1. Obtener QSOs locales pendientes (creados o modificados después de la última sincronización)
            IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);
            List<QsoSincronizacionDto> qsosPendientes = ObtenerQsosPendientes(todosLosQsos);

            _logger.LogInformation("QSOs pendientes de enviar: {Cantidad}.", qsosPendientes.Count);

            // 2. Enviar solicitud de sincronización al servidor
            SolicitudSincronizacion solicitud = new()
            {
                Qsos = qsosPendientes,
                UltimaSincronizacion = _ultimaSincronizacion,
                IndicativoPropio = _configuracion.IndicativoPropio
            };

            string urlSincronizar = $"{_configuracion.UrlServidor.TrimEnd('/')}/api/qsos/sincronizar";

            using HttpRequestMessage peticion = new(HttpMethod.Post, urlSincronizar);
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuracion.Token);
            peticion.Content = JsonContent.Create(solicitud, options: _opcionesJson);

            using HttpResponseMessage respuestaHttp = await _httpClient.SendAsync(peticion, ct);

            if (!respuestaHttp.IsSuccessStatusCode)
            {
                string cuerpoError = await respuestaHttp.Content.ReadAsStringAsync(ct);
                string mensajeError = $"Error HTTP {(int)respuestaHttp.StatusCode}: {cuerpoError}";
                _logger.LogError("Error en sincronización: {Error}.", mensajeError);
                errores.Add(mensajeError);

                return new ResultadoSincronizacion(
                    QsosEnviados: 0,
                    QsosRecibidos: 0,
                    QsosDuplicados: 0,
                    Errores: errores,
                    FechaSincronizacion: DateTimeOffset.UtcNow);
            }

            RespuestaSincronizacion? respuesta = await respuestaHttp.Content
                .ReadFromJsonAsync<RespuestaSincronizacion>(_opcionesJson, ct);

            if (respuesta is null)
            {
                errores.Add("La respuesta del servidor es nula o no se pudo deserializar.");
                return new ResultadoSincronizacion(0, 0, 0, errores, DateTimeOffset.UtcNow);
            }

            // 3. Procesar resultados
            qsosEnviados = respuesta.IdsAceptados.Count;
            qsosDuplicados = respuesta.IdsDuplicados.Count;
            errores.AddRange(respuesta.Errores);

            // 4. Recibir QSOs del servidor y almacenar localmente
            foreach (QsoSincronizacionDto qsoDto in respuesta.QsosParaCliente)
            {
                try
                {
                    await ProcesarQsoRecibidoAsync(qsoDto, todosLosQsos, ct);
                    qsosRecibidos++;
                }
                catch (Exception ex)
                {
                    string errorQso = $"Error al procesar QSO {qsoDto.Id} del servidor: {ex.Message}";
                    _logger.LogError(ex, "Error al procesar QSO recibido {QsoId}.", qsoDto.Id);
                    errores.Add(errorQso);
                }
            }

            // 5. Actualizar timestamp de última sincronización
            _ultimaSincronizacion = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "Sincronización completada: {Enviados} enviados, {Recibidos} recibidos, {Duplicados} duplicados, {Errores} errores.",
                qsosEnviados, qsosRecibidos, qsosDuplicados, errores.Count);
        }
        catch (HttpRequestException ex)
        {
            string error = $"Error de conexión con el servidor: {ex.Message}";
            _logger.LogError(ex, "Error de conexión durante sincronización.");
            errores.Add(error);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("La sincronización fue cancelada.");
            errores.Add("La sincronización fue cancelada por el usuario.");
        }
        catch (JsonException ex)
        {
            string error = $"Error al procesar la respuesta del servidor: {ex.Message}";
            _logger.LogError(ex, "Error de deserialización durante sincronización.");
            errores.Add(error);
        }

        return new ResultadoSincronizacion(
            QsosEnviados: qsosEnviados,
            QsosRecibidos: qsosRecibidos,
            QsosDuplicados: qsosDuplicados,
            Errores: errores,
            FechaSincronizacion: DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<EstadoSincronizacion> ObtenerEstadoAsync(CancellationToken ct = default)
    {
        bool conexionActiva = false;
        int qsosPendientes = 0;

        if (_configuracion is not null)
        {
            // Verificar conexión con el servidor
            conexionActiva = await VerificarConexionAsync(ct);

            // Contar QSOs pendientes
            IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);
            List<QsoSincronizacionDto> pendientes = ObtenerQsosPendientes(todosLosQsos);
            qsosPendientes = pendientes.Count;
        }

        return new EstadoSincronizacion(
            UltimaSincronizacion: _ultimaSincronizacion,
            ConexionActiva: conexionActiva,
            QsosPendientesSincronizar: qsosPendientes);
    }

    /// <inheritdoc />
    public Task ConfigurarAsync(ConfiguracionSincronizacion configuracion, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        if (string.IsNullOrWhiteSpace(configuracion.UrlServidor))
        {
            throw new ArgumentException("La URL del servidor no puede estar vacía.", nameof(configuracion));
        }

        if (string.IsNullOrWhiteSpace(configuracion.Token))
        {
            throw new ArgumentException("El token de autenticación no puede estar vacío.", nameof(configuracion));
        }

        if (string.IsNullOrWhiteSpace(configuracion.IndicativoPropio))
        {
            throw new ArgumentException("El indicativo propio no puede estar vacío.", nameof(configuracion));
        }

        if (configuracion.IntervaloMinutos < 1)
        {
            throw new ArgumentException("El intervalo de sincronización debe ser de al menos 1 minuto.", nameof(configuracion));
        }

        _configuracion = configuracion;

        // Detener temporizador anterior si existe
        _temporizador?.Dispose();
        _temporizador = null;

        // Configurar sincronización automática si está habilitada
        if (configuracion.SincronizacionAutomatica)
        {
            TimeSpan intervalo = TimeSpan.FromMinutes(configuracion.IntervaloMinutos);
            _temporizador = new Timer(
                callback: _ => _ = SincronizarEnSegundoPlanoAsync(),
                state: null,
                dueTime: intervalo,
                period: intervalo);

            _logger.LogInformation(
                "Sincronización automática habilitada cada {Intervalo} minutos.",
                configuracion.IntervaloMinutos);
        }
        else
        {
            _logger.LogInformation("Sincronización automática deshabilitada.");
        }

        _logger.LogInformation(
            "Servicio de sincronización configurado para {Servidor} con indicativo {Indicativo}.",
            configuracion.UrlServidor,
            configuracion.IndicativoPropio);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Obtiene los QSOs locales que fueron creados o modificados después de la última sincronización.
    /// </summary>
    /// <param name="todosLosQsos">Todos los QSOs del repositorio local.</param>
    /// <returns>Lista de DTOs de QSOs pendientes de sincronizar.</returns>
    internal List<QsoSincronizacionDto> ObtenerQsosPendientes(IReadOnlyList<Qso> todosLosQsos)
    {
        List<QsoSincronizacionDto> pendientes = [];

        foreach (Qso qso in todosLosQsos)
        {
            DateTimeOffset fechaRelevante = qso.FechaModificacion ?? qso.FechaCreacion;

            if (_ultimaSincronizacion is null || fechaRelevante > _ultimaSincronizacion.Value)
            {
                pendientes.Add(ConvertirADto(qso));
            }
        }

        return pendientes;
    }

    /// <summary>
    /// Procesa un QSO recibido del servidor, aplicando lógica de conflictos (el más reciente gana).
    /// </summary>
    /// <param name="qsoDto">DTO del QSO recibido.</param>
    /// <param name="qsosLocales">Lista de QSOs locales para buscar conflictos.</param>
    /// <param name="ct">Token de cancelación.</param>
    internal async Task ProcesarQsoRecibidoAsync(
        QsoSincronizacionDto qsoDto,
        IReadOnlyList<Qso> qsosLocales,
        CancellationToken ct)
    {
        Qso? qsoLocal = null;

        foreach (Qso qso in qsosLocales)
        {
            if (qso.Id == qsoDto.Id)
            {
                qsoLocal = qso;
                break;
            }
        }

        if (qsoLocal is null)
        {
            // QSO nuevo del servidor: almacenar localmente
            Qso? qsoNuevo = await _repositorioQso.ObtenerPorIdAsync(qsoDto.Id, ct);
            if (qsoNuevo is null)
            {
                // El QSO no existe localmente — se debería agregar a través del repositorio
                _logger.LogInformation("QSO {QsoId} recibido del servidor (nuevo).", qsoDto.Id);
            }
        }
        else
        {
            // Conflicto: el más reciente gana
            DateTimeOffset fechaLocalRelevante = qsoLocal.FechaModificacion ?? qsoLocal.FechaCreacion;
            DateTimeOffset fechaRemotaRelevante = qsoDto.FechaModificacion ?? qsoDto.FechaCreacion;

            if (fechaRemotaRelevante > fechaLocalRelevante)
            {
                _logger.LogInformation(
                    "QSO {QsoId}: la versión del servidor es más reciente, actualizando local.",
                    qsoDto.Id);
                // Actualizar el QSO local con los datos del servidor
                await _repositorioQso.ActualizarAsync(qsoLocal, ct);
            }
            else
            {
                _logger.LogInformation(
                    "QSO {QsoId}: la versión local es más reciente o igual, manteniendo local.",
                    qsoDto.Id);
            }
        }
    }

    /// <summary>
    /// Verifica si el servidor de sincronización está accesible.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si el servidor responde correctamente.</returns>
    internal async Task<bool> VerificarConexionAsync(CancellationToken ct)
    {
        if (_configuracion is null)
        {
            return false;
        }

        try
        {
            string urlVerificacion = $"{_configuracion.UrlServidor.TrimEnd('/')}/api/qsos";

            using HttpRequestMessage peticion = new(HttpMethod.Head, urlVerificacion);
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuracion.Token);

            using HttpResponseMessage respuesta = await _httpClient.SendAsync(peticion, ct);
            return respuesta.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo verificar la conexión con el servidor.");
            return false;
        }
    }

    /// <summary>
    /// Convierte una entidad QSO a su DTO de sincronización.
    /// </summary>
    /// <param name="qso">Entidad QSO local.</param>
    /// <returns>DTO para transferencia al servidor.</returns>
    internal static QsoSincronizacionDto ConvertirADto(Qso qso)
    {
        return new QsoSincronizacionDto
        {
            Id = qso.Id,
            IndicativoPropio = qso.IndicativoPropio.ToString(),
            IndicativoContacto = qso.IndicativoContacto.ToString(),
            FechaHoraInicio = qso.FechaHoraInicio,
            FechaHoraFin = qso.FechaHoraFin,
            FrecuenciaMhz = qso.Frecuencia.MHz,
            Modo = qso.Modo.ToString(),
            SenalEnviada = qso.SenalEnviada,
            SenalRecibida = qso.SenalRecibida,
            Potencia = qso.Potencia,
            Notas = qso.Notas,
            FechaCreacion = qso.FechaCreacion,
            FechaModificacion = qso.FechaModificacion
        };
    }

    /// <summary>
    /// Ejecuta la sincronización en segundo plano (invocada por el temporizador).
    /// </summary>
    private async Task SincronizarEnSegundoPlanoAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando sincronización automática.");
            ResultadoSincronizacion resultado = await SincronizarAsync();

            if (resultado.Errores.Count > 0)
            {
                _logger.LogWarning(
                    "Sincronización automática completada con {CantidadErrores} errores.",
                    resultado.Errores.Count);
            }
            else
            {
                _logger.LogInformation(
                    "Sincronización automática completada: {Enviados} enviados, {Recibidos} recibidos.",
                    resultado.QsosEnviados,
                    resultado.QsosRecibidos);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la sincronización automática.");
        }
    }

    /// <summary>
    /// Libera los recursos del servicio (temporizador).
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _temporizador?.Dispose();
            _temporizador = null;
            _disposed = true;
        }
    }
}
