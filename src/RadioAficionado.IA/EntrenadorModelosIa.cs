using System.Diagnostics;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Data;
using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.IA;

/// <summary>
/// Servicio que entrena modelos de IA con datos sinteticos ampliados y los exporta en formato ONNX.
/// Soporta entrenamiento de clasificador de senales (SdcaMaximumEntropy) y
/// analizador de propagacion (FastTree regression).
/// </summary>
public sealed class EntrenadorModelosIa : IEntrenadorModelosIa
{
    /// <summary>
    /// Modos que el clasificador puede detectar.
    /// </summary>
    private static readonly string[] _modosClasificables =
        ["CW", "SSB", "FT8", "FM", "AM", "Ruido"];

    /// <summary>
    /// Nombre del archivo ONNX para el clasificador de senales.
    /// </summary>
    private const string NombreArchivoClasificador = "clasificador_senales.onnx";

    /// <summary>
    /// Nombre del archivo ONNX para el analizador de propagacion.
    /// </summary>
    private const string NombreArchivoAnalizador = "analizador_propagacion.onnx";

    /// <inheritdoc />
    public Task<MetricasClasificacion> EntrenarYExportarClasificadorAsync(string rutaSalida, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rutaSalida))
        {
            throw new ArgumentException("La ruta de salida no puede ser nula o vacia.", nameof(rutaSalida));
        }

        ct.ThrowIfCancellationRequested();

        Stopwatch cronometro = Stopwatch.StartNew();

        MLContext contextoMl = new(seed: 42);

        // Generar datos sinteticos ampliados (2000+ muestras por tipo)
        List<DatoEntrenamientoSenal> datosEntrenamiento = GenerarDatosClasificadorAmpliados();

        IDataView vistaEntrenamiento = contextoMl.Data.LoadFromEnumerable(datosEntrenamiento);

        // Dividir en entrenamiento (80%) y prueba (20%)
        DataOperationsCatalog.TrainTestData division = contextoMl.Data.TrainTestSplit(
            vistaEntrenamiento, testFraction: 0.2, seed: 42);

        ct.ThrowIfCancellationRequested();

        IEstimator<ITransformer> pipeline = contextoMl.Transforms.NormalizeMinMax("EspectroNormalizado", "Espectro")
            .Append(contextoMl.Transforms.Conversion.MapValueToKey("Label", "Modo"))
            .Append(contextoMl.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "EspectroNormalizado",
                maximumNumberOfIterations: 150))
            .Append(contextoMl.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        ct.ThrowIfCancellationRequested();

        ITransformer modeloEntrenado = pipeline.Fit(division.TrainSet);

        ct.ThrowIfCancellationRequested();

        // Evaluar el modelo
        IDataView predicciones = modeloEntrenado.Transform(division.TestSet);
        MulticlassClassificationMetrics metricas = contextoMl.MulticlassClassification.Evaluate(
            predicciones, labelColumnName: "Label");

        // Construir representacion de la matriz de confusion
        string matrizConfusion = ConstruirMatrizConfusion(metricas);

        // Exportar a ONNX
        AsegurarDirectorioExiste(rutaSalida);
        using (FileStream flujo = File.Create(rutaSalida))
        {
            contextoMl.Model.ConvertToOnnx(modeloEntrenado, vistaEntrenamiento, flujo);
        }

        cronometro.Stop();

        MetricasClasificacion resultado = new(
            Math.Round(metricas.MacroAccuracy, 6),
            Math.Round(metricas.LogLoss, 6),
            matrizConfusion,
            cronometro.Elapsed);

        return Task.FromResult(resultado);
    }

    /// <inheritdoc />
    public Task<MetricasRegresion> EntrenarYExportarAnalizadorAsync(string rutaSalida, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rutaSalida))
        {
            throw new ArgumentException("La ruta de salida no puede ser nula o vacia.", nameof(rutaSalida));
        }

        ct.ThrowIfCancellationRequested();

        Stopwatch cronometro = Stopwatch.StartNew();

        MLContext contextoMl = new(seed: 42);

        // Generar datos sinteticos ampliados (3000+ muestras)
        Random rng = new(42);
        List<DatoEntrenamientoPropagacion> datosEntrenamiento = GeneradorDatosSinteticos.GenerarDatosPropagacion(3500, rng);

        IDataView vistaEntrenamiento = contextoMl.Data.LoadFromEnumerable(datosEntrenamiento);

        // Dividir en entrenamiento (80%) y prueba (20%)
        DataOperationsCatalog.TrainTestData division = contextoMl.Data.TrainTestSplit(
            vistaEntrenamiento, testFraction: 0.2, seed: 42);

        ct.ThrowIfCancellationRequested();

        IEstimator<ITransformer> pipeline = contextoMl.Transforms.NormalizeMinMax("Sfi")
            .Append(contextoMl.Transforms.NormalizeMinMax("IndiceK"))
            .Append(contextoMl.Transforms.NormalizeMinMax("IndiceA"))
            .Append(contextoMl.Transforms.NormalizeMinMax("ManchasSolares"))
            .Append(contextoMl.Transforms.NormalizeMinMax("HoraUtc"))
            .Append(contextoMl.Transforms.NormalizeMinMax("MesDelAnio"))
            .Append(contextoMl.Transforms.NormalizeMinMax("NumeroBanda"))
            .Append(contextoMl.Transforms.Concatenate("Features",
                "Sfi", "IndiceK", "IndiceA", "ManchasSolares", "HoraUtc", "MesDelAnio", "NumeroBanda"))
            .Append(contextoMl.Transforms.CopyColumns("Label", "ProbabilidadApertura"))
            .Append(contextoMl.Regression.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.2));

        ct.ThrowIfCancellationRequested();

        ITransformer modeloEntrenado = pipeline.Fit(division.TrainSet);

        ct.ThrowIfCancellationRequested();

        // Evaluar el modelo
        IDataView predicciones = modeloEntrenado.Transform(division.TestSet);
        RegressionMetrics metricas = contextoMl.Regression.Evaluate(
            predicciones, labelColumnName: "Label");

        // Exportar a ONNX
        AsegurarDirectorioExiste(rutaSalida);
        using (FileStream flujo = File.Create(rutaSalida))
        {
            contextoMl.Model.ConvertToOnnx(modeloEntrenado, vistaEntrenamiento, flujo);
        }

        cronometro.Stop();

        MetricasRegresion resultado = new(
            Math.Round(metricas.RSquared, 6),
            Math.Round(metricas.RootMeanSquaredError, 6),
            Math.Round(metricas.MeanAbsoluteError, 6),
            cronometro.Elapsed);

        return Task.FromResult(resultado);
    }

    /// <inheritdoc />
    public async Task EntrenarTodosLosModelosAsync(string directorioSalida, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(directorioSalida))
        {
            throw new ArgumentException("El directorio de salida no puede ser nulo o vacio.", nameof(directorioSalida));
        }

        if (!Directory.Exists(directorioSalida))
        {
            Directory.CreateDirectory(directorioSalida);
        }

        string rutaClasificador = Path.Combine(directorioSalida, NombreArchivoClasificador);
        string rutaAnalizador = Path.Combine(directorioSalida, NombreArchivoAnalizador);

        await EntrenarYExportarClasificadorAsync(rutaClasificador, ct).ConfigureAwait(false);
        await EntrenarYExportarAnalizadorAsync(rutaAnalizador, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Genera datos de entrenamiento ampliados para el clasificador de senales.
    /// Produce 2000+ muestras por cada tipo de senal con variaciones de SNR.
    /// </summary>
    /// <returns>Lista completa de datos de entrenamiento.</returns>
    private static List<DatoEntrenamientoSenal> GenerarDatosClasificadorAmpliados()
    {
        Random rng = new(42);
        int muestrasPorTipo = 2000;

        List<DatoEntrenamientoSenal> datos = new(muestrasPorTipo * 6);

        datos.AddRange(GeneradorDatosSinteticos.GenerarEspectrosCw(muestrasPorTipo, rng));
        datos.AddRange(GeneradorDatosSinteticos.GenerarEspectrosSsb(muestrasPorTipo, rng));
        datos.AddRange(GeneradorDatosSinteticos.GenerarEspectrosFm(muestrasPorTipo, rng));
        datos.AddRange(GeneradorDatosSinteticos.GenerarEspectrosFt8(muestrasPorTipo, rng));
        datos.AddRange(GeneradorDatosSinteticos.GenerarEspectrosAm(muestrasPorTipo, rng));
        datos.AddRange(GeneradorDatosSinteticos.GenerarEspectrosRuido(muestrasPorTipo, rng));

        return datos;
    }

    /// <summary>
    /// Construye una representacion textual de la matriz de confusion a partir de las metricas.
    /// </summary>
    /// <param name="metricas">Metricas de clasificacion multiclase.</param>
    /// <returns>Cadena con la matriz de confusion formateada.</returns>
    private static string ConstruirMatrizConfusion(MulticlassClassificationMetrics metricas)
    {
        StringBuilder sb = new();
        sb.AppendLine($"MacroAccuracy: {metricas.MacroAccuracy:F4}");
        sb.AppendLine($"MicroAccuracy: {metricas.MicroAccuracy:F4}");
        sb.AppendLine($"LogLoss: {metricas.LogLoss:F4}");
        sb.AppendLine($"LogLossReduction: {metricas.LogLossReduction:F4}");

        if (metricas.ConfusionMatrix != null)
        {
            sb.AppendLine("Matriz de confusion:");
            sb.Append(metricas.ConfusionMatrix.GetFormattedConfusionTable());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Asegura que el directorio padre de la ruta especificada exista, creandolo si es necesario.
    /// </summary>
    /// <param name="rutaArchivo">Ruta completa del archivo.</param>
    private static void AsegurarDirectorioExiste(string rutaArchivo)
    {
        string? directorio = Path.GetDirectoryName(rutaArchivo);
        if (!string.IsNullOrWhiteSpace(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
        }
    }
}
