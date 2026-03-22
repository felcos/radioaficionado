using MediatR;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Aplicacion.Qsos.RegistrarQso;

/// <summary>
/// Handler que procesa el comando de registrar un nuevo QSO.
/// </summary>
public sealed class RegistrarQsoHandler : IRequestHandler<RegistrarQsoComando, RegistrarQsoResultado>
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ILogger<RegistrarQsoHandler> _logger;

    /// <summary>
    /// Crea una nueva instancia del handler.
    /// </summary>
    public RegistrarQsoHandler(
        IRepositorioQso repositorioQso,
        IUnidadDeTrabajo unidadDeTrabajo,
        ILogger<RegistrarQsoHandler> logger)
    {
        _repositorioQso = repositorioQso;
        _unidadDeTrabajo = unidadDeTrabajo;
        _logger = logger;
    }

    /// <summary>
    /// Procesa el comando de registro de QSO.
    /// </summary>
    public async Task<RegistrarQsoResultado> Handle(RegistrarQsoComando comando, CancellationToken ct)
    {
        try
        {
            Indicativo indicativoPropio = new Indicativo(comando.IndicativoPropio);
            Indicativo indicativoContacto = new Indicativo(comando.IndicativoContacto);
            Frecuencia frecuencia = Frecuencia.DesdeHz(comando.FrecuenciaHz);

            Localizador? localizador = null;
            if (!string.IsNullOrWhiteSpace(comando.LocalizadorContacto))
            {
                localizador = new Localizador(comando.LocalizadorContacto);
            }

            Qso qso = Qso.Crear(
                indicativoPropio,
                indicativoContacto,
                comando.FechaHoraInicio,
                frecuencia,
                comando.Modo,
                comando.SenalEnviada,
                comando.Potencia,
                localizador,
                comando.Notas);

            if (!string.IsNullOrWhiteSpace(comando.SenalRecibida))
            {
                qso.Completar(comando.FechaHoraInicio, comando.SenalRecibida);
            }

            await _repositorioQso.AgregarAsync(qso, ct);
            await _unidadDeTrabajo.GuardarCambiosAsync(ct);

            _logger.LogInformation(
                "QSO registrado: {IndicativoPropio} → {IndicativoContacto} en {Frecuencia} ({Modo})",
                comando.IndicativoPropio,
                comando.IndicativoContacto,
                frecuencia,
                comando.Modo);

            return RegistrarQsoResultado.Exito(qso.Id);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validación al registrar QSO");
            return RegistrarQsoResultado.Fallo(ex.Message);
        }
    }
}
