using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Servicio.Servicios;

/// <summary>
/// Determina el color de un indicativo basandose en su estado DXCC y logbook.
/// Colores:
/// - Rojo (#ff4444): CQ
/// - Verde brillante (#00ff41): nuevo DXCC nunca trabajado
/// - Amarillo (#ffd700): DXCC trabajado pero no confirmado
/// - Blanco (#ffffff): mensaje dirigido a mi
/// - Naranja (#ff8c00): nuevo en esta banda
/// - Gris (#666666): ya trabajado y confirmado
/// </summary>
public sealed class ColoreadorIndicativos
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly string _miIndicativo;

    /// <summary>Color para mensajes CQ.</summary>
    public const string ColorCq = "#ff4444";

    /// <summary>Color para nuevo DXCC nunca trabajado.</summary>
    public const string ColorNuevoDxcc = "#00ff41";

    /// <summary>Color para DXCC trabajado pero no confirmado.</summary>
    public const string ColorSinConfirmar = "#ffd700";

    /// <summary>Color para mensaje dirigido a mi indicativo.</summary>
    public const string ColorDirigido = "#ffffff";

    /// <summary>Color para nuevo en esta banda.</summary>
    public const string ColorNuevoBanda = "#ff8c00";

    /// <summary>Color para ya trabajado y confirmado.</summary>
    public const string ColorTrabajado = "#666666";

    /// <summary>Color por defecto.</summary>
    public const string ColorPorDefecto = "#cccccc";

    /// <summary>
    /// Crea el coloreador de indicativos.
    /// </summary>
    public ColoreadorIndicativos(IRepositorioQso repositorioQso, string miIndicativo)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
        _miIndicativo = miIndicativo ?? throw new ArgumentNullException(nameof(miIndicativo));
    }

    /// <summary>
    /// Determina el color para un mensaje decodificado.
    /// </summary>
    /// <param name="texto">Texto completo del mensaje.</param>
    /// <param name="indicativoEmisor">Indicativo del emisor.</param>
    /// <param name="indicativoDestinatario">Indicativo del destinatario.</param>
    /// <param name="banda">Banda actual.</param>
    /// <returns>Color hex CSS.</returns>
    public string DeterminarColor(
        string texto,
        string? indicativoEmisor,
        string? indicativoDestinatario,
        string? banda)
    {
        // Mensaje dirigido a mi
        if (!string.IsNullOrWhiteSpace(indicativoDestinatario) &&
            indicativoDestinatario.Equals(_miIndicativo, StringComparison.OrdinalIgnoreCase))
        {
            return ColorDirigido;
        }

        // CQ
        if (texto.StartsWith("CQ ", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(indicativoEmisor))
            {
                // Verificar si es un DXCC nuevo
                EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(new Indicativo(indicativoEmisor));
                if (entidad is not null)
                {
                    // Por ahora retornamos CQ rojo — la consulta al logbook
                    // se implementara cuando el repositorio sea async-friendly
                    return ColorCq;
                }
            }

            return ColorCq;
        }

        // Mensaje de otra estacion
        if (!string.IsNullOrWhiteSpace(indicativoEmisor))
        {
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(new Indicativo(indicativoEmisor));
            if (entidad is not null)
            {
                return ColorPorDefecto;
            }
        }

        return ColorPorDefecto;
    }
}
