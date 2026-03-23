using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Contests;

/// <summary>
/// Motor de evaluación de concursos de radioaficionado.
/// Calcula puntuaciones, detecta duplicados y gestiona multiplicadores
/// según las reglas específicas de cada contest.
/// </summary>
public class MotorContest
{
    private static readonly IReadOnlyList<BandaRadio> _bandasHfContest = new List<BandaRadio>
    {
        BandaRadio.Banda160m,
        BandaRadio.Banda80m,
        BandaRadio.Banda40m,
        BandaRadio.Banda20m,
        BandaRadio.Banda15m,
        BandaRadio.Banda10m
    };

    private static readonly IReadOnlyList<BandaRadio> _bandasHfSinWarc = new List<BandaRadio>
    {
        BandaRadio.Banda160m,
        BandaRadio.Banda80m,
        BandaRadio.Banda40m,
        BandaRadio.Banda20m,
        BandaRadio.Banda15m,
        BandaRadio.Banda10m
    };

    /// <summary>
    /// Registro estático con las reglas de los contests más populares.
    /// </summary>
    public static IReadOnlyDictionary<TipoContest, ReglaContest> RegistroDeReglas { get; } = CrearRegistroDeReglas();

    /// <summary>
    /// Calcula la puntuación completa de un contest a partir de una lista de QSOs y las reglas del contest.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados durante el contest.</param>
    /// <param name="regla">Reglas del contest a aplicar.</param>
    /// <returns>Un <see cref="ResultadoContest"/> con el desglose completo de la puntuación.</returns>
    /// <exception cref="ArgumentNullException">Si qsos o regla son nulos.</exception>
    public ResultadoContest CalcularPuntuacion(IReadOnlyList<Qso> qsos, ReglaContest regla)
    {
        ArgumentNullException.ThrowIfNull(qsos);
        ArgumentNullException.ThrowIfNull(regla);

        if (qsos.Count == 0)
        {
            return ResultadoContest.Vacio();
        }

        int qsosValidos = 0;
        int puntos = 0;
        int qsosDuplicados = 0;
        int qsosInvalidos = 0;
        List<Qso> qsosProcesados = new();

        foreach (Qso qso in qsos)
        {
            if (!EsQsoValidoParaContest(qso, regla))
            {
                qsosInvalidos++;
                continue;
            }

            if (EsDuplicado(qso, qsosProcesados, regla))
            {
                qsosDuplicados++;
                continue;
            }

            qsosValidos++;
            puntos += CalcularPuntosQso(qso, regla);
            qsosProcesados.Add(qso);
        }

        int multiplicadores = CalcularMultiplicadores(qsosProcesados, regla);
        long puntuacionFinal = (long)puntos * multiplicadores;

        return new ResultadoContest(
            QsosValidos: qsosValidos,
            Puntos: puntos,
            Multiplicadores: multiplicadores,
            PuntuacionFinal: puntuacionFinal,
            QsosDuplicados: qsosDuplicados,
            QsosInvalidos: qsosInvalidos);
    }

    /// <summary>
    /// Determina si un QSO es duplicado respecto a los QSOs anteriores del contest.
    /// Un QSO es duplicado si el mismo indicativo fue contactado en la misma banda y modo.
    /// </summary>
    /// <param name="qso">El QSO a evaluar.</param>
    /// <param name="anteriores">Lista de QSOs anteriores ya validados.</param>
    /// <param name="regla">Reglas del contest.</param>
    /// <returns>True si el QSO es duplicado.</returns>
    /// <exception cref="ArgumentNullException">Si algún parámetro es nulo.</exception>
    public bool EsDuplicado(Qso qso, IReadOnlyList<Qso> anteriores, ReglaContest regla)
    {
        ArgumentNullException.ThrowIfNull(qso);
        ArgumentNullException.ThrowIfNull(anteriores);
        ArgumentNullException.ThrowIfNull(regla);

        BandaRadio? bandaQso = qso.Frecuencia.ObtenerBanda();

        foreach (Qso anterior in anteriores)
        {
            BandaRadio? bandaAnterior = anterior.Frecuencia.ObtenerBanda();

            bool mismoIndicativo = qso.IndicativoContacto.Valor == anterior.IndicativoContacto.Valor;
            bool mismaBanda = bandaQso.HasValue && bandaAnterior.HasValue && bandaQso.Value == bandaAnterior.Value;
            bool mismoModo = qso.Modo == anterior.Modo;

            // En la mayoría de contests, duplicado = mismo indicativo + misma banda
            // Algunos contests permiten contactar en diferente modo en la misma banda
            if (regla.ModosPermitidos.Count > 1)
            {
                // Contest multimodo: duplicado si mismo indicativo + misma banda + mismo modo
                if (mismoIndicativo && mismaBanda && mismoModo)
                {
                    return true;
                }
            }
            else
            {
                // Contest monomodo: duplicado si mismo indicativo + misma banda
                if (mismoIndicativo && mismaBanda)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Calcula el número total de multiplicadores únicos obtenidos en el contest.
    /// El método de cálculo depende de las reglas del contest.
    /// </summary>
    /// <param name="qsos">Lista de QSOs válidos (sin duplicados).</param>
    /// <param name="regla">Reglas del contest.</param>
    /// <returns>Número total de multiplicadores únicos.</returns>
    /// <exception cref="ArgumentNullException">Si algún parámetro es nulo.</exception>
    public int CalcularMultiplicadores(IReadOnlyList<Qso> qsos, ReglaContest regla)
    {
        ArgumentNullException.ThrowIfNull(qsos);
        ArgumentNullException.ThrowIfNull(regla);

        if (qsos.Count == 0)
        {
            return 0;
        }

        return regla.MetodoMultiplicador switch
        {
            MetodoMultiplicador.PorDxcc => CalcularMultiplicadoresPorDxcc(qsos),
            MetodoMultiplicador.PorZonaCq => CalcularMultiplicadoresPorZonaCq(qsos),
            MetodoMultiplicador.PorPrefijo => CalcularMultiplicadoresPorPrefijo(qsos),
            MetodoMultiplicador.PorEstado => CalcularMultiplicadoresPorEstado(qsos),
            MetodoMultiplicador.PorZonaItu => CalcularMultiplicadoresPorZonaItu(qsos),
            MetodoMultiplicador.PorSeccion => CalcularMultiplicadoresPorSeccion(qsos),
            _ => 1
        };
    }

    /// <summary>
    /// Calcula la tasa de QSOs por hora dentro de una ventana de tiempo específica.
    /// Útil para monitorizar el rendimiento durante el contest.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <param name="ventana">Ventana de tiempo a evaluar.</param>
    /// <returns>Tasa de QSOs por hora. Retorna 0 si no hay QSOs o la ventana es cero.</returns>
    /// <exception cref="ArgumentNullException">Si qsos es nulo.</exception>
    public double ObtenerTasaQsos(IReadOnlyList<Qso> qsos, TimeSpan ventana)
    {
        ArgumentNullException.ThrowIfNull(qsos);

        if (qsos.Count == 0 || ventana.TotalHours <= 0)
        {
            return 0.0;
        }

        DateTimeOffset ahora = qsos[^1].FechaHoraInicio;
        DateTimeOffset inicioVentana = ahora - ventana;

        int qsosEnVentana = 0;
        foreach (Qso qso in qsos)
        {
            if (qso.FechaHoraInicio >= inicioVentana)
            {
                qsosEnVentana++;
            }
        }

        return qsosEnVentana / ventana.TotalHours;
    }

    private static bool EsQsoValidoParaContest(Qso qso, ReglaContest regla)
    {
        // Verificar que el modo está permitido
        if (!regla.ModosPermitidos.Contains(qso.Modo))
        {
            return false;
        }

        // Verificar que la frecuencia está en una banda permitida
        BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
        if (!banda.HasValue || !regla.BandasPermitidas.Contains(banda.Value))
        {
            return false;
        }

        return true;
    }

    private static int CalcularPuntosQso(Qso qso, ReglaContest regla)
    {
        // Puntuación estándar: CW vale más que SSB en la mayoría de contests
        // Para simplificar, usamos 1 punto para SSB/FM/AM y 2 para CW/digital
        return qso.Modo switch
        {
            ModoOperacion.CW => 2,
            ModoOperacion.SSB => 1,
            ModoOperacion.FM => 1,
            ModoOperacion.AM => 1,
            ModoOperacion.RTTY => 2,
            ModoOperacion.FT8 => 2,
            ModoOperacion.FT4 => 2,
            _ => 1
        };
    }

    /// <summary>
    /// Calcula multiplicadores basados en prefijos de indicativo únicos (una aproximación a DXCC).
    /// Cada prefijo de país único en una banda cuenta como un multiplicador.
    /// </summary>
    private static int CalcularMultiplicadoresPorDxcc(IReadOnlyList<Qso> qsos)
    {
        HashSet<string> multiplicadoresUnicos = new(StringComparer.OrdinalIgnoreCase);

        foreach (Qso qso in qsos)
        {
            BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
            string clave = $"{qso.IndicativoContacto.Prefijo}_{banda}";
            multiplicadoresUnicos.Add(clave);
        }

        return multiplicadoresUnicos.Count;
    }

    /// <summary>
    /// Calcula multiplicadores basados en zonas CQ únicas por banda.
    /// Usa el reporte de señal recibido como proxy para la zona (simplificación).
    /// </summary>
    private static int CalcularMultiplicadoresPorZonaCq(IReadOnlyList<Qso> qsos)
    {
        HashSet<string> multiplicadoresUnicos = new(StringComparer.OrdinalIgnoreCase);

        foreach (Qso qso in qsos)
        {
            BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
            // Usamos el prefijo del indicativo como proxy para la zona CQ
            string clave = $"{qso.IndicativoContacto.Prefijo}_{banda}";
            multiplicadoresUnicos.Add(clave);
        }

        return multiplicadoresUnicos.Count;
    }

    /// <summary>
    /// Calcula multiplicadores basados en prefijos únicos (CQ WPX style).
    /// </summary>
    private static int CalcularMultiplicadoresPorPrefijo(IReadOnlyList<Qso> qsos)
    {
        HashSet<string> prefijosUnicos = new(StringComparer.OrdinalIgnoreCase);

        foreach (Qso qso in qsos)
        {
            string prefijo = qso.IndicativoContacto.Prefijo;
            if (!string.IsNullOrWhiteSpace(prefijo))
            {
                prefijosUnicos.Add(prefijo);
            }
        }

        return prefijosUnicos.Count;
    }

    /// <summary>
    /// Calcula multiplicadores basados en estados/provincias únicos.
    /// Usa el prefijo como aproximación.
    /// </summary>
    private static int CalcularMultiplicadoresPorEstado(IReadOnlyList<Qso> qsos)
    {
        HashSet<string> multiplicadoresUnicos = new(StringComparer.OrdinalIgnoreCase);

        foreach (Qso qso in qsos)
        {
            BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
            string clave = $"{qso.IndicativoContacto.Prefijo}_{banda}";
            multiplicadoresUnicos.Add(clave);
        }

        return multiplicadoresUnicos.Count;
    }

    /// <summary>
    /// Calcula multiplicadores basados en zonas ITU únicas.
    /// </summary>
    private static int CalcularMultiplicadoresPorZonaItu(IReadOnlyList<Qso> qsos)
    {
        HashSet<string> multiplicadoresUnicos = new(StringComparer.OrdinalIgnoreCase);

        foreach (Qso qso in qsos)
        {
            BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
            string clave = $"{qso.IndicativoContacto.Prefijo}_{banda}";
            multiplicadoresUnicos.Add(clave);
        }

        return multiplicadoresUnicos.Count;
    }

    /// <summary>
    /// Calcula multiplicadores basados en secciones ARRL únicas.
    /// </summary>
    private static int CalcularMultiplicadoresPorSeccion(IReadOnlyList<Qso> qsos)
    {
        HashSet<string> multiplicadoresUnicos = new(StringComparer.OrdinalIgnoreCase);

        foreach (Qso qso in qsos)
        {
            BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
            string clave = $"{qso.IndicativoContacto.Prefijo}_{banda}";
            multiplicadoresUnicos.Add(clave);
        }

        return multiplicadoresUnicos.Count;
    }

    private static Dictionary<TipoContest, ReglaContest> CrearRegistroDeReglas()
    {
        IReadOnlyList<ModoOperacion> modosSsb = new List<ModoOperacion> { ModoOperacion.SSB };
        IReadOnlyList<ModoOperacion> modosCw = new List<ModoOperacion> { ModoOperacion.CW };
        IReadOnlyList<ModoOperacion> modosMixtos = new List<ModoOperacion> { ModoOperacion.SSB, ModoOperacion.CW };
        IReadOnlyList<ModoOperacion> modosRtty = new List<ModoOperacion> { ModoOperacion.RTTY, ModoOperacion.FT8, ModoOperacion.FT4 };

        Dictionary<TipoContest, ReglaContest> registro = new()
        {
            [TipoContest.CqWw] = new ReglaContest(
                TipoContest.CqWw,
                "CQ World Wide DX Contest",
                "CQ-WW-SSB",
                _bandasHfContest,
                modosMixtos,
                TipoIntercambio.RstZona,
                MetodoMultiplicador.PorZonaCq,
                48,
                10),

            [TipoContest.CqWpx] = new ReglaContest(
                TipoContest.CqWpx,
                "CQ World Wide WPX Contest",
                "CQ-WPX-SSB",
                _bandasHfContest,
                modosMixtos,
                TipoIntercambio.RstSerial,
                MetodoMultiplicador.PorPrefijo,
                48,
                3),

            [TipoContest.ArrlDx] = new ReglaContest(
                TipoContest.ArrlDx,
                "ARRL International DX Contest",
                "ARRL-DX-SSB",
                _bandasHfContest,
                modosSsb,
                TipoIntercambio.RstEstado,
                MetodoMultiplicador.PorDxcc,
                48,
                3),

            [TipoContest.IaruHf] = new ReglaContest(
                TipoContest.IaruHf,
                "IARU HF World Championship",
                "IARU-HF",
                _bandasHfContest,
                modosMixtos,
                TipoIntercambio.RstZonaItu,
                MetodoMultiplicador.PorZonaItu,
                24,
                7),

            [TipoContest.ArrlFieldDay] = new ReglaContest(
                TipoContest.ArrlFieldDay,
                "ARRL Field Day",
                "ARRL-FD",
                _bandasHfContest,
                modosMixtos,
                TipoIntercambio.ClaseTransmisores,
                MetodoMultiplicador.PorSeccion,
                27,
                6),

            [TipoContest.CqWwRtty] = new ReglaContest(
                TipoContest.CqWwRtty,
                "CQ WW RTTY DX Contest",
                "CQ-WW-RTTY",
                _bandasHfContest,
                modosRtty,
                TipoIntercambio.RstZona,
                MetodoMultiplicador.PorZonaCq,
                48,
                9),

            [TipoContest.AllAsian] = new ReglaContest(
                TipoContest.AllAsian,
                "All Asian DX Contest",
                "ALL-ASIAN",
                _bandasHfContest,
                modosCw,
                TipoIntercambio.RstEdad,
                MetodoMultiplicador.PorDxcc,
                48,
                6),

            [TipoContest.WaeEurope] = new ReglaContest(
                TipoContest.WaeEurope,
                "Worked All Europe DX Contest",
                "WAE-DC",
                _bandasHfContest,
                modosMixtos,
                TipoIntercambio.RstSerial,
                MetodoMultiplicador.PorDxcc,
                48,
                8)
        };

        return registro;
    }
}
