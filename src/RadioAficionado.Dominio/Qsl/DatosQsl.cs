using RadioAficionado.Dominio.Entidades;

namespace RadioAficionado.Dominio.Qsl;

/// <summary>
/// Datos necesarios para generar una tarjeta QSL digital.
/// Combina la información del QSO con los datos del operador emisor.
/// </summary>
/// <param name="Contacto">El QSO (contacto de radio) que se confirma con esta tarjeta.</param>
/// <param name="IndicativoPropio">Indicativo de la estación que emite la tarjeta QSL.</param>
/// <param name="NombreOperador">Nombre del operador que emite la tarjeta.</param>
/// <param name="Localizador">Localizador Maidenhead del operador (opcional).</param>
/// <param name="Ciudad">Ciudad del operador (opcional).</param>
/// <param name="Pais">País del operador (opcional).</param>
public record DatosQsl(
    Qso Contacto,
    string IndicativoPropio,
    string NombreOperador,
    string? Localizador = null,
    string? Ciudad = null,
    string? Pais = null);
