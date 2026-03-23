namespace RadioAficionado.Nativo.ModosDigitales.Cw;

/// <summary>
/// Tabla de conversion de codigo Morse a caracteres.
/// Usa notacion estandar: punto (.) para dit y raya (-) para dah.
/// Incluye letras A-Z, digitos 0-9 y signos de puntuacion comunes.
/// </summary>
public static class TablaMorse
{
    /// <summary>
    /// Diccionario de patron Morse a caracter. La clave es el patron (e.g., ".-" para A).
    /// </summary>
    private static readonly Dictionary<string, char> _morseACaracter = new()
    {
        // Letras
        { ".-", 'A' },
        { "-...", 'B' },
        { "-.-.", 'C' },
        { "-..", 'D' },
        { ".", 'E' },
        { "..-.", 'F' },
        { "--.", 'G' },
        { "....", 'H' },
        { "..", 'I' },
        { ".---", 'J' },
        { "-.-", 'K' },
        { ".-..", 'L' },
        { "--", 'M' },
        { "-.", 'N' },
        { "---", 'O' },
        { ".--.", 'P' },
        { "--.-", 'Q' },
        { ".-.", 'R' },
        { "...", 'S' },
        { "-", 'T' },
        { "..-", 'U' },
        { "...-", 'V' },
        { ".--", 'W' },
        { "-..-", 'X' },
        { "-.--", 'Y' },
        { "--..", 'Z' },

        // Digitos
        { "-----", '0' },
        { ".----", '1' },
        { "..---", '2' },
        { "...--", '3' },
        { "....-", '4' },
        { ".....", '5' },
        { "-....", '6' },
        { "--...", '7' },
        { "---..", '8' },
        { "----.", '9' },

        // Puntuacion
        { ".-.-.-", '.' },
        { "--..--", ',' },
        { "..--..", '?' },
        { ".----.", '\'' },
        { "-.-.--", '!' },
        { "-..-.", '/' },
        { "-.--.", '(' },
        { "-.--.-", ')' },
        { ".-...", '&' },
        { "---...", ':' },
        { "-.-.-.", ';' },
        { "-...-", '=' },
        { ".-.-.", '+' },
        { "-....-", '-' },
        { "..--.-", '_' },
        { ".-..-.", '"' },
        { "...-..-", '$' },
        { ".--.-.", '@' },
    };

    /// <summary>
    /// Diccionario inverso de caracter a patron Morse.
    /// </summary>
    private static readonly Dictionary<char, string> _caracterAMorse;

    /// <summary>
    /// Inicializa el diccionario inverso de caracter a Morse.
    /// </summary>
    static TablaMorse()
    {
        _caracterAMorse = new Dictionary<char, string>(_morseACaracter.Count);
        foreach (KeyValuePair<string, char> par in _morseACaracter)
        {
            _caracterAMorse[par.Value] = par.Key;
        }
    }

    /// <summary>
    /// Convierte un patron Morse (e.g., ".-") al caracter correspondiente.
    /// </summary>
    /// <param name="patron">Patron Morse usando '.' para dit y '-' para dah.</param>
    /// <returns>El caracter si el patron es valido; null si no se reconoce.</returns>
    public static char? ConvertirACaracter(string patron)
    {
        if (string.IsNullOrWhiteSpace(patron))
        {
            return null;
        }

        if (_morseACaracter.TryGetValue(patron, out char caracter))
        {
            return caracter;
        }

        return null;
    }

    /// <summary>
    /// Convierte un caracter a su patron Morse correspondiente.
    /// </summary>
    /// <param name="caracter">Caracter a convertir (se convierte a mayuscula automaticamente).</param>
    /// <returns>El patron Morse si el caracter es valido; null si no se reconoce.</returns>
    public static string? ConvertirAMorse(char caracter)
    {
        char caracterMayuscula = char.ToUpperInvariant(caracter);

        if (_caracterAMorse.TryGetValue(caracterMayuscula, out string? patron))
        {
            return patron;
        }

        return null;
    }

    /// <summary>
    /// Verifica si un patron Morse es valido (existe en la tabla).
    /// </summary>
    /// <param name="patron">Patron Morse a verificar.</param>
    /// <returns>True si el patron corresponde a un caracter conocido.</returns>
    public static bool EsPatronValido(string patron)
    {
        if (string.IsNullOrWhiteSpace(patron))
        {
            return false;
        }

        return _morseACaracter.ContainsKey(patron);
    }
}
