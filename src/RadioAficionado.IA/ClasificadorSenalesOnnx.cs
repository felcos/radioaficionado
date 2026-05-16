using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.IA;

/// <summary>
/// Clasificador de senales de radio basado en ONNX Runtime.
/// Utiliza un modelo ONNX pre-cargado para clasificar senales. Si el modelo ONNX
/// no esta disponible, delega al clasificador ML.NET como fallback.
/// </summary>
public sealed class ClasificadorSenalesOnnx : IClasificadorSenales
{
    private readonly IMotorInferenciaOnnx _motorOnnx;
    private readonly ClasificadorSenalesMlNet _clasificadorFallback;

    /// <summary>
    /// Nombre del modelo ONNX para clasificacion de senales.
    /// </summary>
    private const string NombreModeloOnnx = "clasificador_senales";

    /// <summary>
    /// Numero de bins del espectro reducido.
    /// </summary>
    private const int TamanioEspectro = 64;

    /// <summary>
    /// Modos que el clasificador puede detectar, en el orden de salida del modelo.
    /// </summary>
    private static readonly string[] _modosClasificables =
        ["CW", "SSB", "FT8", "FM", "RTTY", "PSK", "AM", "Ruido"];

    /// <summary>
    /// Nombre de esta implementacion para diagnostico.
    /// </summary>
    public string NombreImplementacion => "ClasificadorSenalesOnnx";

    /// <summary>
    /// Inicializa el clasificador ONNX con el motor de inferencia y un clasificador ML.NET de fallback.
    /// </summary>
    /// <param name="motorOnnx">Motor de inferencia ONNX para ejecutar el modelo.</param>
    /// <param name="clasificadorFallback">Clasificador ML.NET para usar cuando el modelo ONNX no esta cargado.</param>
    public ClasificadorSenalesOnnx(IMotorInferenciaOnnx motorOnnx, ClasificadorSenalesMlNet clasificadorFallback)
    {
        ArgumentNullException.ThrowIfNull(motorOnnx);
        ArgumentNullException.ThrowIfNull(clasificadorFallback);

        _motorOnnx = motorOnnx;
        _clasificadorFallback = clasificadorFallback;
    }

    /// <inheritdoc />
    public async Task<ResultadoClasificacion> ClasificarAsync(
        ReadOnlyMemory<float> espectro,
        CancellationToken tokenCancelacion = default)
    {
        tokenCancelacion.ThrowIfCancellationRequested();

        // Si el modelo ONNX no esta cargado, usar fallback ML.NET
        if (!_motorOnnx.ModeloEstaCargado(NombreModeloOnnx))
        {
            return await _clasificadorFallback.ClasificarAsync(espectro, tokenCancelacion).ConfigureAwait(false);
        }

        float[] espectroReducido = ReducirEspectro(espectro);
        float[] espectroNormalizado = NormalizarEspectro(espectroReducido);

        ResultadoInferencia inferencia = await _motorOnnx.EjecutarInferenciaAsync(
            espectroNormalizado, NombreModeloOnnx, tokenCancelacion).ConfigureAwait(false);

        return ConvertirAResultadoClasificacion(inferencia);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ResultadoClasificacion>> ClasificarLoteAsync(
        IReadOnlyList<ReadOnlyMemory<float>> espectros,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(espectros);
        tokenCancelacion.ThrowIfCancellationRequested();

        if (espectros.Count == 0)
        {
            return Array.Empty<ResultadoClasificacion>();
        }

        // Si el modelo ONNX no esta cargado, usar fallback ML.NET
        if (!_motorOnnx.ModeloEstaCargado(NombreModeloOnnx))
        {
            return await _clasificadorFallback.ClasificarLoteAsync(espectros, tokenCancelacion).ConfigureAwait(false);
        }

        List<float[]> entradasOnnx = new(espectros.Count);
        foreach (ReadOnlyMemory<float> espectro in espectros)
        {
            float[] espectroReducido = ReducirEspectro(espectro);
            float[] espectroNormalizado = NormalizarEspectro(espectroReducido);
            entradasOnnx.Add(espectroNormalizado);
        }

        IReadOnlyList<ResultadoInferencia> inferencias = await _motorOnnx.EjecutarInferenciaLoteAsync(
            entradasOnnx, NombreModeloOnnx, tokenCancelacion).ConfigureAwait(false);

        List<ResultadoClasificacion> resultados = new(inferencias.Count);
        foreach (ResultadoInferencia inferencia in inferencias)
        {
            resultados.Add(ConvertirAResultadoClasificacion(inferencia));
        }

        return resultados.AsReadOnly();
    }

    /// <summary>
    /// Convierte un resultado de inferencia ONNX a un ResultadoClasificacion del dominio.
    /// Aplica softmax a las salidas del modelo para obtener probabilidades.
    /// </summary>
    private static ResultadoClasificacion ConvertirAResultadoClasificacion(ResultadoInferencia inferencia)
    {
        float[] probabilidades = AplicarSoftmax(inferencia.Salida);

        List<(string Nombre, float Probabilidad)> modosConProbabilidad = new();
        for (int i = 0; i < Math.Min(probabilidades.Length, _modosClasificables.Length); i++)
        {
            modosConProbabilidad.Add((_modosClasificables[i], probabilidades[i]));
        }

        modosConProbabilidad.Sort((a, b) => b.Probabilidad.CompareTo(a.Probabilidad));

        string modoPrincipal = modosConProbabilidad[0].Nombre;
        float confianzaPrincipal = modosConProbabilidad[0].Probabilidad;

        ModoOperacion modoDetectado = ConvertirNombreAModo(modoPrincipal);

        List<ModoAlternativo> alternativas = new();
        for (int i = 1; i < modosConProbabilidad.Count; i++)
        {
            ModoOperacion modo = ConvertirNombreAModo(modosConProbabilidad[i].Nombre);
            alternativas.Add(new ModoAlternativo(modo, Math.Round(modosConProbabilidad[i].Probabilidad, 4)));
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
            for (int i = 0; i < span.Length; i++)
            {
                resultado[i] = span[i];
            }
            return resultado;
        }

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
    /// Normaliza el espectro dividiendo por el valor maximo para que todos los valores esten en [0, 1].
    /// </summary>
    private static float[] NormalizarEspectro(float[] espectro)
    {
        float maximo = 0f;
        foreach (float valor in espectro)
        {
            if (valor > maximo)
            {
                maximo = valor;
            }
        }

        if (maximo <= 0f)
        {
            return espectro;
        }

        float[] normalizado = new float[espectro.Length];
        for (int i = 0; i < espectro.Length; i++)
        {
            normalizado[i] = espectro[i] / maximo;
        }

        return normalizado;
    }

    /// <summary>
    /// Aplica la funcion softmax a las puntuaciones para obtener probabilidades.
    /// </summary>
    private static float[] AplicarSoftmax(float[] puntuaciones)
    {
        if (puntuaciones.Length == 0)
        {
            return puntuaciones;
        }

        float maximo = puntuaciones.Max();
        float[] exponenciales = new float[puntuaciones.Length];

        for (int i = 0; i < puntuaciones.Length; i++)
        {
            exponenciales[i] = MathF.Exp(puntuaciones[i] - maximo);
        }

        float sumaExp = exponenciales.Sum();

        float[] resultado = new float[puntuaciones.Length];
        for (int i = 0; i < exponenciales.Length; i++)
        {
            resultado[i] = exponenciales[i] / sumaExp;
        }

        return resultado;
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
            _ => ModoOperacion.CW
        };
    }
}
