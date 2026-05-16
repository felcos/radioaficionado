using RadioAficionado.Dominio.IA;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Clasificador de senales de radio basado en modelos de IA.
/// Detecta el modo de operacion a partir del espectro de frecuencia de la senal.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Clasifica automáticamente señales de radio detectando el modo de operación (CW, SSB, FT8, etc.) a partir del espectro FFT de la señal. Útil para identificación automática de modos en el waterfall.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se llama a <see cref="ClasificarAsync"/> con los datos de espectro FFT de una señal para obtener el modo detectado.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.IA.ClasificadorSenalesMlNet</c> (ML.NET), <c>RadioAficionado.IA.ClasificadorSenalesOnnx</c> (ONNX Runtime, disponible como tipo concreto).</para>
/// <para><b>Registro DI:</b> Registrada como Singleton (implementación ML.NET) en <c>RadioAficionado.IA.ConfiguracionServiciosIa.AgregarCapaDeIa()</c>.</para>
/// <para><b>Configuración necesaria:</b> Ninguna configuración externa. Los modelos se cargan internamente.</para>
/// <para><b>Dependencias:</b> ML.NET o ONNX Runtime según la implementación. No depende de otras interfaces de dominio.</para>
/// </remarks>
public interface IClasificadorSenales
{
    /// <summary>
    /// Clasifica una senal individual a partir de su espectro de frecuencia.
    /// </summary>
    /// <param name="espectro">Datos del espectro de frecuencia de la senal (magnitudes FFT).</param>
    /// <param name="tokenCancelacion">Token para cancelar la operacion.</param>
    /// <returns>Resultado de la clasificacion con el modo detectado y alternativas.</returns>
    Task<ResultadoClasificacion> ClasificarAsync(
        ReadOnlyMemory<float> espectro,
        CancellationToken tokenCancelacion = default);

    /// <summary>
    /// Clasifica un lote de senales a partir de sus espectros de frecuencia.
    /// </summary>
    /// <param name="espectros">Lista de espectros de frecuencia a clasificar.</param>
    /// <param name="tokenCancelacion">Token para cancelar la operacion.</param>
    /// <returns>Lista de resultados de clasificacion, uno por cada espectro de entrada.</returns>
    Task<IReadOnlyList<ResultadoClasificacion>> ClasificarLoteAsync(
        IReadOnlyList<ReadOnlyMemory<float>> espectros,
        CancellationToken tokenCancelacion = default);
}
