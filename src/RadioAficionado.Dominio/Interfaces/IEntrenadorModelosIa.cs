using RadioAficionado.Dominio.IA;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio para entrenar modelos de IA y exportarlos en formato ONNX.
/// Permite entrenar el clasificador de senales y el analizador de propagacion
/// con datos sinteticos ampliados y exportar los modelos resultantes.
/// </summary>
public interface IEntrenadorModelosIa
{
    /// <summary>
    /// Entrena un modelo de clasificacion de senales con datos sinteticos ampliados
    /// y lo exporta en formato ONNX a la ruta especificada.
    /// </summary>
    /// <param name="rutaSalida">Ruta completa del archivo .onnx de salida.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Metricas del entrenamiento incluyendo accuracy y log-loss.</returns>
    Task<MetricasClasificacion> EntrenarYExportarClasificadorAsync(string rutaSalida, CancellationToken ct);

    /// <summary>
    /// Entrena un modelo de regresion para prediccion de propagacion con datos sinteticos ampliados
    /// y lo exporta en formato ONNX a la ruta especificada.
    /// </summary>
    /// <param name="rutaSalida">Ruta completa del archivo .onnx de salida.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Metricas del entrenamiento incluyendo R², RMSE y MAE.</returns>
    Task<MetricasRegresion> EntrenarYExportarAnalizadorAsync(string rutaSalida, CancellationToken ct);

    /// <summary>
    /// Entrena y exporta todos los modelos disponibles (clasificador y analizador)
    /// al directorio especificado.
    /// </summary>
    /// <param name="directorioSalida">Directorio donde se guardaran los archivos .onnx.</param>
    /// <param name="ct">Token de cancelacion.</param>
    Task EntrenarTodosLosModelosAsync(string directorioSalida, CancellationToken ct);
}
