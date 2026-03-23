using System.Globalization;
using RadioAficionado.Compartido.Constantes;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Satelites;

namespace RadioAficionado.Infraestructura.Satelites;

/// <summary>
/// Calculador orbital simplificado basado en SGP4 para satélites en órbita baja terrestre (LEO).
/// Precisión objetivo: ±1 grado en posición, ±30 segundos en predicción de pasos.
/// Suficiente para operaciones de radioaficionado con satélites amateur.
/// </summary>
public sealed class CalculadorOrbital
{
    /// <summary>Constante gravitacional terrestre (km³/s²).</summary>
    private const double Mu = 398600.4418;

    /// <summary>Radio ecuatorial de la Tierra en km (WGS-84).</summary>
    private const double RadioTerrestreKm = 6378.137;

    /// <summary>Aplanamiento terrestre (WGS-84).</summary>
    private const double Aplanamiento = 1.0 / 298.257223563;

    /// <summary>J2 — segundo armónico zonal del geopotencial.</summary>
    private const double J2 = 1.08263e-3;

    /// <summary>Minutos por día sidéreo.</summary>
    private const double MinutosPorDia = 1440.0;

    /// <summary>2π.</summary>
    private const double DosPi = 2.0 * Math.PI;

    /// <summary>
    /// Parsea un TLE (Two-Line Element) a partir de tres líneas de texto.
    /// </summary>
    /// <param name="linea0">Línea 0 — nombre del satélite.</param>
    /// <param name="linea1">Línea 1 — datos de identificación y época.</param>
    /// <param name="linea2">Línea 2 — elementos orbitales.</param>
    /// <returns>Objeto <see cref="Tle"/> con los elementos parseados.</returns>
    /// <exception cref="FormatException">Si las líneas no tienen el formato TLE correcto.</exception>
    public static Tle ParsearTle(string linea0, string linea1, string linea2)
    {
        if (string.IsNullOrWhiteSpace(linea0))
        {
            throw new ArgumentException("La línea 0 (nombre) no puede estar vacía.", nameof(linea0));
        }

        if (string.IsNullOrWhiteSpace(linea1) || linea1.Length < 69)
        {
            throw new FormatException("La línea 1 del TLE no tiene el formato correcto (mínimo 69 caracteres).");
        }

        if (string.IsNullOrWhiteSpace(linea2) || linea2.Length < 69)
        {
            throw new FormatException("La línea 2 del TLE no tiene el formato correcto (mínimo 69 caracteres).");
        }

        string nombre = linea0.Trim();

        // Línea 1: número NORAD, época, derivada movimiento medio, BSTAR
        int numeroNorad = int.Parse(linea1.Substring(2, 5).Trim(), CultureInfo.InvariantCulture);
        int anioEpoca = int.Parse(linea1.Substring(18, 2).Trim(), CultureInfo.InvariantCulture);
        double diaEpoca = double.Parse(linea1.Substring(20, 12).Trim(), CultureInfo.InvariantCulture);

        // Derivada del movimiento medio (posición 33-43)
        double derivadaMM = double.Parse(linea1.Substring(33, 10).Trim(), CultureInfo.InvariantCulture);

        // BSTAR (posición 53-61): formato especial con exponente implícito
        double bstar = ParsearExponenteImplicito(linea1.Substring(53, 8).Trim());

        // Convertir época a DateTime
        int anioCompleto = anioEpoca < 57 ? 2000 + anioEpoca : 1900 + anioEpoca;
        DateTime epoca = new DateTime(anioCompleto, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(diaEpoca - 1.0);

        // Línea 2: elementos orbitales
        double inclinacion = double.Parse(linea2.Substring(8, 8).Trim(), CultureInfo.InvariantCulture);
        double raan = double.Parse(linea2.Substring(17, 8).Trim(), CultureInfo.InvariantCulture);

        // Excentricidad: punto decimal implícito al inicio
        string excentricidadTexto = "0." + linea2.Substring(26, 7).Trim();
        double excentricidad = double.Parse(excentricidadTexto, CultureInfo.InvariantCulture);

        double argPerigeo = double.Parse(linea2.Substring(34, 8).Trim(), CultureInfo.InvariantCulture);
        double anomaliaMedia = double.Parse(linea2.Substring(43, 8).Trim(), CultureInfo.InvariantCulture);
        double movimientoMedio = double.Parse(linea2.Substring(52, 11).Trim(), CultureInfo.InvariantCulture);

        int numeroRevolucion = 0;
        string revTexto = linea2.Substring(63, 5).Trim();
        if (!string.IsNullOrWhiteSpace(revTexto))
        {
            int.TryParse(revTexto, CultureInfo.InvariantCulture, out numeroRevolucion);
        }

        return new Tle(
            nombre,
            numeroNorad,
            epoca,
            inclinacion,
            raan,
            excentricidad,
            argPerigeo,
            anomaliaMedia,
            movimientoMedio,
            derivadaMM,
            bstar,
            numeroRevolucion);
    }

    /// <summary>
    /// Parsea múltiples TLE desde un texto con formato estándar (líneas de 3 en 3).
    /// </summary>
    /// <param name="textoTle">Texto con TLEs, separados por saltos de línea.</param>
    /// <returns>Lista de TLEs parseados.</returns>
    public static IReadOnlyList<Tle> ParsearMultiplesTle(string textoTle)
    {
        if (string.IsNullOrWhiteSpace(textoTle))
        {
            return Array.Empty<Tle>();
        }

        string[] lineas = textoTle.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        List<Tle> resultados = new();

        for (int i = 0; i + 2 < lineas.Length; i += 3)
        {
            string l0 = lineas[i].Trim();
            string l1 = lineas[i + 1].Trim();
            string l2 = lineas[i + 2].Trim();

            // Verificar que línea 1 y 2 empiezan con 1 y 2
            if (l1.Length >= 1 && l1[0] == '1' && l2.Length >= 1 && l2[0] == '2')
            {
                try
                {
                    Tle tle = ParsearTle(l0, l1, l2);
                    resultados.Add(tle);
                }
                catch (FormatException)
                {
                    // TLE malformado, saltar
                }
            }
        }

        return resultados.AsReadOnly();
    }

    /// <summary>
    /// Calcula la posición de un satélite en un momento dado, vista desde un observador en tierra.
    /// Implementación simplificada de SGP4 para satélites LEO.
    /// </summary>
    /// <param name="tle">Elementos TLE del satélite.</param>
    /// <param name="observador">Coordenadas del observador en tierra.</param>
    /// <param name="momento">Fecha y hora UTC del cálculo.</param>
    /// <returns>Posición del satélite relativa al observador.</returns>
    public static PosicionSatelite CalcularPosicion(Tle tle, Coordenadas observador, DateTime momento)
    {
        if (tle is null)
        {
            throw new ArgumentNullException(nameof(tle));
        }

        // Tiempo transcurrido desde la época en minutos
        double deltaMinutos = (momento.ToUniversalTime() - tle.Epoca).TotalMinutes;

        // Elementos orbitales en radianes
        double inclinacion = tle.InclinacionGrados * ConstantesRadio.GradosARadianes;
        double raan0 = tle.AscensionRectaGrados * ConstantesRadio.GradosARadianes;
        double excentricidad = tle.Excentricidad;
        double argPerigeo0 = tle.ArgumentoPerigeoGrados * ConstantesRadio.GradosARadianes;
        double anomaliaMedia0 = tle.AnomaliaMediaGrados * ConstantesRadio.GradosARadianes;
        double movimientoMedioRadMin = tle.MovimientoMedioRevDia * DosPi / MinutosPorDia;

        // Semieje mayor a partir del movimiento medio
        double n0 = tle.MovimientoMedioRevDia * DosPi / 86400.0; // rad/s
        double a0 = Math.Pow(Mu / (n0 * n0), 1.0 / 3.0); // km

        // Perturbaciones seculares J2
        double cosI = Math.Cos(inclinacion);
        double sinI = Math.Sin(inclinacion);
        double p = a0 * (1.0 - excentricidad * excentricidad);
        double factorJ2 = 1.5 * J2 * (RadioTerrestreKm / p) * (RadioTerrestreKm / p);

        // Tasa de precesión del RAAN
        double raanPunto = -factorJ2 * movimientoMedioRadMin * cosI;

        // Tasa de precesión del argumento del perigeo
        double argPerigeoPunto = factorJ2 * movimientoMedioRadMin * (2.0 - 2.5 * sinI * sinI);

        // Elementos propagados
        double anomaliaMedia = anomaliaMedia0 + movimientoMedioRadMin * deltaMinutos;
        anomaliaMedia = NormalizarAngulo(anomaliaMedia);

        double raan = raan0 + raanPunto * deltaMinutos;
        double argPerigeo = argPerigeo0 + argPerigeoPunto * deltaMinutos;

        // Resolver ecuación de Kepler (anomalía excéntrica)
        double anomaliaExcentrica = ResolverKepler(anomaliaMedia, excentricidad);

        // Anomalía verdadera
        double sinE = Math.Sin(anomaliaExcentrica);
        double cosE = Math.Cos(anomaliaExcentrica);
        double anomaliaVerdadera = Math.Atan2(
            Math.Sqrt(1.0 - excentricidad * excentricidad) * sinE,
            cosE - excentricidad);

        // Radio orbital
        double radio = a0 * (1.0 - excentricidad * cosE);

        // Posición en el plano orbital
        double argLatitud = anomaliaVerdadera + argPerigeo;
        double cosArgLat = Math.Cos(argLatitud);
        double sinArgLat = Math.Sin(argLatitud);
        double cosRaan = Math.Cos(raan);
        double sinRaan = Math.Sin(raan);

        // Coordenadas ECI (Earth-Centered Inertial)
        double xEci = radio * (cosRaan * cosArgLat - sinRaan * sinArgLat * cosI);
        double yEci = radio * (sinRaan * cosArgLat + cosRaan * sinArgLat * cosI);
        double zEci = radio * sinArgLat * sinI;

        // Convertir ECI a ECEF (rotar por GMST)
        double gmst = CalcularGmst(momento);
        double cosGmst = Math.Cos(gmst);
        double sinGmst = Math.Sin(gmst);

        double xEcef = xEci * cosGmst + yEci * sinGmst;
        double yEcef = -xEci * sinGmst + yEci * cosGmst;
        double zEcef = zEci;

        // Coordenadas geodésicas del punto subsatélite
        double longitudSat = Math.Atan2(yEcef, xEcef) * ConstantesRadio.RadianesAGrados;
        double latitudSat = Math.Atan2(zEcef, Math.Sqrt(xEcef * xEcef + yEcef * yEcef)) * ConstantesRadio.RadianesAGrados;
        double altitudSat = radio - RadioTerrestreKm;

        // Posición del observador en ECEF
        double latObs = observador.Latitud * ConstantesRadio.GradosARadianes;
        double lonObs = observador.Longitud * ConstantesRadio.GradosARadianes;
        double cosLatObs = Math.Cos(latObs);
        double sinLatObs = Math.Sin(latObs);
        double cosLonObs = Math.Cos(lonObs);
        double sinLonObs = Math.Sin(lonObs);

        double xObs = RadioTerrestreKm * cosLatObs * cosLonObs;
        double yObs = RadioTerrestreKm * cosLatObs * sinLonObs;
        double zObs = RadioTerrestreKm * sinLatObs;

        // Vector desde observador al satélite
        double dx = xEcef - xObs;
        double dy = yEcef - yObs;
        double dz = zEcef - zObs;

        double distancia = Math.Sqrt(dx * dx + dy * dy + dz * dz);

        // Convertir a coordenadas topocéntricas (Sur, Este, Arriba)
        double sur = sinLatObs * cosLonObs * dx + sinLatObs * sinLonObs * dy - cosLatObs * dz;
        double este = -sinLonObs * dx + cosLonObs * dy;
        double arriba = cosLatObs * cosLonObs * dx + cosLatObs * sinLonObs * dy + sinLatObs * dz;

        // Azimut y elevación
        double azimut = Math.Atan2(este, -sur) * ConstantesRadio.RadianesAGrados;
        if (azimut < 0.0)
        {
            azimut += 360.0;
        }

        double elevacion = Math.Asin(arriba / distancia) * ConstantesRadio.RadianesAGrados;

        bool visible = elevacion > 0.0;

        return new PosicionSatelite(
            latitudSat,
            longitudSat,
            altitudSat,
            azimut,
            elevacion,
            distancia,
            visible);
    }

    /// <summary>
    /// Predice los pasos de un satélite sobre una ubicación en un rango de tiempo.
    /// </summary>
    /// <param name="tle">Elementos TLE del satélite.</param>
    /// <param name="satelite">Datos del satélite amateur.</param>
    /// <param name="observador">Coordenadas del observador.</param>
    /// <param name="desde">Inicio del rango (UTC).</param>
    /// <param name="hasta">Fin del rango (UTC).</param>
    /// <param name="elevacionMinima">Elevación mínima en grados para considerar un paso válido.</param>
    /// <returns>Lista de pasos ordenados cronológicamente.</returns>
    public static IReadOnlyList<PasoSatelite> PredecirPasos(
        Tle tle,
        SateliteAmateur satelite,
        Coordenadas observador,
        DateTime desde,
        DateTime hasta,
        double elevacionMinima = 5.0)
    {
        if (tle is null)
        {
            throw new ArgumentNullException(nameof(tle));
        }

        if (satelite is null)
        {
            throw new ArgumentNullException(nameof(satelite));
        }

        List<PasoSatelite> pasos = new();

        // Paso de muestreo: 30 segundos para no perder pasos cortos
        TimeSpan paso = TimeSpan.FromSeconds(30);
        DateTime momentoActual = desde;

        bool enPaso = false;
        DateTime aosPaso = desde;
        double elevacionMaxPaso = 0.0;
        double azimutAosPaso = 0.0;
        double ultimoAzimut = 0.0;

        while (momentoActual <= hasta)
        {
            PosicionSatelite posicion = CalcularPosicion(tle, observador, momentoActual);

            if (posicion.Elevacion > 0.0)
            {
                if (!enPaso)
                {
                    // Inicio de paso
                    enPaso = true;
                    aosPaso = momentoActual;
                    elevacionMaxPaso = posicion.Elevacion;
                    azimutAosPaso = posicion.Azimut;
                }
                else
                {
                    if (posicion.Elevacion > elevacionMaxPaso)
                    {
                        elevacionMaxPaso = posicion.Elevacion;
                    }
                }

                ultimoAzimut = posicion.Azimut;
            }
            else if (enPaso)
            {
                // Fin de paso
                enPaso = false;

                if (elevacionMaxPaso >= elevacionMinima)
                {
                    PasoSatelite pasoSatelite = new(
                        satelite,
                        aosPaso,
                        momentoActual,
                        elevacionMaxPaso,
                        azimutAosPaso,
                        ultimoAzimut);

                    pasos.Add(pasoSatelite);
                }

                elevacionMaxPaso = 0.0;
            }

            momentoActual = momentoActual.Add(paso);
        }

        // Si terminamos dentro de un paso, cerrarlo
        if (enPaso && elevacionMaxPaso >= elevacionMinima)
        {
            PasoSatelite pasoSatelite = new(
                satelite,
                aosPaso,
                hasta,
                elevacionMaxPaso,
                azimutAosPaso,
                ultimoAzimut);

            pasos.Add(pasoSatelite);
        }

        return pasos.AsReadOnly();
    }

    /// <summary>
    /// Resuelve la ecuación de Kepler por iteración de Newton-Raphson.
    /// M = E - e·sin(E)
    /// </summary>
    /// <param name="anomaliaMedia">Anomalía media en radianes.</param>
    /// <param name="excentricidad">Excentricidad orbital.</param>
    /// <returns>Anomalía excéntrica en radianes.</returns>
    private static double ResolverKepler(double anomaliaMedia, double excentricidad)
    {
        double e = anomaliaMedia;

        for (int i = 0; i < 50; i++)
        {
            double delta = (e - excentricidad * Math.Sin(e) - anomaliaMedia)
                         / (1.0 - excentricidad * Math.Cos(e));
            e -= delta;

            if (Math.Abs(delta) < 1e-12)
            {
                break;
            }
        }

        return e;
    }

    /// <summary>
    /// Calcula el GMST (Greenwich Mean Sidereal Time) en radianes para un momento dado.
    /// </summary>
    /// <param name="momento">Fecha y hora UTC.</param>
    /// <returns>GMST en radianes.</returns>
    private static double CalcularGmst(DateTime momento)
    {
        // Siglos julianos desde J2000.0
        double jd = CalcularDiaJuliano(momento);
        double t = (jd - 2451545.0) / 36525.0;

        // GMST en segundos
        double gmstSegundos = 67310.54841
                            + (876600.0 * 3600.0 + 8640184.812866) * t
                            + 0.093104 * t * t
                            - 6.2e-6 * t * t * t;

        // Convertir a radianes
        double gmstRad = gmstSegundos % 86400.0 / 86400.0 * DosPi;

        if (gmstRad < 0.0)
        {
            gmstRad += DosPi;
        }

        return gmstRad;
    }

    /// <summary>
    /// Calcula el día juliano para una fecha UTC.
    /// </summary>
    /// <param name="fecha">Fecha y hora UTC.</param>
    /// <returns>Día juliano.</returns>
    private static double CalcularDiaJuliano(DateTime fecha)
    {
        int anio = fecha.Year;
        int mes = fecha.Month;
        double dia = fecha.Day
                   + fecha.Hour / 24.0
                   + fecha.Minute / 1440.0
                   + fecha.Second / 86400.0
                   + fecha.Millisecond / 86400000.0;

        if (mes <= 2)
        {
            anio -= 1;
            mes += 12;
        }

        int a = anio / 100;
        int b = 2 - a + a / 4;

        return Math.Floor(365.25 * (anio + 4716))
             + Math.Floor(30.6001 * (mes + 1))
             + dia + b - 1524.5;
    }

    /// <summary>
    /// Normaliza un ángulo al rango [0, 2π).
    /// </summary>
    /// <param name="angulo">Ángulo en radianes.</param>
    /// <returns>Ángulo normalizado.</returns>
    private static double NormalizarAngulo(double angulo)
    {
        double resultado = angulo % DosPi;

        if (resultado < 0.0)
        {
            resultado += DosPi;
        }

        return resultado;
    }

    /// <summary>
    /// Parsea un número con exponente implícito del formato TLE (ej. "12345-6" → 0.12345e-6).
    /// </summary>
    /// <param name="texto">Texto con el formato de exponente implícito del TLE.</param>
    /// <returns>Valor numérico.</returns>
    private static double ParsearExponenteImplicito(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto == "00000-0" || texto == "00000+0")
        {
            return 0.0;
        }

        // Formato: ±NNNNN±E donde el punto decimal va implícito antes de NNNNN
        // Ejemplo: "12345-6" → 0.12345 × 10^-6
        string limpio = texto.Trim();

        // Encontrar la posición del exponente (+ o - que no está al inicio)
        int posExp = -1;
        for (int i = 1; i < limpio.Length; i++)
        {
            if (limpio[i] == '+' || limpio[i] == '-')
            {
                posExp = i;
            }
        }

        if (posExp < 0)
        {
            // Sin exponente, tratar como número con punto decimal implícito
            return double.Parse("0." + limpio.TrimStart('+', '-', '0'), CultureInfo.InvariantCulture);
        }

        string mantisaTexto = limpio[..posExp];
        string exponenteTexto = limpio[posExp..];

        // Agregar punto decimal implícito
        bool negativo = mantisaTexto.StartsWith('-');
        string digitos = mantisaTexto.TrimStart('+', '-');
        string mantisaConPunto = (negativo ? "-" : "") + "0." + digitos;

        double mantisa = double.Parse(mantisaConPunto, CultureInfo.InvariantCulture);
        int exponente = int.Parse(exponenteTexto, CultureInfo.InvariantCulture);

        return mantisa * Math.Pow(10.0, exponente);
    }
}
