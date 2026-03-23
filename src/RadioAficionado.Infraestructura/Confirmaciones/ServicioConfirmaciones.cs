using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura.Adif;

namespace RadioAficionado.Infraestructura.Confirmaciones;

/// <summary>
/// Servicio orquestador que gestiona la subida y consulta de confirmaciones de QSOs
/// delegando al cliente externo correspondiente (LoTW, eQSL, Club Log).
/// </summary>
public sealed class ServicioConfirmaciones : IServicioConfirmaciones
{
    private readonly IClienteLoTW _clienteLoTW;
    private readonly IClienteEQsl _clienteEQsl;
    private readonly IClienteClubLog _clienteClubLog;
    private readonly ConfiguracionLoTW _configuracionLoTW;
    private readonly ConfiguracionEQsl _configuracionEQsl;
    private readonly ConfiguracionClubLog _configuracionClubLog;
    private readonly ILogger<ServicioConfirmaciones> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ServicioConfirmaciones"/>.
    /// </summary>
    /// <param name="clienteLoTW">Cliente de LoTW.</param>
    /// <param name="clienteEQsl">Cliente de eQSL.</param>
    /// <param name="clienteClubLog">Cliente de Club Log.</param>
    /// <param name="configuracionLoTW">Configuración de LoTW.</param>
    /// <param name="configuracionEQsl">Configuración de eQSL.</param>
    /// <param name="configuracionClubLog">Configuración de Club Log.</param>
    /// <param name="logger">Logger para registrar operaciones.</param>
    public ServicioConfirmaciones(
        IClienteLoTW clienteLoTW,
        IClienteEQsl clienteEQsl,
        IClienteClubLog clienteClubLog,
        ConfiguracionLoTW configuracionLoTW,
        ConfiguracionEQsl configuracionEQsl,
        ConfiguracionClubLog configuracionClubLog,
        ILogger<ServicioConfirmaciones> logger)
    {
        ArgumentNullException.ThrowIfNull(clienteLoTW);
        ArgumentNullException.ThrowIfNull(clienteEQsl);
        ArgumentNullException.ThrowIfNull(clienteClubLog);
        ArgumentNullException.ThrowIfNull(configuracionLoTW);
        ArgumentNullException.ThrowIfNull(configuracionEQsl);
        ArgumentNullException.ThrowIfNull(configuracionClubLog);
        ArgumentNullException.ThrowIfNull(logger);

        _clienteLoTW = clienteLoTW;
        _clienteEQsl = clienteEQsl;
        _clienteClubLog = clienteClubLog;
        _configuracionLoTW = configuracionLoTW;
        _configuracionEQsl = configuracionEQsl;
        _configuracionClubLog = configuracionClubLog;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResultadoSubida> SubirQsosAsync(IReadOnlyList<Qso> qsos, ServicioExterno servicio, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(qsos);

        if (qsos.Count == 0)
        {
            return new ResultadoSubida(false, 0, 0, "No hay QSOs para subir.", servicio);
        }

        _logger.LogInformation("Preparando subida de {Cantidad} QSOs a {Servicio}.", qsos.Count, servicio);

        // Convertir QSOs a ADIF
        IReadOnlyList<RegistroAdif> registros = ConvertidorAdifQso.ConvertirListaAAdif(qsos);
        string contenidoAdif = GeneradorAdif.Generar(registros);

        return servicio switch
        {
            ServicioExterno.LoTW => await _clienteLoTW.SubirAdifAsync(contenidoAdif, ct).ConfigureAwait(false),
            ServicioExterno.EQsl => await _clienteEQsl.SubirAdifAsync(
                contenidoAdif, _configuracionEQsl.Usuario, _configuracionEQsl.Password, ct).ConfigureAwait(false),
            ServicioExterno.ClubLog => await _clienteClubLog.SubirAdifAsync(
                contenidoAdif, _configuracionClubLog.Email, _configuracionClubLog.Password, _configuracionClubLog.Indicativo, ct).ConfigureAwait(false),
            _ => new ResultadoSubida(false, 0, 0, $"Servicio externo no soportado: {servicio}.", servicio)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfirmacionQso>> ConsultarConfirmacionesAsync(ServicioExterno servicio, CancellationToken ct = default)
    {
        _logger.LogInformation("Consultando confirmaciones en {Servicio}.", servicio);

        string contenidoAdif = servicio switch
        {
            ServicioExterno.LoTW => await _clienteLoTW.DescargarConfirmacionesAsync(ct).ConfigureAwait(false),
            ServicioExterno.EQsl => await _clienteEQsl.DescargarConfirmacionesAsync(
                _configuracionEQsl.Usuario, _configuracionEQsl.Password, ct).ConfigureAwait(false),
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(contenidoAdif))
        {
            _logger.LogInformation("No se obtuvieron confirmaciones de {Servicio}.", servicio);
            return Array.Empty<ConfirmacionQso>();
        }

        // Parsear el ADIF y convertir a confirmaciones
        // Por ahora se devuelve una lista vacía ya que el parseo completo de confirmaciones
        // requiere mapear los QSOs descargados contra los locales por indicativo/fecha/banda
        List<ConfirmacionQso> confirmaciones = new();

        _logger.LogInformation("Se obtuvieron {Cantidad} confirmaciones de {Servicio}.", confirmaciones.Count, servicio);

        return confirmaciones;
    }

    /// <inheritdoc />
    public Task<bool> ObtenerEstadoAsync(ServicioExterno servicio, CancellationToken ct = default)
    {
        bool configurado = servicio switch
        {
            ServicioExterno.LoTW => !string.IsNullOrWhiteSpace(_configuracionLoTW.Usuario) &&
                                    !string.IsNullOrWhiteSpace(_configuracionLoTW.Password),
            ServicioExterno.EQsl => !string.IsNullOrWhiteSpace(_configuracionEQsl.Usuario) &&
                                    !string.IsNullOrWhiteSpace(_configuracionEQsl.Password),
            ServicioExterno.ClubLog => !string.IsNullOrWhiteSpace(_configuracionClubLog.Email) &&
                                      !string.IsNullOrWhiteSpace(_configuracionClubLog.Password) &&
                                      !string.IsNullOrWhiteSpace(_configuracionClubLog.Indicativo) &&
                                      !string.IsNullOrWhiteSpace(_configuracionClubLog.ApiKey),
            _ => false
        };

        return Task.FromResult(configurado);
    }
}
