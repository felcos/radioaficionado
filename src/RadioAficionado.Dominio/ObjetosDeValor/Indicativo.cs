using System.Text.RegularExpressions;

namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Indicativo de radioaficionado (callsign) representado como objeto de valor inmutable.
/// Formato válido: prefijo alfanumérico (1-3 chars) + dígito + sufijo alfanumérico (0-3 chars) + letra,
/// opcionalmente seguido de "/" y un modificador.
/// Ejemplos: EA4ABC, W1AW, VK2ABC/P, EA4ABC/M
/// </summary>
public readonly record struct Indicativo : IEquatable<Indicativo>, IComparable<Indicativo>
{
    private static readonly Regex _patronIndicativo = new(
        @"^[A-Z0-9]{1,3}[0-9][A-Z0-9]{0,3}[A-Z](/[A-Z0-9]+)?$",
        RegexOptions.Compiled);

    /// <summary>
    /// Valor completo del indicativo en mayúsculas.
    /// </summary>
    public string Valor { get; }

    /// <summary>
    /// Prefijo del indicativo (parte antes del primer dígito que precede al sufijo de letras).
    /// </summary>
    public string Prefijo { get; }

    /// <summary>
    /// Sufijo del indicativo (parte después del dígito numérico principal).
    /// </summary>
    public string Sufijo { get; }

    /// <summary>
    /// Crea una nueva instancia de <see cref="Indicativo"/> validando el formato.
    /// </summary>
    /// <param name="valor">El indicativo como cadena de texto.</param>
    /// <exception cref="ArgumentException">Si el indicativo es nulo, vacío, o tiene formato inválido.</exception>
    public Indicativo(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El indicativo no puede ser nulo ni estar vacío.",
                nameof(valor));
        }

        string valorMayusculas = valor.Trim().ToUpperInvariant();

        if (valorMayusculas.Length < 3 || valorMayusculas.Length > 10)
        {
            throw new ArgumentException(
                $"El indicativo debe tener entre 3 y 10 caracteres. Se recibió: '{valorMayusculas}' ({valorMayusculas.Length} caracteres).",
                nameof(valor));
        }

        if (!_patronIndicativo.IsMatch(valorMayusculas))
        {
            throw new ArgumentException(
                $"El indicativo '{valorMayusculas}' no tiene un formato válido. " +
                "Debe seguir el patrón: prefijo (1-3 alfanuméricos) + dígito + sufijo (0-3 alfanuméricos) + letra, " +
                "opcionalmente seguido de /modificador. Ejemplo: EA4ABC, W1AW, VK2ABC/P.",
                nameof(valor));
        }

        Valor = valorMayusculas;

        // Extraer la parte base (sin el /modificador)
        string parteBase = valorMayusculas.Contains('/')
            ? valorMayusculas[..valorMayusculas.IndexOf('/')]
            : valorMayusculas;

        // Encontrar el dígito principal: el último dígito que tiene letras después
        int indiceDígito = -1;
        for (int i = 0; i < parteBase.Length; i++)
        {
            if (char.IsDigit(parteBase[i]))
            {
                // Verificar que hay al menos una letra después
                bool hayLetraDespues = false;
                for (int j = i + 1; j < parteBase.Length; j++)
                {
                    if (char.IsLetter(parteBase[j]))
                    {
                        hayLetraDespues = true;
                        break;
                    }
                }

                if (hayLetraDespues)
                {
                    indiceDígito = i;
                }
            }
        }

        if (indiceDígito >= 0)
        {
            Prefijo = parteBase[..indiceDígito];
            Sufijo = parteBase[(indiceDígito + 1)..];
        }
        else
        {
            Prefijo = parteBase;
            Sufijo = string.Empty;
        }
    }

    /// <summary>
    /// Compara este indicativo con otro para ordenamiento alfabético.
    /// </summary>
    /// <param name="other">Otro indicativo a comparar.</param>
    /// <returns>Valor negativo si es menor, cero si es igual, positivo si es mayor.</returns>
    public int CompareTo(Indicativo other)
    {
        return string.Compare(Valor, other.Valor, StringComparison.Ordinal);
    }

    /// <summary>
    /// Conversión implícita de <see cref="Indicativo"/> a <see cref="string"/>.
    /// </summary>
    /// <param name="indicativo">El indicativo a convertir.</param>
    public static implicit operator string(Indicativo indicativo) => indicativo.Valor;

    /// <summary>
    /// Conversión implícita de <see cref="string"/> a <see cref="Indicativo"/>.
    /// </summary>
    /// <param name="valor">La cadena con el indicativo.</param>
    public static implicit operator Indicativo(string valor) => new(valor);

    /// <summary>
    /// Devuelve el valor del indicativo como cadena.
    /// </summary>
    /// <returns>El indicativo en mayúsculas.</returns>
    public override string ToString() => Valor;
}
