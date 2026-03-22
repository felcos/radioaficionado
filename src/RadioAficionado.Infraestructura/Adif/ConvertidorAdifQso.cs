using System.Globalization;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Infraestructura.Adif;

/// <summary>
/// Convierte entre registros ADIF y entidades Qso del dominio.
/// Maneja las transformaciones de formatos de fecha, frecuencia, modo e indicativo
/// entre la representación ADIF (cadenas de texto) y los objetos de valor del dominio.
/// </summary>
public static class ConvertidorAdifQso
{
    // Mapeo de nombres ADIF de banda a frecuencias centrales aproximadas (MHz).
    // Se usa como fallback cuando FREQ no está presente pero BAND sí.
    private static readonly Dictionary<string, double> _frecuenciaCentralPorBanda = new(StringComparer.OrdinalIgnoreCase)
    {
        ["2190m"] = 0.1357,
        ["630m"] = 0.4725,
        ["560m"] = 0.501,
        ["160m"] = 1.9,
        ["80m"] = 3.75,
        ["60m"] = 5.3585,
        ["40m"] = 7.15,
        ["30m"] = 10.125,
        ["20m"] = 14.175,
        ["17m"] = 18.118,
        ["15m"] = 21.225,
        ["12m"] = 24.94,
        ["10m"] = 28.85,
        ["8m"] = 40.0,
        ["6m"] = 52.0,
        ["5m"] = 54.0,
        ["4m"] = 70.25,
        ["2m"] = 146.0,
        ["1.25m"] = 222.0,
        ["70cm"] = 435.0,
        ["33cm"] = 915.0,
        ["23cm"] = 1270.0,
        ["13cm"] = 2375.0,
        ["9cm"] = 3400.0,
        ["6cm"] = 5760.0,
        ["3cm"] = 10250.0,
        ["1.25cm"] = 24125.0
    };

    /// <summary>
    /// Convierte un registro ADIF a una entidad Qso del dominio.
    /// Devuelve null si faltan campos obligatorios (CALL, QSO_DATE, TIME_ON) o si los datos son inválidos.
    /// </summary>
    /// <param name="registro">Registro ADIF a convertir.</param>
    /// <returns>Entidad Qso, o null si los datos son insuficientes o inválidos.</returns>
    /// <exception cref="ArgumentNullException">Si el registro es null.</exception>
    public static Qso? ConvertirAQso(RegistroAdif registro)
    {
        ArgumentNullException.ThrowIfNull(registro);

        // Campo obligatorio: indicativo contactado
        if (string.IsNullOrWhiteSpace(registro.Indicativo))
        {
            return null;
        }

        try
        {
            // Parsear indicativo contactado
            Indicativo indicativoContacto = new(registro.Indicativo);

            // Parsear indicativo propio (STATION_CALLSIGN > OPERATOR > fallback)
            string? indicativoPropioTexto = registro.IndicativoPropio ?? registro.Operador;
            Indicativo indicativoPropio = !string.IsNullOrWhiteSpace(indicativoPropioTexto)
                ? new Indicativo(indicativoPropioTexto)
                : new Indicativo("N0CALL");

            // Parsear fecha y hora de inicio
            DateTimeOffset fechaHoraInicio = ParsearFechaHora(registro.FechaQso, registro.HoraInicio);

            // Parsear frecuencia (FREQ preferido, BAND como fallback)
            Frecuencia frecuencia = ParsearFrecuencia(registro.Frecuencia, registro.Banda);

            // Parsear modo de operación
            ModoOperacion modo = ParsearModo(registro.Modo);

            // Señal enviada (usar valor por defecto según el modo si no existe)
            string senalEnviada = !string.IsNullOrWhiteSpace(registro.SenalEnviada)
                ? registro.SenalEnviada
                : ObtenerSenalPorDefecto(modo);

            // Potencia
            double? potencia = ParsearPotencia(registro.Potencia);

            // Localizador del contacto
            Localizador? localizador = ParsearLocalizador(registro.Localizador);

            // Notas: combinar COMMENT y NOTES si ambos existen
            string? notas = CombinarNotas(registro.Comentario, registro.Notas);

            // Crear el QSO
            Qso qso = Qso.Crear(
                indicativoPropio,
                indicativoContacto,
                fechaHoraInicio,
                frecuencia,
                modo,
                senalEnviada,
                potencia: potencia,
                localizadorContacto: localizador,
                notas: notas);

            // Completar el QSO si hay señal recibida
            if (!string.IsNullOrWhiteSpace(registro.SenalRecibida))
            {
                DateTimeOffset fechaFin;
                if (!string.IsNullOrWhiteSpace(registro.HoraFin))
                {
                    string? fechaFinTexto = registro.FechaQsoFin ?? registro.FechaQso;
                    fechaFin = ParsearFechaHora(fechaFinTexto, registro.HoraFin);
                }
                else
                {
                    // Sin hora de fin: asumir 1 minuto después del inicio
                    fechaFin = fechaHoraInicio.AddMinutes(1);
                }

                try
                {
                    qso.Completar(fechaFin, registro.SenalRecibida);
                }
                catch (ArgumentException)
                {
                    // Fecha fin anterior a inicio u otro problema — dejar QSO sin completar
                }
                catch (InvalidOperationException)
                {
                    // QSO ya completado — ignorar
                }
            }

            return qso;
        }
        catch (Exception)
        {
            // Cualquier error de parseo o validación — devolver null
            return null;
        }
    }

    /// <summary>
    /// Convierte una entidad Qso del dominio a un registro ADIF.
    /// </summary>
    /// <param name="qso">Entidad Qso a convertir.</param>
    /// <returns>Registro ADIF con todos los campos disponibles del Qso.</returns>
    /// <exception cref="ArgumentNullException">Si el Qso es null.</exception>
    public static RegistroAdif ConvertirAAdif(Qso qso)
    {
        ArgumentNullException.ThrowIfNull(qso);

        RegistroAdif registro = new();

        // Indicativos
        registro.Indicativo = qso.IndicativoContacto.Valor;
        registro.IndicativoPropio = qso.IndicativoPropio.Valor;

        // Fecha y hora de inicio
        registro.FechaQso = qso.FechaHoraInicio.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        registro.HoraInicio = qso.FechaHoraInicio.UtcDateTime.ToString("HHmmss", CultureInfo.InvariantCulture);

        // Fecha y hora de fin
        if (qso.FechaHoraFin.HasValue)
        {
            registro.FechaQsoFin = qso.FechaHoraFin.Value.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            registro.HoraFin = qso.FechaHoraFin.Value.UtcDateTime.ToString("HHmmss", CultureInfo.InvariantCulture);
        }

        // Frecuencia y banda
        registro.Frecuencia = qso.Frecuencia.MHz.ToString("F6", CultureInfo.InvariantCulture);
        registro.Modo = qso.Modo.ObtenerNombreAdif();

        BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
        if (banda.HasValue)
        {
            registro.Banda = ConvertirBandaAAdif(banda.Value);
        }

        // Señales
        if (!string.IsNullOrWhiteSpace(qso.SenalEnviada))
        {
            registro.SenalEnviada = qso.SenalEnviada;
        }

        if (!string.IsNullOrWhiteSpace(qso.SenalRecibida))
        {
            registro.SenalRecibida = qso.SenalRecibida;
        }

        // Potencia
        if (qso.Potencia.HasValue)
        {
            // Formato sin decimales innecesarios: 50 en vez de 50.0, pero 50.5 se mantiene
            registro.Potencia = qso.Potencia.Value % 1 == 0
                ? qso.Potencia.Value.ToString("0", CultureInfo.InvariantCulture)
                : qso.Potencia.Value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        // Localizador del contacto
        if (qso.LocalizadorContacto.HasValue)
        {
            registro.Localizador = qso.LocalizadorContacto.Value.Valor;
        }

        // Notas
        if (!string.IsNullOrWhiteSpace(qso.Notas))
        {
            registro.Comentario = qso.Notas;
        }

        return registro;
    }

    /// <summary>
    /// Convierte una lista de registros ADIF a entidades Qso, descartando los inválidos.
    /// </summary>
    /// <param name="registros">Lista de registros ADIF.</param>
    /// <returns>Tupla con la lista de entidades Qso válidas y la cantidad de registros descartados.</returns>
    /// <exception cref="ArgumentNullException">Si la lista de registros es null.</exception>
    public static (IReadOnlyList<Qso> Qsos, int Descartados) ConvertirListaAQsos(IEnumerable<RegistroAdif> registros)
    {
        ArgumentNullException.ThrowIfNull(registros);

        List<Qso> qsos = new();
        int descartados = 0;

        foreach (RegistroAdif registro in registros)
        {
            Qso? qso = ConvertirAQso(registro);
            if (qso is not null)
            {
                qsos.Add(qso);
            }
            else
            {
                descartados++;
            }
        }

        return (qsos, descartados);
    }

    /// <summary>
    /// Convierte una lista de entidades Qso a registros ADIF.
    /// </summary>
    /// <param name="qsos">Lista de entidades Qso.</param>
    /// <returns>Lista de registros ADIF.</returns>
    /// <exception cref="ArgumentNullException">Si la lista de Qsos es null.</exception>
    public static IReadOnlyList<RegistroAdif> ConvertirListaAAdif(IEnumerable<Qso> qsos)
    {
        ArgumentNullException.ThrowIfNull(qsos);

        List<RegistroAdif> registros = new();

        foreach (Qso qso in qsos)
        {
            registros.Add(ConvertirAAdif(qso));
        }

        return registros;
    }

    // ─────────────────────────────────────────────────────────
    // Métodos auxiliares de parseo
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Parsea fecha (YYYYMMDD) y hora (HHMM o HHMMSS) en formato ADIF a DateTimeOffset UTC.
    /// Si los datos son inválidos, devuelve DateTimeOffset.UtcNow como fallback.
    /// </summary>
    private static DateTimeOffset ParsearFechaHora(string? fecha, string? hora)
    {
        if (string.IsNullOrWhiteSpace(fecha) || fecha.Length != 8)
        {
            return DateTimeOffset.UtcNow;
        }

        if (!int.TryParse(fecha[..4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int anio) ||
            !int.TryParse(fecha[4..6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mes) ||
            !int.TryParse(fecha[6..8], NumberStyles.Integer, CultureInfo.InvariantCulture, out int dia))
        {
            return DateTimeOffset.UtcNow;
        }

        int horas = 0;
        int minutos = 0;
        int segundos = 0;

        if (!string.IsNullOrWhiteSpace(hora) && hora.Length >= 4)
        {
            int.TryParse(hora[..2], NumberStyles.Integer, CultureInfo.InvariantCulture, out horas);
            int.TryParse(hora[2..4], NumberStyles.Integer, CultureInfo.InvariantCulture, out minutos);
            if (hora.Length >= 6)
            {
                int.TryParse(hora[4..6], NumberStyles.Integer, CultureInfo.InvariantCulture, out segundos);
            }
        }

        try
        {
            return new DateTimeOffset(anio, mes, dia, horas, minutos, segundos, TimeSpan.Zero);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Parsea la frecuencia desde el campo FREQ (MHz) o calcula una aproximación desde BAND.
    /// Si ambos fallan, devuelve 14.0 MHz (20m) como fallback seguro.
    /// </summary>
    private static Frecuencia ParsearFrecuencia(string? frecuenciaTexto, string? bandaTexto)
    {
        // Intentar primero con FREQ (valor exacto en MHz)
        if (!string.IsNullOrWhiteSpace(frecuenciaTexto) &&
            double.TryParse(frecuenciaTexto, NumberStyles.Float, CultureInfo.InvariantCulture, out double mhz) &&
            mhz > 0)
        {
            try
            {
                return Frecuencia.DesdeMHz(mhz);
            }
            catch (ArgumentException)
            {
                // Valor inválido, intentar con BAND
            }
        }

        // Fallback: usar frecuencia central de la banda
        if (!string.IsNullOrWhiteSpace(bandaTexto) &&
            _frecuenciaCentralPorBanda.TryGetValue(bandaTexto, out double frecuenciaCentral))
        {
            try
            {
                return Frecuencia.DesdeMHz(frecuenciaCentral);
            }
            catch (ArgumentException)
            {
                // Continuar al fallback final
            }
        }

        // Fallback final: 20m (14.0 MHz)
        return Frecuencia.DesdeMHz(14.0);
    }

    /// <summary>
    /// Parsea el modo de operación desde la cadena ADIF.
    /// Devuelve SSB como valor por defecto si el modo no se reconoce.
    /// </summary>
    private static ModoOperacion ParsearModo(string? modoTexto)
    {
        if (string.IsNullOrWhiteSpace(modoTexto))
        {
            return ModoOperacion.SSB;
        }

        if (ModoOperacionExtensiones.IntentarDesdeAdif(modoTexto, out ModoOperacion modo))
        {
            return modo;
        }

        // Mapeos adicionales para modos escritos de forma diferente en algunos programas
        string modoUpper = modoTexto.ToUpperInvariant();
        return modoUpper switch
        {
            "USB" or "LSB" => ModoOperacion.SSB,
            "FSK" => ModoOperacion.RTTY,
            "PACKET" => ModoOperacion.PKT,
            "DATA" or "DIGITAL" => ModoOperacion.FT8,
            _ => ModoOperacion.SSB
        };
    }

    /// <summary>
    /// Parsea la potencia en vatios desde una cadena. Devuelve null si no es válida.
    /// </summary>
    private static double? ParsearPotencia(string? potenciaTexto)
    {
        if (string.IsNullOrWhiteSpace(potenciaTexto))
        {
            return null;
        }

        if (double.TryParse(potenciaTexto, NumberStyles.Float, CultureInfo.InvariantCulture, out double vatios) && vatios > 0)
        {
            return vatios;
        }

        return null;
    }

    /// <summary>
    /// Parsea un localizador Maidenhead. Devuelve null si el formato es inválido.
    /// </summary>
    private static Localizador? ParsearLocalizador(string? localizadorTexto)
    {
        if (string.IsNullOrWhiteSpace(localizadorTexto))
        {
            return null;
        }

        try
        {
            return new Localizador(localizadorTexto);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Combina los campos COMMENT y NOTES en una sola cadena de notas.
    /// </summary>
    private static string? CombinarNotas(string? comentario, string? notas)
    {
        bool tieneComentario = !string.IsNullOrWhiteSpace(comentario);
        bool tieneNotas = !string.IsNullOrWhiteSpace(notas);

        if (tieneComentario && tieneNotas)
        {
            return $"{comentario} | {notas}";
        }

        return tieneComentario ? comentario : tieneNotas ? notas : null;
    }

    /// <summary>
    /// Obtiene un reporte de señal por defecto según el modo de operación.
    /// </summary>
    private static string ObtenerSenalPorDefecto(ModoOperacion modo)
    {
        if (modo is ModoOperacion.CW)
        {
            return "599";
        }

        if (modo.EsDigital())
        {
            return "-10";
        }

        return "59";
    }

    /// <summary>
    /// Convierte un valor de BandaRadio del dominio a la cadena ADIF estándar de banda.
    /// </summary>
    private static string ConvertirBandaAAdif(BandaRadio banda)
    {
        return banda switch
        {
            BandaRadio.Banda2200m => "2190m",
            BandaRadio.Banda630m => "630m",
            BandaRadio.Banda160m => "160m",
            BandaRadio.Banda80m => "80m",
            BandaRadio.Banda60m => "60m",
            BandaRadio.Banda40m => "40m",
            BandaRadio.Banda30m => "30m",
            BandaRadio.Banda20m => "20m",
            BandaRadio.Banda17m => "17m",
            BandaRadio.Banda15m => "15m",
            BandaRadio.Banda12m => "12m",
            BandaRadio.Banda10m => "10m",
            BandaRadio.Banda6m => "6m",
            BandaRadio.Banda4m => "4m",
            BandaRadio.Banda2m => "2m",
            BandaRadio.Banda1_25m => "1.25m",
            BandaRadio.Banda70cm => "70cm",
            BandaRadio.Banda33cm => "33cm",
            BandaRadio.Banda23cm => "23cm",
            BandaRadio.Banda13cm => "13cm",
            BandaRadio.Banda9cm => "9cm",
            BandaRadio.Banda5cm => "6cm",
            BandaRadio.Banda3cm => "3cm",
            BandaRadio.Banda1_2cm => "1.25cm",
            _ => "20m"
        };
    }
}
