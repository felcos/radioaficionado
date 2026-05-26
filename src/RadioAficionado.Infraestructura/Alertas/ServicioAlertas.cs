using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Alertas;
using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Infraestructura.Alertas;

/// <summary>
/// Implementacion del servicio de alertas que evalua spots DX contra reglas configuradas.
/// Cruza spots con el catalogo DXCC y las entidades trabajadas para detectar nuevas.
/// </summary>
public sealed class ServicioAlertas : IServicioAlertas
{
    private readonly List<ReglaAlerta> _reglas = [];
    private readonly object _lockReglas = new();
    private HashSet<int> _entidadesTrabajadas = [];
    private readonly ILogger<ServicioAlertas> _logger;

    /// <inheritdoc />
    public event EventHandler<ResultadoAlerta>? AlertaDisparada;

    /// <summary>
    /// Crea el servicio de alertas.
    /// </summary>
    /// <param name="logger">Logger para registro de eventos.</param>
    public ServicioAlertas(ILogger<ServicioAlertas> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<ResultadoAlerta> EvaluarSpot(
        string spotteador, string dx, long frecuenciaHz, string comentario, DateTime hora)
    {
        if (string.IsNullOrWhiteSpace(dx))
        {
            return [];
        }

        List<ResultadoAlerta> alertas = [];
        Frecuencia frecuencia = Frecuencia.DesdeHz(frecuenciaHz);
        BandaRadio? banda = frecuencia.ObtenerBanda();
        string modoInferido = InferirModo(comentario);
        EntidadDxcc? entidadDxcc = CatalogoDxcc.ObtenerPorIndicativo(dx);

        lock (_lockReglas)
        {
            foreach (ReglaAlerta regla in _reglas)
            {
                if (!regla.Activa)
                {
                    continue;
                }

                string? mensaje = EvaluarRegla(regla, dx, banda, modoInferido, entidadDxcc);

                if (mensaje is not null)
                {
                    ResultadoAlerta resultado = new(
                        regla,
                        spotteador,
                        dx,
                        frecuencia,
                        comentario,
                        hora,
                        entidadDxcc,
                        mensaje);

                    alertas.Add(resultado);
                    AlertaDisparada?.Invoke(this, resultado);

                    _logger.LogInformation(
                        "Alerta disparada: {Nombre} — {Mensaje}",
                        regla.Nombre, mensaje);
                }
            }
        }

        return alertas;
    }

    /// <inheritdoc />
    public void AgregarRegla(ReglaAlerta regla)
    {
        if (regla is null)
        {
            throw new ArgumentNullException(nameof(regla));
        }

        lock (_lockReglas)
        {
            _reglas.Add(regla);
        }

        _logger.LogInformation("Regla de alerta agregada: {Nombre} ({Tipo})", regla.Nombre, regla.Tipo);
    }

    /// <inheritdoc />
    public bool EliminarRegla(Guid idRegla)
    {
        lock (_lockReglas)
        {
            int eliminados = _reglas.RemoveAll(r => r.Id == idRegla);

            if (eliminados > 0)
            {
                _logger.LogInformation("Regla de alerta eliminada: {Id}", idRegla);
            }

            return eliminados > 0;
        }
    }

    /// <inheritdoc />
    public bool CambiarEstadoRegla(Guid idRegla, bool activa)
    {
        lock (_lockReglas)
        {
            ReglaAlerta? regla = _reglas.Find(r => r.Id == idRegla);

            if (regla is null)
            {
                return false;
            }

            regla.Activa = activa;
            _logger.LogInformation(
                "Regla de alerta {Estado}: {Nombre}",
                activa ? "activada" : "desactivada", regla.Nombre);

            return true;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ReglaAlerta> ObtenerReglas()
    {
        lock (_lockReglas)
        {
            return _reglas.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public void ActualizarEntidadesTrabajadas(HashSet<int> numerosEntidadesTrabajadas)
    {
        if (numerosEntidadesTrabajadas is null)
        {
            throw new ArgumentNullException(nameof(numerosEntidadesTrabajadas));
        }

        _entidadesTrabajadas = new HashSet<int>(numerosEntidadesTrabajadas);
        _logger.LogInformation(
            "Cache de entidades DXCC trabajadas actualizada: {Total} entidades",
            _entidadesTrabajadas.Count);
    }

    /// <summary>
    /// Evalua una regla individual contra los datos de un spot.
    /// </summary>
    /// <returns>Mensaje de alerta si la regla se cumple; null si no.</returns>
    private string? EvaluarRegla(
        ReglaAlerta regla, string dx, BandaRadio? banda, string modoInferido, EntidadDxcc? entidadDxcc)
    {
        switch (regla.Tipo)
        {
            case TipoAlerta.DxccNueva:
                return EvaluarDxccNueva(regla, dx, banda, entidadDxcc);

            case TipoAlerta.Banda:
                return EvaluarBanda(regla, banda);

            case TipoAlerta.Modo:
                return EvaluarModo(regla, modoInferido);

            case TipoAlerta.Indicativo:
                return EvaluarIndicativo(regla, dx);

            case TipoAlerta.BandaYModo:
                return EvaluarBandaYModo(regla, banda, modoInferido);

            default:
                return null;
        }
    }

    private string? EvaluarDxccNueva(
        ReglaAlerta regla, string dx, BandaRadio? banda, EntidadDxcc? entidadDxcc)
    {
        if (entidadDxcc is null || entidadDxcc.Eliminada)
        {
            return null;
        }

        // Si se especifica banda, solo alertar en esa banda
        if (regla.Banda is not null && banda != regla.Banda)
        {
            return null;
        }

        if (!_entidadesTrabajadas.Contains(entidadDxcc.Numero))
        {
            return $"DXCC nueva: {entidadDxcc.Nombre} ({entidadDxcc.Prefijo}) — {dx}";
        }

        return null;
    }

    private static string? EvaluarBanda(ReglaAlerta regla, BandaRadio? banda)
    {
        if (regla.Banda is null || banda is null)
        {
            return null;
        }

        if (banda == regla.Banda)
        {
            return $"Spot en banda {banda.Value.ObtenerNombre()}";
        }

        return null;
    }

    private static string? EvaluarModo(ReglaAlerta regla, string modoInferido)
    {
        if (string.IsNullOrWhiteSpace(regla.Modo) || string.IsNullOrWhiteSpace(modoInferido))
        {
            return null;
        }

        if (modoInferido.Equals(regla.Modo, StringComparison.OrdinalIgnoreCase))
        {
            return $"Spot en modo {modoInferido}";
        }

        return null;
    }

    private static string? EvaluarIndicativo(ReglaAlerta regla, string dx)
    {
        if (string.IsNullOrWhiteSpace(regla.Indicativo))
        {
            return null;
        }

        if (dx.Contains(regla.Indicativo, StringComparison.OrdinalIgnoreCase))
        {
            return $"Indicativo coincide: {dx}";
        }

        return null;
    }

    private static string? EvaluarBandaYModo(ReglaAlerta regla, BandaRadio? banda, string modoInferido)
    {
        if (regla.Banda is null || string.IsNullOrWhiteSpace(regla.Modo))
        {
            return null;
        }

        if (banda == regla.Banda &&
            !string.IsNullOrWhiteSpace(modoInferido) &&
            modoInferido.Equals(regla.Modo, StringComparison.OrdinalIgnoreCase))
        {
            return $"Spot en {banda.Value.ObtenerNombre()} {modoInferido}";
        }

        return null;
    }

    /// <summary>
    /// Infiere el modo de operacion a partir del comentario del spot.
    /// </summary>
    private static string InferirModo(string comentario)
    {
        if (string.IsNullOrWhiteSpace(comentario))
        {
            return string.Empty;
        }

        string upper = comentario.ToUpperInvariant();

        string[] modos = ["FT8", "FT4", "CW", "SSB", "LSB", "USB", "RTTY", "PSK", "JT65", "JT9", "WSPR", "JS8", "FM", "AM"];

        foreach (string modo in modos)
        {
            if (upper.Contains(modo))
            {
                // Normalizar LSB/USB a SSB
                if (modo is "LSB" or "USB")
                {
                    return "SSB";
                }

                return modo;
            }
        }

        return string.Empty;
    }
}
