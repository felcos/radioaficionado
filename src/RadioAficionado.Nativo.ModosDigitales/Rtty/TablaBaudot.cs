namespace RadioAficionado.Nativo.ModosDigitales.Rtty;

/// <summary>
/// Tabla de codificacion Baudot ITA2 para RTTY.
/// Cada codigo de 5 bits tiene dos interpretaciones: letras y figuras,
/// seleccionadas mediante los caracteres de control LTRS (shift a letras) y FIGS (shift a figuras).
/// </summary>
public static class TablaBaudot
{
    /// <summary>
    /// Codigo de control para cambiar a modo letras (LTRS).
    /// </summary>
    public const int CodigoLetras = 31;

    /// <summary>
    /// Codigo de control para cambiar a modo figuras (FIGS).
    /// </summary>
    public const int CodigoFiguras = 27;

    /// <summary>
    /// Codigo nulo (sin caracter).
    /// </summary>
    public const int CodigoNulo = 0;

    private static readonly char?[] TablaLetras = new char?[32]
    {
        null,   // 00 - NUL
        'E',    // 01
        '\n',   // 02 - LF
        'A',    // 03
        ' ',    // 04 - SP
        'S',    // 05
        'I',    // 06
        'U',    // 07
        '\r',   // 08 - CR
        'D',    // 09
        'R',    // 10
        'J',    // 11
        'N',    // 12
        'F',    // 13
        'C',    // 14
        'K',    // 15
        'T',    // 16
        'Z',    // 17
        'L',    // 18
        'W',    // 19
        'H',    // 20
        'Y',    // 21
        'P',    // 22
        'Q',    // 23
        'O',    // 24
        'B',    // 25
        'G',    // 26
        null,   // 27 - FIGS (control)
        'M',    // 28
        'X',    // 29
        'V',    // 30
        null    // 31 - LTRS (control)
    };

    private static readonly char?[] TablaFiguras = new char?[32]
    {
        null,   // 00 - NUL
        '3',    // 01
        '\n',   // 02 - LF
        '-',    // 03
        ' ',    // 04 - SP
        '\a',   // 05 - BEL
        '8',    // 06
        '7',    // 07
        '\r',   // 08 - CR
        '$',    // 09 (ENQ en US, $ en ITA2 internacional)
        '4',    // 10
        '\'',   // 11
        ',',    // 12
        '!',    // 13
        ':',    // 14
        '(',    // 15
        '5',    // 16
        '"',    // 17
        ')',    // 18
        '2',    // 19
        '#',    // 20
        '6',    // 21
        '0',    // 22
        '1',    // 23
        '9',    // 24
        '?',    // 25
        '&',    // 26
        null,   // 27 - FIGS (control)
        '.',    // 28
        '/',    // 29
        ';',    // 30
        null    // 31 - LTRS (control)
    };

    /// <summary>
    /// Decodifica un codigo Baudot de 5 bits a su caracter correspondiente.
    /// </summary>
    /// <param name="codigo">Codigo Baudot (0-31).</param>
    /// <param name="esFigura">True si el modo actual es figuras, false si es letras.</param>
    /// <returns>El caracter decodificado, o null si es un codigo de control o nulo.</returns>
    public static char? DecodificarCaracter(int codigo, bool esFigura)
    {
        if (codigo < 0 || codigo > 31)
        {
            return null;
        }

        if (esFigura)
        {
            return TablaFiguras[codigo];
        }

        return TablaLetras[codigo];
    }

    /// <summary>
    /// Determina si el codigo es un cambio a modo figuras (FIGS).
    /// </summary>
    /// <param name="codigo">Codigo Baudot.</param>
    /// <returns>True si es el codigo FIGS.</returns>
    public static bool EsCambioAFiguras(int codigo)
    {
        return codigo == CodigoFiguras;
    }

    /// <summary>
    /// Determina si el codigo es un cambio a modo letras (LTRS).
    /// </summary>
    /// <param name="codigo">Codigo Baudot.</param>
    /// <returns>True si es el codigo LTRS.</returns>
    public static bool EsCambioALetras(int codigo)
    {
        return codigo == CodigoLetras;
    }
}
