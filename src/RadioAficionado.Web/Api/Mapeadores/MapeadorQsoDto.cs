using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.Api.Dtos;

namespace RadioAficionado.Web.Api.Mapeadores;

/// <summary>
/// Métodos de extensión para mapear entre entidades Qso y DTOs de la API.
/// </summary>
public static class MapeadorQsoDto
{
    /// <summary>
    /// Convierte una entidad Qso del dominio a un QsoDto para serialización JSON.
    /// </summary>
    /// <param name="qso">La entidad Qso a convertir.</param>
    /// <returns>DTO con todos los campos del QSO.</returns>
    public static QsoDto ADto(this Qso qso)
    {
        QsoDto dto = new()
        {
            Id = qso.Id,
            IndicativoPropio = qso.IndicativoPropio.Valor,
            IndicativoContacto = qso.IndicativoContacto.Valor,
            FechaHoraInicio = qso.FechaHoraInicio,
            FechaHoraFin = qso.FechaHoraFin,
            FrecuenciaMHz = qso.Frecuencia.MHz,
            Modo = qso.Modo.ToString(),
            SenalEnviada = qso.SenalEnviada,
            SenalRecibida = string.IsNullOrWhiteSpace(qso.SenalRecibida) ? null : qso.SenalRecibida,
            Potencia = qso.Potencia,
            LocalizadorContacto = qso.LocalizadorContacto?.Valor,
            Notas = qso.Notas,
            FechaCreacion = qso.FechaCreacion,
            FechaModificacion = qso.FechaModificacion
        };

        return dto;
    }

    /// <summary>
    /// Convierte un QsoDto de la API a una entidad Qso del dominio.
    /// </summary>
    /// <param name="dto">El DTO recibido del cliente.</param>
    /// <returns>Nueva entidad Qso creada a partir del DTO.</returns>
    /// <exception cref="ArgumentException">Si algún campo del DTO es inválido.</exception>
    public static Qso AEntidad(this QsoDto dto)
    {
        Indicativo indicativoPropio = new(dto.IndicativoPropio);
        Indicativo indicativoContacto = new(dto.IndicativoContacto);
        Frecuencia frecuencia = Frecuencia.DesdeMHz(dto.FrecuenciaMHz);

        if (!Enum.TryParse(dto.Modo, ignoreCase: true, out ModoOperacion modo))
        {
            throw new ArgumentException($"Modo de operación no reconocido: '{dto.Modo}'.");
        }

        Localizador? localizador = !string.IsNullOrWhiteSpace(dto.LocalizadorContacto)
            ? new Localizador(dto.LocalizadorContacto)
            : null;

        Qso qso = Qso.Crear(
            indicativoPropio,
            indicativoContacto,
            dto.FechaHoraInicio,
            frecuencia,
            modo,
            dto.SenalEnviada,
            potencia: dto.Potencia,
            localizadorContacto: localizador,
            notas: dto.Notas);

        if (!string.IsNullOrWhiteSpace(dto.SenalRecibida) && dto.FechaHoraFin.HasValue)
        {
            qso.Completar(dto.FechaHoraFin.Value, dto.SenalRecibida);
        }

        return qso;
    }

    /// <summary>
    /// Convierte una lista de entidades Qso a una lista de DTOs.
    /// </summary>
    /// <param name="qsos">Lista de entidades Qso.</param>
    /// <returns>Lista de solo lectura con los DTOs correspondientes.</returns>
    public static IReadOnlyList<QsoDto> ADtos(this IEnumerable<Qso> qsos)
    {
        List<QsoDto> dtos = new();

        foreach (Qso qso in qsos)
        {
            dtos.Add(qso.ADto());
        }

        return dtos.AsReadOnly();
    }

    /// <summary>
    /// Convierte un FiltroQsoDto de la API al FiltroQso del dominio.
    /// </summary>
    /// <param name="filtroDto">El DTO de filtro recibido del cliente.</param>
    /// <returns>Filtro del dominio, o null si no se especificaron criterios.</returns>
    public static FiltroQso? AFiltro(this FiltroQsoDto filtroDto)
    {
        BandaRadio? banda = null;
        if (!string.IsNullOrWhiteSpace(filtroDto.Banda))
        {
            if (Enum.TryParse($"Banda{filtroDto.Banda}", ignoreCase: true, out BandaRadio bandaParseada))
            {
                banda = bandaParseada;
            }
        }

        ModoOperacion? modo = null;
        if (!string.IsNullOrWhiteSpace(filtroDto.Modo))
        {
            if (Enum.TryParse(filtroDto.Modo, ignoreCase: true, out ModoOperacion modoParseado))
            {
                modo = modoParseado;
            }
        }

        bool tieneFiltros = !string.IsNullOrWhiteSpace(filtroDto.Indicativo) ||
                            banda.HasValue ||
                            modo.HasValue ||
                            filtroDto.FechaDesde.HasValue ||
                            filtroDto.FechaHasta.HasValue;

        if (!tieneFiltros)
        {
            return null;
        }

        return new FiltroQso(
            Indicativo: filtroDto.Indicativo,
            Banda: banda,
            Modo: modo,
            FechaDesde: filtroDto.FechaDesde,
            FechaHasta: filtroDto.FechaHasta);
    }
}
