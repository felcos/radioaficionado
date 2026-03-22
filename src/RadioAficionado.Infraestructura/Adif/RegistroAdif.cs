namespace RadioAficionado.Infraestructura.Adif;

/// <summary>
/// Representa un registro ADIF (un QSO con todos sus campos).
/// Actúa como un diccionario case-insensitive donde cada clave es un nombre de campo ADIF
/// y cada valor es la cadena de texto asociada.
/// </summary>
public sealed class RegistroAdif
{
    private readonly Dictionary<string, string> _campos = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Obtiene o establece un campo ADIF por nombre.
    /// Asignar null elimina el campo del registro.
    /// </summary>
    /// <param name="nombreCampo">Nombre del campo ADIF (case-insensitive).</param>
    /// <returns>El valor del campo, o null si no existe.</returns>
    public string? this[string nombreCampo]
    {
        get => _campos.TryGetValue(nombreCampo.ToUpperInvariant(), out string? valor) ? valor : null;
        set
        {
            string clave = nombreCampo.ToUpperInvariant();
            if (value is null)
                _campos.Remove(clave);
            else
                _campos[clave] = value;
        }
    }

    /// <summary>
    /// Indica si el registro contiene un campo específico con valor no vacío.
    /// </summary>
    /// <param name="nombreCampo">Nombre del campo ADIF.</param>
    /// <returns>True si el campo existe en el registro.</returns>
    public bool TieneCampo(string nombreCampo)
    {
        return _campos.ContainsKey(nombreCampo.ToUpperInvariant());
    }

    /// <summary>
    /// Obtiene todos los nombres de campo presentes en el registro.
    /// </summary>
    public IReadOnlyCollection<string> NombresDeCampo => _campos.Keys;

    /// <summary>
    /// Obtiene el número de campos presentes en el registro.
    /// </summary>
    public int NumeroDeCampos => _campos.Count;

    /// <summary>
    /// Obtiene todos los pares campo-valor del registro como diccionario de solo lectura.
    /// </summary>
    /// <returns>Diccionario con los campos y sus valores.</returns>
    public IReadOnlyDictionary<string, string> ObtenerTodosLosCampos()
    {
        return _campos;
    }

    // ─────────────────────────────────────────────────────────
    // Propiedades de conveniencia para campos comunes de ADIF
    // ─────────────────────────────────────────────────────────

    /// <summary>Indicativo de la estación contactada (campo CALL).</summary>
    public string? Indicativo { get => this["CALL"]; set => this["CALL"] = value; }

    /// <summary>Fecha del QSO en formato YYYYMMDD (campo QSO_DATE).</summary>
    public string? FechaQso { get => this["QSO_DATE"]; set => this["QSO_DATE"] = value; }

    /// <summary>Hora de inicio en formato HHMM o HHMMSS (campo TIME_ON).</summary>
    public string? HoraInicio { get => this["TIME_ON"]; set => this["TIME_ON"] = value; }

    /// <summary>Hora de fin en formato HHMM o HHMMSS (campo TIME_OFF).</summary>
    public string? HoraFin { get => this["TIME_OFF"]; set => this["TIME_OFF"] = value; }

    /// <summary>Nombre de la banda, por ejemplo "20m" (campo BAND).</summary>
    public string? Banda { get => this["BAND"]; set => this["BAND"] = value; }

    /// <summary>Frecuencia en MHz (campo FREQ).</summary>
    public string? Frecuencia { get => this["FREQ"]; set => this["FREQ"] = value; }

    /// <summary>Modo de operación, por ejemplo "FT8", "SSB" (campo MODE).</summary>
    public string? Modo { get => this["MODE"]; set => this["MODE"] = value; }

    /// <summary>Submodo de operación, por ejemplo "USB", "LSB" (campo SUBMODE).</summary>
    public string? SubModo { get => this["SUBMODE"]; set => this["SUBMODE"] = value; }

    /// <summary>Reporte de señal enviado (campo RST_SENT).</summary>
    public string? SenalEnviada { get => this["RST_SENT"]; set => this["RST_SENT"] = value; }

    /// <summary>Reporte de señal recibido (campo RST_RCVD).</summary>
    public string? SenalRecibida { get => this["RST_RCVD"]; set => this["RST_RCVD"] = value; }

    /// <summary>Potencia de transmisión en vatios (campo TX_PWR).</summary>
    public string? Potencia { get => this["TX_PWR"]; set => this["TX_PWR"] = value; }

    /// <summary>Localizador Maidenhead de la estación contactada (campo GRIDSQUARE).</summary>
    public string? Localizador { get => this["GRIDSQUARE"]; set => this["GRIDSQUARE"] = value; }

    /// <summary>Localizador Maidenhead propio (campo MY_GRIDSQUARE).</summary>
    public string? MiLocalizador { get => this["MY_GRIDSQUARE"]; set => this["MY_GRIDSQUARE"] = value; }

    /// <summary>Indicativo de la estación propia (campo STATION_CALLSIGN).</summary>
    public string? IndicativoPropio { get => this["STATION_CALLSIGN"]; set => this["STATION_CALLSIGN"] = value; }

    /// <summary>Indicativo del operador (campo OPERATOR).</summary>
    public string? Operador { get => this["OPERATOR"]; set => this["OPERATOR"] = value; }

    /// <summary>Comentario de texto libre (campo COMMENT).</summary>
    public string? Comentario { get => this["COMMENT"]; set => this["COMMENT"] = value; }

    /// <summary>Notas adicionales (campo NOTES).</summary>
    public string? Notas { get => this["NOTES"]; set => this["NOTES"] = value; }

    /// <summary>Fecha de fin del QSO en formato YYYYMMDD (campo QSO_DATE_OFF).</summary>
    public string? FechaQsoFin { get => this["QSO_DATE_OFF"]; set => this["QSO_DATE_OFF"] = value; }

    /// <summary>Número de entidad DXCC (campo DXCC).</summary>
    public string? Dxcc { get => this["DXCC"]; set => this["DXCC"] = value; }

    /// <summary>Nombre del país (campo COUNTRY).</summary>
    public string? Pais { get => this["COUNTRY"]; set => this["COUNTRY"] = value; }

    /// <summary>Zona CQ (campo CQZ).</summary>
    public string? ZonaCq { get => this["CQZ"]; set => this["CQZ"] = value; }

    /// <summary>Zona ITU (campo ITUZ).</summary>
    public string? ZonaItu { get => this["ITUZ"]; set => this["ITUZ"] = value; }

    /// <summary>Continente (campo CONT).</summary>
    public string? Continente { get => this["CONT"]; set => this["CONT"] = value; }

    /// <summary>Referencia POTA de la estación contactada (campo POTA_REF).</summary>
    public string? ReferenciaPota { get => this["POTA_REF"]; set => this["POTA_REF"] = value; }

    /// <summary>Referencia SOTA de la estación contactada (campo SOTA_REF).</summary>
    public string? ReferenciaSota { get => this["SOTA_REF"]; set => this["SOTA_REF"] = value; }

    /// <summary>Referencia POTA propia (campo MY_POTA_REF).</summary>
    public string? MiReferenciaPota { get => this["MY_POTA_REF"]; set => this["MY_POTA_REF"] = value; }

    /// <summary>Referencia SOTA propia (campo MY_SOTA_REF).</summary>
    public string? MiReferenciaSota { get => this["MY_SOTA_REF"]; set => this["MY_SOTA_REF"] = value; }

    /// <summary>Grupo de actividad especial (campo SIG).</summary>
    public string? ActividadEspecial { get => this["SIG"]; set => this["SIG"] = value; }

    /// <summary>Información de actividad especial (campo SIG_INFO).</summary>
    public string? InfoActividadEspecial { get => this["SIG_INFO"]; set => this["SIG_INFO"] = value; }
}
