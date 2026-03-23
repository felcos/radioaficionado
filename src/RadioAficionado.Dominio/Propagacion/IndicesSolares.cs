namespace RadioAficionado.Dominio.Propagacion;

/// <summary>
/// Indices solares que afectan directamente la propagacion HF.
/// El SFI (Solar Flux Index) mide la radiacion solar en 2800 MHz e indica la ionizacion de la ionosfera.
/// El indice Kp mide la perturbacion geomagnetica (0=tranquilo, 9=tormenta severa).
/// </summary>
/// <param name="Sfi">Solar Flux Index (60-300). Valores altos favorecen bandas altas de HF.</param>
/// <param name="Kp">Indice K planetario (0-9). Valores bajos indican condiciones estables.</param>
/// <param name="Ap">Indice A planetario. Promedio diario de actividad geomagnetica.</param>
/// <param name="NumeroManchasSolares">Numero de manchas solares observadas (SSN).</param>
/// <param name="FechaActualizacion">Fecha y hora UTC de la ultima actualizacion de los datos.</param>
public sealed record IndicesSolares(
    int Sfi,
    int Kp,
    int Ap,
    double NumeroManchasSolares,
    DateTime FechaActualizacion)
{
    /// <summary>
    /// Valida que los indices esten dentro de rangos fisicamente posibles.
    /// </summary>
    /// <returns>True si todos los indices son validos.</returns>
    public bool EsValido()
    {
        return Sfi >= 60 && Sfi <= 300
            && Kp >= 0 && Kp <= 9
            && Ap >= 0
            && NumeroManchasSolares >= 0;
    }

    /// <summary>
    /// Indica si las condiciones geomagneticas estan perturbadas (Kp >= 4).
    /// Condiciones perturbadas degradan la propagacion en bandas altas.
    /// </summary>
    public bool CondicionesPerturbadas => Kp >= 4;

    /// <summary>
    /// Indica si el flujo solar es alto (SFI >= 150), favoreciendo bandas altas de HF.
    /// </summary>
    public bool FlujoSolarAlto => Sfi >= 150;

    /// <summary>
    /// Indica si el flujo solar es bajo (SFI menor a 90), limitando propagacion a bandas bajas.
    /// </summary>
    public bool FlujoSolarBajo => Sfi < 90;
}
