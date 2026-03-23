using System.Text;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales.Cw;

/// <summary>
/// Decodificador de CW (codigo Morse) que implementa <see cref="IDecodificadorDigital"/>.
/// Pipeline de procesamiento:
/// 1. Recibe bloques de audio PCM de 16 bits.
/// 2. Aplica el filtro de Goertzel para detectar el tono CW.
/// 3. Determina si el tono esta presente o ausente usando un umbral adaptativo.
/// 4. Mide la duracion de marks (tono ON) y spaces (tono OFF).
/// 5. Clasifica: dit ~1 unidad, dah ~3 unidades, espacio entre letras ~3 unidades, espacio entre palabras ~7 unidades.
/// 6. Convierte los patrones Morse a caracteres usando <see cref="TablaMorse"/>.
/// </summary>
public sealed class DecodificadorCw : IDecodificadorDigital
{
    private readonly ConfiguracionCw _configuracion;
    private readonly StringBuilder _patronActual = new();
    private readonly StringBuilder _textoDecodificado = new();
    private readonly List<MensajeDecodificado> _mensajesPendientes = new();

    // Estado de deteccion de tono
    private bool _tonoPresente;
    private int _contadorMuestrasEstado;
    private double _magnitudMedia;
    private bool _magnitudMediaInicializada;

    // Estado de velocidad adaptativa
    private double _duracionDitEstimada;
    private readonly List<double> _duracionesRecientes = new();
    private const int MaxDuracionesParaEstimacion = 30;

    // Estado del decodificador
    private bool _estaActivo;
    private bool _dispuesto;

    /// <inheritdoc />
    public ModoOperacion Modo => ModoOperacion.CW;

    /// <inheritdoc />
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } = new List<SubModoOperacion>
    {
        SubModoOperacion.PCW
    }.AsReadOnly();

    /// <inheritdoc />
    public bool EstaActivo => _estaActivo;

    /// <inheritdoc />
    public int TasaDeMuestreoRequeridaHz => _configuracion.FrecuenciaMuestreo;

    /// <inheritdoc />
    public event EventHandler<MensajeDecodificado>? MensajeDecodificadoRecibido;

    /// <summary>
    /// Crea una nueva instancia del decodificador CW con la configuracion especificada.
    /// </summary>
    /// <param name="configuracion">Configuracion del decodificador. Si es null se usa la configuracion por defecto.</param>
    public DecodificadorCw(ConfiguracionCw? configuracion = null)
    {
        _configuracion = configuracion ?? new ConfiguracionCw();

        // Calcular duracion estimada de un dit en muestras basado en WPM inicial.
        // La formula estandar PARIS: 1 dit = 1200ms / WPM
        double duracionDitMs = 1200.0 / _configuracion.VelocidadInicialWpm;
        int muestrasPorBloque = _configuracion.FrecuenciaMuestreo * _configuracion.TamanoBloqueMilisegundos / 1000;
        _duracionDitEstimada = duracionDitMs / _configuracion.TamanoBloqueMilisegundos;

        if (muestrasPorBloque <= 0)
        {
            muestrasPorBloque = _configuracion.FrecuenciaMuestreo / 100; // fallback: 10ms
        }
    }

    /// <inheritdoc />
    public Task IniciarAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        _estaActivo = true;
        _patronActual.Clear();
        _textoDecodificado.Clear();
        _mensajesPendientes.Clear();
        _tonoPresente = false;
        _contadorMuestrasEstado = 0;
        _magnitudMedia = 0.0;
        _magnitudMediaInicializada = false;
        _duracionesRecientes.Clear();

        double duracionDitMs = 1200.0 / _configuracion.VelocidadInicialWpm;
        _duracionDitEstimada = duracionDitMs / _configuracion.TamanoBloqueMilisegundos;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DetenerAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        // Finalizar cualquier caracter en progreso
        FinalizarCaracterActual();
        FinalizarPalabra();

        _estaActivo = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MensajeDecodificado>> ProcesarAudioAsync(MuestraAudio muestra, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        if (!_estaActivo)
        {
            return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(Array.Empty<MensajeDecodificado>());
        }

        List<MensajeDecodificado> mensajes = new();

        ReadOnlySpan<short> datos = muestra.Datos.Span;
        int muestrasPorBloque = _configuracion.FrecuenciaMuestreo * _configuracion.TamanoBloqueMilisegundos / 1000;

        if (muestrasPorBloque <= 0)
        {
            muestrasPorBloque = datos.Length;
        }

        // Procesar en sub-bloques para mayor resolucion temporal
        int posicion = 0;
        while (posicion + muestrasPorBloque <= datos.Length)
        {
            ReadOnlySpan<short> bloque = datos.Slice(posicion, muestrasPorBloque);
            ProcesarBloque(bloque, muestra.MarcaDeTiempo, mensajes);
            posicion += muestrasPorBloque;
        }

        // Procesar bloque residual si hay suficientes muestras (al menos 50%)
        if (posicion < datos.Length && (datos.Length - posicion) >= muestrasPorBloque / 2)
        {
            ReadOnlySpan<short> bloqueResidual = datos.Slice(posicion);
            ProcesarBloque(bloqueResidual, muestra.MarcaDeTiempo, mensajes);
        }

        return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(mensajes.AsReadOnly());
    }

    /// <summary>
    /// Procesa un sub-bloque de muestras para detectar tono y decodificar Morse.
    /// </summary>
    private void ProcesarBloque(ReadOnlySpan<short> bloque, DateTimeOffset marcaDeTiempo, List<MensajeDecodificado> mensajes)
    {
        double magnitud = FiltroGoertzel.CalcularMagnitud(bloque, _configuracion.FrecuenciaTono, _configuracion.FrecuenciaMuestreo);

        // Actualizar media movil exponencial de la magnitud para umbral adaptativo
        if (!_magnitudMediaInicializada)
        {
            _magnitudMedia = magnitud;
            _magnitudMediaInicializada = true;
        }
        else
        {
            _magnitudMedia = _configuracion.FactorSuavizadoUmbral * _magnitudMedia
                + (1.0 - _configuracion.FactorSuavizadoUmbral) * magnitud;
        }

        // Determinar si el tono esta presente
        double umbralActual = _magnitudMedia * (1.0 + _configuracion.UmbralDeteccion);
        bool tonoDetectado = magnitud > umbralActual && magnitud > 1e-6;

        if (tonoDetectado == _tonoPresente)
        {
            // El estado no cambio — incrementar contador
            _contadorMuestrasEstado++;
        }
        else
        {
            // Transicion de estado detectada
            double duracionBloques = _contadorMuestrasEstado;

            if (_tonoPresente)
            {
                // Fin de mark (tono estaba presente, ahora silencio)
                ProcesarMark(duracionBloques);
            }
            else
            {
                // Fin de space (silencio terminado, tono aparece)
                ProcesarSpace(duracionBloques, marcaDeTiempo, mensajes);
            }

            _tonoPresente = tonoDetectado;
            _contadorMuestrasEstado = 1;
        }
    }

    /// <summary>
    /// Procesa el fin de un mark (periodo con tono presente).
    /// Clasifica como dit o dah y lo agrega al patron actual.
    /// </summary>
    private void ProcesarMark(double duracionBloques)
    {
        if (duracionBloques < 1)
        {
            return;
        }

        // Registrar duracion para estimacion adaptativa
        _duracionesRecientes.Add(duracionBloques);
        if (_duracionesRecientes.Count > MaxDuracionesParaEstimacion)
        {
            _duracionesRecientes.RemoveAt(0);
        }

        ActualizarVelocidadAdaptativa();

        // Clasificar: dit si duracion < 2 * ditEstimado, dah si >= 2
        double umbralDitDah = _duracionDitEstimada * 2.0;

        if (duracionBloques < umbralDitDah)
        {
            _patronActual.Append('.');
        }
        else
        {
            _patronActual.Append('-');
        }
    }

    /// <summary>
    /// Procesa el fin de un space (periodo sin tono).
    /// Determina si es espacio entre elementos, entre letras o entre palabras.
    /// </summary>
    private void ProcesarSpace(double duracionBloques, DateTimeOffset marcaDeTiempo, List<MensajeDecodificado> mensajes)
    {
        if (duracionBloques < 1)
        {
            return;
        }

        // Umbrales basados en la duracion estimada del dit:
        // Espacio entre elementos (intra-caracter): ~1 dit
        // Espacio entre letras: ~3 dits
        // Espacio entre palabras: ~7 dits
        double umbralLetra = _duracionDitEstimada * 2.0;
        double umbralPalabra = _duracionDitEstimada * 5.0;

        if (duracionBloques >= umbralPalabra)
        {
            // Espacio entre palabras
            FinalizarCaracterActual();
            FinalizarPalabra();

            // Emitir mensaje si hay texto acumulado
            string textoActual = _textoDecodificado.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(textoActual))
            {
                MensajeDecodificado mensaje = CrearMensaje(textoActual, marcaDeTiempo);
                mensajes.Add(mensaje);
                MensajeDecodificadoRecibido?.Invoke(this, mensaje);
                _textoDecodificado.Clear();
            }
        }
        else if (duracionBloques >= umbralLetra)
        {
            // Espacio entre letras
            FinalizarCaracterActual();
        }
        // Si duracion < umbralLetra, es espacio entre elementos dentro de un caracter — no hacer nada
    }

    /// <summary>
    /// Convierte el patron Morse acumulado en un caracter y lo agrega al texto decodificado.
    /// </summary>
    private void FinalizarCaracterActual()
    {
        if (_patronActual.Length == 0)
        {
            return;
        }

        string patron = _patronActual.ToString();
        char? caracter = TablaMorse.ConvertirACaracter(patron);

        if (caracter.HasValue)
        {
            _textoDecodificado.Append(caracter.Value);
        }
        else
        {
            // Patron no reconocido — agregar marcador
            _textoDecodificado.Append('?');
        }

        _patronActual.Clear();
    }

    /// <summary>
    /// Agrega un espacio al texto decodificado para separar palabras.
    /// </summary>
    private void FinalizarPalabra()
    {
        if (_textoDecodificado.Length > 0 && _textoDecodificado[_textoDecodificado.Length - 1] != ' ')
        {
            _textoDecodificado.Append(' ');
        }
    }

    /// <summary>
    /// Actualiza la estimacion de velocidad WPM basandose en las duraciones recientes de marks.
    /// Usa la media de las duraciones mas cortas (que probablemente sean dits) como referencia.
    /// </summary>
    private void ActualizarVelocidadAdaptativa()
    {
        if (_duracionesRecientes.Count < 4)
        {
            return;
        }

        // Ordenar las duraciones y tomar el percentil 25 como estimacion del dit
        List<double> duracionesOrdenadas = new(_duracionesRecientes);
        duracionesOrdenadas.Sort();

        int indiceCuartil = duracionesOrdenadas.Count / 4;
        if (indiceCuartil < 1)
        {
            indiceCuartil = 1;
        }

        double sumaDits = 0.0;
        for (int i = 0; i < indiceCuartil; i++)
        {
            sumaDits += duracionesOrdenadas[i];
        }

        double nuevaDuracionDit = sumaDits / indiceCuartil;

        // Suavizar el cambio para evitar oscilaciones bruscas
        if (nuevaDuracionDit > 0.5)
        {
            _duracionDitEstimada = 0.7 * _duracionDitEstimada + 0.3 * nuevaDuracionDit;
        }
    }

    /// <summary>
    /// Crea un <see cref="MensajeDecodificado"/> con el texto proporcionado.
    /// </summary>
    private MensajeDecodificado CrearMensaje(string texto, DateTimeOffset marcaDeTiempo)
    {
        return new MensajeDecodificado(
            marcaDeTiempo: marcaDeTiempo,
            frecuenciaAudioHz: (int)_configuracion.FrecuenciaTono,
            snr: 0,
            deltaTiempo: 0.0,
            modo: ModoOperacion.CW,
            texto: texto,
            subModo: SubModoOperacion.PCW
        );
    }

    /// <summary>
    /// Obtiene el texto actualmente acumulado en el buffer de decodificacion (para diagnostico).
    /// </summary>
    /// <returns>Texto decodificado hasta el momento.</returns>
    public string ObtenerTextoEnBuffer()
    {
        return _textoDecodificado.ToString();
    }

    /// <summary>
    /// Obtiene el patron Morse parcial del caracter en curso (para diagnostico).
    /// </summary>
    /// <returns>Patron Morse parcial del caracter actual.</returns>
    public string ObtenerPatronActual()
    {
        return _patronActual.ToString();
    }

    /// <summary>
    /// Obtiene la velocidad estimada actual en WPM.
    /// </summary>
    /// <returns>Velocidad estimada en palabras por minuto.</returns>
    public double ObtenerVelocidadEstimadaWpm()
    {
        double duracionDitMs = _duracionDitEstimada * _configuracion.TamanoBloqueMilisegundos;
        if (duracionDitMs <= 0)
        {
            return _configuracion.VelocidadInicialWpm;
        }

        return 1200.0 / duracionDitMs;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_dispuesto)
        {
            return;
        }

        _dispuesto = true;
        _estaActivo = false;
        _patronActual.Clear();
        _textoDecodificado.Clear();
        _mensajesPendientes.Clear();
        _duracionesRecientes.Clear();
    }
}
