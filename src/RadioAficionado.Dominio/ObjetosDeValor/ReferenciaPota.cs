using System.Text.RegularExpressions;

namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Referencia de parque POTA (Parks on the Air) representada como objeto de valor inmutable.
/// Formato válido: 1-2 letras mayúsculas + guion + 4 o 5 dígitos.
/// Ejemplos: US-0001, K-0001, EA-0001, VK-12345.
/// </summary>
public readonly record struct ReferenciaPota : IEquatable<ReferenciaPota>
{
    private static readonly Regex _patronPota = new(
        @"^[A-Z]{1,2}-\d{4,5}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Valor completo de la referencia POTA en mayúsculas.
    /// </summary>
    public string Valor { get; }

    /// <summary>
    /// Código de país o entidad DXCC (parte antes del guion).
    /// </summary>
    public string Pais { get; }

    /// <summary>
    /// Número del parque (parte después del guion).
    /// </summary>
    public string Numero { get; }

    /// <summary>
    /// Crea una nueva instancia de <see cref="ReferenciaPota"/> validando el formato POTA.
    /// </summary>
    /// <param name="valor">La referencia POTA como cadena de texto.</param>
    /// <exception cref="ArgumentException">Si la referencia es nula, vacía o tiene formato inválido.</exception>
    public ReferenciaPota(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "La referencia POTA no puede ser nula ni estar vacía.",
                nameof(valor));
        }

        string valorNormalizado = valor.Trim().ToUpperInvariant();

        if (!_patronPota.IsMatch(valorNormalizado))
        {
            throw new ArgumentException(
                $"La referencia POTA '{valorNormalizado}' no tiene un formato válido. " +
                "Debe seguir el patrón: 1-2 letras + guion + 4-5 dígitos. " +
                "Ejemplo: US-0001, K-0001, EA-0001.",
                nameof(valor));
        }

        Valor = valorNormalizado;

        string[] partes = valorNormalizado.Split('-');
        Pais = partes[0];
        Numero = partes[1];
    }

    /// <summary>
    /// Conversión implícita de <see cref="ReferenciaPota"/> a <see cref="string"/>.
    /// </summary>
    /// <param name="referencia">La referencia a convertir.</param>
    public static implicit operator string(ReferenciaPota referencia) => referencia.Valor;

    /// <summary>
    /// Devuelve el valor de la referencia POTA como cadena.
    /// </summary>
    /// <returns>La referencia POTA en mayúsculas.</returns>
    public override string ToString() => Valor;
}
