namespace RadioAficionado.Nativo.ModosDigitales.Ft8;

/// <summary>
/// Genera mensajes FT8 formateados para transmisión según el protocolo estándar.
/// Cada método produce el texto que se codificará en tonos FT8.
/// </summary>
public static class GeneradorMensajeFt8
{
    /// <summary>
    /// Genera un mensaje de llamada general (CQ).
    /// Formato: "CQ {indicativo} {localizador}"
    /// </summary>
    /// <param name="indicativo">Indicativo de la estación que llama.</param>
    /// <param name="localizador">Localizador Maidenhead de 4 caracteres.</param>
    /// <returns>Mensaje CQ formateado.</returns>
    /// <exception cref="ArgumentException">Si el indicativo o localizador están vacíos.</exception>
    public static string GenerarCQ(string indicativo, string localizador)
    {
        ValidarParametro(indicativo, nameof(indicativo));
        ValidarParametro(localizador, nameof(localizador));

        return $"CQ {indicativo.ToUpperInvariant()} {localizador.ToUpperInvariant()}";
    }

    /// <summary>
    /// Genera un mensaje de respuesta a un CQ con reporte de señal.
    /// Formato: "{indicativoDestino} {miIndicativo} {reporte}"
    /// </summary>
    /// <param name="indicativoDestino">Indicativo de la estación a la que se responde.</param>
    /// <param name="miIndicativo">Indicativo propio.</param>
    /// <param name="reporte">Reporte de señal en dB (típicamente entre -30 y +20).</param>
    /// <returns>Mensaje de respuesta formateado.</returns>
    /// <exception cref="ArgumentException">Si algún indicativo está vacío.</exception>
    public static string GenerarRespuesta(string indicativoDestino, string miIndicativo, int reporte)
    {
        ValidarParametro(indicativoDestino, nameof(indicativoDestino));
        ValidarParametro(miIndicativo, nameof(miIndicativo));

        string reporteFormateado = reporte >= 0 ? $"+{reporte:D2}" : $"{reporte:D2}";
        return $"{indicativoDestino.ToUpperInvariant()} {miIndicativo.ToUpperInvariant()} {reporteFormateado}";
    }

    /// <summary>
    /// Genera un mensaje de reporte con confirmación (R + reporte).
    /// Formato: "{indicativoDestino} {miIndicativo} R{reporte}"
    /// </summary>
    /// <param name="indicativoDestino">Indicativo de la estación destino.</param>
    /// <param name="miIndicativo">Indicativo propio.</param>
    /// <param name="reporte">Reporte de señal en dB.</param>
    /// <returns>Mensaje de reporte formateado.</returns>
    /// <exception cref="ArgumentException">Si algún indicativo está vacío.</exception>
    public static string GenerarReporte(string indicativoDestino, string miIndicativo, int reporte)
    {
        ValidarParametro(indicativoDestino, nameof(indicativoDestino));
        ValidarParametro(miIndicativo, nameof(miIndicativo));

        string reporteFormateado = reporte >= 0 ? $"R+{reporte:D2}" : $"R{reporte:D2}";
        return $"{indicativoDestino.ToUpperInvariant()} {miIndicativo.ToUpperInvariant()} {reporteFormateado}";
    }

    /// <summary>
    /// Genera un mensaje RRR (confirmación de recepción del reporte).
    /// Formato: "{indicativoDestino} {miIndicativo} RRR"
    /// </summary>
    /// <param name="indicativoDestino">Indicativo de la estación destino.</param>
    /// <param name="miIndicativo">Indicativo propio.</param>
    /// <returns>Mensaje RRR formateado.</returns>
    /// <exception cref="ArgumentException">Si algún indicativo está vacío.</exception>
    public static string GenerarRRR(string indicativoDestino, string miIndicativo)
    {
        ValidarParametro(indicativoDestino, nameof(indicativoDestino));
        ValidarParametro(miIndicativo, nameof(miIndicativo));

        return $"{indicativoDestino.ToUpperInvariant()} {miIndicativo.ToUpperInvariant()} RRR";
    }

    /// <summary>
    /// Genera un mensaje de despedida (73).
    /// Formato: "{indicativoDestino} {miIndicativo} 73"
    /// </summary>
    /// <param name="indicativoDestino">Indicativo de la estación destino.</param>
    /// <param name="miIndicativo">Indicativo propio.</param>
    /// <returns>Mensaje 73 formateado.</returns>
    /// <exception cref="ArgumentException">Si algún indicativo está vacío.</exception>
    public static string Generar73(string indicativoDestino, string miIndicativo)
    {
        ValidarParametro(indicativoDestino, nameof(indicativoDestino));
        ValidarParametro(miIndicativo, nameof(miIndicativo));

        return $"{indicativoDestino.ToUpperInvariant()} {miIndicativo.ToUpperInvariant()} 73";
    }

    private static void ValidarParametro(string valor, string nombreParametro)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException("El valor no puede estar vacío.", nombreParametro);
        }
    }
}
