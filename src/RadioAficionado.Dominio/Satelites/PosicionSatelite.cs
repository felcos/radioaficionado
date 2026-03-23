namespace RadioAficionado.Dominio.Satelites;

/// <summary>
/// Posición instantánea de un satélite vista desde un observador en tierra.
/// Incluye coordenadas geográficas del subsatélite y datos de observación (azimut, elevación).
/// </summary>
/// <param name="Latitud">Latitud del punto subsatélite en grados (-90 a 90).</param>
/// <param name="Longitud">Longitud del punto subsatélite en grados (-180 a 180).</param>
/// <param name="Altitud">Altitud del satélite sobre la superficie terrestre en kilómetros.</param>
/// <param name="Azimut">Azimut desde el observador en grados (0-360, Norte=0).</param>
/// <param name="Elevacion">Elevación desde el observador en grados (-90 a 90). Positiva = sobre el horizonte.</param>
/// <param name="Distancia">Distancia directa (slant range) desde el observador al satélite en kilómetros.</param>
/// <param name="Visible">Indica si el satélite es visible (elevación positiva desde el observador).</param>
public sealed record PosicionSatelite(
    double Latitud,
    double Longitud,
    double Altitud,
    double Azimut,
    double Elevacion,
    double Distancia,
    bool Visible)
{
    /// <summary>
    /// Indica si el satélite está sobre el horizonte del observador (elevación > 0).
    /// </summary>
    public bool SobreHorizonte => Elevacion > 0.0;

    /// <summary>
    /// Devuelve una representación textual de la posición del satélite.
    /// </summary>
    public override string ToString()
    {
        string visibilidad = Visible ? "Visible" : "No visible";
        return $"Lat: {Latitud:F4}°, Lon: {Longitud:F4}°, Alt: {Altitud:F1} km | " +
               $"Az: {Azimut:F1}°, El: {Elevacion:F1}°, Dist: {Distancia:F1} km [{visibilidad}]";
    }
}
