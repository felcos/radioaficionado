using RadioAficionado.Dominio.Compliance;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio que verifica el cumplimiento regulatorio de las operaciones de radio.
/// </summary>
public interface IServicioCompliance
{
    /// <summary>
    /// Verifica si una operación cumple con las regulaciones.
    /// </summary>
    /// <param name="frecuencia">Frecuencia de operación.</param>
    /// <param name="modo">Modo de operación.</param>
    /// <param name="licencia">Licencia del operador.</param>
    /// <param name="potenciaVatios">Potencia de transmisión en vatios (opcional).</param>
    /// <returns>Resultado de la verificación.</returns>
    ResultadoCompliance Verificar(Frecuencia frecuencia, ModoOperacion modo, LicenciaOperador licencia, double? potenciaVatios = null);

    /// <summary>
    /// Obtiene el plan de banda para una banda y región específicas.
    /// </summary>
    PlanDeBanda? ObtenerPlanDeBanda(BandaRadio banda, RegionItu region);

    /// <summary>
    /// Verifica si el operador está cerca del borde de la banda (para alertas).
    /// </summary>
    /// <param name="frecuencia">Frecuencia actual.</param>
    /// <param name="licencia">Licencia del operador.</param>
    /// <param name="margenHz">Margen en Hz para considerar "cerca del borde" (default: 1000 Hz).</param>
    /// <returns>True si está cerca del borde de su privilegio de banda.</returns>
    bool EstaCercaDelBordeDeBanda(Frecuencia frecuencia, LicenciaOperador licencia, int margenHz = 1000);
}
