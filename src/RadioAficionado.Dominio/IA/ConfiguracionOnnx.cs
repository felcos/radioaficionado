namespace RadioAficionado.Dominio.IA;

/// <summary>
/// Configuracion para el motor de inferencia ONNX Runtime.
/// Define parametros de directorio de modelos, uso de GPU e hilos.
/// </summary>
/// <param name="DirectorioModelos">Directorio donde se almacenan los archivos .onnx.</param>
/// <param name="UsarGpu">Indica si se debe usar GPU (CUDA) para la inferencia.</param>
/// <param name="HilosInferencia">Numero de hilos para operaciones intra-op de ONNX Runtime.</param>
/// <param name="TamanoPoolSesiones">Numero maximo de sesiones en el pool por modelo.</param>
public sealed record ConfiguracionOnnx(
    string DirectorioModelos = "modelos",
    bool UsarGpu = false,
    int HilosInferencia = 4,
    int TamanoPoolSesiones = 2);
