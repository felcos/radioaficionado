using Microsoft.ML;
using Microsoft.ML.Data;
using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.IA;

/// <summary>
/// Dato de entrenamiento para el modelo de clasificacion de senales.
/// Cada registro contiene 64 bins de espectro reducido y la etiqueta del modo.
/// </summary>
internal sealed class DatoEntrenamientoSenal
{
    /// <summary>Espectro reducido a 64 bins por binning.</summary>
    [VectorType(64)]
    public float[] Espectro { get; set; } = new float[64];

    /// <summary>Etiqueta del modo de operacion.</summary>
    public string Modo { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de la prediccion del modelo ML.NET de clasificacion de senales.
/// </summary>
internal sealed class ClasificacionSenalSalida
{
    /// <summary>Modo predicho.</summary>
    [ColumnName("PredictedLabel")]
    public string ModoPredicho { get; set; } = string.Empty;

    /// <summary>Probabilidades por clase.</summary>
    [ColumnName("Score")]
    public float[] Puntuaciones { get; set; } = [];
}

/// <summary>
/// Implementacion del clasificador de senales usando ML.NET con SdcaMaximumEntropy.
/// Entrena un modelo multiclase con datos sinteticos que simulan las firmas espectrales
/// de los principales modos de operacion de radioaficionado.
/// Thread-safe mediante lock para el PredictionEngine.
/// </summary>
public sealed class ClasificadorSenalesMlNet : IClasificadorSenales
{
    private readonly MLContext _contextoMl;
    private readonly PredictionEngine<DatoEntrenamientoSenal, ClasificacionSenalSalida> _motorPrediccion;
    private readonly object _bloqueo = new();
    private readonly IReadOnlyList<string> _nombresClases;

    /// <summary>
    /// Numero de bins del espectro reducido.
    /// </summary>
    private const int TamanioEspectro = 64;

    /// <summary>
    /// Modos que el clasificador puede detectar.
    /// </summary>
    private static readonly string[] _modosClasificables =
        ["CW", "SSB", "FT8", "FM", "RTTY", "PSK", "AM", "Ruido"];

    /// <summary>
    /// Inicializa el clasificador entrenando el modelo multiclase con datos sinteticos.
    /// </summary>
    public ClasificadorSenalesMlNet()
    {
        _contextoMl = new MLContext(seed: 42);
        _nombresClases = _modosClasificables.ToList().AsReadOnly();

        List<DatoEntrenamientoSenal> datosEntrenamiento = GenerarDatosSinteticos();

        IDataView vistaEntrenamiento = _contextoMl.Data.LoadFromEnumerable(datosEntrenamiento);

        IEstimator<ITransformer> pipeline = _contextoMl.Transforms.NormalizeMinMax("EspectroNormalizado", "Espectro")
            .Append(_contextoMl.Transforms.Conversion.MapValueToKey("Label", "Modo"))
            .Append(_contextoMl.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "EspectroNormalizado",
                maximumNumberOfIterations: 100))
            .Append(_contextoMl.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        ITransformer modeloEntrenado = pipeline.Fit(vistaEntrenamiento);

        _motorPrediccion = _contextoMl.Model.CreatePredictionEngine<DatoEntrenamientoSenal, ClasificacionSenalSalida>(modeloEntrenado);
    }

    /// <inheritdoc />
    public Task<ResultadoClasificacion> ClasificarAsync(
        ReadOnlyMemory<float> espectro,
        CancellationToken tokenCancelacion = default)
    {
        tokenCancelacion.ThrowIfCancellationRequested();

        ResultadoClasificacion resultado = RealizarClasificacion(espectro);
        return Task.FromResult(resultado);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ResultadoClasificacion>> ClasificarLoteAsync(
        IReadOnlyList<ReadOnlyMemory<float>> espectros,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(espectros);
        tokenCancelacion.ThrowIfCancellationRequested();

        List<ResultadoClasificacion> resultados = new(espectros.Count);

        foreach (ReadOnlyMemory<float> espectro in espectros)
        {
            tokenCancelacion.ThrowIfCancellationRequested();
            ResultadoClasificacion resultado = RealizarClasificacion(espectro);
            resultados.Add(resultado);
        }

        return Task.FromResult<IReadOnlyList<ResultadoClasificacion>>(resultados.AsReadOnly());
    }

    /// <summary>
    /// Realiza la clasificacion de un espectro individual.
    /// Reduce el espectro a 64 bins mediante binning si es necesario.
    /// </summary>
    private ResultadoClasificacion RealizarClasificacion(ReadOnlyMemory<float> espectro)
    {
        float[] espectroReducido = ReducirEspectro(espectro);

        DatoEntrenamientoSenal entrada = new()
        {
            Espectro = espectroReducido
        };

        ClasificacionSenalSalida salida;
        lock (_bloqueo)
        {
            salida = _motorPrediccion.Predict(entrada);
        }

        ModoOperacion modoDetectado = ConvertirNombreAModo(salida.ModoPredicho);

        // Construir lista de alternativas a partir de las puntuaciones
        List<ModoAlternativo> alternativas = new();
        float confianzaPrincipal = 0f;

        if (salida.Puntuaciones != null && salida.Puntuaciones.Length > 0)
        {
            // Aplicar softmax para obtener probabilidades
            float[] probabilidades = AplicarSoftmax(salida.Puntuaciones);

            List<(string Nombre, float Probabilidad)> modosConProbabilidad = new();
            for (int i = 0; i < Math.Min(probabilidades.Length, _nombresClases.Count); i++)
            {
                modosConProbabilidad.Add((_nombresClases[i], probabilidades[i]));
            }

            // Ordenar por probabilidad descendente
            modosConProbabilidad.Sort((a, b) => b.Probabilidad.CompareTo(a.Probabilidad));

            bool primeroProcesado = false;
            foreach ((string nombre, float probabilidad) in modosConProbabilidad)
            {
                ModoOperacion modo = ConvertirNombreAModo(nombre);

                if (!primeroProcesado && nombre == salida.ModoPredicho)
                {
                    confianzaPrincipal = probabilidad;
                    primeroProcesado = true;
                    continue;
                }

                if (!primeroProcesado)
                {
                    // El modo predicho no coincide con el primero por puntuacion;
                    // usar la probabilidad del modo predicho
                    confianzaPrincipal = probabilidad;
                    primeroProcesado = true;
                }

                alternativas.Add(new ModoAlternativo(modo, Math.Round(probabilidad, 4)));
            }

            if (!primeroProcesado)
            {
                confianzaPrincipal = modosConProbabilidad.Count > 0 ? modosConProbabilidad[0].Probabilidad : 0.5f;
            }
        }
        else
        {
            confianzaPrincipal = 0.5f;
        }

        return new ResultadoClasificacion(
            modoDetectado,
            Math.Round(Math.Clamp(confianzaPrincipal, 0.0, 1.0), 4),
            alternativas.AsReadOnly());
    }

    /// <summary>
    /// Reduce un espectro de cualquier tamano a exactamente 64 bins mediante binning.
    /// </summary>
    private static float[] ReducirEspectro(ReadOnlyMemory<float> espectro)
    {
        ReadOnlySpan<float> span = espectro.Span;
        float[] resultado = new float[TamanioEspectro];

        if (span.Length == 0)
        {
            return resultado;
        }

        if (span.Length <= TamanioEspectro)
        {
            // Copiar directamente y rellenar con ceros
            for (int i = 0; i < span.Length; i++)
            {
                resultado[i] = span[i];
            }
            return resultado;
        }

        // Binning: promediar bloques
        double tamanioBin = (double)span.Length / TamanioEspectro;
        for (int i = 0; i < TamanioEspectro; i++)
        {
            int inicio = (int)(i * tamanioBin);
            int fin = (int)((i + 1) * tamanioBin);
            fin = Math.Min(fin, span.Length);

            float suma = 0;
            int cuenta = 0;
            for (int j = inicio; j < fin; j++)
            {
                suma += span[j];
                cuenta++;
            }

            resultado[i] = cuenta > 0 ? suma / cuenta : 0;
        }

        return resultado;
    }

    /// <summary>
    /// Aplica la funcion softmax a las puntuaciones para obtener probabilidades.
    /// </summary>
    private static float[] AplicarSoftmax(float[] puntuaciones)
    {
        float maximo = puntuaciones.Max();
        float[] exponenciales = puntuaciones.Select(p => MathF.Exp(p - maximo)).ToArray();
        float sumaExp = exponenciales.Sum();

        return exponenciales.Select(e => e / sumaExp).ToArray();
    }

    /// <summary>
    /// Convierte el nombre de modo (string) al enum ModoOperacion correspondiente.
    /// </summary>
    private static ModoOperacion ConvertirNombreAModo(string nombre)
    {
        return nombre switch
        {
            "CW" => ModoOperacion.CW,
            "SSB" => ModoOperacion.SSB,
            "FT8" => ModoOperacion.FT8,
            "FM" => ModoOperacion.FM,
            "RTTY" => ModoOperacion.RTTY,
            "PSK" => ModoOperacion.PSK,
            "AM" => ModoOperacion.AM,
            _ => ModoOperacion.CW // Ruido y desconocidos -> CW como fallback
        };
    }

    /// <summary>
    /// Genera datos sinteticos de entrenamiento que simulan las firmas espectrales
    /// de los principales modos de operacion de radioaficionado.
    /// </summary>
    private static List<DatoEntrenamientoSenal> GenerarDatosSinteticos()
    {
        List<DatoEntrenamientoSenal> datos = new();
        Random aleatorio = new(42);

        int muestrasPorModo = 100;

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

        // Inicializar con ruido de fondo
        for (int i = 0; i < TamanioEspectro; i++)
        {
            espectro[i] = (float)(aleatorio.NextDouble() * ruido);
        }

        switch (modo)
        {
            case "CW":
                // Pico estrecho en una frecuencia (~2-3 bins de ancho)
                int posicionCw = aleatorio.Next(10, 54);
                espectro[posicionCw] = 0.9f + (float)(aleatorio.NextDouble() * 0.1);
                espectro[posicionCw - 1] = 0.3f + (float)(aleatorio.NextDouble() * 0.1);
                espectro[posicionCw + 1] = 0.3f + (float)(aleatorio.NextDouble() * 0.1);
                break;

            case "SSB":
                // Energia distribuida en ~2.4 kHz (aprox 10 bins para simular)
                int inicioSsb = aleatorio.Next(5, 40);
                for (int i = inicioSsb; i < Math.Min(inicioSsb + 10, TamanioEspectro); i++)
                {
                    espectro[i] = 0.4f + (float)(aleatorio.NextDouble() * 0.3);
                }
                break;

            case "FT8":
                // Multiples tonos (~8) distribuidos en banda de 3 kHz (~12 bins)
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
                // Portadora fuerte central con desviacion
                int posicionFm = aleatorio.Next(20, 44);
                espectro[posicionFm] = 0.95f + (float)(aleatorio.NextDouble() * 0.05);
                // Bandas laterales por modulacion
                for (int d = 1; d <= 5; d++)
                {
                    float amplitud = 0.4f / d;
                    if (posicionFm - d >= 0) espectro[posicionFm - d] = amplitud + (float)(aleatorio.NextDouble() * 0.05);
                    if (posicionFm + d < TamanioEspectro) espectro[posicionFm + d] = amplitud + (float)(aleatorio.NextDouble() * 0.05);
                }
                break;

            case "RTTY":
                // Dos tonos separados (mark y space, ~170 Hz shift -> ~2 bins)
                int posicionMark = aleatorio.Next(15, 50);
                int posicionSpace = posicionMark + 2 + aleatorio.Next(0, 2);
                espectro[posicionMark] = 0.8f + (float)(aleatorio.NextDouble() * 0.15);
                if (posicionSpace < TamanioEspectro)
                {
                    espectro[posicionSpace] = 0.8f + (float)(aleatorio.NextDouble() * 0.15);
                }
                break;

            case "PSK":
                // Tono unico modulado en fase (pico con ensanchamiento minimo)
                int posicionPsk = aleatorio.Next(10, 54);
                espectro[posicionPsk] = 0.85f + (float)(aleatorio.NextDouble() * 0.1);
                espectro[Math.Max(0, posicionPsk - 1)] = 0.15f + (float)(aleatorio.NextDouble() * 0.1);
                espectro[Math.Min(TamanioEspectro - 1, posicionPsk + 1)] = 0.15f + (float)(aleatorio.NextDouble() * 0.1);
                // PSK es mas ancho que CW pero mas estrecho
                espectro[Math.Max(0, posicionPsk - 2)] = 0.08f + (float)(aleatorio.NextDouble() * 0.05);
                espectro[Math.Min(TamanioEspectro - 1, posicionPsk + 2)] = 0.08f + (float)(aleatorio.NextDouble() * 0.05);
                break;

            case "AM":
                // Portadora central fuerte con bandas laterales simetricas
                int posicionAm = aleatorio.Next(15, 49);
                espectro[posicionAm] = 0.95f + (float)(aleatorio.NextDouble() * 0.05);
                // Bandas laterales simetricas (~5 kHz cada lado -> ~4 bins)
                for (int d = 1; d <= 4; d++)
                {
                    float amplitud = 0.35f - (d * 0.06f);
                    if (posicionAm - d >= 0) espectro[posicionAm - d] = amplitud + (float)(aleatorio.NextDouble() * 0.05);
                    if (posicionAm + d < TamanioEspectro) espectro[posicionAm + d] = amplitud + (float)(aleatorio.NextDouble() * 0.05);
                }
                break;

            case "Ruido":
                // Energia uniforme en todo el espectro
                for (int i = 0; i < TamanioEspectro; i++)
                {
                    espectro[i] = 0.15f + (float)(aleatorio.NextDouble() * 0.15);
                }
                break;
        }

        return espectro;
    }
}
