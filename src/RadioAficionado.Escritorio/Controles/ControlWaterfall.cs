using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using RadioAficionado.Nativo.Dsp;
using SkiaSharp;

namespace RadioAficionado.Escritorio.Controles;

/// <summary>
/// Control personalizado que renderiza un waterfall de espectro de frecuencias usando SkiaSharp.
/// Cada llamada a <see cref="AgregarLinea"/> desplaza el contenido un píxel hacia abajo
/// y pinta la nueva línea de espectro en la fila superior con un gradiente de colores.
/// Diseñado para ~25 líneas/segundo (FFT cada 40 ms).
/// </summary>
public sealed class ControlWaterfall : Control
{
    /// <summary>
    /// Propiedad de Avalonia para el ancho de FFT (cantidad de bins).
    /// </summary>
    public static readonly StyledProperty<int> AnchoFftProperty =
        AvaloniaProperty.Register<ControlWaterfall, int>(nameof(AnchoFft), defaultValue: 1025);

    /// <summary>
    /// Propiedad de Avalonia para el valor mínimo de dB del rango visible.
    /// </summary>
    public static readonly StyledProperty<double> DbMinimoProperty =
        AvaloniaProperty.Register<ControlWaterfall, double>(nameof(DbMinimo), defaultValue: -120.0);

    /// <summary>
    /// Propiedad de Avalonia para el valor máximo de dB del rango visible.
    /// </summary>
    public static readonly StyledProperty<double> DbMaximoProperty =
        AvaloniaProperty.Register<ControlWaterfall, double>(nameof(DbMaximo), defaultValue: 0.0);

    private SKBitmap? _bitmapWaterfall;
    private readonly object _bloqueo = new();
    private bool _hayDatosNuevos;

    // Paleta de colores precalculada (256 entradas): negro → azul → verde → amarillo → rojo
    private static readonly SKColor[] _paletaColores = GenerarPaleta();

    /// <summary>
    /// Ancho de la FFT en bins de frecuencia.
    /// </summary>
    public int AnchoFft
    {
        get => GetValue(AnchoFftProperty);
        set => SetValue(AnchoFftProperty, value);
    }

    /// <summary>
    /// Valor mínimo de dB para el mapeo de colores (señales más débiles).
    /// </summary>
    public double DbMinimo
    {
        get => GetValue(DbMinimoProperty);
        set => SetValue(DbMinimoProperty, value);
    }

    /// <summary>
    /// Valor máximo de dB para el mapeo de colores (señales más fuertes).
    /// </summary>
    public double DbMaximo
    {
        get => GetValue(DbMaximoProperty);
        set => SetValue(DbMaximoProperty, value);
    }

    /// <summary>
    /// Agrega una línea de espectro al waterfall.
    /// Desplaza el contenido existente un píxel hacia abajo y pinta la nueva línea en la fila superior.
    /// Es thread-safe: puede llamarse desde cualquier hilo.
    /// </summary>
    /// <param name="linea">Datos del espectro a pintar.</param>
    public void AgregarLinea(LineaEspectro linea)
    {
        ArgumentNullException.ThrowIfNull(linea);

        lock (_bloqueo)
        {
            AsegurarBitmapCreado();

            if (_bitmapWaterfall is null)
            {
                return;
            }

            int anchoDelBitmap = _bitmapWaterfall.Width;
            int altoDelBitmap = _bitmapWaterfall.Height;

            // Desplazar todo el bitmap 1 píxel hacia abajo copiando filas de abajo hacia arriba
            // para evitar sobreescribir datos que aún no se copiaron.
            unsafe
            {
                byte* pixeles = (byte*)_bitmapWaterfall.GetPixels().ToPointer();
                int bytesPorFila = _bitmapWaterfall.RowBytes;

                for (int fila = altoDelBitmap - 1; fila > 0; fila--)
                {
                    byte* filaDestino = pixeles + fila * bytesPorFila;
                    byte* filaOrigen = pixeles + (fila - 1) * bytesPorFila;
                    Buffer.MemoryCopy(filaOrigen, filaDestino, bytesPorFila, bytesPorFila);
                }
            }

            // Pintar la nueva línea en la fila superior (fila 0)
            double dbMin = DbMinimo;
            double dbMax = DbMaximo;
            double rangoDb = dbMax - dbMin;

            if (rangoDb <= 0.0)
            {
                rangoDb = 1.0;
            }

            double[] magnitudes = linea.MagnitudesDb;
            int cantidadBins = magnitudes.Length;

            for (int x = 0; x < anchoDelBitmap; x++)
            {
                // Mapear la coordenada X del bitmap al bin de frecuencia correspondiente
                int indiceBin = cantidadBins * x / anchoDelBitmap;

                if (indiceBin >= cantidadBins)
                {
                    indiceBin = cantidadBins - 1;
                }

                double magnitudDb = magnitudes[indiceBin];

                // Normalizar al rango [0, 1]
                double normalizado = (magnitudDb - dbMin) / rangoDb;
                normalizado = Math.Clamp(normalizado, 0.0, 1.0);

                // Mapear a índice de paleta [0, 255]
                int indicePaleta = (int)(normalizado * 255.0);
                indicePaleta = Math.Clamp(indicePaleta, 0, 255);

                _bitmapWaterfall.SetPixel(x, 0, _paletaColores[indicePaleta]);
            }

            _hayDatosNuevos = true;
        }

        // Invalidar en el hilo de UI para forzar repintado
        Avalonia.Threading.Dispatcher.UIThread.Post(InvalidateVisual, Avalonia.Threading.DispatcherPriority.Render);
    }

    /// <summary>
    /// Renderiza el control usando una operación de dibujo personalizada de SkiaSharp.
    /// </summary>
    /// <param name="contexto">Contexto de dibujo de Avalonia.</param>
    public override void Render(DrawingContext contexto)
    {
        base.Render(contexto);

        lock (_bloqueo)
        {
            if (_bitmapWaterfall is null || !_hayDatosNuevos)
            {
                // Si no hay bitmap aún, dibujar fondo negro
                contexto.DrawRectangle(
                    Brushes.Black,
                    null,
                    new Rect(0, 0, Bounds.Width, Bounds.Height));
                return;
            }

            OperacionDibujoWaterfall operacion = new(
                new Rect(0, 0, Bounds.Width, Bounds.Height),
                _bitmapWaterfall);

            contexto.Custom(operacion);
        }
    }

    /// <summary>
    /// Libera los recursos del bitmap al descargar el control.
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        lock (_bloqueo)
        {
            _bitmapWaterfall?.Dispose();
            _bitmapWaterfall = null;
        }
    }

    /// <summary>
    /// Asegura que el bitmap interno esté creado con las dimensiones actuales del control.
    /// Debe llamarse dentro del bloqueo.
    /// </summary>
    private void AsegurarBitmapCreado()
    {
        int anchoDeseado = Math.Max((int)Bounds.Width, 1);
        int altoDeseado = Math.Max((int)Bounds.Height, 1);

        if (_bitmapWaterfall is not null
            && _bitmapWaterfall.Width == anchoDeseado
            && _bitmapWaterfall.Height == altoDeseado)
        {
            return;
        }

        SKBitmap? bitmapAnterior = _bitmapWaterfall;
        _bitmapWaterfall = new SKBitmap(anchoDeseado, altoDeseado, SKColorType.Bgra8888, SKAlphaType.Premul);
        _bitmapWaterfall.Erase(SKColors.Black);

        // Si había un bitmap anterior, copiar lo que quepa en el nuevo
        if (bitmapAnterior is not null)
        {
            using SKCanvas canvasTemporal = new(_bitmapWaterfall);
            canvasTemporal.DrawBitmap(bitmapAnterior, 0, 0);
            bitmapAnterior.Dispose();
        }
    }

    /// <summary>
    /// Genera una paleta de 256 colores con gradiente: negro → azul → verde → amarillo → rojo.
    /// </summary>
    /// <returns>Array de 256 colores SKColor.</returns>
    private static SKColor[] GenerarPaleta()
    {
        SKColor[] paleta = new SKColor[256];

        for (int i = 0; i < 256; i++)
        {
            double t = i / 255.0;

            byte r;
            byte g;
            byte b;

            if (t < 0.25)
            {
                // Negro → Azul (0.0 - 0.25)
                double segmento = t / 0.25;
                r = 0;
                g = 0;
                b = (byte)(segmento * 255);
            }
            else if (t < 0.50)
            {
                // Azul → Verde (0.25 - 0.50)
                double segmento = (t - 0.25) / 0.25;
                r = 0;
                g = (byte)(segmento * 255);
                b = (byte)((1.0 - segmento) * 255);
            }
            else if (t < 0.75)
            {
                // Verde → Amarillo (0.50 - 0.75)
                double segmento = (t - 0.50) / 0.25;
                r = (byte)(segmento * 255);
                g = 255;
                b = 0;
            }
            else
            {
                // Amarillo → Rojo (0.75 - 1.0)
                double segmento = (t - 0.75) / 0.25;
                r = 255;
                g = (byte)((1.0 - segmento) * 255);
                b = 0;
            }

            paleta[i] = new SKColor(r, g, b);
        }

        return paleta;
    }

    /// <summary>
    /// Operación de dibujo personalizada que pinta el bitmap del waterfall
    /// directamente en el SKCanvas de SkiaSharp para máximo rendimiento.
    /// </summary>
    private sealed class OperacionDibujoWaterfall : ICustomDrawOperation
    {
        private readonly Rect _limites;
        private readonly SKBitmap _bitmap;

        /// <summary>
        /// Crea una operación de dibujo para el waterfall.
        /// </summary>
        /// <param name="limites">Área del control donde se dibuja.</param>
        /// <param name="bitmap">Bitmap del waterfall a pintar.</param>
        public OperacionDibujoWaterfall(Rect limites, SKBitmap bitmap)
        {
            _limites = limites;
            _bitmap = bitmap;
        }

        /// <inheritdoc />
        public Rect Bounds => _limites;

        /// <inheritdoc />
        public bool HitTest(Point p) => false;

        /// <inheritdoc />
        public bool Equals(ICustomDrawOperation? other) => false;

        /// <inheritdoc />
        public void Dispose()
        {
            // No liberamos el bitmap aquí — lo gestiona el control padre.
        }

        /// <inheritdoc />
        public void Render(ImmediateDrawingContext contexto)
        {
            ISkiaSharpApiLeaseFeature? caracteristicaSkia =
                contexto.TryGetFeature<ISkiaSharpApiLeaseFeature>();

            if (caracteristicaSkia is null)
            {
                return;
            }

            using ISkiaSharpApiLease arrendamiento = caracteristicaSkia.Lease();
            SKCanvas canvas = arrendamiento.SkCanvas;

            SKRect rectDestino = new(
                (float)_limites.X,
                (float)_limites.Y,
                (float)_limites.Right,
                (float)_limites.Bottom);

            using SKPaint pintura = new()
            {
                FilterQuality = SKFilterQuality.Low, // Rápido, suficiente para waterfall
                IsAntialias = false
            };

            canvas.DrawBitmap(_bitmap, rectDestino, pintura);
        }
    }
}
