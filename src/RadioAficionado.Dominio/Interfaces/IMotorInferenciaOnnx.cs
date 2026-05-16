using RadioAficionado.Dominio.IA;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Motor de inferencia basado en ONNX Runtime.
/// Permite cargar modelos ONNX, ejecutar inferencias individuales y en lote,
/// y gestionar el ciclo de vida de las sesiones de inferencia.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Motor genérico de inferencia que carga y ejecuta modelos ONNX. Permite clasificación de señales y predicción de propagación usando modelos entrenados externamente.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se carga un modelo con <see cref="CargarModeloAsync"/> y se ejecutan inferencias con <see cref="EjecutarInferenciaAsync"/>.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.IA.MotorInferenciaOnnx</c>.</para>
/// <para><b>Registro DI:</b> Registrada como Singleton en <c>RadioAficionado.IA.ConfiguracionServiciosIa.AgregarCapaDeIa()</c> con factory que inyecta <c>ConfiguracionOnnx</c>.</para>
/// <para><b>Configuración necesaria:</b> <c>ConfiguracionOnnx</c> con rutas a modelos .onnx y opciones de runtime. Los archivos .onnx deben existir en disco.</para>
/// <para><b>Dependencias:</b> ONNX Runtime (paquete NuGet), <c>ConfiguracionOnnx</c>. No depende de otras interfaces de dominio.</para>
/// </remarks>
public interface IMotorInferenciaOnnx : IDisposable
{
    /// <summary>
    /// Ejecuta una inferencia individual con el modelo especificado.
    /// </summary>
    /// <param name="entrada">Vector de entrada para el modelo.</param>
    /// <param name="nombreModelo">Nombre identificador del modelo cargado.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Resultado de la inferencia con salida, tiempo y etiquetas.</returns>
    Task<ResultadoInferencia> EjecutarInferenciaAsync(float[] entrada, string nombreModelo, CancellationToken ct);

    /// <summary>
    /// Ejecuta inferencias en lote con el modelo especificado.
    /// </summary>
    /// <param name="entradas">Lista de vectores de entrada.</param>
    /// <param name="nombreModelo">Nombre identificador del modelo cargado.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Lista de resultados de inferencia, uno por cada entrada.</returns>
    Task<IReadOnlyList<ResultadoInferencia>> EjecutarInferenciaLoteAsync(IReadOnlyList<float[]> entradas, string nombreModelo, CancellationToken ct);

    /// <summary>
    /// Indica si un modelo con el nombre especificado esta actualmente cargado en memoria.
    /// </summary>
    /// <param name="nombreModelo">Nombre identificador del modelo.</param>
    /// <returns>True si el modelo esta cargado; false en caso contrario.</returns>
    bool ModeloEstaCargado(string nombreModelo);

    /// <summary>
    /// Carga un modelo ONNX desde disco y lo registra con el nombre especificado.
    /// </summary>
    /// <param name="rutaModelo">Ruta completa al archivo .onnx en disco.</param>
    /// <param name="nombreModelo">Nombre identificador para el modelo.</param>
    /// <param name="ct">Token de cancelacion.</param>
    Task CargarModeloAsync(string rutaModelo, string nombreModelo, CancellationToken ct);

    /// <summary>
    /// Descarga un modelo previamente cargado, liberando su sesion de inferencia.
    /// No lanza excepcion si el modelo no esta cargado.
    /// </summary>
    /// <param name="nombreModelo">Nombre identificador del modelo a descargar.</param>
    void DescargarModelo(string nombreModelo);

    /// <summary>
    /// Obtiene la lista de nombres de todos los modelos actualmente cargados.
    /// </summary>
    /// <returns>Lista de nombres de modelos cargados.</returns>
    IReadOnlyList<string> ObtenerModelosCargados();
}
