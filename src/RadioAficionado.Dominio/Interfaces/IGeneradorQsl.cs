using RadioAficionado.Dominio.Qsl;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Interfaz para la generación de tarjetas QSL digitales.
/// Las tarjetas QSL son confirmaciones de contacto de radio, una tradición importante en radioafición.
/// </summary>
public interface IGeneradorQsl
{
    /// <summary>
    /// Genera una tarjeta QSL digital en el formato especificado.
    /// </summary>
    /// <param name="datos">Datos del QSO y del operador para incluir en la tarjeta.</param>
    /// <param name="plantilla">Plantilla visual que define el diseño de la tarjeta.</param>
    /// <param name="formato">Formato de exportación de la imagen resultante.</param>
    /// <returns>Array de bytes con la imagen generada en el formato solicitado.</returns>
    /// <exception cref="ArgumentNullException">Si los datos o la plantilla son nulos.</exception>
    /// <exception cref="NotSupportedException">Si el formato de exportación no está soportado.</exception>
    Task<byte[]> GenerarAsync(DatosQsl datos, PlantillaQsl plantilla, FormatoExportacion formato);

    /// <summary>
    /// Obtiene la lista de plantillas predefinidas disponibles para la generación de tarjetas QSL.
    /// </summary>
    /// <returns>Lista de solo lectura con las plantillas disponibles.</returns>
    Task<IReadOnlyList<PlantillaQsl>> ObtenerPlantillasAsync();
}
