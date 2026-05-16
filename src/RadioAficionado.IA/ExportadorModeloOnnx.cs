using Microsoft.ML;
using Microsoft.ML.Data;

namespace RadioAficionado.IA;

/// <summary>
/// Utilidad para exportar modelos ML.NET entrenados al formato ONNX.
/// Permite convertir el clasificador de senales y el analizador de propagacion
/// a modelos ONNX para su uso con ONNX Runtime.
/// </summary>
public static class ExportadorModeloOnnx
{
    /// <summary>
    /// Numero de bins del espectro reducido usado por el clasificador.
    /// </summary>
    private const int TamanioEspectro = 64;

    /// <summary>
    /// Modos que el clasificador puede detectar.
    /// </summary>
    private static readonly string[] _modosClasificables =
        ["CW", "SSB", "FT8", "FM", "RTTY", "PSK", "AM", "Ruido"];

    /// <summary>
    /// Exporta un modelo ML.NET entrenado (ITransformer) al formato ONNX.
    /// </summary>
    /// <param name="contextoMl">Contexto de ML.NET.</param>
    /// <param name="modelo">Modelo entrenado a exportar.</param>
    /// <param name="datosEntrada">Vista de datos de entrada del modelo (necesaria para inferir el esquema).</param>
    /// <param name="rutaSalida">Ruta completa del archivo .onnx de salida.</param>
    public static void ExportarModeloMlNet(
        MLContext contextoMl,
        ITransformer modelo,
        IDataView datosEntrada,
        string rutaSalida)
    {
        ArgumentNullException.ThrowIfNull(contextoMl);
        ArgumentNullException.ThrowIfNull(modelo);
        ArgumentNullException.ThrowIfNull(datosEntrada);

        if (string.IsNullOrWhiteSpace(rutaSalida))
        {
            throw new ArgumentException("La ruta de salida no puede ser nula o vacia.", nameof(rutaSalida));
        }

        string? directorio = Path.GetDirectoryName(rutaSalida);
        if (!string.IsNullOrWhiteSpace(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        using FileStream flujo = File.Create(rutaSalida);
        contextoMl.Model.ConvertToOnnx(modelo, datosEntrada, flujo);
    }

    /// <summary>
    /// Entrena el clasificador de senales ML.NET y lo exporta como modelo ONNX.
    /// Genera datos sinteticos, entrena el modelo multiclase y lo serializa en formato ONNX.
    /// </summary>
    /// <param name="rutaSalida">Ruta completa del archivo .onnx de salida.</param>
    public static void ExportarClasificador(string rutaSalida)
    {
        if (string.IsNullOrWhiteSpace(rutaSalida))
        {
            throw new ArgumentException("La ruta de salida no puede ser nula o vacia.", nameof(rutaSalida));
        }

        string? directorio = Path.GetDirectoryName(rutaSalida);
        if (!string.IsNullOrWhiteSpace(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        MLContext contextoMl = new(seed: 42);

        List<DatoEntrenamientoSenal> datosEntrenamiento = GenerarDatosSinteticosClasificador();
        IDataView vistaEntrenamiento = contextoMl.Data.LoadFromEnumerable(datosEntrenamiento);

        IEstimator<ITransformer> pipeline = contextoMl.Transforms.NormalizeMinMax("EspectroNormalizado", "Espectro")
            .Append(contextoMl.Transforms.Conversion.MapValueToKey("Label", "Modo"))
            .Append(contextoMl.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "EspectroNormalizado",
                maximumNumberOfIterations: 100))
            .Append(contextoMl.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        ITransformer modeloEntrenado = pipeline.Fit(vistaEntrenamiento);

        using FileStream flujo = File.Create(rutaSalida);
        contextoMl.Model.ConvertToOnnx(modeloEntrenado, vistaEntrenamiento, flujo);
    }

    /// <summary>
    /// Entrena el analizador de propagacion ML.NET y lo exporta como modelo ONNX.
    /// Genera datos sinteticos, entrena el modelo de regresion y lo serializa en formato ONNX.
    /// </summary>
    /// <param name="rutaSalida">Ruta completa del archivo .onnx de salida.</param>
    public static void ExportarAnalizador(string rutaSalida)
    {
        if (string.IsNullOrWhiteSpace(rutaSalida))
        {
            throw new ArgumentException("La ruta de salida no puede ser nula o vacia.", nameof(rutaSalida));
        }

        string? directorio = Path.GetDirectoryName(rutaSalida);
        if (!string.IsNullOrWhiteSpace(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        MLContext contextoMl = new(seed: 42);

        List<DatoEntrenamientoPropagacion> datosEntrenamiento = GenerarDatosSinteticosAnalizador();
        IDataView vistaEntrenamiento = contextoMl.Data.LoadFromEnumerable(datosEntrenamiento);

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

        ITransformer modeloEntrenado = pipeline.Fit(vistaEntrenamiento);

        using FileStream flujo = File.Create(rutaSalida);
        contextoMl.Model.ConvertToOnnx(modeloEntrenado, vistaEntrenamiento, flujo);
    }

    /// <summary>
    /// Genera datos sinteticos de entrenamiento para el clasificador de senales.
    /// </summary>
    private static List<DatoEntrenamientoSenal> GenerarDatosSinteticosClasificador()
    {
        List<DatoEntrenamientoSenal> datos = new();
        Random aleatorio = new(42);
        int muestrasPorModo = 50;

        foreach (string modo in _modosClasificables)
        {
            for (int i = 0; i < muestrasPorModo; i++)
            {
                float[] espectro = GenerarEspectroSintetico(modo, aleatorio);
                datos.Add(new DatoEntrenamientoSenal
                {
                    Espectro = espectro,
                    Modo = modo
                });
            }
        }

        return datos;
    }

    /// <summary>
    /// Genera un espectro sintetico de 64 bins que simula la firma espectral del modo dado.
    /// </summary>
    private static float[] GenerarEspectroSintetico(string modo, Random aleatorio)
    {
        float[] espectro = new float[TamanioEspectro];
        float ruido = 0.05f;

        for (int i = 0; i < TamanioEspectro; i++)
        {
            espectro[i] = (float)(aleatorio.NextDouble() * ruido);
        }

        switch (modo)
        {
            case "CW":
                int posicionCw = aleatorio.Next(10, 54);
                espectro[posicionCw] = 0.9f + (float)(aleatorio.NextDouble() * 0.1);
                break;

            case "SSB":
                int inicioSsb = aleatorio.Next(5, 40);
                for (int i = inicioSsb; i < Math.Min(inicioSsb + 10, TamanioEspectro); i++)
                {
                    espectro[i] = 0.4f + (float)(aleatorio.NextDouble() * 0.3);
                }
                break;

            case "FT8":
                int inicioFt8 = aleatorio.Next(5, 40);
                for (int t = 0; t < 8; t++)
                {
                    int pos = inicioFt8 + (int)(t * 1.5);
                    if (pos < TamanioEspectro)
                    {
                        espectro[pos] = 0.6f + (float)(aleatorio.NextDouble() * 0.3);
                    }
                }
                break;

            case "FM":
                int posicionFm = aleatorio.Next(20, 44);
                espectro[posicionFm] = 0.95f;
                break;

            case "RTTY":
                int posicionMark = aleatorio.Next(15, 50);
                espectro[posicionMark] = 0.8f;
                int posicionSpace = posicionMark + 2;
                if (posicionSpace < TamanioEspectro)
                {
                    espectro[posicionSpace] = 0.8f;
                }
                break;

            case "PSK":
                int posicionPsk = aleatorio.Next(10, 54);
                espectro[posicionPsk] = 0.85f;
                break;

            case "AM":
                int posicionAm = aleatorio.Next(15, 49);
                espectro[posicionAm] = 0.95f;
                for (int d = 1; d <= 4; d++)
                {
                    float amplitud = 0.35f - (d * 0.06f);
                    if (posicionAm - d >= 0) espectro[posicionAm - d] = amplitud;
                    if (posicionAm + d < TamanioEspectro) espectro[posicionAm + d] = amplitud;
                }
                break;

            case "Ruido":
                for (int i = 0; i < TamanioEspectro; i++)
                {
                    espectro[i] = 0.15f + (float)(aleatorio.NextDouble() * 0.15);
                }
                break;
        }

        return espectro;
    }

    /// <summary>
    /// Genera datos sinteticos de entrenamiento para el analizador de propagacion.
    /// </summary>
    private static List<DatoEntrenamientoPropagacion> GenerarDatosSinteticosAnalizador()
    {
        List<DatoEntrenamientoPropagacion> datos = new();
        Random aleatorio = new(42);

        int[] valoresSfi = [65, 100, 150, 200];
        int[] valoresKp = [0, 2, 5, 9];
        float[] horas = [0, 6, 12, 18];
        float[] bandas = [1.9f, 7.15f, 14.175f, 28.85f];

        foreach (int sfi in valoresSfi)
        {
            foreach (int kp in valoresKp)
            {
                foreach (float hora in horas)
                {
                    foreach (float banda in bandas)
                    {
                        float probabilidad = Math.Clamp(
                            (sfi / 300f) * (1f - kp / 9f) + (float)(aleatorio.NextDouble() * 0.1 - 0.05),
                            0f, 1f);

                        datos.Add(new DatoEntrenamientoPropagacion
                        {
                            Sfi = sfi,
                            IndiceK = kp,
                            IndiceA = kp * 4,
                            ManchasSolares = sfi * 0.8f,
                            HoraUtc = hora,
                            MesDelAnio = aleatorio.Next(1, 13),
                            NumeroBanda = banda,
                            ProbabilidadApertura = probabilidad
                        });
                    }
                }
            }
        }

        return datos;
    }
}
