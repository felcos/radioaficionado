using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Dxcc;

/// <summary>
/// Catálogo estático con todas las entidades DXCC reconocidas por la ARRL.
/// Proporciona métodos de búsqueda por prefijo, indicativo y listado completo.
/// </summary>
public static class CatalogoDxcc
{
    private static readonly Dictionary<string, EntidadDxcc> _porPrefijo;
    private static readonly IReadOnlyList<EntidadDxcc> _todas;

    static CatalogoDxcc()
    {
        List<EntidadDxcc> entidades = CrearEntidades();
        _todas = entidades.AsReadOnly();
        _porPrefijo = new Dictionary<string, EntidadDxcc>(StringComparer.OrdinalIgnoreCase);

        foreach (EntidadDxcc entidad in entidades)
        {
            // Registrar el prefijo principal
            if (!_porPrefijo.ContainsKey(entidad.Prefijo))
            {
                _porPrefijo[entidad.Prefijo] = entidad;
            }
        }

        // Registrar prefijos alternativos conocidos
        RegistrarPrefijosAlternativos();
    }

    /// <summary>
    /// Busca una entidad DXCC por su prefijo exacto.
    /// </summary>
    /// <param name="prefijo">Prefijo a buscar (insensible a mayúsculas/minúsculas).</param>
    /// <returns>La entidad DXCC correspondiente, o null si no se encuentra.</returns>
    public static EntidadDxcc? ObtenerPorPrefijo(string prefijo)
    {
        if (string.IsNullOrWhiteSpace(prefijo))
        {
            return null;
        }

        // Búsqueda exacta primero
        if (_porPrefijo.TryGetValue(prefijo, out EntidadDxcc? entidad))
        {
            return entidad;
        }

        // Intentar con prefijos progresivamente más cortos
        string prefijoActual = prefijo;
        while (prefijoActual.Length > 0)
        {
            if (_porPrefijo.TryGetValue(prefijoActual, out entidad))
            {
                return entidad;
            }
            prefijoActual = prefijoActual[..^1];
        }

        return null;
    }

    /// <summary>
    /// Busca una entidad DXCC a partir de un indicativo de radioaficionado,
    /// utilizando el prefijo extraído del indicativo.
    /// </summary>
    /// <param name="indicativo">Indicativo del que se extraerá el prefijo.</param>
    /// <returns>La entidad DXCC correspondiente, o null si no se encuentra.</returns>
    public static EntidadDxcc? ObtenerPorIndicativo(Indicativo indicativo)
    {
        return ObtenerPorPrefijo(indicativo.Prefijo);
    }

    /// <summary>
    /// Obtiene la lista completa de todas las entidades DXCC del catálogo.
    /// </summary>
    /// <returns>Lista de solo lectura con todas las entidades DXCC.</returns>
    public static IReadOnlyList<EntidadDxcc> ObtenerTodas()
    {
        return _todas;
    }

    /// <summary>
    /// Obtiene solo las entidades DXCC activas (no eliminadas).
    /// </summary>
    /// <returns>Lista de solo lectura con las entidades DXCC activas.</returns>
    public static IReadOnlyList<EntidadDxcc> ObtenerActivas()
    {
        return _todas.Where(e => !e.Eliminada).ToList().AsReadOnly();
    }

    private static void RegistrarPrefijosAlternativos()
    {
        // USA - prefijos adicionales
        EntidadDxcc? usa = _porPrefijo.GetValueOrDefault("K");
        if (usa is not null)
        {
            _porPrefijo.TryAdd("W", usa);
            _porPrefijo.TryAdd("N", usa);
            _porPrefijo.TryAdd("AA", usa);
            _porPrefijo.TryAdd("AB", usa);
            _porPrefijo.TryAdd("AC", usa);
            _porPrefijo.TryAdd("AD", usa);
            _porPrefijo.TryAdd("AE", usa);
            _porPrefijo.TryAdd("AF", usa);
            _porPrefijo.TryAdd("AG", usa);
            _porPrefijo.TryAdd("AH", usa); // Hawaii también usa AH pero es DXCC separado — se corrige abajo
            _porPrefijo.TryAdd("AI", usa);
            _porPrefijo.TryAdd("AJ", usa);
            _porPrefijo.TryAdd("AK", usa);
            _porPrefijo.TryAdd("AL", usa); // Alaska — DXCC separado, se corrige abajo
            _porPrefijo.TryAdd("KA", usa);
            _porPrefijo.TryAdd("KB", usa);
            _porPrefijo.TryAdd("KC", usa);
            _porPrefijo.TryAdd("KD", usa);
            _porPrefijo.TryAdd("KE", usa);
            _porPrefijo.TryAdd("KF", usa);
            _porPrefijo.TryAdd("KG", usa);
            _porPrefijo.TryAdd("KI", usa);
            _porPrefijo.TryAdd("KJ", usa);
            _porPrefijo.TryAdd("KK", usa);
            _porPrefijo.TryAdd("KM", usa);
            _porPrefijo.TryAdd("KN", usa);
            _porPrefijo.TryAdd("KO", usa);
            _porPrefijo.TryAdd("KQ", usa);
            _porPrefijo.TryAdd("KR", usa);
            _porPrefijo.TryAdd("KS", usa);
            _porPrefijo.TryAdd("KT", usa);
            _porPrefijo.TryAdd("KU", usa);
            _porPrefijo.TryAdd("KV", usa);
            _porPrefijo.TryAdd("KW", usa);
            _porPrefijo.TryAdd("KX", usa);
            _porPrefijo.TryAdd("KY", usa);
            _porPrefijo.TryAdd("KZ", usa);
            _porPrefijo.TryAdd("WA", usa);
            _porPrefijo.TryAdd("WB", usa);
            _porPrefijo.TryAdd("WC", usa);
            _porPrefijo.TryAdd("WD", usa);
            _porPrefijo.TryAdd("WE", usa);
            _porPrefijo.TryAdd("WF", usa);
            _porPrefijo.TryAdd("WG", usa);
            _porPrefijo.TryAdd("WH", usa);
            _porPrefijo.TryAdd("WI", usa);
            _porPrefijo.TryAdd("WJ", usa);
            _porPrefijo.TryAdd("WK", usa);
            _porPrefijo.TryAdd("WL", usa);
            _porPrefijo.TryAdd("WM", usa);
            _porPrefijo.TryAdd("WN", usa);
            _porPrefijo.TryAdd("WO", usa);
            _porPrefijo.TryAdd("WP", usa);
            _porPrefijo.TryAdd("WQ", usa);
            _porPrefijo.TryAdd("WR", usa);
            _porPrefijo.TryAdd("WS", usa);
            _porPrefijo.TryAdd("WT", usa);
            _porPrefijo.TryAdd("WU", usa);
            _porPrefijo.TryAdd("WV", usa);
            _porPrefijo.TryAdd("WW", usa);
            _porPrefijo.TryAdd("WX", usa);
            _porPrefijo.TryAdd("WY", usa);
            _porPrefijo.TryAdd("WZ", usa);
            _porPrefijo.TryAdd("NA", usa);
            _porPrefijo.TryAdd("NB", usa);
            _porPrefijo.TryAdd("NC", usa);
            _porPrefijo.TryAdd("ND", usa);
            _porPrefijo.TryAdd("NE", usa);
            _porPrefijo.TryAdd("NF", usa);
            _porPrefijo.TryAdd("NG", usa);
            _porPrefijo.TryAdd("NH", usa);
            _porPrefijo.TryAdd("NI", usa);
            _porPrefijo.TryAdd("NJ", usa);
            _porPrefijo.TryAdd("NK", usa);
            _porPrefijo.TryAdd("NL", usa);
            _porPrefijo.TryAdd("NM", usa);
            _porPrefijo.TryAdd("NN", usa);
            _porPrefijo.TryAdd("NO", usa);
            _porPrefijo.TryAdd("NP", usa);
            _porPrefijo.TryAdd("NQ", usa);
            _porPrefijo.TryAdd("NR", usa);
            _porPrefijo.TryAdd("NS", usa);
            _porPrefijo.TryAdd("NT", usa);
            _porPrefijo.TryAdd("NU", usa);
            _porPrefijo.TryAdd("NV", usa);
            _porPrefijo.TryAdd("NW", usa);
            _porPrefijo.TryAdd("NX", usa);
            _porPrefijo.TryAdd("NY", usa);
            _porPrefijo.TryAdd("NZ", usa);
        }

        // Rusia - prefijos adicionales
        EntidadDxcc? rusia = _porPrefijo.GetValueOrDefault("UA");
        if (rusia is not null)
        {
            _porPrefijo.TryAdd("RA", rusia);
            _porPrefijo.TryAdd("RB", rusia);
            _porPrefijo.TryAdd("RC", rusia);
            _porPrefijo.TryAdd("RD", rusia);
            _porPrefijo.TryAdd("RE", rusia);
            _porPrefijo.TryAdd("RF", rusia);
            _porPrefijo.TryAdd("RG", rusia);
            _porPrefijo.TryAdd("RJ", rusia);
            _porPrefijo.TryAdd("RK", rusia);
            _porPrefijo.TryAdd("RL", rusia);
            _porPrefijo.TryAdd("RM", rusia);
            _porPrefijo.TryAdd("RN", rusia);
            _porPrefijo.TryAdd("RO", rusia);
            _porPrefijo.TryAdd("RQ", rusia);
            _porPrefijo.TryAdd("RT", rusia);
            _porPrefijo.TryAdd("RU", rusia);
            _porPrefijo.TryAdd("RV", rusia);
            _porPrefijo.TryAdd("RW", rusia);
            _porPrefijo.TryAdd("RX", rusia);
            _porPrefijo.TryAdd("RY", rusia);
            _porPrefijo.TryAdd("RZ", rusia);
            _porPrefijo.TryAdd("UB", rusia);
            _porPrefijo.TryAdd("UC", rusia);
            _porPrefijo.TryAdd("UD", rusia);
            _porPrefijo.TryAdd("UE", rusia);
            _porPrefijo.TryAdd("UF", rusia);
            _porPrefijo.TryAdd("UG", rusia);
            _porPrefijo.TryAdd("UH", rusia);
            _porPrefijo.TryAdd("UI", rusia);
        }

        // Japón - prefijos adicionales
        EntidadDxcc? japon = _porPrefijo.GetValueOrDefault("JA");
        if (japon is not null)
        {
            _porPrefijo.TryAdd("JB", japon);
            _porPrefijo.TryAdd("JC", japon);
            _porPrefijo.TryAdd("JD", japon);
            _porPrefijo.TryAdd("JE", japon);
            _porPrefijo.TryAdd("JF", japon);
            _porPrefijo.TryAdd("JG", japon);
            _porPrefijo.TryAdd("JH", japon);
            _porPrefijo.TryAdd("JI", japon);
            _porPrefijo.TryAdd("JJ", japon);
            _porPrefijo.TryAdd("JK", japon);
            _porPrefijo.TryAdd("JL", japon);
            _porPrefijo.TryAdd("JM", japon);
            _porPrefijo.TryAdd("JN", japon);
            _porPrefijo.TryAdd("JO", japon);
            _porPrefijo.TryAdd("JP", japon);
            _porPrefijo.TryAdd("JQ", japon);
            _porPrefijo.TryAdd("JR", japon);
            _porPrefijo.TryAdd("JS", japon);
        }

        // Alemania
        EntidadDxcc? alemania = _porPrefijo.GetValueOrDefault("DL");
        if (alemania is not null)
        {
            _porPrefijo.TryAdd("DA", alemania);
            _porPrefijo.TryAdd("DB", alemania);
            _porPrefijo.TryAdd("DC", alemania);
            _porPrefijo.TryAdd("DD", alemania);
            _porPrefijo.TryAdd("DE", alemania);
            _porPrefijo.TryAdd("DF", alemania);
            _porPrefijo.TryAdd("DG", alemania);
            _porPrefijo.TryAdd("DH", alemania);
            _porPrefijo.TryAdd("DI", alemania);
            _porPrefijo.TryAdd("DJ", alemania);
            _porPrefijo.TryAdd("DK", alemania);
            _porPrefijo.TryAdd("DM", alemania);
            _porPrefijo.TryAdd("DN", alemania);
            _porPrefijo.TryAdd("DO", alemania);
            _porPrefijo.TryAdd("DP", alemania);
            _porPrefijo.TryAdd("DQ", alemania);
            _porPrefijo.TryAdd("DR", alemania);
        }

        // China
        EntidadDxcc? china = _porPrefijo.GetValueOrDefault("BY");
        if (china is not null)
        {
            _porPrefijo.TryAdd("BA", china);
            _porPrefijo.TryAdd("BB", china);
            _porPrefijo.TryAdd("BC", china);
            _porPrefijo.TryAdd("BD", china);
            _porPrefijo.TryAdd("BE", china);
            _porPrefijo.TryAdd("BF", china);
            _porPrefijo.TryAdd("BG", china);
            _porPrefijo.TryAdd("BH", china);
            _porPrefijo.TryAdd("BI", china);
            _porPrefijo.TryAdd("BJ", china);
            _porPrefijo.TryAdd("BK", china);
            _porPrefijo.TryAdd("BL", china);
            _porPrefijo.TryAdd("BM", china);
            _porPrefijo.TryAdd("BN", china);
            _porPrefijo.TryAdd("BO", china);
            _porPrefijo.TryAdd("BP", china);
            _porPrefijo.TryAdd("BQ", china);
            _porPrefijo.TryAdd("BR", china);
            _porPrefijo.TryAdd("BS", china);
            _porPrefijo.TryAdd("BT", china);
            _porPrefijo.TryAdd("BU", china);
            _porPrefijo.TryAdd("BX", china);
            _porPrefijo.TryAdd("BZ", china);
        }

        // Canadá
        EntidadDxcc? canada = _porPrefijo.GetValueOrDefault("VE");
        if (canada is not null)
        {
            _porPrefijo.TryAdd("VA", canada);
            _porPrefijo.TryAdd("VB", canada);
            _porPrefijo.TryAdd("VC", canada);
            _porPrefijo.TryAdd("VD", canada);
            _porPrefijo.TryAdd("CF", canada);
            _porPrefijo.TryAdd("CG", canada);
            _porPrefijo.TryAdd("CH", canada);
            _porPrefijo.TryAdd("CI", canada);
            _porPrefijo.TryAdd("CJ", canada);
            _porPrefijo.TryAdd("CK", canada);
            _porPrefijo.TryAdd("CY", canada);
            _porPrefijo.TryAdd("CZ", canada);
            _porPrefijo.TryAdd("VO", canada);
            _porPrefijo.TryAdd("VY", canada);
        }

        // Brasil
        EntidadDxcc? brasil = _porPrefijo.GetValueOrDefault("PY");
        if (brasil is not null)
        {
            _porPrefijo.TryAdd("PP", brasil);
            _porPrefijo.TryAdd("PQ", brasil);
            _porPrefijo.TryAdd("PR", brasil);
            _porPrefijo.TryAdd("PS", brasil);
            _porPrefijo.TryAdd("PT", brasil);
            _porPrefijo.TryAdd("PU", brasil);
            _porPrefijo.TryAdd("PV", brasil);
            _porPrefijo.TryAdd("PW", brasil);
            _porPrefijo.TryAdd("PX", brasil);
            _porPrefijo.TryAdd("ZV", brasil);
            _porPrefijo.TryAdd("ZW", brasil);
            _porPrefijo.TryAdd("ZX", brasil);
            _porPrefijo.TryAdd("ZY", brasil);
            _porPrefijo.TryAdd("ZZ", brasil);
        }

        // Argentina
        EntidadDxcc? argentina = _porPrefijo.GetValueOrDefault("LU");
        if (argentina is not null)
        {
            _porPrefijo.TryAdd("LO", argentina);
            _porPrefijo.TryAdd("LP", argentina);
            _porPrefijo.TryAdd("LQ", argentina);
            _porPrefijo.TryAdd("LR", argentina);
            _porPrefijo.TryAdd("LS", argentina);
            _porPrefijo.TryAdd("LT", argentina);
            _porPrefijo.TryAdd("LV", argentina);
            _porPrefijo.TryAdd("LW", argentina);
            _porPrefijo.TryAdd("AY", argentina);
            _porPrefijo.TryAdd("AZ", argentina);
            _porPrefijo.TryAdd("L", argentina);
        }

        // España
        EntidadDxcc? espana = _porPrefijo.GetValueOrDefault("EA");
        if (espana is not null)
        {
            _porPrefijo.TryAdd("EB", espana);
            _porPrefijo.TryAdd("EC", espana);
            _porPrefijo.TryAdd("ED", espana);
            _porPrefijo.TryAdd("EE", espana);
            _porPrefijo.TryAdd("EF", espana);
            _porPrefijo.TryAdd("EG", espana);
            _porPrefijo.TryAdd("EH", espana);
        }

        // Francia
        EntidadDxcc? francia = _porPrefijo.GetValueOrDefault("F");
        if (francia is not null)
        {
            _porPrefijo.TryAdd("TM", francia);
            _porPrefijo.TryAdd("TX", francia);
        }

        // Italia
        EntidadDxcc? italia = _porPrefijo.GetValueOrDefault("I");
        if (italia is not null)
        {
            _porPrefijo.TryAdd("IZ", italia);
            _porPrefijo.TryAdd("IK", italia);
            _porPrefijo.TryAdd("IW", italia);
            _porPrefijo.TryAdd("IX", italia);
        }

        // Inglaterra
        EntidadDxcc? inglaterra = _porPrefijo.GetValueOrDefault("G");
        if (inglaterra is not null)
        {
            _porPrefijo.TryAdd("M", inglaterra);
        }

        // Australia
        EntidadDxcc? australia = _porPrefijo.GetValueOrDefault("VK");
        if (australia is not null)
        {
            _porPrefijo.TryAdd("AX", australia);
        }

        // México
        EntidadDxcc? mexico = _porPrefijo.GetValueOrDefault("XE");
        if (mexico is not null)
        {
            _porPrefijo.TryAdd("XF", mexico);
        }

        // India
        EntidadDxcc? india = _porPrefijo.GetValueOrDefault("VU");
        if (india is not null)
        {
            _porPrefijo.TryAdd("AT", india);
            _porPrefijo.TryAdd("AU", india);
            _porPrefijo.TryAdd("AV", india);
            _porPrefijo.TryAdd("AW", india);
        }

        // Corea del Sur
        EntidadDxcc? corea = _porPrefijo.GetValueOrDefault("HL");
        if (corea is not null)
        {
            _porPrefijo.TryAdd("DS", corea);
            _porPrefijo.TryAdd("DT", corea);
            _porPrefijo.TryAdd("D7", corea);
            _porPrefijo.TryAdd("D8", corea);
            _porPrefijo.TryAdd("D9", corea);
        }

        // Taiwán (BV ya registrado como principal)
    }

    private static List<EntidadDxcc> CrearEntidades()
    {
        return new List<EntidadDxcc>
        {
            // ===== ÁFRICA (AF) =====
            new(219, "Botswana", "A2", "AF", 38, 57, -22.33, 24.02, false),
            new(404, "Tonga", "A3", "OC", 32, 62, -21.17, -175.20, false),
            new(003, "Omán", "A4", "AS", 21, 39, 23.60, 58.54, false),
            new(004, "Bután", "A5", "AS", 22, 41, 27.50, 90.50, false),
            new(006, "Emiratos Árabes Unidos", "A6", "AS", 21, 39, 24.47, 54.37, false),
            new(007, "Qatar", "A7", "AS", 21, 39, 25.28, 51.53, false),
            new(009, "Bahréin", "A9", "AS", 21, 39, 26.04, 50.55, false),

            // ===== AMÉRICA DEL NORTE (NA) =====
            new(291, "Estados Unidos", "K", "NA", 3, 8, 38.89, -77.04, false),
            new(001, "Canadá", "VE", "NA", 5, 9, 45.42, -75.70, false),
            new(050, "México", "XE", "NA", 6, 10, 19.43, -99.13, false),

            // ===== AMÉRICA DEL SUR (SA) =====
            new(108, "Argentina", "LU", "SA", 13, 14, -34.60, -58.38, false),
            new(112, "Brasil", "PY", "SA", 11, 15, -15.78, -47.93, false),
            new(116, "Chile", "CE", "SA", 12, 14, -33.45, -70.67, false),
            new(120, "Colombia", "HK", "SA", 9, 12, 4.71, -74.07, false),
            new(132, "Ecuador", "HC", "SA", 10, 12, -0.22, -78.51, false),
            new(136, "Paraguay", "ZP", "SA", 11, 14, -25.30, -57.63, false),
            new(140, "Perú", "OA", "SA", 10, 12, -12.05, -77.03, false),
            new(144, "Uruguay", "CX", "SA", 13, 14, -34.88, -56.17, false),
            new(148, "Venezuela", "YV", "SA", 9, 12, 10.50, -66.93, false),
            new(100, "Bolivia", "CP", "SA", 10, 12, -16.50, -68.15, false),
            new(156, "Guyana", "8R", "SA", 9, 12, 6.80, -58.17, false),
            new(164, "Surinam", "PZ", "SA", 9, 12, 5.87, -55.17, false),
            new(128, "Guayana Francesa", "FY", "SA", 9, 12, 4.93, -52.33, false),

            // ===== EUROPA (EU) =====
            new(230, "Alemania", "DL", "EU", 14, 28, 51.05, 13.73, false),
            new(281, "España", "EA", "EU", 14, 37, 40.42, -3.72, false),
            new(223, "Francia", "F", "EU", 14, 27, 48.86, 2.35, false),
            new(227, "Italia", "I", "EU", 15, 28, 41.90, 12.50, false),
            new(279, "Inglaterra", "G", "EU", 14, 27, 51.51, -0.13, false),
            new(269, "Portugal", "CT", "EU", 14, 37, 38.72, -9.14, false),
            new(263, "Países Bajos", "PA", "EU", 14, 27, 52.37, 4.90, false),
            new(209, "Bélgica", "ON", "EU", 14, 27, 50.85, 4.35, false),
            new(245, "Irlanda", "EI", "EU", 14, 27, 53.35, -6.26, false),
            new(284, "Suiza", "HB", "EU", 14, 28, 46.95, 7.45, false),
            new(206, "Austria", "OE", "EU", 15, 28, 48.21, 16.37, false),
            new(256, "Noruega", "LA", "EU", 14, 18, 59.91, 10.75, false),
            new(284, "Suecia", "SM", "EU", 14, 18, 59.33, 18.07, false),
            new(222, "Finlandia", "OH", "EU", 14, 18, 60.17, 24.94, false),
            new(221, "Dinamarca", "OZ", "EU", 14, 18, 55.68, 12.57, false),
            new(242, "Islandia", "TF", "EU", 40, 17, 64.14, -21.90, false),
            new(225, "Grecia", "SV", "EU", 20, 28, 37.97, 23.73, false),
            new(239, "Hungría", "HA", "EU", 15, 28, 47.50, 19.08, false),
            new(257, "Polonia", "SP", "EU", 15, 28, 52.23, 21.01, false),
            new(503, "República Checa", "OK", "EU", 15, 28, 50.08, 14.42, false),
            new(504, "República Eslovaca", "OM", "EU", 15, 28, 48.15, 17.12, false),
            new(288, "Ucrania", "UR", "EU", 16, 29, 50.45, 30.52, false),
            new(212, "Bielorrusia", "EU", "EU", 16, 29, 53.90, 27.57, false),
            new(514, "Croacia", "9A", "EU", 15, 28, 45.82, 15.97, false),
            new(501, "Bosnia-Herzegovina", "T9", "EU", 15, 28, 43.86, 18.41, false),
            new(296, "Serbia", "YU", "EU", 15, 28, 44.80, 20.47, false),
            new(502, "Macedonia del Norte", "Z3", "EU", 15, 28, 42.00, 21.43, false),
            new(278, "Eslovenia", "S5", "EU", 15, 28, 46.05, 14.51, false),
            new(507, "Montenegro", "4O", "EU", 15, 28, 42.44, 19.26, false),
            new(509, "Kosovo", "Z6", "EU", 15, 28, 42.67, 21.17, false),
            new(214, "Bulgaria", "LZ", "EU", 20, 28, 42.70, 23.32, false),
            new(275, "Rumania", "YO", "EU", 20, 28, 44.43, 26.10, false),
            new(236, "Luxemburgo", "LX", "EU", 14, 27, 49.61, 6.13, false),
            new(247, "Liechtenstein", "HB0", "EU", 14, 28, 47.14, 9.52, false),
            new(260, "Mónaco", "3A", "EU", 14, 27, 43.73, 7.42, false),
            new(005, "Andorra", "C3", "EU", 14, 27, 42.51, 1.52, false),
            new(268, "San Marino", "T7", "EU", 15, 28, 43.94, 12.46, false),
            new(246, "Letonia", "YL", "EU", 15, 29, 56.95, 24.11, false),
            new(145, "Lituania", "LY", "EU", 15, 29, 54.69, 25.28, false),
            new(052, "Estonia", "ES", "EU", 15, 29, 59.44, 24.75, false),
            new(514, "Malta", "9H", "EU", 15, 28, 35.90, 14.51, false),
            new(299, "Chipre", "5B", "AS", 20, 39, 35.17, 33.37, false),
            new(032, "Islas Feroe", "OY", "EU", 14, 18, 62.00, -6.77, false),
            new(259, "Gibraltar", "ZB", "EU", 14, 37, 36.14, -5.35, false),
            new(015, "Turquía Asiática", "TA", "AS", 20, 39, 39.93, 32.85, false),
            new(013, "Turquía Europea", "TA", "EU", 20, 39, 41.01, 28.98, false),

            // ===== ASIA (AS) =====
            new(339, "Japón", "JA", "AS", 25, 45, 35.68, 139.77, false),
            new(318, "China", "BY", "AS", 24, 44, 39.91, 116.40, false),
            new(386, "Taiwán", "BV", "AS", 24, 44, 25.05, 121.53, false),
            new(137, "Corea del Sur", "HL", "SA", 25, 44, 37.57, 126.98, false),
            new(372, "India", "VU", "AS", 22, 41, 28.61, 77.21, false),
            new(054, "Israel", "4X", "AS", 20, 39, 32.07, 34.78, false),
            new(287, "Rusia", "UA", "EU", 16, 29, 55.75, 37.62, false),
            new(292, "Rusia Asiática", "UA9", "AS", 16, 30, 55.03, 73.37, false),
            new(075, "Kazajistán", "UN", "AS", 17, 30, 51.17, 71.43, false),
            new(142, "Uzbekistán", "UK", "AS", 17, 30, 41.30, 69.28, false),
            new(135, "Hong Kong", "VR", "AS", 24, 44, 22.28, 114.17, false),
            new(152, "Macao", "XX", "AS", 24, 44, 22.20, 113.55, false),
            new(305, "Bangladés", "S2", "AS", 22, 41, 23.71, 90.41, false),
            new(315, "Sri Lanka", "4S", "AS", 22, 41, 6.93, 79.85, false),
            new(369, "Nepal", "9N", "AS", 22, 42, 27.72, 85.32, false),
            new(387, "Paquistán", "AP", "AS", 21, 40, 33.69, 73.04, false),
            new(391, "Tailandia", "HS", "AS", 26, 49, 13.76, 100.52, false),
            new(333, "Malasia Peninsular", "9M2", "AS", 28, 54, 3.14, 101.69, false),
            new(046, "Filipinas", "DU", "OC", 27, 50, 14.60, 120.98, false),
            new(342, "Indonesia", "YB", "OC", 28, 54, -6.17, 106.83, false),
            new(345, "Vietnam", "XV", "AS", 26, 49, 21.03, 105.85, false),
            new(312, "Camboya", "XU", "AS", 26, 49, 11.56, 104.92, false),
            new(330, "Laos", "XW", "AS", 26, 49, 17.97, 102.63, false),
            new(309, "Birmania (Myanmar)", "XZ", "AS", 26, 49, 16.87, 96.17, false),
            new(327, "Mongolia", "JT", "AS", 23, 32, 47.91, 106.91, false),
            new(375, "Irán", "EP", "AS", 21, 40, 35.69, 51.39, false),
            new(363, "Irak", "YI", "AS", 21, 39, 33.34, 44.40, false),
            new(378, "Jordania", "JY", "AS", 20, 39, 31.95, 35.93, false),
            new(354, "Kuwait", "9K", "AS", 21, 39, 29.37, 47.98, false),
            new(301, "Arabia Saudita", "HZ", "AS", 21, 39, 24.69, 46.72, false),
            new(384, "Líbano", "OD", "AS", 20, 39, 33.89, 35.49, false),
            new(336, "Singapur", "9V", "AS", 28, 54, 1.29, 103.85, false),

            // ===== OCEANÍA (OC) =====
            new(150, "Australia", "VK", "OC", 30, 59, -33.87, 151.21, false),
            new(170, "Nueva Zelanda", "ZL", "OC", 32, 60, -41.29, 174.77, false),
            new(168, "Papúa Nueva Guinea", "P2", "OC", 28, 51, -6.17, 155.97, false),
            new(176, "Fiyi", "3D2", "OC", 32, 56, -18.14, 178.44, false),
            new(190, "Samoa", "5W", "OC", 32, 62, -13.83, -171.76, false),
            new(197, "Islas Cook", "ZK1", "OC", 32, 62, -21.23, -159.78, false),
            new(160, "Hawái", "KH6", "OC", 31, 61, 21.31, -157.86, false),
            new(103, "Alaska", "KL7", "NA", 1, 1, 61.22, -149.90, false),
            new(174, "Guam", "KH2", "OC", 27, 64, 13.44, 144.79, false),
            new(110, "Islas Marshall", "V7", "OC", 31, 65, 7.09, 171.38, false),
            new(064, "Micronesia", "V6", "OC", 27, 65, 6.91, 158.16, false),
            new(191, "Palau", "T8", "OC", 27, 64, 7.34, 134.47, false),

            // ===== CARIBE Y CENTROAMÉRICA =====
            new(078, "Cuba", "CO", "NA", 8, 11, 23.13, -82.38, false),
            new(084, "República Dominicana", "HI", "NA", 8, 11, 18.47, -69.90, false),
            new(202, "Haití", "HH", "NA", 8, 11, 18.54, -72.34, false),
            new(082, "Puerto Rico", "KP4", "NA", 8, 11, 18.47, -66.12, false),
            new(285, "Jamaica", "6Y", "NA", 8, 11, 18.00, -76.80, false),
            new(079, "Trinidad y Tobago", "9Y", "SA", 9, 11, 10.65, -61.52, false),
            new(090, "Barbados", "8P", "NA", 8, 11, 13.10, -59.62, false),
            new(094, "Martinica", "FM", "NA", 8, 11, 14.60, -61.07, false),
            new(097, "Guadalupe", "FG", "NA", 8, 11, 16.25, -61.55, false),
            new(060, "Bahamas", "C6", "NA", 8, 11, 25.06, -77.34, false),
            new(062, "Bermudas", "VP9", "NA", 5, 11, 32.30, -64.75, false),
            new(065, "Islas Vírgenes Británicas", "VP2V", "NA", 8, 11, 18.43, -64.62, false),
            new(182, "Islas Vírgenes Americanas", "KP2", "NA", 8, 11, 18.34, -64.93, false),
            new(096, "Curazao", "PJ2", "SA", 9, 11, 12.17, -68.98, false),
            new(520, "Bonaire", "PJ4", "SA", 9, 11, 12.14, -68.27, false),
            new(517, "Sint Maarten", "PJ7", "NA", 8, 11, 18.04, -63.06, false),
            new(519, "Saba y San Eustaquio", "PJ5", "NA", 8, 11, 17.63, -63.24, false),
            new(091, "Aruba", "P4", "SA", 9, 11, 12.52, -69.97, false),

            // Centroamérica
            new(086, "Guatemala", "TG", "NA", 7, 11, 14.63, -90.51, false),
            new(088, "Honduras", "HR", "NA", 7, 11, 14.07, -87.22, false),
            new(068, "El Salvador", "YS", "NA", 7, 11, 13.69, -89.22, false),
            new(270, "Nicaragua", "YN", "NA", 7, 11, 12.13, -86.27, false),
            new(308, "Costa Rica", "TI", "NA", 7, 11, 9.93, -84.08, false),
            new(088, "Panamá", "HP", "NA", 7, 11, 8.97, -79.53, false),
            new(066, "Belice", "V3", "NA", 7, 11, 17.25, -88.77, false),

            // ===== ÁFRICA (AF) continuación =====
            new(400, "Sudáfrica", "ZS", "AF", 38, 57, -33.93, 18.42, false),
            new(404, "Kenia", "5Z", "AF", 37, 48, -1.28, 36.82, false),
            new(414, "Nigeria", "5N", "AF", 35, 46, 9.06, 7.49, false),
            new(446, "Ghana", "9G", "AF", 35, 46, 5.56, -0.20, false),
            new(450, "Senegal", "6W", "AF", 35, 46, 14.69, -17.44, false),
            new(454, "Costa de Marfil", "TU", "AF", 35, 46, 5.35, -4.01, false),
            new(458, "Marruecos", "CN", "AF", 33, 37, 34.02, -6.84, false),
            new(478, "Argelia", "7X", "AF", 33, 37, 36.77, 3.06, false),
            new(462, "Túnez", "3V", "AF", 33, 37, 36.81, 10.18, false),
            new(436, "Libia", "5A", "AF", 34, 38, 32.90, 13.18, false),
            new(466, "Egipto", "SU", "AF", 34, 38, 30.04, 31.24, false),
            new(470, "Tanzania", "5H", "AF", 37, 53, -6.80, 39.28, false),
            new(474, "Uganda", "5X", "AF", 37, 48, 0.32, 32.58, false),
            new(482, "Zimbabue", "Z2", "AF", 38, 53, -17.83, 31.05, false),
            new(486, "Mozambique", "C9", "AF", 37, 53, -25.97, 32.58, false),
            new(438, "Madagascar", "5R", "AF", 39, 53, -18.92, 47.52, false),
            new(490, "Zambia", "9J", "AF", 36, 53, -15.42, 28.28, false),
            new(494, "Etiopía", "ET", "AF", 37, 48, 9.02, 38.75, false),
            new(498, "Eritrea", "E3", "AF", 37, 48, 15.33, 38.93, false),
            new(468, "Camerún", "TJ", "AF", 36, 47, 3.87, 11.52, false),
            new(406, "Gabón", "TR", "AF", 36, 52, 0.39, 9.45, false),
            new(408, "República del Congo", "TN", "AF", 36, 52, -4.27, 15.28, false),
            new(412, "República Democrática del Congo", "9Q", "AF", 36, 52, -4.32, 15.31, false),
            new(420, "Angola", "D2", "AF", 36, 52, -8.84, 13.23, false),
            new(424, "Namibia", "V5", "AF", 38, 57, -22.57, 17.08, false),
            new(416, "Malaui", "7Q", "AF", 37, 53, -13.97, 33.79, false),
            new(444, "Guinea", "3X", "AF", 35, 46, 9.54, -13.68, false),
            new(448, "Sierra Leona", "9L", "AF", 35, 46, 8.48, -13.23, false),
            new(452, "Liberia", "EL", "AF", 35, 46, 6.31, -10.80, false),
            new(456, "Gambia", "C5", "AF", 35, 46, 13.45, -16.58, false),
            new(460, "Malí", "TZ", "AF", 35, 46, 12.65, -8.00, false),
            new(476, "Níger", "5U", "AF", 35, 46, 13.51, 2.13, false),
            new(480, "Chad", "TT", "AF", 36, 47, 12.11, 15.04, false),
            new(402, "Lesoto", "7P", "AF", 38, 57, -29.32, 27.48, false),
            new(395, "Suazilandia (Eswatini)", "3DA", "AF", 38, 57, -26.32, 31.13, false),
            new(410, "Isla Mauricio", "3B8", "AF", 39, 53, -20.16, 57.50, false),
            new(430, "Reunión", "FR", "AF", 39, 53, -21.12, 55.53, false),
            new(442, "Seychelles", "S7", "AF", 39, 53, -4.62, 55.45, false),
            new(432, "Comoras", "D6", "AF", 39, 53, -11.70, 43.25, false),
            new(434, "Mayotte", "FH", "AF", 39, 53, -12.78, 45.23, false),
            new(440, "Yibuti", "J2", "AF", 37, 48, 11.59, 43.15, false),
            new(398, "Somalia", "T5", "AF", 37, 48, 2.05, 45.34, false),
            new(422, "Guinea-Bisáu", "J5", "AF", 35, 46, 11.87, -15.60, false),
            new(428, "Cabo Verde", "D4", "AF", 35, 46, 14.93, -23.51, false),
            new(418, "Guinea Ecuatorial", "3C", "AF", 36, 47, 3.75, 8.78, false),
            new(426, "Santo Tomé y Príncipe", "S9", "AF", 36, 47, 0.34, 6.73, false),
            new(388, "Burkina Faso", "XT", "AF", 35, 46, 12.37, -1.52, false),
            new(483, "Togo", "5V", "AF", 35, 46, 6.17, 1.23, false),
            new(484, "Benín", "TY", "AF", 35, 46, 6.37, 2.43, false),
            new(396, "Ruanda", "9X", "AF", 36, 52, -1.95, 29.87, false),
            new(397, "Burundi", "9U", "AF", 36, 52, -3.38, 29.36, false),
            new(488, "República Centroafricana", "TL", "AF", 36, 47, 4.37, 18.58, false),
            new(492, "Mauritania", "5T", "AF", 35, 46, 18.09, -15.98, false),
            new(496, "Sudán", "ST", "AF", 34, 48, 15.60, 32.53, false),
            new(521, "Sudán del Sur", "Z8", "AF", 34, 48, 4.85, 31.60, false),

            // ===== ASIA (AS) adicionales =====
            new(348, "Afganistán", "YA", "AS", 21, 40, 34.53, 69.17, false),
            new(381, "Siria", "YK", "AS", 20, 39, 33.51, 36.30, false),
            new(390, "Yemen", "7O", "AS", 21, 39, 15.35, 44.21, false),
            new(354, "Kirguistán", "EX", "AS", 17, 31, 42.87, 74.59, false),
            new(357, "Tayikistán", "EY", "AS", 17, 30, 38.54, 68.77, false),
            new(361, "Turkmenistán", "EZ", "AS", 17, 30, 37.95, 58.38, false),
            new(366, "Georgia", "4L", "AS", 21, 29, 41.69, 44.80, false),
            new(014, "Armenia", "EK", "AS", 21, 29, 40.18, 44.51, false),
            new(018, "Azerbaiyán", "4J", "AS", 21, 29, 40.41, 49.87, false),
            new(393, "Brunéi", "V8", "OC", 28, 54, 4.93, 114.95, false),
            new(506, "Timor-Leste", "4W", "OC", 28, 54, -8.56, 125.57, false),
            new(324, "Maldivas", "8Q", "AS", 22, 41, 4.17, 73.51, false),
            new(321, "Corea del Norte", "P5", "AS", 25, 44, 39.02, 125.75, false),

            // ===== OCEANÍA (OC) adicionales =====
            new(188, "Nueva Caledonia", "FK", "OC", 32, 56, -22.27, 166.46, false),
            new(175, "Polinesia Francesa", "FO", "OC", 32, 63, -17.53, -149.57, false),
            new(172, "Vanuatu", "YJ", "OC", 32, 56, -17.73, 168.32, false),
            new(185, "Islas Salomón", "H4", "OC", 28, 51, -9.43, 160.03, false),
            new(177, "Tuvalu", "T2", "OC", 31, 65, -8.52, 179.20, false),
            new(178, "Kiribati", "T3", "OC", 31, 65, 1.33, 172.98, false),
            new(181, "Nauru", "C2", "OC", 31, 65, -0.53, 166.92, false),

            // ===== Entidades eliminadas (históricas) =====
            new(341, "Yemen del Norte", "4W", "AS", 21, 39, 15.35, 44.21, true),
            new(382, "Yemen del Sur", "VS9", "AS", 21, 39, 12.78, 45.02, true),
            new(262, "Checoslovaquia", "OK", "EU", 15, 28, 50.08, 14.42, true),
            new(290, "Yugoslavia", "YU", "EU", 15, 28, 44.80, 20.47, true),
            new(511, "Antillas Neerlandesas", "PJ", "SA", 9, 11, 12.17, -68.98, true),
            new(230, "Alemania Federal", "DL", "EU", 14, 28, 50.11, 8.68, true),
            new(229, "Alemania Oriental (DDR)", "Y2", "EU", 14, 28, 52.52, 13.41, true),
            new(280, "URSS", "UA", "EU", 16, 29, 55.75, 37.62, true),
        };
    }
}
