using System.Text;
using RadioAficionado.Dominio.Contests;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Infraestructura.Contests;

/// <summary>
/// Genera archivos en formato Cabrillo 3.0, el estándar de la industria
/// para enviar logs de concursos de radioaficionado a los organizadores.
/// </summary>
public class GeneradorCabrillo
{
    /// <summary>
    /// Genera el contenido completo de un archivo Cabrillo 3.0 a partir de los QSOs del contest.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados durante el contest.</param>
    /// <param name="regla">Reglas del contest.</param>
    /// <param name="configuracion">Configuración de la estación participante.</param>
    /// <returns>Contenido del archivo Cabrillo como cadena de texto.</returns>
    /// <exception cref="ArgumentNullException">Si algún parámetro es nulo.</exception>
    public string GenerarCabrillo(
        IReadOnlyList<Qso> qsos,
        ReglaContest regla,
        ConfiguracionContest configuracion)
    {
        ArgumentNullException.ThrowIfNull(qsos);
        ArgumentNullException.ThrowIfNull(regla);
        ArgumentNullException.ThrowIfNull(configuracion);

        StringBuilder sb = new();

        // Cabecera
        sb.AppendLine("START-OF-LOG: 3.0");
        sb.AppendLine($"CONTEST: {regla.Abreviatura}");
        sb.AppendLine($"CALLSIGN: {configuracion.Indicativo.Valor}");
        sb.AppendLine($"CATEGORY-OPERATOR: {configuracion.CategoriaOperador}");
        sb.AppendLine($"CATEGORY-BAND: {configuracion.CategoriaBanda}");
        sb.AppendLine($"CATEGORY-MODE: {configuracion.CategoriaModo}");
        sb.AppendLine($"CATEGORY-POWER: {configuracion.CategoriaPotencia}");
        sb.AppendLine($"CREATED-BY: RadioAficionado v1.0");

        if (!string.IsNullOrWhiteSpace(configuracion.NombreOperador))
        {
            sb.AppendLine($"NAME: {configuracion.NombreOperador}");
        }

        if (!string.IsNullOrWhiteSpace(configuracion.Club))
        {
            sb.AppendLine($"CLUB: {configuracion.Club}");
        }

        if (!string.IsNullOrWhiteSpace(configuracion.Ubicacion))
        {
            sb.AppendLine($"LOCATION: {configuracion.Ubicacion}");
        }

        // QSOs
        foreach (Qso qso in qsos)
        {
            string lineaQso = FormatearLineaQso(qso, configuracion);
            sb.AppendLine(lineaQso);
        }

        sb.AppendLine("END-OF-LOG:");

        return sb.ToString();
    }

    /// <summary>
    /// Formatea una línea QSO individual en formato Cabrillo 3.0.
    /// Formato: QSO: frecKHz MO fecha hora indicativoPropio rstEnv exchEnv indicativoContacto rstRec exchRec
    /// </summary>
    /// <param name="qso">El QSO a formatear.</param>
    /// <param name="configuracion">Configuración de la estación.</param>
    /// <returns>Línea QSO formateada según el estándar Cabrillo.</returns>
    private static string FormatearLineaQso(Qso qso, ConfiguracionContest configuracion)
    {
        string frecuencia = ((int)(qso.Frecuencia.KHz)).ToString();
        string modo = ConvertirModoCabrillo(qso.Modo);
        string fecha = qso.FechaHoraInicio.UtcDateTime.ToString("yyyy-MM-dd");
        string hora = qso.FechaHoraInicio.UtcDateTime.ToString("HHmm");
        string indicativoPropio = configuracion.Indicativo.Valor.PadRight(13);
        string senalEnviada = qso.SenalEnviada.PadRight(3);
        string intercambioEnviado = "00".PadRight(6);
        string indicativoContacto = qso.IndicativoContacto.Valor.PadRight(13);
        string senalRecibida = (!string.IsNullOrWhiteSpace(qso.SenalRecibida) ? qso.SenalRecibida : "59").PadRight(3);
        string intercambioRecibido = "00".PadRight(6);

        return $"QSO: {frecuencia,5} {modo} {fecha} {hora} {indicativoPropio}{senalEnviada}{intercambioEnviado}{indicativoContacto}{senalRecibida}{intercambioRecibido}";
    }

    /// <summary>
    /// Convierte un modo de operación al código Cabrillo correspondiente.
    /// </summary>
    /// <param name="modo">Modo de operación.</param>
    /// <returns>Código Cabrillo del modo (CW, PH, RY, FM).</returns>
    private static string ConvertirModoCabrillo(ModoOperacion modo)
    {
        return modo switch
        {
            ModoOperacion.CW => "CW",
            ModoOperacion.SSB => "PH",
            ModoOperacion.AM => "PH",
            ModoOperacion.FM => "FM",
            ModoOperacion.RTTY => "RY",
            ModoOperacion.FT8 => "DG",
            ModoOperacion.FT4 => "DG",
            ModoOperacion.PSK => "DG",
            _ => "DG"
        };
    }
}
