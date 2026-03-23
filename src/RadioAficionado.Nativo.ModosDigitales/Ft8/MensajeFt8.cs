using System.Text.RegularExpressions;

namespace RadioAficionado.Nativo.ModosDigitales.Ft8;

/// <summary>
/// Representa un mensaje FT8/FT4 decodificado con todos sus campos parseados.
/// </summary>
public sealed record MensajeFt8
{
    /// <summary>
    /// Indicativo de la estación emisora.
    /// </summary>
    public string? IndicativoEmisor { get; init; }

    /// <summary>
    /// Indicativo de la estación receptora.
    /// </summary>
    public string? IndicativoReceptor { get; init; }

    /// <summary>
    /// Localizador Maidenhead (grid square) si está presente en el mensaje.
    /// </summary>
    public string? Localizador { get; init; }

    /// <summary>
    /// Reporte de señal (SNR en dB) extraído del mensaje.
    /// </summary>
    public int? ReporteSenal { get; init; }

    /// <summary>
    /// Frecuencia de audio en Hz (offset dentro de la banda de audio).
    /// </summary>
    public int FrecuenciaAudioHz { get; init; }

    /// <summary>
    /// Delta de tiempo en segundos respecto al inicio de la ventana.
    /// </summary>
    public double DeltaTiempo { get; init; }

    /// <summary>
    /// Tipo de mensaje FT8 identificado.
    /// </summary>
    public TipoMensajeFt8 TipoMensaje { get; init; }

    /// <summary>
    /// Texto original del mensaje tal como fue decodificado.
    /// </summary>
    public string TextoOriginal { get; init; } = string.Empty;

    /// <summary>
    /// SNR medido durante la decodificación.
    /// </summary>
    public int Snr { get; init; }

    /// <summary>
    /// Marca de tiempo UTC de la decodificación.
    /// </summary>
    public DateTimeOffset MarcaDeTiempo { get; init; }

    // Patrón para indicativos de radioaficionado: 1-2 letras/dígitos + dígito + 1-4 letras
    private static readonly Regex _patronIndicativo = new(
        @"^[A-Z0-9]{1,3}[0-9][A-Z]{1,4}$",
        RegexOptions.Compiled);

    // Patrón para localizador Maidenhead de 4 caracteres: 2 letras + 2 dígitos
    private static readonly Regex _patronLocalizador = new(
        @"^[A-R]{2}[0-9]{2}$",
        RegexOptions.Compiled);

    // Patrón para reporte de señal: signo opcional + 2 dígitos
    private static readonly Regex _patronReporte = new(
        @"^R?([+-]?\d{2})$",
        RegexOptions.Compiled);

    /// <summary>
    /// Parsea un mensaje FT8 en formato texto y extrae los campos estructurados.
    /// Reconoce los formatos estándar: CQ, Respuesta, Reporte, RRR, 73 y texto libre.
    /// </summary>
    /// <param name="textoRaw">Texto del mensaje decodificado.</param>
    /// <param name="frecuenciaAudioHz">Frecuencia de audio en Hz.</param>
    /// <param name="deltaTiempo">Delta de tiempo en segundos.</param>
    /// <param name="snr">SNR medido en dB.</param>
    /// <param name="marcaDeTiempo">Marca de tiempo de la decodificación.</param>
    /// <returns>Un <see cref="MensajeFt8"/> con los campos extraídos.</returns>
    public static MensajeFt8 ParsearMensaje(
        string textoRaw,
        int frecuenciaAudioHz = 0,
        double deltaTiempo = 0.0,
        int snr = 0,
        DateTimeOffset? marcaDeTiempo = null)
    {
        if (string.IsNullOrWhiteSpace(textoRaw))
        {
            return new MensajeFt8
            {
                TextoOriginal = textoRaw ?? string.Empty,
                TipoMensaje = TipoMensajeFt8.Libre,
                FrecuenciaAudioHz = frecuenciaAudioHz,
                DeltaTiempo = deltaTiempo,
                Snr = snr,
                MarcaDeTiempo = marcaDeTiempo ?? DateTimeOffset.UtcNow
            };
        }

        string[] partes = textoRaw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        DateTimeOffset marca = marcaDeTiempo ?? DateTimeOffset.UtcNow;

        // Formato CQ: "CQ EA4K IN80" o "CQ DX EA4K IN80"
        if (partes.Length >= 2 && partes[0].Equals("CQ", StringComparison.OrdinalIgnoreCase))
        {
            return ParsearCq(partes, textoRaw, frecuenciaAudioHz, deltaTiempo, snr, marca);
        }

        // Formatos con dos indicativos
        if (partes.Length >= 2 && EsIndicativo(partes[0]) && EsIndicativo(partes[1]))
        {
            return ParsearMensajeConDosIndicativos(partes, textoRaw, frecuenciaAudioHz, deltaTiempo, snr, marca);
        }

        // Texto libre
        return new MensajeFt8
        {
            TextoOriginal = textoRaw,
            TipoMensaje = TipoMensajeFt8.Libre,
            FrecuenciaAudioHz = frecuenciaAudioHz,
            DeltaTiempo = deltaTiempo,
            Snr = snr,
            MarcaDeTiempo = marca
        };
    }

    private static MensajeFt8 ParsearCq(
        string[] partes,
        string textoRaw,
        int frecuenciaAudioHz,
        double deltaTiempo,
        int snr,
        DateTimeOffset marca)
    {
        // "CQ EA4K IN80" — partes[1] es indicativo, partes[2] puede ser localizador
        // "CQ DX EA4K IN80" — partes[1] es modificador, partes[2] es indicativo
        int indiceIndicativo = 1;

        // Si partes[1] no es indicativo, podría ser un modificador como "DX", "NA", "EU", etc.
        if (partes.Length >= 3 && !EsIndicativo(partes[1]) && EsIndicativo(partes[2]))
        {
            indiceIndicativo = 2;
        }

        string? indicativo = indiceIndicativo < partes.Length ? partes[indiceIndicativo] : null;
        string? localizador = null;

        int indiceLocalizador = indiceIndicativo + 1;
        if (indiceLocalizador < partes.Length && EsLocalizador(partes[indiceLocalizador]))
        {
            localizador = partes[indiceLocalizador];
        }

        return new MensajeFt8
        {
            IndicativoEmisor = indicativo,
            Localizador = localizador,
            TipoMensaje = TipoMensajeFt8.CQ,
            TextoOriginal = textoRaw,
            FrecuenciaAudioHz = frecuenciaAudioHz,
            DeltaTiempo = deltaTiempo,
            Snr = snr,
            MarcaDeTiempo = marca
        };
    }

    private static MensajeFt8 ParsearMensajeConDosIndicativos(
        string[] partes,
        string textoRaw,
        int frecuenciaAudioHz,
        double deltaTiempo,
        int snr,
        DateTimeOffset marca)
    {
        string indicativo1 = partes[0]; // Destinatario
        string indicativo2 = partes[1]; // Emisor

        // "W1AW EA4K RRR"
        if (partes.Length >= 3 && partes[2].Equals("RRR", StringComparison.OrdinalIgnoreCase))
        {
            return new MensajeFt8
            {
                IndicativoEmisor = indicativo2,
                IndicativoReceptor = indicativo1,
                TipoMensaje = TipoMensajeFt8.RRR,
                TextoOriginal = textoRaw,
                FrecuenciaAudioHz = frecuenciaAudioHz,
                DeltaTiempo = deltaTiempo,
                Snr = snr,
                MarcaDeTiempo = marca
            };
        }

        // "EA4K W1AW 73"
        if (partes.Length >= 3 && partes[2].Equals("73", StringComparison.OrdinalIgnoreCase))
        {
            return new MensajeFt8
            {
                IndicativoEmisor = indicativo2,
                IndicativoReceptor = indicativo1,
                TipoMensaje = TipoMensajeFt8.Setenta73,
                TextoOriginal = textoRaw,
                FrecuenciaAudioHz = frecuenciaAudioHz,
                DeltaTiempo = deltaTiempo,
                Snr = snr,
                MarcaDeTiempo = marca
            };
        }

        // "EA4K W1AW R-12" — Reporte con R
        if (partes.Length >= 3 && partes[2].StartsWith("R", StringComparison.OrdinalIgnoreCase))
        {
            Match matchReporte = _patronReporte.Match(partes[2]);
            if (matchReporte.Success)
            {
                int reporte = int.Parse(matchReporte.Groups[1].Value);
                return new MensajeFt8
                {
                    IndicativoEmisor = indicativo2,
                    IndicativoReceptor = indicativo1,
                    ReporteSenal = reporte,
                    TipoMensaje = TipoMensajeFt8.Reporte,
                    TextoOriginal = textoRaw,
                    FrecuenciaAudioHz = frecuenciaAudioHz,
                    DeltaTiempo = deltaTiempo,
                    Snr = snr,
                    MarcaDeTiempo = marca
                };
            }
        }

        // "W1AW EA4K -09" — Respuesta con reporte
        if (partes.Length >= 3)
        {
            Match matchReporte = _patronReporte.Match(partes[2]);
            if (matchReporte.Success)
            {
                int reporte = int.Parse(matchReporte.Groups[1].Value);
                return new MensajeFt8
                {
                    IndicativoEmisor = indicativo2,
                    IndicativoReceptor = indicativo1,
                    ReporteSenal = reporte,
                    TipoMensaje = TipoMensajeFt8.Respuesta,
                    TextoOriginal = textoRaw,
                    FrecuenciaAudioHz = frecuenciaAudioHz,
                    DeltaTiempo = deltaTiempo,
                    Snr = snr,
                    MarcaDeTiempo = marca
                };
            }

            // "W1AW EA4K IN80" — Respuesta con localizador
            if (EsLocalizador(partes[2]))
            {
                return new MensajeFt8
                {
                    IndicativoEmisor = indicativo2,
                    IndicativoReceptor = indicativo1,
                    Localizador = partes[2],
                    TipoMensaje = TipoMensajeFt8.Respuesta,
                    TextoOriginal = textoRaw,
                    FrecuenciaAudioHz = frecuenciaAudioHz,
                    DeltaTiempo = deltaTiempo,
                    Snr = snr,
                    MarcaDeTiempo = marca
                };
            }
        }

        // Dos indicativos sin tercer campo reconocido → Respuesta genérica
        return new MensajeFt8
        {
            IndicativoEmisor = indicativo2,
            IndicativoReceptor = indicativo1,
            TipoMensaje = TipoMensajeFt8.Respuesta,
            TextoOriginal = textoRaw,
            FrecuenciaAudioHz = frecuenciaAudioHz,
            DeltaTiempo = deltaTiempo,
            Snr = snr,
            MarcaDeTiempo = marca
        };
    }

    /// <summary>
    /// Verifica si una cadena tiene formato de indicativo de radioaficionado.
    /// </summary>
    /// <param name="texto">Texto a verificar.</param>
    /// <returns>True si el texto tiene formato de indicativo válido.</returns>
    public static bool EsIndicativo(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        return _patronIndicativo.IsMatch(texto.ToUpperInvariant());
    }

    /// <summary>
    /// Verifica si una cadena tiene formato de localizador Maidenhead de 4 caracteres.
    /// </summary>
    /// <param name="texto">Texto a verificar.</param>
    /// <returns>True si el texto tiene formato de localizador válido.</returns>
    public static bool EsLocalizador(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        return _patronLocalizador.IsMatch(texto.ToUpperInvariant());
    }
}
