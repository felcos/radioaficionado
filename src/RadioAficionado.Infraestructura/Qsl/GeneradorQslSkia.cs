using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Qsl;
using SkiaSharp;

namespace RadioAficionado.Infraestructura.Qsl;

/// <summary>
/// Implementación del generador de tarjetas QSL digitales utilizando SkiaSharp para el renderizado.
/// Genera imágenes con los datos del contacto, indicativos, información del operador y borde decorativo.
/// </summary>
public class GeneradorQslSkia : IGeneradorQsl
{
    private static readonly IReadOnlyList<PlantillaQsl> _plantillasPredefinidas = new List<PlantillaQsl>
    {
        new("Clasica",
            AnchoPixeles: 800,
            AltoPixeles: 500,
            ColorFondo: "#1A237E",
            ColorTexto: "#FFFFFF",
            MostrarMapa: false),

        new("Moderna",
            AnchoPixeles: 800,
            AltoPixeles: 500,
            ColorFondo: "#0D47A1",
            ColorTexto: "#E3F2FD",
            MostrarMapa: false),

        new("Minimalista",
            AnchoPixeles: 800,
            AltoPixeles: 500,
            ColorFondo: "#FFFFFF",
            ColorTexto: "#212121",
            MostrarMapa: false)
    };

    /// <inheritdoc />
    public Task<byte[]> GenerarAsync(DatosQsl datos, PlantillaQsl plantilla, FormatoExportacion formato)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ArgumentNullException.ThrowIfNull(plantilla);

        if (formato is not (FormatoExportacion.Png or FormatoExportacion.Jpg))
        {
            throw new NotSupportedException(
                $"El formato '{formato}' no está soportado actualmente. Use Png o Jpg.");
        }

        byte[] resultado = RenderizarTarjeta(datos, plantilla, formato);
        return Task.FromResult(resultado);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PlantillaQsl>> ObtenerPlantillasAsync()
    {
        return Task.FromResult(_plantillasPredefinidas);
    }

    /// <summary>
    /// Renderiza la tarjeta QSL completa usando SkiaSharp.
    /// </summary>
    /// <param name="datos">Datos del QSO y operador.</param>
    /// <param name="plantilla">Plantilla visual.</param>
    /// <param name="formato">Formato de exportación.</param>
    /// <returns>Array de bytes con la imagen renderizada.</returns>
    private static byte[] RenderizarTarjeta(DatosQsl datos, PlantillaQsl plantilla, FormatoExportacion formato)
    {
        using SKBitmap bitmap = new(plantilla.AnchoPixeles, plantilla.AltoPixeles);
        using SKCanvas lienzo = new(bitmap);

        DibujarFondo(lienzo, plantilla);
        DibujarBordeDecorativo(lienzo, plantilla);
        DibujarIndicativoPropio(lienzo, datos, plantilla);
        DibujarConfirmacionContacto(lienzo, datos, plantilla);
        DibujarDatosQso(lienzo, datos, plantilla);
        DibujarInformacionOperador(lienzo, datos, plantilla);
        DibujarPieDeTarjeta(lienzo, plantilla);

        SKEncodedImageFormat formatoSkia = formato == FormatoExportacion.Jpg
            ? SKEncodedImageFormat.Jpeg
            : SKEncodedImageFormat.Png;

        int calidad = formato == FormatoExportacion.Jpg ? 90 : 100;

        using SKImage imagen = SKImage.FromBitmap(bitmap);
        using SKData datosImagen = imagen.Encode(formatoSkia, calidad);

        return datosImagen.ToArray();
    }

    /// <summary>
    /// Dibuja el fondo de la tarjeta (color sólido o degradado según la plantilla).
    /// </summary>
    private static void DibujarFondo(SKCanvas lienzo, PlantillaQsl plantilla)
    {
        SKColor colorFondo = SKColor.Parse(plantilla.ColorFondo);

        if (plantilla.Nombre == "Moderna")
        {
            // Degradado para la plantilla moderna
            SKColor colorInicio = colorFondo;
            SKColor colorFin = new((byte)Math.Min(colorFondo.Red + 60, 255),
                                    (byte)Math.Min(colorFondo.Green + 40, 255),
                                    (byte)Math.Min(colorFondo.Blue + 30, 255));

            using SKPaint pinturaGradiente = new();
            using SKShader shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(plantilla.AnchoPixeles, plantilla.AltoPixeles),
                new[] { colorInicio, colorFin },
                null,
                SKShaderTileMode.Clamp);

            pinturaGradiente.Shader = shader;
            lienzo.DrawRect(0, 0, plantilla.AnchoPixeles, plantilla.AltoPixeles, pinturaGradiente);
        }
        else
        {
            using SKPaint pinturaFondo = new()
            {
                Color = colorFondo,
                Style = SKPaintStyle.Fill
            };

            lienzo.DrawRect(0, 0, plantilla.AnchoPixeles, plantilla.AltoPixeles, pinturaFondo);
        }
    }

    /// <summary>
    /// Dibuja un borde decorativo alrededor de la tarjeta.
    /// Para la plantilla clásica usa color dorado, para las demás un borde sutil.
    /// </summary>
    private static void DibujarBordeDecorativo(SKCanvas lienzo, PlantillaQsl plantilla)
    {
        SKColor colorBorde = plantilla.Nombre == "Clasica"
            ? SKColor.Parse("#FFD700") // Dorado
            : SKColor.Parse(plantilla.ColorTexto).WithAlpha(80);

        float grosorBorde = plantilla.Nombre == "Clasica" ? 4f : 2f;
        float margen = 10f;

        using SKPaint pinturaBorde = new()
        {
            Color = colorBorde,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = grosorBorde,
            IsAntialias = true
        };

        lienzo.DrawRect(
            margen,
            margen,
            plantilla.AnchoPixeles - (margen * 2),
            plantilla.AltoPixeles - (margen * 2),
            pinturaBorde);

        // Borde interior para la plantilla clásica
        if (plantilla.Nombre == "Clasica")
        {
            float margenInterior = 18f;
            using SKPaint pinturaBordeInterior = new()
            {
                Color = colorBorde.WithAlpha(120),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                IsAntialias = true
            };

            lienzo.DrawRect(
                margenInterior,
                margenInterior,
                plantilla.AnchoPixeles - (margenInterior * 2),
                plantilla.AltoPixeles - (margenInterior * 2),
                pinturaBordeInterior);
        }
    }

    /// <summary>
    /// Dibuja el indicativo propio del operador de forma prominente en la parte superior.
    /// </summary>
    private static void DibujarIndicativoPropio(SKCanvas lienzo, DatosQsl datos, PlantillaQsl plantilla)
    {
        SKColor colorTexto = SKColor.Parse(plantilla.ColorTexto);

        using SKTypeface tipoLetra = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        using SKFont fuente = new(tipoLetra, 52f) { Embolden = true };
        using SKPaint pinturaIndicativo = new()
        {
            Color = colorTexto,
            IsAntialias = true
        };

        float centroX = plantilla.AnchoPixeles / 2f;
        lienzo.DrawText(datos.IndicativoPropio, centroX, 70f, SKTextAlign.Center, fuente, pinturaIndicativo);
    }

    /// <summary>
    /// Dibuja el texto de confirmación del contacto con el indicativo de la otra estación.
    /// </summary>
    private static void DibujarConfirmacionContacto(SKCanvas lienzo, DatosQsl datos, PlantillaQsl plantilla)
    {
        SKColor colorTexto = SKColor.Parse(plantilla.ColorTexto);

        using SKTypeface tipoLetra = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using SKFont fuente = new(tipoLetra, 22f);
        using SKPaint pinturaConfirmacion = new()
        {
            Color = colorTexto.WithAlpha(220),
            IsAntialias = true
        };

        float centroX = plantilla.AnchoPixeles / 2f;
        string textoConfirmacion = $"Confirmando QSO con {datos.Contacto.IndicativoContacto}";
        lienzo.DrawText(textoConfirmacion, centroX, 105f, SKTextAlign.Center, fuente, pinturaConfirmacion);
    }

    /// <summary>
    /// Dibuja los datos técnicos del QSO: fecha, hora, frecuencia/banda, modo y señal.
    /// </summary>
    private static void DibujarDatosQso(SKCanvas lienzo, DatosQsl datos, PlantillaQsl plantilla)
    {
        SKColor colorTexto = SKColor.Parse(plantilla.ColorTexto);
        float inicioY = 160f;
        float inicioX = 50f;
        float espaciado = 32f;

        using SKTypeface tipoLetraNegrita = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        using SKFont fuenteEtiqueta = new(tipoLetraNegrita, 16f) { Embolden = true };
        using SKPaint pinturaEtiqueta = new()
        {
            Color = plantilla.Nombre == "Clasica"
                ? SKColor.Parse("#FFD700")
                : colorTexto.WithAlpha(180),
            IsAntialias = true
        };

        using SKTypeface tipoLetraNormal = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using SKFont fuenteValor = new(tipoLetraNormal, 18f);
        using SKPaint pinturaValor = new()
        {
            Color = colorTexto,
            IsAntialias = true
        };

        // Fecha
        DibujarCampo(lienzo, "FECHA:", datos.Contacto.FechaHoraInicio.ToString("dd/MM/yyyy"),
            inicioX, inicioY, fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);

        // Hora UTC
        DibujarCampo(lienzo, "HORA UTC:", datos.Contacto.FechaHoraInicio.ToString("HH:mm"),
            inicioX, inicioY + espaciado, fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);

        // Frecuencia y banda
        string textoFrecuencia = datos.Contacto.Frecuencia.ToString();
        BandaRadio? banda = datos.Contacto.Frecuencia.ObtenerBanda();
        if (banda.HasValue)
        {
            textoFrecuencia += $" ({banda.Value.ObtenerNombre()})";
        }

        DibujarCampo(lienzo, "FRECUENCIA:", textoFrecuencia,
            inicioX, inicioY + (espaciado * 2), fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);

        // Modo
        DibujarCampo(lienzo, "MODO:", datos.Contacto.Modo.ToString(),
            inicioX, inicioY + (espaciado * 3), fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);

        // Señal enviada
        string textoSenal = $"TX: {datos.Contacto.SenalEnviada}";
        if (!string.IsNullOrWhiteSpace(datos.Contacto.SenalRecibida))
        {
            textoSenal += $"  /  RX: {datos.Contacto.SenalRecibida}";
        }

        DibujarCampo(lienzo, "SEÑAL:", textoSenal,
            inicioX, inicioY + (espaciado * 4), fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);

        // Potencia (si está disponible)
        if (datos.Contacto.Potencia.HasValue)
        {
            DibujarCampo(lienzo, "POTENCIA:", $"{datos.Contacto.Potencia.Value:F0} W",
                inicioX, inicioY + (espaciado * 5), fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);
        }
    }

    /// <summary>
    /// Dibuja la información del operador: nombre, localizador, ciudad y país.
    /// </summary>
    private static void DibujarInformacionOperador(SKCanvas lienzo, DatosQsl datos, PlantillaQsl plantilla)
    {
        SKColor colorTexto = SKColor.Parse(plantilla.ColorTexto);
        float inicioX = 450f;
        float inicioY = 160f;
        float espaciado = 32f;

        using SKTypeface tipoLetraNegrita = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        using SKFont fuenteEtiqueta = new(tipoLetraNegrita, 16f) { Embolden = true };
        using SKPaint pinturaEtiqueta = new()
        {
            Color = plantilla.Nombre == "Clasica"
                ? SKColor.Parse("#FFD700")
                : colorTexto.WithAlpha(180),
            IsAntialias = true
        };

        using SKTypeface tipoLetraNormal = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using SKFont fuenteValor = new(tipoLetraNormal, 18f);
        using SKPaint pinturaValor = new()
        {
            Color = colorTexto,
            IsAntialias = true
        };

        int lineaActual = 0;

        // Nombre del operador
        DibujarCampo(lienzo, "OPERADOR:", datos.NombreOperador,
            inicioX, inicioY + (espaciado * lineaActual), fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);
        lineaActual++;

        // Localizador
        if (!string.IsNullOrWhiteSpace(datos.Localizador))
        {
            DibujarCampo(lienzo, "LOCALIZADOR:", datos.Localizador,
                inicioX, inicioY + (espaciado * lineaActual), fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);
            lineaActual++;
        }

        // Ciudad
        if (!string.IsNullOrWhiteSpace(datos.Ciudad))
        {
            DibujarCampo(lienzo, "CIUDAD:", datos.Ciudad,
                inicioX, inicioY + (espaciado * lineaActual), fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);
            lineaActual++;
        }

        // País
        if (!string.IsNullOrWhiteSpace(datos.Pais))
        {
            DibujarCampo(lienzo, "PAÍS:", datos.Pais,
                inicioX, inicioY + (espaciado * lineaActual), fuenteEtiqueta, pinturaEtiqueta, fuenteValor, pinturaValor);
        }
    }

    /// <summary>
    /// Dibuja el pie de la tarjeta con texto de confirmación estándar QSL.
    /// </summary>
    private static void DibujarPieDeTarjeta(SKCanvas lienzo, PlantillaQsl plantilla)
    {
        SKColor colorTexto = SKColor.Parse(plantilla.ColorTexto);

        using SKTypeface tipoLetra = SKTypeface.FromFamilyName("Arial", SKFontStyle.Italic);
        using SKFont fuente = new(tipoLetra, 14f);
        using SKPaint pinturaPie = new()
        {
            Color = colorTexto.WithAlpha(150),
            IsAntialias = true
        };

        float centroX = plantilla.AnchoPixeles / 2f;
        float posicionY = plantilla.AltoPixeles - 30f;

        lienzo.DrawText("Confirmo QSO — 73 de parte de mi estación", centroX, posicionY, SKTextAlign.Center, fuente, pinturaPie);
    }

    /// <summary>
    /// Dibuja un campo con etiqueta y valor en las coordenadas especificadas.
    /// </summary>
    /// <param name="lienzo">Canvas de SkiaSharp.</param>
    /// <param name="etiqueta">Texto de la etiqueta (por ejemplo, "FECHA:").</param>
    /// <param name="valor">Valor del campo.</param>
    /// <param name="x">Coordenada X.</param>
    /// <param name="y">Coordenada Y.</param>
    /// <param name="fuenteEtiqueta">Fuente para la etiqueta.</param>
    /// <param name="pinturaEtiqueta">Pintura para la etiqueta.</param>
    /// <param name="fuenteValor">Fuente para el valor.</param>
    /// <param name="pinturaValor">Pintura para el valor.</param>
    private static void DibujarCampo(
        SKCanvas lienzo,
        string etiqueta,
        string valor,
        float x,
        float y,
        SKFont fuenteEtiqueta,
        SKPaint pinturaEtiqueta,
        SKFont fuenteValor,
        SKPaint pinturaValor)
    {
        lienzo.DrawText(etiqueta, x, y, SKTextAlign.Left, fuenteEtiqueta, pinturaEtiqueta);
        float anchoEtiqueta = fuenteEtiqueta.MeasureText(etiqueta, pinturaEtiqueta);
        lienzo.DrawText(valor, x + anchoEtiqueta + 8f, y, SKTextAlign.Left, fuenteValor, pinturaValor);
    }
}
