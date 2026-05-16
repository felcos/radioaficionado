namespace RadioAficionado.Dominio.IA;

/// <summary>
/// Metricas resultantes del entrenamiento de un modelo de clasificacion multiclase.
/// </summary>
/// <param name="Precision">Accuracy del modelo (0.0 a 1.0).</param>
/// <param name="PerdidaLog">Log-loss del modelo (menor es mejor).</param>
/// <param name="MatrizConfusion">Representacion textual de la matriz de confusion.</param>
/// <param name="TiempoEntrenamiento">Tiempo total que tomo entrenar el modelo.</param>
public sealed record MetricasClasificacion(
    double Precision,
    double PerdidaLog,
    string MatrizConfusion,
    TimeSpan TiempoEntrenamiento);

/// <summary>
/// Metricas resultantes del entrenamiento de un modelo de regresion.
/// </summary>
/// <param name="RCuadrado">Coeficiente de determinacion R² (1.0 es perfecto).</param>
/// <param name="ErrorCuadraticoMedio">RMSE - Root Mean Squared Error.</param>
/// <param name="ErrorAbsolutoMedio">MAE - Mean Absolute Error.</param>
/// <param name="TiempoEntrenamiento">Tiempo total que tomo entrenar el modelo.</param>
public sealed record MetricasRegresion(
    double RCuadrado,
    double ErrorCuadraticoMedio,
    double ErrorAbsolutoMedio,
    TimeSpan TiempoEntrenamiento);
