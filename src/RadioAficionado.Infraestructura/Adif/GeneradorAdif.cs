using System.Globalization;
using System.Text;

namespace RadioAficionado.Infraestructura.Adif;

/// <summary>
/// Genera archivos ADIF 3.1.4 (Amateur Data Interchange Format) a partir de registros.
/// Produce archivos compatibles con WSJT-X, Log4OM, N1MM y otros programas de log.
/// </summary>
public static class GeneradorAdif
{
    private const string VersionAdif = "3.1.4";
    private const string SaltosDeLineaRegistro = "\r\n";

    /// <summary>
    /// Genera contenido ADIF como cadena de texto, incluyendo encabezado estándar.
    /// </summary>
    /// <param name="registros">Colección de registros ADIF a incluir.</param>
    /// <param name="programa">Nombre del programa que genera el archivo. Null para omitirlo.</param>
    /// <returns>Cadena con el contenido ADIF completo listo para guardar.</returns>
    /// <exception cref="ArgumentNullException">Si la colección de registros es null.</exception>
    public static string Generar(IEnumerable<RegistroAdif> registros, string? programa = "RadioAficionado")
    {
        ArgumentNullException.ThrowIfNull(registros);

        StringBuilder sb = new();

        // Escribir encabezado
        EscribirEncabezado(sb, programa);

        // Escribir registros
        foreach (RegistroAdif registro in registros)
        {
            EscribirRegistro(sb, registro);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Genera contenido ADIF con campos de encabezado personalizados.
    /// </summary>
    /// <param name="registros">Colección de registros ADIF a incluir.</param>
    /// <param name="camposEncabezado">Campos adicionales para el encabezado.</param>
    /// <param name="programa">Nombre del programa que genera el archivo. Null para omitirlo.</param>
    /// <returns>Cadena con el contenido ADIF completo.</returns>
    /// <exception cref="ArgumentNullException">Si la colección de registros es null.</exception>
    public static string GenerarConEncabezado(
        IEnumerable<RegistroAdif> registros,
        Dictionary<string, string>? camposEncabezado,
        string? programa = "RadioAficionado")
    {
        ArgumentNullException.ThrowIfNull(registros);

        StringBuilder sb = new();

        // Escribir encabezado con campos personalizados
        EscribirEncabezado(sb, programa, camposEncabezado);

        // Escribir registros
        foreach (RegistroAdif registro in registros)
        {
            EscribirRegistro(sb, registro);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escribe contenido ADIF a un archivo de forma asíncrona.
    /// </summary>
    /// <param name="rutaArchivo">Ruta completa del archivo de salida.</param>
    /// <param name="registros">Colección de registros ADIF a escribir.</param>
    /// <param name="programa">Nombre del programa que genera el archivo. Null para omitirlo.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <exception cref="ArgumentException">Si la ruta es nula o vacía.</exception>
    /// <exception cref="ArgumentNullException">Si la colección de registros es null.</exception>
    public static async Task GenerarArchivoAsync(
        string rutaArchivo,
        IEnumerable<RegistroAdif> registros,
        string? programa = "RadioAficionado",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            throw new ArgumentException(
                "La ruta del archivo no puede ser nula ni estar vacía.",
                nameof(rutaArchivo));
        }

        ArgumentNullException.ThrowIfNull(registros);

        string contenido = Generar(registros, programa);
        await File.WriteAllTextAsync(rutaArchivo, contenido, Encoding.UTF8, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Escribe contenido ADIF a un Stream de forma asíncrona.
    /// </summary>
    /// <param name="flujo">Stream de destino.</param>
    /// <param name="registros">Colección de registros ADIF a escribir.</param>
    /// <param name="programa">Nombre del programa que genera el archivo. Null para omitirlo.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <exception cref="ArgumentNullException">Si el flujo o la colección de registros es null.</exception>
    public static async Task GenerarEnStreamAsync(
        Stream flujo,
        IEnumerable<RegistroAdif> registros,
        string? programa = "RadioAficionado",
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(flujo);
        ArgumentNullException.ThrowIfNull(registros);

        string contenido = Generar(registros, programa);
        using StreamWriter escritor = new(flujo, Encoding.UTF8, leaveOpen: true);
        await escritor.WriteAsync(contenido.AsMemory(), ct).ConfigureAwait(false);
        await escritor.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Escribe el encabezado estándar ADIF.
    /// </summary>
    private static void EscribirEncabezado(
        StringBuilder sb,
        string? programa,
        Dictionary<string, string>? camposAdicionales = null)
    {
        // Texto descriptivo del encabezado (ADIF permite texto libre antes de las etiquetas)
        sb.Append("Archivo ADIF generado por ");
        sb.Append(programa ?? "RadioAficionado");
        sb.Append(SaltosDeLineaRegistro);
        sb.Append(SaltosDeLineaRegistro);

        // Campo ADIF_VER (versión del formato)
        EscribirCampo(sb, "ADIF_VER", VersionAdif);
        sb.Append(SaltosDeLineaRegistro);

        // Campo CREATED_TIMESTAMP
        string marcaTiempo = DateTimeOffset.UtcNow.ToString("yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
        EscribirCampo(sb, "CREATED_TIMESTAMP", marcaTiempo);
        sb.Append(SaltosDeLineaRegistro);

        // Campo PROGRAMID
        if (!string.IsNullOrWhiteSpace(programa))
        {
            EscribirCampo(sb, "PROGRAMID", programa);
            sb.Append(SaltosDeLineaRegistro);
        }

        // Campos adicionales del encabezado
        if (camposAdicionales is not null)
        {
            foreach (KeyValuePair<string, string> campo in camposAdicionales)
            {
                // No duplicar campos ya escritos
                string claveUpper = campo.Key.ToUpperInvariant();
                if (claveUpper is "ADIF_VER" or "CREATED_TIMESTAMP" or "PROGRAMID")
                {
                    continue;
                }

                EscribirCampo(sb, campo.Key.ToUpperInvariant(), campo.Value);
                sb.Append(SaltosDeLineaRegistro);
            }
        }

        // Marcador de fin de encabezado
        sb.Append("<EOH>");
        sb.Append(SaltosDeLineaRegistro);
        sb.Append(SaltosDeLineaRegistro);
    }

    /// <summary>
    /// Escribe un registro ADIF completo, incluyendo todos sus campos y el marcador &lt;eor&gt;.
    /// </summary>
    private static void EscribirRegistro(StringBuilder sb, RegistroAdif registro)
    {
        IReadOnlyDictionary<string, string> campos = registro.ObtenerTodosLosCampos();

        // Escribir campos en un orden lógico: primero los más importantes, luego el resto
        string[] camposPrioritarios =
        [
            "CALL", "QSO_DATE", "TIME_ON", "QSO_DATE_OFF", "TIME_OFF",
            "BAND", "FREQ", "MODE", "SUBMODE",
            "RST_SENT", "RST_RCVD", "TX_PWR",
            "STATION_CALLSIGN", "OPERATOR",
            "GRIDSQUARE", "MY_GRIDSQUARE"
        ];

        HashSet<string> camposYaEscritos = new(StringComparer.OrdinalIgnoreCase);

        // Escribir campos prioritarios primero (si existen)
        foreach (string nombreCampo in camposPrioritarios)
        {
            string? valor = registro[nombreCampo];
            if (valor is not null)
            {
                EscribirCampo(sb, nombreCampo, valor);
                camposYaEscritos.Add(nombreCampo);
            }
        }

        // Escribir el resto de campos
        foreach (KeyValuePair<string, string> campo in campos)
        {
            if (!camposYaEscritos.Contains(campo.Key))
            {
                EscribirCampo(sb, campo.Key, campo.Value);
            }
        }

        sb.Append("<EOR>");
        sb.Append(SaltosDeLineaRegistro);
    }

    /// <summary>
    /// Escribe un campo ADIF individual con formato &lt;NOMBRE:LONGITUD&gt;VALOR.
    /// </summary>
    private static void EscribirCampo(StringBuilder sb, string nombreCampo, string valor)
    {
        int longitud = valor.Length;
        sb.Append('<');
        sb.Append(nombreCampo.ToUpperInvariant());
        sb.Append(':');
        sb.Append(longitud.ToString(CultureInfo.InvariantCulture));
        sb.Append('>');
        sb.Append(valor);
    }
}
