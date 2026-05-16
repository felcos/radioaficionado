using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.IA;

/// <summary>
/// Motor de inferencia basado en ONNX Runtime.
/// Gestiona sesiones de inferencia con cache concurrente y semaforos por modelo
/// para garantizar thread-safety. Implementa IDisposable para liberar sesiones.
/// </summary>
public sealed class MotorInferenciaOnnx : IMotorInferenciaOnnx
{
    private readonly ConcurrentDictionary<string, InferenceSession> _sesiones = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaforos = new();
    private readonly ConfiguracionOnnx _configuracion;
    private bool _disposed;

    /// <summary>
    /// Inicializa una nueva instancia del motor de inferencia ONNX con la configuracion especificada.
    /// </summary>
    /// <param name="configuracion">Configuracion de ONNX Runtime.</param>
    public MotorInferenciaOnnx(ConfiguracionOnnx configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        _configuracion = configuracion;
    }

    /// <inheritdoc />
    public Task<ResultadoInferencia> EjecutarInferenciaAsync(float[] entrada, string nombreModelo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        if (string.IsNullOrWhiteSpace(nombreModelo))
        {
            throw new ArgumentException("El nombre del modelo no puede ser nulo o vacio.", nameof(nombreModelo));
        }

        ct.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sesiones.TryGetValue(nombreModelo, out InferenceSession? sesion))
        {
            throw new InvalidOperationException($"El modelo '{nombreModelo}' no esta cargado. Llame a CargarModeloAsync primero.");
        }

        return EjecutarInferenciaInternaAsync(entrada, nombreModelo, sesion, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ResultadoInferencia>> EjecutarInferenciaLoteAsync(
        IReadOnlyList<float[]> entradas, string nombreModelo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entradas);

        if (string.IsNullOrWhiteSpace(nombreModelo))
        {
            throw new ArgumentException("El nombre del modelo no puede ser nulo o vacio.", nameof(nombreModelo));
        }

        ct.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (entradas.Count == 0)
        {
            return Array.Empty<ResultadoInferencia>();
        }

        if (!_sesiones.TryGetValue(nombreModelo, out InferenceSession? sesion))
        {
            throw new InvalidOperationException($"El modelo '{nombreModelo}' no esta cargado. Llame a CargarModeloAsync primero.");
        }

        List<ResultadoInferencia> resultados = new(entradas.Count);

        foreach (float[] entrada in entradas)
        {
            ct.ThrowIfCancellationRequested();
            ResultadoInferencia resultado = await EjecutarInferenciaInternaAsync(entrada, nombreModelo, sesion, ct)
                .ConfigureAwait(false);
            resultados.Add(resultado);
        }

        return resultados.AsReadOnly();
    }

    /// <inheritdoc />
    public bool ModeloEstaCargado(string nombreModelo)
    {
        if (string.IsNullOrWhiteSpace(nombreModelo))
        {
            return false;
        }

        return _sesiones.ContainsKey(nombreModelo);
    }

    /// <inheritdoc />
    public Task CargarModeloAsync(string rutaModelo, string nombreModelo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rutaModelo))
        {
            throw new ArgumentException("La ruta del modelo no puede ser nula o vacia.", nameof(rutaModelo));
        }

        if (string.IsNullOrWhiteSpace(nombreModelo))
        {
            throw new ArgumentException("El nombre del modelo no puede ser nulo o vacio.", nameof(nombreModelo));
        }

        ct.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!File.Exists(rutaModelo))
        {
            throw new FileNotFoundException($"No se encontro el archivo de modelo ONNX en '{rutaModelo}'.", rutaModelo);
        }

        SessionOptions opciones = CrearOpcionesDeSesion();
        InferenceSession sesion = new(rutaModelo, opciones);

        // Si ya existia una sesion con ese nombre, disponer la anterior
        if (_sesiones.TryGetValue(nombreModelo, out InferenceSession? sesionAnterior))
        {
            sesionAnterior.Dispose();
        }

        _sesiones[nombreModelo] = sesion;
        _semaforos.GetOrAdd(nombreModelo, _ => new SemaphoreSlim(1, 1));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void DescargarModelo(string nombreModelo)
    {
        if (string.IsNullOrWhiteSpace(nombreModelo))
        {
            return;
        }

        if (_sesiones.TryRemove(nombreModelo, out InferenceSession? sesion))
        {
            sesion.Dispose();
        }

        if (_semaforos.TryRemove(nombreModelo, out SemaphoreSlim? semaforo))
        {
            semaforo.Dispose();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ObtenerModelosCargados()
    {
        return _sesiones.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Libera todos los recursos, incluyendo todas las sesiones de inferencia cargadas.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (KeyValuePair<string, InferenceSession> par in _sesiones)
        {
            par.Value.Dispose();
        }

        _sesiones.Clear();

        foreach (KeyValuePair<string, SemaphoreSlim> par in _semaforos)
        {
            par.Value.Dispose();
        }

        _semaforos.Clear();
    }

    /// <summary>
    /// Ejecuta la inferencia internamente, usando un semaforo para garantizar thread-safety por modelo.
    /// </summary>
    private async Task<ResultadoInferencia> EjecutarInferenciaInternaAsync(
        float[] entrada, string nombreModelo, InferenceSession sesion, CancellationToken ct)
    {
        SemaphoreSlim semaforo = _semaforos.GetOrAdd(nombreModelo, _ => new SemaphoreSlim(1, 1));

        await semaforo.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            Stopwatch cronometro = Stopwatch.StartNew();

            // Obtener el nombre de la entrada del modelo
            string nombreEntrada = sesion.InputNames[0];
            int[] dimensiones = [1, entrada.Length];
            DenseTensor<float> tensor = new(entrada, dimensiones);

            List<NamedOnnxValue> entradas = [NamedOnnxValue.CreateFromTensor(nombreEntrada, tensor)];

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> resultados = sesion.Run(entradas);

            cronometro.Stop();

            // Extraer el primer resultado
            DisposableNamedOnnxValue primerResultado = resultados.First();
            float[] salida = primerResultado.AsEnumerable<float>().ToArray();

            return new ResultadoInferencia(
                salida,
                nombreModelo,
                cronometro.Elapsed,
                null);
        }
        finally
        {
            semaforo.Release();
        }
    }

    /// <summary>
    /// Crea las opciones de sesion de ONNX Runtime basandose en la configuracion.
    /// </summary>
    private SessionOptions CrearOpcionesDeSesion()
    {
        SessionOptions opciones = new();

        opciones.IntraOpNumThreads = _configuracion.HilosInferencia;
        opciones.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

        return opciones;
    }
}
