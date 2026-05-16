using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Nativo.Dsp;

/// <summary>
/// Implementacion del servicio de waterfall en vivo.
/// Coordina el pipeline: IAudioPipeline → ProcesadorEspectro → LineaEspectro.
/// Se suscribe al pipeline de audio, procesa cada muestra con FFT
/// y dispara eventos con las lineas de espectro resultantes.
/// </summary>
public sealed class ServicioWaterfall : IServicioWaterfall
{
    private readonly IAudioPipeline _audioPipeline;
    private ProcesadorEspectro? _procesador;
    private Guid? _suscripcionAudio;
    private short[]? _bufferAcumulado;
    private int _posicionBuffer;
    private readonly object _bloqueo = new();
    private bool _descartado;

    /// <inheritdoc />
    public bool EstaActivo { get; private set; }

    /// <inheritdoc />
    public int TamanoFft { get; private set; }

    /// <inheritdoc />
    public int TasaDeMuestreoHz { get; private set; }

    /// <inheritdoc />
    public event EventHandler<LineaEspectroEventArgs>? LineaEspectroGenerada;

    /// <summary>
    /// Crea una nueva instancia del servicio de waterfall.
    /// </summary>
    /// <param name="audioPipeline">Pipeline de audio para suscribirse a las muestras.</param>
    public ServicioWaterfall(IAudioPipeline audioPipeline)
    {
        _audioPipeline = audioPipeline ?? throw new ArgumentNullException(nameof(audioPipeline));
    }

    /// <inheritdoc />
    public Task IniciarAsync(int tamanoFft = 2048, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_descartado, this);

        if (EstaActivo)
        {
            return Task.CompletedTask;
        }

        if (!_audioPipeline.EstaActivo)
        {
            throw new InvalidOperationException(
                "El pipeline de audio debe estar activo antes de iniciar el waterfall.");
        }

        TamanoFft = tamanoFft;
        TasaDeMuestreoHz = _audioPipeline.TasaDeMuestreoHz;
        _procesador = new ProcesadorEspectro(TasaDeMuestreoHz, tamanoFft);
        _bufferAcumulado = new short[tamanoFft];
        _posicionBuffer = 0;

        _suscripcionAudio = _audioPipeline.Suscribir(ProcesarMuestra);
        EstaActivo = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DetenerAsync(CancellationToken ct = default)
    {
        if (!EstaActivo)
        {
            return Task.CompletedTask;
        }

        if (_suscripcionAudio.HasValue)
        {
            _audioPipeline.Desuscribir(_suscripcionAudio.Value);
            _suscripcionAudio = null;
        }

        _procesador?.Dispose();
        _procesador = null;
        _bufferAcumulado = null;
        _posicionBuffer = 0;
        EstaActivo = false;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Procesa una muestra de audio recibida del pipeline.
    /// Acumula muestras hasta tener suficientes para una FFT completa,
    /// luego genera la linea de espectro y dispara el evento.
    /// </summary>
    /// <param name="muestra">Muestra de audio recibida.</param>
    private void ProcesarMuestra(MuestraAudio muestra)
    {
        if (!EstaActivo || _procesador is null || _bufferAcumulado is null)
        {
            return;
        }

        ReadOnlySpan<short> datos = muestra.Datos.Span;

        int posicionDatos = 0;

        while (posicionDatos < datos.Length)
        {
            int espacioDisponible = TamanoFft - _posicionBuffer;
            int muestrasACopiar = Math.Min(espacioDisponible, datos.Length - posicionDatos);

            lock (_bloqueo)
            {
                datos.Slice(posicionDatos, muestrasACopiar).CopyTo(
                    _bufferAcumulado.AsSpan(_posicionBuffer, muestrasACopiar));
                _posicionBuffer += muestrasACopiar;
            }

            posicionDatos += muestrasACopiar;

            if (_posicionBuffer >= TamanoFft)
            {
                GenerarLineaEspectro();
            }
        }
    }

    /// <summary>
    /// Genera una linea de espectro a partir del buffer acumulado
    /// y dispara el evento para que el waterfall se actualice.
    /// </summary>
    private void GenerarLineaEspectro()
    {
        if (_procesador is null || _bufferAcumulado is null)
        {
            return;
        }

        LineaEspectro linea;

        lock (_bloqueo)
        {
            linea = _procesador.Procesar(_bufferAcumulado.AsSpan(0, TamanoFft));

            // Mantener 50% de solapamiento: copiar segunda mitad al inicio
            int mitad = TamanoFft / 2;
            Array.Copy(_bufferAcumulado, mitad, _bufferAcumulado, 0, mitad);
            _posicionBuffer = mitad;
        }

        LineaEspectroEventArgs args = new()
        {
            MarcaDeTiempo = linea.MarcaDeTiempo,
            MagnitudesDb = linea.MagnitudesDb,
            ResolucionHz = linea.ResolucionHz,
            FrecuenciaMinHz = linea.FrecuenciaMinHz,
            FrecuenciaMaxHz = linea.FrecuenciaMaxHz
        };

        LineaEspectroGenerada?.Invoke(this, args);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_descartado)
        {
            _descartado = true;
            await DetenerAsync();
        }
    }
}
