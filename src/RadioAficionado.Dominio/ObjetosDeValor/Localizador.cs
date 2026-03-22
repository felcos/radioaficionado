using System.Text.RegularExpressions;
using RadioAficionado.Compartido.Constantes;

namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Localizador Maidenhead (grid locator) utilizado en radioafición para indicar ubicaciones geográficas.
/// Formatos válidos: 4 caracteres (IO91), 6 caracteres (IO91wm), u 8 caracteres (IO91wm35).
/// </summary>
public readonly record struct Localizador : IEquatable<Localizador>
{
    private static readonly Regex _patronLocalizador = new(
        @"^[A-R]{2}[0-9]{2}([A-X]{2}([0-9]{2})?)?$",
        RegexOptions.Compiled);

    /// <summary>
    /// Valor del localizador en formato normalizado (letras en mayúsculas).
    /// </summary>
    public string Valor { get; }

    /// <summary>
    /// Crea una nueva instancia de <see cref="Localizador"/> validando el formato Maidenhead.
    /// </summary>
    /// <param name="valor">El localizador como cadena de texto (4, 6 u 8 caracteres).</param>
    /// <exception cref="ArgumentException">Si el localizador es nulo, vacío o tiene formato inválido.</exception>
    public Localizador(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El localizador Maidenhead no puede ser nulo ni estar vacío.",
                nameof(valor));
        }

        string valorNormalizado = valor.Trim().ToUpperInvariant();

        if (valorNormalizado.Length != 4 && valorNormalizado.Length != 6 && valorNormalizado.Length != 8)
        {
            throw new ArgumentException(
                $"El localizador Maidenhead debe tener 4, 6 u 8 caracteres. Se recibió: '{valorNormalizado}' ({valorNormalizado.Length} caracteres).",
                nameof(valor));
        }

        if (!_patronLocalizador.IsMatch(valorNormalizado))
        {
            throw new ArgumentException(
                $"El localizador '{valorNormalizado}' no tiene un formato Maidenhead válido. " +
                "Formato esperado: 2 letras (A-R) + 2 dígitos + [2 letras (A-X) + [2 dígitos]]. " +
                "Ejemplo: IO91, IO91WM, IO91WM35.",
                nameof(valor));
        }

        Valor = valorNormalizado;
    }

    /// <summary>
    /// Obtiene las coordenadas geográficas del centro del cuadrado de grid representado por este localizador.
    /// </summary>
    /// <returns>Las <see cref="Coordenadas"/> del centro del cuadrado de grid.</returns>
    public Coordenadas ObtenerCoordenadas()
    {
        // Campo (primeros 2 caracteres): cada uno cubre 20° lon / 10° lat
        double longitud = (Valor[0] - 'A') * 20.0 - 180.0;
        double latitud = (Valor[1] - 'A') * 10.0 - 90.0;

        // Cuadrado (caracteres 3-4): cada uno cubre 2° lon / 1° lat
        longitud += (Valor[2] - '0') * 2.0;
        latitud += (Valor[3] - '0') * 1.0;

        if (Valor.Length >= 6)
        {
            // Subcuadrado (caracteres 5-6): cada uno cubre 5' lon / 2.5' lat
            double subLon = 2.0 / 24.0;
            double subLat = 1.0 / 24.0;

            longitud += (Valor[4] - 'A') * subLon;
            latitud += (Valor[5] - 'A') * subLat;

            if (Valor.Length == 8)
            {
                // Cuadrado extendido (caracteres 7-8)
                double extLon = subLon / 10.0;
                double extLat = subLat / 10.0;

                longitud += (Valor[6] - '0') * extLon;
                latitud += (Valor[7] - '0') * extLat;

                // Centro del cuadrado extendido
                longitud += extLon / 2.0;
                latitud += extLat / 2.0;
            }
            else
            {
                // Centro del subcuadrado
                longitud += subLon / 2.0;
                latitud += subLat / 2.0;
            }
        }
        else
        {
            // Centro del cuadrado (4 caracteres)
            longitud += 1.0;
            latitud += 0.5;
        }

        return new Coordenadas(latitud, longitud);
    }

    /// <summary>
    /// Calcula la distancia en kilómetros entre este localizador y otro.
    /// </summary>
    /// <param name="otro">El localizador destino.</param>
    /// <returns>Distancia en kilómetros.</returns>
    public double CalcularDistancia(Localizador otro)
    {
        Coordenadas coordenadasPropias = ObtenerCoordenadas();
        Coordenadas coordenadasOtro = otro.ObtenerCoordenadas();
        return coordenadasPropias.CalcularDistancia(coordenadasOtro);
    }

    /// <summary>
    /// Devuelve el valor del localizador como cadena.
    /// </summary>
    /// <returns>El localizador en mayúsculas.</returns>
    public override string ToString() => Valor;
}
