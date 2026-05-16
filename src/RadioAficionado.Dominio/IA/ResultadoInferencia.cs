namespace RadioAficionado.Dominio.IA;

/// <summary>
/// Resultado de una inferencia ejecutada por un motor ONNX Runtime.
/// Contiene la salida del modelo, metadatos de ejecucion y etiquetas opcionales.
/// </summary>
/// <param name="Salida">Vector de salida del modelo ONNX.</param>
/// <param name="NombreModelo">Nombre identificador del modelo que genero la salida.</param>
/// <param name="TiempoInferencia">Tiempo que tomo ejecutar la inferencia.</param>
/// <param name="Etiquetas">Mapeo opcional de nombres de etiquetas a sus valores de confianza.</param>
public sealed record ResultadoInferencia(
    float[] Salida,
    string NombreModelo,
    TimeSpan TiempoInferencia,
    Dictionary<string, float>? Etiquetas);
