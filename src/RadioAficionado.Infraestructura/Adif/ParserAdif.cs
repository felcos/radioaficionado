using System.Globalization;
using System.Text.RegularExpressions;

namespace RadioAficionado.Infraestructura.Adif;

/// <summary>
/// Resultado del parseo de un archivo ADIF.
/// Contiene los registros parseados, los campos del encabezado y las advertencias generadas.
/// </summary>
public sealed class ResultadoParserAdif
{
    /// <summary>
    /// Campos del encabezado ADIF (pares clave-valor encontrados antes de &lt;eoh&gt;).
    /// </summary>
    public Dictionary<string, string> Encabezado { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Lista de registros ADIF parseados (cada uno representa un QSO).
    /// </summary>
    public List<RegistroAdif> Registros { get; init; } = new();

    /// <summary>
    /// Número total de registros parseados.
    /// </summary>
    public int TotalRegistros => Registros.Count;

    /// <summary>
    /// Advertencias generadas durante el parseo (campos malformados, datos inesperados, etc.).
    /// </summary>
    public List<string> Advertencias { get; init; } = new();
}

/// <summary>
/// Parser de archivos ADIF 3.1.4 (Amateur Data Interchange Format).
/// Soporta archivos ADI con o sin encabezado, y es tolerante con formatos
/// irregulares producidos por programas como WSJT-X, Log4OM, N1MM, etc.
/// </summary>
public static class ParserAdif
{
    // Patrón para capturar etiquetas ADIF: <NOMBRE:LONGITUD:TIPO> o <NOMBRE:LONGITUD> o <NOMBRE>
    // Grupo 1: nombre del campo
    // Grupo 2: longitud (opcional)
    // Grupo 3: tipo (opcional)
    private static readonly Regex _patronEtiqueta = new(
        @"<\s*([A-Za-z_][A-Za-z0-9_]*)\s*(?::\s*(\d+)\s*(?::\s*([A-Za-z])\s*)?)?\s*>",
        RegexOptions.Compiled);

    /// <summary>
    /// Parsea contenido ADIF desde una cadena de texto.
    /// </summary>
    /// <param name="contenido">Cadena con el contenido ADIF completo.</param>
    /// <returns>Resultado del parseo con registros, encabezado y advertencias.</returns>
    /// <exception cref="ArgumentNullException">Si el contenido es null.</exception>
    public static ResultadoParserAdif Parsear(string contenido)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        ResultadoParserAdif resultado = new();
        int posicion = 0;

        // Determinar si hay encabezado: buscar <eoh> (case-insensitive)
        int posicionEoh = BuscarEtiquetaEspecial(contenido, "EOH");

        if (posicionEoh >= 0)
        {
            // Parsear campos del encabezado (todo antes de <eoh>)
            ParsearCamposEnRegion(contenido, 0, posicionEoh, resultado.Encabezado, resultado.Advertencias);

            // Avanzar la posición después del <eoh> y su etiqueta completa
            Match matchEoh = Regex.Match(contenido[posicionEoh..], @"<\s*EOH\s*>", RegexOptions.IgnoreCase);
            if (matchEoh.Success)
            {
                posicion = posicionEoh + matchEoh.Index + matchEoh.Length;
            }
        }

        // Parsear registros (todo después del encabezado, o todo el contenido si no hay encabezado)
        ParsearRegistros(contenido, posicion, resultado.Registros, resultado.Advertencias);

        return resultado;
    }

    /// <summary>
    /// Parsea contenido ADIF desde un archivo en disco de forma asíncrona.
    /// </summary>
    /// <param name="rutaArchivo">Ruta completa al archivo ADIF (.adi o .adif).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado del parseo con registros, encabezado y advertencias.</returns>
    /// <exception cref="ArgumentException">Si la ruta es nula o vacía.</exception>
    /// <exception cref="FileNotFoundException">Si el archivo no existe.</exception>
    public static async Task<ResultadoParserAdif> ParsearArchivoAsync(
        string rutaArchivo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            throw new ArgumentException(
                "La ruta del archivo no puede ser nula ni estar vacía.",
                nameof(rutaArchivo));
        }

        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException(
                $"No se encontró el archivo ADIF: '{rutaArchivo}'.",
                rutaArchivo);
        }

        string contenido = await File.ReadAllTextAsync(rutaArchivo, ct).ConfigureAwait(false);
        return Parsear(contenido);
    }

    /// <summary>
    /// Parsea contenido ADIF desde un Stream de forma asíncrona.
    /// </summary>
    /// <param name="flujo">Stream con el contenido ADIF.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado del parseo con registros, encabezado y advertencias.</returns>
    /// <exception cref="ArgumentNullException">Si el flujo es null.</exception>
    public static async Task<ResultadoParserAdif> ParsearDesdeStreamAsync(
        Stream flujo,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(flujo);

        using StreamReader lector = new(flujo, leaveOpen: true);
        string contenido = await lector.ReadToEndAsync(ct).ConfigureAwait(false);
        return Parsear(contenido);
    }

    /// <summary>
    /// Busca la posición de una etiqueta especial (EOH o EOR) de forma case-insensitive.
    /// </summary>
    private static int BuscarEtiquetaEspecial(string contenido, string nombreEtiqueta)
    {
        // Buscar <EOH> o <EOR> de forma case-insensitive, permitiendo espacios internos
        string patron = $@"<\s*{Regex.Escape(nombreEtiqueta)}\s*>";
        Match match = Regex.Match(contenido, patron, RegexOptions.IgnoreCase);
        return match.Success ? match.Index : -1;
    }

    /// <summary>
    /// Parsea campos ADIF dentro de una región del texto y los añade al diccionario proporcionado.
    /// </summary>
    private static void ParsearCamposEnRegion(
        string contenido,
        int inicio,
        int fin,
        Dictionary<string, string> destino,
        List<string> advertencias)
    {
        string region = contenido[inicio..fin];
        MatchCollection coincidencias = _patronEtiqueta.Matches(region);

        foreach (Match coincidencia in coincidencias)
        {
            string nombreCampo = coincidencia.Groups[1].Value.ToUpperInvariant();

            // Ignorar etiquetas especiales
            if (string.Equals(nombreCampo, "EOH", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(nombreCampo, "EOR", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string valorCampo = ExtraerValorDeCampo(region, coincidencia, advertencias, nombreCampo);

            if (!string.IsNullOrWhiteSpace(valorCampo))
            {
                destino[nombreCampo] = valorCampo;
            }
        }
    }

    /// <summary>
    /// Parsea todos los registros (QSOs) desde la posición indicada hasta el final del contenido.
    /// </summary>
    private static void ParsearRegistros(
        string contenido,
        int posicionInicio,
        List<RegistroAdif> registros,
        List<string> advertencias)
    {
        string regionRegistros = contenido[posicionInicio..];
        int posicion = 0;

        while (posicion < regionRegistros.Length)
        {
            // Buscar el siguiente <eor>
            Match matchEor = Regex.Match(
                regionRegistros[posicion..],
                @"<\s*EOR\s*>",
                RegexOptions.IgnoreCase);

            if (!matchEor.Success)
            {
                // No hay más registros completos. Verificar si hay campos sueltos (archivo truncado).
                string restoTexto = regionRegistros[posicion..].Trim();
                if (restoTexto.Length > 0 && _patronEtiqueta.IsMatch(restoTexto))
                {
                    advertencias.Add(
                        "Se encontraron campos ADIF después del último <eor>. " +
                        "Posible archivo truncado; los campos sin <eor> fueron ignorados.");
                }
                break;
            }

            int inicioRegistro = posicion;
            int finRegistro = posicion + matchEor.Index;
            string textoRegistro = regionRegistros[inicioRegistro..finRegistro];

            RegistroAdif registro = ParsearUnRegistro(textoRegistro, advertencias);

            // Solo añadir el registro si tiene al menos un campo significativo
            if (registro.NumeroDeCampos > 0)
            {
                registros.Add(registro);
            }

            // Avanzar después del <eor>
            posicion = posicion + matchEor.Index + matchEor.Length;
        }
    }

    /// <summary>
    /// Parsea un único registro ADIF desde un fragmento de texto (sin incluir el &lt;eor&gt;).
    /// </summary>
    private static RegistroAdif ParsearUnRegistro(string textoRegistro, List<string> advertencias)
    {
        RegistroAdif registro = new();
        MatchCollection coincidencias = _patronEtiqueta.Matches(textoRegistro);

        foreach (Match coincidencia in coincidencias)
        {
            string nombreCampo = coincidencia.Groups[1].Value.ToUpperInvariant();

            // Ignorar etiquetas especiales dentro del registro
            if (string.Equals(nombreCampo, "EOH", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(nombreCampo, "EOR", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string valorCampo = ExtraerValorDeCampo(textoRegistro, coincidencia, advertencias, nombreCampo);
            registro[nombreCampo] = valorCampo;
        }

        return registro;
    }

    /// <summary>
    /// Extrae el valor de un campo ADIF a partir de la coincidencia de su etiqueta.
    /// Maneja campos con y sin longitud especificada, y es tolerante con longitudes incorrectas.
    /// </summary>
    private static string ExtraerValorDeCampo(
        string texto,
        Match coincidencia,
        List<string> advertencias,
        string nombreCampo)
    {
        int inicioValor = coincidencia.Index + coincidencia.Length;

        // Si no se especificó longitud, el valor va hasta la siguiente etiqueta o fin de texto
        if (!coincidencia.Groups[2].Success || string.IsNullOrWhiteSpace(coincidencia.Groups[2].Value))
        {
            // Campo sin longitud: tomar texto hasta la siguiente etiqueta '<' o fin
            int finValor = texto.IndexOf('<', inicioValor);
            if (finValor < 0)
            {
                finValor = texto.Length;
            }

            return texto[inicioValor..finValor].Trim();
        }

        // Campo con longitud especificada
        if (!int.TryParse(coincidencia.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int longitud))
        {
            advertencias.Add(
                $"Campo '{nombreCampo}': no se pudo interpretar la longitud '{coincidencia.Groups[2].Value}'. " +
                "Se intentará leer hasta la siguiente etiqueta.");

            int finValor = texto.IndexOf('<', inicioValor);
            if (finValor < 0) finValor = texto.Length;
            return texto[inicioValor..finValor].Trim();
        }

        if (longitud == 0)
        {
            return string.Empty;
        }

        // Verificar que hay suficientes caracteres disponibles
        if (inicioValor + longitud > texto.Length)
        {
            advertencias.Add(
                $"Campo '{nombreCampo}': la longitud declarada ({longitud}) excede el texto disponible " +
                $"(quedan {texto.Length - inicioValor} caracteres). Se tomará lo que haya.");

            return texto[inicioValor..].TrimEnd();
        }

        string valor = texto.Substring(inicioValor, longitud);

        // Verificación de cordura: si la longitud parece incorrecta (contiene '<' dentro del valor),
        // es probable que el archivo sea malformado. Algunos programas calculan mal la longitud.
        int posicionEtiquetaDentro = valor.IndexOf('<');
        if (posicionEtiquetaDentro >= 0)
        {
            advertencias.Add(
                $"Campo '{nombreCampo}': el valor declarado de longitud {longitud} contiene una etiqueta '<'. " +
                "Posible longitud incorrecta en el archivo fuente. Se trunca antes de la etiqueta.");

            valor = valor[..posicionEtiquetaDentro];
        }

        return valor;
    }
}
