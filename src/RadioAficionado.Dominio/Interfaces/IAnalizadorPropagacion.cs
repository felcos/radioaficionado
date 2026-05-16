using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Propagacion;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Analizador de propagacion basado en modelos de IA.
/// Predice la probabilidad de apertura de bandas HF a partir de indices solares.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Usa modelos de machine learning para predecir la probabilidad de apertura de bandas HF a partir de índices solares (SFI, Kp, Ap). Complementa la predicción algorítmica de <see cref="IServicioPropagacion"/>.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se llama a <see cref="PredecirAsync"/> para una banda específica o <see cref="PredecirTodasLasBandasAsync"/> para todas las bandas HF.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.IA.AnalizadorPropagacionMlNet</c> (usa ML.NET).</para>
/// <para><b>Registro DI:</b> Registrada como Singleton en <c>RadioAficionado.IA.ConfiguracionServiciosIa.AgregarCapaDeIa()</c>.</para>
/// <para><b>Configuración necesaria:</b> Ninguna configuración externa. El modelo ML.NET se entrena internamente.</para>
/// <para><b>Dependencias:</b> ML.NET (paquete NuGet). No depende de otras interfaces de dominio.</para>
/// </remarks>
public interface IAnalizadorPropagacion
{
    /// <summary>
    /// Predice la probabilidad de apertura de una banda especifica dados los indices solares actuales.
    /// </summary>
    /// <param name="indices">Indices solares actuales (SFI, Kp, Ap, manchas solares).</param>
    /// <param name="banda">Banda de radioaficionado a evaluar.</param>
    /// <param name="tokenCancelacion">Token para cancelar la operacion.</param>
    /// <returns>Prediccion de propagacion con probabilidad de apertura y confianza.</returns>
    Task<PrediccionPropagacionIa> PredecirAsync(
        IndicesSolares indices,
        BandaRadio banda,
        CancellationToken tokenCancelacion = default);

    /// <summary>
    /// Predice la probabilidad de apertura de todas las bandas HF dados los indices solares actuales.
    /// </summary>
    /// <param name="indices">Indices solares actuales (SFI, Kp, Ap, manchas solares).</param>
    /// <param name="tokenCancelacion">Token para cancelar la operacion.</param>
    /// <returns>Lista de predicciones para todas las bandas HF.</returns>
    Task<IReadOnlyList<PrediccionPropagacionIa>> PredecirTodasLasBandasAsync(
        IndicesSolares indices,
        CancellationToken tokenCancelacion = default);
}
