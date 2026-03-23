using System.Text.RegularExpressions;

namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Referencia de cumbre SOTA (Summits on the Air) representada como objeto de valor inmutable.
/// Formato válido: asociación (1-3 alfanuméricos) + "/" + región (2 letras) + "-" + número (3 dígitos).
/// Ejemplos: EA4/MD-001, W4C/EM-001, G/LD-001.
/// </summary>
public readonly record struct ReferenciaSota : IEquatable<ReferenciaSota>
{
    private static readonly Regex _patronSota = new(
        @"^[A-Z0-9]{1,3}/[A-Z]{2}-\d{3}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Valor completo de la referencia SOTA en mayúsculas.
    /// </summary>
    public string Valor { get; }

    /// <summary>
    /// Código de la asociación SOTA (parte antes de la barra).
    /// </summary>
    public string Asociacion { get; }

    /// <summary>
    /// Código de la región dentro de la asociación (parte entre la barra y el guion).
    /// </summary>
    public string Region { get; }

    /// <summary>
    /// Número de la cumbre dentro de la región (parte después del guion).
    /// </summary>
    public string Numero { get; }

    /// <summary>
    /// Crea una nueva instancia de <see cref="ReferenciaSota"/> validando el formato SOTA.
    /// </summary>
    /// <param name="valor">La referencia SOTA como cadena de texto.</param>
    /// <exception cref="ArgumentException">Si la referencia es nula, vacía o tiene formato inválido.</exception>
    public ReferenciaSota(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "La referencia SOTA no puede ser nula ni estar vacía.",
                nameof(valor));
        }

        string valorNormalizado = valor.Trim().ToUpperInvariant();

        if (!_patronSota.IsMatch(valorNormalizado))
        {
            throw new ArgumentException(
                $"La referencia SOTA '{valorNormalizado}' no tiene un formato válido. " +
                "Debe seguir el patrón: asociación (1-3 alfanuméricos) / región (2 letras) - número (3 dígitos). " +
                "Ejemplo: EA4/MD-001, W4C/EM-001, G/LD-001.",
                nameof(valor));
        }

        Valor = valorNormalizado;

        // Formato: ASOCIACION/REGION-NUMERO
        int indiceBarra = valorNormalizado.IndexOf('/');
        int indiceGuion = valorNormalizado.IndexOf('-');

        Asociacion = valorNormalizado[..indiceBarra];
        Region = valorNormalizado[(indiceBarra + 1)..indiceGuion];
        Numero = valorNormalizado[(indiceGuion + 1)..];
    }

    /// <summary>
    /// Conversión implícita de <see cref="ReferenciaSota"/> a <see cref="string"/>.
    /// </summary>
    /// <param name="referencia">La referencia a convertir.</param>
    public static implicit operator string(ReferenciaSota referencia) => referencia.Valor;

    /// <summary>
    /// Devuelve el valor de la referencia SOTA como cadena.
    /// </summary>
    /// <returns>La referencia SOTA en mayúsculas.</returns>
    public override string ToString() => Valor;
}
