using RadioAficionado.Dominio.Qsl;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Interfaz para la generación de tarjetas QSL digitales.
/// Las tarjetas QSL son confirmaciones de contacto de radio, una tradición importante en radioafición.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Genera tarjetas QSL digitales personalizadas a partir de datos de un QSO y una plantilla visual. Exporta en formatos de imagen (PNG, JPEG).</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se llama a <see cref="ObtenerPlantillasAsync"/> para listar diseños disponibles y a <see cref="GenerarAsync"/> para crear la tarjeta.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.Qsl.GeneradorQslSkia</c> (usa SkiaSharp para renderizado).</para>
/// <para><b>Registro DI:</b> Registrada como Singleton en <c>RadioAficionado.Infraestructura.ConfiguracionServicios.AgregarCapaDeInfraestructura()</c>.</para>
/// <para><b>Configuración necesaria:</b> Ninguna configuración externa. Las plantillas están embebidas.</para>
/// <para><b>Dependencias:</b> SkiaSharp (paquete NuGet para renderizado de gráficos).</para>
/// </remarks>
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
