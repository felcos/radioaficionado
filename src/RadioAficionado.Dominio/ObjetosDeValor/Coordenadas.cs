using RadioAficionado.Compartido.Constantes;

namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Coordenadas geográficas (latitud y longitud) representadas como objeto de valor.
/// </summary>
public readonly record struct Coordenadas : IEquatable<Coordenadas>
{
    /// <summary>
    /// Latitud en grados decimales (-90 a 90).
    /// </summary>
    public double Latitud { get; }

    /// <summary>
    /// Longitud en grados decimales (-180 a 180).
    /// </summary>
    public double Longitud { get; }

    /// <summary>
    /// Crea una nueva instancia de <see cref="Coordenadas"/> validando los rangos.
    /// </summary>
    /// <param name="latitud">Latitud en grados decimales (-90 a 90).</param>
    /// <param name="longitud">Longitud en grados decimales (-180 a 180).</param>
    /// <exception cref="ArgumentOutOfRangeException">Si la latitud o longitud están fuera de rango.</exception>
    public Coordenadas(double latitud, double longitud)
    {
        if (latitud < -90.0 || latitud > 90.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitud),
                latitud,
                "La latitud debe estar entre -90 y 90 grados.");
        }

        if (longitud < -180.0 || longitud > 180.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitud),
                longitud,
                "La longitud debe estar entre -180 y 180 grados.");
        }

        Latitud = latitud;
        Longitud = longitud;
    }

    /// <summary>
    /// Calcula la distancia en kilómetros hasta otras coordenadas usando la fórmula de Haversine.
    /// </summary>
    /// <param name="otra">Las coordenadas destino.</param>
    /// <returns>Distancia en kilómetros.</returns>
    public double CalcularDistancia(Coordenadas otra)
    {
        double latitud1Rad = Latitud * ConstantesRadio.GradosARadianes;
        double latitud2Rad = otra.Latitud * ConstantesRadio.GradosARadianes;
        double deltaLatitud = (otra.Latitud - Latitud) * ConstantesRadio.GradosARadianes;
        double deltaLongitud = (otra.Longitud - Longitud) * ConstantesRadio.GradosARadianes;

        double a = Math.Sin(deltaLatitud / 2.0) * Math.Sin(deltaLatitud / 2.0)
                  + Math.Cos(latitud1Rad) * Math.Cos(latitud2Rad)
                  * Math.Sin(deltaLongitud / 2.0) * Math.Sin(deltaLongitud / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return ConstantesRadio.RadioDeLaTierraKm * c;
    }

    /// <summary>
    /// Convierte las coordenadas a un localizador Maidenhead de 6 caracteres.
    /// </summary>
    /// <returns>Un <see cref="Localizador"/> de 6 caracteres correspondiente a estas coordenadas.</returns>
    public Localizador ObtenerLocalizador()
    {
        double longitud = Longitud + 180.0;
        double latitud = Latitud + 90.0;

        char campo1 = (char)('A' + (int)(longitud / 20.0));
        char campo2 = (char)('A' + (int)(latitud / 10.0));

        char cuadrado1 = (char)('0' + (int)(longitud % 20.0 / 2.0));
        char cuadrado2 = (char)('0' + (int)(latitud % 10.0));

        char subcuadrado1 = (char)('A' + (int)(longitud % 2.0 / (2.0 / 24.0)));
        char subcuadrado2 = (char)('A' + (int)(latitud % 1.0 / (1.0 / 24.0)));

        // Asegurar que los subcuadrados no excedan 'X'
        if (subcuadrado1 > 'X') subcuadrado1 = 'X';
        if (subcuadrado2 > 'X') subcuadrado2 = 'X';

        string localizador = $"{campo1}{campo2}{cuadrado1}{cuadrado2}{subcuadrado1}{subcuadrado2}";
        return new Localizador(localizador);
    }

    /// <summary>
    /// Devuelve la representación textual de las coordenadas.
    /// </summary>
    /// <returns>Cadena con formato "Lat: X.XXXX, Lon: Y.YYYY".</returns>
    public override string ToString()
    {
        return $"Lat: {Latitud:F4}, Lon: {Longitud:F4}";
    }
}
